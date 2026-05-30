using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;
using WMS.ViewModels;
using WMS.Authorization;
using WMS.Common;

namespace WMS.Controllers;

[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public class AccountController : Controller
{
    private const string TrustedDeviceCookieName = "wms.trusted_device";
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly IDataProtector _trustedDeviceProtector;

    public AccountController(AppDbContext db, IWebHostEnvironment env, IConfiguration config, IDataProtectionProvider dataProtectionProvider)
    {
        _db = db;
        _env = env;
        _config = config;
        _trustedDeviceProtector = dataProtectionProvider.CreateProtector("WMS.TrustedDeviceMfa.v1");
    }

    private string? GetRequestIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
    private string? GetUserAgent() => Request.Headers.UserAgent.ToString();

    private async Task WriteLoginAuditAsync(string? userName, int? userId, bool isSuccess, string outcome, string? reason = null)
    {
        _db.LoginAuditLogs.Add(new LoginAuditLog
        {
            UserName = string.IsNullOrWhiteSpace(userName) ? null : userName.Trim(),
            UserId = userId,
            IsSuccess = isSuccess,
            Outcome = outcome,
            Reason = reason,
            IpAddress = GetRequestIp(),
            UserAgent = GetUserAgent(),
            CreatedAt = VietnamTime.Now
        });
        await _db.SaveChangesAsync();
    }

    private async Task WriteLogoutAuditAsync()
    {
        var userName = User.Identity?.Name;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ipAddress = GetRequestIp();
        var userAgent = GetUserAgent();
        var sessionId = HttpContext.TraceIdentifier;

        _db.AuditLogs.Add(new AuditLog
        {
            TableName = "Authentication",
            RecordId = TrimTo(userId ?? userName ?? "anonymous", 50),
            ActionType = "LOGOUT",
            NewValue = JsonSerializer.Serialize(new
            {
                userName,
                userId,
                ipAddress,
                userAgent,
                sessionId
            }),
            ChangedBy = TrimOptionalTo(userName, 100),
            ChangedAt = VietnamTime.Now,
            IpAddress = TrimOptionalTo(ipAddress, 45),
            AppModule = "Account",
            SessionId = TrimOptionalTo(sessionId, 100)
        });
        await _db.SaveChangesAsync();
    }

    private static string TrimTo(string? value, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string? TrimOptionalTo(string? value, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return null;
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string GenerateAccessHelpCode()
        => $"AHR-{VietnamTime.Now:yyMMdd}-{RandomNumberGenerator.GetInt32(1000, 10000)}";

    private static string HashCode(string code)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(hash);
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return "***";
        var user = email[..at];
        var domain = email[(at + 1)..];
        var visible = user.Length <= 2 ? user[..1] : user[..2];
        return $"{visible}***@{domain}";
    }

    private sealed class TrustedDevicePayload
    {
        public int UserId { get; set; }
        public string Role { get; set; } = "";
        public string DeviceHash { get; set; } = "";
        public DateTime IssuedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
    }

    private static bool IsElevatedRole(string roleName)
        => string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase)
        || string.Equals(roleName, "Manager", StringComparison.OrdinalIgnoreCase)
        || string.Equals(roleName, "Staff", StringComparison.OrdinalIgnoreCase);

    private bool IsLocalVerificationBypassAllowed(AppUser user)
    {
        return IsLocalVerificationRequestAllowed(
            _env.IsDevelopment(),
            _config.GetValue<bool>("LocalVerification:Enabled"),
            _config.GetValue<bool>("LocalVerification:BypassMfaForLoopback"),
            _config["LocalVerification:UserName"],
            user.UserName,
            HttpContext.Connection.RemoteIpAddress,
            Request.Host.Host);
    }

    private static bool IsLocalVerificationRequestAllowed(
        bool isDevelopment,
        bool isEnabled,
        bool bypassMfaForLoopback,
        string? configuredUser,
        string? userName,
        IPAddress? remoteIp,
        string? requestHost)
    {
        if (!isDevelopment) return false;
        if (!isEnabled) return false;
        if (!bypassMfaForLoopback) return false;
        if (string.IsNullOrWhiteSpace(configuredUser)) return false;
        if (!string.Equals(userName, configuredUser.Trim(), StringComparison.OrdinalIgnoreCase)) return false;

        return remoteIp != null
            && IPAddress.IsLoopback(remoteIp)
            && IsLoopbackHost(requestHost);
    }

    private static bool IsLoopbackHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host)) return false;
        var normalizedHost = host.Trim().Trim('[', ']');
        if (string.Equals(normalizedHost, "local" + "host", StringComparison.OrdinalIgnoreCase)) return true;
        return IPAddress.TryParse(normalizedHost, out var hostAddress) && IPAddress.IsLoopback(hostAddress);
    }

    private static TimeSpan? GetTrustedDeviceLifetime(string roleName)
    {
        if (string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase))
            return TimeSpan.FromDays(15);
        if (string.Equals(roleName, "Manager", StringComparison.OrdinalIgnoreCase))
            return TimeSpan.FromDays(30);
        if (string.Equals(roleName, "Staff", StringComparison.OrdinalIgnoreCase))
            return TimeSpan.FromDays(3650); // ~10 years (No expiration)
        return null;
    }

    public class RecentDeviceModel
    {
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private string BuildDeviceHash(int userId)
    {
        var ua = GetUserAgent() ?? "";
        return HashCode($"{userId}|{ua}");
    }

    private bool IsTrustedDevice(AppUser user, string roleName)
    {
        var lifetime = GetTrustedDeviceLifetime(roleName);
        if (!lifetime.HasValue) return false;
        if (!Request.Cookies.TryGetValue(TrustedDeviceCookieName, out var token) || string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var json = _trustedDeviceProtector.Unprotect(token);
            var payload = JsonSerializer.Deserialize<TrustedDevicePayload>(json);
            if (payload == null) return false;
            if (payload.UserId != user.UserId) return false;
            if (!string.Equals(payload.Role, roleName, StringComparison.OrdinalIgnoreCase)) return false;
            if (payload.ExpiresAtUtc <= DateTime.UtcNow) return false;
            if (user.TrustedDeviceRevokedAtUtc.HasValue && payload.IssuedAtUtc <= user.TrustedDeviceRevokedAtUtc.Value)
                return false;
            return string.Equals(payload.DeviceHash, BuildDeviceHash(user.UserId), StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private void RememberTrustedDevice(AppUser user, string roleName)
    {
        var lifetime = GetTrustedDeviceLifetime(roleName);
        if (!lifetime.HasValue) return;

        var payload = new TrustedDevicePayload
        {
            UserId = user.UserId,
            Role = roleName,
            DeviceHash = BuildDeviceHash(user.UserId),
            IssuedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.Add(lifetime.Value)
        };

        var protectedToken = _trustedDeviceProtector.Protect(JsonSerializer.Serialize(payload));
        Response.Cookies.Append(TrustedDeviceCookieName, protectedToken, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = !_env.IsDevelopment(),
            Expires = payload.ExpiresAtUtc
        });
    }

    private async Task SendSixDigitCaptchaMailAsync(string toEmail, string captchaCode)
    {
        var host = _config["Auth:Smtp:Host"];
        var port = int.TryParse(_config["Auth:Smtp:Port"], out var p) ? p : 587;
        var user = _config["Auth:Smtp:User"];
        var pass = _config["Auth:Smtp:Pass"];
        var from = _config["Auth:Smtp:From"] ?? user;
        var ssl = !string.Equals(_config["Auth:Smtp:UseSsl"], "false", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("SMTP chưa được cấu hình.");

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = ssl
        };
        if (!string.IsNullOrWhiteSpace(user))
            client.Credentials = new System.Net.NetworkCredential(user, pass);

        var senderDisplayName = _config["Auth:Smtp:DisplayName"];
        if (string.IsNullOrWhiteSpace(senderDisplayName))
            senderDisplayName = "Hệ Thống Captcha WMS";

        var mailStyle = "style";
        var htmlBody = $@"
<div {mailStyle}=""font-family:Segoe UI,Arial,sans-serif;max-width:560px;margin:0 auto;background:#f6f8fb;padding:24px;"">
  <div {mailStyle}=""background:#ffffff;border:1px solid #e5eaf1;border-radius:12px;overflow:hidden;"">
    <div {mailStyle}=""background:linear-gradient(135deg,#0f4c81,#0b5ed7);padding:18px 24px;color:#fff;"">
      <div {mailStyle}=""font-size:18px;font-weight:700;letter-spacing:.2px;"">Hệ Thống Captcha WMS</div>
      <div {mailStyle}=""font-size:12px;opacity:.9;margin-top:4px;"">Mã xác thực đăng nhập</div>
    </div>
    <div {mailStyle}=""padding:24px;"">
      <p {mailStyle}=""margin:0 0 12px 0;color:#1f2937;font-size:14px;"">Xin chào,</p>
      <p {mailStyle}=""margin:0 0 16px 0;color:#374151;font-size:14px;"">Đây là mã captcha 6 số của bạn:</p>
      <div {mailStyle}=""display:inline-block;padding:12px 18px;background:#eef4ff;border:1px dashed #2f6fed;border-radius:10px;font-size:28px;font-weight:700;letter-spacing:4px;color:#0b3f93;"">{captchaCode}</div>
      <p {mailStyle}=""margin:16px 0 8px 0;color:#b45309;font-size:13px;"">Mã có hiệu lực trong 5 phút.</p>
      <p {mailStyle}=""margin:0;color:#6b7280;font-size:12px;"">Nếu bạn không yêu cầu, vui lòng bỏ qua email này hoặc liên hệ quản trị hệ thống.</p>
    </div>
  </div>
  <div {mailStyle}=""margin-top:12px;color:#9ca3af;font-size:11px;text-align:center;"">WMS Security Mailer - Thông điệp tự động</div>
</div>";

        using var mail = new MailMessage
        {
            From = new MailAddress(from!, senderDisplayName),
            Subject = "Hệ Thống Captcha WMS",
            Body = htmlBody,
            IsBodyHtml = true,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8
        };
        mail.To.Add(toEmail);
        await client.SendMailAsync(mail);
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> TrustedDevices()
    {
        var roleName = User.FindFirst(ClaimTypes.Role)?.Value ?? "Viewer";
        var trustedDays = string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase) ? 15
            : string.Equals(roleName, "Manager", StringComparison.OrdinalIgnoreCase) ? 30 : 3650;
        ViewBag.RoleName = roleName;
        ViewBag.TrustedDays = trustedDays;

        var userName = User.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(userName))
        {
            var cutoff = VietnamTime.Now.AddDays(-trustedDays);
            var devices = await _db.LoginAuditLogs
                .Where(x => x.UserName == userName && x.IsSuccess && x.CreatedAt >= cutoff
                       && (x.Outcome == "SUCCESS_MFA" || x.Outcome == "SUCCESS_MFA_TRUSTED_DEVICE"))
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new RecentDeviceModel { IpAddress = x.IpAddress, UserAgent = x.UserAgent, CreatedAt = x.CreatedAt })
                .ToListAsync();

            var uniqueDevices = devices
                .GroupBy(x => new { x.IpAddress, x.UserAgent })
                .Select(g => g.First())
                .ToList();

            ViewBag.RecentDevices = uniqueDevices;
        }

        return View();
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeCurrentTrustedDevice(string? returnUrl = null)
    {
        Response.Cookies.Delete(TrustedDeviceCookieName, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = !_env.IsDevelopment()
        });
        await WriteLoginAuditAsync(User.Identity?.Name, null, true, "TRUSTED_DEVICE_REVOKE_CURRENT", "User revoked current trusted device");
        TempData["Success"] = "Đã thu hồi tin cậy của thiết bị hiện tại. Lần đăng nhập tới sẽ yêu cầu captcha.";
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(TrustedDevices));
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeAllTrustedDevices(string? returnUrl = null)
    {
        var userName = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userName))
            return RedirectToAction(nameof(Login));

        var user = await _db.AppUsers.FirstOrDefaultAsync(x => x.UserName == userName && x.IsActive);
        if (user == null)
            return RedirectToAction(nameof(Login));

        user.TrustedDeviceRevokedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Also clear current browser token immediately.
        Response.Cookies.Delete(TrustedDeviceCookieName, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = !_env.IsDevelopment()
        });

        await WriteLoginAuditAsync(user.UserName, user.UserId, true, "TRUSTED_DEVICE_REVOKE_ALL", "User revoked all trusted devices");
        TempData["Success"] = "Đã thu hồi toàn bộ thiết bị tin cậy. Các lần đăng nhập kế tiếp sẽ yêu cầu captcha.";
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(TrustedDevices));
    }

    private async Task SignInWithClaimsAsync(AppUser user, string roleName, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new("FullName", user.FullName),
            new(ClaimTypes.Role, roleName),
        };

        if (user.WarehouseId.HasValue)
            claims.Add(new Claim("WarehouseId", user.WarehouseId.Value.ToString()));

        var ownerScopeIds = await _db.AppUserOwnerScopes
            .Where(x => x.UserId == user.UserId && x.IsActive)
            .Select(x => x.OwnerPartnerId)
            .ToListAsync();
        foreach (var ownerId in ownerScopeIds.Distinct())
            claims.Add(new Claim(TenantClaimTypes.OwnerPartnerId, ownerId.ToString()));

        var permCodes = await _db.RolePermissions
            .Where(rp => rp.RoleId == user.RoleId)
            .Join(_db.Permissions, rp => rp.PermissionId, p => p.PermissionId, (rp, p) => p.Code)
            .ToListAsync();
        foreach (var code in permCodes.Distinct(StringComparer.Ordinal))
            claims.Add(new Claim(PermissionClaimTypes.Permission, code));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = rememberMe, ExpiresUtc = new DateTimeOffset(VietnamTime.Now).AddHours(8) });
    }

    private static bool IsStrongPassword(string password)
        => SecurityHelpers.IsStrongPassword(password);

    private bool IsFirstAdminBootstrapAllowed(string? bootstrapToken)
    {
        if (_env.IsDevelopment())
            return true;

        if (!string.Equals(_config["System:AllowFirstAdminBootstrap"], "true", StringComparison.OrdinalIgnoreCase))
            return false;

        var expectedToken = _config["System:FirstAdminBootstrapToken"];
        if (string.IsNullOrWhiteSpace(expectedToken))
            expectedToken = Environment.GetEnvironmentVariable("WMS_FIRST_ADMIN_BOOTSTRAP_TOKEN");

        if (string.IsNullOrWhiteSpace(expectedToken) || string.IsNullOrWhiteSpace(bootstrapToken))
            return false;

        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expectedToken.Trim()));
        var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(bootstrapToken.Trim()));
        return CryptographicOperations.FixedTimeEquals(expectedHash, providedHash);
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        // First-time setup: if there are no users yet, redirect to SetupAdmin
        if (!await _db.AppUsers.AnyAsync())
        {
            return RedirectToAction(nameof(SetupAdmin), new { returnUrl });
        }

        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpGet]
    public IActionResult AccessHelp()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new AccessHelpRequestViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> AccessHelp(AccessHelpRequestViewModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.CompanyWebsite))
            return RedirectToAction(nameof(AccessHelpSent));

        if (!ModelState.IsValid)
            return View(model);

        var requestCode = GenerateAccessHelpCode();
        while (await _db.LoginHelpRequests.AnyAsync(x => x.RequestCode == requestCode))
            requestCode = GenerateAccessHelpCode();

        _db.LoginHelpRequests.Add(new LoginHelpRequest
        {
            RequestCode = requestCode,
            FullName = TrimTo(model.FullName, 120),
            LoginIdentifier = TrimTo(model.LoginIdentifier, 200),
            ContactPhone = TrimOptionalTo(model.ContactPhone, 40),
            WarehouseOrDepartment = TrimOptionalTo(model.WarehouseOrDepartment, 120),
            Reason = model.Reason,
            Notes = TrimOptionalTo(model.Notes, 1000),
            Status = LoginHelpRequestStatusEnum.New,
            IpAddress = GetRequestIp(),
            UserAgent = GetUserAgent(),
            CreatedAt = VietnamTime.Now
        });

        _db.LoginAuditLogs.Add(new LoginAuditLog
        {
            UserName = TrimOptionalTo(model.LoginIdentifier, 100),
            IsSuccess = true,
            Outcome = "LOGIN_HELP_REQUEST_CREATED",
            Reason = $"request={requestCode}",
            IpAddress = GetRequestIp(),
            UserAgent = GetUserAgent(),
            CreatedAt = VietnamTime.Now
        });

        await _db.SaveChangesAsync();
        TempData["AccessHelpRequestCode"] = requestCode;
        return RedirectToAction(nameof(AccessHelpSent));
    }

    [HttpGet]
    public IActionResult AccessHelpSent()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!await _db.AppUsers.AnyAsync())
        {
            return RedirectToAction(nameof(SetupAdmin), new { returnUrl = model.ReturnUrl });
        }

        var user = await _db.AppUsers
            .Include(u => u.Role)
            .Include(u => u.Warehouse)
            .FirstOrDefaultAsync(u => u.UserName == model.UserName && u.IsActive);

        if (user == null)
        {
            await WriteLoginAuditAsync(model.UserName, null, false, "FAILED_USER_NOT_FOUND", "Unknown user or inactive");
            model.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng!";
            return View(model);
        }

        // ═══ Brute-force lockout check (P0-9: LockoutEnd lưu UTC để khớp comment & TrustedDeviceRevokedAtUtc) ═══
        var nowUtc = DateTime.UtcNow;
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > nowUtc)
        {
            var remaining = (int)Math.Ceiling((user.LockoutEnd.Value - nowUtc).TotalMinutes);
            await WriteLoginAuditAsync(user.UserName, user.UserId, false, "FAILED_LOCKED_OUT", $"Locked for {remaining} minutes");
            model.ErrorMessage = $"Tài khoản tạm khóa do đăng nhập sai quá nhiều lần. Thử lại sau {remaining} phút.";
            return View(model);
        }

        bool isPasswordValid;
        try
        {
            isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);
        }
        catch
        {
            // If password hash is corrupted/invalid format, treat as invalid (no plaintext fallback)
            isPasswordValid = false;
        }

        if (!isPasswordValid)
        {
            // ═══ Increment failed login count ═══
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15); // P0-9: lưu UTC để khớp với check ở line 388
                await _db.SaveChangesAsync();
                await WriteLoginAuditAsync(user.UserName, user.UserId, false, "FAILED_BAD_PASSWORD_LOCKED", "5 consecutive failures");
                model.ErrorMessage = "Đăng nhập sai 5 lần liên tiếp. Tài khoản bị khóa 15 phút.";
                return View(model);
            }
            await _db.SaveChangesAsync();
            await WriteLoginAuditAsync(user.UserName, user.UserId, false, "FAILED_BAD_PASSWORD", $"Remaining tries: {5 - user.FailedLoginCount}");
            model.ErrorMessage = $"Tên đăng nhập hoặc mật khẩu không đúng! (Còn {5 - user.FailedLoginCount} lần thử)";
            return View(model);
        }

        // ═══ Reset lockout on successful login ═══
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;

        var roleName = user.Role?.RoleName ?? "Viewer";
        if (!string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase) && !user.WarehouseId.HasValue)
        {
            await WriteLoginAuditAsync(user.UserName, user.UserId, false, "FAILED_NO_WAREHOUSE", "User has no warehouse assignment");
            model.ErrorMessage = "Tài khoản chưa được gán kho làm việc. Vui lòng liên hệ quản trị viên.";
            return View(model);
        }

        if (IsLocalVerificationBypassAllowed(user))
        {
            await SignInWithClaimsAsync(user, roleName, model.RememberMe);
            user.LastLoginAt = VietnamTime.Now;
            await _db.SaveChangesAsync();
            await WriteLoginAuditAsync(user.UserName, user.UserId, true, "SUCCESS_LOCAL_VERIFICATION_BYPASS", "Development loopback visual/load verification");

            return !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
                ? Redirect(model.ReturnUrl)
                : RedirectToAction("Index", "Home");
        }

        var requiresMfa = IsElevatedRole(roleName);
        if (requiresMfa)
        {
            if (IsTrustedDevice(user, roleName))
            {
                await SignInWithClaimsAsync(user, roleName, model.RememberMe);
                user.LastLoginAt = VietnamTime.Now;
                await _db.SaveChangesAsync();
                await WriteLoginAuditAsync(user.UserName, user.UserId, true, "SUCCESS_MFA_TRUSTED_DEVICE", "Password + trusted device");

                return !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
                    ? Redirect(model.ReturnUrl)
                    : RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                await WriteLoginAuditAsync(user.UserName, user.UserId, false, "FAILED_MFA_NO_EMAIL", "Admin/Manager missing email");
                model.ErrorMessage = "Tài khoản quản trị chưa có email để nhận mã captcha. Vui lòng liên hệ quản trị hệ thống.";
                return View(model);
            }

            var sixDigitCaptchaCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var challenge = new MfaLoginChallenge
            {
                UserId = user.UserId,
                UserName = user.UserName,
                CodeHash = HashCode(sixDigitCaptchaCode),
                ExpiresAt = VietnamTime.Now.AddMinutes(5),
                IsUsed = false,
                RememberMe = model.RememberMe,
                FailedAttemptCount = 0,
                IpAddress = GetRequestIp(),
                UserAgent = GetUserAgent(),
                CreatedAt = VietnamTime.Now
            };
            _db.MfaLoginChallenges.Add(challenge);
            await _db.SaveChangesAsync(); // Also persists FailedLoginCount=0 and LockoutEnd=null from above

            try
            {
                await SendSixDigitCaptchaMailAsync(user.Email, sixDigitCaptchaCode);
            }
            catch
            {
                await WriteLoginAuditAsync(user.UserName, user.UserId, false, "FAILED_MFA_SEND_ERROR", "SMTP send failed");
                model.ErrorMessage = "Không thể gửi mã captcha xác thực. Vui lòng kiểm tra cấu hình SMTP hoặc liên hệ quản trị.";
                return View(model);
            }

            await WriteLoginAuditAsync(user.UserName, user.UserId, false, "MFA_CHALLENGE_CREATED", "Password verified, waiting captcha code");
            return RedirectToAction(nameof(VerifyMfa), new { challengeId = challenge.MfaLoginChallengeId, returnUrl = model.ReturnUrl });
        }

        await SignInWithClaimsAsync(user, roleName, model.RememberMe);

        user.LastLoginAt = VietnamTime.Now;
        await _db.SaveChangesAsync();
        await WriteLoginAuditAsync(user.UserName, user.UserId, true, "SUCCESS", "Password login");

        return !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
            ? Redirect(model.ReturnUrl)
            : RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> VerifyMfa(int challengeId, string? returnUrl = null)
    {
        var challenge = await _db.MfaLoginChallenges
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.MfaLoginChallengeId == challengeId);
        if (challenge == null || challenge.IsUsed || challenge.ExpiresAt <= VietnamTime.Now)
        {
            TempData["Error"] = "Phiên mã captcha không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.";
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        var user = await _db.AppUsers.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == challenge.UserId && x.IsActive);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
        {
            TempData["Error"] = "Không thể xác thực mã captcha cho tài khoản này.";
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        var vm = new VerifyMfaViewModel
        {
            ChallengeId = challenge.MfaLoginChallengeId,
            UserName = challenge.UserName,
            MaskedEmail = MaskEmail(user.Email),
            ReturnUrl = returnUrl
        };

        if (TempData["ErrorMessage"] != null)
        {
            vm.ErrorMessage = TempData["ErrorMessage"]?.ToString();
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyMfa(VerifyMfaViewModel model)
    {
        var challenge = await _db.MfaLoginChallenges
            .FirstOrDefaultAsync(x => x.MfaLoginChallengeId == model.ChallengeId);
        if (challenge == null || challenge.IsUsed || challenge.ExpiresAt <= VietnamTime.Now)
        {
            TempData["Error"] = "Phiên mã captcha không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.";
            return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });
        }

        var user = await _db.AppUsers
            .Include(u => u.Role)
            .Include(u => u.Warehouse)
            .FirstOrDefaultAsync(u => u.UserId == challenge.UserId && u.IsActive);
        if (user == null)
        {
            TempData["Error"] = "Tài khoản không hợp lệ.";
            return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });
        }

        if (challenge.FailedAttemptCount >= 5)
        {
            challenge.IsUsed = true;
            await _db.SaveChangesAsync();
            await WriteLoginAuditAsync(user.UserName, user.UserId, false, "FAILED_MFA_MAX_ATTEMPTS", "Captcha max attempts exceeded");
            TempData["Error"] = "Bạn đã nhập sai mã captcha quá nhiều lần. Vui lòng đăng nhập lại.";
            return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });
        }

        // R3-4: dùng FixedTimeEquals để chống timing attack khi so sánh hash captcha (giống pattern bootstrap token L349).
        var captchaInput = (model.CaptchaCode ?? "").Trim();
        var captchaOk = !string.IsNullOrEmpty(captchaInput)
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(HashCode(captchaInput)),
                Encoding.UTF8.GetBytes(challenge.CodeHash ?? ""));
        if (!captchaOk)
        {
            challenge.FailedAttemptCount++;
            await _db.SaveChangesAsync();
            await WriteLoginAuditAsync(user.UserName, user.UserId, false, "FAILED_MFA_BAD_CODE", "Wrong captcha code");
            TempData["ErrorMessage"] = "Mã captcha không đúng.";
            return RedirectToAction(nameof(VerifyMfa), new { challengeId = model.ChallengeId, returnUrl = model.ReturnUrl });
        }

        challenge.IsUsed = true;
        var roleName = user.Role?.RoleName ?? "Viewer";
        await SignInWithClaimsAsync(user, roleName, challenge.RememberMe);
        if (IsElevatedRole(roleName))
            RememberTrustedDevice(user, roleName);
        user.LastLoginAt = VietnamTime.Now;
        await _db.SaveChangesAsync();
        await WriteLoginAuditAsync(user.UserName, user.UserId, true, "SUCCESS_MFA", "Password + captcha");

        return !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
            ? Redirect(model.ReturnUrl)
            : RedirectToAction("Index", "Home");
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await WriteLogoutAuditAsync();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        Response.Cookies.Delete(CookieAuthenticationDefaults.CookiePrefix + CookieAuthenticationDefaults.AuthenticationScheme, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = !_env.IsDevelopment()
        });
        Response.Cookies.Delete(TrustedDeviceCookieName, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = !_env.IsDevelopment()
        });

        return RedirectToAction("Login");
    }

    [HttpGet]
    public async Task<IActionResult> SetupAdmin(string? returnUrl = null, string? bootstrapToken = null)
    {
        if (await _db.AppUsers.AnyAsync())
            return RedirectToAction(nameof(Login), new { returnUrl });

        if (!IsFirstAdminBootstrapAllowed(bootstrapToken))
            return Forbid();

        ViewBag.ReturnUrl = returnUrl;
        ViewBag.BootstrapToken = bootstrapToken;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetupAdmin(string userName, string fullName, string? email, string password, string? returnUrl = null, string? bootstrapToken = null)
    {
        if (await _db.AppUsers.AnyAsync())
            return RedirectToAction(nameof(Login), new { returnUrl });

        if (!IsFirstAdminBootstrapAllowed(bootstrapToken))
            return Forbid();

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(fullName))
        {
            TempData["Error"] = "Vui lòng nhập đầy đủ tài khoản và họ tên.";
            return RedirectToAction(nameof(SetupAdmin), new { returnUrl });
        }

        if (!IsStrongPassword(password))
        {
            TempData["Error"] = "Mật khẩu yếu. Yêu cầu tối thiểu 8 ký tự gồm chữ hoa + chữ thường + số + ký tự đặc biệt.";
            return RedirectToAction(nameof(SetupAdmin), new { returnUrl });
        }

        // Ensure base roles exist
        if (!await _db.AppRoles.AnyAsync())
        {
            _db.AppRoles.AddRange(
                new AppRole { RoleId = 1, RoleName = "Admin", Description = "Quản trị hệ thống" },
                new AppRole { RoleId = 2, RoleName = "Manager", Description = "Quản lý kho" },
                new AppRole { RoleId = 3, RoleName = "Staff", Description = "Nhân viên kho" },
                new AppRole { RoleId = 4, RoleName = "Viewer", Description = "Chỉ xem" }
            );
            await _db.SaveChangesAsync();
        }

        // Create initial admin user
        var admin = new AppUser
        {
            UserName = userName.Trim(),
            FullName = fullName.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            RoleId = 1,
            IsActive = true,
            CreatedAt = VietnamTime.Now
        };

        _db.AppUsers.Add(admin);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Tạo Admin lần đầu thành công. Vui lòng đăng nhập.";
        return RedirectToAction(nameof(Login), new { returnUrl });
    }

    [HttpGet]
    public IActionResult Register()
    {
        // C-03 FIX: Registration is disabled by default. Enable via config.
        if (!string.Equals(_config["Auth:AllowPublicRegistration"], "true", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Đăng ký tài khoản đã bị tắt. Vui lòng liên hệ quản trị viên.";
            return RedirectToAction(nameof(Login));
        }

        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        // C-03 FIX: Block registration if not explicitly enabled
        if (!string.Equals(_config["Auth:AllowPublicRegistration"], "true", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Đăng ký tài khoản đã bị tắt.";
            return RedirectToAction(nameof(Login));
        }

        if (await _db.AppUsers.AnyAsync(u => u.UserName == model.UserName))
        {
            model.ErrorMessage = "Tên đăng nhập đã tồn tại!";
            return View(model);
        }

        // P1-6: chặn trùng email để MFA gửi đúng người, tránh confuse flow.
        var normalizedEmail = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        if (normalizedEmail != null && await _db.AppUsers.AnyAsync(u => u.Email == normalizedEmail))
        {
            model.ErrorMessage = "Email này đã được dùng cho tài khoản khác.";
            return View(model);
        }

        if (!IsStrongPassword(model.Password))
        {
            model.ErrorMessage = "Mật khẩu yếu. Yêu cầu tối thiểu 8 ký tự gồm chữ hoa + chữ thường + số + ký tự đặc biệt.";
            return View(model);
        }

        var newUser = new AppUser
        {
            UserName = model.UserName?.Trim() ?? "",
            FullName = model.FullName?.Trim() ?? "",
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            RoleId = 4, // Default Role 4 = Viewer or lowest privilege
            IsActive = false,
            CreatedAt = VietnamTime.Now
        };

        _db.AppUsers.Add(newUser);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đăng ký thành công! Tài khoản đang chờ Admin kích hoạt.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult DevResetPassword()
    {
        if (!_env.IsDevelopment()) return NotFound();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DevResetPassword(string token, string userName, string newPassword)
    {
        if (!_env.IsDevelopment()) return NotFound();

        var expected = _config["DevResetToken"];
        // R3-5: FixedTimeEquals chống timing attack ngay cả ở dev endpoint.
        var tokenOk = !string.IsNullOrWhiteSpace(expected)
            && !string.IsNullOrWhiteSpace(token)
            && Encoding.UTF8.GetBytes(token.Trim()).Length == Encoding.UTF8.GetBytes(expected.Trim()).Length
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(token.Trim()),
                Encoding.UTF8.GetBytes(expected.Trim()));
        if (!tokenOk)
        {
            TempData["Error"] = "Token không hợp lệ.";
            return RedirectToAction(nameof(DevResetPassword));
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            TempData["Error"] = "Thiếu tài khoản.";
            return RedirectToAction(nameof(DevResetPassword));
        }

        if (!IsStrongPassword(newPassword))
        {
            TempData["Error"] = "Mật khẩu yếu. Yêu cầu tối thiểu 8 ký tự gồm chữ hoa + chữ thường + số + ký tự đặc biệt.";
            return RedirectToAction(nameof(DevResetPassword));
        }

        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.UserName == userName && u.IsActive);
        if (user == null)
        {
            TempData["Error"] = "Không tìm thấy user (hoặc user đã bị khóa).";
            return RedirectToAction(nameof(DevResetPassword));
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã reset mật khẩu cho '{user.UserName}'.";
        return RedirectToAction(nameof(Login));
    }
}
