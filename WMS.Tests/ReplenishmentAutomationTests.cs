using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WMS.Data;
using WMS.Models;
using WMS.Services;

namespace WMS.Tests;

public class ReplenishmentAutomationTests
{
    [Fact]
    public async Task BuildSuggestionsAsync_ShouldUseDemandForecastAndRoutingPriority()
    {
        await using var db = CreateDb(nameof(BuildSuggestionsAsync_ShouldUseDemandForecastAndRoutingPriority));
        SeedWarehouse(db);
        SeedReplenishmentItem(db);
        db.Vouchers.Add(new Voucher
        {
            VoucherId = 10,
            VoucherCode = "OUT-DEMAND",
            VoucherType = VoucherTypeEnum.XuatKho,
            WarehouseId = 1,
            RequestedDeliveryDate = DateTime.Today.AddDays(1),
            CreatedBy = "qa"
        });
        db.VoucherDetails.Add(new VoucherDetail
        {
            VoucherDetailId = 10,
            VoucherId = 10,
            ItemId = 1,
            TransactionQty = 12,
            BaseQty = 12,
            TransactionUomId = 1
        });
        db.Vouchers.Add(new Voucher
        {
            VoucherId = 11,
            VoucherCode = "OUT-HISTORY",
            VoucherType = VoucherTypeEnum.XuatKho,
            WarehouseId = 1,
            IsPosted = true,
            CompletedAt = DateTime.Today.AddDays(-2),
            CreatedBy = "qa"
        });
        db.VoucherDetails.Add(new VoucherDetail
        {
            VoucherDetailId = 11,
            VoucherId = 11,
            ItemId = 1,
            TransactionQty = 30,
            BaseQty = 30,
            TransactionUomId = 1
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var rows = await service.BuildSuggestionsAsync(1, null, new ReplenishmentAutomationOptions
        {
            DemandHorizonDays = 3,
            ForecastHistoryDays = 30,
            ForecastHorizonDays = 2,
            ForecastSafetyFactor = 1m
        });

        var row = Assert.Single(rows);
        Assert.Equal(ReplenishmentTriggerTypeEnum.Hybrid, row.TriggerType);
        Assert.Equal(MovementTaskPriorityEnum.Urgent, row.Priority);
        Assert.Equal(12m, row.DemandQty);
        Assert.Equal(2m, row.ForecastQty);
        Assert.Equal(17m, row.SuggestedQty);
        Assert.True(row.RoutePriorityScore > 0);
        Assert.Equal(10, row.SourceAisleSequence);
        Assert.Equal(1, row.DestinationAisleSequence);
    }

    [Fact]
    public async Task RunAsync_ShouldPersistRunLineAndCreatePrioritizedMovementTask()
    {
        await using var db = CreateDb(nameof(RunAsync_ShouldPersistRunLineAndCreatePrioritizedMovementTask));
        SeedWarehouse(db);
        SeedReplenishmentItem(db);
        db.Vouchers.Add(new Voucher
        {
            VoucherId = 20,
            VoucherCode = "OUT-AUTO",
            VoucherType = VoucherTypeEnum.XuatKho,
            WarehouseId = 1,
            RequestedDeliveryDate = DateTime.Today,
            CreatedBy = "qa"
        });
        db.VoucherDetails.Add(new VoucherDetail
        {
            VoucherDetailId = 20,
            VoucherId = 20,
            ItemId = 1,
            TransactionQty = 8,
            BaseQty = 8,
            TransactionUomId = 1
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var run = await service.RunAsync(new ReplenishmentAutomationRunRequest
        {
            WarehouseId = 1,
            Actor = "planner",
            AutoCreateTasks = true,
            MaxTasks = 10,
            Options = new ReplenishmentAutomationOptions
            {
                DemandHorizonDays = 3,
                ForecastHistoryDays = 30,
                ForecastHorizonDays = 0,
                ForecastSafetyFactor = 1m
            }
        });

        Assert.Equal(ReplenishmentRunStatusEnum.Completed, run.Status);
        Assert.Equal(1, run.CreatedTaskCount);

        var line = await db.ReplenishmentAutomationLines.SingleAsync();
        var task = await db.MovementTasks.SingleAsync();
        Assert.Equal(line.MovementTaskId, task.MovementTaskId);
        Assert.Equal(run.ReplenishmentAutomationRunId, task.ReplenishmentAutomationRunId);
        Assert.Equal(line.ReplenishmentAutomationLineId, task.ReplenishmentAutomationLineId);
        Assert.Equal(MovementTaskPriorityEnum.Urgent, task.Priority);
        Assert.Equal(ReplenishmentTriggerTypeEnum.Hybrid, task.ReplenishmentTriggerType);
        Assert.True(task.RoutePriorityScore > 0);
        Assert.Equal("ReplenishmentAutomation", task.SourceModule);
    }

    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static IReplenishmentAutomationService CreateService(AppDbContext db)
    {
        var unitOfWork = new EfUnitOfWork(db);
        var movementTaskService = new MovementTaskService(db, unitOfWork);
        return new ReplenishmentAutomationService(db, unitOfWork, movementTaskService);
    }

    private static void SeedWarehouse(AppDbContext db)
    {
        db.Warehouses.Add(new Warehouse
        {
            WarehouseId = 1,
            WarehouseCode = "WH1",
            WarehouseName = "Main Warehouse",
            IsActive = true
        });
        db.Zones.AddRange(
            new Zone { ZoneId = 1, WarehouseId = 1, ZoneCode = "PICK", ZoneName = "Pick Face", ZoneType = ZoneTypeEnum.Storage, IsActive = true },
            new Zone { ZoneId = 2, WarehouseId = 1, ZoneCode = "BULK", ZoneName = "Bulk", ZoneType = ZoneTypeEnum.Storage, IsActive = true });
        db.Locations.AddRange(
            new Location { LocationId = 1, ZoneId = 1, LocationCode = "PF-01", AisleSequence = 1, IsActive = true },
            new Location { LocationId = 2, ZoneId = 2, LocationCode = "BK-10", AisleSequence = 10, IsActive = true });
    }

    private static void SeedReplenishmentItem(AppDbContext db)
    {
        db.Items.Add(new Item
        {
            ItemId = 1,
            ItemCode = "SKU-REP",
            ItemName = "Replenishment SKU",
            BaseUomId = 1,
            UnitCost = 1,
            MinThreshold = 5,
            MaxThreshold = 20,
            DefaultLocationId = 1,
            IsActive = true
        });
        db.ItemLocations.AddRange(
            new ItemLocation
            {
                ItemLocationId = 1,
                ItemId = 1,
                LocationId = 1,
                Quantity = 3,
                ReservedQty = 0,
                UpdatedAt = DateTime.UtcNow
            },
            new ItemLocation
            {
                ItemLocationId = 2,
                ItemId = 1,
                LocationId = 2,
                Quantity = 50,
                ReservedQty = 0,
                UpdatedAt = DateTime.UtcNow
            });
    }
}
