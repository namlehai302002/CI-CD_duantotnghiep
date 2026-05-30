using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class CompleteYard3PlLaborEnterprise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LaborStandards_TaskType_WarehouseId",
                table: "LaborStandards");

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "LaborStandards",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "LaborStandards",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemClass",
                table: "LaborStandards",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "LaborStandards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ZoneId",
                table: "LaborStandards",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DockAppointments",
                columns: table => new
                {
                    DockAppointmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppointmentCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: true),
                    VoucherId = table.Column<long>(type: "bigint", nullable: true),
                    ShipmentLoadId = table.Column<long>(type: "bigint", nullable: true),
                    Direction = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    DockDoor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PlannedStartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlannedEndAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckInAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DockStartAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DockEndAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckOutAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GoodsType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    IsHazmat = table.Column<bool>(type: "bit", nullable: false),
                    IsRefrigerated = table.Column<bool>(type: "bit", nullable: false),
                    IsUrgent = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    PalletCount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CartonCount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    VolumeCbm = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SuggestedScore = table.Column<int>(type: "int", nullable: false),
                    HasConflictWarning = table.Column<bool>(type: "bit", nullable: false),
                    OverloadWarning = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DockAppointments", x => x.DockAppointmentId);
                    table.ForeignKey(
                        name: "FK_DockAppointments_Partners_OwnerPartnerId",
                        column: x => x.OwnerPartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                    table.ForeignKey(
                        name: "FK_DockAppointments_ShipmentLoads_ShipmentLoadId",
                        column: x => x.ShipmentLoadId,
                        principalTable: "ShipmentLoads",
                        principalColumn: "ShipmentLoadId");
                    table.ForeignKey(
                        name: "FK_DockAppointments_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                    table.ForeignKey(
                        name: "FK_DockAppointments_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "LaborActivities",
                columns: table => new
                {
                    LaborActivityId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ZoneId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShiftCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TaskType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TaskSourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TaskSourceId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    TaskSourceCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: true),
                    ItemClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WorkQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitOfWork = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    ExpectedMinutes = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ActualMinutes = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ProductivityPercent = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    WaitingMinutes = table.Column<int>(type: "int", nullable: false),
                    BacklogAtStart = table.Column<int>(type: "int", nullable: false),
                    IsException = table.Column<bool>(type: "bit", nullable: false),
                    ExceptionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaborActivities", x => x.LaborActivityId);
                    table.ForeignKey(
                        name: "FK_LaborActivities_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_LaborActivities_Partners_OwnerPartnerId",
                        column: x => x.OwnerPartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                    table.ForeignKey(
                        name: "FK_LaborActivities_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                    table.ForeignKey(
                        name: "FK_LaborActivities_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Zones",
                        principalColumn: "ZoneId");
                });

            migrationBuilder.CreateTable(
                name: "ThreePlContracts",
                columns: table => new
                {
                    ThreePlContractId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ContractName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "date", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MinimumCharge = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TaxPercent = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    RequiresAdjustmentApproval = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreePlContracts", x => x.ThreePlContractId);
                    table.ForeignKey(
                        name: "FK_ThreePlContracts_Partners_OwnerPartnerId",
                        column: x => x.OwnerPartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                    table.ForeignKey(
                        name: "FK_ThreePlContracts_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "YardVisitEvidence",
                columns: table => new
                {
                    YardVisitEvidenceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    YardVisitId = table.Column<long>(type: "bigint", nullable: false),
                    EvidenceType = table.Column<byte>(type: "tinyint", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    FileHashSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SealNumberSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContainerNumberSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DriverNameSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VehicleNumberSnapshot = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CapturedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YardVisitEvidence", x => x.YardVisitEvidenceId);
                    table.ForeignKey(
                        name: "FK_YardVisitEvidence_YardVisits_YardVisitId",
                        column: x => x.YardVisitId,
                        principalTable: "YardVisits",
                        principalColumn: "YardVisitId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LaborExceptionReviews",
                columns: table => new
                {
                    LaborExceptionReviewId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LaborActivityId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ProductivityBefore = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    ProductivityAfter = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    IncentiveAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaborExceptionReviews", x => x.LaborExceptionReviewId);
                    table.ForeignKey(
                        name: "FK_LaborExceptionReviews_LaborActivities_LaborActivityId",
                        column: x => x.LaborActivityId,
                        principalTable: "LaborActivities",
                        principalColumn: "LaborActivityId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThreePlContractRates",
                columns: table => new
                {
                    ThreePlContractRateId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThreePlContractId = table.Column<long>(type: "bigint", nullable: false),
                    ChargeType = table.Column<byte>(type: "tinyint", nullable: false),
                    RateCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ServiceCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ChargeUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UnitRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TierFromQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TierToQty = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    IncludedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    MinimumCharge = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SurchargePercent = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    OffHoursSurcharge = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UrgentSurcharge = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    HazmatSurcharge = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ColdStorageSurcharge = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ManualHandlingSurcharge = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SlaPenaltyPercent = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    SlaBonusPercent = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreePlContractRates", x => x.ThreePlContractRateId);
                    table.ForeignKey(
                        name: "FK_ThreePlContractRates_ThreePlContracts_ThreePlContractId",
                        column: x => x.ThreePlContractId,
                        principalTable: "ThreePlContracts",
                        principalColumn: "ThreePlContractId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThreePlInvoices",
                columns: table => new
                {
                    ThreePlInvoiceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ThreePlBillingRunId = table.Column<long>(type: "bigint", nullable: true),
                    ThreePlContractId = table.Column<long>(type: "bigint", nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: true),
                    PeriodFrom = table.Column<DateTime>(type: "date", nullable: false),
                    PeriodTo = table.Column<DateTime>(type: "date", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SubtotalAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AdjustmentAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ApiPublicId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreePlInvoices", x => x.ThreePlInvoiceId);
                    table.ForeignKey(
                        name: "FK_ThreePlInvoices_Partners_OwnerPartnerId",
                        column: x => x.OwnerPartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                    table.ForeignKey(
                        name: "FK_ThreePlInvoices_ThreePlBillingRuns_ThreePlBillingRunId",
                        column: x => x.ThreePlBillingRunId,
                        principalTable: "ThreePlBillingRuns",
                        principalColumn: "ThreePlBillingRunId");
                    table.ForeignKey(
                        name: "FK_ThreePlInvoices_ThreePlContracts_ThreePlContractId",
                        column: x => x.ThreePlContractId,
                        principalTable: "ThreePlContracts",
                        principalColumn: "ThreePlContractId");
                    table.ForeignKey(
                        name: "FK_ThreePlInvoices_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "ThreePlInvoiceLines",
                columns: table => new
                {
                    ThreePlInvoiceLineId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThreePlInvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    ThreePlBillingChargeId = table.Column<long>(type: "bigint", nullable: true),
                    ChargeType = table.Column<byte>(type: "tinyint", nullable: false),
                    LineType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ChargeUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UnitRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SubtotalAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AdjustmentAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AdjustmentReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreePlInvoiceLines", x => x.ThreePlInvoiceLineId);
                    table.ForeignKey(
                        name: "FK_ThreePlInvoiceLines_ThreePlBillingCharges_ThreePlBillingChargeId",
                        column: x => x.ThreePlBillingChargeId,
                        principalTable: "ThreePlBillingCharges",
                        principalColumn: "ThreePlBillingChargeId");
                    table.ForeignKey(
                        name: "FK_ThreePlInvoiceLines_ThreePlInvoices_ThreePlInvoiceId",
                        column: x => x.ThreePlInvoiceId,
                        principalTable: "ThreePlInvoices",
                        principalColumn: "ThreePlInvoiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThreePlDisputes",
                columns: table => new
                {
                    ThreePlDisputeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThreePlInvoiceLineId = table.Column<long>(type: "bigint", nullable: false),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    StaffResponse = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OpenedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ManagerApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ManagerApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreePlDisputes", x => x.ThreePlDisputeId);
                    table.ForeignKey(
                        name: "FK_ThreePlDisputes_Partners_OwnerPartnerId",
                        column: x => x.OwnerPartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                    table.ForeignKey(
                        name: "FK_ThreePlDisputes_ThreePlInvoiceLines_ThreePlInvoiceLineId",
                        column: x => x.ThreePlInvoiceLineId,
                        principalTable: "ThreePlInvoiceLines",
                        principalColumn: "ThreePlInvoiceLineId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LaborStandards_Match",
                table: "LaborStandards",
                columns: new[] { "TaskType", "WarehouseId", "ZoneId", "ItemClass", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_LaborStandards_ZoneId",
                table: "LaborStandards",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_DockAppointments_Door_Window_Status",
                table: "DockAppointments",
                columns: new[] { "WarehouseId", "DockDoor", "PlannedStartAt", "PlannedEndAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DockAppointments_Owner_Warehouse_Start",
                table: "DockAppointments",
                columns: new[] { "OwnerPartnerId", "WarehouseId", "PlannedStartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DockAppointments_ShipmentLoadId",
                table: "DockAppointments",
                column: "ShipmentLoadId");

            migrationBuilder.CreateIndex(
                name: "IX_DockAppointments_VoucherId",
                table: "DockAppointments",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "UX_DockAppointments_Code",
                table: "DockAppointments",
                column: "AppointmentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LaborActivities_OwnerPartnerId",
                table: "LaborActivities",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_LaborActivities_User_Shift_Start",
                table: "LaborActivities",
                columns: new[] { "UserName", "ShiftCode", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LaborActivities_UserId",
                table: "LaborActivities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LaborActivities_Warehouse_Zone_Task_Status",
                table: "LaborActivities",
                columns: new[] { "WarehouseId", "ZoneId", "TaskType", "Status", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LaborActivities_ZoneId",
                table: "LaborActivities",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "UX_LaborActivities_Code",
                table: "LaborActivities",
                column: "ActivityCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_LaborActivities_Source",
                table: "LaborActivities",
                columns: new[] { "TaskSourceType", "TaskSourceId" },
                unique: true,
                filter: "[TaskSourceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LaborExceptionReviews_Activity_Status",
                table: "LaborExceptionReviews",
                columns: new[] { "LaborActivityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ThreePlContractRates_Match",
                table: "ThreePlContractRates",
                columns: new[] { "ThreePlContractId", "ChargeType", "ChargeUnit", "TierFromQty", "EffectiveFrom", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ThreePlContracts_OwnerPartnerId",
                table: "ThreePlContracts",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ThreePlContracts_Scope_Effective",
                table: "ThreePlContracts",
                columns: new[] { "WarehouseId", "OwnerPartnerId", "Status", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "UX_ThreePlContracts_Code",
                table: "ThreePlContracts",
                column: "ContractCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThreePlDisputes_Owner_Status_Date",
                table: "ThreePlDisputes",
                columns: new[] { "OwnerPartnerId", "Status", "OpenedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ThreePlDisputes_ThreePlInvoiceLineId",
                table: "ThreePlDisputes",
                column: "ThreePlInvoiceLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ThreePlInvoiceLines_Invoice_Type",
                table: "ThreePlInvoiceLines",
                columns: new[] { "ThreePlInvoiceId", "ChargeType" });

            migrationBuilder.CreateIndex(
                name: "IX_ThreePlInvoiceLines_ThreePlBillingChargeId",
                table: "ThreePlInvoiceLines",
                column: "ThreePlBillingChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_ThreePlInvoices_OwnerPartnerId",
                table: "ThreePlInvoices",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ThreePlInvoices_Scope_Period_Status",
                table: "ThreePlInvoices",
                columns: new[] { "WarehouseId", "OwnerPartnerId", "PeriodFrom", "PeriodTo", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ThreePlInvoices_ThreePlBillingRunId",
                table: "ThreePlInvoices",
                column: "ThreePlBillingRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ThreePlInvoices_ThreePlContractId",
                table: "ThreePlInvoices",
                column: "ThreePlContractId");

            migrationBuilder.CreateIndex(
                name: "UX_ThreePlInvoices_ApiPublicId",
                table: "ThreePlInvoices",
                column: "ApiPublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ThreePlInvoices_Code",
                table: "ThreePlInvoices",
                column: "InvoiceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YardVisitEvidence_FileHash",
                table: "YardVisitEvidence",
                column: "FileHashSha256",
                filter: "[FileHashSha256] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_YardVisitEvidence_Visit_Type_Date",
                table: "YardVisitEvidence",
                columns: new[] { "YardVisitId", "EvidenceType", "CapturedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_LaborStandards_Zones_ZoneId",
                table: "LaborStandards",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "ZoneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LaborStandards_Zones_ZoneId",
                table: "LaborStandards");

            migrationBuilder.DropTable(
                name: "DockAppointments");

            migrationBuilder.DropTable(
                name: "LaborExceptionReviews");

            migrationBuilder.DropTable(
                name: "ThreePlContractRates");

            migrationBuilder.DropTable(
                name: "ThreePlDisputes");

            migrationBuilder.DropTable(
                name: "YardVisitEvidence");

            migrationBuilder.DropTable(
                name: "LaborActivities");

            migrationBuilder.DropTable(
                name: "ThreePlInvoiceLines");

            migrationBuilder.DropTable(
                name: "ThreePlInvoices");

            migrationBuilder.DropTable(
                name: "ThreePlContracts");

            migrationBuilder.DropIndex(
                name: "IX_LaborStandards_Match",
                table: "LaborStandards");

            migrationBuilder.DropIndex(
                name: "IX_LaborStandards_ZoneId",
                table: "LaborStandards");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "LaborStandards");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "LaborStandards");

            migrationBuilder.DropColumn(
                name: "ItemClass",
                table: "LaborStandards");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "LaborStandards");

            migrationBuilder.DropColumn(
                name: "ZoneId",
                table: "LaborStandards");

            migrationBuilder.CreateIndex(
                name: "IX_LaborStandards_TaskType_WarehouseId",
                table: "LaborStandards",
                columns: new[] { "TaskType", "WarehouseId" },
                unique: true,
                filter: "[WarehouseId] IS NOT NULL");
        }
    }
}
