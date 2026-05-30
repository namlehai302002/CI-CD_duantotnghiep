# Enterprise E1-E13 Full Audit 2026-05-14

## Executive Verdict

**Kết luận: đạt có cảnh báo.**

Mã nguồn hiện tại build, test, format và migration đều qua. Ma trận E1-E13 đã có source, test và tài liệu bao phủ rộng hơn các đợt trước; các lỗi route/UI lớn đã xử lý trước đó như `CrossDockOpportunities`, sidebar thu gọn, MinerU intake, private upload, bootstrap token và controller refactor core đều còn được test bảo vệ.

Tuy nhiên chưa nên tuyên bố **100% production evidence** vì visual runtime có đăng nhập chưa chạy được do thiếu auth state, checklist nghiệm thu vận hành/staging chưa có artifact thật, và audit vẫn ghi nhận một số nợ kỹ thuật không chặn vận hành nhưng cần xử lý trước release lớn.

| Hạng mục | Kết quả |
|---|---|
| Critical | 0 |
| High mở mới | 0 |
| High chấp nhận tạm | Secret/config cụ thể trong `appsettings.json`, giữ nguyên theo quyết định vận hành hiện tại |
| Medium | 1 còn lại sau remediation |
| Low | 2 còn lại sau remediation |
| Cosmetic | 0 còn lại sau remediation |
| Schema migration required | Không |
| Business code changed in this audit | Không |
| Build | Passed |
| Unit/regression tests | Passed, 492/492 |
| Format check | Passed |
| EF migration list | Passed |
| Authenticated visual runtime | Chưa kiểm được do thiếu `WMS_BASE_URL`/`WMS_AUTH_STATE` hoặc auth state tại `tests/visual/.auth/wms-auth-state.json` |

## Remediation Update 2026-05-14

Sau audit này đã xử lý các mục local-actionable trong repo, không đổi schema và không đụng secret trong `appsettings`:

- MED-01 đã xử lý: predictive stockout đọc tồn khả dụng từ `ItemLocation` theo kho/chủ hàng, bỏ phụ thuộc `Item.CurrentStock` làm source of truth.
- LOW-01 đã xử lý: `QualityInspection` dùng `StatusDisplay`, `StatusBadgeClass`, `VoucherTypeName`, tag helper và không còn inline style tĩnh.
- LOW-02 đã xử lý phần trọng điểm: `_Layout`, `Home/Index`, `QualityInspection` đã chuyển link nội bộ chính sang tag helper/route helper; test vẫn chặn URL có `%20`, non-ASCII hoặc action bị dịch nhầm.
- LOW-04 đã xử lý: `OperationsController` và `VouchersController` không còn fallback `?? new ...Service(...)` trong constructor core; test factory inject đủ dependency.
- COS-01 đã xử lý bằng cảnh báo vận hành: startup log `MINERU_LOOPBACK_PRODUCTION_WARNING` khi production bật MinerU nhưng BaseUrl vẫn là loopback host/loopback IPv4.
- Verification mới: `dotnet build` passed, `dotnet test` passed 492/492, `dotnet format --verify-no-changes` passed, `dotnet ef migrations list --no-build` passed.

Còn lại không tự xử lý trong repo: auth state/staging artifact cho visual, checklist nghiệm thu thật, backup/restore drill, k6 release artifact và quyết định externalize secret.

## Scope And Inventory

Audit loại trừ generated/output/upload/log: `bin`, `obj`, `node_modules`, `wwwroot/lib`, `wwwroot/uploads`, Playwright reports, test results và auth state.

| Nhóm | Số file đã inventory |
|---|---:|
| Controllers | 43 |
| Services | 39 |
| Models | 69 |
| Data | 1 |
| ViewModels | 4 |
| Views | 126 |
| wwwroot | 13 |
| WMS.Tests | 21 |
| tests | 8 |
| scripts | 2 |
| Migrations | 150 |

## Verification Evidence

