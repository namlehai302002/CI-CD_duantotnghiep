using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public sealed record OptimizationDashboardSnapshot(
    List<OptimizationRun> Runs,
    List<OptimizationRecommendationLine> Recommendations,
    List<WavelessReleaseQueue> WavelessQueue,
    List<PickPathPlan> PickPathPlans,
    List<ToteClusterPlan> ToteClusterPlans);

public interface IOptimizationEnterpriseService
{
    Task<OptimizationRun> RunSlottingOptimizationAsync(int warehouseId, int? scopedWarehouseId, string actor, CancellationToken ct = default);
    Task<OptimizationRun> RunWaveOptimizationAsync(int warehouseId, int? scopedWarehouseId, string actor, CancellationToken ct = default);
    Task<int> RunWavelessReleaseAsync(int warehouseId, int maxTasks, int? scopedWarehouseId, string actor, CancellationToken ct = default);
    Task<PickPathPlan> GeneratePickPathPlanAsync(int warehouseId, int? scopedWarehouseId, string actor, CancellationToken ct = default);
    Task<ToteClusterPlan> CreateToteClusterPlanAsync(int warehouseId, int? scopedWarehouseId, string actor, CancellationToken ct = default);
    Task<OptimizationDashboardSnapshot> GetDashboardAsync(int? warehouseId, int? scopedWarehouseId, CancellationToken ct = default);
}

public interface IAutomationEnterpriseService
{
    Task<MheAdapterProfile> SaveAdapterProfileAsync(int warehouseId, MheSystemTypeEnum adapterType, string adapterCode, string adapterName, bool isSimulator, int? scopedWarehouseId, string actor, CancellationToken ct = default);
    Task<MheTelemetryEvent> RecordTelemetryAsync(int warehouseId, string equipmentCode, AutomationTelemetryTypeEnum telemetryType, string statusText, int throughputPerHour, int downtimeMinutes, string? errorCode, string? message, int? scopedWarehouseId, CancellationToken ct = default);
    Task<WcsSimulatorRun> RunWcsSimulatorAsync(int warehouseId, WcsSimulatorScenarioEnum scenario, int? scopedWarehouseId, string actor, CancellationToken ct = default);
    Task<AutomationOverride> OverrideMheCommandAsync(long commandId, AutomationOverrideActionEnum action, string reason, int? scopedWarehouseId, string actor, CancellationToken ct = default);
}

public interface IEnterpriseIntegrationService
{
    object BuildOpenApiContract();
    Task<EdiMessage> ImportEdiAsync(EdiMessageTypeEnum messageType, string payload, string? fileName, int? warehouseId, int? partnerId, string actor, CancellationToken ct = default);
    Task<EdiMessage> ExportEdiAsync(EdiMessageTypeEnum messageType, long? referenceId, int? warehouseId, int? partnerId, string actor, CancellationToken ct = default);
    Task<EdiMessage> ReplayEdiAsync(long ediMessageId, string actor, CancellationToken ct = default);
    Task<WebhookSubscription> SaveWebhookSubscriptionAsync(string eventType, string targetUrl, string signingSecret, string actor, CancellationToken ct = default);
    Task<WebhookDelivery> EnqueueWebhookAsync(string eventType, object payload, CancellationToken ct = default);
    Task<WebhookDelivery> ReplayWebhookAsync(long deliveryId, string actor, CancellationToken ct = default);
    Task<List<EnterpriseConnector>> EnsureConnectorPackAsync(string actor, CancellationToken ct = default);
    Task<EnterpriseConnector> CheckConnectorHealthAsync(int connectorId, string actor, CancellationToken ct = default);
    Task<IntegrationOutbox> EmitOutboxEventAsync(OutboxEventTypeEnum eventType, string targetEndpoint, object payload, string idempotencyKey, string targetSystem, CancellationToken ct = default);
}

