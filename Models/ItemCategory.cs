using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("ItemCategories")]
public class ItemCategory
{
    [Key]
    public int CategoryId { get; set; }

    [Required, MaxLength(20)]
    public string CategoryCode { get; set; } = "";

    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = "";

    public int? ParentCategoryId { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("ParentCategoryId")]
    public ItemCategory? ParentCategory { get; set; }

    public ICollection<ItemCategory> ChildCategories { get; set; } = new List<ItemCategory>();

    public ICollection<Item> Items { get; set; } = new List<Item>();
}
