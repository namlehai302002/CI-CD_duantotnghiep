using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace WMS.Common;

/// <summary>
/// P3.2: WMS-specific OpenTelemetry metrics.
/// Records voucher lifecycle timing, SLA compliance, and operational KPIs.
/// </summary>
public class WmsMetrics
{
    public const string MeterName = "WMS.Operations";
    public static readonly ActivitySource ActivitySource = new("WMS.Tracing", "1.0.0");

    private readonly Meter _meter;
    private readonly Counter<long> _voucherCreated;
    private readonly Counter<long> _voucherApproved;
    private readonly Counter<long> _voucherPosted;
    private readonly Counter<long> _pickTaskCompleted;
    private readonly Counter<long> _slaBreached;
    private readonly Histogram<double> _dockToStockDuration;
    private readonly Histogram<double> _pickDuration;
    private readonly Histogram<double> _shipDuration;

    public WmsMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0");

        _voucherCreated = _meter.CreateCounter<long>(
            "wms.voucher.created",
            description: "Total vouchers created");

        _voucherApproved = _meter.CreateCounter<long>(
            "wms.voucher.approved",
            description: "Total vouchers approved");

        _voucherPosted = _meter.CreateCounter<long>(
            "wms.voucher.posted",
            description: "Total vouchers posted (inventory confirmed)");

        _pickTaskCompleted = _meter.CreateCounter<long>(
            "wms.picktask.completed",
            description: "Total pick tasks completed");

        _slaBreached = _meter.CreateCounter<long>(
            "wms.sla.breached",
            description: "Total SLA breaches detected");

        _dockToStockDuration = _meter.CreateHistogram<double>(
            "wms.voucher.dock_to_stock_minutes",
            unit: "min",
            description: "Time from dock appointment to stock confirmed (minutes)");

        _pickDuration = _meter.CreateHistogram<double>(
            "wms.picktask.duration_minutes",
            unit: "min",
            description: "Time to complete a pick task (minutes)");

        _shipDuration = _meter.CreateHistogram<double>(
            "wms.shipment.duration_minutes",
            unit: "min",
            description: "Time from order creation to shipment (minutes)");
    }

    public Activity? StartActivity(string name)
        => ActivitySource.StartActivity(name, ActivityKind.Internal);

    public void RecordVoucherCreated(string voucherType, string warehouseCode)
    {
        _voucherCreated.Add(1,
            new KeyValuePair<string, object?>("voucher.type", voucherType),
            new KeyValuePair<string, object?>("warehouse", warehouseCode));
    }

    public void RecordVoucherApproved(string voucherType)
    {
        _voucherApproved.Add(1,
            new KeyValuePair<string, object?>("voucher.type", voucherType));
    }

    public void RecordVoucherPosted(string voucherType)
    {
        _voucherPosted.Add(1,
            new KeyValuePair<string, object?>("voucher.type", voucherType));
    }

    public void RecordPickTaskCompleted(int warehouseId)
    {
        _pickTaskCompleted.Add(1,
            new KeyValuePair<string, object?>("warehouse.id", warehouseId));
    }

    public void RecordSlaBreach(string slaType, string warehouseCode)
    {
        _slaBreached.Add(1,
            new KeyValuePair<string, object?>("sla.type", slaType),
            new KeyValuePair<string, object?>("warehouse", warehouseCode));
    }

    public void RecordDockToStockMinutes(double minutes, string warehouseCode)
    {
        _dockToStockDuration.Record(minutes,
            new KeyValuePair<string, object?>("warehouse", warehouseCode));
    }

    public void RecordPickDurationMinutes(double minutes, string warehouseCode)
    {
        _pickDuration.Record(minutes,
            new KeyValuePair<string, object?>("warehouse", warehouseCode));
    }

    public void RecordShipDurationMinutes(double minutes, string warehouseCode)
    {
        _shipDuration.Record(minutes,
            new KeyValuePair<string, object?>("warehouse", warehouseCode));
    }
}
