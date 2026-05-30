using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public sealed class CarrierShipmentCallbackRequest
{
    public string CorrelationId { get; init; } = "";
    public string IdempotencyKey { get; init; } = "";
    public CarrierShipmentStatusEnum Status { get; init; }
    public string? TrackingNumber { get; init; }
    public string? ExternalShipmentId { get; init; }
    public string? LabelUrl { get; init; }
    public string? ProofOfDeliveryUrl { get; init; }
    public string? Message { get; init; }
    public string? PayloadJson { get; init; }
}

public interface ICarrierIntegrationService
{
    Task<IReadOnlyList<CarrierShipment>> CreateShipmentsForVoucherAsync(long voucherId, int? carrierConnectorId, int? scopedWarehouseId, string actor, CancellationToken ct = default);
    Task<CarrierShipment> RetryAsync(long carrierShipmentId, int? scopedWarehouseId, string actor, CancellationToken ct = default);
    Task<CarrierShipment> CancelAsync(long carrierShipmentId, int? scopedWarehouseId, string actor, CancellationToken ct = default);
    Task<CarrierShipment> SyncStatusAsync(long carrierShipmentId, int? scopedWarehouseId, string actor, CancellationToken ct = default);
    Task<CarrierShipment> ProcessCallbackAsync(CarrierShipmentCallbackRequest request, CancellationToken ct = default);
    Task EnsureShipmentReadyForShippingAsync(long voucherId, CancellationToken ct = default);
}

public class CarrierIntegrationService : ICarrierIntegrationService
{
    private readonly AppDbContext _db;
    private readonly IIntegrationService _integrationService;
    private readonly IUnitOfWork _unitOfWork;

    public CarrierIntegrationService(AppDbContext db, IIntegrationService integrationService, IUnitOfWork? unitOfWork = null)
    {
        _db = db;
        _integrationService = integrationService;
        _unitOfWork = unitOfWork ?? new EfUnitOfWork(db);
    }

    private static DateTime Now => VietnamTime.Now;

    public async Task<IReadOnlyList<CarrierShipment>> CreateShipmentsForVoucherAsync(long voucherId, int? carrierConnectorId, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        var voucher = await _db.Vouchers
            .Include(v => v.Packages)
            .FirstOrDefaultAsync(v => v.VoucherId == voucherId, ct);
        if (voucher == null)
            throw new BusinessRuleException("Không tìm thấy phiếu xuất để tạo vận đơn.", "CARRIER_VOUCHER_NOT_FOUND", "Voucher");
        EnsureWarehouseScope(voucher.WarehouseId, scopedWarehouseId);
        ValidateVoucherReadyForCarrier(voucher);

        var packages = voucher.Packages
            .Where(p => p.WarehouseId == voucher.WarehouseId)
            .OrderBy(p => p.PackageCode)
            .ToList();
        if (packages.Count == 0)
            throw new BusinessRuleException("Phiếu chưa có kiện xuất để tạo vận đơn.", "CARRIER_PACKAGE_REQUIRED", "OutboundPackage");
        if (packages.Any(p => p.OwnerPartnerId != voucher.OwnerPartnerId))
            throw new BusinessRuleException("Kiện xuất không cùng chủ hàng với phiếu, không thể tạo vận đơn.", "CARRIER_PACKAGE_OWNER_MISMATCH", "OutboundPackage");

        var connector = await ResolveConnectorAsync(voucher.WarehouseId, carrierConnectorId, ct);
        var result = new List<CarrierShipment>();
        foreach (var package in packages)
        {
            result.Add(await CreateShipmentForPackageAsync(voucher, package, connector, actor, ct));
        }

        return result;
    }

    public async Task<CarrierShipment> RetryAsync(long carrierShipmentId, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        var shipment = await _db.CarrierShipments
            .Include(s => s.CarrierConnector)
            .Include(s => s.OutboundPackage)
            .Include(s => s.Voucher)
            .FirstOrDefaultAsync(s => s.CarrierShipmentId == carrierShipmentId, ct);
        if (shipment == null)
            throw new BusinessRuleException("Không tìm thấy vận đơn.", "CARRIER_SHIPMENT_NOT_FOUND", "CarrierShipment");
        EnsureWarehouseScope(shipment.WarehouseId, scopedWarehouseId);
        if (shipment.Status is CarrierShipmentStatusEnum.Created or CarrierShipmentStatusEnum.Delivered)
            return shipment;
        if (shipment.Status == CarrierShipmentStatusEnum.Cancelled)
            throw new BusinessRuleException("Vận đơn đã hủy, không thể gửi lại.", "CARRIER_RETRY_CANCELLED", "CarrierShipment");

        shipment.RetryCount += 1;
        shipment.LastError = null;
        shipment.UpdatedAt = Now;
        shipment.UpdatedBy = CleanActor(actor);
        await DispatchOrMockAsync(shipment, shipment.CarrierConnector, actor, $"retry:{shipment.RetryCount}", ct);
        return shipment;
    }

