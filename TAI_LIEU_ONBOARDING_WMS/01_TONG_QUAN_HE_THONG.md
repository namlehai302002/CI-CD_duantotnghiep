# 01 - Tổng Quan Hệ Thống

## WMS Pro Là Gì?

WMS Pro là hệ thống quản lý kho nội bộ dùng để điều hành các hoạt động kho lớn: nhập hàng, cất hàng, quản lý tồn, lấy hàng, đóng gói, giao hàng, điều phối chuyến xe, quản lý mã kiện, số sê-ri, cân trọng lượng thực tế và truy vết giao dịch tồn kho.

Hệ thống không phải sản phẩm Oracle, SAP hay Manhattan, nhưng được thiết kế theo hướng gần chuẩn của các hệ thống quản lý kho lớn: nghiệp vụ rõ, dữ liệu có kiểm soát, giao dịch không âm tồn, có phân quyền và có kiểm thử hồi quy.

## Nhóm Người Dùng Chính

- Quản trị viên: quản lý tài khoản, phân quyền, cấu hình hệ thống, xem toàn bộ dữ liệu.
- Quản lý kho: điều phối nhập, xuất, nhiệm vụ, ngoại lệ, báo cáo và giao hàng.
- Nhân viên kho: thao tác nhận hàng, lấy hàng, di chuyển, đóng gói và quét mã bằng điện thoại.
- Người chỉ xem: xem dữ liệu được phân quyền, không thực hiện thao tác làm đổi tồn kho.

## Các Khối Nghiệp Vụ Chính

- Nhập kho: tạo phiếu nhập, duyệt, nhận thực tế, kiểm phẩm, nhập số sê-ri, cân trọng lượng thực tế, tạo mã kiện và cất hàng.
- Xuất kho: tạo phiếu xuất, giữ chỗ tồn, tạo nhiệm vụ lấy hàng, lấy hàng, xử lý lấy thiếu, ghi sổ xuất, đóng gói và giao hàng.
- Tồn kho: quản lý số lượng theo vật tư, vị trí, lô, hạn dùng, trạng thái giữ hàng, chủ hàng và mã kiện.
- Mã kiện: quản lý pallet, thùng, tote hoặc case; hỗ trợ mã kiện chứa nhiều vật tư và mã kiện lồng nhau.
- Số sê-ri: quản lý từng đơn vị tồn độc lập, giữ chỗ trước khi lấy và chặn quét sai.
- Sổ giao dịch tồn kho: ghi lại mọi biến động số lượng, giữ chỗ, xuất, nhập, điều chỉnh và đối soát.
- Vận chuyển: quản lý kiện xuất, chuyến xe, vận đơn, nhãn vận chuyển, bàn giao và đối soát giao hàng.
- Kho nhiều chủ hàng: tách tồn kho, phiếu, mã kiện và chi phí theo chủ hàng.

## Điểm Mạnh Hiện Tại

- Có kiến trúc dữ liệu khá đầy đủ cho kho lớn: mã kiện container-centric, số sê-ri độc lập, sổ giao dịch tồn kho, chủ hàng, chuyến xe và vận đơn.
- Có kiểm soát nghiệp vụ ở nhiều điểm quan trọng: không âm tồn, không lấy sai số sê-ri, không giao khi thiếu điều kiện, không trộn chủ hàng.
- Có cơ chế hàng đợi quét khi mạng yếu ở phía trình duyệt cho các thao tác ngắn.
- Có kiểm thử tự động cho nhiều luồng hồi quy quan trọng.
- Có giao diện web dùng được trên điện thoại để thay thế thiết bị cầm tay giai đoạn đầu.

## Những Điều Người Mới Cần Nhớ

- `ItemLocation` là ảnh chụp tồn vận hành để truy vấn nhanh.
- `LicensePlate` là mã kiện hoặc container chứa hàng.
- `LicensePlateDetail` là chi tiết vật tư bên trong mã kiện.
- `SerialNumber` là đơn vị tồn riêng cho hàng theo số sê-ri.
- `InventoryTransaction` là sổ kiểm toán tồn kho, không được xóa lịch sử.
- `OutboundPackage` là kiện xuất sau đóng gói.
- `ShipmentLoad` là chuyến xe gom nhiều phiếu hoặc kiện xuất.
- `CarrierShipment` là vận đơn theo kiện xuất.

