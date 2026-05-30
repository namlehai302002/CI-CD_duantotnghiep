using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("ItemLocations")]
public class ItemLocation : IOwnerScoped
{
    [Key]
    public int ItemLocationId { get; set; }

    public int ItemId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public int LocationId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ReservedQty { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? MaxCapacity { get; set; }

    [MaxLength(50)]
    public string? LotNumber { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? TotalCapacity { get; set; }

    /// <summary>Trạng thái chất lượng tồn kho tại vị trí (Available/QcHold/Quarantine/Damaged/Expired)</summary>
    public InventoryHoldStatusEnum HoldStatus { get; set; } = InventoryHoldStatusEnum.Available;

    public DateTime UpdatedAt { get; set; } = VietnamTime.Now;

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [NotMapped]
    public decimal AvailableQty => Quantity - ReservedQty;

    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    [ForeignKey("LocationId")]
    public Location? Location { get; set; }
}
