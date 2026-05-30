using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public sealed class MinerUOptions
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "http://[::1]:8000";
    public int TimeoutSeconds { get; set; } = 180;
    public int MaxFileSizeMb { get; set; } = 20;
    public bool AllowLegacyFallback { get; set; }
}

public sealed class MinerUDocumentParseResult
{
    public bool Success { get; set; }
    public string ParseStatus { get; set; } = "Failed";
    public string Provider { get; set; } = "MinerU";
    public string? Version { get; set; }
    public string? TaskId { get; set; }
    public string RawText { get; set; } = "";
    public string ContentListJson { get; set; } = "";
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public sealed class DocumentLineCandidate
{
    public int LineNumber { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public decimal? Quantity { get; set; }
    public string? UnitName { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
    public decimal Confidence { get; set; } = 0.5m;
    public string SourceText { get; set; } = "";
}

public sealed class MappedDocumentLine
{
    public int LineNumber { get; set; }
    public int? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public int? BaseUomId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? UnitName { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
    public bool IsMatched { get; set; }
    public bool RequiresReview { get; set; }
    public string MatchKind { get; set; } = "Unmatched";
    public int? SuggestedItemId { get; set; }
    public string? SuggestedItemCode { get; set; }
    public string? SuggestedItemName { get; set; }
    public decimal Confidence { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public sealed class VoucherDocumentIntakeResult
{
    public string Provider { get; set; } = "MinerU";
    public string ParseStatus { get; set; } = "Failed";
    public string RawText { get; set; } = "";
    public long LogId { get; set; }
    public decimal Confidence { get; set; }
    public List<MappedDocumentLine> Lines { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public interface IMineruDocumentParserClient
{
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    Task<MinerUDocumentParseResult> ParseAsync(IFormFile file, CancellationToken cancellationToken = default);
}

public interface IVoucherDocumentIntakeService
{
    Task<VoucherDocumentIntakeResult> AnalyzeAsync(IFormFile file, string actor, CancellationToken cancellationToken = default);
}

public sealed class MineruDocumentParserClient : IMineruDocumentParserClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MinerUOptions _options;
    private readonly ILogger<MineruDocumentParserClient> _logger;

    public MineruDocumentParserClient(
        IHttpClientFactory httpClientFactory,
        IOptions<MinerUOptions> options,
        ILogger<MineruDocumentParserClient>? logger = null)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger ?? NullLogger<MineruDocumentParserClient>.Instance;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BaseUrl))
            return false;

        try
        {
            using var client = CreateClient();
            using var response = await client.GetAsync(BuildUri("/health"), cancellationToken);
            if (!response.IsSuccessStatusCode)
                return false;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return document.RootElement.TryGetProperty("status", out var status)
                && string.Equals(status.GetString(), "healthy", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Không kiểm tra được trạng thái MinerU.");
            return false;
        }
    }

    public async Task<MinerUDocumentParseResult> ParseAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return Failed("Dịch vụ đọc chứng từ MinerU chưa được bật.");

        if (!await IsHealthyAsync(cancellationToken))
            return Failed("Dịch vụ đọc chứng từ MinerU chưa sẵn sàng. Vui lòng kiểm tra máy chủ MinerU nội bộ.");

        try
        {
            using var client = CreateClient();
            using var form = new MultipartFormDataContent();
            await using var input = file.OpenReadStream();
            using var streamContent = new StreamContent(input);
            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            form.Add(streamContent, "files", Path.GetFileName(file.FileName));
            form.Add(new StringContent("latin"), "lang_list");
            form.Add(new StringContent("auto"), "parse_method");
            form.Add(new StringContent("true"), "table_enable");
            form.Add(new StringContent("false"), "formula_enable");
            form.Add(new StringContent("false"), "image_analysis");
            form.Add(new StringContent("true"), "return_md");
            form.Add(new StringContent("false"), "return_middle_json");
            form.Add(new StringContent("false"), "return_model_output");
            form.Add(new StringContent("true"), "return_content_list");
            form.Add(new StringContent("false"), "return_images");
            form.Add(new StringContent("false"), "response_format_zip");

            using var response = await client.PostAsync(BuildUri("/file_parse"), form, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MinerU trả lỗi HTTP {StatusCode}: {Payload}", (int)response.StatusCode, payload);
                return Failed(response.StatusCode == HttpStatusCode.ServiceUnavailable
                    ? "MinerU đang bận hoặc chưa sẵn sàng."
                    : "MinerU chưa xử lý được chứng từ này.");
            }

            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            if (!root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Object)
                return Failed("MinerU trả về dữ liệu không có vùng kết quả.");

            JsonElement? firstResult = null;
            foreach (var property in results.EnumerateObject())
            {
                firstResult = property.Value;
                break;
            }

            if (!firstResult.HasValue || firstResult.Value.ValueKind != JsonValueKind.Object)
                return Failed("MinerU không trả về kết quả đọc chứng từ.");

            var result = firstResult.Value;
            var markdown = result.TryGetProperty("md_content", out var mdContent) ? ReadStringOrRawJson(mdContent) : "";
            var contentList = result.TryGetProperty("content_list", out var contentListElement) ? ReadStringOrRawJson(contentListElement) : "";

            return new MinerUDocumentParseResult
            {
                Success = true,
                ParseStatus = string.IsNullOrWhiteSpace(markdown) && string.IsNullOrWhiteSpace(contentList) ? "Partial" : "Success",
                Version = root.TryGetProperty("version", out var version) ? version.GetString() : null,
                TaskId = root.TryGetProperty("task_id", out var taskId) ? taskId.GetString() : null,
                RawText = markdown,
                ContentListJson = contentList,
                Warnings = string.IsNullOrWhiteSpace(markdown) && string.IsNullOrWhiteSpace(contentList)
                    ? new List<string> { "MinerU đã phản hồi nhưng chưa có nội dung có thể khai thác." }
                    : new List<string>()
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Gọi MinerU thất bại.");
            return Failed("Không thể kết nối hoặc đọc phản hồi từ MinerU.");
        }
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient("MinerU");
        var timeout = _options.TimeoutSeconds > 0 ? _options.TimeoutSeconds : 180;
        client.Timeout = TimeSpan.FromSeconds(timeout);
        return client;
    }

    private Uri BuildUri(string path)
    {
        var baseUrl = (_options.BaseUrl ?? "").Trim().TrimEnd('/');
        return new Uri($"{baseUrl}{path}", UriKind.Absolute);
    }

    private static MinerUDocumentParseResult Failed(string message)
        => new()
        {
            Success = false,
            ParseStatus = "Failed",
            ErrorMessage = message,
            Warnings = new List<string> { message }
        };

    private static string ReadStringOrRawJson(JsonElement element)
        => element.ValueKind == JsonValueKind.String
            ? element.GetString() ?? ""
            : element.GetRawText();
}

public sealed class VoucherDocumentIntakeService : IVoucherDocumentIntakeService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".docx", ".pptx", ".xlsx"
    };

    private static readonly Regex HtmlRowRegex = new("<tr[^>]*>(.*?)</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex HtmlCellRegex = new("<t[dh][^>]*>(.*?)</t[dh]>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex HtmlTagRegex = new("<.*?>", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex MarkdownSeparatorRegex = new("^\\s*\\|?\\s*:?-{3,}:?\\s*(\\|\\s*:?-{3,}:?\\s*)+\\|?\\s*$", RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMineruDocumentParserClient _parserClient;
    private readonly MinerUOptions _options;
    private readonly ILogger<VoucherDocumentIntakeService> _logger;

    public VoucherDocumentIntakeService(
        AppDbContext db,
        IUnitOfWork unitOfWork,
        IMineruDocumentParserClient parserClient,
        IOptions<MinerUOptions> options,
        ILogger<VoucherDocumentIntakeService>? logger = null)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _parserClient = parserClient;
        _options = options.Value;
        _logger = logger ?? NullLogger<VoucherDocumentIntakeService>.Instance;
    }

    public async Task<VoucherDocumentIntakeResult> AnalyzeAsync(IFormFile file, string actor, CancellationToken cancellationToken = default)
    {
        ValidateFile(file);

        var stopwatch = Stopwatch.StartNew();
        var storedPath = await StoreDocumentAsync(file, cancellationToken);
        var parse = await _parserClient.ParseAsync(file, cancellationToken);
        if (!parse.Success)
        {
            await SaveLogAsync(file, storedPath, parse, Array.Empty<MappedDocumentLine>(), 0m, stopwatch.ElapsedMilliseconds, actor, cancellationToken);
            throw new BusinessRuleException(parse.ErrorMessage ?? "MinerU chưa xử lý được chứng từ.", "MINERU_PARSE_FAILED", nameof(AiOcrLog));
        }

        var candidates = ExtractCandidates(parse);
        var mappedLines = await MapCandidatesAsync(candidates, cancellationToken);
        var warnings = parse.Warnings
            .Concat(mappedLines.SelectMany(line => line.Warnings))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (candidates.Count == 0)
            warnings.Add("Chưa tìm thấy bảng dòng hàng đủ rõ để đề xuất vào phiếu.");

        var confidence = CalculateConfidence(mappedLines);
        var parseStatus = ResolveParseStatus(mappedLines, candidates.Count, warnings.Count);
        parse.ParseStatus = parseStatus;
        var log = await SaveLogAsync(file, storedPath, parse, mappedLines, confidence, stopwatch.ElapsedMilliseconds, actor, cancellationToken);

        return new VoucherDocumentIntakeResult
        {
            Provider = parse.Provider,
            ParseStatus = parseStatus,
            RawText = parse.RawText,
            LogId = log.AiOcrLogId,
            Confidence = confidence,
            Lines = mappedLines,
            Warnings = warnings
        };
    }

    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length <= 0)
            throw new BusinessRuleException("Vui lòng chọn chứng từ cần đọc.", "DOCUMENT_FILE_REQUIRED", "Document");

        var extension = Path.GetExtension(file.FileName) ?? "";
        if (!AllowedExtensions.Contains(extension))
            throw new BusinessRuleException("Chỉ hỗ trợ PDF, ảnh, DOCX, PPTX hoặc XLSX.", "DOCUMENT_FILE_TYPE_INVALID", "Document");

        var maxBytes = Math.Max(1, _options.MaxFileSizeMb <= 0 ? 20 : _options.MaxFileSizeMb) * 1024L * 1024L;
        if (file.Length > maxBytes)
            throw new BusinessRuleException($"Chứng từ vượt quá {_options.MaxFileSizeMb} MB.", "DOCUMENT_FILE_TOO_LARGE", "Document");
    }

    private static async Task<string> StoreDocumentAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "uploads", "document-intake");
        Directory.CreateDirectory(root);

        var extension = Path.GetExtension(file.FileName);
        var normalizedStem = NormalizeFileStem(Path.GetFileNameWithoutExtension(file.FileName));
        var storedName = $"{VietnamTime.FileStamp("yyyyMMddHHmmssfff")}_{Guid.NewGuid():N}_{normalizedStem}{extension}";
        var physicalPath = Path.Combine(root, storedName);

        await using var stream = File.Create(physicalPath);
        await file.CopyToAsync(stream, cancellationToken);

        return Path.Combine("App_Data", "uploads", "document-intake", storedName).Replace('\\', '/');
    }

    private async Task<AiOcrLog> SaveLogAsync(
        IFormFile file,
        string storedPath,
        MinerUDocumentParseResult parse,
        IReadOnlyCollection<MappedDocumentLine> lines,
        decimal confidence,
        long elapsedMilliseconds,
        string actor,
        CancellationToken cancellationToken)
    {
        var log = new AiOcrLog
        {
            ImageUrl = storedPath,
            FileName = Path.GetFileName(file.FileName),
            FileSize = file.Length,
            OcrProvider = parse.Provider,
            ModelVersion = parse.Version,
            RawJsonResponse = parse.ContentListJson,
            ParsedData = parse.RawText,
            ConfidenceScore = confidence,
            DetectedItems = lines.Count,
            ProcessingTimeMs = elapsedMilliseconds > int.MaxValue ? int.MaxValue : (int)elapsedMilliseconds,
            Status = parse.ParseStatus switch
            {
                "Success" => 1,
                "Partial" => 2,
                _ => 3
            },
            ErrorMessage = parse.ErrorMessage,
            CreatedBy = string.IsNullOrWhiteSpace(actor) ? "system-document-intake" : actor.Trim()
        };

        _db.AiOcrLogs.Add(log);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return log;
    }

    private static string ResolveParseStatus(IReadOnlyCollection<MappedDocumentLine> lines, int candidateCount, int warningCount)
    {
        if (candidateCount == 0 || lines.Count == 0)
            return "Failed";

        return warningCount == 0 && lines.All(line => line.IsMatched && !line.RequiresReview)
            ? "Success"
            : "Partial";
    }

    private static decimal CalculateConfidence(IReadOnlyCollection<MappedDocumentLine> lines)
    {
        if (lines.Count == 0)
            return 0m;

        var ready = lines.Count(line => line.IsMatched && !line.RequiresReview);
        return Math.Round((decimal)ready / lines.Count, 4, MidpointRounding.AwayFromZero);
    }

    private static List<DocumentLineCandidate> ExtractCandidates(MinerUDocumentParseResult parse)
    {
        var candidates = new List<DocumentLineCandidate>();
        ExtractFromContentList(parse.ContentListJson, candidates);
        ExtractFromMarkdown(parse.RawText, candidates);

        return candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.ItemCode) || !string.IsNullOrWhiteSpace(candidate.ItemName))
            .GroupBy(candidate => string.Join("|",
                NormalizeKey(candidate.ItemCode),
                NormalizeKey(candidate.ItemName),
                candidate.Quantity?.ToString(CultureInfo.InvariantCulture) ?? "",
                NormalizeKey(candidate.UnitName),
                NormalizeKey(candidate.LotNumber)))
            .Select(group => group.First())
            .Select((candidate, index) =>
            {
                candidate.LineNumber = index + 1;
                return candidate;
            })
            .ToList();
    }

    private static void ExtractFromContentList(string contentListJson, ICollection<DocumentLineCandidate> candidates)
    {
        if (string.IsNullOrWhiteSpace(contentListJson))
            return;

        try
        {
            using var document = JsonDocument.Parse(contentListJson);
            foreach (var block in FlattenContentBlocks(document.RootElement))
            {
                if (!block.TryGetProperty("type", out var type)
                    || !string.Equals(type.GetString(), "table", StringComparison.OrdinalIgnoreCase)
                    || !block.TryGetProperty("table_body", out var tableBody))
                {
                    continue;
                }

                AddCandidatesFromRows(ParseHtmlRows(tableBody.GetString() ?? ""), candidates);
            }
        }
        catch (JsonException)
        {
            // Markdown extraction remains as a fallback for malformed content-list payloads.
        }
    }

    private static IEnumerable<JsonElement> FlattenContentBlocks(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
            yield break;

        foreach (var child in element.EnumerateArray())
        {
            if (child.ValueKind == JsonValueKind.Object)
            {
                yield return child;
                continue;
            }

            if (child.ValueKind == JsonValueKind.Array)
            {
                foreach (var nested in FlattenContentBlocks(child))
                    yield return nested;
            }
        }
    }

    private static void ExtractFromMarkdown(string markdown, ICollection<DocumentLineCandidate> candidates)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return;

        var rows = new List<List<string>>();
        foreach (var rawLine in markdown.Split('\n', StringSplitOptions.TrimEntries))
        {
            var line = rawLine.Trim();
            if (!line.Contains('|', StringComparison.Ordinal))
            {
                AddCandidatesFromRows(rows, candidates);
                rows.Clear();
                continue;
            }

            if (MarkdownSeparatorRegex.IsMatch(line))
                continue;

            var cells = line.Trim('|')
                .Split('|', StringSplitOptions.TrimEntries)
                .Select(CleanCell)
                .ToList();
            if (cells.Count > 1)
                rows.Add(cells);
        }

        AddCandidatesFromRows(rows, candidates);
    }

    private static IEnumerable<List<string>> ParseHtmlRows(string html)
    {
        foreach (Match rowMatch in HtmlRowRegex.Matches(html))
        {
            var cells = HtmlCellRegex.Matches(rowMatch.Groups[1].Value)
                .Select(match => CleanCell(match.Groups[1].Value))
                .ToList();
            if (cells.Count > 1)
                yield return cells;
        }
    }

    private static void AddCandidatesFromRows(IEnumerable<List<string>> rows, ICollection<DocumentLineCandidate> candidates)
    {
        var tableRows = rows.ToList();
        if (tableRows.Count < 2)
            return;

        var headers = tableRows[0];
        var map = BuildColumnMap(headers);
        if (!map.ContainsKey("itemCode") && !map.ContainsKey("itemName"))
            return;

        foreach (var cells in tableRows.Skip(1))
        {
            var candidate = BuildCandidate(map, cells);
            if (candidate != null)
                candidates.Add(candidate);
        }
    }

    private static Dictionary<string, int> BuildColumnMap(IReadOnlyList<string> headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < headers.Count; index++)
        {
            var normalized = NormalizeKey(headers[index]);
            if (ContainsAny(normalized, "mavattu", "mahang", "mahh", "mahanghoa", "itemcode", "sku", "barcode"))
                map.TryAdd("itemCode", index);
            else if (ContainsAny(normalized, "tenvattu", "tenhang", "tenhanghoa", "itemname", "productname", "description", "mota"))
                map.TryAdd("itemName", index);
            else if (ContainsAny(normalized, "soluong", "quantity", "qty", "sl"))
                map.TryAdd("quantity", index);
            else if (ContainsAny(normalized, "donvi", "dvt", "unit", "uom"))
                map.TryAdd("unitName", index);
            else if (ContainsAny(normalized, "dongia", "unitprice", "price"))
                map.TryAdd("unitPrice", index);
            else if (ContainsAny(normalized, "solo", "lot", "batch"))
                map.TryAdd("lotNumber", index);
            else if (ContainsAny(normalized, "ngaysx", "nsx", "ngaysanxuat", "manufacturingdate", "mfgdate"))
                map.TryAdd("manufacturingDate", index);
            else if (ContainsAny(normalized, "hansd", "hsd", "handung", "expirydate", "expirationdate"))
                map.TryAdd("expiryDate", index);
            else if (ContainsAny(normalized, "ghichu", "note", "remark"))
                map.TryAdd("notes", index);
        }

        return map;
    }

    private static DocumentLineCandidate? BuildCandidate(IReadOnlyDictionary<string, int> map, IReadOnlyList<string> cells)
    {
        string? Cell(string key)
            => map.TryGetValue(key, out var index) && index >= 0 && index < cells.Count
                ? cells[index]
                : null;

        var itemCode = NullIfBlank(Cell("itemCode"));
        var itemName = NullIfBlank(Cell("itemName"));
        if (itemCode == null && itemName == null)
            return null;

        return new DocumentLineCandidate
        {
            ItemCode = itemCode,
            ItemName = itemName,
            Quantity = ParseDecimal(Cell("quantity")),
            UnitName = NullIfBlank(Cell("unitName")),
            UnitPrice = ParseDecimal(Cell("unitPrice")),
            LotNumber = NullIfBlank(Cell("lotNumber")),
            ManufacturingDate = ParseDate(Cell("manufacturingDate")),
            ExpiryDate = ParseDate(Cell("expiryDate")),
            Notes = NullIfBlank(Cell("notes")),
            Confidence = 0.65m,
            SourceText = string.Join(" | ", cells)
        };
    }

    private async Task<List<MappedDocumentLine>> MapCandidatesAsync(IReadOnlyList<DocumentLineCandidate> candidates, CancellationToken cancellationToken)
    {
        var items = await _db.Items.AsNoTracking()
            .Where(item => item.IsActive)
            .Select(item => new
            {
                item.ItemId,
                item.ItemCode,
                item.ItemName,
                item.SkuCode,
                item.Barcode,
                item.BaseUomId
            })
            .ToListAsync(cancellationToken);

        return candidates.Select(candidate =>
        {
            var line = new MappedDocumentLine
            {
                LineNumber = candidate.LineNumber,
                ItemCode = candidate.ItemCode,
                ItemName = candidate.ItemName,
                Quantity = candidate.Quantity ?? 0m,
                UnitPrice = candidate.UnitPrice ?? 0m,
                UnitName = candidate.UnitName,
                LotNumber = candidate.LotNumber,
                ManufacturingDate = candidate.ManufacturingDate,
                ExpiryDate = candidate.ExpiryDate,
                Notes = candidate.Notes,
                Confidence = candidate.Confidence
            };

            var exact = items.FirstOrDefault(item => MatchesExact(candidate.ItemCode, item.ItemCode, item.SkuCode, item.Barcode))
                ?? items.FirstOrDefault(item => MatchesExact(candidate.ItemName, item.ItemName));

            if (exact != null)
            {
                line.ItemId = exact.ItemId;
                line.ItemCode = exact.ItemCode;
                line.ItemName = exact.ItemName;
                line.BaseUomId = exact.BaseUomId;
                line.IsMatched = true;
                line.MatchKind = "Exact";
                line.Confidence = 1m;
            }
            else
            {
                var fuzzy = items.FirstOrDefault(item => IsFuzzyNameMatch(candidate.ItemName, item.ItemName));
                if (fuzzy != null)
                {
                    line.SuggestedItemId = fuzzy.ItemId;
                    line.SuggestedItemCode = fuzzy.ItemCode;
                    line.SuggestedItemName = fuzzy.ItemName;
                    line.MatchKind = "FuzzySuggestion";
                    line.RequiresReview = true;
                    line.Warnings.Add($"Dòng {candidate.LineNumber}: có gợi ý vật tư [{fuzzy.ItemCode}] {fuzzy.ItemName}, cần chọn thủ công.");
                }
                else
                {
                    line.MatchKind = "Unmatched";
                    line.RequiresReview = true;
                    line.Warnings.Add($"Dòng {candidate.LineNumber}: chưa khớp vật tư nội bộ.");
                }
            }

            if (line.Quantity <= 0)
            {
                line.RequiresReview = true;
                line.Warnings.Add($"Dòng {candidate.LineNumber}: chưa nhận diện được số lượng hợp lệ.");
            }

            return line;
        }).ToList();
    }

    private static bool MatchesExact(string? raw, params string?[] candidates)
    {
        var normalized = NormalizeKey(raw);
        return normalized.Length > 0
            && candidates.Any(candidate => string.Equals(normalized, NormalizeKey(candidate), StringComparison.Ordinal));
    }

    private static bool IsFuzzyNameMatch(string? left, string? right)
    {
        var normalizedLeft = NormalizeKey(left);
        var normalizedRight = NormalizeKey(right);
        if (normalizedLeft.Length < 4 || normalizedRight.Length < 4)
            return false;

        if (normalizedLeft.Contains(normalizedRight, StringComparison.Ordinal)
            || normalizedRight.Contains(normalizedLeft, StringComparison.Ordinal))
        {
            return true;
        }

        var leftTokens = Tokenize(left);
        var rightTokens = Tokenize(right);
        return leftTokens.Count > 0 && leftTokens.Count(token => rightTokens.Contains(token)) >= Math.Min(2, leftTokens.Count);
    }

    private static HashSet<string> Tokenize(string? value)
        => NormalizeText(value)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length >= 3)
            .ToHashSet(StringComparer.Ordinal);

    private static string CleanCell(string? value)
    {
        var decoded = WebUtility.HtmlDecode(value ?? "");
        return HtmlTagRegex.Replace(decoded, " ")
            .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim().Replace(" ", "");
        if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.GetCultureInfo("vi-VN"), out var vi))
            return vi;
        if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var invariant))
            return invariant;

        return null;
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "d-M-yyyy" };
        if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
            return exact.Date;
        if (DateTime.TryParse(value.Trim(), CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var parsed))
            return parsed.Date;

        return null;
    }

    private static bool ContainsAny(string normalized, params string[] expected)
        => expected.Any(value => normalized.Contains(value, StringComparison.Ordinal));

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeFileStem(string? value)
    {
        var text = NormalizeText(value);
        var safe = Regex.Replace(text, "[^a-z0-9]+", "-", RegexOptions.IgnoreCase).Trim('-');
        return string.IsNullOrWhiteSpace(safe) ? "document" : safe[..Math.Min(safe.Length, 60)];
    }

    private static string NormalizeKey(string? value)
        => Regex.Replace(NormalizeText(value), "[^a-z0-9]+", "", RegexOptions.IgnoreCase);

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
