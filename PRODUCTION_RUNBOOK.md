# WMS Pro Production Runbook

Áp dụng cho vận hành production của WMS Pro. Tài liệu này không chứa secret và không yêu cầu sửa `appsettings`.

## 1. Daily Readiness

- Kiểm tra `/health` trả trạng thái healthy trước ca vận hành.
- Kiểm tra dashboard OpenTelemetry/collector: request error rate, p95 latency, DB latency, hosted service failures.
- Kiểm tra job nền: outbox tích hợp, snapshot tồn kho, reconciliation tồn kho, replenishment automation.
- Nếu bật MinerU, kiểm tra `MinerU:BaseUrl` trỏ đúng dịch vụ nội bộ và không có log `MINERU_LOOPBACK_PRODUCTION_WARNING` ngoài trường hợp MinerU thật sự chạy cùng máy host.
- Kiểm tra dung lượng DB, dung lượng thư mục upload, dung lượng nơi lưu Data Protection keys.
- Kiểm tra số lượng scan queue pending/failed và carrier outbox pending/failed.

## 2. Backup And Restore

- Backup DB production theo lịch tối thiểu hằng ngày; hệ thống có giao dịch cao cần backup log nhiều lần trong ngày.
- Lưu backup ở vùng tách biệt với máy ứng dụng; bật mã hóa và kiểm soát quyền truy cập.
- Restore drill hằng tháng trên staging:
  - Khôi phục DB từ backup mới nhất.
  - Trỏ staging vào DB restore bằng cấu hình ngoài repo.
  - Chạy smoke test: login, xem tồn, tạo phiếu nháp, báo cáo tồn, export, `/health`.
  - Ghi lại RPO/RTO thực tế và người xác nhận.
- Không dùng production secret trong staging; rotate secret nếu có dấu hiệu bị lộ.

## 3. Disaster Recovery

- Mất ứng dụng: deploy lại build gần nhất, kiểm tra Data Protection keys và connection string ngoài repo.
- Mất DB: restore backup gần nhất, chạy migration validation, khóa thao tác ghi trong lúc đối soát.
- Mất storage/upload: restore từ backup storage, chạy kiểm tra link chứng từ/ảnh.
- Mất tích hợp carrier/API: chuyển sang chế độ retry/outbox, theo dõi dead-letter, không xác nhận giao hàng tự động nếu chưa có tracking hợp lệ.
- Mất telemetry: không chặn vận hành, nhưng mở incident nếu quá 30 phút không có metric/log.

## 4. Monitoring

- Health endpoint: `/health`.
- Metric bắt buộc: request count/error, latency p95/p99, DB query duration, outbox pending, scan queue pending, posting failures, export failures.
- Alert bắt buộc:
  - `/health` unhealthy trên 2 lần liên tiếp.
  - Tỷ lệ lỗi HTTP 5xx vượt 2% trong 5 phút.
  - Outbox hoặc scan queue pending tăng liên tục trên 15 phút.
  - DB latency p95 vượt ngưỡng SLA.
  - Disk còn dưới 15%.

## 5. Incident Response

- Mức `SEV1`: không đăng nhập được, không post tồn được, DB down, mất dữ liệu, xuất kho/nhập kho dừng toàn bộ.
- Mức `SEV2`: một module chính lỗi, tích hợp carrier/API lỗi hàng loạt, báo cáo/export lỗi nhiều người dùng.
- Mức `SEV3`: lỗi UI đơn lẻ, dữ liệu hiển thị lệch nhưng có workaround.
- Quy trình:
  - Ghi thời điểm, người phát hiện, module, ảnh hưởng, correlation id nếu có.
  - Chặn thao tác rủi ro nếu lỗi có thể làm sai tồn.
  - Lấy log/trace liên quan, xác định phạm vi kho/chủ hàng.
  - Khôi phục dịch vụ hoặc rollback release.
  - Viết postmortem trong 24 giờ cho `SEV1/SEV2`.

## 6. Release Checklist

- `dotnet build WMS.sln -v:minimal` passed.
- `dotnet test WMS.sln -v:minimal` passed.
- `dotnet format WMS.sln --verify-no-changes` passed.
- Migration dry-run đã kiểm tra trên staging nếu có migration.
- Visual regression hoặc checklist UI cho desktop/mobile/zoom 110% đã pass.
- Checklist UI phải kiểm riêng sidebar thu gọn: rail hiển thị đủ nhóm chính và flyout Nhập kho/Xuất kho/Tồn kho/Hệ thống không chồng nội dung.
- Load/smoke test cho posting, scan queue, report lớn, 3PL billing và API tích hợp đã pass.
- Nếu release bật `MinerU:Enabled=true`, gọi `/health` của MinerU từ máy host WMS và xác nhận chức năng `Đọc chứng từ bằng AI` báo preview, không tự tạo master data.
- Backup trước release đã xác nhận restore được.
- Rollback plan đã có người duyệt.

## 7. Visual/UI Release Gate

Chuẩn bị máy chạy visual:

```powershell
npm install
npx playwright install chromium
```

Tạo auth state trên staging/test host:

```powershell
$env:WMS_BASE_URL="https://your-wms-host"
$env:WMS_TEST_USER="visual.admin"
$env:WMS_TEST_PASSWORD="your-test-password"
npm run visual:auth
```

Chạy visual regression:

```powershell
$env:WMS_BASE_URL="https://your-wms-host"
npm run visual:test
```

Nếu tài khoản có MFA hoặc không muốn lưu tài khoản test trong biến môi trường, tạo auth state thủ công rồi truyền:

```powershell
$env:WMS_AUTH_STATE="C:\secure\wms-auth-state.json"
```

Không commit `tests/visual/.auth/wms-auth-state.json`, Playwright report hoặc screenshot thật vào repo nếu có dữ liệu vận hành.

## 8. Ownership

- Người trực vận hành chịu trách nhiệm health, log, queue, backup.
- Người phụ trách nghiệp vụ chịu trách nhiệm xác nhận tồn kho, phiếu, billing và báo cáo sau release.
- Người phụ trách bảo mật chịu trách nhiệm secret rotation, tài khoản admin, trusted devices và audit security events.

## 9. Migration Evidence Addendum

- Idempotent migration script phai duoc tao va luu cung release evidence.
- Changelog, test evidence, migration note va rollback note phai co nguoi phe duyet.
- Sau deploy phai chay `dotnet ef migrations list --no-build` de xac nhan khong con pending migration.


