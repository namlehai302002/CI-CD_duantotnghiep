using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("LicensePlates")]
public class LicensePlate : IOwnerScoped
{
    [Key]
    public long LicensePlateId { get; set; }

    [Required, MaxLength(30)]
    public string LpnCode { get; set; } = "";

    public long VoucherId { get; set; }

    // Legacy inbound-line shortcut columns are intentionally kept for safe data migration.
    // New business logic must use Details and CurrentLocationId instead.
    public long? VoucherDetailId { get; set; }

    public int WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public int? CurrentLocationId { get; set; }

    public long? ParentLpnId { get; set; }

    public LpnStatusEnum Status { get; set; } = LpnStatusEnum.Created;

    public LpnTypeEnum LpnType { get; set; } = LpnTypeEnum.Carton;

    [Column(TypeName = "decimal(18,4)")]
    public decimal? MaxWeightKg { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? ActualWeightKg { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? MaxVolumeCubicCm { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? ActualVolumeCubicCm { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? LengthCm { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? WidthCm { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? HeightCm { get; set; }

    public int? ItemId { get; set; }

    public int? LocationId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? Quantity { get; set; }

    [MaxLength(50)]
    public string? LotNumber { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public DateTime? ManufacturingDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? VoidedAt { get; set; }

    [MaxLength(100)]
    public string? VoidedBy { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey("VoucherId")]
    public Voucher? Voucher { get; set; }

    [ForeignKey(nameof(VoucherDetailId))]
    public VoucherDetail? VoucherDetail { get; set; }

    [ForeignKey(nameof(CurrentLocationId))]
    public Location? CurrentLocation { get; set; }

    [ForeignKey(nameof(ParentLpnId))]
    public LicensePlate? ParentLpn { get; set; }

    public ICollection<LicensePlate> ChildLpns { get; set; } = new List<LicensePlate>();

    public ICollection<LicensePlateDetail> Details { get; set; } = new List<LicensePlateDetail>();

    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [ForeignKey("LocationId")]
    public Location? Location { get; set; }

    [ForeignKey("WarehouseId")]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }
}

[Table("LicensePlateDetails")]
public class LicensePlateDetail : IOwnerScoped
{
    [Key]
    public long LicensePlateDetailId { get; set; }

    public long LicensePlateId { get; set; }

    public int ItemId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public long? VoucherDetailId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    [MaxLength(50)]
    public string? LotNumber { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ExpiryDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ManufacturingDate { get; set; }

    public InventoryHoldStatusEnum HoldStatus { get; set; } = InventoryHoldStatusEnum.Available;

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(LicensePlateId))]
    public LicensePlate? LicensePlate { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }

    [ForeignKey(nameof(VoucherDetailId))]
    public VoucherDetail? VoucherDetail { get; set; }

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }
}
