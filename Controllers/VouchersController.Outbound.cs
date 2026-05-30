using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Http;

using Microsoft.EntityFrameworkCore;

using WMS.Data;

using WMS.Models;

using WMS.ViewModels;

using WMS.Authorization;

using WMS.Common;

using WMS.Services;

using System.Text.Json;

using System.Linq;

using ClosedXML.Excel;

using System.Globalization;

using System.Data;

using Microsoft.Extensions.Logging.Abstractions;

namespace WMS.Controllers;

public partial class VouchersController
{

    // ═══ P0.2 — Multi-Order Wave Planning Engine ═══

    /// <summary>
    /// GET: Hiển thị giao diện lập kế hoạch sóng đa đơn.
    /// Cho phép chọn nhiều phiếu xuất, nhóm theo carrier/zone/route,
    /// gán profile sóng và phát hành một lần cho toàn bộ.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> WavePlanning(int? warehouseId)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.WarehouseCode).ToListAsync();
        ViewBag.WarehouseId = warehouseId;

        // Lấy các phiếu xuất ở trạng thái chờ pick, chưa có wave, để người dùng chọn
        var query = _db.Vouchers.AsNoTracking()
            .Include(v => v.Partner)
            .Include(v => v.Details).ThenInclude(d => d.Item)
            .Where(v => v.VoucherType == VoucherTypeEnum.XuatKho
                && !v.IsCancelled
                && !v.IsPosted
                && v.WaveId == null
                && v.FulfillmentStatus < FulfillmentStatusEnum.WaitingForPick);

        if (warehouseId.HasValue)
            query = query.Where(v => v.WarehouseId == warehouseId.Value);

        var pendingVouchers = await query
            .OrderBy(v => v.VoucherDate)
            .Take(200)
            .ToListAsync();

        // Nhóm theo carrier/partner để gợi ý
        var carrierGroups = pendingVouchers
            .GroupBy(v => v.Partner?.PartnerCode ?? "NO_CARRIER")
            .Select(g => new
            {
                CarrierCode = g.Key,
                CarrierName = g.First().Partner?.PartnerName ?? "—",
                Count = g.Count(),
                Items = g.Sum(v => v.Details.Sum(d => Math.Abs(d.BaseQty)))
            })
            .OrderByDescending(c => c.Count)
            .ToList();

