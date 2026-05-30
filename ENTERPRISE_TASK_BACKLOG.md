# Danh sách tác vụ hệ thống quản lý kho cấp doanh nghiệp

Ngày 30/04/2026. Mục tiêu: ghi task sau khi rà soát hiện trạng, chưa implement tính năng.
Nguyên tắc ưu tiên: sửa lỗi đúng dữ liệu và nợ kỹ thuật trước, sau đó mới mở rộng quy trình Tier-1.

## Hiện Trạng Nhanh
- Đã có nền đợt lấy hàng, nhiệm vụ lấy hàng, giữ chỗ tồn kho, đợt lấy hàng nhiều đơn, lấy hàng bằng thiết bị cầm tay, trạng thái lấy thiếu và lấy hàng theo số sê-ri.
- Đã có `CrossDockOpportunities` và nhiệm vụ chuyển thẳng; luồng auto-detect/match/execute đã được bảo vệ bằng test route để tránh lỗi dịch nhầm action.
- Đã có dock appointment, DockDoorCapacity và báo cáo Dock-to-Stock, nhưng chưa có yard/gate/trailer lifecycle.
- Đã có gợi ý vị trí lưu trữ, phân tích tốc độ luân chuyển và ApplyTối ưu vị trí, nhưng chưa có nhiệm vụ di chuyển tự động.
- Đã có BOM và in label, có thể dùng làm nền cho VAS, nhưng chưa có work order kitting/co-packing riêng.
- Technical debt lớn nhất đã giảm: `OperationsController` và `VouchersController` đã có lớp service/query tách cho scope kho, slotting, exception center, phí bãi, rule phiếu và lưu chứng từ; phần còn lại tiếp tục tách theo workflow khi đụng module.
- Bộ kiểm thử hiện tại: 492/492 đạt (`dotnet test WMS.Tests\WMS.Tests.csproj --no-restore -v:minimal`) sau đợt xử lý backlog E1-E13 local-actionable ngày 14/05/2026.

## Residual Full Audit Backlog 2026-05-12

- [ ] Gắn bằng chứng staging thật cho `PRODUCTION_SECURITY_CHECKLIST.md` trước khi tick các mục thủ công.
- [ ] Gắn bằng chứng nghiệm thu vận hành thật cho `MODULE_ACCEPTANCE_CHECKLIST.md` trước khi tick các mục thủ công.
- [x] Tách lớp service/query cho core nóng `OperationsController` và `VouchersController` mà không đổi route public.
- [ ] Tiếp tục tách nhỏ các workflow legacy còn lại trong `OperationsController` và `VouchersController` khi có thay đổi liên quan.
- [ ] Lưu artifact Playwright/k6 theo từng release staging thay vì chỉ giữ scaffold trong repo.
- [ ] Xác nhận rotate secret, Data Protection key persistence và backup/restore drill trên môi trường hosting thật.

## Residual Full Audit Backlog 2026-05-13

- [ ] Externalize và rotate toàn bộ secret đang có giá trị cụ thể trong `appsettings.json`; mục này đang được giữ lại theo quyết định vận hành hiện tại.
- [ ] Khi có staging/user test, chạy `npm run visual:auth` và `npm run visual:test` để lưu artifact screenshot thật cho dashboard, tạo phiếu, duyệt phiếu, phiếu kho và sơ đồ tồn kho.

## Residual UI Audit Backlog 2026-05-14

- [ ] Khi có auth state visual, chạy lại Playwright cho collapsed sidebar mini rail + flyout ở desktop 100/110/125 và mobile drawer.
- [ ] Khi có auth state visual, chạy Playwright cho các màn dễ vỡ đã đưa vào spec: trang chính, phiếu kho, vận hành nâng cao, `OptimizationDashboard`, `AutomationDashboard`, `IntegrationDashboard`, `WorkflowProfiles`, `DockBoard`, `YardManagement`.
- [ ] Inline style tĩnh ở nhóm view ưu tiên đã được dọn; phần còn lại chủ yếu là style động trong JavaScript/SweetAlert/tiến độ và các view legacy ít rủi ro, xử lý tiếp khi đụng module liên quan.

