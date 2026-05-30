# WMS Enterprise Benchmark Scorecard - 2026-05-21

## Executive Result

So với Oracle Fusion Cloud WMS, SAP EWM, Manhattan Active WM và Blue Yonder WMS, hệ thống hiện đạt khoảng **81% Tier-1 WMS equivalence** nếu tính cả bằng chứng vận hành sản xuất. Ở phạm vi repo/local verification, sau khi visual baseline pass và no-device smoke pass, mức sẵn sàng kỹ thuật nội bộ đạt khoảng **92%**.

Điểm này không nên được ghi là 100% ngang Oracle/Manhattan/SAP/Blue Yonder cho tới khi có bằng chứng thật về thiết bị kho, tải lớn, backup/restore, HA/DR, tích hợp được chứng nhận và dữ liệu sản xuất đủ lớn.

## Source Benchmark

- Oracle WMS nhấn mạnh omnichannel fulfillment, inbound/inventory/outbound, lot/batch/serial, cross-docking, wave/task grouping, MHE/WCS automation, 3PL multi-client, KPI visibility và integration/export: https://www.oracle.com/scm/logistics/warehouse-management/ và https://docs.oracle.com/en/cloud/saas/supply-chain-and-manufacturing/24d/faips/wm-overview-of-integration-types.html
- SAP EWM bao phủ inbound, storage/internal process, physical inventory/cycle count, yard visibility, wave pick/pack/ship, batch/serial/catch weight, labor, VAS, kitting, cross-docking và robotics: https://www.sap.com/products/scm/extended-warehouse-management/features.html
- Manhattan Active WM tập trung cloud-native, microservices, labor/robotics/transportation orchestration, real-time visibility, order streaming, slotting, WES/MHE và versionless updates: https://www.manh.com/solutions/supply-chain-management-software/warehouse-management
- Blue Yonder WMS nhấn mạnh system-directed activities, embedded intelligence, returns, near-real-time analytics, cloud-native reliability, labor/automation orchestration và AI recommendations: https://blueyonder.com/solutions/warehouse-management

## Weighted Score

| Area | Weight | Current | Weighted | Evidence |
|---|---:|---:|---:|---|
| Feature breadth | 30% | 84% | 25.2 | Inbound/outbound, inventory, RF, wave, slotting, yard, 3PL, labor, MHE, carrier, workflow, reports are present. |
| Enterprise UI/UX | 20% | 90% | 18.0 | Zero rough marker scope, dense app shell, visual routes, RF/mobile and print CSS foundation. |
| Business/security controls | 20% | 82% | 16.4 | CSRF, roles, permissions, warehouse/owner scope, safe errors, audit trails and status labels are covered by tests. |
| Integration/reporting | 15% | 78% | 11.7 | API/EDI/webhook/export foundations exist; certified partner integrations and production contracts still need external evidence. |
| Production evidence | 15% | 62% | 9.3 | Build/test/visual can be local-green; k6 is optional, DR restore and hardware evidence need real environment. |
| **Overall Tier-1 equivalence** | **100%** |  | **80.6%** | Rounded to **81%**. |

## Gap To True Tier-1

- Real hardware: USB/Bluetooth scanner, mobile camera, label/document printer and RF handhelds must be certified on real devices.
- Performance: k6 or equivalent load evidence is optional for local UI acceptance, but required for production throughput claims.
- DR/HA: backup/restore drill, failover, recovery time and recovery point evidence require real database/environment permissions.
- Integrations: Oracle/SAP/Manhattan/Blue Yonder-class deployments expect documented ERP/TMS/OMS/MHE/carrier contracts and partner certification artifacts.
- Operations: release evidence should bundle build, tests, visual, security, migration, rollback, monitoring and incident response per release.

## Current Local Evidence Interpretation

- `test-results/.last-run.json` was failed because screenshot baselines were missing, not because the first inspected route crashed. Playwright created actual snapshots and correctly required a baseline acceptance run.
- k6 is not mandatory for local UI/functional completion. It remains an optional production-load evidence path.
- Scanner/printer certification is simulated locally through no-device Playwright smoke; real-device execution is explicitly marked as pending external evidence.
- Existing secrets and connection strings in `appsettings.json` were not printed or modified by this scorecard.

## Re-Audit Addendum - 2026-05-21

- Strict app-code scan now covers 566 included text files after adding dedicated offline shell assets.
- UI rough marker scan for app views and offline HTML is clean: style-block, inline-style and hard-coded root-route markers are all 0 in the enforced UI scope.
- Raw enum fallback labels in Razor views are now 0: helper fallbacks use stable Vietnamese labels instead of `_ => value.ToString()`.
- PWA offline page is externalized through `wwwroot/css/wms-offline.css` and `wwwroot/js/offline-page.js`, both included in the service worker shell cache.
- Verification follow-up passed: build 0 warnings/errors, full .NET suite 553/553, public visual 6/6, auth setup 1/1, authenticated visual 100 executable tests with 4 expected skips, and no-device RF/print 10/10.
- Tier-1 equivalence remains **81%** because feature breadth did not change and production gaps still require real hardware, DR/HA, load and integration certification evidence. Repo/local readiness improves to **93%** because the offline shell and raw enum UI fallback gates are now locked by regression tests.

## Zero Inline Event Follow-Up - 2026-05-21

- Extended zero marker gate to include app HTML inline event attributes (click/change/input/invalid and drag/drop inline event attributes) and script-side inline event assignments (direct click/change/input event-property assignments) in app Views/custom JS.
- Current strict scan result for those new markers is 0. Legacy handlers were moved to delegated `data-wms-*` actions in `wwwroot/js/site.js`.
- Local verification safety was tightened: Development + configured flags + configured username + remote loopback IP + loopback request host are all required. Production/non-Development ignores local verification flags and logs `LOCAL_VERIFICATION_DISABLED_OUTSIDE_DEVELOPMENT`.
- Verification follow-up passed: build 0 warnings/errors, full .NET suite 556/556, public visual 6/6, auth setup 1/1, authenticated visual 100 executable tests with 4 expected skips, no-device RF/print 10/10, and `.last-run.json` stayed `passed`.
- Tier-1 equivalence remains **81%** because external production evidence still controls that score. Repo/local readiness is now **94%** because the UI marker gate is stricter and the local captcha bypass is guarded by regression tests.
