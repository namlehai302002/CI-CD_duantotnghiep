using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

// ─── P2.1: Cross-Dock Execution Model ──────────────────────────────────────────

[Table("CrossDockTasks")]
public class CrossDockTask
{
    [Key]
    public long CrossDockTaskId { get; set; }

    /// <summary>Mã nhiệm vụ cross-dock</summary>
    [Required, MaxLength(30)]
    public string TaskCode { get; set; } = "";

    public long InboundVoucherId { get; set; }

    public long? InboundVoucherDetailId { get; set; }

    public long OutboundVoucherId { get; set; }

    public long? OutboundVoucherDetailId { get; set; }

    public long? StockReservationId { get; set; }

    public int ItemId { get; set; }

    /// <summary>Vị trí staging — nơi đặt hàng trung chuyển</summary>
    public int StageLocationId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ScheduledQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? ActualQty { get; set; }

    [MaxLength(50)]
    public string? LotNumber { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ExpiryDate { get; set; }

    public CrossDockTaskStatusEnum Status { get; set; } = CrossDockTaskStatusEnum.Pending;

    [MaxLength(200)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string AssignedTo { get; set; } = "";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? CompletedAt { get; set; }

    [ForeignKey(nameof(InboundVoucherId))]
    public Voucher? InboundVoucher { get; set; }

    [ForeignKey(nameof(InboundVoucherDetailId))]
    public VoucherDetail? InboundVoucherDetail { get; set; }

    [ForeignKey(nameof(OutboundVoucherId))]
    public Voucher? OutboundVoucher { get; set; }

    [ForeignKey(nameof(OutboundVoucherDetailId))]
    public VoucherDetail? OutboundVoucherDetail { get; set; }

    [ForeignKey(nameof(StockReservationId))]
    public StockReservation? StockReservation { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }

    [ForeignKey(nameof(StageLocationId))]
    public Location? StageLocation { get; set; }
}

public enum CrossDockTaskStatusEnum : byte
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    Short = 5 // không đủ hàng cross-dock
}

// ─── P2.2: Cycle Count Program ───────────────────────────────────────────────────

[Table("CycleCountPrograms")]
public class CycleCountProgram
{
    [Key]
    public int ProgramId { get; set; }

    [Required, MaxLength(100)]
    public string ProgramName { get; set; } = "";

    public int WarehouseId { get; set; }

    /// <summary>Tần suất đếm theo class A / B / C (ngày)</summary>
    public int FrequencyA { get; set; } = 30;   // items cao tốc → đếm thường xuyên
    public int FrequencyB { get; set; } = 90;
    public int FrequencyC { get; set; } = 180;   // items thấp → đếm ít

    /// <summary>Ngày chạy lần cuối</summary>
    public DateTime? LastRunAt { get; set; }

    /// <summary>Ngày chạy tiếp theo (tính toán từ LastRunAt + Frequency)</summary>
    public DateTime? NextRunAt { get; set; }

    /// <summary>Bao gồm blind count? (picker không thấy expected qty)</summary>
    public bool IsBlindCount { get; set; }

    /// <summary>Ngưỡng chênh lệch % để trigger recount</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal VarianceThresholdPct { get; set; } = 5.0m;

    public bool IsActive { get; set; } = true;

    [MaxLength(200)]
    public string CreatedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }
}

[Table("CycleCountSchedules")]
public class CycleCountSchedule
{
    [Key]
    public long ScheduleId { get; set; }

    public int ProgramId { get; set; }

    public int ItemId { get; set; }

    public int LocationId { get; set; }

    /// <summary>ABC velocity class dựa trên tần suất pick</summary>
    public char AbcClass { get; set; } = 'C'; // A, B, C

    /// <summary>Số lần đếm thực tế trong cycle</summary>
    public int CountAttempt { get; set; }

    /// <summary>Ngày đếm lần cuối</summary>
    public DateTime? LastCountedAt { get; set; }