## Residual E1-E13 Audit Backlog 2026-05-14

- [x] Sửa predictive stockout trong `Services/Enterprise1113Services.cs` để dùng tồn khả dụng theo `ItemLocation` + warehouse/owner scope thay vì đọc trực tiếp `Item.CurrentStock` aggregate.
- [ ] Tạo `WMS_AUTH_STATE` hoặc `tests/visual/.auth/wms-auth-state.json`, set `WMS_BASE_URL`, chạy `npm run visual:test` và lưu artifact screenshot cho các màn E1-E13 trọng điểm.
- [x] Việt hóa/dọn fallback enum và inline style còn lại trong `Views/Operations/QualityInspection.cshtml`.
- [ ] Dọn top inline-style legacy còn nhiều: `Views/Vouchers/Create.cshtml`, `Views/Reports/SpaceUtilization.cshtml`, `Views/Reports/DockToStock.cshtml`, `Views/Reports/AuditTrail.cshtml`.
- [x] Chuyển internal link trọng điểm trong `_Layout`, `Home/Index`, `QualityInspection` sang tag helper/route helper; giữ test chặn URL có `%20` hoặc non-ASCII.
- [x] Bỏ optional controller fallback `?? new ...Service(...)` trong `OperationsController` và `VouchersController` sau khi test factory inject đủ service.
- [x] Thêm startup warning `MINERU_LOOPBACK_PRODUCTION_WARNING` khi `MinerU.Enabled=true` ở non-development mà `MinerU:BaseUrl` vẫn trỏ loopback host/loopback IPv4.
- [ ] Gắn bằng chứng nghiệm thu staging thật cho `MODULE_ACCEPTANCE_CHECKLIST.md`, `PRODUCTION_SECURITY_CHECKLIST.md`, k6 load test, backup/restore drill và visual release artifact.

## P0 - Nền Tảng Phải Làm Trước

### ~~[HOÀN THÀNH] TÁC VỤ-P0-01 - Sửa Period Lock theo ngày thực thi~~
**Lý do:** Hiện IsLocked(voucher.VoucherDate, lockDate) chỉ chặn theo ngày lập phiếu. Giao dịch thực tế nên khóa theo ngày posting/completion nếu có.
**Phạm vi:**
- VouchersController.IsLocked: đổi signature để nhận transactionDate.
- Các luồng post/approve/cancel dùng ngày ưu tiên: PostedAt, CompletedAt, VietnamNow, fallback VoucherDate.
- Thêm test cho phiếu ngày cũ nhưng post sau ngày lock, và phiếu ngày mới nhưng completed trong kỳ bị lock.
**Tiêu chí nghiệm thu:**
- [x] Không thể ghi sổ/cập nhật tồn vào ngày đã khóa.
- [x] Các test period lock pass và không phá 317 test hiện có.

### ~~[HOÀN THÀNH] TÁC VỤ-P0-02 - Kiểm tra lại RecalculateReservedQtyAsync theo kho~~
**Lý do:** Hàm hiện tính theo ItemId + LocationId + Lot + Expiry. Nếu location id unique toàn hệ thống thì an toàn, nhưng task này cần test rõ để chặn bug cộng dồn sai kho.
**Phạm vi:**
- Thêm regression test có 2 kho, cùng item/lot/expiry, reservation riêng.
- Nếu phát hiện risk, thêm filter kho qua Location.Zone.KhoId.
- Giữ ReservedQty trên ItemLocation bằng tổng open reservation hợp lệ: ReservedQty - ConsumedQty - ReleasedQty.
**Tiêu chí nghiệm thu:**
- [x] Giữ chỗ kho A không làm đổi số lượng đã giữ của kho B.
- [x] Kiểm thử thể hiện rõ lỗi trước và đạt sau khi sửa nếu có lỗi.

