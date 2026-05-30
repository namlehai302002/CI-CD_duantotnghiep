# WMS Tier-1 100% Benchmark And Task Backlog

Ngày tạo: 17/05/2026  
Chuẩn mục tiêu: parity nội bộ với Oracle WMS, Manhattan Active WM, SAP EWM, Blue Yonder WMS và Infor WMS.  
Phạm vi: mã nguồn, cấu hình, test, tài liệu vận hành và bằng chứng audit trong repo hiện tại.  

> "100%" trong tài liệu này là chuẩn nghiệm thu nội bộ dựa trên capability và bằng chứng vận hành. Đây không phải chứng nhận chính thức của Oracle, Manhattan, SAP, Blue Yonder hoặc Infor.

## 1. Executive Verdict

Điểm hiện tại: **82/100 - mức enterprise mạnh, chưa đủ 100% tier-1 evidence**.  
Độ tin cậy: **Medium-High** vì build/test/format/migration có bằng chứng tự động, nhưng visual, load, staging, secret rotation và DR vẫn thiếu artifact thật.

Kết quả kiểm tra mới nhất:

| Gate | Kết quả | Bằng chứng |
|---|---:|---|
| Build | Pass | `dotnet build WMS.sln --no-restore -v:minimal`, 0 warning, 0 error |
| Unit/regression tests | Pass | `dotnet test WMS.Tests\WMS.Tests.csproj --no-restore -v:minimal`, 500/500 pass |
| EF migrations list | Pass | `dotnet ef migrations list --no-build`, đọc được migration list |
| Format gate | Pass | `dotnet format WMS.sln --verify-no-changes --no-restore -v:minimal` |
| Visual regression | Blocked | `tests/visual/.auth` chỉ có `.gitignore`, chưa có auth state |
| Load test | Scaffold only | `tests/load/k6-wms-dod.js` có kịch bản, chưa có staging artifact |

Kết luận ngắn: hệ thống đã có nền WMS enterprise rất rộng, gồm core inventory, inbound/outbound, RF/mobile, yard/dock, 3PL billing, labor, MHE/integration, BI/AI, security và SRE scaffold. Tuy nhiên chưa được gọi là 100% vì còn thiếu bằng chứng runtime production-grade, secret/config cần externalize/rotate trước production, visual/load/staging/DR artifact chưa có và cần hoàn tất audit runtime mọi điểm dùng `CurrentStock`.

## 2. Tier-1 Benchmark Sources

Nguồn đối chiếu chính thức:

| Vendor | Năng lực tier-1 dùng để benchmark | Nguồn |
|---|---|---|
| Oracle WMS | Advanced wave, cross-docking, VAS, yard-to-dock visibility, labor, MHE/automation, omnichannel inventory | https://www.oracle.com/scm/logistics/warehouse-management/ |
| Manhattan Active WM | Cloud-native WMS, inventory visibility, order streaming, labor, robotics/automation orchestration, transportation workflow | https://www.manh.com/solutions/supply-chain-management-software/warehouse-management |
| SAP EWM | Inbound, storage/internal process control, outbound waves, dock appointment, yard, labor, VAS, kitting, cross-docking, robotics | https://www.sap.com/sea/products/scm/extended-warehouse-management/features.html |
| Blue Yonder WMS | Labor and automation orchestration, AI agents, resource forecasting/orchestration, Robotics Hub, slotting, load building, yard, returns | https://blueyonder.com/solutions/warehouse-management |
| Infor WMS | Labor management, yard/dock coordination, automation/hardware integration, analytics, flexible configuration, integration capability | https://www.infor.com/solutions/scm/warehouse-management-system/what-is-wms |

## 3. Weighted Benchmark Matrix

