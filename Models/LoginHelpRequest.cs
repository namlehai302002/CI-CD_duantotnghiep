using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

public enum LoginHelpRequestStatusEnum : byte
{
    New = 1,
    InReview = 2,
    Resolved = 3,
    Rejected = 4
}

public enum LoginHelpRequestReasonEnum : byte
{
    ForgotPassword = 1,
    LockedAccount = 2,
    NoAccess = 3,
    DeviceChanged = 4,
    Other = 99
}

[Table("LoginHelpRequests")]
public class LoginHelpRequest
{
    [Key]
    public long LoginHelpRequestId { get; set; }

    [Required, MaxLength(32)]
    public string RequestCode { get; set; } = "";

    [Required, MaxLength(120)]
    public string FullName { get; set; } = "";

    [Required, MaxLength(200)]
    public string LoginIdentifier { get; set; } = "";

    [MaxLength(40)]
    public string? ContactPhone { get; set; }

    [MaxLength(120)]
    public string? WarehouseOrDepartment { get; set; }

    public LoginHelpRequestReasonEnum Reason { get; set; } = LoginHelpRequestReasonEnum.ForgotPassword;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public LoginHelpRequestStatusEnum Status { get; set; } = LoginHelpRequestStatusEnum.New;

    [MaxLength(500)]
    public string? ResolutionNote { get; set; }

    [MaxLength(100)]
    public string? HandledBy { get; set; }

    public DateTime? HandledAt { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(300)]
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;
}
