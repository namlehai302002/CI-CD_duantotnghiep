using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddPickCartAndTote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PickCarts",
                columns: table => new
                {
                    PickCartId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CartCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ToteCapacity = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickCarts", x => x.PickCartId);
                    table.ForeignKey(
                        name: "FK_PickCarts_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "PickTotes",
                columns: table => new
                {
                    PickToteId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ToteCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PickCartId = table.Column<int>(type: "int", nullable: true),
                    SlotPosition = table.Column<int>(type: "int", nullable: true),
                    WaveId = table.Column<long>(type: "bigint", nullable: true),
                    VoucherId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickTotes", x => x.PickToteId);
                    table.ForeignKey(
                        name: "FK_PickTotes_PickCarts_PickCartId",
                        column: x => x.PickCartId,
                        principalTable: "PickCarts",
                        principalColumn: "PickCartId");
                    table.ForeignKey(
                        name: "FK_PickTotes_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                    table.ForeignKey(
                        name: "FK_PickTotes_Waves_WaveId",
                        column: x => x.WaveId,
                        principalTable: "Waves",
                        principalColumn: "WaveId");
                });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6726));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6934));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6937));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6939));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6942));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6946));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6948));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6950));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6952));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6955));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6957));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6959));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6961));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6963));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6965));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6987));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6990));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(7002));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(7004));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(7006));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(7008));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(7010));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8075));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8088));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8090));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8092));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8093));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 6, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8096));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 7, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8098));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 8, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8099));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 9, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8125));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 10, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8127));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 11, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8129));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8130));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8132));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8133));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8134));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8136));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8137));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 18, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8140));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 19, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8141));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 20, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8142));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8144));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8145));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8159));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8163));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8165));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8168));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8171));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8173));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8177));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8180));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8182));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8185));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8188));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8190));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8192));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8205));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8207));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 4 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8212));

            migrationBuilder.CreateIndex(
                name: "IX_PickCarts_CartCode",
                table: "PickCarts",
                column: "CartCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickCarts_WarehouseId",
                table: "PickCarts",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PickTotes_PickCartId",
                table: "PickTotes",
                column: "PickCartId");

            migrationBuilder.CreateIndex(
                name: "IX_PickTotes_ToteCode",
                table: "PickTotes",
                column: "ToteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickTotes_VoucherId",
                table: "PickTotes",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_PickTotes_WaveId",
                table: "PickTotes",
                column: "WaveId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PickTotes");

            migrationBuilder.DropTable(
                name: "PickCarts");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(6894));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7007));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7011));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7013));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7015));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7019));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7021));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7023));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7025));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7028));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7030));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7032));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7034));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7036));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7037));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7062));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7064));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7075));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7077));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7078));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7080));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7082));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7734));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7806));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7810));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7812));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7814));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 6, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7817));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 7, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7819));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 8, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7821));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 9, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7845));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 10, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7849));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 11, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7851));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7853));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7855));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7857));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7859));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7861));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7864));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 18, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7867));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 19, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7869));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 20, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7871));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7873));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7876));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7893));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7896));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7898));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7900));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7902));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7904));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7906));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7908));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7910));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7912));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7914));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7916));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(7918));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(8009));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(8011));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 4 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 30, 17, 25, 8, 278, DateTimeKind.Unspecified).AddTicks(8016));
        }
    }
}