    public async Task<CarrierShipment> CancelAsync(long carrierShipmentId, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        var shipment = await _db.CarrierShipments
            .Include(s => s.CarrierConnector)
            .FirstOrDefaultAsync(s => s.CarrierShipmentId == carrierShipmentId, ct);
        if (shipment == null)
            throw new BusinessRuleException("Không tìm thấy vận đơn.", "CARRIER_SHIPMENT_NOT_FOUND", "CarrierShipment");
        EnsureWarehouseScope(shipment.WarehouseId, scopedWarehouseId);
        if (shipment.Status == CarrierShipmentStatusEnum.Cancelled)
            return shipment;
        if (shipment.Status == CarrierShipmentStatusEnum.Delivered)
            throw new BusinessRuleException("Vận đơn đã giao thành công, không thể hủy.", "CARRIER_CANCEL_DELIVERED", "CarrierShipment");

        shipment.Status = CarrierShipmentStatusEnum.Cancelled;
        shipment.CancelledAt = Now;
        shipment.UpdatedAt = Now;
        shipment.UpdatedBy = CleanActor(actor);
        AddEvent(shipment, CarrierShipmentEventTypeEnum.Cancelled, CarrierShipmentStatusEnum.Cancelled, $"{shipment.IdempotencyKey}:cancel", "Đã hủy vận đơn trong hệ thống.", null);
        await _unitOfWork.SaveChangesAsync(ct);

        if (shipment.CarrierConnector.AdapterType == CarrierAdapterTypeEnum.Http && !string.IsNullOrWhiteSpace(shipment.CarrierConnector.EndpointUrl))
        {
            await EnqueueCarrierOutboxAsync(
                OutboxEventTypeEnum.CarrierShipmentCancelled,
                shipment.CarrierConnector,
                BuildPayload(shipment),
                $"{shipment.IdempotencyKey}:cancel:outbox",
                ct);
        }

        return shipment;
    }

    public async Task<CarrierShipment> SyncStatusAsync(long carrierShipmentId, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        var shipment = await _db.CarrierShipments
            .Include(s => s.CarrierConnector)
            .Include(s => s.OutboundPackage)
            .Include(s => s.Voucher)
            .FirstOrDefaultAsync(s => s.CarrierShipmentId == carrierShipmentId, ct);
        if (shipment == null)
            throw new BusinessRuleException("Không tìm thấy vận đơn.", "CARRIER_SHIPMENT_NOT_FOUND", "CarrierShipment");
        EnsureWarehouseScope(shipment.WarehouseId, scopedWarehouseId);
        if (shipment.Status == CarrierShipmentStatusEnum.Cancelled)
            return shipment;

        if (shipment.CarrierConnector.AdapterType == CarrierAdapterTypeEnum.Mock)
        {
            if (shipment.Status == CarrierShipmentStatusEnum.Created)
            {
                shipment.Status = CarrierShipmentStatusEnum.Delivered;
                shipment.DeliveredAt = Now;
                shipment.ProofOfDeliveryUrl ??= $"/Operations/ShippingDispatch?search={Uri.EscapeDataString(shipment.TrackingNumber ?? shipment.CorrelationId)}";
                shipment.UpdatedAt = Now;
                shipment.UpdatedBy = CleanActor(actor);
            }
            var syncNo = await CountStatusSyncEventsAsync(shipment.CarrierShipmentId, ct) + 1;
            AddEvent(shipment, CarrierShipmentEventTypeEnum.StatusSynced, shipment.Status, $"{shipment.IdempotencyKey}:sync:{syncNo}", "Đồng bộ trạng thái vận đơn giả lập.", null);
            await _unitOfWork.SaveChangesAsync(ct);
            return shipment;
        }

        await EnqueueCarrierOutboxAsync(
            OutboxEventTypeEnum.CarrierShipmentStatusRequested,
            shipment.CarrierConnector,
            BuildPayload(shipment),
            $"{shipment.IdempotencyKey}:status:{await CountStatusSyncEventsAsync(shipment.CarrierShipmentId, ct) + 1}",
            ct);

        var httpSyncNo = await CountStatusSyncEventsAsync(shipment.CarrierShipmentId, ct) + 1;
        AddEvent(shipment, CarrierShipmentEventTypeEnum.StatusSynced, shipment.Status, $"{shipment.IdempotencyKey}:sync:{httpSyncNo}", "Đã gửi yêu cầu đồng bộ trạng thái vận đơn.", null);
        shipment.UpdatedAt = Now;
        shipment.UpdatedBy = CleanActor(actor);
        await _unitOfWork.SaveChangesAsync(ct);
        return shipment;
    }

