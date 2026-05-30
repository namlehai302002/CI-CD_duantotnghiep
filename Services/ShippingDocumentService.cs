using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;
using WMS.ViewModels;

namespace WMS.Services;

public interface IShippingDocumentService
{
    Task<LabelPrintJob> CreatePackageShippingLabelAsync(long outboundPackageId, string actor, CancellationToken ct = default);
    Task<LabelPrintJob> CreateShipmentLoadPackageLabelsAsync(long shipmentLoadId, string actor, CancellationToken ct = default);
    Task<LabelPrintJob> CreateShipmentLoadManifestAsync(long shipmentLoadId, string actor, CancellationToken ct = default);
    Task<LabelPrintJob> CreateShipmentLoadHandoverAsync(long shipmentLoadId, string actor, CancellationToken ct = default);
    Task<LabelPrintJob> CreateDirectHandoverAsync(long shippingHandoverLogId, string actor, CancellationToken ct = default);
    Task<ShippingDocumentPrintViewModel> BuildPrintViewModelAsync(long printJobId, string actor, bool markPrinted, CancellationToken ct = default);
}

public class ShippingDocumentService : IShippingDocumentService
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ShippingDocumentService(AppDbContext db, IUnitOfWork? unitOfWork = null)
    {
        _db = db;
        _unitOfWork = unitOfWork ?? new EfUnitOfWork(db);
    }

    private static DateTime Now => VietnamTime.Now;

    public async Task<LabelPrintJob> CreatePackageShippingLabelAsync(long outboundPackageId, string actor, CancellationToken ct = default)
    {
        var package = await _db.OutboundPackages
            .Include(p => p.Voucher)!.ThenInclude(v => v!.Partner)
            .Include(p => p.Warehouse)
            .Include(p => p.ShipmentLoad)
            .FirstOrDefaultAsync(p => p.OutboundPackageId == outboundPackageId, ct)
            ?? throw new BusinessRuleException("Không tìm thấy kiện xuất cần in nhãn vận chuyển.", "SHIP_DOC_PACKAGE_NOT_FOUND", "OutboundPackage");

        if (package.Voucher == null)
            throw new BusinessRuleException("Kiện xuất chưa liên kết phiếu hợp lệ.", "SHIP_DOC_PACKAGE_VOUCHER_MISSING", "OutboundPackage");

        var carrierShipment = await ResolveCarrierShipmentAsync(package.OutboundPackageId, ct);
        var row = BuildPackageLabelRow(package, carrierShipment);
        var payload = new ShippingDocumentPrintViewModel
        {
            DocumentType = "PackageShippingLabel",
            DocumentTitle = "Nhãn vận chuyển",
            DocumentNumber = row.TrackingNumber ?? row.PackageCode,
            WarehouseName = package.Warehouse?.WarehouseName,
            CarrierName = row.CarrierName,
            LoadCode = row.LoadCode,
            ManifestCode = row.ManifestCode,
            TrackingNumber = row.TrackingNumber,
            PackageLabels = new List<ShippingPackageLabelRow> { row }
        };

        var job = CreateDocumentJob(package.Voucher, LabelPurposeEnum.ShippingPackageLabel, "PackageShippingLabel", payload.DocumentNumber, actor);
        job.OutboundPackageId = package.OutboundPackageId;
        job.ShipmentLoadId = package.ShipmentLoadId;
        job.CarrierShipmentId = carrierShipment?.CarrierShipmentId;
        job.TotalLabels = 1;
        job.LabelSize = "100x50";
        job.CodeType = "qr";
        job.SourceDescription = $"Nhãn vận chuyển kiện {package.PackageCode}";
        job.MetadataJson = JsonSerializer.Serialize(payload, JsonOptions);
        await PersistJobAsync(job, ct);
        return job;
    }

    public async Task<LabelPrintJob> CreateShipmentLoadPackageLabelsAsync(long shipmentLoadId, string actor, CancellationToken ct = default)
    {
        var load = await LoadShipmentLoadAsync(shipmentLoadId, ct);
        var packages = await LoadPackagesForLoadAsync(shipmentLoadId, ct);
        if (packages.Count == 0)
            throw new BusinessRuleException("Chuyến xe chưa có kiện để in nhãn vận chuyển.", "SHIP_DOC_LOAD_NO_PACKAGES", "ShipmentLoad");

        var carrierShipments = await LoadCarrierShipmentsByPackageAsync(packages.Select(p => p.OutboundPackageId), ct);
        var rows = packages.Select(p => BuildPackageLabelRow(p, carrierShipments.GetValueOrDefault(p.OutboundPackageId))).ToList();
        var payload = BuildLoadPayload(load, "ShipmentLoadPackageLabels", "Nhãn vận chuyển theo chuyến", rows, new(), new(), new());

        var job = CreateDocumentJob(ResolveFirstVoucher(packages), LabelPurposeEnum.ShippingPackageLabel, "ShipmentLoadPackageLabels", load.LoadCode, actor);
        job.ShipmentLoadId = load.ShipmentLoadId;
        job.TotalLabels = rows.Count;
        job.LabelSize = "100x50";
        job.CodeType = "qr";
        job.SourceDescription = $"Nhãn vận chuyển chuyến {load.LoadCode}";
        job.MetadataJson = JsonSerializer.Serialize(payload, JsonOptions);
        await PersistJobAsync(job, ct);
        return job;
    }

    public async Task<LabelPrintJob> CreateShipmentLoadManifestAsync(long shipmentLoadId, string actor, CancellationToken ct = default)
    {
        var load = await LoadShipmentLoadAsync(shipmentLoadId, ct);
        var packages = await LoadPackagesForLoadAsync(shipmentLoadId, ct);
        if (packages.Count == 0)
            throw new BusinessRuleException("Chuyến xe chưa có kiện để in bản kê.", "SHIP_DOC_LOAD_NO_PACKAGES", "ShipmentLoad");

        var voucherRows = BuildManifestVoucherRows(packages);
        var packageRows = BuildManifestPackageRows(packages, await LoadCarrierShipmentsByPackageAsync(packages.Select(p => p.OutboundPackageId), ct));
        var payload = BuildLoadPayload(load, "ShipmentLoadManifest", "Bản kê chuyến xe", new(), voucherRows, packageRows, new());

        var job = CreateDocumentJob(ResolveFirstVoucher(packages), LabelPurposeEnum.ShipmentLoadManifest, "ShipmentLoadManifest", load.ManifestCode ?? load.LoadCode, actor);
        job.ShipmentLoadId = load.ShipmentLoadId;
        job.TotalLabels = 1;
        job.LabelSize = "a4grid";
        job.CodeType = "qr";
        job.SourceDescription = $"Bản kê chuyến xe {load.LoadCode}";
        job.MetadataJson = JsonSerializer.Serialize(payload, JsonOptions);
        await PersistJobAsync(job, ct);
        return job;
    }

    public async Task<LabelPrintJob> CreateShipmentLoadHandoverAsync(long shipmentLoadId, string actor, CancellationToken ct = default)
    {
        var load = await LoadShipmentLoadAsync(shipmentLoadId, ct);
        var handovers = await _db.ShippingHandoverLogs
            .AsNoTracking()
            .Include(h => h.Voucher)!.ThenInclude(v => v!.Partner)
            .Where(h => h.ShipmentLoadId == shipmentLoadId)
            .OrderBy(h => h.HandedOverAt)
            .ToListAsync(ct);
        if (handovers.Count == 0)
            throw new BusinessRuleException("Chuyến xe chưa có nhật ký bàn giao để in biên bản.", "SHIP_DOC_HANDOVER_NOT_FOUND", "ShippingHandoverLog");

        var packages = await LoadPackagesForLoadAsync(shipmentLoadId, ct);
        var carrierShipments = await LoadCarrierShipmentsByPackageAsync(packages.Select(p => p.OutboundPackageId), ct);
        var payload = BuildLoadPayload(
            load,
            "ShippingHandoverDocument",
            "Biên bản bàn giao",
            new(),
            BuildManifestVoucherRows(packages),
            BuildManifestPackageRows(packages, carrierShipments),
            BuildHandoverRows(handovers));

        var job = CreateDocumentJob(ResolveVoucherFromHandovers(handovers), LabelPurposeEnum.ShippingHandoverDocument, "ShippingHandoverDocument", load.ManifestCode ?? load.LoadCode, actor);
        job.ShipmentLoadId = load.ShipmentLoadId;
        job.ShippingHandoverLogId = handovers.First().ShippingHandoverLogId;
        job.TotalLabels = 1;
        job.LabelSize = "a4grid";
        job.CodeType = "qr";
        job.SourceDescription = $"Biên bản bàn giao chuyến {load.LoadCode}";
        job.MetadataJson = JsonSerializer.Serialize(payload, JsonOptions);
        await PersistJobAsync(job, ct);
        return job;
    }

    public async Task<LabelPrintJob> CreateDirectHandoverAsync(long shippingHandoverLogId, string actor, CancellationToken ct = default)
    {
        var handover = await _db.ShippingHandoverLogs
            .Include(h => h.Voucher)!.ThenInclude(v => v!.Partner)
            .Include(h => h.Warehouse)
            .FirstOrDefaultAsync(h => h.ShippingHandoverLogId == shippingHandoverLogId, ct)
            ?? throw new BusinessRuleException("Không tìm thấy nhật ký bàn giao.", "SHIP_DOC_HANDOVER_NOT_FOUND", "ShippingHandoverLog");
        if (handover.Voucher == null)
            throw new BusinessRuleException("Nhật ký bàn giao chưa liên kết phiếu hợp lệ.", "SHIP_DOC_HANDOVER_VOUCHER_MISSING", "ShippingHandoverLog");

        var payload = new ShippingDocumentPrintViewModel
        {
            DocumentType = "DirectShippingHandover",
            DocumentTitle = "Biên bản bàn giao",
            DocumentNumber = handover.ManifestCode ?? handover.TrackingNumber ?? handover.Voucher.VoucherCode,
            WarehouseName = handover.Warehouse?.WarehouseName,
            CarrierName = handover.CarrierName,
            ManifestCode = handover.ManifestCode,
            TrackingNumber = handover.TrackingNumber,
            VehicleNumber = handover.VehicleNumber,
            Handovers = BuildHandoverRows(new List<ShippingHandoverLog> { handover })
        };

        var job = CreateDocumentJob(handover.Voucher, LabelPurposeEnum.ShippingHandoverDocument, "DirectShippingHandover", payload.DocumentNumber, actor);
        job.ShippingHandoverLogId = handover.ShippingHandoverLogId;
        job.TotalLabels = 1;
        job.LabelSize = "a4grid";
        job.CodeType = "qr";
        job.SourceDescription = $"Biên bản bàn giao phiếu {handover.Voucher.VoucherCode}";
        job.MetadataJson = JsonSerializer.Serialize(payload, JsonOptions);
        await PersistJobAsync(job, ct);
        return job;
    }

    public async Task<ShippingDocumentPrintViewModel> BuildPrintViewModelAsync(long printJobId, string actor, bool markPrinted, CancellationToken ct = default)
    {
        var job = await _db.LabelPrintJobs
            .Include(j => j.Partner)
            .Include(j => j.Voucher)
            .Include(j => j.OutboundPackage)
            .Include(j => j.ShipmentLoad)
            .Include(j => j.ShippingHandoverLog)
            .FirstOrDefaultAsync(j => j.LabelPrintJobId == printJobId, ct)
            ?? throw new BusinessRuleException("Không tìm thấy lệnh in chứng từ vận chuyển.", "SHIP_DOC_JOB_NOT_FOUND", "LabelPrintJob");

        if (job.LabelPurpose is not (LabelPurposeEnum.ShippingPackageLabel or LabelPurposeEnum.ShipmentLoadManifest or LabelPurposeEnum.ShippingHandoverDocument))
            throw new BusinessRuleException("Lệnh in không phải chứng từ vận chuyển.", "SHIP_DOC_JOB_TYPE_INVALID", "LabelPrintJob");

        if (markPrinted && job.Status != LabelPrintJobStatusEnum.Printed)
        {
            job.Status = LabelPrintJobStatusEnum.Printed;
            job.PrintedBy = actor;
            job.PrintedAt = Now;
            await _unitOfWork.SaveChangesAsync(ct);
        }

        var vm = string.IsNullOrWhiteSpace(job.MetadataJson)
            ? new ShippingDocumentPrintViewModel()
            : JsonSerializer.Deserialize<ShippingDocumentPrintViewModel>(job.MetadataJson, JsonOptions) ?? new ShippingDocumentPrintViewModel();
        vm.Job = job;
        vm.DocumentType = job.DocumentType ?? vm.DocumentType;
        vm.DocumentNumber = job.DocumentNumber ?? vm.DocumentNumber;
        vm.DocumentTitle = string.IsNullOrWhiteSpace(vm.DocumentTitle) ? job.PurposeName : vm.DocumentTitle;
        vm.PrintedBy = job.PrintedBy ?? actor;
        vm.PrintedAt = job.PrintedAt ?? Now;
        return vm;
    }

    private static ShippingPackageLabelRow BuildPackageLabelRow(OutboundPackage package, CarrierShipment? carrierShipment)
    {
        var voucher = package.Voucher
            ?? throw new BusinessRuleException("Kiện xuất chưa liên kết phiếu hợp lệ.", "SHIP_DOC_PACKAGE_VOUCHER_MISSING", "OutboundPackage");
        var load = package.ShipmentLoad;
        var tracking = carrierShipment?.TrackingNumber ?? package.TrackingNumber ?? voucher.TrackingNumber;
        var manifest = package.ManifestCode ?? voucher.ManifestCode ?? load?.ManifestCode;
        return new ShippingPackageLabelRow
        {
            OutboundPackageId = package.OutboundPackageId,
            PackageCode = package.PackageCode,
            VoucherCode = voucher.VoucherCode,
            TrackingNumber = tracking,
            ManifestCode = manifest,
            LoadCode = load?.LoadCode,
            PartnerName = voucher.Partner?.PartnerName,
            ShipToAddress = string.IsNullOrWhiteSpace(voucher.Partner?.Address) ? "chưa cập nhật địa chỉ" : voucher.Partner.Address,
            CarrierName = carrierShipment?.CarrierNameSnapshot ?? load?.CarrierName ?? voucher.CarrierName,
            RouteName = load?.RouteName,
            ScanCode = $"PKG:{package.OutboundPackageId}:{package.PackageCode}",
            TotalQuantity = package.TotalQuantity,
            ActualCatchWeight = package.ActualCatchWeight
        };
    }

    private static ShippingDocumentPrintViewModel BuildLoadPayload(
        ShipmentLoad load,
        string documentType,
        string documentTitle,
        List<ShippingPackageLabelRow> labels,
        List<ShippingManifestVoucherRow> vouchers,
        List<ShippingManifestPackageRow> packages,
        List<ShippingHandoverDocumentRow> handovers)
        => new()
        {
            DocumentType = documentType,
            DocumentTitle = documentTitle,
            DocumentNumber = load.ManifestCode ?? load.LoadCode,
            LoadCode = load.LoadCode,
            WarehouseName = load.Warehouse?.WarehouseName,
            CarrierName = load.CarrierName,
            RouteName = load.RouteName,
            VehicleNumber = load.VehicleNumber,
            ManifestCode = load.ManifestCode,
            TrackingNumber = load.TrackingNumber,
            Notes = load.Notes,
            PackageLabels = labels,
            Vouchers = vouchers,
            Packages = packages,
            Handovers = handovers
        };

    private static List<ShippingManifestVoucherRow> BuildManifestVoucherRows(List<OutboundPackage> packages)
        => packages
            .Where(p => p.Voucher != null)
            .GroupBy(p => p.Voucher!)
            .OrderBy(g => g.Key.VoucherCode)
            .Select(g => new ShippingManifestVoucherRow
            {
                VoucherId = g.Key.VoucherId,
                VoucherCode = g.Key.VoucherCode,
                PartnerName = g.Key.Partner?.PartnerName,
                TrackingNumber = g.Key.TrackingNumber,
                ManifestCode = g.Key.ManifestCode,
                PackageCount = g.Count(),
                ShippedAt = g.Key.ShippedAt
            })
            .ToList();

    private static List<ShippingManifestPackageRow> BuildManifestPackageRows(List<OutboundPackage> packages, Dictionary<long, CarrierShipment> carrierShipments)
        => packages
            .OrderBy(p => p.Voucher?.VoucherCode)
            .ThenBy(p => p.PackageCode)
            .Select(p =>
            {
                carrierShipments.TryGetValue(p.OutboundPackageId, out var carrier);
                return new ShippingManifestPackageRow
                {
                    OutboundPackageId = p.OutboundPackageId,
                    PackageCode = p.PackageCode,
                    VoucherCode = p.Voucher?.VoucherCode ?? "",
                    TrackingNumber = carrier?.TrackingNumber ?? p.TrackingNumber ?? p.Voucher?.TrackingNumber,
                    CarrierName = carrier?.CarrierNameSnapshot ?? p.ShipmentLoad?.CarrierName ?? p.Voucher?.CarrierName,
                    IsLoaded = p.ShipmentLoadId.HasValue && p.LoadedAt.HasValue,
                    TotalQuantity = p.TotalQuantity,
                    ActualCatchWeight = p.ActualCatchWeight
                };
            })
            .ToList();

    private static List<ShippingHandoverDocumentRow> BuildHandoverRows(List<ShippingHandoverLog> handovers)
        => handovers
            .OrderBy(h => h.HandedOverAt)
            .Select(h => new ShippingHandoverDocumentRow
            {
                ShippingHandoverLogId = h.ShippingHandoverLogId,
                VoucherCode = h.Voucher?.VoucherCode ?? "",
                TrackingNumber = h.TrackingNumber,
                ManifestCode = h.ManifestCode,
                CarrierName = h.CarrierName,
                VehicleNumber = h.VehicleNumber,
                DriverName = h.DriverName,
                DriverPhone = h.DriverPhone,
                HandedOverBy = h.HandedOverBy,
                HandedOverAt = h.HandedOverAt,
                Notes = h.Notes
            })
            .ToList();

    private async Task<ShipmentLoad> LoadShipmentLoadAsync(long shipmentLoadId, CancellationToken ct)
        => await _db.ShipmentLoads
            .Include(l => l.Warehouse)
            .FirstOrDefaultAsync(l => l.ShipmentLoadId == shipmentLoadId, ct)
            ?? throw new BusinessRuleException("Không tìm thấy chuyến xe.", "SHIP_DOC_LOAD_NOT_FOUND", "ShipmentLoad");

    private async Task<List<OutboundPackage>> LoadPackagesForLoadAsync(long shipmentLoadId, CancellationToken ct)
        => await _db.OutboundPackages
            .Include(p => p.Voucher)!.ThenInclude(v => v!.Partner)
            .Include(p => p.Warehouse)
            .Include(p => p.ShipmentLoad)
            .Where(p => p.ShipmentLoadId == shipmentLoadId)
            .OrderBy(p => p.PackageCode)
            .ToListAsync(ct);

    private async Task<CarrierShipment?> ResolveCarrierShipmentAsync(long outboundPackageId, CancellationToken ct)
        => await _db.CarrierShipments
            .AsNoTracking()
            .Where(s => s.OutboundPackageId == outboundPackageId && s.Status != CarrierShipmentStatusEnum.Cancelled)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

    private async Task<Dictionary<long, CarrierShipment>> LoadCarrierShipmentsByPackageAsync(IEnumerable<long> packageIds, CancellationToken ct)
    {
        var ids = packageIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<long, CarrierShipment>();

        return (await _db.CarrierShipments
                .AsNoTracking()
                .Where(s => ids.Contains(s.OutboundPackageId))
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(ct))
            .GroupBy(s => s.OutboundPackageId)
            .ToDictionary(g => g.Key, g => g.Where(s => s.Status != CarrierShipmentStatusEnum.Cancelled).FirstOrDefault() ?? g.First());
    }

    private static Voucher ResolveFirstVoucher(List<OutboundPackage> packages)
        => packages.Select(p => p.Voucher).FirstOrDefault(v => v?.PartnerId != null)
            ?? throw new BusinessRuleException("Chứng từ vận chuyển cần ít nhất một phiếu có đối tác nhận hàng.", "SHIP_DOC_PARTNER_REQUIRED", "Voucher");

    private static Voucher ResolveVoucherFromHandovers(List<ShippingHandoverLog> handovers)
        => handovers.Select(h => h.Voucher).FirstOrDefault(v => v?.PartnerId != null)
            ?? throw new BusinessRuleException("Biên bản bàn giao cần ít nhất một phiếu có đối tác nhận hàng.", "SHIP_DOC_PARTNER_REQUIRED", "Voucher");

    private static LabelPrintJob CreateDocumentJob(Voucher voucher, LabelPurposeEnum purpose, string documentType, string documentNumber, string actor)
    {
        if (voucher.PartnerId == null)
            throw new BusinessRuleException("Phiếu chưa có đối tác nhận hàng, không thể in chứng từ vận chuyển.", "SHIP_DOC_PARTNER_REQUIRED", "Voucher");

        return new LabelPrintJob
        {
            JobCode = GenerateJobCode(),
            LabelPurpose = purpose,
            Status = LabelPrintJobStatusEnum.Created,
            PartnerId = voucher.PartnerId.Value,
            VoucherId = voucher.VoucherId,
            DocumentType = documentType,
            DocumentNumber = Clean(documentNumber, 80) ?? voucher.VoucherCode,
            RequestedBy = Clean(actor, 100) ?? "system",
            RequestedAt = Now,
            LabelSize = purpose == LabelPurposeEnum.ShippingPackageLabel ? "100x50" : "a4grid",
            CodeType = "qr",
            TotalLabels = 1
        };
    }

    private async Task PersistJobAsync(LabelPrintJob job, CancellationToken ct)
    {
        _db.LabelPrintJobs.Add(job);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static string GenerateJobCode()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"SDOC-{Now:yyyyMMddHHmmss}-{suffix}";
    }

    private static string? Clean(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
