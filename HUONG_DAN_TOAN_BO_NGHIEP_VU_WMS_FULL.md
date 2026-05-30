# HƯỚNG DẪN THỰC HIỆN TOÀN BỘ NGHIỆP VỤ HỆ THỐNG WMS PRO

Tài liệu này viết theo mục tiêu:
- Ghi lại **toàn bộ nghiệp vụ đang có trong hệ thống**.
- Hướng dẫn theo kiểu **người mới nhìn vào vẫn biết phải bấm ở đâu, làm gì tiếp theo**.
- Dùng được cho cả:
  - học sử dụng hệ thống,
  - làm tài liệu nộp,
  - làm script demo khi bảo vệ.

Phạm vi tài liệu này bao phủ:
- Đăng nhập, bảo mật, thiết bị tin cậy.
- Danh mục nền: kho, khu, vị trí, đối tác, danh mục vật tư, đơn vị tính, quy cách đóng gói.
- Sản phẩm / vật tư.
- Toàn bộ **8 loại phiếu**.
- Nghiệp vụ nhập kho, xuất kho, chuyển kho, kiểm kê, chốt tồn, khóa kỳ.
- Nghiệp vụ vận hành kho lớn: wave, pick task, nhận hàng bằng thiết bị cầm tay, lấy hàng bằng thiết bị cầm tay, số sê-ri, LPN, chuyển thẳng, replenishment, slotting.
- Báo cáo, cảnh báo, audit trail.
- Quản trị người dùng và các thao tác hệ thống.

---

## 1. Tổng quan cách hiểu hệ thống

Nếu giải thích cho người chưa biết gì, có thể hiểu hệ thống theo sơ đồ đơn giản sau:

1. Khai báo dữ liệu nền.
2. Khai báo vật tư.
3. Tạo phiếu nhập hoặc phiếu xuất.
4. Duyệt nghiệp vụ.
5. Thao tác kho thực tế.
6. Cập nhật tồn.
7. Tra cứu, kiểm kê, báo cáo, khóa kỳ.

Nói ngắn gọn:
- Muốn có một mã hàng trong hệ thống: vào `Sản phẩm / vật tư`.
- Muốn đưa hàng vào kho: tạo `Phiếu nhập`.
- Muốn đưa hàng ra khỏi kho: tạo `Phiếu xuất`.
- Muốn biết hàng còn bao nhiêu: vào `Xem tồn kho`.
- Muốn biết ai đã làm gì: vào `Nhật ký`.

---

## 2. Các vai trò trong hệ thống

### 2.1 Admin
Được dùng cho:
- quản trị hệ thống,
- quản lý người dùng,
- xem nhật ký,
- chốt tồn,
- khóa kỳ,
- xử lý các nghiệp vụ quan trọng nhất.

### 2.2 Manager
Được dùng cho:
- quản lý kho,
- duyệt phiếu,
- xác nhận nhận hàng,
- tạo nhiệm vụ lấy hàng,
- chốt xuất,
- xử lý các màn vận hành nâng cao.

### 2.3 Staff
Được dùng cho:
- tạo phiếu,
- nhận hàng,
- quét mã,
- lấy hàng,
- xác nhận thao tác thực tế trong kho.

### 2.4 Viewer
Được dùng cho:
- chỉ xem dữ liệu,
- không thao tác nghiệp vụ.

### 2.5 Nguyên tắc nghiệp vụ cần nhớ
- Người tạo phiếu và người duyệt cuối **không nên là cùng một người**.
- Tạo phiếu không phải lúc nào cũng tăng tồn ngay.
- Xuất kho không được làm âm tồn.
- Nếu vật tư có quản lý lô, hạn dùng, số sê-ri thì phải khai báo đầy đủ ở đúng màn.

---

## 3. Cấu trúc menu của hệ thống

### 3.1 Trang chính
Route:
- `/`

Dùng để:
- xem tổng quan hệ thống,
- xem chỉ số nhanh,
- theo dõi tình hình kho.

### 3.2 Nhập kho
Các màn:
- `/Vouchers/Create?type=1` - Tạo phiếu nhập
- `/Operations/Receiving` - Tiếp nhận hàng
- `/Operations/RfReceiving` - Quét nhận hàng bằng thiết bị cầm tay

### 3.3 Xuất kho
Các màn:
- `/Vouchers/Create?type=2` - Tạo phiếu xuất
- `/Operations/Waves` - Đợt lấy hàng
- `/Operations/PickTasks` - Nhiệm vụ lấy hàng
- `/Operations/RfPicking` - Quét lấy hàng bằng thiết bị cầm tay
- `/Operations/Shipping` - Đóng gói và giao hàng
- `/Operations/Chuyển thẳngOpportunities` - Chuyển thẳng

### 3.4 Tồn kho
Các màn:
- `/Items` - Sản phẩm / vật tư
- `/Reports/Inventory` - Xem tồn kho
- `/Warehouses/InventoryMap` - Sơ đồ kho
- `/Reports/StockMovement` - Lịch sử nhập xuất
- `/Operations/LpnLookup` - Tra cứu mã kiện
- `/Operations/SerialLookup` - Tra cứu số sê-ri
- `/Operations/Bổ sung hàng` - Bổ sung hàng
- `/Operations/Tối ưu vị trí` - Tối ưu vị trí
- `/Operations/LaborProductivity` - Năng suất lao động

### 3.5 Tra cứu phiếu
Route:
- `/Vouchers`

### 3.6 Báo cáo
Các màn:
- `/Reports/OpsKpi`
- `/Reports/StockCount`
- `/Reports/TopItems`
- `/Reports/ExpiryReport`
- `/Reports/SlowMovingReport`
- `/Reports/AbcAnalysis`
- `/Operations/ExceptionCenter`

### 3.7 Danh mục
Các màn:
- `/Warehouses`
- `/Partners`
- `/Categories`
- `/Units`

### 3.8 Hệ thống
Các màn:
- `/Users`
- `/Reports/Alerts`
- `/Reports/AuditTrail`
- `/Reports/StockSnapshot`
- `/Reports/PeriodLocks`
- `/Account/TrustedDevices`

### 3.9 Trợ giúp
Route:
- `/Help`

---

## 4. Tài khoản, đăng nhập và bảo mật

### 4.1 Đăng nhập
Route:
- `/Account/Login`