| Capability group | Weight | Current score | Evidence status | Gap to 100% |
|---|---:|---:|---|---|
| Core WMS: inbound, outbound, inventory, lot, serial, LPN, catch weight, stock count | 15 | 13 | Strong code and tests | Re-audit `CurrentStock`, runtime E2E evidence |
| Mobile/RF/offline queue | 8 | 7 | RF views, offline queue, tests | Authenticated mobile visual artifact |
| Yard, dock, carrier, shipping | 8 | 7 | Yard/dock/carrier modules and tests | Real staging smoke, carrier endpoint artifact |
| 3PL and multi-owner billing | 8 | 7 | 3PL billing models/services/views/tests | Accounting acceptance with real contract data |
| Labor management | 6 | 5 | Labor service/dashboard evidence | Engineered standard validation on real shifts |
| Optimization, slotting, wave, waveless | 8 | 6 | Slotting/wave/waveless code and tests | Optimizer benchmark before/after artifact |
| WCS/WES/MHE/robot automation | 7 | 5 | MHE command/telemetry framework | Simulator/hardware staging run evidence |
| Enterprise integration/API/EDI/webhook | 8 | 6 | API/connectors/outbox concepts | Contract/versioning/retry dashboard artifact |
| BI, AI, predictive, semantic layer | 7 | 5 | BI/AI/MinerU modules and tests | Production data governance and source citation audit |
| Security, tenant isolation, audit | 10 | 7 | RBAC, antiforgery, audit, scope tests | Secret rotation, staging security checklist evidence |
| UX, role workspace, documentation quality | 7 | 5 | Enterprise UI and docs exist | Visual regression, typo cleanup, mojibake cleanup |
| Production readiness, SRE, release evidence | 8 | 5 | Runbook/checklists/scaffolds | Format pass, load/backup/restore/DR artifacts |
| **Total** | **100** | **78** | **Good source/test evidence** | **Runtime evidence and hardening remain** |

## 4. Findings And Task IDs

### P0 Findings

| ID | Severity | Finding | File/line | Business impact | Acceptance criteria |
|---|---|---|---|---|---|
| `P0-QG-001` | High | Resolved: format gate previously failed on whitespace | `Controllers/AccountController.cs:538-539` | Quality gate can now pass locally, but must stay enforced in release | `dotnet format WMS.sln --verify-no-changes --no-restore -v:minimal` passes |
| `P0-SEC-001` | High | Live-looking secret/config values exist in app config | `appsettings.json` | Risk of leaked DB/API credentials and non-rotated secrets | Move production secrets to env/secret store, rotate all exposed values, keep repo placeholders only; do not print secret values in reports |
| `P0-DATA-001` | High | `CurrentStock` still has many usages and must remain shadow/compat field only | 159 matches across code/tests | Wrong stock by warehouse/owner if aggregate is used as source of truth | Create audit table of all runtime usages; every decision/report uses `ItemLocation` scoped by warehouse/owner or is explicitly documented as synced display cache |
| `P0-EVID-001` | High | Manual production evidence missing | `PRODUCTION_SECURITY_CHECKLIST.md`, `MODULE_ACCEPTANCE_CHECKLIST.md`, `PRODUCTION_MIGRATION_VALIDATION.md` | Cannot prove 100% production readiness without real staging evidence | Attach artifact links for security review, acceptance, migration dry-run, backup/restore, load and visual runs |
| `P0-DOC-001` | Medium | Several Markdown files render as mojibake | Multiple root `.md` files, highest counts in `HUONG_DAN_THUC_HANH_WMS_CHI_TIET.md`, `ENTERPRISE_NEXT_UPGRADE_ROADMAP.md`, `ENTERPRISE_WMS_100_PERCENT_TASKS.md` | Operational docs are hard to read and unsafe for handover | Regenerate or repair affected docs as UTF-8; add verification scan for mojibake patterns |

### P1 Findings

| ID | Severity | Finding | File/line | Business impact | Acceptance criteria |
|---|---|---|---|---|---|
| `P1-UX-001` | Medium | User-facing typo cleanup needed | `ViewModels/ViewModels.cs:229`, `Views/Operations/CartManagement.cshtml:10`, `Services/VasWorkOrderService.cs:74`, `Controllers/VouchersController.Import.cs:584`, `Controllers/OperationsController.Inventory.cs:971,987` | Users see unprofessional or confusing Vietnamese messages | Static scan finds no `khàng dược`, `khmng`, `dơn hàng`, `dếm`; affected tests updated |
| `P1-VIS-001` | Medium | Visual regression blocked by missing auth state | `tests/visual/.auth` | Cannot prove UI does not break on desktop/mobile/zoom | Create staging/auth state, run `npm run visual:auth`, run `npm run visual:test`, store screenshots/report |
| `P1-LOAD-001` | Medium | k6 load test has no real artifact | `tests/load/k6-wms-dod.js` | Cannot prove throughput/latency under production-like load | Run k6 against staging with 100/500/1000 profiles; store output and thresholds |
| `P1-OBS-001` | Medium | Observability must be proven end-to-end | `Program.cs`, `PRODUCTION_RUNBOOK.md` | Operators cannot diagnose incident without real traces/logs/metrics | Capture one transaction trace UI -> controller -> service -> DB/outbox and link dashboard snapshot |
| `P1-API-001` | Medium | API/integration contract evidence needs hardening | `Controllers/ApiIntegrationController.cs`, integration services | Integrations can break silently across releases | Add versioned API contract tests and retry/dead-letter dashboard artifact |

