using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using WMS.Common;

namespace WMS.Models;

/// <summary>
/// Định dạng lỗi chuẩn theo RFC 7807
/// </summary>
public class ProblemDetails
{
    public string Type { get; set; } = "about:blank";
    public string Title { get; set; } = "An error occurred";
    public int Status { get; set; }
    public string Detail { get; set; } = "";
    public string Instance { get; set; } = "";
    public string TraceId { get; set; } = "";
    public Dictionary<string, object?> Extensions { get; set; } = new();

    public static ProblemDetails FromException(Exception ex, HttpContext context)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var problem = new ProblemDetails
        {
            TraceId = traceId,
            Instance = context.Request.Path
        };

        switch (ex)
        {
            case BusinessRuleException bizEx:
                problem.Status = (int)HttpStatusCode.BadRequest;
                problem.Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1";
                problem.Title = "Vi phạm quy tắc nghiệp vụ";
                problem.Detail = UserSafeError.From(bizEx);
                problem.Extensions["code"] = bizEx.Code;
                problem.Extensions["entity"] = bizEx.EntityName;
                break;

            case ConcurrencyException:
                problem.Status = (int)HttpStatusCode.Conflict;
                problem.Type = "https://tools.ietf.org/html/rfc9110#section-15.5.8";
                problem.Title = "Concurrency Conflict";
                problem.Detail = "Dữ liệu đã được thay đổi bởi người khác. Vui lòng tải lại trang.";
                break;

            case UnauthorizedAccessException:
                problem.Status = (int)HttpStatusCode.Forbidden;
                problem.Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4";
                problem.Title = "Access Denied";
                problem.Detail = "Bạn không có quyền thực hiện thao tác này.";
                break;

            case KeyNotFoundException:
                problem.Status = (int)HttpStatusCode.NotFound;
                problem.Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5";
                problem.Title = "Resource Not Found";
                problem.Detail = "Không tìm thấy dữ liệu yêu cầu hoặc dữ liệu không còn khả dụng.";
                break;

            case SodViolationException sodEx:
                problem.Status = (int)HttpStatusCode.Forbidden;
                problem.Type = "https://wms.enterprise/security#segregation-of-duties";
                problem.Title = "Segregation of Duties Violation";
                problem.Detail = UserSafeError.From(sodEx);
                problem.Extensions["maker"] = sodEx.Maker;
                problem.Extensions["action"] = sodEx.ActionName;
                break;

            case WarehouseLockedException lockedEx:
                problem.Status = (int)HttpStatusCode.Locked;
                problem.Type = "https://wms.enterprise/operations#warehouse-locked";
                problem.Title = "Kỳ kho đã khóa";
                problem.Detail = UserSafeError.From(lockedEx);
                problem.Extensions["lockedDate"] = lockedEx.LockedDate.ToString("yyyy-MM-dd");
                break;

            default:
                problem.Status = (int)HttpStatusCode.InternalServerError;
                problem.Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1";
                problem.Title = "Lỗi nội bộ hệ thống";
                problem.Detail = "Một lỗi không mong muốn đã xảy ra.";
                break;
        }

        return problem;
    }
}

/// <summary>
/// Lỗi vi phạm quy tắc nghiệp vụ.
/// </summary>
public class BusinessRuleException : Exception
{
    public string Code { get; }
    public string EntityName { get; }

    public BusinessRuleException(string message, string code = "", string entityName = "")
        : base(message)
    {
        Code = code;
        EntityName = entityName;
    }
}

/// <summary>
/// Concurrency conflict exception
/// </summary>
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }
}

/// <summary>
/// Segregation of Duties violation exception
/// </summary>
public class SodViolationException : Exception
{
    public string Maker { get; }
    public string ActionName { get; }

    public SodViolationException(string message, string maker, string actionName)
        : base(message)
    {
        Maker = maker;
        ActionName = actionName;
    }
}

