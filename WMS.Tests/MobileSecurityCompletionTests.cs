using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WMS.Authorization;
using WMS.Common;
using WMS.Data;
using WMS.Models;
using WMS.Services;

namespace WMS.Tests;

public class MobileSecurityCompletionTests
{
    [Fact]
    public void Mob01_RfWorkflowBuilder_ShouldSelectRoleWarehouseProfileAndBlockUnauthorizedConfigurator()
    {
        var service = new RfWorkflowBuilderService();
        var profile = service.ValidateAndNormalizeProfile(new RfWorkflowProfile
        {
            ProfileCode = "pick-wh1-staff",
            Process = RfWorkflowProcess.Picking,
            RoleName = "Staff",
            WarehouseId = 1,
            Steps = new[]
            {
                new RfWorkflowStep { Sequence = 20, StepType = RfScanStepType.ScanItem, Label = "Scan SKU" },
                new RfWorkflowStep { Sequence = 10, StepType = RfScanStepType.ScanLocation, Label = "Scan slot" }
            }
        }, "Manager");

        var workflow = service.Build(new RfWorkflowBuildRequest
        {
            Process = RfWorkflowProcess.Picking,
            RoleName = "Staff",
            WarehouseId = 1,
            Profiles = new[] { profile }
        });

        Assert.False(workflow.IsDefaultProfile);
        Assert.Equal("PICK-WH1-STAFF", workflow.ProfileCode);
        Assert.Equal(new[] { RfScanStepType.ScanLocation, RfScanStepType.ScanItem, RfScanStepType.Confirm }, workflow.Steps.Select(s => s.StepType));
        Assert.Throws<UnauthorizedAccessException>(() => service.ValidateAndNormalizeProfile(profile, "Viewer"));
    }

