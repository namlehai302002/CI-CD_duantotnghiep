using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using WMS.Controllers;
using WMS.Data;
using WMS.Models;
using WMS.ViewModels;

namespace WMS.Tests;

public sealed class LoginHelpRequestTests
{
    [Fact]
    public async Task AccessHelp_ShouldCreateRequestWithoutDisclosingAccountExistence()
    {
        await using var db = CreateDb(nameof(AccessHelp_ShouldCreateRequestWithoutDisclosingAccountExistence));
        var controller = CreateAccountController(db);

        var result = await controller.AccessHelp(new AccessHelpRequestViewModel
        {
            FullName = "Nguyen Van A",
            LoginIdentifier = "nva",
            ContactPhone = "0900000000",
            WarehouseOrDepartment = "Kho A",
            Reason = LoginHelpRequestReasonEnum.LockedAccount,
            Notes = "Can ho tro trong ca sang"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AccessHelpSent", redirect.ActionName);

        var request = await db.LoginHelpRequests.SingleAsync();
        Assert.StartsWith("AHR-", request.RequestCode, StringComparison.Ordinal);
        Assert.Equal("nva", request.LoginIdentifier);
        Assert.Equal(LoginHelpRequestStatusEnum.New, request.Status);
        Assert.Equal(LoginHelpRequestReasonEnum.LockedAccount, request.Reason);
        Assert.True(await db.LoginAuditLogs.AnyAsync(x => x.Outcome == "LOGIN_HELP_REQUEST_CREATED"));
    }

    [Fact]
    public async Task AccessHelp_HoneypotShouldReturnSuccessWithoutCreatingRequest()
    {
        await using var db = CreateDb(nameof(AccessHelp_HoneypotShouldReturnSuccessWithoutCreatingRequest));
        var controller = CreateAccountController(db);

        var result = await controller.AccessHelp(new AccessHelpRequestViewModel
        {
            FullName = "Nguyen Van A",
            LoginIdentifier = "nva",
            CompanyWebsite = "https://spam.example"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AccessHelpSent", redirect.ActionName);
        Assert.Empty(await db.LoginHelpRequests.ToListAsync());
    }

    [Fact]
    public async Task ResolveLoginHelpRequest_ShouldResetPasswordAndWriteAudit()
    {
        await using var db = CreateDb(nameof(ResolveLoginHelpRequest_ShouldResetPasswordAndWriteAudit));
        db.AppRoles.Add(new AppRole { RoleId = 1, RoleName = "Admin" });
        db.AppUsers.Add(new AppUser
        {
            UserId = 7,
            UserName = "staff01",
            FullName = "Staff 01",
            Email = "staff01@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass@123"),
            RoleId = 1,
            FailedLoginCount = 4,
            LockoutEnd = DateTime.UtcNow.AddMinutes(10)
        });
        db.LoginHelpRequests.Add(new LoginHelpRequest
        {
            LoginHelpRequestId = 9,
            RequestCode = "AHR-260517-1234",
            FullName = "Staff 01",
            LoginIdentifier = "staff01",
            Reason = LoginHelpRequestReasonEnum.ForgotPassword,
            Status = LoginHelpRequestStatusEnum.InReview,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var controller = CreateUsersController(db);
        var result = await controller.ResolveLoginHelpRequest(9, 7, "NewPass@123", "Da ban giao mat khau tam");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("LoginHelpRequests", redirect.ActionName);

        var user = await db.AppUsers.SingleAsync(x => x.UserId == 7);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPass@123", user.PasswordHash));
        Assert.Equal(0, user.FailedLoginCount);
        Assert.Null(user.LockoutEnd);

        var request = await db.LoginHelpRequests.SingleAsync(x => x.LoginHelpRequestId == 9);
        Assert.Equal(LoginHelpRequestStatusEnum.Resolved, request.Status);
        Assert.Equal("admin", request.HandledBy);
        Assert.NotNull(request.HandledAt);
        Assert.True(await db.LoginAuditLogs.AnyAsync(x => x.Outcome == "PASSWORD_RESET_FROM_LOGIN_HELP"));
        Assert.True(await db.LoginAuditLogs.AnyAsync(x => x.Outcome == "LOGIN_HELP_REQUEST_RESOLVED"));
    }

    [Fact]
    public void LoginHelpUi_ShouldExposeEnterpriseAccessHelpWithoutSecretsOrPasswordField()
    {
        var root = FindRepositoryRoot();
        var login = File.ReadAllText(Path.Combine(root, "Views", "Account", "Login.cshtml"));
        var accessHelp = File.ReadAllText(Path.Combine(root, "Views", "Account", "AccessHelp.cshtml"));
        var queue = File.ReadAllText(Path.Combine(root, "Views", "Users", "LoginHelpRequests.cshtml"));
        var layout = File.ReadAllText(Path.Combine(root, "Views", "Shared", "_Layout.cshtml"));
        var css = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "site.css"));

        Assert.Contains("asp-action=\"AccessHelp\"", login, StringComparison.Ordinal);
        Assert.Contains("access-help-cta", login, StringComparison.Ordinal);
        Assert.Contains("Gửi yêu cầu để quản trị kho hoặc bộ phận công nghệ thông tin kiểm tra và liên hệ lại.", login, StringComparison.Ordinal);
        Assert.Contains("enterprise-auth-help-button", login, StringComparison.Ordinal);
        Assert.Contains("Không truy cập được tài khoản?", accessHelp, StringComparison.Ordinal);
        Assert.Contains("name=\"CompanyWebsite\"", accessHelp, StringComparison.Ordinal);
        Assert.Contains("asp-antiforgery=\"true\"", accessHelp, StringComparison.Ordinal);
        Assert.DoesNotContain("type=\"password\"", accessHelp, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hieuctttb01413", login + accessHelp, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("0347681019", login + accessHelp, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Yêu cầu truy cập", layout, StringComparison.Ordinal);
        Assert.Contains("LoginHelpRequests", queue + layout, StringComparison.Ordinal);
        Assert.Contains(".access-help-trap", css, StringComparison.Ordinal);
        Assert.Contains(".enterprise-auth-help-button", css, StringComparison.Ordinal);
        Assert.Contains(".login-help-table", css, StringComparison.Ordinal);
    }

    private static AppDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"{name}-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options) { SkipAudit = true };
    }

    private static AccountController CreateAccountController(AppDbContext db)
    {
        var configuration = new ConfigurationBuilder().Build();
        var dataProtection = DataProtectionProvider.Create(new DirectoryInfo(Path.GetTempPath()));
        var controller = new AccountController(db, new TestWebHostEnvironment(), configuration, dataProtection);
        AttachHttp(controller, anonymous: true);
        return controller;
    }

    private static UsersController CreateUsersController(AppDbContext db)
    {
        var controller = new UsersController(db);
        AttachHttp(controller, anonymous: false);
        return controller;
    }

    private static void AttachHttp(Controller controller, bool anonymous)
    {
        var identity = anonymous
            ? new ClaimsIdentity()
            : new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "TestAuth");

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
        httpContext.Request.Headers.UserAgent = "WMS.Tests";

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "WMS.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "WMS.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("Cannot find repository root.");
    }
}
