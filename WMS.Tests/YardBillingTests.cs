using Microsoft.EntityFrameworkCore;
using Xunit;
using WMS.Common;
using WMS.Data;
using WMS.Models;
using WMS.Services;

namespace WMS.Tests;

public class YardBillingTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly YardBillingService _service;

    public YardBillingTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new AppDbContext(options);
        _uow = new EfUnitOfWork(_db);

        _db.Warehouses.Add(new Warehouse { WarehouseId = 1, WarehouseCode = "WH01", WarehouseName = "Kho chính" });
        _db.SaveChanges();

        _service = new YardBillingService(_db, _uow);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task CalculateCharge_UnderFreeTime_ReturnsNull()
    {
        _db.YardBillingRates.Add(new YardBillingRate { WarehouseId = 1, FreeTimeMinutes = 120, RatePerHour = 100000, IsActive = true });
        _db.Trailers.Add(new Trailer { TrailerId = 1, CarrierName = "VNS" });
        _db.YardVisits.Add(new YardVisit { YardVisitId = 1, WarehouseId = 1, TrailerId = 1, GateInAt = VietnamTime.Now.AddMinutes(-60) });
        _db.SaveChanges();

        var result = await _service.CalculateChargeAsync(1, "system");

        Assert.Null(result);
    }

    [Fact]
    public async Task CalculateCharge_OverFreeTime_CalculatesCorrectly()
    {
        _db.YardBillingRates.Add(new YardBillingRate { WarehouseId = 1, FreeTimeMinutes = 120, RatePerHour = 100000, IsActive = true });
        _db.Trailers.Add(new Trailer { TrailerId = 1, CarrierName = "VNS" });
        _db.YardVisits.Add(new YardVisit { YardVisitId = 1, WarehouseId = 1, TrailerId = 1, GateInAt = VietnamTime.Now.AddMinutes(-180) });
        _db.SaveChanges();

        var result = await _service.CalculateChargeAsync(1, "system");

        Assert.NotNull(result);
        Assert.Equal(60, result.ChargeableMinutes);
        Assert.Equal(100000m, result.Amount);
        Assert.Equal(YardChargeStatusEnum.Draft, result.Status);
    }

    [Fact]
    public async Task CalculateCharge_WithDailyCap_AppliesCap()
    {
        _db.YardBillingRates.Add(new YardBillingRate { WarehouseId = 1, FreeTimeMinutes = 0, RatePerHour = 100000, MaxDailyRate = 500000, IsActive = true });
        _db.Trailers.Add(new Trailer { TrailerId = 1, CarrierName = "VNS" });
        _db.YardVisits.Add(new YardVisit { YardVisitId = 1, WarehouseId = 1, TrailerId = 1, GateInAt = VietnamTime.Now.AddHours(-26) });
        _db.SaveChanges();

        var result = await _service.CalculateChargeAsync(1, "system");

        Assert.NotNull(result);
        Assert.Equal(700000m, result.Amount);
    }

    [Fact]
    public async Task AutoChargeOnGateOut_MatchesSpecificRate()
    {
        _db.YardBillingRates.Add(new YardBillingRate { WarehouseId = 1, FreeTimeMinutes = 120, RatePerHour = 100000, IsActive = true });
        _db.YardBillingRates.Add(new YardBillingRate { WarehouseId = 1, CarrierName = "VNS", FreeTimeMinutes = 60, RatePerHour = 200000, IsActive = true });
        _db.Trailers.Add(new Trailer { TrailerId = 1, CarrierName = "VNS" });
        _db.YardVisits.Add(new YardVisit { YardVisitId = 1, WarehouseId = 1, TrailerId = 1, GateInAt = VietnamTime.Now.AddMinutes(-90) });
        _db.SaveChanges();

        var result = await _service.AutoChargeOnGateOutAsync(1, "system");

        Assert.NotNull(result);
        Assert.Equal(30, result.ChargeableMinutes);
        Assert.Equal(100000m, result.Amount);
        Assert.Equal(200000m, result.AppliedRatePerHour);
    }

    [Fact]
    public async Task CalculateCharge_PartnerSpecificRate_WinsOverCarrierAndGeneral()
    {
        _db.Partners.Add(new Partner { PartnerId = 1, PartnerCode = "C001", PartnerName = "Khách A", PartnerType = PartnerTypeEnum.Customer });
        _db.Vouchers.Add(new Voucher { VoucherId = 1, VoucherCode = "PN-001", WarehouseId = 1, PartnerId = 1, VoucherType = VoucherTypeEnum.NhapKho, CreatedBy = "system" });
        _db.YardBillingRates.Add(new YardBillingRate { WarehouseId = 1, FreeTimeMinutes = 120, RatePerHour = 100000, IsActive = true });
        _db.YardBillingRates.Add(new YardBillingRate { WarehouseId = 1, CarrierName = "VNS", FreeTimeMinutes = 60, RatePerHour = 200000, IsActive = true });
        _db.YardBillingRates.Add(new YardBillingRate { WarehouseId = 1, PartnerId = 1, FreeTimeMinutes = 30, RatePerHour = 300000, IsActive = true });
        _db.Trailers.Add(new Trailer { TrailerId = 1, CarrierName = "VNS" });
        _db.YardVisits.Add(new YardVisit { YardVisitId = 1, WarehouseId = 1, TrailerId = 1, VoucherId = 1, GateInAt = VietnamTime.Now.AddMinutes(-90) });
        _db.SaveChanges();

        var result = await _service.CalculateChargeAsync(1, "system");

        Assert.NotNull(result);
        Assert.Equal(60, result.ChargeableMinutes);
        Assert.Equal(300000m, result.Amount);
        Assert.Equal(300000m, result.AppliedRatePerHour);
    }

    [Fact]
    public async Task WaiveCharge_ValidCharge_UpdatesStatusAndReason()
    {
        _db.YardBillingCharges.Add(new YardBillingCharge { YardBillingChargeId = 1, YardVisitId = 1, WarehouseId = 1, Status = YardChargeStatusEnum.Draft, Amount = 100000 });
        _db.SaveChanges();

        var result = await _service.WaiveChargeAsync(1, "Khách hàng VIP", null, "admin");

        Assert.Equal(YardChargeStatusEnum.Waived, result.Status);
        Assert.Equal("Khách hàng VIP", result.WaivedReason);
        Assert.Equal("admin", result.WaivedBy);
    }

    [Fact]
    public async Task ConfirmCharge_InvalidStatus_ThrowsException()
    {
        _db.YardBillingCharges.Add(new YardBillingCharge { YardBillingChargeId = 1, YardVisitId = 1, WarehouseId = 1, Status = YardChargeStatusEnum.Waived, Amount = 100000 });
        _db.SaveChanges();

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(async () => await _service.ConfirmChargeAsync(1, null, "admin"));
        Assert.Contains("trạng thái Nháp", ex.Message);
    }

    [Fact]
    public async Task RecalculateDraftCharges_UpdatesOnlyDraftCharges()
    {
        var now = VietnamTime.Now;
        _db.YardBillingRates.Add(new YardBillingRate { WarehouseId = 1, FreeTimeMinutes = 60, RatePerHour = 200000, IsActive = true });
        _db.Trailers.Add(new Trailer { TrailerId = 1, CarrierName = "VNS" });
        _db.YardVisits.Add(new YardVisit { YardVisitId = 1, WarehouseId = 1, TrailerId = 1, GateInAt = now.AddMinutes(-180), GateOutAt = now });
        _db.YardVisits.Add(new YardVisit { YardVisitId = 2, WarehouseId = 1, TrailerId = 1, GateInAt = now.AddMinutes(-180), GateOutAt = now });
        _db.YardBillingCharges.Add(new YardBillingCharge { YardBillingChargeId = 1, YardVisitId = 1, WarehouseId = 1, Status = YardChargeStatusEnum.Draft, FreeTimeMinutes = 120, ChargeableMinutes = 60, AppliedRatePerHour = 100000, Amount = 100000, Currency = "VND" });
        _db.YardBillingCharges.Add(new YardBillingCharge { YardBillingChargeId = 2, YardVisitId = 2, WarehouseId = 1, Status = YardChargeStatusEnum.Confirmed, FreeTimeMinutes = 120, ChargeableMinutes = 60, AppliedRatePerHour = 100000, Amount = 100000, Currency = "VND" });
        _db.SaveChanges();

        var updated = await _service.RecalculateDraftChargesAsync(1, null, "admin");

        var draft = await _db.YardBillingCharges.FindAsync(1L);
        var confirmed = await _db.YardBillingCharges.FindAsync(2L);
        Assert.Equal(1, updated);
        Assert.NotNull(draft);
        Assert.Equal(60, draft.FreeTimeMinutes);
        Assert.Equal(120, draft.ChargeableMinutes);
        Assert.Equal(400000m, draft.Amount);
        Assert.NotNull(confirmed);
        Assert.Equal(120, confirmed.FreeTimeMinutes);
        Assert.Equal(100000m, confirmed.Amount);
    }
}
