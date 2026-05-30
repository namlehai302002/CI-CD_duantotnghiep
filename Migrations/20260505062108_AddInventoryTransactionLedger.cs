using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryTransactionLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    InventoryTransactionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionType = table.Column<byte>(type: "tinyint", nullable: false),
                    TransactionGroupKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "date", nullable: true),
                    HoldStatusBefore = table.Column<byte>(type: "tinyint", nullable: true),
                    HoldStatusAfter = table.Column<byte>(type: "tinyint", nullable: true),
                    QuantityDelta = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReservedDelta = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AvailableDelta = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    QuantityBefore = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    QuantityAfter = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReservedBefore = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReservedAfter = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AvailableBefore = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AvailableAfter = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    VoucherId = table.Column<long>(type: "bigint", nullable: true),
                    VoucherDetailId = table.Column<long>(type: "bigint", nullable: true),
                    PickTaskId = table.Column<long>(type: "bigint", nullable: true),
                    MovementTaskId = table.Column<long>(type: "bigint", nullable: true),
                    StockReservationId = table.Column<long>(type: "bigint", nullable: true),
                    LicensePlateId = table.Column<long>(type: "bigint", nullable: true),
                    SerialNumberId = table.Column<long>(type: "bigint", nullable: true),
                    ReferenceType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    ReferenceId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ReferenceCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Actor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TransactionAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.InventoryTransactionId);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_LicensePlates_LicensePlateId",
                        column: x => x.LicensePlateId,
                        principalTable: "LicensePlates",
                        principalColumn: "LicensePlateId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_MovementTasks_MovementTaskId",
                        column: x => x.MovementTaskId,
                        principalTable: "MovementTasks",
                        principalColumn: "MovementTaskId");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_PickTasks_PickTaskId",
                        column: x => x.PickTaskId,
                        principalTable: "PickTasks",
                        principalColumn: "PickTaskId");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_SerialNumbers_SerialNumberId",
                        column: x => x.SerialNumberId,
                        principalTable: "SerialNumbers",
                        principalColumn: "SerialNumberId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_StockReservations_StockReservationId",
                        column: x => x.StockReservationId,
                        principalTable: "StockReservations",
                        principalColumn: "StockReservationId");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_VoucherDetails_VoucherDetailId",
                        column: x => x.VoucherDetailId,
                        principalTable: "VoucherDetails",
                        principalColumn: "VoucherDetailId");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.Sql(@"
INSERT INTO InventoryTransactions
(
    TransactionType,
    TransactionGroupKey,
    IdempotencyKey,
    WarehouseId,
    ItemId,
    LocationId,
    LotNumber,
    ExpiryDate,
    HoldStatusBefore,
    HoldStatusAfter,
    QuantityDelta,
    ReservedDelta,
    AvailableDelta,
    QuantityBefore,
    QuantityAfter,
    ReservedBefore,
    ReservedAfter,
    AvailableBefore,
    AvailableAfter,
    ReferenceType,
    ReferenceId,
    ReferenceCode,
    Actor,
    TransactionAt,
    MetadataJson
)
SELECT
    1,
    N'opening-balance:cutover',
    CONCAT(N'opening-balance:itemlocation:', il.ItemLocationId),
    z.WarehouseId,
    il.ItemId,
    il.LocationId,
    il.LotNumber,
    il.ExpiryDate,
    NULL,
    il.HoldStatus,
    il.Quantity,
    il.ReservedQty,
    il.Quantity - il.ReservedQty,
    0,
    il.Quantity,
    0,
    il.ReservedQty,
    0,
    il.Quantity - il.ReservedQty,
    N'ItemLocation',
    CONVERT(nvarchar(80), il.ItemLocationId),
    N'OPENING_BALANCE',
    N'migration',
    SYSUTCDATETIME(),
    N'{""source"":""AddInventoryTransactionLedger""}'
FROM ItemLocations il
INNER JOIN Locations l ON l.LocationId = il.LocationId
INNER JOIN Zones z ON z.ZoneId = l.ZoneId
WHERE NOT EXISTS
(
    SELECT 1
    FROM InventoryTransactions tx
    WHERE tx.IdempotencyKey = CONCAT(N'opening-balance:itemlocation:', il.ItemLocationId)
);");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_GroupKey",
                table: "InventoryTransactions",
                column: "TransactionGroupKey");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_Item_Location_Date",
                table: "InventoryTransactions",
                columns: new[] { "ItemId", "LocationId", "TransactionAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_LicensePlateId",
                table: "InventoryTransactions",
                column: "LicensePlateId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_LocationId",
                table: "InventoryTransactions",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_MovementTaskId",
                table: "InventoryTransactions",
                column: "MovementTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_PickTaskId",
                table: "InventoryTransactions",
                column: "PickTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_Reference",
                table: "InventoryTransactions",
                columns: new[] { "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_SerialNumberId",
                table: "InventoryTransactions",
                column: "SerialNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_StockReservationId",
                table: "InventoryTransactions",
                column: "StockReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_Type_Date",
                table: "InventoryTransactions",
                columns: new[] { "TransactionType", "TransactionAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_VoucherDetailId",
                table: "InventoryTransactions",
                column: "VoucherDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_VoucherId",
                table: "InventoryTransactions",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_Warehouse_Date",
                table: "InventoryTransactions",
                columns: new[] { "WarehouseId", "TransactionAt" });

            migrationBuilder.CreateIndex(
                name: "UX_InventoryTransactions_IdempotencyKey",
                table: "InventoryTransactions",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryTransactions");
        }
    }
}
