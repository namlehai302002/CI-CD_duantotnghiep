using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.StaticFiles;
using WMS.Authorization;
using WMS.Common;
using WMS.Data;
using WMS.Models;
using WMS.ViewModels;

namespace WMS.Services;

public interface IOperationsScopeQueryService
{
    int? GetScopedWarehouseId(ClaimsPrincipal user);
    Task<List<int>?> GetScopedZoneIdsAsync(ClaimsPrincipal user);
    Task<List<Warehouse>> GetVisibleWarehousesAsync(ClaimsPrincipal user);
    Task<List<Location>> GetVisibleLocationsAsync(ClaimsPrincipal user, int? warehouseId);
}

public sealed class OperationsScopeQueryService(AppDbContext db) : IOperationsScopeQueryService
{
    public int? GetScopedWarehouseId(ClaimsPrincipal user)
    {
        if (user.IsInRole("Admin"))
            return null;

        var claim = user.FindFirst("WarehouseId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    public async Task<List<int>?> GetScopedZoneIdsAsync(ClaimsPrincipal user)
    {
        if (user.IsInRole("Admin") || user.IsInRole("Manager"))
            return null;

        var userName = user.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userName))
            return new List<int>();

        var appUser = await db.AppUsers.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == userName);
        if (appUser == null)
            return new List<int>();

        return await db.UserZoneAssignments.AsNoTracking()
            .Where(x => x.UserId == appUser.UserId)
            .Select(x => x.ZoneId)
            .ToListAsync();
    }

    public async Task<List<Warehouse>> GetVisibleWarehousesAsync(ClaimsPrincipal user)
    {
        var scopedWarehouseId = GetScopedWarehouseId(user);
        var query = db.Warehouses.AsNoTracking().Where(w => w.IsActive);

        if (scopedWarehouseId.HasValue)
            query = query.Where(w => w.WarehouseId == scopedWarehouseId.Value);

        return await query.OrderBy(w => w.WarehouseCode).ToListAsync();
    }

    public async Task<List<Location>> GetVisibleLocationsAsync(ClaimsPrincipal user, int? warehouseId)
    {
        var scopedWarehouseId = GetScopedWarehouseId(user);
        if (scopedWarehouseId.HasValue)
            warehouseId = scopedWarehouseId.Value;

        var zoneQuery = db.Zones.AsNoTracking().AsQueryable();
        if (warehouseId.HasValue)
            zoneQuery = zoneQuery.Where(z => z.WarehouseId == warehouseId.Value);

        var zoneIds = await zoneQuery.Select(z => z.ZoneId).ToListAsync();
        return await db.Locations.AsNoTracking()
            .Include(l => l.Zone).ThenInclude(z => z.Warehouse)
            .Where(l => l.IsActive && zoneIds.Contains(l.ZoneId))
            .OrderBy(l => l.Zone.Warehouse.WarehouseCode)
            .ThenBy(l => l.LocationCode)
            .ToListAsync();
    }
}

public sealed class SlottingLocationLoad
{
    public decimal WeightLoadKg { get; init; }
    public HashSet<int> OccupiedItemIds { get; init; } = new();
}

public sealed class SlottingCandidateScore
{
    public required Location Location { get; init; }
    public decimal SameItemQty { get; init; }
    public decimal ProjectedWeightLoadKg { get; init; }
    public decimal MaxWeightCapacityKg { get; init; }
    public int VelocityScore { get; init; }
    public int ErgonomicScore { get; init; }
    public int CapacityScore { get; init; }
    public int TotalScore { get; init; }
}

public sealed record SlottingVelocityAnalysisResult(int ClassifiedCount, int AClassCount, int BClassCount, int CClassCount);

public interface ISlottingPlanningService
{
    SlottingCandidateScore? ScoreSlottingCandidate(Location location, Item item, List<ItemLocation> itemRows, decimal totalStock, char abcClass, ItemVelocityClassification? velocity, Dictionary<int, SlottingLocationLoad> loadByLocation, Dictionary<int, List<int>> occupiedDefaults, decimal heavyItemThresholdKg);
    char ResolveAbcClass(Item item, ItemVelocityClassification? velocity);
    string BuildSlottingReason(Item item, SlottingCandidateScore best, char abcClass, string velocityBasis, decimal currentDefaultQty, decimal dominancePercent);
    Task<string> GenerateScenarioCodeAsync();
    Task<SlottingSimulationLine?> BuildSimulationLineAsync(SlottingSimulationScenario scenario, SlottingSuggestionRow suggestion, int warehouseId);
    Task LogSlottingAuditAsync(int itemId, int? oldLocationId, int newLocationId, string actor, string? ipAddress);
    Task<SlottingVelocityAnalysisResult> AnalyzeItemVelocityAsync(int warehouseId, int periodDays);
    Task<List<object>> GetVelocityHeatmapAsync(int warehouseId);
    int GetSlottingScore(string combinedClass);
    int GetPickFacePriority(char abcClass);
}

