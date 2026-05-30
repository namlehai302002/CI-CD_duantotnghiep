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

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> LaborProductivity(int? warehouseId, int days = 7)
    {
        return View(await BuildLaborProductivityModelAsync(warehouseId, days));
    }


    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CrossDockOpportunities(int? warehouseId)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        DateTime today = VietnamNow.Date;
        var inboundItems = await (from d in _db.VoucherDetails.AsNoTracking().Include((VoucherDetail d) => d.Voucher).Include((VoucherDetail d) => d.Item)
                                  where d.Voucher != null && d.Voucher.VoucherType == VoucherTypeEnum.NhapKho && !d.Voucher.IsPosted && !d.Voucher.IsCancelled && (d.Voucher.InboundStatus == InboundStatusEnum.Approved || d.Voucher.InboundStatus == InboundStatusEnum.Receiving) && ((d.Voucher.ReceivedAt.HasValue && d.Voucher.ReceivedAt.Value.Date == today) || (d.Voucher.ExpectedArrivalAt.HasValue && d.Voucher.ExpectedArrivalAt.Value.Date == today) || d.Voucher.VoucherDate == today) && d.Item != null && d.Item.IsActive && (!((int?)warehouseId).HasValue || d.Voucher.WarehouseId == ((int?)warehouseId).Value)
                                  select new
                                  {
                                      VoucherDetailId = d.VoucherDetailId,
                                      ItemId = d.ItemId,
                                      ItemCode = d.Item.ItemCode,
                                      ItemName = d.Item.ItemName,
                                      InboundQty = d.BaseQty - ((d.DefectBaseQty > 0m) ? d.DefectBaseQty : (d.DefectQty * ((d.ConversionRate == 0m) ? 1m : Math.Abs(d.ConversionRate)))),
                                      LotNumber = d.LotNumber,
                                      ExpiryDate = d.ExpiryDate,
                                      InboundVoucherCode = d.Voucher.VoucherCode,
                                      InboundVoucherId = d.VoucherId,
                                      WarehouseId = d.Voucher.WarehouseId
                                  }).ToListAsync();
        List<long> inboundDetailIds = inboundItems.Select(i => i.VoucherDetailId).ToList();
        Dictionary<long, decimal> dictionary = ((inboundDetailIds.Count != 0) ? (await (from t in _db.CrossDockTasks.AsNoTracking()
                                                                                        where t.InboundVoucherDetailId.HasValue && inboundDetailIds.Contains(t.InboundVoucherDetailId.Value) && t.Status != CrossDockTaskStatusEnum.Cancelled
                                                                                        group t by t.InboundVoucherDetailId.GetValueOrDefault() into g
                                                                                        select new
                                                                                        {
                                                                                            DetailId = g.Key,
                                                                                            Qty = g.Sum((CrossDockTask t) => t.ScheduledQty)
                                                                                        }).ToDictionaryAsync(x => x.DetailId, x => x.Qty)) : new Dictionary<long, decimal>());
        Dictionary<long, decimal> matchedInboundQty = dictionary;
        var outboundDemand = await (from d in _db.VoucherDetails.AsNoTracking().Include((VoucherDetail d) => d.Voucher)
                                    where d.Voucher != null && d.Voucher.VoucherType == VoucherTypeEnum.XuatKho && !d.Voucher.IsPosted && !d.Voucher.IsCancelled && (int)d.Voucher.FulfillmentStatus < 2 && (!((int?)warehouseId).HasValue || d.Voucher.WarehouseId == ((int?)warehouseId).Value)
                                    select new
                                    {
                                        VoucherDetailId = d.VoucherDetailId,
                                        ItemId = d.ItemId,
                                        OutboundQty = Math.Abs(d.BaseQty),
                                        LotNumber = d.LotNumber,
                                        ExpiryDate = d.ExpiryDate,
                                        OutboundVoucherCode = d.Voucher.VoucherCode,
                                        OutboundVoucherId = d.VoucherId,
                                        ServiceLevel = d.Voucher.ServiceLevel,
                                        Priority = d.Voucher.Priority,
                                        RequestedDeliveryDate = d.Voucher.RequestedDeliveryDate
                                    }).ToListAsync();
        List<long> outboundDetailIds = outboundDemand.Select(o => o.VoucherDetailId).ToList();
        Dictionary<long, decimal> dictionary2 = ((outboundDetailIds.Count != 0) ? (await (from r in _db.StockReservations.AsNoTracking()
                                                                                          where r.VoucherDetailId.HasValue && outboundDetailIds.Contains(r.VoucherDetailId.Value) && r.Status != ReservationStatusEnum.Released
                                                                                          group r by r.VoucherDetailId.GetValueOrDefault() into g
                                                                                          select new
                                                                                          {
                                                                                              DetailId = g.Key,
                                                                                              Qty = g.Sum((StockReservation r) => r.ReservedQty - r.ReleasedQty)
                                                                                          }).ToDictionaryAsync(x => x.DetailId, x => x.Qty)) : new Dictionary<long, decimal>());
        Dictionary<long, decimal> reservedOutboundQty = dictionary2;
        List<Location> stageLocations = await (from l in _db.Locations.AsNoTracking().Include((Location l) => l.Zone)
                                               where l.IsActive && l.Zone != null && (!((int?)warehouseId).HasValue || l.Zone.WarehouseId == ((int?)warehouseId).Value) && (l.Zone.ZoneType == ZoneTypeEnum.CrossDock || l.Zone.ZoneType == ZoneTypeEnum.Staging || l.Zone.ZoneType == ZoneTypeEnum.Shipping)
                                               orderby (l.Zone.ZoneType == ZoneTypeEnum.CrossDock) ? 0 : ((l.Zone.ZoneType == ZoneTypeEnum.Staging) ? 1 : 2), l.LocationCode
                                               select l).ToListAsync();
        Dictionary<int, Location> stageByWarehouse = (from l in stageLocations
                                                      where l.Zone != null
                                                      group l by l.Zone.WarehouseId).ToDictionary((IGrouping<int, Location> g) => g.Key, (IGrouping<int, Location> g) => g.First());
        List<object> opportunities = new List<object>();
        foreach (var inb in from x in inboundItems
                            orderby (!x.ExpiryDate.HasValue) ? 1 : 0, x.ExpiryDate
                            select x)
        {
            decimal matchedQty;
            decimal remainingInboundQty = Math.Max(0m, inb.InboundQty - (matchedInboundQty.TryGetValue(inb.VoucherDetailId, out matchedQty) ? matchedQty : 0m));
            if (remainingInboundQty <= 0m || !stageByWarehouse.TryGetValue(inb.WarehouseId, out var stageLocation))
            {
                continue;
            }
            var demands = (from o in outboundDemand
                           where o.ItemId == inb.ItemId && (string.IsNullOrWhiteSpace(o.LotNumber) || string.Equals(o.LotNumber, inb.LotNumber, StringComparison.OrdinalIgnoreCase)) && (!o.ExpiryDate.HasValue || o.ExpiryDate == inb.ExpiryDate)
                           orderby (o.ServiceLevel == ServiceLevelEnum.SameDay) ? 100 : 0 descending, (o.ServiceLevel == ServiceLevelEnum.Express) ? 90 : 0 descending, o.Priority descending, o.RequestedDeliveryDate ?? today.AddDays(30.0)
                           select o).ToList();
            foreach (var dem in demands)
            {
                decimal reservedQty;
                decimal openDemand = Math.Max(0m, dem.OutboundQty - (reservedOutboundQty.TryGetValue(dem.VoucherDetailId, out reservedQty) ? reservedQty : 0m));
                decimal crossDockQty = Math.Min(remainingInboundQty, openDemand);
                if (!(crossDockQty <= 0m))
                {
                    opportunities.Add(new
                    {
                        ItemId = inb.ItemId,
                        ItemCode = inb.ItemCode,
                        ItemName = inb.ItemName,
                        InboundVoucherCode = inb.InboundVoucherCode,
                        InboundVoucherId = inb.InboundVoucherId,
                        InboundVoucherDetailId = inb.VoucherDetailId,
                        InboundQty = remainingInboundQty,
                        LotNumber = inb.LotNumber,
                        ExpiryDate = inb.ExpiryDate,
                        OutboundVoucherCode = dem.OutboundVoucherCode,
                        OutboundVoucherId = dem.OutboundVoucherId,
                        OutboundVoucherDetailId = dem.VoucherDetailId,
                        OutboundQty = openDemand,
                        CrossDockQty = crossDockQty,
                        StageLocationId = stageLocation.LocationId,
                        StageLocationCode = stageLocation.LocationCode,
                        WarehouseId = inb.WarehouseId
                    });
                    remainingInboundQty -= crossDockQty;
                    if (remainingInboundQty <= 0m)
                    {
                        break;
                    }
                }
            }
            stageLocation = null;
        }
        base.ViewBag.Warehouses = (await (from w in _db.Warehouses.AsNoTracking()
                                          where w.IsActive
                                          orderby w.WarehouseCode
                                          select w).ToListAsync());
        base.ViewBag.CrossDockTasks = (await (from t in _db.CrossDockTasks.AsNoTracking().Include((CrossDockTask t) => t.InboundVoucher).Include((CrossDockTask t) => t.OutboundVoucher)
                .Include((CrossDockTask t) => t.Item)
                .Include((CrossDockTask t) => t.StageLocation)
                                              where (!((int?)warehouseId).HasValue || (t.InboundVoucher != null && t.InboundVoucher.WarehouseId == ((int?)warehouseId).Value)) && t.Status != CrossDockTaskStatusEnum.Cancelled
                                              orderby t.Status == CrossDockTaskStatusEnum.Pending descending, t.CreatedAt descending
                                              select t).Take(100).ToListAsync());
        base.ViewBag.WarehouseId = warehouseId;
        base.ViewBag.StageLocations = stageLocations;
        base.ViewBag.Opportunities = opportunities;
        return View();
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteCrossDock(long inboundVoucherId, long outboundVoucherId, int itemId, decimal qty, int stageLocationId, long? inboundVoucherDetailId, long? outboundVoucherDetailId)
    {
        try
        {
            WorkflowResult result = await _crossDockService.ExecuteCrossDockAsync(inboundVoucherId, outboundVoucherId, itemId, qty, stageLocationId, GetScopedWarehouseId(), base.User.Identity?.Name ?? "system", base.HttpContext.Connection.RemoteIpAddress?.ToString(), inboundVoucherDetailId, outboundVoucherDetailId);
            if (result.Forbidden)
            {
                return Forbid();
            }
            if (result.Succeeded)
            {
                base.TempData["Success"] = result.Message;
            }
            else
            {
                base.TempData["Error"] = result.Message;
            }
            return RedirectToAction(result.RedirectAction ?? "CrossDockOpportunities", result.RedirectRouteValues);
        }
        catch (Exception ex)
        {
            base.TempData["Error"] = UserSafeError.WithPrefix(ex, "Tạo nhiệm vụ cross-dock thất bại", "Không thể tạo nhiệm vụ cross-dock lúc này. Vui lòng thử lại.");
            return RedirectToAction("CrossDockOpportunities");
        }
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteCrossDockTask(long id)
    {
        WorkflowResult result = await _crossDockService.CompleteCrossDockTaskAsync(id);
        if (result.Succeeded)
        {
            base.TempData["Success"] = result.Message;
        }
        else
        {
            base.TempData["Error"] = result.Message;
        }
        return RedirectToAction("CrossDockOpportunities");
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCycleCountProgram(string programName, int frequencyA, int frequencyB, int frequencyC, bool isBlindCount, decimal varianceThresholdPct)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (!scopedWh.HasValue)
        {
            base.TempData["Error"] = "Cần chỉ định kho.";
            return RedirectToAction("StockCount", "Reports");
        }
        CycleCountProgram program = new CycleCountProgram
        {
            ProgramName = programName,
            WarehouseId = scopedWh.Value,
            FrequencyA = ((frequencyA > 0) ? frequencyA : 30),
            FrequencyB = ((frequencyB > 0) ? frequencyB : 90),
            FrequencyC = ((frequencyC > 0) ? frequencyC : 180),
            IsBlindCount = isBlindCount,
            VarianceThresholdPct = varianceThresholdPct,
            IsActive = true,
            CreatedBy = (base.User.Identity?.Name ?? "system"),
            CreatedAt = VietnamNow
        };
        _db.CycleCountPrograms.Add(program);
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = $"Đã tạo chương trình [{programName}] (A={frequencyA}d, B={frequencyB}d, C={frequencyC}d).";
        return RedirectToAction("StockCount", "Reports");
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunCycleCountProgram(int programId)
    {
        CycleCountProgram? program = await _db.CycleCountPrograms.FirstOrDefaultAsync((CycleCountProgram p) => p.ProgramId == programId);
        if (program == null || !program.IsActive)
        {
            base.TempData["Error"] = "Chương trình không tồn tại.";
            return RedirectToAction("StockCount", "Reports");
        }
        DateTime now = VietnamNow.Date;
        Func<char, int> schedFreq = delegate (char abc)
        {
            if (1 == 0)
            {
            }
            int result = abc switch
            {
                'A' => program.FrequencyA,
                'B' => program.FrequencyB,
                _ => program.FrequencyC,
            };
            if (1 == 0)
            {
            }
            return result;
        };
        List<CycleCountSchedule> due = await (from cycleCountSchedule in _db.CycleCountSchedules.Include((CycleCountSchedule cycleCountSchedule) => cycleCountSchedule.Location).Include((CycleCountSchedule cycleCountSchedule) => cycleCountSchedule.Item)
                                               where cycleCountSchedule.ProgramId == programId && cycleCountSchedule.IsActive && cycleCountSchedule.NextScheduledAt <= now
                                               orderby cycleCountSchedule.NextScheduledAt, cycleCountSchedule.AbcClass, cycleCountSchedule.Location != null ? cycleCountSchedule.Location.LocationCode : "", cycleCountSchedule.Item != null ? cycleCountSchedule.Item.ItemCode : ""
                                               select cycleCountSchedule).Take(50).ToListAsync();
        if (due.Count == 0)
        {
            base.TempData["Info"] = "Không có vị trí nào đến hạn.";
            return RedirectToAction("StockCount", "Reports");
        }
        object arg = now;
        string sheetCode = $"CC-{arg:yyyyMMdd}-{await _db.StockCountSheets.CountAsync() + 1:D4}";
        StockCountSheet sheet = new StockCountSheet
        {
            SheetCode = sheetCode,
            WarehouseId = program.WarehouseId,
            CountDate = now,
            Status = StockCountStatusEnum.Draft,
            CreatedBy = (base.User.Identity?.Name ?? "system"),
            CreatedAt = VietnamNow,
            Notes = "Auto by Cycle Program: " + program.ProgramName
        };
        _db.StockCountSheets.Add(sheet);
        await _unitOfWork.SaveChangesAsync();
        foreach (CycleCountSchedule s in due)
        {
            decimal curQty = (await _db.ItemLocations.Where((ItemLocation il) => il.ItemId == s.ItemId && il.LocationId == s.LocationId).SumAsync((Expression<Func<ItemLocation, decimal?>>)((ItemLocation il) => il.Quantity), default(CancellationToken))).GetValueOrDefault();
            _db.StockCountLines.Add(new StockCountLine
            {
                StockCountSheetId = sheet.StockCountSheetId,
                ItemId = s.ItemId,
                LocationId = s.LocationId,
                SystemQty = curQty,
                CountedQty = null,
                Variance = null,
                Status = 1,
                CountedBy = null,
                CountedAt = null
            });
            s.LastCountedAt = now;
            s.NextScheduledAt = now.AddDays(schedFreq(s.AbcClass));
            s.CountAttempt++;
        }
        program.LastRunAt = now;
        program.NextRunAt = now.AddDays(Math.Min(program.FrequencyA, program.FrequencyB));
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = $"Đã tạo phiếu [{sheetCode}] với {due.Count} vị trí. Blind={program.IsBlindCount}";
        return RedirectToAction("StockCount", "Reports");
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRecallCase(long[] affectedDetailIds, string reason, int severity, int? supplierId)
    {
        if (affectedDetailIds == null || affectedDetailIds.Length == 0)
        {
            base.TempData["Error"] = "Vui lòng chọn ít nhất 1 dòng.";
            return RedirectToAction("QualityInspection");
        }
        object arg = VietnamNow;
        string caseNumber = $"RCL-{arg:yyyyMMdd}-{await _db.RecallCases.CountAsync() + 1:D5}";
        string actor = base.User.Identity?.Name ?? "system";
        if (1 == 0)
        {
        }
        RecallSeverityEnum recallSeverityEnum = severity switch
        {
            1 => RecallSeverityEnum.Low,
            2 => RecallSeverityEnum.Medium,
            3 => RecallSeverityEnum.High,
            4 => RecallSeverityEnum.Critical,
            _ => RecallSeverityEnum.Medium,
        };
        if (1 == 0)
        {
        }
        RecallSeverityEnum sev = recallSeverityEnum;
        RecallCase recallCase = new RecallCase
        {
            CaseNumber = caseNumber,
            Reason = reason,
            Severity = sev,
            SupplierId = supplierId,
            Status = RecallStatusEnum.Issued,
            IssuedBy = actor,
            IssuedAt = VietnamNow
        };
        _db.RecallCases.Add(recallCase);
        await _unitOfWork.SaveChangesAsync();
        List<VoucherDetail> details = await (from d in _db.VoucherDetails.Include((VoucherDetail d) => d.Item)
                                             where Enumerable.Contains(affectedDetailIds, d.VoucherDetailId)
                                             select d).ToListAsync();
        foreach (VoucherDetail detail in details)
        {
            RecallLine line = new RecallLine
            {
                RecallCaseId = recallCase.RecallCaseId,
                ItemId = detail.ItemId,
                OwnerPartnerId = detail.OwnerPartnerId,
                LotNumber = detail.LotNumber,
                AffectedQty = Math.Abs(detail.BaseQty),
                Disposition = RecallDispositionEnum.Quarantine,
                LineStatus = RecallLineStatusEnum.InProgress,
                CreatedAt = VietnamNow
            };
            _db.RecallLines.Add(line);
            foreach (ItemLocation il in await _db.ItemLocations.Where((ItemLocation itemLocation) => itemLocation.ItemId == detail.ItemId && itemLocation.OwnerPartnerId == detail.OwnerPartnerId && (int?)itemLocation.LocationId == detail.LocationId && (detail.LotNumber == null || itemLocation.LotNumber == detail.LotNumber)).ToListAsync())
            {
                il.HoldStatus = InventoryHoldStatusEnum.Quarantine;
                il.UpdatedAt = VietnamNow;
                _db.AuditLogs.Add(new AuditLog
                {
                    TableName = "ItemLocation",
                    RecordId = $"{il.ItemLocationId}",
                    ActionType = "QUARANTINE_BY_RECALL",
                    ColumnChanged = "HoldStatus",
                    OldValue = "Available",
                    NewValue = "Recall:" + caseNumber,
                    ChangedBy = actor,
                    ChangedAt = VietnamNow,
                    IpAddress = base.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    AppModule = "Recall"
                });
            }
        }
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = $"Đã tạo recall [{caseNumber}] với {details.Count} dòng. Tất cả tồn kho đã bị quarantine.";
        return RedirectToAction("QualityInspection");
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveRecallCase(long id, string resolution, int disposition)
    {
        RecallCase? rc = await _db.RecallCases.Include((RecallCase c) => c.Lines).FirstOrDefaultAsync((RecallCase c) => c.RecallCaseId == id);
        if (rc == null)
        {
            base.TempData["Error"] = "Recall không tồn tại.";
            return RedirectToAction("QualityInspection");
        }
        if (rc.Status == RecallStatusEnum.Resolved)
        {
            base.TempData["Error"] = "Đã được giải quyết.";
            return RedirectToAction("QualityInspection");
        }
        string actor = base.User.Identity?.Name ?? "system";
        RecallDispositionEnum disp = (RecallDispositionEnum)disposition;
        rc.Status = RecallStatusEnum.Resolved;
        rc.Resolution = resolution;
        rc.ResolvedBy = actor;
        rc.ResolvedAt = VietnamNow;
        foreach (RecallLine line in rc.Lines)
        {
            line.LineStatus = RecallLineStatusEnum.Dispositioned;
            line.Disposition = disp;
            line.CompletedAt = VietnamNow;
            if (disp - 4 > RecallDispositionEnum.Quarantine)
            {
                continue;
            }
            foreach (ItemLocation il in await _db.ItemLocations.Where((ItemLocation itemLocation) => itemLocation.ItemId == line.ItemId && itemLocation.OwnerPartnerId == line.OwnerPartnerId && (line.LotNumber == null || itemLocation.LotNumber == line.LotNumber)).ToListAsync())
            {
                il.HoldStatus = InventoryHoldStatusEnum.Available;
                il.UpdatedAt = VietnamNow;
            }
        }
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = $"Recall [{rc.CaseNumber}] đã giải quyết. Disposition: {disp}.";
        return RedirectToAction("QualityInspection");
    }


    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CapacitySimulation(int? warehouseId)
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
        if (!warehouseId.HasValue)
        {
            return View(new List<CapacityScenario>());
        }
        List<CapacityScenario> scenarios = await (from s in _db.CapacityScenarios
                                                  where (int?)s.WarehouseId == warehouseId && s.IsActive
                                                  orderby s.CreatedAt descending
                                                  select s).Take(20).ToListAsync();
        await _db.Warehouses.FindAsync(warehouseId.Value);
        int recentVouchers = await _db.Vouchers.Where((Voucher v) => (int?)v.WarehouseId == warehouseId && v.CreatedAt >= VietnamNow.AddDays(-30.0)).CountAsync();
        int dockCount = await _db.Locations.Where((Location l) => (int?)l.Zone.WarehouseId == warehouseId && l.Zone.ZoneType == ZoneTypeEnum.Shipping && l.IsActive).CountAsync();
        int laborCount = await _db.AppUsers.Where((AppUser u) => u.IsActive && u.WarehouseId == warehouseId).CountAsync();
        base.ViewBag.BaselineVolume = recentVouchers;
        base.ViewBag.DockCount = dockCount;
        base.ViewBag.LaborCount = laborCount;
        return View(scenarios);
    }

}