## 5. 100% Backlog

### P0 - Must Fix Before Claiming Tier-1 100%

- [x] `P0-QG-001` Fix whitespace at `AccountController.cs:538-539`.
  - Acceptance: format, build and test all pass.
- [ ] `P0-SEC-001` Externalize and rotate secrets/config.
  - Acceptance: no production secret value in repo config; rotated values confirmed in secret store or environment; checklist updated without printing secrets.
- [ ] `P0-DATA-001` Complete `CurrentStock` source-of-truth audit.
  - Acceptance: all decisions use scoped `ItemLocation`; remaining `CurrentStock` usages are display/sync/test-only or have explicit justification.
- [ ] `P0-EVID-001` Establish evidence registry.
  - Acceptance: every 100% item links to artifact, command output, screenshot, load report, staging record or manual signoff.
- [x] `P0-DOC-001` Repair mojibake Markdown and checklists.
  - Acceptance: affected root docs are readable UTF-8; scan gate catches future mojibake.

### P1 - Runtime Proof And User-Facing Quality

- [x] `P1-UX-001` Fix typo strings and add static typo test.
  - Acceptance: no known typo patterns remain; affected UI/business messages read cleanly.
- [x] `P1-IMG-001` Add compact product-image support to high-confusion flows.
  - Acceptance: voucher item selector and item list show thumbnail/fallback from `Item.ImageUrl`; upload guards remain enforced; SKU/barcode/location remain source controls.
- [ ] `P1-VIS-001` Run authenticated Playwright visual suite.
  - Acceptance: screenshots for dashboard, vouchers, inbound approval, inventory map, optimization, automation, integration, workflow profiles, dock board and yard management at desktop 100/110/125 plus mobile.
- [ ] `P1-LOAD-001` Run k6 load profiles.
  - Acceptance: 100/500/1000 profile artifacts stored; p95 latency and error rate thresholds documented.
- [ ] `P1-OPS-001` Complete production checklist evidence.
  - Acceptance: `PRODUCTION_SECURITY_CHECKLIST.md`, `MODULE_ACCEPTANCE_CHECKLIST.md` and `PRODUCTION_MIGRATION_VALIDATION.md` have real staging/ops evidence links.
- [ ] `P1-SRE-001` Prove backup/restore and DR.
  - Acceptance: successful restore drill with RPO/RTO, data protection key persistence and rollback note.

### P2 - Tier-1 Capability Depth

- [ ] `P2-LAB-001` Validate labor standards against real operations.
  - Acceptance: engineered standard by task/zone/shift, variance report, manager review workflow.
- [ ] `P2-OPT-001` Benchmark optimizer results.
  - Acceptance: slotting/pick path/wave/waveless before-after metrics show travel, pick rate or SLA improvement.
- [ ] `P2-AUTO-001` Stage WCS/MHE simulator or hardware test.
  - Acceptance: command lifecycle, telemetry, retry, manual override and exception case verified end-to-end.
- [ ] `P2-3PL-001` Validate 3PL billing with real contract scenarios.
  - Acceptance: tiered rates, minimums, surcharges, tax/discount/adjustment, invoice settlement and dispute workflow reconciled with accounting sample.
- [ ] `P2-INT-001` Harden integration contracts.
  - Acceptance: API versioning, OpenAPI/contract test, webhook signature, replay, dead-letter and connector health evidence.
- [ ] `P2-BI-001` Govern BI/AI outputs.
  - Acceptance: BI metrics have definitions, owner/warehouse scope, citation/source link and no cross-tenant leakage.

### P3 - Market-Grade Polish

- [ ] `P3-UX-001` Finish role-based enterprise UX polish.
  - Acceptance: Admin/Manager/Staff/Viewer workspaces are validated by role and do not show irrelevant actions.
- [ ] `P3-DOC-001` Build full handover pack.
  - Acceptance: onboarding, runbook, incident, release, backup/restore, integration and module acceptance docs are readable and linked.
- [ ] `P3-COMP-001` Prepare external assessment package.
  - Acceptance: benchmark matrix, architecture diagram, evidence registry, release notes, security checklist and demo script are bundled.

