using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddCatchWeightAndShipmentLoadsEpic6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ShipmentLoadId",
                table: "ShippingHandoverLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualCatchWeight",
                table: "OutboundPackages",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatchWeightUomId",
                table: "OutboundPackages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LoadedAt",
                table: "OutboundPackages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoadedBy",
                table: "OutboundPackages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ShipmentLoadId",
                table: "OutboundPackages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CatchWeightTolerancePercent",
                table: "Items",
                type: "decimal(9,4)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatchWeightUomId",
                table: "Items",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NominalWeightPerBaseUnit",
                table: "Items",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequireCatchWeightAtPickPack",
                table: "Items",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireCatchWeightAtReceive",
                table: "Items",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TrackCatchWeight",
                table: "Items",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CatchWeightEntries",
                columns: table => new
                {
                    CatchWeightEntryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    VoucherId = table.Column<long>(type: "bigint", nullable: true),
                    VoucherDetailId = table.Column<long>(type: "bigint", nullable: true),
                    LicensePlateId = table.Column<long>(type: "bigint", nullable: true),
                    LicensePlateDetailId = table.Column<long>(type: "bigint", nullable: true),
                    OutboundPackageId = table.Column<long>(type: "bigint", nullable: true),
                    PickTaskId = table.Column<long>(type: "bigint", nullable: true),
                    SerialNumberId = table.Column<long>(type: "bigint", nullable: true),
                    BaseQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ActualWeight = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    WeightUomId = table.Column<int>(type: "int", nullable: false),
                    CapturePoint = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CapturedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatchWeightEntries", x => x.CatchWeightEntryId);
                    table.ForeignKey(
                        name: "FK_CatchWeightEntries_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId");
                    table.ForeignKey(
                        name: "FK_CatchWeightEntries_LicensePlateDetails_LicensePlateDetailId",
                        column: x => x.LicensePlateDetailId,
                        principalTable: "LicensePlateDetails",
                        principalColumn: "LicensePlateDetailId");
                    table.ForeignKey(
                        name: "FK_CatchWeightEntries_LicensePlates_LicensePlateId",
                        column: x => x.LicensePlateId,
                        principalTable: "LicensePlates",
                        principalColumn: "LicensePlateId");
                    table.ForeignKey(
                        name: "FK_CatchWeightEntries_OutboundPackages_OutboundPackageId",
                        column: x => x.OutboundPackageId,
                        principalTable: "OutboundPackages",
                        principalColumn: "OutboundPackageId");
                    table.ForeignKey(
                        name: "FK_CatchWeightEntries_PickTasks_PickTaskId",
                        column: x => x.PickTaskId,
                        principalTable: "PickTasks",
                        principalColumn: "PickTaskId");
                    table.ForeignKey(
                        name: "FK_CatchWeightEntries_SerialNumbers_SerialNumberId",
                        column: x => x.SerialNumberId,
                        principalTable: "SerialNumbers",
                        principalColumn: "SerialNumberId");
                    table.ForeignKey(
                        name: "FK_CatchWeightEntries_UnitsOfMeasure_WeightUomId",
                        column: x => x.WeightUomId,
                        principalTable: "UnitsOfMeasure",
                        principalColumn: "UomId");
                    table.ForeignKey(
                        name: "FK_CatchWeightEntries_VoucherDetails_VoucherDetailId",
                        column: x => x.VoucherDetailId,
                        principalTable: "VoucherDetails",
                        principalColumn: "VoucherDetailId");
                    table.ForeignKey(
                        name: "FK_CatchWeightEntries_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                    table.ForeignKey(
                        name: "FK_CatchWeightEntries_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "ShipmentLoads",
                columns: table => new
                {
                    ShipmentLoadId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoadCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CarrierName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RouteCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RouteName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VehicleNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    TrailerId = table.Column<int>(type: "int", nullable: true),
                    YardVisitId = table.Column<long>(type: "bigint", nullable: true),
                    DockDoor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PlannedDepartureAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualDepartureAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SealNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ManifestCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TrackingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TmsReferenceNo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    TotalVoucherCount = table.Column<int>(type: "int", nullable: false),
                    TotalPackageCount = table.Column<int>(type: "int", nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalCatchWeight = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DepartedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DepartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentLoads", x => x.ShipmentLoadId);
                    table.ForeignKey(
                        name: "FK_ShipmentLoads_Trailers_TrailerId",
                        column: x => x.TrailerId,
                        principalTable: "Trailers",
                        principalColumn: "TrailerId");
                    table.ForeignKey(
                        name: "FK_ShipmentLoads_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                    table.ForeignKey(
                        name: "FK_ShipmentLoads_YardVisits_YardVisitId",
                        column: x => x.YardVisitId,
                        principalTable: "YardVisits",
                        principalColumn: "YardVisitId");
                });

            migrationBuilder.CreateTable(
                name: "ShipmentLoadPackages",
                columns: table => new
                {
                    ShipmentLoadPackageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentLoadId = table.Column<long>(type: "bigint", nullable: false),
                    OutboundPackageId = table.Column<long>(type: "bigint", nullable: false),
                    PackageCodeSnapshot = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ReferenceLpnCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsLoaded = table.Column<bool>(type: "bit", nullable: false),
                    LoadedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LoadedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RemovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentLoadPackages", x => x.ShipmentLoadPackageId);
                    table.ForeignKey(
                        name: "FK_ShipmentLoadPackages_OutboundPackages_OutboundPackageId",
                        column: x => x.OutboundPackageId,
                        principalTable: "OutboundPackages",
                        principalColumn: "OutboundPackageId");
                    table.ForeignKey(
                        name: "FK_ShipmentLoadPackages_ShipmentLoads_ShipmentLoadId",
                        column: x => x.ShipmentLoadId,
                        principalTable: "ShipmentLoads",
                        principalColumn: "ShipmentLoadId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentLoadVouchers",
                columns: table => new
                {
                    ShipmentLoadVoucherId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentLoadId = table.Column<long>(type: "bigint", nullable: false),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    StopNumber = table.Column<int>(type: "int", nullable: true),
                    StatusSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AddedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RemovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentLoadVouchers", x => x.ShipmentLoadVoucherId);
                    table.ForeignKey(
                        name: "FK_ShipmentLoadVouchers_ShipmentLoads_ShipmentLoadId",
                        column: x => x.ShipmentLoadId,
                        principalTable: "ShipmentLoads",
                        principalColumn: "ShipmentLoadId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentLoadVouchers_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                });





























































            migrationBuilder.CreateIndex(
                name: "IX_ShippingHandoverLogs_ShipmentLoadId",
                table: "ShippingHandoverLogs",
                column: "ShipmentLoadId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundPackages_CatchWeightUomId",
                table: "OutboundPackages",
                column: "CatchWeightUomId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundPackages_ShipmentLoad",
                table: "OutboundPackages",
                column: "ShipmentLoadId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CatchWeightUomId",
                table: "Items",
                column: "CatchWeightUomId");

            migrationBuilder.CreateIndex(
                name: "IX_CatchWeightEntries_Item_Warehouse_Date",
                table: "CatchWeightEntries",
                columns: new[] { "ItemId", "WarehouseId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CatchWeightEntries_LicensePlateDetailId",
                table: "CatchWeightEntries",
                column: "LicensePlateDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_CatchWeightEntries_LicensePlateId",
                table: "CatchWeightEntries",
                column: "LicensePlateId");

            migrationBuilder.CreateIndex(
                name: "IX_CatchWeightEntries_Package_Status",
                table: "CatchWeightEntries",
                columns: new[] { "OutboundPackageId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CatchWeightEntries_PickTaskId",
                table: "CatchWeightEntries",
                column: "PickTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_CatchWeightEntries_SerialNumberId",
                table: "CatchWeightEntries",
                column: "SerialNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_CatchWeightEntries_VoucherDetail_Status",
                table: "CatchWeightEntries",
                columns: new[] { "VoucherDetailId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CatchWeightEntries_VoucherId",
                table: "CatchWeightEntries",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_CatchWeightEntries_WarehouseId",
                table: "CatchWeightEntries",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_CatchWeightEntries_WeightUomId",
                table: "CatchWeightEntries",
                column: "WeightUomId");

            migrationBuilder.CreateIndex(
                name: "UX_CatchWeightEntries_IdempotencyKey",
                table: "CatchWeightEntries",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentLoadPackages_Load_Loaded",
                table: "ShipmentLoadPackages",
                columns: new[] { "ShipmentLoadId", "IsLoaded" });

            migrationBuilder.CreateIndex(
                name: "UX_ShipmentLoadPackages_Active_Package",
                table: "ShipmentLoadPackages",
                column: "OutboundPackageId",
                unique: true,
                filter: "[RemovedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentLoads_ManifestCode",
                table: "ShipmentLoads",
                column: "ManifestCode",
                filter: "[ManifestCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentLoads_TrailerId",
                table: "ShipmentLoads",
                column: "TrailerId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentLoads_Warehouse_Status_Planned",
                table: "ShipmentLoads",
                columns: new[] { "WarehouseId", "Status", "PlannedDepartureAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentLoads_YardVisitId",
                table: "ShipmentLoads",
                column: "YardVisitId");

            migrationBuilder.CreateIndex(
                name: "UX_ShipmentLoads_LoadCode",
                table: "ShipmentLoads",
                column: "LoadCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentLoadVouchers_Load_Sequence",
                table: "ShipmentLoadVouchers",
                columns: new[] { "ShipmentLoadId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "UX_ShipmentLoadVouchers_Active_Voucher",
                table: "ShipmentLoadVouchers",
                column: "VoucherId",
                unique: true,
                filter: "[RemovedAt] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_UnitsOfMeasure_CatchWeightUomId",
                table: "Items",
                column: "CatchWeightUomId",
                principalTable: "UnitsOfMeasure",
                principalColumn: "UomId");

            migrationBuilder.AddForeignKey(
                name: "FK_OutboundPackages_ShipmentLoads_ShipmentLoadId",
                table: "OutboundPackages",
                column: "ShipmentLoadId",
                principalTable: "ShipmentLoads",
                principalColumn: "ShipmentLoadId");

            migrationBuilder.AddForeignKey(
                name: "FK_OutboundPackages_UnitsOfMeasure_CatchWeightUomId",
                table: "OutboundPackages",
                column: "CatchWeightUomId",
                principalTable: "UnitsOfMeasure",
                principalColumn: "UomId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingHandoverLogs_ShipmentLoads_ShipmentLoadId",
                table: "ShippingHandoverLogs",
                column: "ShipmentLoadId",
                principalTable: "ShipmentLoads",
                principalColumn: "ShipmentLoadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_UnitsOfMeasure_CatchWeightUomId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_OutboundPackages_ShipmentLoads_ShipmentLoadId",
                table: "OutboundPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_OutboundPackages_UnitsOfMeasure_CatchWeightUomId",
                table: "OutboundPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_ShippingHandoverLogs_ShipmentLoads_ShipmentLoadId",
                table: "ShippingHandoverLogs");

            migrationBuilder.DropTable(
                name: "CatchWeightEntries");

            migrationBuilder.DropTable(
                name: "ShipmentLoadPackages");

            migrationBuilder.DropTable(
                name: "ShipmentLoadVouchers");

            migrationBuilder.DropTable(
                name: "ShipmentLoads");

            migrationBuilder.DropIndex(
                name: "IX_ShippingHandoverLogs_ShipmentLoadId",
                table: "ShippingHandoverLogs");

            migrationBuilder.DropIndex(
                name: "IX_OutboundPackages_CatchWeightUomId",
                table: "OutboundPackages");

            migrationBuilder.DropIndex(
                name: "IX_OutboundPackages_ShipmentLoad",
                table: "OutboundPackages");

            migrationBuilder.DropIndex(
                name: "IX_Items_CatchWeightUomId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ShipmentLoadId",
                table: "ShippingHandoverLogs");

            migrationBuilder.DropColumn(
                name: "ActualCatchWeight",
                table: "OutboundPackages");

            migrationBuilder.DropColumn(
                name: "CatchWeightUomId",
                table: "OutboundPackages");

            migrationBuilder.DropColumn(
                name: "LoadedAt",
                table: "OutboundPackages");

            migrationBuilder.DropColumn(
                name: "LoadedBy",
                table: "OutboundPackages");

            migrationBuilder.DropColumn(
                name: "ShipmentLoadId",
                table: "OutboundPackages");

            migrationBuilder.DropColumn(
                name: "CatchWeightTolerancePercent",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "CatchWeightUomId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "NominalWeightPerBaseUnit",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "RequireCatchWeightAtPickPack",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "RequireCatchWeightAtReceive",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "TrackCatchWeight",
                table: "Items");




























































        }
    }
}
