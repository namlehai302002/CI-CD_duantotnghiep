using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryConsistencyAndLpnMovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_MovementTasks_Open_DuplicateGuard'
                      AND object_id = OBJECT_ID(N'[dbo].[MovementTasks]')
                )
                BEGIN
                    DROP INDEX [IX_MovementTasks_Open_DuplicateGuard] ON [dbo].[MovementTasks];
                END;

                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_ItemLocations_ItemId_LocationId'
                      AND object_id = OBJECT_ID(N'[dbo].[ItemLocations]')
                )
                BEGIN
                    DROP INDEX [IX_ItemLocations_ItemId_LocationId] ON [dbo].[ItemLocations];
                END;

                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_ItemLocations_ItemId_LocationId_ExpiryDate'
                      AND object_id = OBJECT_ID(N'[dbo].[ItemLocations]')
                )
                BEGIN
                    DROP INDEX [IX_ItemLocations_ItemId_LocationId_ExpiryDate] ON [dbo].[ItemLocations];
                END;

                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_ItemLocations_ItemId_LocationId_LotNumber'
                      AND object_id = OBJECT_ID(N'[dbo].[ItemLocations]')
                )
                BEGIN
                    DROP INDEX [IX_ItemLocations_ItemId_LocationId_LotNumber] ON [dbo].[ItemLocations];
                END;

                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_ItemLocations_ItemId_LocationId_LotNumber_ExpiryDate'
                      AND object_id = OBJECT_ID(N'[dbo].[ItemLocations]')
                )
                BEGIN
                    DROP INDEX [IX_ItemLocations_ItemId_LocationId_LotNumber_ExpiryDate] ON [dbo].[ItemLocations];
                END;
                """);

            migrationBuilder.AddColumn<long>(
                name: "LicensePlateId",
                table: "MovementTasks",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LpnCodeSnapshot",
                table: "MovementTasks",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LpnDetailCount",
                table: "MovementTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LpnDistinctItemCount",
                table: "MovementTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte>(
                name: "MovementMode",
                table: "MovementTasks",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.CreateTable(
                name: "InventoryReconciliationRuns",
                columns: table => new
                {
                    InventoryReconciliationRunId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedRowCount = table.Column<int>(type: "int", nullable: false),
                    SnapshotRowCount = table.Column<int>(type: "int", nullable: false),
                    IssueCount = table.Column<int>(type: "int", nullable: false),
                    AutoHealedCount = table.Column<int>(type: "int", nullable: false),
                    AlertCount = table.Column<int>(type: "int", nullable: false),
                    ToleranceQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryReconciliationRuns", x => x.InventoryReconciliationRunId);
                    table.ForeignKey(
                        name: "FK_InventoryReconciliationRuns_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "InventorySnapshotOutbox",
                columns: table => new
                {
                    InventorySnapshotOutboxId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<byte>(type: "tinyint", nullable: false),
                    LicensePlateId = table.Column<long>(type: "bigint", nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    SourceLocationId = table.Column<int>(type: "int", nullable: true),
                    DestinationLocationId = table.Column<int>(type: "int", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventorySnapshotOutbox", x => x.InventorySnapshotOutboxId);
                    table.ForeignKey(
                        name: "FK_InventorySnapshotOutbox_LicensePlates_LicensePlateId",
                        column: x => x.LicensePlateId,
                        principalTable: "LicensePlates",
                        principalColumn: "LicensePlateId");
                    table.ForeignKey(
                        name: "FK_InventorySnapshotOutbox_Locations_DestinationLocationId",
                        column: x => x.DestinationLocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_InventorySnapshotOutbox_Locations_SourceLocationId",
                        column: x => x.SourceLocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_InventorySnapshotOutbox_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "InventoryReconciliationIssues",
                columns: table => new
                {
                    InventoryReconciliationIssueId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryReconciliationRunId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: true),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "date", nullable: true),
                    HoldStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    ExpectedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SnapshotQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DeltaQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Action = table.Column<byte>(type: "tinyint", nullable: false),
                    Severity = table.Column<byte>(type: "tinyint", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryReconciliationIssues", x => x.InventoryReconciliationIssueId);
                    table.ForeignKey(
                        name: "FK_InventoryReconciliationIssues_InventoryReconciliationRuns_InventoryReconciliationRunId",
                        column: x => x.InventoryReconciliationRunId,
                        principalTable: "InventoryReconciliationRuns",
                        principalColumn: "InventoryReconciliationRunId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryReconciliationIssues_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId");
                    table.ForeignKey(
                        name: "FK_InventoryReconciliationIssues_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_InventoryReconciliationIssues_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2505));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2617));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2620));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2623));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2625));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2629));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2632));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2634));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2636));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2700));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2703));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2705));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2707));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2709));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2711));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2732));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2734));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2746));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2748));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2750));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2752));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(2754));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3605));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3681));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3685));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3688));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3690));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 6, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3695));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 7, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3698));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 8, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3701));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 9, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3727));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 10, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3731));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 11, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3733));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3736));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3739));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3741));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3744));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3747));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3749));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 18, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3752));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 19, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3754));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 20, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3757));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3759));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3762));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3778));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3781));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3784));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3787));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3790));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3793));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3796));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3799));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3802));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3805));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3808));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3811));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3814));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3917));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3920));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 4 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 8, 11, 25, 148, DateTimeKind.Unspecified).AddTicks(3927));

            migrationBuilder.CreateIndex(
                name: "IX_MovementTasks_ItemId",
                table: "MovementTasks",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MovementTasks_Open_Item_DuplicateGuard",
                table: "MovementTasks",
                columns: new[] { "MovementMode", "ItemId", "SourceLocationId", "DestinationLocationId", "TaskType" },
                unique: true,
                filter: "[Status] IN (1, 2, 3) AND [MovementMode] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_MovementTasks_Open_Lpn_DuplicateGuard",
                table: "MovementTasks",
                column: "LicensePlateId",
                unique: true,
                filter: "[LicensePlateId] IS NOT NULL AND [Status] IN (1, 2, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_MovementTasks_Warehouse_Status_Mode",
                table: "MovementTasks",
                columns: new[] { "WarehouseId", "Status", "MovementMode", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocations_Item_Location_Expiry_Hold",
                table: "ItemLocations",
                columns: new[] { "ItemId", "LocationId", "ExpiryDate", "HoldStatus" },
                unique: true,
                filter: "[LotNumber] IS NULL AND [ExpiryDate] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocations_Item_Location_Hold_NoBatch",
                table: "ItemLocations",
                columns: new[] { "ItemId", "LocationId", "HoldStatus" },
                unique: true,
                filter: "[LotNumber] IS NULL AND [ExpiryDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocations_Item_Location_Lot_Expiry_Hold",
                table: "ItemLocations",
                columns: new[] { "ItemId", "LocationId", "LotNumber", "ExpiryDate", "HoldStatus" },
                unique: true,
                filter: "[LotNumber] IS NOT NULL AND [ExpiryDate] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocations_Item_Location_Lot_Hold",
                table: "ItemLocations",
                columns: new[] { "ItemId", "LocationId", "LotNumber", "HoldStatus" },
                unique: true,
                filter: "[LotNumber] IS NOT NULL AND [ExpiryDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReconciliationIssues_InventoryReconciliationRunId",
                table: "InventoryReconciliationIssues",
                column: "InventoryReconciliationRunId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReconciliationIssues_ItemId",
                table: "InventoryReconciliationIssues",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReconciliationIssues_Key_Open",
                table: "InventoryReconciliationIssues",
                columns: new[] { "WarehouseId", "ItemId", "LocationId", "IsResolved", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReconciliationIssues_LocationId",
                table: "InventoryReconciliationIssues",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReconciliationRuns_Warehouse_Started",
                table: "InventoryReconciliationRuns",
                columns: new[] { "WarehouseId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshotOutbox_DestinationLocationId",
                table: "InventorySnapshotOutbox",
                column: "DestinationLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshotOutbox_IdempotencyKey",
                table: "InventorySnapshotOutbox",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshotOutbox_Lpn_Event_Created",
                table: "InventorySnapshotOutbox",
                columns: new[] { "LicensePlateId", "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshotOutbox_SourceLocationId",
                table: "InventorySnapshotOutbox",
                column: "SourceLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshotOutbox_Status_NextAttempt_Created",
                table: "InventorySnapshotOutbox",
                columns: new[] { "Status", "NextAttemptAt", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshotOutbox_WarehouseId",
                table: "InventorySnapshotOutbox",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovementTasks_LicensePlates_LicensePlateId",
                table: "MovementTasks",
                column: "LicensePlateId",
                principalTable: "LicensePlates",
                principalColumn: "LicensePlateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovementTasks_LicensePlates_LicensePlateId",
                table: "MovementTasks");

            migrationBuilder.DropTable(
                name: "InventoryReconciliationIssues");

            migrationBuilder.DropTable(
                name: "InventorySnapshotOutbox");

            migrationBuilder.DropTable(
                name: "InventoryReconciliationRuns");

            migrationBuilder.DropIndex(
                name: "IX_MovementTasks_ItemId",
                table: "MovementTasks");

            migrationBuilder.DropIndex(
                name: "IX_MovementTasks_Open_Item_DuplicateGuard",
                table: "MovementTasks");

            migrationBuilder.DropIndex(
                name: "IX_MovementTasks_Open_Lpn_DuplicateGuard",
                table: "MovementTasks");

            migrationBuilder.DropIndex(
                name: "IX_MovementTasks_Warehouse_Status_Mode",
                table: "MovementTasks");

            migrationBuilder.DropIndex(
                name: "IX_ItemLocations_Item_Location_Expiry_Hold",
                table: "ItemLocations");

            migrationBuilder.DropIndex(
                name: "IX_ItemLocations_Item_Location_Hold_NoBatch",
                table: "ItemLocations");

            migrationBuilder.DropIndex(
                name: "IX_ItemLocations_Item_Location_Lot_Expiry_Hold",
                table: "ItemLocations");

            migrationBuilder.DropIndex(
                name: "IX_ItemLocations_Item_Location_Lot_Hold",
                table: "ItemLocations");

            migrationBuilder.DropColumn(
                name: "LicensePlateId",
                table: "MovementTasks");

            migrationBuilder.DropColumn(
                name: "LpnCodeSnapshot",
                table: "MovementTasks");

            migrationBuilder.DropColumn(
                name: "LpnDetailCount",
                table: "MovementTasks");

            migrationBuilder.DropColumn(
                name: "LpnDistinctItemCount",
                table: "MovementTasks");

            migrationBuilder.DropColumn(
                name: "MovementMode",
                table: "MovementTasks");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3089));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3138));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3263));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3268));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3270));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3275));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3277));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3280));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3282));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3286));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3288));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3291));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3293));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3295));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3297));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3322));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3324));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3335));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3338));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3340));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3342));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(3345));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4073));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4148));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4152));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4155));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4158));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 6, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4162));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 7, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4165));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 8, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4167));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 9, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4195));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 10, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4199));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 11, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4202));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4204));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4207));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4209));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4212));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4215));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4217));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 18, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4222));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 19, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4224));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 20, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4227));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4230));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4232));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4252));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4256));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4260));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4263));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4267));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4271));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4274));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4277));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4280));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4284));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4287));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4290));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4293));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4393));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4396));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 4 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 7, 40, 33, 476, DateTimeKind.Unspecified).AddTicks(4403));

            migrationBuilder.CreateIndex(
                name: "IX_MovementTasks_Open_DuplicateGuard",
                table: "MovementTasks",
                columns: new[] { "ItemId", "SourceLocationId", "DestinationLocationId", "TaskType" },
                unique: true,
                filter: "[Status] IN (1, 2, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocations_ItemId_LocationId",
                table: "ItemLocations",
                columns: new[] { "ItemId", "LocationId" },
                unique: true,
                filter: "[LotNumber] IS NULL AND [ExpiryDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocations_ItemId_LocationId_ExpiryDate",
                table: "ItemLocations",
                columns: new[] { "ItemId", "LocationId", "ExpiryDate" },
                unique: true,
                filter: "[LotNumber] IS NULL AND [ExpiryDate] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocations_ItemId_LocationId_LotNumber",
                table: "ItemLocations",
                columns: new[] { "ItemId", "LocationId", "LotNumber" },
                unique: true,
                filter: "[LotNumber] IS NOT NULL AND [ExpiryDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocations_ItemId_LocationId_LotNumber_ExpiryDate",
                table: "ItemLocations",
                columns: new[] { "ItemId", "LocationId", "LotNumber", "ExpiryDate" },
                unique: true,
                filter: "[LotNumber] IS NOT NULL AND [ExpiryDate] IS NOT NULL");
        }
    }
}
