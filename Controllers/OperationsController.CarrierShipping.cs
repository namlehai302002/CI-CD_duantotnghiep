using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Models;
using WMS.ViewModels;

namespace WMS.Controllers;

public partial class OperationsController
{
    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> CarrierConnectors(int? warehouseId)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
            warehouseId = scopedWh.Value;

        var query = _db.CarrierConnectors.AsNoTracking()
            .Include(c => c.Warehouse)
            .AsQueryable();
        if (warehouseId.HasValue)
            query = query.Where(c => c.WarehouseId == warehouseId.Value);

        var model = new CarrierConnectorPageViewModel
        {
            WarehouseId = warehouseId,
            Warehouses = await GetVisibleWarehousesAsync(),
            Connectors = await query
                .OrderBy(c => c.Warehouse.WarehouseCode)
                .ThenByDescending(c => c.IsActive)
                .ThenBy(c => c.CarrierCode)
                .ToListAsync()
        };

        return View(model);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCarrierConnector(
        int? carrierConnectorId,
        int warehouseId,
        string carrierCode,
        string carrierName,
        CarrierAdapterTypeEnum adapterType,
        CarrierAuthTypeEnum authType,
        string? endpointUrl,
        string? apiKeyReference,
        bool isSandbox,
        bool isActive,
        bool requireShipmentCreatedBeforeShipping,
        string? notes)
    {
        try
        {
            var scopedWh = GetScopedWarehouseId();
            if (scopedWh.HasValue && scopedWh.Value != warehouseId)
                return Forbid();

            var code = NormalizeCarrierCode(carrierCode);
            var name = NormalizeRequired(carrierName, 120, "tên đơn vị vận chuyển");
            var endpoint = NormalizeOptional(endpointUrl, 300);
            if (adapterType == CarrierAdapterTypeEnum.Http && string.IsNullOrWhiteSpace(endpoint))
                throw new BusinessRuleException("Bộ kết nối HTTP cần có địa chỉ kết nối.", "CARRIER_ENDPOINT_REQUIRED", "CarrierConnector");

            var duplicate = await _db.CarrierConnectors.AnyAsync(c =>
                c.WarehouseId == warehouseId
                && c.CarrierCode == code
                && (!carrierConnectorId.HasValue || c.CarrierConnectorId != carrierConnectorId.Value));
            if (duplicate)
                throw new BusinessRuleException($"Mã đơn vị vận chuyển [{code}] đã tồn tại trong kho này.", "CARRIER_CONNECTOR_DUPLICATE", "CarrierConnector");

            var actor = User.Identity?.Name ?? "system";
            CarrierConnector connector;
            if (carrierConnectorId.HasValue)
            {
                connector = await _db.CarrierConnectors.FirstOrDefaultAsync(c => c.CarrierConnectorId == carrierConnectorId.Value)
                    ?? throw new BusinessRuleException("Không tìm thấy cấu hình đơn vị vận chuyển.", "CARRIER_CONNECTOR_NOT_FOUND", "CarrierConnector");
                if (scopedWh.HasValue && connector.WarehouseId != scopedWh.Value)
                    return Forbid();
            }
            else
            {
                connector = new CarrierConnector
                {
                    WarehouseId = warehouseId,
                    CreatedAt = VietnamNow,
                    CreatedBy = actor
                };
                _db.CarrierConnectors.Add(connector);
            }

            connector.CarrierCode = code;
            connector.CarrierName = name;
            connector.AdapterType = adapterType;
            connector.AuthType = authType;
            connector.EndpointUrl = endpoint;
            connector.ApiKeyReference = NormalizeOptional(apiKeyReference, 200);
            connector.IsSandbox = isSandbox;
            connector.IsActive = isActive;
            connector.RequireShipmentCreatedBeforeShipping = requireShipmentCreatedBeforeShipping;
            connector.Notes = NormalizeOptional(notes, 500);
            connector.UpdatedAt = VietnamNow;
            connector.UpdatedBy = actor;

            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = "Đã lưu cấu hình đơn vị vận chuyển.";
        }
        catch (BusinessRuleException ex)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }

        return RedirectToAction(nameof(CarrierConnectors), new { warehouseId });
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> ShippingDispatch(int? warehouseId, int? carrierConnectorId, CarrierShipmentStatusEnum? status, string? search)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
            warehouseId = scopedWh.Value;

        var connectorsQuery = _db.CarrierConnectors.AsNoTracking().Include(c => c.Warehouse).AsQueryable();
        if (warehouseId.HasValue)
            connectorsQuery = connectorsQuery.Where(c => c.WarehouseId == warehouseId.Value);
        var connectors = await connectorsQuery.OrderBy(c => c.Warehouse.WarehouseCode).ThenBy(c => c.CarrierCode).ToListAsync();

        var query = _db.OutboundPackages.AsNoTracking()
            .Include(p => p.Warehouse)
            .Include(p => p.Voucher)!.ThenInclude(v => v.Partner)
            .Include(p => p.ShipmentLoad)
            .Where(p => p.Voucher != null
                && !p.Voucher.IsCancelled
                && p.Voucher.IsPosted
                && p.Voucher.PackedAt.HasValue
                && (p.Voucher.VoucherType == VoucherTypeEnum.XuatKho
                    || p.Voucher.VoucherType == VoucherTypeEnum.TraNCC
                    || p.Voucher.VoucherType == VoucherTypeEnum.ChuyenKho
                    || p.Voucher.VoucherType == VoucherTypeEnum.XuatSanXuat));
        if (warehouseId.HasValue)
            query = query.Where(p => p.WarehouseId == warehouseId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(p => p.PackageCode.Contains(keyword)
                || (p.ReferenceLpnCode != null && p.ReferenceLpnCode.Contains(keyword))
                || (p.TrackingNumber != null && p.TrackingNumber.Contains(keyword))
                || (p.ManifestCode != null && p.ManifestCode.Contains(keyword))
                || (p.Voucher != null && p.Voucher.VoucherCode.Contains(keyword))
                || (p.Voucher != null && p.Voucher.Partner != null && p.Voucher.Partner.PartnerName.Contains(keyword))
                || (p.ShipmentLoad != null && p.ShipmentLoad.LoadCode.Contains(keyword)));
        }

