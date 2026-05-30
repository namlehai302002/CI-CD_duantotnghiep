# Tài Liệu Onboarding Hệ Thống WMS Pro

Thư mục này dùng để bàn giao cho người mới tham gia dự án hoặc người vận hành mới cần hiểu nhanh hệ thống quản lý kho nội bộ WMS Pro.

## Nên Đọc Theo Thứ Tự

1. `01_TONG_QUAN_HE_THONG.md` - hiểu hệ thống này làm gì, phục vụ ai, đang mạnh ở đâu.
2. `02_KIEN_TRUC_VA_DU_LIEU.md` - hiểu kiến trúc ứng dụng, dữ liệu, mã kiện, tồn kho, số sê-ri và sổ giao dịch.
3. `03_NGHIEP_VU_NHAP_XUAT_TON.md` - hiểu luồng nhập kho, xuất kho, tồn kho và các chặn nghiệp vụ quan trọng.
4. `04_VAN_HANH_DIEN_THOAI_VAN_CHUYEN.md` - hiểu quét bằng điện thoại, chuyến xe, vận đơn và bộ kết nối vận tải.
5. `05_PHAN_QUYEN_BAO_MAT.md` - hiểu vai trò, quyền, kho, chủ hàng và nguyên tắc cấp tài khoản.
6. `06_HUONG_DAN_DEV_TEST.md` - hướng dẫn chạy dự án, build, test và kiểm tra trước khi bàn giao.
7. `07_THUAT_NGU.md` - bảng thuật ngữ Việt hóa để đọc tài liệu và giao diện không bị rối.
8. `08_BAN_DO_MAN_HINH_VA_LUONG_DU_LIEU.md` - bản đồ màn hình, luồng dữ liệu nhập/xuất/tồn và vận chuyển.
9. `09_CHECKLIST_BAN_GIAO_VAN_HANH.md` - checklist bàn giao cho vận hành kho và lập trình viên mới.

## Mục Tiêu Của Bộ Tài Liệu

- Giúp người mới hiểu bức tranh tổng thể trong 1 buổi đọc.
- Giúp quản lý kho hiểu các luồng vận hành chính mà không cần đọc mã nguồn.
- Giúp lập trình viên mới biết nên bắt đầu từ controller, service, model và test nào.
- Giúp cả nhóm dùng cùng một ngôn ngữ nghiệp vụ, tránh hiểu nhầm giữa “tồn kho”, “mã kiện”, “số sê-ri”, “vận đơn” và “chuyến xe”.
- Giúp bạn có thể gửi nguyên thư mục này cho người mới để họ đọc theo thứ tự và nắm được bức tranh tổng thể trước khi vào mã nguồn.

## Trạng Thái Hệ Thống Khi Viết Tài Liệu

- Hệ thống đã có lõi nhập kho, xuất kho, tồn kho, mã kiện lồng nhau, số sê-ri nghiêm ngặt, sổ giao dịch tồn kho, bổ sung hàng tự động, chuyến xe, cân trọng lượng thực tế, kho nhiều chủ hàng, tính phí dịch vụ, thiết bị tự động giả lập, bộ kết nối vận tải và giao diện quét bằng điện thoại.
- Giao diện đang được chuẩn hóa theo phong cách hệ thống nội bộ cấp doanh nghiệp lớn.
- Kiểm thử tự động hiện bao phủ nhiều luồng nghiệp vụ chính; trước khi bàn giao luôn chạy build và test.

