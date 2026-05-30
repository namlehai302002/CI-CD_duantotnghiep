using WMS.Models;

namespace WMS.ViewModels;

public static class EnterpriseUiLabels
{
    public static string MheSystemType(MheSystemTypeEnum type) => type switch
    {
        MheSystemTypeEnum.Wcs => "WCS - hệ điều khiển kho",
        MheSystemTypeEnum.Conveyor => "Băng chuyền",
        MheSystemTypeEnum.Sorter => "Máy chia chọn",
        MheSystemTypeEnum.Robot => "Robot kho",
        MheSystemTypeEnum.Amr => "AMR - xe tự hành",
        _ => "Khác"
    };

    public static string MheCommandStatus(MheCommandStatusEnum status) => status switch
    {
        MheCommandStatusEnum.Pending => "Chờ xử lý",
        MheCommandStatusEnum.Queued => "Đã đưa vào hàng đợi",
        MheCommandStatusEnum.Sent => "Đã gửi",
        MheCommandStatusEnum.Acknowledged => "Thiết bị đã xác nhận",
        MheCommandStatusEnum.InProgress => "Đang thực hiện",
        MheCommandStatusEnum.Completed => "Hoàn tất",
        MheCommandStatusEnum.Failed => "Lỗi",
        MheCommandStatusEnum.Cancelled => "Đã hủy",
        MheCommandStatusEnum.DeadLetter => "Chờ xử lý lỗi",
        _ => "Không xác định"
    };

    public static string MheCommandStatusClass(MheCommandStatusEnum status) => status switch
    {
        MheCommandStatusEnum.Completed or MheCommandStatusEnum.Acknowledged or MheCommandStatusEnum.Sent => "success",
        MheCommandStatusEnum.Failed or MheCommandStatusEnum.DeadLetter or MheCommandStatusEnum.Cancelled => "danger",
        MheCommandStatusEnum.Pending or MheCommandStatusEnum.Queued or MheCommandStatusEnum.InProgress => "warning",
        _ => "neutral"
    };

    public static string AutomationTelemetryType(AutomationTelemetryTypeEnum type) => type switch
    {
        AutomationTelemetryTypeEnum.Heartbeat => "Nhịp kết nối",
        AutomationTelemetryTypeEnum.Throughput => "Năng suất xử lý",
        AutomationTelemetryTypeEnum.Downtime => "Thời gian dừng",
        AutomationTelemetryTypeEnum.Error => "Lỗi thiết bị",
        _ => "Không xác định"
    };

    public static string AutomationScenario(WcsSimulatorScenarioEnum scenario) => scenario switch
    {
        WcsSimulatorScenarioEnum.AcceptAndComplete => "Nhận lệnh và hoàn tất",
        WcsSimulatorScenarioEnum.SorterReject => "Máy chia chọn từ chối kiện",
        WcsSimulatorScenarioEnum.RobotFail => "Robot xử lý thất bại",
        WcsSimulatorScenarioEnum.Timeout => "Quá thời gian phản hồi",
        _ => "Không xác định"
    };

    public static string AutomationScenarioText(string? scenario) => Normalize(scenario) switch
    {
        "acceptandcomplete" => "Nhận lệnh và hoàn tất",
        "sorterreject" => "Máy chia chọn từ chối kiện",
        "robotfail" => "Robot xử lý thất bại",
        "timeout" => "Quá thời gian phản hồi",
        _ => string.IsNullOrWhiteSpace(scenario) ? "Chưa rõ" : scenario.Trim()
    };

    public static string AutomationSourceType(string? sourceType) => Normalize(sourceType) switch
    {
        "wcssimulator" => "Mô phỏng WCS",
        _ => string.IsNullOrWhiteSpace(sourceType) ? "Chưa rõ" : sourceType.Trim()
    };

    public static string AutomationOverrideAction(AutomationOverrideActionEnum action) => action switch
    {
        AutomationOverrideActionEnum.Retry => "Gửi lại",
        AutomationOverrideActionEnum.Cancel => "Hủy lệnh",
        AutomationOverrideActionEnum.Complete => "Đánh dấu hoàn tất",
        AutomationOverrideActionEnum.DeadLetter => "Chuyển hàng lỗi",
        _ => "Không xác định"
    };

    public static string AdapterHealth(string? status) => Normalize(status) switch
    {
        "healthy" => "Ổn định",
        "down" => "Mất kết nối",
        "warning" => "Cảnh báo",
        "unknown" => "Chưa rõ",
        _ => string.IsNullOrWhiteSpace(status) ? "Chưa rõ" : status.Trim()
    };

    public static string TelemetryStatus(string? status) => Normalize(status) switch
    {
        "ok" => "Bình thường",
        "down" => "Mất kết nối",
        "completed" => "Hoàn tất",
        "failed" => "Lỗi",
        "queued" => "Đang chờ",
        "pending" => "Chờ xử lý",
        _ => string.IsNullOrWhiteSpace(status) ? "Chưa rõ" : status.Trim()
    };

    public static string OptimizationStatus(string? status) => Normalize(status) switch
    {
        "recommend" => "Đề xuất áp dụng",
        "review" => "Cần rà soát",
        "readytowave" => "Sẵn sàng lập đợt",
        "inventoryshort" => "Thiếu tồn khả dụng",
        _ => string.IsNullOrWhiteSpace(status) ? "Chưa rõ" : status.Trim()
    };

