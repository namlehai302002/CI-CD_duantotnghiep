using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using WMS.Controllers;

namespace WMS.Tests;

public sealed class WorldClassZeroMarkerEvidenceTests
{
    [Fact]
    public void AppViews_ShouldHaveNoRoughInlineUiMarkers()
    {
        var root = FindRepositoryRoot();
        var viewRoot = Path.Combine(root, "Views");
        var markers = new[]
        {
            "<" + "style",
            "style" + "=",
            "href=\"" + "/",
            "action=\"" + "/"
        };

        var failures = Directory.EnumerateFiles(viewRoot, "*.cshtml", SearchOption.AllDirectories)
            .SelectMany(path =>
            {
                var content = File.ReadAllText(path).ToLowerInvariant();
                return markers
                    .Where(marker => content.Contains(marker, StringComparison.Ordinal))
                    .Select(marker => $"{Path.GetRelativePath(root, path)} contains {marker}.");
            })
            .ToList();

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void AppHtml_ShouldHaveNoInlineEventHandlerAttributes()
    {
        var root = FindRepositoryRoot();
        var files = Directory.EnumerateFiles(Path.Combine(root, "Views"), "*.cshtml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "wwwroot"), "*.html", SearchOption.AllDirectories)
                .Where(path => !IsExcluded(root, path)));

        var failures = files
            .Select(path => new { Path = path, Content = File.ReadAllText(path) })
            .Where(file => Regex.IsMatch(file.Content, @"\son[a-zA-Z]+\s*=", RegexOptions.CultureInvariant))
            .Select(file => $"{Path.GetRelativePath(root, file.Path)} contains inline event handler attribute.")
            .ToList();

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void AppViewScripts_ShouldNotAssignInlineEventProperties()
    {
        var root = FindRepositoryRoot();
        var files = Directory.EnumerateFiles(Path.Combine(root, "Views"), "*.cshtml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "wwwroot"), "*.js", SearchOption.AllDirectories)
                .Where(path => !IsExcluded(root, path)));

        var failures = files
            .Select(path => new { Path = path, Content = File.ReadAllText(path) })
            .Where(file => Regex.IsMatch(file.Content, @"\.(onclick|onchange|oninput|oninvalid|ondrop|ondragover|ondragleave)\s*=", RegexOptions.CultureInvariant))
            .Select(file => $"{Path.GetRelativePath(root, file.Path)} assigns inline event property.")
            .ToList();

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void LocalVerificationBypass_ShouldRequireDevelopmentLoopbackHostAndConfiguredUser()
    {
        var method = typeof(AccountController).GetMethod("IsLocalVerificationRequestAllowed", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var loopbackHost = "local" + "host";
        const string configuredUser = "verify-admin";

        bool Allowed(
            bool isDevelopment = true,
            bool isEnabled = true,
            bool bypassMfaForLoopback = true,
            string? configured = configuredUser,
            string? user = configuredUser,
            IPAddress? remoteIp = null,
            string? requestHost = null)
        {
            return (bool)method!.Invoke(null, new object?[]
            {
                isDevelopment,
                isEnabled,
                bypassMfaForLoopback,
                configured,
                user,
                remoteIp ?? IPAddress.Loopback,
                requestHost ?? loopbackHost
            })!;
        }

        Assert.True(Allowed());
        Assert.False(Allowed(isDevelopment: false));
        Assert.False(Allowed(isEnabled: false));
        Assert.False(Allowed(bypassMfaForLoopback: false));
        Assert.False(Allowed(user: "wrong-user"));
        Assert.False(Allowed(remoteIp: IPAddress.Parse("10.20.30.40")));
        Assert.False(Allowed(requestHost: "wms.example.com"));

        var program = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "Program.cs"));
        Assert.Contains("LOCAL_VERIFICATION_DISABLED_OUTSIDE_DEVELOPMENT", program, StringComparison.Ordinal);
    }

    [Fact]
    public void IncludedRepoText_ShouldHaveNoHardcodedLoopbackMarkersOutsideLocalConfig()
    {
        var root = FindRepositoryRoot();
        var loopbackName = "local" + "host";
        var loopbackV4 = IPAddress.Loopback.ToString();

        var failures = EnumerateIncludedTextFiles(root)
            .Where(path => !string.Equals(Path.GetFileName(path), "appsettings.json", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path =>
            {
                var content = File.ReadAllText(path);
                var relative = Path.GetRelativePath(root, path);
                var hits = new List<string>();
                if (content.Contains(loopbackName, StringComparison.OrdinalIgnoreCase))
                    hits.Add($"{relative} contains loopback host literal.");
                if (content.Contains(loopbackV4, StringComparison.OrdinalIgnoreCase))
                    hits.Add($"{relative} contains loopback IPv4 literal.");
                return hits;
            })
            .ToList();

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void PrintViews_ShouldUseExternalPrintCssFiles()
    {
        var root = FindRepositoryRoot();
        var expected = new[]
        {
            "wwwroot/css/wms-print-labels.css",
            "wwwroot/css/wms-customer-label-print.css",
            "wwwroot/css/wms-shipping-document.css"
        };

        foreach (var relative in expected)
        {
            var fullPath = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(fullPath), $"Missing print stylesheet: {relative}");
            Assert.True(new FileInfo(fullPath).Length > 200, $"Print stylesheet is unexpectedly small: {relative}");
        }
    }

    [Fact]
    public void OfflineShell_ShouldUseExternalAssetsWithoutInlineUiMarkers()
    {
        var root = FindRepositoryRoot();
        var offline = File.ReadAllText(Path.Combine(root, "wwwroot", "offline.html"));
        var serviceWorker = File.ReadAllText(Path.Combine(root, "wwwroot", "service-worker.js"));

        Assert.DoesNotContain("<" + "style", offline, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("style" + "=", offline, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick" + "=", offline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"css/wms-offline.css\"", offline, StringComparison.Ordinal);
        Assert.Contains("src=\"js/offline-page.js\"", offline, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/css/wms-offline.css\"", offline, StringComparison.Ordinal);
        Assert.DoesNotContain("src=\"/js/offline-page.js\"", offline, StringComparison.Ordinal);
        Assert.Contains("'/css/wms-offline.css'", serviceWorker, StringComparison.Ordinal);
        Assert.Contains("'/js/offline-page.js'", serviceWorker, StringComparison.Ordinal);
    }

    [Fact]
    public void AppViews_ShouldNotFallbackToRawEnumToStringLabels()
    {
        var root = FindRepositoryRoot();
        var viewRoot = Path.Combine(root, "Views");
        var failures = Directory.EnumerateFiles(viewRoot, "*.cshtml", SearchOption.AllDirectories)
            .Select(path => new { Path = path, Content = File.ReadAllText(path) })
            .Where(file => Regex.IsMatch(file.Content, @"_\s*=>\s*[A-Za-z0-9_?.]+\s*\.ToString\s*\(\s*\)"))
            .Select(file => $"{Path.GetRelativePath(root, file.Path)} contains raw enum ToString fallback.")
            .ToList();

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private static IEnumerable<string> EnumerateIncludedTextFiles(string root)
    {
        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".cshtml", ".js", ".css", ".md", ".ts", ".ps1", ".json"
        };

        return Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(path => extensions.Contains(Path.GetExtension(path)))
            .Where(path => !IsExcluded(root, path));
    }

    private static bool IsExcluded(string root, string path)
    {
        var relativeParts = Path.GetRelativePath(root, path)
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", "node_modules", "artifacts", "test-results", "uploads", "App_Data", "playwright-report"
        };

        if (relativeParts.Any(part => excluded.Contains(part)))
            return true;

        if (relativeParts.Any(part => string.Equals(part, ".auth", StringComparison.OrdinalIgnoreCase))
            || Path.GetRelativePath(root, path).Contains("-snapshots" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return true;

        return relativeParts.Length >= 2
            && string.Equals(relativeParts[0], "wwwroot", StringComparison.OrdinalIgnoreCase)
            && string.Equals(relativeParts[1], "lib", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "WMS.sln")))
            dir = dir.Parent;

        return dir?.FullName ?? throw new DirectoryNotFoundException("Could not locate WMS.sln.");
    }
}
