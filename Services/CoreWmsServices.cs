using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public sealed class PutawayPlanRequest
{
    public int ItemId { get; init; }
    public int WarehouseId { get; init; }
    public int? OwnerPartnerId { get; init; }
    public decimal Quantity { get; init; } = 1m;
    public string? LotNumber { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public int RowIndex { get; init; }
}

public sealed class PutawayPlanSuggestion
{
    public int ItemId { get; init; }
    public int RowIndex { get; init; }
    public int? LocationId { get; init; }
    public string LocationCode { get; init; } = "";
    public string ZoneName { get; init; } = "";
    public string Strategy { get; init; } = "";
    public string Reason { get; init; } = "";
    public decimal Score { get; init; }
    public bool RequiresOverrideReason { get; init; }
}

public interface IDirectedPutawayService
{
    Task<List<PutawayPlanSuggestion>> SuggestAsync(IReadOnlyCollection<PutawayPlanRequest> requests, CancellationToken ct = default);
    Task<AuditLog> RecordOverrideAsync(PutawayPlanSuggestion suggestion, int chosenLocationId, string overrideReason, string actor, CancellationToken ct = default);
}

public sealed class DirectedPutawayService : IDirectedPutawayService
{
    private readonly AppDbContext _db;

    public DirectedPutawayService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<PutawayPlanSuggestion>> SuggestAsync(IReadOnlyCollection<PutawayPlanRequest> requests, CancellationToken ct = default)
    {
        if (requests.Count == 0)
            return new List<PutawayPlanSuggestion>();

        var warehouseIds = requests.Select(r => r.WarehouseId).Distinct().ToList();
        if (warehouseIds.Count != 1 || warehouseIds[0] <= 0)
            throw new BusinessRuleException("Directed putaway requires exactly one warehouse.", "PUTAWAY_WAREHOUSE_REQUIRED", "Warehouse");

        var warehouseId = warehouseIds[0];
        var itemIds = requests.Select(r => r.ItemId).Distinct().ToList();
        var items = await _db.Items
            .AsNoTracking()
            .Include(i => i.DefaultLocation)!.ThenInclude(l => l!.Zone)
            .Where(i => itemIds.Contains(i.ItemId) && i.IsActive)
            .ToDictionaryAsync(i => i.ItemId, ct);

        var locations = await _db.Locations
            .AsNoTracking()
            .Include(l => l.Zone)
            .Where(l => l.IsActive
                && l.Zone != null
                && l.Zone.IsActive
                && l.Zone.WarehouseId == warehouseId
                && l.Zone.ZoneType != ZoneTypeEnum.Shipping
                && l.Zone.ZoneType != ZoneTypeEnum.Receiving)
            .OrderBy(l => l.Zone.ZoneCode)
            .ThenBy(l => l.LocationCode)
            .ToListAsync(ct);

        var locationIds = locations.Select(l => l.LocationId).ToList();
        var stockRows = await _db.ItemLocations
            .AsNoTracking()
            .Include(il => il.Item)
            .Include(il => il.Location)!.ThenInclude(l => l!.Zone)
            .Where(il => locationIds.Contains(il.LocationId) && il.Quantity > 0)
            .ToListAsync(ct);

        var loadByLocation = stockRows
            .GroupBy(il => il.LocationId)
            .ToDictionary(g => g.Key, g => g.Sum(EstimateLoad));

        var assigned = new HashSet<int>();
        var plannedLoad = new Dictionary<int, decimal>();
        var result = new List<PutawayPlanSuggestion>();

        foreach (var request in requests.OrderBy(r => r.RowIndex))
        {
            if (!items.TryGetValue(request.ItemId, out var item))
            {
                result.Add(new PutawayPlanSuggestion
                {
                    ItemId = request.ItemId,
                    RowIndex = request.RowIndex,
                    Strategy = "Item not found",
                    Reason = "Item is inactive or missing."
                });
                continue;
            }

            var candidate = locations
                .Select(location => ScoreCandidate(request, item, location, stockRows, loadByLocation, plannedLoad, assigned))
                .Where(x => x != null)
                .Select(x => x!)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Location.LocationCode)
                .FirstOrDefault();

            if (candidate == null)
            {
                result.Add(new PutawayPlanSuggestion
                {
                    ItemId = request.ItemId,
                    RowIndex = request.RowIndex,
                    Strategy = "No eligible location",
                    Reason = "All active locations failed zone, owner, lot, capacity, temperature or hazmat rules."
                });
                continue;
            }

            assigned.Add(candidate.Location.LocationId);
            plannedLoad[candidate.Location.LocationId] = plannedLoad.GetValueOrDefault(candidate.Location.LocationId) + EstimateRequestLoad(item, request.Quantity);

            result.Add(new PutawayPlanSuggestion
            {
                ItemId = request.ItemId,
                RowIndex = request.RowIndex,
                LocationId = candidate.Location.LocationId,
                LocationCode = candidate.Location.LocationCode,
                ZoneName = candidate.Location.Zone?.ZoneName ?? "",
                Strategy = candidate.Strategy,
                Reason = candidate.Reason,
                Score = candidate.Score,
                RequiresOverrideReason = true
            });
        }

        return result;
    }

    public async Task<AuditLog> RecordOverrideAsync(PutawayPlanSuggestion suggestion, int chosenLocationId, string overrideReason, string actor, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(overrideReason))
            throw new BusinessRuleException("Putaway override reason is required.", "PUTAWAY_OVERRIDE_REASON_REQUIRED", "AuditLog");

        var audit = new AuditLog
        {
            TableName = "PutawaySuggestion",
            RecordId = $"{suggestion.ItemId}:{suggestion.RowIndex}",
            ActionType = "OVERRIDE",
            ColumnChanged = "LocationId",
            OldValue = suggestion.LocationId?.ToString(),
            NewValue = $"Chosen:{chosenLocationId};Reason:{overrideReason.Trim()}",
            ChangedBy = actor,
            ChangedAt = VietnamTime.Now,
            AppModule = "DirectedPutaway"
        };
        _db.AuditLogs.Add(audit);
        await _db.SaveChangesAsync(ct);
        return audit;
    }

    private sealed class PutawayCandidate
    {
        public required Location Location { get; init; }
        public required string Strategy { get; init; }
        public required string Reason { get; init; }
        public decimal Score { get; init; }
    }

    private static PutawayCandidate? ScoreCandidate(
        PutawayPlanRequest request,
        Item item,
        Location location,
        List<ItemLocation> stockRows,
        IReadOnlyDictionary<int, decimal> loadByLocation,
        IReadOnlyDictionary<int, decimal> plannedLoad,
        HashSet<int> assigned)
    {
        if (assigned.Contains(location.LocationId))
            return null;
        if (location.Zone == null || !IsAllowedZone(item, location.Zone, stockRows.Select(x => x.Location?.Zone).Where(x => x != null).Cast<Zone>()))
            return null;
        if (!OwnerAndMixingAllowed(request, item, location, stockRows))
            return null;
        if (!FitsCapacity(request, item, location, loadByLocation, plannedLoad))
            return null;

        var sameLocationRows = stockRows
            .Where(x => x.LocationId == location.LocationId && x.ItemId == item.ItemId && x.OwnerPartnerId == (request.OwnerPartnerId ?? item.OwnerPartnerId))
            .ToList();
        var exactLot = sameLocationRows.Any(x =>
            (!string.IsNullOrWhiteSpace(request.LotNumber) && string.Equals(x.LotNumber, request.LotNumber, StringComparison.OrdinalIgnoreCase))
            || (request.ExpiryDate.HasValue && x.ExpiryDate?.Date == request.ExpiryDate.Value.Date));
        var sameItem = sameLocationRows.Count > 0;
        var empty = !stockRows.Any(x => x.LocationId == location.LocationId && x.Quantity > 0);
        var sameZoneAsDefault = item.DefaultLocation?.ZoneId == location.ZoneId;
        var isDefault = item.DefaultLocationId == location.LocationId;

        var score = 0m;
        var strategy = "Available bin";
        var reason = "Eligible active location.";

        if (exactLot)
        {
            score += 1000m;
            strategy = "Consolidate same lot / expiry";
            reason = "Same item, owner, lot or expiry keeps traceability tight.";
        }
        else if (sameItem)
        {
            score += 900m;
            strategy = "Consolidate same item";
            reason = "Same item and owner already exist in this bin.";
        }
        else if (isDefault)
        {
            score += 850m;
            strategy = "Fixed bin";
            reason = "Item default location is available and passes capacity rules.";
        }
        else if (empty && sameZoneAsDefault)
        {
            score += 700m;
            strategy = "Empty bin in default zone";
            reason = "Keeps inventory near the configured default zone.";
        }
        else if (empty)
        {
            score += 600m;
            strategy = "Empty eligible bin";
            reason = "Clean empty location that passes operational rules.";
        }

        var abc = ResolveAbc(item);
        if (abc == 'A' && location.IsGoldenZone)
            score += 180m;
        else if (abc == 'B' && location.HeightLevel <= 3)
            score += 80m;

        if (IsHazmat(item) && ZoneContains(location.Zone, "HAZ", "CHEM"))
            score += 140m;
        if (RequiresTemperatureZone(item) && ZoneContains(location.Zone, "COLD", "CHILL", "FREEZE", "TEMP"))
            score += 140m;

        score += CapacityFillScore(request, item, location, loadByLocation, plannedLoad);
        return new PutawayCandidate { Location = location, Strategy = strategy, Reason = reason, Score = score };
    }

    private static bool OwnerAndMixingAllowed(PutawayPlanRequest request, Item item, Location location, List<ItemLocation> stockRows)
    {
        var owner = request.OwnerPartnerId ?? item.OwnerPartnerId;
        var existing = stockRows.Where(x => x.LocationId == location.LocationId && x.Quantity > 0).ToList();
        if (existing.Count == 0)
            return true;

        if (owner.HasValue && existing.Any(x => x.OwnerPartnerId.HasValue && x.OwnerPartnerId != owner))
            return false;

        if (!location.AllowMixedSku && existing.Any(x => x.ItemId != item.ItemId))
            return false;

        return true;
    }

    private static bool FitsCapacity(
        PutawayPlanRequest request,
        Item item,
        Location location,
        IReadOnlyDictionary<int, decimal> loadByLocation,
        IReadOnlyDictionary<int, decimal> plannedLoad)
    {
        var current = Math.Max(location.CurrentLoad, loadByLocation.GetValueOrDefault(location.LocationId));
        var projected = current + plannedLoad.GetValueOrDefault(location.LocationId) + EstimateRequestLoad(item, request.Quantity);
        var max = item.ItemType == ItemTypeEnum.HoaChat
            ? location.MaxCapacity
            : location.MaxWeightCapacityKg ?? location.MaxCapacity;
        return max <= 0m || projected <= max;
    }

    private static decimal CapacityFillScore(
        PutawayPlanRequest request,
        Item item,
        Location location,
        IReadOnlyDictionary<int, decimal> loadByLocation,
        IReadOnlyDictionary<int, decimal> plannedLoad)
    {
        var max = item.ItemType == ItemTypeEnum.HoaChat
            ? location.MaxCapacity
            : location.MaxWeightCapacityKg ?? location.MaxCapacity;
        if (max <= 0m)
            return 0m;

        var current = Math.Max(location.CurrentLoad, loadByLocation.GetValueOrDefault(location.LocationId));
        var projectedFill = (current + plannedLoad.GetValueOrDefault(location.LocationId) + EstimateRequestLoad(item, request.Quantity)) / max;
        return Math.Max(0m, 100m - Math.Abs(0.65m - projectedFill) * 100m);
    }

    private static bool IsAllowedZone(Item item, Zone zone, IEnumerable<Zone> allZones)
    {
        if (!string.IsNullOrWhiteSpace(item.AllowedZoneTypes))
        {
            var allowed = item.AllowedZoneTypes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => byte.TryParse(x, out _))
                .Select(byte.Parse)
                .ToHashSet();
            if (allowed.Count > 0 && !allowed.Contains((byte)zone.ZoneType))
                return false;
        }

        if (IsHazmat(item) && allZones.Any(z => ZoneContains(z, "HAZ", "CHEM")))
            return ZoneContains(zone, "HAZ", "CHEM");

        if (RequiresTemperatureZone(item) && allZones.Any(z => ZoneContains(z, "COLD", "CHILL", "FREEZE", "TEMP")))
            return ZoneContains(zone, "COLD", "CHILL", "FREEZE", "TEMP");

        return zone.ZoneType is ZoneTypeEnum.Storage or ZoneTypeEnum.Staging or ZoneTypeEnum.CrossDock;
    }

    private static bool ZoneContains(Zone? zone, params string[] tokens)
    {
        var text = $"{zone?.ZoneCode} {zone?.ZoneName}".ToUpperInvariant();
        return tokens.Any(text.Contains);
    }

    private static bool IsHazmat(Item item)
        => item.ItemType == ItemTypeEnum.HoaChat
            || ContainsAny(item.Specifications, "HAZMAT", "CHEM", "DANGEROUS")
            || ContainsAny(item.Description, "HAZMAT", "CHEM", "DANGEROUS");

    private static bool RequiresTemperatureZone(Item item)
        => ContainsAny(item.Specifications, "COLD", "CHILL", "FREEZE", "TEMP")
            || ContainsAny(item.Description, "COLD", "CHILL", "FREEZE", "TEMP");

    private static bool ContainsAny(string? value, params string[] tokens)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        var upper = value.ToUpperInvariant();
        return tokens.Any(upper.Contains);
    }

    private static char ResolveAbc(Item item)
    {
        if (string.IsNullOrWhiteSpace(item.AbcClass))
            return 'C';
        var c = char.ToUpperInvariant(item.AbcClass[0]);
        return c is 'A' or 'B' or 'C' ? c : 'C';
    }

    private static decimal EstimateRequestLoad(Item item, decimal quantity)
        => item.ItemType == ItemTypeEnum.HoaChat ? quantity : quantity * (item.Weight ?? 1m);

    private static decimal EstimateLoad(ItemLocation row)
        => row.Item?.ItemType == ItemTypeEnum.HoaChat ? row.Quantity : row.Quantity * (row.Item?.Weight ?? 1m);
}

