using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

/// <summary>
/// Bản ghi kiểm tra chất lượng (QC) cho phiếu nhập kho.
/// Tương đương Oracle WMS Quality Inspection / SAP QM Inspection Lot.
/// </summary>
[Table("QualityInspections")]
public class QualityInspection
{
    [Key]
    public long QualityInspectionId { get; set; }

    public long VoucherId { get; set; }

    public long? VoucherDetailId { get; set; }

    public int ItemId { get; set; }

    public int WarehouseId { get; set; }

    /// <summary>Số lượng tổng cần kiểm tra (từ VoucherDetail.BaseQty)</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalQty { get; set; }

    /// <summary>Số lượng mẫu kiểm tra (dựa trên Partner.QcSamplePercent)</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal SampleQty { get; set; }

    /// <summary>Số lượng đạt kiểm tra</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal PassedQty { get; set; }

    /// <summary>Số lượng không đạt</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal FailedQty { get; set; }

    /// <summary>Tỷ lệ kiểm tra áp dụng (%)</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal SamplePercent { get; set; }

    /// <summary>Kết quả xử lý</summary>
    public QcDispositionEnum Disposition { get; set; } = QcDispositionEnum.Accept;

    /// <summary>Trạng thái QC tổng thể</summary>
    public QualityStatusEnum OverallResult { get; set; } = QualityStatusEnum.Pending;

    [MaxLength(100)]
    public string? InspectorName { get; set; }

    public DateTime? InspectedAt { get; set; }

    [MaxLength(500)]
    public string? DefectDescription { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>Mã lô kiểm tra (tham chiếu)</summary>
    [MaxLength(50)]
    public string? LotNumber { get; set; }

    /// <summary>URL/đường dẫn chứng chỉ đính kèm (Certificate of Analysis)</summary>
    [MaxLength(500)]
    public string? CertificateUrl { get; set; }

    /// <summary>Tên kế hoạch kiểm tra áp dụng (inspection plan template)</summary>
    [MaxLength(100)]
    public string? InspectionPlanName { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [ForeignKey(nameof(VoucherId))]
    public Voucher? Voucher { get; set; }

    [ForeignKey(nameof(VoucherDetailId))]
    public VoucherDetail? VoucherDetail { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    [NotMapped]
    public decimal DefectRate => SampleQty > 0 ? (FailedQty / SampleQty) * 100m : 0;

    [NotMapped]
    public string DispositionName => Disposition switch
    {
        QcDispositionEnum.Accept => "Chấp nhận",
        QcDispositionEnum.Reject => "Từ chối",
        QcDispositionEnum.Rework => "Sửa chữa",
        QcDispositionEnum.ReturnToSupplier => "Trả NCC",
        QcDispositionEnum.Scrap => "Tiêu hủy",
        QcDispositionEnum.Hold => "Tạm giữ",
        QcDispositionEnum.AcceptWithConditions => "Dùng có điều kiện",
        _ => "Khác"
    };

    [NotMapped]
    public string ResultName => OverallResult switch
    {
        QualityStatusEnum.Pending => "Chờ kiểm",
        QualityStatusEnum.Inspecting => "Đang kiểm",
        QualityStatusEnum.Passed => "Đạt",
        QualityStatusEnum.Failed => "Không đạt",
        QualityStatusEnum.Quarantine => "Cách ly",
        QualityStatusEnum.OnHold => "Tạm giữ",
        _ => "Khác"
    };

    [NotMapped]
    public string ResultBadgeClass => OverallResult switch
    {
        QualityStatusEnum.Pending => "badge-secondary",
        QualityStatusEnum.Inspecting => "badge-info",
        QualityStatusEnum.Passed => "badge-success",
        QualityStatusEnum.Failed => "badge-danger",
        QualityStatusEnum.Quarantine => "badge-warning",
        QualityStatusEnum.OnHold => "badge-warning",
        _ => "badge-secondary"
    };
}
