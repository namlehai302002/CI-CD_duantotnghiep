# WMS Load Test Scaffold

Mục tiêu: cung cấp kịch bản load test cho Definition of Done mà không phụ thuộc `appsettings`.

## Chạy bằng k6

```powershell
$env:WMS_BASE_URL="https://wms.example"
$env:WMS_AUTH_COOKIE="secure-auth-cookie-value"
$env:WMS_API_KEY="secure-api-key"
$env:WMS_K6_SUMMARY_PATH="artifacts/load/k6-summary-100.json"
k6 run tests/load/k6-wms-dod.js
```

Không commit cookie, API key hoặc endpoint production thật vào repo.
Kịch bản sẽ fail nếu request rơi về trang đăng nhập hoặc chỉ trả lỗi/redirect, để tránh evidence xanh giả.

Profile tải:

```powershell
$env:WMS_LOAD_PROFILE="100"
k6 run tests/load/k6-wms-dod.js
$env:WMS_LOAD_PROFILE="500"
k6 run tests/load/k6-wms-dod.js
$env:WMS_LOAD_PROFILE="1000"
k6 run tests/load/k6-wms-dod.js
```

Mutation scan/idempotency chỉ bật trên staging có seed disposable:

```powershell
$env:WMS_K6_MUTATION_ENABLED="true"
$env:WMS_K6_PICK_TASK_ID="<seeded-pick-task-id>"
$env:WMS_K6_PICK_QTY="1"
k6 run tests/load/k6-wms-dod.js
```

Nếu chưa bật mutation, kịch bản chỉ kiểm RF picking readiness và không ghi nhận là proof mutation/idempotency thật.

## Kịch bản bắt buộc

- Posting tồn kho: mở báo cáo giao dịch tồn và các endpoint đọc liên quan.
- Scan queue: mô phỏng request queue/idempotency cho receiving/picking/movement.
- Báo cáo lớn: inventory, transactions, stock valuation.
- 3PL billing: billing runs, rates, export run.
- API tích hợp: items, stock, vouchers, KPI.
- BI/SRE dashboards: semantic BI, predictive alerts, SRE dashboard.

## Acceptance

- Tỷ lệ lỗi HTTP dưới 1% với dữ liệu test hợp lệ.
- p95 request quan trọng dưới SLA đã chốt.
- Không tạo giao dịch trùng khi retry/idempotency.
- Queue pending không tăng liên tục sau khi dừng load.

