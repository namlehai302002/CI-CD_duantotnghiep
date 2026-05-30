using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WMS.Common;
using WMS.Data;
using WMS.Models;
using WMS.Services;
using WMS.ViewModels;
using Xunit;

namespace WMS.Tests;

public sealed class OptimizationAutomationIntegrationCompletionTests
{
    [Fact]
    public async Task Optimization_ShouldCreateSlottingWaveWavelessPickPathAndToteCluster()
    {
        await using var db = CreateDb();
        SeedOptimizationData(db);
        var service = new OptimizationEnterpriseService(db);

        var slotting = await service.RunSlottingOptimizationAsync(1, null, "manager");
        Assert.Equal(OptimizationRunStatusEnum.Completed, slotting.Status);
        var slotLine = Assert.Single(slotting.Lines);
        Assert.Equal(2, slotLine.SuggestedLocationId);
        Assert.True(slotLine.Score > 70);

        var wave = await service.RunWaveOptimizationAsync(1, null, "manager");
        Assert.Contains(wave.Lines, x => x.LineType == OptimizationLineTypeEnum.WaveCandidate && x.InventoryAvailable && x.IsOwnerSafe);

        var released = await service.RunWavelessReleaseAsync(1, 10, null, "staff");
        Assert.Equal(2, released);
        Assert.Equal(2, await db.WavelessReleaseQueue.CountAsync(x => x.Status == WavelessQueueStatusEnum.Released));

        var path = await service.GeneratePickPathPlanAsync(1, null, "staff");
        Assert.True(path.BeforeDistance >= path.AfterDistance);
        Assert.Equal(2, path.StopCount);

        var cluster = await service.CreateToteClusterPlanAsync(1, null, "staff");
        Assert.True(cluster.RequiresToteScan);
        Assert.All(cluster.Assignments, x => Assert.False(x.IsScanned));
        Assert.Single(cluster.Assignments.Select(x => x.OwnerPartnerId).Distinct());
    }

    [Fact]
    public async Task Automation_ShouldRecordTelemetrySimulateFailuresAndRequireOverrideReason()
    {
        await using var db = CreateDb();
        SeedWarehouse(db);
        var service = new AutomationEnterpriseService(db);

        var adapter = await service.SaveAdapterProfileAsync(1, MheSystemTypeEnum.Amr, "AMR-01", "AMR fleet", true, null, "manager");
        Assert.Contains("amr", adapter.CapabilitiesJson, StringComparison.OrdinalIgnoreCase);

        var telemetry = await service.RecordTelemetryAsync(1, "AMR-01", AutomationTelemetryTypeEnum.Heartbeat, "OK", 120, 0, null, "heartbeat", null);
        Assert.Equal(AutomationTelemetryTypeEnum.Heartbeat, telemetry.TelemetryType);

        var run = await service.RunWcsSimulatorAsync(1, WcsSimulatorScenarioEnum.RobotFail, null, "staff");
        Assert.Equal(1, run.ExceptionsOpened);
        var automationExceptions = await db.OperationExceptionCases.Where(x => x.CategoryKey == "AUTOMATION").ToListAsync();
        Assert.Single(automationExceptions);

        var command = await db.MheCommands.FirstAsync();
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.OverrideMheCommandAsync(command.MheCommandId, AutomationOverrideActionEnum.Retry, "", null, "manager"));
        Assert.Equal("AUTO_OVERRIDE_REASON_REQUIRED", ex.Code);

