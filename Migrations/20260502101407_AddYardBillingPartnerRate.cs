using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddYardBillingPartnerRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_YardBillingRates_WarehouseId",
                table: "YardBillingRates");

            migrationBuilder.AddColumn<int>(
                name: "PartnerId",
                table: "YardBillingRates",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_YardBillingRates_PartnerId",
                table: "YardBillingRates",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_YardBillingRates_Match",
                table: "YardBillingRates",
                columns: new[] { "WarehouseId", "PartnerId", "CarrierName", "TrailerType", "SpotType", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_YardBillingRates_Partners_PartnerId",
                table: "YardBillingRates",
                column: "PartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_YardBillingRates_Partners_PartnerId",
                table: "YardBillingRates");

            migrationBuilder.DropIndex(
                name: "IX_YardBillingRates_Match",
                table: "YardBillingRates");

            migrationBuilder.DropIndex(
                name: "IX_YardBillingRates_PartnerId",
                table: "YardBillingRates");

            migrationBuilder.DropColumn(
                name: "PartnerId",
                table: "YardBillingRates");

            migrationBuilder.CreateIndex(
                name: "IX_YardBillingRates_WarehouseId",
                table: "YardBillingRates",
                column: "WarehouseId");
        }
    }
}