    /// <summary>Ngày dự kiến đếm tiếp theo</summary>
    public DateTime? NextScheduledAt { get; set; }

    /// <summary>Tổng số variance (delta tuyệt đối)</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal? CumulativeVariance { get; set; }

    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(ProgramId))]
    public CycleCountProgram? Program { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }

    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }
}

// ─── P2.3: Recall Management ────────────────────────────────────────────────────

[Table("RecallCases")]
public class RecallCase
{
    [Key]
    public long RecallCaseId { get; set; }

    /// <summary>Mã recall: RCL-YYYYMMDD-NNNNN</summary>
    [Required, MaxLength(30)]
    public string CaseNumber { get; set; } = "";

    /// <summary>Lý do thu hồi</summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    public RecallSeverityEnum Severity { get; set; } = RecallSeverityEnum.Low;

    public RecallStatusEnum Status { get; set; } = RecallStatusEnum.Issued;

    /// <summary>Nhà cung cấp / nguồn hàng liên quan</summary>
    public int? SupplierId { get; set; }

    [MaxLength(200)]
    public string? IssuedBy { get; set; } = "";

    public DateTime IssuedAt { get; set; } = VietnamTime.Now;

    public DateTime? ResolvedAt { get; set; }

    [MaxLength(500)]
    public string? Resolution { get; set; }

    [MaxLength(200)]
    public string? ResolvedBy { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public Partner? Supplier { get; set; }

    public ICollection<RecallLine> Lines { get; set; } = new List<RecallLine>();
}

[Table("RecallLines")]
public class RecallLine
{
    [Key]
    public long RecallLineId { get; set; }

    public long RecallCaseId { get; set; }

    public int ItemId { get; set; }

    public int? OwnerPartnerId { get; set; }

    [MaxLength(50)]
    public string? LotNumber { get; set; }

    /// <summary>Số serial liên quan (comma-separated)</summary>
    public string? SerialNumbers { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AffectedQty { get; set; }

    /// <summary>Số lượng đã thu hồi</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal RecoveredQty { get; set; }

    public RecallDispositionEnum Disposition { get; set; } = RecallDispositionEnum.Quarantine;

    /// <summary>Trạng thái dòng: đang thu hồi / đã thu hồi / đã xử lý</summary>
    public RecallLineStatusEnum LineStatus { get; set; } = RecallLineStatusEnum.InProgress;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? CompletedAt { get; set; }

    [ForeignKey(nameof(RecallCaseId))]
    public RecallCase? RecallCase { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }
}

public enum RecallSeverityEnum : byte
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4 // thực phẩm / dược phẩm → cần recall ngay lập tức
}

public enum RecallStatusEnum : byte
{
    Issued = 1,
    InProgress = 2,
    PartiallyRecovered = 3,
    Resolved = 4,
    Cancelled = 5
}

public enum RecallDispositionEnum : byte
{
    Quarantine = 1,
    ReturnToSupplier = 2,
    Destroy = 3,
    Rework = 4,
    ReleaseUnderObservations = 5
}

public enum RecallLineStatusEnum : byte
{
    InProgress = 1,
    Recovered = 2,
    Dispositioned = 3
}

// ─── P2.4: Labor Standards (mở rộng từ model hiện có) ─────────────────────────

[Table("LaborActivityStandards")]
public class LaborActivityStandard
{
    [Key]
    public int StandardId { get; set; }

    /// <summary>Loại hoạt động: Picking, Packing, Receiving, Putaway, Counting</summary>
    [Required, MaxLength(50)]
    public string ActivityType { get; set; } = "";

    /// <summary>Đơn vị đo: minute, hour, cycle</summary>
    [MaxLength(20)]
    public string Unit { get; set; } = "minute";

    /// <summary>Thời gian chuẩn cho 1 đơn vị (VD: 0.5 phút/pick</summary>
    [Column(TypeName = "decimal(10,4)")]
    public decimal StandardMinutesPerUnit { get; set; }