| Lệnh/scan | Kết quả |
|---|---|
| `dotnet build WMS.sln --no-restore -v:minimal` | Passed, 0 warnings, 0 errors |
| `dotnet test WMS.Tests\WMS.Tests.csproj --no-restore -v:minimal` | Passed, 492/492 |
| `dotnet format WMS.sln --verify-no-changes --no-restore` | Passed |
| `dotnet ef migrations list --no-build` | Passed, migration list đọc được |
| `TODO/FIXME/HACK/NotImplementedException/async void/.Result/.Wait` trong runtime source | Không có match trong `Controllers`, `Services`, `Models`, `Data`, `ViewModels`, `Views`, `wwwroot`, `scripts` |
| POST form thiếu antiforgery trong view | Không phát hiện; `Program.cs:43` bật `AutoValidateAntiforgeryTokenAttribute` toàn cục và view có 178 token explicit |
| Route/link có `%20` hoặc ký tự tiếng Việt trong URL nội bộ | Không phát hiện |
| Route `Chuyển thẳng` lỗi cũ | Không còn `Chuyển%20thẳng` hoặc `Chuyển thẳngOpportunities`; menu dùng `CrossDockOpportunities` |
| Visual auth | `tests/visual/.auth` chỉ có `.gitignore`; `tests/visual/playwright.config.ts:8-13` yêu cầu `WMS_BASE_URL` và auth state nên chưa chạy được screenshot có đăng nhập |

## E1-E13 Coverage Matrix

| E | Mảng | Trạng thái audit | Bằng chứng chính | Chưa kiểm được / cảnh báo |
|---|---|---|---|---|
| E1 | Core WMS nhập/xuất/tồn | Đạt | `CoreWmsCompletionTests`, `BusinessLogicHardeningTests`, `CoreControllerRefactorTests`, `Enterprise1113CompletionTests`, `InboundExecutionService`, `OutboundExecutionService`, `InventoryBalanceService` | Cần visual authenticated artifact cho luồng end-to-end |
| E2 | Mobile/RF/offline | Đạt | `MobileSecurityCompletionTests`, `wwwroot/js/offline-scan-queue.js`, `wwwroot/js/mobile-scanner.js`, RF views | Cần visual mobile authenticated artifact |
| E3 | Security/tenant | Đạt có cảnh báo chấp nhận | `EnterpriseAuditRemediationTests`, authz attributes, antiforgery global, private upload guarded download | Secret trong `appsettings.json` giữ nguyên theo quyết định vận hành; staging security checklist chưa có bằng chứng thật |
| E4 | Yard/TMS/carrier | Đạt | `YardTms3PlLaborEnterpriseCompletionTests`, `YardBillingTests`, `OperationsController.YardManagement.cs`, carrier/shipping services | Cần smoke runtime có auth cho Yard/Dock/Carrier |
| E5 | 3PL/billing | Đạt | `YardTms3PlLaborEnterpriseCompletionTests`, 3PL views/controllers/services | Cần nghiệm thu kế toán thực tế với dữ liệu hợp đồng thật |
| E6 | Labor | Đạt | `YardTms3PlLaborEnterpriseCompletionTests`, labor dashboard/service | Cần artifact productivity dashboard trên staging |
| E7 | Optimization | Đạt có cảnh báo | `OptimizationAutomationIntegrationCompletionTests`, slotting/refactor services, optimization views | Cần visual authenticated cho `OptimizationDashboard` |
| E8 | Automation/WCS/WES/MHE | Đạt có cảnh báo | `OptimizationAutomationIntegrationCompletionTests`, `AutomationDashboard`, label helper đã Việt hóa | Cần thiết bị thật hoặc simulator staging để xác minh telemetry end-to-end |
| E9 | Integration/API/EDI/webhook | Đạt có cảnh báo | `OptimizationAutomationIntegrationCompletionTests`, `IntegrationDashboard`, OpenAPI link, integration services | Cần connector endpoint thật/staging và outbox artifact |
| E10 | BI/AI/MinerU | Đạt có cảnh báo | `Enterprise1113CompletionTests`, `MineruDocumentIntakeTests`, `MINERU_HOST_DEPLOYMENT_GUIDE.md`, `VoucherDocumentIntakeService` | MinerU host phải chạy riêng và cần cấu hình BaseUrl nội bộ thật trên production |
| E11 | UX enterprise | Đạt có cảnh báo | `EnterpriseUiRedesignTests`, `EnterpriseUiUxPolishTests`, sidebar mini rail/flyout, layout CSS | Authenticated visual chưa chạy; inline style legacy còn nhiều ở một số view, xem LOW-03 |
| E12 | Production/SRE | Đạt có cảnh báo | `PRODUCTION_RUNBOOK.md`, `PRODUCTION_SECURITY_CHECKLIST.md`, load/visual scaffold, health route | Backup/restore, k6, staging monitoring cần artifact thật |
| E13 | QA/test gate | Đạt | 492/492 tests, `DefinitionOfDone100GateTests`, `EnterpriseQualityGateCompletionTests`, `EnterpriseUiUxPolishTests` | Manual acceptance checklist chưa tick vì chưa có staging evidence |