Các bước:
1. Mở màn đăng nhập.
2. Nhập tên đăng nhập và mật khẩu.
3. Nếu hệ thống bật xác thực nhiều lớp, làm tiếp bước MFA.
4. Đăng nhập thành công thì hệ thống chuyển vào trang chính.

### 4.2 Xác thực MFA
Route:
- `/Account/VerifyMfa`

Các bước:
1. Sau khi nhập đúng mật khẩu, hệ thống yêu cầu mã xác thực.
2. Nhập mã MFA.
3. Có thể chọn ghi nhớ thiết bị nếu hệ thống cho phép.
4. Hoàn tất đăng nhập.

### 4.3 Thiết bị tin cậy
Route:
- `/Account/TrustedDevices`

Các bước:
1. Mở màn `Thiết bị tin cậy`.
2. Xem danh sách thiết bị đã được cho phép.
3. Có thể:
   - thu hồi thiết bị hiện tại,
   - thu hồi tất cả thiết bị tin cậy.

Khi dùng:
- khi nghi ngờ tài khoản đã đăng nhập ở máy lạ,
- khi đổi người dùng máy,
- khi muốn buộc xác thực lại.

### 4.4 Tạo Admin đầu tiên
Route:
- `/Account/SetupAdmin`

Dùng khi:
- hệ thống chưa có tài khoản nào.

Các bước:
1. Mở màn setup admin.
2. Điền tên đăng nhập, họ tên, thư điện tử, mật khẩu.
3. Lưu.
4. Đăng nhập lại bằng tài khoản admin vừa tạo.

### 4.5 Đăng ký tài khoản
Route:
- `/Account/Register`

Lưu ý:
- có thể không dùng trong môi trường thật nếu hệ thống nội bộ chỉ cho admin tạo user.

### 4.6 Dev reset password
Route:
- `/Account/DevResetPassword`

Chỉ dùng:
- môi trường dev,
- test,
- không dùng cho demo vận hành thật nếu không cần.

---

## 5. Danh mục nền phải có trước khi chạy nghiệp vụ

Thứ tự thiết lập nên làm:
1. Kho.
2. Khu.
3. Vị trí.
4. Đối tác.
5. Danh mục vật tư.
6. Đơn vị tính.
7. Quy cách đóng gói.
8. Vật tư.

---

## 6. Cấu hình kho, khu, vị trí

### 6.1 Danh sách kho
Route:
- `/Warehouses`

Dùng để:
- xem toàn bộ kho,
- thêm kho mới,
- sửa kho,
- mở chi tiết kho.

### 6.2 Tạo kho mới
Route:
- `/Warehouses/Create`

Các bước:
1. Vào `Danh mục -> Cấu hình kho`.
2. Nhấn `Tạo mới`.
3. Nhập mã kho, tên kho, thông tin mô tả nếu có.
4. Lưu.

Kết quả:
- kho mới xuất hiện trong danh sách,
- các phiếu sau này có thể chọn kho đó.

### 6.3 Sửa kho
Route:
- `/Warehouses/Edit/{id}`

Các bước:
1. Chọn kho cần sửa.
2. Cập nhật thông tin.
3. Lưu.

### 6.4 Xóa kho
Thực hiện tại:
- danh sách kho hoặc chi tiết kho tùy giao diện.

Lưu ý:
- không xóa được nếu trong kho còn dữ liệu vật tư/tồn kho liên quan.

### 6.5 Xem chi tiết kho
Route:
- `/Warehouses/Details/{id}`

Dùng để:
- xem thông tin kho,
- tạo khu,
- tạo vị trí,
- xem cấu trúc lưu trữ bên trong kho.

### 6.6 Tạo khu (Zone)
Thực hiện tại màn chi tiết kho.

Có thể tạo các loại khu:
- lưu trữ,
- tiếp nhận,
- xuất hàng,
- staging,
- chuyển thẳng.

Các bước:
1. Mở chi tiết kho.
2. Chọn tạo khu.
3. Nhập mã khu, tên khu, loại khu.
4. Lưu.

### 6.7 Tạo khu kèm vị trí tự động
Thao tác liên quan:
- `CreateZoneWithLocations`

Dùng khi:
- muốn tạo nhanh một khu và sinh ra các vị trí con đi kèm.

### 6.8 Tạo vị trí
Thực hiện tại màn chi tiết kho.

Các bước:
1. Chọn khu cần thêm vị trí.
2. Nhập mã vị trí.
3. Nếu hệ thống có rack/shelf/bin thì nhập thêm.
4. Lưu.

### 6.9 Sơ đồ kho
Route:
- `/Warehouses/InventoryMap`

Dùng để:
- xem trực quan vị trí nào đang chứa hàng,
- kiểm tra mật độ lưu trữ,
- hỗ trợ điều phối kho.

Các bước:
1. Vào `Tồn kho -> Sơ đồ kho`.
2. Chọn kho.
3. Quan sát các vị trí.
4. Nhấn vào vị trí nếu giao diện cho phép để xem chi tiết tồn tại vị trí đó.

### 6.10 API / công cụ gợi ý vị trí
Các action liên quan:
- `GetSuggestedLocations`
- `GetItemLocations`
- `CheckLocationConflict`
- `GetVoucherLocations`

Ý nghĩa:
- hỗ trợ hệ thống gợi ý vị trí,
- kiểm tra quy tắc 1 ô 1 vật tư,
- dùng trong tạo phiếu và tối ưu xếp hàng.

---

## 7. Đối tác

### 7.1 Danh sách đối tác
Route:
- `/Partners`

Dùng để quản lý:
- nhà cung cấp,
- khách hàng,
- đối tác dùng chung cả hai vai trò.

### 7.2 Tạo đối tác
Route:
- `/Partners/Create`

Các bước:
1. Vào `Danh mục -> Đối tác`.
2. Nhấn `Thêm mới`.
3. Nhập mã đối tác, tên đối tác, loại đối tác.
4. Nhập thông tin liên hệ nếu có.
5. Lưu.

### 7.3 Sửa đối tác
Route:
- `/Partners/Edit/{id}`

### 7.4 Xóa đối tác
Thực hiện tại danh sách đối tác.

Lưu ý:
- nên kiểm tra xem đối tác đã được dùng trong phiếu hay chưa.

---

## 8. Danh mục vật tư

### 8.1 Danh sách danh mục vật tư
Route:
- `/Categories`

Dùng để:
- tạo nhóm vật tư,
- chia vật tư theo loại quản lý nội bộ.

### 8.2 Tạo danh mục vật tư
Route:
- `/Categories/Create`

