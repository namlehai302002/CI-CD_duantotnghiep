using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("PickTotes")]
public class PickTote
{
    [Key]
    public long PickToteId { get; set; }

    [Required, MaxLength(30)]
    public string ToteCode { get; set; } = "";

    /// <summary>Xe đẩy mà tote này đang nằm trên (null = tote rời)</summary>
    public int? PickCartId { get; set; }

    /// <summary>Vị trí slot trên xe (1, 2, 3...)</summary>
    public int? SlotPosition { get; set; }

    /// <summary>Wave hiện tại đang sử dụng tote này</summary>
    public long? WaveId { get; set; }

    /// <summary>Phiếu xuất được map vào tote này trong wave hiện tại</summary>
    public long? VoucherId { get; set; }

    public PickToteStatusEnum Status { get; set; } = PickToteStatusEnum.Empty;

    [MaxLength(100)]
    public string? AssignedBy { get; set; }

    public DateTime? AssignedAt { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(PickCartId))]
    public PickCart? PickCart { get; set; }

    [ForeignKey(nameof(WaveId))]
    public Wave? Wave { get; set; }

    [ForeignKey(nameof(VoucherId))]
    public Voucher? Voucher { get; set; }
}
