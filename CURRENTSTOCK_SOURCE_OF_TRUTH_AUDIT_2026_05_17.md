# CurrentStock Source Of Truth Audit - 2026-05-17

## Decision

`ItemLocation.Quantity` is the inventory source of truth for on-hand stock by warehouse, location, lot, hold state and owner scope.

`Item.CurrentStock` is a display/cache field only. It can be synchronized from `ItemLocation.Quantity`, but business decisions must not trust it as the primary quantity ledger.

## Findings

- CS-AUD-001: Inventory balance reads must aggregate `ItemLocation.Quantity` and group by item before display or alert logic.
- CS-AUD-002: Reports should apply scoped balance maps before rendering `Item.CurrentStock`.
- CS-AUD-003: Voucher approval and execution flows may update the cache after location quantity changes, but must keep location lines as the ledger.
- CS-AUD-004: Stock count system quantity must come from `ItemLocation.Quantity`, not stale item cache.
- CS-AUD-005: Low-stock, over-stock and valuation screens must use scoped balances when warehouse context exists.
- CS-AUD-006: Tests must cover valuation and synchronization paths.
- CS-AUD-007: Direct writes to `Item.CurrentStock` are allowed only as synchronization/cache updates after ledger mutation.
- CS-AUD-008: Any new stock mutation flow must call `SyncCurrentStockAsync` or an equivalent balance synchronization path.

## Acceptance Cho 100%

- `ItemLocation.Quantity` remains the source of truth.
- `Item.CurrentStock` remains a cache for display compatibility.
- Stock reports and dashboards use balance maps before display.
- Voucher, inbound and outbound services synchronize the cache after changing inventory ledger rows.
- Tests verify current valuation and sync behavior.

