using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;
using WMS.ViewModels;

namespace WMS.Services;

public sealed class DeliveryReconciliationFilter
{
    public int? WarehouseId { get; init; }
    public string? Severity { get; init; }
    public string? IssueType { get; init; }
    public string? Search { get; init; }
}

public interface IShippingReconciliationService
{
    Task<List<DeliveryReconciliationRow>> BuildAsync(DeliveryReconciliationFilter filter, CancellationToken ct = default);
}

public class ShippingReconciliationService : IShippingReconciliationService
{
    private readonly AppDbContext _db;

    public ShippingReconciliationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<DeliveryReconciliationRow>> BuildAsync(DeliveryReconciliationFilter filter, CancellationToken ct = default)
    {
        var rows = new List<DeliveryReconciliationRow>();
        await AddShippedReferenceIssuesAsync(rows, filter.WarehouseId, ct);
        await AddLoadScanIssuesAsync(rows, filter.WarehouseId, ct);
        await AddPackageLoadMismatchIssuesAsync(rows, filter.WarehouseId, ct);
        await AddCarrierShipmentIssuesAsync(rows, filter.WarehouseId, ct);
        await AddCarrierRequirementIssuesAsync(rows, filter.WarehouseId, ct);

        if (!string.IsNullOrWhiteSpace(filter.Severity))
            rows = rows.Where(r => string.Equals(r.Severity, filter.Severity.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(filter.IssueType))
            rows = rows.Where(r => string.Equals(r.IssueType, filter.IssueType.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var keyword = filter.Search.Trim();
            rows = rows.Where(r =>
                Contains(r.VoucherCode, keyword)
                || Contains(r.PackageCode, keyword)
                || Contains(r.LoadCode, keyword)
                || Contains(r.TrackingNumber, keyword)
                || Contains(r.Summary, keyword)).ToList();
        }

        return rows
            .GroupBy(r => $"{r.IssueType}:{r.VoucherId}:{r.OutboundPackageId}:{r.ShipmentLoadId}:{r.CarrierShipmentId}")
            .Select(g => g.First())
            .OrderByDescending(r => SeverityRank(r.Severity))
            .ThenBy(r => r.WarehouseName)
            .ThenBy(r => r.VoucherCode)
            .ThenBy(r => r.PackageCode)
            .ToList();
    }

    private async Task AddShippedReferenceIssuesAsync(List<DeliveryReconciliationRow> rows, int? warehouseId, CancellationToken ct)
    {
        var vouchers = await _db.Vouchers.AsNoTracking()
            .Include(v => v.Warehouse)
            .Where(v => v.ShippedAt.HasValue && !v.IsCancelled && (!warehouseId.HasValue || v.WarehouseId == warehouseId.Value))
            .ToListAsync(ct);

        foreach (var voucher in vouchers)
        {
            if (string.IsNullOrWhiteSpace(voucher.TrackingNumber))
            {
                rows.Add(BuildVoucherIssue(voucher, "shipped_missing_tracking", "Phiếu đã giao thiếu mã vận đơn", "warning",
                    "Phiếu đã xác nhận giao hàng nhưng chưa có mã vận đơn để đối soát.",
                    "Cập nhật mã vận đơn hoặc tạo vận đơn từ bảng điều phối vận chuyển."));
            }

            if (string.IsNullOrWhiteSpace(voucher.ManifestCode))
            {
                rows.Add(BuildVoucherIssue(voucher, "shipped_missing_manifest", "Phiếu đã giao thiếu mã chuyến bàn giao", "warning",
                    "Phiếu đã xác nhận giao hàng nhưng chưa có mã chuyến hoặc bản kê bàn giao.",
                    "Cập nhật mã chuyến bàn giao hoặc dùng chuyến xe để bàn giao tập trung."));
            }
        }
    }

    private async Task AddLoadScanIssuesAsync(List<DeliveryReconciliationRow> rows, int? warehouseId, CancellationToken ct)
    {
        var loads = await _db.ShipmentLoads.AsNoTracking()
            .Include(l => l.Warehouse)
            .Where(l => l.Status != ShipmentLoadStatusEnum.Cancelled && (!warehouseId.HasValue || l.WarehouseId == warehouseId.Value))
            .ToListAsync(ct);
        var loadIds = loads.Select(l => l.ShipmentLoadId).ToList();
        if (loadIds.Count == 0)
            return;

        var voucherMappings = await _db.ShipmentLoadVouchers.AsNoTracking()
            .Where(x => loadIds.Contains(x.ShipmentLoadId) && x.RemovedAt == null)
            .ToListAsync(ct);
        var voucherIds = voucherMappings.Select(x => x.VoucherId).Distinct().ToList();
        var packages = await _db.OutboundPackages.AsNoTracking()
            .Include(p => p.Voucher)
            .Where(p => voucherIds.Contains(p.VoucherId))
            .ToListAsync(ct);
        var packageMappings = await _db.ShipmentLoadPackages.AsNoTracking()
            .Where(x => loadIds.Contains(x.ShipmentLoadId) && x.RemovedAt == null)
            .ToListAsync(ct);

        foreach (var load in loads)
        {
            var mappedVoucherIds = voucherMappings.Where(x => x.ShipmentLoadId == load.ShipmentLoadId).Select(x => x.VoucherId).ToHashSet();
            var loadPackages = packages.Where(p => mappedVoucherIds.Contains(p.VoucherId)).ToList();
            var loadedPackageIds = packageMappings
                .Where(x => x.ShipmentLoadId == load.ShipmentLoadId && x.IsLoaded)
                .Select(x => x.OutboundPackageId)
                .ToHashSet();
            foreach (var package in loadPackages.Where(p => !loadedPackageIds.Contains(p.OutboundPackageId)))
            {
                var departed = load.Status is ShipmentLoadStatusEnum.Departed or ShipmentLoadStatusEnum.Closed;
                rows.Add(BuildPackageIssue(load, package, departed ? "departed_load_missing_package" : "load_package_scan_missing",
                    departed ? "Chuyến đã rời kho nhưng còn kiện chưa xếp" : "Kiện chưa quét lên chuyến",
                    departed ? "critical" : "warning",
                    departed
                        ? $"Chuyến {load.LoadCode} đã rời kho nhưng kiện {package.PackageCode} chưa có xác nhận xếp lên xe."
                        : $"Kiện {package.PackageCode} thuộc phiếu trong chuyến {load.LoadCode} nhưng chưa được quét lên chuyến.",
                    "Quét kiện lên chuyến hoặc gỡ phiếu/kiện khỏi chuyến trước khi bàn giao."));
            }
        }
    }

    private async Task AddPackageLoadMismatchIssuesAsync(List<DeliveryReconciliationRow> rows, int? warehouseId, CancellationToken ct)
    {
        var packageMappings = await _db.ShipmentLoadPackages.AsNoTracking()
            .Include(x => x.ShipmentLoad)!.ThenInclude(l => l!.Warehouse)
            .Include(x => x.OutboundPackage)!.ThenInclude(p => p!.Voucher)
            .Where(x => x.RemovedAt == null
                && x.ShipmentLoad != null
                && x.OutboundPackage != null
                && (!warehouseId.HasValue || x.ShipmentLoad.WarehouseId == warehouseId.Value))
            .ToListAsync(ct);
        var voucherIds = packageMappings.Select(x => x.OutboundPackage!.VoucherId).Distinct().ToList();
        var voucherLoadByVoucher = (await _db.ShipmentLoadVouchers.AsNoTracking()
                .Where(x => voucherIds.Contains(x.VoucherId) && x.RemovedAt == null)
                .ToListAsync(ct))
            .GroupBy(x => x.VoucherId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ShipmentLoadId).Distinct().ToList());

        foreach (var mapping in packageMappings)
        {
            var package = mapping.OutboundPackage!;
            if (voucherLoadByVoucher.TryGetValue(package.VoucherId, out var voucherLoadIds)
                && voucherLoadIds.Count > 0
                && !voucherLoadIds.Contains(mapping.ShipmentLoadId))
            {
                rows.Add(BuildPackageIssue(mapping.ShipmentLoad!, package, "package_voucher_load_mismatch", "Kiện và phiếu lệch chuyến", "error",
                    $"Kiện {package.PackageCode} nằm trên chuyến {mapping.ShipmentLoad!.LoadCode} nhưng phiếu thuộc chuyến khác.",
                    "Gỡ liên kết sai và quét lại kiện vào đúng chuyến xe."));
            }
        }
    }

    private async Task AddCarrierShipmentIssuesAsync(List<DeliveryReconciliationRow> rows, int? warehouseId, CancellationToken ct)
    {
        var shipments = await _db.CarrierShipments.AsNoTracking()
            .Include(s => s.Warehouse)
            .Include(s => s.Voucher)
            .Include(s => s.OutboundPackage)
            .Where(s => s.Voucher.ShippedAt.HasValue
                && (s.Status == CarrierShipmentStatusEnum.Cancelled || s.Status == CarrierShipmentStatusEnum.DeliveryFailed)
                && (!warehouseId.HasValue || s.WarehouseId == warehouseId.Value))
            .ToListAsync(ct);

        foreach (var shipment in shipments)
        {
            var cancelled = shipment.Status == CarrierShipmentStatusEnum.Cancelled;
            rows.Add(new DeliveryReconciliationRow
            {
                IssueType = cancelled ? "cancelled_carrier_shipped_voucher" : "delivery_failed_shipped_voucher",
                IssueLabel = cancelled ? "Vận đơn hủy nhưng phiếu đã giao" : "Hãng báo giao thất bại nhưng phiếu đã giao",
                Severity = "critical",
                WarehouseId = shipment.WarehouseId,
                WarehouseName = shipment.Warehouse?.WarehouseName ?? "",
                VoucherId = shipment.VoucherId,
                VoucherCode = shipment.Voucher?.VoucherCode,
                OutboundPackageId = shipment.OutboundPackageId,
                PackageCode = shipment.OutboundPackage?.PackageCode,
                CarrierShipmentId = shipment.CarrierShipmentId,
                TrackingNumber = shipment.TrackingNumber,
                Summary = cancelled
                    ? $"Vận đơn {shipment.TrackingNumber ?? shipment.CorrelationId} đã hủy nhưng phiếu {shipment.Voucher?.VoucherCode} đang ở trạng thái đã giao."
                    : $"Vận đơn {shipment.TrackingNumber ?? shipment.CorrelationId} báo giao thất bại nhưng phiếu {shipment.Voucher?.VoucherCode} đã giao.",
                Recommendation = "Kiểm tra lại với đơn vị vận chuyển và xử lý điều chỉnh nghiệp vụ nếu cần.",
                ActionUrl = $"/Operations/ShippingDispatch?search={Uri.EscapeDataString(shipment.TrackingNumber ?? shipment.CorrelationId)}"
            });
        }
    }

    private async Task AddCarrierRequirementIssuesAsync(List<DeliveryReconciliationRow> rows, int? warehouseId, CancellationToken ct)
    {
        var requiringConnectors = await _db.CarrierConnectors.AsNoTracking()
            .Where(c => c.IsActive && c.RequireShipmentCreatedBeforeShipping && (!warehouseId.HasValue || c.WarehouseId == warehouseId.Value))
            .Select(c => new { c.CarrierConnectorId, c.WarehouseId })
            .ToListAsync(ct);
        if (requiringConnectors.Count == 0)
            return;

        var connectorIds = requiringConnectors.Select(c => c.CarrierConnectorId).ToHashSet();
        var warehousesWithRequirement = requiringConnectors.Select(c => c.WarehouseId).ToHashSet();
        var packages = await _db.OutboundPackages.AsNoTracking()
            .Include(p => p.Warehouse)
            .Include(p => p.Voucher)
            .Where(p => p.Voucher != null
                && p.Voucher.PackedAt.HasValue
                && !p.Voucher.IsCancelled
                && warehousesWithRequirement.Contains(p.WarehouseId)
                && (!warehouseId.HasValue || p.WarehouseId == warehouseId.Value))
            .ToListAsync(ct);
        var packageIds = packages.Select(p => p.OutboundPackageId).ToList();
        var readyPackageIds = await _db.CarrierShipments.AsNoTracking()
            .Where(s => packageIds.Contains(s.OutboundPackageId)
                && connectorIds.Contains(s.CarrierConnectorId)
                && (s.Status == CarrierShipmentStatusEnum.Created || s.Status == CarrierShipmentStatusEnum.Delivered))
            .Select(s => s.OutboundPackageId)
            .Distinct()
            .ToListAsync(ct);
        var readySet = readyPackageIds.ToHashSet();

        foreach (var package in packages.Where(p => !readySet.Contains(p.OutboundPackageId)))
        {
            rows.Add(new DeliveryReconciliationRow
            {
                IssueType = "carrier_required_missing_shipment",
                IssueLabel = "Cấu hình yêu cầu vận đơn nhưng kiện chưa hợp lệ",
                Severity = package.Voucher?.ShippedAt.HasValue == true ? "error" : "warning",
                WarehouseId = package.WarehouseId,
                WarehouseName = package.Warehouse?.WarehouseName ?? "",
                VoucherId = package.VoucherId,
                VoucherCode = package.Voucher?.VoucherCode,
                OutboundPackageId = package.OutboundPackageId,
                PackageCode = package.PackageCode,
                TrackingNumber = package.TrackingNumber ?? package.Voucher?.TrackingNumber,
                Summary = $"Kiện {package.PackageCode} thuộc kho yêu cầu vận đơn trước giao nhưng chưa có vận đơn ở trạng thái hợp lệ.",
                Recommendation = "Tạo vận đơn hoặc xử lý lỗi vận đơn trên bảng điều phối vận chuyển.",
                ActionUrl = $"/Operations/ShippingDispatch?search={Uri.EscapeDataString(package.PackageCode)}"
            });
        }
    }

    private static DeliveryReconciliationRow BuildVoucherIssue(Voucher voucher, string issueType, string label, string severity, string summary, string recommendation)
        => new()
        {
            IssueType = issueType,
            IssueLabel = label,
            Severity = severity,
            WarehouseId = voucher.WarehouseId,
            WarehouseName = voucher.Warehouse?.WarehouseName ?? "",
            VoucherId = voucher.VoucherId,
            VoucherCode = voucher.VoucherCode,
            TrackingNumber = voucher.TrackingNumber,
            Summary = summary,
            Recommendation = recommendation,
            ActionUrl = $"/Vouchers/Details/{voucher.VoucherId}"
        };

    private static DeliveryReconciliationRow BuildPackageIssue(ShipmentLoad load, OutboundPackage package, string issueType, string label, string severity, string summary, string recommendation)
        => new()
        {
            IssueType = issueType,
            IssueLabel = label,
            Severity = severity,
            WarehouseId = load.WarehouseId,
            WarehouseName = load.Warehouse?.WarehouseName ?? "",
            VoucherId = package.VoucherId,
            VoucherCode = package.Voucher?.VoucherCode,
            OutboundPackageId = package.OutboundPackageId,
            PackageCode = package.PackageCode,
            ShipmentLoadId = load.ShipmentLoadId,
            LoadCode = load.LoadCode,
            TrackingNumber = package.TrackingNumber ?? package.Voucher?.TrackingNumber,
            Summary = summary,
            Recommendation = recommendation,
            ActionUrl = $"/Operations/ShipmentLoadDetails/{load.ShipmentLoadId}"
        };

    private static bool Contains(string? value, string keyword)
        => value?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true;

    private static int SeverityRank(string severity) => severity switch
    {
        "critical" => 3,
        "error" => 2,
        "warning" => 1,
        _ => 0
    };
}
