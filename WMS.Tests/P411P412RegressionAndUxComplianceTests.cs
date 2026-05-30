using System.Text;

namespace WMS.Tests;

public class P411P412RegressionAndUxComplianceTests
{
    [Fact]
    public void CoreRegressionSuite_ShouldCoverP4CriticalWorkflows()
    {
        var root = FindRepositoryRoot();
        var testSource = string.Join(
            Environment.NewLine,
            EnumerateFiles(Path.Combine(root, "WMS.Tests"), "*.cs")
                .Where(path => !string.Equals(Path.GetFileName(path), "P411P412RegressionAndUxComplianceTests.cs", StringComparison.OrdinalIgnoreCase))
                .Select(ReadUtf8));

        var requiredMarkers = new Dictionary<string, string>
        {
            ["nhập kho thường"] = "CompleteInbound_ShouldPostVoucherAndMarkCompleted",
            ["nhập kho thiếu vị trí"] = "CompleteInbound_ShouldRejectPositivePutawayQtyWithoutLocation",
            ["nhập kho theo lô và hạn dùng"] = "CompleteInbound_ShouldRequireLotAndExpiryForTrackedItem",
            ["nhập kho có cân trọng lượng thực tế"] = "RequireInboundCatchWeightAsync_ShouldBlockTrackedItemWithoutCapturedWeight",
            ["mã kiện hỗn hợp"] = "ScanLpn_JsonLookup_ShouldSummarizeMixedLpnDetails",
            ["mã kiện lồng nhau"] = "LpnHierarchyService_ShouldBlockSelfParentAndIndirectLoops",
            ["giữ chỗ và ghi sổ xuất theo lô"] = "PartialBatchPick_ShouldAllocateAndPostCorrectly",
            ["lấy thiếu và phân bổ lại"] = "ShortPick_ShouldAutoReallocateFromAlternativeLocation",
            ["lấy hàng đúng khay"] = "ClusterPicking_ConfirmWithWrongTote_ShouldFail",
            ["giao hàng có điều kiện vận đơn"] = "ConfirmShipping_ShouldRespectCarrierRequirementWhenEnabled",
            ["quét bằng hàng đợi nhận hàng"] = "QueuedConfirmReceiving_ShouldReturnJsonAndTreatRetryAsSuccess",
            ["quét bằng hàng đợi lấy hàng"] = "QueuedConfirmPickTask_ShouldReturnJsonAndAvoidDuplicateRetry",
            ["quét bằng hàng đợi di chuyển"] = "QueuedConfirmMovementTask_ShouldReturnJsonAndAvoidDuplicateRetry",
            ["quét kiện lên chuyến bằng hàng đợi"] = "QueuedScanShipmentLoadPackage_ShouldReturnJsonAndAvoidDuplicateMapping",
            ["chuyến xe yêu cầu quét kiện và cân thực tế"] = "ShipmentLoadDepartAsync_ShouldRequirePackageScanAndCatchWeightBeforeDeparture",
            ["vận đơn giả lập chống lặp"] = "CarrierIntegration_MockCreate_ShouldCreatePackageShipmentIdempotently",
            ["vận đơn HTTP qua hàng đợi tích hợp"] = "CarrierIntegration_HttpAdapter_ShouldEnqueueOutboxPayload",
            ["callback vận đơn chống lặp"] = "CarrierCallback_ShouldBeIdempotentAndUpdatePackageTracking",
            ["nhãn vận chuyển và chứng từ bàn giao"] = "ShippingDocument_ShouldCreateLoadManifestHandoverAndBatchLabels",
            ["đối soát giao hàng"] = "ShippingReconciliation_ShouldDetectDeliveryMismatches"
        };

        var missing = requiredMarkers
            .Where(pair => !testSource.Contains(pair.Value, StringComparison.Ordinal))
            .Select(pair => $"{pair.Key}: thiếu kiểm thử `{pair.Value}`")
            .ToList();

        Assert.True(missing.Count == 0, "Thiếu kiểm thử hồi quy P4-11:" + Environment.NewLine + string.Join(Environment.NewLine, missing));
    }

