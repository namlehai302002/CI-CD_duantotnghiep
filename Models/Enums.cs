namespace WMS.Models;

/// <summary>Loại phiếu kho (Voucher.VoucherType)</summary>
public enum VoucherTypeEnum : byte
{
    NhapKho = 1,
    XuatKho = 2,
    TraNCC = 3,
    KhachTra = 4,
    DieuChinh = 5,
    ChuyenKho = 6,
    NhapThanhPham = 7,
    XuatSanXuat = 8
}

/// <summary>Nguồn tạo phiếu (Voucher.SourceType)</summary>
public enum SourceTypeEnum : byte
{
    Manual = 1,
    Excel = 2,
    AI_Gemini = 3
}

/// <summary>Trạng thái thực hiện phiếu xuất (Voucher.FulfillmentStatus)</summary>
public enum FulfillmentStatusEnum : byte
{
    Draft = 1,
    WaitingForPick = 2,
    Picking = 3,
    Picked = 4,
    Completed = 5,
    PartiallyIssued = 6,
    Packed = 7,
    Shipped = 8
}

/// <summary>Kết quả kiểm hàng nhập (Voucher.ReviewResult)</summary>
public enum ReviewResultEnum : byte
{
    Undefined = 0,  // Sentinel value for EF Core default
    Pending = 1,
    Pass = 2,
    PassWithAdjustment = 3,
    Fail = 4
}

/// <summary>Trạng thái giữ hàng (StockReservation.Status)</summary>
public enum ReservationStatusEnum : byte
{
    Active = 1,
    Consumed = 2,
    Released = 3
}

/// <summary>Trạng thái nhiệm vụ lấy hàng (PickTask.Status)</summary>
public enum PickTaskStatusEnum : byte
{
    Pending = 1,
    Assigned = 2,
    InProgress = 3,
    Completed = 4,
    Short = 5,
    Cancelled = 6,
    WaitingForBulk = 7
}

/// <summary>Kiểu nhiệm vụ lấy hàng.</summary>
public enum PickTaskModeEnum : byte
{
    Single = 1,
    Bulk = 2,
    Sort = 3
}

/// <summary>Trạng thái đợt lấy hàng (Wave.Status)</summary>
public enum WaveStatusEnum : byte
{
    Created = 1,
    Released = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5
}

public enum ReplenishmentTriggerTypeEnum : byte
{
    Threshold = 1,
    Demand = 2,
    Forecast = 3,
    Hybrid = 4
}

public enum ReplenishmentRunStatusEnum : byte
{
    Started = 1,
    Completed = 2,
    PartiallyCompleted = 3,
    Failed = 4
}

public enum ReplenishmentAutomationLineStatusEnum : byte
{
    Planned = 1,
    TaskCreated = 2,
    Skipped = 3,
    Failed = 4
}

/// <summary>Trạng thái phiếu kiểm kê (StockCountSheet.Status)</summary>
public enum StockCountStatusEnum : byte
{
    Draft = 1,
    Counting = 2,
    Counted = 3,
    Approved = 4
}

/// <summary>Loại cảnh báo tồn kho (StockAlert.AlertType)</summary>
public enum AlertTypeEnum : byte
{
    LowStock = 1,
    OverStock = 2,
    Expiry = 3
}

/// <summary>Loại đối tác (Partner.PartnerType)</summary>
public enum PartnerTypeEnum : byte
{
    Supplier = 1,
    Customer = 2,
    Both = 3
}

/// <summary>Chế độ xuất kho (VoucherCreateViewModel.ExportMode)</summary>
public enum ExportModeEnum : byte
{
    Internal = 1,
    Sale = 2,
    Sample = 3,
    Warranty = 4,
    Disposal = 5,
    Transfer = 6
}

/// <summary>Trạng thái chất lượng dòng phiếu (VoucherDetail.QualityStatus)</summary>
public enum QualityStatusEnum : byte
{
    Good = 1,
    Defect = 2,
    Pending = 3,
    Inspecting = 4,
    Passed = 5,
    Failed = 6,
    Quarantine = 7,
    OnHold = 8
}

