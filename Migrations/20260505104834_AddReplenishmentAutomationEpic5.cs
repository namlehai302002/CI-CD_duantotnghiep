using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WMS.Data;

#nullable disable

namespace WMS.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260505104834_AddReplenishmentAutomationEpic5")]
    public partial class AddReplenishmentAutomationEpic5 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AutomationBatchKey",
                table: "MovementTasks",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DemandQtySnapshot",
                table: "MovementTasks",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DestinationAisleSequence",
                table: "MovementTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DestinationZoneId",
                table: "MovementTasks",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'MovementTasks', N'DueAt') IS NULL
                BEGIN
                    ALTER TABLE [MovementTasks] ADD [DueAt] datetime2 NULL;
                END
                """);

            migrationBuilder.AddColumn<decimal>(
                name: "ForecastQtySnapshot",
                table: "MovementTasks",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OpenReplenishmentQtySnapshot",
                table: "MovementTasks",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "ReplenishmentAutomationLineId",
                table: "MovementTasks",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReplenishmentAutomationRunId",
                table: "MovementTasks",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "ReplenishmentTriggerType",
                table: "MovementTasks",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoutePriorityScore",
                table: "MovementTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SourceAisleSequence",
                table: "MovementTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SourceZoneId",
                table: "MovementTasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TravelSequenceScore",
                table: "MovementTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ReplenishmentAutomationRuns",
                columns: table => new
                {
                    ReplenishmentAutomationRunId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    AutoCreateTasks = table.Column<bool>(type: "bit", nullable: false),
                    DemandHorizonDays = table.Column<int>(type: "int", nullable: false),
                    ForecastHistoryDays = table.Column<int>(type: "int", nullable: false),
                    ForecastHorizonDays = table.Column<int>(type: "int", nullable: false),
                    ForecastSafetyFactor = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    SuggestedLineCount = table.Column<int>(type: "int", nullable: false),
                    CreatedTaskCount = table.Column<int>(type: "int", nullable: false),
                    SkippedLineCount = table.Column<int>(type: "int", nullable: false),
                    FailedLineCount = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TriggeredBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplenishmentAutomationRuns", x => x.ReplenishmentAutomationRunId);
                    table.ForeignKey(
                        name: "FK_ReplenishmentAutomationRuns_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "ReplenishmentAutomationLines",
                columns: table => new
                {
                    ReplenishmentAutomationLineId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReplenishmentAutomationRunId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    DestinationLocationId = table.Column<int>(type: "int", nullable: false),
                    SourceLocationId = table.Column<int>(type: "int", nullable: false),
                    SourceItemLocationId = table.Column<int>(type: "int", nullable: true),
                    MovementTaskId = table.Column<long>(type: "bigint", nullable: true),
                    TriggerType = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Priority = table.Column<byte>(type: "tinyint", nullable: false),
                    PickFaceQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    OpenReplenishmentQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DemandQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ForecastQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TriggerQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TargetQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SuggestedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SourceAvailableQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "date", nullable: true),
                    DueAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RoutePriorityScore = table.Column<int>(type: "int", nullable: false),
                    TravelSequenceScore = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplenishmentAutomationLines", x => x.ReplenishmentAutomationLineId);
                    table.ForeignKey(
                        name: "FK_ReplenishmentAutomationLines_ItemLocations_SourceItemLocationId",
                        column: x => x.SourceItemLocationId,
                        principalTable: "ItemLocations",
                        principalColumn: "ItemLocationId");
                    table.ForeignKey(
                        name: "FK_ReplenishmentAutomationLines_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId");
                    table.ForeignKey(
                        name: "FK_ReplenishmentAutomationLines_Locations_DestinationLocationId",
                        column: x => x.DestinationLocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_ReplenishmentAutomationLines_Locations_SourceLocationId",
                        column: x => x.SourceLocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_ReplenishmentAutomationLines_MovementTasks_MovementTaskId",
                        column: x => x.MovementTaskId,
                        principalTable: "MovementTasks",
                        principalColumn: "MovementTaskId");
                    table.ForeignKey(
                        name: "FK_ReplenishmentAutomationLines_ReplenishmentAutomationRuns_ReplenishmentAutomationRunId",
                        column: x => x.ReplenishmentAutomationRunId,
                        principalTable: "ReplenishmentAutomationRuns",
                        principalColumn: "ReplenishmentAutomationRunId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReplenishmentAutomationLines_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovementTasks_ReplenishmentRunId",
                table: "MovementTasks",
                column: "ReplenishmentAutomationRunId");

            migrationBuilder.CreateIndex(
                name: "IX_MovementTasks_RoutingQueue",
                table: "MovementTasks",
                columns: new[] { "WarehouseId", "Status", "RoutePriorityScore", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentAutomationLines_DestinationLocationId",
                table: "ReplenishmentAutomationLines",
                column: "DestinationLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentAutomationLines_ItemId",
                table: "ReplenishmentAutomationLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentAutomationLines_MovementTaskId",
                table: "ReplenishmentAutomationLines",
                column: "MovementTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentAutomationLines_Queue",
                table: "ReplenishmentAutomationLines",
                columns: new[] { "WarehouseId", "Status", "Priority", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentAutomationLines_Run_Item_Dest",
                table: "ReplenishmentAutomationLines",
                columns: new[] { "ReplenishmentAutomationRunId", "ItemId", "DestinationLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentAutomationLines_SourceItemLocationId",
                table: "ReplenishmentAutomationLines",
                column: "SourceItemLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentAutomationLines_SourceLocationId",
                table: "ReplenishmentAutomationLines",
                column: "SourceLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentAutomationRuns_Warehouse_Status_Start",
                table: "ReplenishmentAutomationRuns",
                columns: new[] { "WarehouseId", "Status", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentAutomationRuns_WarehouseId",
                table: "ReplenishmentAutomationRuns",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentAutomationRuns_RunCode",
                table: "ReplenishmentAutomationRuns",
                column: "RunCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MovementTasks_ReplenishmentAutomationRuns_ReplenishmentAutomationRunId",
                table: "MovementTasks",
                column: "ReplenishmentAutomationRunId",
                principalTable: "ReplenishmentAutomationRuns",
                principalColumn: "ReplenishmentAutomationRunId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovementTasks_ReplenishmentAutomationRuns_ReplenishmentAutomationRunId",
                table: "MovementTasks");

            migrationBuilder.DropTable(
                name: "ReplenishmentAutomationLines");

            migrationBuilder.DropTable(
                name: "ReplenishmentAutomationRuns");

            migrationBuilder.DropIndex(
                name: "IX_MovementTasks_ReplenishmentRunId",
                table: "MovementTasks");

            migrationBuilder.DropIndex(
                name: "IX_MovementTasks_RoutingQueue",
                table: "MovementTasks");

            migrationBuilder.DropColumn(
                name: "AutomationBatchKey",
                table: "MovementTasks");

            migrationBuilder.DropColumn(
                name: "DemandQtySnapshot",
                table: "MovementTasks");

            migrationBuilder.DropColumn(
                name: "DestinationAisleSequence",
                table: "MovementTasks");

            migrationBuilder.DropColumn(
                name: "DestinationZoneId",
                table: "MovementTasks");

            migrationBuilder.DropColumn(
                name: "DueAt",
                table: "MovementTasks");

            migrationBuilder.DropColumn(
                name: "ForecastQtySnapshot",
                table: "MovementTasks");

            migrationBuilder.DropColumn(
                name: "OpenReplenishmentQtySnapshot",
                table: "MovementTasks");

            migrationBuilder.DropColumn(
                name: "ReplenishmentAutomationLineId",
                table: "MovementTasks");

            migrationBuilder.DropColumn(
                name: "ReplenishmentAutomationRunId",
                table: "MovementTasks");

            migrationBuilder.DropColumn(
                name: "ReplenishmentTriggerType",
                table: "MovementTasks");

            migrationBuilder.DropColumn(
                name: "RoutePriorityScore",
                table: "MovementTasks");

            migrationBuilder.DropColumn(
                name: "SourceAisleSequence",
                table: "MovementTasks");

            migrationBuilder.DropColumn(
                name: "SourceZoneId",
                table: "MovementTasks");

            migrationBuilder.DropColumn(
                name: "TravelSequenceScore",
                table: "MovementTasks");
        }
    }
}
