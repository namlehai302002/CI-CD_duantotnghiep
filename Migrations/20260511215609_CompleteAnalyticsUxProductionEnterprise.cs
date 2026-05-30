using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class CompleteAnalyticsUxProductionEnterprise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiAssistantSessions",
                columns: table => new
                {
                    AiAssistantSessionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: true),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: true),
                    Purpose = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAssistantSessions", x => x.AiAssistantSessionId);
                });

            migrationBuilder.CreateTable(
                name: "AuditAnalyticsFindings",
                columns: table => new
                {
                    AuditAnalyticsFindingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FindingType = table.Column<byte>(type: "tinyint", nullable: false),
                    Severity = table.Column<byte>(type: "tinyint", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: true),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: true),
                    ReferenceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ReferenceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EvidenceJson = table.Column<string>(type: "nvarchar(1600)", maxLength: 1600, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditAnalyticsFindings", x => x.AuditAnalyticsFindingId);
                });

            migrationBuilder.CreateTable(
                name: "EnterprisePredictiveAlerts",
                columns: table => new
                {
                    EnterprisePredictiveAlertId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlertType = table.Column<byte>(type: "tinyint", nullable: false),
                    Severity = table.Column<byte>(type: "tinyint", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: true),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: true),
                    ReferenceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ReferenceId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false),
                    RiskScore = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    ForecastFor = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CitationJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnterprisePredictiveAlerts", x => x.EnterprisePredictiveAlertId);
                    table.ForeignKey(
                        name: "FK_EnterprisePredictiveAlerts_Partners_OwnerPartnerId",
                        column: x => x.OwnerPartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                    table.ForeignKey(
                        name: "FK_EnterprisePredictiveAlerts_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "RequestTelemetryLogs",
                columns: table => new
                {
                    RequestTelemetryLogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CorrelationId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: true),
                    IsError = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestTelemetryLogs", x => x.RequestTelemetryLogId);
                });

            migrationBuilder.CreateTable(
                name: "SemanticMetricDefinitions",
                columns: table => new
                {
                    SemanticMetricDefinitionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MetricCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    MetricName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<byte>(type: "tinyint", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Formula = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SourceLabel = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsFinancial = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemanticMetricDefinitions", x => x.SemanticMetricDefinitionId);
                });

            migrationBuilder.CreateTable(
                name: "SreMetricSnapshots",
                columns: table => new
                {
                    SreMetricSnapshotId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SnapshotAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodMinutes = table.Column<int>(type: "int", nullable: false),
                    AverageLatencyMs = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    P95LatencyMs = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ErrorRatePercent = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    RequestCount = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    QueueDepth = table.Column<int>(type: "int", nullable: false),
                    DeadLetterCount = table.Column<int>(type: "int", nullable: false),
                    ScanRetryCount = table.Column<int>(type: "int", nullable: false),
                    CarrierFailureCount = table.Column<int>(type: "int", nullable: false),
                    WebhookFailureCount = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SreMetricSnapshots", x => x.SreMetricSnapshotId);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseWorkflowProfiles",
                columns: table => new
                {
                    WarehouseWorkflowProfileId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: true),
                    ModuleKey = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ProfileName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    RequireLocationScan = table.Column<bool>(type: "bit", nullable: false),
                    RequireItemScan = table.Column<bool>(type: "bit", nullable: false),
                    RequireToteScan = table.Column<bool>(type: "bit", nullable: false),
                    RequireSerialScan = table.Column<bool>(type: "bit", nullable: false),
                    RequireQc = table.Column<bool>(type: "bit", nullable: false),
                    RequireApproval = table.Column<bool>(type: "bit", nullable: false),
                    RequirePacking = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseWorkflowProfiles", x => x.WarehouseWorkflowProfileId);
                    table.ForeignKey(
                        name: "FK_WarehouseWorkflowProfiles_Partners_OwnerPartnerId",
                        column: x => x.OwnerPartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                    table.ForeignKey(
                        name: "FK_WarehouseWorkflowProfiles_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiAssistantMessages",
                columns: table => new
                {
                    AiAssistantMessageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AiAssistantSessionId = table.Column<long>(type: "bigint", nullable: false),
                    MessageRole = table.Column<byte>(type: "tinyint", nullable: false),
                    Prompt = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Response = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsMutationBlocked = table.Column<bool>(type: "bit", nullable: false),
                    ScopeSummary = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAssistantMessages", x => x.AiAssistantMessageId);
                    table.ForeignKey(
                        name: "FK_AiAssistantMessages_AiAssistantSessions_AiAssistantSessionId",
                        column: x => x.AiAssistantSessionId,
                        principalTable: "AiAssistantSessions",
                        principalColumn: "AiAssistantSessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SemanticMetricSnapshots",
                columns: table => new
                {
                    SemanticMetricSnapshotId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SemanticMetricDefinitionId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: true),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: true),
                    MetricDate = table.Column<DateTime>(type: "date", nullable: false),
                    MetricValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ScopeKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceCitation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SourceCount = table.Column<int>(type: "int", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemanticMetricSnapshots", x => x.SemanticMetricSnapshotId);
                    table.ForeignKey(
                        name: "FK_SemanticMetricSnapshots_Partners_OwnerPartnerId",
                        column: x => x.OwnerPartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                    table.ForeignKey(
                        name: "FK_SemanticMetricSnapshots_SemanticMetricDefinitions_SemanticMetricDefinitionId",
                        column: x => x.SemanticMetricDefinitionId,
                        principalTable: "SemanticMetricDefinitions",
                        principalColumn: "SemanticMetricDefinitionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SemanticMetricSnapshots_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "AiAssistantCitations",
                columns: table => new
                {
                    AiAssistantCitationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AiAssistantMessageId = table.Column<long>(type: "bigint", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceLabel = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Excerpt = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAssistantCitations", x => x.AiAssistantCitationId);
                    table.ForeignKey(
                        name: "FK_AiAssistantCitations_AiAssistantMessages_AiAssistantMessageId",
                        column: x => x.AiAssistantMessageId,
                        principalTable: "AiAssistantMessages",
                        principalColumn: "AiAssistantMessageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiAssistantCitations_AiAssistantMessageId",
                table: "AiAssistantCitations",
                column: "AiAssistantMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_AiAssistantMessages_AiAssistantSessionId",
                table: "AiAssistantMessages",
                column: "AiAssistantSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AiAssistantSessions_SessionCode",
                table: "AiAssistantSessions",
                column: "SessionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditAnalyticsFindings_Type_Status_Time",
                table: "AuditAnalyticsFindings",
                columns: new[] { "FindingType", "Status", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EnterprisePredictiveAlerts_OwnerPartnerId",
                table: "EnterprisePredictiveAlerts",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_EnterprisePredictiveAlerts_Type_Status_Scope",
                table: "EnterprisePredictiveAlerts",
                columns: new[] { "AlertType", "Status", "ForecastFor", "WarehouseId", "OwnerPartnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_EnterprisePredictiveAlerts_WarehouseId",
                table: "EnterprisePredictiveAlerts",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTelemetryLogs_CorrelationId",
                table: "RequestTelemetryLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTelemetryLogs_Time_Path_Status",
                table: "RequestTelemetryLogs",
                columns: new[] { "CreatedAt", "Path", "StatusCode" });

            migrationBuilder.CreateIndex(
                name: "IX_SemanticMetricDefinitions_MetricCode",
                table: "SemanticMetricDefinitions",
                column: "MetricCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SemanticMetricSnapshots_Metric_Date_Scope",
                table: "SemanticMetricSnapshots",
                columns: new[] { "SemanticMetricDefinitionId", "MetricDate", "ScopeKey" });

            migrationBuilder.CreateIndex(
                name: "IX_SemanticMetricSnapshots_OwnerPartnerId",
                table: "SemanticMetricSnapshots",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SemanticMetricSnapshots_WarehouseId",
                table: "SemanticMetricSnapshots",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_SreMetricSnapshots_SnapshotAt",
                table: "SreMetricSnapshots",
                column: "SnapshotAt");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseWorkflowProfiles_OwnerPartnerId",
                table: "WarehouseWorkflowProfiles",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "UX_WarehouseWorkflowProfiles_Scope_Module",
                table: "WarehouseWorkflowProfiles",
                columns: new[] { "WarehouseId", "OwnerPartnerId", "ModuleKey" },
                unique: true,
                filter: "[OwnerPartnerId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiAssistantCitations");

            migrationBuilder.DropTable(
                name: "AuditAnalyticsFindings");

            migrationBuilder.DropTable(
                name: "EnterprisePredictiveAlerts");

            migrationBuilder.DropTable(
                name: "RequestTelemetryLogs");

            migrationBuilder.DropTable(
                name: "SemanticMetricSnapshots");

            migrationBuilder.DropTable(
                name: "SreMetricSnapshots");

            migrationBuilder.DropTable(
                name: "WarehouseWorkflowProfiles");

            migrationBuilder.DropTable(
                name: "AiAssistantMessages");

            migrationBuilder.DropTable(
                name: "SemanticMetricDefinitions");

            migrationBuilder.DropTable(
                name: "AiAssistantSessions");
        }
    }
}
