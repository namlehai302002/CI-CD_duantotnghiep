using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("ThreePlBillingRates")]
public class ThreePlBillingRate
{
    [Key]
    public long ThreePlBillingRateId { get; set; }

    public int WarehouseId { get; set; }

    public int OwnerPartnerId { get; set; }

    public ThreePlChargeTypeEnum ChargeType { get; set; }

    [MaxLength(80)]
    public string RateCode { get; set; } = "";

    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitRate { get; set; }

    [MaxLength(20)]
    public string ChargeUnit { get; set; } = "unit";

    [MaxLength(10)]
    public string Currency { get; set; } = "VND";

    public DateTime EffectiveFrom { get; set; } = VietnamTime.Now.Date;

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(300)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner OwnerPartner { get; set; } = null!;
}

[Table("ThreePlBillingRuns")]
public class ThreePlBillingRun
{
    [Key]
    public long ThreePlBillingRunId { get; set; }

    [Required, MaxLength(40)]
    public string RunCode { get; set; } = "";

    public int WarehouseId { get; set; }

    public int OwnerPartnerId { get; set; }

    [Column(TypeName = "date")]
    public DateTime PeriodFrom { get; set; }

    [Column(TypeName = "date")]
    public DateTime PeriodTo { get; set; }

    public ThreePlBillingRunStatusEnum Status { get; set; } = ThreePlBillingRunStatusEnum.Draft;

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalAmount { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "VND";

    [Required, MaxLength(160)]
    public string IdempotencyKey { get; set; } = "";

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [MaxLength(100)]
    public string? ConfirmedBy { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    [MaxLength(100)]
    public string? VoidedBy { get; set; }

    public DateTime? VoidedAt { get; set; }

    [MaxLength(300)]
    public string? VoidReason { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner OwnerPartner { get; set; } = null!;

    public ICollection<ThreePlBillingCharge> Charges { get; set; } = new List<ThreePlBillingCharge>();
}

[Table("ThreePlBillingCharges")]
public class ThreePlBillingCharge
{
    [Key]
    public long ThreePlBillingChargeId { get; set; }

    public long ThreePlBillingRunId { get; set; }

    public int WarehouseId { get; set; }

    public int OwnerPartnerId { get; set; }

    public long? ThreePlBillingRateId { get; set; }

    public ThreePlChargeTypeEnum ChargeType { get; set; }

    [MaxLength(80)]
    public string SourceType { get; set; } = "";

    [MaxLength(80)]
    public string? SourceId { get; set; }

    [MaxLength(120)]
    public string? SourceCode { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    [MaxLength(20)]
    public string ChargeUnit { get; set; } = "unit";

    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitRate { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Amount { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "VND";

    public ThreePlBillingChargeStatusEnum Status { get; set; } = ThreePlBillingChargeStatusEnum.Draft;

    [Required, MaxLength(180)]
    public string IdempotencyKey { get; set; } = "";

    public string MetadataJson { get; set; } = "{}";

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(ThreePlBillingRunId))]
    public ThreePlBillingRun BillingRun { get; set; } = null!;

    [ForeignKey(nameof(ThreePlBillingRateId))]
    public ThreePlBillingRate? BillingRate { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner OwnerPartner { get; set; } = null!;
}
