using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public sealed class CatchWeightCaptureRequest
{
    public int ItemId { get; init; }
    public int WarehouseId { get; init; }
    public long? VoucherId { get; init; }
    public long? VoucherDetailId { get; init; }
    public long? LicensePlateId { get; init; }
    public long? LicensePlateDetailId { get; init; }
    public long? OutboundPackageId { get; init; }
    public long? PickTaskId { get; init; }
    public long? SerialNumberId { get; init; }
    public decimal BaseQuantity { get; init; }
    public decimal ActualWeight { get; init; }
    public int? WeightUomId { get; init; }
    public CatchWeightCapturePointEnum CapturePoint { get; init; } = CatchWeightCapturePointEnum.Receive;
    public string CapturedBy { get; init; } = "system";
    public string IdempotencyKey { get; init; } = "";
    public string? MetadataJson { get; init; }
}

public interface ICatchWeightService
{
    Task<CatchWeightEntry> CaptureAsync(CatchWeightCaptureRequest request, CancellationToken ct = default);
    Task RequireInboundCatchWeightAsync(Voucher voucher, CancellationToken ct = default);
    Task RequirePackageCatchWeightAsync(long voucherId, CancellationToken ct = default);
    Task<decimal> GetVoucherActualWeightAsync(long voucherId, CancellationToken ct = default);
}

public class CatchWeightService : ICatchWeightService
{
    private const decimal QuantityTolerance = 0.0001m;
    private readonly AppDbContext _db;

    public CatchWeightService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CatchWeightEntry> CaptureAsync(CatchWeightCaptureRequest request, CancellationToken ct = default)
    {
        var idempotencyKey = NormalizeRequired(request.IdempotencyKey, 160, "catch-weight idempotency key");
        var existing = await _db.CatchWeightEntries.FirstOrDefaultAsync(e => e.IdempotencyKey == idempotencyKey, ct);
        if (existing != null)
            return existing;

        var local = _db.CatchWeightEntries.Local.FirstOrDefault(e => e.IdempotencyKey == idempotencyKey);
        if (local != null)
            return local;

        if (request.BaseQuantity <= 0)
            throw new BusinessRuleException("Số lượng theo đơn vị gốc để cân bắt buộc phải lớn hơn 0.", "CATCH_WEIGHT_BASE_QTY_INVALID", "CatchWeightEntry");
        if (request.ActualWeight <= 0)
            throw new BusinessRuleException("Trọng lượng thực tế bắt buộc phải lớn hơn 0.", "CATCH_WEIGHT_ACTUAL_INVALID", "CatchWeightEntry");

        var item = await _db.Items.FirstOrDefaultAsync(i => i.ItemId == request.ItemId, ct);
        if (item == null)
            throw WmsExceptions.ItemNotFound(request.ItemId);
        if (!item.TrackCatchWeight)
            throw new BusinessRuleException($"Vật tư [{item.ItemCode}] chưa bật cân trọng lượng thực tế.", "CATCH_WEIGHT_NOT_ENABLED", "Item");

        var weightUomId = request.WeightUomId ?? item.CatchWeightUomId;
        if (!weightUomId.HasValue)
            throw new BusinessRuleException($"Vật tư [{item.ItemCode}] chưa cấu hình đơn vị cân trọng lượng thực tế.", "CATCH_WEIGHT_UOM_REQUIRED", "Item");

        // P1-R2-2: UoM của lần ghi phải khớp UoM cấu hình của item — nếu khác (kg vs g) tolerance check sai hệ.
        if (item.CatchWeightUomId.HasValue && weightUomId.Value != item.CatchWeightUomId.Value)
            throw new BusinessRuleException(
                $"Đơn vị cân của lần ghi không khớp với UoM cấu hình của [{item.ItemCode}].",
                "CATCH_WEIGHT_UOM_MISMATCH", "CatchWeightEntry");

        ValidateTolerance(item, request.BaseQuantity, request.ActualWeight);

        var entry = new CatchWeightEntry
        {
            ItemId = request.ItemId,
            WarehouseId = request.WarehouseId,
            VoucherId = request.VoucherId,
            VoucherDetailId = request.VoucherDetailId,
            LicensePlateId = request.LicensePlateId,
            LicensePlateDetailId = request.LicensePlateDetailId,
            OutboundPackageId = request.OutboundPackageId,
            PickTaskId = request.PickTaskId,
            SerialNumberId = request.SerialNumberId,
            BaseQuantity = request.BaseQuantity,
            ActualWeight = request.ActualWeight,
            WeightUomId = weightUomId.Value,
            CapturePoint = request.CapturePoint,
            Status = CatchWeightStatusEnum.Captured,
            CapturedBy = NormalizeOptional(request.CapturedBy, 100) ?? "system",
            CapturedAt = VietnamTime.Now,
            IdempotencyKey = idempotencyKey,
            MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson
        };

        _db.CatchWeightEntries.Add(entry);
        await UpdatePackageWeightAsync(request.OutboundPackageId, weightUomId.Value, entry, ct);
        return entry;
    }

