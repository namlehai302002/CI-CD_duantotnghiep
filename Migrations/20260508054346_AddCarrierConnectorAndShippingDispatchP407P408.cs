using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddCarrierConnectorAndShippingDispatchP407P408 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CarrierConnectors",
                columns: table => new
                {
                    CarrierConnectorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    CarrierCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CarrierName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AdapterType = table.Column<byte>(type: "tinyint", nullable: false),
                    AuthType = table.Column<byte>(type: "tinyint", nullable: false),
                    EndpointUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ApiKeyReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsSandbox = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RequireShipmentCreatedBeforeShipping = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrierConnectors", x => x.CarrierConnectorId);
                    table.ForeignKey(
                        name: "FK_CarrierConnectors_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "CarrierShipments",
                columns: table => new
                {
                    CarrierShipmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarrierConnectorId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    OwnerPartnerId = table.Column<int>(type: "int", nullable: true),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
                    OutboundPackageId = table.Column<long>(type: "bigint", nullable: false),
                    ShipmentLoadId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CarrierCodeSnapshot = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CarrierNameSnapshot = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TrackingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LabelUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProofOfDeliveryUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExternalShipmentId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RequestPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponsePayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    QueuedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CarrierCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrierShipments", x => x.CarrierShipmentId);
                    table.ForeignKey(
                        name: "FK_CarrierShipments_CarrierConnectors_CarrierConnectorId",
                        column: x => x.CarrierConnectorId,
                        principalTable: "CarrierConnectors",
                        principalColumn: "CarrierConnectorId");
                    table.ForeignKey(
                        name: "FK_CarrierShipments_OutboundPackages_OutboundPackageId",
                        column: x => x.OutboundPackageId,
                        principalTable: "OutboundPackages",
                        principalColumn: "OutboundPackageId");
                    table.ForeignKey(
                        name: "FK_CarrierShipments_Partners_OwnerPartnerId",
                        column: x => x.OwnerPartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId");
                    table.ForeignKey(
                        name: "FK_CarrierShipments_ShipmentLoads_ShipmentLoadId",
                        column: x => x.ShipmentLoadId,
                        principalTable: "ShipmentLoads",
                        principalColumn: "ShipmentLoadId");
                    table.ForeignKey(
                        name: "FK_CarrierShipments_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId");
                    table.ForeignKey(
                        name: "FK_CarrierShipments_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "CarrierShipmentEvents",
                columns: table => new
                {
                    CarrierShipmentEventId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarrierShipmentId = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ExternalEventId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrierShipmentEvents", x => x.CarrierShipmentEventId);
                    table.ForeignKey(
                        name: "FK_CarrierShipmentEvents_CarrierShipments_CarrierShipmentId",
                        column: x => x.CarrierShipmentId,
                        principalTable: "CarrierShipments",
                        principalColumn: "CarrierShipmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarrierConnectors_Warehouse_Active",
                table: "CarrierConnectors",
                columns: new[] { "WarehouseId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UX_CarrierConnectors_Warehouse_Code",
                table: "CarrierConnectors",
                columns: new[] { "WarehouseId", "CarrierCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarrierShipmentEvents_Shipment_Date",
                table: "CarrierShipmentEvents",
                columns: new[] { "CarrierShipmentId", "EventAt" });

            migrationBuilder.CreateIndex(
                name: "UX_CarrierShipmentEvents_IdempotencyKey",
                table: "CarrierShipmentEvents",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarrierShipments_CarrierConnectorId",
                table: "CarrierShipments",
                column: "CarrierConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_CarrierShipments_OwnerPartnerId",
                table: "CarrierShipments",
                column: "OwnerPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CarrierShipments_Package_Status",
                table: "CarrierShipments",
                columns: new[] { "OutboundPackageId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CarrierShipments_ShipmentLoadId",
                table: "CarrierShipments",
                column: "ShipmentLoadId");

            migrationBuilder.CreateIndex(
                name: "IX_CarrierShipments_Voucher_Status",
                table: "CarrierShipments",
                columns: new[] { "VoucherId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CarrierShipments_Warehouse_Status_Created",
                table: "CarrierShipments",
                columns: new[] { "WarehouseId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_CarrierShipments_CorrelationId",
                table: "CarrierShipments",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CarrierShipments_IdempotencyKey",
                table: "CarrierShipments",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarrierShipmentEvents");

            migrationBuilder.DropTable(
                name: "CarrierShipments");

            migrationBuilder.DropTable(
                name: "CarrierConnectors");
        }
    }
}