## Findings

### Critical

Không phát hiện Critical mới trong lượt audit này.

### High

Không phát hiện High mới trong lượt audit này.

**High chấp nhận tạm:** Secret/config cụ thể trong `appsettings.json` vẫn còn theo quyết định vận hành trước đó. Báo cáo này không in giá trị secret. Nếu triển khai môi trường lớn hơn, vẫn nên externalize sang secret store hoặc biến môi trường và rotate key.

### Medium

#### RESOLVED MED-01 - Cảnh báo dự báo thiếu hàng còn dùng `Item.CurrentStock` aggregate

**File/line:** `Services/Enterprise1113Services.cs:141-148`.

**Mô tả:** `BuildPredictiveAlertsAsync` query trực tiếp `Items.CurrentStock <= MinThreshold` để sinh cảnh báo thiếu hàng. `CurrentStock` đang là shadow/aggregate được sync từ `ItemLocation`; nhiều module core đã coi `ItemLocation` là source of truth.

**Tác động nghiệp vụ:** Với nhiều kho/chủ hàng hoặc trường hợp aggregate chưa sync kịp, BI có thể cảnh báo thiếu hàng sai phạm vi kho, bỏ sót thiếu hàng theo kho, hoặc cảnh báo nhầm khi tổng toàn hệ thống còn hàng nhưng kho đang xem đã thiếu.

**Bằng chứng/tái hiện:** Audit thấy query ở `Services/Enterprise1113Services.cs:141-148` không join `ItemLocations`, không lọc warehouse/owner trước khi so với threshold.

**Đề xuất fix:** Chuyển stockout predictive alert sang query `ItemLocations` theo `warehouseId`, owner scope nếu có, dùng tồn khả dụng chuẩn `Quantity - ReservedQty` và fallback sync aggregate chỉ để tương thích UI cũ.

**Test cần thêm:** Hai kho cùng SKU: kho A thiếu, kho B dư; cảnh báo kho A phải hiện, kho B không hiện, và alert tổng không được che mất thiếu cục bộ.

#### MED-02 - Chưa kiểm được visual runtime có đăng nhập

**File/line:** `tests/visual/playwright.config.ts:8-13`, `tests/visual/.auth`.

**Mô tả:** Visual suite đã có scaffold nhưng yêu cầu `WMS_BASE_URL` và `WMS_AUTH_STATE` hoặc `tests/visual/.auth/wms-auth-state.json`. Thư mục `.auth` hiện chỉ có `.gitignore`.

**Tác động nghiệp vụ:** Không thể xác nhận bằng screenshot thật rằng các màn E1-E13 không đè layout, không vỡ mobile, sidebar collapsed/flyout ổn ở 100/110/125%, và các màn vận hành nâng cao không còn UI chen lấn.

**Bằng chứng/tái hiện:** Chạy inventory thấy `tests/visual/.auth` không có auth state. `playwright.config.ts` sẽ throw nếu thiếu env/base auth.

**Đề xuất fix:** Tạo tài khoản test/staging hợp lệ, chạy `npm run visual:auth` hoặc set `WMS_AUTH_STATE`, sau đó chạy `npm run visual:test` và lưu artifact theo release.

**Test cần thêm:** CI/staging job chạy visual với auth state đã cấp, ít nhất cho trang chính, tạo phiếu, duyệt phiếu, phiếu kho, inventory map, optimization, automation, integration, workflow, dock và yard.

### Low

#### RESOLVED LOW-01 - View kiểm tra chất lượng còn fallback raw enum và inline style dày

**File/line:** `Views/Operations/QualityInspection.cshtml:14-17`.

**Mô tả:** View fallback `v.InboundStatus.ToString()` và `v.VoucherType.ToString()` khi enum chưa được map, đồng thời còn nhiều inline style tĩnh.

**Tác động nghiệp vụ:** Nếu thêm trạng thái mới, UI có thể lộ nhãn tiếng Anh/kỹ thuật hoặc nhìn không đồng bộ với các màn enterprise đã Việt hóa.

**Đề xuất fix:** Dùng helper label chung cho `InboundStatus` và `VoucherType`; chuyển inline style tĩnh sang class CSS như các màn đã polish.

**Test cần thêm:** Static UI test không cho `.ToString()` user-facing ở `QualityInspection`; visual test cho màn QC.

#### LOW-02 - Còn nhiều link nội bộ hard-code bằng `href="/..."`

