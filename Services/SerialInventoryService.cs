using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public sealed record SerialPickSlice(long? StockReservationId, long VoucherId, long? VoucherDetailId, decimal Qty);

public interface ISerialInventoryService
{
    Task AllocateForVoucherAsync(long voucherId, string actor, string idempotencyKey);
    Task ConfirmPickTaskSerialsAsync(PickTask task, IReadOnlyList<SerialPickSlice> slices, IReadOnlyList<string> serialCodes, string actor);
    Task MoveBulkReservedSerialsToStagingAsync(PickTask bulkTask, IReadOnlyList<string> serialCodes, string actor);
    Task BackfillPickedReservationsForVoucherAsync(long voucherId, string actor);
    Task ConsumePickedSerialsForReservationAsync(StockReservation reservation, Voucher voucher, int requiredCount, string actor);
    Task ReleaseOpenForReservationAsync(StockReservation reservation, string actor, int? maxCount = null, string? reason = null);
    Task ReleaseOpenForVoucherAsync(long voucherId, string actor, string? reason = null);
    Task EnsureLpnTreeHasNoOpenSerialReservationAsync(IEnumerable<long> licensePlateIds);
    Task SyncLpnTreeLocationAsync(IEnumerable<long> licensePlateIds, int destinationLocationId, string actor);
}

public class SerialInventoryService : ISerialInventoryService
{
    private static readonly SerialReservationStatusEnum[] OpenReservationStatuses =
    {
        SerialReservationStatusEnum.Reserved,
        SerialReservationStatusEnum.Picked
    };

    private readonly AppDbContext _db;
    private static DateTime Now => VietnamTime.Now;

    public SerialInventoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task AllocateForVoucherAsync(long voucherId, string actor, string idempotencyKey)
    {
        if (await OperationExistsAsync(idempotencyKey))
            return;

        var voucher = await _db.Vouchers.AsNoTracking().FirstOrDefaultAsync(v => v.VoucherId == voucherId);
        if (voucher == null)
            throw WmsExceptions.VoucherNotFound();

        var reservations = await _db.StockReservations
            .Include(r => r.Item)
            .Where(r => r.VoucherId == voucherId && r.Status == ReservationStatusEnum.Active)
            .OrderBy(r => r.StockReservationId)
            .ToListAsync();

        var serialReservations = reservations
            .Where(r => r.Item?.TrackSerial == true && r.ReservedQty > r.ConsumedQty + r.ReleasedQty)
            .ToList();

        if (serialReservations.Count == 0)
        {
            AddOperation(idempotencyKey, "AllocateForVoucher", "Voucher", voucherId, new { voucherId, skipped = true });
            return;
        }

        var reservationIds = serialReservations.Select(r => r.StockReservationId).ToList();
        var taskAllocations = await _db.PickTaskAllocations
            .Include(a => a.PickTask)
            .Where(a => reservationIds.Contains(a.StockReservationId))
            .OrderBy(a => a.PickTaskAllocationId)
            .ToListAsync();

        foreach (var reservation in serialReservations)
        {
            var openQty = reservation.ReservedQty - reservation.ConsumedQty - reservation.ReleasedQty;
            var required = RequireIntegerSerialQty(reservation.Item!, openQty);
            var alreadyReserved = await _db.SerialReservations
                .CountAsync(r => r.StockReservationId == reservation.StockReservationId
                    && OpenReservationStatuses.Contains(r.Status));
            var need = required - alreadyReserved;
            if (need <= 0)
                continue;

            var candidateQuery = _db.SerialNumbers
                .Where(s => s.WarehouseId == voucher.WarehouseId
                    && s.ItemId == reservation.ItemId
                    && s.OwnerPartnerId == voucher.OwnerPartnerId
                    && s.LocationId == reservation.LocationId
                    && s.Status == SerialNumberStatusEnum.Active
                    && s.HoldStatus == InventoryHoldStatusEnum.Available
                    && s.LotNumber == reservation.LotNumber
                    && s.ExpiryDate == reservation.ExpiryDate
                    && !_db.SerialReservations.Any(sr => sr.SerialNumberId == s.SerialNumberId
                        && OpenReservationStatuses.Contains(sr.Status)))
                .OrderBy(s => s.ExpiryDate.HasValue ? 0 : 1)
                .ThenBy(s => s.ExpiryDate)
                .ThenBy(s => s.CreatedAt)
                .ThenBy(s => s.SerialCode);

            var serials = await candidateQuery.Take(need).ToListAsync();
            if (serials.Count != need)
                throw WmsExceptions.SerialMissing(reservation.Item!.ItemCode, required, alreadyReserved + serials.Count);

            var allocationPlan = BuildAllocationPlan(reservation, taskAllocations.Where(a => a.StockReservationId == reservation.StockReservationId).ToList(), need);
            for (var i = 0; i < serials.Count; i++)
            {
                var serial = serials[i];
                var plan = allocationPlan[i];
                var serialReservation = new SerialReservation
                {
                    SerialNumberId = serial.SerialNumberId,
                    StockReservationId = reservation.StockReservationId,
                    PickTaskId = plan.PickTaskId,
                    VoucherId = plan.VoucherId,
                    VoucherDetailId = plan.VoucherDetailId,
                    WarehouseId = serial.WarehouseId,
                    ItemId = serial.ItemId,
                    LocationId = reservation.LocationId,
                    LicensePlateId = serial.LicensePlateId,
                    LotNumber = serial.LotNumber,
                    ExpiryDate = serial.ExpiryDate,
                    HoldStatus = serial.HoldStatus,
                    Status = SerialReservationStatusEnum.Reserved,
                    IdempotencyKey = $"{idempotencyKey}:reservation:{reservation.StockReservationId}:{serial.SerialNumberId}",
                    ReservedBy = actor,
                    ReservedAt = Now,
                    CreatedAt = Now,
                    UpdatedAt = Now
                };
                _db.SerialReservations.Add(serialReservation);

                serial.Status = SerialNumberStatusEnum.Allocated;
                serial.UpdatedAt = Now;
            }
        }

        AddOperation(idempotencyKey, "AllocateForVoucher", "Voucher", voucherId, new { voucherId, serialReservations = true });
    }