Các bước:
1. Vào `Danh mục -> Danh mục vật tư`.
2. Nhấn tạo mới.
3. Nhập mã danh mục.
4. Nhập tên danh mục.
5. Nhập thứ tự sắp xếp nếu cần.
6. Lưu.

### 8.3 Sửa danh mục vật tư
Route:
- `/Categories/Edit/{id}`

### 8.4 Xóa danh mục vật tư
Lưu ý:
- không xóa được nếu đang có vật tư dùng danh mục đó.

---

## 9. Đơn vị tính và quy cách đóng gói

### 9.1 Danh sách đơn vị tính
Route:
- `/Units`

Dùng để:
- tạo đơn vị tính cơ sở,
- quản lý quy cách đóng gói.

### 9.2 Tạo đơn vị tính
Thao tác:
- `UnitsController.Create`

Các bước:
1. Vào `Danh mục -> Đơn vị tính`.
2. Nhập mã đơn vị.
3. Nhập tên đơn vị.
4. Nhập nhóm đơn vị nếu giao diện có.
5. Lưu.

Ví dụ:
- cái,
- kg,
- lít,
- hộp,
- thùng.

### 9.3 Xóa đơn vị tính
Lưu ý:
- không xóa được nếu vật tư đang dùng làm đơn vị cơ sở.

### 9.4 Tạo quy cách đóng gói
Thao tác:
- `CreatePackaging`

Các bước:
1. Mở màn đơn vị tính.
2. Chọn thêm quy cách đóng gói.
3. Nhập tên đóng gói.
4. Chọn đơn vị cơ sở.
5. Nhập giá trị quy đổi.
6. Lưu.

Ví dụ:
- 1 thùng = 24 chai,
- 1 bao = 50 kg.

### 9.5 Xóa quy cách đóng gói
Thao tác:
- `DeletePackaging`

---

## 10. Sản phẩm / vật tư

### 10.1 Danh sách vật tư
Route:
- `/Items`

Dùng để:
- xem toàn bộ vật tư,
- lọc theo danh mục, loại, tồn kho,
- tìm kiếm theo tên, mã,
- vào chi tiết, sửa, in nhãn.

### 10.2 Tạo vật tư mới
Route:
- `/Items/Create`

Các bước:
1. Vào `Tồn kho -> Sản phẩm / vật tư`.
2. Nhấn `Thêm mới`.
3. Nhập mã vật tư.
4. Nhập tên vật tư.
5. Nhập mã vạch nếu có.
6. Nhập SKU nội bộ nếu có.
7. Chọn danh mục.
8. Chọn loại vật tư.
9. Chọn đơn vị tính cơ bản.
10. Nhập cân nặng, kích thước nếu cần.
11. Chọn vị trí mặc định nếu muốn.
12. Nhập tồn tối thiểu, tối đa, điểm đặt hàng lại nếu muốn cảnh báo tồn.
13. Bật:
    - quản lý HSD,
    - quản lý theo lô,
    - quản lý theo số sê-ri
    tùy từng vật tư.
14. Thêm mô tả, thông số kỹ thuật, hình ảnh nếu cần.
15. Lưu.

Lưu ý quan trọng:
- Màn này chỉ khai báo **master data**.
- Màn này **không cộng tồn kho**.

### 10.3 Sinh mã vật tư tự động
Thao tác:
- `/Items/GenerateItemCode`

Dùng khi:
- muốn chọn loại vật tư trước, để hệ thống gợi ý mã.

### 10.4 Sửa vật tư
Route:
- `/Items/Edit/{id}`

### 10.5 Xóa vật tư
Điều kiện chặn:
- còn tồn kho,
- còn phiếu chưa ghi sổ liên quan.

### 10.6 Chi tiết vật tư
Route:
- `/Items/Details/{id}`

Dùng để:
- xem thông tin đầy đủ,
- xem tồn,
- xem ảnh,
- in tem nhanh.

### 10.7 Tìm vật tư bằng mã vạch
Thao tác:
- `/Items/GetItemByMã vạch`

Dùng trong:
- quét nhập,
- quét xuất,
- quick search,
- map mã vạch vào phiếu.

### 10.8 In tem nhãn hàng loạt
Routes:
- `/Items/PrintSetup`
- `/Items/PrintLabels`

Các bước:
1. Vào danh sách vật tư.
2. Chọn các vật tư cần in.
3. Mở `PrintSetup`.
4. Nhập số lượng tem cho từng vật tư.
5. Chọn khổ tem.
6. In.

### 10.9 In nhanh 1 vật tư
Route:
- `/Items/PrintSingle`

Dùng khi:
- đang ở chi tiết vật tư và muốn in tem ngay.

---

## 11. Toàn bộ các loại phiếu trong hệ thống

Hệ thống có 8 loại phiếu:
1. Nhập kho.
2. Xuất kho.
3. Trả NCC.
4. Khách trả.
5. Điều chỉnh.
6. Chuyển kho.
7. Nhập thành phẩm.
8. Xuất sản xuất.

Tất cả đều đi qua màn tạo phiếu chung:
- `/Vouchers/Create`

Chỉ khác nhau ở tham số `type`.

---

## 12. Tạo phiếu nhập kho

### 12.1 Route
- `/Vouchers/Create?type=1`

### 12.2 Dùng khi nào
- hàng mua về nhập kho,
- vật tư nhận từ nhà cung cấp,
- vật tư cần cộng tồn qua quy trình nhập.

### 12.3 Các bước
1. Vào `Nhập kho -> Tạo phiếu nhập`.
2. Chọn kho.
3. Chọn đối tác là nhà cung cấp nếu có.
4. Nhập số chứng từ gốc.
5. Nhập diễn giải.
6. Nhập dữ liệu ASN nếu có:
   - giờ xe đến dự kiến,
   - giờ bắt đầu tiếp nhận,
   - giờ kết thúc tiếp nhận,
   - cửa dock,
   - đơn vị vận chuyển,
   - biển số xe,
   - tài xế,
   - số điện thoại tài xế.
7. Thêm dòng vật tư:
   - chọn vật tư,
   - nhập số lượng,
   - nhập vị trí,
   - nhập số lô,
   - nhập hạn dùng,
   - nhập số sê-ri nếu cần.
8. Có thể quét mã vạch để thêm nhanh vật tư.
9. Có thể import Excel hoặc quét hóa đơn AI nếu dùng tính năng này.
10. Lưu nháp hoặc lưu và trình duyệt.

