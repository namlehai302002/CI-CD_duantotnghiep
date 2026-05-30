using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddStockCountUnlockAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UnlockReason",
                table: "StockCountSheets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnlockedAt",
                table: "StockCountSheets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnlockedBy",
                table: "StockCountSheets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnlockReason",
                table: "StockCountSheets");

            migrationBuilder.DropColumn(
                name: "UnlockedAt",
                table: "StockCountSheets");

            migrationBuilder.DropColumn(
                name: "UnlockedBy",
                table: "StockCountSheets");
        }
    }
}
