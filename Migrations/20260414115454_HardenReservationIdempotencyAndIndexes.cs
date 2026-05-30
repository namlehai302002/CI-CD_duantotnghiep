using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class HardenReservationIdempotencyAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_VoucherId_VoucherDetailId_ItemId_LocationId_LotNumber_ExpiryDate",
                table: "StockReservations",
                columns: new[] { "VoucherId", "VoucherDetailId", "ItemId", "LocationId", "LotNumber", "ExpiryDate" },
                unique: true,
                filter: "[Status] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockReservations_VoucherId_VoucherDetailId_ItemId_LocationId_LotNumber_ExpiryDate",
                table: "StockReservations");
        }
    }
}
