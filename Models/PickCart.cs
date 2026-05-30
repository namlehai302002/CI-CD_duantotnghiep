using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("PickCarts")]
public class PickCart
{
    [Key]
    public int PickCartId { get; set; }

    [Required, MaxLength(30)]
    public string CartCode { get; set; } = "";

    public int WarehouseId { get; set; }

    public PickCartStatusEnum Status { get; set; } = PickCartStatusEnum.Available;

    /// <summary>Số slot tote tối đa trên xe (ví dụ: 4, 6, 8)</summary>
    public int ToteCapacity { get; set; } = 6;

    [MaxLength(200)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    public ICollection<PickTote> Totes { get; set; } = new List<PickTote>();
}
