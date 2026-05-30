using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("MovementTasks")]
public class MovementTask : IOwnerScoped
{
    [Key]
    public long MovementTaskId { get; set; }

    [Required, MaxLength(40)]
    public string TaskCode { get; set; } = "";

    public int WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public int ItemId { get; set; }

    public int SourceLocationId { get; set; }

    public int DestinationLocationId { get; set; }

    public int? SourceItemLocationId { get; set; }

    public MovementTaskModeEnum MovementMode { get; set; } = MovementTaskModeEnum.Item;

    public long? LicensePlateId { get; set; }

    [MaxLength(30)]
    public string? LpnCodeSnapshot { get; set; }

    public int LpnDetailCount { get; set; }

    public int LpnDistinctItemCount { get; set; }

    public MovementTaskTypeEnum TaskType { get; set; } = MovementTaskTypeEnum.Relocate;

    public MovementTaskStatusEnum Status { get; set; } = MovementTaskStatusEnum.Pending;

    public MovementTaskPriorityEnum Priority { get; set; } = MovementTaskPriorityEnum.Normal;

    [Column(TypeName = "decimal(18,4)")]
    public decimal PlannedQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ConfirmedQty { get; set; }

    [MaxLength(50)]
    public string? LotNumber { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ExpiryDate { get; set; }

    public int? PreviousDefaultLocationId { get; set; }

    public bool UpdateDefaultLocationOnComplete { get; set; }

    public long? ReplenishmentAutomationRunId { get; set; }

    public long? ReplenishmentAutomationLineId { get; set; }

    public ReplenishmentTriggerTypeEnum? ReplenishmentTriggerType { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal DemandQtySnapshot { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ForecastQtySnapshot { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal OpenReplenishmentQtySnapshot { get; set; }

    public int RoutePriorityScore { get; set; }

    public int TravelSequenceScore { get; set; }

    public int? SourceZoneId { get; set; }

    public int? DestinationZoneId { get; set; }

    public int SourceAisleSequence { get; set; }

    public int DestinationAisleSequence { get; set; }

    [MaxLength(80)]
    public string? AutomationBatchKey { get; set; }

    [MaxLength(100)]
    public string? AssignedTo { get; set; }

    public DateTime? AssignedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    [MaxLength(100)]
    public string? StartedBy { get; set; }

    public DateTime? CompletedAt { get; set; }

    [MaxLength(100)]
    public string? CompletedBy { get; set; }

    public DateTime? CancelledAt { get; set; }

    [MaxLength(100)]
    public string? CancelledBy { get; set; }

    [MaxLength(300)]
    public string? CancelReason { get; set; }

    [Required, MaxLength(100)]
    public string CreatedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DueAt { get; set; }

    [MaxLength(50)]
    public string SourceModule { get; set; } = "";

    [MaxLength(100)]
    public string? SourceReference { get; set; }

    [MaxLength(500)]
    public string? SourceReason { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }

    [ForeignKey(nameof(SourceLocationId))]
    public Location? SourceLocation { get; set; }

    [ForeignKey(nameof(DestinationLocationId))]
    public Location? DestinationLocation { get; set; }

    [ForeignKey(nameof(SourceItemLocationId))]
    public ItemLocation? SourceItemLocation { get; set; }

    [ForeignKey(nameof(LicensePlateId))]
    public LicensePlate? LicensePlate { get; set; }

    [ForeignKey(nameof(PreviousDefaultLocationId))]
    public Location? PreviousDefaultLocation { get; set; }

    [ForeignKey(nameof(ReplenishmentAutomationRunId))]
    public ReplenishmentAutomationRun? ReplenishmentAutomationRun { get; set; }
}

public enum MovementTaskModeEnum : byte
{
    Item = 1,
    Lpn = 2
}

public enum MovementTaskTypeEnum : byte
{
    Relocate = 1,
    Replenishment = 2,
    Reslotting = 3
}

public enum MovementTaskStatusEnum : byte
{
    Pending = 1,
    Assigned = 2,
    InProgress = 3,
    Completed = 4,
    Short = 5,
    Cancelled = 6
}

public enum MovementTaskPriorityEnum : byte
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}
