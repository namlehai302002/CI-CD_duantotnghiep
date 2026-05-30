using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddYardManagementMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Trailers",
                columns: table => new
                {
                    TrailerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrailerNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContainerNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TrailerType = table.Column<byte>(type: "tinyint", nullable: false),
                    CarrierName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SealNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trailers", x => x.TrailerId);
                });

            migrationBuilder.CreateTable(
                name: "YardSpots",
                columns: table => new
                {
                    YardSpotId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    SpotCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SpotName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SpotType = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YardSpots", x => x.YardSpotId);
                    table.ForeignKey(
                        name: "FK_YardSpots_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "YardVisits",
                columns: table => new
                {
                    YardVisitId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    TrailerId = table.Column<int>(type: "int", nullable: false),
                    CurrentSpotId = table.Column<int>(type: "int", nullable: true),
                    VoucherId = table.Column<long>(type: "bigint", nullable: true),
                    Purpose = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    GateInAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GateOutAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedSpotAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastMovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GateInBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GateOutBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DriverName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DriverPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    VehicleNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DockDoor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DockAppointmentStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DockAppointmentEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YardVisits", x => x.YardVisitId);
                    table.ForeignKey(
                        name: "FK_YardVisits_Trailers_TrailerId",
                        column: x => x.TrailerId,
                        principalTable: "Trailers",
                        principalColumn: "TrailerId");
                    table.ForeignKey(
                        name: "FK_YardVisits_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                    table.ForeignKey(
                        name: "FK_YardVisits_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                    table.ForeignKey(
                        name: "FK_YardVisits_YardSpots_CurrentSpotId",
                        column: x => x.CurrentSpotId,
                        principalTable: "YardSpots",
                        principalColumn: "YardSpotId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trailers_ContainerNumber",
                table: "Trailers",
                column: "ContainerNumber",
                unique: true,
                filter: "[ContainerNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Trailers_TrailerNumber",
                table: "Trailers",
                column: "TrailerNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YardSpots_Warehouse_Status_Active",
                table: "YardSpots",
                columns: new[] { "WarehouseId", "Status", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_YardSpots_WarehouseId_SpotCode",
                table: "YardSpots",
                columns: new[] { "WarehouseId", "SpotCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YardVisits_Active_Spot",
                table: "YardVisits",
                column: "CurrentSpotId",
                unique: true,
                filter: "[CurrentSpotId] IS NOT NULL AND [GateOutAt] IS NULL AND [Status] <> 5");

            migrationBuilder.CreateIndex(
                name: "IX_YardVisits_Active_Trailer",
                table: "YardVisits",
                column: "TrailerId",
                unique: true,
                filter: "[GateOutAt] IS NULL AND [Status] <> 5");

            migrationBuilder.CreateIndex(
                name: "IX_YardVisits_VisitCode",
                table: "YardVisits",
                column: "VisitCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YardVisits_VoucherId",
                table: "YardVisits",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_YardVisits_Warehouse_Status_GateIn",
                table: "YardVisits",
                columns: new[] { "WarehouseId", "Status", "GateInAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YardVisits");

            migrationBuilder.DropTable(
                name: "Trailers");

            migrationBuilder.DropTable(
                name: "YardSpots");

        }
    }
}



