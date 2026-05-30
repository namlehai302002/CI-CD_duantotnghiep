using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("SerialNumbers")]
public class SerialNumber : IOwnerScoped
{
    [Key]
    public long SerialNumberId { get; set; }

    [Required, MaxLength(120)]
    public string SerialCode { get; set; } = "";

    public int WarehouseId { get; set; }
    public int? OwnerPartnerId { get; set; }
    public int ItemId { get; set; }
    public int? LocationId { get; set; }
    public long VoucherId { get; set; }
    public long? VoucherDetailId { get; set; }
    public long? LicensePlateId { get; set; }

    [MaxLength(50)]
    public string? LotNumber { get; set; }

    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufacturingDate { get; set; }

    public SerialNumberStatusEnum Status { get; set; } = SerialNumberStatusEnum.Active;

    public InventoryHoldStatusEnum HoldStatus { get; set; } = InventoryHoldStatusEnum.Available;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;
    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public DateTime? ConsumedAt { get; set; }

    [MaxLength(100)]
    public string? ConsumedBy { get; set; }

    public long? ConsumedVoucherId { get; set; }

    public long? ConsumedPickTaskId { get; set; }

    public DateTime? VoidedAt { get; set; }

    [MaxLength(100)]
    public string? VoidedBy { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }

    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }

    [ForeignKey(nameof(VoucherId))]
    public Voucher? Voucher { get; set; }

    [ForeignKey(nameof(ConsumedVoucherId))]
    public Voucher? ConsumedVoucher { get; set; }

    [ForeignKey(nameof(VoucherDetailId))]
    public VoucherDetail? VoucherDetail { get; set; }

    [ForeignKey(nameof(LicensePlateId))]
    public LicensePlate? LicensePlate { get; set; }

    [ForeignKey(nameof(ConsumedPickTaskId))]
    public PickTask? ConsumedPickTask { get; set; }
}
