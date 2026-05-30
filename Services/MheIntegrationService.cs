using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public sealed class MheCommandRequest
{
    public int WarehouseId { get; init; }
    public int? OwnerPartnerId { get; init; }
    public MheCommandTypeEnum CommandType { get; init; }
    public long? PickTaskId { get; init; }
    public long? MovementTaskId { get; init; }
    public long? WaveId { get; init; }
    public long? OutboundPackageId { get; init; }
    public long? LicensePlateId { get; init; }
    public string? SourceType { get; init; }
    public string? SourceId { get; init; }
    public string? SourceCode { get; init; }
    public string? PayloadJson { get; init; }
    public string IdempotencyKey { get; init; } = "";
    public string Actor { get; init; } = "system";
}

public sealed class MheCallbackRequest
{
    public string CorrelationId { get; init; } = "";
    public string IdempotencyKey { get; init; } = "";
    public MheCommandStatusEnum Status { get; init; }
    public string? ExternalMissionId { get; init; }
    public string? Message { get; init; }
    public string? PayloadJson { get; init; }
}

public interface IMheIntegrationService
{
    Task<MheCommand> CreateCommandAsync(MheCommandRequest request, CancellationToken ct = default);
    Task<MheCommand> CreateFromPickTaskAsync(long pickTaskId, string actor, CancellationToken ct = default);
    Task<MheCommand> CreateFromMovementTaskAsync(long movementTaskId, string actor, CancellationToken ct = default);
    Task<MheCommand> CreateFromWaveAsync(long waveId, string actor, CancellationToken ct = default);
    Task<MheCommand> ProcessCallbackAsync(MheCallbackRequest request, CancellationToken ct = default);
    Task<MheCommand> RetryAsync(long commandId, string actor, CancellationToken ct = default);
    Task<MheCommand> CancelAsync(long commandId, string actor, CancellationToken ct = default);
}

public class MheIntegrationService : IMheIntegrationService
{
    private readonly AppDbContext _db;
    private readonly IIntegrationService _integrationService;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;

    public MheIntegrationService(
        AppDbContext db,
        IIntegrationService integrationService,
        IConfiguration configuration,
        IUnitOfWork? unitOfWork = null)
    {
        _db = db;
        _integrationService = integrationService;
        _configuration = configuration;
        _unitOfWork = unitOfWork ?? new EfUnitOfWork(db);
    }

    private static DateTime Now => VietnamTime.Now;

    public async Task<MheCommand> CreateFromPickTaskAsync(long pickTaskId, string actor, CancellationToken ct = default)
    {
        var task = await _db.PickTasks
            .Include(t => t.Voucher)
            .Include(t => t.Item)
            .Include(t => t.SourceLocation)
            .FirstOrDefaultAsync(t => t.PickTaskId == pickTaskId, ct);
        if (task == null)
            throw new BusinessRuleException("Không tìm thấy pick task.", "MHE_PICK_TASK_NOT_FOUND", "PickTask");

        return await CreateCommandAsync(new MheCommandRequest
        {
            WarehouseId = task.Voucher?.WarehouseId ?? task.Wave?.WarehouseId ?? 0,
            OwnerPartnerId = task.OwnerPartnerId ?? task.Voucher?.OwnerPartnerId,
            CommandType = MheCommandTypeEnum.MoveTote,
            PickTaskId = task.PickTaskId,
            WaveId = task.WaveId,
            SourceType = "PickTask",
            SourceId = task.PickTaskId.ToString(),
            SourceCode = task.TaskCode,
            PayloadJson = JsonSerializer.Serialize(new
            {
                task.TaskCode,
                task.ItemId,
                ItemCode = task.Item?.ItemCode,
                task.SourceLocationId,
                SourceLocationCode = task.SourceLocation?.LocationCode,
                task.TargetLocationId,
                task.TargetQty
            }),
            IdempotencyKey = $"mhe:pick:{task.PickTaskId}",
            Actor = actor
        }, ct);
    }

