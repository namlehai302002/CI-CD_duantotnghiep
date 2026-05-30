using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

/// <summary>
/// Tiêu chuẩn năng suất lao động — Enterprise Labor Standards (engineered standards).
/// Dùng để so sánh hiệu suất thực tế vs kỳ vọng cho từng loại tác vụ kho.
/// Tương đương Oracle WMS Labor Management / Manhattan Labor Standard.
/// </summary>
[Table("LaborStandards")]
public class LaborStandard
{
    [Key]
    public int LaborStandardId { get; set; }

    /// <summary>Loại tác vụ: Pick, Putaway, Pack, Count, Receive</summary>
    [Required, MaxLength(30)]
    public string TaskType { get; set; } = "";

    /// <summary>Tên hiển thị</summary>
    [Required, MaxLength(100)]
    public string TaskTypeName { get; set; } = "";

    /// <summary>Đơn vị đo: Lines, Units, Pallets, Cases</summary>
    [Required, MaxLength(20)]
    public string UnitOfWork { get; set; } = "Lines";

    /// <summary>Số phút kỳ vọng để hoàn thành 1 đơn vị công việc</summary>
    [Column(TypeName = "decimal(8,2)")]
    public decimal ExpectedMinutesPerUnit { get; set; } = 5m;

    /// <summary>Số đơn vị kỳ vọng mỗi giờ (= 60 / ExpectedMinutesPerUnit)</summary>
    [Column(TypeName = "decimal(8,2)")]
    public decimal ExpectedUnitsPerHour { get; set; } = 12m;

    /// <summary>Ngưỡng hiệu suất tối thiểu (%)</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal MinPerformancePercent { get; set; } = 80m;

    /// <summary>Ngưỡng hiệu suất xuất sắc (%)</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal ExcellentPerformancePercent { get; set; } = 120m;

    public int? WarehouseId { get; set; }

    public int? ZoneId { get; set; }

    [MaxLength(50)]
    public string? ItemClass { get; set; }

    [Column(TypeName = "date")]
    public DateTime EffectiveFrom { get; set; } = VietnamTime.Now.Date;

    [Column(TypeName = "date")]
    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey(nameof(ZoneId))]
    public Zone? Zone { get; set; }
}
