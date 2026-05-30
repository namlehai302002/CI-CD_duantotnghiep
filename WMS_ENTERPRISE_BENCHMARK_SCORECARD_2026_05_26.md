# WMS Enterprise Benchmark Scorecard - 2026-05-26

## Executive Result

Sau vòng audit ngày 26/05/2026, WMS Pro đạt **100% repo/local readiness** theo gate hiện có: static audit sạch, build sạch, .NET test sạch, visual public/auth/test/no-device/mobile-deep pass và server-log gate không có warning/error chặn.

Khi so với các hệ thống Tier-1 WMS như Oracle Fusion Cloud Warehouse Management, SAP EWM, Manhattan Active WM và Blue Yonder WMS, điểm production equivalence được chấm trung thực ở mức **86%**. Không ghi 100% production vì chưa có bằng chứng thật về scanner/printer/RF handheld, load lớn, backup/restore + HA/DR, dữ liệu production dài hạn và chứng nhận tích hợp ERP/TMS/OMS/MHE/carrier.

## Official Benchmark Basis

- Oracle Fusion Cloud Warehouse Management 25D nêu inbound master data, metadata, business entities, MHE inbound messages; outbound shipment verification, inventory transactions, wave/replenishment-pick messages, OBLPN shipping, route information, reports, labels, REST/SFTP/printer/file export. Source: https://docs.oracle.com/en/cloud/saas/supply-chain-and-manufacturing/25d/faips/wm-overview-of-integration-types.html
- SAP EWM official features nêu inbound receiving, stock ownership, physical inventory/cycle count, yard visibility, outbound wave pick/pack/ship, batch/serial/catch weight, scheduled dock appointment, labor, VAS, kitting, cross-docking và warehouse robotics. Source: https://www.sap.com/products/scm/extended-warehouse-management/features.html
- Manhattan Active WM được công bố là cloud-native, microservices-based WMS với real-time inventory visibility, intelligent automation, labor optimization, AI agents, transportation/yard unification và phù hợp cả Level 3-5 warehouse operations. Source: https://ir.manh.com/news-releases/news-release-details/manhattan-associates-recognized-leader-gartnerr-magic-quadranttm
- Blue Yonder WMS nêu warehouse operations, AI-powered orchestration, people/automation/resource coordination, yard/resource capability và future-ready warehouse management. Source: https://blueyonder.com/solutions/warehouse-management

## Weighted Score

| Area | Weight | Current | Weighted | Evidence basis |
|---|---:|---:|---:|---|
| Feature breadth | 30% | 85% | 25.5 | Inbound/outbound, inventory, RF/mobile, wave/tasking, slotting, yard, 3PL, labor, MHE/WCS, carrier, workflow, label/print and reporting are present locally. |
| Enterprise UI/UX | 20% | 97% | 19.4 | Dense enterprise shell, zero rough UI marker gate, desktop/zoom/mobile/tablet visual evidence, no-device RF/print and print/offline assets are green. |
| Business/security controls | 20% | 92% | 18.4 | CSRF, roles/permissions, warehouse/owner scope, safe-error handling, confirm/reason flows, audit trail, captcha/MFA guard and status-display gates are covered locally. |
| Integration/reporting | 15% | 82% | 12.3 | API/EDI/webhook/export/reporting foundations exist; certified partner contracts and production payload evidence remain external. |
| Production evidence | 15% | 68% | 10.2 | Local evidence is fully green; real hardware, load, DR/HA, backup/restore and long-running production proof remain external. |
| **Overall Tier-1 production equivalence** | **100%** |  | **85.8%** | Rounded to **86%**. |

## Repo/Local Evidence - 2026-05-26