### 12.4 Sau khi tạo xong
Phiếu nhập có thể đi theo luồng:
1. Nháp.
2. Chờ duyệt.
3. Đã duyệt.
4. Đang nhận hàng.
5. Hoàn tất.

### 12.5 Các thao tác tiếp theo tại chi tiết phiếu
Route:
- `/Vouchers/Details/{id}`

Các nút có thể có:
- `Trình duyệt`
- `Duyệt phiếu`
- `Từ chối`
- `Xác nhận nhận hàng`
- `Tăng tồn`
- `ASN & Dock`

### 12.6 Khi nào tồn mới tăng
- khi phiếu được hoàn tất đúng bước duyệt cuối.

---

## 13. Tạo phiếu xuất kho

### 13.1 Route
- `/Vouchers/Create?type=2`

### 13.2 Dùng khi nào
- xuất nội bộ,
- xuất bán/ra ngoài,
- giao cho bộ phận khác,
- xuất cho luồng pick, pack, ship.

### 13.3 Các bước
1. Vào `Xuất kho -> Tạo phiếu xuất`.
2. Chọn kho xuất.
3. Chọn loại xuất:
   - xuất nội bộ,
   - xuất bán / ra ngoài.
4. Chọn đối tác nếu là xuất cho khách hoặc đối tác.
5. Nhập số chứng từ gốc nếu có.
6. Nhập ghi chú.
7. Nhập ngày cần hàng (SLA) nếu cần.
8. Thêm vật tư cần xuất.
9. Nhập số lượng.
10. Lưu phiếu.

### 13.4 Luồng chuẩn của phiếu xuất
1. Tạo phiếu.
2. Xác nhận tạo nhiệm vụ lấy hàng.
3. Nhân viên lấy hàng.
4. Chốt phần đã lấy hoặc chốt và hủy phần còn lại.
5. Đóng gói.
6. Giao hàng.

### 13.5 Các nút thường gặp tại chi tiết phiếu xuất
- `Xác nhận tạo nhiệm vụ lấy hàng`
- `Chốt phần đã lấy`
- `Chốt & hủy phần còn lại`
- `Đóng gói`
- `Giao hàng`

---

## 14. Phiếu trả NCC

### 14.1 Route
- `/Vouchers/Create?type=3`

### 14.2 Dùng khi nào
- trả hàng lỗi,
- trả hàng dư,
- trả hàng không đạt về nhà cung cấp.

### 14.3 Cách làm
1. Tạo phiếu loại `Trả NCC`.
2. Chọn kho.
3. Chọn nhà cung cấp.
4. Thêm vật tư và số lượng trả.
5. Lưu phiếu.
6. Đi tiếp theo luồng xuất kho nếu hệ thống yêu cầu lấy hàng/chốt xuất.

---

## 15. Phiếu khách trả

### 15.1 Route
- `/Vouchers/Create?type=4`

### 15.2 Dùng khi nào
- khách hàng trả lại hàng.

### 15.3 Cách làm
1. Tạo phiếu loại `Khách trả`.
2. Chọn kho nhận lại.
3. Chọn đối tác là khách hàng.
4. Nhập thông tin chứng từ tham chiếu.
5. Thêm vật tư trả về.
6. Nhập lô/HSD/serial nếu cần.
7. Lưu và duyệt theo luồng nhập.

---

## 16. Phiếu điều chỉnh

### 16.1 Route
- `/Vouchers/Create?type=5`

### 16.2 Dùng khi nào
- điều chỉnh tăng/giảm tồn,
- xử lý chênh lệch sau kiểm kê,
- điều chỉnh số lượng thực tế so với hệ thống.

### 16.3 Cách làm
1. Tạo phiếu loại `Điều chỉnh`.
2. Chọn kho.
3. Chọn vật tư.
4. Nhập số lượng điều chỉnh theo quy định nghiệp vụ.
5. Chọn vị trí/lô liên quan nếu cần.
6. Nhập lý do điều chỉnh.
7. Lưu.
8. Duyệt và hoàn tất theo quyền.

### 16.4 Điều chỉnh tự động từ snapshot
Route:
- `/Vouchers/CreateAdjustmentFromSnapshot`

Dùng khi:
- muốn tạo phiếu điều chỉnh tự động từ chênh lệch chốt tồn.

---

## 17. Phiếu chuyển kho

### 17.1 Route
- `/Vouchers/Create?type=6`

### 17.2 Dùng khi nào
- chuyển hàng giữa 2 kho,
- chuyển vị trí logic giữa nguồn và đích.

### 17.3 Các bước
1. Tạo phiếu loại `Chuyển kho`.
2. Chọn kho nguồn.
3. Chọn kho đích.
4. Thêm vật tư.
5. Nhập số lượng.
6. Nếu có quy đổi đơn vị, kiểm tra số lượng thực chuyển ra đơn vị tồn.
7. Lưu.
8. Duyệt theo quyền.

### 17.4 Ý nghĩa nghiệp vụ
- tổng tồn toàn công ty có thể không đổi,
- chỉ thay đổi vị trí/kho quản lý.

---

## 18. Phiếu nhập thành phẩm

### 18.1 Route
- `/Vouchers/Create?type=7`

### 18.2 Dùng khi nào
- xưởng sản xuất hoàn thành hàng và nhập lại kho thành phẩm.

### 18.3 Cách làm
1. Tạo phiếu loại `Nhập thành phẩm`.
2. Chọn kho nhận.
3. Nhập vật tư thành phẩm.
4. Nhập số lượng.
5. Nhập lô/HSD nếu có.
6. Lưu, duyệt, nhận hàng, hoàn tất như luồng nhập.

---

## 19. Phiếu xuất sản xuất

### 19.1 Route
- `/Vouchers/Create?type=8`

### 19.2 Dùng khi nào
- xuất nguyên vật liệu ra sản xuất.

### 19.3 Cách làm
1. Tạo phiếu loại `Xuất sản xuất`.
2. Chọn kho xuất.
3. Thêm nguyên vật liệu.
4. Nhập số lượng.
5. Nhập diễn giải như lệnh cấp phát cho sản xuất.
6. Lưu.
7. Xác nhận pick, lấy hàng, chốt xuất như outbound.

---

## 20. Danh sách phiếu và tra cứu phiếu

### 20.1 Route
- `/Vouchers`

### 20.2 Dùng để
- xem toàn bộ phiếu,
- lọc theo loại,
- lọc theo thời gian,
- tìm theo mã phiếu, mã chứng từ, nội dung liên quan.

