using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;
using WMS.ViewModels;

namespace WMS.Services;

/// <summary>
/// Engine xếp hạng nhiệm vụ thống nhất (Pick + Movement) theo vị trí hiện tại của picker.
/// Scoring: Proximity 40% + Priority 30% + Urgency 20% + Interleaving Bonus 10%.
/// </summary>
public class TaskInterleavingService
{
    private readonly AppDbContext _db;
    private static DateTime Now => VietnamTime.Now;

    // ── Scoring weights ──
    private const double WeightProximity = 0.40;
    private const double WeightPriority = 0.30;
    private const double WeightUrgency = 0.20;
    private const double WeightInterleaving = 0.10;

    private static readonly MovementTaskStatusEnum[] OpenMovementStatuses =
    {
        MovementTaskStatusEnum.Pending,
        MovementTaskStatusEnum.Assigned,
        MovementTaskStatusEnum.InProgress
    };

    private static readonly PickTaskStatusEnum[] OpenPickStatuses =
    {
        PickTaskStatusEnum.Pending,
        PickTaskStatusEnum.Assigned,
        PickTaskStatusEnum.InProgress
    };

    public TaskInterleavingService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Trả danh sách nhiệm vụ (Pick + Movement) xếp hạng theo vị trí hiện tại.
    /// </summary>
    public async Task<InterleavedTaskQueue> GetNextTasksAsync(
        string currentUser,
        int? currentLocationId,
        int? scopedWarehouseId,
        IReadOnlyList<int>? scopedZoneIds,
        TaskCategoryEnum? lastCompletedCategory = null,
        int maxResults = 10)
    {
        // ── Resolve current location context ──
        var actor = currentUser.Trim();

        Location? currentLocation = null;
        if (currentLocationId.HasValue)
        {
            currentLocation = await _db.Locations
                .Include(l => l.Zone)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LocationId == currentLocationId.Value);
        }

        var currentAisleSeq = currentLocation?.AisleSequence ?? 0;
        var currentZoneId = currentLocation?.ZoneId;
        var currentWarehouseId = currentLocation?.Zone?.WarehouseId ?? scopedWarehouseId;

        // ── Fetch open pick tasks ──
        var pickQuery = _db.PickTasks
            .Include(t => t.Wave)
            .Include(t => t.Voucher)
            .Include(t => t.Item)
            .Include(t => t.SourceLocation).ThenInclude(l => l!.Zone)
            .Where(t => OpenPickStatuses.Contains(t.Status))
            .Where(t =>
                (t.Status == PickTaskStatusEnum.Pending && string.IsNullOrEmpty(t.AssignedTo))
                || (t.Status != PickTaskStatusEnum.Pending && t.AssignedTo == actor));

        if (currentWarehouseId.HasValue)
            pickQuery = pickQuery.Where(t =>
                (t.Wave != null && t.Wave.WarehouseId == currentWarehouseId.Value)
                || (t.Wave == null && t.Voucher != null && t.Voucher.WarehouseId == currentWarehouseId.Value));
        if (scopedZoneIds != null)
            pickQuery = pickQuery.Where(t => t.SourceLocation != null && scopedZoneIds.Contains(t.SourceLocation.ZoneId));

        var pickTasks = await pickQuery.OrderBy(t => t.PickTaskId).Take(200).ToListAsync();

        // ── Fetch open movement tasks ──
        var moveQuery = _db.MovementTasks
            .Include(t => t.Item)
            .Include(t => t.LicensePlate)
            .Include(t => t.SourceLocation).ThenInclude(l => l!.Zone)
            .Include(t => t.DestinationLocation).ThenInclude(l => l!.Zone)
            .Where(t => OpenMovementStatuses.Contains(t.Status))
            .Where(t =>
                (t.Status == MovementTaskStatusEnum.Pending && string.IsNullOrEmpty(t.AssignedTo))
                || (t.Status != MovementTaskStatusEnum.Pending && t.AssignedTo == actor));

        if (currentWarehouseId.HasValue)
            moveQuery = moveQuery.Where(t => t.WarehouseId == currentWarehouseId.Value);
        if (scopedZoneIds != null)
            moveQuery = moveQuery.Where(t => t.SourceLocation != null && scopedZoneIds.Contains(t.SourceLocation.ZoneId));

        var moveTasks = await moveQuery
            .OrderByDescending(t => t.RoutePriorityScore)
            .ThenBy(t => t.DueAt ?? DateTime.MaxValue)
            .ThenBy(t => t.SourceAisleSequence)
            .ThenBy(t => t.MovementTaskId)
            .Take(200)
            .ToListAsync();

        // ── Score and merge ──
        var items = new List<InterleavedTaskItem>();

        foreach (var pt in pickTasks)
        {
            var item = ScorePickTask(pt, currentAisleSeq, currentZoneId, lastCompletedCategory);
            items.Add(item);
        }

