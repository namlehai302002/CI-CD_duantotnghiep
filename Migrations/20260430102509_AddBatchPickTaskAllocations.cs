using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchPickTaskAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BatchGroupKey",
                table: "PickTasks",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBatchPick",
                table: "PickTasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PickTaskAllocations",
                columns: table => new
                {
                    PickTaskAllocationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PickTaskId = table.Column<long>(type: "bigint", nullable: false),
                    StockReservationId = table.Column<long>(type: "bigint", nullable: false),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
                    VoucherDetailId = table.Column<long>(type: "bigint", nullable: true),
                    AllocatedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PickedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickTaskAllocations", x => x.PickTaskAllocationId);
                    table.ForeignKey(
                        name: "FK_PickTaskAllocations_PickTasks_PickTaskId",
                        column: x => x.PickTaskId,
                        principalTable: "PickTasks",
                        principalColumn: "PickTaskId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PickTaskAllocations_StockReservations_StockReservationId",
                        column: x => x.StockReservationId,
                        principalTable: "StockReservations",
                        principalColumn: "StockReservationId");
                    table.ForeignKey(
                        name: "FK_PickTaskAllocations_VoucherDetails_VoucherDetailId",
                        column: x => x.VoucherDetailId,
                        principalTable: "VoucherDetails",
                        principalColumn: "VoucherDetailId");
                    table.ForeignKey(
                        name: "FK_PickTaskAllocations_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PickTaskAllocations_PickTaskId",
                table: "PickTaskAllocations",
                column: "PickTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_PickTaskAllocations_StockReservationId",
                table: "PickTaskAllocations",
                column: "StockReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_PickTaskAllocations_VoucherDetailId",
                table: "PickTaskAllocations",
                column: "VoucherDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_PickTaskAllocations_VoucherId",
                table: "PickTaskAllocations",
                column: "VoucherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PickTaskAllocations");

            migrationBuilder.DropColumn(
                name: "BatchGroupKey",
                table: "PickTasks");

            migrationBuilder.DropColumn(
                name: "IsBatchPick",
                table: "PickTasks");
        }
    }
}
