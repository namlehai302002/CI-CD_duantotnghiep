using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddLotAndRowVersionAndBatchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Be defensive: some databases may not have the expected index name.
            // Drop the old unique index on (ItemId, LocationId) if it exists.
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ItemLocations_ItemId_LocationId'
      AND object_id = OBJECT_ID(N'[dbo].[ItemLocations]')
)
    DROP INDEX [IX_ItemLocations_ItemId_LocationId] ON [dbo].[ItemLocations];
");

            // Also drop any other unique index on ItemLocations that exactly covers (ItemId, LocationId)
            // but has a non-standard name (manual DBA rename/custom scripts).
            migrationBuilder.Sql(@"
DECLARE @idxName sysname;
SELECT TOP(1) @idxName = i.name
FROM sys.indexes i
JOIN sys.index_columns ic1 ON ic1.object_id = i.object_id AND ic1.index_id = i.index_id AND ic1.key_ordinal = 1
JOIN sys.index_columns ic2 ON ic2.object_id = i.object_id AND ic2.index_id = i.index_id AND ic2.key_ordinal = 2
JOIN sys.columns c1 ON c1.object_id = i.object_id AND c1.column_id = ic1.column_id
JOIN sys.columns c2 ON c2.object_id = i.object_id AND c2.column_id = ic2.column_id
WHERE i.object_id = OBJECT_ID(N'[dbo].[ItemLocations]')
  AND i.is_unique = 1
  AND c1.name = N'ItemId'
  AND c2.name = N'LocationId'
  AND i.name <> N'PK_ItemLocations'
  AND i.name <> N'IX_ItemLocations_ItemId_LocationId'
  AND NOT EXISTS (
      SELECT 1
      FROM sys.index_columns icx
      WHERE icx.object_id = i.object_id
        AND icx.index_id = i.index_id
        AND icx.key_ordinal > 2
  );

IF @idxName IS NOT NULL
BEGIN
    EXEC(N'DROP INDEX [' + @idxName + N'] ON [dbo].[ItemLocations];');
END
");

            migrationBuilder.AddColumn<string>(
                name: "LotNumber",
                table: "VoucherDetails",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Items",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ItemLocations",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocations_ItemId_LocationId",
                table: "ItemLocations",
                columns: new[] { "ItemId", "LocationId" },
                unique: true,
                filter: "[LotNumber] IS NULL AND [ExpiryDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocations_ItemId_LocationId_ExpiryDate",
                table: "ItemLocations",
                columns: new[] { "ItemId", "LocationId", "ExpiryDate" },
                unique: true,
                filter: "[LotNumber] IS NULL AND [ExpiryDate] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocations_ItemId_LocationId_LotNumber",
                table: "ItemLocations",
                columns: new[] { "ItemId", "LocationId", "LotNumber" },
                unique: true,
                filter: "[LotNumber] IS NOT NULL AND [ExpiryDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocations_ItemId_LocationId_LotNumber_ExpiryDate",
                table: "ItemLocations",
                columns: new[] { "ItemId", "LocationId", "LotNumber", "ExpiryDate" },
                unique: true,
                filter: "[LotNumber] IS NOT NULL AND [ExpiryDate] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ItemLocations_ItemId_LocationId",
                table: "ItemLocations");

            migrationBuilder.DropIndex(
                name: "IX_ItemLocations_ItemId_LocationId_ExpiryDate",
                table: "ItemLocations");

            migrationBuilder.DropIndex(
                name: "IX_ItemLocations_ItemId_LocationId_LotNumber",
                table: "ItemLocations");

            migrationBuilder.DropIndex(
                name: "IX_ItemLocations_ItemId_LocationId_LotNumber_ExpiryDate",
                table: "ItemLocations");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                table: "VoucherDetails");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ItemLocations");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocations_ItemId_LocationId",
                table: "ItemLocations",
                columns: new[] { "ItemId", "LocationId" },
                unique: true);
        }
    }
}
