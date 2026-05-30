# Hướng Dẫn Thực Hành WMS Chi Tiết

Tài liệu này dùng để đào tạo người mới trong môi trường test/staging. Mỗi bài có mục đích, điều kiện cần có trước khi làm, các bước thao tác chi tiết, lỗi thường gặp và kết quả đúng sau khi hoàn tất. Người dùng không được dùng dữ liệu production thật khi chưa có quyền và chưa có người phụ trách ca xác nhận. Ảnh sản phẩm chỉ là dữ liệu hỗ trợ nhận diện, còn mã SKU, barcode, lô, vị trí, quyền và trạng thái workflow mới là thông tin kiểm soát nghiệp vụ.

## Bài Thực Hành 1: Đăng nhập và kiểm tra vai trò

Mục đích: xác nhận tài khoản vào đúng workspace theo vai trò Admin, Manager, Staff hoặc Viewer. Điều kiện cần có trước khi làm: tài khoản đã được cấp role, MFA hoặc thiết bị tin cậy đã được cấu hình nếu hệ thống yêu cầu. Các bước thao tác chi tiết: mở màn hình đăng nhập, nhập email hoặc tên đăng nhập, nhập mật khẩu, xác nhận MFA, xem menu bên trái, thử mở một màn hình được phép và một màn hình không được phép. Kết quả đúng sau khi hoàn tất: người dùng chỉ thấy đúng chức năng theo quyền. Lỗi thường gặp: nhập sai mật khẩu, tài khoản bị khóa, thiết bị chưa tin cậy hoặc role chưa được gán.

## Bài Thực Hành 2: Tạo vật tư có ảnh nhận diện

Mục đích: tạo master data vật tư đủ mã, tên, đơn vị, barcode, ngưỡng tồn và ảnh nhận diện nhỏ. Điều kiện cần có trước khi làm: có quyền quản lý danh mục và file ảnh hợp lệ. Các bước thao tác chi tiết: vào danh sách vật tư, bấm thêm mới, nhập mã SKU không trùng, nhập tên rõ, chọn đơn vị cơ sở, nhập barcode, chọn nhóm, nhập ngưỡng tồn, upload ảnh JPG/PNG/WEBP, lưu và quay lại danh sách. Kết quả đúng sau khi hoàn tất: vật tư xuất hiện trong bảng, thumbnail hiển thị gọn, dropdown nhập/xuất có ảnh fallback nếu không có ảnh. Lỗi thường gặp: ảnh sai MIME, ảnh quá dung lượng, mã trùng hoặc thiếu đơn vị.

## Bài Thực Hành 3: Tạo đối tác

Mục đích: quản lý nhà cung cấp, khách hàng, chủ hàng 3PL hoặc đơn vị vận chuyển. Điều kiện cần có trước khi làm: có mã đối tác, tên pháp lý và loại đối tác. Các bước thao tác chi tiết: vào đối tác, tạo mới, nhập mã, tên, loại, địa chỉ, người liên hệ, trạng thái hoạt động và lưu. Kết quả đúng sau khi hoàn tất: đối tác có thể chọn trên phiếu phù hợp. Lỗi thường gặp: mã trùng, sai loại hoặc đối tác bị inactive nên không xuất hiện trong dropdown.

## Bài Thực Hành 4: Thiết lập kho, khu, vị trí

Mục đích: dựng cấu trúc kho để theo dõi tồn theo vị trí. Điều kiện cần có trước khi làm: có sơ đồ kho hoặc danh sách zone/bin. Các bước thao tác chi tiết: tạo kho, tạo khu, tạo vị trí, nhập loại vị trí, sức chứa, trạng thái, quy ước mã và vị trí mặc định nếu cần. Kết quả đúng sau khi hoàn tất: vị trí xuất hiện trong phiếu nhập, phiếu xuất, chuyển kho và gợi ý putaway. Lỗi thường gặp: vị trí chưa gắn khu, inactive, sức chứa sai hoặc mã không theo quy ước.

