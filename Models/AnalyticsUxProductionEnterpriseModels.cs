using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

public enum SemanticMetricCategoryEnum : byte
{
    Inventory = 1,
    Order = 2,
    Labor = 3,
    Billing = 4,
    Sla = 5
}

public enum PredictiveAlertTypeEnum : byte
{
    CapacityOverload = 1,
    StockoutRisk = 2,
    SlaDelay = 3,
    ExpiryRisk = 4
}

public enum EnterpriseSeverityEnum : byte
{
    Info = 1,
    Warning = 2,
    Critical = 3
}

public enum EnterpriseFindingStatusEnum : byte
{
    Open = 1,
    Reviewed = 2,
    Resolved = 3
}

public enum AuditFindingTypeEnum : byte
{
    SensitiveExport = 1,
    OutOfHoursAccess = 2,
    ScopeDenied = 3,
    AbnormalMutation = 4
}

public enum AiAssistantSessionStatusEnum : byte
{
    Open = 1,
    Closed = 2
}

public enum AiAssistantMessageRoleEnum : byte
{
    User = 1,
    Assistant = 2,
    System = 3
}

[Table("SemanticMetricDefinitions")]
public class SemanticMetricDefinition
{
    [Key]
    public int SemanticMetricDefinitionId { get; set; }

    [Required, MaxLength(80)]
    public string MetricCode { get; set; } = "";

    [Required, MaxLength(200)]
    public string MetricName { get; set; } = "";

    public SemanticMetricCategoryEnum Category { get; set; }

    [MaxLength(40)]
    public string Unit { get; set; } = "";

    [MaxLength(1000)]
    public string Formula { get; set; } = "";

    [MaxLength(300)]
    public string SourceLabel { get; set; } = "";

    public bool IsFinancial { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public ICollection<SemanticMetricSnapshot> Snapshots { get; set; } = new List<SemanticMetricSnapshot>();
}

[Table("SemanticMetricSnapshots")]
public class SemanticMetricSnapshot
{
    [Key]
    public long SemanticMetricSnapshotId { get; set; }

    public int SemanticMetricDefinitionId { get; set; }

    public int? WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    [Column(TypeName = "date")]
    public DateTime MetricDate { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal MetricValue { get; set; }

    [MaxLength(80)]
    public string ScopeKey { get; set; } = "";

    [MaxLength(500)]
    public string SourceCitation { get; set; } = "";

    public int SourceCount { get; set; }

    public DateTime CalculatedAt { get; set; } = VietnamTime.Now;

    [ForeignKey(nameof(SemanticMetricDefinitionId))]
    public SemanticMetricDefinition MetricDefinition { get; set; } = null!;

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }
}

[Table("EnterprisePredictiveAlerts")]
public class EnterprisePredictiveAlert : IOwnerScoped
{
    [Key]
    public long EnterprisePredictiveAlertId { get; set; }

    public PredictiveAlertTypeEnum AlertType { get; set; }

    public EnterpriseSeverityEnum Severity { get; set; } = EnterpriseSeverityEnum.Warning;

    public int? WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    [MaxLength(80)]
    public string ReferenceType { get; set; } = "";

    [MaxLength(80)]
    public string ReferenceId { get; set; } = "";

    [Required, MaxLength(200)]
    public string Title { get; set; } = "";

    [MaxLength(1200)]
    public string Message { get; set; } = "";

    [Column(TypeName = "decimal(9,4)")]
    public decimal RiskScore { get; set; }

    public DateTime ForecastFor { get; set; } = VietnamTime.Now;

    public EnterpriseFindingStatusEnum Status { get; set; } = EnterpriseFindingStatusEnum.Open;

    [MaxLength(1000)]
    public string CitationJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? ResolvedAt { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }
}

[Table("AuditAnalyticsFindings")]
public class AuditAnalyticsFinding : IOwnerScoped
{
    [Key]
    public long AuditAnalyticsFindingId { get; set; }

    public AuditFindingTypeEnum FindingType { get; set; }

    public EnterpriseSeverityEnum Severity { get; set; } = EnterpriseSeverityEnum.Warning;

    [MaxLength(120)]
    public string? UserName { get; set; }

    public int? WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    [MaxLength(80)]
    public string ReferenceType { get; set; } = "";

    [MaxLength(100)]
    public string ReferenceId { get; set; } = "";

    [Required, MaxLength(200)]
    public string Title { get; set; } = "";

    [MaxLength(1600)]
    public string EvidenceJson { get; set; } = "{}";

    public EnterpriseFindingStatusEnum Status { get; set; } = EnterpriseFindingStatusEnum.Open;

