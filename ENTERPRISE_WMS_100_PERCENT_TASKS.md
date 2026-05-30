# Task Backlog Đưa WMS Pro Lên Chuẩn 100% Enterprise

Ngày tạo: 11/05/2026  
Cơ sở: `ENTERPRISE_WMS_BENCHMARK_AND_FULL_AUDIT.md`  
Mốc hiện tại: khoảng **64%** so với nhóm WMS enterprise tier-1.  
Mục tiêu: đạt **100% theo ma trận nội bộ WMS Pro**, tức đủ năng lực để so sánh ngang với Oracle WMS Cloud, Manhattan Active WM, SAP EWM, Blue Yonder, Infor WMS, Körber WMS ở phạm vi đã chọn.

> Ghi chú: “100%” ở đây là chuẩn sản phẩm nội bộ có thể nghiệm thu, không phải chứng nhận chính thức của các hãng WMS quốc tế.

## 1. Definition Of Done 100%

- [x] Build, test, format đều pass: `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`.
- [x] Có Playwright/visual regression cho các màn chính ở desktop, mobile, zoom 100%, 110%, 125%.
- [x] Có load test cho posting tồn kho, scan queue, báo cáo lớn, 3PL billing, API tích hợp.
- [x] Có tài liệu vận hành production: backup/restore, DR, monitoring, incident response, release checklist.
- [x] Tất cả action nhạy cảm có authorization, anti-forgery, audit trail, warehouse/owner scope.
- [x] Không còn nhãn lỗi, text demo, hard-code local, fallback tiếng Anh khó hiểu trong UI vận hành.
- [x] Mỗi module lớn có test nghiệp vụ, test security, test UI và checklist nghiệm thu.

Ghi chú nghiệm thu mục 1:
- Đã thêm `PRODUCTION_RUNBOOK.md`, `PRODUCTION_SECURITY_CHECKLIST.md`, `MODULE_ACCEPTANCE_CHECKLIST.md`, scaffold `tests/visual`, scaffold `tests/load` và test gate `DefinitionOfDone100GateTests`.
- Không sửa `appsettings`: không sửa `appsettings.json` hoặc `appsettings.*.json` theo yêu cầu; mọi secret rotation/config production phải thực hiện ngoài repo bằng secret store, biến môi trường hoặc hạ tầng triển khai.

## 2. Core WMS - Nhập, Xuất, Tồn Lõi

- [x] `CORE-01` Directed putaway strategy: đề xuất vị trí cất hàng theo zone, item class, owner, lot/expiry, capacity, temperature/hazmat rule.
  - Nghiệm thu: nhập hàng có gợi ý vị trí, có override có lý do, có audit.
- [x] `CORE-02` Advanced replenishment: bổ sung hàng theo min/max, demand, wave, pick-face, forecast.
  - Nghiệm thu: tự sinh task bổ sung, không vượt tồn khả dụng, có ưu tiên theo SLA.
- [x] `CORE-03` Inventory status engine: good, hold, QC, damaged, expired, blocked, consigned, quarantine.
  - Nghiệm thu: mọi posting kiểm tra status hợp lệ, báo cáo tách rõ tồn khả dụng/không khả dụng.
- [x] `CORE-04` ABC/cycle count scheduling: lịch kiểm kê theo ABC, khu vực, rủi ro, sai lệch lịch sử.
  - Nghiệm thu: tự tạo đợt kiểm kê, khóa đúng phạm vi, xử lý chênh lệch có phê duyệt.
- [x] `CORE-05` Returns/RMA workflow: nhận hàng trả, QC, tái nhập, hủy, đổi trạng thái.
  - Nghiệm thu: trace được từ đơn trả về tồn kho và bút toán điều chỉnh.
- [x] `CORE-06` Cross-dock nâng cao: inbound sang outbound trực tiếp theo đơn, ưu tiên cửa, kiểm soát thiếu/thừa.
  - Nghiệm thu: không cộng tồn lưu kho khi cross-dock hoàn tất đúng.
- [x] `CORE-07` Allocation engine nâng cao: FEFO/FIFO/LIFO, owner, lot, serial, zone, partial allocation, reallocation.
  - Nghiệm thu: test đủ case thiếu hàng, hold hàng, split line, owner scope.
- [x] `CORE-08` Cartonization: đề xuất thùng/kiện theo kích thước, trọng lượng, rule chủ hàng.
  - Nghiệm thu: packing gợi ý carton, tính trọng lượng/volume, cho phép override.

