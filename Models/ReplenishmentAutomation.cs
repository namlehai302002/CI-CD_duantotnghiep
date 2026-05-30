using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("ReplenishmentAutomationRuns")]
public class ReplenishmentAutomationRun
{
    [Key]
    public long ReplenishmentAutomationRunId { get; set; }

    [Required, MaxLength(40)]
    public string RunCode { get; set; } = "";

    public int WarehouseId { get; set; }

    public ReplenishmentRunStatusEnum Status { get; set; } = ReplenishmentRunStatusEnum.Started;

    public bool AutoCreateTasks { get; set; }

    public int DemandHorizonDays { get; set; }

    public int ForecastHistoryDays { get; set; }

    public int ForecastHorizonDays { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal ForecastSafetyFactor { get; set; }

    public int SuggestedLineCount { get; set; }

    public int CreatedTaskCount { get; set; }

    public int SkippedLineCount { get; set; }

    public int FailedLineCount { get; set; }

    public DateTime StartedAt { get; set; } = VietnamTime.Now;

    public DateTime? CompletedAt { get; set; }

    [Required, MaxLength(100)]
    public string TriggeredBy { get; set; } = "";

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    public string ConfigJson { get; set; } = "{}";

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    public ICollection<ReplenishmentAutomationLine> Lines { get; set; } = new List<ReplenishmentAutomationLine>();
}

[Table("ReplenishmentAutomationLines")]
public class ReplenishmentAutomationLine
{
    [Key]
    public long ReplenishmentAutomationLineId { get; set; }

    public long ReplenishmentAutomationRunId { get; set; }

    public int WarehouseId { get; set; }

    public int ItemId { get; set; }

    public int DestinationLocationId { get; set; }

    public int SourceLocationId { get; set; }

    public int? SourceItemLocationId { get; set; }

    public long? MovementTaskId { get; set; }

    public ReplenishmentTriggerTypeEnum TriggerType { get; set; } = ReplenishmentTriggerTypeEnum.Threshold;

    public ReplenishmentAutomationLineStatusEnum Status { get; set; } = ReplenishmentAutomationLineStatusEnum.Planned;

    public MovementTaskPriorityEnum Priority { get; set; } = MovementTaskPriorityEnum.Normal;

    [Column(TypeName = "decimal(18,4)")]
    public decimal PickFaceQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal OpenReplenishmentQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal DemandQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ForecastQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TriggerQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TargetQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal SuggestedQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal SourceAvailableQty { get; set; }

    [MaxLength(50)]
    public string? LotNumber { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ExpiryDate { get; set; }

    public DateTime? DueAt { get; set; }

    public int RoutePriorityScore { get; set; }

    public int TravelSequenceScore { get; set; }

    [MaxLength(700)]
    public string Reason { get; set; } = "";

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(ReplenishmentAutomationRunId))]
    public ReplenishmentAutomationRun? Run { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }

    [ForeignKey(nameof(DestinationLocationId))]
    public Location? DestinationLocation { get; set; }

    [ForeignKey(nameof(SourceLocationId))]
    public Location? SourceLocation { get; set; }

    [ForeignKey(nameof(SourceItemLocationId))]
    public ItemLocation? SourceItemLocation { get; set; }

    [ForeignKey(nameof(MovementTaskId))]
    public MovementTask? MovementTask { get; set; }
}