public sealed class SlottingPlanningService(AppDbContext db, IUnitOfWork unitOfWork) : ISlottingPlanningService
{
    private static DateTime VietnamNow => VietnamTime.Now;

    public SlottingCandidateScore? ScoreSlottingCandidate(Location location, Item item, List<ItemLocation> itemRows, decimal totalStock, char abcClass, ItemVelocityClassification? velocity, Dictionary<int, SlottingLocationLoad> loadByLocation, Dictionary<int, List<int>> occupiedDefaults, decimal heavyItemThresholdKg)
    {
        var sameItemQty = itemRows.Where(r => r.LocationId == location.LocationId).Sum(r => r.Quantity);

        if (!location.AllowMixedSku
            && loadByLocation.TryGetValue(location.LocationId, out var load)
            && load.OccupiedItemIds.Any(x => x != item.ItemId))
            return null;

        if (occupiedDefaults.TryGetValue(location.LocationId, out var defaultItemIds)
            && defaultItemIds.Any(x => x != item.ItemId))
            return null;

        if (item.Weight.HasValue)
        {
            if (item.Weight.Value >= heavyItemThresholdKg && location.HeightLevel >= 5 && !location.AllowMechanicalHandling)
                return null;
            if (location.WeightLimitKg.HasValue && item.Weight.Value > location.WeightLimitKg.Value)
                return null;
        }

        var itemWeight = item.Weight ?? 1m;
        var existingWeightLoad = loadByLocation.TryGetValue(location.LocationId, out var locationLoad) ? locationLoad.WeightLoadKg : 0m;
        var stockToMove = Math.Max(0m, totalStock - sameItemQty);
        var projectedWeightLoad = existingWeightLoad + stockToMove * itemWeight;
        var maxWeightCapacity = location.MaxWeightCapacityKg ?? (location.MaxCapacity > 0m ? location.MaxCapacity : 999999m);

        if (projectedWeightLoad > maxWeightCapacity)
            return null;

        var velocityScore = ResolveVelocityScore(abcClass, velocity);
        var ergonomicScore = ResolveErgonomicScore(abcClass, item.Weight, location, heavyItemThresholdKg);
        var capacityScore = ResolveCapacityScore(projectedWeightLoad, maxWeightCapacity, sameItemQty, totalStock);
        var totalScore = (int)Math.Round(velocityScore * 0.45m + ergonomicScore * 0.35m + capacityScore * 0.20m, MidpointRounding.AwayFromZero);

        return new SlottingCandidateScore
        {
            Location = location,
            SameItemQty = sameItemQty,
            ProjectedWeightLoadKg = projectedWeightLoad,
            MaxWeightCapacityKg = maxWeightCapacity,
            VelocityScore = velocityScore,
            ErgonomicScore = ergonomicScore,
            CapacityScore = capacityScore,
            TotalScore = totalScore
        };
    }

    public char ResolveAbcClass(Item item, ItemVelocityClassification? velocity)
    {
        var abcClass = velocity?.AbcClass ?? (string.IsNullOrWhiteSpace(item.AbcClass) ? 'C' : char.ToUpperInvariant(item.AbcClass[0]));
        return abcClass is 'A' or 'B' or 'C' ? abcClass : 'C';
    }

    public string BuildSlottingReason(Item item, SlottingCandidateScore best, char abcClass, string velocityBasis, decimal currentDefaultQty, decimal dominancePercent)
    {
        var parts = new List<string>
        {
            $"Score {best.TotalScore}/100 = velocity {best.VelocityScore}, ergonomic {best.ErgonomicScore}, capacity {best.CapacityScore}.",
            $"{velocityBasis}; suggested level {best.Location.HeightLevel}{(best.Location.IsGoldenZone ? " golden zone" : "")}."
        };

        if (abcClass == 'A' && best.Location.IsGoldenZone)
            parts.Add("Hàng hạng A được ưu tiên vào ô vàng còn trống.");
        if (item.Weight.HasValue)
            parts.Add($"Trọng lượng vật tư {item.Weight.Value:N2} kg hợp lệ cho giới hạn ô {(best.Location.WeightLimitKg.HasValue ? best.Location.WeightLimitKg.Value.ToString("N2") + " kg" : "không giới hạn")}.");
        if (dominancePercent >= 60m)
            parts.Add($"{dominancePercent:N0}% tồn kho hiện tại đã nằm tại vị trí đề xuất, giảm công tái phân kệ.");
        else if (currentDefaultQty <= 0m)
            parts.Add("Vị trí mặc định hiện tại không còn tồn, nên đề xuất ô an toàn/phù hợp hơn.");

        parts.Add($"Tải trọng dự kiến {best.ProjectedWeightLoadKg:N2} / {best.MaxWeightCapacityKg:N2} kg.");
        return string.Join(" ", parts);
    }

