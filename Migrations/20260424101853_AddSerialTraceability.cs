using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddSerialTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TrackSerial",
                table: "Items",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SerialNumbers",
                columns: table => new
                {
                    SerialNumberId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SerialCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
                    VoucherDetailId = table.Column<long>(type: "bigint", nullable: true),
                    LicensePlateId = table.Column<long>(type: "bigint", nullable: true),
                    LotNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ManufacturingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SerialNumbers", x => x.SerialNumberId);
                    table.ForeignKey(
                        name: "FK_SerialNumbers_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId");
                    table.ForeignKey(
                        name: "FK_SerialNumbers_LicensePlates_LicensePlateId",
                        column: x => x.LicensePlateId,
                        principalTable: "LicensePlates",
                        principalColumn: "LicensePlateId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SerialNumbers_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_SerialNumbers_VoucherDetails_VoucherDetailId",
                        column: x => x.VoucherDetailId,
                        principalTable: "VoucherDetails",
                        principalColumn: "VoucherDetailId");
                    table.ForeignKey(
                        name: "FK_SerialNumbers_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                    table.ForeignKey(
                        name: "FK_SerialNumbers_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_ItemId",
                table: "SerialNumbers",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_LicensePlateId",
                table: "SerialNumbers",
                column: "LicensePlateId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_LocationId",
                table: "SerialNumbers",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_SerialCode",
                table: "SerialNumbers",
                column: "SerialCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_Voucher_Status",
                table: "SerialNumbers",
                columns: new[] { "VoucherId", "VoucherDetailId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_VoucherDetailId",
                table: "SerialNumbers",
                column: "VoucherDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_Warehouse_Item_Status",
                table: "SerialNumbers",
                columns: new[] { "WarehouseId", "ItemId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SerialNumbers");

            migrationBuilder.DropColumn(
                name: "TrackSerial",
                table: "Items");
        }
    }
}