### 20.3 Cách dùng
1. Mở `Tra cứu phiếu`.
2. Chọn loại phiếu nếu muốn lọc.
3. Chọn ngày từ - đến.
4. Gõ từ khóa.
5. Mở chi tiết phiếu cần xem.

---

## 21. Chi tiết phiếu - nơi xử lý nghiệp vụ tiếp theo

### 21.1 Route
- `/Vouchers/Details/{id}`

### 21.2 Dùng để
- xem toàn bộ thông tin phiếu,
- thao tác các bước tiếp theo của phiếu,
- in phiếu,
- duyệt hoặc chốt nghiệp vụ.

### 21.3 Những gì cần nhìn trên màn này
- loại phiếu,
- kho,
- đối tác,
- trạng thái,
- các dòng vật tư,
- người tạo,
- thời điểm tạo,
- các nút xử lý tiếp theo.

### 21.4 Nút xử lý theo loại phiếu
Với phiếu nhập:
- trình duyệt,
- duyệt phiếu,
- từ chối,
- xác nhận nhận hàng,
- tăng tồn.

Với phiếu xuất:
- xác nhận tạo nhiệm vụ lấy hàng,
- chốt phần đã lấy,
- chốt và hủy phần còn lại,
- đóng gói,
- giao hàng.

### 21.5 In phiếu
Thực hiện tại chi tiết phiếu:
1. Mở chi tiết phiếu.
2. Nhấn `In Phiếu`.
3. Kiểm tra bản in.

---

## 22. Nghiệp vụ nhập kho theo màn vận hành

### 22.1 Tiếp nhận hàng
Route:
- `/Operations/Receiving`

Dùng để:
- theo dõi xe đến,
- theo dõi lịch dock,
- xem phiếu nhập nào đang ở bước tiếp nhận,
- kiểm tra mức sẵn sàng số sê-ri.

Các bước:
1. Vào `Nhập kho -> Tiếp nhận hàng`.
2. Lọc theo kho.
3. Lọc theo trạng thái nhập.
4. Tìm theo mã phiếu, ASN, dock, xe.
5. Xem danh sách phiếu nhập.
6. Nếu vật tư có số sê-ri, bấm `Nhận số sê-ri`.
7. Bấm `Mở phiếu` để xử lý tiếp.

### 22.2 Quét nhận hàng bằng thiết bị cầm tay
Route:
- `/Operations/RfReceiving`

Dùng để:
- nhận hàng bằng thiết bị quét hoặc thao tác nhanh như bằng thiết bị cầm tay.

Cách làm:
1. Mở màn nhận hàng bằng thiết bị cầm tay.
2. Chọn dock hoặc điều kiện nhận hàng nếu màn yêu cầu.
3. Quét mã vật tư hoặc nhập tay.
4. Nhập số lượng.
5. Xác nhận.

### 22.3 Nhận số sê-ri
Route:
- `/Operations/SerialReceiving/{id}`

Dùng khi:
- phiếu nhập có vật tư quản lý số sê-ri.

Các bước:
1. Từ màn tiếp nhận hàng hoặc chi tiết phiếu, mở `Nhận số sê-ri`.
2. Chọn dòng vật tư cần nhập số sê-ri.
3. Dán hoặc nhập danh sách số sê-ri vào ô input.
4. Lưu đăng ký số sê-ri.
5. Kiểm tra số lượng số sê-ri đã đủ chưa.

Thao tác liên quan:
- `RegisterSerials`

---

## 23. Nghiệp vụ xuất kho theo màn vận hành

### 23.1 Đợt lấy hàng
Route:
- `/Operations/Waves`

Dùng để:
- gom nhiều phiếu xuất lại thành đợt lấy hàng.

Các bước:
1. Vào `Xuất kho -> Đợt lấy hàng`.
2. Lọc theo kho nếu cần.
3. Xem các wave đã tạo.
4. Mở wave để theo dõi tiến độ.

### 23.2 Nhiệm vụ lấy hàng
Route:
- `/Operations/PickTasks`

Dùng để:
- nhân viên xem việc phải lấy gì, ở đâu, bao nhiêu.

Các bước:
1. Vào `Xuất kho -> Nhiệm vụ lấy hàng`.
2. Lọc theo wave hoặc trạng thái.
3. Chọn task.
4. Nếu quản lý, có thể gán hoặc đổi người làm.
5. Nhân viên thực hiện lấy hàng theo task.

Actions liên quan:
- `AssignTask`
- `ReassignTask`

### 23.3 Quét lấy hàng bằng thiết bị cầm tay
Route:
- `/Operations/RfPicking`

Dùng để:
- xác nhận lấy hàng nhanh bằng luồng bằng thiết bị cầm tay.

Các bước:
1. Mở màn lấy hàng bằng thiết bị cầm tay.
2. Chọn hoặc nhận task được giao.
3. Quét mã vật tư, mã lô, số sê-ri hoặc mã liên quan theo màn hình.
4. Nhập số lượng thực lấy được.
5. Xác nhận hoàn tất.

### 23.4 Xác nhận pick trực tiếp trên voucher
Thao tác:
- `ConfirmPickTask`

Dùng để:
- chốt từng tác vụ lấy trên phiếu.

### 23.5 Chốt outbound
Thao tác:
- `PostReservedOutbound`

Có 2 cách:
1. `Chốt phần đã lấy`
2. `Chốt & hủy phần còn lại`

Ý nghĩa:
- cho phép xuất đủ hoặc xuất một phần.

### 23.6 Đóng gói
Thao tác:
- `ConfirmPacking`

Dùng để:
- nhập số kiện,
- loại bao gói,
- mã kiện,
- mã LPN,
- ghi chú đóng gói.

Các bước:
1. Mở chi tiết phiếu xuất đã posted.
2. Nhấn `Đóng gói`.
3. Nhập số kiện.
4. Nhập loại đóng gói.
5. Nhập mã gói hoặc mã kiện nếu có.
6. Lưu.

### 23.7 Giao hàng
Thao tác:
- `ConfirmShipping`

Dùng để:
- nhập tracking,
- manifest,
- ghi chú bàn giao.

Các bước:
1. Mở chi tiết phiếu đã đóng gói.
2. Nhấn `Giao hàng`.
3. Nhập thông tin giao nhận.
4. Xác nhận.

### 23.8 Gán dock giao hàng
Thao tác:
- `AssignDock`

Dùng cho:
- inbound hoặc outbound có quản lý dock.