    public async Task<string> GenerateScenarioCodeAsync()
    {
        var prefix = $"SLT-SIM-{VietnamNow:yyyyMMdd}-";
        var count = await db.SlottingSimulationScenarios.CountAsync(s => s.ScenarioCode.StartsWith(prefix));
        return $"{prefix}{count + 1:0000}";
    }

    public async Task<SlottingSimulationLine?> BuildSimulationLineAsync(SlottingSimulationScenario scenario, SlottingSuggestionRow suggestion, int warehouseId)
    {
        var item = await db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.ItemId == suggestion.ItemId && i.IsActive);
        var suggested = await db.Locations.AsNoTracking().Include(l => l.Zone).FirstOrDefaultAsync(l => l.LocationId == suggestion.SuggestedLocationId && l.IsActive);
        if (item == null || suggested?.Zone == null || suggested.Zone.WarehouseId != warehouseId)
            return null;

        var sourceRows = await db.ItemLocations.AsNoTracking()
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Where(il => il.ItemId == suggestion.ItemId
                && il.LocationId != suggestion.SuggestedLocationId
                && il.Quantity - il.ReservedQty > 0m
                && il.Location != null
                && il.Location.Zone != null
                && il.Location.Zone.WarehouseId == warehouseId)
            .ToListAsync();

        var source = sourceRows
            .OrderByDescending(il => suggestion.CurrentDefaultLocationId.HasValue && il.LocationId == suggestion.CurrentDefaultLocationId.Value)
            .ThenByDescending(il => il.Quantity - il.ReservedQty)
            .FirstOrDefault();
        if (source?.Location?.Zone == null)
            return null;

        var velocity = await db.ItemVelocityClassifications.AsNoTracking()
            .Where(v => v.ItemId == suggestion.ItemId && v.WarehouseId == warehouseId && v.IsActive)
            .OrderByDescending(v => v.LastAnalyzedAt)
            .FirstOrDefaultAsync();

        var dailyPickFrequency = velocity?.DailyPickFrequency
            ?? (suggestion.AbcClass == "A" ? 3.0m : suggestion.AbcClass == "B" ? 1.0m : 0.25m);
        var plannedQty = Math.Max(0m, source.Quantity - source.ReservedQty);
        var beforeDistance = EstimateSlottingTravelDistance(source.Location);
        var afterDistance = EstimateSlottingTravelDistance(suggested);
        var beforeTravelMinutes = beforeDistance * dailyPickFrequency * 0.08m * 30m;
        var afterTravelMinutes = afterDistance * dailyPickFrequency * 0.08m * 30m;
        var estimatedTravelSaved = beforeTravelMinutes - afterTravelMinutes;
        var movementCost = Math.Max(1m, plannedQty * 0.25m + Math.Abs(beforeDistance - afterDistance) * 0.15m);

