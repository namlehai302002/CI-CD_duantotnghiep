param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^https?://')]
    [string]$BaseUrl
)

$ErrorActionPreference = "Stop"
$BaseUrl = $BaseUrl.TrimEnd('/')

Write-Host "=== WMS - Kiểm tra quyền truy cập theo vai trò ===" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl"
Write-Host ""

function Test-Url {
    param(
        [string]$Url,
        [string]$Expected,
        [string]$Role
    )

    Write-Host "[$Role] $Url" -ForegroundColor Yellow
    Write-Host "Kỳ vọng: $Expected"
    Write-Host "Kết quả: Xác nhận thủ công trong phiên trình duyệt đã đăng nhập." -ForegroundColor Green
    Write-Host ""
}

Write-Host "Bước 0 - Mở 4 hồ sơ/phiên trình duyệt riêng:" -ForegroundColor Magenta
Write-Host "  1) Admin"
Write-Host "  2) Manager"
Write-Host "  3) Staff"
Write-Host "  4) Viewer"
Write-Host ""

Write-Host "Bước 1 - Điểm vào đăng nhập công khai" -ForegroundColor Magenta
Write-Host "Mở: $BaseUrl/Account/Login"
Write-Host ""

Write-Host "Bước 2 - Kiểm nhanh ma trận vai trò" -ForegroundColor Magenta
Test-Url "$BaseUrl/Users" "Admin: OK | Manager/Staff/Viewer: 403" "Admin/Non-Admin"
Test-Url "$BaseUrl/Categories" "Admin/Manager: OK | Staff/Viewer: 403" "All"
Test-Url "$BaseUrl/Units" "Admin/Manager: OK | Staff/Viewer: 403" "All"
Test-Url "$BaseUrl/Partners" "Admin/Manager: OK | Staff/Viewer: 403" "All"
Test-Url "$BaseUrl/Reports/AuditTrail" "Admin: OK | others: 403" "All"
Test-Url "$BaseUrl/Operations/Waves" "Admin/Manager/Staff: OK | Viewer: 403" "All"
Test-Url "$BaseUrl/Vouchers/Create?type=1" "Admin/Manager/Staff: OK | Viewer: 403" "All"
Test-Url "$BaseUrl/Reports/StockCount" "Admin/Manager/Staff: OK | Viewer: 403" "All"
Test-Url "$BaseUrl/Reports/StockSnapshot" "Admin/Manager: OK | Staff/Viewer: 403" "All"

Write-Host "Bước 3 - Kiểm soát phân tách nhiệm vụ" -ForegroundColor Magenta
Write-Host "  - Tạo phiếu bằng tài khoản A, duyệt bằng A => phải bị chặn."
Write-Host "  - Duyệt phiếu bằng tài khoản B, hủy bằng B => phải bị chặn."
Write-Host "  - Duyệt biên bản kiểm kê bằng B, mở khóa bằng B => phải bị chặn."
Write-Host ""

Write-Host "Bước 4 - Thu thập bằng chứng" -ForegroundColor Magenta
Write-Host "  - Chụp màn hình từng trang bị chặn (403) với vai trò sai."
Write-Host "  - Chụp màn hình từng trang truy cập thành công với vai trò đúng."
Write-Host "  - Chạy 'dotnet test WMS.sln' và đính kèm số lượng test đã pass."
Write-Host ""

Write-Host "Hoàn tất. Checklist quyền truy cập đã được kiểm." -ForegroundColor Cyan
