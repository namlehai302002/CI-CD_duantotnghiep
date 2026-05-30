using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public sealed class YardGateInRequest
{
    public int WarehouseId { get; init; }
    public string TrailerNumber { get; init; } = "";
    public string? ContainerNumber { get; init; }
    public TrailerTypeEnum TrailerType { get; init; } = TrailerTypeEnum.Trailer;
    public string? CarrierName { get; init; }
    public string? SealNumber { get; init; }
    public string? DriverName { get; init; }
    public string? DriverPhone { get; init; }
    public string? VehicleNumber { get; init; }
    public YardVisitPurposeEnum Purpose { get; init; } = YardVisitPurposeEnum.Inbound;
    public long? VoucherId { get; init; }
    public int? YardSpotId { get; init; }
    public string? Notes { get; init; }
}

public interface IYardManagementService
{
    Task<YardSpot> CreateSpotAsync(int warehouseId, string spotCode, string? spotName, YardSpotTypeEnum spotType, YardSpotStatusEnum status, string? notes, string actor);
    Task<YardVisit> GateInAsync(YardGateInRequest request, int? scopedWarehouseId, string actor);
    Task<YardVisit> AssignSpotAsync(long yardVisitId, int yardSpotId, int? scopedWarehouseId, string actor);
    Task<YardVisit> MoveSpotAsync(long yardVisitId, int yardSpotId, int? scopedWarehouseId, string actor);
    Task<YardVisit> GateOutAsync(long yardVisitId, int? scopedWarehouseId, string actor);
    Task<YardVisitEvidence> AddEvidenceAsync(long yardVisitId, YardEvidenceTypeEnum evidenceType, string fileUrl, string? originalFileName, string? contentType, string? fileHashSha256, int? scopedWarehouseId, string actor, string? notes = null);
}