    public DateTime OccurredAt { get; set; } = VietnamTime.Now;

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(100)]
    public string? ReviewedBy { get; set; }
}

[Table("AiAssistantSessions")]
public class AiAssistantSession : IOwnerScoped
{
    [Key]
    public long AiAssistantSessionId { get; set; }

    [Required, MaxLength(80)]
    public string SessionCode { get; set; } = "";

    [Required, MaxLength(120)]
    public string UserName { get; set; } = "";

    [Required, MaxLength(40)]
    public string RoleName { get; set; } = "";

    public int? WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    [MaxLength(200)]
    public string Purpose { get; set; } = "Operational BI assistant";

    public AiAssistantSessionStatusEnum Status { get; set; } = AiAssistantSessionStatusEnum.Open;

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime LastMessageAt { get; set; } = VietnamTime.Now;

    public ICollection<AiAssistantMessage> Messages { get; set; } = new List<AiAssistantMessage>();
}

[Table("AiAssistantMessages")]
public class AiAssistantMessage
{
    [Key]
    public long AiAssistantMessageId { get; set; }

    public long AiAssistantSessionId { get; set; }

    public AiAssistantMessageRoleEnum MessageRole { get; set; }

    [MaxLength(2000)]
    public string Prompt { get; set; } = "";

    public string Response { get; set; } = "";

    public bool IsMutationBlocked { get; set; }

    [MaxLength(600)]
    public string ScopeSummary { get; set; } = "";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [ForeignKey(nameof(AiAssistantSessionId))]
    public AiAssistantSession Session { get; set; } = null!;

    public ICollection<AiAssistantCitation> Citations { get; set; } = new List<AiAssistantCitation>();
}

[Table("AiAssistantCitations")]
public class AiAssistantCitation
{
    [Key]
    public long AiAssistantCitationId { get; set; }

    public long AiAssistantMessageId { get; set; }

    [Required, MaxLength(80)]
    public string SourceType { get; set; } = "";

    [Required, MaxLength(100)]
    public string SourceId { get; set; } = "";

    [Required, MaxLength(300)]
    public string SourceLabel { get; set; } = "";

    [MaxLength(300)]
    public string? SourceUrl { get; set; }

    [MaxLength(800)]
    public string Excerpt { get; set; } = "";

    [ForeignKey(nameof(AiAssistantMessageId))]
    public AiAssistantMessage Message { get; set; } = null!;
}

[Table("WarehouseWorkflowProfiles")]
public class WarehouseWorkflowProfile : IOwnerScoped
{
    [Key]
    public int WarehouseWorkflowProfileId { get; set; }

    public int WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    [Required, MaxLength(60)]
    public string ModuleKey { get; set; } = "";

    [Required, MaxLength(160)]
    public string ProfileName { get; set; } = "";

    public bool RequireLocationScan { get; set; } = true;

    public bool RequireItemScan { get; set; } = true;

    public bool RequireToteScan { get; set; }

    public bool RequireSerialScan { get; set; }

    public bool RequireQc { get; set; }

    public bool RequireApproval { get; set; }

    public bool RequirePacking { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string UpdatedBy { get; set; } = "";

    public DateTime UpdatedAt { get; set; } = VietnamTime.Now;

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }
}

[Table("RequestTelemetryLogs")]
public class RequestTelemetryLog
{
    [Key]
    public long RequestTelemetryLogId { get; set; }

    [Required, MaxLength(80)]
    public string CorrelationId { get; set; } = "";

    [Required, MaxLength(12)]
    public string Method { get; set; } = "";

    [Required, MaxLength(300)]
    public string Path { get; set; } = "";

    public int StatusCode { get; set; }

    public long DurationMs { get; set; }

    [MaxLength(120)]
    public string? UserName { get; set; }

    public int? WarehouseId { get; set; }

    public bool IsError { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;
}

[Table("SreMetricSnapshots")]
public class SreMetricSnapshot
{
    [Key]
    public long SreMetricSnapshotId { get; set; }

    public DateTime SnapshotAt { get; set; } = VietnamTime.Now;

    public int PeriodMinutes { get; set; } = 15;

    [Column(TypeName = "decimal(18,4)")]
    public decimal AverageLatencyMs { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal P95LatencyMs { get; set; }

    [Column(TypeName = "decimal(9,4)")]
    public decimal ErrorRatePercent { get; set; }

    public int RequestCount { get; set; }

    public int ErrorCount { get; set; }

    public int QueueDepth { get; set; }

    public int DeadLetterCount { get; set; }

    public int ScanRetryCount { get; set; }

    public int CarrierFailureCount { get; set; }

    public int WebhookFailureCount { get; set; }

    [MaxLength(1200)]
    public string Notes { get; set; } = "";
}