public static class InventoryStatusEngine
{
    public static bool IsAvailableForAllocation(InventoryHoldStatusEnum status)
        => status is InventoryHoldStatusEnum.Available or InventoryHoldStatusEnum.Consigned;

    public static bool IsUnavailable(InventoryHoldStatusEnum status)
        => !IsAvailableForAllocation(status);

    public static InventoryHoldStatusEnum FromQualityStatus(QualityStatusEnum status)
        => status switch
        {
            QualityStatusEnum.Good or QualityStatusEnum.Passed => InventoryHoldStatusEnum.Available,
            QualityStatusEnum.Pending or QualityStatusEnum.Inspecting => InventoryHoldStatusEnum.QcHold,
            QualityStatusEnum.Quarantine => InventoryHoldStatusEnum.Quarantine,
            QualityStatusEnum.Defect or QualityStatusEnum.Failed => InventoryHoldStatusEnum.Damaged,
            QualityStatusEnum.OnHold => InventoryHoldStatusEnum.Blocked,
            _ => InventoryHoldStatusEnum.Blocked
        };

    public static void EnsurePostingAllowed(InventoryTransactionTypeEnum transactionType, InventoryHoldStatusEnum status)
    {
        var requiresAvailable = transactionType is InventoryTransactionTypeEnum.Pick
            or InventoryTransactionTypeEnum.Pack
            or InventoryTransactionTypeEnum.Ship
            or InventoryTransactionTypeEnum.TransferOut
            or InventoryTransactionTypeEnum.KitConsume
            or InventoryTransactionTypeEnum.VasConsume;

        if (requiresAvailable && !IsAvailableForAllocation(status))
            throw new BusinessRuleException($"Inventory status {status} is not available for {transactionType}.", "INVENTORY_STATUS_BLOCKED", "ItemLocation");
    }

