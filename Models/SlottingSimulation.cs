using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("SlottingSimulationScenarios")]
public class SlottingSimulationScenario
{
    [Key]
    public int ScenarioId { get; set; }

    [Required, MaxLength(40)]
    public string ScenarioCode { get; set; } = "";

    [Required, MaxLength(100)]
    public string ScenarioName { get; set; } = "";

    public int WarehouseId { get; set; }

    public SlottingSimulationStatusEnum Status { get; set; } = SlottingSimulationStatusEnum.Draft;

    public int LineCount { get; set; }

    public int ApprovedTaskCount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalEstimatedTravelMinutesSaved { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalMovementCostMinutes { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal NetEstimatedMinutesSaved { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ResultJson { get; set; }

    [Required, MaxLength(100)]
    public string CreatedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [MaxLength(100)]
    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    public ICollection<SlottingSimulationLine> Lines { get; set; } = new List<SlottingSimulationLine>();
}

[Table("SlottingSimulationLines")]
public class SlottingSimulationLine
{
    [Key]
    public int ScenarioLineId { get; set; }

    public int ScenarioId { get; set; }

    public int ItemId { get; set; }

    public int? CurrentDefaultLocationId { get; set; }

    public int SourceLocationId { get; set; }

    public int SuggestedLocationId { get; set; }

    public int? SourceItemLocationId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal PlannedMoveQty { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal DailyPickFrequency { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal BeforeTravelDistance { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AfterTravelDistance { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal EstimatedTravelMinutesSaved { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal MovementCostMinutes { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal NetEstimatedMinutesSaved { get; set; }

    public int SlottingScore { get; set; }

    [MaxLength(3)]
    public string AbcClass { get; set; } = "";

    [MaxLength(500)]
    public string Reason { get; set; } = "";

    public SlottingSimulationLineStatusEnum Status { get; set; } = SlottingSimulationLineStatusEnum.Draft;

    public long? MovementTaskId { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    [ForeignKey(nameof(ScenarioId))]
    public SlottingSimulationScenario? Scenario { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }

    [ForeignKey(nameof(CurrentDefaultLocationId))]
    public Location? CurrentDefaultLocation { get; set; }

    [ForeignKey(nameof(SourceLocationId))]
    public Location? SourceLocation { get; set; }

    [ForeignKey(nameof(SuggestedLocationId))]
    public Location? SuggestedLocation { get; set; }

    [ForeignKey(nameof(SourceItemLocationId))]
    public ItemLocation? SourceItemLocation { get; set; }

    [ForeignKey(nameof(MovementTaskId))]
    public MovementTask? MovementTask { get; set; }
}

public enum SlottingSimulationStatusEnum : byte
{
    Draft = 1,
    Approved = 2,
    PartiallyApproved = 3,
    Failed = 4,
    Cancelled = 5
}

public enum SlottingSimulationLineStatusEnum : byte
{
    Draft = 1,
    TaskCreated = 2,
    Failed = 3,
    Skipped = 4
}
