using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("SerialReservations")]
public class SerialReservation
{
    [Key]
    public long SerialReservationId { get; set; }

    public long SerialNumberId { get; set; }
    public long? StockReservationId { get; set; }
    public long? PickTaskId { get; set; }
    public long VoucherId { get; set; }
    public long? VoucherDetailId { get; set; }
    public int WarehouseId { get; set; }
    public int ItemId { get; set; }
    public int LocationId { get; set; }
    public long? LicensePlateId { get; set; }

    [MaxLength(50)]
    public string? LotNumber { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ExpiryDate { get; set; }

    public InventoryHoldStatusEnum HoldStatus { get; set; } = InventoryHoldStatusEnum.Available;
    public SerialReservationStatusEnum Status { get; set; } = SerialReservationStatusEnum.Reserved;

    [Required, MaxLength(180)]
    public string IdempotencyKey { get; set; } = "";

    [MaxLength(100)]
    public string ReservedBy { get; set; } = "";

    public DateTime ReservedAt { get; set; } = VietnamTime.Now;

    [MaxLength(100)]
    public string? PickedBy { get; set; }

    public DateTime? PickedAt { get; set; }

    [MaxLength(100)]
    public string? ConsumedBy { get; set; }

    public DateTime? ConsumedAt { get; set; }

    [MaxLength(100)]
    public string? ReleasedBy { get; set; }

    public DateTime? ReleasedAt { get; set; }

    [MaxLength(100)]
    public string? VoidedBy { get; set; }

    public DateTime? VoidedAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;
    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(SerialNumberId))]
    public SerialNumber? SerialNumber { get; set; }

    [ForeignKey(nameof(StockReservationId))]
    public StockReservation? StockReservation { get; set; }

    [ForeignKey(nameof(PickTaskId))]
    public PickTask? PickTask { get; set; }

    [ForeignKey(nameof(VoucherId))]
    public Voucher? Voucher { get; set; }

    [ForeignKey(nameof(VoucherDetailId))]
    public VoucherDetail? VoucherDetail { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }

    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }

    [ForeignKey(nameof(LicensePlateId))]
    public LicensePlate? LicensePlate { get; set; }
}

[Table("SerialInventoryOperations")]
public class SerialInventoryOperation
{
    [Key]
    public long SerialInventoryOperationId { get; set; }

    [Required, MaxLength(180)]
    public string IdempotencyKey { get; set; } = "";

    [Required, MaxLength(60)]
    public string OperationType { get; set; } = "";

    [MaxLength(80)]
    public string? ReferenceType { get; set; }

    public long? ReferenceId { get; set; }
    public long? SerialNumberId { get; set; }
    public long? SerialReservationId { get; set; }
    public SerialInventoryOperationStatusEnum Status { get; set; } = SerialInventoryOperationStatusEnum.Applied;

    public string? PayloadJson { get; set; }

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;
    public DateTime? AppliedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(SerialNumberId))]
    public SerialNumber? SerialNumber { get; set; }

    [ForeignKey(nameof(SerialReservationId))]
    public SerialReservation? SerialReservation { get; set; }
}
