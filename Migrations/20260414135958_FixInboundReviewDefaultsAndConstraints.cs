using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class FixInboundReviewDefaultsAndConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte>(
                name: "ReviewResult",
                table: "Vouchers",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<decimal>(
                name: "ResponsibilityScore",
                table: "Vouchers",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.Sql("UPDATE [Vouchers] SET [ReviewResult] = 1 WHERE [ReviewResult] = 0;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Vouchers_ResponsibilityScore_Range",
                table: "Vouchers",
                sql: "[ResponsibilityScore] >= 0 AND [ResponsibilityScore] <= 100");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Vouchers_ResponsibilityScore_Range",
                table: "Vouchers");

            migrationBuilder.AlterColumn<byte>(
                name: "ReviewResult",
                table: "Vouchers",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldDefaultValue: (byte)1);

            migrationBuilder.AlterColumn<decimal>(
                name: "ResponsibilityScore",
                table: "Vouchers",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldDefaultValue: 0m);
        }
    }
}