## Bài Thực Hành 5: Tạo phiếu nhập kho

Mục đích: ghi nhận hàng từ nhà cung cấp hoặc nội bộ vào kho. Điều kiện cần có trước khi làm: vật tư, đơn vị, đối tác và vị trí đã tồn tại. Các bước thao tác chi tiết: mở tạo phiếu nhập, chọn đối tác, nhập chứng từ gốc, chọn vật tư trong dropdown có thumbnail, nhập số lượng, đơn vị, lô, hạn dùng, vị trí cất và lưu. Kết quả đúng sau khi hoàn tất: phiếu có trạng thái hợp lệ và chờ duyệt hoặc hoàn tất theo quyền. Lỗi thường gặp: thiếu vị trí, thiếu lô/hạn dùng, sai đơn vị quy đổi hoặc số lượng không hợp lệ.

## Bài Thực Hành 6: Duyệt nhập kho và đồng bộ tồn

Mục đích: xác nhận hàng đã nhận và đưa tồn vào ItemLocation. Điều kiện cần có trước khi làm: phiếu nhập hợp lệ, có quyền duyệt và kỳ kho chưa khóa. Các bước thao tác chi tiết: mở phiếu, kiểm tra đối tác, dòng vật tư, ảnh nhận diện, số lượng, lô, hạn dùng, vị trí, sau đó duyệt hoặc hoàn tất. Kết quả đúng sau khi hoàn tất: ItemLocation tăng, ledger được ghi, CurrentStock cache được sync từ ItemLocation. Lỗi thường gặp: kỳ kho khóa, vị trí inactive hoặc không đủ dữ liệu tracking.

## Bài Thực Hành 7: Tạo phiếu xuất kho

Mục đích: xuất hàng theo đơn, nội bộ hoặc sản xuất. Điều kiện cần có trước khi làm: vật tư có tồn khả dụng trong vị trí hợp lệ. Các bước thao tác chi tiết: tạo phiếu xuất, chọn đối tác hoặc bộ phận, chọn vật tư, kiểm tra SKU và thumbnail, nhập số lượng, đơn vị, vị trí lấy hàng và lưu. Kết quả đúng sau khi hoàn tất: hệ thống tính quy đổi về đơn vị tồn và chuẩn bị task pick. Lỗi thường gặp: tồn không đủ, hàng đang hold, vị trí không chứa vật tư hoặc thiếu quyền xuất.

## Bài Thực Hành 8: Pick theo lô và hạn dùng

Mục đích: thực hiện FEFO/FIFO với hàng có hạn dùng hoặc lot tracking. Điều kiện cần có trước khi làm: có nhiều lô trong kho và rule pick đã bật. Các bước thao tác chi tiết: mở tác vụ pick, xem lô được gợi ý, quét barcode, xác nhận số lượng thực lấy, ghi nhận short pick nếu thiếu. Kết quả đúng sau khi hoàn tất: trừ đúng ItemLocation/lô và có ledger. Lỗi thường gặp: quét sai lô, lô hết hạn, lô bị hold hoặc nhân viên tự đổi lô không ghi lý do.

## Bài Thực Hành 9: Quét mã bằng điện thoại

Mục đích: dùng camera hoặc thiết bị nhập mã để thêm vật tư nhanh. Điều kiện cần có trước khi làm: trình duyệt cho phép camera hoặc ô quét đang focus. Các bước thao tác chi tiết: mở phiếu, đặt focus ô quét, quét barcode, kiểm tra dòng vừa thêm, nếu quét trùng thì kiểm tra số lượng tự tăng. Kết quả đúng sau khi hoàn tất: đúng SKU, đúng số lượng và không tạo dòng trùng ngoài ý muốn. Lỗi thường gặp: camera bị chặn, barcode chưa gắn vật tư, mạng yếu hoặc người dùng quét quá nhanh không kiểm tra lại.

## Bài Thực Hành 10: Hàng đợi quét khi mạng yếu

