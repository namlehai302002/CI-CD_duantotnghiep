using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Models;
using WMS.Services;
using WMS.ViewModels;

namespace WMS.Controllers;

public partial class OperationsController
{
    [Authorize(Policy = WmsPermissions.TenantScopeManage)]
    [HttpGet]
    public async Task<IActionResult> TenantOwnerScopes()
    {
        var model = new TenantOwnerScopePageViewModel
        {
            Users = await _db.AppUsers.AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.IsActive)
                .OrderBy(u => u.UserName)
                .ToListAsync(),
            Owners = await _db.Partners.AsNoTracking()
                .Where(p => p.IsThreePlClient && p.IsActive)
                .OrderBy(p => p.PartnerCode)
                .ToListAsync(),
            Scopes = await _db.AppUserOwnerScopes.AsNoTracking()
                .Include(s => s.User)
                .Include(s => s.OwnerPartner)
                .OrderByDescending(s => s.IsActive)
                .ThenBy(s => s.User.UserName)
                .ThenBy(s => s.OwnerPartner.PartnerCode)
                .ToListAsync()
        };
        return View(model);
    }

    [Authorize(Policy = WmsPermissions.TenantScopeManage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTenantOwnerScope(int userId, int ownerPartnerId)
    {
        try
        {
            var userExists = await _db.AppUsers.AnyAsync(u => u.UserId == userId && u.IsActive);
            var ownerExists = await _db.Partners.AnyAsync(p => p.PartnerId == ownerPartnerId && p.IsThreePlClient && p.IsActive);
            if (!userExists || !ownerExists)
                throw new BusinessRuleException("Người dùng hoặc chủ hàng kho nhiều chủ hàng không hợp lệ.", "TENANT_SCOPE_INVALID", "AppUserOwnerScope");

            var existing = await _db.AppUserOwnerScopes
                .FirstOrDefaultAsync(s => s.UserId == userId && s.OwnerPartnerId == ownerPartnerId);
            if (existing == null)
            {
                _db.AppUserOwnerScopes.Add(new AppUserOwnerScope
                {
                    UserId = userId,
                    OwnerPartnerId = ownerPartnerId,
                    IsActive = true,
                    CreatedBy = User.Identity?.Name ?? "system",
                    CreatedAt = VietnamNow
                });
            }
            else if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.CreatedBy = User.Identity?.Name ?? "system";
                existing.CreatedAt = VietnamNow;
                existing.RevokedAt = null;
                existing.RevokedBy = null;
            }

            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = "Da gan pham vi chu hang cho user.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }

        return RedirectToAction(nameof(TenantOwnerScopes));
    }

    [Authorize(Policy = WmsPermissions.TenantScopeManage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeTenantOwnerScope(long id)
    {
        var scope = await _db.AppUserOwnerScopes.FirstOrDefaultAsync(s => s.AppUserOwnerScopeId == id);
        if (scope != null && scope.IsActive)
        {
            scope.IsActive = false;
            scope.RevokedAt = VietnamNow;
            scope.RevokedBy = User.Identity?.Name ?? "system";
            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = "Da thu hoi pham vi chu hang.";
        }
        return RedirectToAction(nameof(TenantOwnerScopes));
    }

    [Authorize(Policy = WmsPermissions.ThreePlBillingManage)]
    [HttpGet]
    public async Task<IActionResult> ThreePlBillingRates(int? warehouseId, int? ownerPartnerId)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
            warehouseId = scopedWh.Value;

        var ownerIds = await _tenantScopeService.GetAllowedOwnerIdsAsync(User);
        if (ownerPartnerId.HasValue)
            await _tenantScopeService.EnsureCanAccessOwnerAsync(ownerPartnerId.Value, User);

        var query = _db.ThreePlBillingRates.AsNoTracking()
            .Include(r => r.Warehouse)
            .Include(r => r.OwnerPartner)
            .AsQueryable();
        if (warehouseId.HasValue)
            query = query.Where(r => r.WarehouseId == warehouseId.Value);
        if (ownerPartnerId.HasValue)
            query = query.Where(r => r.OwnerPartnerId == ownerPartnerId.Value);
        if (ownerIds.Count > 0)
            query = query.Where(r => ownerIds.Contains(r.OwnerPartnerId));

        var model = new ThreePlBillingRatePageViewModel
        {
            WarehouseId = warehouseId,
            OwnerPartnerId = ownerPartnerId,
            Warehouses = await GetVisibleWarehousesAsync(),
            Owners = await _tenantScopeService.GetVisibleOwnersAsync(User),
            Rates = await query
                .OrderBy(r => r.Warehouse.WarehouseCode)
                .ThenBy(r => r.OwnerPartner.PartnerCode)
                .ThenBy(r => r.ChargeType)
                .ToListAsync()
        };
        return View(model);
    }

    [Authorize(Policy = WmsPermissions.ThreePlBillingManage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveThreePlBillingRate(long? id, int warehouseId, int ownerPartnerId, ThreePlChargeTypeEnum chargeType, string rateCode, decimal unitRate, string chargeUnit, string currency, DateTime effectiveFrom, DateTime? effectiveTo, bool isActive = true, string? notes = null)
    {
        try
        {
            var scopedWh = GetScopedWarehouseId();
            if (scopedWh.HasValue && warehouseId != scopedWh.Value)
                throw new UnauthorizedAccessException("Khong duoc tao bang gia cho kho khac.");
            await _tenantScopeService.EnsureCanAccessOwnerAsync(ownerPartnerId, User);
            if (unitRate < 0)
                throw new BusinessRuleException("Đơn giá kho nhiều chủ hàng không được âm.", "THREEPL_RATE_NEGATIVE", "ThreePlBillingRate");

            ThreePlBillingRate rate;
            if (id.HasValue)
            {
                rate = await _db.ThreePlBillingRates.FirstOrDefaultAsync(r => r.ThreePlBillingRateId == id.Value)
                    ?? throw new BusinessRuleException("Không tìm thấy bảng giá kho nhiều chủ hàng.", "THREEPL_RATE_NOT_FOUND", "ThreePlBillingRate");
                if (scopedWh.HasValue && rate.WarehouseId != scopedWh.Value)
                    throw new UnauthorizedAccessException("Khong duoc sua bang gia kho khac.");
                await _tenantScopeService.EnsureCanAccessOwnerAsync(rate.OwnerPartnerId, User);
                rate.UpdatedAt = VietnamNow;
            }
            else
            {
                rate = new ThreePlBillingRate
                {
                    CreatedAt = VietnamNow,
                    CreatedBy = User.Identity?.Name ?? "system"
                };
                _db.ThreePlBillingRates.Add(rate);
            }

            rate.WarehouseId = warehouseId;
            rate.OwnerPartnerId = ownerPartnerId;
            rate.ChargeType = chargeType;
            rate.RateCode = string.IsNullOrWhiteSpace(rateCode) ? $"{chargeType}-{warehouseId}-{ownerPartnerId}" : rateCode.Trim();
            rate.UnitRate = unitRate;
            rate.ChargeUnit = string.IsNullOrWhiteSpace(chargeUnit) ? "unit" : chargeUnit.Trim();
            rate.Currency = string.IsNullOrWhiteSpace(currency) ? "VND" : currency.Trim().ToUpperInvariant();
            rate.EffectiveFrom = effectiveFrom == default ? VietnamNow.Date : effectiveFrom.Date;
            rate.EffectiveTo = effectiveTo?.Date;
            rate.IsActive = isActive;
            rate.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = "Đã lưu bảng giá kho nhiều chủ hàng.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }

        return RedirectToAction(nameof(ThreePlBillingRates), new { warehouseId, ownerPartnerId });
    }

    [Authorize(Policy = WmsPermissions.ThreePlBillingManage)]
    [HttpGet]
    public async Task<IActionResult> ThreePlBillingRuns(int? warehouseId, int? ownerPartnerId, DateTime? periodFrom, DateTime? periodTo)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
            warehouseId = scopedWh.Value;
        if (ownerPartnerId.HasValue)
            await _tenantScopeService.EnsureCanAccessOwnerAsync(ownerPartnerId.Value, User);
        var ownerIds = await _tenantScopeService.GetAllowedOwnerIdsAsync(User);

        var query = _db.ThreePlBillingRuns.AsNoTracking()
            .Include(r => r.Warehouse)
            .Include(r => r.OwnerPartner)
            .AsQueryable();
        if (warehouseId.HasValue)
            query = query.Where(r => r.WarehouseId == warehouseId.Value);
        if (ownerPartnerId.HasValue)
            query = query.Where(r => r.OwnerPartnerId == ownerPartnerId.Value);
        if (periodFrom.HasValue)
            query = query.Where(r => r.PeriodTo >= periodFrom.Value.Date);
        if (periodTo.HasValue)
            query = query.Where(r => r.PeriodFrom <= periodTo.Value.Date);
        if (ownerIds.Count > 0)
            query = query.Where(r => ownerIds.Contains(r.OwnerPartnerId));

        var model = new ThreePlBillingRunBoardViewModel
        {
            WarehouseId = warehouseId,
            OwnerPartnerId = ownerPartnerId,
            PeriodFrom = periodFrom,
            PeriodTo = periodTo,
            Warehouses = await GetVisibleWarehousesAsync(),
            Owners = await _tenantScopeService.GetVisibleOwnersAsync(User),
            Runs = await query
                .OrderByDescending(r => r.PeriodTo)
                .ThenByDescending(r => r.CreatedAt)
                .Take(300)
                .Select(r => new ThreePlBillingRunRow
                {
                    ThreePlBillingRunId = r.ThreePlBillingRunId,
                    RunCode = r.RunCode,
                    WarehouseName = r.Warehouse.WarehouseName,
                    OwnerName = r.OwnerPartner.PartnerName,
                    PeriodFrom = r.PeriodFrom,
                    PeriodTo = r.PeriodTo,
                    Status = r.Status,
                    TotalAmount = r.TotalAmount,
                    Currency = r.Currency,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync()
        };
        return View(model);
    }

    [Authorize(Policy = WmsPermissions.ThreePlBillingManage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateThreePlBillingRun(int warehouseId, int ownerPartnerId, DateTime periodFrom, DateTime periodTo)
    {
        try
        {
            var scopedWh = GetScopedWarehouseId();
            if (scopedWh.HasValue && warehouseId != scopedWh.Value)
                throw new UnauthorizedAccessException("Khong duoc tinh phi cho kho khac.");
            await _tenantScopeService.EnsureCanAccessOwnerAsync(ownerPartnerId, User);
            var run = await _threePlBillingService.GenerateDraftRunAsync(new ThreePlBillingRunRequest
            {
                WarehouseId = warehouseId,
                OwnerPartnerId = ownerPartnerId,
                PeriodFrom = periodFrom,
                PeriodTo = periodTo,
                Actor = User.Identity?.Name ?? "system"
            });
            TempData["Success"] = "Đã tạo hoặc làm mới kỳ tính phí kho nhiều chủ hàng.";
            return RedirectToAction(nameof(ThreePlBillingRunDetails), new { id = run.ThreePlBillingRunId });
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
            return RedirectToAction(nameof(ThreePlBillingRuns), new { warehouseId, ownerPartnerId, periodFrom, periodTo });
        }
    }

    [Authorize(Policy = WmsPermissions.ThreePlBillingManage)]
    [HttpGet]
    public async Task<IActionResult> ThreePlBillingRunDetails(long id)
    {
        var run = await _db.ThreePlBillingRuns.AsNoTracking()
            .Include(r => r.Warehouse)
            .Include(r => r.OwnerPartner)
            .FirstOrDefaultAsync(r => r.ThreePlBillingRunId == id);
        if (run == null)
            return NotFound();
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && run.WarehouseId != scopedWh.Value)
            return Forbid();
        await _tenantScopeService.EnsureCanAccessOwnerAsync(run.OwnerPartnerId, User);

        var charges = await _db.ThreePlBillingCharges.AsNoTracking()
            .Include(c => c.BillingRate)
            .Where(c => c.ThreePlBillingRunId == id)
            .OrderBy(c => c.ChargeType)
            .ThenBy(c => c.SourceCode)
            .ToListAsync();

        var invoice = await _db.ThreePlInvoices.AsNoTracking()
            .Include(i => i.Lines)
            .Where(i => i.ThreePlBillingRunId == id && i.Status != ThreePlInvoiceStatusEnum.Voided)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync();
        var lineIds = invoice?.Lines.Select(l => l.ThreePlInvoiceLineId).ToList() ?? new List<long>();
        var disputes = lineIds.Count == 0
            ? new List<ThreePlDispute>()
            : await _db.ThreePlDisputes.AsNoTracking()
                .Where(d => lineIds.Contains(d.ThreePlInvoiceLineId))
                .OrderByDescending(d => d.OpenedAt)
                .ToListAsync();

        return View(new ThreePlBillingRunDetailsViewModel { Run = run, Charges = charges, Invoice = invoice, Disputes = disputes });
    }

    [Authorize(Policy = WmsPermissions.ThreePlBillingManage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmThreePlBillingRun(long id)
    {
        try
        {
            await _threePlBillingService.ConfirmRunAsync(id, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            TempData["Success"] = "Đã xác nhận kỳ tính phí kho nhiều chủ hàng.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(ThreePlBillingRunDetails), new { id });
    }

    [Authorize(Policy = WmsPermissions.ThreePlBillingManage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VoidThreePlBillingRun(long id, string reason)
    {
        try
        {
            await _threePlBillingService.VoidRunAsync(id, reason, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            TempData["Success"] = "Đã hủy kỳ tính phí kho nhiều chủ hàng.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(ThreePlBillingRunDetails), new { id });
    }

    [Authorize(Policy = WmsPermissions.ThreePlBillingManage)]
    [HttpGet]
    public async Task<IActionResult> ExportThreePlBillingRunCsv(long id)
    {
        var export = await BuildThreePlBillingExportAsync(id);
        return File(Encoding.UTF8.GetBytes(export.Csv), "text/csv", $"{export.RunCode}.csv");
    }

    [Authorize(Policy = WmsPermissions.ThreePlBillingManage)]
    [HttpGet]
    public async Task<IActionResult> ExportThreePlBillingRunExcel(long id)
    {
        var export = await BuildThreePlBillingExportAsync(id);
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Charges");
        var headers = new[] { "RunCode", "ChargeType", "SourceType", "SourceCode", "Quantity", "UnitRate", "Amount", "Currency", "Status" };
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];
        for (var i = 0; i < export.Charges.Count; i++)
        {
            var c = export.Charges[i];
            sheet.Cell(i + 2, 1).Value = export.RunCode;
            sheet.Cell(i + 2, 2).Value = c.ChargeType.ToString();
            sheet.Cell(i + 2, 3).Value = c.SourceType;
            sheet.Cell(i + 2, 4).Value = c.SourceCode;
            sheet.Cell(i + 2, 5).Value = c.Quantity;
            sheet.Cell(i + 2, 6).Value = c.UnitRate;
            sheet.Cell(i + 2, 7).Value = c.Amount;
            sheet.Cell(i + 2, 8).Value = c.Currency;
            sheet.Cell(i + 2, 9).Value = c.Status.ToString();
        }
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{export.RunCode}.xlsx");
    }

    [Authorize(Policy = WmsPermissions.MheManage)]
    [HttpGet]
    public async Task<IActionResult> MheDashboard(int? warehouseId, MheCommandStatusEnum? status)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
            warehouseId = scopedWh.Value;
        var ownerIds = await _tenantScopeService.GetAllowedOwnerIdsAsync(User);

        var commandQuery = _db.MheCommands.AsNoTracking()
            .Include(c => c.Warehouse)
            .Include(c => c.OwnerPartner)
            .Include(c => c.MheSystem)
            .AsQueryable();
        if (warehouseId.HasValue)
            commandQuery = commandQuery.Where(c => c.WarehouseId == warehouseId.Value);
        if (status.HasValue)
            commandQuery = commandQuery.Where(c => c.Status == status.Value);
        commandQuery = _tenantScopeService.ApplyOwnerScope(commandQuery, ownerIds);

        var model = new MheDashboardViewModel
        {
            WarehouseId = warehouseId,
            Status = status,
            Warehouses = await GetVisibleWarehousesAsync(),
            Systems = await _db.MheSystems.AsNoTracking()
                .Include(s => s.Warehouse)
                .Where(s => !warehouseId.HasValue || s.WarehouseId == warehouseId.Value)
                .OrderBy(s => s.Warehouse.WarehouseCode)
                .ThenBy(s => s.SystemCode)
                .ToListAsync(),
            Commands = await commandQuery
                .OrderByDescending(c => c.CreatedAt)
                .Take(300)
                .Select(c => new MheCommandRow
                {
                    MheCommandId = c.MheCommandId,
                    CommandCode = c.CommandCode,
                    WarehouseName = c.Warehouse.WarehouseName,
                    OwnerName = c.OwnerPartner != null ? c.OwnerPartner.PartnerName : null,
                    SystemCode = c.MheSystem != null ? c.MheSystem.SystemCode : null,
                    CommandType = c.CommandType,
                    Status = c.Status,
                    SourceType = c.SourceType,
                    SourceCode = c.SourceCode,
                    CorrelationId = c.CorrelationId,
                    RetryCount = c.RetryCount,
                    CreatedAt = c.CreatedAt,
                    SentAt = c.SentAt,
                    CompletedAt = c.CompletedAt,
                    LastError = c.LastError
                })
                .ToListAsync()
        };
        model.PendingCount = model.Commands.Count(c => c.Status is MheCommandStatusEnum.Pending or MheCommandStatusEnum.Queued or MheCommandStatusEnum.Sent);
        model.FailedCount = model.Commands.Count(c => c.Status is MheCommandStatusEnum.Failed or MheCommandStatusEnum.DeadLetter);
        return View(model);
    }

    [Authorize(Policy = WmsPermissions.MheManage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMheSystem(int? id, int warehouseId, string systemCode, string systemName, MheSystemTypeEnum systemType, string? endpointUrl, bool isActive = true, string? notes = null)
    {
        try
        {
            var scopedWh = GetScopedWarehouseId();
            if (scopedWh.HasValue && warehouseId != scopedWh.Value)
                throw new UnauthorizedAccessException("Khong duoc cau hinh MHE cho kho khac.");

            MheSystem system;
            if (id.HasValue)
            {
                system = await _db.MheSystems.FirstOrDefaultAsync(s => s.MheSystemId == id.Value)
                    ?? throw new BusinessRuleException("Không tìm thấy hệ thống thiết bị tự động.", "MHE_SYSTEM_NOT_FOUND", "MheSystem");
                system.UpdatedAt = VietnamNow;
            }
            else
            {
                system = new MheSystem { CreatedAt = VietnamNow, CreatedBy = User.Identity?.Name ?? "system" };
                _db.MheSystems.Add(system);
            }

            system.WarehouseId = warehouseId;
            system.SystemCode = string.IsNullOrWhiteSpace(systemCode) ? $"MHE-{warehouseId}" : systemCode.Trim().ToUpperInvariant();
            system.SystemName = string.IsNullOrWhiteSpace(systemName) ? system.SystemCode : systemName.Trim();
            system.SystemType = systemType;
            system.EndpointUrl = string.IsNullOrWhiteSpace(endpointUrl) ? null : endpointUrl.Trim();
            system.IsActive = isActive;
            system.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = "Da luu he thong MHE.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(MheDashboard), new { warehouseId });
    }

    [Authorize(Policy = WmsPermissions.MheManage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendPickTaskToMhe(long pickTaskId)
    {
        return await ExecuteMheActionAsync(() => RequiredMheService().CreateFromPickTaskAsync(pickTaskId, User.Identity?.Name ?? "system"));
    }

    [Authorize(Policy = WmsPermissions.MheManage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMovementTaskToMhe(long movementTaskId)
    {
        return await ExecuteMheActionAsync(() => RequiredMheService().CreateFromMovementTaskAsync(movementTaskId, User.Identity?.Name ?? "system"));
    }

    [Authorize(Policy = WmsPermissions.MheManage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendWaveToMhe(long waveId)
    {
        return await ExecuteMheActionAsync(() => RequiredMheService().CreateFromWaveAsync(waveId, User.Identity?.Name ?? "system"));
    }

    [Authorize(Policy = WmsPermissions.MheManage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetryMheCommand(long commandId)
    {
        return await ExecuteMheActionAsync(() => RequiredMheService().RetryAsync(commandId, User.Identity?.Name ?? "system"));
    }

    [Authorize(Policy = WmsPermissions.MheManage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelMheCommand(long commandId)
    {
        return await ExecuteMheActionAsync(() => RequiredMheService().CancelAsync(commandId, User.Identity?.Name ?? "system"));
    }

    private async Task<IActionResult> ExecuteMheActionAsync(Func<Task<MheCommand>> action)
    {
        try
        {
            var command = await action();
            TempData["Success"] = $"Lệnh thiết bị {command.CommandCode} đang ở trạng thái {command.Status}.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(MheDashboard));
    }

    private IMheIntegrationService RequiredMheService()
        => _mheIntegrationService;

    private async Task<(string RunCode, string Csv, List<ThreePlBillingCharge> Charges)> BuildThreePlBillingExportAsync(long runId)
    {
        var run = await _db.ThreePlBillingRuns.AsNoTracking()
            .Include(r => r.Warehouse)
            .Include(r => r.OwnerPartner)
            .FirstOrDefaultAsync(r => r.ThreePlBillingRunId == runId);
        if (run == null)
            throw new BusinessRuleException("Không tìm thấy kỳ tính phí kho nhiều chủ hàng.", "THREEPL_RUN_NOT_FOUND", "ThreePlBillingRun");
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && run.WarehouseId != scopedWh.Value)
            throw new UnauthorizedAccessException("Khong duoc export ky tinh phi kho khac.");
        await _tenantScopeService.EnsureCanAccessOwnerAsync(run.OwnerPartnerId, User);

        var charges = await _db.ThreePlBillingCharges.AsNoTracking()
            .Where(c => c.ThreePlBillingRunId == runId)
            .OrderBy(c => c.ChargeType)
            .ThenBy(c => c.SourceCode)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("RunCode,Warehouse,Owner,ChargeType,SourceType,SourceId,SourceCode,Quantity,ChargeUnit,UnitRate,Amount,Currency,Status");
        foreach (var c in charges)
        {
            var values = new[]
            {
                run.RunCode,
                run.Warehouse?.WarehouseCode ?? run.WarehouseId.ToString(CultureInfo.InvariantCulture),
                run.OwnerPartner?.PartnerCode ?? run.OwnerPartnerId.ToString(CultureInfo.InvariantCulture),
                c.ChargeType.ToString(),
                c.SourceType,
                c.SourceId ?? "",
                c.SourceCode ?? "",
                c.Quantity.ToString(CultureInfo.InvariantCulture),
                c.ChargeUnit,
                c.UnitRate.ToString(CultureInfo.InvariantCulture),
                c.Amount.ToString(CultureInfo.InvariantCulture),
                c.Currency,
                c.Status.ToString()
            };
            sb.AppendLine(string.Join(",", values.Select(EscapeCsv)));
        }
        return (run.RunCode, sb.ToString(), charges);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
