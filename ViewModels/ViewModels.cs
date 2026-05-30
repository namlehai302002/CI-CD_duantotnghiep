using System.ComponentModel.DataAnnotations;
using WMS.Common;
using WMS.Models;
using WMS.Services;

namespace WMS.ViewModels;

public class DashboardViewModel
{
    public int TotalItems { get; set; }
    public int TotalWarehouses { get; set; }
    public int TotalPartners { get; set; }
    public int TodayVouchers { get; set; }
    public decimal TotalStockValue { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int OverStockCount { get; set; }
    public List<Item> LowStockItems { get; set; } = new();
    public List<Voucher> RecentVouchers { get; set; } = new();
    public List<StockAlert> UnresolvedAlerts { get; set; } = new();
    public Dictionary<string, int> VouchersByType { get; set; } = new();
    public Dictionary<string, decimal> StockByCategory { get; set; } = new();
    public int OpenWaves { get; set; }
    public int OpenPickTasks { get; set; }
    public int ShortPickTasks { get; set; }
    public decimal ReservationFillRate { get; set; }
    public int PendingOutboundVouchers { get; set; }
    public int PendingInboundApprovals { get; set; }
    public int StalePickTasks { get; set; }
    public int UnassignedPickTasks { get; set; }
    public int OverdueVouchers { get; set; }
}

public class LoginViewModel
{
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

public class VerifyMfaViewModel
{
    public int ChallengeId { get; set; }
    public string UserName { get; set; } = "";
    public string MaskedEmail { get; set; } = "";
    public string CaptchaCode { get; set; } = "";
    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

public class VoucherCreateViewModel
{
    public VoucherTypeEnum VoucherType { get; set; }
    public int WarehouseId { get; set; }
    public int? DestWarehouseId { get; set; }

    public int? PartnerId { get; set; }
    public int? OwnerPartnerId { get; set; }

    public string? ReferenceNo { get; set; }
    public string? Description { get; set; }
    public long? ParentVoucherId { get; set; }
    public List<VoucherDetailLine> Lines { get; set; } = new();
    public ExportModeEnum ExportMode { get; set; } = ExportModeEnum.Internal;

    // P2.5: Advanced allocation fields
    /// <summary>Mức dịch vụ: Standard / Express / SameDay / Scheduled / PreOrder</summary>
    public ServiceLevelEnum ServiceLevel { get; set; } = ServiceLevelEnum.Standard;

    /// <summary>Độ ưu tiên đơn hàng (1-100, mặc định 50)</summary>
    public int Priority { get; set; } = 50;

    /// <summary>Cho phép giao thiếu hàng (partial shipment)?</summary>
    public bool PartialShipmentAllowed { get; set; } = false;

    /// <summary>Mã SLA (VD: SLA-EXPRESS-24H)</summary>
    public string? SlaCode { get; set; }

    /// <summary>Thời hạn SLA (giờ)</summary>
    public int? SlaHours { get; set; }

    public List<Warehouse> Warehouses { get; set; } = new();
    public List<Partner> Partners { get; set; } = new();
    public List<Partner> OwnerPartners { get; set; } = new();
    public List<Item> Items { get; set; } = new();
    public List<UnitOfMeasure> Uoms { get; set; } = new();
    public List<Location> Locations { get; set; } = new();
    public List<PackagingUnit> PackagingUnits { get; set; } = new();
    public DateTime? RequestedDeliveryDate { get; set; }
    public DateTime? ExpectedArrivalAt { get; set; }
    public string? CarrierName { get; set; }
    public string? VehicleNumber { get; set; }
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }
    public DateTime? DockAppointmentStart { get; set; }
    public DateTime? DockAppointmentEnd { get; set; }
    public string? DockDoor { get; set; }
}

public class VoucherDetailLine
{
    public int ItemId { get; set; }
    public int? LocationId { get; set; }
    public int? DestLocationId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public decimal TransactionQty { get; set; }

    public decimal DestQty { get; set; }
    public decimal DefectQty { get; set; }
    public int TransactionUomId { get; set; }
    public int? PackagingUnitId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public QualityStatusEnum QualityStatus { get; set; } = QualityStatusEnum.Good;
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public string? LotNumber { get; set; }
    public string? Notes { get; set; }

