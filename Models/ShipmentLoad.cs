using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("ShipmentLoads")]
public class ShipmentLoad : IOwnerScoped
{
    [Key]
    public long ShipmentLoadId { get; set; }

    [Required, MaxLength(40)]
    public string LoadCode { get; set; } = "";

    public int WarehouseId { get; set; }
    public int? OwnerPartnerId { get; set; }
    public ShipmentLoadStatusEnum Status { get; set; } = ShipmentLoadStatusEnum.Planned;

    [MaxLength(100)]
    public string? CarrierName { get; set; }

    [MaxLength(100)]
    public string? RouteCode { get; set; }

    [MaxLength(100)]
    public string? RouteName { get; set; }

    [MaxLength(30)]
    public string? VehicleNumber { get; set; }

    public int? TrailerId { get; set; }
    public long? YardVisitId { get; set; }

    [MaxLength(20)]
    public string? DockDoor { get; set; }

    public DateTime? PlannedDepartureAt { get; set; }
    public DateTime? ActualDepartureAt { get; set; }

    [MaxLength(50)]
    public string? SealNumber { get; set; }

    [MaxLength(50)]
    public string? ManifestCode { get; set; }

    [MaxLength(100)]
    public string? TrackingNumber { get; set; }

    [MaxLength(120)]
    public string? TmsReferenceNo { get; set; }

    public int TotalVoucherCount { get; set; }
    public int TotalPackageCount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalQuantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? TotalCatchWeight { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(100)]
    public string? DepartedBy { get; set; }

    public DateTime? DepartedAt { get; set; }

    [MaxLength(100)]
    public string? ClosedBy { get; set; }

    public DateTime? ClosedAt { get; set; }

    [MaxLength(100)]
    public string? CancelledBy { get; set; }

    public DateTime? CancelledAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    [ForeignKey(nameof(TrailerId))]
    public Trailer? Trailer { get; set; }

    [ForeignKey(nameof(YardVisitId))]
    public YardVisit? YardVisit { get; set; }

    public ICollection<ShipmentLoadVoucher> Vouchers { get; set; } = new List<ShipmentLoadVoucher>();
    public ICollection<ShipmentLoadPackage> Packages { get; set; } = new List<ShipmentLoadPackage>();
}

[Table("ShipmentLoadVouchers")]
public class ShipmentLoadVoucher
{
    [Key]
    public long ShipmentLoadVoucherId { get; set; }

    public long ShipmentLoadId { get; set; }
    public long VoucherId { get; set; }
    public int Sequence { get; set; }
    public int? StopNumber { get; set; }

    [MaxLength(50)]
    public string StatusSnapshot { get; set; } = "";

    [MaxLength(100)]
    public string AddedBy { get; set; } = "";

    public DateTime AddedAt { get; set; } = VietnamTime.Now;

    [MaxLength(100)]
    public string? RemovedBy { get; set; }

    public DateTime? RemovedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(ShipmentLoadId))]
    public ShipmentLoad? ShipmentLoad { get; set; }

    [ForeignKey(nameof(VoucherId))]
    public Voucher? Voucher { get; set; }
}

[Table("ShipmentLoadPackages")]
public class ShipmentLoadPackage
{
    [Key]
    public long ShipmentLoadPackageId { get; set; }

    public long ShipmentLoadId { get; set; }
    public long OutboundPackageId { get; set; }

    [MaxLength(40)]
    public string PackageCodeSnapshot { get; set; } = "";

    [MaxLength(30)]
    public string? ReferenceLpnCode { get; set; }

    public bool IsLoaded { get; set; }

    [MaxLength(100)]
    public string? LoadedBy { get; set; }

    public DateTime? LoadedAt { get; set; }

    [MaxLength(100)]
    public string AddedBy { get; set; } = "";

    public DateTime AddedAt { get; set; } = VietnamTime.Now;

    [MaxLength(100)]
    public string? RemovedBy { get; set; }

    public DateTime? RemovedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(ShipmentLoadId))]
    public ShipmentLoad? ShipmentLoad { get; set; }

    [ForeignKey(nameof(OutboundPackageId))]
    public OutboundPackage? OutboundPackage { get; set; }
}