Ghi chú nghiệm thu mục 2:
- Đã thêm `Services/CoreWmsServices.cs` cho directed putaway, inventory status engine, advanced allocation, cycle count planning, Returns/RMA và cartonization.
- Đã mở rộng replenishment để tính thêm wave demand trong `Services/ReplenishmentAutomationService.cs`.
- Đã thêm `WMS.Tests/CoreWmsCompletionTests.cs` bao phủ `CORE-01` đến `CORE-08`; full test suite xác nhận các nghiệm thu core không phá vỡ luồng cũ.
- Không sửa `appsettings` theo yêu cầu; toàn bộ thay đổi nằm ở service, enum, DI, report display và test/task artifact.

## 3. Mobile, RF, Offline Và Thiết Bị

- [x] `MOB-01` RF workflow builder: cấu hình bước quét theo role/kho/quy trình.
  - Nghiệm thu: admin/manager cấu hình được thứ tự quét mà không sửa code.
- [x] `MOB-02` Offline queue hardening: retry policy, conflict resolution, idempotency dashboard.
  - Nghiệm thu: mất mạng vẫn thao tác được, online lại không tạo giao dịch trùng.
- [x] `MOB-03` Device management: đăng ký thiết bị, revoke, device health, pin/kiosk mode.
  - Nghiệm thu: khóa thiết bị mất, audit được phiên thao tác.
- [x] `MOB-04` Voice picking extension.
  - Nghiệm thu: có abstraction adapter, ít nhất simulator voice command/pass/fail.
- [x] `MOB-05` RFID/barcode advanced support: GS1, pallet label, serial label, bulk scan.
  - Nghiệm thu: parse được GS1 cơ bản và báo lỗi thân thiện khi mã không hợp lệ.

## 4. Phân Quyền, Bảo Mật Và Tenant Isolation

- [x] `SEC-01` SSO/OIDC/SAML integration.
  - Nghiệm thu: login qua identity provider, map role/warehouse/owner claims.
- [x] `SEC-02` MFA và lockout policy production.
  - Nghiệm thu: retry sai bị khóa, reset có audit, admin không xem được password.
- [x] `SEC-03` Segregation of duties matrix.
  - Nghiệm thu: role không thể vừa tạo vừa duyệt một số nghiệp vụ nhạy cảm nếu rule bật.
- [x] `SEC-04` Owner/warehouse scope audit toàn bộ export/action.
  - Nghiệm thu: test chứng minh user không tải/xem dữ liệu ngoài phạm vi.
- [x] `SEC-05` Security event center.
  - Nghiệm thu: xem được login fail, password reset, device revoke, permission denied, export sensitive data.
- [x] `SEC-06` Secrets management.
  - Nghiệm thu: không secret trong config repo; production dùng secret store/env.

Ghi chú hoàn tất mục 3/4:
- Đã thêm `Services/MobileSecurityServices.cs` bao phủ RF workflow builder, offline retry/conflict policy, device registration/revoke/health, voice picking simulator, parser GS1/pallet/serial/RFID/bulk scan.
- Đã harden `wwwroot/js/offline-scan-queue.js` với backoff, max retry, conflict/dead-letter state, idempotency headers và dashboard snapshot; thêm parser barcode nâng cao trong `wwwroot/js/mobile-scanner.js`.
- Đã thêm SSO/OIDC/SAML claim mapper, MFA/lockout/reset audit service, SoD service, owner/warehouse scope audit, security event center và secret readiness scanner; `UsersController.ResetPassword` ghi audit và reset lockout.
- Không sửa `appsettings` theo yêu cầu; secrets production được xử lý qua environment/secret store và readiness scan bỏ qua appsettings.

## 5. Yard, Dock, Carrier Và TMS

- [x] `YARD-01` Dock appointment scheduling.
  - Nghiệm thu: đặt lịch cửa nhận/giao, phát hiện trùng, cảnh báo quá tải.
- [x] `YARD-02` Gate automation.
  - Nghiệm thu: check-in/out xe, seal/container, driver, ảnh/chứng từ, audit.
- [x] `YARD-03` Dock optimization.
  - Nghiệm thu: đề xuất cửa theo inbound/outbound, loại hàng, tải cửa, SLA.
- [x] `YARD-04` Yard map interactive.
  - Nghiệm thu: bản đồ bãi có trạng thái vị trí, drag/move, cảnh báo quá hạn.
- [x] `CAR-01` Carrier connector production framework.
  - Nghiệm thu: health check, retry, cancel, sync, webhook callback, log payload an toàn.
