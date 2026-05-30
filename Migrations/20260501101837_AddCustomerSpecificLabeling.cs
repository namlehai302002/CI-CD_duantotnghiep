using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerSpecificLabeling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PartnerItemLabelRules",
                columns: table => new
                {
                    PartnerItemLabelRuleId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnerId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    CustomerItemCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CustomerItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerItemLabelRules", x => x.PartnerItemLabelRuleId);
                    table.ForeignKey(
                        name: "FK_PartnerItemLabelRules_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartnerItemLabelRules_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartnerLabelTemplates",
                columns: table => new
                {
                    PartnerLabelTemplateId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnerId = table.Column<int>(type: "int", nullable: true),
                    LabelPurpose = table.Column<byte>(type: "tinyint", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LabelSize = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CodeType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HeaderTemplate = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    BodyTemplate = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FooterTemplate = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerLabelTemplates", x => x.PartnerLabelTemplateId);
                    table.ForeignKey(
                        name: "FK_PartnerLabelTemplates_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LabelPrintJobs",
                columns: table => new
                {
                    LabelPrintJobId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LabelPurpose = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    PartnerId = table.Column<int>(type: "int", nullable: false),
                    VoucherId = table.Column<long>(type: "bigint", nullable: true),
                    OutboundPackageId = table.Column<long>(type: "bigint", nullable: true),
                    PartnerLabelTemplateId = table.Column<long>(type: "bigint", nullable: true),
                    LabelSize = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CodeType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalLabels = table.Column<int>(type: "int", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrintedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PrintedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TemplateSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabelPrintJobs", x => x.LabelPrintJobId);
                    table.ForeignKey(
                        name: "FK_LabelPrintJobs_OutboundPackages_OutboundPackageId",
                        column: x => x.OutboundPackageId,
                        principalTable: "OutboundPackages",
                        principalColumn: "OutboundPackageId");
                    table.ForeignKey(
                        name: "FK_LabelPrintJobs_PartnerLabelTemplates_PartnerLabelTemplateId",
                        column: x => x.PartnerLabelTemplateId,
                        principalTable: "PartnerLabelTemplates",
                        principalColumn: "PartnerLabelTemplateId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LabelPrintJobs_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                    table.ForeignKey(
                        name: "FK_LabelPrintJobs_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                });

            migrationBuilder.CreateTable(
                name: "LabelPrintJobLines",
                columns: table => new
                {
                    LabelPrintJobLineId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LabelPrintJobId = table.Column<long>(type: "bigint", nullable: false),
                    VoucherDetailId = table.Column<long>(type: "bigint", nullable: true),
                    OutboundPackageId = table.Column<long>(type: "bigint", nullable: true),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PrintQuantity = table.Column<int>(type: "int", nullable: false),
                    BarcodeValue = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    InternalItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InternalItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerItemCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CustomerItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PartnerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VoucherCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PackageCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    TrackingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HeaderText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    BodyText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FooterText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    RenderDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabelPrintJobLines", x => x.LabelPrintJobLineId);
                    table.ForeignKey(
                        name: "FK_LabelPrintJobLines_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId");
                    table.ForeignKey(
                        name: "FK_LabelPrintJobLines_LabelPrintJobs_LabelPrintJobId",
                        column: x => x.LabelPrintJobId,
                        principalTable: "LabelPrintJobs",
                        principalColumn: "LabelPrintJobId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LabelPrintJobLines_OutboundPackages_OutboundPackageId",
                        column: x => x.OutboundPackageId,
                        principalTable: "OutboundPackages",
                        principalColumn: "OutboundPackageId");
                    table.ForeignKey(
                        name: "FK_LabelPrintJobLines_VoucherDetails_VoucherDetailId",
                        column: x => x.VoucherDetailId,
                        principalTable: "VoucherDetails",
                        principalColumn: "VoucherDetailId");
                });

            migrationBuilder.InsertData(
                table: "PartnerLabelTemplates",
                columns: new[] { "PartnerLabelTemplateId", "BodyTemplate", "CodeType", "CreatedAt", "CreatedBy", "FooterTemplate", "HeaderTemplate", "IsActive", "IsDefault", "LabelPurpose", "LabelSize", "PartnerId", "TemplateName", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1L, "Mã khách: {MaHangKhach}\nTên hàng: {TenHangKhach}\nPhiếu: {MaPhieu}\nSố lượng: {SoLuong}", "barcode", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "system", "In lúc {NgayIn}", "{TenKhachHang}", true, true, (byte)1, "50x30", null, "Mẫu mặc định cho phiếu xuất", null, null },
                    { 2L, "Kiện: {MaKien}\nPhiếu: {MaPhieu}\nMã khách: {MaHangKhach}\nTên hàng: {TenHangKhach}\nSố lượng: {SoLuong}", "barcode", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "system", "Vận đơn: {MaVanDon}", "{TenKhachHang}", true, true, (byte)2, "100x50", null, "Mẫu mặc định cho kiện xuất", null, null }
                });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(718));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(769));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(772));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(774));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(777));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(782));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(784));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(787));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(790));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(794));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(797));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(800));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(802));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(805));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(807));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(913));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(915));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(928));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(931));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(933));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(936));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(938));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(1918));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2137));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2141));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2143));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2146));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 6, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2150));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 7, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2152));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 8, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2155));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 9, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2186));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 10, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2190));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 11, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2192));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2195));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2198));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2200));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2203));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2205));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2207));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 18, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2210));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 19, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2213));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 20, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2215));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2218));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2220));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2236));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2240));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2243));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2246));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2249));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2252));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2255));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2258));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2261));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2264));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2444));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2449));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2452));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2571));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2574));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 4 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 17, 18, 35, 61, DateTimeKind.Unspecified).AddTicks(2580));

            migrationBuilder.CreateIndex(
                name: "IX_LabelPrintJobLines_ItemId",
                table: "LabelPrintJobLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LabelPrintJobLines_Job",
                table: "LabelPrintJobLines",
                column: "LabelPrintJobId");

            migrationBuilder.CreateIndex(
                name: "IX_LabelPrintJobLines_OutboundPackageId",
                table: "LabelPrintJobLines",
                column: "OutboundPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_LabelPrintJobLines_VoucherDetailId",
                table: "LabelPrintJobLines",
                column: "VoucherDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_LabelPrintJobs_JobCode",
                table: "LabelPrintJobs",
                column: "JobCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabelPrintJobs_OutboundPackageId",
                table: "LabelPrintJobs",
                column: "OutboundPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_LabelPrintJobs_Partner_Date",
                table: "LabelPrintJobs",
                columns: new[] { "PartnerId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LabelPrintJobs_PartnerLabelTemplateId",
                table: "LabelPrintJobs",
                column: "PartnerLabelTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_LabelPrintJobs_Voucher_Package",
                table: "LabelPrintJobs",
                columns: new[] { "VoucherId", "OutboundPackageId" });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerItemLabelRules_ItemId",
                table: "PartnerItemLabelRules",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "UX_PartnerItemLabelRules_Partner_Item",
                table: "PartnerItemLabelRules",
                columns: new[] { "PartnerId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartnerLabelTemplates_Default_Purpose",
                table: "PartnerLabelTemplates",
                columns: new[] { "LabelPurpose", "IsDefault", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerLabelTemplates_Partner_Purpose_Active",
                table: "PartnerLabelTemplates",
                columns: new[] { "PartnerId", "LabelPurpose", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabelPrintJobLines");

            migrationBuilder.DropTable(
                name: "PartnerItemLabelRules");

            migrationBuilder.DropTable(
                name: "LabelPrintJobs");

            migrationBuilder.DropTable(
                name: "PartnerLabelTemplates");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5709));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5758));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5761));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5763));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5765));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5769));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5771));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5772));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5774));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5777));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5779));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5781));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5783));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5785));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5786));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5808));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5810));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5822));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5824));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5826));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5828));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(5830));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6697));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6763));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6766));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6768));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6770));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 6, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6774));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 7, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6776));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 8, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6778));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 9, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6807));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 10, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6810));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 11, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6812));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6814));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6815));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6817));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6819));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6820));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6822));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 18, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6825));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 19, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6827));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 20, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6829));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6831));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 1 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6833));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6848));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 2, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6851));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 3, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6853));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 4, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6855));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 5, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6858));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 12, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6860));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 13, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6863));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 14, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6865));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 15, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6867));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 16, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6870));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6872));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6874));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 2 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6876));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 1, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6965));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 3 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6968));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 17, 4 },
                column: "CreatedAt",
                value: new DateTime(2026, 5, 1, 16, 55, 0, 413, DateTimeKind.Unspecified).AddTicks(6973));
        }
    }
}