---

## 24. Chuyển thẳng

### 24.1 Route
- `/Operations/Chuyển thẳngOpportunities`

### 24.2 Dùng khi nào
- hàng vừa nhập có thể chuyển thẳng ra phiếu xuất,
- không cần đưa vào khu lưu kho lâu dài.

### 24.3 Các bước
1. Vào `Xuất kho -> Chuyển thẳng (Chuyển thẳng)`.
2. Chọn kho nếu cần.
3. Xem danh sách cơ hội chuyển thẳng.
4. Kiểm tra vật tư, phiếu nhập, phiếu xuất, số lượng khả dụng.
5. Nhấn `Thực hiện`.

### 24.4 Thao tác thực hiện
- `ExecuteChuyển thẳng`

Kết quả:
- hệ thống chuyển logic xử lý từ inbound sang outbound nhanh hơn.

---

## 25. Bổ sung hàng (Bổ sung hàng)

### 25.1 Route
- `/Operations/Bổ sung hàng`

### 25.2 Dùng khi nào
- bổ sung hàng từ vị trí dự trữ sang vị trí picking,
- tránh ô picking bị hết hàng.

### 25.3 Các bước
1. Vào `Tồn kho -> Bổ sung hàng`.
2. Chọn kho.
3. Xem danh sách đề xuất bổ sung.
4. Kiểm tra nguồn, đích, số lượng đề xuất.
5. Nhấn thực hiện bổ sung.

### 25.4 Thao tác thực hiện
- `ExecuteBổ sung hàng`

---

## 26. Tối ưu vị trí (Tối ưu vị trí)

### 26.1 Route
- `/Operations/Tối ưu vị trí`

### 26.2 Dùng khi nào
- muốn đề xuất vị trí tốt hơn cho vật tư,
- giảm quãng đường đi lại,
- gom hàng hợp lý hơn.

### 26.3 Các bước
1. Vào `Tồn kho -> Tối ưu vị trí`.
2. Chọn kho.
3. Tìm vật tư cần tối ưu.
4. Xem vị trí hiện tại và vị trí được đề xuất.
5. Nhấn áp dụng đề xuất nếu phù hợp.

### 26.4 Thao tác thực hiện
- `ApplyTối ưu vị trí`

---

## 27. Tra cứu mã kiện (LPN)

### 27.1 Route
- `/Operations/LpnLookup`

### 27.2 Dùng khi nào
- cần tìm một mã kiện thuộc vật tư nào,
- kiện đang nằm ở đâu,
- kiện có còn active không.

### 27.3 Cách làm
1. Vào `Tồn kho -> Tra cứu mã kiện`.
2. Chọn kho nếu cần.
3. Nhập mã LPN hoặc thông tin liên quan.
4. Xem kết quả.

### 27.4 Quét LPN
Thao tác:
- `ScanLpn`

### 27.5 Màn nâng cao
Route:
- `/Operations/PackageLookup`

Dùng để:
- tra cứu đóng gói / package nếu hệ thống có dữ liệu package riêng.

---

## 28. Tra cứu số sê-ri

### 28.1 Route
- `/Operations/SerialLookup`

### 28.2 Dùng khi nào
- cần biết số sê-ri thuộc vật tư nào,
- số sê-ri đang active hay đã consumed,
- số sê-ri đi qua phiếu nào.

### 28.3 Cách làm
1. Vào `Tồn kho -> Tra cứu số sê-ri`.
2. Lọc theo kho nếu cần.
3. Gõ số sê-ri hoặc mã liên quan.
4. Xem kết quả.

### 28.4 Quét số sê-ri
Thao tác:
- `ScanSerial`

---

## 29. Xem tồn kho

### 29.1 Route
- `/Reports/Inventory`

### 29.2 Dùng để
- xem tồn tổng,
- xem theo kho,
- xem theo danh mục.

### 29.3 Các bước
1. Vào `Tồn kho -> Xem tồn kho`.
2. Chọn kho.
3. Chọn danh mục nếu cần.
4. Gõ mã hoặc tên vật tư để lọc nhanh.
5. Xem số lượng tồn.

### 29.4 Xuất bảng tính tồn kho
Thao tác:
- `ExportInventory`

---

## 30. Lịch sử nhập xuất

### 30.1 Route
- `/Reports/StockMovement`

### 30.2 Dùng để
- xem thẻ kho,
- xem lịch sử biến động của vật tư.

### 30.3 Các bước
1. Chọn vật tư nếu muốn xem 1 mã.
2. Chọn kho.
3. Chọn khoảng thời gian.
4. Bấm tìm.
5. Xem các lần nhập, xuất, điều chỉnh.

### 30.4 Xuất bảng tính lịch sử
Thao tác:
- `ExportStockMovement`

---

## 31. Kiểm kê

### 31.1 Route
- `/Reports/StockCount`

### 31.2 Dùng khi nào
- kiểm đếm thực tế so với hệ thống,
- phát hiện chênh lệch tồn.

### 31.3 Luồng chính
1. Mở màn kiểm kê.
2. Chọn kho và ngày kiểm kê.
3. Tải dữ liệu hiện tại.
4. Nhập số lượng thực đếm được.
5. Lưu nháp.
6. Trình duyệt hoặc phê duyệt.

### 31.4 Actions
- `StockCountSaveDraft`
- `StockCountApproveDraft`
- `StockCountUnlockApproved`

### 31.5 Cách làm chi tiết
1. Vào `Báo cáo -> Kiểm kê`.
2. Chọn kho.
3. Chọn ngày kiểm kê.
4. Hệ thống tải danh sách vật tư cần kiểm.
5. Nhập số lượng thực tế.
6. Lưu nháp.
7. Khi xong, người có quyền phê duyệt.

---

## 32. Chốt tồn kho (Snapshot)

### 32.1 Route
- `/Reports/StockSnapshot`

### 32.2 Dùng để
- chốt số liệu tồn theo ngày,
- làm mốc đối chiếu,
- sinh điều chỉnh chênh lệch.

### 32.3 Các bước
1. Vào `Hệ thống -> Chốt tồn`.
2. Chọn kho.
3. Chọn ngày snapshot.
4. Bấm xem dữ liệu.
5. Nếu cần, bấm `Chốt tồn (Generate)`.
6. Nếu có chênh lệch, bấm `Điều chỉnh chênh lệch`.

### 32.4 Actions
- `GenerateStockSnapshot`
- `QuickAdjustFromSnapshot`
- `ExportStockSnapshot`