public sealed class OptimizationEnterpriseService : IOptimizationEnterpriseService
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public OptimizationEnterpriseService(AppDbContext db, IUnitOfWork? unitOfWork = null)
    {
        _db = db;
        _unitOfWork = unitOfWork ?? new EfUnitOfWork(db);
    }

    private static DateTime Now => VietnamTime.Now;

    public async Task<OptimizationRun> RunSlottingOptimizationAsync(int warehouseId, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        EnsureWarehouseScope(warehouseId, scopedWarehouseId);
        var locations = await _db.Locations.AsNoTracking()
            .Include(l => l.Zone)
            .Where(l => l.IsActive && l.Zone.WarehouseId == warehouseId)
            .ToListAsync(ct);
        var rows = await _db.ItemLocations.AsNoTracking()
            .Include(il => il.Item)
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Where(il => il.Quantity > 0 && il.Location != null && il.Location.Zone.WarehouseId == warehouseId)
            .ToListAsync(ct);
        var velocities = await _db.ItemVelocityClassifications.AsNoTracking()
            .Where(v => v.WarehouseId == warehouseId && v.IsActive)
            .ToListAsync(ct);

        var run = await NewRunAsync(warehouseId, null, OptimizationRunTypeEnum.Slotting, actor, ct);
        var lineNo = 1;
        foreach (var group in rows.GroupBy(r => r.ItemId))
        {
            var item = group.First().Item;
            if (item == null)
                continue;
            var current = item.DefaultLocationId.HasValue
                ? locations.FirstOrDefault(l => l.LocationId == item.DefaultLocationId.Value)
                : group.OrderByDescending(x => x.Quantity).Select(x => x.Location).FirstOrDefault();
            var velocity = velocities.Where(v => v.ItemId == item.ItemId).OrderByDescending(v => v.LastAnalyzedAt).FirstOrDefault();
            var currentScore = current == null ? 0 : ScoreSlot(item, velocity, current, group.Sum(x => x.Quantity));
            var best = locations
                .Select(l => new { Location = l, Score = ScoreSlot(item, velocity, l, group.Sum(x => x.Quantity)) })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Location.AisleSequence)
                .FirstOrDefault();
            if (best == null || current?.LocationId == best.Location.LocationId)
                continue;

            var before = EstimateDistance(current);
            var after = EstimateDistance(best.Location);
            var saved = Math.Max(0m, before - after) + Math.Max(0, best.Score - currentScore) / 10m;
            run.Lines.Add(new OptimizationRecommendationLine
            {
                LineType = OptimizationLineTypeEnum.SlottingSuggestion,
                WarehouseId = warehouseId,
                OwnerPartnerId = item.OwnerPartnerId,
                ItemId = item.ItemId,
                SourceLocationId = current?.LocationId,
                SuggestedLocationId = best.Location.LocationId,
                Sequence = lineNo++,
                Score = best.Score,
                Quantity = group.Sum(x => x.Quantity),
                BeforeDistance = before,
                AfterDistance = after,
                EstimatedMinutesSaved = saved,
                Reason = $"Velocity {velocity?.CombinedClass ?? item.AbcClass ?? "C"}; affinity/size/capacity score {best.Score}; replenishment cost reduced.",
                StatusText = best.Score > currentScore ? "Recommend" : "Review"
            });
        }

        CompleteRun(run);
        _db.OptimizationRuns.Add(run);
        await _unitOfWork.SaveChangesAsync(ct);
        return run;
    }

    public async Task<OptimizationRun> RunWaveOptimizationAsync(int warehouseId, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        EnsureWarehouseScope(warehouseId, scopedWarehouseId);
        var tasks = await PendingPickTasks(warehouseId)
            .Include(t => t.Voucher)
            .Include(t => t.SourceLocation)
            .OrderByDescending(t => t.Voucher.Priority)
            .ThenBy(t => t.DueAt ?? t.Voucher.RequestedDeliveryDate ?? DateTime.MaxValue)
            .ThenBy(t => t.PickTaskId)
            .Take(300)
            .ToListAsync(ct);
        var run = await NewRunAsync(warehouseId, null, OptimizationRunTypeEnum.WavePlanning, actor, ct);
        var seq = 1;
        foreach (var group in tasks.GroupBy(t => new
        {
            t.OwnerPartnerId,
            Customer = t.Voucher.PartnerId,
            Carrier = t.Voucher.CarrierName ?? "",
            Route = t.Voucher.SlaCode ?? t.Voucher.ServiceLevel.ToString()
        }))
        {
            var available = group.All(t => HasInventoryForTask(t));
            var due = group.Min(t => t.DueAt ?? t.Voucher.RequestedDeliveryDate?.AddHours(t.Voucher.SlaHours ?? 24) ?? Now.AddDays(1));
            var score = group.Max(t => t.Voucher.Priority) + (due <= Now.AddHours(8) ? 30 : 0) + Math.Min(20, group.Count());
            run.Lines.Add(new OptimizationRecommendationLine
            {
                LineType = OptimizationLineTypeEnum.WaveCandidate,
                WarehouseId = warehouseId,
                OwnerPartnerId = group.Key.OwnerPartnerId,
                GroupKey = $"{group.Key.OwnerPartnerId}|{group.Key.Customer}|{group.Key.Carrier}|{group.Key.Route}",
                Sequence = seq++,
                Score = score,
                Quantity = group.Sum(x => x.TargetQty),
                InventoryAvailable = available,
                IsOwnerSafe = group.Select(x => x.OwnerPartnerId).Distinct().Count() <= 1,
                Reason = $"Grouped by owner/customer/carrier/route/SLA; tasks={group.Count()}, due={due:g}.",
                StatusText = available ? "ReadyToWave" : "InventoryShort"
            });
        }

        CompleteRun(run);
        _db.OptimizationRuns.Add(run);
        await _unitOfWork.SaveChangesAsync(ct);
        return run;
    }

    public async Task<int> RunWavelessReleaseAsync(int warehouseId, int maxTasks, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        EnsureWarehouseScope(warehouseId, scopedWarehouseId);
        maxTasks = Math.Clamp(maxTasks, 1, 200);
        var tasks = await PendingPickTasks(warehouseId)
            .Include(t => t.Voucher)
            .OrderByDescending(t => t.Voucher.Priority)
            .ThenBy(t => t.DueAt ?? t.Voucher.RequestedDeliveryDate ?? DateTime.MaxValue)
            .Take(maxTasks * 3)
            .ToListAsync(ct);

        var released = 0;
        foreach (var task in tasks)
        {
            var key = $"waveless:{task.PickTaskId}";
            if (await _db.WavelessReleaseQueue.AnyAsync(q => q.IdempotencyKey == key, ct))
                continue;
            var available = HasInventoryForTask(task);
            var due = task.DueAt ?? task.Voucher.RequestedDeliveryDate?.AddHours(task.Voucher.SlaHours ?? 24);
            var priority = task.Voucher.Priority + (due.HasValue && due.Value <= Now.AddHours(8) ? 40 : 0);
            var queue = new WavelessReleaseQueue
            {
                WarehouseId = warehouseId,
                OwnerPartnerId = task.OwnerPartnerId,
                VoucherId = task.VoucherId,
                PickTaskId = task.PickTaskId,
                IdempotencyKey = key,
                PriorityScore = priority,
                SlaDueAt = due,
                InventoryAvailable = available,
                Status = available ? WavelessQueueStatusEnum.Released : WavelessQueueStatusEnum.Blocked,
                BlockReason = available ? null : "Insufficient available inventory.",
                ReleasedAt = available ? Now : null,
                ReleasedBy = available ? CleanActor(actor) : null
            };
            _db.WavelessReleaseQueue.Add(queue);
            if (available && task.Status == PickTaskStatusEnum.Pending)
            {
                task.Status = PickTaskStatusEnum.Assigned;
                task.AssignedAt ??= Now;
                task.AssignedTo ??= "waveless";
                released++;
            }

            if (released >= maxTasks)
                break;
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return released;
    }

    public async Task<PickPathPlan> GeneratePickPathPlanAsync(int warehouseId, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        EnsureWarehouseScope(warehouseId, scopedWarehouseId);
        var tasks = await PendingPickTasks(warehouseId)
            .Include(t => t.SourceLocation)
            .OrderBy(t => t.PickTaskId)
            .Take(100)
            .ToListAsync(ct);
        var original = tasks.ToList();
        var optimized = tasks
            .OrderBy(t => t.SourceLocation.AisleSequence)
            .ThenBy(t => t.SourceLocation.RackCode)
            .ThenBy(t => t.SourceLocation.ShelfCode)
            .ThenBy(t => t.SourceLocation.BinCode)
            .ToList();
        var before = EstimateRoute(original.Select(t => t.SourceLocation));
        var after = EstimateRoute(optimized.Select(t => t.SourceLocation));
        var plan = new PickPathPlan
        {
            PlanCode = await GenerateCodeAsync("PATH", ct),
            WarehouseId = warehouseId,
            OwnerPartnerId = optimized.Select(t => t.OwnerPartnerId).Distinct().Count() == 1 ? optimized.FirstOrDefault()?.OwnerPartnerId : null,
            BeforeDistance = before,
            AfterDistance = after,
            DistanceSaved = Math.Max(0m, before - after),
            StopCount = optimized.Count,
            PickTaskIdsJson = JsonSerializer.Serialize(optimized.Select(t => t.PickTaskId)),
            CreatedBy = CleanActor(actor),
            CreatedAt = Now
        };
        var previous = (Location?)null;
        var seq = 1;
        foreach (var task in optimized)
        {
            plan.Stops.Add(new PickPathPlanStop
            {
                Sequence = seq++,
                PickTaskId = task.PickTaskId,
                LocationId = task.SourceLocationId,
                DistanceFromPrevious = previous == null ? EstimateDistance(task.SourceLocation) : Math.Abs(EstimateDistance(task.SourceLocation) - EstimateDistance(previous))
            });
            previous = task.SourceLocation;
        }

        _db.PickPathPlans.Add(plan);
        await _unitOfWork.SaveChangesAsync(ct);
        return plan;
    }

    public async Task<ToteClusterPlan> CreateToteClusterPlanAsync(int warehouseId, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        EnsureWarehouseScope(warehouseId, scopedWarehouseId);
        var tasks = await PendingPickTasks(warehouseId)
            .Include(t => t.Voucher)
            .OrderByDescending(t => t.Voucher.Priority)
            .Take(80)
            .ToListAsync(ct);
        var group = tasks
            .GroupBy(t => new { t.OwnerPartnerId, Customer = t.Voucher.PartnerId, Carrier = t.Voucher.CarrierName ?? "" })
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();
        var plan = new ToteClusterPlan
        {
            PlanCode = await GenerateCodeAsync("TOTE", ct),
            WarehouseId = warehouseId,
            OwnerPartnerId = group?.Key.OwnerPartnerId,
            CustomerKey = group?.Key.Customer?.ToString(),
            CarrierCode = group?.Key.Carrier,
            RequiresToteScan = true,
            CreatedBy = CleanActor(actor),
            CreatedAt = Now
        };

        var totes = await _db.PickTotes
            .Where(t => t.Status == PickToteStatusEnum.Empty && (t.PickCart == null || t.PickCart.WarehouseId == warehouseId))
            .OrderBy(t => t.ToteCode)
            .Take(group?.Count() ?? 0)
            .ToListAsync(ct);
        var i = 0;
        foreach (var task in group ?? Enumerable.Empty<PickTask>())
        {
            var tote = i < totes.Count ? totes[i] : null;
            plan.Assignments.Add(new ToteClusterAssignment
            {
                PickToteId = tote?.PickToteId,
                ToteCode = tote?.ToteCode ?? $"PENDING-{task.PickTaskId}",
                VoucherId = task.VoucherId,
                PickTaskId = task.PickTaskId,
                OwnerPartnerId = task.OwnerPartnerId,
                CustomerKey = task.Voucher.PartnerId?.ToString(),
                IsScanned = false
            });
            if (tote != null)
            {
                tote.Status = PickToteStatusEnum.Assigned;
                tote.VoucherId = task.VoucherId;
                tote.AssignedAt = Now;
                tote.AssignedBy = CleanActor(actor);
            }

            i++;
        }

        plan.AssignmentCount = plan.Assignments.Count;
        _db.ToteClusterPlans.Add(plan);
        await _unitOfWork.SaveChangesAsync(ct);
        return plan;
    }

    public async Task<OptimizationDashboardSnapshot> GetDashboardAsync(int? warehouseId, int? scopedWarehouseId, CancellationToken ct = default)
    {
        if (scopedWarehouseId.HasValue)
            warehouseId = scopedWarehouseId;
        return new OptimizationDashboardSnapshot(
            await _db.OptimizationRuns.AsNoTracking().Include(x => x.Warehouse).Where(x => !warehouseId.HasValue || x.WarehouseId == warehouseId.Value).OrderByDescending(x => x.CreatedAt).Take(20).ToListAsync(ct),
            await _db.OptimizationRecommendationLines.AsNoTracking().Include(x => x.Item).Include(x => x.SourceLocation).Include(x => x.SuggestedLocation).Where(x => !warehouseId.HasValue || x.WarehouseId == warehouseId.Value).OrderByDescending(x => x.Score).Take(100).ToListAsync(ct),
            await _db.WavelessReleaseQueue.AsNoTracking().Include(x => x.PickTask).Where(x => !warehouseId.HasValue || x.WarehouseId == warehouseId.Value).OrderByDescending(x => x.CreatedAt).Take(80).ToListAsync(ct),
            await _db.PickPathPlans.AsNoTracking().Where(x => !warehouseId.HasValue || x.WarehouseId == warehouseId.Value).OrderByDescending(x => x.CreatedAt).Take(20).ToListAsync(ct),
            await _db.ToteClusterPlans.AsNoTracking().Include(x => x.Assignments).Where(x => !warehouseId.HasValue || x.WarehouseId == warehouseId.Value).OrderByDescending(x => x.CreatedAt).Take(20).ToListAsync(ct));
    }

    private IQueryable<PickTask> PendingPickTasks(int warehouseId)
        => _db.PickTasks
            .Where(t => t.SourceLocation.Zone.WarehouseId == warehouseId
                && t.Status != PickTaskStatusEnum.Completed
                && t.Status != PickTaskStatusEnum.Cancelled);

    private bool HasInventoryForTask(PickTask task)
        => _db.ItemLocations.Any(il => il.ItemId == task.ItemId
            && il.LocationId == task.SourceLocationId
            && il.HoldStatus == InventoryHoldStatusEnum.Available
            && il.Quantity - il.ReservedQty >= task.TargetQty);

    private static int ScoreSlot(Item item, ItemVelocityClassification? velocity, Location location, decimal qty)
    {
        var abc = velocity?.AbcClass ?? (string.IsNullOrWhiteSpace(item.AbcClass) ? 'C' : char.ToUpperInvariant(item.AbcClass[0]));
        var velocityScore = abc == 'A' ? 35 : abc == 'B' ? 22 : 10;
        var goldenScore = location.IsGoldenZone ? 25 : 5;
        var heightScore = item.Weight.GetValueOrDefault() >= 20m ? Math.Max(0, 20 - location.HeightLevel * 4) : Math.Max(0, 15 - Math.Abs(location.HeightLevel - 2) * 3);
        var capacity = location.MaxCapacity <= 0 ? 999999m : location.MaxCapacity;
        var capacityScore = location.CurrentLoad + qty <= capacity ? 20 : -40;
        var mechanicalScore = item.Weight.GetValueOrDefault() >= 20m && location.AllowMechanicalHandling ? 10 : 0;
        return Math.Clamp(velocityScore + goldenScore + heightScore + capacityScore + mechanicalScore, 0, 100);
    }

    private static decimal EstimateRoute(IEnumerable<Location> locations)
    {
        decimal total = 0;
        Location? previous = null;
        foreach (var location in locations)
        {
            var distance = EstimateDistance(location);
            total += previous == null ? distance : Math.Abs(distance - EstimateDistance(previous));
            previous = location;
        }
        return total;
    }

    private static decimal EstimateDistance(Location? location)
        => location == null ? 999m : location.AisleSequence * 25m + location.HeightLevel * 2m + ParseRack(location.RackCode) * 5m + ParseRack(location.BinCode);

    private static int ParseRack(string? value)
        => int.TryParse(new string((value ?? "").Where(char.IsDigit).ToArray()), out var result) ? result : 0;

    private async Task<OptimizationRun> NewRunAsync(int warehouseId, int? ownerPartnerId, OptimizationRunTypeEnum type, string actor, CancellationToken ct)
        => new()
        {
            RunCode = await GenerateCodeAsync(type.ToString()[..Math.Min(type.ToString().Length, 4)].ToUpperInvariant(), ct),
            WarehouseId = warehouseId,
            OwnerPartnerId = ownerPartnerId,
            RunType = type,
            Status = OptimizationRunStatusEnum.Draft,
            CreatedBy = CleanActor(actor),
            CreatedAt = Now
        };

    private static void CompleteRun(OptimizationRun run)
    {
        run.CandidateCount = run.Lines.Count;
        run.BeforeDistance = run.Lines.Sum(x => x.BeforeDistance);
        run.AfterDistance = run.Lines.Sum(x => x.AfterDistance);
        run.EstimatedMinutesSaved = run.Lines.Sum(x => x.EstimatedMinutesSaved);
        run.Status = OptimizationRunStatusEnum.Completed;
        run.CompletedAt = Now;
        run.ResultJson = JsonSerializer.Serialize(new { run.CandidateCount, run.BeforeDistance, run.AfterDistance, run.EstimatedMinutesSaved });
    }

    private async Task<string> GenerateCodeAsync(string prefix, CancellationToken ct)
    {
        var day = Now.ToString("yyyyMMdd");
        var count = await _db.OptimizationRuns.CountAsync(x => x.RunCode.StartsWith($"{prefix}-{day}-"), ct)
            + await _db.PickPathPlans.CountAsync(x => x.PlanCode.StartsWith($"{prefix}-{day}-"), ct)
            + await _db.ToteClusterPlans.CountAsync(x => x.PlanCode.StartsWith($"{prefix}-{day}-"), ct);
        return $"{prefix}-{day}-{count + 1:0000}";
    }

    private static void EnsureWarehouseScope(int warehouseId, int? scopedWarehouseId)
    {
        if (scopedWarehouseId.HasValue && scopedWarehouseId.Value != warehouseId)
            throw new UnauthorizedAccessException("Khong duoc toi uu kho ngoai pham vi duoc gan.");
    }

    private static string CleanActor(string? actor)
        => string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim()[..Math.Min(actor.Trim().Length, 100)];
}

