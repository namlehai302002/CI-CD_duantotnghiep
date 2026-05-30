using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("ShippingHandoverLogs")]
public class ShippingHandoverLog
{
    [Key]
    public long ShippingHandoverLogId { get; set; }

    public long VoucherId { get; set; }
    public int WarehouseId { get; set; }
    public long? ShipmentLoadId { get; set; }

    [MaxLength(100)]
    public string HandedOverBy { get; set; } = "";

    public DateTime HandedOverAt { get; set; } = VietnamTime.Now;

    [MaxLength(100)]
    public string? TrackingNumber { get; set; }

    [MaxLength(50)]
    public string? ManifestCode { get; set; }

    [MaxLength(100)]
    public string? CarrierName { get; set; }

    [MaxLength(30)]
    public string? VehicleNumber { get; set; }

    [MaxLength(100)]
    public string? DriverName { get; set; }

    [MaxLength(20)]
    public string? DriverPhone { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [ForeignKey(nameof(VoucherId))]
    public Voucher? Voucher { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey(nameof(ShipmentLoadId))]
    public ShipmentLoad? ShipmentLoad { get; set; }
}
