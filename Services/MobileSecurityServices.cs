using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using WMS.Authorization;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public enum RfWorkflowProcess
{
    Receiving = 1,
    Picking = 2,
    Movement = 3,
    CycleCount = 4,
    Shipping = 5,
    Return = 6
}

public enum RfScanStepType
{
    ScanLocation = 1,
    ScanItem = 2,
    ScanLot = 3,
    ScanSerial = 4,
    ScanLpn = 5,
    ScanQuantity = 6,
    ScanDamageReason = 7,
    Confirm = 8,
    ScanUserBadge = 9
}

public sealed class RfWorkflowStep
{
    public int Sequence { get; init; }
    public RfScanStepType StepType { get; init; }
    public string Label { get; init; } = "";
    public bool IsRequired { get; init; } = true;
    public string? ValidationRule { get; init; }
}

public sealed class RfWorkflowProfile
{
    public string ProfileCode { get; init; } = "";
    public RfWorkflowProcess Process { get; init; }
    public string RoleName { get; init; } = "";
    public int? WarehouseId { get; init; }
    public bool IsActive { get; init; } = true;
    public IReadOnlyList<RfWorkflowStep> Steps { get; init; } = Array.Empty<RfWorkflowStep>();
}

public sealed class RfWorkflowBuildRequest
{
    public RfWorkflowProcess Process { get; init; }
    public string RoleName { get; init; } = "";
    public int? WarehouseId { get; init; }
    public IReadOnlyList<RfWorkflowProfile> Profiles { get; init; } = Array.Empty<RfWorkflowProfile>();
}

public sealed class RfWorkflowDefinition
{
    public string ProfileCode { get; init; } = "";
    public RfWorkflowProcess Process { get; init; }
    public string RoleName { get; init; } = "";
    public int? WarehouseId { get; init; }
    public IReadOnlyList<RfWorkflowStep> Steps { get; init; } = Array.Empty<RfWorkflowStep>();
    public bool IsDefaultProfile { get; init; }
}

public interface IRfWorkflowBuilderService
{
    RfWorkflowDefinition Build(RfWorkflowBuildRequest request);
    RfWorkflowProfile ValidateAndNormalizeProfile(RfWorkflowProfile profile, string actorRoleName);
}

