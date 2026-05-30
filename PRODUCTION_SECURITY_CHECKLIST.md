# WMS Pro Production Security Checklist

Tài liệu này là gate bảo mật production. Không sửa `appsettings`; mọi secret phải được quản lý bằng biến môi trường, user-secrets, secret store hoặc cấu hình hạ tầng ngoài repo.

## 1. Secrets And Configuration

- [ ] Không commit thêm secret mới vào repo.
- [ ] Rotate toàn bộ secret đã từng được chia sẻ ngoài secret store.
- [ ] Connection string production được cấp từ secret store hoặc biến môi trường.
- [ ] API key carrier/AI/integration được cấp ngoài repo và có lịch rotate.
- [ ] `DevResetToken` chỉ bật cho môi trường phát triển; production phải tắt hoặc đặt qua secret store có kiểm soát.
- [ ] `System:AllowFirstAdminBootstrap` bị tắt sau khi tạo admin đầu tiên ở production.

## 2. Authentication

- [ ] Cookie `HttpOnly`, `SameSite`, `SecurePolicy` được bật đúng môi trường.
- [ ] Password policy yêu cầu độ mạnh tối thiểu.
- [ ] MFA/trusted device có audit và cơ chế revoke.
- [ ] Login rate limit bật và có alert khi nhiều lần thất bại.
- [ ] Logout xóa phiên và không để lại token thao tác.

## 3. Authorization And Scope

- [ ] Mọi controller/action yêu cầu đăng nhập theo global authorization filter hoặc attribute cụ thể.
- [ ] Mọi action quản trị chỉ cho role/policy phù hợp.
- [ ] Mọi export/report áp dụng warehouse scope và owner scope nếu dữ liệu có phạm vi.
- [ ] Mọi action 3PL/multi-owner kiểm tra `EnsureCanAccessOwnerAsync`.
- [ ] Không có action nhạy cảm cho phép anonymous.

## 4. CSRF And Unsafe Methods

- [ ] `AutoValidateAntiforgeryTokenAttribute` bật toàn cục.
- [ ] Form POST có anti-forgery token hoặc dùng header token cho AJAX.
- [ ] POST/PUT/PATCH/DELETE không nhận thao tác ghi nếu thiếu token.
- [ ] Action xuất file dùng GET chỉ khi read-only và có authorization/scope.

## 5. Audit Trail

- [ ] Tạo/sửa/xóa master data có audit.
- [ ] Posting tồn kho, cancel, approve, unlock, stock adjustment có audit.
- [ ] Reset mật khẩu, revoke device, khóa tài khoản có audit hoặc security event.
- [ ] Export dữ liệu nhạy cảm được log theo user, thời điểm, scope.

## 6. Data Protection

- [ ] Data Protection keys được lưu bền vững và backup.
- [ ] Key folder không public qua web server.
- [ ] Không xóa key khi rolling deploy nếu còn cookie phiên hợp lệ.

## 7. Security Headers

- [ ] `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy` đang bật.
- [ ] Camera chỉ được cấp cho màn scan cần thiết.
- [ ] Static upload không cho thực thi script.

## 8. Acceptance Evidence

- [ ] Link build/test/format pass.
- [ ] Link kết quả static security gate.
- [ ] Link kết quả role/scope/export tests.
- [ ] Link incident drill hoặc security review gần nhất.

