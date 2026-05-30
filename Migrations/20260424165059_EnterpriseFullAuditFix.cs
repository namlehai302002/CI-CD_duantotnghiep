using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class EnterpriseFullAuditFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "Priority",
                table: "Waves",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<int>(
                name: "TargetLocationId",
                table: "PickTasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AisleCode",
                table: "Locations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickTasks_TargetLocationId",
                table: "PickTasks",
                column: "TargetLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_PickTasks_Locations_TargetLocationId",
                table: "PickTasks",
                column: "TargetLocationId",
                principalTable: "Locations",
                principalColumn: "LocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PickTasks_Locations_TargetLocationId",
                table: "PickTasks");

            migrationBuilder.DropIndex(
                name: "IX_PickTasks_TargetLocationId",
                table: "PickTasks");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Waves");

            migrationBuilder.DropColumn(
                name: "TargetLocationId",
                table: "PickTasks");

            migrationBuilder.DropColumn(
                name: "AisleCode",
                table: "Locations");
        }
    }
}