public sealed class RfWorkflowBuilderService : IRfWorkflowBuilderService
{
    public RfWorkflowDefinition Build(RfWorkflowBuildRequest request)
    {
        var role = Clean(request.RoleName);
        var profile = request.Profiles
            .Where(p => p.IsActive && p.Process == request.Process)
            .OrderByDescending(p => p.WarehouseId == request.WarehouseId)
            .ThenByDescending(p => string.Equals(p.RoleName, role, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(p => p.WarehouseId.HasValue)
            .FirstOrDefault(p =>
                (p.WarehouseId == null || p.WarehouseId == request.WarehouseId)
                && (string.IsNullOrWhiteSpace(p.RoleName) || string.Equals(p.RoleName, role, StringComparison.OrdinalIgnoreCase)));

        if (profile == null)
        {
            return new RfWorkflowDefinition
            {
                ProfileCode = $"DEFAULT-{request.Process}-{role}".ToUpperInvariant(),
                Process = request.Process,
                RoleName = role,
                WarehouseId = request.WarehouseId,
                Steps = DefaultSteps(request.Process),
                IsDefaultProfile = true
            };
        }

        var normalized = ValidateAndNormalizeProfile(profile, "Admin");
        return new RfWorkflowDefinition
        {
            ProfileCode = normalized.ProfileCode,
            Process = normalized.Process,
            RoleName = normalized.RoleName,
            WarehouseId = normalized.WarehouseId,
            Steps = normalized.Steps,
            IsDefaultProfile = false
        };
    }

    public RfWorkflowProfile ValidateAndNormalizeProfile(RfWorkflowProfile profile, string actorRoleName)
    {
        if (!IsWorkflowConfigurator(actorRoleName))
            throw new UnauthorizedAccessException("Chỉ Admin hoặc Manager được cấu hình workflow RF.");

        var steps = profile.Steps
            .OrderBy(s => s.Sequence)
            .Select((s, index) => new RfWorkflowStep
            {
                Sequence = index + 1,
                StepType = s.StepType,
                Label = string.IsNullOrWhiteSpace(s.Label) ? LabelFor(s.StepType) : s.Label.Trim(),
                IsRequired = s.IsRequired,
                ValidationRule = string.IsNullOrWhiteSpace(s.ValidationRule) ? null : s.ValidationRule.Trim()
            })
            .ToList();

        if (steps.Count == 0)
            throw new BusinessRuleException("Workflow RF phải có ít nhất một bước quét.", "RF_WORKFLOW_EMPTY", nameof(RfWorkflowProfile));
        if (steps.Select(s => s.StepType).Distinct().Count() != steps.Count)
            throw new BusinessRuleException("Workflow RF không được khai báo trùng bước quét.", "RF_WORKFLOW_DUPLICATE_STEP", nameof(RfWorkflowProfile));
        if (!steps.Any(s => s.StepType == RfScanStepType.Confirm))
            steps.Add(new RfWorkflowStep { Sequence = steps.Count + 1, StepType = RfScanStepType.Confirm, Label = LabelFor(RfScanStepType.Confirm), IsRequired = true });

        return new RfWorkflowProfile
        {
            ProfileCode = string.IsNullOrWhiteSpace(profile.ProfileCode)
                ? $"RF-{profile.Process}-{Clean(profile.RoleName)}-{profile.WarehouseId?.ToString() ?? "ALL"}".ToUpperInvariant()
                : profile.ProfileCode.Trim().ToUpperInvariant(),
            Process = profile.Process,
            RoleName = Clean(profile.RoleName),
            WarehouseId = profile.WarehouseId,
            IsActive = profile.IsActive,
            Steps = steps
        };
    }

    private static IReadOnlyList<RfWorkflowStep> DefaultSteps(RfWorkflowProcess process)
        => process switch
        {
            RfWorkflowProcess.Receiving => new[]
            {
                Step(RfScanStepType.ScanLocation, 1),
                Step(RfScanStepType.ScanItem, 2),
                Step(RfScanStepType.ScanLot, 3, false),
                Step(RfScanStepType.ScanQuantity, 4),
                Step(RfScanStepType.Confirm, 5)
            },
            RfWorkflowProcess.Picking => new[]
            {
                Step(RfScanStepType.ScanLocation, 1),
                Step(RfScanStepType.ScanItem, 2),
                Step(RfScanStepType.ScanSerial, 3, false),
                Step(RfScanStepType.ScanQuantity, 4),
                Step(RfScanStepType.Confirm, 5)
            },
            RfWorkflowProcess.Movement => new[]
            {
                Step(RfScanStepType.ScanLocation, 1),
                Step(RfScanStepType.ScanLpn, 2, false),
                Step(RfScanStepType.ScanItem, 3, false),
                Step(RfScanStepType.ScanQuantity, 4),
                Step(RfScanStepType.Confirm, 5)
            },
            _ => new[] { Step(RfScanStepType.ScanLocation, 1), Step(RfScanStepType.ScanItem, 2), Step(RfScanStepType.Confirm, 3) }
        };

    private static RfWorkflowStep Step(RfScanStepType stepType, int sequence, bool required = true)
        => new() { Sequence = sequence, StepType = stepType, Label = LabelFor(stepType), IsRequired = required };

    private static string LabelFor(RfScanStepType stepType)
        => stepType switch
        {
            RfScanStepType.ScanLocation => "Quét vị trí",
            RfScanStepType.ScanItem => "Quét vật tư",
            RfScanStepType.ScanLot => "Quét lô",
            RfScanStepType.ScanSerial => "Quét serial",
            RfScanStepType.ScanLpn => "Quét LPN/pallet",
            RfScanStepType.ScanQuantity => "Nhập số lượng",
            RfScanStepType.ScanDamageReason => "Chọn lý do lỗi",
            RfScanStepType.ScanUserBadge => "Quét thẻ nhân viên",
            _ => "Xác nhận"
        };

    private static bool IsWorkflowConfigurator(string roleName)
        => string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase)
        || string.Equals(roleName, "Manager", StringComparison.OrdinalIgnoreCase);

    private static string Clean(string value) => string.IsNullOrWhiteSpace(value) ? "Staff" : value.Trim();
}

public enum OfflineQueueDecision
{
    SendNow = 1,
    WaitBackoff = 2,
    Blocked = 3,
    DeadLetter = 4,
    Conflict = 5
}

public sealed class OfflineQueuedOperation
{
    public string OperationId { get; init; } = "";
    public string OperationType { get; init; } = "";
    public string Status { get; init; } = "pending";
    public int Attempts { get; init; }
    public DateTime CreatedAt { get; init; } = VietnamTime.Now;
    public DateTime UpdatedAt { get; init; } = VietnamTime.Now;
    public DateTime? NextRetryAt { get; init; }
    public string? ServerConflictKey { get; init; }
}

public sealed class OfflineQueuePolicyResult
{
    public OfflineQueueDecision Decision { get; init; }
    public DateTime? NextRetryAt { get; init; }
    public string Message { get; init; } = "";
}

public interface IOfflineQueuePolicyService
{
    OfflineQueuePolicyResult Evaluate(OfflineQueuedOperation operation, DateTime now);
    OfflineQueuePolicyResult ClassifyServerResponse(int statusCode, string? serverConflictKey = null);
}

public sealed class OfflineQueuePolicyService : IOfflineQueuePolicyService
{
    private static readonly TimeSpan[] BackoffSteps =
    {
        TimeSpan.Zero,
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15)
    };

