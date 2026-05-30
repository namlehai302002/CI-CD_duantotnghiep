using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;

using Microsoft.EntityFrameworkCore;

using WMS.Data;

using WMS.Models;

using WMS.ViewModels;

using WMS.Authorization;

using WMS.Common;

using WMS.Services;

using System.Text.Json;

using System.Linq;

using ClosedXML.Excel;

using System.Globalization;

using System.Data;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.StaticFiles;

namespace WMS.Controllers;

public partial class VouchersController
{

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AnalyzeReceipt(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("Vui lòng chọn chứng từ cần đọc.");

        var allowLegacyFallback = _config.GetValue<bool>("MinerU:AllowLegacyFallback");
        try
        {
            var intake = await _voucherDocumentIntakeService.AnalyzeAsync(
                file,
                User.Identity?.Name ?? "system",
                HttpContext.RequestAborted);

            return Ok(new
            {
                data = JsonSerializer.Serialize(intake.Lines),
                rawText = intake.RawText,
                provider = intake.Provider,
                logId = intake.LogId,
                warnings = intake.Warnings,
                confidence = intake.Confidence,
                parseStatus = intake.ParseStatus
            });
        }
        catch (BusinessRuleException ex) when (allowLegacyFallback)
        {
            _logger.LogWarning(ex, "MinerU không xử lý được chứng từ, chuyển sang bộ đọc chứng từ dự phòng.");
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(UserSafeError.From(ex));
        }

        try
        {
            // Basic upload hardening
            const long maxBytes = 4 * 1024 * 1024; // 4 MB (Groq base64 limit)
            if (file.Length > maxBytes) return BadRequest("Tệp chứng từ quá lớn cho chế độ dự phòng. Vui lòng chọn ảnh ≤ 4MB.");

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
            var allowedExt = new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExt.Contains(ext)) return BadRequest("Chế độ đọc chứng từ dự phòng chỉ hỗ trợ JPG/PNG/WEBP.");

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();
            var base64Image = Convert.ToBase64String(imageBytes);
            var mimeType = file.ContentType;

            var imageUrl = await StoreLegacyReceiptDocumentAsync(file.FileName, imageBytes, HttpContext.RequestAborted);

            // === PROMPT CHUNG CHO CẢ 2 PROVIDER ===
            var ocrPrompt = "Extract ALL line items from this business document image. Return ONLY a raw JSON array without markdown formatting or code blocks. Include the unit of measurement. Format: [{\"ItemCode\":\"abc\",\"ItemName\":\"xyz\",\"Quantity\":1.0,\"UnitPrice\":1000,\"UnitName\":\"Cái\"}]. UnitName examples: Cái, Bộ, Cuộn, Chai, m², kg, Hộp, Thùng, Pcs, Pair, Set. If UnitPrice is not visible, use 0 instead of null.";

            // === GỜI AI: GROQ (primary) → GEMINI (fallback) ===
            string textResult;
            string providerUsed;
            using var client = _httpClientFactory.CreateClient("AiOcr");

            var groqKey = _config["GroqApiKey"];
            var geminiKey = _config["GeminiApiKey"];

            if (!string.IsNullOrEmpty(groqKey))
            {
                // --- GROQ (OpenAI-compatible, 30 RPM free) ---
                var groqBody = new
                {
                    model = "meta-llama/llama-4-scout-17b-16e-instruct",
                    messages = new object[]
                    {
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new { type = "text", text = ocrPrompt },
                                new { type = "image_url", image_url = new { url = $"data:{mimeType};base64,{base64Image}" } }
                            }
                        }
                    },
                    temperature = 0.1,
                    max_tokens = 4096
                };

                var groqRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
                groqRequest.Headers.Add("Authorization", $"Bearer {groqKey}");
                groqRequest.Content = new StringContent(JsonSerializer.Serialize(groqBody), System.Text.Encoding.UTF8, "application/json");

                var groqResponse = await client.SendAsync(groqRequest);
                var groqResponseStr = await groqResponse.Content.ReadAsStringAsync();