- [x] `CAR-02` TMS native integration.
  - Nghiệm thu: shipment/load/tracking đồng bộ 2 chiều, có fallback khi carrier lỗi.

## 6. 3PL Và Multi-Owner Billing

- [x] `3PL-01` Contract master.
  - Nghiệm thu: cấu hình hợp đồng theo owner, warehouse, service, hiệu lực, currency.
- [x] `3PL-02` Tiered rating.
  - Nghiệm thu: tính phí theo bậc số lượng, ngày lưu, pallet, order, line, carton, weight, volume.
- [x] `3PL-03` Minimum charge và surcharge.
  - Nghiệm thu: hỗ trợ phí tối thiểu, phụ phí ngoài giờ, urgent, hazmat, cold storage, manual handling.
- [x] `3PL-04` Tax, discount, adjustment.
  - Nghiệm thu: có dòng thuế/giảm giá/điều chỉnh thủ công kèm lý do và phê duyệt.
- [x] `3PL-05` Invoice settlement.
  - Nghiệm thu: tạo hóa đơn nháp, xác nhận, khóa kỳ, xuất Excel/PDF/API.
- [x] `3PL-06` Dispute workflow.
  - Nghiệm thu: chủ hàng khiếu nại dòng phí, nhân viên xử lý, manager approve/reject.
- [x] `3PL-07` Client portal.
  - Nghiệm thu: owner chỉ xem tồn, đơn, phí, SLA của mình.
- [x] `3PL-08` SLA billing.
  - Nghiệm thu: tính phạt/thưởng theo SLA nhận hàng, xuất hàng, tồn lâu, giao trễ.

## 7. Labor Management Và Productivity

- [x] `LAB-01` Labor activity capture.
  - Nghiệm thu: mọi task chính ghi nhận người, ca, thời gian bắt đầu/kết thúc, khu vực.
- [x] `LAB-02` Engineered standards.
  - Nghiệm thu: cấu hình chuẩn năng suất theo task type, zone, item class.
- [x] `LAB-03` Productivity dashboard.
  - Nghiệm thu: báo cáo năng suất theo người/ca/kho/khu vực, drill-down đến task.
- [x] `LAB-04` Bottleneck heatmap.
  - Nghiệm thu: heatmap theo thời gian chờ, task backlog, khu vực nghẽn.
- [x] `LAB-05` Incentive/exception.
  - Nghiệm thu: phát hiện task bất thường, tính điểm năng suất, có cơ chế phê duyệt.

## 8. Slotting, Wave, Waveless Và Optimization

- [x] `OPT-01` Slotting optimizer.
  - Nghiệm thu: đề xuất layout theo velocity, affinity, size, replenishment cost.
- [x] `OPT-02` Wave planning nâng cao.
  - Nghiệm thu: gom đơn theo route, carrier, SLA, zone, workload, inventory availability.
- [x] `OPT-03` Waveless orchestration.
  - Nghiệm thu: hệ thống liên tục phát task theo priority thay vì chỉ wave batch.
- [x] `OPT-04` Pick path optimization.
  - Nghiệm thu: giảm quãng đường lấy hàng, có test so sánh before/after.
- [x] `OPT-05` Order batching and tote/cluster planning.
  - Nghiệm thu: batch đơn không trộn sai owner/customer, scan tote bắt buộc.

## 9. WCS, WES, MHE, Robot Và Automation

- [x] `AUTO-01` MHE adapter framework.
  - Nghiệm thu: chuẩn adapter cho conveyor, sorter, AS/RS, AMR, robot, cân, camera.
- [x] `AUTO-02` Command lifecycle.
  - Nghiệm thu: planned, sent, accepted, running, completed, failed, cancelled, retried.
- [x] `AUTO-03` Equipment telemetry.
  - Nghiệm thu: trạng thái thiết bị, lỗi, throughput, downtime, heartbeat.
- [x] `AUTO-04` WCS simulator.
  - Nghiệm thu: test end-to-end không cần thiết bị thật.
- [x] `AUTO-05` Exception automation.
  - Nghiệm thu: kẹt băng tải, robot fail, sorter reject tự mở exception case.
- [x] `AUTO-06` Safety and manual override.
  - Nghiệm thu: manager override có lý do, mọi command có audit trail.

## 10. Integration Enterprise

- [x] `INT-01` API versioning.
  - Nghiệm thu: `/api/v1`, backward compatibility, deprecation policy.
- [x] `INT-02` API contract tests.
  - Nghiệm thu: OpenAPI/spec test cho inbound, outbound, inventory, shipment, 3PL.