### Giai đoạn 6: Ổn định và bảo trì
- [x] P6-01: Sửa lỗi ngôn ngữ giao diện (sửa lỗi mã hóa chữ, lỗi mã hóa kép và Việt hóa đầy đủ giao diện có dấu).
- [ ] P6-02: Triển khai ghi nhật ký đầy đủ (Seq / Serilog).
- [ ] P6-03: Add integration tests for all giao diện lập trình endpoints.

### ~~[HOÀN THÀNH] TÁC VỤ-P0-03 - Lập kế hoạch gỡ số tổng `CurrentStock`~~
**Lý do:** Item.CurrentStock vẫn là số tổng trên master item, dễ lệch khi nhiều kho. Repo đã có nhiều nơi tự SUM ItemLocation, nhưng vẫn còn đọc/ghi CurrentStock trực tiếp.
**Phạm vi:**
- Tạo truy vấn hoặc dịch vụ hỗ trợ InventoryBalanceService tính tồn theo ItemLocation.
- Chuyển màn hình report/dashboard/giao diện lập trình đọc tồn sang SUM theo kho.
- Giai đoạn đầu giữ field CurrentStock để tương thích ngược, nhưng không dùng là nguồn dữ liệu chuẩn.
- Sau khi ổn định mới tính migration để bỏ aggregate.
**Tiêu chí nghiệm thu:**
- [x] Bảng điều hành/giao diện lập trình/báo cáo/vật tư có số tồn đúng theo kho. (Đã fix HomeController, ItemsController Index/Details/Delete)
- [x] Kiểm thử bảo vệ: vật tư có tồn ở 2 kho, người dùng được giới hạn phạm vi kho chỉ thấy tồn kho của mình.

### ~~[HOÀN THÀNH] TÁC VỤ-P0-04 - Tách tầng dịch vụ cho outbound/inbound trước khi thêm tính năng lớn~~
**Lý do:** VouchersController.cs và OperationsController.cs đang quá lớn, thêm wave/yard/VAS trực tiếp vào sẽ khó test và dễ gây hồi quy.
**Phạm vi để tách theo đợt nhỏ:**
- IInventoryReservationService: FEFO allocation, reservation, recalc reserved.
- IOutboundExecutionService: create wave, confirm pick task, post outbound.
- IInboundExecutionService: approve inbound, putaway, số sê-ri receiving.
- IChuyển thẳngService: detect/match/execute chuyển thẳng.
- Controller chỉ giữ gắn dữ liệu yêu cầu HTTP, TempData/ViewModel, phân quyền truy cập.
**Tiêu chí nghiệm thu:**
- [x] Mỗi đợt tách có test hiện tại pass.
- [x] Không đổi route/UI khi refactor.

### ~~[HOÀN THÀNH] TÁC VỤ-P0-05 - Đơn vị công việc cho các quy trình ghi tồn~~
**Lý do:** Nhiều quy trình hiện gửi SaveChangesAsync() ở nhiều điểm trong controller/service. Cần gom transaction boundary rõ để tránh lưu nửa chừng và khó rollback.
**Phạm vi:**
- Tạo abstraction IUnitOfWork bao SaveChangesAsync, BeginTransactionAsync, CommitAsync, RollbackAsync.
- Áp dụng trước cho outbound posting, inbound approve, stock count adjustment, chuyển thẳng execute.
- Controller không tự mở transaction dài dòng; service quy trình quản lý transaction.
**Tiêu chí nghiệm thu:**
- [x] Mỗi quy trình chính chỉ có một transaction boundary rõ ràng.
- [x] Lỗi giữa chừng rollback sạch tồn kho, reservation, audit liên quan.


