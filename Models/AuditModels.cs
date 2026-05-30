using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

// ─── P1.2: Integration Reliability Layer ───────────────────────────────────────

/// <summary>Outbox pattern cho integration — đảm bảo mọi event được gửi đúng 1 lần (at-least-once delivery)</summary>
[Table("IntegrationOutbox")]
public class IntegrationOutbox
{
    [Key]
    public long OutboxId { get; set; }

    /// <summary>Mã event: ShipmentPosted, AsnReceived, ExceptionRaised, StockAlert, RecallIssued</summary>
    [Required, MaxLength(50)]
    public string EventType { get; set; } = "";

    /// <summary>Endpoint đích (ERP/TMS/WCS)</summary>
    [Required, MaxLength(200)]
    public string TargetEndpoint { get; set; } = "";

    /// <summary>Payload JSON gửi đi</summary>
    public string Payload { get; set; } = "";

    /// <summary>HTTP method: POST, PUT, PATCH</summary>
    [MaxLength(10)]
    public string HttpMethod { get; set; } = "POST";

    public OutboxStatusEnum Status { get; set; } = OutboxStatusEnum.Pending;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public int RetryCount { get; set; }

    [MaxLength(500)]
    public string? LastError { get; set; }

    public DateTime? ProcessedAt { get; set; }

    /// <summary>Idempotency key liên kết — để replay</summary>
    [MaxLength(100)]
    public string? IdempotencyKey { get; set; }

    [MaxLength(100)]
    public string? TargetSystem { get; set; } // ERP, TMS, WCS

    [MaxLength(45)]
    public string? CorrelationId { get; set; }

    [MaxLength(200)]
    public string CreatedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;
}

/// <summary>Idempotency key store — ngăn duplicate processing cho integration writes</summary>
[Table("IntegrationIdempotencyKeys")]
public class IntegrationIdempotencyKey
{
    [Key]
    public long KeyId { get; set; }

    [Required, MaxLength(100)]
    public string KeyValue { get; set; } = "";

    [Required, MaxLength(50)]
    public string OperationType { get; set; } = "";

    /// <summary>Response/result đã được cache</summary>
    public string? CachedResponse { get; set; }

    public int ResponseStatusCode { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Loại event outbound ra khỏi WMS
/// </summary>
public enum OutboxEventTypeEnum : byte
{
    ShipmentPosted = 1,
    AsnReceived = 2,
    AsnStatusChanged = 3,
    VoucherCompleted = 4,
    StockAlert = 5,
    ExceptionRaised = 6,
    RecallIssued = 7,
    WaveCompleted = 8,
    MheCommandDispatched = 9,
    CarrierShipmentRequested = 10,
    CarrierShipmentCancelled = 11,
    CarrierShipmentStatusRequested = 12,
    InventoryChanged = 13,
    ShipmentConfirmed = 14,
    ThreePlInvoiceIssued = 15,
    WebhookDelivery = 16,
    EdiMessageProcessed = 17
}

/// <summary>
/// Trạng thái xử lý outbox
/// </summary>
public enum OutboxStatusEnum : byte
{
    Pending = 1,
    Processing = 2,
    Sent = 3,
    Failed = 4,
    DeadLetter = 5
}

[Table("AuditLogs")]
public class AuditLog
{
    [Key]
    public long AuditLogId { get; set; }

    [Required, MaxLength(128)]
    public string TableName { get; set; } = "";

    [Required, MaxLength(50)]
    public string RecordId { get; set; } = "";

    [Required, MaxLength(64)]
    public string ActionType { get; set; } = "";

    [MaxLength(128)]
    public string? ColumnChanged { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    [MaxLength(100)]
    public string? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; } = VietnamTime.Now;

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(100)]
    public string? AppModule { get; set; }

    [MaxLength(100)]
    public string? SessionId { get; set; }
}

[Table("AiOcrLogs")]
public class AiOcrLog
{
    [Key]
    public long AiOcrLogId { get; set; }

    [Required, MaxLength(1000)]
    public string ImageUrl { get; set; } = "";

    [MaxLength(255)]
    public string? FileName { get; set; }

    public long? FileSize { get; set; }

    [MaxLength(50)]
    public string OcrProvider { get; set; } = "Gemini_Vision";

    [MaxLength(50)]
    public string? ModelVersion { get; set; }

    public string? RawJsonResponse { get; set; }

    public string? ParsedData { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal? ConfidenceScore { get; set; }

    public int? DetectedItems { get; set; }

    public int? ProcessingTimeMs { get; set; }

    public byte Status { get; set; } = 1;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    public long? VoucherId { get; set; }

    [Required, MaxLength(100)]
    public string CreatedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [ForeignKey("VoucherId")]
    public Voucher? Voucher { get; set; }
}

[Table("AiOcrAdjustments")]
public class AiOcrAdjustment
{
    [Key]
    public long AdjustmentId { get; set; }

    public long AiOcrLogId { get; set; }

    [Required, MaxLength(100)]
    public string FieldName { get; set; } = "";

    [MaxLength(500)]
    public string? AiOriginalValue { get; set; }

    [MaxLength(500)]
    public string? UserCorrectedValue { get; set; }

    public int? ItemId { get; set; }

    public int? LineNumber { get; set; }

    [MaxLength(300)]
    public string? Reason { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    [Required, MaxLength(100)]
    public string CorrectedBy { get; set; } = "";

    public DateTime CorrectedAt { get; set; } = VietnamTime.Now;

    [ForeignKey("AiOcrLogId")]
    public AiOcrLog? AiOcrLog { get; set; }

    [ForeignKey("ItemId")]
    public Item? Item { get; set; }
}

[Table("StockSnapshots")]
public class StockSnapshot
{
    [Key]
    public long SnapshotId { get; set; }

    [Column(TypeName = "date")]
    public DateTime SnapshotDate { get; set; }

    public int ItemId { get; set; }

    public int WarehouseId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ClosingStock { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitCost { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalValue { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [ForeignKey("WarehouseId")]
    public Warehouse? Warehouse { get; set; }
}

[Table("StockAlerts")]
public class StockAlert
{
    [Key]
    public long AlertId { get; set; }

    public int ItemId { get; set; }

    public AlertTypeEnum AlertType { get; set; } = AlertTypeEnum.LowStock;

    [Column(TypeName = "decimal(18,4)")]
    public decimal CurrentStock { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Threshold { get; set; }

    public bool IsRead { get; set; }

    public bool IsResolved { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? ResolvedAt { get; set; }

    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    public string AlertTypeName => AlertType switch
    {
        AlertTypeEnum.LowStock => "Tồn Kho Thấp",
        AlertTypeEnum.OverStock => "Tồn Kho Cao",
        AlertTypeEnum.Expiry => "Sắp Hết Hạn",
        _ => "Khác"
    };
}
