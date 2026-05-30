using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;

using Microsoft.EntityFrameworkCore;

using WMS.Data;

using WMS.ViewModels;

using ClosedXML.Excel;

using System.IO;

using WMS.Models;

using System.Data;

using WMS.Authorization;

using WMS.Common;

using WMS.Services;

using Microsoft.Extensions.Logging.Abstractions;

namespace WMS.Controllers;

public partial class ReportsController
{

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> StockCount(int? warehouseId, DateTime? countDate)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;
        countDate ??= VietnamNow.Date;

        var vm = new StockCountPageViewModel
        {
            WarehouseId = warehouseId,
            CountDate = countDate.Value,
            Warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.WarehouseCode).ToListAsync()
        };
        if (scopedWh.HasValue)
            vm.Warehouses = vm.Warehouses.Where(w => w.WarehouseId == scopedWh.Value).ToList();

        if (!warehouseId.HasValue)
            return View(vm);

        vm.ExistingSheets = await _db.StockCountSheets.AsNoTracking()
            .Include(s => s.GeneratedAdjustmentVoucher)
            .Where(s => s.WarehouseId == warehouseId.Value && s.CountDate == countDate.Value.Date)
            .OrderByDescending(s => s.StockCountSheetId)
            .Take(50)
            .Select(s => new StockCountSheetSummary
            {
                StockCountSheetId = s.StockCountSheetId,
                CountDate = s.CountDate,
                CreatedBy = s.CreatedBy,
                CreatedAt = s.CreatedAt,
                Status = s.Status,
                ApprovedBy = s.ApprovedBy,
                ApprovedAt = s.ApprovedAt,
                ApprovalReason = s.ApprovalReason,
                UnlockedBy = s.UnlockedBy,
                UnlockedAt = s.UnlockedAt,
                UnlockReason = s.UnlockReason,
                VoucherCode = s.GeneratedAdjustmentVoucher != null ? s.GeneratedAdjustmentVoucher.VoucherCode : null
            })
            .ToListAsync();
        var sheetIds = vm.ExistingSheets.Select(s => s.StockCountSheetId).ToList();
        var lineStats = sheetIds.Count == 0
            ? new Dictionary<long, (int Total, int Diff)>()
            : await _db.StockCountLines.AsNoTracking()
                .Where(l => sheetIds.Contains(l.StockCountSheetId))
                .GroupBy(l => l.StockCountSheetId)
                .Select(g => new { SheetId = g.Key, Total = g.Count(), Diff = g.Count(x => x.Variance != null && x.Variance != 0) })
                .ToDictionaryAsync(x => x.SheetId, x => (x.Total, x.Diff));
        foreach (var sheet in vm.ExistingSheets)
        {
            if (lineStats.TryGetValue(sheet.StockCountSheetId, out var stats))
            {
                sheet.TotalLines = stats.Total;
                sheet.DiffLines = stats.Diff;
            }
        }

        vm.Lines = await _db.ItemLocations.AsNoTracking()
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Include(il => il.Item)
            .Where(il => il.Quantity != 0
                && il.Location != null
                && il.Location.Zone != null
                && il.Location.Zone.WarehouseId == warehouseId.Value)
            .OrderBy(il => il.ItemId).ThenBy(il => il.LocationId)
            .Select(il => new StockCountLineInput
            {
                ItemId = il.ItemId,
                ItemCode = il.Item != null ? il.Item.ItemCode : "",
                ItemName = il.Item != null ? il.Item.ItemName : "",
                LocationId = il.LocationId,
                LocationCode = il.Location != null ? il.Location.LocationCode : "",
                LotNumber = il.LotNumber,
                ExpiryDate = il.ExpiryDate,
                SystemQty = il.Quantity,
                CountedQty = il.Quantity
            })
            .ToListAsync();

        return View(vm);
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StockCountSaveDraft(StockCountPageViewModel vm)
    {
        if (!vm.WarehouseId.HasValue || vm.WarehouseId.Value <= 0)
        {
            TempData["Error"] = "Vui lòng chọn kho kiểm kê.";
            return RedirectToAction(nameof(StockCount));
        }

        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && vm.WarehouseId.Value != scopedWh.Value)
            return Forbid();

        var lockDate = await _db.WarehousePeriodLocks.AsNoTracking()
            .Where(l => l.WarehouseId == vm.WarehouseId.Value && l.IsActive)
            .OrderByDescending(l => l.LockDate)
            .Select(l => (DateTime?)l.LockDate)
            .FirstOrDefaultAsync();
        if (lockDate.HasValue && vm.CountDate.Date <= lockDate.Value.Date)
        {
            TempData["Error"] = $"Kho đã khóa kỳ đến {lockDate.Value:dd/MM/yyyy}. Không thể tạo điều chỉnh kiểm kê cho ngày {vm.CountDate:dd/MM/yyyy}.";
            return RedirectToAction(nameof(StockCount), new { warehouseId = vm.WarehouseId, countDate = vm.CountDate });
        }
        var approvedExists = await _db.StockCountSheets.AsNoTracking()
            .AnyAsync(s => s.WarehouseId == vm.WarehouseId.Value && s.CountDate == vm.CountDate.Date && s.Status == StockCountStatusEnum.Approved);
        if (approvedExists)
        {
            TempData["Error"] = "Ngày kiểm kê này đã được duyệt. Hệ thống khóa không cho tạo/sửa phiếu kiểm kê mới.";
            return RedirectToAction(nameof(StockCount), new { warehouseId = vm.WarehouseId, countDate = vm.CountDate });
        }

        var normalizedLines = (vm.Lines ?? new List<StockCountLineInput>())
            .Where(l => l.ItemId > 0 && l.LocationId > 0)
            .Select(l => new StockCountLineInput
            {
                ItemId = l.ItemId,
                LocationId = l.LocationId,
                LotNumber = string.IsNullOrWhiteSpace(l.LotNumber) ? null : l.LotNumber.Trim(),
                ExpiryDate = l.ExpiryDate?.Date,
                CountedQty = l.CountedQty
            })
            .GroupBy(l => new { l.ItemId, l.LocationId, l.LotNumber, l.ExpiryDate })
            .Select(g => g.Last())
            .ToList();

        if (normalizedLines.Count == 0)
        {
            TempData["Error"] = "Không có dòng kiểm kê hợp lệ.";
            return RedirectToAction(nameof(StockCount), new { warehouseId = vm.WarehouseId, countDate = vm.CountDate });
        }
        if (normalizedLines.Any(l => l.CountedQty < 0))
        {
            TempData["Error"] = "Số lượng thực tế không được âm.";
            return RedirectToAction(nameof(StockCount), new { warehouseId = vm.WarehouseId, countDate = vm.CountDate });
        }
        var inputLocationIds = normalizedLines.Select(l => l.LocationId).Distinct().ToList();
        var validLocationIds = await _db.Locations.AsNoTracking()
            .Include(l => l.Zone)
            .Where(l => inputLocationIds.Contains(l.LocationId)
                && l.Zone != null
                && l.Zone.WarehouseId == vm.WarehouseId.Value)
            .Select(l => l.LocationId)
            .ToListAsync();
        if (validLocationIds.Count != inputLocationIds.Count)
        {
            TempData["Error"] = "Có vị trí không thuộc kho đang kiểm kê.";
            return RedirectToAction(nameof(StockCount), new { warehouseId = vm.WarehouseId, countDate = vm.CountDate });
        }

        // Validate items still exist and are active
        var inputItemIds = normalizedLines.Select(l => l.ItemId).Distinct().ToList();
        var activeItemIds = await _db.Items.AsNoTracking()
            .Where(i => inputItemIds.Contains(i.ItemId) && i.IsActive)
            .Select(i => i.ItemId)
            .ToListAsync();
        var inactiveItems = inputItemIds.Except(activeItemIds).ToList();
        if (inactiveItems.Count > 0)
        {
            var inactiveNames = await _db.Items.AsNoTracking()
                .Where(i => inactiveItems.Contains(i.ItemId))
                .Select(i => i.ItemCode)
                .ToListAsync();
            TempData["Error"] = $"Vật tư đã bị vô hiệu hóa hoặc không tồn tại: {string.Join(", ", inactiveNames)}. Vui lòng loại bỏ khỏi phiếu kiểm kê.";
            return RedirectToAction(nameof(StockCount), new { warehouseId = vm.WarehouseId, countDate = vm.CountDate });
        }

        // Security/integrity: never trust client SystemQty; recompute from DB by batch key.
        var itemIds = normalizedLines.Select(x => x.ItemId).Distinct().ToList();
        var locationIds = normalizedLines.Select(x => x.LocationId).Distinct().ToList();
        var currentRows = await _db.ItemLocations.AsNoTracking()
            .Where(il => itemIds.Contains(il.ItemId) && locationIds.Contains(il.LocationId))
            .Select(il => new { il.ItemId, il.LocationId, il.LotNumber, il.ExpiryDate, il.Quantity })
            .ToListAsync();
        foreach (var l in normalizedLines)
        {
            l.SystemQty = currentRows
                .Where(r => r.ItemId == l.ItemId
                    && r.LocationId == l.LocationId
                    && r.LotNumber == l.LotNumber
                    && r.ExpiryDate == l.ExpiryDate)
                .Select(r => r.Quantity)
                .FirstOrDefault();
        }

        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var sheet = new StockCountSheet
            {
                WarehouseId = vm.WarehouseId.Value,
                CountDate = vm.CountDate.Date,
                Notes = vm.Notes,
                Status = StockCountStatusEnum.Draft,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = VietnamNow
            };
            _db.StockCountSheets.Add(sheet);
            await _unitOfWork.SaveChangesAsync();

            foreach (var l in normalizedLines)
            {
                _db.StockCountLines.Add(new StockCountLine
                {
                    StockCountSheetId = sheet.StockCountSheetId,
                    ItemId = l.ItemId,
                    LocationId = l.LocationId,
                    LotNumber = l.LotNumber,
                    ExpiryDate = l.ExpiryDate,
                    SystemQty = l.SystemQty,
                    CountedQty = l.CountedQty,
                    Variance = l.CountedQty - l.SystemQty
                });
            }
            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.CommitAsync();
            TempData["Success"] = $"Đã lưu phiếu kiểm kê nháp #{sheet.StockCountSheetId}. Vui lòng duyệt để sinh phiếu điều chỉnh.";
            return RedirectToAction(nameof(StockCount), new { warehouseId = vm.WarehouseId, countDate = vm.CountDate });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            TempData["Error"] = UserSafeError.WithPrefix(ex, "Lỗi lưu phiếu kiểm kê", "Không thể lưu phiếu kiểm kê lúc này. Vui lòng thử lại.");
            return RedirectToAction(nameof(StockCount), new { warehouseId = vm.WarehouseId, countDate = vm.CountDate });
        }
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StockCountApproveDraft(long id, string? approvalReason)
    {
        if (string.IsNullOrWhiteSpace(approvalReason))
        {
            TempData["Error"] = "Vui lòng nhập lý do duyệt kiểm kê.";
            return RedirectToAction(nameof(StockCount));
        }
        approvalReason = approvalReason.Trim();
        if (approvalReason.Length > 500)
            approvalReason = approvalReason[..500];
        var approver = User.Identity?.Name ?? "system";
        var scopedWh = GetScopedWarehouseId();
        int? redirectWarehouseId = null;
        DateTime? redirectCountDate = null;
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var sheet = await _db.StockCountSheets
                .FirstOrDefaultAsync(s => s.StockCountSheetId == id);
            if (sheet == null)
            {
                TempData["Error"] = "Không tìm thấy phiếu kiểm kê.";
                return RedirectToAction(nameof(StockCount));
            }
            using var ledgerScope = _inventoryTransactionService.BeginScope(new InventoryTransactionContext
            {
                TransactionType = InventoryTransactionTypeEnum.Adjust,
                TransactionGroupKey = $"stock-count:{sheet.StockCountSheetId}:approve",
                IdempotencyKeyPrefix = $"stock-count:{sheet.StockCountSheetId}:approve",
                WarehouseId = sheet.WarehouseId,
                ReferenceType = "StockCountSheet",
                ReferenceId = sheet.StockCountSheetId.ToString(),
                ReferenceCode = $"COUNT-{sheet.StockCountSheetId}",
                Actor = approver
            });
            if (scopedWh.HasValue && sheet.WarehouseId != scopedWh.Value)
                return Forbid();
            redirectWarehouseId = sheet.WarehouseId;
            redirectCountDate = sheet.CountDate;
            if (sheet.Status != StockCountStatusEnum.Draft)
            {
                TempData["Error"] = "Phiếu kiểm kê này đã được duyệt.";
                return RedirectToAction(nameof(StockCount), new { warehouseId = sheet.WarehouseId, countDate = sheet.CountDate });
            }
            if (sheet.GeneratedAdjustmentVoucherId.HasValue)
            {
                TempData["Error"] = "Phiếu kiểm kê này đã có phiếu điều chỉnh.";
                return RedirectToAction(nameof(StockCount), new { warehouseId = sheet.WarehouseId, countDate = sheet.CountDate });
            }
            if (string.Equals(sheet.CreatedBy, approver, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Người tạo phiếu kiểm kê không được tự duyệt. Vui lòng nhờ Manager/Admin khác duyệt.";
                return RedirectToAction(nameof(StockCount), new { warehouseId = sheet.WarehouseId, countDate = sheet.CountDate });
            }

            var duplicatedApproved = await _db.StockCountSheets
                .AnyAsync(s => s.StockCountSheetId != sheet.StockCountSheetId
                    && s.WarehouseId == sheet.WarehouseId
                    && s.CountDate == sheet.CountDate
                    && s.Status == StockCountStatusEnum.Approved);
            if (duplicatedApproved)
            {
                TempData["Error"] = "Đã tồn tại phiếu kiểm kê cùng kho/cùng ngày đã được duyệt. Không thể duyệt trùng.";
                return RedirectToAction(nameof(StockCount), new { warehouseId = sheet.WarehouseId, countDate = sheet.CountDate });
            }

            var lockDate = await _db.WarehousePeriodLocks.AsNoTracking()
                .Where(l => l.WarehouseId == sheet.WarehouseId && l.IsActive)
                .OrderByDescending(l => l.LockDate)
                .Select(l => (DateTime?)l.LockDate)
                .FirstOrDefaultAsync();
            if (lockDate.HasValue && sheet.CountDate.Date <= lockDate.Value.Date)
            {
                TempData["Error"] = $"Kho đã khóa kỳ đến {lockDate.Value:dd/MM/yyyy}. Không thể duyệt kiểm kê ngày {sheet.CountDate:dd/MM/yyyy}.";
                return RedirectToAction(nameof(StockCount), new { warehouseId = sheet.WarehouseId, countDate = sheet.CountDate });
            }

            var diffLines = await _db.StockCountLines
                .Where(l => l.StockCountSheetId == sheet.StockCountSheetId && l.Variance != null && l.Variance != 0)
                .ToListAsync();
            if (diffLines.Count > 0)
            {
                var diffLocationIds = diffLines.Select(x => x.LocationId).Distinct().ToList();
                var validDiffLocationIds = await _db.Locations.AsNoTracking()
                    .Include(l => l.Zone)
                    .Where(l => diffLocationIds.Contains(l.LocationId)
                        && l.Zone != null
                        && l.Zone.WarehouseId == sheet.WarehouseId)
                    .Select(l => l.LocationId)
                    .ToListAsync();
                if (validDiffLocationIds.Count != diffLocationIds.Count)
                    throw WmsExceptions.StockCountLocationMismatch();
            }

            Voucher? createdVoucher = null;
            if (diffLines.Count > 0)
            {
                var items = await _db.Items
                    .Where(i => diffLines.Select(d => d.ItemId).Contains(i.ItemId))
                    .ToDictionaryAsync(i => i.ItemId, i => i);

                var prefix = "PDC";
                var dateStr = VietnamNow.ToString("yyyyMMdd");
                for (var attempt = 0; attempt < 5; attempt++)
                {
                    var seq = await _db.Vouchers.CountAsync(v => v.VoucherCode.StartsWith(prefix + "-" + dateStr)) + 1;
                    var random = Random.Shared.Next(0, 100).ToString("D2");
                    var voucherCode = $"{prefix}-{dateStr}-{seq:D5}{random}";
                    var voucher = new Voucher
                    {
                        VoucherCode = voucherCode,
                        VoucherType = VoucherTypeEnum.DieuChinh,
                        VoucherDate = sheet.CountDate.Date,
                        WarehouseId = sheet.WarehouseId,
                        Description = $"Điều chỉnh từ kiểm kê #{sheet.StockCountSheetId}" + (string.IsNullOrWhiteSpace(sheet.Notes) ? "" : $" - {sheet.Notes}"),
                        SourceType = SourceTypeEnum.Manual,
                        CreatedBy = approver,
                        CreatedAt = VietnamNow,
                        IsPosted = true
                    };
                    _db.Vouchers.Add(voucher);
                    try
                    {
                        await _unitOfWork.SaveChangesAsync();
                        createdVoucher = voucher;
                        break;
                    }
                    catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
                        || ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true
                        || ex.InnerException?.Message.Contains("2627", StringComparison.OrdinalIgnoreCase) == true
                        || ex.InnerException?.Message.Contains("2601", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        _db.Entry(voucher).State = EntityState.Detached;
                    }
                }
                if (createdVoucher == null)
                    throw WmsExceptions.ReportAdjustmentCodeFailed();

                int lineNo = 0;
                decimal totalAmount = 0;
                foreach (var l in diffLines)
                {
                    if (!items.TryGetValue(l.ItemId, out var item))
                        throw WmsExceptions.ItemDisabled(l.ItemId);
                    lineNo++;
                    decimal baseQty = l.DiffQty ?? 0m;
                    if (baseQty == 0) continue;
                    var abs = Math.Abs(baseQty);
                    var lineAmount = item.UnitCost * abs;
                    var unitPrice = abs > 0 ? lineAmount / abs : 0m;

                    _db.VoucherDetails.Add(new VoucherDetail
                    {
                        VoucherId = createdVoucher.VoucherId,
                        ItemId = l.ItemId,
                        LocationId = l.LocationId,
                        TransactionQty = abs,
                        TransactionUomId = item.BaseUomId,
                        ConversionRate = 1m,
                        BaseQty = baseQty,
                        UnitPrice = unitPrice,
                        LineAmount = lineAmount,
                        QualityStatus = QualityStatusEnum.Good,
                        ExpiryDate = l.ExpiryDate,
                        LotNumber = l.LotNumber,
                        Notes = $"Kiểm kê #{sheet.StockCountSheetId}: hệ thống {l.SystemQty:N2}, thực tế {l.CountedQty:N2}",
                        LineNumber = lineNo,
                        DefectQty = 0,
                        DefectBaseQty = 0
                    });
                    totalAmount += lineAmount;

                    var itemLoc = await _db.ItemLocations.FirstOrDefaultAsync(il =>
                        il.ItemId == l.ItemId
                        && il.LocationId == l.LocationId
                        && il.LotNumber == l.LotNumber
                        && il.ExpiryDate == l.ExpiryDate);
                    if (itemLoc == null)
                    {
                        itemLoc = new ItemLocation
                        {
                            ItemId = l.ItemId,
                            LocationId = l.LocationId,
                            LotNumber = l.LotNumber,
                            ExpiryDate = l.ExpiryDate,
                            Quantity = 0,
                            UpdatedAt = VietnamNow
                        };
                        _db.ItemLocations.Add(itemLoc);
                    }
                    itemLoc.Quantity += baseQty;
                    if (itemLoc.Quantity < 0)
                        throw WmsExceptions.StockAdjustmentMakesNegativeLocation(item.ItemCode);

                    item.CurrentStock += baseQty;
                    if (item.CurrentStock < 0)
                        throw WmsExceptions.StockAdjustmentMakesNegativeItem(item.ItemCode);
                    item.TotalStockValue = item.CurrentStock * item.UnitCost;
                    item.UpdatedAt = VietnamNow;
                }

                createdVoucher.TotalLines = lineNo;
                createdVoucher.TotalAmount = totalAmount;
                sheet.GeneratedAdjustmentVoucherId = createdVoucher.VoucherId;
            }

            sheet.Status = StockCountStatusEnum.Approved;
            sheet.ApprovedBy = approver;
            sheet.ApprovedAt = VietnamNow;
            sheet.ApprovalReason = approvalReason;
            sheet.CompletedAt = VietnamNow;

            await _unitOfWork.SaveChangesAsync();

            // P0-03: Sync CurrentStock from ItemLocation source of truth
            if (diffLines.Count > 0)
            {
                var stockCountAffectedItemIds = diffLines.Select(l => l.ItemId).Distinct();
                await _inventoryBalanceService.SyncCurrentStockAsync(stockCountAffectedItemIds);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            if (sheet.GeneratedAdjustmentVoucherId.HasValue)
            {
                TempData["Success"] = $"Đã duyệt phiếu kiểm kê #{sheet.StockCountSheetId} và sinh phiếu điều chỉnh.";
                return RedirectToAction("Details", "Vouchers", new { id = sheet.GeneratedAdjustmentVoucherId.Value });
            }

            TempData["Success"] = $"Đã duyệt phiếu kiểm kê #{sheet.StockCountSheetId}. Không có chênh lệch.";
            return RedirectToAction(nameof(StockCount), new { warehouseId = sheet.WarehouseId, countDate = sheet.CountDate });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogWarning(ex, "Concurrency conflict when approving stock count. SheetId={SheetId}, Actor={Actor}", id, User.Identity?.Name);
            TempData["Error"] = "Dữ liệu kiểm kê đã thay đổi bởi phiên khác. Vui lòng tải lại và thử duyệt lại.";
            return RedirectToAction(nameof(StockCount), new { warehouseId = redirectWarehouseId, countDate = redirectCountDate });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Stock count approval failed. SheetId={SheetId}, Actor={Actor}", id, User.Identity?.Name);
            TempData["Error"] = UserSafeError.WithPrefix(ex, "Lỗi duyệt phiếu kiểm kê", "Không thể duyệt phiếu kiểm kê lúc này. Vui lòng thử lại.");
            return RedirectToAction(nameof(StockCount), new { warehouseId = redirectWarehouseId, countDate = redirectCountDate });
        }
    }


    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StockCountUnlockApproved(long id, string? unlockReason)
    {
        if (string.IsNullOrWhiteSpace(unlockReason))
        {
            TempData["Error"] = "Vui lòng nhập lý do mở khóa.";
            return RedirectToAction(nameof(StockCount));
        }
        unlockReason = unlockReason.Trim();
        if (unlockReason.Length > 500)
            unlockReason = unlockReason[..500];
        var actor = User.Identity?.Name ?? "system";

        var sheet = await _db.StockCountSheets
            .FirstOrDefaultAsync(s => s.StockCountSheetId == id);
        if (sheet == null)
        {
            TempData["Error"] = "Không tìm thấy phiếu kiểm kê.";
            return RedirectToAction(nameof(StockCount));
        }
        if (sheet.Status != StockCountStatusEnum.Approved)
        {
            TempData["Error"] = "Chỉ phiếu đã duyệt mới được mở khóa.";
            return RedirectToAction(nameof(StockCount), new { warehouseId = sheet.WarehouseId, countDate = sheet.CountDate });
        }
        if (!string.IsNullOrWhiteSpace(sheet.ApprovedBy)
            && string.Equals(sheet.ApprovedBy, actor, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Người duyệt phiếu kiểm kê không được tự mở khóa. Vui lòng chuyển Admin khác xử lý.";
            return RedirectToAction(nameof(StockCount), new { warehouseId = sheet.WarehouseId, countDate = sheet.CountDate });
        }

        if (sheet.GeneratedAdjustmentVoucherId.HasValue)
        {
            var voucher = await _db.Vouchers
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.VoucherId == sheet.GeneratedAdjustmentVoucherId.Value);
            if (voucher != null && !voucher.IsCancelled)
            {
                TempData["Error"] = "Phiếu điều chỉnh phát sinh từ kiểm kê chưa hủy. Vui lòng hủy phiếu điều chỉnh trước khi mở khóa kiểm kê.";
                return RedirectToAction(nameof(StockCount), new { warehouseId = sheet.WarehouseId, countDate = sheet.CountDate });
            }

            var hasChildren = await _db.Vouchers.AsNoTracking()
                .AnyAsync(v => v.ParentVoucherId == sheet.GeneratedAdjustmentVoucherId.Value && !v.IsCancelled);
            if (hasChildren)
            {
                TempData["Error"] = "Phiếu điều chỉnh đã được tham chiếu bởi nghiệp vụ khác. Không thể mở khóa.";
                return RedirectToAction(nameof(StockCount), new { warehouseId = sheet.WarehouseId, countDate = sheet.CountDate });
            }
        }

        sheet.Status = StockCountStatusEnum.Draft;
        sheet.ApprovedBy = null;
        sheet.ApprovedAt = null;
        sheet.ApprovalReason = null;
        sheet.CompletedAt = null;
        sheet.GeneratedAdjustmentVoucherId = null;
        sheet.UnlockedBy = actor;
        sheet.UnlockedAt = VietnamNow;
        sheet.UnlockReason = unlockReason;
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = $"Đã mở khóa phiếu kiểm kê #{sheet.StockCountSheetId}.";
        return RedirectToAction(nameof(StockCount), new { warehouseId = sheet.WarehouseId, countDate = sheet.CountDate });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> PeriodLocks()
    {
        var scopedWh = GetScopedWarehouseId();
        var whQuery = _db.Warehouses.Where(w => w.IsActive);
        if (scopedWh.HasValue) whQuery = whQuery.Where(w => w.WarehouseId == scopedWh.Value);

        ViewBag.Warehouses = await whQuery.OrderBy(w => w.WarehouseCode).ToListAsync();
        var locksQuery = _db.WarehousePeriodLocks
            .Include(l => l.Warehouse)
            .AsQueryable();
        if (scopedWh.HasValue) locksQuery = locksQuery.Where(l => l.WarehouseId == scopedWh.Value);

        var locks = await locksQuery
            .OrderByDescending(l => l.IsActive)
            .ThenByDescending(l => l.LockDate)
            .Take(200)
            .ToListAsync();
        return View(locks);
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPeriodLock(int warehouseId, DateTime lockDate, string? reason)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && warehouseId != scopedWh.Value) return Forbid();

        var wh = await _db.Warehouses.FirstOrDefaultAsync(w => w.WarehouseId == warehouseId && w.IsActive);
        if (wh == null)
        {
            TempData["Error"] = "Kho không hợp lệ.";
            return RedirectToAction(nameof(PeriodLocks));
        }

        var active = await _db.WarehousePeriodLocks
            .FirstOrDefaultAsync(l => l.WarehouseId == warehouseId && l.IsActive);
        if (active == null)
        {
            active = new WarehousePeriodLock
            {
                WarehouseId = warehouseId,
                LockDate = lockDate.Date,
                Reason = reason,
                LockedBy = User.Identity?.Name ?? "system",
                LockedAt = VietnamNow,
                IsActive = true
            };
            _db.WarehousePeriodLocks.Add(active);
        }
        else
        {
            active.LockDate = lockDate.Date;
            active.Reason = reason;
            active.LockedBy = User.Identity?.Name ?? "system";
            active.LockedAt = VietnamNow;
            active.IsActive = true;
            active.UnlockedAt = null;
            active.UnlockedBy = null;
        }

        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = $"Đã khóa kỳ kho {wh.WarehouseCode} đến ngày {lockDate:dd/MM/yyyy}.";
        return RedirectToAction(nameof(PeriodLocks));
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearPeriodLock(long id)
    {
        var lockRow = await _db.WarehousePeriodLocks
            .Include(l => l.Warehouse)
            .FirstOrDefaultAsync(l => l.WarehousePeriodLockId == id);
        if (lockRow == null) return NotFound();

        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && lockRow.WarehouseId != scopedWh.Value) return Forbid();

        lockRow.IsActive = false;
        lockRow.UnlockedAt = VietnamNow;
        lockRow.UnlockedBy = User.Identity?.Name ?? "system";
        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = $"Đã mở khóa kỳ cho kho {lockRow.Warehouse?.WarehouseCode}.";
        return RedirectToAction(nameof(PeriodLocks));
    }

}
