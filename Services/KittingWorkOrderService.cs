using System.Data;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public interface IKittingWorkOrderService
{
    Task<KittingWorkOrder> CreateFromBomAsync(CreateKittingWorkOrderCommand command, string actor);
    Task<KittingWorkOrder> ReserveAsync(long workOrderId, string actor);
    Task<KittingWorkOrder> CompleteAsync(long workOrderId, string actor);
    Task<KittingWorkOrder> CancelAsync(long workOrderId, string reason, string actor);
}

public class CreateKittingWorkOrderCommand
{
    public int WarehouseId { get; set; }
    public int? OwnerPartnerId { get; set; }
    public int FinishedItemId { get; set; }
    public int FinishedLocationId { get; set; }
    public decimal PlannedQty { get; set; }
    public string? FinishedLotNumber { get; set; }
    public DateTime? FinishedExpiryDate { get; set; }
    public string? Notes { get; set; }
}

public class KittingWorkOrderService : IKittingWorkOrderService
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryReservationService _reservationService;
    private readonly IInventoryBalanceService _inventoryBalanceService;
    private readonly IInventoryTransactionService _inventoryTransactionService;

    public KittingWorkOrderService(
        AppDbContext db,
        IUnitOfWork unitOfWork,
        IInventoryReservationService reservationService,
        IInventoryBalanceService inventoryBalanceService,
        IInventoryTransactionService? inventoryTransactionService = null)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _reservationService = reservationService;
        _inventoryBalanceService = inventoryBalanceService;
        _inventoryTransactionService = inventoryTransactionService ?? new InventoryTransactionService(db);
    }

    public async Task<KittingWorkOrder> CreateFromBomAsync(CreateKittingWorkOrderCommand command, string actor)
    {
        var now = VietnamTime.Now;
        ValidateWholePositiveQty(command.PlannedQty);

        var warehouseExists = await _db.Warehouses.AnyAsync(w => w.WarehouseId == command.WarehouseId && w.IsActive);
        if (!warehouseExists)
            throw new BusinessRuleException("Không tìm thấy kho hoặc kho đã bị khóa.", "KITTING_WAREHOUSE_NOT_FOUND", nameof(Warehouse));

        var finishedItem = await _db.Items
            .Include(i => i.BaseUom)
            .FirstOrDefaultAsync(i => i.ItemId == command.FinishedItemId && i.IsActive);
        if (finishedItem == null)
            throw new BusinessRuleException("Không tìm thấy mã hàng bộ thành phẩm.", "KITTING_FINISHED_ITEM_NOT_FOUND", nameof(Item));
        if (finishedItem.TrackSerial)
            throw new BusinessRuleException("P3-01 chưa hỗ trợ ráp bộ cho mã hàng bộ quản lý số sê-ri. Vui lòng tắt quản lý số sê-ri hoặc dùng quy trình truy vết số sê-ri riêng.", "KITTING_SERIAL_NOT_SUPPORTED", nameof(Item));

        if (finishedItem.TrackLot && string.IsNullOrWhiteSpace(command.FinishedLotNumber))
            throw new BusinessRuleException("Mã hàng bộ đang quản lý theo lô, vui lòng nhập số lô thành phẩm.", "KITTING_FINISHED_LOT_REQUIRED", nameof(KittingWorkOrder));
        if (finishedItem.TrackExpiry && !command.FinishedExpiryDate.HasValue)
            throw new BusinessRuleException("Mã hàng bộ đang quản lý hạn sử dụng, vui lòng nhập HSD thành phẩm.", "KITTING_FINISHED_EXPIRY_REQUIRED", nameof(KittingWorkOrder));

        var destinationOk = await IsLocationInWarehouseAsync(command.FinishedLocationId, command.WarehouseId);
        if (!destinationOk)
            throw new BusinessRuleException("Vị trí nhập thành phẩm không thuộc kho đã chọn.", "KITTING_DESTINATION_INVALID", nameof(Location));

        var bomLines = await LoadActiveBomAsync(command.FinishedItemId, now);
        if (bomLines.Count == 0)
            throw new BusinessRuleException("Mã hàng bộ chưa có định mức nguyên vật liệu đang hiệu lực tại ngày tạo lệnh.", "KITTING_BOM_NOT_FOUND", nameof(BillOfMaterial));

        var serialComponent = bomLines.FirstOrDefault(b => b.ChildItem?.TrackSerial == true);
        if (serialComponent?.ChildItem != null)
            throw new BusinessRuleException($"P3-01 chưa hỗ trợ vật tư thành phần quản lý số sê-ri [{serialComponent.ChildItem.ItemCode}].", "KITTING_SERIAL_COMPONENT_NOT_SUPPORTED", nameof(Item));

        var workOrder = new KittingWorkOrder
        {
            WorkOrderCode = await GenerateWorkOrderCodeAsync(now),
            WarehouseId = command.WarehouseId,
            OwnerPartnerId = command.OwnerPartnerId ?? finishedItem.OwnerPartnerId,
            FinishedItemId = command.FinishedItemId,
            FinishedLocationId = command.FinishedLocationId,
            PlannedQty = command.PlannedQty,
            FinishedLotNumber = Normalize(command.FinishedLotNumber),
            FinishedExpiryDate = command.FinishedExpiryDate?.Date,
            Status = KittingWorkOrderStatusEnum.Draft,
            Notes = Normalize(command.Notes),
            CreatedBy = actor,
            CreatedAt = now
        };

        foreach (var bom in bomLines)
        {
            var requiredQty = CalculateRequiredQty(command.PlannedQty, bom);
            if (requiredQty <= 0) continue;

            workOrder.Lines.Add(new KittingWorkOrderLine
            {
                OwnerPartnerId = workOrder.OwnerPartnerId,
                ComponentItemId = bom.ChildItemId,
                RequiredQty = requiredQty,
                Status = KittingWorkOrderLineStatusEnum.Draft,
                CreatedAt = now,
                Notes = $"Định mức #{bom.BomId}, phế phẩm {bom.ScrapPercent:N2}%"
            });
        }

        if (workOrder.Lines.Count == 0)
            throw new BusinessRuleException("Định mức nguyên vật liệu không có dòng vật tư thành phần hợp lệ để tạo lệnh ráp bộ.", "KITTING_BOM_EMPTY", nameof(BillOfMaterial));

        _db.KittingWorkOrders.Add(workOrder);
        await _db.SaveChangesAsync();
        return workOrder;
    }

    public async Task<KittingWorkOrder> ReserveAsync(long workOrderId, string actor)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var now = VietnamTime.Now;
            var workOrder = await LoadWorkOrderForUpdateAsync(workOrderId);
            using var ledgerScope = _inventoryTransactionService.BeginScope(new InventoryTransactionContext
            {
                TransactionType = InventoryTransactionTypeEnum.Pick,
                TransactionGroupKey = $"kitting:{workOrderId}:reserve",
                IdempotencyKeyPrefix = $"kitting:{workOrderId}:reserve",
                WarehouseId = workOrder.WarehouseId,
                OwnerPartnerId = workOrder.OwnerPartnerId,
                ReferenceType = "KittingWorkOrder",
                ReferenceId = workOrder.KittingWorkOrderId.ToString(),
                ReferenceCode = workOrder.WorkOrderCode,
                Actor = actor
            });
            if (workOrder.Status != KittingWorkOrderStatusEnum.Draft)
                throw new BusinessRuleException("Chỉ lệnh nháp mới được giữ chỗ vật tư thành phần.", "KITTING_INVALID_STATUS", nameof(KittingWorkOrder));

            var requiredByItem = workOrder.Lines
                .GroupBy(l => l.ComponentItemId)
                .Select(g => new { ComponentItemId = g.Key, RequiredQty = g.Sum(x => x.RequiredQty) })
                .ToList();
            if (requiredByItem.Count == 0)
                throw new BusinessRuleException("Lệnh ráp bộ chưa có vật tư thành phần để giữ chỗ.", "KITTING_NO_COMPONENTS", nameof(KittingWorkOrderLine));

            var allocatedLines = new List<KittingWorkOrderLine>();
            var affectedItemLocationIds = new List<int>();
            var tempAllocatedByItemLocation = new Dictionary<int, decimal>();

            foreach (var item in requiredByItem)
            {
                var allocations = await AllocateFefoAsync(workOrder.WarehouseId, item.ComponentItemId, item.RequiredQty, tempAllocatedByItemLocation, workOrder.OwnerPartnerId);
                foreach (var allocation in allocations)
                {
                    tempAllocatedByItemLocation[allocation.ItemLocationId] =
                        tempAllocatedByItemLocation.GetValueOrDefault(allocation.ItemLocationId) + allocation.Qty;
                    affectedItemLocationIds.Add(allocation.ItemLocationId);

                    allocatedLines.Add(new KittingWorkOrderLine
                    {
                        KittingWorkOrderId = workOrder.KittingWorkOrderId,
                        OwnerPartnerId = workOrder.OwnerPartnerId,
                        ComponentItemId = item.ComponentItemId,
                        SourceLocationId = allocation.LocationId,
                        SourceItemLocationId = allocation.ItemLocationId,
                        RequiredQty = allocation.Qty,
                        ReservedQty = allocation.Qty,
                        LotNumber = allocation.LotNumber,
                        ExpiryDate = allocation.ExpiryDate,
                        Status = KittingWorkOrderLineStatusEnum.Reserved,
                        CreatedAt = now,
                        Notes = "Giữ chỗ tự động theo nguyên tắc hết hạn trước xuất trước"
                    });
                }
            }

            _db.KittingWorkOrderLines.RemoveRange(workOrder.Lines);
            _db.KittingWorkOrderLines.AddRange(allocatedLines);
            workOrder.Status = KittingWorkOrderStatusEnum.Reserved;
            workOrder.ReservedAt = now;
            workOrder.ReservedBy = actor;
            workOrder.UpdatedAt = now;

            await _unitOfWork.SaveChangesAsync();
            await _reservationService.RecalculateReservedQtyAsync(affectedItemLocationIds);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            return await LoadWorkOrderForDisplayAsync(workOrderId);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<KittingWorkOrder> CompleteAsync(long workOrderId, string actor)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var now = VietnamTime.Now;
            var workOrder = await LoadWorkOrderForUpdateAsync(workOrderId);
            using var ledgerScope = _inventoryTransactionService.BeginScope(new InventoryTransactionContext
            {
                TransactionType = InventoryTransactionTypeEnum.KitConsume,
                TransactionGroupKey = $"kitting:{workOrderId}:complete",
                IdempotencyKeyPrefix = $"kitting:{workOrderId}:complete",
                WarehouseId = workOrder.WarehouseId,
                OwnerPartnerId = workOrder.OwnerPartnerId,
                ReferenceType = "KittingWorkOrder",
                ReferenceId = workOrder.KittingWorkOrderId.ToString(),
                ReferenceCode = workOrder.WorkOrderCode,
                Actor = actor
            });
            if (workOrder.Status != KittingWorkOrderStatusEnum.Reserved)
                throw new BusinessRuleException("Chỉ lệnh đã giữ chỗ đủ vật tư thành phần mới được hoàn tất.", "KITTING_INVALID_STATUS", nameof(KittingWorkOrder));

            var finishedItem = await _db.Items.FindAsync(workOrder.FinishedItemId)
                ?? throw new BusinessRuleException("Không tìm thấy mã hàng bộ thành phẩm.", "KITTING_FINISHED_ITEM_NOT_FOUND", nameof(Item));
            if (finishedItem.TrackLot && string.IsNullOrWhiteSpace(workOrder.FinishedLotNumber))
                throw new BusinessRuleException("Mã hàng bộ đang quản lý theo lô, vui lòng nhập số lô thành phẩm.", "KITTING_FINISHED_LOT_REQUIRED", nameof(KittingWorkOrder));
            if (finishedItem.TrackExpiry && !workOrder.FinishedExpiryDate.HasValue)
                throw new BusinessRuleException("Mã hàng bộ đang quản lý hạn sử dụng, vui lòng nhập HSD thành phẩm.", "KITTING_FINISHED_EXPIRY_REQUIRED", nameof(KittingWorkOrder));

            var reservedLines = workOrder.Lines.Where(l => l.Status == KittingWorkOrderLineStatusEnum.Reserved).ToList();
            if (reservedLines.Count == 0 || reservedLines.Any(l => l.SourceItemLocationId == null || l.SourceLocationId == null || l.ReservedQty <= 0))
                throw new BusinessRuleException("Lệnh ráp bộ chưa có dòng giữ chỗ hợp lệ.", "KITTING_RESERVATION_INCOMPLETE", nameof(KittingWorkOrderLine));

            var affectedItemLocationIds = new List<int>();
            var affectedItemIds = new HashSet<int> { workOrder.FinishedItemId };

            foreach (var line in reservedLines)
            {
                var sourceItemLocationId = line.SourceItemLocationId!.Value;
                var sourceLocationId = line.SourceLocationId!.Value;
                var source = await _db.ItemLocations.FirstOrDefaultAsync(il =>
                    il.ItemLocationId == sourceItemLocationId
                    && il.ItemId == line.ComponentItemId
                    && il.OwnerPartnerId == line.OwnerPartnerId
                    && il.LocationId == sourceLocationId
                    && il.LotNumber == line.LotNumber
                    && il.ExpiryDate == line.ExpiryDate);

                if (source == null)
                    throw new BusinessRuleException("Không tìm thấy tồn kho vật tư thành phần đã giữ chỗ.", "KITTING_SOURCE_STOCK_NOT_FOUND", nameof(ItemLocation));
                if (source.HoldStatus != InventoryHoldStatusEnum.Available)
                    throw new BusinessRuleException("Vật tư thành phần đang bị giữ chất lượng, không thể hoàn tất lệnh ráp bộ.", "KITTING_SOURCE_HOLD_BLOCKED", nameof(ItemLocation));
                if (source.Quantity < line.ReservedQty)
                    throw new BusinessRuleException("Tồn thực tế của vật tư thành phần đã thay đổi, không đủ số lượng để tiêu hao.", "KITTING_SOURCE_STOCK_CHANGED", nameof(ItemLocation));

                source.Quantity -= line.ReservedQty;
                source.UpdatedAt = now;

                line.ConsumedQty = line.ReservedQty;
                line.Status = KittingWorkOrderLineStatusEnum.Consumed;
                line.UpdatedAt = now;

                affectedItemLocationIds.Add(source.ItemLocationId);
                affectedItemIds.Add(line.ComponentItemId);
            }

            var destination = await _db.ItemLocations.FirstOrDefaultAsync(il =>
                il.ItemId == workOrder.FinishedItemId
                && il.OwnerPartnerId == workOrder.OwnerPartnerId
                && il.LocationId == workOrder.FinishedLocationId
                && il.LotNumber == workOrder.FinishedLotNumber
                && il.ExpiryDate == workOrder.FinishedExpiryDate);
            if (destination == null)
            {
                destination = new ItemLocation
                {
                    ItemId = workOrder.FinishedItemId,
                    OwnerPartnerId = workOrder.OwnerPartnerId,
                    LocationId = workOrder.FinishedLocationId,
                    Quantity = 0,
                    ReservedQty = 0,
                    LotNumber = workOrder.FinishedLotNumber,
                    ExpiryDate = workOrder.FinishedExpiryDate,
                    HoldStatus = InventoryHoldStatusEnum.Available,
                    UpdatedAt = now
                };
                _db.ItemLocations.Add(destination);
            }

            destination.Quantity += workOrder.PlannedQty;
            destination.UpdatedAt = now;

            workOrder.CompletedQty = workOrder.PlannedQty;
            workOrder.Status = KittingWorkOrderStatusEnum.Completed;
            workOrder.CompletedAt = now;
            workOrder.CompletedBy = actor;
            workOrder.UpdatedAt = now;

            await _unitOfWork.SaveChangesAsync();
            await _reservationService.RecalculateReservedQtyAsync(affectedItemLocationIds);
            await _unitOfWork.SaveChangesAsync();
            await _inventoryBalanceService.SyncCurrentStockAsync(affectedItemIds);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            return await LoadWorkOrderForDisplayAsync(workOrderId);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<KittingWorkOrder> CancelAsync(long workOrderId, string reason, string actor)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var now = VietnamTime.Now;
            var workOrder = await LoadWorkOrderForUpdateAsync(workOrderId);
            using var ledgerScope = _inventoryTransactionService.BeginScope(new InventoryTransactionContext
            {
                TransactionType = InventoryTransactionTypeEnum.Cancel,
                TransactionGroupKey = $"kitting:{workOrderId}:cancel",
                IdempotencyKeyPrefix = $"kitting:{workOrderId}:cancel",
                WarehouseId = workOrder.WarehouseId,
                ReferenceType = "KittingWorkOrder",
                ReferenceId = workOrder.KittingWorkOrderId.ToString(),
                ReferenceCode = workOrder.WorkOrderCode,
                Actor = actor
            });
            if (workOrder.Status == KittingWorkOrderStatusEnum.Completed)
                throw new BusinessRuleException("Không thể hủy lệnh ráp bộ đã hoàn tất.", "KITTING_COMPLETED_CANNOT_CANCEL", nameof(KittingWorkOrder));
            if (workOrder.Status == KittingWorkOrderStatusEnum.Cancelled)
                return workOrder;

            var affectedItemLocationIds = new List<int>();
            foreach (var line in workOrder.Lines.Where(l => l.Status == KittingWorkOrderLineStatusEnum.Reserved))
            {
                line.ReleasedQty = Math.Max(0, line.ReservedQty - line.ConsumedQty);
                line.Status = KittingWorkOrderLineStatusEnum.Released;
                line.UpdatedAt = now;
                if (line.SourceItemLocationId.HasValue)
                    affectedItemLocationIds.Add(line.SourceItemLocationId.Value);
            }

            workOrder.Status = KittingWorkOrderStatusEnum.Cancelled;
            workOrder.CancelledAt = now;
            workOrder.CancelledBy = actor;
            workOrder.CancelReason = Normalize(reason);
            workOrder.UpdatedAt = now;

            await _unitOfWork.SaveChangesAsync();
            await _reservationService.RecalculateReservedQtyAsync(affectedItemLocationIds);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            return await LoadWorkOrderForDisplayAsync(workOrderId);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    private async Task<List<BillOfMaterial>> LoadActiveBomAsync(int finishedItemId, DateTime now)
    {
        var today = now.Date;
        return await _db.BillOfMaterials
            .Include(b => b.ChildItem)
            .Include(b => b.Uom)
            .Where(b => b.ParentItemId == finishedItemId
                && b.IsActive
                && (b.EffectiveFrom == null || b.EffectiveFrom.Value.Date <= today)
                && (b.EffectiveTo == null || b.EffectiveTo.Value.Date >= today))
            .OrderBy(b => b.BomLevel)
            .ThenBy(b => b.BomId)
            .ToListAsync();
    }

    private async Task<List<KittingAllocation>> AllocateFefoAsync(
        int warehouseId,
        int componentItemId,
        decimal requiredQty,
        Dictionary<int, decimal> tempAllocatedByItemLocation,
        int? ownerPartnerId)
    {
        var remaining = requiredQty;
        var locationIds = await GetWarehouseLocationIdsAsync(warehouseId);
        var candidates = await _db.ItemLocations
            .Where(il => il.ItemId == componentItemId
                && il.OwnerPartnerId == ownerPartnerId
                && locationIds.Contains(il.LocationId)
                && il.HoldStatus == InventoryHoldStatusEnum.Available
                && il.Quantity > il.ReservedQty)
            .OrderBy(il => il.ExpiryDate.HasValue ? 0 : 1)
            .ThenBy(il => il.ExpiryDate)
            .ThenBy(il => il.LotNumber)
            .ThenBy(il => il.ItemLocationId)
            .ToListAsync();

        var allocations = new List<KittingAllocation>();
        foreach (var candidate in candidates)
        {
            var tempReserved = tempAllocatedByItemLocation.GetValueOrDefault(candidate.ItemLocationId);
            var available = Math.Max(0, candidate.Quantity - candidate.ReservedQty - tempReserved);
            if (available <= 0) continue;

            var take = Math.Min(remaining, available);
            allocations.Add(new KittingAllocation(
                candidate.ItemLocationId,
                candidate.LocationId,
                candidate.LotNumber,
                candidate.ExpiryDate,
                take));

            remaining -= take;
            if (remaining <= 0) break;
        }

        if (remaining > 0)
        {
            var itemCode = await _db.Items
                .Where(i => i.ItemId == componentItemId)
                .Select(i => i.ItemCode)
                .FirstOrDefaultAsync() ?? componentItemId.ToString();
            throw new BusinessRuleException($"Không đủ tồn khả dụng để giữ chỗ vật tư thành phần [{itemCode}]. Thiếu {remaining:N2}.", "KITTING_INSUFFICIENT_COMPONENT_STOCK", nameof(ItemLocation));
        }

        return allocations;
    }

    private async Task<KittingWorkOrder> LoadWorkOrderForUpdateAsync(long workOrderId)
    {
        return await _db.KittingWorkOrders
            .Include(k => k.Lines)
            .FirstOrDefaultAsync(k => k.KittingWorkOrderId == workOrderId)
            ?? throw new BusinessRuleException("Không tìm thấy lệnh ráp bộ.", "KITTING_NOT_FOUND", nameof(KittingWorkOrder));
    }

    private async Task<KittingWorkOrder> LoadWorkOrderForDisplayAsync(long workOrderId)
    {
        return await _db.KittingWorkOrders
            .Include(k => k.Warehouse)
            .Include(k => k.FinishedItem).ThenInclude(i => i!.BaseUom)
            .Include(k => k.FinishedLocation)
            .Include(k => k.Lines).ThenInclude(l => l.ComponentItem).ThenInclude(i => i!.BaseUom)
            .Include(k => k.Lines).ThenInclude(l => l.SourceLocation)
            .FirstAsync(k => k.KittingWorkOrderId == workOrderId);
    }

    private async Task<bool> IsLocationInWarehouseAsync(int locationId, int warehouseId)
    {
        var zoneIds = await _db.Zones.Where(z => z.WarehouseId == warehouseId).Select(z => z.ZoneId).ToListAsync();
        return await _db.Locations.AnyAsync(l => l.LocationId == locationId && zoneIds.Contains(l.ZoneId) && l.IsActive);
    }

    private async Task<List<int>> GetWarehouseLocationIdsAsync(int warehouseId)
    {
        var zoneIds = await _db.Zones.Where(z => z.WarehouseId == warehouseId).Select(z => z.ZoneId).ToListAsync();
        return await _db.Locations.Where(l => zoneIds.Contains(l.ZoneId) && l.IsActive).Select(l => l.LocationId).ToListAsync();
    }

    private async Task<string> GenerateWorkOrderCodeAsync(DateTime now)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var code = $"KIT-{now:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
            if (!await _db.KittingWorkOrders.AnyAsync(k => k.WorkOrderCode == code))
                return code;
        }

        return $"KIT-{now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
    }

    private static decimal CalculateRequiredQty(decimal plannedQty, BillOfMaterial bom)
    {
        var scrapFactor = 1 + (bom.ScrapPercent / 100m);
        return decimal.Round(plannedQty * bom.Quantity * scrapFactor, 4, MidpointRounding.AwayFromZero);
    }

    private static void ValidateWholePositiveQty(decimal qty)
    {
        if (qty <= 0 || qty != decimal.Truncate(qty))
            throw new BusinessRuleException("Số lượng bộ phải là số nguyên dương.", "KITTING_QTY_INVALID", nameof(KittingWorkOrder));
    }

    private static string? Normalize(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private sealed record KittingAllocation(int ItemLocationId, int LocationId, string? LotNumber, DateTime? ExpiryDate, decimal Qty);
}