    public async Task<MheCommand> CreateFromMovementTaskAsync(long movementTaskId, string actor, CancellationToken ct = default)
    {
        var task = await _db.MovementTasks
            .Include(t => t.Item)
            .Include(t => t.SourceLocation)
            .Include(t => t.DestinationLocation)
            .Include(t => t.LicensePlate)
            .FirstOrDefaultAsync(t => t.MovementTaskId == movementTaskId, ct);
        if (task == null)
            throw new BusinessRuleException("Không tìm thấy movement task.", "MHE_MOVEMENT_TASK_NOT_FOUND", "MovementTask");

        return await CreateCommandAsync(new MheCommandRequest
        {
            WarehouseId = task.WarehouseId,
            OwnerPartnerId = task.OwnerPartnerId,
            CommandType = task.MovementMode == MovementTaskModeEnum.Lpn ? MheCommandTypeEnum.MoveLpn : MheCommandTypeEnum.MoveInventory,
            MovementTaskId = task.MovementTaskId,
            LicensePlateId = task.LicensePlateId,
            SourceType = "MovementTask",
            SourceId = task.MovementTaskId.ToString(),
            SourceCode = task.TaskCode,
            PayloadJson = JsonSerializer.Serialize(new
            {
                task.TaskCode,
                task.MovementMode,
                task.ItemId,
                ItemCode = task.Item?.ItemCode,
                task.SourceLocationId,
                SourceLocationCode = task.SourceLocation?.LocationCode,
                task.DestinationLocationId,
                DestinationLocationCode = task.DestinationLocation?.LocationCode,
                task.LpnCodeSnapshot,
                task.PlannedQty
            }),
            IdempotencyKey = $"mhe:movement:{task.MovementTaskId}",
            Actor = actor
        }, ct);
    }

    public async Task<MheCommand> CreateFromWaveAsync(long waveId, string actor, CancellationToken ct = default)
    {
        var wave = await _db.Waves.FirstOrDefaultAsync(w => w.WaveId == waveId, ct);
        if (wave == null)
            throw new BusinessRuleException("Không tìm thấy wave.", "MHE_WAVE_NOT_FOUND", "Wave");

        return await CreateCommandAsync(new MheCommandRequest
        {
            WarehouseId = wave.WarehouseId,
            OwnerPartnerId = wave.OwnerPartnerId,
            CommandType = MheCommandTypeEnum.ReleaseWave,
            WaveId = wave.WaveId,
            SourceType = "Wave",
            SourceId = wave.WaveId.ToString(),
            SourceCode = wave.WaveCode,
            PayloadJson = JsonSerializer.Serialize(new { wave.WaveId, wave.WaveCode, wave.WaveProfile, wave.Priority }),
            IdempotencyKey = $"mhe:wave:{wave.WaveId}",
            Actor = actor
        }, ct);
    }