    // Used for VoucherType = 5 (Điều chỉnh). 1 = Tăng, -1 = Giảm
    public sbyte AdjustSign { get; set; } = 1;
}

public class ItemFormViewModel
{
    public Item Item { get; set; } = new();
    public List<ItemCategory> Categories { get; set; } = new();
    public List<UnitOfMeasure> Uoms { get; set; } = new();
    public List<Location> Locations { get; set; } = new();
}

public class ReportFilterViewModel
{
    public int? ItemId { get; set; }
    public int? WarehouseId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public List<Item> Items { get; set; } = new();
    public List<Warehouse> Warehouses { get; set; } = new();
}

public class StockSnapshotCompareRow
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string UomCode { get; set; } = "";
    public decimal SnapshotQty { get; set; }
    public decimal CurrentQty { get; set; }
    public decimal DiffQty { get; set; }
    public decimal UnitCost { get; set; }
    public decimal SnapshotValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal DiffValue { get; set; }
}

public class StockValuationPageViewModel
{
    public int? WarehouseId { get; set; }
    public int? CategoryId { get; set; }
    public string? ItemSearch { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? SnapshotDate { get; set; }
    public string Mode { get; set; } = "current";
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<ItemCategory> Categories { get; set; } = new();
    public List<StockValuationRow> Rows { get; set; } = new();
    public bool IsSnapshotMode => string.Equals(Mode, "snapshot", StringComparison.OrdinalIgnoreCase);
    public bool MissingSnapshot { get; set; }
    public string? Notice { get; set; }
    public int TotalItemCount => Rows.Select(r => r.ItemId).Distinct().Count();
    public decimal TotalQuantity => Rows.Sum(r => r.Quantity);
    public decimal TotalReservedQty => Rows.Sum(r => r.ReservedQty);
    public decimal TotalAvailableQty => Rows.Sum(r => r.AvailableQty);
    public decimal TotalValue => Rows.Sum(r => r.StockValue);
    public Dictionary<string, decimal> ValueByWarehouse => Rows
        .GroupBy(r => r.WarehouseName)
        .OrderBy(g => g.Key)
        .ToDictionary(g => g.Key, g => g.Sum(r => r.StockValue));
    public Dictionary<string, decimal> ValueByCategory => Rows
        .GroupBy(r => string.IsNullOrWhiteSpace(r.CategoryName) ? "Chưa phân loại" : r.CategoryName)
        .OrderBy(g => g.Key)
        .ToDictionary(g => g.Key, g => g.Sum(r => r.StockValue));
}

public class StockValuationRow
{
    public int ItemId { get; set; }
    public string WarehouseCode { get; set; } = "";
    public string WarehouseName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string UomCode { get; set; } = "";
    public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public InventoryHoldStatusEnum? HoldStatus { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReservedQty { get; set; }
    public decimal AvailableQty { get; set; }
    public decimal UnitCost { get; set; }
    public decimal StockValue { get; set; }
}

public class StockCountLineInput
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public int LocationId { get; set; }
    public string LocationCode { get; set; } = "";
    public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal SystemQty { get; set; }
    public decimal CountedQty { get; set; }
    /// <summary>Chênh lệch = CountedQty - SystemQty</summary>
    public decimal DiffQty => CountedQty - SystemQty;
}

public class StockCountPageViewModel
{
    public int? WarehouseId { get; set; }
    public DateTime CountDate { get; set; } = VietnamTime.Now.Date;
    public string? Notes { get; set; }
    /// <summary>Kiểm kê mù — ẩn SL hệ thống, nhân viên đếm không biết tồn kho lý thuyết</summary>
    public bool IsBlindCount { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<StockCountLineInput> Lines { get; set; } = new();
    public List<StockCountSheetSummary> ExistingSheets { get; set; } = new();
}

public class StockCountSheetSummary
{
    public long StockCountSheetId { get; set; }
    public DateTime CountDate { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public StockCountStatusEnum Status { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalReason { get; set; }
    public string? UnlockedBy { get; set; }
    public DateTime? UnlockedAt { get; set; }
    public string? UnlockReason { get; set; }
    public int TotalLines { get; set; }
    public int DiffLines { get; set; }
    public string? VoucherCode { get; set; }
}

public class WaveBoardRow
{
    public long? WaveId { get; set; }
    public string WaveCode { get; set; } = "";
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public WaveStatusEnum Status { get; set; }
    public int OpenTasks { get; set; }
    public int DoneTasks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class PickTaskBoardRow
{
    public long PickTaskId { get; set; }
    public string TaskCode { get; set; } = "";
    public long? WaveId { get; set; }
    public string WaveCode { get; set; } = "";
    public long VoucherId { get; set; }
    public string VoucherCode { get; set; } = "";
    public string ItemCode { get; set; } = "";
    public string? ItemName { get; set; }
    public string LocationCode { get; set; } = "";
    public decimal TargetQty { get; set; }
    public decimal PickedQty { get; set; }
    public PickTaskStatusEnum Status { get; set; }
    public PickTaskModeEnum PickTaskMode { get; set; } = PickTaskModeEnum.Single;
    public long? ParentPickTaskId { get; set; }
    public string? TargetLocationCode { get; set; }
    public bool IsBatchPick { get; set; }
    public int AllocationCount { get; set; }
    public string? AssignedTo { get; set; }
    public string PreferredScanValue { get; set; } = "";
    public string? LotNumber { get; set; }
    public string? ItemBarcode { get; set; }
    public string? ItemSkuCode { get; set; }
    public bool TrackSerial { get; set; }
    public int RequiredSerialCount { get; set; }
    public int PickedSerialCount { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // P1-02: Cluster picking
    public string? ToteCode { get; set; }
    public string? CartCode { get; set; }

    // P1-03: Zone picking
    public string? ZoneCode { get; set; }
}

public class TopItemRow
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string? CategoryName { get; set; }
    public string UomCode { get; set; } = "";
    public decimal TotalQty { get; set; }
    public decimal TotalValue { get; set; }
    public int VoucherCount { get; set; }
}

public class PrintLabelItem
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string Barcode { get; set; } = "";
    public string? SkuCode { get; set; }
    public string Unit { get; set; } = "";
    public string LocationCode { get; set; } = "";
    public int PrintQuantity { get; set; } = 1;
}

public class PrintLabelBatchViewModel
{
    public List<PrintLabelItem> Items { get; set; } = new();
    public string LabelSize { get; set; } = "50x30";
    public string CodeType { get; set; } = "barcode";
}

public class PartnerLabelTemplateFormViewModel
{
    public long PartnerLabelTemplateId { get; set; }
    public int? PartnerId { get; set; }
    public LabelPurposeEnum LabelPurpose { get; set; } = LabelPurposeEnum.OutboundVoucher;
    public string TemplateName { get; set; } = "";
    public string LabelSize { get; set; } = "50x30";
    public string CodeType { get; set; } = "barcode";
    public string HeaderTemplate { get; set; } = "";
    public string BodyTemplate { get; set; } = "";
    public string FooterTemplate { get; set; } = "";
    public bool IsDefault { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public List<Partner> Partners { get; set; } = new();
}

public class PartnerItemLabelRulePageViewModel
{
    public int? PartnerId { get; set; }
    public string? Search { get; set; }
    public List<Partner> Partners { get; set; } = new();
    public List<Item> Items { get; set; } = new();
    public List<PartnerItemLabelRule> Rules { get; set; } = new();
}

public class CustomerLabelPrintViewModel
{
    public LabelPrintJob Job { get; set; } = new();
    public List<CustomerLabelPrintLineViewModel> Lines { get; set; } = new();
    public string LabelSize { get; set; } = "50x30";
    public string CodeType { get; set; } = "barcode";
    public int TotalLabels { get; set; }
}

public class CustomerLabelPrintLineViewModel
{
    public long LabelPrintJobLineId { get; set; }
    public string BarcodeValue { get; set; } = "";
    public string HeaderText { get; set; } = "";
    public string BodyText { get; set; } = "";
    public string FooterText { get; set; } = "";
    public int PrintQuantity { get; set; } = 1;
    public string PartnerName { get; set; } = "";
    public string? VoucherCode { get; set; }
    public string? PackageCode { get; set; }
    public string InternalItemCode { get; set; } = "";
    public string InternalItemName { get; set; } = "";
    public string? CustomerItemCode { get; set; }
    public string? CustomerItemName { get; set; }
}

public class InboundReceivingRow
{
    public long VoucherId { get; set; }
    public string VoucherCode { get; set; } = "";
    public string? AsnCode { get; set; }
    public string WarehouseName { get; set; } = "";
    public string? PartnerName { get; set; }
    public DateTime VoucherDate { get; set; }
    public DateTime? ExpectedArrivalAt { get; set; }
    public DateTime? DockAppointmentStart { get; set; }
    public DateTime? DockAppointmentEnd { get; set; }
    public string? DockDoor { get; set; }
    public string? VehicleNumber { get; set; }
    public string? CarrierName { get; set; }
    public InboundStatusEnum InboundStatus { get; set; }
    public int TotalLines { get; set; }
    public bool IsPosted { get; set; }
    public string CreatedBy { get; set; } = "";
    public string? SubmittedBy { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public bool HasSerialTrackedLines { get; set; }
    public int PendingSerialCount { get; set; }
    public int RequiredSerialCount { get; set; }
}

public class InboundApprovalQueueViewModel
{
    public int? WarehouseId { get; set; }
    public string? Search { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<InboundReceivingRow> Rows { get; set; } = new();
    public int PendingCount => Rows.Count;
}

public class InventoryMapPageViewModel
{
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<Voucher> RecentVouchers { get; set; } = new();
    public int? SelectedWarehouseId { get; set; }
    public string SelectedWarehouseCode { get; set; } = "";
    public string SelectedWarehouseName { get; set; } = "";
    public List<InventoryMapZoneViewModel> Zones { get; set; } = new();
    public int TotalLocations { get; set; }
    public int OccupiedLocations { get; set; }
    public int WarningLocations { get; set; }
    public int CriticalLocations { get; set; }
    public int HoldLocations { get; set; }
    public decimal TotalCapacity { get; set; }
    public decimal UsedCapacity { get; set; }
    public decimal UtilizationPercent => TotalCapacity > 0 ? Math.Round((UsedCapacity / TotalCapacity) * 100m, 1) : 0m;
    public bool HasWarehouse => SelectedWarehouseId.HasValue;
}

public class InventoryMapZoneViewModel
{
    public int ZoneId { get; set; }
    public string ZoneCode { get; set; } = "";
    public string ZoneName { get; set; } = "";
    public ZoneTypeEnum ZoneType { get; set; }
    public string ZoneTypeName => ZoneType switch
    {
        ZoneTypeEnum.Storage => "Lưu trữ",
        ZoneTypeEnum.Receiving => "Tiếp nhận",
        ZoneTypeEnum.Shipping => "Xuất hàng",
        ZoneTypeEnum.Staging => "Cách ly/QC",
        ZoneTypeEnum.CrossDock => "Cross-dock",
        _ => "Khác"
    };
    public List<InventoryMapAisleGroup> Aisles { get; set; } = new();
    public int LocationCount => Aisles.Sum(a => a.Racks.Sum(r => r.Locations.Count));
    public int OccupiedCount => Aisles.Sum(a => a.Racks.Sum(r => r.Locations.Count(l => l.IsOccupied)));
    public decimal UtilizationPercent
    {
        get
        {
            var locations = Aisles.SelectMany(a => a.Racks).SelectMany(r => r.Locations).ToList();
            var capacity = locations.Sum(l => l.MaxCapacity);
            return capacity > 0 ? Math.Round((locations.Sum(l => l.CurrentLoad) / capacity) * 100m, 1) : 0m;
        }
    }
}

public class InventoryMapAisleGroup
{
    public string AisleCode { get; set; } = "";
    public List<InventoryMapRackGroup> Racks { get; set; } = new();
}

public class InventoryMapRackGroup
{
    public string RackCode { get; set; } = "";
    public List<InventoryMapLocationTile> Locations { get; set; } = new();
}

public class InventoryMapLocationTile
{
    public int LocationId { get; set; }
    public string LocationCode { get; set; } = "";
    public string AisleCode { get; set; } = "";
    public string RackCode { get; set; } = "";
    public string ShelfCode { get; set; } = "";
    public string BinCode { get; set; } = "";
    public int HeightLevel { get; set; }
    public bool IsGoldenZone { get; set; }
    public bool IsOccupied => CurrentLoad > 0;
    public decimal CurrentLoad { get; set; }
    public decimal ReservedLoad { get; set; }
    public decimal AvailableLoad => Math.Max(0, CurrentLoad - ReservedLoad);
    public decimal MaxCapacity { get; set; }
    public decimal FillPercent { get; set; }
    public int SkuCount { get; set; }
    public string PrimaryItemCode { get; set; } = "";
    public string PrimaryItemName { get; set; } = "";
    public InventoryHoldStatusEnum? HoldStatus { get; set; }
    public string StatusKey { get; set; } = "empty";
    public string StatusLabel { get; set; } = "Trống";
    public List<InventoryMapStockLine> StockLines { get; set; } = new();
}

public class InventoryMapStockLine
{
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string Uom { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal ReservedQty { get; set; }
    public decimal AvailableQty { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public InventoryHoldStatusEnum HoldStatus { get; set; }
}

public class DockBoardPageViewModel
{
    public int? WarehouseId { get; set; }
    public DateTime BoardDate { get; set; }
    public DateTime Now { get; set; }
    public int RefreshSeconds { get; set; } = 30;
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<DockDoorBoardRow> Doors { get; set; } = new();
    public List<DockBoardRow> Rows { get; set; } = new();
    public List<DockAppointment> EnterpriseAppointments { get; set; } = new();
    public int ScheduledCount { get; set; }
    public int ArrivedCount { get; set; }
    public int UnloadingCount { get; set; }
    public int CompletedCount { get; set; }
    public int DelayedCount { get; set; }
}

public class DockDoorBoardRow
{
    public string DockDoor { get; set; } = "";
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public DockDoorTypeEnum DoorType { get; set; }
    public int ActiveCount { get; set; }
    public int DelayedCount { get; set; }
    public int CompletedTodayCount { get; set; }
    public List<DockBoardRow> ActiveAppointments { get; set; } = new();
}

public class DockBoardRow
{
    public long VoucherId { get; set; }
    public string VoucherCode { get; set; } = "";
    public string? AsnCode { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public string? PartnerName { get; set; }
    public string? DockDoor { get; set; }
    public DateTime? ExpectedArrivalAt { get; set; }
    public DateTime? DockAppointmentStart { get; set; }
    public DateTime? DockAppointmentEnd { get; set; }
    public string? CarrierName { get; set; }
    public string? VehicleNumber { get; set; }
    public InboundStatusEnum InboundStatus { get; set; }
    public DockOperationStatusEnum DockStatus { get; set; }
    public DockOperationStatusEnum EffectiveDockStatus { get; set; }
    public DateTime? GateInAt { get; set; }
    public DateTime? DockArrivalAt { get; set; }
    public DateTime? UnloadStartAt { get; set; }
    public DateTime? UnloadEndAt { get; set; }
    public DateTime? DockCompletedAt { get; set; }
    public bool IsDelayed { get; set; }
    public int? DelayMinutes { get; set; }
    public int? DwellMinutes { get; set; }
    public int? UnloadMinutes { get; set; }
    public string CurrentMilestone { get; set; } = "";
}

public class YardManagementPageViewModel
{
    public int? WarehouseId { get; set; }
    public string? Search { get; set; }
    public DateTime Now { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<YardVisitRow> ActiveVisits { get; set; } = new();
    public List<YardSpotRow> Spots { get; set; } = new();
    public List<YardVoucherOption> VoucherOptions { get; set; } = new();
    public List<DockAppointment> DockAppointments { get; set; } = new();
    public List<YardVisitEvidence> RecentEvidence { get; set; } = new();
    public int AvailableSpotCount { get; set; }
    public int OccupiedSpotCount { get; set; }
    public int BlockedSpotCount { get; set; }
    public int ActiveVisitCount { get; set; }
    public int OverdueVisitCount { get; set; }
}

public class YardVisitRow
{
    public long YardVisitId { get; set; }
    public string VisitCode { get; set; } = "";
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public int TrailerId { get; set; }
    public string TrailerNumber { get; set; } = "";
    public string? ContainerNumber { get; set; }
    public TrailerTypeEnum TrailerType { get; set; }
    public string? CarrierName { get; set; }
    public string? SealNumber { get; set; }
    public string? CurrentSpotCode { get; set; }
    public int? CurrentSpotId { get; set; }
    public YardVisitPurposeEnum Purpose { get; set; }
    public YardVisitStatusEnum Status { get; set; }
    public DateTime GateInAt { get; set; }
    public DateTime? GateOutAt { get; set; }
    public int DwellMinutes { get; set; }
    public long? VoucherId { get; set; }
    public string? VoucherCode { get; set; }
    public string? DockDoor { get; set; }
    public DateTime? DockAppointmentStart { get; set; }
    public DateTime? DockAppointmentEnd { get; set; }
    public string? DriverName { get; set; }
    public string? VehicleNumber { get; set; }
    public string? Notes { get; set; }
}

public class YardSpotRow
{
    public int YardSpotId { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public string SpotCode { get; set; } = "";
    public string? SpotName { get; set; }
    public YardSpotTypeEnum SpotType { get; set; }
    public YardSpotStatusEnum Status { get; set; }
    public bool IsActive { get; set; }
    public string? OccupiedByTrailer { get; set; }
    public long? ActiveVisitId { get; set; }
}

public class YardVoucherOption
{
    public long VoucherId { get; set; }
    public string VoucherCode { get; set; } = "";
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public string? DockDoor { get; set; }
    public DateTime? DockAppointmentStart { get; set; }
    public DateTime? DockAppointmentEnd { get; set; }
    public string? CarrierName { get; set; }
    public string? VehicleNumber { get; set; }
}

public class LpnLookupRow
{
    public long LicensePlateId { get; set; }
    public string LpnCode { get; set; } = "";
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public long VoucherId { get; set; }
    public string VoucherCode { get; set; } = "";
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string? LocationCode { get; set; }
    public decimal Quantity { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public LpnStatusEnum Status { get; set; }
    public LpnTypeEnum LpnType { get; set; }
    public int DetailCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SerialLookupRow
{
    public long SerialNumberId { get; set; }
    public string SerialCode { get; set; } = "";
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public long VoucherId { get; set; }
    public string VoucherCode { get; set; } = "";
    public long? ConsumedVoucherId { get; set; }
    public string? ConsumedVoucherCode { get; set; }
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string? LocationCode { get; set; }
    public string? LpnCode { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public SerialNumberStatusEnum Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
}

public class SerialReceivingPageViewModel
{
    public long VoucherId { get; set; }
    public string VoucherCode { get; set; } = "";
    public string WarehouseName { get; set; } = "";
    public string? PartnerName { get; set; }
    public InboundStatusEnum InboundStatus { get; set; }
    public List<SerialReceivingLineRow> Lines { get; set; } = new();
}

public class SerialReceivingLineRow
{
    public long VoucherDetailId { get; set; }
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string? LocationCode { get; set; }
    public string UomCode { get; set; } = "";
    public decimal RequiredQty { get; set; }
    public int RequiredSerialCount { get; set; }
    public int RegisteredSerialCount { get; set; }
    public int RemainingSerialCount => Math.Max(0, RequiredSerialCount - RegisteredSerialCount);
    public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public List<string> ExistingSerials { get; set; } = new();
}

public class ReplenishmentSuggestionRow
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    // P2-R2-3: carry OwnerPartnerId từ source ItemLocation để downstream MovementTask không lệch owner trong 3PL.
    public int? OwnerPartnerId { get; set; }
    public int DefaultLocationId { get; set; }
    public string DefaultLocationCode { get; set; } = "";
    public int SourceItemLocationId { get; set; }
    public int SourceLocationId { get; set; }
    public string SourceLocationCode { get; set; } = "";
    public decimal PickFaceQty { get; set; }
    public decimal OpenReplenishmentQty { get; set; }
    public decimal EffectivePickFaceQty { get; set; }
    public decimal SourceAvailableQty { get; set; }
    public decimal TriggerQty { get; set; }
    public decimal TargetQty { get; set; }
    public decimal SuggestedQty { get; set; }
    public decimal DemandQty { get; set; }
    public decimal ForecastQty { get; set; }
    public ReplenishmentTriggerTypeEnum TriggerType { get; set; } = ReplenishmentTriggerTypeEnum.Threshold;
    public MovementTaskPriorityEnum Priority { get; set; } = MovementTaskPriorityEnum.Normal;
    public DateTime? DueAt { get; set; }
    public int RoutePriorityScore { get; set; }
    public int TravelSequenceScore { get; set; }
    public string? SourceZoneCode { get; set; }
    public string? DestinationZoneCode { get; set; }
    public int SourceAisleSequence { get; set; }
    public int DestinationAisleSequence { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool HasActiveLpn { get; set; }
    public decimal LocationMaxCapacity { get; set; }
    public decimal LocationCurrentLoad { get; set; }
    public decimal ItemWeightOrVolume { get; set; }
    public string CapacityUnit { get; set; } = "";
    public bool CanExecute => SuggestedQty > 0;
    public string SuggestionReason { get; set; } = "";
}

public class SlottingSuggestionRow
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public int? CurrentDefaultLocationId { get; set; }
    public string? CurrentDefaultLocationCode { get; set; }
    public int SuggestedLocationId { get; set; }
    public string SuggestedLocationCode { get; set; } = "";
    public decimal TotalStockQty { get; set; }
    public decimal SuggestedLocationQty { get; set; }
    public decimal DominancePercent { get; set; }
    public string AbcClass { get; set; } = "";
    public string VelocityBasis { get; set; } = "";
    public int SlottingScore { get; set; }
    public int VelocityScore { get; set; }
    public int ErgonomicScore { get; set; }
    public int CapacityScore { get; set; }
    public int SuggestedHeightLevel { get; set; }
    public bool SuggestedIsGoldenZone { get; set; }
    public decimal? ItemWeightKg { get; set; }
    public string Reason { get; set; } = "";
    public bool CanApply { get; set; }
}

public class SlottingSimulationPageViewModel
{
    public int? WarehouseId { get; set; }
    public string? Search { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<SlottingSimulationScenario> Scenarios { get; set; } = new();
    public List<SlottingSimulationLine> PreviewLines { get; set; } = new();
    public int CurrentSuggestionCount { get; set; }
}

public class CreateSlottingSimulationRequest
{
    public int WarehouseId { get; set; }
    public string ScenarioName { get; set; } = "";
    public string? Search { get; set; }
    public int MaxLines { get; set; } = 50;
}

public class MovementTaskPageViewModel
{
    public int? WarehouseId { get; set; }
    public MovementTaskTypeEnum? TaskType { get; set; }
    public MovementTaskStatusEnum? Status { get; set; }
    public string? AssignedTo { get; set; }
    public string? Search { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<MovementTaskRow> Tasks { get; set; } = new();
    public int OpenCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int ShortCount { get; set; }
}

public class MovementTaskRow
{
    public long MovementTaskId { get; set; }
    public string TaskCode { get; set; } = "";
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public MovementTaskModeEnum MovementMode { get; set; } = MovementTaskModeEnum.Item;
    public long? LicensePlateId { get; set; }
    public string? LpnCode { get; set; }
    public int LpnDetailCount { get; set; }
    public int LpnDistinctItemCount { get; set; }
    public bool IsLpnMode => MovementMode == MovementTaskModeEnum.Lpn;
    public string SourceLocationCode { get; set; } = "";
    public string DestinationLocationCode { get; set; } = "";
    public MovementTaskTypeEnum TaskType { get; set; }
    public MovementTaskStatusEnum Status { get; set; }
    public MovementTaskPriorityEnum Priority { get; set; }
    public decimal PlannedQty { get; set; }
    public decimal ConfirmedQty { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? SourceReason { get; set; }
    public string? SourceModule { get; set; }
    public ReplenishmentTriggerTypeEnum? ReplenishmentTriggerType { get; set; }
    public decimal DemandQtySnapshot { get; set; }
    public decimal ForecastQtySnapshot { get; set; }
    public int RoutePriorityScore { get; set; }
    public int TravelSequenceScore { get; set; }
}

public class KittingWorkOrderPageViewModel
{
    public int? WarehouseId { get; set; }
    public KittingWorkOrderStatusEnum? Status { get; set; }
    public string? Search { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<KittingWorkOrder> WorkOrders { get; set; } = new();
    public int DraftCount { get; set; }
    public int ReservedCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
}

public class KittingWorkOrderCreatePageViewModel
{
    public CreateKittingWorkOrderCommand Request { get; set; } = new();
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<Item> FinishedItems { get; set; } = new();
    public List<Location> Locations { get; set; } = new();
}

public class VasWorkOrderPageViewModel
{
    public int? WarehouseId { get; set; }
    public VasWorkOrderStatusEnum? Status { get; set; }
    public VasOperationTypeEnum? OperationType { get; set; }
    public string? Search { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<VasWorkOrder> WorkOrders { get; set; } = new();
    public int DraftCount { get; set; }
    public int ReservedCount { get; set; }
    public int InProgressCount { get; set; }
    public int QcPendingCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
}

public class VasWorkOrderCreatePageViewModel
{
    public CreateVasWorkOrderCommand Request { get; set; } = new();
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<Partner> Partners { get; set; } = new();
    public List<Voucher> Vouchers { get; set; } = new();
    public List<Item> Items { get; set; } = new();
    public List<Item> MaterialItems { get; set; } = new();
}

public class OperationExceptionRow
{
    public string ExceptionKey { get; set; } = "";
    public string CategoryKey { get; set; } = "";
    public string CategoryLabel { get; set; } = "";
    public string SeverityKey { get; set; } = "";
    public string SeverityLabel { get; set; } = "";
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public string ReferenceCode { get; set; } = "";
    public string? SecondaryReference { get; set; }
    public string Summary { get; set; } = "";
    public string Detail { get; set; } = "";
    public string? ItemCode { get; set; }
    public string? LocationCode { get; set; }
    public DateTime? DueAt { get; set; }
    public double? AgeHours { get; set; }
    public string CaseStatusKey { get; set; } = "open";
    public string CaseStatusLabel { get; set; } = "Mới phát hiện";
    public string? AssignedTo { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
    public string? ResolutionNote { get; set; }
    public string ActionUrl { get; set; } = "";
    public string ActionLabel { get; set; } = "";
}

public class ShippingBoardRow
{
    public long VoucherId { get; set; }
    public string VoucherCode { get; set; } = "";
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public string? PartnerName { get; set; }
    public VoucherTypeEnum VoucherType { get; set; }
    public string VoucherTypeName { get; set; } = "";
    public DateTime VoucherDate { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public FulfillmentStatusEnum FulfillmentStatus { get; set; }
    public DateTime? PackedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public string? TrackingNumber { get; set; }
    public string? ManifestCode { get; set; }
    public int TotalLines { get; set; }
    public int PackageCount { get; set; }
    public string? PackageSummary { get; set; }
    public bool IsOverdue { get; set; }
    public bool RequiresManifest { get; set; }
    public bool RequiresTrackingOrManifest { get; set; }
    public string? LoadCode { get; set; }
    public ShipmentLoadStatusEnum? LoadStatus { get; set; }
    public string? LoadCarrierName { get; set; }
    public string? LoadRouteName { get; set; }
    public int CarrierShipmentCreatedCount { get; set; }
    public int CarrierShipmentQueuedCount { get; set; }
    public int CarrierShipmentFailedCount { get; set; }
    public string? CarrierShipmentSummary { get; set; }
    public string StageLabel => ShippedAt.HasValue
        ? "Đã giao hàng"
        : PackedAt.HasValue
            ? "Chờ giao hàng"
            : "Chờ đóng gói";
}

public class OutboundPackageLookupRow
{
    public long OutboundPackageId { get; set; }
    public string PackageCode { get; set; } = "";
    public long VoucherId { get; set; }
    public string VoucherCode { get; set; } = "";
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public string? PartnerName { get; set; }
    public string SourceType { get; set; } = "Manual";
    public string? PackageType { get; set; }
    public string? ReferenceLpnCode { get; set; }
    public decimal? TotalQuantity { get; set; }
    public int ItemCount { get; set; }
    public string PackedBy { get; set; } = "";
    public DateTime PackedAt { get; set; }
    public string? TrackingNumber { get; set; }
    public string? ManifestCode { get; set; }
    public decimal? ActualCatchWeight { get; set; }
    public string? LoadCode { get; set; }
    public ShipmentLoadStatusEnum? LoadStatus { get; set; }
    public string? CarrierName { get; set; }
    public CarrierShipmentStatusEnum? CarrierStatus { get; set; }
    public string? CarrierTrackingNumber { get; set; }
    public string? CarrierLastError { get; set; }
    public string CarrierStatusLabel => CarrierStatus.HasValue ? CarrierStatusLabels.GetLabel(CarrierStatus.Value) : "Chưa tạo vận đơn";
    public string? Notes { get; set; }
}

public class ShipmentLoadBoardRow
{
    public long ShipmentLoadId { get; set; }
    public string LoadCode { get; set; } = "";
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public ShipmentLoadStatusEnum Status { get; set; }
    public string? CarrierName { get; set; }
    public string? RouteName { get; set; }
    public string? VehicleNumber { get; set; }
    public string? DockDoor { get; set; }
    public string? ManifestCode { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime? PlannedDepartureAt { get; set; }
    public DateTime? ActualDepartureAt { get; set; }
    public int TotalVoucherCount { get; set; }
    public int TotalPackageCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal? TotalCatchWeight { get; set; }
    public int CarrierShipmentCreatedCount { get; set; }
    public int CarrierShipmentFailedCount { get; set; }
    public string? CarrierShipmentSummary { get; set; }
}

public class ShipmentLoadDetailsViewModel
{
    public ShipmentLoad Load { get; set; } = new();
    public List<ShipmentLoadVoucherRow> Vouchers { get; set; } = new();
    public List<ShipmentLoadPackageRow> Packages { get; set; } = new();
    public List<Voucher> CandidateVouchers { get; set; } = new();
}

public class ShipmentLoadVoucherRow
{
    public long VoucherId { get; set; }
    public string VoucherCode { get; set; } = "";
    public string? PartnerName { get; set; }
    public int Sequence { get; set; }
    public int? StopNumber { get; set; }
    public FulfillmentStatusEnum FulfillmentStatus { get; set; }
    public DateTime? PackedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
}

public class ShipmentLoadPackageRow
{
    public long OutboundPackageId { get; set; }
    public string PackageCode { get; set; } = "";
    public string VoucherCode { get; set; } = "";
    public bool IsLoaded { get; set; }
    public string? LoadedBy { get; set; }
    public DateTime? LoadedAt { get; set; }
    public decimal? TotalQuantity { get; set; }
    public decimal? ActualCatchWeight { get; set; }
    public string? CarrierName { get; set; }
    public CarrierShipmentStatusEnum? CarrierStatus { get; set; }
    public string? CarrierTrackingNumber { get; set; }
    public string? CarrierLastError { get; set; }
    public string CarrierStatusLabel => CarrierStatus.HasValue ? CarrierStatusLabels.GetLabel(CarrierStatus.Value) : "Chưa tạo vận đơn";
}

public class CarrierConnectorPageViewModel
{
    public int? WarehouseId { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<CarrierConnector> Connectors { get; set; } = new();
}

public class ShippingDispatchViewModel
{
    public int? WarehouseId { get; set; }
    public int? CarrierConnectorId { get; set; }
    public CarrierShipmentStatusEnum? Status { get; set; }
    public string? Search { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<CarrierConnector> Connectors { get; set; } = new();
    public List<ShippingDispatchRow> Rows { get; set; } = new();
}

public class ShippingDispatchRow
{
    public long VoucherId { get; set; }
    public string VoucherCode { get; set; } = "";
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public string? PartnerName { get; set; }
    public long OutboundPackageId { get; set; }
    public string PackageCode { get; set; } = "";
    public string? PackageTrackingNumber { get; set; }
    public string? ManifestCode { get; set; }
    public string? LoadCode { get; set; }
    public ShipmentLoadStatusEnum? LoadStatus { get; set; }
    public DateTime PackedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public long? CarrierShipmentId { get; set; }
    public int? CarrierConnectorId { get; set; }
    public string? CarrierCode { get; set; }
    public string? CarrierName { get; set; }
    public CarrierShipmentStatusEnum? CarrierStatus { get; set; }
    public string? CarrierTrackingNumber { get; set; }
    public string? LabelUrl { get; set; }
    public string? ProofOfDeliveryUrl { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CarrierStatusLabel => CarrierStatus.HasValue ? CarrierStatusLabels.GetLabel(CarrierStatus.Value) : "Chưa tạo vận đơn";
    public bool CanCreate => !CarrierShipmentId.HasValue && !ShippedAt.HasValue;
    public bool CanRetry => CarrierStatus is CarrierShipmentStatusEnum.Failed or CarrierShipmentStatusEnum.Pending or CarrierShipmentStatusEnum.Queued;
    public bool CanCancel => CarrierShipmentId.HasValue && CarrierStatus is not (CarrierShipmentStatusEnum.Cancelled or CarrierShipmentStatusEnum.Delivered);
    public bool CanSync => CarrierShipmentId.HasValue && CarrierStatus is not CarrierShipmentStatusEnum.Cancelled;
}

public static class CarrierStatusLabels
{
    public static string GetLabel(CarrierShipmentStatusEnum status) => status switch
    {
        CarrierShipmentStatusEnum.Pending => "Chờ gửi",
        CarrierShipmentStatusEnum.Queued => "Đang chờ kết nối",
        CarrierShipmentStatusEnum.Created => "Đã tạo vận đơn",
        CarrierShipmentStatusEnum.Failed => "Lỗi kết nối",
        CarrierShipmentStatusEnum.Cancelled => "Đã hủy",
        CarrierShipmentStatusEnum.Delivered => "Đã giao thành công",
        CarrierShipmentStatusEnum.DeliveryFailed => "Giao thất bại",
        _ => "Không xác định"
    };
}

public class ShippingDocumentPrintViewModel
{
    public LabelPrintJob Job { get; set; } = new();
    public string DocumentType { get; set; } = "";
    public string DocumentTitle { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public string PrintedBy { get; set; } = "";
    public DateTime PrintedAt { get; set; }
    public List<ShippingPackageLabelRow> PackageLabels { get; set; } = new();
    public List<ShippingManifestVoucherRow> Vouchers { get; set; } = new();
    public List<ShippingManifestPackageRow> Packages { get; set; } = new();
    public List<ShippingHandoverDocumentRow> Handovers { get; set; } = new();
    public string? LoadCode { get; set; }
    public string? WarehouseName { get; set; }
    public string? CarrierName { get; set; }
    public string? RouteName { get; set; }
    public string? VehicleNumber { get; set; }
    public string? ManifestCode { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Notes { get; set; }
}

public class ShippingPackageLabelRow
{
    public long OutboundPackageId { get; set; }
    public string PackageCode { get; set; } = "";
    public string VoucherCode { get; set; } = "";
    public string? TrackingNumber { get; set; }
    public string? ManifestCode { get; set; }
    public string? LoadCode { get; set; }
    public string? PartnerName { get; set; }
    public string? ShipToAddress { get; set; }
    public string? CarrierName { get; set; }
    public string? RouteName { get; set; }
    public string? ScanCode { get; set; }
    public decimal? TotalQuantity { get; set; }
    public decimal? ActualCatchWeight { get; set; }
}

public class ShippingManifestVoucherRow
{
    public long VoucherId { get; set; }
    public string VoucherCode { get; set; } = "";
    public string? PartnerName { get; set; }
    public string? TrackingNumber { get; set; }
    public string? ManifestCode { get; set; }
    public int PackageCount { get; set; }
    public DateTime? ShippedAt { get; set; }
}

public class ShippingManifestPackageRow
{
    public long OutboundPackageId { get; set; }
    public string PackageCode { get; set; } = "";
    public string VoucherCode { get; set; } = "";
    public string? TrackingNumber { get; set; }
    public string? CarrierName { get; set; }
    public bool IsLoaded { get; set; }
    public decimal? TotalQuantity { get; set; }
    public decimal? ActualCatchWeight { get; set; }
}

public class ShippingHandoverDocumentRow
{
    public long? ShippingHandoverLogId { get; set; }
    public string VoucherCode { get; set; } = "";
    public string? TrackingNumber { get; set; }
    public string? ManifestCode { get; set; }
    public string? CarrierName { get; set; }
    public string? VehicleNumber { get; set; }
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }
    public string HandedOverBy { get; set; } = "";
    public DateTime HandedOverAt { get; set; }
    public string? Notes { get; set; }
}

public class DeliveryReconciliationViewModel
{
    public int? WarehouseId { get; set; }
    public string? Severity { get; set; }
    public string? IssueType { get; set; }
    public string? Search { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<DeliveryReconciliationRow> Rows { get; set; } = new();
}

public class DeliveryReconciliationRow
{
    public string IssueType { get; set; } = "";
    public string IssueLabel { get; set; } = "";
    public string Severity { get; set; } = "warning";
    public string SeverityLabel => Severity switch
    {
        "critical" => "Nghiêm trọng",
        "error" => "Lỗi",
        "warning" => "Cảnh báo",
        _ => "Thông tin"
    };
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public long? VoucherId { get; set; }
    public string? VoucherCode { get; set; }
    public long? OutboundPackageId { get; set; }
    public string? PackageCode { get; set; }
    public long? ShipmentLoadId { get; set; }
    public string? LoadCode { get; set; }
    public long? CarrierShipmentId { get; set; }
    public string? TrackingNumber { get; set; }
    public string Summary { get; set; } = "";
    public string Recommendation { get; set; } = "";
    public string ActionUrl { get; set; } = "";
}

public class CarrierSlaRow
{
    public string CarrierName { get; set; } = "";
    public int TotalShipped { get; set; }
    public int OnTimeCount { get; set; }
    public int OverdueCount { get; set; }
    public decimal OnTimeRate { get; set; }
    public double AvgLeadHours { get; set; }
    public double AvgPackToShipHours { get; set; }
}

// P3.1: Slotting Optimization - Velocity Heatmap
public class VelocityHeatmapRow
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string AbcClass { get; set; } = "";
    public string XyzClass { get; set; } = "";
    public string CombinedClass { get; set; } = "";
    public int PickCount { get; set; }
    public decimal DailyFrequency { get; set; }
    public string CurrentLocation { get; set; } = "";
    public int Score { get; set; }
    public int PickFacePriority { get; set; }
    public string SlottingRecommendation { get; set; } = "";
}

// P3.3: Capacity Simulation
public class CapacityScenarioRow
{
    public int ScenarioId { get; set; }
    public string ScenarioName { get; set; } = "";
    public string? Description { get; set; }
    public DateTime ScenarioDate { get; set; }
    public int DailyVolume { get; set; }
    public int VolumeGrowthPct { get; set; }
    public int DockCount { get; set; }
    public int LaborCount { get; set; }
    public string? Bottlenecks { get; set; }
    public string? Recommendations { get; set; }
    public string? CriticalBottleneck { get; set; }
    public int ConfidenceScore { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

// ═══ Enterprise Validators ViewModels ═══

public class StockCountApprovalViewModel
{
    public long SheetId { get; set; }
    public string AdjustmentVoucherCode { get; set; } = "";
    public bool GenerateAdjustment { get; set; } = true;
    public string? Notes { get; set; }
}

public class QcInspectionViewModel
{
    public long VoucherId { get; set; }
    public long VoucherDetailId { get; set; }
    public int ItemId { get; set; }
    public decimal TotalQty { get; set; }
    public decimal SampleQty { get; set; }
    public decimal PassedQty { get; set; }
    public decimal FailedQty { get; set; }
    public int Disposition { get; set; }
    public string? Notes { get; set; }
    public string? DefectDescription { get; set; }
}

public class SerialRegistrationViewModel
{
    public long VoucherId { get; set; }
    public long VoucherDetailId { get; set; }
    public string SerialCodes { get; set; } = "";
}

public class WaveCreationViewModel
{
    public List<long> VoucherIds { get; set; } = new();
    public string WaveProfile { get; set; } = "Standard";
    public string? CarrierCode { get; set; }
    public string? CarrierName { get; set; }
    public string? RouteCode { get; set; }
    public DateTime? CutoffTime { get; set; }
    public int Priority { get; set; } = 2;
    public string? Notes { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// P3-05: Task Interleaving — Hàng đợi nhiệm vụ thống nhất
// ═══════════════════════════════════════════════════════════════

/// <summary>Phân loại nhiệm vụ trong hàng đợi interleaving</summary>
public enum TaskCategoryEnum : byte
{
    Pick = 1,
    Movement = 2
}

/// <summary>Hàng đợi nhiệm vụ đã xếp hạng theo vị trí hiện tại của picker</summary>
public class InterleavedTaskQueue
{
    public int? CurrentLocationId { get; set; }
    public string? CurrentLocationCode { get; set; }
    public string? CurrentZoneCode { get; set; }
    public int? WarehouseId { get; set; }
    public string CurrentUser { get; set; } = "";
    public List<InterleavedTaskItem> Tasks { get; set; } = new();
    public int TotalPickTasks { get; set; }
    public int TotalMovementTasks { get; set; }
    public string ScoringExplanation { get; set; } = "";
}

/// <summary>Một nhiệm vụ (Pick hoặc Movement) đã được tính điểm xếp hạng</summary>
public class InterleavedTaskItem
{
    public TaskCategoryEnum Category { get; set; }
    public long TaskId { get; set; }
    public string TaskCode { get; set; } = "";
    public string TaskTypeName { get; set; } = "";
    public string ItemCode { get; set; } = "";
    public string? ItemName { get; set; }
    public string SourceLocationCode { get; set; } = "";
    public string DestinationLocationCode { get; set; } = "";
    public string? ZoneCode { get; set; }
    public string? AisleCode { get; set; }
    public int AisleSequence { get; set; }
    public decimal PlannedQty { get; set; }
    public decimal CompletedQty { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? DueAt { get; set; }
    public MovementTaskPriorityEnum Priority { get; set; }
    public int ProximityScore { get; set; }
    public int PriorityScore { get; set; }
    public int UrgencyScore { get; set; }
    public int InterleavingBonus { get; set; }
    public int TotalScore { get; set; }
    public string ScoreBreakdown { get; set; } = "";
    public string ActionUrl { get; set; } = "";
}

// ═══ P2-03B: Yard Billing ViewModels ═══

public class YardBillingRatePageViewModel
{
    public int? WarehouseId { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<Partner> Partners { get; set; } = new();
    public List<YardBillingRate> Rates { get; set; } = new();
}

public class YardBillingChargePageViewModel
{
    public int? WarehouseId { get; set; }
    public YardChargeStatusEnum? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<YardBillingChargeRow> Charges { get; set; } = new();
    public int DraftCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int WaivedCount { get; set; }
    public decimal TotalDraftAmount { get; set; }
    public decimal TotalConfirmedAmount { get; set; }
}

public class YardBillingChargeRow
{
    public long YardBillingChargeId { get; set; }
    public long YardVisitId { get; set; }
    public string VisitCode { get; set; } = "";
    public string TrailerNumber { get; set; } = "";
    public string? ContainerNumber { get; set; }
    public string? CarrierName { get; set; }
    public string? PartnerName { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public int TotalDwellMinutes { get; set; }
    public int FreeTimeMinutes { get; set; }
    public int ChargeableMinutes { get; set; }
    public decimal AppliedRatePerHour { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public YardChargeStatusEnum Status { get; set; }
    public DateTime? GateInAt { get; set; }
    public DateTime? GateOutAt { get; set; }
    public string? ConfirmedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? WaivedBy { get; set; }
    public string? WaivedReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

// P3-06: 3PL tenant owner assignment and billing
public class TenantOwnerScopePageViewModel
{
    public List<AppUser> Users { get; set; } = new();
    public List<Partner> Owners { get; set; } = new();
    public List<AppUserOwnerScope> Scopes { get; set; } = new();
}

public class ThreePlBillingRatePageViewModel
{
    public int? WarehouseId { get; set; }
    public int? OwnerPartnerId { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<Partner> Owners { get; set; } = new();
    public List<ThreePlBillingRate> Rates { get; set; } = new();
}

public class ThreePlBillingRunBoardViewModel
{
    public int? WarehouseId { get; set; }
    public int? OwnerPartnerId { get; set; }
    public DateTime? PeriodFrom { get; set; }
    public DateTime? PeriodTo { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<Partner> Owners { get; set; } = new();
    public List<ThreePlBillingRunRow> Runs { get; set; } = new();
}

public class ThreePlBillingRunRow
{
    public long ThreePlBillingRunId { get; set; }
    public string RunCode { get; set; } = "";
    public string WarehouseName { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public ThreePlBillingRunStatusEnum Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTime CreatedAt { get; set; }
}

public class ThreePlBillingRunDetailsViewModel
{
    public ThreePlBillingRun Run { get; set; } = new();
    public List<ThreePlBillingCharge> Charges { get; set; } = new();
    public ThreePlInvoice? Invoice { get; set; }
    public List<ThreePlDispute> Disputes { get; set; } = new();
}

public class ThreePlContractPageViewModel
{
    public int? WarehouseId { get; set; }
    public int? OwnerPartnerId { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<Partner> Owners { get; set; } = new();
    public List<ThreePlContract> Contracts { get; set; } = new();
}

public class ThreePlInvoiceDetailsViewModel
{
    public ThreePlInvoice Invoice { get; set; } = new();
    public List<ThreePlDispute> Disputes { get; set; } = new();
}

public class ThreePlClientPortalViewModel
{
    public int? OwnerPartnerId { get; set; }
    public List<Partner> Owners { get; set; } = new();
    public List<ItemLocation> Inventory { get; set; } = new();
    public List<Voucher> Orders { get; set; } = new();
    public List<ThreePlInvoice> Invoices { get; set; } = new();
    public List<SlaMetric> SlaMetrics { get; set; } = new();
    public decimal TotalInventoryQty { get; set; }
    public decimal OpenFeeAmount { get; set; }
}

public class LaborProductivityPageViewModel
{
    public int? WarehouseId { get; set; }
    public int Days { get; set; } = 7;
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<Zone> Zones { get; set; } = new();
    public List<LaborStandard> Standards { get; set; } = new();
    public List<LaborActivity> Activities { get; set; } = new();
    public List<LaborExceptionReview> Exceptions { get; set; } = new();
    public List<LaborProductivityRow> WorkerRows { get; set; } = new();
    public List<LaborBottleneckRow> Bottlenecks { get; set; } = new();
}

public class LaborProductivityRow
{
    public string UserName { get; set; } = "";
    public string ShiftCode { get; set; } = "";
    public string WarehouseName { get; set; } = "";
    public string? ZoneName { get; set; }
    public int TaskCount { get; set; }
    public decimal WorkQuantity { get; set; }
    public decimal ExpectedMinutes { get; set; }
    public decimal ActualMinutes { get; set; }
    public decimal ProductivityPercent { get; set; }
    public int ExceptionCount { get; set; }
}

public class LaborBottleneckRow
{
    public string WarehouseName { get; set; } = "";
    public string ZoneName { get; set; } = "";
    public string TaskType { get; set; } = "";
    public int Backlog { get; set; }
    public decimal AverageWaitingMinutes { get; set; }
    public decimal AverageProductivityPercent { get; set; }
}

// P3-07: MHE / Robot / AMR operations dashboard
public class MheDashboardViewModel
{
    public int? WarehouseId { get; set; }
    public MheCommandStatusEnum? Status { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<MheSystem> Systems { get; set; } = new();
    public List<MheCommandRow> Commands { get; set; } = new();
    public int PendingCount { get; set; }
    public int FailedCount { get; set; }
}

public class MheCommandRow
{
    public long MheCommandId { get; set; }
    public string CommandCode { get; set; } = "";
    public string WarehouseName { get; set; } = "";
    public string? OwnerName { get; set; }
    public string? SystemCode { get; set; }
    public MheCommandTypeEnum CommandType { get; set; }
    public MheCommandStatusEnum Status { get; set; }
    public string? SourceType { get; set; }
    public string? SourceCode { get; set; }
    public string CorrelationId { get; set; } = "";
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? LastError { get; set; }
}

public class OptimizationEnterpriseDashboardViewModel
{
    public int? WarehouseId { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<OptimizationRun> Runs { get; set; } = new();
    public List<OptimizationRecommendationLine> Recommendations { get; set; } = new();
    public List<WavelessReleaseQueue> WavelessQueue { get; set; } = new();
    public List<PickPathPlan> PickPathPlans { get; set; } = new();
    public List<ToteClusterPlan> ToteClusterPlans { get; set; } = new();
}

public class AutomationEnterpriseDashboardViewModel
{
    public int? WarehouseId { get; set; }
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<MheAdapterProfile> AdapterProfiles { get; set; } = new();
    public List<MheTelemetryEvent> TelemetryEvents { get; set; } = new();
    public List<WcsSimulatorRun> SimulatorRuns { get; set; } = new();
    public List<MheCommand> Commands { get; set; } = new();
    public List<AutomationOverride> Overrides { get; set; } = new();
}

public class IntegrationEnterpriseDashboardViewModel
{
    public List<EdiMessage> EdiMessages { get; set; } = new();
    public List<WebhookSubscription> WebhookSubscriptions { get; set; } = new();
    public List<WebhookDelivery> WebhookDeliveries { get; set; } = new();
    public List<EnterpriseConnector> Connectors { get; set; } = new();
    public List<EnterpriseConnectorDelivery> ConnectorDeliveries { get; set; } = new();
    public List<IntegrationOutbox> Outbox { get; set; } = new();
}
