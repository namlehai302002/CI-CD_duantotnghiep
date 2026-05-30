using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;

using Microsoft.EntityFrameworkCore;

using WMS.Data;

using WMS.ViewModels;

using ClosedXML.Excel;

using System.IO;

using WMS.Models;

using System.Data;

using WMS.Authorization;

using WMS.Common;

using WMS.Services;

using Microsoft.Extensions.Logging.Abstractions;

namespace WMS.Controllers;

public partial class ReportsController
{

    // ═══ ENTERPRISE: Scheduled Reports Management ═══
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ScheduledReports(int? warehouseId)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        var query = _db.ScheduledReports.AsNoTracking().Include(r => r.Warehouse).AsQueryable();
        if (warehouseId.HasValue)
            query = query.Where(r => r.WarehouseId == warehouseId.Value || r.WarehouseId == null);

        var reports = await query.OrderBy(r => r.ReportName).ToListAsync();

        ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.WarehouseCode).ToListAsync();
        ViewBag.WarehouseId = warehouseId;
        return View(reports);
    }


    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveScheduledReport(int? scheduledReportId, string reportName, string reportType,
        string schedule, int runAtHour, int? dayOfWeek, int? dayOfMonth,
        string? recipients, int? warehouseId, string? outputFormat, string? description)
    {
        if (string.IsNullOrWhiteSpace(reportName) || string.IsNullOrWhiteSpace(reportType))
        { TempData["Error"] = "Tên báo cáo và loại báo cáo là bắt buộc."; return RedirectToAction(nameof(ScheduledReports)); }

        ScheduledReport report;
        if (scheduledReportId.HasValue && scheduledReportId.Value > 0)
        {
            report = await _db.ScheduledReports.FindAsync(scheduledReportId.Value) ?? throw WmsExceptions.ScheduledReportNotFound();
        }
        else
        {
            report = new ScheduledReport { CreatedBy = User.Identity?.Name };
            _db.ScheduledReports.Add(report);
        }

        report.ReportName = reportName.Trim();
        report.ReportType = reportType.Trim();
        report.Description = description?.Trim();
        report.Schedule = schedule?.Trim() ?? "Daily";
        report.RunAtHour = Math.Clamp(runAtHour, 0, 23);
        report.DayOfWeek = dayOfWeek;
        report.DayOfMonth = dayOfMonth;
        report.Recipients = recipients?.Trim();
        report.WarehouseId = warehouseId;
        report.OutputFormat = outputFormat?.Trim() ?? "Excel";
        report.IsActive = true;

        // Calculate next run
        var now = VietnamNow;
        report.NextRunAt = report.Schedule switch
        {
            "Daily" => now.Date.AddDays(1).AddHours(report.RunAtHour),
            "Weekly" => now.Date.AddDays(((report.DayOfWeek ?? 1) - (int)now.DayOfWeek + 7) % 7).AddHours(report.RunAtHour),
            "Monthly" => new DateTime(now.Year, now.Month, Math.Min(report.DayOfMonth ?? 1, DateTime.DaysInMonth(now.Year, now.Month))).AddMonths(1).AddHours(report.RunAtHour),
            _ => now.Date.AddDays(1).AddHours(report.RunAtHour)
        };

        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = $"Đã lưu lịch báo cáo [{report.ReportName}].";
        return RedirectToAction(nameof(ScheduledReports), new { warehouseId });
    }


    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleScheduledReport(int id)
    {
        var report = await _db.ScheduledReports.FindAsync(id);
        if (report == null) return NotFound();
        report.IsActive = !report.IsActive;
        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = $"Đã {(report.IsActive ? "kích hoạt" : "tạm dừng")} [{report.ReportName}].";
        return RedirectToAction(nameof(ScheduledReports));
    }


    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteScheduledReport(int id)
    {
        var report = await _db.ScheduledReports.FindAsync(id);
        if (report == null) return NotFound();
        _db.ScheduledReports.Remove(report);
        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = "Đã xóa lịch báo cáo.";
        return RedirectToAction(nameof(ScheduledReports));
    }

}
