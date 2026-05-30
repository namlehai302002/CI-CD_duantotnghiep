using System.Security.Cryptography;
using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Models;
using WMS.Services;
using WMS.ViewModels;

namespace WMS.Controllers;

public partial class OperationsController
{
    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDockAppointment(
        int warehouseId,
        int? ownerPartnerId,
        long? voucherId,
        long? shipmentLoadId,
        DockAppointmentDirectionEnum direction,
        string? dockDoor,
        DateTime plannedStartAt,
        DateTime plannedEndAt,
        string? goodsType,
        bool isHazmat = false,
        bool isRefrigerated = false,
        bool isUrgent = false,
        decimal palletCount = 0,
        decimal cartonCount = 0,
        decimal weightKg = 0,
        decimal volumeCbm = 0,
        string? notes = null)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
            warehouseId = scopedWh.Value;
        try
        {
            if (ownerPartnerId.HasValue)
                await _tenantScopeService.EnsureCanAccessOwnerAsync(ownerPartnerId, User);
            var appointment = await _dockAppointmentService.CreateAsync(new DockAppointmentRequest
            {
                WarehouseId = warehouseId,
                OwnerPartnerId = ownerPartnerId,
                VoucherId = voucherId,
                ShipmentLoadId = shipmentLoadId,
                Direction = direction,
                DockDoor = dockDoor,
                PlannedStartAt = plannedStartAt,
                PlannedEndAt = plannedEndAt,
                GoodsType = goodsType,
                IsHazmat = isHazmat,
                IsRefrigerated = isRefrigerated,
                IsUrgent = isUrgent,
                PalletCount = palletCount,
                CartonCount = cartonCount,
                WeightKg = weightKg,
                VolumeCbm = volumeCbm,
                Notes = notes,
                Actor = User.Identity?.Name ?? "system"
            }, scopedWh);
            TempData["Success"] = $"Da lap lich cua ben {appointment.AppointmentCode}.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(DockBoard), new { warehouseId, date = plannedStartAt.Date });
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RescheduleDockAppointment(long appointmentId, DateTime plannedStartAt, DateTime plannedEndAt, string? dockDoor, int? warehouseId = null)
    {
        try
        {
            var appointment = await _dockAppointmentService.RescheduleAsync(appointmentId, plannedStartAt, plannedEndAt, dockDoor, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            TempData["Success"] = $"Da doi lich {appointment.AppointmentCode}.";
            warehouseId = appointment.WarehouseId;
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(DockBoard), new { warehouseId, date = plannedStartAt.Date });
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelDockAppointment(long appointmentId, string? reason, int? warehouseId = null)
    {
        try
        {
            var appointment = await _dockAppointmentService.CancelAsync(appointmentId, GetScopedWarehouseId(), User.Identity?.Name ?? "system", reason);
            TempData["Success"] = $"Da huy lich {appointment.AppointmentCode}.";
            warehouseId = appointment.WarehouseId;
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(DockBoard), new { warehouseId });
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckInDockAppointment(long appointmentId, int? warehouseId = null)
    {
        try
        {
            var appointment = await _dockAppointmentService.CheckInAsync(appointmentId, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            TempData["Success"] = $"Da check-in {appointment.AppointmentCode}.";
            warehouseId = appointment.WarehouseId;
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(DockBoard), new { warehouseId });
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckOutDockAppointment(long appointmentId, int? warehouseId = null)
    {
        try
        {
            var appointment = await _dockAppointmentService.CheckOutAsync(appointmentId, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            TempData["Success"] = $"Da check-out {appointment.AppointmentCode}.";
            warehouseId = appointment.WarehouseId;
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(DockBoard), new { warehouseId });
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadYardVisitEvidence(long yardVisitId, YardEvidenceTypeEnum evidenceType, IFormFile evidenceFile, string? notes, int? warehouseId = null, string? search = null)
    {
        try
        {
            if (evidenceFile == null || evidenceFile.Length == 0)
                throw new BusinessRuleException("Vui long chon file bang chung.", "YARD_EVIDENCE_FILE_REQUIRED", "YardVisitEvidence");
            if (evidenceFile.Length > 10 * 1024 * 1024)
                throw new BusinessRuleException("File bang chung toi da 10MB.", "YARD_EVIDENCE_FILE_TOO_LARGE", "YardVisitEvidence");

            var extension = Path.GetExtension(evidenceFile.FileName).ToLowerInvariant();
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
            if (!allowed.Contains(extension))
                throw new BusinessRuleException("Chi ho tro anh JPG/PNG/WEBP hoac PDF.", "YARD_EVIDENCE_FILE_TYPE_INVALID", "YardVisitEvidence");

            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "uploads", "yard-evidence");
            Directory.CreateDirectory(uploadDir);
            var fileName = $"{yardVisitId}-{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadDir, fileName);
            await using (var stream = new FileStream(filePath, FileMode.CreateNew))
                await evidenceFile.CopyToAsync(stream);

            await using var readStream = System.IO.File.OpenRead(filePath);
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(readStream)).ToLowerInvariant();
            var evidence = await _yardService.AddEvidenceAsync(
                yardVisitId,
                evidenceType,
                Path.Combine("App_Data", "uploads", "yard-evidence", fileName).Replace('\\', '/'),
                evidenceFile.FileName,
                evidenceFile.ContentType,
                hash,
                GetScopedWarehouseId(),
                User.Identity?.Name ?? "system",
                notes);
            warehouseId = evidence.YardVisit.WarehouseId;
            TempData["Success"] = "Da luu bang chung gate/yard.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or IOException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(YardManagement), new { warehouseId, search });
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> DownloadYardVisitEvidence(long evidenceId)
    {
        var evidence = await _db.YardVisitEvidence
            .Include(x => x.YardVisit)
            .FirstOrDefaultAsync(x => x.YardVisitEvidenceId == evidenceId);
        if (evidence == null)
            return NotFound();

        var scopedWarehouseId = GetScopedWarehouseId();
        if (scopedWarehouseId.HasValue && evidence.YardVisit.WarehouseId != scopedWarehouseId.Value)
            return Forbid();

        string physicalPath;
        try
        {
            physicalPath = ResolvePrivateYardEvidencePath(evidence.FileUrl);
        }
        catch (Exception ex) when (ex is FileNotFoundException or UnauthorizedAccessException or ArgumentException)
        {
            _ = ex;
            TempData["Error"] = "Không tìm thấy file bằng chứng hoặc đường dẫn không hợp lệ.";
            return NotFound();
        }

        if (!System.IO.File.Exists(physicalPath))
            return NotFound();

        var downloadName = string.IsNullOrWhiteSpace(evidence.OriginalFileName)
            ? Path.GetFileName(physicalPath)
            : Path.GetFileName(evidence.OriginalFileName);
        return PhysicalFile(physicalPath, ResolveYardEvidenceContentType(physicalPath, evidence.ContentType), downloadName);
    }

    private static string ResolvePrivateYardEvidencePath(string storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            throw new FileNotFoundException("Thiếu đường dẫn bằng chứng.");

        var appRoot = Path.GetFullPath(Directory.GetCurrentDirectory());
        var privateEvidenceRoot = Path.GetFullPath(Path.Combine(appRoot, "App_Data", "uploads", "yard-evidence"));
        var normalized = storedPath.Trim().TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);

        string candidate;
        if (normalized.StartsWith($"uploads{Path.DirectorySeparatorChar}yard-evidence{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            candidate = Path.Combine(privateEvidenceRoot, Path.GetFileName(normalized));
        }
        else
        {
            candidate = Path.Combine(appRoot, normalized);
        }

        var fullPath = Path.GetFullPath(candidate);
        if (!fullPath.StartsWith(privateEvidenceRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Đường dẫn bằng chứng không hợp lệ.");

        return fullPath;
    }

    private static string ResolveYardEvidenceContentType(string physicalPath, string? storedContentType)
    {
        if (!string.IsNullOrWhiteSpace(storedContentType))
            return storedContentType;

        var provider = new FileExtensionContentTypeProvider();
        return provider.TryGetContentType(physicalPath, out var contentType)
            ? contentType
            : "application/octet-stream";
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> ExportDockAppointments(int? warehouseId, DateTime? date)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
            warehouseId = scopedWh.Value;
        var day = (date ?? VietnamNow.Date).Date;
        var query = _db.DockAppointments.AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.OwnerPartner)
            .Where(x => (!warehouseId.HasValue || x.WarehouseId == warehouseId.Value)
                && x.PlannedStartAt >= day
                && x.PlannedStartAt < day.AddDays(1));
        var rows = await query.OrderBy(x => x.DockDoor).ThenBy(x => x.PlannedStartAt).ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("DockAppointments");
        var headers = new[] { "Code", "Warehouse", "Owner", "Door", "Direction", "Status", "Start", "End", "Warning" };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            ws.Cell(i + 2, 1).Value = r.AppointmentCode;
            ws.Cell(i + 2, 2).Value = r.Warehouse.WarehouseCode;
            ws.Cell(i + 2, 3).Value = r.OwnerPartner?.PartnerCode ?? "";
            ws.Cell(i + 2, 4).Value = r.DockDoor;
            ws.Cell(i + 2, 5).Value = r.Direction.ToString();
            ws.Cell(i + 2, 6).Value = r.Status.ToString();
            ws.Cell(i + 2, 7).Value = r.PlannedStartAt;
            ws.Cell(i + 2, 8).Value = r.PlannedEndAt;
            ws.Cell(i + 2, 9).Value = r.OverloadWarning ?? "";
        }
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"dock-appointments-{day:yyyyMMdd}.xlsx");
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> CarrierConnectorHealth(int carrierConnectorId)
    {
        var connector = await _db.CarrierConnectors.AsNoTracking().FirstOrDefaultAsync(x => x.CarrierConnectorId == carrierConnectorId);
        if (connector == null)
            return NotFound();
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && connector.WarehouseId != scopedWh.Value)
            return Forbid();

        var recent = await _db.CarrierShipments.AsNoTracking()
            .Where(x => x.CarrierConnectorId == carrierConnectorId && x.CreatedAt >= VietnamNow.AddDays(-7))
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();
        return Json(new
        {
            connector.CarrierCode,
            connector.CarrierName,
            connector.AdapterType,
            connector.IsActive,
            Ready = connector.IsActive && (connector.AdapterType == CarrierAdapterTypeEnum.Mock || !string.IsNullOrWhiteSpace(connector.EndpointUrl)),
            RecentStatus = recent
        });
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncShipmentLoadTms(long shipmentLoadId)
    {
        try
        {
            var scopedWh = GetScopedWarehouseId();
            var load = await _db.ShipmentLoads.FirstOrDefaultAsync(x => x.ShipmentLoadId == shipmentLoadId)
                ?? throw new BusinessRuleException("Khong tim thay chuyen xe.", "SHIPMENT_LOAD_NOT_FOUND", "ShipmentLoad");
            if (scopedWh.HasValue && load.WarehouseId != scopedWh.Value)
                throw new UnauthorizedAccessException("Khong duoc dong bo TMS cho kho khac.");

            var shipments = await _db.CarrierShipments
                .Where(x => x.ShipmentLoadId == shipmentLoadId && x.Status != CarrierShipmentStatusEnum.Cancelled)
                .ToListAsync();
            if (shipments.Count == 0)
            {
                load.TmsReferenceNo ??= $"TMS-FALLBACK-{load.LoadCode}";
                load.TrackingNumber ??= load.TmsReferenceNo;
                load.UpdatedAt = VietnamNow;
                load.UpdatedBy = User.Identity?.Name ?? "system";
                TempData["Success"] = "Carrier chua co van don, da dung fallback TMS reference cho chuyen xe.";
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                foreach (var shipment in shipments)
                    await _carrierIntegrationService.SyncStatusAsync(shipment.CarrierShipmentId, scopedWh, User.Identity?.Name ?? "system");
                load.TmsReferenceNo ??= shipments.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.ExternalShipmentId))?.ExternalShipmentId;
                load.TrackingNumber ??= shipments.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.TrackingNumber))?.TrackingNumber;
                load.UpdatedAt = VietnamNow;
                load.UpdatedBy = User.Identity?.Name ?? "system";
                await _unitOfWork.SaveChangesAsync();
                TempData["Success"] = "Da dong bo shipment/load/tracking voi TMS/carrier.";
            }
            return RedirectToAction(nameof(ShipmentLoadDetails), new { id = shipmentLoadId });
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
            return RedirectToAction(nameof(ShipmentLoads));
        }
    }

    [Authorize(Policy = WmsPermissions.ThreePlBillingManage)]
    [HttpGet]
    public async Task<IActionResult> ThreePlContracts(int? warehouseId, int? ownerPartnerId)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
            warehouseId = scopedWh.Value;
        if (ownerPartnerId.HasValue)
            await _tenantScopeService.EnsureCanAccessOwnerAsync(ownerPartnerId.Value, User);
        var ownerIds = await _tenantScopeService.GetAllowedOwnerIdsAsync(User);

        var query = _db.ThreePlContracts.AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.OwnerPartner)
            .Include(x => x.Rates)
            .AsQueryable();
        if (warehouseId.HasValue)
            query = query.Where(x => x.WarehouseId == warehouseId.Value);
        if (ownerPartnerId.HasValue)
            query = query.Where(x => x.OwnerPartnerId == ownerPartnerId.Value);
        query = _tenantScopeService.ApplyOwnerScope(query, ownerIds);

        return View(new ThreePlContractPageViewModel
        {
            WarehouseId = warehouseId,
            OwnerPartnerId = ownerPartnerId,
            Warehouses = await GetVisibleWarehousesAsync(),
            Owners = await _tenantScopeService.GetVisibleOwnersAsync(User),
            Contracts = await query.OrderByDescending(x => x.EffectiveFrom).Take(300).ToListAsync()
        });
    }

    [Authorize(Policy = WmsPermissions.ThreePlBillingManage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveThreePlContract(long? id, int warehouseId, int ownerPartnerId, string? contractCode, string? contractName, DateTime effectiveFrom, DateTime? effectiveTo, string currency, decimal minimumCharge, decimal taxPercent, decimal discountPercent, bool requiresAdjustmentApproval = true, string? notes = null)
    {
        try
        {
            var scopedWh = GetScopedWarehouseId();
            if (scopedWh.HasValue)
                warehouseId = scopedWh.Value;
            await _tenantScopeService.EnsureCanAccessOwnerAsync(ownerPartnerId, User);
            await _threePlEnterpriseBillingService.SaveContractAsync(new ThreePlContractRequest
            {
                ContractId = id,
                WarehouseId = warehouseId,
                OwnerPartnerId = ownerPartnerId,
                ContractCode = contractCode,
                ContractName = contractName,
                EffectiveFrom = effectiveFrom,
                EffectiveTo = effectiveTo,
                Currency = currency,
                MinimumCharge = minimumCharge,
                TaxPercent = taxPercent,
                DiscountPercent = discountPercent,
                RequiresAdjustmentApproval = requiresAdjustmentApproval,
                Notes = notes,
                Actor = User.Identity?.Name ?? "system"
            }, scopedWh);
            TempData["Success"] = "Đã lưu hợp đồng kho nhiều chủ hàng.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(ThreePlContracts), new { warehouseId, ownerPartnerId });
    }

    [Authorize(Policy = WmsPermissions.ThreePlBillingManage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveThreePlContractRate(long contractId, long? contractRateId, ThreePlChargeTypeEnum chargeType, string? rateCode, string? serviceCode, string chargeUnit, decimal unitRate, decimal tierFromQty, decimal? tierToQty, decimal includedQty, decimal minimumCharge, decimal surchargePercent, decimal offHoursSurcharge, decimal urgentSurcharge, decimal hazmatSurcharge, decimal coldStorageSurcharge, decimal manualHandlingSurcharge, decimal slaPenaltyPercent, decimal slaBonusPercent, DateTime effectiveFrom, DateTime? effectiveTo, bool isActive = true)
    {
        int? warehouseId = null;
        int? ownerPartnerId = null;
        try
        {
            var rate = await _threePlEnterpriseBillingService.SaveContractRateAsync(new ThreePlContractRateRequest
            {
                ContractId = contractId,
                ContractRateId = contractRateId,
                ChargeType = chargeType,
                RateCode = rateCode,
                ServiceCode = serviceCode,
                ChargeUnit = chargeUnit,
                UnitRate = unitRate,
                TierFromQty = tierFromQty,
                TierToQty = tierToQty,
                IncludedQty = includedQty,
                MinimumCharge = minimumCharge,
                SurchargePercent = surchargePercent,
                OffHoursSurcharge = offHoursSurcharge,
                UrgentSurcharge = urgentSurcharge,
                HazmatSurcharge = hazmatSurcharge,
                ColdStorageSurcharge = coldStorageSurcharge,
                ManualHandlingSurcharge = manualHandlingSurcharge,
                SlaPenaltyPercent = slaPenaltyPercent,
                SlaBonusPercent = slaBonusPercent,
                EffectiveFrom = effectiveFrom,
                EffectiveTo = effectiveTo,
                IsActive = isActive
            }, GetScopedWarehouseId());
            await _db.Entry(rate).Reference(x => x.Contract).LoadAsync();
            warehouseId = rate.Contract.WarehouseId;
            ownerPartnerId = rate.Contract.OwnerPartnerId;
            TempData["Success"] = "Đã lưu dòng giá hợp đồng kho nhiều chủ hàng.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(ThreePlContracts), new { warehouseId, ownerPartnerId });
    }

    [Authorize(Policy = WmsPermissions.ThreePlBillingManage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateThreePlInvoice(long runId)
    {
        try
        {
            var invoice = await _threePlEnterpriseBillingService.GenerateInvoiceFromRunAsync(runId, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            TempData["Success"] = "Đã tạo hóa đơn kho nhiều chủ hàng dạng nháp.";
            return RedirectToAction(nameof(ThreePlInvoiceDetails), new { id = invoice.ThreePlInvoiceId });
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
            return RedirectToAction(nameof(ThreePlBillingRunDetails), new { id = runId });
        }
    }

    [Authorize(Policy = WmsPermissions.ThreePlBillingManage)]
    [HttpGet]
    public async Task<IActionResult> ThreePlInvoiceDetails(long id)
    {
        var invoice = await _db.ThreePlInvoices.AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.OwnerPartner)
            .Include(x => x.Contract)
            .Include(x => x.BillingRun)
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.ThreePlInvoiceId == id);
        if (invoice == null)
            return NotFound();
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && invoice.WarehouseId != scopedWh.Value)
            return Forbid();
        await _tenantScopeService.EnsureCanAccessOwnerAsync(invoice.OwnerPartnerId, User);
        var lineIds = invoice.Lines.Select(x => x.ThreePlInvoiceLineId).ToList();
        var disputes = await _db.ThreePlDisputes.AsNoTracking()
            .Where(x => lineIds.Contains(x.ThreePlInvoiceLineId))
            .OrderByDescending(x => x.OpenedAt)
            .ToListAsync();
        return View(new ThreePlInvoiceDetailsViewModel { Invoice = invoice, Disputes = disputes });
    }

    [Authorize(Policy = WmsPermissions.ThreePlBillingManage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmThreePlInvoice(long id)
    {
        try
        {
            await _threePlEnterpriseBillingService.ConfirmInvoiceAsync(id, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            TempData["Success"] = "Đã xác nhận và khóa hóa đơn kho nhiều chủ hàng.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(ThreePlInvoiceDetails), new { id });
    }

    [Authorize(Policy = WmsPermissions.ThreePlBillingManage)]
    [HttpGet]
    public async Task<IActionResult> ExportThreePlInvoiceExcel(long id)
    {
        var invoice = await LoadInvoiceForExportAsync(id);
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Invoice");
        var headers = new[] { "Hóa đơn", "Dòng", "Loại phí", "Mô tả", "Số lượng", "Đơn vị", "Đơn giá", "Tạm tính", "Thuế", "Chiết khấu", "Điều chỉnh", "Tổng", "Tiền tệ" };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        for (var i = 0; i < invoice.Lines.Count; i++)
        {
            var line = invoice.Lines.ElementAt(i);
            ws.Cell(i + 2, 1).Value = invoice.InvoiceCode;
            ws.Cell(i + 2, 2).Value = line.LineType;
            ws.Cell(i + 2, 3).Value = line.ChargeType.ToString();
            ws.Cell(i + 2, 4).Value = line.Description;
            ws.Cell(i + 2, 5).Value = line.Quantity;
            ws.Cell(i + 2, 6).Value = line.ChargeUnit;
            ws.Cell(i + 2, 7).Value = line.UnitRate;
            ws.Cell(i + 2, 8).Value = line.SubtotalAmount;
            ws.Cell(i + 2, 9).Value = line.TaxAmount;
            ws.Cell(i + 2, 10).Value = line.DiscountAmount;
            ws.Cell(i + 2, 11).Value = line.AdjustmentAmount;
            ws.Cell(i + 2, 12).Value = line.TotalAmount;
            ws.Cell(i + 2, 13).Value = line.Currency;
        }
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{invoice.InvoiceCode}.xlsx");
    }

    [Authorize(Policy = WmsPermissions.ThreePlBillingManage)]
    [HttpGet]
    public async Task<IActionResult> ExportThreePlInvoicePdf(long id)
    {
        var invoice = await LoadInvoiceForExportAsync(id);
        var body = $"{invoice.InvoiceCode}\n{invoice.Warehouse.WarehouseCode} - {invoice.OwnerPartner?.PartnerCode}\nTotal {invoice.TotalAmount:N2} {invoice.Currency}";
        var pdf = BuildMinimalPdf(body);
        return File(pdf, "application/pdf", $"{invoice.InvoiceCode}.pdf");
    }

    [Authorize(Policy = WmsPermissions.ThreePlBillingManage)]
    [HttpGet]
    public async Task<IActionResult> ThreePlInvoiceApi(long id)
    {
        var invoice = await LoadInvoiceForExportAsync(id);
        return Json(new
        {
            invoice.InvoiceCode,
            invoice.ApiPublicId,
            invoice.Status,
            invoice.PeriodFrom,
            invoice.PeriodTo,
            invoice.Currency,
            invoice.SubtotalAmount,
            invoice.TaxAmount,
            invoice.DiscountAmount,
            invoice.AdjustmentAmount,
            invoice.TotalAmount,
            Lines = invoice.Lines.Select(x => new { x.LineType, x.ChargeType, x.Description, x.Quantity, x.ChargeUnit, x.UnitRate, x.TotalAmount })
        });
    }

    [Authorize(Roles = "Admin,Manager,Staff,Viewer")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateThreePlDispute(long invoiceLineId, decimal requestedAmount, string reason, long? invoiceId = null)
    {
        try
        {
            var line = await _db.ThreePlInvoiceLines.AsNoTracking()
                .Include(x => x.Invoice)
                .FirstOrDefaultAsync(x => x.ThreePlInvoiceLineId == invoiceLineId)
                ?? throw new BusinessRuleException("Không tìm thấy dòng hóa đơn kho nhiều chủ hàng.", "THREEPL_INVOICE_LINE_NOT_FOUND", "ThreePlInvoiceLine");
            invoiceId = line.ThreePlInvoiceId;
            if (line.Invoice.OwnerPartnerId.HasValue)
                await _tenantScopeService.EnsureCanAccessOwnerAsync(line.Invoice.OwnerPartnerId, User);

            var dispute = await _threePlEnterpriseBillingService.CreateDisputeAsync(invoiceLineId, requestedAmount, reason, User.Identity?.Name ?? "system");
            TempData["Success"] = "Đã gửi khiếu nại dòng phí kho nhiều chủ hàng.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return invoiceId.HasValue ? RedirectToAction(nameof(ThreePlInvoiceDetails), new { id = invoiceId.Value }) : RedirectToAction(nameof(ThreePlClientPortal));
    }

    [Authorize(Policy = WmsPermissions.ThreePlBillingManage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveThreePlDispute(long disputeId, bool approve, decimal approvedAmount, string response, long invoiceId)
    {
        try
        {
            await _threePlEnterpriseBillingService.ResolveDisputeAsync(disputeId, approve, approvedAmount, response, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            TempData["Success"] = "Đã xử lý khiếu nại phí kho nhiều chủ hàng.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(ThreePlInvoiceDetails), new { id = invoiceId });
    }

    [Authorize(Roles = "Admin,Manager,Staff,Viewer")]
    [HttpGet]
    public async Task<IActionResult> ThreePlClientPortal(int? ownerPartnerId)
    {
        var owners = await _tenantScopeService.GetVisibleOwnersAsync(User);
        if (ownerPartnerId.HasValue)
            await _tenantScopeService.EnsureCanAccessOwnerAsync(ownerPartnerId, User);
        ownerPartnerId ??= owners.FirstOrDefault()?.PartnerId;

        var inventoryQuery = _db.ItemLocations.AsNoTracking().Include(x => x.Item).Include(x => x.Location)!.ThenInclude(x => x!.Zone).Where(x => ownerPartnerId.HasValue && x.OwnerPartnerId == ownerPartnerId.Value);
        var orderQuery = _db.Vouchers.AsNoTracking().Where(x => ownerPartnerId.HasValue && x.OwnerPartnerId == ownerPartnerId.Value);
        var invoiceQuery = _db.ThreePlInvoices.AsNoTracking().Include(x => x.Lines).Where(x => ownerPartnerId.HasValue && x.OwnerPartnerId == ownerPartnerId.Value);
        var voucherIds = await orderQuery.OrderByDescending(x => x.VoucherDate).Take(100).Select(x => x.VoucherId).ToListAsync();

        var model = new ThreePlClientPortalViewModel
        {
            OwnerPartnerId = ownerPartnerId,
            Owners = owners,
            Inventory = await inventoryQuery.OrderBy(x => x.Item!.ItemCode).Take(200).ToListAsync(),
            Orders = await orderQuery.OrderByDescending(x => x.VoucherDate).Take(100).ToListAsync(),
            Invoices = await invoiceQuery.OrderByDescending(x => x.PeriodTo).Take(100).ToListAsync(),
            SlaMetrics = await _db.SlaMetrics.AsNoTracking().Where(x => voucherIds.Contains(x.VoucherId)).OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync()
        };
        model.TotalInventoryQty = model.Inventory.Sum(x => x.Quantity);
        model.OpenFeeAmount = model.Invoices.Where(x => x.Status != ThreePlInvoiceStatusEnum.Voided).Sum(x => x.TotalAmount);
        return View(model);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLaborStandard(int? id, string taskType, string taskTypeName, string unitOfWork, decimal expectedMinutesPerUnit, decimal expectedUnitsPerHour, decimal minPerformancePercent, decimal excellentPerformancePercent, int? warehouseId, int? zoneId, string? itemClass, DateTime? effectiveFrom, DateTime? effectiveTo)
    {
        try
        {
            await _laborManagementService.SaveStandardAsync(new LaborStandardRequest
            {
                LaborStandardId = id,
                TaskType = taskType,
                TaskTypeName = taskTypeName,
                UnitOfWork = unitOfWork,
                ExpectedMinutesPerUnit = expectedMinutesPerUnit,
                ExpectedUnitsPerHour = expectedUnitsPerHour,
                MinPerformancePercent = minPerformancePercent,
                ExcellentPerformancePercent = excellentPerformancePercent,
                WarehouseId = warehouseId,
                ZoneId = zoneId,
                ItemClass = itemClass,
                EffectiveFrom = effectiveFrom ?? VietnamNow.Date,
                EffectiveTo = effectiveTo
            }, GetScopedWarehouseId());
            TempData["Success"] = "Da luu chuan nang suat lao dong.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(LaborProductivity), new { warehouseId });
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartLaborActivity(int warehouseId, int? zoneId, string taskType, string taskSourceType, string? taskSourceId, string? taskSourceCode, decimal workQuantity, string unitOfWork)
    {
        try
        {
            var scopedWh = GetScopedWarehouseId();
            if (scopedWh.HasValue)
                warehouseId = scopedWh.Value;
            await _laborManagementService.StartActivityAsync(new LaborActivityRequest
            {
                WarehouseId = warehouseId,
                ZoneId = zoneId,
                UserName = User.Identity?.Name ?? "system",
                TaskType = taskType,
                TaskSourceType = taskSourceType,
                TaskSourceId = taskSourceId,
                TaskSourceCode = taskSourceCode,
                WorkQuantity = workQuantity,
                UnitOfWork = unitOfWork,
                Actor = User.Identity?.Name ?? "system"
            }, scopedWh);
            TempData["Success"] = "Da bat dau tac vu lao dong.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(LaborProductivity), new { warehouseId });
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteLaborActivity(long activityId, decimal? workQuantity, string? exceptionReason, int? warehouseId = null)
    {
        try
        {
            var activity = await _laborManagementService.CompleteActivityAsync(activityId, workQuantity, exceptionReason, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            warehouseId = activity.WarehouseId;
            TempData["Success"] = "Da hoan tat tac vu lao dong.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(LaborProductivity), new { warehouseId });
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveLaborException(long reviewId, bool approve, decimal productivityAfter, decimal incentiveAmount, string notes, int? warehouseId = null)
    {
        try
        {
            var review = await _laborManagementService.ApproveExceptionAsync(reviewId, approve, productivityAfter, incentiveAmount, notes, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            warehouseId = review.LaborActivity.WarehouseId;
            TempData["Success"] = "Da xu ly ngoai le nang suat.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(LaborProductivity), new { warehouseId });
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> ExportLaborProductivity(int? warehouseId, int days = 7)
    {
        var model = await BuildLaborProductivityModelAsync(warehouseId, days);
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("LaborProductivity");
        var headers = new[] { "User", "Shift", "Warehouse", "Zone", "Tasks", "Quantity", "ExpectedMinutes", "ActualMinutes", "Productivity", "Exceptions" };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        for (var i = 0; i < model.WorkerRows.Count; i++)
        {
            var row = model.WorkerRows[i];
            ws.Cell(i + 2, 1).Value = row.UserName;
            ws.Cell(i + 2, 2).Value = row.ShiftCode;
            ws.Cell(i + 2, 3).Value = row.WarehouseName;
            ws.Cell(i + 2, 4).Value = row.ZoneName ?? "";
            ws.Cell(i + 2, 5).Value = row.TaskCount;
            ws.Cell(i + 2, 6).Value = row.WorkQuantity;
            ws.Cell(i + 2, 7).Value = row.ExpectedMinutes;
            ws.Cell(i + 2, 8).Value = row.ActualMinutes;
            ws.Cell(i + 2, 9).Value = row.ProductivityPercent;
            ws.Cell(i + 2, 10).Value = row.ExceptionCount;
        }
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"labor-productivity-{days}d.xlsx");
    }

    private async Task<ThreePlInvoice> LoadInvoiceForExportAsync(long id)
    {
        var invoice = await _db.ThreePlInvoices.AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.OwnerPartner)
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.ThreePlInvoiceId == id)
            ?? throw new BusinessRuleException("Không tìm thấy hóa đơn kho nhiều chủ hàng.", "THREEPL_INVOICE_NOT_FOUND", "ThreePlInvoice");
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && invoice.WarehouseId != scopedWh.Value)
            throw new UnauthorizedAccessException("Khong duoc export hoa don kho khac.");
        await _tenantScopeService.EnsureCanAccessOwnerAsync(invoice.OwnerPartnerId, User);
        return invoice;
    }

    private async Task<LaborProductivityPageViewModel> BuildLaborProductivityModelAsync(int? warehouseId, int days)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
            warehouseId = scopedWh.Value;
        days = Math.Clamp(days, 1, 120);
        await _laborManagementService.CaptureCompletedWarehouseTasksAsync(warehouseId, days);
        var cutoff = VietnamNow.AddDays(-days);

        var activitiesQuery = _db.LaborActivities.AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.Zone)
            .Where(x => x.StartedAt >= cutoff && (!warehouseId.HasValue || x.WarehouseId == warehouseId.Value));

        var activities = await activitiesQuery
            .OrderByDescending(x => x.StartedAt)
            .Take(500)
            .ToListAsync();

        var workerRows = activities
            .Where(x => x.Status is LaborActivityStatusEnum.Completed or LaborActivityStatusEnum.Exception)
            .GroupBy(x => new { x.UserName, x.ShiftCode, x.WarehouseId, WarehouseName = x.Warehouse.WarehouseName, x.ZoneId, ZoneName = x.Zone != null ? x.Zone.ZoneName : null })
            .Select(g =>
            {
                var expected = g.Sum(x => x.ExpectedMinutes);
                var actual = g.Sum(x => x.ActualMinutes);
                return new LaborProductivityRow
                {
                    UserName = g.Key.UserName,
                    ShiftCode = g.Key.ShiftCode,
                    WarehouseName = g.Key.WarehouseName,
                    ZoneName = g.Key.ZoneName,
                    TaskCount = g.Count(),
                    WorkQuantity = g.Sum(x => x.WorkQuantity),
                    ExpectedMinutes = expected,
                    ActualMinutes = actual,
                    ProductivityPercent = actual <= 0 ? 0 : Math.Round(expected / actual * 100m, 2, MidpointRounding.AwayFromZero),
                    ExceptionCount = g.Count(x => x.IsException)
                };
            })
            .OrderByDescending(x => x.ProductivityPercent)
            .ThenByDescending(x => x.TaskCount)
            .ToList();

        var bottlenecks = activities
            .GroupBy(x => new { x.Warehouse.WarehouseName, ZoneName = x.Zone != null ? x.Zone.ZoneName : "Khong xac dinh", x.TaskType })
            .Select(g => new LaborBottleneckRow
            {
                WarehouseName = g.Key.WarehouseName,
                ZoneName = g.Key.ZoneName,
                TaskType = g.Key.TaskType,
                Backlog = g.Count(x => x.Status == LaborActivityStatusEnum.InProgress) + g.Max(x => x.BacklogAtStart),
                AverageWaitingMinutes = Math.Round(g.Average(x => (decimal)x.WaitingMinutes), 2, MidpointRounding.AwayFromZero),
                AverageProductivityPercent = Math.Round(g.Average(x => x.ProductivityPercent), 2, MidpointRounding.AwayFromZero)
            })
            .OrderByDescending(x => x.Backlog)
            .ThenByDescending(x => x.AverageWaitingMinutes)
            .Take(50)
            .ToList();

        var model = new LaborProductivityPageViewModel
        {
            WarehouseId = warehouseId,
            Days = days,
            Warehouses = await GetVisibleWarehousesAsync(),
            Zones = await _db.Zones.AsNoTracking()
                .Where(x => !warehouseId.HasValue || x.WarehouseId == warehouseId.Value)
                .OrderBy(x => x.ZoneCode)
                .ToListAsync(),
            Standards = await _db.LaborStandards.AsNoTracking()
                .Include(x => x.Warehouse)
                .Include(x => x.Zone)
                .Where(x => !warehouseId.HasValue || !x.WarehouseId.HasValue || x.WarehouseId == warehouseId.Value)
                .OrderBy(x => x.TaskType)
                .ThenBy(x => x.ZoneId)
                .ToListAsync(),
            Activities = activities,
            Exceptions = await _db.LaborExceptionReviews.AsNoTracking()
                .Include(x => x.LaborActivity)
                .ThenInclude(x => x.Warehouse)
                .Where(x => x.Status == LaborExceptionStatusEnum.Open && (!warehouseId.HasValue || x.LaborActivity.WarehouseId == warehouseId.Value))
                .OrderByDescending(x => x.RequestedAt)
                .Take(100)
                .ToListAsync(),
            WorkerRows = workerRows,
            Bottlenecks = bottlenecks
        };
        return model;
    }

    private static byte[] BuildMinimalPdf(string text)
    {
        var safe = text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("\r", "").Replace("\n", ") Tj T* (");
        var stream = $"BT /F1 12 Tf 50 760 Td ({safe}) Tj ET";
        var pdf = "%PDF-1.4\n"
            + "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n"
            + "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj\n"
            + "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj\n"
            + "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj\n"
            + $"5 0 obj << /Length {stream.Length} >> stream\n{stream}\nendstream endobj\n"
            + "xref\n0 6\n0000000000 65535 f \n"
            + "trailer << /Root 1 0 R /Size 6 >>\nstartxref\n0\n%%EOF";
        return Encoding.ASCII.GetBytes(pdf);
    }
}
