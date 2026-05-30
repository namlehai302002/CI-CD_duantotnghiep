using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

public enum OptimizationRunTypeEnum : byte
{
    Slotting = 1,
    WavePlanning = 2,
    Waveless = 3,
    PickPath = 4,
    ToteCluster = 5
}

public enum OptimizationRunStatusEnum : byte
{
    Draft = 1,
    Completed = 2,
    Failed = 3,
    Approved = 4
}

public enum OptimizationLineTypeEnum : byte
{
    SlottingSuggestion = 1,
    WaveCandidate = 2,
    WavelessTask = 3,
    PickPathStop = 4,
    ToteAssignment = 5
}

public enum WavelessQueueStatusEnum : byte
{
    Pending = 1,
    Released = 2,
    Skipped = 3,
    Blocked = 4
}

[Table("OptimizationRuns")]
public class OptimizationRun : IOwnerScoped
{
    [Key]
    public long OptimizationRunId { get; set; }

    [Required, MaxLength(40)]
    public string RunCode { get; set; } = "";

    public int WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public OptimizationRunTypeEnum RunType { get; set; }

    public OptimizationRunStatusEnum Status { get; set; } = OptimizationRunStatusEnum.Draft;

    [MaxLength(120)]
    public string? ScopeKey { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal BeforeDistance { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AfterDistance { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal EstimatedMinutesSaved { get; set; }

    public int CandidateCount { get; set; }

    public int ReleasedTaskCount { get; set; }

    public string InputJson { get; set; } = "{}";

    public string ResultJson { get; set; } = "{}";

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? CompletedAt { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    public ICollection<OptimizationRecommendationLine> Lines { get; set; } = new List<OptimizationRecommendationLine>();
}

[Table("OptimizationRecommendationLines")]
public class OptimizationRecommendationLine : IOwnerScoped
{
    [Key]
    public long OptimizationRecommendationLineId { get; set; }

    public long OptimizationRunId { get; set; }

    public OptimizationLineTypeEnum LineType { get; set; }

    public int WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public int? ItemId { get; set; }

    public long? VoucherId { get; set; }

    public long? WaveId { get; set; }

    public long? PickTaskId { get; set; }

    public int? SourceLocationId { get; set; }

    public int? SuggestedLocationId { get; set; }

    [MaxLength(120)]
    public string? GroupKey { get; set; }

    public int Sequence { get; set; }

    public int Score { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal BeforeDistance { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AfterDistance { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal EstimatedMinutesSaved { get; set; }

    public bool InventoryAvailable { get; set; } = true;

    public bool RequiresToteScan { get; set; }

    public bool IsOwnerSafe { get; set; } = true;

    [MaxLength(500)]
    public string Reason { get; set; } = "";

    [MaxLength(80)]
    public string StatusText { get; set; } = "Ready";

    [ForeignKey(nameof(OptimizationRunId))]
    public OptimizationRun OptimizationRun { get; set; } = null!;

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }

    [ForeignKey(nameof(SourceLocationId))]
    public Location? SourceLocation { get; set; }

    [ForeignKey(nameof(SuggestedLocationId))]
    public Location? SuggestedLocation { get; set; }
}

[Table("WavelessReleaseQueue")]
public class WavelessReleaseQueue : IOwnerScoped
{
    [Key]
    public long WavelessReleaseQueueId { get; set; }

    public int WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public long? VoucherId { get; set; }

    public long? PickTaskId { get; set; }

    [Required, MaxLength(160)]
    public string IdempotencyKey { get; set; } = "";

    public int PriorityScore { get; set; }

    public DateTime? SlaDueAt { get; set; }

    public bool InventoryAvailable { get; set; }

    public WavelessQueueStatusEnum Status { get; set; } = WavelessQueueStatusEnum.Pending;

    [MaxLength(500)]
    public string? BlockReason { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? ReleasedAt { get; set; }

    [MaxLength(100)]
    public string? ReleasedBy { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    [ForeignKey(nameof(PickTaskId))]
    public PickTask? PickTask { get; set; }
}

[Table("PickPathPlans")]
public class PickPathPlan : IOwnerScoped
{
    [Key]
    public long PickPathPlanId { get; set; }

    [Required, MaxLength(40)]
    public string PlanCode { get; set; } = "";

    public int WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal BeforeDistance { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AfterDistance { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal DistanceSaved { get; set; }

    public int StopCount { get; set; }

    [Required]
    public string PickTaskIdsJson { get; set; } = "[]";

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    public ICollection<PickPathPlanStop> Stops { get; set; } = new List<PickPathPlanStop>();
}

[Table("PickPathPlanStops")]
public class PickPathPlanStop
{
    [Key]
    public long PickPathPlanStopId { get; set; }

    public long PickPathPlanId { get; set; }

    public int Sequence { get; set; }

    public long? PickTaskId { get; set; }

    public int? LocationId { get; set; }

    [MaxLength(40)]
    public string? ToteCode { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal DistanceFromPrevious { get; set; }

    [ForeignKey(nameof(PickPathPlanId))]
    public PickPathPlan PickPathPlan { get; set; } = null!;

    [ForeignKey(nameof(PickTaskId))]
    public PickTask? PickTask { get; set; }

    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }
}

[Table("ToteClusterPlans")]
public class ToteClusterPlan : IOwnerScoped
{
    [Key]
    public long ToteClusterPlanId { get; set; }

    [Required, MaxLength(40)]
    public string PlanCode { get; set; } = "";

    public int WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    [MaxLength(80)]
    public string? CustomerKey { get; set; }

    [MaxLength(50)]
    public string? CarrierCode { get; set; }

    [MaxLength(50)]
    public string? RouteCode { get; set; }

    public bool RequiresToteScan { get; set; } = true;

    public int AssignmentCount { get; set; }

    [MaxLength(80)]
    public string StatusText { get; set; } = "Ready";

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    public ICollection<ToteClusterAssignment> Assignments { get; set; } = new List<ToteClusterAssignment>();
}

[Table("ToteClusterAssignments")]
public class ToteClusterAssignment : IOwnerScoped
{
    [Key]
    public long ToteClusterAssignmentId { get; set; }

    public long ToteClusterPlanId { get; set; }

    public long? PickToteId { get; set; }

    [MaxLength(40)]
    public string ToteCode { get; set; } = "";

    public long? VoucherId { get; set; }

    public long? PickTaskId { get; set; }

    public int? OwnerPartnerId { get; set; }

    [MaxLength(80)]
    public string? CustomerKey { get; set; }

    public bool IsScanned { get; set; }

    public DateTime? ScannedAt { get; set; }

    [ForeignKey(nameof(ToteClusterPlanId))]
    public ToteClusterPlan ToteClusterPlan { get; set; } = null!;

    [ForeignKey(nameof(PickToteId))]
    public PickTote? PickTote { get; set; }

    [ForeignKey(nameof(PickTaskId))]
    public PickTask? PickTask { get; set; }

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }
}

public enum AutomationTelemetryTypeEnum : byte
{
    Heartbeat = 1,
    Throughput = 2,
    Downtime = 3,
    Error = 4
}

public enum WcsSimulatorScenarioEnum : byte
{
    AcceptAndComplete = 1,
    SorterReject = 2,
    RobotFail = 3,
    Timeout = 4
}

public enum AutomationOverrideActionEnum : byte
{
    Retry = 1,
    Cancel = 2,
    Complete = 3,
    DeadLetter = 4
}

[Table("MheAdapterProfiles")]
public class MheAdapterProfile
{
    [Key]
    public int MheAdapterProfileId { get; set; }

    public int WarehouseId { get; set; }

    public int? MheSystemId { get; set; }

    [Required, MaxLength(40)]
    public string AdapterCode { get; set; } = "";

    [Required, MaxLength(120)]
    public string AdapterName { get; set; } = "";

    public MheSystemTypeEnum AdapterType { get; set; } = MheSystemTypeEnum.Wcs;

    public bool IsSimulator { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    public string CapabilitiesJson { get; set; } = "{}";

    [MaxLength(40)]
    public string HealthStatus { get; set; } = "Unknown";

    public DateTime? LastHeartbeatAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(MheSystemId))]
    public MheSystem? MheSystem { get; set; }
}

[Table("MheTelemetryEvents")]
public class MheTelemetryEvent
{
    [Key]
    public long MheTelemetryEventId { get; set; }

    public int WarehouseId { get; set; }

    public int? MheSystemId { get; set; }

    public int? MheAdapterProfileId { get; set; }

    public AutomationTelemetryTypeEnum TelemetryType { get; set; }

    [MaxLength(80)]
    public string EquipmentCode { get; set; } = "";

    [MaxLength(40)]
    public string StatusText { get; set; } = "OK";

    public int ThroughputPerHour { get; set; }

    public int DowntimeMinutes { get; set; }

    [MaxLength(80)]
    public string? ErrorCode { get; set; }

    [MaxLength(500)]
    public string? Message { get; set; }

    public DateTime EventAt { get; set; } = VietnamTime.Now;

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(MheSystemId))]
    public MheSystem? MheSystem { get; set; }

    [ForeignKey(nameof(MheAdapterProfileId))]
    public MheAdapterProfile? AdapterProfile { get; set; }
}

[Table("WcsSimulatorRuns")]
public class WcsSimulatorRun
{
    [Key]
    public long WcsSimulatorRunId { get; set; }

    public int WarehouseId { get; set; }

    public int? MheSystemId { get; set; }

    public WcsSimulatorScenarioEnum Scenario { get; set; }

    [MaxLength(40)]
    public string StatusText { get; set; } = "Completed";

    public int CommandsCreated { get; set; }

    public int CallbacksSent { get; set; }

    public int ExceptionsOpened { get; set; }

    public string ResultJson { get; set; } = "{}";

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;
}

[Table("AutomationOverrides")]
public class AutomationOverride
{
    [Key]
    public long AutomationOverrideId { get; set; }

    public long MheCommandId { get; set; }

    public AutomationOverrideActionEnum Action { get; set; }

    [Required, MaxLength(500)]
    public string Reason { get; set; } = "";

    [MaxLength(100)]
    public string ApprovedBy { get; set; } = "system";

    public DateTime ApprovedAt { get; set; } = VietnamTime.Now;

    [ForeignKey(nameof(MheCommandId))]
    public MheCommand MheCommand { get; set; } = null!;
}

public enum EdiMessageTypeEnum : byte
{
    Asn = 1,
    Order940 = 2,
    ShipAdvice945 = 3,
    Shipment856 = 4
}

public enum EdiDirectionEnum : byte
{
    Inbound = 1,
    Outbound = 2
}

public enum EdiMessageStatusEnum : byte
{
    Received = 1,
    Validated = 2,
    Rejected = 3,
    Exported = 4,
    Replayed = 5
}

[Table("EdiMessages")]
public class EdiMessage
{
    [Key]
    public long EdiMessageId { get; set; }

    public EdiMessageTypeEnum MessageType { get; set; }

    public EdiDirectionEnum Direction { get; set; }

    public EdiMessageStatusEnum Status { get; set; } = EdiMessageStatusEnum.Received;

    public int? WarehouseId { get; set; }

    public int? PartnerId { get; set; }

    [Required, MaxLength(120)]
    public string ControlNumber { get; set; } = "";

    [MaxLength(260)]
    public string? SourceFileName { get; set; }

    public string Payload { get; set; } = "";

    public string ValidationErrorsJson { get; set; } = "[]";

    public string RejectReport { get; set; } = "";

    public long? ReplayOfMessageId { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? ReplayedAt { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey(nameof(PartnerId))]
    public Partner? Partner { get; set; }
}

public enum WebhookDeliveryStatusEnum : byte
{
    Pending = 1,
    Sent = 2,
    Failed = 3,
    DeadLetter = 4
}

[Table("WebhookSubscriptions")]
public class WebhookSubscription
{
    [Key]
    public long WebhookSubscriptionId { get; set; }

    [Required, MaxLength(80)]
    public string SubscriptionCode { get; set; } = "";

    [Required, MaxLength(80)]
    public string EventType { get; set; } = "";

    [Required, MaxLength(500)]
    public string TargetUrl { get; set; } = "";

    [Required, MaxLength(200)]
    public string SigningSecret { get; set; } = "";

    public bool IsActive { get; set; } = true;

    public int MaxRetries { get; set; } = 3;

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public ICollection<WebhookDelivery> Deliveries { get; set; } = new List<WebhookDelivery>();
}

[Table("WebhookDeliveries")]
public class WebhookDelivery
{
    [Key]
    public long WebhookDeliveryId { get; set; }

    public long WebhookSubscriptionId { get; set; }

    [Required, MaxLength(80)]
    public string EventType { get; set; } = "";

    [Required, MaxLength(160)]
    public string IdempotencyKey { get; set; } = "";

    public string PayloadJson { get; set; } = "{}";

    [MaxLength(128)]
    public string Signature { get; set; } = "";

    public WebhookDeliveryStatusEnum Status { get; set; } = WebhookDeliveryStatusEnum.Pending;

    public int RetryCount { get; set; }

    [MaxLength(500)]
    public string? LastError { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? SentAt { get; set; }

    public DateTime? NextRetryAt { get; set; }

    [ForeignKey(nameof(WebhookSubscriptionId))]
    public WebhookSubscription Subscription { get; set; } = null!;
}

public enum EnterpriseConnectorTypeEnum : byte
{
    Erp = 1,
    Tms = 2,
    Oms = 3
}

public enum EnterpriseConnectorHealthEnum : byte
{
    Unknown = 1,
    Healthy = 2,
    Warning = 3,
    Down = 4
}

[Table("EnterpriseConnectors")]
public class EnterpriseConnector
{
    [Key]
    public int EnterpriseConnectorId { get; set; }

    public EnterpriseConnectorTypeEnum ConnectorType { get; set; }

    [Required, MaxLength(40)]
    public string ConnectorCode { get; set; } = "";

    [Required, MaxLength(120)]
    public string ConnectorName { get; set; } = "";

    [MaxLength(500)]
    public string? EndpointUrl { get; set; }

    [MaxLength(200)]
    public string? SecretReference { get; set; }

    public bool IsMock { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public EnterpriseConnectorHealthEnum HealthStatus { get; set; } = EnterpriseConnectorHealthEnum.Unknown;

    public int RetryCount { get; set; }

    public DateTime? LastHealthCheckAt { get; set; }

    [MaxLength(500)]
    public string? LastError { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;
}

[Table("EnterpriseConnectorDeliveries")]
public class EnterpriseConnectorDelivery
{
    [Key]
    public long EnterpriseConnectorDeliveryId { get; set; }

    public int EnterpriseConnectorId { get; set; }

    [Required, MaxLength(80)]
    public string EventType { get; set; } = "";

    [Required, MaxLength(160)]
    public string IdempotencyKey { get; set; } = "";

    public string PayloadJson { get; set; } = "{}";

    public OutboxStatusEnum Status { get; set; } = OutboxStatusEnum.Pending;

    public int RetryCount { get; set; }

    [MaxLength(500)]
    public string? LastError { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? ProcessedAt { get; set; }

    [ForeignKey(nameof(EnterpriseConnectorId))]
    public EnterpriseConnector Connector { get; set; } = null!;
}
