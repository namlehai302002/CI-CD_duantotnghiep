using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WMS.Common;
using WMS.Models;
using WMS.Services;

namespace WMS.Controllers;

public partial class VouchersController
{
    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CaptureCatchWeight(
        long voucherId,
        long voucherDetailId,
        decimal baseQuantity,
        decimal actualWeight,
        int? weightUomId,
        CatchWeightCapturePointEnum capturePoint = CatchWeightCapturePointEnum.Receive,
        long? outboundPackageId = null,
        long? licensePlateId = null,
        long? licensePlateDetailId = null,
        long? pickTaskId = null,
        long? serialNumberId = null)
    {
        var detail = await _db.VoucherDetails
            .Include(d => d.Voucher)
            .Include(d => d.Item)
            .FirstOrDefaultAsync(d => d.VoucherDetailId == voucherDetailId && d.VoucherId == voucherId);
        if (detail == null || detail.Voucher == null)
        {
            TempData["Error"] = "Không tìm thấy dòng phiếu cần ghi nhận cân trọng lượng thực tế.";
            return RedirectToAction("Details", new { id = voucherId });
        }

        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && detail.Voucher.WarehouseId != scopedWh.Value)
            return Forbid();

        try
        {
            await _catchWeightService.CaptureAsync(new CatchWeightCaptureRequest
            {
                ItemId = detail.ItemId,
                WarehouseId = detail.Voucher.WarehouseId,
                VoucherId = detail.VoucherId,
                VoucherDetailId = detail.VoucherDetailId,
                LicensePlateId = licensePlateId,
                LicensePlateDetailId = licensePlateDetailId,
                OutboundPackageId = outboundPackageId,
                PickTaskId = pickTaskId,
                SerialNumberId = serialNumberId,
                BaseQuantity = baseQuantity,
                ActualWeight = actualWeight,
                WeightUomId = weightUomId,
                CapturePoint = capturePoint,
                CapturedBy = User.Identity?.Name ?? "system",
                IdempotencyKey = BuildCatchWeightIdempotencyKey(
                    voucherId,
                    voucherDetailId,
                    capturePoint,
                    outboundPackageId,
                    licensePlateId,
                    licensePlateDetailId,
                    pickTaskId,
                    serialNumberId,
                    baseQuantity,
                    actualWeight,
                    weightUomId)
            });
            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = "Đã ghi nhận cân trọng lượng thực tế.";
        }
        catch (BusinessRuleException ex)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }

        return RedirectToAction("Details", new { id = voucherId });
    }

    private static string BuildCatchWeightIdempotencyKey(
        long voucherId,
        long voucherDetailId,
        CatchWeightCapturePointEnum capturePoint,
        long? outboundPackageId,
        long? licensePlateId,
        long? licensePlateDetailId,
        long? pickTaskId,
        long? serialNumberId,
        decimal baseQuantity,
        decimal actualWeight,
        int? weightUomId)
    {
        static string LongToken(long? value) => value.HasValue
            ? value.Value.ToString(CultureInfo.InvariantCulture)
            : "na";
        static string IntToken(int? value) => value.HasValue
            ? value.Value.ToString(CultureInfo.InvariantCulture)
            : "na";
        static string DecimalToken(decimal value) => value.ToString("0.####", CultureInfo.InvariantCulture);

        return string.Join(':',
            "cw",
            $"v{voucherId.ToString(CultureInfo.InvariantCulture)}",
            $"d{voucherDetailId.ToString(CultureInfo.InvariantCulture)}",
            $"p{capturePoint}",
            $"pkg{LongToken(outboundPackageId)}",
            $"lpn{LongToken(licensePlateId)}",
            $"lpnd{LongToken(licensePlateDetailId)}",
            $"pt{LongToken(pickTaskId)}",
            $"sn{LongToken(serialNumberId)}",
            $"q{DecimalToken(baseQuantity)}",
            $"w{DecimalToken(actualWeight)}",
            $"u{IntToken(weightUomId)}");
    }
}