    public OfflineQueuePolicyResult Evaluate(OfflineQueuedOperation operation, DateTime now)
    {
        if (operation.Attempts >= 8)
            return new OfflineQueuePolicyResult { Decision = OfflineQueueDecision.DeadLetter, Message = "Thao tác đã thử quá số lần cho phép, cần xử lý thủ công." };
        if (string.Equals(operation.Status, "blocked", StringComparison.OrdinalIgnoreCase))
            return new OfflineQueuePolicyResult { Decision = OfflineQueueDecision.Blocked, Message = "Thao tác bị chặn bởi điều kiện nghiệp vụ hoặc phân quyền." };
        if (!string.IsNullOrWhiteSpace(operation.ServerConflictKey))
            return new OfflineQueuePolicyResult { Decision = OfflineQueueDecision.Conflict, Message = "Thao tác có xung đột idempotency với máy chủ." };

        var nextRetry = operation.NextRetryAt ?? operation.UpdatedAt.Add(BackoffFor(operation.Attempts));
        if (nextRetry > now)
            return new OfflineQueuePolicyResult { Decision = OfflineQueueDecision.WaitBackoff, NextRetryAt = nextRetry, Message = "Chưa đến thời điểm gửi lại theo retry policy." };

        return new OfflineQueuePolicyResult { Decision = OfflineQueueDecision.SendNow, Message = "Có thể gửi thao tác offline." };
    }

    public OfflineQueuePolicyResult ClassifyServerResponse(int statusCode, string? serverConflictKey = null)
    {
        if (statusCode == 409)
            return new OfflineQueuePolicyResult { Decision = OfflineQueueDecision.Conflict, Message = serverConflictKey ?? "Xung đột dữ liệu khi gửi lại thao tác." };
        if (statusCode is 400 or 401 or 403 or 419 or 422)
            return new OfflineQueuePolicyResult { Decision = OfflineQueueDecision.Blocked, Message = "Máy chủ từ chối thao tác, cần người vận hành kiểm tra." };
        return new OfflineQueuePolicyResult { Decision = OfflineQueueDecision.SendNow, Message = "Có thể retry theo backoff." };
    }

    private static TimeSpan BackoffFor(int attempts)
    {
        var index = Math.Clamp(attempts, 0, BackoffSteps.Length - 1);
        return BackoffSteps[index];
    }
}

public sealed class MobileDeviceRegistrationRequest
{
    public int UserId { get; init; }
    public string DeviceId { get; init; } = "";
    public string? UserAgent { get; init; }
    public string? IpAddress { get; init; }
    public bool IsKioskMode { get; init; }
    public bool PinRequired { get; init; }
}

public sealed class MobileDeviceRegistration
{
    public string DeviceHash { get; init; } = "";
    public int UserId { get; init; }
    public bool IsKioskMode { get; init; }
    public bool PinRequired { get; init; }
    public DateTime RegisteredAt { get; init; }
}

public sealed class MobileDeviceHealthReport
{
    public int UserId { get; init; }
    public string DeviceHash { get; init; } = "";
    public int PendingQueueCount { get; init; }
    public int? BatteryPercent { get; init; }
    public bool IsOnline { get; init; }
    public DateTime LastSeenAt { get; init; } = VietnamTime.Now;
}

public interface IMobileDeviceManagementService
{
    Task<MobileDeviceRegistration> RegisterAsync(MobileDeviceRegistrationRequest request, CancellationToken ct = default);
    Task RecordHealthAsync(MobileDeviceHealthReport report, CancellationToken ct = default);
    Task RevokeUserDevicesAsync(int userId, string actor, string reason, CancellationToken ct = default);
}

public sealed class MobileDeviceManagementService : IMobileDeviceManagementService
{
    private readonly AppDbContext _db;

    public MobileDeviceManagementService(AppDbContext db) => _db = db;

