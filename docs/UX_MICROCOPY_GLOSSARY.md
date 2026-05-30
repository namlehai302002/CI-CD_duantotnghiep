# UX Microcopy Glossary

Date: 2026-05-20  
System: WMS Pro  
Tone: Vietnamese enterprise operations, concise, formal, and suitable for warehouse supervisors.

| Concept | Preferred Vietnamese | Notes |
|---|---|---|
| login | Đăng nhập | Use for sign-in actions and screen titles. |
| MFA | Xác thực đa yếu tố | Use with "mã xác thực" for the code field. |
| owner | Chủ hàng | Use for 3PL ownership and client-owned inventory. |
| warehouse | Kho | Use for warehouse scope and warehouse selection. |
| voucher | Phiếu kho | Use across inbound, outbound, transfer, and adjustment flows. |
| wave | Đợt lấy hàng | Use for planned wave picking. |
| pick | Lấy hàng | Use for picking task actions. |
| pack | Đóng gói | Use for packing, cartons, and package confirmation. |
| ship | Giao hàng | Use for shipping confirmation and dispatch. |
| billing | Tính phí | Use for 3PL charges, invoice preparation, and settlement. |
| audit | Nhật ký kiểm soát | Use for audit trail and security evidence. |
| device | Thiết bị | Use for trusted devices, scanners, and MHE devices. |
| exception | Ngoại lệ vận hành | Use for process exceptions and exception center. |

## Rules

- Keep brand text as `WMS Pro`.
- Use Vietnamese labels for operator-facing text unless the source term is an integration standard such as EDI, API, ASN, 940, 945, or 856.
- Avoid mixed English labels such as `User`, `Login`, `Password`, `Report`, or `Export` when a clear Vietnamese equivalent exists.
- Do not use mojibake or broken encoding sequences. Regression tests must check known bad strings, not raw characters that can appear in valid Vietnamese text.