public class YardManagementService : IYardManagementService
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IYardBillingService? _billingService;

    public YardManagementService(AppDbContext db, IUnitOfWork unitOfWork, IYardBillingService? billingService = null)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _billingService = billingService;
    }

    private static DateTime Now => VietnamTime.Now;

    public async Task<YardSpot> CreateSpotAsync(int warehouseId, string spotCode, string? spotName, YardSpotTypeEnum spotType, YardSpotStatusEnum status, string? notes, string actor)
    {
        var normalized = NormalizeCode(spotCode);
        if (string.IsNullOrWhiteSpace(normalized))
            throw new BusinessRuleException("Vui lòng nhập mã vị trí bãi.", "YARD_SPOT_CODE_REQUIRED", "YardSpot");

        var warehouseExists = await _db.Warehouses.AnyAsync(w => w.WarehouseId == warehouseId && w.IsActive);
        if (!warehouseExists)
            throw new BusinessRuleException("Kho không hoạt động hoặc không tồn tại.", "WAREHOUSE_NOT_FOUND", "Warehouse");

        var duplicate = await _db.YardSpots.AnyAsync(s => s.WarehouseId == warehouseId && s.SpotCode == normalized);
        if (duplicate)
            throw new BusinessRuleException($"Vị trí bãi {normalized} đã tồn tại trong kho này.", "YARD_SPOT_DUPLICATE", "YardSpot");

        var spot = new YardSpot
        {
            WarehouseId = warehouseId,
            SpotCode = normalized,
            SpotName = Clean(spotName),
            SpotType = spotType,
            Status = status,
            Notes = Clean(notes),
            CreatedAt = Now
        };

        _db.YardSpots.Add(spot);
        await _unitOfWork.SaveChangesAsync();
        return spot;
    }

    public async Task<YardVisit> GateInAsync(YardGateInRequest request, int? scopedWarehouseId, string actor)
    {
        if (scopedWarehouseId.HasValue && request.WarehouseId != scopedWarehouseId.Value)
            throw new UnauthorizedAccessException("Bạn không thể cho xe vào cổng kho khác.");

        var trailerNumber = NormalizeCode(request.TrailerNumber);
        if (string.IsNullOrWhiteSpace(trailerNumber))
            throw new BusinessRuleException("Vui lòng nhập số xe hoặc số công-ten-nơ.", "TRAILER_NUMBER_REQUIRED", "Trailer");

        var warehouseExists = await _db.Warehouses.AnyAsync(w => w.WarehouseId == request.WarehouseId && w.IsActive);
        if (!warehouseExists)
            throw new BusinessRuleException("Kho không hoạt động hoặc không tồn tại.", "WAREHOUSE_NOT_FOUND", "Warehouse");

        Voucher? voucher = null;
        if (request.VoucherId.HasValue)
        {
            voucher = await _db.Vouchers.FirstOrDefaultAsync(v => v.VoucherId == request.VoucherId.Value && !v.IsCancelled);
            if (voucher == null || voucher.WarehouseId != request.WarehouseId)
                throw new BusinessRuleException("Phiếu liên kết không thuộc kho này.", "YARD_VOUCHER_INVALID", "Voucher");
        }

        var trailer = await _db.Trailers.FirstOrDefaultAsync(t => t.TrailerNumber == trailerNumber);
        if (trailer == null)
        {
            trailer = new Trailer
            {
                TrailerNumber = trailerNumber,
                CreatedAt = Now
            };
            _db.Trailers.Add(trailer);
        }

        trailer.ContainerNumber = NormalizeOptionalCode(request.ContainerNumber);
        trailer.TrailerType = request.TrailerType;
        trailer.CarrierName = Clean(request.CarrierName ?? voucher?.CarrierName);
        trailer.SealNumber = Clean(request.SealNumber);
        trailer.IsActive = true;
        trailer.UpdatedAt = Now;

        var hasActiveVisit = await _db.YardVisits.AnyAsync(v => v.TrailerId == trailer.TrailerId && v.GateOutAt == null && v.Status != YardVisitStatusEnum.Cancelled);
        if (hasActiveVisit)
            throw new BusinessRuleException($"Xe hoặc công-ten-nơ {trailerNumber} đã có lượt vào bãi đang hoạt động.", "YARD_ACTIVE_VISIT_EXISTS", "YardVisit");

        var visit = new YardVisit
        {
            VisitCode = await GenerateVisitCodeAsync(),
            WarehouseId = request.WarehouseId,
            Trailer = trailer,
            Purpose = request.Purpose,
            Status = YardVisitStatusEnum.GatedIn,
            GateInAt = Now,
            GateInBy = actor,
            DriverName = Clean(request.DriverName ?? voucher?.DriverName),
            DriverPhone = Clean(request.DriverPhone ?? voucher?.DriverPhone),
            VehicleNumber = Clean(request.VehicleNumber ?? voucher?.VehicleNumber),
            VoucherId = voucher?.VoucherId,
            DockDoor = voucher?.DockDoor,
            DockAppointmentStart = voucher?.DockAppointmentStart,
            DockAppointmentEnd = voucher?.DockAppointmentEnd,
            Notes = Clean(request.Notes),
            CreatedAt = Now
        };

        _db.YardVisits.Add(visit);
        await _unitOfWork.SaveChangesAsync();

        if (request.YardSpotId.HasValue)
            visit = await AssignSpotAsync(visit.YardVisitId, request.YardSpotId.Value, scopedWarehouseId, actor);

        return visit;
    }

    public Task<YardVisit> AssignSpotAsync(long yardVisitId, int yardSpotId, int? scopedWarehouseId, string actor)
        => SetSpotAsync(yardVisitId, yardSpotId, scopedWarehouseId, actor, isMove: false);

    public Task<YardVisit> MoveSpotAsync(long yardVisitId, int yardSpotId, int? scopedWarehouseId, string actor)
        => SetSpotAsync(yardVisitId, yardSpotId, scopedWarehouseId, actor, isMove: true);

    public async Task<YardVisit> GateOutAsync(long yardVisitId, int? scopedWarehouseId, string actor)
    {
        var visit = await _db.YardVisits
            .Include(v => v.CurrentSpot)
            .FirstOrDefaultAsync(v => v.YardVisitId == yardVisitId);
        if (visit == null)
            throw new BusinessRuleException("Không tìm thấy lượt vào bãi.", "YARD_VISIT_NOT_FOUND", "YardVisit");

        EnsureWarehouseScope(visit.WarehouseId, scopedWarehouseId);
        EnsureActiveVisit(visit);

        var now = Now;
        visit.GateOutAt = now;
        visit.GateOutBy = actor;
        visit.Status = YardVisitStatusEnum.GatedOut;
        visit.UpdatedAt = now;

        if (visit.CurrentSpot != null)
        {
            visit.CurrentSpot.Status = YardSpotStatusEnum.Available;
            visit.CurrentSpot.UpdatedAt = now;
        }

        await _unitOfWork.SaveChangesAsync();

        // P2-03B: Hook tự động tính phí lưu bãi khi gate out
        if (_billingService != null)
        {
            try { await _billingService.AutoChargeOnGateOutAsync(visit.YardVisitId, actor); }
            catch { /* Không chặn gate out nếu billing lỗi */ }
        }

        return visit;
    }

    public async Task<YardVisitEvidence> AddEvidenceAsync(long yardVisitId, YardEvidenceTypeEnum evidenceType, string fileUrl, string? originalFileName, string? contentType, string? fileHashSha256, int? scopedWarehouseId, string actor, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new BusinessRuleException("Can file bang chung cho luot vao/ra bai.", "YARD_EVIDENCE_FILE_REQUIRED", "YardVisitEvidence");

        var visit = await _db.YardVisits
            .Include(v => v.Trailer)
            .FirstOrDefaultAsync(v => v.YardVisitId == yardVisitId);
        if (visit == null)
            throw new BusinessRuleException("Khong tim thay luot vao bai.", "YARD_VISIT_NOT_FOUND", "YardVisit");
        EnsureWarehouseScope(visit.WarehouseId, scopedWarehouseId);

        var evidence = new YardVisitEvidence
        {
            YardVisitId = yardVisitId,
            EvidenceType = evidenceType,
            FileUrl = fileUrl.Trim(),
            OriginalFileName = Clean(originalFileName),
            ContentType = Clean(contentType),
            FileHashSha256 = Clean(fileHashSha256),
            SealNumberSnapshot = visit.Trailer?.SealNumber,
            ContainerNumberSnapshot = visit.Trailer?.ContainerNumber,
            DriverNameSnapshot = visit.DriverName,
            VehicleNumberSnapshot = visit.VehicleNumber,
            Notes = Clean(notes),
            CapturedBy = Clean(actor) ?? "system",
            CapturedAt = Now,
            YardVisit = visit
        };
        _db.YardVisitEvidence.Add(evidence);
        await _unitOfWork.SaveChangesAsync();
        return evidence;
    }

    private async Task<YardVisit> SetSpotAsync(long yardVisitId, int yardSpotId, int? scopedWarehouseId, string actor, bool isMove)
    {
        var visit = await _db.YardVisits
            .Include(v => v.CurrentSpot)
            .FirstOrDefaultAsync(v => v.YardVisitId == yardVisitId);
        if (visit == null)
            throw new BusinessRuleException("Không tìm thấy lượt vào bãi.", "YARD_VISIT_NOT_FOUND", "YardVisit");

        EnsureWarehouseScope(visit.WarehouseId, scopedWarehouseId);
        EnsureActiveVisit(visit);

        var spot = await _db.YardSpots.FirstOrDefaultAsync(s => s.YardSpotId == yardSpotId && s.WarehouseId == visit.WarehouseId && s.IsActive);
        if (spot == null)
            throw new BusinessRuleException("Vị trí bãi không hoạt động hoặc không thuộc kho này.", "YARD_SPOT_INVALID", "YardSpot");

        if (spot.Status is YardSpotStatusEnum.Blocked or YardSpotStatusEnum.Maintenance)
            throw new BusinessRuleException($"Vị trí bãi {spot.SpotCode} đang bị chặn hoặc bảo trì.", "YARD_SPOT_BLOCKED", "YardSpot");

        var occupied = await _db.YardVisits.AnyAsync(v =>
            v.YardVisitId != yardVisitId
            && v.CurrentSpotId == yardSpotId
            && v.GateOutAt == null
            && v.Status != YardVisitStatusEnum.Cancelled);
        if (occupied)
            throw new BusinessRuleException($"Vị trí bãi {spot.SpotCode} đã có xe đang đậu.", "YARD_SPOT_OCCUPIED", "YardSpot");

        var now = Now;
        if (visit.CurrentSpot != null && visit.CurrentSpot.YardSpotId != spot.YardSpotId)
        {
            visit.CurrentSpot.Status = YardSpotStatusEnum.Available;
            visit.CurrentSpot.UpdatedAt = now;
        }

        visit.CurrentSpotId = spot.YardSpotId;
        visit.Status = YardVisitStatusEnum.Parked;
        visit.AssignedSpotAt ??= now;
        visit.LastMovedAt = isMove ? now : visit.LastMovedAt;
        visit.UpdatedAt = now;

        spot.Status = YardSpotStatusEnum.Occupied;
        spot.UpdatedAt = now;

        await _unitOfWork.SaveChangesAsync();
        return visit;
    }

    private async Task<string> GenerateVisitCodeAsync()
    {
        var prefix = $"YV-{Now:yyyyMMdd}-";
        var count = await _db.YardVisits.CountAsync(v => v.VisitCode.StartsWith(prefix));
        return $"{prefix}{count + 1:0000}";
    }

    private static void EnsureWarehouseScope(int warehouseId, int? scopedWarehouseId)
    {
        if (scopedWarehouseId.HasValue && warehouseId != scopedWarehouseId.Value)
            throw new UnauthorizedAccessException("Bạn không thể thao tác lượt vào bãi từ kho khác.");
    }

    private static void EnsureActiveVisit(YardVisit visit)
    {
        if (visit.GateOutAt.HasValue || visit.Status is YardVisitStatusEnum.GatedOut or YardVisitStatusEnum.Cancelled)
            throw new BusinessRuleException("Lượt vào bãi đã kết thúc.", "YARD_VISIT_CLOSED", "YardVisit");
    }

    private static string NormalizeCode(string? value)
        => (value ?? string.Empty).Trim().ToUpperInvariant();

    private static string? NormalizeOptionalCode(string? value)
    {
        var normalized = NormalizeCode(value);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
