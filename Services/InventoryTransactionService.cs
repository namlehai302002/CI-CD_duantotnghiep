using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public sealed class InventoryTransactionWriteRequest
{
    public InventoryTransactionTypeEnum TransactionType { get; init; }
    public string TransactionGroupKey { get; init; } = "";
    public string IdempotencyKey { get; init; } = "";
    public int WarehouseId { get; init; }
    public int? OwnerPartnerId { get; init; }
    public int ItemId { get; init; }
    public int LocationId { get; init; }
    public string? LotNumber { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public InventoryHoldStatusEnum? HoldStatusBefore { get; init; }
    public InventoryHoldStatusEnum? HoldStatusAfter { get; init; }
    public decimal QuantityBefore { get; init; }
    public decimal QuantityAfter { get; init; }
    public decimal ReservedBefore { get; init; }
    public decimal ReservedAfter { get; init; }
    public long? VoucherId { get; init; }
    public long? VoucherDetailId { get; init; }
    public long? PickTaskId { get; init; }
    public long? MovementTaskId { get; init; }
    public long? StockReservationId { get; init; }
    public long? LicensePlateId { get; init; }
    public long? SerialNumberId { get; init; }
    public string? ReferenceType { get; init; }
    public string? ReferenceId { get; init; }
    public string? ReferenceCode { get; init; }
    public string Actor { get; init; } = "system";
    public DateTime? TransactionAt { get; init; }
    public string? MetadataJson { get; init; }
}

public interface IInventoryTransactionService
{
    IDisposable BeginScope(InventoryTransactionContext context);
    Task<InventoryTransaction> RecordAsync(InventoryTransactionWriteRequest request, CancellationToken ct = default);
}

public class InventoryTransactionService : IInventoryTransactionService
{
    private readonly AppDbContext _db;

    public InventoryTransactionService(AppDbContext db)
    {
        _db = db;
    }

    public IDisposable BeginScope(InventoryTransactionContext context)
    {
        var previous = _db.CurrentInventoryTransactionContext;
        _db.CurrentInventoryTransactionContext = context;
        return new Scope(_db, previous);
    }

    public async Task<InventoryTransaction> RecordAsync(InventoryTransactionWriteRequest request, CancellationToken ct = default)
    {
        var existing = await _db.InventoryTransactions
            .FirstOrDefaultAsync(t => t.IdempotencyKey == request.IdempotencyKey, ct);
        if (existing != null)
            return existing;

        var local = _db.InventoryTransactions.Local.FirstOrDefault(t => t.IdempotencyKey == request.IdempotencyKey);
        if (local != null)
            return local;

        var quantityDelta = request.QuantityAfter - request.QuantityBefore;
        var reservedDelta = request.ReservedAfter - request.ReservedBefore;
        var availableBefore = request.QuantityBefore - request.ReservedBefore;
        var availableAfter = request.QuantityAfter - request.ReservedAfter;
        var transaction = new InventoryTransaction
        {
            TransactionType = request.TransactionType,
            TransactionGroupKey = Normalize(request.TransactionGroupKey, 100),
            IdempotencyKey = Normalize(request.IdempotencyKey, 160),
            WarehouseId = request.WarehouseId,
            OwnerPartnerId = request.OwnerPartnerId,
            ItemId = request.ItemId,
            LocationId = request.LocationId,
            LotNumber = NormalizeNullable(request.LotNumber, 50),
            ExpiryDate = request.ExpiryDate,
            HoldStatusBefore = request.HoldStatusBefore,
            HoldStatusAfter = request.HoldStatusAfter,
            QuantityDelta = quantityDelta,
            ReservedDelta = reservedDelta,
            AvailableDelta = availableAfter - availableBefore,
            QuantityBefore = request.QuantityBefore,
            QuantityAfter = request.QuantityAfter,
            ReservedBefore = request.ReservedBefore,
            ReservedAfter = request.ReservedAfter,
            AvailableBefore = availableBefore,
            AvailableAfter = availableAfter,
            VoucherId = request.VoucherId,
            VoucherDetailId = request.VoucherDetailId,
            PickTaskId = request.PickTaskId,
            MovementTaskId = request.MovementTaskId,
            StockReservationId = request.StockReservationId,
            LicensePlateId = request.LicensePlateId,
            SerialNumberId = request.SerialNumberId,
            ReferenceType = NormalizeNullable(request.ReferenceType, 60),
            ReferenceId = NormalizeNullable(request.ReferenceId, 80),
            ReferenceCode = NormalizeNullable(request.ReferenceCode, 120),
            Actor = Normalize(request.Actor, 100),
            TransactionAt = request.TransactionAt ?? VietnamTime.Now,
            MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson
        };

        InventoryTransactionSemanticRules.Validate(transaction);
        _db.InventoryTransactions.Add(transaction);
        return transaction;
    }

    private static string Normalize(string value, int maxLength)
    {
        var trimmed = string.IsNullOrWhiteSpace(value) ? "system" : value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string? NormalizeNullable(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private sealed class Scope : IDisposable
    {
        private readonly AppDbContext _db;
        private readonly InventoryTransactionContext? _previous;
        private bool _disposed;

        public Scope(AppDbContext db, InventoryTransactionContext? previous)
        {
            _db = db;
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _db.CurrentInventoryTransactionContext = _previous;
            _disposed = true;
        }
    }
}
