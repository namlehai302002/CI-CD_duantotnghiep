# 07 - Thuật Ngữ

## Thuật Ngữ Nghiệp Vụ

| Thuật ngữ | Nghĩa trong hệ thống |
|---|---|
| Phiếu nhập | Chứng từ đưa hàng vào kho. |
| Phiếu xuất | Chứng từ đưa hàng ra khỏi kho. |
| Vật tư | Mã hàng hoặc sản phẩm được quản lý tồn kho. |
| Vị trí | Ô/kệ/khu vực cụ thể trong kho. |
| Lô | Nhóm hàng cùng lô sản xuất hoặc nhập hàng. |
| Hạn dùng | Ngày hết hạn dùng cho vật tư cần quản lý hạn. |
| Số sê-ri | Mã định danh từng đơn vị hàng riêng lẻ. |
| Mã kiện | Mã định danh pallet, thùng, tote hoặc container chứa hàng. |
| Mã kiện lồng nhau | Mã kiện cha chứa một hoặc nhiều mã kiện con. |
| Giữ chỗ | Tồn được khóa trước cho phiếu xuất. |
| Nhiệm vụ lấy hàng | Công việc cụ thể giao cho nhân viên lấy hàng. |
| Lấy thiếu | Nhân viên không lấy đủ số lượng được giao. |
| Đóng gói | Gom hàng đã xuất vào kiện xuất. |
| Kiện xuất | Gói hàng sau khi đóng gói để giao. |
| Chuyến xe | Đợt bàn giao gom nhiều phiếu hoặc kiện. |
| Vận đơn | Mã giao hàng từ đơn vị vận chuyển. |
| Bộ kết nối vận tải | Cấu hình cách WMS kết nối với đơn vị vận chuyển. |
| Đối soát giao hàng | Kiểm tra lệch giữa phiếu, kiện, chuyến xe, vận đơn và bàn giao. |
| Cân trọng lượng thực tế | Ghi nhận cân nặng thật của hàng cần quản lý trọng lượng. |
| Chủ hàng | Khách hàng sở hữu hàng trong mô hình kho nhiều chủ hàng. |

## Tên Kỹ Thuật Thường Gặp

| Tên kỹ thuật | Diễn giải |
|---|---|
| `Item` | Bảng vật tư. |
| `ItemLocation` | Snapshot tồn kho theo vị trí. |
| `LicensePlate` | Mã kiện header. |
| `LicensePlateDetail` | Chi tiết vật tư bên trong mã kiện. |
| `SerialNumber` | Số sê-ri của từng đơn vị hàng. |
| `SerialReservation` | Giữ chỗ số sê-ri cho xuất kho. |
| `Voucher` | Phiếu nghiệp vụ. |
| `VoucherDetail` | Dòng chi tiết phiếu. |
| `PickTask` | Nhiệm vụ lấy hàng. |
| `MovementTask` | Nhiệm vụ di chuyển. |
| `OutboundPackage` | Kiện xuất. |
| `ShipmentLoad` | Chuyến xe. |
| `CarrierConnector` | Bộ kết nối vận tải. |
| `CarrierShipment` | Vận đơn theo kiện xuất. |
| `InventoryTransaction` | Sổ giao dịch tồn kho. |
| `CatchWeightEntry` | Bản ghi cân trọng lượng thực tế. |
| `IntegrationOutbox` | Hàng đợi gửi tích hợp ra ngoài. |

## Cách Dịch Thống Nhất

- Warehouse management system: hệ thống quản lý kho.
- Enterprise: cấp doanh nghiệp lớn hoặc quy mô doanh nghiệp lớn.
- License plate: mã kiện.
- Serial: số sê-ri.
- Shipment/load: chuyến xe hoặc chuyến giao hàng tùy ngữ cảnh.
- Carrier connector: bộ kết nối vận tải.
- Carrier shipment: vận đơn.
- Outbox: hàng đợi tích hợp.
- Reconciliation: đối soát.
- Dashboard: bảng điều hành.