public sealed class AutomationEnterpriseService : IAutomationEnterpriseService
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public AutomationEnterpriseService(AppDbContext db, IUnitOfWork? unitOfWork = null)
    {
        _db = db;
        _unitOfWork = unitOfWork ?? new EfUnitOfWork(db);
    }

    private static DateTime Now => VietnamTime.Now;

    public async Task<MheAdapterProfile> SaveAdapterProfileAsync(int warehouseId, MheSystemTypeEnum adapterType, string adapterCode, string adapterName, bool isSimulator, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        EnsureWarehouseScope(warehouseId, scopedWarehouseId);
        if (string.IsNullOrWhiteSpace(adapterCode) || string.IsNullOrWhiteSpace(adapterName))
            throw new BusinessRuleException("Adapter code/name la bat buoc.", "MHE_ADAPTER_REQUIRED", "MheAdapterProfile");
        var code = adapterCode.Trim().ToUpperInvariant();
        var profile = await _db.MheAdapterProfiles.FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && x.AdapterCode == code, ct);
        if (profile == null)
        {
            profile = new MheAdapterProfile { WarehouseId = warehouseId, AdapterCode = code };
            _db.MheAdapterProfiles.Add(profile);
        }

        profile.AdapterName = adapterName.Trim();
        profile.AdapterType = adapterType;
        profile.IsSimulator = isSimulator;
        profile.IsActive = true;
        profile.HealthStatus = profile.LastHeartbeatAt.HasValue ? "Healthy" : "Unknown";
        profile.CapabilitiesJson = JsonSerializer.Serialize(new
        {
            conveyor = true,
            sorter = true,
            asrs = adapterType is MheSystemTypeEnum.Wcs or MheSystemTypeEnum.Other,
            amr = adapterType == MheSystemTypeEnum.Amr,
            robot = adapterType is MheSystemTypeEnum.Robot or MheSystemTypeEnum.Amr,
            scale = true,
            camera = true,
            simulator = isSimulator,
            updatedBy = actor
        });
        await _unitOfWork.SaveChangesAsync(ct);
        return profile;
    }

    public async Task<MheTelemetryEvent> RecordTelemetryAsync(int warehouseId, string equipmentCode, AutomationTelemetryTypeEnum telemetryType, string statusText, int throughputPerHour, int downtimeMinutes, string? errorCode, string? message, int? scopedWarehouseId, CancellationToken ct = default)
    {
        EnsureWarehouseScope(warehouseId, scopedWarehouseId);
        var eventRow = new MheTelemetryEvent
        {
            WarehouseId = warehouseId,
            EquipmentCode = string.IsNullOrWhiteSpace(equipmentCode) ? "AUTO" : equipmentCode.Trim().ToUpperInvariant(),
            TelemetryType = telemetryType,
            StatusText = string.IsNullOrWhiteSpace(statusText) ? "OK" : statusText.Trim(),
            ThroughputPerHour = Math.Max(0, throughputPerHour),
            DowntimeMinutes = Math.Max(0, downtimeMinutes),
            ErrorCode = Clean(errorCode, 80),
            Message = Clean(message, 500),
            EventAt = Now
        };
        _db.MheTelemetryEvents.Add(eventRow);

        var profile = await _db.MheAdapterProfiles.FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && x.AdapterCode == eventRow.EquipmentCode, ct);
        if (profile != null)
        {
            profile.LastHeartbeatAt = telemetryType == AutomationTelemetryTypeEnum.Heartbeat ? eventRow.EventAt : profile.LastHeartbeatAt;
            profile.HealthStatus = telemetryType == AutomationTelemetryTypeEnum.Error ? "Down" : "Healthy";
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return eventRow;
    }

    public async Task<WcsSimulatorRun> RunWcsSimulatorAsync(int warehouseId, WcsSimulatorScenarioEnum scenario, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        EnsureWarehouseScope(warehouseId, scopedWarehouseId);
        var system = await _db.MheSystems.AsNoTracking().FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && x.IsActive, ct);
        var command = new MheCommand
        {
            CommandCode = $"SIM-{Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}",
            WarehouseId = warehouseId,
            MheSystemId = system?.MheSystemId,
            CommandType = scenario == WcsSimulatorScenarioEnum.SorterReject ? MheCommandTypeEnum.DivertPackage : MheCommandTypeEnum.RobotMission,
            Status = scenario == WcsSimulatorScenarioEnum.AcceptAndComplete ? MheCommandStatusEnum.Completed : MheCommandStatusEnum.Failed,
            SourceType = "WcsSimulator",
            SourceId = scenario.ToString(),
            SourceCode = scenario.ToString(),
            IdempotencyKey = $"wcs-sim:{warehouseId}:{scenario}:{Guid.NewGuid():N}",
            CorrelationId = Guid.NewGuid().ToString("N"),
            PayloadJson = JsonSerializer.Serialize(new { scenario, warehouseId }),
            CreatedBy = CleanActor(actor),
            CreatedAt = Now,
            SentAt = Now,
            AcknowledgedAt = Now,
            StartedAt = Now,
            CompletedAt = scenario == WcsSimulatorScenarioEnum.AcceptAndComplete ? Now : null,
            FailedAt = scenario == WcsSimulatorScenarioEnum.AcceptAndComplete ? null : Now,
            LastError = scenario == WcsSimulatorScenarioEnum.AcceptAndComplete ? null : scenario.ToString()
        };
        command.MissionEvents.Add(new MheMissionEvent
        {
            Status = command.Status,
            ExternalMissionId = $"SIM-{Guid.NewGuid():N}",
            Message = scenario.ToString(),
            IdempotencyKey = $"wcs-sim-callback:{command.CorrelationId}",
            PayloadJson = command.PayloadJson,
            EventAt = Now
        });
        _db.MheCommands.Add(command);

        var exceptionCount = 0;
        if (command.Status == MheCommandStatusEnum.Failed)
        {
            _db.OperationExceptionCases.Add(new OperationExceptionCase
            {
                ExceptionKey = $"AUTO-{command.CorrelationId}",
                CategoryKey = "AUTOMATION",
                CategoryLabel = "Ngoại lệ tự động hóa",
                WarehouseId = warehouseId,
                ReferenceCode = command.CommandCode,
                SecondaryReference = scenario.ToString(),
                Status = OperationExceptionStatusEnum.Open,
                FirstDetectedAt = Now,
                LastDetectedAt = Now
            });
            exceptionCount = 1;
        }

        var run = new WcsSimulatorRun
        {
            WarehouseId = warehouseId,
            MheSystemId = system?.MheSystemId,
            Scenario = scenario,
            StatusText = command.Status.ToString(),
            CommandsCreated = 1,
            CallbacksSent = 1,
            ExceptionsOpened = exceptionCount,
            ResultJson = JsonSerializer.Serialize(new { command.CommandCode, command.CorrelationId, command.Status }),
            CreatedBy = CleanActor(actor),
            CreatedAt = Now
        };
        _db.WcsSimulatorRuns.Add(run);
        await RecordTelemetryAsync(warehouseId, "SIMULATOR", command.Status == MheCommandStatusEnum.Failed ? AutomationTelemetryTypeEnum.Error : AutomationTelemetryTypeEnum.Heartbeat, command.Status.ToString(), command.Status == MheCommandStatusEnum.Completed ? 120 : 0, command.Status == MheCommandStatusEnum.Failed ? 5 : 0, command.LastError, scenario.ToString(), scopedWarehouseId, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return run;
    }

    public async Task<AutomationOverride> OverrideMheCommandAsync(long commandId, AutomationOverrideActionEnum action, string reason, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new BusinessRuleException("Ghi đè lệnh thiết bị cần có lý do của quản lý.", "AUTO_OVERRIDE_REASON_REQUIRED", "AutomationOverride");
        var command = await _db.MheCommands.FirstOrDefaultAsync(x => x.MheCommandId == commandId, ct)
            ?? throw new BusinessRuleException("Khong tim thay MHE command.", "MHE_COMMAND_NOT_FOUND", "MheCommand");
        EnsureWarehouseScope(command.WarehouseId, scopedWarehouseId);
        command.Status = action switch
        {
            AutomationOverrideActionEnum.Retry => MheCommandStatusEnum.Queued,
            AutomationOverrideActionEnum.Cancel => MheCommandStatusEnum.Cancelled,
            AutomationOverrideActionEnum.Complete => MheCommandStatusEnum.Completed,
            AutomationOverrideActionEnum.DeadLetter => MheCommandStatusEnum.DeadLetter,
            _ => command.Status
        };
        command.UpdatedAt = Now;
        command.LastError = action is AutomationOverrideActionEnum.Retry or AutomationOverrideActionEnum.Complete ? null : command.LastError;
        var row = new AutomationOverride
        {
            MheCommandId = commandId,
            Action = action,
            Reason = reason.Trim(),
            ApprovedBy = CleanActor(actor),
            ApprovedAt = Now
        };
        row.MheCommand = command;
        _db.AutomationOverrides.Add(row);
        await _unitOfWork.SaveChangesAsync(ct);
        return row;
    }

    private static void EnsureWarehouseScope(int warehouseId, int? scopedWarehouseId)
    {
        if (scopedWarehouseId.HasValue && scopedWarehouseId.Value != warehouseId)
            throw new UnauthorizedAccessException("Khong duoc thao tac automation ngoai kho duoc gan.");
    }

    private static string CleanActor(string? actor)
        => Clean(actor, 100) ?? "system";

    private static string? Clean(string? value, int max)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
}