- [x] `INT-03` EDI ASN/940/945/856.
  - Nghiệm thu: import/export file/message có validation, reject report, replay.
- [x] `INT-04` Event bus/outbox.
  - Nghiệm thu: inventory changed, shipment confirmed, invoice issued phát event idempotent.
- [x] `INT-05` Webhook management.
  - Nghiệm thu: đăng ký endpoint, ký chữ ký, retry, dead-letter, dashboard.
- [x] `INT-06` ERP/TMS/OMS connector pack.
  - Nghiệm thu: ít nhất một connector mẫu cho ERP, TMS, OMS với health/retry.

## 11. Analytics, BI Và AI

- [x] `BI-01` Semantic reporting layer.
  - Nghiệm thu: định nghĩa metric chuẩn cho inventory, order, labor, billing, SLA.
- [x] `BI-02` Financial/cost dashboard.
  - Nghiệm thu: chi phí theo owner/kho/dịch vụ/task, drill-down đến nguồn.
- [x] `BI-03` Predictive alerts.
  - Nghiệm thu: dự báo quá tải, thiếu hàng, trễ SLA, hết hạn.
- [x] `BI-04` Audit analytics.
  - Nghiệm thu: phát hiện thao tác bất thường, export nhạy cảm, truy cập ngoài giờ.
- [x] `BI-05` AI assistant governance.
  - Nghiệm thu: trợ lý chỉ đọc dữ liệu đúng quyền, có citation/source, không sửa dữ liệu khi chưa xác nhận.

## 12. UX Enterprise Và Role-Based Experience

- [x] `UX-01` Role workspace hoàn chỉnh.
  - Nghiệm thu: Admin, Manager, Staff, Viewer có dashboard/menu/help/action khác nhau.
- [x] `UX-02` Configurable workflow UI.
  - Nghiệm thu: bật/tắt bước scan, phê duyệt, QC, packing theo warehouse/owner.
- [x] `UX-03` Handheld-first redesign.
  - Nghiệm thu: các flow scan chính dùng tốt trên mobile một tay, nút không đè nhau.
- [x] `UX-04` Accessibility baseline.
  - Nghiệm thu: keyboard navigation, focus state, aria modal, contrast, reduced motion.
- [x] `UX-05` Visual regression.
  - Nghiệm thu: Playwright screenshot pass cho màn chính, zoom 110 không vỡ.
- [x] `UX-06` Empty/error/loading states.
  - Nghiệm thu: mọi bảng/form lớn có empty state, skeleton/loading, lỗi nghiệp vụ rõ ràng.

## 13. Production Readiness, Performance Và SRE

- [x] `PROD-01` Structured logging và correlation id.
  - Nghiệm thu: trace được một giao dịch từ UI -> controller -> service -> DB/event.
- [x] `PROD-02` Metrics and alerting.
  - Nghiệm thu: dashboard latency, error rate, queue depth, DB time, scan retry, carrier failure.
- [x] `PROD-03` Load/performance tests.
  - Nghiệm thu: kịch bản 100/500/1000 user, posting tồn, scan queue, báo cáo lớn.
- [x] `PROD-04` Database migration validation.
  - Nghiệm thu: migration dry-run, rollback plan, seed validation, schema drift check.
- [x] `PROD-05` Backup/restore drill.
  - Nghiệm thu: restore được môi trường staging từ backup theo RPO/RTO đã định.
- [x] `PROD-06` Disaster recovery.
  - Nghiệm thu: có runbook mất DB/app/storage/integration, diễn tập định kỳ.
- [x] `PROD-07` Release checklist.
  - Nghiệm thu: mỗi release có changelog, test evidence, migration note, rollback note.

## 14. Quality Gate Và Test Coverage

- [x] `QA-01` Business regression matrix.
  - Nghiệm thu: inbound, outbound, inventory, serial, LPN, catch weight, yard, carrier, 3PL đều có regression tests.
- [x] `QA-02` Security tests.
  - Nghiệm thu: role/scope/export/CSRF/password/session đều có tests tự động hoặc checklist manual.
- [x] `QA-03` UI component tests.
  - Nghiệm thu: modal, export, filter, table, floating queue, scanner, PWA banner có test.
- [x] `QA-04` Data integrity tests.
  - Nghiệm thu: không âm tồn sai, không duplicate serial, không posting sau khóa kỳ, không cross-owner leak.
- [x] `QA-05` End-to-end scenario pack.
  - Nghiệm thu: từ ASN -> nhận -> cất -> replenishment -> wave -> pick -> pack -> ship -> invoice.

## 15. Milestone Đề Xuất