## P1 - Pick Execution Nâng Cao

### ~~[HOÀN THÀNH] TÁC VỤ-P1-01 - Lấy hàng theo lô gom thực dụng trên nền đợt lấy hàng hiện có~~
**Lý do:** Multi-order wave đã có, nhưng pick task vẫn nghiêng về từng voucher/detail. Lấy hàng theo lô gom nên gom cùng item/location/lot trên nhiều đơn thành một task lấy tổng.
**Phạm vi:**
- Thêm nhóm nhiệm vụ lấy hàng hoặc khóa gom nhóm trên `PickTask`.
- Khi tạo wave, gom allocation cùng ItemId + SourceLocationId + Lot + Expiry.
- Sau khi pick tổng, tách qty về từng voucher reservation.
**Tiêu chí nghiệm thu:**
- [x] 3 đơn cùng SKU/cùng vị trí sinh 1 batch pick task.
- [x] Post outbound từng voucher vẫn đúng số reservation của voucher đó.

### ~~[HOÀN THÀNH] TÁC VỤ-P1-02 - Cluster picking với tote/cart~~
**Lý do:** Cần cho picker lấy một chuyến và bỏ vào nhiều tote theo đơn.
**Phạm vi:**
- Model PickCart, PickTote, map tote → voucher.
- Lấy hàng bằng thiết bị cầm tay hiển thị tote code và bắt scan tote trước khi confirm.
- nhật ký kiểm toán khi gán/hủy tote.
**Tiêu chí nghiệm thu:**
- [x] Một wave có nhiều voucher được gán nhiều tote trên cùng cart.
- [x] Confirm sai tote bị chặn.
- [x] Backward compatible: wave không gán tote → picking hoạt động bình thường.
- [x] 3 unit tests pass: đúng tote, sai tote, không có tote.
- [x] Cart/Tote CRUD management page.
- [x] AssignTotes page for wave-voucher-tote mapping.

### ~~[HOÀN THÀNH] TÁC VỤ-P1-03 - Zone picking assignment~~
**Lý do:** Đã có Zone/Location, nhưng chưa có quy trình "nhân viên chỉ pick trong zone".
**Phạm vi:**
- Thêm cấu hình user-zone assignment (Data model: `UserZoneAssignment`).
- nhiệm vụ lấy hàng chỉ hiển thị cho nhân viên lấy hàng nếu source location thuộc zone được gán.
- Manager có board chuyển task giữa zone/picker.
**Tiêu chí nghiệm thu:**
- [x] Picker scoped zone A không thấy task zone B.
- [x] Manager vẫn thấy tổng quan.

### ~~[HOÀN THÀNH] TÁC VỤ-P1-04 - Short pick auto-reallocation~~
**Lý do:** Đã có PickTaskStatusEnum.Short, nhưng cần xử lý thiếu hàng tự động.
**Phạm vi:**
- Khi picker báo short, tạo exception case và tìm location bù khác bằng FEFO/available stock.
- Nếu có hàng bù: tạo pick task mới, release phần reservation cũ.
- Nếu không có hàng: đề xuất partial shipment/backorder.
**Tiêu chí nghiệm thu:**
- [x] Short 2/5 từ location A, hệ thống tự tạo task lấy 3 ở location B nếu có.

### ~~[HOÀN THÀNH] TÁC VỤ-P1-05 - Two-step picking và sortation~~
**Lý do:** Cần cho kho lớn lấy bulk ra staging/sort area, sau đó chia lẻ vào từng đơn/tote.
**Phạm vi:**
- Thêm loại task: BulkPickTask và SortTask hoặc thêm PickTaskMode.
- Cấu hình staging/sort location theo kho.
- Bulk pick gom tổng SKU/location, sort task tách về voucher/tote.
- Luồng thiết bị cầm tay: scan source → staging, sau đó scan staging → tote/order.
**Tiêu chí nghiệm thu:**
- [x] Nhiều đơn cùng SKU tạo 1 bulk pick task và nhiều sort task.
- [x] Hàng chưa sort xong không được post outbound.

