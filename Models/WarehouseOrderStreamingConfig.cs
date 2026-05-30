using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("WarehouseOrderStreamingConfigs")]
public class WarehouseOrderStreamingConfig
{
    [Key]
    public int WarehouseOrderStreamingConfigId { get; set; }

    public int WarehouseId { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public int MinPriority { get; set; } = 80;

    public int DeliveryWindowHours { get; set; } = 24;

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
}