    public async Task<CarrierShipment> ProcessCallbackAsync(CarrierShipmentCallbackRequest request, CancellationToken ct = default)
    {
        var correlationId = CleanRequired(request.CorrelationId, 80, "mã tương quan");
        var eventKey = CleanRequired(request.IdempotencyKey, 160, "mã chống lặp callback");

        var shipment = await _db.CarrierShipments
            .Include(s => s.OutboundPackage)
            .Include(s => s.Voucher)
            .FirstOrDefaultAsync(s => s.CorrelationId == correlationId, ct);
        if (shipment == null)
            throw new BusinessRuleException("Không tìm thấy vận đơn theo mã tương quan.", "CARRIER_CALLBACK_NOT_FOUND", "CarrierShipment");

        var duplicate = await _db.CarrierShipmentEvents.AnyAsync(e => e.IdempotencyKey == eventKey, ct);
        if (duplicate)
            return shipment;

        ApplyStatus(shipment, request.Status, Clean(request.Message, 500));
        ApplyCarrierReferences(shipment, request.TrackingNumber, request.ExternalShipmentId, request.LabelUrl, request.ProofOfDeliveryUrl);
        shipment.ResponsePayloadJson = string.IsNullOrWhiteSpace(request.PayloadJson) ? "{}" : request.PayloadJson;
        shipment.UpdatedAt = Now;

        AddEvent(shipment, CarrierShipmentEventTypeEnum.Callback, request.Status, eventKey, request.Message, request.PayloadJson);
        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // P2-R2-6: 2 callback song song vượt qua duplicate check ở dòng 188, unique index chặn được ở DB.
            // Nếu row đã tồn tại → idempotent return; ngược lại rethrow.
            var alreadyExists = await _db.CarrierShipmentEvents.AsNoTracking().AnyAsync(e => e.IdempotencyKey == eventKey, ct);
            if (!alreadyExists) throw;
        }
        return shipment;
    }

    public async Task EnsureShipmentReadyForShippingAsync(long voucherId, CancellationToken ct = default)
    {
        var voucher = await _db.Vouchers.AsNoTracking()
            .FirstOrDefaultAsync(v => v.VoucherId == voucherId, ct);
        if (voucher == null)
            throw new BusinessRuleException("Không tìm thấy phiếu xuất.", "CARRIER_VOUCHER_NOT_FOUND", "Voucher");

        var requiringConnectorIds = await _db.CarrierConnectors.AsNoTracking()
            .Where(c => c.WarehouseId == voucher.WarehouseId && c.IsActive && c.RequireShipmentCreatedBeforeShipping)
            .Select(c => c.CarrierConnectorId)
            .ToListAsync(ct);
        if (requiringConnectorIds.Count == 0)
            return;

        var packages = await _db.OutboundPackages.AsNoTracking()
            .Where(p => p.VoucherId == voucherId)
            .Select(p => new { p.OutboundPackageId, p.PackageCode })
            .ToListAsync(ct);
        if (packages.Count == 0)
            throw new BusinessRuleException("Phiếu chưa có kiện xuất, không thể giao hàng.", "CARRIER_PACKAGE_REQUIRED", "OutboundPackage");

        var packageIds = packages.Select(p => p.OutboundPackageId).ToList();
        var readyPackageIds = await _db.CarrierShipments.AsNoTracking()
            .Where(s => packageIds.Contains(s.OutboundPackageId)
                && requiringConnectorIds.Contains(s.CarrierConnectorId)
                && (s.Status == CarrierShipmentStatusEnum.Created || s.Status == CarrierShipmentStatusEnum.Delivered))
            .Select(s => s.OutboundPackageId)
            .Distinct()
            .ToListAsync(ct);

        var readySet = readyPackageIds.ToHashSet();
        var missing = packages.Where(p => !readySet.Contains(p.OutboundPackageId)).Select(p => p.PackageCode).ToList();
        if (missing.Count > 0)
        {
            throw new BusinessRuleException(
                $"Đơn vị vận chuyển đang yêu cầu tạo vận đơn trước khi giao hàng. Các kiện còn thiếu vận đơn hợp lệ: {string.Join(", ", missing.Take(10))}.",
                "CARRIER_SHIPMENT_REQUIRED_BEFORE_SHIPPING",
                "CarrierShipment");
        }
    }

    private async Task<CarrierShipment> CreateShipmentForPackageAsync(Voucher voucher, OutboundPackage package, CarrierConnector connector, string actor, CancellationToken ct)
    {
        var idempotencyKey = $"carrier:create:{connector.CarrierConnectorId}:package:{package.OutboundPackageId}";
        var existing = await _db.CarrierShipments
            .Include(s => s.CarrierConnector)
            .FirstOrDefaultAsync(s => s.IdempotencyKey == idempotencyKey, ct);
        if (existing != null)
            return existing;

        var shipment = new CarrierShipment
        {
            CarrierConnectorId = connector.CarrierConnectorId,
            WarehouseId = voucher.WarehouseId,
            OwnerPartnerId = voucher.OwnerPartnerId,
            VoucherId = voucher.VoucherId,
            OutboundPackageId = package.OutboundPackageId,
            ShipmentLoadId = package.ShipmentLoadId,
            Status = CarrierShipmentStatusEnum.Pending,
            CarrierCodeSnapshot = connector.CarrierCode,
            CarrierNameSnapshot = connector.CarrierName,
            IdempotencyKey = idempotencyKey,
            CorrelationId = Guid.NewGuid().ToString("N"),
            RequestPayloadJson = JsonSerializer.Serialize(BuildRequestPayload(voucher, package, connector)),
            CreatedBy = CleanActor(actor),
            CreatedAt = Now
        };
        _db.CarrierShipments.Add(shipment);
        await _unitOfWork.SaveChangesAsync(ct);

        await DispatchOrMockAsync(shipment, connector, actor, "create", ct);
        return shipment;
    }

    private async Task DispatchOrMockAsync(CarrierShipment shipment, CarrierConnector connector, string actor, string eventSuffix, CancellationToken ct)
    {
        if (connector.AdapterType == CarrierAdapterTypeEnum.Mock)
        {
            var trackingNumber = BuildMockTrackingNumber(connector, shipment.OutboundPackage);
            ApplyStatus(shipment, CarrierShipmentStatusEnum.Created, null);
            ApplyCarrierReferences(
                shipment,
                trackingNumber,
                $"MOCK-{shipment.CarrierShipmentId}",
                $"/Labels/PrintPackage?outboundPackageId={shipment.OutboundPackageId}",
                null);
            shipment.ResponsePayloadJson = JsonSerializer.Serialize(new
            {
                trackingNumber,
                externalShipmentId = shipment.ExternalShipmentId,
                labelUrl = shipment.LabelUrl,
                sandbox = true
            });
            shipment.UpdatedAt = Now;
            shipment.UpdatedBy = CleanActor(actor);
            AddEvent(shipment, CarrierShipmentEventTypeEnum.Created, CarrierShipmentStatusEnum.Created, $"{shipment.IdempotencyKey}:event:{eventSuffix}", "Đã tạo vận đơn giả lập.", shipment.ResponsePayloadJson);
            await _unitOfWork.SaveChangesAsync(ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(connector.EndpointUrl))
        {
            shipment.Status = CarrierShipmentStatusEnum.Failed;
            shipment.LastError = "Cấu hình đơn vị vận chuyển chưa có địa chỉ kết nối.";
            shipment.UpdatedAt = Now;
            shipment.UpdatedBy = CleanActor(actor);
            AddEvent(shipment, CarrierShipmentEventTypeEnum.Failed, CarrierShipmentStatusEnum.Failed, $"{shipment.IdempotencyKey}:event:{eventSuffix}", shipment.LastError, null);
            await _unitOfWork.SaveChangesAsync(ct);
            return;
        }

        shipment.Status = CarrierShipmentStatusEnum.Queued;
        shipment.QueuedAt = Now;
        shipment.UpdatedAt = Now;
        shipment.UpdatedBy = CleanActor(actor);
        AddEvent(shipment, CarrierShipmentEventTypeEnum.Requested, CarrierShipmentStatusEnum.Queued, $"{shipment.IdempotencyKey}:event:{eventSuffix}", "Đã đưa yêu cầu tạo vận đơn vào hàng đợi tích hợp.", shipment.RequestPayloadJson);
        await _unitOfWork.SaveChangesAsync(ct);

        await EnqueueCarrierOutboxAsync(
            OutboxEventTypeEnum.CarrierShipmentRequested,
            connector,
            BuildPayload(shipment),
            $"{shipment.IdempotencyKey}:outbox:{eventSuffix}",
            ct);
    }

    private async Task EnqueueCarrierOutboxAsync(OutboxEventTypeEnum eventType, CarrierConnector connector, object payload, string idempotencyKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await _integrationService.EnqueueAsync(eventType, connector.EndpointUrl ?? "carrier://mock", payload, Clean(idempotencyKey, 100), connector.CarrierCode);
    }

    private async Task<CarrierConnector> ResolveConnectorAsync(int warehouseId, int? carrierConnectorId, CancellationToken ct)
    {
        IQueryable<CarrierConnector> query = _db.CarrierConnectors.Where(c => c.WarehouseId == warehouseId && c.IsActive);
        if (carrierConnectorId.HasValue)
            query = query.Where(c => c.CarrierConnectorId == carrierConnectorId.Value);

        var connector = await query.OrderBy(c => c.CarrierCode).FirstOrDefaultAsync(ct);
        if (connector == null)
            throw new BusinessRuleException("Chưa có cấu hình đơn vị vận chuyển đang hoạt động cho kho này.", "CARRIER_CONNECTOR_NOT_FOUND", "CarrierConnector");
        return connector;
    }

    private Task<int> CountStatusSyncEventsAsync(long carrierShipmentId, CancellationToken ct)
        => _db.CarrierShipmentEvents.CountAsync(e => e.CarrierShipmentId == carrierShipmentId && e.EventType == CarrierShipmentEventTypeEnum.StatusSynced, ct);

    private static void ValidateVoucherReadyForCarrier(Voucher voucher)
    {
        if (voucher.IsCancelled)
            throw new BusinessRuleException("Phiếu đã hủy, không thể tạo vận đơn.", "CARRIER_VOUCHER_CANCELLED", "Voucher");
        if (voucher.VoucherType is not (VoucherTypeEnum.XuatKho or VoucherTypeEnum.TraNCC or VoucherTypeEnum.ChuyenKho or VoucherTypeEnum.XuatSanXuat))
            throw new BusinessRuleException("Chỉ tạo vận đơn cho phiếu xuất hoặc chuyển kho.", "CARRIER_VOUCHER_TYPE_INVALID", "Voucher");
        if (!voucher.IsPosted)
            throw new BusinessRuleException("Phiếu chưa chốt xuất kho, chưa thể tạo vận đơn.", "CARRIER_VOUCHER_NOT_POSTED", "Voucher");
        if (!voucher.PackedAt.HasValue)
            throw new BusinessRuleException("Phiếu chưa đóng gói, chưa thể tạo vận đơn.", "CARRIER_VOUCHER_NOT_PACKED", "Voucher");
        if (voucher.ShippedAt.HasValue)
            throw new BusinessRuleException("Phiếu đã giao hàng, không thể tạo vận đơn mới.", "CARRIER_VOUCHER_SHIPPED", "Voucher");
    }

    private static object BuildRequestPayload(Voucher voucher, OutboundPackage package, CarrierConnector connector)
        => new
        {
            connector.CarrierCode,
            connector.CarrierName,
            connector.IsSandbox,
            voucher.VoucherId,
            voucher.VoucherCode,
            voucher.WarehouseId,
            voucher.OwnerPartnerId,
            voucher.PartnerId,
            voucher.Partner?.PartnerName,
            voucher.RequestedDeliveryDate,
            package.OutboundPackageId,
            package.PackageCode,
            package.TotalQuantity,
            package.ActualCatchWeight,
            package.ReferenceLpnCode
        };

    private static object BuildPayload(CarrierShipment shipment)
        => new
        {
            shipment.CarrierShipmentId,
            shipment.CorrelationId,
            shipment.IdempotencyKey,
            shipment.CarrierCodeSnapshot,
            shipment.CarrierNameSnapshot,
            shipment.WarehouseId,
            shipment.OwnerPartnerId,
            shipment.VoucherId,
            VoucherCode = shipment.Voucher?.VoucherCode,
            shipment.OutboundPackageId,
            PackageCode = shipment.OutboundPackage?.PackageCode,
            shipment.ShipmentLoadId,
            shipment.RequestPayloadJson
        };

    private static void ApplyStatus(CarrierShipment shipment, CarrierShipmentStatusEnum status, string? error)
    {
        shipment.Status = status;
        shipment.LastError = status is CarrierShipmentStatusEnum.Failed or CarrierShipmentStatusEnum.DeliveryFailed ? error : null;
        var now = Now;
        switch (status)
        {
            case CarrierShipmentStatusEnum.Created:
                shipment.CarrierCreatedAt ??= now;
                break;
            case CarrierShipmentStatusEnum.Cancelled:
                shipment.CancelledAt ??= now;
                break;
            case CarrierShipmentStatusEnum.Delivered:
                shipment.DeliveredAt ??= now;
                break;
            case CarrierShipmentStatusEnum.Queued:
                shipment.QueuedAt ??= now;
                break;
            case CarrierShipmentStatusEnum.Failed:
            case CarrierShipmentStatusEnum.DeliveryFailed:
                shipment.LastError = Clean(error, 500);
                break;
        }
    }

    private static void ApplyCarrierReferences(CarrierShipment shipment, string? trackingNumber, string? externalShipmentId, string? labelUrl, string? proofOfDeliveryUrl)
    {
        var tracking = Clean(trackingNumber, 100);
        if (!string.IsNullOrWhiteSpace(tracking))
        {
            shipment.TrackingNumber = tracking;
            if (shipment.OutboundPackage != null && string.IsNullOrWhiteSpace(shipment.OutboundPackage.TrackingNumber))
                shipment.OutboundPackage.TrackingNumber = tracking;
            if (shipment.Voucher != null && string.IsNullOrWhiteSpace(shipment.Voucher.TrackingNumber))
                shipment.Voucher.TrackingNumber = tracking;
        }

        shipment.ExternalShipmentId = Clean(externalShipmentId, 120) ?? shipment.ExternalShipmentId;
        shipment.LabelUrl = Clean(labelUrl, 500) ?? shipment.LabelUrl;
        shipment.ProofOfDeliveryUrl = Clean(proofOfDeliveryUrl, 500) ?? shipment.ProofOfDeliveryUrl;
    }

    private static void AddEvent(CarrierShipment shipment, CarrierShipmentEventTypeEnum eventType, CarrierShipmentStatusEnum status, string idempotencyKey, string? message, string? payloadJson)
    {
        shipment.Events.Add(new CarrierShipmentEvent
        {
            EventType = eventType,
            Status = status,
            IdempotencyKey = CleanRequired(idempotencyKey, 160, "mã chống lặp sự kiện"),
            Message = Clean(message, 500),
            PayloadJson = string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson,
            EventAt = Now
        });
    }

    private static string BuildMockTrackingNumber(CarrierConnector connector, OutboundPackage? package)
    {
        var packageCode = package?.PackageCode ?? Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var raw = $"{connector.CarrierCode}-{packageCode}".Replace(" ", "", StringComparison.Ordinal);
        return raw.Length <= 100 ? raw : raw[..100];
    }

    private static void EnsureWarehouseScope(int warehouseId, int? scopedWarehouseId)
    {
        if (scopedWarehouseId.HasValue && warehouseId != scopedWarehouseId.Value)
            throw new UnauthorizedAccessException("Bạn không có quyền thao tác kho này.");
    }

    private static string CleanActor(string? value)
        => Clean(value, 100) ?? "system";

    private static string? Clean(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string CleanRequired(string? value, int maxLength, string field)
    {
        var cleaned = Clean(value, maxLength);
        if (cleaned == null)
            throw new BusinessRuleException($"Thiếu {field}.", "CARRIER_REQUIRED_FIELD_MISSING", "CarrierShipment");
        return cleaned;
    }
}