        return new SlottingSimulationLine
        {
            Scenario = scenario,
            ItemId = suggestion.ItemId,
            CurrentDefaultLocationId = suggestion.CurrentDefaultLocationId,
            SourceLocationId = source.LocationId,
            SuggestedLocationId = suggestion.SuggestedLocationId,
            SourceItemLocationId = source.ItemLocationId,
            PlannedMoveQty = plannedQty,
            DailyPickFrequency = dailyPickFrequency,
            BeforeTravelDistance = beforeDistance,
            AfterTravelDistance = afterDistance,
            EstimatedTravelMinutesSaved = estimatedTravelSaved,
            MovementCostMinutes = movementCost,
            NetEstimatedMinutesSaved = estimatedTravelSaved - movementCost,
            SlottingScore = suggestion.SlottingScore,
            AbcClass = suggestion.AbcClass,
            Reason = $"{suggestion.Reason} Simulation horizon 30 days; before travel {beforeTravelMinutes:N2} min, after {afterTravelMinutes:N2} min, move cost {movementCost:N2} min."
        };
    }

    public async Task LogSlottingAuditAsync(int itemId, int? oldLocationId, int newLocationId, string actor, string? ipAddress)
    {
        db.AuditLogs.Add(new AuditLog
        {
            TableName = "Items",
            RecordId = itemId.ToString(),
            ActionType = "UPDATE",
            ColumnChanged = "DefaultLocationId",
            OldValue = oldLocationId?.ToString(),
            NewValue = newLocationId.ToString(),
            ChangedBy = actor,
            ChangedAt = VietnamNow,
            IpAddress = ipAddress,
            AppModule = "Slotting"
        });
        await unitOfWork.SaveChangesAsync();
    }

    public async Task<SlottingVelocityAnalysisResult> AnalyzeItemVelocityAsync(int warehouseId, int periodDays)
    {
        var cutoff = VietnamNow.AddDays(-periodDays);
        var pickRows = await db.PickTasks.Include(t => t.Voucher)
            .Where(t => t.Status == PickTaskStatusEnum.Completed && t.CompletedAt >= cutoff)
            .Where(t => (t.WaveId.HasValue && db.Waves.Any(w => w.WaveId == t.WaveId.Value && w.WarehouseId == warehouseId))
                || (!t.WaveId.HasValue && t.Voucher != null && t.Voucher.WarehouseId == warehouseId))
            .GroupBy(t => new { t.ItemId, t.SourceLocationId })
            .Select(g => new
            {
                g.Key.ItemId,
                PickCount = g.Count(),
                TotalQty = g.Sum(t => t.PickedQty)
            })
            .ToListAsync();

        var itemTotals = pickRows
            .GroupBy(p => p.ItemId)
            .Select(g => new
            {
                ItemId = g.Key,
                TotalPickCount = g.Sum(x => x.PickCount),
                TotalQty = g.Sum(x => x.TotalQty)
            })
            .OrderByDescending(x => x.TotalPickCount)
            .ToList();

        var totalPicks = itemTotals.Sum(x => x.TotalPickCount);
        var cumulative = 0m;
        foreach (var itemTotal in itemTotals)
        {
            cumulative += itemTotal.TotalPickCount;
            var percentile = totalPicks > 0 ? cumulative / totalPicks * 100m : 0m;
            var abcClass = percentile <= 80m ? 'A' : percentile <= 95m ? 'B' : 'C';
            var item = await db.Items.FindAsync(itemTotal.ItemId);
            var existing = await db.ItemVelocityClassifications.FirstOrDefaultAsync(v => v.ItemId == itemTotal.ItemId && v.WarehouseId == warehouseId);
            if (existing != null)
            {
                existing.AbcClass = abcClass;
                existing.XyzClass = 'X';
                existing.CombinedClass = $"{abcClass}X";
                existing.PickCount = itemTotal.TotalPickCount;
                existing.TotalPickQty = itemTotal.TotalQty;
                existing.DailyPickFrequency = periodDays > 0 ? itemTotal.TotalPickCount / (decimal)periodDays : 0m;
                existing.CurrentLocationId = item?.DefaultLocationId;
                existing.LastAnalyzedAt = VietnamNow;
                existing.AnalysisPeriodDays = periodDays;
            }
            else
            {
                db.ItemVelocityClassifications.Add(new ItemVelocityClassification
                {
                    ItemId = itemTotal.ItemId,
                    WarehouseId = warehouseId,
                    AbcClass = abcClass,
                    XyzClass = 'X',
                    CombinedClass = $"{abcClass}X",
                    PickCount = itemTotal.TotalPickCount,
                    TotalPickQty = itemTotal.TotalQty,
                    DailyPickFrequency = periodDays > 0 ? itemTotal.TotalPickCount / (decimal)periodDays : 0m,
                    CurrentLocationId = item?.DefaultLocationId,
                    LastAnalyzedAt = VietnamNow,
                    AnalysisPeriodDays = periodDays
                });
            }
        }

        await unitOfWork.SaveChangesAsync();
        var classified = itemTotals.Count;
        var aClass = itemTotals.Count(x => itemTotals.IndexOf(x) < classified * 0.2m);
        var bClass = itemTotals.Count - aClass - itemTotals.Count(x => itemTotals.IndexOf(x) >= classified * 0.7m);
        return new SlottingVelocityAnalysisResult(classified, aClass, bClass, classified - aClass - bClass);
    }

    public async Task<List<object>> GetVelocityHeatmapAsync(int warehouseId)
    {
        return (await db.ItemVelocityClassifications.AsNoTracking()
                .Include(v => v.Item)
                .Include(v => v.CurrentLocation)
                .Where(v => v.WarehouseId == warehouseId && v.IsActive)
                .OrderByDescending(v => v.PickCount)
                .Take(500)
                .ToListAsync())
            .Select(v => new
            {
                itemId = v.ItemId,
                itemCode = v.Item?.ItemCode ?? "",
                itemName = v.Item?.ItemName ?? "",
                abcClass = v.AbcClass.ToString(),
                xyzClass = v.XyzClass.ToString(),
                combinedClass = v.CombinedClass,
                pickCount = v.PickCount,
                dailyFrequency = v.DailyPickFrequency,
                currentLocation = v.CurrentLocation?.LocationCode ?? "Chưa cập nhật",
                score = GetSlottingScore(v.CombinedClass),
                pickFacePriority = GetPickFacePriority(v.AbcClass)
            })
            .Cast<object>()
            .ToList();
    }

    public int GetSlottingScore(string combinedClass) => combinedClass switch
    {
        "AX" => 100,
        "AY" => 85,
        "AZ" => 70,
        "BX" => 75,
        "BY" => 60,
        "BZ" => 50,
        "CX" => 45,
        "CY" => 35,
        "CZ" => 20,
        _ => 50
    };

    public int GetPickFacePriority(char abcClass) => abcClass switch
    {
        'A' => 1,
        'B' => 2,
        'C' => 3,
        _ => 2
    };

    private int ResolveVelocityScore(char abcClass, ItemVelocityClassification? velocity)
    {
        if (velocity != null)
            return GetSlottingScore(velocity.CombinedClass);

        return abcClass switch
        {
            'A' => 90,
            'B' => 65,
            'C' => 40,
            _ => 40
        };
    }

    private static int ResolveErgonomicScore(char abcClass, decimal? itemWeightKg, Location location, decimal heavyItemThresholdKg)
    {
        var score = 70;
        if (location.IsGoldenZone)
            score += abcClass == 'A' ? 25 : 15;
        if (location.HeightLevel <= 2)
            score += 10;
        if (location.HeightLevel >= 5)
            score -= location.AllowMechanicalHandling ? 10 : 35;
        if (itemWeightKg.HasValue && itemWeightKg.Value >= heavyItemThresholdKg)
            score += location.HeightLevel <= 2 ? 20 : !location.AllowMechanicalHandling ? -20 : 0;
        if (location.WeightLimitKg.HasValue && itemWeightKg.HasValue)
        {
            var ratio = location.WeightLimitKg.Value <= 0m ? 1m : itemWeightKg.Value / location.WeightLimitKg.Value;
            if (ratio >= 0.9m)
                score -= 10;
            else if (ratio <= 0.5m)
                score += 5;
        }
        return Math.Clamp(score, 0, 100);
    }

    private static int ResolveCapacityScore(decimal projectedWeightLoadKg, decimal maxWeightCapacityKg, decimal sameItemQty, decimal totalStock)
    {
        if (maxWeightCapacityKg <= 0m)
            return 0;

        var loadRatio = projectedWeightLoadKg / maxWeightCapacityKg;
        var baseScore = loadRatio <= 0.70m ? 100
            : loadRatio <= 0.85m ? 80
            : loadRatio <= 0.95m ? 55
            : loadRatio <= 1.00m ? 30
            : 0;
        if (totalStock > 0m && sameItemQty > 0m)
            baseScore += (int)Math.Min(15m, Math.Round(sameItemQty / totalStock * 15m, MidpointRounding.AwayFromZero));

        return Math.Clamp(baseScore, 0, 100);
    }

    private static decimal EstimateSlottingTravelDistance(Location? location)
    {
        if (location == null)
            return 100m;

        var aisleSequence = location.AisleSequence > 0
            ? location.AisleSequence
            : TryParseFirstNumber(location.AisleCode) ?? TryParseFirstNumber(location.RackCode) ?? TryParseFirstNumber(location.LocationCode) ?? 10;
        var heightMultiplier = location.HeightLevel switch
        {
            <= 1 => 1.15m,
            2 or 3 or 4 => 1.00m,
            5 => 1.25m,
            _ => 1.40m
        };
        var aisleDistance = aisleSequence * 10m;
        var binDistance = (TryParseFirstNumber(location.BinCode) ?? 1) * 0.5m;
        return Math.Round((aisleDistance + binDistance) * heightMultiplier, 4);
    }

    private static int? TryParseFirstNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var number) && number > 0 ? number : null;
    }
}

