using System.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using WMS.Controllers;
using WMS.Data;
using WMS.Models;
using WMS.Services;

namespace WMS.Tests;

public sealed class ApiIntegrationScopeHardeningTests
{
    [Fact]
    public async Task GetItemsAndStock_ShouldUseScopedWarehouseAndOwnerBalances()
    {
        await using var db = CreateDb();
        SeedWarehouseOwnerFixture(db);
        await db.SaveChangesAsync();

        var controller = CreateController(db, scopedWarehouseId: 1, scopedOwnerPartnerId: 10);

        var itemsResult = await controller.GetItems(search: null, categoryId: null, active: null);
        var itemRows = GetDataRows(Assert.IsType<OkObjectResult>(itemsResult).Value!);
        var item = Assert.Single(itemRows);
        Assert.Equal(5m, GetDecimal(item, "CurrentStock"));

        var stockResult = await controller.GetStock(warehouseId: null, itemId: 1);
        var stockRows = GetDataRows(Assert.IsType<OkObjectResult>(stockResult).Value!);
        var stock = Assert.Single(stockRows);
        Assert.Equal(5m, GetDecimal(stock, "Quantity"));
        Assert.Equal(1, GetInt(stock, "WarehouseId"));
    }

    [Fact]
    public async Task DirectIdReadExportAndMutation_ShouldReturnSafeEnvelopeOutsideApiScope()
    {
        await using var db = CreateDb();
        SeedWarehouseOwnerFixture(db);
        db.Vouchers.Add(new Voucher
        {
            VoucherId = 9001,
            VoucherCode = "PX-OUT-OF-SCOPE",
            VoucherType = VoucherTypeEnum.XuatKho,
            WarehouseId = 2,
            OwnerPartnerId = 20,
            VoucherDate = DateTime.Today,
            CreatedBy = "test"
        });
        db.EdiMessages.Add(new EdiMessage
        {
            EdiMessageId = 9002,
            MessageType = EdiMessageTypeEnum.Order940,
            Direction = EdiDirectionEnum.Outbound,
            Status = EdiMessageStatusEnum.Validated,
            WarehouseId = 2,
            PartnerId = 20,
            ControlNumber = "EDI-OUT-OF-SCOPE",
            Payload = "ISA*00*~ST*940*0001~SE*2*0001~"
        });
        db.CarrierShipments.Add(new CarrierShipment
        {
            CarrierShipmentId = 9003,
            CarrierConnectorId = 1,
            WarehouseId = 2,
            OwnerPartnerId = 20,
            VoucherId = 9001,
            OutboundPackageId = 1,
            Status = CarrierShipmentStatusEnum.Pending,
            CarrierCodeSnapshot = "MOCK",
            CarrierNameSnapshot = "Mock Carrier",
            IdempotencyKey = "scope-carrier-9003",
            CorrelationId = "scope-carrier-9003"
        });
        db.ThreePlInvoices.Add(new ThreePlInvoice
        {
            ThreePlInvoiceId = 9004,
            InvoiceCode = "3PL-OUT-OF-SCOPE",
            WarehouseId = 2,
            OwnerPartnerId = 20,
            PeriodFrom = DateTime.Today.AddDays(-1),
            PeriodTo = DateTime.Today,
            ApiPublicId = "api-9004",
            CreatedBy = "test"
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db, scopedWarehouseId: 1, scopedOwnerPartnerId: 10);

        AssertForbiddenScope(await controller.GetVoucherDetail(9001));
        AssertForbiddenScope(await controller.ExportEdi(9002));
        AssertForbiddenScope(await controller.ConfirmShipment(9003));
        AssertForbiddenScope(await controller.IssueThreePlInvoice(9004));
    }

    [Fact]
    public async Task ImportEdi_ShouldDefaultToApiScopeAndRejectExplicitForeignScope()
    {
        await using var db = CreateDb();
        SeedWarehouseOwnerFixture(db);
        await db.SaveChangesAsync();
        var controller = CreateController(db, scopedWarehouseId: 1, scopedOwnerPartnerId: 10);

        var accepted = await controller.ImportEdi(new ApiEdiImportRequest
        {
            MessageType = EdiMessageTypeEnum.Order940,
            Payload = "ISA*00*~ST*940*0001~SE*2*0001~",
            FileName = "order-940.edi"
        });

        Assert.IsType<ObjectResult>(accepted);
        var message = Assert.Single(await db.EdiMessages.ToListAsync());
        Assert.Equal(1, message.WarehouseId);
        Assert.Equal(10, message.PartnerId);

        var rejected = await controller.ImportEdi(new ApiEdiImportRequest
        {
            MessageType = EdiMessageTypeEnum.Order940,
            Payload = "ISA*00*~ST*940*0002~SE*2*0002~",
            WarehouseId = 2,
            PartnerId = 10
        });

        AssertForbiddenScope(rejected);
    }

    private static ApiIntegrationController CreateController(AppDbContext db, int? scopedWarehouseId, int? scopedOwnerPartnerId)
    {
        const string apiKey = "unit-test-api-key";
        var values = new Dictionary<string, string?>
        {
            ["Api:Key"] = apiKey
        };
        if (scopedWarehouseId.HasValue)
            values["Api:ScopedWarehouseId"] = scopedWarehouseId.Value.ToString();
        if (scopedOwnerPartnerId.HasValue)
            values["Api:ScopedOwnerPartnerId"] = scopedOwnerPartnerId.Value.ToString();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var unitOfWork = new EfUnitOfWork(db);
        var integrationService = new NullIntegrationService();
        var controller = new ApiIntegrationController(
            db,
            configuration,
            new InventoryBalanceService(db),
            new MheIntegrationService(db, integrationService, configuration, unitOfWork),
            new CarrierIntegrationService(db, integrationService, unitOfWork),
            new EnterpriseIntegrationService(db, unitOfWork));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-API-Key"] = apiKey;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static void SeedWarehouseOwnerFixture(AppDbContext db)
    {
        db.UnitsOfMeasure.Add(new UnitOfMeasure { UomId = 1, UomCode = "EA", UomName = "Each" });
        db.Warehouses.AddRange(
            new Warehouse { WarehouseId = 1, WarehouseCode = "WH1", WarehouseName = "Warehouse 1" },
            new Warehouse { WarehouseId = 2, WarehouseCode = "WH2", WarehouseName = "Warehouse 2" });
        db.Zones.AddRange(
            new Zone { ZoneId = 1, WarehouseId = 1, ZoneCode = "Z1", ZoneName = "Zone 1" },
            new Zone { ZoneId = 2, WarehouseId = 2, ZoneCode = "Z2", ZoneName = "Zone 2" });
        db.Locations.AddRange(
            new Location { LocationId = 1, ZoneId = 1, LocationCode = "WH1-A1" },
            new Location { LocationId = 2, ZoneId = 2, LocationCode = "WH2-A1" });
        db.Partners.AddRange(
            new Partner { PartnerId = 10, PartnerCode = "OWN-A", PartnerName = "Owner A", PartnerType = PartnerTypeEnum.Customer, IsActive = true },
            new Partner { PartnerId = 20, PartnerCode = "OWN-B", PartnerName = "Owner B", PartnerType = PartnerTypeEnum.Customer, IsActive = true });
        db.Items.Add(new Item
        {
            ItemId = 1,
            ItemCode = "ITEM-SCOPE",
            ItemName = "Scoped Item",
            BaseUomId = 1,
            OwnerPartnerId = 10,
            IsActive = true
        });
        db.ItemLocations.AddRange(
            new ItemLocation { ItemLocationId = 1, ItemId = 1, LocationId = 1, OwnerPartnerId = 10, Quantity = 5, LotNumber = "LOT-A" },
            new ItemLocation { ItemLocationId = 2, ItemId = 1, LocationId = 1, OwnerPartnerId = 20, Quantity = 7, LotNumber = "LOT-A" },
            new ItemLocation { ItemLocationId = 3, ItemId = 1, LocationId = 1, OwnerPartnerId = null, Quantity = 11, LotNumber = "LOT-A" },
            new ItemLocation { ItemLocationId = 4, ItemId = 1, LocationId = 2, OwnerPartnerId = 10, Quantity = 13, LotNumber = "LOT-A" });
    }

    private static IReadOnlyList<object> GetDataRows(object envelope)
    {
        var data = GetAnonValue(envelope, "data");
        var rows = Assert.IsAssignableFrom<IEnumerable>(data);
        return rows.Cast<object>().ToList();
    }

    private static object? GetAnonValue(object? source, string propertyName)
        => source?.GetType().GetProperty(propertyName)?.GetValue(source);

    private static decimal GetDecimal(object source, string propertyName)
        => Assert.IsType<decimal>(GetAnonValue(source, propertyName));

    private static int GetInt(object source, string propertyName)
        => Assert.IsType<int>(GetAnonValue(source, propertyName));

    private static void AssertForbiddenScope(IActionResult result)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        Assert.Equal("API_SCOPE_FORBIDDEN", GetAnonValue(objectResult.Value, "code"));
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
