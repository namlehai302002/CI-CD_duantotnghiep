using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("CatchWeightEntries")]
public class CatchWeightEntry : IOwnerScoped
{
    [Key]
    public long CatchWeightEntryId { get; set; }

    public int ItemId { get; set; }
    public int WarehouseId { get; set; }
    public int? OwnerPartnerId { get; set; }
    public long? VoucherId { get; set; }
    public long? VoucherDetailId { get; set; }
    public long? LicensePlateId { get; set; }
    public long? LicensePlateDetailId { get; set; }
    public long? OutboundPackageId { get; set; }
    public long? PickTaskId { get; set; }
    public long? SerialNumberId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal BaseQuantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ActualWeight { get; set; }

    public int WeightUomId { get; set; }
    public CatchWeightCapturePointEnum CapturePoint { get; set; } = CatchWeightCapturePointEnum.Receive;
    public CatchWeightStatusEnum Status { get; set; } = CatchWeightStatusEnum.Captured;

    [MaxLength(100)]
    public string CapturedBy { get; set; } = "";

    public DateTime CapturedAt { get; set; } = VietnamTime.Now;

    [Required, MaxLength(160)]
    public string IdempotencyKey { get; set; } = "";

    public string MetadataJson { get; set; } = "{}";

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    [ForeignKey(nameof(VoucherId))]
    public Voucher? Voucher { get; set; }

    [ForeignKey(nameof(VoucherDetailId))]
    public VoucherDetail? VoucherDetail { get; set; }

    [ForeignKey(nameof(LicensePlateId))]
    public LicensePlate? LicensePlate { get; set; }

    [ForeignKey(nameof(LicensePlateDetailId))]
    public LicensePlateDetail? LicensePlateDetail { get; set; }

    [ForeignKey(nameof(OutboundPackageId))]
    public OutboundPackage? OutboundPackage { get; set; }

    [ForeignKey(nameof(PickTaskId))]
    public PickTask? PickTask { get; set; }

    [ForeignKey(nameof(SerialNumberId))]
    public SerialNumber? SerialNumber { get; set; }

    [ForeignKey(nameof(WeightUomId))]
    public UnitOfMeasure? WeightUom { get; set; }
}
