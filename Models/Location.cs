using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models;

[Table("Locations")]
public class Location
{
    [Key]
    public int LocationId { get; set; }

    public int ZoneId { get; set; }

    [Required, MaxLength(50)]
    public string LocationCode { get; set; } = "";

    /// <summary>Mã lối đi (Aisle) — SAP: Zone → Aisle → Rack → Shelf → Bin</summary>
    [MaxLength(20)]
    public string? AisleCode { get; set; }

    /// <summary>Thứ tự lối đi — dùng sort pick path (S-shape/Z-shape)</summary>
    public int AisleSequence { get; set; } = 0;

    [MaxLength(20)]
    public string? RackCode { get; set; }

    [MaxLength(20)]
    public string? ShelfCode { get; set; }

    [MaxLength(20)]
    public string? BinCode { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal CurrentLoad { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal MaxCapacity { get; set; } = 999999m;

    [Column(TypeName = "decimal(18,4)")]
    public decimal? MaxWeightCapacityKg { get; set; }

    public int HeightLevel { get; set; } = 1;

    public bool IsGoldenZone { get; set; }

    public bool AllowMechanicalHandling { get; set; }

    public bool AllowMixedSku { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? WeightLimitKg { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string? Barcode { get; set; }

    [ForeignKey("ZoneId")]
    public Zone Zone { get; set; } = null!;

    public virtual ICollection<ItemLocation> ItemLocations { get; set; } = new List<ItemLocation>();
}
