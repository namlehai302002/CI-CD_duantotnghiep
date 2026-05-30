using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using WMS.Authorization;
using WMS.Controllers;

namespace WMS.Tests;

public class DefinitionOfDone100GateTests
{
    [Fact]
    public void DefinitionOfDoneSection_ShouldBeCompletedAndDocumentAppsettingsConstraint()
    {
        var root = FindRepositoryRoot();
        var tasks = ReadUtf8(Path.Combine(root, "ENTERPRISE_WMS_100_PERCENT_TASKS.md"));
        var section = Slice(tasks, "## 1. Definition Of Done 100%", "## 2. Core WMS");

        Assert.Equal(7, section.Split("- [x]", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("- [ ]", section, StringComparison.Ordinal);
        Assert.Contains("Không sửa `appsettings`", section, StringComparison.Ordinal);
        Assert.Contains("secret rotation", section, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductionReadinessDocs_ShouldCoverRunbookSecurityAndModuleAcceptance()
    {
        var root = FindRepositoryRoot();
        var runbook = ReadUtf8(Path.Combine(root, "PRODUCTION_RUNBOOK.md"));
        var security = ReadUtf8(Path.Combine(root, "PRODUCTION_SECURITY_CHECKLIST.md"));
        var modules = ReadUtf8(Path.Combine(root, "MODULE_ACCEPTANCE_CHECKLIST.md"));

        foreach (var token in new[] { "Backup And Restore", "Disaster Recovery", "Monitoring", "Incident Response", "Release Checklist", "/health" })
            Assert.Contains(token, runbook, StringComparison.Ordinal);

        foreach (var token in new[] { "Secrets And Configuration", "Authentication", "Authorization And Scope", "CSRF", "Audit Trail", "Data Protection", "Security Headers" })
            Assert.Contains(token, security, StringComparison.Ordinal);

        foreach (var token in new[] { "Inbound", "Outbound", "Inventory", "Users And Security", "Reports And Analytics", "Yard And Dock", "Carrier", "3PL Billing", "Mobile, RF And Offline", "Integration And API" })
            Assert.Contains(token, modules, StringComparison.Ordinal);
    }

    [Fact]
    public void VisualAndLoadScaffolds_ShouldDefineRequiredScenariosWithoutAppsettingsDependency()
    {
        var root = FindRepositoryRoot();
        var visualReadme = ReadUtf8(Path.Combine(root, "tests", "visual", "README.md"));
        var visualConfig = ReadUtf8(Path.Combine(root, "tests", "visual", "playwright.config.ts"));
        var visualSpec = ReadUtf8(Path.Combine(root, "tests", "visual", "wms-visual-regression.spec.ts"));
        var loadReadme = ReadUtf8(Path.Combine(root, "tests", "load", "README.md"));
        var loadScript = ReadUtf8(Path.Combine(root, "tests", "load", "k6-wms-dod.js"));

        Assert.Contains("WMS_BASE_URL", visualReadme, StringComparison.Ordinal);
        Assert.Contains("WMS_AUTH_STATE", visualReadme, StringComparison.Ordinal);
        Assert.Contains("desktop-100", visualConfig, StringComparison.Ordinal);
        Assert.Contains("desktop-110", visualConfig, StringComparison.Ordinal);
        Assert.Contains("desktop-125", visualConfig, StringComparison.Ordinal);
        Assert.Contains("mobile", visualConfig, StringComparison.Ordinal);
        Assert.Contains("1.1", visualSpec, StringComparison.Ordinal);
        Assert.Contains("ThreePlBillingRuns", visualSpec, StringComparison.Ordinal);
        Assert.Contains("Users", visualSpec, StringComparison.Ordinal);

        Assert.Contains("WMS_BASE_URL", loadReadme, StringComparison.Ordinal);
        Assert.Contains("WMS_AUTH_COOKIE", loadReadme, StringComparison.Ordinal);
        Assert.Contains("WMS_API_KEY", loadReadme, StringComparison.Ordinal);
        Assert.Contains("WMS_K6_MUTATION_ENABLED", loadReadme, StringComparison.Ordinal);
        Assert.Contains("does not fall back to login", loadScript, StringComparison.Ordinal);
        Assert.Contains("status is successful", loadScript, StringComparison.Ordinal);
        Assert.Contains("handleSummary", loadScript, StringComparison.Ordinal);
        foreach (var scenario in new[] { "inventory_posting_reads", "scan_queue_retry", "large_reports", "three_pl_billing", "integration_api" })
            Assert.Contains(scenario, loadScript, StringComparison.Ordinal);
    }

    [Fact]
    public void OperationalSource_ShouldAvoidDodBannedLabelsAndBlockingPatterns()
    {
        var root = FindRepositoryRoot();
        var operationalRoots = new[]
        {
            "Controllers",
            "Services",
            "Data",
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
            var content = ReadUtf8(file);

            foreach (var banned in new[] { "N/A", "local" + "host", "File bàn giao", "HUONG_DAN_THUC_HANH_WMS_CHI_TIET.md", "NotImplementedException", "async void" })
            {
                if (content.Contains(banned, StringComparison.OrdinalIgnoreCase))
                    failures.Add($"{relative} chứa `{banned}`.");
            }

            if (Regex.IsMatch(content, @"\.Result(?![A-Za-z0-9_])"))
                failures.Add($"{relative} chứa `.Result` blocking.");

            if (Regex.IsMatch(content, @"\.Wait\s*\("))
                failures.Add($"{relative} chứa `.Wait(` blocking.");

            if (Regex.IsMatch(content, @"\.GetAwaiter\(\)\.GetResult\(\)")
                && !relative.Equals("Data/AppDbContext.cs", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{relative} chứa sync-over-async `.GetAwaiter().GetResult()`.");
            }

            if (Regex.IsMatch(content, @"\.SaveChanges\s*\(")
                && !relative.Equals("Data/AppDbContext.cs", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{relative} chứa caller sync `.SaveChanges()`.");
            }

            if ((relative.StartsWith("Views/", StringComparison.OrdinalIgnoreCase)
                    || relative.StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase))
                && content.Contains("demo", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{relative} còn nhãn demo trong UI.");
            }
        }

        Assert.True(failures.Count == 0, "DoD static scan phát hiện chuỗi không đạt:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void OperationalVietnameseText_ShouldNotContainKnownTypingRegressions()
    {
        var root = FindRepositoryRoot();
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

        var banned = new[] { "khàng dược", "khmng", "Khmng", "dơn hàng", "dếm", "mm phỏng" };
        var failures = new List<string>();

        foreach (var file in files)
        {
            var content = ReadUtf8(file);
            foreach (var typo in banned)
            {
                if (content.Contains(typo, StringComparison.OrdinalIgnoreCase))
                    failures.Add($"{ToRelativePath(root, file)} chứa lỗi gõ `{typo}`.");
            }
        }

        Assert.True(failures.Count == 0, "Chuỗi tiếng Việt user-facing còn lỗi gõ:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void MinerUHostGuide_ShouldDocumentInterDataSharedHostingConstraint()
    {
        var root = FindRepositoryRoot();
        var guide = ReadUtf8(Path.Combine(root, "MINERU_HOST_DEPLOYMENT_GUIDE.md"));

        foreach (var token in new[]
        {
            "InterData",
            "Hosting Windows Sinh Viên",
            "shared ASP.NET",
            "không nên chạy MinerU chung",
            "VPS",
            "Python",
            "Docker",
            "\"Enabled\": false",
            "Không cần xóa các giá trị hiện có trong `appsettings.json`"
        })
        {
            Assert.Contains(token, guide, StringComparison.Ordinal);
        }

        foreach (var mojibake in new[]
        {
            TextFromCodePoints(0x00C3),
            TextFromCodePoints(0x00C4),
            TextFromCodePoints(0x00C6),
            TextFromCodePoints(0x00E1, 0x00BA),
            TextFromCodePoints(0x00E1, 0x00BB)
        })
        {
            Assert.DoesNotContain(mojibake, guide, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CurrentStockAudit_ShouldProtectItemLocationAsInventorySourceOfTruth()
    {
        var root = FindRepositoryRoot();
        var audit = ReadUtf8(Path.Combine(root, "CURRENTSTOCK_SOURCE_OF_TRUTH_AUDIT_2026_05_17.md"));
        var service = ReadUtf8(Path.Combine(root, "Services", "InventoryBalanceService.cs"));
        var home = ReadUtf8(Path.Combine(root, "Controllers", "HomeController.cs"));
        var reportsInventory = ReadUtf8(Path.Combine(root, "Controllers", "ReportsController.Inventory.cs"));
        var reportsStockCount = ReadUtf8(Path.Combine(root, "Controllers", "ReportsController.StockCount.cs"));
        var voucherIndex = ReadUtf8(Path.Combine(root, "Controllers", "VouchersController.Index.cs"));
        var voucherInbound = ReadUtf8(Path.Combine(root, "Controllers", "VouchersController.Inbound.cs"));
        var inboundExecution = ReadUtf8(Path.Combine(root, "Services", "InboundExecutionService.cs"));
        var outboundExecution = ReadUtf8(Path.Combine(root, "Services", "OutboundExecutionService.cs"));
        var tests = string.Join(Environment.NewLine, EnumerateFiles(Path.Combine(root, "WMS.Tests"), "*.cs").Select(ReadUtf8));

        foreach (var token in new[]
        {
            "ItemLocation.Quantity",
            "`Item.CurrentStock`",
            "cache",
            "CS-AUD-001",
            "CS-AUD-008",
            "Acceptance Cho 100%"
        })
        {
            Assert.Contains(token, audit, StringComparison.Ordinal);
        }

        Assert.Contains("_db.ItemLocations", service, StringComparison.Ordinal);
        Assert.Contains("GroupBy(il => il.ItemId)", service, StringComparison.Ordinal);
        Assert.Contains("SyncCurrentStockAsync", service, StringComparison.Ordinal);
        Assert.Contains("_inventoryBalanceService.GetStockByItemAsync", home, StringComparison.Ordinal);
        Assert.Contains("_inventoryBalanceService.ApplyStockBalances", home, StringComparison.Ordinal);
        Assert.Contains("GetStockByItemAsync(warehouseId, items.Select(i => i.ItemId))", reportsInventory, StringComparison.Ordinal);
        Assert.Contains("_inventoryBalanceService.ApplyStockBalances(items, stockMap)", reportsInventory, StringComparison.Ordinal);
        Assert.Contains("_db.ItemLocations.AsNoTracking()", reportsStockCount, StringComparison.Ordinal);
        Assert.Contains("SystemQty = il.Quantity", reportsStockCount, StringComparison.Ordinal);
        Assert.Contains("var runningStockByItem = await _inventoryBalanceService.GetStockByItemAsync(null, lineItemIds)", voucherIndex, StringComparison.Ordinal);
        Assert.Contains("var runningStockByItem = await _inventoryBalanceService.GetStockByItemAsync(null, approveItemIds)", voucherInbound, StringComparison.Ordinal);
        Assert.Contains("var runningStockByItem = await _inventoryBalanceService.GetStockByItemAsync(null, approveItemIds)", inboundExecution, StringComparison.Ordinal);
        Assert.Contains("var runningStockByItem = await _inventoryBalanceService.GetStockByItemAsync(null, itemIds)", outboundExecution, StringComparison.Ordinal);
        Assert.DoesNotContain("var oldStock = item.CurrentStock;", voucherIndex, StringComparison.Ordinal);
        Assert.DoesNotContain("var oldStock = item.CurrentStock;", voucherInbound, StringComparison.Ordinal);
        Assert.DoesNotContain("var oldStock = item.CurrentStock;", inboundExecution, StringComparison.Ordinal);
        Assert.DoesNotContain("Direct CurrentStock update", outboundExecution, StringComparison.Ordinal);
        Assert.DoesNotContain("var activeAlerts = await _db.StockAlerts.AsNoTracking()", voucherIndex, StringComparison.Ordinal);
        Assert.DoesNotContain("var approveActiveAlerts = await _db.StockAlerts.AsNoTracking()", voucherInbound, StringComparison.Ordinal);
        Assert.Contains("StockValuation_Current_ShouldUseItemLocationQuantityNotCurrentStock", tests, StringComparison.Ordinal);
        Assert.Contains("SyncCurrentStock_ShouldRecalculateFromItemLocations", tests, StringComparison.Ordinal);
    }

    [Fact]
    public void CurrentStockReportFilters_ShouldUseScopedBalanceMapBeforeDisplayCache()
    {
        var root = FindRepositoryRoot();
        var reportsInventory = ReadUtf8(Path.Combine(root, "Controllers", "ReportsController.Inventory.cs"));
        var reportsAnalytics = ReadUtf8(Path.Combine(root, "Controllers", "ReportsController.Analytics.cs"));

        Assert.Contains("stockMap.TryGetValue(i.ItemId, out var scopedQty) && scopedQty > 0", reportsInventory, StringComparison.Ordinal);
        Assert.Contains("stockMap.TryGetValue(i.ItemId, out var scopedQty) && scopedQty > 0", reportsAnalytics, StringComparison.Ordinal);
        Assert.DoesNotContain("items = items.Where(i => i.CurrentStock > 0).ToList();", reportsInventory, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimeDynamicHtml_ShouldEscapeWarehouseStockAndBarcodeFallbacks()
    {
        var root = FindRepositoryRoot();
        var warehouseDetails = ReadUtf8(Path.Combine(root, "Views", "Warehouses", "Details.cshtml"));
        var printLabels = ReadUtf8(Path.Combine(root, "Views", "Items", "PrintLabels.cshtml"));

        Assert.Contains("escapeHtml(item.itemCode || '---')", warehouseDetails, StringComparison.Ordinal);
        Assert.Contains("escapeHtml(item.itemName || '---')", warehouseDetails, StringComparison.Ordinal);
        Assert.Contains("escapeHtml(e?.message || 'Lỗi mạng hoặc tải dữ liệu thất bại!')", warehouseDetails, StringComparison.Ordinal);
        Assert.Contains("document.createElementNS('http://www.w3.org/2000/svg', 'text')", printLabels, StringComparison.Ordinal);
        Assert.Contains("text.textContent = `ERR: ${val}`;", printLabels, StringComparison.Ordinal);
        Assert.DoesNotContain("svg.innerHTML = `<text", printLabels, StringComparison.Ordinal);
    }

    [Fact]
    public void ErrorSafetyGate_ShouldRouteBusinessMessagesThroughUserSafeError()
    {
        var root = FindRepositoryRoot();
        var api = ReadUtf8(Path.Combine(root, "Controllers", "ApiIntegrationController.cs"));
        var inbound = ReadUtf8(Path.Combine(root, "Services", "InboundExecutionService.cs"));
        var outbound = ReadUtf8(Path.Combine(root, "Services", "OutboundExecutionService.cs"));
        var integration = ReadUtf8(Path.Combine(root, "Services", "IntegrationService.cs"));
        var snapshots = ReadUtf8(Path.Combine(root, "Services", "InventorySnapshotService.cs"));
        var replenishment = ReadUtf8(Path.Combine(root, "Services", "ReplenishmentAutomationService.cs"));

        Assert.Contains("UserSafeError.From(ex)", api, StringComparison.Ordinal);
        Assert.Contains("WorkflowResult.Failure(UserSafeError.From(ex)", inbound, StringComparison.Ordinal);
        Assert.Contains("WorkflowResult.Failure(UserSafeError.From(ex)", outbound, StringComparison.Ordinal);
        Assert.Contains("item.LastError = UserSafeError.From(ex", integration, StringComparison.Ordinal);
        Assert.Contains("outbox.LastError = UserSafeError.From(ex", snapshots, StringComparison.Ordinal);
        Assert.Contains("evt.LastError = UserSafeError.From(ex", snapshots, StringComparison.Ordinal);
        Assert.Contains("run.ErrorMessage = UserSafeError.From(ex", snapshots, StringComparison.Ordinal);
        Assert.Contains("line.ErrorMessage = UserSafeError.From(ex", replenishment, StringComparison.Ordinal);
        Assert.DoesNotContain("errors = new[] { ex.Message }", api, StringComparison.Ordinal);
        Assert.DoesNotContain("WorkflowResult.Failure(ex.Message", inbound, StringComparison.Ordinal);
        Assert.DoesNotContain("WorkflowResult.Failure(ex.Message", outbound, StringComparison.Ordinal);
        Assert.DoesNotContain("item.LastError = ex.Message", integration, StringComparison.Ordinal);
        Assert.DoesNotContain("LastError = ex.Message", snapshots, StringComparison.Ordinal);
        Assert.DoesNotContain("ErrorMessage = ex.Message", snapshots, StringComparison.Ordinal);
        Assert.DoesNotContain("ErrorMessage = ex.Message", replenishment, StringComparison.Ordinal);
    }

    [Fact]
    public void PriorityDynamicHtml_ShouldEscapeRuntimeValuesOrUseDomApis()
    {
        var root = FindRepositoryRoot();
        var create = ReadUtf8(Path.Combine(root, "Views", "Vouchers", "Create.cshtml"));
        var details = ReadUtf8(Path.Combine(root, "Views", "Vouchers", "Details.cshtml"));
        var quality = ReadUtf8(Path.Combine(root, "Views", "Operations", "QualityInspection.cshtml"));

        Assert.Contains("System.Text.Json.JsonSerializer.Serialize(d.Item?.ItemCode ?? \"\")", quality, StringComparison.Ordinal);
        Assert.Contains("data-voucher-code=\"@v.VoucherCode\"", quality, StringComparison.Ordinal);
        Assert.Contains("data-wms-call-self=\"openQcFromButton\"", quality, StringComparison.Ordinal);
        Assert.Contains("s.replaceChildren(new Option(", quality, StringComparison.Ordinal);
        Assert.DoesNotContain("s.innerHTML='<option", quality, StringComparison.Ordinal);
        Assert.Contains("appendHiddenInput(form, '__RequestVerificationToken', token)", details, StringComparison.Ordinal);
        Assert.DoesNotContain("form.innerHTML = `<input", details, StringComparison.Ordinal);
        Assert.Contains("suggestionArg(loc.locationCode)", create, StringComparison.Ordinal);
        Assert.Contains("escapeDocumentText(loc.lotNumber)", create, StringComparison.Ordinal);
        Assert.Contains("escapeDocumentText(err?.message || '", create, StringComparison.Ordinal);
        Assert.Contains("const strategyLocation = escapeDocumentText(s.locationCode)", create, StringComparison.Ordinal);
        Assert.DoesNotContain("`${s.locationCode}`", create, StringComparison.Ordinal);
        Assert.DoesNotContain("${s.reason}", create, StringComparison.Ordinal);
    }

    [Fact]
    public void AuditTrailDynamicHtml_ShouldEscapeRuntimeValuesAndUseCssClasses()
    {
        var root = FindRepositoryRoot();
        var auditTrail = ReadUtf8(Path.Combine(root, "Views", "Reports", "AuditTrail.cshtml"));

        Assert.Contains("function escapeAuditText(value)", auditTrail, StringComparison.Ordinal);
        Assert.Contains("return escapeAuditText(String(val));", auditTrail, StringComparison.Ordinal);
        Assert.Contains("class=\"audit-diff-table\"", auditTrail, StringComparison.Ordinal);
        Assert.Contains("${escapeAuditText(translateField(key))}", auditTrail, StringComparison.Ordinal);
        Assert.Contains("${escapeAuditText(translateTable(table))}", auditTrail, StringComparison.Ordinal);
        Assert.DoesNotContain("style" + "=\"padding:8px 12px", auditTrail, StringComparison.Ordinal);
        Assert.DoesNotContain("style" + "=\"text-align:left", auditTrail, StringComparison.Ordinal);
    }

    [Fact]
    public void PriorityMobileViews_ShouldAvoidInlineStyleDebtInRecentlyHardenedViews()
    {
        var root = FindRepositoryRoot();
        var wavePlanning = ReadUtf8(Path.Combine(root, "Views", "Vouchers", "WavePlanning.cshtml"));
        var serialReceiving = ReadUtf8(Path.Combine(root, "Views", "Operations", "SerialReceiving.cshtml"));
        var abcAnalysis = ReadUtf8(Path.Combine(root, "Views", "Reports", "AbcAnalysis.cshtml"));
        var analytics = ReadUtf8(Path.Combine(root, "Views", "Reports", "Analytics.cshtml"));
        var lpnLookup = ReadUtf8(Path.Combine(root, "Views", "Operations", "LpnLookup.cshtml"));
        var receiving = ReadUtf8(Path.Combine(root, "Views", "Operations", "Receiving.cshtml"));

        Assert.Contains("wave-filter-select", wavePlanning, StringComparison.Ordinal);
        Assert.Contains("carrier-chip-group", wavePlanning, StringComparison.Ordinal);
        Assert.Contains("serial-summary-grid", serialReceiving, StringComparison.Ordinal);
        Assert.Contains("serial-form-footer", serialReceiving, StringComparison.Ordinal);
        Assert.Contains("analytics-chart-card", analytics, StringComparison.Ordinal);
        Assert.Contains("lpn-lookup-row", lpnLookup, StringComparison.Ordinal);
        Assert.Contains("receiving-table-toolbar", receiving, StringComparison.Ordinal);
        Assert.DoesNotContain("style" + "=", wavePlanning, StringComparison.Ordinal);
        Assert.DoesNotContain("style" + "=", serialReceiving, StringComparison.Ordinal);
        Assert.DoesNotContain("style" + "=", abcAnalysis, StringComparison.Ordinal);
        Assert.DoesNotContain("style" + "=", analytics, StringComparison.Ordinal);
        Assert.DoesNotContain("style" + "=", lpnLookup, StringComparison.Ordinal);
        Assert.DoesNotContain("style" + "=", receiving, StringComparison.Ordinal);
    }

    [Fact]
    public void KeyAuditMarkdown_ShouldBeReadableUtf8WithoutMojibake()
    {
        var root = FindRepositoryRoot();
        var files = new[]
        {
            "CURRENTSTOCK_SOURCE_OF_TRUTH_AUDIT_2026_05_17.md",
            "ENTERPRISE_WMS_100_PERCENT_REMAINING_TASKS_MOBILE_AUDIT_2026_05_17.md",
            "ENTERPRISE_WMS_100_PERCENT_EVIDENCE_REGISTER_2026_05_17.md"
        };
        var banned = new[]
        {
            TextFromCodePoints(0x004E, 0x0067, 0x00C3),
            TextFromCodePoints(0x004B, 0x00E1, 0x00BA),
            TextFromCodePoints(0x00C4, 0x0090),
            TextFromCodePoints(0x0043, 0x0068, 0x00E1, 0x00BB),
            TextFromCodePoints(0x006E, 0x0067, 0x0075, 0x00E1, 0x00BB),
            TextFromCodePoints(0x006D, 0x00E1, 0x00BB),
            TextFromCodePoints(0x00E1, 0x00BA),
            TextFromCodePoints(0x00E1, 0x00BB)
        };
        var failures = new List<string>();

        foreach (var file in files)
        {
            var content = ReadUtf8(Path.Combine(root, file));
            foreach (var token in banned)
            {
                if (content.Contains(token, StringComparison.Ordinal))
                    failures.Add($"{file} chứa mojibake `{token}`.");
            }
        }

        Assert.True(failures.Count == 0, "Audit docs còn lỗi mã hóa:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void SecurityGate_ShouldUseGlobalAuthCsrfAndScopedExports()
    {
        var root = FindRepositoryRoot();
        var program = ReadUtf8(Path.Combine(root, "Program.cs"));
        var reportsInventory = ReadUtf8(Path.Combine(root, "Controllers", "ReportsController.Inventory.cs"));
        var reportsAnalytics = ReadUtf8(Path.Combine(root, "Controllers", "ReportsController.Analytics.cs"));
        var threePl = ReadUtf8(Path.Combine(root, "Controllers", "OperationsController.ThreePlMhe.cs"));
        var delivery = ReadUtf8(Path.Combine(root, "Controllers", "OperationsController.DeliveryReconciliation.cs"));

        Assert.Contains("AuthorizeFilter(policy)", program, StringComparison.Ordinal);
        Assert.Contains("AutoValidateAntiforgeryTokenAttribute", program, StringComparison.Ordinal);
        Assert.Contains("app.UseAuthentication()", program, StringComparison.Ordinal);
        Assert.Contains("app.UseAuthorization()", program, StringComparison.Ordinal);
        Assert.Contains("app.UseRateLimiter()", program, StringComparison.Ordinal);
        Assert.Contains("X-Content-Type-Options", program, StringComparison.Ordinal);

        foreach (var source in new[] { reportsInventory, reportsAnalytics, delivery })
        {
            Assert.Contains("GetScopedWarehouseId", source, StringComparison.Ordinal);
            Assert.Contains("Authorize", source, StringComparison.Ordinal);
        }

        Assert.Contains("WmsPermissions.ThreePlBillingManage", threePl, StringComparison.Ordinal);
        Assert.Contains("EnsureCanAccessOwnerAsync", threePl, StringComparison.Ordinal);
        Assert.Contains("BuildThreePlBillingExportAsync", threePl, StringComparison.Ordinal);
    }

    [Fact]
    public void SensitivePostActions_ShouldBeProtectedByGlobalCsrfConvention()
    {
        var root = FindRepositoryRoot();
        var program = ReadUtf8(Path.Combine(root, "Program.cs"));
        Assert.Contains("AutoValidateAntiforgeryTokenAttribute", program, StringComparison.Ordinal);

        var controllerTypes = typeof(HomeController).Assembly
            .GetTypes()
            .Where(t => typeof(Controller).IsAssignableFrom(t) && t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .ToList();

        var failures = new List<string>();
        foreach (var controller in controllerTypes)
        {
            foreach (var method in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(IsActionMethod))
            {
                var isPost = method.GetCustomAttribute<HttpPostAttribute>() != null;
                if (!isPost) continue;

                var isAnonymous = method.GetCustomAttribute<AllowAnonymousAttribute>() != null
                    || controller.GetCustomAttribute<AllowAnonymousAttribute>() != null;
                var hasRoleOrPolicy = method.GetCustomAttributes<AuthorizeAttribute>(true).Any()
                    || controller.GetCustomAttributes<AuthorizeAttribute>(true).Any()
                    || program.Contains("AuthorizeFilter(policy)", StringComparison.Ordinal);

                if (!isAnonymous && !hasRoleOrPolicy)
                    failures.Add($"{controller.Name}.{method.Name} thiếu authorization convention.");
            }
        }

        Assert.True(failures.Count == 0, "POST action chưa đạt security convention:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void MajorModules_ShouldHaveBusinessSecurityUiAndAcceptanceCoverageMarkers()
    {
        var root = FindRepositoryRoot();
        var tests = string.Join(Environment.NewLine, EnumerateFiles(Path.Combine(root, "WMS.Tests"), "*.cs").Select(ReadUtf8));
        var acceptance = ReadUtf8(Path.Combine(root, "MODULE_ACCEPTANCE_CHECKLIST.md"));

        foreach (var marker in new[]
        {
            "BusinessLogicHardeningTests",
            "AuthorizationMatrixTests",
            "EnterpriseUiRedesignTests",
            "P411P412RegressionAndUxComplianceTests",
            "YardBillingTests",
            "Epic6CatchWeightAndShipmentLoadTests",
            "ReplenishmentAutomationTests"
        })
        {
            Assert.Contains(marker, tests, StringComparison.Ordinal);
        }

        foreach (var module in new[] { "Inbound", "Outbound", "Inventory", "Users And Security", "Reports And Analytics", "Yard And Dock", "Carrier", "3PL Billing", "Mobile, RF And Offline", "Integration And API" })
            Assert.Contains(module, acceptance, StringComparison.Ordinal);
    }

    private static bool IsActionMethod(MethodInfo method)
    {
        if (method.IsSpecialName) return false;
        if (method.GetCustomAttribute<NonActionAttribute>() != null) return false;
        return typeof(IActionResult).IsAssignableFrom(method.ReturnType)
            || (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
                && typeof(IActionResult).IsAssignableFrom(method.ReturnType.GenericTypeArguments[0]));
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
                && !path.Contains($"{Path.DirectorySeparatorChar}wwwroot{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

    private static string Slice(string content, string startMarker, string endMarker)
    {
        var start = content.IndexOf(startMarker, StringComparison.Ordinal);
        var end = content.IndexOf(endMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Không tìm thấy `{startMarker}`.");
        Assert.True(end > start, $"Không tìm thấy `{endMarker}` sau `{startMarker}`.");
        return content[start..end];
    }

    private static string ReadUtf8(string path)
        => File.ReadAllText(path);

    private static string TextFromCodePoints(params int[] codePoints)
    {
        var builder = new StringBuilder();
        foreach (var codePoint in codePoints)
            builder.Append(char.ConvertFromUtf32(codePoint));
        return builder.ToString();
    }

    private static string ToRelativePath(string root, string path)
        => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "WMS.csproj"))
                && Directory.Exists(Path.Combine(directory.FullName, "WMS.Tests")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Không tìm thấy thư mục gốc dự án WMS.");
    }
}