/// <summary>Kết quả xử lý hàng sau kiểm tra QC (QualityInspection.Disposition)</summary>
public enum QcDispositionEnum : byte
{
    /// <summary>Chấp nhận nhập kho bình thường</summary>
    Accept = 1,
    /// <summary>Từ chối — trả lại NCC</summary>
    Reject = 2,
    /// <summary>Yêu cầu sửa chữa / tái chế</summary>
    Rework = 3,
    /// <summary>Trả lại nhà cung cấp</summary>
    ReturnToSupplier = 4,
    /// <summary>Hủy bỏ / tiêu hủy</summary>
    Scrap = 5,
    /// <summary>Giữ lại để kiểm tra thêm</summary>
    Hold = 6,
    /// <summary>Chấp nhận sử dụng theo điều kiện (với giảm giá / ghi chú)</summary>
    AcceptWithConditions = 7
}

/// <summary>Trạng thái tồn kho theo chất lượng tại vị trí (ItemLocation quality hold)</summary>
public enum InventoryHoldStatusEnum : byte
{
    Available = 1,
    QcHold = 2,
    Quarantine = 3,
    Damaged = 4,
    Expired = 5,
    Blocked = 6,
    Consigned = 7
}

/// <summary>Core outbound allocation strategy.</summary>
public enum AllocationStrategyEnum : byte
{
    Fefo = 1,
    Fifo = 2,
    Lifo = 3
}

/// <summary>Loại vật tư (Item.ItemType)</summary>
public enum ItemTypeEnum : byte
{
    NguyenVatLieu = 1,
    ThanhPham = 2,
    BanThanhPham = 3,
    PhuTung = 4,
    BaoBi = 5,
    HoaChat = 6,
    HangMau = 7
}

/// <summary>Loại khu vực kho (Zone.ZoneType)</summary>
public enum ZoneTypeEnum : byte
{
    Storage = 1,
    Receiving = 2,
    Shipping = 3,
    Staging = 4,
    CrossDock = 5
}

/// <summary>Mã lý do hủy phiếu chuẩn hóa cho báo cáo phân tích</summary>
public enum CancelReasonEnum : byte
{
    InsufficientStock = 1,
    WrongInfo = 2,
    CustomerCancelled = 3,
    ItemDiscontinued = 4,
    DuplicateVoucher = 5,
    OperationalError = 6,
    Other = 99
}

/// <summary>Chiến lược put-away tự động khi nhập kho</summary>
public enum PutawayStrategyEnum : byte
{
    /// <summary>Ưu tiên ô mặc định của vật tư</summary>
    Default = 1,
    /// <summary>Tìm ô trống gần nhất cùng zone</summary>
    NearestEmpty = 2,
    /// <summary>Gộp vào ô đã có hàng đó (consolidate)</summary>
    Consolidate = 3
}

/// <summary>Trạng thái luồng nhập kho enterprise 5 bước</summary>
public enum InboundStatusEnum : byte
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Receiving = 4,
    Completed = 5,
    Rejected = 6
}

/// <summary>Hạng nhà cung cấp dùng để cấu hình tỷ lệ QC</summary>
public enum VendorRatingEnum : byte
{
    A = 1,
    B = 2,
    C = 3,
    New = 4
}

/// <summary>Trạng thái xử lý bất thường vận hành</summary>
public enum OperationExceptionStatusEnum : byte
{
    Open = 1,
    Acknowledged = 2,
    Resolved = 3
}

/// <summary>Trạng thái truy vết serial</summary>
public enum SerialNumberStatusEnum : byte
{
    Active = 1,
    Available = 1,
    Consumed = 2,
    Shipped = 2,
    Voided = 3,
    Allocated = 4,
    Picked = 5
}

/// <summary>Trang thai giu cho serial trong outbound pick/post lifecycle.</summary>
public enum SerialReservationStatusEnum : byte
{
    Reserved = 1,
    Picked = 2,
    Consumed = 3,
    Released = 4,
    Voided = 5
}

/// <summary>Trang thai operation idempotency nhe cho serial inventory.</summary>
public enum SerialInventoryOperationStatusEnum : byte
{
    Applied = 1,
    Failed = 2
}

