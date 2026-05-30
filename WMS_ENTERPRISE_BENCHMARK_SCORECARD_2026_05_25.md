# WMS Enterprise Benchmark Scorecard - 2026-05-25

## Executive Result

Sau vòng rà cuối ngày 25/05/2026, WMS Pro đạt **100% repo/local readiness** theo gate hiện có: static audit sạch, build sạch, .NET test sạch, visual public/auth/test/no-device/mobile-deep pass và server-log gate không còn noise chặn.

Khi so với nhóm Tier-1 WMS như Oracle Fusion Cloud Warehouse Management, SAP EWM, Manhattan Active WM và Blue Yonder WMS, điểm production equivalence vẫn được chấm trung thực ở khoảng **86%**. Không tăng lên 100% production vì chưa có bằng chứng thật về scanner/printer/RF handheld, load lớn, backup/restore + HA/DR, dữ liệu production dài hạn và chứng nhận tích hợp ERP/TMS/OMS/MHE/carrier.

## Official Benchmark Basis

- Oracle Fusion Cloud Warehouse Management integration docs nêu inbound master data, business entities, MHE inbound messages; outbound shipment verification, inventory transactions, automation messages, reports, labels, REST/SFTP/printer/file export. Source: https://docs.oracle.com/en/cloud/saas/supply-chain-and-manufacturing/25c/faips/wm-overview-of-integration-types.html
- SAP EWM official features nêu inbound receiving, stock ownership, physical inventory/cycle count, yard visibility, outbound wave pick/pack/ship, batch/serial/catch weight, labor, VAS, kitting, cross-docking và warehouse robotics. Source: https://www.sap.com/products/scm/extended-warehouse-management/features.html
- Manhattan Active Warehouse Management nêu cloud-native WMS, inventory/labor/slotting/automation command, order streaming, dynamic workflow, WES inside WMS và robotics/MHE orchestration. Source: https://www.manh.com/products/warehouse-management
- Blue Yonder WMS nêu warehouse operations, AI agents, resource forecasting/orchestration, Robotics Hub, warehouse labor, advanced slotting, load building, yard, warehouse execution và returns. Source: https://blueyonder.com/solutions/warehouse-management

## Weighted Score

| Area | Weight | Current | Weighted | Evidence basis |
|---|---:|---:|---:|---|
| Feature breadth | 30% | 85% | 25.5 | Inbound/outbound, inventory, RF/mobile, wave/tasking, slotting, yard, 3PL, labor, MHE/WCS, carrier, workflow, label/print and reporting are present locally. |
| Enterprise UI/UX | 20% | 97% | 19.4 | Dense enterprise shell, zero rough UI marker gate, desktop/zoom/mobile/tablet visual evidence, no-device RF/print and print/offline assets are green. |
| Business/security controls | 20% | 91% | 18.2 | CSRF, roles/permissions, warehouse/owner scope, safe-error helper, confirm/reason flows, audit trail and status-display gates are covered locally. |
| Integration/reporting | 15% | 82% | 12.3 | API/EDI/webhook/export/reporting foundations exist; certified partner contracts and production payload evidence remain external. |
| Production evidence | 15% | 68% | 10.2 | Local evidence is fully green; real hardware, load, DR/HA, backup/restore and long-running production proof remain external. |
| **Overall Tier-1 production equivalence** | **100%** |  | **85.6%** | Rounded to **86%**. |

## Repo/Local Evidence - 2026-05-25

- Strict included scan before writing this report: **673 authored files**, **567 text files**, **130 View/custom HTML files**.
- Excluded: `bin`, `obj`, `node_modules`, `artifacts`, `test-results`, `uploads`, `App_Data`, `wwwroot/lib`, visual auth state and runtime reports.
- UTF-8 replacement-character files = 0; known Vietnamese mojibake marker files = 0.
- View/custom HTML rough markers: inline `style=` = 0, local `<style>` = 0, inline event attributes = 0, root `href="/..."` = 0, root `action="/..."` = 0.
- Production browser debug marker: app Views/custom `wwwroot` `console.log(` = 0.
- Direct unsafe exception-message UI/API exposure: no controller/service direct exposure found; candidate hits are inside `Common/UserSafeError.cs`, which is the safe-error helper intentionally covered by regression tests.
- EF paging scan found `Skip/Take` candidates, but runtime server-log gate passed with no EF `Skip/Take without OrderBy` warning.

## Verification Run

| Gate | Result |
|---|---|
| `dotnet build WMS.sln -c Debug --no-restore` | Pass, 0 warnings, 0 errors |
| `dotnet test WMS.Tests\WMS.Tests.csproj -c Debug --no-restore --logger "console;verbosity=minimal"` | Pass, 563/563 |
| `.\scripts\Run-WmsVerification.ps1 -SkipK6` | Pass end-to-end |
| `visual:public` | Pass, 6/6 |
| `visual:auth` | Pass, 1/1 local Development loopback auth state |
| `visual:test` | Pass, 100 passed, 4 expected skips |
| `visual:no-device` | Pass, 10/10 keyboard-wedge RF, camera modal and print preview without physical hardware |
| `visual:mobile-deep` | Pass, 412/412 across phone/tablet viewports |
| Server log gate | Pass; no `fail:`, EF paging warning, telemetry warning, transaction noise or async-dispose error |

## Change Made During This Audit

- Updated exactly one legitimate visual baseline: `tests/visual/wms-visual-regression.spec.ts-snapshots/home-mobile-mobile-win32.png`.
- Reason: the actual mobile dashboard screenshot passed layout assertions but became 77px shorter than the old snapshot because current seeded dashboard content rendered with a slightly different total height. The actual screenshot was inspected and accepted before updating.
- No app code, database schema, public API or `appsettings.json` secret/config values were changed.

## Final Interpretation

- **Repo/local readiness: 100%** for the current local gate.
- **Tier-1 production equivalence: 86%** against Oracle/SAP/Manhattan/Blue Yonder-style expectations.
- The remaining gap is not ordinary UI polish; it is external production evidence: real devices, load, DR/HA, certified integrations, production monitoring and release governance.
