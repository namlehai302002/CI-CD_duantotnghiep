using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WMS.Data;
using WMS.Common;
using WMS.Models;
using WMS.Authorization;
using WMS.ViewModels;

namespace WMS.Controllers;

[Authorize]
public class WarehousesController : Controller
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly IMemoryCache _cache;
    public WarehousesController(AppDbContext db, IConfiguration config, IWebHostEnvironment env, IMemoryCache cache)
    {
        _db = db;
        _config = config;
        _env = env;
        _cache = cache;
    }

    private async Task<List<AppUser>> GetManagerCandidatesAsync()
    {
        return await _db.AppUsers
            .Include(u => u.Role)
            .Where(u => u.IsActive && u.Role != null && (u.Role.RoleName == "Admin" || u.Role.RoleName == "Manager"))
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    private int? GetScopedWarehouseId()
    {
        if (User.IsInRole("Admin")) return null;
        var claim = User.FindFirst("WarehouseId")?.Value;
        return int.TryParse(claim, out var id) ? id : (int?)null;
    }

    public async Task<IActionResult> Index()
    {
        var scopedWh = GetScopedWarehouseId();
        var query = _db.Warehouses
            .Include(w => w.Zones)
            .Include(w => w.ManagerUser)
            .Where(w => w.IsActive)
            .AsQueryable();
        if (scopedWh.HasValue) query = query.Where(w => w.WarehouseId == scopedWh.Value);
        var warehouses = await query.ToListAsync();
        return View(warehouses);
    }

    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.WarehouseConfigManage)]
    public async Task<IActionResult> Create()
    {
        ViewBag.ManagerUsers = await GetManagerCandidatesAsync();
        return View(new Warehouse());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.WarehouseConfigManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Warehouse wh)
    {
        if (await _db.Warehouses.AnyAsync(w => w.WarehouseCode == wh.WarehouseCode))
        {
            ModelState.AddModelError("WarehouseCode", "Mã kho đã tồn tại.");
            ViewBag.ManagerUsers = await GetManagerCandidatesAsync();
            return View(wh);
        }

        if (wh.ManagerUserId.HasValue)
        {
            var manager = await _db.AppUsers.FindAsync(wh.ManagerUserId.Value);
            wh.ManagerName = manager?.FullName;
        }
        else
        {
            wh.ManagerName = null;
        }

        wh.CreatedAt = VietnamTime.Now;
        _db.Warehouses.Add(wh);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm kho '{wh.WarehouseName}'.";
        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.WarehouseConfigManage)]
    public async Task<IActionResult> Edit(int id)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && id != scopedWh.Value) return Forbid();

        var wh = await _db.Warehouses.FindAsync(id);
        if (wh == null) return NotFound();
        ViewBag.ManagerUsers = await GetManagerCandidatesAsync();
        return View(wh);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.WarehouseConfigManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Warehouse wh)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && id != scopedWh.Value) return Forbid();

        var existing = await _db.Warehouses.FindAsync(id);
        if (existing == null) return NotFound();
        existing.WarehouseCode = wh.WarehouseCode;
        existing.WarehouseName = wh.WarehouseName;
        existing.Address = wh.Address;
        existing.ManagerUserId = wh.ManagerUserId;
        if (wh.ManagerUserId.HasValue)
        {
            var manager = await _db.AppUsers.FindAsync(wh.ManagerUserId.Value);
            existing.ManagerName = manager?.FullName;
        }
        else
        {
            existing.ManagerName = null;
        }
        existing.Phone = wh.Phone;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật thông tin kho.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Details(int id)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && id != scopedWh.Value) return Forbid();

        var wh = await _db.Warehouses
            .Include(w => w.Zones).ThenInclude(z => z.Locations)
            .Include(w => w.ManagerUser)
            .FirstOrDefaultAsync(w => w.WarehouseId == id);
        if (wh == null) return NotFound();
        return View(wh);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.WarehouseConfigManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && id != scopedWh.Value) return Forbid();

        var wh = await _db.Warehouses.FindAsync(id);
        if (wh == null) return NotFound();

        bool hasStock = await _db.ItemLocations
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .AnyAsync(il => il.Quantity > 0
                && il.Location != null
                && il.Location.Zone != null
                && il.Location!.Zone!.WarehouseId == id);

        if (hasStock)
        {
            TempData["Error"] = $"Không thể xóa kho '{wh.WarehouseName}' vì vẫn còn vật tư/hàng hóa bên trong. Vui lòng xuất hoặc chuyển kho trước.";
            return RedirectToAction("Index");
        }

        wh.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã ẩn kho '{wh.WarehouseName}' khỏi danh sách thao tác.";
        return RedirectToAction("Index");
    }

    // Zone CRUD
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.WarehouseConfigManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateZone(int warehouseId, string zoneCode, string zoneName, ZoneTypeEnum zoneType)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && warehouseId != scopedWh.Value) return Forbid();

        _db.Zones.Add(new Zone { WarehouseId = warehouseId, ZoneCode = zoneCode, ZoneName = zoneName, ZoneType = zoneType });
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm khu vực '{zoneName}'.";
        return RedirectToAction("Details", new { id = warehouseId });
    }

    // Thêm Khu mới với Locations mặc định (từ InventoryMap modal)
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.WarehouseConfigManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateZoneWithLocations(int warehouseId, string zoneCode, string zoneName, ZoneTypeEnum zoneType, string? description)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && warehouseId != scopedWh.Value) return Forbid();

        if (string.IsNullOrWhiteSpace(zoneCode) || string.IsNullOrWhiteSpace(zoneName))
        {
            TempData["Error"] = "Vui lòng nhập đầy đủ tên khu và mã khu.";
            return RedirectToAction("InventoryMap", new { id = warehouseId });
        }

        if (await _db.Zones.AnyAsync(z => z.WarehouseId == warehouseId && z.ZoneCode == zoneCode))
        {
            TempData["Error"] = $"Mã khu '{zoneCode}' đã tồn tại trong kho này.";
            return RedirectToAction("InventoryMap", new { id = warehouseId });
        }

        var zone = new Zone
        {
            WarehouseId = warehouseId,
            ZoneCode = zoneCode,
            ZoneName = zoneName,
            ZoneType = zoneType,
            IsActive = true
        };
        _db.Zones.Add(zone);
        await _db.SaveChangesAsync();

        // Tạo locations mặc định dựa trên loại khu
        if (zoneType == ZoneTypeEnum.Storage) // Khu kệ chứa hàng: 6 tầng × 1 ô = 6 ô (2 tấn/ô)
        {
            for (int level = 1; level <= 6; level++)
            {
                var locCode = $"{zoneCode}-{level:D2}-01";
                _db.Locations.Add(new Location
                {
                    ZoneId = zone.ZoneId,
                    LocationCode = locCode,
                    RackCode = zoneCode,
                    ShelfCode = level.ToString(),
                    BinCode = "1",
                    HeightLevel = level,
                    IsGoldenZone = level >= 2 && level <= 4,
                    WeightLimitKg = level >= 5 ? 23m : (level >= 3 ? 50m : null),
                    MaxWeightCapacityKg = 2000m,

                    CurrentLoad = 0,
                    MaxCapacity = 2000m,
                    IsActive = true
                });
            }
        }
        else // Khu chất lỏng: 4 bồn
        {
            for (int tank = 1; tank <= 4; tank++)
            {
                var locCode = $"{zoneCode}-Tank{tank:D2}";
                _db.Locations.Add(new Location
                {
                    ZoneId = zone.ZoneId,
                    LocationCode = locCode,
                    RackCode = zoneCode,
                    ShelfCode = "1",
                    BinCode = tank.ToString(),
                    HeightLevel = 1,

                    CurrentLoad = 0,
                    MaxCapacity = 50000m,
                    IsActive = true
                });
            }
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm khu '{zoneName}' và tạo sẵn các vị trí mặc định.";
        return RedirectToAction("InventoryMap", new { id = warehouseId });
    }

    // Location CRUD
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.WarehouseConfigManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLocation(int zoneId, string locationCode, string? rackCode, string? shelfCode, string? binCode)
    {
        var zone = await _db.Zones.FindAsync(zoneId);
        if (zone == null) return NotFound();
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && zone.WarehouseId != scopedWh.Value) return Forbid();

        if (await _db.Locations.AnyAsync(l => l.LocationCode == locationCode))
        {
            TempData["Error"] = $"Mã vị trí '{locationCode}' đã tồn tại. Vui lòng chọn mã khác.";
            return RedirectToAction("InventoryMap", new { id = zone.WarehouseId });
        }

        var heightLevel = ParseHeightLevel(shelfCode);
        _db.Locations.Add(new Location
        {
            ZoneId = zoneId,
            LocationCode = locationCode,
            RackCode = rackCode,
            ShelfCode = shelfCode,
            BinCode = binCode,
            HeightLevel = heightLevel,
            IsGoldenZone = heightLevel >= 2 && heightLevel <= 4,
            WeightLimitKg = heightLevel >= 5 ? 23m : (heightLevel >= 3 ? 50m : null),
            MaxWeightCapacityKg = zone.ZoneType == ZoneTypeEnum.Storage ? 2000m : null,
            MaxCapacity = zone.ZoneType == ZoneTypeEnum.Storage ? 2000m : 50000m
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm vị trí '{locationCode}'.";
        return RedirectToAction("InventoryMap", new { id = zone.WarehouseId });
    }

    private static int ParseHeightLevel(string? shelfCode)
    {
        if (int.TryParse(shelfCode, out var level) && level > 0)
            return level;

        return 1;
    }



    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> GetLocationStock(int locationId)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            var wh = await _db.Locations
                .Include(l => l.Zone)
                .Where(l => l.LocationId == locationId && l.Zone != null)
                .Select(l => (int?)l.Zone!.WarehouseId)
                .FirstOrDefaultAsync();
            if (!wh.HasValue || wh.Value != scopedWh.Value) return Forbid();
        }

        var stock = await _db.ItemLocations
            .Include(il => il.Item).ThenInclude(i => i!.BaseUom)
            .Where(il => il.LocationId == locationId && il.Quantity > 0)
            .Select(il => new
            {
                ItemCode = il.Item != null ? il.Item.ItemCode : "Chưa cập nhật",
                ItemName = il.Item != null ? il.Item.ItemName : "Chưa cập nhật",
                Quantity = il.Quantity,
                Uom = (il.Item != null && il.Item.BaseUom != null) ? il.Item.BaseUom.UomCode : "Không áp dụng"
            })
            .ToListAsync();

        return Json(stock);
    }

    // API: Lấy vị trí đang chứa vật tư (dành cho chức năng xuất kho)
    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> GetItemLocations(int itemId, int? warehouseId)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;
        if (!warehouseId.HasValue)
        {
            return BadRequest(new { message = "Chưa xác định được kho để lấy vị trí vật tư." });
        }
        var locs = await _db.ItemLocations
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Where(il => il.ItemId == itemId
                && il.Quantity > 0
                && il.Location != null
                && il.Location.IsActive
                && (!warehouseId.HasValue || (il.Location.Zone != null && il.Location.Zone.WarehouseId == warehouseId.Value)))
            // FEFO sort: nearest expiry first (nulls last), then larger qty
            .OrderBy(il => il.ExpiryDate == null)
            .ThenBy(il => il.ExpiryDate)
            .ThenByDescending(il => il.Quantity)
            .Select(il => new
            {
                locationId = il.LocationId,
                locationCode = il.Location!.LocationCode,
                zoneName = il.Location.Zone != null ? il.Location.Zone.ZoneName : "",
                quantity = il.Quantity,
                expiryDate = il.ExpiryDate,
                lotNumber = il.LotNumber
            })
            .ToListAsync();
        return Json(locs);
    }

    // API: Kiểm tra xem vị trí có chứa vật tư khác không (quy tắc 1 ô 1 vật tư)
    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> CheckLocationConflict(int locationId, int itemId)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            var wh = await _db.Locations
                .Include(l => l.Zone)
                .Where(l => l.LocationId == locationId && l.Zone != null)
                .Select(l => (int?)l.Zone!.WarehouseId)
                .FirstOrDefaultAsync();
            if (!wh.HasValue || wh.Value != scopedWh.Value) return Forbid();
        }

        var otherItemsInLocation = await _db.ItemLocations
            .Where(il => il.LocationId == locationId
                      && il.ItemId != itemId
                      && il.Quantity > 0)
            .Include(il => il.Item)
            .FirstOrDefaultAsync();

        if (otherItemsInLocation != null)
        {
            var conflictName = otherItemsInLocation.Item != null ? $"[{otherItemsInLocation.Item.ItemCode}] {otherItemsInLocation.Item.ItemName}" : otherItemsInLocation.ItemId.ToString();
            return Json(new { conflict = true, conflictItemName = conflictName });
        }

        return Json(new { conflict = false });
    }

    // API: Lấy danh sách LocationId của tất cả dòng trong một phiếu (để highlight trên sơ đồ)
    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> GetVoucherLocations(long voucherId)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            var voucherWh = await _db.Vouchers
                .Where(v => v.VoucherId == voucherId)
                .Select(v => (int?)v.WarehouseId)
                .FirstOrDefaultAsync();
            if (!voucherWh.HasValue || voucherWh.Value != scopedWh.Value) return Forbid();
        }

        var locationIds = await _db.VoucherDetails
            .Where(d => d.VoucherId == voucherId && d.LocationId != null)
            .Select(d => d.LocationId!.Value)
            .Distinct()
            .ToListAsync();

        // Cũng lấy DestLocationId nếu có (phiếu chuyển kho)
        var destLocationIds = await _db.VoucherDetails
            .Where(d => d.VoucherId == voucherId && d.DestLocationId != null)
            .Select(d => d.DestLocationId!.Value)
            .Distinct()
            .ToListAsync();

        var allIds = locationIds.Union(destLocationIds).Distinct().ToList();
        return Json(allIds);
    }

    // API: Gợi ý vị trí cho nhập kho
    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> GetSuggestedLocations(int itemId, decimal qty, int? warehouseId)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;
        if (!warehouseId.HasValue)
        {
            return BadRequest(new { message = "Chưa xác định được kho để gợi ý vị trí." });
        }

        // Lấy tất cả locations thuộc warehouse (nếu có)
        var locQuery = _db.Locations
            .Include(l => l.Zone)
            .Include(l => l.ItemLocations).ThenInclude(il => il.Item)
            .Where(l => l.IsActive);

        if (warehouseId.HasValue)
        {
            locQuery = locQuery.Where(l => l.Zone != null && l.Zone.WarehouseId == warehouseId.Value);
        }

        var locations = await locQuery.ToListAsync();
        var item = await _db.Items.FindAsync(itemId);

        if (item != null)
        {
            if (item.ItemType == ItemTypeEnum.HoaChat)
            {
                locations = locations.Where(l => l.LocationCode.Contains("Tank") || (l.Zone != null && l.Zone.ZoneType == ZoneTypeEnum.Staging)).ToList();
            }
            else
            {
                locations = locations.Where(l => !l.LocationCode.Contains("Tank") && (l.Zone == null || (l.Zone.ZoneType != ZoneTypeEnum.Shipping && l.Zone.ZoneType != ZoneTypeEnum.Staging))).ToList();
            }
        }

        var result = locations.Select(loc =>
        {
            var isLiquid = loc.LocationCode.Contains("Tank", StringComparison.OrdinalIgnoreCase)
                || loc.Zone?.ZoneType == ZoneTypeEnum.Staging;
            var currentLoad = loc.ItemLocations?.Where(il => il.Quantity > 0).Sum(il => isLiquid ? il.Quantity : il.Quantity * (il.Item?.Weight ?? 1m)) ?? 0m;
            var hasSameItem = loc.ItemLocations?.Any(il => il.ItemId == itemId && il.Quantity > 0) ?? false;
            var hasOtherItem = loc.ItemLocations?.Any(il => il.ItemId != itemId && il.Quantity > 0) ?? false;
            var currentItemName = loc.ItemLocations?.FirstOrDefault(il => il.Quantity > 0)?.Item?.ItemName;

            var maxCapacity = isLiquid ? 50000m : 2000m;
            var available = maxCapacity - currentLoad;
            if (available < 0) available = 0;
            var fillPercent = Math.Min(100, Math.Round((currentLoad / maxCapacity) * 100));

            return new
            {
                locationId = loc.LocationId,
                locationCode = loc.LocationCode,
                zoneName = loc.Zone?.ZoneName ?? "",
                currentLoad = currentLoad,
                available = available,
                fillPercent = fillPercent,
                hasSameItem = hasSameItem,
                hasOtherItem = hasOtherItem,
                currentItemName = currentItemName ?? "",
                uom = item?.BaseUom?.UomCode ?? (loc.Zone?.ZoneType == ZoneTypeEnum.Storage ? "kg" : "L")
            };
        })
        .Where(x => !x.hasOtherItem && x.available > 0) // Loại bỏ ô đã chứa vật tư khác VÀ ô đã đầy
        // Ưu tiên: 1. Ô đã chứa cùng vật tư nhưng còn trống, 2. Ô trống nhiều nhất
        .OrderByDescending(x => x.hasSameItem)
        .ThenByDescending(x => x.available)
        .ToList();

        return Json(result);
    }

    private static InventoryMapPageViewModel BuildInventoryMapViewModel(Warehouse? selectedWarehouse, List<Warehouse> warehouses, List<Voucher> recentVouchers)
    {
        var vm = new InventoryMapPageViewModel
        {
            Warehouses = warehouses,
            RecentVouchers = recentVouchers
        };

        if (selectedWarehouse == null)
        {
            return vm;
        }

        vm.SelectedWarehouseId = selectedWarehouse.WarehouseId;
        vm.SelectedWarehouseCode = selectedWarehouse.WarehouseCode;
        vm.SelectedWarehouseName = selectedWarehouse.WarehouseName;

        foreach (var zone in selectedWarehouse.Zones.Where(z => z.IsActive).OrderBy(z => z.ZoneCode))
        {
            var zoneVm = new InventoryMapZoneViewModel
            {
                ZoneId = zone.ZoneId,
                ZoneCode = zone.ZoneCode,
                ZoneName = zone.ZoneName,
                ZoneType = zone.ZoneType
            };

            var tiles = zone.Locations
                .Where(l => l.IsActive)
                .OrderBy(l => l.AisleSequence)
                .ThenBy(l => l.AisleCode)
                .ThenBy(l => l.RackCode)
                .ThenBy(l => ParseHeightLevel(l.ShelfCode))
                .ThenBy(l => l.BinCode)
                .ThenBy(l => l.LocationCode)
                .Select(location => BuildInventoryLocationTile(zone, location))
                .ToList();

            zoneVm.Aisles = tiles
                .GroupBy(t => string.IsNullOrWhiteSpace(t.AisleCode) ? "Lối chính" : t.AisleCode)
                .Select(aisle => new InventoryMapAisleGroup
                {
                    AisleCode = aisle.Key,
                    Racks = aisle
                        .GroupBy(t => string.IsNullOrWhiteSpace(t.RackCode) ? zone.ZoneCode : t.RackCode)
                        .Select(rack => new InventoryMapRackGroup
                        {
                            RackCode = rack.Key,
                            Locations = rack.ToList()
                        })
                        .OrderBy(r => r.RackCode)
                        .ToList()
                })
                .OrderBy(a => a.AisleCode)
                .ToList();

            vm.Zones.Add(zoneVm);
        }

        var allLocations = vm.Zones.SelectMany(z => z.Aisles).SelectMany(a => a.Racks).SelectMany(r => r.Locations).ToList();
        vm.TotalLocations = allLocations.Count;
        vm.OccupiedLocations = allLocations.Count(l => l.IsOccupied);
        vm.WarningLocations = allLocations.Count(l => l.StatusKey == "warning");
        vm.CriticalLocations = allLocations.Count(l => l.StatusKey == "critical");
        vm.HoldLocations = allLocations.Count(l => l.StatusKey == "hold");
        vm.TotalCapacity = allLocations.Sum(l => l.MaxCapacity);
        vm.UsedCapacity = allLocations.Sum(l => l.CurrentLoad);

        return vm;
    }

    private static InventoryMapLocationTile BuildInventoryLocationTile(Zone zone, Location location)
    {
        var isLiquid = zone.ZoneType != ZoneTypeEnum.Storage
            || location.LocationCode.Contains("Tank", StringComparison.OrdinalIgnoreCase);
        var stockLines = location.ItemLocations
            .Where(il => il.Quantity > 0)
            .OrderBy(il => il.Item?.ItemCode)
            .ThenBy(il => il.LotNumber)
            .Select(il => new InventoryMapStockLine
            {
                ItemCode = il.Item?.ItemCode ?? "Chưa cập nhật",
                ItemName = il.Item?.ItemName ?? "Chưa cập nhật",
                Uom = il.Item?.BaseUom?.UomCode ?? (isLiquid ? "L" : "ĐVT"),
                Quantity = il.Quantity,
                ReservedQty = il.ReservedQty,
                AvailableQty = il.AvailableQty,
                LotNumber = il.LotNumber,
                ExpiryDate = il.ExpiryDate,
                HoldStatus = il.HoldStatus
            })
            .ToList();

        var currentLoad = location.ItemLocations
            .Where(il => il.Quantity > 0)
            .Sum(il => isLiquid ? il.Quantity : il.Quantity * (il.Item?.Weight ?? 1m));
        var reservedLoad = location.ItemLocations
            .Where(il => il.ReservedQty > 0)
            .Sum(il => isLiquid ? il.ReservedQty : il.ReservedQty * (il.Item?.Weight ?? 1m));
        var maxCapacity = location.MaxCapacity > 0 ? location.MaxCapacity : (isLiquid ? 50000m : 2000m);
        var rawFill = maxCapacity > 0 ? (currentLoad / maxCapacity) * 100m : 0m;
        var hold = stockLines.FirstOrDefault(l => l.HoldStatus != InventoryHoldStatusEnum.Available)?.HoldStatus;
        var primary = stockLines.FirstOrDefault();
        var statusKey = ResolveInventoryTileStatusKey(currentLoad, rawFill, hold);

        return new InventoryMapLocationTile
        {
            LocationId = location.LocationId,
            LocationCode = location.LocationCode,
            AisleCode = string.IsNullOrWhiteSpace(location.AisleCode) ? "Lối chính" : location.AisleCode,
            RackCode = string.IsNullOrWhiteSpace(location.RackCode) ? zone.ZoneCode : location.RackCode,
            ShelfCode = string.IsNullOrWhiteSpace(location.ShelfCode) ? "-" : location.ShelfCode,
            BinCode = string.IsNullOrWhiteSpace(location.BinCode) ? "-" : location.BinCode,
            HeightLevel = location.HeightLevel,
            IsGoldenZone = location.IsGoldenZone,
            CurrentLoad = currentLoad,
            ReservedLoad = reservedLoad,
            MaxCapacity = maxCapacity,
            FillPercent = Math.Round(Math.Min(rawFill, 999m), 1),
            SkuCount = stockLines.Select(l => l.ItemCode).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            PrimaryItemCode = primary?.ItemCode ?? "",
            PrimaryItemName = primary?.ItemName ?? "",
            HoldStatus = hold,
            StatusKey = statusKey,
            StatusLabel = ResolveInventoryTileStatusLabel(statusKey),
            StockLines = stockLines
        };
    }

    private static string ResolveInventoryTileStatusKey(decimal currentLoad, decimal fillPercent, InventoryHoldStatusEnum? holdStatus)
    {
        if (holdStatus.HasValue)
        {
            return "hold";
        }
        if (currentLoad <= 0)
        {
            return "empty";
        }
        if (fillPercent >= 90)
        {
            return "critical";
        }
        if (fillPercent >= 70)
        {
            return "warning";
        }
        return "normal";
    }

    private static string ResolveInventoryTileStatusLabel(string statusKey) => statusKey switch
    {
        "hold" => "Đang giữ",
        "critical" => "Đầy / quá tải",
        "warning" => "Gần đầy",
        "normal" => "Đang dùng",
        _ => "Trống"
    };

    public async Task<IActionResult> InventoryMap(int? id)
    {
        var warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.WarehouseCode).ToListAsync();

        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            id = scopedWh.Value;
            warehouses = warehouses.Where(w => w.WarehouseId == scopedWh.Value).ToList();
        }

        Warehouse? selectedWarehouse = null;
        if (id.HasValue)
        {
            selectedWarehouse = await _db.Warehouses
                .Include(w => w.Zones.Where(z => z.IsActive))
                .ThenInclude(z => z.Locations.Where(l => l.IsActive))
                .ThenInclude(l => l.ItemLocations)
                .ThenInclude(il => il.Item)
                .ThenInclude(i => i!.BaseUom)
                .FirstOrDefaultAsync(w => w.WarehouseId == id.Value);
        }
        else if (warehouses.Any())
        {
            selectedWarehouse = await _db.Warehouses
                .Include(w => w.Zones.Where(z => z.IsActive))
                .ThenInclude(z => z.Locations.Where(l => l.IsActive))
                .ThenInclude(l => l.ItemLocations)
                .ThenInclude(il => il.Item)
                .ThenInclude(i => i!.BaseUom)
                .FirstOrDefaultAsync(w => w.WarehouseId == warehouses.First().WarehouseId);
        }

        var selectedWarehouseId = selectedWarehouse?.WarehouseId;
        var recentVouchers = await _db.Vouchers
            .Where(v => !v.IsCancelled
                && v.VoucherType == VoucherTypeEnum.NhapKho
                && (!selectedWarehouseId.HasValue || v.WarehouseId == selectedWarehouseId.Value))
            .OrderByDescending(v => v.VoucherDate)
            .Take(30)
            .ToListAsync();

        return View(BuildInventoryMapViewModel(selectedWarehouse, warehouses, recentVouchers));
    }
}
