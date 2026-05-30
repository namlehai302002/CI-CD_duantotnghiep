namespace WMS.Tests;

public sealed class CoreControllerRefactorTests
{
    [Fact]
    public void Program_ShouldRegisterCoreControllerRefactorServices()
    {
        var root = FindRepositoryRoot();
        var program = ReadUtf8(Path.Combine(root, "Program.cs"));

        foreach (var registration in new[]
        {
            "AddScoped<IOperationsScopeQueryService, OperationsScopeQueryService>",
            "AddScoped<ISlottingPlanningService, SlottingPlanningService>",
            "AddScoped<IOperationExceptionQueryService, OperationExceptionQueryService>",
            "AddScoped<IYardBillingQueryService, YardBillingQueryService>",
            "AddScoped<IVoucherCreateWorkflowService, VoucherCreateWorkflowService>",
            "AddScoped<IVoucherDetailQueryService, VoucherDetailQueryService>",
            "AddScoped<IVoucherImportQueryService, VoucherImportQueryService>",
            "AddScoped<IVoucherSharedRuleService, VoucherSharedRuleService>"
        })
        {
            Assert.Contains(registration, program, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void OperationsController_ShouldDelegateRepeatedQueriesAndKeepPublicRoutes()
    {
        var root = FindRepositoryRoot();
        var controller = ReadUtf8(Path.Combine(root, "Controllers", "OperationsController.cs"));
        var yard = ReadUtf8(Path.Combine(root, "Controllers", "OperationsController.YardManagement.cs"));
        var inventory = ReadUtf8(Path.Combine(root, "Controllers", "OperationsController.Inventory.cs"));

        Assert.Contains("public async Task<IActionResult> AnalyzeItemVelocity", controller, StringComparison.Ordinal);
        Assert.Contains("public async Task<IActionResult> GetVelocityHeatmap", controller, StringComparison.Ordinal);
        Assert.Contains("public async Task<IActionResult> ExportYardBillingChargesExcel", controller, StringComparison.Ordinal);
        Assert.Contains("_slottingPlanningService.AnalyzeItemVelocityAsync", controller, StringComparison.Ordinal);
        Assert.Contains("_slottingPlanningService.GetVelocityHeatmapAsync", controller, StringComparison.Ordinal);
        Assert.Contains("_operationExceptionQueryService.MapSeverity", controller, StringComparison.Ordinal);
        Assert.Contains("_yardBillingQueryService", controller, StringComparison.Ordinal);
        Assert.Contains(".BuildChargeQuery(", controller, StringComparison.Ordinal);
        Assert.Contains("_yardBillingQueryService.BuildChargeQuery", yard, StringComparison.Ordinal);
        Assert.Contains("_slottingPlanningService.ScoreSlottingCandidate", inventory, StringComparison.Ordinal);

        Assert.DoesNotContain("private IQueryable<YardBillingCharge> BuildYardChargeQuery", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("private static List<YardBillingChargeRow> MapYardChargeRows", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("private static string YardChargeStatusText", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("private static SlottingCandidateScore? ScoreSlottingCandidate", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("private async Task<SlottingSimulationLine?> BuildSlottingSimulationLineAsync", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void VouchersController_ShouldDelegateSharedRulesAndKeepRoutes()
    {
        var root = FindRepositoryRoot();
        var controller = ReadUtf8(Path.Combine(root, "Controllers", "VouchersController.cs"));
        var helpers = ReadUtf8(Path.Combine(root, "Controllers", "VouchersController.Helpers.cs"));
        var import = ReadUtf8(Path.Combine(root, "Controllers", "VouchersController.Import.cs"));
        var index = ReadUtf8(Path.Combine(root, "Controllers", "VouchersController.Index.cs"));
        var inbound = ReadUtf8(Path.Combine(root, "Controllers", "VouchersController.Inbound.cs"));

        Assert.Contains("public async Task<IActionResult> Create", index, StringComparison.Ordinal);
        Assert.Contains("public async Task<IActionResult> AnalyzeReceipt", import, StringComparison.Ordinal);
        Assert.Contains("public async Task<IActionResult> ApproveInbound", inbound, StringComparison.Ordinal);
        Assert.Contains("public async Task<IActionResult> RejectInbound", inbound, StringComparison.Ordinal);

        Assert.Contains("_voucherSharedRuleService.EnforceSod", controller, StringComparison.Ordinal);
        Assert.Contains("_voucherSharedRuleService.GetScopedWarehouseId", controller, StringComparison.Ordinal);
        Assert.Contains("_voucherSharedRuleService.ResolveConversionRate", controller, StringComparison.Ordinal);
        Assert.Contains("_voucherCreateWorkflowService.BuildItemAllowedSourceUomsJsonAsync", helpers, StringComparison.Ordinal);
        Assert.Contains("_voucherImportQueryService.StoreLegacyReceiptDocumentAsync", import, StringComparison.Ordinal);
        Assert.Contains("_voucherImportQueryService.ResolvePrivateReceiptPath", import, StringComparison.Ordinal);
    }

    [Fact]
    public void CoreControllers_ShouldRequireRegisteredServicesInsteadOfConstructingFallbacks()
    {
        var root = FindRepositoryRoot();
        var operations = ReadUtf8(Path.Combine(root, "Controllers", "OperationsController.cs"));
        var vouchers = ReadUtf8(Path.Combine(root, "Controllers", "VouchersController.cs"));

        foreach (var source in new[] { operations, vouchers })
        {
            Assert.DoesNotContain("?? new ", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Service? ", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RefactorServices_ShouldContainThinServiceContracts()
    {
        var root = FindRepositoryRoot();
        var services = ReadUtf8(Path.Combine(root, "Services", "CoreControllerRefactorServices.cs"));

        foreach (var contract in new[]
        {
            "public interface IOperationsScopeQueryService",
            "public interface ISlottingPlanningService",
            "public interface IOperationExceptionQueryService",
            "public interface IYardBillingQueryService",
            "public interface IVoucherCreateWorkflowService",
            "public interface IVoucherDetailQueryService",
            "public interface IVoucherImportQueryService",
            "public interface IVoucherSharedRuleService"
        })
        {
            Assert.Contains(contract, services, StringComparison.Ordinal);
        }

        Assert.Contains("class SlottingPlanningService", services, StringComparison.Ordinal);
        Assert.Contains("class VoucherSharedRuleService", services, StringComparison.Ordinal);
        Assert.Contains("class VoucherImportQueryService", services, StringComparison.Ordinal);
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

        throw new DirectoryNotFoundException("Could not locate WMS.sln.");
    }

    private static string ReadUtf8(string path)
        => File.ReadAllText(path);
}
