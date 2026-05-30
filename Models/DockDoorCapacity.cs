using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

// ─── P1.3: Dock Door Capacity Model ─────────────────────────────────────────────

/// <summary>Cấu hình năng lực cửa dock theo ngày/giờ</summary>
[Table("DockDoorCapacities")]
public class DockDoorCapacity
{
    [Key]
    public int CapacityId { get; set; }

    /// <summary>Mã cửa dock (VD: DOCK-01)</summary>
    [Required, MaxLength(20)]
    public string DockDoor { get; set; } = "";

    public int WarehouseId { get; set; }

    /// <summary>Ngày trong tuần: 0=CN, 1=T2, ..., 6=T7. Null = áp dụng mọi ngày.</summary>
    public DayOfWeek? DayOfWeek { get; set; }

    /// <summary>Giờ bắt đầu ca (VD: 8:00 = 480 phút)</summary>
    public int SlotStartMinutes { get; set; }

    /// <summary>Giờ kết thúc ca</summary>
    public int SlotEndMinutes { get; set; }

    /// <summary>Số lượng appointment tối đa trong ca này</summary>
    public int MaxAppointments { get; set; } = 4;

    /// <summary>Thời gian dỡ hàng trung bình (phút)</summary>
    public int AvgUnloadMinutes { get; set; } = 60;

    /// <summary>Loại cửa: Receiving / Shipping / Both</summary>
    public DockDoorTypeEnum DoorType { get; set; } = DockDoorTypeEnum.Both;

    /// <summary>Cửa có hỗ trợ hàng lạnh?</summary>
    public bool IsRefrigerated { get; set; }

    /// <summary>Cửa có hỗ trợ hàng nguy hiểm?</summary>
    public bool IsHazmat { get; set; }

    [MaxLength(100)]
    public string? Notes { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }
}

public enum DockDoorTypeEnum : byte
{
    Both = 1,
    Receiving = 2,
    Shipping = 3
}
