# Enterprise WMS 100% Remaining Tasks And Mobile UI Audit

Ngày tạo: 2026-05-17  
Mục tiêu: đưa WMS từ mức hiện tại khoảng **91/100** lên chuẩn **100% Tier-1 parity nội bộ** so với Oracle WMS, Manhattan Active WM, SAP EWM, Blue Yonder WMS và Infor WMS.

Evidence register local/blocked được theo dõi tại `ENTERPRISE_WMS_100_PERCENT_EVIDENCE_REGISTER_2026_05_17.md`.

## 1. Nguyên Tắc Quan Trọng

- Không xóa connection string, API key hoặc các giá trị hiện có trong `appsettings.json` trong task này, theo quyết định vận hành của owner.
- Không in secret value ra tài liệu, log, report hoặc checklist.
- Khi lên production thật, có thể giữ file repo như hiện tại nhưng phải cấu hình override bằng host/Plesk/IIS environment/secret store và rotate giá trị production nếu từng bị gửi qua kênh không kiểm soát.
- “100%” ở đây là chuẩn nội bộ có bằng chứng vận hành, không phải chứng nhận chính thức từ Oracle, Manhattan, SAP, Blue Yonder hoặc Infor.

## 2. Kết Luận Hiện Tại

Điểm hiện tại sau remediation local + public visual evidence ngày 2026-05-18: **91/100**.

Lý do chưa thể gọi là 100%:

- Local gate đã tốt: build/test/format/migration pass, test hiện tại 523/523; public Playwright login/helpdesk visual 6/6 pass.
- Nhưng còn thiếu bằng chứng runtime thật: visual mobile/desktop có auth, k6 staging, backup/restore, DR, security rotation artifact, hardware/API/MHE/Carrier artifact.
- Mobile UI có nền tốt nhưng chưa đủ chứng cứ không lỗi trên thiết bị thật.
- Một số view vận hành còn inline style/fixed width/table lớn, có rủi ro tràn ngang hoặc khó thao tác bằng một tay trên mobile.

## 3. Evidence Đã Rà Soát

### Local Quality Gates

| Gate | Trạng thái hiện tại | Ghi chú |
|---|---:|---|
| `dotnet build WMS.sln --no-restore -v:minimal` | Pass | 0 warning, 0 error ở lần chạy gần nhất |
| `dotnet test WMS.Tests\WMS.Tests.csproj --no-restore -v:minimal` | Pass | 523/523 |
| `dotnet format WMS.sln --verify-no-changes --no-restore -v:minimal` | Pass | Format gate xanh |
| `dotnet ef migrations list --no-build` | Pass | Migration list đọc được |

### Mobile UI Static Audit

| Hạng mục | Kết quả |
|---|---|
| Viewport mobile | Có trong `Views/Shared/_Layout.cshtml`: `width=device-width, initial-scale=1.0` |
| PWA/mobile meta | Có manifest, theme color, mobile-web-app và apple-web-app meta |
| Visual mobile scaffold | Có project `mobile` trong `tests/visual/playwright.config.ts`, viewport `390x844`, device Pixel 7 |
| Auth visual artifact | Chưa có `tests/visual/.auth/wms-auth-state.json` |
| Visual report | Chưa có `tests/visual/playwright-report` |
| CSS responsive | `wwwroot/css/site.css` có 26 media query, nhiều token mobile/scanner/safe-area |
| Rủi ro table mobile | CSS còn các min-width lớn như 680/760/860/980/1080/1180px, chủ yếu dựa vào scroll ngang |
| Rủi ro inline style | Nhiều view vận hành còn `inline-style marker`, dễ gây layout khó kiểm soát trên mobile |

Top file cần ưu tiên audit mobile theo static scan sau pass local:

