using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddYardBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "YardBillingRates",
                columns: table => new
                {
                    YardBillingRateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    CarrierName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrailerType = table.Column<byte>(type: "tinyint", nullable: true),
                    SpotType = table.Column<byte>(type: "tinyint", nullable: true),
                    FreeTimeMinutes = table.Column<int>(type: "int", nullable: false),
                    RatePerHour = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxDailyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YardBillingRates", x => x.YardBillingRateId);
                    table.ForeignKey(
                        name: "FK_YardBillingRates_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "YardBillingCharges",
                columns: table => new
                {
                    YardBillingChargeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    YardVisitId = table.Column<long>(type: "bigint", nullable: false),
                    YardBillingRateId = table.Column<int>(type: "int", nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    TotalDwellMinutes = table.Column<int>(type: "int", nullable: false),
                    FreeTimeMinutes = table.Column<int>(type: "int", nullable: false),
                    ChargeableMinutes = table.Column<int>(type: "int", nullable: false),
                    AppliedRatePerHour = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    WaivedReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    WaivedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WaivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YardBillingCharges", x => x.YardBillingChargeId);
                    table.ForeignKey(
                        name: "FK_YardBillingCharges_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                    table.ForeignKey(
                        name: "FK_YardBillingCharges_YardBillingRates_YardBillingRateId",
                        column: x => x.YardBillingRateId,
                        principalTable: "YardBillingRates",
                        principalColumn: "YardBillingRateId");
                    table.ForeignKey(
                        name: "FK_YardBillingCharges_YardVisits_YardVisitId",
                        column: x => x.YardVisitId,
                        principalTable: "YardVisits",
                        principalColumn: "YardVisitId");
                });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 954, DateTimeKind.Unspecified).AddTicks(9988));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(39));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(42));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(44));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(45));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(49));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(51));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(53));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(56));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(58));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(60));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(62));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(64));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(65));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(67));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(87));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(90));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(104));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(106));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(108));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(110));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(112));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1030));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1112));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1116));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1118));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1120));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 6, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1124));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 7, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1127));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 8, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1129));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 9, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1155));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 10, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1158));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 11, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1160));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1163));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1165));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1166));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1169));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1171));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1173));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 18, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1175));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 19, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1178));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 20, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1180));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1182));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1184));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1206));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1210));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1212));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1214));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1217));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1220));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1223));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1225));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1228));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1231));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1233));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1236));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1238));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1348));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1350));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 4 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 16, 36, 6, 955, DateTimeKind.Unspecified).AddTicks(1356));

            migrationBuilder.CreateIndex(
                name: "IX_YardBillingCharges_Visit_Status",
                table: "YardBillingCharges",
                columns: new[] { "YardVisitId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_YardBillingCharges_WarehouseId",
                table: "YardBillingCharges",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_YardBillingCharges_YardBillingRateId",
                table: "YardBillingCharges",
                column: "YardBillingRateId");

            migrationBuilder.CreateIndex(
                name: "IX_YardBillingRates_WarehouseId",
                table: "YardBillingRates",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YardBillingCharges");

            migrationBuilder.DropTable(
                name: "YardBillingRates");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9315));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9373));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9376));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9378));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9381));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9385));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9388));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9390));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9392));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9395));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9397));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9399));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9401));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9403));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9406));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9429));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9431));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9443));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9445));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9447));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9449));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 642, DateTimeKind.Unspecified).AddTicks(9452));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(397));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(560));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(566));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(568));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(571));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 6, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(576));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 7, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(578));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 8, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(581));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 9, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(606));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 10, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(610));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 11, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(613));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(616));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(618));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(624));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(627));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(629));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 18, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(633));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 19, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(635));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 20, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(637));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(640));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(642));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(723));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(728));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(731));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(734));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(737));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(740));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(744));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(747));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(750));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(754));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(757));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(760));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(763));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(888));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(892));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 4 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 2, 1, 26, 55, 643, DateTimeKind.Unspecified).AddTicks(898));
        }
    }
}
