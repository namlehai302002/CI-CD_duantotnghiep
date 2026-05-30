using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddSerialInventoryUnitReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_SerialNumbers_LicensePlateId'
                      AND object_id = OBJECT_ID(N'[dbo].[SerialNumbers]')
                )
                BEGIN
                    DROP INDEX [IX_SerialNumbers_LicensePlateId] ON [dbo].[SerialNumbers];
                END
                """);

            migrationBuilder.AddColumn<byte>(
                name: "HoldStatus",
                table: "SerialNumbers",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<long>(
                name: "SerialReservationId",
                table: "PickTaskSerialAssignments",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SerialReservations",
                columns: table => new
                {
                    SerialReservationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SerialNumberId = table.Column<long>(type: "bigint", nullable: false),
                    StockReservationId = table.Column<long>(type: "bigint", nullable: true),
                    PickTaskId = table.Column<long>(type: "bigint", nullable: true),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
                    VoucherDetailId = table.Column<long>(type: "bigint", nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    LicensePlateId = table.Column<long>(type: "bigint", nullable: true),
                    LotNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "date", nullable: true),
                    HoldStatus = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    ReservedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReservedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PickedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PickedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConsumedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConsumedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReleasedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReleasedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SerialReservations", x => x.SerialReservationId);
                    table.ForeignKey(
                        name: "FK_SerialReservations_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId");
                    table.ForeignKey(
                        name: "FK_SerialReservations_LicensePlates_LicensePlateId",
                        column: x => x.LicensePlateId,
                        principalTable: "LicensePlates",
                        principalColumn: "LicensePlateId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SerialReservations_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_SerialReservations_PickTasks_PickTaskId",
                        column: x => x.PickTaskId,
                        principalTable: "PickTasks",
                        principalColumn: "PickTaskId");
                    table.ForeignKey(
                        name: "FK_SerialReservations_SerialNumbers_SerialNumberId",
                        column: x => x.SerialNumberId,
                        principalTable: "SerialNumbers",
                        principalColumn: "SerialNumberId");
                    table.ForeignKey(
                        name: "FK_SerialReservations_StockReservations_StockReservationId",
                        column: x => x.StockReservationId,
                        principalTable: "StockReservations",
                        principalColumn: "StockReservationId");
                    table.ForeignKey(
                        name: "FK_SerialReservations_VoucherDetails_VoucherDetailId",
                        column: x => x.VoucherDetailId,
                        principalTable: "VoucherDetails",
                        principalColumn: "VoucherDetailId");
                    table.ForeignKey(
                        name: "FK_SerialReservations_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                    table.ForeignKey(
                        name: "FK_SerialReservations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "SerialInventoryOperations",
                columns: table => new
                {
                    SerialInventoryOperationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ReferenceId = table.Column<long>(type: "bigint", nullable: true),
                    SerialNumberId = table.Column<long>(type: "bigint", nullable: true),
                    SerialReservationId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SerialInventoryOperations", x => x.SerialInventoryOperationId);
                    table.ForeignKey(
                        name: "FK_SerialInventoryOperations_SerialNumbers_SerialNumberId",
                        column: x => x.SerialNumberId,
                        principalTable: "SerialNumbers",
                        principalColumn: "SerialNumberId");
                    table.ForeignKey(
                        name: "FK_SerialInventoryOperations_SerialReservations_SerialReservationId",
                        column: x => x.SerialReservationId,
                        principalTable: "SerialReservations",
                        principalColumn: "SerialReservationId");
                });

            migrationBuilder.Sql(
                @"
DECLARE @now datetime2 = SYSUTCDATETIME();

;WITH LegacyAssignments AS
(
    SELECT
        a.PickTaskSerialAssignmentId,
        a.PickTaskId,
        a.VoucherId,
        a.VoucherDetailId,
        a.SerialNumberId,
        a.ScannedBy,
        a.ScannedAt,
        s.WarehouseId,
        s.ItemId,
        COALESCE(s.LocationId, pt.SourceLocationId) AS LocationId,
        s.LicensePlateId,
        s.LotNumber,
        CAST(s.ExpiryDate AS date) AS ExpiryDate,
        s.HoldStatus,
        sr.StockReservationId,
        ROW_NUMBER() OVER (PARTITION BY a.SerialNumberId ORDER BY a.ScannedAt DESC, a.PickTaskSerialAssignmentId DESC) AS SerialRank
    FROM PickTaskSerialAssignments a
    INNER JOIN SerialNumbers s ON s.SerialNumberId = a.SerialNumberId
    INNER JOIN PickTasks pt ON pt.PickTaskId = a.PickTaskId
    OUTER APPLY
    (
        SELECT TOP (1) r.StockReservationId
        FROM StockReservations r
        WHERE r.VoucherId = a.VoucherId
            AND (r.VoucherDetailId = a.VoucherDetailId OR (r.VoucherDetailId IS NULL AND a.VoucherDetailId IS NULL))
            AND r.ItemId = s.ItemId
            AND r.LocationId = COALESCE(s.LocationId, pt.SourceLocationId)
            AND ISNULL(r.LotNumber, '') = ISNULL(s.LotNumber, '')
            AND ((r.ExpiryDate IS NULL AND s.ExpiryDate IS NULL) OR r.ExpiryDate = CAST(s.ExpiryDate AS date))
        ORDER BY CASE WHEN r.Status = 1 THEN 0 ELSE 1 END, r.StockReservationId
    ) sr
    WHERE a.VoidedAt IS NULL
        AND a.PostedAt IS NULL
        AND a.SerialReservationId IS NULL
)
INSERT INTO SerialReservations
(
    SerialNumberId,
    StockReservationId,
    PickTaskId,
    VoucherId,
    VoucherDetailId,
    WarehouseId,
    ItemId,
    LocationId,
    LicensePlateId,
    LotNumber,
    ExpiryDate,
    HoldStatus,
    Status,
    IdempotencyKey,
    ReservedBy,
    ReservedAt,
    PickedBy,
    PickedAt,
    VoidedBy,
    VoidedAt,
    Notes,
    CreatedAt,
    UpdatedAt
)
SELECT
    la.SerialNumberId,
    la.StockReservationId,
    la.PickTaskId,
    la.VoucherId,
    la.VoucherDetailId,
    la.WarehouseId,
    la.ItemId,
    la.LocationId,
    la.LicensePlateId,
    la.LotNumber,
    la.ExpiryDate,
    la.HoldStatus,
    CASE WHEN la.SerialRank = 1 THEN CAST(2 AS tinyint) ELSE CAST(5 AS tinyint) END,
    CONCAT('legacy-assignment:', la.PickTaskSerialAssignmentId, ':', la.SerialNumberId),
    COALESCE(NULLIF(la.ScannedBy, ''), 'migration'),
    COALESCE(la.ScannedAt, @now),
    COALESCE(NULLIF(la.ScannedBy, ''), 'migration'),
    COALESCE(la.ScannedAt, @now),
    CASE WHEN la.SerialRank = 1 THEN NULL ELSE 'migration' END,
    CASE WHEN la.SerialRank = 1 THEN NULL ELSE @now END,
    CASE
        WHEN la.SerialRank = 1 THEN 'Backfilled from legacy PickTaskSerialAssignment during Epic 3 migration.'
        ELSE 'Backfilled as voided because duplicate open legacy assignments existed for the same serial.'
    END,
    COALESCE(la.ScannedAt, @now),
    @now
FROM LegacyAssignments la
WHERE NOT EXISTS
(
    SELECT 1
    FROM SerialReservations existing
    WHERE existing.IdempotencyKey = CONCAT('legacy-assignment:', la.PickTaskSerialAssignmentId, ':', la.SerialNumberId)
);

UPDATE a
SET SerialReservationId = r.SerialReservationId
FROM PickTaskSerialAssignments a
INNER JOIN SerialReservations r
    ON r.IdempotencyKey = CONCAT('legacy-assignment:', a.PickTaskSerialAssignmentId, ':', a.SerialNumberId)
WHERE a.VoidedAt IS NULL
    AND a.PostedAt IS NULL
    AND a.SerialReservationId IS NULL;

UPDATE s
SET Status = 5,
    UpdatedAt = @now
FROM SerialNumbers s
INNER JOIN PickTaskSerialAssignments a ON a.SerialNumberId = s.SerialNumberId
INNER JOIN SerialReservations r ON r.SerialReservationId = a.SerialReservationId
WHERE a.VoidedAt IS NULL
    AND a.PostedAt IS NULL
    AND r.Status IN (1, 2)
    AND s.Status = 1;
");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4639));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4752));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4756));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4759));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4761));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4766));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4768));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4770));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4772));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4776));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4779));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4781));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4782));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4784));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4786));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4808));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4811));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4822));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4824));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4826));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4828));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(4830));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5613));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5697));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5701));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5704));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5706));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 6, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5710));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 7, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5712));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 8, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5715));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 9, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5743));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 10, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5747));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 11, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5749));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5752));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5754));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5757));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5759));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5761));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5764));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 18, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5767));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 19, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5770));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 20, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5771));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5774));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5776));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5791));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5795));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5797));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5800));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5804));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5806));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5809));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5836));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5839));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5842));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5845));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5849));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5852));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5962));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5965));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 4 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 12, 55, 3, 400, DateTimeKind.Unspecified).AddTicks(5971));

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_LicensePlate_Status",
                table: "SerialNumbers",
                columns: new[] { "LicensePlateId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_Warehouse_Item_Location_Status_Hold",
                table: "SerialNumbers",
                columns: new[] { "WarehouseId", "ItemId", "LocationId", "Status", "HoldStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_PickTaskSerialAssignments_SerialReservationId",
                table: "PickTaskSerialAssignments",
                column: "SerialReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialInventoryOperations_IdempotencyKey",
                table: "SerialInventoryOperations",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SerialInventoryOperations_Operation_Reference",
                table: "SerialInventoryOperations",
                columns: new[] { "OperationType", "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_SerialInventoryOperations_SerialNumberId",
                table: "SerialInventoryOperations",
                column: "SerialNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialInventoryOperations_SerialReservationId",
                table: "SerialInventoryOperations",
                column: "SerialReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialReservations_IdempotencyKey",
                table: "SerialReservations",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SerialReservations_ItemId",
                table: "SerialReservations",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialReservations_LicensePlateId",
                table: "SerialReservations",
                column: "LicensePlateId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialReservations_LocationId",
                table: "SerialReservations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialReservations_PickTask_Status",
                table: "SerialReservations",
                columns: new[] { "PickTaskId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SerialReservations_StockReservation_Status",
                table: "SerialReservations",
                columns: new[] { "StockReservationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SerialReservations_Voucher_Status",
                table: "SerialReservations",
                columns: new[] { "VoucherId", "VoucherDetailId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SerialReservations_VoucherDetailId",
                table: "SerialReservations",
                column: "VoucherDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialReservations_Warehouse_Item_Location_Status",
                table: "SerialReservations",
                columns: new[] { "WarehouseId", "ItemId", "LocationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_SerialReservations_ActiveSerial",
                table: "SerialReservations",
                column: "SerialNumberId",
                unique: true,
                filter: "[Status] IN (1, 2)");

            migrationBuilder.AddForeignKey(
                name: "FK_PickTaskSerialAssignments_SerialReservations_SerialReservationId",
                table: "PickTaskSerialAssignments",
                column: "SerialReservationId",
                principalTable: "SerialReservations",
                principalColumn: "SerialReservationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PickTaskSerialAssignments_SerialReservations_SerialReservationId",
                table: "PickTaskSerialAssignments");

            migrationBuilder.DropTable(
                name: "SerialInventoryOperations");

            migrationBuilder.DropTable(
                name: "SerialReservations");

            migrationBuilder.DropIndex(
                name: "IX_SerialNumbers_LicensePlate_Status",
                table: "SerialNumbers");

            migrationBuilder.DropIndex(
                name: "IX_SerialNumbers_Warehouse_Item_Location_Status_Hold",
                table: "SerialNumbers");

            migrationBuilder.DropIndex(
                name: "IX_PickTaskSerialAssignments_SerialReservationId",
                table: "PickTaskSerialAssignments");

            migrationBuilder.DropColumn(
                name: "HoldStatus",
                table: "SerialNumbers");

            migrationBuilder.DropColumn(
                name: "SerialReservationId",
                table: "PickTaskSerialAssignments");

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
                name: "IX_SerialNumbers_LicensePlateId",
                table: "SerialNumbers",
                column: "LicensePlateId");
        }
    }
}