| File | Inline style count | Rủi ro |
|---|---:|---|
| `Views/Vouchers/Create.cshtml` | 52 | Bảng dòng phiếu phức tạp, Select2, scanner, thumbnail, nhiều cột; wrapper mobile đã có, JS/modal legacy còn follow-up |
| `Views/Reports/SpaceUtilization.cshtml` | 0 | Đã chuyển progress/table chính sang class và mobile card |
| `Views/Reports/DockToStock.cshtml` | 0 | Đã thêm mobile card và bỏ inline style chính |
| `Views/Reports/AuditTrail.cshtml` | 16 | Mobile card đã có; inline style còn nằm trong JS diff/modal legacy |
| `Views/Vouchers/Details.cshtml` | 10 | Nhiều bảng chi tiết, dock/packing sections; chưa nằm trong scope local ưu tiên |
| `Views/Operations/MovementTasks.cshtml` | 0 | Đã có mobile card/source table |
| `Views/Reports/StockValuation.cshtml` | 0 | Đã có mobile card/source table |
| `Views/Operations/RfReceiving.cshtml` | 0 | Đã chuyển sang RF one-hand classes; cần test thiết bị thật |
| `Views/Operations/RfPicking.cshtml` | 0 | Đã chuyển sang RF one-hand classes; cần test thiết bị thật |
| `Views/Operations/SerialReceiving.cshtml` | 0 | Inline layout chính đã chuyển sang class; vẫn cần kiểm tra focus/keyboard trên thiết bị thật |

## 4. P0 - Bắt Buộc Trước Khi Gọi 100%

### `P0-EVID-001` - Evidence Registry Thật

Việc cần làm:

- Tạo evidence registry có link artifact cho từng gate: build, test, format, migration, visual, load, backup, restore, security, staging smoke.
- Không cho tick “100%” nếu chỉ có claim mà không có artifact.

Acceptance:

- Có file/register ghi ngày chạy, người chạy, môi trường, command, kết quả và đường dẫn artifact.
- Mỗi claim Tier-1 đều có bằng chứng tương ứng.

### `P0-SEC-001` - Production Secret Override Không Xóa Repo Value

Việc cần làm:

- Giữ nguyên `appsettings.json` trong repo theo yêu cầu owner.
- Trên host production, cấu hình override connection/API key bằng Plesk/IIS environment/app settings hoặc secret store.
- Rotate production secrets nếu từng bị chia sẻ ngoài kênh tin cậy.

Acceptance:

- Có checklist production ghi rõ key nào được override, không in value.
- App production đọc config override thành công.
- Không commit/log secret value.

### `P0-MOB-001` - Chạy Visual Mobile Có Auth

Việc cần làm:

- Tạo auth state thật cho Playwright.
- Chạy visual project mobile.
- Lưu screenshot/report.

Command:

```powershell
$env:WMS_BASE_URL='https://<staging-host>'
npm run visual:auth
npm run visual:test -- --project=mobile
```

Acceptance:

- Có `tests/visual/.auth/wms-auth-state.json` hoặc auth state path tương đương.
- Có report/screenshot mobile cho các route trọng yếu.
- Không có text overlap, nút bị che, bảng làm vỡ viewport hoặc modal vượt màn hình.

### `P0-MOB-002` - Audit Mobile Manual Trên Thiết Bị Thật

Thiết bị/browser tối thiểu:

- iPhone SE width nhỏ.
- iPhone 14/15 Safari.
- Pixel 7 Chrome.
- Android mid-range Chrome.
- Tablet 768px.

Acceptance:

- Có checklist pass/fail cho login, dashboard, voucher create, item select thumbnail, RF receiving, RF picking, RF movement, scanner modal, item list, reports, dock/yard.
- Touch target tối thiểu 44px cho thao tác chính.
- Không có input bị keyboard che ở luồng RF.

### `P0-DATA-001` - CurrentStock Runtime Audit

Việc cần làm:

- Hoàn tất các mục trong `CURRENTSTOCK_SOURCE_OF_TRUTH_AUDIT_2026_05_17.md`.
- Chứng minh quyết định nhập/xuất/allocate/replenishment dùng `ItemLocation`/reservation/LPN scoped, không dùng `CurrentStock` làm source of truth.

Acceptance:

- Có test cùng SKU ở nhiều kho/chủ hàng.
- Xuất kho A không đọc nhầm tồn kho B.
- Reconciliation phát hiện lệch giữa `CurrentStock` cache và tổng `ItemLocation.Quantity`.

### `P0-SRE-001` - Backup, Restore, DR

Việc cần làm:

- Chạy backup DB thật trên staging/production-like.
- Restore sang môi trường khác.
- Ghi RPO/RTO.

Acceptance:

- Có file/log chứng minh backup và restore thành công.
- Có rollback plan cho migration.
- Có data protection key persistence nếu chạy multi-instance/hosting restart.

