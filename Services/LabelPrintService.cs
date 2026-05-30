using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;
using WMS.ViewModels;

namespace WMS.Services;

public interface ILabelPrintService
{
    Task<LabelPrintJob> CreateVoucherPrintJobAsync(long voucherId, string actor);
    Task<LabelPrintJob> CreatePackagePrintJobAsync(long outboundPackageId, string actor);
    Task<CustomerLabelPrintViewModel> BuildPrintViewModelAsync(long printJobId, string actor, bool markPrinted);
}

public class LabelPrintService : ILabelPrintService
{
    private const int MaxLabelsPerJob = 1000;
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public LabelPrintService(AppDbContext db, IUnitOfWork unitOfWork)
    {
        _db = db;
        _unitOfWork = unitOfWork;
    }

    public async Task<LabelPrintJob> CreateVoucherPrintJobAsync(long voucherId, string actor)
    {
        var voucher = await _db.Vouchers
            .Include(v => v.Partner)
            .Include(v => v.Details).ThenInclude(d => d.Item)
            .FirstOrDefaultAsync(v => v.VoucherId == voucherId)
            ?? throw new BusinessRuleException("Không tìm thấy phiếu xuất cần in nhãn.", "LABEL_VOUCHER_NOT_FOUND", nameof(Voucher));

        EnsurePrintableVoucher(voucher);
        var template = await ResolveTemplateAsync(voucher.PartnerId!.Value, LabelPurposeEnum.OutboundVoucher);
        var rules = await LoadRulesAsync(voucher.PartnerId.Value, voucher.Details.Select(d => d.ItemId));

        var job = CreateJob(voucher.PartnerId.Value, LabelPurposeEnum.OutboundVoucher, template, actor);
        job.VoucherId = voucher.VoucherId;
        job.SourceDescription = $"Phiếu xuất {voucher.VoucherCode}";

        foreach (var detail in voucher.Details.OrderBy(d => d.LineNumber).ThenBy(d => d.VoucherDetailId))
        {
            if (detail.Item == null) continue;
            var qty = ResolveLineQty(detail);
            if (qty <= 0) continue;
            job.Lines.Add(BuildLine(job, template, voucher, null, detail, detail.Item, rules.GetValueOrDefault(detail.ItemId), qty));
        }

        EnsureJobHasLines(job);
        await PersistJobAsync(job);
        return job;
    }

    public async Task<LabelPrintJob> CreatePackagePrintJobAsync(long outboundPackageId, string actor)
    {
        var package = await _db.OutboundPackages
            .Include(p => p.Voucher).ThenInclude(v => v!.Partner)
            .Include(p => p.Voucher).ThenInclude(v => v!.Details).ThenInclude(d => d.Item)
            .FirstOrDefaultAsync(p => p.OutboundPackageId == outboundPackageId)
            ?? throw new BusinessRuleException("Không tìm thấy kiện xuất cần in nhãn.", "LABEL_PACKAGE_NOT_FOUND", nameof(OutboundPackage));

        var voucher = package.Voucher
            ?? throw new BusinessRuleException("Kiện xuất chưa liên kết với phiếu xuất hợp lệ.", "LABEL_PACKAGE_VOUCHER_NOT_FOUND", nameof(OutboundPackage));

        EnsurePrintableVoucher(voucher);
        var template = await ResolveTemplateAsync(voucher.PartnerId!.Value, LabelPurposeEnum.OutboundPackage);
        var rules = await LoadRulesAsync(voucher.PartnerId.Value, voucher.Details.Select(d => d.ItemId));

        var job = CreateJob(voucher.PartnerId.Value, LabelPurposeEnum.OutboundPackage, template, actor);
        job.VoucherId = voucher.VoucherId;
        job.OutboundPackageId = package.OutboundPackageId;
        job.SourceDescription = $"Kiện xuất {package.PackageCode} / phiếu {voucher.VoucherCode}";

        var packageContents = await ResolvePackageContentsAsync(package, voucher);
        foreach (var (detail, qty) in packageContents)
        {
            job.Lines.Add(BuildLine(job, template, voucher, package, detail, detail.Item!, rules.GetValueOrDefault(detail.ItemId), qty));
        }

        EnsureJobHasLines(job);
        await PersistJobAsync(job);
        return job;
    }

