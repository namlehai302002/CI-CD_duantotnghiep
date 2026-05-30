using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using WMS.Models;
using WMS.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WMS.Data;

public class AppDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // ═══════════════════════════════════════════════════════════════
    // Bảng cần theo dõi trong Audit Trail
    // ═══════════════════════════════════════════════════════════════
    private static readonly HashSet<string> _trackedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "Item", "Voucher", "VoucherDetail", "Warehouse", "Zone", "Location", "WarehouseSortationConfig", "WarehouseOrderStreamingConfig",
        "ItemLocation", "Partner", "AppUserOwnerScope", "ThreePlBillingRate", "ThreePlBillingRun", "ThreePlBillingCharge", "ThreePlContract", "ThreePlContractRate", "ThreePlInvoice", "ThreePlInvoiceLine", "ThreePlDispute", "MheSystem", "MheCommand", "MheMissionEvent", "MheAdapterProfile", "MheTelemetryEvent", "WcsSimulatorRun", "AutomationOverride", "EdiMessage", "WebhookSubscription", "WebhookDelivery", "EnterpriseConnector", "EnterpriseConnectorDelivery", "CarrierConnector", "CarrierShipment", "CarrierShipmentEvent", "DockAppointment", "YardVisitEvidence", "ItemCategory", "PackagingUnit", "UnitOfMeasure",
        "UnitConversion", "AppUser", "AppRole", "StockReservation", "Wave", "WaveLine", "PickTask", "PickTaskAllocation", "PickTaskScanLog", "PickTaskSerialAssignment",
        "LoginAuditLog", "MfaLoginChallenge", "LoginHelpRequest", "OperationExceptionCase", "LicensePlate", "LicensePlateDetail", "SerialNumber", "SerialReservation", "SerialInventoryOperation", "ShippingHandoverLog", "OutboundPackage", "CatchWeightEntry", "ShipmentLoad", "ShipmentLoadVoucher", "ShipmentLoadPackage",
        "OptimizationRun", "OptimizationRecommendationLine", "WavelessReleaseQueue", "PickPathPlan", "PickPathPlanStop", "ToteClusterPlan", "ToteClusterAssignment",
        // P1.2: Cluster picking
        "PickCart", "PickTote",
        // P1.3: Zone picking
        "UserZoneAssignment",
        // P2.1: Cross-dock
        "CrossDockTask",
        // P2-03: Yard management MVP
        "YardSpot", "Trailer", "YardVisit",
        // P2-03B: Yard billing detention/demurrage
        "YardBillingRate", "YardBillingCharge",
        // P2.4: Movement task execution
        "MovementTask",
        // P3-01: VAS kitting
        "KittingWorkOrder", "KittingWorkOrderLine",
        // P3-02: Customer-specific labeling
        "PartnerLabelTemplate", "PartnerItemLabelRule", "LabelPrintJob", "LabelPrintJobLine",
        // P3-03: VAS light assembly/co-packing
        "VasWorkOrder", "VasOperation", "VasMaterialLine",
        // P2.2: Cycle count
        "CycleCountProgram", "CycleCountSchedule",
        // P2.3: Recall
        "RecallCase", "RecallLine",
        // P2.4: Labor
        "LaborActivityStandard", "LaborStandard", "LaborActivity", "LaborExceptionReview",
        // P3.1: Velocity
        "ItemVelocityClassification",
        // P3.2: SLA
        "SlaMetric",
        // P3.3: Capacity
        "CapacityScenario",
        // Enterprise audit: outbox + idempotency + AI OCR
        "IntegrationOutbox", "IntegrationIdempotencyKey", "AiOcrLog", "AiOcrAdjustment",
        "InventorySnapshotOutbox", "InventoryReconciliationRun", "InventoryReconciliationIssue",
        "SemanticMetricDefinition", "SemanticMetricSnapshot", "EnterprisePredictiveAlert", "AuditAnalyticsFinding",
        "AiAssistantSession", "AiAssistantMessage", "AiAssistantCitation", "WarehouseWorkflowProfile", "RequestTelemetryLog", "SreMetricSnapshot"
        , "ReplenishmentAutomationRun", "ReplenishmentAutomationLine"
    };

    // Thuộc tính nên bỏ qua khi so sánh (navigation properties, computed, etc.)
    private static readonly HashSet<string> _ignoredProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "Voucher", "Item", "Location", "DestLocation", "TransactionUom", "PackagingUnit",
        "Category", "BaseUom", "ParentCategory", "ChildCategories", "Items", "Warehouse",
        "DestWarehouse", "Partner", "OwnerPartner", "OwnerPartnerScopes", "UserOwnerScopes", "User", "Details", "Zones", "Locations", "ItemLocations",
        "DefaultLocation", "Zone", "Role", "AiOcrLog", "FromUom", "ToUom",
        "ParentItem", "ChildItem", "Uom", "BaseUom", "PasswordHash",
        "Trailer", "CurrentSpot", "SourceLocation", "DestinationLocation", "SourceItemLocation", "PreviousDefaultLocation",
        "ReplenishmentAutomationRun", "ReplenishmentAutomationLine",
        "ParentPickTask", "ChildPickTasks", "SortationStageLocation", "SortationDestinationLocation",
        "StagingLocation", "SortationLocation",
        "BillingRate", "YardVisit", "EvidenceItems", "FinishedItem", "FinishedLocation", "ComponentItem", "KittingWorkOrder",
        "PartnerLabelTemplate", "LabelPrintJob", "OutboundPackage", "CatchWeightEntries", "CatchWeightUom", "WeightUom", "ShipmentLoad", "ShipmentLoadVouchers", "ShipmentLoadPackages", "ShippingHandoverLog", "CarrierShipment",
        "VasWorkOrder", "PrimaryItem", "MaterialItem", "Operations", "MaterialLines",
        "CurrentLocation", "ParentLpn", "ChildLpns", "Details", "LicensePlate",
        "Run", "BillingRun", "BillingRate", "MheSystem", "MissionEvents", "PickTask", "MovementTask", "Wave",
        "Contract", "Rates", "Invoice", "Invoices", "InvoiceLine", "Lines", "Disputes", "ExceptionReviews",
        "OptimizationRun", "Stops", "PickPathPlan", "ToteClusterPlan", "Assignments", "AdapterProfile", "Subscription", "Deliveries", "Connector",
        "CarrierConnector", "CarrierShipments", "CarrierShipment", "CarrierShipmentEvents", "Events",
        "MetricDefinition", "Snapshots", "Session", "Messages", "Message", "Citations"
    };

    /// <summary>Set to true to bypass audit trail (used during backup restore)</summary>
    public bool SkipAudit { get; set; }

    public InventoryTransactionContext? CurrentInventoryTransactionContext { get; set; }

    public override int SaveChanges()
        => SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Fast path: skip audit entirely (used during restore operations)
        if (SkipAudit)
            return await base.SaveChangesAsync(cancellationToken);

        // Ensure audit logs and business data commit atomically
        var startedTx = false;
        IDbContextTransaction? createdTx = null;
        if (Database.CurrentTransaction == null)
        {
            createdTx = await Database.BeginTransactionAsync(cancellationToken);
            startedTx = true;
        }
        try
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditLog>();
            var httpContext = _httpContextAccessor?.HttpContext;
            var userName = httpContext?.User?.Identity?.Name ?? "system";
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
            var inventorySnapshots = CaptureItemLocationChangeSnapshots();

            var entries = ChangeTracker.Entries()
                .Where(e => _trackedTables.Contains(e.Entity.GetType().Name)
                         && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .ToList();

            // Track which entries are INSERTs (we'll capture their values AFTER save when keys are generated)
            var insertEntries = entries.Where(e => e.State == EntityState.Added).ToList();

            // ──────── PRE-SAVE: Capture DELETE & UPDATE old values ────────
            foreach (var entry in entries)
            {
                var tableName = entry.Entity.GetType().Name;

                if (entry.State == EntityState.Deleted)
                {
                    var oldValues = GetPropertyValues(entry, EntityState.Deleted);
                    var recordId = GetPrimaryKeyValue(entry);

                    auditEntries.Add(new AuditLog
                    {
                        TableName = tableName,
                        RecordId = recordId,
                        ActionType = "DELETE",
                        OldValue = SerializeDict(oldValues),
                        NewValue = null,
                        ChangedBy = userName,
                        ChangedAt = VietnamTime.Now,
                        IpAddress = ipAddress,
                        AppModule = "EF_AutoAudit"
                    });
                }
                else if (entry.State == EntityState.Modified)
                {
                    var oldValues = new Dictionary<string, object?>();
                    var newValues = new Dictionary<string, object?>();
                    var changedColumns = new List<string>();

                    foreach (var prop in entry.Properties)
                    {
                        if (_ignoredProperties.Contains(prop.Metadata.Name)) continue;
                        if (prop.Metadata.IsPrimaryKey()) continue;

                        if (prop.IsModified && !Equals(prop.OriginalValue, prop.CurrentValue))
                        {
                            oldValues[prop.Metadata.Name] = prop.OriginalValue;
                            newValues[prop.Metadata.Name] = prop.CurrentValue;
                            changedColumns.Add(prop.Metadata.Name);
                        }
                    }

                    if (changedColumns.Count > 0)
                    {
                        var recordId = GetPrimaryKeyValue(entry);

                        auditEntries.Add(new AuditLog
                        {
                            TableName = tableName,
                            RecordId = recordId,
                            ActionType = "UPDATE",
                            ColumnChanged = string.Join(", ", changedColumns),
                            OldValue = SerializeDict(oldValues),
                            NewValue = SerializeDict(newValues),
                            ChangedBy = userName,
                            ChangedAt = VietnamTime.Now,
                            IpAddress = ipAddress,
                            AppModule = "EF_AutoAudit"
                        });
                    }
                }
            }

            // ──────── SAVE actual changes to database ────────
            var result = await base.SaveChangesAsync(cancellationToken);
            var inventoryTransactions = await BuildInventoryTransactionsAsync(inventorySnapshots, userName, cancellationToken);

            // ──────── POST-SAVE: Capture INSERT new values (now keys are generated) ────────
            foreach (var entry in insertEntries)
            {
                var tableName = entry.Entity.GetType().Name;
                var recordId = GetPrimaryKeyValue(entry); // Now has the DB-generated key
                var newValues = GetPropertyValues(entry, EntityState.Added);

                auditEntries.Add(new AuditLog
                {
                    TableName = tableName,
                    RecordId = recordId,
                    ActionType = "INSERT",
                    OldValue = null,
                    NewValue = SerializeDict(newValues),
                    ChangedBy = userName,
                    ChangedAt = VietnamTime.Now,
                    IpAddress = ipAddress,
                    AppModule = "EF_AutoAudit"
                });
            }

            // ──────── Persist audit logs ────────
            if (inventoryTransactions.Count > 0)
                InventoryTransactions.AddRange(inventoryTransactions);

            if (auditEntries.Count > 0)
                AuditLogs.AddRange(auditEntries);

            if (inventoryTransactions.Count > 0 || auditEntries.Count > 0)
                await base.SaveChangesAsync(cancellationToken);

            if (startedTx && createdTx != null)
                await createdTx.CommitAsync(cancellationToken);

            return result;
        }
        catch
        {
            if (startedTx && createdTx != null)
                await createdTx.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            if (createdTx != null)
                await createdTx.DisposeAsync();
        }
    }

    private List<ItemLocationChangeSnapshot> CaptureItemLocationChangeSnapshots()
    {
        var snapshots = new List<ItemLocationChangeSnapshot>();
        foreach (var entry in ChangeTracker.Entries<ItemLocation>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            if (entry.State == EntityState.Modified && !HasLedgerRelevantItemLocationChange(entry))
                continue;

            snapshots.Add(ItemLocationChangeSnapshot.FromEntry(entry));
        }

        return snapshots;
    }

    private static bool HasLedgerRelevantItemLocationChange(EntityEntry<ItemLocation> entry)
        => IsModified(entry, nameof(ItemLocation.Quantity))
            || IsModified(entry, nameof(ItemLocation.ReservedQty))
            || IsModified(entry, nameof(ItemLocation.HoldStatus))
            || IsModified(entry, nameof(ItemLocation.LocationId))
            || IsModified(entry, nameof(ItemLocation.ItemId));

    private static bool IsModified(EntityEntry entry, string propertyName)
        => entry.Property(propertyName).IsModified
            && !Equals(entry.Property(propertyName).OriginalValue, entry.Property(propertyName).CurrentValue);

    private async Task<List<InventoryTransaction>> BuildInventoryTransactionsAsync(
        IReadOnlyCollection<ItemLocationChangeSnapshot> snapshots,
        string userName,
        CancellationToken cancellationToken)
    {
        if (snapshots.Count == 0)
            return new List<InventoryTransaction>();

        var context = CurrentInventoryTransactionContext;
        var locationIds = snapshots
            .Select(s => s.EffectiveLocationId)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        var warehouseByLocation = await ResolveWarehouseByLocationAsync(locationIds, cancellationToken);
        var candidates = new List<InventoryTransaction>();

        foreach (var snapshot in snapshots)
        {
            var transaction = CreateInventoryTransaction(snapshot, context, warehouseByLocation, userName);
            if (transaction == null)
                continue;

            InventoryTransactionSemanticRules.Validate(transaction);
            candidates.Add(transaction);
        }

        if (candidates.Count == 0)
            return candidates;

        var keys = candidates.Select(t => t.IdempotencyKey).Distinct().ToList();
        var existingKeys = await InventoryTransactions
            .AsNoTracking()
            .Where(t => keys.Contains(t.IdempotencyKey))
            .Select(t => t.IdempotencyKey)
            .ToListAsync(cancellationToken);
        var existing = new HashSet<string>(existingKeys, StringComparer.OrdinalIgnoreCase);
        foreach (var localKey in InventoryTransactions.Local.Select(t => t.IdempotencyKey))
            existing.Add(localKey);

        return candidates
            .Where(t => !existing.Contains(t.IdempotencyKey))
            .ToList();
    }

    private InventoryTransaction? CreateInventoryTransaction(
        ItemLocationChangeSnapshot snapshot,
        InventoryTransactionContext? context,
        IReadOnlyDictionary<int, int> warehouseByLocation,
        string userName)
    {
        var quantityDelta = snapshot.QuantityAfter - snapshot.QuantityBefore;
        var reservedDelta = snapshot.ReservedAfter - snapshot.ReservedBefore;
        var availableBefore = snapshot.QuantityBefore - snapshot.ReservedBefore;
        var availableAfter = snapshot.QuantityAfter - snapshot.ReservedAfter;
        var availableDelta = availableAfter - availableBefore;
        var holdChanged = snapshot.HoldStatusBefore != snapshot.HoldStatusAfter;

        if (IsEffectivelyZero(quantityDelta) && IsEffectivelyZero(reservedDelta) && IsEffectivelyZero(availableDelta) && !holdChanged)
            return null;

        var transactionType = InventoryTransactionSemanticRules.ResolveType(
            context?.TransactionType,
            context?.ForceTransactionType ?? false,
            quantityDelta,
            reservedDelta,
            availableDelta,
            snapshot.HoldStatusBefore,
            snapshot.HoldStatusAfter);

        var warehouseId = context?.WarehouseId
            ?? (warehouseByLocation.TryGetValue(snapshot.EffectiveLocationId, out var mappedWarehouseId) ? mappedWarehouseId : 0);
        if (warehouseId <= 0)
        {
            throw new BusinessRuleException(
                "Sổ giao dịch tồn kho không xác định được kho cho thay đổi tồn theo vị trí.",
                "INVENTORY_LEDGER_WAREHOUSE_REQUIRED",
                "InventoryTransaction");
        }

        var groupKey = Truncate(NonBlank(context?.TransactionGroupKey, BuildDefaultLedgerGroupKey(transactionType, snapshot)), 100);
        var idempotencyPrefix = Truncate(NonBlank(context?.IdempotencyKeyPrefix, groupKey), 120);
        var idempotencyKey = BuildLedgerIdempotencyKey(idempotencyPrefix, snapshot, transactionType);
        var actor = Truncate(NonBlank(context?.Actor, userName), 100);

        return new InventoryTransaction
        {
            TransactionType = transactionType,
            TransactionGroupKey = groupKey,
            IdempotencyKey = idempotencyKey,
            WarehouseId = warehouseId,
            OwnerPartnerId = context?.OwnerPartnerId ?? snapshot.Entry.Entity.OwnerPartnerId,
            ItemId = snapshot.EffectiveItemId,
            LocationId = snapshot.EffectiveLocationId,
            LotNumber = TruncateNullable(snapshot.EffectiveLotNumber, 50),
            ExpiryDate = snapshot.EffectiveExpiryDate,
            HoldStatusBefore = snapshot.HoldStatusBefore,
            HoldStatusAfter = snapshot.HoldStatusAfter,
            QuantityDelta = quantityDelta,
            ReservedDelta = reservedDelta,
            AvailableDelta = availableDelta,
            QuantityBefore = snapshot.QuantityBefore,
            QuantityAfter = snapshot.QuantityAfter,
            ReservedBefore = snapshot.ReservedBefore,
            ReservedAfter = snapshot.ReservedAfter,
            AvailableBefore = availableBefore,
            AvailableAfter = availableAfter,
            VoucherId = context?.VoucherId,
            VoucherDetailId = context?.VoucherDetailId,
            PickTaskId = context?.PickTaskId,
            MovementTaskId = context?.MovementTaskId,
            StockReservationId = context?.StockReservationId,
            LicensePlateId = context?.LicensePlateId,
            SerialNumberId = context?.SerialNumberId,
            ReferenceType = TruncateNullable(context?.ReferenceType, 60),
            ReferenceId = TruncateNullable(context?.ReferenceId, 80),
            ReferenceCode = TruncateNullable(context?.ReferenceCode, 120),
            Actor = actor,
            TransactionAt = VietnamTime.Now,
            MetadataJson = string.IsNullOrWhiteSpace(context?.MetadataJson) ? "{}" : context.MetadataJson
        };
    }

    private async Task<Dictionary<int, int>> ResolveWarehouseByLocationAsync(IReadOnlyCollection<int> locationIds, CancellationToken cancellationToken)
    {
        if (locationIds.Count == 0)
            return new Dictionary<int, int>();

        var locations = await Locations
            .AsNoTracking()
            .Where(l => locationIds.Contains(l.LocationId))
            .Select(l => new { l.LocationId, l.ZoneId })
            .ToListAsync(cancellationToken);
        var zoneIds = locations.Select(l => l.ZoneId).Distinct().ToList();
        var warehouseByZone = await Zones
            .AsNoTracking()
            .Where(z => zoneIds.Contains(z.ZoneId))
            .Select(z => new { z.ZoneId, z.WarehouseId })
            .ToDictionaryAsync(z => z.ZoneId, z => z.WarehouseId, cancellationToken);

        return locations
            .Where(l => warehouseByZone.ContainsKey(l.ZoneId))
            .ToDictionary(l => l.LocationId, l => warehouseByZone[l.ZoneId]);
    }

    private static string BuildDefaultLedgerGroupKey(InventoryTransactionTypeEnum type, ItemLocationChangeSnapshot snapshot)
        => $"auto:{type}:{snapshot.State}:{snapshot.EffectiveItemId}:{snapshot.EffectiveLocationId}";

    private static string BuildLedgerIdempotencyKey(string prefix, ItemLocationChangeSnapshot snapshot, InventoryTransactionTypeEnum type)
    {
        var raw = string.Join("|",
            prefix,
            type,
            snapshot.State,
            snapshot.EffectiveItemId,
            snapshot.EffectiveLocationId,
            snapshot.EffectiveLotNumber ?? "",
            snapshot.EffectiveExpiryDate?.ToString("yyyy-MM-dd") ?? "",
            snapshot.HoldStatusBefore?.ToString() ?? "",
            snapshot.HoldStatusAfter?.ToString() ?? "",
            snapshot.QuantityBefore.ToString("0.####"),
            snapshot.QuantityAfter.ToString("0.####"),
            snapshot.ReservedBefore.ToString("0.####"),
            snapshot.ReservedAfter.ToString("0.####"),
            snapshot.Entry.Entity.ItemLocationId);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..24];
        return Truncate($"{prefix}:{hash}", 160);
    }

    private static bool IsEffectivelyZero(decimal value)
        => Math.Abs(value) <= 0.0001m;

    private static string NonBlank(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

    private static string? TruncateNullable(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string GetPrimaryKeyValue(EntityEntry entry)
    {
        var keyProps = entry.Properties.Where(p => p.Metadata.IsPrimaryKey()).ToList();
        if (keyProps.Count == 1)
            return keyProps[0].CurrentValue?.ToString() ?? "0";
        return string.Join("-", keyProps.Select(p => p.CurrentValue?.ToString() ?? "0"));
    }

    private Dictionary<string, object?> GetPropertyValues(EntityEntry entry, EntityState forState)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in entry.Properties)
        {
            if (_ignoredProperties.Contains(prop.Metadata.Name)) continue;
            var value = forState == EntityState.Deleted ? prop.OriginalValue : prop.CurrentValue;
            dict[prop.Metadata.Name] = value;
        }
        return dict;
    }

    private static string? SerializeDict(Dictionary<string, object?> dict)
    {
        if (dict.Count == 0) return null;
        try
        {
            return JsonSerializer.Serialize(dict, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
        catch
        {
            return JsonSerializer.Serialize(dict.ToDictionary(k => k.Key, k => k.Value?.ToString()));
        }
    }

    private sealed class ItemLocationChangeSnapshot
    {
        private ItemLocationChangeSnapshot(
            EntityEntry<ItemLocation> entry,
            EntityState state,
            int itemIdBefore,
            int itemIdAfter,
            int locationIdBefore,
            int locationIdAfter,
            string? lotNumberBefore,
            string? lotNumberAfter,
            DateTime? expiryDateBefore,
            DateTime? expiryDateAfter,
            InventoryHoldStatusEnum? holdStatusBefore,
            InventoryHoldStatusEnum? holdStatusAfter,
            decimal quantityBefore,
            decimal quantityAfter,
            decimal reservedBefore,
            decimal reservedAfter)
        {
            Entry = entry;
            State = state;
            ItemIdBefore = itemIdBefore;
            ItemIdAfter = itemIdAfter;
            LocationIdBefore = locationIdBefore;
            LocationIdAfter = locationIdAfter;
            LotNumberBefore = lotNumberBefore;
            LotNumberAfter = lotNumberAfter;
            ExpiryDateBefore = expiryDateBefore;
            ExpiryDateAfter = expiryDateAfter;
            HoldStatusBefore = holdStatusBefore;
            HoldStatusAfter = holdStatusAfter;
            QuantityBefore = quantityBefore;
            QuantityAfter = quantityAfter;
            ReservedBefore = reservedBefore;
            ReservedAfter = reservedAfter;
        }

        public EntityEntry<ItemLocation> Entry { get; }
        public EntityState State { get; }
        public int ItemIdBefore { get; }
        public int ItemIdAfter { get; }
        public int LocationIdBefore { get; }
        public int LocationIdAfter { get; }
        public string? LotNumberBefore { get; }
        public string? LotNumberAfter { get; }
        public DateTime? ExpiryDateBefore { get; }
        public DateTime? ExpiryDateAfter { get; }
        public InventoryHoldStatusEnum? HoldStatusBefore { get; }
        public InventoryHoldStatusEnum? HoldStatusAfter { get; }
        public decimal QuantityBefore { get; }
        public decimal QuantityAfter { get; }
        public decimal ReservedBefore { get; }
        public decimal ReservedAfter { get; }

        public int EffectiveItemId => ItemIdAfter > 0 ? ItemIdAfter : ItemIdBefore;
        public int EffectiveLocationId => LocationIdAfter > 0 ? LocationIdAfter : LocationIdBefore;
        public string? EffectiveLotNumber => LotNumberAfter ?? LotNumberBefore;
        public DateTime? EffectiveExpiryDate => ExpiryDateAfter ?? ExpiryDateBefore;

        public static ItemLocationChangeSnapshot FromEntry(EntityEntry<ItemLocation> entry)
        {
            return entry.State switch
            {
                EntityState.Added => new ItemLocationChangeSnapshot(
                    entry,
                    entry.State,
                    0,
                    CurrentInt(entry, nameof(ItemLocation.ItemId)),
                    0,
                    CurrentInt(entry, nameof(ItemLocation.LocationId)),
                    null,
                    CurrentNullableString(entry, nameof(ItemLocation.LotNumber)),
                    null,
                    CurrentNullableDate(entry, nameof(ItemLocation.ExpiryDate)),
                    CurrentHoldStatus(entry, nameof(ItemLocation.HoldStatus)),
                    CurrentHoldStatus(entry, nameof(ItemLocation.HoldStatus)),
                    0,
                    CurrentDecimal(entry, nameof(ItemLocation.Quantity)),
                    0,
                    CurrentDecimal(entry, nameof(ItemLocation.ReservedQty))),
                EntityState.Deleted => new ItemLocationChangeSnapshot(
                    entry,
                    entry.State,
                    OriginalInt(entry, nameof(ItemLocation.ItemId)),
                    0,
                    OriginalInt(entry, nameof(ItemLocation.LocationId)),
                    0,
                    OriginalNullableString(entry, nameof(ItemLocation.LotNumber)),
                    null,
                    OriginalNullableDate(entry, nameof(ItemLocation.ExpiryDate)),
                    null,
                    OriginalHoldStatus(entry, nameof(ItemLocation.HoldStatus)),
                    OriginalHoldStatus(entry, nameof(ItemLocation.HoldStatus)),
                    OriginalDecimal(entry, nameof(ItemLocation.Quantity)),
                    0,
                    OriginalDecimal(entry, nameof(ItemLocation.ReservedQty)),
                    0),
                _ => new ItemLocationChangeSnapshot(
                    entry,
                    entry.State,
                    OriginalInt(entry, nameof(ItemLocation.ItemId)),
                    CurrentInt(entry, nameof(ItemLocation.ItemId)),
                    OriginalInt(entry, nameof(ItemLocation.LocationId)),
                    CurrentInt(entry, nameof(ItemLocation.LocationId)),
                    OriginalNullableString(entry, nameof(ItemLocation.LotNumber)),
                    CurrentNullableString(entry, nameof(ItemLocation.LotNumber)),
                    OriginalNullableDate(entry, nameof(ItemLocation.ExpiryDate)),
                    CurrentNullableDate(entry, nameof(ItemLocation.ExpiryDate)),
                    OriginalHoldStatus(entry, nameof(ItemLocation.HoldStatus)),
                    CurrentHoldStatus(entry, nameof(ItemLocation.HoldStatus)),
                    OriginalDecimal(entry, nameof(ItemLocation.Quantity)),
                    CurrentDecimal(entry, nameof(ItemLocation.Quantity)),
                    OriginalDecimal(entry, nameof(ItemLocation.ReservedQty)),
                    CurrentDecimal(entry, nameof(ItemLocation.ReservedQty)))
            };
        }

        private static int CurrentInt(EntityEntry entry, string propertyName)
            => Convert.ToInt32(entry.Property(propertyName).CurrentValue);

        private static int OriginalInt(EntityEntry entry, string propertyName)
            => Convert.ToInt32(entry.Property(propertyName).OriginalValue);

        private static decimal CurrentDecimal(EntityEntry entry, string propertyName)
            => Convert.ToDecimal(entry.Property(propertyName).CurrentValue);

        private static decimal OriginalDecimal(EntityEntry entry, string propertyName)
            => Convert.ToDecimal(entry.Property(propertyName).OriginalValue);

        private static string? CurrentNullableString(EntityEntry entry, string propertyName)
            => entry.Property(propertyName).CurrentValue?.ToString();

        private static string? OriginalNullableString(EntityEntry entry, string propertyName)
            => entry.Property(propertyName).OriginalValue?.ToString();

        private static DateTime? CurrentNullableDate(EntityEntry entry, string propertyName)
            => entry.Property(propertyName).CurrentValue is DateTime value ? value : null;

        private static DateTime? OriginalNullableDate(EntityEntry entry, string propertyName)
            => entry.Property(propertyName).OriginalValue is DateTime value ? value : null;

        private static InventoryHoldStatusEnum? CurrentHoldStatus(EntityEntry entry, string propertyName)
            => entry.Property(propertyName).CurrentValue is InventoryHoldStatusEnum value ? value : null;

        private static InventoryHoldStatusEnum? OriginalHoldStatus(EntityEntry entry, string propertyName)
            => entry.Property(propertyName).OriginalValue is InventoryHoldStatusEnum value ? value : null;
    }

    // Lookup & Reference
    public DbSet<ItemCategory> ItemCategories { get; set; }
    public DbSet<UnitOfMeasure> UnitsOfMeasure { get; set; }
    public DbSet<UnitConversion> UnitConversions { get; set; }
    public DbSet<PackagingUnit> PackagingUnits { get; set; }

    // Item Master
    public DbSet<Item> Items { get; set; }
    public DbSet<BillOfMaterial> BillOfMaterials { get; set; }

    // Warehouse Topology
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<Zone> Zones { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<ItemLocation> ItemLocations { get; set; }
    public DbSet<WarehouseSortationConfig> WarehouseSortationConfigs { get; set; }
    public DbSet<WarehouseOrderStreamingConfig> WarehouseOrderStreamingConfigs { get; set; }

    // Partners
    public DbSet<Partner> Partners { get; set; }
    public DbSet<AppUserOwnerScope> AppUserOwnerScopes { get; set; }

    // Vouchers
    public DbSet<Voucher> Vouchers { get; set; }
    public DbSet<VoucherDetail> VoucherDetails { get; set; }

    // Users & Roles
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<AppRole> AppRoles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

    // Audit & AI
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<LoginAuditLog> LoginAuditLogs { get; set; }
    public DbSet<MfaLoginChallenge> MfaLoginChallenges { get; set; }
    public DbSet<LoginHelpRequest> LoginHelpRequests { get; set; }

    // P1.2: Integration reliability
    public DbSet<IntegrationOutbox> IntegrationOutbox { get; set; }
    public DbSet<IntegrationIdempotencyKey> IntegrationIdempotencyKeys { get; set; }
    public DbSet<SemanticMetricDefinition> SemanticMetricDefinitions { get; set; }
    public DbSet<SemanticMetricSnapshot> SemanticMetricSnapshots { get; set; }
    public DbSet<EnterprisePredictiveAlert> EnterprisePredictiveAlerts { get; set; }
    public DbSet<AuditAnalyticsFinding> AuditAnalyticsFindings { get; set; }
    public DbSet<AiAssistantSession> AiAssistantSessions { get; set; }
    public DbSet<AiAssistantMessage> AiAssistantMessages { get; set; }
    public DbSet<AiAssistantCitation> AiAssistantCitations { get; set; }
    public DbSet<WarehouseWorkflowProfile> WarehouseWorkflowProfiles { get; set; }
    public DbSet<RequestTelemetryLog> RequestTelemetryLogs { get; set; }
    public DbSet<SreMetricSnapshot> SreMetricSnapshots { get; set; }

    // P1.3: Dock scheduling optimizer
    public DbSet<DockDoorCapacity> DockDoorCapacities { get; set; }

    // P2.1: Cross-dock execution
    public DbSet<CrossDockTask> CrossDockTasks { get; set; }

    // P2.2: Cycle count program
    public DbSet<CycleCountProgram> CycleCountPrograms { get; set; }
    public DbSet<CycleCountSchedule> CycleCountSchedules { get; set; }

    // P2.3: Recall management
    public DbSet<RecallCase> RecallCases { get; set; }
    public DbSet<RecallLine> RecallLines { get; set; }

    // P2.4: Labor management
    public DbSet<AiOcrLog> AiOcrLogs { get; set; }
    public DbSet<AiOcrAdjustment> AiOcrAdjustments { get; set; }

    // Stock
    public DbSet<StockSnapshot> StockSnapshots { get; set; }
    public DbSet<StockAlert> StockAlerts { get; set; }
    public DbSet<StockCountSheet> StockCountSheets { get; set; }
    public DbSet<StockCountLine> StockCountLines { get; set; }
    public DbSet<WarehousePeriodLock> WarehousePeriodLocks { get; set; }
    public DbSet<StockReservation> StockReservations { get; set; }
    public DbSet<Wave> Waves { get; set; }
    public DbSet<WaveLine> WaveLines { get; set; }
    public DbSet<PickTask> PickTasks { get; set; }
    public DbSet<PickTaskAllocation> PickTaskAllocations { get; set; }
    public DbSet<PickTaskScanLog> PickTaskScanLogs { get; set; }
    public DbSet<PickTaskSerialAssignment> PickTaskSerialAssignments { get; set; }
    public DbSet<LicensePlate> LicensePlates { get; set; }
    public DbSet<LicensePlateDetail> LicensePlateDetails { get; set; }
    public DbSet<InventorySnapshotOutbox> InventorySnapshotOutbox { get; set; }
    public DbSet<InventoryReconciliationRun> InventoryReconciliationRuns { get; set; }
    public DbSet<InventoryReconciliationIssue> InventoryReconciliationIssues { get; set; }
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
    public DbSet<SerialNumber> SerialNumbers { get; set; }
    public DbSet<SerialReservation> SerialReservations { get; set; }
    public DbSet<SerialInventoryOperation> SerialInventoryOperations { get; set; }
    public DbSet<ShippingHandoverLog> ShippingHandoverLogs { get; set; }
    public DbSet<OutboundPackage> OutboundPackages { get; set; }
    public DbSet<CatchWeightEntry> CatchWeightEntries { get; set; }
    public DbSet<ShipmentLoad> ShipmentLoads { get; set; }
    public DbSet<ShipmentLoadVoucher> ShipmentLoadVouchers { get; set; }
    public DbSet<ShipmentLoadPackage> ShipmentLoadPackages { get; set; }
    public DbSet<CarrierConnector> CarrierConnectors { get; set; }
    public DbSet<CarrierShipment> CarrierShipments { get; set; }
    public DbSet<CarrierShipmentEvent> CarrierShipmentEvents { get; set; }
    public DbSet<DockAppointment> DockAppointments { get; set; }
    public DbSet<ThreePlBillingRate> ThreePlBillingRates { get; set; }
    public DbSet<ThreePlBillingRun> ThreePlBillingRuns { get; set; }
    public DbSet<ThreePlBillingCharge> ThreePlBillingCharges { get; set; }
    public DbSet<ThreePlContract> ThreePlContracts { get; set; }
    public DbSet<ThreePlContractRate> ThreePlContractRates { get; set; }
    public DbSet<ThreePlInvoice> ThreePlInvoices { get; set; }
    public DbSet<ThreePlInvoiceLine> ThreePlInvoiceLines { get; set; }
    public DbSet<ThreePlDispute> ThreePlDisputes { get; set; }
    public DbSet<MheSystem> MheSystems { get; set; }
    public DbSet<MheCommand> MheCommands { get; set; }
    public DbSet<MheMissionEvent> MheMissionEvents { get; set; }
    public DbSet<MheAdapterProfile> MheAdapterProfiles { get; set; }
    public DbSet<MheTelemetryEvent> MheTelemetryEvents { get; set; }
    public DbSet<WcsSimulatorRun> WcsSimulatorRuns { get; set; }
    public DbSet<AutomationOverride> AutomationOverrides { get; set; }
    public DbSet<OperationExceptionCase> OperationExceptionCases { get; set; }

    // Enterprise upgrade: QC, Labor Standards, Currency
    public DbSet<QualityInspection> QualityInspections { get; set; }
    public DbSet<LaborStandard> LaborStandards { get; set; }
    public DbSet<LaborActivity> LaborActivities { get; set; }
    public DbSet<LaborExceptionReview> LaborExceptionReviews { get; set; }
    public DbSet<CurrencyRate> CurrencyRates { get; set; }
    public DbSet<InspectionPlanTemplate> InspectionPlanTemplates { get; set; }
    public DbSet<ScheduledReport> ScheduledReports { get; set; }

    // P2.4: Labor Activity Standards (mở rộng)
    public DbSet<LaborActivityStandard> LaborActivityStandards { get; set; }

    // P3.2: SLA Metrics & Observability
    public DbSet<SlaMetric> SlaMetrics { get; set; }

    // P3.3: Capacity Simulation
    public DbSet<CapacityScenario> CapacityScenarios { get; set; }
    public DbSet<SlottingSimulationScenario> SlottingSimulationScenarios { get; set; }
    public DbSet<SlottingSimulationLine> SlottingSimulationLines { get; set; }
    public DbSet<OptimizationRun> OptimizationRuns { get; set; }
    public DbSet<OptimizationRecommendationLine> OptimizationRecommendationLines { get; set; }
    public DbSet<WavelessReleaseQueue> WavelessReleaseQueue { get; set; }
    public DbSet<PickPathPlan> PickPathPlans { get; set; }
    public DbSet<PickPathPlanStop> PickPathPlanStops { get; set; }
    public DbSet<ToteClusterPlan> ToteClusterPlans { get; set; }
    public DbSet<ToteClusterAssignment> ToteClusterAssignments { get; set; }

    // P3.1: Item Velocity Classification (ABC/XYZ)
    public DbSet<ItemVelocityClassification> ItemVelocityClassifications { get; set; }

    // P1.2: Cluster picking — Cart & Tote
    public DbSet<PickCart> PickCarts { get; set; }
    public DbSet<PickTote> PickTotes { get; set; }

    // P1.3: Zone picking assignment
    public DbSet<UserZoneAssignment> UserZoneAssignments { get; set; }

    // P2-03: Yard management MVP
    public DbSet<YardSpot> YardSpots { get; set; }
    public DbSet<Trailer> Trailers { get; set; }
    public DbSet<YardVisit> YardVisits { get; set; }
    public DbSet<YardVisitEvidence> YardVisitEvidence { get; set; }

    // P2-03B: Yard billing detention/demurrage
    public DbSet<YardBillingRate> YardBillingRates { get; set; }
    public DbSet<YardBillingCharge> YardBillingCharges { get; set; }
    public DbSet<EdiMessage> EdiMessages { get; set; }
    public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; }
    public DbSet<WebhookDelivery> WebhookDeliveries { get; set; }
    public DbSet<EnterpriseConnector> EnterpriseConnectors { get; set; }
    public DbSet<EnterpriseConnectorDelivery> EnterpriseConnectorDeliveries { get; set; }

    // P2-04: Movement tasks
    public DbSet<MovementTask> MovementTasks { get; set; }
    public DbSet<ReplenishmentAutomationRun> ReplenishmentAutomationRuns { get; set; }
    public DbSet<ReplenishmentAutomationLine> ReplenishmentAutomationLines { get; set; }

    // P3-01: VAS kitting work orders
    public DbSet<KittingWorkOrder> KittingWorkOrders { get; set; }
    public DbSet<KittingWorkOrderLine> KittingWorkOrderLines { get; set; }

    // P3-02: Customer-specific labeling
    public DbSet<PartnerLabelTemplate> PartnerLabelTemplates { get; set; }
    public DbSet<PartnerItemLabelRule> PartnerItemLabelRules { get; set; }
    public DbSet<LabelPrintJob> LabelPrintJobs { get; set; }
    public DbSet<LabelPrintJobLine> LabelPrintJobLines { get; set; }

    // P3-03: VAS light assembly and co-packing
    public DbSet<VasWorkOrder> VasWorkOrders { get; set; }
    public DbSet<VasOperation> VasOperations { get; set; }
    public DbSet<VasMaterialLine> VasMaterialLines { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ItemCategory self-referencing
        modelBuilder.Entity<ItemCategory>()
            .HasOne(c => c.ParentCategory)
            .WithMany(c => c.ChildCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ItemCategory>()
            .HasIndex(c => c.CategoryCode).IsUnique();

        // UnitOfMeasure
        modelBuilder.Entity<UnitOfMeasure>()
            .HasIndex(u => u.UomCode).IsUnique();

        // UnitConversion
        // Item-specific conversion unique
        modelBuilder.Entity<UnitConversion>()
            .HasIndex(uc => new { uc.ItemId, uc.FromUomId, uc.ToUomId })
            .IsUnique()
            .HasFilter("[ItemId] IS NOT NULL");

        // Global conversion unique (ItemId = NULL)
        modelBuilder.Entity<UnitConversion>()
            .HasIndex(uc => new { uc.FromUomId, uc.ToUomId })
            .IsUnique()
            .HasFilter("[ItemId] IS NULL");

        modelBuilder.Entity<UnitConversion>()
            .HasOne(uc => uc.FromUom)
            .WithMany()
            .HasForeignKey(uc => uc.FromUomId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UnitConversion>()
            .HasOne(uc => uc.ToUom)
            .WithMany()
            .HasForeignKey(uc => uc.ToUomId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UnitConversion>()
            .HasOne(uc => uc.Item)
            .WithMany()
            .HasForeignKey(uc => uc.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        // PackagingUnit
        modelBuilder.Entity<PackagingUnit>()
            .HasIndex(p => p.TenDongGoi).IsUnique();

        modelBuilder.Entity<PackagingUnit>()
            .HasOne(p => p.BaseUom)
            .WithMany()
            .HasForeignKey(p => p.BaseUomId)
            .OnDelete(DeleteBehavior.NoAction);

        // Item
        modelBuilder.Entity<Item>()
            .HasIndex(i => i.ItemCode).IsUnique();

        modelBuilder.Entity<Item>()
            .HasOne(i => i.BaseUom)
            .WithMany()
            .HasForeignKey(i => i.BaseUomId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Item>()
            .HasOne(i => i.CatchWeightUom)
            .WithMany()
            .HasForeignKey(i => i.CatchWeightUomId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Item>()
            .HasOne(i => i.Category)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // BOM
        modelBuilder.Entity<BillOfMaterial>()
            .HasOne(b => b.ParentItem)
            .WithMany()
            .HasForeignKey(b => b.ParentItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<BillOfMaterial>()
            .HasOne(b => b.ChildItem)
            .WithMany()
            .HasForeignKey(b => b.ChildItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<BillOfMaterial>()
            .HasOne(b => b.Uom)
            .WithMany()
            .HasForeignKey(b => b.UomId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<BillOfMaterial>()
            .HasIndex(b => new { b.ParentItemId, b.ChildItemId, b.EffectiveFrom }).IsUnique();

        // Warehouse
        modelBuilder.Entity<Warehouse>()
            .HasIndex(w => w.WarehouseCode).IsUnique();

        modelBuilder.Entity<Warehouse>()
            .HasOne(w => w.ManagerUser)
            .WithMany()
            .HasForeignKey(w => w.ManagerUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Zone
        modelBuilder.Entity<Zone>()
            .HasIndex(z => new { z.WarehouseId, z.ZoneCode }).IsUnique();

        // Location
        modelBuilder.Entity<Location>()
            .HasIndex(l => l.LocationCode).IsUnique();

        modelBuilder.Entity<Location>()
            .Property(l => l.HeightLevel)
            .HasDefaultValue(1);

        modelBuilder.Entity<Location>()
            .Property(l => l.IsGoldenZone)
            .HasDefaultValue(false);

        modelBuilder.Entity<Location>()
            .Property(l => l.AllowMechanicalHandling)
            .HasDefaultValue(false);

        modelBuilder.Entity<Location>()
            .Property(l => l.AllowMixedSku)
            .HasDefaultValue(false);

        // ItemLocation
        // ItemLocation uniqueness by batch:
        // SQL Server unique indexes allow multiple NULLs, so we use filtered unique indexes to prevent duplicates.
        // 1) No lot + no expiry => one snapshot row per (OwnerPartnerId, ItemId, LocationId, HoldStatus)
        modelBuilder.Entity<ItemLocation>()
            .HasIndex(il => new { il.OwnerPartnerId, il.ItemId, il.LocationId, il.HoldStatus })
            .IsUnique()
            .HasFilter("[LotNumber] IS NULL AND [ExpiryDate] IS NULL")
            .HasDatabaseName("IX_ItemLocations_Item_Location_Hold_NoBatch");

        // 2) Lot present + expiry NULL => unique by (OwnerPartnerId, ItemId, LocationId, LotNumber, HoldStatus)
        modelBuilder.Entity<ItemLocation>()
            .HasIndex(il => new { il.OwnerPartnerId, il.ItemId, il.LocationId, il.LotNumber, il.HoldStatus })
            .IsUnique()
            .HasFilter("[LotNumber] IS NOT NULL AND [ExpiryDate] IS NULL")
            .HasDatabaseName("IX_ItemLocations_Item_Location_Lot_Hold");

        // 3) Lot NULL + expiry present => unique by (OwnerPartnerId, ItemId, LocationId, ExpiryDate, HoldStatus)
        modelBuilder.Entity<ItemLocation>()
            .HasIndex(il => new { il.OwnerPartnerId, il.ItemId, il.LocationId, il.ExpiryDate, il.HoldStatus })
            .IsUnique()
            .HasFilter("[LotNumber] IS NULL AND [ExpiryDate] IS NOT NULL")
            .HasDatabaseName("IX_ItemLocations_Item_Location_Expiry_Hold");

        // 4) Lot present + expiry present => unique by full snapshot key
        modelBuilder.Entity<ItemLocation>()
            .HasIndex(il => new { il.OwnerPartnerId, il.ItemId, il.LocationId, il.LotNumber, il.ExpiryDate, il.HoldStatus })
            .IsUnique()
            .HasFilter("[LotNumber] IS NOT NULL AND [ExpiryDate] IS NOT NULL")
            .HasDatabaseName("IX_ItemLocations_Item_Location_Lot_Expiry_Hold");

        // Partner
        modelBuilder.Entity<Partner>()
            .HasIndex(p => p.PartnerCode).IsUnique();

        modelBuilder.Entity<Partner>()
            .HasIndex(p => new { p.IsThreePlClient, p.IsActive })
            .HasDatabaseName("IX_Partners_3PL_Active");

        modelBuilder.Entity<AppUserOwnerScope>()
            .HasIndex(x => new { x.UserId, x.OwnerPartnerId, x.IsActive })
            .HasDatabaseName("IX_AppUserOwnerScopes_User_Owner_Active");

        modelBuilder.Entity<AppUserOwnerScope>()
            .HasIndex(x => new { x.UserId, x.OwnerPartnerId })
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("UX_AppUserOwnerScopes_User_Owner_Active");

        modelBuilder.Entity<AppUserOwnerScope>()
            .HasOne(x => x.User)
            .WithMany(u => u.OwnerScopes)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AppUserOwnerScope>()
            .HasOne(x => x.OwnerPartner)
            .WithMany(p => p.UserOwnerScopes)
            .HasForeignKey(x => x.OwnerPartnerId)
            .OnDelete(DeleteBehavior.NoAction);

        // Voucher
        // P0-5: composite unique (WarehouseId, VoucherCode) — counter per-warehouse không clash global,
        // 3PL multi-tenant không bị reject khi 2 kho cùng dùng prefix giống nhau.
        modelBuilder.Entity<Voucher>()
            .HasIndex(v => new { v.WarehouseId, v.VoucherCode }).IsUnique();

        modelBuilder.Entity<Voucher>()
            .HasIndex(v => v.AsnCode)
            .IsUnique()
            .HasFilter("[AsnCode] IS NOT NULL");

        modelBuilder.Entity<Voucher>()
            .HasOne(v => v.Warehouse)
            .WithMany()
            .HasForeignKey(v => v.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Voucher>()
            .HasOne(v => v.DestWarehouse)
            .WithMany()
            .HasForeignKey(v => v.DestWarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Voucher>()
            .HasOne(v => v.Partner)
            .WithMany()
            .HasForeignKey(v => v.PartnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Voucher>()
            .HasOne(v => v.Wave)
            .WithMany(w => w.Vouchers)
            .HasForeignKey(v => v.WaveId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Voucher>()
            .HasOne(v => v.ParentVoucher)
            .WithMany(v => v.ChildVouchers)
            .HasForeignKey(v => v.ParentVoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Voucher>()
            .HasIndex(v => v.ParentVoucherId)
            .HasDatabaseName("IX_Vouchers_ParentVoucherId");

        modelBuilder.Entity<Voucher>()
            .Property(v => v.ReviewResult)
            .HasDefaultValue(ReviewResultEnum.Pending)
            .HasSentinel(ReviewResultEnum.Undefined);

        modelBuilder.Entity<Voucher>()
            .Property(v => v.ResponsibilityScore)
            .HasDefaultValue(0m);

        modelBuilder.Entity<Voucher>()
            .ToTable(tb => tb.HasCheckConstraint("CK_Vouchers_ResponsibilityScore_Range", "[ResponsibilityScore] >= 0 AND [ResponsibilityScore] <= 100"));

        // P2-1: defense-in-depth — chặn DB level các giá trị âm/quá giới hạn ở các bảng tồn kho quan trọng.
        modelBuilder.Entity<VoucherDetail>()
            .ToTable(tb => tb.HasCheckConstraint("CK_VoucherDetails_DefectQty_NonNegative",
                "[DefectQty] >= 0 AND [DefectBaseQty] >= 0"));

        modelBuilder.Entity<StockReservation>()
            .ToTable(tb => tb.HasCheckConstraint("CK_StockReservations_Qty_NonNegative",
                "[ReservedQty] >= 0 AND [ConsumedQty] >= 0 AND [ReleasedQty] >= 0"));

        // VoucherDetail
        modelBuilder.Entity<VoucherDetail>()
            .ToTable(tb =>
            {
                tb.HasTrigger("TR_VoucherDetails_AfterInsert");
                tb.HasTrigger("TR_VoucherDetails_PreventModify");
            });

        modelBuilder.Entity<VoucherDetail>()
            .HasOne(vd => vd.Item)
            .WithMany()
            .HasForeignKey(vd => vd.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<VoucherDetail>()
            .HasOne(vd => vd.Location)
            .WithMany()
            .HasForeignKey(vd => vd.LocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<VoucherDetail>()
            .HasOne(vd => vd.DestLocation)
            .WithMany()
            .HasForeignKey(vd => vd.DestLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<VoucherDetail>()
            .HasOne(vd => vd.TransactionUom)
            .WithMany()
            .HasForeignKey(vd => vd.TransactionUomId)
            .OnDelete(DeleteBehavior.NoAction);

        // AppUser
        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.UserName).IsUnique();

        modelBuilder.Entity<AppUser>()
            .HasOne(u => u.Warehouse)
            .WithMany()
            .HasForeignKey(u => u.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<AppUser>()
            .HasOne(u => u.Role)
            .WithMany()
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.NoAction);

        // AppRole
        modelBuilder.Entity<AppRole>()
            .HasIndex(r => r.RoleName).IsUnique();

        modelBuilder.Entity<LoginAuditLog>()
            .HasIndex(x => new { x.UserName, x.CreatedAt });

        modelBuilder.Entity<LoginAuditLog>()
            .HasIndex(x => new { x.IsSuccess, x.CreatedAt });

        modelBuilder.Entity<MfaLoginChallenge>()
            .HasIndex(x => new { x.UserId, x.CreatedAt });

        modelBuilder.Entity<MfaLoginChallenge>()
            .HasIndex(x => new { x.UserName, x.IsUsed, x.ExpiresAt });

        modelBuilder.Entity<MfaLoginChallenge>()
            .HasIndex(x => new { x.IsUsed, x.ExpiresAt })
            .HasDatabaseName("IX_MfaLoginChallenges_IsUsed_ExpiresAt");

        modelBuilder.Entity<LoginHelpRequest>()
            .HasIndex(x => x.RequestCode)
            .IsUnique();

        modelBuilder.Entity<LoginHelpRequest>()
            .HasIndex(x => new { x.Status, x.CreatedAt })
            .HasDatabaseName("IX_LoginHelpRequests_Status_CreatedAt");

        modelBuilder.Entity<LoginHelpRequest>()
            .HasIndex(x => new { x.Reason, x.Status, x.CreatedAt })
            .HasDatabaseName("IX_LoginHelpRequests_Reason_Status_CreatedAt");

        // ═══ LARGE WAREHOUSE PERFORMANCE INDEXES ═══

        // Voucher: most common query patterns (list by warehouse + status + date)
        modelBuilder.Entity<Voucher>()
            .HasIndex(v => new { v.WarehouseId, v.IsPosted, v.IsCancelled, v.VoucherDate })
            .HasDatabaseName("IX_Vouchers_Warehouse_Status_Date");

        modelBuilder.Entity<Voucher>()
            .HasIndex(v => new { v.VoucherType, v.VoucherDate })
            .HasDatabaseName("IX_Vouchers_Type_Date");

        modelBuilder.Entity<Voucher>()
            .HasIndex(v => new { v.WarehouseId, v.DockDoor, v.DockAppointmentStart, v.DockAppointmentEnd })
            .HasDatabaseName("IX_Vouchers_Warehouse_Dock_Window");

        modelBuilder.Entity<Voucher>()
            .Property(v => v.DockStatus)
            .HasSentinel((DockOperationStatusEnum)0)
            .HasDefaultValue(DockOperationStatusEnum.Scheduled);

        modelBuilder.Entity<Voucher>()
            .HasIndex(v => new { v.WarehouseId, v.DockStatus, v.DockAppointmentStart })
            .HasDatabaseName("IX_Vouchers_Warehouse_Dock_Status");

        modelBuilder.Entity<Voucher>()
            .HasIndex(v => v.WaveId)
            .HasDatabaseName("IX_Vouchers_WaveId");

        // VoucherDetail: lookup by voucher + item
        modelBuilder.Entity<VoucherDetail>()
            .HasIndex(vd => new { vd.VoucherId, vd.ItemId })
            .HasDatabaseName("IX_VoucherDetails_Voucher_Item");

        // StockReservation: common query for Cancel/Release operations
        modelBuilder.Entity<StockReservation>()
            .HasIndex(sr => new { sr.VoucherId, sr.Status })
            .HasDatabaseName("IX_StockReservations_Voucher_Status");

        modelBuilder.Entity<StockReservation>()
            .HasIndex(sr => new { sr.ItemId, sr.LocationId, sr.Status })
            .HasDatabaseName("IX_StockReservations_Item_Location_Status");

        // StockAlert: active alerts lookup
        modelBuilder.Entity<StockAlert>()
            .HasIndex(sa => new { sa.ItemId, sa.IsResolved, sa.AlertType })
            .HasDatabaseName("IX_StockAlerts_Item_Resolved_Type");

        // AuditLog: timeline queries
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.TableName, a.ChangedAt })
            .HasDatabaseName("IX_AuditLogs_Table_Date");

        // P1.2: IntegrationOutbox — pending items, idempotency lookup, event type
        modelBuilder.Entity<IntegrationOutbox>()
            .HasIndex(o => new { o.Status, o.CreatedAt })
            .HasDatabaseName("IX_IntegrationOutbox_Status_Created");
        modelBuilder.Entity<IntegrationOutbox>()
            .HasIndex(o => o.IdempotencyKey)
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL")
            .HasDatabaseName("IX_IntegrationOutbox_IdempotencyKey");

        modelBuilder.Entity<IntegrationOutbox>()
            .Property(o => o.RowVersion)
            .IsRowVersion();

        // P1.2: IntegrationIdempotencyKeys — unique key value
        modelBuilder.Entity<IntegrationIdempotencyKey>()
            .HasIndex(k => k.KeyValue)
            .IsUnique()
            .HasDatabaseName("IX_IntegrationIdempotencyKeys_KeyValue");

        // Item: barcode search + type/active filter
        modelBuilder.Entity<Item>()
            .HasIndex(i => i.Barcode)
            .HasDatabaseName("IX_Items_Barcode");

        modelBuilder.Entity<Item>()
            .HasIndex(i => new { i.ItemType, i.IsActive })
            .HasDatabaseName("IX_Items_Type_Active");

        // ItemLocation: covering index for stock queries
        modelBuilder.Entity<ItemLocation>()
            .HasIndex(il => new { il.LocationId, il.Quantity })
            .HasDatabaseName("IX_ItemLocations_Location_Qty");

        modelBuilder.Entity<Item>()
            .HasIndex(i => new { i.OwnerPartnerId, i.IsActive })
            .HasDatabaseName("IX_Items_Owner_Active");

        modelBuilder.Entity<Voucher>()
            .HasIndex(v => new { v.OwnerPartnerId, v.WarehouseId, v.CreatedAt })
            .HasDatabaseName("IX_Vouchers_Owner_Warehouse_Date");

        modelBuilder.Entity<VoucherDetail>()
            .HasIndex(v => new { v.OwnerPartnerId, v.ItemId })
            .HasDatabaseName("IX_VoucherDetails_Owner_Item");

        modelBuilder.Entity<ItemLocation>()
            .HasIndex(il => new { il.OwnerPartnerId, il.ItemId, il.LocationId, il.LotNumber, il.ExpiryDate, il.HoldStatus })
            .HasDatabaseName("IX_ItemLocations_Owner_SnapshotKey");

        modelBuilder.Entity<StockReservation>()
            .HasIndex(r => new { r.OwnerPartnerId, r.ItemId, r.LocationId, r.Status })
            .HasDatabaseName("IX_StockReservations_Owner_Item_Location_Status");

        modelBuilder.Entity<LicensePlate>()
            .HasIndex(l => new { l.OwnerPartnerId, l.WarehouseId, l.Status })
            .HasDatabaseName("IX_LicensePlates_Owner_Warehouse_Status");

        modelBuilder.Entity<LicensePlateDetail>()
            .HasIndex(d => new { d.OwnerPartnerId, d.ItemId, d.LicensePlateId })
            .HasDatabaseName("IX_LicensePlateDetails_Owner_Item_LPN");

        modelBuilder.Entity<SerialNumber>()
            .HasIndex(s => new { s.OwnerPartnerId, s.WarehouseId, s.ItemId, s.Status })
            .HasDatabaseName("IX_SerialNumbers_Owner_Warehouse_Item_Status");

        modelBuilder.Entity<PickTask>()
            .HasIndex(t => new { t.OwnerPartnerId, t.Status, t.DueAt })
            .HasDatabaseName("IX_PickTasks_Owner_Status_Due");

        modelBuilder.Entity<MovementTask>()
            .HasIndex(t => new { t.OwnerPartnerId, t.Status, t.DueAt })
            .HasDatabaseName("IX_MovementTasks_Owner_Status_Due");

        modelBuilder.Entity<OutboundPackage>()
            .HasIndex(p => new { p.OwnerPartnerId, p.WarehouseId, p.PackedAt })
            .HasDatabaseName("IX_OutboundPackages_Owner_Warehouse_Packed");

        modelBuilder.Entity<InventoryTransaction>()
            .HasIndex(t => new { t.OwnerPartnerId, t.WarehouseId, t.TransactionAt })
            .HasDatabaseName("IX_InventoryTransactions_Owner_Warehouse_Date");

        // ═══ CONCURRENCY TOKENS (RowVersion) ═══
        modelBuilder.Entity<Item>()
            .Property(i => i.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<ItemLocation>()
            .Property(il => il.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<InventoryTransaction>()
            .HasIndex(t => t.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("UX_InventoryTransactions_IdempotencyKey");

        modelBuilder.Entity<InventoryTransaction>()
            .HasIndex(t => new { t.WarehouseId, t.TransactionAt })
            .HasDatabaseName("IX_InventoryTransactions_Warehouse_Date");

        modelBuilder.Entity<InventoryTransaction>()
            .HasIndex(t => new { t.ItemId, t.LocationId, t.TransactionAt })
            .HasDatabaseName("IX_InventoryTransactions_Item_Location_Date");

        modelBuilder.Entity<InventoryTransaction>()
            .HasIndex(t => new { t.TransactionType, t.TransactionAt })
            .HasDatabaseName("IX_InventoryTransactions_Type_Date");

        modelBuilder.Entity<InventoryTransaction>()
            .HasIndex(t => new { t.ReferenceType, t.ReferenceId })
            .HasDatabaseName("IX_InventoryTransactions_Reference");

        modelBuilder.Entity<InventoryTransaction>()
            .HasIndex(t => t.TransactionGroupKey)
            .HasDatabaseName("IX_InventoryTransactions_GroupKey");

        modelBuilder.Entity<InventoryTransaction>()
            .HasIndex(t => t.LicensePlateId)
            .HasDatabaseName("IX_InventoryTransactions_LicensePlateId");

        modelBuilder.Entity<InventoryTransaction>()
            .HasIndex(t => t.SerialNumberId)
            .HasDatabaseName("IX_InventoryTransactions_SerialNumberId");

        modelBuilder.Entity<InventoryTransaction>()
            .Property(t => t.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<InventoryTransaction>()
            .Property(t => t.MetadataJson)
            .HasDefaultValue("{}");

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.Warehouse)
            .WithMany()
            .HasForeignKey(t => t.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.Item)
            .WithMany()
            .HasForeignKey(t => t.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.Location)
            .WithMany()
            .HasForeignKey(t => t.LocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.Voucher)
            .WithMany()
            .HasForeignKey(t => t.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.VoucherDetail)
            .WithMany()
            .HasForeignKey(t => t.VoucherDetailId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.PickTask)
            .WithMany()
            .HasForeignKey(t => t.PickTaskId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.MovementTask)
            .WithMany()
            .HasForeignKey(t => t.MovementTaskId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.StockReservation)
            .WithMany()
            .HasForeignKey(t => t.StockReservationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.LicensePlate)
            .WithMany()
            .HasForeignKey(t => t.LicensePlateId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.SerialNumber)
            .WithMany()
            .HasForeignKey(t => t.SerialNumberId)
            .OnDelete(DeleteBehavior.SetNull);

        // Permissions
        modelBuilder.Entity<Permission>()
            .HasIndex(p => p.Code).IsUnique();

        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany()
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany()
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed base permissions (enterprise RBAC building blocks)
        var seedCreatedAt = new DateTime(2026, 1, 1, 0, 0, 0);
        var permSeed = WmsPermissions.All
            .Select((code, idx) => new Permission
            {
                PermissionId = idx + 1,
                Code = code,
                Description = code,
                CreatedAt = seedCreatedAt
            })
            .ToArray();
        modelBuilder.Entity<Permission>().HasData(permSeed);

        // Seed role-permission mapping (RoleId is bootstrapped as 1..4 in SetupAdmin)
        RolePermission RP(int roleId, int permissionId) => new RolePermission { RoleId = roleId, PermissionId = permissionId, CreatedAt = seedCreatedAt };
        var p = permSeed.ToDictionary(x => x.Code, x => x.PermissionId);
        var rolePerms = new List<RolePermission>();

        // Admin: all permissions
        rolePerms.AddRange(permSeed.Select(x => RP(1, x.PermissionId)));

        // Manager: operate warehouse + approve/cancel + reports
        rolePerms.AddRange(new[]
        {
            RP(2, p[WmsPermissions.VoucherCreate]),
            RP(2, p[WmsPermissions.VoucherApproveInbound]),
            RP(2, p[WmsPermissions.VoucherCancel]),
            RP(2, p[WmsPermissions.VoucherPostOutbound]),
            RP(2, p[WmsPermissions.VoucherReleasePicking]),
            RP(2, p[WmsPermissions.MasterItemManage]),
            RP(2, p[WmsPermissions.MasterPartnerManage]),
            RP(2, p[WmsPermissions.MasterCategoryManage]),
            RP(2, p[WmsPermissions.MasterUomManage]),
            RP(2, p[WmsPermissions.WarehouseConfigManage]),
            RP(2, p[WmsPermissions.ReportView]),
            RP(2, p[WmsPermissions.ReportViewFinancial]),
            RP(2, p[WmsPermissions.PickTaskReassign]),
            RP(2, p[WmsPermissions.TenantScopeManage]),
            RP(2, p[WmsPermissions.ThreePlBillingManage]),
            RP(2, p[WmsPermissions.MheManage]),
        });

        // Staff: create voucher + execute picking + view reports (no approve/cancel)
        rolePerms.AddRange(new[]
        {
            RP(3, p[WmsPermissions.VoucherCreate]),
            RP(3, p[WmsPermissions.ReportView]),
        });

        // Viewer: view reports only
        rolePerms.AddRange(new[]
        {
            RP(4, p[WmsPermissions.ReportView]),
        });

        modelBuilder.Entity<RolePermission>().HasData(rolePerms.ToArray());

        // StockSnapshot
        modelBuilder.Entity<StockSnapshot>()
            .HasIndex(s => new { s.SnapshotDate, s.ItemId, s.WarehouseId }).IsUnique();

        modelBuilder.Entity<StockSnapshot>()
            .HasOne(s => s.Item)
            .WithMany()
            .HasForeignKey(s => s.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<StockSnapshot>()
            .HasOne(s => s.Warehouse)
            .WithMany()
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        // StockAlert
        modelBuilder.Entity<StockAlert>()
            .HasOne(a => a.Item)
            .WithMany()
            .HasForeignKey(a => a.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        // Stock count sheet/lines
        modelBuilder.Entity<StockCountSheet>()
            .HasOne(s => s.Warehouse)
            .WithMany()
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<StockCountSheet>()
            .HasOne(s => s.GeneratedAdjustmentVoucher)
            .WithMany()
            .HasForeignKey(s => s.GeneratedAdjustmentVoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<StockCountLine>()
            .HasOne(l => l.StockCountSheet)
            .WithMany(s => s.Lines)
            .HasForeignKey(l => l.StockCountSheetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockCountLine>()
            .HasOne(l => l.Item)
            .WithMany()
            .HasForeignKey(l => l.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<StockCountLine>()
            .HasOne(l => l.Location)
            .WithMany()
            .HasForeignKey(l => l.LocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<StockCountLine>()
            .HasIndex(l => new { l.StockCountSheetId, l.ItemId, l.LocationId, l.LotNumber, l.ExpiryDate })
            .IsUnique();

        modelBuilder.Entity<SlottingSimulationScenario>()
            .HasIndex(s => s.ScenarioCode)
            .IsUnique();

        modelBuilder.Entity<SlottingSimulationScenario>()
            .HasOne(s => s.Warehouse)
            .WithMany()
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SlottingSimulationLine>()
            .HasOne(l => l.Scenario)
            .WithMany(s => s.Lines)
            .HasForeignKey(l => l.ScenarioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SlottingSimulationLine>()
            .HasIndex(l => new { l.ScenarioId, l.ItemId, l.SourceLocationId, l.SuggestedLocationId })
            .HasDatabaseName("IX_SlottingSimulationLines_Scenario_Item_Move");

        modelBuilder.Entity<SlottingSimulationLine>()
            .HasOne(l => l.Item)
            .WithMany()
            .HasForeignKey(l => l.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SlottingSimulationLine>()
            .HasOne(l => l.CurrentDefaultLocation)
            .WithMany()
            .HasForeignKey(l => l.CurrentDefaultLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SlottingSimulationLine>()
            .HasOne(l => l.SourceLocation)
            .WithMany()
            .HasForeignKey(l => l.SourceLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SlottingSimulationLine>()
            .HasOne(l => l.SuggestedLocation)
            .WithMany()
            .HasForeignKey(l => l.SuggestedLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SlottingSimulationLine>()
            .HasOne(l => l.SourceItemLocation)
            .WithMany()
            .HasForeignKey(l => l.SourceItemLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SlottingSimulationLine>()
            .HasOne(l => l.MovementTask)
            .WithMany()
            .HasForeignKey(l => l.MovementTaskId)
            .OnDelete(DeleteBehavior.NoAction);

        // Warehouse period lock
        modelBuilder.Entity<WarehousePeriodLock>()
            .HasOne(p => p.Warehouse)
            .WithMany()
            .HasForeignKey(p => p.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WarehousePeriodLock>()
            .HasIndex(p => new { p.WarehouseId, p.IsActive });

        // Unique constraint: one lock per warehouse per date
        modelBuilder.Entity<WarehousePeriodLock>()
            .HasIndex(p => new { p.WarehouseId, p.LockDate })
            .IsUnique()
            .HasDatabaseName("IX_WarehousePeriodLocks_Warehouse_LockDate");

        // Reservation
        modelBuilder.Entity<StockReservation>()
            .HasOne(r => r.Voucher)
            .WithMany()
            .HasForeignKey(r => r.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        // RowVersion for optimistic concurrency
        modelBuilder.Entity<StockReservation>()
            .Property(r => r.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<StockReservation>()
            .HasOne(r => r.VoucherDetail)
            .WithMany()
            .HasForeignKey(r => r.VoucherDetailId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<StockReservation>()
            .HasOne(r => r.Item)
            .WithMany()
            .HasForeignKey(r => r.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<StockReservation>()
            .HasOne(r => r.Location)
            .WithMany()
            .HasForeignKey(r => r.LocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<StockReservation>()
            .HasIndex(r => new { r.VoucherId, r.Status });

        modelBuilder.Entity<StockReservation>()
            .HasIndex(r => new { r.ItemId, r.LocationId, r.LotNumber, r.ExpiryDate, r.Status });

        modelBuilder.Entity<StockReservation>()
            .HasIndex(r => new { r.VoucherId, r.VoucherDetailId, r.ItemId, r.LocationId, r.LotNumber, r.ExpiryDate })
            .IsUnique()
            .HasFilter("[Status] = 1");

        // Wave + picking
        modelBuilder.Entity<Wave>()
            .HasOne(w => w.Warehouse)
            .WithMany()
            .HasForeignKey(w => w.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Wave>()
            .HasIndex(w => w.WaveCode)
            .IsUnique();

        modelBuilder.Entity<WaveLine>()
            .HasOne(wl => wl.Wave)
            .WithMany(w => w.Lines)
            .HasForeignKey(wl => wl.WaveId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WaveLine>()
            .HasOne(wl => wl.Voucher)
            .WithMany()
            .HasForeignKey(wl => wl.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WaveLine>()
            .HasOne(wl => wl.Item)
            .WithMany()
            .HasForeignKey(wl => wl.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WaveLine>()
            .HasIndex(wl => new { wl.WaveId, wl.VoucherId, wl.ItemId });

        modelBuilder.Entity<PickTask>()
            .HasOne(t => t.Wave)
            .WithMany(w => w.PickTasks)
            .HasForeignKey(t => t.WaveId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTask>()
            .HasOne(t => t.Voucher)
            .WithMany()
            .HasForeignKey(t => t.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTask>()
            .HasOne(t => t.VoucherDetail)
            .WithMany()
            .HasForeignKey(t => t.VoucherDetailId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTask>()
            .HasOne(t => t.Item)
            .WithMany()
            .HasForeignKey(t => t.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTask>()
            .HasOne(t => t.SourceLocation)
            .WithMany()
            .HasForeignKey(t => t.SourceLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTask>()
            .Property(t => t.PickTaskMode)
            .HasDefaultValue(PickTaskModeEnum.Single)
            .HasSentinel((PickTaskModeEnum)0);

        modelBuilder.Entity<PickTask>()
            .HasOne(t => t.TargetLocation)
            .WithMany()
            .HasForeignKey(t => t.TargetLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTask>()
            .HasOne(t => t.ParentPickTask)
            .WithMany(t => t.ChildPickTasks)
            .HasForeignKey(t => t.ParentPickTaskId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTask>()
            .HasOne(t => t.SortationStageLocation)
            .WithMany()
            .HasForeignKey(t => t.SortationStageLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTask>()
            .HasOne(t => t.SortationDestinationLocation)
            .WithMany()
            .HasForeignKey(t => t.SortationDestinationLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTask>()
            .HasIndex(t => t.SourceLocationId)
            .HasDatabaseName("IX_PickTasks_SourceLocationId");

        modelBuilder.Entity<PickTask>()
            .HasIndex(t => t.TargetLocationId)
            .HasDatabaseName("IX_PickTasks_TargetLocationId");

        modelBuilder.Entity<PickTask>()
            .HasIndex(t => t.TaskCode)
            .IsUnique();

        modelBuilder.Entity<PickTask>()
            .HasIndex(t => new { t.WaveId, t.Status, t.AssignedTo });

        modelBuilder.Entity<PickTask>()
            .HasIndex(t => new { t.WaveId, t.PickTaskMode, t.Status })
            .HasDatabaseName("IX_PickTasks_Wave_Mode_Status");

        modelBuilder.Entity<PickTask>()
            .HasIndex(t => new { t.VoucherId, t.Status })
            .HasDatabaseName("IX_PickTasks_Voucher_Status");

        modelBuilder.Entity<PickTask>()
            .HasIndex(t => t.ParentPickTaskId)
            .HasDatabaseName("IX_PickTasks_ParentPickTaskId");

        modelBuilder.Entity<PickTaskAllocation>()
            .HasOne(a => a.PickTask)
            .WithMany(t => t.Allocations)
            .HasForeignKey(a => a.PickTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PickTaskAllocation>()
            .HasOne(a => a.StockReservation)
            .WithMany()
            .HasForeignKey(a => a.StockReservationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTaskAllocation>()
            .HasOne(a => a.Voucher)
            .WithMany()
            .HasForeignKey(a => a.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTaskAllocation>()
            .HasOne(a => a.VoucherDetail)
            .WithMany()
            .HasForeignKey(a => a.VoucherDetailId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTaskAllocation>()
            .HasIndex(a => a.PickTaskId)
            .HasDatabaseName("IX_PickTaskAllocations_PickTaskId");

        modelBuilder.Entity<PickTaskAllocation>()
            .HasIndex(a => a.VoucherId)
            .HasDatabaseName("IX_PickTaskAllocations_VoucherId");

        modelBuilder.Entity<PickTaskAllocation>()
            .HasIndex(a => a.StockReservationId)
            .HasDatabaseName("IX_PickTaskAllocations_StockReservationId");

        modelBuilder.Entity<PickTaskScanLog>()
            .HasOne(l => l.PickTask)
            .WithMany()
            .HasForeignKey(l => l.PickTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PickTaskSerialAssignment>()
            .HasIndex(x => x.SerialNumberId)
            .IsUnique()
            .HasFilter("[VoidedAt] IS NULL AND [PostedAt] IS NULL");

        modelBuilder.Entity<PickTaskSerialAssignment>()
            .HasIndex(x => new { x.PickTaskId, x.PostedAt, x.VoidedAt })
            .HasDatabaseName("IX_PickTaskSerialAssignments_Task_Status");

        modelBuilder.Entity<PickTaskSerialAssignment>()
            .HasIndex(x => x.SerialReservationId)
            .HasDatabaseName("IX_PickTaskSerialAssignments_SerialReservationId");

        modelBuilder.Entity<PickTaskSerialAssignment>()
            .HasOne(x => x.PickTask)
            .WithMany()
            .HasForeignKey(x => x.PickTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PickTaskSerialAssignment>()
            .HasOne(x => x.Voucher)
            .WithMany()
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTaskSerialAssignment>()
            .HasOne(x => x.VoucherDetail)
            .WithMany()
            .HasForeignKey(x => x.VoucherDetailId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTaskSerialAssignment>()
            .HasOne(x => x.SerialNumber)
            .WithMany()
            .HasForeignKey(x => x.SerialNumberId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTaskSerialAssignment>()
            .HasOne(x => x.SerialReservation)
            .WithMany()
            .HasForeignKey(x => x.SerialReservationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LicensePlate>()
            .HasIndex(l => l.LpnCode)
            .IsUnique();

        modelBuilder.Entity<LicensePlate>()
            .HasIndex(l => new { l.WarehouseId, l.Status, l.CurrentLocationId, l.IsActive })
            .HasDatabaseName("IX_LicensePlates_Warehouse_Status_Location_Active");

        modelBuilder.Entity<LicensePlate>()
            .HasIndex(l => l.VoucherId)
            .HasDatabaseName("IX_LicensePlates_VoucherId");

        modelBuilder.Entity<LicensePlate>()
            .HasIndex(l => l.ParentLpnId)
            .HasDatabaseName("IX_LicensePlates_ParentLpnId");

        modelBuilder.Entity<LicensePlate>()
            .Property(l => l.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<LicensePlate>()
            .Property(l => l.Status)
            .HasDefaultValue(LpnStatusEnum.Created)
            .HasSentinel((LpnStatusEnum)0);

        modelBuilder.Entity<LicensePlate>()
            .Property(l => l.LpnType)
            .HasDefaultValue(LpnTypeEnum.Carton)
            .HasSentinel((LpnTypeEnum)0);

        modelBuilder.Entity<LicensePlate>()
            .ToTable(t => t.HasCheckConstraint("CK_LicensePlates_NoSelfParent", "[ParentLpnId] IS NULL OR [ParentLpnId] <> [LicensePlateId]"));

        modelBuilder.Entity<LicensePlate>()
            .HasOne(l => l.Voucher)
            .WithMany()
            .HasForeignKey(l => l.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LicensePlate>()
            .HasOne(l => l.VoucherDetail)
            .WithMany()
            .HasForeignKey(l => l.VoucherDetailId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LicensePlate>()
            .HasOne(l => l.CurrentLocation)
            .WithMany()
            .HasForeignKey(l => l.CurrentLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LicensePlate>()
            .HasOne(l => l.ParentLpn)
            .WithMany(l => l.ChildLpns)
            .HasForeignKey(l => l.ParentLpnId)
            .OnDelete(DeleteBehavior.NoAction);

        // Legacy FK kept for safe migration only. New code uses LicensePlateDetail.ItemId.
        modelBuilder.Entity<LicensePlate>()
            .HasOne(l => l.Item)
            .WithMany()
            .HasForeignKey(l => l.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        // Legacy FK kept for safe migration only. New code uses CurrentLocationId.
        modelBuilder.Entity<LicensePlate>()
            .HasOne(l => l.Location)
            .WithMany()
            .HasForeignKey(l => l.LocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LicensePlate>()
            .HasOne(l => l.Warehouse)
            .WithMany()
            .HasForeignKey(l => l.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LicensePlateDetail>()
            .HasIndex(d => d.LicensePlateId)
            .HasDatabaseName("IX_LicensePlateDetails_LicensePlateId");

        modelBuilder.Entity<LicensePlateDetail>()
            .HasIndex(d => new { d.LicensePlateId, d.ItemId, d.LotNumber, d.ExpiryDate })
            .HasDatabaseName("IX_LicensePlateDetails_Lpn_Item_Lot_Expiry");

        modelBuilder.Entity<LicensePlateDetail>()
            .HasIndex(d => new { d.ItemId, d.LotNumber, d.ExpiryDate })
            .HasDatabaseName("IX_LicensePlateDetails_Item_Lot_Expiry");

        modelBuilder.Entity<LicensePlateDetail>()
            .HasOne(d => d.LicensePlate)
            .WithMany(l => l.Details)
            .HasForeignKey(d => d.LicensePlateId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LicensePlateDetail>()
            .HasOne(d => d.Item)
            .WithMany()
            .HasForeignKey(d => d.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LicensePlateDetail>()
            .HasOne(d => d.VoucherDetail)
            .WithMany()
            .HasForeignKey(d => d.VoucherDetailId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<InventorySnapshotOutbox>()
            .HasIndex(o => new { o.Status, o.NextAttemptAt, o.CreatedAt })
            .HasDatabaseName("IX_InventorySnapshotOutbox_Status_NextAttempt_Created");

        modelBuilder.Entity<InventorySnapshotOutbox>()
            .HasIndex(o => o.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("IX_InventorySnapshotOutbox_IdempotencyKey");

        modelBuilder.Entity<InventorySnapshotOutbox>()
            .HasIndex(o => new { o.LicensePlateId, o.EventType, o.CreatedAt })
            .HasDatabaseName("IX_InventorySnapshotOutbox_Lpn_Event_Created");

        modelBuilder.Entity<InventorySnapshotOutbox>()
            .Property(o => o.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<InventorySnapshotOutbox>()
            .HasOne(o => o.LicensePlate)
            .WithMany()
            .HasForeignKey(o => o.LicensePlateId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<InventorySnapshotOutbox>()
            .HasOne(o => o.Warehouse)
            .WithMany()
            .HasForeignKey(o => o.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<InventorySnapshotOutbox>()
            .HasOne(o => o.SourceLocation)
            .WithMany()
            .HasForeignKey(o => o.SourceLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<InventorySnapshotOutbox>()
            .HasOne(o => o.DestinationLocation)
            .WithMany()
            .HasForeignKey(o => o.DestinationLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<InventoryReconciliationRun>()
            .HasIndex(r => new { r.WarehouseId, r.StartedAt })
            .HasDatabaseName("IX_InventoryReconciliationRuns_Warehouse_Started");

        modelBuilder.Entity<InventoryReconciliationRun>()
            .HasOne(r => r.Warehouse)
            .WithMany()
            .HasForeignKey(r => r.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<InventoryReconciliationIssue>()
            .HasIndex(i => new { i.WarehouseId, i.ItemId, i.LocationId, i.IsResolved, i.CreatedAt })
            .HasDatabaseName("IX_InventoryReconciliationIssues_Key_Open");

        modelBuilder.Entity<InventoryReconciliationIssue>()
            .HasOne(i => i.Run)
            .WithMany(r => r.Issues)
            .HasForeignKey(i => i.InventoryReconciliationRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InventoryReconciliationIssue>()
            .HasOne(i => i.Warehouse)
            .WithMany()
            .HasForeignKey(i => i.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<InventoryReconciliationIssue>()
            .HasOne(i => i.Item)
            .WithMany()
            .HasForeignKey(i => i.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<InventoryReconciliationIssue>()
            .HasOne(i => i.Location)
            .WithMany()
            .HasForeignKey(i => i.LocationId)
            .OnDelete(DeleteBehavior.NoAction);

        // P0-5: serial code unique theo (WarehouseId, ItemId, SerialCode) thay vì global.
        // Cùng vendor có thể gửi cùng SN sang 2 kho/owner khác — insert thứ 2 không còn bị reject sai.
        modelBuilder.Entity<SerialNumber>()
            .HasIndex(s => new { s.WarehouseId, s.ItemId, s.SerialCode })
            .IsUnique();

        modelBuilder.Entity<SerialNumber>()
            .HasIndex(s => new { s.WarehouseId, s.ItemId, s.Status })
            .HasDatabaseName("IX_SerialNumbers_Warehouse_Item_Status");

        modelBuilder.Entity<SerialNumber>()
            .HasIndex(s => new { s.WarehouseId, s.ItemId, s.LocationId, s.Status, s.HoldStatus })
            .HasDatabaseName("IX_SerialNumbers_Warehouse_Item_Location_Status_Hold");

        modelBuilder.Entity<SerialNumber>()
            .HasIndex(s => new { s.LicensePlateId, s.Status })
            .HasDatabaseName("IX_SerialNumbers_LicensePlate_Status");

        modelBuilder.Entity<SerialNumber>()
            .HasIndex(s => new { s.VoucherId, s.VoucherDetailId, s.Status })
            .HasDatabaseName("IX_SerialNumbers_Voucher_Status");

        modelBuilder.Entity<SerialNumber>()
            .HasOne(s => s.Warehouse)
            .WithMany()
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SerialNumber>()
            .HasOne(s => s.Item)
            .WithMany()
            .HasForeignKey(s => s.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SerialNumber>()
            .HasOne(s => s.Location)
            .WithMany()
            .HasForeignKey(s => s.LocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SerialNumber>()
            .HasOne(s => s.Voucher)
            .WithMany()
            .HasForeignKey(s => s.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SerialNumber>()
            .HasOne(s => s.ConsumedVoucher)
            .WithMany()
            .HasForeignKey(s => s.ConsumedVoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SerialNumber>()
            .HasOne(s => s.ConsumedPickTask)
            .WithMany(t => t.ConsumedSerials)
            .HasForeignKey(s => s.ConsumedPickTaskId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SerialNumber>()
            .HasIndex(s => s.ConsumedPickTaskId)
            .HasDatabaseName("IX_SerialNumbers_ConsumedPickTaskId");

        modelBuilder.Entity<SerialNumber>()
            .HasOne(s => s.VoucherDetail)
            .WithMany()
            .HasForeignKey(s => s.VoucherDetailId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SerialNumber>()
            .HasOne(s => s.LicensePlate)
            .WithMany()
            .HasForeignKey(s => s.LicensePlateId)
            .OnDelete(DeleteBehavior.SetNull);

        // RowVersion for optimistic concurrency
        modelBuilder.Entity<SerialNumber>()
            .Property(s => s.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<SerialNumber>()
            .Property(s => s.HoldStatus)
            .HasDefaultValue(InventoryHoldStatusEnum.Available)
            .HasSentinel((InventoryHoldStatusEnum)0);

        modelBuilder.Entity<SerialReservation>()
            .HasIndex(r => r.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("IX_SerialReservations_IdempotencyKey");

        modelBuilder.Entity<SerialReservation>()
            .HasIndex(r => r.SerialNumberId)
            .IsUnique()
            .HasFilter("[Status] IN (1, 2)")
            .HasDatabaseName("UX_SerialReservations_ActiveSerial");

        modelBuilder.Entity<SerialReservation>()
            .HasIndex(r => new { r.StockReservationId, r.Status })
            .HasDatabaseName("IX_SerialReservations_StockReservation_Status");

        modelBuilder.Entity<SerialReservation>()
            .HasIndex(r => new { r.PickTaskId, r.Status })
            .HasDatabaseName("IX_SerialReservations_PickTask_Status");

        modelBuilder.Entity<SerialReservation>()
            .HasIndex(r => new { r.VoucherId, r.VoucherDetailId, r.Status })
            .HasDatabaseName("IX_SerialReservations_Voucher_Status");

        modelBuilder.Entity<SerialReservation>()
            .HasIndex(r => new { r.WarehouseId, r.ItemId, r.LocationId, r.Status })
            .HasDatabaseName("IX_SerialReservations_Warehouse_Item_Location_Status");

        modelBuilder.Entity<SerialReservation>()
            .Property(r => r.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<SerialReservation>()
            .Property(r => r.HoldStatus)
            .HasDefaultValue(InventoryHoldStatusEnum.Available)
            .HasSentinel((InventoryHoldStatusEnum)0);

        modelBuilder.Entity<SerialReservation>()
            .Property(r => r.Status)
            .HasDefaultValue(SerialReservationStatusEnum.Reserved)
            .HasSentinel((SerialReservationStatusEnum)0);

        modelBuilder.Entity<SerialReservation>()
            .HasOne(r => r.SerialNumber)
            .WithMany()
            .HasForeignKey(r => r.SerialNumberId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SerialReservation>()
            .HasOne(r => r.StockReservation)
            .WithMany()
            .HasForeignKey(r => r.StockReservationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SerialReservation>()
            .HasOne(r => r.PickTask)
            .WithMany()
            .HasForeignKey(r => r.PickTaskId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SerialReservation>()
            .HasOne(r => r.Voucher)
            .WithMany()
            .HasForeignKey(r => r.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SerialReservation>()
            .HasOne(r => r.VoucherDetail)
            .WithMany()
            .HasForeignKey(r => r.VoucherDetailId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SerialReservation>()
            .HasOne(r => r.Warehouse)
            .WithMany()
            .HasForeignKey(r => r.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SerialReservation>()
            .HasOne(r => r.Item)
            .WithMany()
            .HasForeignKey(r => r.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SerialReservation>()
            .HasOne(r => r.Location)
            .WithMany()
            .HasForeignKey(r => r.LocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SerialReservation>()
            .HasOne(r => r.LicensePlate)
            .WithMany()
            .HasForeignKey(r => r.LicensePlateId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SerialInventoryOperation>()
            .HasIndex(o => o.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("IX_SerialInventoryOperations_IdempotencyKey");

        modelBuilder.Entity<SerialInventoryOperation>()
            .HasIndex(o => new { o.OperationType, o.ReferenceType, o.ReferenceId })
            .HasDatabaseName("IX_SerialInventoryOperations_Operation_Reference");

        modelBuilder.Entity<SerialInventoryOperation>()
            .Property(o => o.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<SerialInventoryOperation>()
            .HasOne(o => o.SerialNumber)
            .WithMany()
            .HasForeignKey(o => o.SerialNumberId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SerialInventoryOperation>()
            .HasOne(o => o.SerialReservation)
            .WithMany()
            .HasForeignKey(o => o.SerialReservationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ShippingHandoverLog>()
            .HasIndex(x => new { x.WarehouseId, x.HandedOverAt })
            .HasDatabaseName("IX_ShippingHandoverLogs_Warehouse_Date");

        modelBuilder.Entity<ShippingHandoverLog>()
            .HasIndex(x => x.VoucherId);

        // P2-R2-5: chặn duplicate handover log khi 2 Depart song song trên cùng Voucher+ShipmentLoad.
        modelBuilder.Entity<ShippingHandoverLog>()
            .HasIndex(x => new { x.VoucherId, x.ShipmentLoadId })
            .IsUnique()
            .HasFilter("[ShipmentLoadId] IS NOT NULL")
            .HasDatabaseName("UX_ShippingHandoverLogs_Voucher_ShipmentLoad");

        modelBuilder.Entity<ShippingHandoverLog>()
            .HasOne(x => x.Voucher)
            .WithMany()
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ShippingHandoverLog>()
            .HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<OutboundPackage>()
            .HasIndex(x => x.PackageCode)
            .IsUnique();

        modelBuilder.Entity<OutboundPackage>()
            .HasIndex(x => new { x.VoucherId, x.PackedAt })
            .HasDatabaseName("IX_OutboundPackages_Voucher_Date");

        modelBuilder.Entity<OutboundPackage>()
            .HasIndex(x => new { x.WarehouseId, x.PackedAt })
            .HasDatabaseName("IX_OutboundPackages_Warehouse_Date");

        modelBuilder.Entity<OutboundPackage>()
            .HasOne(x => x.Voucher)
            .WithMany(v => v.Packages)
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<OutboundPackage>()
            .HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<OutboundPackage>()
            .HasIndex(x => x.ShipmentLoadId)
            .HasDatabaseName("IX_OutboundPackages_ShipmentLoad");

        modelBuilder.Entity<OutboundPackage>()
            .HasOne(x => x.CatchWeightUom)
            .WithMany()
            .HasForeignKey(x => x.CatchWeightUomId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<OutboundPackage>()
            .HasOne(x => x.ShipmentLoad)
            .WithMany()
            .HasForeignKey(x => x.ShipmentLoadId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ShippingHandoverLog>()
            .HasOne(x => x.ShipmentLoad)
            .WithMany()
            .HasForeignKey(x => x.ShipmentLoadId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CatchWeightEntry>()
            .HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("UX_CatchWeightEntries_IdempotencyKey");

        modelBuilder.Entity<CatchWeightEntry>()
            .HasIndex(x => new { x.VoucherDetailId, x.Status })
            .HasDatabaseName("IX_CatchWeightEntries_VoucherDetail_Status");

        modelBuilder.Entity<CatchWeightEntry>()
            .HasIndex(x => new { x.OutboundPackageId, x.Status })
            .HasDatabaseName("IX_CatchWeightEntries_Package_Status");

        modelBuilder.Entity<CatchWeightEntry>()
            .HasIndex(x => new { x.ItemId, x.WarehouseId, x.CapturedAt })
            .HasDatabaseName("IX_CatchWeightEntries_Item_Warehouse_Date");

        modelBuilder.Entity<CatchWeightEntry>()
            .Property(x => x.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<CatchWeightEntry>()
            .HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CatchWeightEntry>()
            .HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CatchWeightEntry>()
            .HasOne(x => x.Voucher)
            .WithMany()
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CatchWeightEntry>()
            .HasOne(x => x.VoucherDetail)
            .WithMany()
            .HasForeignKey(x => x.VoucherDetailId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CatchWeightEntry>()
            .HasOne(x => x.LicensePlate)
            .WithMany()
            .HasForeignKey(x => x.LicensePlateId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CatchWeightEntry>()
            .HasOne(x => x.LicensePlateDetail)
            .WithMany()
            .HasForeignKey(x => x.LicensePlateDetailId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CatchWeightEntry>()
            .HasOne(x => x.OutboundPackage)
            .WithMany()
            .HasForeignKey(x => x.OutboundPackageId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CatchWeightEntry>()
            .HasOne(x => x.PickTask)
            .WithMany()
            .HasForeignKey(x => x.PickTaskId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CatchWeightEntry>()
            .HasOne(x => x.SerialNumber)
            .WithMany()
            .HasForeignKey(x => x.SerialNumberId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CatchWeightEntry>()
            .HasOne(x => x.WeightUom)
            .WithMany()
            .HasForeignKey(x => x.WeightUomId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ShipmentLoad>()
            .HasIndex(x => x.LoadCode)
            .IsUnique()
            .HasDatabaseName("UX_ShipmentLoads_LoadCode");

        modelBuilder.Entity<ShipmentLoad>()
            .HasIndex(x => new { x.WarehouseId, x.Status, x.PlannedDepartureAt })
            .HasDatabaseName("IX_ShipmentLoads_Warehouse_Status_Planned");

        modelBuilder.Entity<ShipmentLoad>()
            .HasIndex(x => x.ManifestCode)
            .HasFilter("[ManifestCode] IS NOT NULL")
            .HasDatabaseName("IX_ShipmentLoads_ManifestCode");

        modelBuilder.Entity<ShipmentLoad>()
            .Property(x => x.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<ShipmentLoad>()
            .HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ShipmentLoad>()
            .HasOne(x => x.Trailer)
            .WithMany()
            .HasForeignKey(x => x.TrailerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ShipmentLoad>()
            .HasOne(x => x.YardVisit)
            .WithMany()
            .HasForeignKey(x => x.YardVisitId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ShipmentLoadVoucher>()
            .HasIndex(x => x.VoucherId)
            .IsUnique()
            .HasFilter("[RemovedAt] IS NULL")
            .HasDatabaseName("UX_ShipmentLoadVouchers_Active_Voucher");

        modelBuilder.Entity<ShipmentLoadVoucher>()
            .HasIndex(x => new { x.ShipmentLoadId, x.Sequence })
            .HasDatabaseName("IX_ShipmentLoadVouchers_Load_Sequence");

        modelBuilder.Entity<ShipmentLoadVoucher>()
            .Property(x => x.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<ShipmentLoadVoucher>()
            .HasOne(x => x.ShipmentLoad)
            .WithMany(x => x.Vouchers)
            .HasForeignKey(x => x.ShipmentLoadId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShipmentLoadVoucher>()
            .HasOne(x => x.Voucher)
            .WithMany()
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ShipmentLoadPackage>()
            .HasIndex(x => x.OutboundPackageId)
            .IsUnique()
            .HasFilter("[RemovedAt] IS NULL")
            .HasDatabaseName("UX_ShipmentLoadPackages_Active_Package");

        modelBuilder.Entity<ShipmentLoadPackage>()
            .HasIndex(x => new { x.ShipmentLoadId, x.IsLoaded })
            .HasDatabaseName("IX_ShipmentLoadPackages_Load_Loaded");

        modelBuilder.Entity<ShipmentLoadPackage>()
            .Property(x => x.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<ShipmentLoadPackage>()
            .HasOne(x => x.ShipmentLoad)
            .WithMany(x => x.Packages)
            .HasForeignKey(x => x.ShipmentLoadId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShipmentLoadPackage>()
            .HasOne(x => x.OutboundPackage)
            .WithMany()
            .HasForeignKey(x => x.OutboundPackageId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CarrierConnector>()
            .HasIndex(x => new { x.WarehouseId, x.CarrierCode })
            .IsUnique()
            .HasDatabaseName("UX_CarrierConnectors_Warehouse_Code");

        modelBuilder.Entity<CarrierConnector>()
            .HasIndex(x => new { x.WarehouseId, x.IsActive })
            .HasDatabaseName("IX_CarrierConnectors_Warehouse_Active");

        modelBuilder.Entity<CarrierConnector>()
            .Property(x => x.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<CarrierConnector>()
            .HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CarrierShipment>()
            .HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("UX_CarrierShipments_IdempotencyKey");

        modelBuilder.Entity<CarrierShipment>()
            .HasIndex(x => x.CorrelationId)
            .IsUnique()
            .HasDatabaseName("UX_CarrierShipments_CorrelationId");

        modelBuilder.Entity<CarrierShipment>()
            .HasIndex(x => new { x.WarehouseId, x.Status, x.CreatedAt })
            .HasDatabaseName("IX_CarrierShipments_Warehouse_Status_Created");

        modelBuilder.Entity<CarrierShipment>()
            .HasIndex(x => new { x.VoucherId, x.Status })
            .HasDatabaseName("IX_CarrierShipments_Voucher_Status");

        modelBuilder.Entity<CarrierShipment>()
            .HasIndex(x => new { x.OutboundPackageId, x.Status })
            .HasDatabaseName("IX_CarrierShipments_Package_Status");

        modelBuilder.Entity<CarrierShipment>()
            .Property(x => x.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<CarrierShipment>()
            .HasOne(x => x.CarrierConnector)
            .WithMany()
            .HasForeignKey(x => x.CarrierConnectorId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CarrierShipment>()
            .HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CarrierShipment>()
            .HasOne(x => x.OwnerPartner)
            .WithMany()
            .HasForeignKey(x => x.OwnerPartnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CarrierShipment>()
            .HasOne(x => x.Voucher)
            .WithMany()
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CarrierShipment>()
            .HasOne(x => x.OutboundPackage)
            .WithMany()
            .HasForeignKey(x => x.OutboundPackageId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CarrierShipment>()
            .HasOne(x => x.ShipmentLoad)
            .WithMany()
            .HasForeignKey(x => x.ShipmentLoadId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CarrierShipmentEvent>()
            .HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("UX_CarrierShipmentEvents_IdempotencyKey");

        modelBuilder.Entity<CarrierShipmentEvent>()
            .HasIndex(x => new { x.CarrierShipmentId, x.EventAt })
            .HasDatabaseName("IX_CarrierShipmentEvents_Shipment_Date");

        modelBuilder.Entity<CarrierShipmentEvent>()
            .HasOne(x => x.CarrierShipment)
            .WithMany(x => x.Events)
            .HasForeignKey(x => x.CarrierShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DockAppointment>()
            .HasIndex(x => x.AppointmentCode)
            .IsUnique()
            .HasDatabaseName("UX_DockAppointments_Code");

        modelBuilder.Entity<DockAppointment>()
            .HasIndex(x => new { x.WarehouseId, x.DockDoor, x.PlannedStartAt, x.PlannedEndAt, x.Status })
            .HasDatabaseName("IX_DockAppointments_Door_Window_Status");

        modelBuilder.Entity<DockAppointment>()
            .HasIndex(x => new { x.OwnerPartnerId, x.WarehouseId, x.PlannedStartAt })
            .HasDatabaseName("IX_DockAppointments_Owner_Warehouse_Start");

        modelBuilder.Entity<DockAppointment>()
            .Property(x => x.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<DockAppointment>()
            .HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<DockAppointment>()
            .HasOne(x => x.OwnerPartner)
            .WithMany()
            .HasForeignKey(x => x.OwnerPartnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<DockAppointment>()
            .HasOne(x => x.Voucher)
            .WithMany()
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<DockAppointment>()
            .HasOne(x => x.ShipmentLoad)
            .WithMany()
            .HasForeignKey(x => x.ShipmentLoadId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ThreePlBillingRate>()
            .HasIndex(x => new { x.WarehouseId, x.OwnerPartnerId, x.ChargeType, x.IsActive, x.EffectiveFrom })
            .HasDatabaseName("IX_ThreePlBillingRates_Match");

        modelBuilder.Entity<ThreePlBillingRate>()
            .Property(x => x.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<ThreePlBillingRate>()
            .HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ThreePlBillingRate>()
            .HasOne(x => x.OwnerPartner)
            .WithMany()
            .HasForeignKey(x => x.OwnerPartnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ThreePlBillingRun>()
            .HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("UX_ThreePlBillingRuns_IdempotencyKey");

        modelBuilder.Entity<ThreePlBillingRun>()
            .HasIndex(x => new { x.WarehouseId, x.OwnerPartnerId, x.PeriodFrom, x.PeriodTo, x.Status })
            .HasDatabaseName("IX_ThreePlBillingRuns_Owner_Period_Status");

        modelBuilder.Entity<ThreePlBillingRun>()
            .Property(x => x.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<ThreePlBillingRun>()
            .HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ThreePlBillingRun>()
            .HasOne(x => x.OwnerPartner)
            .WithMany()
            .HasForeignKey(x => x.OwnerPartnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ThreePlBillingCharge>()
            .HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("UX_ThreePlBillingCharges_IdempotencyKey");

        modelBuilder.Entity<ThreePlBillingCharge>()
            .HasIndex(x => new { x.OwnerPartnerId, x.WarehouseId, x.ChargeType, x.CreatedAt })
            .HasDatabaseName("IX_ThreePlBillingCharges_Owner_Type_Date");

        modelBuilder.Entity<ThreePlBillingCharge>()
            .HasOne(x => x.BillingRun)
            .WithMany(x => x.Charges)
            .HasForeignKey(x => x.ThreePlBillingRunId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ThreePlBillingCharge>()
            .HasOne(x => x.BillingRate)
            .WithMany()
            .HasForeignKey(x => x.ThreePlBillingRateId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ThreePlBillingCharge>()
            .HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ThreePlBillingCharge>()
            .HasOne(x => x.OwnerPartner)
            .WithMany()
            .HasForeignKey(x => x.OwnerPartnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ThreePlContract>()
            .HasIndex(x => x.ContractCode)
            .IsUnique()
            .HasDatabaseName("UX_ThreePlContracts_Code");

        modelBuilder.Entity<ThreePlContract>()
            .HasIndex(x => new { x.WarehouseId, x.OwnerPartnerId, x.Status, x.EffectiveFrom, x.EffectiveTo })
            .HasDatabaseName("IX_ThreePlContracts_Scope_Effective");

        modelBuilder.Entity<ThreePlContract>()
            .Property(x => x.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<ThreePlContract>()
            .HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ThreePlContract>()
            .HasOne(x => x.OwnerPartner)
            .WithMany()
            .HasForeignKey(x => x.OwnerPartnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ThreePlContractRate>()
            .HasIndex(x => new { x.ThreePlContractId, x.ChargeType, x.ChargeUnit, x.TierFromQty, x.EffectiveFrom, x.IsActive })
            .HasDatabaseName("IX_ThreePlContractRates_Match");

        modelBuilder.Entity<ThreePlContractRate>()
            .HasOne(x => x.Contract)
            .WithMany(x => x.Rates)
            .HasForeignKey(x => x.ThreePlContractId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ThreePlInvoice>()
            .HasIndex(x => x.InvoiceCode)
            .IsUnique()
            .HasDatabaseName("UX_ThreePlInvoices_Code");

        modelBuilder.Entity<ThreePlInvoice>()
            .HasIndex(x => x.ApiPublicId)
            .IsUnique()
            .HasDatabaseName("UX_ThreePlInvoices_ApiPublicId");

        modelBuilder.Entity<ThreePlInvoice>()
            .HasIndex(x => new { x.WarehouseId, x.OwnerPartnerId, x.PeriodFrom, x.PeriodTo, x.Status })
            .HasDatabaseName("IX_ThreePlInvoices_Scope_Period_Status");

        modelBuilder.Entity<ThreePlInvoice>()
            .Property(x => x.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<ThreePlInvoice>()
            .HasOne(x => x.BillingRun)
            .WithMany()
            .HasForeignKey(x => x.ThreePlBillingRunId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ThreePlInvoice>()
            .HasOne(x => x.Contract)
            .WithMany()
            .HasForeignKey(x => x.ThreePlContractId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ThreePlInvoice>()
            .HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ThreePlInvoice>()
            .HasOne(x => x.OwnerPartner)
            .WithMany()
            .HasForeignKey(x => x.OwnerPartnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ThreePlInvoiceLine>()
            .HasIndex(x => new { x.ThreePlInvoiceId, x.ChargeType })
            .HasDatabaseName("IX_ThreePlInvoiceLines_Invoice_Type");

        modelBuilder.Entity<ThreePlInvoiceLine>()
            .HasOne(x => x.Invoice)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.ThreePlInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ThreePlInvoiceLine>()
            .HasOne(x => x.BillingCharge)
            .WithMany()
            .HasForeignKey(x => x.ThreePlBillingChargeId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ThreePlDispute>()
            .HasIndex(x => new { x.OwnerPartnerId, x.Status, x.OpenedAt })
            .HasDatabaseName("IX_ThreePlDisputes_Owner_Status_Date");

        modelBuilder.Entity<ThreePlDispute>()
            .HasOne(x => x.InvoiceLine)
            .WithMany()
            .HasForeignKey(x => x.ThreePlInvoiceLineId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ThreePlDispute>()
            .HasOne(x => x.OwnerPartner)
            .WithMany()
            .HasForeignKey(x => x.OwnerPartnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<MheSystem>()
            .HasIndex(x => new { x.WarehouseId, x.SystemCode })
            .IsUnique()
            .HasDatabaseName("UX_MheSystems_Warehouse_Code");

        modelBuilder.Entity<MheSystem>()
            .Property(x => x.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<MheCommand>()
            .HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("UX_MheCommands_IdempotencyKey");

        modelBuilder.Entity<MheCommand>()
            .HasIndex(x => x.CorrelationId)
            .IsUnique()
            .HasDatabaseName("UX_MheCommands_CorrelationId");

        modelBuilder.Entity<MheCommand>()
            .HasIndex(x => new { x.WarehouseId, x.OwnerPartnerId, x.Status, x.CreatedAt })
            .HasDatabaseName("IX_MheCommands_Warehouse_Owner_Status_Date");

        modelBuilder.Entity<MheCommand>()
            .Property(x => x.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<MheMissionEvent>()
            .HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("UX_MheMissionEvents_IdempotencyKey");

        modelBuilder.Entity<MheMissionEvent>()
            .HasOne(x => x.MheCommand)
            .WithMany(x => x.MissionEvents)
            .HasForeignKey(x => x.MheCommandId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PartnerLabelTemplate>()
            .HasIndex(t => new { t.PartnerId, t.LabelPurpose, t.IsActive })
            .HasDatabaseName("IX_PartnerLabelTemplates_Partner_Purpose_Active");

        modelBuilder.Entity<PartnerLabelTemplate>()
            .HasIndex(t => new { t.LabelPurpose, t.IsDefault, t.IsActive })
            .HasDatabaseName("IX_PartnerLabelTemplates_Default_Purpose");

        modelBuilder.Entity<PartnerLabelTemplate>()
            .HasOne(t => t.Partner)
            .WithMany()
            .HasForeignKey(t => t.PartnerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PartnerLabelTemplate>()
            .Property(t => t.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<PartnerLabelTemplate>().HasData(
            new
            {
                PartnerLabelTemplateId = 1L,
                PartnerId = (int?)null,
                LabelPurpose = LabelPurposeEnum.OutboundVoucher,
                TemplateName = "Mẫu mặc định cho phiếu xuất",
                LabelSize = "50x30",
                CodeType = "barcode",
                HeaderTemplate = "{TenKhachHang}",
                BodyTemplate = "Mã khách: {MaHangKhach}\nTên hàng: {TenHangKhach}\nPhiếu: {MaPhieu}\nSố lượng: {SoLuong}",
                FooterTemplate = "In lúc {NgayIn}",
                IsDefault = true,
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = new DateTime(2026, 5, 1, 0, 0, 0)
            },
            new
            {
                PartnerLabelTemplateId = 2L,
                PartnerId = (int?)null,
                LabelPurpose = LabelPurposeEnum.OutboundPackage,
                TemplateName = "Mẫu mặc định cho kiện xuất",
                LabelSize = "100x50",
                CodeType = "barcode",
                HeaderTemplate = "{TenKhachHang}",
                BodyTemplate = "Kiện: {MaKien}\nPhiếu: {MaPhieu}\nMã khách: {MaHangKhach}\nTên hàng: {TenHangKhach}\nSố lượng: {SoLuong}",
                FooterTemplate = "Vận đơn: {MaVanDon}",
                IsDefault = true,
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = new DateTime(2026, 5, 1, 0, 0, 0)
            });

        modelBuilder.Entity<PartnerItemLabelRule>()
            .HasIndex(r => new { r.PartnerId, r.ItemId })
            .IsUnique()
            .HasDatabaseName("UX_PartnerItemLabelRules_Partner_Item");

        modelBuilder.Entity<PartnerItemLabelRule>()
            .HasOne(r => r.Partner)
            .WithMany()
            .HasForeignKey(r => r.PartnerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PartnerItemLabelRule>()
            .HasOne(r => r.Item)
            .WithMany()
            .HasForeignKey(r => r.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PartnerItemLabelRule>()
            .Property(r => r.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<LabelPrintJob>()
            .HasIndex(j => j.JobCode)
            .IsUnique();

        modelBuilder.Entity<LabelPrintJob>()
            .HasIndex(j => new { j.PartnerId, j.RequestedAt })
            .HasDatabaseName("IX_LabelPrintJobs_Partner_Date");

        modelBuilder.Entity<LabelPrintJob>()
            .HasIndex(j => new { j.VoucherId, j.OutboundPackageId })
            .HasDatabaseName("IX_LabelPrintJobs_Voucher_Package");

        modelBuilder.Entity<LabelPrintJob>()
            .HasIndex(j => new { j.ShipmentLoadId, j.DocumentType, j.RequestedAt })
            .HasDatabaseName("IX_LabelPrintJobs_Load_Document_Date");

        modelBuilder.Entity<LabelPrintJob>()
            .HasIndex(j => new { j.DocumentType, j.DocumentNumber })
            .HasFilter("[DocumentType] IS NOT NULL AND [DocumentNumber] IS NOT NULL")
            .HasDatabaseName("IX_LabelPrintJobs_Document_Number");

        modelBuilder.Entity<LabelPrintJob>()
            .HasOne(j => j.Partner)
            .WithMany()
            .HasForeignKey(j => j.PartnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LabelPrintJob>()
            .HasOne(j => j.Voucher)
            .WithMany()
            .HasForeignKey(j => j.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LabelPrintJob>()
            .HasOne(j => j.OutboundPackage)
            .WithMany()
            .HasForeignKey(j => j.OutboundPackageId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LabelPrintJob>()
            .HasOne(j => j.ShipmentLoad)
            .WithMany()
            .HasForeignKey(j => j.ShipmentLoadId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LabelPrintJob>()
            .HasOne(j => j.ShippingHandoverLog)
            .WithMany()
            .HasForeignKey(j => j.ShippingHandoverLogId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LabelPrintJob>()
            .HasOne(j => j.CarrierShipment)
            .WithMany()
            .HasForeignKey(j => j.CarrierShipmentId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LabelPrintJob>()
            .HasOne(j => j.PartnerLabelTemplate)
            .WithMany(t => t.PrintJobs)
            .HasForeignKey(j => j.PartnerLabelTemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<LabelPrintJobLine>()
            .HasIndex(l => l.LabelPrintJobId)
            .HasDatabaseName("IX_LabelPrintJobLines_Job");

        modelBuilder.Entity<LabelPrintJobLine>()
            .HasOne(l => l.LabelPrintJob)
            .WithMany(j => j.Lines)
            .HasForeignKey(l => l.LabelPrintJobId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LabelPrintJobLine>()
            .HasOne(l => l.VoucherDetail)
            .WithMany()
            .HasForeignKey(l => l.VoucherDetailId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LabelPrintJobLine>()
            .HasOne(l => l.OutboundPackage)
            .WithMany()
            .HasForeignKey(l => l.OutboundPackageId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LabelPrintJobLine>()
            .HasOne(l => l.Item)
            .WithMany()
            .HasForeignKey(l => l.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<VasWorkOrder>()
            .HasIndex(v => v.WorkOrderCode)
            .IsUnique();

        modelBuilder.Entity<VasWorkOrder>()
            .HasIndex(v => new { v.WarehouseId, v.Status, v.CreatedAt })
            .HasDatabaseName("IX_VasWorkOrders_Warehouse_Status_Date");

        modelBuilder.Entity<VasWorkOrder>()
            .HasIndex(v => new { v.PartnerId, v.CreatedAt })
            .HasDatabaseName("IX_VasWorkOrders_Partner_Date");

        modelBuilder.Entity<VasWorkOrder>()
            .HasOne(v => v.Warehouse)
            .WithMany()
            .HasForeignKey(v => v.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<VasWorkOrder>()
            .HasOne(v => v.Partner)
            .WithMany()
            .HasForeignKey(v => v.PartnerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<VasWorkOrder>()
            .HasOne(v => v.Voucher)
            .WithMany()
            .HasForeignKey(v => v.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<VasWorkOrder>()
            .HasOne(v => v.PrimaryItem)
            .WithMany()
            .HasForeignKey(v => v.PrimaryItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<VasWorkOrder>()
            .Property(v => v.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<VasOperation>()
            .HasIndex(o => new { o.VasWorkOrderId, o.StepNumber })
            .IsUnique()
            .HasDatabaseName("UX_VasOperations_WorkOrder_Step");

        modelBuilder.Entity<VasOperation>()
            .HasOne(o => o.VasWorkOrder)
            .WithMany(v => v.Operations)
            .HasForeignKey(o => o.VasWorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VasOperation>()
            .Property(o => o.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<VasMaterialLine>()
            .HasIndex(l => new { l.VasWorkOrderId, l.MaterialItemId, l.Status })
            .HasDatabaseName("IX_VasMaterialLines_WorkOrder_Item_Status");

        modelBuilder.Entity<VasMaterialLine>()
            .HasIndex(l => l.SourceItemLocationId)
            .HasDatabaseName("IX_VasMaterialLines_SourceItemLocationId");

        modelBuilder.Entity<VasMaterialLine>()
            .HasOne(l => l.VasWorkOrder)
            .WithMany(v => v.MaterialLines)
            .HasForeignKey(l => l.VasWorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VasMaterialLine>()
            .HasOne(l => l.MaterialItem)
            .WithMany()
            .HasForeignKey(l => l.MaterialItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<VasMaterialLine>()
            .HasOne(l => l.SourceLocation)
            .WithMany()
            .HasForeignKey(l => l.SourceLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<VasMaterialLine>()
            .HasOne(l => l.SourceItemLocation)
            .WithMany()
            .HasForeignKey(l => l.SourceItemLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<VasMaterialLine>()
            .Property(l => l.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<OperationExceptionCase>()
            .HasIndex(c => c.ExceptionKey)
            .IsUnique();

        modelBuilder.Entity<OperationExceptionCase>()
            .HasIndex(c => new { c.WarehouseId, c.Status, c.CategoryKey });

        modelBuilder.Entity<OperationExceptionCase>()
            .HasOne(c => c.Warehouse)
            .WithMany()
            .HasForeignKey(c => c.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        // AiOcrLog
        modelBuilder.Entity<AiOcrLog>()
            .HasIndex(a => a.VoucherId)
            .HasDatabaseName("IX_AiOcrLogs_VoucherId");

        modelBuilder.Entity<AiOcrLog>()
            .Property(a => a.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<AiOcrLog>()
            .HasOne(a => a.Voucher)
            .WithMany()
            .HasForeignKey(a => a.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        // AiOcrAdjustment
        modelBuilder.Entity<AiOcrAdjustment>()
            .HasOne(a => a.Item)
            .WithMany()
            .HasForeignKey(a => a.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<AiOcrAdjustment>()
            .Property(a => a.RowVersion)
            .IsRowVersion();

        // ═══ ENTERPRISE UPGRADE: Quality Inspection ═══
        modelBuilder.Entity<QualityInspection>()
            .HasOne(qi => qi.Voucher)
            .WithMany()
            .HasForeignKey(qi => qi.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<QualityInspection>()
            .HasOne(qi => qi.VoucherDetail)
            .WithMany()
            .HasForeignKey(qi => qi.VoucherDetailId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<QualityInspection>()
            .HasOne(qi => qi.Item)
            .WithMany()
            .HasForeignKey(qi => qi.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<QualityInspection>()
            .HasOne(qi => qi.Warehouse)
            .WithMany()
            .HasForeignKey(qi => qi.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<QualityInspection>()
            .HasIndex(qi => new { qi.VoucherId, qi.ItemId });

        modelBuilder.Entity<QualityInspection>()
            .HasIndex(qi => qi.VoucherDetailId)
            .IsUnique()
            .HasFilter("[VoucherDetailId] IS NOT NULL");

        // ═══ ENTERPRISE UPGRADE: Labor Standards ═══
        modelBuilder.Entity<LaborStandard>()
            .HasOne(ls => ls.Warehouse)
            .WithMany()
            .HasForeignKey(ls => ls.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LaborStandard>()
            .HasOne(ls => ls.Zone)
            .WithMany()
            .HasForeignKey(ls => ls.ZoneId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LaborStandard>()
            .HasIndex(ls => new { ls.TaskType, ls.WarehouseId, ls.ZoneId, ls.ItemClass, ls.EffectiveFrom })
            .HasDatabaseName("IX_LaborStandards_Match");

        modelBuilder.Entity<LaborActivity>()
            .HasIndex(x => x.ActivityCode)
            .IsUnique()
            .HasDatabaseName("UX_LaborActivities_Code");

        modelBuilder.Entity<LaborActivity>()
            .HasIndex(x => new { x.WarehouseId, x.ZoneId, x.TaskType, x.Status, x.StartedAt })
            .HasDatabaseName("IX_LaborActivities_Warehouse_Zone_Task_Status");

        modelBuilder.Entity<LaborActivity>()
            .HasIndex(x => new { x.UserName, x.ShiftCode, x.StartedAt })
            .HasDatabaseName("IX_LaborActivities_User_Shift_Start");

        modelBuilder.Entity<LaborActivity>()
            .HasIndex(x => new { x.TaskSourceType, x.TaskSourceId })
            .IsUnique()
            .HasFilter("[TaskSourceId] IS NOT NULL")
            .HasDatabaseName("UX_LaborActivities_Source");

        modelBuilder.Entity<LaborActivity>()
            .HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LaborActivity>()
            .HasOne(x => x.Zone)
            .WithMany()
            .HasForeignKey(x => x.ZoneId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LaborActivity>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LaborActivity>()
            .HasOne(x => x.OwnerPartner)
            .WithMany()
            .HasForeignKey(x => x.OwnerPartnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LaborExceptionReview>()
            .HasIndex(x => new { x.LaborActivityId, x.Status })
            .HasDatabaseName("IX_LaborExceptionReviews_Activity_Status");

        modelBuilder.Entity<LaborExceptionReview>()
            .HasOne(x => x.LaborActivity)
            .WithMany(x => x.ExceptionReviews)
            .HasForeignKey(x => x.LaborActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        // ═══ ENTERPRISE UPGRADE: Currency Rates ═══
        modelBuilder.Entity<CurrencyRate>()
            .HasIndex(cr => new { cr.FromCurrency, cr.ToCurrency, cr.EffectiveDate })
            .IsUnique();

        // ═══ ENTERPRISE UPGRADE: Inspection Plan Templates ═══
        modelBuilder.Entity<InspectionPlanTemplate>()
            .HasOne(t => t.ItemCategory)
            .WithMany()
            .HasForeignKey(t => t.ItemCategoryId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<InspectionPlanTemplate>()
            .HasIndex(t => t.PlanName)
            .IsUnique();

        // ═══ ENTERPRISE UPGRADE: Scheduled Reports ═══
        modelBuilder.Entity<ScheduledReport>()
            .HasOne(r => r.Warehouse)
            .WithMany()
            .HasForeignKey(r => r.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ScheduledReport>()
            .HasIndex(r => new { r.ReportType, r.IsActive });

        // ═══ P2.1: Cross-Dock Execution ═══
        modelBuilder.Entity<CrossDockTask>()
            .HasIndex(x => x.TaskCode)
            .IsUnique();

        modelBuilder.Entity<CrossDockTask>()
            .HasOne(x => x.InboundVoucher)
            .WithMany()
            .HasForeignKey(x => x.InboundVoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CrossDockTask>()
            .HasOne(x => x.InboundVoucherDetail)
            .WithMany()
            .HasForeignKey(x => x.InboundVoucherDetailId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CrossDockTask>()
            .HasOne(x => x.OutboundVoucher)
            .WithMany()
            .HasForeignKey(x => x.OutboundVoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CrossDockTask>()
            .HasOne(x => x.OutboundVoucherDetail)
            .WithMany()
            .HasForeignKey(x => x.OutboundVoucherDetailId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CrossDockTask>()
            .HasOne(x => x.StockReservation)
            .WithMany()
            .HasForeignKey(x => x.StockReservationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CrossDockTask>()
            .HasIndex(x => new { x.InboundVoucherDetailId, x.Status });

        modelBuilder.Entity<CrossDockTask>()
            .HasIndex(x => new { x.OutboundVoucherDetailId, x.Status });

        modelBuilder.Entity<CrossDockTask>()
            .HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CrossDockTask>()
            .HasOne(x => x.StageLocation)
            .WithMany()
            .HasForeignKey(x => x.StageLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        // ═══ P2.2: Cycle Count Program ═══
        modelBuilder.Entity<CycleCountProgram>()
            .HasOne(p => p.Warehouse)
            .WithMany()
            .HasForeignKey(p => p.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CycleCountProgram>()
            .HasIndex(p => new { p.WarehouseId, p.ProgramName })
            .IsUnique();

        modelBuilder.Entity<CycleCountSchedule>()
            .HasIndex(s => new { s.ProgramId, s.ItemId, s.LocationId })
            .IsUnique();

        modelBuilder.Entity<CycleCountSchedule>()
            .HasOne(s => s.Program)
            .WithMany()
            .HasForeignKey(s => s.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CycleCountSchedule>()
            .HasOne(s => s.Item)
            .WithMany()
            .HasForeignKey(s => s.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CycleCountSchedule>()
            .HasOne(s => s.Location)
            .WithMany()
            .HasForeignKey(s => s.LocationId)
            .OnDelete(DeleteBehavior.NoAction);

        // ═══ P2.3: Recall Management ═══
        modelBuilder.Entity<RecallCase>()
            .HasIndex(r => r.CaseNumber)
            .IsUnique();

        modelBuilder.Entity<RecallCase>()
            .HasOne(r => r.Supplier)
            .WithMany()
            .HasForeignKey(r => r.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<RecallLine>()
            .HasOne(l => l.RecallCase)
            .WithMany(c => c.Lines)
            .HasForeignKey(l => l.RecallCaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RecallLine>()
            .HasOne(l => l.Item)
            .WithMany()
            .HasForeignKey(l => l.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        // ═══ P2.4: Labor Activity Standards ═══
        modelBuilder.Entity<RecallLine>()
            .HasIndex(l => new { l.OwnerPartnerId, l.ItemId, l.LotNumber })
            .HasDatabaseName("IX_RecallLines_Owner_Item_Lot");

        modelBuilder.Entity<RecallLine>()
            .HasOne(l => l.OwnerPartner)
            .WithMany()
            .HasForeignKey(l => l.OwnerPartnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LaborActivityStandard>()
            .HasIndex(l => l.ActivityType)
            .IsUnique();

        // ═══ P3.1: Item Velocity Classification ═══
        modelBuilder.Entity<ItemVelocityClassification>()
            .HasIndex(v => new { v.ItemId, v.WarehouseId })
            .IsUnique();

        modelBuilder.Entity<ItemVelocityClassification>()
            .HasOne(v => v.Item)
            .WithMany()
            .HasForeignKey(v => v.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ItemVelocityClassification>()
            .HasOne(v => v.Warehouse)
            .WithMany()
            .HasForeignKey(v => v.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ItemVelocityClassification>()
            .HasOne(v => v.CurrentLocation)
            .WithMany()
            .HasForeignKey(v => v.CurrentLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ItemVelocityClassification>()
            .HasOne(v => v.SuggestedLocation)
            .WithMany()
            .HasForeignKey(v => v.SuggestedLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ItemVelocityClassification>()
            .HasOne(v => v.SuggestedPickFaceLocation)
            .WithMany()
            .HasForeignKey(v => v.SuggestedPickFaceLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ItemVelocityClassification>()
            .HasOne(v => v.SuggestedBulkLocation)
            .WithMany()
            .HasForeignKey(v => v.SuggestedBulkLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        // ═══ P3.2: SLA Metrics ═══
        modelBuilder.Entity<SlaMetric>()
            .HasIndex(s => new { s.VoucherId, s.SlaType })
            .IsUnique();

        modelBuilder.Entity<SlaMetric>()
            .HasOne(s => s.Voucher)
            .WithMany()
            .HasForeignKey(s => s.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        // ═══ P3.3: Capacity Scenario ═══
        modelBuilder.Entity<CapacityScenario>()
            .HasOne(c => c.Warehouse)
            .WithMany()
            .HasForeignKey(c => c.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CapacityScenario>()
            .HasIndex(c => new { c.WarehouseId, c.ScenarioName })
            .IsUnique();

        // ═══ P1.2: Cluster Picking — Cart & Tote ═══
        modelBuilder.Entity<PickCart>()
            .HasIndex(c => c.CartCode).IsUnique();

        modelBuilder.Entity<PickCart>()
            .HasOne(c => c.Warehouse)
            .WithMany()
            .HasForeignKey(c => c.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTote>()
            .HasIndex(t => t.ToteCode).IsUnique();

        modelBuilder.Entity<PickTote>()
            .HasOne(t => t.PickCart)
            .WithMany(c => c.Totes)
            .HasForeignKey(t => t.PickCartId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTote>()
            .HasOne(t => t.Wave)
            .WithMany()
            .HasForeignKey(t => t.WaveId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickTote>()
            .HasOne(t => t.Voucher)
            .WithMany()
            .HasForeignKey(t => t.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        // ═══ P1.3: Zone Picking Assignment ═══
        // P1-05: Two-step picking and sortation
        modelBuilder.Entity<WarehouseSortationConfig>()
            .HasIndex(c => new { c.WarehouseId, c.IsActive })
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("IX_WarehouseSortationConfigs_ActiveWarehouse");

        modelBuilder.Entity<WarehouseSortationConfig>()
            .HasOne(c => c.Warehouse)
            .WithMany()
            .HasForeignKey(c => c.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WarehouseSortationConfig>()
            .HasOne(c => c.StagingLocation)
            .WithMany()
            .HasForeignKey(c => c.StagingLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WarehouseSortationConfig>()
            .HasOne(c => c.SortationLocation)
            .WithMany()
            .HasForeignKey(c => c.SortationLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WarehouseSortationConfig>()
            .Property(c => c.RowVersion)
            .IsRowVersion();

        // P1-06: Phát hành đơn trực tiếp không cần đợt lấy hàng
        modelBuilder.Entity<WarehouseOrderStreamingConfig>()
            .HasIndex(c => new { c.WarehouseId, c.IsActive })
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("IX_WarehouseOrderStreamingConfigs_ActiveWarehouse");

        modelBuilder.Entity<WarehouseOrderStreamingConfig>()
            .HasOne(c => c.Warehouse)
            .WithMany()
            .HasForeignKey(c => c.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WarehouseOrderStreamingConfig>()
            .Property(c => c.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<UserZoneAssignment>()
            .HasIndex(x => new { x.UserId, x.ZoneId }).IsUnique();

        modelBuilder.Entity<UserZoneAssignment>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserZoneAssignment>()
            .HasOne(x => x.Zone)
            .WithMany()
            .HasForeignKey(x => x.ZoneId)
            .OnDelete(DeleteBehavior.NoAction);

        // P2-03: Yard Management MVP
        modelBuilder.Entity<YardSpot>()
            .HasIndex(s => new { s.WarehouseId, s.SpotCode })
            .IsUnique();

        modelBuilder.Entity<YardSpot>()
            .HasIndex(s => new { s.WarehouseId, s.Status, s.IsActive })
            .HasDatabaseName("IX_YardSpots_Warehouse_Status_Active");

        modelBuilder.Entity<YardSpot>()
            .HasOne(s => s.Warehouse)
            .WithMany()
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Trailer>()
            .HasIndex(t => t.TrailerNumber)
            .IsUnique();

        modelBuilder.Entity<Trailer>()
            .HasIndex(t => t.ContainerNumber)
            .IsUnique()
            .HasFilter("[ContainerNumber] IS NOT NULL");

        modelBuilder.Entity<YardVisit>()
            .HasIndex(v => v.VisitCode)
            .IsUnique();

        modelBuilder.Entity<YardVisit>()
            .HasIndex(v => v.TrailerId)
            .IsUnique()
            .HasFilter("[GateOutAt] IS NULL AND [Status] <> 5")
            .HasDatabaseName("IX_YardVisits_Active_Trailer");

        modelBuilder.Entity<YardVisit>()
            .HasIndex(v => v.CurrentSpotId)
            .IsUnique()
            .HasFilter("[CurrentSpotId] IS NOT NULL AND [GateOutAt] IS NULL AND [Status] <> 5")
            .HasDatabaseName("IX_YardVisits_Active_Spot");

        modelBuilder.Entity<YardVisit>()
            .HasIndex(v => new { v.WarehouseId, v.Status, v.GateInAt })
            .HasDatabaseName("IX_YardVisits_Warehouse_Status_GateIn");

        modelBuilder.Entity<YardVisit>()
            .HasOne(v => v.Warehouse)
            .WithMany()
            .HasForeignKey(v => v.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<YardVisit>()
            .HasOne(v => v.Trailer)
            .WithMany(t => t.Visits)
            .HasForeignKey(v => v.TrailerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<YardVisit>()
            .HasOne(v => v.CurrentSpot)
            .WithMany()
            .HasForeignKey(v => v.CurrentSpotId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<YardVisit>()
            .HasOne(v => v.Voucher)
            .WithMany()
            .HasForeignKey(v => v.VoucherId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<YardVisitEvidence>()
            .HasIndex(x => new { x.YardVisitId, x.EvidenceType, x.CapturedAt })
            .HasDatabaseName("IX_YardVisitEvidence_Visit_Type_Date");

        modelBuilder.Entity<YardVisitEvidence>()
            .HasIndex(x => x.FileHashSha256)
            .HasFilter("[FileHashSha256] IS NOT NULL")
            .HasDatabaseName("IX_YardVisitEvidence_FileHash");

        modelBuilder.Entity<YardVisitEvidence>()
            .HasOne(x => x.YardVisit)
            .WithMany(x => x.EvidenceItems)
            .HasForeignKey(x => x.YardVisitId)
            .OnDelete(DeleteBehavior.Cascade);

        // P2-04: Movement Task execution
        modelBuilder.Entity<MovementTask>()
            .HasIndex(t => t.TaskCode)
            .IsUnique();

        modelBuilder.Entity<MovementTask>()
            .HasIndex(t => new { t.WarehouseId, t.Status, t.TaskType, t.CreatedAt })
            .HasDatabaseName("IX_MovementTasks_Warehouse_Status_Type");

        modelBuilder.Entity<MovementTask>()
            .Property(t => t.MovementMode)
            .HasDefaultValue(MovementTaskModeEnum.Item)
            .HasSentinel((MovementTaskModeEnum)0);

        modelBuilder.Entity<MovementTask>()
            .HasIndex(t => new { t.MovementMode, t.ItemId, t.SourceLocationId, t.DestinationLocationId, t.TaskType })
            .IsUnique()
            .HasFilter("[Status] IN (1, 2, 3) AND [MovementMode] = 1")
            .HasDatabaseName("IX_MovementTasks_Open_Item_DuplicateGuard");

        modelBuilder.Entity<MovementTask>()
            .HasIndex(t => t.LicensePlateId)
            .IsUnique()
            .HasFilter("[LicensePlateId] IS NOT NULL AND [Status] IN (1, 2, 3)")
            .HasDatabaseName("IX_MovementTasks_Open_Lpn_DuplicateGuard");

        modelBuilder.Entity<MovementTask>()
            .HasIndex(t => new { t.WarehouseId, t.Status, t.MovementMode, t.CreatedAt })
            .HasDatabaseName("IX_MovementTasks_Warehouse_Status_Mode");

        modelBuilder.Entity<MovementTask>()
            .HasOne(t => t.Warehouse)
            .WithMany()
            .HasForeignKey(t => t.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<MovementTask>()
            .HasOne(t => t.Item)
            .WithMany()
            .HasForeignKey(t => t.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<MovementTask>()
            .HasOne(t => t.SourceLocation)
            .WithMany()
            .HasForeignKey(t => t.SourceLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<MovementTask>()
            .HasOne(t => t.DestinationLocation)
            .WithMany()
            .HasForeignKey(t => t.DestinationLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<MovementTask>()
            .HasOne(t => t.SourceItemLocation)
            .WithMany()
            .HasForeignKey(t => t.SourceItemLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<MovementTask>()
            .HasOne(t => t.LicensePlate)
            .WithMany()
            .HasForeignKey(t => t.LicensePlateId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<MovementTask>()
            .HasOne(t => t.PreviousDefaultLocation)
            .WithMany()
            .HasForeignKey(t => t.PreviousDefaultLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<MovementTask>()
            .HasIndex(t => new { t.WarehouseId, t.Status, t.RoutePriorityScore, t.DueAt })
            .HasDatabaseName("IX_MovementTasks_RoutingQueue");

        modelBuilder.Entity<MovementTask>()
            .HasIndex(t => t.ReplenishmentAutomationRunId)
            .HasDatabaseName("IX_MovementTasks_ReplenishmentRunId");

        modelBuilder.Entity<MovementTask>()
            .HasOne(t => t.ReplenishmentAutomationRun)
            .WithMany()
            .HasForeignKey(t => t.ReplenishmentAutomationRunId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ReplenishmentAutomationRun>()
            .HasIndex(r => r.RunCode)
            .IsUnique();

        modelBuilder.Entity<ReplenishmentAutomationRun>()
            .HasIndex(r => new { r.WarehouseId, r.Status, r.StartedAt })
            .HasDatabaseName("IX_ReplenishmentAutomationRuns_Warehouse_Status_Start");

        modelBuilder.Entity<ReplenishmentAutomationRun>()
            .Property(r => r.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<ReplenishmentAutomationRun>()
            .HasOne(r => r.Warehouse)
            .WithMany()
            .HasForeignKey(r => r.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ReplenishmentAutomationLine>()
            .HasIndex(l => new { l.ReplenishmentAutomationRunId, l.ItemId, l.DestinationLocationId })
            .HasDatabaseName("IX_ReplenishmentAutomationLines_Run_Item_Dest");

        modelBuilder.Entity<ReplenishmentAutomationLine>()
            .HasIndex(l => new { l.WarehouseId, l.Status, l.Priority, l.DueAt })
            .HasDatabaseName("IX_ReplenishmentAutomationLines_Queue");

        modelBuilder.Entity<ReplenishmentAutomationLine>()
            .Property(l => l.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<ReplenishmentAutomationLine>()
            .HasOne(l => l.Run)
            .WithMany(r => r.Lines)
            .HasForeignKey(l => l.ReplenishmentAutomationRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReplenishmentAutomationLine>()
            .HasOne(l => l.Warehouse)
            .WithMany()
            .HasForeignKey(l => l.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ReplenishmentAutomationLine>()
            .HasOne(l => l.Item)
            .WithMany()
            .HasForeignKey(l => l.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ReplenishmentAutomationLine>()
            .HasOne(l => l.DestinationLocation)
            .WithMany()
            .HasForeignKey(l => l.DestinationLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ReplenishmentAutomationLine>()
            .HasOne(l => l.SourceLocation)
            .WithMany()
            .HasForeignKey(l => l.SourceLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ReplenishmentAutomationLine>()
            .HasOne(l => l.SourceItemLocation)
            .WithMany()
            .HasForeignKey(l => l.SourceItemLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ReplenishmentAutomationLine>()
            .HasOne(l => l.MovementTask)
            .WithMany()
            .HasForeignKey(l => l.MovementTaskId)
            .OnDelete(DeleteBehavior.NoAction);

        // P3-01: VAS kitting work orders
        modelBuilder.Entity<KittingWorkOrder>()
            .HasIndex(k => k.WorkOrderCode)
            .IsUnique();

        modelBuilder.Entity<KittingWorkOrder>()
            .HasIndex(k => new { k.WarehouseId, k.Status, k.CreatedAt })
            .HasDatabaseName("IX_KittingWorkOrders_Warehouse_Status_Date");

        modelBuilder.Entity<KittingWorkOrder>()
            .Property(k => k.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<KittingWorkOrder>()
            .HasOne(k => k.Warehouse)
            .WithMany()
            .HasForeignKey(k => k.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<KittingWorkOrder>()
            .HasOne(k => k.FinishedItem)
            .WithMany()
            .HasForeignKey(k => k.FinishedItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<KittingWorkOrder>()
            .HasOne(k => k.FinishedLocation)
            .WithMany()
            .HasForeignKey(k => k.FinishedLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<KittingWorkOrderLine>()
            .HasIndex(l => new { l.KittingWorkOrderId, l.ComponentItemId, l.SourceLocationId, l.LotNumber, l.ExpiryDate })
            .HasDatabaseName("IX_KittingWorkOrderLines_WorkOrder_Component_Source");

        modelBuilder.Entity<KittingWorkOrderLine>()
            .HasIndex(l => new { l.ComponentItemId, l.SourceLocationId, l.Status })
            .HasDatabaseName("IX_KittingWorkOrderLines_Component_Source_Status");

        modelBuilder.Entity<KittingWorkOrderLine>()
            .Property(l => l.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<KittingWorkOrderLine>()
            .HasOne(l => l.KittingWorkOrder)
            .WithMany(k => k.Lines)
            .HasForeignKey(l => l.KittingWorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KittingWorkOrderLine>()
            .HasOne(l => l.ComponentItem)
            .WithMany()
            .HasForeignKey(l => l.ComponentItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<KittingWorkOrderLine>()
            .HasOne(l => l.SourceLocation)
            .WithMany()
            .HasForeignKey(l => l.SourceLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<KittingWorkOrderLine>()
            .HasOne(l => l.SourceItemLocation)
            .WithMany()
            .HasForeignKey(l => l.SourceItemLocationId)
            .OnDelete(DeleteBehavior.NoAction);
        // ═══ P2-03B: Yard Billing ═══
        modelBuilder.Entity<YardBillingRate>()
            .Property(r => r.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<YardBillingRate>()
            .HasOne(r => r.Warehouse)
            .WithMany()
            .HasForeignKey(r => r.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<YardBillingRate>()
            .HasOne(r => r.Partner)
            .WithMany()
            .HasForeignKey(r => r.PartnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<YardBillingRate>()
            .HasIndex(r => new { r.WarehouseId, r.PartnerId, r.CarrierName, r.TrailerType, r.SpotType, r.IsActive })
            .HasDatabaseName("IX_YardBillingRates_Match");

        modelBuilder.Entity<YardBillingCharge>()
            .HasOne(c => c.YardVisit)
            .WithMany()
            .HasForeignKey(c => c.YardVisitId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<YardBillingCharge>()
            .HasOne(c => c.BillingRate)
            .WithMany()
            .HasForeignKey(c => c.YardBillingRateId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<YardBillingCharge>()
            .HasOne(c => c.Warehouse)
            .WithMany()
            .HasForeignKey(c => c.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<YardBillingCharge>()
            .HasIndex(c => new { c.YardVisitId, c.Status })
            .HasDatabaseName("IX_YardBillingCharges_Visit_Status");

        // Enterprise 8-10: optimization, automation telemetry, integration contracts
        modelBuilder.Entity<OptimizationRun>()
            .HasIndex(x => new { x.WarehouseId, x.RunType, x.Status, x.CreatedAt })
            .HasDatabaseName("IX_OptimizationRuns_Warehouse_Type_Status_Date");

        modelBuilder.Entity<OptimizationRun>()
            .HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<OptimizationRun>()
            .HasOne(x => x.OwnerPartner)
            .WithMany()
            .HasForeignKey(x => x.OwnerPartnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<OptimizationRecommendationLine>()
            .HasIndex(x => new { x.OptimizationRunId, x.LineType, x.Score })
            .HasDatabaseName("IX_OptimizationLines_Run_Type_Score");

        modelBuilder.Entity<OptimizationRecommendationLine>()
            .HasOne(x => x.OptimizationRun)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.OptimizationRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OptimizationRecommendationLine>()
            .HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<OptimizationRecommendationLine>()
            .HasOne(x => x.OwnerPartner)
            .WithMany()
            .HasForeignKey(x => x.OwnerPartnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<OptimizationRecommendationLine>()
            .HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<OptimizationRecommendationLine>()
            .HasOne(x => x.SourceLocation)
            .WithMany()
            .HasForeignKey(x => x.SourceLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<OptimizationRecommendationLine>()
            .HasOne(x => x.SuggestedLocation)
            .WithMany()
            .HasForeignKey(x => x.SuggestedLocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WavelessReleaseQueue>()
            .HasIndex(x => x.IdempotencyKey)
            .IsUnique();

        modelBuilder.Entity<WavelessReleaseQueue>()
            .HasIndex(x => new { x.WarehouseId, x.Status, x.PriorityScore })
            .HasDatabaseName("IX_WavelessQueue_Warehouse_Status_Priority");

        modelBuilder.Entity<WavelessReleaseQueue>()
            .HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WavelessReleaseQueue>()
            .HasOne(x => x.PickTask)
            .WithMany()
            .HasForeignKey(x => x.PickTaskId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickPathPlan>()
            .HasIndex(x => new { x.WarehouseId, x.CreatedAt })
            .HasDatabaseName("IX_PickPathPlans_Warehouse_Date");

        modelBuilder.Entity<PickPathPlan>()
            .HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickPathPlanStop>()
            .HasOne(x => x.PickPathPlan)
            .WithMany(x => x.Stops)
            .HasForeignKey(x => x.PickPathPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PickPathPlanStop>()
            .HasOne(x => x.PickTask)
            .WithMany()
            .HasForeignKey(x => x.PickTaskId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PickPathPlanStop>()
            .HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ToteClusterPlan>()
            .HasIndex(x => new { x.WarehouseId, x.OwnerPartnerId, x.CustomerKey, x.CreatedAt })
            .HasDatabaseName("IX_ToteClusterPlans_Scope_Date");

        modelBuilder.Entity<ToteClusterAssignment>()
            .HasOne(x => x.ToteClusterPlan)
            .WithMany(x => x.Assignments)
            .HasForeignKey(x => x.ToteClusterPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ToteClusterAssignment>()
            .HasOne(x => x.PickTote)
            .WithMany()
            .HasForeignKey(x => x.PickToteId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ToteClusterAssignment>()
            .HasOne(x => x.PickTask)
            .WithMany()
            .HasForeignKey(x => x.PickTaskId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<MheAdapterProfile>()
            .HasIndex(x => new { x.WarehouseId, x.AdapterCode })
            .IsUnique();

        modelBuilder.Entity<MheAdapterProfile>()
            .HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<MheAdapterProfile>()
            .HasOne(x => x.MheSystem)
            .WithMany()
            .HasForeignKey(x => x.MheSystemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<MheTelemetryEvent>()
            .HasIndex(x => new { x.WarehouseId, x.EquipmentCode, x.EventAt })
            .HasDatabaseName("IX_MheTelemetry_Warehouse_Equipment_Date");

        modelBuilder.Entity<MheTelemetryEvent>()
            .HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<MheTelemetryEvent>()
            .HasOne(x => x.AdapterProfile)
            .WithMany()
            .HasForeignKey(x => x.MheAdapterProfileId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<AutomationOverride>()
            .HasOne(x => x.MheCommand)
            .WithMany()
            .HasForeignKey(x => x.MheCommandId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<EdiMessage>()
            .HasIndex(x => new { x.MessageType, x.Direction, x.ControlNumber })
            .IsUnique();

        modelBuilder.Entity<EdiMessage>()
            .HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<EdiMessage>()
            .HasOne(x => x.Partner)
            .WithMany()
            .HasForeignKey(x => x.PartnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WebhookSubscription>()
            .HasIndex(x => x.SubscriptionCode)
            .IsUnique();

        modelBuilder.Entity<WebhookDelivery>()
            .HasIndex(x => x.IdempotencyKey)
            .IsUnique();

        modelBuilder.Entity<WebhookDelivery>()
            .HasOne(x => x.Subscription)
            .WithMany(x => x.Deliveries)
            .HasForeignKey(x => x.WebhookSubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EnterpriseConnector>()
            .HasIndex(x => new { x.ConnectorType, x.ConnectorCode })
            .IsUnique();

        modelBuilder.Entity<EnterpriseConnectorDelivery>()
            .HasIndex(x => x.IdempotencyKey)
            .IsUnique();

        modelBuilder.Entity<EnterpriseConnectorDelivery>()
            .HasOne(x => x.Connector)
            .WithMany()
            .HasForeignKey(x => x.EnterpriseConnectorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SemanticMetricDefinition>()
            .HasIndex(x => x.MetricCode)
            .IsUnique();

        modelBuilder.Entity<SemanticMetricSnapshot>()
            .HasIndex(x => new { x.SemanticMetricDefinitionId, x.MetricDate, x.ScopeKey })
            .HasDatabaseName("IX_SemanticMetricSnapshots_Metric_Date_Scope");

        modelBuilder.Entity<SemanticMetricSnapshot>()
            .HasOne(x => x.MetricDefinition)
            .WithMany(x => x.Snapshots)
            .HasForeignKey(x => x.SemanticMetricDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EnterprisePredictiveAlert>()
            .HasIndex(x => new { x.AlertType, x.Status, x.ForecastFor, x.WarehouseId, x.OwnerPartnerId })
            .HasDatabaseName("IX_EnterprisePredictiveAlerts_Type_Status_Scope");

        modelBuilder.Entity<AuditAnalyticsFinding>()
            .HasIndex(x => new { x.FindingType, x.Status, x.OccurredAt })
            .HasDatabaseName("IX_AuditAnalyticsFindings_Type_Status_Time");

        modelBuilder.Entity<AiAssistantSession>()
            .HasIndex(x => x.SessionCode)
            .IsUnique();

        modelBuilder.Entity<AiAssistantMessage>()
            .HasOne(x => x.Session)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.AiAssistantSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AiAssistantCitation>()
            .HasOne(x => x.Message)
            .WithMany(x => x.Citations)
            .HasForeignKey(x => x.AiAssistantMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WarehouseWorkflowProfile>()
            .HasIndex(x => new { x.WarehouseId, x.OwnerPartnerId, x.ModuleKey })
            .IsUnique()
            .HasDatabaseName("UX_WarehouseWorkflowProfiles_Scope_Module");

        modelBuilder.Entity<RequestTelemetryLog>()
            .HasIndex(x => new { x.CreatedAt, x.Path, x.StatusCode })
            .HasDatabaseName("IX_RequestTelemetryLogs_Time_Path_Status");

        modelBuilder.Entity<RequestTelemetryLog>()
            .HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_RequestTelemetryLogs_CorrelationId");

        modelBuilder.Entity<SreMetricSnapshot>()
            .HasIndex(x => x.SnapshotAt)
            .HasDatabaseName("IX_SreMetricSnapshots_SnapshotAt");
    }
}
