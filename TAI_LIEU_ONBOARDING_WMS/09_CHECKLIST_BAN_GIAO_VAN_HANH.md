# Checklist Bàn Giao Vận Hành

## 1. Trước khi bàn giao cho người vận hành

- Đã tạo kho, khu vực và vị trí cơ bản.
- Đã tạo đối tác chính: nhà cung cấp, khách hàng và chủ hàng nếu có.
- Đã tạo danh mục vật tư, đơn vị tính và vật tư mẫu.
- Đã tạo tài khoản theo vai trò: quản trị viên, quản lý kho, nhân viên kho và người chỉ xem.
- Đã gán kho thao tác, khu vực và phạm vi chủ hàng cho người dùng cần giới hạn.
- Đã kiểm tra thiết bị điện thoại có thể mở web, đăng nhập và quét mã.

## 2. Trước khi chạy nhập kho thật

- Phiếu nhập có kho, đối tác và dòng vật tư đúng.
- Vật tư theo lô có số lô.
- Vật tư theo hạn dùng có ngày hết hạn.
- Vật tư theo số sê-ri đã có danh sách số sê-ri đủ số lượng.
- Vật tư cần cân trọng lượng thực tế đã ghi nhận cân.
- Dòng cần cất hàng có vị trí cất hợp lệ cùng kho.
- Trung tâm ngoại lệ không còn lỗi chặn hoàn tất nhập.

## 3. Trước khi chạy xuất kho thật

- Phiếu xuất đã phát hành và giữ chỗ thành công.
- Nhiệm vụ lấy hàng có người xử lý hoặc có thể lấy qua màn nhiệm vụ tiếp theo.
- Nhân viên quét đúng mã hàng, vị trí, mã kiện và số sê-ri nếu có.
- Phiếu đã ghi sổ xuất trước khi đóng gói theo vòng đời hiện tại.
- Kiện xuất đã có cân trọng lượng thực tế nếu vật tư yêu cầu.
- Vận đơn hoặc chuyến xe đã được gắn theo quy tắc giao hàng.

## 4. Trước khi bàn giao vận chuyển

- Kiện xuất đã được quét lên chuyến nếu giao theo chuyến xe.
- Vận đơn ở trạng thái hợp lệ nếu cấu hình yêu cầu tạo vận đơn trước khi giao.
- Bản kê chuyến xe và biên bản bàn giao đã in hoặc có thể in lại.
- Báo cáo đối soát không còn lỗi thiếu vận đơn, thiếu chuyến, lệch chuyến hoặc kiện chưa xếp.
- Nhật ký bàn giao không bị trùng khi người dùng gửi lại thao tác.

## 5. Trước khi bàn giao cho lập trình viên mới

- Đọc hết tài liệu trong thư mục này theo thứ tự.
- Chạy `dotnet build WMS.sln --no-restore -v:minimal`.
- Chạy `dotnet test WMS.Tests\WMS.Tests.csproj --no-restore -v:minimal`.
- Không sửa dữ liệu mẫu, di trú hoặc cấu hình thật nếu chưa có người phụ trách xác nhận.
- Khi thay đổi giao diện, giữ tiếng Việt nhất quán và bổ sung kiểm thử tĩnh nếu thêm nhãn quan trọng.

