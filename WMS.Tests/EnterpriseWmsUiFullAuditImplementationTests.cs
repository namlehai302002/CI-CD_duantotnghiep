using System.Text;
using System.Text.RegularExpressions;

namespace WMS.Tests;

public sealed class EnterpriseWmsUiFullAuditImplementationTests
{
    private static readonly string[][] AppCodeStrictPriorityViews =
    {
        new[] { "Views", "Operations", "DockBoard.cshtml" },
        new[] { "Views", "Operations", "InboundApprovals.cshtml" },
        new[] { "Views", "Operations", "DeliveryReconciliation.cshtml" },
        new[] { "Views", "Reports", "StockCount.cshtml" },
        new[] { "Views", "Operations", "SlottingSimulation.cshtml" },
        new[] { "Views", "Operations", "KittingWorkOrders.cshtml" },
        new[] { "Views", "Operations", "VasWorkOrders.cshtml" },
        new[] { "Views", "Operations", "YardBillingCharges.cshtml" },
        new[] { "Views", "Operations", "YardBillingRates.cshtml" },
        new[] { "Views", "Operations", "OrderStreamingConfigs.cshtml" },
        new[] { "Views", "Operations", "SortationConfigs.cshtml" },
        new[] { "Views", "Operations", "NextTask.cshtml" },
        new[] { "Views", "Operations", "Replenishment.cshtml" },
        new[] { "Views", "Operations", "CapacitySimulation.cshtml" },
        new[] { "Views", "Operations", "CreateKittingWorkOrder.cshtml" },
        new[] { "Views", "Operations", "CreateVasWorkOrder.cshtml" },
        new[] { "Views", "Operations", "CarrierConnectors.cshtml" },
        new[] { "Views", "Operations", "CrossDockOpportunities.cshtml" }
    };

    private static readonly string[][] VoucherCoreViews =
    {
        new[] { "Views", "Vouchers", "Create.cshtml" },
        new[] { "Views", "Vouchers", "Details.cshtml" }
    };

    [Fact]
    public void PriorityRedesignedViews_ShouldNotUseRoughInlineStylesOrLocalStyleBlocks()
    {
        var root = FindRepositoryRoot();
        var priorityViews = new[]
        {
            Path.Combine(root, "Views", "Labels", "Templates.cshtml"),
            Path.Combine(root, "Views", "Labels", "ItemRules.cshtml"),
            Path.Combine(root, "Views", "Labels", "TemplateForm.cshtml"),
            Path.Combine(root, "Views", "Labels", "PrintJobs.cshtml"),
            Path.Combine(root, "Views", "Operations", "QualityInspection.cshtml"),
            Path.Combine(root, "Views", "Operations", "Waves.cshtml"),
            Path.Combine(root, "Views", "Operations", "ZoneAssignment.cshtml")
        };

        var failures = priorityViews
            .Select(path => new { Path = path, Content = Read(path) })
            .Where(file => file.Content.Contains("style" + "=", StringComparison.OrdinalIgnoreCase)
                || file.Content.Contains("<" + "style", StringComparison.OrdinalIgnoreCase))
            .Select(file => Path.GetRelativePath(root, file.Path))
            .ToList();

        Assert.True(failures.Count == 0, "Priority redesigned views still contain local styling: " + string.Join(", ", failures));
    }

    [Fact]
    public void PriorityPostForms_ShouldCarryAntiForgeryTokens()
    {
        var root = FindRepositoryRoot();
        var formViews = new[]
        {
            Path.Combine(root, "Views", "Labels", "Templates.cshtml"),
            Path.Combine(root, "Views", "Labels", "ItemRules.cshtml"),
            Path.Combine(root, "Views", "Labels", "TemplateForm.cshtml"),
            Path.Combine(root, "Views", "Operations", "QualityInspection.cshtml"),
            Path.Combine(root, "Views", "Operations", "ZoneAssignment.cshtml")
        };

        foreach (var path in formViews)
        {
            var content = Read(path);
            var postForms = Regex.Matches(content, "<form[^>]*method=\"post\"[\\s\\S]*?</form>", RegexOptions.IgnoreCase);

            Assert.True(postForms.Count > 0, Path.GetRelativePath(root, path) + " should contain at least one POST form.");
            foreach (Match form in postForms)
            {
                Assert.True(form.Value.Contains("@Html.AntiForgeryToken()", StringComparison.Ordinal)
                    || form.Value.Contains("asp-antiforgery=\"true\"", StringComparison.Ordinal),
                    Path.GetRelativePath(root, path) + " has a POST form without antiforgery.");
            }
        }
    }

