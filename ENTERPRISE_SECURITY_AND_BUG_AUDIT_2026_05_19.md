# Báo cáo Kiểm toán Bảo mật và Lỗi - 2026-05-19

## 1. Tóm tắt điều hành

**Kết luận phát hành:** `NOT APPROVED FOR THIRD-PARTY HOSTING` cho đến khi các mục `Critical` được xử lý, secret được xoay vòng, artifact được làm sạch và quét lại.

Báo cáo này rà soát theo hướng enterprise: không chỉ xem code có chạy được hay không, mà đánh giá cả khả năng rò rỉ bí mật, artifact build, log, upload, data protection key, telemetry và các điểm có thể gây rủi ro khi đẩy lên hosting bên thứ ba.

**Lưu ý quan trọng:** nếu secret thật nằm trong `appsettings.json` hoặc artifact publish/build rồi được upload lên hosting, thì việc hosting có cơ chế bảo vệ file cấu hình không loại bỏ rủi ro. Chuẩn hệ thống lớn là không đưa secret plaintext vào repo/package; secret đã từng xuất hiện trong workspace/artifact phải được xem là đã lộ và cần rotate.

### Trạng thái tổng quan

| Hạng mục | Trạng thái | Ghi chú |
|---|---:|---|
| Secret trong source/config | Critical | Có secret plaintext trong `appsettings.json`; giá trị đã được redact trong báo cáo này. |
| Secret lan sang artifact | Critical | `appsettings.json` có mặt trong `bin/`, `WMS.Tests/bin/` và publish output. |
| DataProtection key/log/upload | High | Workspace có key XML, log runtime và ảnh upload trong `App_Data`. |
| Telemetry/logging | High | EF SQL statement telemetry và console exporter có thể làm lộ metadata vận hành. |
| API key validation | Medium | So sánh API key bằng equality thông thường; nên dùng hashed/constant-time compare. |
| Dependency vulnerability scan | Passed | Không thấy package vulnerable theo NuGet source hiện tại. |
| Automated test suite | Passed | `523/523` tests passed. |
| Build verification | Inconclusive | Build riêng bị lock file do chạy song song với test; cần rerun tuần tự. |

## 2. Phạm vi và phương pháp

### Phạm vi đã rà soát

- Source chính: `Controllers`, `Services`, `Models`, `Data`, `Common`, `Authorization`, `ViewModels`, `Views`, `wwwroot/js`, `wwwroot/css`, `Program.cs`.
- Config và manifest: `appsettings.json`, `appsettings.Development.json`, `Properties/launchSettings.json`, `.gitignore`, `WMS.csproj`, `package.json`.
- Dữ liệu và artifact: `App_Data`, `artifacts`, `bin`, `obj`, `WMS.Tests/bin`, publish output.
- Test và tài liệu: `WMS.Tests`, `tests`, các file `.md` hiện có.
- Dependency source trong `node_modules` không được đánh giá như code nghiệp vụ, chỉ kiểm kê như surface packaging/security.

### Nguyên tắc báo cáo

- Không in lại secret thật. Mọi secret đều được ghi là `[REDACTED]`.
- Mức độ rủi ro gồm: `Critical`, `High`, `Medium`, `Low`, `Passed`.
- Bằng chứng ghi theo file và line number để có thể xử lý ngay.
- Báo cáo này là audit artifact; không thay đổi config, không rotate secret, không xóa file và không sửa `.gitignore`.

## 3. Bảng bằng chứng

