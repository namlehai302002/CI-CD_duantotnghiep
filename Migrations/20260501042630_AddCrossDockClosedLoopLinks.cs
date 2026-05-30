using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddCrossDockClosedLoopLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "CrossDockTasks",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "InboundVoucherDetailId",
                table: "CrossDockTasks",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LotNumber",
                table: "CrossDockTasks",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OutboundVoucherDetailId",
                table: "CrossDockTasks",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StockReservationId",
                table: "CrossDockTasks",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrossDockTasks_InboundVoucherDetailId_Status",
                table: "CrossDockTasks",
                columns: new[] { "InboundVoucherDetailId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CrossDockTasks_OutboundVoucherDetailId_Status",
                table: "CrossDockTasks",
                columns: new[] { "OutboundVoucherDetailId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CrossDockTasks_StockReservationId",
                table: "CrossDockTasks",
                column: "StockReservationId");

            migrationBuilder.AddForeignKey(
                name: "FK_CrossDockTasks_StockReservations_StockReservationId",
                table: "CrossDockTasks",
                column: "StockReservationId",
                principalTable: "StockReservations",
                principalColumn: "StockReservationId");

            migrationBuilder.AddForeignKey(
                name: "FK_CrossDockTasks_VoucherDetails_InboundVoucherDetailId",
                table: "CrossDockTasks",
                column: "InboundVoucherDetailId",
                principalTable: "VoucherDetails",
                principalColumn: "VoucherDetailId");

            migrationBuilder.AddForeignKey(
                name: "FK_CrossDockTasks_VoucherDetails_OutboundVoucherDetailId",
                table: "CrossDockTasks",
                column: "OutboundVoucherDetailId",
                principalTable: "VoucherDetails",
                principalColumn: "VoucherDetailId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CrossDockTasks_StockReservations_StockReservationId",
                table: "CrossDockTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_CrossDockTasks_VoucherDetails_InboundVoucherDetailId",
                table: "CrossDockTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_CrossDockTasks_VoucherDetails_OutboundVoucherDetailId",
                table: "CrossDockTasks");

            migrationBuilder.DropIndex(
                name: "IX_CrossDockTasks_InboundVoucherDetailId_Status",
                table: "CrossDockTasks");

            migrationBuilder.DropIndex(
                name: "IX_CrossDockTasks_OutboundVoucherDetailId_Status",
                table: "CrossDockTasks");

            migrationBuilder.DropIndex(
                name: "IX_CrossDockTasks_StockReservationId",
                table: "CrossDockTasks");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "CrossDockTasks");

            migrationBuilder.DropColumn(
                name: "InboundVoucherDetailId",
                table: "CrossDockTasks");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                table: "CrossDockTasks");

            migrationBuilder.DropColumn(
                name: "OutboundVoucherDetailId",
                table: "CrossDockTasks");

            migrationBuilder.DropColumn(
                name: "StockReservationId",
                table: "CrossDockTasks");
        }
    }
}
