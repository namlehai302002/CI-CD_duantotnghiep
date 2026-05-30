using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddPackingShippingDock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DockDoor",
                table: "Vouchers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManifestCode",
                table: "Vouchers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PackedAt",
                table: "Vouchers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackedBy",
                table: "Vouchers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippedAt",
                table: "Vouchers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippedBy",
                table: "Vouchers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingNumber",
                table: "Vouchers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DockDoor",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ManifestCode",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "PackedAt",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "PackedBy",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ShippedAt",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ShippedBy",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "TrackingNumber",
                table: "Vouchers");
        }
    }
}