        var ov = await service.OverrideMheCommandAsync(command.MheCommandId, AutomationOverrideActionEnum.Retry, "Restart robot mission after inspection", null, "manager");
        Assert.Equal(AutomationOverrideActionEnum.Retry, ov.Action);
        Assert.Equal(MheCommandStatusEnum.Queued, ov.MheCommand.Status);
    }

    [Fact]
    public async Task Integration_ShouldExposeOpenApiEdiWebhookConnectorAndOutbox()
    {
        await using var db = CreateDb();
        SeedWarehouse(db);
        var service = new EnterpriseIntegrationService(db);

        var openApi = System.Text.Json.JsonSerializer.Serialize(service.BuildOpenApiContract());
        foreach (var token in new[] { "inbound", "outbound", "inventory", "shipment", "3pl" })
            Assert.Contains(token, openApi, StringComparison.OrdinalIgnoreCase);

        var valid = await service.ImportEdiAsync(EdiMessageTypeEnum.Order940, "ISA*00*~ST*940*0001~SE*2*0001~", "940.edi", 1, null, "api");
        Assert.Equal(EdiMessageStatusEnum.Validated, valid.Status);

        var rejected = await service.ImportEdiAsync(EdiMessageTypeEnum.ShipAdvice945, "BAD", "bad.edi", 1, null, "api");
        Assert.Equal(EdiMessageStatusEnum.Rejected, rejected.Status);
        Assert.NotEmpty(rejected.RejectReport);

        var replay = await service.ReplayEdiAsync(valid.EdiMessageId, "api");
        Assert.Equal(valid.EdiMessageId, replay.ReplayOfMessageId);

        await service.SaveWebhookSubscriptionAsync("InventoryChanged", "mock://inventory", "secret", "manager");
        var delivery = await service.EnqueueWebhookAsync("InventoryChanged", new { itemId = 1, qty = 5m });
        Assert.Equal(WebhookDeliveryStatusEnum.Sent, delivery.Status);
        Assert.Equal(64, delivery.Signature.Length);

        var connectors = await service.EnsureConnectorPackAsync("manager");
        Assert.Contains(connectors, x => x.ConnectorType == EnterpriseConnectorTypeEnum.Erp);
        Assert.Contains(connectors, x => x.ConnectorType == EnterpriseConnectorTypeEnum.Tms);
        Assert.Contains(connectors, x => x.ConnectorType == EnterpriseConnectorTypeEnum.Oms);

        var outbox = await service.EmitOutboxEventAsync(OutboxEventTypeEnum.InventoryChanged, "mock://inventory", new { itemId = 1 }, "inventory:1", "ERP");
        var duplicate = await service.EmitOutboxEventAsync(OutboxEventTypeEnum.InventoryChanged, "mock://inventory", new { itemId = 1 }, "inventory:1", "ERP");
        Assert.Equal(outbox.OutboxId, duplicate.OutboxId);
    }

    [Fact]
    public void Enterprise8910StaticArtifacts_ShouldExposeUiApiMigrationAndChecklist()
    {
        var root = FindRepositoryRoot();
        var controller = Read(Path.Combine(root, "Controllers", "OperationsController.Enterprise8910.cs"));
        var api = Read(Path.Combine(root, "Controllers", "ApiIntegrationController.cs"));
        var optimization = Read(Path.Combine(root, "Views", "Operations", "OptimizationDashboard.cshtml"));
        var automation = Read(Path.Combine(root, "Views", "Operations", "AutomationDashboard.cshtml"));
        var integration = Read(Path.Combine(root, "Views", "Operations", "IntegrationDashboard.cshtml"));
        var tasks = Read(Path.Combine(root, "ENTERPRISE_WMS_100_PERCENT_TASKS.md"));
        var migrations = string.Join('\n', Directory.GetFiles(Path.Combine(root, "Migrations")).Select(Path.GetFileName));

        foreach (var action in new[] { "RunSlottingOptimization", "RunWaveOptimization", "RunWavelessRelease", "GeneratePickPathPlan", "CreateToteClusterPlan", "RunWcsSimulator", "OverrideMheCommand", "IntegrationDashboard" })
            Assert.Contains(action, controller, StringComparison.Ordinal);
        Assert.True(controller.Split("[HttpPost]").Length - 1 <= controller.Split("[ValidateAntiForgeryToken]").Length - 1,
            "Every POST in enterprise 8-10 MVC controller must carry anti-forgery.");

        foreach (var token in new[] { "openapi.json", "edi/import", "shipments/{carrierShipmentId:long}/confirm", "3pl/invoices/{invoiceId:long}/issue", "webhooks/{deliveryId:long}/replay" })
            Assert.Contains(token, api, StringComparison.Ordinal);

        Assert.Contains("ExportOptimizationLines", optimization, StringComparison.Ordinal);
        Assert.Contains("RunWcsSimulator", automation, StringComparison.Ordinal);
        Assert.Contains("ReplayWebhookDelivery", integration, StringComparison.Ordinal);
        Assert.Contains("CompleteOptimizationAutomationIntegrationEnterprise", migrations, StringComparison.Ordinal);

        foreach (var code in new[] { "OPT-01", "OPT-02", "OPT-03", "OPT-04", "OPT-05", "AUTO-01", "AUTO-02", "AUTO-03", "AUTO-04", "AUTO-05", "AUTO-06", "INT-01", "INT-02", "INT-03", "INT-04", "INT-05", "INT-06" })
            Assert.Contains($"- [x] `{code}`", tasks, StringComparison.Ordinal);
    }

    [Fact]
    public void EnterpriseUiLabels_ShouldTranslateAdvancedOperationCodes()
    {
        Assert.Equal("Nhận lệnh và hoàn tất", EnterpriseUiLabels.AutomationScenario(WcsSimulatorScenarioEnum.AcceptAndComplete));
        Assert.Equal("Nhịp kết nối", EnterpriseUiLabels.AutomationTelemetryType(AutomationTelemetryTypeEnum.Heartbeat));
        Assert.Equal("Gửi lại", EnterpriseUiLabels.AutomationOverrideAction(AutomationOverrideActionEnum.Retry));
        Assert.Equal("Đề xuất áp dụng", EnterpriseUiLabels.OptimizationStatus("Recommend"));
        Assert.Equal("Thiếu tồn khả dụng", EnterpriseUiLabels.OptimizationStatus("InventoryShort"));
        Assert.Equal("ERP - quản trị doanh nghiệp", EnterpriseUiLabels.ConnectorType(EnterpriseConnectorTypeEnum.Erp));
        Assert.Equal("Tồn kho thay đổi", EnterpriseUiLabels.IntegrationEvent("InventoryChanged"));
        Assert.Equal("Nhập kho", EnterpriseUiLabels.DockAppointmentDirection(DockAppointmentDirectionEnum.Inbound));
        Assert.Equal("Giấy tờ tài xế", EnterpriseUiLabels.YardEvidenceType(YardEvidenceTypeEnum.DriverDocument));
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static void SeedWarehouse(AppDbContext db)
    {
        db.Warehouses.Add(new Warehouse { WarehouseId = 1, WarehouseCode = "WH01", WarehouseName = "Main" });
        db.SaveChanges();
    }

    private static void SeedOptimizationData(AppDbContext db)
    {
        db.Warehouses.Add(new Warehouse { WarehouseId = 1, WarehouseCode = "WH01", WarehouseName = "Main" });
        db.Partners.Add(new Partner { PartnerId = 10, PartnerCode = "OWN01", PartnerName = "Owner 01", PartnerType = PartnerTypeEnum.Customer, IsActive = true });
        db.Zones.Add(new Zone { ZoneId = 1, WarehouseId = 1, ZoneCode = "PICK", ZoneName = "Pick face" });
        db.Locations.AddRange(
            new Location { LocationId = 1, ZoneId = 1, LocationCode = "C-09-03", AisleSequence = 9, RackCode = "09", BinCode = "03", HeightLevel = 4, IsGoldenZone = false, MaxCapacity = 1000, IsActive = true },
            new Location { LocationId = 2, ZoneId = 1, LocationCode = "A-01-01", AisleSequence = 1, RackCode = "01", BinCode = "01", HeightLevel = 1, IsGoldenZone = true, MaxCapacity = 1000, IsActive = true });
        db.Items.Add(new Item { ItemId = 1, ItemCode = "FAST", ItemName = "Fast mover", BaseUomId = 1, OwnerPartnerId = 10, AbcClass = "A", DefaultLocationId = 1, IsActive = true });
        db.ItemVelocityClassifications.Add(new ItemVelocityClassification { ClassificationId = 1, ItemId = 1, WarehouseId = 1, AbcClass = 'A', XyzClass = 'X', CombinedClass = "AX", PickCount = 100, DailyPickFrequency = 10, IsActive = true });
        db.ItemLocations.Add(new ItemLocation { ItemLocationId = 1, ItemId = 1, OwnerPartnerId = 10, LocationId = 1, Quantity = 100, ReservedQty = 0, HoldStatus = InventoryHoldStatusEnum.Available });
        db.Vouchers.AddRange(
            new Voucher { VoucherId = 1, VoucherCode = "SO-1", VoucherType = VoucherTypeEnum.XuatKho, WarehouseId = 1, PartnerId = 20, OwnerPartnerId = 10, Priority = 90, CarrierName = "FASTSHIP", SlaCode = "R1", RequestedDeliveryDate = VietnamTime.Now.Date, CreatedBy = "test" },
            new Voucher { VoucherId = 2, VoucherCode = "SO-2", VoucherType = VoucherTypeEnum.XuatKho, WarehouseId = 1, PartnerId = 20, OwnerPartnerId = 10, Priority = 80, CarrierName = "FASTSHIP", SlaCode = "R1", RequestedDeliveryDate = VietnamTime.Now.Date, CreatedBy = "test" });
        db.PickTasks.AddRange(
            new PickTask { PickTaskId = 1, TaskCode = "PICK-1", VoucherId = 1, ItemId = 1, OwnerPartnerId = 10, SourceLocationId = 1, TargetQty = 5, Status = PickTaskStatusEnum.Pending },
            new PickTask { PickTaskId = 2, TaskCode = "PICK-2", VoucherId = 2, ItemId = 1, OwnerPartnerId = 10, SourceLocationId = 1, TargetQty = 7, Status = PickTaskStatusEnum.Pending });
        db.PickCarts.Add(new PickCart { PickCartId = 1, CartCode = "CART-1", WarehouseId = 1, ToteCapacity = 4, Status = PickCartStatusEnum.Available });
        db.PickTotes.AddRange(
            new PickTote { PickToteId = 1, ToteCode = "TOTE-1", PickCartId = 1, Status = PickToteStatusEnum.Empty },
            new PickTote { PickToteId = 2, ToteCode = "TOTE-2", PickCartId = 1, Status = PickToteStatusEnum.Empty });
        db.SaveChanges();
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

    private static string Read(string path)
        => File.ReadAllText(path);
}
