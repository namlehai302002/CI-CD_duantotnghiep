using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("PickTaskSerialAssignments")]
public class PickTaskSerialAssignment
{
    [Key]
    public long PickTaskSerialAssignmentId { get; set; }

    public long PickTaskId { get; set; }
    public long VoucherId { get; set; }
    public long? VoucherDetailId { get; set; }
    public long SerialNumberId { get; set; }
    public long? SerialReservationId { get; set; }

    [MaxLength(120)]
    public string SerialCode { get; set; } = "";

    [MaxLength(100)]
    public string ScannedBy { get; set; } = "";

    public DateTime ScannedAt { get; set; } = VietnamTime.Now;
    public DateTime? PostedAt { get; set; }
    public DateTime? VoidedAt { get; set; }

    [MaxLength(100)]
    public string? VoidedBy { get; set; }

    [ForeignKey(nameof(PickTaskId))]
    public PickTask? PickTask { get; set; }

    [ForeignKey(nameof(VoucherId))]
    public Voucher? Voucher { get; set; }

    [ForeignKey(nameof(VoucherDetailId))]
    public VoucherDetail? VoucherDetail { get; set; }

    [ForeignKey(nameof(SerialNumberId))]
    public SerialNumber? SerialNumber { get; set; }

    [ForeignKey(nameof(SerialReservationId))]
    public SerialReservation? SerialReservation { get; set; }
}
