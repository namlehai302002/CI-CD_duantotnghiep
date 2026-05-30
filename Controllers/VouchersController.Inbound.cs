using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Http;

using Microsoft.EntityFrameworkCore;

using WMS.Data;

using WMS.Models;

using WMS.ViewModels;

using WMS.Authorization;

using WMS.Common;

using WMS.Services;

using System.Text.Json;

using System.Linq;

using ClosedXML.Excel;

using System.Globalization;

using System.Data;

using Microsoft.Extensions.Logging.Abstractions;

namespace WMS.Controllers;

public partial class VouchersController
{

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.VoucherApproveInbound)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(long id, ReviewResultEnum reviewResult = ReviewResultEnum.Pass, decimal responsibilityScore = 0, string? reviewNote = null)
    {
        var scopedWh = GetScopedWarehouseId();
        try
        {
            var reviewer = User.Identity?.Name ?? "system";
            var voucher = await _db.Vouchers
                .Include(v => v.Details).ThenInclude(d => d.Item)
                .FirstOrDefaultAsync(v => v.VoucherId == id);
            if (voucher == null) return NotFound();
            if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value)
                return Forbid();
            if (voucher.IsCancelled)
            {
                TempData["Error"] = "Phiếu đã bị hủy, không thể duyệt.";
                return RedirectToAction("Details", new { id });
            }
            if (voucher.IsPosted)
            {
                TempData["Error"] = "Phiếu này đã được duyệt và ghi nhận tồn kho!";
                return RedirectToAction("Details", new { id });
            }
            if (voucher.IsInboundFlow && voucher.InboundStatus != InboundStatusEnum.Receiving)
            {
                TempData["Error"] = "Phiếu nhập chỉ được ghi sổ khi đã ở bước Đang Nhận Hàng.";
                return RedirectToAction("Details", new { id });
            }
            if (voucher.VoucherType is VoucherTypeEnum.XuatKho or VoucherTypeEnum.TraNCC or VoucherTypeEnum.ChuyenKho or VoucherTypeEnum.XuatSanXuat)
            {
                TempData["Error"] = "Phiếu xuất dùng luồng Xác Nhận Lấy Hàng + Ghi Sổ Xuất, không duyệt theo luồng nhập kho.";
                return RedirectToAction("Details", new { id });
            }
            // P1.1 — SoD enforcement: người tạo phiếu không được duyệt phiếu (4-mắt)
            EnforceSod(voucher.CreatedBy, WmsPermissions.VoucherApproveInbound);

            if (reviewResult is not (ReviewResultEnum.Pass or ReviewResultEnum.PassWithAdjustment))
                reviewResult = ReviewResultEnum.Pass;
            if (responsibilityScore < 0 || responsibilityScore > 100)
                throw WmsExceptions.ResponsibilityOutOfRange();
            if (reviewResult == ReviewResultEnum.PassWithAdjustment)
            {
                if (string.IsNullOrWhiteSpace(reviewNote))
                    throw WmsExceptions.AdjustmentWithVarianceRequiresNotes();
                if (responsibilityScore <= 0)
                    throw WmsExceptions.ResponsibilityRequiredWithVariance();
            }
            else
            {
                responsibilityScore = 0;
            }

            var lockDate = await GetActiveLockDateAsync(voucher.WarehouseId);
            var transactionDate = ResolveLockTransactionDate(voucher, VietnamNow);
            if (IsLocked(transactionDate, lockDate))
            {
                TempData["Error"] = $"Kho đã khóa kỳ đến {lockDate:dd/MM/yyyy}. Không thể duyệt phiếu ngày {voucher.VoucherDate:dd/MM/yyyy}.";
                return RedirectToAction("Details", new { id });
            }

            if (voucher.IsInboundFlow)
            {
                var result = await _inboundExecutionService.CompleteInboundAsync(
                    id,
                    scopedWh,
                    reviewer,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    reviewResult,
                    responsibilityScore,
                    reviewNote);

                if (result.NotFound) return NotFound();
                if (result.Forbidden) return Forbid();
                if (result.Succeeded)
                {
                    TempData["Success"] = result.Message ?? $"Đã hoàn tất nhập kho phiếu {voucher.VoucherCode}.";
                    if (!string.IsNullOrWhiteSpace(result.Warning))
                        TempData["Warning"] = result.Warning;
                }
                else
                {
                    TempData["Error"] = result.Message ?? "Không thể hoàn tất nhập kho.";
                }

                return RedirectToAction(result.RedirectAction ?? "Details", result.RedirectRouteValues ?? new { id });
            }

            var locationsUsedInThisVoucher = new Dictionary<int, int>();
            var locationsAddedWeightThisVoucher = new Dictionary<int, decimal>();

            // ═══ Bulk-load dữ liệu để tránh N+1 query trong vòng lặp Approve ═══
            var approveLocationIds = voucher.Details.Where(d => d.LocationId.HasValue).Select(d => d.LocationId!.Value).Distinct().ToList();
            var approveLocationStockTotals = approveLocationIds.Count > 0
                ? await _db.ItemLocations.AsNoTracking().Where(il => approveLocationIds.Contains(il.LocationId))
                    .GroupBy(il => il.LocationId)
                    .Select(g => new { LocationId = g.Key, Total = g.Sum(il => il.Quantity) })
                    .ToDictionaryAsync(x => x.LocationId, x => x.Total)
                : new Dictionary<int, decimal>();
            // Pre-load item name lookup cho conflict check (Items đã được Include qua voucher.Details.Item)
            var approveItemIds = voucher.Details.Select(d => d.ItemId).Distinct().ToList();
            var approveItemNamesDict = voucher.Details
                .Where(d => d.Item != null)
                .Select(d => d.Item!)
                .DistinctBy(i => i.ItemId)
                .ToDictionary(i => i.ItemId, i => i.ItemCode);
            // Pre-load active StockAlerts cho tất cả item liên quan
            var approveActiveAlerts = await _db.StockAlerts
                .Where(a => approveItemIds.Contains(a.ItemId) && !a.IsResolved && a.AlertType == AlertTypeEnum.LowStock)
                .ToListAsync();
            var runningStockByItem = await _inventoryBalanceService.GetStockByItemAsync(null, approveItemIds);

            // ═══ H-01 FIX: Pre-load ALL ItemLocations to eliminate N+1 queries ═══
            var approveItemLocations = approveLocationIds.Count > 0
                ? await _db.ItemLocations
                    .Where(il => approveLocationIds.Contains(il.LocationId))
                    .Include(il => il.Item)
                    .ToListAsync()
                : new List<ItemLocation>();
            // Build lookup dictionary: (ItemId, LocationId, LotNumber, ExpiryDate, Owner) -> ItemLocation
            var ilLookup = approveItemLocations
                .ToDictionary(il => (il.ItemId, il.LocationId, il.LotNumber ?? "", il.ExpiryDate, il.OwnerPartnerId), il => il);
            // Conflict check: locations that already have OTHER items with quantity > 0
            var locationConflicts = approveItemLocations
                .Where(il => il.Quantity > 0)
                .GroupBy(il => il.LocationId)
                .ToDictionary(g => g.Key, g => g.ToList());
            var createdLpnCount = 0;
            var serialTrackedDetailIds = voucher.Details
                .Where(d => d.Item?.TrackSerial == true)
                .Select(d => d.VoucherDetailId)
                .ToList();
            var serialCountsByDetail = serialTrackedDetailIds.Count > 0
                ? await _db.SerialNumbers
                    .Where(s => s.Status == SerialNumberStatusEnum.Active
                        && s.VoucherId == voucher.VoucherId
                        && s.VoucherDetailId.HasValue
                        && serialTrackedDetailIds.Contains(s.VoucherDetailId.Value))
                    .GroupBy(s => s.VoucherDetailId!.Value)
                    .Select(g => new { VoucherDetailId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.VoucherDetailId, x => x.Count)
                : new Dictionary<long, int>();
            var serialRowsByDetail = serialTrackedDetailIds.Count > 0
                ? await _db.SerialNumbers
                    .Where(s => s.Status == SerialNumberStatusEnum.Active
                        && s.VoucherId == voucher.VoucherId
                        && s.VoucherDetailId.HasValue
                        && serialTrackedDetailIds.Contains(s.VoucherDetailId.Value))
                    .ToListAsync()
                : new List<SerialNumber>();

            await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);

            foreach (var detail in voucher.Details)
            {
                var item = detail.Item;
                if (item == null) continue;

                // P0-6: ConversionRate âm là dữ liệu bẩn — bỏ Math.Abs để dấu lỗi bộc lộ thay vì nuốt sai.
                if (detail.ConversionRate < 0)
                    throw WmsExceptions.InvalidConversionRate(item.ItemCode);
                var conversionRate = detail.ConversionRate == 0 ? 1m : detail.ConversionRate;
                var defectBase = detail.DefectBaseQty > 0 ? detail.DefectBaseQty : detail.DefectQty * conversionRate;
                defectBase = Math.Max(0, defectBase);
                if (defectBase > detail.BaseQty)
                    throw WmsExceptions.DefectExceedsExpected(item.ItemCode, defectBase, detail.BaseQty);

                var goodBaseQty = detail.BaseQty - defectBase;
                if (goodBaseQty < 0) goodBaseQty = 0;

                if (item.TrackSerial)
                {
                    if (goodBaseQty != decimal.Truncate(goodBaseQty))
                        throw WmsExceptions.SerialNotInteger(item.ItemCode);

                    var requiredSerialCount = (int)goodBaseQty;
                    var actualSerialCount = serialCountsByDetail.TryGetValue(detail.VoucherDetailId, out var serialCount)
                        ? serialCount
                        : 0;
                    if (actualSerialCount != requiredSerialCount)
                        throw WmsExceptions.SerialCountInsufficient(item.ItemCode, requiredSerialCount, actualSerialCount);
                }

                var isChemical = item.ItemType == ItemTypeEnum.HoaChat;
                var unitWeight = item.Weight ?? 1m;
                var weightAdded = isChemical ? goodBaseQty : goodBaseQty * unitWeight;
                var maxCapacity = isChemical ? SecurityHelpers.WarehouseCapacity.MaxChemicalLiters : SecurityHelpers.WarehouseCapacity.MaxStorageKg;
                var unitName = isChemical ? SecurityHelpers.WarehouseCapacity.VolumeUnit : SecurityHelpers.WarehouseCapacity.WeightUnit;

                if (detail.LocationId.HasValue && (voucher.VoucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham || (voucher.VoucherType == VoucherTypeEnum.DieuChinh && detail.BaseQty > 0)))
                {
                    var currentStock = approveLocationStockTotals.TryGetValue(detail.LocationId.Value, out var csApp) ? csApp : 0m;
                    var localAdded = locationsAddedWeightThisVoucher.ContainsKey(detail.LocationId.Value) ? locationsAddedWeightThisVoucher[detail.LocationId.Value] : 0;

                    var addedForCapacity = voucher.VoucherType == VoucherTypeEnum.DieuChinh ? detail.BaseQty : goodBaseQty;
                    var weightForCapacity = isChemical ? addedForCapacity : addedForCapacity * unitWeight;
                    var totalExpectedWeight = (isChemical ? currentStock : currentStock * unitWeight) + localAdded + weightForCapacity;
                    if (totalExpectedWeight > maxCapacity) throw WmsExceptions.CapacityExceeded(detail.LocationId.Value.ToString(), maxCapacity, unitName, item.ItemCode, totalExpectedWeight);

                    locationsAddedWeightThisVoucher[detail.LocationId.Value] = localAdded + weightForCapacity;
                }

                if (detail.LocationId.HasValue)
                {
                    // Ràng buộc quy tắc: 1 ô chỉ chứa 1 vật tư
                    if (voucher.VoucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham)
                    {
                        if (locationsUsedInThisVoucher.TryGetValue(detail.LocationId.Value, out int usedItemId) && usedItemId != detail.ItemId)
                        {
                            var conflictNameLocal = approveItemNamesDict.TryGetValue(usedItemId, out var cName) ? cName : usedItemId.ToString();
                            throw WmsExceptions.OneLocationOneItemConflictLocal(item.ItemCode, conflictNameLocal);
                        }
                        locationsUsedInThisVoucher[detail.LocationId.Value] = detail.ItemId;

                        // H-01 FIX: Use pre-loaded data instead of per-iteration query
                        if (locationConflicts.TryGetValue(detail.LocationId.Value, out var locsInSlot))
                        {
                            var otherItemsInLocation = locsInSlot.FirstOrDefault(il => il.ItemId != detail.ItemId && il.Quantity > 0);
                            if (otherItemsInLocation != null)
                            {
                                var conflictName = otherItemsInLocation.Item != null ? otherItemsInLocation.Item.ItemCode : otherItemsInLocation.ItemId.ToString();
                                throw WmsExceptions.OneLocationOneItemConflict(item.ItemCode, conflictName, detail.LocationId.Value.ToString());
                            }
                        }
                    }

                    // H-01 FIX: Use pre-loaded dictionary lookup instead of per-iteration query
                    var ilKey = (detail.ItemId, detail.LocationId.Value, detail.LotNumber ?? "", detail.ExpiryDate, voucher.OwnerPartnerId);
                    ilLookup.TryGetValue(ilKey, out var itemLocation);

                    if (itemLocation == null)
                    {
                        itemLocation = new ItemLocation
                        {
                            ItemId = detail.ItemId,
                            OwnerPartnerId = voucher.OwnerPartnerId,
                            LocationId = detail.LocationId.Value,
                            Quantity = 0,
                            ExpiryDate = detail.ExpiryDate,
                            LotNumber = detail.LotNumber,
                            UpdatedAt = VietnamNow
                        };
                        _db.ItemLocations.Add(itemLocation);
                        ilLookup[ilKey] = itemLocation; // Add to lookup for subsequent iterations
                    }

                    if (voucher.VoucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham)
                    {
                        itemLocation.Quantity += goodBaseQty;
                    }
                    else if (voucher.VoucherType == VoucherTypeEnum.DieuChinh)
                    {
                        itemLocation.Quantity += detail.BaseQty;
                        if (itemLocation.Quantity < 0)
                            throw WmsExceptions.NegativeLocationStock(item.ItemCode, itemLocation.Quantity - detail.BaseQty);
                    }

                    itemLocation.UpdatedAt = VietnamNow;

                    if (voucher.VoucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham
                        && goodBaseQty > 0)
                    {
                        var licensePlate = new LicensePlate
                        {
                            LpnCode = await GenerateNextLpnCodeAsync(),
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
                                    Quantity = goodBaseQty,
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

                        if (item.TrackSerial)
                        {
                            foreach (var serial in serialRowsByDetail.Where(s => s.VoucherDetailId == detail.VoucherDetailId))
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

                if (voucher.VoucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham)
                {
                    // ═══ WEIGHTED AVERAGE COST (Bình quân gia quyền — VAS-02) ═══
                    var purchasePrice = detail.UnitPrice;
                    runningStockByItem.TryGetValue(item.ItemId, out var oldStock);
                    var oldCost = item.UnitCost;
                    var newStock = oldStock + goodBaseQty;
                    item.CurrentStock = newStock;
                    runningStockByItem[item.ItemId] = newStock;
                    if (newStock > 0)
                    {
                        item.UnitCost = (oldStock * oldCost + goodBaseQty * purchasePrice) / newStock;
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

                // Auto-resolve StockAlert nếu tồn kho phục hồi trên ngưỡng sau duyệt (sử dụng bộ nhớ đã bulk-load)
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
            voucher.ReviewedBy = reviewer;
            voucher.ReviewedAt = VietnamNow;
            voucher.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? null : reviewNote.Trim();
            voucher.ResponsibilityScore = responsibilityScore;
            if (voucher.IsInboundFlow)
            {
                voucher.InboundStatus = InboundStatusEnum.Completed;
                voucher.CompletedBy = reviewer;
                voucher.CompletedAt = VietnamNow;
                voucher.UnloadStartAt ??= voucher.ReceivedAt ?? VietnamNow;
                voucher.UnloadEndAt ??= VietnamNow;
                voucher.DockCompletedAt ??= VietnamNow;
                voucher.DockStatus = DockOperationStatusEnum.Completed;
            }
            voucher.UpdatedAt = VietnamNow;

            await _unitOfWork.SaveChangesAsync();

            // P0-03: Sync CurrentStock from ItemLocation source of truth
            var approveAffectedItemIds = voucher.Details.Select(d => d.ItemId).Distinct();
            await _inventoryBalanceService.SyncCurrentStockAsync(approveAffectedItemIds);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            TempData["Success"] = reviewResult == ReviewResultEnum.PassWithAdjustment
                ? $"Đã kiểm và duyệt phiếu {voucher.VoucherCode}. Hệ thống ghi nhận sai lệch (điểm trách nhiệm: {responsibilityScore:N0})."
                : $"Đã kiểm và duyệt phiếu {voucher.VoucherCode}, tồn kho đã cập nhật thành công.";
            if (createdLpnCount > 0)
            {
                TempData["Success"] += $" Đã tạo {createdLpnCount} mã kiện (LPN) cho hàng đã nhập.";
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            await _unitOfWork.RollbackAsync();
            TempData["Error"] = "Dữ liệu tồn kho đã bị thay đổi bởi người khác. Vui lòng tải lại trang và thử lại.";
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            TempData["Error"] = UserSafeError.WithPrefix(ex, "Lỗi duyệt phiếu", "Không thể duyệt phiếu lúc này. Vui lòng thử lại.");
        }

        return RedirectToAction("Details", new { id });
    }


    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.VoucherApproveInbound)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmActualReceivingQty(long id, long detailId, decimal actualQty, string? reviewNote)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var scopedWh = GetScopedWarehouseId();
            var voucher = await _db.Vouchers
                .Include(v => v.Details)
                .FirstOrDefaultAsync(v => v.VoucherId == id);
            if (voucher == null)
            {
                await _unitOfWork.RollbackAsync();
                return NotFound();
            }
            if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value)
            {
                await _unitOfWork.RollbackAsync();
                return Forbid();
            }
            if (voucher.IsCancelled || voucher.IsPosted)
                throw WmsExceptions.VoucherAlreadyApproved();
            if (voucher.InboundStatus != InboundStatusEnum.Receiving)
                throw WmsExceptions.ReceivingOnlyForInbound();

            var detail = voucher.Details.FirstOrDefault(d => d.VoucherDetailId == detailId);
            if (detail == null) throw WmsExceptions.DetailNotFound();

            // P0-6: từ chối ConversionRate âm, không nuốt dấu bằng Math.Abs.
            if (detail.ConversionRate < 0)
                throw WmsExceptions.InvalidConversionRate(detail.Item?.ItemCode ?? "Unknown");
            var rate = detail.ConversionRate == 0 ? 1 : detail.ConversionRate;
            var expectedQty = rate > 0 ? (detail.BaseQty / rate) : detail.BaseQty;

            if (actualQty < 0 || actualQty > expectedQty)
                throw WmsExceptions.QtyOutOfRange(detail.Item?.ItemCode ?? "Unknown", 0, expectedQty);

            var defectQty = expectedQty - actualQty;
            detail.DefectQty = defectQty;
            // R3-7: round về scale của column decimal(18,4) để tránh truncate trên DB gây sai tổng.
            detail.DefectBaseQty = Math.Round(defectQty * rate, 4, MidpointRounding.AwayFromZero);

            if (!string.IsNullOrWhiteSpace(reviewNote))
                detail.Notes = $"[ACTUAL:{actualQty:N2} | {VietnamNow:yyyy-MM-dd HH:mm}] {reviewNote.Trim()}";

            voucher.ReviewResult = defectQty > 0 ? ReviewResultEnum.PassWithAdjustment : ReviewResultEnum.Pass;
            voucher.UpdatedAt = VietnamNow;

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
            TempData["Success"] = $"Đã ghi nhận số lượng thực tế: {actualQty:N4} (Lệch: {defectQty:N4}).";
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            TempData["Error"] = UserSafeError.From(ex, "Không thể ghi nhận số lượng thực tế lúc này. Vui lòng thử lại.");
        }

        return RedirectToAction("Details", new { id });
    }


    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.VoucherApproveInbound)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateInboundDefect(long id, long detailId, decimal defectQty, string? reviewNote)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var scopedWh = GetScopedWarehouseId();
            var voucher = await _db.Vouchers
                .Include(v => v.Details)
                .FirstOrDefaultAsync(v => v.VoucherId == id);
            if (voucher == null)
            {
                await _unitOfWork.RollbackAsync();
                return NotFound();
            }
            if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value)
            {
                await _unitOfWork.RollbackAsync();
                return Forbid();
            }
            if (voucher.IsCancelled || voucher.IsPosted)
                throw WmsExceptions.VoucherAlreadyApproved();
            if (voucher.VoucherType is not (VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham))
                throw WmsExceptions.PickingOnlyForInbound();
            var reviewer = User.Identity?.Name ?? "system";
            if (string.Equals(voucher.CreatedBy, reviewer, StringComparison.OrdinalIgnoreCase))
                throw WmsExceptions.InspectorCannotBeCreator();

            var detail = voucher.Details.FirstOrDefault(d => d.VoucherDetailId == detailId);
            if (detail == null) throw WmsExceptions.DetailNotFound();

            // P0-6: từ chối ConversionRate âm, không nuốt dấu bằng Math.Abs.
            if (detail.ConversionRate < 0)
                throw WmsExceptions.InvalidConversionRate(detail.Item?.ItemCode ?? "Unknown");
            var rate = detail.ConversionRate == 0 ? 1 : detail.ConversionRate;
            var maxDefectQty = rate > 0 ? (detail.BaseQty / rate) : detail.BaseQty;
            if (defectQty < 0 || defectQty > maxDefectQty)
                throw WmsExceptions.MaxDefectExceeded(detail.Item?.ItemCode ?? "Unknown", maxDefectQty);

            detail.DefectQty = defectQty;
            // R3-7: round về scale decimal(18,4) tránh truncate DB sai tổng.
            detail.DefectBaseQty = Math.Round(defectQty * rate, 4, MidpointRounding.AwayFromZero);
            if (!string.IsNullOrWhiteSpace(reviewNote))
                detail.Notes = $"[KIEM:{VietnamNow:yyyy-MM-dd HH:mm}] {reviewNote.Trim()}";

            voucher.ReviewResult = ReviewResultEnum.PassWithAdjustment;
            voucher.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? voucher.ReviewNote : reviewNote.Trim();
            voucher.UpdatedAt = VietnamNow;

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
            TempData["Success"] = "Đã cập nhật sai lệch kiểm hàng cho dòng vật tư.";
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            TempData["Error"] = UserSafeError.WithPrefix(ex, "Không thể cập nhật sai lệch", "Không thể cập nhật sai lệch lúc này. Vui lòng thử lại.");
        }

        return RedirectToAction("Details", new { id });
    }


    // ═══════════════════════════════════════════════════════════════
    // ENTERPRISE INBOUND 5-STEP WORKFLOW ACTIONS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Draft → PendingApproval</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitForApproval(long id)
    {
        var voucher = await _db.Vouchers.FirstOrDefaultAsync(v => v.VoucherId == id);
        if (voucher == null) return NotFound();
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value) return Forbid();
        if (!voucher.IsInboundFlow)
        {
            TempData["Error"] = "Chỉ áp dụng cho phiếu nhập kho.";
            return RedirectToAction("Details", new { id });
        }
        if (voucher.InboundStatus != InboundStatusEnum.Draft && voucher.InboundStatus != InboundStatusEnum.Rejected)
        {
            TempData["Error"] = "Chỉ có thể gửi duyệt phiếu ở trạng thái Nháp hoặc Từ Chối.";
            return RedirectToAction("Details", new { id });
        }

        try
        {
            await ValidateInboundPlanningAsync(
                voucher.VoucherType,
                voucher.WarehouseId,
                true,
                voucher.ExpectedArrivalAt,
                voucher.DockAppointmentStart,
                voucher.DockAppointmentEnd,
                voucher.DockDoor,
                voucher.VoucherId);

            if (string.IsNullOrWhiteSpace(voucher.AsnCode))
                voucher.AsnCode = await GenerateNextAsnCodeAsync();

            if (string.IsNullOrWhiteSpace(voucher.DockDoor)
                && voucher.DockAppointmentStart.HasValue
                && voucher.DockAppointmentEnd.HasValue)
            {
                voucher.DockDoor = await SuggestAvailableDockDoorAsync(
                    voucher.WarehouseId,
                    voucher.DockAppointmentStart,
                    voucher.DockAppointmentEnd,
                    voucher.VoucherId);
            }

            voucher.InboundStatus = InboundStatusEnum.PendingApproval;
            voucher.SubmittedBy = User.Identity?.Name ?? "system";
            voucher.SubmittedAt = VietnamNow;
            voucher.RejectionReason = null;
            voucher.UpdatedAt = VietnamNow;

            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = $"Phiếu {voucher.VoucherCode} đã gửi duyệt. Nhân viên xem lại ở Tra cứu phiếu; quản lý duyệt ở Nhập kho > Duyệt phiếu nhập.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = UserSafeError.From(ex, "Không thể gửi duyệt phiếu lúc này. Vui lòng thử lại.");
        }

        return RedirectToAction("Details", new { id });
    }


    /// <summary>PendingApproval → Approved (4-eyes: approver ≠ creator)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveInbound(long id)
    {
        var voucher = await _db.Vouchers.FirstOrDefaultAsync(v => v.VoucherId == id);
        if (voucher == null) return NotFound();
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value) return Forbid();
        if (!voucher.IsInboundFlow || voucher.InboundStatus != InboundStatusEnum.PendingApproval)
        {
            TempData["Error"] = "Phiếu phải ở trạng thái Chờ Duyệt.";
            return RedirectToAction("Details", new { id });
        }

        var approver = User.Identity?.Name ?? "system";
        if (string.Equals(voucher.CreatedBy, approver, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Nguyên tắc 4 mắt: Người duyệt không được trùng người tạo phiếu.";
            return RedirectToAction("Details", new { id });
        }

        try
        {
            await ValidateInboundPlanningAsync(
                voucher.VoucherType,
                voucher.WarehouseId,
                true,
                voucher.ExpectedArrivalAt,
                voucher.DockAppointmentStart,
                voucher.DockAppointmentEnd,
                voucher.DockDoor,
                voucher.VoucherId);

            voucher.InboundStatus = InboundStatusEnum.Approved;
            voucher.ApprovedBy = approver;
            voucher.ApprovedAt = VietnamNow;
            voucher.UpdatedAt = VietnamNow;

            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = $"Phiếu {voucher.VoucherCode} đã được duyệt. Tiến hành nhận hàng.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = UserSafeError.From(ex, "Không thể duyệt phiếu nhập lúc này. Vui lòng thử lại.");
        }

        return RedirectToAction("Details", new { id });
    }


    /// <summary>PendingApproval → Rejected</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectInbound(long id, string? rejectionReason)
    {
        var voucher = await _db.Vouchers.FirstOrDefaultAsync(v => v.VoucherId == id);
        if (voucher == null) return NotFound();
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value) return Forbid();
        if (!voucher.IsInboundFlow || voucher.InboundStatus != InboundStatusEnum.PendingApproval)
        {
            TempData["Error"] = "Phiếu phải ở trạng thái Chờ Duyệt.";
            return RedirectToAction("Details", new { id });
        }

        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            TempData["Error"] = "Vui lòng nhập lý do từ chối.";
            return RedirectToAction("Details", new { id });
        }

        voucher.InboundStatus = InboundStatusEnum.Rejected;
        voucher.RejectionReason = rejectionReason.Trim();
        voucher.UpdatedAt = VietnamNow;

        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = $"Phiếu {voucher.VoucherCode} đã bị từ chối.";
        return RedirectToAction("Details", new { id });
    }


    /// <summary>Approved → Receiving</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmReceiving(long id)
    {
        var queued = QueuedOperationResponse.IsQueued(this);
        var redirectUrl = $"/Vouchers/Details/{id}";
        var voucher = await _db.Vouchers.FirstOrDefaultAsync(v => v.VoucherId == id);
        if (voucher == null)
            return queued
                ? QueuedOperationResponse.Json(this, false, "Không tìm thấy phiếu nhập.", null, StatusCodes.Status404NotFound, "NOT_FOUND")
                : NotFound();
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value)
            return queued
                ? QueuedOperationResponse.Json(this, false, "Bạn không có quyền thao tác kho của phiếu này.", null, StatusCodes.Status403Forbidden, "FORBIDDEN")
                : Forbid();
        if (!voucher.IsInboundFlow || voucher.InboundStatus != InboundStatusEnum.Approved)
        {
            if (queued && voucher.IsInboundFlow && voucher.InboundStatus is InboundStatusEnum.Receiving or InboundStatusEnum.Completed)
                return QueuedOperationResponse.Json(this, true, $"Phiếu {voucher.VoucherCode} đã được chuyển sang bước nhận hàng.", redirectUrl);

            if (queued)
                return QueuedOperationResponse.Json(this, false, "Phiếu phải ở trạng thái Đã Duyệt.", redirectUrl, StatusCodes.Status409Conflict, "INVALID_STATUS");

            TempData["Error"] = "Phiếu phải ở trạng thái Đã Duyệt.";
            return RedirectToAction("Details", new { id });
        }

        if (string.IsNullOrWhiteSpace(voucher.AsnCode) || !voucher.ExpectedArrivalAt.HasValue)
        {
            if (queued)
                return QueuedOperationResponse.Json(this, false, "Phiếu nhập cần đủ thông tin lịch xe đến trước khi chuyển sang bước nhận hàng.", redirectUrl, StatusCodes.Status422UnprocessableEntity, "INBOUND_APPOINTMENT_REQUIRED");

            TempData["Error"] = "Phiếu nhập cần đủ thông tin lịch xe đến trước khi chuyển sang bước nhận hàng.";
            return RedirectToAction("Details", new { id });
        }

        voucher.InboundStatus = InboundStatusEnum.Receiving;
        voucher.ReceivedBy = User.Identity?.Name ?? "system";
        voucher.ReceivedAt = VietnamNow;
        voucher.UpdatedAt = VietnamNow;

        await _unitOfWork.SaveChangesAsync();
        if (queued)
            return QueuedOperationResponse.Json(this, true, $"Phiếu {voucher.VoucherCode} đang nhận hàng. Kiểm tra và hoàn tất.", redirectUrl);

        TempData["Success"] = $"Phiếu {voucher.VoucherCode} đang nhận hàng. Kiểm tra và hoàn tất.";
        return RedirectToAction("Details", new { id });
    }


    /// <summary>Receiving → Completed (updates stock via existing Approve logic)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.VoucherApproveInbound)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteInbound(long id)
    {
        var voucher = await _db.Vouchers.FirstOrDefaultAsync(v => v.VoucherId == id);
        if (voucher == null) return NotFound();
        if (!voucher.IsInboundFlow || voucher.InboundStatus != InboundStatusEnum.Receiving)
        {
            TempData["Error"] = "Phiếu phải ở trạng thái Đang Nhận Hàng.";
            return RedirectToAction("Details", new { id });
        }

        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value) return Forbid();

        var resolvedReviewResult = voucher.ReviewResult == ReviewResultEnum.Pending
            ? ReviewResultEnum.Pass
            : voucher.ReviewResult;

        return await Approve(id, resolvedReviewResult, voucher.ResponsibilityScore, voucher.ReviewNote);
    }


    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ReplenishDefect(long id)
    {
        var original = await _db.Vouchers
            .Include(v => v.Details)
            .FirstOrDefaultAsync(v => v.VoucherId == id);

        if (original == null) return NotFound();
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && original.WarehouseId != scopedWh.Value)
            return Forbid();
        if (!original.IsPartial)
        {
            TempData["Error"] = "Phiếu này không có hàng lỗi để bù!";
            return RedirectToAction("Details", new { id });
        }

        var newVoucher = new VoucherCreateViewModel
        {
            VoucherType = VoucherTypeEnum.NhapKho, // Nhập kho
            WarehouseId = original.WarehouseId,
            PartnerId = original.PartnerId,
            ReferenceNo = $"BUHANG-{original.VoucherCode}",
            Description = $"Bù hàng lỗi cho phiếu {original.VoucherCode}",
            ParentVoucherId = original.VoucherId,
        };

        foreach (var d in original.Details.Where(x => x.DefectQty > 0))
        {
            newVoucher.Lines.Add(new VoucherDetailLine
            {
                ItemId = d.ItemId,
                LocationId = d.LocationId,
                TransactionQty = d.DefectQty, // Lấy số lượng lỗi làm số lượng chính để nhập bù
                DefectQty = 0,
                UnitPrice = d.UnitPrice,
                LineAmount = d.UnitPrice * d.DefectQty, // Base quantity for replenishment is DefectQty
                TransactionUomId = d.TransactionUomId
            });
        }

        TempData["Info"] = $"Đang tạo phiếu bù hàng từ {original.VoucherCode}. Vui lòng kiểm tra và lưu lại.";

        // Pass the prefilled model to standard Create view
        newVoucher.Warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync();
        newVoucher.Partners = await _db.Partners.Where(p => p.IsActive).ToListAsync();
        newVoucher.Items = await _db.Items.Include(i => i.BaseUom).Where(i => i.IsActive).OrderBy(i => i.ItemCode).ToListAsync();
        newVoucher.Uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).ToListAsync();
        newVoucher.Locations = await _db.Locations.Where(l => l.IsActive).ToListAsync();

        ViewBag.CanSeeFinancial = CanSeeFinancial();
        await PopulateVoucherCreateMetadataAsync(newVoucher);
        return View("Create", newVoucher);
    }

}
