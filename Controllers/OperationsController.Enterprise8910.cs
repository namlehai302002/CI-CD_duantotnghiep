using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Models;
using WMS.ViewModels;

namespace WMS.Controllers;

public partial class OperationsController
{
    [Authorize(Roles = "Admin,Manager,Staff,Viewer")]
    [HttpGet]
    public async Task<IActionResult> OptimizationDashboard(int? warehouseId = null)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
            warehouseId = scopedWh.Value;
        var snapshot = await _optimizationEnterpriseService.GetDashboardAsync(warehouseId, scopedWh);
        var vm = new OptimizationEnterpriseDashboardViewModel
        {
            WarehouseId = warehouseId,
            Warehouses = await _db.Warehouses.AsNoTracking().OrderBy(x => x.WarehouseCode).ToListAsync(),
            Runs = snapshot.Runs,
            Recommendations = snapshot.Recommendations,
            WavelessQueue = snapshot.WavelessQueue,
            PickPathPlans = snapshot.PickPathPlans,
            ToteClusterPlans = snapshot.ToteClusterPlans
        };
        return View(vm);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunSlottingOptimization(int warehouseId)
    {
        await ExecuteOptimizationActionAsync(() => _optimizationEnterpriseService.RunSlottingOptimizationAsync(warehouseId, GetScopedWarehouseId(), User.Identity?.Name ?? "system"), "Đã chạy tối ưu vị trí.");
        return RedirectToAction(nameof(OptimizationDashboard), new { warehouseId });
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunWaveOptimization(int warehouseId)
    {
        await ExecuteOptimizationActionAsync(() => _optimizationEnterpriseService.RunWaveOptimizationAsync(warehouseId, GetScopedWarehouseId(), User.Identity?.Name ?? "system"), "Đã chạy lập đợt lấy hàng.");
        return RedirectToAction(nameof(OptimizationDashboard), new { warehouseId });
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunWavelessRelease(int warehouseId, int maxTasks = 50)
    {
        try
        {
            var count = await _optimizationEnterpriseService.RunWavelessReleaseAsync(warehouseId, maxTasks, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            TempData["Success"] = $"Đã phát {count} nhiệm vụ trực tiếp.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(OptimizationDashboard), new { warehouseId });
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GeneratePickPathPlan(int warehouseId)
    {
        try
        {
            var plan = await _optimizationEnterpriseService.GeneratePickPathPlanAsync(warehouseId, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            TempData["Success"] = $"Đã tạo đường lấy hàng {plan.PlanCode}, giảm {plan.DistanceSaved:N2} quãng đường.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(OptimizationDashboard), new { warehouseId });
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateToteClusterPlan(int warehouseId)
    {
        try
        {
            var plan = await _optimizationEnterpriseService.CreateToteClusterPlanAsync(warehouseId, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            TempData["Success"] = $"Đã tạo nhóm thùng lấy hàng {plan.PlanCode} với {plan.AssignmentCount} dòng gán.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(OptimizationDashboard), new { warehouseId });
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> ExportOptimizationLines(int? warehouseId)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
            warehouseId = scopedWh.Value;
        var rows = await _db.OptimizationRecommendationLines.AsNoTracking()
            .Include(x => x.Item)
            .Include(x => x.SourceLocation)
            .Include(x => x.SuggestedLocation)
            .Where(x => !warehouseId.HasValue || x.WarehouseId == warehouseId.Value)
            .OrderByDescending(x => x.Score)
            .Take(5000)
            .ToListAsync();
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Optimization");
        var headers = new[] { "Type", "Item", "Source", "Suggested", "Group", "Score", "Qty", "Before", "After", "Saved", "Reason", "Status" };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            ws.Cell(r + 2, 1).Value = row.LineType.ToString();
            ws.Cell(r + 2, 2).Value = row.Item?.ItemCode;
            ws.Cell(r + 2, 3).Value = row.SourceLocation?.LocationCode;
            ws.Cell(r + 2, 4).Value = row.SuggestedLocation?.LocationCode;
            ws.Cell(r + 2, 5).Value = row.GroupKey;
            ws.Cell(r + 2, 6).Value = row.Score;
            ws.Cell(r + 2, 7).Value = row.Quantity;
            ws.Cell(r + 2, 8).Value = row.BeforeDistance;
            ws.Cell(r + 2, 9).Value = row.AfterDistance;
            ws.Cell(r + 2, 10).Value = row.EstimatedMinutesSaved;
            ws.Cell(r + 2, 11).Value = row.Reason;
            ws.Cell(r + 2, 12).Value = row.StatusText;
        }
        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"optimization-{VietnamNow:yyyyMMddHHmmss}.xlsx");
    }

    [Authorize(Roles = "Admin,Manager,Staff,Viewer")]
    [HttpGet]
    public async Task<IActionResult> AutomationDashboard(int? warehouseId = null)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
            warehouseId = scopedWh.Value;
        var vm = new AutomationEnterpriseDashboardViewModel
        {
            WarehouseId = warehouseId,
            Warehouses = await _db.Warehouses.AsNoTracking().OrderBy(x => x.WarehouseCode).ToListAsync(),
            AdapterProfiles = await _db.MheAdapterProfiles.AsNoTracking().Where(x => !warehouseId.HasValue || x.WarehouseId == warehouseId.Value).OrderBy(x => x.AdapterCode).ToListAsync(),
            TelemetryEvents = await _db.MheTelemetryEvents.AsNoTracking().Where(x => !warehouseId.HasValue || x.WarehouseId == warehouseId.Value).OrderByDescending(x => x.EventAt).Take(100).ToListAsync(),
            SimulatorRuns = await _db.WcsSimulatorRuns.AsNoTracking().Where(x => !warehouseId.HasValue || x.WarehouseId == warehouseId.Value).OrderByDescending(x => x.CreatedAt).Take(30).ToListAsync(),
            Commands = await _db.MheCommands.AsNoTracking().Include(x => x.MheSystem).Where(x => !warehouseId.HasValue || x.WarehouseId == warehouseId.Value).OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(),
            Overrides = await _db.AutomationOverrides.AsNoTracking().Include(x => x.MheCommand).OrderByDescending(x => x.ApprovedAt).Take(50).ToListAsync()
        };
        return View(vm);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMheAdapterProfile(int warehouseId, MheSystemTypeEnum adapterType, string adapterCode, string adapterName, bool isSimulator = false)
    {
        try
        {
            await _automationEnterpriseService.SaveAdapterProfileAsync(warehouseId, adapterType, adapterCode, adapterName, isSimulator, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            TempData["Success"] = "Đã lưu bộ kết nối thiết bị.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(AutomationDashboard), new { warehouseId });
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordMheTelemetry(int warehouseId, string equipmentCode, AutomationTelemetryTypeEnum telemetryType, string statusText, int throughputPerHour = 0, int downtimeMinutes = 0, string? errorCode = null, string? message = null)
    {
        try
        {
            await _automationEnterpriseService.RecordTelemetryAsync(warehouseId, equipmentCode, telemetryType, statusText, throughputPerHour, downtimeMinutes, errorCode, message, GetScopedWarehouseId());
            TempData["Success"] = "Đã ghi tín hiệu thiết bị.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(AutomationDashboard), new { warehouseId });
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunWcsSimulator(int warehouseId, WcsSimulatorScenarioEnum scenario)
    {
        try
        {
            var run = await _automationEnterpriseService.RunWcsSimulatorAsync(warehouseId, scenario, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            TempData["Success"] = $"Mo phong {run.Scenario} tao {run.CommandsCreated} lenh, ngoai le {run.ExceptionsOpened}.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(AutomationDashboard), new { warehouseId });
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OverrideMheCommand(long commandId, AutomationOverrideActionEnum action, string reason, int? warehouseId = null)
    {
        try
        {
            var row = await _automationEnterpriseService.OverrideMheCommandAsync(commandId, action, reason, GetScopedWarehouseId(), User.Identity?.Name ?? "system");
            warehouseId = row.MheCommand.WarehouseId;
            TempData["Success"] = "Đã ghi đè có lý do.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(AutomationDashboard), new { warehouseId });
    }

    [Authorize(Roles = "Admin,Manager,Staff,Viewer")]
    [HttpGet]
    public async Task<IActionResult> IntegrationDashboard()
    {
        var vm = new IntegrationEnterpriseDashboardViewModel
        {
            EdiMessages = await _db.EdiMessages.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(),
            WebhookSubscriptions = await _db.WebhookSubscriptions.AsNoTracking().OrderBy(x => x.EventType).ToListAsync(),
            WebhookDeliveries = await _db.WebhookDeliveries.AsNoTracking().Include(x => x.Subscription).OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(),
            Connectors = await _db.EnterpriseConnectors.AsNoTracking().OrderBy(x => x.ConnectorType).ThenBy(x => x.ConnectorCode).ToListAsync(),
            ConnectorDeliveries = await _db.EnterpriseConnectorDeliveries.AsNoTracking().Include(x => x.Connector).OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(),
            Outbox = await _db.IntegrationOutbox.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync()
        };
        return View(vm);
    }

    [Authorize(Roles = "Admin,Manager,Staff,Viewer")]
    [HttpGet]
    public IActionResult IntegrationOpenApiContract()
        => Json(_enterpriseIntegrationService.BuildOpenApiContract());

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportEdiMessage(EdiMessageTypeEnum messageType, string payload, int? warehouseId = null, int? partnerId = null, string? fileName = null)
    {
        try
        {
            var message = await _enterpriseIntegrationService.ImportEdiAsync(messageType, payload, fileName, warehouseId, partnerId, User.Identity?.Name ?? "system");
            TempData[message.Status == EdiMessageStatusEnum.Rejected ? "Error" : "Success"] = message.Status == EdiMessageStatusEnum.Rejected ? message.RejectReport : "Đã nhập thông điệp trao đổi dữ liệu hợp lệ.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(IntegrationDashboard));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportEdiMessage(EdiMessageTypeEnum messageType, long? referenceId = null, int? warehouseId = null, int? partnerId = null)
    {
        await ExecuteIntegrationActionAsync(() => _enterpriseIntegrationService.ExportEdiAsync(messageType, referenceId, warehouseId, partnerId, User.Identity?.Name ?? "system"), "Đã xuất thông điệp trao đổi dữ liệu.");
        return RedirectToAction(nameof(IntegrationDashboard));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplayEdiMessage(long ediMessageId)
    {
        await ExecuteIntegrationActionAsync(() => _enterpriseIntegrationService.ReplayEdiAsync(ediMessageId, User.Identity?.Name ?? "system"), "Đã phát lại thông điệp trao đổi dữ liệu.");
        return RedirectToAction(nameof(IntegrationDashboard));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveWebhookSubscription(string eventType, string targetUrl, string signingSecret)
    {
        await ExecuteIntegrationActionAsync(() => _enterpriseIntegrationService.SaveWebhookSubscriptionAsync(eventType, targetUrl, signingSecret, User.Identity?.Name ?? "system"), "Đã lưu điểm nhận tự động.");
        return RedirectToAction(nameof(IntegrationDashboard));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplayWebhookDelivery(long webhookDeliveryId)
    {
        await ExecuteIntegrationActionAsync(() => _enterpriseIntegrationService.ReplayWebhookAsync(webhookDeliveryId, User.Identity?.Name ?? "system"), "Đã phát lại lần gửi đến điểm nhận tự động.");
        return RedirectToAction(nameof(IntegrationDashboard));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnsureEnterpriseConnectorPack()
    {
        try
        {
            var connectors = await _enterpriseIntegrationService.EnsureConnectorPackAsync(User.Identity?.Name ?? "system");
            TempData["Success"] = $"Đã sẵn sàng {connectors.Count} bộ kết nối mô phỏng.";
        }
        catch (Exception ex) when (ex is BusinessRuleException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
        return RedirectToAction(nameof(IntegrationDashboard));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckEnterpriseConnectorHealth(int connectorId)
    {
        await ExecuteIntegrationActionAsync(() => _enterpriseIntegrationService.CheckConnectorHealthAsync(connectorId, User.Identity?.Name ?? "system"), "Đã kiểm tra trạng thái bộ kết nối.");
        return RedirectToAction(nameof(IntegrationDashboard));
    }

    private async Task ExecuteOptimizationActionAsync(Func<Task<OptimizationRun>> action, string successMessage)
    {
        try
        {
            await action();
            TempData["Success"] = successMessage;
        }
        catch (Exception ex) when (ex is BusinessRuleException or UnauthorizedAccessException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
    }

    private async Task ExecuteIntegrationActionAsync<T>(Func<Task<T>> action, string successMessage)
    {
        try
        {
            await action();
            TempData["Success"] = successMessage;
        }
        catch (Exception ex) when (ex is BusinessRuleException or DbUpdateException)
        {
            TempData["Error"] = UserSafeError.From(ex);
        }
    }
}