| Severity | File | Line | Bằng chứng đã redact | Tác động |
|---|---|---:|---|---|
| Critical | `appsettings.json` | 11 | `ConnectionStrings:DefaultConnection` chứa DB host/user/password plaintext | Lộ thông tin truy cập database khi upload repo/package/artifact. |
| Critical | `appsettings.json` | 13 | `GroqApiKey` có giá trị plaintext `[REDACTED]` | API key bên thứ ba có thể bị sử dụng trái phép. |
| Critical | `appsettings.json` | 14 | `GeminiApiKey` có giá trị plaintext `[REDACTED]` | API key bên thứ ba có thể bị sử dụng trái phép. |
| Critical | `appsettings.json` | 22 | `DevResetToken` có token cấu hình trong file | Token reset dev không nên nằm trong config publishable. |
| Critical | `appsettings.json` | 24-29 | SMTP host/user/pass/from trong plaintext | Có thể bị lạm dụng gửi mail hoặc khóa tài khoản mail. |
| High | `appsettings.json` | 36 | `System:AllowFirstAdminBootstrap` đang bật | Cần đảm bảo production có token riêng và chỉ bật khi bootstrap có kiểm soát. |
| Medium | `appsettings.json` | 9 | `AllowedHosts` là `*` | Nên giới hạn domain hosting production. |
| Medium | `appsettings.json` | 17 | `MinerU:BaseUrl` trỏ về `loopback IPv4` | Có thể gây lỗi production nếu hosting không chạy MinerU nội bộ. |
| Critical | `bin/Debug/net8.0/appsettings.json` | n/a | Bản sao config có secret plaintext | Build output đã mang theo secret. |
| Critical | `bin/Release/net8.0/appsettings.json` | n/a | Bản sao config có secret plaintext | Release output đã mang theo secret. |
| Critical | `WMS.Tests/bin/*/net8.0/appsettings.json` | n/a | Bản sao config có secret plaintext | Test output đã mang theo secret. |
| Critical | `WMS.Tests/bin/Debug/net8.0/publish/appsettings.json` | n/a | Publish output có config secret | Nếu upload publish folder, secret được đưa lên hosting. |
| High | `App_Data/DataProtection-Keys/*.xml` | n/a | Data Protection key XML tồn tại trong workspace | Key dùng bảo vệ cookie/token; cần bảo vệ như secret. |
| High | `App_Data/*.log`, `artifacts/runtime/*.log` | n/a | Log có host/service runtime và lỗi kết nối DB | Log có thể làm lộ topology/metadata và nên tách khỏi package. |
| High | `App_Data/uploads/legacy-public-receipts/*` | n/a | Ảnh chứng từ/upload cũ tồn tại trong workspace | Có thể chứa dữ liệu nghiệp vụ/PII; không nên đi kèm source deploy. |
| High | `Program.cs` | 207 | `SetDbStatementForText = true` | Có nguy cơ ghi SQL statement vào trace/log. |
| High | `Program.cs` | 211, 231 | `AddConsoleExporter()` cho tracing/metrics | Có thể đẩy telemetry nhạy cảm ra console/log hosting. |
| High | `Program.cs` | 304-306 | Persist Data Protection key vào `App_Data/DataProtection-Keys` | Cần storage an toàn và lifecycle key rõ ràng. |
| Medium | `Controllers/ApiIntegrationController.cs` | 38-43 | API key đọc từ config/header và so sánh bằng `==` | Nên dùng hash + constant-time compare + rotation/key id. |
| Medium | `Controllers/ApiIntegrationController.cs` | 50-59 | Có cơ chế `ExposeFinancialFields` và `ScopedWarehouseId` | Điểm khá tốt; cần đảm bảo production bật scope đúng đối tác. |
| High | `.gitignore` | 1-5 | Chỉ ignore DP key và một số test result | Thiếu ignore cho `bin/`, `obj/`, `node_modules/`, broad logs, uploads, artifacts. |
| Low | `Properties/launchSettings.json` | 18 | Local SQL Server connection dev | Ít rủi ro hơn production secret nhưng không nên đóng gói deploy. |

## 4. Phát hiện chi tiết

### SEC-CRIT-001 - Secret plaintext trong `appsettings.json`

**Severity:** Critical