    public static string ConnectorType(EnterpriseConnectorTypeEnum type) => type switch
    {
        EnterpriseConnectorTypeEnum.Erp => "ERP - quản trị doanh nghiệp",
        EnterpriseConnectorTypeEnum.Tms => "TMS - vận tải",
        EnterpriseConnectorTypeEnum.Oms => "OMS - quản lý đơn hàng",
        _ => "Khác"
    };

    public static string ConnectorHealth(EnterpriseConnectorHealthEnum health) => health switch
    {
        EnterpriseConnectorHealthEnum.Unknown => "Chưa rõ",
        EnterpriseConnectorHealthEnum.Healthy => "Ổn định",
        EnterpriseConnectorHealthEnum.Warning => "Cảnh báo",
        EnterpriseConnectorHealthEnum.Down => "Mất kết nối",
        _ => "Không xác định"
    };

    public static string DeliveryStatus(WebhookDeliveryStatusEnum status) => status switch
    {
        WebhookDeliveryStatusEnum.Pending => "Chờ gửi",
        WebhookDeliveryStatusEnum.Sent => "Đã gửi",
        WebhookDeliveryStatusEnum.Failed => "Lỗi",
        WebhookDeliveryStatusEnum.DeadLetter => "Hàng lỗi",
        _ => "Không xác định"
    };

    public static string OutboxStatus(OutboxStatusEnum status) => status switch
    {
        OutboxStatusEnum.Pending => "Chờ xử lý",
        OutboxStatusEnum.Processing => "Đang xử lý",
        OutboxStatusEnum.Sent => "Đã gửi",
        OutboxStatusEnum.Failed => "Lỗi",
        OutboxStatusEnum.DeadLetter => "Hàng lỗi",
        _ => "Không xác định"
    };

    public static string IntegrationEvent(string? eventType) => Normalize(eventType) switch
    {
        "shipmentposted" => "Phiếu giao hàng đã ghi sổ",
        "asnreceived" => "ASN đã nhận",
        "asnstatuschanged" => "Trạng thái ASN thay đổi",
        "vouchercompleted" => "Phiếu hoàn tất",
        "stockalert" => "Cảnh báo tồn kho",
        "exceptionraised" => "Phát sinh ngoại lệ",
        "recallissued" => "Phát hành thu hồi",
        "wavecompleted" => "Đợt lấy hàng hoàn tất",
        "mhecommanddispatched" => "Lệnh thiết bị đã phát",
        "carriershipmentrequested" => "Yêu cầu vận đơn",
        "carriershipmentcancelled" => "Hủy vận đơn",
        "carriershipmentstatusrequested" => "Yêu cầu cập nhật vận đơn",
        "inventorychanged" => "Tồn kho thay đổi",
        "shipmentconfirmed" => "Giao hàng đã xác nhận",
        "threeplinvoiceissued" => "Hóa đơn kho nhiều chủ hàng đã phát hành",
        "webhookdelivery" => "Điểm nhận tự động",
        "edimessageprocessed" => "Thông điệp EDI đã xử lý",
        _ => string.IsNullOrWhiteSpace(eventType) ? "Chưa rõ" : eventType.Trim()
    };

    public static string TargetSystem(string? targetSystem) => Normalize(targetSystem) switch
    {
        "erp" => "ERP",
        "tms" => "TMS",
        "oms" => "OMS",
        "wcs" => "WCS",
        _ => string.IsNullOrWhiteSpace(targetSystem) ? "Chưa rõ" : targetSystem.Trim()
    };

    public static string DockAppointmentDirection(DockAppointmentDirectionEnum direction) => direction switch
    {
        DockAppointmentDirectionEnum.Inbound => "Nhập kho",
        DockAppointmentDirectionEnum.Outbound => "Xuất kho",
        DockAppointmentDirectionEnum.Transfer => "Luân chuyển",
        _ => "Không xác định"
    };

    public static string DockAppointmentStatus(DockAppointmentStatusEnum status) => status switch
    {
        DockAppointmentStatusEnum.Scheduled => "Đã lên lịch",
        DockAppointmentStatusEnum.CheckedIn => "Đã vào cổng",
        DockAppointmentStatusEnum.AtDock => "Đang tại cửa bến",
        DockAppointmentStatusEnum.Completed => "Hoàn tất",
        DockAppointmentStatusEnum.Cancelled => "Đã hủy",
        DockAppointmentStatusEnum.NoShow => "Không đến",
        _ => "Không xác định"
    };

    public static string YardEvidenceType(YardEvidenceTypeEnum type) => type switch
    {
        YardEvidenceTypeEnum.GateInPhoto => "Ảnh vào cổng",
        YardEvidenceTypeEnum.GateOutPhoto => "Ảnh ra cổng",
        YardEvidenceTypeEnum.SealPhoto => "Ảnh niêm phong",
        YardEvidenceTypeEnum.DriverDocument => "Giấy tờ tài xế",
        YardEvidenceTypeEnum.ContainerCondition => "Tình trạng công-ten-nơ",
        _ => "Khác"
    };

    private static string Normalize(string? value) => string.IsNullOrWhiteSpace(value)
        ? string.Empty
        : value.Trim().Replace("_", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
}