    public async Task<MheCommand> CreateCommandAsync(MheCommandRequest request, CancellationToken ct = default)
    {
        var idempotencyKey = CleanRequired(request.IdempotencyKey, 160, "idempotency key");
        var existing = await _db.MheCommands.FirstOrDefaultAsync(c => c.IdempotencyKey == idempotencyKey, ct);
        if (existing != null)
            return existing;

        if (request.WarehouseId <= 0)
            throw new BusinessRuleException("MHE command thiếu warehouse.", "MHE_WAREHOUSE_REQUIRED", "MheCommand");

        var system = await ResolveDispatchSystemAsync(request.WarehouseId, ct);

        var correlationId = Guid.NewGuid().ToString("N");
        var command = new MheCommand
        {
            CommandCode = await GenerateCommandCodeAsync(request.WarehouseId, ct),
            WarehouseId = request.WarehouseId,
            OwnerPartnerId = request.OwnerPartnerId,
            MheSystemId = system?.MheSystemId,
            CommandType = request.CommandType,
            Status = system == null ? MheCommandStatusEnum.Pending : MheCommandStatusEnum.Queued,
            PickTaskId = request.PickTaskId,
            MovementTaskId = request.MovementTaskId,
            WaveId = request.WaveId,
            OutboundPackageId = request.OutboundPackageId,
            LicensePlateId = request.LicensePlateId,
            SourceType = Clean(request.SourceType, 80),
            SourceId = Clean(request.SourceId, 80),
            SourceCode = Clean(request.SourceCode, 120),
            IdempotencyKey = idempotencyKey,
            CorrelationId = correlationId,
            PayloadJson = string.IsNullOrWhiteSpace(request.PayloadJson) ? "{}" : request.PayloadJson,
            CreatedBy = CleanActor(request.Actor),
            CreatedAt = Now
        };
        _db.MheCommands.Add(command);
        await _unitOfWork.SaveChangesAsync(ct);

        if (system != null)
            await DispatchAsync(command, system, ct);

        return command;
    }