### ~~[HOÀN THÀNH] TÁC VỤ-P1-06 - Phát hành đơn liên tục không cần đợt lấy hàng~~ 
**Lý do:** Một số kho cần đẩy đơn xuống bằng thiết bị cầm tay theo thời gian thực, không đợi gom wave.
**Phạm vi:**
- Tạo hàng đợi `OrderStreamingQueue` hoặc tái sử dụng `PickTask` với `WaveId` tùy chọn sau khi tái cấu trúc.
- Rule release theo priority/SLA/available stock.
- bằng thiết bị cầm tay picker nhận task tiếp theo liên tục.
- Có feature flag để bật/tắt, không phá wave quy trình hiện có.
**Tiêu chí nghiệm thu:**
- [x] Đơn urgent có thể sinh pick task ngay khi đủ điều kiện.
- [x] Quy trình đợt lấy hàng cũ vẫn hoạt động song song.


## P2 - Cross-Docking, Dock/Yard, Tối ưu vị trí

### ~~[HOÀN THÀNH] TÁC VỤ-P2-01 - Chuyển thẳng auto-detect và auto-match khép kín~~
**Lý do:** Đã có opportunity/task, nhưng execute chưa tạo reservation/pick/stock movement đầy đủ.
**Phạm vi:**
- Auto-detect inbound trong ngày có thể cấp cho outbound open demand.
- Auto-match inbound detail với outbound open demand theo item/lot/expiry/priority.
- Tạo reservation chuyển thẳng từ inbound sang outbound staging.
- CompleteChuyển thẳngTask cập nhật stock tại staging và link outbound reservation.
**Tiêu chí nghiệm thu:**
- [x] Hàng vừa receive được match sang đơn xuất trong ngày, không cần putaway lên kệ.

### ~~[HOÀN THÀNH] TÁC VỤ-P2-02 - Real-time dock board và time tracking~~
**Lý do:** Đã có dock appointment và Dock-to-Stock report, cần board vận hành theo thời gian thực.
**Phạm vi:**
- View Operations/DockBoard auto-refresh/polling.
- Trạng thái: scheduled, arrived, unloading, completed, delayed.
- Ghi mốc thời gian: gate in, dock arrival, unload start, unload end, completed.
**Tiêu chí nghiệm thu:**
- [x] Màn hình TV nhìn được cửa nào đang bận/trễ/hoàn tất.

### ~~[HOÀN THÀNH] TÁC VỤ-P2-03 - Yard management MVP~~
**Lý do:** Chưa có model trailer/container/gate/yard spot. Nên làm MVP trước, không nhảy thẳng detention phức tạp.
**Phạm vi:**
- Model YardSpot, YardVisit, Trailer.
- Gate in/out, assign spot, move spot, link voucher/dock appointment.
- Tính dwell time cơ bản.
**Tiêu chí nghiệm thu:**
- [x] Biết container nào đang ở bãi, đang đậu spot nào, vào/ra lúc nào.

### ~~[HOÀN THÀNH] TÁC VỤ-P2-03B - Yard billing detention/demurrage~~ **
**Lý do:** Phí lưu bãi chỉ nên tính sau khi có gate in/out và dwell time chuẩn.
**Phạm vi:**
- [x] Cấu hình free time theo carrier/customer/container type.
- [x] Tính detention/demurrage từ dwell time, có override miễn/giảm phí.
- [x] Báo cáo chi phí và export cho kế toán (UI quản lý/đối soát phí).
**Tiêu chí nghiệm thu:**
- [x] Tự động sinh phí dựa trên dwell time khi gate out vượt free time.
- [x] Container quá free time tự sinh dòng phí để đối soát.

