using System.Data;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public interface IVasWorkOrderService
{
    Task<VasWorkOrder> CreateAsync(CreateVasWorkOrderCommand command, string actor);
    Task<VasWorkOrder> ReserveAsync(long workOrderId, string actor);
    Task<VasWorkOrder> StartAsync(long workOrderId, string actor);
    Task<VasWorkOrder> CompleteOperationAsync(long operationId, decimal actualMinutes, string? notes, string actor);
    Task<VasWorkOrder> SubmitQcAsync(long workOrderId, decimal completedQty, string actor);
    Task<VasWorkOrder> RecordQcAsync(long workOrderId, VasQcResultEnum result, decimal passedQty, decimal failedQty, string? note, string actor);
    Task<VasWorkOrder> CompleteAsync(long workOrderId, string actor);
    Task<VasWorkOrder> CancelAsync(long workOrderId, string reason, string actor);
}

public class CreateVasWorkOrderCommand
{
    public int WarehouseId { get; set; }
    public int? OwnerPartnerId { get; set; }
    public int? PartnerId { get; set; }
    public long? VoucherId { get; set; }
    public int PrimaryItemId { get; set; }
    public VasOperationTypeEnum OperationType { get; set; } = VasOperationTypeEnum.CoPacking;
    public decimal PlannedQty { get; set; } = 1;
    public decimal LaborRatePerHour { get; set; }
    public string? Notes { get; set; }
    public List<CreateVasMaterialLineCommand> MaterialLines { get; set; } = new();
}

public class CreateVasMaterialLineCommand
{
    public int MaterialItemId { get; set; }
    public decimal RequiredQty { get; set; }
    public string? Notes { get; set; }
}