        var packages = await query.OrderByDescending(p => p.PackedAt).Take(500).ToListAsync();
        var packageIds = packages.Select(p => p.OutboundPackageId).ToList();
        var shipments = await _db.CarrierShipments.AsNoTracking()
            .Include(s => s.CarrierConnector)
            .Where(s => packageIds.Contains(s.OutboundPackageId)
                && (!carrierConnectorId.HasValue || s.CarrierConnectorId == carrierConnectorId.Value))
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        var shipmentByPackage = shipments
            .GroupBy(s => s.OutboundPackageId)
            .ToDictionary(
                g => g.Key,
                g => g.Where(s => s.Status != CarrierShipmentStatusEnum.Cancelled).OrderByDescending(s => s.CreatedAt).FirstOrDefault()
                    ?? g.OrderByDescending(s => s.CreatedAt).First());

        var rows = packages.Select(p =>
        {
            shipmentByPackage.TryGetValue(p.OutboundPackageId, out var shipment);
            var voucher = p.Voucher!;
            return new ShippingDispatchRow
            {
                VoucherId = p.VoucherId,
                VoucherCode = voucher.VoucherCode,
                WarehouseId = p.WarehouseId,
                WarehouseName = p.Warehouse?.WarehouseName ?? "",
                PartnerName = voucher.Partner?.PartnerName,
                OutboundPackageId = p.OutboundPackageId,
                PackageCode = p.PackageCode,
                PackageTrackingNumber = p.TrackingNumber,
                ManifestCode = p.ManifestCode,
                LoadCode = p.ShipmentLoad?.LoadCode,
                LoadStatus = p.ShipmentLoad?.Status,
                PackedAt = p.PackedAt,
                ShippedAt = voucher.ShippedAt,
                CarrierShipmentId = shipment?.CarrierShipmentId,
                CarrierConnectorId = shipment?.CarrierConnectorId,
                CarrierCode = shipment?.CarrierCodeSnapshot,
                CarrierName = shipment?.CarrierNameSnapshot,
                CarrierStatus = shipment?.Status,
                CarrierTrackingNumber = shipment?.TrackingNumber,
                LabelUrl = shipment?.LabelUrl,
                ProofOfDeliveryUrl = shipment?.ProofOfDeliveryUrl,
                RetryCount = shipment?.RetryCount ?? 0,
                LastError = shipment?.LastError,
                CorrelationId = shipment?.CorrelationId,
                CreatedAt = shipment?.CreatedAt,
                UpdatedAt = shipment?.UpdatedAt
            };
        }).ToList();

        if (status.HasValue)
            rows = rows.Where(r => r.CarrierStatus == status.Value).ToList();

        var model = new ShippingDispatchViewModel
        {
            WarehouseId = warehouseId,
            CarrierConnectorId = carrierConnectorId,
            Status = status,
            Search = search,
            Warehouses = await GetVisibleWarehousesAsync(),
            Connectors = connectors,
            Rows = rows
        };
        return View(model);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCarrierShipment(long voucherId, int? carrierConnectorId)
    {
        try
        {
            var shipments = await _carrierIntegrationService.CreateShipmentsForVoucherAsync(voucherId, carrierConnectorId, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            TempData["Success"] = $"Đã tạo hoặc gửi yêu cầu tạo {shipments.Count} vận đơn.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }

        return RedirectToAction(nameof(ShippingDispatch));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetryCarrierShipment(long carrierShipmentId)
    {
        await ExecuteCarrierActionAsync(() => _carrierIntegrationService.RetryAsync(carrierShipmentId, GetScopedWarehouseId(), User.Identity?.Name ?? "system"), "Đã gửi lại yêu cầu vận đơn.");
        return RedirectToAction(nameof(ShippingDispatch));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelCarrierShipment(long carrierShipmentId)
    {
        await ExecuteCarrierActionAsync(() => _carrierIntegrationService.CancelAsync(carrierShipmentId, GetScopedWarehouseId(), User.Identity?.Name ?? "system"), "Đã hủy vận đơn.");
        return RedirectToAction(nameof(ShippingDispatch));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncCarrierShipment(long carrierShipmentId)
    {
        await ExecuteCarrierActionAsync(() => _carrierIntegrationService.SyncStatusAsync(carrierShipmentId, GetScopedWarehouseId(), User.Identity?.Name ?? "system"), "Đã gửi yêu cầu đồng bộ trạng thái vận đơn.");
        return RedirectToAction(nameof(ShippingDispatch));
    }

    private async Task ExecuteCarrierActionAsync(Func<Task<CarrierShipment>> action, string successMessage)
    {
        try
        {
            await action();
            TempData["Success"] = successMessage;
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
    }

    private static string NormalizeCarrierCode(string? value)
    {
        var cleaned = NormalizeRequired(value, 40, "mã đơn vị vận chuyển").ToUpperInvariant();
        return cleaned.Replace(" ", "", StringComparison.Ordinal);
    }

    private static string NormalizeRequired(string? value, int maxLength, string label)
    {
        var cleaned = NormalizeOptional(value, maxLength);
        if (cleaned == null)
            throw new BusinessRuleException($"Vui lòng nhập {label}.", "CARRIER_REQUIRED_FIELD_MISSING", "CarrierConnector");
        return cleaned;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
