using System.Data;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public interface IOrderStreamingService
{
    Task<WorkflowResult> TryAutoReleaseAsync(long voucherId, string actor, int? scopedWarehouseId = null);
    Task<WorkflowResult> ReleaseNowAsync(long voucherId, string actor, int? scopedWarehouseId = null);
}

public class OrderStreamingService : IOrderStreamingService
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryReservationService _reservationService;
    private readonly ISerialInventoryService _serialInventoryService;
    private static DateTime VietnamNow => VietnamTime.Now;

    private sealed record FefoAllocation(int LocationId, string? LotNumber, DateTime? ExpiryDate, decimal Qty);

    public OrderStreamingService(
        AppDbContext db,
        IUnitOfWork unitOfWork,
        IInventoryReservationService reservationService,
        ISerialInventoryService? serialInventoryService = null)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _reservationService = reservationService;
        _serialInventoryService = serialInventoryService ?? new SerialInventoryService(db);
    }

    public async Task<WorkflowResult> TryAutoReleaseAsync(long voucherId, string actor, int? scopedWarehouseId = null)
    {
        var voucher = await _db.Vouchers.AsNoTracking().FirstOrDefaultAsync(v => v.VoucherId == voucherId);
        if (voucher == null)
            return WorkflowResult.NotFoundResult("Không tìm thấy phiếu.");

        var config = await _db.WarehouseOrderStreamingConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.WarehouseId == voucher.WarehouseId && c.IsActive && c.IsEnabled);
        if (config == null)
            return WorkflowResult.Success("Kho chưa bật phát hành trực tiếp.");

        if (!ShouldAutoRelease(voucher, config))
            return WorkflowResult.Success("Phiếu chưa đạt điều kiện phát hành trực tiếp tự động.");

        return await ReleaseInternalAsync(voucherId, actor, scopedWarehouseId, requireEnabledConfig: true, isAutomatic: true);
    }

    public Task<WorkflowResult> ReleaseNowAsync(long voucherId, string actor, int? scopedWarehouseId = null)
        => ReleaseInternalAsync(voucherId, actor, scopedWarehouseId, requireEnabledConfig: true, isAutomatic: false);

    private static bool ShouldAutoRelease(Voucher voucher, WarehouseOrderStreamingConfig config)
    {
        if (voucher.ServiceLevel is ServiceLevelEnum.SameDay or ServiceLevelEnum.Express)
            return true;
        if (voucher.Priority >= config.MinPriority)
            return true;
        if (voucher.RequestedDeliveryDate.HasValue)
        {
            var deadline = voucher.RequestedDeliveryDate.Value.Date.AddDays(1);
            return deadline <= VietnamNow.AddHours(config.DeliveryWindowHours);
        }
        return false;
    }

    private async Task<WorkflowResult> ReleaseInternalAsync(
        long voucherId,
        string actor,
        int? scopedWarehouseId,
        bool requireEnabledConfig,
        bool isAutomatic)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var voucher = await _db.Vouchers
                .Include(v => v.Details)
                .FirstOrDefaultAsync(v => v.VoucherId == voucherId);
            if (voucher == null)
            {
                await _unitOfWork.RollbackAsync();
                return WorkflowResult.NotFoundResult("Không tìm thấy phiếu.");
            }
            if (scopedWarehouseId.HasValue && voucher.WarehouseId != scopedWarehouseId.Value)
            {
                await _unitOfWork.RollbackAsync();
                return WorkflowResult.ForbiddenResult();
            }
            if (!IsEligibleVoucher(voucher, out var invalidReason))
            {
                await _unitOfWork.RollbackAsync();
                return WorkflowResult.Failure(invalidReason, "Details", new { id = voucherId });
            }

            if (requireEnabledConfig)
            {
                var configExists = await _db.WarehouseOrderStreamingConfigs.AsNoTracking()
                    .AnyAsync(c => c.WarehouseId == voucher.WarehouseId && c.IsActive && c.IsEnabled);
                if (!configExists)
                {
                    await _unitOfWork.RollbackAsync();
                    return WorkflowResult.Failure("Kho chưa bật cấu hình phát hành trực tiếp.", "Details", new { id = voucherId });
                }
            }

            var hasOpenReservation = await _db.StockReservations
                .AnyAsync(r => r.VoucherId == voucher.VoucherId
                    && r.Status == ReservationStatusEnum.Active
                    && (r.ReservedQty - r.ConsumedQty - r.ReleasedQty) > 0);
            var hasOpenTask = await _db.PickTasks
                .AnyAsync(t => t.VoucherId == voucher.VoucherId
                    && t.Status != PickTaskStatusEnum.Completed
                    && t.Status != PickTaskStatusEnum.Cancelled);
            if (hasOpenReservation || hasOpenTask)
            {
                await _unitOfWork.RollbackAsync();
                return WorkflowResult.Failure("Phiếu đã có nhiệm vụ lấy hàng hoặc giữ chỗ đang mở, không thể phát hành trực tiếp lần nữa.", "Details", new { id = voucherId });
            }

            var affectedItemLocationIds = new List<int>();
            var plannedLines = new List<(VoucherDetail Detail, List<FefoAllocation> Allocations)>();
            foreach (var detail in voucher.Details.OrderBy(d => d.LineNumber))
            {
                if (detail.OwnerPartnerId.HasValue && detail.OwnerPartnerId != voucher.OwnerPartnerId)
                {
                    await _unitOfWork.RollbackAsync();
                    return WorkflowResult.Failure("Dòng phiếu không cùng chủ hàng với phiếu xuất.", "Details", new { id = voucherId });
                }
                detail.OwnerPartnerId ??= voucher.OwnerPartnerId;

                var requiredQty = Math.Abs(detail.BaseQty);
                if (requiredQty <= 0) continue;

                var allocations = detail.LocationId.HasValue
                    ? await AllocateFromRequestedLocationOrFefoAsync(detail, voucher.WarehouseId, requiredQty)
                    : await AllocateFefoAsync(detail.ItemId, voucher.WarehouseId, requiredQty, detail.LotNumber, detail.ExpiryDate, voucher.OwnerPartnerId);

                var allocatedQty = allocations.Sum(a => a.Qty);
                if (allocatedQty < requiredQty)
                {
                    await _unitOfWork.RollbackAsync();
                    return WorkflowResult.Failure($"Không đủ tồn khả dụng để phát hành trực tiếp cho dòng hàng [{detail.ItemId}]. Cần {requiredQty:N2}, khả dụng {allocatedQty:N2}.", "Details", new { id = voucherId });
                }
                plannedLines.Add((detail, allocations));
            }

            if (plannedLines.Count == 0)
            {
                await _unitOfWork.RollbackAsync();
                return WorkflowResult.Failure("Phiếu không có dòng hàng hợp lệ để phát hành trực tiếp.", "Details", new { id = voucherId });
            }

            var taskSeq = await _db.PickTasks.CountAsync(t => t.VoucherId == voucher.VoucherId) + 1;
            var createdTaskCount = 0;
            foreach (var planned in plannedLines)
            {
                foreach (var allocation in planned.Allocations)
                {
                    var reservation = new StockReservation
                    {
                        VoucherId = voucher.VoucherId,
                        VoucherDetailId = planned.Detail.VoucherDetailId,
                        ItemId = planned.Detail.ItemId,
                        OwnerPartnerId = voucher.OwnerPartnerId,
                        LocationId = allocation.LocationId,
                        LotNumber = allocation.LotNumber,
                        ExpiryDate = allocation.ExpiryDate,
                        ReservedQty = allocation.Qty,
                        Status = ReservationStatusEnum.Active,
                        CreatedBy = actor,
                        CreatedAt = VietnamNow,
                        Notes = isAutomatic ? "Phát hành trực tiếp tự động" : "Phát hành trực tiếp thủ công"
                    };
                    _db.StockReservations.Add(reservation);

                    var task = new PickTask
                    {
                        TaskCode = await GenerateTaskCodeAsync(voucher.VoucherId, taskSeq++),
                        WaveId = null,
                        VoucherId = voucher.VoucherId,
                        VoucherDetailId = planned.Detail.VoucherDetailId,
                        OwnerPartnerId = voucher.OwnerPartnerId,
                        ItemId = planned.Detail.ItemId,
                        SourceLocationId = allocation.LocationId,
                        LotNumber = allocation.LotNumber,
                        ExpiryDate = allocation.ExpiryDate,
                        TargetQty = allocation.Qty,
                        PickTaskMode = PickTaskModeEnum.Single,
                        IsBatchPick = false,
                        Status = PickTaskStatusEnum.Pending,
                        DueAt = ResolveDueAt(voucher)
                    };
                    _db.PickTasks.Add(task);
                    _db.PickTaskAllocations.Add(new PickTaskAllocation
                    {
                        PickTask = task,
                        StockReservation = reservation,
                        VoucherId = voucher.VoucherId,
                        VoucherDetailId = planned.Detail.VoucherDetailId,
                        AllocatedQty = allocation.Qty,
                        PickedQty = 0
                    });
                    createdTaskCount++;

                    var itemLocationId = await _db.ItemLocations
                        .Where(il => il.ItemId == planned.Detail.ItemId
                            && il.OwnerPartnerId == voucher.OwnerPartnerId
                            && il.LocationId == allocation.LocationId
                            && il.LotNumber == allocation.LotNumber
                            && il.ExpiryDate == allocation.ExpiryDate)
                        .Select(il => (int?)il.ItemLocationId)
                        .FirstOrDefaultAsync();
                    if (itemLocationId.HasValue)
                        affectedItemLocationIds.Add(itemLocationId.Value);
                }
            }

            voucher.FulfillmentStatus = FulfillmentStatusEnum.WaitingForPick;
            voucher.UpdatedAt = VietnamNow;
            await _unitOfWork.SaveChangesAsync();
            await _serialInventoryService.AllocateForVoucherAsync(voucher.VoucherId, actor, $"voucher:{voucher.VoucherId}:direct-release-serial");
            await _reservationService.RecalculateReservedQtyAsync(affectedItemLocationIds);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            return WorkflowResult.Success($"Đã phát hành trực tiếp phiếu {voucher.VoucherCode} và tạo {createdTaskCount} nhiệm vụ lấy hàng.", "Details", new { id = voucherId });
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    private static bool IsEligibleVoucher(Voucher voucher, out string invalidReason)
    {
        invalidReason = "";
        if (voucher.IsCancelled) { invalidReason = "Phiếu đã hủy."; return false; }
        if (voucher.IsPosted) { invalidReason = "Phiếu đã ghi sổ."; return false; }
        if (voucher.WaveId.HasValue) { invalidReason = "Phiếu đã thuộc đợt lấy hàng, không thể phát hành trực tiếp."; return false; }
        if (voucher.FulfillmentStatus >= FulfillmentStatusEnum.WaitingForPick) { invalidReason = "Phiếu đã phát hành lấy hàng."; return false; }
        if (voucher.VoucherType is not (VoucherTypeEnum.XuatKho or VoucherTypeEnum.TraNCC or VoucherTypeEnum.ChuyenKho or VoucherTypeEnum.XuatSanXuat))
        {
            invalidReason = "Chỉ áp dụng phát hành trực tiếp cho phiếu xuất/chuyển kho.";
            return false;
        }
        return true;
    }

    private async Task<List<FefoAllocation>> AllocateFromRequestedLocationOrFefoAsync(VoucherDetail detail, int warehouseId, decimal requiredQty)
    {
        var requested = await _db.ItemLocations
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Where(il => il.ItemId == detail.ItemId
                && il.OwnerPartnerId == detail.OwnerPartnerId
                && il.LocationId == detail.LocationId!.Value
                && il.Location != null
                && il.Location.IsActive
                && il.Location.Zone != null
                && il.Location.Zone.WarehouseId == warehouseId
                && il.HoldStatus == InventoryHoldStatusEnum.Available
                && (il.ExpiryDate == null || il.ExpiryDate >= VietnamNow.Date)
                && (detail.LotNumber == null || il.LotNumber == detail.LotNumber)
                && (!detail.ExpiryDate.HasValue || il.ExpiryDate == detail.ExpiryDate))
            .OrderBy(il => il.ExpiryDate.HasValue ? 0 : 1)
            .ThenBy(il => il.ExpiryDate)
            .ThenByDescending(il => il.Quantity - il.ReservedQty)
            .ThenBy(il => il.ItemLocationId)
            .FirstOrDefaultAsync(il => il.Quantity - il.ReservedQty > 0);
        if (requested != null)
        {
            var available = Math.Max(0, requested.Quantity - requested.ReservedQty);
            if (available >= requiredQty)
                return new List<FefoAllocation> { new(requested.LocationId, requested.LotNumber, requested.ExpiryDate, requiredQty) };
        }
        return await AllocateFefoAsync(detail.ItemId, warehouseId, requiredQty, detail.LotNumber, detail.ExpiryDate, detail.OwnerPartnerId);
    }

    private async Task<List<FefoAllocation>> AllocateFefoAsync(int itemId, int warehouseId, decimal requiredQty, string? lotNumber, DateTime? expiryDate, int? ownerPartnerId)
    {
        var rows = await _db.ItemLocations
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Where(il => il.ItemId == itemId
                && il.OwnerPartnerId == ownerPartnerId
                && il.HoldStatus == InventoryHoldStatusEnum.Available
                && il.Location != null
                && il.Location.IsActive
                && il.Location.Zone != null
                && il.Location.Zone.WarehouseId == warehouseId
                && (il.ExpiryDate == null || il.ExpiryDate >= VietnamNow.Date)
                && (lotNumber == null || il.LotNumber == lotNumber)
                && (!expiryDate.HasValue || il.ExpiryDate == expiryDate))
            .OrderBy(il => il.ExpiryDate.HasValue ? 0 : 1)
            .ThenBy(il => il.ExpiryDate)
            .ThenBy(il => il.Location!.AisleSequence)
            .ThenBy(il => il.Location!.LocationCode)
            .ToListAsync();

        var result = new List<FefoAllocation>();
        var remaining = requiredQty;
        foreach (var row in rows)
        {
            var available = Math.Max(0, row.Quantity - row.ReservedQty);
            if (available <= 0) continue;
            var take = Math.Min(remaining, available);
            result.Add(new FefoAllocation(row.LocationId, row.LotNumber, row.ExpiryDate, take));
            remaining -= take;
            if (remaining <= 0) break;
        }
        return result;
    }

    private static DateTime ResolveDueAt(Voucher voucher)
    {
        if (voucher.RequestedDeliveryDate.HasValue)
            return voucher.RequestedDeliveryDate.Value.Date.AddDays(1).AddTicks(-1);
        if (voucher.SlaHours.HasValue && voucher.SlaHours.Value > 0)
            return VietnamNow.AddHours(voucher.SlaHours.Value);
        return voucher.ServiceLevel switch
        {
            ServiceLevelEnum.SameDay => VietnamNow.Date.AddDays(1).AddTicks(-1),
            ServiceLevelEnum.Express => VietnamNow.AddHours(4),
            _ => VietnamNow.AddHours(24)
        };
    }

    private async Task<string> GenerateTaskCodeAsync(long voucherId, int seq)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = $"PT-TT-{voucherId}-{seq + attempt:D3}";
            if (!await _db.PickTasks.AnyAsync(t => t.TaskCode == code))
                return code;
        }
        throw new BusinessRuleException("Không thể sinh mã nhiệm vụ phát hành trực tiếp duy nhất.");
    }
}