    public async Task ConfirmPickTaskSerialsAsync(PickTask task, IReadOnlyList<SerialPickSlice> slices, IReadOnlyList<string> serialCodes, string actor)
    {
        var item = task.Item ?? await _db.Items.FirstAsync(i => i.ItemId == task.ItemId);
        var normalizedCodes = NormalizeSerialCodes(serialCodes);
        var required = RequireIntegerSerialQty(item, slices.Sum(s => s.Qty));
        if (normalizedCodes.Count != required)
            throw WmsExceptions.SerialMissing(item.ItemCode, required, normalizedCodes.Count);

        var serialReservations = await LoadOpenReservationsForPickAsync(task, slices);
        if (serialReservations.Count == 0)
        {
            serialReservations = await CreateLegacyPickReservationsAsync(task, slices, normalizedCodes, actor);
        }

        var byCode = serialReservations
            .Where(r => r.Status == SerialReservationStatusEnum.Reserved && r.SerialNumber != null)
            .ToDictionary(r => r.SerialNumber!.SerialCode, StringComparer.OrdinalIgnoreCase);

        var missing = normalizedCodes.Where(code => !byCode.ContainsKey(code)).ToList();
        if (missing.Count > 0)
            throw new BusinessRuleException(
                $"Serial khong thuoc danh sach da giu cho task nay: {string.Join(", ", missing.Take(5))}",
                "SERIAL_NOT_RESERVED_FOR_TASK",
                "SerialReservation");

        foreach (var code in normalizedCodes)
        {
            var reservation = byCode[code];
            var serial = reservation.SerialNumber!;
            // P2-5: chỉ chấp nhận reservation chưa gắn task khác, tránh nhận serial đã reserved cho PickTask khác.
            if (reservation.PickTaskId.HasValue && reservation.PickTaskId.Value != task.PickTaskId)
                throw new BusinessRuleException(
                    $"Serial [{serial.SerialCode}] đã được giữ cho task #{reservation.PickTaskId.Value} khác.",
                    "SERIAL_RESERVED_FOR_OTHER_TASK",
                    "SerialReservation");
            if (serial.OwnerPartnerId != task.OwnerPartnerId)
                throw new BusinessRuleException($"Serial [{serial.SerialCode}] không cùng chủ hàng với nhiệm vụ lấy hàng.", "SERIAL_OWNER_MISMATCH", "SerialNumber");
            if (serial.LocationId != task.SourceLocationId)
                throw new BusinessRuleException($"Serial [{serial.SerialCode}] khong nam tai vi tri nguon cua task.", "SERIAL_LOCATION_MISMATCH", "SerialNumber");
            if (serial.HoldStatus != InventoryHoldStatusEnum.Available || reservation.HoldStatus != InventoryHoldStatusEnum.Available)
                throw new BusinessRuleException($"Serial [{serial.SerialCode}] dang bi hold, khong the pick.", "SERIAL_HOLD_BLOCKED", "SerialNumber");

            reservation.Status = SerialReservationStatusEnum.Picked;
            reservation.PickTaskId = task.PickTaskId;
            reservation.PickedBy = actor;
            reservation.PickedAt = Now;
            reservation.UpdatedAt = Now;

            serial.Status = SerialNumberStatusEnum.Picked;
            serial.LicensePlateId = null;
            serial.UpdatedAt = Now;

            if (!await _db.PickTaskSerialAssignments.AnyAsync(a =>
                    a.PickTaskId == task.PickTaskId
                    && a.SerialNumberId == serial.SerialNumberId
                    && a.VoidedAt == null))
            {
                _db.PickTaskSerialAssignments.Add(new PickTaskSerialAssignment
                {
                    PickTaskId = task.PickTaskId,
                    VoucherId = reservation.VoucherId,
                    VoucherDetailId = reservation.VoucherDetailId,
                    SerialNumberId = serial.SerialNumberId,
                    SerialReservation = reservation,
                    SerialCode = serial.SerialCode,
                    ScannedBy = actor,
                    ScannedAt = Now
                });
            }
        }

        AddOperation($"pick-task:{task.PickTaskId}:serial-pick:{string.Join("|", normalizedCodes.OrderBy(x => x))}",
            "ConfirmPickTaskSerials", "PickTask", task.PickTaskId, new { task.PickTaskId, count = normalizedCodes.Count });
    }