public interface IOperationExceptionQueryService
{
    (string Key, string Label, int Rank) MapSeverity(double ageHours);
    int GetSeverityRank(string severityKey);
    string ComputeExceptionKey(OperationExceptionRow row);
    (string Key, string Label) MapCaseStatus(OperationExceptionStatusEnum status);
}

public sealed class OperationExceptionQueryService : IOperationExceptionQueryService
{
    public (string Key, string Label, int Rank) MapSeverity(double ageHours)
        => ageHours >= 8.0 ? ("critical", "Khẩn cấp", 0)
            : ageHours >= 2.0 ? ("high", "Cao", 1)
            : ("medium", "Trung bình", 2);

    public int GetSeverityRank(string severityKey) => severityKey switch
    {
        "critical" => 0,
        "high" => 1,
        "medium" => 2,
        _ => 3
    };

    public string ComputeExceptionKey(OperationExceptionRow row)
        => string.Join("|",
            row.CategoryKey.Trim().ToUpperInvariant(),
            row.WarehouseId,
            (row.ReferenceCode ?? string.Empty).Trim().ToUpperInvariant(),
            (row.SecondaryReference ?? string.Empty).Trim().ToUpperInvariant(),
            (row.ItemCode ?? string.Empty).Trim().ToUpperInvariant(),
            (row.LocationCode ?? string.Empty).Trim().ToUpperInvariant());

    public (string Key, string Label) MapCaseStatus(OperationExceptionStatusEnum status) => status switch
    {
        OperationExceptionStatusEnum.Acknowledged => ("acknowledged", "Đang xử lý"),
        OperationExceptionStatusEnum.Resolved => ("resolved", "Đã đóng"),
        _ => ("open", "Mới phát hiện")
    };
}

