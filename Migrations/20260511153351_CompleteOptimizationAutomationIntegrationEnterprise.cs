using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class CompleteOptimizationAutomationIntegrationEnterprise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutomationOverrides",
                columns: table => new
                {
                    AutomationOverrideId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MheCommandId = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<byte>(type: "tinyint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationOverrides", x => x.AutomationOverrideId);
                    table.ForeignKey(
                        name: "FK_AutomationOverrides_MheCommands_MheCommandId",
                        column: x => x.MheCommandId,
                        principalTable: "MheCommands",
                        principalColumn: "MheCommandId");
                });

            migrationBuilder.CreateTable(
                name: "EdiMessages",
                columns: table => new
                {
                    EdiMessageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageType = table.Column<byte>(type: "tinyint", nullable: false),
                    Direction = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: true),
                    PartnerId = table.Column<int>(type: "int", nullable: true),
                    ControlNumber = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SourceFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidationErrorsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RejectReport = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReplayOfMessageId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReplayedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EdiMessages", x => x.EdiMessageId);
                    table.ForeignKey(
                        name: "FK_EdiMessages_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                    table.ForeignKey(
                        name: "FK_EdiMessages_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "EnterpriseConnectors",
                columns: table => new
                {
                    EnterpriseConnectorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConnectorType = table.Column<byte>(type: "tinyint", nullable: false),
                    ConnectorCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ConnectorName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EndpointUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SecretReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsMock = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    HealthStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    LastHealthCheckAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnterpriseConnectors", x => x.EnterpriseConnectorId);
                });

            migrationBuilder.CreateTable(
                name: "MheAdapterProfiles",
                columns: table => new
                {
                    MheAdapterProfileId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    MheSystemId = table.Column<int>(type: "int", nullable: true),
                    AdapterCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AdapterName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AdapterType = table.Column<byte>(type: "tinyint", nullable: false),
                    IsSimulator = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CapabilitiesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HealthStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LastHeartbeatAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MheAdapterProfiles", x => x.MheAdapterProfileId);
                    table.ForeignKey(
                        name: "FK_MheAdapterProfiles_MheSystems_MheSystemId",
                        column: x => x.MheSystemId,
                        principalTable: "MheSystems",
                        principalColumn: "MheSystemId");
                    table.ForeignKey(
                        name: "FK_MheAdapterProfiles_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "OptimizationRuns",
                columns: table => new
                {
                    OptimizationRunId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: true),
                    RunType = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    ScopeKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    BeforeDistance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AfterDistance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    EstimatedMinutesSaved = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CandidateCount = table.Column<int>(type: "int", nullable: false),
                    ReleasedTaskCount = table.Column<int>(type: "int", nullable: false),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptimizationRuns", x => x.OptimizationRunId);
                    table.ForeignKey(
                        name: "FK_OptimizationRuns_Partners_OwnerPartnerId",
                        column: x => x.OwnerPartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                    table.ForeignKey(
                        name: "FK_OptimizationRuns_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "PickPathPlans",
                columns: table => new
                {
                    PickPathPlanId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: true),
                    BeforeDistance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AfterDistance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DistanceSaved = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    StopCount = table.Column<int>(type: "int", nullable: false),
                    PickTaskIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickPathPlans", x => x.PickPathPlanId);
                    table.ForeignKey(
                        name: "FK_PickPathPlans_Partners_OwnerPartnerId",
                        column: x => x.OwnerPartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                    table.ForeignKey(
                        name: "FK_PickPathPlans_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "ToteClusterPlans",
                columns: table => new
                {
                    ToteClusterPlanId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: true),
                    CustomerKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CarrierCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RouteCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequiresToteScan = table.Column<bool>(type: "bit", nullable: false),
                    AssignmentCount = table.Column<int>(type: "int", nullable: false),
                    StatusText = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToteClusterPlans", x => x.ToteClusterPlanId);
                    table.ForeignKey(
                        name: "FK_ToteClusterPlans_Partners_OwnerPartnerId",
                        column: x => x.OwnerPartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                    table.ForeignKey(
                        name: "FK_ToteClusterPlans_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WavelessReleaseQueue",
                columns: table => new
                {
                    WavelessReleaseQueueId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: true),
                    VoucherId = table.Column<long>(type: "bigint", nullable: true),
                    PickTaskId = table.Column<long>(type: "bigint", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    PriorityScore = table.Column<int>(type: "int", nullable: false),
                    SlaDueAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InventoryAvailable = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    BlockReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReleasedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WavelessReleaseQueue", x => x.WavelessReleaseQueueId);
                    table.ForeignKey(
                        name: "FK_WavelessReleaseQueue_Partners_OwnerPartnerId",
                        column: x => x.OwnerPartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                    table.ForeignKey(
                        name: "FK_WavelessReleaseQueue_PickTasks_PickTaskId",
                        column: x => x.PickTaskId,
                        principalTable: "PickTasks",
                        principalColumn: "PickTaskId");
                    table.ForeignKey(
                        name: "FK_WavelessReleaseQueue_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "WcsSimulatorRuns",
                columns: table => new
                {
                    WcsSimulatorRunId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    MheSystemId = table.Column<int>(type: "int", nullable: true),
                    Scenario = table.Column<byte>(type: "tinyint", nullable: false),
                    StatusText = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CommandsCreated = table.Column<int>(type: "int", nullable: false),
                    CallbacksSent = table.Column<int>(type: "int", nullable: false),
                    ExceptionsOpened = table.Column<int>(type: "int", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WcsSimulatorRuns", x => x.WcsSimulatorRunId);
                    table.ForeignKey(
                        name: "FK_WcsSimulatorRuns_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebhookSubscriptions",
                columns: table => new
                {
                    WebhookSubscriptionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubscriptionCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TargetUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SigningSecret = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MaxRetries = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookSubscriptions", x => x.WebhookSubscriptionId);
                });

            migrationBuilder.CreateTable(
                name: "EnterpriseConnectorDeliveries",
                columns: table => new
                {
                    EnterpriseConnectorDeliveryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnterpriseConnectorId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnterpriseConnectorDeliveries", x => x.EnterpriseConnectorDeliveryId);
                    table.ForeignKey(
                        name: "FK_EnterpriseConnectorDeliveries_EnterpriseConnectors_EnterpriseConnectorId",
                        column: x => x.EnterpriseConnectorId,
                        principalTable: "EnterpriseConnectors",
                        principalColumn: "EnterpriseConnectorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MheTelemetryEvents",
                columns: table => new
                {
                    MheTelemetryEventId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    MheSystemId = table.Column<int>(type: "int", nullable: true),
                    MheAdapterProfileId = table.Column<int>(type: "int", nullable: true),
                    TelemetryType = table.Column<byte>(type: "tinyint", nullable: false),
                    EquipmentCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    StatusText = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ThroughputPerHour = table.Column<int>(type: "int", nullable: false),
                    DowntimeMinutes = table.Column<int>(type: "int", nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EventAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MheTelemetryEvents", x => x.MheTelemetryEventId);
                    table.ForeignKey(
                        name: "FK_MheTelemetryEvents_MheAdapterProfiles_MheAdapterProfileId",
                        column: x => x.MheAdapterProfileId,
                        principalTable: "MheAdapterProfiles",
                        principalColumn: "MheAdapterProfileId");
                    table.ForeignKey(
                        name: "FK_MheTelemetryEvents_MheSystems_MheSystemId",
                        column: x => x.MheSystemId,
                        principalTable: "MheSystems",
                        principalColumn: "MheSystemId");
                    table.ForeignKey(
                        name: "FK_MheTelemetryEvents_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "OptimizationRecommendationLines",
                columns: table => new
                {
                    OptimizationRecommendationLineId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OptimizationRunId = table.Column<long>(type: "bigint", nullable: false),
                    LineType = table.Column<byte>(type: "tinyint", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: true),
                    ItemId = table.Column<int>(type: "int", nullable: true),
                    VoucherId = table.Column<long>(type: "bigint", nullable: true),
                    WaveId = table.Column<long>(type: "bigint", nullable: true),
                    PickTaskId = table.Column<long>(type: "bigint", nullable: true),
                    SourceLocationId = table.Column<int>(type: "int", nullable: true),
                    SuggestedLocationId = table.Column<int>(type: "int", nullable: true),
                    GroupKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    BeforeDistance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AfterDistance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    EstimatedMinutesSaved = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    InventoryAvailable = table.Column<bool>(type: "bit", nullable: false),
                    RequiresToteScan = table.Column<bool>(type: "bit", nullable: false),
                    IsOwnerSafe = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StatusText = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptimizationRecommendationLines", x => x.OptimizationRecommendationLineId);
                    table.ForeignKey(
                        name: "FK_OptimizationRecommendationLines_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId");
                    table.ForeignKey(
                        name: "FK_OptimizationRecommendationLines_Locations_SourceLocationId",
                        column: x => x.SourceLocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_OptimizationRecommendationLines_Locations_SuggestedLocationId",
                        column: x => x.SuggestedLocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_OptimizationRecommendationLines_OptimizationRuns_OptimizationRunId",
                        column: x => x.OptimizationRunId,
                        principalTable: "OptimizationRuns",
                        principalColumn: "OptimizationRunId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OptimizationRecommendationLines_Partners_OwnerPartnerId",
                        column: x => x.OwnerPartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                    table.ForeignKey(
                        name: "FK_OptimizationRecommendationLines_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "PickPathPlanStops",
                columns: table => new
                {
                    PickPathPlanStopId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PickPathPlanId = table.Column<long>(type: "bigint", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    PickTaskId = table.Column<long>(type: "bigint", nullable: true),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    ToteCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    DistanceFromPrevious = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickPathPlanStops", x => x.PickPathPlanStopId);
                    table.ForeignKey(
                        name: "FK_PickPathPlanStops_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_PickPathPlanStops_PickPathPlans_PickPathPlanId",
                        column: x => x.PickPathPlanId,
                        principalTable: "PickPathPlans",
                        principalColumn: "PickPathPlanId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PickPathPlanStops_PickTasks_PickTaskId",
                        column: x => x.PickTaskId,
                        principalTable: "PickTasks",
                        principalColumn: "PickTaskId");
                });

            migrationBuilder.CreateTable(
                name: "ToteClusterAssignments",
                columns: table => new
                {
                    ToteClusterAssignmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ToteClusterPlanId = table.Column<long>(type: "bigint", nullable: false),
                    PickToteId = table.Column<long>(type: "bigint", nullable: true),
                    ToteCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    VoucherId = table.Column<long>(type: "bigint", nullable: true),
                    PickTaskId = table.Column<long>(type: "bigint", nullable: true),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: true),
                    CustomerKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    IsScanned = table.Column<bool>(type: "bit", nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToteClusterAssignments", x => x.ToteClusterAssignmentId);
                    table.ForeignKey(
                        name: "FK_ToteClusterAssignments_Partners_OwnerPartnerId",
                        column: x => x.OwnerPartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                    table.ForeignKey(
                        name: "FK_ToteClusterAssignments_PickTasks_PickTaskId",
                        column: x => x.PickTaskId,
                        principalTable: "PickTasks",
                        principalColumn: "PickTaskId");
                    table.ForeignKey(
                        name: "FK_ToteClusterAssignments_PickTotes_PickToteId",
                        column: x => x.PickToteId,
                        principalTable: "PickTotes",
                        principalColumn: "PickToteId");
                    table.ForeignKey(
                        name: "FK_ToteClusterAssignments_ToteClusterPlans_ToteClusterPlanId",
                        column: x => x.ToteClusterPlanId,
                        principalTable: "ToteClusterPlans",
                        principalColumn: "ToteClusterPlanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebhookDeliveries",
                columns: table => new
                {
                    WebhookDeliveryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WebhookSubscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Signature = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookDeliveries", x => x.WebhookDeliveryId);
                    table.ForeignKey(
                        name: "FK_WebhookDeliveries_WebhookSubscriptions_WebhookSubscriptionId",
                        column: x => x.WebhookSubscriptionId,
                        principalTable: "WebhookSubscriptions",
                        principalColumn: "WebhookSubscriptionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationOverrides_MheCommandId",
                table: "AutomationOverrides",
                column: "MheCommandId");

            migrationBuilder.CreateIndex(
                name: "IX_EdiMessages_MessageType_Direction_ControlNumber",
                table: "EdiMessages",
                columns: new[] { "MessageType", "Direction", "ControlNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EdiMessages_PartnerId",
                table: "EdiMessages",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_EdiMessages_WarehouseId",
                table: "EdiMessages",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseConnectorDeliveries_EnterpriseConnectorId",
                table: "EnterpriseConnectorDeliveries",
                column: "EnterpriseConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseConnectorDeliveries_IdempotencyKey",
                table: "EnterpriseConnectorDeliveries",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseConnectors_ConnectorType_ConnectorCode",
                table: "EnterpriseConnectors",
                columns: new[] { "ConnectorType", "ConnectorCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MheAdapterProfiles_MheSystemId",
                table: "MheAdapterProfiles",
                column: "MheSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_MheAdapterProfiles_WarehouseId_AdapterCode",
                table: "MheAdapterProfiles",
                columns: new[] { "WarehouseId", "AdapterCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MheTelemetry_Warehouse_Equipment_Date",
                table: "MheTelemetryEvents",
                columns: new[] { "WarehouseId", "EquipmentCode", "EventAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MheTelemetryEvents_MheAdapterProfileId",
                table: "MheTelemetryEvents",
                column: "MheAdapterProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_MheTelemetryEvents_MheSystemId",
                table: "MheTelemetryEvents",
                column: "MheSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationLines_Run_Type_Score",
                table: "OptimizationRecommendationLines",
                columns: new[] { "OptimizationRunId", "LineType", "Score" });

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationRecommendationLines_ItemId",
                table: "OptimizationRecommendationLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationRecommendationLines_OwnerPartnerId",
                table: "OptimizationRecommendationLines",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationRecommendationLines_SourceLocationId",
                table: "OptimizationRecommendationLines",
                column: "SourceLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationRecommendationLines_SuggestedLocationId",
                table: "OptimizationRecommendationLines",
                column: "SuggestedLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationRecommendationLines_WarehouseId",
                table: "OptimizationRecommendationLines",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationRuns_OwnerPartnerId",
                table: "OptimizationRuns",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationRuns_Warehouse_Type_Status_Date",
                table: "OptimizationRuns",
                columns: new[] { "WarehouseId", "RunType", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PickPathPlans_OwnerPartnerId",
                table: "PickPathPlans",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PickPathPlans_Warehouse_Date",
                table: "PickPathPlans",
                columns: new[] { "WarehouseId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PickPathPlanStops_LocationId",
                table: "PickPathPlanStops",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PickPathPlanStops_PickPathPlanId",
                table: "PickPathPlanStops",
                column: "PickPathPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PickPathPlanStops_PickTaskId",
                table: "PickPathPlanStops",
                column: "PickTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ToteClusterAssignments_OwnerPartnerId",
                table: "ToteClusterAssignments",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ToteClusterAssignments_PickTaskId",
                table: "ToteClusterAssignments",
                column: "PickTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ToteClusterAssignments_PickToteId",
                table: "ToteClusterAssignments",
                column: "PickToteId");

            migrationBuilder.CreateIndex(
                name: "IX_ToteClusterAssignments_ToteClusterPlanId",
                table: "ToteClusterAssignments",
                column: "ToteClusterPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ToteClusterPlans_OwnerPartnerId",
                table: "ToteClusterPlans",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ToteClusterPlans_Scope_Date",
                table: "ToteClusterPlans",
                columns: new[] { "WarehouseId", "OwnerPartnerId", "CustomerKey", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WavelessQueue_Warehouse_Status_Priority",
                table: "WavelessReleaseQueue",
                columns: new[] { "WarehouseId", "Status", "PriorityScore" });

            migrationBuilder.CreateIndex(
                name: "IX_WavelessReleaseQueue_IdempotencyKey",
                table: "WavelessReleaseQueue",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WavelessReleaseQueue_OwnerPartnerId",
                table: "WavelessReleaseQueue",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_WavelessReleaseQueue_PickTaskId",
                table: "WavelessReleaseQueue",
                column: "PickTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_WcsSimulatorRuns_WarehouseId",
                table: "WcsSimulatorRuns",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_IdempotencyKey",
                table: "WebhookDeliveries",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_WebhookSubscriptionId",
                table: "WebhookDeliveries",
                column: "WebhookSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookSubscriptions_SubscriptionCode",
                table: "WebhookSubscriptions",
                column: "SubscriptionCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutomationOverrides");

            migrationBuilder.DropTable(
                name: "EdiMessages");

            migrationBuilder.DropTable(
                name: "EnterpriseConnectorDeliveries");

            migrationBuilder.DropTable(
                name: "MheTelemetryEvents");

            migrationBuilder.DropTable(
                name: "OptimizationRecommendationLines");

            migrationBuilder.DropTable(
                name: "PickPathPlanStops");

            migrationBuilder.DropTable(
                name: "ToteClusterAssignments");

            migrationBuilder.DropTable(
                name: "WavelessReleaseQueue");

            migrationBuilder.DropTable(
                name: "WcsSimulatorRuns");

            migrationBuilder.DropTable(
                name: "WebhookDeliveries");

            migrationBuilder.DropTable(
                name: "EnterpriseConnectors");

            migrationBuilder.DropTable(
                name: "MheAdapterProfiles");

            migrationBuilder.DropTable(
                name: "OptimizationRuns");

            migrationBuilder.DropTable(
                name: "PickPathPlans");

            migrationBuilder.DropTable(
                name: "ToteClusterPlans");

            migrationBuilder.DropTable(
                name: "WebhookSubscriptions");
        }
    }
}
