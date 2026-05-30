using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("Partners")]
public class Partner
{
    [Key]
    public int PartnerId { get; set; }

    [Required, MaxLength(20)]
    public string PartnerCode { get; set; } = "";

    [Required, MaxLength(200)]
    public string PartnerName { get; set; } = "";

    public PartnerTypeEnum PartnerType { get; set; } = PartnerTypeEnum.Supplier;

    public bool IsThreePlClient { get; set; }

    [MaxLength(40)]
    public string? BillingAccountCode { get; set; }

    [MaxLength(10)]
    public string BillingCurrency { get; set; } = "VND";

    public bool RequireOwnerScopeIsolation { get; set; } = true;

    [MaxLength(20)]
    public string? TaxCode { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? ContactPerson { get; set; }

    // Đặc tả 6.2: Vendor Rating (A/B/C) — dùng cấu hình tỷ lệ QC
    public VendorRatingEnum VendorRating { get; set; } = VendorRatingEnum.New;

    // Đặc tả 6.2: Lead time (ngày) — thời gian giao hàng trung bình
    public int? LeadTimeDays { get; set; }

    /// <summary>Tỷ lệ kiểm QC (%) theo VendorRating. A=5%, B=20%, C=50%, New=100%.</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal QcSamplePercent { get; set; } = 100m;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public ICollection<AppUserOwnerScope> UserOwnerScopes { get; set; } = new List<AppUserOwnerScope>();

    public string PartnerTypeName => PartnerType switch
    {
        PartnerTypeEnum.Supplier => "Nhà Cung Cấp",
        PartnerTypeEnum.Customer => "Khách Hàng",
        PartnerTypeEnum.Both => "NCC + Khách Hàng",
        _ => "Khác"
    };
}
