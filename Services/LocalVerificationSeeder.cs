using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public sealed class LocalVerificationSeeder
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LocalVerificationSeeder> _logger;

    public LocalVerificationSeeder(AppDbContext db, IConfiguration config, IWebHostEnvironment env, ILogger<LocalVerificationSeeder> logger)
    {
        _db = db;
        _config = config;
        _env = env;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!_env.IsDevelopment()) return;
        if (!_config.GetValue<bool>("LocalVerification:Enabled")) return;
        if (!_config.GetValue<bool>("LocalVerification:SeedAdmin")) return;

        var userName = _config["LocalVerification:UserName"]?.Trim();
        var password = _config["LocalVerification:Password"];
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            return;

        if (!SecurityHelpers.IsStrongPassword(password))
        {
            _logger.LogWarning("LOCAL_VERIFICATION_SEED_SKIPPED_WEAK_PASSWORD");
            return;
        }

        var adminRole = await _db.AppRoles.FirstOrDefaultAsync(r => r.RoleName == "Admin", ct);
        if (adminRole == null)
        {
            adminRole = new AppRole { RoleName = "Admin", Description = "Quan tri he thong" };
            _db.AppRoles.Add(adminRole);
            await _db.SaveChangesAsync(ct);
        }

        foreach (var code in WmsPermissions.All)
        {
            if (!await _db.Permissions.AnyAsync(p => p.Code == code, ct))
            {
                _db.Permissions.Add(new Permission
                {
                    Code = code,
                    Description = code,
                    CreatedAt = VietnamTime.Now,
                    CreatedBy = "local-verification"
                });
            }
        }
        await _db.SaveChangesAsync(ct);

        var permissionIds = await _db.Permissions
            .Where(p => WmsPermissions.All.Contains(p.Code))
            .Select(p => p.PermissionId)
            .ToListAsync(ct);

        foreach (var permissionId in permissionIds)
        {
            var exists = await _db.RolePermissions.AnyAsync(rp => rp.RoleId == adminRole.RoleId && rp.PermissionId == permissionId, ct);
            if (!exists)
            {
                _db.RolePermissions.Add(new RolePermission
                {
                    RoleId = adminRole.RoleId,
                    PermissionId = permissionId,
                    CreatedAt = VietnamTime.Now
                });
            }
        }

        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.UserName == userName, ct);
        if (user == null)
        {
            user = new AppUser
            {
                UserName = userName,
                CreatedAt = VietnamTime.Now
            };
            _db.AppUsers.Add(user);
        }

        user.FullName = _config["LocalVerification:FullName"]?.Trim() ?? "Local Verification Admin";
        user.Email = _config["LocalVerification:Email"]?.Trim();
        user.RoleId = adminRole.RoleId;
        user.WarehouseId = null;
        user.IsActive = true;
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        user.TrustedDeviceRevokedAtUtc = null;

        var shouldHash = true;
        try
        {
            shouldHash = string.IsNullOrWhiteSpace(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }
        catch
        {
            shouldHash = true;
        }

        if (shouldHash)
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("LOCAL_VERIFICATION_ADMIN_READY user={UserName}", userName);
    }
}
