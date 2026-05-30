using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using WMS.Controllers;
using WMS.ViewModels;

namespace WMS.Tests;

public class CoreWmsUiApprovalRedesignTests
{
    [Fact]
    public void InboundApprovalHub_ShouldBeManagerOnlyAndExposeQueueModel()
    {
        var action = typeof(OperationsController).GetMethod("InboundApprovals", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(action);

        var authorize = action!.GetCustomAttributes<AuthorizeAttribute>().ToList();
        Assert.Contains(authorize, a => string.Equals(a.Roles, "Admin,Manager", StringComparison.Ordinal));
        Assert.NotNull(typeof(InboundApprovalQueueViewModel).GetProperty(nameof(InboundApprovalQueueViewModel.Rows)));
        Assert.NotNull(typeof(DashboardViewModel).GetProperty(nameof(DashboardViewModel.PendingInboundApprovals)));
    }

    [Fact]
    public void Layout_ShouldExposeInboundApprovalHubWithoutDuplicateWorkspaceHeading()
    {
        var root = FindRepositoryRoot();
        var layout = ReadUtf8(Path.Combine(root, "Views", "Shared", "_Layout.cshtml"));

        Assert.Contains("Duyệt phiếu nhập", layout, StringComparison.Ordinal);
        Assert.Contains("layoutPendingInboundApprovals", layout, StringComparison.Ordinal);
        Assert.Contains("isAdminOrManager", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"workspace-heading\"", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("topbar-brand-text", layout, StringComparison.Ordinal);
    }

    [Fact]
    public void VoucherListAndCreate_ShouldClarifyInboundApprovalPath()
    {
        var root = FindRepositoryRoot();
        var controller = ReadUtf8(Path.Combine(root, "Controllers", "VouchersController.Index.cs"));
        var indexView = ReadUtf8(Path.Combine(root, "Views", "Vouchers", "Index.cshtml"));
        var createView = ReadUtf8(Path.Combine(root, "Views", "Vouchers", "Create.cshtml"));

        Assert.Contains("InboundStatusEnum? inboundStatus", controller, StringComparison.Ordinal);
        Assert.Contains("name=\"inboundStatus\"", indexView, StringComparison.Ordinal);
        Assert.Contains("Lưu và gửi duyệt", createView, StringComparison.Ordinal);
        Assert.Contains("Nhập kho > Duyệt phiếu nhập", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void InventoryMap_ShouldUseOperationalMapViewModelAndDrawer()
    {
        var root = FindRepositoryRoot();
        var controller = ReadUtf8(Path.Combine(root, "Controllers", "WarehousesController.cs"));
        var view = ReadUtf8(Path.Combine(root, "Views", "Warehouses", "InventoryMap.cshtml"));
        var css = ReadUtf8(Path.Combine(root, "wwwroot", "css", "site.css"));

        Assert.Contains("BuildInventoryMapViewModel", controller, StringComparison.Ordinal);
        Assert.Contains("InventoryMapPageViewModel", view, StringComparison.Ordinal);
        Assert.Contains("inventory-location-drawer", view, StringComparison.Ordinal);
        Assert.Contains("inventory-location-tile", css, StringComparison.Ordinal);
        Assert.Contains("inventory-map-toolbar", css, StringComparison.Ordinal);
    }

    [Fact]
    public void DashboardAndOfflineQueue_ShouldAvoidAnnoyingPersistentChrome()
    {
        var root = FindRepositoryRoot();
        var layout = ReadUtf8(Path.Combine(root, "Views", "Shared", "_Layout.cshtml"));
        var offlineQueueScript = ReadUtf8(Path.Combine(root, "wwwroot", "js", "offline-scan-queue.js"));
        var css = ReadUtf8(Path.Combine(root, "wwwroot", "css", "site.css"));

        Assert.Contains("offlineQueueHide", layout, StringComparison.Ordinal);
        Assert.Contains("offlineQueueRestore", layout, StringComparison.Ordinal);
        Assert.Contains("wms_offline_queue_hidden", offlineQueueScript, StringComparison.Ordinal);
        Assert.Contains("setWidgetHidden", offlineQueueScript, StringComparison.Ordinal);
        Assert.Contains(".offline-queue-widget.is-hidden", css, StringComparison.Ordinal);
        Assert.Contains(".role-workspace-panel", css, StringComparison.Ordinal);
        Assert.Contains("border-left: 0;", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".role-workspace-panel {\r\n    border-left: 4px solid var(--accent);", css, StringComparison.Ordinal);
    }

    private static string ReadUtf8(string path)
        => File.ReadAllText(path);

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
