using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("Warehouses")]
public class Warehouse
{
    [Key]
    public int WarehouseId { get; set; }

    [Required, MaxLength(20)]
    public string WarehouseCode { get; set; } = "";

    [Required, MaxLength(100)]
    public string WarehouseName { get; set; } = "";

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? ManagerName { get; set; }

    public int? ManagerUserId { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public AppUser? ManagerUser { get; set; }

    public ICollection<Zone> Zones { get; set; } = new List<Zone>();
}
