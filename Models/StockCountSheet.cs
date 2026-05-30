using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("StockCountSheets")]
public class StockCountSheet
{
    [Key]
    public long StockCountSheetId { get; set; }

    /// <summary>Mã phiếu kiểm kho: CC-YYYYMMDD-NNNN</summary>
    [MaxLength(30)]
    public string? SheetCode { get; set; }

    public int WarehouseId { get; set; }

    [Column(TypeName = "date")]
    public DateTime CountDate { get; set; } = VietnamTime.Now.Date;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public StockCountStatusEnum Status { get; set; } = StockCountStatusEnum.Draft;

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? CompletedAt { get; set; }

    [MaxLength(100)]
    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [MaxLength(500)]
    public string? ApprovalReason { get; set; }

    [MaxLength(100)]
    public string? UnlockedBy { get; set; }

    public DateTime? UnlockedAt { get; set; }

    [MaxLength(500)]
    public string? UnlockReason { get; set; }

    public long? GeneratedAdjustmentVoucherId { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey(nameof(GeneratedAdjustmentVoucherId))]
    public Voucher? GeneratedAdjustmentVoucher { get; set; }

    public ICollection<StockCountLine> Lines { get; set; } = new List<StockCountLine>();
}

