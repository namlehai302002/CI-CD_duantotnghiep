# Enterprise WMS 100% Evidence Register

Ngày tạo: 2026-05-17

## Nguyên Tắc

- Không xóa hoặc sửa connection string/API key trong `appsettings.json` trong pass này.
- Không in secret value ra tài liệu, log hoặc checklist.
- Các mục cần staging, host, thiết bị thật hoặc backup thật phải ghi `Blocked`, không tick giả.
- `100%` nghĩa là local implementation complete cộng với evidence register trung thực cho artifact chưa có.

## Evidence Matrix

| ID | Gate | Status | Evidence | Owner action còn thiếu |
|---|---|---:|---|---|
| `EV-BUILD-001` | Build | Pass | `dotnet build WMS.sln --no-restore -v:minimal` | Re-run before release |
| `EV-TEST-001` | Regression tests | Pass | `dotnet test WMS.Tests\WMS.Tests.csproj --no-restore -v:minimal`, 523/523 latest known pass | Re-run after each patch |
| `EV-FORMAT-001` | Format verify | Pass | `dotnet format WMS.sln --verify-no-changes --no-restore -v:minimal` | Keep verify-only in this pass |
| `EV-MIG-001` | EF migration list | Pass | `dotnet ef migrations list --no-build` | Re-run against release branch |
| `EV-JS-001` | JavaScript syntax | Pass | `node --check` for runtime `wwwroot/js/*.js` | Re-run after script edits |
| `EV-MD-001` | Markdown/TS mojibake scan | Pass | Key audit docs regenerated/readable UTF-8; mojibake scan returned no matches for `CURRENTSTOCK_SOURCE_OF_TRUTH_AUDIT_2026_05_17.md`, `ENTERPRISE_WMS_100_PERCENT_REMAINING_TASKS_MOBILE_AUDIT_2026_05_17.md`, and this register | Re-run after docs/spec edits |
| `EV-VIS-PUBLIC-001` | Public auth/helpdesk visual | Pass | `npm run visual:public` with `WMS_BASE_URL=http://loopback host:5073`, 6/6 pass; screenshots and HTML report under `artifacts/visual-public` | Re-run after login/helpdesk UI changes |
| `EV-MOB-001` | Mobile CSS/static audit | Local-only | Shared mobile utilities and priority mobile view markers in source/tests | Needs real visual/device proof |
| `EV-STYLE-001` | Inline style inventory | Local-only | Full `rg -n "inline-style marker" Views -S` now returns 179 legacy matches; `WavePlanning`, `SerialReceiving`, `AbcAnalysis`, `Analytics`, `LpnLookup`, `Receiving` return 0 matches | Continue de-inline work outside priority scope |
| `EV-MINWIDTH-001` | Fixed-width scan | Local-only | `rg -n "min-width:\s*(680|760|860|980|1080|1180)px" wwwroot\css Views -S` returns 8 legacy matches; priority reports now have mobile card alternatives | Continue CSS debt cleanup |
| `EV-MOB-002` | Authenticated Playwright mobile visual | Blocked: needs auth state | `tests/visual/playwright.config.ts` has mobile project; `npm run visual:auth` was attempted but no valid `WMS_TEST_USER`/`WMS_TEST_PASSWORD` env was available | Provide MFA-safe test account or pre-created `WMS_AUTH_STATE` |
| `EV-MOB-003` | Real mobile device pass | Blocked: needs real device | Manual checklist defined in remaining-task file | Run iPhone/Android/tablet checklist |
| `EV-LOAD-001` | k6 staging load | Blocked: needs staging/k6 runtime | `tests/load/k6-wms-dod.js` now fails on login fallback/redirect false positives and writes summary artifacts | Install k6 and run against staging URL with auth/API key; enable mutation only with disposable seed |
| `EV-BACKUP-001` | Backup/restore drill | Blocked: needs staging/DB backup | Runbook/checklists exist | Execute restore drill and record RPO/RTO |
| `EV-SEC-001` | Production secret override | Blocked: needs host config | `appsettings.json` preserved per owner request | Configure Plesk/IIS/env override without printing values |
| `EV-CSTOCK-001` | CurrentStock source-of-truth | Local-only | Report filters and WAC/running stock cache now start from `InventoryBalanceService`; stock alert resolution no longer mutates `AsNoTracking()` entities | Add real multi-warehouse/owner runtime evidence |
| `EV-ERR-001` | User-safe error messages | Local-only | API/workflow and background outbox/snapshot/replenishment failure paths now use `UserSafeError.From(ex)`; `rg -n "ex\.Message" Controllers Services -S` returns 0 | Continue scan for new catch blocks |
| `EV-XSS-001` | Dynamic HTML escaping | Local-only | LPN, serial, slotting, warehouse-stock modal, barcode label fallback, QC item options, voucher detail submit forms, location suggestion modal and audit trail diff modal escape/runtime-render safely | Continue audit for legacy modal/table HTML |
| `EV-MINERU-001` | MinerU hosting decision | Local-only | `MINERU_HOST_DEPLOYMENT_GUIDE.md` documents InterData shared-host constraint | Use separate VPS/API before enabling |

## Mobile Audit Snapshot

| Area | Status | Notes |
|---|---:|---|
| Layout viewport/PWA meta | Pass | `_Layout.cshtml` has mobile viewport and PWA meta |
| RF receiving/picking/movement one-hand classes | Local-only | Shared classes added; still needs device proof |
| Voucher create mobile safety | Local-only | Table remains desktop-style, mobile safety handled by wrappers/classes; visual proof blocked |
| Report table mobile summaries | Local-only | Inventory, StockValuation, SpaceUtilization, AuditTrail, DockToStock and MovementTasks expose mobile card summaries |
| Scanner modal | Local-only | CSS has safe-area and visual spec includes mobile fit assertion |
| Public visual auth/helpdesk | Pass | Desktop/mobile screenshots produced for Login, AccessHelp and AccessHelpSent |
| Authenticated visual app routes | Blocked: needs auth state | Do not create fake auth/report artifacts |

## Production Secret Decision

Owner decision: keep existing `appsettings.json` values in repo during this pass because hosting is expected to protect deployment config.

100% production requirement remains: configure production overrides in host/environment/secret store and rotate production values if they were shared outside the trusted deployment path. Do not print values in evidence.