    [Fact]
    public void EnterpriseWmsCss_ShouldExposeSharedTokensForRedesignedOperations()
    {
        var root = FindRepositoryRoot();
        var css = Read(Path.Combine(root, "wwwroot", "css", "site.css"));
        var js = Read(Path.Combine(root, "wwwroot", "js", "site.js"));

        foreach (var token in new[]
        {
            ".enterprise-card",
            ".inline-post-form",
            ".labelops-template-editor",
            ".labelops-source-stack",
            ".quality-step-grid",
            ".quality-step",
            ".qc-overlay.is-open",
            ".qc-row-4",
            ".wave-progress-track",
            ".wave-progress-fill",
            ".zone-assignment-form",
            ".zone-option-list",
            ".empty-inline-alert",
            ".dock-board-shell",
            ".slotting-simulation-form",
            ".swal-enterprise-body",
            ".carrier-edit-form",
            ".crossdock-confirm-body",
            ".next-task-grid"
        })
        {
            Assert.Contains(token, css, StringComparison.Ordinal);
        }

        Assert.Contains("enhanceDataWidths", js, StringComparison.Ordinal);
        Assert.Contains("data-progress-width", js, StringComparison.Ordinal);
        Assert.Contains("data-segment-width", js, StringComparison.Ordinal);
    }

