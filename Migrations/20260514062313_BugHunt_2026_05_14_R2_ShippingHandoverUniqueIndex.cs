using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class BugHunt_2026_05_14_R2_ShippingHandoverUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_ShippingHandoverLogs_Voucher_ShipmentLoad",
                table: "ShippingHandoverLogs",
                columns: new[] { "VoucherId", "ShipmentLoadId" },
                unique: true,
                filter: "[ShipmentLoadId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ShippingHandoverLogs_Voucher_ShipmentLoad",
                table: "ShippingHandoverLogs");
        }
    }
}