- Strict included scan after small-bug regression pass: **674 authored files**, **568 text files**, **135 View/custom HTML files**.
- Excluded: `bin`, `obj`, `node_modules`, `artifacts`, `test-results`, `uploads`, `App_Data`, `wwwroot/lib`, visual auth state and runtime outputs.
- UTF-8 replacement-character files = 0; known Vietnamese corruption marker files = 0.
- View/custom HTML rough markers: inline `style=` = 0, local `<style>` = 0, inline event attributes = 0, root `href/src/action="/..."` = 0.
- Production browser debug marker in Views/custom `wwwroot`: `console.log(` = 0.
- Direct unsafe exception-message UI/API exposure = 0 outside the intentional `Common/UserSafeError.cs` safe-error helper.
- Broad EF paging scan still finds normal `Skip/Take` candidates, but runtime server-log gate passed with no EF `Skip/Take without OrderBy` warning.

## Test Quality Review

- `tests/visual/auth.setup.ts` uses env credentials or Development-only `LocalVerification` to create a Playwright storage state; it fails if MFA is reached instead of silently bypassing a real hosted account.
- Local verification and captcha/MFA bypass are guarded by Development environment, enabled local verification flags, configured username, loopback remote IP and loopback request host.
- Authenticated visual skips are expected and narrow: desktop-only checks are skipped on mobile, mobile-only checks are skipped on desktop projects.
- UOM regression no longer skips when selectable seed data is missing; it fails loudly so an empty source-unit dropdown cannot pass as green.
- TempData success/error notifications are locked to the shared `enterpriseNotify` toast helper and covered by the topbar collision visual gate.
- `tests/load/k6-wms-dod.js` remains optional production-load evidence. It requires explicit env/config, can reuse Playwright auth state, and does not log secret values.

## Verification Run

| Gate | Result |
|---|---|
| `dotnet build WMS.sln -c Debug --no-restore` | Pass, 0 warnings, 0 errors |
| `dotnet test WMS.Tests\WMS.Tests.csproj -c Debug --no-restore --logger "console;verbosity=minimal"` | Pass, 565/565, 0 skipped |
| `.\scripts\Run-WmsVerification.ps1 -SkipK6` | Pass end-to-end |
| `visual:public` | Pass, 6/6 |
| `visual:auth` | Pass, 1/1 local Development loopback auth state |
| `visual:test` | Pass, 103 passed, 9 expected skips |
| `visual:no-device` | Pass, 10/10 keyboard-wedge RF, camera modal and print preview without physical hardware |
| `visual:mobile-deep` | Pass, 412/412 across phone/tablet viewports |
| Server log gate | Pass; no `fail:`, EF paging warning, telemetry warning, transaction noise or async-dispose error |

## Changes Made During This Audit

- Hardened `Models/CustomExceptions.cs` so forbidden/not-found/internal problem details use safe fixed messages instead of raw exception details.
- Cleaned documentation/comment detector strings so zero-marker scans do not flag report-only text.
- Hardened `tests/load/k6-wms-dod.js` login fallback detection to use stable form markers instead of localized Vietnamese copy, keeping optional load evidence less brittle.
- Tightened `tests/visual/wms-visual-regression.spec.ts` so the voucher-create UOM gate fails if seeded selectable items are missing instead of skipping the source-unit regression.
- Added `WMS.Tests/EnterpriseUiUxPolishTests.cs` coverage proving TempData success/error toasts use the shared enterprise notification helper and remain tied to the visual topbar collision gate.
- No database schema, migration, public API, visual baseline or `appsettings.json` secret/config value was changed.

## Final Interpretation

- **Repo/local readiness: 100%** for the current local gate.
- **Tier-1 production equivalence: 86%** against Oracle/SAP/Manhattan/Blue Yonder-style expectations.
- Remaining gap is external production evidence: real devices, load, DR/HA, certified integrations, production monitoring and release governance.

## Addendum - 2026-05-27 Final Line-By-Line Repo/Local Audit

