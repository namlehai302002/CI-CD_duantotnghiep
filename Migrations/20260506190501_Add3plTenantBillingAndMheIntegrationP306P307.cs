using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class Add3plTenantBillingAndMheIntegrationP306P307 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_ItemLocations_Item_Location_Expiry_Hold'
                      AND object_id = OBJECT_ID(N'[dbo].[ItemLocations]')
                )
                BEGIN
                    DROP INDEX [IX_ItemLocations_Item_Location_Expiry_Hold] ON [dbo].[ItemLocations];
                END;

                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_ItemLocations_Item_Location_Hold_NoBatch'
                      AND object_id = OBJECT_ID(N'[dbo].[ItemLocations]')
                )
                BEGIN
                    DROP INDEX [IX_ItemLocations_Item_Location_Hold_NoBatch] ON [dbo].[ItemLocations];
                END;

                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_ItemLocations_Item_Location_Lot_Expiry_Hold'
                      AND object_id = OBJECT_ID(N'[dbo].[ItemLocations]')
                )
                BEGIN
                    DROP INDEX [IX_ItemLocations_Item_Location_Lot_Expiry_Hold] ON [dbo].[ItemLocations];
                END;

                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_ItemLocations_Item_Location_Lot_Hold'
                      AND object_id = OBJECT_ID(N'[dbo].[ItemLocations]')
                )
                BEGIN
                    DROP INDEX [IX_ItemLocations_Item_Location_Lot_Hold] ON [dbo].[ItemLocations];
                END;
                """);

            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "YardVisits",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "YardBillingCharges",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "Waves",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "Vouchers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "VoucherDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "VasWorkOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "VasMaterialLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "StockReservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "ShipmentLoads",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "SerialNumbers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "PickTasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingAccountCode",
                table: "Partners",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingCurrency",
                table: "Partners",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsThreePlClient",
                table: "Partners",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireOwnerScopeIsolation",
                table: "Partners",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "OutboundPackages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "MovementTasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "LicensePlates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "LicensePlateDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "KittingWorkOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "KittingWorkOrderLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "Items",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "ItemLocations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "InventoryTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerPartnerId",
                table: "CatchWeightEntries",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppUserOwnerScopes",
                columns: table => new
                {
                    AppUserOwnerScopeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserOwnerScopes", x => x.AppUserOwnerScopeId);
                    table.ForeignKey(
                        name: "FK_AppUserOwnerScopes_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppUserOwnerScopes_Partners_OwnerPartnerId",
                        column: x => x.OwnerPartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                });

            migrationBuilder.CreateTable(
                name: "MheSystems",
                columns: table => new
                {
                    MheSystemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    SystemCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SystemName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SystemType = table.Column<byte>(type: "tinyint", nullable: false),
                    EndpointUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ApiKeyReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MheSystems", x => x.MheSystemId);
                    table.ForeignKey(
                        name: "FK_MheSystems_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThreePlBillingRates",
                columns: table => new
                {
                    ThreePlBillingRateId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: false),
                    ChargeType = table.Column<byte>(type: "tinyint", nullable: false),
                    RateCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    UnitRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ChargeUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreePlBillingRates", x => x.ThreePlBillingRateId);
                    table.ForeignKey(
                        name: "FK_ThreePlBillingRates_Partners_OwnerPartnerId",
                        column: x => x.OwnerPartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                    table.ForeignKey(
                        name: "FK_ThreePlBillingRates_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "ThreePlBillingRuns",
                columns: table => new
                {
                    ThreePlBillingRunId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: false),
                    PeriodFrom = table.Column<DateTime>(type: "date", nullable: false),
                    PeriodTo = table.Column<DateTime>(type: "date", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreePlBillingRuns", x => x.ThreePlBillingRunId);
                    table.ForeignKey(
                        name: "FK_ThreePlBillingRuns_Partners_OwnerPartnerId",
                        column: x => x.OwnerPartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                    table.ForeignKey(
                        name: "FK_ThreePlBillingRuns_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "MheCommands",
                columns: table => new
                {
                    MheCommandId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommandCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: true),
                    MheSystemId = table.Column<int>(type: "int", nullable: true),
                    CommandType = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    PickTaskId = table.Column<long>(type: "bigint", nullable: true),
                    MovementTaskId = table.Column<long>(type: "bigint", nullable: true),
                    WaveId = table.Column<long>(type: "bigint", nullable: true),
                    OutboundPackageId = table.Column<long>(type: "bigint", nullable: true),
                    LicensePlateId = table.Column<long>(type: "bigint", nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SourceId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SourceCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastCallbackJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MheCommands", x => x.MheCommandId);
                    table.ForeignKey(
                        name: "FK_MheCommands_LicensePlates_LicensePlateId",
                        column: x => x.LicensePlateId,
                        principalTable: "LicensePlates",
                        principalColumn: "LicensePlateId");
                    table.ForeignKey(
                        name: "FK_MheCommands_MheSystems_MheSystemId",
                        column: x => x.MheSystemId,
                        principalTable: "MheSystems",
                        principalColumn: "MheSystemId");
                    table.ForeignKey(
                        name: "FK_MheCommands_MovementTasks_MovementTaskId",
                        column: x => x.MovementTaskId,
                        principalTable: "MovementTasks",
                        principalColumn: "MovementTaskId");
                    table.ForeignKey(
                        name: "FK_MheCommands_OutboundPackages_OutboundPackageId",
                        column: x => x.OutboundPackageId,
                        principalTable: "OutboundPackages",
                        principalColumn: "OutboundPackageId");
                    table.ForeignKey(
                        name: "FK_MheCommands_Partners_OwnerPartnerId",
                        column: x => x.OwnerPartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                    table.ForeignKey(
                        name: "FK_MheCommands_PickTasks_PickTaskId",
                        column: x => x.PickTaskId,
                        principalTable: "PickTasks",
                        principalColumn: "PickTaskId");
                    table.ForeignKey(
                        name: "FK_MheCommands_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MheCommands_Waves_WaveId",
                        column: x => x.WaveId,
                        principalTable: "Waves",
                        principalColumn: "WaveId");
                });

            migrationBuilder.CreateTable(
                name: "ThreePlBillingCharges",
                columns: table => new
                {
                    ThreePlBillingChargeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThreePlBillingRunId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: false),
                    ThreePlBillingRateId = table.Column<long>(type: "bigint", nullable: true),
                    ChargeType = table.Column<byte>(type: "tinyint", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SourceCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ChargeUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UnitRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreePlBillingCharges", x => x.ThreePlBillingChargeId);
                    table.ForeignKey(
                        name: "FK_ThreePlBillingCharges_Partners_OwnerPartnerId",
                        column: x => x.OwnerPartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                    table.ForeignKey(
                        name: "FK_ThreePlBillingCharges_ThreePlBillingRates_ThreePlBillingRateId",
                        column: x => x.ThreePlBillingRateId,
                        principalTable: "ThreePlBillingRates",
                        principalColumn: "ThreePlBillingRateId");
                    table.ForeignKey(
                        name: "FK_ThreePlBillingCharges_ThreePlBillingRuns_ThreePlBillingRunId",
                        column: x => x.ThreePlBillingRunId,
                        principalTable: "ThreePlBillingRuns",
                        principalColumn: "ThreePlBillingRunId");
                    table.ForeignKey(
                        name: "FK_ThreePlBillingCharges_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "MheMissionEvents",
                columns: table => new
                {
                    MheMissionEventId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MheCommandId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    ExternalMissionId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MheMissionEvents", x => x.MheMissionEventId);
                    table.ForeignKey(
                        name: "FK_MheMissionEvents_MheCommands_MheCommandId",
                        column: x => x.MheCommandId,
                        principalTable: "MheCommands",
                        principalColumn: "MheCommandId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "PermissionId", "Code", "CreatedAt", "CreatedBy", "Description", "UpdatedAt" },
                values: new object[,]
                {
                    { 23, "tenant.scope.manage", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "tenant.scope.manage", null },
                    { 24, "billing.3pl.manage", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "billing.3pl.manage", null },
                    { 25, "mhe.manage", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "mhe.manage", null }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId", "CreatedAt" },
                values: new object[,]
                {
                    { 23, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 24, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 25, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 23, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 24, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 25, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_YardVisits_OwnerPartnerId",
                table: "YardVisits",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_YardBillingCharges_OwnerPartnerId",
                table: "YardBillingCharges",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Waves_OwnerPartnerId",
                table: "Waves",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_Owner_Warehouse_Date",
                table: "Vouchers",
                columns: new[] { "OwnerPartnerId", "WarehouseId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VoucherDetails_Owner_Item",
                table: "VoucherDetails",
                columns: new[] { "OwnerPartnerId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_VasWorkOrders_OwnerPartnerId",
                table: "VasWorkOrders",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_VasMaterialLines_OwnerPartnerId",
                table: "VasMaterialLines",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_Owner_Item_Location_Status",
                table: "StockReservations",
                columns: new[] { "OwnerPartnerId", "ItemId", "LocationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentLoads_OwnerPartnerId",
                table: "ShipmentLoads",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_Owner_Warehouse_Item_Status",
                table: "SerialNumbers",
                columns: new[] { "OwnerPartnerId", "WarehouseId", "ItemId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PickTasks_Owner_Status_Due",
                table: "PickTasks",
                columns: new[] { "OwnerPartnerId", "Status", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Partners_3PL_Active",
                table: "Partners",
                columns: new[] { "IsThreePlClient", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboundPackages_Owner_Warehouse_Packed",
                table: "OutboundPackages",
                columns: new[] { "OwnerPartnerId", "WarehouseId", "PackedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MovementTasks_Owner_Status_Due",
                table: "MovementTasks",
                columns: new[] { "OwnerPartnerId", "Status", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LicensePlates_Owner_Warehouse_Status",
                table: "LicensePlates",
                columns: new[] { "OwnerPartnerId", "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LicensePlateDetails_Owner_Item_LPN",
                table: "LicensePlateDetails",
                columns: new[] { "OwnerPartnerId", "ItemId", "LicensePlateId" });

            migrationBuilder.CreateIndex(
                name: "IX_KittingWorkOrders_OwnerPartnerId",
                table: "KittingWorkOrders",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_KittingWorkOrderLines_OwnerPartnerId",
                table: "KittingWorkOrderLines",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Owner_Active",
                table: "Items",
                columns: new[] { "OwnerPartnerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocations_Item_Location_Expiry_Hold",
                table: "ItemLocations",
                columns: new[] { "OwnerPartnerId", "ItemId", "LocationId", "ExpiryDate", "HoldStatus" },
                unique: true,
                filter: "[LotNumber] IS NULL AND [ExpiryDate] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocations_Item_Location_Hold_NoBatch",
                table: "ItemLocations",
                columns: new[] { "OwnerPartnerId", "ItemId", "LocationId", "HoldStatus" },
                unique: true,
                filter: "[LotNumber] IS NULL AND [ExpiryDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocations_Item_Location_Lot_Hold",
                table: "ItemLocations",
                columns: new[] { "OwnerPartnerId", "ItemId", "LocationId", "LotNumber", "HoldStatus" },
                unique: true,
                filter: "[LotNumber] IS NOT NULL AND [ExpiryDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocations_ItemId",
                table: "ItemLocations",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocations_Owner_SnapshotKey",
                table: "ItemLocations",
                columns: new[] { "OwnerPartnerId", "ItemId", "LocationId", "LotNumber", "ExpiryDate", "HoldStatus" },
                unique: true,
                filter: "[LotNumber] IS NOT NULL AND [ExpiryDate] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_Owner_Warehouse_Date",
                table: "InventoryTransactions",
                columns: new[] { "OwnerPartnerId", "WarehouseId", "TransactionAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CatchWeightEntries_OwnerPartnerId",
                table: "CatchWeightEntries",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserOwnerScopes_OwnerPartnerId",
                table: "AppUserOwnerScopes",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserOwnerScopes_User_Owner_Active",
                table: "AppUserOwnerScopes",
                columns: new[] { "UserId", "OwnerPartnerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UX_AppUserOwnerScopes_User_Owner_Active",
                table: "AppUserOwnerScopes",
                columns: new[] { "UserId", "OwnerPartnerId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_MheCommands_LicensePlateId",
                table: "MheCommands",
                column: "LicensePlateId");

            migrationBuilder.CreateIndex(
                name: "IX_MheCommands_MheSystemId",
                table: "MheCommands",
                column: "MheSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_MheCommands_MovementTaskId",
                table: "MheCommands",
                column: "MovementTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_MheCommands_OutboundPackageId",
                table: "MheCommands",
                column: "OutboundPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_MheCommands_OwnerPartnerId",
                table: "MheCommands",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_MheCommands_PickTaskId",
                table: "MheCommands",
                column: "PickTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_MheCommands_Warehouse_Owner_Status_Date",
                table: "MheCommands",
                columns: new[] { "WarehouseId", "OwnerPartnerId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MheCommands_WaveId",
                table: "MheCommands",
                column: "WaveId");

            migrationBuilder.CreateIndex(
                name: "UX_MheCommands_CorrelationId",
                table: "MheCommands",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_MheCommands_IdempotencyKey",
                table: "MheCommands",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MheMissionEvents_MheCommandId",
                table: "MheMissionEvents",
                column: "MheCommandId");

            migrationBuilder.CreateIndex(
                name: "UX_MheMissionEvents_IdempotencyKey",
                table: "MheMissionEvents",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_MheSystems_Warehouse_Code",
                table: "MheSystems",
                columns: new[] { "WarehouseId", "SystemCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThreePlBillingCharges_Owner_Type_Date",
                table: "ThreePlBillingCharges",
                columns: new[] { "OwnerPartnerId", "WarehouseId", "ChargeType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ThreePlBillingCharges_ThreePlBillingRateId",
                table: "ThreePlBillingCharges",
                column: "ThreePlBillingRateId");

            migrationBuilder.CreateIndex(
                name: "IX_ThreePlBillingCharges_ThreePlBillingRunId",
                table: "ThreePlBillingCharges",
                column: "ThreePlBillingRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ThreePlBillingCharges_WarehouseId",
                table: "ThreePlBillingCharges",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "UX_ThreePlBillingCharges_IdempotencyKey",
                table: "ThreePlBillingCharges",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThreePlBillingRates_Match",
                table: "ThreePlBillingRates",
                columns: new[] { "WarehouseId", "OwnerPartnerId", "ChargeType", "IsActive", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ThreePlBillingRates_OwnerPartnerId",
                table: "ThreePlBillingRates",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ThreePlBillingRuns_Owner_Period_Status",
                table: "ThreePlBillingRuns",
                columns: new[] { "WarehouseId", "OwnerPartnerId", "PeriodFrom", "PeriodTo", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ThreePlBillingRuns_OwnerPartnerId",
                table: "ThreePlBillingRuns",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "UX_ThreePlBillingRuns_IdempotencyKey",
                table: "ThreePlBillingRuns",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CatchWeightEntries_Partners_OwnerPartnerId",
                table: "CatchWeightEntries",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Partners_OwnerPartnerId",
                table: "InventoryTransactions",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemLocations_Partners_OwnerPartnerId",
                table: "ItemLocations",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Partners_OwnerPartnerId",
                table: "Items",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_KittingWorkOrderLines_Partners_OwnerPartnerId",
                table: "KittingWorkOrderLines",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_KittingWorkOrders_Partners_OwnerPartnerId",
                table: "KittingWorkOrders",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_LicensePlateDetails_Partners_OwnerPartnerId",
                table: "LicensePlateDetails",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_LicensePlates_Partners_OwnerPartnerId",
                table: "LicensePlates",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovementTasks_Partners_OwnerPartnerId",
                table: "MovementTasks",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_OutboundPackages_Partners_OwnerPartnerId",
                table: "OutboundPackages",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_PickTasks_Partners_OwnerPartnerId",
                table: "PickTasks",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_SerialNumbers_Partners_OwnerPartnerId",
                table: "SerialNumbers",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentLoads_Partners_OwnerPartnerId",
                table: "ShipmentLoads",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockReservations_Partners_OwnerPartnerId",
                table: "StockReservations",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_VasMaterialLines_Partners_OwnerPartnerId",
                table: "VasMaterialLines",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_VasWorkOrders_Partners_OwnerPartnerId",
                table: "VasWorkOrders",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_VoucherDetails_Partners_OwnerPartnerId",
                table: "VoucherDetails",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_Partners_OwnerPartnerId",
                table: "Vouchers",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Waves_Partners_OwnerPartnerId",
                table: "Waves",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_YardBillingCharges_Partners_OwnerPartnerId",
                table: "YardBillingCharges",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_YardVisits_Partners_OwnerPartnerId",
                table: "YardVisits",
                column: "OwnerPartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CatchWeightEntries_Partners_OwnerPartnerId",
                table: "CatchWeightEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Partners_OwnerPartnerId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemLocations_Partners_OwnerPartnerId",
                table: "ItemLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_Partners_OwnerPartnerId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_KittingWorkOrderLines_Partners_OwnerPartnerId",
                table: "KittingWorkOrderLines");

            migrationBuilder.DropForeignKey(
                name: "FK_KittingWorkOrders_Partners_OwnerPartnerId",
                table: "KittingWorkOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_LicensePlateDetails_Partners_OwnerPartnerId",
                table: "LicensePlateDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_LicensePlates_Partners_OwnerPartnerId",
                table: "LicensePlates");

            migrationBuilder.DropForeignKey(
                name: "FK_MovementTasks_Partners_OwnerPartnerId",
                table: "MovementTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_OutboundPackages_Partners_OwnerPartnerId",
                table: "OutboundPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_PickTasks_Partners_OwnerPartnerId",
                table: "PickTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_SerialNumbers_Partners_OwnerPartnerId",
                table: "SerialNumbers");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentLoads_Partners_OwnerPartnerId",
                table: "ShipmentLoads");

            migrationBuilder.DropForeignKey(
                name: "FK_StockReservations_Partners_OwnerPartnerId",
                table: "StockReservations");

            migrationBuilder.DropForeignKey(
                name: "FK_VasMaterialLines_Partners_OwnerPartnerId",
                table: "VasMaterialLines");

            migrationBuilder.DropForeignKey(
                name: "FK_VasWorkOrders_Partners_OwnerPartnerId",
                table: "VasWorkOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_VoucherDetails_Partners_OwnerPartnerId",
                table: "VoucherDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_Partners_OwnerPartnerId",
                table: "Vouchers");

            migrationBuilder.DropForeignKey(
                name: "FK_Waves_Partners_OwnerPartnerId",
                table: "Waves");

            migrationBuilder.DropForeignKey(
                name: "FK_YardBillingCharges_Partners_OwnerPartnerId",
                table: "YardBillingCharges");

            migrationBuilder.DropForeignKey(
                name: "FK_YardVisits_Partners_OwnerPartnerId",
                table: "YardVisits");

            migrationBuilder.DropTable(
                name: "AppUserOwnerScopes");

            migrationBuilder.DropTable(
                name: "MheMissionEvents");

            migrationBuilder.DropTable(
                name: "ThreePlBillingCharges");

            migrationBuilder.DropTable(
                name: "MheCommands");

            migrationBuilder.DropTable(
                name: "ThreePlBillingRates");

            migrationBuilder.DropTable(
                name: "ThreePlBillingRuns");

            migrationBuilder.DropTable(
                name: "MheSystems");

            migrationBuilder.DropIndex(
                name: "IX_YardVisits_OwnerPartnerId",
                table: "YardVisits");

            migrationBuilder.DropIndex(
                name: "IX_YardBillingCharges_OwnerPartnerId",
                table: "YardBillingCharges");

            migrationBuilder.DropIndex(
                name: "IX_Waves_OwnerPartnerId",
                table: "Waves");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_Owner_Warehouse_Date",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_VoucherDetails_Owner_Item",
                table: "VoucherDetails");

            migrationBuilder.DropIndex(
                name: "IX_VasWorkOrders_OwnerPartnerId",
                table: "VasWorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_VasMaterialLines_OwnerPartnerId",
                table: "VasMaterialLines");

            migrationBuilder.DropIndex(
                name: "IX_StockReservations_Owner_Item_Location_Status",
                table: "StockReservations");

            migrationBuilder.DropIndex(
                name: "IX_ShipmentLoads_OwnerPartnerId",
                table: "ShipmentLoads");

            migrationBuilder.DropIndex(
                name: "IX_SerialNumbers_Owner_Warehouse_Item_Status",
                table: "SerialNumbers");

            migrationBuilder.DropIndex(
                name: "IX_PickTasks_Owner_Status_Due",
                table: "PickTasks");

            migrationBuilder.DropIndex(
                name: "IX_Partners_3PL_Active",
                table: "Partners");

            migrationBuilder.DropIndex(
                name: "IX_OutboundPackages_Owner_Warehouse_Packed",
                table: "OutboundPackages");

            migrationBuilder.DropIndex(
                name: "IX_MovementTasks_Owner_Status_Due",
                table: "MovementTasks");

            migrationBuilder.DropIndex(
                name: "IX_LicensePlates_Owner_Warehouse_Status",
                table: "LicensePlates");

            migrationBuilder.DropIndex(
                name: "IX_LicensePlateDetails_Owner_Item_LPN",
                table: "LicensePlateDetails");

            migrationBuilder.DropIndex(
                name: "IX_KittingWorkOrders_OwnerPartnerId",
                table: "KittingWorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_KittingWorkOrderLines_OwnerPartnerId",
                table: "KittingWorkOrderLines");

            migrationBuilder.DropIndex(
                name: "IX_Items_Owner_Active",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_ItemLocations_Item_Location_Expiry_Hold",
                table: "ItemLocations");

            migrationBuilder.DropIndex(
                name: "IX_ItemLocations_Item_Location_Hold_NoBatch",
                table: "ItemLocations");

            migrationBuilder.DropIndex(
                name: "IX_ItemLocations_Item_Location_Lot_Hold",
                table: "ItemLocations");

            migrationBuilder.DropIndex(
                name: "IX_ItemLocations_ItemId",
                table: "ItemLocations");

            migrationBuilder.DropIndex(
                name: "IX_ItemLocations_Owner_SnapshotKey",
                table: "ItemLocations");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_Owner_Warehouse_Date",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CatchWeightEntries_OwnerPartnerId",
                table: "CatchWeightEntries");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 23, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 24, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 25, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 23, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 24, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 25, 2 });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 25);

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "YardVisits");

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "YardBillingCharges");

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "Waves");

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "VoucherDetails");

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "VasWorkOrders");

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "VasMaterialLines");

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "StockReservations");

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "ShipmentLoads");

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "SerialNumbers");

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "PickTasks");

            migrationBuilder.DropColumn(
                name: "BillingAccountCode",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "BillingCurrency",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "IsThreePlClient",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "RequireOwnerScopeIsolation",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "OutboundPackages");

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "MovementTasks");

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "LicensePlates");

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "LicensePlateDetails");

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "KittingWorkOrders");

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "KittingWorkOrderLines");

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "ItemLocations");

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "OwnerPartnerId",
                table: "CatchWeightEntries");

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
        }
    }
}