---

## 33. Khóa kỳ kế toán theo kho

### 33.1 Route
- `/Reports/PeriodLocks`

### 33.2 Dùng khi nào
- đã chốt số liệu đến một ngày,
- muốn chặn tạo, duyệt hoặc hủy phiếu của giai đoạn đã khóa.

### 33.3 Các bước
1. Vào `Hệ thống -> Khóa kỳ`.
2. Chọn kho.
3. Chọn ngày khóa.
4. Nhập lý do.
5. Nhấn `Khóa / Cập Nhật`.

### 33.4 Mở khóa
1. Mở danh sách khóa kỳ.
2. Chọn bản ghi cần mở.
3. Nhấn `Mở khóa`.

### 33.5 Actions
- `SetPeriodLock`
- `ClearPeriodLock`

---

## 34. Báo cáo vận hành và phân tích

### 34.1 Chỉ số vận hành
Route:
- `/Reports/OpsKpi`

Dùng để:
- xem chỉ số tổng quan kho.

### 34.2 Top nhập/xuất
Route:
- `/Reports/TopItems`

Dùng để:
- thống kê mặt hàng nhập nhiều nhất hoặc xuất nhiều nhất.

Có thể export:
- `ExportTopItems`

### 34.3 Hàng sắp hết hạn
Route:
- `/Reports/ExpiryReport`

Dùng để:
- theo dõi hàng gần hết hạn,
- ưu tiên xử lý theo FEFO.

### 34.4 Hàng chậm luân chuyển
Route:
- `/Reports/SlowMovingReport`

Dùng để:
- tìm vật tư ít biến động trong nhiều ngày.

### 34.5 Phân loại ABC
Route:
- `/Reports/AbcAnalysis`

Dùng để:
- phân nhóm vật tư theo mức độ quan trọng/biến động.

---

## 35. Cảnh báo

### 35.1 Route
- `/Reports/Alerts`

### 35.2 Dùng để
- xem cảnh báo tồn thấp,
- tồn cao,
- sắp hết hạn.

### 35.3 Các bước
1. Vào `Hệ thống -> Cảnh báo`.
2. Lọc theo loại cảnh báo nếu cần.
3. Chọn chỉ xem cảnh báo chưa xử lý nếu muốn.
4. Mở cảnh báo liên quan.
5. Có thể refresh cảnh báo hạn dùng.
6. Có thể đánh dấu xử lý xong.

### 35.4 Actions
- `RefreshExpiryAlerts`
- `ResolveAlert`

---

## 36. Nhật ký hệ thống

### 36.1 Route
- `/Reports/AuditTrail`

### 36.2 Dùng để
- biết ai đã tạo, sửa, xóa, duyệt dữ liệu.

### 36.3 Cách dùng
1. Mở `Nhật ký`.
2. Lọc theo bảng dữ liệu.
3. Lọc theo người thay đổi.
4. Lọc theo khoảng ngày.
5. Xem chi tiết log.

---

## 37. Bất thường vận hành

### 37.1 Route
- `/Operations/ExceptionCenter`

### 37.2 Dùng để
- gom các case bất thường trong vận hành,
- giao người xử lý,
- theo dõi trạng thái xử lý.

### 37.3 Các bước
1. Vào `Báo cáo -> Bất thường`.
2. Lọc theo kho, loại, mức độ, từ khóa.
3. Mở case cần xử lý.
4. Có thể:
   - acknowledge,
   - assign,
   - resolve.

### 37.4 Actions
- `AcknowledgeException`
- `AssignException`
- `ResolveException`

---

## 38. Năng suất lao động

### 38.1 Route
- `/Operations/LaborProductivity`

### 38.2 Dùng để
- xem năng suất theo nhân sự,
- xem hiệu suất vận hành trong số ngày gần đây.

### 38.3 Các bước
1. Vào `Tồn kho -> Năng suất lao động`.
2. Chọn kho.
3. Chọn số ngày muốn xem.
4. Xem thống kê.

---

## 39. Người dùng

### 39.1 Route
- `/Users`

### 39.2 Dùng để
- tạo tài khoản,
- reset mật khẩu,
- xóa user,
- gán vai trò,
- gán kho quản lý nếu có.

### 39.3 Tạo user
Các bước:
1. Vào `Hệ thống -> Người dùng`.
2. Nhấn tạo mới.
3. Nhập tên đăng nhập, họ tên, thư điện tử, mật khẩu.
4. Chọn role.
5. Chọn kho nếu cần.
6. Lưu.

### 39.4 Reset password
1. Mở danh sách user.
2. Chọn user.
3. Nhập mật khẩu mới.
4. Xác nhận.

### 39.5 Xóa user
1. Chọn user.
2. Nhấn xóa.
3. Xác nhận.

---

## 40. Các chức năng hệ thống nguy hiểm

Controller:
- `SystemController`

Chỉ dành cho:
- Admin,
- có policy danger ops,
- thường dùng trong dev hoặc môi trường được cho phép.

### 40.1 Seed data
Thao tác:
- `SeedData`

Dùng để:
- nạp dữ liệu mẫu.

### 40.2 Gộp vị trí theo tầng
Thao tác:
- `MergeLocationsPerLevel`

Dùng để:
- chuẩn hóa dữ liệu vị trí theo logic nội bộ.

### 40.3 Reset database
Thao tác:
- `ResetDatabase`

Rất nguy hiểm:
- chỉ dùng khi thật sự cần,
- không dùng bừa trong bài demo chính.

---

## 41. Tìm kiếm nhanh toàn hệ thống

Trên giao diện có quick search:
- nhấn `Ctrl + K`

Dùng để:
- tìm mã phiếu,
- tìm vật tư,
- tìm mã vạch,
- nhảy nhanh đến các màn tra cứu.

Các bước:
1. Nhấn `Ctrl + K`.
2. Gõ từ khóa.
3. Chọn kết quả phù hợp.

---

## 42. Các tính năng import và AI

### 42.1 Quét hóa đơn AI
Thao tác:
- `AnalyzeReceipt`

Dùng để:
- tải ảnh hóa đơn,
- AI nhận diện dòng vật tư,
- map vào phiếu.

Cách làm:
1. Ở màn tạo phiếu, chọn `Quét Hóa Đơn AI`.
2. Chọn ảnh hóa đơn.
3. Chờ AI xử lý.
4. Kiểm tra các dòng được sinh ra.
5. Chỉnh lại nếu cần.

