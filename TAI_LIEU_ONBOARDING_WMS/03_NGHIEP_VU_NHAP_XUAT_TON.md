# 03 - Nghiệp Vụ Nhập, Xuất Và Tồn Kho

## Luồng Nhập Kho Chuẩn

1. Tạo phiếu nhập.
2. Duyệt phiếu nếu quy trình yêu cầu.
3. Nhân viên mở màn nhận hàng bằng điện thoại hoặc màn tiếp nhận.
4. Nhập số lượng thực nhận, vị trí, lô, hạn dùng, số sê-ri hoặc cân thực tế nếu vật tư yêu cầu.
5. Kiểm phẩm nếu cần.
6. Tạo mã kiện nếu hàng được quản lý theo pallet/thùng/tote.
7. Hoàn tất nhập, hệ thống tăng tồn, cập nhật mã kiện, số sê-ri và sổ giao dịch tồn kho.

## Các Chặn Quan Trọng Khi Nhập

- Vật tư theo lô bắt buộc có số lô.
- Vật tư theo hạn dùng bắt buộc có hạn dùng.
- Vật tư theo số sê-ri bắt buộc đủ số sê-ri, không trùng.
- Vật tư cân trọng lượng thực tế bắt buộc có cân hợp lệ nếu cấu hình yêu cầu.
- Số lượng tốt cần cất hàng phải có vị trí hợp lệ cùng kho.
- Hàng lỗi không được tính vào số lượng tốt.
- Chuyển thẳng không được ghi tăng tồn cất hàng lần hai.

## Luồng Xuất Kho Chuẩn

1. Tạo phiếu xuất.
2. Phát hành lấy hàng, hệ thống giữ chỗ tồn.
3. Tạo nhiệm vụ lấy hàng.
4. Nhân viên dùng điện thoại quét vị trí, vật tư, tote, số sê-ri hoặc mã kiện.
5. Nếu lấy thiếu, hệ thống ghi nhận và có thể phân bổ lại.
6. Ghi sổ xuất, hệ thống giảm tồn và ghi sổ giao dịch.
7. Đóng gói tạo kiện xuất.
8. Tạo vận đơn nếu quy tắc vận chuyển yêu cầu.
9. Giao hàng trực tiếp hoặc xếp kiện lên chuyến xe rồi cho xe rời kho.

## Các Chặn Quan Trọng Khi Xuất

- Không giữ chỗ hoặc lấy hàng khác chủ hàng.
- Không lấy sai vị trí nguồn.
- Không lấy sai số sê-ri đã giữ chỗ.
- Không đóng gói mã kiện sai kho, sai chủ hàng hoặc chứa vật tư ngoài phiếu.
- Không vượt số lượng đã xuất khi đóng gói.
- Không giao nếu thiếu kiện xuất.
- Không giao nếu vật tư yêu cầu cân thực tế nhưng kiện chưa có cân.
- Không giao trực tiếp nếu phiếu đang nằm trong chuyến xe còn hoạt động.
- Không tạo trùng nhật ký bàn giao khi gửi lại.

## Quản Lý Tồn Kho

Tồn kho được xem theo vật tư, kho, vị trí, lô, hạn dùng, trạng thái giữ hàng và chủ hàng. Người vận hành thường dùng màn:

- Xem tồn kho.
- Sơ đồ kho.
- Tra cứu mã kiện.
- Tra cứu số sê-ri.
- Sổ giao dịch tồn kho.
- Lịch sử nhập xuất.

## Ngoại Lệ Cần Theo Dõi

- Thiếu số sê-ri nhập kho.
- Thiếu cân trọng lượng thực tế.
- Thiếu vị trí cất hàng.
- Lệch số lượng nhận thực tế.
- Lấy thiếu chưa xử lý.
- Kiện xuất chưa quét lên chuyến.
- Chuyến xe rời kho nhưng còn kiện chưa xếp.
- Phiếu đã giao nhưng thiếu mã vận đơn hoặc bản kê.

