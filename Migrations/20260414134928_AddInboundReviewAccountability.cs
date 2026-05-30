using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddInboundReviewAccountability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ResponsibilityScore",
                table: "Vouchers",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "Vouchers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "ReviewResult",
                table: "Vouchers",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "Vouchers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedBy",
                table: "Vouchers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResponsibilityScore",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ReviewResult",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "Vouchers");
        }
    }
}
