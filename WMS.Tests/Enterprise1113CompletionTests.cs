using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WMS.Authorization;
using WMS.Common;
using WMS.Data;
using WMS.Models;
using WMS.Services;
using Xunit;

namespace WMS.Tests;

public sealed class Enterprise1113CompletionTests
{
    [Fact]
    public async Task BiPredictiveAuditAndAssistant_ShouldRespectScopeCitationAndMutationBlock()
    {
        await using var db = CreateDb();
        SeedEnterpriseAnalyticsData(db);
        var service = new EnterpriseAnalyticsService(db);
        var manager = Principal("manager", "Manager", warehouseId: 1, canSeeFinancial: true);

        var semantic = await service.BuildSemanticDashboardAsync(1, 30, canSeeFinancial: true);
        Assert.Contains(semantic.Definitions, x => x.MetricCode == "inventory.total_stock");
        Assert.Contains(semantic.Snapshots, x => x.MetricDefinition.MetricCode == "billing.total_cost" && x.MetricValue == 150000m);

        var financial = await service.BuildFinancialCostDashboardAsync(1, 30, canSeeFinancial: true);
        Assert.Equal(157500m, financial.TotalCost);
        Assert.Contains(financial.Rows, x => x.SourceType == "3PL invoice" && x.SourceCode == "INV-001");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.BuildFinancialCostDashboardAsync(1, 30, canSeeFinancial: false));

        var alerts = await service.BuildPredictiveAlertsAsync(1);
        Assert.Contains(alerts.Alerts, x => x.AlertType == PredictiveAlertTypeEnum.StockoutRisk);
        Assert.Contains(alerts.Alerts, x => x.AlertType == PredictiveAlertTypeEnum.SlaDelay);
        Assert.Contains(alerts.Alerts, x => x.AlertType == PredictiveAlertTypeEnum.ExpiryRisk);

        var audit = await service.BuildAuditAnalyticsAsync();
        Assert.True(audit.SensitiveExportCount >= 1);
        Assert.True(audit.ScopeDeniedCount >= 1);
        Assert.True(audit.OutOfHoursCount >= 1);

        var answer = await service.AskAssistantAsync(manager, "Tóm tắt tồn kho và SLA", null);
        Assert.False(answer.IsMutationBlocked);
        Assert.NotEmpty(answer.Citations);
        var inventoryAnswer = await service.AskAssistantAsync(manager, "Rà soát giúp tôi xem tồn kho còn bao nhiêu", null);
        Assert.False(inventoryAnswer.IsMutationBlocked);
        Assert.Contains("Tổng tồn kho hiện tại", inventoryAnswer.Response);
        Assert.Contains("đơn vị tồn kho", inventoryAnswer.Response);
        Assert.Contains("Nguồn đối chiếu: ItemLocation", inventoryAnswer.Response);
        Assert.DoesNotContain("Tổng chi phí 3PL", inventoryAnswer.Response);
        Assert.DoesNotContain("Năng suất lao động", inventoryAnswer.Response);

        var itemAnswer = await service.AskAssistantAsync(manager, "SKU-LOW còn bao nhiêu hàng trong kho?", null);
        Assert.Contains("Vật tư [SKU-LOW]", itemAnswer.Response);
        Assert.Contains("Pcs", itemAnswer.Response);
        Assert.Contains("Khả dụng", itemAnswer.Response);
        Assert.Contains("Tóm tắt", answer.Response);

