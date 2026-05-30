using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class Fix_BugHunt_2026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlaMetrics_Vouchers_VoucherId",
                table: "SlaMetrics");

            migrationBuilder.AddColumn<long>(
                name: "WaveId",
                table: "SerialNumbers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "RolePermissions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Permissions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Permissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Permissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "IntegrationOutbox",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AiOcrLogs",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AiOcrAdjustments",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1002), null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1038), null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1040), null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1041), null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1041), null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1043), null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1044), null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1045), null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 9,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1045), null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 10,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1047), null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1047), null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1048), null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1048), null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1049), null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1050), null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 16,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1050), null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 17,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1051), null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 18,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1057), null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 19,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1057), null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 20,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1058), null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 21,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1059), null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 22,
                columns: new[] { "CreatedAt", "CreatedBy", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1059), null, null });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1304));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1307));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1308));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1309));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1310));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 6, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1311));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 7, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1312));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 8, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1313));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 9, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1313));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 10, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1314));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 11, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1315));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1315));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1316));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1316));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1317));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1317));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1318));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 18, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1319));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 19, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1320));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 20, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1320));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1321));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1321));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1328));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1329));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1330));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1331));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1332));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1333));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1334));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1334));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1335));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1336));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1337));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1338));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1339));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1345));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1346));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 4 },
                column: "CreatedAt",
                value: new DateTime(2026, 4, 26, 20, 2, 43, 690, DateTimeKind.Unspecified).AddTicks(1348));

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_ParentVoucherId",
                table: "Vouchers",
                column: "ParentVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_ConsumedPickTaskId",
                table: "SerialNumbers",
                column: "ConsumedPickTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_WaveId",
                table: "SerialNumbers",
                column: "WaveId");

            migrationBuilder.CreateIndex(
                name: "IX_MfaLoginChallenges_IsUsed_ExpiresAt",
                table: "MfaLoginChallenges",
                columns: new[] { "IsUsed", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemVelocityClassifications_SuggestedBulkLocationId",
                table: "ItemVelocityClassifications",
                column: "SuggestedBulkLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemVelocityClassifications_SuggestedPickFaceLocationId",
                table: "ItemVelocityClassifications",
                column: "SuggestedPickFaceLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemVelocityClassifications_Locations_SuggestedBulkLocationId",
                table: "ItemVelocityClassifications",
                column: "SuggestedBulkLocationId",
                principalTable: "Locations",
                principalColumn: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemVelocityClassifications_Locations_SuggestedPickFaceLocationId",
                table: "ItemVelocityClassifications",
                column: "SuggestedPickFaceLocationId",
                principalTable: "Locations",
                principalColumn: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_SerialNumbers_PickTasks_ConsumedPickTaskId",
                table: "SerialNumbers",
                column: "ConsumedPickTaskId",
                principalTable: "PickTasks",
                principalColumn: "PickTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_SerialNumbers_PickTasks_WaveId",
                table: "SerialNumbers",
                column: "WaveId",
                principalTable: "PickTasks",
                principalColumn: "PickTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_SlaMetrics_Vouchers_VoucherId",
                table: "SlaMetrics",
                column: "VoucherId",
                principalTable: "Vouchers",
                principalColumn: "VoucherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_Vouchers_ParentVoucherId",
                table: "Vouchers",
                column: "ParentVoucherId",
                principalTable: "Vouchers",
                principalColumn: "VoucherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemVelocityClassifications_Locations_SuggestedBulkLocationId",
                table: "ItemVelocityClassifications");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemVelocityClassifications_Locations_SuggestedPickFaceLocationId",
                table: "ItemVelocityClassifications");

            migrationBuilder.DropForeignKey(
                name: "FK_SerialNumbers_PickTasks_ConsumedPickTaskId",
                table: "SerialNumbers");

            migrationBuilder.DropForeignKey(
                name: "FK_SerialNumbers_PickTasks_WaveId",
                table: "SerialNumbers");

            migrationBuilder.DropForeignKey(
                name: "FK_SlaMetrics_Vouchers_VoucherId",
                table: "SlaMetrics");

            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_Vouchers_ParentVoucherId",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_ParentVoucherId",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_SerialNumbers_ConsumedPickTaskId",
                table: "SerialNumbers");

            migrationBuilder.DropIndex(
                name: "IX_SerialNumbers_WaveId",
                table: "SerialNumbers");

            migrationBuilder.DropIndex(
                name: "IX_MfaLoginChallenges_IsUsed_ExpiresAt",
                table: "MfaLoginChallenges");

            migrationBuilder.DropIndex(
                name: "IX_ItemVelocityClassifications_SuggestedBulkLocationId",
                table: "ItemVelocityClassifications");

            migrationBuilder.DropIndex(
                name: "IX_ItemVelocityClassifications_SuggestedPickFaceLocationId",
                table: "ItemVelocityClassifications");

            migrationBuilder.DropColumn(
                name: "WaveId",
                table: "SerialNumbers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IntegrationOutbox");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AiOcrLogs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AiOcrAdjustments");

            migrationBuilder.AddForeignKey(
                name: "FK_SlaMetrics_Vouchers_VoucherId",
                table: "SlaMetrics",
                column: "VoucherId",
                principalTable: "Vouchers",
                principalColumn: "VoucherId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