    public static (decimal AvailableQty, decimal UnavailableQty) SplitAvailability(IEnumerable<ItemLocation> rows)
    {
        var available = 0m;
        var unavailable = 0m;
        foreach (var row in rows)
        {
            var qty = Math.Max(0m, row.Quantity - row.ReservedQty);
            if (IsAvailableForAllocation(row.HoldStatus))
                available += qty;
            else
                unavailable += qty;
        }

        return (available, unavailable);
    }
}

public sealed class AllocationRequest
{
    public int ItemId { get; init; }
    public int WarehouseId { get; init; }
    public int? OwnerPartnerId { get; init; }
    public decimal RequiredQty { get; init; }
    public AllocationStrategyEnum Strategy { get; init; } = AllocationStrategyEnum.Fefo;
    public bool AllowPartial { get; init; }
    public string? LotNumber { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public IReadOnlyCollection<int>? ZoneIds { get; init; }
    public IReadOnlyCollection<int>? ExcludedLocationIds { get; init; }
}

public sealed record AllocationSlice(int ItemLocationId, int LocationId, string? LotNumber, DateTime? ExpiryDate, decimal Qty);

public sealed class AllocationPlan
{
    public decimal RequestedQty { get; init; }
    public decimal AllocatedQty { get; init; }
    public decimal ShortQty => Math.Max(0m, RequestedQty - AllocatedQty);
    public bool IsComplete => ShortQty <= 0m;
    public List<AllocationSlice> Slices { get; init; } = new();
}

public interface IAdvancedAllocationService
{
    Task<AllocationPlan> AllocateAsync(AllocationRequest request, CancellationToken ct = default);
}

public sealed class AdvancedAllocationService : IAdvancedAllocationService
{
    private readonly AppDbContext _db;

