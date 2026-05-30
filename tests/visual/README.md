# WMS Visual Regression

Mục tiêu: kiểm tra các màn vận hành chính ở desktop, mobile và zoom 100%, 110%, 125% trước khi xác nhận UI đạt chuẩn release.

## Chuẩn bị

```powershell
npm install
npx playwright install chromium
```

Tạo storage state đã đăng nhập bằng tài khoản test:

```powershell
$env:WMS_BASE_URL="https://wms.example"
$env:WMS_TEST_USER="visual.admin"
$env:WMS_TEST_PASSWORD="StrongPassword@123"
npm run visual:auth
```

Nếu tài khoản có MFA, tạo storage state thủ công và truyền đường dẫn:

```powershell
$env:WMS_AUTH_STATE="C:\secure\wms-auth-state.json"
```

Không commit file auth state vào repo.

## Chạy kiểm thử

```powershell
$env:WMS_BASE_URL="https://wms.example"
npm run visual:test
```

Nếu đây là lần đầu tạo baseline, hoặc bạn đã duyệt thay đổi giao diện có chủ đích:

```powershell
$env:WMS_BASE_URL="https://wms.example"
npm run visual:update
npm run visual:test
```

`visual:update` dùng để chấp nhận ảnh chuẩn. Nếu thiếu baseline, Playwright sẽ tạo ảnh actual nhưng vẫn báo failed ở run đầu để người rà soát xác nhận.

Script tổng hợp cũng hỗ trợ baseline update có chủ đích:

```powershell
.\scripts\Run-WmsVerification.ps1 -UpdateVisualBaselines
```

## Evidence không cần thiết bị thật

```powershell
$env:WMS_BASE_URL="https://wms.example"
npm run visual:no-device
```

Smoke này kiểm RF receiving/picking/movement bằng keyboard-wedge giả lập, modal camera bằng DOM state, và preview nhãn in từ dữ liệu vật tư hiện có. Scanner/printer thật vẫn là checklist sản xuất riêng, không phải gate local bắt buộc.

## Rà mobile sâu

```powershell
$env:WMS_BASE_URL="https://wms.example"
npm run visual:mobile-deep
```

Gate này chạy nhiều kích thước mobile/tablet và kiểm runtime assertions thay vì baseline ảnh: không overflow toàn trang, không có chrome cố định che nút/input, tap target đủ lớn, text nút không tràn, không mojibake hiển thị, không console error hoặc response 5xx. Các màn chi tiết cần dữ liệu sẽ lấy link đầu tiên từ trang danh sách; nếu seed chưa có dữ liệu thì test attach `no-seed-data` thay vì fail giả.

## Màn bắt buộc

- Trang chính
- Hướng dẫn theo vai trò
- Quản lý người dùng
- Tạo phiếu kho
- Tiếp nhận hàng
- Nhận hàng bằng thiết bị cầm tay
- Lấy hàng
- Lấy hàng bằng thiết bị cầm tay
- Tồn kho
- Trung tâm ngoại lệ
- Quản lý bãi
- Bảng cửa nhận/xuất
- Tối ưu vận hành
- Tự động hóa thiết bị
- Tích hợp hệ thống
- Kết nối hãng vận chuyển
- Đối soát giao hàng
- Đợt tính phí 3PL
- Bảng giá 3PL
- Lớp báo cáo semantic
- Cảnh báo dự báo
- Trợ lý AI có kiểm soát
- Hồ sơ workflow
- Bảng SRE

## Viewport và zoom

- Desktop: `1440x900`
- Desktop zoom 110%: `1440x900`, CSS zoom `1.1`
- Desktop zoom 125%: `1440x900`, CSS zoom `1.25`
- Mobile: `390x844`
- Mobile deep: `360x740`, `390x844`, `430x932`, `768x1024`

## Tiêu chí đạt

- Không có text chồng nhau.
- Không có bảng bị bóp cột thành chữ dọc.
- Hàng đợi quét, banner cài app, modal và footer action không đè nhau.
- Các nút chính vẫn bấm được ở mobile và zoom 110%.
- Mobile deep không có overflow/collision/mojibake/console error trên route GET an toàn.
- `test-results/.last-run.json` phải về `passed` sau khi baseline đã được chấp nhận.