    public async Task MoveBulkReservedSerialsToStagingAsync(PickTask bulkTask, IReadOnlyList<string> serialCodes, string actor)
    {
        var item = bulkTask.Item ?? await _db.Items.FirstAsync(i => i.ItemId == bulkTask.ItemId);
        if (!bulkTask.TargetLocationId.HasValue)
            throw new BusinessRuleException("Bulk pick task thieu vi tri staging.", "BULK_TARGET_LOCATION_MISSING", "PickTask");

        var normalizedCodes = NormalizeSerialCodes(serialCodes);
        var required = RequireIntegerSerialQty(item, bulkTask.TargetQty - bulkTask.PickedQty);
        if (normalizedCodes.Count != required)
            throw WmsExceptions.SerialMissing(item.ItemCode, required, normalizedCodes.Count);

        var childReservationIds = await _db.PickTaskAllocations
            .Where(a => a.PickTask != null
                && a.PickTask.ParentPickTaskId == bulkTask.PickTaskId
                && a.PickTask.PickTaskMode == PickTaskModeEnum.Sort)
            .Select(a => a.StockReservationId)
            .Distinct()
            .ToListAsync();

        var reservations = childReservationIds.Count == 0
            ? new List<SerialReservation>()
            : await _db.SerialReservations
                .Include(r => r.SerialNumber)
                .Where(r => childReservationIds.Contains(r.StockReservationId ?? 0)
                    && r.Status == SerialReservationStatusEnum.Reserved)
                .ToListAsync();

        if (reservations.Count == 0)
        {
            await MoveLegacyBulkSerialsAsync(bulkTask, normalizedCodes);
            return;
        }

        var byCode = reservations
            .Where(r => r.SerialNumber != null)
            .ToDictionary(r => r.SerialNumber!.SerialCode, StringComparer.OrdinalIgnoreCase);
        var missing = normalizedCodes.Where(code => !byCode.ContainsKey(code)).ToList();
        if (missing.Count > 0)
            throw new BusinessRuleException($"Serial khong thuoc batch da giu cho: {string.Join(", ", missing.Take(5))}", "SERIAL_NOT_RESERVED_FOR_BULK", "SerialReservation");

        foreach (var code in normalizedCodes)
        {
            var reservation = byCode[code];
            var serial = reservation.SerialNumber!;
            if (serial.OwnerPartnerId != bulkTask.OwnerPartnerId)
                throw new BusinessRuleException($"Serial [{serial.SerialCode}] không cùng chủ hàng với nhiệm vụ lấy tổng.", "SERIAL_OWNER_MISMATCH", "SerialNumber");
            if (serial.LocationId != bulkTask.SourceLocationId)
                throw new BusinessRuleException($"Serial [{serial.SerialCode}] khong nam tai vi tri nguon cua bulk task.", "SERIAL_LOCATION_MISMATCH", "SerialNumber");

            serial.LocationId = bulkTask.TargetLocationId.Value;
            serial.LicensePlateId = null;
            serial.UpdatedAt = Now;
            reservation.LocationId = bulkTask.TargetLocationId.Value;
            reservation.UpdatedAt = Now;
        }

        AddOperation($"pick-task:{bulkTask.PickTaskId}:serial-bulk-stage",
            "MoveBulkReservedSerialsToStaging", "PickTask", bulkTask.PickTaskId, new { bulkTask.PickTaskId, count = normalizedCodes.Count });
    }

