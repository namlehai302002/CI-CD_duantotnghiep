using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Common;
using WMS.Models;
using WMS.Authorization;

namespace WMS.Controllers;

[Authorize]
public class UnitsController : Controller
{
    private readonly AppDbContext _db;
    public UnitsController(AppDbContext db) => _db = db;

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Index()
    {
        var uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).OrderBy(u => u.UomCode).ToListAsync();
        ViewBag.Conversions = await _db.UnitConversions
            .Include(c => c.FromUom).Include(c => c.ToUom).Include(c => c.Item)
            .Where(c => c.IsActive).ToListAsync();
        ViewBag.PackagingUnits = await _db.PackagingUnits
            .Include(p => p.BaseUom)
            .Where(p => p.IsActive).OrderBy(p => p.TenDongGoi).ToListAsync();
        ViewBag.AllUoms = uoms;
        return View(uoms);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.MasterUomManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string uomCode, string uomName, string? uomGroup)
    {
        if (await _db.UnitsOfMeasure.AnyAsync(u => u.UomCode == uomCode))
        {
            TempData["Error"] = $"Mã đơn vị '{uomCode}' đã tồn tại. Vui lòng chọn mã khác.";
            return RedirectToAction("Index");
        }

        _db.UnitsOfMeasure.Add(new UnitOfMeasure { UomCode = uomCode, UomName = uomName, UomGroup = uomGroup });
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm đơn vị '{uomName}'.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.MasterUomManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var uom = await _db.UnitsOfMeasure.FindAsync(id);
        if (uom == null) return NotFound();

        var isUsed = await _db.Items.AnyAsync(i => i.BaseUomId == id && i.IsActive);
        if (isUsed)
        {
            TempData["Error"] = $"Không thể xóa ĐVT '{uom.UomName}' vì đang được vật tư sử dụng làm đơn vị cơ sở.";
            return RedirectToAction("Index");
        }

        uom.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa đơn vị '{uom.UomName}'.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.MasterUomManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePackaging(string tenDongGoi, int baseUomId, decimal giaTri)
    {
        if (!string.IsNullOrWhiteSpace(tenDongGoi) && giaTri > 0)
        {
            if (await _db.PackagingUnits.AnyAsync(p => p.TenDongGoi == tenDongGoi && p.IsActive))
            {
                TempData["Error"] = $"Tên đóng gói '{tenDongGoi}' đã tồn tại.";
            }
            else
            {
                _db.PackagingUnits.Add(new PackagingUnit
                {
                    TenDongGoi = tenDongGoi,
                    BaseUomId = baseUomId,
                    GiaTri = giaTri,
                    IsActive = true
                });
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã thêm quy cách đóng gói '{tenDongGoi}'.";
            }
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.MasterUomManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePackaging(int id)
    {
        var pkg = await _db.PackagingUnits.FindAsync(id);
        if (pkg != null)
        {
            pkg.IsActive = false;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa đóng gói '{pkg.TenDongGoi}'.";
        }
        return RedirectToAction("Index");
    }
}
