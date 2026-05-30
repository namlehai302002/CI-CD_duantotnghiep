using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("PickTasks")]
public class PickTask : IOwnerScoped
{
    [Key]
    public long PickTaskId { get; set; }

    [Required, MaxLength(40)]
    public string TaskCode { get; set; } = "";

    public long? WaveId { get; set; }

    public long VoucherId { get; set; }

    public long? VoucherDetailId { get; set; }

    public int ItemId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public int SourceLocationId { get; set; }

    /// <summary>Staging location nơi đặt hàng sau khi pick (null = dock/packing area)</summary>
    public int? TargetLocationId { get; set; }

    [MaxLength(50)]
    public string? LotNumber { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ExpiryDate { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TargetQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal PickedQty { get; set; }

    public PickTaskStatusEnum Status { get; set; } = PickTaskStatusEnum.Pending;

    public PickTaskModeEnum PickTaskMode { get; set; } = PickTaskModeEnum.Single;

    public long? ParentPickTaskId { get; set; }

    public int? SortationStageLocationId { get; set; }

    public int? SortationDestinationLocationId { get; set; }

    public bool IsBatchPick { get; set; }

    [MaxLength(200)]
    public string? BatchGroupKey { get; set; }

    [MaxLength(100)]
    public string? AssignedTo { get; set; }

    public DateTime? AssignedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    /// <summary>Thời điểm picker bắt đầu thực hiện task</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>Deadline for this pick task.</summary>
    public DateTime? DueAt { get; set; }

    [ForeignKey(nameof(WaveId))]
    public Wave? Wave { get; set; }

    [ForeignKey(nameof(VoucherId))]
    public Voucher Voucher { get; set; } = null!;

    [ForeignKey(nameof(VoucherDetailId))]
    public VoucherDetail? VoucherDetail { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item Item { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    [ForeignKey(nameof(SourceLocationId))]
    public Location SourceLocation { get; set; } = null!;

    [ForeignKey(nameof(TargetLocationId))]
    public Location? TargetLocation { get; set; }

    [ForeignKey(nameof(ParentPickTaskId))]
    public PickTask? ParentPickTask { get; set; }

    public ICollection<PickTask> ChildPickTasks { get; set; } = new List<PickTask>();

    [ForeignKey(nameof(SortationStageLocationId))]
    public Location? SortationStageLocation { get; set; }

    [ForeignKey(nameof(SortationDestinationLocationId))]
    public Location? SortationDestinationLocation { get; set; }

    [InverseProperty(nameof(SerialNumber.ConsumedPickTask))]
    public ICollection<SerialNumber>? ConsumedSerials { get; set; }

    public ICollection<PickTaskAllocation> Allocations { get; set; } = new List<PickTaskAllocation>();
}