- Strict included scan after the final pass: **675 authored files**, **569 text files**, **129 View/custom HTML files**.
- Fixed the last real root-route marker group by replacing hard-coded internal JS routes with `Url.Action` in `Views/Items/Index.cshtml`, `Views/Reports/ScheduledReports.cshtml` and `Views/Vouchers/Details.cshtml`.
- Final static gates: UTF-8 replacement files = 0; known question-mark Vietnamese corruption files = 0; inline style/style block/inline event/root route/production `console.log` = 0; unsafe exception-message UI/API candidates = 0.
- UTF-8 sequence scan was manually classified: uppercase Vietnamese text such as `Mã`/`Luân` in source comments is valid Unicode text, not mojibake.
- Build gate: `dotnet build WMS.sln -c Debug --no-restore` passed with **0 warnings, 0 errors**.
- Test gate: `dotnet test WMS.Tests\WMS.Tests.csproj -c Debug --no-restore --logger "console;verbosity=minimal"` passed **565/565**, 0 failed, 0 skipped.
- Verification gate: `.\scripts\Run-WmsVerification.ps1 -SkipK6` passed end-to-end: visual public 6/6, auth 1/1, visual test 103 passed with 9 expected skips, no-device 10/10, mobile-deep 412/412 and server-log gate clean.
- Artifact status files are `passed` for `test-results/.last-run.json`, `artifacts/visual-no-device/test-results/.last-run.json` and `artifacts/visual-mobile-deep/test-results/.last-run.json`.
- Local verification ports checked after the run: 5073, 5299, 5300 and 5301 were closed.
- No `appsettings.json` secret/config value, database schema, migration, public API or visual baseline was changed during this addendum.

## Addendum - 2026-05-27 Final Ultra-Deep Tier-1 Completion Audit

- Re-ran strict repo/local audit on **675 authored files**, **568 text files**, **129 View/custom HTML files**.
- Final static gates after this pass: UTF-8 replacement files = 0; known question-mark Vietnamese corruption files = 0; inline style/style block/inline event/root asset route/production `console.log` = 0; unsafe exception-message app candidates = 0; hard-coded loopback app files outside local verification/config = 0.
- Fixed global ProblemDetails safety: `Program.cs` now routes error detail through `UserSafeError.From(exception, ...)` instead of exposing raw exception text in Development responses.
- Fixed RFC 7807 model safety: `Models/CustomExceptions.cs` now routes safe business details through `UserSafeError.From(...)` for business, SOD and warehouse-lock cases.
- Redacted evidence-script exception output: `scripts/Run-WmsVerification.ps1` and `scripts/Invoke-WmsDrEvidence.ps1` now write safe exception type codes instead of raw exception messages.
- Tightened 3PL financial workflow UX: `Views/Operations/ThreePlInvoiceDetails.cshtml` now requires enterprise confirm before locking invoices or resolving disputes, and requires a response note when approving/rejecting a dispute.
- Added regression coverage in `WMS.Tests/EnterpriseUiUxPolishTests.cs` for safe ProblemDetails, redacted evidence scripts and 3PL dispute confirm/reason behavior.
- POST antiforgery scan: raw POST forms missing antiforgery = 0.
- Sensitive action scan: no unclassified destructive/approve/execute form remains; existing category/partner/UOM/warehouse deletes use dedicated JS confirm handlers, inbound rejection uses prompt + reason, voucher cancellation uses SweetAlert + reason code.
- UOM, toast and AI assistant small-bug coverage remains active: voucher-create source-unit regression, enterprise toast topbar collision and inventory-first assistant response are covered by .NET/Playwright gates.
- Build gate: `dotnet build WMS.sln -c Debug --no-restore` passed with **0 warnings, 0 errors**.
- Test gate: `dotnet test WMS.Tests\WMS.Tests.csproj -c Debug --no-restore --logger "console;verbosity=minimal"` passed **566/566**, 0 failed, 0 skipped.
- Verification gate: `.\scripts\Run-WmsVerification.ps1 -SkipK6` passed end-to-end: visual public 6/6, auth 1/1, visual test 103 passed with 9 expected skips, no-device 10/10, mobile-deep 412/412 and current server-log gate clean.
- Artifact status files are `passed` for `test-results/.last-run.json`, `artifacts/visual-no-device/test-results/.last-run.json` and `artifacts/visual-mobile-deep/test-results/.last-run.json`.
- Current verification logs under `artifacts/verification` have no blocking `fail:`, EF paging warning, telemetry warning, transaction noise or async-dispose error.
- Local verification ports checked after the run: 5073, 5299, 5300 and 5301 were closed.
- No `appsettings.json` secret/config value, database schema, migration, public API or visual baseline was changed during this addendum.
- Final score remains **100% repo/local readiness** and **86% Tier-1 production equivalence**. The path to production 100% still requires real hardware certification, k6/staging load evidence, DR/HA drill evidence, certified ERP/TMS/OMS/MHE/carrier integration contracts and long-running production observability.

