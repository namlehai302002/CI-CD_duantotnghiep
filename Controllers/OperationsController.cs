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

[Microsoft.AspNetCore.Authorization.Authorize]
public partial class OperationsController : Microsoft.AspNetCore.Mvc.Controller
{
    private readonly AppDbContext _db;


    private readonly IUnitOfWork _unitOfWork;


    private readonly ICrossDockService _crossDockService;


    private readonly IYardManagementService _yardService;


    private readonly IMovementTaskService _movementTaskService;


    private readonly IKittingWorkOrderService _kittingService;


    private readonly IVasWorkOrderService _vasService;


    private readonly TaskInterleavingService _taskInterleavingService;


    private readonly IYardBillingService _yardBillingService;


    private readonly IReplenishmentAutomationService _replenishmentAutomationService;


    private readonly IShipmentLoadService _shipmentLoadService;


    private readonly ITenantScopeService _tenantScopeService;


    private readonly IThreePlBillingService _threePlBillingService;

    private readonly IDockAppointmentService _dockAppointmentService;

    private readonly IThreePlEnterpriseBillingService _threePlEnterpriseBillingService;

    private readonly ILaborManagementService _laborManagementService;

    private readonly IOptimizationEnterpriseService _optimizationEnterpriseService;

    private readonly IAutomationEnterpriseService _automationEnterpriseService;

    private readonly IEnterpriseIntegrationService _enterpriseIntegrationService;


    private readonly IMheIntegrationService _mheIntegrationService;


    private readonly ICarrierIntegrationService _carrierIntegrationService;


    private readonly IShippingReconciliationService _shippingReconciliationService;

    private readonly IOperationsScopeQueryService _operationsScopeQueryService;

    private readonly ISlottingPlanningService _slottingPlanningService;

    private readonly IOperationExceptionQueryService _operationExceptionQueryService;

    private readonly IYardBillingQueryService _yardBillingQueryService;


    private static DateTime VietnamNow => VietnamTime.Now;


