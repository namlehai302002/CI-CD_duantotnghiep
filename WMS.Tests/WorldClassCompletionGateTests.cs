using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using WMS.Controllers;

namespace WMS.Tests;

public sealed class WorldClassCompletionGateTests
{
    [Fact]
    public void ProductionPackageScript_ShouldGateRuntimeDevArtifactsWithoutPrintingConfigValues()
    {
        var root = FindRepositoryRoot();
        var script = Read(Path.Combine(root, "scripts", "Build-ProductionPackage.ps1"));
        var evidence = Read(Path.Combine(root, "RELEASE_EVIDENCE_2026_05_20.md"));
        var report = Read(Path.Combine(root, "ENTERPRISE_WMS_100_PERCENT_IMPLEMENTATION_REPORT_2026_05_20.md"));

        foreach (var token in new[]
        {
            "dotnet",
            "publish",
            "package-manifest.txt",
            "config-hashes.txt",
            "Get-FileHash",
            "App_Data/.*\\.log",
            "(^|/).*\\.log$",
            "App_Data/auto_backup_config\\.json",
            "App_Data/DataProtection-Keys",
            "App_Data/uploads",
            "appsettings.Development.json",
            "node_modules",
            "artifacts",
            "package.json",
            "package-lock\\.json",
            "test-results",
            "'local' + 'host|'",
            "Config values are not printed by this script"
        })
        {
            Assert.Contains(token, script, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("Get-Content -LiteralPath $_.FullName", script, StringComparison.Ordinal);
        Assert.Contains("appsettings.json", evidence, StringComparison.Ordinal);
        Assert.Contains("8774FCA21C5C3300F66E3E8A9959E391ECE69329F753D4DDED6C08E7C809DE9B", evidence, StringComparison.Ordinal);
        Assert.Contains("does not print connection strings", report, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=", evidence + report, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("User ID=", evidence + report, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReleaseEvidence_ShouldRequireEnterpriseEvidenceWithoutClaimingExternalPasses()
    {
        var root = FindRepositoryRoot();
        var evidence = Read(Path.Combine(root, "RELEASE_EVIDENCE_2026_05_20.md"));

        foreach (var token in new[]
        {
            "Build Evidence",
            "Test Evidence",
            "Vulnerability Scan",
            "Migration List",
            "Config Hash Evidence",
            "Packaging Manifest",
            "Backup/Restore Drill",
            "Visual Regression Evidence",
            "k6 Load Evidence",
            "Security Scope Scan",
            "Rollback Notes",
            "pending external evidence"
        })
        {
            Assert.Contains(token, evidence, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("Backup/Restore Drill\n\n- Status: passed", evidence, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("k6 Load Evidence\n\n- Status: passed", evidence, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Visual Regression Evidence\n\n- Status: passed", evidence, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApiIntegrationContracts_ShouldPreserveCompatibilityAndCoverEdiWebhookConnectorHealth()
    {
        var root = FindRepositoryRoot();
        var contracts = Read(Path.Combine(root, "docs", "API_INTEGRATION_ENTERPRISE_CONTRACTS.md"));
        var edi = Read(Path.Combine(root, "docs", "EDI_ENTERPRISE_ROADMAP.md"));
        var apiController = Read(Path.Combine(root, "Controllers", "ApiIntegrationController.cs"));
        var models = Read(Path.Combine(root, "Models", "OptimizationAutomationIntegrationEnterpriseModels.cs"));
        var service = Read(Path.Combine(root, "Services", "OptimizationAutomationIntegrationEnterpriseService.cs"));

        foreach (var token in new[] { "X-API-Key", "Api:Key", "backward-compatible", "/api/v1", "Breaking changes require a new version" })
            Assert.Contains(token, contracts, StringComparison.OrdinalIgnoreCase);

        foreach (var token in new[] { "ASN / 856", "Warehouse Shipping Order / 940", "Shipment Confirmation / 945", "Inventory Advice", "Receipt Confirmation" })
            Assert.Contains(token, edi, StringComparison.Ordinal);

        foreach (var token in new[] { "Request.Headers[\"X-API-Key\"]", "ValidateApiKey", "ExportEdi", "ReplayWebhook", "webhooks/{deliveryId:long}/replay" })
            Assert.Contains(token, apiController, StringComparison.Ordinal);

        foreach (var token in new[]
        {
            "GetApiScopedWarehouseId",
            "GetApiScopedOwnerPartnerId",
            "Api:ScopedOwnerPartnerId",
            "Api__ScopedOwnerPartnerId",
            "IsApiScopeAllowed",
            "ForbiddenScope403",
            "API_SCOPE_FORBIDDEN",
            "GetStockByItemAsync(scopedWh, itemRows.Select(i => i.ItemId), scopedOwner)",
            "GetStockByItemAsync(warehouseId, ownerPartnerId: scopedOwner)",
            "ExportEdi(long id)",
            "ReplayEdi(long id)",
            "ConfirmShipment(long carrierShipmentId)",
            "IssueThreePlInvoice(long invoiceId)"
        })
        {
            Assert.Contains(token, apiController, StringComparison.Ordinal);
        }

        foreach (var token in new[] { "WebhookDeliveryStatusEnum", "DeadLetter", "RetryCount", "NextRetryAt", "ReplayWebhookAsync", "EnterpriseConnectorTypeEnum", "EnterpriseConnectorHealthEnum" })
            Assert.Contains(token, models + service, StringComparison.Ordinal);

        foreach (var family in new[] { "Erp", "Tms", "Oms" })
            Assert.Contains($"EnterpriseConnectorTypeEnum.{family}", service, StringComparison.Ordinal);
    }

    [Fact]
    public void UxI18nAndAccessibility_ShouldLockPlaceholderGlossaryAndKnownMojibakeRules()
    {
        var root = FindRepositoryRoot();
        var scheduled = Read(Path.Combine(root, "Views", "Reports", "ScheduledReports.cshtml"));
        var glossary = Read(Path.Combine(root, "docs", "UX_MICROCOPY_GLOSSARY.md"));
        var integration = Read(Path.Combine(root, "Views", "Operations", "IntegrationDashboard.cshtml"));
        var threePlInvoice = Read(Path.Combine(root, "Views", "Operations", "ThreePlInvoiceDetails.cshtml"));
        var vasDetails = Read(Path.Combine(root, "Views", "Operations", "VasWorkOrderDetails.cshtml"));
        var kittingDetails = Read(Path.Combine(root, "Views", "Operations", "KittingWorkOrderDetails.cshtml"));
        var mheDashboard = Read(Path.Combine(root, "Views", "Operations", "MheDashboard.cshtml"));
        var assignTotes = Read(Path.Combine(root, "Views", "Operations", "AssignTotes.cshtml"));
        var siteCss = Read(Path.Combine(root, "wwwroot", "css", "site.css"));
        var views = Directory.EnumerateFiles(Path.Combine(root, "Views"), "*.cshtml", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(path => (Path: path, Content: Read(path)))
            .ToList();

        Assert.Contains("nguoidung1@congty.vn;nguoidung2@congty.vn", scheduled, StringComparison.Ordinal);
        Assert.DoesNotContain("user1@company.com;user2@company.com", scheduled, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mt-16", assignTotes, StringComparison.Ordinal);

        foreach (var token in new[]
        {
            "Đăng nhập",
            "Xác thực đa yếu tố",
            "Chủ hàng",
            "Kho",
            "Phiếu kho",
            "Đợt lấy hàng",
            "Lấy hàng",
            "Đóng gói",
            "Giao hàng",
            "Tính phí",
            "Nhật ký kiểm soát",
            "Thiết bị",
            "Ngoại lệ vận hành"
        })
        {
            Assert.Contains(token, glossary, StringComparison.Ordinal);
        }

        var knownBadMojibake = new[]
        {
            TextFromCodePoints(0x004E, 0x0067, 0x00C3, 0x0192),
            TextFromCodePoints(0x004B, 0x00C3, 0x00A1, 0x00C2, 0x00BA),
            TextFromCodePoints(0x00C3, 0x201E, 0x00C2, 0x0090),
            TextFromCodePoints(0x0043, 0x0068, 0x00C3, 0x00A1, 0x00C2, 0x00BB),
            TextFromCodePoints(0x0064, 0x00C6, 0x00A1, 0x006E, 0x0020, 0x0068, 0x00C3, 0x00A0, 0x006E, 0x0067),
            TextFromCodePoints(0x006B, 0x0068, 0x00C3, 0x00A0, 0x006E, 0x0067, 0x0020, 0x0064, 0x00C6, 0x00B0, 0x00E1, 0x00BB, 0x00A3, 0x0063)
        };
        var mojibakeFailures = views
            .SelectMany(view => knownBadMojibake
                .Where(token => view.Content.Contains(token, StringComparison.Ordinal))
                .Select(token => $"{Path.GetRelativePath(root, view.Path)} contains {token}"))
            .ToList();
        Assert.True(mojibakeFailures.Count == 0, "Known mojibake regressions:" + Environment.NewLine + string.Join(Environment.NewLine, mojibakeFailures));

        Assert.Contains(":focus-visible", siteCss, StringComparison.Ordinal);
        Assert.Contains(".table-subtext", siteCss, StringComparison.Ordinal);
        Assert.Contains("aria-label", Read(Path.Combine(root, "Views", "Shared", "_Layout.cshtml")), StringComparison.Ordinal);
        Assert.All(views.Where(v => v.Content.Contains("<table", StringComparison.OrdinalIgnoreCase)), view =>
            Assert.Contains("<th", view.Content, StringComparison.OrdinalIgnoreCase));

        Assert.Contains("asp-action=\"IntegrationOpenApiContract\"", integration, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/api/v1/openapi.json\"", integration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name=\"signingSecret\" type=\"password\"", integration, StringComparison.Ordinal);
        Assert.Contains("autocomplete=\"new-password\"", integration, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Khóa ký webhook\"", integration, StringComparison.Ordinal);
        Assert.Contains("data-redacted=\"webhook-signature\"", integration, StringComparison.Ordinal);
        Assert.DoesNotContain(">@d.Signature<", integration, StringComparison.Ordinal);
        Assert.Contains("\"[đã che]\"", integration, StringComparison.Ordinal);

        foreach (var token in new[]
        {
            "aria-label=\"Số tiền khiếu nại\"",
            "aria-label=\"Lý do khiếu nại\"",
            "aria-label=\"Gửi khiếu nại phí\"",
            "aria-label=\"Số tiền được duyệt\"",
            "aria-label=\"Phản hồi xử lý khiếu nại\""
        })
        {
            Assert.Contains(token, threePlInvoice, StringComparison.Ordinal);
        }

        foreach (var token in new[]
        {
            "aria-label=\"Phút thực tế\"",
            "aria-label=\"Ghi chú thao tác\"",
            "aria-label=\"Số lượng hoàn tất gửi kiểm tra\"",
            "aria-label=\"Kết quả kiểm tra chất lượng\"",
            "aria-label=\"Số lượng đạt\"",
            "aria-label=\"Số lượng lỗi\"",
            "aria-label=\"Ghi chú kiểm tra chất lượng\"",
            "aria-label=\"Lý do hủy lệnh gia công phụ trợ\""
        })
        {
            Assert.Contains(token, vasDetails, StringComparison.Ordinal);
        }

        Assert.Contains("aria-label=\"Lý do hủy phiếu lắp bộ hàng\"", kittingDetails, StringComparison.Ordinal);
        Assert.Contains("class=\"table-subtext\"", kittingDetails, StringComparison.Ordinal);
        Assert.DoesNotContain("style" + "=\"display:flex", kittingDetails, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("style" + "=\"width:240px", kittingDetails, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("aria-label=\"Gửi lại lệnh thiết bị @command.CommandCode\"", mheDashboard, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Hủy lệnh thiết bị @command.CommandCode\"", mheDashboard, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Đóng hộp thoại thêm lịch báo cáo\"", scheduled, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"@(r.IsActive ? \"Tạm dừng lịch báo cáo\" : \"Kích hoạt lịch báo cáo\") @r.ReportName\"", scheduled, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Xóa lịch báo cáo @r.ReportName\"", scheduled, StringComparison.Ordinal);

        foreach (var priorityForm in new[]
        {
            Path.Combine(root, "Views", "Account", "Login.cshtml"),
            Path.Combine(root, "Views", "Account", "VerifyMfa.cshtml"),
            Path.Combine(root, "Views", "Reports", "ScheduledReports.cshtml"),
            Path.Combine(root, "Views", "Operations", "ThreePlContracts.cshtml"),
            Path.Combine(root, "Views", "Operations", "IntegrationDashboard.cshtml")
        })
        {
            var content = Read(priorityForm);
            Assert.True(content.Contains("<label", StringComparison.OrdinalIgnoreCase) || content.Contains("aria-label", StringComparison.OrdinalIgnoreCase),
                $"{Path.GetRelativePath(root, priorityForm)} must expose labels or aria-labels on priority forms.");
        }
    }

    [Fact]
    public void ExportDownloadApiScopeRegistry_ShouldCoverEverySensitiveReadSurface()
    {
        var root = FindRepositoryRoot();
        var registry = Read(Path.Combine(root, "docs", "EXPORT_DOWNLOAD_API_SCOPE_REGISTRY.md"));
        var controllerTypes = typeof(HomeController).Assembly
            .GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .ToList();

        var exportActions = controllerTypes
            .SelectMany(controller => controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(IsActionMethod)
                .Where(method => method.Name.Contains("Export", StringComparison.OrdinalIgnoreCase)
                    || method.Name.Contains("Download", StringComparison.OrdinalIgnoreCase))
                .Select(method => $"{controller.Name.Replace("Controller", "", StringComparison.Ordinal)}.{method.Name}"))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        var missing = exportActions
            .Where(action => !registry.Contains(action, StringComparison.Ordinal))
            .ToList();

        Assert.True(missing.Count == 0, "Missing registry entries:" + Environment.NewLine + string.Join(Environment.NewLine, missing));

        foreach (var token in new[] { "Authorization", "Warehouse scope", "Owner scope", "Audit logging", "Anti-forgery", "No cross-owner leakage" })
            Assert.Contains(token, registry, StringComparison.Ordinal);

        foreach (var scopeToken in new[] { "multi-owner", "partial warehouse scope", "partial owner scope", "Owner billing scope", "Analytics owner-scope" })
            Assert.Contains(scopeToken, registry, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnterpriseDepthEvidence_ShouldUseExistingServicesFor3PlLaborOptimizationAndWes()
    {
        var root = FindRepositoryRoot();
        var allTests = string.Join(Environment.NewLine, Directory.EnumerateFiles(Path.Combine(root, "WMS.Tests"), "*.cs", SearchOption.TopDirectoryOnly).Select(Read));
        var threePlService = Read(Path.Combine(root, "Services", "ThreePlEnterpriseBillingService.cs"));
        var laborService = Read(Path.Combine(root, "Services", "LaborManagementService.cs"));
        var optimizationService = Read(Path.Combine(root, "Services", "OptimizationAutomationIntegrationEnterpriseService.cs"));
        var mheModels = Read(Path.Combine(root, "Models", "MheIntegrationModels.cs")) + Read(Path.Combine(root, "Models", "Enums.cs"));

        foreach (var token in new[] { "MinimumCharge", "TierFromQty", "TierToQty", "SurchargePercent", "OffHoursSurcharge", "UrgentSurcharge", "HazmatSurcharge", "ColdStorageSurcharge", "ManualHandlingSurcharge", "ThreePlInvoiceStatusEnum", "ThreePlDisputeStatusEnum" })
            Assert.Contains(token, threePlService + Read(Path.Combine(root, "Models", "YardTms3PlLaborEnterpriseModels.cs")), StringComparison.Ordinal);

        foreach (var token in new[] { "LaborStandard", "TaskType", "ZoneId", "ShiftCode", "TaskSourceType", "WaitingMinutes", "ExceptionReason", "LaborExceptionReview", "IncentiveAmount" })
            Assert.Contains(token, laborService + Read(Path.Combine(root, "Models", "LaborStandard.cs")) + Read(Path.Combine(root, "Models", "YardTms3PlLaborEnterpriseModels.cs")), StringComparison.Ordinal);

        foreach (var token in new[] { "RunSlottingOptimizationAsync", "RunWaveOptimizationAsync", "RunWavelessReleaseAsync", "GeneratePickPathPlanAsync", "CreateToteClusterPlanAsync", "Reason", "BlockReason", "GroupKey" })
            Assert.Contains(token, optimizationService, StringComparison.OrdinalIgnoreCase);

        foreach (var token in new[] { "Conveyor", "Sorter", "Robot", "Amr", "DivertPackage", "RobotMission", "Sent", "Acknowledged", "InProgress", "Completed", "Failed", "Cancelled", "DeadLetter", "Retry", "Override" })
            Assert.Contains(token, mheModels + optimizationService, StringComparison.OrdinalIgnoreCase);

        foreach (var marker in new[]
        {
            "ThreePlBilling_ShouldRateInvoiceLockAndResolveDispute",
            "LaborManagement_ShouldCaptureExceptionAndManagerApproval",
            "Optimization_ShouldCreateSlottingWaveWavelessPickPathAndToteCluster",
            "Automation_ShouldRecordTelemetrySimulateFailuresAndRequireOverrideReason"
        })
        {
            Assert.Contains(marker, allTests, StringComparison.Ordinal);
        }
    }

    private static bool IsActionMethod(MethodInfo method)
    {
        if (method.IsSpecialName) return false;
        if (method.GetCustomAttribute<NonActionAttribute>() != null) return false;
        return typeof(IActionResult).IsAssignableFrom(method.ReturnType)
            || (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
                && typeof(IActionResult).IsAssignableFrom(method.ReturnType.GenericTypeArguments[0]));
    }

    private static string Read(string path)
        => File.ReadAllText(path, Encoding.UTF8);

    private static string TextFromCodePoints(params int[] codePoints)
    {
        var builder = new StringBuilder();
        foreach (var codePoint in codePoints)
            builder.Append(char.ConvertFromUtf32(codePoint));
        return builder.ToString();
    }

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
