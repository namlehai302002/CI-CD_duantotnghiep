# EDI Enterprise Roadmap

Date: 2026-05-20  
System: WMS Pro  
Scope: roadmap for enterprise EDI coverage without changing current public API contracts.

## Documents

| Document | Direction | Purpose | Minimum Contract Gate |
|---|---|---|---|
| ASN / 856 | Inbound | Supplier or owner advance shipment notice | Validate control number, warehouse, owner, item, quantity, lot/serial/expiry where applicable |
| Warehouse Shipping Order / 940 | Inbound | External order release into outbound execution | Validate owner, warehouse, ship-to, lines, allocation rules, carrier/cutoff metadata |
| Shipment Confirmation / 945 | Outbound | Confirm shipped quantities, packages, carrier, tracking | Validate shipment status, package list, owner-safe payload, idempotency |
| Inventory Advice | Outbound | Report stock, holds, availability, and adjustments | Validate owner/warehouse scope and no cross-owner leakage |
| Receipt Confirmation | Outbound | Confirm received quantity, variance, QC, lot/serial/catch weight | Validate voucher status, posted ledger, and exception evidence |

## Implementation Tasks

- Add typed parser/validator per document instead of ad hoc string manipulation.
- Keep the existing EDI persistence foundation in `EdiMessages`.
- Add replay and rejection evidence for every EDI document type.
- Add owner and warehouse scope tests for all EDI exports.
- Add partner-level mapping rules for item code, UOM, location, carrier, and service level.

## Acceptance

- Contract tests must cover valid, rejected, replayed, and exported states.
- Error reports must be safe for operators and must not expose secrets.
- Staging certification evidence must be attached to the release evidence pack before any vendor-facing claim.