public interface IYardBillingQueryService
{
    IQueryable<YardBillingCharge> BuildChargeQuery(int? warehouseId, YardChargeStatusEnum? status, DateTime? dateFrom, DateTime? dateTo);
    List<YardBillingChargeRow> MapChargeRows(IEnumerable<YardBillingCharge> charges);
    string StatusText(YardChargeStatusEnum status);
}

public sealed class YardBillingQueryService(AppDbContext db) : IYardBillingQueryService
{
    public IQueryable<YardBillingCharge> BuildChargeQuery(int? warehouseId, YardChargeStatusEnum? status, DateTime? dateFrom, DateTime? dateTo)
    {
        var query = db.YardBillingCharges
            .Include(c => c.YardVisit).ThenInclude(v => v.Trailer)
            .Include(c => c.YardVisit).ThenInclude(v => v.Voucher).ThenInclude(v => v!.Partner)
            .Include(c => c.Warehouse)
            .AsNoTracking()
            .AsQueryable();

        if (warehouseId.HasValue)
            query = query.Where(c => c.WarehouseId == warehouseId.Value);
        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);
        if (dateFrom.HasValue)
            query = query.Where(c => c.CreatedAt >= dateFrom.Value.Date);
        if (dateTo.HasValue)
            query = query.Where(c => c.CreatedAt < dateTo.Value.Date.AddDays(1));

        return query;
    }

    public List<YardBillingChargeRow> MapChargeRows(IEnumerable<YardBillingCharge> charges)
        => charges.Select(c => new YardBillingChargeRow
        {
            YardBillingChargeId = c.YardBillingChargeId,
            YardVisitId = c.YardVisitId,
            VisitCode = c.YardVisit?.VisitCode ?? "",
            TrailerNumber = c.YardVisit?.Trailer?.TrailerNumber ?? "",
            ContainerNumber = c.YardVisit?.Trailer?.ContainerNumber,
            CarrierName = c.YardVisit?.Trailer?.CarrierName,
            PartnerName = c.YardVisit?.Voucher?.Partner?.PartnerName,
            WarehouseId = c.WarehouseId,
            WarehouseName = c.Warehouse?.WarehouseName ?? "",
            TotalDwellMinutes = c.TotalDwellMinutes,
            FreeTimeMinutes = c.FreeTimeMinutes,
            ChargeableMinutes = c.ChargeableMinutes,
            AppliedRatePerHour = c.AppliedRatePerHour,
            Amount = c.Amount,
            Currency = c.Currency,
            Status = c.Status,
            GateInAt = c.YardVisit?.GateInAt,
            GateOutAt = c.YardVisit?.GateOutAt,
            ConfirmedBy = c.ConfirmedBy,
            ConfirmedAt = c.ConfirmedAt,
            WaivedBy = c.WaivedBy,
            WaivedReason = c.WaivedReason,
            CreatedAt = c.CreatedAt
        }).ToList();

    public string StatusText(YardChargeStatusEnum status) => status switch
    {
        YardChargeStatusEnum.Draft => "Nháp",
        YardChargeStatusEnum.Confirmed => "Đã xác nhận",
        YardChargeStatusEnum.Invoiced => "Đã xuất HĐ",
        YardChargeStatusEnum.Waived => "Đã miễn",
        _ => status.ToString()
    };
}

public interface IVoucherSharedRuleService
{
    void EnforceSod(ClaimsPrincipal user, string createdBy, string verifierPermission);
    int? GetScopedWarehouseId(ClaimsPrincipal user);
    bool CanSeeFinancial(ClaimsPrincipal user);
    List<int> GetOwnerScopeClaimIds(ClaimsPrincipal user);
    Task EnsureVoucherOwnerScopeAsync(ClaimsPrincipal user, int? ownerPartnerId);
    DateTime ResolveLockTransactionDate(Voucher voucher, DateTime? operationTime = null);
    bool IsLocked(DateTime transactionDate, DateTime? lockDate);
    bool IsInboundVoucherType(VoucherTypeEnum voucherType);
    string? NormalizeText(string? value, bool toUpper = false);
    int GetRequiredSerialCount(decimal qty);
    List<string> ParseSerialCodes(string? raw);
    bool RequiresManifest(VoucherTypeEnum voucherType);
    bool RequiresTrackingOrManifest(VoucherTypeEnum voucherType);
    bool RequiresPartner(VoucherTypeEnum voucherType, ExportModeEnum exportMode);
    decimal? ResolveConversionRate(IEnumerable<UnitConversion> conversions, int itemId, int fromUomId, int toUomId);
    Task<DateTime?> GetActiveLockDateAsync(int warehouseId);
    Task<int?> GetLocationWarehouseIdAsync(int locationId);
}

public sealed class VoucherSharedRuleService(AppDbContext db) : IVoucherSharedRuleService
{
    public void EnforceSod(ClaimsPrincipal user, string createdBy, string verifierPermission)
    {
        var actor = user.Identity?.Name ?? "system";
        if (!string.Equals(createdBy, actor, StringComparison.OrdinalIgnoreCase))
            return;

        var entry = WmsPermissions.SodMatrix.FirstOrDefault(s => s.VerifierPermission == verifierPermission);
        var label = entry.VerifierLabel ?? verifierPermission;
        throw WmsExceptions.SodViolation(actor, label);
    }

