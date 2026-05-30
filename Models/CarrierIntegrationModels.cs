using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

public enum CarrierAdapterTypeEnum : byte
{
    Mock = 1,
    Http = 2
}

public enum CarrierAuthTypeEnum : byte
{
    None = 1,
    ApiKey = 2,
    Bearer = 3,
    Basic = 4
}

public enum CarrierShipmentStatusEnum : byte
{
    Pending = 1,
    Queued = 2,
    Created = 3,
    Failed = 4,
    Cancelled = 5,
    Delivered = 6,
    DeliveryFailed = 7
}

public enum CarrierShipmentEventTypeEnum : byte
{
    Requested = 1,
    Created = 2,
    Failed = 3,
    Cancelled = 4,
    StatusSynced = 5,
    Delivered = 6,
    Callback = 7
}

[Table("CarrierConnectors")]
public class CarrierConnector
{
    [Key]
    public int CarrierConnectorId { get; set; }

    public int WarehouseId { get; set; }

    [Required, MaxLength(40)]
    public string CarrierCode { get; set; } = "";

    [Required, MaxLength(120)]
    public string CarrierName { get; set; } = "";

    public CarrierAdapterTypeEnum AdapterType { get; set; } = CarrierAdapterTypeEnum.Mock;

    public CarrierAuthTypeEnum AuthType { get; set; } = CarrierAuthTypeEnum.None;

    [MaxLength(300)]
    public string? EndpointUrl { get; set; }

    [MaxLength(200)]
    public string? ApiKeyReference { get; set; }

    public bool IsSandbox { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public bool RequireShipmentCreatedBeforeShipping { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;
}

[Table("CarrierShipments")]
public class CarrierShipment : IOwnerScoped
{
    [Key]
    public long CarrierShipmentId { get; set; }

    public int CarrierConnectorId { get; set; }

    public int WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public long VoucherId { get; set; }

    public long OutboundPackageId { get; set; }

    public long? ShipmentLoadId { get; set; }

    public CarrierShipmentStatusEnum Status { get; set; } = CarrierShipmentStatusEnum.Pending;

    [Required, MaxLength(40)]
    public string CarrierCodeSnapshot { get; set; } = "";

    [Required, MaxLength(120)]
    public string CarrierNameSnapshot { get; set; } = "";

    [MaxLength(100)]
    public string? TrackingNumber { get; set; }

    [MaxLength(500)]
    public string? LabelUrl { get; set; }

    [MaxLength(500)]
    public string? ProofOfDeliveryUrl { get; set; }

    [MaxLength(120)]
    public string? ExternalShipmentId { get; set; }

    [Required, MaxLength(160)]
    public string IdempotencyKey { get; set; } = "";

    [Required, MaxLength(80)]
    public string CorrelationId { get; set; } = "";

    public string RequestPayloadJson { get; set; } = "{}";

    public string? ResponsePayloadJson { get; set; }

    [MaxLength(500)]
    public string? LastError { get; set; }

    public int RetryCount { get; set; }

    public DateTime? QueuedAt { get; set; }

    public DateTime? CarrierCreatedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(CarrierConnectorId))]
    public CarrierConnector CarrierConnector { get; set; } = null!;

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    [ForeignKey(nameof(VoucherId))]
    public Voucher Voucher { get; set; } = null!;

    [ForeignKey(nameof(OutboundPackageId))]
    public OutboundPackage OutboundPackage { get; set; } = null!;

    [ForeignKey(nameof(ShipmentLoadId))]
    public ShipmentLoad? ShipmentLoad { get; set; }

    public ICollection<CarrierShipmentEvent> Events { get; set; } = new List<CarrierShipmentEvent>();
}

[Table("CarrierShipmentEvents")]
public class CarrierShipmentEvent
{
    [Key]
    public long CarrierShipmentEventId { get; set; }

    public long CarrierShipmentId { get; set; }

    public CarrierShipmentEventTypeEnum EventType { get; set; }

    public CarrierShipmentStatusEnum Status { get; set; }

    [Required, MaxLength(160)]
    public string IdempotencyKey { get; set; } = "";

    [MaxLength(120)]
    public string? ExternalEventId { get; set; }

    [MaxLength(500)]
    public string? Message { get; set; }

    public string PayloadJson { get; set; } = "{}";

    public DateTime EventAt { get; set; } = VietnamTime.Now;

    [ForeignKey(nameof(CarrierShipmentId))]
    public CarrierShipment CarrierShipment { get; set; } = null!;
}