Mục đích: xác nhận retry và idempotency khi thiết bị mất mạng. Điều kiện cần có trước khi làm: PWA/offline queue bật trong staging. Các bước thao tác chi tiết: tạo tình huống mạng yếu, quét nhận/pick/chuyển, xem hàng đợi, bật mạng lại, đồng bộ và kiểm tra kết quả. Kết quả đúng sau khi hoàn tất: retry không tạo trùng tồn, trùng task hoặc trùng package. Lỗi thường gặp: deadletter chưa xử lý, conflict trạng thái hoặc tắt tab trước khi đồng bộ.

## Bài Thực Hành 11: Kiểm kê mù

Mục đích: đếm tồn thực tế mà nhân viên không thấy số lượng lý thuyết. Điều kiện cần có trước khi làm: có kế hoạch kiểm kê và vị trí cần đếm. Các bước thao tác chi tiết: tạo phiếu kiểm kê, phân công người đếm, nhập số lượng, gửi duyệt, xem chênh lệch và điều chỉnh nếu có quyền. Kết quả đúng sau khi hoàn tất: chênh lệch có audit trail và tôn trọng kỳ kho. Lỗi thường gặp: đếm nhầm vị trí, thiếu lô, số âm hoặc điều chỉnh trong kỳ khóa.

## Bài Thực Hành 12: Chuyển kho hoặc chuyển vị trí

Mục đích: di chuyển tồn từ nguồn sang đích. Điều kiện cần có trước khi làm: nguồn có tồn khả dụng và đích hoạt động. Các bước thao tác chi tiết: tạo phiếu chuyển, chọn vật tư, nguồn, đích, số lượng, kiểm tra lô/vị trí và xác nhận. Kết quả đúng sau khi hoàn tất: nguồn giảm, đích tăng, tổng tồn không lệch. Lỗi thường gặp: nguồn không đủ, đích không tương thích hoặc chuyển vào vị trí bị khóa.

## Bài Thực Hành 13: Putaway theo gợi ý vị trí

Mục đích: dùng gợi ý cất hàng sau khi nhận. Điều kiện cần có trước khi làm: vị trí có sức chứa và rule/default location đã cấu hình. Các bước thao tác chi tiết: chọn vật tư, xem vị trí đề xuất, kiểm tra sức chứa, chọn vị trí và xác nhận. Kết quả đúng sau khi hoàn tất: vị trí hợp lệ, không xếp lẫn sai vật tư. Lỗi thường gặp: default location inactive, vị trí đầy hoặc rule không có dữ liệu.

## Bài Thực Hành 14: Replenishment bổ sung hàng

Mục đích: bổ sung từ bulk sang pick face. Điều kiện cần có trước khi làm: có vùng nguồn, vùng pick, reorder point và tồn khả dụng. Các bước thao tác chi tiết: chạy gợi ý replenishment, xem nhu cầu wave/đơn hàng, tạo task, xác nhận di chuyển. Kết quả đúng sau khi hoàn tất: pick face đủ tồn để pick. Lỗi thường gặp: nguồn hold, task trùng hoặc không có đích hợp lệ.

## Bài Thực Hành 15: Wave và waveless picking

Mục đích: so sánh xử lý đơn theo wave và order streaming. Điều kiện cần có trước khi làm: nhiều đơn xuất và tồn khả dụng. Các bước thao tác chi tiết: tạo wave, xem batch pick, chạy waveless cho đơn nhỏ, xác nhận pick và pack. Kết quả đúng sau khi hoàn tất: đơn được ưu tiên đúng SLA và không giữ tồn trùng. Lỗi thường gặp: rule chọn sai đơn, tồn thiếu hoặc ưu tiên sai.

## Bài Thực Hành 16: Đóng gói và bàn giao vận chuyển