        var blocked = await service.AskAssistantAsync(manager, "Xóa phiếu trễ SLA giúp tôi", answer.Session.AiAssistantSessionId);
        Assert.True(blocked.IsMutationBlocked);
        Assert.Contains("bị chặn", blocked.Response);
    }

    [Fact]
    public async Task VoucherCreateWorkflow_ShouldAlwaysExposeBaseUomForSelectableItems()
    {
        await using var db = CreateDb();
        db.UnitsOfMeasure.AddRange(
            new UnitOfMeasure { UomId = 10, UomCode = "Pcs", UomName = "Cái", UomGroup = "Count", IsActive = true },
            new UnitOfMeasure { UomId = 11, UomCode = "Box", UomName = "Thùng", UomGroup = "Count", IsActive = true });
        db.Items.Add(new Item { ItemId = 10, ItemCode = "SKU-UOM", ItemName = "Vật tư kiểm đơn vị", BaseUomId = 10, IsActive = true });
        db.UnitConversions.Add(new UnitConversion { ConversionId = 10, ItemId = 10, FromUomId = 11, ToUomId = 10, ConversionRate = 12, IsActive = true });
        await db.SaveChangesAsync();

        var service = new VoucherCreateWorkflowService(db);
        var json = await service.BuildItemAllowedSourceUomsJsonAsync(await db.Items.AsNoTracking().ToListAsync());
        var map = JsonSerializer.Deserialize<Dictionary<int, List<int>>>(json) ?? new();

        Assert.True(map.TryGetValue(10, out var allowed));
        Assert.Contains(10, allowed);
        Assert.Contains(11, allowed);
        Assert.DoesNotContain(0, allowed);
    }

    [Fact]
    public async Task RoleWorkflowAndSre_ShouldBuildRoleWorkspaceSaveWorkflowAndCaptureTelemetry()
    {
        await using var db = CreateDb();
        SeedEnterpriseAnalyticsData(db);

        var workspaceService = new RoleWorkspaceService();
        var adminWorkspace = workspaceService.Build(Principal("admin", "Admin", null, true));
        Assert.Equal("Admin", adminWorkspace.RoleKey);
        Assert.Equal("Quản trị viên", adminWorkspace.RoleLabel);
        Assert.Contains(adminWorkspace.QuickActions, x => x.Url == "/System/SreDashboard");
        Assert.Contains(adminWorkspace.QuickActions, x => x.Label == "Báo cáo dữ liệu");
        Assert.Contains(adminWorkspace.QuickActions, x => x.Label == "Giám sát hệ thống");
        Assert.Contains(adminWorkspace.QuickActions, x => x.Label == "Cấu hình quy trình");
        Assert.DoesNotContain(adminWorkspace.QuickActions, x => x.Label.Contains("BI semantic", StringComparison.OrdinalIgnoreCase) || x.Label.Contains("Workflow", StringComparison.OrdinalIgnoreCase) || x.Label.Equals("SRE", StringComparison.OrdinalIgnoreCase));

        var staffWorkspace = workspaceService.Build(Principal("staff", "Staff", 1, false));
        Assert.Equal("Staff", staffWorkspace.RoleKey);
        Assert.Equal("Nhân viên kho", staffWorkspace.RoleLabel);
        Assert.DoesNotContain(staffWorkspace.QuickActions, x => x.Url == "/Users");

        db.WarehouseWorkflowProfiles.Add(new WarehouseWorkflowProfile
        {
            WarehouseId = 1,
            ModuleKey = "picking",
            ProfileName = "Lấy hàng chuẩn",
            RequireLocationScan = true,
            RequireItemScan = true,
            RequireToteScan = true,
            RequirePacking = true,
            UpdatedBy = "manager"
        });
        await db.SaveChangesAsync();
        Assert.True(await db.WarehouseWorkflowProfiles.AnyAsync(x => x.ModuleKey == "picking" && x.RequireToteScan));

        var sre = new ProductionSreService(db);
        await sre.RecordRequestAsync(new RequestTelemetryLog
        {
            CorrelationId = "corr-1113",
            Method = "GET",
            Path = "/Reports/SemanticBi",
            StatusCode = 200,
            DurationMs = 120,
            UserName = "manager",
            WarehouseId = 1
        });
        await sre.RecordRequestAsync(new RequestTelemetryLog
        {
            CorrelationId = "corr-1113-error",
            Method = "GET",
            Path = "/Reports/PredictiveAlerts",
            StatusCode = 500,
            DurationMs = 1900,
            IsError = true,
            UserName = "manager",
            WarehouseId = 1
        });
        db.IntegrationOutbox.Add(new IntegrationOutbox { EventType = "WebhookDelivery", TargetEndpoint = "mock://sre", TargetSystem = "Webhook", Status = OutboxStatusEnum.Pending, Payload = "{}" });
        await db.SaveChangesAsync();

        var dashboard = await sre.BuildDashboardAsync(60);
        Assert.Equal(2, dashboard.Snapshot.RequestCount);
        Assert.Equal(1, dashboard.Snapshot.ErrorCount);
        Assert.True(dashboard.Snapshot.QueueDepth >= 1);
        Assert.Contains(dashboard.RecentRequests, x => x.CorrelationId == "corr-1113");
    }

    [Fact]
    public async Task PredictiveStockout_ShouldUseWarehouseScopedItemLocationsInsteadOfCurrentStock()
    {
        await using var db = CreateDb();
        db.Warehouses.AddRange(
            new Warehouse { WarehouseId = 1, WarehouseCode = "WH-A", WarehouseName = "Kho A", IsActive = true },
            new Warehouse { WarehouseId = 2, WarehouseCode = "WH-B", WarehouseName = "Kho B", IsActive = true });
        db.Zones.AddRange(
            new Zone { ZoneId = 1, WarehouseId = 1, ZoneCode = "A", ZoneName = "A", IsActive = true },
            new Zone { ZoneId = 2, WarehouseId = 2, ZoneCode = "B", ZoneName = "B", IsActive = true });
        db.Locations.AddRange(
            new Location { LocationId = 1, ZoneId = 1, LocationCode = "A-01", IsActive = true, MaxCapacity = 100 },
            new Location { LocationId = 2, ZoneId = 2, LocationCode = "B-01", IsActive = true, MaxCapacity = 100 });
        db.Items.Add(new Item { ItemId = 10, ItemCode = "SKU-SCOPE", ItemName = "Scoped low stock", IsActive = true, CurrentStock = 999, MinThreshold = 5, UnitCost = 1 });
        db.ItemLocations.AddRange(
            new ItemLocation { ItemLocationId = 10, ItemId = 10, LocationId = 1, Quantity = 2, ReservedQty = 0, HoldStatus = InventoryHoldStatusEnum.Available },
            new ItemLocation { ItemLocationId = 11, ItemId = 10, LocationId = 2, Quantity = 20, ReservedQty = 0, HoldStatus = InventoryHoldStatusEnum.Available });
        await db.SaveChangesAsync();

        var service = new EnterpriseAnalyticsService(db);

        var warehouseA = await service.BuildPredictiveAlertsAsync(1);
        var alert = Assert.Single(warehouseA.Alerts, x => x.AlertType == PredictiveAlertTypeEnum.StockoutRisk && x.Title.Contains("SKU-SCOPE", StringComparison.Ordinal));
        Assert.Equal(1, alert.WarehouseId);

        using var citation = JsonDocument.Parse(alert.CitationJson);
        Assert.Equal(2m, citation.RootElement.GetProperty("availableQty").GetDecimal());
        Assert.Equal(5m, citation.RootElement.GetProperty("minThreshold").GetDecimal());

        var warehouseB = await service.BuildPredictiveAlertsAsync(2);
        Assert.DoesNotContain(warehouseB.Alerts, x => x.AlertType == PredictiveAlertTypeEnum.StockoutRisk && x.Title.Contains("SKU-SCOPE", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PredictiveStockout_ShouldSeparateOwnersAndIgnoreUnavailableHoldStock()
    {
        await using var db = CreateDb();
        db.Warehouses.Add(new Warehouse { WarehouseId = 1, WarehouseCode = "WH1", WarehouseName = "Kho 1", IsActive = true });
        db.Zones.Add(new Zone { ZoneId = 1, WarehouseId = 1, ZoneCode = "A", ZoneName = "A", IsActive = true });
        db.Locations.Add(new Location { LocationId = 1, ZoneId = 1, LocationCode = "A-01", IsActive = true, MaxCapacity = 100 });
        db.Partners.AddRange(
            new Partner { PartnerId = 10, PartnerCode = "OWN10", PartnerName = "Owner 10", IsActive = true, IsThreePlClient = true },
            new Partner { PartnerId = 20, PartnerCode = "OWN20", PartnerName = "Owner 20", IsActive = true, IsThreePlClient = true });
        db.Items.Add(new Item { ItemId = 20, ItemCode = "SKU-HOLD", ItemName = "Hold stock", IsActive = true, CurrentStock = 999, MinThreshold = 5, UnitCost = 1 });
        db.ItemLocations.AddRange(
            new ItemLocation { ItemLocationId = 20, ItemId = 20, OwnerPartnerId = 10, LocationId = 1, Quantity = 6, ReservedQty = 0, HoldStatus = InventoryHoldStatusEnum.Consigned },
            new ItemLocation { ItemLocationId = 21, ItemId = 20, OwnerPartnerId = 20, LocationId = 1, Quantity = 100, ReservedQty = 0, HoldStatus = InventoryHoldStatusEnum.Blocked });
        await db.SaveChangesAsync();

        var alerts = await new EnterpriseAnalyticsService(db).BuildPredictiveAlertsAsync(1);
        Assert.DoesNotContain(alerts.Alerts, x => x.AlertType == PredictiveAlertTypeEnum.StockoutRisk && x.OwnerPartnerId == 10);

        var owner20 = Assert.Single(alerts.Alerts, x => x.AlertType == PredictiveAlertTypeEnum.StockoutRisk && x.OwnerPartnerId == 20);
        using var citation = JsonDocument.Parse(owner20.CitationJson);
        Assert.Equal(0m, citation.RootElement.GetProperty("availableQty").GetDecimal());
    }

    [Fact]
    public void Enterprise1113StaticArtifacts_ShouldExposeRoutesConfigDocsScaffoldsAndChecklist()
    {
        var root = FindRepositoryRoot();
        var reports = Read(Path.Combine(root, "Controllers", "ReportsController.Enterprise1113.cs"));
        var operations = Read(Path.Combine(root, "Controllers", "OperationsController.WorkflowProfiles.cs"));
        var system = Read(Path.Combine(root, "Controllers", "SystemController.cs"));
        var program = Read(Path.Combine(root, "Program.cs"));
        var appsettings = Read(Path.Combine(root, "appsettings.json"));
        var tasks = Read(Path.Combine(root, "ENTERPRISE_WMS_100_PERCENT_TASKS.md"));
        var visual = Read(Path.Combine(root, "tests", "visual", "wms-visual-regression.spec.ts"));
        var load = Read(Path.Combine(root, "tests", "load", "k6-wms-dod.js"));
        var migrationDoc = Read(Path.Combine(root, "PRODUCTION_MIGRATION_VALIDATION.md"));

        foreach (var token in new[] { "SemanticBi", "FinancialCostDashboard", "PredictiveAlerts", "AuditAnalytics", "AiAssistant", "AskAiAssistant" })
            Assert.Contains(token, reports, StringComparison.Ordinal);
        Assert.Contains("WorkflowProfiles", operations, StringComparison.Ordinal);
        Assert.Contains("SaveWorkflowProfile", operations, StringComparison.Ordinal);
        Assert.Contains("SreDashboard", system, StringComparison.Ordinal);
        Assert.Contains("ExportSreSnapshot", system, StringComparison.Ordinal);
        Assert.Contains("UseWmsCorrelationTelemetry", program, StringComparison.Ordinal);
        Assert.Contains("X-Correlation-ID", Read(Path.Combine(root, "Services", "Enterprise1113Services.cs")), StringComparison.Ordinal);

        foreach (var token in new[] { "AnalyticsGovernance", "RoleWorkspace", "ProductionSre", "TelemetrySamplingPercent" })
            Assert.Contains(token, appsettings, StringComparison.Ordinal);

        foreach (var code in new[] { "BI-01", "BI-02", "BI-03", "BI-04", "BI-05", "UX-01", "UX-02", "UX-03", "UX-04", "UX-05", "UX-06", "PROD-01", "PROD-02", "PROD-03", "PROD-04", "PROD-05", "PROD-06", "PROD-07" })
            Assert.Contains($"- [x] `{code}`", tasks, StringComparison.Ordinal);

        foreach (var token in new[] { "semantic-bi", "predictive-alerts", "ai-assistant", "workflow-profiles", "sre-dashboard" })
            Assert.Contains(token, visual, StringComparison.Ordinal);
        foreach (var token in new[] { "WMS_LOAD_PROFILE", "1000", "bi_sre_dashboards", "biSreDashboards" })
            Assert.Contains(token, load, StringComparison.Ordinal);
        foreach (var token in new[] { "Dry Run", "Rollback Plan", "Seed And Drift Validation", "dotnet ef migrations script --idempotent" })
            Assert.Contains(token, migrationDoc, StringComparison.Ordinal);

        foreach (var view in new[]
        {
            "SemanticBi.cshtml",
            "FinancialCostDashboard.cshtml",
            "PredictiveAlerts.cshtml",
            "AuditAnalytics.cshtml",
            "AiAssistant.cshtml"
        })
        {
            var content = Read(Path.Combine(root, "Views", "Reports", view));
            Assert.Contains("enterprise-section", content, StringComparison.Ordinal);
            Assert.Contains("empty-state", content, StringComparison.Ordinal);
        }
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("enterprise1113-" + Guid.NewGuid())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static void SeedEnterpriseAnalyticsData(AppDbContext db)
    {
        var now = VietnamTime.Now;
        db.UnitsOfMeasure.Add(new UnitOfMeasure { UomId = 1, UomCode = "Pcs", UomName = "Cái", UomGroup = "Count", IsActive = true });
        db.Warehouses.Add(new Warehouse { WarehouseId = 1, WarehouseCode = "WH1", WarehouseName = "Kho 1", IsActive = true });
        db.Zones.Add(new Zone { ZoneId = 1, WarehouseId = 1, ZoneCode = "A", ZoneName = "Zone A", IsActive = true });
        db.Locations.Add(new Location { LocationId = 1, ZoneId = 1, LocationCode = "A-01", MaxCapacity = 100, IsActive = true });
        db.Partners.Add(new Partner { PartnerId = 1, PartnerCode = "OWN1", PartnerName = "Owner 1", IsActive = true, IsThreePlClient = true });
        db.Items.Add(new Item { ItemId = 1, ItemCode = "SKU-LOW", ItemName = "Low stock", BaseUomId = 1, IsActive = true, CurrentStock = 999, MinThreshold = 5, UnitCost = 1000 });
        db.ItemLocations.Add(new ItemLocation { ItemLocationId = 1, ItemId = 1, LocationId = 1, Quantity = 2, ReservedQty = 1, LotNumber = "L1", ExpiryDate = now.Date.AddDays(5) });
        db.Vouchers.Add(new Voucher
        {
            VoucherId = 1,
            VoucherCode = "PX-OVERDUE",
            VoucherType = VoucherTypeEnum.XuatKho,
            WarehouseId = 1,
            OwnerPartnerId = 1,
            RequestedDeliveryDate = now.Date.AddDays(-1),
            VoucherDate = now.Date,
            CreatedBy = "manager",
            IsPosted = false,
            IsCancelled = false
        });
        db.ThreePlInvoices.Add(new ThreePlInvoice
        {
            ThreePlInvoiceId = 1,
            InvoiceCode = "INV-001",
            WarehouseId = 1,
            OwnerPartnerId = 1,
            PeriodFrom = now.Date.AddDays(-30),
            PeriodTo = now.Date,
            ApiPublicId = "api-inv-001",
            CreatedAt = now,
            TotalAmount = 150000
        });
        db.ThreePlInvoiceLines.Add(new ThreePlInvoiceLine
        {
            ThreePlInvoiceLineId = 1,
            ThreePlInvoiceId = 1,
            ChargeType = ThreePlChargeTypeEnum.Storage,
            Description = "Storage",
            Quantity = 3,
            UnitRate = 50000,
            TotalAmount = 150000
        });
        db.LaborActivities.Add(new LaborActivity
        {
            LaborActivityId = 1,
            ActivityCode = "LAB-001",
            WarehouseId = 1,
            UserName = "staff",
            TaskType = "Picking",
            TaskSourceType = "PickTask",
            StartedAt = now.AddHours(-1),
            ActualMinutes = 3,
            WorkQuantity = 1,
            ProductivityPercent = 120
        });
        db.AuditLogs.AddRange(
            new AuditLog { AuditLogId = 1, TableName = "Security", RecordId = "Export", ActionType = "EXPORT", ChangedBy = "manager", ChangedAt = now },
            new AuditLog { AuditLogId = 2, TableName = "Security", RecordId = "Scope", ActionType = "DENIED", ChangedBy = "staff", ChangedAt = now });
        db.LoginAuditLogs.Add(new LoginAuditLog { LoginAuditLogId = 1, UserName = "night.user", IsSuccess = true, Outcome = "LOGIN_OK", CreatedAt = now.Date.AddHours(23) });
        db.SaveChanges();
    }

    private static ClaimsPrincipal Principal(string name, string role, int? warehouseId, bool canSeeFinancial)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Role, role)
        };
        if (warehouseId.HasValue) claims.Add(new Claim("WarehouseId", warehouseId.Value.ToString()));
        if (canSeeFinancial) claims.Add(new Claim(PermissionClaimTypes.Permission, WmsPermissions.ReportViewFinancial));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static string FindRepositoryRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "WMS.sln")))
            dir = Directory.GetParent(dir)?.FullName;
        return dir ?? throw new DirectoryNotFoundException("Repository root not found.");
    }

    private static string Read(string path) => File.ReadAllText(path);
}
