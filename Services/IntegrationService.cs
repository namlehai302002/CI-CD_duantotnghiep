using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WMS.Data;
using WMS.Models;
using WMS.Common;

namespace WMS.Services;

/// <summary>
/// P1.2 — Integration Reliability Service
/// Cung cấp outbox pattern và idempotency check cho tất cả integration operations.
/// </summary>
public interface IIntegrationService
{
    /// <summary>Enqueue một event vào outbox (bất đồng bộ, retry tự động)</summary>
    Task EnqueueAsync(OutboxEventTypeEnum eventType, string targetEndpoint, object payload,
        string? idempotencyKey = null, string? targetSystem = null);

    /// <summary>Check idempotency key — trả về cached response nếu đã xử lý</summary>
    Task<(bool IsDuplicate, string? CachedResponse, int StatusCode)> CheckIdempotencyAsync(
        string keyValue, string operationType);

    /// <summary>Mark idempotency key sau khi xử lý thành công</summary>
    Task SetIdempotencyAsync(string keyValue, string operationType, string response, int statusCode);

    /// <summary>Background job: đọc pending outbox, gửi HTTP, retry nếu fail → dead-letter</summary>
    Task ProcessOutboxBatchAsync(CancellationToken ct = default);
}

public class IntegrationService : IIntegrationService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<IntegrationService> _logger;
    private const int MaxRetries = 5;
    private const int DeadLetterAfterRetries = 3; // sau 3 lần fail → dead-letter để ops xử lý

    public IntegrationService(AppDbContext db, IHttpClientFactory httpClientFactory, ILogger<IntegrationService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task EnqueueAsync(OutboxEventTypeEnum eventType, string targetEndpoint, object payload,
        string? idempotencyKey = null, string? targetSystem = null)
    {
        var actor = "system";
        var correlationId = Guid.NewGuid().ToString("N");
        var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        _db.IntegrationOutbox.Add(new IntegrationOutbox
        {
            EventType = eventType.ToString(),
            TargetEndpoint = targetEndpoint,
            Payload = payloadJson,
            HttpMethod = "POST",
            Status = OutboxStatusEnum.Pending,
            RetryCount = 0,
            IdempotencyKey = idempotencyKey,
            TargetSystem = targetSystem,
            CorrelationId = correlationId,
            CreatedBy = actor,
            CreatedAt = VietnamTime.Now
        });

        await _db.SaveChangesAsync();
        _logger.LogInformation("[Outbox] Enqueued {EventType} to {Endpoint} (correlationId={CorrelationId})",
            eventType, targetEndpoint, correlationId);
    }

    public async Task<(bool IsDuplicate, string? CachedResponse, int StatusCode)> CheckIdempotencyAsync(
        string keyValue, string operationType)
    {
        var key = await _db.IntegrationIdempotencyKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.KeyValue == keyValue && k.OperationType == operationType
                && k.ExpiresAt > VietnamTime.Now);

        if (key != null)
        {
            _logger.LogInformation("[Idempotency] Duplicate key detected: {Key} ({Operation})",
                keyValue, operationType);
            return (true, key.CachedResponse, key.ResponseStatusCode);
        }

        return (false, null, 0);
    }

    public async Task SetIdempotencyAsync(string keyValue, string operationType, string response, int statusCode)
    {
        // Xóa key cũ nếu có
        var existing = await _db.IntegrationIdempotencyKeys
            .FirstOrDefaultAsync(k => k.KeyValue == keyValue && k.OperationType == operationType);
        if (existing != null)
        {
            existing.CachedResponse = response;
            existing.ResponseStatusCode = statusCode;
            existing.CreatedAt = VietnamTime.Now;
            existing.ExpiresAt = VietnamTime.Now.AddHours(24);
        }
        else
        {
            _db.IntegrationIdempotencyKeys.Add(new IntegrationIdempotencyKey
            {
                KeyValue = keyValue,
                OperationType = operationType,
                CachedResponse = response,
                ResponseStatusCode = statusCode,
                CreatedAt = VietnamTime.Now,
                ExpiresAt = VietnamTime.Now.AddHours(24)
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task ProcessOutboxBatchAsync(CancellationToken ct = default)
    {
        // P2-R2-4: sweep các item kẹt ở Processing quá 30 phút (app crash giữa SaveChanges Processing và HTTP send)
        // → reset về Pending để retry, tránh orphan vĩnh viễn. Dùng CreatedAt vì IntegrationOutbox không có UpdatedAt.
        var staleCutoff = VietnamTime.Now.AddMinutes(-30);
        var stale = await _db.IntegrationOutbox
            .Where(o => o.Status == OutboxStatusEnum.Processing && o.CreatedAt < staleCutoff)
            .OrderBy(o => o.CreatedAt)
            .Take(50)
            .ToListAsync(ct);
        foreach (var s in stale)
        {
            s.Status = OutboxStatusEnum.Pending;
            s.LastError = "Reset từ Processing kẹt > 30 phút (worker crash giữa chừng).";
        }
        if (stale.Count > 0)
            await _db.SaveChangesAsync(ct);

        var batch = await _db.IntegrationOutbox
            .Where(o => o.Status == OutboxStatusEnum.Pending || o.Status == OutboxStatusEnum.Failed)
            .OrderBy(o => o.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        foreach (var item in batch)
        {
            if (ct.IsCancellationRequested) break;

            item.Status = OutboxStatusEnum.Processing;
            await _db.SaveChangesAsync(ct);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, item.TargetEndpoint);
                request.Content = new StringContent(item.Payload, System.Text.Encoding.UTF8, "application/json");
                request.Headers.Add("X-Correlation-Id", item.CorrelationId ?? "");
                if (!string.IsNullOrEmpty(item.IdempotencyKey))
                    request.Headers.Add("X-Idempotency-Key", item.IdempotencyKey);

                using var client = _httpClientFactory.CreateClient("Integration");
                using var response = await client.SendAsync(request, ct);

                if (response.IsSuccessStatusCode)
                {
                    item.Status = OutboxStatusEnum.Sent;
                    item.ProcessedAt = VietnamTime.Now;
                    _logger.LogInformation("[Outbox] Sent {EventType} → {Endpoint} [{CorrelationId}]",
                        item.EventType, item.TargetEndpoint, item.CorrelationId);
                }
                else
                {
                    item.RetryCount++;
                    item.LastError = $"HTTP {response.StatusCode}: {await response.Content.ReadAsStringAsync(ct)}";
                    item.Status = item.RetryCount >= DeadLetterAfterRetries
                        ? OutboxStatusEnum.DeadLetter
                        : OutboxStatusEnum.Pending;
                    _logger.LogWarning("[Outbox] Failed {EventType} → {Endpoint} [{RetryCount}]: {Error}",
                        item.EventType, item.TargetEndpoint, item.RetryCount, item.LastError);
                }
            }
            catch (Exception ex)
            {
                item.RetryCount++;
                item.LastError = UserSafeError.From(ex, "Không thể gửi sự kiện tích hợp lúc này. Hệ thống sẽ tự thử lại.");
                item.Status = item.RetryCount >= DeadLetterAfterRetries
                    ? OutboxStatusEnum.DeadLetter
                    : OutboxStatusEnum.Pending;
                _logger.LogError(ex, "[Outbox] Exception sending {EventType}", item.EventType);
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