    public AdvancedAllocationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AllocationPlan> AllocateAsync(AllocationRequest request, CancellationToken ct = default)
    {
        if (request.RequiredQty <= 0m)
            return new AllocationPlan { RequestedQty = request.RequiredQty };

        var item = await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.ItemId == request.ItemId && i.IsActive, ct);
        if (item == null)
            throw new BusinessRuleException("Allocation item is inactive or missing.", "ALLOCATION_ITEM_NOT_FOUND", "Item");

        var excluded = (request.ExcludedLocationIds ?? Array.Empty<int>()).ToHashSet();
        var zoneIds = (request.ZoneIds ?? Array.Empty<int>()).ToHashSet();
        var rows = await _db.ItemLocations
            .AsNoTracking()
            .Include(il => il.Location)!.ThenInclude(l => l!.Zone)
            .Where(il => il.ItemId == request.ItemId
                && il.OwnerPartnerId == request.OwnerPartnerId
                && !excluded.Contains(il.LocationId)
                && il.Quantity > il.ReservedQty
                && (request.LotNumber == null || il.LotNumber == request.LotNumber)
                && (!request.ExpiryDate.HasValue || il.ExpiryDate == request.ExpiryDate)
                && il.Location != null
                && il.Location.IsActive
                && il.Location.Zone != null
                && il.Location.Zone.WarehouseId == request.WarehouseId
                && (zoneIds.Count == 0 || zoneIds.Contains(il.Location.ZoneId)))
            .ToListAsync(ct);

        rows = rows
            .Where(il => InventoryStatusEngine.IsAvailableForAllocation(il.HoldStatus))
            .ToList();

        if (item.TrackSerial)
            rows = await CapRowsByAvailableSerialsAsync(rows, request.OwnerPartnerId, ct);

        IOrderedEnumerable<ItemLocation> ordered = request.Strategy switch
        {
            AllocationStrategyEnum.Fifo => rows.OrderBy(x => x.UpdatedAt).ThenBy(x => x.ItemLocationId),
            AllocationStrategyEnum.Lifo => rows.OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.ItemLocationId),
            _ => rows.OrderBy(x => x.ExpiryDate.HasValue ? 0 : 1).ThenBy(x => x.ExpiryDate).ThenBy(x => x.UpdatedAt).ThenBy(x => x.ItemLocationId)
        };

        var remaining = request.RequiredQty;
        var slices = new List<AllocationSlice>();
        foreach (var row in ordered)
        {
            InventoryStatusEngine.EnsurePostingAllowed(InventoryTransactionTypeEnum.Pick, row.HoldStatus);
            var available = Math.Max(0m, row.Quantity - row.ReservedQty);
            if (available <= 0m)
                continue;

            var take = Math.Min(remaining, available);
            slices.Add(new AllocationSlice(row.ItemLocationId, row.LocationId, row.LotNumber, row.ExpiryDate, take));
            remaining -= take;
            if (remaining <= 0m)
                break;
        }

        var allocated = slices.Sum(s => s.Qty);
        if (allocated < request.RequiredQty && !request.AllowPartial)
            throw new BusinessRuleException("Available inventory is not enough for full allocation.", "ALLOCATION_INSUFFICIENT_STOCK", "ItemLocation");

