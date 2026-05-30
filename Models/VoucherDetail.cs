using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("VoucherDetails")]
public class VoucherDetail : IOwnerScoped
{
    [Key]
    public long VoucherDetailId { get; set; }

    public long VoucherId { get; set; }

    public int ItemId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public int? LocationId { get; set; }

    public int? DestLocationId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TransactionQty { get; set; }

    public int TransactionUomId { get; set; }

    public int? PackagingUnitId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal DefectQty { get; set; } = 0;

    [Column(TypeName = "decimal(18,4)")]
    public decimal DefectBaseQty { get; set; } = 0;

    [Column(TypeName = "decimal(18,6)")]
    public decimal ConversionRate { get; set; } = 1;

    [Column(TypeName = "decimal(18,4)")]
    public decimal BaseQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal LineAmount { get; set; }

    public QualityStatusEnum QualityStatus { get; set; } = QualityStatusEnum.Good;

    [Column(TypeName = "date")]
    public DateTime? ExpiryDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ManufacturingDate { get; set; }

    [MaxLength(50)]
    public string? LotNumber { get; set; }

    [MaxLength(300)]
    public string? Notes { get; set; }

    public int LineNumber { get; set; }

    [ForeignKey("VoucherId")]
    public Voucher Voucher { get; set; } = null!;

    [ForeignKey("ItemId")]
    public Item Item { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    [ForeignKey("LocationId")]
    public Location? Location { get; set; }

    [ForeignKey("DestLocationId")]
    public Location? DestLocation { get; set; }

    [ForeignKey("TransactionUomId")]
    public UnitOfMeasure TransactionUom { get; set; } = null!;

    [ForeignKey("PackagingUnitId")]
    public PackagingUnit? PackagingUnit { get; set; }

    public string QualityStatusName => QualityStatus switch
    {
        QualityStatusEnum.Good => "Tốt",
        QualityStatusEnum.Defect => "Lỗi",
        QualityStatusEnum.Pending => "Chờ kiểm",
        QualityStatusEnum.Inspecting => "Đang kiểm",
        QualityStatusEnum.Passed => "Đạt",
        QualityStatusEnum.Failed => "Không đạt",
        QualityStatusEnum.Quarantine => "Cách ly",
        QualityStatusEnum.OnHold => "Tạm giữ",
        _ => "Khác"
    };
}