/// <summary>Độ ưu tiên đợt lấy hàng (Wave.Priority)</summary>
/// <summary>Trạng thái vận hành của mã kiện/container (LPN).</summary>
public enum LpnStatusEnum : byte
{
    Created = 1,
    Stored = 2,
    Allocated = 3,
    Picked = 4,
    Packed = 5,
    Shipped = 6,
    Voided = 7
}

/// <summary>Loại container/LPN dùng cho pallet, thùng, tote và các biến thể đóng gói.</summary>
public enum LpnTypeEnum : byte
{
    Pallet = 1,
    Carton = 2,
    Tote = 3,
    Case = 4,
    Other = 99
}

/// <summary>Enterprise inventory ledger transaction taxonomy.</summary>
public enum InventoryTransactionTypeEnum : byte
{
    OpeningBalance = 1,
    Receive = 2,
    Putaway = 3,
    Move = 4,
    Pick = 5,
    Pack = 6,
    Ship = 7,
    Adjust = 8,
    Cancel = 9,
    TransferIn = 10,
    TransferOut = 11,
    Hold = 12,
    ReleaseHold = 13,
    Reconcile = 14,
    KitConsume = 15,
    KitProduce = 16,
    VasConsume = 17
}

public enum CatchWeightCapturePointEnum : byte
{
    Receive = 1,
    Putaway = 2,
    Pick = 3,
    Pack = 4,
    Ship = 5,
    Adjust = 6
}

public enum CatchWeightStatusEnum : byte
{
    Captured = 1,
    Voided = 2
}

public enum ShipmentLoadStatusEnum : byte
{
    Planned = 1,
    Staged = 2,
    Loading = 3,
    Loaded = 4,
    Departed = 5,
    Closed = 6,
    Cancelled = 7
}

public enum ThreePlChargeTypeEnum : byte
{
    Storage = 1,
    InboundHandling = 2,
    OutboundHandling = 3,
    Vas = 4,
    Yard = 5,
    PackageHandling = 6,
    ManualAdjustment = 7
}

public enum ThreePlBillingRunStatusEnum : byte
{
    Draft = 1,
    Confirmed = 2,
    Voided = 3
}

public enum ThreePlBillingChargeStatusEnum : byte
{
    Draft = 1,
    Confirmed = 2,
    Voided = 3
}

public enum MheSystemTypeEnum : byte
{
    Wcs = 1,
    Conveyor = 2,
    Sorter = 3,
    Robot = 4,
    Amr = 5,
    Other = 99
}

public enum MheCommandTypeEnum : byte
{
    MoveTote = 1,
    ReleaseWave = 2,
    DivertPackage = 3,
    RobotMission = 4,
    MoveLpn = 5,
    MoveInventory = 6
}

public enum MheCommandStatusEnum : byte
{
    Pending = 1,
    Queued = 2,
    Sent = 3,
    Acknowledged = 4,
    InProgress = 5,
    Completed = 6,
    Failed = 7,
    Cancelled = 8,
    DeadLetter = 9
}

public enum WavePriorityEnum : byte
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}

/// <summary>
/// Mức dịch vụ vận chuyển — ảnh hưởng đến SLA và thứ tự ưu tiên xử lý đơn hàng
/// Dùng trong Voucher.ServiceLevel để phân loại đơn hàng theo tốc độ giao hàng
/// </summary>
public enum ServiceLevelEnum : byte
{
    /// <summary>Tiêu chuẩn — giao trong 3-5 ngày làm việc</summary>
    Standard = 1,
    /// <summary>Nhanh — giao trong 1-2 ngày làm việc</summary>
    Express = 2,
    /// <summary>Hỏa tốc — giao trong ngày hoặc ngày hôm sau</summary>
    SameDay = 3,
    /// <summary>Giao trong khung giờ cố định (ví dụ: 8h-12h, 14h-18h)</summary>
    Scheduled = 4,
    /// <summary>Giao sau khi đặt hàng X ngày (ví dụ: PO chờ lắp ráp)</summary>
    PreOrder = 5
}

/// <summary>Trạng thái vận hành của lịch dock trên bảng điều phối thời gian thực.</summary>
public enum DockOperationStatusEnum : byte
{
    Scheduled = 1,
    Arrived = 2,
    Unloading = 3,
    Completed = 4,
    Delayed = 5
}
