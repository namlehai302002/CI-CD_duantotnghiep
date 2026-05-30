using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("UnitsOfMeasure")]
public class UnitOfMeasure
{
    [Key]
    public int UomId { get; set; }

    [Required, MaxLength(10)]
    public string UomCode { get; set; } = "";

    [Required, MaxLength(50)]
    public string UomName { get; set; } = "";

    [MaxLength(50)]
    public string? UomGroup { get; set; }

    public bool IsActive { get; set; } = true;
}