    public async Task BackfillPickedReservationsForVoucherAsync(long voucherId, string actor)
    {
        var assignments = await _db.PickTaskSerialAssignments
            .Include(a => a.SerialNumber)
            .Include(a => a.PickTask)
            .Where(a => a.VoucherId == voucherId
                && a.VoidedAt == null
                && a.SerialReservationId == null)
            .OrderBy(a => a.ScannedAt)
            .ToListAsync();

        foreach (var assignment in assignments)
        {
            if (assignment.SerialNumber == null)
                continue;

            var serial = assignment.SerialNumber;
            var existing = await _db.SerialReservations.FirstOrDefaultAsync(r => r.SerialNumberId == serial.SerialNumberId
                && OpenReservationStatuses.Contains(r.Status));
            if (existing != null)
            {
                assignment.SerialReservationId = existing.SerialReservationId;
                continue;
            }

            var stockReservation = await _db.StockReservations
                .Where(r => r.VoucherId == assignment.VoucherId
                    && r.VoucherDetailId == assignment.VoucherDetailId
                    && r.ItemId == serial.ItemId
                    && r.LocationId == serial.LocationId
                    && r.LotNumber == serial.LotNumber
                    && r.ExpiryDate == serial.ExpiryDate
                    && r.Status == ReservationStatusEnum.Active)
                .OrderBy(r => r.StockReservationId)
                .FirstOrDefaultAsync();

            var locationId = serial.LocationId ?? assignment.PickTask?.SourceLocationId ?? stockReservation?.LocationId;
            if (!locationId.HasValue)
                throw new BusinessRuleException($"Serial [{serial.SerialCode}] khong co vi tri hop le de backfill reservation.", "SERIAL_BACKFILL_LOCATION_MISSING", "SerialReservation");

            var backfill = new SerialReservation
            {
                SerialNumberId = serial.SerialNumberId,
                StockReservationId = stockReservation?.StockReservationId,
                PickTaskId = assignment.PickTaskId,
                VoucherId = assignment.VoucherId,
                VoucherDetailId = assignment.VoucherDetailId,
                WarehouseId = serial.WarehouseId,
                ItemId = serial.ItemId,
                LocationId = locationId.Value,
                LicensePlateId = serial.LicensePlateId,
                LotNumber = serial.LotNumber,
                ExpiryDate = serial.ExpiryDate,
                HoldStatus = serial.HoldStatus,
                Status = SerialReservationStatusEnum.Picked,
                IdempotencyKey = $"legacy-assignment:{assignment.PickTaskSerialAssignmentId}:{serial.SerialNumberId}",
                ReservedBy = actor,
                ReservedAt = assignment.ScannedAt,
                PickedBy = assignment.ScannedBy,
                PickedAt = assignment.ScannedAt,
                CreatedAt = assignment.ScannedAt,
                UpdatedAt = Now,
                Notes = "Backfilled from PickTaskSerialAssignment."
            };
            _db.SerialReservations.Add(backfill);
            assignment.SerialReservation = backfill;
            serial.Status = SerialNumberStatusEnum.Picked;
            serial.UpdatedAt = Now;
        }
    }