## 6. Verification Commands

Run these commands for every release candidate:

```powershell
dotnet build WMS.sln --no-restore -v:minimal
dotnet test WMS.Tests\WMS.Tests.csproj --no-restore -v:minimal
dotnet format WMS.sln --verify-no-changes --no-restore -v:minimal
dotnet ef migrations list --no-build
```

Runtime evidence commands:

```powershell
$env:WMS_BASE_URL='https://<staging-host>'
npm run visual:auth
npm run visual:test
k6 run tests/load/k6-wms-dod.js
```

Static scans to keep:

```powershell
rg -n "khàng dược|khmng|dơn hàng|dếm" Controllers Services Models Data ViewModels Views Common wwwroot\js wwwroot\css
rg -n "CurrentStock" Controllers Services Models Data ViewModels Views Common WMS.Tests
```

## 7. Definition Of Done For 100%

- Build, test, format and migration gates pass.
- No production secret values remain in repo config; all exposed values are rotated.
- Every module has business, security/scope and UI/runtime evidence.
- Visual regression passes on authenticated desktop and mobile flows.
- Load test artifacts exist for agreed profiles and thresholds.
- Backup/restore and DR drills have dated evidence.
- `CurrentStock` is not used as inventory source of truth.
- User-facing Vietnamese text is clean, readable and UTF-8.
- Every benchmark capability is either implemented with evidence or explicitly marked out of scope.

## 8. Current Percentage Rationale

The repo is not a simple CRUD WMS anymore. It already contains broad enterprise capabilities, many migrations, 500 passing tests, source-level modules for yard, 3PL, labor, optimization, MHE, integration, BI/AI and SRE. That earns a strong score.

The missing 18 points are evidence and hardening debt: secrets/config exposure, missing authenticated visual artifact, missing load/staging/DR evidence, production backup/restore proof, and final `CurrentStock` source-of-truth runtime audit. When remaining P0/P1 evidence is complete with artifacts, expected score is 90-93. P2/P3 evidence and real operational validation are required before calling it 100%.

## 9. Implementation Update - 17/05/2026

- Product image UX: use existing `Item.ImageUrl`; add compact thumbnail/fallback in voucher item Select2 results, selected voucher line preview and item list. Image is evidence/support for nhận diện, not a replacement for SKU, barcode, lot, location or scan validation.
- Benchmark note for product images: Oracle WMS exposes item image data via an item-image endpoint/field but avoids returning large image data by default; Manhattan Active WM describes workflows with built-in instructions, images and actions; SAP EWM keeps warehouse-number-dependent product master data as the operational base. Therefore this repo uses small thumbnails only in high-confusion flows and keeps SKU/barcode/location as the control fields.
- Product image sources checked: https://docs.oracle.com/en/cloud/saas/warehouse-management/21d/owmre/item-image.html, https://www.manh.com/solutions/supply-chain-management-software/warehouse-management, https://help.sap.com/docs/SAP_S4HANA_ON-PREMISE/9832125c23154a179bfa1784cdc9577a/4fc7441636fe1b10e10000000a42189d.html
- MinerU hosting: `MINERU_HOST_DEPLOYMENT_GUIDE.md` regenerated as readable UTF-8 and documents the InterData shared ASP.NET case. WMS can deploy there; MinerU should run on separate VPS/internal server/external API unless provider explicitly allows Python/Docker/background custom service and enough storage/RAM.
- App config decision: `appsettings.json` values are intentionally left unchanged per owner request in this implementation pass. For 100% tier-1 production evidence, externalize and rotate production secrets remains a P0 operating task; secret values must not be printed in reports.
- CurrentStock audit: `CURRENTSTOCK_SOURCE_OF_TRUTH_AUDIT_2026_05_17.md` records source-of-truth rules, existing tests and the remaining runtime paths that must prove they sync from `ItemLocation`.
- Evidence blockers: authenticated Playwright visual state, real staging k6 load artifact, backup/restore drill and production security checklist evidence remain required before claiming 100%.
- Remaining 100% backlog and mobile UI audit are tracked in `ENTERPRISE_WMS_100_PERCENT_REMAINING_TASKS_MOBILE_AUDIT_2026_05_17.md`.
- Local/blocked artifact status is tracked in `ENTERPRISE_WMS_100_PERCENT_EVIDENCE_REGISTER_2026_05_17.md`; no production secret values are printed there.

