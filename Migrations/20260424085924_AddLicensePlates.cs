using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddLicensePlates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LicensePlates",
                columns: table => new
                {
                    LicensePlateId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LpnCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
                    VoucherDetailId = table.Column<long>(type: "bigint", nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ManufacturingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicensePlates", x => x.LicensePlateId);
                    table.ForeignKey(
                        name: "FK_LicensePlates_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId");
                    table.ForeignKey(
                        name: "FK_LicensePlates_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_LicensePlates_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                    table.ForeignKey(
                        name: "FK_LicensePlates_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LicensePlates_ItemId",
                table: "LicensePlates",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LicensePlates_LocationId",
                table: "LicensePlates",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_LicensePlates_LpnCode",
                table: "LicensePlates",
                column: "LpnCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LicensePlates_VoucherId",
                table: "LicensePlates",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_LicensePlates_Warehouse_Item_Location_Active",
                table: "LicensePlates",
                columns: new[] { "WarehouseId", "ItemId", "LocationId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LicensePlates");
        }
    }
}