    public async Task ConsumePickedSerialsForReservationAsync(StockReservation reservation, Voucher voucher, int requiredCount, string actor)
    {
        var serialReservations = await _db.SerialReservations
            .Include(r => r.SerialNumber)
            .Where(r => r.StockReservationId == reservation.StockReservationId
                && r.Status == SerialReservationStatusEnum.Picked)
            .OrderBy(r => r.PickedAt)
            .ThenBy(r => r.SerialReservationId)
            .Take(requiredCount)
            .ToListAsync();

        if (serialReservations.Count != requiredCount)
            throw WmsExceptions.SerialMissing(reservation.Item?.ItemCode ?? reservation.ItemId.ToString(), requiredCount, serialReservations.Count);

        foreach (var serialReservation in serialReservations)
        {
            var serial = serialReservation.SerialNumber!;
            serialReservation.Status = SerialReservationStatusEnum.Consumed;
            serialReservation.ConsumedBy = actor;
            serialReservation.ConsumedAt = Now;
            serialReservation.UpdatedAt = Now;

            var assignment = await _db.PickTaskSerialAssignments
                .FirstOrDefaultAsync(a => a.SerialReservationId == serialReservation.SerialReservationId
                    && a.PostedAt == null
                    && a.VoidedAt == null);
            if (assignment != null)
                assignment.PostedAt = Now;

            serial.LicensePlateId = null;
            serial.UpdatedAt = Now;

            if (voucher.VoucherType == VoucherTypeEnum.ChuyenKho)
            {
                var targetDetail = voucher.Details.FirstOrDefault(d => d.VoucherDetailId == reservation.VoucherDetailId);
                if (targetDetail?.DestLocationId == null)
                    throw WmsExceptions.TransferDestLocationMissingForSerial();

                serial.Status = SerialNumberStatusEnum.Active;
                serial.LocationId = targetDetail.DestLocationId.Value;
                serial.HoldStatus = InventoryHoldStatusEnum.Available;
                serial.ConsumedAt = null;
                serial.ConsumedBy = null;
                serial.ConsumedVoucherId = null;
                serial.ConsumedPickTaskId = null;
            }
            else
            {
                serial.Status = SerialNumberStatusEnum.Consumed;
                serial.LocationId = null;
                serial.ConsumedAt = Now;
                serial.ConsumedBy = actor;
                serial.ConsumedVoucherId = voucher.VoucherId;
                serial.ConsumedPickTaskId = serialReservation.PickTaskId;
            }
        }
    }

    public async Task ReleaseOpenForReservationAsync(StockReservation reservation, string actor, int? maxCount = null, string? reason = null)
    {
        var query = _db.SerialReservations
            .Include(r => r.SerialNumber)
            .Where(r => r.StockReservationId == reservation.StockReservationId
                && OpenReservationStatuses.Contains(r.Status))
            .OrderBy(r => r.Status == SerialReservationStatusEnum.Reserved ? 0 : 1)
            .ThenBy(r => r.SerialReservationId);

        var serialReservations = maxCount.HasValue
            ? await query.Take(maxCount.Value).ToListAsync()
            : await query.ToListAsync();

        foreach (var serialReservation in serialReservations)
            await ReleaseReservationAsync(serialReservation, actor, reason);
    }

    public async Task ReleaseOpenForVoucherAsync(long voucherId, string actor, string? reason = null)
    {
        var serialReservations = await _db.SerialReservations
            .Include(r => r.SerialNumber)
            .Where(r => r.VoucherId == voucherId && OpenReservationStatuses.Contains(r.Status))
            .ToListAsync();

        foreach (var serialReservation in serialReservations)
            await ReleaseReservationAsync(serialReservation, actor, reason);
    }

    public async Task EnsureLpnTreeHasNoOpenSerialReservationAsync(IEnumerable<long> licensePlateIds)
    {
        var ids = licensePlateIds.Distinct().ToList();
        if (ids.Count == 0)
            return;

        var blocked = await _db.SerialNumbers
            .Where(s => s.LicensePlateId.HasValue
                && ids.Contains(s.LicensePlateId.Value)
                && (s.Status == SerialNumberStatusEnum.Allocated || s.Status == SerialNumberStatusEnum.Picked
                    || _db.SerialReservations.Any(r => r.SerialNumberId == s.SerialNumberId
                        && OpenReservationStatuses.Contains(r.Status))))
            .OrderBy(s => s.SerialCode)
            .Select(s => s.SerialCode)
            .Take(5)
            .ToListAsync();

        if (blocked.Count > 0)
            throw new BusinessRuleException(
                $"LPN dang chua serial da duoc allocate/pick: {string.Join(", ", blocked)}",
                "LPN_SERIAL_RESERVATION_BLOCKED",
                "SerialReservation");
    }