    public int? GetScopedWarehouseId(ClaimsPrincipal user)
    {
        if (user.IsInRole("Admin"))
            return null;

        var claim = user.FindFirst("WarehouseId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    public bool CanSeeFinancial(ClaimsPrincipal user)
        => user.Claims.Any(c => c.Type == PermissionClaimTypes.Permission
            && string.Equals(c.Value, WmsPermissions.ReportViewFinancial, StringComparison.Ordinal));

    public List<int> GetOwnerScopeClaimIds(ClaimsPrincipal user)
        => user.FindAll(TenantClaimTypes.OwnerPartnerId)
            .Select(c => int.TryParse(c.Value, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

    public async Task EnsureVoucherOwnerScopeAsync(ClaimsPrincipal user, int? ownerPartnerId)
    {
        var allowed = GetOwnerScopeClaimIds(user);
        if (allowed.Count == 0)
            return;
        if (!ownerPartnerId.HasValue || !allowed.Contains(ownerPartnerId.Value))
            throw new UnauthorizedAccessException("Bạn không có quyền thao tác chủ hàng kho nhiều chủ hàng này.");

        var ownerOk = await db.Partners.AnyAsync(p => p.PartnerId == ownerPartnerId.Value && p.IsThreePlClient && p.IsActive);
        if (!ownerOk)
            throw new BusinessRuleException("Chủ hàng kho nhiều chủ hàng không hợp lệ.", "TENANT_OWNER_INVALID", "Voucher");
    }

    public DateTime ResolveLockTransactionDate(Voucher voucher, DateTime? operationTime = null)
        => voucher.CompletedAt ?? operationTime ?? voucher.VoucherDate;

    public bool IsLocked(DateTime transactionDate, DateTime? lockDate)
        => lockDate.HasValue && transactionDate.Date <= lockDate.Value.Date;

    public bool IsInboundVoucherType(VoucherTypeEnum voucherType)
        => voucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham;

    public string? NormalizeText(string? value, bool toUpper = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        return toUpper ? normalized.ToUpperInvariant() : normalized;
    }

    public int GetRequiredSerialCount(decimal qty)
        => (int)Math.Ceiling(Math.Max(0m, qty));

    public List<string> ParseSerialCodes(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new List<string>();

        return raw.Split(new[] { '\r', '\n', ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToUpperInvariant())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public bool RequiresManifest(VoucherTypeEnum voucherType)
        => voucherType is VoucherTypeEnum.ChuyenKho or VoucherTypeEnum.XuatSanXuat;

    public bool RequiresTrackingOrManifest(VoucherTypeEnum voucherType)
        => voucherType is VoucherTypeEnum.XuatKho or VoucherTypeEnum.TraNCC or VoucherTypeEnum.ChuyenKho or VoucherTypeEnum.XuatSanXuat;

    public bool RequiresPartner(VoucherTypeEnum voucherType, ExportModeEnum exportMode)
        => voucherType switch
        {
            VoucherTypeEnum.NhapKho => true,
            VoucherTypeEnum.KhachTra => true,
            VoucherTypeEnum.TraNCC => true,
            VoucherTypeEnum.XuatKho => exportMode == ExportModeEnum.Sale,
            _ => false
        };

    public decimal? ResolveConversionRate(IEnumerable<UnitConversion> conversions, int itemId, int fromUomId, int toUomId)
    {
        if (fromUomId == toUomId)
            return 1m;

        var directCandidates = conversions
            .Where(c => (c.ItemId == null || c.ItemId == itemId)
                && c.FromUomId == fromUomId
                && c.ToUomId == toUomId)
            .OrderByDescending(c => c.ItemId == itemId)
            .ToList();
        if (directCandidates.Count > 1 && directCandidates.Count(c => c.ItemId == null) > 1)
            throw WmsExceptions.DuplicateUnitConversion();
        var direct = directCandidates.FirstOrDefault();
        if (direct != null)
            return direct.ConversionRate;

        var reverseCandidates = conversions
            .Where(c => (c.ItemId == null || c.ItemId == itemId)
                && c.FromUomId == toUomId
                && c.ToUomId == fromUomId
                && c.ConversionRate != 0m)
            .OrderByDescending(c => c.ItemId == itemId)
            .ToList();
        if (reverseCandidates.Count > 1 && reverseCandidates.Count(c => c.ItemId == null) > 1)
            throw WmsExceptions.DuplicateUnitConversionReverse();
        var reverse = reverseCandidates.FirstOrDefault();

        return reverse != null ? 1m / reverse.ConversionRate : null;
    }

    public async Task<DateTime?> GetActiveLockDateAsync(int warehouseId)
        => await db.WarehousePeriodLocks.AsNoTracking()
            .Where(l => l.WarehouseId == warehouseId && l.IsActive)
            .OrderByDescending(l => l.LockDate)
            .Select(l => (DateTime?)l.LockDate)
            .FirstOrDefaultAsync();

    public async Task<int?> GetLocationWarehouseIdAsync(int locationId)
        => await db.Locations.AsNoTracking()
            .Include(l => l.Zone)
            .Where(l => l.LocationId == locationId && l.Zone != null)
            .Select(l => (int?)l.Zone!.WarehouseId)
            .FirstOrDefaultAsync();
}

public interface IVoucherImportQueryService
{
    Task<string> StoreLegacyReceiptDocumentAsync(string originalFileName, byte[] imageBytes, CancellationToken cancellationToken);
    string ResolvePrivateReceiptPath(string storedPath);
    string ResolveContentType(string physicalPath, string? storedContentType = null);
}

public sealed class VoucherImportQueryService : IVoucherImportQueryService
{
    public async Task<string> StoreLegacyReceiptDocumentAsync(string originalFileName, byte[] imageBytes, CancellationToken cancellationToken)
    {
        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "uploads", "document-intake-legacy");
        Directory.CreateDirectory(uploadDir);

        var extension = Path.GetExtension(originalFileName);
        var safeStem = NormalizePrivateFileStem(Path.GetFileNameWithoutExtension(originalFileName));
        var uniqueFileName = $"{VietnamTime.FileStamp("yyyyMMddHHmmssfff")}_{Guid.NewGuid():N}_{safeStem}{extension}";
        var physicalPath = Path.Combine(uploadDir, uniqueFileName);

        await File.WriteAllBytesAsync(physicalPath, imageBytes, cancellationToken);
        return Path.Combine("App_Data", "uploads", "document-intake-legacy", uniqueFileName).Replace('\\', '/');
    }

    public string ResolvePrivateReceiptPath(string storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            throw new FileNotFoundException("Thiếu đường dẫn chứng từ.");

        var appRoot = Path.GetFullPath(Directory.GetCurrentDirectory());
        var privateUploadsRoot = Path.GetFullPath(Path.Combine(appRoot, "App_Data", "uploads"));
        var normalized = storedPath.Trim().TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        var candidate = normalized.StartsWith($"uploads{Path.DirectorySeparatorChar}receipts{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(privateUploadsRoot, "legacy-public-receipts", Path.GetFileName(normalized))
            : Path.Combine(appRoot, normalized);

        var fullPath = Path.GetFullPath(candidate);
        if (!fullPath.StartsWith(privateUploadsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Đường dẫn chứng từ không hợp lệ.");

        return fullPath;
    }

    public string ResolveContentType(string physicalPath, string? storedContentType = null)
    {
        if (!string.IsNullOrWhiteSpace(storedContentType))
            return storedContentType;

        var provider = new FileExtensionContentTypeProvider();
        return provider.TryGetContentType(physicalPath, out var contentType)
            ? contentType
            : "application/octet-stream";
    }

    private static string NormalizePrivateFileStem(string? value)
    {
        var source = string.IsNullOrWhiteSpace(value) ? "document" : value.Trim();
        var chars = source
            .Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-')
            .ToArray();
        var normalized = new string(chars).Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "document" : normalized[..Math.Min(normalized.Length, 80)];
    }
}

public interface IVoucherCreateWorkflowService
{
    Task<string> BuildItemAllowedSourceUomsJsonAsync(IEnumerable<Item> items);
}

public sealed class VoucherCreateWorkflowService(AppDbContext db) : IVoucherCreateWorkflowService
{
    public async Task<string> BuildItemAllowedSourceUomsJsonAsync(IEnumerable<Item> items)
    {
        var materializedItems = items.ToList();
        var itemIds = materializedItems.Select(i => i.ItemId).Distinct().ToList();
        var conversions = itemIds.Count == 0
            ? new List<UnitConversion>()
            : await db.UnitConversions
                .AsNoTracking()
                .Where(uc => uc.IsActive && (uc.ItemId == null || itemIds.Contains(uc.ItemId.Value)))
                .ToListAsync();

        var allowedMap = new Dictionary<int, List<int>>();
        foreach (var item in materializedItems)
        {
            var allowedUomIds = conversions
                .Where(uc => (uc.ItemId == null || uc.ItemId == item.ItemId)
                    && (uc.ToUomId == item.BaseUomId || uc.FromUomId == item.BaseUomId))
                .SelectMany(uc => new[] { uc.FromUomId, uc.ToUomId })
                .Append(item.BaseUomId)
                .Where(uomId => uomId > 0)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            allowedMap[item.ItemId] = allowedUomIds;
        }

        return System.Text.Json.JsonSerializer.Serialize(allowedMap);
    }
}

public interface IVoucherDetailQueryService
{
}

public sealed class VoucherDetailQueryService : IVoucherDetailQueryService
{
}
