using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("Waves")]
public class Wave : IOwnerScoped
{
    [Key]
    public long WaveId { get; set; }

    [Required, MaxLength(30)]
    public string WaveCode { get; set; } = "";

    /// <summary>Mã hồ sơ sóng: Standard / CarrierGroup / ZoneGroup / RouteGroup / Priority</summary>
    [MaxLength(50)]
    public string? WaveProfile { get; set; }

    /// <summary>Mã hãng vận chuyển — dùng để gom đơn theo carrier</summary>
    [MaxLength(50)]
    public string? CarrierCode { get; set; }

    /// <summary>Tên hãng vận chuyển</summary>
    [MaxLength(100)]
    public string? CarrierName { get; set; }

    /// <summary>Mã tuyến giao hàng</summary>
    [MaxLength(50)]
    public string? RouteCode { get; set; }

    /// <summary>Giờ cutoff — đơn phải được pick trước giờ này</summary>
    public DateTime? CutoffTime { get; set; }

    /// <summary>Độ ưu tiên sóng — cao hơn được ưu tiên pick trước</summary>
    public WavePriorityEnum Priority { get; set; } = WavePriorityEnum.Normal;

    public int WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public WaveStatusEnum Status { get; set; } = WaveStatusEnum.Created;

    [MaxLength(300)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? ReleasedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    public ICollection<WaveLine> Lines { get; set; } = new List<WaveLine>();
    public ICollection<PickTask> PickTasks { get; set; } = new List<PickTask>();
    public ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
}
