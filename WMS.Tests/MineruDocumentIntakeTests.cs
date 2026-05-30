using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using WMS.Common;
using WMS.Controllers;
using WMS.Data;
using WMS.Models;
using WMS.Services;

namespace WMS.Tests;

public class MineruDocumentIntakeTests
{
    [Fact]
    public async Task MineruClient_ShouldParseFileParseResponse()
    {
        var handler = new StubHttpHandler(async request =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri?.AbsolutePath == "/health")
                return JsonResponse("""{"status":"healthy"}""");

            if (request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath == "/file_parse")
            {
                var multipart = Assert.IsType<MultipartFormDataContent>(request.Content);
                Assert.Contains(multipart, part => string.Equals(
                    part.Headers.ContentDisposition?.Name?.Trim('"'),
                    "files",
                    StringComparison.Ordinal));
                return JsonResponse("""
                {
                  "version": "test-1",
                  "task_id": "task-42",
                  "results": {
                    "receipt": {
                      "md_content": "| Mã vật tư | Tên vật tư | Số lượng |\n| --- | --- | --- |\n| SKU-1 | Bột giặt | 2 |",
                      "content_list": [
                        { "type": "table", "table_body": "<table><tr><th>Mã vật tư</th><th>Số lượng</th></tr><tr><td>SKU-1</td><td>2</td></tr></table>" }
                      ]
                    }
                  }
                }
                """);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var client = new MineruDocumentParserClient(
            new StubHttpClientFactory(handler),
            Options.Create(new MinerUOptions { Enabled = true, BaseUrl = "http://mineru.local", TimeoutSeconds = 30 }));

        var result = await client.ParseAsync(Upload("receipt.pdf", "fake-pdf"));

        Assert.True(result.Success);
        Assert.Equal("Success", result.ParseStatus);
        Assert.Equal("test-1", result.Version);
        Assert.Equal("task-42", result.TaskId);
        Assert.Contains("SKU-1", result.RawText);
        Assert.Contains("table_body", result.ContentListJson);
    }

    [Fact]
    public async Task DocumentIntake_ShouldMapExactCodeBarcodeAndName()
    {
        await using var db = CreateDb(nameof(DocumentIntake_ShouldMapExactCodeBarcodeAndName));
        SeedMasterData(db);
        db.Items.AddRange(
            new Item { ItemId = 1, ItemCode = "MAT-001", ItemName = "Vật tư theo mã", BaseUomId = 1, IsActive = true },
            new Item { ItemId = 2, ItemCode = "MAT-002", ItemName = "Vật tư theo barcode", Barcode = "BAR-002", BaseUomId = 1, IsActive = true },
            new Item { ItemId = 3, ItemCode = "MAT-003", ItemName = "Tên chính xác", BaseUomId = 1, IsActive = true });
        await db.SaveChangesAsync();

        var service = CreateIntakeService(db, MarkdownResult("""
        | Mã vật tư | Tên vật tư | Số lượng | ĐVT | Đơn giá | Số lô | NSX | HSD |
        | --- | --- | --- | --- | --- | --- | --- | --- |
        | MAT-001 | Vật tư theo mã | 2 | Cái | 1000 | L001 | 01/05/2026 | 31/12/2026 |
        | BAR-002 | Vật tư theo barcode | 4 | Cái | 2000 | L002 | 02/05/2026 | 30/12/2026 |
        |  | Tên chính xác | 3 | Cái | 3000 | L003 | 03/05/2026 | 29/12/2026 |
        """));

        var result = await service.AnalyzeAsync(Upload("receipt.pdf", "payload"), "staff.user");

        Assert.Equal("Success", result.ParseStatus);
        Assert.Equal(1m, result.Confidence);
        Assert.Equal(new int?[] { 1, 2, 3 }, result.Lines.Select(line => line.ItemId).ToArray());
        Assert.All(result.Lines, line =>
        {
            Assert.True(line.IsMatched);
            Assert.False(line.RequiresReview);
        });
        Assert.Equal(3, await db.Items.CountAsync());
        CleanupStoredDocument(db);
    }

    [Fact]
    public async Task DocumentIntake_ShouldWarnUnknownItemAndNeverCreateMasterData()
    {
        await using var db = CreateDb(nameof(DocumentIntake_ShouldWarnUnknownItemAndNeverCreateMasterData));
        SeedMasterData(db);
        db.Items.Add(new Item { ItemId = 1, ItemCode = "KNOWN", ItemName = "Hàng đã có", BaseUomId = 1, IsActive = true });
        await db.SaveChangesAsync();

        var service = CreateIntakeService(db, MarkdownResult("""
        | Mã vật tư | Tên vật tư | Số lượng |
        | --- | --- | --- |
        | NEW-999 | Hàng chưa có master | 5 |
        """));

        var result = await service.AnalyzeAsync(Upload("unknown.docx", "payload"), "manager.user");

        var line = Assert.Single(result.Lines);
        Assert.Null(line.ItemId);
        Assert.True(line.RequiresReview);
        Assert.Equal("Unmatched", line.MatchKind);
        Assert.Contains(result.Warnings, warning => warning.Contains("chưa khớp vật tư", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, await db.Items.CountAsync());
        CleanupStoredDocument(db);
    }

    [Fact]
    public async Task DocumentIntake_ShouldFlagFuzzySuggestionForManualReview()
    {
        await using var db = CreateDb(nameof(DocumentIntake_ShouldFlagFuzzySuggestionForManualReview));
        SeedMasterData(db);
        db.Items.Add(new Item { ItemId = 10, ItemCode = "OMO-001", ItemName = "Bột giặt OMO", BaseUomId = 1, IsActive = true });
        await db.SaveChangesAsync();

        var service = CreateIntakeService(db, MarkdownResult("""
        | Tên vật tư | Số lượng |
        | --- | --- |
        | Bột giặt OMO đậm đặc | 2 |
        """));

        var result = await service.AnalyzeAsync(Upload("fuzzy.png", "payload", "image/png"), "staff.user");

        var line = Assert.Single(result.Lines);
        Assert.Null(line.ItemId);
        Assert.Equal(10, line.SuggestedItemId);
        Assert.True(line.RequiresReview);
        Assert.Equal("FuzzySuggestion", line.MatchKind);
        Assert.Contains(line.Warnings, warning => warning.Contains("cần chọn thủ công", StringComparison.OrdinalIgnoreCase));
        CleanupStoredDocument(db);
    }

    [Fact]
    public async Task DocumentIntake_ShouldRejectUnsafeExtension()
    {
        await using var db = CreateDb(nameof(DocumentIntake_ShouldRejectUnsafeExtension));
        SeedMasterData(db);
        await db.SaveChangesAsync();
        var service = CreateIntakeService(db, MarkdownResult(""));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.AnalyzeAsync(Upload("malware.exe", "payload"), "staff.user"));

        Assert.Equal("DOCUMENT_FILE_TYPE_INVALID", ex.Code);
        Assert.Empty(await db.AiOcrLogs.ToListAsync());
    }

    [Fact]
    public async Task AnalyzeReceipt_ShouldUseDocumentIntakeServiceAndReturnCompatibleJson()
    {
        await using var db = CreateDb(nameof(AnalyzeReceipt_ShouldUseDocumentIntakeServiceAndReturnCompatibleJson));
        var intake = new RecordingIntakeService(new VoucherDocumentIntakeResult
        {
            Provider = "MinerU",
            ParseStatus = "Success",
            RawText = "markdown",
            LogId = 99,
            Confidence = 1m,
            Lines =
            {
                new MappedDocumentLine { ItemId = 1, ItemCode = "MAT-1", ItemName = "Vật tư", Quantity = 2, IsMatched = true }
            }
        });
        var controller = CreateController(db, intake: intake);

        var action = await controller.AnalyzeReceipt(Upload("receipt.pdf", "payload"));

        Assert.True(intake.Called);
        var ok = Assert.IsType<OkObjectResult>(action);
        Assert.Equal("MinerU", GetAnonValue(ok.Value!, "provider"));
        Assert.Equal("Success", GetAnonValue(ok.Value!, "parseStatus"));
        Assert.Equal(99L, GetAnonValue(ok.Value!, "logId"));
        var data = Assert.IsType<string>(GetAnonValue(ok.Value!, "data"));
        Assert.Contains("MAT-1", data);
    }

    [Fact]
    public async Task AnalyzeReceipt_ShouldNotUseLegacyFallbackUnlessEnabled()
    {
        await using var db = CreateDb(nameof(AnalyzeReceipt_ShouldNotUseLegacyFallbackUnlessEnabled));
        var controller = CreateController(db, intake: null);

        var action = await controller.AnalyzeReceipt(Upload("receipt.jpg", "payload", "image/jpeg"));

        var badRequest = Assert.IsType<BadRequestObjectResult>(action);
        Assert.Contains("MinerU", Assert.IsType<string>(badRequest.Value));
    }

    [Fact]
    public async Task CreateVoucherUi_ShouldUseVietnameseDocumentIntakeWording()
    {
        var view = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Views", "Vouchers", "Create.cshtml"));

        Assert.Contains("Đọc chứng từ bằng AI", view);
        Assert.Contains(".pdf,.jpg,.jpeg,.png,.webp,.docx,.pptx,.xlsx", view);
        Assert.Contains("Tải danh sách từ Excel", view);
        Assert.Contains("ImportLinesExcel", view);
        Assert.DoesNotContain("Đọc hóa đơn", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AI OCR", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bill", view, StringComparison.OrdinalIgnoreCase);
    }

    private static VoucherDocumentIntakeService CreateIntakeService(AppDbContext db, MinerUDocumentParseResult parseResult)
        => new(
            db,
            new EfUnitOfWork(db),
            new StaticMineruParserClient(parseResult),
            Options.Create(new MinerUOptions { Enabled = true, MaxFileSizeMb = 20 }));

    private static MinerUDocumentParseResult MarkdownResult(string markdown)
        => new()
        {
            Success = true,
            ParseStatus = "Success",
            RawText = markdown,
            Provider = "MinerU"
        };

    private static AppDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("mineru-" + name + "-" + Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static void SeedMasterData(AppDbContext db)
    {
        db.UnitsOfMeasure.Add(new UnitOfMeasure { UomId = 1, UomCode = "EA", UomName = "Cái", IsActive = true });
    }

    private static IFormFile Upload(string fileName, string content, string contentType = "application/pdf")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static VouchersController CreateController(AppDbContext db, IVoucherDocumentIntakeService? intake)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MinerU:AllowLegacyFallback"] = "false"
            })
            .Build();
        var unitOfWork = new EfUnitOfWork(db);
        var reservationService = new InventoryReservationService(db);
        var balanceService = new InventoryBalanceService(db);
        var inboundService = new InboundExecutionService(db, unitOfWork, balanceService);
        var outboundService = new OutboundExecutionService(db, unitOfWork, reservationService, balanceService);
        var cancellationService = new VoucherCancellationService(db, unitOfWork, reservationService, balanceService);
        var orderStreamingService = new OrderStreamingService(db, unitOfWork, reservationService);
        var integrationService = new NullIntegrationService();
        var documentIntakeService = intake ?? new FailingVoucherDocumentIntakeService();
        var controller = new VouchersController(
            db,
            configuration,
            new StubHttpClientFactory(new StubHttpHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)))),
            integrationService,
            reservationService,
            unitOfWork,
            outboundService,
            inboundService,
            balanceService,
            cancellationService,
            orderStreamingService,
            new SerialInventoryService(db),
            new InventoryTransactionService(db),
            new CatchWeightService(db),
            new ShipmentLoadService(db, unitOfWork),
            new CarrierIntegrationService(db, integrationService, unitOfWork),
            documentIntakeService,
            new VoucherSharedRuleService(db),
            new VoucherImportQueryService(),
            new VoucherCreateWorkflowService(db),
            new VoucherDetailQueryService());

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "staff.user"),
            new Claim(ClaimTypes.Role, "Staff")
        }, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }

    private static object? GetAnonValue(object source, string propertyName)
        => source.GetType()
            .GetProperties()
            .First(property => string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            .GetValue(source);

    private static HttpResponseMessage JsonResponse(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private static void CleanupStoredDocument(AppDbContext db)
    {
        foreach (var relativePath in db.AiOcrLogs.Select(log => log.ImageUrl).Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath!.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(physicalPath))
                File.Delete(physicalPath);
        }
    }

    private sealed class StaticMineruParserClient(MinerUDocumentParseResult result) : IMineruDocumentParserClient
    {
        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<MinerUDocumentParseResult> ParseAsync(IFormFile file, CancellationToken cancellationToken = default)
            => Task.FromResult(result);
    }

    private sealed class FailingVoucherDocumentIntakeService : IVoucherDocumentIntakeService
    {
        public Task<VoucherDocumentIntakeResult> AnalyzeAsync(IFormFile file, string actor, CancellationToken cancellationToken = default)
            => throw new BusinessRuleException("Dịch vụ đọc chứng từ MinerU chưa sẵn sàng.", "MINERU_UNAVAILABLE", nameof(AiOcrLog));
    }

    private sealed class RecordingIntakeService(VoucherDocumentIntakeResult result) : IVoucherDocumentIntakeService
    {
        public bool Called { get; private set; }

        public Task<VoucherDocumentIntakeResult> AnalyzeAsync(IFormFile file, string actor, CancellationToken cancellationToken = default)
        {
            Called = true;
            Assert.Equal("staff.user", actor);
            return Task.FromResult(result);
        }
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(handler, disposeHandler: false);
    }

    private sealed class StubHttpHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => respond(request);
    }

    private sealed class NullIntegrationService : IIntegrationService
    {
        public Task EnqueueAsync(OutboxEventTypeEnum eventType, string targetEndpoint, object payload, string? idempotencyKey = null, string? targetSystem = null)
            => Task.CompletedTask;

        public Task<(bool IsDuplicate, string? CachedResponse, int StatusCode)> CheckIdempotencyAsync(string keyValue, string operationType)
            => Task.FromResult((false, (string?)null, 0));

        public Task SetIdempotencyAsync(string keyValue, string operationType, string response, int statusCode)
            => Task.CompletedTask;

        public Task ProcessOutboxBatchAsync(CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
