using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddRecallLineOwnerScopeFinalAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "RecallLines",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecallLines_Owner_Item_Lot",
                table: "RecallLines",
                columns: new[] { "OwnerPartnerId", "ItemId", "LotNumber" });

            migrationBuilder.AddForeignKey(
                name: "FK_RecallLines_Partners_OwnerPartnerId",
                table: "RecallLines",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecallLines_Partners_OwnerPartnerId",
                table: "RecallLines");

            migrationBuilder.DropIndex(
                name: "IX_RecallLines_Owner_Item_Lot",
                table: "RecallLines");

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "RecallLines");
        }
    }
}
