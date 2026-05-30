using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("KittingWorkOrders")]
public class KittingWorkOrder : IOwnerScoped
{
    [Key]
    public long KittingWorkOrderId { get; set; }

    [Required, MaxLength(40)]
    public string WorkOrderCode { get; set; } = "";

    public int WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public int FinishedItemId { get; set; }

    public int FinishedLocationId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal PlannedQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal CompletedQty { get; set; }

    [MaxLength(50)]
    public string? FinishedLotNumber { get; set; }

    [Column(TypeName = "date")]
    public DateTime? FinishedExpiryDate { get; set; }

    public KittingWorkOrderStatusEnum Status { get; set; } = KittingWorkOrderStatusEnum.Draft;

    public DateTime? ReservedAt { get; set; }

    [MaxLength(100)]
    public string? ReservedBy { get; set; }

    public DateTime? CompletedAt { get; set; }

    [MaxLength(100)]
    public string? CompletedBy { get; set; }

    public DateTime? CancelledAt { get; set; }

    [MaxLength(100)]
    public string? CancelledBy { get; set; }

    [MaxLength(300)]
    public string? CancelReason { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required, MaxLength(100)]
    public string CreatedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    [ForeignKey(nameof(FinishedItemId))]
    public Item FinishedItem { get; set; } = null!;

    [ForeignKey(nameof(FinishedLocationId))]
    public Location FinishedLocation { get; set; } = null!;

    public ICollection<KittingWorkOrderLine> Lines { get; set; } = new List<KittingWorkOrderLine>();
}

[Table("KittingWorkOrderLines")]
public class KittingWorkOrderLine : IOwnerScoped
{
    [Key]
    public long KittingWorkOrderLineId { get; set; }

    public long KittingWorkOrderId { get; set; }

    public int ComponentItemId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public int? SourceLocationId { get; set; }

    public int? SourceItemLocationId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal RequiredQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ReservedQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ConsumedQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ReleasedQty { get; set; }

    [MaxLength(50)]
    public string? LotNumber { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ExpiryDate { get; set; }

    public KittingWorkOrderLineStatusEnum Status { get; set; } = KittingWorkOrderLineStatusEnum.Draft;

    [MaxLength(300)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(KittingWorkOrderId))]
    public KittingWorkOrder KittingWorkOrder { get; set; } = null!;

    [ForeignKey(nameof(ComponentItemId))]
    public Item ComponentItem { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    [ForeignKey(nameof(SourceLocationId))]
    public Location? SourceLocation { get; set; }

    [ForeignKey(nameof(SourceItemLocationId))]
    public ItemLocation? SourceItemLocation { get; set; }
}

public enum KittingWorkOrderStatusEnum : byte
{
    Draft = 1,
    Reserved = 2,
    Completed = 3,
    Cancelled = 4
}

public enum KittingWorkOrderLineStatusEnum : byte
{
    Draft = 1,
    Reserved = 2,
    Consumed = 3,
    Released = 4
}
