using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("UnitConversions")]
public class UnitConversion
{
    [Key]
    public int ConversionId { get; set; }

    public int? ItemId { get; set; }

    public int FromUomId { get; set; }

    public int ToUomId { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal ConversionRate { get; set; }

    public bool IsActive { get; set; } = true;

    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [ForeignKey("FromUomId")]
    public UnitOfMeasure? FromUom { get; set; }

    [ForeignKey("ToUomId")]
    public UnitOfMeasure? ToUom { get; set; }
}
