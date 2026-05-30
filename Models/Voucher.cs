using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using WMS.Common;

namespace WMS.Models;

[Table("Vouchers")]
public class Voucher : IOwnerScoped
{
    [Key]
    public long VoucherId { get; set; }

    [Required, MaxLength(30)]
    public string VoucherCode { get; set; } = "";

    public VoucherTypeEnum VoucherType { get; set; }

    [Column(TypeName = "date")]
    public DateTime VoucherDate { get; set; } = VietnamTime.Now.Date;

    public int WarehouseId { get; set; }

    public int? DestWarehouseId { get; set; }

    public int? PartnerId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public SourceTypeEnum SourceType { get; set; } = SourceTypeEnum.Manual;

    [MaxLength(50)]
    public string? ReferenceNo { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalAmount { get; set; }

    /// <summary>Mã tiền tệ (ISO 4217) — VND, USD, EUR. Mặc định VND.</summary>
    [MaxLength(3)]
    public string CurrencyCode { get; set; } = "VND";

    public int TotalLines { get; set; }

    public bool IsPosted { get; set; } = false;

    public bool IsCancelled { get; set; }

    [MaxLength(100)]
    public string? CancelledBy { get; set; }

    public DateTime? CancelledAt { get; set; }

    [MaxLength(500)]
    public string? CancelReason { get; set; }

    public CancelReasonEnum? CancelReasonCode { get; set; }

    [Required, MaxLength(100)]
    public string CreatedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    public long? AiOcrLogId { get; set; }

    public long? ParentVoucherId { get; set; }

    public long? WaveId { get; set; }

    public FulfillmentStatusEnum FulfillmentStatus { get; set; } = FulfillmentStatusEnum.Draft;

    // ═══ P2.5: ADVANCED ALLOCATION - Service Level & Priority ═══
    /// <summary>
    /// Mức dịch vụ vận chuyển: Standard / Express / SameDay / Scheduled / PreOrder
    /// Ảnh hưởng đến SLA và thứ tự ưu tiên xử lý đơn hàng trong wave
    /// </summary>
    public ServiceLevelEnum ServiceLevel { get; set; } = ServiceLevelEnum.Standard;

    /// <summary>
    /// Độ ưu tiên đơn hàng — số càng lớn ưu tiên càng cao
    /// Mặc định 50, range 1-100. VD: 80=VIP, 90=Critical, 100=Emergency
    /// </summary>
    public int Priority { get; set; } = 50;

    /// <summary>
    /// Cho phép giao thiếu hàng (partial shipment)?
    /// Nếu true: khi không đủ tồn sẽ giao số có sẵn và tạo backorder cho phần còn thiếu
    /// Nếu false: bắt buộc đủ hàng mới được xuất (hold đơn)
    /// </summary>
    public bool PartialShipmentAllowed { get; set; } = false;

    /// <summary>
    /// Mã SLA deadline — dùng để tracking và báo cáo
    /// </summary>
    [MaxLength(30)]
    public string? SlaCode { get; set; }

    /// <summary>
    /// Thời hạn SLA tính bằng giờ (VD: 24=1 ngày, 48=2 ngày)
    /// </summary>
    public int? SlaHours { get; set; }

    /// <summary>
    /// Ngày KH / bộ phận cần nhận hàng — dùng để tính SLA</summary>
    [Column(TypeName = "date")]
    public DateTime? RequestedDeliveryDate { get; set; }

    [MaxLength(100)]
    public string? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(1000)]
    public string? ReviewNote { get; set; }

    public ReviewResultEnum ReviewResult { get; set; } = ReviewResultEnum.Pending;

    [Column(TypeName = "decimal(5,2)")]
    public decimal ResponsibilityScore { get; set; } = 0;

    // ═══ ENTERPRISE INBOUND 5-STEP WORKFLOW ═══
    public InboundStatusEnum InboundStatus { get; set; } = InboundStatusEnum.Draft;

    [MaxLength(100)]
    public string? SubmittedBy { get; set; }
    public DateTime? SubmittedAt { get; set; }

    [MaxLength(100)]
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    [MaxLength(100)]
    public string? ReceivedBy { get; set; }
    public DateTime? ReceivedAt { get; set; }