## Addendum - 2026-05-27 Ultra-Deep Final Small-Bug Audit

- Re-ran strict audit on **675 authored files**, **569 text files**, **129 View/custom HTML files**.
- Static gates remained clean: UTF-8 replacement files = 0; known question-mark Vietnamese corruption files = 0; inline style/style block/inline event/root route/production `console.log` = 0; unsafe exception-message UI/API candidates = 0.
- Fixed the last user-facing raw enum display found in the audit: `Views/Operations/YardBillingRates.cshtml` now shows Vietnamese enterprise labels for trailer type and yard spot type instead of raw enum names.
- Confirmed previous small-bug protections remain covered: voucher create source-unit/UOM regression, TempData/enterprise toast topbar collision, AI inventory answer source/wording, no-device RF/print and mobile-deep layout gates.
- Build gate: `dotnet build WMS.sln -c Debug --no-restore` passed with **0 warnings, 0 errors**.
- Test gate: `dotnet test WMS.Tests\WMS.Tests.csproj -c Debug --no-restore --logger "console;verbosity=minimal"` passed **565/565**, 0 failed, 0 skipped.
- Verification gate: `.\scripts\Run-WmsVerification.ps1 -SkipK6` passed end-to-end: visual public 6/6, auth 1/1, visual test 103 passed with 9 expected skips, no-device 10/10, mobile-deep 412/412 and server-log gate clean.
- Artifact status files are `passed` for `test-results/.last-run.json`, `artifacts/visual-no-device/test-results/.last-run.json` and `artifacts/visual-mobile-deep/test-results/.last-run.json`.
- Local verification ports checked after the run: 5073, 5299, 5300 and 5301 were closed.
- No `appsettings.json` secret/config value, database schema, migration, public API or visual baseline was changed during this addendum.

## Addendum - 2026-05-27 Final Repo/Local Ultra-Deep Audit

- Re-ran the strict scope audit on **675 authored files**, **569 text files**, **129 View/custom HTML files**.
- Final static gates after fixes: UTF-8 replacement files = 0; known question-mark Vietnamese corruption files = 0; inline style/style block/inline event/root asset route/production `console.log` = 0; unsafe exception-message UI/API candidates = 0; hard-coded loopback marker files = 0.
- Hardened Playwright auth setup: `tests/visual/auth.setup.ts` now reads `LocalVerification` credentials only when `WMS_BASE_URL` is loopback/local, so local captcha/MFA bypass cannot be accidentally exercised against a hosted URL.
- Added regression coverage in `WMS.Tests/EnterpriseAuditRemediationTests.cs` for the Playwright auth loopback guard.
- Added missing enterprise confirms/reason handling on sensitive forms:
  - `Views/Items/Index.cshtml`: delete item requires enterprise confirm.
  - `Views/Operations/CrossDockOpportunities.cshtml`: complete cross-dock task requires enterprise confirm.
  - `Views/Operations/LaborProductivity.cshtml`: approve/reject labor exception requires enterprise confirm.
  - `Views/Users/LoginHelpRequests.cshtml`: resolve/reset/reject login help requests require enterprise confirm, and rejection reason is required.
  - `Views/Operations/VasWorkOrderDetails.cshtml`: complete VAS operation requires enterprise confirm.
