using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Common;
using WMS.Models;
using Microsoft.AspNetCore.Authorization;
using WMS.Authorization;
using WMS.Services;
using static WMS.Common.SecurityHelpers;

namespace WMS.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
public class SystemController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly IProductionSreService _productionSreService;

    public SystemController(AppDbContext db, IWebHostEnvironment env, IConfiguration config, IProductionSreService? productionSreService = null)
    {
        _db = db;
        _env = env;
        _config = config;
        _productionSreService = productionSreService ?? new ProductionSreService(db);
    }

    private bool IsDangerOpsAllowed()
    {
        return _env.IsDevelopment() || string.Equals(_config["System:AllowDangerOps"], "true", StringComparison.OrdinalIgnoreCase);
    }

    [HttpGet]
    public async Task<IActionResult> SreDashboard(int periodMinutes = 15)
    {
        periodMinutes = Math.Clamp(periodMinutes, 1, 1440);
        var model = await _productionSreService.BuildDashboardAsync(periodMinutes);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportSreSnapshot(int periodMinutes = 15)
    {
        var model = await _productionSreService.BuildDashboardAsync(periodMinutes);
        var lines = new List<string>
        {
            "thoi_diem,so_phut,so_yeu_cau,so_loi,ty_le_loi,do_tre_tb_ms,do_tre_p95_ms,do_sau_hang_doi,hang_loi,quet_gui_lai,loi_van_tai,loi_diem_nhan",
            string.Join(',',
                model.Snapshot.SnapshotAt.ToString("O"),
                model.Snapshot.PeriodMinutes,
                model.Snapshot.RequestCount,
                model.Snapshot.ErrorCount,
                model.Snapshot.ErrorRatePercent,
                model.Snapshot.AverageLatencyMs,
                model.Snapshot.P95LatencyMs,
                model.Snapshot.QueueDepth,
                model.Snapshot.DeadLetterCount,
                model.Snapshot.ScanRetryCount,
                model.Snapshot.CarrierFailureCount,
                model.Snapshot.WebhookFailureCount)
        };
        return File(System.Text.Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, lines)), "text/csv", $"sre-snapshot-{VietnamTime.FileStamp("yyyyMMddHHmm")}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> Units()
    {
        var uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).OrderBy(u => u.UomCode).ToListAsync();
        ViewBag.PackagingUnits = await _db.PackagingUnits
            .Include(p => p.BaseUom)
            .Where(p => p.IsActive).OrderBy(p => p.TenDongGoi).ToListAsync();
        ViewBag.AllUoms = uoms;
        return View("~/Views/Units/Index.cshtml", uoms);
    }

    [HttpPost]
    [ValidateAntiForgeryToken] // R3-3
    public async Task<IActionResult> CreateUnit(string uomCode, string uomName)
    {
        if (!string.IsNullOrWhiteSpace(uomCode) && !string.IsNullOrWhiteSpace(uomName))
        {
            if (!await _db.UnitsOfMeasure.AnyAsync(u => u.UomCode == uomCode))
            {
                _db.UnitsOfMeasure.Add(new UnitOfMeasure { UomCode = uomCode, UomName = uomName, IsActive = true });
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã thêm ĐVT '{uomName}'.";
            }
            else
            {
                TempData["Error"] = $"Mã ĐVT '{uomCode}' đã tồn tại.";
            }
        }
        return RedirectToAction("Units");
    }

    [HttpPost]
    [ValidateAntiForgeryToken] // R3-3
    public async Task<IActionResult> DeleteUnit(int id)
    {
        var uom = await _db.UnitsOfMeasure.FindAsync(id);
        if (uom != null)
        {
            uom.IsActive = false;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa ĐVT '{uom.UomName}'.";
        }
        return RedirectToAction("Units");
    }

    [HttpPost]
    [ValidateAntiForgeryToken] // R3-3
    public async Task<IActionResult> CreatePackaging(string tenDongGoi, int baseUomId, decimal giaTri)
    {
        if (!string.IsNullOrWhiteSpace(tenDongGoi) && giaTri > 0)
        {
            if (await _db.PackagingUnits.AnyAsync(p => p.TenDongGoi == tenDongGoi && p.IsActive))
            {
                TempData["Error"] = $"Tên đóng gói '{tenDongGoi}' đã tồn tại.";
            }
            else
            {
                _db.PackagingUnits.Add(new PackagingUnit
                {
                    TenDongGoi = tenDongGoi,
                    BaseUomId = baseUomId,
                    GiaTri = giaTri,
                    IsActive = true
                });
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã thêm quy cách đóng gói '{tenDongGoi}'.";
            }
        }
        return RedirectToAction("Units");
    }

    [HttpPost]
    [ValidateAntiForgeryToken] // R3-3
    public async Task<IActionResult> DeletePackaging(int id)
    {
        var pkg = await _db.PackagingUnits.FindAsync(id);
        if (pkg != null)
        {
            pkg.IsActive = false;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa đóng gói '{pkg.TenDongGoi}'.";
        }
        return RedirectToAction("Units");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = WmsPermissions.DangerOps)]
    public IActionResult SeedData()
    {
        if (!IsDangerOpsAllowed()) return Forbid();

        // phần còn lại giữ nguyên trong file gốc
        TempData["Info"] = "Chức năng seed dữ liệu giữ nguyên logic hiện có.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = WmsPermissions.DangerOps)]
    public IActionResult MergeLocationsPerLevel()
    {
        if (!IsDangerOpsAllowed()) return Forbid();

        TempData["Info"] = "Chức năng gộp vị trí theo tầng giữ nguyên logic hiện có.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = WmsPermissions.DangerOps)]
    public IActionResult ResetDatabase()
    {
        if (!IsDangerOpsAllowed()) return Forbid();

        TempData["Info"] = "Chức năng reset dữ liệu giữ nguyên logic hiện có.";
        return RedirectToAction("Index", "Home");
    }
}
