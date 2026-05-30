using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WMS.Data;
using WMS.Models;
using WMS.Services;

namespace WMS.Tests;

public class CoreWmsCompletionTests
{
    [Fact]
    public async Task Core01_DirectedPutaway_ShouldUseOwnerLotCapacityHazmatAndAuditOverride()
    {
        await using var db = CreateDb(nameof(Core01_DirectedPutaway_ShouldUseOwnerLotCapacityHazmatAndAuditOverride));
        SeedWarehouse(db);
        db.Zones.Add(new Zone { ZoneId = 10, WarehouseId = 1, ZoneCode = "HAZ", ZoneName = "Hazmat storage", ZoneType = ZoneTypeEnum.Storage, IsActive = true });
        db.Locations.Add(new Location { LocationId = 10, ZoneId = 10, LocationCode = "HZ-01", MaxCapacity = 100, AllowMixedSku = false, IsActive = true });
        db.Items.Add(new Item
        {
            ItemId = 10,
            ItemCode = "CHEM-A",
            ItemName = "Chemical A",
            BaseUomId = 1,
            UnitCost = 1,
            ItemType = ItemTypeEnum.HoaChat,
            OwnerPartnerId = 100,
            AbcClass = "A",
            IsActive = true
        });
        db.ItemLocations.Add(new ItemLocation
        {
            ItemLocationId = 10,
            ItemId = 10,
            OwnerPartnerId = 100,
            LocationId = 10,
            Quantity = 5,
            LotNumber = "LOT-HZ",
            ExpiryDate = new DateTime(2027, 1, 1),
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new DirectedPutawayService(db);
        var suggestions = await service.SuggestAsync(new[]
        {
            new PutawayPlanRequest
            {
                ItemId = 10,
                WarehouseId = 1,
                OwnerPartnerId = 100,
                Quantity = 2,
                LotNumber = "LOT-HZ",
                ExpiryDate = new DateTime(2027, 1, 1)
            }
        });

        var suggestion = Assert.Single(suggestions);
        Assert.Equal(10, suggestion.LocationId);
        Assert.Contains("lot", suggestion.Strategy, StringComparison.OrdinalIgnoreCase);
        Assert.True(suggestion.RequiresOverrideReason);

        var audit = await service.RecordOverrideAsync(suggestion, chosenLocationId: 1, "Supervisor needed fast dock unload.", "manager");
        Assert.Equal("OVERRIDE", audit.ActionType);
        Assert.Contains("Supervisor", audit.NewValue);
    }

    [Fact]
    public async Task Core02_Replenishment_ShouldIncludeWaveDemandAndCreateSlaPriorityTask()
    {
        await using var db = CreateDb(nameof(Core02_Replenishment_ShouldIncludeWaveDemandAndCreateSlaPriorityTask));
        SeedWarehouse(db);
        SeedItem(db, itemId: 20, defaultLocationId: 1, itemCode: "REP-WAVE");
        db.ItemLocations.AddRange(
            new ItemLocation { ItemLocationId = 20, ItemId = 20, LocationId = 1, Quantity = 0, UpdatedAt = DateTime.UtcNow },
            new ItemLocation { ItemLocationId = 21, ItemId = 20, LocationId = 2, Quantity = 50, UpdatedAt = DateTime.UtcNow });
        db.Vouchers.Add(new Voucher { VoucherId = 20, VoucherCode = "OUT-WAVE", VoucherType = VoucherTypeEnum.XuatKho, WarehouseId = 1, CreatedBy = "qa" });
        db.Waves.Add(new Wave
        {
            WaveId = 20,
            WaveCode = "WV-20",
            WarehouseId = 1,
            Status = WaveStatusEnum.Released,
            ReleasedAt = DateTime.Now.AddHours(-1),
            CreatedBy = "qa"
        });
        db.WaveLines.Add(new WaveLine { WaveLineId = 20, WaveId = 20, VoucherId = 20, ItemId = 20, RequiredQty = 9, PickedQty = 1, Status = 1 });
        await db.SaveChangesAsync();

        var service = CreateReplenishmentService(db);
        var rows = await service.BuildSuggestionsAsync(1, null, new ReplenishmentAutomationOptions { DemandHorizonDays = 0, ForecastHorizonDays = 0 });

        var row = Assert.Single(rows);
        Assert.Equal(8, row.DemandQty);
        Assert.Equal(MovementTaskPriorityEnum.Urgent, row.Priority);
        Assert.Equal(20, row.SuggestedQty);
    }

    [Fact]
    public void Core03_InventoryStatusEngine_ShouldSplitAvailabilityAndBlockInvalidPosting()
    {
        var rows = new[]
        {
            new ItemLocation { Quantity = 10, ReservedQty = 2, HoldStatus = InventoryHoldStatusEnum.Available },
            new ItemLocation { Quantity = 5, ReservedQty = 0, HoldStatus = InventoryHoldStatusEnum.Consigned },
            new ItemLocation { Quantity = 4, ReservedQty = 0, HoldStatus = InventoryHoldStatusEnum.QcHold },
            new ItemLocation { Quantity = 3, ReservedQty = 0, HoldStatus = InventoryHoldStatusEnum.Blocked }
        };

        var split = InventoryStatusEngine.SplitAvailability(rows);

        Assert.Equal(13, split.AvailableQty);
        Assert.Equal(7, split.UnavailableQty);
        Assert.Equal(InventoryHoldStatusEnum.QcHold, InventoryStatusEngine.FromQualityStatus(QualityStatusEnum.Pending));
        Assert.Throws<BusinessRuleException>(() => InventoryStatusEngine.EnsurePostingAllowed(InventoryTransactionTypeEnum.Pick, InventoryHoldStatusEnum.Damaged));
        InventoryStatusEngine.EnsurePostingAllowed(InventoryTransactionTypeEnum.Pick, InventoryHoldStatusEnum.Consigned);
    }

    [Fact]
    public async Task Core04_CycleCountPlanning_ShouldCreateDueSheetByAbcZoneAndAdvanceSchedule()
    {
        await using var db = CreateDb(nameof(Core04_CycleCountPlanning_ShouldCreateDueSheetByAbcZoneAndAdvanceSchedule));
        SeedWarehouse(db);
        SeedItem(db, itemId: 30, defaultLocationId: 1, itemCode: "ABC-A", abcClass: "A");
        SeedItem(db, itemId: 31, defaultLocationId: 2, itemCode: "ABC-C", abcClass: "C");
        db.ItemLocations.AddRange(
            new ItemLocation { ItemLocationId = 30, ItemId = 30, LocationId = 1, Quantity = 10, UpdatedAt = DateTime.UtcNow },
            new ItemLocation { ItemLocationId = 31, ItemId = 31, LocationId = 2, Quantity = 5, HoldStatus = InventoryHoldStatusEnum.Damaged, UpdatedAt = DateTime.UtcNow });
        db.CycleCountPrograms.Add(new CycleCountProgram
        {
            ProgramId = 30,
            ProgramName = "ABC program",
            WarehouseId = 1,
            FrequencyA = 1,
            FrequencyB = 7,
            FrequencyC = 30,
            IsBlindCount = true,
            CreatedBy = "qa"
        });
        await db.SaveChangesAsync();

        var service = new CycleCountPlanningService(db, new EfUnitOfWork(db));
        var scheduled = await service.CreateOrRefreshSchedulesAsync(30, new[] { 1, 2 });
        var sheet = await service.GenerateDueSheetAsync(30, "counter");

        Assert.Equal(2, scheduled);
        Assert.Equal(StockCountStatusEnum.Draft, sheet.Status);
        Assert.Equal(2, await db.StockCountLines.CountAsync(l => l.StockCountSheetId == sheet.StockCountSheetId));
        var aSchedule = await db.CycleCountSchedules.SingleAsync(s => s.ItemId == 30);
        Assert.Equal('A', aSchedule.AbcClass);
        Assert.Equal(DateTime.Today.AddDays(1), aSchedule.NextScheduledAt!.Value.Date);
        Assert.Equal(1, aSchedule.CountAttempt);
    }

    [Fact]
    public async Task Core05_ReturnRma_ShouldTraceOriginalOutboundAndRestockAfterQc()
    {
        await using var db = CreateDb(nameof(Core05_ReturnRma_ShouldTraceOriginalOutboundAndRestockAfterQc));
        SeedWarehouse(db);
        SeedItem(db, itemId: 40, defaultLocationId: 1, itemCode: "RET-ITEM");
        db.Vouchers.Add(new Voucher { VoucherId = 40, VoucherCode = "OUT-40", VoucherType = VoucherTypeEnum.XuatKho, WarehouseId = 1, IsPosted = true, CreatedBy = "qa" });
        await db.SaveChangesAsync();

        var service = new ReturnRmaService(db, new EfUnitOfWork(db), new InventoryBalanceService(db));
        var rma = await service.CreateReturnAsync(new ReturnRmaRequest
        {
            OriginalOutboundVoucherId = 40,
            WarehouseId = 1,
            OwnerPartnerId = 100,
            Reason = "Customer return",
            Actor = "csr",
            Lines =
            {
                new ReturnRmaLineRequest { ItemId = 40, Quantity = 3, BaseUomId = 1, LocationId = 1, LotNumber = "RMA-LOT" }
            }
        });
        var disposition = await service.DispositionAsync(rma.VoucherId, QcDispositionEnum.Accept, "qc", "Passed visual inspection");

        var saved = await db.Vouchers.Include(v => v.Details).SingleAsync(v => v.VoucherId == rma.VoucherId);
        Assert.Equal(40, saved.ParentVoucherId);
        Assert.Equal(VoucherTypeEnum.KhachTra, saved.VoucherType);
        Assert.True(saved.IsPosted);
        Assert.Equal(QualityStatusEnum.Passed, saved.Details.Single().QualityStatus);
        Assert.Equal(3, disposition.RestockedQty);
        Assert.Equal(3, await db.ItemLocations.Where(il => il.ItemId == 40 && il.HoldStatus == InventoryHoldStatusEnum.Available).SumAsync(il => il.Quantity));
        Assert.Contains(await db.AuditLogs.ToListAsync(), a => a.AppModule == "ReturnsRMA" && a.ActionType == "RMA_QC");
    }

    [Fact]
    public async Task Core06_CrossDock_ShouldSubtractInboundPutawayAndAvoidDoubleStorage()
    {
        await using var db = CreateDb(nameof(Core06_CrossDock_ShouldSubtractInboundPutawayAndAvoidDoubleStorage));
        SeedWarehouse(db);
        SeedItem(db, itemId: 50, defaultLocationId: 1, itemCode: "XD-ITEM");
        db.Vouchers.AddRange(
            new Voucher { VoucherId = 50, VoucherCode = "IN-50", VoucherType = VoucherTypeEnum.NhapKho, WarehouseId = 1, InboundStatus = InboundStatusEnum.Receiving, CreatedBy = "qa" },
            new Voucher { VoucherId = 51, VoucherCode = "OUT-51", VoucherType = VoucherTypeEnum.XuatKho, WarehouseId = 1, CreatedBy = "qa" });
        db.VoucherDetails.AddRange(
            new VoucherDetail { VoucherDetailId = 50, VoucherId = 50, ItemId = 50, LocationId = 1, TransactionQty = 10, BaseQty = 10, TransactionUomId = 1, QualityStatus = QualityStatusEnum.Good },
            new VoucherDetail { VoucherDetailId = 51, VoucherId = 51, ItemId = 50, TransactionQty = 4, BaseQty = 4, TransactionUomId = 1 });
        await db.SaveChangesAsync();

        var unitOfWork = new EfUnitOfWork(db);
        var crossDock = new CrossDockService(db, unitOfWork);
        var created = await crossDock.ExecuteCrossDockAsync(50, 51, 50, 4, 3, 1, "planner", null, 50, 51);
        Assert.True(created.Succeeded);
        var taskId = await db.CrossDockTasks.Select(t => t.CrossDockTaskId).SingleAsync();
        var completed = await crossDock.CompleteCrossDockTaskAsync(taskId);
        Assert.True(completed.Succeeded);

        var inbound = new InboundExecutionService(db, unitOfWork, new InventoryBalanceService(db));
        var inboundResult = await inbound.CompleteInboundAsync(50, 1, "receiver", null);
        Assert.True(inboundResult.Succeeded);

        Assert.Equal(6, await db.ItemLocations.Where(il => il.ItemId == 50 && il.LocationId == 1).SumAsync(il => il.Quantity));
        Assert.Equal(4, await db.ItemLocations.Where(il => il.ItemId == 50 && il.LocationId == 3).SumAsync(il => il.Quantity));
    }

    [Fact]
    public async Task Core07_AdvancedAllocation_ShouldSupportFefoLifoOwnerLotPartialAndReallocation()
    {
        await using var db = CreateDb(nameof(Core07_AdvancedAllocation_ShouldSupportFefoLifoOwnerLotPartialAndReallocation));
        SeedWarehouse(db);
        SeedItem(db, itemId: 60, defaultLocationId: 1, itemCode: "ALLOC");
        db.ItemLocations.AddRange(
            new ItemLocation { ItemLocationId = 60, ItemId = 60, OwnerPartnerId = 100, LocationId = 1, Quantity = 5, LotNumber = "LATE", ExpiryDate = new DateTime(2027, 1, 1), UpdatedAt = DateTime.UtcNow.AddDays(-5) },
            new ItemLocation { ItemLocationId = 61, ItemId = 60, OwnerPartnerId = 100, LocationId = 2, Quantity = 5, LotNumber = "EARLY", ExpiryDate = new DateTime(2026, 1, 1), UpdatedAt = DateTime.UtcNow.AddDays(-1) },
            new ItemLocation { ItemLocationId = 62, ItemId = 60, OwnerPartnerId = 200, LocationId = 2, Quantity = 99, ExpiryDate = new DateTime(2025, 1, 1), UpdatedAt = DateTime.UtcNow },
            new ItemLocation { ItemLocationId = 63, ItemId = 60, OwnerPartnerId = 100, LocationId = 1, Quantity = 99, HoldStatus = InventoryHoldStatusEnum.Blocked, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new AdvancedAllocationService(db);
        var fefo = await service.AllocateAsync(new AllocationRequest { ItemId = 60, WarehouseId = 1, OwnerPartnerId = 100, RequiredQty = 7, Strategy = AllocationStrategyEnum.Fefo });
        Assert.True(fefo.IsComplete);
        Assert.Equal(61, fefo.Slices.First().ItemLocationId);
        Assert.Equal(7, fefo.AllocatedQty);

        var lifo = await service.AllocateAsync(new AllocationRequest { ItemId = 60, WarehouseId = 1, OwnerPartnerId = 100, RequiredQty = 3, Strategy = AllocationStrategyEnum.Lifo });
        Assert.Equal(61, lifo.Slices.First().ItemLocationId);

        var partial = await service.AllocateAsync(new AllocationRequest
        {
            ItemId = 60,
            WarehouseId = 1,
            OwnerPartnerId = 100,
            RequiredQty = 7,
            ExcludedLocationIds = new[] { 2 },
            AllowPartial = true
        });
        Assert.False(partial.IsComplete);
        Assert.Equal(5, partial.AllocatedQty);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.AllocateAsync(new AllocationRequest { ItemId = 60, WarehouseId = 1, OwnerPartnerId = 100, RequiredQty = 11 }));
    }

    [Fact]
    public async Task Core08_Cartonization_ShouldSuggestCartonByWeightVolumeAndRequireOverrideReason()
    {
        await using var db = CreateDb(nameof(Core08_Cartonization_ShouldSuggestCartonByWeightVolumeAndRequireOverrideReason));
        SeedWarehouse(db);
        db.Items.Add(new Item
        {
            ItemId = 70,
            ItemCode = "BOX-SKU",
            ItemName = "Boxed SKU",
            BaseUomId = 1,
            UnitCost = 1,
            Weight = 2,
            Length = 20,
            Width = 20,
            Height = 20,
            IsActive = true
        });
        db.Vouchers.Add(new Voucher { VoucherId = 70, VoucherCode = "OUT-70", VoucherType = VoucherTypeEnum.XuatKho, WarehouseId = 1, OwnerPartnerId = 100, CreatedBy = "qa" });
        db.VoucherDetails.Add(new VoucherDetail { VoucherDetailId = 70, VoucherId = 70, ItemId = 70, TransactionQty = 4, BaseQty = 4, TransactionUomId = 1 });
        await db.SaveChangesAsync();

        var service = new CartonizationService(db);
        var recommendation = await service.RecommendAsync(70, new[]
        {
            new CartonOption { Code = "S", PackageType = "Small", MaxWeightKg = 3, LengthCm = 25, WidthCm = 20, HeightCm = 20 },
            new CartonOption { Code = "M", PackageType = "Medium", MaxWeightKg = 12, LengthCm = 50, WidthCm = 40, HeightCm = 30, OwnerPartnerId = 100 }
        });

        Assert.Equal("Medium", recommendation.PackageType);
        Assert.Equal(1, recommendation.PackageCount);
        Assert.Equal(8, recommendation.EstimatedWeightKg);
        Assert.Throws<BusinessRuleException>(() => service.BuildOverrideNote(recommendation, "Small", ""));
        Assert.Contains("chosen Large", service.BuildOverrideNote(recommendation, "Large", "Fragile goods need void fill."));
    }

    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static IReplenishmentAutomationService CreateReplenishmentService(AppDbContext db)
    {
        var unitOfWork = new EfUnitOfWork(db);
        var movementTaskService = new MovementTaskService(db, unitOfWork);
        return new ReplenishmentAutomationService(db, unitOfWork, movementTaskService);
    }

    private static void SeedWarehouse(AppDbContext db)
    {
        db.Warehouses.Add(new Warehouse { WarehouseId = 1, WarehouseCode = "WH1", WarehouseName = "Main", IsActive = true });
        db.Zones.AddRange(
            new Zone { ZoneId = 1, WarehouseId = 1, ZoneCode = "PICK", ZoneName = "Pick face", ZoneType = ZoneTypeEnum.Storage, IsActive = true },
            new Zone { ZoneId = 2, WarehouseId = 1, ZoneCode = "BULK", ZoneName = "Bulk", ZoneType = ZoneTypeEnum.Storage, IsActive = true },
            new Zone { ZoneId = 3, WarehouseId = 1, ZoneCode = "XD", ZoneName = "Cross dock", ZoneType = ZoneTypeEnum.CrossDock, IsActive = true });
        db.Locations.AddRange(
            new Location { LocationId = 1, ZoneId = 1, LocationCode = "PF-01", AisleSequence = 1, MaxCapacity = 1000, AllowMixedSku = false, IsActive = true },
            new Location { LocationId = 2, ZoneId = 2, LocationCode = "BK-01", AisleSequence = 10, MaxCapacity = 1000, AllowMixedSku = true, IsActive = true },
            new Location { LocationId = 3, ZoneId = 3, LocationCode = "XD-01", AisleSequence = 2, MaxCapacity = 1000, AllowMixedSku = true, IsActive = true });
    }

    private static void SeedItem(AppDbContext db, int itemId, int defaultLocationId, string itemCode, string? abcClass = null)
    {
        db.Items.Add(new Item
        {
            ItemId = itemId,
            ItemCode = itemCode,
            ItemName = itemCode,
            BaseUomId = 1,
            UnitCost = 1,
            MinThreshold = 5,
            MaxThreshold = 20,
            DefaultLocationId = defaultLocationId,
            AbcClass = abcClass,
            IsActive = true
        });
    }
}
