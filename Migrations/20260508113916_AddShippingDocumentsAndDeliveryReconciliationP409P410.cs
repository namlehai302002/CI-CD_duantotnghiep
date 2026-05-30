using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingDocumentsAndDeliveryReconciliationP409P410 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CarrierShipmentId",
                table: "LabelPrintJobs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentNumber",
                table: "LabelPrintJobs",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentType",
                table: "LabelPrintJobs",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "LabelPrintJobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ShipmentLoadId",
                table: "LabelPrintJobs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ShippingHandoverLogId",
                table: "LabelPrintJobs",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabelPrintJobs_CarrierShipmentId",
                table: "LabelPrintJobs",
                column: "CarrierShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_LabelPrintJobs_Document_Number",
                table: "LabelPrintJobs",
                columns: new[] { "DocumentType", "DocumentNumber" },
                filter: "[DocumentType] IS NOT NULL AND [DocumentNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LabelPrintJobs_Load_Document_Date",
                table: "LabelPrintJobs",
                columns: new[] { "ShipmentLoadId", "DocumentType", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LabelPrintJobs_ShippingHandoverLogId",
                table: "LabelPrintJobs",
                column: "ShippingHandoverLogId");

            migrationBuilder.AddForeignKey(
                name: "FK_LabelPrintJobs_CarrierShipments_CarrierShipmentId",
                table: "LabelPrintJobs",
                column: "CarrierShipmentId",
                principalTable: "CarrierShipments",
                principalColumn: "CarrierShipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_LabelPrintJobs_ShipmentLoads_ShipmentLoadId",
                table: "LabelPrintJobs",
                column: "ShipmentLoadId",
                principalTable: "ShipmentLoads",
                principalColumn: "ShipmentLoadId");

            migrationBuilder.AddForeignKey(
                name: "FK_LabelPrintJobs_ShippingHandoverLogs_ShippingHandoverLogId",
                table: "LabelPrintJobs",
                column: "ShippingHandoverLogId",
                principalTable: "ShippingHandoverLogs",
                principalColumn: "ShippingHandoverLogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LabelPrintJobs_CarrierShipments_CarrierShipmentId",
                table: "LabelPrintJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_LabelPrintJobs_ShipmentLoads_ShipmentLoadId",
                table: "LabelPrintJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_LabelPrintJobs_ShippingHandoverLogs_ShippingHandoverLogId",
                table: "LabelPrintJobs");

            migrationBuilder.DropIndex(
                name: "IX_LabelPrintJobs_CarrierShipmentId",
                table: "LabelPrintJobs");

            migrationBuilder.DropIndex(
                name: "IX_LabelPrintJobs_Document_Number",
                table: "LabelPrintJobs");

            migrationBuilder.DropIndex(
                name: "IX_LabelPrintJobs_Load_Document_Date",
                table: "LabelPrintJobs");

            migrationBuilder.DropIndex(
                name: "IX_LabelPrintJobs_ShippingHandoverLogId",
                table: "LabelPrintJobs");

            migrationBuilder.DropColumn(
                name: "CarrierShipmentId",
                table: "LabelPrintJobs");

            migrationBuilder.DropColumn(
                name: "DocumentNumber",
                table: "LabelPrintJobs");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "LabelPrintJobs");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "LabelPrintJobs");

            migrationBuilder.DropColumn(
                name: "ShipmentLoadId",
                table: "LabelPrintJobs");

            migrationBuilder.DropColumn(
                name: "ShippingHandoverLogId",
                table: "LabelPrintJobs");
        }
    }
}