Mục đích: đóng package, in nhãn và tạo chứng từ giao hàng. Điều kiện cần có trước khi làm: pick đã hoàn tất, package type và carrier đã cấu hình. Các bước thao tác chi tiết: mở pack station, tạo package, nhập cân nặng nếu bắt buộc, in nhãn, tạo manifest và bàn giao. Kết quả đúng sau khi hoàn tất: package có mã, tracking hoặc manifest. Lỗi thường gặp: thiếu cân thực tế, thiếu carrier hoặc quét package trùng.

## Bài Thực Hành 17: Yard và dock appointment

Mục đích: điều phối xe vào bãi, cửa dock và thời gian bốc dỡ. Điều kiện cần có trước khi làm: có dock door, lịch hẹn và yard spot. Các bước thao tác chi tiết: tạo appointment, gate-in, gán spot, chuyển dock, unload/load và gate-out. Kết quả đúng sau khi hoàn tất: trạng thái xe rõ ràng, thời gian lưu bãi được ghi. Lỗi thường gặp: trùng dock, xe đang active visit hoặc quên gate-out.

## Bài Thực Hành 18: 3PL billing

Mục đích: tính phí dịch vụ kho cho chủ hàng. Điều kiện cần có trước khi làm: owner, contract, rate và dữ liệu vận hành. Các bước thao tác chi tiết: cấu hình rate, chạy billing run, kiểm tra dòng phí, lock invoice và xử lý dispute. Kết quả đúng sau khi hoàn tất: invoice có phí đúng contract. Lỗi thường gặp: thiếu owner scope, rate hết hiệu lực hoặc dữ liệu chưa đủ.

## Bài Thực Hành 19: VAS work order

Mục đích: xử lý dịch vụ gia tăng như dán nhãn, đóng gói lại, kitting hoặc kiểm phẩm. Điều kiện cần có trước khi làm: có lệnh VAS, định mức nhân công và tồn khả dụng. Các bước thao tác chi tiết: tạo lệnh, reserve tồn, ghi nhận nhân công, QC và hoàn tất. Kết quả đúng sau khi hoàn tất: tồn và chi phí được ghi nhận. Lỗi thường gặp: đơn giá âm, thiếu tồn, QC fail hoặc thiếu người duyệt.

## Bài Thực Hành 20: Kitting work order

Mục đích: tạo thành phẩm kit từ nhiều component. Điều kiện cần có trước khi làm: BOM rõ và component đủ tồn. Các bước thao tác chi tiết: tạo lệnh, reserve component, issue component, receive kit và kiểm tra tồn. Kết quả đúng sau khi hoàn tất: component giảm, kit tăng, ledger đầy đủ. Lỗi thường gặp: thiếu component, hold stock hoặc rollback không hoàn tất.

## Bài Thực Hành 21: MHE/WCS simulation

Mục đích: kiểm tra lệnh gửi thiết bị tự động và telemetry mô phỏng. Điều kiện cần có trước khi làm: simulator hoặc connector staging bật. Các bước thao tác chi tiết: tạo command, theo dõi trạng thái, giả lập lỗi, retry, override có lý do và đóng task. Kết quả đúng sau khi hoàn tất: command lifecycle rõ ràng. Lỗi thường gặp: connector inactive, thiếu endpoint hoặc override không có lý do.

## Bài Thực Hành 22: Báo cáo tồn kho và BI

Mục đích: đọc báo cáo tồn, cảnh báo hết hàng, hàng chậm luân chuyển và ABC. Điều kiện cần có trước khi làm: có quyền xem báo cáo và dữ liệu đã sync. Các bước thao tác chi tiết: mở báo cáo, lọc kho/chủ hàng, xem số liệu, xuất Excel nếu được phép và đối chiếu ItemLocation khi nghi ngờ. Kết quả đúng sau khi hoàn tất: số liệu đúng scope. Lỗi thường gặp: dùng cache chưa sync hoặc lọc sai scope.

## Bài Thực Hành 23: Đọc chứng từ bằng MinerU/API ngoài

