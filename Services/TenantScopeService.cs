using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public interface ITenantScopeService
{
    Task<IReadOnlyList<int>> GetAllowedOwnerIdsAsync(ClaimsPrincipal? user = null, CancellationToken ct = default);
    Task<bool> HasOwnerScopeAsync(ClaimsPrincipal? user = null, CancellationToken ct = default);
    IQueryable<T> ApplyOwnerScope<T>(IQueryable<T> query, IReadOnlyCollection<int> allowedOwnerIds) where T : class, IOwnerScoped;
    Task EnsureCanAccessOwnerAsync(int? ownerPartnerId, ClaimsPrincipal? user = null, CancellationToken ct = default);
    Task<List<Partner>> GetVisibleOwnersAsync(ClaimsPrincipal? user = null, CancellationToken ct = default);
}

public class TenantScopeService : ITenantScopeService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantScopeService(AppDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IReadOnlyList<int>> GetAllowedOwnerIdsAsync(ClaimsPrincipal? user = null, CancellationToken ct = default)
    {
        user ??= _httpContextAccessor.HttpContext?.User;
        if (user == null || user.Identity?.IsAuthenticated != true)
            return Array.Empty<int>();

        var claimOwnerIds = user.FindAll(TenantClaimTypes.OwnerPartnerId)
            .Select(c => int.TryParse(c.Value, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        if (claimOwnerIds.Count > 0)
            return claimOwnerIds;

        var userIdRaw = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdRaw, out var userId))
            return Array.Empty<int>();

        return await _db.AppUserOwnerScopes.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive)
            .Select(x => x.OwnerPartnerId)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task<bool> HasOwnerScopeAsync(ClaimsPrincipal? user = null, CancellationToken ct = default)
        => (await GetAllowedOwnerIdsAsync(user, ct)).Count > 0;

    public IQueryable<T> ApplyOwnerScope<T>(IQueryable<T> query, IReadOnlyCollection<int> allowedOwnerIds) where T : class, IOwnerScoped
    {
        if (allowedOwnerIds.Count == 0)
            return query;

        return query.Where(x => x.OwnerPartnerId.HasValue && allowedOwnerIds.Contains(x.OwnerPartnerId.Value));
    }

    public async Task EnsureCanAccessOwnerAsync(int? ownerPartnerId, ClaimsPrincipal? user = null, CancellationToken ct = default)
    {
        var allowed = await GetAllowedOwnerIdsAsync(user, ct);
        if (allowed.Count == 0)
            return;

        if (!ownerPartnerId.HasValue || !allowed.Contains(ownerPartnerId.Value))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền truy cập dữ liệu của chủ hàng này.");
        }
    }

    public async Task<List<Partner>> GetVisibleOwnersAsync(ClaimsPrincipal? user = null, CancellationToken ct = default)
    {
        var query = _db.Partners.AsNoTracking()
            .Where(p => p.IsThreePlClient && p.IsActive);
        var allowed = await GetAllowedOwnerIdsAsync(user, ct);
        if (allowed.Count > 0)
            query = query.Where(p => allowed.Contains(p.PartnerId));

        return await query
            .OrderBy(p => p.PartnerCode)
            .ToListAsync(ct);
    }
}