    public async Task SyncLpnTreeLocationAsync(IEnumerable<long> licensePlateIds, int destinationLocationId, string actor)
    {
        var ids = licensePlateIds.Distinct().ToList();
        if (ids.Count == 0)
            return;

        await EnsureLpnTreeHasNoOpenSerialReservationAsync(ids);
        var serials = await _db.SerialNumbers
            .Where(s => s.LicensePlateId.HasValue
                && ids.Contains(s.LicensePlateId.Value)
                && s.Status == SerialNumberStatusEnum.Active)
            .ToListAsync();

        foreach (var serial in serials)
        {
            serial.LocationId = destinationLocationId;
            serial.UpdatedAt = Now;
        }

        AddOperation($"lpn-tree:{string.Join("-", ids.OrderBy(x => x))}:serial-location:{destinationLocationId}",
            "SyncLpnTreeLocation", "LicensePlate", ids.First(), new { ids, destinationLocationId, actor });
    }

    private async Task<List<SerialReservation>> LoadOpenReservationsForPickAsync(PickTask task, IReadOnlyList<SerialPickSlice> slices)
    {
        var stockReservationIds = slices
            .Where(s => s.StockReservationId.HasValue)
            .Select(s => s.StockReservationId!.Value)
            .Distinct()
            .ToList();

        return await _db.SerialReservations
            .Include(r => r.SerialNumber)
            .Where(r => OpenReservationStatuses.Contains(r.Status)
                && ((r.PickTaskId == task.PickTaskId)
                    || (r.StockReservationId.HasValue && stockReservationIds.Contains(r.StockReservationId.Value))))
            .ToListAsync();
    }

    private async Task<List<SerialReservation>> CreateLegacyPickReservationsAsync(PickTask task, IReadOnlyList<SerialPickSlice> slices, IReadOnlyList<string> normalizedCodes, string actor)
    {
        var serials = await _db.SerialNumbers
            .Where(s => s.Status == SerialNumberStatusEnum.Active
                && s.ItemId == task.ItemId
                && s.OwnerPartnerId == task.OwnerPartnerId
                && s.LocationId == task.SourceLocationId
                && normalizedCodes.Contains(s.SerialCode)
                && (task.LotNumber == null || s.LotNumber == task.LotNumber)
                && (!task.ExpiryDate.HasValue || s.ExpiryDate == task.ExpiryDate)
                && !_db.SerialReservations.Any(r => r.SerialNumberId == s.SerialNumberId
                    && OpenReservationStatuses.Contains(r.Status)))
            .ToListAsync();

        if (serials.Count != normalizedCodes.Count)
        {
            var item = task.Item ?? await _db.Items.FirstAsync(i => i.ItemId == task.ItemId);
            throw WmsExceptions.SerialMissing(item.ItemCode, normalizedCodes.Count, serials.Count);
        }

        var firstSlice = slices.FirstOrDefault();
        var result = new List<SerialReservation>();
        foreach (var serial in serials)
        {
            var reservation = new SerialReservation
            {
                SerialNumberId = serial.SerialNumberId,
                StockReservationId = firstSlice?.StockReservationId,
                PickTaskId = task.PickTaskId,
                VoucherId = firstSlice?.VoucherId ?? task.VoucherId,
                VoucherDetailId = firstSlice?.VoucherDetailId ?? task.VoucherDetailId,
                WarehouseId = serial.WarehouseId,
                ItemId = serial.ItemId,
                LocationId = serial.LocationId ?? task.SourceLocationId,
                LicensePlateId = serial.LicensePlateId,
                LotNumber = serial.LotNumber,
                ExpiryDate = serial.ExpiryDate,
                HoldStatus = serial.HoldStatus,
                Status = SerialReservationStatusEnum.Reserved,
                IdempotencyKey = $"legacy-pick:{task.PickTaskId}:{serial.SerialNumberId}",
                ReservedBy = actor,
                ReservedAt = Now,
                CreatedAt = Now,
                UpdatedAt = Now,
                SerialNumber = serial
            };
            _db.SerialReservations.Add(reservation);
            serial.Status = SerialNumberStatusEnum.Allocated;
            serial.UpdatedAt = Now;
            result.Add(reservation);
        }

        return result;
    }

