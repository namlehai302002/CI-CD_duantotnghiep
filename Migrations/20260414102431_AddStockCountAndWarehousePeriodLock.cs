using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddStockCountAndWarehousePeriodLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockCountSheets",
                columns: table => new
                {
                    StockCountSheetId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    CountDate = table.Column<DateTime>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GeneratedAdjustmentVoucherId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCountSheets", x => x.StockCountSheetId);
                    table.ForeignKey(
                        name: "FK_StockCountSheets_Vouchers_GeneratedAdjustmentVoucherId",
                        column: x => x.GeneratedAdjustmentVoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                    table.ForeignKey(
                        name: "FK_StockCountSheets_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "WarehousePeriodLocks",
                columns: table => new
                {
                    WarehousePeriodLockId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    LockDate = table.Column<DateTime>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    LockedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UnlockedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UnlockedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehousePeriodLocks", x => x.WarehousePeriodLockId);
                    table.ForeignKey(
                        name: "FK_WarehousePeriodLocks_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "StockCountLines",
                columns: table => new
                {
                    StockCountLineId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockCountSheetId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "date", nullable: true),
                    SystemQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CountedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DiffQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCountLines", x => x.StockCountLineId);
                    table.ForeignKey(
                        name: "FK_StockCountLines_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId");
                    table.ForeignKey(
                        name: "FK_StockCountLines_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_StockCountLines_StockCountSheets_StockCountSheetId",
                        column: x => x.StockCountSheetId,
                        principalTable: "StockCountSheets",
                        principalColumn: "StockCountSheetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_ItemId",
                table: "StockCountLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_LocationId",
                table: "StockCountLines",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_StockCountSheetId_ItemId_LocationId_LotNumber_ExpiryDate",
                table: "StockCountLines",
                columns: new[] { "StockCountSheetId", "ItemId", "LocationId", "LotNumber", "ExpiryDate" },
                unique: true,
                filter: "[LotNumber] IS NOT NULL AND [ExpiryDate] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountSheets_GeneratedAdjustmentVoucherId",
                table: "StockCountSheets",
                column: "GeneratedAdjustmentVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountSheets_WarehouseId",
                table: "StockCountSheets",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehousePeriodLocks_WarehouseId_IsActive",
                table: "WarehousePeriodLocks",
                columns: new[] { "WarehouseId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockCountLines");

            migrationBuilder.DropTable(
                name: "WarehousePeriodLocks");

            migrationBuilder.DropTable(
                name: "StockCountSheets");
        }
    }
}