        ViewBag.CarrierGroups = carrierGroups;
        return View(pendingVouchers);
    }


    /// <summary>
    /// POST: Tạo và phát hành sóng cho nhiều phiếu xuất cùng lúc.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.VoucherReleasePicking)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateWave(string waveProfile, string? carrierCode, string? carrierName,
        string? routeCode, DateTime? cutoffTime, int priority, long[] selectedVoucherIds, string? notes)
    {
        var scopedWh = GetScopedWarehouseId();
        var priorityEnum = priority switch
        {
            1 => WavePriorityEnum.Low,
            2 => WavePriorityEnum.Normal,
            3 => WavePriorityEnum.High,
            4 => WavePriorityEnum.Urgent,
            _ => WavePriorityEnum.Normal
        };
        var actor = User.Identity?.Name ?? "system";

        try
        {
            var result = await _outboundExecutionService.CreateWaveAsync(
                waveProfile, carrierCode, carrierName, routeCode, cutoffTime, priorityEnum,
                selectedVoucherIds, notes, scopedWh, actor);

            if (result.Succeeded)
            {
                TempData["Success"] = result.Message;
                if (result.RedirectRouteValues != null)
                    return RedirectToAction(result.RedirectAction, result.RedirectRouteValues);
                return RedirectToAction(result.RedirectAction);
            }
            else
            {
                TempData["Error"] = result.Message;
                if (result.RedirectRouteValues != null)
                    return RedirectToAction(result.RedirectAction, result.RedirectRouteValues);
                return RedirectToAction(result.RedirectAction ?? nameof(WavePlanning));
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = UserSafeError.WithPrefix(ex, "Tạo sóng thất bại", "Không thể tạo sóng lấy hàng lúc này. Vui lòng thử lại.");
            return RedirectToAction(nameof(WavePlanning));
        }
    }


    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.VoucherReleasePicking)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReleaseDirect(long id)
    {
        try
        {
            var result = await _orderStreamingService.ReleaseNowAsync(id, User.Identity?.Name ?? "system", GetScopedWarehouseId());
            if (result.Forbidden) return Forbid();
            if (result.NotFound) return NotFound();

            if (result.Succeeded)
                TempData["Success"] = result.Message;
            else
                TempData["Error"] = result.Message;

            if (result.RedirectRouteValues != null)
                return RedirectToAction(result.RedirectAction ?? "Details", result.RedirectRouteValues);
            return RedirectToAction("Details", new { id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = UserSafeError.WithPrefix(ex, "Không thể phát hành trực tiếp", "Không thể phát hành trực tiếp lúc này. Vui lòng thử lại.");
            return RedirectToAction("Details", new { id });
        }
    }


    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.VoucherReleasePicking)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmForPicking(long id)
    {
        try
        {
            var scopedWh = GetScopedWarehouseId();
            var voucher = await _db.Vouchers.AsNoTracking().FirstOrDefaultAsync(v => v.VoucherId == id);
            if (voucher == null)
            {
                TempData["Error"] = "Không tìm thấy phiếu.";
                return RedirectToAction("Details", new { id });
            }
            if (voucher.IsCancelled)
            {
                TempData["Error"] = "Phiếu đã hủy.";
                return RedirectToAction("Details", new { id });
            }
            if (voucher.IsPosted)
            {
                TempData["Error"] = "Phiếu đã ghi sổ.";
                return RedirectToAction("Details", new { id });
            }
            if (voucher.VoucherType is not (VoucherTypeEnum.XuatKho or VoucherTypeEnum.TraNCC or VoucherTypeEnum.ChuyenKho or VoucherTypeEnum.XuatSanXuat))
            {
                TempData["Error"] = "Chỉ áp dụng cho phiếu xuất/chuyển kho.";
                return RedirectToAction("Details", new { id });
            }
            if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value)
                return Forbid();
            var lockDate = await GetActiveLockDateAsync(voucher.WarehouseId);
            var transactionDate = ResolveLockTransactionDate(voucher, VietnamNow);
            if (IsLocked(transactionDate, lockDate))
                throw WmsExceptions.WarehouseLockedForPost(transactionDate.ToString("dd/MM/yyyy"), lockDate!.Value);

            var result = await _outboundExecutionService.ReleaseVoucherForPickingAsync(id, scopedWh, User.Identity?.Name ?? "system");
            if (result.Forbidden) return Forbid();
            if (result.NotFound)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction("Details", new { id });
            }
            if (result.Succeeded)
                TempData["Success"] = result.Message;
            else
                TempData["Error"] = result.Message;

            if (result.RedirectRouteValues != null)
                return RedirectToAction(result.RedirectAction ?? "Details", result.RedirectRouteValues);
            return RedirectToAction(result.RedirectAction ?? "Details", new { id });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict when releasing picking. VoucherId={VoucherId}, Actor={Actor}", id, User.Identity?.Name);
            TempData["Error"] = "Dữ liệu đã thay đổi bởi phiên khác trong lúc phát hành đợt lấy hàng. Vui lòng tải lại phiếu và thao tác lại.";
            return RedirectToAction("Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wave release failed. VoucherId={VoucherId}, Actor={Actor}, WarehouseScope={WarehouseScope}", id, User.Identity?.Name, GetScopedWarehouseId());
            TempData["Error"] = UserSafeError.WithPrefix(ex, "Không thể phát hành đợt lấy hàng", "Không thể phát hành đợt lấy hàng lúc này. Vui lòng thử lại.");
            return RedirectToAction("Details", new { id });
        }
    }


    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPickTask(long id, decimal qty, string? scanValue, string? serialInput, string? toteCode = null, string? sourceLocationCode = null, string? targetLocationCode = null, bool reportShort = false, decimal? queuedBaselinePickedQty = null)
    {
        const decimal qtyTolerance = 0.0001m;
        var queued = QueuedOperationResponse.IsQueued(this);
        var referer = Request.Headers.Referer.ToString();
        string GetFallbackRedirectUrl()
        {
            if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
            {
                var localUrl = $"{refererUri.PathAndQuery}{refererUri.Fragment}";
                if (Url?.IsLocalUrl(localUrl) == true) return localUrl;
            }
            return Url?.Action(nameof(Index)) ?? "/Vouchers";
        }

        string ResolveRedirectUrl(WorkflowResult result)
        {
            if (string.IsNullOrWhiteSpace(result.RedirectAction))
                return GetFallbackRedirectUrl();

            return result.RedirectRouteValues != null
                ? Url?.Action(result.RedirectAction, result.RedirectRouteValues) ?? GetFallbackRedirectUrl()
                : Url?.Action(result.RedirectAction) ?? GetFallbackRedirectUrl();
        }

        var actor = User.Identity?.Name ?? "system";
        var canOverrideAssignee = User.IsInRole("Admin") || User.IsInRole("Manager");
        var serialCodes = ParseSerialCodes(serialInput);

        if (queued)
        {
            var taskState = await _db.PickTasks.AsNoTracking()
                .FirstOrDefaultAsync(t => t.PickTaskId == id);
            if (taskState == null)
                return QueuedOperationResponse.Json(this, false, "Không tìm thấy nhiệm vụ lấy hàng.", null, StatusCodes.Status404NotFound, "NOT_FOUND");

            var baselineSatisfied = queuedBaselinePickedQty.HasValue
                && taskState.PickedQty + qtyTolerance >= queuedBaselinePickedQty.Value + qty;
            if (taskState.Status is PickTaskStatusEnum.Completed or PickTaskStatusEnum.Short || baselineSatisfied)
                return QueuedOperationResponse.Json(this, true, $"Nhiệm vụ {taskState.TaskCode} đã được xác nhận trước đó.", GetFallbackRedirectUrl());
        }

        try
        {
            var result = await _outboundExecutionService.ConfirmPickTaskAsync(
                id, qty, scanValue ?? string.Empty, serialCodes, actor, canOverrideAssignee, toteCode, sourceLocationCode, targetLocationCode, reportShort);

            if (queued)
            {
                if (result.NotFound)
                    return QueuedOperationResponse.Json(this, false, result.Message ?? "Không tìm thấy nhiệm vụ lấy hàng.", null, StatusCodes.Status404NotFound, "NOT_FOUND");
                if (result.Forbidden)
                    return QueuedOperationResponse.Json(this, false, "Bạn không có quyền xác nhận nhiệm vụ này.", null, StatusCodes.Status403Forbidden, "FORBIDDEN");

                return QueuedOperationResponse.Json(
                    this,
                    result.Succeeded,
                    result.Message ?? (result.Succeeded ? "Đã xác nhận nhiệm vụ lấy hàng." : "Không thể xác nhận nhiệm vụ lấy hàng."),
                    ResolveRedirectUrl(result),
                    result.Succeeded ? StatusCodes.Status200OK : StatusCodes.Status422UnprocessableEntity,
                    result.Succeeded ? null : "BUSINESS_RULE");
            }

            if (result.Succeeded)
            {
                if (!string.IsNullOrWhiteSpace(result.Message))
                    TempData["Success"] = result.Message;
                else
                    TempData["Info"] = "Nhiệm vụ đã đủ số lượng cần lấy.";

                if (result.RedirectRouteValues != null)
                    return RedirectToAction(result.RedirectAction, result.RedirectRouteValues);
                return RedirectToAction(result.RedirectAction);
            }
            else
            {
                TempData["Error"] = result.Message;
                if (result.RedirectRouteValues != null)
                    return RedirectToAction(result.RedirectAction, result.RedirectRouteValues);
                return Redirect(GetFallbackRedirectUrl());
            }
        }
        catch (Exception ex)
        {
            var safeMessage = UserSafeError.WithPrefix(ex, "Quét xác nhận lấy hàng thất bại", "Không thể xác nhận lấy hàng lúc này. Vui lòng thử lại.");
            if (queued)
                return QueuedOperationResponse.Json(this, false, safeMessage, GetFallbackRedirectUrl(), StatusCodes.Status422UnprocessableEntity, "BUSINESS_RULE");

            TempData["Error"] = safeMessage;
            return Redirect(GetFallbackRedirectUrl());
        }
    }


    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.VoucherPostOutbound)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostReservedOutbound(long id, bool cancelRemaining = false)
    {
        try
        {
            // P1.1 — SoD enforcement: người tạo phiếu không được ghi sổ xuất
            var voucherForSod = await _db.Vouchers.AsNoTracking()
                .FirstOrDefaultAsync(v => v.VoucherId == id);
            if (voucherForSod != null)
                EnforceSod(voucherForSod.CreatedBy, WmsPermissions.VoucherPostOutbound);

            // Lock-period check (stays in controller: needs HttpContext/lock queries)
            if (voucherForSod != null)
            {
                var lockDate = await GetActiveLockDateAsync(voucherForSod.WarehouseId);
                var transactionDate = ResolveLockTransactionDate(voucherForSod, VietnamNow);
                if (IsLocked(transactionDate, lockDate))
                    throw WmsExceptions.WarehouseLockedForPost(transactionDate.ToString("dd/MM/yyyy"), lockDate!.Value);
            }

            // QC disposition gate (stays in controller: lightweight pre-check before service)
            if (voucherForSod != null)
            {
                var blockedDetails = await _db.VoucherDetails
                    .Include(d => d.Item)
                    .Where(d => d.VoucherId == id
                        && (d.QualityStatus == QualityStatusEnum.OnHold || d.QualityStatus == QualityStatusEnum.Defect))
                    .Select(d => new { d.ItemId, d.QualityStatus, ItemCode = d.Item != null ? d.Item.ItemCode : d.ItemId.ToString() })
                    .ToListAsync();
                if (blockedDetails.Count > 0)
                {
                    var blocked = string.Join("; ", blockedDetails.Select(d => $"{d.ItemCode} (Status: {d.QualityStatus})"));
                    throw WmsExceptions.QcHoldBlocked(blocked);
                }
            }

            // P0-04/P0-05: Delegate to service (single transaction boundary inside service)
            var scopedWh = GetScopedWarehouseId();
            var actor = User.Identity?.Name ?? "system";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _outboundExecutionService.PostReservedOutboundAsync(id, cancelRemaining, scopedWh, actor, ip);

            // Map WorkflowResult to IActionResult
            if (result.Forbidden) return Forbid();
            if (result.NotFound) return NotFound();
            if (result.Warning != null) TempData["Warning"] = result.Warning;
            if (result.Succeeded)
                TempData["Success"] = result.Message;
            else
                TempData["Error"] = result.Message;
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "Dữ liệu tồn kho đã bị thay đổi bởi người khác. Vui lòng tải lại trang và thử lại.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = UserSafeError.WithPrefix(ex, "Ghi sổ phiếu xuất thất bại", "Không thể ghi sổ phiếu xuất lúc này. Vui lòng thử lại.");
        }

        return RedirectToAction("Details", new { id });
    }


    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Authorize(Policy = WmsPermissions.VoucherCancel)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(long id, string? cancelReason, CancelReasonEnum? cancelReasonCode)
    {
        var actor = User.Identity?.Name ?? "system";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var scopedWh = GetScopedWarehouseId();

        try
        {
            var voucher = await _db.Vouchers.AsNoTracking().FirstOrDefaultAsync(v => v.VoucherId == id);
            DateTime? lockDate = null;
            if (voucher != null)
            {
                lockDate = await GetActiveLockDateAsync(voucher.WarehouseId);
            }

            var result = await _cancellationService.CancelVoucherAsync(
                id, cancelReason, cancelReasonCode, scopedWh, actor, ip, lockDate);

            if (result.Succeeded)
            {
                TempData["Success"] = result.Message;
                if (result.RedirectRouteValues != null)
                    return RedirectToAction(result.RedirectAction, result.RedirectRouteValues);
                return RedirectToAction(result.RedirectAction);
            }
            else
            {
                TempData["Error"] = result.Message;
                if (result.RedirectRouteValues != null)
                    return RedirectToAction(result.RedirectAction, result.RedirectRouteValues);
                return RedirectToAction("Details", new { id });
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "Dữ liệu tồn kho đã bị thay đổi bởi người khác. Vui lòng tải lại trang và thử lại.";
            return RedirectToAction("Details", new { id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = UserSafeError.WithPrefix(ex, "Lỗi hủy phiếu", "Không thể hủy phiếu lúc này. Vui lòng thử lại.");
            return RedirectToAction("Details", new { id });
        }
    }


    // ═══════════════════════════════════════════════════════════════
    // OUT-04: XÁC NHẬN ĐÓNG GÓI (Packing Confirmation)
    // ═══════════════════════════════════════════════════════════════
    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPacking(long id, int? manualPackageCount, string? packageType, string? packageCodes, string? lpnCodes, string? packingNote, decimal? actualCatchWeight = null, int? catchWeightUomId = null)
    {
        var voucher = await _db.Vouchers
            .Include(v => v.Details)
            .ThenInclude(d => d.Item)
            .FirstOrDefaultAsync(v => v.VoucherId == id);
        if (voucher == null) return NotFound();
        if (voucher.IsCancelled) { TempData["Error"] = "Phiếu đã hủy."; return RedirectToAction("Details", new { id }); }

        var isOutbound = voucher.VoucherType is VoucherTypeEnum.XuatKho or VoucherTypeEnum.TraNCC or VoucherTypeEnum.ChuyenKho or VoucherTypeEnum.XuatSanXuat;
        if (!isOutbound) { TempData["Error"] = "Chỉ áp dụng cho phiếu xuất/chuyển kho."; return RedirectToAction("Details", new { id }); }
        if (!voucher.IsPosted) { TempData["Error"] = "Phiếu chưa hoàn tất xuất kho, không thể đóng gói."; return RedirectToAction("Details", new { id }); }
        if (voucher.PackedAt.HasValue) { TempData["Error"] = "Phiếu đã được xác nhận đóng gói."; return RedirectToAction("Details", new { id }); }

        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value) return Forbid();

        var actor = User.Identity?.Name ?? "system";
        var normalizedPackageType = NormalizeText(packageType);
        var normalizedPackingNote = NormalizeText(packingNote);
        var requestedManualCount = Math.Max(0, manualPackageCount ?? 0);
        var inputPackageCodes = ParseSerialCodes(packageCodes);
        var scannedLpnCodes = ParseSerialCodes(lpnCodes);

        if (requestedManualCount == 0 && scannedLpnCodes.Count == 0)
            requestedManualCount = 1;

        if (inputPackageCodes.Count > 0 && inputPackageCodes.Count != requestedManualCount)
        {
            TempData["Error"] = "Số mã kiện nhập tay phải khớp với số kiện thủ công cần tạo.";
            return RedirectToAction("Details", new { id });
        }

        var existingPackages = await _db.OutboundPackages
            .Where(p => p.VoucherId == voucher.VoucherId)
            .ToListAsync();
        if (existingPackages.Count > 0)
        {
            TempData["Error"] = $"Phiếu đã có {existingPackages.Count} kiện xuất, không thể xác nhận đóng gói lần nữa.";
            return RedirectToAction("Details", new { id });
        }

        var ownerMismatchDetail = voucher.Details.FirstOrDefault(d => d.OwnerPartnerId.HasValue && d.OwnerPartnerId != voucher.OwnerPartnerId);
        if (ownerMismatchDetail != null)
        {
            TempData["Error"] = "Dòng phiếu không cùng chủ hàng với phiếu xuất, không thể đóng gói.";
            return RedirectToAction("Details", new { id });
        }
        foreach (var detail in voucher.Details)
            detail.OwnerPartnerId ??= voucher.OwnerPartnerId;

        var voucherItemIds = voucher.Details.Select(d => d.ItemId).Distinct().ToHashSet();
        var voucherQtyByItem = voucher.Details
            .GroupBy(d => d.ItemId)
            .ToDictionary(g => g.Key, g => g.Sum(d => Math.Abs(d.BaseQty)));
        var packagesToCreate = new List<OutboundPackage>();

        if (scannedLpnCodes.Count > 0)
        {
            var lpns = await _db.LicensePlates.AsNoTracking()
                .Include(l => l.Details)
                .Where(l => l.IsActive
                    && l.WarehouseId == voucher.WarehouseId
                    && scannedLpnCodes.Contains(l.LpnCode))
                .OrderBy(l => l.LpnCode)
                .ToListAsync();

            var foundCodes = lpns.Select(l => l.LpnCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingCodes = scannedLpnCodes.Where(code => !foundCodes.Contains(code)).ToList();
            if (missingCodes.Count > 0)
            {
                TempData["Error"] = $"Không tìm thấy mã kiện (LPN) hợp lệ: {string.Join(", ", missingCodes.Take(5))}.";
                return RedirectToAction("Details", new { id });
            }

            var blockedStatusLpns = lpns
                .Where(l => l.Status is LpnStatusEnum.Voided or LpnStatusEnum.Shipped)
                .Select(l => l.LpnCode)
                .ToList();
            if (blockedStatusLpns.Count > 0)
            {
                TempData["Error"] = $"Mã kiện (LPN) đã hủy hoặc đã giao, không thể đóng gói: {string.Join(", ", blockedStatusLpns.Take(5))}.";
                return RedirectToAction("Details", new { id });
            }

            var ownerMismatchLpns = lpns
                .Where(l => l.OwnerPartnerId != voucher.OwnerPartnerId
                    || l.Details.Any(d => (d.OwnerPartnerId ?? l.OwnerPartnerId) != voucher.OwnerPartnerId))
                .Select(l => l.LpnCode)
                .ToList();
            if (ownerMismatchLpns.Count > 0)
            {
                TempData["Error"] = $"Mã kiện (LPN) không cùng chủ hàng với phiếu xuất: {string.Join(", ", ownerMismatchLpns.Take(5))}.";
                return RedirectToAction("Details", new { id });
            }

            var foreignSkuLpns = lpns
                .Where(l => l.Details.Count == 0 || l.Details.Any(d => !voucherItemIds.Contains(d.ItemId)))
                .Select(l => l.LpnCode)
                .ToList();
            if (foreignSkuLpns.Count > 0)
            {
                TempData["Error"] = $"Mã kiện (LPN) không thuộc danh sách vật tư của phiếu: {string.Join(", ", foreignSkuLpns.Take(5))}.";
                return RedirectToAction("Details", new { id });
            }

            var lpnOverflowItems = lpns
                .SelectMany(l => l.Details)
                .GroupBy(d => d.ItemId)
                .Select(g => new
                {
                    ItemId = g.Key,
                    LpnQty = g.Sum(d => d.Quantity),
                    VoucherQty = voucherQtyByItem.TryGetValue(g.Key, out var voucherQty) ? voucherQty : 0m
                })
                .Where(x => x.LpnQty - x.VoucherQty > 0.0001m)
                .ToList();
            if (lpnOverflowItems.Count > 0)
            {
                var itemCodes = voucher.Details
                    .Where(d => lpnOverflowItems.Select(x => x.ItemId).Contains(d.ItemId))
                    .Select(d => d.Item?.ItemCode ?? d.ItemId.ToString())
                    .Distinct()
                    .Take(5);
                TempData["Error"] = $"Số lượng trong mã kiện (LPN) vượt số lượng phiếu đã xuất: {string.Join(", ", itemCodes)}.";
                return RedirectToAction("Details", new { id });
            }

            foreach (var lpn in lpns)
            {
                var codeExists = await _db.OutboundPackages.AnyAsync(p => p.PackageCode == lpn.LpnCode);
                if (codeExists)
                {
                    TempData["Error"] = $"Mã kiện (LPN) [{lpn.LpnCode}] đã được dùng làm mã kiện cho phiếu khác.";
                    return RedirectToAction("Details", new { id });
                }

                packagesToCreate.Add(new OutboundPackage
                {
                    PackageCode = lpn.LpnCode,
                    VoucherId = voucher.VoucherId,
                    WarehouseId = voucher.WarehouseId,
                    OwnerPartnerId = voucher.OwnerPartnerId,
                    SourceType = "LPN",
                    PackageType = normalizedPackageType ?? "Kiện LPN",
                    ReferenceLpnCode = lpn.LpnCode,
                    TotalQuantity = lpn.Details.Sum(d => d.Quantity),
                    ItemCount = lpn.Details.Select(d => d.ItemId).Distinct().Count(),
                    PackedBy = actor,
                    PackedAt = VietnamNow,
                    Notes = normalizedPackingNote
                });
            }
        }

        var manualPackageCodes = inputPackageCodes;
        if (requestedManualCount > 0 && manualPackageCodes.Count == 0)
        {
            manualPackageCodes = new List<string>();
            for (var i = 0; i < requestedManualCount; i++)
                manualPackageCodes.Add(await GenerateNextPackageCodeAsync());
        }

        if (requestedManualCount > 0)
        {
            var duplicateCodes = manualPackageCodes
                .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateCodes.Count > 0)
            {
                TempData["Error"] = $"Mã kiện bị trùng: {string.Join(", ", duplicateCodes.Take(5))}.";
                return RedirectToAction("Details", new { id });
            }

            var existingManualCodes = await _db.OutboundPackages
                .Where(p => manualPackageCodes.Contains(p.PackageCode))
                .Select(p => p.PackageCode)
                .ToListAsync();
            if (existingManualCodes.Count > 0)
            {
                TempData["Error"] = $"Mã kiện đã tồn tại: {string.Join(", ", existingManualCodes.Take(5))}.";
                return RedirectToAction("Details", new { id });
            }

            var distinctItemCount = voucher.Details.Select(d => d.ItemId).Distinct().Count();
            foreach (var code in manualPackageCodes)
            {
                packagesToCreate.Add(new OutboundPackage
                {
                    PackageCode = code,
                    VoucherId = voucher.VoucherId,
                    WarehouseId = voucher.WarehouseId,
                    OwnerPartnerId = voucher.OwnerPartnerId,
                    SourceType = "Manual",
                    PackageType = normalizedPackageType ?? "Kiện xuất",
                    ItemCount = distinctItemCount,
                    PackedBy = actor,
                    PackedAt = VietnamNow,
                    Notes = normalizedPackingNote
                });
            }
        }

        if (packagesToCreate.Count == 0)
        {
            TempData["Error"] = "Vui lòng tạo ít nhất một kiện hoặc quét ít nhất một mã kiện (LPN) trước khi xác nhận đóng gói.";
            return RedirectToAction("Details", new { id });
        }

        var catchDetails = voucher.Details
            .Where(d => d.Item?.TrackCatchWeight == true && d.Item.RequireCatchWeightAtPickPack)
            .ToList();
        if (catchDetails.Count > 0)
        {
            if (!actualCatchWeight.HasValue || actualCatchWeight.Value <= 0)
            {
                TempData["Error"] = "Phiếu có vật tư cân trọng lượng thực tế nên phải nhập trọng lượng thực tế khi đóng gói.";
                return RedirectToAction("Details", new { id });
            }

            var resolvedWeightUomId = catchWeightUomId ?? catchDetails.Select(d => d.Item?.CatchWeightUomId).FirstOrDefault(x => x.HasValue);
            if (!resolvedWeightUomId.HasValue)
            {
                TempData["Error"] = "Vật tư cân trọng lượng thực tế chưa cấu hình đơn vị cân.";
                return RedirectToAction("Details", new { id });
            }
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            // P0-4: re-check idempotency BÊN TRONG transaction để chặn double ConfirmPacking race.
            var alreadyPacked = await _db.Vouchers.AsNoTracking()
                .Where(v => v.VoucherId == voucher.VoucherId)
                .Select(v => (DateTime?)v.PackedAt)
                .FirstOrDefaultAsync();
            if (alreadyPacked.HasValue)
            {
                await _unitOfWork.RollbackAsync();
                TempData["Error"] = "Phiếu đã được xác nhận đóng gói bởi người khác.";
                return RedirectToAction("Details", new { id });
            }
            var packageRaceCount = await _db.OutboundPackages.CountAsync(p => p.VoucherId == voucher.VoucherId);
            if (packageRaceCount > 0)
            {
                await _unitOfWork.RollbackAsync();
                TempData["Error"] = $"Phiếu đã có {packageRaceCount} kiện xuất, không thể xác nhận đóng gói lần nữa.";
                return RedirectToAction("Details", new { id });
            }

            _db.OutboundPackages.AddRange(packagesToCreate);
            voucher.PackedBy = actor;
            voucher.PackedAt = VietnamNow;
            voucher.FulfillmentStatus = FulfillmentStatusEnum.Packed;
            voucher.UpdatedAt = VietnamNow;
            await _unitOfWork.SaveChangesAsync();

            if (catchDetails.Count > 0 && actualCatchWeight.HasValue)
            {
                var packageCount = packagesToCreate.Count;
                var totalExpectedWeight = catchDetails.Sum(d => (d.Item?.NominalWeightPerBaseUnit ?? 0m) * d.BaseQty);
                var totalBaseQty = catchDetails.Sum(d => d.BaseQty);
                var resolvedWeightUomId = catchWeightUomId ?? catchDetails.Select(d => d.Item?.CatchWeightUomId).First(x => x.HasValue);

                foreach (var package in packagesToCreate)
                {
                    foreach (var detail in catchDetails)
                    {
                        var ratio = totalExpectedWeight > 0
                            ? ((detail.Item?.NominalWeightPerBaseUnit ?? 0m) * detail.BaseQty) / totalExpectedWeight
                            : detail.BaseQty / Math.Max(totalBaseQty, 0.0001m);
                        await _catchWeightService.CaptureAsync(new CatchWeightCaptureRequest
                        {
                            ItemId = detail.ItemId,
                            WarehouseId = voucher.WarehouseId,
                            VoucherId = voucher.VoucherId,
                            VoucherDetailId = detail.VoucherDetailId,
                            OutboundPackageId = package.OutboundPackageId,
                            BaseQuantity = detail.BaseQty / packageCount,
                            ActualWeight = actualCatchWeight.Value * ratio / packageCount,
                            WeightUomId = resolvedWeightUomId,
                            CapturePoint = CatchWeightCapturePointEnum.Pack,
                            CapturedBy = actor,
                            IdempotencyKey = $"voucher:{voucher.VoucherId}:pack:{package.OutboundPackageId}:detail:{detail.VoucherDetailId}"
                        });
                    }
                }

                await _unitOfWork.SaveChangesAsync();
            }

            await _unitOfWork.CommitAsync();
        }
        catch (BusinessRuleException ex)
        {
            await _unitOfWork.RollbackAsync();
            TempData["Error"] = UserSafeError.From(ex);
            return RedirectToAction("Details", new { id });
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }

        TempData["Success"] = $"Xác nhận đóng gói phiếu {voucher.VoucherCode} thành công. Đã tạo {packagesToCreate.Count} kiện xuất.";
        return RedirectToAction("Details", new { id });
    }


    // ═══════════════════════════════════════════════════════════════
    // OUT-05: XÁC NHẬN GIAO HÀNG / BÀN GIAO VẬN CHUYỂN (Shipping)
    // ═══════════════════════════════════════════════════════════════
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmShipping(long id, string? trackingNumber, string? manifestCode, string? handoverNote)
    {
        var voucher = await _db.Vouchers.FirstOrDefaultAsync(v => v.VoucherId == id);
        if (voucher == null) return NotFound();
        if (voucher.IsCancelled) { TempData["Error"] = "Phiếu đã hủy."; return RedirectToAction("Details", new { id }); }

        var isOutbound = voucher.VoucherType is VoucherTypeEnum.XuatKho or VoucherTypeEnum.TraNCC or VoucherTypeEnum.ChuyenKho or VoucherTypeEnum.XuatSanXuat;
        if (!isOutbound) { TempData["Error"] = "Chỉ áp dụng cho phiếu xuất/chuyển kho."; return RedirectToAction("Details", new { id }); }
        if (!voucher.IsPosted) { TempData["Error"] = "Phiếu chưa hoàn tất xuất kho."; return RedirectToAction("Details", new { id }); }
        if (!voucher.PackedAt.HasValue) { TempData["Error"] = "Phiếu chưa đóng gói, không thể giao hàng. Vui lòng xác nhận đóng gói trước."; return RedirectToAction("Details", new { id }); }
        if (voucher.ShippedAt.HasValue) { TempData["Error"] = "Phiếu đã được xác nhận giao hàng."; return RedirectToAction("Details", new { id }); }
        if (RequiresManifest(voucher.VoucherType) && string.IsNullOrWhiteSpace(manifestCode))
        {
            TempData["Error"] = "Phiếu này yêu cầu mã chuyến bàn giao trước khi xác nhận giao hàng.";
            return RedirectToAction("Details", new { id });
        }
        if (RequiresTrackingOrManifest(voucher.VoucherType)
            && string.IsNullOrWhiteSpace(trackingNumber)
            && string.IsNullOrWhiteSpace(manifestCode))
        {
            TempData["Error"] = "Vui lòng nhập ít nhất mã vận đơn hoặc mã chuyến bàn giao trước khi giao hàng.";
            return RedirectToAction("Details", new { id });
        }

        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value) return Forbid();

        // P1.1 — SoD enforcement: người tạo phiếu không được xác nhận giao hàng
        EnforceSod(voucher.CreatedBy, WmsPermissions.VoucherConfirmShipping);

        trackingNumber = NormalizeText(trackingNumber, toUpper: true);
        manifestCode = NormalizeText(manifestCode, toUpper: true);
        handoverNote = NormalizeText(handoverNote);
        if (!string.IsNullOrWhiteSpace(trackingNumber) && trackingNumber.Length > 100)
        {
            TempData["Error"] = "Mã vận đơn tối đa 100 ký tự.";
            return RedirectToAction("Details", new { id });
        }
        if (!string.IsNullOrWhiteSpace(manifestCode) && manifestCode.Length > 100)
        {
            TempData["Error"] = "Mã chuyến bàn giao tối đa 100 ký tự.";
            return RedirectToAction("Details", new { id });
        }
        if (!string.IsNullOrWhiteSpace(handoverNote) && handoverNote.Length > 500)
        {
            TempData["Error"] = "Ghi chú bàn giao tối đa 500 ký tự.";
            return RedirectToAction("Details", new { id });
        }

        var activeLoad = await _db.ShipmentLoadVouchers
            .Include(x => x.ShipmentLoad)
            .Where(x => x.VoucherId == voucher.VoucherId
                && x.RemovedAt == null
                && x.ShipmentLoad != null
                && x.ShipmentLoad.Status != ShipmentLoadStatusEnum.Cancelled
                && x.ShipmentLoad.Status != ShipmentLoadStatusEnum.Closed
                && x.ShipmentLoad.Status != ShipmentLoadStatusEnum.Departed)
            .Select(x => x.ShipmentLoad)
            .FirstOrDefaultAsync();
        if (activeLoad != null)
        {
            TempData["Error"] = $"Phiếu đang thuộc chuyến xe {activeLoad.LoadCode}; vui lòng xác nhận rời kho cho chuyến xe thay vì giao trực tiếp.";
            return RedirectToAction("Details", new { id });
        }

        try
        {
            await _catchWeightService.RequirePackageCatchWeightAsync(voucher.VoucherId);
        }
        catch (BusinessRuleException ex)
        {
            TempData["Error"] = UserSafeError.From(ex);
            return RedirectToAction("Details", new { id });
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);

            var packages = await _db.OutboundPackages
                .Where(p => p.VoucherId == voucher.VoucherId)
                .ToListAsync();
            if (packages.Count == 0)
                throw new BusinessRuleException("Phiếu chưa có kiện xuất, không thể giao hàng.", "SHIPPING_PACKAGE_REQUIRED", "OutboundPackage");
            if (packages.Any(p => p.WarehouseId != voucher.WarehouseId || p.OwnerPartnerId != voucher.OwnerPartnerId))
                throw new BusinessRuleException("Kiện xuất không cùng kho hoặc cùng chủ hàng với phiếu, không thể giao hàng.", "SHIPPING_PACKAGE_SCOPE_MISMATCH", "OutboundPackage");

            await _carrierIntegrationService.EnsureShipmentReadyForShippingAsync(voucher.VoucherId);

            voucher.ShippedBy = User.Identity?.Name ?? "system";
            voucher.ShippedAt = VietnamNow;
            voucher.TrackingNumber = trackingNumber;
            voucher.ManifestCode = manifestCode;
            voucher.FulfillmentStatus = FulfillmentStatusEnum.Shipped;
            voucher.UpdatedAt = VietnamNow;

            foreach (var package in packages)
            {
                package.TrackingNumber = voucher.TrackingNumber;
                package.ManifestCode = voucher.ManifestCode;
            }

            var handoverExists = await _db.ShippingHandoverLogs.AnyAsync(log =>
                log.VoucherId == voucher.VoucherId
                && log.ShipmentLoadId == null
                && log.TrackingNumber == voucher.TrackingNumber
                && log.ManifestCode == voucher.ManifestCode);
            if (!handoverExists)
            {
                _db.ShippingHandoverLogs.Add(new ShippingHandoverLog
                {
                    VoucherId = voucher.VoucherId,
                    WarehouseId = voucher.WarehouseId,
                    HandedOverBy = User.Identity?.Name ?? "system",
                    HandedOverAt = VietnamNow,
                    TrackingNumber = voucher.TrackingNumber,
                    ManifestCode = voucher.ManifestCode,
                    CarrierName = voucher.CarrierName,
                    VehicleNumber = voucher.VehicleNumber,
                    DriverName = voucher.DriverName,
                    DriverPhone = voucher.DriverPhone,
                    Notes = handoverNote
                });
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
        }
        catch (BusinessRuleException ex)
        {
            await _unitOfWork.RollbackAsync();
            TempData["Error"] = UserSafeError.From(ex);
            return RedirectToAction("Details", new { id });
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }

        TempData["Success"] = $"Xác nhận giao hàng phiếu {voucher.VoucherCode} thành công."
            + (!string.IsNullOrWhiteSpace(voucher.TrackingNumber) ? $" Mã vận đơn: {voucher.TrackingNumber}" : "");
        return RedirectToAction("Details", new { id });
    }


    // ═══════════════════════════════════════════════════════════════
    // INB-06: GÁN CẦU DOCK (Dock Assignment cho nhận hàng)
    // ═══════════════════════════════════════════════════════════════
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignDock(
        long id,
        string? dockDoor,
        DateTime? expectedArrivalAt,
        DateTime? dockAppointmentStart,
        DateTime? dockAppointmentEnd,
        string? carrierName,
        string? vehicleNumber,
        string? driverName,
        string? driverPhone)
    {
        var voucher = await _db.Vouchers.FirstOrDefaultAsync(v => v.VoucherId == id);
        if (voucher == null) return NotFound();

        if (!voucher.IsInboundFlow) { TempData["Error"] = "Gán cửa nhận hàng chỉ áp dụng cho phiếu nhập kho."; return RedirectToAction("Details", new { id }); }
        if (voucher.IsCancelled || voucher.IsPosted || voucher.InboundStatus == InboundStatusEnum.Completed)
        {
            TempData["Error"] = "Phiếu đã khóa, không thể cập nhật lịch xe/cửa nhận hàng.";
            return RedirectToAction("Details", new { id });
        }

        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value) return Forbid();

        try
        {
            dockDoor = NormalizeText(dockDoor, toUpper: true);
            carrierName = NormalizeText(carrierName);
            vehicleNumber = NormalizeText(vehicleNumber, toUpper: true);
            driverName = NormalizeText(driverName);
            driverPhone = NormalizeText(driverPhone);

            await ValidateInboundPlanningAsync(
                voucher.VoucherType,
                voucher.WarehouseId,
                voucher.InboundStatus != InboundStatusEnum.Draft,
                expectedArrivalAt,
                dockAppointmentStart,
                dockAppointmentEnd,
                dockDoor,
                voucher.VoucherId);

            if (string.IsNullOrWhiteSpace(dockDoor)
                && dockAppointmentStart.HasValue
                && dockAppointmentEnd.HasValue)
            {
                dockDoor = await SuggestAvailableDockDoorAsync(
                    voucher.WarehouseId,
                    dockAppointmentStart,
                    dockAppointmentEnd,
                    voucher.VoucherId);
            }

            voucher.AsnCode ??= await GenerateNextAsnCodeAsync();
            voucher.ExpectedArrivalAt = expectedArrivalAt;
            voucher.DockAppointmentStart = dockAppointmentStart;
            voucher.DockAppointmentEnd = dockAppointmentEnd;
            voucher.DockDoor = dockDoor;
            voucher.CarrierName = carrierName;
            voucher.VehicleNumber = vehicleNumber;
            voucher.DriverName = driverName;
            voucher.DriverPhone = driverPhone;
            voucher.UpdatedAt = VietnamNow;
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = string.IsNullOrWhiteSpace(voucher.DockDoor)
                ? $"Đã cập nhật lịch xe đến cho phiếu {voucher.VoucherCode}."
                : $"Đã cập nhật lịch xe đến và gán cửa nhận hàng {voucher.DockDoor} cho phiếu {voucher.VoucherCode}.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = UserSafeError.From(ex, "Không thể cập nhật lịch xe/cửa nhận hàng lúc này. Vui lòng thử lại.");
        }

        return RedirectToAction("Details", new { id });
    }


    // ═══ Giai đoạn 6: tự sinh phiếu bù khi xuất thiếu ═══
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBackorder(long id)
    {
        var voucher = await _db.Vouchers
            .Include(v => v.Details).ThenInclude(d => d.Item)
            .Include(v => v.Warehouse)
            .FirstOrDefaultAsync(v => v.VoucherId == id);
        if (voucher == null) return NotFound();

        var isOutbound = voucher.VoucherType is VoucherTypeEnum.XuatKho or VoucherTypeEnum.TraNCC or VoucherTypeEnum.ChuyenKho or VoucherTypeEnum.XuatSanXuat;
        if (!isOutbound || !voucher.IsPosted)
        {
            TempData["Error"] = "Chỉ tạo phiếu bổ sung cho phiếu xuất đã hoàn tất.";
            return RedirectToAction("Details", new { id });
        }

        var expectedRef = $"Phiếu bổ sung từ {voucher.VoucherCode}";
        var existingBO = await _db.Vouchers.FirstOrDefaultAsync(v => v.ReferenceNo == expectedRef);
        if (existingBO != null)
        {
            TempData["Error"] = $"Phiếu này đã được tạo phiếu bổ sung trước đó: {existingBO.VoucherCode}";
            return RedirectToAction("Details", new { id });
        }

        // Tính short-shipped items: lines mà BaseQty > thực tế đã pick
        var pickTasks = await _db.PickTasks
            .Where(t => t.VoucherId == voucher.VoucherId)
            .GroupBy(t => t.VoucherDetailId)
            .Select(g => new { VoucherDetailId = g.Key, TotalPicked = g.Sum(t => t.PickedQty) })
            .ToListAsync();

        var shortLines = new List<(VoucherDetail detail, decimal shortQty)>();
        foreach (var d in voucher.Details)
        {
            var picked = pickTasks.FirstOrDefault(p => p.VoucherDetailId == d.VoucherDetailId)?.TotalPicked ?? 0;
            var shortQty = d.BaseQty - picked;
            if (shortQty > 0.0001m)
                shortLines.Add((d, shortQty));
        }

        if (shortLines.Count == 0)
        {
            TempData["Info"] = "Phiếu đã xuất đầy đủ, không cần tạo phiếu bổ sung.";
            return RedirectToAction("Details", new { id });
        }

        var boCode = $"BO-{voucher.VoucherCode}-{VietnamNow:HHmm}";
        var backorder = new Voucher
        {
            VoucherCode = boCode,
            VoucherType = voucher.VoucherType,
            WarehouseId = voucher.WarehouseId,
            PartnerId = voucher.PartnerId,
            VoucherDate = VietnamNow.Date,
            ReferenceNo = expectedRef,
            Description = $"Tự động tạo phiếu bổ sung cho {shortLines.Count} dòng thiếu từ phiếu {voucher.VoucherCode}",
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = VietnamNow,
        };

        int lineNo = 0;
        foreach (var (d, shortQty) in shortLines)
        {
            lineNo++;
            var convRate = d.TransactionQty > 0 ? d.BaseQty / d.TransactionQty : 1m;
            var transQty = convRate != 0 ? shortQty / convRate : shortQty;

            backorder.Details.Add(new VoucherDetail
            {
                ItemId = d.ItemId,
                LocationId = d.LocationId,
                DestLocationId = d.DestLocationId,
                TransactionQty = transQty,
                BaseQty = shortQty,
                TransactionUomId = d.TransactionUomId,
                ConversionRate = d.ConversionRate,
                UnitPrice = d.UnitPrice,
                LineAmount = d.UnitPrice * transQty,
                LotNumber = d.LotNumber,
                ExpiryDate = d.ExpiryDate,
                LineNumber = lineNo,
                Notes = $"Phiếu bổ sung: thiếu {shortQty:N2} từ dòng {d.LineNumber}"
            });
        }

        backorder.TotalLines = backorder.Details.Count;
        backorder.TotalAmount = backorder.Details.Sum(d => d.LineAmount);
        _db.Vouchers.Add(backorder);

        voucher.Description = string.IsNullOrWhiteSpace(voucher.Description)
            ? $"[Đã sinh phiếu bù: {boCode}]"
            : voucher.Description + $" [Đã sinh phiếu bù: {boCode}]";
        _db.Vouchers.Update(voucher);

        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = $"Đã tạo phiếu bổ sung [{boCode}] với {shortLines.Count} dòng hàng thiếu (tổng {shortLines.Sum(s => s.shortQty):N2} đơn vị).";
        return RedirectToAction("Details", new { id = backorder.VoucherId });
    }

}