public sealed class EnterpriseIntegrationService : IEnterpriseIntegrationService
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public EnterpriseIntegrationService(AppDbContext db, IUnitOfWork? unitOfWork = null)
    {
        _db = db;
        _unitOfWork = unitOfWork ?? new EfUnitOfWork(db);
    }

    private static DateTime Now => VietnamTime.Now;

    public object BuildOpenApiContract()
        => new
        {
            openapi = "3.0.3",
            info = new { title = "WMS Pro API", version = "v1", deprecationPolicy = "Backward compatible within /api/v1; deprecated fields remain for at least one release cycle." },
            paths = new Dictionary<string, object>
            {
                ["/api/v1/inbound/asn"] = new { post = new { summary = "Import inbound ASN", tags = new[] { "inbound", "edi" } } },
                ["/api/v1/outbound/orders"] = new { post = new { summary = "Create outbound order", tags = new[] { "outbound" } } },
                ["/api/v1/inventory/stock"] = new { get = new { summary = "Inventory stock contract", tags = new[] { "inventory" } } },
                ["/api/v1/shipments/{id}/confirm"] = new { post = new { summary = "Confirm shipment and emit event", tags = new[] { "shipment" } } },
                ["/api/v1/3pl/invoices/{id}/issue"] = new { post = new { summary = "Issue 3PL invoice event", tags = new[] { "3pl" } } },
                ["/api/v1/edi/import"] = new { post = new { summary = "Import EDI ASN/940/945/856", tags = new[] { "edi" } } },
                ["/api/v1/webhooks/{id}/replay"] = new { post = new { summary = "Replay webhook delivery", tags = new[] { "webhook" } } }
            }
        };

    public async Task<EdiMessage> ImportEdiAsync(EdiMessageTypeEnum messageType, string payload, string? fileName, int? warehouseId, int? partnerId, string actor, CancellationToken ct = default)
    {
        var control = ExtractControlNumber(payload, messageType);
        var errors = ValidateEdi(messageType, payload);
        var message = new EdiMessage
        {
            MessageType = messageType,
            Direction = EdiDirectionEnum.Inbound,
            Status = errors.Count == 0 ? EdiMessageStatusEnum.Validated : EdiMessageStatusEnum.Rejected,
            WarehouseId = warehouseId,
            PartnerId = partnerId,
            ControlNumber = control,
            SourceFileName = fileName,
            Payload = payload ?? "",
            ValidationErrorsJson = JsonSerializer.Serialize(errors),
            RejectReport = string.Join("; ", errors),
            CreatedBy = CleanActor(actor),
            CreatedAt = Now
        };
        _db.EdiMessages.Add(message);
        await _unitOfWork.SaveChangesAsync(ct);
        return message;
    }

    public async Task<EdiMessage> ExportEdiAsync(EdiMessageTypeEnum messageType, long? referenceId, int? warehouseId, int? partnerId, string actor, CancellationToken ct = default)
    {
        var payload = $"ISA*00*          *00*          *ZZ*WMSPRO         *ZZ*PARTNER        *{Now:yyMMdd}*{Now:HHmm}*U*00401*{Now:yyyyMMddHHmmss}*0*T*>~ST*{EdiTransactionCode(messageType)}*0001~REF*WMS*{referenceId?.ToString() ?? "NA"}~SE*3*0001~IEA*1*{Now:yyyyMMddHHmmss}~";
        var message = new EdiMessage
        {
            MessageType = messageType,
            Direction = EdiDirectionEnum.Outbound,
            Status = EdiMessageStatusEnum.Exported,
            WarehouseId = warehouseId,
            PartnerId = partnerId,
            ControlNumber = $"EXP-{Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}",
            Payload = payload,
            CreatedBy = CleanActor(actor),
            CreatedAt = Now
        };
        _db.EdiMessages.Add(message);
        await _unitOfWork.SaveChangesAsync(ct);
        return message;
    }

    public async Task<EdiMessage> ReplayEdiAsync(long ediMessageId, string actor, CancellationToken ct = default)
    {
        var original = await _db.EdiMessages.AsNoTracking().FirstOrDefaultAsync(x => x.EdiMessageId == ediMessageId, ct)
            ?? throw new BusinessRuleException("Khong tim thay EDI message.", "EDI_NOT_FOUND", "EdiMessage");
        var replay = new EdiMessage
        {
            MessageType = original.MessageType,
            Direction = original.Direction,
            Status = EdiMessageStatusEnum.Replayed,
            WarehouseId = original.WarehouseId,
            PartnerId = original.PartnerId,
            ControlNumber = $"{original.ControlNumber}-R{Now:HHmmss}",
            SourceFileName = original.SourceFileName,
            Payload = original.Payload,
            ValidationErrorsJson = original.ValidationErrorsJson,
            RejectReport = original.RejectReport,
            ReplayOfMessageId = original.EdiMessageId,
            CreatedBy = CleanActor(actor),
            CreatedAt = Now,
            ReplayedAt = Now
        };
        _db.EdiMessages.Add(replay);
        await _unitOfWork.SaveChangesAsync(ct);
        return replay;
    }

    public async Task<WebhookSubscription> SaveWebhookSubscriptionAsync(string eventType, string targetUrl, string signingSecret, string actor, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(eventType) || string.IsNullOrWhiteSpace(targetUrl) || string.IsNullOrWhiteSpace(signingSecret))
            throw new BusinessRuleException("Webhook can event, URL va signing secret.", "WEBHOOK_REQUIRED", "WebhookSubscription");
        var code = $"WH-{eventType.Trim().ToUpperInvariant()}-{Math.Abs(targetUrl.Trim().ToUpperInvariant().GetHashCode())}";
        var subscription = await _db.WebhookSubscriptions.FirstOrDefaultAsync(x => x.SubscriptionCode == code, ct);
        if (subscription == null)
        {
            subscription = new WebhookSubscription { SubscriptionCode = code, CreatedBy = CleanActor(actor), CreatedAt = Now };
            _db.WebhookSubscriptions.Add(subscription);
        }

        subscription.EventType = eventType.Trim();
        subscription.TargetUrl = targetUrl.Trim();
        subscription.SigningSecret = signingSecret.Trim();
        subscription.IsActive = true;
        await _unitOfWork.SaveChangesAsync(ct);
        return subscription;
    }

    public async Task<WebhookDelivery> EnqueueWebhookAsync(string eventType, object payload, CancellationToken ct = default)
    {
        var subscription = await _db.WebhookSubscriptions.FirstOrDefaultAsync(x => x.EventType == eventType && x.IsActive, ct)
            ?? throw new BusinessRuleException("Chua co webhook subscription cho event.", "WEBHOOK_SUBSCRIPTION_NOT_FOUND", "WebhookSubscription");
        var payloadJson = JsonSerializer.Serialize(payload);
        var delivery = new WebhookDelivery
        {
            WebhookSubscriptionId = subscription.WebhookSubscriptionId,
            EventType = eventType,
            IdempotencyKey = $"webhook:{eventType}:{Guid.NewGuid():N}",
            PayloadJson = payloadJson,
            Signature = Sign(payloadJson, subscription.SigningSecret),
            Status = subscription.TargetUrl.StartsWith("mock://", StringComparison.OrdinalIgnoreCase) ? WebhookDeliveryStatusEnum.Sent : WebhookDeliveryStatusEnum.Pending,
            CreatedAt = Now,
            SentAt = subscription.TargetUrl.StartsWith("mock://", StringComparison.OrdinalIgnoreCase) ? Now : null,
            NextRetryAt = subscription.TargetUrl.StartsWith("mock://", StringComparison.OrdinalIgnoreCase) ? null : Now.AddMinutes(5)
        };
        _db.WebhookDeliveries.Add(delivery);
        await EmitOutboxEventAsync(OutboxEventTypeEnum.WebhookDelivery, subscription.TargetUrl, payload, delivery.IdempotencyKey, "Webhook", ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return delivery;
    }

    public async Task<WebhookDelivery> ReplayWebhookAsync(long deliveryId, string actor, CancellationToken ct = default)
    {
        var original = await _db.WebhookDeliveries.Include(x => x.Subscription).FirstOrDefaultAsync(x => x.WebhookDeliveryId == deliveryId, ct)
            ?? throw new BusinessRuleException("Khong tim thay webhook delivery.", "WEBHOOK_DELIVERY_NOT_FOUND", "WebhookDelivery");
        var replay = new WebhookDelivery
        {
            WebhookSubscriptionId = original.WebhookSubscriptionId,
            EventType = original.EventType,
            IdempotencyKey = $"webhook-replay:{deliveryId}:{Guid.NewGuid():N}",
            PayloadJson = original.PayloadJson,
            Signature = Sign(original.PayloadJson, original.Subscription.SigningSecret),
            Status = WebhookDeliveryStatusEnum.Pending,
            CreatedAt = Now,
            NextRetryAt = Now
        };
        _db.WebhookDeliveries.Add(replay);
        await _unitOfWork.SaveChangesAsync(ct);
        return replay;
    }

    public async Task<List<EnterpriseConnector>> EnsureConnectorPackAsync(string actor, CancellationToken ct = default)
    {
        var result = new List<EnterpriseConnector>();
        foreach (var type in new[] { EnterpriseConnectorTypeEnum.Erp, EnterpriseConnectorTypeEnum.Tms, EnterpriseConnectorTypeEnum.Oms })
        {
            var code = $"{type.ToString().ToUpperInvariant()}-MOCK";
            var connector = await _db.EnterpriseConnectors.FirstOrDefaultAsync(x => x.ConnectorType == type && x.ConnectorCode == code, ct);
            if (connector == null)
            {
                connector = new EnterpriseConnector
                {
                    ConnectorType = type,
                    ConnectorCode = code,
                    ConnectorName = $"{type} mock connector",
                    EndpointUrl = $"mock://{type.ToString().ToLowerInvariant()}",
                    IsMock = true,
                    IsActive = true,
                    HealthStatus = EnterpriseConnectorHealthEnum.Healthy,
                    LastHealthCheckAt = Now,
                    CreatedBy = CleanActor(actor),
                    CreatedAt = Now
                };
                _db.EnterpriseConnectors.Add(connector);
            }

            result.Add(connector);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return result;
    }

    public async Task<EnterpriseConnector> CheckConnectorHealthAsync(int connectorId, string actor, CancellationToken ct = default)
    {
        var connector = await _db.EnterpriseConnectors.FirstOrDefaultAsync(x => x.EnterpriseConnectorId == connectorId, ct)
            ?? throw new BusinessRuleException("Khong tim thay connector.", "CONNECTOR_NOT_FOUND", "EnterpriseConnector");
        connector.LastHealthCheckAt = Now;
        connector.HealthStatus = connector.IsMock || (connector.EndpointUrl?.StartsWith("mock://", StringComparison.OrdinalIgnoreCase) ?? false)
            ? EnterpriseConnectorHealthEnum.Healthy
            : EnterpriseConnectorHealthEnum.Warning;
        connector.LastError = connector.HealthStatus == EnterpriseConnectorHealthEnum.Healthy ? null : "Kiểm tra kết nối thật cần cấu hình địa chỉ nhận bên ngoài.";
        _db.EnterpriseConnectorDeliveries.Add(new EnterpriseConnectorDelivery
        {
            EnterpriseConnectorId = connector.EnterpriseConnectorId,
            EventType = "HealthCheck",
            IdempotencyKey = $"connector-health:{connector.EnterpriseConnectorId}:{Guid.NewGuid():N}",
            PayloadJson = JsonSerializer.Serialize(new { connector.ConnectorCode, connector.HealthStatus, checkedBy = actor }),
            Status = OutboxStatusEnum.Sent,
            CreatedAt = Now,
            ProcessedAt = Now
        });
        await _unitOfWork.SaveChangesAsync(ct);
        return connector;
    }

    public async Task<IntegrationOutbox> EmitOutboxEventAsync(OutboxEventTypeEnum eventType, string targetEndpoint, object payload, string idempotencyKey, string targetSystem, CancellationToken ct = default)
    {
        var existing = await _db.IntegrationOutbox.FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey && x.EventType == eventType.ToString(), ct);
        if (existing != null)
            return existing;
        var row = new IntegrationOutbox
        {
            EventType = eventType.ToString(),
            TargetEndpoint = targetEndpoint,
            Payload = JsonSerializer.Serialize(payload),
            HttpMethod = "POST",
            Status = targetEndpoint.StartsWith("mock://", StringComparison.OrdinalIgnoreCase) ? OutboxStatusEnum.Sent : OutboxStatusEnum.Pending,
            IdempotencyKey = idempotencyKey,
            TargetSystem = targetSystem,
            CorrelationId = Guid.NewGuid().ToString("N"),
            CreatedBy = "enterprise-integration",
            CreatedAt = Now,
            ProcessedAt = targetEndpoint.StartsWith("mock://", StringComparison.OrdinalIgnoreCase) ? Now : null
        };
        _db.IntegrationOutbox.Add(row);
        await _unitOfWork.SaveChangesAsync(ct);
        return row;
    }

    private static List<string> ValidateEdi(EdiMessageTypeEnum type, string? payload)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(payload))
            errors.Add("Payload is required.");
        var transactionCode = EdiTransactionCode(type);
        if (!(payload ?? "").Contains($"ST*{transactionCode}", StringComparison.OrdinalIgnoreCase))
            errors.Add($"Missing ST*{transactionCode} transaction header.");
        if (!(payload ?? "").Contains("SE*", StringComparison.OrdinalIgnoreCase))
            errors.Add("Missing SE transaction trailer.");
        return errors;
    }

    private static string ExtractControlNumber(string? payload, EdiMessageTypeEnum type)
    {
        var token = (payload ?? "").Split('~', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(x => x.StartsWith($"ST*{EdiTransactionCode(type)}*", StringComparison.OrdinalIgnoreCase));
        var parts = token?.Split('*');
        return parts != null && parts.Length >= 3 ? parts[2].Trim() : $"EDI-{type}-{Guid.NewGuid():N}"[..40];
    }

    private static string EdiTransactionCode(EdiMessageTypeEnum type)
        => type switch
        {
            EdiMessageTypeEnum.Asn => "856",
            EdiMessageTypeEnum.Order940 => "940",
            EdiMessageTypeEnum.ShipAdvice945 => "945",
            EdiMessageTypeEnum.Shipment856 => "856",
            _ => "997"
        };

    private static string Sign(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static string CleanActor(string? actor)
        => string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim()[..Math.Min(actor.Trim().Length, 100)];
}
