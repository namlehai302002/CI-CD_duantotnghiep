using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoStepPickingSortation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ParentPickTaskId",
                table: "PickTasks",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "PickTaskMode",
                table: "PickTasks",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<int>(
                name: "SortationDestinationLocationId",
                table: "PickTasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortationStageLocationId",
                table: "PickTasks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WarehouseSortationConfigs",
                columns: table => new
                {
                    WarehouseSortationConfigId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    StagingLocationId = table.Column<int>(type: "int", nullable: false),
                    SortationLocationId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseSortationConfigs", x => x.WarehouseSortationConfigId);
                    table.ForeignKey(
                        name: "FK_WarehouseSortationConfigs_Locations_SortationLocationId",
                        column: x => x.SortationLocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_WarehouseSortationConfigs_Locations_StagingLocationId",
                        column: x => x.StagingLocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_WarehouseSortationConfigs_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PickTasks_ParentPickTaskId",
                table: "PickTasks",
                column: "ParentPickTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_PickTasks_SortationDestinationLocationId",
                table: "PickTasks",
                column: "SortationDestinationLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PickTasks_SortationStageLocationId",
                table: "PickTasks",
                column: "SortationStageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PickTasks_Wave_Mode_Status",
                table: "PickTasks",
                columns: new[] { "WaveId", "PickTaskMode", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseSortationConfigs_ActiveWarehouse",
                table: "WarehouseSortationConfigs",
                columns: new[] { "WarehouseId", "IsActive" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseSortationConfigs_SortationLocationId",
                table: "WarehouseSortationConfigs",
                column: "SortationLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseSortationConfigs_StagingLocationId",
                table: "WarehouseSortationConfigs",
                column: "StagingLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_PickTasks_Locations_SortationDestinationLocationId",
                table: "PickTasks",
                column: "SortationDestinationLocationId",
                principalTable: "Locations",
                principalColumn: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_PickTasks_Locations_SortationStageLocationId",
                table: "PickTasks",
                column: "SortationStageLocationId",
                principalTable: "Locations",
                principalColumn: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_PickTasks_PickTasks_ParentPickTaskId",
                table: "PickTasks",
                column: "ParentPickTaskId",
                principalTable: "PickTasks",
                principalColumn: "PickTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PickTasks_Locations_SortationDestinationLocationId",
                table: "PickTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_PickTasks_Locations_SortationStageLocationId",
                table: "PickTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_PickTasks_PickTasks_ParentPickTaskId",
                table: "PickTasks");

            migrationBuilder.DropTable(
                name: "WarehouseSortationConfigs");

            migrationBuilder.DropIndex(
                name: "IX_PickTasks_ParentPickTaskId",
                table: "PickTasks");

            migrationBuilder.DropIndex(
                name: "IX_PickTasks_SortationDestinationLocationId",
                table: "PickTasks");

            migrationBuilder.DropIndex(
                name: "IX_PickTasks_SortationStageLocationId",
                table: "PickTasks");

            migrationBuilder.DropIndex(
                name: "IX_PickTasks_Wave_Mode_Status",
                table: "PickTasks");

            migrationBuilder.DropColumn(
                name: "ParentPickTaskId",
                table: "PickTasks");

            migrationBuilder.DropColumn(
                name: "PickTaskMode",
                table: "PickTasks");

            migrationBuilder.DropColumn(
                name: "SortationDestinationLocationId",
                table: "PickTasks");

            migrationBuilder.DropColumn(
                name: "SortationStageLocationId",
                table: "PickTasks");
        }
    }
}

