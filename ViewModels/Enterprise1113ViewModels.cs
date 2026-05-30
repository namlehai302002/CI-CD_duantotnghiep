using WMS.Models;

namespace WMS.ViewModels;

public sealed class SemanticBiDashboardViewModel
{
    public int? WarehouseId { get; set; }
    public int Days { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<SemanticMetricDefinition> Definitions { get; set; } = new();
    public List<SemanticMetricSnapshot> Snapshots { get; set; } = new();
    public bool CanSeeFinancial { get; set; }
}

public sealed class FinancialCostDashboardViewModel
{
    public int? WarehouseId { get; set; }
    public int Days { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<FinancialCostRow> Rows { get; set; } = new();
    public decimal TotalCost { get; set; }
}

public sealed class FinancialCostRow
{
    public string OwnerName { get; set; } = "";
    public string WarehouseName { get; set; } = "";
    public string ServiceType { get; set; } = "";
    public string SourceType { get; set; } = "";
    public string SourceCode { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
}

public sealed class PredictiveAlertsViewModel
{
    public int? WarehouseId { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<EnterprisePredictiveAlert> Alerts { get; set; } = new();
}

public sealed class AuditAnalyticsViewModel
{
    public List<AuditAnalyticsFinding> Findings { get; set; } = new();
    public int SensitiveExportCount { get; set; }
    public int OutOfHoursCount { get; set; }
    public int ScopeDeniedCount { get; set; }
    public int AbnormalMutationCount { get; set; }
}

public sealed class AiAssistantViewModel
{
    public long? SessionId { get; set; }
    public string RoleName { get; set; } = "";
    public string ScopeSummary { get; set; } = "";
    public List<AiAssistantMessage> Messages { get; set; } = new();
}

public sealed class RoleWorkspaceViewModel
{
    public string RoleKey { get; set; } = "Viewer";
    public string RoleLabel { get; set; } = "Chỉ xem";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public List<RoleWorkspaceAction> QuickActions { get; set; } = new();
    public List<string> VisibleSections { get; set; } = new();
}

public sealed class RoleWorkspaceAction
{
    public string Label { get; set; } = "";
    public string Url { get; set; } = "";
    public string Icon { get; set; } = "fa-circle";
    public string Variant { get; set; } = "secondary";
}

public sealed class WorkflowProfilesViewModel
{
    public int? WarehouseId { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<Partner> Owners { get; set; } = new();
    public List<WarehouseWorkflowProfile> Profiles { get; set; } = new();
}

public sealed class SreDashboardViewModel
{
    public int PeriodMinutes { get; set; }
    public SreMetricSnapshot Snapshot { get; set; } = new();
    public List<SreMetricSnapshot> RecentSnapshots { get; set; } = new();
    public List<RequestTelemetryLog> RecentRequests { get; set; } = new();
    public List<IntegrationOutbox> OutboxRows { get; set; } = new();
    public List<WebhookDelivery> WebhookFailures { get; set; } = new();
}
