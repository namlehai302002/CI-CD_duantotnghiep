using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;
using WMS.ViewModels;

namespace WMS.Services;

public sealed class ReplenishmentAutomationOptions
{
    public const string SectionName = "ReplenishmentAutomation";

    public bool Enabled { get; init; } = true;
    public bool AutoCreateTasks { get; init; } = true;
    public int IntervalSeconds { get; init; } = 300;
    public int BatchSize { get; init; } = 100;
    public int MaxTasksPerRun { get; init; } = 50;
    public int DemandHorizonDays { get; init; } = 3;
    public int ForecastHistoryDays { get; init; } = 30;
    public int ForecastHorizonDays { get; init; } = 2;
    public decimal ForecastSafetyFactor { get; init; } = 1.15m;
    public decimal MinimumSuggestedQty { get; init; } = 0.0001m;

    public static ReplenishmentAutomationOptions Normalize(ReplenishmentAutomationOptions? options)
    {
        var value = options ?? new ReplenishmentAutomationOptions();
        return new ReplenishmentAutomationOptions
        {
            Enabled = value.Enabled,
            AutoCreateTasks = value.AutoCreateTasks,
            IntervalSeconds = Math.Clamp(value.IntervalSeconds, 30, 86_400),
            BatchSize = Math.Clamp(value.BatchSize, 1, 500),
            MaxTasksPerRun = Math.Clamp(value.MaxTasksPerRun, 1, 500),
            DemandHorizonDays = Math.Clamp(value.DemandHorizonDays, 0, 30),
            ForecastHistoryDays = Math.Clamp(value.ForecastHistoryDays, 1, 365),
            ForecastHorizonDays = Math.Clamp(value.ForecastHorizonDays, 0, 30),
            ForecastSafetyFactor = value.ForecastSafetyFactor <= 0 ? 1m : Math.Min(value.ForecastSafetyFactor, 5m),
            MinimumSuggestedQty = value.MinimumSuggestedQty <= 0 ? 0.0001m : value.MinimumSuggestedQty
        };
    }
}

public sealed class ReplenishmentAutomationRunRequest
{
    public int WarehouseId { get; init; }
    public string Actor { get; init; } = "system";
    public bool? AutoCreateTasks { get; init; }
    public int? MaxTasks { get; init; }
    public string? Search { get; init; }
    public ReplenishmentAutomationOptions? Options { get; init; }
}

public interface IReplenishmentAutomationService
{
    Task<List<ReplenishmentSuggestionRow>> BuildSuggestionsAsync(int? warehouseId, string? search, ReplenishmentAutomationOptions? options = null);
    Task<ReplenishmentAutomationRun> RunAsync(ReplenishmentAutomationRunRequest request);
}

public sealed class ReplenishmentAutomationService : IReplenishmentAutomationService
{
    private static readonly VoucherTypeEnum[] OutboundTypes =
    {
        VoucherTypeEnum.XuatKho,
        VoucherTypeEnum.TraNCC,
        VoucherTypeEnum.ChuyenKho,
        VoucherTypeEnum.XuatSanXuat
    };

    private static readonly MovementTaskStatusEnum[] OpenMovementStatuses =
    {
        MovementTaskStatusEnum.Pending,
        MovementTaskStatusEnum.Assigned,
        MovementTaskStatusEnum.InProgress
    };

    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMovementTaskService _movementTaskService;

