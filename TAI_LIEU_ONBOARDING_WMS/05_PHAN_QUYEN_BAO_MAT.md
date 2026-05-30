# 05 - Phân Quyền Và Bảo Mật

## Nguyên Tắc Chung

- Cấp quyền tối thiểu đủ làm việc.
- Tài khoản không phải quản trị viên phải gắn kho thao tác.
- Không dùng chung tài khoản giữa nhiều người.
- Không xóa lịch sử thao tác của tài khoản cũ; nếu nghỉ việc thì khóa tài khoản.
- Những thao tác nhạy cảm cần có quyền rõ ràng và kiểm tra vai trò.

## Vai Trò Chính

### Quản trị viên

- Toàn quyền hệ thống.
- Quản lý tài khoản và phân quyền.
- Cấu hình kho, danh mục, quyền, khóa kỳ và thao tác rủi ro.
- Xem toàn bộ dữ liệu.

### Quản lý kho

- Điều phối nhập kho, xuất kho, nhiệm vụ và ngoại lệ.
- Xem báo cáo vận hành.
- Xử lý bổ sung hàng, phân công, chuyến xe và vận chuyển.
- Không nên có quyền thao tác hệ thống nguy hiểm nếu không cần.

### Nhân viên kho

- Nhận hàng, lấy hàng, di chuyển, đóng gói, quét kiện.
- Chỉ thao tác trong phạm vi kho được gán.
- Không cấu hình danh mục hoặc phân quyền.

### Chỉ xem

- Xem thông tin được phép.
- Không làm đổi tồn kho hoặc trạng thái phiếu.

## Các Lớp Phân Quyền

- Vai trò: Admin, Manager, Staff, Viewer.
- Quyền chi tiết: lưu ở bảng permission và role-permission.
- Kho: giới hạn theo `WarehouseId`.
- Chủ hàng: giới hạn theo owner partner nếu dùng kho nhiều chủ hàng.
- Khu vực: giới hạn theo zone assignment cho một số luồng vận hành.

## Tạo Tài Khoản Mới

Khi tạo tài khoản, cần kiểm:

1. Tên đăng nhập rõ ràng, gắn với nhân sự thật.
2. Họ tên đầy đủ.
3. Thư điện tử nếu có.
4. Vai trò đúng nhiệm vụ.
5. Kho thao tác đúng.
6. Mật khẩu mạnh.
7. Có cần gán chủ hàng hoặc khu vực không.

## Khóa Tài Khoản

Không nên xóa vật lý tài khoản vì hệ thống cần giữ lịch sử. Khi nhân sự nghỉ hoặc không còn quyền, thao tác đúng là khóa tài khoản.

## Đăng Nhập Và Thiết Bị Tin Cậy

Một số vai trò nhạy cảm có thể yêu cầu xác thực mạnh hơn. Màn thiết bị tin cậy dùng để quản lý thiết bị đã xác thực, giúp kiểm soát rủi ro khi tài khoản được dùng trên máy lạ.

