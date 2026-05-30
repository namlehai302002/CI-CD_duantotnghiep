# Export, Download, And API Read Scope Registry

Date: 2026-05-20  
System: WMS Pro  
Purpose: single registry for sensitive read/export/download surfaces.

| Surface | Controller action | Authorization | Warehouse scope | Owner scope | Audit logging | Anti-forgery | No cross-owner leakage gate |
|---|---|---|---|---|---|---|---|
| EDI payload export | `ApiIntegration.ExportEdi` | API key + integration route guard | Required when payload maps to warehouse | Required when payload maps to partner/owner | Required in integration event/audit evidence | Not applicable for GET API export | Registry and EDI owner-scope test |
| Yard billing export | `Operations.ExportYardBillingChargesExcel` | Manager/Admin operational policy | Required | Not applicable unless owner-billed | Required for sensitive export | GET export, not applicable | Warehouse scoped export test |
| Delivery reconciliation CSV | `Operations.ExportDeliveryReconciliationCsv` | Staff/Manager/Admin | Required | Required through voucher/package ownership where present | Required for sensitive export | GET export, not applicable | Multi-owner package test |
| Delivery reconciliation Excel | `Operations.ExportDeliveryReconciliationExcel` | Staff/Manager/Admin | Required | Required through voucher/package ownership where present | Required for sensitive export | GET export, not applicable | Multi-owner package test |
| Optimization export | `Operations.ExportOptimizationLines` | Manager/Admin | Required | Required through optimization line owner safety | Required for sensitive export | GET export, not applicable | Optimization owner-safe test |
| EDI message export | `Operations.ExportEdiMessage` | Manager/Admin integration policy | Required | Required by partner/owner parameter | Required in EDI message lifecycle | POST uses global CSRF | EDI owner-scope test |
| Yard visit evidence download | `Operations.DownloadYardVisitEvidence` | Staff/Manager/Admin | Required | Not applicable unless visit has owner | Required for document access | GET download, not applicable | Private storage guarded download test |
| Dock appointments export | `Operations.ExportDockAppointments` | Staff/Manager/Admin | Required | Not applicable | Required for sensitive export | GET export, not applicable | Warehouse scoped export test |
| 3PL invoice Excel | `Operations.ExportThreePlInvoiceExcel` | 3PL billing permission | Required | Required by invoice owner | Required for billing export | GET export, not applicable | Owner portal/billing scope test |
| 3PL invoice PDF | `Operations.ExportThreePlInvoicePdf` | 3PL billing permission | Required | Required by invoice owner | Required for billing export | GET export, not applicable | Owner portal/billing scope test |
| Labor productivity export | `Operations.ExportLaborProductivity` | Manager/Admin | Required | Optional owner dimension when present | Required for sensitive export | GET export, not applicable | Warehouse/team scope test |
| Shipment loads export | `Operations.ExportShipmentLoadsCsv` | Staff/Manager/Admin | Required | Required through voucher/package ownership where present | Required for sensitive export | GET export, not applicable | Shipping owner-scope test |
| 3PL billing run CSV | `Operations.ExportThreePlBillingRunCsv` | 3PL billing permission | Required | Required by charge owner | Required for billing export | GET export, not applicable | Owner billing scope test |
| 3PL billing run Excel | `Operations.ExportThreePlBillingRunExcel` | 3PL billing permission | Required | Required by charge owner | Required for billing export | GET export, not applicable | Owner billing scope test |
| Top items export | `Reports.ExportTopItems` | Report permission | Required when filtered | Required for owner-scoped analytics | Required for sensitive export | GET export, not applicable | Analytics owner-scope test |
| Stock movement export | `Reports.ExportStockMovement` | Report permission | Required | Required through item/location owner | Required for inventory export | GET export, not applicable | Multi-owner stock test |
| Inventory transactions export | `Reports.ExportInventoryTransactions` | Report permission | Required | Required through transaction owner | Required for inventory export | GET export, not applicable | Ledger owner-scope test |
| Inventory export | `Reports.ExportInventory` | Report permission | Required | Required through item/location owner | Required for inventory export | GET export, not applicable | Multi-owner inventory report test |
| Stock valuation export | `Reports.ExportStockValuation` | Financial report permission | Required | Required through owner valuation | Required for financial export | GET export, not applicable | Financial owner-scope test |
| Stock snapshot export | `Reports.ExportStockSnapshot` | Report permission | Required | Required through snapshot owner | Required for inventory export | GET export, not applicable | Snapshot owner-scope test |
| SRE snapshot export | `System.ExportSreSnapshot` | Admin/SRE policy | Not applicable | Not applicable | Required for operational export | GET export, not applicable | No business data payload test |
| Import template download | `Vouchers.DownloadImportTemplate` | Staff/Manager/Admin | Not applicable | Not applicable | Optional | GET download, not applicable | Static template only |
| Receipt document download | `Vouchers.DownloadReceiptDocument` | Staff/Manager/Admin | Required through voucher/log | Required through voucher owner | Required for document access | GET download, not applicable | Private storage guarded download test |
| Sample import download | `Vouchers.DownloadSampleImport100` | Staff/Manager/Admin | Not applicable | Not applicable | Optional | GET download, not applicable | Static sample only |

## Acceptance

- Every export/download/API read action must appear in this registry.
- Tests must fail when a new export/download action is added without registry coverage.
- Registry entries must state authorization, scope, audit, anti-forgery applicability, and leakage gate.
- Regression scenarios must include multi-owner same item/location/lot, partial warehouse scope, partial owner scope, Owner billing scope, and Analytics owner-scope.

