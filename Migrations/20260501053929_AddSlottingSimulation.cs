using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddSlottingSimulation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlottingSimulationScenarios",
                columns: table => new
                {
                    ScenarioId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScenarioCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ScenarioName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    LineCount = table.Column<int>(type: "int", nullable: false),
                    ApprovedTaskCount = table.Column<int>(type: "int", nullable: false),
                    TotalEstimatedTravelMinutesSaved = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalMovementCostMinutes = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    NetEstimatedMinutesSaved = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlottingSimulationScenarios", x => x.ScenarioId);
                    table.ForeignKey(
                        name: "FK_SlottingSimulationScenarios_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "SlottingSimulationLines",
                columns: table => new
                {
                    ScenarioLineId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScenarioId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    CurrentDefaultLocationId = table.Column<int>(type: "int", nullable: true),
                    SourceLocationId = table.Column<int>(type: "int", nullable: false),
                    SuggestedLocationId = table.Column<int>(type: "int", nullable: false),
                    SourceItemLocationId = table.Column<int>(type: "int", nullable: true),
                    PlannedMoveQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DailyPickFrequency = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    BeforeTravelDistance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AfterTravelDistance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    EstimatedTravelMinutesSaved = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    MovementCostMinutes = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    NetEstimatedMinutesSaved = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SlottingScore = table.Column<int>(type: "int", nullable: false),
                    AbcClass = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    MovementTaskId = table.Column<long>(type: "bigint", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlottingSimulationLines", x => x.ScenarioLineId);
                    table.ForeignKey(
                        name: "FK_SlottingSimulationLines_ItemLocations_SourceItemLocationId",
                        column: x => x.SourceItemLocationId,
                        principalTable: "ItemLocations",
                        principalColumn: "ItemLocationId");
                    table.ForeignKey(
                        name: "FK_SlottingSimulationLines_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId");
                    table.ForeignKey(
                        name: "FK_SlottingSimulationLines_Locations_CurrentDefaultLocationId",
                        column: x => x.CurrentDefaultLocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_SlottingSimulationLines_Locations_SourceLocationId",
                        column: x => x.SourceLocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_SlottingSimulationLines_Locations_SuggestedLocationId",
                        column: x => x.SuggestedLocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_SlottingSimulationLines_MovementTasks_MovementTaskId",
                        column: x => x.MovementTaskId,
                        principalTable: "MovementTasks",
                        principalColumn: "MovementTaskId");
                    table.ForeignKey(
                        name: "FK_SlottingSimulationLines_SlottingSimulationScenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalTable: "SlottingSimulationScenarios",
                        principalColumn: "ScenarioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlottingSimulationLines_CurrentDefaultLocationId",
                table: "SlottingSimulationLines",
                column: "CurrentDefaultLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SlottingSimulationLines_ItemId",
                table: "SlottingSimulationLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SlottingSimulationLines_MovementTaskId",
                table: "SlottingSimulationLines",
                column: "MovementTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_SlottingSimulationLines_Scenario_Item_Move",
                table: "SlottingSimulationLines",
                columns: new[] { "ScenarioId", "ItemId", "SourceLocationId", "SuggestedLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SlottingSimulationLines_SourceItemLocationId",
                table: "SlottingSimulationLines",
                column: "SourceItemLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SlottingSimulationLines_SourceLocationId",
                table: "SlottingSimulationLines",
                column: "SourceLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SlottingSimulationLines_SuggestedLocationId",
                table: "SlottingSimulationLines",
                column: "SuggestedLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SlottingSimulationScenarios_ScenarioCode",
                table: "SlottingSimulationScenarios",
                column: "ScenarioCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlottingSimulationScenarios_WarehouseId",
                table: "SlottingSimulationScenarios",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlottingSimulationLines");

            migrationBuilder.DropTable(
                name: "SlottingSimulationScenarios");
        }
    }
}
