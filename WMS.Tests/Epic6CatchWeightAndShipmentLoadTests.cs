using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WMS.Common;
using WMS.Data;
using WMS.Models;
using WMS.Services;

namespace WMS.Tests;

public class Epic6CatchWeightAndShipmentLoadTests
{
    [Fact]
    public async Task RequireInboundCatchWeightAsync_ShouldBlockTrackedItemWithoutCapturedWeight()
    {
        await using var db = CreateDb(nameof(RequireInboundCatchWeightAsync_ShouldBlockTrackedItemWithoutCapturedWeight));
        SeedMasterData(db);
        SeedInboundVoucher(db);
        await db.SaveChangesAsync();

        var service = new CatchWeightService(db);
        var voucher = await db.Vouchers.Include(v => v.Details).ThenInclude(d => d.Item).SingleAsync(v => v.VoucherId == 100);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.RequireInboundCatchWeightAsync(voucher));
    }

    [Fact]
    public async Task CaptureAsync_ShouldEnforceToleranceAndIdempotency()
    {
        await using var db = CreateDb(nameof(CaptureAsync_ShouldEnforceToleranceAndIdempotency));
        SeedMasterData(db);
        SeedInboundVoucher(db);
        await db.SaveChangesAsync();

        var service = new CatchWeightService(db);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CaptureAsync(new CatchWeightCaptureRequest
        {
            ItemId = 1,
            WarehouseId = 1,
            VoucherId = 100,
            VoucherDetailId = 1000,
            BaseQuantity = 10,
            ActualWeight = 30,
            WeightUomId = 2,
            CapturePoint = CatchWeightCapturePointEnum.Receive,
            CapturedBy = "qa",
            IdempotencyKey = "cw-outside-tolerance"
        }));

        var entry = await service.CaptureAsync(new CatchWeightCaptureRequest
        {
            ItemId = 1,
            WarehouseId = 1,
            VoucherId = 100,
            VoucherDetailId = 1000,
            BaseQuantity = 10,
            ActualWeight = 20,
            WeightUomId = 2,
            CapturePoint = CatchWeightCapturePointEnum.Receive,
            CapturedBy = "qa",
            IdempotencyKey = "cw-inbound-1000"
        });
        await db.SaveChangesAsync();

        var retry = await service.CaptureAsync(new CatchWeightCaptureRequest
        {
            ItemId = 1,
            WarehouseId = 1,
            VoucherId = 100,
            VoucherDetailId = 1000,
            BaseQuantity = 10,
            ActualWeight = 20,
            WeightUomId = 2,
            CapturePoint = CatchWeightCapturePointEnum.Receive,
            CapturedBy = "qa",
            IdempotencyKey = "cw-inbound-1000"
        });
        await db.SaveChangesAsync();

        Assert.Equal(entry.CatchWeightEntryId, retry.CatchWeightEntryId);
        Assert.Equal(1, await db.CatchWeightEntries.CountAsync(e => e.IdempotencyKey == "cw-inbound-1000"));
    }

    [Fact]
    public async Task RequirePackageCatchWeightAsync_ShouldBlockPackageWithoutActualWeight()
    {
        await using var db = CreateDb(nameof(RequirePackageCatchWeightAsync_ShouldBlockPackageWithoutActualWeight));
        SeedMasterData(db);
        SeedPackedOutboundVoucher(db);
        await db.SaveChangesAsync();

        var service = new CatchWeightService(db);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.RequirePackageCatchWeightAsync(200));

        await service.CaptureAsync(new CatchWeightCaptureRequest
        {
            ItemId = 1,
            WarehouseId = 1,
            VoucherId = 200,
            VoucherDetailId = 2000,
            OutboundPackageId = 900,
            BaseQuantity = 5,
            ActualWeight = 10,
            WeightUomId = 2,
            CapturePoint = CatchWeightCapturePointEnum.Pack,
            CapturedBy = "packer",
            IdempotencyKey = "cw-package-900"
        });
        await db.SaveChangesAsync();

        await service.RequirePackageCatchWeightAsync(200);
        var package = await db.OutboundPackages.SingleAsync(p => p.OutboundPackageId == 900);
        Assert.Equal(10m, package.ActualCatchWeight);
    }

    [Fact]
    public async Task ShipmentLoadDepartAsync_ShouldRequirePackageScanAndCatchWeightBeforeDeparture()
    {
        await using var db = CreateDb(nameof(ShipmentLoadDepartAsync_ShouldRequirePackageScanAndCatchWeightBeforeDeparture));
        SeedMasterData(db);
        SeedPackedOutboundVoucher(db);
        await db.SaveChangesAsync();

        var catchWeightService = new CatchWeightService(db);
        var loadService = new ShipmentLoadService(db, new EfUnitOfWork(db), catchWeightService);
        var load = await loadService.CreateAsync(new ShipmentLoadCreateRequest
        {
            WarehouseId = 1,
            LoadCode = "LOAD-EPIC6",
            CarrierName = "Carrier A",
            RouteCode = "R1"
        }, scopedWarehouseId: 1, actor: "planner");
        await loadService.AddVoucherAsync(load.ShipmentLoadId, 200, stopNumber: 1, scopedWarehouseId: 1, actor: "planner");

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            loadService.DepartAsync(load.ShipmentLoadId, "TRK-1", "MAN-1", null, scopedWarehouseId: 1, actor: "shipper"));

        await loadService.AddPackageByScanAsync(load.ShipmentLoadId, "PKG-200", scopedWarehouseId: 1, actor: "loader");

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            loadService.DepartAsync(load.ShipmentLoadId, "TRK-1", "MAN-1", null, scopedWarehouseId: 1, actor: "shipper"));

        await catchWeightService.CaptureAsync(new CatchWeightCaptureRequest
        {
            ItemId = 1,
            WarehouseId = 1,
            VoucherId = 200,
            VoucherDetailId = 2000,
            OutboundPackageId = 900,
            BaseQuantity = 5,
            ActualWeight = 10,
            WeightUomId = 2,
            CapturePoint = CatchWeightCapturePointEnum.Pack,
            CapturedBy = "packer",
            IdempotencyKey = "cw-load-package-900"
        });
        await db.SaveChangesAsync();

        await loadService.DepartAsync(load.ShipmentLoadId, "TRK-1", "MAN-1", null, scopedWarehouseId: 1, actor: "shipper");

        var voucher = await db.Vouchers.SingleAsync(v => v.VoucherId == 200);
        var package = await db.OutboundPackages.SingleAsync(p => p.OutboundPackageId == 900);
        Assert.Equal(FulfillmentStatusEnum.Shipped, voucher.FulfillmentStatus);
        Assert.NotNull(voucher.ShippedAt);
        Assert.Equal(load.ShipmentLoadId, package.ShipmentLoadId);
        Assert.Equal(1, await db.ShippingHandoverLogs.CountAsync(h => h.VoucherId == 200 && h.ShipmentLoadId == load.ShipmentLoadId));
    }

    [Fact]
    public async Task ShipmentLoadAddVoucherAsync_ShouldBlockDuplicateActiveLoadAssignment()
    {
        await using var db = CreateDb(nameof(ShipmentLoadAddVoucherAsync_ShouldBlockDuplicateActiveLoadAssignment));
        SeedMasterData(db);
        SeedPackedOutboundVoucher(db);
        await db.SaveChangesAsync();

        var service = new ShipmentLoadService(db, new EfUnitOfWork(db), new CatchWeightService(db));
        var firstLoad = await service.CreateAsync(new ShipmentLoadCreateRequest
        {
            WarehouseId = 1,
            LoadCode = "LOAD-A"
        }, scopedWarehouseId: 1, actor: "planner");
        var secondLoad = await service.CreateAsync(new ShipmentLoadCreateRequest
        {
            WarehouseId = 1,
            LoadCode = "LOAD-B"
        }, scopedWarehouseId: 1, actor: "planner");

        await service.AddVoucherAsync(firstLoad.ShipmentLoadId, 200, stopNumber: 1, scopedWarehouseId: 1, actor: "planner");

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.AddVoucherAsync(secondLoad.ShipmentLoadId, 200, stopNumber: 1, scopedWarehouseId: 1, actor: "planner"));
    }

    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static void SeedMasterData(AppDbContext db)
    {
        db.UnitsOfMeasure.AddRange(
            new UnitOfMeasure { UomId = 1, UomCode = "EA", UomName = "Each", IsActive = true },
            new UnitOfMeasure { UomId = 2, UomCode = "KG", UomName = "Kilogram", IsActive = true });
        db.Warehouses.Add(new Warehouse
        {
            WarehouseId = 1,
            WarehouseCode = "WH1",
            WarehouseName = "Main Warehouse",
            IsActive = true
        });
        db.Items.Add(new Item
        {
            ItemId = 1,
            ItemCode = "CW-SKU",
            ItemName = "Catch Weight SKU",
            BaseUomId = 1,
            TrackCatchWeight = true,
            CatchWeightUomId = 2,
            NominalWeightPerBaseUnit = 2m,
            CatchWeightTolerancePercent = 10m,
            RequireCatchWeightAtReceive = true,
            RequireCatchWeightAtPickPack = true,
            UnitCost = 1m,
            IsActive = true
        });
    }

    private static void SeedInboundVoucher(AppDbContext db)
    {
        db.Vouchers.Add(new Voucher
        {
            VoucherId = 100,
            VoucherCode = "IN-CW",
            VoucherType = VoucherTypeEnum.NhapKho,
            WarehouseId = 1,
            InboundStatus = InboundStatusEnum.Receiving,
            CreatedBy = "qa"
        });
        db.VoucherDetails.Add(new VoucherDetail
        {
            VoucherDetailId = 1000,
            VoucherId = 100,
            ItemId = 1,
            TransactionQty = 10m,
            BaseQty = 10m,
            TransactionUomId = 1,
            LineNumber = 1
        });
    }

    private static void SeedPackedOutboundVoucher(AppDbContext db)
    {
        db.Vouchers.Add(new Voucher
        {
            VoucherId = 200,
            VoucherCode = "OUT-CW",
            VoucherType = VoucherTypeEnum.XuatKho,
            WarehouseId = 1,
            IsPosted = true,
            PackedAt = DateTime.UtcNow,
            PackedBy = "packer",
            FulfillmentStatus = FulfillmentStatusEnum.Packed,
            CreatedBy = "qa"
        });
        db.VoucherDetails.Add(new VoucherDetail
        {
            VoucherDetailId = 2000,
            VoucherId = 200,
            ItemId = 1,
            TransactionQty = 5m,
            BaseQty = 5m,
            TransactionUomId = 1,
            LineNumber = 1
        });
        db.OutboundPackages.Add(new OutboundPackage
        {
            OutboundPackageId = 900,
            PackageCode = "PKG-200",
            VoucherId = 200,
            WarehouseId = 1,
            TotalQuantity = 5m,
            ItemCount = 1,
            PackedBy = "packer",
            PackedAt = DateTime.UtcNow
        });
    }
}
