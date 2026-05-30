using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class EnterpriseModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "HoldStatus",
                table: "ItemLocations",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.CreateTable(
                name: "CurrencyRates",
                columns: table => new
                {
                    CurrencyRateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ToCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyRates", x => x.CurrencyRateId);
                });

            migrationBuilder.CreateTable(
                name: "LaborStandards",
                columns: table => new
                {
                    LaborStandardId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TaskTypeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UnitOfWork = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExpectedMinutesPerUnit = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    ExpectedUnitsPerHour = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    MinPerformancePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ExcellentPerformancePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaborStandards", x => x.LaborStandardId);
                    table.ForeignKey(
                        name: "FK_LaborStandards_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "QualityInspections",
                columns: table => new
                {
                    QualityInspectionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
                    VoucherDetailId = table.Column<long>(type: "bigint", nullable: true),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    TotalQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SampleQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PassedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    FailedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SamplePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Disposition = table.Column<byte>(type: "tinyint", nullable: false),
                    OverallResult = table.Column<byte>(type: "tinyint", nullable: false),
                    InspectorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InspectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DefectDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LotNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityInspections", x => x.QualityInspectionId);
                    table.ForeignKey(
                        name: "FK_QualityInspections_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId");
                    table.ForeignKey(
                        name: "FK_QualityInspections_VoucherDetails_VoucherDetailId",
                        column: x => x.VoucherDetailId,
                        principalTable: "VoucherDetails",
                        principalColumn: "VoucherDetailId");
                    table.ForeignKey(
                        name: "FK_QualityInspections_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                    table.ForeignKey(
                        name: "FK_QualityInspections_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyRates_FromCurrency_ToCurrency_EffectiveDate",
                table: "CurrencyRates",
                columns: new[] { "FromCurrency", "ToCurrency", "EffectiveDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LaborStandards_TaskType_WarehouseId",
                table: "LaborStandards",
                columns: new[] { "TaskType", "WarehouseId" },
                unique: true,
                filter: "[WarehouseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LaborStandards_WarehouseId",
                table: "LaborStandards",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_ItemId",
                table: "QualityInspections",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_VoucherDetailId",
                table: "QualityInspections",
                column: "VoucherDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_VoucherId_ItemId",
                table: "QualityInspections",
                columns: new[] { "VoucherId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_WarehouseId",
                table: "QualityInspections",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurrencyRates");

            migrationBuilder.DropTable(
                name: "LaborStandards");

            migrationBuilder.DropTable(
                name: "QualityInspections");

            migrationBuilder.DropColumn(
                name: "HoldStatus",
                table: "ItemLocations");
        }
    }
}
