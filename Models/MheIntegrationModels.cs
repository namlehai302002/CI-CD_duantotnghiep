using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("MheSystems")]
public class MheSystem
{
    [Key]
    public int MheSystemId { get; set; }

    public int WarehouseId { get; set; }

    [Required, MaxLength(40)]
    public string SystemCode { get; set; } = "";

    [Required, MaxLength(120)]
    public string SystemName { get; set; } = "";

    public MheSystemTypeEnum SystemType { get; set; } = MheSystemTypeEnum.Wcs;

    [MaxLength(300)]
    public string? EndpointUrl { get; set; }

    [MaxLength(200)]
    public string? ApiKeyReference { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;
}

[Table("MheCommands")]
public class MheCommand : IOwnerScoped
{
    [Key]
    public long MheCommandId { get; set; }

    [Required, MaxLength(50)]
    public string CommandCode { get; set; } = "";

    public int WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public int? MheSystemId { get; set; }

    public MheCommandTypeEnum CommandType { get; set; }

    public MheCommandStatusEnum Status { get; set; } = MheCommandStatusEnum.Pending;

    public long? PickTaskId { get; set; }

    public long? MovementTaskId { get; set; }

    public long? WaveId { get; set; }

    public long? OutboundPackageId { get; set; }

    public long? LicensePlateId { get; set; }

    [MaxLength(80)]
    public string? SourceType { get; set; }

    [MaxLength(80)]
    public string? SourceId { get; set; }

    [MaxLength(120)]
    public string? SourceCode { get; set; }

    [Required, MaxLength(160)]
    public string IdempotencyKey { get; set; } = "";

    [Required, MaxLength(80)]
    public string CorrelationId { get; set; } = "";

    public string PayloadJson { get; set; } = "{}";

    public string? LastCallbackJson { get; set; }

    [MaxLength(500)]
    public string? LastError { get; set; }

    public int RetryCount { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? AcknowledgedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? FailedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    [ForeignKey(nameof(MheSystemId))]
    public MheSystem? MheSystem { get; set; }

    [ForeignKey(nameof(PickTaskId))]
    public PickTask? PickTask { get; set; }

    [ForeignKey(nameof(MovementTaskId))]
    public MovementTask? MovementTask { get; set; }

    [ForeignKey(nameof(WaveId))]
    public Wave? Wave { get; set; }

    [ForeignKey(nameof(OutboundPackageId))]
    public OutboundPackage? OutboundPackage { get; set; }

    [ForeignKey(nameof(LicensePlateId))]
    public LicensePlate? LicensePlate { get; set; }

    public ICollection<MheMissionEvent> MissionEvents { get; set; } = new List<MheMissionEvent>();
}

[Table("MheMissionEvents")]
public class MheMissionEvent
{
    [Key]
    public long MheMissionEventId { get; set; }

    public long MheCommandId { get; set; }

    public MheCommandStatusEnum Status { get; set; }

    [MaxLength(80)]
    public string? ExternalMissionId { get; set; }

    [MaxLength(500)]
    public string? Message { get; set; }

    [Required, MaxLength(160)]
    public string IdempotencyKey { get; set; } = "";

    public string PayloadJson { get; set; } = "{}";

    public DateTime EventAt { get; set; } = VietnamTime.Now;

    [ForeignKey(nameof(MheCommandId))]
    public MheCommand MheCommand { get; set; } = null!;
}
