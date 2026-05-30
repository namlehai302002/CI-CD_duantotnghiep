using System.Data;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public sealed class MovementTaskCreateRequest
{
    public int WarehouseId { get; init; }
    public int? OwnerPartnerId { get; init; }
    public int ItemId { get; init; }
    public int SourceLocationId { get; init; }
    public int DestinationLocationId { get; init; }
    public int? SourceItemLocationId { get; init; }
    public long? LicensePlateId { get; init; }
    public MovementTaskTypeEnum TaskType { get; init; }
    public MovementTaskPriorityEnum Priority { get; init; } = MovementTaskPriorityEnum.Normal;
    public decimal PlannedQty { get; init; }
    public string? LotNumber { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public int? PreviousDefaultLocationId { get; init; }
    public bool UpdateDefaultLocationOnComplete { get; init; }
    public long? ReplenishmentAutomationRunId { get; init; }
    public long? ReplenishmentAutomationLineId { get; init; }
    public ReplenishmentTriggerTypeEnum? ReplenishmentTriggerType { get; init; }
    public decimal DemandQtySnapshot { get; init; }
    public decimal ForecastQtySnapshot { get; init; }
    public decimal OpenReplenishmentQtySnapshot { get; init; }
    public int RoutePriorityScore { get; init; }
    public int TravelSequenceScore { get; init; }
    public int? SourceZoneId { get; init; }
    public int? DestinationZoneId { get; init; }
    public int SourceAisleSequence { get; init; }
    public int DestinationAisleSequence { get; init; }
    public string? AutomationBatchKey { get; init; }
    public DateTime? DueAt { get; init; }
    public string SourceModule { get; init; } = "";
    public string? SourceReference { get; init; }
    public string? SourceReason { get; init; }
    public string? AssignedTo { get; init; }
    public string? Notes { get; init; }
}

public interface IMovementTaskService
{
    Task<MovementTask> CreateMovementTaskAsync(MovementTaskCreateRequest request, int? scopedWarehouseId, string actor);
    Task<MovementTask> CreateLpnMovementTaskAsync(string lpnCode, int destinationLocationId, MovementTaskTypeEnum taskType, MovementTaskPriorityEnum priority, int? scopedWarehouseId, string actor, string? sourceModule = null, string? sourceReason = null);
    Task<MovementTask> CreateReslottingTaskAsync(int itemId, int suggestedLocationId, int? scopedWarehouseId, string actor);
    Task<MovementTask> CreateReplenishmentTaskAsync(int itemId, int destinationLocationId, decimal qty, int? sourceItemLocationId, int? scopedWarehouseId, string actor);
    Task<MovementTask> AssignAsync(long movementTaskId, string assignedTo, int? scopedWarehouseId, string actor);
    Task<MovementTask> AcceptAsync(long movementTaskId, int? scopedWarehouseId, string actor);
    Task<MovementTask> StartAsync(long movementTaskId, int? scopedWarehouseId, string actor);
    Task<MovementTask> CancelAsync(long movementTaskId, string? reason, int? scopedWarehouseId, string actor);
    Task<MovementTask> CompleteAsync(long movementTaskId, string? sourceScan, string? destinationScan, decimal confirmedQty, int? scopedWarehouseId, string actor, string? lpnScan = null);
}

public class MovementTaskService : IMovementTaskService
{
    private static readonly MovementTaskStatusEnum[] OpenStatuses =
    {
        MovementTaskStatusEnum.Pending,
        MovementTaskStatusEnum.Assigned,
        MovementTaskStatusEnum.InProgress
    };

    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventorySnapshotService _inventorySnapshotService;
    private readonly IInventoryBalanceService _inventoryBalanceService;
    private readonly ISerialInventoryService _serialInventoryService;
    private readonly IInventoryTransactionService _inventoryTransactionService;

    public MovementTaskService(
        AppDbContext db,
        IUnitOfWork unitOfWork,
        IInventorySnapshotService? inventorySnapshotService = null,
        IInventoryBalanceService? inventoryBalanceService = null,
        ISerialInventoryService? serialInventoryService = null,
        IInventoryTransactionService? inventoryTransactionService = null)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _inventorySnapshotService = inventorySnapshotService ?? new InventorySnapshotService(db);
        _inventoryBalanceService = inventoryBalanceService ?? new InventoryBalanceService(db);
        _serialInventoryService = serialInventoryService ?? new SerialInventoryService(db);
        _inventoryTransactionService = inventoryTransactionService ?? new InventoryTransactionService(db);
    }

    private static DateTime Now => VietnamTime.Now;

    public async Task<MovementTask> CreateMovementTaskAsync(MovementTaskCreateRequest request, int? scopedWarehouseId, string actor)
    {
        EnsureWarehouseScope(request.WarehouseId, scopedWarehouseId);
        if (request.PlannedQty <= 0)
            throw new BusinessRuleException("Số lượng điều chuyển phải lớn hơn 0.", "MOVEMENT_QTY_INVALID", "MovementTask");
        if (request.SourceLocationId == request.DestinationLocationId)
            throw new BusinessRuleException("Vị trí nguồn và vị trí đích phải khác nhau.", "MOVEMENT_SAME_LOCATION", "MovementTask");

        var item = await _db.Items.FirstOrDefaultAsync(i => i.ItemId == request.ItemId && i.IsActive);
        if (item == null)
            throw new BusinessRuleException("Vật tư không hoạt động hoặc không tồn tại.", "MOVEMENT_ITEM_NOT_FOUND", "Item");

        var sourceLocation = await _db.Locations.Include(l => l.Zone).FirstOrDefaultAsync(l => l.LocationId == request.SourceLocationId && l.IsActive);
        var destinationLocation = await _db.Locations.Include(l => l.Zone).FirstOrDefaultAsync(l => l.LocationId == request.DestinationLocationId && l.IsActive);
        if (sourceLocation?.Zone == null || destinationLocation?.Zone == null)
            throw new BusinessRuleException("Vị trí nguồn/đích không hợp lệ.", "MOVEMENT_LOCATION_INVALID", "Location");
        if (sourceLocation.Zone.WarehouseId != request.WarehouseId || destinationLocation.Zone.WarehouseId != request.WarehouseId)
            throw new BusinessRuleException("Vị trí nguồn và đích phải cùng kho của nhiệm vụ.", "MOVEMENT_LOCATION_WAREHOUSE_MISMATCH", "Location");

        var sourceStock = await ResolveSourceStockAsync(request.ItemId, request.SourceLocationId, request.SourceItemLocationId, request.LotNumber, request.ExpiryDate, request.OwnerPartnerId);
        if (sourceStock == null)
            throw new BusinessRuleException("Không tìm thấy dòng tồn kho nguồn.", "MOVEMENT_SOURCE_STOCK_NOT_FOUND", "ItemLocation");
        if (sourceStock.AvailableQty < request.PlannedQty)
            throw new BusinessRuleException("Tồn kho nguồn không đủ cho nhiệm vụ điều chuyển này.", "MOVEMENT_SOURCE_STOCK_INSUFFICIENT", "ItemLocation");

        await EnsureNoActiveLpnAsync(request.ItemId, request.SourceLocationId, sourceStock.LotNumber, sourceStock.ExpiryDate, sourceStock.OwnerPartnerId);
        await EnsureNoDuplicateOpenTaskAsync(request.ItemId, request.SourceLocationId, request.DestinationLocationId, request.TaskType, sourceStock.OwnerPartnerId);

        var task = new MovementTask
        {
            TaskCode = await GenerateTaskCodeAsync(request.TaskType),
            WarehouseId = request.WarehouseId,
            OwnerPartnerId = sourceStock.OwnerPartnerId,
            ItemId = request.ItemId,
            SourceLocationId = request.SourceLocationId,
            DestinationLocationId = request.DestinationLocationId,
            SourceItemLocationId = sourceStock.ItemLocationId,
            MovementMode = MovementTaskModeEnum.Item,
            TaskType = request.TaskType,
            Priority = request.Priority,
            PlannedQty = request.PlannedQty,
            LotNumber = sourceStock.LotNumber,
            ExpiryDate = sourceStock.ExpiryDate,
            PreviousDefaultLocationId = request.PreviousDefaultLocationId,
            UpdateDefaultLocationOnComplete = request.UpdateDefaultLocationOnComplete,
            ReplenishmentAutomationRunId = request.ReplenishmentAutomationRunId,
            ReplenishmentAutomationLineId = request.ReplenishmentAutomationLineId,
            ReplenishmentTriggerType = request.ReplenishmentTriggerType,
            DemandQtySnapshot = request.DemandQtySnapshot,
            ForecastQtySnapshot = request.ForecastQtySnapshot,
            OpenReplenishmentQtySnapshot = request.OpenReplenishmentQtySnapshot,
            RoutePriorityScore = request.RoutePriorityScore,
            TravelSequenceScore = request.TravelSequenceScore,
            SourceZoneId = request.SourceZoneId ?? sourceLocation.ZoneId,
            DestinationZoneId = request.DestinationZoneId ?? destinationLocation.ZoneId,
            SourceAisleSequence = request.SourceAisleSequence == 0 ? sourceLocation.AisleSequence : request.SourceAisleSequence,
            DestinationAisleSequence = request.DestinationAisleSequence == 0 ? destinationLocation.AisleSequence : request.DestinationAisleSequence,
            AutomationBatchKey = Clean(request.AutomationBatchKey),
            DueAt = request.DueAt,
            AssignedTo = Clean(request.AssignedTo),
            AssignedAt = string.IsNullOrWhiteSpace(request.AssignedTo) ? null : Now,
            Status = string.IsNullOrWhiteSpace(request.AssignedTo) ? MovementTaskStatusEnum.Pending : MovementTaskStatusEnum.Assigned,
            SourceModule = Clean(request.SourceModule) ?? request.TaskType.ToString(),
            SourceReference = Clean(request.SourceReference),
            SourceReason = Clean(request.SourceReason),
            Notes = Clean(request.Notes),
            CreatedBy = actor,
            CreatedAt = Now
        };

        _db.MovementTasks.Add(task);
        await _unitOfWork.SaveChangesAsync();
        return task;
    }

    public async Task<MovementTask> CreateLpnMovementTaskAsync(
        string lpnCode,
        int destinationLocationId,
        MovementTaskTypeEnum taskType,
        MovementTaskPriorityEnum priority,
        int? scopedWarehouseId,
        string actor,
        string? sourceModule = null,
        string? sourceReason = null)
    {
        var normalizedCode = Clean(lpnCode);
        if (normalizedCode == null)
            throw new BusinessRuleException("Vui long nhap ma LPN.", "MOVEMENT_LPN_REQUIRED", "LicensePlate");

        var root = await _db.LicensePlates
            .Include(l => l.CurrentLocation)!.ThenInclude(l => l!.Zone)
            .FirstOrDefaultAsync(l => l.LpnCode == normalizedCode && l.IsActive);
        if (root == null)
            throw new BusinessRuleException("Khong tim thay LPN hoat dong.", "MOVEMENT_LPN_NOT_FOUND", "LicensePlate");

        EnsureWarehouseScope(root.WarehouseId, scopedWarehouseId);
        if (!IsMovableLpnStatus(root.Status) || !root.CurrentLocationId.HasValue)
            throw new BusinessRuleException("LPN khong o trang thai co the di chuyen.", "MOVEMENT_LPN_STATUS_BLOCKED", "LicensePlate");
        if (root.CurrentLocationId.Value == destinationLocationId)
            throw new BusinessRuleException("Vi tri nguon va dich phai khac nhau.", "MOVEMENT_SAME_LOCATION", "MovementTask");

        var destinationLocation = await _db.Locations.Include(l => l.Zone).FirstOrDefaultAsync(l => l.LocationId == destinationLocationId && l.IsActive);
        if (root.CurrentLocation?.Zone == null || destinationLocation?.Zone == null)
            throw new BusinessRuleException("Vi tri nguon/dich khong hop le.", "MOVEMENT_LOCATION_INVALID", "Location");
        if (destinationLocation.Zone.WarehouseId != root.WarehouseId || root.CurrentLocation.Zone.WarehouseId != root.WarehouseId)
            throw new BusinessRuleException("LPN va vi tri dich phai thuoc cung kho.", "MOVEMENT_LOCATION_WAREHOUSE_MISMATCH", "Location");

        await EnsureNoDuplicateOpenLpnTaskAsync(root.LicensePlateId);

        var tree = await LoadLpnTreeAsync(root.LicensePlateId, root.WarehouseId);
        if (tree.Any(l => !IsMovableLpnStatus(l.Status)))
            throw new BusinessRuleException("Cay LPN co kien con khong o trang thai co the di chuyen.", "MOVEMENT_LPN_TREE_STATUS_BLOCKED", "LicensePlate");
        if (tree.Any(l => l.CurrentLocationId.HasValue && l.CurrentLocationId.Value != root.CurrentLocationId.Value))
            throw new BusinessRuleException("Cay LPN dang lech vi tri vat ly. Hay reconciliation truoc khi di chuyen.", "MOVEMENT_LPN_TREE_LOCATION_MISMATCH", "LicensePlate");

        var treeIds = tree.Select(l => l.LicensePlateId).ToList();
        await _serialInventoryService.EnsureLpnTreeHasNoOpenSerialReservationAsync(treeIds);
        var details = await _db.LicensePlateDetails
            .Where(d => treeIds.Contains(d.LicensePlateId) && d.Quantity > 0)
            .OrderBy(d => d.LicensePlateDetailId)
            .ToListAsync();
        if (details.Count == 0)
            throw new BusinessRuleException("LPN khong co detail ton kho de di chuyen.", "MOVEMENT_LPN_EMPTY", "LicensePlateDetail");

        var firstDetail = details[0];
        var distinctItems = details.Select(d => d.ItemId).Distinct().Count();
        var singleLot = details.Select(d => d.LotNumber).Distinct().Count() == 1 ? firstDetail.LotNumber : null;
        var singleExpiry = details.Select(d => d.ExpiryDate).Distinct().Count() == 1 ? firstDetail.ExpiryDate : null;

        var task = new MovementTask
        {
            TaskCode = await GenerateTaskCodeAsync(taskType),
            WarehouseId = root.WarehouseId,
            OwnerPartnerId = root.OwnerPartnerId,
            ItemId = firstDetail.ItemId,
            SourceLocationId = root.CurrentLocationId.Value,
            DestinationLocationId = destinationLocationId,
            MovementMode = MovementTaskModeEnum.Lpn,
            LicensePlateId = root.LicensePlateId,
            LpnCodeSnapshot = root.LpnCode,
            LpnDetailCount = details.Count,
            LpnDistinctItemCount = distinctItems,
            TaskType = taskType,
            Priority = priority,
            PlannedQty = details.Sum(d => d.Quantity),
            LotNumber = singleLot,
            ExpiryDate = singleExpiry,
            Status = MovementTaskStatusEnum.Pending,
            SourceModule = Clean(sourceModule) ?? "LPN",
            SourceReference = root.LpnCode,
            SourceReason = Clean(sourceReason) ?? $"Atomic LPN movement to {destinationLocation.LocationCode}.",
            CreatedBy = actor,
            CreatedAt = Now
        };

        _db.MovementTasks.Add(task);
        await _unitOfWork.SaveChangesAsync();
        return task;
    }

    public async Task<MovementTask> CreateReslottingTaskAsync(int itemId, int suggestedLocationId, int? scopedWarehouseId, string actor)
    {
        var item = await _db.Items
            .Include(i => i.DefaultLocation)!.ThenInclude(l => l!.Zone)
            .FirstOrDefaultAsync(i => i.ItemId == itemId && i.IsActive);
        if (item == null)
            throw new BusinessRuleException("Vật tư không hoạt động hoặc không tồn tại.", "MOVEMENT_ITEM_NOT_FOUND", "Item");

        var destination = await _db.Locations.Include(l => l.Zone).FirstOrDefaultAsync(l => l.LocationId == suggestedLocationId && l.IsActive);
        if (destination?.Zone == null)
            throw new BusinessRuleException("Vị trí đề xuất không hợp lệ.", "MOVEMENT_LOCATION_INVALID", "Location");

        EnsureWarehouseScope(destination.Zone.WarehouseId, scopedWarehouseId);

        var sourceRows = await _db.ItemLocations
            .Include(il => il.Location)!.ThenInclude(l => l!.Zone)
            .Where(il => il.ItemId == itemId
                && il.LocationId != suggestedLocationId
                && il.Quantity - il.ReservedQty > 0
                && il.Location != null
                && il.Location.Zone != null
                && il.Location.Zone.WarehouseId == destination.Zone.WarehouseId)
            .ToListAsync();

        var source = sourceRows
            .OrderByDescending(il => item.DefaultLocationId.HasValue && il.LocationId == item.DefaultLocationId.Value)
            .ThenByDescending(il => il.Quantity - il.ReservedQty)
            .FirstOrDefault();
        if (source == null)
            throw new BusinessRuleException("Không có tồn kho nguồn bên ngoài vị trí đề xuất cần di chuyển.", "MOVEMENT_SOURCE_STOCK_NOT_FOUND", "ItemLocation");

        var plannedQty = Math.Max(0, source.Quantity - source.ReservedQty);
        var reason = $"Đề xuất tái phân kệ: di chuyển tồn kho hoạt động về {destination.LocationCode}; vị trí mặc định sẽ cập nhật sau khi màn quét xác nhận.";

        var lpnCandidate = await FindMovableLpnForSourceAsync(itemId, source.LocationId, source.LotNumber, source.ExpiryDate, destination.Zone.WarehouseId);
        if (lpnCandidate != null)
        {
            return await CreateLpnMovementTaskAsync(
                lpnCandidate.LpnCode,
                suggestedLocationId,
                MovementTaskTypeEnum.Reslotting,
                MovementTaskPriorityEnum.High,
                scopedWarehouseId,
                actor,
                "Slotting",
                $"Source inventory is LPN-managed; created atomic LPN reslotting for {lpnCandidate.LpnCode}.");
        }

        return await CreateMovementTaskAsync(new MovementTaskCreateRequest
        {
            WarehouseId = destination.Zone.WarehouseId,
            ItemId = itemId,
            SourceLocationId = source.LocationId,
            DestinationLocationId = suggestedLocationId,
            SourceItemLocationId = source.ItemLocationId,
            TaskType = MovementTaskTypeEnum.Reslotting,
            Priority = MovementTaskPriorityEnum.High,
            PlannedQty = plannedQty,
            LotNumber = source.LotNumber,
            ExpiryDate = source.ExpiryDate,
            PreviousDefaultLocationId = item.DefaultLocationId,
            UpdateDefaultLocationOnComplete = true,
            SourceModule = "Slotting",
            SourceReference = item.ItemCode,
            SourceReason = reason
        }, scopedWarehouseId, actor);
    }

    public async Task<MovementTask> CreateReplenishmentTaskAsync(int itemId, int destinationLocationId, decimal qty, int? sourceItemLocationId, int? scopedWarehouseId, string actor)
    {
        var item = await _db.Items
            .Include(i => i.DefaultLocation)!.ThenInclude(l => l!.Zone)
            .FirstOrDefaultAsync(i => i.ItemId == itemId && i.IsActive);
        if (item == null)
            throw new BusinessRuleException("Vật tư không hoạt động hoặc không tồn tại.", "MOVEMENT_ITEM_NOT_FOUND", "Item");

        var destination = await _db.Locations.Include(l => l.Zone).FirstOrDefaultAsync(l => l.LocationId == destinationLocationId && l.IsActive);
        if (destination?.Zone == null)
            throw new BusinessRuleException("Vị trí đích không hợp lệ.", "MOVEMENT_LOCATION_INVALID", "Location");

        EnsureWarehouseScope(destination.Zone.WarehouseId, scopedWarehouseId);

        ItemLocation? source = null;
        if (sourceItemLocationId.HasValue)
        {
            source = await _db.ItemLocations
                .Include(il => il.Location)!.ThenInclude(l => l!.Zone)
                .FirstOrDefaultAsync(il => il.ItemLocationId == sourceItemLocationId.Value && il.ItemId == itemId);
        }
        else if (item.DefaultLocationId.HasValue)
        {
            source = await _db.ItemLocations
                .Include(il => il.Location)!.ThenInclude(l => l!.Zone)
                .FirstOrDefaultAsync(il => il.ItemId == itemId && il.LocationId == item.DefaultLocationId.Value);
        }

        if (source?.Location?.Zone == null)
            throw new BusinessRuleException("Không tìm thấy dòng tồn kho nguồn.", "MOVEMENT_SOURCE_STOCK_NOT_FOUND", "ItemLocation");
        if (source.Location.Zone.WarehouseId != destination.Zone.WarehouseId)
            throw new BusinessRuleException("Vị trí nguồn và đích phải cùng kho.", "MOVEMENT_LOCATION_WAREHOUSE_MISMATCH", "Location");

        var sourceAvailable = Math.Max(0, source.Quantity - source.ReservedQty);
        var plannedQty = qty > 0 ? Math.Min(qty, sourceAvailable) : sourceAvailable;
        if (plannedQty <= 0)
            throw new BusinessRuleException("Số lượng điều chuyển phải lớn hơn 0.", "MOVEMENT_QTY_INVALID", "MovementTask");

        var replenishmentLpn = await FindMovableLpnForSourceAsync(itemId, source.LocationId, source.LotNumber, source.ExpiryDate, destination.Zone.WarehouseId);
        if (replenishmentLpn != null)
        {
            return await CreateLpnMovementTaskAsync(
                replenishmentLpn.LpnCode,
                destinationLocationId,
                MovementTaskTypeEnum.Replenishment,
                MovementTaskPriorityEnum.High,
                scopedWarehouseId,
                actor,
                "Replenishment",
                $"Source inventory is LPN-managed; created atomic LPN replenishment for {replenishmentLpn.LpnCode}.");
        }

        return await CreateMovementTaskAsync(new MovementTaskCreateRequest
        {
            WarehouseId = destination.Zone.WarehouseId,
            ItemId = itemId,
            SourceLocationId = source.LocationId,
            DestinationLocationId = destinationLocationId,
            SourceItemLocationId = source.ItemLocationId,
            TaskType = MovementTaskTypeEnum.Replenishment,
            Priority = MovementTaskPriorityEnum.High,
            PlannedQty = plannedQty,
            LotNumber = source.LotNumber,
            ExpiryDate = source.ExpiryDate,
            SourceModule = "Replenishment",
            SourceReference = item.ItemCode,
            SourceReason = $"Nhiệm vụ bổ sung hàng cho vị trí lấy hàng {destination.LocationCode}."
        }, scopedWarehouseId, actor);
    }

    public async Task<MovementTask> AssignAsync(long movementTaskId, string assignedTo, int? scopedWarehouseId, string actor)
    {
        var task = await FindTaskAsync(movementTaskId);
        EnsureWarehouseScope(task.WarehouseId, scopedWarehouseId);
        EnsureOpenTask(task);
        if (string.IsNullOrWhiteSpace(assignedTo))
            throw new BusinessRuleException("Vui lòng nhập tài khoản người được gán.", "MOVEMENT_ASSIGNEE_REQUIRED", "MovementTask");

        task.AssignedTo = assignedTo.Trim();
        task.AssignedAt = Now;
        task.Status = MovementTaskStatusEnum.Assigned;
        task.UpdatedAt = Now;
        await _unitOfWork.SaveChangesAsync();
        return task;
    }

    public async Task<MovementTask> AcceptAsync(long movementTaskId, int? scopedWarehouseId, string actor)
    {
        var task = await FindTaskAsync(movementTaskId);
        EnsureWarehouseScope(task.WarehouseId, scopedWarehouseId);
        EnsureOpenTask(task);

        if (!string.IsNullOrWhiteSpace(task.AssignedTo)
            && !string.Equals(task.AssignedTo, actor, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException(
                $"Nhiệm vụ điều chuyển {task.TaskCode} đã được gán cho {task.AssignedTo}.",
                "MOVEMENT_TASK_ASSIGNED_TO_OTHER",
                "MovementTask");
        }

        if (string.IsNullOrWhiteSpace(task.AssignedTo))
        {
            if (task.Status != MovementTaskStatusEnum.Pending)
                throw new BusinessRuleException("Nhiệm vụ điều chuyển không ở trạng thái chờ nhận.", "MOVEMENT_TASK_NOT_PENDING", "MovementTask");

            task.AssignedTo = actor;
            task.AssignedAt = Now;
            task.Status = MovementTaskStatusEnum.Assigned;
            task.UpdatedAt = Now;
            await _unitOfWork.SaveChangesAsync();
        }

        return task;
    }

    public async Task<MovementTask> StartAsync(long movementTaskId, int? scopedWarehouseId, string actor)
    {
        var task = await FindTaskAsync(movementTaskId);
        EnsureWarehouseScope(task.WarehouseId, scopedWarehouseId);
        EnsureOpenTask(task);
        task.Status = MovementTaskStatusEnum.InProgress;
        task.StartedAt ??= Now;
        task.StartedBy ??= actor;
        task.AssignedTo ??= actor;
        task.AssignedAt ??= Now;
        task.UpdatedAt = Now;
        await _unitOfWork.SaveChangesAsync();
        return task;
    }

    public async Task<MovementTask> CancelAsync(long movementTaskId, string? reason, int? scopedWarehouseId, string actor)
    {
        var task = await FindTaskAsync(movementTaskId);
        EnsureWarehouseScope(task.WarehouseId, scopedWarehouseId);
        EnsureOpenTask(task);
        task.Status = MovementTaskStatusEnum.Cancelled;
        task.CancelledAt = Now;
        task.CancelledBy = actor;
        task.CancelReason = Clean(reason);
        task.UpdatedAt = Now;
        await _unitOfWork.SaveChangesAsync();
        return task;
    }

    public async Task<MovementTask> CompleteAsync(long movementTaskId, string? sourceScan, string? destinationScan, decimal confirmedQty, int? scopedWarehouseId, string actor, string? lpnScan = null)
    {
        var task = await FindTaskAsync(movementTaskId);
        EnsureWarehouseScope(task.WarehouseId, scopedWarehouseId);
        EnsureOpenTask(task);
        if (confirmedQty <= 0 || confirmedQty > task.PlannedQty)
            throw new BusinessRuleException("Số lượng xác nhận phải lớn hơn 0 và không vượt quá số lượng kế hoạch.", "MOVEMENT_CONFIRM_QTY_INVALID", "MovementTask");

        if (task.MovementMode == MovementTaskModeEnum.Lpn)
            return await CompleteLpnTaskAsync(task, sourceScan, destinationScan, confirmedQty, actor, lpnScan);

        ValidateLocationScan(sourceScan, task.SourceLocation, "source");
        ValidateLocationScan(destinationScan, task.DestinationLocation, "destination");

        // P2-R2-2: mở transaction trước khi load + check source stock để khớp với pattern Inbound/Cancel
        // (ItemLocation đã có RowVersion bảo vệ ở SaveChanges, nhưng Serializable cho UX message rõ hơn).
        await _unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        var sourceStock = await ResolveSourceStockAsync(task.ItemId, task.SourceLocationId, task.SourceItemLocationId, task.LotNumber, task.ExpiryDate, task.OwnerPartnerId);
        if (sourceStock == null)
            throw new BusinessRuleException("Không tìm thấy dòng tồn kho nguồn.", "MOVEMENT_SOURCE_STOCK_NOT_FOUND", "ItemLocation");

        var sourceAvailable = Math.Max(0, sourceStock.Quantity - sourceStock.ReservedQty);
        if (sourceAvailable < task.PlannedQty)
            throw new BusinessRuleException("Tồn kho nguồn đã thay đổi trước khi xác nhận. Vui lòng kiểm tra lại nhiệm vụ điều chuyển.", "MOVEMENT_SOURCE_STOCK_CHANGED", "ItemLocation");

        await EnsureNoActiveLpnAsync(task.ItemId, task.SourceLocationId, task.LotNumber, task.ExpiryDate, task.OwnerPartnerId);

        using var ledgerScope = _inventoryTransactionService.BeginScope(new InventoryTransactionContext
        {
            TransactionType = InventoryTransactionTypeEnum.Move,
            TransactionGroupKey = $"movement-task:{task.MovementTaskId}:complete",
            IdempotencyKeyPrefix = $"movement-task:{task.MovementTaskId}:complete",
            WarehouseId = task.WarehouseId,
            OwnerPartnerId = task.OwnerPartnerId,
            MovementTaskId = task.MovementTaskId,
            ReferenceType = "MovementTask",
            ReferenceId = task.MovementTaskId.ToString(),
            ReferenceCode = task.TaskCode,
            Actor = actor
        });

        sourceStock.Quantity -= confirmedQty;
        sourceStock.UpdatedAt = Now;

        var destinationStock = await _db.ItemLocations.FirstOrDefaultAsync(il =>
            il.ItemId == task.ItemId
            && il.OwnerPartnerId == task.OwnerPartnerId
            && il.LocationId == task.DestinationLocationId
            && il.LotNumber == task.LotNumber
            && il.ExpiryDate == task.ExpiryDate);
        if (destinationStock == null)
        {
            destinationStock = new ItemLocation
            {
                ItemId = task.ItemId,
                OwnerPartnerId = task.OwnerPartnerId,
                LocationId = task.DestinationLocationId,
                Quantity = confirmedQty,
                ReservedQty = 0,
                LotNumber = task.LotNumber,
                ExpiryDate = task.ExpiryDate,
                UpdatedAt = Now
            };
            _db.ItemLocations.Add(destinationStock);
        }
        else
        {
            destinationStock.Quantity += confirmedQty;
            destinationStock.UpdatedAt = Now;
        }

        task.ConfirmedQty = confirmedQty;
        task.Status = confirmedQty == task.PlannedQty ? MovementTaskStatusEnum.Completed : MovementTaskStatusEnum.Short;
        task.CompletedAt = Now;
        task.CompletedBy = actor;
        task.StartedAt ??= Now;
        task.StartedBy ??= actor;
        task.AssignedTo ??= actor;
        task.AssignedAt ??= Now;
        task.UpdatedAt = Now;

        _db.AuditLogs.Add(new AuditLog
        {
            TableName = "MovementTask",
            RecordId = task.MovementTaskId.ToString(),
            ActionType = task.Status == MovementTaskStatusEnum.Short ? "MOVE_SHORT" : "MOVE_COMPLETE",
            ColumnChanged = "Quantity",
            OldValue = $"SRC:{task.SourceLocation?.LocationCode};DEST:{task.DestinationLocation?.LocationCode}",
            NewValue = $"TASK:{task.TaskCode};QTY:{confirmedQty:N4};LOT:{task.LotNumber ?? "-"}",
            ChangedBy = actor,
            ChangedAt = Now,
            AppModule = "MovementTask"
        });

        if (task.TaskType == MovementTaskTypeEnum.Reslotting
            && task.UpdateDefaultLocationOnComplete
            && task.Status == MovementTaskStatusEnum.Completed)
        {
            var item = await _db.Items.FirstAsync(i => i.ItemId == task.ItemId);
            var oldDefault = item.DefaultLocationId;
            item.DefaultLocationId = task.DestinationLocationId;
            item.UpdatedAt = Now;
            _db.AuditLogs.Add(new AuditLog
            {
                TableName = "Item",
                RecordId = item.ItemId.ToString(),
                ActionType = "SLOTTING_APPLIED",
                ColumnChanged = "DefaultLocationId",
                OldValue = oldDefault?.ToString(),
                NewValue = task.DestinationLocationId.ToString(),
                ChangedBy = actor,
                ChangedAt = Now,
                AppModule = "Slotting"
            });
        }

        await _unitOfWork.SaveChangesAsync();
        await _unitOfWork.CommitAsync(); // P2-R2-2: đóng transaction Serializable đã mở ở đầu method
        return task;
    }

    private async Task<MovementTask> CompleteLpnTaskAsync(MovementTask task, string? sourceScan, string? destinationScan, decimal confirmedQty, string actor, string? lpnScan)
    {
        if (!task.LicensePlateId.HasValue)
            throw new BusinessRuleException("Nhiem vu LPN thieu LicensePlateId.", "MOVEMENT_LPN_TASK_INVALID", "MovementTask");
        if (confirmedQty != task.PlannedQty)
            throw new BusinessRuleException("LPN movement bat buoc xac nhan nguyen kien.", "MOVEMENT_LPN_PARTIAL_BLOCKED", "MovementTask");

        ValidateLocationScan(sourceScan, task.SourceLocation, "source");
        ValidateLocationScan(destinationScan, task.DestinationLocation, "destination");

        var root = await _db.LicensePlates.FirstOrDefaultAsync(l => l.LicensePlateId == task.LicensePlateId.Value && l.IsActive);
        if (root == null)
            throw new BusinessRuleException("Khong tim thay LPN cua nhiem vu.", "MOVEMENT_LPN_NOT_FOUND", "LicensePlate");
        if (!IsMovableLpnStatus(root.Status) || root.CurrentLocationId != task.SourceLocationId)
            throw new BusinessRuleException("LPN da thay doi trang thai hoac vi tri truoc khi xac nhan.", "MOVEMENT_LPN_CHANGED", "LicensePlate");
        ValidateLpnScan(lpnScan, root);

        var tree = await LoadLpnTreeAsync(root.LicensePlateId, task.WarehouseId);
        if (tree.Any(l => !IsMovableLpnStatus(l.Status)))
            throw new BusinessRuleException("Cay LPN co kien con khong o trang thai co the di chuyen.", "MOVEMENT_LPN_TREE_STATUS_BLOCKED", "LicensePlate");
        if (tree.Any(l => l.CurrentLocationId.HasValue && l.CurrentLocationId.Value != task.SourceLocationId))
            throw new BusinessRuleException("Cay LPN dang lech vi tri vat ly. Hay reconciliation truoc khi di chuyen.", "MOVEMENT_LPN_TREE_LOCATION_MISMATCH", "LicensePlate");

        var startedTx = !_unitOfWork.HasActiveTransaction;
        if (startedTx)
            await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            using var ledgerScope = _inventoryTransactionService.BeginScope(new InventoryTransactionContext
            {
                TransactionType = InventoryTransactionTypeEnum.Move,
                TransactionGroupKey = $"movement-task:{task.MovementTaskId}:lpn-moved",
                IdempotencyKeyPrefix = $"movement-task:{task.MovementTaskId}:lpn-moved",
                WarehouseId = task.WarehouseId,
                OwnerPartnerId = task.OwnerPartnerId,
                MovementTaskId = task.MovementTaskId,
                LicensePlateId = task.LicensePlateId,
                ReferenceType = "MovementTask",
                ReferenceId = task.MovementTaskId.ToString(),
                ReferenceCode = task.TaskCode,
                Actor = actor
            });
            var treeIds = tree.Select(l => l.LicensePlateId).ToList();
            await _serialInventoryService.EnsureLpnTreeHasNoOpenSerialReservationAsync(treeIds);

            foreach (var lpn in tree)
            {
                lpn.CurrentLocationId = task.DestinationLocationId;
                lpn.LocationId = task.DestinationLocationId;
                lpn.UpdatedAt = Now;
            }
            await _serialInventoryService.SyncLpnTreeLocationAsync(treeIds, task.DestinationLocationId, actor);

            var affectedItemIds = await _inventorySnapshotService.RecordAndApplyLpnMovedAsync(new LpnMovementSnapshotRequest(
                root.LicensePlateId,
                task.WarehouseId,
                task.SourceLocationId,
                task.DestinationLocationId,
                $"movement-task:{task.MovementTaskId}:lpn-moved",
                actor,
                task.SourceReason));

            task.ConfirmedQty = confirmedQty;
            task.Status = MovementTaskStatusEnum.Completed;
            task.CompletedAt = Now;
            task.CompletedBy = actor;
            task.StartedAt ??= Now;
            task.StartedBy ??= actor;
            task.AssignedTo ??= actor;
            task.AssignedAt ??= Now;
            task.UpdatedAt = Now;

            _db.AuditLogs.Add(new AuditLog
            {
                TableName = "MovementTask",
                RecordId = task.MovementTaskId.ToString(),
                ActionType = "LPN_MOVE_COMPLETE",
                ColumnChanged = "LicensePlateId",
                OldValue = $"SRC:{task.SourceLocation?.LocationCode};LPN:{task.LpnCodeSnapshot ?? root.LpnCode}",
                NewValue = $"DEST:{task.DestinationLocation?.LocationCode};QTY:{confirmedQty:N4}",
                ChangedBy = actor,
                ChangedAt = Now,
                AppModule = "MovementTask"
            });

            await _unitOfWork.SaveChangesAsync();

            if (affectedItemIds.Count > 0)
            {
                await _inventoryBalanceService.SyncCurrentStockAsync(affectedItemIds);
                await _unitOfWork.SaveChangesAsync();
            }

            if (startedTx)
                await _unitOfWork.CommitAsync();

            return task;
        }
        catch
        {
            if (startedTx)
                await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    private async Task<MovementTask> FindTaskAsync(long movementTaskId)
    {
        var task = await _db.MovementTasks
            .Include(t => t.Item)
            .Include(t => t.LicensePlate)
            .Include(t => t.SourceLocation)
            .Include(t => t.DestinationLocation)
            .FirstOrDefaultAsync(t => t.MovementTaskId == movementTaskId);
        if (task == null)
            throw new BusinessRuleException("Không tìm thấy nhiệm vụ điều chuyển.", "MOVEMENT_TASK_NOT_FOUND", "MovementTask");
        return task;
    }

    private async Task<ItemLocation?> ResolveSourceStockAsync(int itemId, int sourceLocationId, int? sourceItemLocationId, string? lotNumber, DateTime? expiryDate, int? ownerPartnerId)
    {
        if (sourceItemLocationId.HasValue)
            return await _db.ItemLocations.FirstOrDefaultAsync(il => il.ItemLocationId == sourceItemLocationId.Value && il.ItemId == itemId && il.LocationId == sourceLocationId && (!ownerPartnerId.HasValue || il.OwnerPartnerId == ownerPartnerId));

        return await _db.ItemLocations.FirstOrDefaultAsync(il =>
            il.ItemId == itemId
            && (!ownerPartnerId.HasValue || il.OwnerPartnerId == ownerPartnerId)
            && il.LocationId == sourceLocationId
            && il.LotNumber == lotNumber
            && il.ExpiryDate == expiryDate);
    }

    private async Task EnsureNoActiveLpnAsync(int itemId, int sourceLocationId, string? lotNumber, DateTime? expiryDate, int? ownerPartnerId)
    {
        var hasActiveLpn = await _db.LicensePlates.AnyAsync(l =>
            l.IsActive
            && l.OwnerPartnerId == ownerPartnerId
            && l.Status != LpnStatusEnum.Voided
            && l.CurrentLocationId == sourceLocationId
            && l.Details.Any(d =>
                d.ItemId == itemId
                && d.LotNumber == lotNumber
                && d.ExpiryDate == expiryDate));
        if (hasActiveLpn)
            throw new BusinessRuleException("Vị trí nguồn đang có mã kiện (LPN) hoạt động. Không thể điều chuyển kiểu này.", "MOVEMENT_SOURCE_LPN_BLOCKED", "LicensePlate");
    }

    private async Task EnsureNoDuplicateOpenTaskAsync(int itemId, int sourceLocationId, int destinationLocationId, MovementTaskTypeEnum taskType, int? ownerPartnerId)
    {
        var exists = await _db.MovementTasks.AnyAsync(t =>
            t.MovementMode == MovementTaskModeEnum.Item
            && t.ItemId == itemId
            && t.SourceLocationId == sourceLocationId
            && t.DestinationLocationId == destinationLocationId
            && t.OwnerPartnerId == ownerPartnerId
            && t.TaskType == taskType
            && OpenStatuses.Contains(t.Status));
        if (exists)
            throw new BusinessRuleException("Đã có nhiệm vụ điều chuyển đang mở cho vật tư/nguồn/đích/loại này.", "MOVEMENT_DUPLICATE_OPEN_TASK", "MovementTask");
    }

    private async Task EnsureNoDuplicateOpenLpnTaskAsync(long licensePlateId)
    {
        var exists = await _db.MovementTasks.AnyAsync(t =>
            t.MovementMode == MovementTaskModeEnum.Lpn
            && t.LicensePlateId == licensePlateId
            && OpenStatuses.Contains(t.Status));
        if (exists)
            throw new BusinessRuleException("Da co nhiem vu LPN dang mo cho ma kien nay.", "MOVEMENT_DUPLICATE_OPEN_LPN_TASK", "MovementTask");
    }

    private async Task<LicensePlate?> FindMovableLpnForSourceAsync(int itemId, int sourceLocationId, string? lotNumber, DateTime? expiryDate, int warehouseId)
        => await _db.LicensePlates
            .Where(l => l.IsActive
                && l.WarehouseId == warehouseId
                && l.CurrentLocationId == sourceLocationId
                && (l.Status == LpnStatusEnum.Stored
                    || l.Status == LpnStatusEnum.Allocated
                    || l.Status == LpnStatusEnum.Picked
                    || l.Status == LpnStatusEnum.Packed)
                && l.Details.Any(d => d.ItemId == itemId && d.LotNumber == lotNumber && d.ExpiryDate == expiryDate))
            .OrderBy(l => l.CreatedAt)
            .FirstOrDefaultAsync();

    private async Task<List<LicensePlate>> LoadLpnTreeAsync(long rootLicensePlateId, int warehouseId)
    {
        var lpns = await _db.LicensePlates
            .Where(l => l.WarehouseId == warehouseId && l.IsActive)
            .ToListAsync();

        var childrenByParent = lpns
            .Where(l => l.ParentLpnId.HasValue)
            .GroupBy(l => l.ParentLpnId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<LicensePlate>();
        var stack = new Stack<long>();
        var seen = new HashSet<long>();
        stack.Push(rootLicensePlateId);

        while (stack.Count > 0)
        {
            var id = stack.Pop();
            if (!seen.Add(id))
                continue;

            var lpn = lpns.FirstOrDefault(l => l.LicensePlateId == id);
            if (lpn == null)
                continue;

            result.Add(lpn);
            if (childrenByParent.TryGetValue(id, out var children))
            {
                foreach (var child in children)
                    stack.Push(child.LicensePlateId);
            }
        }

        return result;
    }

    private static bool IsMovableLpnStatus(LpnStatusEnum status)
        => status is LpnStatusEnum.Stored or LpnStatusEnum.Allocated or LpnStatusEnum.Picked or LpnStatusEnum.Packed;

    private async Task<string> GenerateTaskCodeAsync(MovementTaskTypeEnum taskType)
    {
        var prefix = taskType switch
        {
            MovementTaskTypeEnum.Replenishment => "MVT-REP",
            MovementTaskTypeEnum.Reslotting => "MVT-SLT",
            _ => "MVT-REL"
        };
        var dayPrefix = $"{prefix}-{Now:yyyyMMdd}-";
        var count = await _db.MovementTasks.CountAsync(t => t.TaskCode.StartsWith(dayPrefix));
        return $"{dayPrefix}{count + 1:0000}";
    }

    private static void EnsureWarehouseScope(int warehouseId, int? scopedWarehouseId)
    {
        if (scopedWarehouseId.HasValue && warehouseId != scopedWarehouseId.Value)
            throw new UnauthorizedAccessException("Bạn không thể thao tác nhiệm vụ điều chuyển từ kho khác.");
    }

    private static void EnsureOpenTask(MovementTask task)
    {
        if (!OpenStatuses.Contains(task.Status))
            throw new BusinessRuleException("Nhiệm vụ điều chuyển đã kết thúc.", "MOVEMENT_TASK_CLOSED", "MovementTask");
    }

    private static void ValidateLocationScan(string? scan, Location? expectedLocation, string label)
    {
        if (string.IsNullOrWhiteSpace(scan) || expectedLocation == null)
            return;

        var normalized = scan.Trim();
        var matches = string.Equals(normalized, expectedLocation.LocationCode, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(expectedLocation.Barcode) && string.Equals(normalized, expectedLocation.Barcode, StringComparison.OrdinalIgnoreCase));
        if (!matches)
            throw new BusinessRuleException($"Vị trí {label} quét không khớp với nhiệm vụ điều chuyển.", "MOVEMENT_LOCATION_SCAN_MISMATCH", "MovementTask");
    }

    private static void ValidateLpnScan(string? scan, LicensePlate expectedLpn)
    {
        if (string.IsNullOrWhiteSpace(scan))
            throw new BusinessRuleException("Vui long quet ma LPN truoc khi xac nhan di chuyen.", "MOVEMENT_LPN_SCAN_REQUIRED", "MovementTask");

        var normalized = scan.Trim();
        if (!string.Equals(normalized, expectedLpn.LpnCode, StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("Ma LPN quet khong khop voi nhiem vu di chuyen.", "MOVEMENT_LPN_SCAN_MISMATCH", "MovementTask");
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