    public async Task<MobileDeviceRegistration> RegisterAsync(MobileDeviceRegistrationRequest request, CancellationToken ct = default)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.UserId == request.UserId && u.IsActive, ct)
            ?? throw new BusinessRuleException("Không tìm thấy người dùng đang hoạt động để đăng ký thiết bị.", "DEVICE_USER_NOT_FOUND", nameof(AppUser));
        var hash = BuildDeviceHash(request.UserId, request.DeviceId, request.UserAgent);
        var registeredAt = VietnamTime.Now;
        _db.LoginAuditLogs.Add(new LoginAuditLog
        {
            UserName = user.UserName,
            UserId = user.UserId,
            IsSuccess = true,
            Outcome = "DEVICE_REGISTERED",
            Reason = $"device={hash}; kiosk={request.IsKioskMode}; pin={request.PinRequired}",
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            CreatedAt = registeredAt
        });
        await _db.SaveChangesAsync(ct);

        return new MobileDeviceRegistration
        {
            DeviceHash = hash,
            UserId = user.UserId,
            IsKioskMode = request.IsKioskMode,
            PinRequired = request.PinRequired,
            RegisteredAt = registeredAt
        };
    }

    public async Task RecordHealthAsync(MobileDeviceHealthReport report, CancellationToken ct = default)
    {
        _db.LoginAuditLogs.Add(new LoginAuditLog
        {
            UserId = report.UserId,
            IsSuccess = report.IsOnline,
            Outcome = "DEVICE_HEALTH",
            Reason = $"device={report.DeviceHash}; queue={report.PendingQueueCount}; battery={report.BatteryPercent?.ToString() ?? "unknown"}",
            CreatedAt = report.LastSeenAt
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task RevokeUserDevicesAsync(int userId, string actor, string reason, CancellationToken ct = default)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng để thu hồi thiết bị.");
        user.TrustedDeviceRevokedAtUtc = DateTime.UtcNow;
        // R3-6: strip CRLF khỏi reason để chặn log injection (kẻ tấn công nhập "evil\r\n[INFO] fake").
        var safeReason = (reason ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
        var safeActor = (actor ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
        _db.LoginAuditLogs.Add(new LoginAuditLog
        {
            UserName = user.UserName,
            UserId = user.UserId,
            IsSuccess = true,
            Outcome = "DEVICE_REVOKED",
            Reason = $"actor={safeActor}; reason={safeReason}",
            CreatedAt = VietnamTime.Now
        });
        await _db.SaveChangesAsync(ct);
    }

    private static string BuildDeviceHash(int userId, string deviceId, string? userAgent)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{userId}|{deviceId.Trim()}|{userAgent?.Trim()}"));
        return Convert.ToHexString(bytes)[..32];
    }
}

public sealed class VoicePickingCommandResult
{
    public bool Success { get; init; }
    public string Intent { get; init; } = "";
    public decimal? Quantity { get; init; }
    public string? LocationCode { get; init; }
    public string? SerialCode { get; init; }
    public string Message { get; init; } = "";
}

public interface IVoicePickingAdapter
{
    Task<VoicePickingCommandResult> InterpretAsync(string spokenCommand, CancellationToken ct = default);
}

public sealed class VoicePickingSimulatorAdapter : IVoicePickingAdapter
{
    public Task<VoicePickingCommandResult> InterpretAsync(string spokenCommand, CancellationToken ct = default)
    {
        var text = (spokenCommand ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(Fail("Không nghe được lệnh voice picking."));
        if (text.Contains("repeat") || text.Contains("nhắc lại"))
            return Task.FromResult(Ok("repeat", "Nhắc lại nhiệm vụ hiện tại."));
        if (text.Contains("short") || text.Contains("thiếu"))
            return Task.FromResult(Ok("short", "Ghi nhận báo thiếu."));
        if (text.Contains("confirm") || text.Contains("xác nhận") || text.Contains("done"))
            return Task.FromResult(Ok("confirm", "Xác nhận bước lấy hàng.", ExtractQuantity(text), ExtractToken(text, "loc"), ExtractToken(text, "serial")));

        return Task.FromResult(Fail("Lệnh voice không hợp lệ. Hãy nói xác nhận, báo thiếu hoặc nhắc lại."));
    }

    private static VoicePickingCommandResult Ok(string intent, string message, decimal? qty = null, string? location = null, string? serial = null)
        => new() { Success = true, Intent = intent, Message = message, Quantity = qty, LocationCode = location, SerialCode = serial };

    private static VoicePickingCommandResult Fail(string message)
        => new() { Success = false, Intent = "unknown", Message = message };

    private static decimal? ExtractQuantity(string text)
    {
        var match = Regex.Match(text, @"(?:qty|quantity|số lượng)\s*(\d+(?:[.,]\d+)?)", RegexOptions.IgnoreCase);
        return match.Success && decimal.TryParse(match.Groups[1].Value.Replace(',', '.'), out var value) ? value : null;
    }

    private static string? ExtractToken(string text, string key)
    {
        var match = Regex.Match(text, key + @"\s*[:#-]?\s*([a-z0-9\-]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
    }
}

public enum AdvancedBarcodeKind
{
    Unknown = 0,
    Gs1 = 1,
    Pallet = 2,
    Serial = 3,
    Rfid = 4,
    Plain = 5
}

public sealed class AdvancedBarcodeParseResult
{
    public bool Success { get; init; }
    public AdvancedBarcodeKind Kind { get; init; }
    public string RawValue { get; init; } = "";
    public string? Gtin { get; init; }
    public string? Sscc { get; init; }
    public string? LotNumber { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public string? SerialNumber { get; init; }
    public string? PalletCode { get; init; }
    public string? RfidEpc { get; init; }
    public decimal? Quantity { get; init; }
    public string? ErrorMessage { get; init; }
}

public interface IAdvancedBarcodeParser
{
    AdvancedBarcodeParseResult Parse(string rawValue);
    IReadOnlyList<AdvancedBarcodeParseResult> ParseBulk(string rawValues);
}

public sealed class AdvancedBarcodeParser : IAdvancedBarcodeParser
{
    private const char GroupSeparator = (char)29;

    public AdvancedBarcodeParseResult Parse(string rawValue)
    {
        var raw = (rawValue ?? "").Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return Error(raw, "Mã quét đang trống.");

        if (raw.StartsWith("PLT:", StringComparison.OrdinalIgnoreCase) || raw.StartsWith("PALLET:", StringComparison.OrdinalIgnoreCase))
            return new AdvancedBarcodeParseResult { Success = true, Kind = AdvancedBarcodeKind.Pallet, RawValue = raw, PalletCode = raw.Split(':', 2)[1].Trim().ToUpperInvariant() };
        if (raw.StartsWith("SER:", StringComparison.OrdinalIgnoreCase) || raw.StartsWith("S:", StringComparison.OrdinalIgnoreCase))
            return new AdvancedBarcodeParseResult { Success = true, Kind = AdvancedBarcodeKind.Serial, RawValue = raw, SerialNumber = raw.Split(':', 2)[1].Trim().ToUpperInvariant() };
        if (raw.StartsWith("EPC:", StringComparison.OrdinalIgnoreCase) || raw.StartsWith("RFID:", StringComparison.OrdinalIgnoreCase))
            return new AdvancedBarcodeParseResult { Success = true, Kind = AdvancedBarcodeKind.Rfid, RawValue = raw, RfidEpc = raw.Split(':', 2)[1].Trim().ToUpperInvariant() };

        var value = raw.StartsWith("]C1", StringComparison.Ordinal) ? raw[3..] : raw;
        if (value.Contains("(01)", StringComparison.Ordinal) || value.StartsWith("01", StringComparison.Ordinal))
            return ParseGs1(raw, value.Replace("(", "").Replace(")", ""));

        return new AdvancedBarcodeParseResult { Success = true, Kind = AdvancedBarcodeKind.Plain, RawValue = raw };
    }

    public IReadOnlyList<AdvancedBarcodeParseResult> ParseBulk(string rawValues)
        => (rawValues ?? "")
            .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Parse)
            .ToList();

    private static AdvancedBarcodeParseResult ParseGs1(string raw, string compact)
    {
        string? gtin = null;
        string? sscc = null;
        string? lot = null;
        string? serial = null;
        DateTime? expiry = null;
        decimal? qty = null;

        var index = 0;
        while (index < compact.Length)
        {
            if (TryReadFixed(compact, ref index, "01", 14, out var gtinValue))
            {
                gtin = gtinValue;
                continue;
            }
            if (TryReadFixed(compact, ref index, "00", 18, out var ssccValue))
            {
                sscc = ssccValue;
                continue;
            }
            if (TryReadFixed(compact, ref index, "17", 6, out var expiryValue))
            {
                expiry = ParseGs1Date(expiryValue);
                continue;
            }
            if (TryReadVariable(compact, ref index, "10", 20, out var lotValue))
            {
                lot = lotValue;
                continue;
            }
            if (TryReadVariable(compact, ref index, "21", 20, out var serialValue))
            {
                serial = serialValue;
                continue;
            }
            if (TryReadVariable(compact, ref index, "37", 8, out var qtyValue))
            {
                qty = decimal.TryParse(qtyValue, out var parsed) ? parsed : null;
                continue;
            }

            return Error(raw, $"Không đọc được AI GS1 tại vị trí {index + 1}.");
        }

        if (gtin == null && sscc == null)
            return Error(raw, "Mã GS1 thiếu GTIN hoặc SSCC.");

        return new AdvancedBarcodeParseResult
        {
            Success = true,
            Kind = AdvancedBarcodeKind.Gs1,
            RawValue = raw,
            Gtin = gtin,
            Sscc = sscc,
            LotNumber = lot,
            ExpiryDate = expiry,
            SerialNumber = serial,
            Quantity = qty
        };
    }

    private static bool TryReadFixed(string value, ref int index, string ai, int length, out string result)
    {
        result = "";
        if (!value.AsSpan(index).StartsWith(ai, StringComparison.Ordinal)) return false;
        var start = index + ai.Length;
        if (start + length > value.Length) throw new BusinessRuleException($"Mã GS1 AI {ai} thiếu dữ liệu.", "GS1_INVALID", "Barcode");
        result = value.Substring(start, length);
        index = start + length;
        SkipGroupSeparator(value, ref index);
        return true;
    }

    private static bool TryReadVariable(string value, ref int index, string ai, int maxLength, out string result)
    {
        result = "";
        if (!value.AsSpan(index).StartsWith(ai, StringComparison.Ordinal)) return false;
        var start = index + ai.Length;
        var end = value.IndexOf(GroupSeparator, start);
        if (end < 0) end = Math.Min(value.Length, start + maxLength);
        result = value[start..end];
        index = end;
        SkipGroupSeparator(value, ref index);
        return true;
    }

    private static void SkipGroupSeparator(string value, ref int index)
    {
        if (index < value.Length && value[index] == GroupSeparator)
            index++;
    }

    private static DateTime? ParseGs1Date(string yymmdd)
    {
        if (yymmdd.Length != 6) return null;
        var year = 2000 + int.Parse(yymmdd[..2]);
        var month = int.Parse(yymmdd.Substring(2, 2));
        var day = int.Parse(yymmdd.Substring(4, 2));
        day = day == 0 ? DateTime.DaysInMonth(year, month) : day;
        return new DateTime(year, month, day);
    }

    private static AdvancedBarcodeParseResult Error(string raw, string message)
        => new() { Success = false, Kind = AdvancedBarcodeKind.Unknown, RawValue = raw, ErrorMessage = message };
}

public sealed class ExternalIdentityLoginRequest
{
    public string Provider { get; init; } = "";
    public IReadOnlyDictionary<string, string> Claims { get; init; } = new Dictionary<string, string>();
}

public sealed class ExternalIdentityMappingResult
{
    public bool Success { get; init; }
    public ClaimsPrincipal? Principal { get; init; }
    public AppUser? User { get; init; }
    public string Message { get; init; } = "";
}

public interface IExternalIdentityMappingService
{
    Task<ExternalIdentityMappingResult> MapAsync(ExternalIdentityLoginRequest request, CancellationToken ct = default);
}

public sealed class ExternalIdentityMappingService : IExternalIdentityMappingService
{
    private static readonly HashSet<string> SupportedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "OIDC", "OpenIDConnect", "AzureAD", "Okta", "SAML", "SAML2"
    };

    private readonly AppDbContext _db;

    public ExternalIdentityMappingService(AppDbContext db) => _db = db;

    public async Task<ExternalIdentityMappingResult> MapAsync(ExternalIdentityLoginRequest request, CancellationToken ct = default)
    {
        if (!SupportedProviders.Contains(request.Provider))
            return new ExternalIdentityMappingResult { Success = false, Message = "Identity provider chưa được hỗ trợ." };

        var userName = ReadClaim(request.Claims, ClaimTypes.Name, "preferred_username", "upn", "email", "name");
        if (string.IsNullOrWhiteSpace(userName))
            return new ExternalIdentityMappingResult { Success = false, Message = "SSO thiếu claim định danh người dùng." };

        var user = await _db.AppUsers
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.IsActive && (u.UserName == userName || u.Email == userName), ct);
        if (user == null)
            return new ExternalIdentityMappingResult { Success = false, Message = "Tài khoản SSO chưa được cấp trong WMS." };

        var requestedRole = ReadClaim(request.Claims, ClaimTypes.Role, "role", "roles", "wms_role");
        if (!string.IsNullOrWhiteSpace(requestedRole)
            && !string.Equals(requestedRole, user.Role.RoleName, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(user.Role.RoleName, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return new ExternalIdentityMappingResult { Success = false, Message = "Role từ identity provider không khớp phân quyền WMS." };
        }

        var warehouseClaim = ReadClaim(request.Claims, "WarehouseId", "warehouse_id", "warehouse");
        if (user.WarehouseId.HasValue && !string.IsNullOrWhiteSpace(warehouseClaim)
            && int.TryParse(warehouseClaim, out var wh) && wh != user.WarehouseId.Value)
        {
            return new ExternalIdentityMappingResult { Success = false, Message = "Claim kho không nằm trong phạm vi tài khoản WMS." };
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Role, user.Role.RoleName)
        };
        if (user.WarehouseId.HasValue)
            claims.Add(new Claim("WarehouseId", user.WarehouseId.Value.ToString()));

        var permissions = await _db.RolePermissions
            .AsNoTracking()
            .Include(rp => rp.Permission)
            .Where(rp => rp.RoleId == user.RoleId && rp.Permission != null)
            .Select(rp => rp.Permission!.Code)
            .ToListAsync(ct);
        claims.AddRange(permissions.Select(p => new Claim(PermissionClaimTypes.Permission, p)));

        var ownerIds = await ResolveOwnerClaimsAsync(user.UserId, request.Claims, ct);
        claims.AddRange(ownerIds.Select(id => new Claim(TenantClaimTypes.OwnerPartnerId, id.ToString())));

        _db.LoginAuditLogs.Add(new LoginAuditLog
        {
            UserName = user.UserName,
            UserId = user.UserId,
            IsSuccess = true,
            Outcome = "SSO_MAPPED",
            Reason = request.Provider,
            CreatedAt = VietnamTime.Now
        });
        await _db.SaveChangesAsync(ct);

        return new ExternalIdentityMappingResult
        {
            Success = true,
            User = user,
            Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, request.Provider)),
            Message = "Đã map SSO vào tài khoản WMS."
        };
    }

    private async Task<IReadOnlyList<int>> ResolveOwnerClaimsAsync(int userId, IReadOnlyDictionary<string, string> claims, CancellationToken ct)
    {
        var ownerRaw = ReadClaim(claims, TenantClaimTypes.OwnerPartnerId, "owner_partner_id", "owner_ids", "owners");
        var claimed = (ownerRaw ?? "")
            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => int.TryParse(x, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var allowed = await _db.AppUserOwnerScopes.AsNoTracking()
            .Where(s => s.UserId == userId && s.IsActive)
            .Select(s => s.OwnerPartnerId)
            .ToListAsync(ct);
        if (claimed.Count == 0)
            return allowed;
        return claimed.Where(allowed.Contains).ToList();
    }

    private static string? ReadClaim(IReadOnlyDictionary<string, string> claims, params string[] names)
    {
        foreach (var name in names)
        {
            var hit = claims.FirstOrDefault(c => string.Equals(c.Key, name, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(hit.Value))
                return hit.Value.Trim();
        }

        return null;
    }
}

public interface IProductionMfaLockoutService
{
    Task<bool> RegisterFailedAttemptAsync(string userName, CancellationToken ct = default);
    Task ResetPasswordByAdminAsync(int userId, string newPassword, string actor, CancellationToken ct = default);
}

public sealed class ProductionMfaLockoutService : IProductionMfaLockoutService
{
    private readonly AppDbContext _db;

    public ProductionMfaLockoutService(AppDbContext db) => _db = db;

    public async Task<bool> RegisterFailedAttemptAsync(string userName, CancellationToken ct = default)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.UserName == userName, ct);
        if (user == null) return false;
        user.FailedLoginCount++;
        var locked = user.FailedLoginCount >= 5;
        if (locked)
            user.LockoutEnd = VietnamTime.Now.AddMinutes(15);
        _db.LoginAuditLogs.Add(new LoginAuditLog
        {
            UserName = user.UserName,
            UserId = user.UserId,
            IsSuccess = false,
            Outcome = locked ? "FAILED_LOCKOUT_POLICY" : "FAILED_ATTEMPT_POLICY",
            Reason = $"failed={user.FailedLoginCount}",
            CreatedAt = VietnamTime.Now
        });
        await _db.SaveChangesAsync(ct);
        return locked;
    }

    public async Task ResetPasswordByAdminAsync(int userId, string newPassword, string actor, CancellationToken ct = default)
    {
        if (!SecurityHelpers.IsStrongPassword(newPassword))
            throw new BusinessRuleException("Mật khẩu mới chưa đạt chuẩn bảo mật.", "WEAK_PASSWORD", nameof(AppUser));

        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        _db.LoginAuditLogs.Add(new LoginAuditLog
        {
            UserName = user.UserName,
            UserId = user.UserId,
            IsSuccess = true,
            Outcome = "PASSWORD_RESET_BY_ADMIN",
            Reason = $"actor={actor}",
            CreatedAt = VietnamTime.Now
        });
        await _db.SaveChangesAsync(ct);
    }
}

public interface ISegregationOfDutiesService
{
    Task EnforceAsync(string? makerUserName, string verifierUserName, string verifierPermission, bool ruleEnabled = true, CancellationToken ct = default);
}

public sealed class SegregationOfDutiesService : ISegregationOfDutiesService
{
    private readonly AppDbContext _db;

    public SegregationOfDutiesService(AppDbContext db) => _db = db;

    public async Task EnforceAsync(string? makerUserName, string verifierUserName, string verifierPermission, bool ruleEnabled = true, CancellationToken ct = default)
    {
        if (!ruleEnabled || string.IsNullOrWhiteSpace(makerUserName))
            return;
        if (!string.Equals(makerUserName.Trim(), verifierUserName.Trim(), StringComparison.OrdinalIgnoreCase))
            return;

        var label = WmsPermissions.SodMatrix.FirstOrDefault(s => s.VerifierPermission == verifierPermission).VerifierLabel
            ?? verifierPermission;
        _db.AuditLogs.Add(new AuditLog
        {
            TableName = "Security",
            RecordId = verifierUserName,
            ActionType = "SOD_BLOCK",
            NewValue = $"permission={verifierPermission}; label={label}",
            ChangedBy = verifierUserName,
            ChangedAt = VietnamTime.Now,
            AppModule = "SegregationOfDuties"
        });
        await _db.SaveChangesAsync(ct);
        throw WmsExceptions.SodViolation(verifierUserName, label);
    }
}

public sealed class ScopedOperationRequest
{
    public ClaimsPrincipal User { get; init; } = new();
    public int? WarehouseId { get; init; }
    public int? OwnerPartnerId { get; init; }
    public string OperationName { get; init; } = "";
    public bool IsExport { get; init; }
}

public interface ISecurityScopeAuditService
{
    Task EnsureScopeAsync(ScopedOperationRequest request, CancellationToken ct = default);
}

public sealed class SecurityScopeAuditService : ISecurityScopeAuditService
{
    private readonly ITenantScopeService _tenantScopeService;
    private readonly AppDbContext _db;

    public SecurityScopeAuditService(ITenantScopeService tenantScopeService, AppDbContext db)
    {
        _tenantScopeService = tenantScopeService;
        _db = db;
    }

    public async Task EnsureScopeAsync(ScopedOperationRequest request, CancellationToken ct = default)
    {
        var user = request.User;
        var actor = user.Identity?.Name ?? "system";
        if (!user.IsInRole("Admin"))
        {
            var whClaim = user.FindFirst("WarehouseId")?.Value;
            if (request.WarehouseId.HasValue && int.TryParse(whClaim, out var allowedWh) && allowedWh != request.WarehouseId.Value)
            {
                await RecordAsync("DENIED", request.OperationName, actor, $"warehouse={request.WarehouseId}; allowed={allowedWh}", ct);
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác kho ngoài phạm vi.");
            }
        }

        try
        {
            await _tenantScopeService.EnsureCanAccessOwnerAsync(request.OwnerPartnerId, user, ct);
        }
        catch (UnauthorizedAccessException)
        {
            await RecordAsync("DENIED", request.OperationName, actor, $"owner={request.OwnerPartnerId}", ct);
            throw;
        }

        if (request.IsExport)
            await RecordAsync("EXPORT", request.OperationName, actor, $"warehouse={request.WarehouseId}; owner={request.OwnerPartnerId}", ct);
    }

    private async Task RecordAsync(string action, string operationName, string actor, string value, CancellationToken ct)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            TableName = "Security",
            RecordId = string.IsNullOrWhiteSpace(operationName) ? "operation" : operationName.Trim(),
            ActionType = action,
            NewValue = value,
            ChangedBy = actor,
            ChangedAt = VietnamTime.Now,
            AppModule = "ScopeAudit"
        });
        await _db.SaveChangesAsync(ct);
    }
}

