using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

public enum DockAppointmentDirectionEnum : byte
{
    Inbound = 1,
    Outbound = 2,
    Transfer = 3
}

public enum DockAppointmentStatusEnum : byte
{
    Scheduled = 1,
    CheckedIn = 2,
    AtDock = 3,
    Completed = 4,
    Cancelled = 5,
    NoShow = 6
}

public enum YardEvidenceTypeEnum : byte
{
    GateInPhoto = 1,
    GateOutPhoto = 2,
    SealPhoto = 3,
    DriverDocument = 4,
    ContainerCondition = 5,
    Other = 99
}

[Table("DockAppointments")]
public class DockAppointment : IOwnerScoped
{
    [Key]
    public long DockAppointmentId { get; set; }

    [Required, MaxLength(40)]
    public string AppointmentCode { get; set; } = "";

    public int WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public long? VoucherId { get; set; }

    public long? ShipmentLoadId { get; set; }

    public DockAppointmentDirectionEnum Direction { get; set; } = DockAppointmentDirectionEnum.Inbound;

    public DockAppointmentStatusEnum Status { get; set; } = DockAppointmentStatusEnum.Scheduled;

    [Required, MaxLength(20)]
    public string DockDoor { get; set; } = "";

    public DateTime PlannedStartAt { get; set; } = VietnamTime.Now;

    public DateTime PlannedEndAt { get; set; } = VietnamTime.Now.AddHours(1);

    public DateTime? CheckInAt { get; set; }

    public DateTime? DockStartAt { get; set; }

    public DateTime? DockEndAt { get; set; }

    public DateTime? CheckOutAt { get; set; }

    [MaxLength(80)]
    public string? GoodsType { get; set; }

    public bool IsHazmat { get; set; }

    public bool IsRefrigerated { get; set; }

    public bool IsUrgent { get; set; }

    public int Priority { get; set; } = 5;

    [Column(TypeName = "decimal(18,4)")]
    public decimal PalletCount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal CartonCount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal WeightKg { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal VolumeCbm { get; set; }

    public int SuggestedScore { get; set; }

    public bool HasConflictWarning { get; set; }

    [MaxLength(500)]
    public string? OverloadWarning { get; set; }

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

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    [ForeignKey(nameof(VoucherId))]
    public Voucher? Voucher { get; set; }

    [ForeignKey(nameof(ShipmentLoadId))]
    public ShipmentLoad? ShipmentLoad { get; set; }
}

[Table("YardVisitEvidence")]
public class YardVisitEvidence
{
    [Key]
    public long YardVisitEvidenceId { get; set; }

    public long YardVisitId { get; set; }

    public YardEvidenceTypeEnum EvidenceType { get; set; } = YardEvidenceTypeEnum.Other;

    [Required, MaxLength(500)]
    public string FileUrl { get; set; } = "";

    [MaxLength(260)]
    public string? OriginalFileName { get; set; }

    [MaxLength(120)]
    public string? ContentType { get; set; }

    [MaxLength(64)]
    public string? FileHashSha256 { get; set; }

    [MaxLength(50)]
    public string? SealNumberSnapshot { get; set; }

    [MaxLength(50)]
    public string? ContainerNumberSnapshot { get; set; }

    [MaxLength(100)]
    public string? DriverNameSnapshot { get; set; }

    [MaxLength(30)]
    public string? VehicleNumberSnapshot { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string CapturedBy { get; set; } = "system";

    public DateTime CapturedAt { get; set; } = VietnamTime.Now;

    [ForeignKey(nameof(YardVisitId))]
    public YardVisit YardVisit { get; set; } = null!;
}

public enum ThreePlContractStatusEnum : byte
{
    Draft = 1,
    Active = 2,
    Suspended = 3,
    Expired = 4
}

public enum ThreePlInvoiceStatusEnum : byte
{
    Draft = 1,
    Confirmed = 2,
    Locked = 3,
    Voided = 4
}

public enum ThreePlDisputeStatusEnum : byte
{
    Open = 1,
    InReview = 2,
    Approved = 3,
    Rejected = 4,
    Cancelled = 5
}

[Table("ThreePlContracts")]
public class ThreePlContract : IOwnerScoped
{
    [Key]
    public long ThreePlContractId { get; set; }

    [Required, MaxLength(40)]
    public string ContractCode { get; set; } = "";

    [Required, MaxLength(160)]
    public string ContractName { get; set; } = "";

    public int WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public ThreePlContractStatusEnum Status { get; set; } = ThreePlContractStatusEnum.Active;

