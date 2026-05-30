using System.Data;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

/// <summary>
/// P0-04: Service layer for voucher cancellation workflow.
/// Encapsulates stock reversal, reservation release, LPN/serial voiding,
/// and cascade PickTask/Wave cancellation with a single transaction boundary.
/// </summary>
public interface IVoucherCancellationService
{
    Task<WorkflowResult> CancelVoucherAsync(
        long voucherId,
        string? cancelReason,
        CancelReasonEnum? cancelReasonCode,
        int? scopedWarehouseId,
        string actor,
        string? ipAddress,
        DateTime? lockDate);
}

public class VoucherCancellationService : IVoucherCancellationService
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryReservationService _reservationService;
    private readonly IInventoryBalanceService _inventoryBalanceService;
    private readonly ISerialInventoryService _serialInventoryService;
    private readonly IInventoryTransactionService _inventoryTransactionService;

    private static DateTime VietnamNow => VietnamTime.Now;

    public VoucherCancellationService(
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

    public async Task<WorkflowResult> CancelVoucherAsync(
        long voucherId,
        string? cancelReason,
        CancelReasonEnum? cancelReasonCode,
        int? scopedWarehouseId,
        string actor,
        string? ipAddress,
        DateTime? lockDate)
    {
        // ─── Normalize cancel reason ───
        var normalizedCancelReason = string.IsNullOrWhiteSpace(cancelReason) ? null : cancelReason.Trim();
        if (cancelReasonCode.HasValue && cancelReasonCode.Value != CancelReasonEnum.Other)
        {
            if (string.IsNullOrWhiteSpace(normalizedCancelReason))
            {
                normalizedCancelReason = cancelReasonCode.Value switch
                {
                    CancelReasonEnum.InsufficientStock => "Thiếu tồn kho",
                    CancelReasonEnum.WrongInfo => "Sai thông tin",
                    CancelReasonEnum.CustomerCancelled => "KH/Bộ phận hủy",
                    CancelReasonEnum.ItemDiscontinued => "Hàng ngưng sản xuất",
                    CancelReasonEnum.DuplicateVoucher => "Trùng phiếu",
                    CancelReasonEnum.OperationalError => "Lỗi thao tác",
                    _ => cancelReasonCode.Value.ToString()
                };
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(normalizedCancelReason))
                throw WmsExceptions.CancelReasonRequired();
        }
        if (normalizedCancelReason!.Length > 500)
            throw WmsExceptions.CancelReasonTooLong();

        var voucher = await _db.Vouchers
            .Include(v => v.Details).ThenInclude(d => d.Item)
            .FirstOrDefaultAsync(v => v.VoucherId == voucherId);

        if (voucher == null) throw WmsExceptions.VoucherNotFound();
        if (voucher.IsCancelled) throw WmsExceptions.VoucherAlreadyCancelled();
        if (string.Equals(voucher.CreatedBy, actor, StringComparison.OrdinalIgnoreCase))
            throw WmsExceptions.CannotCancelOwnVoucher(actor);
        if (!string.IsNullOrWhiteSpace(voucher.ReviewedBy)
            && string.Equals(voucher.ReviewedBy, actor, StringComparison.OrdinalIgnoreCase))
            throw WmsExceptions.CannotCancelApprovedVoucher();

        if (scopedWarehouseId.HasValue && voucher.WarehouseId != scopedWarehouseId.Value)
            return WorkflowResult.ForbiddenResult();

        var transactionDate = ResolveLockTransactionDate(voucher, VietnamNow);
        if (lockDate.HasValue && transactionDate.Date <= lockDate.Value.Date)
            throw WmsExceptions.WarehouseLockedForCancel(transactionDate.ToString("dd/MM/yyyy"), lockDate.Value);

        // P0-05: Single transaction boundary
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // P1-8: re-check warehouse period lock BÊN TRONG transaction để chặn TOCTOU
            // (kho có thể bị lock sau khi controller snapshot lockDate trước khi vào service).
            var freshLockDate = await _db.WarehousePeriodLocks.AsNoTracking()
                .Where(l => l.WarehouseId == voucher.WarehouseId && l.IsActive)
                .OrderByDescending(l => l.LockDate)
                .Select(l => (DateTime?)l.LockDate)
                .FirstOrDefaultAsync();
            if (freshLockDate.HasValue && transactionDate.Date <= freshLockDate.Value.Date)
            {
                await _unitOfWork.RollbackAsync();
                throw WmsExceptions.WarehouseLockedForCancel(transactionDate.ToString("dd/MM/yyyy"), freshLockDate.Value);
            }

            using var ledgerScope = _inventoryTransactionService.BeginScope(new InventoryTransactionContext
            {
                TransactionType = InventoryTransactionTypeEnum.Cancel,
                TransactionGroupKey = $"voucher:{voucher.VoucherId}:cancel",
                IdempotencyKeyPrefix = $"voucher:{voucher.VoucherId}:cancel",
                WarehouseId = voucher.WarehouseId,
                VoucherId = voucher.VoucherId,
                ReferenceType = "Voucher",
                ReferenceId = voucher.VoucherId.ToString(),
                ReferenceCode = voucher.VoucherCode,
                Actor = actor
            });
            var isOutboundVoucher = voucher.VoucherType is VoucherTypeEnum.XuatKho or VoucherTypeEnum.TraNCC or VoucherTypeEnum.ChuyenKho or VoucherTypeEnum.XuatSanXuat;
            var hasConsumedReservations = isOutboundVoucher && await _db.StockReservations
                .AnyAsync(r => r.VoucherId == voucher.VoucherId && r.ConsumedQty > 0);

            if (voucher.IsPosted || hasConsumedReservations)
            {
                if (isOutboundVoucher)
                {
                    await UndoOutboundPosted(voucher);
                }
                else
                {
                    await UndoInboundPosted(voucher);
                }
            }
            else
            {
                await ReleaseDraftReservations(voucher);
            }
            await _serialInventoryService.ReleaseOpenForVoucherAsync(voucher.VoucherId, actor, "Voucher cancelled.");

            voucher.IsCancelled = true;
            voucher.CancelledBy = actor;
            voucher.CancelledAt = VietnamNow;
            voucher.CancelReason = normalizedCancelReason;
            voucher.CancelReasonCode = cancelReasonCode ?? CancelReasonEnum.Other;

            // Void LPNs
            var relatedLpns = await _db.LicensePlates
                .Where(l => l.VoucherId == voucher.VoucherId && l.IsActive)
                .ToListAsync();
            foreach (var lpn in relatedLpns)
            {
                lpn.IsActive = false;
                lpn.Status = LpnStatusEnum.Voided;
                lpn.VoidedAt = VietnamNow;
                lpn.VoidedBy = actor;
                lpn.UpdatedAt = VietnamNow;
            }

            // Void Serials
            var relatedSerials = await _db.SerialNumbers
                .Where(s => s.VoucherId == voucher.VoucherId && s.Status == SerialNumberStatusEnum.Active)
                .ToListAsync();
            foreach (var serial in relatedSerials)
            {
                serial.Status = SerialNumberStatusEnum.Voided;
                serial.VoidedAt = VietnamNow;
                serial.VoidedBy = actor;
                serial.UpdatedAt = VietnamNow;
            }

            // Void serial assignments
            var openSerialAssignments = await _db.PickTaskSerialAssignments
                .Where(x => x.VoucherId == voucher.VoucherId && x.VoidedAt == null && x.PostedAt == null)
                .ToListAsync();
            foreach (var assignment in openSerialAssignments)
            {
                assignment.VoidedAt = VietnamNow;
                assignment.VoidedBy = actor;
            }

            // R3-2: void CatchWeightEntries để không còn cân được tính sau khi phiếu đã hủy.
            var relatedCatchWeights = await _db.CatchWeightEntries
                .Where(c => c.VoucherId == voucher.VoucherId && c.Status == CatchWeightStatusEnum.Captured)
                .ToListAsync();
            foreach (var cw in relatedCatchWeights)
                cw.Status = CatchWeightStatusEnum.Voided;

            // ═══ CASCADE: Hủy PickTask & cập nhật Wave khi hủy phiếu ═══
            var relatedTasks = await _db.PickTasks
                .Where(t => t.VoucherId == voucher.VoucherId
                    && t.Status != PickTaskStatusEnum.Cancelled)
                .ToListAsync();
            foreach (var task in relatedTasks)
            {
                task.Status = PickTaskStatusEnum.Cancelled;
                task.CompletedAt = VietnamNow;
            }

            if (voucher.WaveId.HasValue)
            {
                var waveHasActiveVouchers = await _db.Vouchers
                    .AnyAsync(v => v.WaveId == voucher.WaveId.Value
                        && v.VoucherId != voucher.VoucherId
                        && !v.IsCancelled);
                if (!waveHasActiveVouchers)
                {
                    var wave = await _db.Waves.FindAsync(voucher.WaveId.Value);
                    if (wave != null)
                    {
                        wave.Status = WaveStatusEnum.Cancelled;
                        wave.CompletedAt = VietnamNow;
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();

            // P0-03: Sync CurrentStock from ItemLocation source of truth after cancel reversal
            var cancelAffectedItemIds = voucher.Details.Select(d => d.ItemId).Distinct();
            await _inventoryBalanceService.SyncCurrentStockAsync(cancelAffectedItemIds);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            var message = relatedTasks.Count > 0
                ? $"Đã hủy phiếu thành công! Đồng thời hủy {relatedTasks.Count} nhiệm vụ lấy hàng liên quan."
                : $"Đã hủy phiếu thành công!";
            return WorkflowResult.Success(message, "Details", new { id = voucherId });
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    private async Task UndoOutboundPosted(Voucher voucher)
    {
        var consumedReservations = await _db.StockReservations
            .Where(r => r.VoucherId == voucher.VoucherId && (r.Status == ReservationStatusEnum.Consumed || r.ConsumedQty > 0))
            .ToListAsync();

        var itemIds = consumedReservations.Select(r => r.ItemId).Distinct().ToList();
        var itemsById = await _db.Items.Where(i => itemIds.Contains(i.ItemId)).ToDictionaryAsync(i => i.ItemId, i => i);
        var affectedItemLocationIds = new List<int>();

        // N+1 FIX: Pre-load all relevant ItemLocations
        var cancelLocationIds = consumedReservations.Select(r => r.LocationId).ToList();
        var destLocationIds = voucher.Details.Where(d => d.DestLocationId.HasValue).Select(d => d.DestLocationId!.Value).ToList();
        var allCancelLocIds = cancelLocationIds.Union(destLocationIds).Distinct().ToList();
        var cancelOwnerIds = consumedReservations.Select(r => r.OwnerPartnerId).Distinct().ToList();
        var cancelItemLocations = allCancelLocIds.Count > 0
            ? await _db.ItemLocations.Where(il => allCancelLocIds.Contains(il.LocationId) && itemIds.Contains(il.ItemId) && cancelOwnerIds.Contains(il.OwnerPartnerId)).ToListAsync()
            : new List<ItemLocation>();
        var cancelIlLookup = new Dictionary<(int, int, string, DateTime?, int?), ItemLocation>();
        foreach (var il in cancelItemLocations)
            cancelIlLookup[(il.ItemId, il.LocationId, il.LotNumber ?? "", il.ExpiryDate, il.OwnerPartnerId)] = il;

        foreach (var r in consumedReservations)
        {
            var consumed = Math.Max(0, r.ConsumedQty);
            if (consumed == 0) continue;

            var srcKey = (r.ItemId, r.LocationId, r.LotNumber ?? "", r.ExpiryDate, r.OwnerPartnerId);
            cancelIlLookup.TryGetValue(srcKey, out var src);
            if (src == null)
                throw WmsExceptions.UndoLocationNotFound(r.ItemId.ToString());
            src.Quantity += consumed;
            src.UpdatedAt = VietnamNow;
            affectedItemLocationIds.Add(src.ItemLocationId);

            if (voucher.VoucherType == VoucherTypeEnum.ChuyenKho)
            {
                var detail = voucher.Details.FirstOrDefault(d => d.VoucherDetailId == r.VoucherDetailId);
                if (detail?.DestLocationId == null)
                    throw WmsExceptions.UndoTransferDestinationLocationNotFound();

                var destKey = (r.ItemId, detail.DestLocationId.Value, r.LotNumber ?? "", r.ExpiryDate, r.OwnerPartnerId);
                cancelIlLookup.TryGetValue(destKey, out var dest);
                if (dest == null)
                    throw WmsExceptions.UndoItemLocationNotFound(r.ItemId.ToString());
                dest.Quantity -= consumed;
                if (dest.Quantity < 0)
                    throw WmsExceptions.UndoTransferMakesNegativeDestination(r.ItemId.ToString());
                dest.UpdatedAt = VietnamNow;
                affectedItemLocationIds.Add(dest.ItemLocationId);
            }
            else if (itemsById.TryGetValue(r.ItemId, out var itemOut))
            {
                // P1-2: dùng UnitPrice tại thời điểm xuất (snapshot trên VoucherDetail) thay vì UnitCost hiện tại,
                // để hủy không lệch giá trị tồn nếu Admin đã chỉnh UnitCost sau khi xuất.
                var detailAtIssue = voucher.Details.FirstOrDefault(d => d.VoucherDetailId == r.VoucherDetailId);
                var unitPriceAtIssue = detailAtIssue?.UnitPrice ?? itemOut.UnitCost;
                itemOut.CurrentStock += consumed;
                itemOut.TotalStockValue += consumed * unitPriceAtIssue;
                itemOut.UpdatedAt = VietnamNow;
            }

            r.ReleasedQty += consumed;
            r.ConsumedQty = 0;
            r.Status = ReservationStatusEnum.Released;
            r.UpdatedAt = VietnamNow;
        }

        var activeReservations = await _db.StockReservations
            .Where(r => r.VoucherId == voucher.VoucherId && r.Status == ReservationStatusEnum.Active)
            .ToListAsync();
        foreach (var r in activeReservations)
        {
            r.ReleasedQty = r.ReservedQty - r.ConsumedQty;
            r.Status = ReservationStatusEnum.Released;
            r.UpdatedAt = VietnamNow;

            var activeKey = (r.ItemId, r.LocationId, r.LotNumber ?? "", r.ExpiryDate, r.OwnerPartnerId);
            if (cancelIlLookup.TryGetValue(activeKey, out var il))
                affectedItemLocationIds.Add(il.ItemLocationId);
        }
        await _reservationService.RecalculateReservedQtyAsync(affectedItemLocationIds);
    }

    private async Task UndoInboundPosted(Voucher voucher)
    {
        var inboundLocIds = voucher.Details.Where(d => d.LocationId.HasValue).Select(d => d.LocationId!.Value).Distinct().ToList();
        var inboundItemIds = voucher.Details.Select(d => d.ItemId).Distinct().ToList();
        var inboundOwnerIds = voucher.Details.Select(d => d.OwnerPartnerId ?? voucher.OwnerPartnerId).Distinct().ToList();
        var inboundIlList = inboundLocIds.Count > 0
            ? await _db.ItemLocations.Where(il => inboundLocIds.Contains(il.LocationId) && inboundItemIds.Contains(il.ItemId) && inboundOwnerIds.Contains(il.OwnerPartnerId)).ToListAsync()
            : new List<ItemLocation>();
        var inboundIlLookup = new Dictionary<(int, int, string, DateTime?, int?), ItemLocation>();
        foreach (var il in inboundIlList)
            inboundIlLookup[(il.ItemId, il.LocationId, il.LotNumber ?? "", il.ExpiryDate, il.OwnerPartnerId)] = il;

        foreach (var detail in voucher.Details)
        {
            var item = detail.Item;
            if (item == null) continue;

            decimal appliedBaseQty = detail.BaseQty;
            if (voucher.VoucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.NhapThanhPham)
            {
                var defectBase = detail.DefectBaseQty > 0 ? detail.DefectBaseQty : (detail.DefectQty * (detail.ConversionRate == 0 ? 1 : Math.Abs(detail.ConversionRate)));
                defectBase = Math.Max(0, defectBase);
                appliedBaseQty = Math.Max(0, detail.BaseQty - defectBase);
            }

            if (detail.LocationId.HasValue)
            {
                var inboundKey = (detail.ItemId, detail.LocationId.Value, detail.LotNumber ?? "", detail.ExpiryDate, detail.OwnerPartnerId ?? voucher.OwnerPartnerId);
                inboundIlLookup.TryGetValue(inboundKey, out var itemLoc);
                if (itemLoc == null)
                    throw WmsExceptions.UndoItemLocationNotFound(item.ItemCode);

                if (voucher.VoucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham)
                {
                    itemLoc.Quantity -= appliedBaseQty;
                    if (itemLoc.Quantity < 0)
                        throw WmsExceptions.UndoCancelMakesNegativeLocationStock(item.ItemCode, itemLoc.Quantity + detail.BaseQty);
                }
                else if (voucher.VoucherType == VoucherTypeEnum.DieuChinh)
                {
                    itemLoc.Quantity -= detail.BaseQty;
                    if (itemLoc.Quantity < 0)
                        throw WmsExceptions.UndoAdjustMakesNegativeLocationStock(item.ItemCode, itemLoc.Quantity + detail.BaseQty);
                }
                itemLoc.UpdatedAt = VietnamNow;
            }

            if (voucher.VoucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham)
            {
                item.CurrentStock -= appliedBaseQty;
                if (item.CurrentStock < 0)
                    throw WmsExceptions.UndoCancelMakesNegativeItemStock(item.ItemCode);
            }
            else if (voucher.VoucherType == VoucherTypeEnum.DieuChinh)
            {
                item.CurrentStock -= detail.BaseQty;
                if (item.CurrentStock < 0)
                    throw WmsExceptions.UndoAdjustMakesNegativeItemStock(item.ItemCode);
            }

            item.TotalStockValue = item.CurrentStock * item.UnitCost;
            item.UpdatedAt = VietnamNow;
        }
    }

    private async Task ReleaseDraftReservations(Voucher voucher)
    {
        var activeReservations = await _db.StockReservations
            .Where(r => r.VoucherId == voucher.VoucherId && r.Status == ReservationStatusEnum.Active)
            .ToListAsync();
        if (activeReservations.Count > 0)
        {
            var affectedItemLocationIds = new List<int>();

            var draftLocIds = activeReservations.Select(r => r.LocationId).Distinct().ToList();
            var draftItemIds = activeReservations.Select(r => r.ItemId).Distinct().ToList();
            var draftOwnerIds = activeReservations.Select(r => r.OwnerPartnerId).Distinct().ToList();
            var draftIlList = await _db.ItemLocations.AsNoTracking()
                .Where(il => draftLocIds.Contains(il.LocationId) && draftItemIds.Contains(il.ItemId) && draftOwnerIds.Contains(il.OwnerPartnerId))
                .ToListAsync();
            var draftIlLookup = new Dictionary<(int, int, string, DateTime?, int?), ItemLocation>();
            foreach (var il in draftIlList)
                draftIlLookup[(il.ItemId, il.LocationId, il.LotNumber ?? "", il.ExpiryDate, il.OwnerPartnerId)] = il;

            foreach (var r in activeReservations)
            {
                r.ReleasedQty = r.ReservedQty - r.ConsumedQty;
                r.Status = ReservationStatusEnum.Released;
                r.UpdatedAt = VietnamNow;

                var draftKey = (r.ItemId, r.LocationId, r.LotNumber ?? "", r.ExpiryDate, r.OwnerPartnerId);
                if (draftIlLookup.TryGetValue(draftKey, out var il))
                    affectedItemLocationIds.Add(il.ItemLocationId);
            }
            await _reservationService.RecalculateReservedQtyAsync(affectedItemLocationIds);
        }
    }

    private static DateTime ResolveLockTransactionDate(Voucher voucher, DateTime now)
    {
        return voucher.CompletedAt
            ?? voucher.ReviewedAt
            ?? now;
    }
}