public class VasWorkOrderService : IVasWorkOrderService
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryReservationService _reservationService;
    private readonly IInventoryBalanceService _inventoryBalanceService;
    private readonly IInventoryTransactionService _inventoryTransactionService;

    public VasWorkOrderService(
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

    public async Task<VasWorkOrder> CreateAsync(CreateVasWorkOrderCommand command, string actor)
    {
        var now = VietnamTime.Now;
        if (command.WarehouseId <= 0)
            throw new BusinessRuleException("Vui lòng chọn kho thực hiện gia công phụ trợ.", "VAS_WAREHOUSE_REQUIRED", nameof(VasWorkOrder));
        if (command.PrimaryItemId <= 0)
            throw new BusinessRuleException("Vui lòng chọn mã hàng chính của lệnh gia công phụ trợ.", "VAS_PRIMARY_ITEM_REQUIRED", nameof(Item));
        if (command.PlannedQty <= 0)
            throw new BusinessRuleException("Số lượng kế hoạch phải lớn hơn 0.", "VAS_QTY_INVALID", nameof(VasWorkOrder));
        if (command.LaborRatePerHour < 0)
            throw new BusinessRuleException("Đơn giá nhân công không được âm.", "VAS_LABOR_RATE_INVALID", nameof(VasWorkOrder));

        var warehouseOk = await _db.Warehouses.AnyAsync(w => w.WarehouseId == command.WarehouseId && w.IsActive);
        if (!warehouseOk)
            throw new BusinessRuleException("Không tìm thấy kho hoặc kho đã bị khóa.", "VAS_WAREHOUSE_NOT_FOUND", nameof(Warehouse));

        var primaryItem = await _db.Items.FirstOrDefaultAsync(i => i.ItemId == command.PrimaryItemId && i.IsActive);
        if (primaryItem == null)
            throw new BusinessRuleException("Không tìm thấy mã hàng chính của lệnh gia công phụ trợ.", "VAS_PRIMARY_ITEM_NOT_FOUND", nameof(Item));
        if (primaryItem.TrackSerial)
            throw new BusinessRuleException("Chưa hỗ trợ gia công phụ trợ cho mã hàng chính quản lý số sê-ri. Vui lòng dùng quy trình truy vết số sê-ri riêng.", "VAS_SERIAL_PRIMARY_NOT_SUPPORTED", nameof(Item));

        int? partnerId = command.PartnerId;
        int? ownerPartnerId = command.OwnerPartnerId ?? primaryItem.OwnerPartnerId;
        if (command.VoucherId.HasValue)
        {
            var voucher = await _db.Vouchers.AsNoTracking().FirstOrDefaultAsync(v => v.VoucherId == command.VoucherId.Value);
            if (voucher == null || voucher.IsCancelled)
                throw new BusinessRuleException("Phiếu liên kết không tồn tại hoặc đã hủy.", "VAS_VOUCHER_NOT_FOUND", nameof(Voucher));
            if (voucher.WarehouseId != command.WarehouseId)
                throw new BusinessRuleException("Phiếu liên kết không thuộc kho thực hiện gia công phụ trợ.", "VAS_VOUCHER_WAREHOUSE_MISMATCH", nameof(Voucher));
            partnerId ??= voucher.PartnerId;
            ownerPartnerId ??= voucher.OwnerPartnerId;
        }

        if (partnerId.HasValue)
        {
            var partnerOk = await _db.Partners.AnyAsync(p => p.PartnerId == partnerId.Value && p.IsActive);
            if (!partnerOk)
                throw new BusinessRuleException("Khách hàng/đối tác không hợp lệ.", "VAS_PARTNER_NOT_FOUND", nameof(Partner));
        }
        else if (command.OperationType is VasOperationTypeEnum.CoPacking or VasOperationTypeEnum.Repack or VasOperationTypeEnum.Relabel
            && string.IsNullOrWhiteSpace(command.Notes))
        {
            throw new BusinessRuleException("Lệnh gia công phụ trợ nội bộ không gắn khách hàng/phiếu phải có ghi chú lý do rõ ràng.", "VAS_INTERNAL_NOTE_REQUIRED", nameof(VasWorkOrder));
        }

        var materialInputs = command.MaterialLines
            .Where(l => l.MaterialItemId > 0 || l.RequiredQty > 0)
            .ToList();
        if (materialInputs.Count == 0)
            throw new BusinessRuleException("Vui lòng nhập ít nhất một vật tư phụ/bao bì/nhãn cần dùng.", "VAS_MATERIAL_REQUIRED", nameof(VasMaterialLine));
        if (materialInputs.Any(l => l.MaterialItemId <= 0 || l.RequiredQty <= 0))
            throw new BusinessRuleException("Mỗi dòng vật tư phụ phải có mã hàng và số lượng lớn hơn 0.", "VAS_MATERIAL_LINE_INVALID", nameof(VasMaterialLine));

        var materialIds = materialInputs.Select(l => l.MaterialItemId).Distinct().ToList();
        var materialItems = await _db.Items.Where(i => materialIds.Contains(i.ItemId) && i.IsActive).ToDictionaryAsync(i => i.ItemId);
        if (materialItems.Count != materialIds.Count)
            throw new BusinessRuleException("Có vật tư phụ không tồn tại hoặc đã bị khóa.", "VAS_MATERIAL_NOT_FOUND", nameof(Item));
        var serialMaterial = materialItems.Values.FirstOrDefault(i => i.TrackSerial);
        if (serialMaterial != null)
            throw new BusinessRuleException($"Chưa hỗ trợ gia công phụ trợ với vật tư phụ quản lý số sê-ri [{serialMaterial.ItemCode}].", "VAS_SERIAL_MATERIAL_NOT_SUPPORTED", nameof(Item));

        var workOrder = new VasWorkOrder
        {
            WorkOrderCode = await GenerateWorkOrderCodeAsync(now),
            OperationType = command.OperationType,
            Status = VasWorkOrderStatusEnum.Draft,
            WarehouseId = command.WarehouseId,
            OwnerPartnerId = ownerPartnerId,
            PartnerId = partnerId,
            VoucherId = command.VoucherId,
            PrimaryItemId = command.PrimaryItemId,
            PlannedQty = command.PlannedQty,
            LaborRatePerHour = command.LaborRatePerHour,
            Notes = Normalize(command.Notes),
            CreatedBy = actor,
            CreatedAt = now
        };

        workOrder.Operations.Add(new VasOperation
        {
            StepNumber = 1,
            OperationName = DefaultOperationName(command.OperationType),
            CreatedAt = now
        });

        foreach (var line in materialInputs)
        {
            var item = materialItems[line.MaterialItemId];
            workOrder.MaterialLines.Add(new VasMaterialLine
            {
                MaterialItemId = line.MaterialItemId,
                RequiredQty = line.RequiredQty,
                UnitCostSnapshot = item.UnitCost,
                Status = VasMaterialLineStatusEnum.Draft,
                Notes = Normalize(line.Notes),
                CreatedAt = now
            });
        }

        _db.VasWorkOrders.Add(workOrder);
        await _db.SaveChangesAsync();
        return workOrder;
    }

    public async Task<VasWorkOrder> ReserveAsync(long workOrderId, string actor)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var now = VietnamTime.Now;
            var workOrder = await LoadForUpdateAsync(workOrderId);
            using var ledgerScope = _inventoryTransactionService.BeginScope(new InventoryTransactionContext
            {
                TransactionType = InventoryTransactionTypeEnum.Pick,
                TransactionGroupKey = $"vas:{workOrderId}:reserve",
                IdempotencyKeyPrefix = $"vas:{workOrderId}:reserve",
                WarehouseId = workOrder.WarehouseId,
                OwnerPartnerId = workOrder.OwnerPartnerId,
                VoucherId = workOrder.VoucherId,
                ReferenceType = "VasWorkOrder",
                ReferenceId = workOrder.VasWorkOrderId.ToString(),
                ReferenceCode = workOrder.WorkOrderCode,
                Actor = actor
            });
            if (workOrder.Status != VasWorkOrderStatusEnum.Draft)
                throw new BusinessRuleException("Chỉ lệnh gia công phụ trợ nháp mới được giữ chỗ vật tư.", "VAS_INVALID_STATUS", nameof(VasWorkOrder));

            var requiredByItem = workOrder.MaterialLines
                .GroupBy(l => l.MaterialItemId)
                .Select(g => new { MaterialItemId = g.Key, RequiredQty = g.Sum(x => x.RequiredQty) })
                .ToList();
            if (requiredByItem.Count == 0)
                throw new BusinessRuleException("Lệnh gia công phụ trợ chưa có vật tư phụ để giữ chỗ.", "VAS_NO_MATERIALS", nameof(VasMaterialLine));

            var allocatedLines = new List<VasMaterialLine>();
            var affectedItemLocationIds = new List<int>();
            var tempAllocatedByItemLocation = new Dictionary<int, decimal>();
            var materialCosts = await _db.Items
                .Where(i => requiredByItem.Select(r => r.MaterialItemId).Contains(i.ItemId))
                .ToDictionaryAsync(i => i.ItemId, i => i.UnitCost);

            foreach (var item in requiredByItem)
            {
                var allocations = await AllocateFefoAsync(workOrder.WarehouseId, item.MaterialItemId, item.RequiredQty, tempAllocatedByItemLocation, workOrder.OwnerPartnerId);
                foreach (var allocation in allocations)
                {
                    tempAllocatedByItemLocation[allocation.ItemLocationId] =
                        tempAllocatedByItemLocation.GetValueOrDefault(allocation.ItemLocationId) + allocation.Qty;
                    affectedItemLocationIds.Add(allocation.ItemLocationId);

                    allocatedLines.Add(new VasMaterialLine
                    {
                        VasWorkOrderId = workOrder.VasWorkOrderId,
                        OwnerPartnerId = workOrder.OwnerPartnerId,
                        MaterialItemId = item.MaterialItemId,
                        SourceLocationId = allocation.LocationId,
                        SourceItemLocationId = allocation.ItemLocationId,
                        RequiredQty = allocation.Qty,
                        ReservedQty = allocation.Qty,
                        UnitCostSnapshot = materialCosts.GetValueOrDefault(item.MaterialItemId),
                        LotNumber = allocation.LotNumber,
                        ExpiryDate = allocation.ExpiryDate,
                        Status = VasMaterialLineStatusEnum.Reserved,
                        CreatedAt = now,
                        Notes = "Giữ chỗ tự động theo nguyên tắc hạn dùng gần nhất xuất trước"
                    });
                }
            }

            _db.VasMaterialLines.RemoveRange(workOrder.MaterialLines);
            _db.VasMaterialLines.AddRange(allocatedLines);
            workOrder.Status = VasWorkOrderStatusEnum.Reserved;
            workOrder.ReservedAt = now;
            workOrder.ReservedBy = actor;
            workOrder.UpdatedAt = now;

            await _unitOfWork.SaveChangesAsync();
            await _reservationService.RecalculateReservedQtyAsync(affectedItemLocationIds);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            return await LoadForDisplayAsync(workOrderId);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<VasWorkOrder> StartAsync(long workOrderId, string actor)
    {
        var now = VietnamTime.Now;
        var workOrder = await LoadForUpdateAsync(workOrderId);
        if (workOrder.Status != VasWorkOrderStatusEnum.Reserved)
            throw new BusinessRuleException("Chỉ lệnh gia công phụ trợ đã giữ chỗ đủ vật tư mới được bắt đầu.", "VAS_INVALID_STATUS", nameof(VasWorkOrder));

        workOrder.Status = VasWorkOrderStatusEnum.InProgress;
        workOrder.StartedAt = now;
        workOrder.StartedBy = actor;
        workOrder.UpdatedAt = now;

        var firstOperation = workOrder.Operations.OrderBy(o => o.StepNumber).FirstOrDefault();
        if (firstOperation != null && !firstOperation.StartedAt.HasValue)
        {
            firstOperation.StartedAt = now;
            firstOperation.PerformedBy = actor;
            firstOperation.UpdatedAt = now;
        }

        await _db.SaveChangesAsync();
        return await LoadForDisplayAsync(workOrderId);
    }

    public async Task<VasWorkOrder> CompleteOperationAsync(long operationId, decimal actualMinutes, string? notes, string actor)
    {
        if (actualMinutes <= 0)
            throw new BusinessRuleException("Thời gian lao động thực tế phải lớn hơn 0 phút.", "VAS_LABOR_MINUTES_INVALID", nameof(VasOperation));

        var now = VietnamTime.Now;
        var operation = await _db.VasOperations
            .Include(o => o.VasWorkOrder)
            .FirstOrDefaultAsync(o => o.VasOperationId == operationId)
            ?? throw new BusinessRuleException("Không tìm thấy bước thao tác gia công phụ trợ.", "VAS_OPERATION_NOT_FOUND", nameof(VasOperation));
        var workOrder = operation.VasWorkOrder!;
        if (workOrder.Status != VasWorkOrderStatusEnum.InProgress)
            throw new BusinessRuleException("Chỉ ghi nhận thao tác khi lệnh gia công phụ trợ đang thực hiện.", "VAS_INVALID_STATUS", nameof(VasWorkOrder));

        operation.StartedAt ??= now;
        operation.CompletedAt = now;
        operation.PerformedBy = actor;
        operation.ActualMinutes = actualMinutes;
        operation.Notes = Normalize(notes);
        operation.UpdatedAt = now;

        workOrder.ActualLaborMinutes = await _db.VasOperations
            .Where(o => o.VasWorkOrderId == workOrder.VasWorkOrderId && o.VasOperationId != operationId)
            .SumAsync(o => o.ActualMinutes) + actualMinutes;
        workOrder.LaborCost = CalculateLaborCost(workOrder.ActualLaborMinutes, workOrder.LaborRatePerHour);
        workOrder.TotalCost = workOrder.MaterialCost + workOrder.LaborCost;
        workOrder.UpdatedAt = now;

        await _db.SaveChangesAsync();
        return await LoadForDisplayAsync(workOrder.VasWorkOrderId);
    }

    public async Task<VasWorkOrder> SubmitQcAsync(long workOrderId, decimal completedQty, string actor)
    {
        var now = VietnamTime.Now;
        var workOrder = await LoadForUpdateAsync(workOrderId);
        if (workOrder.Status != VasWorkOrderStatusEnum.InProgress)
            throw new BusinessRuleException("Chỉ lệnh gia công phụ trợ đang thực hiện mới được gửi kiểm tra chất lượng.", "VAS_INVALID_STATUS", nameof(VasWorkOrder));
        if (completedQty <= 0 || completedQty > workOrder.PlannedQty)
            throw new BusinessRuleException("Số lượng hoàn tất phải lớn hơn 0 và không vượt số lượng kế hoạch.", "VAS_COMPLETED_QTY_INVALID", nameof(VasWorkOrder));
        if (!workOrder.Operations.Any(o => o.ActualMinutes > 0 && o.CompletedAt.HasValue))
            throw new BusinessRuleException("Vui lòng ghi nhận ít nhất một bước thao tác và thời gian lao động trước khi gửi kiểm tra chất lượng.", "VAS_LABOR_REQUIRED", nameof(VasOperation));

        workOrder.CompletedQty = completedQty;
        workOrder.Status = VasWorkOrderStatusEnum.QcPending;
        workOrder.QcResult = VasQcResultEnum.Pending;
        workOrder.UpdatedAt = now;

        await _db.SaveChangesAsync();
        return await LoadForDisplayAsync(workOrderId);
    }

    public async Task<VasWorkOrder> RecordQcAsync(long workOrderId, VasQcResultEnum result, decimal passedQty, decimal failedQty, string? note, string actor)
    {
        var now = VietnamTime.Now;
        var workOrder = await LoadForUpdateAsync(workOrderId);
        if (workOrder.Status != VasWorkOrderStatusEnum.QcPending)
            throw new BusinessRuleException("Chỉ lệnh gia công phụ trợ đang chờ kiểm tra chất lượng mới được ghi nhận kết quả.", "VAS_INVALID_STATUS", nameof(VasWorkOrder));
        if (passedQty < 0 || failedQty < 0 || passedQty + failedQty > workOrder.CompletedQty)
            throw new BusinessRuleException("Số lượng kiểm tra đạt/không đạt không hợp lệ.", "VAS_QC_QTY_INVALID", nameof(VasWorkOrder));

        workOrder.QcResult = result;
        workOrder.QcPassedQty = passedQty;
        workOrder.QcFailedQty = failedQty;
        workOrder.QcNote = Normalize(note);
        workOrder.QcBy = actor;
        workOrder.QcAt = now;
        workOrder.UpdatedAt = now;

        if (result is VasQcResultEnum.Failed or VasQcResultEnum.ReworkRequired)
            workOrder.Status = VasWorkOrderStatusEnum.InProgress;

        await _db.SaveChangesAsync();
        return await LoadForDisplayAsync(workOrderId);
    }

    public async Task<VasWorkOrder> CompleteAsync(long workOrderId, string actor)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var now = VietnamTime.Now;
            var workOrder = await LoadForUpdateAsync(workOrderId);
            using var ledgerScope = _inventoryTransactionService.BeginScope(new InventoryTransactionContext
            {
                TransactionType = InventoryTransactionTypeEnum.VasConsume,
                TransactionGroupKey = $"vas:{workOrderId}:complete",
                IdempotencyKeyPrefix = $"vas:{workOrderId}:complete",
                WarehouseId = workOrder.WarehouseId,
                OwnerPartnerId = workOrder.OwnerPartnerId,
                VoucherId = workOrder.VoucherId,
                ReferenceType = "VasWorkOrder",
                ReferenceId = workOrder.VasWorkOrderId.ToString(),
                ReferenceCode = workOrder.WorkOrderCode,
                Actor = actor
            });
            if (workOrder.Status != VasWorkOrderStatusEnum.QcPending)
                throw new BusinessRuleException("Chỉ lệnh gia công phụ trợ đã gửi kiểm tra chất lượng mới được hoàn tất.", "VAS_INVALID_STATUS", nameof(VasWorkOrder));
            if (workOrder.QcResult != VasQcResultEnum.Passed)
                throw new BusinessRuleException("Không thể hoàn tất khi kết quả kiểm tra chất lượng chưa đạt.", "VAS_QC_NOT_PASSED", nameof(VasWorkOrder));
            if (workOrder.CompletedQty <= 0 || workOrder.CompletedQty > workOrder.PlannedQty)
                throw new BusinessRuleException("Số lượng hoàn tất không hợp lệ.", "VAS_COMPLETED_QTY_INVALID", nameof(VasWorkOrder));
            if (!workOrder.Operations.Any(o => o.ActualMinutes > 0 && o.CompletedAt.HasValue))
                throw new BusinessRuleException("Lệnh gia công phụ trợ chưa có ghi nhận thời gian lao động.", "VAS_LABOR_REQUIRED", nameof(VasOperation));

            var reservedLines = workOrder.MaterialLines.Where(l => l.Status == VasMaterialLineStatusEnum.Reserved).ToList();
            if (reservedLines.Count == 0 || reservedLines.Any(l => l.SourceItemLocationId == null || l.SourceLocationId == null || l.ReservedQty <= 0))
                throw new BusinessRuleException("Lệnh gia công phụ trợ chưa giữ chỗ đủ vật tư phụ.", "VAS_RESERVATION_INCOMPLETE", nameof(VasMaterialLine));

            // P1-R2-3: nếu có line bị partial-cancel (Released/Consumed) trước đó, tổng ReservedQty < RequiredQty
            // ⇒ không đủ vật tư so với BoM. Throw rõ để user reserve lại thay vì consume thiếu lặng lẽ.
            var requiredByItem = workOrder.MaterialLines
                .GroupBy(l => l.MaterialItemId)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.RequiredQty));
            var reservedByItem = reservedLines
                .GroupBy(l => l.MaterialItemId)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.ReservedQty));
            foreach (var kv in requiredByItem)
            {
                var reservedQty = reservedByItem.TryGetValue(kv.Key, out var r) ? r : 0m;
                if (reservedQty + 0.0001m < kv.Value)
                    throw new BusinessRuleException(
                        $"Vật tư #{kv.Key} có {reservedQty:N4} đang giữ nhưng cần {kv.Value:N4}. Có thể đã hủy giữ chỗ trước đó — vui lòng reserve lại.",
                        "VAS_RESERVATION_PARTIAL", nameof(VasMaterialLine));
            }

            var affectedItemLocationIds = new List<int>();
            var affectedItemIds = new HashSet<int>();
            decimal materialCost = 0;

            foreach (var line in reservedLines)
            {
                var source = await _db.ItemLocations.FirstOrDefaultAsync(il =>
                    il.ItemLocationId == line.SourceItemLocationId!.Value
                    && il.ItemId == line.MaterialItemId
                    && il.OwnerPartnerId == line.OwnerPartnerId
                    && il.LocationId == line.SourceLocationId!.Value
                    && il.LotNumber == line.LotNumber
                    && il.ExpiryDate == line.ExpiryDate);

                if (source == null)
                    throw new BusinessRuleException("Không tìm thấy tồn kho vật tư phụ đã giữ chỗ.", "VAS_SOURCE_STOCK_NOT_FOUND", nameof(ItemLocation));
                if (source.HoldStatus != InventoryHoldStatusEnum.Available)
                    throw new BusinessRuleException("Vật tư phụ đang bị giữ chất lượng, không thể hoàn tất lệnh gia công phụ trợ.", "VAS_SOURCE_HOLD_BLOCKED", nameof(ItemLocation));
                if (source.Quantity < line.ReservedQty)
                    throw new BusinessRuleException("Tồn thực tế của vật tư phụ đã thay đổi, không đủ số lượng để tiêu hao.", "VAS_SOURCE_STOCK_CHANGED", nameof(ItemLocation));

                source.Quantity -= line.ReservedQty;
                source.UpdatedAt = now;

                line.ConsumedQty = line.ReservedQty;
                line.ConsumedCost = line.ConsumedQty * line.UnitCostSnapshot;
                line.Status = VasMaterialLineStatusEnum.Consumed;
                line.UpdatedAt = now;

                materialCost += line.ConsumedCost;
                affectedItemLocationIds.Add(source.ItemLocationId);
                affectedItemIds.Add(line.MaterialItemId);
            }

            workOrder.MaterialCost = materialCost;
            workOrder.ActualLaborMinutes = workOrder.Operations.Sum(o => o.ActualMinutes);
            workOrder.LaborCost = CalculateLaborCost(workOrder.ActualLaborMinutes, workOrder.LaborRatePerHour);
            workOrder.TotalCost = workOrder.MaterialCost + workOrder.LaborCost;
            workOrder.Status = VasWorkOrderStatusEnum.Completed;
            workOrder.CompletedAt = now;
            workOrder.CompletedBy = actor;
            workOrder.UpdatedAt = now;

            await _unitOfWork.SaveChangesAsync();
            await _reservationService.RecalculateReservedQtyAsync(affectedItemLocationIds);
            await _unitOfWork.SaveChangesAsync();
            await _inventoryBalanceService.SyncCurrentStockAsync(affectedItemIds);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            return await LoadForDisplayAsync(workOrderId);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<VasWorkOrder> CancelAsync(long workOrderId, string reason, string actor)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var now = VietnamTime.Now;
            var workOrder = await LoadForUpdateAsync(workOrderId);
            using var ledgerScope = _inventoryTransactionService.BeginScope(new InventoryTransactionContext
            {
                TransactionType = InventoryTransactionTypeEnum.Cancel,
                TransactionGroupKey = $"vas:{workOrderId}:cancel",
                IdempotencyKeyPrefix = $"vas:{workOrderId}:cancel",
                WarehouseId = workOrder.WarehouseId,
                OwnerPartnerId = workOrder.OwnerPartnerId,
                VoucherId = workOrder.VoucherId,
                ReferenceType = "VasWorkOrder",
                ReferenceId = workOrder.VasWorkOrderId.ToString(),
                ReferenceCode = workOrder.WorkOrderCode,
                Actor = actor
            });
            if (workOrder.Status == VasWorkOrderStatusEnum.Completed)
                throw new BusinessRuleException("Không thể hủy lệnh gia công phụ trợ đã hoàn tất.", "VAS_COMPLETED_CANNOT_CANCEL", nameof(VasWorkOrder));
            if (workOrder.Status == VasWorkOrderStatusEnum.Cancelled)
                return workOrder;

            var affectedItemLocationIds = new List<int>();
            foreach (var line in workOrder.MaterialLines.Where(l => l.Status == VasMaterialLineStatusEnum.Reserved))
            {
                line.ReleasedQty = Math.Max(0, line.ReservedQty - line.ConsumedQty);
                line.Status = VasMaterialLineStatusEnum.Released;
                line.UpdatedAt = now;
                if (line.SourceItemLocationId.HasValue)
                    affectedItemLocationIds.Add(line.SourceItemLocationId.Value);
            }

            workOrder.Status = VasWorkOrderStatusEnum.Cancelled;
            workOrder.CancelledAt = now;
            workOrder.CancelledBy = actor;
            workOrder.CancelReason = Normalize(reason);
            workOrder.UpdatedAt = now;

            await _unitOfWork.SaveChangesAsync();
            await _reservationService.RecalculateReservedQtyAsync(affectedItemLocationIds);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            return await LoadForDisplayAsync(workOrderId);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    private async Task<List<VasAllocation>> AllocateFefoAsync(
        int warehouseId,
        int materialItemId,
        decimal requiredQty,
        Dictionary<int, decimal> tempAllocatedByItemLocation,
        int? ownerPartnerId)
    {
        var remaining = requiredQty;
        var locationIds = await GetWarehouseLocationIdsAsync(warehouseId);
        var candidates = await _db.ItemLocations
            .Where(il => il.ItemId == materialItemId
                && il.OwnerPartnerId == ownerPartnerId
                && locationIds.Contains(il.LocationId)
                && il.HoldStatus == InventoryHoldStatusEnum.Available
                && il.Quantity > il.ReservedQty)
            .OrderBy(il => il.ExpiryDate.HasValue ? 0 : 1)
            .ThenBy(il => il.ExpiryDate)
            .ThenBy(il => il.LotNumber)
            .ThenBy(il => il.ItemLocationId)
            .ToListAsync();

        var allocations = new List<VasAllocation>();
        foreach (var candidate in candidates)
        {
            var tempReserved = tempAllocatedByItemLocation.GetValueOrDefault(candidate.ItemLocationId);
            var available = Math.Max(0, candidate.Quantity - candidate.ReservedQty - tempReserved);
            if (available <= 0) continue;

            var take = Math.Min(remaining, available);
            allocations.Add(new VasAllocation(candidate.ItemLocationId, candidate.LocationId, candidate.LotNumber, candidate.ExpiryDate, take));
            remaining -= take;
            if (remaining <= 0) break;
        }

        if (remaining > 0)
        {
            var itemCode = await _db.Items.Where(i => i.ItemId == materialItemId).Select(i => i.ItemCode).FirstOrDefaultAsync() ?? materialItemId.ToString();
            throw new BusinessRuleException($"Không đủ tồn khả dụng để giữ chỗ vật tư phụ [{itemCode}]. Thiếu {remaining:N2}.", "VAS_INSUFFICIENT_MATERIAL_STOCK", nameof(ItemLocation));
        }

        return allocations;
    }

    private async Task<VasWorkOrder> LoadForUpdateAsync(long workOrderId)
    {
        return await _db.VasWorkOrders
            .Include(v => v.Operations)
            .Include(v => v.MaterialLines)
            .FirstOrDefaultAsync(v => v.VasWorkOrderId == workOrderId)
            ?? throw new BusinessRuleException("Không tìm thấy lệnh gia công phụ trợ.", "VAS_NOT_FOUND", nameof(VasWorkOrder));
    }

    private async Task<VasWorkOrder> LoadForDisplayAsync(long workOrderId)
    {
        return await _db.VasWorkOrders
            .Include(v => v.Warehouse)
            .Include(v => v.Partner)
            .Include(v => v.Voucher)
            .Include(v => v.PrimaryItem).ThenInclude(i => i!.BaseUom)
            .Include(v => v.Operations)
            .Include(v => v.MaterialLines).ThenInclude(l => l.MaterialItem).ThenInclude(i => i!.BaseUom)
            .Include(v => v.MaterialLines).ThenInclude(l => l.SourceLocation)
            .FirstAsync(v => v.VasWorkOrderId == workOrderId);
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
            var code = $"GCP-{now:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
            if (!await _db.VasWorkOrders.AnyAsync(v => v.WorkOrderCode == code))
                return code;
        }

        return $"GCP-{now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
    }

    private static decimal CalculateLaborCost(decimal minutes, decimal ratePerHour)
        => decimal.Round(minutes * ratePerHour / 60m, 4, MidpointRounding.AwayFromZero);

    private static string DefaultOperationName(VasOperationTypeEnum type) => type switch
    {
        VasOperationTypeEnum.LightAssembly => "Lắp ráp nhẹ",
        VasOperationTypeEnum.CoPacking => "Đóng gói phối hợp",
        VasOperationTypeEnum.Repack => "Đóng gói lại",
        VasOperationTypeEnum.Relabel => "Dán nhãn lại",
        _ => "Thao tác gia công phụ trợ"
    };

    private static string? Normalize(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private sealed record VasAllocation(int ItemLocationId, int LocationId, string? LotNumber, DateTime? ExpiryDate, decimal Qty);
}
