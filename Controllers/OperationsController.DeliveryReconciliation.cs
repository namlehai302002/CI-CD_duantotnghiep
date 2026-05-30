using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Services;
using WMS.ViewModels;

namespace WMS.Controllers;

public partial class OperationsController
{
    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> DeliveryReconciliation(int? warehouseId, string? severity, string? issueType, string? search)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
            warehouseId = scopedWh.Value;

        var filter = new DeliveryReconciliationFilter
        {
            WarehouseId = warehouseId,
            Severity = severity,
            IssueType = issueType,
            Search = search
        };
        var model = new DeliveryReconciliationViewModel
        {
            WarehouseId = warehouseId,
            Severity = severity,
            IssueType = issueType,
            Search = search,
            Warehouses = await GetVisibleWarehousesAsync(),
            Rows = await _shippingReconciliationService.BuildAsync(filter)
        };
        return View(model);
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> ExportDeliveryReconciliationCsv(int? warehouseId, string? severity, string? issueType, string? search)
    {
        var rows = await BuildDeliveryReconciliationRowsAsync(warehouseId, severity, issueType, search);
        var sb = new StringBuilder();
        sb.AppendLine("MucDo,LoaiLech,Kho,Phieu,Kien,Chuyen,VanDon,TomTat,KhuyenNghi");
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                Csv(row.SeverityLabel),
                Csv(row.IssueLabel),
                Csv(row.WarehouseName),
                Csv(row.VoucherCode),
                Csv(row.PackageCode),
                Csv(row.LoadCode),
                Csv(row.TrackingNumber),
                Csv(row.Summary),
                Csv(row.Recommendation)
            }));
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"delivery_reconciliation_{VietnamNow:yyyyMMddHHmmss}.csv");
    }

    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> ExportDeliveryReconciliationExcel(int? warehouseId, string? severity, string? issueType, string? search)
    {
        var rows = await BuildDeliveryReconciliationRowsAsync(warehouseId, severity, issueType, search);
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("DoiSoatGiaoHang");
        var headers = new[]
        {
            "Mức độ", "Loại lệch", "Kho", "Phiếu", "Kiện", "Chuyến xe", "Vận đơn", "Tóm tắt", "Khuyến nghị", "Đường dẫn xử lý"
        };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var r = 2;
        foreach (var row in rows)
        {
            ws.Cell(r, 1).Value = row.SeverityLabel;
            ws.Cell(r, 2).Value = row.IssueLabel;
            ws.Cell(r, 3).Value = row.WarehouseName;
            ws.Cell(r, 4).Value = row.VoucherCode ?? "";
            ws.Cell(r, 5).Value = row.PackageCode ?? "";
            ws.Cell(r, 6).Value = row.LoadCode ?? "";
            ws.Cell(r, 7).Value = row.TrackingNumber ?? "";
            ws.Cell(r, 8).Value = row.Summary;
            ws.Cell(r, 9).Value = row.Recommendation;
            ws.Cell(r, 10).Value = row.ActionUrl;
            r++;
        }

        ws.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"delivery_reconciliation_{VietnamNow:yyyyMMddHHmmss}.xlsx");
    }

    private async Task<List<DeliveryReconciliationRow>> BuildDeliveryReconciliationRowsAsync(int? warehouseId, string? severity, string? issueType, string? search)
    {
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
            warehouseId = scopedWh.Value;

        return await _shippingReconciliationService.BuildAsync(new DeliveryReconciliationFilter
        {
            WarehouseId = warehouseId,
            Severity = severity,
            IssueType = issueType,
            Search = search
        });
    }
}
