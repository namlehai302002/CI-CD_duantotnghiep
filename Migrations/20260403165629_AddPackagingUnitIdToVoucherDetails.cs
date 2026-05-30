using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddPackagingUnitIdToVoucherDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PackagingUnitId",
                table: "VoucherDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoucherDetails_PackagingUnitId",
                table: "VoucherDetails",
                column: "PackagingUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_VoucherDetails_PackagingUnits_PackagingUnitId",
                table: "VoucherDetails",
                column: "PackagingUnitId",
                principalTable: "PackagingUnits",
                principalColumn: "PackagingUnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VoucherDetails_PackagingUnits_PackagingUnitId",
                table: "VoucherDetails");

            migrationBuilder.DropIndex(
                name: "IX_VoucherDetails_PackagingUnitId",
                table: "VoucherDetails");

            migrationBuilder.DropColumn(
                name: "PackagingUnitId",
                table: "VoucherDetails");
        }
    }
}
