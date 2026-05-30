using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddLargeWarehouseExecutionCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "FulfillmentStatus",
                table: "Vouchers",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<long>(
                name: "WaveId",
                table: "Vouchers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReservedQty",
                table: "ItemLocations",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "StockReservations",
                columns: table => new
                {
                    StockReservationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
                    VoucherDetailId = table.Column<long>(type: "bigint", nullable: true),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "date", nullable: true),
                    ReservedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ConsumedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReleasedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReservations", x => x.StockReservationId);
                    table.ForeignKey(
                        name: "FK_StockReservations_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId");
                    table.ForeignKey(
                        name: "FK_StockReservations_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_StockReservations_VoucherDetails_VoucherDetailId",
                        column: x => x.VoucherDetailId,
                        principalTable: "VoucherDetails",
                        principalColumn: "VoucherDetailId");
                    table.ForeignKey(
                        name: "FK_StockReservations_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                });

            migrationBuilder.CreateTable(
                name: "Waves",
                columns: table => new
                {
                    WaveId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WaveCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Waves", x => x.WaveId);
                    table.ForeignKey(
                        name: "FK_Waves_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "PickTasks",
                columns: table => new
                {
                    PickTaskId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    WaveId = table.Column<long>(type: "bigint", nullable: false),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
                    VoucherDetailId = table.Column<long>(type: "bigint", nullable: true),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    SourceLocationId = table.Column<int>(type: "int", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "date", nullable: true),
                    TargetQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PickedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    AssignedTo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickTasks", x => x.PickTaskId);
                    table.ForeignKey(
                        name: "FK_PickTasks_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId");
                    table.ForeignKey(
                        name: "FK_PickTasks_Locations_SourceLocationId",
                        column: x => x.SourceLocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_PickTasks_VoucherDetails_VoucherDetailId",
                        column: x => x.VoucherDetailId,
                        principalTable: "VoucherDetails",
                        principalColumn: "VoucherDetailId");
                    table.ForeignKey(
                        name: "FK_PickTasks_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                    table.ForeignKey(
                        name: "FK_PickTasks_Waves_WaveId",
                        column: x => x.WaveId,
                        principalTable: "Waves",
                        principalColumn: "WaveId");
                });

            migrationBuilder.CreateTable(
                name: "WaveLines",
                columns: table => new
                {
                    WaveLineId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WaveId = table.Column<long>(type: "bigint", nullable: false),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    RequiredQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PickedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaveLines", x => x.WaveLineId);
                    table.ForeignKey(
                        name: "FK_WaveLines_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId");
                    table.ForeignKey(
                        name: "FK_WaveLines_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                    table.ForeignKey(
                        name: "FK_WaveLines_Waves_WaveId",
                        column: x => x.WaveId,
                        principalTable: "Waves",
                        principalColumn: "WaveId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PickTaskScanLogs",
                columns: table => new
                {
                    PickTaskScanLogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PickTaskId = table.Column<long>(type: "bigint", nullable: false),
                    ScannedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ScanValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ScannedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickTaskScanLogs", x => x.PickTaskScanLogId);
                    table.ForeignKey(
                        name: "FK_PickTaskScanLogs_PickTasks_PickTaskId",
                        column: x => x.PickTaskId,
                        principalTable: "PickTasks",
                        principalColumn: "PickTaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_WaveId",
                table: "Vouchers",
                column: "WaveId");

            migrationBuilder.CreateIndex(
                name: "IX_PickTasks_ItemId",
                table: "PickTasks",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PickTasks_SourceLocationId",
                table: "PickTasks",
                column: "SourceLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PickTasks_TaskCode",
                table: "PickTasks",
                column: "TaskCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickTasks_VoucherDetailId",
                table: "PickTasks",
                column: "VoucherDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_PickTasks_VoucherId",
                table: "PickTasks",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_PickTasks_WaveId_Status_AssignedTo",
                table: "PickTasks",
                columns: new[] { "WaveId", "Status", "AssignedTo" });

            migrationBuilder.CreateIndex(
                name: "IX_PickTaskScanLogs_PickTaskId",
                table: "PickTaskScanLogs",
                column: "PickTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_ItemId_LocationId_LotNumber_ExpiryDate_Status",
                table: "StockReservations",
                columns: new[] { "ItemId", "LocationId", "LotNumber", "ExpiryDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_LocationId",
                table: "StockReservations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_VoucherDetailId",
                table: "StockReservations",
                column: "VoucherDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_VoucherId_Status",
                table: "StockReservations",
                columns: new[] { "VoucherId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WaveLines_ItemId",
                table: "WaveLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_WaveLines_VoucherId",
                table: "WaveLines",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_WaveLines_WaveId_VoucherId_ItemId",
                table: "WaveLines",
                columns: new[] { "WaveId", "VoucherId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_Waves_WarehouseId",
                table: "Waves",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Waves_WaveCode",
                table: "Waves",
                column: "WaveCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_Waves_WaveId",
                table: "Vouchers",
                column: "WaveId",
                principalTable: "Waves",
                principalColumn: "WaveId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_Waves_WaveId",
                table: "Vouchers");

            migrationBuilder.DropTable(
                name: "PickTaskScanLogs");

            migrationBuilder.DropTable(
                name: "StockReservations");

            migrationBuilder.DropTable(
                name: "WaveLines");

            migrationBuilder.DropTable(
                name: "PickTasks");

            migrationBuilder.DropTable(
                name: "Waves");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_WaveId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "FulfillmentStatus",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "WaveId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ReservedQty",
                table: "ItemLocations");
        }
    }
}
