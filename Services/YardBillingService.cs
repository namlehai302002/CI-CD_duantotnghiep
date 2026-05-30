using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public interface IYardBillingService
{
    Task<YardBillingCharge?> CalculateChargeAsync(long yardVisitId, string actor);

    Task<YardBillingCharge?> AutoChargeOnGateOutAsync(long yardVisitId, string actor);

    Task<YardBillingCharge> ConfirmChargeAsync(long chargeId, int? scopedWarehouseId, string actor);

    Task<YardBillingCharge> WaiveChargeAsync(long chargeId, string reason, int? scopedWarehouseId, string actor);

    Task<int> RecalculateDraftChargesAsync(int? warehouseId, int? scopedWarehouseId, string actor);
}

public class YardBillingService : IYardBillingService
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public YardBillingService(AppDbContext db, IUnitOfWork unitOfWork)
    {
        _db = db;
        _unitOfWork = unitOfWork;
    }

    private static DateTime Now => VietnamTime.Now;

    public async Task<YardBillingCharge?> CalculateChargeAsync(long yardVisitId, string actor)
    {
        var visit = await LoadVisitAsync(yardVisitId);
        if (visit == null)
            throw new BusinessRuleException("Không tìm thấy lượt vào bãi.", "YARD_VISIT_NOT_FOUND", "YardVisit");

        var existingCharge = await _db.YardBillingCharges
            .AnyAsync(c => c.YardVisitId == yardVisitId && c.Status != YardChargeStatusEnum.Waived);
        if (existingCharge)
            throw new BusinessRuleException("Lượt vào bãi này đã có dòng phí đang xử lý.", "YARD_CHARGE_DUPLICATE", "YardBillingCharge");

        var charge = await BuildChargeAsync(visit, actor);
        if (charge == null)
            return null;

        _db.YardBillingCharges.Add(charge);
        await _unitOfWork.SaveChangesAsync();
        return charge;
    }

    public async Task<YardBillingCharge?> AutoChargeOnGateOutAsync(long yardVisitId, string actor)
    {
        var hasCharge = await _db.YardBillingCharges
            .AnyAsync(c => c.YardVisitId == yardVisitId && c.Status != YardChargeStatusEnum.Waived);
        if (hasCharge)
            return null;

        var visit = await LoadVisitAsync(yardVisitId);
        if (visit == null)
            return null;

        var charge = await BuildChargeAsync(visit, actor);
        if (charge == null)
            return null;

        _db.YardBillingCharges.Add(charge);
        await _unitOfWork.SaveChangesAsync();
        return charge;
    }

    public async Task<YardBillingCharge> ConfirmChargeAsync(long chargeId, int? scopedWarehouseId, string actor)
    {
        var charge = await _db.YardBillingCharges
            .FirstOrDefaultAsync(c => c.YardBillingChargeId == chargeId);
        if (charge == null)
            throw new BusinessRuleException("Không tìm thấy dòng phí.", "YARD_CHARGE_NOT_FOUND", "YardBillingCharge");

        EnsureScope(charge.WarehouseId, scopedWarehouseId);

        if (charge.Status != YardChargeStatusEnum.Draft)
            throw new BusinessRuleException(
                $"Chỉ có thể xác nhận phí ở trạng thái Nháp. Trạng thái hiện tại: {charge.Status}.",
                "YARD_CHARGE_INVALID_STATUS", "YardBillingCharge");

        var now = Now;
        charge.Status = YardChargeStatusEnum.Confirmed;
        charge.ConfirmedBy = actor;
        charge.ConfirmedAt = now;
        charge.UpdatedAt = now;

        await _unitOfWork.SaveChangesAsync();
        return charge;
    }

    public async Task<YardBillingCharge> WaiveChargeAsync(long chargeId, string reason, int? scopedWarehouseId, string actor)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new BusinessRuleException("Vui lòng nhập lý do miễn phí.", "YARD_WAIVE_REASON_REQUIRED", "YardBillingCharge");

        var charge = await _db.YardBillingCharges
            .FirstOrDefaultAsync(c => c.YardBillingChargeId == chargeId);
        if (charge == null)
            throw new BusinessRuleException("Không tìm thấy dòng phí.", "YARD_CHARGE_NOT_FOUND", "YardBillingCharge");

        EnsureScope(charge.WarehouseId, scopedWarehouseId);

        if (charge.Status is YardChargeStatusEnum.Invoiced)
            throw new BusinessRuleException(
                "Không thể miễn phí đã xuất hóa đơn.",
                "YARD_CHARGE_ALREADY_INVOICED", "YardBillingCharge");

        var now = Now;
        charge.Status = YardChargeStatusEnum.Waived;
        charge.WaivedBy = actor;
        charge.WaivedAt = now;
        charge.WaivedReason = reason.Trim();
        charge.UpdatedAt = now;

        await _unitOfWork.SaveChangesAsync();
        return charge;
    }

    public async Task<int> RecalculateDraftChargesAsync(int? warehouseId, int? scopedWarehouseId, string actor)
    {
        if (scopedWarehouseId.HasValue)
        {
            if (warehouseId.HasValue && warehouseId.Value != scopedWarehouseId.Value)
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác phí lưu bãi của kho khác.");

            warehouseId = scopedWarehouseId.Value;
        }

        var query = _db.YardBillingCharges
            .Include(c => c.YardVisit)!.ThenInclude(v => v!.Trailer)
            .Include(c => c.YardVisit)!.ThenInclude(v => v!.CurrentSpot)
            .Include(c => c.YardVisit)!.ThenInclude(v => v!.Voucher)
            .Where(c => c.Status == YardChargeStatusEnum.Draft);

        if (warehouseId.HasValue)
            query = query.Where(c => c.WarehouseId == warehouseId.Value);

        var charges = await query.ToListAsync();
        var now = Now;
        var updated = 0;

        foreach (var charge in charges)
        {
            var visit = charge.YardVisit;
            if (visit == null)
                continue;

            var rate = await FindBestRateAsync(visit);
            if (rate == null)
                continue;

            ApplyRateSnapshot(charge, visit, rate, now);
            charge.UpdatedAt = now;
            updated++;
        }

        if (updated > 0)
            await _unitOfWork.SaveChangesAsync();

        return updated;
    }

    private async Task<YardVisit?> LoadVisitAsync(long yardVisitId)
        => await _db.YardVisits
            .Include(v => v.Trailer)
            .Include(v => v.CurrentSpot)
            .Include(v => v.Voucher)
            .FirstOrDefaultAsync(v => v.YardVisitId == yardVisitId);

    private async Task<YardBillingCharge?> BuildChargeAsync(YardVisit visit, string actor)
    {
        var rate = await FindBestRateAsync(visit);
        if (rate == null)
            return null;

        var now = Now;
        var dwellMinutes = visit.GetDwellMinutes(now);
        var chargeableMinutes = Math.Max(0, dwellMinutes - rate.FreeTimeMinutes);
        if (chargeableMinutes <= 0)
            return null;

        return new YardBillingCharge
        {
            YardVisitId = visit.YardVisitId,
            YardBillingRateId = rate.YardBillingRateId,
            WarehouseId = visit.WarehouseId,
            TotalDwellMinutes = dwellMinutes,
            FreeTimeMinutes = rate.FreeTimeMinutes,
            ChargeableMinutes = chargeableMinutes,
            AppliedRatePerHour = rate.RatePerHour,
            Amount = CalculateAmount(chargeableMinutes, rate.RatePerHour, rate.MaxDailyRate),
            Currency = rate.Currency,
            Status = YardChargeStatusEnum.Draft,
            CreatedBy = actor,
            CreatedAt = now
        };
    }

    private static void ApplyRateSnapshot(YardBillingCharge charge, YardVisit visit, YardBillingRate rate, DateTime now)
    {
        var dwellMinutes = visit.GetDwellMinutes(now);
        var chargeableMinutes = Math.Max(0, dwellMinutes - rate.FreeTimeMinutes);

        charge.YardBillingRateId = rate.YardBillingRateId;
        charge.TotalDwellMinutes = dwellMinutes;
        charge.FreeTimeMinutes = rate.FreeTimeMinutes;
        charge.ChargeableMinutes = chargeableMinutes;
        charge.AppliedRatePerHour = rate.RatePerHour;
        charge.Amount = CalculateAmount(chargeableMinutes, rate.RatePerHour, rate.MaxDailyRate);
        charge.Currency = rate.Currency;
    }

    /// <summary>
    /// Find the most specific active rate. Partner > carrier > trailer type > spot type.
    /// Null rate fields are wildcards.
    /// </summary>
    private async Task<YardBillingRate?> FindBestRateAsync(YardVisit visit)
    {
        var partnerId = visit.Voucher?.PartnerId;
        var carrierName = visit.Trailer?.CarrierName;
        var trailerType = visit.Trailer?.TrailerType;
        var spotType = visit.CurrentSpot?.SpotType;

        var rates = await _db.YardBillingRates
            .Where(r => r.WarehouseId == visit.WarehouseId && r.IsActive)
            .ToListAsync();

        if (rates.Count == 0)
            return null;

        return rates
            .Select(r => new
            {
                Rate = r,
                Score =
                    (r.PartnerId.HasValue && r.PartnerId == partnerId ? 8 : r.PartnerId == null ? 0 : -100) +
                    (r.CarrierName != null && string.Equals(r.CarrierName, carrierName, StringComparison.OrdinalIgnoreCase) ? 4 : r.CarrierName == null ? 0 : -100) +
                    (r.TrailerType != null && r.TrailerType == trailerType ? 2 : r.TrailerType == null ? 0 : -100) +
                    (r.SpotType != null && r.SpotType == spotType ? 1 : r.SpotType == null ? 0 : -100)
            })
            .Where(x => x.Score >= 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Rate.UpdatedAt ?? x.Rate.CreatedAt)
            .ThenByDescending(x => x.Rate.YardBillingRateId)
            .Select(x => x.Rate)
            .FirstOrDefault();
    }

    private static decimal CalculateAmount(int chargeableMinutes, decimal ratePerHour, decimal maxDailyRate)
    {
        var rawAmount = Math.Round(chargeableMinutes / 60m * ratePerHour, 0, MidpointRounding.AwayFromZero);

        if (maxDailyRate > 0)
        {
            var fullDays = chargeableMinutes / (24 * 60);
            var remainderMinutes = chargeableMinutes % (24 * 60);
            var dailyCapped = fullDays * maxDailyRate;
            var remainderAmount = Math.Min(Math.Round(remainderMinutes / 60m * ratePerHour, 0, MidpointRounding.AwayFromZero), maxDailyRate);
            rawAmount = dailyCapped + remainderAmount;
        }

        return rawAmount;
    }

    private static void EnsureScope(int warehouseId, int? scopedWarehouseId)
    {
        if (scopedWarehouseId.HasValue && warehouseId != scopedWarehouseId.Value)
            throw new UnauthorizedAccessException("Bạn không có quyền thao tác phí lưu bãi của kho khác.");
    }
}