        return new AllocationPlan
        {
            RequestedQty = request.RequiredQty,
            AllocatedQty = allocated,
            Slices = slices
        };
    }

    private async Task<List<ItemLocation>> CapRowsByAvailableSerialsAsync(List<ItemLocation> rows, int? ownerPartnerId, CancellationToken ct)
    {
        if (rows.Count == 0)
            return rows;

        var itemIds = rows.Select(x => x.ItemId).Distinct().ToList();
        var locationIds = rows.Select(x => x.LocationId).Distinct().ToList();
        var serials = await _db.SerialNumbers
            .AsNoTracking()
            .Where(s => itemIds.Contains(s.ItemId)
                && s.LocationId.HasValue
                && locationIds.Contains(s.LocationId.Value)
                && s.OwnerPartnerId == ownerPartnerId
                && s.HoldStatus == InventoryHoldStatusEnum.Available
                && (s.Status == SerialNumberStatusEnum.Active || s.Status == SerialNumberStatusEnum.Available))
            .GroupBy(s => new { s.ItemId, LocationId = s.LocationId!.Value, s.LotNumber, s.ExpiryDate })
            .Select(g => new { g.Key.ItemId, g.Key.LocationId, g.Key.LotNumber, g.Key.ExpiryDate, Count = g.Count() })
            .ToListAsync(ct);

        var counts = serials.ToDictionary(x => (x.ItemId, x.LocationId, x.LotNumber, x.ExpiryDate), x => x.Count);
        foreach (var row in rows)
        {
            var availableSerials = counts.GetValueOrDefault((row.ItemId, row.LocationId, row.LotNumber, row.ExpiryDate));
            row.Quantity = Math.Min(row.Quantity, availableSerials);
            row.ReservedQty = 0m;
        }

        return rows.Where(x => x.Quantity > 0m).ToList();
    }
}

public interface ICycleCountPlanningService
{
    Task<int> CreateOrRefreshSchedulesAsync(int programId, IReadOnlyCollection<int>? zoneIds, CancellationToken ct = default);
    Task<StockCountSheet> GenerateDueSheetAsync(int programId, string actor, int maxLines = 50, CancellationToken ct = default);
}

public sealed class CycleCountPlanningService : ICycleCountPlanningService
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public CycleCountPlanningService(AppDbContext db, IUnitOfWork unitOfWork)
    {
        _db = db;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> CreateOrRefreshSchedulesAsync(int programId, IReadOnlyCollection<int>? zoneIds, CancellationToken ct = default)
    {
        var program = await _db.CycleCountPrograms.FirstOrDefaultAsync(p => p.ProgramId == programId && p.IsActive, ct)
            ?? throw new BusinessRuleException("Cycle count program is missing.", "CYCLE_PROGRAM_NOT_FOUND", "CycleCountProgram");

        var zoneSet = (zoneIds ?? Array.Empty<int>()).ToHashSet();
        var today = VietnamTime.Now.Date;
        var rows = await _db.ItemLocations
            .AsNoTracking()
            .Include(il => il.Item)
            .Include(il => il.Location)!.ThenInclude(l => l!.Zone)
            .Where(il => il.Quantity != 0
                && il.Location != null
                && il.Location.Zone != null
                && il.Location.Zone.WarehouseId == program.WarehouseId
                && (zoneSet.Count == 0 || zoneSet.Contains(il.Location.ZoneId)))
            .ToListAsync(ct);

        var existing = await _db.CycleCountSchedules
            .Where(s => s.ProgramId == programId)
            .ToDictionaryAsync(s => (s.ItemId, s.LocationId), ct);

        var upserts = 0;
        foreach (var row in rows)
        {
            var abc = ResolveAbc(row.Item);
            var key = (row.ItemId, row.LocationId);
            var highRisk = row.HoldStatus != InventoryHoldStatusEnum.Available;
            if (!existing.TryGetValue(key, out var schedule))
            {
                schedule = new CycleCountSchedule
                {
                    ProgramId = programId,
                    ItemId = row.ItemId,
                    LocationId = row.LocationId,
                    AbcClass = abc,
                    NextScheduledAt = today,
                    IsActive = true
                };
                _db.CycleCountSchedules.Add(schedule);
                upserts++;
                continue;
            }

            schedule.AbcClass = abc;
            schedule.IsActive = true;
            if (!schedule.NextScheduledAt.HasValue || highRisk)
                schedule.NextScheduledAt = today;
            upserts++;
        }

        program.NextRunAt = today;
        await _unitOfWork.SaveChangesAsync();
        return upserts;
    }

    public async Task<StockCountSheet> GenerateDueSheetAsync(int programId, string actor, int maxLines = 50, CancellationToken ct = default)
    {
        var program = await _db.CycleCountPrograms.FirstOrDefaultAsync(p => p.ProgramId == programId && p.IsActive, ct)
            ?? throw new BusinessRuleException("Cycle count program is missing.", "CYCLE_PROGRAM_NOT_FOUND", "CycleCountProgram");

        var today = VietnamTime.Now.Date;
        var due = await _db.CycleCountSchedules
            .Include(s => s.Item)
            .Where(s => s.ProgramId == programId
                && s.IsActive
                && (!s.NextScheduledAt.HasValue || s.NextScheduledAt.Value.Date <= today))
            .OrderBy(s => s.AbcClass)
            .ThenByDescending(s => s.CumulativeVariance ?? 0m)
            .ThenBy(s => s.LocationId)
            .Take(Math.Clamp(maxLines, 1, 500))
            .ToListAsync(ct);

        if (due.Count == 0)
            throw new BusinessRuleException("No cycle count schedules are due.", "CYCLE_NO_DUE_LINES", "CycleCountSchedule");

        var sheet = new StockCountSheet
        {
            SheetCode = $"CC-{today:yyyyMMdd}-{await _db.StockCountSheets.CountAsync(ct) + 1:D4}",
            WarehouseId = program.WarehouseId,
            CountDate = today,
            Status = StockCountStatusEnum.Draft,
            CreatedBy = actor,
            CreatedAt = VietnamTime.Now,
            Notes = $"Cycle count: {program.ProgramName}; blind={program.IsBlindCount}"
        };
        _db.StockCountSheets.Add(sheet);
        await _unitOfWork.SaveChangesAsync();

        foreach (var schedule in due)
        {
            var systemQty = await _db.ItemLocations
                .Where(il => il.ItemId == schedule.ItemId && il.LocationId == schedule.LocationId)
                .SumAsync(il => (decimal?)il.Quantity, ct) ?? 0m;

            _db.StockCountLines.Add(new StockCountLine
            {
                StockCountSheetId = sheet.StockCountSheetId,
                ItemId = schedule.ItemId,
                LocationId = schedule.LocationId,
                SystemQty = systemQty,
                CountedQty = null,
                Variance = null,
                Status = 1
            });

            schedule.LastCountedAt = today;
            schedule.NextScheduledAt = today.AddDays(FrequencyFor(program, schedule.AbcClass));
            schedule.CountAttempt++;
        }

        program.LastRunAt = today;
        program.NextRunAt = today.AddDays(Math.Min(program.FrequencyA, Math.Min(program.FrequencyB, program.FrequencyC)));
        await _unitOfWork.SaveChangesAsync();
        return sheet;
    }

    private static char ResolveAbc(Item? item)
    {
        if (string.IsNullOrWhiteSpace(item?.AbcClass))
            return 'C';
        var c = char.ToUpperInvariant(item.AbcClass[0]);
        return c is 'A' or 'B' or 'C' ? c : 'C';
    }

    private static int FrequencyFor(CycleCountProgram program, char abc)
        => abc switch
        {
            'A' => Math.Max(1, program.FrequencyA),
            'B' => Math.Max(1, program.FrequencyB),
            _ => Math.Max(1, program.FrequencyC)
        };
}