    [Fact]
    public void UserFacingSource_ShouldNotContainLegacyLanguageOrEncodingArtifacts()
    {
        var root = FindRepositoryRoot();
        var files = new[]
            {
                "Views",
                "Controllers",
                "Services",
                "Models",
                "ViewModels",
                Path.Combine("wwwroot", "js"),
                Path.Combine("wwwroot", "css")
            }
            .Select(path => Path.Combine(root, path))
            .Where(Directory.Exists)
            .SelectMany(path => EnumerateFiles(path, "*.*"))
            .Where(IsOperationalTextFile)
            .ToList();

        var banned = new (string Reason, string Value, StringComparison Comparison)[]
        {
            ("địa chỉ cục bộ", "local" + "host", StringComparison.OrdinalIgnoreCase),
            ("lỗi mã hóa tiếng Việt", TextFromCodePoints(0x00C4), StringComparison.Ordinal),
            ("lỗi mã hóa tiếng Việt", TextFromCodePoints(0x00C6), StringComparison.Ordinal),
            ("lỗi mã hóa tiếng Việt", TextFromCodePoints(0x00E1, 0x00BA), StringComparison.Ordinal),
            ("lỗi mã hóa tiếng Việt", TextFromCodePoints(0x00E1, 0x00BB), StringComparison.Ordinal),
            ("lỗi mã hóa tiếng Việt", TextFromCodePoints(0x004B, 0x0068, 0x00C3), StringComparison.Ordinal),
            ("lỗi mã hóa tiếng Việt", TextFromCodePoints(0x0043, 0x00C3), StringComparison.Ordinal),
            ("lỗi mã hóa tiếng Việt", TextFromCodePoints(0x0056, 0x00E1), StringComparison.Ordinal),
            ("thuật ngữ thiết bị quét cũ", "súng quét", StringComparison.OrdinalIgnoreCase),
            ("thuật ngữ ô quét cũ", "ô scan", StringComparison.OrdinalIgnoreCase),
            ("thuật ngữ xác nhận cũ", "RF xác nhận", StringComparison.OrdinalIgnoreCase),
            ("thuật ngữ quét cũ", "Chưa scan", StringComparison.Ordinal),
            ("tiêu đề lỗi tiếng Anh", "Business Rule Violation", StringComparison.OrdinalIgnoreCase),
            ("tiêu đề xuất tệp tiếng Anh", "LoadCode,Warehouse", StringComparison.OrdinalIgnoreCase),
            ("nhóm sóng nửa Anh nửa Việt", "Carrier Group", StringComparison.OrdinalIgnoreCase),
            ("nhóm tuyến nửa Anh nửa Việt", "Route Group", StringComparison.OrdinalIgnoreCase),
            ("thông báo kết nối tiếng Anh", "Network error", StringComparison.OrdinalIgnoreCase),
            ("chú thích thiết bị quét nửa Anh nửa Việt", "mã vạch guns", StringComparison.OrdinalIgnoreCase),
            ("chú thích thiết bị quét tiếng Anh", "USB/Bluetooth scanners", StringComparison.OrdinalIgnoreCase),
            ("chú thích thiết bị quét tiếng Anh", "scanner gun", StringComparison.OrdinalIgnoreCase),
            ("nhãn quy tắc nửa Anh nửa Việt", "Rule áp", StringComparison.OrdinalIgnoreCase),
            ("mô tả quy tắc nửa Anh nửa Việt", "Rule cụ", StringComparison.OrdinalIgnoreCase),
            ("cột phạm vi quy tắc nửa Anh nửa Việt", "Rule scope", StringComparison.OrdinalIgnoreCase),
            ("trạng thái quy tắc nửa Anh nửa Việt", "Rule có", StringComparison.OrdinalIgnoreCase),
            ("trạng thái quy tắc mặc định nửa Anh nửa Việt", "Rule mặc", StringComparison.OrdinalIgnoreCase),
            ("cột bộ kết nối nửa Anh nửa Việt", ">Adapter<", StringComparison.OrdinalIgnoreCase),
            ("nhãn bộ kết nối nửa Anh nửa Việt", "Kiểu adapter", StringComparison.OrdinalIgnoreCase),
            ("mô tả bộ kết nối nửa Anh nửa Việt", "adapter giả", StringComparison.OrdinalIgnoreCase),
            ("tiêu đề bãi đỗ tiếng Anh", "Yard Management", StringComparison.OrdinalIgnoreCase),
            ("nhãn vị trí bãi nửa Anh nửa Việt", "Spot trống", StringComparison.OrdinalIgnoreCase),
            ("nhãn vị trí bãi nửa Anh nửa Việt", "Spot đang", StringComparison.OrdinalIgnoreCase),
            ("nhãn công-ten-nơ tiếng Anh", "Container No.", StringComparison.OrdinalIgnoreCase),
            ("cột thời gian lưu bãi tiếng Anh", "Dwell (phút)", StringComparison.OrdinalIgnoreCase),
            ("cột miễn phí lưu bãi tiếng Anh", "Free (phút)", StringComparison.OrdinalIgnoreCase),
            ("nút xuất tệp gây khó hiểu", "Xuất tệp phân tách", StringComparison.OrdinalIgnoreCase),
            ("nhãn tùy chọn xuất tệp gây khó hiểu", "Tệp phân tách", StringComparison.OrdinalIgnoreCase),
            ("thông báo vị trí bãi chưa chuẩn", "Ô bãi xe", StringComparison.OrdinalIgnoreCase),
            ("thông báo số công-ten-nơ chưa chuẩn", "số xe/container", StringComparison.OrdinalIgnoreCase),
            ("nhãn xe hoặc công-ten-nơ chưa chuẩn", "Xe/container", StringComparison.OrdinalIgnoreCase)
        };

        var failures = new List<string>();
        foreach (var file in files)
        {
            var content = ReadUtf8(file);
            foreach (var pair in banned)
            {
                if (content.Contains(pair.Value, pair.Comparison))
                {
                    failures.Add($"{ToRelativePath(root, file)} chứa `{pair.Value}` ({pair.Reason}).");
                }
            }
        }

        Assert.True(failures.Count == 0, "Rà soát P4-12 phát hiện chuỗi chưa đạt:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void Roadmap_ShouldMarkP411AndP412Completed()
    {
        var roadmap = ReadUtf8(Path.Combine(FindRepositoryRoot(), "ENTERPRISE_NEXT_UPGRADE_ROADMAP.md"));

        Assert.Contains("### [x] P4-11 - Kiểm Thử Hồi Quy Nghiệp Vụ Lõi", roadmap, StringComparison.Ordinal);
        Assert.Contains("### [x] P4-12 - Rà Soát Ngôn Ngữ Và Trải Nghiệm Người Dùng Vận Hành", roadmap, StringComparison.Ordinal);
        Assert.Contains("dotnet build WMS.sln --no-restore -v:minimal", roadmap, StringComparison.Ordinal);
        Assert.Contains("dotnet test WMS.Tests\\WMS.Tests.csproj --no-restore -v:minimal", roadmap, StringComparison.Ordinal);
    }

    private static bool IsOperationalTextFile(string path)
    {
        if (path.Contains($"{Path.DirectorySeparatorChar}wwwroot{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var extension = Path.GetExtension(path);
        return extension is ".cs" or ".cshtml" or ".js" or ".css";
    }

    private static IEnumerable<string> EnumerateFiles(string root, string searchPattern)
        => Directory.EnumerateFiles(root, searchPattern, SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

    private static string ReadUtf8(string path)
        => File.ReadAllText(path, Encoding.UTF8);

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