                if (groqResponse.IsSuccessStatusCode)
                {
                    var groqDoc = JsonDocument.Parse(groqResponseStr);
                    textResult = groqDoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "[]";
                    providerUsed = "Groq";
                    _logger.LogInformation("[Đọc chứng từ dự phòng] Groq OK — model: llama-4-scout");
                }
                else if (!string.IsNullOrEmpty(geminiKey))
                {
                    // Groq failed → fallback to Gemini
                    _logger.LogWarning("[Đọc chứng từ dự phòng] Groq failed ({StatusCode}), falling back to Gemini...", (int)groqResponse.StatusCode);
                    (textResult, providerUsed) = await CallGeminiOcr(client, geminiKey, ocrPrompt, mimeType, base64Image);
                }
                else
                {
                    var code = (int)groqResponse.StatusCode;
                    if (code == 429) return BadRequest("Dịch vụ đọc chứng từ đã hết lượt tạm thời. Vui lòng thử lại sau 1 phút.");
                    _logger.LogError("[Đọc chứng từ dự phòng] Groq Error ({StatusCode}): {Response}", code, groqResponseStr);
                    return BadRequest("Lỗi dịch vụ đọc chứng từ dự phòng. Vui lòng thử lại sau.");
                }
            }
            else if (!string.IsNullOrEmpty(geminiKey))
            {
                // No Groq key → use Gemini directly
                (textResult, providerUsed) = await CallGeminiOcr(client, geminiKey, ocrPrompt, mimeType, base64Image);
            }
            else
            {
                return BadRequest("Thiếu cấu hình dịch vụ đọc chứng từ dự phòng.");
            }

            // === STRIP MARKDOWN CODE FENCES ===
            if (textResult != null)
            {
                textResult = textResult.Trim();
                if (textResult.StartsWith("```", StringComparison.Ordinal))
                {
                    var firstNewline = textResult.IndexOf('\n');
                    if (firstNewline >= 0) textResult = textResult[(firstNewline + 1)..];
                    if (textResult.EndsWith("```", StringComparison.Ordinal)) textResult = textResult[..^3];
                }
                textResult = textResult.Trim();
            }
            else
            {
                textResult = "[]";
            }

            // === LƯU LOG OCR ===
            var ocrLog = new AiOcrLog
            {
                ImageUrl = imageUrl,
                FileName = file.FileName,
                FileSize = file.Length,
                ParsedData = textResult,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = VietnamNow
            };
            _db.Set<AiOcrLog>().Add(ocrLog);
            await _unitOfWork.SaveChangesAsync();

            // === TỰ ĐỘNG MAP VẬT TƯ ===
            var mappedItems = new List<object>();
            var warnings = new List<string>();
            try
            {
                using var doc = JsonDocument.Parse(textResult);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var defaultUomId = await _db.UnitsOfMeasure.Select(u => u.UomId).FirstOrDefaultAsync();
                    var allUoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).ToListAsync();
                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        var code = el.TryGetProperty("ItemCode", out var codeProp) ? codeProp.GetString() : null;
                        var name = el.TryGetProperty("ItemName", out var nameProp) ? nameProp.GetString() : null;
                        var price = el.TryGetProperty("UnitPrice", out var priceProp) && priceProp.ValueKind == JsonValueKind.Number && priceProp.TryGetDecimal(out var decPrice) ? decPrice : 0;
                        var qty = el.TryGetProperty("Quantity", out var qtyProp) && qtyProp.ValueKind == JsonValueKind.Number && qtyProp.TryGetDecimal(out var decQty) ? decQty : 1;
                        var unitName = el.TryGetProperty("UnitName", out var unitProp) ? unitProp.GetString() : null;

                        // Khớp đơn vị tính từ tên đơn vị trên chứng từ.
                        int matchedUomId = defaultUomId;
                        if (!string.IsNullOrEmpty(unitName))
                        {
                            var unitLower = unitName.ToLower().Trim();
                            var matchedUom = allUoms.FirstOrDefault(u =>
                                u.UomCode.ToLower() == unitLower ||
                                u.UomName.ToLower() == unitLower ||
                                u.UomName.ToLower().Contains(unitLower) ||
                                unitLower.Contains(u.UomName.ToLower()) ||
                                u.UomCode.ToLower().Contains(unitLower) ||
                                unitLower.Contains(u.UomCode.ToLower())
                            );
                            if (matchedUom != null) matchedUomId = matchedUom.UomId;
                        }

                        if (!string.IsNullOrEmpty(code) || !string.IsNullOrEmpty(name))
                        {
                            var searchName = name ?? code;
                            // 1. Exact match by code or name
                            var existingItem = await _db.Items.FirstOrDefaultAsync(x => (code != null && x.ItemCode == code) || (name != null && x.ItemName == name));
                            // 2. Fuzzy match: Contains on name (handles typos like "bột" vs "bọt")
                            if (existingItem == null && !string.IsNullOrEmpty(name))
                            {
                                var nameLower = name.ToLower();
                                var candidates = await _db.Items.Where(x => x.IsActive).ToListAsync();
                                existingItem = candidates.FirstOrDefault(x =>
                                    x.ItemName.ToLower().Contains(nameLower) ||
                                    nameLower.Contains(x.ItemName.ToLower()) ||
                                    nameLower.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                        .Where(w => w.Length >= 3)
                                        .Count(w => x.ItemName.ToLower().Contains(w)) >= 2
                                );
                            }
                            // 3. Fuzzy match by code fragments
                            if (existingItem == null && !string.IsNullOrEmpty(code))
                            {
                                var codeLower = code.ToLower();
                                existingItem = await _db.Items.FirstOrDefaultAsync(x => x.ItemCode.ToLower().Contains(codeLower) || codeLower.Contains(x.ItemCode.ToLower()));
                            }
                            // 4. Không tự tạo mới master data từ chứng từ đọc máy.
                            if (existingItem == null)
                            {
                                warnings.Add($"Chưa khớp vật tư: {searchName ?? code ?? "dòng không tên"}. Vui lòng chọn thủ công.");
                                continue;
                            }
                            mappedItems.Add(new { ItemId = existingItem.ItemId, ItemCode = existingItem.ItemCode, ItemName = existingItem.ItemName, Quantity = qty, UnitPrice = price, BaseUomId = existingItem.BaseUomId, IsNew = false, IsMatched = true, RequiresReview = false });
                        }
                    }
                }
            }
            catch (Exception mapEx)
            {
                _logger.LogWarning(mapEx, "[Đọc chứng từ dự phòng] Mapping error");
                warnings.Add("Không map được một phần dữ liệu từ chứng từ dự phòng.");
            }

            var parseStatus = mappedItems.Count == 0
                ? "Failed"
                : warnings.Count == 0 ? "Success" : "Partial";
            var confidence = mappedItems.Count == 0
                ? 0m
                : warnings.Count == 0 ? 0.75m : 0.5m;

            return Ok(new
            {
                data = System.Text.Json.JsonSerializer.Serialize(mappedItems),
                rawText = textResult,
                provider = providerUsed,
                logId = ocrLog.AiOcrLogId,
                warnings,
                confidence,
                parseStatus
            });
        }
        catch (Exception ex)
        {
            // P1-R2-1: log chi tiết server-side, không leak lỗi hệ thống ra client.
            _logger.LogError(ex, "Fallback document parser failed");
            return BadRequest("Lỗi đọc chứng từ dự phòng. Vui lòng kiểm tra file và thử lại.");
        }
    }


    /// <summary>
    /// Helper: gọi Gemini API để đọc chứng từ dự phòng khi Groq không khả dụng.
    /// </summary>
    private async Task<(string textResult, string provider)> CallGeminiOcr(HttpClient client, string apiKey, string prompt, string mimeType, string base64Image)
    {
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new { inlineData = new { mimeType = mimeType, data = base64Image } }
                    }
                }
            }
        };

        var body = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}", body);
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var code = (int)response.StatusCode;
            var msg = code == 429
                ? "Cả Groq và Gemini đều hết quota. Vui lòng thử lại sau 1 phút."
                : "Lỗi dịch vụ đọc chứng từ dự phòng. Vui lòng thử lại sau.";
            _logger.LogError("[Đọc chứng từ dự phòng] Gemini Error ({StatusCode}): {Response}", code, responseString);
            throw new BusinessRuleException(msg, code: "DOCUMENT_READ_ERROR", entityName: "AI");
        }

        var jsonDoc = JsonDocument.Parse(responseString);
        var text = jsonDoc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "[]";
        _logger.LogInformation("[Đọc chứng từ dự phòng] Gemini OK (fallback)");
        return (text, "Gemini");
    }

    // Legacy storage contract: Path.Combine("App_Data", "uploads", "document-intake-legacy", fileName).
    private async Task<string> StoreLegacyReceiptDocumentAsync(string originalFileName, byte[] imageBytes, CancellationToken cancellationToken)
        => await _voucherImportQueryService.StoreLegacyReceiptDocumentAsync(originalFileName, imageBytes, cancellationToken);

    private static string NormalizePrivateFileStem(string? value)
    {
        var source = string.IsNullOrWhiteSpace(value) ? "document" : value.Trim();
        var chars = source
            .Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-')
            .ToArray();
        var normalized = new string(chars).Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "document" : normalized[..Math.Min(normalized.Length, 80)];
    }

    private string ResolvePrivateReceiptPath(string storedPath)
        => _voucherImportQueryService.ResolvePrivateReceiptPath(storedPath);

    private string ResolveContentType(string physicalPath, string? storedContentType = null)
        => _voucherImportQueryService.ResolveContentType(physicalPath, storedContentType);

    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> DownloadReceiptDocument(long logId)
    {
        var log = await _db.Set<AiOcrLog>()
            .Include(x => x.Voucher)
            .FirstOrDefaultAsync(x => x.AiOcrLogId == logId);
        if (log == null)
            return NotFound();

        var scopedWarehouseId = GetScopedWarehouseId();
        if (log.Voucher != null && scopedWarehouseId.HasValue && log.Voucher.WarehouseId != scopedWarehouseId.Value)
            return Forbid();

        var currentUser = User.Identity?.Name ?? "";
        if (log.Voucher == null
            && !User.IsInRole("Admin")
            && !User.IsInRole("Manager")
            && !string.Equals(log.CreatedBy, currentUser, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        string physicalPath;
        try
        {
            physicalPath = ResolvePrivateReceiptPath(log.ImageUrl);
        }
        catch (Exception ex) when (ex is FileNotFoundException or UnauthorizedAccessException or ArgumentException)
        {
            _logger.LogWarning(ex, "Đường dẫn chứng từ không hợp lệ cho log {LogId}.", logId);
            return NotFound();
        }

        if (!System.IO.File.Exists(physicalPath))
            return NotFound();

        var downloadName = string.IsNullOrWhiteSpace(log.FileName)
            ? Path.GetFileName(physicalPath)
            : Path.GetFileName(log.FileName);
        return PhysicalFile(physicalPath, ResolveContentType(physicalPath), downloadName);
    }


    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> DownloadSampleImport100()
    {
        // Generate a 100-row sample file using current master data where available.
        var items = await _db.Items
            .Include(i => i.BaseUom)
            .Where(i => i.IsActive)
            .OrderBy(i => i.ItemCode)
            .Take(300)
            .ToListAsync();

        var locations = await _db.Locations
            .Where(l => l.IsActive)
            .OrderBy(l => l.LocationCode)
            .Take(300)
            .Select(l => l.LocationCode)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("ImportLines");

        // Header
        ws.Cell(1, 1).Value = "ItemCode";
        ws.Cell(1, 2).Value = "ItemName";
        ws.Cell(1, 3).Value = "Quantity";
        ws.Cell(1, 4).Value = "UnitPrice";
        ws.Cell(1, 5).Value = "UnitName";
        ws.Cell(1, 6).Value = "LocationCode";
        ws.Cell(1, 7).Value = "ExpiryDate (yyyy-MM-dd)";
        ws.Cell(1, 8).Value = "LotNumber";
        ws.Cell(1, 9).Value = "DefectQty";
        ws.Cell(1, 10).Value = "Notes";

        ws.Range(1, 1, 1, 10).Style.Font.Bold = true;
        ws.Range(1, 1, 1, 10).Style.Fill.BackgroundColor = XLColor.FromHtml("#111827");
        ws.Range(1, 1, 1, 10).Style.Font.FontColor = XLColor.White;

        var rng = new Random();
        for (int i = 0; i < 100; i++)
        {
            var row = i + 2;
            var it = items.Count > 0 ? items[rng.Next(items.Count)] : null;
            var loc = locations.Count > 0 ? locations[rng.Next(locations.Count)] : "";

            var qty = rng.Next(1, 30);
            var defect = rng.NextDouble() < 0.10 ? rng.Next(1, Math.Min(3, qty + 1)) : 0; // ~10% rows have small defect
            var unitName = it?.BaseUom?.UomCode ?? "Pcs";
            var price = it != null ? it.UnitCost : 0;

            ws.Cell(row, 1).Value = it?.ItemCode ?? $"MAU-{(i + 1):D3}";
            ws.Cell(row, 2).Value = it?.ItemName ?? $"Vật tư mẫu {(i + 1):D3}";
            ws.Cell(row, 3).Value = qty;
            ws.Cell(row, 4).Value = price;
            ws.Cell(row, 5).Value = unitName;
            ws.Cell(row, 6).Value = loc;

            // Add expiry dates to some rows to show FEFO-compatible data.
            if (rng.NextDouble() < 0.35)
            {
                var days = rng.Next(5, 180);
                ws.Cell(row, 7).Value = VietnamNow.Date.AddDays(days).ToString("yyyy-MM-dd");
            }
            else
            {
                ws.Cell(row, 7).Value = "";
            }

            // Always include a LotNumber so sample import covers batch tracking.
            ws.Cell(row, 8).Value = $"LOT-{VietnamNow:yyMMdd}-{rng.Next(1000, 9999)}";
            ws.Cell(row, 9).Value = defect;
            ws.Cell(row, 10).Value = defect > 0 ? "Cần kiểm tra số lượng lỗi/thiếu" : "";
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "WMS_DanhSachVatTu_Mau_100dong.xlsx");
    }


    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportLinesExcel(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("Vui lòng chọn file Excel.");
        var canAutoCreateItem = User.IsInRole("Admin") || User.IsInRole("Manager");
        const long maxBytes = 5 * 1024 * 1024; // 5MB
        if (file.Length > maxBytes) return BadRequest("File quá lớn. Vui lòng chọn file ≤ 5MB.");

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
        if (ext != ".xlsx") return BadRequest("Định dạng không hợp lệ. Chỉ hỗ trợ .xlsx");

        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheets.FirstOrDefault();
            if (ws == null) return BadRequest("File Excel không có worksheet.");

            var defaultUomId = await _db.UnitsOfMeasure.Select(u => u.UomId).FirstOrDefaultAsync();
            var allUoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).ToListAsync();

            var locMap = await _db.Locations
                .Where(l => l.IsActive)
                .ToDictionaryAsync(l => l.LocationCode.ToLower(), l => l.LocationId);

            // P1-R2-4: validate header row trước khi loop để tránh import file sai format thành dòng rác.
            var headerCellA = ws.Cell(1, 1).GetString()?.Trim() ?? "";
            if (!headerCellA.Contains("ItemCode", StringComparison.OrdinalIgnoreCase)
                && !headerCellA.Contains("Mã", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Sai định dạng header. Cột A phải là 'ItemCode' (mã vật tư).");

            var mappedItems = new List<object>();

            var row = 2;
            while (true)
            {
                var code = ws.Cell(row, 1).GetString()?.Trim();
                var name = ws.Cell(row, 2).GetString()?.Trim();
                var qtyStr = ws.Cell(row, 3).GetString()?.Trim();
                var priceStr = ws.Cell(row, 4).GetString()?.Trim();
                var unitName = ws.Cell(row, 5).GetString()?.Trim();
                var locationCode = ws.Cell(row, 6).GetString()?.Trim();
                var expiryStr = ws.Cell(row, 7).GetString()?.Trim();
                var lotNumber = ws.Cell(row, 8).GetString()?.Trim();
                var defectStr = ws.Cell(row, 9).GetString()?.Trim();
                var notes = ws.Cell(row, 10).GetString()?.Trim();

                if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(qtyStr))
                    break;

                if (!decimal.TryParse(qtyStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var qty))
                    decimal.TryParse(qtyStr, NumberStyles.Any, new CultureInfo("vi-VN"), out qty);
                if (qty <= 0) qty = 1;

                decimal price = 0;
                if (!decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out price))
                    decimal.TryParse(priceStr, NumberStyles.Any, new CultureInfo("vi-VN"), out price);

                decimal defectQty = 0;
                if (!decimal.TryParse(defectStr, NumberStyles.Any, CultureInfo.InvariantCulture, out defectQty))
                    decimal.TryParse(defectStr, NumberStyles.Any, new CultureInfo("vi-VN"), out defectQty);
                if (defectQty < 0) defectQty = 0;

                DateTime? expiry = null;
                if (!string.IsNullOrWhiteSpace(expiryStr))
                {
                    if (DateTime.TryParseExact(expiryStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                        expiry = dt.Date;
                    else if (DateTime.TryParse(expiryStr, out var dt2))
                        expiry = dt2.Date;
                }

                int matchedUomId = defaultUomId;
                if (!string.IsNullOrEmpty(unitName))
                {
                    var unitLower = unitName.ToLower().Trim();
                    var matchedUom = allUoms.FirstOrDefault(u =>
                        u.UomCode.ToLower() == unitLower ||
                        u.UomName.ToLower() == unitLower ||
                        u.UomName.ToLower().Contains(unitLower) ||
                        unitLower.Contains(u.UomName.ToLower()) ||
                        u.UomCode.ToLower().Contains(unitLower) ||
                        unitLower.Contains(u.UomCode.ToLower())
                    );
                    if (matchedUom != null) matchedUomId = matchedUom.UomId;
                }

                var searchName = name ?? code;
                bool isNew = false;

                Item? existingItem = null;
                if (!string.IsNullOrWhiteSpace(code))
                    existingItem = await _db.Items.FirstOrDefaultAsync(x => x.ItemCode == code);
                if (existingItem == null && !string.IsNullOrWhiteSpace(name))
                    existingItem = await _db.Items.FirstOrDefaultAsync(x => x.ItemName == name);

                if (existingItem == null)
                {
                    if (!canAutoCreateItem)
                        return BadRequest($"Dòng {row}: vật tư '{searchName ?? code}' chưa tồn tại. Nhân viên không được tự tạo vật tư mới.");
                    var nCode = string.IsNullOrEmpty(code) ? $"IMP-{Guid.NewGuid().ToString()[..6]}" : code!;
                    existingItem = new Item
                    {
                        ItemCode = nCode,
                        ItemName = searchName ?? nCode,
                        ItemType = ItemTypeEnum.NguyenVatLieu,
                        BaseUomId = matchedUomId,
                        UnitCost = price,
                        IsActive = true,
                        CreatedBy = "Excel Import",
                        CreatedAt = VietnamNow
                    };
                    _db.Items.Add(existingItem);
                    await _unitOfWork.SaveChangesAsync();
                    isNew = true;
                }

                int? locationId = null;
                if (!string.IsNullOrWhiteSpace(locationCode))
                {
                    var key = locationCode.ToLower().Trim();
                    if (locMap.TryGetValue(key, out var lid)) locationId = lid;
                }

                mappedItems.Add(new
                {
                    ItemId = existingItem.ItemId,
                    ItemCode = existingItem.ItemCode,
                    ItemName = existingItem.ItemName,
                    Quantity = qty,
                    UnitPrice = price,
                    BaseUomId = existingItem.BaseUomId,
                    IsNew = isNew,
                    LocationId = locationId,
                    ExpiryDate = expiry?.ToString("yyyy-MM-dd"),
                    LotNumber = lotNumber,
                    DefectQty = defectQty,
                    Notes = notes
                });

                row++;
                if (row > 1000) break; // P1-R2-4: hạ cap 5000 → 1000 để giảm rủi ro OOM với file lớn bất thường.
            }

            return Ok(new { data = System.Text.Json.JsonSerializer.Serialize(mappedItems) });
        }
        catch (Exception ex)
        {
            // P1-R2-1: log chi tiết server-side, không leak lỗi hệ thống ra client.
            _logger?.LogError(ex, "Excel import failed");
            return BadRequest("Lỗi xử lý file Excel. Vui lòng kiểm tra định dạng và thử lại.");
        }
    }

}
