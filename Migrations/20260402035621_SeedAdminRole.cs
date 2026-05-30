using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PackagingUnits",
                columns: table => new
                {
                    PackagingUnitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDongGoi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BaseUomId = table.Column<int>(type: "int", nullable: false),
                    GiaTri = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackagingUnits", x => x.PackagingUnitId);
                    table.ForeignKey(
                        name: "FK_PackagingUnits_UnitsOfMeasure_BaseUomId",
                        column: x => x.BaseUomId,
                        principalTable: "UnitsOfMeasure",
                        principalColumn: "UomId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackagingUnits_BaseUomId",
                table: "PackagingUnits",
                column: "BaseUomId");

            migrationBuilder.CreateIndex(
                name: "IX_PackagingUnits_TenDongGoi",
                table: "PackagingUnits",
                column: "TenDongGoi",
                unique: true);

            // Seed AppRoles
            migrationBuilder.InsertData(
                table: "AppRoles",
                columns: new[] { "RoleId", "RoleName", "Description" },
                values: new object[,]
                {
                    { 1, "Admin", "Quản trị viên hệ thống" },
                    { 2, "Manager", "Quản lý kho" },
                    { 3, "Staff", "Nhân viên kho" },
                    { 4, "Viewer", "Xem báo cáo" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackagingUnits");
        }
    }
}
