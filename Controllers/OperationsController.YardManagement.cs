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

    private async Task<YardManagementPageViewModel> BuildYardManagementAsync(int? warehouseId, string? search)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        DateTime now = VietnamNow;
        List<Warehouse> warehouses = await (from w in _db.Warehouses.AsNoTracking()
                                            where w.IsActive && (!((int?)scopedWh).HasValue || w.WarehouseId == ((int?)scopedWh).Value)
                                            orderby w.WarehouseCode
                                            select w).ToListAsync();
        IQueryable<YardSpot> spotQuery = from s in _db.YardSpots.AsNoTracking().Include((YardSpot s) => s.Warehouse)
                                         where !((int?)warehouseId).HasValue || s.WarehouseId == ((int?)warehouseId).Value
                                         select s;
        IQueryable<YardVisit> visitQuery = from v in _db.YardVisits.AsNoTracking().Include((YardVisit v) => v.Warehouse).Include((YardVisit v) => v.Trailer)
                .Include((YardVisit v) => v.CurrentSpot)
                .Include((YardVisit v) => v.Voucher)
                                           where v.GateOutAt == null && v.Status != YardVisitStatusEnum.GatedOut && v.Status != YardVisitStatusEnum.Cancelled && (!((int?)warehouseId).HasValue || v.WarehouseId == ((int?)warehouseId).Value)
                                           select v;
        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();
            visitQuery = visitQuery.Where((YardVisit v) => v.VisitCode.Contains(keyword) || (v.Trailer != null && v.Trailer.TrailerNumber.Contains(keyword)) || (v.Trailer != null && v.Trailer.ContainerNumber != null && v.Trailer.ContainerNumber.Contains(keyword)) || (v.Voucher != null && v.Voucher.VoucherCode.Contains(keyword)) || (v.CurrentSpot != null && v.CurrentSpot.SpotCode.Contains(keyword)));
            spotQuery = spotQuery.Where((YardSpot s) => s.SpotCode.Contains(keyword) || (s.SpotName != null && s.SpotName.Contains(keyword)));
        }
        List<YardVisitRow> activeVisits = (await (from v in visitQuery
                                                  orderby (v.CurrentSpot == null) ? 0 : 1, v.GateInAt
                                                  select v).Take(500).ToListAsync()).Select((YardVisit v) => new YardVisitRow
                                                  {
                                                      YardVisitId = v.YardVisitId,
                                                      VisitCode = v.VisitCode,
                                                      WarehouseId = v.WarehouseId,
                                                      WarehouseName = (v.Warehouse?.WarehouseName ?? ""),
                                                      TrailerId = v.TrailerId,
                                                      TrailerNumber = (v.Trailer?.TrailerNumber ?? ""),
                                                      ContainerNumber = v.Trailer?.ContainerNumber,
                                                      TrailerType = (v.Trailer?.TrailerType ?? TrailerTypeEnum.Trailer),
                                                      CarrierName = v.Trailer?.CarrierName,
                                                      SealNumber = v.Trailer?.SealNumber,
                                                      CurrentSpotId = v.CurrentSpotId,
                                                      CurrentSpotCode = v.CurrentSpot?.SpotCode,
                                                      Purpose = v.Purpose,
                                                      Status = v.Status,
                                                      GateInAt = v.GateInAt,
                                                      GateOutAt = v.GateOutAt,
                                                      DwellMinutes = v.GetDwellMinutes(now),
                                                      VoucherId = v.VoucherId,
                                                      VoucherCode = v.Voucher?.VoucherCode,
                                                      DockDoor = v.DockDoor,
                                                      DockAppointmentStart = v.DockAppointmentStart,
                                                      DockAppointmentEnd = v.DockAppointmentEnd,
                                                      DriverName = v.DriverName,
                                                      VehicleNumber = v.VehicleNumber,
                                                      Notes = v.Notes
                                                  }).ToList();
        Dictionary<int, YardVisitRow> activeBySpot = activeVisits.Where((YardVisitRow v) => v.CurrentSpotId.HasValue).ToDictionary((YardVisitRow v) => v.CurrentSpotId.GetValueOrDefault(), (YardVisitRow v) => v);
        List<YardSpotRow> spots = await (from s in (from s in spotQuery
                                                    orderby s.Warehouse.WarehouseCode, s.SpotCode
                                                    select s).Take(500)
                                         select new YardSpotRow
                                         {
                                             YardSpotId = s.YardSpotId,
                                             WarehouseId = s.WarehouseId,
                                             WarehouseName = ((s.Warehouse != null) ? s.Warehouse.WarehouseName : ""),
                                             SpotCode = s.SpotCode,
                                             SpotName = s.SpotName,
                                             SpotType = s.SpotType,
                                             Status = s.Status,
                                             IsActive = s.IsActive
                                         }).ToListAsync();
        foreach (YardSpotRow spot in spots)
        {
            if (activeBySpot.TryGetValue(spot.YardSpotId, out var active))
            {
                spot.OccupiedByTrailer = active.TrailerNumber;
                spot.ActiveVisitId = active.YardVisitId;
            }
            active = null;
        }
        List<YardVoucherOption> voucherOptions = await (from v in (from v in _db.Vouchers.AsNoTracking().Include((Voucher v) => v.Warehouse)
                                                                   where !v.IsCancelled && (!((int?)warehouseId).HasValue || v.WarehouseId == ((int?)warehouseId).Value) && (v.DockAppointmentStart.HasValue || v.ExpectedArrivalAt.HasValue || v.DockDoor != null) && v.DockStatus != DockOperationStatusEnum.Completed
                                                                   orderby v.DockAppointmentStart ?? v.ExpectedArrivalAt ?? v.VoucherDate
                                                                   select v).Take(200)
                                                        select new YardVoucherOption
                                                        {
                                                            VoucherId = v.VoucherId,
                                                            VoucherCode = v.VoucherCode,
                                                            WarehouseId = v.WarehouseId,
                                                            WarehouseName = ((v.Warehouse != null) ? v.Warehouse.WarehouseName : ""),
                                                            DockDoor = v.DockDoor,
                                                            DockAppointmentStart = v.DockAppointmentStart,
                                                            DockAppointmentEnd = v.DockAppointmentEnd,
                                                            CarrierName = v.CarrierName,
                                                            VehicleNumber = v.VehicleNumber
                                                        }).ToListAsync();
        List<DockAppointment> dockAppointments = await _db.DockAppointments.AsNoTracking()
            .Include(a => a.Warehouse)
            .Include(a => a.OwnerPartner)
            .Where(a => (!((int?)warehouseId).HasValue || a.WarehouseId == ((int?)warehouseId).Value)
                && a.Status != DockAppointmentStatusEnum.Completed
                && a.Status != DockAppointmentStatusEnum.Cancelled)
            .OrderBy(a => a.PlannedStartAt)
            .Take(100)
            .ToListAsync();
        List<YardVisitEvidence> recentEvidence = await _db.YardVisitEvidence.AsNoTracking()
            .Include(e => e.YardVisit)
            .ThenInclude(v => v.Trailer)
            .Where(e => !((int?)warehouseId).HasValue || e.YardVisit.WarehouseId == ((int?)warehouseId).Value)
            .OrderByDescending(e => e.CapturedAt)
            .Take(20)
            .ToListAsync();
        return new YardManagementPageViewModel
        {
            WarehouseId = warehouseId,
            Search = search,
            Now = now,
            Warehouses = warehouses,
            ActiveVisits = activeVisits,
            Spots = spots,
            VoucherOptions = voucherOptions,
            DockAppointments = dockAppointments,
            RecentEvidence = recentEvidence,
            AvailableSpotCount = spots.Count((YardSpotRow s) => s.IsActive && s.Status == YardSpotStatusEnum.Available),
            OccupiedSpotCount = activeVisits.Count((YardVisitRow v) => v.CurrentSpotId.HasValue),
            BlockedSpotCount = spots.Count(delegate (YardSpotRow s)
            {
                YardSpotStatusEnum status = s.Status;
                return status - 3 <= YardSpotStatusEnum.Available;
            }),
            ActiveVisitCount = activeVisits.Count,
            OverdueVisitCount = activeVisits.Count(v => v.DockAppointmentEnd.HasValue && v.DockAppointmentEnd.Value < now)
        };
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> YardManagement(int? warehouseId, string? search)
    {
        return View(await BuildYardManagementAsync(warehouseId, search));
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateYardSpot(int warehouseId, string spotCode, string? spotName, YardSpotTypeEnum spotType = YardSpotTypeEnum.Standard, YardSpotStatusEnum status = YardSpotStatusEnum.Available, string? notes = null)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        try
        {
            await _yardService.CreateSpotAsync(warehouseId, spotCode, spotName, spotType, status, notes, base.User.Identity?.Name ?? "system");
            base.TempData["Success"] = "Đã tạo vị trí bãi đỗ " + spotCode?.Trim().ToUpperInvariant() + ".";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("YardManagement", new { warehouseId });
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GateInYardVisit(int warehouseId, string trailerNumber, string? containerNumber, TrailerTypeEnum trailerType, string? carrierName, string? sealNumber, string? driverName, string? driverPhone, string? vehicleNumber, YardVisitPurposeEnum purpose, long? voucherId, int? yardSpotId, string? notes, string? search = null)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        try
        {
            YardVisit visit = await _yardService.GateInAsync(new YardGateInRequest
            {
                WarehouseId = warehouseId,
                TrailerNumber = trailerNumber,
                ContainerNumber = containerNumber,
                TrailerType = trailerType,
                CarrierName = carrierName,
                SealNumber = sealNumber,
                DriverName = driverName,
                DriverPhone = driverPhone,
                VehicleNumber = vehicleNumber,
                Purpose = purpose,
                VoucherId = voucherId,
                YardSpotId = yardSpotId,
                Notes = notes
            }, scopedWh, base.User.Identity?.Name ?? "system");
            base.TempData["Success"] = "Đã gate-in " + visit.VisitCode + ".";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("YardManagement", new { warehouseId, search });
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignYardSpot(long yardVisitId, int yardSpotId, int? warehouseId = null, string? search = null)
    {
        int? scopedWh = GetScopedWarehouseId();
        try
        {
            YardVisit visit = await _yardService.AssignSpotAsync(yardVisitId, yardSpotId, scopedWh, base.User.Identity?.Name ?? "system");
            warehouseId = scopedWh ?? visit.WarehouseId;
            base.TempData["Success"] = "Đã gán vị trí cho " + visit.VisitCode + ".";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("YardManagement", new
        {
            warehouseId = (scopedWh ?? warehouseId),
            search = search
        });
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveYardSpot(long yardVisitId, int yardSpotId, int? warehouseId = null, string? search = null)
    {
        int? scopedWh = GetScopedWarehouseId();
        try
        {
            YardVisit visit = await _yardService.MoveSpotAsync(yardVisitId, yardSpotId, scopedWh, base.User.Identity?.Name ?? "system");
            warehouseId = scopedWh ?? visit.WarehouseId;
            base.TempData["Success"] = "Đã đổi vị trí cho " + visit.VisitCode + ".";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("YardManagement", new
        {
            warehouseId = (scopedWh ?? warehouseId),
            search = search
        });
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GateOutYardVisit(long yardVisitId, int? warehouseId = null, string? search = null)
    {
        int? scopedWh = GetScopedWarehouseId();
        try
        {
            YardVisit visit = await _yardService.GateOutAsync(yardVisitId, scopedWh, base.User.Identity?.Name ?? "system");
            warehouseId = scopedWh ?? visit.WarehouseId;
            base.TempData["Success"] = "Đã gate-out " + visit.VisitCode + ".";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("YardManagement", new
        {
            warehouseId = (scopedWh ?? warehouseId),
            search = search
        });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> YardBillingRates(int? warehouseId)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        List<YardBillingRate> rates = await (from r in _db.YardBillingRates.Include((YardBillingRate r) => r.Warehouse).Include((YardBillingRate r) => r.Partner)
                                             where !((int?)warehouseId).HasValue || r.WarehouseId == ((int?)warehouseId).Value
                                             orderby r.Warehouse.WarehouseCode, r.IsActive descending, (r.Partner != null ? r.Partner.PartnerName : ""), r.CarrierName
                                             select r).AsNoTracking().ToListAsync();
        List<Partner> partners = await (from p in _db.Partners
                                        where p.IsActive && (p.PartnerType == PartnerTypeEnum.Customer || p.PartnerType == PartnerTypeEnum.Both)
                                        orderby p.PartnerCode
                                        select p).AsNoTracking().ToListAsync();
        YardBillingRatePageViewModel yardBillingRatePageViewModel = new YardBillingRatePageViewModel
        {
            WarehouseId = warehouseId
        };
        YardBillingRatePageViewModel yardBillingRatePageViewModel2 = yardBillingRatePageViewModel;
        yardBillingRatePageViewModel2.Warehouses = await GetVisibleWarehousesAsync();
        yardBillingRatePageViewModel.Partners = partners;
        yardBillingRatePageViewModel.Rates = rates;
        return View(yardBillingRatePageViewModel);
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> YardBillingCharges(int? warehouseId, YardChargeStatusEnum? status, DateTime? dateFrom, DateTime? dateTo)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        IQueryable<YardBillingCharge> query = _yardBillingQueryService.BuildChargeQuery(warehouseId, status, dateFrom, dateTo);
        List<YardBillingChargeRow> rows = _yardBillingQueryService.MapChargeRows(await query.OrderByDescending((YardBillingCharge c) => c.CreatedAt).Take(500).ToListAsync());
        IQueryable<YardBillingCharge> statsQuery = from c in _db.YardBillingCharges.AsNoTracking()
                                                   where !((int?)warehouseId).HasValue || c.WarehouseId == ((int?)warehouseId).Value
                                                   select c;
        var stats = await (from c in statsQuery
                           group c by c.Status into g
                           select new
                           {
                               Key = g.Key,
                               Count = g.Count(),
                               Total = g.Sum((YardBillingCharge x) => x.Amount)
                           }).ToListAsync();
        YardBillingChargePageViewModel yardBillingChargePageViewModel = new YardBillingChargePageViewModel
        {
            WarehouseId = warehouseId,
            Status = status,
            DateFrom = dateFrom,
            DateTo = dateTo
        };
        YardBillingChargePageViewModel yardBillingChargePageViewModel2 = yardBillingChargePageViewModel;
        yardBillingChargePageViewModel2.Warehouses = await GetVisibleWarehousesAsync();
        yardBillingChargePageViewModel.Charges = rows;
        yardBillingChargePageViewModel.DraftCount = stats.FirstOrDefault(x => x.Key == YardChargeStatusEnum.Draft)?.Count ?? 0;
        yardBillingChargePageViewModel.ConfirmedCount = stats.FirstOrDefault(x => x.Key == YardChargeStatusEnum.Confirmed)?.Count ?? 0;
        yardBillingChargePageViewModel.WaivedCount = stats.FirstOrDefault(x => x.Key == YardChargeStatusEnum.Waived)?.Count ?? 0;
        yardBillingChargePageViewModel.TotalDraftAmount = stats.FirstOrDefault(x => x.Key == YardChargeStatusEnum.Draft)?.Total ?? 0m;
        yardBillingChargePageViewModel.TotalConfirmedAmount = stats.FirstOrDefault(x => x.Key == YardChargeStatusEnum.Confirmed)?.Total ?? 0m;
        return View(yardBillingChargePageViewModel);
    }

}
