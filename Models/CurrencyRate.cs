using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

/// <summary>
/// Tỷ giá chuyển đổi tiền tệ — Enterprise Multi-currency Support.
/// Dùng khi phiếu nhập/xuất có CurrencyCode ≠ VND.
/// </summary>
[Table("CurrencyRates")]
public class CurrencyRate
{
    [Key]
    public int CurrencyRateId { get; set; }

    /// <summary>Mã tiền tệ nguồn (ISO 4217): USD, EUR, JPY...</summary>
    [Required, MaxLength(3)]
    public string FromCurrency { get; set; } = "";

    /// <summary>Mã tiền tệ đích (ISO 4217): VND</summary>
    [Required, MaxLength(3)]
    public string ToCurrency { get; set; } = "VND";

    /// <summary>Tỷ giá: 1 FromCurrency = Rate × ToCurrency</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal Rate { get; set; }

    /// <summary>Ngày hiệu lực</summary>
    [Column(TypeName = "date")]
    public DateTime EffectiveDate { get; set; } = VietnamTime.Now.Date;

    [MaxLength(200)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }
}