public sealed class ReturnRmaLineRequest
{
    public int ItemId { get; init; }
    public decimal Quantity { get; init; }
    public int BaseUomId { get; init; }
    public int LocationId { get; init; }
    public string? LotNumber { get; init; }
    public DateTime? ExpiryDate { get; init; }
}

public sealed class ReturnRmaRequest
{
    public long? OriginalOutboundVoucherId { get; init; }
    public int WarehouseId { get; init; }
    public int? CustomerPartnerId { get; init; }
    public int? OwnerPartnerId { get; init; }
    public string Reason { get; init; } = "";
    public string Actor { get; init; } = "system";
    public List<ReturnRmaLineRequest> Lines { get; init; } = new();
}

public sealed record ReturnRmaDispositionResult(long ReturnVoucherId, decimal RestockedQty, InventoryHoldStatusEnum ResultStatus);

public interface IReturnRmaService
{
    Task<Voucher> CreateReturnAsync(ReturnRmaRequest request, CancellationToken ct = default);
    Task<ReturnRmaDispositionResult> DispositionAsync(long returnVoucherId, QcDispositionEnum disposition, string actor, string reason, CancellationToken ct = default);
}

public sealed class ReturnRmaService : IReturnRmaService
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryBalanceService _inventoryBalanceService;

    public ReturnRmaService(AppDbContext db, IUnitOfWork unitOfWork, IInventoryBalanceService inventoryBalanceService)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _inventoryBalanceService = inventoryBalanceService;
    }

    public async Task<Voucher> CreateReturnAsync(ReturnRmaRequest request, CancellationToken ct = default)
    {
        if (request.Lines.Count == 0)
            throw new BusinessRuleException("RMA return requires at least one line.", "RMA_LINES_REQUIRED", "Voucher");

        var voucher = new Voucher
        {
            VoucherCode = await GenerateReturnCodeAsync(ct),
            VoucherType = VoucherTypeEnum.KhachTra,
            WarehouseId = request.WarehouseId,
            PartnerId = request.CustomerPartnerId,
            OwnerPartnerId = request.OwnerPartnerId,
            ParentVoucherId = request.OriginalOutboundVoucherId,
            VoucherDate = VietnamTime.Now.Date,
            SourceType = SourceTypeEnum.Manual,
            Description = request.Reason,
            CreatedBy = request.Actor,
            CreatedAt = VietnamTime.Now,
            InboundStatus = InboundStatusEnum.Receiving,
            ReviewResult = ReviewResultEnum.Pending
        };
        _db.Vouchers.Add(voucher);
        await _unitOfWork.SaveChangesAsync();

        var lineNo = 0;
        foreach (var line in request.Lines)
        {
            if (line.Quantity <= 0)
                throw new BusinessRuleException("RMA line quantity must be greater than zero.", "RMA_QTY_INVALID", "VoucherDetail");

            lineNo++;
            _db.VoucherDetails.Add(new VoucherDetail
            {
                VoucherId = voucher.VoucherId,
                ItemId = line.ItemId,
                OwnerPartnerId = request.OwnerPartnerId,
                LocationId = line.LocationId,
                TransactionQty = line.Quantity,
                TransactionUomId = line.BaseUomId,
                ConversionRate = 1m,
                BaseQty = line.Quantity,
                QualityStatus = QualityStatusEnum.Pending,
                LotNumber = line.LotNumber,
                ExpiryDate = line.ExpiryDate,
                LineNumber = lineNo,
                Notes = "RMA return pending QC"
            });
        }

        voucher.TotalLines = lineNo;
        _db.AuditLogs.Add(new AuditLog
        {
            TableName = "Voucher",
            RecordId = voucher.VoucherId.ToString(),
            ActionType = "RMA_CREATE",
            NewValue = $"Parent:{request.OriginalOutboundVoucherId};Lines:{lineNo}",
            ChangedBy = request.Actor,
            ChangedAt = VietnamTime.Now,
            AppModule = "ReturnsRMA"
        });

        await _unitOfWork.SaveChangesAsync();
        return voucher;
    }

    public async Task<ReturnRmaDispositionResult> DispositionAsync(long returnVoucherId, QcDispositionEnum disposition, string actor, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new BusinessRuleException("RMA disposition reason is required.", "RMA_DISPOSITION_REASON_REQUIRED", "Voucher");

        var voucher = await _db.Vouchers
            .Include(v => v.Details)
            .FirstOrDefaultAsync(v => v.VoucherId == returnVoucherId && v.VoucherType == VoucherTypeEnum.KhachTra, ct)
            ?? throw new BusinessRuleException("RMA voucher is missing.", "RMA_NOT_FOUND", "Voucher");

        var targetStatus = disposition switch
        {
            QcDispositionEnum.Accept or QcDispositionEnum.AcceptWithConditions => InventoryHoldStatusEnum.Available,
            QcDispositionEnum.Hold or QcDispositionEnum.Rework => InventoryHoldStatusEnum.QcHold,
            QcDispositionEnum.Scrap or QcDispositionEnum.Reject or QcDispositionEnum.ReturnToSupplier => InventoryHoldStatusEnum.Blocked,
            _ => InventoryHoldStatusEnum.Quarantine
        };

        var restockedQty = 0m;
        var affectedItems = new HashSet<int>();
        foreach (var detail in voucher.Details)
        {
            detail.QualityStatus = targetStatus == InventoryHoldStatusEnum.Available
                ? QualityStatusEnum.Passed
                : targetStatus == InventoryHoldStatusEnum.Blocked
                    ? QualityStatusEnum.Failed
                    : QualityStatusEnum.OnHold;

            if (targetStatus == InventoryHoldStatusEnum.Blocked)
                continue;

            var row = await _db.ItemLocations.FirstOrDefaultAsync(il =>
                il.ItemId == detail.ItemId
                && il.OwnerPartnerId == detail.OwnerPartnerId
                && il.LocationId == detail.LocationId!.Value
                && il.LotNumber == detail.LotNumber
                && il.ExpiryDate == detail.ExpiryDate
                && il.HoldStatus == targetStatus, ct);
            if (row == null)
            {
                row = new ItemLocation
                {
                    ItemId = detail.ItemId,
                    OwnerPartnerId = detail.OwnerPartnerId,
                    LocationId = detail.LocationId!.Value,
                    LotNumber = detail.LotNumber,
                    ExpiryDate = detail.ExpiryDate,
                    HoldStatus = targetStatus,
                    UpdatedAt = VietnamTime.Now
                };
                _db.ItemLocations.Add(row);
            }

            row.Quantity += detail.BaseQty;
            row.UpdatedAt = VietnamTime.Now;
            restockedQty += detail.BaseQty;
            affectedItems.Add(detail.ItemId);
        }

        voucher.IsPosted = true;
        voucher.InboundStatus = InboundStatusEnum.Completed;
        voucher.CompletedBy = actor;
        voucher.CompletedAt = VietnamTime.Now;
        voucher.Description = $"{voucher.Description}; RMA disposition={disposition}; reason={reason.Trim()}";

        _db.AuditLogs.Add(new AuditLog
        {
            TableName = "Voucher",
            RecordId = voucher.VoucherId.ToString(),
            ActionType = "RMA_QC",
            OldValue = "PendingQC",
            NewValue = $"{disposition};Restocked:{restockedQty:N4};Status:{targetStatus}",
            ChangedBy = actor,
            ChangedAt = VietnamTime.Now,
            AppModule = "ReturnsRMA"
        });

        await _unitOfWork.SaveChangesAsync();
        if (affectedItems.Count > 0)
        {
            await _inventoryBalanceService.SyncCurrentStockAsync(affectedItems);
            await _unitOfWork.SaveChangesAsync();
        }

        return new ReturnRmaDispositionResult(returnVoucherId, restockedQty, targetStatus);
    }

    private async Task<string> GenerateReturnCodeAsync(CancellationToken ct)
    {
        var prefix = $"RMA-{VietnamTime.Now:yyyyMMdd}-";
        var count = await _db.Vouchers.CountAsync(v => v.VoucherCode.StartsWith(prefix), ct);
        return $"{prefix}{count + 1:D5}";
    }
}