Mục đích: upload chứng từ để gợi ý dòng phiếu. Điều kiện cần có trước khi làm: MinerU enabled trên service riêng hoặc API ngoài; shared hosting InterData không chạy MinerU chung. Các bước thao tác chi tiết: upload file, xem preview, kiểm tra confidence, chọn dòng đã khớp, tự chỉnh dòng cần review và lưu khi đã xác nhận. Kết quả đúng sau khi hoàn tất: OCR không tự post tồn kho. Lỗi thường gặp: service tắt, file quá lớn, timeout hoặc đọc sai.

## Bài Thực Hành 24: Quản trị bảo mật

Mục đích: kiểm tra quyền, thiết bị tin cậy, MFA và audit trail. Điều kiện cần có trước khi làm: Admin hoặc người được ủy quyền. Các bước thao tác chi tiết: xem user, role, permission, revoke trusted device, kiểm tra log đăng nhập và thử thao tác bị từ chối bằng role thấp hơn. Kết quả đúng sau khi hoàn tất: quyền bị chặn đúng và có audit. Lỗi thường gặp: cấp quá quyền, quên revoke thiết bị cũ hoặc chia sẻ tài khoản.

## Bài Thực Hành 25: Release readiness

Mục đích: chạy checklist trước khi đưa bản lên staging/production. Điều kiện cần có trước khi làm: code đã build, test, format và migration list. Các bước thao tác chi tiết: chạy build, test, format, migration list, visual, load, backup/restore, security checklist, appsettings override và rollback plan. Kết quả đúng sau khi hoàn tất: có artifact cho từng gate. Lỗi thường gặp: test xanh local nhưng thiếu staging auth, chưa rotate secret hoặc chưa có restore drill.

## Checklist Cuối Ca

- Kiểm tra tất cả phiếu đang xử lý có owner, kho, vị trí và trạng thái hợp lệ.
- Kiểm tra hàng đợi quét không còn lỗi deadletter chưa xử lý.
- Đối chiếu tồn bất thường giữa ItemLocation và CurrentStock cache.
- Xem cảnh báo hàng hết hạn, hàng sắp hết và vị trí vượt sức chứa.
- Đảm bảo mọi override có lý do, người duyệt và log.
- Xác nhận backup/restore, monitoring và incident note nếu có sự cố trong ca.

## Checklist Rà Soát Lỗi Thường Gặp

- Vật tư có tên giống nhau nhưng SKU khác: dùng barcode, mã SKU và thumbnail hỗ trợ nhận diện.
- Phiếu nhập thiếu lô/hạn dùng: quay lại dòng phiếu và bổ sung trước khi hoàn tất.
- Xuất không đủ tồn: kiểm tra hold, reservation, kho/chủ hàng và vị trí.
- Quét trùng: kiểm tra idempotency và số lượng tăng tự động.
- OCR/MinerU đọc sai: chỉ dùng preview, không tự động post tồn.
- Shared hosting không chạy được service nền: tắt MinerU tại host và trỏ sang VPS/API riêng khi có.

## Phụ Lục A: Kịch Bản Một Ca Vận Hành Mẫu

Ca vận hành mẫu bắt đầu bằng việc Manager kiểm tra dashboard tồn kho, danh sách phiếu đang chờ, trạng thái dock và hàng đợi quét. Staff nhận nhiệm vụ nhập kho phải mở đúng phiếu, kiểm tra đối tác, biển số xe, số chứng từ gốc, từng dòng vật tư, lô, hạn dùng, vị trí cất và số lượng thực nhận. Nếu vật tư có ảnh, ảnh chỉ dùng để nhận diện phụ trợ; khi có mâu thuẫn giữa ảnh và SKU/barcode thì phải dừng thao tác và hỏi quản lý, không được tự sửa master data ngoài quy trình.

Sau khi nhập kho, Staff chuyển sang putaway. Người thao tác phải kiểm tra vị trí được gợi ý có đúng khu, đúng sức chứa và không xếp lẫn vật tư không tương thích. Nếu vị trí đã có hàng cùng SKU, kiểm tra lô và hold status trước khi xác nhận. Nếu vị trí trống nhưng không phù hợp vật lý, ghi chú lý do override và chọn vị trí khác theo quyền được cấp. Mọi quyết định cuối cùng về tồn phải dựa trên ItemLocation, không dựa vào số tổng hiển thị nếu nghi ngờ chưa đồng bộ.

