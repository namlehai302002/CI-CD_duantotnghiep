using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddUserZoneAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserZoneAssignments",
                columns: table => new
                {
                    UserZoneAssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ZoneId = table.Column<int>(type: "int", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserZoneAssignments", x => x.UserZoneAssignmentId);
                    table.ForeignKey(
                        name: "FK_UserZoneAssignments_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_UserZoneAssignments_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Zones",
                        principalColumn: "ZoneId");
                });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4037));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4154));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4157));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4159));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4161));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4166));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4168));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4170));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4173));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4177));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4179));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4182));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4184));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4186));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4188));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4210));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4213));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4224));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4227));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4229));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4231));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(4233));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5102));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5171));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5174));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5177));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5179));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 6, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5182));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 7, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5186));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 8, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5188));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 9, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5213));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 10, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5216));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 11, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5219));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5221));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5223));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5225));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5228));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5230));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5233));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 18, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5237));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 19, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5240));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 20, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5242));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5245));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5247));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5261));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5265));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5267));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5269));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5272));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5275));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5277));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5279));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5282));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5284));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5287));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5289));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5291));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5485));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5488));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 4 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 3, 6, 22, 837, DateTimeKind.Unspecified).AddTicks(5494));

            migrationBuilder.CreateIndex(
                name: "IX_UserZoneAssignments_UserId_ZoneId",
                table: "UserZoneAssignments",
                columns: new[] { "UserId", "ZoneId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserZoneAssignments_ZoneId",
                table: "UserZoneAssignments",
                column: "ZoneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserZoneAssignments");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6726));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6934));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6937));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6939));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6942));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6946));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6948));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6950));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6952));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6955));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6957));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6959));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6961));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6963));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6965));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6987));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(6990));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(7002));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(7004));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(7006));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(7008));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(7010));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8075));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8088));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8090));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8092));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8093));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 6, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8096));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 7, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8098));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 8, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8099));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 9, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8125));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 10, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8127));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 11, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8129));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8130));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8132));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8133));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8134));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8136));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8137));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 18, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8140));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 19, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8141));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 20, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8142));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8144));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8145));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8159));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8163));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8165));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8168));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8171));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8173));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8177));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8180));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8182));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8185));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8188));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8190));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8192));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8205));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8207));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 4 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 1, 35, 39, 141, DateTimeKind.Unspecified).AddTicks(8212));
        }
    }
}
