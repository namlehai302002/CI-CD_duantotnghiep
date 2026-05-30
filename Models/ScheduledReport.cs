using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

/// <summary>
/// Cấu hình báo cáo tự động — Scheduled Report.
/// Tương đương Oracle BI Scheduler / SAP BW Report Scheduling.
/// Lưu trữ cấu hình và lịch sử chạy báo cáo định kỳ.
/// </summary>
[Table("ScheduledReports")]
public class ScheduledReport
{
    [Key]
    public int ScheduledReportId { get; set; }

    /// <summary>Tên báo cáo</summary>
    [Required, MaxLength(100)]
    public string ReportName { get; set; } = "";

    /// <summary>Loại báo cáo: StockSnapshot, OpsKpi, ExpiryAlert, AbcAnalysis, DockToStock, SpaceUtil</summary>
    [Required, MaxLength(50)]
    public string ReportType { get; set; } = "";

    /// <summary>Mô tả</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Biểu thức lịch: Daily, Weekly, Monthly, Quarterly</summary>
    [Required, MaxLength(30)]
    public string Schedule { get; set; } = "Daily";

    /// <summary>Giờ chạy (0-23)</summary>
    public int RunAtHour { get; set; } = 6;

    /// <summary>Ngày trong tuần (1=Mon, 7=Sun) — cho Weekly</summary>
    public int? DayOfWeek { get; set; }

    /// <summary>Ngày trong tháng (1-28) — cho Monthly</summary>
    public int? DayOfMonth { get; set; }

    /// <summary>Email người nhận (nhiều email cách bởi ;)</summary>
    [MaxLength(500)]
    public string? Recipients { get; set; }

    /// <summary>Kho áp dụng (null = tất cả)</summary>
    public int? WarehouseId { get; set; }

    /// <summary>Định dạng xuất: PDF, Excel, CSV</summary>
    [MaxLength(10)]
    public string OutputFormat { get; set; } = "Excel";

    /// <summary>Lần chạy cuối</summary>
    public DateTime? LastRunAt { get; set; }

    /// <summary>Kết quả lần chạy cuối</summary>
    [MaxLength(500)]
    public string? LastRunResult { get; set; }

    /// <summary>Lần chạy tiếp theo (tính toán)</summary>
    public DateTime? NextRunAt { get; set; }

    /// <summary>Tổng số lần đã chạy</summary>
    public int RunCount { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }
}
