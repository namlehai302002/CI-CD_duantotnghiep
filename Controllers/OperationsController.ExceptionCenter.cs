using System;

using System.Collections.Generic;

using System.Data;

using System.IO;

using System.Linq;

using System.Text.Json;

using System.Threading.Tasks;

using System.Linq.Expressions;

using ClosedXML.Excel;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using WMS.Common;

using WMS.Data;

using WMS.Models;

using WMS.Services;

using WMS.ViewModels;

namespace WMS.Controllers;

public partial class OperationsController
{

    private async Task SyncExceptionCasesAsync(List<OperationExceptionRow> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }
        DateTime now = VietnamNow;
        List<string> keys = rows.Select((OperationExceptionRow r) => r.ExceptionKey).Distinct().ToList();
        Dictionary<string, OperationExceptionCase> existingCases = await _db.OperationExceptionCases.Where((OperationExceptionCase c) => keys.Contains(c.ExceptionKey)).ToDictionaryAsync((OperationExceptionCase c) => c.ExceptionKey);
        foreach (OperationExceptionRow row in rows)
        {
            if (!existingCases.TryGetValue(row.ExceptionKey, out var exceptionCase))
            {
                exceptionCase = new OperationExceptionCase
                {
                    ExceptionKey = row.ExceptionKey,
                    CategoryKey = row.CategoryKey,
                    CategoryLabel = row.CategoryLabel,
                    WarehouseId = row.WarehouseId,
                    ReferenceCode = row.ReferenceCode,
                    SecondaryReference = row.SecondaryReference,
                    Status = OperationExceptionStatusEnum.Open,
                    FirstDetectedAt = now,
                    LastDetectedAt = now,
                    UpdatedAt = now
                };
                _db.OperationExceptionCases.Add(exceptionCase);
                existingCases[row.ExceptionKey] = exceptionCase;
                continue;
            }
            exceptionCase.CategoryLabel = row.CategoryLabel;
            exceptionCase.ReferenceCode = row.ReferenceCode;
            exceptionCase.SecondaryReference = row.SecondaryReference;
            exceptionCase.LastDetectedAt = now;
            exceptionCase.UpdatedAt = now;
            if (exceptionCase.Status == OperationExceptionStatusEnum.Resolved)
            {
                exceptionCase.Status = OperationExceptionStatusEnum.Open;
                exceptionCase.ResolvedBy = null;
                exceptionCase.ResolvedAt = null;
                exceptionCase.ResolutionNote = null;
                exceptionCase.AcknowledgedBy = null;
                exceptionCase.AcknowledgedAt = null;
            }
            exceptionCase = null;
        }
        await _unitOfWork.SaveChangesAsync();
    }


    private async Task DecorateExceptionRowsAsync(List<OperationExceptionRow> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }
        List<string> keys = rows.Select((OperationExceptionRow r) => r.ExceptionKey).Distinct().ToList();
        Dictionary<string, OperationExceptionCase> cases = await (from c in _db.OperationExceptionCases.AsNoTracking()
                                                                  where keys.Contains(c.ExceptionKey)
                                                                  select c).ToDictionaryAsync((OperationExceptionCase c) => c.ExceptionKey);
        foreach (OperationExceptionRow row in rows)
        {
            if (cases.TryGetValue(row.ExceptionKey, out var exceptionCase))
            {
                (string Key, string Label) mappedStatus = MapCaseStatus(exceptionCase.Status);
                row.CaseStatusKey = mappedStatus.Key;
                row.CaseStatusLabel = mappedStatus.Label;
                row.AssignedTo = exceptionCase.AssignedTo;
                row.AcknowledgedAt = exceptionCase.AcknowledgedAt;
                row.ResolvedAt = exceptionCase.ResolvedAt;
                row.ResolvedBy = exceptionCase.ResolvedBy;
                row.ResolutionNote = exceptionCase.ResolutionNote;
                exceptionCase = null;
            }
        }
    }


    private async Task<OperationExceptionCase?> GetScopedExceptionCaseAsync(string exceptionKey)
    {
        OperationExceptionCase? exceptionCase = await _db.OperationExceptionCases.FirstOrDefaultAsync((OperationExceptionCase c) => c.ExceptionKey == exceptionKey);
        if (exceptionCase == null)
        {
            return null;
        }
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && exceptionCase.WarehouseId != scopedWh.Value)
        {
            return null;
        }
        return exceptionCase;
    }


    private async Task<List<OperationExceptionRow>> BuildExceptionCenterRowsAsync(int? warehouseId, string? category, string? severity, string? search)
    {
        DateTime now = VietnamNow;
        List<OperationExceptionRow> rows = new List<OperationExceptionRow>();
        List<Voucher> inboundVouchers = await (from v in _db.Vouchers.AsNoTracking().Include((Voucher v) => v.Warehouse).Include((Voucher v) => v.Partner)
                                               where !v.IsCancelled && !v.IsPosted && (v.VoucherType == VoucherTypeEnum.NhapKho || v.VoucherType == VoucherTypeEnum.KhachTra || v.VoucherType == VoucherTypeEnum.NhapThanhPham) && (!((int?)warehouseId).HasValue || v.WarehouseId == ((int?)warehouseId).Value)
                                               select v).ToListAsync();
        foreach (Voucher voucher in inboundVouchers)
        {
            InboundStatusEnum inboundStatus = voucher.InboundStatus;
            if (inboundStatus - 2 <= InboundStatusEnum.Draft)
            {
                List<string> missingPlanning = new List<string>();
                if (string.IsNullOrWhiteSpace(voucher.AsnCode))
                {
                    missingPlanning.Add("thiếu mã lịch nhận hàng");
                }
                if (!voucher.ExpectedArrivalAt.HasValue)
                {
                    missingPlanning.Add("thiếu giờ xe đến");
                }
                if (!voucher.DockAppointmentStart.HasValue || !voucher.DockAppointmentEnd.HasValue)
                {
                    missingPlanning.Add("thiếu khung giờ nhận hàng");
                }
                if (string.IsNullOrWhiteSpace(voucher.DockDoor))
                {
                    missingPlanning.Add("chưa gắn cửa nhận hàng");
                }
                if (missingPlanning.Count > 0)
                {
                    rows.Add(new OperationExceptionRow
                    {
                        CategoryKey = "inbound_planning_gap",
                        CategoryLabel = "Phiếu nhập thiếu kế hoạch",
                        SeverityKey = "high",
                        SeverityLabel = "Cao",
                        WarehouseId = voucher.WarehouseId,
                        WarehouseName = (voucher.Warehouse?.WarehouseName ?? $"Kho {voucher.WarehouseId}"),
                        ReferenceCode = voucher.VoucherCode,
                        SecondaryReference = voucher.AsnCode,
                        Summary = "Phiếu nhập chưa đủ thông tin lịch xe đến / cửa nhận hàng.",
                        Detail = "Cần bổ sung " + string.Join(", ", missingPlanning) + " trước khi điều độ nhận hàng.",
                        DueAt = voucher.ExpectedArrivalAt,
                        AgeHours = (voucher.ExpectedArrivalAt.HasValue ? new double?(Math.Max(0.0, (now - voucher.ExpectedArrivalAt.GetValueOrDefault()).TotalHours)) : ((double?)null)),
                        ActionUrl = $"/Vouchers/Details/{voucher.VoucherId}",
                        ActionLabel = "Mở phiếu"
                    });
                }
            }
            bool hasValue = voucher.ExpectedArrivalAt.HasValue;
            bool flag = hasValue;
            if (flag)
            {
                inboundStatus = voucher.InboundStatus;
                bool flag2 = inboundStatus - 3 <= InboundStatusEnum.Draft;
                flag = flag2;
            }
            if (flag && voucher.ExpectedArrivalAt.HasValue && voucher.ExpectedArrivalAt.Value < now)
            {
                double ageHours = Math.Max(0.01, (now - voucher.ExpectedArrivalAt.GetValueOrDefault()).TotalHours);
                (string Key, string Label, int Rank) severityInfo = MapSeverity(ageHours);
                rows.Add(new OperationExceptionRow
                {
                    CategoryKey = "inbound_overdue",
                    CategoryLabel = "Phiếu nhập quá giờ",
                    SeverityKey = severityInfo.Key,
                    SeverityLabel = severityInfo.Label,
                    WarehouseId = voucher.WarehouseId,
                    WarehouseName = (voucher.Warehouse?.WarehouseName ?? $"Kho {voucher.WarehouseId}"),
                    ReferenceCode = voucher.VoucherCode,
                    SecondaryReference = voucher.AsnCode,
                    Summary = ((voucher.InboundStatus == InboundStatusEnum.Receiving) ? "Xe đã vào luồng nhận nhưng chưa hoàn tất đúng giờ." : "Xe đến trễ hoặc phiếu chưa được tiếp nhận đúng lịch."),
                    Detail = $"Phiếu đã quá mức dự kiến {ageHours:N1} giờ. Đối tác: {voucher.Partner?.PartnerName ?? "---"}, cửa nhận hàng: {voucher.DockDoor ?? "---"}.",
                    DueAt = voucher.ExpectedArrivalAt,
                    AgeHours = ageHours,
                    ActionUrl = $"/Vouchers/Details/{voucher.VoucherId}",
                    ActionLabel = "Xử lý phiếu nhập"
                });
            }
            if (voucher.DockAppointmentEnd.HasValue && voucher.InboundStatus == InboundStatusEnum.Approved && voucher.DockAppointmentEnd.Value < now)
            {
                double ageHours2 = Math.Max(0.01, (now - voucher.DockAppointmentEnd.Value).TotalHours);
                (string Key, string Label, int Rank) severityInfo2 = MapSeverity(ageHours2);
                rows.Add(new OperationExceptionRow
                {
                    CategoryKey = "dock_missed_window",
                    CategoryLabel = "Lệch khung nhận hàng",
                    SeverityKey = severityInfo2.Key,
                    SeverityLabel = severityInfo2.Label,
                    WarehouseId = voucher.WarehouseId,
                    WarehouseName = (voucher.Warehouse?.WarehouseName ?? $"Kho {voucher.WarehouseId}"),
                    ReferenceCode = voucher.VoucherCode,
                    SecondaryReference = voucher.DockDoor,
                    Summary = "Phiếu nhập đã lỡ khung giờ nhận hàng nhưng chưa chuyển sang bước nhận hàng.",
                    Detail = $"Khung giờ nhận hàng kết thúc lúc {voucher.DockAppointmentEnd:dd/MM HH:mm}, cần điều độ lại hoặc xác nhận thực tế.",
                    DueAt = voucher.DockAppointmentEnd,
                    AgeHours = ageHours2,
                    ActionUrl = $"/Vouchers/Details/{voucher.VoucherId}",
                    ActionLabel = "Điều độ lại"
                });
            }
        }
        List<long> inboundIds = inboundVouchers.Select((Voucher v) => v.VoucherId).ToList();
        if (inboundIds.Count > 0)
        {
            var receivingDetails = await (from d in _db.VoucherDetails.AsNoTracking().Include((VoucherDetail d) => d.Item).Include((VoucherDetail d) => d.Voucher).ThenInclude((Voucher? v) => v!.Warehouse)
                                          where inboundIds.Contains(d.VoucherId) && d.Voucher != null && d.Voucher.InboundStatus == InboundStatusEnum.Receiving
                                          select d).ToListAsync();
            List<long> receivingDetailIds = receivingDetails.Select((VoucherDetail d) => d.VoucherDetailId).ToList();
            Dictionary<long, decimal> completedCrossDockByDetail = receivingDetailIds.Count == 0
                ? new Dictionary<long, decimal>()
                : await (from t in _db.CrossDockTasks.AsNoTracking()
                         where t.InboundVoucherDetailId.HasValue && receivingDetailIds.Contains(t.InboundVoucherDetailId.Value) && t.Status == CrossDockTaskStatusEnum.Completed
                         group t by t.InboundVoucherDetailId!.Value into g
                         select new
                         {
                             VoucherDetailId = g.Key,
                             Qty = g.Sum((CrossDockTask t) => t.ActualQty ?? t.ScheduledQty)
                         }).ToDictionaryAsync(x => x.VoucherDetailId, x => x.Qty);
            Dictionary<long, decimal> catchWeightBaseByDetail = receivingDetailIds.Count == 0
                ? new Dictionary<long, decimal>()
                : await (from e in _db.CatchWeightEntries.AsNoTracking()
                         where e.VoucherDetailId.HasValue && receivingDetailIds.Contains(e.VoucherDetailId.Value) && e.Status == CatchWeightStatusEnum.Captured && (e.CapturePoint == CatchWeightCapturePointEnum.Receive || e.CapturePoint == CatchWeightCapturePointEnum.Putaway)
                         group e by e.VoucherDetailId!.Value into g
                         select new
                         {
                             VoucherDetailId = g.Key,
                             BaseQuantity = g.Sum((CatchWeightEntry e) => e.BaseQuantity)
                         }).ToDictionaryAsync(x => x.VoucherDetailId, x => x.BaseQuantity);
            foreach (VoucherDetail detail in receivingDetails)
            {
                if (detail.Item == null || detail.Voucher == null)
                {
                    continue;
                }
                decimal defectBase = detail.DefectBaseQty > 0m ? detail.DefectBaseQty : detail.DefectQty * (detail.ConversionRate == 0m ? 1m : Math.Abs(detail.ConversionRate));
                decimal goodBaseQty = Math.Max(0m, detail.BaseQty - Math.Max(0m, defectBase));
                string warehouseName = detail.Voucher.Warehouse?.WarehouseName ?? $"Kho {detail.Voucher.WarehouseId}";
                string actionUrl = $"/Vouchers/Details/{detail.VoucherId}";
                if (defectBase > 0.0001m)
                {
                    rows.Add(new OperationExceptionRow
                    {
                        CategoryKey = "inbound_receipt_variance",
                        CategoryLabel = "Lệch số lượng nhận thực tế",
                        SeverityKey = "high",
                        SeverityLabel = "Cao",
                        WarehouseId = detail.Voucher.WarehouseId,
                        WarehouseName = detail.Voucher.Warehouse?.WarehouseName ?? $"Kho {detail.Voucher.WarehouseId}",
                        ReferenceCode = detail.Voucher.VoucherCode,
                        SecondaryReference = detail.Item.ItemCode,
                        Summary = "Dòng hàng đã ghi nhận số lượng thực tế thấp hơn số lượng kế hoạch.",
                        Detail = $"Kế hoạch {detail.BaseQty:N4}, thực nhận tốt {goodBaseQty:N4}, lệch {Math.Max(0m, defectBase):N4}. Cần xác nhận nguyên nhân và trách nhiệm trước khi hoàn tất nhập kho.",
                        ItemCode = detail.Item.ItemCode,
                        DueAt = detail.Voucher.ExpectedArrivalAt,
                        AgeHours = detail.Voucher.ReceivedAt.HasValue ? Math.Max(0.01, (now - detail.Voucher.ReceivedAt.Value).TotalHours) : null,
                        ActionUrl = actionUrl,
                        ActionLabel = "Mở phiếu"
                    });
                }
                if (goodBaseQty <= 0.0001m)
                {
                    continue;
                }
                decimal putawayBaseQty = Math.Max(0m, goodBaseQty - (completedCrossDockByDetail.TryGetValue(detail.VoucherDetailId, out decimal crossDockQty) ? crossDockQty : 0m));
                if (detail.Item.TrackLot && string.IsNullOrWhiteSpace(detail.LotNumber))
                {
                    rows.Add(new OperationExceptionRow
                    {
                        CategoryKey = "inbound_missing_lot",
                        CategoryLabel = "Thiếu số lô nhập kho",
                        SeverityKey = "high",
                        SeverityLabel = "Cao",
                        WarehouseId = detail.Voucher.WarehouseId,
                        WarehouseName = warehouseName,
                        ReferenceCode = detail.Voucher.VoucherCode,
                        SecondaryReference = detail.Item.ItemCode,
                        Summary = "Dòng hàng bắt buộc quản lý theo lô nhưng chưa nhập số lô.",
                        Detail = $"Vật tư {detail.Item.ItemCode} cần số lô trước khi hoàn tất nhập kho.",
                        ItemCode = detail.Item.ItemCode,
                        DueAt = detail.Voucher.ExpectedArrivalAt,
                        ActionUrl = actionUrl,
                        ActionLabel = "Mở phiếu"
                    });
                }
                if (detail.Item.TrackExpiry && !detail.ExpiryDate.HasValue)
                {
                    rows.Add(new OperationExceptionRow
                    {
                        CategoryKey = "inbound_missing_expiry",
                        CategoryLabel = "Thiếu hạn dùng nhập kho",
                        SeverityKey = "high",
                        SeverityLabel = "Cao",
                        WarehouseId = detail.Voucher.WarehouseId,
                        WarehouseName = warehouseName,
                        ReferenceCode = detail.Voucher.VoucherCode,
                        SecondaryReference = detail.Item.ItemCode,
                        Summary = "Dòng hàng bắt buộc quản lý hạn dùng nhưng chưa nhập ngày hết hạn.",
                        Detail = $"Vật tư {detail.Item.ItemCode} cần hạn dùng trước khi hoàn tất nhập kho.",
                        ItemCode = detail.Item.ItemCode,
                        DueAt = detail.Voucher.ExpectedArrivalAt,
                        ActionUrl = actionUrl,
                        ActionLabel = "Mở phiếu"
                    });
                }
                if (putawayBaseQty > 0.0001m && !detail.LocationId.HasValue)
                {
                    rows.Add(new OperationExceptionRow
                    {
                        CategoryKey = "inbound_missing_putaway_location",
                        CategoryLabel = "Thiếu vị trí cất hàng",
                        SeverityKey = "high",
                        SeverityLabel = "Cao",
                        WarehouseId = detail.Voucher.WarehouseId,
                        WarehouseName = warehouseName,
                        ReferenceCode = detail.Voucher.VoucherCode,
                        SecondaryReference = detail.Item.ItemCode,
                        Summary = "Dòng hàng còn số lượng cần cất nhưng chưa có vị trí.",
                        Detail = $"Còn {putawayBaseQty:N4} đơn vị của {detail.Item.ItemCode} cần chọn vị trí cất hàng.",
                        ItemCode = detail.Item.ItemCode,
                        DueAt = detail.Voucher.ExpectedArrivalAt,
                        ActionUrl = actionUrl,
                        ActionLabel = "Mở phiếu"
                    });
                }
                if (detail.QualityStatus == QualityStatusEnum.Pending || detail.QualityStatus == QualityStatusEnum.Inspecting || detail.QualityStatus == QualityStatusEnum.Failed || detail.QualityStatus == QualityStatusEnum.Quarantine || detail.QualityStatus == QualityStatusEnum.OnHold)
                {
                    rows.Add(new OperationExceptionRow
                    {
                        CategoryKey = "inbound_quality_not_ready",
                        CategoryLabel = "Kiểm phẩm chưa sẵn sàng",
                        SeverityKey = "medium",
                        SeverityLabel = "Trung bình",
                        WarehouseId = detail.Voucher.WarehouseId,
                        WarehouseName = warehouseName,
                        ReferenceCode = detail.Voucher.VoucherCode,
                        SecondaryReference = detail.Item.ItemCode,
                        Summary = "Dòng hàng chưa đạt điều kiện kiểm phẩm để hoàn tất nhập kho.",
                        Detail = $"Trạng thái kiểm phẩm hiện tại: {detail.QualityStatusName}.",
                        ItemCode = detail.Item.ItemCode,
                        DueAt = detail.Voucher.ExpectedArrivalAt,
                        ActionUrl = actionUrl,
                        ActionLabel = "Mở phiếu"
                    });
                }
                if (detail.Item.TrackCatchWeight && detail.Item.RequireCatchWeightAtReceive)
                {
                    decimal capturedBaseQty = catchWeightBaseByDetail.TryGetValue(detail.VoucherDetailId, out decimal capturedQty) ? capturedQty : 0m;
                    if (capturedBaseQty + 0.0001m < goodBaseQty)
                    {
                        rows.Add(new OperationExceptionRow
                        {
                            CategoryKey = "inbound_missing_catch_weight",
                            CategoryLabel = "Thiếu cân thực tế nhập kho",
                            SeverityKey = "high",
                            SeverityLabel = "Cao",
                            WarehouseId = detail.Voucher.WarehouseId,
                            WarehouseName = warehouseName,
                            ReferenceCode = detail.Voucher.VoucherCode,
                            SecondaryReference = detail.Item.ItemCode,
                            Summary = "Dòng hàng yêu cầu cân trọng lượng thực tế nhưng chưa ghi nhận đủ.",
                            Detail = $"Đã cân {capturedBaseQty:N4}/{goodBaseQty:N4} đơn vị gốc của {detail.Item.ItemCode}.",
                            ItemCode = detail.Item.ItemCode,
                            DueAt = detail.Voucher.ExpectedArrivalAt,
                            ActionUrl = actionUrl,
                            ActionLabel = "Mở phiếu"
                        });
                    }
                }
            }
            var serialTrackedDetails = await (from d in _db.VoucherDetails.AsNoTracking().Include((VoucherDetail d) => d.Item).Include((VoucherDetail d) => d.Voucher)
                                              where inboundIds.Contains(d.VoucherId) && d.Item != null && d.Item.TrackSerial && d.Voucher != null && d.Voucher.InboundStatus == InboundStatusEnum.Receiving
                                              select new
                                              {
                                                  VoucherId = d.VoucherId,
                                                  VoucherDetailId = d.VoucherDetailId,
                                                  ItemId = d.ItemId,
                                                  ItemCode = d.Item.ItemCode,
                                                  RequiredQty = d.BaseQty - ((d.DefectBaseQty > 0m) ? d.DefectBaseQty : 0m)
                                              }).ToListAsync();
            Dictionary<(long VoucherId, long VoucherDetailId), int> registeredLookup = (await (from s in _db.SerialNumbers.AsNoTracking()
                                                                                               where inboundIds.Contains(s.VoucherId) && s.Status == SerialNumberStatusEnum.Active
                                                                                               group s by new { s.VoucherId, s.VoucherDetailId } into g
                                                                                               select new
                                                                                               {
                                                                                                   VoucherId = g.Key.VoucherId,
                                                                                                   VoucherDetailId = g.Key.VoucherDetailId,
                                                                                                   Count = g.Count()
                                                                                               }).ToListAsync()).ToDictionary(x => (VoucherId: x.VoucherId, VoucherDetailId: x.VoucherDetailId.GetValueOrDefault()), x => x.Count);
            foreach (Voucher voucher2 in inboundVouchers.Where((Voucher v) => v.InboundStatus == InboundStatusEnum.Receiving))
            {
                var detailRows = serialTrackedDetails.Where(d => d.VoucherId == voucher2.VoucherId).ToList();
                if (detailRows.Count != 0)
                {
                    int required = detailRows.Sum(d => GetRequiredSerialCount(Math.Max(0m, d.RequiredQty)));
                    int registered = detailRows.Sum(d => registeredLookup.TryGetValue((d.VoucherId, d.VoucherDetailId), out var value) ? value : 0);
                    if (registered < required)
                    {
                        rows.Add(new OperationExceptionRow
                        {
                            CategoryKey = "serial_missing_registration",
                            CategoryLabel = "Thiếu số sê-ri nhập kho",
                            SeverityKey = "high",
                            SeverityLabel = "Cao",
                            WarehouseId = voucher2.WarehouseId,
                            WarehouseName = (voucher2.Warehouse?.WarehouseName ?? $"Kho {voucher2.WarehouseId}"),
                            ReferenceCode = voucher2.VoucherCode,
                            SecondaryReference = voucher2.AsnCode,
                            Summary = "Phiếu nhập còn thiếu số sê-ri nên chưa đủ điều kiện hoàn tất.",
                            Detail = $"Đã đăng ký {registered}/{required} số sê-ri. Cần bổ sung trước khi hoàn tất nhập kho.",
                            DueAt = voucher2.ExpectedArrivalAt,
                            AgeHours = ((voucher2.ExpectedArrivalAt.HasValue && voucher2.ExpectedArrivalAt.Value < now) ? new double?(Math.Max(0.01, (now - voucher2.ExpectedArrivalAt.Value).TotalHours)) : ((double?)null)),
                            ActionUrl = $"/Operations/SerialReceiving/{voucher2.VoucherId}",
                            ActionLabel = "Nhận số sê-ri"
                        });
                    }
                }
            }
        }
        List<PickTask> pickTasks = await (from t in _db.PickTasks.AsNoTracking().Include((PickTask t) => t.Wave).ThenInclude((Wave? w) => w!.Warehouse)
                .Include((PickTask t) => t.Voucher)
                .ThenInclude((Voucher v) => v.Warehouse)
                .Include((PickTask t) => t.Item)
                .Include((PickTask t) => t.SourceLocation)
                                          where (t.Status == PickTaskStatusEnum.Pending || t.Status == PickTaskStatusEnum.Assigned || t.Status == PickTaskStatusEnum.InProgress || t.Status == PickTaskStatusEnum.Short) && (!((int?)warehouseId).HasValue || (t.Wave != null && t.Wave.WarehouseId == ((int?)warehouseId).Value) || (t.Wave == null && t.Voucher != null && t.Voucher.WarehouseId == ((int?)warehouseId).Value))
                                          select t).ToListAsync();
        List<long> pickTaskIds = pickTasks.Select((PickTask t) => t.PickTaskId).ToList();
        Dictionary<long, int> dictionary = ((pickTaskIds.Count != 0) ? (await (from x in _db.PickTaskSerialAssignments.AsNoTracking()
                                                                               where pickTaskIds.Contains(x.PickTaskId) && x.VoidedAt == null
                                                                               group x by x.PickTaskId into g
                                                                               select new
                                                                               {
                                                                                   PickTaskId = g.Key,
                                                                                   Count = g.Count()
                                                                               }).ToDictionaryAsync(x => x.PickTaskId, x => x.Count)) : new Dictionary<long, int>());
        Dictionary<long, int> serialAssignmentCountsByTask = dictionary;
        foreach (PickTask task in pickTasks)
        {
            int taskWarehouseId = task.Wave?.WarehouseId ?? task.Voucher?.WarehouseId ?? 0;
            string warehouseName = task.Wave?.Warehouse?.WarehouseName ?? task.Voucher?.Warehouse?.WarehouseName ?? $"Kho {taskWarehouseId}";
            if (task.Status == PickTaskStatusEnum.Short)
            {
                decimal shortQty = Math.Max(0m, task.TargetQty - task.PickedQty);
                rows.Add(new OperationExceptionRow
                {
                    CategoryKey = "pick_short",
                    CategoryLabel = "Lấy hàng thiếu",
                    SeverityKey = "high",
                    SeverityLabel = "Cao",
                    WarehouseId = taskWarehouseId,
                    WarehouseName = warehouseName,
                    ReferenceCode = task.TaskCode,
                    SecondaryReference = task.Voucher?.VoucherCode,
                    Summary = "Nhân viên đã báo thiếu hàng tại vị trí nguồn.",
                    Detail = $"Đã lấy {task.PickedQty:N2}/{task.TargetQty:N2}; còn thiếu {shortQty:N2}. Kiểm tra nhiệm vụ bù hoặc xử lý thiếu hàng/phiếu bổ sung.",
                    ItemCode = task.Item?.ItemCode,
                    LocationCode = task.SourceLocation?.LocationCode,
                    DueAt = task.DueAt,
                    AgeHours = (task.CompletedAt.HasValue ? new double?(Math.Max(0.01, (now - task.CompletedAt.Value).TotalHours)) : ((double?)null)),
                    ActionUrl = (task.WaveId.HasValue ? $"/Operations/PickTasks?waveId={task.WaveId}" : "/Operations/PickTasks"),
                    ActionLabel = "Mở nhiệm vụ lấy hàng"
                });
            }
            bool flag3 = string.IsNullOrWhiteSpace(task.AssignedTo);
            bool flag4 = flag3;
            if (flag4)
            {
                PickTaskStatusEnum status = task.Status;
                bool flag2 = status - 1 <= PickTaskStatusEnum.Pending;
                flag4 = flag2;
            }
            if (flag4)
            {
                rows.Add(new OperationExceptionRow
                {
                    CategoryKey = "pick_unassigned",
                    CategoryLabel = "Nhiệm vụ chưa gán",
                    SeverityKey = ((task.DueAt.HasValue && task.DueAt.Value < now) ? "high" : "medium"),
                    SeverityLabel = ((task.DueAt.HasValue && task.DueAt.Value < now) ? "Cao" : "Trung bình"),
                    WarehouseId = taskWarehouseId,
                    WarehouseName = warehouseName,
                    ReferenceCode = task.TaskCode,
                    SecondaryReference = task.Voucher?.VoucherCode,
                    Summary = "Nhiệm vụ lấy hàng chưa có người phụ trách.",
                    Detail = $"Vật tư {task.Item?.ItemCode ?? "---"} tại vị trí {task.SourceLocation?.LocationCode ?? "---"} đang chờ gán nhân viên.",
                    ItemCode = task.Item?.ItemCode,
                    LocationCode = task.SourceLocation?.LocationCode,
                    DueAt = task.DueAt,
                    AgeHours = ((task.DueAt.HasValue && task.DueAt.Value < now) ? new double?(Math.Max(0.01, (now - task.DueAt.GetValueOrDefault()).TotalHours)) : ((double?)null)),
                    ActionUrl = (task.WaveId.HasValue ? $"/Operations/PickTasks?waveId={task.WaveId}" : "/Operations/PickTasks"),
                    ActionLabel = "Mở nhiệm vụ"
                });
            }
            bool flag5 = task.DueAt.HasValue && task.DueAt.Value < now;
            bool flag6 = flag5;
            if (flag6)
            {
                PickTaskStatusEnum status = task.Status;
                bool flag2 = ((status - 1 <= PickTaskStatusEnum.Assigned || status == PickTaskStatusEnum.Short) ? true : false);
                flag6 = flag2;
            }
            if (flag6)
            {
                double ageHours3 = Math.Max(0.01, (now - task.DueAt.GetValueOrDefault()).TotalHours);
                (string Key, string Label, int Rank) severityInfo3 = MapSeverity(ageHours3);
                rows.Add(new OperationExceptionRow
                {
                    CategoryKey = "pick_overdue",
                    CategoryLabel = "Nhiệm vụ quá hạn",
                    SeverityKey = severityInfo3.Key,
                    SeverityLabel = severityInfo3.Label,
                    WarehouseId = taskWarehouseId,
                    WarehouseName = warehouseName,
                    ReferenceCode = task.TaskCode,
                    SecondaryReference = task.Voucher?.VoucherCode,
                    Summary = ((task.Status == PickTaskStatusEnum.Short) ? "Nhiệm vụ lấy hàng đã phát sinh thiếu nhưng chưa chốt xử lý." : "Nhiệm vụ lấy hàng vượt hạn hoàn thành."),
                    Detail = $"Quá hạn {ageHours3:N1} giờ. Người phụ trách: {task.AssignedTo ?? "chưa gán"}; vị trí: {task.SourceLocation?.LocationCode ?? "---"}.",
                    ItemCode = task.Item?.ItemCode,
                    LocationCode = task.SourceLocation?.LocationCode,
                    DueAt = task.DueAt,
                    AgeHours = ageHours3,
                    ActionUrl = (task.WaveId.HasValue ? $"/Operations/PickTasks?waveId={task.WaveId}" : "/Operations/PickTasks"),
                    ActionLabel = "Xem nhiệm vụ"
                });
            }
            if ((task.Item?.TrackSerial ?? false) && task.PickedQty > 0m)
            {
                int requiredSerials = GetRequiredSerialCount(task.PickedQty);
                int count;
                int actualSerials = (serialAssignmentCountsByTask.TryGetValue(task.PickTaskId, out count) ? count : 0);
                if (actualSerials < requiredSerials)
                {
                    rows.Add(new OperationExceptionRow
                    {
                        CategoryKey = "outbound_serial_gap",
                        CategoryLabel = "Thiếu số sê-ri lấy hàng",
                        SeverityKey = ((task.Status == PickTaskStatusEnum.Completed) ? "high" : "medium"),
                        SeverityLabel = ((task.Status == PickTaskStatusEnum.Completed) ? "Cao" : "Trung bình"),
                        WarehouseId = taskWarehouseId,
                        WarehouseName = warehouseName,
                        ReferenceCode = task.TaskCode,
                        SecondaryReference = task.Voucher?.VoucherCode,
                        Summary = "Nhiệm vụ xuất hàng đã lấy nhưng chưa đủ số sê-ri quét.",
                        Detail = $"Đã lấy {task.PickedQty:N0} nhưng mới có {actualSerials}/{requiredSerials} số sê-ri. Cần quét bù trước khi ghi sổ phiếu xuất.",
                        ItemCode = task.Item.ItemCode,
                        LocationCode = task.SourceLocation?.LocationCode,
                        DueAt = task.DueAt,
                        AgeHours = ((task.DueAt.HasValue && task.DueAt.Value < now) ? new double?(Math.Max(0.01, (now - task.DueAt.GetValueOrDefault()).TotalHours)) : ((double?)null)),
                        ActionUrl = (task.WaveId.HasValue ? "/Operations/RfPicking" : "/Operations/PickTasks"),
                        ActionLabel = "Bổ sung số sê-ri"
                    });
                }
            }
        }
        List<Voucher> shippingVouchers = await (from v in _db.Vouchers.AsNoTracking().Include((Voucher v) => v.Warehouse).Include((Voucher v) => v.Partner)
                                                where !v.IsCancelled && v.IsPosted && (v.VoucherType == VoucherTypeEnum.XuatKho || v.VoucherType == VoucherTypeEnum.TraNCC || v.VoucherType == VoucherTypeEnum.ChuyenKho || v.VoucherType == VoucherTypeEnum.XuatSanXuat) && (!((int?)warehouseId).HasValue || v.WarehouseId == ((int?)warehouseId).Value)
                                                select v).ToListAsync();
        List<long> shippingVoucherIds = shippingVouchers.Select((Voucher v) => v.VoucherId).Distinct().ToList();
        Dictionary<long, int> dictionary2 = ((shippingVoucherIds.Count != 0) ? (await (from p in _db.OutboundPackages.AsNoTracking()
                                                                                       where shippingVoucherIds.Contains(p.VoucherId)
                                                                                       group p by p.VoucherId into g
                                                                                       select new
                                                                                       {
                                                                                           VoucherId = g.Key,
                                                                                           Count = g.Count()
                                                                                       }).ToDictionaryAsync(x => x.VoucherId, x => x.Count)) : new Dictionary<long, int>());
        Dictionary<long, int> shippingPackageCounts = dictionary2;
        HashSet<long> catchWeightRequiredVoucherIds = shippingVoucherIds.Count == 0
            ? new HashSet<long>()
            : (await (from d in _db.VoucherDetails.AsNoTracking().Include((VoucherDetail d) => d.Item)
                      where shippingVoucherIds.Contains(d.VoucherId) && d.Item != null && d.Item.TrackCatchWeight && d.Item.RequireCatchWeightAtPickPack
                      select d.VoucherId).Distinct().ToListAsync()).ToHashSet();
        Dictionary<long, int> missingPackageCatchWeightCounts = shippingVoucherIds.Count == 0
            ? new Dictionary<long, int>()
            : await (from p in _db.OutboundPackages.AsNoTracking()
                     where shippingVoucherIds.Contains(p.VoucherId) && (!p.ActualCatchWeight.HasValue || p.ActualCatchWeight.Value <= 0m)
                     group p by p.VoucherId into g
                     select new
                     {
                         VoucherId = g.Key,
                         Count = g.Count()
                     }).ToDictionaryAsync(x => x.VoucherId, x => x.Count);
        foreach (Voucher voucher3 in shippingVouchers)
        {
            string warehouseName2 = voucher3.Warehouse?.WarehouseName ?? $"Kho {voucher3.WarehouseId}";
            int count2;
            int packageCount = (shippingPackageCounts.TryGetValue(voucher3.VoucherId, out count2) ? count2 : 0);
            if (!voucher3.PackedAt.HasValue && voucher3.RequestedDeliveryDate.HasValue && voucher3.RequestedDeliveryDate.Value.Date < now.Date)
            {
                double ageHours4 = Math.Max(0.01, (now.Date - voucher3.RequestedDeliveryDate.Value.Date).TotalHours);
                rows.Add(new OperationExceptionRow
                {
                    CategoryKey = "shipping_not_packed_overdue",
                    CategoryLabel = "Chưa đóng gói đúng hẹn",
                    SeverityKey = ((ageHours4 >= 24.0) ? "high" : "medium"),
                    SeverityLabel = ((ageHours4 >= 24.0) ? "Cao" : "Trung bình"),
                    WarehouseId = voucher3.WarehouseId,
                    WarehouseName = warehouseName2,
                    ReferenceCode = voucher3.VoucherCode,
                    SecondaryReference = voucher3.Partner?.PartnerName,
                    Summary = "Phiếu xuất đã ghi sổ nhưng vẫn chưa đóng gói.",
                    Detail = $"Đã quá ngày giao {voucher3.RequestedDeliveryDate:dd/MM/yyyy}. Cần hoàn tất đóng gói trước khi bàn giao vận chuyển.",
                    DueAt = voucher3.RequestedDeliveryDate,
                    AgeHours = ageHours4,
                    ActionUrl = $"/Operations/Shipping?warehouseId={voucher3.WarehouseId}&status=packing&search={Uri.EscapeDataString(voucher3.VoucherCode)}",
                    ActionLabel = "Mở bảng giao hàng"
                });
            }
            if (voucher3.PackedAt.HasValue && !voucher3.ShippedAt.HasValue)
            {
                if (packageCount == 0)
                {
                    rows.Add(new OperationExceptionRow
                    {
                        CategoryKey = "shipping_missing_packages",
                        CategoryLabel = "Thiếu dữ liệu kiện xuất",
                        SeverityKey = "high",
                        SeverityLabel = "Cao",
                        WarehouseId = voucher3.WarehouseId,
                        WarehouseName = warehouseName2,
                        ReferenceCode = voucher3.VoucherCode,
                        SecondaryReference = voucher3.Partner?.PartnerName,
                        Summary = "Phiếu đã đóng gói nhưng chưa có kiện xuất nào được ghi nhận.",
                        Detail = "Vui lòng cập nhật lại bước đóng gói theo kiện / mã kiện để phục vụ truy vết vận chuyển.",
                        DueAt = voucher3.PackedAt,
                        AgeHours = Math.Max(0.01, (now - voucher3.PackedAt.Value).TotalHours),
                        ActionUrl = $"/Vouchers/Details/{voucher3.VoucherId}",
                        ActionLabel = "Mở phiếu"
                    });
                }
                bool referenceMissing = (RequiresTrackingOrManifest(voucher3.VoucherType) && string.IsNullOrWhiteSpace(voucher3.TrackingNumber) && string.IsNullOrWhiteSpace(voucher3.ManifestCode)) || (RequiresManifest(voucher3.VoucherType) && string.IsNullOrWhiteSpace(voucher3.ManifestCode));
                if (voucher3.PackedAt.Value < now.AddHours(-4.0) || (voucher3.RequestedDeliveryDate.HasValue && voucher3.RequestedDeliveryDate.Value.Date < now.Date))
                {
                    double ageHours5 = Math.Max(0.01, (now - voucher3.PackedAt.Value).TotalHours);
                    rows.Add(new OperationExceptionRow
                    {
                        CategoryKey = "shipping_ready_not_shipped",
                        CategoryLabel = "Đã đóng gói nhưng chưa giao",
                        SeverityKey = ((ageHours5 >= 24.0) ? "high" : "medium"),
                        SeverityLabel = ((ageHours5 >= 24.0) ? "Cao" : "Trung bình"),
                        WarehouseId = voucher3.WarehouseId,
                        WarehouseName = warehouseName2,
                        ReferenceCode = voucher3.VoucherCode,
                        SecondaryReference = voucher3.Partner?.PartnerName,
                        Summary = "Phiếu xuất đã sẵn sàng nhưng chưa xác nhận giao hàng.",
                        Detail = $"Đã đóng gói từ {voucher3.PackedAt:dd/MM HH:mm}. Mã vận đơn: {voucher3.TrackingNumber ?? "---"}, mã chuyến bàn giao: {voucher3.ManifestCode ?? "---"}.",
                        DueAt = (voucher3.RequestedDeliveryDate ?? voucher3.PackedAt),
                        AgeHours = ageHours5,
                        ActionUrl = $"/Operations/Shipping?warehouseId={voucher3.WarehouseId}&status=ready&search={Uri.EscapeDataString(voucher3.VoucherCode)}",
                        ActionLabel = "Theo dõi giao hàng"
                    });
                }
                if (referenceMissing)
                {
                    rows.Add(new OperationExceptionRow
                    {
                        CategoryKey = "shipping_missing_reference",
                        CategoryLabel = "Thiếu mã giao hàng",
                        SeverityKey = "medium",
                        SeverityLabel = "Trung bình",
                        WarehouseId = voucher3.WarehouseId,
                        WarehouseName = warehouseName2,
                        ReferenceCode = voucher3.VoucherCode,
                        SecondaryReference = voucher3.Partner?.PartnerName,
                        Summary = "Phiếu đã đóng gói nhưng chưa đủ mã tham chiếu vận chuyển.",
                        Detail = (RequiresManifest(voucher3.VoucherType) ? "Cần nhập mã chuyến bàn giao trước khi xác nhận giao hàng." : "Cần nhập ít nhất mã vận đơn hoặc mã chuyến bàn giao trước khi xác nhận giao hàng."),
                        DueAt = (voucher3.RequestedDeliveryDate ?? voucher3.PackedAt),
                        AgeHours = (voucher3.PackedAt.HasValue ? new double?(Math.Max(0.01, (now - voucher3.PackedAt.Value).TotalHours)) : ((double?)null)),
                        ActionUrl = $"/Vouchers/Details/{voucher3.VoucherId}",
                        ActionLabel = "Mở phiếu"
                    });
                }
                if (catchWeightRequiredVoucherIds.Contains(voucher3.VoucherId)
                    && missingPackageCatchWeightCounts.TryGetValue(voucher3.VoucherId, out int missingCatchPackages)
                    && missingCatchPackages > 0)
                {
                    rows.Add(new OperationExceptionRow
                    {
                        CategoryKey = "shipping_missing_package_catch_weight",
                        CategoryLabel = "Thiếu cân kiện xuất",
                        SeverityKey = "high",
                        SeverityLabel = "Cao",
                        WarehouseId = voucher3.WarehouseId,
                        WarehouseName = warehouseName2,
                        ReferenceCode = voucher3.VoucherCode,
                        SecondaryReference = voucher3.Partner?.PartnerName,
                        Summary = "Phiếu có hàng cân trọng lượng thực tế nhưng còn kiện chưa ghi cân.",
                        Detail = $"Còn {missingCatchPackages} kiện xuất chưa có trọng lượng thực tế.",
                        DueAt = (voucher3.RequestedDeliveryDate ?? voucher3.PackedAt),
                        AgeHours = (voucher3.PackedAt.HasValue ? new double?(Math.Max(0.01, (now - voucher3.PackedAt.Value).TotalHours)) : ((double?)null)),
                        ActionUrl = $"/Vouchers/Details/{voucher3.VoucherId}",
                        ActionLabel = "Mở phiếu"
                    });
                }
            }
            if (voucher3.ShippedAt.HasValue && ((RequiresTrackingOrManifest(voucher3.VoucherType) && string.IsNullOrWhiteSpace(voucher3.TrackingNumber) && string.IsNullOrWhiteSpace(voucher3.ManifestCode)) || (RequiresManifest(voucher3.VoucherType) && string.IsNullOrWhiteSpace(voucher3.ManifestCode))))
            {
                rows.Add(new OperationExceptionRow
                {
                    CategoryKey = "shipping_posted_missing_reference",
                    CategoryLabel = "Thiếu mã vận đơn/bản kê sau giao",
                    SeverityKey = "high",
                    SeverityLabel = "Cao",
                    WarehouseId = voucher3.WarehouseId,
                    WarehouseName = warehouseName2,
                    ReferenceCode = voucher3.VoucherCode,
                    SecondaryReference = voucher3.Partner?.PartnerName,
                    Summary = "Phiếu đã giao nhưng thiếu mã tham chiếu phục vụ đối soát.",
                    Detail = "Cần cập nhật mã vận đơn / mã chuyến bàn giao để đối soát vận chuyển và tra cứu chứng từ.",
                    DueAt = voucher3.ShippedAt,
                    AgeHours = Math.Max(0.01, (now - voucher3.ShippedAt.Value).TotalHours),
                    ActionUrl = $"/Vouchers/Details/{voucher3.VoucherId}",
                    ActionLabel = "Bổ sung thông tin"
                });
            }
        }
        List<ShipmentLoad> shipmentLoads = await (from l in _db.ShipmentLoads.AsNoTracking().Include((ShipmentLoad l) => l.Warehouse)
                                                  where l.Status != ShipmentLoadStatusEnum.Cancelled && (!((int?)warehouseId).HasValue || l.WarehouseId == ((int?)warehouseId).Value)
                                                  orderby l.PlannedDepartureAt ?? l.CreatedAt descending
                                                  select l).Take(300).ToListAsync();
        List<long> shipmentLoadIds = shipmentLoads.Select((ShipmentLoad l) => l.ShipmentLoadId).ToList();
        if (shipmentLoadIds.Count > 0)
        {
            List<ShipmentLoadVoucher> loadVoucherMappings = await _db.ShipmentLoadVouchers.AsNoTracking()
                .Where((ShipmentLoadVoucher x) => shipmentLoadIds.Contains(x.ShipmentLoadId) && x.RemovedAt == null)
                .ToListAsync();
            Dictionary<long, List<long>> voucherIdsByLoad = loadVoucherMappings
                .GroupBy((ShipmentLoadVoucher x) => x.ShipmentLoadId)
                .ToDictionary((IGrouping<long, ShipmentLoadVoucher> g) => g.Key, (IGrouping<long, ShipmentLoadVoucher> g) => g.Select((ShipmentLoadVoucher x) => x.VoucherId).Distinct().ToList());
            List<long> loadVoucherIds = loadVoucherMappings.Select((ShipmentLoadVoucher x) => x.VoucherId).Distinct().ToList();
            List<OutboundPackage> loadPackages = loadVoucherIds.Count == 0
                ? new List<OutboundPackage>()
                : await _db.OutboundPackages.AsNoTracking()
                    .Include((OutboundPackage p) => p.Voucher)
                    .Where((OutboundPackage p) => loadVoucherIds.Contains(p.VoucherId))
                    .ToListAsync();
            Dictionary<long, HashSet<long>> loadedPackageIdsByLoad = (await _db.ShipmentLoadPackages.AsNoTracking()
                    .Where((ShipmentLoadPackage x) => shipmentLoadIds.Contains(x.ShipmentLoadId) && x.RemovedAt == null && x.IsLoaded)
                    .Select((ShipmentLoadPackage x) => new { x.ShipmentLoadId, x.OutboundPackageId })
                    .ToListAsync())
                .GroupBy(x => x.ShipmentLoadId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.OutboundPackageId).ToHashSet());
            foreach (ShipmentLoad load in shipmentLoads)
            {
                if (!voucherIdsByLoad.TryGetValue(load.ShipmentLoadId, out List<long>? voucherIds) || voucherIds.Count == 0)
                {
                    continue;
                }
                List<OutboundPackage> expectedPackages = loadPackages.Where((OutboundPackage p) => voucherIds.Contains(p.VoucherId)).ToList();
                if (expectedPackages.Count == 0)
                {
                    continue;
                }
                HashSet<long> loadedPackageIds = loadedPackageIdsByLoad.TryGetValue(load.ShipmentLoadId, out HashSet<long>? matchedLoadedPackageIds)
                    ? matchedLoadedPackageIds
                    : new HashSet<long>();
                List<OutboundPackage> missingPackages = expectedPackages.Where((OutboundPackage p) => !loadedPackageIds.Contains(p.OutboundPackageId)).ToList();
                if (missingPackages.Count == 0)
                {
                    continue;
                }
                bool departedOrClosed = load.Status is ShipmentLoadStatusEnum.Departed or ShipmentLoadStatusEnum.Closed;
                bool plannedDepartureDue = load.PlannedDepartureAt.HasValue && load.PlannedDepartureAt.Value <= now.AddHours(1.0);
                bool shouldRaiseBeforeDeparture = load.Status is ShipmentLoadStatusEnum.Staged or ShipmentLoadStatusEnum.Loading or ShipmentLoadStatusEnum.Loaded || plannedDepartureDue;
                if (!departedOrClosed && !shouldRaiseBeforeDeparture)
                {
                    continue;
                }
                double? ageHours = departedOrClosed && load.ActualDepartureAt.HasValue
                    ? Math.Max(0.01, (now - load.ActualDepartureAt.Value).TotalHours)
                    : load.PlannedDepartureAt.HasValue && load.PlannedDepartureAt.Value < now
                        ? Math.Max(0.01, (now - load.PlannedDepartureAt.Value).TotalHours)
                        : null;
                string samplePackages = string.Join(", ", missingPackages.Select((OutboundPackage p) => p.PackageCode).Take(5));
                rows.Add(new OperationExceptionRow
                {
                    CategoryKey = departedOrClosed ? "load_departed_package_missing" : "load_package_scan_missing",
                    CategoryLabel = departedOrClosed ? "Chuyến đã rời kho còn thiếu kiện" : "Kiện xuất chưa quét lên chuyến",
                    SeverityKey = departedOrClosed ? "critical" : plannedDepartureDue ? "high" : "medium",
                    SeverityLabel = departedOrClosed ? "Khẩn cấp" : plannedDepartureDue ? "Cao" : "Trung bình",
                    WarehouseId = load.WarehouseId,
                    WarehouseName = load.Warehouse?.WarehouseName ?? $"Kho {load.WarehouseId}",
                    ReferenceCode = load.LoadCode,
                    SecondaryReference = samplePackages,
                    Summary = departedOrClosed ? "Chuyến xe đã rời kho nhưng dữ liệu xếp kiện chưa đầy đủ." : "Chuyến xe còn kiện xuất chưa được quét xác nhận lên chuyến.",
                    Detail = $"Còn {missingPackages.Count}/{expectedPackages.Count} kiện chưa được ghi nhận đã xếp lên chuyến. Mẫu thiếu: {samplePackages}.",
                    DueAt = load.PlannedDepartureAt ?? load.ActualDepartureAt,
                    AgeHours = ageHours,
                    ActionUrl = $"/Operations/ShipmentLoadDetails/{load.ShipmentLoadId}",
                    ActionLabel = "Mở chuyến xe"
                });
            }
        }
        foreach (ReplenishmentSuggestionRow suggestion in await BuildReplenishmentSuggestionsAsync(warehouseId, null))
        {
            if (suggestion.HasActiveLpn)
            {
                rows.Add(new OperationExceptionRow
                {
                    CategoryKey = "replenishment_blocked_lpn",
                    CategoryLabel = "Bổ sung hàng bị chặn",
                    SeverityKey = ((suggestion.PickFaceQty <= 0m) ? "high" : "medium"),
                    SeverityLabel = ((suggestion.PickFaceQty <= 0m) ? "Cao" : "Trung bình"),
                    WarehouseId = suggestion.WarehouseId,
                    WarehouseName = suggestion.WarehouseName,
                    ReferenceCode = suggestion.ItemCode,
                    SecondaryReference = suggestion.SourceLocationCode,
                    Summary = "Vị trí lấy hàng đang thiếu hàng nhưng nguồn chứa tổng còn gắn mã kiện (LPN).",
                    Detail = $"Cần điều chuyển theo mã kiện (LPN) từ {suggestion.SourceLocationCode} về {suggestion.DefaultLocationCode}. Mức đề nghị: {suggestion.SuggestedQty:N2}.",
                    ItemCode = suggestion.ItemCode,
                    LocationCode = suggestion.DefaultLocationCode,
                    ActionUrl = $"/Operations/Replenishment?warehouseId={suggestion.WarehouseId}&search={Uri.EscapeDataString(suggestion.ItemCode)}",
                    ActionLabel = "Mở bổ sung hàng"
                });
            }
        }
        foreach (LicensePlate lpn in await (from l in _db.LicensePlates.AsNoTracking().Include((LicensePlate l) => l.Warehouse).Include((LicensePlate l) => l.Details).ThenInclude((LicensePlateDetail d) => d.Item)
                                            where l.IsActive && l.Status != LpnStatusEnum.Voided && (!((int?)warehouseId).HasValue || l.WarehouseId == ((int?)warehouseId).Value) && l.CurrentLocationId == (int?)null
                                            orderby l.CreatedAt descending
                                            select l).Take(100).ToListAsync())
        {
            double ageHours6 = Math.Max(0.01, (now - lpn.CreatedAt).TotalHours);
            rows.Add(new OperationExceptionRow
            {
                CategoryKey = "lpn_missing_location",
                CategoryLabel = "Mã kiện (LPN) chưa có vị trí",
                SeverityKey = ((ageHours6 >= 4.0) ? "high" : "medium"),
                SeverityLabel = ((ageHours6 >= 4.0) ? "Cao" : "Trung bình"),
                WarehouseId = lpn.WarehouseId,
                WarehouseName = (lpn.Warehouse?.WarehouseName ?? $"Kho {lpn.WarehouseId}"),
                ReferenceCode = lpn.LpnCode,
                SecondaryReference = string.Join(", ", lpn.Details.Select(d => d.Item != null ? d.Item.ItemCode : d.ItemId.ToString()).Distinct().Take(3)),
                Summary = "Mã kiện (LPN) đang hoạt động nhưng chưa được gán vị trí cất hàng.",
                Detail = "Cần hoàn tất cất hàng hoặc cập nhật vị trí thực tế cho mã kiện (LPN) [" + lpn.LpnCode + "].",
                ItemCode = lpn.Details.Select(d => d.Item != null ? d.Item.ItemCode : d.ItemId.ToString()).FirstOrDefault(),
                DueAt = lpn.CreatedAt,
                AgeHours = ageHours6,
                ActionUrl = $"/Operations/LpnLookup?warehouseId={lpn.WarehouseId}&search={Uri.EscapeDataString(lpn.LpnCode)}",
                ActionLabel = "Tra cứu mã kiện"
            });
        }
        foreach (OperationExceptionRow row in rows)
        {
            row.ExceptionKey = ComputeExceptionKey(row);
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            string normalizedCategory = category.Trim();
            rows = rows.Where((OperationExceptionRow r) => string.Equals(r.CategoryKey, normalizedCategory, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        if (!string.IsNullOrWhiteSpace(severity))
        {
            string normalizedSeverity = severity.Trim();
            rows = rows.Where((OperationExceptionRow r) => string.Equals(r.SeverityKey, normalizedSeverity, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();
            rows = rows.Where(delegate (OperationExceptionRow r)
            {
                int result;
                if (!r.ReferenceCode.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    string? secondaryReference = r.SecondaryReference;
                    if (secondaryReference == null || !secondaryReference.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        string? itemCode = r.ItemCode;
                        if (itemCode == null || !itemCode.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            string? locationCode = r.LocationCode;
                            if ((locationCode == null || !locationCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)) && !r.Summary.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                            {
                                result = (r.Detail.Contains(keyword, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
                                goto IL_008e;
                            }
                        }
                    }
                }
                result = 1;
                goto IL_008e;
            IL_008e:
                return (byte)result != 0;
            }).ToList();
        }
        return (from r in rows
                orderby GetSeverityRank(r.SeverityKey), r.AgeHours.GetValueOrDefault() descending, r.WarehouseName, r.CategoryLabel, r.ReferenceCode
                select r).Take(300).ToList();
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> ExceptionCenter(int? warehouseId, string? category, string? severity, string? search)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        List<OperationExceptionRow> rows = await BuildExceptionCenterRowsAsync(warehouseId, category, severity, search);
        await SyncExceptionCasesAsync(rows);
        await DecorateExceptionRowsAsync(rows);
        base.ViewBag.Warehouses = (await (from w in _db.Warehouses.AsNoTracking()
                                          where w.IsActive
                                          orderby w.WarehouseCode
                                          select w).ToListAsync());
        base.ViewBag.WarehouseId = warehouseId;
        base.ViewBag.Category = category;
        base.ViewBag.Severity = severity;
        base.ViewBag.Search = search;
        base.ViewBag.TotalExceptions = rows.Count;
        base.ViewBag.CriticalCount = rows.Count((OperationExceptionRow r) => r.SeverityKey == "critical");
        base.ViewBag.HighCount = rows.Count((OperationExceptionRow r) => r.SeverityKey == "high");
        base.ViewBag.MediumCount = rows.Count((OperationExceptionRow r) => r.SeverityKey == "medium");
        base.ViewBag.Categories = (from r in rows
                                   group r by new { r.CategoryKey, r.CategoryLabel } into g
                                   orderby g.Count() descending, g.Key.CategoryLabel
                                   select (CategoryKey: g.Key.CategoryKey, CategoryLabel: g.Key.CategoryLabel, Count: g.Count())).ToList();
        IQueryable<AppUser> userQuery = from u in _db.AppUsers.AsNoTracking().Include((AppUser u) => u.Role)
                                        where u.IsActive && (u.Role.RoleName == "Staff" || u.Role.RoleName == "Manager")
                                        select u;
        if (warehouseId.HasValue)
        {
            userQuery = userQuery.Where((AppUser u) => u.WarehouseId == (int?)((int?)warehouseId).Value);
        }
        else if (scopedWh.HasValue)
        {
            userQuery = userQuery.Where((AppUser u) => u.WarehouseId == (int?)((int?)scopedWh).Value);
        }
        base.ViewBag.EligibleUsers = (await userQuery.OrderBy((AppUser u) => u.FullName).ToListAsync());
        return View(rows);
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcknowledgeException(string exceptionKey)
    {
        if (string.IsNullOrWhiteSpace(exceptionKey))
        {
            return RedirectToAction("ExceptionCenter");
        }
        OperationExceptionCase? exceptionCase = await GetScopedExceptionCaseAsync(exceptionKey.Trim());
        if (exceptionCase == null)
        {
            return Forbid();
        }
        if (exceptionCase.Status != OperationExceptionStatusEnum.Resolved)
        {
            exceptionCase.Status = OperationExceptionStatusEnum.Acknowledged;
            exceptionCase.AcknowledgedBy = base.User.Identity?.Name ?? "system";
            exceptionCase.AcknowledgedAt = VietnamNow;
            exceptionCase.UpdatedAt = VietnamNow;
            await _unitOfWork.SaveChangesAsync();
            base.TempData["Success"] = "Đã ghi nhận bất thường [" + exceptionCase.ReferenceCode + "] vào trạng thái đang xử lý.";
        }
        return RedirectToAction("ExceptionCenter");
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignException(string exceptionKey, string assignedTo)
    {
        if (string.IsNullOrWhiteSpace(exceptionKey))
        {
            return RedirectToAction("ExceptionCenter");
        }
        OperationExceptionCase? exceptionCase = await GetScopedExceptionCaseAsync(exceptionKey.Trim());
        if (exceptionCase == null)
        {
            return Forbid();
        }
        (bool IsValid, string? NormalizedUserName, string ErrorMessage) validation = await ValidateAssigneeAsync(assignedTo, exceptionCase.WarehouseId);
        if (!validation.IsValid)
        {
            base.TempData["Error"] = validation.ErrorMessage;
            return RedirectToAction("ExceptionCenter");
        }
        exceptionCase.AssignedTo = validation.NormalizedUserName;
        if (exceptionCase.Status == OperationExceptionStatusEnum.Open)
        {
            exceptionCase.Status = OperationExceptionStatusEnum.Acknowledged;
            exceptionCase.AcknowledgedBy = base.User.Identity?.Name ?? "system";
            exceptionCase.AcknowledgedAt = VietnamNow;
        }
        exceptionCase.UpdatedAt = VietnamNow;
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = $"Đã giao bất thường [{exceptionCase.ReferenceCode}] cho [{validation.NormalizedUserName}].";
        return RedirectToAction("ExceptionCenter");
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveException(string exceptionKey, string resolutionNote)
    {
        if (string.IsNullOrWhiteSpace(exceptionKey))
        {
            return RedirectToAction("ExceptionCenter");
        }
        OperationExceptionCase? exceptionCase = await GetScopedExceptionCaseAsync(exceptionKey.Trim());
        if (exceptionCase == null)
        {
            return Forbid();
        }
        if (string.IsNullOrWhiteSpace(resolutionNote))
        {
            base.TempData["Error"] = "Vui lòng nhập ghi chú xử lý trước khi đóng bất thường.";
            return RedirectToAction("ExceptionCenter");
        }
        exceptionCase.Status = OperationExceptionStatusEnum.Resolved;
        exceptionCase.ResolvedBy = base.User.Identity?.Name ?? "system";
        exceptionCase.ResolvedAt = VietnamNow;
        exceptionCase.ResolutionNote = resolutionNote.Trim();
        if (!exceptionCase.AcknowledgedAt.HasValue)
        {
            exceptionCase.AcknowledgedBy = base.User.Identity?.Name ?? "system";
            exceptionCase.AcknowledgedAt = VietnamNow;
        }
        exceptionCase.UpdatedAt = VietnamNow;
        await _unitOfWork.SaveChangesAsync();
        base.TempData["Success"] = "Đã đóng bất thường [" + exceptionCase.ReferenceCode + "].";
        return RedirectToAction("ExceptionCenter");
    }

}
