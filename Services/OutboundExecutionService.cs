using System.Data;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

/// <summary>
/// P0-04: Service layer for outbound posting workflow.
/// Encapsulates reservation consumption, stock deduction, and serial handling
/// with a single transaction boundary (P0-05).
/// </summary>
public interface IOutboundExecutionService
{
    Task<WorkflowResult> PostReservedOutboundAsync(
        long voucherId,
        bool cancelRemaining,
        int? scopedWarehouseId,
        string actor,
        string? ipAddress);

    Task<WorkflowResult> CreateWaveAsync(
        string waveProfile, string? carrierCode, string? carrierName,
        string? routeCode, DateTime? cutoffTime, WavePriorityEnum priority,
        long[] selectedVoucherIds, string? notes,
        int? scopedWarehouseId, string actor);

    Task<WorkflowResult> ReleaseVoucherForPickingAsync(
        long voucherId,
        int? scopedWarehouseId,
        string actor);

    Task<WorkflowResult> ConfirmPickTaskAsync(
        long taskId, decimal qty, string scannedValue,
        List<string>? serialCodes, string actor, bool canOverrideAssignee,
        string? toteCode = null,
        string? sourceLocationCode = null,
        string? targetLocationCode = null,
        bool reportShort = false);
}

public class OutboundExecutionService : IOutboundExecutionService
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryReservationService _reservationService;
    private readonly IInventoryBalanceService _inventoryBalanceService;
    private readonly ISerialInventoryService _serialInventoryService;
    private readonly IInventoryTransactionService _inventoryTransactionService;

    private static DateTime VietnamNow => VietnamTime.Now;

    public OutboundExecutionService(
        AppDbContext db,
        IUnitOfWork unitOfWork,
        IInventoryReservationService reservationService,
        IInventoryBalanceService inventoryBalanceService,
        ISerialInventoryService? serialInventoryService = null,
        IInventoryTransactionService? inventoryTransactionService = null)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _reservationService = reservationService;
        _inventoryBalanceService = inventoryBalanceService;
        _serialInventoryService = serialInventoryService ?? new SerialInventoryService(db);
        _inventoryTransactionService = inventoryTransactionService ?? new InventoryTransactionService(db);
    }

    public async Task<WorkflowResult> PostReservedOutboundAsync(
        long voucherId,
        bool cancelRemaining,
        int? scopedWarehouseId,
        string actor,
        string? ipAddress)
    {
        var voucher = await _db.Vouchers
            .Include(v => v.Details).ThenInclude(d => d.Item)
            .FirstOrDefaultAsync(v => v.VoucherId == voucherId);
        if (voucher == null)
            return WorkflowResult.Failure("Không tìm thấy phiếu.", "Details");
        if (voucher.IsCancelled)
            return WorkflowResult.Failure("Phiếu đã hủy.", "Details");
        if (voucher.IsPosted)
            return WorkflowResult.Failure("Phiếu đã ghi sổ.", "Details");
        if (voucher.VoucherType is not (VoucherTypeEnum.XuatKho or VoucherTypeEnum.TraNCC
            or VoucherTypeEnum.ChuyenKho or VoucherTypeEnum.XuatSanXuat))
            return WorkflowResult.Failure("Chỉ áp dụng cho phiếu outbound.", "Details");

        if (scopedWarehouseId.HasValue && voucher.WarehouseId != scopedWarehouseId.Value)
            return WorkflowResult.ForbiddenResult();

        var unfinishedSortTasks = await _db.PickTaskAllocations
            .Include(a => a.PickTask)
            .Where(a => a.VoucherId == voucher.VoucherId
                && a.PickTask != null
                && a.PickTask.PickTaskMode == PickTaskModeEnum.Sort
                && a.PickTask.Status != PickTaskStatusEnum.Completed)
            .CountAsync();
        if (unfinishedSortTasks > 0)
            return WorkflowResult.Failure("Phiếu còn hàng chưa phân loại xong nên chưa thể ghi sổ xuất.", "Details", new { id = voucherId });

        // Load all reservations for this voucher
        var allReservations = await _db.StockReservations
            .Where(r => r.VoucherId == voucher.VoucherId)
            .ToListAsync();
        var reservations = allReservations
            .Where(r => r.Status == ReservationStatusEnum.Active)
            .ToList();

        // Calculate picked totals from PickTaskAllocations (batch) and legacy PickTasks
        var totalReserved = reservations.Sum(r => r.ReservedQty);
        var allocationPickedTotal = await _db.PickTaskAllocations
            .Where(a => a.VoucherId == voucher.VoucherId)
            .SumAsync(a => (decimal?)a.PickedQty) ?? 0m;
        var legacyPickedTotal = await _db.PickTasks
            .Where(t => t.VoucherId == voucher.VoucherId
                && t.PickTaskMode == PickTaskModeEnum.Single
                && !t.Allocations.Any())
            .SumAsync(t => (decimal?)t.PickedQty) ?? 0m;
        var totalPicked = allocationPickedTotal + legacyPickedTotal;
        var totalConsumed = reservations.Sum(r => r.ConsumedQty);
        var pickedAndReady = totalPicked + totalConsumed;

        // Partial shipment gate
        if (!voucher.PartialShipmentAllowed && pickedAndReady < totalReserved && pickedAndReady > 0)
        {
            var shortfall = totalReserved - pickedAndReady;
            return WorkflowResult.Failure(
                $"Còn thiếu {shortfall:N2} đơn vị chưa được pick. Vui lòng pick đủ hoặc liên hệ quản lý để bật chế độ partial shipment.",
                "Details");
        }

        if (!allReservations.Any())
            return WorkflowResult.Failure("Không có reservation nào cho phiếu này.", "Details");

        // P0-05: Single transaction boundary for the entire workflow
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            using var ledgerScope = _inventoryTransactionService.BeginScope(new InventoryTransactionContext
            {
                TransactionType = voucher.VoucherType == VoucherTypeEnum.ChuyenKho
                    ? InventoryTransactionTypeEnum.TransferOut
                    : InventoryTransactionTypeEnum.Ship,
                TransactionGroupKey = $"voucher:{voucher.VoucherId}:outbound-post",
                IdempotencyKeyPrefix = $"voucher:{voucher.VoucherId}:outbound-post",
                WarehouseId = voucher.WarehouseId,
                OwnerPartnerId = voucher.OwnerPartnerId,
                VoucherId = voucher.VoucherId,
                ReferenceType = "Voucher",
                ReferenceId = voucher.VoucherId.ToString(),
                ReferenceCode = voucher.VoucherCode,
                Actor = actor
            });
            // Handle partial shipment backorder
            if (voucher.PartialShipmentAllowed && pickedAndReady < totalReserved && pickedAndReady > 0)
            {
                var shortfall = totalReserved - pickedAndReady;
                // FIX: Only release the UNPICKED portion of each reservation
                foreach (var r in reservations)
                {
                    var openQty = r.ReservedQty - r.ConsumedQty - r.ReleasedQty;
                    if (openQty <= 0) continue;

                    // Check how much was actually picked for this specific reservation
                    var pickedForThisRes = await GetPickedQtyForReservationAsync(r, voucher.VoucherId);
                    var unconsumedPicked = Math.Max(0, pickedForThisRes - r.ConsumedQty);
                    var releaseQty = Math.Max(0, openQty - unconsumedPicked);

                    if (releaseQty > 0)
                    {
                        await _serialInventoryService.ReleaseOpenForReservationAsync(r, actor, (int)Math.Ceiling(releaseQty), "Partial shipment backorder.");
                        r.ReleasedQty += releaseQty;
                        if (r.ReservedQty - r.ConsumedQty - r.ReleasedQty <= 0)
                            r.Status = ReservationStatusEnum.Released;
                        r.UpdatedAt = VietnamNow;

                        _db.AuditLogs.Add(new AuditLog
                        {
                            TableName = "StockReservation",
                            RecordId = r.StockReservationId.ToString(),
                            ActionType = "PARTIAL_SHIPMENT_BACKORDER",
                            ColumnChanged = "ReleasedQty",
                            OldValue = $"Reserved:{r.ReservedQty}",
                            NewValue = $"PartialShipped:{pickedAndReady};Released:{releaseQty}",
                            ChangedBy = actor,
                            ChangedAt = VietnamNow,
                            AppModule = "Outbound"
                        });
                    }
                }

                voucher.FulfillmentStatus = FulfillmentStatusEnum.PartiallyIssued;
            }

            // Refresh active reservations after partial release
            reservations = allReservations
                .Where(r => r.Status == ReservationStatusEnum.Active)
                .ToList();

            // Idempotency guard
            var hasOpenBeforePost = allReservations.Any(r => (r.ReservedQty - r.ConsumedQty - r.ReleasedQty) > 0);
            if (!reservations.Any() || !hasOpenBeforePost)
            {
                voucher.IsPosted = true;
                voucher.FulfillmentStatus = FulfillmentStatusEnum.Completed;
                voucher.CompletedAt ??= VietnamNow;
                voucher.UpdatedAt = VietnamNow;
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
                return WorkflowResult.Success(
                    $"Phiếu outbound {voucher.VoucherCode} đã hoàn tất trước đó.",
                    "Details", new { id = voucherId });
            }

            // Build picked qty map from allocations + legacy tasks
            var allocationPickedByTaskKey = await _db.PickTaskAllocations
                .Where(a => a.VoucherId == voucher.VoucherId && a.StockReservation != null)
                .GroupBy(a => new
                {
                    a.VoucherDetailId,
                    a.StockReservation!.ItemId,
                    LocationId = a.StockReservation.LocationId,
                    a.StockReservation.LotNumber,
                    a.StockReservation.ExpiryDate
                })
                .Select(g => new PickedReservationQty(
                    g.Key.VoucherDetailId,
                    g.Key.ItemId,
                    g.Key.LocationId,
                    g.Key.LotNumber,
                    g.Key.ExpiryDate,
                    g.Sum(x => x.PickedQty)))
                .ToListAsync();

            var legacyPickedByTaskKey = await _db.PickTasks
                .Where(t => t.VoucherId == voucher.VoucherId
                    && t.PickTaskMode == PickTaskModeEnum.Single
                    && !t.Allocations.Any())
                .GroupBy(t => new { t.VoucherDetailId, t.ItemId, t.SourceLocationId, t.LotNumber, t.ExpiryDate })
                .Select(g => new PickedReservationQty(
                    g.Key.VoucherDetailId,
                    g.Key.ItemId,
                    g.Key.SourceLocationId,
                    g.Key.LotNumber,
                    g.Key.ExpiryDate,
                    g.Sum(x => x.PickedQty)))
                .ToListAsync();

            var pickedByTaskKey = allocationPickedByTaskKey
                .Concat(legacyPickedByTaskKey)
                .GroupBy(x => new { x.VoucherDetailId, x.ItemId, x.LocationId, x.LotNumber, x.ExpiryDate })
                .Select(g => new PickedReservationQty(
                    g.Key.VoucherDetailId,
                    g.Key.ItemId,
                    g.Key.LocationId,
                    g.Key.LotNumber,
                    g.Key.ExpiryDate,
                    g.Sum(x => x.PickedQty)))
                .ToList();

            var affectedItemLocationIds = new List<int>();
            var affectedItemIds = new HashSet<int>();
            var itemIds = reservations.Select(r => r.ItemId).Distinct().ToList();
            var items = await _db.Items.Where(i => itemIds.Contains(i.ItemId)).ToDictionaryAsync(i => i.ItemId, i => i);
            var runningStockByItem = await _inventoryBalanceService.GetStockByItemAsync(null, itemIds);
            await _serialInventoryService.BackfillPickedReservationsForVoucherAsync(voucher.VoucherId, actor);
            await _unitOfWork.SaveChangesAsync();

            var hasPostedAnyQty = false;

            foreach (var r in reservations)
            {
                var pending = r.ReservedQty - r.ConsumedQty - r.ReleasedQty;
                if (pending <= 0) continue;

                var pickedQty = pickedByTaskKey
                    .Where(x => x.VoucherDetailId == r.VoucherDetailId
                        && x.ItemId == r.ItemId
                        && x.LocationId == r.LocationId
                        && x.LotNumber == r.LotNumber
                        && x.ExpiryDate == r.ExpiryDate)
                    .Select(x => x.PickedQty)
                    .FirstOrDefault();

                var consumedBefore = allReservations
                    .Where(x => x.StockReservationId != r.StockReservationId
                        && x.VoucherDetailId == r.VoucherDetailId
                        && x.ItemId == r.ItemId
                        && x.LocationId == r.LocationId
                        && x.LotNumber == r.LotNumber
                        && x.ExpiryDate == r.ExpiryDate)
                    .Sum(x => x.ConsumedQty);

                var postableFromPicked = Math.Max(0, pickedQty - consumedBefore - r.ConsumedQty);
                var postQty = Math.Min(pending, postableFromPicked);

                if (postQty <= 0)
                {
                    if (cancelRemaining)
                    {
                        await _serialInventoryService.ReleaseOpenForReservationAsync(r, actor, (int)Math.Ceiling(pending), "Cancel remaining during outbound post.");
                        r.ReleasedQty += pending;
                        r.Status = ReservationStatusEnum.Released;
                        r.UpdatedAt = VietnamNow;
                        var releasedLoc = await _db.ItemLocations
                            .FirstOrDefaultAsync(il => il.ItemId == r.ItemId
                                && il.OwnerPartnerId == r.OwnerPartnerId
                                && il.LocationId == r.LocationId
                                && il.LotNumber == r.LotNumber
                                && il.ExpiryDate == r.ExpiryDate);
                        if (releasedLoc != null) affectedItemLocationIds.Add(releasedLoc.ItemLocationId);
                    }
                    continue;
                }

                var itemLoc = await _db.ItemLocations
                    .FirstOrDefaultAsync(il => il.ItemId == r.ItemId
                        && il.OwnerPartnerId == r.OwnerPartnerId
                        && il.LocationId == r.LocationId
                        && il.LotNumber == r.LotNumber
                        && il.ExpiryDate == r.ExpiryDate);
                if (itemLoc == null) throw WmsExceptions.UndoLocationNotFound(r.ItemId.ToString());
                if (itemLoc.Quantity < postQty) throw new BusinessRuleException("Tồn thực tế đã thay đổi, không đủ để post.");

                // Serial handling
                if (items.TryGetValue(r.ItemId, out var serialAwareItem) && serialAwareItem.TrackSerial)
                {
                    if (itemLoc.HoldStatus != InventoryHoldStatusEnum.Available)
                        throw WmsExceptions.HoldStatusBlocked(
                            itemLoc.Location?.LocationCode ?? r.LocationId.ToString(),
                            itemLoc.HoldStatus.ToString(), serialAwareItem.ItemCode);

                    if (postQty != decimal.Truncate(postQty))
                        throw WmsExceptions.SerialNotInteger(serialAwareItem.ItemCode);

                    var requiredSerialCount = (int)Math.Ceiling(postQty);
                    await _serialInventoryService.ConsumePickedSerialsForReservationAsync(r, voucher, requiredSerialCount, actor);
                }

                // Deduct ItemLocation quantity
                itemLoc.Quantity -= postQty;
                itemLoc.UpdatedAt = VietnamNow;
                affectedItemLocationIds.Add(itemLoc.ItemLocationId);
                affectedItemIds.Add(r.ItemId);
                hasPostedAnyQty = true;

                // Handle transfer destination
                if (voucher.VoucherType == VoucherTypeEnum.ChuyenKho)
                {
                    var targetDetail = voucher.Details.FirstOrDefault(d => d.VoucherDetailId == r.VoucherDetailId);
                    if (targetDetail?.DestLocationId == null)
                        throw WmsExceptions.TransferDestLocationMissing();

                    var dest = await _db.ItemLocations
                        .FirstOrDefaultAsync(il => il.ItemId == r.ItemId
                            && il.OwnerPartnerId == r.OwnerPartnerId
                            && il.LocationId == targetDetail.DestLocationId.Value
                            && il.LotNumber == r.LotNumber
                            && il.ExpiryDate == r.ExpiryDate);
                    if (dest == null)
                    {
                        dest = new ItemLocation
                        {
                            ItemId = r.ItemId,
                            OwnerPartnerId = r.OwnerPartnerId,
                            LocationId = targetDetail.DestLocationId.Value,
                            Quantity = 0,
                            LotNumber = r.LotNumber,
                            ExpiryDate = r.ExpiryDate,
                            UpdatedAt = VietnamNow
                        };
                        _db.ItemLocations.Add(dest);
                    }
                    dest.Quantity += postQty;
                    dest.UpdatedAt = VietnamNow;
                }
                else if (items.TryGetValue(r.ItemId, out var item))
                {
                    // P0-03: ItemLocation remains source of truth; running cache is only for alert/display before final sync.
                    runningStockByItem.TryGetValue(item.ItemId, out var oldStock);
                    var newStock = oldStock - postQty;
                    item.CurrentStock = newStock;
                    runningStockByItem[item.ItemId] = newStock;
                    if (newStock < 0) throw WmsExceptions.NegativeItemStock(item.ItemCode, newStock);
                    item.TotalStockValue = item.CurrentStock * item.UnitCost;
                    item.UpdatedAt = VietnamNow;

                    // Replenishment trigger
                    if (item.ReorderPoint > 0 && item.CurrentStock <= item.ReorderPoint)
                    {
                        var existingReplenAlert = await _db.StockAlerts
                            .AnyAsync(a => a.ItemId == item.ItemId && !a.IsResolved && a.AlertType == AlertTypeEnum.LowStock);
                        if (!existingReplenAlert)
                        {
                            _db.StockAlerts.Add(new StockAlert
                            {
                                ItemId = item.ItemId,
                                AlertType = AlertTypeEnum.LowStock,
                                CurrentStock = item.CurrentStock,
                                Threshold = item.ReorderPoint ?? 0,
                                IsRead = false,
                                IsResolved = false,
                                CreatedAt = VietnamNow
                            });
                        }
                    }
                }

                // Update reservation status
                r.ConsumedQty += postQty;
                var pendingAfter = r.ReservedQty - r.ConsumedQty - r.ReleasedQty;
                if (pendingAfter <= 0)
                {
                    r.Status = ReservationStatusEnum.Consumed;
                }
                else if (cancelRemaining)
                {
                    await _serialInventoryService.ReleaseOpenForReservationAsync(r, actor, (int)Math.Ceiling(pendingAfter), "Cancel remaining after partial post.");
                    r.ReleasedQty += pendingAfter;
                    r.Status = ReservationStatusEnum.Consumed;
                    var releasedLoc = await _db.ItemLocations
                        .FirstOrDefaultAsync(il => il.ItemId == r.ItemId
                            && il.OwnerPartnerId == r.OwnerPartnerId
                            && il.LocationId == r.LocationId
                            && il.LotNumber == r.LotNumber
                            && il.ExpiryDate == r.ExpiryDate);
                    if (releasedLoc != null) affectedItemLocationIds.Add(releasedLoc.ItemLocationId);
                }
                else
                {
                    r.Status = ReservationStatusEnum.Active;
                }
                r.UpdatedAt = VietnamNow;
            }

            if (!hasPostedAnyQty && !cancelRemaining)
                throw WmsExceptions.NoPickQty();

            await _unitOfWork.SaveChangesAsync();
            await _reservationService.RecalculateReservedQtyAsync(affectedItemLocationIds);

            await _unitOfWork.SaveChangesAsync();

            // P0-03: Sync CurrentStock from ItemLocation source of truth
            await _inventoryBalanceService.SyncCurrentStockAsync(affectedItemIds);

            var activeRemaining = reservations.Any(r => r.Status == ReservationStatusEnum.Active && (r.ReservedQty - r.ConsumedQty - r.ReleasedQty) > 0);

            voucher.IsPosted = !activeRemaining;
            voucher.FulfillmentStatus = activeRemaining ? FulfillmentStatusEnum.PartiallyIssued : FulfillmentStatusEnum.Completed;
            if (!activeRemaining)
                voucher.CompletedAt ??= VietnamNow;
            voucher.UpdatedAt = VietnamNow;

            // Update wave/pick task status
            if (voucher.WaveId.HasValue)
            {
                var relatedTasks = await _db.PickTasks
                    .Where(t => t.VoucherId == voucher.VoucherId && (t.Status == PickTaskStatusEnum.Pending || t.Status == PickTaskStatusEnum.Assigned || t.Status == PickTaskStatusEnum.InProgress))
                    .ToListAsync();

                foreach (var task in relatedTasks)
                {
                    if (cancelRemaining && task.PickedQty < task.TargetQty)
                    {
                        task.Status = PickTaskStatusEnum.Cancelled;
                        task.CompletedAt ??= VietnamNow;
                    }
                    else if (task.PickedQty >= task.TargetQty)
                    {
                        task.Status = PickTaskStatusEnum.Completed;
                        task.CompletedAt ??= VietnamNow;
                    }
                    else if (!activeRemaining)
                    {
                        task.Status = PickTaskStatusEnum.Short;
                        task.CompletedAt ??= VietnamNow;
                    }
                }

                var openTasks = await _db.PickTasks
                    .CountAsync(t => t.WaveId == voucher.WaveId.Value && (t.Status == PickTaskStatusEnum.Pending || t.Status == PickTaskStatusEnum.Assigned || t.Status == PickTaskStatusEnum.InProgress));
                if (openTasks == 0)
                {
                    var wave = await _db.Waves.FirstOrDefaultAsync(w => w.WaveId == voucher.WaveId.Value);
                    if (wave != null)
                    {
                        wave.Status = WaveStatusEnum.Completed;
                        wave.CompletedAt ??= VietnamNow;
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            var warningMsg = voucher.PartialShipmentAllowed && pickedAndReady < totalReserved && pickedAndReady > 0
                ? $"Đã xuất {pickedAndReady:N2}/{totalReserved:N2} đơn vị. Phần còn thiếu đã được tạo phiếu bổ sung tự động."
                : null;

            return WorkflowResult.Success(
                activeRemaining
                    ? $"Đã ghi sổ xuất một phần cho phiếu {voucher.VoucherCode}. Phần còn lại vẫn chờ xử lý."
                    : $"Đã ghi sổ hoàn tất phiếu outbound {voucher.VoucherCode}.",
                "Details",
                new { id = voucherId },
                warningMsg);
        }
        catch (BusinessRuleException ex)
        {
            await _unitOfWork.RollbackAsync();
            return WorkflowResult.Failure(UserSafeError.From(ex), "Details", new { id = voucherId });
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Helper to calculate how much was picked for a specific reservation (from batch allocations + legacy picks)
    /// </summary>
    private async Task<decimal> GetPickedQtyForReservationAsync(StockReservation reservation, long voucherId)
    {
        var allocPicked = await _db.PickTaskAllocations
            .Where(a => a.VoucherId == voucherId
                && a.StockReservation != null
                && a.StockReservation.ItemId == reservation.ItemId
                && a.StockReservation.LocationId == reservation.LocationId
                && a.StockReservation.LotNumber == reservation.LotNumber
                && a.StockReservation.ExpiryDate == reservation.ExpiryDate
                && a.VoucherDetailId == reservation.VoucherDetailId)
            .SumAsync(a => (decimal?)a.PickedQty) ?? 0m;

        var legacyPicked = await _db.PickTasks
            .Where(t => t.VoucherId == voucherId
                && t.PickTaskMode == PickTaskModeEnum.Single
                && !t.Allocations.Any()
                && t.ItemId == reservation.ItemId
                && t.SourceLocationId == reservation.LocationId
                && t.LotNumber == reservation.LotNumber
                && t.ExpiryDate == reservation.ExpiryDate
                && t.VoucherDetailId == reservation.VoucherDetailId)
            .SumAsync(t => (decimal?)t.PickedQty) ?? 0m;

        return allocPicked + legacyPicked;
    }

    private sealed record PickedReservationQty(
        long? VoucherDetailId, int ItemId, int LocationId,
        string? LotNumber, DateTime? ExpiryDate, decimal PickedQty);

    private sealed record FefoAllocation(int LocationId, string? LotNumber, DateTime? ExpiryDate, decimal Qty);

    private static bool IsTwoStepProfile(string? waveProfile)
        => string.Equals(waveProfile, "TwoStep", StringComparison.OrdinalIgnoreCase);

    private async Task<WarehouseSortationConfig?> GetActiveSortationConfigAsync(int warehouseId)
        => await _db.WarehouseSortationConfigs
            .Include(c => c.StagingLocation).ThenInclude(l => l!.Zone)
            .Include(c => c.SortationLocation).ThenInclude(l => l!.Zone)
            .FirstOrDefaultAsync(c => c.WarehouseId == warehouseId && c.IsActive);

    private static bool IsLocationInWarehouse(Location? location, int warehouseId)
        => location?.Zone != null && location.Zone.WarehouseId == warehouseId;

    // ═══════════════════════════════════════════════════════════════
    // P0-04: CreateWaveAsync — extracted from VouchersController
    // ═══════════════════════════════════════════════════════════════
    public async Task<WorkflowResult> CreateWaveAsync(
        string waveProfile, string? carrierCode, string? carrierName,
        string? routeCode, DateTime? cutoffTime, WavePriorityEnum priority,
        long[] selectedVoucherIds, string? notes,
        int? scopedWarehouseId, string actor)
    {
        if (selectedVoucherIds == null || selectedVoucherIds.Length == 0)
            return WorkflowResult.Failure("Vui lòng chọn ít nhất 1 phiếu xuất để tạo sóng.", "WavePlanning");

        if (cutoffTime.HasValue && cutoffTime.Value <= VietnamNow)
            return WorkflowResult.Failure("Giờ cutoff phải lớn hơn thời điểm hiện tại.", "WavePlanning");

        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var vouchers = await _db.Vouchers
                .Include(v => v.Details)
                .Where(v => selectedVoucherIds.Contains(v.VoucherId)
                    && v.VoucherType == VoucherTypeEnum.XuatKho
                    && !v.IsCancelled && !v.IsPosted && v.WaveId == null)
                .ToListAsync();

            if (vouchers.Count == 0)
                return WorkflowResult.Failure("Không có phiếu hợp lệ nào để tạo sóng.", "WavePlanning");

            if (scopedWarehouseId.HasValue && vouchers.Any(v => v.WarehouseId != scopedWarehouseId.Value))
                return WorkflowResult.Failure("Một số phiếu không thuộc kho bạn được phép quản lý.", "WavePlanning");

            var warehouseId = vouchers.First().WarehouseId;
            if (vouchers.Any(v => v.WarehouseId != warehouseId))
                return WorkflowResult.Failure("Chỉ được tạo một đợt lấy hàng cho các phiếu cùng kho.", "WavePlanning");

            var ownerPartnerId = vouchers.First().OwnerPartnerId;
            if (vouchers.Any(v => v.OwnerPartnerId != ownerPartnerId))
                return WorkflowResult.Failure("Khong duoc tao chung wave cho nhieu chu hang 3PL khac nhau.", "WavePlanning");

            var isTwoStep = IsTwoStepProfile(waveProfile);
            WarehouseSortationConfig? sortationConfig = null;
            if (isTwoStep)
            {
                sortationConfig = await GetActiveSortationConfigAsync(warehouseId);
                if (sortationConfig == null
                    || !IsLocationInWarehouse(sortationConfig.StagingLocation, warehouseId)
                    || !IsLocationInWarehouse(sortationConfig.SortationLocation, warehouseId))
                {
                    return WorkflowResult.Failure("Kho chưa có cấu hình vị trí tập kết và vị trí phân loại hợp lệ. Vui lòng cấu hình phân loại đơn trước khi tạo đợt lấy hàng hai bước.", "WavePlanning");
                }
            }

            Wave? wave = null;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                var baseSeq = await _db.Waves.CountAsync(w => w.WarehouseId == warehouseId);
                var seq = baseSeq + 1 + attempt;
                var waveCode = $"WV-{VietnamNow:yyyyMMdd}-{seq:D5}";
                wave = new Wave
                {
                    WaveCode = waveCode,
                    WaveProfile = waveProfile,
                    CarrierCode = carrierCode,
                    CarrierName = carrierName,
                    RouteCode = routeCode,
                    CutoffTime = cutoffTime,
                    Priority = priority,
                    WarehouseId = warehouseId,
                    OwnerPartnerId = ownerPartnerId,
                    Status = WaveStatusEnum.Released,
                    CreatedBy = actor,
                    CreatedAt = VietnamNow,
                    ReleasedAt = VietnamNow,
                    Notes = notes ?? $"Sóng đa đơn gồm {vouchers.Count} phiếu"
                };
                _db.Waves.Add(wave);
                try { await _unitOfWork.SaveChangesAsync(); break; }
                catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
                    || ex.InnerException?.Message?.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _db.Entry(wave).State = EntityState.Detached;
                    wave = null;
                    if (attempt == 9) throw WmsExceptions.WaveCodeDuplicate();
                }
            }
            if (wave == null) throw WmsExceptions.WaveCreationFailed();

            var taskSeq = 0;
            var totalAllocated = 0;
            var affectedItemLocationIds = new List<int>();
            var batchPickAllocations = new List<(StockReservation Reservation, int ItemId, int LocationId, string? LotNumber, DateTime? ExpiryDate, int? OwnerPartnerId, decimal Qty)>();

            vouchers = vouchers
                .OrderByDescending(v => v.ServiceLevel == ServiceLevelEnum.SameDay ? 100 : 0)
                .ThenByDescending(v => v.ServiceLevel == ServiceLevelEnum.Express ? 90 : 0)
                .ThenByDescending(v => v.ServiceLevel == ServiceLevelEnum.Scheduled ? 80 : 0)
                .ThenByDescending(v => v.ServiceLevel == ServiceLevelEnum.Standard ? 70 : 0)
                .ThenByDescending(v => v.Priority)
                .ThenBy(v => v.RequestedDeliveryDate ?? VietnamNow.Date.AddDays(7))
                .ToList();

            foreach (var voucher in vouchers)
            {
                voucher.WaveId = wave.WaveId;
                voucher.FulfillmentStatus = FulfillmentStatusEnum.WaitingForPick;

                foreach (var d in voucher.Details)
                {
                    if (d.OwnerPartnerId.HasValue && d.OwnerPartnerId != voucher.OwnerPartnerId)
                    {
                        await _unitOfWork.RollbackAsync();
                        return WorkflowResult.Failure("Dòng phiếu không cùng chủ hàng với phiếu xuất.", "WavePlanning");
                    }
                    d.OwnerPartnerId ??= voucher.OwnerPartnerId;

                    var req = Math.Abs(d.BaseQty);
                    if (req <= 0) continue;

                    List<FefoAllocation> allocations;
                    if (d.LocationId.HasValue)
                    {
                        var sourceCandidates = await _db.ItemLocations
                            .Where(il => il.ItemId == d.ItemId && il.LocationId == d.LocationId.Value
                                && il.OwnerPartnerId == voucher.OwnerPartnerId
                                && il.HoldStatus == InventoryHoldStatusEnum.Available
                                && (d.LotNumber == null || il.LotNumber == d.LotNumber)
                                && (!d.ExpiryDate.HasValue || il.ExpiryDate == d.ExpiryDate))
                            .OrderBy(il => il.ExpiryDate.HasValue ? 0 : 1).ThenBy(il => il.ExpiryDate)
                            .ThenByDescending(il => il.Quantity - il.ReservedQty).ThenBy(il => il.ItemLocationId)
                            .ToListAsync();
                        var sourceLoc = sourceCandidates.FirstOrDefault(il => (il.Quantity - il.ReservedQty) > 0);
                        allocations = sourceLoc != null
                            ? new List<FefoAllocation> { new(sourceLoc.LocationId, sourceLoc.LotNumber, sourceLoc.ExpiryDate, Math.Min(req, Math.Max(0, sourceLoc.Quantity - sourceLoc.ReservedQty))) }
                            : await AllocateFefoAsync(d.ItemId, warehouseId, req, voucher.OwnerPartnerId);
                    }
                    else
                    {
                        allocations = await AllocateFefoAsync(d.ItemId, warehouseId, req, voucher.OwnerPartnerId);
                    }

                    _db.WaveLines.Add(new WaveLine { WaveId = wave.WaveId, VoucherId = voucher.VoucherId, ItemId = d.ItemId, RequiredQty = req, PickedQty = 0, Status = 1 });

                    foreach (var a in allocations)
                    {
                        var reservation = new StockReservation
                        {
                            VoucherId = voucher.VoucherId,
                            VoucherDetailId = d.VoucherDetailId,
                            ItemId = d.ItemId,
                            LocationId = a.LocationId,
                            OwnerPartnerId = voucher.OwnerPartnerId,
                            LotNumber = a.LotNumber,
                            ExpiryDate = a.ExpiryDate,
                            ReservedQty = a.Qty,
                            Status = ReservationStatusEnum.Active,
                            CreatedBy = actor,
                            CreatedAt = VietnamNow,
                            Notes = $"Wave multi-order {wave.WaveCode}"
                        };
                        _db.StockReservations.Add(reservation);
                        batchPickAllocations.Add((reservation, d.ItemId, a.LocationId, a.LotNumber, a.ExpiryDate, voucher.OwnerPartnerId, a.Qty));

                        var itemLoc = await _db.ItemLocations
                            .FirstOrDefaultAsync(il => il.ItemId == d.ItemId && il.LocationId == a.LocationId && il.LotNumber == a.LotNumber && il.ExpiryDate == a.ExpiryDate && il.OwnerPartnerId == voucher.OwnerPartnerId);
                        if (itemLoc != null) affectedItemLocationIds.Add(itemLoc.ItemLocationId);
                    }
                    totalAllocated++;
                }
            }

            foreach (var group in batchPickAllocations.GroupBy(x => new { x.ItemId, x.LocationId, x.LotNumber, x.ExpiryDate, x.OwnerPartnerId }))
            {
                taskSeq++;
                var first = group.First();
                var targetQty = group.Sum(x => x.Qty);
                var distinctVoucherCount = group.Select(x => x.Reservation.VoucherId).Distinct().Count();
                var batchGroupKey = string.Join("|", group.Key.ItemId, group.Key.LocationId,
                    group.Key.LotNumber ?? "", group.Key.ExpiryDate?.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture) ?? "");

                if (isTwoStep)
                {
                    var bulkTask = new PickTask
                    {
                        TaskCode = $"PT-{wave.WaveCode}-{taskSeq:D3}",
                        WaveId = wave.WaveId,
                        VoucherId = first.Reservation.VoucherId,
                        VoucherDetailId = first.Reservation.VoucherDetailId,
                        OwnerPartnerId = group.Key.OwnerPartnerId,
                        ItemId = group.Key.ItemId,
                        SourceLocationId = group.Key.LocationId,
                        TargetLocationId = sortationConfig!.StagingLocationId,
                        SortationStageLocationId = sortationConfig.StagingLocationId,
                        SortationDestinationLocationId = sortationConfig.SortationLocationId,
                        LotNumber = group.Key.LotNumber,
                        ExpiryDate = group.Key.ExpiryDate,
                        TargetQty = targetQty,
                        AssignedTo = actor,
                        AssignedAt = VietnamNow,
                        Status = PickTaskStatusEnum.Assigned,
                        DueAt = cutoffTime ?? VietnamNow.AddHours(24),
                        IsBatchPick = true,
                        PickTaskMode = PickTaskModeEnum.Bulk,
                        BatchGroupKey = batchGroupKey
                    };
                    _db.PickTasks.Add(bulkTask);

                    var sortSeq = 0;
                    foreach (var allocation in group)
                    {
                        sortSeq++;
                        var sortTask = new PickTask
                        {
                            TaskCode = $"PT-{wave.WaveCode}-{taskSeq:D3}-S{sortSeq:D2}",
                            WaveId = wave.WaveId,
                            VoucherId = allocation.Reservation.VoucherId,
                            VoucherDetailId = allocation.Reservation.VoucherDetailId,
                            OwnerPartnerId = group.Key.OwnerPartnerId,
                            ItemId = allocation.ItemId,
                            SourceLocationId = sortationConfig.StagingLocationId,
                            TargetLocationId = sortationConfig.SortationLocationId,
                            SortationStageLocationId = sortationConfig.StagingLocationId,
                            SortationDestinationLocationId = sortationConfig.SortationLocationId,
                            LotNumber = allocation.LotNumber,
                            ExpiryDate = allocation.ExpiryDate,
                            TargetQty = allocation.Qty,
                            Status = PickTaskStatusEnum.WaitingForBulk,
                            DueAt = cutoffTime ?? VietnamNow.AddHours(24),
                            IsBatchPick = false,
                            PickTaskMode = PickTaskModeEnum.Sort,
                            ParentPickTask = bulkTask,
                            BatchGroupKey = batchGroupKey
                        };
                        _db.PickTasks.Add(sortTask);
                        _db.PickTaskAllocations.Add(new PickTaskAllocation
                        {
                            PickTask = sortTask,
                            StockReservation = allocation.Reservation,
                            VoucherId = allocation.Reservation.VoucherId,
                            VoucherDetailId = allocation.Reservation.VoucherDetailId,
                            AllocatedQty = allocation.Qty,
                            PickedQty = 0
                        });
                    }

                    continue;
                }

                var task = new PickTask
                {
                    TaskCode = $"PT-{wave.WaveCode}-{taskSeq:D3}",
                    WaveId = wave.WaveId,
                    VoucherId = first.Reservation.VoucherId,
                    VoucherDetailId = first.Reservation.VoucherDetailId,
                    OwnerPartnerId = group.Key.OwnerPartnerId,
                    ItemId = group.Key.ItemId,
                    SourceLocationId = group.Key.LocationId,
                    LotNumber = group.Key.LotNumber,
                    ExpiryDate = group.Key.ExpiryDate,
                    TargetQty = targetQty,
                    AssignedTo = actor,
                    AssignedAt = VietnamNow,
                    Status = PickTaskStatusEnum.Assigned,
                    DueAt = cutoffTime ?? VietnamNow.AddHours(24),
                    IsBatchPick = distinctVoucherCount > 1 || group.Count() > 1,
                    PickTaskMode = PickTaskModeEnum.Single,
                    BatchGroupKey = batchGroupKey
                };
                _db.PickTasks.Add(task);
                foreach (var allocation in group)
                {
                    _db.PickTaskAllocations.Add(new PickTaskAllocation
                    {
                        PickTask = task,
                        StockReservation = allocation.Reservation,
                        VoucherId = allocation.Reservation.VoucherId,
                        VoucherDetailId = allocation.Reservation.VoucherDetailId,
                        AllocatedQty = allocation.Qty,
                        PickedQty = 0
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();
            foreach (var voucher in vouchers)
                await _serialInventoryService.AllocateForVoucherAsync(voucher.VoucherId, actor, $"voucher:{voucher.VoucherId}:multi-wave-serial");
            if (affectedItemLocationIds.Count > 0)
                await _reservationService.RecalculateReservedQtyAsync(affectedItemLocationIds);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            var msg = $"Đã tạo và phát hành sóng [{wave.WaveCode}] gồm {vouchers.Count} phiếu, {taskSeq} nhiệm vụ lấy."
                + (cutoffTime.HasValue ? $" Cutoff: {cutoffTime.Value:HH:mm dd/MM}." : "");
            return WorkflowResult.Success(msg, "Details", new { id = vouchers.First().VoucherId });
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<WorkflowResult> ReleaseVoucherForPickingAsync(
        long voucherId,
        int? scopedWarehouseId,
        string actor)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var voucher = await _db.Vouchers
                .Include(v => v.Details)
                .FirstOrDefaultAsync(v => v.VoucherId == voucherId);
            if (voucher == null)
            {
                await _unitOfWork.RollbackAsync();
                return WorkflowResult.NotFoundResult("Không tìm thấy phiếu.");
            }
            if (voucher.IsCancelled)
            {
                await _unitOfWork.RollbackAsync();
                return WorkflowResult.Failure("Phiếu đã hủy.", "Details", new { id = voucherId });
            }
            if (voucher.IsPosted)
            {
                await _unitOfWork.RollbackAsync();
                return WorkflowResult.Failure("Phiếu đã ghi sổ.", "Details", new { id = voucherId });
            }
            if (voucher.VoucherType is not (VoucherTypeEnum.XuatKho or VoucherTypeEnum.TraNCC or VoucherTypeEnum.ChuyenKho or VoucherTypeEnum.XuatSanXuat))
            {
                await _unitOfWork.RollbackAsync();
                return WorkflowResult.Failure("Chỉ áp dụng cho phiếu xuất/chuyển kho.", "Details", new { id = voucherId });
            }
            if (scopedWarehouseId.HasValue && voucher.WarehouseId != scopedWarehouseId.Value)
            {
                await _unitOfWork.RollbackAsync();
                return WorkflowResult.ForbiddenResult();
            }

            if (voucher.WaveId.HasValue || voucher.FulfillmentStatus >= FulfillmentStatusEnum.WaitingForPick)
            {
                var existingActive = await _db.StockReservations.AnyAsync(r => r.VoucherId == voucher.VoucherId && r.Status == ReservationStatusEnum.Active);
                if (existingActive)
                {
                    voucher.FulfillmentStatus = FulfillmentStatusEnum.WaitingForPick;
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitAsync();
                    return WorkflowResult.Success("Phiếu đã được giữ chỗ trước đó.", "Details", new { id = voucherId });
                }

                await _unitOfWork.CommitAsync();
                return WorkflowResult.Failure(
                    "Phiếu đã qua bước phát hành đợt lấy hàng, không thể thực hiện lại. Vui lòng tạo phiếu mới hoặc hủy phiếu hiện tại.",
                    "Details",
                    new { id = voucherId });
            }

            using var ledgerScope = _inventoryTransactionService.BeginScope(new InventoryTransactionContext
            {
                TransactionType = InventoryTransactionTypeEnum.Pick,
                TransactionGroupKey = $"voucher:{voucher.VoucherId}:release-picking",
                IdempotencyKeyPrefix = $"voucher:{voucher.VoucherId}:release-picking",
                WarehouseId = voucher.WarehouseId,
                OwnerPartnerId = voucher.OwnerPartnerId,
                VoucherId = voucher.VoucherId,
                ReferenceType = "Voucher",
                ReferenceId = voucher.VoucherId.ToString(),
                ReferenceCode = voucher.VoucherCode,
                Actor = actor
            });

            Wave? wave = null;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                var baseSeq = await _db.Waves.CountAsync(w => w.WarehouseId == voucher.WarehouseId);
                var waveCode = $"WV-{VietnamNow:yyyyMMdd}-{(baseSeq + 1 + attempt):D5}";
                wave = new Wave
                {
                    WaveCode = waveCode,
                    WarehouseId = voucher.WarehouseId,
                    OwnerPartnerId = voucher.OwnerPartnerId,
                    Status = WaveStatusEnum.Released,
                    CreatedBy = actor,
                    CreatedAt = VietnamNow,
                    ReleasedAt = VietnamNow,
                    Notes = $"Đợt lấy hàng tạo từ phiếu {voucher.VoucherCode}"
                };
                _db.Waves.Add(wave);
                try
                {
                    await _unitOfWork.SaveChangesAsync();
                    break;
                }
                catch (DbUpdateException ex) when ((ex.InnerException?.Message?.Contains("2601") ?? false)
                    || (ex.InnerException?.Message?.Contains("2627") ?? false)
                    || (ex.InnerException?.Message?.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ?? false)
                    || (ex.InnerException?.Message?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    _db.Entry(wave).State = EntityState.Detached;
                    wave = null;
                    if (attempt == 9)
                        throw WmsExceptions.PickTaskCodeDuplicate();
                }
            }
            if (wave == null)
                throw WmsExceptions.PickTaskCreationFailed();

            voucher.WaveId = wave.WaveId;
            voucher.FulfillmentStatus = FulfillmentStatusEnum.WaitingForPick;
            var affectedItemLocationIds = new List<int>();
            var taskSeq = 0;
            decimal totalRequested = 0, totalAllocated = 0;
            var partialCount = 0;

            foreach (var d in voucher.Details)
            {
                if (d.OwnerPartnerId.HasValue && d.OwnerPartnerId != voucher.OwnerPartnerId)
                {
                    await _unitOfWork.RollbackAsync();
                    return WorkflowResult.Failure("Dòng phiếu không cùng chủ hàng với phiếu xuất.", "Details", new { id = voucherId });
                }
                d.OwnerPartnerId ??= voucher.OwnerPartnerId;

                var req = Math.Abs(d.BaseQty);
                if (req <= 0) continue;

                List<FefoAllocation> allocations;
                if (d.LocationId.HasValue)
                {
                    var sourceCandidates = await _db.ItemLocations
                        .Where(il => il.ItemId == d.ItemId
                            && il.LocationId == d.LocationId.Value
                            && il.OwnerPartnerId == voucher.OwnerPartnerId
                            && il.HoldStatus == InventoryHoldStatusEnum.Available
                            && (d.LotNumber == null || il.LotNumber == d.LotNumber)
                            && (!d.ExpiryDate.HasValue || il.ExpiryDate == d.ExpiryDate))
                        .OrderBy(il => il.ExpiryDate.HasValue ? 0 : 1)
                        .ThenBy(il => il.ExpiryDate)
                        .ThenByDescending(il => il.Quantity - il.ReservedQty)
                        .ThenBy(il => il.ItemLocationId)
                        .ToListAsync();
                    var sourceLoc = sourceCandidates.FirstOrDefault(il => (il.Quantity - il.ReservedQty) > 0);
                    allocations = sourceLoc != null
                        ? new List<FefoAllocation> { new(sourceLoc.LocationId, sourceLoc.LotNumber, sourceLoc.ExpiryDate, Math.Min(req, Math.Max(0, sourceLoc.Quantity - sourceLoc.ReservedQty))) }
                        : await AllocateFefoAsync(d.ItemId, voucher.WarehouseId, req, voucher.OwnerPartnerId);
                }
                else
                {
                    allocations = await AllocateFefoAsync(d.ItemId, voucher.WarehouseId, req, voucher.OwnerPartnerId);
                }

                _db.WaveLines.Add(new WaveLine
                {
                    WaveId = wave.WaveId,
                    VoucherId = voucher.VoucherId,
                    ItemId = d.ItemId,
                    RequiredQty = req,
                    PickedQty = 0,
                    Status = 1
                });

                var allocatedForLine = allocations.Sum(x => x.Qty);
                totalRequested += req;
                totalAllocated += allocatedForLine;
                if (allocatedForLine < req) partialCount++;

                foreach (var a in allocations)
                {
                    var reservation = new StockReservation
                    {
                        VoucherId = voucher.VoucherId,
                        VoucherDetailId = d.VoucherDetailId,
                        ItemId = d.ItemId,
                        LocationId = a.LocationId,
                        OwnerPartnerId = voucher.OwnerPartnerId,
                        LotNumber = a.LotNumber,
                        ExpiryDate = a.ExpiryDate,
                        ReservedQty = a.Qty,
                        Status = ReservationStatusEnum.Active,
                        CreatedBy = actor,
                        CreatedAt = VietnamNow,
                        Notes = $"Reserved for {voucher.VoucherCode}"
                    };
                    _db.StockReservations.Add(reservation);

                    taskSeq++;
                    var task = new PickTask
                    {
                        TaskCode = $"PT-{wave.WaveCode}-{taskSeq:D3}",
                        WaveId = wave.WaveId,
                        VoucherId = voucher.VoucherId,
                        VoucherDetailId = d.VoucherDetailId,
                        OwnerPartnerId = voucher.OwnerPartnerId,
                        ItemId = d.ItemId,
                        SourceLocationId = a.LocationId,
                        LotNumber = a.LotNumber,
                        ExpiryDate = a.ExpiryDate,
                        TargetQty = a.Qty,
                        AssignedTo = actor,
                        AssignedAt = VietnamNow,
                        Status = PickTaskStatusEnum.Assigned,
                        DueAt = VietnamNow.AddHours(24)
                    };
                    _db.PickTasks.Add(task);
                    _db.PickTaskAllocations.Add(new PickTaskAllocation
                    {
                        PickTask = task,
                        StockReservation = reservation,
                        VoucherId = voucher.VoucherId,
                        VoucherDetailId = d.VoucherDetailId,
                        AllocatedQty = a.Qty,
                        PickedQty = 0
                    });

                    var itemLoc = await _db.ItemLocations
                        .FirstOrDefaultAsync(il => il.ItemId == d.ItemId
                            && il.LocationId == a.LocationId
                            && il.LotNumber == a.LotNumber
                            && il.ExpiryDate == a.ExpiryDate
                            && il.OwnerPartnerId == voucher.OwnerPartnerId);
                    if (itemLoc != null)
                        affectedItemLocationIds.Add(itemLoc.ItemLocationId);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _serialInventoryService.AllocateForVoucherAsync(voucher.VoucherId, actor, $"voucher:{voucher.VoucherId}:wave-release-serial");
            if (affectedItemLocationIds.Count > 0)
                await _reservationService.RecalculateReservedQtyAsync(affectedItemLocationIds);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            var message = partialCount > 0
                ? $"Đã phát hành đợt lấy hàng {wave.WaveCode}. Giữ chỗ {totalAllocated:N0}/{totalRequested:N0} - có {partialCount} vật tư thiếu tồn, phần còn lại sẽ giao đợt sau."
                : $"Đã phát hành đợt lấy hàng {wave.WaveCode} và giữ chỗ đủ {totalAllocated:N0} đơn vị.";
            return WorkflowResult.Success(message, "Details", new { id = voucherId });
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // P0-04: ConfirmPickTaskAsync — extracted from VouchersController
    // ═══════════════════════════════════════════════════════════════
    public async Task<WorkflowResult> ConfirmPickTaskAsync(
        long taskId, decimal qty, string scannedValue,
        List<string>? serialCodes, string actor, bool canOverrideAssignee,
        string? toteCode = null,
        string? sourceLocationCode = null,
        string? targetLocationCode = null,
        bool reportShort = false)
    {
        if (qty < 0 || (!reportShort && qty <= 0)) return WorkflowResult.Failure("Số lượng lấy hàng phải lớn hơn 0.", null);

        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var task = await _db.PickTasks
                .Include(t => t.Allocations).ThenInclude(a => a.StockReservation)
                .Include(t => t.Wave)
                .Include(t => t.Voucher)
                .Include(t => t.Item)
                .Include(t => t.SourceLocation)
                .Include(t => t.TargetLocation)
                .FirstOrDefaultAsync(t => t.PickTaskId == taskId);
            if (task == null) return WorkflowResult.Failure("Không tìm thấy nhiệm vụ lấy hàng.", null);

            var voucher = await _db.Vouchers.FirstOrDefaultAsync(v => v.VoucherId == task.VoucherId);
            if (voucher == null) return WorkflowResult.Failure("Không tìm thấy phiếu liên quan.", null);
            if (voucher.IsCancelled) return WorkflowResult.Failure("Phiếu đã hủy, không thể xác nhận pick.", null);
            if (voucher.IsPosted) return WorkflowResult.Failure("Phiếu đã ghi sổ, không thể xác nhận pick.", null);
            if (task.OwnerPartnerId != voucher.OwnerPartnerId
                || task.Allocations.Any(a => a.StockReservation != null && a.StockReservation.OwnerPartnerId != voucher.OwnerPartnerId))
            {
                return WorkflowResult.Failure("Nhiệm vụ lấy hàng không cùng chủ hàng với phiếu xuất.", "Details", new { id = task.VoucherId });
            }

            if (!string.IsNullOrWhiteSpace(task.AssignedTo)
                && !string.Equals(task.AssignedTo, actor, StringComparison.OrdinalIgnoreCase)
                && !canOverrideAssignee)
                return WorkflowResult.Failure($"Nhiệm vụ này đang được giao cho '{task.AssignedTo}'. Bạn không thể xác nhận thay.", "Details", new { id = task.VoucherId });

            // P1-02: Tote validation for cluster picking
            if (task.WaveId.HasValue && task.IsBatchPick && task.Allocations.Any())
            {
                // Determine which voucher this pick slice goes to
                var targetAllocation = task.Allocations.OrderBy(a => a.PickTaskAllocationId)
                    .FirstOrDefault(a => a.AllocatedQty - a.PickedQty > 0);
                var targetVoucherId = targetAllocation?.VoucherId ?? task.VoucherId;

                // Check if this wave has tote assignments
                var toteAssignment = await _db.PickTotes
                    .FirstOrDefaultAsync(t => t.WaveId == task.WaveId.Value && t.VoucherId == targetVoucherId
                        && t.Status != PickToteStatusEnum.Empty);

                if (toteAssignment != null)
                {
                    if (string.IsNullOrWhiteSpace(toteCode))
                        return WorkflowResult.Failure(
                            $"Wave này yêu cầu quét tote. Vui lòng quét tote [{toteAssignment.ToteCode}] trước khi xác nhận.",
                            "Details", new { id = task.VoucherId });

                    if (!string.Equals(toteCode.Trim(), toteAssignment.ToteCode, StringComparison.OrdinalIgnoreCase))
                        return WorkflowResult.Failure(
                            $"Sai tote! Phiếu [{voucher.VoucherCode}] cần bỏ vào tote [{toteAssignment.ToteCode}], không phải [{toteCode.Trim()}].",
                            "Details", new { id = task.VoucherId });

                    // Mark tote as actively picking
                    if (toteAssignment.Status == PickToteStatusEnum.Assigned)
                    {
                        toteAssignment.Status = PickToteStatusEnum.Picking;
                        toteAssignment.UpdatedAt = VietnamNow;
                    }
                }
            }
            else if (task.WaveId.HasValue)
            {
                // Single-voucher task: check tote for that voucher
                var toteAssignment = await _db.PickTotes
                    .FirstOrDefaultAsync(t => t.WaveId == task.WaveId.Value && t.VoucherId == task.VoucherId
                        && t.Status != PickToteStatusEnum.Empty);

                if (toteAssignment != null)
                {
                    if (string.IsNullOrWhiteSpace(toteCode))
                        return WorkflowResult.Failure(
                            $"Wave này yêu cầu quét tote. Vui lòng quét tote [{toteAssignment.ToteCode}] trước khi xác nhận.",
                            "Details", new { id = task.VoucherId });

                    if (!string.Equals(toteCode.Trim(), toteAssignment.ToteCode, StringComparison.OrdinalIgnoreCase))
                        return WorkflowResult.Failure(
                            $"Sai tote! Phiếu [{voucher.VoucherCode}] cần bỏ vào tote [{toteAssignment.ToteCode}], không phải [{toteCode.Trim()}].",
                            "Details", new { id = task.VoucherId });

                    if (toteAssignment.Status == PickToteStatusEnum.Assigned)
                    {
                        toteAssignment.Status = PickToteStatusEnum.Picking;
                        toteAssignment.UpdatedAt = VietnamNow;
                    }
                }
            }

            var taskItem = await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.ItemId == task.ItemId);
            if (taskItem == null) return WorkflowResult.Failure("Không tìm thấy vật tư.", null);

            if (!string.IsNullOrWhiteSpace(sourceLocationCode)
                && task.SourceLocation != null
                && !string.Equals(sourceLocationCode.Trim(), task.SourceLocation.LocationCode, StringComparison.OrdinalIgnoreCase))
            {
                return WorkflowResult.Failure($"Vị trí nguồn không đúng. Nhiệm vụ này cần quét vị trí [{task.SourceLocation.LocationCode}].", "Details", new { id = task.VoucherId });
            }

            // Scan validation
            var allowedScanKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { taskItem.ItemCode.Trim() };
            if (!string.IsNullOrWhiteSpace(taskItem.Barcode)) allowedScanKeys.Add(taskItem.Barcode.Trim());
            if (!string.IsNullOrWhiteSpace(taskItem.SkuCode)) allowedScanKeys.Add(taskItem.SkuCode.Trim());
            if (!string.IsNullOrWhiteSpace(task.LotNumber)) allowedScanKeys.Add(task.LotNumber.Trim());

            var scanned = string.IsNullOrWhiteSpace(scannedValue)
                ? (!string.IsNullOrWhiteSpace(task.LotNumber) ? task.LotNumber.Trim()
                    : (!string.IsNullOrWhiteSpace(taskItem.Barcode) ? taskItem.Barcode.Trim()
                        : (!string.IsNullOrWhiteSpace(taskItem.SkuCode) ? taskItem.SkuCode.Trim() : taskItem.ItemCode.Trim())))
                : scannedValue.Trim();

            if (!allowedScanKeys.Contains(scanned))
                return WorkflowResult.Failure($"Mã quét không khớp vật tư nhiệm vụ [{taskItem.ItemCode}].", "Details", new { id = task.VoucherId });

            if (task.PickTaskMode == PickTaskModeEnum.Bulk)
            {
                var bulkResult = await ConfirmBulkPickTaskAsync(task, taskItem, qty, scanned, serialCodes, actor, targetLocationCode, reportShort);
                await _unitOfWork.CommitAsync();
                return bulkResult;
            }

            var remain = task.TargetQty - task.PickedQty;
            var actual = Math.Min(remain, qty);
            if (actual <= 0 && !reportShort)
            {
                await _unitOfWork.CommitAsync();
                return WorkflowResult.Success("Nhiệm vụ đã đủ số lượng cần lấy.", "Details", new { id = task.VoucherId });
            }

            // Allocation distribution
            var allocationSlices = new List<(PickTaskAllocation? Allocation, decimal Qty, long VoucherId, long? VoucherDetailId)>();
            var qtyToAllocate = actual;
            foreach (var allocation in task.Allocations.OrderBy(a => a.PickTaskAllocationId))
            {
                var allocationRemain = allocation.AllocatedQty - allocation.PickedQty;
                if (allocationRemain <= 0) continue;
                var take = Math.Min(qtyToAllocate, allocationRemain);
                if (take <= 0) continue;
                allocationSlices.Add((allocation, take, allocation.VoucherId, allocation.VoucherDetailId));
                qtyToAllocate -= take;
                if (qtyToAllocate <= 0) break;
            }
            if (qtyToAllocate > 0 && task.Allocations.Any())
                return WorkflowResult.Failure("Phân bổ batch pick không đủ số lượng còn lại.", "Details", new { id = task.VoucherId });
            if (!task.Allocations.Any())
                allocationSlices.Add((null, actual, task.VoucherId, task.VoucherDetailId));

            // Serial handling
            if (taskItem.TrackSerial && actual > 0)
            {
                if (actual != decimal.Truncate(actual))
                    return WorkflowResult.Failure($"[{taskItem.ItemCode}] đang bật quản lý serial nên số lượng lấy phải là số nguyên.", "Details", new { id = task.VoucherId });

                var requiredSerialCount = (int)Math.Ceiling(actual);
                if (serialCodes == null || serialCodes.Count != requiredSerialCount)
                    return WorkflowResult.Failure($"[{taskItem.ItemCode}] cần quét đúng {requiredSerialCount} serial cho lần lấy này.", "Details", new { id = task.VoucherId });

                var blockedLocationIds = await _db.ItemLocations.AsNoTracking()
                    .Where(il => il.LocationId == task.SourceLocationId && il.HoldStatus != InventoryHoldStatusEnum.Available)
                    .Select(il => il.LocationId).ToListAsync();
                if (blockedLocationIds.Count > 0)
                    return WorkflowResult.Failure("Vị trí nguồn đang có hàng bị giữ (hold/quarantine).", "Details", new { id = task.VoucherId });

                var pickSlices = allocationSlices
                    .Select(s => new SerialPickSlice(s.Allocation?.StockReservationId, s.VoucherId, s.VoucherDetailId, s.Qty))
                    .ToList();
                await _serialInventoryService.ConfirmPickTaskSerialsAsync(task, pickSlices, serialCodes, actor);
            }

            foreach (var slice in allocationSlices.Where(s => s.Allocation != null))
                slice.Allocation!.PickedQty += slice.Qty;

            task.PickedQty += actual;
            task.Status = task.PickedQty >= task.TargetQty ? PickTaskStatusEnum.Completed : PickTaskStatusEnum.InProgress;
            if (task.Status == PickTaskStatusEnum.Completed) task.CompletedAt = VietnamNow;
            if (string.IsNullOrWhiteSpace(task.AssignedTo))
            {
                task.AssignedTo = actor;
                task.AssignedAt = VietnamNow;
            }

            ShortPickReallocationResult? shortPickResult = null;
            if (reportShort && task.PickedQty < task.TargetQty)
            {
                shortPickResult = await ReallocateShortPickAsync(task, actor);
            }

            _db.PickTaskScanLogs.Add(new PickTaskScanLog
            {
                PickTaskId = task.PickTaskId,
                ScannedBy = actor,
                ScanValue = scanned,
                Qty = actual,
                Notes = taskItem.TrackSerial ? $"Xác nhận lấy + {serialCodes?.Count ?? 0} serial" : "Xác nhận lấy"
            });

            await _unitOfWork.SaveChangesAsync();

            if (task.WaveId.HasValue)
            {
                var openTasks = await _db.PickTasks.CountAsync(t => t.WaveId == task.WaveId.Value
                    && (t.Status == PickTaskStatusEnum.Pending
                        || t.Status == PickTaskStatusEnum.Assigned
                        || t.Status == PickTaskStatusEnum.InProgress
                        || t.Status == PickTaskStatusEnum.WaitingForBulk
                        || t.Status == PickTaskStatusEnum.Short));
                if (openTasks == 0)
                {
                    var wave = await _db.Waves.FirstOrDefaultAsync(w => w.WaveId == task.WaveId.Value);
                    if (wave != null) { wave.Status = WaveStatusEnum.Completed; wave.CompletedAt = VietnamNow; }
                    var waveVouchers = await _db.Vouchers.Where(v => v.WaveId == task.WaveId.Value && !v.IsCancelled).ToListAsync();
                    foreach (var v in waveVouchers) v.FulfillmentStatus = FulfillmentStatusEnum.Picked;
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            else
            {
                var openVoucherTasks = await _db.PickTasks.CountAsync(t => t.VoucherId == task.VoucherId
                    && (t.Status == PickTaskStatusEnum.Pending
                        || t.Status == PickTaskStatusEnum.Assigned
                        || t.Status == PickTaskStatusEnum.InProgress
                        || t.Status == PickTaskStatusEnum.WaitingForBulk
                        || t.Status == PickTaskStatusEnum.Short));
                voucher.FulfillmentStatus = openVoucherTasks == 0
                    ? FulfillmentStatusEnum.Picked
                    : FulfillmentStatusEnum.Picking;
                voucher.UpdatedAt = VietnamNow;
                await _unitOfWork.SaveChangesAsync();
            }
            if (task.WaveId.HasValue && voucher.FulfillmentStatus < FulfillmentStatusEnum.Picking)
            {
                voucher.FulfillmentStatus = FulfillmentStatusEnum.Picking;
                voucher.UpdatedAt = VietnamNow;
                await _unitOfWork.SaveChangesAsync();
            }

            await _unitOfWork.CommitAsync();
            if (shortPickResult != null)
            {
                var message = shortPickResult.BackorderSuggestedQty > 0
                    ? $"Đã báo thiếu hàng. Hệ thống đã tạo {shortPickResult.ReplacementTaskCount} nhiệm vụ bù cho {shortPickResult.ReallocatedQty:N2}; còn {shortPickResult.BackorderSuggestedQty:N2} đề xuất phiếu bổ sung."
                    : $"Đã báo thiếu hàng. Hệ thống đã tạo {shortPickResult.ReplacementTaskCount} nhiệm vụ bù với tổng số lượng {shortPickResult.ReallocatedQty:N2}.";
                return WorkflowResult.Success(message, "Details", new { id = task.VoucherId });
            }
            return WorkflowResult.Success("Đã ghi nhận lần quét xác nhận lấy hàng.", "Details", new { id = task.VoucherId });
        }
        catch (BusinessRuleException ex)
        {
            await _unitOfWork.RollbackAsync();
            var voucherRoute = await _db.PickTasks.AsNoTracking()
                .Where(t => t.PickTaskId == taskId)
                .Select(t => new { id = t.VoucherId })
                .FirstOrDefaultAsync();
            return WorkflowResult.Failure(UserSafeError.From(ex), "Details", voucherRoute);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    private async Task<WorkflowResult> ConfirmBulkPickTaskAsync(
        PickTask task,
        Item taskItem,
        decimal qty,
        string scanned,
        List<string>? serialCodes,
        string actor,
        string? targetLocationCode,
        bool reportShort)
    {
        if (reportShort)
            return WorkflowResult.Failure("Nhiệm vụ lấy tổng chưa hỗ trợ báo thiếu trực tiếp. Vui lòng điều phối lại đợt lấy hàng hoặc hủy phiếu liên quan.", "Details", new { id = task.VoucherId });

        if (task.TargetLocationId == null || task.TargetLocation == null)
            return WorkflowResult.Failure("Nhiệm vụ lấy tổng chưa có vị trí tập kết hợp lệ.", "Details", new { id = task.VoucherId });

        if (string.IsNullOrWhiteSpace(targetLocationCode)
            || !string.Equals(targetLocationCode.Trim(), task.TargetLocation.LocationCode, StringComparison.OrdinalIgnoreCase))
        {
            return WorkflowResult.Failure($"Vị trí tập kết không đúng. Vui lòng quét vị trí [{task.TargetLocation.LocationCode}].", "Details", new { id = task.VoucherId });
        }

        var remain = task.TargetQty - task.PickedQty;
        if (qty != remain)
            return WorkflowResult.Failure($"Nhiệm vụ lấy tổng phải xác nhận đủ {remain:N2} trong một lần để đảm bảo phân loại chính xác.", "Details", new { id = task.VoucherId });

        var source = await _db.ItemLocations
            .FirstOrDefaultAsync(il => il.ItemId == task.ItemId
                && il.OwnerPartnerId == task.OwnerPartnerId
                && il.LocationId == task.SourceLocationId
                && il.LotNumber == task.LotNumber
                && il.ExpiryDate == task.ExpiryDate);
        if (source == null)
            return WorkflowResult.Failure("Không tìm thấy tồn tại vị trí nguồn.", "Details", new { id = task.VoucherId });
        if (source.HoldStatus != InventoryHoldStatusEnum.Available)
            return WorkflowResult.Failure("Vị trí nguồn đang có hàng bị giữ, không thể lấy tổng.", "Details", new { id = task.VoucherId });
        if (source.Quantity < qty)
            return WorkflowResult.Failure("Tồn thực tế tại vị trí nguồn đã thay đổi, không đủ số lượng để lấy tổng.", "Details", new { id = task.VoucherId });

        if (taskItem.TrackSerial)
        {
            if (qty != decimal.Truncate(qty))
                return WorkflowResult.Failure($"[{taskItem.ItemCode}] đang bật quản lý serial nên số lượng lấy phải là số nguyên.", "Details", new { id = task.VoucherId });

            var requiredSerialCount = (int)Math.Ceiling(qty);
            if (serialCodes == null || serialCodes.Count != requiredSerialCount)
                return WorkflowResult.Failure($"[{taskItem.ItemCode}] cần quét đúng {requiredSerialCount} serial cho bước lấy tổng.", "Details", new { id = task.VoucherId });

            await _serialInventoryService.MoveBulkReservedSerialsToStagingAsync(task, serialCodes, actor);
        }

        var destination = await _db.ItemLocations
            .FirstOrDefaultAsync(il => il.ItemId == task.ItemId
                && il.OwnerPartnerId == task.OwnerPartnerId
                && il.LocationId == task.TargetLocationId.Value
                && il.LotNumber == task.LotNumber
                && il.ExpiryDate == task.ExpiryDate);
        if (destination == null)
        {
            destination = new ItemLocation
            {
                ItemId = task.ItemId,
                OwnerPartnerId = task.OwnerPartnerId,
                LocationId = task.TargetLocationId.Value,
                LotNumber = task.LotNumber,
                ExpiryDate = task.ExpiryDate,
                HoldStatus = InventoryHoldStatusEnum.Available,
                Quantity = 0,
                ReservedQty = 0,
                UpdatedAt = VietnamNow
            };
            _db.ItemLocations.Add(destination);
            await _unitOfWork.SaveChangesAsync();
        }

        source.Quantity -= qty;
        source.UpdatedAt = VietnamNow;
        destination.Quantity += qty;
        destination.UpdatedAt = VietnamNow;

        var childSortTasks = await _db.PickTasks
            .Include(t => t.Allocations).ThenInclude(a => a.StockReservation)
            .Where(t => t.ParentPickTaskId == task.PickTaskId && t.PickTaskMode == PickTaskModeEnum.Sort)
            .ToListAsync();

        foreach (var child in childSortTasks)
        {
            child.Status = PickTaskStatusEnum.Pending;
            child.SourceLocationId = task.TargetLocationId.Value;
            child.SortationStageLocationId = task.TargetLocationId.Value;
            foreach (var allocation in child.Allocations)
            {
                if (allocation.StockReservation == null) continue;
                allocation.StockReservation.LocationId = task.TargetLocationId.Value;
                allocation.StockReservation.UpdatedAt = VietnamNow;
                allocation.StockReservation.Notes = string.IsNullOrWhiteSpace(allocation.StockReservation.Notes)
                    ? $"Đã chuyển sang vị trí tập kết {task.TargetLocation.LocationCode}"
                    : $"{allocation.StockReservation.Notes}; Tập kết {task.TargetLocation.LocationCode}";
            }
        }

        task.PickedQty = task.TargetQty;
        task.Status = PickTaskStatusEnum.Completed;
        task.CompletedAt = VietnamNow;
        if (string.IsNullOrWhiteSpace(task.AssignedTo))
        {
            task.AssignedTo = actor;
            task.AssignedAt = VietnamNow;
        }

        _db.PickTaskScanLogs.Add(new PickTaskScanLog
        {
            PickTaskId = task.PickTaskId,
            ScannedBy = actor,
            ScanValue = scanned,
            Qty = qty,
            Notes = $"Hoàn tất lấy tổng sang vị trí tập kết {task.TargetLocation.LocationCode}"
        });

        _db.AuditLogs.Add(new AuditLog
        {
            TableName = "PickTask",
            RecordId = task.PickTaskId.ToString(),
            ActionType = "BULK_PICK_TO_SORTATION",
            ColumnChanged = "LocationId",
            OldValue = task.SourceLocation?.LocationCode ?? task.SourceLocationId.ToString(),
            NewValue = task.TargetLocation.LocationCode,
            ChangedBy = actor,
            ChangedAt = VietnamNow,
            AppModule = "TwoStepPicking"
        });

        await _unitOfWork.SaveChangesAsync();
        await _reservationService.RecalculateReservedQtyAsync(new[] { source.ItemLocationId, destination.ItemLocationId });
        await _unitOfWork.SaveChangesAsync();

        var voucherIds = childSortTasks.SelectMany(t => t.Allocations.Select(a => a.VoucherId)).Distinct().ToList();
        var vouchers = await _db.Vouchers.Where(v => voucherIds.Contains(v.VoucherId)).ToListAsync();
        foreach (var v in vouchers)
        {
            if (v.FulfillmentStatus < FulfillmentStatusEnum.Picking)
                v.FulfillmentStatus = FulfillmentStatusEnum.Picking;
            v.UpdatedAt = VietnamNow;
        }
        await _unitOfWork.SaveChangesAsync();

        return WorkflowResult.Success("Đã lấy tổng vào vị trí tập kết. Các nhiệm vụ phân loại liên quan đã sẵn sàng.", "Details", new { id = task.VoucherId });
    }

    private sealed record ShortPickSlice(
        PickTaskAllocation? Allocation,
        StockReservation? StockReservation,
        long VoucherId,
        long? VoucherDetailId,
        decimal Qty);

    private sealed record ReplacementReservation(
        StockReservation Reservation,
        int LocationId,
        string? LotNumber,
        DateTime? ExpiryDate,
        decimal Qty);

    private sealed record ShortPickReallocationResult(
        decimal ShortQty,
        decimal ReallocatedQty,
        decimal BackorderSuggestedQty,
        int ReplacementTaskCount);

    private async Task<int> ResolveTaskWarehouseIdAsync(PickTask task)
    {
        if (task.Wave?.WarehouseId > 0)
            return task.Wave.WarehouseId;
        if (task.WaveId.HasValue)
            return await _db.Waves.Where(w => w.WaveId == task.WaveId.Value).Select(w => w.WarehouseId).FirstAsync();
        if (task.Voucher?.WarehouseId > 0)
            return task.Voucher.WarehouseId;
        return await _db.Vouchers.Where(v => v.VoucherId == task.VoucherId).Select(v => v.WarehouseId).FirstAsync();
    }

    private async Task<ShortPickReallocationResult> ReallocateShortPickAsync(PickTask task, string actor)
    {
        var shortQty = task.TargetQty - task.PickedQty;
        if (shortQty <= 0)
            return new ShortPickReallocationResult(0, 0, 0, 0);

        var slices = await BuildShortPickSlicesAsync(task);
        if (slices.Count == 0)
            return new ShortPickReallocationResult(shortQty, 0, shortQty, 0);

        var affectedItemLocationIds = new List<int>();
        foreach (var slice in slices)
        {
            var reservation = slice.StockReservation;
            if (reservation == null)
                continue;

            var openQty = reservation.ReservedQty - reservation.ConsumedQty - reservation.ReleasedQty;
            var releaseQty = Math.Min(openQty, slice.Qty);
            if (releaseQty <= 0)
                continue;

            await _serialInventoryService.ReleaseOpenForReservationAsync(reservation, actor, (int)Math.Ceiling(releaseQty), "Short pick reallocation.");
            reservation.ReleasedQty += releaseQty;
            if (reservation.ReservedQty - reservation.ConsumedQty - reservation.ReleasedQty <= 0)
                reservation.Status = ReservationStatusEnum.Released;
            reservation.UpdatedAt = VietnamNow;

            var oldItemLocationId = await _db.ItemLocations
                .Where(il => il.ItemId == reservation.ItemId
                    && il.OwnerPartnerId == reservation.OwnerPartnerId
                    && il.LocationId == reservation.LocationId
                    && il.LotNumber == reservation.LotNumber
                    && il.ExpiryDate == reservation.ExpiryDate)
                .Select(il => (int?)il.ItemLocationId)
                .FirstOrDefaultAsync();
            if (oldItemLocationId.HasValue)
                affectedItemLocationIds.Add(oldItemLocationId.Value);
        }

        task.Status = PickTaskStatusEnum.Short;
        task.CompletedAt = VietnamNow;

        var warehouseId = await ResolveTaskWarehouseIdAsync(task);
        var alternativePicks = await AllocateFefoExcludingLocationsAsync(
            task.ItemId,
            warehouseId,
            shortQty,
            new[] { task.SourceLocationId },
            task.OwnerPartnerId);

        var replacements = new List<ReplacementReservation>();
        var pickIndex = 0;
        var currentPickRemaining = pickIndex < alternativePicks.Count ? alternativePicks[pickIndex].Qty : 0m;

        foreach (var slice in slices)
        {
            var sliceRemaining = slice.Qty;
            while (sliceRemaining > 0 && pickIndex < alternativePicks.Count)
            {
                var pick = alternativePicks[pickIndex];
                var take = Math.Min(sliceRemaining, currentPickRemaining);
                if (take <= 0)
                    break;

                var reservation = new StockReservation
                {
                    VoucherId = slice.VoucherId,
                    VoucherDetailId = slice.VoucherDetailId,
                    ItemId = task.ItemId,
                    OwnerPartnerId = task.OwnerPartnerId,
                    LocationId = pick.LocationId,
                    LotNumber = pick.LotNumber,
                    ExpiryDate = pick.ExpiryDate,
                    ReservedQty = take,
                    Status = ReservationStatusEnum.Active,
                    CreatedBy = actor,
                    CreatedAt = VietnamNow,
                    Notes = $"Phân bổ lại do thiếu hàng từ {task.TaskCode}"
                };
                _db.StockReservations.Add(reservation);
                replacements.Add(new ReplacementReservation(reservation, pick.LocationId, pick.LotNumber, pick.ExpiryDate, take));

                var newItemLocationId = await _db.ItemLocations
                    .Where(il => il.ItemId == task.ItemId
                        && il.OwnerPartnerId == task.OwnerPartnerId
                        && il.LocationId == pick.LocationId
                        && il.LotNumber == pick.LotNumber
                        && il.ExpiryDate == pick.ExpiryDate)
                    .Select(il => (int?)il.ItemLocationId)
                    .FirstOrDefaultAsync();
                if (newItemLocationId.HasValue)
                    affectedItemLocationIds.Add(newItemLocationId.Value);

                sliceRemaining -= take;
                currentPickRemaining -= take;
                if (currentPickRemaining <= 0)
                {
                    pickIndex++;
                    currentPickRemaining = pickIndex < alternativePicks.Count ? alternativePicks[pickIndex].Qty : 0m;
                }
            }
        }

        var replacementTaskCount = await CreateReplacementPickTasksAsync(task, replacements, actor);
        var reallocatedQty = replacements.Sum(r => r.Qty);
        var backorderSuggestedQty = Math.Max(0, shortQty - reallocatedQty);

        await UpsertShortPickExceptionAsync(task, shortQty, reallocatedQty, backorderSuggestedQty, actor);
        await _unitOfWork.SaveChangesAsync();
        foreach (var voucherId in replacements.Select(r => r.Reservation.VoucherId).Distinct())
            await _serialInventoryService.AllocateForVoucherAsync(voucherId, actor, $"voucher:{voucherId}:short-reallocation:{task.PickTaskId}");
        await _reservationService.RecalculateReservedQtyAsync(affectedItemLocationIds);

        return new ShortPickReallocationResult(shortQty, reallocatedQty, backorderSuggestedQty, replacementTaskCount);
    }

    private async Task<List<ShortPickSlice>> BuildShortPickSlicesAsync(PickTask task)
    {
        var slices = new List<ShortPickSlice>();
        if (task.Allocations.Any())
        {
            foreach (var allocation in task.Allocations.OrderBy(a => a.PickTaskAllocationId))
            {
                var remaining = allocation.AllocatedQty - allocation.PickedQty;
                if (remaining <= 0)
                    continue;

                slices.Add(new ShortPickSlice(
                    allocation,
                    allocation.StockReservation,
                    allocation.VoucherId,
                    allocation.VoucherDetailId,
                    remaining));
            }

            return slices;
        }

        var legacyRemaining = task.TargetQty - task.PickedQty;
        if (legacyRemaining <= 0)
            return slices;

        var reservation = await _db.StockReservations
            .FirstOrDefaultAsync(r => r.VoucherId == task.VoucherId
                && r.VoucherDetailId == task.VoucherDetailId
                && r.ItemId == task.ItemId
                && r.LocationId == task.SourceLocationId
                && r.LotNumber == task.LotNumber
                && r.ExpiryDate == task.ExpiryDate
                && r.Status == ReservationStatusEnum.Active);

        slices.Add(new ShortPickSlice(null, reservation, task.VoucherId, task.VoucherDetailId, legacyRemaining));
        return slices;
    }

    private async Task<int> CreateReplacementPickTasksAsync(
        PickTask originalTask,
        List<ReplacementReservation> replacements,
        string actor)
    {
        var taskCount = 0;
        foreach (var group in replacements.GroupBy(x => new { x.LocationId, x.LotNumber, x.ExpiryDate }))
        {
            var targetQty = group.Sum(x => x.Qty);
            if (targetQty <= 0)
                continue;

            var first = group.First();
            var distinctVoucherCount = group.Select(x => x.Reservation.VoucherId).Distinct().Count();
            var replacementTask = new PickTask
            {
                TaskCode = await GenerateReplacementTaskCodeAsync(originalTask.WaveId, originalTask.Wave?.WaveCode, originalTask.VoucherId),
                WaveId = originalTask.WaveId,
                VoucherId = first.Reservation.VoucherId,
                VoucherDetailId = first.Reservation.VoucherDetailId,
                OwnerPartnerId = originalTask.OwnerPartnerId,
                ItemId = originalTask.ItemId,
                SourceLocationId = group.Key.LocationId,
                TargetLocationId = originalTask.TargetLocationId,
                LotNumber = group.Key.LotNumber,
                ExpiryDate = group.Key.ExpiryDate,
                TargetQty = targetQty,
                PickedQty = 0,
                Status = string.IsNullOrWhiteSpace(originalTask.AssignedTo) ? PickTaskStatusEnum.Pending : PickTaskStatusEnum.Assigned,
                AssignedTo = originalTask.AssignedTo,
                AssignedAt = string.IsNullOrWhiteSpace(originalTask.AssignedTo) ? null : VietnamNow,
                DueAt = originalTask.DueAt ?? VietnamNow.AddHours(24),
                IsBatchPick = originalTask.IsBatchPick || distinctVoucherCount > 1 || group.Count() > 1,
                BatchGroupKey = $"{originalTask.BatchGroupKey ?? originalTask.PickTaskId.ToString()}|REALLOC|{originalTask.PickTaskId}"
            };
            _db.PickTasks.Add(replacementTask);

            foreach (var replacement in group)
            {
                _db.PickTaskAllocations.Add(new PickTaskAllocation
                {
                    PickTask = replacementTask,
                    StockReservation = replacement.Reservation,
                    VoucherId = replacement.Reservation.VoucherId,
                    VoucherDetailId = replacement.Reservation.VoucherDetailId,
                    AllocatedQty = replacement.Qty,
                    PickedQty = 0
                });
            }

            _db.AuditLogs.Add(new AuditLog
            {
                TableName = "PickTasks",
                RecordId = originalTask.PickTaskId.ToString(),
                ActionType = "SHORT_PICK_REALLOCATION",
                ColumnChanged = "Status",
                OldValue = "InProgress",
                NewValue = $"Short; replacement {replacementTask.TaskCode}; qty {targetQty:N2}",
                ChangedBy = actor,
                ChangedAt = VietnamNow,
                AppModule = "Outbound"
            });

            taskCount++;
        }

        return taskCount;
    }

    private async Task<string> GenerateReplacementTaskCodeAsync(long? waveId, string? waveCode, long voucherId)
    {
        var prefix = string.IsNullOrWhiteSpace(waveCode)
            ? (waveId.HasValue ? $"WV{waveId.Value}" : $"TT{voucherId}")
            : waveCode.Trim();
        for (var attempt = 1; attempt <= 99; attempt++)
        {
            var seq = waveId.HasValue
                ? await _db.PickTasks.CountAsync(t => t.WaveId == waveId) + attempt
                : await _db.PickTasks.CountAsync(t => t.VoucherId == voucherId) + attempt;
            var code = $"PT-{prefix}-R{seq:D3}";
            if (code.Length > 40)
                code = waveId.HasValue ? $"PT-R{waveId.Value}-{seq:D3}" : $"PT-RTT{voucherId}-{seq:D3}";
            if (!await _db.PickTasks.AnyAsync(t => t.TaskCode == code))
                return code;
        }

        return $"PT-R{(waveId?.ToString() ?? $"TT{voucherId}")}-{Guid.NewGuid():N}"[..40];
    }

    private async Task<List<FefoAllocation>> AllocateFefoExcludingLocationsAsync(
        int itemId,
        int warehouseId,
        decimal requiredBaseQty,
        IEnumerable<int> excludedLocationIds,
        int? ownerPartnerId = null)
    {
        var remaining = requiredBaseQty;
        var picks = new List<FefoAllocation>();
        if (requiredBaseQty <= 0)
            return picks;

        var excluded = excludedLocationIds.ToHashSet();
        var minExpiryDate = VietnamNow.Date.AddDays(30);
        var candidates = await _db.ItemLocations
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Where(il => il.ItemId == itemId
                && il.OwnerPartnerId == ownerPartnerId
                && !excluded.Contains(il.LocationId)
                && il.Quantity > il.ReservedQty
                && il.HoldStatus == InventoryHoldStatusEnum.Available
                && il.Location != null && il.Location.IsActive
                && il.Location.Zone != null && il.Location.Zone.WarehouseId == warehouseId
                && (il.ExpiryDate == null || il.ExpiryDate >= minExpiryDate))
            .OrderBy(il => il.ExpiryDate.HasValue ? 0 : 1).ThenBy(il => il.ExpiryDate)
            .ThenByDescending(il => il.Quantity - il.ReservedQty)
            .ThenBy(il => il.ItemLocationId)
            .ToListAsync();

        foreach (var candidate in candidates)
        {
            var available = candidate.Quantity - candidate.ReservedQty;
            if (available <= 0)
                continue;

            var take = Math.Min(remaining, available);
            if (take <= 0)
                continue;

            picks.Add(new FefoAllocation(candidate.LocationId, candidate.LotNumber, candidate.ExpiryDate, take));
            remaining -= take;
            if (remaining <= 0)
                break;
        }

        return picks;
    }

    private async Task UpsertShortPickExceptionAsync(
        PickTask task,
        decimal shortQty,
        decimal reallocatedQty,
        decimal backorderSuggestedQty,
        string actor)
    {
        var warehouseId = await ResolveTaskWarehouseIdAsync(task);
        var voucherCode = task.Voucher?.VoucherCode ?? await _db.Vouchers
            .Where(v => v.VoucherId == task.VoucherId)
            .Select(v => v.VoucherCode)
            .FirstOrDefaultAsync() ?? "";
        var itemCode = task.Item?.ItemCode ?? await _db.Items
            .Where(i => i.ItemId == task.ItemId)
            .Select(i => i.ItemCode)
            .FirstOrDefaultAsync() ?? "";
        var locationCode = task.SourceLocation?.LocationCode ?? await _db.Locations
            .Where(l => l.LocationId == task.SourceLocationId)
            .Select(l => l.LocationCode)
            .FirstOrDefaultAsync() ?? "";

        var exceptionKey = ComputeOperationExceptionKey("pick_short", warehouseId, task.TaskCode, voucherCode, itemCode, locationCode);
        var exceptionCase = await _db.OperationExceptionCases.FirstOrDefaultAsync(c => c.ExceptionKey == exceptionKey);
        if (exceptionCase == null)
        {
            exceptionCase = new OperationExceptionCase
            {
                ExceptionKey = exceptionKey,
                CategoryKey = "pick_short",
                CategoryLabel = "Lấy hàng thiếu",
                WarehouseId = warehouseId,
                ReferenceCode = task.TaskCode,
                SecondaryReference = voucherCode,
                Status = OperationExceptionStatusEnum.Open,
                FirstDetectedAt = VietnamNow
            };
            _db.OperationExceptionCases.Add(exceptionCase);
        }

        exceptionCase.LastDetectedAt = VietnamNow;
        exceptionCase.UpdatedAt = VietnamNow;
        exceptionCase.ResolutionNote = $"Short {shortQty:N2}; reallocated {reallocatedQty:N2}; partial/backorder suggested {backorderSuggestedQty:N2}; actor {actor}";
        if (exceptionCase.Status == OperationExceptionStatusEnum.Resolved)
        {
            exceptionCase.Status = OperationExceptionStatusEnum.Open;
            exceptionCase.ResolvedAt = null;
            exceptionCase.ResolvedBy = null;
        }
    }

    private static string ComputeOperationExceptionKey(
        string categoryKey,
        int warehouseId,
        string referenceCode,
        string? secondaryReference,
        string? itemCode,
        string? locationCode)
        => string.Join("|",
            categoryKey.Trim().ToUpperInvariant(),
            warehouseId,
            (referenceCode ?? string.Empty).Trim().ToUpperInvariant(),
            (secondaryReference ?? string.Empty).Trim().ToUpperInvariant(),
            (itemCode ?? string.Empty).Trim().ToUpperInvariant(),
            (locationCode ?? string.Empty).Trim().ToUpperInvariant());

    private async Task<List<FefoAllocation>> AllocateFefoAsync(int itemId, int warehouseId, decimal requiredBaseQty, int? ownerPartnerId = null)
    {
        var remaining = requiredBaseQty;
        var picks = new List<FefoAllocation>();
        if (requiredBaseQty <= 0) return picks;

        var minExpiryDate = VietnamNow.Date.AddDays(30);
        var candidates = await _db.ItemLocations
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Where(il => il.ItemId == itemId && il.OwnerPartnerId == ownerPartnerId && il.Quantity > il.ReservedQty
                && il.HoldStatus == InventoryHoldStatusEnum.Available
                && il.Location != null && il.Location.IsActive
                && il.Location.Zone != null && il.Location.Zone.WarehouseId == warehouseId
                && (il.ExpiryDate == null || il.ExpiryDate >= minExpiryDate))
            .OrderBy(il => il.ExpiryDate.HasValue ? 0 : 1).ThenBy(il => il.ExpiryDate)
            .ThenByDescending(il => il.Quantity - il.ReservedQty)
            .ToListAsync();

        foreach (var c in candidates)
        {
            var available = c.Quantity - c.ReservedQty;
            if (available <= 0) continue;
            var take = Math.Min(remaining, available);
            if (take <= 0) continue;
            picks.Add(new FefoAllocation(c.LocationId, c.LotNumber, c.ExpiryDate, take));
            remaining -= take;
            if (remaining <= 0) break;
        }

        if (picks.Count == 0) throw WmsExceptions.NoAvailableStock(itemId);
        return picks;
    }
}