    public async Task<MheCommand> ProcessCallbackAsync(MheCallbackRequest request, CancellationToken ct = default)
    {
        var correlationId = CleanRequired(request.CorrelationId, 80, "correlation id");
        var eventKey = CleanRequired(request.IdempotencyKey, 160, "callback idempotency key");
        var command = await _db.MheCommands.FirstOrDefaultAsync(c => c.CorrelationId == correlationId, ct);
        if (command == null)
            throw new BusinessRuleException("Không tìm thấy command MHE theo correlation id.", "MHE_COMMAND_NOT_FOUND", "MheCommand");

        var duplicate = await _db.MheMissionEvents.AnyAsync(e => e.IdempotencyKey == eventKey, ct);
        if (duplicate)
            return command;

        command.Status = request.Status;
        command.LastCallbackJson = string.IsNullOrWhiteSpace(request.PayloadJson) ? "{}" : request.PayloadJson;
        command.UpdatedAt = Now;
        ApplyStatusTimestamp(command, request.Status);
        if (request.Status == MheCommandStatusEnum.Failed || request.Status == MheCommandStatusEnum.DeadLetter)
            command.LastError = Clean(request.Message, 500);

        _db.MheMissionEvents.Add(new MheMissionEvent
        {
            MheCommandId = command.MheCommandId,
            Status = request.Status,
            ExternalMissionId = Clean(request.ExternalMissionId, 80),
            Message = Clean(request.Message, 500),
            IdempotencyKey = eventKey,
            PayloadJson = string.IsNullOrWhiteSpace(request.PayloadJson) ? "{}" : request.PayloadJson,
            EventAt = Now
        });

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // P2-R2-6: 2 callback song song vượt qua duplicate check ở dòng 216, unique index chặn được ở DB.
            // Nếu phát hiện row đã tồn tại → idempotent return; ngược lại rethrow.
            var alreadyExists = await _db.MheMissionEvents.AsNoTracking().AnyAsync(e => e.IdempotencyKey == eventKey, ct);
            if (!alreadyExists) throw;
        }
        return command;
    }

    public async Task<MheCommand> RetryAsync(long commandId, string actor, CancellationToken ct = default)
    {
        var command = await _db.MheCommands.Include(c => c.MheSystem).FirstOrDefaultAsync(c => c.MheCommandId == commandId, ct);
        if (command == null)
            throw new BusinessRuleException("Không tìm thấy MHE command.", "MHE_COMMAND_NOT_FOUND", "MheCommand");
        if (command.Status is not (MheCommandStatusEnum.Failed or MheCommandStatusEnum.DeadLetter or MheCommandStatusEnum.Pending))
            throw new BusinessRuleException("Chỉ retry command pending/failed/dead-letter.", "MHE_RETRY_STATUS_INVALID", "MheCommand");

        var system = command.MheSystem ?? await ResolveDispatchSystemAsync(command.WarehouseId, ct);
        command.RetryCount += 1;
        command.Status = system == null ? MheCommandStatusEnum.Pending : MheCommandStatusEnum.Queued;
        command.MheSystemId = system?.MheSystemId;
        command.LastError = null;
        command.UpdatedAt = Now;
        await _unitOfWork.SaveChangesAsync(ct);

        if (system != null)
            await DispatchAsync(command, system, ct);
        return command;
    }

    public async Task<MheCommand> CancelAsync(long commandId, string actor, CancellationToken ct = default)
    {
        var command = await _db.MheCommands.FirstOrDefaultAsync(c => c.MheCommandId == commandId, ct);
        if (command == null)
            throw new BusinessRuleException("Không tìm thấy MHE command.", "MHE_COMMAND_NOT_FOUND", "MheCommand");
        if (command.Status is MheCommandStatusEnum.Completed or MheCommandStatusEnum.Cancelled)
            return command;

        command.Status = MheCommandStatusEnum.Cancelled;
        command.CancelledAt = Now;
        command.UpdatedAt = Now;
        await _unitOfWork.SaveChangesAsync(ct);
        return command;
    }

    private async Task DispatchAsync(MheCommand command, MheSystem system, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(system.EndpointUrl))
            return;

        var payload = new
        {
            command.CommandCode,
            command.CommandType,
            command.CorrelationId,
            command.WarehouseId,
            command.OwnerPartnerId,
            command.SourceType,
            command.SourceId,
            command.SourceCode,
            command.PayloadJson
        };

        await _integrationService.EnqueueAsync(
            OutboxEventTypeEnum.MheCommandDispatched,
            system.EndpointUrl,
            payload,
            command.IdempotencyKey,
            system.SystemCode);

        command.Status = MheCommandStatusEnum.Sent;
        command.SentAt = Now;
        command.UpdatedAt = Now;
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<MheSystem?> ResolveDispatchSystemAsync(int warehouseId, CancellationToken ct)
    {
        if (!_configuration.GetValue<bool>("MheIntegration:Enabled"))
            return null;

        return await _db.MheSystems.AsNoTracking()
            .Where(s => s.WarehouseId == warehouseId && s.IsActive && s.EndpointUrl != null && s.EndpointUrl != "")
            .OrderBy(s => s.MheSystemId)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<string> GenerateCommandCodeAsync(int warehouseId, CancellationToken ct)
    {
        var prefix = $"MHE-{Now:yyyyMMdd}-{warehouseId}-";
        var count = await _db.MheCommands.CountAsync(c => c.CommandCode.StartsWith(prefix), ct);
        return $"{prefix}{count + 1:0000}";
    }

    private static void ApplyStatusTimestamp(MheCommand command, MheCommandStatusEnum status)
    {
        switch (status)
        {
            case MheCommandStatusEnum.Acknowledged:
                command.AcknowledgedAt ??= Now;
                break;
            case MheCommandStatusEnum.InProgress:
                command.StartedAt ??= Now;
                break;
            case MheCommandStatusEnum.Completed:
                command.CompletedAt ??= Now;
                break;
            case MheCommandStatusEnum.Failed:
            case MheCommandStatusEnum.DeadLetter:
                command.FailedAt ??= Now;
                break;
            case MheCommandStatusEnum.Cancelled:
                command.CancelledAt ??= Now;
                break;
        }
    }

    private static string CleanActor(string? value)
        => Clean(value, 100) ?? "system";

    private static string? Clean(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string CleanRequired(string? value, int maxLength, string field)
    {
        var cleaned = Clean(value, maxLength);
        if (cleaned == null)
            throw new BusinessRuleException($"MHE command thiếu {field}.", "MHE_REQUIRED_FIELD_MISSING", "MheCommand");
        return cleaned;
    }
}
