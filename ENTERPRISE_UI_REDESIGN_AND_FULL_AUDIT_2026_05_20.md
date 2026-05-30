# Enterprise WMS UI Redesign & Full Audit - 2026-05-20

## Phạm vi đã rà

- Inventory tự động: `Views` 129 file, `Controllers` 43 file, `Services` 39 file, `Models` 70 file, `ViewModels` 5 file, `wwwroot` 72 file, `docs` 4 file.
- Đã loại trừ nhóm build/runtime: `bin`, `obj`, `node_modules`, `artifacts`, `test-results`.
- Không đọc/in giá trị secret trong report. `appsettings.json` được giữ nguyên theo yêu cầu.

## Redesign đã triển khai

- `Views/Labels/Templates.cshtml`: chuyển sang dashboard cấu hình mẫu nhãn có KPI, filter, bảng dày thông tin, empty state và action rõ ràng.
- `Views/Labels/ItemRules.cshtml`: chuẩn hóa màn quy tắc mã hàng đối tác, form nhập liệu, bảng mapping và trạng thái.
- `Views/Labels/TemplateForm.cshtml`: chuẩn hóa form tạo/sửa mẫu nhãn, tách action động an toàn hơn và bỏ inline style.
- `Views/Labels/PrintJobs.cshtml`: nâng nhật ký in nhãn/chứng từ thành bảng vận hành có KPI, filter và export.
- `Views/Operations/QualityInspection.cshtml`: redesign quy trình kiểm phẩm, modal QC, trạng thái hiển thị, empty state và export.
- `Views/Operations/Waves.cshtml`: redesign bảng đợt gom đơn với playbook, KPI, progress bar và route tag helper.
- `Views/Operations/ZoneAssignment.cshtml`: redesign phân khu vực lấy hàng theo kiểu enterprise, có KPI, playbook, bảng phân quyền, confirm và link route đúng.
- `wwwroot/css/site.css`: thêm token dùng chung cho `enterprise-card`, label workbench, QC modal, wave progress, zone assignment và empty/inline alerts.

## Lỗi nghiệp vụ/UI đã sửa

- `SaveZoneAssignment` giờ kiểm tra warehouse scope ở POST, không chỉ dựa vào màn GET.
- Khu vực được gán phải active và thuộc kho hợp lệ của nhân viên/phạm vi người quản lý.
- GET `ZoneAssignment` chỉ lấy assignment thuộc tập nhân viên/khu vực đang hiển thị, tránh kéo dữ liệu ngoài phạm vi vào view.
- Thay link cũ `/Operations/Zones` bằng route quản lý kho/khu vực hợp lệ.
- Các form POST ở màn ưu tiên đều có antiforgery token.
- Các màn ưu tiên không còn `inline-style marker` hoặc `local-style block` cục bộ, trừ màn in nhãn/chứng từ vốn cần CSS print riêng.

## Regression tests

- Thêm `WMS.Tests/EnterpriseWmsUiFullAuditImplementationTests.cs`.
- Test kiểm: không inline style ở màn ưu tiên, POST có antiforgery, CSS token enterprise tồn tại, ZoneAssignment dùng route/scope an toàn, và màn mới không chứa marker mojibake đã biết.

## Pending cần môi trường thật

- Visual Playwright có đăng nhập cần auth state/tài khoản môi trường để chụp đủ route sau login.
- Manual smoke với máy quét/RF thật vẫn cần thiết cho luồng quét mã, in tem và thao tác kho trên thiết bị.

