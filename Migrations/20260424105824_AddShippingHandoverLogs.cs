using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingHandoverLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShippingHandoverLogs",
                columns: table => new
                {
                    ShippingHandoverLogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    HandedOverBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HandedOverAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrackingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ManifestCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CarrierName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VehicleNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DriverName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DriverPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingHandoverLogs", x => x.ShippingHandoverLogId);
                    table.ForeignKey(
                        name: "FK_ShippingHandoverLogs_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                    table.ForeignKey(
                        name: "FK_ShippingHandoverLogs_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingHandoverLogs_VoucherId",
                table: "ShippingHandoverLogs",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingHandoverLogs_Warehouse_Date",
                table: "ShippingHandoverLogs",
                columns: new[] { "WarehouseId", "HandedOverAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShippingHandoverLogs");
        }
    }
}
