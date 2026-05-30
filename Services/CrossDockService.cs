using System.Data;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public interface ICrossDockService
{
    Task<WorkflowResult> ExecuteCrossDockAsync(
        long inboundVoucherId,
        long outboundVoucherId,
        int itemId,
        decimal qty,
        int stageLocationId,
        int? scopedWarehouseId,
        string actor,
        string? ipAddress,
        long? inboundVoucherDetailId = null,
        long? outboundVoucherDetailId = null);

    Task<WorkflowResult> CompleteCrossDockTaskAsync(long id);
}

public class CrossDockService : ICrossDockService
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryReservationService _reservationService;
    private readonly IInventoryBalanceService _inventoryBalanceService;
    private readonly IInventoryTransactionService _inventoryTransactionService;

    private static DateTime VietnamNow => VietnamTime.Now;

    public CrossDockService(
        AppDbContext db,
        IUnitOfWork unitOfWork,
        IInventoryReservationService? reservationService = null,
        IInventoryBalanceService? inventoryBalanceService = null,
        IInventoryTransactionService? inventoryTransactionService = null)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _reservationService = reservationService ?? new InventoryReservationService(db);
        _inventoryBalanceService = inventoryBalanceService ?? new InventoryBalanceService(db);
        _inventoryTransactionService = inventoryTransactionService ?? new InventoryTransactionService(db);
    }

    public async Task<WorkflowResult> ExecuteCrossDockAsync(
        long inboundVoucherId,
        long outboundVoucherId,
        int itemId,
        decimal qty,
        int stageLocationId,
        int? scopedWarehouseId,
        string actor,
        string? ipAddress,
        long? inboundVoucherDetailId = null,
        long? outboundVoucherDetailId = null)
    {
        if (qty <= 0)
            return WorkflowResult.Failure("Số lượng phải lớn hơn 0.", "CrossDockOpportunities");

        var inbound = await _db.Vouchers
            .Include(v => v.Details)
            .FirstOrDefaultAsync(v => v.VoucherId == inboundVoucherId && !v.IsCancelled);
        var outbound = await _db.Vouchers
            .Include(v => v.Details)
            .FirstOrDefaultAsync(v => v.VoucherId == outboundVoucherId && !v.IsCancelled);
        var item = await _db.Items.FirstOrDefaultAsync(i => i.ItemId == itemId);
        var stageLocation = await _db.Locations
            .Include(l => l.Zone)
            .FirstOrDefaultAsync(l => l.LocationId == stageLocationId && l.IsActive);

        if (inbound == null || outbound == null || item == null || stageLocation?.Zone == null)
            return WorkflowResult.Failure("Không tìm thấy phiếu, vật tư hoặc vị trí trung chuyển.", "CrossDockOpportunities");

        if (inbound.VoucherType != VoucherTypeEnum.NhapKho || outbound.VoucherType != VoucherTypeEnum.XuatKho)
            return WorkflowResult.Failure("Chuyển thẳng chỉ áp dụng từ phiếu nhập sang phiếu xuất.", "CrossDockOpportunities");

        if (inbound.WarehouseId != outbound.WarehouseId || inbound.WarehouseId != stageLocation.Zone.WarehouseId)
            return WorkflowResult.Failure("Phiếu nhập, phiếu xuất và vị trí trung chuyển phải cùng kho.", "CrossDockOpportunities");

        if (scopedWarehouseId.HasValue && inbound.WarehouseId != scopedWarehouseId.Value)
            return WorkflowResult.ForbiddenResult();

        var inboundDetail = inbound.Details
            .Where(d => d.ItemId == itemId && (!inboundVoucherDetailId.HasValue || d.VoucherDetailId == inboundVoucherDetailId.Value))
            .OrderBy(d => d.ExpiryDate.HasValue ? 0 : 1)
            .ThenBy(d => d.ExpiryDate)
            .ThenBy(d => d.VoucherDetailId)
            .FirstOrDefault();
        var outboundDetail = outbound.Details
            .Where(d => d.ItemId == itemId && (!outboundVoucherDetailId.HasValue || d.VoucherDetailId == outboundVoucherDetailId.Value))
            .OrderBy(d => d.VoucherDetailId)
            .FirstOrDefault();

        if (inboundDetail == null || outboundDetail == null)
            return WorkflowResult.Failure("Không tìm thấy dòng phiếu nhập/xuất phù hợp.", "CrossDockOpportunities");

        if (!LotExpiryCompatible(inboundDetail, outboundDetail))
            return WorkflowResult.Failure("Lô/hạn dùng của phiếu nhập không khớp nhu cầu phiếu xuất.", "CrossDockOpportunities");

        var inboundOpenQty = await GetInboundCrossDockOpenQtyAsync(inboundDetail);
        var outboundOpenQty = await GetOutboundOpenDemandQtyAsync(outboundDetail);
        var executableQty = Math.Min(qty, Math.Min(inboundOpenQty, outboundOpenQty));
        if (executableQty <= 0)
            return WorkflowResult.Failure("Không còn số lượng khả dụng để chuyển thẳng.", "CrossDockOpportunities");

        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            using var ledgerScope = _inventoryTransactionService.BeginScope(new InventoryTransactionContext
            {
                TransactionType = InventoryTransactionTypeEnum.Pick,
                TransactionGroupKey = $"crossdock:{inboundVoucherId}:{outboundVoucherId}:reserve",
                IdempotencyKeyPrefix = $"crossdock:{inboundVoucherId}:{outboundVoucherId}:reserve",
                WarehouseId = inbound.WarehouseId,
                OwnerPartnerId = outbound.OwnerPartnerId,
                VoucherId = outbound.VoucherId,
                VoucherDetailId = outboundDetail.VoucherDetailId,
                ReferenceType = "CrossDockReservation",
                ReferenceId = $"{inboundVoucherId}:{outboundVoucherId}",
                ReferenceCode = $"{inbound.VoucherCode}->{outbound.VoucherCode}",
                Actor = actor
            });
            var taskCode = await GenerateNextCrossDockTaskCodeAsync();
            var reservation = new StockReservation
            {
                VoucherId = outboundVoucherId,
                VoucherDetailId = outboundDetail.VoucherDetailId,
                ItemId = itemId,
                OwnerPartnerId = outbound.OwnerPartnerId,
                LocationId = stageLocationId,
                LotNumber = ResolveLotNumber(inboundDetail, outboundDetail),
                ExpiryDate = ResolveExpiryDate(inboundDetail, outboundDetail),
                ReservedQty = executableQty,
                Status = ReservationStatusEnum.Active,
                CreatedBy = actor,
                CreatedAt = VietnamNow,
                Notes = $"Giữ chỗ chuyển thẳng: {inbound.VoucherCode} -> {outbound.VoucherCode}"
            };
            _db.StockReservations.Add(reservation);

            var task = new CrossDockTask
            {
                TaskCode = taskCode,
                InboundVoucherId = inboundVoucherId,
                InboundVoucherDetailId = inboundDetail.VoucherDetailId,
                OutboundVoucherId = outboundVoucherId,
                OutboundVoucherDetailId = outboundDetail.VoucherDetailId,
                StockReservation = reservation,
                ItemId = itemId,
                StageLocationId = stageLocationId,
                ScheduledQty = executableQty,
                LotNumber = reservation.LotNumber,
                ExpiryDate = reservation.ExpiryDate,
                Status = CrossDockTaskStatusEnum.Pending,
                Notes = $"Chuyển thẳng {item.ItemCode} x {executableQty:N4} từ {inbound.VoucherCode} -> {outbound.VoucherCode}",
                AssignedTo = actor,
                CreatedAt = VietnamNow
            };
            _db.CrossDockTasks.Add(task);
            _db.AuditLogs.Add(new AuditLog
            {
                TableName = "CrossDockTask",
                RecordId = taskCode,
                ActionType = "CROSSDOCK_TASK_CREATED",
                ColumnChanged = "Qty",
                OldValue = $"Inbound:{inbound.VoucherCode}",
                NewValue = $"Outbound:{outbound.VoucherCode};Qty:{executableQty:N4};Stage:{stageLocation.LocationCode}",
                ChangedBy = actor,
                ChangedAt = VietnamNow,
                IpAddress = ipAddress,
                AppModule = "CrossDock"
            });

            await _unitOfWork.SaveChangesAsync();
            var stageItemLocationId = await GetItemLocationIdAsync(itemId, stageLocationId, reservation.LotNumber, reservation.ExpiryDate, reservation.OwnerPartnerId);
            if (stageItemLocationId.HasValue)
                await _reservationService.RecalculateReservedQtyAsync(new[] { stageItemLocationId.Value });
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            return WorkflowResult.Success(
                $"Đã tạo nhiệm vụ chuyển thẳng [{taskCode}]: {item.ItemCode} x {executableQty:N2} - trung chuyển tại {stageLocation.LocationCode}",
                "CrossDockOpportunities",
                new { warehouseId = inbound.WarehouseId });
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<WorkflowResult> CompleteCrossDockTaskAsync(long id)
    {
        var task = await _db.CrossDockTasks
            .Include(t => t.InboundVoucher)
            .Include(t => t.InboundVoucherDetail)
            .Include(t => t.OutboundVoucher)
            .Include(t => t.StockReservation)
            .Include(t => t.StageLocation)
            .FirstOrDefaultAsync(t => t.CrossDockTaskId == id);
        if (task == null)
            return WorkflowResult.Failure("Không tìm thấy nhiệm vụ.", "CrossDockOpportunities");

        if (task.Status != CrossDockTaskStatusEnum.Pending && task.Status != CrossDockTaskStatusEnum.InProgress)
            return WorkflowResult.Failure("Nhiệm vụ đã hoàn tất hoặc bị hủy.", "CrossDockOpportunities");

        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            using var ledgerScope = _inventoryTransactionService.BeginScope(new InventoryTransactionContext
            {
                TransactionType = InventoryTransactionTypeEnum.Move,
                TransactionGroupKey = $"crossdock-task:{task.CrossDockTaskId}:complete",
                IdempotencyKeyPrefix = $"crossdock-task:{task.CrossDockTaskId}:complete",
                WarehouseId = task.InboundVoucher?.WarehouseId,
                OwnerPartnerId = task.StockReservation?.OwnerPartnerId,
                VoucherId = task.OutboundVoucherId,
                VoucherDetailId = task.OutboundVoucherDetailId,
                StockReservationId = task.StockReservationId,
                ReferenceType = "CrossDockTask",
                ReferenceId = task.CrossDockTaskId.ToString(),
                ReferenceCode = task.TaskCode,
                Actor = task.AssignedTo ?? "system"
            });
            var actualQty = task.ScheduledQty;
            var affectedItemLocationIds = new List<int>();
            var stageItemLocation = await GetOrCreateItemLocationAsync(
                task.ItemId,
                task.StageLocationId,
                task.LotNumber,
                task.ExpiryDate,
                task.StockReservation?.OwnerPartnerId);

            if (task.InboundVoucher?.IsPosted == true && task.InboundVoucherDetail?.LocationId.HasValue == true)
            {
                var sourceLocationId = task.InboundVoucherDetail.LocationId.Value;
                if (sourceLocationId != task.StageLocationId)
                {
                    var sourceItemLocation = await _db.ItemLocations
                        .FirstOrDefaultAsync(il => il.ItemId == task.ItemId
                            && il.OwnerPartnerId == task.StockReservation!.OwnerPartnerId
                            && il.LocationId == sourceLocationId
                            && il.LotNumber == task.LotNumber
                            && il.ExpiryDate == task.ExpiryDate);
                    if (sourceItemLocation == null || sourceItemLocation.Quantity < actualQty)
                        return WorkflowResult.Failure("Tồn phiếu nhập không đủ để chuyển sang vị trí trung chuyển.", "CrossDockOpportunities");

                    sourceItemLocation.Quantity -= actualQty;
                    sourceItemLocation.UpdatedAt = VietnamNow;
                    affectedItemLocationIds.Add(sourceItemLocation.ItemLocationId);

                    stageItemLocation.Quantity += actualQty;
                }
            }
            else
            {
                stageItemLocation.Quantity += actualQty;
            }

            stageItemLocation.UpdatedAt = VietnamNow;
            affectedItemLocationIds.Add(stageItemLocation.ItemLocationId);

            if (task.StockReservation != null)
            {
                task.StockReservation.Status = ReservationStatusEnum.Active;
                task.StockReservation.UpdatedAt = VietnamNow;
            }

            task.Status = CrossDockTaskStatusEnum.Completed;
            task.ActualQty = actualQty;
            task.CompletedAt = VietnamNow;

            _db.AuditLogs.Add(new AuditLog
            {
                TableName = "CrossDockTask",
                RecordId = task.TaskCode,
                ActionType = "CROSSDOCK_TASK_COMPLETED",
                ColumnChanged = "Status",
                OldValue = CrossDockTaskStatusEnum.Pending.ToString(),
                NewValue = $"Completed;Qty:{actualQty:N4};Stage:{task.StageLocation?.LocationCode ?? task.StageLocationId.ToString()}",
                ChangedBy = task.AssignedTo,
                ChangedAt = VietnamNow,
                AppModule = "CrossDock"
            });

            await _unitOfWork.SaveChangesAsync();
            await _reservationService.RecalculateReservedQtyAsync(affectedItemLocationIds);
            await _inventoryBalanceService.SyncCurrentStockAsync(new[] { task.ItemId });
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
            return WorkflowResult.Success($"Đã xác nhận hoàn tất chuyển thẳng [{task.TaskCode}].", "CrossDockOpportunities");
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    internal async Task<decimal> GetInboundCrossDockOpenQtyAsync(VoucherDetail inboundDetail)
    {
        var goodQty = Math.Max(0, inboundDetail.BaseQty - GetDefectBaseQty(inboundDetail));
        var alreadyMatched = await _db.CrossDockTasks
            .Where(t => t.InboundVoucherDetailId == inboundDetail.VoucherDetailId
                && t.Status != CrossDockTaskStatusEnum.Cancelled)
            .SumAsync(t => (decimal?)t.ScheduledQty) ?? 0m;
        return Math.Max(0, goodQty - alreadyMatched);
    }

    internal async Task<decimal> GetOutboundOpenDemandQtyAsync(VoucherDetail outboundDetail)
    {
        var demand = Math.Abs(outboundDetail.BaseQty);
        var reservedOrConsumed = await _db.StockReservations
            .Where(r => r.VoucherDetailId == outboundDetail.VoucherDetailId
                && r.Status != ReservationStatusEnum.Released)
            .SumAsync(r => (decimal?)(r.ReservedQty - r.ReleasedQty)) ?? 0m;
        return Math.Max(0, demand - reservedOrConsumed);
    }

    internal static bool LotExpiryCompatible(VoucherDetail inboundDetail, VoucherDetail outboundDetail)
    {
        if (!string.IsNullOrWhiteSpace(outboundDetail.LotNumber)
            && !string.Equals(inboundDetail.LotNumber, outboundDetail.LotNumber, StringComparison.OrdinalIgnoreCase))
            return false;

        if (outboundDetail.ExpiryDate.HasValue && inboundDetail.ExpiryDate != outboundDetail.ExpiryDate)
            return false;

        return true;
    }

    private async Task<string> GenerateNextCrossDockTaskCodeAsync()
    {
        var prefix = $"CD-{VietnamNow:yyyyMMdd}-";
        for (var attempt = 1; attempt <= 50; attempt++)
        {
            var seq = await _db.CrossDockTasks.CountAsync(t => t.TaskCode.StartsWith(prefix)) + attempt;
            var code = $"{prefix}{seq:D5}";
            if (!await _db.CrossDockTasks.AnyAsync(t => t.TaskCode == code))
                return code;
        }

        return $"CD-{VietnamNow:yyyyMMdd}-{Guid.NewGuid():N}"[..30];
    }

    private async Task<ItemLocation> GetOrCreateItemLocationAsync(
        int itemId,
        int locationId,
        string? lotNumber,
        DateTime? expiryDate,
        int? ownerPartnerId)
    {
        var itemLocation = await _db.ItemLocations
            .FirstOrDefaultAsync(il => il.ItemId == itemId
                && il.OwnerPartnerId == ownerPartnerId
                && il.LocationId == locationId
                && il.LotNumber == lotNumber
                && il.ExpiryDate == expiryDate);
        if (itemLocation != null)
            return itemLocation;

        itemLocation = new ItemLocation
        {
            ItemId = itemId,
            OwnerPartnerId = ownerPartnerId,
            LocationId = locationId,
            LotNumber = lotNumber,
            ExpiryDate = expiryDate,
            Quantity = 0,
            UpdatedAt = VietnamNow
        };
        _db.ItemLocations.Add(itemLocation);
        await _unitOfWork.SaveChangesAsync();
        return itemLocation;
    }

    private async Task<int?> GetItemLocationIdAsync(int itemId, int locationId, string? lotNumber, DateTime? expiryDate, int? ownerPartnerId)
    {
        return await _db.ItemLocations
            .Where(il => il.ItemId == itemId
                && il.OwnerPartnerId == ownerPartnerId
                && il.LocationId == locationId
                && il.LotNumber == lotNumber
                && il.ExpiryDate == expiryDate)
            .Select(il => (int?)il.ItemLocationId)
            .FirstOrDefaultAsync();
    }

    private static decimal GetDefectBaseQty(VoucherDetail detail)
    {
        // P2-R2-1: đồng bộ với round 1 — ConversionRate âm đã bị reject ở write-path,
        // không cần Math.Abs (nuốt dấu sẽ che lỗi nếu somehow rate âm slip in).
        var rate = detail.ConversionRate <= 0 ? 1m : detail.ConversionRate;
        return detail.DefectBaseQty > 0 ? detail.DefectBaseQty : detail.DefectQty * rate;
    }

    private static string? ResolveLotNumber(VoucherDetail inboundDetail, VoucherDetail outboundDetail)
        => !string.IsNullOrWhiteSpace(outboundDetail.LotNumber) ? outboundDetail.LotNumber : inboundDetail.LotNumber;

    private static DateTime? ResolveExpiryDate(VoucherDetail inboundDetail, VoucherDetail outboundDetail)
        => outboundDetail.ExpiryDate ?? inboundDetail.ExpiryDate;
}
