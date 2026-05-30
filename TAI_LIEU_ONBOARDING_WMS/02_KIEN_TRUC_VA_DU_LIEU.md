# 02 - Kiến Trúc Và Dữ Liệu

## Kiến Trúc Ứng Dụng

Hệ thống dùng ASP.NET Core MVC, Razor View, Entity Framework Core và SQL Server. Kiến trúc thực tế chia thành các lớp:

- `Controllers`: nhận yêu cầu từ giao diện, kiểm tra quyền, điều phối service và trả view hoặc JSON.
- `Services`: xử lý nghiệp vụ chính như nhập kho, xuất kho, mã kiện, số sê-ri, vận chuyển, đối soát.
- `Models`: định nghĩa bảng dữ liệu, enum trạng thái và quan hệ.
- `ViewModels`: dữ liệu dành riêng cho màn hình.
- `Views`: giao diện Razor.
- `WMS.Tests`: kiểm thử nghiệp vụ và kiểm thử tĩnh giao diện.

## Nguyên Tắc Thiết Kế Dữ Liệu

- Không xóa dữ liệu nghiệp vụ quan trọng nếu cần truy vết.
- Thao tác làm đổi tồn phải đi qua service có kiểm tra nghiệp vụ.
- Dữ liệu cũ được giữ lại khi nâng cấp, migration ưu tiên add-only.
- Tồn kho vận hành và sổ kiểm toán tồn kho là hai lớp khác nhau.
- Các nghiệp vụ lớn phải có chống lặp để tránh gửi lại tạo giao dịch trùng.

## Các Bảng Dữ Liệu Quan Trọng

### Vật Tư Và Tồn Kho

- `Item`: danh mục vật tư, cờ theo lô, hạn dùng, số sê-ri, cân trọng lượng thực tế.
- `ItemLocation`: tồn kho snapshot theo vật tư, kho, vị trí, lô, hạn dùng, trạng thái giữ hàng và chủ hàng.
- `InventoryTransaction`: sổ giao dịch tồn kho dùng để kiểm toán mọi biến động.

### Mã Kiện

- `LicensePlate`: mã kiện header, có mã kiện, kho, vị trí hiện tại, mã kiện cha, trạng thái và loại mã kiện.
- `LicensePlateDetail`: chi tiết vật tư nằm trong mã kiện.
- Mã kiện có thể chứa nhiều vật tư và có thể lồng nhau.

### Số Sê-ri

- `SerialNumber`: từng số sê-ri, gắn với vật tư, vị trí, mã kiện, trạng thái và chủ hàng.
- `SerialReservation`: giữ chỗ số sê-ri theo phiếu, dòng phiếu, nhiệm vụ lấy hàng và giữ chỗ tồn.
- `PickTaskSerialAssignment`: bản tương thích để giao diện/report cũ vẫn đọc được số sê-ri đã lấy.

### Nhập Xuất Và Nhiệm Vụ

- `Voucher`: phiếu nghiệp vụ, gồm phiếu nhập, phiếu xuất, điều chỉnh và chuyển kho.
- `VoucherDetail`: dòng vật tư của phiếu.
- `StockReservation`: giữ chỗ tồn cho xuất kho.
- `PickTask`: nhiệm vụ lấy hàng.
- `MovementTask`: nhiệm vụ di chuyển tồn hoặc mã kiện.

### Đóng Gói, Chuyến Xe Và Vận Đơn

- `OutboundPackage`: kiện xuất sau khi đóng gói.
- `ShipmentLoad`: chuyến xe gom nhiều phiếu hoặc kiện.
- `ShipmentLoadVoucher`: phiếu thuộc chuyến.
- `ShipmentLoadPackage`: kiện thuộc chuyến.
- `CarrierConnector`: cấu hình bộ kết nối vận tải.
- `CarrierShipment`: vận đơn theo kiện xuất.
- `CarrierShipmentEvent`: lịch sử trạng thái vận đơn hoặc callback từ đơn vị vận chuyển.

## Luồng Dữ Liệu Tồn Kho

1. Nhập kho làm tăng `ItemLocation` và ghi `InventoryTransaction`.
2. Mã kiện được tạo ở `LicensePlate` và `LicensePlateDetail`.
3. Nếu vật tư theo số sê-ri, từng số sê-ri được gắn vị trí/mã kiện.
4. Xuất kho giữ chỗ tồn bằng `StockReservation`.
5. Lấy hàng tạo hoặc cập nhật `PickTask`, số sê-ri được chuyển trạng thái theo reservation.
6. Ghi sổ xuất làm giảm tồn, ghi ledger và cập nhật trạng thái phiếu.
7. Đóng gói tạo `OutboundPackage`.
8. Giao hàng hoặc rời chuyến xe cập nhật trạng thái bàn giao và vận đơn.

## Những Điểm Dễ Nhầm

- `Quantity` trong chi tiết mã kiện là số lượng theo đơn vị cơ sở, không phải trọng lượng thực tế.
- Cân trọng lượng thực tế nằm ở `CatchWeightEntry`, không làm thay đổi nghĩa của số lượng tồn.
- Vận đơn theo kiện xuất, không thay thế phiếu xuất.
- Chuyến xe là lớp gom phiếu/kiện để bàn giao, không phải hệ thống quản lý vận tải đầy đủ.
- Bộ kết nối vận tải hiện là khung tích hợp và adapter giả lập/HTTP, chưa tích hợp hãng thật.

