# ENTERPRISE WMS ARCHITECTURE UPGRADE BACKLOG

Tài liệu này theo dõi tiến độ của chiến dịch "Đại phẫu Kiến trúc WMS" nhằm nâng cấp hệ thống từ mức độ Tiêu chuẩn (Standard) lên mức độ Doanh nghiệp quy mô lớn (Global Enterprise WMS) dựa trên kết quả kiểm toán (Audit).

**Trạng thái:**
- `[ ]` Chưa bắt đầu (To Do)
- `[/]` Đang tiến hành (In Progress)
- `[x]` Đã hoàn thành (Done)
- `[-]` Bị hoãn / Đã hủy (Deferred/Cancelled)

---

## EPIC 1: LPN ARCHITECTURE TRANSFORMATION (Mã kiện Container-Centric) ~~[COMPLETED]~~
**Mục tiêu:** Đập bỏ kiến trúc LPN phẳng 1:1, chuyển sang Nested LPN làm nền tảng cốt lõi (Source of Truth).

- `[x]` **P1-01: Core LPN Models**
  - Tạo `LicensePlate` (Header) với `Status`, `LpnType` (Pallet/Carton/Tote).
  - Tạo `LicensePlateDetail` (Line items) chứa Item, Qty, Lot, Expiry.
- `[x]` **P1-02: Nested LPN Support**
  - Thêm `ParentLpnId` hỗ trợ lồng nhau.
  - Áp dụng Hierarchy constraint chống vòng lặp (Loop A -> B -> A).
- `[x ]` **P1-03: Performance Optimization**
  - Chuẩn hóa ngược (Denormalize) `CurrentLocationId` vào Header để tối ưu read performance.
- `[x]` **P1-04: Cartonization & Load Planning Prep**
  - Thiết kế sẵn các trường `Capacity`, `Volume`, `Weight` trên LPN.

---

## EPIC 2: DISTRIBUTED INVENTORY CONSISTENCY (Nhất quán Tồn kho) ~~[COMPLETED]~~
**Mục tiêu:** Chuyển `ItemLocation` thành bản Snapshot và quản lý di chuyển (Atomic Movement) dựa trên sự kiện LPN.

- `[x]` **P2-01: LPN Atomic Movement**
  - Refactor các tính năng Move/Chuyển kho theo LPN (Move by LPN thay vì Move by Item).
- `[x]` **P2-02: Event-driven Snapshot Sync**
  - Áp dụng **Outbox Pattern** để sync dữ liệu liên tục từ LPN Events sang `ItemLocation` snapshot.
- `[x]` **P2-03: SLA Reconciliation Job**
  - Thiết lập Background Job đối soát (Eventual Consistency) chạy chu kỳ 5-15 phút.
  - Định nghĩa Ngưỡng lệch (Threshold), cơ chế Auto-heal (Tự động sửa Snapshot) và Alert.

---

## EPIC 3: HIGH-CONCURRENCY SERIAL TRACKING (Quản lý Serial chịu tải cao)~~[COMPLETED]~~
**Mục tiêu:** Biến Serial thành một "Inventory Unit" độc lập, chống Deadlock.

- `[x]` **P3-01: Serial Independence**
  - Gắn chặt Serial với `LocationId`, `LicensePlateId`, và `Status` (Available/Allocated/Picked/Shipped).
- `[x]` **P3-02: Concurrency & Lock Handling**
  - Triển khai **Idempotent Transactions**.
  - Thiết kế **Reservation Pattern** (Allocate/Giữ chỗ Serial trước khi Pick thực tế) thay vì chỉ dùng DB Constraint.

---

## EPIC 4: AUDIT TRAIL & COMPLIANCE (Nhật ký Giao dịch Chuẩn) ~~[COMPLETED]~~
**Mục tiêu:** Truy xuất nguồn gốc trọn vẹn vòng đời hàng hóa, kể cả khi LPN đã bị hủy.

- `[x]` **P4-01: Inventory Transaction Log**
  - Tạo bảng `InventoryTransaction` lưu lượng Delta (+/-), Before/After state, và Reference (Voucher/Task).
- `[x]` **P4-02: Semantic Rule Mapping**
  - Khởi tạo quy tắc Transaction Types (RECEIVE / PUTAWAY / MOVE / PICK / PACK / ADJUST).
  - VD: Mapping cứng rule `PICK` = `- Available`, `+ Allocated/Issued`.

---

## EPIC 5: ADVANCED AUTOMATION (Tự động hóa Nâng cao - Optional) ~~[COMPLETED]~~
**Mục tiêu:** Tối ưu hóa vận hành nhà kho và đường đi của thiết bị (Forklift).

- `[x]` **P5-01: Demand-based & Forecast Replenishment**
  - Bổ sung logic tự động sinh Task bổ sung hàng dựa trên Wave Planning hoặc dự báo thay vì chỉ dựa vào MinThreshold.
- `[x]` **P5-02: Task Prioritization & Routing**
  - Sắp xếp và phân bổ Movement Task theo Queue / Priority / Zone để xe nâng không phải di chuyển lung tung (Travel Path Optimization).

---

## EPIC 6: NICHE LOGISTICS INTEGRATION (Tích hợp Hệ sinh thái) ~~[COMPLETED]~~
**Mục tiêu:** Các tính năng chuyên biệt dành riêng cho Logistics quy mô lớn.

- `[x]` **P6-01: Catch Weight Management** *(Optional - Kho Lạnh/Thực phẩm)*
  - Áp dụng Feature Flag tại `ItemMaster`. Xử lý xuất hàng theo Piece nhưng ghi nhận Weight thực tế.
- `[x]` **P6-02: Master Load / Shipment** *(Optional - Tích hợp TMS)*
  - Xây dựng tầng `Load` gom nhóm nhiều `Voucher` vào một Chuyến xe để quản lý bến bãi (Dock).