`appsettings.json` hiện đang chứa nhiều giá trị nhạy cảm thật: connection string database, API key AI, SMTP credential, token reset dev và cấu hình bootstrap admin. Báo cáo này không lặp lại giá trị secret để tránh làm lộ thêm.

**Vì sao đây là blocker:** đẩy lên hosting bên thứ ba, đưa vào repo, copy qua artifact, gửi zip cho người khác hoặc publish lên server đều làm tăng phạm vi tiếp xúc của secret. Trong chuẩn enterprise, file config trong source chỉ nên chứa placeholder hoặc default không nhạy cảm.

**Khuyến nghị:**

- Rotate ngay DB password, Groq key, Gemini key và SMTP app password đã từng xuất hiện.
- Thay giá trị trong `appsettings.json` bằng placeholder như `${DefaultConnection}`, `${GroqApiKey}`, `${GeminiApiKey}`.
- Đưa secret vào biến môi trường, secret store của hosting, user-secrets local, Key Vault hoặc cơ chế tương đương.
- Thêm secret scanning gate vào CI/local release checklist.

### SEC-CRIT-002 - Secret lan sang build/publish artifact

**Severity:** Critical

Các bản sao `appsettings.json` có secret plaintext đã được tìm thấy trong `bin/Debug`, `bin/Release`, `WMS.Tests/bin` và publish output. Đây là rủi ro riêng với source gốc, vì người dùng thường upload publish folder lên hosting.

**Khuyến nghị:**

- Làm sạch artifact cũ sau khi rotate secret.
- Không upload `bin/`, `obj/`, `WMS.Tests/bin`, test publish output hoặc artifact runtime lên hosting.
- Tạo publish package mới từ config đã redact.
- Kiểm tra lại bằng secret scan trước khi nén zip/deploy.

### SEC-HIGH-003 - Data Protection key nằm trong workspace

**Severity:** High

Ứng dụng persist ASP.NET Core Data Protection key vào `App_Data/DataProtection-Keys`. Key XML đang tồn tại trong workspace. Key này liên quan đến cookie/token được protect; nếu bị lấy cùng artifact, rủi ro session/token protection tăng lên.

**Khuyến nghị:**

- Không đưa key XML vào repo/package.
- Trên production, lưu key trong storage có ACL tốt hoặc provider an toàn của hosting.
- Backup key riêng cho production để tránh logout toàn bộ người dùng khi recycle/deploy, nhưng không đóng gói chung với source.
- Kiểm tra permission folder `App_Data/DataProtection-Keys` trên hosting.

### SEC-HIGH-004 - Log và upload data có thể bị đóng gói nhầm

**Severity:** High

`App_Data` và `artifacts/runtime` có log runtime, lỗi kết nối DB, service host metadata, cùng các file upload cũ. Các file upload có thể là chứng từ nghiệp vụ hoặc dữ liệu khách hàng. Nếu đẩy lên hosting hoặc chia sẻ source zip, đây là rò rỉ dữ liệu.

**Khuyến nghị:**

- Tách data runtime/upload/log khỏi source package.
- Trước deployment, tạo folder runtime trong hosting và cấp permission riêng.
- Không commit/upload log cũ, ảnh upload cũ, hoặc test evidence có dữ liệu nhạy cảm.
- Thêm retention policy và log redaction.

### SEC-HIGH-005 - Telemetry có nguy cơ lộ SQL/metadata

**Severity:** High

`Program.cs` bật `SetDbStatementForText = true` và thêm console exporter cho tracing/metrics. Trong production, SQL statement, DB/service metadata hoặc request trace có thể đi vào log hosting.

**Khuyến nghị:**

- Tắt `SetDbStatementForText` trong production hoặc chỉ bật qua flag ngắn hạn khi debug incident.
- Không dùng console exporter ở production nếu log không có redaction và retention chuẩn.
- Gửi telemetry về collector được kiểm soát, có sampling và redaction.
- Đảm bảo log không ghi query có tham số nhạy cảm.

