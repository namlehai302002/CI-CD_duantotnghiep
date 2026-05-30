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

using Microsoft.AspNetCore.Http;

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

    private async Task<MovementTaskPageViewModel> BuildMovementTasksAsync(int? warehouseId, MovementTaskTypeEnum? taskType, MovementTaskStatusEnum? status, string? assignedTo, string? search)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        IQueryable<MovementTask> query = _db.MovementTasks.AsNoTracking().Include((MovementTask t) => t.Warehouse).Include((MovementTask t) => t.Item)
            .Include((MovementTask t) => t.LicensePlate)
            .Include((MovementTask t) => t.SourceLocation)
            .Include((MovementTask t) => t.DestinationLocation)
            .AsQueryable();
        if (warehouseId.HasValue)
        {
            query = query.Where((MovementTask t) => t.WarehouseId == ((int?)warehouseId).Value);
        }
        if (taskType.HasValue)
        {
            query = query.Where((MovementTask t) => t.TaskType == ((MovementTaskTypeEnum?)taskType).Value);
        }
        if (status.HasValue)
        {
            query = query.Where((MovementTask t) => t.Status == ((MovementTaskStatusEnum?)status).Value);
        }
        if (!string.IsNullOrWhiteSpace(assignedTo))
        {
            string assignee = assignedTo.Trim();
            query = query.Where((MovementTask t) => t.AssignedTo != null && t.AssignedTo.Contains(assignee));
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();
            query = query.Where((MovementTask t) => t.TaskCode.Contains(keyword) || (t.Item != null && (t.Item.ItemCode.Contains(keyword) || t.Item.ItemName.Contains(keyword))) || (t.LpnCodeSnapshot != null && t.LpnCodeSnapshot.Contains(keyword)) || (t.LicensePlate != null && t.LicensePlate.LpnCode.Contains(keyword)) || (t.SourceLocation != null && t.SourceLocation.LocationCode.Contains(keyword)) || (t.DestinationLocation != null && t.DestinationLocation.LocationCode.Contains(keyword)) || (t.LotNumber != null && t.LotNumber.Contains(keyword)));
        }
        List<MovementTaskRow> tasks = await (from t in (from t in query
                                                        orderby (t.Status == MovementTaskStatusEnum.InProgress) ? 0 : ((t.Status == MovementTaskStatusEnum.Assigned) ? 1 : ((t.Status == MovementTaskStatusEnum.Pending) ? 2 : 3)), t.Priority descending, t.RoutePriorityScore descending, t.DueAt, t.CreatedAt
                                                        select t).Take(500)
                                             select new MovementTaskRow
                                             {
                                                 MovementTaskId = t.MovementTaskId,
                                                 TaskCode = t.TaskCode,
                                                 WarehouseId = t.WarehouseId,
                                                 WarehouseName = ((t.Warehouse != null) ? t.Warehouse.WarehouseName : ""),
                                                 ItemId = t.ItemId,
                                                 ItemCode = ((t.Item != null) ? t.Item.ItemCode : ""),
                                                 ItemName = ((t.Item != null) ? t.Item.ItemName : ""),
                                                 MovementMode = t.MovementMode,
                                                 LicensePlateId = t.LicensePlateId,
                                                 LpnCode = t.LpnCodeSnapshot ?? ((t.LicensePlate != null) ? t.LicensePlate.LpnCode : null),
                                                 LpnDetailCount = t.LpnDetailCount,
                                                 LpnDistinctItemCount = t.LpnDistinctItemCount,
                                                 SourceLocationCode = ((t.SourceLocation != null) ? t.SourceLocation.LocationCode : ""),
                                                 DestinationLocationCode = ((t.DestinationLocation != null) ? t.DestinationLocation.LocationCode : ""),
                                                 TaskType = t.TaskType,
                                                 Status = t.Status,
                                                 Priority = t.Priority,
                                                 PlannedQty = t.PlannedQty,
                                                 ConfirmedQty = t.ConfirmedQty,
                                                 LotNumber = t.LotNumber,
                                                 ExpiryDate = t.ExpiryDate,
                                                 AssignedTo = t.AssignedTo,
                                                 CreatedAt = t.CreatedAt,
                                                 DueAt = t.DueAt,
                                                 StartedAt = t.StartedAt,
                                                 CompletedAt = t.CompletedAt,
                                                 SourceReason = t.SourceReason,
                                                 SourceModule = t.SourceModule,
                                                 ReplenishmentTriggerType = t.ReplenishmentTriggerType,
                                                 DemandQtySnapshot = t.DemandQtySnapshot,
                                                 ForecastQtySnapshot = t.ForecastQtySnapshot,
                                                 RoutePriorityScore = t.RoutePriorityScore,
                                                 TravelSequenceScore = t.TravelSequenceScore
                                             }).ToListAsync();
        List<Warehouse> warehouses = await (from w in _db.Warehouses.AsNoTracking()
                                            where w.IsActive && (!((int?)scopedWh).HasValue || w.WarehouseId == ((int?)scopedWh).Value)
                                            orderby w.WarehouseCode
                                            select w).ToListAsync();
        return new MovementTaskPageViewModel
        {
            WarehouseId = warehouseId,
            TaskType = taskType,
            Status = status,
            AssignedTo = assignedTo,
            Search = search,
            Warehouses = warehouses,
            Tasks = tasks,
            OpenCount = tasks.Count(delegate (MovementTaskRow t)
            {
                MovementTaskStatusEnum status2 = t.Status;
                return status2 - 1 <= MovementTaskStatusEnum.Pending;
            }),
            InProgressCount = tasks.Count((MovementTaskRow t) => t.Status == MovementTaskStatusEnum.InProgress),
            CompletedCount = tasks.Count((MovementTaskRow t) => t.Status == MovementTaskStatusEnum.Completed),
            ShortCount = tasks.Count((MovementTaskRow t) => t.Status == MovementTaskStatusEnum.Short)
        };
    }


    private async Task<List<ReplenishmentSuggestionRow>> BuildReplenishmentSuggestionsAsync(int? warehouseId, string? search)
    {
        if (_replenishmentAutomationService is not null)
        {
            return await _replenishmentAutomationService.BuildSuggestionsAsync(warehouseId, search);
        }

        List<Item> items = await (from i in _db.Items.AsNoTracking().Include((Item i) => i.DefaultLocation).ThenInclude((Location? l) => l!.Zone)
                                  where i.IsActive && i.DefaultLocationId.HasValue && i.DefaultLocation != null && i.DefaultLocation.IsActive && i.DefaultLocation.Zone != null && (!((int?)warehouseId).HasValue || i.DefaultLocation!.Zone.WarehouseId == ((int?)warehouseId).Value)
                                  orderby i.ItemCode
                                  select i).ToListAsync();
        if (!items.Any())
        {
            return new List<ReplenishmentSuggestionRow>();
        }
        List<int> itemIds = items.Select((Item i) => i.ItemId).Distinct().ToList();
        List<int> warehouseIds = items.Select((Item i) => i.DefaultLocation!.Zone.WarehouseId).Distinct().ToList();
        List<ItemLocation> itemLocations = await (from il in _db.ItemLocations.AsNoTracking().Include((ItemLocation il) => il.Location).ThenInclude((Location? l) => l!.Zone)
                                                  where itemIds.Contains(il.ItemId) && il.Quantity > 0m && il.Location != null && il.Location.IsActive && il.Location.Zone != null && warehouseIds.Contains(il.Location!.Zone.WarehouseId)
                                                  select il).ToListAsync();
        Dictionary<(int ItemId, int, string Lot, DateTime? ExpiryDate), int> lpnLookup = (await (from d in _db.LicensePlateDetails.AsNoTracking()
                                                                                                 where itemIds.Contains(d.ItemId)
                                                                                                     && d.LicensePlate != null
                                                                                                     && d.LicensePlate.IsActive
                                                                                                     && d.LicensePlate.Status != LpnStatusEnum.Voided
                                                                                                     && d.LicensePlate.CurrentLocationId.HasValue
                                                                                                     && warehouseIds.Contains(d.LicensePlate.WarehouseId)
                                                                                                 group d by new
                                                                                                 {
                                                                                                     d.ItemId,
                                                                                                     LocationId = d.LicensePlate!.CurrentLocationId.GetValueOrDefault(),
                                                                                                     Lot = (d.LotNumber ?? ""),
                                                                                                     d.ExpiryDate
                                                                                                 } into g
                                                                                                 select new
                                                                                                 {
                                                                                                     g.Key.ItemId,
                                                                                                     g.Key.LocationId,
                                                                                                     g.Key.Lot,
                                                                                                     g.Key.ExpiryDate,
                                                                                                     Count = g.Count()
                                                                                                 }).ToListAsync()).ToDictionary(x => (ItemId: x.ItemId, x.LocationId, Lot: x.Lot, ExpiryDate: x.ExpiryDate), x => x.Count);
        List<int> defaultLocationIds = (from i in items
                                        where i.DefaultLocationId.HasValue
                                        select i.DefaultLocationId.GetValueOrDefault()).Distinct().ToList();
        (from il in await (from il in _db.ItemLocations.AsNoTracking().Include((ItemLocation il) => il.Item)
                           where defaultLocationIds.Contains(il.LocationId) && il.Quantity > 0m
                           select il).ToListAsync()
         group il by il.LocationId).ToDictionary((IGrouping<int, ItemLocation> g) => g.Key, (IGrouping<int, ItemLocation> g) => g.Sum(delegate (ItemLocation il)
     {
         Item? item2 = il.Item;
         return (item2 != null && item2.ItemType == ItemTypeEnum.HoaChat) ? il.Quantity : (il.Quantity * (il.Item?.Weight ?? 1m));
     }));
        List<ReplenishmentSuggestionRow> rows = new List<ReplenishmentSuggestionRow>();
        foreach (Item item in items)
        {
            Location? defaultLocation = item.DefaultLocation;
            if (defaultLocation?.Zone == null)
            {
                continue;
            }
            decimal triggerQty = item.ReorderPoint ?? item.MinThreshold;
            if (triggerQty <= 0m)
            {
                continue;
            }
            decimal targetQty = item.MaxThreshold ?? Math.Max(triggerQty * 2m, triggerQty);
            int warehouseIdForItem = defaultLocation.Zone.WarehouseId;
            List<ItemLocation> relevantRows = itemLocations.Where(delegate (ItemLocation il)
            {
                int result;
                if (il.ItemId == item.ItemId)
                {
                    Location? location = il.Location;
                    result = ((location != null && location.Zone?.WarehouseId == warehouseIdForItem) ? 1 : 0);
                }
                else
                {
                    result = 0;
                }
                return (byte)result != 0;
            }).ToList();
            decimal pickFaceQty = relevantRows.Where((ItemLocation il) => il.LocationId == defaultLocation.LocationId).Sum((ItemLocation il) => Math.Max(0m, il.Quantity - il.ReservedQty));
            if (pickFaceQty >= triggerQty)
            {
                continue;
            }
            List<ItemLocation> sourceCandidates = relevantRows.Where((ItemLocation il) => il.LocationId != defaultLocation.LocationId && il.Quantity - il.ReservedQty > 0m).ToList();
            if (!string.IsNullOrWhiteSpace(item.AllowedZoneTypes))
            {
                HashSet<byte> allowedZt = new HashSet<byte>();
                string[] array = item.AllowedZoneTypes.Split(',');
                foreach (string s in array)
                {
                    if (byte.TryParse(s.Trim(), out var zt))
                    {
                        allowedZt.Add(zt);
                    }
                }
                if (allowedZt.Count > 0)
                {
                    sourceCandidates = sourceCandidates.Where((ItemLocation il) => il.Location?.Zone != null && allowedZt.Contains((byte)il.Location.Zone.ZoneType)).ToList();
                }
            }
            sourceCandidates = (from il in sourceCandidates
                                orderby (!item.TrackExpiry) ? DateTime.MaxValue : (il.ExpiryDate ?? DateTime.MaxValue), il.Quantity - il.ReservedQty descending
                                select il).ToList();
            if (sourceCandidates.Any())
            {
                ItemLocation source = sourceCandidates[0];
                decimal sourceAvailable = Math.Max(0m, source.Quantity - source.ReservedQty);
                decimal suggestedQty = Math.Min(Math.Max(0m, targetQty - pickFaceQty), sourceAvailable);
                if (!(suggestedQty <= 0m))
                {
                    bool hasActiveLpn = lpnLookup.ContainsKey((item.ItemId, source.LocationId, source.LotNumber ?? "", source.ExpiryDate));
                    ReplenishmentSuggestionRow row = new ReplenishmentSuggestionRow
                    {
                        WarehouseId = warehouseIdForItem,
                        WarehouseName = (defaultLocation.Zone.Warehouse?.WarehouseName ?? $"Kho {warehouseIdForItem}"),
                        ItemId = item.ItemId,
                        ItemCode = item.ItemCode,
                        ItemName = item.ItemName,
                        DefaultLocationId = defaultLocation.LocationId,
                        DefaultLocationCode = defaultLocation.LocationCode,
                        SourceItemLocationId = source.ItemLocationId,
                        SourceLocationId = source.LocationId,
                        SourceLocationCode = (source.Location?.LocationCode ?? source.LocationId.ToString()),
                        PickFaceQty = pickFaceQty,
                        SourceAvailableQty = sourceAvailable,
                        TriggerQty = triggerQty,
                        TargetQty = targetQty,
                        SuggestedQty = suggestedQty,
                        LotNumber = source.LotNumber,
                        ExpiryDate = source.ExpiryDate,
                        HasActiveLpn = hasActiveLpn,
                        SuggestionReason = (hasActiveLpn ? "Nguồn hàng đang gắn mã kiện (LPN), nên di chuyển theo từng kiện để giữ truy vết." : ((pickFaceQty <= 0m) ? "Vị trí lấy hàng đã hết tồn, nên bổ sung ngay." : "Vị trí lấy hàng xuống dưới ngưỡng bổ sung."))
                    };
                    rows.Add(row);
                }
            }
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();
            rows = rows.Where((ReplenishmentSuggestionRow r) => r.ItemCode.Contains(keyword, StringComparison.OrdinalIgnoreCase) || r.ItemName.Contains(keyword, StringComparison.OrdinalIgnoreCase) || r.DefaultLocationCode.Contains(keyword, StringComparison.OrdinalIgnoreCase) || r.SourceLocationCode.Contains(keyword, StringComparison.OrdinalIgnoreCase) || (r.LotNumber?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        }
        return (from r in rows
                orderby r.PickFaceQty <= 0m descending, r.TriggerQty - r.PickFaceQty descending, r.ItemCode
                select r).ToList();
    }


    private async Task<List<SlottingSuggestionRow>> BuildSlottingSuggestionsAsync(int? warehouseId, string? search)
    {
        List<ItemLocation> itemLocations = await (from il in _db.ItemLocations.AsNoTracking().Include((ItemLocation il) => il.Item).Include((ItemLocation il) => il.Location)
                .ThenInclude((Location? l) => l!.Zone)
                .ThenInclude((Zone z) => z.Warehouse)
                                                  where il.Quantity > 0m && il.Item != null && il.Item.IsActive && il.Location != null && il.Location.IsActive && il.Location.Zone != null && il.Location.Zone.IsActive && (!((int?)warehouseId).HasValue || il.Location!.Zone.WarehouseId == ((int?)warehouseId).Value)
                                                  select il).ToListAsync();
        List<Location> locations = await (from l in _db.Locations.AsNoTracking().Include((Location l) => l.Zone).ThenInclude((Zone z) => z.Warehouse)
                                          where l.IsActive && l.Zone != null && l.Zone.IsActive && l.Zone.ZoneType == ZoneTypeEnum.Storage && (!((int?)warehouseId).HasValue || l.Zone.WarehouseId == ((int?)warehouseId).Value)
                                          select l).ToListAsync();
        List<int> itemIds = Enumerable.Union(second: (await (from i in _db.Items.AsNoTracking().Include((Item i) => i.DefaultLocation).ThenInclude((Location? l) => l!.Zone)
                .ThenInclude((Zone z) => z.Warehouse)
                                                             where i.IsActive && i.DefaultLocationId.HasValue && i.DefaultLocation != null && i.DefaultLocation.Zone != null && (!((int?)warehouseId).HasValue || i.DefaultLocation!.Zone.WarehouseId == ((int?)warehouseId).Value)
                                                             select i).ToListAsync()).Select((Item i) => i.ItemId), first: itemLocations.Select((ItemLocation il) => il.ItemId)).Distinct().ToList();
        if (itemIds.Count == 0)
        {
            return new List<SlottingSuggestionRow>();
        }
        Dictionary<int, Item> itemMaster = await (from i in _db.Items.AsNoTracking().Include((Item i) => i.DefaultLocation).ThenInclude((Location? l) => l!.Zone)
                .ThenInclude((Zone z) => z.Warehouse)
                                                  where itemIds.Contains(i.ItemId)
                                                  select i).ToDictionaryAsync((Item i) => i.ItemId);
        List<int> warehouseIds = (from l in locations
                                  where l.Zone != null
                                  select l.Zone.WarehouseId).Union(from il in itemLocations
                                                                   where il.Location?.Zone != null
                                                                   select il.Location!.Zone.WarehouseId).Distinct().ToList();
        List<ItemVelocityClassification> velocityRows = await (from v in _db.ItemVelocityClassifications.AsNoTracking()
                                                               where v.IsActive && itemIds.Contains(v.ItemId) && warehouseIds.Contains(v.WarehouseId)
                                                               select v).ToListAsync();
        Dictionary<int, List<int>> occupiedDefaults = (from i in await (from i in _db.Items.AsNoTracking()
                                                                        where i.IsActive && i.DefaultLocationId.HasValue
                                                                        select new
                                                                        {
                                                                            ItemId = i.ItemId,
                                                                            DefaultLocationId = i.DefaultLocationId.GetValueOrDefault()
                                                                        }).ToListAsync()
                                                       group i by i.DefaultLocationId).ToDictionary(g => g.Key, g => g.Select(x => x.ItemId).ToList());
        Dictionary<int, SlottingLocationLoad> loadByLocation = (from il in itemLocations
                                                                group il by il.LocationId).ToDictionary((IGrouping<int, ItemLocation> g) => g.Key, (IGrouping<int, ItemLocation> g) => new SlottingLocationLoad
                                                                {
                                                                    WeightLoadKg = g.Sum((ItemLocation x) => x.Quantity * (x.Item?.Weight ?? 1m)),
                                                                    OccupiedItemIds = g.Select((ItemLocation x) => x.ItemId).Distinct().ToHashSet()
                                                                });
        List<SlottingSuggestionRow> suggestions = new List<SlottingSuggestionRow>();
        foreach (int itemId in itemIds)
        {
            if (!itemMaster.TryGetValue(itemId, out var item))
            {
                continue;
            }
            List<ItemLocation> rows = itemLocations.Where((ItemLocation il) => il.ItemId == itemId).ToList();
            decimal totalStock = rows.Sum((ItemLocation r) => r.Quantity);
            if (totalStock <= 0m && !item.DefaultLocationId.HasValue)
            {
                continue;
            }
            int? currentDefaultId = item.DefaultLocationId;
            decimal currentDefaultQty = (currentDefaultId.HasValue ? rows.Where((ItemLocation r) => r.LocationId == currentDefaultId.Value).Sum((ItemLocation r) => r.Quantity) : 0m);
            List<int> itemWarehouseIds = (from r in rows
                                          where r.Location?.Zone != null
                                          select r.Location!.Zone.WarehouseId).Distinct().ToList();
            if (item.DefaultLocation?.Zone != null && !itemWarehouseIds.Contains(item.DefaultLocation.Zone.WarehouseId))
            {
                itemWarehouseIds.Add(item.DefaultLocation.Zone.WarehouseId);
            }
            if (warehouseId.HasValue && !itemWarehouseIds.Contains(warehouseId.Value))
            {
                itemWarehouseIds.Add(warehouseId.Value);
            }
            if (itemWarehouseIds.Count == 0)
            {
                continue;
            }
            foreach (int itemWarehouseId in itemWarehouseIds)
            {
                ItemVelocityClassification? velocity = (from v in velocityRows
                                                        where v.ItemId == itemId && v.WarehouseId == itemWarehouseId
                                                        orderby v.LastAnalyzedAt descending
                                                        select v).FirstOrDefault();
                char abcClass = _slottingPlanningService.ResolveAbcClass(item, velocity);
                string velocityBasis = ((velocity != null) ? ("Velocity " + velocity.CombinedClass) : (string.IsNullOrWhiteSpace(item.AbcClass) ? "Default ABC C" : $"Item ABC {abcClass}"));
                List<SlottingCandidateScore> candidates = (from l in locations.Where(delegate (Location l)
                    {
                        Zone? zone = l.Zone;
                        return zone != null && zone.WarehouseId == itemWarehouseId;
                    })
                                                           select _slottingPlanningService.ScoreSlottingCandidate(l, item, rows, totalStock, abcClass, velocity, loadByLocation, occupiedDefaults, 23m) into c
                                                           where c != null
                                                           select (c)).ToList();
                if (abcClass == 'A' && candidates.Any((SlottingCandidateScore c) => c.Location.IsGoldenZone))
                {
                    candidates = candidates.Where((SlottingCandidateScore c) => c.Location.IsGoldenZone).ToList();
                }
                SlottingCandidateScore? best = (from c in candidates
                                                orderby c.TotalScore descending, c.SameItemQty descending, Math.Abs(c.Location.HeightLevel - 3), c.Location.LocationCode
                                                select c).FirstOrDefault();
                if (best?.Location?.Zone != null && (!currentDefaultId.HasValue || currentDefaultId.Value != best.Location.LocationId))
                {
                    decimal dominancePercent = ((totalStock <= 0m) ? 0m : Math.Round(best.SameItemQty / totalStock * 100m, 2));
                    suggestions.Add(new SlottingSuggestionRow
                    {
                        WarehouseId = best.Location.Zone.WarehouseId,
                        WarehouseName = (best.Location.Zone.Warehouse?.WarehouseName ?? $"Kho {best.Location.Zone.WarehouseId}"),
                        ItemId = item.ItemId,
                        ItemCode = item.ItemCode,
                        ItemName = item.ItemName,
                        CurrentDefaultLocationId = currentDefaultId,
                        CurrentDefaultLocationCode = item.DefaultLocation?.LocationCode,
                        SuggestedLocationId = best.Location.LocationId,
                        SuggestedLocationCode = best.Location.LocationCode,
                        TotalStockQty = totalStock,
                        SuggestedLocationQty = best.SameItemQty,
                        DominancePercent = dominancePercent,
                        AbcClass = abcClass.ToString(),
                        VelocityBasis = velocityBasis,
                        SlottingScore = best.TotalScore,
                        VelocityScore = best.VelocityScore,
                        ErgonomicScore = best.ErgonomicScore,
                        CapacityScore = best.CapacityScore,
                        SuggestedHeightLevel = best.Location.HeightLevel,
                        SuggestedIsGoldenZone = best.Location.IsGoldenZone,
                        ItemWeightKg = item.Weight,
                        Reason = _slottingPlanningService.BuildSlottingReason(item, best, abcClass, velocityBasis, currentDefaultQty, dominancePercent),
                        CanApply = true
                    });
                }
            }
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();
            suggestions = suggestions.Where(delegate (SlottingSuggestionRow s)
            {
                int result;
                if (!s.ItemCode.Contains(keyword, StringComparison.OrdinalIgnoreCase) && !s.ItemName.Contains(keyword, StringComparison.OrdinalIgnoreCase) && !s.AbcClass.Contains(keyword, StringComparison.OrdinalIgnoreCase) && !s.VelocityBasis.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    string? currentDefaultLocationCode = s.CurrentDefaultLocationCode;
                    if (currentDefaultLocationCode == null || !currentDefaultLocationCode.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        result = (s.SuggestedLocationCode.Contains(keyword, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
                        goto IL_0080;
                    }
                }
                result = 1;
                goto IL_0080;
            IL_0080:
                return (byte)result != 0;
            }).ToList();
        }
        return (from s in suggestions
                orderby s.SlottingScore descending, s.DominancePercent descending, s.ItemCode
                select s).ToList();
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> MovementTasks(int? warehouseId, MovementTaskTypeEnum? taskType, MovementTaskStatusEnum? status, string? assignedTo, string? search)
    {
        return View(await BuildMovementTasksAsync(warehouseId, taskType, status, assignedTo, search));
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> RfMovement(int? warehouseId)
    {
        MovementTaskPageViewModel model = await BuildMovementTasksAsync(warehouseId, null, null, (base.User.IsInRole("Admin") || base.User.IsInRole("Manager")) ? null : base.User.Identity?.Name, null);
        model.Tasks = model.Tasks.Where(delegate (MovementTaskRow t)
        {
            MovementTaskStatusEnum status = t.Status;
            return status - 1 <= MovementTaskStatusEnum.Assigned;
        }).ToList();
        return View(model);
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLpnMovementTask(string lpnCode, string destinationScan, MovementTaskTypeEnum taskType = MovementTaskTypeEnum.Relocate, int? warehouseId = null)
    {
        int? scopedWh = GetScopedWarehouseId();
        try
        {
            string scan = (destinationScan ?? "").Trim();
            Location? destination = await _db.Locations.Include((Location l) => l.Zone).FirstOrDefaultAsync((Location l) =>
                l.IsActive
                && (l.LocationCode == scan || l.Barcode == scan || l.LocationId.ToString() == scan)
                && (!((int?)scopedWh).HasValue || l.Zone!.WarehouseId == ((int?)scopedWh).Value));
            if (destination == null)
            {
                throw new BusinessRuleException("Khong tim thay vi tri dich cho LPN movement.", "MOVEMENT_DESTINATION_NOT_FOUND", "Location");
            }
            MovementTask task = await _movementTaskService.CreateLpnMovementTaskAsync(lpnCode, destination.LocationId, taskType, MovementTaskPriorityEnum.High, scopedWh, base.User.Identity?.Name ?? "system", "ManualLpnMove", "Manual atomic LPN movement.");
            warehouseId = scopedWh ?? task.WarehouseId;
            base.TempData["Success"] = "Da tao nhiem vu LPN " + task.TaskCode + ".";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("MovementTasks", new
        {
            warehouseId = (scopedWh ?? warehouseId)
        });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignMovementTask(long movementTaskId, string assignedTo, int? warehouseId = null)
    {
        int? scopedWh = GetScopedWarehouseId();
        try
        {
            MovementTask task = await _movementTaskService.AssignAsync(movementTaskId, assignedTo, scopedWh, base.User.Identity?.Name ?? "system");
            warehouseId = scopedWh ?? task.WarehouseId;
            base.TempData["Success"] = $"Đã gán nhiệm vụ {task.TaskCode} cho {assignedTo}.";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("MovementTasks", new
        {
            warehouseId = (scopedWh ?? warehouseId)
        });
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartMovementTask(long movementTaskId, int? warehouseId = null)
    {
        int? scopedWh = GetScopedWarehouseId();
        try
        {
            MovementTask task = await _movementTaskService.StartAsync(movementTaskId, scopedWh, base.User.Identity?.Name ?? "system");
            warehouseId = scopedWh ?? task.WarehouseId;
            base.TempData["Success"] = "Đã bắt đầu nhiệm vụ " + task.TaskCode + ".";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("RfMovement", new
        {
            warehouseId = (scopedWh ?? warehouseId)
        });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelMovementTask(long movementTaskId, string? reason, int? warehouseId = null)
    {
        int? scopedWh = GetScopedWarehouseId();
        try
        {
            MovementTask task = await _movementTaskService.CancelAsync(movementTaskId, reason, scopedWh, base.User.Identity?.Name ?? "system");
            warehouseId = scopedWh ?? task.WarehouseId;
            base.TempData["Success"] = "Đã hủy nhiệm vụ " + task.TaskCode + ".";
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("MovementTasks", new
        {
            warehouseId = (scopedWh ?? warehouseId)
        });
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmMovementTask(long movementTaskId, string? sourceScan, string? destinationScan, decimal confirmedQty, int? warehouseId = null, string? lpnScan = null, decimal? queuedBaselineConfirmedQty = null)
    {
        const decimal qtyTolerance = 0.0001m;
        bool queued = QueuedOperationResponse.IsQueued(this);
        int? scopedWh = GetScopedWarehouseId();
        static string MovementRedirectUrl(int? wh)
            => wh.HasValue ? $"/Operations/RfMovement?warehouseId={wh.Value}" : "/Operations/RfMovement";

        if (queued)
        {
            MovementTask? taskState = await _db.MovementTasks.AsNoTracking()
                .FirstOrDefaultAsync(t => t.MovementTaskId == movementTaskId);
            if (taskState == null)
            {
                return QueuedOperationResponse.Json(this, false, "Không tìm thấy nhiệm vụ di chuyển.", null, StatusCodes.Status404NotFound, "NOT_FOUND");
            }
            if (scopedWh.HasValue && taskState.WarehouseId != scopedWh.Value)
            {
                return QueuedOperationResponse.Json(this, false, "Bạn không có quyền thao tác kho của nhiệm vụ này.", null, StatusCodes.Status403Forbidden, "FORBIDDEN");
            }

            bool baselineSatisfied = queuedBaselineConfirmedQty.HasValue
                && taskState.ConfirmedQty + qtyTolerance >= queuedBaselineConfirmedQty.Value + confirmedQty;
            if (taskState.Status is MovementTaskStatusEnum.Completed or MovementTaskStatusEnum.Short || baselineSatisfied)
            {
                return QueuedOperationResponse.Json(this, true, $"Nhiệm vụ {taskState.TaskCode} đã được xác nhận trước đó.", MovementRedirectUrl(scopedWh ?? warehouseId ?? taskState.WarehouseId));
            }
        }

        try
        {
            MovementTask task = await _movementTaskService.CompleteAsync(movementTaskId, sourceScan, destinationScan, confirmedQty, scopedWh, base.User.Identity?.Name ?? "system", lpnScan);
            warehouseId = scopedWh ?? task.WarehouseId;
            if (queued)
            {
                string message = task.Status == MovementTaskStatusEnum.Short
                    ? $"Đã xác nhận thiếu nhiệm vụ {task.TaskCode}: {task.ConfirmedQty:N2}/{task.PlannedQty:N2}."
                    : "Đã hoàn tất nhiệm vụ " + task.TaskCode + ".";
                return QueuedOperationResponse.Json(this, true, message, MovementRedirectUrl(warehouseId));
            }
            base.TempData["Success"] = ((task.Status == MovementTaskStatusEnum.Short) ? $"Đã xác nhận thiếu nhiệm vụ {task.TaskCode}: {task.ConfirmedQty:N2}/{task.PlannedQty:N2}." : ("Đã hoàn tất nhiệm vụ " + task.TaskCode + "."));
        }
        catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
        {
            if (queued)
            {
                int statusCode = ex is UnauthorizedAccessException ? StatusCodes.Status403Forbidden : StatusCodes.Status422UnprocessableEntity;
                string code = ex is UnauthorizedAccessException ? "FORBIDDEN" : "BUSINESS_RULE";
                return QueuedOperationResponse.Json(this, false, UserSafeError.From(ex), MovementRedirectUrl(scopedWh ?? warehouseId), statusCode, code);
            }
            base.TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction("RfMovement", new
        {
            warehouseId = (scopedWh ?? warehouseId)
        });
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> LpnLookup(int? warehouseId, string? search, bool activeOnly = true)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        IQueryable<LicensePlate> query = _db.LicensePlates.AsNoTracking().Include((LicensePlate l) => l.Warehouse)
            .Include((LicensePlate l) => l.CurrentLocation)
            .Include((LicensePlate l) => l.Voucher)
            .Include((LicensePlate l) => l.Details).ThenInclude((LicensePlateDetail d) => d.Item)
            .AsQueryable();
        if (warehouseId.HasValue)
        {
            query = query.Where((LicensePlate l) => l.WarehouseId == ((int?)warehouseId).Value);
        }
        if (activeOnly)
        {
            query = query.Where((LicensePlate l) => l.IsActive);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();
            query = query.Where((LicensePlate l) => l.LpnCode.Contains(keyword)
                || (l.Voucher != null && l.Voucher.VoucherCode.Contains(keyword))
                || (l.CurrentLocation != null && l.CurrentLocation.LocationCode.Contains(keyword))
                || l.Details.Any(d => (d.Item != null && (d.Item.ItemCode.Contains(keyword) || d.Item.ItemName.Contains(keyword))) || (d.LotNumber != null && d.LotNumber.Contains(keyword))));
        }
        List<LicensePlate> lpns = await query.OrderByDescending((LicensePlate l) => l.CreatedAt).Take(500).ToListAsync();
        List<LpnLookupRow> rows = lpns.Select(l =>
        {
            var details = l.Details.OrderBy(d => d.LicensePlateDetailId).ToList();
            var first = details.FirstOrDefault();
            var itemSummary = details.Count == 0
                ? ""
                : string.Join(", ", details.Take(2).Select(d => d.Item?.ItemCode ?? d.ItemId.ToString())) + (details.Count > 2 ? $" +{details.Count - 2}" : "");
            var lotSummary = details.Count == 0
                ? null
                : string.Join(", ", details.Select(d => d.LotNumber).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Take(2));
            return new LpnLookupRow
            {
                LicensePlateId = l.LicensePlateId,
                LpnCode = l.LpnCode,
                WarehouseId = l.WarehouseId,
                WarehouseName = ((l.Warehouse != null) ? l.Warehouse.WarehouseName : ""),
                VoucherId = l.VoucherId,
                VoucherCode = ((l.Voucher != null) ? l.Voucher.VoucherCode : ""),
                ItemId = first?.ItemId ?? 0,
                ItemCode = itemSummary,
                ItemName = details.Count == 1 ? first?.Item?.ItemName ?? "" : $"{details.Select(d => d.ItemId).Distinct().Count()} loại vật tư",
                LocationCode = ((l.CurrentLocation != null) ? l.CurrentLocation.LocationCode : null),
                Quantity = details.Sum(d => d.Quantity),
                LotNumber = string.IsNullOrWhiteSpace(lotSummary) ? null : lotSummary,
                ExpiryDate = details.Select(d => d.ExpiryDate).Where(d => d.HasValue).OrderBy(d => d).FirstOrDefault(),
                IsActive = l.IsActive,
                Status = l.Status,
                LpnType = l.LpnType,
                DetailCount = details.Count,
                CreatedAt = l.CreatedAt
            };
        }).ToList();
        base.ViewBag.Warehouses = (await (from w in _db.Warehouses.AsNoTracking()
                                          where w.IsActive
                                          orderby w.WarehouseCode
                                          select w).ToListAsync());
        base.ViewBag.WarehouseId = warehouseId;
        base.ViewBag.Search = search;
        base.ViewBag.ActiveOnly = activeOnly;
        return View(rows);
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> PackageLookup(int? warehouseId, string? search)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        IQueryable<OutboundPackage> query = _db.OutboundPackages.AsNoTracking().Include((OutboundPackage p) => p.Voucher).ThenInclude((Voucher v) => v.Partner)
            .Include((OutboundPackage p) => p.Warehouse)
            .Include((OutboundPackage p) => p.ShipmentLoad)
            .AsQueryable();
        if (warehouseId.HasValue)
        {
            query = query.Where((OutboundPackage p) => p.WarehouseId == ((int?)warehouseId).Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();
            query = query.Where((OutboundPackage p) => p.PackageCode.Contains(keyword) || (p.Voucher != null && p.Voucher.VoucherCode.Contains(keyword)) || (p.TrackingNumber != null && p.TrackingNumber.Contains(keyword)) || (p.ManifestCode != null && p.ManifestCode.Contains(keyword)) || (p.ReferenceLpnCode != null && p.ReferenceLpnCode.Contains(keyword)) || (p.ShipmentLoad != null && p.ShipmentLoad.LoadCode.Contains(keyword)) || p.PackedBy.Contains(keyword));
        }
        List<OutboundPackageLookupRow> rows = await (from p in query.OrderByDescending((OutboundPackage p) => p.PackedAt).Take(500)
                                                     select new OutboundPackageLookupRow
                                                     {
                                                         OutboundPackageId = p.OutboundPackageId,
                                                         PackageCode = p.PackageCode,
                                                         VoucherId = p.VoucherId,
                                                         VoucherCode = ((p.Voucher != null) ? p.Voucher.VoucherCode : ""),
                                                         WarehouseId = p.WarehouseId,
                                                         WarehouseName = ((p.Warehouse != null) ? p.Warehouse.WarehouseName : ""),
                                                         PartnerName = ((p.Voucher != null && p.Voucher.Partner != null) ? p.Voucher.Partner.PartnerName : null),
                                                         SourceType = p.SourceType,
                                                         PackageType = p.PackageType,
                                                         ReferenceLpnCode = p.ReferenceLpnCode,
                                                         TotalQuantity = p.TotalQuantity,
                                                         ItemCount = p.ItemCount,
                                                         PackedBy = p.PackedBy,
                                                         PackedAt = p.PackedAt,
                                                         TrackingNumber = p.TrackingNumber,
                                                         ManifestCode = p.ManifestCode,
                                                         ActualCatchWeight = p.ActualCatchWeight,
                                                         LoadCode = p.ShipmentLoad != null ? p.ShipmentLoad.LoadCode : null,
                                                         LoadStatus = p.ShipmentLoad != null ? p.ShipmentLoad.Status : null,
                                                         Notes = p.Notes
                                                     }).ToListAsync();
        List<long> packageIds = rows.Select((OutboundPackageLookupRow r) => r.OutboundPackageId).ToList();
        Dictionary<long, CarrierShipment> carrierByPackage = (await _db.CarrierShipments.AsNoTracking()
            .Where((CarrierShipment s) => packageIds.Contains(s.OutboundPackageId))
            .OrderByDescending((CarrierShipment s) => s.CreatedAt)
            .ToListAsync())
            .GroupBy((CarrierShipment s) => s.OutboundPackageId)
            .ToDictionary(
                (IGrouping<long, CarrierShipment> g) => g.Key,
                (IGrouping<long, CarrierShipment> g) => g.Where((CarrierShipment s) => s.Status != CarrierShipmentStatusEnum.Cancelled).OrderByDescending((CarrierShipment s) => s.CreatedAt).FirstOrDefault()
                    ?? g.OrderByDescending((CarrierShipment s) => s.CreatedAt).First());
        foreach (OutboundPackageLookupRow row in rows)
        {
            if (carrierByPackage.TryGetValue(row.OutboundPackageId, out CarrierShipment? shipment))
            {
                row.CarrierName = shipment.CarrierNameSnapshot;
                row.CarrierStatus = shipment.Status;
                row.CarrierTrackingNumber = shipment.TrackingNumber;
                row.CarrierLastError = shipment.LastError;
            }
        }
        base.ViewBag.Warehouses = (await (from w in _db.Warehouses.AsNoTracking()
                                          where w.IsActive
                                          orderby w.WarehouseCode
                                          select w).ToListAsync());
        base.ViewBag.WarehouseId = warehouseId;
        base.ViewBag.Search = search;
        return View(rows);
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> Replenishment(int? warehouseId, string? search)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        List<ReplenishmentSuggestionRow> rows = await BuildReplenishmentSuggestionsAsync(warehouseId, search);
        base.ViewBag.Warehouses = (await (from w in _db.Warehouses.AsNoTracking()
                                          where w.IsActive
                                          orderby w.WarehouseCode
                                          select w).ToListAsync());
        base.ViewBag.WarehouseId = warehouseId;
        base.ViewBag.Search = search;
        return View(rows);
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> Slotting(int? warehouseId, string? search)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        List<SlottingSuggestionRow> rows = await BuildSlottingSuggestionsAsync(warehouseId, search);
        base.ViewBag.Warehouses = (await (from w in _db.Warehouses.AsNoTracking()
                                          where w.IsActive
                                          orderby w.WarehouseCode
                                          select w).ToListAsync());
        base.ViewBag.WarehouseId = warehouseId;
        base.ViewBag.Search = search;
        return View(rows);
    }


    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> SlottingSimulation(int? warehouseId, string? search, int? scenarioId)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        List<Warehouse> warehouses = await (from w in _db.Warehouses.AsNoTracking()
                                            where w.IsActive
                                            orderby w.WarehouseCode
                                            select w).ToListAsync();
        IQueryable<SlottingSimulationScenario> scenariosQuery = from s in _db.SlottingSimulationScenarios.AsNoTracking().Include((SlottingSimulationScenario s) => s.Lines).Include((SlottingSimulationScenario s) => s.Warehouse)
                                                                where s.IsActive
                                                                select s;
        if (warehouseId.HasValue)
        {
            scenariosQuery = scenariosQuery.Where((SlottingSimulationScenario s) => s.WarehouseId == ((int?)warehouseId).Value);
        }
        List<SlottingSimulationScenario> scenarios = await scenariosQuery.OrderByDescending((SlottingSimulationScenario s) => s.CreatedAt).Take(20).ToListAsync();
        List<SlottingSimulationLine> previewLines = new List<SlottingSimulationLine>();
        if (scenarioId.HasValue)
        {
            SlottingSimulationScenario? selected = await _db.SlottingSimulationScenarios.AsNoTracking().Include((SlottingSimulationScenario s) => s.Lines).ThenInclude((SlottingSimulationLine l) => l.Item)
                .Include((SlottingSimulationScenario s) => s.Lines)
                .ThenInclude((SlottingSimulationLine l) => l.SourceLocation)
                .Include((SlottingSimulationScenario s) => s.Lines)
                .ThenInclude((SlottingSimulationLine l) => l.SuggestedLocation)
                .Include((SlottingSimulationScenario s) => s.Lines)
                .ThenInclude((SlottingSimulationLine l) => l.MovementTask)
                .FirstOrDefaultAsync((SlottingSimulationScenario s) => s.ScenarioId == ((int?)scenarioId).Value && s.IsActive);
            if (selected != null)
            {
                if (scopedWh.HasValue && selected.WarehouseId != scopedWh.Value)
                {
                    return Forbid();
                }
                warehouseId = selected.WarehouseId;
                previewLines = selected.Lines.OrderByDescending((SlottingSimulationLine l) => l.NetEstimatedMinutesSaved).ToList();
                base.ViewBag.SelectedScenario = selected;
            }
        }
        int num = (warehouseId.HasValue ? (await BuildSlottingSuggestionsAsync(warehouseId, search)).Count : 0);
        int currentSuggestionCount = num;
        return View(new SlottingSimulationPageViewModel
        {
            WarehouseId = warehouseId,
            Search = search,
            Warehouses = warehouses,
            Scenarios = scenarios,
            PreviewLines = previewLines,
            CurrentSuggestionCount = currentSuggestionCount
        });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSlottingSimulation(CreateSlottingSimulationRequest request)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            request.WarehouseId = scopedWh.Value;
        }
        if (request.WarehouseId <= 0)
        {
            base.TempData["Error"] = "Vui lòng chọn kho trước khi tạo mô phỏng.";
            return RedirectToAction("SlottingSimulation");
        }
        string scenarioName = (string.IsNullOrWhiteSpace(request.ScenarioName) ? $"Mô phỏng định vị lưu kho {VietnamNow:yyyyMMdd-HHmm}" : request.ScenarioName.Trim());
        List<SlottingSuggestionRow> suggestions = (from s in await BuildSlottingSuggestionsAsync(request.WarehouseId, request.Search)
                                                   where s.CanApply
                                                   orderby s.SlottingScore descending
                                                   select s).Take(Math.Clamp((request.MaxLines <= 0) ? 50 : request.MaxLines, 1, 200)).ToList();
        if (!suggestions.Any())
        {
            base.TempData["Error"] = "Không có slotting suggestion hợp lệ để tạo mô phỏng.";
            return RedirectToAction("SlottingSimulation", new
            {
                warehouseId = request.WarehouseId,
                search = request.Search
            });
        }
        SlottingSimulationScenario slottingSimulationScenario = new SlottingSimulationScenario();
        SlottingSimulationScenario slottingSimulationScenario2 = slottingSimulationScenario;
        slottingSimulationScenario2.ScenarioCode = await _slottingPlanningService.GenerateScenarioCodeAsync();
        slottingSimulationScenario.ScenarioName = scenarioName;
        slottingSimulationScenario.WarehouseId = request.WarehouseId;
        slottingSimulationScenario.CreatedBy = base.User.Identity?.Name ?? "system";
        slottingSimulationScenario.CreatedAt = VietnamNow;
        slottingSimulationScenario.Status = SlottingSimulationStatusEnum.Draft;
        slottingSimulationScenario.Notes = request.Search;
        SlottingSimulationScenario scenario = slottingSimulationScenario;
        foreach (SlottingSuggestionRow suggestion in suggestions)
        {
            SlottingSimulationLine? line = await _slottingPlanningService.BuildSimulationLineAsync(scenario, suggestion, request.WarehouseId);
            if (line != null)
            {
                scenario.Lines.Add(line);
            }
        }
        if (!scenario.Lines.Any())
        {
            base.TempData["Error"] = "Không tìm thấy tồn nguồn hợp lệ bên ngoài vị trí đề xuất để mô phỏng di chuyển.";
            return RedirectToAction("SlottingSimulation", new
            {
                warehouseId = request.WarehouseId,
                search = request.Search
            });
        }
        scenario.LineCount = scenario.Lines.Count;
        scenario.TotalEstimatedTravelMinutesSaved = scenario.Lines.Sum((SlottingSimulationLine l) => l.EstimatedTravelMinutesSaved);
        scenario.TotalMovementCostMinutes = scenario.Lines.Sum((SlottingSimulationLine l) => l.MovementCostMinutes);
        scenario.NetEstimatedMinutesSaved = scenario.Lines.Sum((SlottingSimulationLine l) => l.NetEstimatedMinutesSaved);
        scenario.ResultJson = JsonSerializer.Serialize(new
        {
            horizonDays = 30,
            lineCount = scenario.LineCount,
            totalTravelSaved = scenario.TotalEstimatedTravelMinutesSaved,
            movementCost = scenario.TotalMovementCostMinutes,
            netSaving = scenario.NetEstimatedMinutesSaved
        });
        _db.SlottingSimulationScenarios.Add(scenario);
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = $"Đã tạo mô phỏng định vị lưu kho [{scenario.ScenarioCode}] với {scenario.LineCount} dòng.";
        return RedirectToAction("SlottingSimulation", new
        {
            warehouseId = request.WarehouseId,
            search = request.Search,
            scenarioId = scenario.ScenarioId
        });
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveSlottingSimulation(int scenarioId)
    {
        SlottingSimulationScenario? scenario = await _db.SlottingSimulationScenarios.Include((SlottingSimulationScenario s) => s.Lines).FirstOrDefaultAsync((SlottingSimulationScenario s) => s.ScenarioId == scenarioId && s.IsActive);
        if (scenario == null)
        {
            return NotFound();
        }
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && scenario.WarehouseId != scopedWh.Value)
        {
            return Forbid();
        }
        if (scenario.Status != SlottingSimulationStatusEnum.Draft)
        {
            base.TempData["Error"] = "Scenario này đã được xử lý, không thể approve lại.";
            return RedirectToAction("SlottingSimulation", new
            {
                warehouseId = scenario.WarehouseId,
                scenarioId = scenarioId
            });
        }
        int created = 0;
        foreach (SlottingSimulationLine line in from l in scenario.Lines
                                                where l.Status == SlottingSimulationLineStatusEnum.Draft
                                                orderby l.NetEstimatedMinutesSaved descending
                                                select l)
        {
            if (line.NetEstimatedMinutesSaved <= 0m)
            {
                line.Status = SlottingSimulationLineStatusEnum.Skipped;
                line.ErrorMessage = "Mức tiết kiệm ròng không dương, bỏ qua để tránh tái phân kệ không hiệu quả.";
                continue;
            }
            try
            {
                line.MovementTaskId = (await _movementTaskService.CreateMovementTaskAsync(new MovementTaskCreateRequest
                {
                    WarehouseId = scenario.WarehouseId,
                    ItemId = line.ItemId,
                    SourceLocationId = line.SourceLocationId,
                    DestinationLocationId = line.SuggestedLocationId,
                    SourceItemLocationId = line.SourceItemLocationId,
                    TaskType = MovementTaskTypeEnum.Reslotting,
                    Priority = ((line.NetEstimatedMinutesSaved >= 60m) ? MovementTaskPriorityEnum.Urgent : MovementTaskPriorityEnum.High),
                    PlannedQty = line.PlannedMoveQty,
                    PreviousDefaultLocationId = line.CurrentDefaultLocationId,
                    UpdateDefaultLocationOnComplete = true,
                    SourceModule = "SlottingSimulation",
                    SourceReference = scenario.ScenarioCode,
                    SourceReason = $"Mô phỏng định vị lưu kho approved. Net saving {line.NetEstimatedMinutesSaved:N2} phút.",
                    Notes = line.Reason
                }, scopedWh, base.User.Identity?.Name ?? "system")).MovementTaskId;
                line.Status = SlottingSimulationLineStatusEnum.TaskCreated;
                line.ErrorMessage = null;
                created++;
            }
            catch (Exception ex) when (((ex is BusinessRuleException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
            {
                line.Status = SlottingSimulationLineStatusEnum.Failed;
                line.ErrorMessage = UserSafeError.From(ex);
            }
        }
        scenario.ApprovedTaskCount = created;
        scenario.ApprovedBy = base.User.Identity?.Name ?? "system";
        scenario.ApprovedAt = VietnamNow;
        scenario.Status = ((created == scenario.LineCount) ? SlottingSimulationStatusEnum.Approved : ((created > 0) ? SlottingSimulationStatusEnum.PartiallyApproved : SlottingSimulationStatusEnum.Failed));
        _db.AuditLogs.Add(new AuditLog
        {
            TableName = "SlottingSimulationScenario",
            RecordId = scenario.ScenarioId.ToString(),
            ActionType = "APPROVE",
            ColumnChanged = "Status",
            OldValue = SlottingSimulationStatusEnum.Draft.ToString(),
            NewValue = scenario.Status.ToString(),
            ChangedBy = (base.User.Identity?.Name ?? "system"),
            ChangedAt = VietnamNow,
            AppModule = "SlottingSimulation"
        });
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = $"Đã duyệt kịch bản {scenario.ScenarioCode}: tạo {created}/{scenario.LineCount} nhiệm vụ điều chuyển.";
        return RedirectToAction("SlottingSimulation", new
        {
            warehouseId = scenario.WarehouseId,
            scenarioId = scenarioId
        });
    }

}
