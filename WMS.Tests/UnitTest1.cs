using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Authorization;
using WMS.Controllers;
using WMS.Data;
using WMS.Models;
using WMS.Services;

namespace WMS.Tests;

public class AuthorizationMatrixTests
{
    [Theory]
    [InlineData(typeof(UsersController), nameof(UsersController.Index), new[] { "Admin" })]
    [InlineData(typeof(UsersController), nameof(UsersController.Create), new[] { "Admin" })]
    [InlineData(typeof(UsersController), nameof(UsersController.ResetPassword), new[] { "Admin" })]
    [InlineData(typeof(UsersController), nameof(UsersController.LoginHelpRequests), new[] { "Admin" })]
    [InlineData(typeof(UsersController), nameof(UsersController.MarkLoginHelpInReview), new[] { "Admin" })]
    [InlineData(typeof(UsersController), nameof(UsersController.ResolveLoginHelpRequest), new[] { "Admin" })]
    [InlineData(typeof(UsersController), nameof(UsersController.RejectLoginHelpRequest), new[] { "Admin" })]
    [InlineData(typeof(UsersController), nameof(UsersController.Delete), new[] { "Admin" })]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.Index), new[] { "Admin", "Manager" })]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.Create), new[] { "Admin", "Manager" })]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.Edit), new[] { "Admin", "Manager" })]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.Delete), new[] { "Admin" })]
    [InlineData(typeof(UnitsController), nameof(UnitsController.Index), new[] { "Admin", "Manager" })]
    [InlineData(typeof(UnitsController), nameof(UnitsController.Create), new[] { "Admin", "Manager" })]
    [InlineData(typeof(UnitsController), nameof(UnitsController.Delete), new[] { "Admin", "Manager" })]
    [InlineData(typeof(PartnersController), nameof(PartnersController.Index), new[] { "Admin", "Manager" })]
    [InlineData(typeof(PartnersController), nameof(PartnersController.Create), new[] { "Admin", "Manager" })]
    [InlineData(typeof(PartnersController), nameof(PartnersController.Edit), new[] { "Admin", "Manager" })]
    [InlineData(typeof(PartnersController), nameof(PartnersController.Delete), new[] { "Admin" })]
    [InlineData(typeof(WarehousesController), nameof(WarehousesController.Edit), new[] { "Admin", "Manager" })]
    [InlineData(typeof(WarehousesController), nameof(WarehousesController.Create), new[] { "Admin", "Manager" })]
    [InlineData(typeof(WarehousesController), nameof(WarehousesController.Delete), new[] { "Admin", "Manager" })]
    [InlineData(typeof(WarehousesController), nameof(WarehousesController.CreateZone), new[] { "Admin", "Manager" })]
    [InlineData(typeof(WarehousesController), nameof(WarehousesController.CreateZoneWithLocations), new[] { "Admin", "Manager" })]
    [InlineData(typeof(WarehousesController), nameof(WarehousesController.CreateLocation), new[] { "Admin", "Manager" })]

    [InlineData(typeof(ItemsController), nameof(ItemsController.Create), new[] { "Admin", "Manager" })]
    [InlineData(typeof(ItemsController), nameof(ItemsController.Edit), new[] { "Admin", "Manager" })]
    [InlineData(typeof(ItemsController), nameof(ItemsController.Delete), new[] { "Admin" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.Waves), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.PickTasks), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.Shipping), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.ShippingDispatch), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.DeliveryReconciliation), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.ExportDeliveryReconciliationCsv), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.ExportDeliveryReconciliationExcel), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CarrierConnectors), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.SaveCarrierConnector), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CreateCarrierShipment), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.RetryCarrierShipment), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CancelCarrierShipment), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.SyncCarrierShipment), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.DockBoard), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.DockBoardData), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.UpdateDockMilestone), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.YardManagement), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CreateYardSpot), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.GateInYardVisit), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.AssignYardSpot), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.MoveYardSpot), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.GateOutYardVisit), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.MovementTasks), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.RfMovement), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.AssignMovementTask), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.StartMovementTask), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CancelMovementTask), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.ConfirmMovementTask), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.SortationConfigs), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.SaveSortationConfig), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.DisableSortationConfig), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.OrderStreamingConfigs), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.SaveOrderStreamingConfig), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.DisableOrderStreamingConfig), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.KittingWorkOrders), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CreateKittingWorkOrder), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.KittingWorkOrderDetails), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.ReserveKittingWorkOrder), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CompleteKittingWorkOrder), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.PrintKittingWorkOrderLabels), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CancelKittingWorkOrder), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.VasWorkOrders), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CreateVasWorkOrder), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.VasWorkOrderDetails), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.ReserveVasWorkOrder), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.StartVasWorkOrder), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CompleteVasOperation), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.SubmitVasQc), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.RecordVasQc), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CompleteVasWorkOrder), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CancelVasWorkOrder), new[] { "Admin", "Manager" })]
    [InlineData(typeof(LabelsController), nameof(LabelsController.PrintJobs), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(LabelsController), nameof(LabelsController.PrintVoucher), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(LabelsController), nameof(LabelsController.PrintPackage), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(LabelsController), nameof(LabelsController.PrintShippingPackage), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(LabelsController), nameof(LabelsController.PrintShipmentLoadPackageLabels), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(LabelsController), nameof(LabelsController.PrintShipmentLoadManifest), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(LabelsController), nameof(LabelsController.PrintShipmentLoadHandover), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(LabelsController), nameof(LabelsController.PrintDirectHandover), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(LabelsController), nameof(LabelsController.Print), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(LabelsController), nameof(LabelsController.ShippingDocument), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(LabelsController), nameof(LabelsController.Templates), new[] { "Admin", "Manager" })]
    [InlineData(typeof(LabelsController), nameof(LabelsController.CreateTemplate), new[] { "Admin", "Manager" })]
    [InlineData(typeof(LabelsController), nameof(LabelsController.EditTemplate), new[] { "Admin", "Manager" })]
    [InlineData(typeof(LabelsController), nameof(LabelsController.ToggleTemplate), new[] { "Admin", "Manager" })]
    [InlineData(typeof(LabelsController), nameof(LabelsController.ItemRules), new[] { "Admin", "Manager" })]
    [InlineData(typeof(LabelsController), nameof(LabelsController.SaveItemRule), new[] { "Admin", "Manager" })]
    [InlineData(typeof(LabelsController), nameof(LabelsController.DeleteItemRule), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.SlottingSimulation), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CreateSlottingSimulation), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.ApproveSlottingSimulation), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.ExceptionCenter), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.SerialLookup), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.SerialReceiving), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.RegisterSerials), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.AcknowledgeException), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.AssignException), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.ResolveException), new[] { "Admin", "Manager" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.ReassignTask), new[] { "Admin", "Manager" })]
    [InlineData(typeof(ReportsController), nameof(ReportsController.StockCount), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(ReportsController), nameof(ReportsController.StockCountSaveDraft), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(ReportsController), nameof(ReportsController.StockCountApproveDraft), new[] { "Admin", "Manager" })]
    [InlineData(typeof(VouchersController), nameof(VouchersController.Cancel), new[] { "Admin", "Manager" })]
    [InlineData(typeof(VouchersController), nameof(VouchersController.Create), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(VouchersController), nameof(VouchersController.ReleaseDirect), new[] { "Admin", "Manager" })]
    [InlineData(typeof(VouchersController), nameof(VouchersController.ConfirmForPicking), new[] { "Admin", "Manager" })]
    [InlineData(typeof(VouchersController), nameof(VouchersController.ConfirmPickTask), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(VouchersController), nameof(VouchersController.PostReservedOutbound), new[] { "Admin", "Manager" })]
    [InlineData(typeof(VouchersController), nameof(VouchersController.ConfirmPacking), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(VouchersController), nameof(VouchersController.ConfirmShipping), new[] { "Admin", "Manager" })]
    [InlineData(typeof(VouchersController), nameof(VouchersController.Approve), new[] { "Admin", "Manager" })]
    [InlineData(typeof(VouchersController), nameof(VouchersController.UpdateInboundDefect), new[] { "Admin", "Manager" })]
    [InlineData(typeof(VouchersController), nameof(VouchersController.ReplenishDefect), new[] { "Admin", "Manager" })]
    [InlineData(typeof(VouchersController), nameof(VouchersController.AnalyzeReceipt), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(VouchersController), nameof(VouchersController.DownloadReceiptDocument), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(VouchersController), nameof(VouchersController.DownloadImportTemplate), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(VouchersController), nameof(VouchersController.DownloadSampleImport100), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(VouchersController), nameof(VouchersController.ImportLinesExcel), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(OperationsController), nameof(OperationsController.DownloadYardVisitEvidence), new[] { "Admin", "Manager", "Staff" })]
    [InlineData(typeof(ReportsController), nameof(ReportsController.StockCountUnlockApproved), new[] { "Admin" })]
    [InlineData(typeof(ReportsController), nameof(ReportsController.PeriodLocks), new[] { "Admin", "Manager" })]
    [InlineData(typeof(ReportsController), nameof(ReportsController.SetPeriodLock), new[] { "Admin", "Manager" })]
    [InlineData(typeof(ReportsController), nameof(ReportsController.ClearPeriodLock), new[] { "Admin", "Manager" })]
    [InlineData(typeof(ReportsController), nameof(ReportsController.StockValuation), new[] { "Admin", "Manager" })]
    [InlineData(typeof(ReportsController), nameof(ReportsController.ExportStockValuation), new[] { "Admin", "Manager" })]
    [InlineData(typeof(ReportsController), nameof(ReportsController.StockSnapshot), new[] { "Admin", "Manager" })]
    [InlineData(typeof(ReportsController), nameof(ReportsController.GenerateStockSnapshot), new[] { "Admin", "Manager" })]
    [InlineData(typeof(ReportsController), nameof(ReportsController.ExportStockSnapshot), new[] { "Admin", "Manager" })]
    [InlineData(typeof(ReportsController), nameof(ReportsController.AuditTrail), new[] { "Admin" })]
    [InlineData(typeof(ReportsController), nameof(ReportsController.Alerts), new[] { "Admin" })]
    [InlineData(typeof(ReportsController), nameof(ReportsController.RefreshExpiryAlerts), new[] { "Admin" })]
    [InlineData(typeof(ReportsController), nameof(ReportsController.ResolveAlert), new[] { "Admin" })]
    [InlineData(typeof(ReportsController), nameof(ReportsController.OpsKpi), new[] { "Admin", "Manager" })]
    public void CriticalActions_ShouldMatchExpectedRoleMatrix(Type controllerType, string actionName, string[] expectedRoles)
    {
        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => string.Equals(m.Name, actionName, StringComparison.Ordinal))
            .Where(IsActionMethod)
            .ToList();

        Assert.NotEmpty(methods);
        foreach (var method in methods)
        {
            var actualRoles = GetEffectiveRoles(controllerType, method);
            Assert.Equal(expectedRoles.OrderBy(x => x), actualRoles.OrderBy(x => x));
        }
    }

    [Theory]
    [InlineData(typeof(ReportsController), nameof(ReportsController.StockValuation), WmsPermissions.ReportViewFinancial)]
    [InlineData(typeof(ReportsController), nameof(ReportsController.ExportStockValuation), WmsPermissions.ReportViewFinancial)]
    public void FinancialReportActions_ShouldRequireFinancialPolicy(Type controllerType, string actionName, string expectedPolicy)
    {
        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => string.Equals(m.Name, actionName, StringComparison.Ordinal))
            .Where(IsActionMethod)
            .ToList();

        Assert.NotEmpty(methods);
        foreach (var method in methods)
        {
            var policies = method.GetCustomAttributes<AuthorizeAttribute>(true)
                .Concat(controllerType.GetCustomAttributes<AuthorizeAttribute>(true))
                .Select(a => a.Policy)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();
            Assert.Contains(expectedPolicy, policies);
        }
    }

    [Theory]
    [InlineData(typeof(HomeController), nameof(HomeController.Index))]
    [InlineData(typeof(ItemsController), nameof(ItemsController.Index))]
    [InlineData(typeof(ItemsController), nameof(ItemsController.Details))]
    [InlineData(typeof(WarehousesController), nameof(WarehousesController.Index))]
    [InlineData(typeof(WarehousesController), nameof(WarehousesController.Details))]
    [InlineData(typeof(WarehousesController), nameof(WarehousesController.InventoryMap))]
    [InlineData(typeof(VouchersController), nameof(VouchersController.Index))]
    [InlineData(typeof(VouchersController), nameof(VouchersController.Details))]
    [InlineData(typeof(ReportsController), nameof(ReportsController.Inventory))]
    [InlineData(typeof(ReportsController), nameof(ReportsController.StockMovement))]
    public void ReadOnlyActions_ShouldRequireAuthenticationAtMinimum(Type controllerType, string actionName)
    {
        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => string.Equals(m.Name, actionName, StringComparison.Ordinal))
            .Where(IsActionMethod)
            .ToList();

        Assert.NotEmpty(methods);
        foreach (var method in methods)
        {
            var isAnonymous = method.GetCustomAttribute<AllowAnonymousAttribute>() != null
                || controllerType.GetCustomAttribute<AllowAnonymousAttribute>() != null;
            Assert.False(isAnonymous);
        }
    }

    [Fact]
    public void DangerousSystemActions_ShouldRequireAdminAndAntiForgery()
    {
        var controllerType = typeof(SystemController);
        var actionNames = new[]
        {
            nameof(SystemController.SeedData),
            nameof(SystemController.MergeLocationsPerLevel),
            nameof(SystemController.ResetDatabase)
        };

        foreach (var actionName in actionNames)
        {
            var method = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .First(m => m.Name == actionName && IsActionMethod(m));

            var roles = GetEffectiveRoles(controllerType, method);
            Assert.Equal(new[] { "Admin" }, roles);
            Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        }
    }

    [Theory]
    [InlineData(typeof(VouchersController), nameof(VouchersController.Cancel))]
    [InlineData(typeof(VouchersController), nameof(VouchersController.Approve))]
    [InlineData(typeof(VouchersController), nameof(VouchersController.PostReservedOutbound))]
    [InlineData(typeof(VouchersController), nameof(VouchersController.ConfirmPacking))]
    [InlineData(typeof(VouchersController), nameof(VouchersController.ConfirmShipping))]
    [InlineData(typeof(ReportsController), nameof(ReportsController.StockCountApproveDraft))]
    [InlineData(typeof(ReportsController), nameof(ReportsController.StockCountUnlockApproved))]
    [InlineData(typeof(UsersController), nameof(UsersController.Delete))]
    [InlineData(typeof(UsersController), nameof(UsersController.MarkLoginHelpInReview))]
    [InlineData(typeof(UsersController), nameof(UsersController.ResolveLoginHelpRequest))]
    [InlineData(typeof(UsersController), nameof(UsersController.RejectLoginHelpRequest))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CreateYardSpot))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.GateInYardVisit))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.AssignYardSpot))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.MoveYardSpot))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.GateOutYardVisit))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.AssignMovementTask))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.StartMovementTask))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CancelMovementTask))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.ConfirmMovementTask))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.SaveSortationConfig))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.DisableSortationConfig))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.SaveOrderStreamingConfig))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.DisableOrderStreamingConfig))]
    [InlineData(typeof(VouchersController), nameof(VouchersController.ReleaseDirect))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.ReserveKittingWorkOrder))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CompleteKittingWorkOrder))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CancelKittingWorkOrder))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.ReserveVasWorkOrder))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.StartVasWorkOrder))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CompleteVasOperation))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.SubmitVasQc))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.RecordVasQc))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CompleteVasWorkOrder))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CancelVasWorkOrder))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.SaveCarrierConnector))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CreateCarrierShipment))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.RetryCarrierShipment))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CancelCarrierShipment))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.SyncCarrierShipment))]
    [InlineData(typeof(LabelsController), nameof(LabelsController.ToggleTemplate))]
    [InlineData(typeof(LabelsController), nameof(LabelsController.SaveItemRule))]
    [InlineData(typeof(LabelsController), nameof(LabelsController.DeleteItemRule))]
    [InlineData(typeof(LabelsController), nameof(LabelsController.PrintVoucher))]
    [InlineData(typeof(LabelsController), nameof(LabelsController.PrintPackage))]
    [InlineData(typeof(LabelsController), nameof(LabelsController.PrintShippingPackage))]
    [InlineData(typeof(LabelsController), nameof(LabelsController.PrintShipmentLoadPackageLabels))]
    [InlineData(typeof(LabelsController), nameof(LabelsController.PrintShipmentLoadManifest))]
    [InlineData(typeof(LabelsController), nameof(LabelsController.PrintShipmentLoadHandover))]
    [InlineData(typeof(LabelsController), nameof(LabelsController.PrintDirectHandover))]
    public void SensitivePostActions_ShouldUseAntiForgery(Type controllerType, string actionName)
    {
        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == actionName && IsActionMethod(m))
            .ToList();

        Assert.NotEmpty(methods);
        foreach (var method in methods)
        {
            Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
            Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        }
    }

    [Theory]
    [InlineData(typeof(LabelsController), nameof(LabelsController.CreateTemplate))]
    [InlineData(typeof(LabelsController), nameof(LabelsController.EditTemplate))]
    [InlineData(typeof(AccountController), nameof(AccountController.AccessHelp))]
    [InlineData(typeof(OperationsController), nameof(OperationsController.CreateVasWorkOrder))]
    public void SensitivePostOverloads_ShouldUseAntiForgery(Type controllerType, string actionName)
    {
        var postMethods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == actionName && IsActionMethod(m))
            .Where(m => m.GetCustomAttribute<HttpPostAttribute>() != null)
            .ToList();

        Assert.NotEmpty(postMethods);
        foreach (var method in postMethods)
            Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    [Fact]
    public void PeriodLock_ShouldUseOperationDate_WhenOldVoucherIsPostedAfterLockedPeriod()
    {
        var voucher = new Voucher
        {
            VoucherDate = new DateTime(2026, 4, 1)
        };
        var operationDate = new DateTime(2026, 4, 30, 9, 0, 0);
        var lockDate = new DateTime(2026, 4, 15);

        var transactionDate = ResolveLockTransactionDate(voucher, operationDate);

        Assert.Equal(operationDate, transactionDate);
        Assert.False(IsPeriodLocked(transactionDate, lockDate));
    }

    [Fact]
    public void PeriodLock_ShouldUseCompletedAt_WhenNewVoucherCompletedInsideLockedPeriod()
    {
        var completedAt = new DateTime(2026, 4, 10, 16, 30, 0);
        var voucher = new Voucher
        {
            VoucherDate = new DateTime(2026, 4, 30),
            CompletedAt = completedAt
        };
        var operationDate = new DateTime(2026, 4, 30, 9, 0, 0);
        var lockDate = new DateTime(2026, 4, 15);

        var transactionDate = ResolveLockTransactionDate(voucher, operationDate);

        Assert.Equal(completedAt, transactionDate);
        Assert.True(IsPeriodLocked(transactionDate, lockDate));
    }

    [Fact]
    public async Task YardGateIn_ShouldPreventSecondActiveVisitForSameTrailer()
    {
        await using var db = CreateYardTestDb();
        var service = CreateYardService(db);

        await service.GateInAsync(new YardGateInRequest { WarehouseId = 1, TrailerNumber = "TR-100" }, null, "tester");

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.GateInAsync(new YardGateInRequest { WarehouseId = 1, TrailerNumber = "TR-100" }, null, "tester"));

        Assert.Equal("YARD_ACTIVE_VISIT_EXISTS", ex.Code);
    }

    [Fact]
    public async Task YardAssignSpot_ShouldPreventTwoActiveVisitsInOneSpot()
    {
        await using var db = CreateYardTestDb();
        var service = CreateYardService(db);
        var spot = await service.CreateSpotAsync(1, "Y-01", null, YardSpotTypeEnum.Standard, YardSpotStatusEnum.Available, null, "tester");
        var first = await service.GateInAsync(new YardGateInRequest { WarehouseId = 1, TrailerNumber = "TR-101", YardSpotId = spot.YardSpotId }, null, "tester");
        var second = await service.GateInAsync(new YardGateInRequest { WarehouseId = 1, TrailerNumber = "TR-102" }, null, "tester");

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.AssignSpotAsync(second.YardVisitId, spot.YardSpotId, null, "tester"));

        Assert.Equal("YARD_SPOT_OCCUPIED", ex.Code);
        Assert.Equal(YardSpotStatusEnum.Occupied, db.YardSpots.Single(s => s.YardSpotId == spot.YardSpotId).Status);
        Assert.Null(db.YardVisits.Single(v => v.YardVisitId == second.YardVisitId).CurrentSpotId);
        Assert.Equal(spot.YardSpotId, db.YardVisits.Single(v => v.YardVisitId == first.YardVisitId).CurrentSpotId);
    }

    [Fact]
    public async Task YardMoveSpot_ShouldUpdateCurrentSpotAndKeepVisitOpen()
    {
        await using var db = CreateYardTestDb();
        var service = CreateYardService(db);
        var spotA = await service.CreateSpotAsync(1, "Y-02", null, YardSpotTypeEnum.Standard, YardSpotStatusEnum.Available, null, "tester");
        var spotB = await service.CreateSpotAsync(1, "Y-03", null, YardSpotTypeEnum.Standard, YardSpotStatusEnum.Available, null, "tester");
        var visit = await service.GateInAsync(new YardGateInRequest { WarehouseId = 1, TrailerNumber = "TR-103", YardSpotId = spotA.YardSpotId }, null, "tester");

        var moved = await service.MoveSpotAsync(visit.YardVisitId, spotB.YardSpotId, null, "tester");

        Assert.Equal(spotB.YardSpotId, moved.CurrentSpotId);
        Assert.Null(moved.GateOutAt);
        Assert.Equal(YardVisitStatusEnum.Parked, moved.Status);
        Assert.Equal(YardSpotStatusEnum.Available, db.YardSpots.Single(s => s.YardSpotId == spotA.YardSpotId).Status);
        Assert.Equal(YardSpotStatusEnum.Occupied, db.YardSpots.Single(s => s.YardSpotId == spotB.YardSpotId).Status);
    }

    [Fact]
    public async Task YardGateOut_ShouldFinalizeVisitAndReleaseSpot()
    {
        await using var db = CreateYardTestDb();
        var service = CreateYardService(db);
        var spot = await service.CreateSpotAsync(1, "Y-04", null, YardSpotTypeEnum.Standard, YardSpotStatusEnum.Available, null, "tester");
        var visit = await service.GateInAsync(new YardGateInRequest { WarehouseId = 1, TrailerNumber = "TR-104", YardSpotId = spot.YardSpotId }, null, "tester");

        var closed = await service.GateOutAsync(visit.YardVisitId, null, "tester");

        Assert.Equal(YardVisitStatusEnum.GatedOut, closed.Status);
        Assert.NotNull(closed.GateOutAt);
        Assert.True(closed.GetDwellMinutes(closed.GateOutAt.Value) >= 0);
        Assert.Equal(YardSpotStatusEnum.Available, db.YardSpots.Single(s => s.YardSpotId == spot.YardSpotId).Status);
    }

    private static bool IsActionMethod(MethodInfo method)
    {
        if (method.IsSpecialName) return false;
        if (method.GetCustomAttribute<NonActionAttribute>() != null) return false;
        return typeof(IActionResult).IsAssignableFrom(method.ReturnType)
            || (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
                && typeof(IActionResult).IsAssignableFrom(method.ReturnType.GenericTypeArguments[0]));
    }

    private static string[] GetEffectiveRoles(Type controllerType, MethodInfo method)
    {
        var allowAnonymous = method.GetCustomAttribute<AllowAnonymousAttribute>() != null
            || controllerType.GetCustomAttribute<AllowAnonymousAttribute>() != null;
        if (allowAnonymous) return Array.Empty<string>();

        var methodAuth = method.GetCustomAttributes<AuthorizeAttribute>(true).ToList();
        if (methodAuth.Any(a => !string.IsNullOrWhiteSpace(a.Roles)))
            return ParseRoles(methodAuth);

        var classAuth = controllerType.GetCustomAttributes<AuthorizeAttribute>(true).ToList();
        return ParseRoles(classAuth);
    }

    private static string[] ParseRoles(IEnumerable<AuthorizeAttribute> attributes)
    {
        return attributes
            .SelectMany(a => (a.Roles ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x)
            .ToArray();
    }

    private static DateTime ResolveLockTransactionDate(Voucher voucher, DateTime operationDate)
    {
        var method = typeof(VouchersController).GetMethod("ResolveLockTransactionDate", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        return (DateTime)method.Invoke(null, new object?[] { voucher, operationDate })!;
    }

    private static bool IsPeriodLocked(DateTime transactionDate, DateTime lockDate)
    {
        var method = typeof(VouchersController).GetMethod("IsLocked", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        return (bool)method.Invoke(null, new object?[] { transactionDate, lockDate })!;
    }

    private static AppDbContext CreateYardTestDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"yard-tests-{Guid.NewGuid():N}")
            .Options;
        var db = new AppDbContext(options)
        {
            SkipAudit = true
        };
        db.Warehouses.Add(new Warehouse { WarehouseId = 1, WarehouseCode = "WH1", WarehouseName = "Warehouse 1", IsActive = true });
        db.SaveChanges();
        return db;
    }

    private static YardManagementService CreateYardService(AppDbContext db)
        => new(db, new EfUnitOfWork(db));
}
