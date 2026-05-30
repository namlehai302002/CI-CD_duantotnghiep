using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("PackagingUnits")]
public class PackagingUnit
{
    [Key]
    public int PackagingUnitId { get; set; }

    [Required, MaxLength(100)]
    public string TenDongGoi { get; set; } = ""; // VD: "Thùng 50L", "Hộp 1000 cái"

    public int BaseUomId { get; set; } // FK → UnitOfMeasure (đơn vị gốc: L, Cái, ...)

    [Column(TypeName = "decimal(18,4)")]
    public decimal GiaTri { get; set; } // Số lượng đơn vị gốc, VD: 50 (lít), 1000 (cái)

    public bool IsActive { get; set; } = true;

    [ForeignKey("BaseUomId")]
    public UnitOfMeasure? BaseUom { get; set; }
}
