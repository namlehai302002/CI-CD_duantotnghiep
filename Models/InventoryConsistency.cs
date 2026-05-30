using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("InventorySnapshotOutbox")]
public class InventorySnapshotOutbox
{
    [Key]
    public long InventorySnapshotOutboxId { get; set; }

    public InventorySnapshotEventTypeEnum EventType { get; set; }

    public long? LicensePlateId { get; set; }

    public int WarehouseId { get; set; }

    public int? SourceLocationId { get; set; }

    public int? DestinationLocationId { get; set; }

    [Required, MaxLength(120)]
    public string IdempotencyKey { get; set; } = "";

    [Required]
    public string PayloadJson { get; set; } = "{}";

    public InventorySnapshotOutboxStatusEnum Status { get; set; } = InventorySnapshotOutboxStatusEnum.Pending;

    public int RetryCount { get; set; }

    public DateTime? NextAttemptAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    [MaxLength(1000)]
    public string? LastError { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(LicensePlateId))]
    public LicensePlate? LicensePlate { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey(nameof(SourceLocationId))]
    public Location? SourceLocation { get; set; }

    [ForeignKey(nameof(DestinationLocationId))]
    public Location? DestinationLocation { get; set; }
}

public enum InventorySnapshotEventTypeEnum : byte
{
    LpnMoved = 1,
    LpnReceived = 2,
    LpnVoided = 3,
    LpnDetailChanged = 4,
    SnapshotRebuildRequested = 5
}

public enum InventorySnapshotOutboxStatusEnum : byte
{
    Pending = 1,
    Processing = 2,
    Processed = 3,
    Failed = 4
}

[Table("InventoryReconciliationRuns")]
public class InventoryReconciliationRun
{
    [Key]
    public long InventoryReconciliationRunId { get; set; }

    public int? WarehouseId { get; set; }

    public DateTime StartedAt { get; set; } = VietnamTime.Now;

    public DateTime? CompletedAt { get; set; }

    public int ExpectedRowCount { get; set; }

    public int SnapshotRowCount { get; set; }

    public int IssueCount { get; set; }

    public int AutoHealedCount { get; set; }

    public int AlertCount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ToleranceQty { get; set; }

    public InventoryReconciliationRunStatusEnum Status { get; set; } = InventoryReconciliationRunStatusEnum.Running;

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    public ICollection<InventoryReconciliationIssue> Issues { get; set; } = new List<InventoryReconciliationIssue>();
}

public enum InventoryReconciliationRunStatusEnum : byte
{
    Running = 1,
    Completed = 2,
    Failed = 3
}

[Table("InventoryReconciliationIssues")]
public class InventoryReconciliationIssue
{
    [Key]
    public long InventoryReconciliationIssueId { get; set; }

    public long InventoryReconciliationRunId { get; set; }

    public int? WarehouseId { get; set; }

    public int ItemId { get; set; }

    public int LocationId { get; set; }

    [MaxLength(50)]
    public string? LotNumber { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ExpiryDate { get; set; }

    public InventoryHoldStatusEnum HoldStatus { get; set; } = InventoryHoldStatusEnum.Available;

    [Column(TypeName = "decimal(18,4)")]
    public decimal ExpectedQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal SnapshotQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal DeltaQty { get; set; }

    public InventoryReconciliationActionEnum Action { get; set; }

    public InventoryReconciliationSeverityEnum Severity { get; set; }

    [MaxLength(1000)]
    public string? Message { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public bool IsResolved { get; set; }

    public DateTime? ResolvedAt { get; set; }

    [MaxLength(100)]
    public string? ResolvedBy { get; set; }

    [ForeignKey(nameof(InventoryReconciliationRunId))]
    public InventoryReconciliationRun? Run { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }

    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }
}

public enum InventoryReconciliationActionEnum : byte
{
    None = 0,
    AutoHealed = 1,
    Alert = 2
}

public enum InventoryReconciliationSeverityEnum : byte
{
    Info = 1,
    Warning = 2,
    Critical = 3
}
