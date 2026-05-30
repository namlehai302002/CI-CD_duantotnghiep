using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("AppUsers")]
public class AppUser
{
    [Key]
    public int UserId { get; set; }

    [Required, MaxLength(100)]
    public string UserName { get; set; } = "";

    [Required, MaxLength(200)]
    public string FullName { get; set; } = "";

    [MaxLength(200)]
    public string? Email { get; set; }

    // P2-4: default "!" thay vì rỗng — BCrypt.Verify với hash không hợp lệ sẽ luôn fail,
    // chặn login nếu seed/migration quên set PasswordHash thực.
    [Required, MaxLength(500)]
    public string PasswordHash { get; set; } = "!";

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    public int? WarehouseId { get; set; }

    public int RoleId { get; set; } = 3;

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    /// <summary>Brute-force protection: consecutive failed login count</summary>
    public int FailedLoginCount { get; set; }

    /// <summary>Account locked until this UTC time after too many failed attempts</summary>
    public DateTime? LockoutEnd { get; set; }

    /// <summary>Trusted-device revocation watermark (UTC). Any token issued before this time is invalid.</summary>
    public DateTime? TrustedDeviceRevokedAtUtc { get; set; }

    /// <summary>Whether 2FA (TOTP) is enabled for this user</summary>
    public bool IsMfaEnabled { get; set; }

    /// <summary>TOTP shared secret (base32) for authenticator apps</summary>
    [MaxLength(200)]
    public string? MfaSecretKey { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [ForeignKey("WarehouseId")]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey("RoleId")]
    public AppRole Role { get; set; } = null!;

    public ICollection<AppUserOwnerScope> OwnerScopes { get; set; } = new List<AppUserOwnerScope>();
}

[Table("AppRoles")]
public class AppRole
{
    [Key]
    public int RoleId { get; set; }

    [Required, MaxLength(50)]
    public string RoleName { get; set; } = "";

    [MaxLength(200)]
    public string? Description { get; set; }
}
