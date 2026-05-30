using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("PickTaskScanLogs")]
public class PickTaskScanLog
{
    [Key]
    public long PickTaskScanLogId { get; set; }

    public long PickTaskId { get; set; }

    [MaxLength(100)]
    public string ScannedBy { get; set; } = "";

    [MaxLength(200)]
    public string ScanValue { get; set; } = "";

    [Column(TypeName = "decimal(18,4)")]
    public decimal Qty { get; set; }

    [MaxLength(300)]
    public string? Notes { get; set; }

    public DateTime ScannedAt { get; set; } = VietnamTime.Now;

    [ForeignKey(nameof(PickTaskId))]
    public PickTask? PickTask { get; set; }
}

