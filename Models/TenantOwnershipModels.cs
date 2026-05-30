using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

public static class TenantClaimTypes
{
    public const string OwnerPartnerId = "OwnerPartnerId";
}

public interface IOwnerScoped
{
    int? OwnerPartnerId { get; set; }
}

[Table("AppUserOwnerScopes")]
public class AppUserOwnerScope
{
    [Key]
    public long AppUserOwnerScopeId { get; set; }

    public int UserId { get; set; }

    public int OwnerPartnerId { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? RevokedAt { get; set; }

    [MaxLength(100)]
    public string? RevokedBy { get; set; }

    [ForeignKey(nameof(UserId))]
    public AppUser User { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner OwnerPartner { get; set; } = null!;
}
