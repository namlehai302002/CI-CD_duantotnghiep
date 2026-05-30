using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("StockCountLines")]
public class StockCountLine
{
    [Key]
    public long StockCountLineId { get; set; }

    public long StockCountSheetId { get; set; }

    public int ItemId { get; set; }

    public int LocationId { get; set; }

    [MaxLength(50)]
    public string? LotNumber { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ExpiryDate { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal SystemQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? CountedQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? Variance { get; set; }

    /// <summary>Chênh lệch = CountedQty - SystemQty (computed, not stored)</summary>
    [NotMapped]
    public decimal? DiffQty => CountedQty.HasValue ? CountedQty.Value - SystemQty : null;

    /// <summary>Status của dòng kiểm kho: 1=Open, 2=Approved, 3=Recount</summary>
    public byte Status { get; set; } = 1;

    /// <summary>Người đếm</summary>
    [MaxLength(100)]
    public string? CountedBy { get; set; }

    /// <summary>Thời điểm đếm</summary>
    public DateTime? CountedAt { get; set; }

    [ForeignKey(nameof(StockCountSheetId))]
    public StockCountSheet? StockCountSheet { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }

    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }
}