### 42.2 Import Excel
Actions:
- `DownloadImportTemplate`
- `DownloadDemoImport100`
- `ImportLinesExcel`

Cách làm:
1. Tải file mẫu import.
2. Điền dữ liệu theo đúng cột yêu cầu.
3. Tại màn tạo phiếu, chọn `Nhập Excel`.
4. Upload file.
5. Hệ thống thêm dòng vật tư vào phiếu.
6. Kiểm tra lại trước khi lưu.

---

## 43. Tính năng tự động gợi ý vị trí cất hàng

Actions:
- `SuggestPutawayLocation`
- `SuggestPutaway`

Dùng để:
- gợi ý vị trí cất hàng khi nhập kho,
- tự động đề xuất putaway.

Cách làm:
1. Tại màn tạo phiếu nhập, thêm vật tư.
2. Nhấn `Auto Putaway` nếu màn có nút này.
3. Hệ thống tính vị trí tối ưu.
4. Kiểm tra đề xuất.
5. Lưu phiếu.

---

## 44. Luồng demo toàn hệ thống nên đi theo thứ tự nào

Nếu muốn trình diễn full hệ thống mà không rối, nên đi theo thứ tự:

1. Đăng nhập.
2. Mở `Cấu hình kho` để giới thiệu kho, khu, vị trí.
3. Mở `Đối tác`.
4. Mở `Danh mục vật tư`.
5. Mở `Đơn vị tính`.
6. Mở `Sản phẩm / vật tư` và tạo 1 vật tư.
7. Tạo 1 phiếu nhập.
8. Duyệt và nhận hàng.
9. Vào `Xem tồn kho` để kiểm tra tồn đã tăng.
10. Tạo 1 phiếu xuất.
11. Tạo nhiệm vụ lấy hàng.
12. Vào `Nhiệm vụ lấy hàng` hoặc `Quét lấy hàng`.
13. Chốt outbound.
14. Đóng gói.
15. Giao hàng.
16. Mở `Lịch sử nhập xuất`.
17. Mở `Tra cứu số sê-ri` hoặc `Tra cứu mã kiện`.
18. Mở `Kiểm kê`.
19. Mở `Chốt tồn`.
20. Mở `Khóa kỳ`.
21. Mở `Cảnh báo`.
22. Mở `Nhật ký`.

---

## 45. Giải thích ngắn gọn từng màn để trả lời khi bị hỏi bất chợt

### “Sản phẩm / vật tư là gì?”
- Là nơi khai báo master data của hàng hóa/vật tư.

### “Phiếu nhập là gì?”
- Là chứng từ điện tử ghi nhận hàng đi vào kho.

### “Phiếu xuất là gì?”
- Là chứng từ điện tử ghi nhận hàng đi ra kho.

### “Đợt lấy hàng là gì?”
- Là đợt gom nhiều yêu cầu lấy hàng thành một nhóm xử lý.

### “Pick task là gì?”
- Là nhiệm vụ lấy hàng cụ thể giao cho nhân viên.

### “nhận hàng bằng thiết bị cầm tay / lấy hàng bằng thiết bị cầm tay là gì?”
- Là màn thao tác nhanh theo kiểu thiết bị quét cầm tay.

### “Snapshot là gì?”
- Là ảnh chụp số liệu tồn tại một thời điểm.

### “Khóa kỳ là gì?”
- Là chặn sửa nghiệp vụ của giai đoạn đã chốt.

### “Chuyển thẳng là gì?”
- Là hàng vừa nhập có thể chuyển thẳng ra xuất mà không phải lưu kho làu.

### “Tối ưu vị trí là gì?”
- Là tối ưu vị trí đặt hàng trong kho.

### “Bổ sung hàng là gì?”
- Là bổ sung hàng từ ô dự trữ sang ô picking.

---

## 46. Các lỗi hay gặp và cách xử lý

### 46.1 Không lưu được vật tư
Kiểm tra:
- đã nhập mã chưa,
- đã nhập tên chưa,
- đã chọn đơn vị tính chưa.

### 46.2 Không lưu được phiếu
Kiểm tra:
- đã chọn kho chưa,
- đã có ít nhất 1 dòng vật tư chưa,
- số lượng có > 0 không,
- dòng nào thiếu vị trí/lô/HSD/serial không.

### 46.3 Không tìm thấy vật tư khi quét
Kiểm tra:
- mã vạch có đúng không,
- vật tư đã khai báo chưa,
- thử bằng mã vật tư.

### 46.4 Không chốt xuất được
Kiểm tra:
- pick task đã hoàn tất chưa,
- tồn có đủ không,
- số sê-ri đã đủ chưa,
- có dòng nào đang thiếu xác nhận không.

### 46.5 Không thấy nút duyệt
Nguyên nhân thường là:
- tài khoản hiện tại không có quyền,
- phiếu chưa ở đúng trạng thái để hiện nút.

---

## 47. Checklist dữ liệu tối thiểu để chạy demo full hệ thống

Nên chuẩn bị:
- 1 tài khoản Admin.
- 1 tài khoản Manager.
- 1 tài khoản Staff.
- 1 kho.
- 2 đến 3 khu.
- vài vị trí.
- 2 nhà cung cấp.
- 2 khách hàng.
- 3 đến 5 đơn vị tính.
- 3 đến 5 danh mục vật tư.
- 5 đến 10 vật tư mẫu.
- Ít nhất:
  - 1 vật tư thường,
  - 1 vật tư có lô,
  - 1 vật tư có HSD,
  - 1 vật tư có số sê-ri.

---

## 48. Kết luận

Muốn học nhanh hệ thống này, chỉ cần nhớ 5 ý:

1. Khai báo nền trước, rồi mới làm nghiệp vụ.
2. Vật tư là master data, không phải tồn kho.
3. Nhập kho và xuất kho đi bằng phiếu.
4. Tồn chỉ thay đổi sau đúng bước nghiệp vụ.
5. Mọi thứ đều có thể tra cứu lại bằng báo cáo, cảnh báo và nhật ký.

Nếu dùng tài liệu này để bảo vệ, bạn có thể nói:

> “Hệ thống của em không chỉ có nhập và xuất, mà còn bao trùm toàn bộ vòng đời vận hành kho: khai báo danh mục, tạo phiếu, duyệt, nhận hàng, lấy hàng, đóng gói, giao hàng, kiểm kê, chốt tồn, khóa kỳ, cảnh báo và truy vết.”


