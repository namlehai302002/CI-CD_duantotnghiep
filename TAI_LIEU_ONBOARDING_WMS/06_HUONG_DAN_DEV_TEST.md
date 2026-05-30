# 06 - Hướng Dẫn Cho Lập Trình Viên Mới

## Cách Chạy Dự Án

Từ thư mục gốc dự án:

```powershell
dotnet restore WMS.sln
dotnet build WMS.sln --no-restore -v:minimal
dotnet run --no-build --urls http://loopback host:5073
```

Đường dẫn mặc định khi chạy local:

```text
http://loopback host:5073
```

## Cách Chạy Kiểm Thử

```powershell
dotnet test WMS.Tests\WMS.Tests.csproj --no-restore -v:minimal
```

Trước khi bàn giao code, tối thiểu phải đạt:

- Build không cảnh báo, không lỗi.
- Test pass toàn bộ.
- Không sửa migration nếu nhiệm vụ không đổi dữ liệu.
- Không format hoặc chỉnh hàng loạt ngoài phạm vi nếu không cần.

## Cấu Trúc Thư Mục Quan Trọng

- `Controllers`: điểm vào từ giao diện.
- `Services`: nghiệp vụ chính.
- `Models`: entity và enum.
- `ViewModels`: dữ liệu màn hình.
- `Views`: Razor view.
- `wwwroot/css/site.css`: hệ thiết kế giao diện chính.
- `wwwroot/js`: mã quét điện thoại, hàng đợi quét, PWA.
- `Migrations`: lịch sử thay đổi cơ sở dữ liệu.
- `WMS.Tests`: kiểm thử nghiệp vụ, hồi quy và kiểm thử tĩnh giao diện.

## Quy Tắc Khi Sửa Nghiệp Vụ

- Đọc service hiện có trước khi sửa controller.
- Không ghi tồn kho trực tiếp nếu đã có service xử lý.
- Luồng làm đổi tồn cần ghi sổ giao dịch tồn kho nếu thuộc nghiệp vụ tồn.
- Không phá compatibility của dữ liệu cũ.
- Không drop cột hoặc bảng trong migration nếu chưa có yêu cầu rõ.
- Luôn nghĩ đến retry/idempotency với thao tác vận đơn, quét hàng, nhiệm vụ hoặc outbox.

## Quy Tắc Khi Sửa Giao Diện

- Dùng class chung trong `site.css`.
- Tránh inline style mới nếu không thật cần.
- Nhãn hiển thị phải là tiếng Việt dễ hiểu.
- Không dùng từ nửa Anh nửa Việt trong giao diện người dùng.
- Màn vận hành kho phải ưu tiên thao tác nhanh hơn trang trí.
- Bảng dữ liệu cần cuộn ngang ổn trên màn nhỏ.
- Form phải có label thật, lỗi rõ và nút hành động dễ thấy.

## Các Lệnh Kiểm Tra Hay Dùng

```powershell
dotnet build WMS.sln --no-restore -v:minimal
dotnet test WMS.Tests\WMS.Tests.csproj --no-restore -v:minimal
dotnet format WMS.sln --no-restore --verify-no-changes -v:minimal
dotnet list WMS.sln package --vulnerable --include-transitive
dotnet ef migrations list --no-build
```

## Khi Nào Cần Migration?

Cần migration khi thêm/sửa bảng, cột, index, ràng buộc hoặc seed dữ liệu. Không cần migration khi chỉ sửa view, CSS, JS, controller validation hoặc tài liệu.