### SEC-MED-006 - API key validation có thể harden hơn

**Severity:** Medium

`ValidateApiKey()` đọc key từ config/header và so sánh bằng equality thông thường. Hiện tại có rate limiting, nhưng API key enterprise nên có hash, key id, expiry, rotation và constant-time comparison.

**Khuyến nghị:**

- Lưu hash của API key thay vì plaintext.
- Dùng `CryptographicOperations.FixedTimeEquals`.
- Hỗ trợ nhiều key đang active với `keyId`, expiry, owner/warehouse scope và audit.
- Từ chối config placeholder như `${ApiKey}` trong production.

### SEC-MED-007 - Hardening cấu hình production

**Severity:** Medium

`AllowedHosts` đang là `*`; `MinerU:BaseUrl` trỏ về loopback host; bootstrap admin có flag bật. Các setting này có thể hợp lý cho dev, nhưng cần chốt production profile riêng trước hosting.

**Khuyến nghị:**

- Giới hạn `AllowedHosts` theo domain thật.
- Tắt hoặc cấu hình đúng `MinerU` trên production hosting.
- Chỉ bật first-admin bootstrap trong cửa sổ vận hành ngắn, yêu cầu token secret qua environment.
- Xác minh `ASPNETCORE_ENVIRONMENT=Production` trên hosting.

### SEC-HIGH-008 - `.gitignore`/packaging hygiene chưa đạt enterprise

**Severity:** High

`.gitignore` hiện chỉ ignore một số file rất hẹp. Workspace có `bin`, `obj`, `node_modules`, `artifacts`, `App_Data` log/upload và test output. Nếu repo/zip được tạo từ workspace hiện tại, nguy cơ đóng gói nhầm rất cao.

**Khuyến nghị:**

- Bổ sung ignore cho `bin/`, `obj/`, `node_modules/`, `artifacts/`, `*.log`, `App_Data/uploads/`, publish output và test output.
- Tạo script/hướng dẫn release package chỉ lấy file cần deploy.
- Thêm gate fail nếu phát hiện secret trong artifact.

## 5. Rủi ro lỗi và chất lượng vận hành

| Severity | Phát hiện | Tác động | Khuyến nghị |
|---|---|---|---|
| Medium | Build verification riêng bị lock file khi chạy song song với test | Không kết luận được build clean từ lệnh riêng | Dùng quy trình verify tuần tự, đóng app/compiler nếu cần. |
| Medium | `App_Data` trộn runtime data, log, key và upload | Dễ nhầm lẫn giữa source và data vận hành | Tách source/deploy/runtime data bằng convention rõ ràng. |
| Low | Có nhiều tài liệu audit cũ và artifact evidence trong root | Làm release package nặng và khó review | Chuyển audit/evidence cũ vào thư mục archival ngoài package deploy. |
| Low | `node_modules` nằm trong workspace | Tăng kích thước và tạo nhiều false positive scan | Không đưa `node_modules` vào package; dùng `npm ci` nếu cần build asset. |

## 6. Điểm đã đạt

| Hạng mục | Trạng thái | Bằng chứng |
|---|---:|---|
| Test tự động | Passed | `dotnet test WMS.Tests\WMS.Tests.csproj -c Debug --no-restore` passed `523/523`, skipped `0`. |
| NuGet vulnerability scan | Passed | `dotnet list WMS.csproj package --vulnerable --include-transitive --no-restore` không tìm thấy vulnerable package theo source hiện tại. |
| Global auth/CSRF convention | Passed | `Program.cs` có `AuthorizeFilter` và `AutoValidateAntiforgeryTokenAttribute`. |
| HTTPS/HSTS production posture | Passed | `Program.cs` có `UseHsts()` ngoài Development và `UseHttpsRedirection()`. |
| API financial exposure default | Passed một phần | `ExposeFinancialFields()` mặc định chỉ expose khi config bật `true`. |
| API warehouse scoping capability | Passed một phần | Có `Api:ScopedWarehouseId`; cần bật/cấu hình theo tenant/partner production. |

