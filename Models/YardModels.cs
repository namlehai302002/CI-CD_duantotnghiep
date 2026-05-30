using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("YardSpots")]
public class YardSpot
{
    [Key]
    public int YardSpotId { get; set; }

    public int WarehouseId { get; set; }

    [Required, MaxLength(30)]
    public string SpotCode { get; set; } = "";

    [MaxLength(100)]
    public string? SpotName { get; set; }

    public YardSpotTypeEnum SpotType { get; set; } = YardSpotTypeEnum.Standard;

    public YardSpotStatusEnum Status { get; set; } = YardSpotStatusEnum.Available;

    public bool IsActive { get; set; } = true;

    [MaxLength(300)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;
}

[Table("Trailers")]
public class Trailer
{
    [Key]
    public int TrailerId { get; set; }

    [Required, MaxLength(50)]
    public string TrailerNumber { get; set; } = "";

    [MaxLength(50)]
    public string? ContainerNumber { get; set; }

    public TrailerTypeEnum TrailerType { get; set; } = TrailerTypeEnum.Trailer;

    [MaxLength(100)]
    public string? CarrierName { get; set; }

    [MaxLength(50)]
    public string? SealNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<YardVisit> Visits { get; set; } = new List<YardVisit>();
}

[Table("YardVisits")]
public class YardVisit : IOwnerScoped
{
    [Key]
    public long YardVisitId { get; set; }

    [Required, MaxLength(30)]
    public string VisitCode { get; set; } = "";

    public int WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public int TrailerId { get; set; }

    public int? CurrentSpotId { get; set; }

    public long? VoucherId { get; set; }

    public YardVisitPurposeEnum Purpose { get; set; } = YardVisitPurposeEnum.Inbound;

    public YardVisitStatusEnum Status { get; set; } = YardVisitStatusEnum.GatedIn;

    public DateTime GateInAt { get; set; } = VietnamTime.Now;

    public DateTime? GateOutAt { get; set; }

    public DateTime? AssignedSpotAt { get; set; }

    public DateTime? LastMovedAt { get; set; }

    [MaxLength(100)]
    public string GateInBy { get; set; } = "";

    [MaxLength(100)]
    public string? GateOutBy { get; set; }

    [MaxLength(100)]
    public string? DriverName { get; set; }

    [MaxLength(20)]
    public string? DriverPhone { get; set; }

    [MaxLength(30)]
    public string? VehicleNumber { get; set; }

    [MaxLength(20)]
    public string? DockDoor { get; set; }

    public DateTime? DockAppointmentStart { get; set; }

    public DateTime? DockAppointmentEnd { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    [ForeignKey(nameof(TrailerId))]
    public Trailer Trailer { get; set; } = null!;

    [ForeignKey(nameof(CurrentSpotId))]
    public YardSpot? CurrentSpot { get; set; }

    [ForeignKey(nameof(VoucherId))]
    public Voucher? Voucher { get; set; }

    public ICollection<YardVisitEvidence> EvidenceItems { get; set; } = new List<YardVisitEvidence>();

    [NotMapped]
    public bool IsActiveVisit => GateOutAt == null && Status != YardVisitStatusEnum.GatedOut && Status != YardVisitStatusEnum.Cancelled;

    public int GetDwellMinutes(DateTime now)
        => (int)Math.Max(0, ((GateOutAt ?? now) - GateInAt).TotalMinutes);
}

public enum YardSpotTypeEnum : byte
{
    Standard = 1,
    Reefer = 2,
    Hazmat = 3,
    Empty = 4,
    Maintenance = 5
}

public enum YardSpotStatusEnum : byte
{
    Available = 1,
    Occupied = 2,
    Blocked = 3,
    Maintenance = 4
}

public enum TrailerTypeEnum : byte
{
    Trailer = 1,
    Container = 2,
    Reefer = 3,
    Flatbed = 4,
    Tanker = 5
}

public enum YardVisitPurposeEnum : byte
{
    Inbound = 1,
    Outbound = 2,
    Transfer = 3,
    EmptyStorage = 4,
    Other = 9
}

public enum YardVisitStatusEnum : byte
{
    GatedIn = 1,
    Parked = 2,
    AtDock = 3,
    GatedOut = 4,
    Cancelled = 5
}
