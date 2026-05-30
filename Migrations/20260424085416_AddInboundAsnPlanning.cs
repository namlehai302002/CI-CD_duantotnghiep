using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddInboundAsnPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AsnCode",
                table: "Vouchers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CarrierName",
                table: "Vouchers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DockAppointmentEnd",
                table: "Vouchers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DockAppointmentStart",
                table: "Vouchers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "Vouchers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverPhone",
                table: "Vouchers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedArrivalAt",
                table: "Vouchers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleNumber",
                table: "Vouchers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_AsnCode",
                table: "Vouchers",
                column: "AsnCode",
                unique: true,
                filter: "[AsnCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_Warehouse_Dock_Window",
                table: "Vouchers",
                columns: new[] { "WarehouseId", "DockDoor", "DockAppointmentStart", "DockAppointmentEnd" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vouchers_AsnCode",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_Warehouse_Dock_Window",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "AsnCode",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "CarrierName",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "DockAppointmentEnd",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "DockAppointmentStart",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "DriverPhone",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ExpectedArrivalAt",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "VehicleNumber",
                table: "Vouchers");
        }
    }
}
