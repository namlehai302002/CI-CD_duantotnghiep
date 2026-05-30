using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddQualityInspectionUniqueDetailIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QualityInspections_VoucherDetailId",
                table: "QualityInspections");

            migrationBuilder.Sql(@"
WITH DuplicateRows AS (
    SELECT
        QualityInspectionId,
        ROW_NUMBER() OVER (
            PARTITION BY VoucherDetailId
            ORDER BY
                ISNULL(InspectedAt, CreatedAt) DESC,
                QualityInspectionId DESC
        ) AS rn
    FROM QualityInspections
    WHERE VoucherDetailId IS NOT NULL
)
DELETE FROM QualityInspections
WHERE QualityInspectionId IN (
    SELECT QualityInspectionId
    FROM DuplicateRows
    WHERE rn > 1
);");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_VoucherDetailId",
                table: "QualityInspections",
                column: "VoucherDetailId",
                unique: true,
                filter: "[VoucherDetailId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QualityInspections_VoucherDetailId",
                table: "QualityInspections");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_VoucherDetailId",
                table: "QualityInspections",
                column: "VoucherDetailId");
        }
    }
}