**Bằng chứng:** Static scan thấy 246 internal hard-coded href trong view. Không phát hiện URL có `%20` hoặc tiếng Việt, nên đây là nợ bảo trì chứ chưa phải bug route hiện tại.

**Tác động nghiệp vụ:** Khi đổi route/action trong tương lai, link hard-code dễ tái phát lỗi kiểu `Chuyển thẳng` trước đây.

**Đề xuất fix:** Ưu tiên chuyển link nhạy cảm trong `_Layout`, dashboard và Operations sang tag helper `asp-controller`/`asp-action`; giữ test chặn URL có dấu/khoảng trắng.

**Test cần thêm:** Static test cho các route trọng điểm và rule không cho URL `/Operations/*` chứa `%20`/non-ASCII.

#### LOW-03 - Inline style legacy vẫn còn nhiều ở một số view lớn

**Bằng chứng scan top:** `Views/Vouchers/Create.cshtml` 58, `Views/Reports/SpaceUtilization.cshtml` 19, `Views/Reports/DockToStock.cshtml` 18, `Views/Operations/QualityInspection.cshtml` 17, `Views/Reports/AuditTrail.cshtml` 16.

**Tác động nghiệp vụ:** Khó bảo trì responsive layout; dễ phát sinh lỗi text/nút chen nhau khi zoom hoặc mobile.

**Đề xuất fix:** Dọn theo cụm view khi có visual auth: form tạo phiếu, báo cáo không gian, dock-to-stock, QC, audit trail.

**Test cần thêm:** Static whitelist inline style động; visual desktop/mobile cho top 5 view còn inline style cao.

#### RESOLVED LOW-04 - Controller vẫn còn optional service fallback `new ...Service(...)`

**File/line:** `Controllers/OperationsController.cs:122-138`, `Controllers/VouchersController.cs:117-126`.

**Mô tả:** Sau refactor core, controller đã mỏng hơn nhưng constructor vẫn còn fallback tự tạo service khi DI null.

**Tác động nghiệp vụ:** Production thường vẫn resolve qua DI nên không vỡ ngay, nhưng fallback có thể làm test và runtime lệch dependency, nhất là service có outbox/audit/tenant scope.

**Đề xuất fix:** Đợt refactor tiếp theo bỏ nullable fallback, ép DI bắt buộc, cập nhật test factory/builder để inject đầy đủ.

**Test cần thêm:** Container resolve test và static test chặn `?? new .*Service` trong controller.

### Cosmetic

#### RESOLVED COS-01 - MinerU option còn default loopback host trong code

**File/line:** `Services/VoucherDocumentIntakeService.cs:19-25`.

**Mô tả:** `MinerUOptions.BaseUrl` default là `http://loopback IPv4:8000`. Đây là default dev hợp lý, nhưng trên host cần override rõ.

**Tác động:** Nếu bật `MinerU.Enabled=true` trên host mà chưa cấu hình `MinerU:BaseUrl`, chức năng đọc chứng từ sẽ báo dịch vụ chưa sẵn sàng.

**Trạng thái sau remediation:** Đã thêm startup warning `MINERU_LOOPBACK_PRODUCTION_WARNING` khi `Enabled=true` và BaseUrl vẫn là loopback host/loopback IPv4 trong non-development. Vẫn giữ `MINERU_HOST_DEPLOYMENT_GUIDE.md` làm hướng dẫn cấu hình host.

## Accepted Temporary Items

- Không sửa secret trong `appsettings.json` theo quyết định vận hành hiện tại.
- Không tự tạo tài khoản test hoặc nới bảo mật chỉ để chạy screenshot.
- Không sửa code nghiệp vụ trong lượt audit này.
- Visual authenticated runtime chỉ ghi blocker cho đến khi có auth state thật.

## Backlog Remaining

- Fix MED-02: tạo auth state staging và chạy `npm run visual:test`, lưu artifact screenshot.
- Tiếp tục LOW-03: dọn các view legacy còn inline style cao như `Views/Vouchers/Create.cshtml`, `Views/Reports/SpaceUtilization.cshtml`, `Views/Reports/DockToStock.cshtml`, `Views/Reports/AuditTrail.cshtml` khi có visual auth.
- Tiếp tục LOW-02 ở mức bảo trì: các link trọng điểm đã chuyển, phần hard-coded ASCII còn lại trong view legacy xử lý dần khi đụng module liên quan.
- Hoàn tất manual/staging evidence cho `MODULE_ACCEPTANCE_CHECKLIST.md`, `PRODUCTION_SECURITY_CHECKLIST.md`, k6 load test, backup/restore drill và visual release artifact.


