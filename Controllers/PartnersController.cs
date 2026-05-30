using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using WMS.Data;
using WMS.Common;
using WMS.Models;
using WMS.Authorization;

namespace WMS.Controllers;

[Authorize]
public class PartnersController : Controller
{
    private readonly AppDbContext _db;
    public PartnersController(AppDbContext db) => _db = db;

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Index(PartnerTypeEnum? type, string? search)
    {
        var query = _db.Partners.Where(p => p.IsActive).AsQueryable();
        if (type.HasValue)
            query = query.Where(p => p.PartnerType == type.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.PartnerName.Contains(search) || p.PartnerCode.Contains(search));

        ViewBag.Type = type;
        ViewBag.Search = search;
        return View(await query.OrderBy(p => p.PartnerCode).ToListAsync());
    }

    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.MasterPartnerManage)]
    public IActionResult Create() => View(new Partner());

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.MasterPartnerManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Partner partner)
    {
        partner.PartnerCode = (partner.PartnerCode ?? "").Trim();
        partner.PartnerName = (partner.PartnerName ?? "").Trim();
        partner.Email = string.IsNullOrWhiteSpace(partner.Email) ? null : partner.Email.Trim();

        if (!string.IsNullOrWhiteSpace(partner.Email) && !new EmailAddressAttribute().IsValid(partner.Email))
            ModelState.AddModelError(nameof(partner.Email), "Email đối tác không đúng định dạng.");
        if (await _db.Partners.AnyAsync(p => p.PartnerCode == partner.PartnerCode))
            ModelState.AddModelError(nameof(partner.PartnerCode), $"Mã đối tác '{partner.PartnerCode}' đã tồn tại.");
        if (!ModelState.IsValid)
            return View(partner);

        partner.CreatedAt = VietnamTime.Now;
        _db.Partners.Add(partner);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm đối tác '{partner.PartnerName}'.";
        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.MasterPartnerManage)]
    public async Task<IActionResult> Edit(int id)
    {
        var p = await _db.Partners.FindAsync(id);
        return p == null ? NotFound() : View(p);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.MasterPartnerManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Partner partner)
    {
        var existing = await _db.Partners.FindAsync(id);
        if (existing == null) return NotFound();
        partner.PartnerCode = (partner.PartnerCode ?? "").Trim();
        partner.PartnerName = (partner.PartnerName ?? "").Trim();
        partner.Email = string.IsNullOrWhiteSpace(partner.Email) ? null : partner.Email.Trim();

        if (!string.IsNullOrWhiteSpace(partner.Email) && !new EmailAddressAttribute().IsValid(partner.Email))
            ModelState.AddModelError(nameof(partner.Email), "Email đối tác không đúng định dạng.");
        if (await _db.Partners.AnyAsync(p => p.PartnerId != id && p.PartnerCode == partner.PartnerCode))
            ModelState.AddModelError(nameof(partner.PartnerCode), $"Mã đối tác '{partner.PartnerCode}' đã tồn tại.");
        if (!ModelState.IsValid)
            return View(partner);

        existing.PartnerCode = partner.PartnerCode;
        existing.PartnerName = partner.PartnerName;
        existing.PartnerType = partner.PartnerType;
        existing.TaxCode = partner.TaxCode;
        existing.Phone = partner.Phone;
        existing.Email = partner.Email;
        existing.Address = partner.Address;
        existing.ContactPerson = partner.ContactPerson;
        existing.VendorRating = partner.VendorRating;
        existing.LeadTimeDays = partner.LeadTimeDays;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật đối tác.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Authorize(Policy = WmsPermissions.MasterPartnerManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Partners.FindAsync(id);
        if (p == null) return NotFound();

        var hasPendingVouchers = await _db.Vouchers
            .AnyAsync(v => v.PartnerId == id && !v.IsPosted && !v.IsCancelled);
        if (hasPendingVouchers)
        {
            TempData["Error"] = $"Không thể xóa đối tác '{p.PartnerName}' vì đang có phiếu chưa ghi sổ liên quan.";
            return RedirectToAction("Index");
        }

        p.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa đối tác '{p.PartnerName}'.";
        return RedirectToAction("Index");
    }
}
