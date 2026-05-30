using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddCapacityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CapacityPerUnit",
                table: "Items",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CapacityUomId",
                table: "Items",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_CapacityUomId",
                table: "Items",
                column: "CapacityUomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_UnitsOfMeasure_CapacityUomId",
                table: "Items",
                column: "CapacityUomId",
                principalTable: "UnitsOfMeasure",
                principalColumn: "UomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_UnitsOfMeasure_CapacityUomId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_CapacityUomId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "CapacityPerUnit",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "CapacityUomId",
                table: "Items");
        }
    }
}