    [MaxLength(100)]
    public string? CompletedBy { get; set; }
    public DateTime? CompletedAt { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    // ═══ OUTBOUND: PACKING (đặc tả OUT-04) ═══
    [MaxLength(100)]
    public string? PackedBy { get; set; }
    public DateTime? PackedAt { get; set; }

    // ═══ OUTBOUND: SHIPPING (đặc tả OUT-05) ═══
    [MaxLength(100)]
    public string? ShippedBy { get; set; }
    public DateTime? ShippedAt { get; set; }

    [MaxLength(100)]
    public string? TrackingNumber { get; set; }

    [MaxLength(50)]
    public string? ManifestCode { get; set; }

    // ═══ DOCK MANAGEMENT (đặc tả INB-06) ═══
    [MaxLength(30)]
    public string? AsnCode { get; set; }

    public DateTime? ExpectedArrivalAt { get; set; }

    [MaxLength(100)]
    public string? CarrierName { get; set; }

    [MaxLength(30)]
    public string? VehicleNumber { get; set; }

    [MaxLength(100)]
    public string? DriverName { get; set; }

    [MaxLength(20)]
    public string? DriverPhone { get; set; }

    public DateTime? DockAppointmentStart { get; set; }

    public DateTime? DockAppointmentEnd { get; set; }

    [MaxLength(20)]
    public string? DockDoor { get; set; } // Cầu dock tiếp nhận hàng

    public DockOperationStatusEnum DockStatus { get; set; } = DockOperationStatusEnum.Scheduled;

    public DateTime? GateInAt { get; set; }

    public DateTime? DockArrivalAt { get; set; }

    public DateTime? UnloadStartAt { get; set; }

    public DateTime? UnloadEndAt { get; set; }

    public DateTime? DockCompletedAt { get; set; }

    [ForeignKey("WarehouseId")]
    public Warehouse Warehouse { get; set; } = null!;

    [ForeignKey("DestWarehouseId")]
    public Warehouse? DestWarehouse { get; set; }

    [ForeignKey("PartnerId")]
    public Partner? Partner { get; set; }

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    public ICollection<VoucherDetail> Details { get; set; } = new List<VoucherDetail>();
    public ICollection<OutboundPackage> Packages { get; set; } = new List<OutboundPackage>();

    [ForeignKey(nameof(ParentVoucherId))]
    public Voucher? ParentVoucher { get; set; }

    public ICollection<Voucher> ChildVouchers { get; set; } = new List<Voucher>();

    [ForeignKey(nameof(WaveId))]
    public Wave? Wave { get; set; }

    public string VoucherTypeName => VoucherType switch
    {
        VoucherTypeEnum.NhapKho => "Nhập kho",
        VoucherTypeEnum.XuatKho => "Xuất kho",
        VoucherTypeEnum.TraNCC => "Trả NCC",
        VoucherTypeEnum.KhachTra => "Khách trả",
        VoucherTypeEnum.DieuChinh => "Điều chỉnh",
        VoucherTypeEnum.ChuyenKho => "Chuyển kho",
        VoucherTypeEnum.NhapThanhPham => "Nhập thành phẩm",
        VoucherTypeEnum.XuatSanXuat => "Xuất sản xuất",
        _ => "Khác"
    };

    public string SourceTypeName => SourceType switch
    {
        SourceTypeEnum.Manual => "Thủ công",
        SourceTypeEnum.Excel => "Excel",
        SourceTypeEnum.AI_Gemini => "AI Gemini",
        _ => "Khác"
    };

    [NotMapped]
    public bool IsPartial => Details != null && Details.Any(d => d.DefectQty > 0);

    [NotMapped]
    public bool IsOverdue => RequestedDeliveryDate.HasValue
        && RequestedDeliveryDate.Value.Date < VietnamTime.Now.Date
        && !IsPosted && !IsCancelled
        && VoucherType is VoucherTypeEnum.XuatKho or VoucherTypeEnum.TraNCC or VoucherTypeEnum.ChuyenKho or VoucherTypeEnum.XuatSanXuat;

    [NotMapped]
    public bool IsOutboundPartialIssued => VoucherType is VoucherTypeEnum.XuatKho or VoucherTypeEnum.TraNCC or VoucherTypeEnum.ChuyenKho or VoucherTypeEnum.XuatSanXuat
        && FulfillmentStatus == FulfillmentStatusEnum.PartiallyIssued;

    [NotMapped]
    public bool IsInboundFlow => VoucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham;

    public string StatusDisplay => IsCancelled
        ? "Đã hủy"
        : IsOutboundPartialIssued
            ? "Xuất một phần"
            : IsInboundFlow
                ? InboundStatus switch
                {
                    InboundStatusEnum.Draft => "Nháp",
                    InboundStatusEnum.PendingApproval => "Chờ duyệt",
                    InboundStatusEnum.Approved => "Đã duyệt",
                    InboundStatusEnum.Receiving => "Đang nhận hàng",
                    InboundStatusEnum.Completed => IsPartial ? "Hoàn tất (thiếu)" : "Hoàn tất",
                    InboundStatusEnum.Rejected => "Từ chối",
                    _ => "Nháp"
                }
            : IsPosted
                ? (IsPartial ? "Còn thiếu" : "Đã ghi sổ")
                : FulfillmentStatus switch
                {
                    FulfillmentStatusEnum.WaitingForPick => "Chờ lấy hàng",
                    FulfillmentStatusEnum.Picking => "Đang lấy hàng",
                    FulfillmentStatusEnum.Picked => "Đã lấy hàng",
                    FulfillmentStatusEnum.Packed => "Đã đóng gói",
                    FulfillmentStatusEnum.Shipped => "Đã giao",
                    _ => "Nháp"
                };

    public string StatusBadgeClass => IsCancelled
        ? "badge-danger"
        : IsOutboundPartialIssued
            ? "badge-warning"
            : IsInboundFlow
                ? InboundStatus switch
                {
                    InboundStatusEnum.Draft => "badge-secondary",
                    InboundStatusEnum.PendingApproval => "badge-info",
                    InboundStatusEnum.Approved => "badge-accent",
                    InboundStatusEnum.Receiving => "badge-warning",
                    InboundStatusEnum.Completed => IsPartial ? "badge-warning" : "badge-success",
                    InboundStatusEnum.Rejected => "badge-danger",
                    _ => "badge-secondary"
                }
            : IsPosted
                ? (IsPartial ? "badge-warning" : "badge-success")
                : FulfillmentStatus switch
                {
                    FulfillmentStatusEnum.WaitingForPick => "badge-info",
                    FulfillmentStatusEnum.Picking => "badge-warning",
                    FulfillmentStatusEnum.Picked => "badge-accent",
                    FulfillmentStatusEnum.Packed => "badge-info",
                    FulfillmentStatusEnum.Shipped => "badge-success",
                    _ => "badge-secondary"
                };
}
