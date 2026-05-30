using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddVasWorkOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VasWorkOrders",
                columns: table => new
                {
                    VasWorkOrderId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkOrderCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OperationType = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    PartnerId = table.Column<int>(type: "int", nullable: true),
                    VoucherId = table.Column<long>(type: "bigint", nullable: true),
                    PrimaryItemId = table.Column<int>(type: "int", nullable: false),
                    PlannedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CompletedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    QcPassedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    QcFailedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    QcResult = table.Column<byte>(type: "tinyint", nullable: false),
                    QcNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QcBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    QcAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualLaborMinutes = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LaborRatePerHour = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    MaterialCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LaborCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReservedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReservedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VasWorkOrders", x => x.VasWorkOrderId);
                    table.ForeignKey(
                        name: "FK_VasWorkOrders_Items_PrimaryItemId",
                        column: x => x.PrimaryItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId");
                    table.ForeignKey(
                        name: "FK_VasWorkOrders_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VasWorkOrders_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                    table.ForeignKey(
                        name: "FK_VasWorkOrders_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "VasMaterialLines",
                columns: table => new
                {
                    VasMaterialLineId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VasWorkOrderId = table.Column<long>(type: "bigint", nullable: false),
                    MaterialItemId = table.Column<int>(type: "int", nullable: false),
                    SourceLocationId = table.Column<int>(type: "int", nullable: true),
                    SourceItemLocationId = table.Column<int>(type: "int", nullable: true),
                    RequiredQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReservedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ConsumedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReleasedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitCostSnapshot = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ConsumedCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "date", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VasMaterialLines", x => x.VasMaterialLineId);
                    table.ForeignKey(
                        name: "FK_VasMaterialLines_ItemLocations_SourceItemLocationId",
                        column: x => x.SourceItemLocationId,
                        principalTable: "ItemLocations",
                        principalColumn: "ItemLocationId");
                    table.ForeignKey(
                        name: "FK_VasMaterialLines_Items_MaterialItemId",
                        column: x => x.MaterialItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId");
                    table.ForeignKey(
                        name: "FK_VasMaterialLines_Locations_SourceLocationId",
                        column: x => x.SourceLocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_VasMaterialLines_VasWorkOrders_VasWorkOrderId",
                        column: x => x.VasWorkOrderId,
                        principalTable: "VasWorkOrders",
                        principalColumn: "VasWorkOrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VasOperations",
                columns: table => new
                {
                    VasOperationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VasWorkOrderId = table.Column<long>(type: "bigint", nullable: false),
                    StepNumber = table.Column<int>(type: "int", nullable: false),
                    OperationName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualMinutes = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VasOperations", x => x.VasOperationId);
                    table.ForeignKey(
                        name: "FK_VasOperations_VasWorkOrders_VasWorkOrderId",
                        column: x => x.VasWorkOrderId,
                        principalTable: "VasWorkOrders",
                        principalColumn: "VasWorkOrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(57));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(109));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(112));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(114));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(117));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(196));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(199));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(201));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(203));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(208));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(210));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(212));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(214));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(216));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(218));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(242));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(244));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(260));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(262));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(264));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(266));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(269));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1655));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1871));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1875));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1877));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1880));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 6, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1884));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 7, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1886));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 8, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1889));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 9, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1923));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 10, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1928));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 11, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1931));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1934));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1937));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1939));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1941));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1943));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1946));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 18, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1949));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 19, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1951));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 20, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1954));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1956));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1958));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1976));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1981));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1984));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1988));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1992));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1995));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(1999));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(2003));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(2006));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(2010));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(2013));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(2017));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(2020));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(2316));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(2319));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 4 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 41, 15, 217, DateTimeKind.Unspecified).AddTicks(2327));

            migrationBuilder.CreateIndex(
                name: "IX_VasMaterialLines_MaterialItemId",
                table: "VasMaterialLines",
                column: "MaterialItemId");

            migrationBuilder.CreateIndex(
                name: "IX_VasMaterialLines_SourceItemLocationId",
                table: "VasMaterialLines",
                column: "SourceItemLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_VasMaterialLines_SourceLocationId",
                table: "VasMaterialLines",
                column: "SourceLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_VasMaterialLines_WorkOrder_Item_Status",
                table: "VasMaterialLines",
                columns: new[] { "VasWorkOrderId", "MaterialItemId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_VasOperations_WorkOrder_Step",
                table: "VasOperations",
                columns: new[] { "VasWorkOrderId", "StepNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VasWorkOrders_Partner_Date",
                table: "VasWorkOrders",
                columns: new[] { "PartnerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VasWorkOrders_PrimaryItemId",
                table: "VasWorkOrders",
                column: "PrimaryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_VasWorkOrders_VoucherId",
                table: "VasWorkOrders",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_VasWorkOrders_Warehouse_Status_Date",
                table: "VasWorkOrders",
                columns: new[] { "WarehouseId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VasWorkOrders_WorkOrderCode",
                table: "VasWorkOrders",
                column: "WorkOrderCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VasMaterialLines");

            migrationBuilder.DropTable(
                name: "VasOperations");

            migrationBuilder.DropTable(
                name: "VasWorkOrders");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(718));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(769));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(772));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(774));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(777));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(782));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(784));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(787));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(790));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(794));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(797));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(800));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(802));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(805));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(807));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(913));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(915));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(928));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(931));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(933));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(936));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(938));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(1918));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2137));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2141));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2143));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2146));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 6, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2150));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 7, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2152));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 8, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2155));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 9, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2186));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 10, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2190));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 11, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2192));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2195));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2198));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2200));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2203));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2205));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2207));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 18, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2210));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 19, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2213));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 20, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2215));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2218));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2220));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2236));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2240));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2243));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2246));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2249));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2252));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2255));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2258));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2261));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2264));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2444));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2449));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2452));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2571));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2574));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 4 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2580));
        }
    }
}
