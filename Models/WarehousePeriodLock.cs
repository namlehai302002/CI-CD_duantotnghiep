using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("WarehousePeriodLocks")]
public class WarehousePeriodLock
{
    [Key]
    public long WarehousePeriodLockId { get; set; }

    public int WarehouseId { get; set; }

    [Column(TypeName = "date")]
    public DateTime LockDate { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(300)]
    public string? Reason { get; set; }

    [MaxLength(100)]
    public string LockedBy { get; set; } = "";

    public DateTime LockedAt { get; set; } = VietnamTime.Now;

    [MaxLength(100)]
    public string? UnlockedBy { get; set; }

    public DateTime? UnlockedAt { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }
}

