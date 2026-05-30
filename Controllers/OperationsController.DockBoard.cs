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

    private static DockOperationStatusEnum ResolveEffectiveDockStatus(Voucher voucher, DateTime now)
    {
        if (voucher.DockCompletedAt.HasValue || voucher.DockStatus == DockOperationStatusEnum.Completed)
        {
            return DockOperationStatusEnum.Completed;
        }
        if (voucher.DockStatus == DockOperationStatusEnum.Delayed || (voucher.DockAppointmentEnd.HasValue && voucher.DockAppointmentEnd.Value < now))
        {
            return DockOperationStatusEnum.Delayed;
        }
        if (voucher.UnloadStartAt.HasValue && !voucher.UnloadEndAt.HasValue)
        {
            return DockOperationStatusEnum.Unloading;
        }
        if (voucher.GateInAt.HasValue || voucher.DockArrivalAt.HasValue)
        {
            return DockOperationStatusEnum.Arrived;
        }
        if (voucher.DockAppointmentStart.HasValue && voucher.DockAppointmentStart.Value < now && !voucher.GateInAt.HasValue)
        {
            return DockOperationStatusEnum.Delayed;
        }
        return DockOperationStatusEnum.Scheduled;
    }


    private static string GetDockMilestoneLabel(Voucher voucher)
    {
        if (voucher.DockCompletedAt.HasValue)
        {
            return "Hoàn tất cập cổng";
        }
        if (voucher.UnloadEndAt.HasValue)
        {
            return "Đã dỡ hàng xong";
        }
        if (voucher.UnloadStartAt.HasValue)
        {
            return "Đang dỡ hàng";
        }
        if (voucher.DockArrivalAt.HasValue)
        {
            return "Đã vào cửa cập cổng";
        }
        if (voucher.GateInAt.HasValue)
        {
            return "Đã qua cổng";
        }
        return "Đã lên lịch";
    }


    private static DockBoardRow ToDockBoardRow(Voucher voucher, DateTime now)
    {
        DockOperationStatusEnum dockOperationStatusEnum = ResolveEffectiveDockStatus(voucher, now);
        bool flag = dockOperationStatusEnum == DockOperationStatusEnum.Delayed;
        DateTime? dateTime = voucher.DockAppointmentEnd ?? voucher.DockAppointmentStart ?? voucher.ExpectedArrivalAt;
        DateTime? dateTime2 = voucher.GateInAt ?? voucher.DockArrivalAt;
        DateTime? dateTime3 = voucher.DockCompletedAt ?? voucher.UnloadEndAt;
        return new DockBoardRow
        {
            VoucherId = voucher.VoucherId,
            VoucherCode = voucher.VoucherCode,
            AsnCode = voucher.AsnCode,
            WarehouseId = voucher.WarehouseId,
            WarehouseName = (voucher.Warehouse?.WarehouseName ?? ""),
            PartnerName = voucher.Partner?.PartnerName,
            DockDoor = voucher.DockDoor,
            ExpectedArrivalAt = voucher.ExpectedArrivalAt,
            DockAppointmentStart = voucher.DockAppointmentStart,
            DockAppointmentEnd = voucher.DockAppointmentEnd,
            CarrierName = voucher.CarrierName,
            VehicleNumber = voucher.VehicleNumber,
            InboundStatus = voucher.InboundStatus,
            DockStatus = voucher.DockStatus,
            EffectiveDockStatus = dockOperationStatusEnum,
            GateInAt = voucher.GateInAt,
            DockArrivalAt = voucher.DockArrivalAt,
            UnloadStartAt = voucher.UnloadStartAt,
            UnloadEndAt = voucher.UnloadEndAt,
            DockCompletedAt = voucher.DockCompletedAt,
            IsDelayed = flag,
            DelayMinutes = ((flag && dateTime.HasValue) ? new int?(Math.Max(0, (int)(now - dateTime.Value).TotalMinutes)) : ((int?)null)),
            DwellMinutes = (dateTime2.HasValue ? new int?((int)((dateTime3 ?? now) - dateTime2.Value).TotalMinutes) : ((int?)null)),
            UnloadMinutes = (voucher.UnloadStartAt.HasValue ? new int?((int)((voucher.UnloadEndAt ?? now) - voucher.UnloadStartAt.Value).TotalMinutes) : ((int?)null)),
            CurrentMilestone = GetDockMilestoneLabel(voucher)
        };
    }


    private async Task<DockBoardPageViewModel> BuildDockBoardAsync(int? warehouseId, DateTime? boardDate)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        DateTime now = VietnamNow;
        DateTime date = (boardDate ?? now.Date).Date;
        DateTime windowStart = date.AddHours(-6.0);
        DateTime windowEnd = date.AddDays(1.0).AddHours(6.0);
        IQueryable<Voucher> query = from v in _db.Vouchers.AsNoTracking().Include((Voucher v) => v.Warehouse).Include((Voucher v) => v.Partner)
                                    where !v.IsCancelled && (v.VoucherType == VoucherTypeEnum.NhapKho || v.VoucherType == VoucherTypeEnum.KhachTra || v.VoucherType == VoucherTypeEnum.NhapThanhPham) && (v.DockDoor != null || v.DockAppointmentStart.HasValue || v.ExpectedArrivalAt.HasValue || v.GateInAt.HasValue) && (!((int?)warehouseId).HasValue || v.WarehouseId == ((int?)warehouseId).Value) && ((v.DockAppointmentStart.HasValue && v.DockAppointmentStart.Value >= windowStart && v.DockAppointmentStart.Value <= windowEnd) || (v.DockAppointmentEnd.HasValue && v.DockAppointmentEnd.Value >= windowStart && v.DockAppointmentEnd.Value <= windowEnd) || (v.ExpectedArrivalAt.HasValue && v.ExpectedArrivalAt.Value >= windowStart && v.ExpectedArrivalAt.Value <= windowEnd) || (v.GateInAt.HasValue && v.GateInAt.Value >= windowStart && v.GateInAt.Value <= windowEnd) || (v.DockCompletedAt.HasValue && v.DockCompletedAt.Value >= windowStart && v.DockCompletedAt.Value <= windowEnd) || (!v.DockCompletedAt.HasValue && v.DockStatus != DockOperationStatusEnum.Completed && v.DockAppointmentStart.HasValue && v.DockAppointmentStart.Value <= windowEnd))
                                    select v;
        List<DockBoardRow> rows = (await (from v in query
                                          orderby v.DockDoor ?? "ZZZ", v.DockAppointmentStart ?? v.ExpectedArrivalAt ?? v.VoucherDate
                                          select v).Take(300).ToListAsync()).Select((Voucher v) => ToDockBoardRow(v, now)).ToList();
        List<DockAppointment> enterpriseAppointments = await _db.DockAppointments.AsNoTracking()
            .Include(a => a.Warehouse)
            .Include(a => a.OwnerPartner)
            .Where(a => (!((int?)warehouseId).HasValue || a.WarehouseId == ((int?)warehouseId).Value)
                && a.Status != DockAppointmentStatusEnum.Cancelled
                && a.PlannedStartAt >= windowStart
                && a.PlannedStartAt <= windowEnd)
            .OrderBy(a => a.DockDoor)
            .ThenBy(a => a.PlannedStartAt)
            .Take(300)
            .ToListAsync();
        var doorKeys = (from d in await (from d in _db.DockDoorCapacities.AsNoTracking().Include((DockDoorCapacity d) => d.Warehouse)
                                         where !((int?)warehouseId).HasValue || d.WarehouseId == ((int?)warehouseId).Value
                                         orderby d.DockDoor
                                         select d).ToListAsync()
                        select new
                        {
                            DockDoor = d.DockDoor,
                            WarehouseId = d.WarehouseId,
                            WarehouseName = (d.Warehouse?.WarehouseName ?? ""),
                            DoorType = d.DoorType
                        } into d
                        group d by new
                        {
                            Door = d.DockDoor.ToUpperInvariant(),
                            WarehouseId = d.WarehouseId
                        } into g
                        select g.First()).ToList();
        foreach (DockBoardRow row in rows.Where((DockBoardRow r) => !string.IsNullOrWhiteSpace(r.DockDoor)))
        {
            if (!doorKeys.Any(d => d.WarehouseId == row.WarehouseId && string.Equals(d.DockDoor, row.DockDoor, StringComparison.OrdinalIgnoreCase)))
            {
                doorKeys.Add(new
                {
                    DockDoor = row.DockDoor,
                    WarehouseId = row.WarehouseId,
                    WarehouseName = row.WarehouseName,
                    DoorType = DockDoorTypeEnum.Both
                });
            }
        }
        foreach (DockAppointment appointment in enterpriseAppointments)
        {
            if (!doorKeys.Any(d => d.WarehouseId == appointment.WarehouseId && string.Equals(d.DockDoor, appointment.DockDoor, StringComparison.OrdinalIgnoreCase)))
            {
                doorKeys.Add(new
                {
                    DockDoor = appointment.DockDoor,
                    WarehouseId = appointment.WarehouseId,
                    WarehouseName = appointment.Warehouse?.WarehouseName ?? "",
                    DoorType = appointment.Direction == DockAppointmentDirectionEnum.Outbound ? DockDoorTypeEnum.Shipping : DockDoorTypeEnum.Receiving
                });
            }
        }
        List<DockDoorBoardRow> doors = doorKeys.OrderBy(d => d.DockDoor).Select(d =>
        {
            List<DockBoardRow> list = (from r in rows
                                       where r.WarehouseId == d.WarehouseId && string.Equals(r.DockDoor, d.DockDoor, StringComparison.OrdinalIgnoreCase)
                                       orderby r.DockAppointmentStart ?? r.ExpectedArrivalAt ?? DateTime.MaxValue
                                       select r).ToList();
            return new DockDoorBoardRow
            {
                DockDoor = d.DockDoor,
                WarehouseId = d.WarehouseId,
                WarehouseName = d.WarehouseName,
                DoorType = d.DoorType,
                ActiveCount = list.Count((DockBoardRow r) => r.EffectiveDockStatus != DockOperationStatusEnum.Completed),
                DelayedCount = list.Count((DockBoardRow r) => r.IsDelayed),
                CompletedTodayCount = list.Count((DockBoardRow r) => r.EffectiveDockStatus == DockOperationStatusEnum.Completed),
                ActiveAppointments = list
            };
        }).ToList();
        DockBoardPageViewModel dockBoardPageViewModel = new DockBoardPageViewModel
        {
            WarehouseId = warehouseId,
            BoardDate = date,
            Now = now
        };
        DockBoardPageViewModel dockBoardPageViewModel2 = dockBoardPageViewModel;
        dockBoardPageViewModel2.Warehouses = await (from w in _db.Warehouses.AsNoTracking()
                                                    where w.IsActive
                                                    orderby w.WarehouseCode
                                                    select w).ToListAsync();
        dockBoardPageViewModel.Doors = doors;
        dockBoardPageViewModel.Rows = rows;
        dockBoardPageViewModel.EnterpriseAppointments = enterpriseAppointments;
        dockBoardPageViewModel.ScheduledCount = rows.Count((DockBoardRow r) => r.EffectiveDockStatus == DockOperationStatusEnum.Scheduled) + enterpriseAppointments.Count(a => a.Status == DockAppointmentStatusEnum.Scheduled);
        dockBoardPageViewModel.ArrivedCount = rows.Count((DockBoardRow r) => r.EffectiveDockStatus == DockOperationStatusEnum.Arrived) + enterpriseAppointments.Count(a => a.Status == DockAppointmentStatusEnum.CheckedIn || a.Status == DockAppointmentStatusEnum.AtDock);
        dockBoardPageViewModel.UnloadingCount = rows.Count((DockBoardRow r) => r.EffectiveDockStatus == DockOperationStatusEnum.Unloading);
        dockBoardPageViewModel.CompletedCount = rows.Count((DockBoardRow r) => r.EffectiveDockStatus == DockOperationStatusEnum.Completed) + enterpriseAppointments.Count(a => a.Status == DockAppointmentStatusEnum.Completed);
        dockBoardPageViewModel.DelayedCount = rows.Count((DockBoardRow r) => r.EffectiveDockStatus == DockOperationStatusEnum.Delayed) + enterpriseAppointments.Count(a => a.Status == DockAppointmentStatusEnum.Scheduled && a.PlannedEndAt < now);
        return dockBoardPageViewModel;
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> DockBoard(int? warehouseId, DateTime? date)
    {
        return View(await BuildDockBoardAsync(warehouseId, date));
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> DockBoardData(int? warehouseId, DateTime? date)
    {
        DockBoardPageViewModel model = await BuildDockBoardAsync(warehouseId, date);
        return Json(new
        {
            now = model.Now,
            counts = new
            {
                scheduled = model.ScheduledCount,
                arrived = model.ArrivedCount,
                unloading = model.UnloadingCount,
                completed = model.CompletedCount,
                delayed = model.DelayedCount
            },
            enterpriseAppointments = model.EnterpriseAppointments.Select(a => new
            {
                a.DockAppointmentId,
                a.AppointmentCode,
                a.DockDoor,
                a.WarehouseId,
                WarehouseName = a.Warehouse?.WarehouseName,
                OwnerName = a.OwnerPartner?.PartnerName,
                a.Direction,
                a.Status,
                a.PlannedStartAt,
                a.PlannedEndAt,
                a.OverloadWarning
            }),
            doors = model.Doors.Select((DockDoorBoardRow d) => new
            {
                DockDoor = d.DockDoor,
                WarehouseId = d.WarehouseId,
                WarehouseName = d.WarehouseName,
                doorType = d.DoorType.ToString(),
                ActiveCount = d.ActiveCount,
                DelayedCount = d.DelayedCount,
                CompletedTodayCount = d.CompletedTodayCount,
                appointments = d.ActiveAppointments.Select((DockBoardRow r) => new
                {
                    VoucherId = r.VoucherId,
                    VoucherCode = r.VoucherCode,
                    AsnCode = r.AsnCode,
                    DockDoor = r.DockDoor,
                    WarehouseName = r.WarehouseName,
                    PartnerName = r.PartnerName,
                    ExpectedArrivalAt = r.ExpectedArrivalAt,
                    DockAppointmentStart = r.DockAppointmentStart,
                    DockAppointmentEnd = r.DockAppointmentEnd,
                    CarrierName = r.CarrierName,
                    VehicleNumber = r.VehicleNumber,
                    dockStatus = r.EffectiveDockStatus.ToString(),
                    IsDelayed = r.IsDelayed,
                    DelayMinutes = r.DelayMinutes,
                    DwellMinutes = r.DwellMinutes,
                    UnloadMinutes = r.UnloadMinutes,
                    CurrentMilestone = r.CurrentMilestone,
                    GateInAt = r.GateInAt,
                    DockArrivalAt = r.DockArrivalAt,
                    UnloadStartAt = r.UnloadStartAt,
                    UnloadEndAt = r.UnloadEndAt,
                    DockCompletedAt = r.DockCompletedAt
                })
            })
        });
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDockMilestone(long voucherId, string milestone, int? warehouseId = null, DateTime? date = null)
    {
        int? scopedWh = GetScopedWarehouseId();
        Voucher? voucher = await _db.Vouchers.FirstOrDefaultAsync((Voucher v) => v.VoucherId == voucherId && !v.IsCancelled && (v.VoucherType == VoucherTypeEnum.NhapKho || v.VoucherType == VoucherTypeEnum.KhachTra || v.VoucherType == VoucherTypeEnum.NhapThanhPham));
        if (voucher == null || (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value))
        {
            base.TempData["Error"] = "Không tìm thấy lịch dock hoặc bạn không có quyền thao tác.";
            return RedirectToAction("DockBoard", new { warehouseId, date });
        }
        DateTime now = VietnamNow;
        string normalized = (milestone ?? string.Empty).Trim().ToLowerInvariant();
        string actor = base.User.Identity?.Name ?? "system";
        switch (normalized)
        {
            case "gate-in":
            case "gatein":
                {
                    Voucher voucher2 = voucher;
                    DateTime? gateInAt = voucher2.GateInAt;
                    gateInAt.GetValueOrDefault();
                    if (!gateInAt.HasValue)
                    {
                        voucher2.GateInAt = now;
                    }
                    voucher.DockStatus = DockOperationStatusEnum.Arrived;
                    break;
                }
            case "dock-arrival":
            case "dockarrival":
            case "arrived":
                {
                    Voucher voucher2 = voucher;
                    DateTime? gateInAt = voucher2.GateInAt;
                    gateInAt.GetValueOrDefault();
                    if (!gateInAt.HasValue)
                    {
                        voucher2.GateInAt = now;
                    }
                    voucher2 = voucher;
                    gateInAt = voucher2.DockArrivalAt;
                    gateInAt.GetValueOrDefault();
                    if (!gateInAt.HasValue)
                    {
                        voucher2.DockArrivalAt = now;
                    }
                    voucher.DockStatus = DockOperationStatusEnum.Arrived;
                    break;
                }
            case "unload-start":
            case "unloadstart":
            case "unloading":
                {
                    Voucher voucher2 = voucher;
                    DateTime? gateInAt = voucher2.GateInAt;
                    gateInAt.GetValueOrDefault();
                    if (!gateInAt.HasValue)
                    {
                        voucher2.GateInAt = now;
                    }
                    voucher2 = voucher;
                    gateInAt = voucher2.DockArrivalAt;
                    gateInAt.GetValueOrDefault();
                    if (!gateInAt.HasValue)
                    {
                        voucher2.DockArrivalAt = now;
                    }
                    voucher2 = voucher;
                    gateInAt = voucher2.UnloadStartAt;
                    gateInAt.GetValueOrDefault();
                    if (!gateInAt.HasValue)
                    {
                        voucher2.UnloadStartAt = now;
                    }
                    voucher.DockStatus = DockOperationStatusEnum.Unloading;
                    if (voucher.InboundStatus == InboundStatusEnum.Approved)
                    {
                        voucher.InboundStatus = InboundStatusEnum.Receiving;
                        voucher2 = voucher;
                        if (voucher2.ReceivedBy == null)
                        {
                            voucher2.ReceivedBy = actor;
                        }
                        voucher2 = voucher;
                        gateInAt = voucher2.ReceivedAt;
                        gateInAt.GetValueOrDefault();
                        if (!gateInAt.HasValue)
                        {
                            voucher2.ReceivedAt = now;
                        }
                    }
                    break;
                }
            case "unload-end":
            case "unloadend":
                {
                    Voucher voucher2 = voucher;
                    DateTime? gateInAt = voucher2.GateInAt;
                    gateInAt.GetValueOrDefault();
                    if (!gateInAt.HasValue)
                    {
                        voucher2.GateInAt = now;
                    }
                    voucher2 = voucher;
                    gateInAt = voucher2.DockArrivalAt;
                    gateInAt.GetValueOrDefault();
                    if (!gateInAt.HasValue)
                    {
                        voucher2.DockArrivalAt = now;
                    }
                    voucher2 = voucher;
                    gateInAt = voucher2.UnloadStartAt;
                    gateInAt.GetValueOrDefault();
                    if (!gateInAt.HasValue)
                    {
                        voucher2.UnloadStartAt = now;
                    }
                    voucher2 = voucher;
                    gateInAt = voucher2.UnloadEndAt;
                    gateInAt.GetValueOrDefault();
                    if (!gateInAt.HasValue)
                    {
                        voucher2.UnloadEndAt = now;
                    }
                    voucher.DockStatus = DockOperationStatusEnum.Unloading;
                    break;
                }
            case "complete":
            case "completed":
                {
                    Voucher voucher2 = voucher;
                    DateTime? gateInAt = voucher2.GateInAt;
                    gateInAt.GetValueOrDefault();
                    if (!gateInAt.HasValue)
                    {
                        voucher2.GateInAt = now;
                    }
                    voucher2 = voucher;
                    gateInAt = voucher2.DockArrivalAt;
                    gateInAt.GetValueOrDefault();
                    if (!gateInAt.HasValue)
                    {
                        voucher2.DockArrivalAt = now;
                    }
                    voucher2 = voucher;
                    gateInAt = voucher2.UnloadStartAt;
                    gateInAt.GetValueOrDefault();
                    if (!gateInAt.HasValue)
                    {
                        voucher2.UnloadStartAt = now;
                    }
                    voucher2 = voucher;
                    gateInAt = voucher2.UnloadEndAt;
                    gateInAt.GetValueOrDefault();
                    if (!gateInAt.HasValue)
                    {
                        voucher2.UnloadEndAt = now;
                    }
                    voucher2 = voucher;
                    gateInAt = voucher2.DockCompletedAt;
                    gateInAt.GetValueOrDefault();
                    if (!gateInAt.HasValue)
                    {
                        voucher2.DockCompletedAt = now;
                    }
                    voucher.DockStatus = DockOperationStatusEnum.Completed;
                    break;
                }
            case "delayed":
                if (voucher.DockStatus != DockOperationStatusEnum.Completed)
                {
                    voucher.DockStatus = DockOperationStatusEnum.Delayed;
                }
                break;
            default:
                base.TempData["Error"] = "Mốc thời gian dock không hợp lệ.";
                return RedirectToAction("DockBoard", new { warehouseId, date });
        }
        voucher.UpdatedAt = now;
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = "Đã cập nhật mốc dock cho phiếu " + voucher.VoucherCode + ".";
        return RedirectToAction("DockBoard", new
        {
            warehouseId = (scopedWh ?? warehouseId),
            date = date
        });
    }

}