## 5. P1 - Mobile UI Và UX Cần Làm Thêm

### `P1-MOB-003` - Refactor Inline Style Trên View Mobile Trọng Yếu

Ưu tiên:

- `Views/Vouchers/Create.cshtml`
- `Views/Operations/RfReceiving.cshtml`
- `Views/Operations/RfPicking.cshtml`
- `Views/Operations/RfMovement.cshtml`
- `Views/Operations/MovementTasks.cshtml`
- `Views/Reports/Inventory.cshtml`
- `Views/Reports/StockValuation.cshtml`
- `Views/Reports/SpaceUtilization.cshtml`

Acceptance:

- Các view mobile trọng yếu không còn `inline-style marker` cho layout chính.
- Style chuyển về `wwwroot/css/site.css` bằng class tái sử dụng.
- Visual mobile không đổi xấu sau refactor.

Local pass 2026-05-17:

- Đã thêm shared mobile classes trong `wwwroot/css/site.css`.
- Đã xử lý RF receiving/picking/movement, movement tasks, inventory, stock valuation, space utilization và dock-to-stock về pattern mobile/hybrid.
- `Views/Vouchers/Create.cshtml` đã có wrapper mobile line editor; các inline style còn lại chủ yếu nằm trong HTML string/JS modal legacy nên tiếp tục theo dõi riêng, không claim sạch tuyệt đối.

### `P1-MOB-004` - Voucher Create Mobile Line Editor

Việc cần làm:

- Kiểm tra màn tạo phiếu trên mobile với nhiều dòng vật tư.
- Nếu bảng quá rộng, chuyển mobile sang dạng line-card hoặc có sticky item/qty/action rõ ràng.
- Thumbnail sản phẩm không làm tăng chiều rộng dòng quá mức.

Acceptance:

- Người dùng chọn vật tư, scan barcode, sửa số lượng, chọn lô/vị trí và xóa dòng bằng một tay.
- Select2 dropdown không vượt viewport.
- Add/delete row không gây nhảy layout.

### `P1-MOB-005` - RF Receiving/Picking/Movement One-Hand Mode

Việc cần làm:

- Tối ưu RF screens cho thao tác nhanh trong kho.
- Nút quét camera, input barcode, confirm button và lỗi phải nằm trong vùng dễ chạm.
- Keyboard mobile không che input chính.

Acceptance:

- RF receiving/picking/movement hoàn thành được trên màn 390x844.
- Camera scanner modal vừa màn, có safe-area, close button dễ chạm.
- Error/success state rõ trong điều kiện ánh sáng kho.

### `P1-MOB-006` - Report/Table Mobile Pattern

Việc cần làm:

- Các report nhiều cột cần mobile summary/card hoặc pinned key columns.
- Không chỉ dựa vào scroll ngang cho màn cần dùng thường xuyên.

Acceptance:

- `Inventory`, `StockValuation`, `SpaceUtilization`, `AuditTrail`, `DockToStock` có mobile pattern rõ.
- Không có bảng min-width lớn mà không có hướng thao tác/summary.

Local pass 2026-05-17:

- `Inventory`, `StockValuation`, `SpaceUtilization`, `AuditTrail` và `DockToStock` đã có mobile card/list song song với desktop table.
- Visual/device proof vẫn blocked cho đến khi có staging auth state và thiết bị thật.

### `P1-MOB-007` - Long Vietnamese Text And Zoom 125%

Việc cần làm:

- Kiểm tra nhãn dài tiếng Việt ở button, badge, table header, modal.
- Bảo đảm wrap/ellipsis đúng chỗ.

Acceptance:

- Không overlap ở mobile và desktop zoom 125%.
- Không dùng font scale theo viewport width.
- Button không vỡ text hoặc che icon.

### `P1-MOB-008` - Fix Visual Spec Encoding

Việc cần làm:

- Sửa các chuỗi mojibake còn trong `tests/visual/wms-visual-regression.spec.ts`.

Acceptance:

- Visual test labels readable UTF-8.
- Static mojibake scan không phát hiện pattern trong visual tests.

## 6. P1 - Runtime Proof Và Production Readiness

### `P1-LOAD-001` - k6 Staging Artifact

Command:

```powershell
$env:WMS_BASE_URL='https://<staging-host>'
k6 run tests/load/k6-wms-dod.js
```

Acceptance:

- Có kết quả p95 latency, error rate, throughput.
- Có profile 100/500/1000 hoặc profile phù hợp tài nguyên staging.

### `P1-OBS-001` - Observability End-To-End

Acceptance:

- Có trace từ UI -> controller -> service -> DB/outbox cho ít nhất một phiếu nhập và một phiếu xuất.
- Có dashboard lỗi, latency, request count, background job/outbox.

### `P1-API-001` - Integration Contract Evidence

Acceptance:

- Có OpenAPI/contract test.
- Webhook có signature/replay protection.
- Outbox retry/deadletter có dashboard và manual replay an toàn.

### `P1-HOST-001` - Hosting Checklist

Acceptance:

- WMS deploy được lên host đã chọn.
- Nếu dùng InterData shared ASP.NET: `MinerU` disabled hoặc trỏ sang VPS/API riêng.
- HTTPS, database connection, file upload, static files, logs và backup đều được kiểm tra.

## 7. P2 - Tier-1 Capability Depth

### `P2-LAB-001` - Labor Standard Real Shift Validation

Acceptance:

- Có dữ liệu ca thật/staging: pick rate, receive rate, travel time, exception.
- Manager review variance.

### `P2-OPT-001` - Optimization Benchmark

Acceptance:

- Slotting/pick path/wave/waveless có before-after metrics.
- Chứng minh giảm travel time, tăng pick rate hoặc tăng SLA.

### `P2-AUTO-001` - WCS/MHE Simulator Or Hardware Proof

Acceptance:

- Có command lifecycle: created, sent, acknowledged, completed, failed, retry, override.
- Có telemetry và incident path.

### `P2-3PL-001` - 3PL Billing Real Contract Pack

Acceptance:

- Test minimum fee, storage fee, handling fee, surcharge, tax/discount, dispute, invoice lock.
- Kế toán hoặc owner signoff.

### `P2-BI-001` - BI/AI Governance

Acceptance:

- Mỗi metric có định nghĩa, owner, source, scope kho/chủ hàng.
- AI/MinerU output có confidence/review và không auto-post inventory.

## 8. P3 - External Assessment Package

### `P3-DOC-001` - SOP And Training Pack

Acceptance:

- Hướng dẫn người dùng, SOP nhập/xuất/kiểm kê, incident, rollback, release, backup/restore được gom thành pack.

### `P3-DEMO-001` - Tier-1 Demo Script

Acceptance:

- Có kịch bản demo end-to-end: ASN -> receiving -> putaway -> replenishment -> wave/waveless -> pick -> pack -> ship -> invoice -> report.

### `P3-COMP-001` - External Review Bundle

Acceptance:

- Có kiến trúc, benchmark matrix, evidence registry, security checklist, screenshots, load report, runbook và release notes.

## 9. Lệnh Rà Soát Giữ Lại

```powershell
dotnet build WMS.sln --no-restore -v:minimal
dotnet test WMS.Tests\WMS.Tests.csproj --no-restore -v:minimal
dotnet format WMS.sln --verify-no-changes --no-restore -v:minimal
dotnet ef migrations list --no-build
$mojibakePattern = [string]::Join('|', @([char]0x00C3, [char]0x00C4, [char]0x00C6, ([char]0x00E1 + [char]0x00BA), ([char]0x00E1 + [char]0x00BB)))
rg -n $mojibakePattern -g "*.md" -g "*.ts" -S
rg -n "inline-style marker" Views -S
rg -n "min-width:\s*(680|760|860|980|1080|1180)px" wwwroot\css Views -S
npm run visual:auth
npm run visual:test
k6 run tests/load/k6-wms-dod.js
```

## 10. Định Nghĩa 100%

Chỉ gọi 100% khi tất cả điều kiện sau có bằng chứng:

- Local build/test/format/migration pass.
- Mobile visual pass, desktop visual pass, zoom 110/125 pass.
- Mobile manual test pass trên thiết bị thật.
- k6 staging pass.
- Backup/restore/DR pass.
- Production config override/secret rotation có checklist, không in secret value.
- CurrentStock chỉ còn là cache/display/sync, quyết định tồn dùng source scoped.
- Carrier/API/MHE/MinerU hoặc các integration chính có contract/health/retry evidence.
- SOP/training/runbook đủ để người mới vận hành không cần hỏi dev cho thao tác thường ngày.