    [Fact]
    public void AppCodeStrictPriorityViews_ShouldUseSharedStylesAndSafeRoutes()
    {
        var root = FindRepositoryRoot();
        var failures = new List<string>();

        foreach (var parts in AppCodeStrictPriorityViews)
        {
            var path = Path.Combine(new[] { root }.Concat(parts).ToArray());
            var content = Read(path);
            foreach (var banned in new[] { "style" + "=", "<" + "style", "href=\"" + "/", "action=\"" + "/" })
            {
                if (content.Contains(banned, StringComparison.OrdinalIgnoreCase))
                    failures.Add($"{Path.GetRelativePath(root, path)} contains {banned}");
            }
        }

        Assert.True(failures.Count == 0, "App-code strict priority views still contain rough UI/route patterns:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void VoucherCoreViews_ShouldUseTagHelpersForInternalRoutes()
    {
        var root = FindRepositoryRoot();
        var failures = new List<string>();

        foreach (var parts in VoucherCoreViews)
        {
            var path = Path.Combine(new[] { root }.Concat(parts).ToArray());
            var content = Read(path);
            foreach (var banned in new[] { "href=\"" + "/", "action=\"" + "/" })
            {
                if (content.Contains(banned, StringComparison.OrdinalIgnoreCase))
                    failures.Add($"{Path.GetRelativePath(root, path)} contains {banned}");
            }
        }

        var createPath = Path.Combine(root, "Views", "Vouchers", "Create.cshtml");
        Assert.DoesNotContain("<" + "style", Read(createPath), StringComparison.OrdinalIgnoreCase);

        Assert.True(failures.Count == 0, "Voucher core views still contain hard-coded internal routes:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void AppCodeStrictPriorityPostForms_ShouldHaveAntiForgeryAndCriticalConfirmations()
    {
        var root = FindRepositoryRoot();
        var failures = new List<string>();

        foreach (var parts in AppCodeStrictPriorityViews)
        {
            var path = Path.Combine(new[] { root }.Concat(parts).ToArray());
            var content = Read(path);
            var postForms = Regex.Matches(content, "<form[^>]*method=\"post\"[\\s\\S]*?</form>", RegexOptions.IgnoreCase);

            foreach (Match form in postForms)
            {
                var value = form.Value;
                var hasToken = value.Contains("@Html.AntiForgeryToken()", StringComparison.Ordinal)
                    || value.Contains("asp-antiforgery=\"true\"", StringComparison.Ordinal)
                    || value.Contains("__RequestVerificationToken", StringComparison.Ordinal);
                if (!hasToken)
                    failures.Add($"{Path.GetRelativePath(root, path)} has a POST form without antiforgery.");
            }
        }

        Assert.True(failures.Count == 0, "Antiforgery gaps found:" + Environment.NewLine + string.Join(Environment.NewLine, failures));

        var stockCount = Read(Path.Combine(root, "Views", "Reports", "StockCount.cshtml"));
        var inbound = Read(Path.Combine(root, "Views", "Operations", "InboundApprovals.cshtml"));
        var yardCharges = Read(Path.Combine(root, "Views", "Operations", "YardBillingCharges.cshtml"));
        var slotting = Read(Path.Combine(root, "Views", "Operations", "SlottingSimulation.cshtml"));
        Assert.Contains("approvalReason", stockCount, StringComparison.Ordinal);
        Assert.Contains("unlockReason", stockCount, StringComparison.Ordinal);
        Assert.Contains("rejectInboundQueueReason", inbound, StringComparison.Ordinal);
        Assert.Contains("data-enterprise-confirm", yardCharges, StringComparison.Ordinal);
        Assert.Contains("data-enterprise-confirm", slotting, StringComparison.Ordinal);
    }

    [Fact]
    public void ZoneAssignment_ShouldUseSafeRoutesAndWarehouseScopedPersistence()
    {
        var root = FindRepositoryRoot();
        var view = Read(Path.Combine(root, "Views", "Operations", "ZoneAssignment.cshtml"));
        var pickingController = Read(Path.Combine(root, "Controllers", "OperationsController.Picking.cs"));
        var operationsController = Read(Path.Combine(root, "Controllers", "OperationsController.cs"));

        Assert.Contains("asp-controller=\"Operations\" asp-action=\"SaveZoneAssignment\"", view, StringComparison.Ordinal);
        Assert.Contains("asp-controller=\"Warehouses\" asp-action=\"Index\"", view, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/Operations/Zones\"", view, StringComparison.Ordinal);
        Assert.DoesNotContain("action=\"/Operations/SaveZoneAssignment\"", view, StringComparison.Ordinal);

        foreach (var token in new[]
        {
            "int? scopedWh = GetScopedWarehouseId();",
            "return Forbid();",
            "requestedZoneIds.Contains(z.ZoneId)",
            "user.WarehouseId.HasValue",
            "validZonesQuery",
            "requestedZoneIds = validZoneIds",
            "RemoveRange(old)"
        })
        {
            Assert.Contains(token, operationsController, StringComparison.Ordinal);
        }

        Assert.Contains("userIds.Contains(a.UserId) && zoneIds.Contains(a.ZoneId)", pickingController, StringComparison.Ordinal);
    }

    [Fact]
    public void RedesignedVietnameseViews_ShouldNotContainKnownMojibakeMarkers()
    {
        var root = FindRepositoryRoot();
        var priorityViews = new[]
        {
            Path.Combine(root, "Views", "Labels", "Templates.cshtml"),
            Path.Combine(root, "Views", "Labels", "ItemRules.cshtml"),
            Path.Combine(root, "Views", "Labels", "TemplateForm.cshtml"),
            Path.Combine(root, "Views", "Labels", "PrintJobs.cshtml"),
            Path.Combine(root, "Views", "Operations", "QualityInspection.cshtml"),
            Path.Combine(root, "Views", "Operations", "Waves.cshtml"),
            Path.Combine(root, "Views", "Operations", "ZoneAssignment.cshtml")
        };
        var mojibakeMarkers = new[]
        {
            TextFromCodePoints(0x00C3),
            TextFromCodePoints(0x00C4),
            TextFromCodePoints(0x00C2),
            TextFromCodePoints(0x00E1, 0x00BA),
            TextFromCodePoints(0x00E1, 0x00BB)
        };

        var failures = priorityViews
            .Select(path => new { Path = path, Content = Read(path) })
            .SelectMany(file => mojibakeMarkers
                .Where(marker => file.Content.Contains(marker, StringComparison.Ordinal))
                .Select(marker => $"{Path.GetRelativePath(root, file.Path)} contains {marker}"))
            .ToList();

        Assert.True(failures.Count == 0, "Mojibake markers found:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
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
}
