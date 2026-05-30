using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Common;
using WMS.Models;
using WMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using WMS.Authorization;
using WMS.Services;

namespace WMS.Controllers;

[Authorize]
public class ItemsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IInventoryBalanceService _inventoryBalanceService;

    private bool CanSeeFinancial()
        => User.Claims.Any(c =>
            c.Type == PermissionClaimTypes.Permission &&
            string.Equals(c.Value, WmsPermissions.ReportViewFinancial, StringComparison.Ordinal));

    public ItemsController(AppDbContext db, IWebHostEnvironment env, IInventoryBalanceService? inventoryBalanceService = null)
    {
        _db = db;
        _env = env;
        _inventoryBalanceService = inventoryBalanceService ?? new InventoryBalanceService(db);
    }

    private int? GetScopedWarehouseId()
    {
        if (User.IsInRole("Admin")) return null;
        var claim = User.FindFirst("WarehouseId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    public async Task<IActionResult> Index(string? search, int? categoryId, ItemTypeEnum? itemType, string? stockStatus)
    {
        var query = _db.Items.Include(i => i.Category).Include(i => i.BaseUom)
            .Where(i => i.IsActive).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(i => i.ItemCode.Contains(search) || i.ItemName.Contains(search) || (i.Barcode != null && i.Barcode.Contains(search)));
        if (categoryId.HasValue)
            query = query.Where(i => i.CategoryId == categoryId.Value);
        if (itemType.HasValue)
            query = query.Where(i => i.ItemType == itemType.Value);

        var items = await query.OrderBy(i => i.ItemCode).ToListAsync();
        var scopedWarehouseId = GetScopedWarehouseId();
        var stockByItem = await _inventoryBalanceService.GetStockByItemAsync(scopedWarehouseId, items.Select(i => i.ItemId));
        _inventoryBalanceService.ApplyStockBalances(items, stockByItem);

        if (stockStatus == "low")
            items = items.Where(i => i.StockStatus == "Sắp Hết" || i.StockStatus == "Hết Hàng").ToList();
        else if (stockStatus == "out")
            items = items.Where(i => i.StockStatus == "Hết Hàng").ToList();
        else if (stockStatus == "over")
            items = items.Where(i => i.StockStatus == "Vượt Định Mức").ToList();

        ViewBag.Categories = await _db.ItemCategories.Where(c => c.IsActive).ToListAsync();
        ViewBag.Uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).OrderBy(u => u.UomCode).ToListAsync();
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.ItemType = itemType;
        ViewBag.StockStatus = stockStatus;

        return View(items);
    }

    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.MasterItemManage)]
    public async Task<IActionResult> Create()
    {
        var count = await _db.Items.Where(i => i.ItemType == ItemTypeEnum.NguyenVatLieu).CountAsync();
        var newCode = $"NVL-{(count + 1):D3}";

        var occupied = _db.ItemLocations.Where(il => il.Quantity > 0).Select(il => il.LocationId)
            .Union(_db.Items.Where(i => i.IsActive && i.DefaultLocationId.HasValue).Select(i => i.DefaultLocationId!.Value));

        var vm = new ItemFormViewModel
        {
            Item = new Item { ItemCode = newCode, ItemType = ItemTypeEnum.NguyenVatLieu },
            Categories = await _db.ItemCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync(),
            Uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).OrderBy(u => u.UomCode).ToListAsync(),
            Locations = await _db.Locations.Where(l => l.IsActive && !occupied.Contains(l.LocationId)).OrderBy(l => l.LocationCode).ToListAsync()
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> GenerateItemCode(ItemTypeEnum itemType)
    {
        string prefix = itemType switch
        {
            ItemTypeEnum.NguyenVatLieu => "NVL",
            ItemTypeEnum.HoaChat => "HC",
            _ => "VT"
        };

        var existingCodes = await _db.Items
            .Where(i => i.ItemType == itemType && i.ItemCode.StartsWith(prefix + "-"))
            .Select(i => i.ItemCode)
            .ToListAsync();

        int maxNum = 0;
        foreach (var code in existingCodes)
        {
            var parts = code.Split('-');
            if (parts.Length >= 2 && int.TryParse(parts.Last(), out var num) && num > maxNum)
                maxNum = num;
        }

        var newCode = $"{prefix}-{(maxNum + 1):D3}";
        return Json(new { code = newCode });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.MasterItemManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ItemFormViewModel vm, IFormFile? ImageFile)
    {
        var item = vm.Item;
        var catchWeightError = ValidateCatchWeightConfig(item);
        if (catchWeightError != null)
        {
            TempData["Error"] = catchWeightError;
            vm.Categories = await _db.ItemCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();
            vm.Uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).OrderBy(u => u.UomCode).ToListAsync();
            vm.Locations = await _db.Locations.Where(l => l.IsActive).OrderBy(l => l.LocationCode).ToListAsync();
            return View(vm);
        }

        if (await _db.Items.AnyAsync(i => i.ItemCode == item.ItemCode))
        {
            TempData["Error"] = $"Mã vật tư '{item.ItemCode}' đã tồn tại. Vui lòng chọn mã khác.";
            vm.Categories = await _db.ItemCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();
            vm.Uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).OrderBy(u => u.UomCode).ToListAsync();
            vm.Locations = await _db.Locations.Where(l => l.IsActive).OrderBy(l => l.LocationCode).ToListAsync();
            return View(vm);
        }

        if (string.IsNullOrWhiteSpace(item.Barcode))
        {
            item.Barcode = item.ItemCode;
        }

        if (ImageFile != null && ImageFile.Length > 0)
        {
            item.ImageUrl = await SaveImageFile(ImageFile);
        }

        item.CreatedAt = VietnamTime.Now;
        item.CreatedBy = User.Identity?.Name ?? "system";
        _db.Items.Add(item);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm vật tư '{item.ItemName}' thành công.";
        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.MasterItemManage)]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Items.FindAsync(id);
        if (item == null) return NotFound();

        var occupied = _db.ItemLocations.Where(il => il.Quantity > 0 && il.ItemId != id).Select(il => il.LocationId)
            .Union(_db.Items.Where(i => i.IsActive && i.DefaultLocationId.HasValue && i.ItemId != id).Select(i => i.DefaultLocationId!.Value));

        var vm = new ItemFormViewModel
        {
            Item = item,
            Categories = await _db.ItemCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync(),
            Uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).OrderBy(u => u.UomCode).ToListAsync(),
            Locations = await _db.Locations.Where(l => l.IsActive && !occupied.Contains(l.LocationId)).OrderBy(l => l.LocationCode).ToListAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.MasterItemManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ItemFormViewModel vm, IFormFile? ImageFile)
    {
        var existing = await _db.Items.FindAsync(id);
        if (existing == null) return NotFound();
        vm.Item.ItemCode = (vm.Item.ItemCode ?? "").Trim();

        if (await _db.Items.AnyAsync(i => i.ItemId != id && i.ItemCode == vm.Item.ItemCode))
        {
            TempData["Error"] = $"Mã vật tư '{vm.Item.ItemCode}' đã tồn tại. Vui lòng chọn mã khác.";
            var occupied = _db.ItemLocations.Where(il => il.Quantity > 0 && il.ItemId != id).Select(il => il.LocationId)
                .Union(_db.Items.Where(i => i.IsActive && i.DefaultLocationId.HasValue && i.ItemId != id).Select(i => i.DefaultLocationId!.Value));
            vm.Categories = await _db.ItemCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();
            vm.Uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).OrderBy(u => u.UomCode).ToListAsync();
            vm.Locations = await _db.Locations.Where(l => l.IsActive && (!occupied.Contains(l.LocationId) || l.LocationId == existing.DefaultLocationId)).OrderBy(l => l.LocationCode).ToListAsync();
            return View(vm);
        }

        existing.ItemCode = vm.Item.ItemCode;
        existing.ItemName = vm.Item.ItemName;
        existing.Barcode = string.IsNullOrWhiteSpace(vm.Item.Barcode) ? vm.Item.ItemCode : vm.Item.Barcode;
        existing.SkuCode = vm.Item.SkuCode;
        existing.CategoryId = vm.Item.CategoryId;
        existing.ItemType = vm.Item.ItemType;
        existing.BaseUomId = vm.Item.BaseUomId;
        existing.Weight = vm.Item.Weight;
        existing.ReorderPoint = vm.Item.ReorderPoint;
        existing.TrackExpiry = vm.Item.TrackExpiry;
        existing.TrackLot = vm.Item.TrackLot;
        existing.TrackSerial = vm.Item.TrackSerial;
        var catchWeightError = ValidateCatchWeightConfig(vm.Item);
        if (catchWeightError != null)
        {
            TempData["Error"] = catchWeightError;
            var occupied = _db.ItemLocations.Where(il => il.Quantity > 0 && il.ItemId != id).Select(il => il.LocationId)
                .Union(_db.Items.Where(i => i.IsActive && i.DefaultLocationId.HasValue && i.ItemId != id).Select(i => i.DefaultLocationId!.Value));
            vm.Categories = await _db.ItemCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();
            vm.Uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).OrderBy(u => u.UomCode).ToListAsync();
            vm.Locations = await _db.Locations.Where(l => l.IsActive && (!occupied.Contains(l.LocationId) || l.LocationId == existing.DefaultLocationId)).OrderBy(l => l.LocationCode).ToListAsync();
            return View(vm);
        }
        existing.TrackCatchWeight = vm.Item.TrackCatchWeight;
        existing.CatchWeightUomId = vm.Item.TrackCatchWeight ? vm.Item.CatchWeightUomId : null;
        existing.NominalWeightPerBaseUnit = vm.Item.TrackCatchWeight ? vm.Item.NominalWeightPerBaseUnit : null;
        existing.CatchWeightTolerancePercent = vm.Item.TrackCatchWeight ? vm.Item.CatchWeightTolerancePercent : null;
        existing.RequireCatchWeightAtReceive = vm.Item.TrackCatchWeight && vm.Item.RequireCatchWeightAtReceive;
        existing.RequireCatchWeightAtPickPack = vm.Item.TrackCatchWeight && vm.Item.RequireCatchWeightAtPickPack;
        existing.Description = vm.Item.Description;
        existing.Specifications = vm.Item.Specifications;
        existing.MinThreshold = vm.Item.MinThreshold;
        existing.MaxThreshold = vm.Item.MaxThreshold;
        existing.DefaultLocationId = vm.Item.DefaultLocationId;
        existing.UpdatedAt = VietnamTime.Now;

        if (ImageFile != null && ImageFile.Length > 0)
        {
            existing.ImageUrl = await SaveImageFile(ImageFile);
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật vật tư '{existing.ItemName}'.";
        return RedirectToAction("Index");
    }

    private static string? ValidateCatchWeightConfig(Item item)
    {
        if (!item.TrackCatchWeight)
            return null;
        if (!item.CatchWeightUomId.HasValue)
            return "Vật tư bật cân trọng lượng thực tế phải chọn đơn vị cân.";
        if (!item.NominalWeightPerBaseUnit.HasValue || item.NominalWeightPerBaseUnit.Value <= 0)
            return "Vật tư bật cân trọng lượng thực tế phải nhập trọng lượng danh nghĩa trên mỗi đơn vị base.";
        if (item.CatchWeightTolerancePercent.HasValue && item.CatchWeightTolerancePercent.Value < 0)
            return "Dung sai cân trọng lượng thực tế không được âm.";
        if (!item.RequireCatchWeightAtReceive && !item.RequireCatchWeightAtPickPack)
            return "Vật tư bật cân trọng lượng thực tế phải yêu cầu cân khi nhập hoặc khi lấy hàng, đóng gói.";
        return null;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Authorize(Policy = WmsPermissions.MasterItemManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Items.FindAsync(id);
        if (item == null) return NotFound();

        // P0-03: Check actual stock from ItemLocation (source of truth) instead of Item.CurrentStock shadow field
        var actualStock = await _db.ItemLocations
            .Where(il => il.ItemId == id)
            .SumAsync(il => il.Quantity);
        if (actualStock > 0)
        {
            TempData["Error"] = $"Không thể xóa vật tư '{item.ItemName}' vì vẫn còn tồn kho ({actualStock:N0}). Vui lòng xuất hết hàng trước.";
            return RedirectToAction("Index");
        }

        var hasPendingVouchers = await _db.VoucherDetails
            .Include(vd => vd.Voucher)
            .AnyAsync(vd => vd.ItemId == id && vd.Voucher != null && !vd.Voucher.IsPosted && !vd.Voucher.IsCancelled);
        if (hasPendingVouchers)
        {
            TempData["Error"] = $"Không thể xóa vật tư '{item.ItemName}' vì đang có phiếu chưa ghi sổ liên quan.";
            return RedirectToAction("Index");
        }

        item.IsActive = false;
        item.UpdatedAt = VietnamTime.Now;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa vật tư '{item.ItemName}'.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Details(int id)
    {
        var canSeeFinancial = CanSeeFinancial();
        var scopedWarehouseId = GetScopedWarehouseId();
        var item = await _db.Items
            .Include(i => i.Category).Include(i => i.BaseUom).Include(i => i.CatchWeightUom)
            .FirstOrDefaultAsync(i => i.ItemId == id);
        if (item == null) return NotFound();

        var itemStock = await _inventoryBalanceService.GetStockByItemAsync(scopedWarehouseId, new[] { id });
        _inventoryBalanceService.ApplyStockBalances(new[] { item }, itemStock);

        var locationQuery = _db.ItemLocations
            .Include(il => il.Location).ThenInclude(l => l!.Zone).ThenInclude(z => z!.Warehouse)
            .Where(il => il.ItemId == id && il.Quantity > 0)
            .AsQueryable();
        if (scopedWarehouseId.HasValue)
            locationQuery = locationQuery.Where(il => il.Location != null && il.Location.Zone != null && il.Location.Zone.WarehouseId == scopedWarehouseId.Value);

        ViewBag.Locations = await locationQuery.ToListAsync();

        ViewBag.RecentVouchers = await _db.VoucherDetails
            .Include(vd => vd.Voucher).ThenInclude(v => v!.Warehouse)
            .Where(vd => vd.ItemId == id)
            .OrderByDescending(vd => vd.Voucher!.VoucherDate)
            .Take(20).ToListAsync();

        ViewBag.CanSeeFinancial = canSeeFinancial;

        if (!canSeeFinancial)
        {
            item.UnitCost = 0m;
            item.TotalStockValue = 0m;

            if (ViewBag.RecentVouchers is List<VoucherDetail> rv)
            {
                foreach (var d in rv)
                {
                    d.UnitPrice = 0m;
                    d.LineAmount = 0m;
                }
            }
        }

        return View(item);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> GetItemJson(int id)
    {
        var canSeeFinancial = CanSeeFinancial();
        var item = await _db.Items.Include(i => i.BaseUom).FirstOrDefaultAsync(i => i.ItemId == id);
        if (item == null) return NotFound();
        return Json(new
        {
            item.ItemId,
            item.ItemCode,
            item.ItemName,
            UnitCost = canSeeFinancial ? item.UnitCost : 0m,
            item.Weight,
            BaseUom = item.BaseUom?.UomCode,
            BaseUomId = item.BaseUomId
        });
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> GetItemByBarcode(string barcode)
    {
        var canSeeFinancial = CanSeeFinancial();
        if (string.IsNullOrWhiteSpace(barcode))
            return BadRequest(new { message = "Mã vạch trống." });

        var code = barcode.Trim();

        var item = await _db.Items.Include(i => i.BaseUom)
            .FirstOrDefaultAsync(i => i.IsActive && (
                i.Barcode == code ||
                i.ItemCode == code ||
                i.SkuCode == code
            ));

        if (item == null)
        {
            item = await _db.Items.Include(i => i.BaseUom)
                .FirstOrDefaultAsync(i => i.IsActive && (
                    (i.Barcode != null && i.Barcode.ToLower().Contains(code.ToLower())) ||
                    i.ItemCode.ToLower().Contains(code.ToLower()) ||
                    (i.SkuCode != null && i.SkuCode.ToLower().Contains(code.ToLower()))
                ));
        }

        if (item == null)
            return NotFound(new { message = $"Không tìm thấy vật tư với mã: {code}" });

        return Json(new
        {
            item.ItemId,
            item.ItemCode,
            item.ItemName,
            UnitCost = canSeeFinancial ? item.UnitCost : 0m,
            BaseUomId = item.BaseUomId,
            BaseUomCode = item.BaseUom?.UomCode,
            item.Barcode
        });
    }

    [HttpGet]
    public async Task<IActionResult> PrintSetup(string ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
        {
            TempData["Error"] = "Vui lòng chọn ít nhất 1 vật tư để in nhãn.";
            return RedirectToAction("Index");
        }

        var itemIds = ids.Split(',')
            .Where(s => int.TryParse(s, out _))
            .Select(int.Parse)
            .ToList();

        var items = await _db.Items
            .Include(i => i.BaseUom)
            .Include(i => i.DefaultLocation)
            .Where(i => itemIds.Contains(i.ItemId) && i.IsActive)
            .ToListAsync();

        if (!items.Any())
        {
            TempData["Error"] = "Không tìm thấy vật tư nào.";
            return RedirectToAction("Index");
        }

        var vm = new PrintLabelBatchViewModel
        {
            Items = items.Select(i => new PrintLabelItem
            {
                ItemId = i.ItemId,
                ItemCode = i.ItemCode,
                ItemName = i.ItemName,
                Barcode = string.IsNullOrEmpty(i.Barcode) ? i.ItemCode : i.Barcode,
                SkuCode = i.SkuCode,
                Unit = i.BaseUom?.UomName ?? i.BaseUom?.UomCode ?? "",
                LocationCode = i.DefaultLocation?.LocationCode ?? "Chưa cập nhật",
                PrintQuantity = 1
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult PrintLabels(PrintLabelBatchViewModel vm)
    {
        if (vm.Items == null || !vm.Items.Any())
        {
            TempData["Error"] = "Không có dữ liệu để in.";
            return RedirectToAction("Index");
        }

        var totalLabels = vm.Items.Sum(i => i.PrintQuantity);
        if (totalLabels > 500)
        {
            TempData["Error"] = $"Tổng số nhãn ({totalLabels}) vượt quá giới hạn 500. Vui lòng chia thành nhiều đợt.";
            return RedirectToAction("Index");
        }

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> PrintSingle(int id, int qty = 1, string labelSize = "50x30")
    {
        var item = await _db.Items
            .Include(i => i.BaseUom)
            .Include(i => i.DefaultLocation)
            .FirstOrDefaultAsync(i => i.ItemId == id && i.IsActive);

        if (item == null) return NotFound();

        var vm = new PrintLabelBatchViewModel
        {
            LabelSize = string.IsNullOrEmpty(labelSize) ? "50x30" : labelSize,
            Items = new List<PrintLabelItem>
            {
                new PrintLabelItem
                {
                    ItemId = item.ItemId,
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    Barcode = string.IsNullOrEmpty(item.Barcode) ? item.ItemCode : item.Barcode,
                    SkuCode = item.SkuCode,
                    Unit = item.BaseUom?.UomName ?? item.BaseUom?.UomCode ?? "",
                    LocationCode = item.DefaultLocation?.LocationCode ?? "Chưa cập nhật",
                    PrintQuantity = Math.Clamp(qty, 1, 500)
                }
            }
        };

        return View("PrintLabels", vm);
    }

    private async Task<string> SaveImageFile(IFormFile file)
    {
        try
        {
            var allowedExtensions = SecurityHelpers.FileUpload.AllowedImageExtensions;
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
                throw new InvalidOperationException("Định dạng file không được hỗ trợ.");

            // P1-5: enforce content-type — KHÔNG bypass khi rỗng để defense-in-depth song song với check extension.
            if (string.IsNullOrWhiteSpace(file.ContentType) ||
                !SecurityHelpers.FileUpload.AllowedImageMimeTypes.Contains(file.ContentType))
                throw new InvalidOperationException("Loại file không hợp lệ. Chỉ chấp nhận ảnh JPG/PNG/GIF/WEBP.");

            if (file.Length > 5 * 1024 * 1024)
                throw new InvalidOperationException("Kích thước file vượt quá 5MB.");

            // P2-3: kiểm tra magic bytes để đảm bảo file thực sự là ảnh, không chỉ extension/mime.
            using (var probeStream = file.OpenReadStream())
            {
                if (!SecurityHelpers.FileUpload.IsValidImageContent(probeStream))
                    throw new InvalidOperationException("Nội dung file không phải ảnh hợp lệ.");
            }

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "items");
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{VietnamTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}{extension}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/items/{fileName}";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Image upload failed: {ex.GetType().Name}");
            throw;
        }
    }
}
