using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public sealed class ThreePlBillingRunRequest
{
    public int WarehouseId { get; init; }
    public int OwnerPartnerId { get; init; }
    public DateTime PeriodFrom { get; init; }
    public DateTime PeriodTo { get; init; }
    public string Actor { get; init; } = "system";
}

public interface IThreePlBillingService
{
    Task<ThreePlBillingRun> GenerateDraftRunAsync(ThreePlBillingRunRequest request, CancellationToken ct = default);
    Task<ThreePlBillingRun> ConfirmRunAsync(long runId, int? scopedWarehouseId, string actor, CancellationToken ct = default);
    Task<ThreePlBillingRun> VoidRunAsync(long runId, string reason, int? scopedWarehouseId, string actor, CancellationToken ct = default);
}

public class ThreePlBillingService : IThreePlBillingService
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public ThreePlBillingService(AppDbContext db, IUnitOfWork? unitOfWork = null)
    {
        _db = db;
        _unitOfWork = unitOfWork ?? new EfUnitOfWork(db);
    }

    private static DateTime Now => VietnamTime.Now;

    public async Task<ThreePlBillingRun> GenerateDraftRunAsync(ThreePlBillingRunRequest request, CancellationToken ct = default)
    {
        if (request.WarehouseId <= 0 || request.OwnerPartnerId <= 0)
            throw new BusinessRuleException("Kho và chủ hàng là bắt buộc.", "THREEPL_SCOPE_REQUIRED", "ThreePlBillingRun");
        var from = request.PeriodFrom.Date;
        var to = request.PeriodTo.Date;
        if (to < from)
            throw new BusinessRuleException("Ngày kết thúc kỳ tính phí phải lớn hơn hoặc bằng ngày bắt đầu.", "THREEPL_PERIOD_INVALID", "ThreePlBillingRun");

        var ownerOk = await _db.Partners.AnyAsync(p => p.PartnerId == request.OwnerPartnerId && p.IsThreePlClient && p.IsActive, ct);
        if (!ownerOk)
            throw new BusinessRuleException("Chủ hàng 3PL không hợp lệ hoặc chưa hoạt động.", "THREEPL_OWNER_INVALID", "Partner");

        var key = BuildRunKey(request.WarehouseId, request.OwnerPartnerId, from, to);
        var run = await _db.ThreePlBillingRuns
            .Include(r => r.Charges)
            .FirstOrDefaultAsync(r => r.IdempotencyKey == key, ct);
        if (run == null)
        {
            run = new ThreePlBillingRun
            {
                WarehouseId = request.WarehouseId,
                OwnerPartnerId = request.OwnerPartnerId,
                PeriodFrom = from,
                PeriodTo = to,
                RunCode = await GenerateRunCodeAsync(request.WarehouseId, request.OwnerPartnerId, from, ct),
                Status = ThreePlBillingRunStatusEnum.Draft,
                Currency = "VND",
                IdempotencyKey = key,
                CreatedBy = CleanActor(request.Actor),
                CreatedAt = Now
            };
            _db.ThreePlBillingRuns.Add(run);
            await _unitOfWork.SaveChangesAsync(ct);
        }
        else if (run.Status != ThreePlBillingRunStatusEnum.Draft)
        {
            return run;
        }

        var rates = await _db.ThreePlBillingRates.AsNoTracking()
            .Where(r => r.WarehouseId == request.WarehouseId
                && r.OwnerPartnerId == request.OwnerPartnerId
                && r.IsActive
                && r.EffectiveFrom.Date <= to
                && (!r.EffectiveTo.HasValue || r.EffectiveTo.Value.Date >= from))
            .OrderByDescending(r => r.EffectiveFrom)
            .ThenByDescending(r => r.ThreePlBillingRateId)
            .ToListAsync(ct);

        await AddStorageChargesAsync(run, rates, request.Actor, ct);
        await AddInboundChargesAsync(run, rates, request.Actor, ct);
        await AddOutboundChargesAsync(run, rates, request.Actor, ct);
        await AddPackageChargesAsync(run, rates, request.Actor, ct);
        await AddVasChargesAsync(run, rates, request.Actor, ct);
        await AddYardChargesAsync(run, rates, request.Actor, ct);

        run.TotalAmount = await _db.ThreePlBillingCharges
            .Where(c => c.ThreePlBillingRunId == run.ThreePlBillingRunId && c.Status != ThreePlBillingChargeStatusEnum.Voided)
            .SumAsync(c => (decimal?)c.Amount, ct) ?? 0m;
        run.UpdatedAt = Now;
        await _unitOfWork.SaveChangesAsync(ct);
        return run;
    }

    public async Task<ThreePlBillingRun> ConfirmRunAsync(long runId, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        var run = await LoadRunAsync(runId, scopedWarehouseId, ct);
        if (run.Status != ThreePlBillingRunStatusEnum.Draft)
            return run;

        run.Status = ThreePlBillingRunStatusEnum.Confirmed;
        run.ConfirmedAt = Now;
        run.ConfirmedBy = CleanActor(actor);
        foreach (var charge in run.Charges.Where(c => c.Status == ThreePlBillingChargeStatusEnum.Draft))
            charge.Status = ThreePlBillingChargeStatusEnum.Confirmed;
        await _unitOfWork.SaveChangesAsync(ct);
        return run;
    }

    public async Task<ThreePlBillingRun> VoidRunAsync(long runId, string reason, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new BusinessRuleException("Vui lòng nhập lý do hủy kỳ tính phí.", "THREEPL_VOID_REASON_REQUIRED", "ThreePlBillingRun");

        var run = await LoadRunAsync(runId, scopedWarehouseId, ct);
        if (run.Status == ThreePlBillingRunStatusEnum.Voided)
            return run;

        run.Status = ThreePlBillingRunStatusEnum.Voided;
        run.VoidedAt = Now;
        run.VoidedBy = CleanActor(actor);
        run.VoidReason = reason.Trim();
        foreach (var charge in run.Charges)
            charge.Status = ThreePlBillingChargeStatusEnum.Voided;
        await _unitOfWork.SaveChangesAsync(ct);
        return run;
    }

    private async Task<ThreePlBillingRun> LoadRunAsync(long runId, int? scopedWarehouseId, CancellationToken ct)
    {
        var run = await _db.ThreePlBillingRuns
            .Include(r => r.Charges)
            .FirstOrDefaultAsync(r => r.ThreePlBillingRunId == runId, ct);
        if (run == null)
            throw new BusinessRuleException("Không tìm thấy kỳ tính phí 3PL.", "THREEPL_RUN_NOT_FOUND", "ThreePlBillingRun");
        if (scopedWarehouseId.HasValue && run.WarehouseId != scopedWarehouseId.Value)
            throw new UnauthorizedAccessException("Bạn không có quyền thao tác kỳ tính phí của kho khác.");
        return run;
    }

    private async Task AddStorageChargesAsync(ThreePlBillingRun run, List<ThreePlBillingRate> rates, string actor, CancellationToken ct)
    {
        var rate = PickRate(rates, ThreePlChargeTypeEnum.Storage);
        if (rate == null) return;

        var rows = await _db.ItemLocations.AsNoTracking()
            .Include(il => il.Location)!.ThenInclude(l => l!.Zone)
            .Include(il => il.Item)
            .Where(il => il.OwnerPartnerId == run.OwnerPartnerId
                && il.Quantity > 0
                && il.Location != null
                && il.Location.Zone != null
                && il.Location.Zone.WarehouseId == run.WarehouseId)
            .Select(il => new
            {
                il.ItemLocationId,
                il.ItemId,
                ItemCode = il.Item != null ? il.Item.ItemCode : il.ItemId.ToString(),
                il.Quantity
            })
            .ToListAsync(ct);

        foreach (var row in rows)
            await AddChargeIfMissingAsync(run, rate, ThreePlChargeTypeEnum.Storage, "ItemLocation", row.ItemLocationId.ToString(), row.ItemCode, row.Quantity, actor, ct);
    }

    private async Task AddInboundChargesAsync(ThreePlBillingRun run, List<ThreePlBillingRate> rates, string actor, CancellationToken ct)
    {
        var rate = PickRate(rates, ThreePlChargeTypeEnum.InboundHandling);
        if (rate == null) return;

        var rows = await _db.Vouchers.AsNoTracking()
            .Where(v => v.OwnerPartnerId == run.OwnerPartnerId
                && v.WarehouseId == run.WarehouseId
                && (v.VoucherType == VoucherTypeEnum.NhapKho
                    || v.VoucherType == VoucherTypeEnum.KhachTra
                    || v.VoucherType == VoucherTypeEnum.NhapThanhPham)
                && v.InboundStatus == InboundStatusEnum.Completed
                && v.CompletedAt.HasValue
                && v.CompletedAt.Value.Date >= run.PeriodFrom
                && v.CompletedAt.Value.Date <= run.PeriodTo)
            .Select(v => new { v.VoucherId, v.VoucherCode, Qty = v.Details.Sum(d => Math.Abs(d.BaseQty)) })
            .ToListAsync(ct);

        foreach (var row in rows)
            await AddChargeIfMissingAsync(run, rate, ThreePlChargeTypeEnum.InboundHandling, "Voucher", row.VoucherId.ToString(), row.VoucherCode, row.Qty, actor, ct);
    }

    private async Task AddOutboundChargesAsync(ThreePlBillingRun run, List<ThreePlBillingRate> rates, string actor, CancellationToken ct)
    {
        var rate = PickRate(rates, ThreePlChargeTypeEnum.OutboundHandling);
        if (rate == null) return;

        var rows = await _db.Vouchers.AsNoTracking()
            .Where(v => v.OwnerPartnerId == run.OwnerPartnerId
                && v.WarehouseId == run.WarehouseId
                && (v.VoucherType == VoucherTypeEnum.XuatKho || v.VoucherType == VoucherTypeEnum.TraNCC || v.VoucherType == VoucherTypeEnum.ChuyenKho || v.VoucherType == VoucherTypeEnum.XuatSanXuat)
                && v.IsPosted
                && ((v.CompletedAt.HasValue && v.CompletedAt.Value.Date >= run.PeriodFrom && v.CompletedAt.Value.Date <= run.PeriodTo)
                    || (v.ShippedAt.HasValue && v.ShippedAt.Value.Date >= run.PeriodFrom && v.ShippedAt.Value.Date <= run.PeriodTo)))
            .Select(v => new { v.VoucherId, v.VoucherCode, Qty = v.Details.Sum(d => Math.Abs(d.BaseQty)) })
            .ToListAsync(ct);

        foreach (var row in rows)
            await AddChargeIfMissingAsync(run, rate, ThreePlChargeTypeEnum.OutboundHandling, "Voucher", row.VoucherId.ToString(), row.VoucherCode, row.Qty, actor, ct);
    }

    private async Task AddPackageChargesAsync(ThreePlBillingRun run, List<ThreePlBillingRate> rates, string actor, CancellationToken ct)
    {
        var rate = PickRate(rates, ThreePlChargeTypeEnum.PackageHandling);
        if (rate == null) return;

        var rows = await _db.OutboundPackages.AsNoTracking()
            .Where(p => p.OwnerPartnerId == run.OwnerPartnerId
                && p.WarehouseId == run.WarehouseId
                && p.PackedAt.Date >= run.PeriodFrom
                && p.PackedAt.Date <= run.PeriodTo)
            .Select(p => new { p.OutboundPackageId, p.PackageCode })
            .ToListAsync(ct);

        foreach (var row in rows)
            await AddChargeIfMissingAsync(run, rate, ThreePlChargeTypeEnum.PackageHandling, "OutboundPackage", row.OutboundPackageId.ToString(), row.PackageCode, 1m, actor, ct);
    }

    private async Task AddVasChargesAsync(ThreePlBillingRun run, List<ThreePlBillingRate> rates, string actor, CancellationToken ct)
    {
        var rate = PickRate(rates, ThreePlChargeTypeEnum.Vas);
        if (rate == null) return;

        var rows = await _db.VasWorkOrders.AsNoTracking()
            .Where(v => v.OwnerPartnerId == run.OwnerPartnerId
                && v.WarehouseId == run.WarehouseId
                && v.Status == VasWorkOrderStatusEnum.Completed
                && v.CompletedAt.HasValue
                && v.CompletedAt.Value.Date >= run.PeriodFrom
                && v.CompletedAt.Value.Date <= run.PeriodTo)
            .Select(v => new { v.VasWorkOrderId, v.WorkOrderCode, Qty = v.CompletedQty <= 0 ? 1m : v.CompletedQty })
            .ToListAsync(ct);

        foreach (var row in rows)
            await AddChargeIfMissingAsync(run, rate, ThreePlChargeTypeEnum.Vas, "VasWorkOrder", row.VasWorkOrderId.ToString(), row.WorkOrderCode, row.Qty, actor, ct);
    }

    private async Task AddYardChargesAsync(ThreePlBillingRun run, List<ThreePlBillingRate> rates, string actor, CancellationToken ct)
    {
        var rate = PickRate(rates, ThreePlChargeTypeEnum.Yard);
        if (rate == null) return;

        var rows = await _db.YardBillingCharges.AsNoTracking()
            .Where(y => y.OwnerPartnerId == run.OwnerPartnerId
                && y.WarehouseId == run.WarehouseId
                && y.Status != YardChargeStatusEnum.Waived
                && y.Amount > 0
                && y.CreatedAt.Date >= run.PeriodFrom
                && y.CreatedAt.Date <= run.PeriodTo)
            .Select(y => new { y.YardBillingChargeId, SourceCode = "YARD-" + y.YardBillingChargeId, y.Amount, y.Currency })
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            await AddPassThroughChargeIfMissingAsync(
                run,
                rate,
                ThreePlChargeTypeEnum.Yard,
                "YardBillingCharge",
                row.YardBillingChargeId.ToString(),
                row.SourceCode,
                row.Amount,
                string.IsNullOrWhiteSpace(row.Currency) ? rate.Currency : row.Currency,
                actor,
                ct);
        }
    }

    private async Task AddPassThroughChargeIfMissingAsync(ThreePlBillingRun run, ThreePlBillingRate rate, ThreePlChargeTypeEnum type, string sourceType, string sourceId, string sourceCode, decimal amount, string currency, string actor, CancellationToken ct)
    {
        if (amount <= 0) return;
        var key = $"3pl:{run.ThreePlBillingRunId}:{type}:{sourceType}:{sourceId}";
        var exists = await _db.ThreePlBillingCharges.AnyAsync(c => c.IdempotencyKey == key, ct)
            || _db.ThreePlBillingCharges.Local.Any(c => c.IdempotencyKey == key);
        if (exists) return;

        _db.ThreePlBillingCharges.Add(new ThreePlBillingCharge
        {
            ThreePlBillingRunId = run.ThreePlBillingRunId,
            WarehouseId = run.WarehouseId,
            OwnerPartnerId = run.OwnerPartnerId,
            ThreePlBillingRateId = rate.ThreePlBillingRateId,
            ChargeType = type,
            SourceType = sourceType,
            SourceId = sourceId,
            SourceCode = sourceCode,
            Quantity = 1m,
            ChargeUnit = "charge",
            UnitRate = amount,
            Amount = amount,
            Currency = currency,
            Status = ThreePlBillingChargeStatusEnum.Draft,
            IdempotencyKey = key,
            MetadataJson = JsonSerializer.Serialize(new { run.PeriodFrom, run.PeriodTo, PassThrough = true }),
            CreatedBy = CleanActor(actor),
            CreatedAt = Now
        });
    }

    private async Task AddChargeIfMissingAsync(ThreePlBillingRun run, ThreePlBillingRate rate, ThreePlChargeTypeEnum type, string sourceType, string sourceId, string sourceCode, decimal quantity, string actor, CancellationToken ct)
    {
        if (quantity <= 0) return;
        var key = $"3pl:{run.ThreePlBillingRunId}:{type}:{sourceType}:{sourceId}";
        var exists = await _db.ThreePlBillingCharges.AnyAsync(c => c.IdempotencyKey == key, ct)
            || _db.ThreePlBillingCharges.Local.Any(c => c.IdempotencyKey == key);
        if (exists) return;

        _db.ThreePlBillingCharges.Add(new ThreePlBillingCharge
        {
            ThreePlBillingRunId = run.ThreePlBillingRunId,
            WarehouseId = run.WarehouseId,
            OwnerPartnerId = run.OwnerPartnerId,
            ThreePlBillingRateId = rate.ThreePlBillingRateId,
            ChargeType = type,
            SourceType = sourceType,
            SourceId = sourceId,
            SourceCode = sourceCode,
            Quantity = quantity,
            ChargeUnit = rate.ChargeUnit,
            UnitRate = rate.UnitRate,
            Amount = Math.Round(quantity * rate.UnitRate, 4, MidpointRounding.AwayFromZero),
            Currency = rate.Currency,
            Status = ThreePlBillingChargeStatusEnum.Draft,
            IdempotencyKey = key,
            MetadataJson = JsonSerializer.Serialize(new { run.PeriodFrom, run.PeriodTo }),
            CreatedBy = CleanActor(actor),
            CreatedAt = Now
        });
    }

    private static ThreePlBillingRate? PickRate(IEnumerable<ThreePlBillingRate> rates, ThreePlChargeTypeEnum type)
        => rates.Where(r => r.ChargeType == type).OrderByDescending(r => r.EffectiveFrom).FirstOrDefault();

    private static string BuildRunKey(int warehouseId, int ownerPartnerId, DateTime from, DateTime to)
        => $"3pl:run:{warehouseId}:{ownerPartnerId}:{from:yyyyMMdd}:{to:yyyyMMdd}";

    private async Task<string> GenerateRunCodeAsync(int warehouseId, int ownerPartnerId, DateTime from, CancellationToken ct)
    {
        var prefix = $"3PL-{from:yyyyMM}-{warehouseId}-{ownerPartnerId}-";
        var count = await _db.ThreePlBillingRuns.CountAsync(r => r.RunCode.StartsWith(prefix), ct);
        return $"{prefix}{count + 1:0000}";
    }

    private static string CleanActor(string? actor)
        => string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim()[..Math.Min(actor.Trim().Length, 100)];
}
