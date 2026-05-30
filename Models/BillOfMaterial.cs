using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("BillOfMaterials")]
public class BillOfMaterial
{
    [Key]
    public int BomId { get; set; }

    public int ParentItemId { get; set; }

    public int ChildItemId { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal Quantity { get; set; }

    public int UomId { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal ScrapPercent { get; set; }

    public int BomLevel { get; set; } = 1;

    public DateTime? EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [ForeignKey("ParentItemId")]
    public Item? ParentItem { get; set; }

    [ForeignKey("ChildItemId")]
    public Item? ChildItem { get; set; }

    [ForeignKey("UomId")]
    public UnitOfMeasure? Uom { get; set; }
}