## 7. Nhật ký xác minh

### Đã chạy

```text
dotnet test WMS.Tests\WMS.Tests.csproj -c Debug --no-restore
Result: Passed - Failed: 0, Passed: 523, Skipped: 0, Total: 523, Duration: 24s
```

```text
dotnet list WMS.csproj package --vulnerable --include-transitive --no-restore
Result: No vulnerable packages given the current NuGet sources.
```

```text
dotnet build WMS.sln -c Debug --no-restore
Result: Inconclusive - failed because WMS.dll was locked by another process while test/build ran in parallel.
```

### Cần chạy lại sau khi khắc phục

```powershell
dotnet build WMS.sln -c Debug --no-restore
dotnet test WMS.Tests\WMS.Tests.csproj -c Debug --no-restore --logger "console;verbosity=minimal"
dotnet list WMS.csproj package --vulnerable --include-transitive --no-restore
rg -n -i --hidden -uu -g '!node_modules/**' -g '!wwwroot/lib/**' "(password|secret|api[_-]?key|token|connectionstring|user id|smtp|private key|BEGIN RSA|BEGIN OPENSSH)"
```

## 8. Checklist xử lý ưu tiên

### P0 - Bắt buộc trước hosting

- [ ] Rotate DB password, Groq key, Gemini key, SMTP app password và bất kỳ API key/token nào đã từng nằm trong `appsettings.json`.
- [ ] Thay secret plaintext trong `appsettings.json` bằng placeholder hoặc default không nhạy cảm.
- [ ] Đưa secret production vào environment variables/hosting secret store.
- [ ] Xóa hoặc tạo lại artifact build/publish cũ có secret.
- [ ] Kiểm tra publish package mới không chứa secret bằng secret scan.
- [ ] Không upload `App_Data/DataProtection-Keys`, logs, uploads cũ, `bin`, `obj`, `node_modules`, test output lên hosting.

### P1 - Hardening bảo mật ngay sau P0

- [ ] Tắt EF SQL statement telemetry trong production hoặc đặt sau feature flag có audit.
- [ ] Bỏ console telemetry exporter trong production hoặc chỉ gửi đến collector đã redact/sampling.
- [ ] Harden API key validation: hash storage, constant-time compare, key id, expiry, rotation, owner/warehouse scope.
- [ ] Giới hạn `AllowedHosts` theo domain production.
- [ ] Kiểm soát `System:AllowFirstAdminBootstrap` bằng token secret và tắt sau bootstrap.
- [ ] Cấu hình `MinerU` đúng topology production hoặc tắt nếu hosting không hỗ trợ.

### P2 - Release engineering

- [ ] Mở rộng `.gitignore` để chặn generated output, dependency cache, log, upload, artifact và publish output.
- [ ] Tạo release packaging checklist/script chỉ lấy file cần deploy.
- [ ] Thêm CI/local gate fail nếu phát hiện secret trong source hoặc artifact.
- [ ] Tách thư mục audit/evidence runtime khỏi deployable package.

### P3 - Quản trị vận hành

- [ ] Định nghĩa retention cho log và upload.
- [ ] Kiểm tra permission của runtime folders trên hosting.
- [ ] Lập runbook rotate secret và invalidate compromised keys.
- [ ] Lưu Data Protection key production ở storage có backup và ACL rõ ràng.

## 9. Quyết định phát hành

Hệ thống có nhiều điểm `Passed` về test tự động và một số convention bảo mật, nhưng **chưa đủ điều kiện hosting production** vì secret plaintext đã nằm trong source và artifact. Sau khi hoàn tất P0, cần quét lại và cập nhật báo cáo bằng chứng mới. Chỉ khi không còn secret trong source/artifact và build/test pass tuần tự, mới nên chuyển quyết định sang `CONDITIONALLY APPROVED`.