/// <summary>
/// Ngoại lệ khi kỳ kho đã bị khóa
/// </summary>
public class WarehouseLockedException : Exception
{
    public DateTime LockedDate { get; }

    public WarehouseLockedException(string message, DateTime lockedDate)
        : base(message)
    {
        LockedDate = lockedDate;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// FACTORY METHODS — Dùng thay throw new Exception để đảm bảo typed exceptions
// ═══════════════════════════════════════════════════════════════════════════════

public static class WmsExceptions
{
    // Validation errors (BadRequest)
    public static BusinessRuleException Validation(string message)
        => new(message, code: "VALIDATION", entityName: "");

    public static BusinessRuleException ItemNotFound(int itemId)
        => new($"Không tìm thấy vật tư với ID {itemId}. Vui lòng tải lại màn hình và thử lại.",
            code: "ITEM_NOT_FOUND", entityName: "Item");

    public static BusinessRuleException UnitConversionNotFound(string itemCode)
        => new($"Không tìm thấy quy đổi từ ĐVT nguồn sang ĐVT tồn cho [{itemCode}]. Vui lòng cấu hình bảng quy đổi.",
            code: "UNIT_CONVERSION_MISSING", entityName: "UnitConversion");

    public static BusinessRuleException DuplicateUnitConversion()
        => new("Trùng quy đổi toàn cục cho cùng cặp ĐVT.",
            code: "DUPLICATE_UNIT_CONVERSION", entityName: "UnitConversion");

    public static BusinessRuleException DuplicateUnitConversionReverse()
        => new("Trùng quy đổi ngược toàn cục cho cùng cặp ĐVT.",
            code: "DUPLICATE_UNIT_CONVERSION_REVERSE", entityName: "UnitConversion");

    public static BusinessRuleException CodeGenerationFailed(string entity)
        => new($"Không thể sinh mã {entity} mới. Vui lòng thử lại.",
            code: "CODE_GENERATION_FAILED", entityName: entity);

    public static BusinessRuleException DuplicateCode(string entity)
        => new($"Không thể tạo mã {entity} do trùng mã đồng thời. Vui lòng thử lại.",
            code: "DUPLICATE_CODE", entityName: entity);

    public static BusinessRuleException RequiredPartner()
        => new("Loại phiếu hiện tại bắt buộc chọn đối tác.",
            code: "PARTNER_REQUIRED", entityName: "Voucher");

    public static BusinessRuleException DestinationWarehouseRequired()
        => new("Phiếu chuyển kho phải chọn kho đích.",
            code: "DEST_WAREHOUSE_REQUIRED", entityName: "Voucher");

    public static BusinessRuleException DestinationWarehouseSameAsSource()
        => new("Kho đích không được trùng kho nguồn.",
            code: "SAME_WAREHOUSE", entityName: "Voucher");

    public static BusinessRuleException LocationRequired(string context)
        => new($"Vị trí nguồn không thuộc kho đã chọn cho [{context}].",
            code: "LOCATION_MISMATCH", entityName: "Location");

    public static BusinessRuleException DestinationLocationRequired(string itemCode)
        => new($"Phiếu chuyển kho thiếu vị trí đích cho [{itemCode}].",
            code: "DEST_LOCATION_REQUIRED", entityName: "Location");

    public static BusinessRuleException DestinationLocationSameAsSource(string itemCode)
        => new($"Vị trí đích không được trùng vị trí nguồn cho [{itemCode}].",
            code: "SAME_LOCATION", entityName: "Location");

    public static BusinessRuleException LocationNotInWarehouse(string itemCode, string warehouse)
        => new($"Vị trí nguồn không thuộc kho {warehouse} cho [{itemCode}].",
            code: "LOCATION_WRONG_WAREHOUSE", entityName: "Location");

    public static BusinessRuleException LocationDestinationNotInWarehouse(string itemCode, string warehouse)
        => new($"Vị trí đích không thuộc kho đích cho [{itemCode}].",
            code: "DEST_LOCATION_WRONG_WAREHOUSE", entityName: "Location");

    public static BusinessRuleException CapacityExceeded(string locationCode, decimal maxCapacity, string unit, string itemCode, decimal actual)
        => new($"Ô {locationCode} chứa tối đa {maxCapacity:N0} {unit}! Mã {itemCode} làm ô quá tải lên thành {actual:N2} {unit}.",
            code: "CAPACITY_EXCEEDED", entityName: "Location");

    public static BusinessRuleException NegativeUnitPrice(string itemCode)
        => new($"Đơn giá không được âm cho [{itemCode}].",
            code: "NEGATIVE_UNIT_PRICE", entityName: "VoucherDetail");

    public static BusinessRuleException NegativeLineAmount(string itemCode)
        => new($"Thành tiền không được âm cho [{itemCode}].",
            code: "NEGATIVE_LINE_AMOUNT", entityName: "VoucherDetail");

    public static BusinessRuleException DefectQtyExceedsLineQty(string itemCode, decimal defectQty, decimal lineQty)
        => new($"SL lỗi/thiếu không hợp lệ cho [{itemCode}]. SL lỗi/thiếu ({defectQty:N2}) không được lớn hơn SL đích ({lineQty:N2}).",
            code: "DEFECT_QTY_INVALID", entityName: "VoucherDetail");

    public static BusinessRuleException OneLocationOneItemConflict(string itemCode, string conflictItemCode, string locationCode)
        => new($"Vi phạm quy tắc '1 ô 1 vật tư': Vị trí này đang chứa mặt hàng [{conflictItemCode}]. Bạn không được xếp [{itemCode}] chung vào dãy này!",
            code: "ONE_LOCATION_ONE_ITEM", entityName: "ItemLocation");

    public static BusinessRuleException OneLocationOneItemConflictLocal(string itemCode, string conflictItemCode)
        => new($"Vi phạm quy tắc '1 ô 1 vật tư': Ngay trong cùng 1 phiếu, bạn đang cố xếp [{itemCode}] và [{conflictItemCode}] đè lên nhau tại cùng 1 vị trí!",
            code: "ONE_LOCATION_ONE_ITEM_LOCAL", entityName: "ItemLocation");

    public static BusinessRuleException InsufficientStock(string itemCode, decimal available, string location = "")
        => new($"Lỗi: Vật tư {itemCode} không đủ tồn kho{(string.IsNullOrEmpty(location) ? "" : $" tại vị trí {location}")} (chỉ còn {available:N2}).",
            code: "INSUFFICIENT_STOCK", entityName: "ItemLocation");

    public static BusinessRuleException NegativeItemStock(string itemCode, decimal currentStock)
        => new($"Lỗi: Điều chỉnh giảm làm âm tổng tồn của {itemCode} (hiện chỉ còn {currentStock:N2}).",
            code: "NEGATIVE_ITEM_STOCK", entityName: "Item");

    public static BusinessRuleException NegativeLocationStock(string itemCode, decimal currentStock, string locationCode = "")
        => new($"{(string.IsNullOrEmpty(locationCode) ? "" : $"Tại vị trí {locationCode}: ")}Vật tư {itemCode} chỉ còn {currentStock:N2} trước điều chỉnh.",
            code: "NEGATIVE_LOCATION_STOCK", entityName: "ItemLocation");

    public static BusinessRuleException TransferInsufficientStock(string itemCode, decimal available)
        => new($"Lỗi: Vật tư {itemCode} không đủ tồn kho để chuyển (chỉ còn {available:N2}).",
            code: "TRANSFER_INSUFFICIENT_STOCK", entityName: "ItemLocation");

    public static BusinessRuleException HsdRequired(string itemCode)
        => new($"[{itemCode}] yêu cầu quản lý hạn sử dụng (HSD). Vui lòng nhập ngày hết hạn.",
            code: "HSD_REQUIRED", entityName: "VoucherDetail");

    public static BusinessRuleException LotRequired(string itemCode)
        => new($"[{itemCode}] yêu cầu quản lý theo lô (Lot). Vui lòng nhập số lô.",
            code: "LOT_REQUIRED", entityName: "VoucherDetail");

    public static BusinessRuleException ConversionRateMismatch(string itemCode)
        => new($"Hệ số quy đổi nhập tay của [{itemCode}] không khớp với bảng quy đổi hệ thống.",
            code: "CONVERSION_RATE_MISMATCH", entityName: "VoucherDetail");

    public static BusinessRuleException InvalidConversionRate(string itemCode)
        => new($"Hệ số quy đổi của [{itemCode}] không hợp lệ (phải > 0).",
            code: "INVALID_CONVERSION_RATE", entityName: "VoucherDetail");

    public static BusinessRuleException SerialNotInteger(string itemCode)
        => new($"[{itemCode}] đang bật quản lý serial nên số lượng xuất phải là số nguyên.",
            code: "SERIAL_NOT_INTEGER", entityName: "SerialNumber");

    public static BusinessRuleException SerialMissing(string itemCode, int required, int actual)
        => new($"[{itemCode}] thiếu serial đã quét cho phần xuất kho. Cần {required}, hiện có {actual}.",
            code: "SERIAL_MISSING", entityName: "SerialNumber");

    public static BusinessRuleException HoldStatusBlocked(string locationCode, string holdStatus, string itemCode)
        => new($"Vị trí {locationCode} có hàng bị giữ (HoldStatus = {holdStatus}). Không thể xuất. Vui lòng liên hệ QC để giải quyết trước.",
            code: "HOLD_STATUS_BLOCKED", entityName: "ItemLocation");

    // Inbound/Outbound validation
    public static BusinessRuleException InboundExpectedArrivalRequired()
        => new("Phiếu nhập trình duyệt cần có thời gian xe dự kiến đến (mã lịch nhận hàng).",
            code: "ARRIVAL_TIME_REQUIRED", entityName: "Voucher");

    public static BusinessRuleException DockAppointmentIncomplete()
        => new("Khung giờ nhận hàng phải nhập đủ cả giờ bắt đầu và giờ kết thúc.",
            code: "DOCK_APPOINTMENT_INCOMPLETE", entityName: "Voucher");

    public static BusinessRuleException DockAppointmentEndBeforeStart()
        => new("Giờ kết thúc khung nhận hàng phải lớn hơn giờ bắt đầu.",
            code: "DOCK_APPOINTMENT_TIME_INVALID", entityName: "Voucher");

    public static BusinessRuleException ArrivalTimeMismatch()
        => new("Giờ xe đến dự kiến đang lệch quá xa so với khung giờ nhận hàng. Vui lòng kiểm tra lại.",
            code: "ARRIVAL_TIME_MISMATCH", entityName: "Voucher");

    public static BusinessRuleException DockConflict(string dockDoor, string voucherCode, DateTime start, DateTime end)
        => new($"Cửa nhận hàng {dockDoor} đã được đặt bởi phiếu {voucherCode} trong khung {start:dd/MM HH:mm} - {end:dd/MM HH:mm}.",
            code: "DOCK_CONFLICT", entityName: "Voucher");

    public static BusinessRuleException NoAvailableStock(int itemId)
        => new($"Không có tồn khả dụng cho vật tư #{itemId}. Không thể giữ chỗ.",
            code: "NO_AVAILABLE_STOCK", entityName: "ItemLocation");

    public static BusinessRuleException NoReservation()
        => new("Phiếu chưa có reservation. Vui lòng Confirm for Picking trước.",
            code: "NO_RESERVATION", entityName: "Voucher");

    public static BusinessRuleException NoPickQty()
        => new("Chưa có số lượng nào được pick để ghi sổ. Có thể dùng tùy chọn hủy phần còn lại.",
            code: "NO_PICK_QTY", entityName: "PickTask");

    public static BusinessRuleException QcHoldBlocked(string blockedItems)
        => new($"Phiếu có vật tư đang bị QC giữ (OnHold/Defect), không thể xuất: {blockedItems}. Vui lòng liên hệ QC để giải quyết trước khi xuất.",
            code: "QC_HOLD_BLOCKED", entityName: "VoucherDetail");

    public static BusinessRuleException PartialShipmentNotAllowed(string? shortfallMessage = null)
        => new(string.IsNullOrEmpty(shortfallMessage)
            ? "Phiếu không cho phép giao thiếu hàng (PartialShipmentAllowed=false)."
            : $"Phiếu không cho phép giao thiếu hàng (PartialShipmentAllowed=false). {shortfallMessage}",
            code: "PARTIAL_SHIPMENT_NOT_ALLOWED", entityName: "Voucher");

    public static BusinessRuleException QtyMismatchForSerial(string itemCode, decimal expected, decimal actual)
        => new($"[{itemCode}] đang bật quản lý serial nên số lượng thực nhập phải là số nguyên (expecting {expected}, got {actual}).",
            code: "QTY_MISMATCH_FOR_SERIAL", entityName: "SerialNumber");

    public static BusinessRuleException SerialCountInsufficient(string itemCode, int required, int actual)
        => new($"[{itemCode}] cần đủ {required} serial trước khi hoàn tất nhập. Hiện mới có {actual}.",
            code: "SERIAL_COUNT_INSUFFICIENT", entityName: "SerialNumber");

    public static BusinessRuleException QtyOutOfRange(string itemCode, decimal min, decimal max)
        => new($"Số lượng thực nhận phải nằm trong khoảng {min:N4} đến {max:N4} cho [{itemCode}].",
            code: "QTY_OUT_OF_RANGE", entityName: "VoucherDetail");

    // Cancel/Edit validation
    public static BusinessRuleException CancelReasonRequired()
        => new("Vui lòng nhập lý do hủy phiếu.",
            code: "CANCEL_REASON_REQUIRED", entityName: "Voucher");

    public static BusinessRuleException CancelReasonTooLong()
        => new("Lý do hủy tối đa 500 ký tự.",
            code: "CANCEL_REASON_TOO_LONG", entityName: "Voucher");

    public static BusinessRuleException VoucherNotFound()
        => new("Không tìm thấy phiếu.",
            code: "VOUCHER_NOT_FOUND", entityName: "Voucher");

    public static BusinessRuleException VoucherAlreadyCancelled()
        => new("Phiếu đã được hủy.",
            code: "VOUCHER_ALREADY_CANCELLED", entityName: "Voucher");

    public static BusinessRuleException VoucherAlreadyApproved()
        => new("Phiếu đã hủy hoặc đã duyệt, không thể chỉnh.",
            code: "VOUCHER_NOT_EDITABLE", entityName: "Voucher");

    public static BusinessRuleException CannotCancelOwnVoucher(string actor)
        => new($"Người tạo phiếu không được tự hủy. Vui lòng chuyển người có thẩm quyền khác xử lý.",
            code: "CANNOT_CANCEL_OWN_VOUCHER", entityName: "Voucher");

    public static BusinessRuleException CannotCancelApprovedVoucher()
        => new("Người đã duyệt phiếu không được tự hủy phiếu này.",
            code: "CANNOT_CANCEL_APPROVED", entityName: "Voucher");

    public static BusinessRuleException UndoLocationNotFound(string itemCode)
        => new($"Không tìm thấy tồn nguồn để hoàn tác cho item {itemCode}.",
            code: "UNDO_LOCATION_NOT_FOUND", entityName: "ItemLocation");

    public static BusinessRuleException UndoTransferDestinationLocationNotFound()
        => new("Không xác định được vị trí đích khi hoàn tác chuyển kho.",
            code: "UNDO_DEST_LOCATION_NOT_FOUND", entityName: "ItemLocation");

    public static BusinessRuleException UndoCancelMakesNegativeLocationStock(string itemCode, decimal currentStock)
        => new($"Hủy phiếu làm âm tồn vị trí cho {itemCode}. Vui lòng hủy các phiếu xuất/chuyển liên quan trước.",
            code: "UNDO_MAKES_NEGATIVE_LOCATION", entityName: "ItemLocation");

    public static BusinessRuleException UndoCancelMakesNegativeItemStock(string itemCode)
        => new($"Hủy phiếu làm âm tổng tồn cho {itemCode}. Vui lòng kiểm tra nghiệp vụ phát sinh sau phiếu này.",
            code: "UNDO_MAKES_NEGATIVE_ITEM", entityName: "Item");

    public static BusinessRuleException UndoAdjustMakesNegativeItemStock(string itemCode)
        => new($"Hủy điều chỉnh làm âm tổng tồn của {itemCode}.",
            code: "UNDO_ADJUST_MAKES_NEGATIVE", entityName: "Item");

    public static BusinessRuleException UndoAdjustMakesNegativeLocationStock(string itemCode, decimal currentStock, string locationCode = "")
        => new($"Hủy điều chỉnh làm âm tồn tại vị trí. Vật tư {itemCode} chỉ còn {currentStock:N2} trước khi hủy.",
            code: "UNDO_ADJUST_MAKES_NEGATIVE_LOCATION", entityName: "ItemLocation");

    public static BusinessRuleException UndoTransferMakesNegativeDestination(string itemCode)
        => new($"Hoàn tác chuyển kho làm âm tồn vị trí đích cho item {itemCode}.",
            code: "UNDO_TRANSFER_MAKES_NEGATIVE", entityName: "ItemLocation");

    public static BusinessRuleException UndoItemLocationNotFound(string itemCode)
        => new($"Không tìm thấy tồn vị trí/lô để hoàn tác cho {itemCode}. Vui lòng kiểm tra dữ liệu tồn kho.",
            code: "UNDO_ITEM_LOCATION_NOT_FOUND", entityName: "ItemLocation");

    // Stock count validation
    public static BusinessRuleException ResponsibilityOutOfRange()
        => new("Điểm trách nhiệm phải nằm trong khoảng 0-100.",
            code: "RESPONSIBILITY_OUT_OF_RANGE", entityName: "StockCountLine");

    public static BusinessRuleException AdjustmentWithVarianceRequiresNotes()
        => new("Khi có sai lệch, phải nhập ghi chú kiểm và lý do chỉnh.",
            code: "ADJUSTMENT_REQUIRES_NOTES", entityName: "StockCountLine");

    public static BusinessRuleException ResponsibilityRequiredWithVariance()
        => new("Khi có sai lệch, điểm trách nhiệm phải lớn hơn 0.",
            code: "RESPONSIBILITY_REQUIRED", entityName: "StockCountLine");

    public static BusinessRuleException DefectExceedsExpected(string itemCode, decimal defect, decimal expected)
        => new($"Dòng vật tư [{itemCode}] có SL lỗi/thiếu ({defect:N2}) lớn hơn SL đích ({expected:N2}). Vui lòng chỉnh lại trước khi duyệt.",
            code: "DEFECT_EXCEEDS_EXPECTED", entityName: "VoucherDetail");

    public static BusinessRuleException StockCountLocationMismatch()
        => new("Phiếu kiểm kê có vị trí không thuộc kho của phiếu. Dừng duyệt để đảm bảo an toàn dữ liệu.",
            code: "LOCATION_MISMATCH", entityName: "StockCountSheet");

    public static BusinessRuleException StockAdjustmentMakesNegativeLocation(string itemCode)
        => new($"Điều chỉnh kiểm kê làm âm tồn vị trí cho mã {itemCode}.",
            code: "ADJUSTMENT_MAKES_NEGATIVE_LOCATION", entityName: "ItemLocation");

    public static BusinessRuleException StockAdjustmentMakesNegativeItem(string itemCode)
        => new($"Điều chỉnh kiểm kê làm âm tồn tổng cho mã {itemCode}.",
            code: "ADJUSTMENT_MAKES_NEGATIVE_ITEM", entityName: "Item");

    public static BusinessRuleException StockAdjustmentNoLotFound(string itemCode)
        => new($"Không tìm được lớp tồn theo lô/hạn cho mã {itemCode} để điều chỉnh giảm.",
            code: "NO_LOT_FOUND", entityName: "ItemLocation");

    public static BusinessRuleException StockAdjustmentInsufficientLotStock(string itemCode)
        => new($"Không đủ tồn theo lô/hạn để điều chỉnh giảm cho mã {itemCode}.",
            code: "INSUFFICIENT_LOT_STOCK", entityName: "ItemLocation");

    public static BusinessRuleException StockAdjustmentNoDefaultLocation(string itemCode)
        => new($"Mã {itemCode} chưa có vị trí mặc định để điều chỉnh tăng. Vui lòng cấu hình vị trí.",
            code: "NO_DEFAULT_LOCATION", entityName: "Item");

    public static BusinessRuleException AdjustmentMakesNegativeLocation(string itemCode)
        => new($"Điều chỉnh làm âm tồn vị trí cho mã {itemCode}.",
            code: "ADJUSTMENT_MAKES_NEGATIVE_LOCATION", entityName: "ItemLocation");

    public static BusinessRuleException AdjustmentMakesNegativeItem(string itemCode)
        => new($"Điều chỉnh làm âm tổng tồn cho mã {itemCode}.",
            code: "ADJUSTMENT_MAKES_NEGATIVE_ITEM", entityName: "Item");

    // Warehouse lock
    public static WarehouseLockedException WarehouseLocked(string voucherDate, DateTime lockDate)
        => new($"Kho đã khóa kỳ đến {lockDate:dd/MM/yyyy}. Không thể tạo phiếu ngày {voucherDate}.",
            lockDate);

    public static WarehouseLockedException WarehouseLockedForCancel(string voucherDate, DateTime lockDate)
        => new($"Kho đã khóa kỳ đến {lockDate:dd/MM/yyyy}. Không thể hủy phiếu ngày {voucherDate}.",
            lockDate);

    public static WarehouseLockedException WarehouseLockedForPost(string voucherDate, DateTime lockDate)
        => new($"Kho đã khóa kỳ đến {lockDate:dd/MM/yyyy}. Không thể chốt xuất phiếu ngày {voucherDate}.",
            lockDate);

    public static WarehouseLockedException WarehouseLockedForApprove(string voucherDate, DateTime lockDate)
        => new($"Kho đã khóa kỳ đến {lockDate:dd/MM/yyyy}. Không thể chốt xuất phiếu ngày {voucherDate}.",
            lockDate);

    // SoD violation
    public static SodViolationException SodViolation(string actor, string action)
        => new($"Vi phạm SoD: bạn [{actor}] là người tạo phiếu nên không được thực hiện action [{action}]. Vui lòng chuyển người khác thực hiện.",
            actor, action);

    // Wave/Pick errors
    public static BusinessRuleException WaveCreationFailed()
        => new("Không thể tạo sóng.",
            code: "WAVE_CREATION_FAILED", entityName: "Wave");

    public static BusinessRuleException WaveCodeDuplicate()
        => new("Không thể tạo mã sóng do trùng. Vui lòng thử lại.",
            code: "WAVE_CODE_DUPLICATE", entityName: "Wave");

    public static BusinessRuleException PickTaskCreationFailed()
        => new("Không thể tạo đợt lấy hàng. Vui lòng thử lại.",
            code: "PICK_TASK_FAILED", entityName: "PickTask");

    public static BusinessRuleException PickTaskCodeDuplicate()
        => new("Không thể tạo mã đợt lấy hàng do trùng mã đồng thời. Vui lòng thử lại.",
            code: "PICK_TASK_DUPLICATE", entityName: "PickTask");

    // Item creation validation
    public static BusinessRuleException CannotCreateNewItem(string searchName)
        => new($"Không thể tự tạo vật tư mới '{searchName ?? ""}'. Nhân viên chỉ được map vào vật tư đã có sẵn.",
            code: "CANNOT_CREATE_NEW_ITEM", entityName: "Item");

    // Misc
    public static BusinessRuleException ScheduledReportNotFound()
        => new("Không tìm thấy báo cáo theo lịch.",
            code: "SCHEDULED_REPORT_NOT_FOUND", entityName: "ScheduledReport");

    public static BusinessRuleException ApproveRequiresInspector()
        => new("Phiếu đã hủy hoặc đã duyệt, không thể chỉnh.",
            code: "VOUCHER_NOT_EDITABLE", entityName: "Voucher");

    public static BusinessRuleException InspectorCannotBeCreator()
        => new("Người kiểm không được trùng người nhập khi chỉnh sai lệch.",
            code: "INSPECTOR_SAME_AS_CREATOR", entityName: "Voucher");

    public static BusinessRuleException DetailNotFound()
        => new("Không tìm thấy dòng chi tiết cần chỉnh.",
            code: "DETAIL_NOT_FOUND", entityName: "VoucherDetail");

    public static BusinessRuleException MaxDefectExceeded(string itemCode, decimal maxDefectQty)
        => new($"SL lỗi phải nằm trong khoảng 0 đến {maxDefectQty:N4} cho [{itemCode}].",
            code: "MAX_DEFECT_EXCEEDED", entityName: "VoucherDetail");

    public static BusinessRuleException ReceivingOnlyForInbound()
        => new("Phiếu đã hủy hoặc đã duyệt, không thể chỉnh.",
            code: "VOUCHER_NOT_EDITABLE", entityName: "Voucher");

    public static BusinessRuleException PickingOnlyForInbound()
        => new("Chỉ cho chỉnh sai lệch với phiếu nhập.",
            code: "ONLY_FOR_INBOUND", entityName: "Voucher");

    public static BusinessRuleException VoucherDetailNotFound()
        => new("Không tìm thấy dòng chi tiết cần chỉnh.",
            code: "VOUCHER_DETAIL_NOT_FOUND", entityName: "VoucherDetail");

    public static BusinessRuleException TransferDestLocationMissing()
        => new("Phiếu chuyển kho thiếu vị trí đích.",
            code: "DEST_LOCATION_MISSING", entityName: "VoucherDetail");

    public static BusinessRuleException TransferDestLocationMissingForSerial()
        => new("Phiếu chuyển kho thiếu vị trí đích cho serial.",
            code: "DEST_LOCATION_MISSING_SERIAL", entityName: "SerialNumber");

    public static BusinessRuleException PickingOnlyForInbounds()
        => new("Chỉ được nhập số lượng thực tế trong bước Receiving.",
            code: "ONLY_DURING_RECEIVING", entityName: "VoucherDetail");

    public static BusinessRuleException ReportAdjustmentCodeFailed()
        => new("Không thể tạo mã phiếu điều chỉnh, vui lòng thử lại.",
            code: "ADJUSTMENT_CODE_FAILED", entityName: "Voucher");

    public static BusinessRuleException ItemDisabled(int itemId)
        => new($"Vật tư ID={itemId} không còn trong hệ thống hoặc đã bị vô hiệu hóa. Không thể sinh phiếu điều chỉnh.",
            code: "ITEM_DISABLED", entityName: "Item");

    public static BusinessRuleException VoucherCodeFailed()
        => new("Không thể tạo mã phiếu. Vui lòng thử lại.",
            code: "VOUCHER_CODE_FAILED", entityName: "Voucher");

    // Operations
    public static BusinessRuleException KichBanEmpty()
        => new("Tên kịch bản không được trống.",
            code: "KICH_BAN_EMPTY", entityName: "KichBan");
}