### ~~[HOÀN THÀNH] TÁC VỤ-P2-04 - Auto-reslotting movement task~~
**Lý do:** ApplyTối ưu vị trí mới đổi default location, chưa tạo việc dời hàng thật.
**Phạm vi:**
- Model MovementTask cho relocate/replenishment/reslotting.
- Từ gợi ý vị trí lưu trữ tạo movement task.
- Luồng thiết bị cầm tay confirm move from location → to location.
**Tiêu chí nghiệm thu:**
- [x] Áp dụng gợi ý slotting không sửa tồn ngay, mà tạo task cho nhân viên thực hiện.

### ~~[HOÀN THÀNH] TÁC VỤ-P2-05 - Ergonomic/golden zone slotting~~
**Lý do:** Đã có phân tích tốc độ luân chuyển. Cần thêm rule hàng nặng/bán chạy vào vùng vàng, hàng nặng kệ thấp.
**Phạm vi:**
- Mở rộng Location: height level/golden zone/weight limit.
- Mở rộng Item: weight/dimensions đã có một phần, dùng vào scoring.
- Tối ưu vị trí score = velocity + ergonomics + capacity.
**Tiêu chí nghiệm thu:**
- [x] Hàng nặng không được gợi ý lên kệ cao.
- [x] Hàng A-class được ưu tiên golden zone nếu còn slot.

### ~~[HOÀN THÀNH] TÁC VỤ-P2-06 - Tối ưu vị trí simulation~~
**Lý do:** Trước khi đổi vị trí thật, cần giả lập xem tiết kiệm được bao nhiêu thời gian/quãng đường.
**Phạm vi:**
- Tạo scenario slotting simulation từ danh sách suggestion.
- Tính before/after theo pick frequency, travel distance/time, movement cost.
- Cho phép approve scenario để sinh MovementTask hàng loạt.
**Tiêu chí nghiệm thu:**
- [x] Người quản lý thấy được estimated saving trước khi áp dụng.


## P3 - Dịch vụ gia tăng và cấp doanh nghiệp lớn

### ~~[HOÀN THÀNH] TÁC VỤ-P3-01 - Lệnh ráp bộ dịch vụ gia tăng~~ 
**Lý do:** Có định mức nguyên vật liệu, nhưng cần quy trình tạo bộ hàng riêng.
**Phạm vi:**
- Bảng lệnh ráp bộ và dòng vật tư thành phần.
- Giữ chỗ vật tư thành phần, tiêu hao vật tư thành phần, tạo mã hàng bộ thành phẩm.
- In tem bộ hàng sau khi hoàn tất.
**Tiêu chí nghiệm thu:**
- [x] Tạo set quà tặng từ nhiều mã hàng thành 1 mã hàng bộ, có nhật ký tồn.

### ~~[HOÀN THÀNH]~~ TÁC VỤ-P3-02 - Customer-specific labeling 
**Lý do:** Đã có in label item, cần nhãn phụ theo khách hàng/đơn hàng.
**Phạm vi:**
- Template label theo customer/partner.
- Print job log.
- Link label với outbound package/voucher.
**Tiêu chí nghiệm thu:**
- [x] Cùng SKU nhưng khách A/B in nội dung tem khác nhau.
- [x] In nhãn kiện chỉ chạy khi xác định chắc nội dung: single-line package hoặc LPN-backed package; manual multi-line ambiguous bị chặn bằng business error.

### ~~[HOÀN THÀNH]~~ TÁC VỤ-P3-03 - Light assembly và co-packing 
**Lý do:** VAS không chỉ kitting; kho 3PL thường cần lắp ráp nhẹ, đóng gói lại, gắn phụ kiện, repack theo yêu cầu khách.
**Phạm vi:**
- Model VasWorkOrder, VasOperation, VasMaterialLine.
- Operation type: light assembly, co-packing, repack, relabel.
- Reserve vật tư phụ, ghi labor time, quality check sau VAS.
- Link VAS work order với inbound/outbound/customer.
**Tiêu chí nghiệm thu:**
- [x] Tạo và complete một lệnh co-packing có consume vật tư phụ và audit chi phí/labor.

