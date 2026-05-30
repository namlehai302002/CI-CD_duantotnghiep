# Enterprise Next Upgrade Roadmap

Ngày cập nhật: 2026-05-17

### [x] P4-11 - Kiểm Thử Hồi Quy Nghiệp Vụ Lõi

Đã giữ các marker hồi quy cho nhập kho, xuất kho, LPN, catch weight, carrier, shipping, reconciliation, hàng đợi quét và luồng từ ASN đến invoice.

### [x] P4-12 - Rà Soát Ngôn Ngữ Và Trải Nghiệm Người Dùng Vận Hành

Đã rà soát các chuỗi người dùng dễ thấy, sửa lỗi gõ tiếng Việt và bổ sung kiểm thử static để tránh tái phát các typo đã biết.

## Verification

```powershell
dotnet build WMS.sln --no-restore -v:minimal
dotnet test WMS.Tests\WMS.Tests.csproj --no-restore -v:minimal
dotnet format WMS.sln --verify-no-changes --no-restore -v:minimal
dotnet ef migrations list --no-build
```

## Follow-Up

- Hoàn tất artifact visual regression có auth state thật.
- Chạy k6 trên staging thật và lưu kết quả.
- Hoàn tất evidence backup/restore và checklist production.


