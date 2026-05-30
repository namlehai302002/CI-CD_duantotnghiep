using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("PickTaskAllocations")]
public class PickTaskAllocation
{
    [Key]
    public long PickTaskAllocationId { get; set; }

    public long PickTaskId { get; set; }

    public long StockReservationId { get; set; }

    public long VoucherId { get; set; }

    public long? VoucherDetailId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AllocatedQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal PickedQty { get; set; }

    [ForeignKey(nameof(PickTaskId))]
    public PickTask? PickTask { get; set; }

    [ForeignKey(nameof(StockReservationId))]
    public StockReservation? StockReservation { get; set; }

    [ForeignKey(nameof(VoucherId))]
    public Voucher? Voucher { get; set; }

    [ForeignKey(nameof(VoucherDetailId))]
    public VoucherDetail? VoucherDetail { get; set; }
}
