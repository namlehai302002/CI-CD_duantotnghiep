using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

public static class WmsPermissions
{
    // Inventory / vouchers
    public const string VoucherCreate = "voucher.create";
    public const string VoucherApproveInbound = "voucher.approve.inbound";
    public const string VoucherCancel = "voucher.cancel";
    public const string VoucherPostOutbound = "voucher.post.outbound";
    public const string VoucherReleasePicking = "voucher.release.picking";
    public const string PickTaskReassign = "picktask.reassign";
    public const string VoucherConfirmShipping = "voucher.confirm.shipping";
    public const string VoucherApproveOutbound = "voucher.approve.outbound";
    public const string QcSubmitInspection = "qc.submit.inspection";
    public const string QcResolveHold = "qc.resolve.hold";
    public const string StockCountApprove = "stockcount.approve";
    public const string StockCountUnlock = "stockcount.unlock";

    // Master data
    public const string MasterItemManage = "master.item.manage";
    public const string MasterPartnerManage = "master.partner.manage";
    public const string MasterCategoryManage = "master.category.manage";
    public const string MasterUomManage = "master.uom.manage";

    // Warehouse topology
    public const string WarehouseConfigManage = "warehouse.config.manage";

    // Reports / audit
    public const string ReportView = "report.view";
    // Financial report / cost fields (least-privilege: Admin/Manager only)
    public const string ReportViewFinancial = "report.view.financial";
    public const string AuditTrailView = "audit.view";

    // Users / system
    public const string UserManage = "user.manage";
    public const string DangerOps = "system.danger.ops";
    public const string TenantScopeManage = "tenant.scope.manage";
    public const string ThreePlBillingManage = "billing.3pl.manage";
    public const string MheManage = "mhe.manage";

    /// <summary>
    /// SoD (Segregation of Duties) Matrix — nguyên tắc 4-mắt.
    /// Người tạo (maker) KHÔNG ĐƯỢC thực hiện các action bên dưới (verifier).
    /// Kiểm tra: CreatedBy != currentUser cho mỗi cặp.
    /// </summary>
    public static readonly (string MakerPermission, string VerifierPermission, string VerifierLabel)[] SodMatrix =
    {
        (VoucherCreate,      VoucherApproveInbound,    "Duyệt phiếu nhập"),
        (VoucherCreate,      VoucherReleasePicking,    "Phát hành picking"),
        (VoucherCreate,      VoucherPostOutbound,     "Ghi sổ xuất"),
        (VoucherCreate,      VoucherConfirmShipping,  "Xác nhận giao hàng"),
        (VoucherCreate,      VoucherCancel,           "Hủy phiếu"),
        (VoucherCreate,      QcSubmitInspection,      "QC kiểm tra"),
        (VoucherCreate,      StockCountApprove,        "Phê duyệt kiểm kho"),
    };

    public static readonly string[] All =
    {
        VoucherCreate,
        VoucherApproveInbound,
        VoucherCancel,
        VoucherPostOutbound,
        VoucherReleasePicking,
        VoucherConfirmShipping,
        VoucherApproveOutbound,
        QcSubmitInspection,
        QcResolveHold,
        StockCountApprove,
        StockCountUnlock,
        MasterItemManage,
        MasterPartnerManage,
        MasterCategoryManage,
        MasterUomManage,
        WarehouseConfigManage,
        ReportView,
        AuditTrailView,
        UserManage,
        DangerOps,
        ReportViewFinancial,
        PickTaskReassign,
        TenantScopeManage,
        ThreePlBillingManage,
        MheManage
    };
}

[Table("Permissions")]
public class Permission
{
    [Key]
    public int PermissionId { get; set; }

    [Required, MaxLength(100)]
    public string Code { get; set; } = "";

    [MaxLength(200)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(100)]
    public string? CreatedBy { get; set; }
}

[Table("RolePermissions")]
public class RolePermission
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    [ForeignKey("RoleId")]
    public AppRole? Role { get; set; }

    [ForeignKey("PermissionId")]
    public Permission? Permission { get; set; }
}