    public async Task RequireInboundCatchWeightAsync(Voucher voucher, CancellationToken ct = default)
    {
        var details = await LoadDetailsAsync(voucher, ct);
        foreach (var detail in details.Where(d => d.Item?.TrackCatchWeight == true && d.Item.RequireCatchWeightAtReceive))
        {
            var expectedBaseQty = GetGoodBaseQty(detail, voucher);
            if (expectedBaseQty <= QuantityTolerance)
                continue;

            var capturedBaseQty = await _db.CatchWeightEntries
                .Where(e => e.Status == CatchWeightStatusEnum.Captured
                    && e.VoucherDetailId == detail.VoucherDetailId
                    && (e.CapturePoint == CatchWeightCapturePointEnum.Receive || e.CapturePoint == CatchWeightCapturePointEnum.Putaway))
                .SumAsync(e => (decimal?)e.BaseQuantity, ct) ?? 0m;

            if (capturedBaseQty + QuantityTolerance < expectedBaseQty)
            {
                var itemCode = detail.Item?.ItemCode ?? detail.ItemId.ToString();
                throw new BusinessRuleException(
                    $"[{itemCode}] bật cân trọng lượng thực tế nên phải ghi nhận trọng lượng thực tế trước khi hoàn tất nhập. Cần {expectedBaseQty:N4}, đã cân {capturedBaseQty:N4}.",
                    "CATCH_WEIGHT_RECEIVE_REQUIRED",
                    "CatchWeightEntry");
            }
        }
    }

    public async Task RequirePackageCatchWeightAsync(long voucherId, CancellationToken ct = default)
    {
        var voucher = await _db.Vouchers
            .Include(v => v.Details).ThenInclude(d => d.Item)
            .FirstOrDefaultAsync(v => v.VoucherId == voucherId, ct);
        if (voucher == null)
            throw WmsExceptions.VoucherNotFound();

        var requiresCatchWeight = voucher.Details.Any(d => d.Item?.TrackCatchWeight == true && d.Item.RequireCatchWeightAtPickPack);
        if (!requiresCatchWeight)
            return;

        var packages = await _db.OutboundPackages
            .Where(p => p.VoucherId == voucherId)
            .ToListAsync(ct);
        if (packages.Count == 0)
            throw new BusinessRuleException("Phiếu có hàng Catch Weight nhưng chưa có kiện đóng gói để ghi nhận trọng lượng.", "CATCH_WEIGHT_PACKAGE_MISSING", "OutboundPackage");

        foreach (var package in packages)
        {
            var hasWeight = package.ActualCatchWeight.HasValue && package.ActualCatchWeight.Value > 0;
            if (!hasWeight)
            {
                hasWeight = await _db.CatchWeightEntries.AnyAsync(e =>
                    e.OutboundPackageId == package.OutboundPackageId
                    && e.Status == CatchWeightStatusEnum.Captured
                    && e.ActualWeight > 0, ct);
            }

            if (!hasWeight)
                throw new BusinessRuleException($"Kiện [{package.PackageCode}] chứa hàng Catch Weight nhưng chưa ghi nhận trọng lượng thực tế.", "CATCH_WEIGHT_PACK_REQUIRED", "OutboundPackage");
        }
    }

