# WMS Pro Module Acceptance Checklist

Mỗi module lớn chỉ được coi là đạt Definition of Done khi đủ test nghiệp vụ, security/scope, UI/static và checklist nghiệm thu dưới đây.

## Inbound

- [ ] Business tests: tạo phiếu, duyệt, nhận hàng, lot/expiry, serial, catch weight, putaway, QC.
- [ ] Security tests: role tạo/duyệt/hoàn tất, warehouse scope, owner scope.
- [ ] UI tests: form tạo phiếu, nhận hàng, RF receiving, import Excel, lỗi nghiệp vụ.
- [ ] Acceptance: tồn tăng đúng, audit đầy đủ, không post khi thiếu dữ liệu bắt buộc.

## Outbound

- [ ] Business tests: release, allocation, pick, short pick, pack, ship, cancel, backorder.
- [ ] Security tests: role release/post/cancel, warehouse scope, owner scope.
- [ ] UI tests: wave, pick task, packing, shipping, mobile scan.
- [ ] Acceptance: tồn giữ chỗ/giảm đúng, serial/LPN không bị trùng, giao hàng có chứng từ.

## Inventory

- [ ] Business tests: balance, ledger, movement, adjustment, stock count, period lock.
- [ ] Security tests: unlock/adjust chỉ đúng role, export đúng scope.
- [ ] UI tests: tồn kho, giao dịch tồn, snapshot, kiểm kê.
- [ ] Acceptance: không âm tồn sai, không sửa kỳ khóa khi chưa unlock hợp lệ.

## Users And Security

- [ ] Business tests: tạo user, đổi mật khẩu, khóa tài khoản, trusted devices.
- [ ] Security tests: role/policy matrix, anti-forgery, MFA/trusted-device revoke.
- [ ] UI tests: Users table, export danh sách, reset password modal, Help lọc theo role.
- [ ] Acceptance: không user nào xem/thao tác vượt role/kho/chủ hàng.

## Reports And Analytics

- [ ] Business tests: inventory, movement, valuation, KPI, audit trail.
- [ ] Security tests: financial report policy, warehouse scope, export scope.
- [ ] UI tests: filter, export Excel, empty state, loading/error state.
- [ ] Acceptance: số liệu đối chiếu được với ledger và scope người dùng.

## Yard And Dock

- [ ] Business tests: gate-in, assign spot, move spot, gate-out, billing yard.
- [ ] Security tests: manager/admin config, staff vận hành, warehouse scope.
- [ ] UI tests: yard board, dock board, billing rates, charges.
- [ ] Acceptance: không có hai xe active cùng spot, phí bãi tính đúng rule.

## Carrier And Delivery Reconciliation

- [ ] Business tests: create shipment, retry, cancel, sync, callback idempotent, reconciliation.
- [ ] Security tests: connector config đúng role, callback/API scope, export reconciliation.
- [ ] UI tests: connector page, dispatch board, reconciliation page.
- [ ] Acceptance: không tạo vận đơn trùng, lỗi carrier vào outbox/retry rõ ràng.

## 3PL Billing And Multi-Owner

- [ ] Business tests: owner scope, rate, billing run, export, confirm/void.
- [ ] Security tests: `EnsureCanAccessOwnerAsync`, warehouse scope, billing policy.
- [ ] UI tests: billing runs, billing rates, modal edit, export.
- [ ] Acceptance: user owner/kho không xem được kỳ tính phí ngoài phạm vi.

## Mobile, RF And Offline Queue

- [ ] Business tests: queued receiving, picking, movement, shipment scan idempotent.
- [ ] Security tests: operational role only, token/anti-forgery for unsafe requests.
- [ ] UI tests: scan input, camera modal, offline queue widget, mobile viewport.
- [ ] Acceptance: mất mạng không mất thao tác, online lại không tạo giao dịch trùng.

## Integration And API

- [ ] Business tests: API item, stock, voucher, carrier/MHE callback.
- [ ] Security tests: API key, rate limit, scoped warehouse, payload validation.
- [ ] UI/static tests: API docs không lộ secret, connector health rõ ràng.
- [ ] Acceptance: integration lỗi vào log/outbox, không làm sai dữ liệu lõi.


