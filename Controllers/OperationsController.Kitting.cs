using System;

using System.Collections.Generic;

using System.Data;

using System.IO;

using System.Linq;

using System.Text.Json;

using System.Threading.Tasks;

using System.Linq.Expressions;

using ClosedXML.Excel;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using WMS.Common;

using WMS.Data;

using WMS.Models;

using WMS.Services;

using WMS.ViewModels;

namespace WMS.Controllers;

public partial class OperationsController
{

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> KittingWorkOrders(int? warehouseId, KittingWorkOrderStatusEnum? status, string? search)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        IQueryable<KittingWorkOrder> query = _db.KittingWorkOrders.AsNoTracking().Include((KittingWorkOrder k) => k.Warehouse).Include((KittingWorkOrder k) => k.FinishedItem)
            .ThenInclude((Item i) => i.BaseUom)
            .Include((KittingWorkOrder k) => k.FinishedLocation)
            .AsQueryable();
        if (warehouseId.HasValue)
        {
            query = query.Where((KittingWorkOrder k) => k.WarehouseId == ((int?)warehouseId).Value);
        }
        if (status.HasValue)
        {
            query = query.Where((KittingWorkOrder k) => k.Status == ((KittingWorkOrderStatusEnum?)status).Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();
            query = query.Where((KittingWorkOrder k) => k.WorkOrderCode.Contains(keyword) || k.FinishedItem.ItemCode.Contains(keyword) || k.FinishedItem.ItemName.Contains(keyword) || (k.FinishedLotNumber != null && k.FinishedLotNumber.Contains(keyword)));
        }
        IQueryable<KittingWorkOrder> baseQuery = _db.KittingWorkOrders.AsNoTracking();
        if (warehouseId.HasValue)
        {
            baseQuery = baseQuery.Where((KittingWorkOrder k) => k.WarehouseId == ((int?)warehouseId).Value);
        }
        KittingWorkOrderPageViewModel kittingWorkOrderPageViewModel = new KittingWorkOrderPageViewModel
        {
            WarehouseId = warehouseId,
            Status = status,
            Search = search
        };
        KittingWorkOrderPageViewModel kittingWorkOrderPageViewModel2 = kittingWorkOrderPageViewModel;
        kittingWorkOrderPageViewModel2.Warehouses = await GetVisibleWarehousesAsync();
        KittingWorkOrderPageViewModel kittingWorkOrderPageViewModel3 = kittingWorkOrderPageViewModel;
        kittingWorkOrderPageViewModel3.WorkOrders = await query.OrderByDescending((KittingWorkOrder k) => k.CreatedAt).Take(200).ToListAsync();
        KittingWorkOrderPageViewModel kittingWorkOrderPageViewModel4 = kittingWorkOrderPageViewModel;
        kittingWorkOrderPageViewModel4.DraftCount = await baseQuery.CountAsync((KittingWorkOrder k) => k.Status == KittingWorkOrderStatusEnum.Draft);
        KittingWorkOrderPageViewModel kittingWorkOrderPageViewModel5 = kittingWorkOrderPageViewModel;
        kittingWorkOrderPageViewModel5.ReservedCount = await baseQuery.CountAsync((KittingWorkOrder k) => k.Status == KittingWorkOrderStatusEnum.Reserved);
        KittingWorkOrderPageViewModel kittingWorkOrderPageViewModel6 = kittingWorkOrderPageViewModel;
        kittingWorkOrderPageViewModel6.CompletedCount = await baseQuery.CountAsync((KittingWorkOrder k) => k.Status == KittingWorkOrderStatusEnum.Completed);
        KittingWorkOrderPageViewModel kittingWorkOrderPageViewModel7 = kittingWorkOrderPageViewModel;
        kittingWorkOrderPageViewModel7.CancelledCount = await baseQuery.CountAsync((KittingWorkOrder k) => k.Status == KittingWorkOrderStatusEnum.Cancelled);
        KittingWorkOrderPageViewModel model = kittingWorkOrderPageViewModel;
        return View(model);
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> CreateKittingWorkOrder(int? warehouseId = null)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        List<int> parentItemIds = await (from b in _db.BillOfMaterials.AsNoTracking()
                                         where b.IsActive
                                         select b.ParentItemId).Distinct().ToListAsync();
        KittingWorkOrderCreatePageViewModel kittingWorkOrderCreatePageViewModel = new KittingWorkOrderCreatePageViewModel
        {
            Request = new CreateKittingWorkOrderCommand
            {
                WarehouseId = warehouseId.GetValueOrDefault(),
                PlannedQty = 1m
            }
        };
        KittingWorkOrderCreatePageViewModel kittingWorkOrderCreatePageViewModel2 = kittingWorkOrderCreatePageViewModel;
        kittingWorkOrderCreatePageViewModel2.Warehouses = await GetVisibleWarehousesAsync();
        KittingWorkOrderCreatePageViewModel kittingWorkOrderCreatePageViewModel3 = kittingWorkOrderCreatePageViewModel;
        kittingWorkOrderCreatePageViewModel3.FinishedItems = await (from i in _db.Items.Include((Item i) => i.BaseUom)
                                                                    where i.IsActive && parentItemIds.Contains(i.ItemId)
                                                                    orderby i.ItemCode
                                                                    select i).ToListAsync();
        KittingWorkOrderCreatePageViewModel kittingWorkOrderCreatePageViewModel4 = kittingWorkOrderCreatePageViewModel;
        kittingWorkOrderCreatePageViewModel4.Locations = await GetVisibleLocationsAsync(warehouseId);
        KittingWorkOrderCreatePageViewModel model = kittingWorkOrderCreatePageViewModel;
        return View(model);
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateKittingWorkOrder(CreateKittingWorkOrderCommand request)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            request.WarehouseId = scopedWh.Value;
        }
        try
        {
            KittingWorkOrder workOrder = await _kittingService.CreateFromBomAsync(request, base.User.Identity?.Name ?? "system");
            base.TempData["Success"] = "Đã tạo phiếu lắp bộ hàng [" + workOrder.WorkOrderCode + "].";
            return RedirectToAction("KittingWorkOrderDetails", new
            {
                id = workOrder.KittingWorkOrderId
            });
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
            return RedirectToAction("CreateKittingWorkOrder", new
            {
                warehouseId = request.WarehouseId
            });
        }
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> KittingWorkOrderDetails(long id)
    {
        int? scopedWh = GetScopedWarehouseId();
        KittingWorkOrder? workOrder = await _db.KittingWorkOrders.AsNoTracking().Include((KittingWorkOrder k) => k.Warehouse).Include((KittingWorkOrder k) => k.FinishedItem)
            .ThenInclude((Item i) => i.BaseUom)
            .Include((KittingWorkOrder k) => k.FinishedLocation)
            .Include((KittingWorkOrder k) => k.Lines)
            .ThenInclude((KittingWorkOrderLine l) => l.ComponentItem)
            .ThenInclude((Item i) => i.BaseUom)
            .Include((KittingWorkOrder k) => k.Lines)
            .ThenInclude((KittingWorkOrderLine l) => l.SourceLocation)
            .FirstOrDefaultAsync((KittingWorkOrder k) => k.KittingWorkOrderId == id);
        if (workOrder == null)
        {
            return NotFound();
        }
        if (scopedWh.HasValue && workOrder.WarehouseId != scopedWh.Value)
        {
            return Forbid();
        }
        return View(workOrder);
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReserveKittingWorkOrder(long id)
    {
        try
        {
            int? scopedWh = GetScopedWarehouseId();
            int wh = await (from k in _db.KittingWorkOrders.AsNoTracking()
                            where k.KittingWorkOrderId == id
                            select k.WarehouseId).FirstOrDefaultAsync();
            if (scopedWh.HasValue && wh != scopedWh.Value)
            {
                return Forbid();
            }
            KittingWorkOrder workOrder = await _kittingService.ReserveAsync(id, base.User.Identity?.Name ?? "system");
            base.TempData["Success"] = "Đã giữ chỗ vật tư thành phần cho [" + workOrder.WorkOrderCode + "].";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("KittingWorkOrderDetails", new { id });
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteKittingWorkOrder(long id)
    {
        try
        {
            int? scopedWh = GetScopedWarehouseId();
            int wh = await (from k in _db.KittingWorkOrders.AsNoTracking()
                            where k.KittingWorkOrderId == id
                            select k.WarehouseId).FirstOrDefaultAsync();
            if (scopedWh.HasValue && wh != scopedWh.Value)
            {
                return Forbid();
            }
            KittingWorkOrder workOrder = await _kittingService.CompleteAsync(id, base.User.Identity?.Name ?? "system");
            base.TempData["Success"] = "Đã hoàn tất phiếu lắp bộ hàng [" + workOrder.WorkOrderCode + "] và nhập mã hàng bộ.";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("KittingWorkOrderDetails", new { id });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelKittingWorkOrder(long id, string reason)
    {
        try
        {
            int? scopedWh = GetScopedWarehouseId();
            int wh = await (from k in _db.KittingWorkOrders.AsNoTracking()
                            where k.KittingWorkOrderId == id
                            select k.WarehouseId).FirstOrDefaultAsync();
            if (scopedWh.HasValue && wh != scopedWh.Value)
            {
                return Forbid();
            }
            KittingWorkOrder workOrder = await _kittingService.CancelAsync(id, reason, base.User.Identity?.Name ?? "system");
            base.TempData["Success"] = "Đã hủy phiếu lắp bộ hàng [" + workOrder.WorkOrderCode + "].";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("KittingWorkOrderDetails", new { id });
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> PrintKittingWorkOrderLabels(long id, string labelSize = "50x30")
    {
        int? scopedWh = GetScopedWarehouseId();
        KittingWorkOrder? workOrder = await _db.KittingWorkOrders.AsNoTracking().Include((KittingWorkOrder k) => k.FinishedItem).ThenInclude((Item i) => i.BaseUom)
            .Include((KittingWorkOrder k) => k.FinishedLocation)
            .FirstOrDefaultAsync((KittingWorkOrder k) => k.KittingWorkOrderId == id);
        if (workOrder == null)
        {
            return NotFound();
        }
        if (scopedWh.HasValue && workOrder.WarehouseId != scopedWh.Value)
        {
            return Forbid();
        }
        if (workOrder.Status != KittingWorkOrderStatusEnum.Completed)
        {
            base.TempData["Error"] = "Chỉ in tem bộ hàng sau khi lệnh đã hoàn tất.";
            return RedirectToAction("KittingWorkOrderDetails", new { id });
        }
        Item item = workOrder.FinishedItem;
        PrintLabelBatchViewModel model = new PrintLabelBatchViewModel
        {
            LabelSize = (string.IsNullOrWhiteSpace(labelSize) ? "50x30" : labelSize),
            Items = new List<PrintLabelItem>
            {
                new PrintLabelItem
                {
                    ItemId = item.ItemId,
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    Barcode = (string.IsNullOrWhiteSpace(item.Barcode) ? item.ItemCode : item.Barcode),
                    SkuCode = item.SkuCode,
                    Unit = (item.BaseUom?.UomName ?? item.BaseUom?.UomCode ?? ""),
                    LocationCode = (workOrder.FinishedLocation?.LocationCode ?? "Chưa cập nhật"),
                    PrintQuantity = (int)Math.Clamp(workOrder.CompletedQty, 1m, 500m)
                }
            }
        };
        return View("~/Views/Items/PrintLabels.cshtml", model);
    }

}