Ở luồng xuất kho, Staff kiểm tra đơn, SLA, requested delivery date, carrier requirement và tồn khả dụng. Khi pick theo lô/hạn dùng, người thao tác phải ưu tiên logic FEFO nếu hàng có hạn sử dụng. Nếu quét sai mã, hệ thống phải báo lỗi và không trừ tồn. Nếu thiếu hàng tại vị trí, dùng quy trình short pick hoặc reallocation thay vì tự điều chỉnh tồn. Khi đóng gói, package phải có mã rõ ràng, cân nặng nếu bắt buộc và chứng từ bàn giao nếu carrier yêu cầu.

Cuối ca, Manager đối chiếu các phiếu hoàn tất trong ngày với báo cáo transaction ledger, kiểm tra deadletter queue, kiểm tra cảnh báo tồn âm, cảnh báo hết hạn, cảnh báo vị trí vượt sức chứa và các override có lý do. Nếu phát hiện lệch tồn, không sửa trực tiếp CurrentStock; phải mở quy trình kiểm kê hoặc reconciliation để điều chỉnh từ nguồn dữ liệu vị trí/lô/package. Các lỗi tích hợp carrier, MHE hoặc MinerU phải được ghi thành incident note có thời điểm, người xử lý, ảnh chụp màn hình hoặc log liên quan.

## Phụ Lục B: Quy Tắc Ghi Chú Và Bàn Giao

Ghi chú nghiệp vụ cần ngắn, rõ, có bối cảnh. Một ghi chú tốt gồm: mã phiếu hoặc task, vật tư, vị trí, số lượng, nguyên nhân và hành động đã làm. Ví dụ: “PX-20260517-001, SKU VT-001, vị trí A-01-02 thiếu 3 cái so với task, đã tạo short pick và chờ Manager duyệt reallocation.” Không ghi chú chung chung như “lỗi hệ thống”, “không được”, “xử lý rồi” vì ca sau không thể kiểm tra.

Bàn giao ca phải có danh sách phiếu chưa hoàn tất, task đang chờ người khác, xe còn trong bãi, package đã đóng nhưng chưa ship, dòng OCR/MinerU cần review, dòng billing 3PL đang dispute và các cảnh báo an toàn. Nếu có thao tác thủ công ngoài quy trình, phải ghi rõ ai duyệt, duyệt lúc nào và vì sao không dùng luồng tự động. Khi bàn giao cho ca sau, chỉ bàn giao bằng màn hình hệ thống hoặc tài liệu vận hành đã lưu; không dùng tin nhắn riêng làm nguồn sự thật.

## Phụ Lục C: Quy Tắc Khi Lên Hosting

Với hosting ASP.NET shared, WMS chỉ nên chạy phần web app, MVC, API nội bộ và kết nối database theo cấu hình provider cho phép. Không giả định rằng host có thể chạy MinerU, Docker, Python service, worker nền hoặc mở port API riêng. Nếu cần đọc chứng từ bằng AI, triển khai MinerU trên VPS riêng hoặc dùng API ngoài, sau đó cấu hình WMS trỏ đến endpoint đó. Khi chưa có service riêng, đặt MinerU disabled trên production override để người dùng không tưởng rằng chức năng OCR đang sẵn sàng.

Không xóa connection string hoặc API key trong appsettings.json trong pass này theo quyết định vận hành của chủ hệ thống. Tuy nhiên khi đưa vào production thật, nên override bằng biến môi trường hoặc cấu hình Plesk/IIS, giới hạn quyền đọc file, bật HTTPS, cấu hình backup database và không gửi file cấu hình cho người ngoài. Nếu một secret từng được gửi qua kênh không kiểm soát, phải rotate trước khi dùng production.

