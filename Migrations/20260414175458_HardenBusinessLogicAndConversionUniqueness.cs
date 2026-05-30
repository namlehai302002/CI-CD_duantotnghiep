using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class HardenBusinessLogicAndConversionUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_UnitConversions_FromUomId_ToUomId'
      AND object_id = OBJECT_ID(N'[UnitConversions]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_UnitConversions_FromUomId_ToUomId]
    ON [UnitConversions]([FromUomId], [ToUomId])
    WHERE [ItemId] IS NULL;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_UnitConversions_FromUomId_ToUomId'
      AND object_id = OBJECT_ID(N'[UnitConversions]')
)
BEGIN
    DROP INDEX [IX_UnitConversions_FromUomId_ToUomId] ON [UnitConversions];
END");
        }
    }
}
