using System.ComponentModel.DataAnnotations;
using WMS.Models;

namespace WMS.ViewModels;

public class AccessHelpRequestViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [MaxLength(120)]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập email hoặc tên đăng nhập.")]
    [MaxLength(200)]
    public string LoginIdentifier { get; set; } = "";

    [MaxLength(40)]
    public string? ContactPhone { get; set; }

    [MaxLength(120)]
    public string? WarehouseOrDepartment { get; set; }

    public LoginHelpRequestReasonEnum Reason { get; set; } = LoginHelpRequestReasonEnum.ForgotPassword;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Honeypot field: real users never see or fill it.
    public string? CompanyWebsite { get; set; }

    public string? ErrorMessage { get; set; }
}

public class LoginHelpRequestQueueViewModel
{
    public string? Search { get; set; }
    public LoginHelpRequestStatusEnum? Status { get; set; }
    public LoginHelpRequestReasonEnum? Reason { get; set; }
    public List<LoginHelpRequestAdminRow> Requests { get; set; } = new();
    public Dictionary<LoginHelpRequestStatusEnum, int> StatusCounts { get; set; } = new();
}

public class LoginHelpRequestAdminRow
{
    public LoginHelpRequest Request { get; set; } = new();
    public AppUser? MatchedUser { get; set; }
}
