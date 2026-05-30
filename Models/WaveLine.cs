using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("WaveLines")]
public class WaveLine
{
    [Key]
    public long WaveLineId { get; set; }

    public long WaveId { get; set; }

    public long VoucherId { get; set; }

    public int ItemId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal RequiredQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal PickedQty { get; set; }

    public byte Status { get; set; } = 1; // 1=Open,2=InProgress,3=Completed

    [ForeignKey(nameof(WaveId))]
    public Wave? Wave { get; set; }

    [ForeignKey(nameof(VoucherId))]
    public Voucher? Voucher { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }
}

