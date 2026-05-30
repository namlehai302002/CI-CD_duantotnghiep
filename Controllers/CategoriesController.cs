using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Common;
using WMS.Models;
using WMS.Authorization;

namespace WMS.Controllers;

public class CategoriesController : Controller
{
    private readonly AppDbContext _db;
    public CategoriesController(AppDbContext db) => _db = db;

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Index()
    {
        var cats = await _db.ItemCategories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
        return View(cats);
    }

    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.MasterCategoryManage)]
    public IActionResult Create() => View(new ItemCategory());

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.MasterCategoryManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ItemCategory cat)
    {
        if (await _db.ItemCategories.AnyAsync(c => c.CategoryCode == cat.CategoryCode))
        {
            TempData["Error"] = $"Mã danh mục '{cat.CategoryCode}' đã tồn tại. Vui lòng chọn mã khác.";
            return View(cat);
        }

        cat.CreatedAt = VietnamTime.Now;
        _db.ItemCategories.Add(cat);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm danh mục '{cat.CategoryName}'.";
        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.MasterCategoryManage)]
    public async Task<IActionResult> Edit(int id)
    {
        var cat = await _db.ItemCategories.FindAsync(id);
        return cat == null ? NotFound() : View(cat);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.MasterCategoryManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ItemCategory cat)
    {
        var existing = await _db.ItemCategories.FindAsync(id);
        if (existing == null) return NotFound();

        if (await _db.ItemCategories.AnyAsync(c => c.CategoryCode == cat.CategoryCode && c.CategoryId != id))
        {
            TempData["Error"] = $"Mã danh mục '{cat.CategoryCode}' đã tồn tại. Vui lòng chọn mã khác.";
            return View(cat);
        }

        existing.CategoryCode = cat.CategoryCode;
        existing.CategoryName = cat.CategoryName;
        existing.SortOrder = cat.SortOrder;
        existing.UpdatedAt = VietnamTime.Now;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật danh mục.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Authorize(Policy = WmsPermissions.MasterCategoryManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await _db.ItemCategories.FindAsync(id);
        if (cat == null) return NotFound();

        var hasItems = await _db.Items.AnyAsync(i => i.CategoryId == id && i.IsActive);
        if (hasItems)
        {
            TempData["Error"] = $"Không thể xóa danh mục '{cat.CategoryName}' vì đang có vật tư thuộc danh mục này.";
            return RedirectToAction("Index");
        }

        cat.IsActive = false;
        cat.UpdatedAt = VietnamTime.Now;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa danh mục '{cat.CategoryName}'.";
        return RedirectToAction("Index");
    }
}