    public OperationsController(AppDbContext db, IUnitOfWork unitOfWork, ICrossDockService crossDockService, IYardManagementService yardService, IMovementTaskService movementTaskService, IKittingWorkOrderService kittingService, IVasWorkOrderService vasService, TaskInterleavingService taskInterleavingService, IYardBillingService yardBillingService, IReplenishmentAutomationService replenishmentAutomationService, IShipmentLoadService shipmentLoadService, ITenantScopeService tenantScopeService, IThreePlBillingService threePlBillingService, IMheIntegrationService mheIntegrationService, ICarrierIntegrationService carrierIntegrationService, IShippingReconciliationService shippingReconciliationService, IDockAppointmentService dockAppointmentService, IThreePlEnterpriseBillingService threePlEnterpriseBillingService, ILaborManagementService laborManagementService, IOptimizationEnterpriseService optimizationEnterpriseService, IAutomationEnterpriseService automationEnterpriseService, IEnterpriseIntegrationService enterpriseIntegrationService, IOperationsScopeQueryService operationsScopeQueryService, ISlottingPlanningService slottingPlanningService, IOperationExceptionQueryService operationExceptionQueryService, IYardBillingQueryService yardBillingQueryService)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _crossDockService = crossDockService;
        _yardService = yardService;
        _movementTaskService = movementTaskService;
        _kittingService = kittingService;
        _vasService = vasService;
        _taskInterleavingService = taskInterleavingService;
        _yardBillingService = yardBillingService;
        _replenishmentAutomationService = replenishmentAutomationService;
        _shipmentLoadService = shipmentLoadService;
        _tenantScopeService = tenantScopeService;
        _threePlBillingService = threePlBillingService;
        _dockAppointmentService = dockAppointmentService;
        _threePlEnterpriseBillingService = threePlEnterpriseBillingService;
        _laborManagementService = laborManagementService;
        _optimizationEnterpriseService = optimizationEnterpriseService;
        _automationEnterpriseService = automationEnterpriseService;
        _enterpriseIntegrationService = enterpriseIntegrationService;
        _mheIntegrationService = mheIntegrationService;
        _carrierIntegrationService = carrierIntegrationService;
        _shippingReconciliationService = shippingReconciliationService;
        _operationsScopeQueryService = operationsScopeQueryService;
        _slottingPlanningService = slottingPlanningService;
        _operationExceptionQueryService = operationExceptionQueryService;
        _yardBillingQueryService = yardBillingQueryService;
    }

    private async Task<(bool IsValid, string? NormalizedUserName, string ErrorMessage)> ValidateAssigneeAsync(string? rawUserName, int? warehouseId)
    {
        string? normalized = string.IsNullOrWhiteSpace(rawUserName) ? null : rawUserName.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return (IsValid: false, NormalizedUserName: null, ErrorMessage: "Vui lòng nhập tài khoản nhân viên cần gán.");
        }
        AppUser? user = await _db.AppUsers.AsNoTracking().Include((AppUser u) => u.Role).FirstOrDefaultAsync((AppUser u) => u.UserName == normalized && u.IsActive);
        if (user == null)
        {
            return (IsValid: false, NormalizedUserName: null, ErrorMessage: "Không tìm thấy tài khoản [" + normalized + "] hoặc tài khoản đã bị khóa.");
        }
        string role = user.Role?.RoleName ?? string.Empty;
        if (!string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase) && !string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase))
        {
            return (IsValid: false, NormalizedUserName: null, ErrorMessage: "Tài khoản [" + normalized + "] không thuộc nhóm Staff/Manager để nhận nhiệm vụ lấy hàng.");
        }
        if (warehouseId.HasValue && user.WarehouseId != warehouseId.Value)
        {
            return (IsValid: false, NormalizedUserName: null, ErrorMessage: "Tài khoản [" + normalized + "] không thuộc kho hiện tại, không thể gán nhiệm vụ.");
        }
        return (IsValid: true, NormalizedUserName: user.UserName, ErrorMessage: string.Empty);
    }


    private int? GetScopedWarehouseId()
        => _operationsScopeQueryService.GetScopedWarehouseId(base.User);


    private async Task<List<int>?> GetScopedZoneIdsAsync()
        => await _operationsScopeQueryService.GetScopedZoneIdsAsync(base.User);


    private async Task<List<Warehouse>> GetVisibleWarehousesAsync()
        => await _operationsScopeQueryService.GetVisibleWarehousesAsync(base.User);


    private async Task<List<Location>> GetVisibleLocationsAsync(int? warehouseId)
        => await _operationsScopeQueryService.GetVisibleLocationsAsync(base.User, warehouseId);


    private static bool RequiresManifest(VoucherTypeEnum voucherType)
    {
        if (voucherType == VoucherTypeEnum.ChuyenKho || voucherType == VoucherTypeEnum.XuatSanXuat)
        {
            return true;
        }
        return false;
    }


    private static bool RequiresTrackingOrManifest(VoucherTypeEnum voucherType)
    {
        if (voucherType - 2 <= VoucherTypeEnum.NhapKho)
        {
            return true;
        }
        return false;
    }


    private async Task LogReplenishmentAuditAsync(int itemId, int sourceLocationId, int destLocationId, decimal qty, string? lotNumber)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            TableName = "ItemLocations",
            RecordId = $"{itemId}:{sourceLocationId}->{destLocationId}",
            ActionType = "MOVE",
            ColumnChanged = "Quantity",
            OldValue = $"SRC:{sourceLocationId};DEST:{destLocationId}",
            NewValue = $"QTY:{qty:N4};LOT:{lotNumber ?? "-"}",
            ChangedBy = (base.User.Identity?.Name ?? "system"),
            ChangedAt = VietnamNow,
            IpAddress = base.HttpContext.Connection.RemoteIpAddress?.ToString(),
            AppModule = "Replenishment"
        });
        await _unitOfWork.SaveChangesAsync();
    }


    private (string Key, string Label, int Rank) MapSeverity(double ageHours)
        => _operationExceptionQueryService.MapSeverity(ageHours);


    private int GetSeverityRank(string severityKey)
        => _operationExceptionQueryService.GetSeverityRank(severityKey);


    private string ComputeExceptionKey(OperationExceptionRow row)
        => _operationExceptionQueryService.ComputeExceptionKey(row);


    private (string Key, string Label) MapCaseStatus(OperationExceptionStatusEnum status)
        => _operationExceptionQueryService.MapCaseStatus(status);


    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> LaborVariance(int? warehouseId, int days = 30)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        base.ViewBag.Warehouses = (await (from w in _db.Warehouses
                                          where w.IsActive
                                          orderby w.WarehouseCode
                                          select w).ToListAsync());
        base.ViewBag.WarehouseId = warehouseId;
        base.ViewBag.Days = days;
        DateTime cutoff = VietnamNow.AddDays(-days);
        var groups = (from t in await (from t in _db.PickTasks.Include((PickTask t) => t.Wave).Include((PickTask t) => t.Voucher)
                                       where t.Status == PickTaskStatusEnum.Completed && t.CompletedAt >= cutoff && !string.IsNullOrWhiteSpace(t.AssignedTo)
                                       where !((int?)warehouseId).HasValue || (t.Wave != null && t.Wave.WarehouseId == ((int?)warehouseId).Value) || (t.Wave == null && t.Voucher != null && t.Voucher.WarehouseId == ((int?)warehouseId).Value)
                                       select t).ToListAsync()
                      group t by t.AssignedTo into g
                      select new
                      {
                          UserName = g.Key,
                          TaskCount = g.Count(),
                          TotalQty = g.Sum((PickTask t) => t.PickedQty),
                          AvgMinutes = g.Average((PickTask t) => (t.CompletedAt.HasValue && t.StartedAt.HasValue) ? (t.CompletedAt.Value - t.StartedAt.Value).TotalMinutes : 0.0)
                      } into u
                      orderby u.TaskCount descending
                      select u).ToList();
        base.ViewBag.UserGroups = groups;
        return View();
    }


    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CheckPartialShipmentViability(long voucherId)
    {
        Voucher? voucher = await _db.Vouchers.Include((Voucher v) => v.Details).ThenInclude((VoucherDetail voucherDetail) => voucherDetail.Item).FirstOrDefaultAsync((Voucher v) => v.VoucherId == voucherId);
        if (voucher == null)
        {
            return NotFound();
        }
        List<object> results = new List<object>();
        foreach (VoucherDetail d in voucher.Details.Where((VoucherDetail voucherDetail) => voucherDetail.BaseQty != 0m))
        {
            decimal avail = await _db.ItemLocations.Where((ItemLocation il) => il.ItemId == d.ItemId && il.Quantity - il.ReservedQty > 0m && il.HoldStatus == InventoryHoldStatusEnum.Available).SumAsync((ItemLocation il) => il.Quantity - il.ReservedQty);
            decimal shortfall = Math.Max(0m, Math.Abs(d.BaseQty) - avail);
            results.Add(new
            {
                ItemCode = (d.Item?.ItemCode ?? d.ItemId.ToString()),
                Requested = Math.Abs(d.BaseQty),
                Available = avail,
                Shortfall = shortfall,
                CanFulfill = (avail >= Math.Abs(d.BaseQty)),
                Suggested = ((avail >= Math.Abs(d.BaseQty)) ? Math.Abs(d.BaseQty) : avail)
            });
        }
        return Json(results);
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterSerials(long voucherId, long voucherDetailId, string? serialCodes)
    {
        List<string> codes = ParseSerialCodes(serialCodes);
        if (codes.Count == 0)
        {
            base.TempData["Error"] = "Vui lòng quét hoặc nhập ít nhất một số sê-ri.";
            return RedirectToAction("SerialReceiving", new { voucherId });
        }
        int? scopedWh = GetScopedWarehouseId();
        Voucher? voucher = await _db.Vouchers.AsNoTracking().FirstOrDefaultAsync((Voucher v) => v.VoucherId == voucherId);
        if (voucher == null)
        {
            base.TempData["Error"] = "Phiếu không tồn tại.";
            return RedirectToAction("SerialReceiving", new { voucherId });
        }
        if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value)
        {
            base.TempData["Error"] = "Bạn không có quyền truy cập phiếu này.";
            return RedirectToAction("SerialReceiving", new { voucherId });
        }
        if (!voucher.IsInboundFlow || voucher.InboundStatus != InboundStatusEnum.Receiving)
        {
            base.TempData["Error"] = "Chỉ có thể đăng ký số sê-ri khi phiếu đang trong trạng thái nhận hàng.";
            return RedirectToAction("SerialReceiving", new { voucherId });
        }
        VoucherDetail? detail = await _db.VoucherDetails.Include((VoucherDetail d) => d.Item).FirstOrDefaultAsync((VoucherDetail d) => d.VoucherDetailId == voucherDetailId && d.VoucherId == voucherId);
        if (detail == null)
        {
            base.TempData["Error"] = "Dòng phiếu không tồn tại.";
            return RedirectToAction("SerialReceiving", new { voucherId });
        }
        if (detail.Item?.TrackSerial != true)
        {
            base.TempData["Error"] = "Dòng phiếu này chưa bật quản lý số sê-ri.";
            return RedirectToAction("SerialReceiving", new { voucherId });
        }
        decimal requiredBaseQty = detail.BaseQty - ((detail.DefectBaseQty > 0m) ? detail.DefectBaseQty : detail.DefectQty * (detail.ConversionRate == 0m ? 1m : Math.Abs(detail.ConversionRate)));
        requiredBaseQty = Math.Max(0m, requiredBaseQty);
        if (requiredBaseQty != decimal.Truncate(requiredBaseQty))
        {
            base.TempData["Error"] = $"[{detail.Item.ItemCode}] đang bật quản lý số sê-ri nên số lượng nhận phải là số nguyên.";
            return RedirectToAction("SerialReceiving", new { voucherId });
        }
        int existingSerialCount = await _db.SerialNumbers.CountAsync((SerialNumber s) => s.VoucherDetailId == (long?)voucherDetailId && s.Status == SerialNumberStatusEnum.Active && s.VoidedAt == null);
        List<string> existingCodes = await _db.SerialNumbers
            .Where((SerialNumber s) => s.SerialCode != null && codes.Contains(s.SerialCode) && s.VoidedAt == null)
            .Select((SerialNumber s) => s.SerialCode!)
            .ToListAsync();
        HashSet<string> existingCodeSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<string> codesToCreate = codes.Where((string code) => !existingCodeSet.Contains(code)).ToList();
        int totalAfterRegistration = existingSerialCount + codesToCreate.Count;
        if ((decimal)totalAfterRegistration > requiredBaseQty)
        {
            base.TempData["Error"] = $"Đăng ký {codesToCreate.Count} số sê-ri mới sẽ vượt quá số lượng yêu cầu ({requiredBaseQty}). Đã có {existingSerialCount} số sê-ri được đăng ký.";
            return RedirectToAction("SerialReceiving", new { voucherId });
        }
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            foreach (string code in codesToCreate)
            {
                SerialNumber serial = new SerialNumber
                {
                    SerialCode = code,
                    ItemId = detail.ItemId,
                    OwnerPartnerId = voucher.OwnerPartnerId,
                    VoucherDetailId = voucherDetailId,
                    VoucherId = voucherId,
                    WarehouseId = voucher.WarehouseId,
                    LocationId = detail.LocationId,
                    LotNumber = detail.LotNumber,
                    ExpiryDate = detail.ExpiryDate,
                    ManufacturingDate = detail.ManufacturingDate,
                    CreatedAt = VietnamNow,
                    Status = SerialNumberStatusEnum.Active,
                    HoldStatus = InventoryHoldStatusEnum.Available
                };
                _db.SerialNumbers.Add(serial);
            }
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
            base.TempData["Success"] = $"Đăng ký {codesToCreate.Count} số sê-ri mới thành công. Tổng: {existingSerialCount + codesToCreate.Count}/{detail.BaseQty}.";
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            // P1-R2-1: không leak lỗi hệ thống ra client. Ghi nhận server-side cho debug.
            System.Diagnostics.Debug.WriteLine($"[SerialReceiving] {ex}");
            base.TempData["Error"] = "Lỗi khi đăng ký số sê-ri. Vui lòng thử lại.";
        }
        return RedirectToAction("SerialReceiving", new { voucherId });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReassignTask(long id, string newAssignedTo)
    {
        PickTask? task = await _db.PickTasks.FindAsync(id);
        if (task == null)
        {
            base.TempData["Error"] = "Không tìm thấy nhiệm vụ lấy hàng.";
            return RedirectToAction("PickTasks");
        }
        task.AssignedTo = newAssignedTo;
        task.AssignedAt = VietnamNow;
        task.Status = PickTaskStatusEnum.Assigned;
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = "Đã giao nhiệm vụ cho " + newAssignedTo + ".";
        return RedirectToAction("PickTasks");
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> ScanLpn(string? lpnCode, string? code = null)
    {
        bool jsonMode = !string.IsNullOrWhiteSpace(code);
        lpnCode = string.IsNullOrWhiteSpace(lpnCode) ? code : lpnCode;
        if (string.IsNullOrWhiteSpace(lpnCode))
        {
            if (!jsonMode)
                return View(new List<LicensePlate>());

            return Json(new
            {
                success = false,
                message = "Vui lòng nhập mã kiện (LPN)."
            });
        }
        int? scopedWh = GetScopedWarehouseId();
        if (!scopedWh.HasValue)
        {
            if (!jsonMode)
                return View(new List<LicensePlate>());

            return Json(new
            {
                success = false,
                message = "Không xác định được phạm vi kho."
            });
        }
        IQueryable<LicensePlate> query = from l in _db.LicensePlates.Include((LicensePlate l) => l.CurrentLocation).Include((LicensePlate l) => l.Details).ThenInclude((LicensePlateDetail d) => d.Item)
                                         where l.WarehouseId == ((int?)scopedWh).Value && (l.LpnCode.ToUpper() == lpnCode.ToUpper() || l.LpnCode.Contains(lpnCode))
                                         select l;
        if (!jsonMode)
        {
            List<LicensePlate> lpns = await query
                .OrderByDescending(l => l.LpnCode.ToUpper() == lpnCode.ToUpper())
                .ThenBy(l => l.LpnCode)
                .Take(20)
                .ToListAsync();
            base.ViewBag.LpnCode = lpnCode;
            return View(lpns);
        }

        LicensePlate? lpn = await query
            .OrderByDescending(l => l.LpnCode.ToUpper() == lpnCode.ToUpper())
            .ThenBy(l => l.LpnCode)
            .FirstOrDefaultAsync();
        if (lpn == null)
        {
            return Json(new
            {
                success = false,
                message = "Không tìm thấy mã kiện (LPN)."
            });
        }

        var details = lpn.Details.OrderBy(d => d.LicensePlateDetailId).ToList();
        var first = details.FirstOrDefault();
        var itemCodes = details
            .Select(d => d.Item?.ItemCode ?? d.ItemId.ToString())
            .Distinct()
            .ToList();
        var itemCode = string.Join(", ", itemCodes.Take(2)) + (itemCodes.Count > 2 ? $" +{itemCodes.Count - 2}" : "");
        var itemName = itemCodes.Count switch
        {
            0 => "",
            1 => first?.Item?.ItemName ?? "",
            _ => $"{itemCodes.Count} loại vật tư"
        };

        return Json(new
        {
            success = true,
            data = new
            {
                lpnCode = lpn.LpnCode,
                itemCode,
                itemName,
                locationCode = lpn.CurrentLocation?.LocationCode,
                quantity = details.Sum(d => d.Quantity),
                detailCount = details.Count,
                status = lpn.Status.ToString(),
                lpnType = lpn.LpnType.ToString()
            }
        });
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> ScanSerial(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Json(new
            {
                success = false,
                message = "Vui lòng nhập số sê-ri."
            });
        }
        int? scopedWh = GetScopedWarehouseId();
        SerialNumber? serial = await _db.SerialNumbers.Include((SerialNumber s) => s.Item).Include((SerialNumber s) => s.Location).Include((SerialNumber s) => s.Warehouse)
            .Include((SerialNumber s) => s.Voucher)
            .Include((SerialNumber s) => s.LicensePlate)
            .FirstOrDefaultAsync((SerialNumber s) => (scopedWh == (int?)null || s.WarehouseId == ((int?)scopedWh).Value) && s.SerialCode != null && s.SerialCode.Contains(code));
        if (serial == null)
        {
            return Json(new
            {
                success = false,
                message = "Không tìm thấy số sê-ri."
            });
        }
        return Json(new
        {
            success = true,
            data = new
            {
                serialCode = (serial.SerialCode ?? ""),
                itemCode = (serial.Item?.ItemCode ?? ""),
                itemName = (serial.Item?.ItemName ?? ""),
                locationCode = serial.Location?.LocationCode,
                lpnCode = serial.LicensePlate?.LpnCode,
                warehouseName = (serial.Warehouse?.WarehouseName ?? ""),
                voucherCode = serial.Voucher?.VoucherCode,
                status = serial.Status.ToString()
            }
        });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveOrderStreamingConfig(int warehouseId, bool isEnabled, int minPriority, int deliveryWindowHours, string? notes)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && warehouseId != scopedWh.Value)
        {
            return Forbid();
        }
        if (!(await _db.Warehouses.AnyAsync((Warehouse w) => w.WarehouseId == warehouseId && w.IsActive)))
        {
            base.TempData["Error"] = "Kho đã chọn không hợp lệ.";
            return RedirectToAction("OrderStreamingConfigs", new { warehouseId });
        }
        if (minPriority < 1 || minPriority > 100)
        {
            base.TempData["Error"] = "Mức ưu tiên tối thiểu phải nằm trong khoảng 1 đến 100.";
            return RedirectToAction("OrderStreamingConfigs", new { warehouseId });
        }
        if (deliveryWindowHours < 1 || deliveryWindowHours > 168)
        {
            base.TempData["Error"] = "Cửa sổ ngày giao phải nằm trong khoảng 1 đến 168 giờ.";
            return RedirectToAction("OrderStreamingConfigs", new { warehouseId });
        }
        string actor = base.User.Identity?.Name ?? "system";
        WarehouseOrderStreamingConfig? existing = await _db.WarehouseOrderStreamingConfigs.FirstOrDefaultAsync((WarehouseOrderStreamingConfig c) => c.WarehouseId == warehouseId && c.IsActive);
        if (existing == null)
        {
            _db.WarehouseOrderStreamingConfigs.Add(new WarehouseOrderStreamingConfig
            {
                WarehouseId = warehouseId,
                IsEnabled = isEnabled,
                MinPriority = minPriority,
                DeliveryWindowHours = deliveryWindowHours,
                Notes = notes,
                CreatedBy = actor,
                CreatedAt = VietnamNow,
                IsActive = true
            });
        }
        else
        {
            existing.IsEnabled = isEnabled;
            existing.MinPriority = minPriority;
            existing.DeliveryWindowHours = deliveryWindowHours;
            existing.Notes = notes;
            existing.UpdatedBy = actor;
            existing.UpdatedAt = VietnamNow;
        }
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = "Đã lưu cấu hình phát hành trực tiếp.";
        return RedirectToAction("OrderStreamingConfigs", new { warehouseId });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableOrderStreamingConfig(int id, int? warehouseId = null)
    {
        WarehouseOrderStreamingConfig? config = await _db.WarehouseOrderStreamingConfigs.FirstOrDefaultAsync((WarehouseOrderStreamingConfig c) => c.WarehouseOrderStreamingConfigId == id);
        if (config == null)
        {
            return NotFound();
        }
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && config.WarehouseId != scopedWh.Value)
        {
            return Forbid();
        }
        config.IsActive = false;
        config.IsEnabled = false;
        config.UpdatedBy = base.User.Identity?.Name ?? "system";
        config.UpdatedAt = VietnamNow;
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = "Đã tắt cấu hình phát hành trực tiếp.";
        return RedirectToAction("OrderStreamingConfigs", new
        {
            warehouseId = (scopedWh ?? warehouseId ?? config.WarehouseId)
        });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> SortationConfigs(int? warehouseId)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        base.ViewBag.Warehouses = (await GetVisibleWarehousesAsync());
        base.ViewBag.WarehouseId = warehouseId;
        base.ViewBag.Locations = (await GetVisibleLocationsAsync(warehouseId));
        IQueryable<WarehouseSortationConfig> query = _db.WarehouseSortationConfigs.Include((WarehouseSortationConfig c) => c.Warehouse).Include((WarehouseSortationConfig c) => c.StagingLocation).Include((WarehouseSortationConfig c) => c.SortationLocation)
            .AsNoTracking()
            .AsQueryable();
        if (warehouseId.HasValue)
        {
            query = query.Where((WarehouseSortationConfig c) => c.WarehouseId == ((int?)warehouseId).Value);
        }
        return View(await (from c in query
                           orderby c.Warehouse.WarehouseCode, c.IsActive descending
                           select c).ToListAsync());
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSortationConfig(int warehouseId, int stagingLocationId, int sortationLocationId, string? notes)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && warehouseId != scopedWh.Value)
        {
            return Forbid();
        }
        List<int> locationIds = new int[2] { stagingLocationId, sortationLocationId }.Distinct().ToList();
        if (await _db.Locations.Include((Location l) => l.Zone).CountAsync((Location l) => locationIds.Contains(l.LocationId) && l.IsActive && l.Zone != null && l.Zone.WarehouseId == warehouseId) != locationIds.Count)
        {
            base.TempData["Error"] = "Vị trí tập kết hoặc vị trí phân loại không thuộc kho đã chọn.";
            return RedirectToAction("SortationConfigs", new { warehouseId });
        }
        string actor = base.User.Identity?.Name ?? "system";
        WarehouseSortationConfig? existing = await _db.WarehouseSortationConfigs.FirstOrDefaultAsync((WarehouseSortationConfig c) => c.WarehouseId == warehouseId && c.IsActive);
        if (existing == null)
        {
            _db.WarehouseSortationConfigs.Add(new WarehouseSortationConfig
            {
                WarehouseId = warehouseId,
                StagingLocationId = stagingLocationId,
                SortationLocationId = sortationLocationId,
                Notes = notes,
                CreatedBy = actor,
                CreatedAt = VietnamNow,
                IsActive = true
            });
        }
        else
        {
            existing.StagingLocationId = stagingLocationId;
            existing.SortationLocationId = sortationLocationId;
            existing.Notes = notes;
            existing.UpdatedBy = actor;
            existing.UpdatedAt = VietnamNow;
        }
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = "Đã lưu cấu hình phân loại đơn.";
        return RedirectToAction("SortationConfigs", new { warehouseId });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableSortationConfig(int id, int? warehouseId = null)
    {
        WarehouseSortationConfig? config = await _db.WarehouseSortationConfigs.FirstOrDefaultAsync((WarehouseSortationConfig c) => c.WarehouseSortationConfigId == id);
        if (config == null)
        {
            return NotFound();
        }
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && config.WarehouseId != scopedWh.Value)
        {
            return Forbid();
        }
        config.IsActive = false;
        config.UpdatedBy = base.User.Identity?.Name ?? "system";
        config.UpdatedAt = VietnamNow;
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = "Đã tắt cấu hình phân loại đơn.";
        return RedirectToAction("SortationConfigs", new
        {
            warehouseId = (scopedWh ?? warehouseId ?? config.WarehouseId)
        });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteReplenishment(int itemId, int toLocationId, decimal qty = 0m, int? sourceItemLocationId = null)
    {
        int? scopedWh = GetScopedWarehouseId();
        try
        {
            MovementTask task = await _movementTaskService.CreateReplenishmentTaskAsync(itemId, toLocationId, qty, sourceItemLocationId, scopedWh, base.User.Identity?.Name ?? "system");
            base.TempData["Success"] = "Đã tạo nhiệm vụ điều chuyển " + task.TaskCode + ". Tồn kho sẽ chỉ cập nhật sau khi màn quét xác nhận.";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("Replenishment");
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunReplenishmentAutomation(int? warehouseId, int maxTasks = 50, string? search = null)
    {
        int? scopedWh = GetScopedWarehouseId();
        int? effectiveWarehouseId = scopedWh ?? warehouseId;
        if (!effectiveWarehouseId.HasValue)
        {
            base.TempData["Error"] = "Vui lòng chọn kho trước khi chạy tự động bổ sung hàng.";
            return RedirectToAction("Replenishment", new { warehouseId, search });
        }

        try
        {
            ReplenishmentAutomationRun run = await _replenishmentAutomationService.RunAsync(new ReplenishmentAutomationRunRequest
            {
                WarehouseId = effectiveWarehouseId.Value,
                Actor = base.User.Identity?.Name ?? "system",
                AutoCreateTasks = true,
                MaxTasks = maxTasks,
                Search = search
            });
            base.TempData["Success"] = $"Đã chạy tự động hóa {run.RunCode}: tạo {run.CreatedTaskCount}/{run.SuggestedLineCount} nhiệm vụ bổ sung hàng.";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }

        return RedirectToAction("Replenishment", new
        {
            warehouseId = effectiveWarehouseId,
            search
        });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplySlotting(int itemId, int suggestedLocationId)
    {
        int? scopedWh = GetScopedWarehouseId();
        try
        {
            MovementTask task = await _movementTaskService.CreateReslottingTaskAsync(itemId, suggestedLocationId, scopedWh, base.User.Identity?.Name ?? "system");
            base.TempData["Success"] = "Đã tạo nhiệm vụ điều chuyển " + task.TaskCode + ". Vị trí mặc định sẽ cập nhật sau khi màn quét xác nhận.";
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (BusinessRuleException ex2)
        {
            BusinessRuleException ex3 = ex2;
            base.TempData["Error"] = ex3.Message;
        }
        return RedirectToAction("Slotting");
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AnalyzeItemVelocity(int warehouseId, int periodDays = 90)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        SlottingVelocityAnalysisResult result = await _slottingPlanningService.AnalyzeItemVelocityAsync(warehouseId, periodDays);
        base.TempData["Success"] = $"Đã phân tích velocity cho {result.ClassifiedCount} vật tư: A={result.AClassCount}, B={result.BClassCount}, C={result.CClassCount}.";
        return RedirectToAction("Slotting");
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> GetVelocityHeatmap(int warehouseId)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        var result = await _slottingPlanningService.GetVelocityHeatmapAsync(warehouseId);
        return Json(result);
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordSlaMetrics(long voucherId)
    {
        Voucher? voucher = await _db.Vouchers.FindAsync(voucherId);
        if (voucher == null)
        {
            base.TempData["Error"] = "Không tìm thấy phiếu.";
            return RedirectToAction("Index", "Vouchers");
        }
        if (base.User.Identity?.Name == null)
        {
        }
        if (voucher.InboundStatus == InboundStatusEnum.Completed && voucher.DockAppointmentStart.HasValue)
        {
            DateTime completedAt = voucher.CompletedAt ?? VietnamNow;
            int dockToStockMinutes = (int)(completedAt - voucher.DockAppointmentStart.Value).TotalMinutes;
            int targetMinutes = (voucher.SlaHours ?? 48) * 60;
            _db.SlaMetrics.Add(new SlaMetric
            {
                VoucherId = voucherId,
                SlaType = "DockToStock",
                SlaCode = voucher.SlaCode,
                TargetMinutes = targetMinutes,
                ActualMinutes = dockToStockMinutes,
                StartedAt = voucher.DockAppointmentStart.Value,
                CompletedAt = completedAt,
                VarianceMinutes = dockToStockMinutes - targetMinutes,
                Status = ((dockToStockMinutes <= targetMinutes) ? SlaStatusEnum.OnTime : SlaStatusEnum.Breached)
            });
        }
        if (voucher.FulfillmentStatus == FulfillmentStatusEnum.Completed)
        {
            List<PickTask> pickTasks = await _db.PickTasks.Where((PickTask t) => t.VoucherId == voucherId && t.Status == PickTaskStatusEnum.Completed).ToListAsync();
            if (pickTasks.Any())
            {
                double avgPickMinutes = (from t in pickTasks
                                         where t.CompletedAt.HasValue && t.AssignedAt.HasValue
                                         select (int)(t.CompletedAt.GetValueOrDefault() - t.AssignedAt.GetValueOrDefault()).TotalMinutes).DefaultIfEmpty(0).Average();
                DateTime startTime = pickTasks.Where((PickTask t) => t.AssignedAt.HasValue).Min((PickTask t) => t.AssignedAt) ?? VietnamNow;
                DateTime endTime = pickTasks.Where((PickTask t) => t.CompletedAt.HasValue).Max((PickTask t) => t.CompletedAt) ?? VietnamNow;
                _db.SlaMetrics.Add(new SlaMetric
                {
                    VoucherId = voucherId,
                    SlaType = "PickSla",
                    TargetMinutes = 60,
                    ActualMinutes = (int)avgPickMinutes,
                    StartedAt = startTime,
                    CompletedAt = endTime,
                    VarianceMinutes = (int)avgPickMinutes - 60,
                    Status = ((avgPickMinutes <= 60.0) ? SlaStatusEnum.OnTime : SlaStatusEnum.Breached)
                });
            }
        }
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = "Đã ghi nhận chỉ số cam kết dịch vụ.";
        return RedirectToAction("Details", "Vouchers", new
        {
            id = voucherId
        });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunCapacitySimulation(CapacityScenario scenario)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            scenario.WarehouseId = scopedWh.Value;
        }
        if (string.IsNullOrWhiteSpace(scenario.ScenarioName))
        {
            throw WmsExceptions.KichBanEmpty();
        }
        scenario.CreatedBy = base.User.Identity?.Name ?? "system";
        scenario.CreatedAt = VietnamNow;
        scenario.ScenarioDate = VietnamNow.Date;
        decimal effectiveVolume = (decimal)scenario.DailyVolume * (1m + (decimal)scenario.VolumeGrowthPct / 100m);
        decimal peakVolume = effectiveVolume * scenario.PeakFactor;
        List<string> bottlenecks = new List<string>();
        List<string> recommendations = new List<string>();
        string criticalBottleneck = "";
        int confidence = 80;
        decimal avgUnloadMinutes = 30m;
        decimal docksNeededForBaseline = (decimal)scenario.DailyVolume * avgUnloadMinutes / (decimal)(scenario.WorkingHoursPerDay * 60);
        decimal docksNeededForPeak = peakVolume * avgUnloadMinutes / (decimal)(scenario.WorkingHoursPerDay * 60);
        if (docksNeededForBaseline > (decimal)scenario.DockCount)
        {
            bottlenecks.Add($"Dock: cần {Math.Ceiling(docksNeededForBaseline)} cửa, hiện có {scenario.DockCount}");
            recommendations.Add($"Cần thêm {Math.Max(1, (int)Math.Ceiling(docksNeededForBaseline) - scenario.DockCount)} dock door");
            criticalBottleneck = ((criticalBottleneck == "") ? "Dock" : criticalBottleneck);
        }
        decimal avgMinutesPerOrder = 5m;
        decimal laborHoursNeeded = effectiveVolume * avgMinutesPerOrder / 60m;
        int laborHoursAvailable = scenario.LaborCount * scenario.WorkingHoursPerDay;
        if (laborHoursNeeded > (decimal)laborHoursAvailable)
        {
            bottlenecks.Add($"Nhân lực: cần {Math.Ceiling(laborHoursNeeded)}h, có {laborHoursAvailable}h");
            recommendations.Add($"Cần thêm {Math.Max(1, (int)Math.Ceiling(laborHoursNeeded / (decimal)scenario.WorkingHoursPerDay) - scenario.LaborCount)} nhân viên");
            criticalBottleneck = ((criticalBottleneck == "") ? "Labor" : criticalBottleneck);
        }
        if (scenario.PeakFactor > 1.5m)
        {
            bottlenecks.Add($"Cao điểm: hệ số {scenario.PeakFactor:P0} cao hơn bình thường");
            recommendations.Add("Cần cấu hình ca làm thêm hoặc điều phối đơn hàng ra ngoài giờ cao điểm");
            confidence -= 10;
        }
        scenario.Bottlenecks = string.Join("; ", bottlenecks);
        scenario.Recommendations = string.Join("; ", recommendations);
        scenario.CriticalBottleneck = criticalBottleneck;
        scenario.ConfidenceScore = confidence;
        scenario.ResultJson = JsonSerializer.Serialize(new
        {
            effectiveVolume = effectiveVolume,
            peakVolume = peakVolume,
            docksNeededBaseline = Math.Ceiling(docksNeededForBaseline),
            docksNeededPeak = Math.Ceiling(docksNeededForPeak),
            laborHoursNeeded = Math.Ceiling(laborHoursNeeded),
            laborHoursAvailable = laborHoursAvailable,
            utilizationDock = ((scenario.DockCount > 0) ? (docksNeededForBaseline / (decimal)scenario.DockCount * 100m) : 0m),
            utilizationLabor = ((laborHoursAvailable > 0) ? (laborHoursNeeded / (decimal)laborHoursAvailable * 100m) : 0m)
        });
        _db.CapacityScenarios.Add(scenario);
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = $"Đã chạy mô phỏng [{scenario.ScenarioName}]. Nút thắt: {(bottlenecks.Any() ? string.Join(", ", bottlenecks) : "Không có")}";
        return RedirectToAction("CapacitySimulation", new
        {
            warehouseId = scenario.WarehouseId
        });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitInspection(long voucherId, long voucherDetailId, int itemId, decimal totalQty, decimal sampleQty, decimal passedQty, decimal failedQty, int disposition, string? defectDescription, string? notes)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            string actor = base.User.Identity?.Name ?? "system";
            Voucher? voucher = await _db.Vouchers.FindAsync(voucherId);
            if (voucher == null)
            {
                base.TempData["Error"] = "Phiếu không tồn tại.";
                return RedirectToAction("QualityInspection");
            }
            VoucherDetail? detail = await _db.VoucherDetails.FindAsync(voucherDetailId);
            if (detail == null)
            {
                base.TempData["Error"] = "Dòng phiếu không tồn tại.";
                return RedirectToAction("QualityInspection");
            }
            QualityStatusEnum overallResult = ((failedQty > 0m) ? QualityStatusEnum.Failed : QualityStatusEnum.Passed);
            QcDispositionEnum qualityDisposition = (QcDispositionEnum)disposition;
            QualityInspection inspection = new QualityInspection
            {
                VoucherId = voucherId,
                VoucherDetailId = voucherDetailId,
                ItemId = itemId,
                WarehouseId = voucher.WarehouseId,
                TotalQty = totalQty,
                SampleQty = sampleQty,
                PassedQty = passedQty,
                FailedQty = failedQty,
                SamplePercent = ((sampleQty > 0m && totalQty > 0m) ? (sampleQty / totalQty * 100m) : 0m),
                Disposition = qualityDisposition,
                OverallResult = overallResult,
                InspectorName = actor,
                InspectedAt = VietnamNow,
                Notes = notes,
                DefectDescription = defectDescription
            };
            _db.QualityInspections.Add(inspection);
            bool flag = overallResult == QualityStatusEnum.Failed;
            bool flag2 = flag;
            if (!flag2)
            {
                bool flag3;
                switch (qualityDisposition)
                {
                    case QcDispositionEnum.Reject:
                    case QcDispositionEnum.ReturnToSupplier:
                    case QcDispositionEnum.Hold:
                        flag3 = true;
                        break;
                    default:
                        flag3 = false;
                        break;
                }
                flag2 = flag3;
            }
            if (flag2)
            {
                detail.QualityStatus = ((failedQty > 0m) ? QualityStatusEnum.Defect : QualityStatusEnum.OnHold);
                detail.Notes = (string.IsNullOrWhiteSpace(detail.Notes) ? $"[QC {VietnamNow:yyyy-MM-dd HH:mm}] {defectDescription ?? qualityDisposition.ToString()}" : (detail.Notes + $"; [QC {VietnamNow:yyyy-MM-dd HH:mm}] {defectDescription ?? qualityDisposition.ToString()}"));
                List<ItemLocation> affectedItemLocations = await _db.ItemLocations.Where((ItemLocation itemLocation) => itemLocation.ItemId == itemId && itemLocation.OwnerPartnerId == detail.OwnerPartnerId && (int?)itemLocation.LocationId == detail.LocationId && (detail.LotNumber == null || itemLocation.LotNumber == detail.LotNumber) && itemLocation.HoldStatus == InventoryHoldStatusEnum.Available).ToListAsync();
                foreach (ItemLocation il in affectedItemLocations)
                {
                    InventoryHoldStatusEnum previousStatus = il.HoldStatus;
                    il.HoldStatus = InventoryHoldStatusEnum.Quarantine;
                    il.UpdatedAt = VietnamNow;
                    _db.AuditLogs.Add(new AuditLog
                    {
                        TableName = "ItemLocation",
                        RecordId = il.ItemLocationId.ToString(),
                        ActionType = "AUTO_QUARANTINE_BY_QC",
                        ColumnChanged = "HoldStatus",
                        OldValue = previousStatus.ToString(),
                        NewValue = $"QC:{qualityDisposition};DefectQty:{failedQty};InspectionId:{inspection.QualityInspectionId}",
                        ChangedBy = actor,
                        ChangedAt = VietnamNow,
                        IpAddress = base.HttpContext.Connection.RemoteIpAddress?.ToString(),
                        AppModule = "QualityInspection"
                    });
                }
                base.TempData["Warning"] = $"Đã tự động cách ly {affectedItemLocations.Count} vị trí tồn kho. Vật tư sẽ bị chặn xuất cho đến khi QC giải quyết.";
            }
            else
            {
                detail.QualityStatus = QualityStatusEnum.Passed;
            }
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
            base.TempData["Success"] = ((overallResult == QualityStatusEnum.Passed) ? "Kiểm tra đạt chất lượng. Hàng được phép xuất kho." : "Kiểm tra không đạt. Hàng đã tự động bị cách ly.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            // P1-R2-1: không leak lỗi hệ thống ra client.
            System.Diagnostics.Debug.WriteLine($"[QualityInspection] {ex}");
            base.TempData["Error"] = "Lỗi khi ghi nhận kiểm tra. Vui lòng thử lại.";
        }
        return RedirectToAction("QualityInspection", new
        {
            warehouseId = GetScopedWarehouseId()
        });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReleaseQuarantine(long voucherId, long voucherDetailId, int itemId, int disposition, string? resolutionNote)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            string actor = base.User.Identity?.Name ?? "system";
            VoucherDetail? detail = await _db.VoucherDetails.FindAsync(voucherDetailId);
            if (detail == null)
            {
                base.TempData["Error"] = "Dòng phiếu không tồn tại.";
                return RedirectToAction("Details", "Vouchers", new
                {
                    id = voucherId
                });
            }
            if (detail.QualityStatus != QualityStatusEnum.OnHold && detail.QualityStatus != QualityStatusEnum.Defect)
            {
                base.TempData["Info"] = "Dòng này không bị giữ/cách ly.";
                return RedirectToAction("Details", "Vouchers", new
                {
                    id = voucherId
                });
            }
            QcDispositionEnum qualityDisposition = (QcDispositionEnum)disposition;
            QcDispositionEnum qcDispositionEnum = qualityDisposition;
            if ((qcDispositionEnum == QcDispositionEnum.Accept || qcDispositionEnum == QcDispositionEnum.Rework || qcDispositionEnum == QcDispositionEnum.AcceptWithConditions) ? true : false)
            {
                detail.QualityStatus = QualityStatusEnum.Passed;
                detail.Notes = (string.IsNullOrWhiteSpace(detail.Notes) ? $"[QC RELEASE {VietnamNow:yyyy-MM-dd HH:mm}] {resolutionNote ?? qualityDisposition.ToString()}" : (detail.Notes + $"; [QC RELEASE {VietnamNow:yyyy-MM-dd HH:mm}] {resolutionNote ?? qualityDisposition.ToString()}"));
                List<ItemLocation> affectedItemLocations = await _db.ItemLocations.Where((ItemLocation itemLocation) => itemLocation.ItemId == itemId && itemLocation.OwnerPartnerId == detail.OwnerPartnerId && (int?)itemLocation.LocationId == detail.LocationId && (detail.LotNumber == null || itemLocation.LotNumber == detail.LotNumber) && itemLocation.HoldStatus == InventoryHoldStatusEnum.Quarantine).ToListAsync();
                foreach (ItemLocation il in affectedItemLocations)
                {
                    il.HoldStatus = InventoryHoldStatusEnum.Available;
                    il.UpdatedAt = VietnamNow;
                    _db.AuditLogs.Add(new AuditLog
                    {
                        TableName = "ItemLocation",
                        RecordId = il.ItemLocationId.ToString(),
                        ActionType = "QC_RELEASE_QUARANTINE",
                        ColumnChanged = "HoldStatus",
                        OldValue = "Quarantine",
                        NewValue = $"Released:{qualityDisposition};Note:{resolutionNote}",
                        ChangedBy = actor,
                        ChangedAt = VietnamNow,
                        IpAddress = base.HttpContext.Connection.RemoteIpAddress?.ToString(),
                        AppModule = "QualityInspection"
                    });
                }
                base.TempData["Success"] = $"Đã giải phóng cách ly cho {affectedItemLocations.Count} vị trí. Vật tư có thể xuất kho.";
            }
            else
            {
                base.TempData["Warning"] = $"Phương án xử lý [{qualityDisposition}] không cho phép giải phóng tự động. Vật tư vẫn bị cách ly.";
            }
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            // P1-R2-1: không leak lỗi hệ thống ra client.
            System.Diagnostics.Debug.WriteLine($"[Operation] {ex}");
            base.TempData["Error"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
        }
        return RedirectToAction("Details", "Vouchers", new
        {
            id = voucherId
        });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCart(string cartCode, int warehouseId, int toteCapacity, string? notes)
    {
        if (string.IsNullOrWhiteSpace(cartCode))
        {
            base.TempData["Error"] = "Mã xe đẩy không được để trống.";
            return RedirectToAction("CartManagement");
        }
        if (await _db.PickCarts.AnyAsync((PickCart c) => c.CartCode == cartCode.Trim()))
        {
            base.TempData["Error"] = "Mã xe đẩy [" + cartCode.Trim() + "] đã tồn tại.";
            return RedirectToAction("CartManagement");
        }
        if (toteCapacity < 1 || toteCapacity > 20)
        {
            toteCapacity = 6;
        }
        PickCart cart = new PickCart
        {
            CartCode = cartCode.Trim(),
            WarehouseId = warehouseId,
            ToteCapacity = toteCapacity,
            Notes = notes?.Trim(),
            Status = PickCartStatusEnum.Available
        };
        _db.PickCarts.Add(cart);
        for (int i = 1; i <= toteCapacity; i++)
        {
            _db.PickTotes.Add(new PickTote
            {
                ToteCode = $"{cartCode.Trim()}-T{i:D2}",
                PickCart = cart,
                SlotPosition = i,
                Status = PickToteStatusEnum.Empty
            });
        }
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = $"Đã tạo xe đẩy [{cart.CartCode}] với {toteCapacity} khay hàng.";
        return RedirectToAction("CartManagement");
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCart(int cartId)
    {
        PickCart? cart = await _db.PickCarts.Include((PickCart c) => c.Totes).FirstOrDefaultAsync((PickCart c) => c.PickCartId == cartId);
        if (cart == null)
        {
            base.TempData["Error"] = "Không tìm thấy xe đẩy.";
            return RedirectToAction("CartManagement");
        }
        if (cart.Totes.Any((PickTote t) => t.Status != PickToteStatusEnum.Empty))
        {
            base.TempData["Error"] = "Xe đẩy đang có khay hàng được sử dụng. Không thể xóa.";
            return RedirectToAction("CartManagement");
        }
        _db.PickTotes.RemoveRange(cart.Totes);
        _db.PickCarts.Remove(cart);
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = "Đã xóa xe đẩy [" + cart.CartCode + "].";
        return RedirectToAction("CartManagement");
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveToteAssignment(long waveId, Dictionary<long, long> voucherToteMap)
    {
        Wave? wave = await _db.Waves.Include((Wave w) => w.Vouchers).FirstOrDefaultAsync((Wave w) => w.WaveId == waveId);
        if (wave == null)
        {
            base.TempData["Error"] = "Không tìm thấy sóng.";
            return RedirectToAction("Waves");
        }
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && wave.WarehouseId != scopedWh.Value)
        {
            return Forbid();
        }
        string actor = base.User.Identity?.Name ?? "system";
        foreach (PickTote t in await _db.PickTotes.Where((PickTote pickTote) => pickTote.WaveId == (long?)waveId).ToListAsync())
        {
            t.WaveId = null;
            t.VoucherId = null;
            t.Status = PickToteStatusEnum.Empty;
            t.AssignedBy = null;
            t.AssignedAt = null;
            t.UpdatedAt = VietnamNow;
        }
        int assignedCount = 0;
        foreach (KeyValuePair<long, long> item in voucherToteMap)
        {
            var voucherId = item.Key;
            var toteId = item.Value;
            if (toteId > 0)
            {
                PickTote? tote = await _db.PickTotes.FirstOrDefaultAsync((PickTote pickTote) => pickTote.PickToteId == toteId);
                if (tote != null)
                {
                    tote.WaveId = waveId;
                    tote.VoucherId = voucherId;
                    tote.Status = PickToteStatusEnum.Assigned;
                    tote.AssignedBy = actor;
                    tote.AssignedAt = VietnamNow;
                    tote.UpdatedAt = VietnamNow;
                    assignedCount++;
                }
            }
        }
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = $"Đã gán {assignedCount} khay hàng cho đợt gom đơn [{wave.WaveCode}].";
        return RedirectToAction("AssignTotes", new { waveId });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveZoneAssignment(int userId, List<int> zoneIds)
    {
        int? scopedWh = GetScopedWarehouseId();
        AppUser? user = await _db.AppUsers.FindAsync(userId);
        if (user == null)
        {
            base.TempData["Error"] = "Không tìm thấy nhân viên.";
            return RedirectToAction("ZoneAssignment");
        }
        if (scopedWh.HasValue && user.WarehouseId != scopedWh.Value)
        {
            return Forbid();
        }
        List<int> requestedZoneIds = zoneIds == null ? new List<int>() : zoneIds.Distinct().ToList();
        if (requestedZoneIds.Count > 0)
        {
            IQueryable<Zone> validZonesQuery = _db.Zones.AsNoTracking()
                .Where((Zone z) => z.IsActive && requestedZoneIds.Contains(z.ZoneId));
            if (scopedWh.HasValue)
            {
                validZonesQuery = validZonesQuery.Where((Zone z) => z.WarehouseId == scopedWh.Value);
            }
            if (user.WarehouseId.HasValue)
            {
                validZonesQuery = validZonesQuery.Where((Zone z) => z.WarehouseId == user.WarehouseId.Value);
            }
            List<int> validZoneIds = await validZonesQuery.Select((Zone z) => z.ZoneId).ToListAsync();
            if (validZoneIds.Count != requestedZoneIds.Count)
            {
                if (scopedWh.HasValue)
                {
                    return Forbid();
                }
                base.TempData["Error"] = "Khu vực được chọn không hợp lệ hoặc không thuộc kho của nhân viên.";
                return RedirectToAction("ZoneAssignment");
            }
            requestedZoneIds = validZoneIds;
        }
        string actor = base.User.Identity?.Name ?? "system";
        List<UserZoneAssignment> old = await _db.UserZoneAssignments.Where((UserZoneAssignment x) => x.UserId == userId).ToListAsync();
        _db.UserZoneAssignments.RemoveRange(old);
        if (requestedZoneIds.Count > 0)
        {
            foreach (int zoneId in requestedZoneIds)
            {
                _db.UserZoneAssignments.Add(new UserZoneAssignment
                {
                    UserId = userId,
                    ZoneId = zoneId,
                    AssignedBy = actor
                });
            }
        }
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = $"Đã cập nhật khu vực cho [{user.FullName}]: {requestedZoneIds.Count} khu vực.";
        return RedirectToAction("ZoneAssignment");
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptNextTask(TaskCategoryEnum category, long taskId, int? warehouseId)
    {
        int? scopedWh = GetScopedWarehouseId();
        List<int>? scopedZoneIds = await GetScopedZoneIdsAsync();
        string actor = base.User.Identity?.Name ?? "system";
        try
        {
            if (category == TaskCategoryEnum.Pick)
            {
                PickTask? task = await _db.PickTasks.Include((PickTask t) => t.Wave).Include((PickTask t) => t.Voucher).Include((PickTask t) => t.SourceLocation)
                    .FirstOrDefaultAsync((PickTask t) => t.PickTaskId == taskId);
                if (task == null)
                {
                    base.TempData["Error"] = "Không tìm thấy nhiệm vụ lấy hàng.";
                    return RedirectToAction("NextTask", new
                    {
                        warehouseId = (scopedWh ?? warehouseId)
                    });
                }
                Wave? wave = task.Wave;
                int? taskWarehouseId = ((wave != null) ? new int?(wave.WarehouseId) : task.Voucher?.WarehouseId);
                if (scopedWh.HasValue && taskWarehouseId != scopedWh.Value)
                {
                    throw new UnauthorizedAccessException("Bạn không thể nhận nhiệm vụ lấy hàng từ kho khác.");
                }
                if (!scopedWh.HasValue && warehouseId.HasValue && taskWarehouseId.HasValue && taskWarehouseId.Value != warehouseId.Value)
                {
                    throw new BusinessRuleException("Nhiệm vụ lấy hàng không thuộc kho đang chọn.", "PICK_TASK_WAREHOUSE_MISMATCH", "PickTask");
                }
                if (scopedZoneIds != null && (task.SourceLocation == null || !scopedZoneIds.Contains(task.SourceLocation.ZoneId)))
                {
                    throw new UnauthorizedAccessException("Bạn không thể nhận nhiệm vụ lấy hàng ngoài zone được phân công.");
                }
                PickTaskStatusEnum status = task.Status;
                if (status - 1 > PickTaskStatusEnum.Assigned)
                {
                    throw new BusinessRuleException("Nhiệm vụ lấy hàng đã kết thúc, không thể nhận lại.", "PICK_TASK_CLOSED", "PickTask");
                }
                if (!string.IsNullOrWhiteSpace(task.AssignedTo) && !string.Equals(task.AssignedTo, actor, StringComparison.OrdinalIgnoreCase))
                {
                    throw new BusinessRuleException($"Nhiệm vụ lấy hàng {task.TaskCode} đã được gán cho {task.AssignedTo}.", "PICK_TASK_ASSIGNED_TO_OTHER", "PickTask");
                }
                if (string.IsNullOrWhiteSpace(task.AssignedTo))
                {
                    if (task.Status != PickTaskStatusEnum.Pending)
                    {
                        throw new BusinessRuleException("Nhiệm vụ lấy hàng không ở trạng thái chờ nhận.", "PICK_TASK_NOT_PENDING", "PickTask");
                    }
                    task.AssignedTo = actor;
                    task.AssignedAt = VietnamNow;
                    task.Status = PickTaskStatusEnum.Assigned;
                    await _unitOfWork.SaveChangesAsync();
                }
                base.TempData["Success"] = "Đã nhận nhiệm vụ lấy hàng " + task.TaskCode + ".";
                return RedirectToAction("RfPicking", new
                {
                    warehouseId = (scopedWh ?? warehouseId)
                });
            }
            MovementTask task2 = await _movementTaskService.AcceptAsync(taskId, scopedWh, actor);
            base.TempData["Success"] = "Đã nhận nhiệm vụ điều chuyển " + task2.TaskCode + ".";
            return RedirectToAction("RfMovement", new
            {
                warehouseId = (scopedWh ?? task2.WarehouseId)
            });
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("NextTask", new
        {
            warehouseId = (scopedWh ?? warehouseId)
        });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveYardBillingRate(int warehouseId, int? partnerId, string? carrierName, TrailerTypeEnum? trailerType, YardSpotTypeEnum? spotType, int freeTimeMinutes, decimal ratePerHour, decimal maxDailyRate, string currency, string? notes, int? yardBillingRateId)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && warehouseId != scopedWh.Value)
        {
            return Forbid();
        }
        if (freeTimeMinutes < 0)
        {
            base.TempData["Error"] = "Thời gian miễn phí không được âm.";
            return RedirectToAction("YardBillingRates", new { warehouseId });
        }
        if (ratePerHour < 0m)
        {
            base.TempData["Error"] = "Đơn giá mỗi giờ không được âm.";
            return RedirectToAction("YardBillingRates", new { warehouseId });
        }
        if (maxDailyRate < 0m)
        {
            base.TempData["Error"] = "Trần phí mỗi ngày không được âm.";
            return RedirectToAction("YardBillingRates", new { warehouseId });
        }
        bool hasValue = partnerId.HasValue;
        bool flag = hasValue;
        if (flag)
        {
            flag = !(await _db.Partners.AnyAsync((Partner p) => p.PartnerId == partnerId.GetValueOrDefault() && p.IsActive));
        }
        if (flag)
        {
            base.TempData["Error"] = "Không tìm thấy khách hàng/đối tác áp dụng.";
            return RedirectToAction("YardBillingRates", new { warehouseId });
        }
        string actor = base.User.Identity?.Name ?? "system";
        string? cleanCarrier = string.IsNullOrWhiteSpace(carrierName) ? null : carrierName.Trim();
        if (yardBillingRateId.HasValue && yardBillingRateId.Value > 0)
        {
            YardBillingRate? existing = await _db.YardBillingRates.FindAsync(yardBillingRateId.Value);
            if (existing == null)
            {
                return NotFound();
            }
            if (scopedWh.HasValue && existing.WarehouseId != scopedWh.Value)
            {
                return Forbid();
            }
            existing.WarehouseId = warehouseId;
            existing.PartnerId = partnerId;
            existing.CarrierName = cleanCarrier;
            existing.TrailerType = trailerType;
            existing.SpotType = spotType;
            existing.FreeTimeMinutes = freeTimeMinutes;
            existing.RatePerHour = ratePerHour;
            existing.MaxDailyRate = maxDailyRate;
            existing.Currency = (string.IsNullOrWhiteSpace(currency) ? "VND" : currency.Trim().ToUpperInvariant());
            existing.Notes = (string.IsNullOrWhiteSpace(notes) ? null : notes.Trim());
            existing.UpdatedBy = actor;
            existing.UpdatedAt = VietnamNow;
        }
        else
        {
            _db.YardBillingRates.Add(new YardBillingRate
            {
                WarehouseId = warehouseId,
                PartnerId = partnerId,
                CarrierName = cleanCarrier,
                TrailerType = trailerType,
                SpotType = spotType,
                FreeTimeMinutes = freeTimeMinutes,
                RatePerHour = ratePerHour,
                MaxDailyRate = maxDailyRate,
                Currency = (string.IsNullOrWhiteSpace(currency) ? "VND" : currency.Trim().ToUpperInvariant()),
                Notes = (string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()),
                CreatedBy = actor,
                CreatedAt = VietnamNow,
                IsActive = true
            });
        }
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = "Đã lưu bảng giá phí lưu bãi.";
        return RedirectToAction("YardBillingRates", new { warehouseId });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleYardBillingRateActive(int yardBillingRateId, int? warehouseId)
    {
        int? scopedWh = GetScopedWarehouseId();
        YardBillingRate? rate = await _db.YardBillingRates.FindAsync(yardBillingRateId);
        if (rate == null)
        {
            return NotFound();
        }
        if (scopedWh.HasValue && rate.WarehouseId != scopedWh.Value)
        {
            return Forbid();
        }
        rate.IsActive = !rate.IsActive;
        rate.UpdatedBy = base.User.Identity?.Name ?? "system";
        rate.UpdatedAt = VietnamNow;
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = (rate.IsActive ? "Đã kích hoạt bảng giá." : "Đã ngừng áp dụng bảng giá.");
        return RedirectToAction("YardBillingRates", new
        {
            warehouseId = (scopedWh ?? warehouseId ?? rate.WarehouseId)
        });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> ExportYardBillingChargesExcel(int? warehouseId, YardChargeStatusEnum? status, DateTime? dateFrom, DateTime? dateTo)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        List<YardBillingChargeRow> rows = _yardBillingQueryService.MapChargeRows(await _yardBillingQueryService
            .BuildChargeQuery(warehouseId, status, dateFrom, dateTo)
            .OrderByDescending(c => c.CreatedAt)
            .Take(5000)
            .ToListAsync());
        using XLWorkbook workbook = new XLWorkbook();
        IXLWorksheet ws = workbook.Worksheets.Add("PhiLuuBai");
        string[] headers = new string[19]
        {
            "Mã lượt", "Kho", "Xe/Công-ten-nơ", "Đơn vị vận chuyển", "Khách hàng/đối tác", "Vào cổng", "Ra cổng", "Thời gian lưu bãi (phút)", "Miễn phí (phút)", "Tính phí (phút)",
            "Giá/giờ", "Thành tiền", "Tiền tệ", "Trạng thái", "Xác nhận bởi", "Xác nhận lúc", "Miễn bởi", "Lý do miễn", "Ngày tạo"
        };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }
        int r = 2;
        foreach (YardBillingChargeRow row in rows)
        {
            ws.Cell(r, 1).Value = row.VisitCode;
            ws.Cell(r, 2).Value = row.WarehouseName;
            ws.Cell(r, 3).Value = (string.IsNullOrWhiteSpace(row.ContainerNumber) ? row.TrailerNumber : (row.TrailerNumber + " / " + row.ContainerNumber));
            ws.Cell(r, 4).Value = row.CarrierName ?? "";
            ws.Cell(r, 5).Value = row.PartnerName ?? "";
            ws.Cell(r, 6).Value = row.GateInAt?.ToString("dd/MM/yyyy HH:mm") ?? "";
            ws.Cell(r, 7).Value = row.GateOutAt?.ToString("dd/MM/yyyy HH:mm") ?? "";
            ws.Cell(r, 8).Value = row.TotalDwellMinutes;
            ws.Cell(r, 9).Value = row.FreeTimeMinutes;
            ws.Cell(r, 10).Value = row.ChargeableMinutes;
            ws.Cell(r, 11).Value = row.AppliedRatePerHour;
            ws.Cell(r, 12).Value = row.Amount;
            ws.Cell(r, 13).Value = row.Currency;
            ws.Cell(r, 14).Value = _yardBillingQueryService.StatusText(row.Status);
            ws.Cell(r, 15).Value = row.ConfirmedBy ?? "";
            ws.Cell(r, 16).Value = row.ConfirmedAt?.ToString("dd/MM/yyyy HH:mm") ?? "";
            ws.Cell(r, 17).Value = row.WaivedBy ?? "";
            ws.Cell(r, 18).Value = row.WaivedReason ?? "";
            ws.Cell(r, 19).Value = row.CreatedAt.ToString("dd/MM/yyyy HH:mm");
            r++;
        }
        ws.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();
        ws.Column(11).Style.NumberFormat.Format = "#,##0";
        ws.Column(12).Style.NumberFormat.Format = "#,##0";
        using MemoryStream stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(fileDownloadName: $"yard-billing-{VietnamNow:yyyyMMdd-HHmm}.xlsx", fileContents: stream.ToArray(), contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmYardCharge(long chargeId, int? warehouseId)
    {
        try
        {
            await _yardBillingService.ConfirmChargeAsync(chargeId, GetScopedWarehouseId(), base.User.Identity?.Name ?? "system");
            base.TempData["Success"] = "Đã xác nhận phí lưu bãi.";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("YardBillingCharges", new { warehouseId });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WaiveYardCharge(long chargeId, string reason, int? warehouseId)
    {
        try
        {
            await _yardBillingService.WaiveChargeAsync(chargeId, reason, GetScopedWarehouseId(), base.User.Identity?.Name ?? "system");
            base.TempData["Success"] = "Đã miễn phí lưu bãi.";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("YardBillingCharges", new { warehouseId });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CalculateYardCharge(long yardVisitId, int? warehouseId)
    {
        try
        {
            int? scopedWh = GetScopedWarehouseId();
            if (scopedWh.HasValue)
            {
                int? visitWarehouseId = await (from v in _db.YardVisits.AsNoTracking()
                                               where v.YardVisitId == yardVisitId
                                               select v).Select((Expression<Func<YardVisit, int?>>)((YardVisit v) => v.WarehouseId)).FirstOrDefaultAsync();
                if (visitWarehouseId.HasValue && visitWarehouseId.Value != scopedWh.Value)
                {
                    return Forbid();
                }
            }
            YardBillingCharge? charge = await _yardBillingService.CalculateChargeAsync(yardVisitId, base.User.Identity?.Name ?? "system");
            base.TempData["Success"] = ((charge != null) ? $"Đã tạo phí lưu bãi: {charge.Amount:N0} {charge.Currency}." : "Lượt vào bãi chưa vượt thời gian miễn phí.");
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("YardBillingCharges", new { warehouseId });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecalculateAllDraftCharges(int? warehouseId, YardChargeStatusEnum? status, DateTime? dateFrom, DateTime? dateTo)
    {
        try
        {
            int count = await _yardBillingService.RecalculateDraftChargesAsync(warehouseId, GetScopedWarehouseId(), base.User.Identity?.Name ?? "system");
            base.TempData["Success"] = $"Đã tính lại {count:N0} dòng phí Nháp.";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("YardBillingCharges", new { warehouseId, status, dateFrom, dateTo });
    }

}