### ~~[HOÀN THÀNH]~~ TÁC VỤ-P3-04 - Stock valuation report 
**Lý do:** Backend có UnitCost và TotalStockValue, nhưng cần màn hình tài chính đúng theo tồn thực từ ItemLocation.
**Phạm vi:**
- Report theo kho/category/item/lot.
- Giá trị = SUM(ItemLocation.Quantity) * UnitCost, tồn trong kỳ lấy theo movement nếu cần snapshot.
- Áp policy ReportViewFinancial.
**Tiêu chí nghiệm thu:**
- [x] User không có quyền tài chính không xem được value.

### ~~[HOÀN THÀNH]~~ TÁC VỤ-P3-05 - Xen kẽ nhiệm vụ sau khi có `MovementTask`
**Lý do:** Chỉ nên làm sau khi có movement/reslot/replenishment task chung.
**Phạm vi:**
- TaskInterleavingService: scoring engine 4 yếu tố (Proximity 40%, Priority 30%, Urgency 20%, Interleaving Bonus 10%).
- Gom `PickTask` và `MovementTask` vào hàng đợi nhiệm vụ xen kẽ thống nhất.
- Màn thiết bị cầm tay mới NextTask.cshtml: quét vị trí hiện tại → gợi ý task gần nhất với score bar trực quan.
- AcceptNextTask: picker nhận nhiệm vụ → auto-assign + redirect đến bằng thiết bị cầm tay tương ứng.
- 19 unit/regression tests (scoring, scope, assignment ownership, proximity theory, bằng thiết bị cầm tay visibility).
**Tiêu chí nghiệm thu:**
- [x] Picker vừa putaway ở dãy A được gợi ý pick/move gần dãy A nếu có.
- [x] 317/317 tests đạt.

### ~~[HOÀN THÀNH]~~ TÁC VỤ-P3-06 - Multi-tenant / 3PL billing
**Lý do:** Một kho cho nhiều chủ hàng cần tách dữ liệu tồn, đơn, báo cáo và tính phí riêng.
**Phạm vi:**
- Thêm Tenant/Client/Owner vào item, voucher, inventory, reservation, billing.
- Tenant scope cho UI/giao diện lập trình/report.
- Billing rules: storage fee, inbound/outbound handling, VAS, yard fee.
- Migration/seed/test đảm bảo không rò dữ liệu giữa tenant.
**Tiêu chí nghiệm thu:**
- [x] User tenant A không thấy tồn/đơn/báo cáo tenant B.
- [x] Hệ thống tính được phí lưu kho và phí xử lý theo tenant.

### ~~[HOÀN THÀNH]~~ TÁC VỤ-P3-07 - MHE / Robot / AMR integration
**Lý do:** Tích hợp băng chuyền, sorter, robot cần contract sự kiện ổn định, không nên gửi trực tiếp từ controller.
**Phạm vi:**
- Định nghĩa event/command: move tote, release wave, divert package, robot mission.
- Dùng outbox/idempotency/retry cho lệnh gửi MHE.
- Màn hình theo dõi mission status và lỗi integration.
- Adapter layer để sau này nối WCS/AMR vendor.
**Tiêu chí nghiệm thu:**
- [x] Pick/move task có thể phát command ra hệ thống ngoài và nhận callback cập nhật status.

## Defer - Chưa Nên Làm Ngay
- **TÁC VỤ-P3-06 Multi-tenant/3PL billing:** ~~Đã hoàn tất sau khi tồn kho theo kho/itemlocation đã chuẩn.~~
- **TÁC VỤ-P3-07 MHE/Robot/AMR:** ~~Đã hoàn tất integration contract, outbox và callback status.~~