    /// <summary>Thời gian di chuyển chuẩn giữa 2 vị trí (phút)</summary>
    [Column(TypeName = "decimal(10,4)")]
    public decimal TravelMinutesPerLocation { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string UpdatedBy { get; set; } = "";

    public DateTime UpdatedAt { get; set; } = VietnamTime.Now;
}

// ─── P3.2: OpenTelemetry Observability ─────────────────────────────────────────

/// <summary>
/// SLA metrics tracking cho từng voucher lifecycle
/// Dùng cho OpenTelemetry metrics: dock-to-stock time, pick SLA, ship SLA
/// </summary>
[Table("SlaMetrics")]
public class SlaMetric
{
    [Key]
    public long SlaMetricId { get; set; }

    public long VoucherId { get; set; }

    /// <summary>Loại SLA: DockToStock, PickSla, ShipSla, OrderToShip</summary>
    [Required, MaxLength(30)]
    public string SlaType { get; set; } = "";

    /// <summary>Mã SLA config (VD: SLA-EXPRESS-24H, SLA-STANDARD-72H)</summary>
    [MaxLength(30)]
    public string? SlaCode { get; set; }

    /// <summary>Thời gian mục tiêu (target) tính bằng phút</summary>
    public int TargetMinutes { get; set; }

    /// <summary>Thời gian thực tế tính bằng phút</summary>
    public int ActualMinutes { get; set; }

    /// <summary>Thời điểm bắt đầu đo SLA</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>Thời điểm kết thúc đo SLA</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Trạng thái: OnTrack / AtRisk / Breached</summary>
    public SlaStatusEnum Status { get; set; } = SlaStatusEnum.OnTrack;

    /// <summary>Độ vượt/quá SLA (phút dương=vượt, âm=chậm)</summary>
    public int VarianceMinutes { get; set; }

    [MaxLength(100)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [ForeignKey(nameof(VoucherId))]
    public Voucher? Voucher { get; set; }
}

public enum SlaStatusEnum : byte
{
    OnTrack = 1,   // Đúng tiến độ
    AtRisk = 2,    // Có nguy cơ trễ (< 20% thời gian còn lại)
    Breached = 3,  // Đã trễ SLA
    OnTime = 4     // Hoàn thành đúng hạn
}

// ─── P3.3: Capacity Simulation ─────────────────────────────────────────────────

/// <summary>
/// Kịch bản what-if cho capacity planning
/// VD: "Nếu volume tăng 30%, cần thêm bao nhiêu dock/labor?"
/// </summary>
[Table("CapacityScenarios")]
public class CapacityScenario
{
    [Key]
    public int ScenarioId { get; set; }

    [Required, MaxLength(100)]
    public string ScenarioName { get; set; } = "";

    [MaxLength(500)]
    public string? Description { get; set; }

    public int WarehouseId { get; set; }

    /// <summary>Ngày áp dụng kịch bản</summary>
    [Column(TypeName = "date")]
    public DateTime ScenarioDate { get; set; } = VietnamTime.Now.Date;

    // ── Input Parameters (các tham số đầu vào) ──
    /// <summary>Volume dự kiến (đơn hàng/ngày)</summary>
    public int DailyVolume { get; set; }

    /// <summary>Tăng trưởng so với baseline (%)</summary>
    public int VolumeGrowthPct { get; set; }

    /// <summary>Số giờ cao điểm trong ngày</summary>
    public int PeakHours { get; set; }

    /// <summary>Hệ số peak (VD: 1.5 = peak cao gấp 1.5 lần bình thường)</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal PeakFactor { get; set; } = 1.0m;

    /// <summary>Số dock door hiện có</summary>
    public int DockCount { get; set; }

    /// <summary>Số labor hiện có</summary>
    public int LaborCount { get; set; }

    /// <summary>Giờ làm việc/ngày</summary>
    public int WorkingHoursPerDay { get; set; } = 8;

