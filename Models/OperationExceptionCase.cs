using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("OperationExceptionCases")]
public class OperationExceptionCase
{
    [Key]
    public long OperationExceptionCaseId { get; set; }

    [Required, MaxLength(200)]
    public string ExceptionKey { get; set; } = "";

    [Required, MaxLength(50)]
    public string CategoryKey { get; set; } = "";

    [MaxLength(100)]
    public string CategoryLabel { get; set; } = "";

    public int WarehouseId { get; set; }

    [MaxLength(100)]
    public string ReferenceCode { get; set; } = "";

    [MaxLength(100)]
    public string? SecondaryReference { get; set; }

    public OperationExceptionStatusEnum Status { get; set; } = OperationExceptionStatusEnum.Open;

    [MaxLength(100)]
    public string? AssignedTo { get; set; }

    [MaxLength(100)]
    public string? AcknowledgedBy { get; set; }

    public DateTime? AcknowledgedAt { get; set; }

    [MaxLength(100)]
    public string? ResolvedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }

    [MaxLength(500)]
    public string? ResolutionNote { get; set; }

    public DateTime FirstDetectedAt { get; set; } = VietnamTime.Now;

    public DateTime LastDetectedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }
}
