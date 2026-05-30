using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Models;
using WMS.ViewModels;

namespace WMS.Controllers;

public partial class OperationsController
{
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> WorkflowProfiles(int? warehouseId = null)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        var query = _db.WarehouseWorkflowProfiles
            .AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.OwnerPartner)
            .AsQueryable();
        if (warehouseId.HasValue) query = query.Where(x => x.WarehouseId == warehouseId.Value);

        var model = new WorkflowProfilesViewModel
        {
            WarehouseId = warehouseId,
            Warehouses = await _db.Warehouses.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.WarehouseCode).ToListAsync(),
            Owners = await _db.Partners.AsNoTracking().Where(x => x.IsActive && x.IsThreePlClient).OrderBy(x => x.PartnerCode).ToListAsync(),
            Profiles = await query.OrderBy(x => x.Warehouse.WarehouseCode).ThenBy(x => x.OwnerPartner != null ? x.OwnerPartner.PartnerCode : "").ThenBy(x => x.ModuleKey).ToListAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> SaveWorkflowProfile(
        int? id,
        int warehouseId,
        int? ownerPartnerId,
        string moduleKey,
        string profileName,
        bool requireLocationScan = false,
        bool requireItemScan = false,
        bool requireToteScan = false,
        bool requireSerialScan = false,
        bool requireQc = false,
        bool requireApproval = false,
        bool requirePacking = false,
        bool isActive = true)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        if (string.IsNullOrWhiteSpace(moduleKey) || string.IsNullOrWhiteSpace(profileName))
        {
            TempData["Error"] = "Vui lòng nhập phân hệ và tên cấu hình quy trình.";
            return RedirectToAction(nameof(WorkflowProfiles), new { warehouseId });
        }

        var profile = id.HasValue
            ? await _db.WarehouseWorkflowProfiles.FirstOrDefaultAsync(x => x.WarehouseWorkflowProfileId == id.Value)
            : await _db.WarehouseWorkflowProfiles.FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && x.OwnerPartnerId == ownerPartnerId && x.ModuleKey == moduleKey.Trim());

        if (profile == null)
        {
            profile = new WarehouseWorkflowProfile { WarehouseId = warehouseId };
            _db.WarehouseWorkflowProfiles.Add(profile);
        }
        else if (scopedWh.HasValue && profile.WarehouseId != scopedWh.Value)
        {
            return Forbid();
        }

        profile.WarehouseId = warehouseId;
        profile.OwnerPartnerId = ownerPartnerId;
        profile.ModuleKey = moduleKey.Trim();
        profile.ProfileName = profileName.Trim();
        profile.RequireLocationScan = requireLocationScan;
        profile.RequireItemScan = requireItemScan;
        profile.RequireToteScan = requireToteScan;
        profile.RequireSerialScan = requireSerialScan;
        profile.RequireQc = requireQc;
        profile.RequireApproval = requireApproval;
        profile.RequirePacking = requirePacking;
        profile.IsActive = isActive;
        profile.UpdatedBy = User.Identity?.Name ?? "system";
        profile.UpdatedAt = VietnamNow;

        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = "Đã lưu cấu hình quy trình theo kho/chủ hàng.";
        return RedirectToAction(nameof(WorkflowProfiles), new { warehouseId });
    }
}
