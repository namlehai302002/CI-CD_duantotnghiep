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

    public async Task<IActionResult> StockMovement(int? itemId, int? warehouseId, DateTime? dateFrom, DateTime? dateTo)
    {
        dateFrom ??= VietnamNow.Date.AddDays(-30);
        dateTo ??= VietnamNow.Date;

        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        var query = _db.VoucherDetails
            .Include(vd => vd.Voucher).ThenInclude(v => v!.Warehouse)
            .Include(vd => vd.Item).ThenInclude(i => i!.BaseUom)
            .Include(vd => vd.Location)
            .Include(vd => vd.TransactionUom)
            .Where(vd => vd.Voucher != null
                && !vd.Voucher.IsCancelled
                && vd.Voucher.IsPosted
                && vd.Voucher.VoucherDate >= dateFrom.Value
                && vd.Voucher.VoucherDate <= dateTo.Value);

        if (itemId.HasValue)
            query = query.Where(vd => vd.ItemId == itemId.Value);
        if (warehouseId.HasValue)
            query = query.Where(vd => vd.Voucher!.WarehouseId == warehouseId.Value);

        var data = await query.OrderByDescending(vd => vd.Voucher!.VoucherDate)
            .ThenBy(vd => vd.LineNumber).Take(500).ToListAsync();

        ViewBag.Items = await _db.Items.Where(i => i.IsActive).OrderBy(i => i.ItemCode).ToListAsync();
        ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync();
        ViewBag.ItemId = itemId;
        ViewBag.WarehouseId = warehouseId;
        ViewBag.DateFrom = dateFrom;
        ViewBag.DateTo = dateTo;
        ViewBag.Data = data;

        return View();
    }


    public async Task<IActionResult> ExportStockMovement(int? itemId, int? warehouseId, DateTime? dateFrom, DateTime? dateTo)
    {
        dateFrom ??= VietnamNow.Date.AddDays(-30);
        dateTo ??= VietnamNow.Date;

        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        var query = _db.VoucherDetails.AsNoTracking()
            .Include(vd => vd.Voucher).ThenInclude(v => v!.Warehouse)
            .Include(vd => vd.Item).ThenInclude(i => i!.BaseUom)
            .Include(vd => vd.TransactionUom)
            .Where(vd => vd.Voucher != null
                && !vd.Voucher.IsCancelled
                && vd.Voucher.IsPosted
                && vd.Voucher.VoucherDate >= dateFrom.Value
                && vd.Voucher.VoucherDate <= dateTo.Value);

        if (itemId.HasValue)
            query = query.Where(vd => vd.ItemId == itemId.Value);
        if (warehouseId.HasValue)
            query = query.Where(vd => vd.Voucher!.WarehouseId == warehouseId.Value);

        var data = await query
            .OrderByDescending(vd => vd.Voucher!.VoucherDate)
            .ThenBy(vd => vd.LineNumber)
            .Take(2000)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("XuatNhapTon");

        var row = 1;
        ws.Cell(row, 1).Value = "Ngày";
        ws.Cell(row, 2).Value = "Mã phiếu";
        ws.Cell(row, 3).Value = "Loại phiếu";
        ws.Cell(row, 4).Value = "Kho";
        ws.Cell(row, 5).Value = "Mã VT";
        ws.Cell(row, 6).Value = "Tên VT";
        ws.Cell(row, 7).Value = "SL (+/-)";
        ws.Cell(row, 8).Value = "ĐVT";

        ws.Range("A1:H1").Style.Font.Bold = true;
        ws.Range("A1:H1").Style.Fill.BackgroundColor = XLColor.AirForceBlue;
        ws.Range("A1:H1").Style.Font.FontColor = XLColor.White;

        foreach (var d in data)
        {
            row++;
            var v = d.Voucher!;

            ws.Cell(row, 1).Value = v.VoucherDate.ToString("dd/MM/yyyy");
            ws.Cell(row, 2).Value = v.VoucherCode;
            ws.Cell(row, 3).Value = v.VoucherTypeName;
            ws.Cell(row, 4).Value = v.Warehouse?.WarehouseName ?? "";
            ws.Cell(row, 5).Value = d.Item?.ItemCode ?? "";
            ws.Cell(row, 6).Value = d.Item?.ItemName ?? "";

            var signedQty = v.VoucherType switch
            {
                VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham => d.BaseQty,
                VoucherTypeEnum.XuatKho or VoucherTypeEnum.TraNCC or VoucherTypeEnum.XuatSanXuat => -d.BaseQty,
                VoucherTypeEnum.DieuChinh => d.BaseQty, // already carries sign (+/-)
                VoucherTypeEnum.ChuyenKho => 0m,        // transfer does not change total stock
                _ => 0m
            };
            ws.Cell(row, 7).Value = signedQty;
            ws.Cell(row, 8).Value = d.Item?.BaseUom?.UomCode ?? d.TransactionUom?.UomCode ?? "Không áp dụng";
        }

        ws.Column(7).Style.NumberFormat.Format = "#,##0.00";
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        var fileName = $"XuatNhapTon_{VietnamNow:yyyyMMdd_HHmm}.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }


    public async Task<IActionResult> InventoryTransactions(
        int? itemId,
        int? warehouseId,
        int? locationId,
        InventoryTransactionTypeEnum? transactionType,
        string? referenceType,
        string? referenceCode,
        long? licensePlateId,
        long? serialNumberId,
        DateTime? dateFrom,
        DateTime? dateTo)
    {
        var from = dateFrom?.Date ?? VietnamNow.Date.AddDays(-30);
        var to = dateTo?.Date ?? VietnamNow.Date;
        var toExclusive = to.AddDays(1);
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
            warehouseId = scopedWh.Value;

        var query = _db.InventoryTransactions
            .AsNoTracking()
            .Include(t => t.Warehouse)
            .Include(t => t.Item)
            .Include(t => t.Location)
            .Include(t => t.LicensePlate)
            .Include(t => t.SerialNumber)
            .Where(t => t.TransactionAt >= from && t.TransactionAt < toExclusive);

        if (itemId.HasValue)
            query = query.Where(t => t.ItemId == itemId.Value);
        if (warehouseId.HasValue)
            query = query.Where(t => t.WarehouseId == warehouseId.Value);
        if (locationId.HasValue)
            query = query.Where(t => t.LocationId == locationId.Value);
        if (transactionType.HasValue)
            query = query.Where(t => t.TransactionType == transactionType.Value);
        if (!string.IsNullOrWhiteSpace(referenceType))
            query = query.Where(t => t.ReferenceType == referenceType.Trim());
        if (!string.IsNullOrWhiteSpace(referenceCode))
        {
            var cleanReference = referenceCode.Trim();
            query = query.Where(t => t.ReferenceCode != null && t.ReferenceCode.Contains(cleanReference));
        }
        if (licensePlateId.HasValue)
            query = query.Where(t => t.LicensePlateId == licensePlateId.Value);
        if (serialNumberId.HasValue)
            query = query.Where(t => t.SerialNumberId == serialNumberId.Value);

        var data = await query
            .OrderByDescending(t => t.TransactionAt)
            .ThenByDescending(t => t.InventoryTransactionId)
            .Take(1000)
            .ToListAsync();

        ViewBag.Items = await _db.Items.AsNoTracking().Where(i => i.IsActive).OrderBy(i => i.ItemCode).ToListAsync();
        ViewBag.Warehouses = await _db.Warehouses.AsNoTracking().Where(w => w.IsActive).OrderBy(w => w.WarehouseCode).ToListAsync();
        ViewBag.Locations = await _db.Locations.AsNoTracking().OrderBy(l => l.LocationCode).ToListAsync();
        ViewBag.TransactionTypes = Enum.GetValues<InventoryTransactionTypeEnum>();
        ViewBag.ItemId = itemId;
        ViewBag.WarehouseId = warehouseId;
        ViewBag.LocationId = locationId;
        ViewBag.TransactionType = transactionType;
        ViewBag.ReferenceType = referenceType;
        ViewBag.ReferenceCode = referenceCode;
        ViewBag.LicensePlateId = licensePlateId;
        ViewBag.SerialNumberId = serialNumberId;
        ViewBag.DateFrom = from;
        ViewBag.DateTo = to;
        ViewBag.Data = data;

        return View();
    }


    public async Task<IActionResult> ExportInventoryTransactions(
        int? itemId,
        int? warehouseId,
        int? locationId,
        InventoryTransactionTypeEnum? transactionType,
        string? referenceType,
        string? referenceCode,
        long? licensePlateId,
        long? serialNumberId,
        DateTime? dateFrom,
        DateTime? dateTo)
    {
        var from = dateFrom?.Date ?? VietnamNow.Date.AddDays(-30);
        var to = dateTo?.Date ?? VietnamNow.Date;
        var toExclusive = to.AddDays(1);
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
            warehouseId = scopedWh.Value;

        var query = _db.InventoryTransactions
            .AsNoTracking()
            .Include(t => t.Warehouse)
            .Include(t => t.Item)
            .Include(t => t.Location)
            .Include(t => t.LicensePlate)
            .Include(t => t.SerialNumber)
            .Where(t => t.TransactionAt >= from && t.TransactionAt < toExclusive);

        if (itemId.HasValue)
            query = query.Where(t => t.ItemId == itemId.Value);
        if (warehouseId.HasValue)
            query = query.Where(t => t.WarehouseId == warehouseId.Value);
        if (locationId.HasValue)
            query = query.Where(t => t.LocationId == locationId.Value);
        if (transactionType.HasValue)
            query = query.Where(t => t.TransactionType == transactionType.Value);
        if (!string.IsNullOrWhiteSpace(referenceType))
            query = query.Where(t => t.ReferenceType == referenceType.Trim());
        if (!string.IsNullOrWhiteSpace(referenceCode))
        {
            var cleanReference = referenceCode.Trim();
            query = query.Where(t => t.ReferenceCode != null && t.ReferenceCode.Contains(cleanReference));
        }
        if (licensePlateId.HasValue)
            query = query.Where(t => t.LicensePlateId == licensePlateId.Value);
        if (serialNumberId.HasValue)
            query = query.Where(t => t.SerialNumberId == serialNumberId.Value);

        var data = await query
            .OrderByDescending(t => t.TransactionAt)
            .ThenByDescending(t => t.InventoryTransactionId)
            .Take(5000)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("InventoryLedger");
        var headers = new[]
        {
            "Time", "Type", "Group", "Idempotency", "Warehouse", "Item", "Location", "Lot", "Expiry",
            "Hold Before", "Hold After", "Qty Delta", "Reserved Delta", "Available Delta",
            "Qty Before", "Qty After", "Reserved Before", "Reserved After", "Available Before", "Available After",
            "Reference", "LPN", "Serial", "Actor"
        };

        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        ws.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
        ws.Range(1, 1, 1, headers.Length).Style.Fill.BackgroundColor = XLColor.AirForceBlue;
        ws.Range(1, 1, 1, headers.Length).Style.Font.FontColor = XLColor.White;

        var row = 1;
        foreach (var transaction in data)
        {
            row++;
            ws.Cell(row, 1).Value = transaction.TransactionAt;
            ws.Cell(row, 2).Value = transaction.TransactionType.ToString();
            ws.Cell(row, 3).Value = transaction.TransactionGroupKey;
            ws.Cell(row, 4).Value = transaction.IdempotencyKey;
            ws.Cell(row, 5).Value = transaction.Warehouse?.WarehouseCode ?? transaction.WarehouseId.ToString();
            ws.Cell(row, 6).Value = transaction.Item?.ItemCode ?? transaction.ItemId.ToString();
            ws.Cell(row, 7).Value = transaction.Location?.LocationCode ?? transaction.LocationId.ToString();
            ws.Cell(row, 8).Value = transaction.LotNumber ?? "";
            ws.Cell(row, 9).Value = transaction.ExpiryDate?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(row, 10).Value = transaction.HoldStatusBefore?.ToString() ?? "";
            ws.Cell(row, 11).Value = transaction.HoldStatusAfter?.ToString() ?? "";
            ws.Cell(row, 12).Value = transaction.QuantityDelta;
            ws.Cell(row, 13).Value = transaction.ReservedDelta;
            ws.Cell(row, 14).Value = transaction.AvailableDelta;
            ws.Cell(row, 15).Value = transaction.QuantityBefore;
            ws.Cell(row, 16).Value = transaction.QuantityAfter;
            ws.Cell(row, 17).Value = transaction.ReservedBefore;
            ws.Cell(row, 18).Value = transaction.ReservedAfter;
            ws.Cell(row, 19).Value = transaction.AvailableBefore;
            ws.Cell(row, 20).Value = transaction.AvailableAfter;
            ws.Cell(row, 21).Value = transaction.ReferenceCode ?? transaction.ReferenceId ?? transaction.ReferenceType ?? "";
            ws.Cell(row, 22).Value = transaction.LicensePlate?.LpnCode ?? transaction.LicensePlateId?.ToString() ?? "";
            ws.Cell(row, 23).Value = transaction.SerialNumber?.SerialCode ?? transaction.SerialNumberId?.ToString() ?? "";
            ws.Cell(row, 24).Value = transaction.Actor;
        }

        ws.Column(1).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
        ws.Columns(12, 20).Style.NumberFormat.Format = "#,##0.0000";
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();
        var fileName = $"InventoryLedger_{VietnamNow:yyyyMMdd_HHmm}.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }


    public async Task<IActionResult> Inventory(int? warehouseId, int? categoryId)
    {
        var canSeeFinancial = CanSeeFinancial();
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        var query = _db.Items.AsNoTracking().Include(i => i.Category).Include(i => i.BaseUom)
            .Where(i => i.IsActive).AsQueryable();

        if (categoryId.HasValue)
        {
            var targetCategoryIds = await _db.ItemCategories
                .Where(c => c.CategoryId == categoryId.Value || c.ParentCategoryId == categoryId.Value)
                .Select(c => c.CategoryId)
                .ToListAsync();

            query = query.Where(i => i.CategoryId.HasValue && targetCategoryIds.Contains(i.CategoryId.Value));
        }

        var items = await query.OrderBy(i => i.ItemCode).ToListAsync();
        var stockMap = await _inventoryBalanceService.GetStockByItemAsync(warehouseId, items.Select(i => i.ItemId));
        items = items
            .Where(i => stockMap.TryGetValue(i.ItemId, out var scopedQty) && scopedQty > 0)
            .ToList();
        _inventoryBalanceService.ApplyStockBalances(items, stockMap);

        // Least privilege: hide financial/cost fields for non-financial users.
        if (!canSeeFinancial)
        {
            foreach (var item in items)
            {
                item.UnitCost = 0m;
                item.TotalStockValue = 0m;
            }
        }

        ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync();
        ViewBag.Categories = await _db.ItemCategories.Where(c => c.IsActive).ToListAsync();
        ViewBag.WarehouseId = warehouseId;
        ViewBag.CategoryId = categoryId;
        ViewBag.CanSeeFinancial = canSeeFinancial;

        return View(items);
    }


    [Authorize(Policy = WmsPermissions.ReportViewFinancial)]
    public async Task<IActionResult> ExportInventory(int? warehouseId, int? categoryId)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        var query = _db.Items.AsNoTracking().Include(i => i.Category).Include(i => i.BaseUom)
            .Where(i => i.IsActive).AsQueryable();

        if (categoryId.HasValue)
        {
            var targetCategoryIds = await _db.ItemCategories
                .Where(c => c.CategoryId == categoryId.Value || c.ParentCategoryId == categoryId.Value)
                .Select(c => c.CategoryId)
                .ToListAsync();

            query = query.Where(i => i.CategoryId.HasValue && targetCategoryIds.Contains(i.CategoryId.Value));
        }

        var items = await query.OrderBy(i => i.ItemCode).ToListAsync();
        var stockMap = await _inventoryBalanceService.GetStockByItemAsync(warehouseId, items.Select(i => i.ItemId));
        items = items
            .Where(i => stockMap.TryGetValue(i.ItemId, out var scopedQty) && scopedQty > 0)
            .ToList();
        _inventoryBalanceService.ApplyStockBalances(items, stockMap);

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("BaoCaoTonKho");
            var currentRow = 1;

            // Header Row
            worksheet.Cell(currentRow, 1).Value = "Mã VT";
            worksheet.Cell(currentRow, 2).Value = "Tên Vật Tư";
            worksheet.Cell(currentRow, 3).Value = "Loại";
            worksheet.Cell(currentRow, 4).Value = "Danh Mục";
            worksheet.Cell(currentRow, 5).Value = "ĐVT";
            worksheet.Cell(currentRow, 6).Value = "Tồn Kho";
            worksheet.Cell(currentRow, 7).Value = "Giá Vốn BQ";
            worksheet.Cell(currentRow, 8).Value = "Tổng Tiền (VNĐ)";

            // Header Styling
            worksheet.Range("A1:H1").Style.Font.Bold = true;
            worksheet.Range("A1:H1").Style.Fill.BackgroundColor = XLColor.AirForceBlue;
            worksheet.Range("A1:H1").Style.Font.FontColor = XLColor.White;

            foreach (var item in items)
            {
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = item.ItemCode;
                worksheet.Cell(currentRow, 2).Value = item.ItemName;
                worksheet.Cell(currentRow, 3).Value = item.ItemTypeName;
                worksheet.Cell(currentRow, 4).Value = item.Category?.CategoryName ?? "---";
                worksheet.Cell(currentRow, 5).Value = item.BaseUom?.UomCode;
                worksheet.Cell(currentRow, 6).Value = item.CurrentStock;
                worksheet.Cell(currentRow, 7).Value = item.UnitCost;
                worksheet.Cell(currentRow, 8).Value = item.TotalStockValue;
            }

            // Formatting columns
            worksheet.Column(6).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Column(7).Style.NumberFormat.Format = "#,##0";
            worksheet.Column(8).Style.NumberFormat.Format = "#,##0";
            worksheet.Columns().AdjustToContents(); // Auto-fit

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCaoTonKho_{VietnamNow:yyyyMMdd_HHmm}.xlsx");
            }
        }
    }


    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.ReportViewFinancial)]
    public async Task<IActionResult> StockValuation(
        int? warehouseId,
        int? categoryId,
        string? itemSearch,
        string? lotNumber,
        DateTime? expiryDate,
        string mode = "current",
        DateTime? snapshotDate = null)
    {
        var model = await BuildStockValuationModelAsync(warehouseId, categoryId, itemSearch, lotNumber, expiryDate, mode, snapshotDate);
        return View(model);
    }


    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.ReportViewFinancial)]
    public async Task<IActionResult> ExportStockValuation(
        int? warehouseId,
        int? categoryId,
        string? itemSearch,
        string? lotNumber,
        DateTime? expiryDate,
        string mode = "current",
        DateTime? snapshotDate = null)
    {
        var model = await BuildStockValuationModelAsync(warehouseId, categoryId, itemSearch, lotNumber, expiryDate, mode, snapshotDate);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("DinhGiaTonKho");

        ws.Cell(1, 1).Value = "Báo cáo";
        ws.Cell(1, 2).Value = "Định giá tồn kho";
        ws.Cell(2, 1).Value = "Chế độ xem";
        ws.Cell(2, 2).Value = model.IsSnapshotMode ? "Ngày đã chốt" : "Tồn hiện tại";
        ws.Cell(3, 1).Value = "Ngày chốt";
        ws.Cell(3, 2).Value = model.SnapshotDate?.ToString("dd/MM/yyyy") ?? "";
        ws.Cell(4, 1).Value = "Tổng số mã hàng";
        ws.Cell(4, 2).Value = model.TotalItemCount;
        ws.Cell(5, 1).Value = "Tổng số lượng tồn";
        ws.Cell(5, 2).Value = model.TotalQuantity;
        ws.Cell(6, 1).Value = "Tổng giá trị tồn";
        ws.Cell(6, 2).Value = model.TotalValue;

        if (!string.IsNullOrWhiteSpace(model.Notice))
        {
            ws.Cell(7, 1).Value = "Thông báo";
            ws.Cell(7, 2).Value = model.Notice;
        }

        var headerRow = 9;
        var headers = new[]
        {
            "Kho", "Danh mục", "Mã hàng", "Tên hàng", "Đơn vị tính", "Lô", "Hạn dùng",
            "Trạng thái tồn", "Số lượng tồn", "Đã giữ chỗ", "Khả dụng", "Đơn giá vốn", "Giá trị tồn"
        };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(headerRow, i + 1).Value = headers[i];

        ws.Range(headerRow, 1, headerRow, headers.Length).Style.Font.Bold = true;
        ws.Range(headerRow, 1, headerRow, headers.Length).Style.Fill.BackgroundColor = XLColor.AirForceBlue;
        ws.Range(headerRow, 1, headerRow, headers.Length).Style.Font.FontColor = XLColor.White;

        var row = headerRow;
        foreach (var line in model.Rows)
        {
            row++;
            ws.Cell(row, 1).Value = $"{line.WarehouseCode} - {line.WarehouseName}";
            ws.Cell(row, 2).Value = string.IsNullOrWhiteSpace(line.CategoryName) ? "Chưa phân loại" : line.CategoryName;
            ws.Cell(row, 3).Value = line.ItemCode;
            ws.Cell(row, 4).Value = line.ItemName;
            ws.Cell(row, 5).Value = line.UomCode;
            ws.Cell(row, 6).Value = line.LotNumber ?? "";
            ws.Cell(row, 7).Value = line.ExpiryDate?.ToString("dd/MM/yyyy") ?? "";
            ws.Cell(row, 8).Value = GetHoldStatusDisplay(line.HoldStatus);
            ws.Cell(row, 9).Value = line.Quantity;
            ws.Cell(row, 10).Value = line.ReservedQty;
            ws.Cell(row, 11).Value = line.AvailableQty;
            ws.Cell(row, 12).Value = line.UnitCost;
            ws.Cell(row, 13).Value = line.StockValue;
        }

        var totalRow = row + 1;
        ws.Cell(totalRow, 8).Value = "Tổng cộng";
        ws.Cell(totalRow, 9).Value = model.TotalQuantity;
        ws.Cell(totalRow, 10).Value = model.TotalReservedQty;
        ws.Cell(totalRow, 11).Value = model.TotalAvailableQty;
        ws.Cell(totalRow, 13).Value = model.TotalValue;
        ws.Range(totalRow, 8, totalRow, 13).Style.Font.Bold = true;

        ws.Columns(9, 13).Style.NumberFormat.Format = "#,##0.####";
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"DinhGiaTonKho_{VietnamNow:yyyyMMdd_HHmm}.xlsx");
    }


    private async Task<StockValuationPageViewModel> BuildStockValuationModelAsync(
        int? warehouseId,
        int? categoryId,
        string? itemSearch,
        string? lotNumber,
        DateTime? expiryDate,
        string? mode,
        DateTime? snapshotDate)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        var normalizedMode = string.Equals(mode, "snapshot", StringComparison.OrdinalIgnoreCase) ? "snapshot" : "current";
        itemSearch = string.IsNullOrWhiteSpace(itemSearch) ? null : itemSearch.Trim();
        lotNumber = string.IsNullOrWhiteSpace(lotNumber) ? null : lotNumber.Trim();
        expiryDate = expiryDate?.Date;
        snapshotDate = snapshotDate?.Date;

        var model = new StockValuationPageViewModel
        {
            WarehouseId = warehouseId,
            CategoryId = categoryId,
            ItemSearch = itemSearch,
            LotNumber = lotNumber,
            ExpiryDate = expiryDate,
            SnapshotDate = snapshotDate,
            Mode = normalizedMode,
            Warehouses = await _db.Warehouses.AsNoTracking()
                .Where(w => w.IsActive && (!scopedWh.HasValue || w.WarehouseId == scopedWh.Value))
                .OrderBy(w => w.WarehouseCode)
                .ToListAsync(),
            Categories = await _db.ItemCategories.AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategoryName)
                .ToListAsync()
        };

        var targetCategoryIds = new List<int>();
        if (categoryId.HasValue)
        {
            targetCategoryIds = await _db.ItemCategories.AsNoTracking()
                .Where(c => c.CategoryId == categoryId.Value || c.ParentCategoryId == categoryId.Value)
                .Select(c => c.CategoryId)
                .ToListAsync();
        }

        if (model.IsSnapshotMode)
        {
            model.SnapshotDate ??= VietnamNow.Date;
            if (!warehouseId.HasValue)
            {
                model.Notice = "Vui lòng chọn kho để xem dữ liệu ngày đã chốt.";
                model.MissingSnapshot = true;
                return model;
            }

            var snapshotExists = await _db.StockSnapshots.AsNoTracking()
                .AnyAsync(s => s.WarehouseId == warehouseId.Value && s.SnapshotDate == model.SnapshotDate.Value);
            if (!snapshotExists)
            {
                model.Notice = "Chưa có dữ liệu chốt tồn cho kho và ngày đã chọn.";
                model.MissingSnapshot = true;
                return model;
            }

            var snapshotRows = await _db.StockSnapshots.AsNoTracking()
                .Include(s => s.Warehouse)
                .Include(s => s.Item).ThenInclude(i => i!.Category)
                .Include(s => s.Item).ThenInclude(i => i!.BaseUom)
                .Where(s => s.WarehouseId == warehouseId.Value && s.SnapshotDate == model.SnapshotDate.Value)
                .ToListAsync();

            if (targetCategoryIds.Count > 0)
                snapshotRows = snapshotRows.Where(s => s.Item?.CategoryId != null && targetCategoryIds.Contains(s.Item.CategoryId.Value)).ToList();
            if (!string.IsNullOrWhiteSpace(itemSearch))
            {
                var keyword = itemSearch.ToLowerInvariant();
                snapshotRows = snapshotRows
                    .Where(s => (s.Item?.ItemCode ?? "").ToLowerInvariant().Contains(keyword)
                        || (s.Item?.ItemName ?? "").ToLowerInvariant().Contains(keyword))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(lotNumber) || expiryDate.HasValue)
                model.Notice = "Dữ liệu ngày đã chốt được lưu theo mã hàng, không tách theo lô hoặc hạn dùng.";

            model.Rows = snapshotRows
                .Where(s => s.Item != null)
                .Select(s => new StockValuationRow
                {
                    ItemId = s.ItemId,
                    WarehouseCode = s.Warehouse?.WarehouseCode ?? "",
                    WarehouseName = s.Warehouse?.WarehouseName ?? "",
                    CategoryName = s.Item?.Category?.CategoryName ?? "",
                    ItemCode = s.Item?.ItemCode ?? "",
                    ItemName = s.Item?.ItemName ?? "",
                    UomCode = s.Item?.BaseUom?.UomCode ?? "",
                    LotNumber = null,
                    ExpiryDate = null,
                    HoldStatus = null,
                    Quantity = s.ClosingStock,
                    ReservedQty = 0,
                    AvailableQty = s.ClosingStock,
                    UnitCost = s.UnitCost,
                    StockValue = s.TotalValue
                })
                .OrderBy(r => r.WarehouseCode)
                .ThenBy(r => r.CategoryName)
                .ThenBy(r => r.ItemCode)
                .ToList();

            return model;
        }

        var itemLocationRows = await _db.ItemLocations.AsNoTracking()
            .Include(il => il.Item).ThenInclude(i => i!.Category)
            .Include(il => il.Item).ThenInclude(i => i!.BaseUom)
            .Include(il => il.Location).ThenInclude(l => l!.Zone).ThenInclude(z => z!.Warehouse)
            .Where(il => il.Quantity != 0
                && il.Item != null
                && il.Item.IsActive
                && il.Location != null
                && il.Location.Zone != null
                && il.Location.Zone.Warehouse != null)
            .ToListAsync();

        if (warehouseId.HasValue)
            itemLocationRows = itemLocationRows.Where(il => il.Location!.Zone!.WarehouseId == warehouseId.Value).ToList();
        if (targetCategoryIds.Count > 0)
            itemLocationRows = itemLocationRows.Where(il => il.Item?.CategoryId != null && targetCategoryIds.Contains(il.Item.CategoryId.Value)).ToList();
        if (!string.IsNullOrWhiteSpace(itemSearch))
        {
            var keyword = itemSearch.ToLowerInvariant();
            itemLocationRows = itemLocationRows
                .Where(il => (il.Item?.ItemCode ?? "").ToLowerInvariant().Contains(keyword)
                    || (il.Item?.ItemName ?? "").ToLowerInvariant().Contains(keyword))
                .ToList();
        }
        if (!string.IsNullOrWhiteSpace(lotNumber))
            itemLocationRows = itemLocationRows.Where(il => (il.LotNumber ?? "").Contains(lotNumber, StringComparison.OrdinalIgnoreCase)).ToList();
        if (expiryDate.HasValue)
            itemLocationRows = itemLocationRows.Where(il => il.ExpiryDate?.Date == expiryDate.Value).ToList();

        model.Rows = itemLocationRows
            .GroupBy(il => new
            {
                il.Location!.Zone!.WarehouseId,
                il.Location.Zone.Warehouse!.WarehouseCode,
                il.Location.Zone.Warehouse.WarehouseName,
                CategoryName = il.Item!.Category != null ? il.Item.Category.CategoryName : "",
                il.ItemId,
                il.Item.ItemCode,
                il.Item.ItemName,
                UomCode = il.Item.BaseUom != null ? il.Item.BaseUom.UomCode : "",
                il.LotNumber,
                ExpiryDate = il.ExpiryDate?.Date,
                il.HoldStatus,
                il.Item.UnitCost
            })
            .Select(g =>
            {
                var quantity = g.Sum(x => x.Quantity);
                var reservedQty = g.Sum(x => x.ReservedQty);
                return new StockValuationRow
                {
                    ItemId = g.Key.ItemId,
                    WarehouseCode = g.Key.WarehouseCode,
                    WarehouseName = g.Key.WarehouseName,
                    CategoryName = g.Key.CategoryName,
                    ItemCode = g.Key.ItemCode,
                    ItemName = g.Key.ItemName,
                    UomCode = g.Key.UomCode,
                    LotNumber = g.Key.LotNumber,
                    ExpiryDate = g.Key.ExpiryDate,
                    HoldStatus = g.Key.HoldStatus,
                    Quantity = quantity,
                    ReservedQty = reservedQty,
                    AvailableQty = quantity - reservedQty,
                    UnitCost = g.Key.UnitCost,
                    StockValue = quantity * g.Key.UnitCost
                };
            })
            .OrderBy(r => r.WarehouseCode)
            .ThenBy(r => r.CategoryName)
            .ThenBy(r => r.ItemCode)
            .ThenBy(r => r.ExpiryDate)
            .ThenBy(r => r.LotNumber)
            .ThenBy(r => r.HoldStatus)
            .ToList();

        return model;
    }


    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> StockSnapshot(int? warehouseId, DateTime? snapshotDate)
    {
        snapshotDate ??= VietnamNow.Date;

        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.WarehouseCode).ToListAsync();
        ViewBag.WarehouseId = warehouseId;
        ViewBag.SnapshotDate = snapshotDate;

        if (!warehouseId.HasValue)
        {
            return View(new List<StockSnapshotCompareRow>());
        }

        var snapshotRows = await _db.StockSnapshots.AsNoTracking()
            .Include(s => s.Item).ThenInclude(i => i!.BaseUom)
            .Where(s => s.SnapshotDate == snapshotDate.Value && s.WarehouseId == warehouseId.Value)
            .OrderBy(s => s.Item!.ItemCode)
            .ToListAsync();

        // Current stock per item in warehouse
        var currentStocks = await _db.ItemLocations.AsNoTracking()
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Where(il => il.Quantity != 0
                && il.Location != null
                && il.Location.Zone != null
                && il.Location.Zone.WarehouseId == warehouseId.Value)
            .GroupBy(il => il.ItemId)
            .Select(g => new { ItemId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Qty);

        List<StockSnapshotCompareRow> data;

        if (snapshotRows.Count == 0)
        {
            // No snapshot yet -> show PREVIEW of what will be snapshotted (current stock)
            var itemStocks = await _db.ItemLocations.AsNoTracking()
                .Include(il => il.Location).ThenInclude(l => l!.Zone)
                .Where(il => il.Quantity != 0
                    && il.Location != null
                    && il.Location.Zone != null
                    && il.Location.Zone.WarehouseId == warehouseId.Value)
                .GroupBy(il => il.ItemId)
                .Select(g => new { ItemId = g.Key, Qty = g.Sum(x => x.Quantity) })
                .ToListAsync();

            var itemIds = itemStocks.Select(x => x.ItemId).ToList();
            var items = await _db.Items.AsNoTracking()
                .Include(i => i.BaseUom)
                .Where(i => i.IsActive && itemIds.Contains(i.ItemId))
                .OrderBy(i => i.ItemCode)
                .ToListAsync();

            var qtyMap = itemStocks.ToDictionary(x => x.ItemId, x => x.Qty);
            data = items.Select(it =>
            {
                var currentQty = qtyMap.TryGetValue(it.ItemId, out var q) ? q : 0m;
                var snapshotValue = currentQty * it.UnitCost;
                return new StockSnapshotCompareRow
                {
                    ItemId = it.ItemId,
                    ItemCode = it.ItemCode,
                    ItemName = it.ItemName,
                    UomCode = it.BaseUom?.UomCode ?? "",
                    SnapshotQty = currentQty, // preview: will be saved as snapshot qty
                    CurrentQty = currentQty,
                    DiffQty = 0,
                    UnitCost = it.UnitCost,
                    SnapshotValue = snapshotValue,
                    CurrentValue = snapshotValue,
                    DiffValue = 0
                };
            }).ToList();

            ViewBag.IsPreview = true;
        }
        else
        {
            data = snapshotRows.Select(s =>
            {
                var currentQty = currentStocks.TryGetValue(s.ItemId, out var q) ? q : 0m;
                var diffQty = s.ClosingStock - currentQty; // needed adjustment to match snapshot
                var snapshotValue = s.ClosingStock * s.UnitCost;
                var currentValue = currentQty * s.UnitCost;
                return new StockSnapshotCompareRow
                {
                    ItemId = s.ItemId,
                    ItemCode = s.Item?.ItemCode ?? "",
                    ItemName = s.Item?.ItemName ?? "",
                    UomCode = s.Item?.BaseUom?.UomCode ?? "",
                    SnapshotQty = s.ClosingStock,
                    CurrentQty = currentQty,
                    DiffQty = diffQty,
                    UnitCost = s.UnitCost,
                    SnapshotValue = snapshotValue,
                    CurrentValue = currentValue,
                    DiffValue = snapshotValue - currentValue
                };
            }).ToList();

            ViewBag.IsPreview = false;
        }

        ViewBag.HasSnapshot = snapshotRows.Count > 0;
        ViewBag.DiffCount = data.Count(x => x.DiffQty != 0);
        return View(data);
    }


    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateStockSnapshot(int warehouseId, DateTime snapshotDate)
    {
        snapshotDate = snapshotDate.Date;

        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && warehouseId != scopedWh.Value)
            return Forbid();

        var wh = await _db.Warehouses.FirstOrDefaultAsync(w => w.WarehouseId == warehouseId && w.IsActive);
        if (wh == null)
        {
            TempData["Error"] = "Kho không hợp lệ.";
            return RedirectToAction(nameof(StockSnapshot), new { warehouseId, snapshotDate });
        }

        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // Remove existing snapshot for the day+warehouse to allow re-generate
            var existing = await _db.StockSnapshots
                .Where(s => s.WarehouseId == warehouseId && s.SnapshotDate == snapshotDate)
                .ToListAsync();
            if (existing.Count > 0)
            {
                _db.StockSnapshots.RemoveRange(existing);
                await _unitOfWork.SaveChangesAsync();
            }

            // Aggregate stock per item in warehouse
            var itemStocks = await _db.ItemLocations.AsNoTracking()
                .Include(il => il.Location).ThenInclude(l => l!.Zone)
                .Where(il => il.Quantity != 0
                    && il.Location != null
                    && il.Location.Zone != null
                    && il.Location.Zone.WarehouseId == warehouseId)
                .GroupBy(il => il.ItemId)
                .Select(g => new { ItemId = g.Key, Qty = g.Sum(x => x.Quantity) })
                .ToListAsync();

            var itemIds = itemStocks.Select(x => x.ItemId).ToList();
            var items = await _db.Items.AsNoTracking()
                .Where(i => i.IsActive && itemIds.Contains(i.ItemId))
                .ToDictionaryAsync(i => i.ItemId, i => i);

            var snapshots = new List<StockSnapshot>(itemStocks.Count);
            foreach (var s in itemStocks)
            {
                if (!items.TryGetValue(s.ItemId, out var item)) continue;
                var qty = s.Qty;
                var unitCost = item.UnitCost;
                snapshots.Add(new StockSnapshot
                {
                    SnapshotDate = snapshotDate,
                    ItemId = item.ItemId,
                    WarehouseId = warehouseId,
                    ClosingStock = qty,
                    UnitCost = unitCost,
                    TotalValue = qty * unitCost,
                    CreatedAt = VietnamNow
                });
            }

            if (snapshots.Count > 0)
            {
                await _db.StockSnapshots.AddRangeAsync(snapshots);
                await _unitOfWork.SaveChangesAsync();
            }

            await _unitOfWork.CommitAsync();
            TempData["Success"] = $"Đã chốt tồn kho '{wh.WarehouseName}' ngày {snapshotDate:dd/MM/yyyy} ({snapshots.Count} mã).";
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Generate stock snapshot failed. WarehouseId={WarehouseId}, SnapshotDate={SnapshotDate}, Actor={Actor}", warehouseId, snapshotDate, User.Identity?.Name);
            TempData["Error"] = UserSafeError.WithPrefix(ex, "Lỗi chốt tồn", "Không thể chốt tồn lúc này. Vui lòng thử lại.");
        }

        return RedirectToAction(nameof(StockSnapshot), new { warehouseId, snapshotDate });
    }


    /// <summary>
    /// Tạo phiếu điều chỉnh 1-click từ snapshot: tính chênh lệch, tạo phiếu, cập nhật tồn kho — tất cả trong 1 bước.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickAdjustFromSnapshot(int warehouseId, DateTime snapshotDate, string? notes)
    {
        snapshotDate = snapshotDate.Date;
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && warehouseId != scopedWh.Value)
            return Forbid();

        // ── 1. Load snapshot rows ──
        var snapshotRows = await _db.StockSnapshots.AsNoTracking()
            .Where(s => s.WarehouseId == warehouseId && s.SnapshotDate == snapshotDate)
            .ToListAsync();
        if (snapshotRows.Count == 0)
        {
            TempData["Error"] = "Chưa có snapshot cho kho/ngày đã chọn. Vui lòng chốt tồn trước.";
            return RedirectToAction(nameof(StockSnapshot), new { warehouseId, snapshotDate });
        }

        // ── 2. Tính chênh lệch snapshot vs tồn hiện tại ──
        var currentStocks = await _db.ItemLocations.AsNoTracking()
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Where(il => il.Quantity != 0
                && il.Location != null
                && il.Location.Zone != null
                && il.Location.Zone.WarehouseId == warehouseId)
            .GroupBy(il => il.ItemId)
            .Select(g => new { ItemId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Qty);

        var itemIds = snapshotRows.Select(s => s.ItemId).Distinct().ToList();
        var items = await _db.Items.Where(i => i.IsActive && itemIds.Contains(i.ItemId))
            .ToDictionaryAsync(i => i.ItemId, i => i);

        // Tìm vị trí tồn kho theo lô/hạn để giữ đúng granularity batch khi điều chỉnh giảm
        var stockLocs = await _db.ItemLocations.AsNoTracking()
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Where(il => il.Quantity > 0
                && il.Location != null
                && il.Location.Zone != null
                && il.Location.Zone.WarehouseId == warehouseId
                && itemIds.Contains(il.ItemId))
            .OrderBy(il => il.ExpiryDate == null)
            .ThenBy(il => il.ExpiryDate)
            .ThenByDescending(il => il.Quantity)
            .ToListAsync();

        var bestLocByItem = stockLocs
            .GroupBy(il => il.ItemId)
            .ToDictionary(g => g.Key, g => g.First().LocationId);
        var stockLayersByItem = stockLocs
            .GroupBy(il => il.ItemId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Build diff list
        var diffLines = new List<(int ItemId, decimal DiffQty, int? LocationId, string? LotNumber, DateTime? ExpiryDate)>();
        foreach (var s in snapshotRows)
        {
            if (!items.ContainsKey(s.ItemId)) continue;
            var currentQty = currentStocks.TryGetValue(s.ItemId, out var q) ? q : 0m;
            var diff = s.ClosingStock - currentQty;
            if (diff == 0) continue;

            var item = items[s.ItemId];
            if (diff < 0)
            {
                if (!stockLayersByItem.TryGetValue(s.ItemId, out var layers) || layers.Count == 0)
                    throw WmsExceptions.StockAdjustmentNoLotFound(item.ItemCode);

                var remainingToReduce = Math.Abs(diff);
                foreach (var layer in layers)
                {
                    if (remainingToReduce <= 0) break;
                    if (layer.Quantity <= 0) continue;

                    var take = Math.Min(remainingToReduce, layer.Quantity);
                    if (take <= 0) continue;

                    diffLines.Add((s.ItemId, -take, layer.LocationId, layer.LotNumber, layer.ExpiryDate));
                    remainingToReduce -= take;
                }

                if (remainingToReduce > 0)
                    throw WmsExceptions.StockAdjustmentInsufficientLotStock(item.ItemCode);
            }
            else
            {
                int? locId = item.DefaultLocationId
                    ?? (bestLocByItem.TryGetValue(s.ItemId, out var loc2) ? loc2 : null);
                if (!locId.HasValue)
                    throw WmsExceptions.StockAdjustmentNoDefaultLocation(item.ItemCode);
                diffLines.Add((s.ItemId, diff, locId, null, null));
            }
        }

        if (diffLines.Count == 0)
        {
            TempData["Info"] = "Không có chênh lệch giữa snapshot và tồn hiện tại. Không cần điều chỉnh.";
            return RedirectToAction(nameof(StockSnapshot), new { warehouseId, snapshotDate });
        }

        // ── 3. Check khóa kỳ ──
        var voucherDate = VietnamNow.Date;
        var lockDate = await _db.WarehousePeriodLocks.AsNoTracking()
            .Where(l => l.WarehouseId == warehouseId && l.IsActive)
            .OrderByDescending(l => l.LockDate)
            .Select(l => (DateTime?)l.LockDate)
            .FirstOrDefaultAsync();
        if (lockDate.HasValue && voucherDate.Date <= lockDate.Value.Date)
        {
            TempData["Error"] = $"Kho đã khóa kỳ đến {lockDate.Value:dd/MM/yyyy}. Không thể tạo phiếu điều chỉnh.";
            return RedirectToAction(nameof(StockSnapshot), new { warehouseId, snapshotDate });
        }

        // ── 4. Tạo phiếu + cập nhật tồn kho trong transaction ──
        var actor = User.Identity?.Name ?? "system";
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            using var ledgerScope = _inventoryTransactionService.BeginScope(new InventoryTransactionContext
            {
                TransactionType = InventoryTransactionTypeEnum.Adjust,
                TransactionGroupKey = $"snapshot:{warehouseId}:{snapshotDate:yyyyMMdd}:quick-adjust",
                IdempotencyKeyPrefix = $"snapshot:{warehouseId}:{snapshotDate:yyyyMMdd}:quick-adjust",
                WarehouseId = warehouseId,
                ReferenceType = "StockSnapshot",
                ReferenceId = $"{warehouseId}:{snapshotDate:yyyyMMdd}",
                ReferenceCode = $"SNAP-{warehouseId}-{snapshotDate:yyyyMMdd}",
                Actor = actor
            });
            // Generate voucher code
            var prefix = "PDC";
            var dateStr = VietnamNow.ToString("yyyyMMdd");
            Voucher? voucher = null;
            for (var attempt = 0; attempt < 5; attempt++)
            {
                var seq = await _db.Vouchers.CountAsync(v => v.VoucherCode.StartsWith(prefix + "-" + dateStr)) + 1;
                var random = Random.Shared.Next(0, 100).ToString("D2");
                var voucherCode = $"{prefix}-{dateStr}-{seq:D5}{random}";
                voucher = new Voucher
                {
                    VoucherCode = voucherCode,
                    VoucherType = VoucherTypeEnum.DieuChinh,
                    VoucherDate = voucherDate,
                    WarehouseId = warehouseId,
                    Description = string.IsNullOrWhiteSpace(notes)
                        ? $"Điều chỉnh tồn theo snapshot {snapshotDate:dd/MM/yyyy}"
                        : $"Điều chỉnh tồn theo snapshot {snapshotDate:dd/MM/yyyy} — {notes.Trim()}",
                    SourceType = SourceTypeEnum.Manual,
                    CreatedBy = actor,
                    CreatedAt = VietnamNow,
                    IsPosted = true
                };
                _db.Vouchers.Add(voucher);
                try
                {
                    await _unitOfWork.SaveChangesAsync();
                    break;
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
                    || ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true
                    || ex.InnerException?.Message.Contains("2627", StringComparison.OrdinalIgnoreCase) == true
                    || ex.InnerException?.Message.Contains("2601", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _db.Entry(voucher).State = EntityState.Detached;
                    voucher = null;
                }
            }
            if (voucher == null)
                throw WmsExceptions.ReportAdjustmentCodeFailed();

            // Create detail lines + update stock
            int lineNo = 0;
            decimal totalAmount = 0;
            foreach (var (itemId, diffQty, locationId, lotNumber, expiryDate) in diffLines)
            {
                var item = items[itemId];
                lineNo++;
                var abs = Math.Abs(diffQty);
                var lineAmount = item.UnitCost * abs;
                var snapQty = snapshotRows.First(r => r.ItemId == itemId).ClosingStock;

                _db.VoucherDetails.Add(new VoucherDetail
                {
                    VoucherId = voucher.VoucherId,
                    ItemId = itemId,
                    LocationId = locationId,
                    LotNumber = lotNumber,
                    ExpiryDate = expiryDate,
                    TransactionQty = abs,
                    TransactionUomId = item.BaseUomId,
                    ConversionRate = 1m,
                    BaseQty = diffQty, // carries sign: + increase, - decrease
                    UnitPrice = abs > 0 ? lineAmount / abs : 0m,
                    LineAmount = lineAmount,
                    QualityStatus = QualityStatusEnum.Good,
                    Notes = $"Snapshot {snapshotDate:dd/MM/yyyy}: chốt {snapQty:N2}, hiện tại {snapQty - diffQty:N2}, điều chỉnh {(diffQty > 0 ? "+" : "")}{diffQty:N2}",
                    LineNumber = lineNo,
                    DefectQty = 0,
                    DefectBaseQty = 0
                });
                totalAmount += lineAmount;

                // Update ItemLocation stock
                if (locationId.HasValue)
                {
                    var itemLoc = await _db.ItemLocations
                        .FirstOrDefaultAsync(il => il.ItemId == itemId
                            && il.LocationId == locationId.Value
                            && il.LotNumber == lotNumber
                            && il.ExpiryDate == expiryDate);
                    if (itemLoc == null)
                    {
                        itemLoc = new ItemLocation
                        {
                            ItemId = itemId,
                            LocationId = locationId.Value,
                            LotNumber = lotNumber,
                            ExpiryDate = expiryDate,
                            Quantity = 0,
                            UpdatedAt = VietnamNow
                        };
                        _db.ItemLocations.Add(itemLoc);
                    }
                    itemLoc.Quantity += diffQty;
                    if (itemLoc.Quantity < 0)
                        throw WmsExceptions.AdjustmentMakesNegativeLocation(item.ItemCode);
                    itemLoc.UpdatedAt = VietnamNow;
                }

                // Update Item total stock
                item.CurrentStock += diffQty;
                if (item.CurrentStock < 0)
                    throw WmsExceptions.AdjustmentMakesNegativeItem(item.ItemCode);
                item.TotalStockValue = item.CurrentStock * item.UnitCost;
                item.UpdatedAt = VietnamNow;
            }

            voucher.TotalLines = lineNo;
            voucher.TotalAmount = totalAmount;

            await _unitOfWork.SaveChangesAsync();

            // P0-03: Sync CurrentStock from ItemLocation source of truth
            var quickAdjustAffectedItemIds = diffLines.Select(l => l.ItemId).Distinct();
            await _inventoryBalanceService.SyncCurrentStockAsync(quickAdjustAffectedItemIds);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            TempData["Success"] = $"Đã tạo phiếu điều chỉnh theo snapshot {snapshotDate:dd/MM/yyyy} ({lineNo} dòng chênh lệch).";
            return RedirectToAction("Details", "Vouchers", new { id = voucher.VoucherId });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogWarning(ex, "Concurrency conflict when quick-adjust from snapshot. WarehouseId={WarehouseId}, SnapshotDate={SnapshotDate}, Actor={Actor}", warehouseId, snapshotDate, User.Identity?.Name);
            TempData["Error"] = "Dữ liệu đã thay đổi bởi phiên khác trong lúc tạo phiếu điều chỉnh. Vui lòng tải lại và thử lại.";
            return RedirectToAction(nameof(StockSnapshot), new { warehouseId, snapshotDate });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Quick adjust from snapshot failed. WarehouseId={WarehouseId}, SnapshotDate={SnapshotDate}, Actor={Actor}", warehouseId, snapshotDate, User.Identity?.Name);
            TempData["Error"] = UserSafeError.WithPrefix(ex, "Lỗi tạo phiếu điều chỉnh", "Không thể tạo phiếu điều chỉnh lúc này. Vui lòng thử lại.");
            return RedirectToAction(nameof(StockSnapshot), new { warehouseId, snapshotDate });
        }
    }


    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ExportStockSnapshot(int warehouseId, DateTime snapshotDate)
    {
        snapshotDate = snapshotDate.Date;
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && warehouseId != scopedWh.Value)
            return Forbid();

        var wh = await _db.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.WarehouseId == warehouseId);
        if (wh == null) return NotFound();

        var data = await _db.StockSnapshots.AsNoTracking()
            .Include(s => s.Item).ThenInclude(i => i!.BaseUom)
            .Where(s => s.WarehouseId == warehouseId && s.SnapshotDate == snapshotDate)
            .OrderBy(s => s.Item!.ItemCode)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("ChotTon");

        ws.Cell(1, 1).Value = "Kho";
        ws.Cell(1, 2).Value = wh.WarehouseName;
        ws.Cell(2, 1).Value = "Ngày chốt";
        ws.Cell(2, 2).Value = snapshotDate.ToString("dd/MM/yyyy");

        var row = 4;
        ws.Cell(row, 1).Value = "Mã VT";
        ws.Cell(row, 2).Value = "Tên VT";
        ws.Cell(row, 3).Value = "ĐVT";
        ws.Cell(row, 4).Value = "Tồn chốt";
        ws.Cell(row, 5).Value = "Giá vốn";
        ws.Cell(row, 6).Value = "Thành tiền";

        ws.Range(row, 1, row, 6).Style.Font.Bold = true;
        ws.Range(row, 1, row, 6).Style.Fill.BackgroundColor = XLColor.AirForceBlue;
        ws.Range(row, 1, row, 6).Style.Font.FontColor = XLColor.White;

        foreach (var s in data)
        {
            row++;
            ws.Cell(row, 1).Value = s.Item?.ItemCode ?? "";
            ws.Cell(row, 2).Value = s.Item?.ItemName ?? "";
            ws.Cell(row, 3).Value = s.Item?.BaseUom?.UomCode ?? "";
            ws.Cell(row, 4).Value = s.ClosingStock;
            ws.Cell(row, 5).Value = s.UnitCost;
            ws.Cell(row, 6).Value = s.TotalValue;
        }

        ws.Column(4).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(5).Style.NumberFormat.Format = "#,##0";
        ws.Column(6).Style.NumberFormat.Format = "#,##0";
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        var fileName = $"ChotTon_{wh.WarehouseCode}_{snapshotDate:yyyyMMdd}.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

}
