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

    [Authorize(Roles = "Admin")]
    [Authorize(Policy = WmsPermissions.AuditTrailView)]
    public async Task<IActionResult> AuditTrail(string? tableName, string? changedBy, DateTime? dateFrom, DateTime? dateTo)
    {
        dateFrom ??= VietnamNow.Date.AddDays(-7);
        dateTo ??= VietnamNow.Date.AddDays(1);

        var query = _db.AuditLogs
            .Where(a => a.ChangedAt >= dateFrom.Value && a.ChangedAt <= dateTo.Value);

        if (!string.IsNullOrWhiteSpace(tableName))
            query = query.Where(a => a.TableName == tableName);
        if (!string.IsNullOrWhiteSpace(changedBy))
            query = query.Where(a => a.ChangedBy != null && a.ChangedBy.Contains(changedBy));

        ViewBag.TableName = tableName;
        ViewBag.ChangedBy = changedBy;
        ViewBag.DateFrom = dateFrom;
        ViewBag.DateTo = dateTo;

        var logs = await query.OrderByDescending(a => a.ChangedAt).Take(200).ToListAsync();
        return View(logs);
    }


    [Authorize(Roles = "Admin")]
    [Authorize(Policy = WmsPermissions.ReportView)]
    public async Task<IActionResult> Alerts(AlertTypeEnum? type, bool? unresolvedOnly, int days = 30)
    {
        unresolvedOnly ??= true;
        if (days < 1) days = 1;
        if (days > 365) days = 365;

        ViewBag.Type = type;
        ViewBag.UnresolvedOnly = unresolvedOnly;
        ViewBag.Days = days;

        var query = _db.StockAlerts.AsNoTracking()
            .Include(a => a.Item)
            .Where(a => a.Item != null && a.Item.IsActive);

        if (type.HasValue) query = query.Where(a => a.AlertType == type.Value);
        if (unresolvedOnly == true) query = query.Where(a => !a.IsResolved);

        var alerts = await query.OrderBy(a => a.IsResolved).ThenByDescending(a => a.CreatedAt).Take(500).ToListAsync();
        return View(alerts);
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshExpiryAlerts(int days = 30)
    {
        if (days < 1) days = 1;
        if (days > 365) days = 365;

        var today = VietnamNow.Date;
        var cutoff = today.AddDays(days);

        // Aggregate expiring quantity per item (within window) and the nearest expiry date
        var expiring = await _db.ItemLocations.AsNoTracking()
            .Include(il => il.Item)
            .Where(il => il.Quantity > 0
                && il.Item != null
                && il.Item.IsActive
                && il.ExpiryDate.HasValue
                && il.ExpiryDate.Value.Date <= cutoff)
            .GroupBy(il => il.ItemId)
            .Select(g => new
            {
                ItemId = g.Key,
                Qty = g.Sum(x => x.Quantity),
                NearestExpiry = g.Min(x => x.ExpiryDate)
            })
            .ToListAsync();

        var expiringMap = expiring.ToDictionary(x => x.ItemId, x => x);

        // Upsert unresolved expiry alerts
        var existing = await _db.StockAlerts
            .Where(a => a.AlertType == AlertTypeEnum.Expiry && !a.IsResolved)
            .ToListAsync();

        foreach (var alert in existing)
        {
            if (!expiringMap.TryGetValue(alert.ItemId, out var e) || e.NearestExpiry == null)
            {
                alert.IsResolved = true;
                alert.ResolvedAt = VietnamNow;
                continue;
            }

            var nearest = e.NearestExpiry.Value.Date;
            var daysLeft = (nearest - today).Days;
            alert.CurrentStock = e.Qty;
            alert.Threshold = daysLeft;
            alert.IsRead = false;
        }

        foreach (var e in expiring)
        {
            if (e.NearestExpiry == null) continue;
            if (existing.Any(a => a.ItemId == e.ItemId)) continue;

            var nearest = e.NearestExpiry.Value.Date;
            var daysLeft = (nearest - today).Days;

            _db.StockAlerts.Add(new StockAlert
            {
                ItemId = e.ItemId,
                AlertType = AlertTypeEnum.Expiry,
                CurrentStock = e.Qty, // expiring qty
                Threshold = daysLeft, // days left
                IsRead = false,
                IsResolved = false,
                CreatedAt = VietnamNow
            });
        }

        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = $"Đã làm mới cảnh báo hết hạn (<= {days} ngày).";
        return RedirectToAction(nameof(Alerts), new { type = (byte)3, unresolvedOnly = true, days });
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveAlert(long id, byte? type, bool? unresolvedOnly, int days = 30)
    {
        var alert = await _db.StockAlerts.FindAsync(id);
        if (alert == null) return NotFound();
        alert.IsResolved = true;
        alert.ResolvedAt = VietnamNow;
        await _unitOfWork.SaveChangesAsync();
        return RedirectToAction(nameof(Alerts), new { type, unresolvedOnly, days });
    }


    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> OpsKpi(int? warehouseId)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        var wavesQuery = _db.Waves.AsNoTracking().AsQueryable();
        if (warehouseId.HasValue) wavesQuery = wavesQuery.Where(w => w.WarehouseId == warehouseId.Value);
        var waveIds = await wavesQuery.Select(w => w.WaveId).ToListAsync();

        var tasksQuery = _db.PickTasks.AsNoTracking().AsQueryable();
        if (warehouseId.HasValue)
        {
            tasksQuery = tasksQuery.Where(t =>
                (t.WaveId.HasValue && waveIds.Contains(t.WaveId.Value))
                || (!t.WaveId.HasValue && t.Voucher != null && t.Voucher.WarehouseId == warehouseId.Value));
        }
        var totalTasks = await tasksQuery.CountAsync();
        var doneTasks = await tasksQuery.CountAsync(t => t.Status == PickTaskStatusEnum.Completed);
        var shortTasks = await tasksQuery.CountAsync(t => t.Status == PickTaskStatusEnum.Short);
        var openTasks = await tasksQuery.CountAsync(t => t.Status == PickTaskStatusEnum.Pending || t.Status == PickTaskStatusEnum.Assigned || t.Status == PickTaskStatusEnum.InProgress);
        var completedDurations = await tasksQuery
            .Where(t => t.CompletedAt.HasValue && t.AssignedAt.HasValue)
            .Select(t => EF.Functions.DateDiffMinute(t.AssignedAt!.Value, t.CompletedAt!.Value))
            .ToListAsync();
        var avgMinutes = completedDurations.Count > 0 ? completedDurations.Average() : 0;

        var reservations = await _db.StockReservations.AsNoTracking()
            .Where(r => !warehouseId.HasValue || (r.Voucher != null && r.Voucher.WarehouseId == warehouseId.Value))
            .ToListAsync();
        var reserved = reservations.Sum(r => r.ReservedQty);
        var consumed = reservations.Sum(r => r.ConsumedQty);
        var fillRate = reserved > 0 ? (consumed / reserved) * 100m : 0m;

        var shippingQuery = _db.Vouchers.AsNoTracking()
            .Where(v => !v.IsCancelled
                && v.IsPosted
                && (v.VoucherType == VoucherTypeEnum.XuatKho
                    || v.VoucherType == VoucherTypeEnum.TraNCC
                    || v.VoucherType == VoucherTypeEnum.ChuyenKho
                    || v.VoucherType == VoucherTypeEnum.XuatSanXuat));
        if (warehouseId.HasValue)
            shippingQuery = shippingQuery.Where(v => v.WarehouseId == warehouseId.Value);

        var shippingRows = await shippingQuery
            .Select(v => new
            {
                v.VoucherId,
                v.WarehouseId,
                v.VoucherCode,
                v.VoucherDate,
                v.RequestedDeliveryDate,
                v.PackedAt,
                v.ShippedAt,
                v.TrackingNumber,
                v.ManifestCode,
                v.VoucherType
            })
            .ToListAsync();

        var waitingPacking = shippingRows.Count(v => !v.PackedAt.HasValue);
        var readyToShip = shippingRows.Count(v => v.PackedAt.HasValue && !v.ShippedAt.HasValue);
        var shippedCount = shippingRows.Count(v => v.ShippedAt.HasValue);
        var overdueShipping = shippingRows.Count(v => v.RequestedDeliveryDate.HasValue && v.RequestedDeliveryDate.Value.Date < VietnamNow.Date && !v.ShippedAt.HasValue);
        var shippedToday = shippingRows.Count(v => v.ShippedAt.HasValue && v.ShippedAt.Value.Date == VietnamNow.Date);
        var onTimeShipCandidates = shippingRows.Where(v => v.ShippedAt.HasValue && v.RequestedDeliveryDate.HasValue).ToList();
        var onTimeShipRate = onTimeShipCandidates.Count > 0
            ? onTimeShipCandidates.Count(v => v.ShippedAt!.Value.Date <= v.RequestedDeliveryDate!.Value.Date) * 100m / onTimeShipCandidates.Count
            : 0m;
        var packLeadHours = shippingRows
            .Where(v => v.PackedAt.HasValue)
            .Select(v => (v.PackedAt!.Value - v.VoucherDate).TotalHours)
            .ToList();
        var shipLeadHours = shippingRows
            .Where(v => v.ShippedAt.HasValue)
            .Select(v => (v.ShippedAt!.Value - (v.PackedAt ?? v.VoucherDate)).TotalHours)
            .ToList();
        var avgPackLeadHours = packLeadHours.Count > 0 ? packLeadHours.Average() : 0;
        var avgShipLeadHours = shipLeadHours.Count > 0 ? shipLeadHours.Average() : 0;

        var recentHandoverQuery = _db.ShippingHandoverLogs.AsNoTracking()
            .Include(x => x.Voucher)
            .Include(x => x.Warehouse)
            .AsQueryable();
        if (warehouseId.HasValue)
            recentHandoverQuery = recentHandoverQuery.Where(x => x.WarehouseId == warehouseId.Value);

        var recentHandovers = await recentHandoverQuery
            .OrderByDescending(x => x.HandedOverAt)
            .Take(15)
            .ToListAsync();

        // ── SLA theo đơn vị vận chuyển (Carrier) ──
        var allHandovers = await _db.ShippingHandoverLogs.AsNoTracking()
            .Include(h => h.Voucher)
            .Where(h => !string.IsNullOrEmpty(h.CarrierName)
                && (!warehouseId.HasValue || h.WarehouseId == warehouseId.Value))
            .ToListAsync();

        var carrierSlaRows = allHandovers
            .GroupBy(h => h.CarrierName ?? "Không xác định")
            .Select(g =>
            {
                var shipped = g.ToList();
                var total = shipped.Count;
                var onTime = shipped.Count(h =>
                    h.Voucher != null
                    && h.Voucher.RequestedDeliveryDate.HasValue
                    && h.HandedOverAt.Date <= h.Voucher.RequestedDeliveryDate.Value.Date);
                var overdue = shipped.Count(h =>
                    h.Voucher != null
                    && h.Voucher.RequestedDeliveryDate.HasValue
                    && h.HandedOverAt.Date > h.Voucher.RequestedDeliveryDate.Value.Date);
                var leadHours = shipped
                    .Where(h => h.Voucher != null)
                    .Select(h => (h.HandedOverAt - h.Voucher!.VoucherDate).TotalHours)
                    .ToList();
                var packToShipHours = shipped
                    .Where(h => h.Voucher != null && h.Voucher.PackedAt.HasValue)
                    .Select(h => (h.HandedOverAt - h.Voucher!.PackedAt!.Value).TotalHours)
                    .ToList();
                return new CarrierSlaRow
                {
                    CarrierName = g.Key,
                    TotalShipped = total,
                    OnTimeCount = onTime,
                    OverdueCount = overdue,
                    OnTimeRate = total > 0 ? Math.Round(onTime * 100m / total, 1) : 0m,
                    AvgLeadHours = leadHours.Count > 0 ? Math.Round(leadHours.Average(), 1) : 0,
                    AvgPackToShipHours = packToShipHours.Count > 0 ? Math.Round(packToShipHours.Average(), 1) : 0
                };
            })
            .OrderByDescending(r => r.TotalShipped)
            .ToList();

        ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.WarehouseCode).ToListAsync();
        ViewBag.WarehouseId = warehouseId;
        ViewBag.TotalTasks = totalTasks;
        ViewBag.DoneTasks = doneTasks;
        ViewBag.ShortTasks = shortTasks;
        ViewBag.OpenTasks = openTasks;
        ViewBag.AvgMinutes = avgMinutes;
        ViewBag.FillRate = fillRate;
        ViewBag.WaveCount = waveIds.Count;
        ViewBag.WaitingPacking = waitingPacking;
        ViewBag.ReadyToShip = readyToShip;
        ViewBag.ShippedCount = shippedCount;
        ViewBag.OverdueShipping = overdueShipping;
        ViewBag.ShippedToday = shippedToday;
        ViewBag.OnTimeShipRate = onTimeShipRate;
        ViewBag.AvgPackLeadHours = avgPackLeadHours;
        ViewBag.AvgShipLeadHours = avgShipLeadHours;
        ViewBag.RecentHandovers = recentHandovers;
        ViewBag.CarrierSlaRows = carrierSlaRows;

        var recentWaves = await wavesQuery
            .OrderByDescending(w => w.CreatedAt)
            .Take(10)
            .Select(w => new WaveBoardRow
            {
                WaveId = w.WaveId,
                WaveCode = w.WaveCode,
                WarehouseId = w.WarehouseId,
                WarehouseName = w.Warehouse != null ? w.Warehouse.WarehouseCode + " - " + w.Warehouse.WarehouseName : "",
                Status = w.Status,
                OpenTasks = 0,
                DoneTasks = 0,
                CreatedAt = w.CreatedAt,
                CompletedAt = w.CompletedAt
            })
            .ToListAsync();
        var recentWaveIds = recentWaves.Select(w => w.WaveId).Where(id => id.HasValue).Select(id => id!.Value).ToList();
        var taskStatsByWave = recentWaveIds.Count == 0
            ? new Dictionary<long, (int Open, int Done)>()
            : await _db.PickTasks.AsNoTracking()
                .Where(t => t.WaveId.HasValue && recentWaveIds.Contains(t.WaveId.Value))
                .GroupBy(t => t.WaveId!.Value)
                .Select(g => new
                {
                    WaveId = g.Key,
                    Open = g.Count(t => t.Status == PickTaskStatusEnum.Pending || t.Status == PickTaskStatusEnum.Assigned || t.Status == PickTaskStatusEnum.InProgress),
                    Done = g.Count(t => t.Status == PickTaskStatusEnum.Completed)
                })
                .ToDictionaryAsync(x => x.WaveId, x => (x.Open, x.Done));
        foreach (var wave in recentWaves)
        {
            if (wave.WaveId.HasValue && taskStatsByWave.TryGetValue(wave.WaveId.Value, out var stats))
            {
                wave.OpenTasks = stats.Open;
                wave.DoneTasks = stats.Done;
            }
        }

        var recentTasksQuery = _db.PickTasks.AsNoTracking()
            .Include(t => t.Wave)
            .Include(t => t.Voucher)
            .Include(t => t.Item)
            .Include(t => t.SourceLocation)
            .AsQueryable();
        if (warehouseId.HasValue)
            recentTasksQuery = recentTasksQuery.Where(t =>
                (t.Wave != null && t.Wave.WarehouseId == warehouseId.Value)
                || (t.Wave == null && t.Voucher != null && t.Voucher.WarehouseId == warehouseId.Value));

        var recentTasks = await recentTasksQuery
            .OrderByDescending(t => t.AssignedAt ?? t.CompletedAt ?? DateTime.MinValue)
            .ThenByDescending(t => t.PickTaskId)
            .Take(20)
            .Select(t => new PickTaskBoardRow
            {
                PickTaskId = t.PickTaskId,
                TaskCode = t.TaskCode,
                WaveId = t.WaveId,
                WaveCode = t.Wave != null ? t.Wave.WaveCode : "Phát hành trực tiếp",
                VoucherCode = t.Voucher != null ? t.Voucher.VoucherCode : "",
                ItemCode = t.Item != null ? t.Item.ItemCode : "",
                LocationCode = t.SourceLocation != null ? t.SourceLocation.LocationCode : "",
                TargetQty = t.TargetQty,
                PickedQty = t.PickedQty,
                Status = t.Status,
                AssignedTo = t.AssignedTo,
                CompletedAt = t.CompletedAt
            })
            .ToListAsync();

        ViewBag.RecentWaves = (object)recentWaves;
        ViewBag.RecentTasks = (object)recentTasks;
        return View();
    }


    // ═══════════════════════════════════════════════════════════════
    // Top hàng nhập / xuất nhiều nhất
    // ═══════════════════════════════════════════════════════════════
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.ReportView)]
    public async Task<IActionResult> TopItems(DateTime? dateFrom, DateTime? dateTo, string direction = "in", int top = 20, string sortBy = "qty")
    {
        var canSeeFinancial = CanSeeFinancial();
        dateFrom ??= VietnamNow.Date.AddDays(-30);
        dateTo ??= VietnamNow.Date;
        var endDate = dateTo.Value.AddDays(1);

        ViewBag.DateFrom = dateFrom;
        ViewBag.DateTo = dateTo;
        ViewBag.Direction = direction;
        ViewBag.Top = top;
        ViewBag.SortBy = sortBy;
        ViewBag.CanSeeFinancial = canSeeFinancial;

        // Inbound = VoucherType 1,4,7 ; Outbound = 2,3,8
        VoucherTypeEnum[] types = direction == "out"
            ? new VoucherTypeEnum[] { VoucherTypeEnum.XuatKho, VoucherTypeEnum.TraNCC, VoucherTypeEnum.XuatSanXuat }
            : new VoucherTypeEnum[] { VoucherTypeEnum.NhapKho, VoucherTypeEnum.KhachTra, VoucherTypeEnum.NhapThanhPham };

        var query = _db.VoucherDetails.AsNoTracking()
            .Include(d => d.Voucher)
            .Include(d => d.Item).ThenInclude(i => i!.Category)
            .Include(d => d.Item).ThenInclude(i => i!.BaseUom)
            .Where(d => d.Voucher != null
                && !d.Voucher.IsCancelled
                && d.Voucher.IsPosted
                && types.Contains(d.Voucher.VoucherType)
                && d.Voucher.VoucherDate >= dateFrom.Value
                && d.Voucher.VoucherDate < endDate
                && d.Item != null);

        var grouped = await query
            .GroupBy(d => new
            {
                d.ItemId,
                ItemCode = d.Item!.ItemCode,
                ItemName = d.Item!.ItemName,
                CategoryName = d.Item!.Category != null ? d.Item.Category.CategoryName : "Chưa phân loại",
                UomCode = d.Item!.BaseUom != null ? d.Item.BaseUom.UomCode : ""
            })
            .Select(g => new TopItemRow
            {
                ItemId = g.Key.ItemId,
                ItemCode = g.Key.ItemCode,
                ItemName = g.Key.ItemName,
                CategoryName = g.Key.CategoryName,
                UomCode = g.Key.UomCode,
                TotalQty = g.Sum(d => d.BaseQty),
                TotalValue = g.Sum(d => d.LineAmount),
                VoucherCount = g.Select(d => d.VoucherId).Distinct().Count()
            })
            .ToListAsync();

        // Sort and take top N
        var data = sortBy == "value"
            ? grouped.OrderByDescending(x => x.TotalValue).Take(top).ToList()
            : grouped.OrderByDescending(x => x.TotalQty).Take(top).ToList();

        if (!canSeeFinancial)
            foreach (var row in data) row.TotalValue = 0m;

        return View(data);
    }


    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.ReportView)]
    public async Task<IActionResult> ExportTopItems(DateTime? dateFrom, DateTime? dateTo, string direction = "in", int top = 50, string sortBy = "qty")
    {
        var canSeeFinancial = CanSeeFinancial();
        dateFrom ??= VietnamNow.Date.AddDays(-30);
        dateTo ??= VietnamNow.Date;
        var endDate = dateTo.Value.AddDays(1);

        VoucherTypeEnum[] types = direction == "out"
            ? new VoucherTypeEnum[] { VoucherTypeEnum.XuatKho, VoucherTypeEnum.TraNCC, VoucherTypeEnum.XuatSanXuat }
            : new VoucherTypeEnum[] { VoucherTypeEnum.NhapKho, VoucherTypeEnum.KhachTra, VoucherTypeEnum.NhapThanhPham };

        var grouped = await _db.VoucherDetails.AsNoTracking()
            .Include(d => d.Voucher)
            .Include(d => d.Item).ThenInclude(i => i!.Category)
            .Include(d => d.Item).ThenInclude(i => i!.BaseUom)
            .Where(d => d.Voucher != null
                && !d.Voucher.IsCancelled
                && d.Voucher.IsPosted
                && types.Contains(d.Voucher.VoucherType)
                && d.Voucher.VoucherDate >= dateFrom.Value
                && d.Voucher.VoucherDate < endDate
                && d.Item != null)
            .GroupBy(d => new
            {
                d.ItemId,
                ItemCode = d.Item!.ItemCode,
                ItemName = d.Item!.ItemName,
                CategoryName = d.Item!.Category != null ? d.Item.Category.CategoryName : "Chưa phân loại",
                UomCode = d.Item!.BaseUom != null ? d.Item.BaseUom.UomCode : ""
            })
            .Select(g => new TopItemRow
            {
                ItemId = g.Key.ItemId,
                ItemCode = g.Key.ItemCode,
                ItemName = g.Key.ItemName,
                CategoryName = g.Key.CategoryName,
                UomCode = g.Key.UomCode,
                TotalQty = g.Sum(d => d.BaseQty),
                TotalValue = g.Sum(d => d.LineAmount),
                VoucherCount = g.Select(d => d.VoucherId).Distinct().Count()
            })
            .ToListAsync();

        var data = sortBy == "value"
            ? grouped.OrderByDescending(x => x.TotalValue).Take(top).ToList()
            : grouped.OrderByDescending(x => x.TotalQty).Take(top).ToList();

        var dirLabel = direction == "out" ? "Xuất" : "Nhập";

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add($"Top{dirLabel}");

        ws.Cell(1, 1).Value = $"Top {top} Hàng {dirLabel} Nhiều Nhất";
        ws.Cell(2, 1).Value = $"Từ {dateFrom:dd/MM/yyyy} đến {dateTo:dd/MM/yyyy}";

        var row = 4;
        ws.Cell(row, 1).Value = "#";
        ws.Cell(row, 2).Value = "Mã VT";
        ws.Cell(row, 3).Value = "Tên Vật Tư";
        ws.Cell(row, 4).Value = "Danh Mục";
        ws.Cell(row, 5).Value = "ĐVT";
        ws.Cell(row, 6).Value = "Tổng SL";
        ws.Cell(row, 7).Value = "Số Phiếu";
        if (canSeeFinancial) ws.Cell(row, 8).Value = "Tổng Tiền";

        ws.Range(row, 1, row, canSeeFinancial ? 8 : 7).Style.Font.Bold = true;
        ws.Range(row, 1, row, canSeeFinancial ? 8 : 7).Style.Fill.BackgroundColor = XLColor.AirForceBlue;
        ws.Range(row, 1, row, canSeeFinancial ? 8 : 7).Style.Font.FontColor = XLColor.White;

        var rank = 0;
        foreach (var item in data)
        {
            row++; rank++;
            ws.Cell(row, 1).Value = rank;
            ws.Cell(row, 2).Value = item.ItemCode;
            ws.Cell(row, 3).Value = item.ItemName;
            ws.Cell(row, 4).Value = item.CategoryName ?? "";
            ws.Cell(row, 5).Value = item.UomCode;
            ws.Cell(row, 6).Value = item.TotalQty;
            ws.Cell(row, 7).Value = item.VoucherCount;
            if (canSeeFinancial) ws.Cell(row, 8).Value = item.TotalValue;
        }

        ws.Column(6).Style.NumberFormat.Format = "#,##0.00";
        if (canSeeFinancial) ws.Column(8).Style.NumberFormat.Format = "#,##0";
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Top{dirLabel}_{dateFrom:yyyyMMdd}_{dateTo:yyyyMMdd}.xlsx");
    }


    // ═══════════════════════════════════════════════════════════════
    // RPT-06: BÁO CÁO HÀNG SẮP HẾT HẠN (đặc tả 7.2)
    // ═══════════════════════════════════════════════════════════════
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> ExpiryReport(int? warehouseId)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;
        var today = VietnamNow.Date;
        var d30 = today.AddDays(30);
        var d60 = today.AddDays(60);
        var d90 = today.AddDays(90);

        var query = _db.ItemLocations.AsNoTracking()
            .Include(il => il.Item)
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Where(il => il.Quantity > 0
                && il.ExpiryDate.HasValue
                && il.ExpiryDate.Value <= d90
                && il.Location != null && il.Location.Zone != null);

        if (warehouseId.HasValue)
            query = query.Where(il => il.Location!.Zone!.WarehouseId == warehouseId.Value);

        var data = await query
            .OrderBy(il => il.ExpiryDate)
            .ThenBy(il => il.Item!.ItemCode)
            .Select(il => new
            {
                il.ItemId,
                ItemCode = il.Item!.ItemCode,
                ItemName = il.Item.ItemName,
                LocationCode = il.Location!.LocationCode,
                ZoneName = il.Location.Zone!.ZoneName,
                WarehouseName = il.Location.Zone.Warehouse != null ? il.Location.Zone.Warehouse.WarehouseName : "",
                il.LotNumber,
                il.ExpiryDate,
                il.Quantity,
                DaysToExpiry = il.ExpiryDate.HasValue ? (int)(il.ExpiryDate.Value - today).TotalDays : 999
            })
            .Take(500)
            .ToListAsync();

        var summary = new
        {
            Expired = data.Count(d => d.DaysToExpiry < 0),
            Within30 = data.Count(d => d.DaysToExpiry >= 0 && d.DaysToExpiry <= 30),
            Within60 = data.Count(d => d.DaysToExpiry > 30 && d.DaysToExpiry <= 60),
            Within90 = data.Count(d => d.DaysToExpiry > 60 && d.DaysToExpiry <= 90),
            TotalQty = data.Sum(d => d.Quantity)
        };

        ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.WarehouseCode).ToListAsync();
        ViewBag.WarehouseId = warehouseId;
        ViewBag.Data = data;
        ViewBag.Summary = summary;
        ViewBag.Today = today;
        return View();
    }


    // ═══════════════════════════════════════════════════════════════
    // RPT-07: BÁO CÁO HÀNG CHẬM LUÂN CHUYỂN (đặc tả 7.2)
    // ═══════════════════════════════════════════════════════════════
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> SlowMovingReport(int? warehouseId, int days = 90)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;
        var cutoff = VietnamNow.Date.AddDays(-days);

        var stockMap = await _inventoryBalanceService.GetStockByItemAsync(warehouseId);
        var itemIds = stockMap.Keys.ToList();
        var itemsWithStock = itemIds.Count == 0
            ? new List<Item>()
            : await _db.Items.AsNoTracking()
                .Include(i => i.Category).Include(i => i.BaseUom)
                .Where(i => i.IsActive && itemIds.Contains(i.ItemId))
                .OrderBy(i => i.ItemCode)
                .ToListAsync();

        var itemIdsWithStock = itemsWithStock.Select(i => i.ItemId).ToList();
        var lastTxQuery = _db.VoucherDetails.AsNoTracking()
            .Include(vd => vd.Voucher)
            .Where(vd => vd.Voucher != null
                && !vd.Voucher.IsCancelled
                && vd.Voucher.IsPosted
                && itemIdsWithStock.Contains(vd.ItemId));
        if (warehouseId.HasValue)
            lastTxQuery = lastTxQuery.Where(vd => vd.Voucher!.WarehouseId == warehouseId.Value);

        var lastTxDates = itemIdsWithStock.Count == 0
            ? new Dictionary<int, DateTime>()
            : await lastTxQuery
                .GroupBy(vd => vd.ItemId)
                .Select(g => new { ItemId = g.Key, LastDate = g.Max(vd => vd.Voucher!.VoucherDate) })
                .ToDictionaryAsync(x => x.ItemId, x => x.LastDate);

        var slowItems = itemsWithStock
            .Where(i =>
            {
                if (!lastTxDates.TryGetValue(i.ItemId, out var lastDate))
                    return true; // never transacted = extremely slow
                return lastDate < cutoff;
            })
            .Select(i => new
            {
                i.ItemId,
                i.ItemCode,
                i.ItemName,
                CategoryName = i.Category?.CategoryName ?? "—",
                UomCode = i.BaseUom?.UomCode ?? "—",
                CurrentStock = stockMap.TryGetValue(i.ItemId, out var qty) ? qty : 0m,
                StockValue = (stockMap.TryGetValue(i.ItemId, out var stockQty) ? stockQty : 0m) * i.UnitCost,
                LastTransactionDate = lastTxDates.TryGetValue(i.ItemId, out var ld) ? ld : (DateTime?)null,
                DaysSinceLastTx = lastTxDates.TryGetValue(i.ItemId, out var ld2) ? (int)(VietnamNow.Date - ld2).TotalDays : 9999
            })
            .OrderByDescending(x => x.DaysSinceLastTx)
            .ToList();

        ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.WarehouseCode).ToListAsync();
        ViewBag.WarehouseId = warehouseId;
        ViewBag.Days = days;
        ViewBag.Data = slowItems;
        ViewBag.CanSeeFinancial = CanSeeFinancial();
        return View();
    }


    // ═══════════════════════════════════════════════════════════════
    // RPT-12: BÁO CÁO ABC ANALYSIS (đặc tả 7.2)
    // Phân loại SKU theo Pareto: A=80% giá trị, B=15%, C=5%
    // ═══════════════════════════════════════════════════════════════
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> AbcAnalysis()
    {
        var canSeeFinancial = CanSeeFinancial();
        var itemRows = await _db.Items.AsNoTracking()
            .Include(i => i.Category).Include(i => i.BaseUom)
            .Where(i => i.IsActive)
            .ToListAsync();
        var stockMap = await _inventoryBalanceService.GetStockByItemAsync(itemIds: itemRows.Select(i => i.ItemId));
        _inventoryBalanceService.ApplyStockBalances(itemRows, stockMap);

        var items = itemRows
            .Where(i => stockMap.TryGetValue(i.ItemId, out var scopedQty) && scopedQty > 0)
            .OrderByDescending(i => i.TotalStockValue)
            .Select(i => new
            {
                i.ItemId,
                i.ItemCode,
                i.ItemName,
                CategoryName = i.Category != null ? i.Category.CategoryName : "—",
                UomCode = i.BaseUom != null ? i.BaseUom.UomCode : "—",
                i.CurrentStock,
                i.UnitCost,
                i.TotalStockValue
            })
            .ToList();

        var totalValue = items.Sum(i => i.TotalStockValue);
        if (totalValue == 0) totalValue = 1; // avoid div/0

        var results = new List<dynamic>();
        decimal cumulative = 0;
        int rank = 0;
        foreach (var item in items)
        {
            rank++;
            cumulative += item.TotalStockValue;
            var cumulativePct = cumulative / totalValue * 100;
            string abcClass;
            if (cumulativePct <= 80) abcClass = "A";
            else if (cumulativePct <= 95) abcClass = "B";
            else abcClass = "C";

            results.Add(new
            {
                Rank = rank,
                item.ItemCode,
                item.ItemName,
                item.CategoryName,
                item.UomCode,
                item.CurrentStock,
                item.UnitCost,
                item.TotalStockValue,
                CumulativePct = cumulativePct,
                AbcClass = abcClass
            });
        }

        var countA = results.Count(r => ((dynamic)r).AbcClass == "A");
        var countB = results.Count(r => ((dynamic)r).AbcClass == "B");
        var countC = results.Count(r => ((dynamic)r).AbcClass == "C");
        var valueA = items.Take(countA).Sum(i => i.TotalStockValue);
        var valueB = items.Skip(countA).Take(countB).Sum(i => i.TotalStockValue);
        var valueC = items.Skip(countA + countB).Sum(i => i.TotalStockValue);

        ViewBag.Data = results;
        ViewBag.TotalValue = totalValue;
        ViewBag.CountA = countA; ViewBag.CountB = countB; ViewBag.CountC = countC;
        ViewBag.ValueA = valueA; ViewBag.ValueB = valueB; ViewBag.ValueC = valueC;
        ViewBag.CanSeeFinancial = canSeeFinancial;
        return View();
    }


    // ═══════════════════════════════════════════════════════════════
    // ENTERPRISE: Analytics Dashboard (BI)
    // ═══════════════════════════════════════════════════════════════
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Analytics(int? warehouseId, int? days)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;
        days ??= 30;
        var fromDate = VietnamNow.Date.AddDays(-days.Value);
        var toDate = VietnamNow.Date;

        ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.WarehouseCode).ToListAsync();
        ViewBag.WarehouseId = warehouseId;
        ViewBag.Days = days.Value;

        var vq = _db.Vouchers.AsNoTracking().Where(v => !v.IsCancelled && v.IsPosted && v.VoucherDate >= fromDate && v.VoucherDate <= toDate);
        if (warehouseId.HasValue) vq = vq.Where(v => v.WarehouseId == warehouseId.Value);

        // Throughput by day
        var dailyThroughput = await vq
            .GroupBy(v => new { v.VoucherDate, v.VoucherType })
            .Select(g => new { g.Key.VoucherDate, g.Key.VoucherType, Count = g.Count(), Lines = g.Sum(v => v.TotalLines) })
            .ToListAsync();

        var dates = Enumerable.Range(0, days.Value + 1).Select(i => fromDate.AddDays(i)).ToList();
        ViewBag.ChartDates = dates.Select(d => d.ToString("dd/MM")).ToList();
        ViewBag.InboundByDay = dates.Select(d => dailyThroughput.Where(t => t.VoucherDate == d && (t.VoucherType == VoucherTypeEnum.NhapKho || t.VoucherType == VoucherTypeEnum.KhachTra || t.VoucherType == VoucherTypeEnum.NhapThanhPham)).Sum(t => t.Count)).ToList();
        ViewBag.OutboundByDay = dates.Select(d => dailyThroughput.Where(t => t.VoucherDate == d && (t.VoucherType == VoucherTypeEnum.XuatKho || t.VoucherType == VoucherTypeEnum.TraNCC || t.VoucherType == VoucherTypeEnum.XuatSanXuat)).Sum(t => t.Count)).ToList();
        ViewBag.LinesByDay = dates.Select(d => dailyThroughput.Where(t => t.VoucherDate == d).Sum(t => t.Lines)).ToList();

        // Summary KPIs
        var totalInbound = await vq.CountAsync(v => v.VoucherType == VoucherTypeEnum.NhapKho || v.VoucherType == VoucherTypeEnum.KhachTra || v.VoucherType == VoucherTypeEnum.NhapThanhPham);
        var totalOutbound = await vq.CountAsync(v => v.VoucherType == VoucherTypeEnum.XuatKho || v.VoucherType == VoucherTypeEnum.TraNCC || v.VoucherType == VoucherTypeEnum.XuatSanXuat);
        var totalLines = await vq.SumAsync(v => v.TotalLines);
        var totalValue = await vq.SumAsync(v => v.TotalAmount);
        ViewBag.TotalInbound = totalInbound;
        ViewBag.TotalOutbound = totalOutbound;
        ViewBag.TotalLines = totalLines;
        ViewBag.TotalValue = totalValue;

        // Inventory turnover
        var totalStock = await _inventoryBalanceService.GetTotalStockAsync(warehouseId);
        var avgDailyOutbound = days.Value > 0 ? (decimal)totalOutbound / days.Value : 0;
        ViewBag.TotalStock = totalStock;
        ViewBag.AvgDailyOutbound = avgDailyOutbound;
        ViewBag.DaysOfSupply = avgDailyOutbound > 0 ? Math.Round(totalStock / avgDailyOutbound, 1) : 0m;

        // QC summary
        var qcInspections = await _db.QualityInspections.AsNoTracking()
            .Where(qi => qi.CreatedAt >= fromDate)
            .ToListAsync();
        ViewBag.QcTotal = qcInspections.Count;
        ViewBag.QcPassed = qcInspections.Count(qi => qi.OverallResult == QualityStatusEnum.Passed);
        ViewBag.QcFailed = qcInspections.Count(qi => qi.OverallResult == QualityStatusEnum.Failed);

        return View();
    }


    // ═══════════════════════════════════════════════════════════════
    // ENTERPRISE: Space Utilization
    // ═══════════════════════════════════════════════════════════════
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> SpaceUtilization(int? warehouseId)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.WarehouseCode).ToListAsync();
        ViewBag.WarehouseId = warehouseId;

        var lq = _db.Locations.AsNoTracking()
            .Include(l => l.Zone).ThenInclude(z => z!.Warehouse)
            .Where(l => l.IsActive && l.Zone != null && l.Zone.IsActive);
        if (warehouseId.HasValue) lq = lq.Where(l => l.Zone!.WarehouseId == warehouseId.Value);

        var locations = await lq.OrderBy(l => l.Zone!.ZoneCode).ThenBy(l => l.LocationCode).ToListAsync();
        var locationIds = locations.Select(l => l.LocationId).ToList();

        var stockByLocationDict = locationIds.Count > 0
            ? await _db.ItemLocations.AsNoTracking()
                .Where(il => locationIds.Contains(il.LocationId) && il.Quantity > 0)
                .GroupBy(il => il.LocationId)
                .Select(g => new { LocationId = g.Key, ItemCount = g.Select(il => il.ItemId).Distinct().Count(), TotalQty = g.Sum(il => il.Quantity) })
                .ToDictionaryAsync(x => x.LocationId, x => new { x.ItemCount, x.TotalQty })
            : null;

        var rows = locations.Select(l =>
        {
            var s2 = stockByLocationDict != null && stockByLocationDict.ContainsKey(l.LocationId) ? stockByLocationDict[l.LocationId] : null;
            var totalQty = s2 != null ? s2.TotalQty : 0m;
            var maxCap = l.MaxCapacity > 0 ? l.MaxCapacity : 100m;
            var usedPercent = maxCap > 0 ? Math.Min(100, Math.Round(totalQty / maxCap * 100, 1)) : 0m;
            return new
            {
                l.LocationId,
                l.LocationCode,
                ZoneCode = l.Zone?.ZoneCode ?? "",
                ZoneName = l.Zone?.ZoneName ?? "",
                WarehouseName = l.Zone?.Warehouse?.WarehouseName ?? "",
                TotalQty = totalQty,
                MaxCapacity = maxCap,
                UsedPercent = usedPercent,
                ItemCount = s2 != null ? s2.ItemCount : 0,
                Status = usedPercent >= 90 ? "critical" : usedPercent >= 70 ? "warning" : usedPercent > 0 ? "ok" : "empty"
            };
        }).ToList();

        ViewBag.Rows = rows;
        ViewBag.TotalLocations = rows.Count;
        ViewBag.OccupiedLocations = rows.Count(r => r.TotalQty > 0);
        ViewBag.AvgUtilization = rows.Count > 0 ? Math.Round(rows.Average(r => (double)r.UsedPercent), 1) : 0;
        ViewBag.CriticalCount = rows.Count(r => r.Status == "critical");

        return View();
    }


    // ═══════════════════════════════════════════════════════════════
    // ENTERPRISE: Dock-to-Stock Time
    // ═══════════════════════════════════════════════════════════════
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DockToStock(int? warehouseId, int? days)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;
        days ??= 30;
        var fromDate = VietnamNow.Date.AddDays(-days.Value);

        ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.WarehouseCode).ToListAsync();
        ViewBag.WarehouseId = warehouseId;
        ViewBag.Days = days.Value;

        var q = _db.Vouchers.AsNoTracking()
            .Include(v => v.Warehouse).Include(v => v.Partner)
            .Where(v => !v.IsCancelled
                && v.IsPosted
                && (v.VoucherType == VoucherTypeEnum.NhapKho || v.VoucherType == VoucherTypeEnum.KhachTra || v.VoucherType == VoucherTypeEnum.NhapThanhPham)
                && v.CreatedAt >= fromDate);
        if (warehouseId.HasValue) q = q.Where(v => v.WarehouseId == warehouseId.Value);

        var vouchers = await q.OrderByDescending(v => v.CreatedAt).Take(200).ToListAsync();

        var rows = vouchers.Select(v =>
        {
            var dockArrival = v.ExpectedArrivalAt ?? v.CreatedAt;
            var receiveStart = v.SubmittedAt ?? v.CreatedAt;
            var completed = v.CompletedAt ?? v.UpdatedAt ?? v.CreatedAt;
            var dockToReceiveHours = Math.Max(0, (receiveStart - dockArrival).TotalHours);
            var receiveToStockHours = Math.Max(0, (completed - receiveStart).TotalHours);
            var totalHours = dockToReceiveHours + receiveToStockHours;
            return new
            {
                v.VoucherId,
                v.VoucherCode,
                WarehouseName = v.Warehouse?.WarehouseName ?? "",
                PartnerName = v.Partner?.PartnerName ?? "---",
                DockArrival = dockArrival,
                ReceiveStart = receiveStart,
                Completed = completed,
                DockToReceiveHours = Math.Round(dockToReceiveHours, 1),
                ReceiveToStockHours = Math.Round(receiveToStockHours, 1),
                TotalHours = Math.Round(totalHours, 1),
                Sla = totalHours <= 4 ? "good" : totalHours <= 8 ? "warning" : "critical"
            };
        }).ToList();

        ViewBag.Rows = rows;
        ViewBag.AvgDockToReceive = rows.Count > 0 ? Math.Round(rows.Average(r => (double)r.DockToReceiveHours), 1) : 0;
        ViewBag.AvgReceiveToStock = rows.Count > 0 ? Math.Round(rows.Average(r => (double)r.ReceiveToStockHours), 1) : 0;
        ViewBag.AvgTotal = rows.Count > 0 ? Math.Round(rows.Average(r => (double)r.TotalHours), 1) : 0;
        ViewBag.GoodCount = rows.Count(r => r.Sla == "good");
        ViewBag.WarningCount = rows.Count(r => r.Sla == "warning");
        ViewBag.CriticalCount = rows.Count(r => r.Sla == "critical");

        return View();
    }

}
