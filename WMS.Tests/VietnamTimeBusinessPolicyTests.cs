using System.Text;
using System.Text.RegularExpressions;

namespace WMS.Tests;

public sealed class VietnamTimeBusinessPolicyTests
{
    [Fact]
    public void UserFacingBusinessCode_ShouldUseVietnamTimeAndAllowUtcOnlyForSecurityBoundaries()
    {
        var root = FindRepositoryRoot();
        var allowlistedUtcFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Common/VietnamTime.cs",
            "Controllers/AccountController.cs",
            "Services/MobileSecurityServices.cs"
        };
        var failures = new List<string>();

        foreach (var path in EnumerateBusinessTextFiles(root))
        {
            var relative = ToRelative(root, path);
            var content = Read(path);

            if (!allowlistedUtcFiles.Contains(relative)
                && Regex.IsMatch(content, @"DateTime\.(Now|Today|UtcNow)|DateTimeOffset\.(Now|UtcNow)", RegexOptions.CultureInvariant))
            {
                failures.Add($"{relative} uses machine-local/UTC clock directly instead of VietnamTime.");
            }

            if (!string.Equals(relative, "Common/VietnamTime.cs", StringComparison.OrdinalIgnoreCase)
                && content.Contains("TimeZoneInfo.ConvertTimeFromUtc", StringComparison.Ordinal))
            {
                failures.Add($"{relative} converts a business timestamp as UTC in user-facing code.");
            }

            if ((relative.StartsWith("Views/", StringComparison.OrdinalIgnoreCase)
                    || relative.StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase))
                && Regex.IsMatch(content, @"toISOString\(\)\.(slice|substring)\(0,\s*10\)", RegexOptions.CultureInvariant))
            {
                failures.Add($"{relative} builds a user-facing date with UTC ISO slicing.");
            }
        }

        Assert.True(failures.Count == 0, "Vietnam time policy violations:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void KeyOperationalScreens_ShouldPinDateDefaultsAndClientClocksToVietnamTimezone()
    {
        var root = FindRepositoryRoot();
        var stockSnapshot = Read(Path.Combine(root, "Views", "Reports", "StockSnapshot.cshtml"));
        var inventoryTransactions = Read(Path.Combine(root, "Views", "Reports", "InventoryTransactions.cshtml"));
        var threePlRuns = Read(Path.Combine(root, "Views", "Operations", "ThreePlBillingRuns.cshtml"));
        var voucherCreate = Read(Path.Combine(root, "Views", "Vouchers", "Create.cshtml"));
        var voucherDetails = Read(Path.Combine(root, "Views", "Vouchers", "Details.cshtml"));
        var layout = Read(Path.Combine(root, "Views", "Shared", "_Layout.cshtml"));
        var dockBoard = Read(Path.Combine(root, "Views", "Operations", "DockBoard.cshtml"));
        var inventoryMap = Read(Path.Combine(root, "Views", "Warehouses", "InventoryMap.cshtml"));

        Assert.Contains("WMS.Common.VietnamTime.Today", stockSnapshot, StringComparison.Ordinal);
        Assert.Contains("WMS.Common.VietnamTime.Today", inventoryTransactions, StringComparison.Ordinal);
        Assert.Contains("WMS.Common.VietnamTime.Today", threePlRuns, StringComparison.Ordinal);
        Assert.Contains("WMS.Common.VietnamTime.Today", voucherCreate, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTime.UtcNow", voucherCreate, StringComparison.Ordinal);
        Assert.DoesNotContain("ConvertTimeFromUtc", voucherDetails, StringComparison.Ordinal);

        Assert.Contains("function getVietnamDateStamp()", layout, StringComparison.Ordinal);
        Assert.Contains("timeZone: 'Asia/Ho_Chi_Minh'", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("toISOString().slice(0,10)", layout, StringComparison.Ordinal);

        Assert.Contains("vietnamClockOptions", dockBoard, StringComparison.Ordinal);
        Assert.Contains("timeZone: 'Asia/Ho_Chi_Minh'", dockBoard, StringComparison.Ordinal);
        Assert.Contains("timeZone: 'Asia/Ho_Chi_Minh'", inventoryMap, StringComparison.Ordinal);
    }

    [Fact]
    public void ExportAndDocumentFilenames_ShouldUseVietnamTimestampStamps()
    {
        var root = FindRepositoryRoot();
        var shipmentLoads = Read(Path.Combine(root, "Controllers", "OperationsController.ShipmentLoads.cs"));
        var system = Read(Path.Combine(root, "Controllers", "SystemController.cs"));
        var coreServices = Read(Path.Combine(root, "Services", "CoreControllerRefactorServices.cs"));
        var documentIntake = Read(Path.Combine(root, "Services", "VoucherDocumentIntakeService.cs"));

        foreach (var content in new[] { shipmentLoads, system, coreServices, documentIntake })
        {
            Assert.Contains("VietnamTime.FileStamp", content, StringComparison.Ordinal);
            Assert.DoesNotContain("DateTime.UtcNow:", content, StringComparison.Ordinal);
        }
    }

    private static IEnumerable<string> EnumerateBusinessTextFiles(string root)
    {
        var roots = new[]
        {
            "Controllers",
            "Services",
            "Views",
            "wwwroot",
            "Common",
            "Models",
            "ViewModels"
        };
        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs",
            ".cshtml",
            ".js",
            ".html",
            ".css",
            ".webmanifest"
        };

        return roots
            .Select(path => Path.Combine(root, path))
            .Where(Directory.Exists)
            .SelectMany(path => Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
            .Where(path => extensions.Contains(Path.GetExtension(path)))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}wwwroot{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
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

    private static string Read(string path) => File.ReadAllText(path, Encoding.UTF8);

    private static string ToRelative(string root, string path)
        => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
}
