using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Models;
using WMS.Services;
using WMS.ViewModels;

namespace WMS.Controllers;

public partial class OperationsController
{
    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> ShipmentLoads(int? warehouseId, ShipmentLoadStatusEnum? status, string? search)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
            warehouseId = scopedWh.Value;

        var query = _db.ShipmentLoads.AsNoTracking()
            .Include(l => l.Warehouse)
            .AsQueryable();
        if (warehouseId.HasValue)
            query = query.Where(l => l.WarehouseId == warehouseId.Value);
        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(l => l.LoadCode.Contains(keyword)
                || (l.ManifestCode != null && l.ManifestCode.Contains(keyword))
                || (l.CarrierName != null && l.CarrierName.Contains(keyword))
                || (l.RouteName != null && l.RouteName.Contains(keyword))
                || (l.VehicleNumber != null && l.VehicleNumber.Contains(keyword)));
        }

        var rows = await query
            .OrderByDescending(l => l.PlannedDepartureAt ?? l.CreatedAt)
            .Take(300)
            .Select(l => new ShipmentLoadBoardRow
            {
                ShipmentLoadId = l.ShipmentLoadId,
                LoadCode = l.LoadCode,
                WarehouseId = l.WarehouseId,
                WarehouseName = l.Warehouse != null ? l.Warehouse.WarehouseName : "",
                Status = l.Status,
                CarrierName = l.CarrierName,
                RouteName = l.RouteName,
                VehicleNumber = l.VehicleNumber,
                DockDoor = l.DockDoor,
                ManifestCode = l.ManifestCode,
                TrackingNumber = l.TrackingNumber,
                PlannedDepartureAt = l.PlannedDepartureAt,
                ActualDepartureAt = l.ActualDepartureAt,
                TotalVoucherCount = l.TotalVoucherCount,
                TotalPackageCount = l.TotalPackageCount,
                TotalQuantity = l.TotalQuantity,
                TotalCatchWeight = l.TotalCatchWeight
            })
            .ToListAsync();
        var loadIds = rows.Select(r => r.ShipmentLoadId).ToList();
        var carrierShipments = await _db.CarrierShipments.AsNoTracking()
            .Include(s => s.OutboundPackage)
            .Where(s => (s.ShipmentLoadId.HasValue && loadIds.Contains(s.ShipmentLoadId.Value))
                || (s.OutboundPackage != null && s.OutboundPackage.ShipmentLoadId.HasValue && loadIds.Contains(s.OutboundPackage.ShipmentLoadId.Value)))
            .ToListAsync();
        foreach (var row in rows)
        {
            var loadShipments = carrierShipments
                .Where(s => s.ShipmentLoadId == row.ShipmentLoadId || s.OutboundPackage?.ShipmentLoadId == row.ShipmentLoadId)
                .ToList();
            row.CarrierShipmentCreatedCount = loadShipments.Count(s => s.Status == CarrierShipmentStatusEnum.Created || s.Status == CarrierShipmentStatusEnum.Delivered);
            row.CarrierShipmentFailedCount = loadShipments.Count(s => s.Status == CarrierShipmentStatusEnum.Failed || s.Status == CarrierShipmentStatusEnum.DeliveryFailed);
            row.CarrierShipmentSummary = loadShipments.Count > 0 ? $"{row.CarrierShipmentCreatedCount}/{row.TotalPackageCount} vận đơn" : null;
        }

        ViewBag.Warehouses = await GetVisibleWarehousesAsync();
        ViewBag.WarehouseId = warehouseId;
        ViewBag.Status = status;
        ViewBag.Search = search;
        return View(rows);
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> ShipmentLoadDetails(long id)
    {
        var scopedWh = GetScopedWarehouseId();
        var load = await _db.ShipmentLoads.AsNoTracking()
            .Include(l => l.Warehouse)
            .Include(l => l.YardVisit)
            .Include(l => l.Trailer)
            .FirstOrDefaultAsync(l => l.ShipmentLoadId == id);
        if (load == null)
            return NotFound();
        if (scopedWh.HasValue && load.WarehouseId != scopedWh.Value)
            return Forbid();

        var voucherRows = await _db.ShipmentLoadVouchers.AsNoTracking()
            .Where(x => x.ShipmentLoadId == id && x.RemovedAt == null)
            .Include(x => x.Voucher)!.ThenInclude(v => v!.Partner)
            .OrderBy(x => x.Sequence)
            .Select(x => new ShipmentLoadVoucherRow
            {
                VoucherId = x.VoucherId,
                VoucherCode = x.Voucher != null ? x.Voucher.VoucherCode : "",
                PartnerName = x.Voucher != null && x.Voucher.Partner != null ? x.Voucher.Partner.PartnerName : null,
                Sequence = x.Sequence,
                StopNumber = x.StopNumber,
                FulfillmentStatus = x.Voucher != null ? x.Voucher.FulfillmentStatus : FulfillmentStatusEnum.Draft,
                PackedAt = x.Voucher != null ? x.Voucher.PackedAt : null,
                ShippedAt = x.Voucher != null ? x.Voucher.ShippedAt : null
            })
            .ToListAsync();

        var packageRows = await _db.ShipmentLoadPackages.AsNoTracking()
            .Where(x => x.ShipmentLoadId == id && x.RemovedAt == null)
            .Include(x => x.OutboundPackage)!.ThenInclude(p => p!.Voucher)
            .OrderByDescending(x => x.LoadedAt ?? x.AddedAt)
            .Select(x => new ShipmentLoadPackageRow
            {
                OutboundPackageId = x.OutboundPackageId,
                PackageCode = x.OutboundPackage != null ? x.OutboundPackage.PackageCode : x.PackageCodeSnapshot,
                VoucherCode = x.OutboundPackage != null && x.OutboundPackage.Voucher != null ? x.OutboundPackage.Voucher.VoucherCode : "",
                IsLoaded = x.IsLoaded,
                LoadedBy = x.LoadedBy,
                LoadedAt = x.LoadedAt,
                TotalQuantity = x.OutboundPackage != null ? x.OutboundPackage.TotalQuantity : null,
                ActualCatchWeight = x.OutboundPackage != null ? x.OutboundPackage.ActualCatchWeight : null
            })
            .ToListAsync();
        var packageIds = packageRows.Select(p => p.OutboundPackageId).ToList();
        var carrierByPackage = (await _db.CarrierShipments.AsNoTracking()
            .Where(s => packageIds.Contains(s.OutboundPackageId))
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync())
            .GroupBy(s => s.OutboundPackageId)
            .ToDictionary(
                g => g.Key,
                g => g.Where(s => s.Status != CarrierShipmentStatusEnum.Cancelled).OrderByDescending(s => s.CreatedAt).FirstOrDefault()
                    ?? g.OrderByDescending(s => s.CreatedAt).First());
        foreach (var row in packageRows)
        {
            if (carrierByPackage.TryGetValue(row.OutboundPackageId, out var shipment))
            {
                row.CarrierName = shipment.CarrierNameSnapshot;
                row.CarrierStatus = shipment.Status;
                row.CarrierTrackingNumber = shipment.TrackingNumber;
                row.CarrierLastError = shipment.LastError;
            }
        }

        var mappedVoucherIds = voucherRows.Select(v => v.VoucherId).ToList();
        var candidateVouchers = await _db.Vouchers.AsNoTracking()
            .Include(v => v.Partner)
            .Where(v => v.WarehouseId == load.WarehouseId
                && !mappedVoucherIds.Contains(v.VoucherId)
                && !v.IsCancelled
                && v.IsPosted
                && v.PackedAt.HasValue
                && !v.ShippedAt.HasValue
                && (v.VoucherType == VoucherTypeEnum.XuatKho
                    || v.VoucherType == VoucherTypeEnum.TraNCC
                    || v.VoucherType == VoucherTypeEnum.ChuyenKho
                    || v.VoucherType == VoucherTypeEnum.XuatSanXuat))
            .OrderBy(v => v.VoucherCode)
            .Take(100)
            .ToListAsync();

        return View(new ShipmentLoadDetailsViewModel
        {
            Load = load,
            Vouchers = voucherRows,
            Packages = packageRows,
            CandidateVouchers = candidateVouchers
        });
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateShipmentLoad(int warehouseId, string? loadCode, string? carrierName, string? routeCode, string? routeName, string? vehicleNumber, long? yardVisitId, string? dockDoor, DateTime? plannedDepartureAt, string? sealNumber, string? manifestCode, string? tmsReferenceNo, string? notes)
    {
        try
        {
            var load = await _shipmentLoadService.CreateAsync(new ShipmentLoadCreateRequest
            {
                WarehouseId = warehouseId,
                LoadCode = loadCode,
                CarrierName = carrierName,
                RouteCode = routeCode,
                RouteName = routeName,
                VehicleNumber = vehicleNumber,
                YardVisitId = yardVisitId,
                DockDoor = dockDoor,
                PlannedDepartureAt = plannedDepartureAt,
                SealNumber = sealNumber,
                ManifestCode = manifestCode,
                TmsReferenceNo = tmsReferenceNo,
                Notes = notes
            }, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            TempData["Success"] = $"Đã tạo chuyến xe {load.LoadCode}.";
            return RedirectToAction(nameof(ShipmentLoadDetails), new { id = load.ShipmentLoadId });
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException)
        {
            TempData["Error"] = UserSafeError.From(ex);
            return RedirectToAction(nameof(ShipmentLoads), new { warehouseId });
        }
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddVoucherToShipmentLoad(long loadId, long voucherId, int? stopNumber)
    {
        try
        {
            await _shipmentLoadService.AddVoucherAsync(loadId, voucherId, stopNumber, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            TempData["Success"] = "Đã thêm phiếu vào chuyến xe.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }

        return RedirectToAction(nameof(ShipmentLoadDetails), new { id = loadId });
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveVoucherFromShipmentLoad(long loadId, long voucherId)
    {
        try
        {
            await _shipmentLoadService.RemoveVoucherAsync(loadId, voucherId, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            TempData["Success"] = "Đã gỡ phiếu khỏi chuyến xe.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }

        return RedirectToAction(nameof(ShipmentLoadDetails), new { id = loadId });
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ScanShipmentLoadPackage(long loadId, string packageCode)
    {
        var queued = QueuedOperationResponse.IsQueued(this);
        var redirectUrl = $"/Operations/ShipmentLoadDetails/{loadId}";
        try
        {
            await _shipmentLoadService.AddPackageByScanAsync(loadId, packageCode, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            var message = $"Đã quét kiện {packageCode}.";
            if (queued)
                return QueuedOperationResponse.Json(this, true, message, redirectUrl);
            TempData["Success"] = message;
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException)
        {
            if (queued)
            {
                var statusCode = ex is UnauthorizedAccessException ? StatusCodes.Status403Forbidden : StatusCodes.Status422UnprocessableEntity;
                var code = ex is UnauthorizedAccessException ? "FORBIDDEN" : "BUSINESS_RULE";
                return QueuedOperationResponse.Json(this, false, UserSafeError.From(ex), redirectUrl, statusCode, code);
            }

            TempData["Error"] = UserSafeError.From(ex);
        }

        return RedirectToAction(nameof(ShipmentLoadDetails), new { id = loadId });
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkShipmentLoadStatus(long loadId, ShipmentLoadStatusEnum status)
    {
        try
        {
            await _shipmentLoadService.MarkStatusAsync(loadId, status, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            TempData["Success"] = "Đã cập nhật trạng thái chuyến xe.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }

        return RedirectToAction(nameof(ShipmentLoadDetails), new { id = loadId });
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DepartShipmentLoad(long loadId, string? trackingNumber, string? manifestCode, string? note)
    {
        try
        {
            await _shipmentLoadService.DepartAsync(loadId, trackingNumber, manifestCode, note, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            TempData["Success"] = "Đã cho chuyến xe rời kho và cập nhật giao hàng cho các phiếu.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }

        return RedirectToAction(nameof(ShipmentLoadDetails), new { id = loadId });
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelShipmentLoad(long loadId)
    {
        try
        {
            await _shipmentLoadService.CancelAsync(loadId, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            TempData["Success"] = "Đã hủy chuyến xe.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }

        return RedirectToAction(nameof(ShipmentLoadDetails), new { id = loadId });
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> ExportShipmentLoadsCsv(int? warehouseId, ShipmentLoadStatusEnum? status, string? search)
    {
        var result = await ShipmentLoads(warehouseId, status, search);
        var rows = result is ViewResult view && view.Model is List<ShipmentLoadBoardRow> model
            ? model
            : new List<ShipmentLoadBoardRow>();

        var sb = new StringBuilder();
        sb.AppendLine("MaChuyen,Kho,TrangThai,DonViVanChuyen,Tuyen,Xe,BanKe,DuKienRoiKho,ThucTeRoiKho,SoPhieu,SoKien,SoLuong,TongCanThucTe");
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                Csv(row.LoadCode),
                Csv(row.WarehouseName),
                Csv(row.Status.ToString()),
                Csv(row.CarrierName),
                Csv(row.RouteName),
                Csv(row.VehicleNumber),
                Csv(row.ManifestCode),
                Csv(row.PlannedDepartureAt?.ToString("yyyy-MM-dd HH:mm")),
                Csv(row.ActualDepartureAt?.ToString("yyyy-MM-dd HH:mm")),
                row.TotalVoucherCount.ToString(),
                row.TotalPackageCount.ToString(),
                row.TotalQuantity.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture),
                (row.TotalCatchWeight ?? 0m).ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)
            }));
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"shipment_loads_{VietnamTime.FileStamp()}.csv");
    }

    private static string Csv(string? value)
        => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
}