- POST form audit: raw POST forms missing antiforgery = 0.
- Destructive/approve/execute scan remaining candidates are classified false positives: cross-dock execution uses `enterpriseConfirm` through `.crossdock-btn`, and inbound stock approval uses `Swal.fire` through `approveBtn` before form submission.
- Build gate: `dotnet build WMS.sln -c Debug --no-restore` passed with **0 warnings, 0 errors**.
- Test gate: `dotnet test WMS.Tests\WMS.Tests.csproj -c Debug --no-restore --logger "console;verbosity=minimal"` passed **565/565**, 0 failed, 0 skipped.
- Verification gate: `.\scripts\Run-WmsVerification.ps1 -SkipK6` passed end-to-end: visual public 6/6, auth 1/1, visual test 103 passed with 9 expected skips, no-device 10/10, mobile-deep 412/412 and current verification server-log gate clean.
- Artifact status files are `passed` for `test-results/.last-run.json`, `artifacts/visual-no-device/test-results/.last-run.json` and `artifacts/visual-mobile-deep/test-results/.last-run.json`.
- Current verification logs under `artifacts/verification` have no blocking `fail:`, EF paging warning, telemetry warning, transaction noise or async-dispose error. Older excluded runtime artifacts may still contain historical logs from previous runs and were not deleted.
- Local verification ports checked after the run: 5073, 5299, 5300 and 5301 were closed.
- No `appsettings.json` secret/config value, database schema, migration, public API or visual baseline was changed during this addendum.

## Addendum - 2026-05-27 Vietnam Time And Core WMS Business Audit

- Re-ran the strict repo/local scope after the Vietnam-time pass: **676 included files**, **569 text files**, **129 View/custom HTML files**.
- Business time policy is now explicit in `Common/VietnamTime.cs`: `VietnamTime.Now`, `VietnamTime.Today`, `VietnamTime.IanaTimeZoneId` and `VietnamTime.FileStamp(...)` are the standard for user-facing warehouse dates, report defaults and export/document filenames.
- Fixed user-facing date defaults and file stamps to use Vietnam business time in stock snapshot, inventory transaction report, 3PL billing/rates/contracts, labor productivity, voucher creation, shipment-load export, SRE snapshot export, legacy receipt filenames and document-intake filenames.
- Fixed client-side date handling to use `Asia/Ho_Chi_Minh` instead of browser/UTC assumptions in layout export filenames, voucher OCR date normalization, dock-board live clocks and inventory-map expiry display.
- Fixed business timestamp display that previously converted already-local Vietnam operational timestamps as if they were UTC in home alerts, alert reports, audit trail, users, login-help requests and voucher details.
- Final time-policy static scan has no unclassified `DateTime.Now`, `DateTime.Today`, `DateTime.UtcNow`, `DateTimeOffset.Now/UtcNow`, `ConvertTimeFromUtc`, or `toISOString().slice/substr(0,10)` usage in business UI/app code. Remaining UTC usage is intentionally limited to `Common/VietnamTime.cs`, `Controllers/AccountController.cs` and `Services/MobileSecurityServices.cs` for helper/security/token fields with `Utc` semantics.
- Added `WMS.Tests/VietnamTimeBusinessPolicyTests.cs` to lock the policy: business code must not introduce direct clock usage, key screens must pin date defaults/client clocks to Vietnam time, and export/document filenames must use Vietnam timestamp helpers.
- Updated audit evidence inventory count in `ENTERPRISE_FULL_SYSTEM_DEEP_AUDIT_2026_05_13.md` from 26 to 27 WMS test files after adding the new regression test file.
- Build gate: `dotnet build WMS.sln -c Debug --no-restore` passed with **0 warnings, 0 errors**.
- Test gate: `dotnet test WMS.Tests\WMS.Tests.csproj -c Debug --no-restore --logger "console;verbosity=minimal"` passed **569/569**, 0 failed, 0 skipped.
- Verification gate: `.\scripts\Run-WmsVerification.ps1 -SkipK6` passed end-to-end: build clean, .NET tests 569/569, visual public 6/6, auth 1/1, visual test 103 passed with 9 expected skips, no-device 10/10, mobile-deep 412/412 and server-log gate clean.
- Artifact status files are `passed` for `test-results/.last-run.json`, `artifacts/visual-no-device/test-results/.last-run.json` and `artifacts/visual-mobile-deep/test-results/.last-run.json`.
- Local verification ports checked after the run: 5073, 5299, 5300, 5301, 5302 and 5303 were closed.
- No `appsettings.json` secret/config value, database schema, migration, public API or visual baseline was changed during this addendum.