public sealed class SecurityEventRow
{
    public string EventType { get; init; } = "";
    public string Severity { get; init; } = "info";
    public string Actor { get; init; } = "";
    public string Summary { get; init; } = "";
    public DateTime OccurredAt { get; init; }
}

public interface ISecurityEventCenterService
{
    Task<IReadOnlyList<SecurityEventRow>> GetRecentEventsAsync(DateTime from, DateTime to, CancellationToken ct = default);
}

public sealed class SecurityEventCenterService : ISecurityEventCenterService
{
    private readonly AppDbContext _db;

    public SecurityEventCenterService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<SecurityEventRow>> GetRecentEventsAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var loginEvents = await _db.LoginAuditLogs.AsNoTracking()
            .Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
            .Select(x => new SecurityEventRow
            {
                EventType = x.Outcome,
                Severity = x.IsSuccess ? "info" : "warning",
                Actor = x.UserName ?? "",
                Summary = x.Reason ?? "",
                OccurredAt = x.CreatedAt
            })
            .ToListAsync(ct);

        var auditEvents = await _db.AuditLogs.AsNoTracking()
            .Where(x => x.ChangedAt >= from && x.ChangedAt <= to
                && x.TableName == "Security"
                && (x.ActionType == "DENIED" || x.ActionType == "EXPORT" || x.ActionType == "SOD_BLOCK"))
            .Select(x => new SecurityEventRow
            {
                EventType = x.ActionType,
                Severity = x.ActionType == "EXPORT" ? "info" : "critical",
                Actor = x.ChangedBy ?? "",
                Summary = x.NewValue ?? "",
                OccurredAt = x.ChangedAt
            })
            .ToListAsync(ct);

