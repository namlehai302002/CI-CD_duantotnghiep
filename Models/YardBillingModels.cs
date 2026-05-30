using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

// P2-03B: Yard Billing - Detention / Demurrage

/// <summary>Bang gia phi luu bai theo doi tac/carrier/loai xe/loai o.</summary>
[Table("YardBillingRates")]
public class YardBillingRate
{
    [Key]
    public int YardBillingRateId { get; set; }

    public int WarehouseId { get; set; }

    /// <summary>Khach hang/doi tac. Null = ap dung moi doi tac.</summary>
    public int? PartnerId { get; set; }

    /// <summary>Carrier name. Null means the rate applies to every carrier.</summary>
    [MaxLength(100)]
    public string? CarrierName { get; set; }

    /// <summary>Loai xe. Null = ap dung moi loai.</summary>
    public TrailerTypeEnum? TrailerType { get; set; }

    /// <summary>Loai o bai. Null = ap dung moi loai o.</summary>
    public YardSpotTypeEnum? SpotType { get; set; }

    /// <summary>Thoi gian mien phi (phut). VD: 2880 = 48 gio.</summary>
    public int FreeTimeMinutes { get; set; } = 2880;

    /// <summary>Don gia moi gio vuot qua free time.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal RatePerHour { get; set; }

    /// <summary>Tran phi toi da moi ngay. 0 = khong gioi han.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal MaxDailyRate { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "VND";

    public bool IsActive { get; set; } = true;

    [MaxLength(300)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(PartnerId))]
    public Partner? Partner { get; set; }
}

/// <summary>Dong phi luu bai da sinh cho mot luot xe vao bai.</summary>
[Table("YardBillingCharges")]
public class YardBillingCharge : IOwnerScoped
{
    [Key]
    public long YardBillingChargeId { get; set; }

    public long YardVisitId { get; set; }

    public int? YardBillingRateId { get; set; }

    public int WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    /// <summary>Tong thoi gian luu bai (phut).</summary>
    public int TotalDwellMinutes { get; set; }

    /// <summary>Thoi gian mien phi ap dung (phut).</summary>
    public int FreeTimeMinutes { get; set; }

    /// <summary>Thoi gian tinh phi = max(0, TotalDwell - FreeTime).</summary>
    public int ChargeableMinutes { get; set; }

    /// <summary>Don gia moi gio tai thoi diem tinh.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal AppliedRatePerHour { get; set; }

    /// <summary>Tong phi.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "VND";

    public YardChargeStatusEnum Status { get; set; } = YardChargeStatusEnum.Draft;

    [MaxLength(300)]
    public string? WaivedReason { get; set; }

    [MaxLength(100)]
    public string? WaivedBy { get; set; }

    public DateTime? WaivedAt { get; set; }

    [MaxLength(100)]
    public string? ConfirmedBy { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(YardVisitId))]
    public YardVisit YardVisit { get; set; } = null!;

    [ForeignKey(nameof(YardBillingRateId))]
    public YardBillingRate? BillingRate { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }
}

public enum YardChargeStatusEnum : byte
{
    Draft = 1,
    Confirmed = 2,
    Invoiced = 3,
    Waived = 4
}