public sealed class CartonOption
{
    public string Code { get; init; } = "";
    public string PackageType { get; init; } = "";
    public decimal MaxWeightKg { get; init; }
    public decimal LengthCm { get; init; }
    public decimal WidthCm { get; init; }
    public decimal HeightCm { get; init; }
    public int? OwnerPartnerId { get; init; }
}

public sealed class CartonizationRecommendation
{
    public string PackageType { get; init; } = "";
    public int PackageCount { get; init; }
    public decimal EstimatedWeightKg { get; init; }
    public decimal EstimatedVolumeCbm { get; init; }
    public string Reason { get; init; } = "";
}

public interface ICartonizationService
{
    Task<CartonizationRecommendation> RecommendAsync(long voucherId, IReadOnlyCollection<CartonOption>? options = null, CancellationToken ct = default);
    string BuildOverrideNote(CartonizationRecommendation recommendation, string chosenPackageType, string overrideReason);
}

public sealed class CartonizationService : ICartonizationService
{
    private readonly AppDbContext _db;

    public CartonizationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CartonizationRecommendation> RecommendAsync(long voucherId, IReadOnlyCollection<CartonOption>? options = null, CancellationToken ct = default)
    {
        var voucher = await _db.Vouchers
            .AsNoTracking()
            .Include(v => v.Details)!.ThenInclude(d => d.Item)
            .FirstOrDefaultAsync(v => v.VoucherId == voucherId, ct)
            ?? throw new BusinessRuleException("Voucher is missing.", "CARTON_VOUCHER_NOT_FOUND", "Voucher");

        var lines = voucher.Details.Where(d => d.BaseQty != 0m).ToList();
        if (lines.Count == 0)
            throw new BusinessRuleException("Voucher has no packable lines.", "CARTON_LINES_REQUIRED", "VoucherDetail");

        var totalWeight = lines.Sum(d => Math.Abs(d.BaseQty) * (d.Item?.Weight ?? 1m));
        var totalVolumeCm3 = lines.Sum(d =>
        {
            var item = d.Item;
            var unitVolume = (item?.Length ?? 10m) * (item?.Width ?? 10m) * (item?.Height ?? 10m);
            return Math.Abs(d.BaseQty) * unitVolume;
        });

        var availableOptions = (options == null || options.Count == 0 ? DefaultOptions() : options)
            .Where(o => !o.OwnerPartnerId.HasValue || o.OwnerPartnerId == voucher.OwnerPartnerId)
            .OrderBy(o => o.LengthCm * o.WidthCm * o.HeightCm)
            .ThenBy(o => o.MaxWeightKg)
            .ToList();
        if (availableOptions.Count == 0)
            availableOptions = DefaultOptions();

        var best = availableOptions
            .Select(option =>
            {
                var volumeCapacity = Math.Max(1m, option.LengthCm * option.WidthCm * option.HeightCm);
                var byWeight = option.MaxWeightKg <= 0m ? 1 : (int)Math.Ceiling(totalWeight / option.MaxWeightKg);
                var byVolume = (int)Math.Ceiling(totalVolumeCm3 / volumeCapacity);
                return new { Option = option, Count = Math.Max(1, Math.Max(byWeight, byVolume)), VolumeCapacity = volumeCapacity };
            })
            .OrderBy(x => x.Count)
            .ThenBy(x => x.VolumeCapacity)
            .First();

        return new CartonizationRecommendation
        {
            PackageType = best.Option.PackageType,
            PackageCount = best.Count,
            EstimatedWeightKg = totalWeight,
            EstimatedVolumeCbm = Math.Round(totalVolumeCm3 / 1_000_000m, 6),
            Reason = $"Fits by weight {totalWeight:N2}kg and volume {totalVolumeCm3 / 1_000_000m:N4}cbm using {best.Option.Code}."
        };
    }