## Phụ Lục D: Bảng Đối Chiếu Nghiệp Vụ Và Bằng Chứng

Mỗi thao tác trong WMS cần có bằng chứng đủ để người quản lý ca kiểm tra lại sau này. Với nhập kho, bằng chứng gồm phiếu nhập, chứng từ gốc, dòng vật tư, số lượng, đơn vị, lô, hạn dùng, vị trí cất, người thao tác, người duyệt và thời điểm hoàn tất. Với xuất kho, bằng chứng gồm đơn xuất, phân bổ tồn, task pick, vị trí lấy, lô/hạn dùng, package, cân nặng nếu bắt buộc, carrier, tracking, manifest và trạng thái ship. Với chuyển kho, bằng chứng gồm nguồn, đích, lý do chuyển, số lượng và ledger trước/sau.

Người mới cần hiểu rằng WMS không chỉ là phần mềm nhập số liệu. Đây là hệ thống kiểm soát dòng hàng thật trong kho. Một thao tác sai có thể làm sai tồn, sai báo cáo tài chính, giao nhầm khách, giữ nhầm lô hết hạn hoặc làm thất lạc hàng của chủ hàng 3PL. Vì vậy mọi màn hình đều phải được đọc theo thứ tự: đối tượng nghiệp vụ, quyền người dùng, scope kho/chủ hàng, trạng thái workflow, dữ liệu dòng hàng, log/audit và bằng chứng sau thao tác.

Khi gặp lỗi, không nên thử bấm lại nhiều lần nếu không hiểu trạng thái hiện tại. Trước tiên ghi lại mã phiếu/task/package, thông báo lỗi, thời điểm, người thao tác và ảnh chụp màn hình. Sau đó kiểm tra hàng đợi, audit trail, trạng thái phiếu và tồn vị trí. Nếu lỗi liên quan tích hợp bên ngoài như carrier, MHE hoặc MinerU, kiểm tra connector health trước khi kết luận lỗi nghiệp vụ. Nếu lỗi liên quan tồn kho, tuyệt đối không sửa cache tổng bằng tay; phải đi qua kiểm kê, reversal, cancellation hoặc reconciliation.

## Phụ Lục E: Quy Tắc Đào Tạo Người Mới

Người mới nên đi theo thứ tự từ đọc dữ liệu đến thao tác thật. Ngày đầu chỉ xem dashboard, danh mục, danh sách phiếu và báo cáo tồn. Ngày thứ hai thực hành tạo vật tư test, tạo phiếu nhập test và hoàn tất nhập với vị trí test. Ngày thứ ba thực hành xuất kho, pick, pack, ship và đối soát. Ngày thứ tư thực hành lỗi có kiểm soát: thiếu tồn, thiếu lô, sai barcode, mạng yếu, hàng đợi quét, thiếu quyền và kỳ kho khóa. Ngày thứ năm mới thực hành ca vận hành đầy đủ dưới giám sát.

Người hướng dẫn phải kiểm tra ba thói quen: luôn đọc SKU/barcode trước tên vật tư, luôn kiểm tra kho/chủ hàng trước khi thao tác và luôn đọc trạng thái workflow trước khi bấm nút. Thumbnail sản phẩm giúp giảm nhầm lẫn khi tên vật tư giống nhau, nhưng ảnh không bao giờ là điều kiện duy nhất để nhận hàng hoặc xuất hàng. Nếu ảnh sai, thiếu hoặc cũ, ghi task sửa master data; không tự suy luận rằng mã hàng cũng sai.

Cuối mỗi buổi, người mới phải tự giải thích được mình đã tạo dữ liệu gì, dữ liệu đó ảnh hưởng bảng nào, bằng chứng ở đâu, rollback bằng cách nào và ai có quyền duyệt. Nếu không trả lời được, chưa nên cho thao tác trên dữ liệu thật. Đây là cách giảm rủi ro vận hành trước khi hệ thống có đầy đủ visual regression, load artifact, staging evidence và production runbook hoàn chỉnh.

