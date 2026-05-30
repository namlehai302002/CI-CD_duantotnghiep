using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class EnterpriseAuditGapClosure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CertificateUrl",
                table: "QualityInspections",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectionPlanName",
                table: "QualityInspections",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMfaEnabled",
                table: "AppUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MfaSecretKey",
                table: "AppUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InspectionPlanTemplates",
                columns: table => new
                {
                    InspectionPlanTemplateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ItemCategoryId = table.Column<int>(type: "int", nullable: true),
                    ChecklistItems = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SampleSizeFormula = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SampleSizeValue = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    DefaultPassDisposition = table.Column<byte>(type: "tinyint", nullable: false),
                    DefaultFailDisposition = table.Column<byte>(type: "tinyint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionPlanTemplates", x => x.InspectionPlanTemplateId);
                    table.ForeignKey(
                        name: "FK_InspectionPlanTemplates_ItemCategories_ItemCategoryId",
                        column: x => x.ItemCategoryId,
                        principalTable: "ItemCategories",
                        principalColumn: "CategoryId");
                });

            migrationBuilder.CreateTable(
                name: "ScheduledReports",
                columns: table => new
                {
                    ScheduledReportId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReportType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Schedule = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RunAtHour = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: true),
                    DayOfMonth = table.Column<int>(type: "int", nullable: true),
                    Recipients = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: true),
                    OutputFormat = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastRunResult = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NextRunAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RunCount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledReports", x => x.ScheduledReportId);
                    table.ForeignKey(
                        name: "FK_ScheduledReports_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InspectionPlanTemplates_ItemCategoryId",
                table: "InspectionPlanTemplates",
                column: "ItemCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionPlanTemplates_PlanName",
                table: "InspectionPlanTemplates",
                column: "PlanName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledReports_ReportType_IsActive",
                table: "ScheduledReports",
                columns: new[] { "ReportType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledReports_WarehouseId",
                table: "ScheduledReports",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InspectionPlanTemplates");

            migrationBuilder.DropTable(
                name: "ScheduledReports");

            migrationBuilder.DropColumn(
                name: "CertificateUrl",
                table: "QualityInspections");

            migrationBuilder.DropColumn(
                name: "InspectionPlanName",
                table: "QualityInspections");

            migrationBuilder.DropColumn(
                name: "IsMfaEnabled",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "MfaSecretKey",
                table: "AppUsers");
        }
    }
}