    public string BuildOverrideNote(CartonizationRecommendation recommendation, string chosenPackageType, string overrideReason)
    {
        if (string.IsNullOrWhiteSpace(overrideReason))
            throw new BusinessRuleException("Cartonization override reason is required.", "CARTON_OVERRIDE_REASON_REQUIRED", "OutboundPackage");

        return $"Cartonization suggested {recommendation.PackageType} x{recommendation.PackageCount}; chosen {chosenPackageType}; reason={overrideReason.Trim()}";
    }

    private static List<CartonOption> DefaultOptions()
        => new()
        {
            new CartonOption { Code = "S", PackageType = "Small carton", MaxWeightKg = 5m, LengthCm = 30m, WidthCm = 20m, HeightCm = 15m },
            new CartonOption { Code = "M", PackageType = "Medium carton", MaxWeightKg = 15m, LengthCm = 50m, WidthCm = 35m, HeightCm = 30m },
            new CartonOption { Code = "L", PackageType = "Large carton", MaxWeightKg = 30m, LengthCm = 70m, WidthCm = 50m, HeightCm = 45m },
            new CartonOption { Code = "P", PackageType = "Pallet", MaxWeightKg = 800m, LengthCm = 120m, WidthCm = 100m, HeightCm = 120m }
        };
}