        return loginEvents.Concat(auditEvents)
            .OrderByDescending(e => e.OccurredAt)
            .ToList();
    }
}

public sealed class SecretReadinessResult
{
    public bool IsReady { get; init; }
    public IReadOnlyList<string> Findings { get; init; } = Array.Empty<string>();
}

public interface ISecretReadinessService
{
    string? ResolveSecret(string name);
    Task<SecretReadinessResult> ScanRepositoryAsync(string rootPath, CancellationToken ct = default);
}

public sealed class SecretReadinessService : ISecretReadinessService
{
    private static readonly Regex SecretPattern = new(
        @"(?i)(api[_-]?key|secret|password|client[_-]?secret)\s*[:=]\s*[""']?[a-z0-9_\-]{16,}",
        RegexOptions.Compiled);

    public string? ResolveSecret(string name) => Environment.GetEnvironmentVariable(name);

    public async Task<SecretReadinessResult> ScanRepositoryAsync(string rootPath, CancellationToken ct = default)
    {
        var findings = new List<string>();
        if (!Directory.Exists(rootPath))
            return new SecretReadinessResult { IsReady = false, Findings = new[] { "Không tìm thấy thư mục source." } };

        foreach (var file in Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories))
        {
            if (ShouldSkip(file)) continue;
            var text = await File.ReadAllTextAsync(file, ct);
            if (SecretPattern.IsMatch(text))
                findings.Add(Path.GetRelativePath(rootPath, file));
        }

        return new SecretReadinessResult { IsReady = findings.Count == 0, Findings = findings };
    }

    private static bool ShouldSkip(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/logs/", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith("appsettings.json", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("appsettings.", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
    }
}
