using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("VasWorkOrders")]
public class VasWorkOrder : IOwnerScoped
{
    [Key]
    public long VasWorkOrderId { get; set; }

    [Required, MaxLength(40)]
    public string WorkOrderCode { get; set; } = "";

    public VasOperationTypeEnum OperationType { get; set; } = VasOperationTypeEnum.CoPacking;

    public VasWorkOrderStatusEnum Status { get; set; } = VasWorkOrderStatusEnum.Draft;

    public int WarehouseId { get; set; }

    public int? PartnerId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public long? VoucherId { get; set; }

    public int PrimaryItemId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal PlannedQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal CompletedQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal QcPassedQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal QcFailedQty { get; set; }

    public VasQcResultEnum QcResult { get; set; } = VasQcResultEnum.Pending;

    [MaxLength(500)]
    public string? QcNote { get; set; }

    [MaxLength(100)]
    public string? QcBy { get; set; }

    public DateTime? QcAt { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ActualLaborMinutes { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal LaborRatePerHour { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal MaterialCost { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal LaborCost { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalCost { get; set; }

    public DateTime? ReservedAt { get; set; }

    [MaxLength(100)]
    public string? ReservedBy { get; set; }

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

    [ForeignKey(nameof(PartnerId))]
    public Partner? Partner { get; set; }

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    [ForeignKey(nameof(VoucherId))]
    public Voucher? Voucher { get; set; }

    [ForeignKey(nameof(PrimaryItemId))]
    public Item PrimaryItem { get; set; } = null!;

    public ICollection<VasOperation> Operations { get; set; } = new List<VasOperation>();
    public ICollection<VasMaterialLine> MaterialLines { get; set; } = new List<VasMaterialLine>();

    [NotMapped]
    public string OperationTypeName => OperationType switch
    {
        VasOperationTypeEnum.LightAssembly => "Lắp ráp nhẹ",
        VasOperationTypeEnum.CoPacking => "Đóng gói phối hợp",
        VasOperationTypeEnum.Repack => "Đóng gói lại",
        VasOperationTypeEnum.Relabel => "Dán nhãn lại",
        _ => "Khác"
    };

    [NotMapped]
    public string StatusName => Status switch
    {
        VasWorkOrderStatusEnum.Draft => "Nháp",
        VasWorkOrderStatusEnum.Reserved => "Đã giữ chỗ",
        VasWorkOrderStatusEnum.InProgress => "Đang thực hiện",
        VasWorkOrderStatusEnum.QcPending => "Chờ kiểm tra chất lượng",
        VasWorkOrderStatusEnum.Completed => "Hoàn tất",
        VasWorkOrderStatusEnum.Cancelled => "Đã hủy",
        _ => "Khác"
    };

    [NotMapped]
    public string QcResultName => QcResult switch
    {
        VasQcResultEnum.Pending => "Chờ kiểm tra",
        VasQcResultEnum.Passed => "Đạt",
        VasQcResultEnum.Failed => "Không đạt",
        VasQcResultEnum.ReworkRequired => "Cần xử lý lại",
        _ => "Khác"
    };
}

[Table("VasOperations")]
public class VasOperation
{
    [Key]
    public long VasOperationId { get; set; }

    public long VasWorkOrderId { get; set; }

    public int StepNumber { get; set; }

    [Required, MaxLength(120)]
    public string OperationName { get; set; } = "";

    [MaxLength(100)]
    public string? PerformedBy { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ActualMinutes { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(VasWorkOrderId))]
    public VasWorkOrder VasWorkOrder { get; set; } = null!;

    [NotMapped]
    public bool IsCompleted => CompletedAt.HasValue;
}

[Table("VasMaterialLines")]
public class VasMaterialLine : IOwnerScoped
{
    [Key]
    public long VasMaterialLineId { get; set; }

    public long VasWorkOrderId { get; set; }

    public int MaterialItemId { get; set; }

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

    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitCostSnapshot { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ConsumedCost { get; set; }

    [MaxLength(50)]
    public string? LotNumber { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ExpiryDate { get; set; }

    public VasMaterialLineStatusEnum Status { get; set; } = VasMaterialLineStatusEnum.Draft;

    [MaxLength(300)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(VasWorkOrderId))]
    public VasWorkOrder VasWorkOrder { get; set; } = null!;

    [ForeignKey(nameof(MaterialItemId))]
    public Item MaterialItem { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    [ForeignKey(nameof(SourceLocationId))]
    public Location? SourceLocation { get; set; }

    [ForeignKey(nameof(SourceItemLocationId))]
    public ItemLocation? SourceItemLocation { get; set; }
}

public enum VasOperationTypeEnum : byte
{
    LightAssembly = 1,
    CoPacking = 2,
    Repack = 3,
    Relabel = 4
}

public enum VasWorkOrderStatusEnum : byte
{
    Draft = 1,
    Reserved = 2,
    InProgress = 3,
    QcPending = 4,
    Completed = 5,
    Cancelled = 6
}

public enum VasMaterialLineStatusEnum : byte
{
    Draft = 1,
    Reserved = 2,
    Consumed = 3,
    Released = 4
}

public enum VasQcResultEnum : byte
{
    Pending = 1,
    Passed = 2,
    Failed = 3,
    ReworkRequired = 4
}
