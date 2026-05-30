using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

public enum LabelPurposeEnum : byte
{
    OutboundVoucher = 1,
    OutboundPackage = 2,
    ShippingPackageLabel = 3,
    ShipmentLoadManifest = 4,
    ShippingHandoverDocument = 5
}

public enum LabelPrintJobStatusEnum : byte
{
    Created = 1,
    Printed = 2
}

[Table("PartnerLabelTemplates")]
public class PartnerLabelTemplate
{
    [Key]
    public long PartnerLabelTemplateId { get; set; }

    public int? PartnerId { get; set; }

    public LabelPurposeEnum LabelPurpose { get; set; } = LabelPurposeEnum.OutboundVoucher;

    [Required, MaxLength(120)]
    public string TemplateName { get; set; } = "";

    [Required, MaxLength(20)]
    public string LabelSize { get; set; } = "50x30";

    [Required, MaxLength(20)]
    public string CodeType { get; set; } = "barcode";

    [MaxLength(300)]
    public string HeaderTemplate { get; set; } = "";

    [MaxLength(1000)]
    public string BodyTemplate { get; set; } = "";

    [MaxLength(300)]
    public string FooterTemplate { get; set; } = "";

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(PartnerId))]
    public Partner? Partner { get; set; }

    public ICollection<LabelPrintJob> PrintJobs { get; set; } = new List<LabelPrintJob>();

    public string PurposeName => LabelPurpose switch
    {
        LabelPurposeEnum.OutboundVoucher => "Phiếu xuất",
        LabelPurposeEnum.OutboundPackage => "Kiện xuất",
        LabelPurposeEnum.ShippingPackageLabel => "Nhãn vận chuyển",
        LabelPurposeEnum.ShipmentLoadManifest => "Bản kê chuyến xe",
        LabelPurposeEnum.ShippingHandoverDocument => "Biên bản bàn giao",
        _ => "Khác"
    };
}

[Table("PartnerItemLabelRules")]
public class PartnerItemLabelRule
{
    [Key]
    public long PartnerItemLabelRuleId { get; set; }

    public int PartnerId { get; set; }

    public int ItemId { get; set; }

    [Required, MaxLength(80)]
    public string CustomerItemCode { get; set; } = "";

    [MaxLength(200)]
    public string? CustomerItemName { get; set; }

    [MaxLength(300)]
    public string? CustomText { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(PartnerId))]
    public Partner? Partner { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }
}

[Table("LabelPrintJobs")]
public class LabelPrintJob
{
    [Key]
    public long LabelPrintJobId { get; set; }

    [Required, MaxLength(40)]
    public string JobCode { get; set; } = "";

    public LabelPurposeEnum LabelPurpose { get; set; }

    public LabelPrintJobStatusEnum Status { get; set; } = LabelPrintJobStatusEnum.Created;

    public int PartnerId { get; set; }

    public long? VoucherId { get; set; }

    public long? OutboundPackageId { get; set; }

    public long? ShipmentLoadId { get; set; }

    public long? ShippingHandoverLogId { get; set; }

    public long? CarrierShipmentId { get; set; }

    public long? PartnerLabelTemplateId { get; set; }

    [MaxLength(40)]
    public string? DocumentType { get; set; }

    [MaxLength(80)]
    public string? DocumentNumber { get; set; }

    [Required, MaxLength(20)]
    public string LabelSize { get; set; } = "50x30";

    [Required, MaxLength(20)]
    public string CodeType { get; set; } = "barcode";

    public int TotalLabels { get; set; }

    [MaxLength(100)]
    public string RequestedBy { get; set; } = "";

    public DateTime RequestedAt { get; set; } = VietnamTime.Now;

    [MaxLength(100)]
    public string? PrintedBy { get; set; }

    public DateTime? PrintedAt { get; set; }

    [MaxLength(300)]
    public string? SourceDescription { get; set; }

    public string? TemplateSnapshotJson { get; set; }

    public string? MetadataJson { get; set; }

    [ForeignKey(nameof(PartnerId))]
    public Partner? Partner { get; set; }

    [ForeignKey(nameof(VoucherId))]
    public Voucher? Voucher { get; set; }

    [ForeignKey(nameof(OutboundPackageId))]
    public OutboundPackage? OutboundPackage { get; set; }

    [ForeignKey(nameof(ShipmentLoadId))]
    public ShipmentLoad? ShipmentLoad { get; set; }

    [ForeignKey(nameof(ShippingHandoverLogId))]
    public ShippingHandoverLog? ShippingHandoverLog { get; set; }

    [ForeignKey(nameof(CarrierShipmentId))]
    public CarrierShipment? CarrierShipment { get; set; }

    [ForeignKey(nameof(PartnerLabelTemplateId))]
    public PartnerLabelTemplate? PartnerLabelTemplate { get; set; }

    public ICollection<LabelPrintJobLine> Lines { get; set; } = new List<LabelPrintJobLine>();

    public string PurposeName => LabelPurpose switch
    {
        LabelPurposeEnum.OutboundVoucher => "Phiếu xuất",
        LabelPurposeEnum.OutboundPackage => "Kiện xuất",
        LabelPurposeEnum.ShippingPackageLabel => "Nhãn vận chuyển",
        LabelPurposeEnum.ShipmentLoadManifest => "Bản kê chuyến xe",
        LabelPurposeEnum.ShippingHandoverDocument => "Biên bản bàn giao",
        _ => "Khác"
    };

    public string StatusName => Status switch
    {
        LabelPrintJobStatusEnum.Created => "Đã tạo",
        LabelPrintJobStatusEnum.Printed => "Đã in",
        _ => "Khác"
    };
}

[Table("LabelPrintJobLines")]
public class LabelPrintJobLine
{
    [Key]
    public long LabelPrintJobLineId { get; set; }

    public long LabelPrintJobId { get; set; }

    public long? VoucherDetailId { get; set; }

    public long? OutboundPackageId { get; set; }

    public int ItemId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    public int PrintQuantity { get; set; } = 1;

    [Required, MaxLength(120)]
    public string BarcodeValue { get; set; } = "";

    [Required, MaxLength(50)]
    public string InternalItemCode { get; set; } = "";

    [Required, MaxLength(200)]
    public string InternalItemName { get; set; } = "";

    [MaxLength(80)]
    public string? CustomerItemCode { get; set; }

    [MaxLength(200)]
    public string? CustomerItemName { get; set; }

    [MaxLength(200)]
    public string PartnerName { get; set; } = "";

    [MaxLength(30)]
    public string? VoucherCode { get; set; }

    [MaxLength(40)]
    public string? PackageCode { get; set; }

    [MaxLength(100)]
    public string? TrackingNumber { get; set; }

    [MaxLength(300)]
    public string HeaderText { get; set; } = "";

    [MaxLength(1000)]
    public string BodyText { get; set; } = "";

    [MaxLength(300)]
    public string FooterText { get; set; } = "";

    public string? RenderDataJson { get; set; }

    [ForeignKey(nameof(LabelPrintJobId))]
    public LabelPrintJob? LabelPrintJob { get; set; }

    [ForeignKey(nameof(VoucherDetailId))]
    public VoucherDetail? VoucherDetail { get; set; }

    [ForeignKey(nameof(OutboundPackageId))]
    public OutboundPackage? OutboundPackage { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }
}
