using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("OutboundPackages")]
public class OutboundPackage : IOwnerScoped
{
    [Key]
    public long OutboundPackageId { get; set; }

    [Required, MaxLength(40)]
    public string PackageCode { get; set; } = "";

    public long VoucherId { get; set; }
    public int WarehouseId { get; set; }
    public int? OwnerPartnerId { get; set; }

    [MaxLength(30)]
    public string SourceType { get; set; } = "Manual";

    [MaxLength(50)]
    public string? PackageType { get; set; }

    [MaxLength(30)]
    public string? ReferenceLpnCode { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? TotalQuantity { get; set; }

    public int ItemCount { get; set; }

    [MaxLength(100)]
    public string PackedBy { get; set; } = "";

    public DateTime PackedAt { get; set; } = VietnamTime.Now;

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string? TrackingNumber { get; set; }

    [MaxLength(50)]
    public string? ManifestCode { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? ActualCatchWeight { get; set; }

    public int? CatchWeightUomId { get; set; }

    public long? ShipmentLoadId { get; set; }

    public DateTime? LoadedAt { get; set; }

    [MaxLength(100)]
    public string? LoadedBy { get; set; }

    [ForeignKey(nameof(VoucherId))]
    public Voucher Voucher { get; set; } = null!;

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    [ForeignKey(nameof(CatchWeightUomId))]
    public UnitOfMeasure? CatchWeightUom { get; set; }

    [ForeignKey(nameof(ShipmentLoadId))]
    public ShipmentLoad? ShipmentLoad { get; set; }
}
