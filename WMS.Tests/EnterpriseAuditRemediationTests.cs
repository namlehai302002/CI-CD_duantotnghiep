using System.Text;

namespace WMS.Tests;

public sealed class EnterpriseAuditRemediationTests
{
    [Fact]
    public void DocumentAndYardEvidenceUploads_ShouldUsePrivateStorageAndGuardedDownload()
    {
        var root = FindRepositoryRoot();
        var voucherImport = Read(Path.Combine(root, "Controllers", "VouchersController.Import.cs"));
        var yardEnterprise = Read(Path.Combine(root, "Controllers", "OperationsController.Enterprise567.cs"));
        var yardView = Read(Path.Combine(root, "Views", "Operations", "YardManagement.cshtml"));

        Assert.DoesNotContain("\"wwwroot\", \"uploads\", \"receipts\"", voucherImport, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/uploads/receipts/", voucherImport, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("App_Data\", \"uploads\", \"document-intake-legacy", voucherImport, StringComparison.Ordinal);
        Assert.Contains("DownloadReceiptDocument", voucherImport, StringComparison.Ordinal);
        Assert.Contains("ResolvePrivateReceiptPath", voucherImport, StringComparison.Ordinal);

        Assert.DoesNotContain("\"wwwroot\", \"uploads\", \"yard-evidence\"", yardEnterprise, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/uploads/yard-evidence/", yardEnterprise, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("App_Data\", \"uploads\", \"yard-evidence", yardEnterprise, StringComparison.Ordinal);
        Assert.Contains("DownloadYardVisitEvidence", yardEnterprise, StringComparison.Ordinal);
        Assert.Contains("ResolvePrivateYardEvidencePath", yardEnterprise, StringComparison.Ordinal);
        Assert.Contains("DownloadYardVisitEvidence", yardView, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"@e.FileUrl\"", yardView, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FirstAdminBootstrap_ShouldRequireOneTimeTokenOutsideDevelopment()
    {
        var root = FindRepositoryRoot();
        var accountController = Read(Path.Combine(root, "Controllers", "AccountController.cs"));
        var setupView = Read(Path.Combine(root, "Views", "Account", "SetupAdmin.cshtml"));

        Assert.Contains("IsFirstAdminBootstrapAllowed", accountController, StringComparison.Ordinal);
        Assert.Contains("WMS_FIRST_ADMIN_BOOTSTRAP_TOKEN", accountController, StringComparison.Ordinal);
        Assert.Contains("CryptographicOperations.FixedTimeEquals", accountController, StringComparison.Ordinal);
        Assert.Contains("bootstrapToken", setupView, StringComparison.Ordinal);
    }

    [Fact]
    public void DemoWordingAndEnglishAmountFinding_ShouldBeCleaned()
    {
        var root = FindRepositoryRoot();
        var voucherImport = Read(Path.Combine(root, "Controllers", "VouchersController.Import.cs"));
        var yardBilling = Read(Path.Combine(root, "Views", "Operations", "YardBillingCharges.cshtml"));
        var roleScriptPath = Path.Combine(root, "scripts", "ROLE_ACCESS_CHECK.ps1");

        Assert.False(File.Exists(Path.Combine(root, "scripts", "DEMO_ROLE_CHECK.ps1")));
        Assert.True(File.Exists(roleScriptPath));

        var roleScript = Read(roleScriptPath);
        Assert.DoesNotContain("local" + "host", roleScript, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("demo", roleScript, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DownloadDemoImport100", voucherImport, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Demo Item", voucherImport, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DEMO-", voucherImport, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DownloadSampleImport100", voucherImport, StringComparison.Ordinal);
        Assert.DoesNotContain(">Amount<", yardBilling, StringComparison.Ordinal);
        Assert.Contains("Số tiền", yardBilling, StringComparison.Ordinal);
    }

    [Fact]
    public void VisualRegression_ShouldHaveRunnableAuthStateScaffold()
    {
        var root = FindRepositoryRoot();
        var packageJson = Read(Path.Combine(root, "package.json"));
        var publicConfig = Read(Path.Combine(root, "tests", "visual", "playwright.public.config.ts"));
        var publicSpec = Read(Path.Combine(root, "tests", "visual", "wms-public-auth.spec.ts"));
        var visualConfig = Read(Path.Combine(root, "tests", "visual", "playwright.config.ts"));
        var authConfig = Read(Path.Combine(root, "tests", "visual", "playwright.auth.config.ts"));
        var authSetup = Read(Path.Combine(root, "tests", "visual", "auth.setup.ts"));
        var readme = Read(Path.Combine(root, "tests", "visual", "README.md"));

        Assert.Contains("visual:public", packageJson, StringComparison.Ordinal);
        Assert.Contains("visual:auth", packageJson, StringComparison.Ordinal);
        Assert.Contains("visual:test", packageJson, StringComparison.Ordinal);
        Assert.Contains("@playwright/test", packageJson, StringComparison.Ordinal);
        Assert.Contains("wms-public-auth.spec.ts", publicConfig, StringComparison.Ordinal);
        Assert.Contains("artifacts", publicSpec, StringComparison.Ordinal);
        Assert.Contains("AccessHelp", publicSpec, StringComparison.Ordinal);
        Assert.Contains("existsSync", visualConfig, StringComparison.Ordinal);
        Assert.Contains("tests/visual/.auth/wms-auth-state.json", visualConfig, StringComparison.Ordinal);
        Assert.Contains("WMS_TEST_USER", authSetup, StringComparison.Ordinal);
        Assert.Contains("WMS_TEST_PASSWORD", authSetup, StringComparison.Ordinal);
        Assert.Contains("isLoopbackBaseUrl", authSetup, StringComparison.Ordinal);
        Assert.Contains("LocalVerification fallback is allowed only when WMS_BASE_URL is a loopback URL.", authSetup, StringComparison.Ordinal);
        Assert.Contains("MFA", authSetup, StringComparison.Ordinal);
        Assert.Contains("auth\\.setup\\.ts", authConfig, StringComparison.Ordinal);
        Assert.Contains("npm run visual:auth", readme, StringComparison.Ordinal);
        Assert.Contains("npm run visual:test", readme, StringComparison.Ordinal);
    }


    [Fact]
    public void SecurityHardening_ShouldUseConstantTimeApiKeyValidation()
    {
        var root = FindRepositoryRoot();
        var apiController = Read(Path.Combine(root, "Controllers", "ApiIntegrationController.cs"));

        Assert.Contains("using System.Security.Cryptography;", apiController, StringComparison.Ordinal);
        Assert.Contains("using System.Text;", apiController, StringComparison.Ordinal);
        Assert.Contains("_config[\"Api:Key\"]", apiController, StringComparison.Ordinal);
        Assert.Contains("Request.Headers[\"X-API-Key\"]", apiController, StringComparison.Ordinal);
        Assert.Contains("SHA256.HashData", apiController, StringComparison.Ordinal);
        Assert.Contains("CryptographicOperations.FixedTimeEquals", apiController, StringComparison.Ordinal);
        Assert.DoesNotContain("headerKey == configKey", apiController, StringComparison.Ordinal);
    }

    [Fact]
    public void SecurityHardening_ShouldKeepTelemetryProductionSafe()
    {
        var root = FindRepositoryRoot();
        var program = Read(Path.Combine(root, "Program.cs"));

        Assert.Contains("options.SetDbStatementForText = builder.Environment.IsDevelopment();", program, StringComparison.Ordinal);
        Assert.DoesNotContain("options.SetDbStatementForText = true;", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddConsoleExporter", program, StringComparison.Ordinal);
        Assert.Contains("AddOtlpExporter", program, StringComparison.Ordinal);
        Assert.Contains("Uri.TryCreate(otelEndpoint, UriKind.Absolute", program, StringComparison.Ordinal);
    }

    [Fact]
    public void AuditLogActionType_ShouldSupportEnterpriseActionCodes()
    {
        var root = FindRepositoryRoot();
        var model = Read(Path.Combine(root, "Models", "AuditModels.cs"));
        var snapshot = Read(Path.Combine(root, "Migrations", "AppDbContextModelSnapshot.cs"));
        var migration = Read(Directory.GetFiles(Path.Combine(root, "Migrations"), "*WidenAuditLogActionType.cs").Single());
        var crossDock = Read(Path.Combine(root, "Services", "CrossDockService.cs"));
        var outbound = Read(Path.Combine(root, "Services", "OutboundExecutionService.cs"));
        var movement = Read(Path.Combine(root, "Services", "MovementTaskService.cs"));

        Assert.Contains("[Required, MaxLength(64)]", model, StringComparison.Ordinal);
        Assert.Contains("type: \"nvarchar(64)\"", migration, StringComparison.Ordinal);
        Assert.Contains("oldType: \"nvarchar(10)\"", migration, StringComparison.Ordinal);

        var auditLogSnapshot = SliceFrom(snapshot, "modelBuilder.Entity(\"WMS.Models.AuditLog\"", "b.Property<string>(\"AppModule\")");
        Assert.Contains(".HasMaxLength(64)", auditLogSnapshot, StringComparison.Ordinal);
        Assert.Contains(".HasColumnType(\"nvarchar(64)\")", auditLogSnapshot, StringComparison.Ordinal);
        Assert.DoesNotContain(".HasMaxLength(10)", auditLogSnapshot, StringComparison.Ordinal);
        Assert.DoesNotContain(".HasColumnType(\"nvarchar(10)\")", auditLogSnapshot, StringComparison.Ordinal);

        Assert.Contains("ActionType = \"CROSSDOCK_TASK_CREATED\"", crossDock, StringComparison.Ordinal);
        Assert.Contains("ActionType = \"CROSSDOCK_TASK_COMPLETED\"", crossDock, StringComparison.Ordinal);
        Assert.Contains("ActionType = \"SHORT_PICK_REALLOCATION\"", outbound, StringComparison.Ordinal);
        Assert.Contains("ActionType = \"PARTIAL_SHIPMENT_BACKORDER\"", outbound, StringComparison.Ordinal);
        Assert.Contains("ActionType = \"LPN_MOVE_COMPLETE\"", movement, StringComparison.Ordinal);
    }

    [Fact]
    public void SecurityHardening_ShouldProtectMfaAndLogoutFlow()
    {
        var root = FindRepositoryRoot();
        var accountController = Read(Path.Combine(root, "Controllers", "AccountController.cs"));

        var verifyMfaAttributes = AttributeBlockBefore(accountController, "public async Task<IActionResult> VerifyMfa(VerifyMfaViewModel model)");
        Assert.Contains("[ValidateAntiForgeryToken]", verifyMfaAttributes, StringComparison.Ordinal);
        Assert.DoesNotContain("[IgnoreAntiforgeryToken]", verifyMfaAttributes, StringComparison.Ordinal);

        var logoutAudit = SliceFrom(accountController, "private async Task WriteLogoutAuditAsync()", "private static string TrimTo");
        Assert.Contains("_db.AuditLogs.Add(new AuditLog", logoutAudit, StringComparison.Ordinal);
        Assert.Contains("TableName = \"Authentication\"", logoutAudit, StringComparison.Ordinal);
        Assert.Contains("ActionType = \"LOGOUT\"", logoutAudit, StringComparison.Ordinal);
        Assert.Contains("User.FindFirstValue(ClaimTypes.NameIdentifier)", logoutAudit, StringComparison.Ordinal);
        Assert.Contains("GetUserAgent()", logoutAudit, StringComparison.Ordinal);
        Assert.Contains("SessionId", logoutAudit, StringComparison.Ordinal);

        var logoutAction = SliceFrom(accountController, "public async Task<IActionResult> Logout()", "[HttpGet]");
        Assert.Contains("await WriteLogoutAuditAsync();", logoutAction, StringComparison.Ordinal);
        Assert.Contains("HttpContext.SignOutAsync", logoutAction, StringComparison.Ordinal);
        Assert.True(
            logoutAction.IndexOf("await WriteLogoutAuditAsync();", StringComparison.Ordinal)
            < logoutAction.IndexOf("HttpContext.SignOutAsync", StringComparison.Ordinal));
        Assert.Contains("Response.Cookies.Delete(TrustedDeviceCookieName", logoutAction, StringComparison.Ordinal);
        Assert.Contains("CookieAuthenticationDefaults.CookiePrefix + CookieAuthenticationDefaults.AuthenticationScheme", logoutAction, StringComparison.Ordinal);
    }

    [Fact]
    public void SecurityHardening_ShouldIgnoreGeneratedAndRuntimeOutputs()
    {
        var root = FindRepositoryRoot();
        var gitignore = Read(Path.Combine(root, ".gitignore"));

        foreach (var token in new[]
        {
            "bin/",
            "obj/",
            "node_modules/",
            "artifacts/",
            "*.log",
            "App_Data/uploads/",
            "publish/",
            "test-results/",
            "TestResults/"
        })
        {
            Assert.Contains(token, gitignore, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SecurityHardening_ShouldKeepAuthViewsVietnameseAndEnterpriseTone()
    {
        var root = FindRepositoryRoot();
        var login = Read(Path.Combine(root, "Views", "Account", "Login.cshtml"));
        var verifyMfa = Read(Path.Combine(root, "Views", "Account", "VerifyMfa.cshtml"));
        var devReset = Read(Path.Combine(root, "Views", "Account", "DevResetPassword.cshtml"));
        var register = Read(Path.Combine(root, "Views", "Account", "Register.cshtml"));
        var setupAdmin = Read(Path.Combine(root, "Views", "Account", "SetupAdmin.cshtml"));
        var accessHelp = Read(Path.Combine(root, "Views", "Account", "AccessHelp.cshtml"));
        var accessHelpSent = Read(Path.Combine(root, "Views", "Account", "AccessHelpSent.cshtml"));
        var allAccountViews = string.Join("\n", Directory.GetFiles(Path.Combine(root, "Views", "Account"), "*.cshtml").Select(Read));

        Assert.Contains("<h2>WMS Pro</h2>", login, StringComparison.Ordinal);
        Assert.Contains("Email Hoặc Tên đăng nhập", login, StringComparison.Ordinal);
        Assert.Contains("quản trị kho hoặc bộ phận công nghệ thông tin", login, StringComparison.Ordinal);
        Assert.DoesNotContain("Warehouse Management System", login, StringComparison.Ordinal);
        Assert.Contains("Xác thực mã bảo mật", verifyMfa, StringComparison.Ordinal);
        Assert.Contains("Mã xác thực (6 số)", verifyMfa, StringComparison.Ordinal);
        Assert.DoesNotContain("Xác thực Captcha", verifyMfa, StringComparison.Ordinal);
        Assert.DoesNotContain("Mã Captcha", verifyMfa, StringComparison.Ordinal);
        Assert.DoesNotContain("production", devReset, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("môi trường vận hành thật", devReset, StringComparison.Ordinal);
        Assert.Contains("Đăng ký tài khoản WMS Pro", register, StringComparison.Ordinal);
        Assert.Contains("quantri@congty.vn", setupAdmin, StringComparison.Ordinal);
        Assert.Contains("quantri@congty.vn", devReset, StringComparison.Ordinal);
        Assert.Contains("Email Hoặc Tên đăng nhập", accessHelp, StringComparison.Ordinal);
        Assert.Contains("quản trị kho hoặc bộ phận công nghệ thông tin", accessHelp, StringComparison.Ordinal);
        Assert.Contains("quản trị kho hoặc bộ phận công nghệ thông tin", accessHelpSent, StringComparison.Ordinal);
        Assert.DoesNotContain("kho/IT", allAccountViews, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("company.com", allAccountViews, StringComparison.OrdinalIgnoreCase);
        foreach (var mojibake in new[]
        {
            TextFromCodePoints(0x00C3),
            TextFromCodePoints(0x00C4),
            TextFromCodePoints(0x00E1, 0x00BA),
            TextFromCodePoints(0x00E1, 0x00BB),
            TextFromCodePoints(0x00C6),
            TextFromCodePoints(0x00E2, 0x20AC)
        })
        {
            Assert.DoesNotContain(mojibake, allAccountViews, StringComparison.Ordinal);
        }
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "WMS.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("Cannot find repository root.");
    }

    private static string Read(string path) => File.ReadAllText(path);

    private static string TextFromCodePoints(params int[] codePoints)
    {
        var builder = new StringBuilder();
        foreach (var codePoint in codePoints)
            builder.Append(char.ConvertFromUtf32(codePoint));
        return builder.ToString();
    }

    private static string AttributeBlockBefore(string content, string methodSignature)
    {
        var methodIndex = content.IndexOf(methodSignature, StringComparison.Ordinal);
        Assert.True(methodIndex >= 0, $"Cannot find method signature: {methodSignature}");

        var blockStart = content.LastIndexOf("[HttpPost]", methodIndex, StringComparison.Ordinal);
        Assert.True(blockStart >= 0, $"Cannot find HttpPost attribute before: {methodSignature}");

        return content.Substring(blockStart, methodIndex - blockStart);
    }

    private static string SliceFrom(string content, string start, string end)
    {
        var startIndex = content.IndexOf(start, StringComparison.Ordinal);
        Assert.True(startIndex >= 0, $"Cannot find start token: {start}");

        var endIndex = content.IndexOf(end, startIndex + start.Length, StringComparison.Ordinal);
        return endIndex >= 0 ? content.Substring(startIndex, endIndex - startIndex) : content[startIndex..];
    }
}
