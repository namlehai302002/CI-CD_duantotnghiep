using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddInboundWorkflowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Conditionally drop indexes that may not exist in all environments
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Vouchers_WarehouseId' AND object_id = OBJECT_ID('Vouchers')) DROP INDEX IX_Vouchers_WarehouseId ON Vouchers;");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_VoucherDetails_VoucherId' AND object_id = OBJECT_ID('VoucherDetails')) DROP INDEX IX_VoucherDetails_VoucherId ON VoucherDetails;");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockAlerts_ItemId' AND object_id = OBJECT_ID('StockAlerts')) DROP INDEX IX_StockAlerts_ItemId ON StockAlerts;");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ItemLocations_LocationId' AND object_id = OBJECT_ID('ItemLocations')) DROP INDEX IX_ItemLocations_LocationId ON ItemLocations;");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockReservations_VoucherId_Status' AND object_id = OBJECT_ID('StockReservations')) EXEC sp_rename N'StockReservations.IX_StockReservations_VoucherId_Status', N'IX_StockReservations_Voucher_Status', N'INDEX';");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Vouchers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "Vouchers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Vouchers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletedBy",
                table: "Vouchers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "InboundStatus",
                table: "Vouchers",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedAt",
                table: "Vouchers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceivedBy",
                table: "Vouchers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Vouchers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "Vouchers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedBy",
                table: "Vouchers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManufacturingDate",
                table: "VoucherDetails",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_Type_Date",
                table: "Vouchers",
                columns: new[] { "VoucherType", "VoucherDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_Warehouse_Status_Date",
                table: "Vouchers",
                columns: new[] { "WarehouseId", "IsPosted", "IsCancelled", "VoucherDate" });

            migrationBuilder.CreateIndex(
                name: "IX_VoucherDetails_Voucher_Item",
                table: "VoucherDetails",
                columns: new[] { "VoucherId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_Item_Location_Status",
                table: "StockReservations",
                columns: new[] { "ItemId", "LocationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAlerts_Item_Resolved_Type",
                table: "StockAlerts",
                columns: new[] { "ItemId", "IsResolved", "AlertType" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_Barcode",
                table: "Items",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Type_Active",
                table: "Items",
                columns: new[] { "ItemType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocations_Location_Qty",
                table: "ItemLocations",
                columns: new[] { "LocationId", "Quantity" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Table_Date",
                table: "AuditLogs",
                columns: new[] { "TableName", "ChangedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vouchers_Type_Date",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_Warehouse_Status_Date",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_VoucherDetails_Voucher_Item",
                table: "VoucherDetails");

            migrationBuilder.DropIndex(
                name: "IX_StockReservations_Item_Location_Status",
                table: "StockReservations");

            migrationBuilder.DropIndex(
                name: "IX_StockAlerts_Item_Resolved_Type",
                table: "StockAlerts");

            migrationBuilder.DropIndex(
                name: "IX_Items_Barcode",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_Type_Active",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_ItemLocations_Location_Qty",
                table: "ItemLocations");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Table_Date",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "CompletedBy",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "InboundStatus",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ReceivedAt",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ReceivedBy",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "SubmittedBy",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ManufacturingDate",
                table: "VoucherDetails");

            migrationBuilder.RenameIndex(
                name: "IX_StockReservations_Voucher_Status",
                table: "StockReservations",
                newName: "IX_StockReservations_VoucherId_Status");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_WarehouseId",
                table: "Vouchers",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_VoucherDetails_VoucherId",
                table: "VoucherDetails",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAlerts_ItemId",
                table: "StockAlerts",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocations_LocationId",
                table: "ItemLocations",
                column: "LocationId");
        }
    }
}