    [Column(TypeName = "date")]
    public DateTime EffectiveFrom { get; set; } = VietnamTime.Now.Date;

    [Column(TypeName = "date")]
    public DateTime? EffectiveTo { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "VND";

    [Column(TypeName = "decimal(18,4)")]
    public decimal MinimumCharge { get; set; }

    [Column(TypeName = "decimal(9,4)")]
    public decimal TaxPercent { get; set; }

    [Column(TypeName = "decimal(9,4)")]
    public decimal DiscountPercent { get; set; }

    public bool RequiresAdjustmentApproval { get; set; } = true;

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

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    public ICollection<ThreePlContractRate> Rates { get; set; } = new List<ThreePlContractRate>();
}

[Table("ThreePlContractRates")]
public class ThreePlContractRate
{
    [Key]
    public long ThreePlContractRateId { get; set; }

    public long ThreePlContractId { get; set; }

    public ThreePlChargeTypeEnum ChargeType { get; set; } = ThreePlChargeTypeEnum.Storage;

    [Required, MaxLength(80)]
    public string RateCode { get; set; } = "";

    [MaxLength(80)]
    public string ServiceCode { get; set; } = "";

    [MaxLength(20)]
    public string ChargeUnit { get; set; } = "unit";

    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitRate { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TierFromQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? TierToQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal IncludedQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal MinimumCharge { get; set; }

    [Column(TypeName = "decimal(9,4)")]
    public decimal SurchargePercent { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal OffHoursSurcharge { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal UrgentSurcharge { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal HazmatSurcharge { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ColdStorageSurcharge { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ManualHandlingSurcharge { get; set; }

    [Column(TypeName = "decimal(9,4)")]
    public decimal SlaPenaltyPercent { get; set; }

    [Column(TypeName = "decimal(9,4)")]
    public decimal SlaBonusPercent { get; set; }

    [Column(TypeName = "date")]
    public DateTime EffectiveFrom { get; set; } = VietnamTime.Now.Date;

    [Column(TypeName = "date")]
    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(300)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [ForeignKey(nameof(ThreePlContractId))]
    public ThreePlContract Contract { get; set; } = null!;
}

[Table("ThreePlInvoices")]
public class ThreePlInvoice : IOwnerScoped
{
    [Key]
    public long ThreePlInvoiceId { get; set; }

    [Required, MaxLength(40)]
    public string InvoiceCode { get; set; } = "";

    public long? ThreePlBillingRunId { get; set; }

    public long? ThreePlContractId { get; set; }

    public int WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    [Column(TypeName = "date")]
    public DateTime PeriodFrom { get; set; }

    [Column(TypeName = "date")]
    public DateTime PeriodTo { get; set; }

    public ThreePlInvoiceStatusEnum Status { get; set; } = ThreePlInvoiceStatusEnum.Draft;

    [MaxLength(10)]
    public string Currency { get; set; } = "VND";

    [Column(TypeName = "decimal(18,4)")]
    public decimal SubtotalAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AdjustmentAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalAmount { get; set; }

    [Required, MaxLength(80)]
    public string ApiPublicId { get; set; } = "";

    public DateTime? ConfirmedAt { get; set; }

    [MaxLength(100)]
    public string? ConfirmedBy { get; set; }

    public DateTime? LockedAt { get; set; }

    [MaxLength(100)]
    public string? LockedBy { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(ThreePlBillingRunId))]
    public ThreePlBillingRun? BillingRun { get; set; }

    [ForeignKey(nameof(ThreePlContractId))]
    public ThreePlContract? Contract { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    public ICollection<ThreePlInvoiceLine> Lines { get; set; } = new List<ThreePlInvoiceLine>();
}

[Table("ThreePlInvoiceLines")]
public class ThreePlInvoiceLine
{
    [Key]
    public long ThreePlInvoiceLineId { get; set; }

    public long ThreePlInvoiceId { get; set; }

    public long? ThreePlBillingChargeId { get; set; }

    public ThreePlChargeTypeEnum ChargeType { get; set; } = ThreePlChargeTypeEnum.Storage;

    [MaxLength(80)]
    public string LineType { get; set; } = "Charge";

    [MaxLength(180)]
    public string Description { get; set; } = "";

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    [MaxLength(20)]
    public string ChargeUnit { get; set; } = "unit";

    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitRate { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal SubtotalAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AdjustmentAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalAmount { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "VND";

    [MaxLength(300)]
    public string? AdjustmentReason { get; set; }

    [MaxLength(100)]
    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [ForeignKey(nameof(ThreePlInvoiceId))]
    public ThreePlInvoice Invoice { get; set; } = null!;

    [ForeignKey(nameof(ThreePlBillingChargeId))]
    public ThreePlBillingCharge? BillingCharge { get; set; }
}

[Table("ThreePlDisputes")]
public class ThreePlDispute : IOwnerScoped
{
    [Key]
    public long ThreePlDisputeId { get; set; }

    public long ThreePlInvoiceLineId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public ThreePlDisputeStatusEnum Status { get; set; } = ThreePlDisputeStatusEnum.Open;

    [Required, MaxLength(500)]
    public string Reason { get; set; } = "";

    [Column(TypeName = "decimal(18,4)")]
    public decimal RequestedAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ApprovedAmount { get; set; }

    [MaxLength(500)]
    public string? StaffResponse { get; set; }

    [MaxLength(500)]
    public string? ResolutionNotes { get; set; }

    [MaxLength(100)]
    public string OpenedBy { get; set; } = "system";

    public DateTime OpenedAt { get; set; } = VietnamTime.Now;

    [MaxLength(100)]
    public string? ResolvedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }

    [MaxLength(100)]
    public string? ManagerApprovedBy { get; set; }

    public DateTime? ManagerApprovedAt { get; set; }

    [ForeignKey(nameof(ThreePlInvoiceLineId))]
    public ThreePlInvoiceLine InvoiceLine { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }
}

public enum LaborActivityStatusEnum : byte
{
    Planned = 1,
    InProgress = 2,
    Completed = 3,
    Exception = 4,
    Cancelled = 5
}

public enum LaborExceptionStatusEnum : byte
{
    Open = 1,
    Approved = 2,
    Rejected = 3
}

[Table("LaborActivities")]
public class LaborActivity : IOwnerScoped
{
    [Key]
    public long LaborActivityId { get; set; }

    [Required, MaxLength(40)]
    public string ActivityCode { get; set; } = "";

    public int WarehouseId { get; set; }

    public int? ZoneId { get; set; }

    public int? UserId { get; set; }

    [Required, MaxLength(100)]
    public string UserName { get; set; } = "";

    [MaxLength(30)]
    public string ShiftCode { get; set; } = "";

    [Required, MaxLength(40)]
    public string TaskType { get; set; } = "";

    [Required, MaxLength(80)]
    public string TaskSourceType { get; set; } = "";

    [MaxLength(80)]
    public string? TaskSourceId { get; set; }

    [MaxLength(120)]
    public string? TaskSourceCode { get; set; }

    public int? OwnerPartnerId { get; set; }

    [MaxLength(50)]
    public string? ItemClass { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal WorkQuantity { get; set; } = 1m;

    [MaxLength(20)]
    public string UnitOfWork { get; set; } = "unit";

    public DateTime StartedAt { get; set; } = VietnamTime.Now;

    public DateTime? EndedAt { get; set; }

    public LaborActivityStatusEnum Status { get; set; } = LaborActivityStatusEnum.InProgress;

    [Column(TypeName = "decimal(18,4)")]
    public decimal ExpectedMinutes { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ActualMinutes { get; set; }

    [Column(TypeName = "decimal(9,4)")]
    public decimal ProductivityPercent { get; set; }

    public int WaitingMinutes { get; set; }

    public int BacklogAtStart { get; set; }

    public bool IsException { get; set; }

    [MaxLength(500)]
    public string? ExceptionReason { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(ZoneId))]
    public Zone? Zone { get; set; }

    [ForeignKey(nameof(UserId))]
    public AppUser? User { get; set; }

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    public ICollection<LaborExceptionReview> ExceptionReviews { get; set; } = new List<LaborExceptionReview>();
}

[Table("LaborExceptionReviews")]
public class LaborExceptionReview
{
    [Key]
    public long LaborExceptionReviewId { get; set; }

    public long LaborActivityId { get; set; }

    public LaborExceptionStatusEnum Status { get; set; } = LaborExceptionStatusEnum.Open;

    [MaxLength(500)]
    public string Reason { get; set; } = "";

    [Column(TypeName = "decimal(9,4)")]
    public decimal ProductivityBefore { get; set; }

    [Column(TypeName = "decimal(9,4)")]
    public decimal ProductivityAfter { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal IncentiveAmount { get; set; }

    [MaxLength(100)]
    public string RequestedBy { get; set; } = "system";

    public DateTime RequestedAt { get; set; } = VietnamTime.Now;

    [MaxLength(100)]
    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [MaxLength(500)]
    public string? ResolutionNotes { get; set; }

    [ForeignKey(nameof(LaborActivityId))]
    public LaborActivity LaborActivity { get; set; } = null!;
}
