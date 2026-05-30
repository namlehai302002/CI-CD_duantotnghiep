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
    public async Task<IActionResult> VasWorkOrders(int? warehouseId, VasWorkOrderStatusEnum? status, VasOperationTypeEnum? operationType, string? search)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        IQueryable<VasWorkOrder> query = _db.VasWorkOrders.AsNoTracking().Include((VasWorkOrder v) => v.Warehouse).Include((VasWorkOrder v) => v.Partner)
            .Include((VasWorkOrder v) => v.Voucher)
            .Include((VasWorkOrder v) => v.PrimaryItem)
            .ThenInclude((Item i) => i.BaseUom)
            .AsQueryable();
        if (warehouseId.HasValue)
        {
            query = query.Where((VasWorkOrder v) => v.WarehouseId == ((int?)warehouseId).Value);
        }
        if (status.HasValue)
        {
            query = query.Where((VasWorkOrder v) => v.Status == ((VasWorkOrderStatusEnum?)status).Value);
        }
        if (operationType.HasValue)
        {
            query = query.Where((VasWorkOrder v) => v.OperationType == ((VasOperationTypeEnum?)operationType).Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();
            query = query.Where((VasWorkOrder v) => v.WorkOrderCode.Contains(keyword) || v.PrimaryItem.ItemCode.Contains(keyword) || v.PrimaryItem.ItemName.Contains(keyword) || (v.Partner != null && (v.Partner.PartnerCode.Contains(keyword) || v.Partner.PartnerName.Contains(keyword))) || (v.Voucher != null && v.Voucher.VoucherCode.Contains(keyword)));
        }
        IQueryable<VasWorkOrder> baseQuery = _db.VasWorkOrders.AsNoTracking();
        if (warehouseId.HasValue)
        {
            baseQuery = baseQuery.Where((VasWorkOrder v) => v.WarehouseId == ((int?)warehouseId).Value);
        }
        VasWorkOrderPageViewModel vasWorkOrderPageViewModel = new VasWorkOrderPageViewModel
        {
            WarehouseId = warehouseId,
            Status = status,
            OperationType = operationType,
            Search = search
        };
        VasWorkOrderPageViewModel vasWorkOrderPageViewModel2 = vasWorkOrderPageViewModel;
        vasWorkOrderPageViewModel2.Warehouses = await GetVisibleWarehousesAsync();
        VasWorkOrderPageViewModel vasWorkOrderPageViewModel3 = vasWorkOrderPageViewModel;
        vasWorkOrderPageViewModel3.WorkOrders = await query.OrderByDescending((VasWorkOrder v) => v.CreatedAt).Take(200).ToListAsync();
        VasWorkOrderPageViewModel vasWorkOrderPageViewModel4 = vasWorkOrderPageViewModel;
        vasWorkOrderPageViewModel4.DraftCount = await baseQuery.CountAsync((VasWorkOrder v) => v.Status == VasWorkOrderStatusEnum.Draft);
        VasWorkOrderPageViewModel vasWorkOrderPageViewModel5 = vasWorkOrderPageViewModel;
        vasWorkOrderPageViewModel5.ReservedCount = await baseQuery.CountAsync((VasWorkOrder v) => v.Status == VasWorkOrderStatusEnum.Reserved);
        VasWorkOrderPageViewModel vasWorkOrderPageViewModel6 = vasWorkOrderPageViewModel;
        vasWorkOrderPageViewModel6.InProgressCount = await baseQuery.CountAsync((VasWorkOrder v) => v.Status == VasWorkOrderStatusEnum.InProgress);
        VasWorkOrderPageViewModel vasWorkOrderPageViewModel7 = vasWorkOrderPageViewModel;
        vasWorkOrderPageViewModel7.QcPendingCount = await baseQuery.CountAsync((VasWorkOrder v) => v.Status == VasWorkOrderStatusEnum.QcPending);
        VasWorkOrderPageViewModel vasWorkOrderPageViewModel8 = vasWorkOrderPageViewModel;
        vasWorkOrderPageViewModel8.CompletedCount = await baseQuery.CountAsync((VasWorkOrder v) => v.Status == VasWorkOrderStatusEnum.Completed);
        VasWorkOrderPageViewModel vasWorkOrderPageViewModel9 = vasWorkOrderPageViewModel;
        vasWorkOrderPageViewModel9.CancelledCount = await baseQuery.CountAsync((VasWorkOrder v) => v.Status == VasWorkOrderStatusEnum.Cancelled);
        VasWorkOrderPageViewModel model = vasWorkOrderPageViewModel;
        return View(model);
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> CreateVasWorkOrder(int? warehouseId = null)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        IQueryable<Voucher> vouchersQuery = from v in _db.Vouchers.AsNoTracking().Include((Voucher v) => v.Partner)
                                            where !v.IsCancelled && (!((int?)warehouseId).HasValue || v.WarehouseId == ((int?)warehouseId).Value)
                                            select v;
        VasWorkOrderCreatePageViewModel vasWorkOrderCreatePageViewModel = new VasWorkOrderCreatePageViewModel
        {
            Request = new CreateVasWorkOrderCommand
            {
                WarehouseId = warehouseId.GetValueOrDefault(),
                PlannedQty = 1m,
                LaborRatePerHour = 0m,
                MaterialLines = new List<CreateVasMaterialLineCommand>
                {
                    new CreateVasMaterialLineCommand(),
                    new CreateVasMaterialLineCommand(),
                    new CreateVasMaterialLineCommand()
                }
            }
        };
        VasWorkOrderCreatePageViewModel vasWorkOrderCreatePageViewModel2 = vasWorkOrderCreatePageViewModel;
        vasWorkOrderCreatePageViewModel2.Warehouses = await GetVisibleWarehousesAsync();
        VasWorkOrderCreatePageViewModel vasWorkOrderCreatePageViewModel3 = vasWorkOrderCreatePageViewModel;
        vasWorkOrderCreatePageViewModel3.Partners = await (from p in _db.Partners.AsNoTracking()
                                                           where p.IsActive
                                                           orderby p.PartnerCode
                                                           select p).ToListAsync();
        VasWorkOrderCreatePageViewModel vasWorkOrderCreatePageViewModel4 = vasWorkOrderCreatePageViewModel;
        vasWorkOrderCreatePageViewModel4.Vouchers = await (from v in vouchersQuery
                                                           orderby v.VoucherDate descending, v.VoucherId descending
                                                           select v).Take(150).ToListAsync();
        VasWorkOrderCreatePageViewModel vasWorkOrderCreatePageViewModel5 = vasWorkOrderCreatePageViewModel;
        vasWorkOrderCreatePageViewModel5.Items = await (from i in _db.Items.AsNoTracking().Include((Item i) => i.BaseUom)
                                                        where i.IsActive
                                                        orderby i.ItemCode
                                                        select i).Take(500).ToListAsync();
        VasWorkOrderCreatePageViewModel vasWorkOrderCreatePageViewModel6 = vasWorkOrderCreatePageViewModel;
        vasWorkOrderCreatePageViewModel6.MaterialItems = await (from i in _db.Items.AsNoTracking().Include((Item i) => i.BaseUom)
                                                                where i.IsActive
                                                                orderby i.ItemCode
                                                                select i).Take(500).ToListAsync();
        VasWorkOrderCreatePageViewModel model = vasWorkOrderCreatePageViewModel;
        return View(model);
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVasWorkOrder(CreateVasWorkOrderCommand request)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            request.WarehouseId = scopedWh.Value;
        }
        try
        {
            VasWorkOrder workOrder = await _vasService.CreateAsync(request, base.User.Identity?.Name ?? "system");
            base.TempData["Success"] = "Đã tạo lệnh gia công phụ trợ [" + workOrder.WorkOrderCode + "].";
            return RedirectToAction("VasWorkOrderDetails", new
            {
                id = workOrder.VasWorkOrderId
            });
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
            return RedirectToAction("CreateVasWorkOrder", new
            {
                warehouseId = request.WarehouseId
            });
        }
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> VasWorkOrderDetails(long id)
    {
        int? scopedWh = GetScopedWarehouseId();
        VasWorkOrder? workOrder = await _db.VasWorkOrders.AsNoTracking().Include((VasWorkOrder v) => v.Warehouse).Include((VasWorkOrder v) => v.Partner)
            .Include((VasWorkOrder v) => v.Voucher)
            .Include((VasWorkOrder v) => v.PrimaryItem)
            .ThenInclude((Item i) => i.BaseUom)
            .Include((VasWorkOrder v) => v.Operations)
            .Include((VasWorkOrder v) => v.MaterialLines)
            .ThenInclude((VasMaterialLine l) => l.MaterialItem)
            .ThenInclude((Item i) => i.BaseUom)
            .Include((VasWorkOrder v) => v.MaterialLines)
            .ThenInclude((VasMaterialLine l) => l.SourceLocation)
            .FirstOrDefaultAsync((VasWorkOrder v) => v.VasWorkOrderId == id);
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
    public async Task<IActionResult> ReserveVasWorkOrder(long id)
    {
        try
        {
            int? scopedWh = GetScopedWarehouseId();
            int wh = await (from v in _db.VasWorkOrders.AsNoTracking()
                            where v.VasWorkOrderId == id
                            select v.WarehouseId).FirstOrDefaultAsync();
            if (scopedWh.HasValue && wh != scopedWh.Value)
            {
                return Forbid();
            }
            VasWorkOrder workOrder = await _vasService.ReserveAsync(id, base.User.Identity?.Name ?? "system");
            base.TempData["Success"] = "Đã giữ chỗ vật tư phụ cho [" + workOrder.WorkOrderCode + "].";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("VasWorkOrderDetails", new { id });
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartVasWorkOrder(long id)
    {
        try
        {
            int? scopedWh = GetScopedWarehouseId();
            int wh = await (from v in _db.VasWorkOrders.AsNoTracking()
                            where v.VasWorkOrderId == id
                            select v.WarehouseId).FirstOrDefaultAsync();
            if (scopedWh.HasValue && wh != scopedWh.Value)
            {
                return Forbid();
            }
            VasWorkOrder workOrder = await _vasService.StartAsync(id, base.User.Identity?.Name ?? "system");
            base.TempData["Success"] = "Đã bắt đầu lệnh gia công phụ trợ [" + workOrder.WorkOrderCode + "].";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("VasWorkOrderDetails", new { id });
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteVasOperation(long operationId, decimal actualMinutes, string? notes)
    {
        long id = 0L;
        try
        {
            VasOperation? op = await _db.VasOperations.AsNoTracking().Include((VasOperation o) => o.VasWorkOrder).FirstOrDefaultAsync((VasOperation o) => o.VasOperationId == operationId);
            if (op == null)
            {
                return NotFound();
            }
            id = op.VasWorkOrderId;
            int? scopedWh = GetScopedWarehouseId();
            if (scopedWh.HasValue && op.VasWorkOrder?.WarehouseId != scopedWh.Value)
            {
                return Forbid();
            }
            VasWorkOrder workOrder = await _vasService.CompleteOperationAsync(operationId, actualMinutes, notes, base.User.Identity?.Name ?? "system");
            base.TempData["Success"] = "Đã ghi nhận thời gian thao tác cho [" + workOrder.WorkOrderCode + "].";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("VasWorkOrderDetails", new { id });
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitVasQc(long id, decimal completedQty)
    {
        try
        {
            int? scopedWh = GetScopedWarehouseId();
            int wh = await (from v in _db.VasWorkOrders.AsNoTracking()
                            where v.VasWorkOrderId == id
                            select v.WarehouseId).FirstOrDefaultAsync();
            if (scopedWh.HasValue && wh != scopedWh.Value)
            {
                return Forbid();
            }
            VasWorkOrder workOrder = await _vasService.SubmitQcAsync(id, completedQty, base.User.Identity?.Name ?? "system");
            base.TempData["Success"] = "Đã gửi kiểm tra chất lượng cho lệnh gia công phụ trợ [" + workOrder.WorkOrderCode + "].";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("VasWorkOrderDetails", new { id });
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordVasQc(long id, VasQcResultEnum result, decimal passedQty, decimal failedQty, string? note)
    {
        try
        {
            int? scopedWh = GetScopedWarehouseId();
            int wh = await (from v in _db.VasWorkOrders.AsNoTracking()
                            where v.VasWorkOrderId == id
                            select v.WarehouseId).FirstOrDefaultAsync();
            if (scopedWh.HasValue && wh != scopedWh.Value)
            {
                return Forbid();
            }
            VasWorkOrder workOrder = await _vasService.RecordQcAsync(id, result, passedQty, failedQty, note, base.User.Identity?.Name ?? "system");
            base.TempData["Success"] = ((workOrder.Status == VasWorkOrderStatusEnum.InProgress) ? ("Kiểm tra chất lượng chưa đạt, lệnh [" + workOrder.WorkOrderCode + "] đã quay lại trạng thái đang thực hiện.") : ("Đã ghi nhận kết quả kiểm tra chất lượng cho [" + workOrder.WorkOrderCode + "]."));
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("VasWorkOrderDetails", new { id });
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteVasWorkOrder(long id)
    {
        try
        {
            int? scopedWh = GetScopedWarehouseId();
            int wh = await (from v in _db.VasWorkOrders.AsNoTracking()
                            where v.VasWorkOrderId == id
                            select v.WarehouseId).FirstOrDefaultAsync();
            if (scopedWh.HasValue && wh != scopedWh.Value)
            {
                return Forbid();
            }
            VasWorkOrder workOrder = await _vasService.CompleteAsync(id, base.User.Identity?.Name ?? "system");
            base.TempData["Success"] = "Đã hoàn tất lệnh gia công phụ trợ [" + workOrder.WorkOrderCode + "] v? ghi nhận chi ph?.";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("VasWorkOrderDetails", new { id });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelVasWorkOrder(long id, string reason)
    {
        try
        {
            int? scopedWh = GetScopedWarehouseId();
            int wh = await (from v in _db.VasWorkOrders.AsNoTracking()
                            where v.VasWorkOrderId == id
                            select v.WarehouseId).FirstOrDefaultAsync();
            if (scopedWh.HasValue && wh != scopedWh.Value)
            {
                return Forbid();
            }
            VasWorkOrder workOrder = await _vasService.CancelAsync(id, reason, base.User.Identity?.Name ?? "system");
            base.TempData["Success"] = "Đã hủy lệnh gia công phụ trợ [" + workOrder.WorkOrderCode + "].";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("VasWorkOrderDetails", new { id });
    }

}
