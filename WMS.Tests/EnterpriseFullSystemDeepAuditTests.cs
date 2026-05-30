using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using WMS.Controllers;

namespace WMS.Tests;

public sealed class EnterpriseFullSystemDeepAuditTests
{
    [Fact]
    public void DeepAuditReport_ShouldExistAndMatchAuditedFileInventory()
    {
        var root = FindRepositoryRoot();
        var report = Read(Path.Combine(root, "ENTERPRISE_FULL_SYSTEM_DEEP_AUDIT_2026_05_13.md"));

        var expectedCounts = CountAuditedFiles(root);
        foreach (var pair in expectedCounts)
            Assert.Contains($"| {pair.Key} | {pair.Value} |", report, StringComparison.Ordinal);

        Assert.Contains("| Critical | 0 |", report, StringComparison.Ordinal);
        Assert.Contains("appsettings", report, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secret", report, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Backlog", report, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Appsettings_ShouldContainRequiredSectionsSafeDefaultsAndSensitiveClassification()
    {
        var root = FindRepositoryRoot();
        using var document = JsonDocument.Parse(Read(Path.Combine(root, "appsettings.json")));
        var json = document.RootElement;

        foreach (var path in new[]
        {
            "ConnectionStrings.DefaultConnection",
            "Api.Key",
            "Auth.Smtp.Host",
            "Auth.Smtp.Port",
            "Auth.Smtp.User",
            "Auth.Smtp.Pass",
            "Auth.Smtp.From",
            "Auth.Smtp.UseSsl",
            "Auth.Smtp.DisplayName",
            "Auth.AllowPublicRegistration",
            "System.AllowFirstAdminBootstrap",
            "System.AllowDangerOps",
            "MinerU.Enabled",
            "MinerU.BaseUrl",
            "MinerU.TimeoutSeconds",
            "MinerU.MaxFileSizeMb",
            "MinerU.AllowLegacyFallback",
            "InventoryConsistency.OutboxIntervalSeconds",
            "ReplenishmentAutomation.Enabled",
            "WarehouseDefaults.HsdThresholdDays",
            "MheIntegration.Enabled",
            "OpenTelemetry.ServiceName",
            "ReverseProxy.KnownProxies",
            "AnalyticsGovernance.PredictiveAlertHorizonDays",
            "RoleWorkspace.EnableRoleHomeCards",
            "ProductionSre.TelemetrySamplingPercent"
        })
        {
            Assert.True(HasPath(json, path), $"Missing appsettings key path `{path}`.");
        }

        Assert.True(IsFalse(json, "Auth.AllowPublicRegistration"));
        Assert.True(IsFalse(json, "System.AllowDangerOps"));
        Assert.True(IsBoolean(json, "MinerU.Enabled"));
        Assert.True(IsFalse(json, "MinerU.AllowLegacyFallback"));
        Assert.Contains("MINERU_LOOPBACK_PRODUCTION_WARNING", Read(Path.Combine(root, "Program.cs")), StringComparison.Ordinal);

        var report = Read(Path.Combine(root, "ENTERPRISE_FULL_SYSTEM_DEEP_AUDIT_2026_05_13.md"));
        foreach (var sensitive in new[]
        {
            "ConnectionStrings.DefaultConnection",
            "Api.Key",
            "Auth.Smtp.Pass",
            "GroqApiKey",
            "GeminiApiKey",
            "DevResetToken"
        })
        {
            Assert.Contains(sensitive, report, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("ScopedWarehouseId is required", report, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OperationalSource_ShouldHaveNoOpenCriticalPatterns()
    {
        var root = FindRepositoryRoot();
        var files = OperationalRoots
            .Select(path => Path.Combine(root, path))
            .Where(Directory.Exists)
            .SelectMany(path => EnumerateFiles(path, "*.*"))
            .Where(IsOperationalTextFile)
            .ToList();

        var failures = new List<string>();
        foreach (var file in files)
        {
            var relative = ToRelativePath(root, file);
            var content = Read(file);

            foreach (var banned in new[] { "NotImplementedException", "throw new NotSupportedException", "async void", "local" + "host", "N/A", "File bàn giao", "HUONG_DAN_THUC_HANH_WMS_CHI_TIET.md" })
            {
                if (content.Contains(banned, StringComparison.OrdinalIgnoreCase))
                    failures.Add($"{relative} contains `{banned}`.");
            }

            if (Regex.IsMatch(content, @"\.Result(?![A-Za-z0-9_])"))
                failures.Add($"{relative} contains blocking `.Result`.");
            if (Regex.IsMatch(content, @"\.Wait\s*\("))
                failures.Add($"{relative} contains blocking `.Wait(`.");

            if ((relative.StartsWith("Views/", StringComparison.OrdinalIgnoreCase)
                    || relative.StartsWith("wwwroot/js/", StringComparison.OrdinalIgnoreCase)
                    || relative.StartsWith("wwwroot/css/", StringComparison.OrdinalIgnoreCase))
                && content.Contains("demo", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{relative} contains demo text in user-facing source.");
            }
        }

        Assert.True(failures.Count == 0, "Critical deep-audit source findings:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void Controllers_ShouldProtectExportsReportsAndSensitivePosts()
    {
        var root = FindRepositoryRoot();
        var program = Read(Path.Combine(root, "Program.cs"));

        Assert.Contains("AuthorizeFilter(policy)", program, StringComparison.Ordinal);
        Assert.Contains("AutoValidateAntiforgeryTokenAttribute", program, StringComparison.Ordinal);

        var failures = new List<string>();
        foreach (var controller in typeof(HomeController).Assembly.GetTypes().Where(t => typeof(ControllerBase).IsAssignableFrom(t)))
        {
            foreach (var method in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(IsActionMethod))
            {
                var isAnonymous = method.GetCustomAttribute<AllowAnonymousAttribute>() != null
                    || controller.GetCustomAttribute<AllowAnonymousAttribute>() != null;
                var isExportLike = method.Name.Contains("Export", StringComparison.OrdinalIgnoreCase)
                    || method.Name.Contains("Download", StringComparison.OrdinalIgnoreCase);

                if (isExportLike && isAnonymous)
                    failures.Add($"{controller.Name}.{method.Name} is anonymous export/download.");

                var isPost = method.GetCustomAttribute<HttpPostAttribute>() != null;
                if (isPost && !isAnonymous && method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() == null)
                {
                    // Global antiforgery is accepted for normal MVC forms, but explicit attributes remain required
                    // for critical hand-written POST gates listed in legacy tests.
                    Assert.Contains("AutoValidateAntiforgeryTokenAttribute", program, StringComparison.Ordinal);
                }
            }
        }

        foreach (var sourcePath in new[]
        {
            Path.Combine("Controllers", "ReportsController.Inventory.cs"),
            Path.Combine("Controllers", "ReportsController.Analytics.cs"),
            Path.Combine("Controllers", "ReportsController.Enterprise1113.cs"),
            Path.Combine("Controllers", "OperationsController.DeliveryReconciliation.cs")
        })
        {
            var source = Read(Path.Combine(root, sourcePath));
            Assert.Contains("GetScopedWarehouseId", source, StringComparison.Ordinal);
            Assert.Contains("Authorize", source, StringComparison.Ordinal);
        }

        var threePl = Read(Path.Combine(root, "Controllers", "OperationsController.ThreePlMhe.cs"));
        Assert.Contains("EnsureCanAccessOwnerAsync", threePl, StringComparison.Ordinal);
        Assert.Contains("WmsPermissions.ThreePlBillingManage", threePl, StringComparison.Ordinal);

        Assert.True(failures.Count == 0, "Controller protection findings:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void ManualChecklistAndResidualRisks_ShouldRemainExplicitNotSilentlyTicked()
    {
        var root = FindRepositoryRoot();
        var securityChecklist = Read(Path.Combine(root, "PRODUCTION_SECURITY_CHECKLIST.md"));
        var moduleChecklist = Read(Path.Combine(root, "MODULE_ACCEPTANCE_CHECKLIST.md"));
        var backlog = Read(Path.Combine(root, "ENTERPRISE_TASK_BACKLOG.md"));
        var report = Read(Path.Combine(root, "ENTERPRISE_FULL_SYSTEM_DEEP_AUDIT_2026_05_12.md"));

        Assert.Contains("- [ ]", securityChecklist, StringComparison.Ordinal);
        Assert.Contains("- [ ]", moduleChecklist, StringComparison.Ordinal);
        Assert.Contains("Residual Full Audit Backlog 2026-05-12", backlog, StringComparison.Ordinal);
        Assert.Contains("manual checklist evidence", report, StringComparison.Ordinal);
        Assert.Contains("large controller refactor", report, StringComparison.Ordinal);
    }

    private static readonly string[] OperationalRoots =
    {
        "Controllers",
        "Services",
        "Models",
        "Data",
        "ViewModels",
        "Views",
        Path.Combine("wwwroot", "js"),
        Path.Combine("wwwroot", "css")
    };

    private static Dictionary<string, int> CountAuditedFiles(string root)
    {
        var groups = new[] { "Controllers", "Services", "Models", "Data", "ViewModels", "Views", "wwwroot", "WMS.Tests", "tests", "scripts", "Migrations" };
        return groups.ToDictionary(
            group => group,
            group =>
            {
                var path = Path.Combine(root, group);
                if (!Directory.Exists(path))
                    return 0;
                return EnumerateFiles(path, "*.*").Count();
            },
            StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsActionMethod(MethodInfo method)
    {
        if (method.IsSpecialName) return false;
        if (method.GetCustomAttribute<NonActionAttribute>() != null) return false;
        return typeof(IActionResult).IsAssignableFrom(method.ReturnType)
            || (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
                && typeof(IActionResult).IsAssignableFrom(method.ReturnType.GenericTypeArguments[0]));
    }

    private static bool HasPath(JsonElement root, string path)
        => TryGetPath(root, path, out _);

    private static bool IsFalse(JsonElement root, string path)
    {
        if (!TryGetPath(root, path, out var value))
            return false;
        return value.ValueKind == JsonValueKind.False
            || (value.ValueKind == JsonValueKind.String && string.Equals(value.GetString(), "false", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsBoolean(JsonElement root, string path)
    {
        if (!TryGetPath(root, path, out var value))
            return false;
        return value.ValueKind is JsonValueKind.True or JsonValueKind.False
            || (value.ValueKind == JsonValueKind.String
                && (string.Equals(value.GetString(), "true", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value.GetString(), "false", StringComparison.OrdinalIgnoreCase)));
    }

    private static bool TryGetPath(JsonElement root, string path, out JsonElement value)
    {
        value = root;
        foreach (var part in path.Split('.'))
        {
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(part, out value))
                return false;
        }

        return true;
    }

    private static bool IsOperationalTextFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension is ".cs" or ".cshtml" or ".js" or ".css";
    }

    private static IEnumerable<string> EnumerateFiles(string root, string searchPattern)
        => Directory.EnumerateFiles(root, searchPattern, SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}uploads{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}test-results{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}.auth{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}playwright-report{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains("-snapshots" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));

    private static string Read(string path)
        => File.ReadAllText(path, Encoding.UTF8);

    private static string ToRelativePath(string root, string path)
        => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "WMS.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate WMS.sln from test output directory.");
    }
}
