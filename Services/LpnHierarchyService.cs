using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public interface ILpnHierarchyService
{
    Task EnsureCanAssignParentAsync(long licensePlateId, long? parentLpnId, CancellationToken cancellationToken = default);
}

public sealed class LpnHierarchyService : ILpnHierarchyService
{
    private readonly AppDbContext _db;

    public LpnHierarchyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task EnsureCanAssignParentAsync(long licensePlateId, long? parentLpnId, CancellationToken cancellationToken = default)
    {
        if (!parentLpnId.HasValue)
            return;

        if (licensePlateId == parentLpnId.Value)
            throw new BusinessRuleException("Mã kiện cha không được trùng với chính mã kiện hiện tại.", "LPN_SELF_PARENT", nameof(LicensePlate));

        var parentExists = await _db.LicensePlates
            .AsNoTracking()
            .AnyAsync(l => l.LicensePlateId == parentLpnId.Value, cancellationToken);
        if (!parentExists)
            throw new BusinessRuleException("Không tìm thấy mã kiện cha.", "LPN_PARENT_NOT_FOUND", nameof(LicensePlate));

        var visited = new HashSet<long> { licensePlateId };
        var currentParentId = parentLpnId;

        while (currentParentId.HasValue)
        {
            if (!visited.Add(currentParentId.Value))
                throw new BusinessRuleException("Cấu trúc mã kiện bị vòng lặp, không thể gán mã kiện cha.", "LPN_PARENT_LOOP", nameof(LicensePlate));

            currentParentId = await _db.LicensePlates
                .AsNoTracking()
                .Where(l => l.LicensePlateId == currentParentId.Value)
                .Select(l => l.ParentLpnId)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
