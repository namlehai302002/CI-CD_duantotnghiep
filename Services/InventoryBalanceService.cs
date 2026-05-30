using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public interface IInventoryBalanceService
{
    Task<Dictionary<int, decimal>> GetStockByItemAsync(int? warehouseId = null, IEnumerable<int>? itemIds = null, int? ownerPartnerId = null);
    Task<decimal> GetTotalStockAsync(int? warehouseId = null);
    void ApplyStockBalances(IEnumerable<Item> items, IReadOnlyDictionary<int, decimal> stockByItem);

    /// <summary>
    /// Sync Item.CurrentStock and TotalStockValue from SUM(ItemLocation.Quantity).
    /// ItemLocation is the fast inventory snapshot; LPN-managed movement keeps it
    /// synchronized from container events in the Epic 2 flow.
    /// </summary>
    Task SyncCurrentStockAsync(IEnumerable<int> itemIds);
}

public class InventoryBalanceService : IInventoryBalanceService
{
    private readonly AppDbContext _db;

    public InventoryBalanceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Dictionary<int, decimal>> GetStockByItemAsync(int? warehouseId = null, IEnumerable<int>? itemIds = null, int? ownerPartnerId = null)
    {
        var query = _db.ItemLocations
            .AsNoTracking()
            .Where(il => il.Quantity != 0);

        if (warehouseId.HasValue)
        {
            query = query.Where(il => il.Location != null
                && il.Location.Zone != null
                && il.Location.Zone.WarehouseId == warehouseId.Value);
        }

        if (itemIds != null)
        {
            var ids = itemIds.Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<int, decimal>();
            query = query.Where(il => ids.Contains(il.ItemId));
        }

        if (ownerPartnerId.HasValue)
        {
            query = query.Where(il => il.OwnerPartnerId == ownerPartnerId.Value);
        }

        return await query
            .GroupBy(il => il.ItemId)
            .Select(g => new { ItemId = g.Key, Qty = g.Sum(il => il.Quantity) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Qty);
    }

    public async Task<decimal> GetTotalStockAsync(int? warehouseId = null)
    {
        var stockByItem = await GetStockByItemAsync(warehouseId);
        return stockByItem.Values.Sum();
    }

    public void ApplyStockBalances(IEnumerable<Item> items, IReadOnlyDictionary<int, decimal> stockByItem)
    {
        foreach (var item in items)
        {
            item.CurrentStock = stockByItem.TryGetValue(item.ItemId, out var qty) ? qty : 0m;
            item.TotalStockValue = item.CurrentStock * item.UnitCost;
        }
    }

    public async Task SyncCurrentStockAsync(IEnumerable<int> itemIds)
    {
        var ids = itemIds.Distinct().ToList();
        if (ids.Count == 0) return;

        // Compute stock from ItemLocation snapshot.
        var stockByItem = await _db.ItemLocations
            .Where(il => ids.Contains(il.ItemId))
            .GroupBy(il => il.ItemId)
            .Select(g => new { ItemId = g.Key, Qty = g.Sum(il => il.Quantity) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Qty);

        var items = await _db.Items.Where(i => ids.Contains(i.ItemId)).ToListAsync();
        foreach (var item in items)
        {
            var computedStock = stockByItem.TryGetValue(item.ItemId, out var qty) ? qty : 0m;
            item.CurrentStock = computedStock;
            item.TotalStockValue = item.CurrentStock * item.UnitCost;
        }
    }
}
