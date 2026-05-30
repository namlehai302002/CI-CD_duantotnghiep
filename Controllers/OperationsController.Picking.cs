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
    public async Task<IActionResult> Waves()
    {
        int? scopedWh = GetScopedWarehouseId();
        List<WaveBoardRow> rows = (await (from w in _db.Waves.Include((Wave w) => w.Warehouse).Include((Wave w) => w.PickTasks)
                                          where !((int?)scopedWh).HasValue || w.WarehouseId == ((int?)scopedWh).Value
                                          orderby w.CreatedAt descending
                                          select w).Take(100).ToListAsync()).Select((Wave w) => new WaveBoardRow
                                          {
                                              WaveId = w.WaveId,
                                              WaveCode = w.WaveCode,
                                              WarehouseId = w.WarehouseId,
                                              WarehouseName = (w.Warehouse?.WarehouseName ?? ""),
                                              Status = w.Status,
                                              OpenTasks = w.PickTasks.Count((PickTask t) => t.Status == PickTaskStatusEnum.Pending || t.Status == PickTaskStatusEnum.Assigned || t.Status == PickTaskStatusEnum.InProgress),
                                              DoneTasks = w.PickTasks.Count((PickTask t) => t.Status == PickTaskStatusEnum.Completed || t.Status == PickTaskStatusEnum.Short),
                                              CreatedAt = w.CreatedAt,
                                              CompletedAt = w.CompletedAt
                                          }).ToList();
        base.ViewBag.Warehouses = (await (from w in _db.Warehouses.AsNoTracking()
                                          where w.IsActive
                                          orderby w.WarehouseCode
                                          select w).ToListAsync());
        base.ViewBag.WarehouseId = scopedWh;
        return View(rows);
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> PickTasks(int? warehouseId, long? waveId, byte? status)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        List<int>? scopedZoneIds = await GetScopedZoneIdsAsync();
        base.ViewBag.Waves = (await (from w in _db.Waves.AsNoTracking()
                                     where !((int?)warehouseId).HasValue || w.WarehouseId == ((int?)warehouseId).Value
                                     orderby w.CreatedAt descending
                                     select w).Take(50).ToListAsync());
        base.ViewBag.WaveId = waveId;
        base.ViewBag.WarehouseId = warehouseId;
        base.ViewBag.Status = status;
        IQueryable<PickTask> query = from t in _db.PickTasks.Include((PickTask t) => t.Wave).Include((PickTask t) => t.Voucher).Include((PickTask t) => t.Item)
                .Include((PickTask t) => t.SourceLocation)
                .ThenInclude((Location l) => l.Zone)
                .Include((PickTask t) => t.TargetLocation)
                .Include((PickTask t) => t.Allocations)
                                     where !((int?)warehouseId).HasValue || (t.Wave != null && t.Wave.WarehouseId == ((int?)warehouseId).Value) || (t.Wave == null && t.Voucher != null && t.Voucher.WarehouseId == ((int?)warehouseId).Value)
                                     select t;
        if (waveId.HasValue)
        {
            query = query.Where((PickTask t) => t.WaveId == (long?)((long?)waveId).Value);
        }
        if (status.HasValue)
        {
            query = query.Where((PickTask t) => (byte)t.Status == ((byte?)status).Value);
        }
        if (scopedZoneIds != null)
        {
            query = query.Where((PickTask t) => t.SourceLocation != null && scopedZoneIds.Contains(t.SourceLocation.ZoneId));
        }
        List<PickTaskBoardRow> rows = (await (from t in query
                                              orderby (t.SourceLocation != null) ? t.SourceLocation.AisleSequence : int.MaxValue, (t.SourceLocation != null) ? t.SourceLocation.LocationCode : "", t.AssignedAt descending
                                              select t).Take(200).ToListAsync()).Select((PickTask t) => new PickTaskBoardRow
                                              {
                                                  PickTaskId = t.PickTaskId,
                                                  TaskCode = (t.TaskCode ?? ""),
                                                  WaveId = t.WaveId,
                                                  WaveCode = (t.Wave?.WaveCode ?? "Phát hành trực tiếp"),
                                                  VoucherId = t.VoucherId,
                                                  VoucherCode = ((t.IsBatchPick && t.Allocations.Count > 1) ? $"{t.Voucher?.VoucherCode ?? ""} +{t.Allocations.Select((PickTaskAllocation a) => a.VoucherId).Distinct().Count() - 1}" : (t.Voucher?.VoucherCode ?? "")),
                                                  ItemCode = (t.Item?.ItemCode ?? ""),
                                                  ItemName = t.Item?.ItemName,
                                                  LocationCode = (t.SourceLocation?.LocationCode ?? ""),
                                                  TargetQty = t.TargetQty,
                                                  PickedQty = t.PickedQty,
                                                  Status = t.Status,
                                                  PickTaskMode = t.PickTaskMode,
                                                  ParentPickTaskId = t.ParentPickTaskId,
                                                  TargetLocationCode = ((t.TargetLocation != null) ? t.TargetLocation.LocationCode : null),
                                                  IsBatchPick = t.IsBatchPick,
                                                  AllocationCount = t.Allocations.Count,
                                                  AssignedTo = t.AssignedTo,
                                                  PreferredScanValue = (t.Item?.Barcode ?? ""),
                                                  LotNumber = t.LotNumber,
                                                  ItemBarcode = t.Item?.Barcode,
                                                  ItemSkuCode = t.Item?.SkuCode,
                                                  TrackSerial = (t.Item?.TrackSerial ?? false),
                                                  RequiredSerialCount = 0,
                                                  PickedSerialCount = 0,
                                                  StartedAt = t.StartedAt,
                                                  CompletedAt = t.CompletedAt,
                                                  ZoneCode = t.SourceLocation?.Zone?.ZoneCode
                                              }).ToList();
        IQueryable<AppUser> userQuery = from u in _db.AppUsers.AsNoTracking().Include((AppUser u) => u.Role)
                                        where u.IsActive && (u.Role.RoleName == "Staff" || u.Role.RoleName == "Manager")
                                        select u;
        if (warehouseId.HasValue)
        {
            userQuery = userQuery.Where((AppUser u) => u.WarehouseId == (int?)((int?)warehouseId).Value);
        }
        else if (scopedWh.HasValue)
        {
            userQuery = userQuery.Where((AppUser u) => u.WarehouseId == (int?)((int?)scopedWh).Value);
        }
        base.ViewBag.EligibleUsers = (await userQuery.OrderBy((AppUser u) => u.FullName).ToListAsync());
        return View(rows);
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> Shipping(int? warehouseId, string? status, string? search)
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
        base.ViewBag.Status = status;
        base.ViewBag.Search = search;
        IQueryable<Voucher> query = from v in _db.Vouchers.Include((Voucher v) => v.Warehouse).Include((Voucher v) => v.Partner).Include((Voucher v) => v.Details)
                .Include((Voucher v) => v.Packages)
                                    where v.VoucherType == VoucherTypeEnum.XuatKho && v.IsPosted && !v.ShippedAt.HasValue
                                    select v;
        if (warehouseId.HasValue)
        {
            query = query.Where((Voucher v) => v.WarehouseId == ((int?)warehouseId).Value);
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("packing", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where((Voucher v) => !v.PackedAt.HasValue);
            }
            else if (status.Equals("ready", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where((Voucher v) => v.PackedAt.HasValue);
            }
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where((Voucher v) => v.VoucherCode.Contains(search) || (v.Partner != null && v.Partner.PartnerName.Contains(search)) || (v.TrackingNumber != null && v.TrackingNumber.Contains(search)) || (v.ManifestCode != null && v.ManifestCode.Contains(search)));
        }
        List<Voucher> raw = await query.OrderByDescending((Voucher v) => v.CreatedAt).Take(100).ToListAsync();
        List<long> voucherIds = raw.Select((Voucher v) => v.VoucherId).ToList();
        Dictionary<long, List<OutboundPackage>> packagesByVoucher = (from p in await _db.OutboundPackages.Where((OutboundPackage p) => voucherIds.Contains(p.VoucherId)).ToListAsync()
                                                                     group p by p.VoucherId).ToDictionary((IGrouping<long, OutboundPackage> g) => g.Key, (IGrouping<long, OutboundPackage> g) => g.ToList());
        Dictionary<long, ShipmentLoad> loadByVoucher = await _db.ShipmentLoadVouchers.AsNoTracking()
            .Include((ShipmentLoadVoucher x) => x.ShipmentLoad)
            .Where((ShipmentLoadVoucher x) => voucherIds.Contains(x.VoucherId)
                && x.RemovedAt == null
                && x.ShipmentLoad != null
                && x.ShipmentLoad.Status != ShipmentLoadStatusEnum.Cancelled)
            .ToDictionaryAsync((ShipmentLoadVoucher x) => x.VoucherId, (ShipmentLoadVoucher x) => x.ShipmentLoad!);
        Dictionary<long, List<CarrierShipment>> carrierShipmentsByVoucher = (await _db.CarrierShipments.AsNoTracking()
            .Where((CarrierShipment s) => voucherIds.Contains(s.VoucherId))
            .ToListAsync())
            .GroupBy((CarrierShipment s) => s.VoucherId)
            .ToDictionary((IGrouping<long, CarrierShipment> g) => g.Key, (IGrouping<long, CarrierShipment> g) => g.ToList());
        List<ShippingBoardRow> rows = raw.Select(delegate (Voucher v)
        {
            List<OutboundPackage>? value;
            List<OutboundPackage> list = (packagesByVoucher.TryGetValue(v.VoucherId, out value) ? value : new List<OutboundPackage>());
            loadByVoucher.TryGetValue(v.VoucherId, out ShipmentLoad? load);
            carrierShipmentsByVoucher.TryGetValue(v.VoucherId, out List<CarrierShipment>? carrierShipments);
            carrierShipments ??= new List<CarrierShipment>();
            int carrierCreated = carrierShipments.Count((CarrierShipment s) => s.Status == CarrierShipmentStatusEnum.Created || s.Status == CarrierShipmentStatusEnum.Delivered);
            int carrierQueued = carrierShipments.Count((CarrierShipment s) => s.Status == CarrierShipmentStatusEnum.Pending || s.Status == CarrierShipmentStatusEnum.Queued);
            int carrierFailed = carrierShipments.Count((CarrierShipment s) => s.Status == CarrierShipmentStatusEnum.Failed || s.Status == CarrierShipmentStatusEnum.DeliveryFailed);
            return new ShippingBoardRow
            {
                VoucherId = v.VoucherId,
                VoucherCode = v.VoucherCode,
                WarehouseId = v.WarehouseId,
                WarehouseName = (v.Warehouse?.WarehouseName ?? ""),
                PartnerName = v.Partner?.PartnerName,
                VoucherType = v.VoucherType,
                VoucherTypeName = v.VoucherTypeName,
                VoucherDate = v.VoucherDate,
                RequestedDeliveryDate = v.RequestedDeliveryDate,
                FulfillmentStatus = v.FulfillmentStatus,
                PackedAt = v.PackedAt,
                ShippedAt = v.ShippedAt,
                TrackingNumber = v.TrackingNumber,
                ManifestCode = v.ManifestCode,
                TotalLines = v.Details.Count,
                PackageCount = list.Count,
                PackageSummary = string.Join(", ", list.Select((OutboundPackage p) => p.PackageCode ?? "")),
                IsOverdue = (v.RequestedDeliveryDate.HasValue && v.RequestedDeliveryDate.Value < VietnamNow),
                RequiresManifest = (v.VoucherType == VoucherTypeEnum.ChuyenKho || v.VoucherType == VoucherTypeEnum.XuatSanXuat),
                RequiresTrackingOrManifest = (v.VoucherType == VoucherTypeEnum.XuatKho || v.VoucherType == VoucherTypeEnum.TraNCC),
                LoadCode = load?.LoadCode,
                LoadStatus = load?.Status,
                LoadCarrierName = load?.CarrierName,
                LoadRouteName = load?.RouteName,
                CarrierShipmentCreatedCount = carrierCreated,
                CarrierShipmentQueuedCount = carrierQueued,
                CarrierShipmentFailedCount = carrierFailed,
                CarrierShipmentSummary = carrierShipments.Count > 0 ? $"{carrierCreated}/{list.Count} kiện có vận đơn" : null
            };
        }).ToList();
        base.ViewBag.Vouchers = rows;
        base.ViewBag.FilterStatus = status;
        base.ViewBag.SearchText = search;
        return View(rows);
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> RfPicking(int? warehouseId)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        List<int>? scopedZoneIds = await GetScopedZoneIdsAsync();
        IQueryable<PickTask> tasksQuery = from t in _db.PickTasks.Include((PickTask t) => t.Wave).Include((PickTask t) => t.Voucher).Include((PickTask t) => t.VoucherDetail)
                .ThenInclude((VoucherDetail? d) => d!.Item)
                .Include((PickTask t) => t.Item)
                .Include((PickTask t) => t.SourceLocation)
                .ThenInclude((Location l) => l.Zone)
                .Include((PickTask t) => t.TargetLocation)
                .Include((PickTask t) => t.Allocations)
                                          where t.Status == PickTaskStatusEnum.Assigned || t.Status == PickTaskStatusEnum.Pending || t.Status == PickTaskStatusEnum.InProgress
                                          select t;
        if (warehouseId.HasValue)
        {
            tasksQuery = tasksQuery.Where((PickTask t) => (t.Wave != null && t.Wave.WarehouseId == ((int?)warehouseId).Value) || (t.Wave == null && t.Voucher != null && t.Voucher.WarehouseId == ((int?)warehouseId).Value));
        }
        if (scopedZoneIds != null)
        {
            tasksQuery = tasksQuery.Where((PickTask t) => t.SourceLocation != null && scopedZoneIds.Contains(t.SourceLocation.ZoneId));
        }
        if (!base.User.IsInRole("Admin") && !base.User.IsInRole("Manager"))
        {
            string actor = base.User.Identity?.Name ?? "";
            tasksQuery = tasksQuery.Where((PickTask t) => (t.Status == PickTaskStatusEnum.Pending && string.IsNullOrEmpty(t.AssignedTo)) || (t.Status != PickTaskStatusEnum.Pending && t.AssignedTo == actor));
        }
        List<PickTaskBoardRow> rows = (await (from t in tasksQuery
                                              orderby (t.SourceLocation != null) ? t.SourceLocation.AisleSequence : int.MaxValue, (t.SourceLocation != null) ? t.SourceLocation.LocationCode : "", t.DueAt descending
                                              select t).Take(100).ToListAsync()).Select((PickTask t) => new PickTaskBoardRow
                                              {
                                                  PickTaskId = t.PickTaskId,
                                                  TaskCode = t.TaskCode,
                                                  WaveId = t.WaveId,
                                                  WaveCode = (t.Wave?.WaveCode ?? "Phát hành trực tiếp"),
                                                  VoucherId = t.VoucherId,
                                                  VoucherCode = ((t.IsBatchPick && t.Allocations.Count > 1) ? $"{t.Voucher?.VoucherCode ?? ""} +{t.Allocations.Select((PickTaskAllocation a) => a.VoucherId).Distinct().Count() - 1}" : (t.Voucher?.VoucherCode ?? "")),
                                                  ItemCode = (t.Item?.ItemCode ?? t.VoucherDetail?.Item?.ItemCode ?? ""),
                                                  ItemName = (t.Item?.ItemName ?? t.VoucherDetail?.Item?.ItemName),
                                                  LocationCode = (t.SourceLocation?.LocationCode ?? ""),
                                                  TargetQty = t.TargetQty,
                                                  PickedQty = t.PickedQty,
                                                  Status = t.Status,
                                                  AssignedTo = t.AssignedTo,
                                                  PickTaskMode = t.PickTaskMode,
                                                  ParentPickTaskId = t.ParentPickTaskId,
                                                  TargetLocationCode = ((t.TargetLocation != null) ? t.TargetLocation.LocationCode : null),
                                                  IsBatchPick = t.IsBatchPick,
                                                  AllocationCount = t.Allocations.Count,
                                                  CompletedAt = t.CompletedAt,
                                                  TrackSerial = (t.VoucherDetail?.Item?.TrackSerial == true),
                                                  ZoneCode = t.SourceLocation?.Zone?.ZoneCode
                                              }).ToList();
        List<long> waveIds = (from r in rows
                              select r.WaveId into w
                              where w.HasValue
                              select w.Value).Distinct().ToList();
        if (waveIds.Count > 0)
        {
            List<PickTote> toteAssignments = await (from t in _db.PickTotes.Include((PickTote t) => t.PickCart)
                                                    where t.WaveId.HasValue && waveIds.Contains(t.WaveId.Value) && (int)t.Status != 0
                                                    select t).ToListAsync();
            foreach (PickTaskBoardRow row in rows)
            {
                PickTote? tote = toteAssignments.FirstOrDefault((PickTote t) => t.WaveId == row.WaveId && t.VoucherId == row.VoucherId);
                if (tote != null)
                {
                    row.ToteCode = tote.ToteCode;
                    row.CartCode = tote.PickCart?.CartCode;
                }
            }
        }
        base.ViewBag.WarehouseId = warehouseId;
        return View(rows);
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> OrderStreamingConfigs(int? warehouseId)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        base.ViewBag.Warehouses = (await GetVisibleWarehousesAsync());
        base.ViewBag.WarehouseId = warehouseId;
        IQueryable<WarehouseOrderStreamingConfig> query = _db.WarehouseOrderStreamingConfigs.Include((WarehouseOrderStreamingConfig c) => c.Warehouse).AsNoTracking().AsQueryable();
        if (warehouseId.HasValue)
        {
            query = query.Where((WarehouseOrderStreamingConfig c) => c.WarehouseId == ((int?)warehouseId).Value);
        }
        return View(await (from c in query
                           orderby c.Warehouse.WarehouseCode, c.IsActive descending
                           select c).ToListAsync());
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> CartManagement()
    {
        int? scopedWh = GetScopedWarehouseId();
        IQueryable<PickCart> cartsQuery = _db.PickCarts.Include((PickCart c) => c.Warehouse).Include((PickCart c) => c.Totes).AsQueryable();
        if (scopedWh.HasValue)
        {
            cartsQuery = cartsQuery.Where((PickCart c) => c.WarehouseId == ((int?)scopedWh).Value);
        }
        List<PickCart> carts = await cartsQuery.OrderBy((PickCart c) => c.CartCode).ToListAsync();
        base.ViewBag.Warehouses = (await (from w in _db.Warehouses
                                          where w.IsActive
                                          orderby w.WarehouseCode
                                          select w).ToListAsync());
        return View(carts);
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> AssignTotes(long waveId)
    {
        Wave? wave = await _db.Waves.Include((Wave w) => w.Vouchers).ThenInclude((Voucher v) => v.Partner).FirstOrDefaultAsync((Wave w) => w.WaveId == waveId);
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
        List<PickTote> availableTotes = await (from t in _db.PickTotes.Include((PickTote t) => t.PickCart)
                                               where (int)t.Status == 0 && (t.PickCart == null || t.PickCart.WarehouseId == wave.WarehouseId)
                                               orderby (t.PickCart != null) ? t.PickCart.CartCode : "~", t.SlotPosition
                                               select t).ToListAsync();
        List<PickTote> existingAssignments = await (from t in _db.PickTotes.Include((PickTote t) => t.PickCart)
                                                    where t.WaveId == (long?)waveId && (int)t.Status != 0
                                                    select t).ToListAsync();
        base.ViewBag.Wave = wave;
        base.ViewBag.AvailableTotes = availableTotes;
        base.ViewBag.ExistingAssignments = existingAssignments;
        return View();
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> ZoneAssignment()
    {
        int? scopedWh = GetScopedWarehouseId();
        IQueryable<AppUser> usersQuery = from u in _db.AppUsers.AsNoTracking().Include((AppUser u) => u.Role).Include((AppUser u) => u.Warehouse)
                                         where u.IsActive && u.Role.RoleName == "Staff"
                                         select u;
        if (scopedWh.HasValue)
        {
            usersQuery = usersQuery.Where((AppUser u) => u.WarehouseId == (int?)((int?)scopedWh).Value);
        }
        List<AppUser> users = await usersQuery.OrderBy((AppUser u) => u.FullName).ToListAsync();
        IQueryable<Zone> zonesQuery = from z in _db.Zones.AsNoTracking().Include((Zone z) => z.Warehouse)
                                      where z.IsActive
                                      select z;
        if (scopedWh.HasValue)
        {
            zonesQuery = zonesQuery.Where((Zone z) => z.WarehouseId == ((int?)scopedWh).Value);
        }
        List<Zone> zones = await zonesQuery.OrderBy((Zone z) => z.ZoneCode).ToListAsync();
        List<int> userIds = users.Select((AppUser u) => u.UserId).ToList();
        List<int> zoneIds = zones.Select((Zone z) => z.ZoneId).ToList();
        List<UserZoneAssignment> assignments = await (from a in _db.UserZoneAssignments.AsNoTracking()
                                                       where userIds.Contains(a.UserId) && zoneIds.Contains(a.ZoneId)
                                                       select a).ToListAsync();
        base.ViewBag.Users = users;
        base.ViewBag.Zones = zones;
        base.ViewBag.Assignments = assignments;
        return View();
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> NextTask(int? warehouseId, int? locationId, string? locationScan, string? lastCategory)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        List<int>? scopedZoneIds = await GetScopedZoneIdsAsync();
        string actor = base.User.Identity?.Name ?? "";
        int? resolvedLocationId = locationId;
        if (!resolvedLocationId.HasValue && !string.IsNullOrWhiteSpace(locationScan))
        {
            Location? loc = await _taskInterleavingService.ResolveLocationAsync(locationScan, warehouseId ?? scopedWh);
            resolvedLocationId = loc?.LocationId;
            if (loc == null && !string.IsNullOrWhiteSpace(locationScan))
            {
                base.TempData["Error"] = "Không tìm thấy vị trí '" + locationScan.Trim() + "' trong kho.";
            }
        }
        string? text = lastCategory?.ToLowerInvariant();
        if (1 == 0)
        {
        }
        TaskCategoryEnum? taskCategoryEnum;
        switch (text)
        {
            case "pick":
                taskCategoryEnum = TaskCategoryEnum.Pick;
                break;
            case "movement":
            case "move":
                taskCategoryEnum = TaskCategoryEnum.Movement;
                break;
            default:
                taskCategoryEnum = null;
                break;
        }
        if (1 == 0)
        {
        }
        InterleavedTaskQueue model = await _taskInterleavingService.GetNextTasksAsync(lastCompletedCategory: taskCategoryEnum, currentUser: actor, currentLocationId: resolvedLocationId, scopedWarehouseId: warehouseId ?? scopedWh, scopedZoneIds: scopedZoneIds);
        base.ViewBag.WarehouseId = warehouseId;
        base.ViewBag.Warehouses = (await GetVisibleWarehousesAsync());
        return View(model);
    }

}
