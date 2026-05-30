using System.Data;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

/// <summary>
/// P0-04: Service layer for inbound completion workflow.
/// Encapsulates stock posting, LPN creation, weighted average cost update,
/// and alert management with a single transaction boundary (P0-05).
/// </summary>
public interface IInboundExecutionService
{
    /// <summary>
    /// Complete an inbound voucher: post stock to ItemLocation, create LPN,
    /// update WAC, sync Item.CurrentStock, and handle serial number assignment.
    /// </summary>
    Task<WorkflowResult> CompleteInboundAsync(
        long voucherId,
        int? scopedWarehouseId,
        string actor,
        string? ipAddress,
        ReviewResultEnum reviewResult = ReviewResultEnum.Pass,
        decimal responsibilityScore = 0,
        string? reviewNote = null);
}

public class InboundExecutionService : IInboundExecutionService
{
    private const decimal QuantityTolerance = 0.0001m;
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryBalanceService _inventoryBalanceService;
    private readonly IInventoryTransactionService _inventoryTransactionService;
    private readonly ICatchWeightService _catchWeightService;

    private static DateTime VietnamNow => VietnamTime.Now;

    public InboundExecutionService(
        AppDbContext db,
        IUnitOfWork unitOfWork,
        IInventoryBalanceService inventoryBalanceService,
        IInventoryTransactionService? inventoryTransactionService = null,
        ICatchWeightService? catchWeightService = null)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _inventoryBalanceService = inventoryBalanceService;
        _inventoryTransactionService = inventoryTransactionService ?? new InventoryTransactionService(db);
        _catchWeightService = catchWeightService ?? new CatchWeightService(db);
    }

    public async Task<WorkflowResult> CompleteInboundAsync(
        long voucherId,
        int? scopedWarehouseId,
        string actor,
        string? ipAddress,
        ReviewResultEnum reviewResult = ReviewResultEnum.Pass,
        decimal responsibilityScore = 0,
        string? reviewNote = null)
    {
        var voucher = await _db.Vouchers
            .Include(v => v.Details).ThenInclude(d => d.Item)
            .FirstOrDefaultAsync(v => v.VoucherId == voucherId);

        if (voucher == null)
            return WorkflowResult.NotFoundResult("Không tìm thấy phiếu.");
        if (voucher.IsCancelled)
            return WorkflowResult.Failure("Phiếu đã hủy.", "Details");
        if (voucher.IsPosted)
            return WorkflowResult.Failure("Phiếu đã ghi sổ.", "Details");

        if (!voucher.IsInboundFlow)
            return WorkflowResult.Failure("Chỉ áp dụng cho phiếu nhập kho.", "Details");

        if (voucher.InboundStatus != InboundStatusEnum.Receiving)
            return WorkflowResult.Failure("Phiếu phải ở trạng thái Đang Nhận Hàng.", "Details");

        if (scopedWarehouseId.HasValue && voucher.WarehouseId != scopedWarehouseId.Value)
            return WorkflowResult.ForbiddenResult();

        // P0-05: Single transaction boundary
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            await _catchWeightService.RequireInboundCatchWeightAsync(voucher);

            using var ledgerScope = _inventoryTransactionService.BeginScope(new InventoryTransactionContext
            {
                TransactionType = voucher.VoucherType == VoucherTypeEnum.DieuChinh
                    ? InventoryTransactionTypeEnum.Adjust
                    : InventoryTransactionTypeEnum.Receive,
                TransactionGroupKey = $"voucher:{voucher.VoucherId}:inbound-complete",
                IdempotencyKeyPrefix = $"voucher:{voucher.VoucherId}:inbound-complete",
                WarehouseId = voucher.WarehouseId,
                OwnerPartnerId = voucher.OwnerPartnerId,
                VoucherId = voucher.VoucherId,
                ReferenceType = "Voucher",
                ReferenceId = voucher.VoucherId.ToString(),
                ReferenceCode = voucher.VoucherCode,
                Actor = actor
            });
            var affectedItemIds = new HashSet<int>();

            // Pre-load serial numbers for this voucher
            var serialTrackedDetailIds = voucher.Details
                .Where(d => d.Item != null && d.Item.TrackSerial)
                .Select(d => d.VoucherDetailId)
                .ToList();
            var serialRowsByDetail = serialTrackedDetailIds.Count == 0
                ? new List<SerialNumber>()
                : await _db.SerialNumbers
                    .Where(s => s.VoucherId == voucherId
                        && s.VoucherDetailId.HasValue
                        && serialTrackedDetailIds.Contains(s.VoucherDetailId.Value)
                        && s.Status == SerialNumberStatusEnum.Active)
                    .ToListAsync();

            // Pre-load active stock alerts for items in this voucher
            var approveItemIds = voucher.Details.Select(d => d.ItemId).Distinct().ToList();
            var approveActiveAlerts = await _db.StockAlerts
                .Where(a => approveItemIds.Contains(a.ItemId) && !a.IsResolved && a.AlertType == AlertTypeEnum.LowStock)
                .ToListAsync();
            var runningStockByItem = await _inventoryBalanceService.GetStockByItemAsync(null, approveItemIds);

            var detailIds = voucher.Details.Select(d => d.VoucherDetailId).ToList();
            var completedCrossDockQtyByDetail = detailIds.Count == 0
                ? new Dictionary<long, decimal>()
                : await _db.CrossDockTasks
                    .Where(t => t.InboundVoucherDetailId.HasValue
                        && detailIds.Contains(t.InboundVoucherDetailId.Value)
                        && t.Status == CrossDockTaskStatusEnum.Completed)
                    .GroupBy(t => t.InboundVoucherDetailId!.Value)
                    .Select(g => new { VoucherDetailId = g.Key, Qty = g.Sum(t => t.ActualQty ?? t.ScheduledQty) })
                    .ToDictionaryAsync(x => x.VoucherDetailId, x => x.Qty);

            await ValidateInboundReadinessAsync(voucher, completedCrossDockQtyByDetail);

            int createdLpnCount = 0;

            foreach (var detail in voucher.Details)
            {
                var item = detail.Item;
                if (item == null) throw WmsExceptions.ItemDisabled(detail.ItemId);

                // Calculate good qty (exclude defects)
                var goodBaseQty = detail.BaseQty;
                if (voucher.VoucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham)
                {
                    var defectBase = detail.DefectBaseQty > 0
                        ? detail.DefectBaseQty
                        : (detail.DefectQty * (detail.ConversionRate == 0 ? 1 : Math.Abs(detail.ConversionRate)));
                    defectBase = Math.Max(0, defectBase);
                    goodBaseQty = Math.Max(0, detail.BaseQty - defectBase);
                }
                var crossDockQty = completedCrossDockQtyByDetail.TryGetValue(detail.VoucherDetailId, out var matchedCrossDockQty)
                    ? matchedCrossDockQty
                    : 0m;
                var putawayBaseQty = Math.Max(0, goodBaseQty - crossDockQty);

                // Update ItemLocation
                if (detail.LocationId.HasValue && detail.LocationId > 0)
                {
                    var itemLocation = await _db.ItemLocations
                        .FirstOrDefaultAsync(il => il.ItemId == detail.ItemId
                            && il.OwnerPartnerId == voucher.OwnerPartnerId
                            && il.LocationId == detail.LocationId.Value
                            && il.LotNumber == detail.LotNumber
                            && il.ExpiryDate == detail.ExpiryDate);

                    if (itemLocation == null)
                    {
                        itemLocation = new ItemLocation
                        {
                            ItemId = detail.ItemId,
                            OwnerPartnerId = voucher.OwnerPartnerId,
                            LocationId = detail.LocationId.Value,
                            Quantity = 0,
                            LotNumber = detail.LotNumber,
                            ExpiryDate = detail.ExpiryDate,
                            UpdatedAt = VietnamNow
                        };
                        _db.ItemLocations.Add(itemLocation);
                    }

                    if (voucher.VoucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham)
                    {
                        itemLocation.Quantity += putawayBaseQty;
                    }
                    else if (voucher.VoucherType == VoucherTypeEnum.DieuChinh)
                    {
                        itemLocation.Quantity += detail.BaseQty;
                        if (itemLocation.Quantity < 0)
                            throw WmsExceptions.NegativeLocationStock(item.ItemCode, itemLocation.Quantity - detail.BaseQty);
                    }
                    itemLocation.UpdatedAt = VietnamNow;

                    // Create LPN for inbound
                    if (voucher.VoucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham
                        && putawayBaseQty > 0)
                    {
                        var lpnCode = await GenerateNextLpnCodeAsync();
                        var licensePlate = new LicensePlate
                        {
                            LpnCode = lpnCode,
                            VoucherId = voucher.VoucherId,
                            WarehouseId = voucher.WarehouseId,
                            OwnerPartnerId = voucher.OwnerPartnerId,
                            CurrentLocationId = detail.LocationId,
                            Status = LpnStatusEnum.Stored,
                            LpnType = LpnTypeEnum.Carton,
                            CreatedAt = VietnamNow,
                            UpdatedAt = VietnamNow,
                            IsActive = true,
                            Details =
                            {
                                new LicensePlateDetail
                                {
                                    ItemId = detail.ItemId,
                                    OwnerPartnerId = voucher.OwnerPartnerId,
                                    VoucherDetailId = detail.VoucherDetailId,
                                    Quantity = putawayBaseQty,
                                    LotNumber = detail.LotNumber,
                                    ExpiryDate = detail.ExpiryDate,
                                    ManufacturingDate = detail.ManufacturingDate,
                                    HoldStatus = InventoryHoldStatusEnum.Available,
                                    CreatedAt = VietnamNow,
                                    UpdatedAt = VietnamNow
                                }
                            }
                        };
                        _db.LicensePlates.Add(licensePlate);
                        createdLpnCount++;

                        // Assign serial numbers to LPN
                        if (item.TrackSerial)
                        {
                            var putawaySerialCount = (int)Math.Ceiling(putawayBaseQty);
                            foreach (var serial in serialRowsByDetail
                                .Where(s => s.VoucherDetailId == detail.VoucherDetailId)
                                .Take(putawaySerialCount))
                            {
                                serial.LicensePlate = licensePlate;
                                serial.OwnerPartnerId = voucher.OwnerPartnerId;
                                serial.LocationId = detail.LocationId;
                                serial.Status = SerialNumberStatusEnum.Active;
                                serial.HoldStatus = InventoryHoldStatusEnum.Available;
                                serial.UpdatedAt = VietnamNow;
                            }
                        }
                    }
                }

                // P0-03: Update Item.CurrentStock (WAC calculation)
                if (voucher.VoucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham)
                {
                    var purchasePrice = detail.UnitPrice;
                    runningStockByItem.TryGetValue(item.ItemId, out var oldStock);
                    var oldCost = item.UnitCost;
                    var newStock = oldStock + putawayBaseQty;
                    item.CurrentStock = newStock;
                    runningStockByItem[item.ItemId] = newStock;
                    if (newStock > 0)
                    {
                        item.UnitCost = (oldStock * oldCost + putawayBaseQty * purchasePrice) / newStock;
                    }
                    item.LastCost = purchasePrice;
                }
                else if (voucher.VoucherType == VoucherTypeEnum.DieuChinh)
                {
                    runningStockByItem.TryGetValue(item.ItemId, out var oldStock);
                    var newStock = oldStock + detail.BaseQty;
                    item.CurrentStock = newStock;
                    runningStockByItem[item.ItemId] = newStock;
                    if (newStock < 0)
                        throw WmsExceptions.NegativeItemStock(item.ItemCode, 0);
                }

                item.TotalStockValue = item.CurrentStock * item.UnitCost;
                item.UpdatedAt = VietnamNow;
                affectedItemIds.Add(item.ItemId);

                // Auto-resolve LowStock alerts
                if (item.MinThreshold > 0 && item.CurrentStock > item.MinThreshold)
                {
                    var alertsForItem = approveActiveAlerts.Where(a => a.ItemId == item.ItemId && !a.IsResolved).ToList();
                    foreach (var alert in alertsForItem)
                    {
                        alert.IsResolved = true;
                        alert.ResolvedAt = VietnamNow;
                    }
                }
            }

            voucher.IsPosted = true;
            voucher.ReviewResult = reviewResult;
            voucher.ReviewedBy = actor;
            voucher.ReviewedAt = VietnamNow;
            voucher.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? null : reviewNote.Trim();
            voucher.ResponsibilityScore = responsibilityScore;
            voucher.InboundStatus = InboundStatusEnum.Completed;
            voucher.CompletedBy = actor;
            voucher.CompletedAt = VietnamNow;
            voucher.UnloadStartAt ??= voucher.ReceivedAt ?? VietnamNow;
            voucher.UnloadEndAt ??= VietnamNow;
            voucher.DockCompletedAt ??= VietnamNow;
            voucher.DockStatus = DockOperationStatusEnum.Completed;
            voucher.UpdatedAt = VietnamNow;

            await _unitOfWork.SaveChangesAsync();

            // P0-03: Sync CurrentStock from ItemLocation source of truth
            await _inventoryBalanceService.SyncCurrentStockAsync(affectedItemIds);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            var message = reviewResult == ReviewResultEnum.PassWithAdjustment
                ? $"Đã kiểm và duyệt phiếu {voucher.VoucherCode}. Hệ thống ghi nhận sai lệch (điểm trách nhiệm: {responsibilityScore:N0})."
                : $"Đã duyệt và hoàn tất nhập kho phiếu {voucher.VoucherCode}.";

            return WorkflowResult.Success(message, "Details", new { id = voucherId });
        }
        catch (BusinessRuleException ex)
        {
            await _unitOfWork.RollbackAsync();
            return WorkflowResult.Failure(UserSafeError.From(ex), "Details", new { id = voucherId });
        }
        catch (DbUpdateConcurrencyException)
        {
            await _unitOfWork.RollbackAsync();
            return WorkflowResult.Failure("Dữ liệu tồn kho đã bị thay đổi bởi người khác. Vui lòng tải lại trang và thử lại.", "Details", new { id = voucherId });
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    private async Task ValidateInboundReadinessAsync(
        Voucher voucher,
        IReadOnlyDictionary<long, decimal> completedCrossDockQtyByDetail)
    {
        if (voucher.Details.Count == 0)
            throw new BusinessRuleException("Phiếu nhập chưa có dòng hàng để hoàn tất.", "INBOUND_LINES_REQUIRED", "VoucherDetail");

        var locationIds = voucher.Details
            .Where(d => d.LocationId.HasValue && d.LocationId.Value > 0)
            .Select(d => d.LocationId!.Value)
            .Distinct()
            .ToList();
        var locationWarehouseById = locationIds.Count == 0
            ? new Dictionary<int, int>()
            : await _db.Locations
                .AsNoTracking()
                .Include(l => l.Zone)
                .Where(l => locationIds.Contains(l.LocationId) && l.IsActive && l.Zone != null)
                .ToDictionaryAsync(l => l.LocationId, l => l.Zone!.WarehouseId);

        var serialTrackedDetailIds = voucher.Details
            .Where(d => d.Item?.TrackSerial == true)
            .Select(d => d.VoucherDetailId)
            .ToList();
        var serialCountsByDetail = serialTrackedDetailIds.Count == 0
            ? new Dictionary<long, int>()
            : await _db.SerialNumbers
                .AsNoTracking()
                .Where(s => s.VoucherId == voucher.VoucherId
                    && s.VoucherDetailId.HasValue
                    && serialTrackedDetailIds.Contains(s.VoucherDetailId.Value)
                    && s.Status == SerialNumberStatusEnum.Active
                    && s.VoidedAt == null)
                .GroupBy(s => s.VoucherDetailId!.Value)
                .Select(g => new { VoucherDetailId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.VoucherDetailId, x => x.Count);

        foreach (var detail in voucher.Details.OrderBy(d => d.LineNumber).ThenBy(d => d.VoucherDetailId))
        {
            var item = detail.Item ?? throw WmsExceptions.ItemNotFound(detail.ItemId);
            if (detail.OwnerPartnerId.HasValue && detail.OwnerPartnerId != voucher.OwnerPartnerId)
            {
                throw new BusinessRuleException(
                    $"Dòng [{item.ItemCode}] không cùng chủ hàng với phiếu nhập.",
                    "INBOUND_OWNER_MISMATCH",
                    "VoucherDetail");
            }
            detail.OwnerPartnerId ??= voucher.OwnerPartnerId;

            var goodBaseQty = CalculateInboundGoodBaseQty(voucher, detail);
            var crossDockQty = completedCrossDockQtyByDetail.TryGetValue(detail.VoucherDetailId, out var completedCrossDockQty)
                ? completedCrossDockQty
                : 0m;
            if (crossDockQty - goodBaseQty > QuantityTolerance)
            {
                throw new BusinessRuleException(
                    $"Dòng [{item.ItemCode}] có số lượng chuyển thẳng lớn hơn số lượng tốt thực nhận.",
                    "INBOUND_CROSSDOCK_QTY_INVALID",
                    "CrossDockTask");
            }

            if (goodBaseQty <= QuantityTolerance)
                continue;

            if (item.TrackLot && string.IsNullOrWhiteSpace(detail.LotNumber))
                throw WmsExceptions.LotRequired(item.ItemCode);
            if (item.TrackExpiry && !detail.ExpiryDate.HasValue)
                throw WmsExceptions.HsdRequired(item.ItemCode);
            if (detail.QualityStatus is QualityStatusEnum.Pending or QualityStatusEnum.Inspecting or QualityStatusEnum.Failed or QualityStatusEnum.Quarantine or QualityStatusEnum.OnHold)
            {
                throw new BusinessRuleException(
                    $"Dòng [{item.ItemCode}] chưa đạt điều kiện kiểm phẩm để hoàn tất nhập kho.",
                    "INBOUND_QC_NOT_READY",
                    "VoucherDetail");
            }

            if (item.TrackSerial)
            {
                if (goodBaseQty != decimal.Truncate(goodBaseQty))
                    throw WmsExceptions.SerialNotInteger(item.ItemCode);
                var requiredSerialCount = (int)Math.Ceiling(goodBaseQty);
                var actualSerialCount = serialCountsByDetail.TryGetValue(detail.VoucherDetailId, out var serialCount)
                    ? serialCount
                    : 0;
                if (actualSerialCount != requiredSerialCount)
                    throw WmsExceptions.SerialCountInsufficient(item.ItemCode, requiredSerialCount, actualSerialCount);
            }

            var putawayBaseQty = Math.Max(0, goodBaseQty - crossDockQty);
            if (putawayBaseQty <= QuantityTolerance)
                continue;

            if (!detail.LocationId.HasValue || detail.LocationId.Value <= 0)
            {
                throw new BusinessRuleException(
                    $"Dòng [{item.ItemCode}] còn {putawayBaseQty:N4} đơn vị cần cất hàng nhưng chưa chọn vị trí.",
                    "INBOUND_PUTAWAY_LOCATION_REQUIRED",
                    "VoucherDetail");
            }

            if (!locationWarehouseById.TryGetValue(detail.LocationId.Value, out var locationWarehouseId)
                || locationWarehouseId != voucher.WarehouseId)
            {
                throw new BusinessRuleException(
                    $"Vị trí cất hàng của [{item.ItemCode}] không hợp lệ hoặc không thuộc kho của phiếu.",
                    "INBOUND_PUTAWAY_LOCATION_INVALID",
                    "Location");
            }
        }
    }

    private static decimal CalculateInboundGoodBaseQty(Voucher voucher, VoucherDetail detail)
    {
        if (!voucher.IsInboundFlow)
            return Math.Max(0, detail.BaseQty);

        var defectBase = detail.DefectBaseQty > 0
            ? detail.DefectBaseQty
            : detail.DefectQty * (detail.ConversionRate == 0 ? 1 : Math.Abs(detail.ConversionRate));
        defectBase = Math.Max(0, defectBase);
        if (defectBase - detail.BaseQty > QuantityTolerance)
            throw WmsExceptions.DefectExceedsExpected(detail.Item?.ItemCode ?? detail.ItemId.ToString(), defectBase, detail.BaseQty);
        return Math.Max(0, detail.BaseQty - defectBase);
    }

    private async Task<string> GenerateNextLpnCodeAsync()
    {
        var dateStr = VietnamNow.ToString("yyyyMMdd");
        var prefix = $"LPN-{dateStr}-";

        var maxCode = await _db.LicensePlates
            .Where(l => l.LpnCode.StartsWith(prefix))
            .OrderByDescending(l => l.LpnCode)
            .Select(l => l.LpnCode)
            .FirstOrDefaultAsync();

        var nextSeq = 1;
        if (maxCode != null && maxCode.Length > prefix.Length
            && int.TryParse(maxCode[prefix.Length..], out var lastSeq))
        {
            nextSeq = lastSeq + 1;
        }

        return $"{prefix}{nextSeq:D6}";
    }
}
