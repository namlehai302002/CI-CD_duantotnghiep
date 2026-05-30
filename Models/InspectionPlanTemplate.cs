using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

/// <summary>
/// Mẫu kế hoạch kiểm tra chất lượng — Inspection Plan Template.
/// Tương đương SAP QM Inspection Plan / Oracle QM Plan.
/// Cho phép cấu hình sẵn checklist kiểm tra theo loại vật tư.
/// </summary>
[Table("InspectionPlanTemplates")]
public class InspectionPlanTemplate
{
    [Key]
    public int InspectionPlanTemplateId { get; set; }

    /// <summary>Tên kế hoạch kiểm tra</summary>
    [Required, MaxLength(100)]
    public string PlanName { get; set; } = "";

    /// <summary>Mô tả chi tiết</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Loại vật tư áp dụng (null = tất cả)</summary>
    public int? ItemCategoryId { get; set; }

    /// <summary>Danh sách mục kiểm tra (JSON array)</summary>
    [MaxLength(2000)]
    public string? ChecklistItems { get; set; }

    /// <summary>Công thức tính cỡ mẫu: Fixed, Percentage, AQL</summary>
    [MaxLength(30)]
    public string SampleSizeFormula { get; set; } = "Percentage";

    /// <summary>Giá trị cỡ mẫu (% hoặc số cố định)</summary>
    [Column(TypeName = "decimal(8,2)")]
    public decimal SampleSizeValue { get; set; } = 10m;

    /// <summary>Disposition mặc định khi đạt</summary>
    public QcDispositionEnum DefaultPassDisposition { get; set; } = QcDispositionEnum.Accept;

    /// <summary>Disposition mặc định khi không đạt</summary>
    public QcDispositionEnum DefaultFailDisposition { get; set; } = QcDispositionEnum.Reject;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    [ForeignKey(nameof(ItemCategoryId))]
    public ItemCategory? ItemCategory { get; set; }
}