    public ReplenishmentAutomationService(
        AppDbContext db,
        IUnitOfWork unitOfWork,
        IMovementTaskService movementTaskService)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _movementTaskService = movementTaskService;
    }

    private static DateTime Now => VietnamTime.Now;

    public async Task<List<ReplenishmentSuggestionRow>> BuildSuggestionsAsync(
        int? warehouseId,
        string? search,
        ReplenishmentAutomationOptions? options = null)
    {
        var normalizedOptions = ReplenishmentAutomationOptions.Normalize(options);
        var items = await _db.Items
            .AsNoTracking()
            .Include(i => i.DefaultLocation)!.ThenInclude(l => l!.Zone)!.ThenInclude(z => z!.Warehouse)
            .Where(i => i.IsActive
                && i.DefaultLocationId.HasValue
                && i.DefaultLocation != null
                && i.DefaultLocation.IsActive
                && i.DefaultLocation.Zone != null
                && i.DefaultLocation.Zone.IsActive
                && (!warehouseId.HasValue || i.DefaultLocation.Zone.WarehouseId == warehouseId.Value))
            .OrderBy(i => i.ItemCode)
            .ToListAsync();

        if (items.Count == 0)
            return new List<ReplenishmentSuggestionRow>();

        var itemIds = items.Select(i => i.ItemId).Distinct().ToList();
        var warehouseIds = items
            .Select(i => i.DefaultLocation!.Zone!.WarehouseId)
            .Distinct()
            .ToList();

        var itemLocations = await _db.ItemLocations
            .AsNoTracking()
            .Include(il => il.Item)
            .Include(il => il.Location)!.ThenInclude(l => l!.Zone)!.ThenInclude(z => z!.Warehouse)
            .Where(il => itemIds.Contains(il.ItemId)
                && il.Quantity > 0m
                && il.HoldStatus == InventoryHoldStatusEnum.Available
                && il.Location != null
                && il.Location.IsActive
                && il.Location.Zone != null
                && warehouseIds.Contains(il.Location.Zone.WarehouseId))
            .ToListAsync();

        var openReplenishmentByDestination = await BuildOpenReplenishmentMapAsync(itemIds, warehouseIds);
        var lpnLookup = await BuildActiveLpnLookupAsync(itemIds, warehouseIds);
        var demandMap = await BuildDemandMapAsync(itemIds, warehouseIds, normalizedOptions);
        var forecastMap = await BuildForecastMapAsync(itemIds, warehouseIds, normalizedOptions);

        var rows = new List<ReplenishmentSuggestionRow>();
        foreach (var item in items)
        {
            var defaultLocation = item.DefaultLocation;
            if (defaultLocation?.Zone == null)
                continue;

            var itemWarehouseId = defaultLocation.Zone.WarehouseId;
            var triggerQty = item.ReorderPoint ?? item.MinThreshold;
            var demand = GetDemand(demandMap, item.ItemId, itemWarehouseId);
            var forecastQty = GetForecast(forecastMap, item.ItemId, itemWarehouseId);
            if (triggerQty <= 0m && demand.Quantity <= 0m && forecastQty <= 0m)
                continue;

            var relevantRows = itemLocations
                .Where(il => il.ItemId == item.ItemId
                    && il.Location?.Zone != null
                    && il.Location.Zone.WarehouseId == itemWarehouseId)
                .ToList();

            var pickFaceQty = relevantRows
                .Where(il => il.LocationId == defaultLocation.LocationId)
                .Sum(il => Math.Max(0m, il.Quantity - il.ReservedQty));
            var openReplenishmentQty = GetOpenReplenishment(openReplenishmentByDestination, item.ItemId, defaultLocation.LocationId);
            var effectivePickFaceQty = pickFaceQty + openReplenishmentQty;
            var thresholdTarget = item.MaxThreshold ?? Math.Max(triggerQty * 2m, triggerQty);
            var demandTarget = demand.Quantity + forecastQty;
            var targetQty = Math.Max(thresholdTarget, triggerQty + demandTarget);
            if (triggerQty <= 0m)
                targetQty = demandTarget;

            var needsThreshold = triggerQty > 0m && effectivePickFaceQty < triggerQty;
            var needsDemand = demand.Quantity > 0m && effectivePickFaceQty < demand.Quantity;
            var needsForecast = forecastQty > 0m && effectivePickFaceQty < forecastQty;
            if (!needsThreshold && !needsDemand && !needsForecast)
                continue;

            var shortageQty = targetQty - effectivePickFaceQty;
            if (shortageQty < normalizedOptions.MinimumSuggestedQty)
                continue;

            var sourceCandidates = FilterSourceCandidates(relevantRows, item, defaultLocation.LocationId);
            if (sourceCandidates.Count == 0)
                continue;

            var source = sourceCandidates[0];
            if (source.Location?.Zone == null)
                continue;

            var sourceAvailable = Math.Max(0m, source.Quantity - source.ReservedQty);
            var suggestedQty = Math.Min(shortageQty, sourceAvailable);
            if (suggestedQty < normalizedOptions.MinimumSuggestedQty)
                continue;

            var triggerType = ResolveTriggerType(needsThreshold, needsDemand, needsForecast);
            var priority = ResolvePriority(pickFaceQty, effectivePickFaceQty, triggerQty, demand.Quantity, forecastQty, demand.EarliestDueAt);
            var dueAt = demand.EarliestDueAt ?? ResolveDefaultDueAt(priority);
            var travelScore = TaskInterleavingService.CalculateProximityScore(
                source.Location.AisleSequence,
                defaultLocation.AisleSequence,
                source.Location.ZoneId,
                defaultLocation.ZoneId);
            var routePriorityScore = CalculateRoutePriorityScore(priority, dueAt, travelScore, demand.Quantity, forecastQty, pickFaceQty);
            var hasActiveLpn = lpnLookup.ContainsKey((item.ItemId, source.LocationId, source.LotNumber ?? "", source.ExpiryDate));

            rows.Add(new ReplenishmentSuggestionRow
            {
                WarehouseId = itemWarehouseId,
                WarehouseName = defaultLocation.Zone.Warehouse?.WarehouseName ?? $"Kho {itemWarehouseId}",
                ItemId = item.ItemId,
                ItemCode = item.ItemCode,
                ItemName = item.ItemName,
                OwnerPartnerId = source.OwnerPartnerId, // P2-R2-3
                DefaultLocationId = defaultLocation.LocationId,
                DefaultLocationCode = defaultLocation.LocationCode,
                SourceItemLocationId = source.ItemLocationId,
                SourceLocationId = source.LocationId,
                SourceLocationCode = source.Location?.LocationCode ?? source.LocationId.ToString(),
                PickFaceQty = pickFaceQty,
                OpenReplenishmentQty = openReplenishmentQty,
                EffectivePickFaceQty = effectivePickFaceQty,
                SourceAvailableQty = sourceAvailable,
                TriggerQty = triggerQty,
                TargetQty = targetQty,
                SuggestedQty = suggestedQty,
                DemandQty = demand.Quantity,
                ForecastQty = forecastQty,
                TriggerType = triggerType,
                Priority = priority,
                DueAt = dueAt,
                RoutePriorityScore = routePriorityScore,
                TravelSequenceScore = travelScore,
                SourceZoneCode = source.Location?.Zone?.ZoneCode,
                DestinationZoneCode = defaultLocation.Zone.ZoneCode,
                SourceAisleSequence = source.Location?.AisleSequence ?? 0,
                DestinationAisleSequence = defaultLocation.AisleSequence,
                LotNumber = source.LotNumber,
                ExpiryDate = source.ExpiryDate,
                HasActiveLpn = hasActiveLpn,
                LocationMaxCapacity = defaultLocation.MaxCapacity,
                LocationCurrentLoad = defaultLocation.CurrentLoad,
                ItemWeightOrVolume = item.Weight ?? 1m,
                CapacityUnit = item.ItemType == ItemTypeEnum.HoaChat ? "unit" : "kg",
                SuggestionReason = BuildSuggestionReason(triggerType, priority, pickFaceQty, openReplenishmentQty, demand.Quantity, forecastQty, hasActiveLpn)
            });
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            rows = rows.Where(r =>
                    r.ItemCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    || r.ItemName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    || r.DefaultLocationCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    || r.SourceLocationCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    || (r.LotNumber?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        return rows
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.DueAt ?? DateTime.MaxValue)
            .ThenByDescending(r => r.RoutePriorityScore)
            .ThenBy(r => r.SourceAisleSequence)
            .ThenBy(r => r.DefaultLocationCode)
            .Take(normalizedOptions.BatchSize)
            .ToList();
    }

    public async Task<ReplenishmentAutomationRun> RunAsync(ReplenishmentAutomationRunRequest request)
    {
        var options = ReplenishmentAutomationOptions.Normalize(request.Options);
        var autoCreate = request.AutoCreateTasks ?? options.AutoCreateTasks;
        var maxTasks = Math.Clamp(request.MaxTasks ?? options.MaxTasksPerRun, 1, 500);
        var warehouse = await _db.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.WarehouseId == request.WarehouseId && w.IsActive);
        if (warehouse == null)
            throw new BusinessRuleException("Kho không tồn tại hoặc không hoạt động.", "REPLENISHMENT_WAREHOUSE_NOT_FOUND", "Warehouse");

        var suggestions = await BuildSuggestionsAsync(request.WarehouseId, request.Search, options);
        var run = new ReplenishmentAutomationRun
        {
            RunCode = await GenerateRunCodeAsync(),
            WarehouseId = request.WarehouseId,
            Status = ReplenishmentRunStatusEnum.Started,
            AutoCreateTasks = autoCreate,
            DemandHorizonDays = options.DemandHorizonDays,
            ForecastHistoryDays = options.ForecastHistoryDays,
            ForecastHorizonDays = options.ForecastHorizonDays,
            ForecastSafetyFactor = options.ForecastSafetyFactor,
            SuggestedLineCount = suggestions.Count,
            TriggeredBy = Clean(request.Actor) ?? "system",
            StartedAt = Now,
            ConfigJson = JsonSerializer.Serialize(options)
        };
        _db.ReplenishmentAutomationRuns.Add(run);
        await _unitOfWork.SaveChangesAsync();

        var createdTasks = 0;
        var runLines = new List<ReplenishmentAutomationLine>();
        foreach (var suggestion in suggestions.Take(maxTasks))
        {
            var line = BuildRunLine(run, suggestion);
            _db.ReplenishmentAutomationLines.Add(line);
            runLines.Add(line);
            await _unitOfWork.SaveChangesAsync();

            if (!autoCreate)
            {
                line.Status = ReplenishmentAutomationLineStatusEnum.Planned;
                continue;
            }

            try
            {
                var task = await CreateMovementTaskForSuggestionAsync(run, line, suggestion, request.Actor);
                line.MovementTaskId = task.MovementTaskId;
                line.Status = ReplenishmentAutomationLineStatusEnum.TaskCreated;
                line.ErrorMessage = null;
                createdTasks++;
            }
            catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
            {
                line.Status = ReplenishmentAutomationLineStatusEnum.Failed;
                line.ErrorMessage = UserSafeError.From(ex, "Không thể tạo nhiệm vụ bổ sung hàng lúc này.");
            }
        }

        run.CreatedTaskCount = createdTasks;
        run.FailedLineCount = runLines.Count(l => l.Status == ReplenishmentAutomationLineStatusEnum.Failed);
        run.SkippedLineCount = Math.Max(0, suggestions.Count - runLines.Count);
        run.CompletedAt = Now;
        run.Status = run.FailedLineCount == 0
            ? ReplenishmentRunStatusEnum.Completed
            : createdTasks > 0
                ? ReplenishmentRunStatusEnum.PartiallyCompleted
                : ReplenishmentRunStatusEnum.Failed;

        await _unitOfWork.SaveChangesAsync();
        return run;
    }

    private async Task<MovementTask> CreateMovementTaskForSuggestionAsync(
        ReplenishmentAutomationRun run,
        ReplenishmentAutomationLine line,
        ReplenishmentSuggestionRow suggestion,
        string actor)
    {
        var task = await _movementTaskService.CreateMovementTaskAsync(new MovementTaskCreateRequest
        {
            WarehouseId = suggestion.WarehouseId,
            ItemId = suggestion.ItemId,
            SourceLocationId = suggestion.SourceLocationId,
            DestinationLocationId = suggestion.DefaultLocationId,
            SourceItemLocationId = suggestion.SourceItemLocationId,
            TaskType = MovementTaskTypeEnum.Replenishment,
            Priority = suggestion.Priority,
            PlannedQty = suggestion.SuggestedQty,
            LotNumber = suggestion.LotNumber,
            ExpiryDate = suggestion.ExpiryDate,
            ReplenishmentAutomationRunId = run.ReplenishmentAutomationRunId,
            ReplenishmentAutomationLineId = line.ReplenishmentAutomationLineId,
            ReplenishmentTriggerType = suggestion.TriggerType,
            DemandQtySnapshot = suggestion.DemandQty,
            ForecastQtySnapshot = suggestion.ForecastQty,
            OpenReplenishmentQtySnapshot = suggestion.OpenReplenishmentQty,
            RoutePriorityScore = suggestion.RoutePriorityScore,
            TravelSequenceScore = suggestion.TravelSequenceScore,
            SourceAisleSequence = suggestion.SourceAisleSequence,
            DestinationAisleSequence = suggestion.DestinationAisleSequence,
            AutomationBatchKey = run.RunCode,
            DueAt = suggestion.DueAt,
            SourceModule = "ReplenishmentAutomation",
            SourceReference = run.RunCode,
            SourceReason = suggestion.SuggestionReason,
            Notes = $"Trigger={suggestion.TriggerType}; Demand={suggestion.DemandQty:N4}; Forecast={suggestion.ForecastQty:N4}; OpenRepl={suggestion.OpenReplenishmentQty:N4}"
        }, suggestion.WarehouseId, Clean(actor) ?? "system");

        task.ReplenishmentAutomationRunId = run.ReplenishmentAutomationRunId;
        task.ReplenishmentAutomationLineId = line.ReplenishmentAutomationLineId;
        task.ReplenishmentTriggerType = suggestion.TriggerType;
        task.DemandQtySnapshot = suggestion.DemandQty;
        task.ForecastQtySnapshot = suggestion.ForecastQty;
        task.OpenReplenishmentQtySnapshot = suggestion.OpenReplenishmentQty;
        task.RoutePriorityScore = suggestion.RoutePriorityScore;
        task.TravelSequenceScore = suggestion.TravelSequenceScore;
        task.AutomationBatchKey = run.RunCode;
        task.DueAt = suggestion.DueAt;
        await _unitOfWork.SaveChangesAsync();

        return task;
    }

    private async Task<Dictionary<(int ItemId, int LocationId), decimal>> BuildOpenReplenishmentMapAsync(
        IReadOnlyCollection<int> itemIds,
        IReadOnlyCollection<int> warehouseIds)
        => (await _db.MovementTasks
            .AsNoTracking()
            .Where(t => t.TaskType == MovementTaskTypeEnum.Replenishment
                && OpenMovementStatuses.Contains(t.Status)
                && itemIds.Contains(t.ItemId)
                && warehouseIds.Contains(t.WarehouseId))
            .GroupBy(t => new { t.ItemId, LocationId = t.DestinationLocationId })
            .Select(g => new
            {
                g.Key.ItemId,
                g.Key.LocationId,
                Qty = g.Sum(t => t.PlannedQty - t.ConfirmedQty)
            })
            .ToListAsync())
            .ToDictionary(x => (x.ItemId, x.LocationId), x => Math.Max(0m, x.Qty));

    private async Task<Dictionary<(int ItemId, int LocationId, string Lot, DateTime? ExpiryDate), int>> BuildActiveLpnLookupAsync(
        IReadOnlyCollection<int> itemIds,
        IReadOnlyCollection<int> warehouseIds)
        => (await _db.LicensePlateDetails
            .AsNoTracking()
            .Where(d => itemIds.Contains(d.ItemId)
                && d.LicensePlate != null
                && d.LicensePlate.IsActive
                && d.LicensePlate.Status != LpnStatusEnum.Voided
                && d.LicensePlate.CurrentLocationId.HasValue
                && warehouseIds.Contains(d.LicensePlate.WarehouseId))
            .GroupBy(d => new
            {
                d.ItemId,
                LocationId = d.LicensePlate!.CurrentLocationId!.Value,
                Lot = d.LotNumber ?? "",
                d.ExpiryDate
            })
            .Select(g => new
            {
                g.Key.ItemId,
                g.Key.LocationId,
                g.Key.Lot,
                g.Key.ExpiryDate,
                Count = g.Count()
            })
            .ToListAsync())
            .ToDictionary(x => (x.ItemId, x.LocationId, x.Lot, x.ExpiryDate), x => x.Count);

    private async Task<Dictionary<(int ItemId, int WarehouseId), DemandSnapshot>> BuildDemandMapAsync(
        IReadOnlyCollection<int> itemIds,
        IReadOnlyCollection<int> warehouseIds,
        ReplenishmentAutomationOptions options)
    {
        var horizonDate = Now.Date.AddDays(options.DemandHorizonDays);
        var rows = await _db.VoucherDetails
            .AsNoTracking()
            .Include(d => d.Voucher)
            .Where(d => d.Voucher != null
                && itemIds.Contains(d.ItemId)
                && warehouseIds.Contains(d.Voucher.WarehouseId)
                && OutboundTypes.Contains(d.Voucher.VoucherType)
                && !d.Voucher.IsCancelled
                && !d.Voucher.IsPosted
                && d.Voucher.FulfillmentStatus != FulfillmentStatusEnum.Shipped
                && d.Voucher.FulfillmentStatus != FulfillmentStatusEnum.Completed
                && (!d.Voucher.RequestedDeliveryDate.HasValue || d.Voucher.RequestedDeliveryDate.Value <= horizonDate))
            .Select(d => new
            {
                d.VoucherId,
                d.ItemId,
                d.BaseQty,
                d.Voucher!.WarehouseId,
                d.Voucher.RequestedDeliveryDate,
                d.Voucher.SlaHours,
                d.Voucher.CreatedAt
            })
            .ToListAsync();

        var demand = rows
            .GroupBy(x => (x.ItemId, x.WarehouseId))
            .ToDictionary(
                g => g.Key,
                g => new DemandSnapshot(
                    g.Sum(x => Math.Max(0m, x.BaseQty)),
                    g.Select(x => ResolveDemandDueAt(x.RequestedDeliveryDate, x.SlaHours, x.CreatedAt))
                        .Where(d => d.HasValue)
                        .DefaultIfEmpty()
                        .Min()));

        var voucherIdsAlreadyCounted = rows.Select(x => x.VoucherId).Distinct().ToHashSet();
        var waveRows = await _db.WaveLines
            .AsNoTracking()
            .Include(l => l.Wave)
            .Where(l => itemIds.Contains(l.ItemId)
                && l.Wave != null
                && warehouseIds.Contains(l.Wave.WarehouseId)
                && l.Status < 3
                && l.RequiredQty > l.PickedQty
                && !voucherIdsAlreadyCounted.Contains(l.VoucherId))
            .Select(l => new
            {
                l.ItemId,
                l.Wave!.WarehouseId,
                Qty = l.RequiredQty - l.PickedQty,
                l.Wave.ReleasedAt
            })
            .ToListAsync();

        foreach (var waveGroup in waveRows.GroupBy(x => (x.ItemId, x.WarehouseId)))
        {
            var waveQty = waveGroup.Sum(x => Math.Max(0m, x.Qty));
            var dueAt = waveGroup
                .Select(x => (DateTime?)(x.ReleasedAt ?? Now).AddHours(4))
                .DefaultIfEmpty()
                .Min();
            if (demand.TryGetValue(waveGroup.Key, out var existing))
            {
                demand[waveGroup.Key] = new DemandSnapshot(
                    existing.Quantity + waveQty,
                    MinNullable(existing.EarliestDueAt, dueAt));
            }
            else
            {
                demand[waveGroup.Key] = new DemandSnapshot(waveQty, dueAt);
            }
        }

        return demand;
    }

    private async Task<Dictionary<(int ItemId, int WarehouseId), decimal>> BuildForecastMapAsync(
        IReadOnlyCollection<int> itemIds,
        IReadOnlyCollection<int> warehouseIds,
        ReplenishmentAutomationOptions options)
    {
        var historyStart = Now.Date.AddDays(-options.ForecastHistoryDays);
        var postedRows = await _db.VoucherDetails
            .AsNoTracking()
            .Include(d => d.Voucher)
            .Where(d => d.Voucher != null
                && itemIds.Contains(d.ItemId)
                && warehouseIds.Contains(d.Voucher.WarehouseId)
                && OutboundTypes.Contains(d.Voucher.VoucherType)
                && d.Voucher.IsPosted
                && !d.Voucher.IsCancelled
                && (d.Voucher.CompletedAt ?? d.Voucher.VoucherDate) >= historyStart)
            .Select(d => new
            {
                d.ItemId,
                d.BaseQty,
                d.Voucher!.WarehouseId
            })
            .ToListAsync();

        var forecast = postedRows
            .GroupBy(x => (x.ItemId, x.WarehouseId))
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => Math.Max(0m, x.BaseQty)) / options.ForecastHistoryDays * options.ForecastHorizonDays * options.ForecastSafetyFactor);

        var velocityRows = await _db.ItemVelocityClassifications
            .AsNoTracking()
            .Where(v => v.IsActive && itemIds.Contains(v.ItemId) && warehouseIds.Contains(v.WarehouseId))
            .ToListAsync();

        foreach (var velocity in velocityRows
            .GroupBy(v => (v.ItemId, v.WarehouseId))
            .Select(g => g.OrderByDescending(v => v.LastAnalyzedAt).First()))
        {
            var velocityForecast = velocity.AnalysisPeriodDays > 0
                ? velocity.TotalPickQty / velocity.AnalysisPeriodDays * options.ForecastHorizonDays * options.ForecastSafetyFactor
                : 0m;
            var key = (velocity.ItemId, velocity.WarehouseId);
            forecast[key] = forecast.TryGetValue(key, out var existing)
                ? Math.Max(existing, velocityForecast)
                : velocityForecast;
        }

        return forecast;
    }

    private static List<ItemLocation> FilterSourceCandidates(
        IReadOnlyCollection<ItemLocation> rows,
        Item item,
        int defaultLocationId)
    {
        var candidates = rows
            .Where(il => il.LocationId != defaultLocationId
                && il.Location?.Zone != null
                && il.Quantity - il.ReservedQty > 0m)
            .ToList();

        if (!string.IsNullOrWhiteSpace(item.AllowedZoneTypes))
        {
            var allowedZoneTypes = item.AllowedZoneTypes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => byte.TryParse(s, out _))
                .Select(byte.Parse)
                .ToHashSet();
            if (allowedZoneTypes.Count > 0)
            {
                candidates = candidates
                    .Where(il => il.Location?.Zone != null && allowedZoneTypes.Contains((byte)il.Location.Zone.ZoneType))
                    .ToList();
            }
        }

        return candidates
            .OrderBy(il => item.TrackExpiry ? il.ExpiryDate ?? DateTime.MaxValue : DateTime.MaxValue)
            .ThenBy(il => il.Location?.Zone?.ZoneType == ZoneTypeEnum.Storage ? 0 : 1)
            .ThenBy(il => il.Location?.AisleSequence ?? int.MaxValue)
            .ThenByDescending(il => il.Quantity - il.ReservedQty)
            .ToList();
    }

    private static ReplenishmentAutomationLine BuildRunLine(ReplenishmentAutomationRun run, ReplenishmentSuggestionRow suggestion)
        => new()
        {
            ReplenishmentAutomationRunId = run.ReplenishmentAutomationRunId,
            WarehouseId = suggestion.WarehouseId,
            ItemId = suggestion.ItemId,
            DestinationLocationId = suggestion.DefaultLocationId,
            SourceLocationId = suggestion.SourceLocationId,
            SourceItemLocationId = suggestion.SourceItemLocationId,
            TriggerType = suggestion.TriggerType,
            Status = ReplenishmentAutomationLineStatusEnum.Planned,
            Priority = suggestion.Priority,
            PickFaceQty = suggestion.PickFaceQty,
            OpenReplenishmentQty = suggestion.OpenReplenishmentQty,
            DemandQty = suggestion.DemandQty,
            ForecastQty = suggestion.ForecastQty,
            TriggerQty = suggestion.TriggerQty,
            TargetQty = suggestion.TargetQty,
            SuggestedQty = suggestion.SuggestedQty,
            SourceAvailableQty = suggestion.SourceAvailableQty,
            LotNumber = suggestion.LotNumber,
            ExpiryDate = suggestion.ExpiryDate,
            DueAt = suggestion.DueAt,
            RoutePriorityScore = suggestion.RoutePriorityScore,
            TravelSequenceScore = suggestion.TravelSequenceScore,
            Reason = suggestion.SuggestionReason,
            CreatedAt = Now
        };

    private async Task<string> GenerateRunCodeAsync()
    {
        var prefix = $"REP-AUTO-{Now:yyyyMMdd}-";
        var count = await _db.ReplenishmentAutomationRuns.CountAsync(r => r.RunCode.StartsWith(prefix));
        return $"{prefix}{count + 1:0000}";
    }

    private static DemandSnapshot GetDemand(
        IReadOnlyDictionary<(int ItemId, int WarehouseId), DemandSnapshot> demandMap,
        int itemId,
        int warehouseId)
        => demandMap.TryGetValue((itemId, warehouseId), out var demand) ? demand : DemandSnapshot.Empty;

    private static DateTime? MinNullable(DateTime? left, DateTime? right)
    {
        if (!left.HasValue)
            return right;
        if (!right.HasValue)
            return left;
        return left.Value <= right.Value ? left : right;
    }

    private static decimal GetForecast(
        IReadOnlyDictionary<(int ItemId, int WarehouseId), decimal> forecastMap,
        int itemId,
        int warehouseId)
        => forecastMap.TryGetValue((itemId, warehouseId), out var forecast) ? Math.Max(0m, forecast) : 0m;

    private static decimal GetOpenReplenishment(
        IReadOnlyDictionary<(int ItemId, int LocationId), decimal> openMap,
        int itemId,
        int locationId)
        => openMap.TryGetValue((itemId, locationId), out var qty) ? Math.Max(0m, qty) : 0m;

    private static ReplenishmentTriggerTypeEnum ResolveTriggerType(bool threshold, bool demand, bool forecast)
    {
        var count = Convert.ToInt32(threshold) + Convert.ToInt32(demand) + Convert.ToInt32(forecast);
        if (count > 1)
            return ReplenishmentTriggerTypeEnum.Hybrid;
        if (demand)
            return ReplenishmentTriggerTypeEnum.Demand;
        if (forecast)
            return ReplenishmentTriggerTypeEnum.Forecast;
        return ReplenishmentTriggerTypeEnum.Threshold;
    }

    private static MovementTaskPriorityEnum ResolvePriority(
        decimal pickFaceQty,
        decimal effectivePickFaceQty,
        decimal triggerQty,
        decimal demandQty,
        decimal forecastQty,
        DateTime? dueAt)
    {
        if (pickFaceQty <= 0m
            || dueAt.HasValue && dueAt.Value <= Now.AddHours(4)
            || demandQty > 0m && effectivePickFaceQty < demandQty)
        {
            return MovementTaskPriorityEnum.Urgent;
        }

        if (demandQty > 0m
            || forecastQty > triggerQty
            || triggerQty > 0m && effectivePickFaceQty <= triggerQty * 0.5m)
        {
            return MovementTaskPriorityEnum.High;
        }

        return MovementTaskPriorityEnum.Normal;
    }

    private static DateTime ResolveDefaultDueAt(MovementTaskPriorityEnum priority)
        => priority switch
        {
            MovementTaskPriorityEnum.Urgent => Now.AddHours(2),
            MovementTaskPriorityEnum.High => Now.AddHours(8),
            MovementTaskPriorityEnum.Normal => Now.AddHours(24),
            _ => Now.AddDays(2)
        };

    private static int CalculateRoutePriorityScore(
        MovementTaskPriorityEnum priority,
        DateTime? dueAt,
        int travelScore,
        decimal demandQty,
        decimal forecastQty,
        decimal pickFaceQty)
    {
        var priorityScore = priority switch
        {
            MovementTaskPriorityEnum.Urgent => 100,
            MovementTaskPriorityEnum.High => 75,
            MovementTaskPriorityEnum.Normal => 50,
            MovementTaskPriorityEnum.Low => 25,
            _ => 50
        };
        var urgencyScore = TaskInterleavingService.CalculateUrgencyScore(dueAt);
        var demandScore = demandQty > pickFaceQty ? 30 : forecastQty > 0m ? 15 : 0;
        return Math.Clamp(priorityScore * 4 + urgencyScore * 2 + travelScore + demandScore, 0, 1_000);
    }

    private static DateTime? ResolveDemandDueAt(DateTime? requestedDeliveryDate, int? slaHours, DateTime createdAt)
    {
        if (requestedDeliveryDate.HasValue)
            return requestedDeliveryDate.Value.Date.AddHours(17);
        if (slaHours.HasValue)
            return createdAt.AddHours(slaHours.Value);
        return null;
    }

    private static string BuildSuggestionReason(
        ReplenishmentTriggerTypeEnum triggerType,
        MovementTaskPriorityEnum priority,
        decimal pickFaceQty,
        decimal openReplenishmentQty,
        decimal demandQty,
        decimal forecastQty,
        bool hasActiveLpn)
    {
        var lpnNote = hasActiveLpn ? " Nguồn đang LPN-managed; ưu tiên di chuyển nguyên kiện." : "";
        return triggerType switch
        {
            ReplenishmentTriggerTypeEnum.Demand => $"Demand-based replenishment: demand {demandQty:N2}, pick-face {pickFaceQty:N2}, open task {openReplenishmentQty:N2}, priority {priority}.{lpnNote}",
            ReplenishmentTriggerTypeEnum.Forecast => $"Forecast replenishment: forecast {forecastQty:N2}, pick-face {pickFaceQty:N2}, open task {openReplenishmentQty:N2}, priority {priority}.{lpnNote}",
            ReplenishmentTriggerTypeEnum.Hybrid => $"Hybrid replenishment: demand {demandQty:N2}, forecast {forecastQty:N2}, pick-face {pickFaceQty:N2}, open task {openReplenishmentQty:N2}, priority {priority}.{lpnNote}",
            _ => $"Threshold replenishment: pick-face {pickFaceQty:N2}, open task {openReplenishmentQty:N2}, priority {priority}.{lpnNote}"
        };
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record DemandSnapshot(decimal Quantity, DateTime? EarliestDueAt)
    {
        public static readonly DemandSnapshot Empty = new(0m, null);
    }
}
