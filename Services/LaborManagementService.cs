using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public sealed class LaborStandardRequest
{
    public int? LaborStandardId { get; init; }
    public string TaskType { get; init; } = "";
    public string TaskTypeName { get; init; } = "";
    public string UnitOfWork { get; init; } = "unit";
    public decimal ExpectedMinutesPerUnit { get; init; }
    public decimal ExpectedUnitsPerHour { get; init; }
    public decimal MinPerformancePercent { get; init; } = 80m;
    public decimal ExcellentPerformancePercent { get; init; } = 120m;
    public int? WarehouseId { get; init; }
    public int? ZoneId { get; init; }
    public string? ItemClass { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
}

public sealed class LaborActivityRequest
{
    public int WarehouseId { get; init; }
    public int? ZoneId { get; init; }
    public int? UserId { get; init; }
    public string UserName { get; init; } = "";
    public string? ShiftCode { get; init; }
    public string TaskType { get; init; } = "";
    public string TaskSourceType { get; init; } = "Manual";
    public string? TaskSourceId { get; init; }
    public string? TaskSourceCode { get; init; }
    public int? OwnerPartnerId { get; init; }
    public string? ItemClass { get; init; }
    public decimal WorkQuantity { get; init; } = 1m;
    public string UnitOfWork { get; init; } = "unit";
    public DateTime? StartedAt { get; init; }
    public int WaitingMinutes { get; init; }
    public int BacklogAtStart { get; init; }
    public string Actor { get; init; } = "system";
}

public interface ILaborManagementService
{
    Task<LaborStandard> SaveStandardAsync(LaborStandardRequest request, int? scopedWarehouseId = null, CancellationToken ct = default);
    Task<LaborActivity> StartActivityAsync(LaborActivityRequest request, int? scopedWarehouseId = null, CancellationToken ct = default);
    Task<LaborActivity> CompleteActivityAsync(long activityId, decimal? workQuantity, string? exceptionReason, int? scopedWarehouseId, string actor, CancellationToken ct = default);
    Task<int> CaptureCompletedWarehouseTasksAsync(int? warehouseId = null, int days = 30, CancellationToken ct = default);
    Task<LaborExceptionReview> ApproveExceptionAsync(long reviewId, bool approve, decimal productivityAfter, decimal incentiveAmount, string notes, int? scopedWarehouseId, string actor, CancellationToken ct = default);
}

public class LaborManagementService : ILaborManagementService
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public LaborManagementService(AppDbContext db, IUnitOfWork? unitOfWork = null)
    {
        _db = db;
        _unitOfWork = unitOfWork ?? new EfUnitOfWork(db);
    }

    private static DateTime Now => VietnamTime.Now;

    public async Task<LaborStandard> SaveStandardAsync(LaborStandardRequest request, int? scopedWarehouseId = null, CancellationToken ct = default)
    {
        EnsureWarehouseScope(request.WarehouseId, scopedWarehouseId);
        if (string.IsNullOrWhiteSpace(request.TaskType))
            throw new BusinessRuleException("Can loai tac vu cho chuan nang suat.", "LABOR_STANDARD_TASK_REQUIRED", "LaborStandard");
        if (request.ExpectedMinutesPerUnit <= 0 && request.ExpectedUnitsPerHour <= 0)
            throw new BusinessRuleException("Can chuan nang suat lon hon 0.", "LABOR_STANDARD_RATE_REQUIRED", "LaborStandard");

        LaborStandard standard;
        if (request.LaborStandardId.HasValue)
        {
            standard = await _db.LaborStandards.FirstOrDefaultAsync(x => x.LaborStandardId == request.LaborStandardId.Value, ct)
                ?? throw new BusinessRuleException("Khong tim thay chuan nang suat.", "LABOR_STANDARD_NOT_FOUND", "LaborStandard");
            EnsureWarehouseScope(standard.WarehouseId, scopedWarehouseId);
            standard.UpdatedAt = Now;
        }
        else
        {
            standard = new LaborStandard { CreatedAt = Now };
            _db.LaborStandards.Add(standard);
        }

        standard.TaskType = request.TaskType.Trim();
        standard.TaskTypeName = string.IsNullOrWhiteSpace(request.TaskTypeName) ? standard.TaskType : request.TaskTypeName.Trim();
        standard.UnitOfWork = string.IsNullOrWhiteSpace(request.UnitOfWork) ? "unit" : request.UnitOfWork.Trim();
        standard.ExpectedMinutesPerUnit = request.ExpectedMinutesPerUnit > 0
            ? request.ExpectedMinutesPerUnit
            : Math.Round(60m / request.ExpectedUnitsPerHour, 2, MidpointRounding.AwayFromZero);
        standard.ExpectedUnitsPerHour = request.ExpectedUnitsPerHour > 0
            ? request.ExpectedUnitsPerHour
            : Math.Round(60m / standard.ExpectedMinutesPerUnit, 2, MidpointRounding.AwayFromZero);
        standard.MinPerformancePercent = request.MinPerformancePercent <= 0 ? 80m : request.MinPerformancePercent;
        standard.ExcellentPerformancePercent = request.ExcellentPerformancePercent <= 0 ? 120m : request.ExcellentPerformancePercent;
        standard.WarehouseId = request.WarehouseId;
        standard.ZoneId = request.ZoneId;
        standard.ItemClass = Clean(request.ItemClass);
        standard.EffectiveFrom = request.EffectiveFrom == default ? Now.Date : request.EffectiveFrom.Date;
        standard.EffectiveTo = request.EffectiveTo?.Date;
        standard.IsActive = true;

        await _unitOfWork.SaveChangesAsync(ct);
        return standard;
    }

    public async Task<LaborActivity> StartActivityAsync(LaborActivityRequest request, int? scopedWarehouseId = null, CancellationToken ct = default)
    {
        EnsureWarehouseScope(request.WarehouseId, scopedWarehouseId);
        if (string.IsNullOrWhiteSpace(request.UserName))
            throw new BusinessRuleException("Can nhan vien thuc hien tac vu.", "LABOR_ACTIVITY_USER_REQUIRED", "LaborActivity");
        if (string.IsNullOrWhiteSpace(request.TaskType))
            throw new BusinessRuleException("Can loai tac vu lao dong.", "LABOR_ACTIVITY_TASK_REQUIRED", "LaborActivity");

        if (!string.IsNullOrWhiteSpace(request.TaskSourceId))
        {
            var existing = await _db.LaborActivities.FirstOrDefaultAsync(x => x.TaskSourceType == request.TaskSourceType && x.TaskSourceId == request.TaskSourceId, ct);
            if (existing != null)
                return existing;
        }

        var start = request.StartedAt ?? Now;
        var activity = new LaborActivity
        {
            ActivityCode = await GenerateActivityCodeAsync(request.WarehouseId, start, ct),
            WarehouseId = request.WarehouseId,
            ZoneId = request.ZoneId,
            UserId = request.UserId,
            UserName = request.UserName.Trim(),
            ShiftCode = string.IsNullOrWhiteSpace(request.ShiftCode) ? BuildShiftCode(start) : request.ShiftCode.Trim(),
            TaskType = request.TaskType.Trim(),
            TaskSourceType = request.TaskSourceType.Trim(),
            TaskSourceId = Clean(request.TaskSourceId),
            TaskSourceCode = Clean(request.TaskSourceCode),
            OwnerPartnerId = request.OwnerPartnerId,
            ItemClass = Clean(request.ItemClass),
            WorkQuantity = request.WorkQuantity <= 0 ? 1m : request.WorkQuantity,
            UnitOfWork = string.IsNullOrWhiteSpace(request.UnitOfWork) ? "unit" : request.UnitOfWork.Trim(),
            StartedAt = start,
            Status = LaborActivityStatusEnum.InProgress,
            WaitingMinutes = Math.Max(0, request.WaitingMinutes),
            BacklogAtStart = Math.Max(0, request.BacklogAtStart),
            CreatedBy = CleanActor(request.Actor),
            CreatedAt = Now
        };
        activity.ExpectedMinutes = await CalculateExpectedMinutesAsync(activity, ct);

        _db.LaborActivities.Add(activity);
        await _unitOfWork.SaveChangesAsync(ct);
        return activity;
    }

    public async Task<LaborActivity> CompleteActivityAsync(long activityId, decimal? workQuantity, string? exceptionReason, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        var activity = await _db.LaborActivities.Include(x => x.ExceptionReviews).FirstOrDefaultAsync(x => x.LaborActivityId == activityId, ct)
            ?? throw new BusinessRuleException("Khong tim thay tac vu lao dong.", "LABOR_ACTIVITY_NOT_FOUND", "LaborActivity");
        EnsureWarehouseScope(activity.WarehouseId, scopedWarehouseId);
        activity.WorkQuantity = workQuantity.HasValue && workQuantity.Value > 0 ? workQuantity.Value : activity.WorkQuantity;
        activity.EndedAt ??= Now;
        activity.ActualMinutes = Math.Round((decimal)Math.Max(0.01, (activity.EndedAt.Value - activity.StartedAt).TotalMinutes), 4, MidpointRounding.AwayFromZero);
        activity.ExpectedMinutes = await CalculateExpectedMinutesAsync(activity, ct);
        activity.ProductivityPercent = activity.ActualMinutes <= 0
            ? 0
            : Math.Round(activity.ExpectedMinutes / activity.ActualMinutes * 100m, 4, MidpointRounding.AwayFromZero);
        activity.Status = LaborActivityStatusEnum.Completed;

        var standard = await FindStandardAsync(activity, ct);
        var minPerformance = standard?.MinPerformancePercent ?? 80m;
        if (!string.IsNullOrWhiteSpace(exceptionReason) || activity.ProductivityPercent < minPerformance || activity.WaitingMinutes > 30)
        {
            activity.IsException = true;
            activity.ExceptionReason = Clean(exceptionReason) ?? $"Productivity {activity.ProductivityPercent:N2}% below {minPerformance:N2}% or waiting time high.";
            activity.Status = LaborActivityStatusEnum.Exception;
            if (!activity.ExceptionReviews.Any(r => r.Status == LaborExceptionStatusEnum.Open))
            {
                activity.ExceptionReviews.Add(new LaborExceptionReview
                {
                    Status = LaborExceptionStatusEnum.Open,
                    Reason = activity.ExceptionReason,
                    ProductivityBefore = activity.ProductivityPercent,
                    ProductivityAfter = activity.ProductivityPercent,
                    RequestedBy = CleanActor(actor),
                    RequestedAt = Now
                });
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return activity;
    }

    public async Task<int> CaptureCompletedWarehouseTasksAsync(int? warehouseId = null, int days = 30, CancellationToken ct = default)
    {
        var cutoff = Now.AddDays(-Math.Max(1, days));
        var count = 0;

        var pickTasks = await _db.PickTasks.AsNoTracking()
            .Include(t => t.Voucher)
            .Include(t => t.Wave)
            .Include(t => t.SourceLocation)
            .Where(t => t.CompletedAt.HasValue
                && t.CompletedAt.Value >= cutoff
                && !string.IsNullOrWhiteSpace(t.AssignedTo)
                && t.Status == PickTaskStatusEnum.Completed
                && (!warehouseId.HasValue || (t.Wave != null && t.Wave.WarehouseId == warehouseId.Value) || t.Voucher.WarehouseId == warehouseId.Value))
            .OrderByDescending(t => t.CompletedAt)
            .ThenByDescending(t => t.PickTaskId)
            .Take(500)
            .ToListAsync(ct);
        foreach (var task in pickTasks)
        {
            var wh = task.Wave?.WarehouseId ?? task.Voucher.WarehouseId;
            if (await TryCaptureAsync(new LaborActivityRequest
            {
                WarehouseId = wh,
                ZoneId = task.SourceLocation?.ZoneId,
                UserName = task.AssignedTo!,
                TaskType = "Pick",
                TaskSourceType = "PickTask",
                TaskSourceId = task.PickTaskId.ToString(),
                TaskSourceCode = task.TaskCode,
                OwnerPartnerId = task.OwnerPartnerId,
                WorkQuantity = task.PickedQty <= 0 ? task.TargetQty : task.PickedQty,
                UnitOfWork = "unit",
                StartedAt = task.StartedAt ?? task.AssignedAt ?? task.CompletedAt,
                WaitingMinutes = task.AssignedAt.HasValue && task.StartedAt.HasValue ? (int)Math.Max(0, (task.StartedAt.Value - task.AssignedAt.Value).TotalMinutes) : 0,
                Actor = "labor-capture"
            }, task.CompletedAt!.Value, ct))
                count++;
        }

        var moves = await _db.MovementTasks.AsNoTracking()
            .Where(t => t.CompletedAt.HasValue
                && t.CompletedAt.Value >= cutoff
                && !string.IsNullOrWhiteSpace(t.CompletedBy ?? t.AssignedTo)
                && t.Status == MovementTaskStatusEnum.Completed
                && (!warehouseId.HasValue || t.WarehouseId == warehouseId.Value))
            .OrderByDescending(t => t.CompletedAt)
            .ThenByDescending(t => t.MovementTaskId)
            .Take(500)
            .ToListAsync(ct);
        foreach (var task in moves)
        {
            if (await TryCaptureAsync(new LaborActivityRequest
            {
                WarehouseId = task.WarehouseId,
                ZoneId = task.SourceZoneId ?? task.DestinationZoneId,
                UserName = task.CompletedBy ?? task.AssignedTo ?? "system",
                TaskType = task.TaskType.ToString(),
                TaskSourceType = "MovementTask",
                TaskSourceId = task.MovementTaskId.ToString(),
                TaskSourceCode = task.TaskCode,
                OwnerPartnerId = task.OwnerPartnerId,
                WorkQuantity = task.ConfirmedQty <= 0 ? task.PlannedQty : task.ConfirmedQty,
                UnitOfWork = task.MovementMode == MovementTaskModeEnum.Lpn ? "lpn" : "unit",
                StartedAt = task.StartedAt ?? task.AssignedAt ?? task.CompletedAt,
                WaitingMinutes = task.AssignedAt.HasValue && task.StartedAt.HasValue ? (int)Math.Max(0, (task.StartedAt.Value - task.AssignedAt.Value).TotalMinutes) : 0,
                Actor = "labor-capture"
            }, task.CompletedAt!.Value, ct))
                count++;
        }

        var vouchers = await _db.Vouchers.AsNoTracking()
            .Where(v => !v.IsCancelled
                && v.CompletedAt.HasValue
                && v.CompletedAt.Value >= cutoff
                && !string.IsNullOrWhiteSpace(v.CompletedBy ?? v.ReceivedBy ?? v.ShippedBy)
                && (!warehouseId.HasValue || v.WarehouseId == warehouseId.Value))
            .OrderByDescending(v => v.CompletedAt)
            .ThenByDescending(v => v.VoucherId)
            .Take(500)
            .ToListAsync(ct);
        foreach (var voucher in vouchers)
        {
            var taskType = voucher.VoucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham ? "Receive" : "Ship";
            if (await TryCaptureAsync(new LaborActivityRequest
            {
                WarehouseId = voucher.WarehouseId,
                UserName = voucher.CompletedBy ?? voucher.ReceivedBy ?? voucher.ShippedBy ?? "system",
                TaskType = taskType,
                TaskSourceType = "Voucher",
                TaskSourceId = voucher.VoucherId.ToString(),
                TaskSourceCode = voucher.VoucherCode,
                OwnerPartnerId = voucher.OwnerPartnerId,
                WorkQuantity = Math.Max(1, voucher.TotalLines),
                UnitOfWork = "line",
                StartedAt = voucher.ReceivedAt ?? voucher.PackedAt ?? voucher.ShippedAt ?? voucher.CompletedAt,
                WaitingMinutes = voucher.DockAppointmentStart.HasValue && voucher.ReceivedAt.HasValue ? (int)Math.Max(0, (voucher.ReceivedAt.Value - voucher.DockAppointmentStart.Value).TotalMinutes) : 0,
                Actor = "labor-capture"
            }, voucher.CompletedAt!.Value, ct))
                count++;
        }

        var vas = await _db.VasWorkOrders.AsNoTracking()
            .Where(v => v.CompletedAt.HasValue
                && v.CompletedAt.Value >= cutoff
                && !string.IsNullOrWhiteSpace(v.CompletedBy ?? v.StartedBy)
                && v.Status == VasWorkOrderStatusEnum.Completed
                && (!warehouseId.HasValue || v.WarehouseId == warehouseId.Value))
            .OrderByDescending(v => v.CompletedAt)
            .ThenByDescending(v => v.VasWorkOrderId)
            .Take(500)
            .ToListAsync(ct);
        foreach (var wo in vas)
        {
            if (await TryCaptureAsync(new LaborActivityRequest
            {
                WarehouseId = wo.WarehouseId,
                UserName = wo.CompletedBy ?? wo.StartedBy ?? "system",
                TaskType = "VAS",
                TaskSourceType = "VasWorkOrder",
                TaskSourceId = wo.VasWorkOrderId.ToString(),
                TaskSourceCode = wo.WorkOrderCode,
                OwnerPartnerId = wo.OwnerPartnerId,
                WorkQuantity = wo.CompletedQty <= 0 ? 1 : wo.CompletedQty,
                UnitOfWork = "unit",
                StartedAt = wo.StartedAt ?? wo.CompletedAt,
                Actor = "labor-capture"
            }, wo.CompletedAt!.Value, ct))
                count++;
        }

        return count;
    }

    public async Task<LaborExceptionReview> ApproveExceptionAsync(long reviewId, bool approve, decimal productivityAfter, decimal incentiveAmount, string notes, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        var review = await _db.LaborExceptionReviews.Include(x => x.LaborActivity).FirstOrDefaultAsync(x => x.LaborExceptionReviewId == reviewId, ct)
            ?? throw new BusinessRuleException("Khong tim thay de xuat xu ly ngoai le nang suat.", "LABOR_EXCEPTION_NOT_FOUND", "LaborExceptionReview");
        EnsureWarehouseScope(review.LaborActivity.WarehouseId, scopedWarehouseId);
        review.Status = approve ? LaborExceptionStatusEnum.Approved : LaborExceptionStatusEnum.Rejected;
        review.ProductivityAfter = approve && productivityAfter > 0 ? productivityAfter : review.ProductivityBefore;
        review.IncentiveAmount = approve ? Math.Max(0, incentiveAmount) : 0;
        review.ApprovedAt = Now;
        review.ApprovedBy = CleanActor(actor);
        review.ResolutionNotes = Clean(notes);

        if (approve)
        {
            review.LaborActivity.ProductivityPercent = review.ProductivityAfter;
            review.LaborActivity.Status = LaborActivityStatusEnum.Completed;
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return review;
    }

    private async Task<bool> TryCaptureAsync(LaborActivityRequest request, DateTime endedAt, CancellationToken ct)
    {
        var existing = await StartActivityAsync(request, null, ct);
        if (existing.EndedAt.HasValue)
            return false;
        existing.EndedAt = endedAt;
        existing.ActualMinutes = Math.Round((decimal)Math.Max(0.01, (endedAt - existing.StartedAt).TotalMinutes), 4, MidpointRounding.AwayFromZero);
        existing.ExpectedMinutes = await CalculateExpectedMinutesAsync(existing, ct);
        existing.ProductivityPercent = existing.ActualMinutes <= 0 ? 0 : Math.Round(existing.ExpectedMinutes / existing.ActualMinutes * 100m, 4, MidpointRounding.AwayFromZero);
        existing.Status = LaborActivityStatusEnum.Completed;
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    private async Task<decimal> CalculateExpectedMinutesAsync(LaborActivity activity, CancellationToken ct)
    {
        var standard = await FindStandardAsync(activity, ct);
        if (standard == null)
            return Math.Max(1m, activity.WorkQuantity * 5m);
        return Math.Round(Math.Max(1m, activity.WorkQuantity * standard.ExpectedMinutesPerUnit), 4, MidpointRounding.AwayFromZero);
    }

    private async Task<LaborStandard?> FindStandardAsync(LaborActivity activity, CancellationToken ct)
    {
        var query = _db.LaborStandards.AsNoTracking()
            .Where(s => s.IsActive
                && s.TaskType == activity.TaskType
                && (!s.WarehouseId.HasValue || s.WarehouseId == activity.WarehouseId)
                && (!s.ZoneId.HasValue || s.ZoneId == activity.ZoneId)
                && (s.ItemClass == null || s.ItemClass == activity.ItemClass)
                && s.EffectiveFrom <= activity.StartedAt.Date
                && (!s.EffectiveTo.HasValue || s.EffectiveTo.Value >= activity.StartedAt.Date));

        return await query
            .OrderByDescending(s => s.WarehouseId.HasValue)
            .ThenByDescending(s => s.ZoneId.HasValue)
            .ThenByDescending(s => s.ItemClass != null)
            .ThenByDescending(s => s.EffectiveFrom)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<string> GenerateActivityCodeAsync(int warehouseId, DateTime date, CancellationToken ct)
    {
        var prefix = $"LAB-{date:yyyyMMdd}-{warehouseId}-";
        var count = await _db.LaborActivities.CountAsync(x => x.ActivityCode.StartsWith(prefix), ct);
        return $"{prefix}{count + 1:0000}";
    }

    private static string BuildShiftCode(DateTime value)
        => value.Hour switch
        {
            >= 6 and < 14 => "DAY",
            >= 14 and < 22 => "SWING",
            _ => "NIGHT"
        };

    private static void EnsureWarehouseScope(int? warehouseId, int? scopedWarehouseId)
    {
        if (scopedWarehouseId.HasValue && warehouseId.HasValue && warehouseId.Value != scopedWarehouseId.Value)
            throw new UnauthorizedAccessException("Khong duoc thao tac du lieu labor cua kho khac.");
    }

    private static string CleanActor(string? actor)
        => string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim()[..Math.Min(actor.Trim().Length, 100)];

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
