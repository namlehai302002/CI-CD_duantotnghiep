using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("LoginAuditLogs")]
public class LoginAuditLog
{
    [Key]
    public long LoginAuditLogId { get; set; }

    [MaxLength(100)]
    public string? UserName { get; set; }

    public int? UserId { get; set; }

    public bool IsSuccess { get; set; }

    [MaxLength(50)]
    public string Outcome { get; set; } = "";

    [MaxLength(500)]
    public string? Reason { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(300)]
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;
}

[Table("MfaLoginChallenges")]
public class MfaLoginChallenge
{
    [Key]
    public int MfaLoginChallengeId { get; set; }

    public int UserId { get; set; }

    [Required, MaxLength(200)]
    public string UserName { get; set; } = "";

    [Required, MaxLength(200)]
    public string CodeHash { get; set; } = "";

    public DateTime ExpiresAt { get; set; }

    public int FailedAttemptCount { get; set; }

    public bool IsUsed { get; set; }

    public bool RememberMe { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(300)]
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;
}