## Addendum - 2026-05-27 Final Ultra-Deep Tier-1 Benchmark Implementation

- Rechecked official benchmark basis against current public vendor sources:
  - Oracle Fusion Cloud Warehouse Management 26x integration covers inbound/outbound integration, master/business entities, MHE messages, REST, SFTP, printer, file export, reports and labels.
  - SAP EWM covers inbound/outbound, stock ownership, cycle count, yard visibility, wave pick/pack/ship, batch/serial/catch weight, dock appointment, labor, VAS, kitting, cross-docking and robotics.
  - Manhattan Active WM emphasizes cloud-native microservices, real-time inventory from inbound through outbound, labor, slotting, automation, robotics and WES inside WMS.
  - Blue Yonder WMS emphasizes AI-powered orchestration, warehouse AI agents, resource forecasting/orchestration, robotics hub, labor, advanced slotting, load building, yard and returns.
- Strict repo/local scope after this run: **676 included files**, **569 text files**, **129 View/custom HTML files**.
- Static gates remained clean in authored scope: UI rough markers = 0, real mojibake markers = 0, unsafe exception-message candidates = 0 outside the safe-error helper, and business-time misuse remains classified to VietnamTime/security UTC boundaries only.
- Fixed one final user-facing raw enum candidate: `Controllers/OperationsController.Picking.cs` now maps shipping-board `VoucherTypeName` from the `Voucher.VoucherTypeName` display property instead of `VoucherType.ToString()`.
- Remaining enum `.ToString()` candidates are classified as API contract values, export/log fields or telemetry/status payloads, not direct user-facing labels in Views.
- Build gate: `dotnet build WMS.sln -c Debug --no-restore` passed with **0 warnings, 0 errors**.
- Test gate: `dotnet test WMS.Tests\WMS.Tests.csproj -c Debug --no-restore --logger "console;verbosity=minimal"` passed **569/569**, 0 failed, 0 skipped.
- Verification gate: `.\scripts\Run-WmsVerification.ps1 -SkipK6` passed end-to-end: build clean, .NET tests 569/569, visual public 6/6, auth 1/1, visual test 103 passed with 9 expected skips, no-device 10/10, mobile-deep 412/412 and server-log gate clean.
- Artifact status files are `passed` for `test-results/.last-run.json`, `artifacts/visual-no-device/test-results/.last-run.json` and `artifacts/visual-mobile-deep/test-results/.last-run.json`.
- Local verification ports checked after the run: 5073, 5299, 5300, 5301, 5302 and 5303 were closed.
- Final score remains **100% repo/local readiness** and **86% Tier-1 production equivalence**. Moving the production score toward 100% requires real RF/scanner/printer evidence, staging load evidence, DR/HA drill evidence, certified ERP/TMS/OMS/MHE/carrier integration contracts and long-running production observability.
- No `appsettings.json` secret/config value, database schema, migration, public API or visual baseline was changed during this addendum.
