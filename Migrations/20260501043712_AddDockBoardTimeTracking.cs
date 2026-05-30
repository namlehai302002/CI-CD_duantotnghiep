using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddDockBoardTimeTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DockArrivalAt",
                table: "Vouchers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DockCompletedAt",
                table: "Vouchers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "DockStatus",
                table: "Vouchers",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<DateTime>(
                name: "GateInAt",
                table: "Vouchers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnloadEndAt",
                table: "Vouchers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnloadStartAt",
                table: "Vouchers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_Warehouse_Dock_Status",
                table: "Vouchers",
                columns: new[] { "WarehouseId", "DockStatus", "DockAppointmentStart" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vouchers_Warehouse_Dock_Status",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "DockArrivalAt",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "DockCompletedAt",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "DockStatus",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "GateInAt",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "UnloadEndAt",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "UnloadStartAt",
                table: "Vouchers");
        }
    }
}