    // ── Output Results (kết quả phân tích) ──
    /// <summary>JSON kết quả bottleneck analysis</summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? ResultJson { get; set; }

    /// <summary>Các bottleneck phát hiện được (comma-separated)</summary>
    [MaxLength(500)]
    public string? Bottlenecks { get; set; }

    /// <summary>Đề xuất: cần thêm bao nhiêu dock/labor</summary>
    [MaxLength(500)]
    public string? Recommendations { get; set; }

    /// <summary>Điểm nghẽn nghiêm trọng nhất</summary>
    [MaxLength(50)]
    public string? CriticalBottleneck { get; set; }

    /// <summary>Confidence score của simulation (0-100%)</summary>
    public int ConfidenceScore { get; set; } = 80;

    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string CreatedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }
}

// ─── P3.1: Slotting Optimization - ABC/XYZ Velocity Classification ─────────────────────────

/// <summary>
/// Phân loại velocity (tần suất di chuyển) của vật tư theo ABC và XYZ
/// A = High velocity (di chuyển thường xuyên) → đặt ở vị trí thuận tiện nhất
/// B = Medium velocity
/// C = Low velocity
/// X = High value (ABC analysis)
/// Y = Medium value
/// Z = Low value (slow moving)
/// Kết hợp: AX = hot item, CZ = dead stock
/// </summary>
[Table("ItemVelocityClassifications")]
public class ItemVelocityClassification
{
    [Key]
    public int ClassificationId { get; set; }

    public int ItemId { get; set; }

    public int WarehouseId { get; set; }

    /// <summary>ABC class: A (cao tốc), B (trung bình), C (chậm)</summary>
    public char AbcClass { get; set; } = 'C';

    /// <summary>XYZ class: X (đều), Y (biến động), Z (bất thường)</summary>
    public char XyzClass { get; set; } = 'Z';

    /// <summary>Tổ hợp: AX, AY, AZ, BX, BY, BZ, CX, CY, CZ</summary>
    [MaxLength(3)]
    public string CombinedClass { get; set; } = "CZ";

    /// <summary>Số lần pick trong kỳ phân tích</summary>
    public int PickCount { get; set; }

    /// <summary>Tổng số lượng pick trong kỳ</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalPickQty { get; set; }

    /// <summary>Biến động demand (coefficient of variation)</summary>
    [Column(TypeName = "decimal(10,4)")]
    public decimal DemandVariability { get; set; }

    /// <summary>Pick frequency (lần/ngày trung bình)</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal DailyPickFrequency { get; set; }

    /// <summary>Vị trí hiện tại được gán</summary>
    public int? CurrentLocationId { get; set; }

    /// <summary>Vị trí được đề xuất (sau khi re-slotting)</summary>
    public int? SuggestedLocationId { get; set; }

    /// <summary>Vị trí pick-face được đề xuất</summary>
    public int? SuggestedPickFaceLocationId { get; set; }

    /// <summary>Vị trí bulk được đề xuất</summary>
    public int? SuggestedBulkLocationId { get; set; }

    /// <summary>Điểm số slotting (0-100)</summary>
    public int SlottingScore { get; set; }

    /// <summary>Ghi chú / lý do thay đổi</summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>Ngày phân tích cuối</summary>
    public DateTime LastAnalyzedAt { get; set; } = VietnamTime.Now;

    /// <summary>Kỳ phân tích (ngày)</summary>
    public int AnalysisPeriodDays { get; set; } = 90;

    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey(nameof(CurrentLocationId))]
    public Location? CurrentLocation { get; set; }

    [ForeignKey(nameof(SuggestedLocationId))]
    public Location? SuggestedLocation { get; set; }

    [ForeignKey(nameof(SuggestedPickFaceLocationId))]
    public Location? SuggestedPickFaceLocation { get; set; }

    [ForeignKey(nameof(SuggestedBulkLocationId))]
    public Location? SuggestedBulkLocation { get; set; }
}
