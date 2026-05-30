using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using WMS.Common;
using WMS.Controllers;
using WMS.Data;
using WMS.Models;

namespace WMS.Tests;

public sealed class EnterpriseQualityGateCompletionTests
{
    [Fact]
    public void QaSection_ShouldBeCompletedAndBackedByFullAuditEvidence()
    {
        var root = FindRepositoryRoot();
        var tasks = Read(Path.Combine(root, "ENTERPRISE_WMS_100_PERCENT_TASKS.md"));
        var qaSection = Slice(tasks, "## 14.", "## 15.");
        var audit = Read(Path.Combine(root, "ENTERPRISE_QUALITY_GATE_AND_FULL_AUDIT.md"));

        foreach (var code in new[] { "QA-01", "QA-02", "QA-03", "QA-04", "QA-05" })
        {
            Assert.Contains($"- [x] `{code}`", qaSection, StringComparison.Ordinal);
            Assert.Contains(code, audit, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("- [ ]", qaSection, StringComparison.Ordinal);
        Assert.Contains("Critical findings: 0 open", audit, StringComparison.Ordinal);
        Assert.Contains("Residual backlog", audit, StringComparison.Ordinal);
    }

    [Fact]
    public void Qa01_BusinessRegressionMatrix_ShouldCoverAllTierOneWmsDomains()
    {
        var testSource = ReadAllTestSourceExceptSelf();
        var requiredMarkers = new Dictionary<string, string[]>
        {
            ["inbound"] = new[] { "CompleteInbound_ShouldPostVoucherAndMarkCompleted", "CompleteInbound_ShouldRequireLotAndExpiryForTrackedItem" },
            ["outbound"] = new[] { "PartialBatchPick_ShouldAllocateAndPostCorrectly", "ConfirmShipping_ShouldRespectCarrierRequirementWhenEnabled" },
            ["inventory"] = new[] { "Core03_InventoryStatusEngine_ShouldSplitAvailabilityAndBlockInvalidPosting", "CycleCountPlanning_ShouldCreateDueSheetByAbcZoneAndAdvanceSchedule" },
            ["serial"] = new[] { "SerialInventory_ShouldReserveStrictPickAndPostLifecycle", "ConfirmPickTask_ShouldRequireAndAssignSerialsForSerialTrackedItem" },
            ["lpn"] = new[] { "ScanLpn_JsonLookup_ShouldSummarizeMixedLpnDetails", "LpnHierarchyService_ShouldBlockSelfParentAndIndirectLoops" },
            ["catch weight"] = new[] { "RequireInboundCatchWeightAsync_ShouldBlockTrackedItemWithoutCapturedWeight", "ShipmentLoadDepartAsync_ShouldRequirePackageScanAndCatchWeightBeforeDeparture" },
            ["yard"] = new[] { "DockAppointment_ShouldSuggestDoorAndRejectOverlappingWindow", "YardGateIn_ShouldPreventSecondActiveVisitForSameTrailer" },
            ["carrier"] = new[] { "CarrierIntegration_MockCreate_ShouldCreatePackageShipmentIdempotently", "CarrierCallback_ShouldBeIdempotentAndUpdatePackageTracking" },
            ["3pl"] = new[] { "ThreePlBilling_ShouldRateInvoiceLockAndResolveDispute", "ThreePlBilling_ShouldRateInvoiceLockAndResolveDispute" },
            ["labor"] = new[] { "LaborManagement_ShouldCaptureExceptionAndManagerApproval" },
            ["analytics"] = new[] { "BiPredictiveAuditAndAssistant_ShouldRespectScopeCitationAndMutationBlock" },
            ["optimization"] = new[] { "Optimization_ShouldCreateSlottingWaveWavelessPickPathAndToteCluster" },
            ["automation"] = new[] { "Automation_ShouldRecordTelemetrySimulateFailuresAndRequireOverrideReason" },
            ["integration"] = new[] { "Integration_ShouldExposeOpenApiEdiWebhookConnectorAndOutbox" }
        };

        var missing = requiredMarkers
            .SelectMany(group => group.Value
                .Where(marker => !testSource.Contains(marker, StringComparison.Ordinal))
                .Select(marker => $"{group.Key}: {marker}"))
            .ToList();

        Assert.True(missing.Count == 0, "Missing business regression markers:" + Environment.NewLine + string.Join(Environment.NewLine, missing));
    }

    [Fact]
    public void Qa02_SecurityGate_ShouldCoverRoleScopeExportCsrfPasswordAndSession()
    {
        var root = FindRepositoryRoot();
        var program = Read(Path.Combine(root, "Program.cs"));
        var testSource = ReadAllTestSourceExceptSelf();
        var controllers = string.Join(Environment.NewLine, EnumerateFiles(Path.Combine(root, "Controllers"), "*.cs").Select(Read));
        var securityChecklist = Read(Path.Combine(root, "PRODUCTION_SECURITY_CHECKLIST.md"));

        foreach (var token in new[]
        {
            "AuthorizeFilter(policy)",
            "AutoValidateAntiforgeryTokenAttribute",
            "app.UseAuthentication()",
            "app.UseAuthorization()",
            "UseWmsCorrelationTelemetry"
        })
        {
            Assert.Contains(token, program, StringComparison.Ordinal);
        }

        foreach (var marker in new[]
        {
            "AuthorizationMatrixTests",
            "SensitivePostActions_ShouldUseAntiForgery",
            "Sec01_ExternalIdentityMapper_ShouldMapOidcSamlClaimsToRoleWarehouseOwnerAndPermissions",
            "Sec02_MfaLockoutAndPasswordReset_ShouldLockResetAndNeverStorePlainPassword",
            "Sec04_ScopeAudit_ShouldAllowScopedExportAndDenyForeignWarehouse"
        })
        {
            Assert.Contains(marker, testSource, StringComparison.Ordinal);
        }

        foreach (var token in new[] { "GetScopedWarehouseId", "GetOwnerScopeClaimIds", "EnsureCanAccessOwnerAsync", "ValidateApiKey", "X-API-Key" })
            Assert.Contains(token, controllers, StringComparison.Ordinal);

        foreach (var token in new[] { "CSRF", "Authorization And Scope", "Authentication", "Data Protection", "Security Headers" })
            Assert.Contains(token, securityChecklist, StringComparison.Ordinal);

        AssertNoAnonymousExportActions();
    }

    [Fact]
    public void Qa03_UiComponentGate_ShouldCoverModalExportFilterTableFloatingQueueScannerAndPwa()
    {
        var root = FindRepositoryRoot();
        var css = Read(Path.Combine(root, "wwwroot", "css", "site.css"));
        var queue = Read(Path.Combine(root, "wwwroot", "js", "offline-scan-queue.js"));
        var scanner = Read(Path.Combine(root, "wwwroot", "js", "mobile-scanner.js"));
        var pwa = Read(Path.Combine(root, "wwwroot", "js", "pwa.js"));
        var layout = Read(Path.Combine(root, "Views", "Shared", "_Layout.cshtml"));
        var visualSpec = Read(Path.Combine(root, "tests", "visual", "wms-visual-regression.spec.ts"));
        var uiTests = Read(Path.Combine(root, "WMS.Tests", "EnterpriseUiRedesignTests.cs"));

        foreach (var token in new[]
        {
            ".modal-body",
            ".filter-panel",
            ".data-table",
            ".btn-export",
            "--wms-floating-widget-bottom",
            "--wms-install-banner-bottom",
            ".scanner-modal-body",
            ".mobile-action-bar"
        })
        {
            Assert.Contains(token, css, StringComparison.Ordinal);
        }

        foreach (var token in new[] { "MAX_ATTEMPTS", "BACKOFF_MS", "deadletter", "conflict", "exportQueueSnapshot" })
            Assert.Contains(token, queue, StringComparison.Ordinal);

        foreach (var token in new[] { "parseBarcode", "parseBulk", "Html5Qrcode" })
            Assert.Contains(token, scanner, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("html5-qrcode.min.js", layout, StringComparison.OrdinalIgnoreCase);

        foreach (var token in new[] { "beforeinstallprompt", "wms-install-ready", "document.body?.dataset?.wmsOperational" })
            Assert.Contains(token, pwa, StringComparison.Ordinal);

        foreach (var token in new[] { "desktop-100", "desktop-110", "desktop-125", "mobile", "toHaveScreenshot" })
            Assert.Contains(token, visualSpec + Read(Path.Combine(root, "tests", "visual", "playwright.config.ts")), StringComparison.Ordinal);

        foreach (var marker in new[]
        {
            "ModalCss_ShouldKeepActionFooterVisibleWithoutFullscreen",
            "ThreePlBilling_ShouldUseEnterpriseWorkspaceAndDownloadActions",
            "FloatingUtilities_ShouldReserveBottomSpaceForOperationalActions",
            "UserManagement_ShouldRenderEnterpriseIdentityWorkspace"
        })
        {
            Assert.Contains(marker, uiTests, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Qa04_DataIntegrityGate_ShouldProtectLedgerSerialPeriodLockAndTenantIsolation()
    {
        using var db = CreateDb();
        var model = db.Model;

        // P0-5: SerialCode unique theo composite (WarehouseId, ItemId, SerialCode) thay vì global, cho phép multi-warehouse/owner.
        Assert.True(HasAnyUniqueIndexContaining(model.FindEntityType(typeof(SerialNumber))!, nameof(SerialNumber.SerialCode)));
        Assert.True(HasUniqueIndex(model.FindEntityType(typeof(PickTaskSerialAssignment))!, nameof(PickTaskSerialAssignment.SerialNumberId)));
        Assert.True(HasUniqueIndex(model.FindEntityType(typeof(InventoryTransaction))!, nameof(InventoryTransaction.IdempotencyKey)));
        Assert.True(HasUniqueIndex(model.FindEntityType(typeof(WarehousePeriodLock))!, nameof(WarehousePeriodLock.WarehouseId), nameof(WarehousePeriodLock.LockDate)));
        Assert.True(HasAnyUniqueIndexContaining(model.FindEntityType(typeof(ItemLocation))!, nameof(ItemLocation.OwnerPartnerId), nameof(ItemLocation.ItemId), nameof(ItemLocation.LocationId), nameof(ItemLocation.HoldStatus)));

        var invalidLedger = new InventoryTransaction
        {
            TransactionType = InventoryTransactionTypeEnum.Receive,
            TransactionGroupKey = "qa-ledger",
            IdempotencyKey = "qa-ledger-1",
            WarehouseId = 1,
            ItemId = 1,
            LocationId = 1,
            QuantityBefore = 0,
            QuantityAfter = 5,
            QuantityDelta = 4,
            ReservedBefore = 0,
            ReservedAfter = 0,
            AvailableBefore = 0,
            AvailableAfter = 5
        };

        Assert.Throws<BusinessRuleException>(() => InventoryTransactionSemanticRules.Validate(invalidLedger));

        var testSource = ReadAllTestSourceExceptSelf();
        foreach (var marker in new[]
        {
            "PeriodLock_ShouldUseCompletedAt_WhenNewVoucherCompletedInsideLockedPeriod",
            "ScanLpn_ShouldRejectForeignWarehouseScope",
            "Sec04_ScopeAudit_ShouldAllowScopedExportAndDenyForeignWarehouse",
            "VasReserve_ShouldIgnoreHeldStockAndFailWhenNoAvailableQty",
            "KittingReserve_ShouldIgnoreHeldStockAndFailWhenNoAvailableQty"
        })
        {
            Assert.Contains(marker, testSource, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Qa05_EndToEndScenarioPack_ShouldMapAsnToInvoiceWithExistingRegressionCoverage()
    {
        var root = FindRepositoryRoot();
        var audit = Read(Path.Combine(root, "ENTERPRISE_QUALITY_GATE_AND_FULL_AUDIT.md"));
        var testSource = ReadAllTestSourceExceptSelf();

        foreach (var step in new[]
        {
            "ASN",
            "Receiving",
            "Putaway",
            "Replenishment",
            "Wave",
            "Waveless",
            "Pick",
            "Pack",
            "Ship",
            "Invoice"
        })
        {
            Assert.Contains(step, audit, StringComparison.Ordinal);
        }

        foreach (var marker in new[]
        {
            "CompleteInbound_ShouldPostVoucherAndMarkCompleted",
            "Core02_Replenishment_ShouldIncludeWaveDemandAndCreateSlaPriorityTask",
            "Optimization_ShouldCreateSlottingWaveWavelessPickPathAndToteCluster",
            "PartialBatchPick_ShouldAllocateAndPostCorrectly",
            "ConfirmPacking",
            "ShipmentLoadDepartAsync_ShouldRequirePackageScanAndCatchWeightBeforeDeparture",
            "GenerateInvoiceFromRunAsync"
        })
        {
            Assert.Contains(marker, testSource, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void PriorityRules16_ShouldBeEnforcedByAuditableGates()
    {
        var root = FindRepositoryRoot();
        var tasks = Read(Path.Combine(root, "ENTERPRISE_WMS_100_PERCENT_TASKS.md"));
        var audit = Read(Path.Combine(root, "ENTERPRISE_QUALITY_GATE_AND_FULL_AUDIT.md"));
        var outbound = Read(Path.Combine(root, "Controllers", "VouchersController.Outbound.cs"));
        var inbound = Read(Path.Combine(root, "Controllers", "VouchersController.Inbound.cs"));
        var cancellation = Read(Path.Combine(root, "Services", "VoucherCancellationService.cs"));
        var outboundExecution = Read(Path.Combine(root, "Services", "OutboundExecutionService.cs"));
        var visual = Read(Path.Combine(root, "tests", "visual", "wms-visual-regression.spec.ts"));

        var section16 = Slice(tasks, "## 16.", "");
        foreach (var token in new[] { "SEC", "PROD", "QA", "idempotency", "audit trail", "warehouse scope", "owner scope", "zoom 110%" })
            Assert.Contains(token, section16, StringComparison.OrdinalIgnoreCase);

        foreach (var source in new[] { outboundExecution, cancellation })
            Assert.Contains("IdempotencyKeyPrefix", source, StringComparison.Ordinal);

        foreach (var source in new[] { inbound, outbound, cancellation })
            Assert.Contains("RollbackAsync", source, StringComparison.Ordinal);

        foreach (var token in new[] { "desktop-110", "mobile", "users", "semantic-bi", "sre-dashboard" })
            Assert.Contains(token, visual, StringComparison.Ordinal);

        Assert.Contains("Priority Rule 16 Compliance", audit, StringComparison.Ordinal);
        Assert.Contains("workflow status + role + log + test", audit, StringComparison.Ordinal);
    }

    [Fact]
    public void FullSystemAudit_ShouldHaveNoOpenCriticalPatternsInOperationalSource()
    {
        var root = FindRepositoryRoot();
        var audit = Read(Path.Combine(root, "ENTERPRISE_QUALITY_GATE_AND_FULL_AUDIT.md"));
        var operationalRoots = new[]
        {
            "Controllers",
            "Services",
            "Models",
            "ViewModels",
            "Views",
            Path.Combine("wwwroot", "js"),
            Path.Combine("wwwroot", "css")
        };

        var files = operationalRoots
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

            foreach (var banned in new[] { "NotImplementedException", "async void", "local" + "host", "File bàn giao", "HUONG_DAN_THUC_HANH_WMS_CHI_TIET.md" })
            {
                if (content.Contains(banned, StringComparison.OrdinalIgnoreCase))
                    failures.Add($"{relative} contains `{banned}`.");
            }

            if (Regex.IsMatch(content, @"\.Result(?![A-Za-z0-9_])"))
                failures.Add($"{relative} contains blocking `.Result`.");
            if (Regex.IsMatch(content, @"\.Wait\s*\("))
                failures.Add($"{relative} contains blocking `.Wait(`.");
        }

        Assert.True(failures.Count == 0, "Open critical audit findings:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
        Assert.Contains("Reviewed directories: Controllers, Services, Models, ViewModels, Views, wwwroot/js, wwwroot/css, WMS.Tests, tests, scripts, docs", audit, StringComparison.Ordinal);
        Assert.Contains("Critical findings: 0 open", audit, StringComparison.Ordinal);
    }

    private static void AssertNoAnonymousExportActions()
    {
        var controllerTypes = typeof(HomeController).Assembly
            .GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .ToList();

        var failures = new List<string>();
        foreach (var controller in controllerTypes)
        {
            foreach (var method in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(IsActionMethod))
            {
                var isExportLike = method.Name.Contains("Export", StringComparison.OrdinalIgnoreCase)
                    || method.Name.Contains("Download", StringComparison.OrdinalIgnoreCase);
                if (!isExportLike)
                    continue;

                var isAnonymous = method.GetCustomAttribute<AllowAnonymousAttribute>() != null
                    || controller.GetCustomAttribute<AllowAnonymousAttribute>() != null;
                if (isAnonymous)
                    failures.Add($"{controller.Name}.{method.Name}");
            }
        }

        Assert.True(failures.Count == 0, "Export/download actions must not be anonymous:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    private static bool IsActionMethod(MethodInfo method)
    {
        if (method.IsSpecialName) return false;
        if (method.GetCustomAttribute<NonActionAttribute>() != null) return false;
        return typeof(IActionResult).IsAssignableFrom(method.ReturnType)
            || (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
                && typeof(IActionResult).IsAssignableFrom(method.ReturnType.GenericTypeArguments[0]));
    }

    private static bool HasUniqueIndex(IEntityType entity, params string[] propertyNames)
    {
        var expected = propertyNames.ToHashSet(StringComparer.Ordinal);
        return entity.GetIndexes().Any(index => index.IsUnique && index.Properties.Select(p => p.Name).ToHashSet(StringComparer.Ordinal).SetEquals(expected));
    }

    private static bool HasAnyUniqueIndexContaining(IEntityType entity, params string[] propertyNames)
    {
        var expected = propertyNames.ToHashSet(StringComparer.Ordinal);
        return entity.GetIndexes().Any(index => index.IsUnique && expected.IsSubsetOf(index.Properties.Select(p => p.Name).ToHashSet(StringComparer.Ordinal)));
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("quality-gate-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private static string ReadAllTestSourceExceptSelf()
    {
        var root = FindRepositoryRoot();
        return string.Join(
            Environment.NewLine,
            EnumerateFiles(Path.Combine(root, "WMS.Tests"), "*.cs")
                .Where(path => !string.Equals(Path.GetFileName(path), nameof(EnterpriseQualityGateCompletionTests) + ".cs", StringComparison.OrdinalIgnoreCase))
                .Select(Read));
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
                && !path.Contains($"{Path.DirectorySeparatorChar}uploads{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

    private static string Slice(string content, string startMarker, string endMarker)
    {
        var start = content.IndexOf(startMarker, StringComparison.Ordinal);
        if (start < 0) return "";
        if (string.IsNullOrWhiteSpace(endMarker)) return content[start..];
        var end = content.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        return end < 0 ? content[start..] : content[start..end];
    }

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