    private async Task MoveLegacyBulkSerialsAsync(PickTask bulkTask, IReadOnlyList<string> normalizedCodes)
    {
        var serials = await _db.SerialNumbers
            .Where(s => s.Status == SerialNumberStatusEnum.Active
                && s.ItemId == bulkTask.ItemId
                && s.OwnerPartnerId == bulkTask.OwnerPartnerId
                && s.LocationId == bulkTask.SourceLocationId
                && normalizedCodes.Contains(s.SerialCode)
                && (bulkTask.LotNumber == null || s.LotNumber == bulkTask.LotNumber)
                && (!bulkTask.ExpiryDate.HasValue || s.ExpiryDate == bulkTask.ExpiryDate))
            .ToListAsync();
        if (serials.Count != normalizedCodes.Count)
        {
            var item = bulkTask.Item ?? await _db.Items.FirstAsync(i => i.ItemId == bulkTask.ItemId);
            throw WmsExceptions.SerialMissing(item.ItemCode, normalizedCodes.Count, serials.Count);
        }

        foreach (var serial in serials)
        {
            serial.LocationId = bulkTask.TargetLocationId!.Value;
            serial.LicensePlateId = null;
            serial.UpdatedAt = Now;
        }
    }

    private async Task ReleaseReservationAsync(SerialReservation serialReservation, string actor, string? reason)
    {
        serialReservation.Status = SerialReservationStatusEnum.Released;
        serialReservation.ReleasedBy = actor;
        serialReservation.ReleasedAt = Now;
        serialReservation.UpdatedAt = Now;
        serialReservation.Notes = string.IsNullOrWhiteSpace(reason)
            ? serialReservation.Notes
            : string.IsNullOrWhiteSpace(serialReservation.Notes)
                ? reason
                : $"{serialReservation.Notes}; {reason}";

        if (serialReservation.SerialNumber != null
            && serialReservation.SerialNumber.Status is SerialNumberStatusEnum.Allocated or SerialNumberStatusEnum.Picked)
        {
            serialReservation.SerialNumber.Status = SerialNumberStatusEnum.Active;
            serialReservation.SerialNumber.UpdatedAt = Now;
        }

        var assignment = await _db.PickTaskSerialAssignments
            .FirstOrDefaultAsync(a => a.SerialReservationId == serialReservation.SerialReservationId
                && a.VoidedAt == null
                && a.PostedAt == null);
        if (assignment != null)
        {
            assignment.VoidedAt = Now;
            assignment.VoidedBy = actor;
        }
    }

    private static List<(long? PickTaskId, long VoucherId, long? VoucherDetailId)> BuildAllocationPlan(
        StockReservation reservation,
        IReadOnlyList<PickTaskAllocation> allocations,
        int required)
    {
        var plan = new List<(long? PickTaskId, long VoucherId, long? VoucherDetailId)>();
        foreach (var allocation in allocations)
        {
            var count = RequireIntegerQty(allocation.AllocatedQty);
            for (var i = 0; i < count && plan.Count < required; i++)
                plan.Add((allocation.PickTaskId, allocation.VoucherId, allocation.VoucherDetailId));
        }

        while (plan.Count < required)
            plan.Add((null, reservation.VoucherId, reservation.VoucherDetailId));

        return plan;
    }

    private int RequireIntegerSerialQty(Item item, decimal qty)
    {
        if (qty != decimal.Truncate(qty))
            throw WmsExceptions.SerialNotInteger(item.ItemCode);
        return RequireIntegerQty(qty);
    }

    private static int RequireIntegerQty(decimal qty)
        => (int)Math.Ceiling(qty);

    private static List<string> NormalizeSerialCodes(IEnumerable<string> serialCodes)
    {
        var codes = serialCodes
            .Select(s => (s ?? string.Empty).Trim().ToUpperInvariant())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return codes;
    }

    private void AddOperation(string idempotencyKey, string operationType, string referenceType, long referenceId, object payload)
    {
        if (_db.SerialInventoryOperations.Local.Any(o => o.IdempotencyKey == idempotencyKey))
            return;

        _db.SerialInventoryOperations.Add(new SerialInventoryOperation
        {
            IdempotencyKey = idempotencyKey,
            OperationType = operationType,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Status = SerialInventoryOperationStatusEnum.Applied,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAt = Now,
            AppliedAt = Now
        });
    }

    private async Task<bool> OperationExistsAsync(string idempotencyKey)
        => _db.SerialInventoryOperations.Local.Any(o => o.IdempotencyKey == idempotencyKey)
            || await _db.SerialInventoryOperations.AnyAsync(o => o.IdempotencyKey == idempotencyKey);
}
