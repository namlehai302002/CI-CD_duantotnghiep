using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class BugHunt_2026_05_14_IndexesAndChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // DB hiện tại có thể đã không còn IX_Vouchers_VoucherCode (drop manual trước đó).
            // Dùng IF EXISTS để idempotent.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Vouchers_VoucherCode' AND object_id = OBJECT_ID('Vouchers'))
    DROP INDEX [IX_Vouchers_VoucherCode] ON [Vouchers];");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SerialNumbers_SerialCode' AND object_id = OBJECT_ID('SerialNumbers'))
    DROP INDEX [IX_SerialNumbers_SerialCode] ON [SerialNumbers];");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_WarehouseId_VoucherCode",
                table: "Vouchers",
                columns: new[] { "WarehouseId", "VoucherCode" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_VoucherDetails_DefectQty_NonNegative",
                table: "VoucherDetails",
                sql: "[DefectQty] >= 0 AND [DefectBaseQty] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockReservations_Qty_NonNegative",
                table: "StockReservations",
                sql: "[ReservedQty] >= 0 AND [ConsumedQty] >= 0 AND [ReleasedQty] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_WarehouseId_ItemId_SerialCode",
                table: "SerialNumbers",
                columns: new[] { "WarehouseId", "ItemId", "SerialCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // R3-8: dùng raw SQL idempotent. Down() ÍT khi chạy prod, nhưng nếu chạy thì:
            // - Không drop check constraint nếu data hiện vi phạm sẽ gây inconsistent state khác — vẫn drop.
            // - Recreate index single trên VoucherCode chỉ khi không có duplicate, vì DB prod đã từng có
            //   2 voucher cùng VoucherCode khác warehouse → recreate unique single sẽ fail.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Vouchers_WarehouseId_VoucherCode' AND object_id = OBJECT_ID('Vouchers'))
    DROP INDEX [IX_Vouchers_WarehouseId_VoucherCode] ON [Vouchers];");

            migrationBuilder.DropCheckConstraint(
                name: "CK_VoucherDetails_DefectQty_NonNegative",
                table: "VoucherDetails");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockReservations_Qty_NonNegative",
                table: "StockReservations");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SerialNumbers_WarehouseId_ItemId_SerialCode' AND object_id = OBJECT_ID('SerialNumbers'))
    DROP INDEX [IX_SerialNumbers_WarehouseId_ItemId_SerialCode] ON [SerialNumbers];");

            // Chỉ recreate single-column unique index nếu data hiện không vi phạm.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM (SELECT VoucherCode FROM Vouchers GROUP BY VoucherCode HAVING COUNT(*) > 1) x)
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Vouchers_VoucherCode' AND object_id = OBJECT_ID('Vouchers'))
    CREATE UNIQUE INDEX [IX_Vouchers_VoucherCode] ON [Vouchers]([VoucherCode]);");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM (SELECT SerialCode FROM SerialNumbers GROUP BY SerialCode HAVING COUNT(*) > 1) x)
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SerialNumbers_SerialCode' AND object_id = OBJECT_ID('SerialNumbers'))
    CREATE UNIQUE INDEX [IX_SerialNumbers_SerialCode] ON [SerialNumbers]([SerialCode]);");
        }
    }
}