        foreach (var mt in moveTasks)
        {
            var item = ScoreMovementTask(mt, currentAisleSeq, currentZoneId, lastCompletedCategory);
            items.Add(item);
        }

        items = items.OrderByDescending(i => i.TotalScore)
            .ThenBy(i => i.DueAt ?? DateTime.MaxValue)
            .ThenBy(i => i.TaskCode)
            .Take(maxResults)
            .ToList();

        return new InterleavedTaskQueue
        {
            CurrentLocationId = currentLocationId,
            CurrentLocationCode = currentLocation?.LocationCode,
            CurrentZoneCode = currentLocation?.Zone?.ZoneCode,
            WarehouseId = currentWarehouseId,
            CurrentUser = currentUser,
            Tasks = items,
            TotalPickTasks = pickTasks.Count,
            TotalMovementTasks = moveTasks.Count,
            ScoringExplanation = BuildScoringExplanation(currentLocation)
        };
    }

    /// <summary>
    /// Resolve location bằng scan code (LocationCode hoặc Barcode).
    /// </summary>
    public async Task<Location?> ResolveLocationAsync(string? scanCode, int? scopedWarehouseId)
    {
        if (string.IsNullOrWhiteSpace(scanCode)) return null;
        var code = scanCode.Trim();

        var location = await _db.Locations
            .Include(l => l.Zone)
            .FirstOrDefaultAsync(l =>
                l.IsActive
                && (l.LocationCode == code || l.Barcode == code)
                && (!scopedWarehouseId.HasValue || l.Zone!.WarehouseId == scopedWarehouseId.Value));

        return location;
    }

    // ════════════════════════════════════════════════════════════════
    // SCORING HELPERS
    // ════════════════════════════════════════════════════════════════

    private InterleavedTaskItem ScorePickTask(PickTask pt, int currentAisleSeq, int? currentZoneId, TaskCategoryEnum? lastCategory)
    {
        var sourceAisle = pt.SourceLocation?.AisleSequence ?? 0;
        var sourceZoneId = pt.SourceLocation?.ZoneId;

        var proximityScore = CalculateProximityScore(currentAisleSeq, sourceAisle, currentZoneId, sourceZoneId);
        var priorityScore = 50; // PickTask không có priority enum, mặc định Normal
        var urgencyScore = CalculateUrgencyScore(pt.DueAt);
        var interleavingBonus = (lastCategory.HasValue && lastCategory.Value != TaskCategoryEnum.Pick) ? 20 : 0;

        var totalScore = (int)(
            proximityScore * WeightProximity +
            priorityScore * WeightPriority +
            urgencyScore * WeightUrgency +
            interleavingBonus * WeightInterleaving);

        return new InterleavedTaskItem
        {
            Category = TaskCategoryEnum.Pick,
            TaskId = pt.PickTaskId,
            TaskCode = pt.TaskCode,
            TaskTypeName = "Lấy hàng",
            ItemCode = pt.Item?.ItemCode ?? "",
            ItemName = pt.Item?.ItemName,
            SourceLocationCode = pt.SourceLocation?.LocationCode ?? "",
            DestinationLocationCode = pt.TargetLocation?.LocationCode ?? "Dock/Đóng gói",
            ZoneCode = pt.SourceLocation?.Zone?.ZoneCode,
            AisleCode = pt.SourceLocation?.AisleCode,
            AisleSequence = sourceAisle,
            PlannedQty = pt.TargetQty,
            CompletedQty = pt.PickedQty,
            AssignedTo = pt.AssignedTo,
            DueAt = pt.DueAt,
            Priority = MovementTaskPriorityEnum.Normal,
            ProximityScore = proximityScore,
            PriorityScore = priorityScore,
            UrgencyScore = urgencyScore,
            InterleavingBonus = interleavingBonus,
            TotalScore = totalScore,
            ScoreBreakdown = $"Gần: {proximityScore} | Ưu tiên: {priorityScore} | Khẩn: {urgencyScore} | Xen kẽ: {interleavingBonus} → Tổng: {totalScore}",
            ActionUrl = "/Operations/RfPicking"
        };
    }

    private InterleavedTaskItem ScoreMovementTask(MovementTask mt, int currentAisleSeq, int? currentZoneId, TaskCategoryEnum? lastCategory)
    {
        var sourceAisle = mt.SourceLocation?.AisleSequence ?? 0;
        var sourceZoneId = mt.SourceLocation?.ZoneId;

        var proximityScore = CalculateProximityScore(currentAisleSeq, sourceAisle, currentZoneId, sourceZoneId);
        var priorityScore = mt.Priority switch
        {
            MovementTaskPriorityEnum.Urgent => 100,
            MovementTaskPriorityEnum.High => 75,
            MovementTaskPriorityEnum.Normal => 50,
            MovementTaskPriorityEnum.Low => 25,
            _ => 50
        };
        var urgencyScore = CalculateUrgencyScore(mt.DueAt);
        var interleavingBonus = (lastCategory.HasValue && lastCategory.Value != TaskCategoryEnum.Movement) ? 20 : 0;

        var computedScore = (int)(
            proximityScore * WeightProximity +
            priorityScore * WeightPriority +
            urgencyScore * WeightUrgency +
            interleavingBonus * WeightInterleaving);
        var totalScore = Math.Max(computedScore, Math.Min(100, mt.RoutePriorityScore / 10));

        var typeName = mt.TaskType switch
        {
            MovementTaskTypeEnum.Replenishment => "Bổ sung hàng",
            MovementTaskTypeEnum.Reslotting => "Tái phân kệ",
            MovementTaskTypeEnum.Relocate => "Điều chuyển",
            _ => "Điều chuyển"
        };

        return new InterleavedTaskItem
        {
            Category = TaskCategoryEnum.Movement,
            TaskId = mt.MovementTaskId,
            TaskCode = mt.TaskCode,
            TaskTypeName = typeName,
            ItemCode = mt.MovementMode == MovementTaskModeEnum.Lpn ? (mt.LpnCodeSnapshot ?? mt.LicensePlate?.LpnCode ?? "") : (mt.Item?.ItemCode ?? ""),
            ItemName = mt.MovementMode == MovementTaskModeEnum.Lpn ? $"{mt.LpnDistinctItemCount} SKU / {mt.LpnDetailCount} lines" : mt.Item?.ItemName,
            SourceLocationCode = mt.SourceLocation?.LocationCode ?? "",
            DestinationLocationCode = mt.DestinationLocation?.LocationCode ?? "",
            ZoneCode = mt.SourceLocation?.Zone?.ZoneCode,
            AisleCode = mt.SourceLocation?.AisleCode,
            AisleSequence = sourceAisle,
            PlannedQty = mt.PlannedQty,
            CompletedQty = mt.ConfirmedQty,
            AssignedTo = mt.AssignedTo,
            DueAt = mt.DueAt,
            Priority = mt.Priority,
            ProximityScore = proximityScore,
            PriorityScore = priorityScore,
            UrgencyScore = urgencyScore,
            InterleavingBonus = interleavingBonus,
            TotalScore = totalScore,
            ScoreBreakdown = $"Gần: {proximityScore} | Ưu tiên: {priorityScore} | Khẩn: {urgencyScore} | Xen kẽ: {interleavingBonus} → Tổng: {totalScore}",
            ActionUrl = "/Operations/RfMovement"
        };
    }

    /// <summary>
    /// Tính điểm khoảng cách: cùng aisle=100, aisle gần=80-60, cùng zone=70, khác zone=30.
    /// </summary>
    public static int CalculateProximityScore(int currentAisleSeq, int taskAisleSeq, int? currentZoneId, int? taskZoneId)
    {
        // Không có vị trí hiện tại → trả điểm trung bình
        if (currentAisleSeq == 0 && !currentZoneId.HasValue)
            return 50;

        // Cùng aisle
        var aisleDelta = Math.Abs(currentAisleSeq - taskAisleSeq);
        if (aisleDelta == 0 && currentZoneId == taskZoneId)
            return 100;

        // Cùng zone
        if (currentZoneId.HasValue && currentZoneId == taskZoneId)
        {
            // Gần hơn → điểm cao hơn (max delta ~20 aisle)
            var proximityInZone = Math.Max(0, 90 - aisleDelta * 4);
            return Math.Max(50, proximityInZone);
        }

        // Khác zone
        return 30;
    }

    /// <summary>
    /// Tính điểm khẩn cấp dựa trên DueAt.
    /// </summary>
    public static int CalculateUrgencyScore(DateTime? dueAt)
    {
        if (!dueAt.HasValue)
            return 30; // Không có deadline → thấp

        var now = Now;
        var hoursRemaining = (dueAt.Value - now).TotalHours;

        if (hoursRemaining <= 0) return 100;    // Quá hạn
        if (hoursRemaining <= 1) return 80;     // < 1 giờ
        if (hoursRemaining <= 4) return 60;     // < 4 giờ
        if (hoursRemaining <= 8) return 45;     // < 8 giờ
        return 30;                              // > 8 giờ
    }

    private static string BuildScoringExplanation(Location? currentLocation)
    {
        if (currentLocation == null)
            return "Chưa xác định vị trí hiện tại — xếp hạng theo độ ưu tiên và độ khẩn.";

        var parts = new List<string>
        {
            $"Vị trí hiện tại: {currentLocation.LocationCode}",
            $"Khu: {currentLocation.Zone?.ZoneCode ?? "Chưa cập nhật"}",
            $"Dãy: {currentLocation.AisleCode ?? "Chưa cập nhật"} (thứ tự: {currentLocation.AisleSequence})"
        };
        parts.Add("Xếp hạng: Khoảng cách 40% + Ưu tiên 30% + Khẩn 20% + Xen kẽ 10%.");
        return string.Join(" | ", parts);
    }
}
