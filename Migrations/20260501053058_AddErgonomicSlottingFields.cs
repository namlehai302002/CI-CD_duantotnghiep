using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddErgonomicSlottingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowMechanicalHandling",
                table: "Locations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowMixedSku",
                table: "Locations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "HeightLevel",
                table: "Locations",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "IsGoldenZone",
                table: "Locations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxWeightCapacityKg",
                table: "Locations",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightLimitKg",
                table: "Locations",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE Locations
                SET HeightLevel = CASE
                    WHEN TRY_CONVERT(int, ShelfCode) IS NOT NULL AND TRY_CONVERT(int, ShelfCode) > 0
                        THEN TRY_CONVERT(int, ShelfCode)
                    ELSE 1
                END;

                UPDATE l
                SET
                    IsGoldenZone = CASE WHEN l.HeightLevel BETWEEN 2 AND 4 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END,
                    WeightLimitKg = CASE
                        WHEN l.HeightLevel >= 5 THEN CAST(23.0000 AS decimal(18,4))
                        WHEN l.HeightLevel BETWEEN 3 AND 4 THEN CAST(50.0000 AS decimal(18,4))
                        ELSE NULL
                    END,
                    MaxWeightCapacityKg = CASE
                        WHEN l.MaxCapacity > 0 AND l.MaxCapacity < 999999 THEN l.MaxCapacity
                        ELSE CAST(2000.0000 AS decimal(18,4))
                    END
                FROM Locations l
                INNER JOIN Zones z ON z.ZoneId = l.ZoneId
                WHERE z.ZoneType = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowMechanicalHandling",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "AllowMixedSku",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "HeightLevel",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "IsGoldenZone",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "MaxWeightCapacityKg",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "WeightLimitKg",
                table: "Locations");
        }
    }
}
