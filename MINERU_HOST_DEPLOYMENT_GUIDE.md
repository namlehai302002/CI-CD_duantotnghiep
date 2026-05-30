# Hướng Dẫn Deploy MinerU Cho WMS

Ngày cập nhật: 2026-05-17

## Kết Luận Nhanh

Với gói **Hosting Windows Sinh Viên InterData** dạng shared ASP.NET/IIS, 1GB RAM và 2GB SSD, bạn có thể deploy WMS ASP.NET Core nếu provider hỗ trợ đúng runtime, database và quyền cấu hình cần thiết. Tuy nhiên **không nên chạy MinerU chung trên gói host này**.

MinerU cần môi trường Python/service riêng, thường chạy như API service hoặc Docker container, có model/cache và cần tài nguyên RAM/SSD lớn hơn nhiều so với gói shared host 1GB/2GB. Shared Windows hosting cũng thường không cho mở process nền dài hạn, custom port API, Docker, GPU hoặc Python runtime tự quản.

## Cách Dùng MinerU Đúng Kiến Trúc

Khuyến nghị triển khai theo 1 trong 3 hướng:

1. **WMS trên InterData, MinerU trên VPS riêng**
   - WMS chạy ASP.NET Core/IIS trên shared hosting.
   - MinerU chạy trên VPS Linux/Windows riêng có Python/Docker.
   - WMS gọi MinerU qua `MinerU:BaseUrl`.

2. **WMS và MinerU trên cùng VPS mạnh hơn**
   - Phù hợp khi bạn thuê VPS có đủ RAM/SSD.
   - Dễ kiểm soát background service, port, log và firewall hơn shared host.

3. **Dùng API ngoài tương thích MinerU**
   - Phù hợp nếu không muốn tự vận hành model/service.
   - Cần kiểm tra bảo mật file chứng từ, vùng lưu trữ, SLA và chi phí.

## Cấu Hình Cho Gói InterData Shared Hosting

Trên gói InterData shared ASP.NET hiện tại, đặt MinerU ở trạng thái tắt trong cấu hình production override của hosting:

```json
"MinerU": {
  "Enabled": false,
  "BaseUrl": "",
  "TimeoutSeconds": 120
}
```

Không cần xóa các giá trị hiện có trong `appsettings.json` ở repo trong pass này. Khi lên production, bạn có thể override bằng biến môi trường, Plesk/IIS app settings hoặc file cấu hình riêng do host quản lý. Nếu sau này dùng VPS MinerU riêng thì đổi `Enabled=true` và trỏ `BaseUrl` tới service MinerU riêng.

## Checklist Trước Khi Bật MinerU

- Provider xác nhận có thể chạy Python/Docker hoặc background service dài hạn.
- Có endpoint health check của MinerU, ví dụ `/health`.
- WMS gọi được endpoint parse file qua mạng nội bộ hoặc URL bảo mật.
- Có giới hạn dung lượng upload, MIME type, đuôi file và scan virus nếu nhận chứng từ từ người dùng.
- Có timeout, retry có kiểm soát và log lỗi không chứa dữ liệu nhạy cảm.
- Kết quả đọc chứng từ chỉ đưa vào màn hình xem trước; không tự động post tồn kho khi người dùng chưa xác nhận.

## Nguồn Tham Khảo

- MinerU Quick Start: https://opendatalab.github.io/MinerU/quick_start/
- MinerU Docker/API Deployment: https://opendatalab.github.io/MinerU/quick_start/docker_deployment/


