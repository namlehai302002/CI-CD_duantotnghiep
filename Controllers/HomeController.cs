using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;
using WMS.Authorization;
using WMS.ViewModels;
using WMS.Common;
using WMS.Services;

namespace WMS.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly IInventoryBalanceService _inventoryBalanceService;
    private readonly IRoleWorkspaceService _roleWorkspaceService;

    private static DateTime VietnamNow => VietnamTime.Now;

    public HomeController(AppDbContext db, IInventoryBalanceService inventoryBalanceService, IRoleWorkspaceService? roleWorkspaceService = null)
    {
        _db = db;
        _inventoryBalanceService = inventoryBalanceService;
        _roleWorkspaceService = roleWorkspaceService ?? new RoleWorkspaceService();
    }

    private bool CanSeeFinancial()
        => User.Claims.Any(c =>
            c.Type == PermissionClaimTypes.Permission &&
            string.Equals(c.Value, WmsPermissions.ReportViewFinancial, StringComparison.Ordinal));

    private int? GetScopedWarehouseId()
    {
        var warehouseClaim = User.FindFirst("WarehouseId")?.Value;
        return int.TryParse(warehouseClaim, out var warehouseId) ? warehouseId : null;
    }

    public async Task<IActionResult> Index()
    {
        var canSeeFinancial = CanSeeFinancial();
        ViewBag.CanSeeFinancial = canSeeFinancial;
        ViewBag.RoleWorkspace = _roleWorkspaceService.Build(User);

        var today = VietnamNow.Date;
        var scopedWh = GetScopedWarehouseId();
        var vm = scopedWh.HasValue
            ? await BuildScopedDashboardAsync(scopedWh.Value, today, canSeeFinancial)
            : await BuildEnterpriseDashboardAsync(today, canSeeFinancial);

        return View(vm);
    }

    private async Task<DashboardViewModel> BuildScopedDashboardAsync(int warehouseId, DateTime today, bool canSeeFinancial)
    {
        var stockMap = await _inventoryBalanceService.GetStockByItemAsync(warehouseId);

        var defaultLocationItemIds = await _db.Items.AsNoTracking()
            .Where(i => i.IsActive
                && i.DefaultLocationId.HasValue
                && i.DefaultLocation != null
                && i.DefaultLocation.Zone != null
                && i.DefaultLocation.Zone.WarehouseId == warehouseId)
            .Select(i => i.ItemId)
            .ToListAsync();

        var warehouseItemIds = stockMap.Keys
            .Union(defaultLocationItemIds)
            .Distinct()
            .ToList();

        var warehouseItems = warehouseItemIds.Count == 0
            ? new List<Item>()
            : await _db.Items.AsNoTracking()
                .Include(i => i.Category)
                .Include(i => i.BaseUom)
                .Where(i => i.IsActive && warehouseItemIds.Contains(i.ItemId))
                .OrderBy(i => i.ItemCode)
                .ToListAsync();

        _inventoryBalanceService.ApplyStockBalances(warehouseItems, stockMap);

        var vm = new DashboardViewModel
        {
            TotalItems = warehouseItems.Count,
            TotalWarehouses = 1,
            TotalPartners = await _db.Vouchers.AsNoTracking()
                .Where(v => v.WarehouseId == warehouseId && v.PartnerId.HasValue && !v.IsCancelled)
                .Select(v => v.PartnerId!.Value)
                .Distinct()
                .CountAsync(),
            TodayVouchers = await _db.Vouchers.CountAsync(v => v.WarehouseId == warehouseId && v.VoucherDate == today && !v.IsCancelled),
            TotalStockValue = canSeeFinancial ? warehouseItems.Sum(i => i.TotalStockValue) : 0m,
            LowStockCount = warehouseItems.Count(i => i.MinThreshold > 0 && i.CurrentStock > 0 && i.CurrentStock <= i.MinThreshold),
            OutOfStockCount = warehouseItems.Count(i => i.CurrentStock <= 0),
            OverStockCount = warehouseItems.Count(i => i.MaxThreshold.HasValue && i.CurrentStock >= i.MaxThreshold.Value),
            LowStockItems = warehouseItems
                .Where(i => i.MinThreshold > 0 && i.CurrentStock <= i.MinThreshold)
                .OrderBy(i => i.CurrentStock)
                .Take(10)
                .ToList(),
            RecentVouchers = await _db.Vouchers
                .AsNoTracking()
                .Include(v => v.Warehouse)
                .Include(v => v.Partner)
                .Where(v => v.WarehouseId == warehouseId)
                .OrderByDescending(v => v.CreatedAt)
                .Take(10)
                .ToListAsync(),
            UnresolvedAlerts = warehouseItemIds.Count == 0
                ? new List<StockAlert>()
                : await _db.StockAlerts
                    .AsNoTracking()
                    .Include(a => a.Item)
                    .Where(a => !a.IsResolved && warehouseItemIds.Contains(a.ItemId))
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(10)
                    .ToListAsync()
        };

        vm.OpenWaves = await _db.Waves.CountAsync(w =>
            w.WarehouseId == warehouseId &&
            (w.Status == WaveStatusEnum.Released || w.Status == WaveStatusEnum.InProgress));
        vm.OpenPickTasks = await _db.PickTasks.CountAsync(t =>
            ((t.Wave != null && t.Wave.WarehouseId == warehouseId)
                || (t.Wave == null && t.Voucher != null && t.Voucher.WarehouseId == warehouseId)) &&
            (t.Status == PickTaskStatusEnum.Pending || t.Status == PickTaskStatusEnum.Assigned || t.Status == PickTaskStatusEnum.InProgress));
        vm.ShortPickTasks = await _db.PickTasks.CountAsync(t =>
            ((t.Wave != null && t.Wave.WarehouseId == warehouseId)
                || (t.Wave == null && t.Voucher != null && t.Voucher.WarehouseId == warehouseId)) &&
            t.Status == PickTaskStatusEnum.Short);

        var activeReservations = await _db.StockReservations
            .AsNoTracking()
            .Where(r => r.Status == ReservationStatusEnum.Active
                && r.Voucher != null
                && r.Voucher.WarehouseId == warehouseId)
            .Select(r => new { r.ReservedQty, r.ConsumedQty })
            .ToListAsync();
        var totalReserved = activeReservations.Sum(x => x.ReservedQty);
        var totalConsumed = activeReservations.Sum(x => x.ConsumedQty);
        vm.ReservationFillRate = totalReserved > 0 ? (totalConsumed / totalReserved) * 100m : 0m;

        vm.PendingOutboundVouchers = await _db.Vouchers.CountAsync(v =>
            v.WarehouseId == warehouseId
            && !v.IsCancelled && !v.IsPosted
            && (v.VoucherType == VoucherTypeEnum.XuatKho || v.VoucherType == VoucherTypeEnum.TraNCC
                || v.VoucherType == VoucherTypeEnum.ChuyenKho || v.VoucherType == VoucherTypeEnum.XuatSanXuat)
            && v.FulfillmentStatus < FulfillmentStatusEnum.Completed);

        vm.PendingInboundApprovals = await _db.Vouchers.CountAsync(v =>
            v.WarehouseId == warehouseId
            && !v.IsCancelled
            && (v.VoucherType == VoucherTypeEnum.NhapKho || v.VoucherType == VoucherTypeEnum.KhachTra || v.VoucherType == VoucherTypeEnum.NhapThanhPham)
            && v.InboundStatus == InboundStatusEnum.PendingApproval);

        vm.StalePickTasks = await _db.PickTasks.CountAsync(t =>
            ((t.Wave != null && t.Wave.WarehouseId == warehouseId)
                || (t.Wave == null && t.Voucher != null && t.Voucher.WarehouseId == warehouseId))
            && (t.Status == PickTaskStatusEnum.Pending || t.Status == PickTaskStatusEnum.Assigned)
            && t.DueAt.HasValue && t.DueAt.Value < VietnamNow);

        vm.UnassignedPickTasks = await _db.PickTasks.CountAsync(t =>
            ((t.Wave != null && t.Wave.WarehouseId == warehouseId)
                || (t.Wave == null && t.Voucher != null && t.Voucher.WarehouseId == warehouseId))
            && t.Status == PickTaskStatusEnum.Pending
            && string.IsNullOrEmpty(t.AssignedTo));

        vm.OverdueVouchers = await _db.Vouchers.CountAsync(v =>
            v.WarehouseId == warehouseId
            && !v.IsCancelled && !v.IsPosted
            && v.RequestedDeliveryDate.HasValue
            && v.RequestedDeliveryDate.Value < today);

        var thirtyDaysAgo = today.AddDays(-30);
        var vouchersByType = await _db.Vouchers
            .AsNoTracking()
            .Where(v => v.WarehouseId == warehouseId && v.VoucherDate >= thirtyDaysAgo && !v.IsCancelled)
            .GroupBy(v => v.VoucherType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync();

        var typeNames = new Dictionary<VoucherTypeEnum, string>
        {
            { VoucherTypeEnum.NhapKho, "Nhập kho" },
            { VoucherTypeEnum.XuatKho, "Xuất kho" },
            { VoucherTypeEnum.TraNCC, "Trả NCC" },
            { VoucherTypeEnum.KhachTra, "Khách trả" },
            { VoucherTypeEnum.DieuChinh, "Điều chỉnh" },
            { VoucherTypeEnum.ChuyenKho, "Chuyển kho" },
            { VoucherTypeEnum.NhapThanhPham, "Nhập TP" },
            { VoucherTypeEnum.XuatSanXuat, "Xuất SX" }
        };
        vm.VouchersByType = vouchersByType.ToDictionary(
            v => typeNames.GetValueOrDefault(v.Type, "Khác"),
            v => v.Count);

        if (canSeeFinancial)
        {
            vm.StockByCategory = warehouseItems
                .GroupBy(i => i.Category?.CategoryName ?? "Chưa phân loại")
                .ToDictionary(g => g.Key, g => g.Sum(i => i.TotalStockValue));
        }
        else
        {
            vm.StockByCategory = warehouseItems
                .GroupBy(i => i.Category?.CategoryName ?? "Chưa phân loại")
                .ToDictionary(g => g.Key, _ => 0m);
        }

        return vm;
    }

    private async Task<DashboardViewModel> BuildEnterpriseDashboardAsync(DateTime today, bool canSeeFinancial)
    {
        var stockMap = await _inventoryBalanceService.GetStockByItemAsync();
        var activeItems = await _db.Items.AsNoTracking()
            .Include(i => i.Category)
            .Include(i => i.BaseUom)
            .Where(i => i.IsActive)
            .OrderBy(i => i.ItemCode)
            .ToListAsync();
        _inventoryBalanceService.ApplyStockBalances(activeItems, stockMap);

        var vm = new DashboardViewModel
        {
            TotalItems = activeItems.Count,
            TotalWarehouses = await _db.Warehouses.CountAsync(w => w.IsActive),
            TotalPartners = await _db.Partners.CountAsync(p => p.IsActive),
            TodayVouchers = await _db.Vouchers.CountAsync(v => v.VoucherDate == today && !v.IsCancelled),
            TotalStockValue = canSeeFinancial ? activeItems.Sum(i => i.TotalStockValue) : 0m,
            LowStockCount = activeItems.Count(i => i.MinThreshold > 0 && i.CurrentStock <= i.MinThreshold && i.CurrentStock > 0),
            OutOfStockCount = activeItems.Count(i => i.CurrentStock <= 0),
            OverStockCount = activeItems.Count(i => i.MaxThreshold.HasValue && i.CurrentStock >= i.MaxThreshold.Value),
            LowStockItems = activeItems
                .Where(i => i.MinThreshold > 0 && i.CurrentStock <= i.MinThreshold)
                .OrderBy(i => i.CurrentStock)
                .Take(10).ToList(),
            RecentVouchers = await _db.Vouchers
                .Include(v => v.Warehouse).Include(v => v.Partner)
                .OrderByDescending(v => v.CreatedAt)
                .Take(10).ToListAsync(),
            UnresolvedAlerts = await _db.StockAlerts
                .Include(a => a.Item)
                .Where(a => !a.IsResolved)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10).ToListAsync()
        };

        vm.OpenWaves = await _db.Waves.CountAsync(w => w.Status == WaveStatusEnum.Released || w.Status == WaveStatusEnum.InProgress);
        vm.OpenPickTasks = await _db.PickTasks.CountAsync(t => t.Status == PickTaskStatusEnum.Pending || t.Status == PickTaskStatusEnum.Assigned || t.Status == PickTaskStatusEnum.InProgress);
        vm.ShortPickTasks = await _db.PickTasks.CountAsync(t => t.Status == PickTaskStatusEnum.Short);
        var activeReservations = await _db.StockReservations
            .Where(r => r.Status == ReservationStatusEnum.Active)
            .Select(r => new { r.ReservedQty, r.ConsumedQty })
            .ToListAsync();
        var totalReserved = activeReservations.Sum(x => x.ReservedQty);
        var totalConsumed = activeReservations.Sum(x => x.ConsumedQty);
        vm.ReservationFillRate = totalReserved > 0 ? (totalConsumed / totalReserved) * 100m : 0m;

        vm.PendingOutboundVouchers = await _db.Vouchers.CountAsync(v =>
            !v.IsCancelled && !v.IsPosted
            && (v.VoucherType == VoucherTypeEnum.XuatKho || v.VoucherType == VoucherTypeEnum.TraNCC
                || v.VoucherType == VoucherTypeEnum.ChuyenKho || v.VoucherType == VoucherTypeEnum.XuatSanXuat)
            && v.FulfillmentStatus < FulfillmentStatusEnum.Completed);

        vm.PendingInboundApprovals = await _db.Vouchers.CountAsync(v =>
            !v.IsCancelled
            && (v.VoucherType == VoucherTypeEnum.NhapKho || v.VoucherType == VoucherTypeEnum.KhachTra || v.VoucherType == VoucherTypeEnum.NhapThanhPham)
            && v.InboundStatus == InboundStatusEnum.PendingApproval);

        vm.StalePickTasks = await _db.PickTasks.CountAsync(t =>
            (t.Status == PickTaskStatusEnum.Pending || t.Status == PickTaskStatusEnum.Assigned)
            && t.DueAt.HasValue && t.DueAt.Value < VietnamNow);

        vm.UnassignedPickTasks = await _db.PickTasks.CountAsync(t =>
            (t.Status == PickTaskStatusEnum.Pending)
            && string.IsNullOrEmpty(t.AssignedTo));

        vm.OverdueVouchers = await _db.Vouchers.CountAsync(v =>
            !v.IsCancelled && !v.IsPosted
            && v.RequestedDeliveryDate.HasValue
            && v.RequestedDeliveryDate.Value < today);

        var thirtyDaysAgo = today.AddDays(-30);
        var vouchersByType = await _db.Vouchers
            .Where(v => v.VoucherDate >= thirtyDaysAgo && !v.IsCancelled)
            .GroupBy(v => v.VoucherType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync();

        var typeNames = new Dictionary<VoucherTypeEnum, string>
        {
            { VoucherTypeEnum.NhapKho, "Nhập kho" },
            { VoucherTypeEnum.XuatKho, "Xuất kho" },
            { VoucherTypeEnum.TraNCC, "Trả NCC" },
            { VoucherTypeEnum.KhachTra, "Khách trả" },
            { VoucherTypeEnum.DieuChinh, "Điều chỉnh" },
            { VoucherTypeEnum.ChuyenKho, "Chuyển kho" },
            { VoucherTypeEnum.NhapThanhPham, "Nhập TP" },
            { VoucherTypeEnum.XuatSanXuat, "Xuất SX" }
        };
        vm.VouchersByType = vouchersByType.ToDictionary(
            v => typeNames.GetValueOrDefault(v.Type, "Khác"),
            v => v.Count);

        if (canSeeFinancial)
        {
            // P0-03: Use already-balanced in-memory items (ApplyStockBalances was called above)
            vm.StockByCategory = activeItems
                .GroupBy(i => i.Category?.CategoryName ?? "Chưa phân loại")
                .ToDictionary(g => g.Key, g => g.Sum(i => i.TotalStockValue));
        }
        else
        {
            var categories = await _db.Items
                .Where(i => i.IsActive)
                .Include(i => i.Category)
                .GroupBy(i => i.Category != null ? i.Category.CategoryName : "Chưa phân loại")
                .Select(g => g.Key)
                .ToListAsync();
            vm.StockByCategory = categories.ToDictionary(c => c, _ => 0m);
        }

        return vm;
    }
}
