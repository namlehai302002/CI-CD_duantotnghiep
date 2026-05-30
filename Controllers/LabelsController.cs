using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;
using WMS.Services;
using WMS.ViewModels;

namespace WMS.Controllers;

[Authorize(Roles = "Admin,Manager,Staff")]
public class LabelsController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILabelPrintService _labelService;
    private readonly IShippingDocumentService _shippingDocumentService;

    public LabelsController(AppDbContext db, ILabelPrintService? labelService = null, IShippingDocumentService? shippingDocumentService = null)
    {
        _db = db;
        _labelService = labelService ?? new LabelPrintService(db, new EfUnitOfWork(db));
        _shippingDocumentService = shippingDocumentService ?? new ShippingDocumentService(db, new EfUnitOfWork(db));
    }

    public IActionResult Index()
        => RedirectToAction(nameof(PrintJobs));

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> Templates(int? partnerId, LabelPurposeEnum? purpose)
    {
        var query = _db.PartnerLabelTemplates
            .AsNoTracking()
            .Include(t => t.Partner)
            .AsQueryable();

        if (partnerId.HasValue)
            query = query.Where(t => t.PartnerId == partnerId.Value);
        if (purpose.HasValue)
            query = query.Where(t => t.LabelPurpose == purpose.Value);

        ViewBag.Partners = await GetCustomerPartnersAsync();
        ViewBag.PartnerId = partnerId;
        ViewBag.Purpose = purpose;

        var templates = await query
            .OrderByDescending(t => t.IsActive)
            .ThenBy(t => t.PartnerId == null ? 0 : 1)
            .ThenBy(t => t.Partner!.PartnerCode)
            .ThenBy(t => t.TemplateName)
            .ToListAsync();

        return View(templates);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> CreateTemplate()
    {
        return View("TemplateForm", new PartnerLabelTemplateFormViewModel
        {
            TemplateName = "Mẫu nhãn khách hàng",
            HeaderTemplate = "{TenKhachHang}",
            BodyTemplate = "Mã khách: {MaHangKhach}\nTên hàng: {TenHangKhach}\nPhiếu: {MaPhieu}\nSố lượng: {SoLuong}",
            FooterTemplate = "In lúc {NgayIn}",
            Partners = await GetCustomerPartnersAsync()
        });
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTemplate(PartnerLabelTemplateFormViewModel vm)
    {
        if (!ValidateTemplate(vm))
        {
            vm.Partners = await GetCustomerPartnersAsync();
            return View("TemplateForm", vm);
        }

        var template = new PartnerLabelTemplate
        {
            PartnerId = vm.PartnerId,
            LabelPurpose = vm.LabelPurpose,
            TemplateName = vm.TemplateName.Trim(),
            LabelSize = vm.LabelSize.Trim(),
            CodeType = vm.CodeType.Trim(),
            HeaderTemplate = vm.HeaderTemplate?.Trim() ?? "",
            BodyTemplate = vm.BodyTemplate?.Trim() ?? "",
            FooterTemplate = vm.FooterTemplate?.Trim() ?? "",
            IsDefault = vm.IsDefault,
            IsActive = vm.IsActive,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = VietnamTime.Now
        };

        _db.PartnerLabelTemplates.Add(template);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã tạo mẫu nhãn khách hàng.";
        return RedirectToAction(nameof(Templates));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> EditTemplate(long id)
    {
        var template = await _db.PartnerLabelTemplates.FindAsync(id);
        if (template == null) return NotFound();

        return View("TemplateForm", new PartnerLabelTemplateFormViewModel
        {
            PartnerLabelTemplateId = template.PartnerLabelTemplateId,
            PartnerId = template.PartnerId,
            LabelPurpose = template.LabelPurpose,
            TemplateName = template.TemplateName,
            LabelSize = template.LabelSize,
            CodeType = template.CodeType,
            HeaderTemplate = template.HeaderTemplate,
            BodyTemplate = template.BodyTemplate,
            FooterTemplate = template.FooterTemplate,
            IsDefault = template.IsDefault,
            IsActive = template.IsActive,
            Partners = await GetCustomerPartnersAsync()
        });
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTemplate(PartnerLabelTemplateFormViewModel vm)
    {
        if (!ValidateTemplate(vm))
        {
            vm.Partners = await GetCustomerPartnersAsync();
            return View("TemplateForm", vm);
        }

        var template = await _db.PartnerLabelTemplates.FindAsync(vm.PartnerLabelTemplateId);
        if (template == null) return NotFound();

        template.PartnerId = vm.PartnerId;
        template.LabelPurpose = vm.LabelPurpose;
        template.TemplateName = vm.TemplateName.Trim();
        template.LabelSize = vm.LabelSize.Trim();
        template.CodeType = vm.CodeType.Trim();
        template.HeaderTemplate = vm.HeaderTemplate?.Trim() ?? "";
        template.BodyTemplate = vm.BodyTemplate?.Trim() ?? "";
        template.FooterTemplate = vm.FooterTemplate?.Trim() ?? "";
        template.IsDefault = vm.IsDefault;
        template.IsActive = vm.IsActive;
        template.UpdatedBy = User.Identity?.Name ?? "system";
        template.UpdatedAt = VietnamTime.Now;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật mẫu nhãn khách hàng.";
        return RedirectToAction(nameof(Templates));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTemplate(long id)
    {
        var template = await _db.PartnerLabelTemplates.FindAsync(id);
        if (template == null) return NotFound();

        template.IsActive = !template.IsActive;
        template.UpdatedBy = User.Identity?.Name ?? "system";
        template.UpdatedAt = VietnamTime.Now;
        await _db.SaveChangesAsync();
        TempData["Success"] = template.IsActive ? "Đã bật mẫu nhãn." : "Đã tắt mẫu nhãn.";
        return RedirectToAction(nameof(Templates));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> ItemRules(int? partnerId, string? search)
    {
        var rulesQuery = _db.PartnerItemLabelRules
            .AsNoTracking()
            .Include(r => r.Partner)
            .Include(r => r.Item)
            .AsQueryable();

        if (partnerId.HasValue)
            rulesQuery = rulesQuery.Where(r => r.PartnerId == partnerId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            rulesQuery = rulesQuery.Where(r =>
                r.CustomerItemCode.Contains(keyword)
                || (r.CustomerItemName != null && r.CustomerItemName.Contains(keyword))
                || r.Item!.ItemCode.Contains(keyword)
                || r.Item.ItemName.Contains(keyword)
                || r.Partner!.PartnerCode.Contains(keyword)
                || r.Partner.PartnerName.Contains(keyword));
        }

        var itemQuery = _db.Items.AsNoTracking().Where(i => i.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            itemQuery = itemQuery.Where(i => i.ItemCode.Contains(keyword) || i.ItemName.Contains(keyword) || (i.Barcode != null && i.Barcode.Contains(keyword)));
        }

        var vm = new PartnerItemLabelRulePageViewModel
        {
            PartnerId = partnerId,
            Search = search,
            Partners = await GetCustomerPartnersAsync(),
            Items = await itemQuery.OrderBy(i => i.ItemCode).Take(200).ToListAsync(),
            Rules = await rulesQuery
                .OrderBy(r => r.Partner!.PartnerCode)
                .ThenBy(r => r.Item!.ItemCode)
                .Take(500)
                .ToListAsync()
        };

        return View(vm);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveItemRule(int partnerId, int itemId, string customerItemCode, string? customerItemName, string? customText, bool isActive = true)
    {
        if (partnerId <= 0 || itemId <= 0 || string.IsNullOrWhiteSpace(customerItemCode))
        {
            TempData["Error"] = "Vui lòng chọn khách hàng, SKU nội bộ và nhập mã hàng khách hàng.";
            return RedirectToAction(nameof(ItemRules), new { partnerId });
        }

        var partnerOk = await _db.Partners.AnyAsync(p => p.PartnerId == partnerId && p.IsActive);
        var itemOk = await _db.Items.AnyAsync(i => i.ItemId == itemId && i.IsActive);
        if (!partnerOk || !itemOk)
        {
            TempData["Error"] = "Khách hàng hoặc SKU nội bộ không hợp lệ.";
            return RedirectToAction(nameof(ItemRules), new { partnerId });
        }

        var actor = User.Identity?.Name ?? "system";
        var now = VietnamTime.Now;
        var rule = await _db.PartnerItemLabelRules.FirstOrDefaultAsync(r => r.PartnerId == partnerId && r.ItemId == itemId);
        if (rule == null)
        {
            rule = new PartnerItemLabelRule
            {
                PartnerId = partnerId,
                ItemId = itemId,
                CreatedBy = actor,
                CreatedAt = now
            };
            _db.PartnerItemLabelRules.Add(rule);
        }
        else
        {
            rule.UpdatedBy = actor;
            rule.UpdatedAt = now;
        }

        rule.CustomerItemCode = customerItemCode.Trim();
        rule.CustomerItemName = string.IsNullOrWhiteSpace(customerItemName) ? null : customerItemName.Trim();
        rule.CustomText = string.IsNullOrWhiteSpace(customText) ? null : customText.Trim();
        rule.IsActive = isActive;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã lưu mã hàng theo khách hàng.";
        return RedirectToAction(nameof(ItemRules), new { partnerId });
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItemRule(long id)
    {
        var rule = await _db.PartnerItemLabelRules.FindAsync(id);
        if (rule == null) return NotFound();

        var partnerId = rule.PartnerId;
        _db.PartnerItemLabelRules.Remove(rule);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa mã hàng theo khách hàng.";
        return RedirectToAction(nameof(ItemRules), new { partnerId });
    }

    [HttpGet]
    public async Task<IActionResult> PrintJobs(int? partnerId, string? search)
    {
        var query = _db.LabelPrintJobs
            .AsNoTracking()
            .Include(j => j.Partner)
            .Include(j => j.Voucher)
            .Include(j => j.OutboundPackage)
            .Include(j => j.ShipmentLoad)
            .Include(j => j.CarrierShipment)
            .AsQueryable();

        if (partnerId.HasValue)
            query = query.Where(j => j.PartnerId == partnerId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(j =>
                j.JobCode.Contains(keyword)
                || (j.DocumentNumber != null && j.DocumentNumber.Contains(keyword))
                || (j.DocumentType != null && j.DocumentType.Contains(keyword))
                || (j.Voucher != null && j.Voucher.VoucherCode.Contains(keyword))
                || (j.OutboundPackage != null && j.OutboundPackage.PackageCode.Contains(keyword))
                || (j.ShipmentLoad != null && j.ShipmentLoad.LoadCode.Contains(keyword))
                || (j.CarrierShipment != null && j.CarrierShipment.TrackingNumber != null && j.CarrierShipment.TrackingNumber.Contains(keyword))
                || j.Partner!.PartnerCode.Contains(keyword)
                || j.Partner.PartnerName.Contains(keyword));
        }

        ViewBag.Partners = await GetCustomerPartnersAsync();
        ViewBag.PartnerId = partnerId;
        ViewBag.Search = search;

        var jobs = await query
            .OrderByDescending(j => j.RequestedAt)
            .Take(300)
            .ToListAsync();

        return View(jobs);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PrintVoucher(long voucherId)
    {
        try
        {
            var job = await _labelService.CreateVoucherPrintJobAsync(voucherId, User.Identity?.Name ?? "system");
            return RedirectToAction(nameof(Print), new { id = job.LabelPrintJobId });
        }
        catch (BusinessRuleException ex)
        {
            TempData["Error"] = UserSafeError.From(ex);
            return RedirectToAction("Details", "Vouchers", new { id = voucherId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PrintPackage(long outboundPackageId)
    {
        long? voucherId = null;
        try
        {
            voucherId = await _db.OutboundPackages
                .Where(p => p.OutboundPackageId == outboundPackageId)
                .Select(p => (long?)p.VoucherId)
                .FirstOrDefaultAsync();
            var job = await _labelService.CreatePackagePrintJobAsync(outboundPackageId, User.Identity?.Name ?? "system");
            return RedirectToAction(nameof(Print), new { id = job.LabelPrintJobId });
        }
        catch (BusinessRuleException ex)
        {
            TempData["Error"] = UserSafeError.From(ex);
            if (voucherId.HasValue)
                return RedirectToAction("Details", "Vouchers", new { id = voucherId.Value });
            return RedirectToAction("PackageLookup", "Operations");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PrintShippingPackage(long outboundPackageId)
    {
        try
        {
            var job = await _shippingDocumentService.CreatePackageShippingLabelAsync(outboundPackageId, User.Identity?.Name ?? "system");
            return RedirectToAction(nameof(ShippingDocument), new { id = job.LabelPrintJobId });
        }
        catch (BusinessRuleException ex)
        {
            TempData["Error"] = UserSafeError.From(ex);
            return RedirectToAction("PackageLookup", "Operations");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PrintShipmentLoadPackageLabels(long shipmentLoadId)
    {
        try
        {
            var job = await _shippingDocumentService.CreateShipmentLoadPackageLabelsAsync(shipmentLoadId, User.Identity?.Name ?? "system");
            return RedirectToAction(nameof(ShippingDocument), new { id = job.LabelPrintJobId });
        }
        catch (BusinessRuleException ex)
        {
            TempData["Error"] = UserSafeError.From(ex);
            return RedirectToAction("ShipmentLoadDetails", "Operations", new { id = shipmentLoadId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PrintShipmentLoadManifest(long shipmentLoadId)
    {
        try
        {
            var job = await _shippingDocumentService.CreateShipmentLoadManifestAsync(shipmentLoadId, User.Identity?.Name ?? "system");
            return RedirectToAction(nameof(ShippingDocument), new { id = job.LabelPrintJobId });
        }
        catch (BusinessRuleException ex)
        {
            TempData["Error"] = UserSafeError.From(ex);
            return RedirectToAction("ShipmentLoadDetails", "Operations", new { id = shipmentLoadId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PrintShipmentLoadHandover(long shipmentLoadId)
    {
        try
        {
            var job = await _shippingDocumentService.CreateShipmentLoadHandoverAsync(shipmentLoadId, User.Identity?.Name ?? "system");
            return RedirectToAction(nameof(ShippingDocument), new { id = job.LabelPrintJobId });
        }
        catch (BusinessRuleException ex)
        {
            TempData["Error"] = UserSafeError.From(ex);
            return RedirectToAction("ShipmentLoadDetails", "Operations", new { id = shipmentLoadId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PrintDirectHandover(long shippingHandoverLogId)
    {
        try
        {
            var job = await _shippingDocumentService.CreateDirectHandoverAsync(shippingHandoverLogId, User.Identity?.Name ?? "system");
            return RedirectToAction(nameof(ShippingDocument), new { id = job.LabelPrintJobId });
        }
        catch (BusinessRuleException ex)
        {
            TempData["Error"] = UserSafeError.From(ex);
            return RedirectToAction("Shipping", "Operations");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Print(long id)
    {
        var vm = await _labelService.BuildPrintViewModelAsync(id, User.Identity?.Name ?? "system", markPrinted: true);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ShippingDocument(long id)
    {
        var vm = await _shippingDocumentService.BuildPrintViewModelAsync(id, User.Identity?.Name ?? "system", markPrinted: true);
        return View(vm);
    }

    private bool ValidateTemplate(PartnerLabelTemplateFormViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.TemplateName))
            ModelState.AddModelError(nameof(vm.TemplateName), "Vui lòng nhập tên mẫu nhãn.");
        if (string.IsNullOrWhiteSpace(vm.BodyTemplate))
            ModelState.AddModelError(nameof(vm.BodyTemplate), "Vui lòng nhập nội dung thân nhãn.");
        if (!new[] { "50x30", "100x50", "a4grid" }.Contains(vm.LabelSize))
            ModelState.AddModelError(nameof(vm.LabelSize), "Khổ nhãn không hợp lệ.");
        if (!new[] { "barcode", "qr" }.Contains(vm.CodeType))
            ModelState.AddModelError(nameof(vm.CodeType), "Loại mã in không hợp lệ.");
        return ModelState.IsValid;
    }

    private Task<List<Partner>> GetCustomerPartnersAsync()
        => _db.Partners
            .AsNoTracking()
            .Where(p => p.IsActive && (p.PartnerType == PartnerTypeEnum.Customer || p.PartnerType == PartnerTypeEnum.Both))
            .OrderBy(p => p.PartnerCode)
            .ToListAsync();
}
