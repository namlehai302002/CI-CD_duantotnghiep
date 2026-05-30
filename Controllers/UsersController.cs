using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Common;
using WMS.Models;
using WMS.Authorization;
using WMS.ViewModels;

namespace WMS.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = WmsPermissions.UserManage)]
public class UsersController : Controller
{
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db) => _db = db;

    private static bool IsStrongPassword(string password)
        => SecurityHelpers.IsStrongPassword(password);

    private string CurrentActor => User.Identity?.Name ?? "system";

    public async Task<IActionResult> Index()
    {
        var users = await _db.AppUsers.Include(u => u.Role).Include(u => u.Warehouse)
            .Where(u => u.IsActive).OrderBy(u => u.UserName).ToListAsync();
        ViewBag.Roles = await _db.AppRoles.ToListAsync();
        ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync();
        return View(users);
    }

    public async Task<IActionResult> LoginHelpRequests(string? search, LoginHelpRequestStatusEnum? status, LoginHelpRequestReasonEnum? reason)
    {
        var query = _db.LoginHelpRequests.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (reason.HasValue)
            query = query.Where(x => x.Reason == reason.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.RequestCode.Contains(term)
                || x.FullName.Contains(term)
                || x.LoginIdentifier.Contains(term)
                || (x.ContactPhone != null && x.ContactPhone.Contains(term))
                || (x.WarehouseOrDepartment != null && x.WarehouseOrDepartment.Contains(term)));
        }

        var requests = await query
            .OrderBy(x => x.Status == LoginHelpRequestStatusEnum.New ? 0 : 1)
            .ThenByDescending(x => x.CreatedAt)
            .Take(200)
            .ToListAsync();

        var identifiers = requests
            .Select(x => x.LoginIdentifier.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.ToLowerInvariant())
            .Distinct()
            .ToList();

        var users = identifiers.Count == 0
            ? new List<AppUser>()
            : await _db.AppUsers
                .AsNoTracking()
                .Include(x => x.Role)
                .Include(x => x.Warehouse)
                .Where(x => identifiers.Contains(x.UserName.ToLower()) || (x.Email != null && identifiers.Contains(x.Email.ToLower())))
                .ToListAsync();

        var userByIdentifier = users
            .SelectMany(user => new[]
            {
                new { Key = user.UserName.ToLowerInvariant(), User = user },
                string.IsNullOrWhiteSpace(user.Email) ? null : new { Key = user.Email!.ToLowerInvariant(), User = user }
            })
            .Where(x => x != null)
            .GroupBy(x => x!.Key)
            .ToDictionary(g => g.Key, g => g.First()!.User, StringComparer.OrdinalIgnoreCase);

        var statusCounts = await _db.LoginHelpRequests
            .AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        var vm = new LoginHelpRequestQueueViewModel
        {
            Search = search,
            Status = status,
            Reason = reason,
            StatusCounts = statusCounts,
            Requests = requests.Select(request =>
            {
                userByIdentifier.TryGetValue(request.LoginIdentifier.Trim().ToLowerInvariant(), out var matchedUser);
                return new LoginHelpRequestAdminRow { Request = request, MatchedUser = matchedUser };
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string userName, string fullName, string email, string password, int roleId, int? warehouseId)
    {
        if (await _db.AppUsers.AnyAsync(u => u.UserName == userName))
        {
            TempData["Error"] = "Tên đăng nhập đã tồn tại.";
            return RedirectToAction("Index");
        }

        var role = await _db.AppRoles.FirstOrDefaultAsync(r => r.RoleId == roleId);
        if (role == null)
        {
            TempData["Error"] = "Vai trò không hợp lệ.";
            return RedirectToAction("Index");
        }

        if (!string.Equals(role.RoleName, "Admin", StringComparison.OrdinalIgnoreCase) && !warehouseId.HasValue)
        {
            TempData["Error"] = "Người dùng không phải Admin bắt buộc phải gán kho.";
            return RedirectToAction("Index");
        }

        if (!IsStrongPassword(password))
        {
            TempData["Error"] = "Mật khẩu yếu. Yêu cầu tối thiểu 8 ký tự gồm chữ hoa, chữ thường, số và ký tự đặc biệt.";
            return RedirectToAction("Index");
        }

        if (!string.IsNullOrWhiteSpace(email) && !SecurityHelpers.IsValidEmail(email))
        {
            TempData["Error"] = "Địa chỉ email không hợp lệ.";
            return RedirectToAction("Index");
        }

        _db.AppUsers.Add(new AppUser
        {
            UserName = userName?.Trim() ?? "",
            FullName = fullName?.Trim() ?? "",
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            RoleId = roleId,
            WarehouseId = warehouseId,
            CreatedAt = VietnamTime.Now
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã tạo tài khoản '{userName}'.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id, string newPassword)
    {
        var user = await _db.AppUsers.FindAsync(id);
        if (user == null) return NotFound();
        if (!IsStrongPassword(newPassword))
        {
            TempData["Error"] = "Mật khẩu yếu. Yêu cầu tối thiểu 8 ký tự gồm chữ hoa, chữ thường, số và ký tự đặc biệt.";
            return RedirectToAction("Index");
        }
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        _db.LoginAuditLogs.Add(new LoginAuditLog
        {
            UserName = user.UserName,
            UserId = user.UserId,
            IsSuccess = true,
            Outcome = "PASSWORD_RESET_BY_ADMIN",
            Reason = $"actor={User.Identity?.Name ?? "system"}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            CreatedAt = VietnamTime.Now
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã đổi mật khẩu cho '{user.UserName}'.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkLoginHelpInReview(long id)
    {
        var request = await _db.LoginHelpRequests.FindAsync(id);
        if (request == null) return NotFound();

        request.Status = LoginHelpRequestStatusEnum.InReview;
        request.HandledBy = CurrentActor;
        request.ResolutionNote = string.IsNullOrWhiteSpace(request.ResolutionNote)
            ? "Admin đã tiếp nhận yêu cầu."
            : request.ResolutionNote;

        _db.LoginAuditLogs.Add(new LoginAuditLog
        {
            UserName = request.LoginIdentifier,
            IsSuccess = true,
            Outcome = "LOGIN_HELP_REQUEST_IN_REVIEW",
            Reason = $"request={request.RequestCode};actor={CurrentActor}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            CreatedAt = VietnamTime.Now
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã tiếp nhận yêu cầu {request.RequestCode}.";
        return RedirectToAction(nameof(LoginHelpRequests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveLoginHelpRequest(long id, int? resetUserId, string? newPassword, string? resolutionNote)
    {
        var request = await _db.LoginHelpRequests.FindAsync(id);
        if (request == null) return NotFound();

        var note = string.IsNullOrWhiteSpace(resolutionNote)
            ? "Đã xử lý yêu cầu hỗ trợ đăng nhập."
            : resolutionNote.Trim();

        if (resetUserId.HasValue || !string.IsNullOrWhiteSpace(newPassword))
        {
            if (!resetUserId.HasValue)
            {
                TempData["Error"] = "Vui lòng chọn tài khoản cần đổi mật khẩu hoặc bỏ trống mật khẩu tạm.";
                return RedirectToAction(nameof(LoginHelpRequests));
            }

            if (string.IsNullOrWhiteSpace(newPassword) || !IsStrongPassword(newPassword))
            {
                TempData["Error"] = "Mật khẩu tạm phải đủ mạnh: tối thiểu 8 ký tự gồm chữ hoa, chữ thường, số và ký tự đặc biệt.";
                return RedirectToAction(nameof(LoginHelpRequests));
            }

            var user = await _db.AppUsers.FindAsync(resetUserId.Value);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản cần đổi mật khẩu.";
                return RedirectToAction(nameof(LoginHelpRequests));
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.FailedLoginCount = 0;
            user.LockoutEnd = null;

            _db.LoginAuditLogs.Add(new LoginAuditLog
            {
                UserName = user.UserName,
                UserId = user.UserId,
                IsSuccess = true,
                Outcome = "PASSWORD_RESET_FROM_LOGIN_HELP",
                Reason = $"request={request.RequestCode};actor={CurrentActor}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                CreatedAt = VietnamTime.Now
            });
        }

        request.Status = LoginHelpRequestStatusEnum.Resolved;
        request.ResolutionNote = note.Length <= 500 ? note : note[..500];
        request.HandledBy = CurrentActor;
        request.HandledAt = VietnamTime.Now;

        _db.LoginAuditLogs.Add(new LoginAuditLog
        {
            UserName = request.LoginIdentifier,
            IsSuccess = true,
            Outcome = "LOGIN_HELP_REQUEST_RESOLVED",
            Reason = $"request={request.RequestCode};actor={CurrentActor}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            CreatedAt = VietnamTime.Now
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã hoàn tất yêu cầu {request.RequestCode}.";
        return RedirectToAction(nameof(LoginHelpRequests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectLoginHelpRequest(long id, string? resolutionNote)
    {
        var request = await _db.LoginHelpRequests.FindAsync(id);
        if (request == null) return NotFound();

        var note = string.IsNullOrWhiteSpace(resolutionNote)
            ? "Yêu cầu chưa đủ thông tin để xử lý."
            : resolutionNote.Trim();

        request.Status = LoginHelpRequestStatusEnum.Rejected;
        request.ResolutionNote = note.Length <= 500 ? note : note[..500];
        request.HandledBy = CurrentActor;
        request.HandledAt = VietnamTime.Now;

        _db.LoginAuditLogs.Add(new LoginAuditLog
        {
            UserName = request.LoginIdentifier,
            IsSuccess = true,
            Outcome = "LOGIN_HELP_REQUEST_REJECTED",
            Reason = $"request={request.RequestCode};actor={CurrentActor}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            CreatedAt = VietnamTime.Now
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã đóng yêu cầu {request.RequestCode}.";
        return RedirectToAction(nameof(LoginHelpRequests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.AppUsers.FindAsync(id);
        if (user == null) return NotFound();
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(currentUserId, out var selfId) && selfId == id)
        {
            TempData["Error"] = "Không thể tự khóa tài khoản đang đăng nhập.";
            return RedirectToAction("Index");
        }
        user.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã khóa tài khoản '{user.UserName}'.";
        return RedirectToAction("Index");
    }
}
