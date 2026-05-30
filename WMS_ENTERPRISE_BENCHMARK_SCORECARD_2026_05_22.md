# WMS Enterprise Benchmark Scorecard - 2026-05-22

## Executive Result

So với Oracle Fusion Cloud WMS, SAP EWM, Manhattan Active WM và Blue Yonder WMS, hệ thống hiện đạt khoảng **86% Tier-1 WMS equivalence** nếu tính cả tiêu chí production evidence. Ở phạm vi **repo/local verification**, hệ thống đạt **100% local evidence green** theo gate hiện có: build, test, visual, no-device RF/print, mobile-deep, static marker và server-log noise đều pass.

Điểm này không phải chứng nhận chính thức ngang Oracle/SAP/Manhattan/Blue Yonder. Lý do chưa ghi production 100% là còn thiếu bằng chứng thật về scanner/printer/RF handheld, load lớn, backup/restore và HA/DR, dữ liệu production đủ lớn, cùng certification/contract cho ERP/TMS/OMS/MHE/carrier integration.

## Official Benchmark Basis

- Oracle WMS: inbound/outbound integration, master data, fulfillment orders, inbound shipments, MHE messages, shipment verification, inventory transactions, wave/replenishment pick, OBLPN shipping, route information, reports and labels. Sources: https://www.oracle.com/scm/logistics/warehouse-management/ and https://docs.oracle.com/en/cloud/saas/supply-chain-and-manufacturing/25c/faips/wm-overview-of-integration-types.html
- SAP EWM: inbound receiving, storage/internal process, physical inventory/cycle count, yard visibility, outbound wave pick/pack/ship, batch/serial/catch weight, labor, VAS, kitting, cross-docking and warehouse robotics. Source: https://www.sap.com/products/scm/extended-warehouse-management/features.html
- Manhattan Active WM: cloud-native WMS, workflow orchestration across labor, robotics and transportation, built-in WES, robot-ready automation and slotting optimization. Source: https://www.manh.com/products/warehouse-management
- Blue Yonder WMS: warehouse operations, AI agents, resource forecasting/orchestration, Robotics Hub, labor, advanced slotting, load building, yard, warehouse execution and returns. Source: https://blueyonder.com/solutions/warehouse-management

## Weighted Score

| Area | Weight | Current | Weighted | Evidence basis |
|---|---:|---:|---:|---|
| Feature breadth | 30% | 85% | 25.5 | Inbound/outbound, inventory, RF/mobile, wave/tasking, slotting, yard, 3PL, labor, MHE/WCS, carrier, workflow and reporting are present. |
| Enterprise UI/UX | 20% | 97% | 19.4 | Dense enterprise shell, zero rough UI marker gate, desktop/zoom/mobile/tablet visual evidence, no-device RF/print evidence and stable print/offline assets. |
| Business/security controls | 20% | 91% | 18.2 | CSRF, roles/permissions, warehouse/owner scope, safe-error patterns, confirm/reason flows, audit trail and status label gates are covered locally. |
| Integration/reporting | 15% | 82% | 12.3 | API/EDI/webhook/export/reporting foundations exist; certified partner contracts and production integration evidence remain external. |
| Production evidence | 15% | 68% | 10.2 | Local verification is green; real hardware, load, DR/HA, backup/restore and long-running production evidence remain external. |
| **Overall Tier-1 equivalence** | **100%** |  | **85.6%** | Rounded to **86%**. |

## Repo/Local 100% Evidence

- Strict included scope: app code, Views, Controllers, Services, Models/ViewModels, Data, custom `wwwroot`, tests, scripts, docs, migrations and project/config text files.
- Excluded generated/vendor/runtime scope: `bin`, `obj`, `node_modules`, `artifacts`, `test-results`, `uploads`, `App_Data`, `wwwroot/lib`, and visual auth state.
- Latest strict scan: **673 included authored files**, **566 included text files**, UTF-8 replacement-character hits = 0, known question-mark Vietnamese corruption hits = 0.
- UI marker gate: root-prefixed custom asset route markers = 0, inline style = 0, local style block = 0, inline event attributes = 0, production console debug calls = 0.
- Runtime evidence: `test-results/.last-run.json`, no-device visual artifact and mobile-deep visual artifact are all `passed`.
- Verification: `scripts/Run-WmsVerification.ps1 -SkipK6` passed with build 0 warnings/errors, .NET test 563/563, public visual 6/6, auth setup 1/1, authenticated visual 100/100 with 4 expected skips, no-device RF/print 10/10, mobile-deep 412/412 and clean server-log gate.

## Findings From This Re-Audit

- No new code bug requiring schema/API/UI mutation was found in the strict local pass.
- PowerShell `Get-Content` can display UTF-8 Vietnamese text incorrectly on this machine; the source-of-truth checks are UTF-8 `.NET` reads and browser/Playwright text scans.
- The broad single-character mojibake-like scan can flag valid Vietnamese uppercase text such as barcode comments in voucher creation; this is classified as false positive because UTF-8 decode and browser text are correct.
- `NotImplemented` hits are detector strings inside regression tests, not runtime application code.
- Exception-message hits are either safe-error helper logic, comments explaining XSS hardening, or regression tests forbidding unsafe exposure; no direct unsafe UI/API exposure pattern was found in Controllers/Services/Common.

## Gap To Production Tier-1 100%

- Run real-device certification for USB/Bluetooth scanner, camera scan, RF handheld, label printer and document printer.
- Run k6 or equivalent production-load profile with realistic data volume and authenticated/staging environment.
- Execute DB backup/restore and HA/DR drill with measured RPO/RTO.
- Produce signed integration evidence for ERP/TMS/OMS/MHE/carrier contracts, payloads, retries, monitoring and partner certification where required.
- Capture a release evidence bundle per production deployment: build, tests, visual, migration dry-run, rollback plan, security review, monitoring and incident response readiness.

## Final Interpretation

- **Repo/local readiness: 100%** for the currently defined local evidence gate.
- **Tier-1 production equivalence: 86%** against Oracle/SAP/Manhattan/Blue Yonder-class expectations.
- The remaining 14% is not ordinary local UI polish; it is mainly production-scale proof, certified integrations, real hardware, DR/HA and long-running operational evidence.