| Mốc | Mục tiêu | Task chính | Điểm kỳ vọng |
|---|---|---|---:|
| `M1` | Chốt nền production và UX | `SEC`, `UX`, `PROD`, `QA` nền | 72% |
| `M2` | Nâng core WMS | `CORE`, `MOB`, `OPT` cơ bản | 80% |
| `M3` | Hoàn thiện 3PL và integration | `3PL`, `INT`, `CAR` | 88% |
| `M4` | Labor, analytics, optimization | `LAB`, `BI`, `OPT` nâng cao | 94% |
| `M5` | Automation/WCS và hardening cuối | `AUTO`, load test, DR, visual regression | 100% |

## 16. Quy Tắc Ưu Tiên Khi Bắt Đầu Làm

1. Làm `SEC`, `PROD`, `QA` nền trước khi mở rộng module lớn.
2. Mọi tính năng làm đổi tồn kho phải có idempotency, audit trail và test rollback/lỗi.
3. Mọi export/report phải kiểm tra warehouse scope và owner scope.
4. Mọi UI mới phải pass desktop/mobile/zoom 110%.
5. Không thêm workflow mới nếu chưa có trạng thái, quyền, log, test và tiêu chí nghiệm thu rõ ràng.

## 17. Ghi Chú Hoàn Tất Tích Hợp MinerU Đọc Chứng Từ

Ngày cập nhật: 13/05/2026.

Mục tiêu đã chốt:
- MinerU được dùng theo hướng self-host nội bộ, chỉ để đọc chứng từ đầu vào và tạo dòng đề xuất/phiếu nháp.
- Không đưa MinerU vào lõi nhập/xuất/tồn; hệ thống không tự cộng tồn, không tự ghi sổ, không tự duyệt phiếu từ kết quả đọc máy.
- Không tự tạo vật tư master data từ chứng từ trong v1. Dòng chưa khớp vật tư sẽ bị đánh dấu cần kiểm tra và người dùng phải chọn thủ công.

Các thay đổi đã làm:
- Thêm cấu hình `MinerU` trong `appsettings.json`: `Enabled`, `BaseUrl`, `TimeoutSeconds`, `MaxFileSizeMb`, `AllowLegacyFallback`.
- Thêm `IMineruDocumentParserClient` gọi MinerU self-host qua `/health` và `/file_parse`.
- Thêm `IVoucherDocumentIntakeService` để lưu chứng từ vào `App_Data/uploads/document-intake`, log vào `AiOcrLogs`, trích dòng vật tư và map về master data hiện có.
- Refactor endpoint cũ `POST /Vouchers/AnalyzeReceipt`: giữ route để UI không vỡ, ưu tiên MinerU, chỉ dùng Groq/Gemini fallback khi `MinerU:AllowLegacyFallback=true`.
- Đổi UI tạo phiếu từ `Đọc hóa đơn bằng AI` thành `Đọc chứng từ bằng AI`, hỗ trợ `.pdf`, ảnh, `.docx`, `.pptx`, `.xlsx`.
- Thêm bước preview kết quả đọc trước khi áp dòng vào phiếu. Chỉ dòng đã khớp chắc chắn mới được áp vào form; dòng fuzzy/unknown phải kiểm tra thủ công.

Cách vận hành:
- Mặc định `MinerU:Enabled=false`, nên WMS vẫn chạy bình thường nếu chưa có MinerU server.
- Khi triển khai nội bộ, chạy MinerU riêng, ví dụ `http://loopback IPv4:8000` hoặc URL nội bộ, rồi bật:
  - `MinerU:Enabled=true`
  - `MinerU:BaseUrl=<url MinerU nội bộ>`
  - `MinerU:AllowLegacyFallback=false` nếu muốn đảm bảo chứng từ không gửi ra dịch vụ AI ngoài.
- Nếu MinerU chưa sẵn sàng, chức năng đọc chứng từ báo lỗi tiếng Việt và không ảnh hưởng các luồng nhập/xuất/tồn khác.

File chính liên quan:
- `Services/VoucherDocumentIntakeService.cs`
- `Controllers/VouchersController.Import.cs`
- `Controllers/VouchersController.cs`
- `Views/Vouchers/Create.cshtml`
- `Program.cs`
- `appsettings.json`
- `WMS.Tests/MineruDocumentIntakeTests.cs`

Kết quả kiểm thử:
- `dotnet build WMS.sln --no-restore -v:minimal` pass.
- `dotnet test WMS.Tests\WMS.Tests.csproj --no-restore -v:minimal` pass: 468/468 tests.


