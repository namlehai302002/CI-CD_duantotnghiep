using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedWmsModels_P2_P3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 6, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 7, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 8, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 9, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 10, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 11, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 11, 3 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 11, 4 });

            migrationBuilder.DropColumn(
                name: "DiffQty",
                table: "StockCountLines");

            migrationBuilder.AddColumn<string>(
                name: "CarrierCode",
                table: "Waves",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CarrierName",
                table: "Waves",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CutoffTime",
                table: "Waves",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteCode",
                table: "Waves",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WaveProfile",
                table: "Waves",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PartialShipmentAllowed",
                table: "Vouchers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Vouchers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte>(
                name: "ServiceLevel",
                table: "Vouchers",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "SlaCode",
                table: "Vouchers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlaHours",
                table: "Vouchers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StockReservations",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "SheetCode",
                table: "StockCountSheets",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CountedQty",
                table: "StockCountLines",
                type: "decimal(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CountedAt",
                table: "StockCountLines",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountedBy",
                table: "StockCountLines",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "StockCountLines",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<decimal>(
                name: "Variance",
                table: "StockCountLines",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SerialNumbers",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "PickTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CapacityScenarios",
                columns: table => new
                {
                    ScenarioId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScenarioName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ScenarioDate = table.Column<DateTime>(type: "date", nullable: false),
                    DailyVolume = table.Column<int>(type: "int", nullable: false),
                    VolumeGrowthPct = table.Column<int>(type: "int", nullable: false),
                    PeakHours = table.Column<int>(type: "int", nullable: false),
                    PeakFactor = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DockCount = table.Column<int>(type: "int", nullable: false),
                    LaborCount = table.Column<int>(type: "int", nullable: false),
                    WorkingHoursPerDay = table.Column<int>(type: "int", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bottlenecks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Recommendations = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CriticalBottleneck = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConfidenceScore = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapacityScenarios", x => x.ScenarioId);
                    table.ForeignKey(
                        name: "FK_CapacityScenarios_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "CrossDockTasks",
                columns: table => new
                {
                    CrossDockTaskId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    InboundVoucherId = table.Column<long>(type: "bigint", nullable: false),
                    OutboundVoucherId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    StageLocationId = table.Column<int>(type: "int", nullable: false),
                    ScheduledQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ActualQty = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AssignedTo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrossDockTasks", x => x.CrossDockTaskId);
                    table.ForeignKey(
                        name: "FK_CrossDockTasks_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId");
                    table.ForeignKey(
                        name: "FK_CrossDockTasks_Locations_StageLocationId",
                        column: x => x.StageLocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_CrossDockTasks_Vouchers_InboundVoucherId",
                        column: x => x.InboundVoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                    table.ForeignKey(
                        name: "FK_CrossDockTasks_Vouchers_OutboundVoucherId",
                        column: x => x.OutboundVoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                });

            migrationBuilder.CreateTable(
                name: "CycleCountPrograms",
                columns: table => new
                {
                    ProgramId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgramName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    FrequencyA = table.Column<int>(type: "int", nullable: false),
                    FrequencyB = table.Column<int>(type: "int", nullable: false),
                    FrequencyC = table.Column<int>(type: "int", nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextRunAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsBlindCount = table.Column<bool>(type: "bit", nullable: false),
                    VarianceThresholdPct = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CycleCountPrograms", x => x.ProgramId);
                    table.ForeignKey(
                        name: "FK_CycleCountPrograms_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            // DockDoorCapacities - use SQL to handle existing table
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DockDoorCapacities')
                BEGIN
                    CREATE TABLE [DockDoorCapacities] (
                        [CapacityId] int IDENTITY(1,1) NOT NULL,
                        [DockDoor] nvarchar(20) NOT NULL,
                        [WarehouseId] int NOT NULL,
                        [DayOfWeek] int NULL,
                        [SlotStartMinutes] int NOT NULL,
                        [SlotEndMinutes] int NOT NULL,
                        [MaxAppointments] int NOT NULL,
                        [AvgUnloadMinutes] int NOT NULL,
                        [DoorType] tinyint NOT NULL,
                        [IsRefrigerated] bit NOT NULL,
                        [IsHazmat] bit NOT NULL,
                        [Notes] nvarchar(100) NULL,
                        CONSTRAINT [PK_DockDoorCapacities] PRIMARY KEY ([CapacityId]),
                        CONSTRAINT [FK_DockDoorCapacities_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([WarehouseId]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_DockDoorCapacities_WarehouseId] ON [DockDoorCapacities] ([WarehouseId]);
                END");

            migrationBuilder.CreateTable(
                name: "IntegrationIdempotencyKeys",
                columns: table => new
                {
                    KeyId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KeyValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CachedResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationIdempotencyKeys", x => x.KeyId);
                });

            // IntegrationOutbox - use SQL to handle existing table
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'IntegrationOutbox')
                BEGIN
                    CREATE TABLE [IntegrationOutbox] (
                        [OutboxId] bigint IDENTITY(1,1) NOT NULL,
                        [EventType] nvarchar(50) NOT NULL,
                        [TargetEndpoint] nvarchar(200) NOT NULL,
                        [Payload] nvarchar(max) NOT NULL,
                        [HttpMethod] nvarchar(10) NOT NULL,
                        [Status] tinyint NOT NULL,
                        [RetryCount] int NOT NULL,
                        [LastError] nvarchar(500) NULL,
                        [ProcessedAt] datetime2 NULL,
                        [IdempotencyKey] nvarchar(100) NULL,
                        [TargetSystem] nvarchar(100) NULL,
                        [CorrelationId] nvarchar(45) NULL,
                        [CreatedBy] nvarchar(200) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_IntegrationOutbox] PRIMARY KEY ([OutboxId])
                    );
                    CREATE INDEX [IX_IntegrationOutbox_IdempotencyKey] ON [IntegrationOutbox] ([IdempotencyKey]) WHERE [IdempotencyKey] IS NOT NULL;
                    CREATE INDEX [IX_IntegrationOutbox_Status_Created] ON [IntegrationOutbox] ([Status], [CreatedAt]);
                END");

            migrationBuilder.CreateTable(
                name: "ItemVelocityClassifications",
                columns: table => new
                {
                    ClassificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    AbcClass = table.Column<string>(type: "nvarchar(1)", nullable: false),
                    XyzClass = table.Column<string>(type: "nvarchar(1)", nullable: false),
                    CombinedClass = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    PickCount = table.Column<int>(type: "int", nullable: false),
                    TotalPickQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DemandVariability = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    DailyPickFrequency = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CurrentLocationId = table.Column<int>(type: "int", nullable: true),
                    SuggestedLocationId = table.Column<int>(type: "int", nullable: true),
                    SuggestedPickFaceLocationId = table.Column<int>(type: "int", nullable: true),
                    SuggestedBulkLocationId = table.Column<int>(type: "int", nullable: true),
                    SlottingScore = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastAnalyzedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnalysisPeriodDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemVelocityClassifications", x => x.ClassificationId);
                    table.ForeignKey(
                        name: "FK_ItemVelocityClassifications_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemVelocityClassifications_Locations_CurrentLocationId",
                        column: x => x.CurrentLocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_ItemVelocityClassifications_Locations_SuggestedLocationId",
                        column: x => x.SuggestedLocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK_ItemVelocityClassifications_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "LaborActivityStandards",
                columns: table => new
                {
                    StandardId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StandardMinutesPerUnit = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    TravelMinutesPerLocation = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaborActivityStandards", x => x.StandardId);
                });

            migrationBuilder.CreateTable(
                name: "RecallCases",
                columns: table => new
                {
                    RecallCaseId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Severity = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    IssuedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecallCases", x => x.RecallCaseId);
                    table.ForeignKey(
                        name: "FK_RecallCases_Partners_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SlaMetrics",
                columns: table => new
                {
                    SlaMetricId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
                    SlaType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SlaCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    TargetMinutes = table.Column<int>(type: "int", nullable: false),
                    ActualMinutes = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    VarianceMinutes = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaMetrics", x => x.SlaMetricId);
                    table.ForeignKey(
                        name: "FK_SlaMetrics_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CycleCountSchedules",
                columns: table => new
                {
                    ScheduleId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgramId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    AbcClass = table.Column<string>(type: "nvarchar(1)", nullable: false),
                    CountAttempt = table.Column<int>(type: "int", nullable: false),
                    LastCountedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CumulativeVariance = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CycleCountSchedules", x => x.ScheduleId);
                    table.ForeignKey(
                        name: "FK_CycleCountSchedules_CycleCountPrograms_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "CycleCountPrograms",
                        principalColumn: "ProgramId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CycleCountSchedules_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId");
                    table.ForeignKey(
                        name: "FK_CycleCountSchedules_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                });

            migrationBuilder.CreateTable(
                name: "RecallLines",
                columns: table => new
                {
                    RecallLineId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecallCaseId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SerialNumbers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AffectedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RecoveredQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Disposition = table.Column<byte>(type: "tinyint", nullable: false),
                    LineStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecallLines", x => x.RecallLineId);
                    table.ForeignKey(
                        name: "FK_RecallLines_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId");
                    table.ForeignKey(
                        name: "FK_RecallLines_RecallCases_RecallCaseId",
                        column: x => x.RecallCaseId,
                        principalTable: "RecallCases",
                        principalColumn: "RecallCaseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                columns: new[] { "Code", "Description" },
                values: new object[] { "voucher.confirm.shipping", "voucher.confirm.shipping" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                columns: new[] { "Code", "Description" },
                values: new object[] { "voucher.approve.outbound", "voucher.approve.outbound" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8,
                columns: new[] { "Code", "Description" },
                values: new object[] { "qc.submit.inspection", "qc.submit.inspection" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 9,
                columns: new[] { "Code", "Description" },
                values: new object[] { "qc.resolve.hold", "qc.resolve.hold" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 10,
                columns: new[] { "Code", "Description" },
                values: new object[] { "stockcount.approve", "stockcount.approve" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11,
                columns: new[] { "Code", "Description" },
                values: new object[] { "stockcount.unlock", "stockcount.unlock" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12,
                columns: new[] { "Code", "Description" },
                values: new object[] { "master.item.manage", "master.item.manage" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13,
                columns: new[] { "Code", "Description" },
                values: new object[] { "master.partner.manage", "master.partner.manage" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14,
                columns: new[] { "Code", "Description" },
                values: new object[] { "master.category.manage", "master.category.manage" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15,
                columns: new[] { "Code", "Description" },
                values: new object[] { "master.uom.manage", "master.uom.manage" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 16,
                columns: new[] { "Code", "Description" },
                values: new object[] { "warehouse.config.manage", "warehouse.config.manage" });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "PermissionId", "Code", "Description" },
                values: new object[,]
                {
                    { 17, "report.view", "report.view" },
                    { 18, "audit.view", "audit.view" },
                    { 19, "user.manage", "user.manage" },
                    { 20, "system.danger.ops", "system.danger.ops" },
                    { 21, "report.view.financial", "report.view.financial" },
                    { 22, "picktask.reassign", "picktask.reassign" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 12, 2 },
                    { 13, 2 },
                    { 14, 2 },
                    { 17, 1 },
                    { 18, 1 },
                    { 19, 1 },
                    { 20, 1 },
                    { 21, 1 },
                    { 22, 1 },
                    { 17, 2 },
                    { 21, 2 },
                    { 22, 2 },
                    { 17, 3 },
                    { 17, 4 }
                });

            // Remove duplicates before creating unique index
            migrationBuilder.Sql(@"
                DELETE FROM [WarehousePeriodLocks]
                WHERE [WarehousePeriodLockId] IN (
                    SELECT [WarehousePeriodLockId] FROM (
                        SELECT [WarehousePeriodLockId], ROW_NUMBER() OVER (PARTITION BY [WarehouseId], [LockDate] ORDER BY [WarehousePeriodLockId]) AS rn
                        FROM [WarehousePeriodLocks]
                    ) dups WHERE rn > 1
                )");

            migrationBuilder.CreateIndex(
                name: "IX_WarehousePeriodLocks_Warehouse_LockDate",
                table: "WarehousePeriodLocks",
                columns: new[] { "WarehouseId", "LockDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickTaskSerialAssignments_VoucherDetailId",
                table: "PickTaskSerialAssignments",
                column: "VoucherDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_PickTaskSerialAssignments_VoucherId",
                table: "PickTaskSerialAssignments",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_LicensePlates_VoucherDetailId",
                table: "LicensePlates",
                column: "VoucherDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_CapacityScenarios_WarehouseId_ScenarioName",
                table: "CapacityScenarios",
                columns: new[] { "WarehouseId", "ScenarioName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrossDockTasks_InboundVoucherId",
                table: "CrossDockTasks",
                column: "InboundVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_CrossDockTasks_ItemId",
                table: "CrossDockTasks",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CrossDockTasks_OutboundVoucherId",
                table: "CrossDockTasks",
                column: "OutboundVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_CrossDockTasks_StageLocationId",
                table: "CrossDockTasks",
                column: "StageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_CrossDockTasks_TaskCode",
                table: "CrossDockTasks",
                column: "TaskCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CycleCountPrograms_WarehouseId_ProgramName",
                table: "CycleCountPrograms",
                columns: new[] { "WarehouseId", "ProgramName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CycleCountSchedules_ItemId",
                table: "CycleCountSchedules",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CycleCountSchedules_LocationId",
                table: "CycleCountSchedules",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_CycleCountSchedules_ProgramId_ItemId_LocationId",
                table: "CycleCountSchedules",
                columns: new[] { "ProgramId", "ItemId", "LocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationIdempotencyKeys_KeyValue",
                table: "IntegrationIdempotencyKeys",
                column: "KeyValue",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemVelocityClassifications_CurrentLocationId",
                table: "ItemVelocityClassifications",
                column: "CurrentLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemVelocityClassifications_ItemId_WarehouseId",
                table: "ItemVelocityClassifications",
                columns: new[] { "ItemId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemVelocityClassifications_SuggestedLocationId",
                table: "ItemVelocityClassifications",
                column: "SuggestedLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemVelocityClassifications_WarehouseId",
                table: "ItemVelocityClassifications",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_LaborActivityStandards_ActivityType",
                table: "LaborActivityStandards",
                column: "ActivityType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecallCases_CaseNumber",
                table: "RecallCases",
                column: "CaseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecallCases_SupplierId",
                table: "RecallCases",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_RecallLines_ItemId",
                table: "RecallLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RecallLines_RecallCaseId",
                table: "RecallLines",
                column: "RecallCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaMetrics_VoucherId_SlaType",
                table: "SlaMetrics",
                columns: new[] { "VoucherId", "SlaType" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LicensePlates_VoucherDetails_VoucherDetailId",
                table: "LicensePlates",
                column: "VoucherDetailId",
                principalTable: "VoucherDetails",
                principalColumn: "VoucherDetailId");

            migrationBuilder.AddForeignKey(
                name: "FK_PickTaskSerialAssignments_VoucherDetails_VoucherDetailId",
                table: "PickTaskSerialAssignments",
                column: "VoucherDetailId",
                principalTable: "VoucherDetails",
                principalColumn: "VoucherDetailId");

            migrationBuilder.AddForeignKey(
                name: "FK_PickTaskSerialAssignments_Vouchers_VoucherId",
                table: "PickTaskSerialAssignments",
                column: "VoucherId",
                principalTable: "Vouchers",
                principalColumn: "VoucherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LicensePlates_VoucherDetails_VoucherDetailId",
                table: "LicensePlates");

            migrationBuilder.DropForeignKey(
                name: "FK_PickTaskSerialAssignments_VoucherDetails_VoucherDetailId",
                table: "PickTaskSerialAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_PickTaskSerialAssignments_Vouchers_VoucherId",
                table: "PickTaskSerialAssignments");

            migrationBuilder.DropTable(
                name: "CapacityScenarios");

            migrationBuilder.DropTable(
                name: "CrossDockTasks");

            migrationBuilder.DropTable(
                name: "CycleCountSchedules");

            migrationBuilder.DropTable(
                name: "DockDoorCapacities");

            migrationBuilder.DropTable(
                name: "IntegrationIdempotencyKeys");

            migrationBuilder.DropTable(
                name: "IntegrationOutbox");

            migrationBuilder.DropTable(
                name: "ItemVelocityClassifications");

            migrationBuilder.DropTable(
                name: "LaborActivityStandards");

            migrationBuilder.DropTable(
                name: "RecallLines");

            migrationBuilder.DropTable(
                name: "SlaMetrics");

            migrationBuilder.DropTable(
                name: "CycleCountPrograms");

            migrationBuilder.DropTable(
                name: "RecallCases");

            migrationBuilder.DropIndex(
                name: "IX_WarehousePeriodLocks_Warehouse_LockDate",
                table: "WarehousePeriodLocks");

            migrationBuilder.DropIndex(
                name: "IX_PickTaskSerialAssignments_VoucherDetailId",
                table: "PickTaskSerialAssignments");

            migrationBuilder.DropIndex(
                name: "IX_PickTaskSerialAssignments_VoucherId",
                table: "PickTaskSerialAssignments");

            migrationBuilder.DropIndex(
                name: "IX_LicensePlates_VoucherDetailId",
                table: "LicensePlates");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 18, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 19, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 20, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 3 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 4 });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 22);

            migrationBuilder.DropColumn(
                name: "CarrierCode",
                table: "Waves");

            migrationBuilder.DropColumn(
                name: "CarrierName",
                table: "Waves");

            migrationBuilder.DropColumn(
                name: "CutoffTime",
                table: "Waves");

            migrationBuilder.DropColumn(
                name: "RouteCode",
                table: "Waves");

            migrationBuilder.DropColumn(
                name: "WaveProfile",
                table: "Waves");

            migrationBuilder.DropColumn(
                name: "PartialShipmentAllowed",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ServiceLevel",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "SlaCode",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "SlaHours",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StockReservations");

            migrationBuilder.DropColumn(
                name: "SheetCode",
                table: "StockCountSheets");

            migrationBuilder.DropColumn(
                name: "CountedAt",
                table: "StockCountLines");

            migrationBuilder.DropColumn(
                name: "CountedBy",
                table: "StockCountLines");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "StockCountLines");

            migrationBuilder.DropColumn(
                name: "Variance",
                table: "StockCountLines");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SerialNumbers");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "PickTasks");

            migrationBuilder.AlterColumn<decimal>(
                name: "CountedQty",
                table: "StockCountLines",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiffQty",
                table: "StockCountLines",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                columns: new[] { "Code", "Description" },
                values: new object[] { "master.item.manage", "master.item.manage" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                columns: new[] { "Code", "Description" },
                values: new object[] { "master.partner.manage", "master.partner.manage" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8,
                columns: new[] { "Code", "Description" },
                values: new object[] { "master.category.manage", "master.category.manage" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 9,
                columns: new[] { "Code", "Description" },
                values: new object[] { "master.uom.manage", "master.uom.manage" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 10,
                columns: new[] { "Code", "Description" },
                values: new object[] { "warehouse.config.manage", "warehouse.config.manage" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11,
                columns: new[] { "Code", "Description" },
                values: new object[] { "report.view", "report.view" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12,
                columns: new[] { "Code", "Description" },
                values: new object[] { "audit.view", "audit.view" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13,
                columns: new[] { "Code", "Description" },
                values: new object[] { "user.manage", "user.manage" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14,
                columns: new[] { "Code", "Description" },
                values: new object[] { "system.danger.ops", "system.danger.ops" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15,
                columns: new[] { "Code", "Description" },
                values: new object[] { "report.view.financial", "report.view.financial" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 16,
                columns: new[] { "Code", "Description" },
                values: new object[] { "picktask.reassign", "picktask.reassign" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 6, 2 },
                    { 7, 2 },
                    { 8, 2 },
                    { 9, 2 },
                    { 10, 2 },
                    { 11, 2 },
                    { 11, 3 },
                    { 11, 4 }
                });
        }
    }
}
