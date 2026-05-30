using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddItemFixesV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DefectBaseQty",
                table: "VoucherDetails",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DefaultLocationId",
                table: "Items",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_DefaultLocationId",
                table: "Items",
                column: "DefaultLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Locations_DefaultLocationId",
                table: "Items",
                column: "DefaultLocationId",
                principalTable: "Locations",
                principalColumn: "LocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Locations_DefaultLocationId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_DefaultLocationId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "DefectBaseQty",
                table: "VoucherDetails");

            migrationBuilder.DropColumn(
                name: "DefaultLocationId",
                table: "Items");
        }
    }
}
