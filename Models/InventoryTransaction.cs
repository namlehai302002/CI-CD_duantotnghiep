using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("InventoryTransactions")]
public class InventoryTransaction : IOwnerScoped
{
    [Key]
    public long InventoryTransactionId { get; set; }

    public InventoryTransactionTypeEnum TransactionType { get; set; }

    [Required, MaxLength(100)]
    public string TransactionGroupKey { get; set; } = "";

    [Required, MaxLength(160)]
    public string IdempotencyKey { get; set; } = "";

    public int WarehouseId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public int ItemId { get; set; }

    public int LocationId { get; set; }

    [MaxLength(50)]
    public string? LotNumber { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ExpiryDate { get; set; }

    public InventoryHoldStatusEnum? HoldStatusBefore { get; set; }

    public InventoryHoldStatusEnum? HoldStatusAfter { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal QuantityDelta { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ReservedDelta { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AvailableDelta { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal QuantityBefore { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal QuantityAfter { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ReservedBefore { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ReservedAfter { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AvailableBefore { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AvailableAfter { get; set; }

    public long? VoucherId { get; set; }

    public long? VoucherDetailId { get; set; }

    public long? PickTaskId { get; set; }

    public long? MovementTaskId { get; set; }

    public long? StockReservationId { get; set; }

    public long? LicensePlateId { get; set; }

    public long? SerialNumberId { get; set; }

    [MaxLength(60)]
    public string? ReferenceType { get; set; }

    [MaxLength(80)]
    public string? ReferenceId { get; set; }

    [MaxLength(120)]
    public string? ReferenceCode { get; set; }

    [Required, MaxLength(100)]
    public string Actor { get; set; } = "system";

    public DateTime TransactionAt { get; set; } = VietnamTime.Now;

    public string MetadataJson { get; set; } = "{}";

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }

    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }

    [ForeignKey(nameof(VoucherId))]
    public Voucher? Voucher { get; set; }

    [ForeignKey(nameof(VoucherDetailId))]
    public VoucherDetail? VoucherDetail { get; set; }

    [ForeignKey(nameof(PickTaskId))]
    public PickTask? PickTask { get; set; }

    [ForeignKey(nameof(MovementTaskId))]
    public MovementTask? MovementTask { get; set; }

    [ForeignKey(nameof(StockReservationId))]
    public StockReservation? StockReservation { get; set; }

    [ForeignKey(nameof(LicensePlateId))]
    public LicensePlate? LicensePlate { get; set; }

    [ForeignKey(nameof(SerialNumberId))]
    public SerialNumber? SerialNumber { get; set; }
}

public sealed class InventoryTransactionContext
{
    public InventoryTransactionTypeEnum? TransactionType { get; init; }

    public bool ForceTransactionType { get; init; }

    [MaxLength(100)]
    public string? TransactionGroupKey { get; init; }

    [MaxLength(120)]
    public string? IdempotencyKeyPrefix { get; init; }

    public int? WarehouseId { get; init; }

    public int? OwnerPartnerId { get; init; }

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

    public string? Actor { get; init; }

    public string? MetadataJson { get; init; }
}

public static class InventoryTransactionSemanticRules
{
    private const decimal Tolerance = 0.0001m;

    public static InventoryTransactionTypeEnum ResolveType(
        InventoryTransactionTypeEnum? preferred,
        bool forcePreferred,
        decimal quantityDelta,
        decimal reservedDelta,
        decimal availableDelta,
        InventoryHoldStatusEnum? holdBefore,
        InventoryHoldStatusEnum? holdAfter)
    {
        if (preferred.HasValue && (forcePreferred || IsCompatible(preferred.Value, quantityDelta, reservedDelta, availableDelta, holdBefore, holdAfter)))
            return preferred.Value;

        if (preferred.HasValue)
        {
            var pairedType = ResolvePairedType(preferred.Value, quantityDelta);
            if (pairedType.HasValue && IsCompatible(pairedType.Value, quantityDelta, reservedDelta, availableDelta, holdBefore, holdAfter))
                return pairedType.Value;
        }

        return InferType(quantityDelta, reservedDelta, availableDelta, holdBefore, holdAfter);
    }

    public static void Validate(InventoryTransaction transaction)
    {
        if (string.IsNullOrWhiteSpace(transaction.IdempotencyKey))
            throw new BusinessRuleException("Inventory transaction idempotency key is required.", "INVENTORY_LEDGER_IDEMPOTENCY_REQUIRED", "InventoryTransaction");
        if (string.IsNullOrWhiteSpace(transaction.TransactionGroupKey))
            throw new BusinessRuleException("Inventory transaction group key is required.", "INVENTORY_LEDGER_GROUP_REQUIRED", "InventoryTransaction");

        EnsureClose(transaction.QuantityAfter - transaction.QuantityBefore, transaction.QuantityDelta, "QuantityDelta");
        EnsureClose(transaction.ReservedAfter - transaction.ReservedBefore, transaction.ReservedDelta, "ReservedDelta");
        EnsureClose(transaction.AvailableAfter - transaction.AvailableBefore, transaction.AvailableDelta, "AvailableDelta");
        EnsureClose(transaction.QuantityBefore - transaction.ReservedBefore, transaction.AvailableBefore, "AvailableBefore");
        EnsureClose(transaction.QuantityAfter - transaction.ReservedAfter, transaction.AvailableAfter, "AvailableAfter");

        if (!IsCompatible(
                transaction.TransactionType,
                transaction.QuantityDelta,
                transaction.ReservedDelta,
                transaction.AvailableDelta,
                transaction.HoldStatusBefore,
                transaction.HoldStatusAfter))
        {
            throw new BusinessRuleException(
                $"Inventory transaction type {transaction.TransactionType} is not compatible with the supplied deltas.",
                "INVENTORY_LEDGER_RULE_VIOLATION",
                "InventoryTransaction");
        }
    }

    public static bool IsCompatible(
        InventoryTransactionTypeEnum type,
        decimal quantityDelta,
        decimal reservedDelta,
        decimal availableDelta,
        InventoryHoldStatusEnum? holdBefore,
        InventoryHoldStatusEnum? holdAfter)
    {
        var holdChanged = holdBefore != holdAfter;
        var hasQuantity = !IsZero(quantityDelta);
        var hasReserved = !IsZero(reservedDelta);
        var hasAvailable = !IsZero(availableDelta);

        return type switch
        {
            InventoryTransactionTypeEnum.OpeningBalance => quantityDelta >= -Tolerance && reservedDelta >= -Tolerance,
            InventoryTransactionTypeEnum.Receive => quantityDelta > Tolerance && !hasReserved,
            InventoryTransactionTypeEnum.Putaway => quantityDelta > Tolerance && !hasReserved,
            InventoryTransactionTypeEnum.TransferIn => quantityDelta > Tolerance && !hasReserved,
            InventoryTransactionTypeEnum.KitProduce => quantityDelta > Tolerance && !hasReserved,
            InventoryTransactionTypeEnum.Move => hasQuantity && !hasReserved,
            InventoryTransactionTypeEnum.Pick => IsZero(quantityDelta) && reservedDelta > Tolerance && availableDelta < -Tolerance,
            InventoryTransactionTypeEnum.Pack => IsZero(quantityDelta) && hasReserved,
            InventoryTransactionTypeEnum.Ship => quantityDelta < -Tolerance,
            InventoryTransactionTypeEnum.TransferOut => quantityDelta < -Tolerance,
            InventoryTransactionTypeEnum.KitConsume => quantityDelta < -Tolerance,
            InventoryTransactionTypeEnum.VasConsume => quantityDelta < -Tolerance,
            InventoryTransactionTypeEnum.Adjust => hasQuantity || hasReserved || hasAvailable || holdChanged,
            InventoryTransactionTypeEnum.Cancel => hasQuantity || hasReserved || hasAvailable || holdChanged,
            InventoryTransactionTypeEnum.Reconcile => hasQuantity || hasReserved || hasAvailable,
            InventoryTransactionTypeEnum.Hold => IsZero(quantityDelta) && IsZero(reservedDelta) && holdChanged && holdAfter != InventoryHoldStatusEnum.Available,
            InventoryTransactionTypeEnum.ReleaseHold => IsZero(quantityDelta) && IsZero(reservedDelta) && holdChanged && holdAfter == InventoryHoldStatusEnum.Available,
            _ => false
        };
    }

    private static InventoryTransactionTypeEnum InferType(
        decimal quantityDelta,
        decimal reservedDelta,
        decimal availableDelta,
        InventoryHoldStatusEnum? holdBefore,
        InventoryHoldStatusEnum? holdAfter)
    {
        if (holdBefore != holdAfter && IsZero(quantityDelta) && IsZero(reservedDelta))
            return holdAfter == InventoryHoldStatusEnum.Available
                ? InventoryTransactionTypeEnum.ReleaseHold
                : InventoryTransactionTypeEnum.Hold;

        if (IsZero(quantityDelta) && reservedDelta > Tolerance && availableDelta < -Tolerance)
            return InventoryTransactionTypeEnum.Pick;

        if (reservedDelta < -Tolerance && IsZero(quantityDelta))
            return InventoryTransactionTypeEnum.Cancel;

        return InventoryTransactionTypeEnum.Adjust;
    }

    private static InventoryTransactionTypeEnum? ResolvePairedType(InventoryTransactionTypeEnum preferred, decimal quantityDelta)
        => preferred switch
        {
            InventoryTransactionTypeEnum.TransferOut when quantityDelta > Tolerance => InventoryTransactionTypeEnum.TransferIn,
            InventoryTransactionTypeEnum.TransferIn when quantityDelta < -Tolerance => InventoryTransactionTypeEnum.TransferOut,
            InventoryTransactionTypeEnum.KitConsume when quantityDelta > Tolerance => InventoryTransactionTypeEnum.KitProduce,
            InventoryTransactionTypeEnum.KitProduce when quantityDelta < -Tolerance => InventoryTransactionTypeEnum.KitConsume,
            _ => null
        };

    private static bool IsZero(decimal value)
        => Math.Abs(value) <= Tolerance;

    private static void EnsureClose(decimal actual, decimal expected, string field)
    {
        if (Math.Abs(actual - expected) > Tolerance)
        {
            throw new BusinessRuleException(
                $"Inventory transaction {field} does not match before/after balance.",
                "INVENTORY_LEDGER_BALANCE_MISMATCH",
                "InventoryTransaction");
        }
    }
}
