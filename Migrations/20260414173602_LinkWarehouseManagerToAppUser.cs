using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class LinkWarehouseManagerToAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ManagerUserId",
                table: "Warehouses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_ManagerUserId",
                table: "Warehouses",
                column: "ManagerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_AppUsers_ManagerUserId",
                table: "Warehouses",
                column: "ManagerUserId",
                principalTable: "AppUsers",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_AppUsers_ManagerUserId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_ManagerUserId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "ManagerUserId",
                table: "Warehouses");
        }
    }
}
