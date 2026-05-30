using System.Data;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public sealed class ShipmentLoadCreateRequest
{
    public int WarehouseId { get; init; }
    public string? LoadCode { get; init; }
    public string? CarrierName { get; init; }
    public string? RouteCode { get; init; }
    public string? RouteName { get; init; }
    public string? VehicleNumber { get; init; }
    public int? TrailerId { get; init; }
    public long? YardVisitId { get; init; }
    public string? DockDoor { get; init; }
    public DateTime? PlannedDepartureAt { get; init; }
    public string? SealNumber { get; init; }
    public string? ManifestCode { get; init; }
    public string? TmsReferenceNo { get; init; }
    public string? Notes { get; init; }
}

public interface IShipmentLoadService
{
    Task<ShipmentLoad> CreateAsync(ShipmentLoadCreateRequest request, int? scopedWarehouseId, string actor, CancellationToken ct = default);
    Task<ShipmentLoad> AddVoucherAsync(long loadId, long voucherId, int? stopNumber, int? scopedWarehouseId, string actor, CancellationToken ct = default);
    Task<ShipmentLoad> RemoveVoucherAsync(long loadId, long voucherId, int? scopedWarehouseId, string actor, CancellationToken ct = default);
    Task<ShipmentLoad> AddPackageByScanAsync(long loadId, string packageOrLpnCode, int? scopedWarehouseId, string actor, CancellationToken ct = default);
    Task<ShipmentLoad> MarkStatusAsync(long loadId, ShipmentLoadStatusEnum status, int? scopedWarehouseId, string actor, CancellationToken ct = default);
    Task<ShipmentLoad> DepartAsync(long loadId, string? trackingNumber, string? manifestCode, string? note, int? scopedWarehouseId, string actor, CancellationToken ct = default);
    Task<ShipmentLoad> CancelAsync(long loadId, int? scopedWarehouseId, string actor, CancellationToken ct = default);
}