    [Fact]
    public void Mob02_OfflineQueuePolicyAndJs_ShouldSupportBackoffConflictDeadletterAndDashboardSnapshot()
    {
        var service = new OfflineQueuePolicyService();
        var now = new DateTime(2026, 5, 11, 10, 0, 0);
        var wait = service.Evaluate(new OfflineQueuedOperation
        {
            OperationId = "op-1",
            Attempts = 2,
            UpdatedAt = now.AddSeconds(-5)
        }, now);
        var dead = service.Evaluate(new OfflineQueuedOperation { OperationId = "op-2", Attempts = 8 }, now);
        var conflict = service.ClassifyServerResponse(409, "server-op-1");

        Assert.Equal(OfflineQueueDecision.WaitBackoff, wait.Decision);
        Assert.Equal(OfflineQueueDecision.DeadLetter, dead.Decision);
        Assert.Equal(OfflineQueueDecision.Conflict, conflict.Decision);

        var js = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "wwwroot", "js", "offline-scan-queue.js"));
        Assert.Contains("MAX_ATTEMPTS", js, StringComparison.Ordinal);
        Assert.Contains("BACKOFF_MS", js, StringComparison.Ordinal);
        Assert.Contains("conflict", js, StringComparison.Ordinal);
        Assert.Contains("deadletter", js, StringComparison.Ordinal);
        Assert.Contains("exportQueueSnapshot", js, StringComparison.Ordinal);
        Assert.Contains("X-WMS-Offline-Attempt", js, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Mob03_DeviceManagement_ShouldRegisterHealthAndRevokeWithAudit()
    {
        await using var db = CreateDb(nameof(Mob03_DeviceManagement_ShouldRegisterHealthAndRevokeWithAudit));
        SeedUser(db, userId: 10, roleId: 3, roleName: "Staff", warehouseId: 1);
        await db.SaveChangesAsync();

        var service = new MobileDeviceManagementService(db);
        var registration = await service.RegisterAsync(new MobileDeviceRegistrationRequest
        {
            UserId = 10,
            DeviceId = "rf-gun-01",
            UserAgent = "Android RF",
            IpAddress = "10.0.0.5",
            IsKioskMode = true,
            PinRequired = true
        });
        await service.RecordHealthAsync(new MobileDeviceHealthReport
        {
            UserId = 10,
            DeviceHash = registration.DeviceHash,
            BatteryPercent = 88,
            IsOnline = true,
            PendingQueueCount = 2
        });
        await service.RevokeUserDevicesAsync(10, "admin", "lost device");

        var user = await db.AppUsers.SingleAsync(u => u.UserId == 10);
        Assert.NotNull(user.TrustedDeviceRevokedAtUtc);
        Assert.Equal(3, await db.LoginAuditLogs.CountAsync());
        Assert.Contains(await db.LoginAuditLogs.Select(x => x.Outcome).ToListAsync(), x => x == "DEVICE_REVOKED");
    }

    [Fact]
    public async Task Mob04_VoicePickingSimulator_ShouldParsePassFailCommands()
    {
        var adapter = new VoicePickingSimulatorAdapter();
        var ok = await adapter.InterpretAsync("confirm qty 3 loc rk-a1 serial sn001");
        var repeat = await adapter.InterpretAsync("nhắc lại");
        var fail = await adapter.InterpretAsync("hello");

        Assert.True(ok.Success);
        Assert.Equal("confirm", ok.Intent);
        Assert.Equal(3, ok.Quantity);
        Assert.Equal("RK-A1", ok.LocationCode);
        Assert.True(repeat.Success);
        Assert.False(fail.Success);
    }

    [Fact]
    public void Mob05_AdvancedBarcodeParser_ShouldSupportGs1PalletSerialRfidAndBulkScan()
    {
        var parser = new AdvancedBarcodeParser();
        var gs1 = parser.Parse("]C101012345678901281725123110LOT-A" + (char)29 + "21SER001");
        var pallet = parser.Parse("PLT:PAL-0001");
        var serial = parser.Parse("SER:SN-0001");
        var rfid = parser.Parse("EPC:3034257BF7194E4000001A85");
        var bulk = parser.ParseBulk("SER:SN1\nPLT:P1\nEPC:E1");

        Assert.True(gs1.Success);
        Assert.Equal("01234567890128", gs1.Gtin);
        Assert.Equal("LOT-A", gs1.LotNumber);
        Assert.Equal("SER001", gs1.SerialNumber);
        Assert.Equal(new DateTime(2025, 12, 31), gs1.ExpiryDate);
        Assert.Equal("PAL-0001", pallet.PalletCode);
        Assert.Equal("SN-0001", serial.SerialNumber);
        Assert.Equal("3034257BF7194E4000001A85", rfid.RfidEpc);
        Assert.Equal(3, bulk.Count);

        var js = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "wwwroot", "js", "mobile-scanner.js"));
        Assert.Contains("parseBarcode", js, StringComparison.Ordinal);
        Assert.Contains("parseBulk", js, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sec01_ExternalIdentityMapper_ShouldMapOidcSamlClaimsToRoleWarehouseOwnerAndPermissions()
    {
        await using var db = CreateDb(nameof(Sec01_ExternalIdentityMapper_ShouldMapOidcSamlClaimsToRoleWarehouseOwnerAndPermissions));
        SeedUser(db, userId: 20, roleId: 2, roleName: "Manager", warehouseId: 1, email: "manager@wms.local");
        db.Permissions.Add(new Permission { PermissionId = 1, Code = WmsPermissions.ReportView });
        db.RolePermissions.Add(new RolePermission { RoleId = 2, PermissionId = 1 });
        db.Partners.Add(new Partner { PartnerId = 100, PartnerCode = "OWN100", PartnerName = "Owner 100", IsThreePlClient = true, IsActive = true });
        db.AppUserOwnerScopes.Add(new AppUserOwnerScope { UserId = 20, OwnerPartnerId = 100, CreatedBy = "seed" });
        await db.SaveChangesAsync();

        var service = new ExternalIdentityMappingService(db);
        var mapped = await service.MapAsync(new ExternalIdentityLoginRequest
        {
            Provider = "OIDC",
            Claims = new Dictionary<string, string>
            {
                ["email"] = "manager@wms.local",
                ["role"] = "Manager",
                ["warehouse_id"] = "1",
                ["owner_ids"] = "100,999"
            }
        });

        Assert.True(mapped.Success);
        Assert.NotNull(mapped.Principal);
        Assert.Equal("Manager", mapped.Principal!.FindFirstValue(ClaimTypes.Role));
        Assert.Equal("1", mapped.Principal.FindFirstValue("WarehouseId"));
        Assert.Equal("100", mapped.Principal.FindFirstValue(TenantClaimTypes.OwnerPartnerId));
        Assert.Contains(mapped.Principal.Claims, c => c.Type == PermissionClaimTypes.Permission && c.Value == WmsPermissions.ReportView);
        Assert.True(await db.LoginAuditLogs.AnyAsync(x => x.Outcome == "SSO_MAPPED"));
    }

    [Fact]
    public async Task Sec02_MfaLockoutAndPasswordReset_ShouldLockResetAndNeverStorePlainPassword()
    {
        await using var db = CreateDb(nameof(Sec02_MfaLockoutAndPasswordReset_ShouldLockResetAndNeverStorePlainPassword));
        SeedUser(db, userId: 30, roleId: 3, roleName: "Staff", warehouseId: 1);
        await db.SaveChangesAsync();

        var service = new ProductionMfaLockoutService(db);
        var locked = false;
        for (var i = 0; i < 5; i++)
            locked = await service.RegisterFailedAttemptAsync("user30");

        Assert.True(locked);
        Assert.NotNull((await db.AppUsers.SingleAsync(u => u.UserId == 30)).LockoutEnd);

        await service.ResetPasswordByAdminAsync(30, "NewPass@123", "admin");
        var user = await db.AppUsers.SingleAsync(u => u.UserId == 30);
        Assert.Null(user.LockoutEnd);
        Assert.Equal(0, user.FailedLoginCount);
        Assert.NotEqual("NewPass@123", user.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPass@123", user.PasswordHash));
        Assert.True(await db.LoginAuditLogs.AnyAsync(x => x.Outcome == "PASSWORD_RESET_BY_ADMIN"));
    }

    [Fact]
    public async Task Sec03_SegregationOfDuties_ShouldBlockMakerVerifierAndAudit()
    {
        await using var db = CreateDb(nameof(Sec03_SegregationOfDuties_ShouldBlockMakerVerifierAndAudit));
        var service = new SegregationOfDutiesService(db);

        await Assert.ThrowsAsync<SodViolationException>(() => service.EnforceAsync("maker", "maker", WmsPermissions.VoucherApproveInbound));
        Assert.True(await db.AuditLogs.AnyAsync(x => x.ActionType == "SOD_BLOCK" && x.TableName == "Security"));
    }

    [Fact]
    public async Task Sec04_ScopeAudit_ShouldAllowScopedExportAndDenyForeignWarehouse()
    {
        await using var db = CreateDb(nameof(Sec04_ScopeAudit_ShouldAllowScopedExportAndDenyForeignWarehouse));
        SeedUser(db, userId: 40, roleId: 2, roleName: "Manager", warehouseId: 1);
        db.Partners.Add(new Partner { PartnerId = 100, PartnerCode = "OWN100", PartnerName = "Owner 100", IsThreePlClient = true, IsActive = true });
        db.AppUserOwnerScopes.Add(new AppUserOwnerScope { UserId = 40, OwnerPartnerId = 100, CreatedBy = "seed" });
        await db.SaveChangesAsync();

        var principal = Principal("manager40", "Manager", userId: 40, warehouseId: 1);
        var tenant = new TenantScopeService(db, new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = principal } });
        var service = new SecurityScopeAuditService(tenant, db);

        await service.EnsureScopeAsync(new ScopedOperationRequest
        {
            User = principal,
            WarehouseId = 1,
            OwnerPartnerId = 100,
            OperationName = "ExportStock",
            IsExport = true
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.EnsureScopeAsync(new ScopedOperationRequest
        {
            User = principal,
            WarehouseId = 2,
            OwnerPartnerId = 100,
            OperationName = "ExportStock",
            IsExport = true
        }));

        Assert.True(await db.AuditLogs.AnyAsync(x => x.ActionType == "EXPORT"));
        Assert.True(await db.AuditLogs.AnyAsync(x => x.ActionType == "DENIED"));
    }

    [Fact]
    public async Task Sec05_SecurityEventCenter_ShouldAggregateLoginResetDeviceDeniedAndExportEvents()
    {
        await using var db = CreateDb(nameof(Sec05_SecurityEventCenter_ShouldAggregateLoginResetDeviceDeniedAndExportEvents));
        var now = VietnamTime.Now;
        db.LoginAuditLogs.AddRange(
            new LoginAuditLog { UserName = "u1", IsSuccess = false, Outcome = "FAILED_BAD_PASSWORD", CreatedAt = now },
            new LoginAuditLog { UserName = "u2", IsSuccess = true, Outcome = "PASSWORD_RESET_BY_ADMIN", CreatedAt = now },
            new LoginAuditLog { UserName = "u3", IsSuccess = true, Outcome = "DEVICE_REVOKED", CreatedAt = now });
        db.AuditLogs.AddRange(
            new AuditLog { TableName = "Security", RecordId = "Export", ActionType = "EXPORT", ChangedBy = "u1", ChangedAt = now },
            new AuditLog { TableName = "Security", RecordId = "Scope", ActionType = "DENIED", ChangedBy = "u1", ChangedAt = now });
        await db.SaveChangesAsync();

        var service = new SecurityEventCenterService(db);
        var events = await service.GetRecentEventsAsync(now.AddMinutes(-1), now.AddMinutes(1));

        Assert.Contains(events, e => e.EventType == "FAILED_BAD_PASSWORD");
        Assert.Contains(events, e => e.EventType == "PASSWORD_RESET_BY_ADMIN");
        Assert.Contains(events, e => e.EventType == "DEVICE_REVOKED");
        Assert.Contains(events, e => e.EventType == "EXPORT");
        Assert.Contains(events, e => e.EventType == "DENIED");
    }

    [Fact]
    public async Task Sec06_SecretReadiness_ShouldResolveEnvironmentAndScanWithoutTouchingAppsettings()
    {
        var service = new SecretReadinessService();
        Environment.SetEnvironmentVariable("WMS_TEST_SECRET", "from-env");
        Assert.Equal("from-env", service.ResolveSecret("WMS_TEST_SECRET"));

        var dir = Path.Combine(Path.GetTempPath(), "wms-secret-scan-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "Worker.cs"), "var ApiKey = \"abcdefghijklmnop123456\";");
        await File.WriteAllTextAsync(Path.Combine(dir, "appsettings.json"), "{ \"ApiKey\": \"abcdefghijklmnop123456\" }");

        var result = await service.ScanRepositoryAsync(dir);

        Assert.False(result.IsReady);
        Assert.Single(result.Findings);
        Assert.Equal("Worker.cs", result.Findings[0]);
    }

    [Fact]
    public void TaskFile_ShouldMarkMobileAndSecuritySectionsComplete()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..");
        var task = File.ReadAllText(Path.Combine(root, "ENTERPRISE_WMS_100_PERCENT_TASKS.md"));
        foreach (var code in new[] { "MOB-01", "MOB-02", "MOB-03", "MOB-04", "MOB-05", "SEC-01", "SEC-02", "SEC-03", "SEC-04", "SEC-05", "SEC-06" })
            Assert.Contains($"[x] `{code}`", task, StringComparison.Ordinal);
    }

    private static AppDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name + Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options, new HttpContextAccessor());
    }

    private static void SeedUser(AppDbContext db, int userId, int roleId, string roleName, int? warehouseId, string? email = null)
    {
        db.AppRoles.Add(new AppRole { RoleId = roleId, RoleName = roleName });
        db.AppUsers.Add(new AppUser
        {
            UserId = userId,
            UserName = $"user{userId}",
            FullName = $"User {userId}",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Start@123"),
            RoleId = roleId,
            WarehouseId = warehouseId,
            IsActive = true
        });
    }

    private static ClaimsPrincipal Principal(string name, string role, int userId, int? warehouseId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, name),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        if (warehouseId.HasValue)
            claims.Add(new Claim("WarehouseId", warehouseId.Value.ToString()));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }
}