    public async Task<CustomerLabelPrintViewModel> BuildPrintViewModelAsync(long printJobId, string actor, bool markPrinted)
    {
        var job = await _db.LabelPrintJobs
            .Include(j => j.Partner)
            .Include(j => j.Voucher)
            .Include(j => j.OutboundPackage)
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.LabelPrintJobId == printJobId)
            ?? throw new BusinessRuleException("Không tìm thấy lệnh in nhãn.", "LABEL_JOB_NOT_FOUND", nameof(LabelPrintJob));

        if (markPrinted && job.Status != LabelPrintJobStatusEnum.Printed)
        {
            job.Status = LabelPrintJobStatusEnum.Printed;
            job.PrintedBy = actor;
            job.PrintedAt = VietnamTime.Now;
            await _unitOfWork.SaveChangesAsync();
        }

        return new CustomerLabelPrintViewModel
        {
            Job = job,
            LabelSize = job.LabelSize,
            CodeType = job.CodeType,
            TotalLabels = job.TotalLabels,
            Lines = job.Lines
                .OrderBy(l => l.LabelPrintJobLineId)
                .Select(l => new CustomerLabelPrintLineViewModel
                {
                    LabelPrintJobLineId = l.LabelPrintJobLineId,
                    BarcodeValue = l.BarcodeValue,
                    HeaderText = l.HeaderText,
                    BodyText = l.BodyText,
                    FooterText = l.FooterText,
                    PrintQuantity = l.PrintQuantity,
                    PartnerName = l.PartnerName,
                    VoucherCode = l.VoucherCode,
                    PackageCode = l.PackageCode,
                    InternalItemCode = l.InternalItemCode,
                    InternalItemName = l.InternalItemName,
                    CustomerItemCode = l.CustomerItemCode,
                    CustomerItemName = l.CustomerItemName
                })
                .ToList()
        };
    }

    private async Task<PartnerLabelTemplate> ResolveTemplateAsync(int partnerId, LabelPurposeEnum purpose)
    {
        var template = await _db.PartnerLabelTemplates
            .AsNoTracking()
            .Where(t => t.IsActive
                && t.LabelPurpose == purpose
                && (t.PartnerId == partnerId || t.PartnerId == null))
            .OrderByDescending(t => t.PartnerId == partnerId)
            .ThenByDescending(t => t.IsDefault)
            .ThenBy(t => t.PartnerLabelTemplateId)
            .FirstOrDefaultAsync();

        return template
            ?? throw new BusinessRuleException("Chưa có mẫu nhãn hợp lệ cho khách hàng hoặc mẫu mặc định. Vui lòng tạo/bật mẫu nhãn trước khi in.", "LABEL_TEMPLATE_NOT_FOUND", nameof(PartnerLabelTemplate));
    }

    private async Task<Dictionary<int, PartnerItemLabelRule>> LoadRulesAsync(int partnerId, IEnumerable<int> itemIds)
    {
        var ids = itemIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, PartnerItemLabelRule>();

        return await _db.PartnerItemLabelRules
            .AsNoTracking()
            .Where(r => r.PartnerId == partnerId && r.IsActive && ids.Contains(r.ItemId))
            .ToDictionaryAsync(r => r.ItemId);
    }

    private static void EnsurePrintableVoucher(Voucher voucher)
    {
        if (voucher.IsCancelled)
            throw new BusinessRuleException("Phiếu đã hủy, không thể in nhãn khách hàng.", "LABEL_VOUCHER_CANCELLED", nameof(Voucher));
        if (voucher.PartnerId == null || voucher.Partner == null)
            throw new BusinessRuleException("Phiếu xuất chưa có khách hàng/đối tác, không thể in nhãn khách hàng.", "LABEL_PARTNER_REQUIRED", nameof(Partner));
        if (voucher.VoucherType is not (VoucherTypeEnum.XuatKho or VoucherTypeEnum.TraNCC or VoucherTypeEnum.ChuyenKho or VoucherTypeEnum.XuatSanXuat))
            throw new BusinessRuleException("Chỉ phiếu xuất kho mới được in nhãn khách hàng.", "LABEL_OUTBOUND_REQUIRED", nameof(Voucher));
    }

    private static LabelPrintJob CreateJob(int partnerId, LabelPurposeEnum purpose, PartnerLabelTemplate template, string actor)
    {
        return new LabelPrintJob
        {
            JobCode = GenerateJobCode(),
            LabelPurpose = purpose,
            Status = LabelPrintJobStatusEnum.Created,
            PartnerId = partnerId,
            PartnerLabelTemplateId = template.PartnerLabelTemplateId,
            LabelSize = template.LabelSize,
            CodeType = template.CodeType,
            RequestedBy = actor,
            RequestedAt = VietnamTime.Now,
            TemplateSnapshotJson = JsonSerializer.Serialize(new
            {
                template.PartnerLabelTemplateId,
                template.TemplateName,
                template.LabelPurpose,
                template.LabelSize,
                template.CodeType,
                template.HeaderTemplate,
                template.BodyTemplate,
                template.FooterTemplate
            })
        };
    }

    private static LabelPrintJobLine BuildLine(
        LabelPrintJob job,
        PartnerLabelTemplate template,
        Voucher voucher,
        OutboundPackage? package,
        VoucherDetail detail,
        Item item,
        PartnerItemLabelRule? rule,
        decimal qty)
    {
        var customerCode = Normalize(rule?.CustomerItemCode) ?? item.ItemCode;
        var customerName = Normalize(rule?.CustomerItemName) ?? item.ItemName;
        var packageCode = package?.PackageCode;
        var barcodeValue = job.LabelPurpose == LabelPurposeEnum.OutboundPackage
            ? (packageCode ?? voucher.VoucherCode)
            : (Normalize(rule?.CustomerItemCode) ?? Normalize(item.Barcode) ?? item.ItemCode);

        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["{TenKhachHang}"] = voucher.Partner?.PartnerName ?? "",
            ["{MaKhachHang}"] = voucher.Partner?.PartnerCode ?? "",
            ["{MaPhieu}"] = voucher.VoucherCode,
            ["{MaKien}"] = packageCode ?? "",
            ["{MaVanDon}"] = package?.TrackingNumber ?? voucher.TrackingNumber ?? "",
            ["{MaHangNoiBo}"] = item.ItemCode,
            ["{TenHangNoiBo}"] = item.ItemName,
            ["{MaHangKhach}"] = customerCode,
            ["{TenHangKhach}"] = customerName,
            ["{SoLuong}"] = qty.ToString("N2"),
            ["{NgayIn}"] = VietnamTime.Now.ToString("dd/MM/yyyy HH:mm")
        };

        if (!string.IsNullOrWhiteSpace(rule?.CustomText))
            values["{GhiChuRieng}"] = rule.CustomText!.Trim();

        return new LabelPrintJobLine
        {
            VoucherDetailId = detail.VoucherDetailId,
            OutboundPackageId = package?.OutboundPackageId,
            ItemId = item.ItemId,
            Quantity = qty,
            PrintQuantity = Math.Max(1, (int)Math.Ceiling(qty)),
            BarcodeValue = barcodeValue,
            InternalItemCode = item.ItemCode,
            InternalItemName = item.ItemName,
            CustomerItemCode = customerCode,
            CustomerItemName = customerName,
            PartnerName = voucher.Partner?.PartnerName ?? "",
            VoucherCode = voucher.VoucherCode,
            PackageCode = packageCode,
            TrackingNumber = package?.TrackingNumber ?? voucher.TrackingNumber,
            HeaderText = Render(template.HeaderTemplate, values),
            BodyText = Render(template.BodyTemplate, values),
            FooterText = Render(template.FooterTemplate, values),
            RenderDataJson = JsonSerializer.Serialize(values.ToDictionary(k => k.Key.Trim('{', '}'), v => v.Value))
        };
    }

    private static void EnsureJobHasLines(LabelPrintJob job)
    {
        if (job.Lines.Count == 0)
            throw new BusinessRuleException("Không có dòng hàng hợp lệ để in nhãn khách hàng.", "LABEL_NO_LINES", nameof(LabelPrintJobLine));

        job.TotalLabels = job.Lines.Sum(l => l.PrintQuantity);
        if (job.TotalLabels > MaxLabelsPerJob)
            throw new BusinessRuleException($"Lệnh in có {job.TotalLabels:N0} tem, vượt giới hạn {MaxLabelsPerJob:N0} tem/lần. Vui lòng tách lệnh in.", "LABEL_TOO_MANY", nameof(LabelPrintJob));
    }

    private async Task PersistJobAsync(LabelPrintJob job)
    {
        _db.LabelPrintJobs.Add(job);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<List<(VoucherDetail Detail, decimal Qty)>> ResolvePackageContentsAsync(OutboundPackage package, Voucher voucher)
    {
        var details = voucher.Details
            .Where(d => d.Item != null && ResolveLineQty(d) > 0)
            .OrderBy(d => d.LineNumber)
            .ThenBy(d => d.VoucherDetailId)
            .ToList();

        var lpnCode = Normalize(package.ReferenceLpnCode)
            ?? (string.Equals(package.SourceType, "LPN", StringComparison.OrdinalIgnoreCase) ? Normalize(package.PackageCode) : null);
        if (lpnCode != null)
        {
            var lpn = await _db.LicensePlates
                .AsNoTracking()
                .Include(l => l.Details)
                .FirstOrDefaultAsync(l => l.VoucherId == voucher.VoucherId && l.LpnCode == lpnCode);
            if (lpn?.Details.Count > 0)
            {
                var result = new List<(VoucherDetail Detail, decimal Qty)>();
                foreach (var lpnLine in lpn.Details.OrderBy(d => d.LicensePlateDetailId))
                {
                    var voucherDetail = lpnLine.VoucherDetailId.HasValue
                        ? details.FirstOrDefault(d => d.VoucherDetailId == lpnLine.VoucherDetailId.Value)
                        : details.FirstOrDefault(d => d.ItemId == lpnLine.ItemId);
                    if (voucherDetail != null && lpnLine.Quantity > 0)
                        result.Add((voucherDetail, Math.Abs(lpnLine.Quantity)));
                }

                if (result.Count > 0)
                    return result;
            }
        }

        if (details.Count == 1)
        {
            var detail = details[0];
            var qty = package.TotalQuantity.HasValue ? Math.Abs(package.TotalQuantity.Value) : ResolveLineQty(detail);
            if (qty > 0)
                return new List<(VoucherDetail Detail, decimal Qty)> { (detail, qty) };
        }

        throw new BusinessRuleException(
            "Không thể xác định chắc chắn nội dung kiện để in nhãn. Vui lòng in theo phiếu hoặc dùng kiện có LPN liên kết rõ dòng hàng.",
            "LABEL_PACKAGE_CONTENT_AMBIGUOUS",
            nameof(OutboundPackage));
    }

    private static decimal ResolveLineQty(VoucherDetail detail)
    {
        var qty = detail.BaseQty != 0 ? detail.BaseQty : detail.TransactionQty;
        return Math.Abs(qty);
    }

    private static string Render(string? template, Dictionary<string, string> values)
    {
        var output = template ?? "";
        foreach (var pair in values)
            output = output.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
        return output.Trim();
    }

    private static string GenerateJobCode()
        => $"LBL-{VietnamTime.Now:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}";

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