public class ShipmentLoadService : IShipmentLoadService
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICatchWeightService _catchWeightService;
    private readonly ICarrierIntegrationService? _carrierIntegrationService;

    public ShipmentLoadService(
        AppDbContext db,
        IUnitOfWork? unitOfWork = null,
        ICatchWeightService? catchWeightService = null,
        ICarrierIntegrationService? carrierIntegrationService = null)
    {
        _db = db;
        _unitOfWork = unitOfWork ?? new EfUnitOfWork(db);
        _catchWeightService = catchWeightService ?? new CatchWeightService(db);
        _carrierIntegrationService = carrierIntegrationService;
    }

    private static DateTime Now => VietnamTime.Now;

    public async Task<ShipmentLoad> CreateAsync(ShipmentLoadCreateRequest request, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        EnsureWarehouseScope(request.WarehouseId, scopedWarehouseId);
        var warehouseExists = await _db.Warehouses.AnyAsync(w => w.WarehouseId == request.WarehouseId && w.IsActive, ct);
        if (!warehouseExists)
            throw new BusinessRuleException("Kho không hoạt động hoặc không tồn tại.", "SHIPMENT_LOAD_WAREHOUSE_INVALID", "Warehouse");

        if (request.YardVisitId.HasValue)
        {
            var yardVisit = await _db.YardVisits.AsNoTracking()
                .FirstOrDefaultAsync(v => v.YardVisitId == request.YardVisitId.Value, ct);
            if (yardVisit == null || yardVisit.WarehouseId != request.WarehouseId || yardVisit.Status is YardVisitStatusEnum.Cancelled or YardVisitStatusEnum.GatedOut)
                throw new BusinessRuleException("Lượt yard visit không hợp lệ cho chuyến xe.", "SHIPMENT_LOAD_YARD_VISIT_INVALID", "YardVisit");
        }

        var loadCode = NormalizeOptional(request.LoadCode, 40) ?? await GenerateLoadCodeAsync(request.WarehouseId, ct);
        var duplicate = await _db.ShipmentLoads.AnyAsync(l => l.LoadCode == loadCode, ct);
        if (duplicate)
            throw new BusinessRuleException($"Mã chuyến [{loadCode}] đã tồn tại.", "SHIPMENT_LOAD_DUPLICATE", "ShipmentLoad");

        var load = new ShipmentLoad
        {
            LoadCode = loadCode,
            WarehouseId = request.WarehouseId,
            Status = ShipmentLoadStatusEnum.Planned,
            CarrierName = NormalizeOptional(request.CarrierName, 100),
            RouteCode = NormalizeOptional(request.RouteCode, 100),
            RouteName = NormalizeOptional(request.RouteName, 100),
            VehicleNumber = NormalizeOptional(request.VehicleNumber, 30),
            TrailerId = request.TrailerId,
            YardVisitId = request.YardVisitId,
            DockDoor = NormalizeOptional(request.DockDoor, 20),
            PlannedDepartureAt = request.PlannedDepartureAt,
            SealNumber = NormalizeOptional(request.SealNumber, 50),
            ManifestCode = NormalizeOptional(request.ManifestCode, 50),
            TmsReferenceNo = NormalizeOptional(request.TmsReferenceNo, 120),
            Notes = NormalizeOptional(request.Notes, 500),
            CreatedBy = NormalizeActor(actor),
            CreatedAt = Now
        };

        _db.ShipmentLoads.Add(load);
        await _unitOfWork.SaveChangesAsync(ct);
        return load;
    }

    public async Task<ShipmentLoad> AddVoucherAsync(long loadId, long voucherId, int? stopNumber, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        var load = await LoadHeaderAsync(loadId, scopedWarehouseId, ct);
        EnsureEditable(load);

        var voucher = await _db.Vouchers
            .Include(v => v.Packages)
            .FirstOrDefaultAsync(v => v.VoucherId == voucherId, ct);
        ValidateVoucherForLoad(load, voucher);

        var activeElsewhere = await _db.ShipmentLoadVouchers
            .Include(x => x.ShipmentLoad)
            .AnyAsync(x => x.VoucherId == voucherId
                && x.RemovedAt == null
                && x.ShipmentLoadId != loadId
                && x.ShipmentLoad != null
                && x.ShipmentLoad.Status != ShipmentLoadStatusEnum.Cancelled
                && x.ShipmentLoad.Status != ShipmentLoadStatusEnum.Closed, ct);
        if (activeElsewhere)
            throw new BusinessRuleException($"Phiếu [{voucher!.VoucherCode}] đã thuộc một chuyến xe khác.", "SHIPMENT_LOAD_VOUCHER_DUPLICATE", "ShipmentLoadVoucher");

        var existing = await _db.ShipmentLoadVouchers
            .FirstOrDefaultAsync(x => x.ShipmentLoadId == loadId && x.VoucherId == voucherId && x.RemovedAt == null, ct);
        if (existing == null)
        {
            var nextSeq = await _db.ShipmentLoadVouchers
                .Where(x => x.ShipmentLoadId == loadId)
                .Select(x => (int?)x.Sequence)
                .MaxAsync(ct) ?? 0;
            _db.ShipmentLoadVouchers.Add(new ShipmentLoadVoucher
            {
                ShipmentLoadId = loadId,
                VoucherId = voucherId,
                Sequence = nextSeq + 1,
                StopNumber = stopNumber,
                StatusSnapshot = voucher!.FulfillmentStatus.ToString(),
                AddedBy = NormalizeActor(actor),
                AddedAt = Now
            });
        }

        await RefreshTotalsAsync(load, ct);
        load.UpdatedAt = Now;
        load.UpdatedBy = NormalizeActor(actor);
        await _unitOfWork.SaveChangesAsync(ct);
        return load;
    }

    public async Task<ShipmentLoad> RemoveVoucherAsync(long loadId, long voucherId, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        var load = await LoadHeaderAsync(loadId, scopedWarehouseId, ct);
        EnsureEditable(load);

        var mapping = await _db.ShipmentLoadVouchers
            .FirstOrDefaultAsync(x => x.ShipmentLoadId == loadId && x.VoucherId == voucherId && x.RemovedAt == null, ct);
        if (mapping == null)
            return load;

        var now = Now;
        mapping.RemovedAt = now;
        mapping.RemovedBy = NormalizeActor(actor);

        var packageMappings = await _db.ShipmentLoadPackages
            .Include(x => x.OutboundPackage)
            .Where(x => x.ShipmentLoadId == loadId
                && x.RemovedAt == null
                && x.OutboundPackage != null
                && x.OutboundPackage.VoucherId == voucherId)
            .ToListAsync(ct);
        foreach (var packageMapping in packageMappings)
        {
            packageMapping.RemovedAt = now;
            packageMapping.RemovedBy = NormalizeActor(actor);
            if (packageMapping.OutboundPackage != null)
            {
                packageMapping.OutboundPackage.ShipmentLoadId = null;
                packageMapping.OutboundPackage.LoadedAt = null;
                packageMapping.OutboundPackage.LoadedBy = null;
            }
        }

        await RefreshTotalsAsync(load, ct);
        load.UpdatedAt = now;
        load.UpdatedBy = NormalizeActor(actor);
        await _unitOfWork.SaveChangesAsync(ct);
        return load;
    }

    public async Task<ShipmentLoad> AddPackageByScanAsync(long loadId, string packageOrLpnCode, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        var load = await LoadHeaderAsync(loadId, scopedWarehouseId, ct);
        EnsureEditable(load);
        if (load.Status == ShipmentLoadStatusEnum.Planned)
            load.Status = ShipmentLoadStatusEnum.Loading;

        var code = NormalizeRequiredCode(packageOrLpnCode, 40, "mã kiện");
        var package = await _db.OutboundPackages
            .Include(p => p.Voucher)
            .FirstOrDefaultAsync(p => p.PackageCode == code || p.ReferenceLpnCode == code, ct);
        if (package == null)
            throw new BusinessRuleException($"Không tìm thấy kiện [{code}].", "SHIPMENT_LOAD_PACKAGE_NOT_FOUND", "OutboundPackage");
        if (package.WarehouseId != load.WarehouseId)
            throw new BusinessRuleException("Kiện không thuộc cùng kho với chuyến xe.", "SHIPMENT_LOAD_PACKAGE_WAREHOUSE_MISMATCH", "OutboundPackage");

        var voucherMapped = await _db.ShipmentLoadVouchers.AnyAsync(x =>
            x.ShipmentLoadId == loadId
            && x.VoucherId == package.VoucherId
            && x.RemovedAt == null, ct);
        if (!voucherMapped)
            throw new BusinessRuleException("Kiện thuộc phiếu chưa được thêm vào chuyến xe.", "SHIPMENT_LOAD_PACKAGE_VOUCHER_MISSING", "ShipmentLoadVoucher");

        var activeElsewhere = await _db.ShipmentLoadPackages
            .Include(x => x.ShipmentLoad)
            .AnyAsync(x => x.OutboundPackageId == package.OutboundPackageId
                && x.RemovedAt == null
                && x.ShipmentLoadId != loadId
                && x.ShipmentLoad != null
                && x.ShipmentLoad.Status != ShipmentLoadStatusEnum.Cancelled
                && x.ShipmentLoad.Status != ShipmentLoadStatusEnum.Closed, ct);
        if (activeElsewhere)
            throw new BusinessRuleException($"Kiện [{package.PackageCode}] đã thuộc chuyến xe khác.", "SHIPMENT_LOAD_PACKAGE_DUPLICATE", "ShipmentLoadPackage");

        var now = Now;
        var existing = await _db.ShipmentLoadPackages
            .FirstOrDefaultAsync(x => x.ShipmentLoadId == loadId && x.OutboundPackageId == package.OutboundPackageId && x.RemovedAt == null, ct);
        if (existing == null)
        {
            existing = new ShipmentLoadPackage
            {
                ShipmentLoadId = loadId,
                OutboundPackageId = package.OutboundPackageId,
                PackageCodeSnapshot = package.PackageCode,
                ReferenceLpnCode = package.ReferenceLpnCode,
                AddedBy = NormalizeActor(actor),
                AddedAt = now
            };
            _db.ShipmentLoadPackages.Add(existing);
        }

        existing.IsLoaded = true;
        existing.LoadedBy = NormalizeActor(actor);
        existing.LoadedAt = now;
        package.ShipmentLoadId = loadId;
        package.LoadedBy = NormalizeActor(actor);
        package.LoadedAt = now;

        await RefreshTotalsAsync(load, ct);
        load.UpdatedAt = now;
        load.UpdatedBy = NormalizeActor(actor);
        await _unitOfWork.SaveChangesAsync(ct);
        return load;
    }

    public async Task<ShipmentLoad> MarkStatusAsync(long loadId, ShipmentLoadStatusEnum status, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        var load = await LoadHeaderAsync(loadId, scopedWarehouseId, ct);
        if (status is ShipmentLoadStatusEnum.Departed or ShipmentLoadStatusEnum.Cancelled)
            throw new BusinessRuleException("Vui lòng dùng thao tác xác nhận rời kho hoặc hủy chuyên biệt cho chuyến xe.", "SHIPMENT_LOAD_STATUS_ACTION_INVALID", "ShipmentLoad");
        if (load.Status is ShipmentLoadStatusEnum.Closed or ShipmentLoadStatusEnum.Cancelled
            || (load.Status == ShipmentLoadStatusEnum.Departed && status != ShipmentLoadStatusEnum.Closed))
            throw new BusinessRuleException("Chuyến xe đã kết thúc, không thể đổi trạng thái.", "SHIPMENT_LOAD_CLOSED", "ShipmentLoad");
        if (status == ShipmentLoadStatusEnum.Closed && load.Status != ShipmentLoadStatusEnum.Departed)
            throw new BusinessRuleException("Chỉ được đóng chuyến sau khi đã xác nhận rời kho.", "SHIPMENT_LOAD_CLOSE_REQUIRES_DEPART", "ShipmentLoad");

        load.Status = status;
        load.UpdatedAt = Now;
        load.UpdatedBy = NormalizeActor(actor);
        if (status == ShipmentLoadStatusEnum.Closed)
        {
            load.ClosedAt = Now;
            load.ClosedBy = NormalizeActor(actor);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return load;
    }

    public async Task<ShipmentLoad> DepartAsync(long loadId, string? trackingNumber, string? manifestCode, string? note, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        var load = await _db.ShipmentLoads
            .Include(l => l.YardVisit)!.ThenInclude(v => v!.CurrentSpot)
            .FirstOrDefaultAsync(l => l.ShipmentLoadId == loadId, ct);
        if (load == null)
            throw new BusinessRuleException("Không tìm thấy chuyến xe.", "SHIPMENT_LOAD_NOT_FOUND", "ShipmentLoad");
        EnsureWarehouseScope(load.WarehouseId, scopedWarehouseId);
        if (load.Status is ShipmentLoadStatusEnum.Departed or ShipmentLoadStatusEnum.Closed)
            return load;
        EnsureEditable(load);

        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var voucherMappings = await _db.ShipmentLoadVouchers
                .Where(x => x.ShipmentLoadId == loadId && x.RemovedAt == null)
                .ToListAsync(ct);
            if (voucherMappings.Count == 0)
                throw new BusinessRuleException("Chuyến xe chưa có phiếu xuất.", "SHIPMENT_LOAD_EMPTY", "ShipmentLoadVoucher");

            var voucherIds = voucherMappings.Select(x => x.VoucherId).ToList();
            var vouchers = await _db.Vouchers
                .Where(v => voucherIds.Contains(v.VoucherId))
                .ToListAsync(ct);
            foreach (var voucher in vouchers)
            {
                ValidateVoucherForDepart(voucher);
                await _catchWeightService.RequirePackageCatchWeightAsync(voucher.VoucherId, ct);
            }

            var packages = await _db.OutboundPackages
                .Where(p => voucherIds.Contains(p.VoucherId))
                .ToListAsync(ct);
            if (packages.Count == 0)
                throw new BusinessRuleException("Chuyến xe không có kiện để bàn giao.", "SHIPMENT_LOAD_NO_PACKAGES", "OutboundPackage");

            var packageIds = packages.Select(p => p.OutboundPackageId).ToList();
            var loadedPackageIds = await _db.ShipmentLoadPackages
                .Where(x => x.ShipmentLoadId == loadId
                    && x.RemovedAt == null
                    && x.IsLoaded
                    && packageIds.Contains(x.OutboundPackageId))
                .Select(x => x.OutboundPackageId)
                .ToListAsync(ct);
            var loadedSet = loadedPackageIds.ToHashSet();
            var missingPackages = packages.Where(p => !loadedSet.Contains(p.OutboundPackageId)).Select(p => p.PackageCode).ToList();
            if (missingPackages.Count > 0)
                throw new BusinessRuleException($"Chưa quét đủ kiện lên chuyến: {string.Join(", ", missingPackages.Take(10))}.", "SHIPMENT_LOAD_PACKAGE_SCAN_MISSING", "ShipmentLoadPackage");

            if (_carrierIntegrationService != null)
            {
                foreach (var voucherId in voucherIds)
                    await _carrierIntegrationService.EnsureShipmentReadyForShippingAsync(voucherId, ct);
            }

            var now = Now;
            var actorName = NormalizeActor(actor);
            var normalizedTracking = NormalizeOptional(trackingNumber, 100) ?? load.TrackingNumber;
            var normalizedManifest = NormalizeOptional(manifestCode, 50) ?? load.ManifestCode ?? load.LoadCode;

            load.Status = ShipmentLoadStatusEnum.Departed;
            load.ActualDepartureAt = now;
            load.DepartedAt = now;
            load.DepartedBy = actorName;
            load.TrackingNumber = normalizedTracking;
            load.ManifestCode = normalizedManifest;
            load.UpdatedAt = now;
            load.UpdatedBy = actorName;

            foreach (var voucher in vouchers)
            {
                voucher.ShippedBy = actorName;
                voucher.ShippedAt = now;
                voucher.TrackingNumber = normalizedTracking;
                voucher.ManifestCode = normalizedManifest;
                voucher.FulfillmentStatus = FulfillmentStatusEnum.Shipped;
                voucher.UpdatedAt = now;

                var logExists = await _db.ShippingHandoverLogs.AnyAsync(h =>
                    h.VoucherId == voucher.VoucherId && h.ShipmentLoadId == load.ShipmentLoadId, ct);
                if (!logExists)
                {
                    _db.ShippingHandoverLogs.Add(new ShippingHandoverLog
                    {
                        VoucherId = voucher.VoucherId,
                        WarehouseId = voucher.WarehouseId,
                        ShipmentLoadId = load.ShipmentLoadId,
                        HandedOverBy = actorName,
                        HandedOverAt = now,
                        TrackingNumber = normalizedTracking,
                        ManifestCode = normalizedManifest,
                        CarrierName = load.CarrierName ?? voucher.CarrierName,
                        VehicleNumber = load.VehicleNumber ?? voucher.VehicleNumber,
                        DriverName = voucher.DriverName,
                        DriverPhone = voucher.DriverPhone,
                        Notes = NormalizeOptional(note, 500)
                    });
                }
            }

            foreach (var package in packages)
            {
                package.ShipmentLoadId = load.ShipmentLoadId;
                package.TrackingNumber = normalizedTracking;
                package.ManifestCode = normalizedManifest;
                package.LoadedAt ??= now;
                package.LoadedBy ??= actorName;
            }

            SyncYardVisitOnDepart(load, actorName, now);
            await RefreshTotalsAsync(load, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitAsync(ct);
            return load;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<ShipmentLoad> CancelAsync(long loadId, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        var load = await LoadHeaderAsync(loadId, scopedWarehouseId, ct);
        if (load.Status is ShipmentLoadStatusEnum.Departed or ShipmentLoadStatusEnum.Closed)
            throw new BusinessRuleException("Chuyến xe đã rời kho hoặc đã đóng, không thể hủy.", "SHIPMENT_LOAD_CANCEL_AFTER_DEPART", "ShipmentLoad");
        if (load.Status == ShipmentLoadStatusEnum.Cancelled)
            return load;

        var now = Now;
        var actorName = NormalizeActor(actor);
        load.Status = ShipmentLoadStatusEnum.Cancelled;
        load.CancelledAt = now;
        load.CancelledBy = actorName;
        load.UpdatedAt = now;
        load.UpdatedBy = actorName;

        var voucherMappings = await _db.ShipmentLoadVouchers
            .Where(x => x.ShipmentLoadId == loadId && x.RemovedAt == null)
            .ToListAsync(ct);
        foreach (var mapping in voucherMappings)
        {
            mapping.RemovedAt = now;
            mapping.RemovedBy = actorName;
        }

        var packageMappings = await _db.ShipmentLoadPackages
            .Include(x => x.OutboundPackage)
            .Where(x => x.ShipmentLoadId == loadId && x.RemovedAt == null)
            .ToListAsync(ct);
        foreach (var mapping in packageMappings)
        {
            mapping.RemovedAt = now;
            mapping.RemovedBy = actorName;
            if (mapping.OutboundPackage != null)
            {
                mapping.OutboundPackage.ShipmentLoadId = null;
                mapping.OutboundPackage.LoadedAt = null;
                mapping.OutboundPackage.LoadedBy = null;
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return load;
    }

    private async Task<ShipmentLoad> LoadHeaderAsync(long loadId, int? scopedWarehouseId, CancellationToken ct)
    {
        var load = await _db.ShipmentLoads.FirstOrDefaultAsync(l => l.ShipmentLoadId == loadId, ct);
        if (load == null)
            throw new BusinessRuleException("Không tìm thấy chuyến xe.", "SHIPMENT_LOAD_NOT_FOUND", "ShipmentLoad");
        EnsureWarehouseScope(load.WarehouseId, scopedWarehouseId);
        return load;
    }

    private async Task RefreshTotalsAsync(ShipmentLoad load, CancellationToken ct)
    {
        var voucherIds = await _db.ShipmentLoadVouchers
            .Where(x => x.ShipmentLoadId == load.ShipmentLoadId && x.RemovedAt == null)
            .Select(x => x.VoucherId)
            .ToListAsync(ct);
        var activeVoucherIds = voucherIds.ToHashSet();
        foreach (var entry in _db.ChangeTracker.Entries<ShipmentLoadVoucher>()
            .Where(e => e.Entity.ShipmentLoadId == load.ShipmentLoadId))
        {
            var mapping = entry.Entity;
            if (entry.State == EntityState.Added && mapping.RemovedAt == null)
            {
                activeVoucherIds.Add(mapping.VoucherId);
            }
            else if (entry.State == EntityState.Deleted || mapping.RemovedAt != null)
            {
                activeVoucherIds.Remove(mapping.VoucherId);
            }
        }

        voucherIds = activeVoucherIds.ToList();
        load.TotalVoucherCount = voucherIds.Count;

        var packages = voucherIds.Count == 0
            ? new List<OutboundPackage>()
            : await _db.OutboundPackages
                .Where(p => voucherIds.Contains(p.VoucherId))
                .ToListAsync(ct);
        load.TotalPackageCount = packages.Count;
        load.TotalQuantity = packages.Sum(p => p.TotalQuantity ?? 0m);
        var weight = packages.Where(p => p.ActualCatchWeight.HasValue).Sum(p => p.ActualCatchWeight!.Value);
        load.TotalCatchWeight = weight > 0 ? weight : null;
    }

    private async Task<string> GenerateLoadCodeAsync(int warehouseId, CancellationToken ct)
    {
        var prefix = $"LOAD-{Now:yyyyMMdd}-{warehouseId}-";
        var count = await _db.ShipmentLoads.CountAsync(l => l.LoadCode.StartsWith(prefix), ct);
        return $"{prefix}{count + 1:0000}";
    }

    private static void ValidateVoucherForLoad(ShipmentLoad load, Voucher? voucher)
    {
        if (voucher == null)
            throw WmsExceptions.VoucherNotFound();
        if (voucher.WarehouseId != load.WarehouseId)
            throw new BusinessRuleException("Phiếu không cùng kho với chuyến xe.", "SHIPMENT_LOAD_VOUCHER_WAREHOUSE_MISMATCH", "Voucher");
        if (voucher.IsCancelled)
            throw new BusinessRuleException("Không thể thêm phiếu đã hủy vào chuyến xe.", "SHIPMENT_LOAD_VOUCHER_CANCELLED", "Voucher");
        if (!IsOutboundVoucher(voucher.VoucherType))
            throw new BusinessRuleException("Chỉ được thêm phiếu xuất/chuyển kho vào chuyến xe.", "SHIPMENT_LOAD_VOUCHER_TYPE_INVALID", "Voucher");
        if (!voucher.IsPosted || !voucher.PackedAt.HasValue || voucher.FulfillmentStatus < FulfillmentStatusEnum.Packed)
            throw new BusinessRuleException("Phiếu phải đã chốt xuất và đóng gói trước khi thêm vào chuyến xe.", "SHIPMENT_LOAD_VOUCHER_NOT_PACKED", "Voucher");
        if (voucher.ShippedAt.HasValue || voucher.FulfillmentStatus == FulfillmentStatusEnum.Shipped)
            throw new BusinessRuleException("Phiếu đã giao hàng, không thể thêm vào chuyến xe.", "SHIPMENT_LOAD_VOUCHER_SHIPPED", "Voucher");
    }

    private static void ValidateVoucherForDepart(Voucher voucher)
    {
        if (voucher.IsCancelled)
            throw new BusinessRuleException($"Phiếu [{voucher.VoucherCode}] đã hủy.", "SHIPMENT_LOAD_DEPART_VOUCHER_CANCELLED", "Voucher");
        if (!voucher.IsPosted || !voucher.PackedAt.HasValue)
            throw new BusinessRuleException($"Phiếu [{voucher.VoucherCode}] chưa đóng gói.", "SHIPMENT_LOAD_DEPART_VOUCHER_NOT_PACKED", "Voucher");
        if (voucher.ShippedAt.HasValue)
            throw new BusinessRuleException($"Phiếu [{voucher.VoucherCode}] đã giao hàng.", "SHIPMENT_LOAD_DEPART_VOUCHER_SHIPPED", "Voucher");
    }

    private static bool IsOutboundVoucher(VoucherTypeEnum voucherType)
        => voucherType is VoucherTypeEnum.XuatKho or VoucherTypeEnum.TraNCC or VoucherTypeEnum.ChuyenKho or VoucherTypeEnum.XuatSanXuat;

    private static void EnsureEditable(ShipmentLoad load)
    {
        if (load.Status is ShipmentLoadStatusEnum.Departed or ShipmentLoadStatusEnum.Closed or ShipmentLoadStatusEnum.Cancelled)
            throw new BusinessRuleException("Chuyến xe đã kết thúc, không thể chỉnh.", "SHIPMENT_LOAD_NOT_EDITABLE", "ShipmentLoad");
    }

    private static void EnsureWarehouseScope(int warehouseId, int? scopedWarehouseId)
    {
        if (scopedWarehouseId.HasValue && warehouseId != scopedWarehouseId.Value)
            throw new UnauthorizedAccessException("Bạn không thể thao tác chuyến xe của kho khác.");
    }

    private static void SyncYardVisitOnDepart(ShipmentLoad load, string actor, DateTime now)
    {
        var visit = load.YardVisit;
        if (visit == null || visit.Status is YardVisitStatusEnum.Cancelled or YardVisitStatusEnum.GatedOut)
            return;

        visit.Status = YardVisitStatusEnum.GatedOut;
        visit.GateOutAt = now;
        visit.GateOutBy = actor;
        visit.UpdatedAt = now;
        if (!string.IsNullOrWhiteSpace(load.DockDoor))
            visit.DockDoor = load.DockDoor;
        if (visit.CurrentSpot != null)
        {
            visit.CurrentSpot.Status = YardSpotStatusEnum.Available;
            visit.CurrentSpot.UpdatedAt = now;
        }
    }

    private static string NormalizeActor(string? value)
        => NormalizeOptional(value, 100) ?? "system";

    private static string NormalizeRequiredCode(string? value, int maxLength, string fieldName)
    {
        var normalized = NormalizeOptional(value, maxLength)?.ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new BusinessRuleException($"Vui lòng nhập {fieldName}.", "SHIPMENT_LOAD_CODE_REQUIRED", "ShipmentLoad");
        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
