using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddContainerCentricLpn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_LicensePlates_Warehouse_Item_Location_Active'
                      AND object_id = OBJECT_ID(N'[dbo].[LicensePlates]')
                )
                BEGIN
                    DROP INDEX [IX_LicensePlates_Warehouse_Item_Location_Active] ON [dbo].[LicensePlates];
                END
                """);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "LicensePlates",
                type: "decimal(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)");

            migrationBuilder.AlterColumn<int>(
                name: "ItemId",
                table: "LicensePlates",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<decimal>(
                name: "ActualVolumeCubicCm",
                table: "LicensePlates",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualWeightKg",
                table: "LicensePlates",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentLocationId",
                table: "LicensePlates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HeightCm",
                table: "LicensePlates",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LengthCm",
                table: "LicensePlates",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "LpnType",
                table: "LicensePlates",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)2);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxVolumeCubicCm",
                table: "LicensePlates",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxWeightKg",
                table: "LicensePlates",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ParentLpnId",
                table: "LicensePlates",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "LicensePlates",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "LicensePlates",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<decimal>(
                name: "WidthCm",
                table: "LicensePlates",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LicensePlateDetails",
                columns: table => new
                {
                    LicensePlateDetailId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LicensePlateId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    VoucherDetailId = table.Column<long>(type: "bigint", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "date", nullable: true),
                    ManufacturingDate = table.Column<DateTime>(type: "date", nullable: true),
                    HoldStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicensePlateDetails", x => x.LicensePlateDetailId);
                    table.ForeignKey(
                        name: "FK_LicensePlateDetails_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId");
                    table.ForeignKey(
                        name: "FK_LicensePlateDetails_LicensePlates_LicensePlateId",
                        column: x => x.LicensePlateId,
                        principalTable: "LicensePlates",
                        principalColumn: "LicensePlateId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LicensePlateDetails_VoucherDetails_VoucherDetailId",
                        column: x => x.VoucherDetailId,
                        principalTable: "VoucherDetails",
                        principalColumn: "VoucherDetailId");
                });

            migrationBuilder.Sql("""
                UPDATE lp
                SET CurrentLocationId = lp.LocationId,
                    Status = CASE WHEN lp.IsActive = 1 AND lp.VoidedAt IS NULL THEN CAST(2 AS tinyint) ELSE CAST(7 AS tinyint) END,
                    LpnType = CAST(2 AS tinyint)
                FROM LicensePlates lp;

                INSERT INTO LicensePlateDetails
                    (LicensePlateId, ItemId, VoucherDetailId, Quantity, LotNumber, ExpiryDate, ManufacturingDate, HoldStatus, CreatedAt, UpdatedAt)
                SELECT
                    lp.LicensePlateId,
                    lp.ItemId,
                    lp.VoucherDetailId,
                    lp.Quantity,
                    lp.LotNumber,
                    CAST(lp.ExpiryDate AS date),
                    CAST(lp.ManufacturingDate AS date),
                    CAST(1 AS tinyint),
                    lp.CreatedAt,
                    NULL
                FROM LicensePlates lp
                WHERE lp.ItemId IS NOT NULL
                    AND lp.Quantity IS NOT NULL
                    AND NOT EXISTS (
                        SELECT 1
                        FROM LicensePlateDetails d
                        WHERE d.LicensePlateId = lp.LicensePlateId
                    );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_LicensePlates_CurrentLocationId",
                table: "LicensePlates",
                column: "CurrentLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_LicensePlates_ParentLpnId",
                table: "LicensePlates",
                column: "ParentLpnId");

            migrationBuilder.CreateIndex(
                name: "IX_LicensePlates_Warehouse_Status_Location_Active",
                table: "LicensePlates",
                columns: new[] { "WarehouseId", "Status", "CurrentLocationId", "IsActive" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_LicensePlates_NoSelfParent",
                table: "LicensePlates",
                sql: "[ParentLpnId] IS NULL OR [ParentLpnId] <> [LicensePlateId]");

            migrationBuilder.CreateIndex(
                name: "IX_LicensePlateDetails_Item_Lot_Expiry",
                table: "LicensePlateDetails",
                columns: new[] { "ItemId", "LotNumber", "ExpiryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LicensePlateDetails_LicensePlateId",
                table: "LicensePlateDetails",
                column: "LicensePlateId");

            migrationBuilder.CreateIndex(
                name: "IX_LicensePlateDetails_Lpn_Item_Lot_Expiry",
                table: "LicensePlateDetails",
                columns: new[] { "LicensePlateId", "ItemId", "LotNumber", "ExpiryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LicensePlateDetails_VoucherDetailId",
                table: "LicensePlateDetails",
                column: "VoucherDetailId");

            migrationBuilder.AddForeignKey(
                name: "FK_LicensePlates_LicensePlates_ParentLpnId",
                table: "LicensePlates",
                column: "ParentLpnId",
                principalTable: "LicensePlates",
                principalColumn: "LicensePlateId");

            migrationBuilder.AddForeignKey(
                name: "FK_LicensePlates_Locations_CurrentLocationId",
                table: "LicensePlates",
                column: "CurrentLocationId",
                principalTable: "Locations",
                principalColumn: "LocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LicensePlates_LicensePlates_ParentLpnId",
                table: "LicensePlates");

            migrationBuilder.DropForeignKey(
                name: "FK_LicensePlates_Locations_CurrentLocationId",
                table: "LicensePlates");

            migrationBuilder.DropTable(
                name: "LicensePlateDetails");

            migrationBuilder.DropIndex(
                name: "IX_LicensePlates_CurrentLocationId",
                table: "LicensePlates");

            migrationBuilder.DropIndex(
                name: "IX_LicensePlates_ParentLpnId",
                table: "LicensePlates");

            migrationBuilder.DropIndex(
                name: "IX_LicensePlates_Warehouse_Status_Location_Active",
                table: "LicensePlates");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LicensePlates_NoSelfParent",
                table: "LicensePlates");

            migrationBuilder.DropColumn(
                name: "ActualVolumeCubicCm",
                table: "LicensePlates");

            migrationBuilder.DropColumn(
                name: "ActualWeightKg",
                table: "LicensePlates");

            migrationBuilder.DropColumn(
                name: "CurrentLocationId",
                table: "LicensePlates");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "LicensePlates");

            migrationBuilder.DropColumn(
                name: "LengthCm",
                table: "LicensePlates");

            migrationBuilder.DropColumn(
                name: "LpnType",
                table: "LicensePlates");

            migrationBuilder.DropColumn(
                name: "MaxVolumeCubicCm",
                table: "LicensePlates");

            migrationBuilder.DropColumn(
                name: "MaxWeightKg",
                table: "LicensePlates");

            migrationBuilder.DropColumn(
                name: "ParentLpnId",
                table: "LicensePlates");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "LicensePlates");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "LicensePlates");

            migrationBuilder.DropColumn(
                name: "WidthCm",
                table: "LicensePlates");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "LicensePlates",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ItemId",
                table: "LicensePlates",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LicensePlates_Warehouse_Item_Location_Active",
                table: "LicensePlates",
                columns: new[] { "WarehouseId", "ItemId", "LocationId", "IsActive" });
        }
    }
}
