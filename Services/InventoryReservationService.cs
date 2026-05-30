using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public interface IInventoryReservationService
{
    /// <summary>
    /// Tính lại ReservedQty của các ItemLocation từ 3 nguồn (StockReservation/Kitting/Vas).
    /// LƯU Ý: method này chỉ mutate entity tracked, caller PHẢI gọi <c>SaveChangesAsync</c>
    /// (thường nằm trong cùng transaction outer). Nếu quên, ReservedQty sẽ không persist.
    /// </summary>
    Task RecalculateReservedQtyAsync(IEnumerable<int> itemLocationIds, InventoryTransactionContext? ledgerContext = null);
}

public class InventoryReservationService : IInventoryReservationService
{
    private readonly AppDbContext _db;
    private readonly IInventoryTransactionService _inventoryTransactionService;

    public InventoryReservationService(AppDbContext db, IInventoryTransactionService? inventoryTransactionService = null)
    {
        _db = db;
        _inventoryTransactionService = inventoryTransactionService ?? new InventoryTransactionService(db);
    }

    public async Task RecalculateReservedQtyAsync(IEnumerable<int> itemLocationIds, InventoryTransactionContext? ledgerContext = null)
    {
        using var ledgerScope = ledgerContext == null ? null : _inventoryTransactionService.BeginScope(ledgerContext);
        var ids = itemLocationIds.Distinct().ToList();
        if (ids.Count == 0) return;

        var locRows = await _db.ItemLocations.Where(x => ids.Contains(x.ItemLocationId)).ToListAsync();
        var keyRows = locRows.Select(l => new { l.ItemId, l.LocationId, l.LotNumber, l.ExpiryDate, l.OwnerPartnerId }).Distinct().ToList();
        var itemIds = keyRows.Select(k => k.ItemId).Distinct().ToList();
        var locationIds = keyRows.Select(k => k.LocationId).Distinct().ToList();
        var activeReservations = await _db.StockReservations.AsNoTracking()
            .Where(r => r.Status == ReservationStatusEnum.Active && itemIds.Contains(r.ItemId) && locationIds.Contains(r.LocationId))
            .Select(r => new { r.ItemId, r.LocationId, r.LotNumber, r.ExpiryDate, r.OwnerPartnerId, OpenQty = (r.ReservedQty - r.ConsumedQty - r.ReleasedQty) })
            .ToListAsync();

        var activeKittingReservations = await _db.KittingWorkOrderLines.AsNoTracking()
            .Where(r => r.Status == KittingWorkOrderLineStatusEnum.Reserved
                && itemIds.Contains(r.ComponentItemId)
                && r.SourceLocationId.HasValue
                && locationIds.Contains(r.SourceLocationId.Value))
            .Select(r => new
            {
                ItemId = r.ComponentItemId,
                LocationId = r.SourceLocationId!.Value,
                r.LotNumber,
                r.ExpiryDate,
                r.OwnerPartnerId,
                OpenQty = (r.ReservedQty - r.ConsumedQty - r.ReleasedQty)
            })
            .ToListAsync();

        var activeVasReservations = await _db.VasMaterialLines.AsNoTracking()
            .Where(r => r.Status == VasMaterialLineStatusEnum.Reserved
                && itemIds.Contains(r.MaterialItemId)
                && r.SourceLocationId.HasValue
                && locationIds.Contains(r.SourceLocationId.Value))
            .Select(r => new
            {
                ItemId = r.MaterialItemId,
                LocationId = r.SourceLocationId!.Value,
                r.LotNumber,
                r.ExpiryDate,
                r.OwnerPartnerId,
                OpenQty = (r.ReservedQty - r.ConsumedQty - r.ReleasedQty)
            })
            .ToListAsync();

        foreach (var loc in locRows)
        {
            var voucherReserved = activeReservations
                .Where(r => r.ItemId == loc.ItemId
                    && r.LocationId == loc.LocationId
                    && r.LotNumber == loc.LotNumber
                    && r.ExpiryDate == loc.ExpiryDate
                    && r.OwnerPartnerId == loc.OwnerPartnerId)
                .Sum(r => r.OpenQty);
            var kittingReserved = activeKittingReservations
                .Where(r => r.ItemId == loc.ItemId
                    && r.LocationId == loc.LocationId
                    && r.LotNumber == loc.LotNumber
                    && r.ExpiryDate == loc.ExpiryDate
                    && r.OwnerPartnerId == loc.OwnerPartnerId)
                .Sum(r => r.OpenQty);
            var reserved = voucherReserved + kittingReserved;
            var vasReserved = activeVasReservations
                .Where(r => r.ItemId == loc.ItemId
                    && r.LocationId == loc.LocationId
                    && r.LotNumber == loc.LotNumber
                    && r.ExpiryDate == loc.ExpiryDate
                    && r.OwnerPartnerId == loc.OwnerPartnerId)
                .Sum(r => r.OpenQty);
            reserved += vasReserved;
            loc.ReservedQty = Math.Max(0, reserved);
            loc.UpdatedAt = VietnamTime.Now;
        }

        // P0-7: persist trong cùng transaction outer để tránh caller quên SaveChanges.
        // An toàn vì các caller hiện tại đều mở transaction trước khi gọi method này.
        await _db.SaveChangesAsync();
    }
}
