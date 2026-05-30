using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboundSerialAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConsumedAt",
                table: "SerialNumbers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsumedBy",
                table: "SerialNumbers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ConsumedPickTaskId",
                table: "SerialNumbers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ConsumedVoucherId",
                table: "SerialNumbers",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PickTaskSerialAssignments",
                columns: table => new
                {
                    PickTaskSerialAssignmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PickTaskId = table.Column<long>(type: "bigint", nullable: false),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
                    VoucherDetailId = table.Column<long>(type: "bigint", nullable: true),
                    SerialNumberId = table.Column<long>(type: "bigint", nullable: false),
                    SerialCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ScannedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickTaskSerialAssignments", x => x.PickTaskSerialAssignmentId);
                    table.ForeignKey(
                        name: "FK_PickTaskSerialAssignments_PickTasks_PickTaskId",
                        column: x => x.PickTaskId,
                        principalTable: "PickTasks",
                        principalColumn: "PickTaskId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PickTaskSerialAssignments_SerialNumbers_SerialNumberId",
                        column: x => x.SerialNumberId,
                        principalTable: "SerialNumbers",
                        principalColumn: "SerialNumberId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_ConsumedVoucherId",
                table: "SerialNumbers",
                column: "ConsumedVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_PickTaskSerialAssignments_SerialNumberId",
                table: "PickTaskSerialAssignments",
                column: "SerialNumberId",
                unique: true,
                filter: "[VoidedAt] IS NULL AND [PostedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PickTaskSerialAssignments_Task_Status",
                table: "PickTaskSerialAssignments",
                columns: new[] { "PickTaskId", "PostedAt", "VoidedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_SerialNumbers_Vouchers_ConsumedVoucherId",
                table: "SerialNumbers",
                column: "ConsumedVoucherId",
                principalTable: "Vouchers",
                principalColumn: "VoucherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SerialNumbers_Vouchers_ConsumedVoucherId",
                table: "SerialNumbers");

            migrationBuilder.DropTable(
                name: "PickTaskSerialAssignments");

            migrationBuilder.DropIndex(
                name: "IX_SerialNumbers_ConsumedVoucherId",
                table: "SerialNumbers");

            migrationBuilder.DropColumn(
                name: "ConsumedAt",
                table: "SerialNumbers");

            migrationBuilder.DropColumn(
                name: "ConsumedBy",
                table: "SerialNumbers");

            migrationBuilder.DropColumn(
                name: "ConsumedPickTaskId",
                table: "SerialNumbers");

            migrationBuilder.DropColumn(
                name: "ConsumedVoucherId",
                table: "SerialNumbers");
        }
    }
}
