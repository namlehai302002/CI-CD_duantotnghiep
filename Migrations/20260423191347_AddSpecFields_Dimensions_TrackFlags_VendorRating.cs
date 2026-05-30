using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecFields_Dimensions_TrackFlags_VendorRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LeadTimeDays",
                table: "Partners",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "VendorRating",
                table: "Partners",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<decimal>(
                name: "Height",
                table: "Items",
                type: "decimal(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Length",
                table: "Items",
                type: "decimal(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TrackExpiry",
                table: "Items",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TrackLot",
                table: "Items",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Width",
                table: "Items",
                type: "decimal(8,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LeadTimeDays",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "VendorRating",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "TrackExpiry",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "TrackLot",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "Items");
        }
    }
}
