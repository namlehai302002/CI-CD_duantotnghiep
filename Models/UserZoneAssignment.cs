using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("UserZoneAssignments")]
public class UserZoneAssignment
{
    [Key]
    public int UserZoneAssignmentId { get; set; }

    public int UserId { get; set; }

    public int ZoneId { get; set; }

    [MaxLength(100)]
    public string? AssignedBy { get; set; }

    public DateTime AssignedAt { get; set; } = VietnamTime.Now;

    [ForeignKey(nameof(UserId))]
    public AppUser? User { get; set; }

    [ForeignKey(nameof(ZoneId))]
    public Zone? Zone { get; set; }
}
