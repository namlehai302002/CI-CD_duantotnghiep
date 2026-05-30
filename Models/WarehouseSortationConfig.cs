using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("WarehouseSortationConfigs")]
public class WarehouseSortationConfig
{
    [Key]
    public int WarehouseSortationConfigId { get; set; }

    public int WarehouseId { get; set; }

    public int StagingLocationId { get; set; }

    public int SortationLocationId { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(300)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(StagingLocationId))]
    public Location StagingLocation { get; set; } = null!;

    [ForeignKey(nameof(SortationLocationId))]
    public Location SortationLocation { get; set; } = null!;
}
