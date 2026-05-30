# 04 - Vận Hành Điện Thoại, Chuyến Xe Và Vận Chuyển

## Quét Bằng Điện Thoại

Hệ thống ưu tiên dùng web trên điện thoại thay vì mua thiết bị cầm tay ngay từ đầu. Nhân viên kho mở web, đăng nhập và dùng camera để quét mã vạch hoặc mã QR.

Các màn chính:

- Nhận hàng bằng điện thoại.
- Lấy hàng bằng điện thoại.
- Di chuyển tồn kho.
- Nhiệm vụ tiếp theo.
- Tra cứu mã kiện.
- Tra cứu số sê-ri.
- Chi tiết chuyến xe để quét kiện lên chuyến.

## Hàng Đợi Quét Khi Mạng Yếu

Một số thao tác quét ngắn có thể được lưu tạm trong trình duyệt khi mạng yếu:

- Bắt đầu nhận hàng.
- Xác nhận nhiệm vụ lấy hàng.
- Xác nhận nhiệm vụ di chuyển.
- Quét kiện lên chuyến xe.

Hệ thống không hỗ trợ hoàn tất nghiệp vụ lớn khi mất mạng dài. Những thao tác như hoàn tất nhập, ghi sổ xuất, giao hàng hoặc đóng chuyến vẫn cần kết nối ổn định.

## Chuyến Xe

`ShipmentLoad` là chuyến xe trong WMS. Nó dùng để gom nhiều phiếu hoặc nhiều kiện xuất vào một đợt bàn giao.

Các trạng thái thường gặp:

- Lên kế hoạch.
- Đã tập kết.
- Đang xếp hàng.
- Đã xếp xong.
- Đã rời kho.
- Đã đóng.
- Đã hủy.

Chuyến xe giúp kiểm soát:

- Phiếu nào thuộc chuyến nào.
- Kiện nào đã quét lên chuyến.
- Cửa bến hoặc bãi đỗ liên quan.
- Bản kê chuyến xe và biên bản bàn giao.

## Vận Đơn

`CarrierShipment` là vận đơn ở cấp kiện xuất. Một phiếu có thể có nhiều kiện, vì vậy có thể có nhiều vận đơn.

Vận đơn dùng để:

- Lưu mã vận đơn.
- Lưu trạng thái tạo vận đơn.
- Lưu nhãn vận chuyển.
- Lưu lỗi gửi hãng vận chuyển.
- Lưu số lần thử lại.
- Nhận trạng thái giao hàng từ đơn vị vận chuyển.

## Bộ Kết Nối Vận Tải Là Gì?

Bộ kết nối vận tải là nơi cấu hình cách WMS kết nối với đơn vị vận chuyển. Ví dụ sau này có thể gắn GHN, GHTK, Viettel Post, DHL, FedEx hoặc UPS.

Hiện tại hệ thống có:

- Kết nối giả lập: dùng để kiểm thử nội bộ, tạo vận đơn giả ngay trong hệ thống.
- Kết nối HTTP qua hàng đợi tích hợp: chuẩn bị sẵn để gửi yêu cầu ra API hãng vận chuyển sau này.

Các thao tác dự kiến:

- Tạo vận đơn.
- Hủy vận đơn.
- Gửi lại vận đơn lỗi.
- Đồng bộ trạng thái.
- Nhận callback trạng thái từ hãng.
- In nhãn vận chuyển.

## Điều Phối Vận Chuyển

Màn điều phối vận chuyển cho biết:

- Kiện nào chưa có vận đơn.
- Kiện nào đã tạo vận đơn.
- Kiện nào lỗi kết nối.
- Kiện nào đã hủy vận đơn.
- Kiện nào hãng báo giao thất bại.
- Kiện nào cần gửi lại hoặc đồng bộ trạng thái.

## Đối Soát Giao Hàng

Báo cáo đối soát giao hàng chỉ cảnh báo, không tự sửa dữ liệu. Nó phát hiện:

- Phiếu đã giao nhưng thiếu vận đơn.
- Phiếu đã giao nhưng thiếu mã chuyến bàn giao.
- Kiện chưa quét lên chuyến.
- Chuyến rời kho nhưng còn kiện chưa xếp.
- Vận đơn bị hủy nhưng phiếu đã giao.
- Hãng báo giao thất bại nhưng WMS đã ghi giao.

