using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddUomGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UomGroup",
                table: "UnitsOfMeasure",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // migrationBuilder.AddColumn<string>(
            //     name: "LotNumber",
            //     table: "ItemLocations",
            //     type: "nvarchar(50)",
            //     maxLength: 50,
            //     nullable: true);

            // migrationBuilder.AddColumn<decimal>(
            //     name: "MaxCapacity",
            //     table: "ItemLocations",
            //     type: "decimal(18,4)",
            //     nullable: true);

            // migrationBuilder.AddColumn<decimal>(
            //     name: "TotalCapacity",
            //     table: "ItemLocations",
            //     type: "decimal(18,4)",
            //     nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UomGroup",
                table: "UnitsOfMeasure");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                table: "ItemLocations");

            migrationBuilder.DropColumn(
                name: "MaxCapacity",
                table: "ItemLocations");

            migrationBuilder.DropColumn(
                name: "TotalCapacity",
                table: "ItemLocations");
        }
    }
}
