using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboundPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboundPackages",
                columns: table => new
                {
                    OutboundPackageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PackageType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReferenceLpnCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    TotalQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    PackedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PackedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrackingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ManifestCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundPackages", x => x.OutboundPackageId);
                    table.ForeignKey(
                        name: "FK_OutboundPackages_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                    table.ForeignKey(
                        name: "FK_OutboundPackages_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboundPackages_PackageCode",
                table: "OutboundPackages",
                column: "PackageCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboundPackages_Voucher_Date",
                table: "OutboundPackages",
                columns: new[] { "VoucherId", "PackedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboundPackages_Warehouse_Date",
                table: "OutboundPackages",
                columns: new[] { "WarehouseId", "PackedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboundPackages");
        }
    }
}