    public async Task<decimal> GetVoucherActualWeightAsync(long voucherId, CancellationToken ct = default)
        => await _db.CatchWeightEntries
            .Where(e => e.VoucherId == voucherId && e.Status == CatchWeightStatusEnum.Captured)
            .SumAsync(e => (decimal?)e.ActualWeight, ct) ?? 0m;

    private async Task UpdatePackageWeightAsync(long? outboundPackageId, int weightUomId, CatchWeightEntry pendingEntry, CancellationToken ct)
    {
        if (!outboundPackageId.HasValue)
            return;

        var package = await _db.OutboundPackages.FirstOrDefaultAsync(p => p.OutboundPackageId == outboundPackageId.Value, ct);
        if (package == null)
            throw new BusinessRuleException("Không tìm thấy kiện xuất để ghi nhận Catch Weight.", "OUTBOUND_PACKAGE_NOT_FOUND", "OutboundPackage");

        var persistedWeight = await _db.CatchWeightEntries
            .Where(e => e.OutboundPackageId == outboundPackageId.Value && e.Status == CatchWeightStatusEnum.Captured)
            .SumAsync(e => (decimal?)e.ActualWeight, ct) ?? 0m;

        var localWeight = _db.CatchWeightEntries.Local
            .Where(e => e != pendingEntry && e.OutboundPackageId == outboundPackageId.Value && e.Status == CatchWeightStatusEnum.Captured)
            .Sum(e => e.ActualWeight);

        package.ActualCatchWeight = persistedWeight + localWeight + pendingEntry.ActualWeight;
        package.CatchWeightUomId = weightUomId;
    }

    private async Task<List<VoucherDetail>> LoadDetailsAsync(Voucher voucher, CancellationToken ct)
    {
        if (voucher.Details.Any(d => d.Item != null))
            return voucher.Details.ToList();

        return await _db.VoucherDetails
            .Include(d => d.Item)
            .Where(d => d.VoucherId == voucher.VoucherId)
            .ToListAsync(ct);
    }

    private static decimal GetGoodBaseQty(VoucherDetail detail, Voucher voucher)
    {
        if (!voucher.IsInboundFlow)
            return Math.Max(0, detail.BaseQty);

        var defectBase = detail.DefectBaseQty > 0
            ? detail.DefectBaseQty
            : detail.DefectQty * (detail.ConversionRate == 0 ? 1 : Math.Abs(detail.ConversionRate));
        return Math.Max(0, detail.BaseQty - Math.Max(0, defectBase));
    }

    private static void ValidateTolerance(Item item, decimal baseQuantity, decimal actualWeight)
    {
        if (!item.NominalWeightPerBaseUnit.HasValue || item.NominalWeightPerBaseUnit.Value <= 0)
            return;

        var tolerancePercent = item.CatchWeightTolerancePercent ?? 0m;
        if (tolerancePercent < 0)
            throw new BusinessRuleException("Dung sai Catch Weight không được âm.", "CATCH_WEIGHT_TOLERANCE_INVALID", "Item");

        var expected = item.NominalWeightPerBaseUnit.Value * baseQuantity;
        var tolerance = expected * tolerancePercent / 100m;
        var min = expected - tolerance;
        var max = expected + tolerance;
        if (actualWeight < min || actualWeight > max)
        {
            throw new BusinessRuleException(
                $"Trọng lượng thực tế của [{item.ItemCode}] ngoài dung sai. Kỳ vọng {expected:N4}, cho phép {min:N4} - {max:N4}.",
                "CATCH_WEIGHT_TOLERANCE_EXCEEDED",
                "CatchWeightEntry");
        }
    }

    private static string NormalizeRequired(string value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new BusinessRuleException($"Thiếu {fieldName}.", "CATCH_WEIGHT_IDEMPOTENCY_REQUIRED", "CatchWeightEntry");
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
