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

    private static int GetRequiredSerialCount(decimal qty)
    {
        if (qty <= 0m)
        {
            return 0;
        }
        return (int)Math.Ceiling(qty);
    }


    private static List<string> ParseSerialCodes(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<string>();
        }
        return (from s in raw.Split(new char[5] { '\r', '\n', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                select s.Trim().ToUpperInvariant() into s
                where !string.IsNullOrWhiteSpace(s)
                select s).Distinct<string>(StringComparer.OrdinalIgnoreCase).ToList();
    }


    private async Task DecorateInboundSerialSummaryAsync(List<InboundReceivingRow> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }
        List<long> voucherIds = rows.Select((InboundReceivingRow r) => r.VoucherId).Distinct().ToList();
        var detailRows = await (from d in _db.VoucherDetails.AsNoTracking().Include((VoucherDetail d) => d.Item)
                                where voucherIds.Contains(d.VoucherId) && d.Item != null && d.Item.TrackSerial
                                select new
                                {
                                    VoucherId = d.VoucherId,
                                    RequiredQty = d.BaseQty - ((d.DefectBaseQty > 0m) ? d.DefectBaseQty : 0m)
                                }).ToListAsync();
        if (detailRows.Count == 0)
        {
            return;
        }
        Dictionary<long, int> serialCounts = await (from s in _db.SerialNumbers.AsNoTracking()
                                                    where voucherIds.Contains(s.VoucherId) && s.Status == SerialNumberStatusEnum.Active
                                                    group s by s.VoucherId into g
                                                    select new
                                                    {
                                                        VoucherId = g.Key,
                                                        Count = g.Count()
                                                    }).ToDictionaryAsync(x => x.VoucherId, x => x.Count);
        Dictionary<long, int> requiredByVoucher = (from x in detailRows
                                                   group x by x.VoucherId).ToDictionary(g => g.Key, g => g.Sum(x => GetRequiredSerialCount(x.RequiredQty)));
        foreach (InboundReceivingRow row in rows)
        {
            if (requiredByVoucher.TryGetValue(row.VoucherId, out var required))
            {
                int count;
                int registered = (serialCounts.TryGetValue(row.VoucherId, out count) ? count : 0);
                row.HasSerialTrackedLines = required > 0;
                row.RequiredSerialCount = required;
                row.PendingSerialCount = Math.Max(0, required - registered);
            }
        }
    }


    private async Task<SerialReceivingPageViewModel?> BuildSerialReceivingPageAsync(long voucherId, int? scopedWh)
    {
        Voucher? voucher = await _db.Vouchers.AsNoTracking().Include((Voucher v) => v.Warehouse).Include((Voucher v) => v.Partner)
            .Include((Voucher v) => v.Details)
            .ThenInclude((VoucherDetail d) => d.Item)
            .Include((Voucher v) => v.Details)
            .ThenInclude((VoucherDetail d) => d.Location)
            .Include((Voucher v) => v.Details)
            .ThenInclude((VoucherDetail d) => d.TransactionUom)
            .FirstOrDefaultAsync((Voucher v) => v.VoucherId == voucherId && !v.IsCancelled && (v.VoucherType == VoucherTypeEnum.NhapKho || v.VoucherType == VoucherTypeEnum.KhachTra || v.VoucherType == VoucherTypeEnum.NhapThanhPham));
        if (voucher == null)
        {
            return null;
        }
        if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value)
        {
            return null;
        }
        List<long> detailIds = (from d in voucher.Details
                                where d.Item != null && d.Item.TrackSerial
                                select d.VoucherDetailId).ToList();
        List<SerialNumber> list = ((detailIds.Count != 0) ? (await (from s in _db.SerialNumbers.AsNoTracking()
                                                                    where s.Status == SerialNumberStatusEnum.Active && s.VoucherId == voucherId && s.VoucherDetailId.HasValue && detailIds.Contains(s.VoucherDetailId.Value)
                                                                    orderby s.SerialCode
                                                                    select s).ToListAsync()) : new List<SerialNumber>());
        List<SerialNumber> serials = list;
        return new SerialReceivingPageViewModel
        {
            VoucherId = voucher.VoucherId,
            VoucherCode = voucher.VoucherCode,
            WarehouseName = (voucher.Warehouse?.WarehouseName ?? $"Kho {voucher.WarehouseId}"),
            PartnerName = voucher.Partner?.PartnerName,
            InboundStatus = voucher.InboundStatus,
            Lines = (from d in voucher.Details
                     where d.Item != null && d.Item.TrackSerial
                     orderby d.LineNumber
                     select d).Select(delegate (VoucherDetail d)
                 {
                     decimal num = Math.Max(0m, d.BaseQty - ((d.DefectBaseQty > 0m) ? d.DefectBaseQty : 0m));
                     List<string> list2 = (from s in serials
                                           where s.VoucherDetailId == d.VoucherDetailId
                                           select s.SerialCode).ToList();
                     return new SerialReceivingLineRow
                     {
                         VoucherDetailId = d.VoucherDetailId,
                         ItemId = d.ItemId,
                         ItemCode = d.Item.ItemCode,
                         ItemName = d.Item.ItemName,
                         LocationCode = d.Location?.LocationCode,
                         UomCode = (d.TransactionUom?.UomCode ?? d.Item.BaseUom?.UomCode ?? ""),
                         RequiredQty = num,
                         RequiredSerialCount = GetRequiredSerialCount(num),
                         RegisteredSerialCount = list2.Count,
                         LotNumber = d.LotNumber,
                         ExpiryDate = d.ExpiryDate,
                         ExistingSerials = list2
                     };
                 }).ToList()
        };
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> QualityInspection(int? warehouseId)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        IQueryable<Voucher> query = from v in _db.Vouchers.AsNoTracking().Include((Voucher v) => v.Warehouse).Include((Voucher v) => v.Partner)
                .Include((Voucher v) => v.Details)
                                    where !v.IsCancelled && (v.VoucherType == VoucherTypeEnum.NhapKho || v.VoucherType == VoucherTypeEnum.KhachTra || v.VoucherType == VoucherTypeEnum.NhapThanhPham) && (v.InboundStatus == InboundStatusEnum.PendingApproval || v.InboundStatus == InboundStatusEnum.Receiving)
                                    select v;
        if (warehouseId.HasValue)
        {
            query = query.Where((Voucher v) => v.WarehouseId == ((int?)warehouseId).Value);
        }
        List<Voucher> vouchers = await query.OrderBy((Voucher v) => v.ExpectedArrivalAt ?? v.VoucherDate).Take(200).ToListAsync();
        List<long> voucherIds = vouchers.Select((Voucher v) => v.VoucherId).ToList();
        List<QualityInspection> inspections = await (from qi in _db.QualityInspections.AsNoTracking()
                                                     where voucherIds.Contains(qi.VoucherId)
                                                     select qi).ToListAsync();
        List<int> partnerIds = (from v in vouchers
                                where v.PartnerId.HasValue
                                select v.PartnerId.GetValueOrDefault()).Distinct().ToList();
        Dictionary<int, Partner> partners = await (from p in _db.Partners.AsNoTracking()
                                                   where partnerIds.Contains(p.PartnerId)
                                                   select p).ToDictionaryAsync((Partner p) => p.PartnerId);
        base.ViewBag.Warehouses = (await (from w in _db.Warehouses.AsNoTracking()
                                          where w.IsActive
                                          orderby w.WarehouseCode
                                          select w).ToListAsync());
        base.ViewBag.WarehouseId = warehouseId;
        base.ViewBag.ExistingInspections = inspections;
        base.ViewBag.Partners = partners;
        return View(vouchers);
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> Receiving(int? warehouseId, byte? status, string? search)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        IQueryable<Voucher> query = from v in _db.Vouchers.AsNoTracking().Include((Voucher v) => v.Warehouse).Include((Voucher v) => v.Partner)
                                    where !v.IsCancelled && (v.VoucherType == VoucherTypeEnum.NhapKho || v.VoucherType == VoucherTypeEnum.KhachTra || v.VoucherType == VoucherTypeEnum.NhapThanhPham)
                                    select v;
        if (warehouseId.HasValue)
        {
            query = query.Where((Voucher v) => v.WarehouseId == ((int?)warehouseId).Value);
        }
        if (status.HasValue)
        {
            query = query.Where((Voucher v) => (byte)v.InboundStatus == ((byte?)status).Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();
            query = query.Where((Voucher v) => v.VoucherCode.Contains(keyword) || (v.AsnCode != null && v.AsnCode.Contains(keyword)) || (v.DockDoor != null && v.DockDoor.Contains(keyword)) || (v.VehicleNumber != null && v.VehicleNumber.Contains(keyword)) || (v.CarrierName != null && v.CarrierName.Contains(keyword)) || (v.Partner != null && v.Partner.PartnerName.Contains(keyword)));
        }
        List<InboundReceivingRow> rows = await (from v in (from v in query
                                                           orderby (v.InboundStatus == InboundStatusEnum.Receiving) ? 0 : 1, v.ExpectedArrivalAt ?? v.VoucherDate
                                                           select v).Take(300)
                                                select new InboundReceivingRow
                                                {
                                                    VoucherId = v.VoucherId,
                                                    VoucherCode = v.VoucherCode,
                                                    AsnCode = v.AsnCode,
                                                    WarehouseName = ((v.Warehouse != null) ? v.Warehouse.WarehouseName : ""),
                                                    PartnerName = ((v.Partner != null) ? v.Partner.PartnerName : null),
                                                    VoucherDate = v.VoucherDate,
                                                    ExpectedArrivalAt = v.ExpectedArrivalAt,
                                                    DockAppointmentStart = v.DockAppointmentStart,
                                                    DockAppointmentEnd = v.DockAppointmentEnd,
                                                    DockDoor = v.DockDoor,
                                                    VehicleNumber = v.VehicleNumber,
                                                    CarrierName = v.CarrierName,
                                                    InboundStatus = v.InboundStatus,
                                                    TotalLines = v.TotalLines,
                                                    IsPosted = v.IsPosted,
                                                    CreatedBy = v.CreatedBy,
                                                    SubmittedBy = v.SubmittedBy,
                                                    SubmittedAt = v.SubmittedAt
                                                }).ToListAsync();
        await DecorateInboundSerialSummaryAsync(rows);
        base.ViewBag.Warehouses = (await (from w in _db.Warehouses.AsNoTracking()
                                          where w.IsActive
                                          orderby w.WarehouseCode
                                          select w).ToListAsync());
        base.ViewBag.WarehouseId = warehouseId;
        base.ViewBag.Status = status;
        base.ViewBag.Search = search;
        return View(rows);
    }


    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> InboundApprovals(int? warehouseId, string? search)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }

        IQueryable<Voucher> query = from v in _db.Vouchers.AsNoTracking().Include((Voucher v) => v.Warehouse).Include((Voucher v) => v.Partner)
                                    where !v.IsCancelled
                                        && (v.VoucherType == VoucherTypeEnum.NhapKho || v.VoucherType == VoucherTypeEnum.KhachTra || v.VoucherType == VoucherTypeEnum.NhapThanhPham)
                                        && v.InboundStatus == InboundStatusEnum.PendingApproval
                                    select v;

        if (warehouseId.HasValue)
        {
            query = query.Where((Voucher v) => v.WarehouseId == ((int?)warehouseId).Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();
            query = query.Where((Voucher v) =>
                v.VoucherCode.Contains(keyword)
                || (v.AsnCode != null && v.AsnCode.Contains(keyword))
                || (v.ReferenceNo != null && v.ReferenceNo.Contains(keyword))
                || (v.DockDoor != null && v.DockDoor.Contains(keyword))
                || (v.VehicleNumber != null && v.VehicleNumber.Contains(keyword))
                || (v.CarrierName != null && v.CarrierName.Contains(keyword))
                || (v.Partner != null && v.Partner.PartnerName.Contains(keyword)));
        }

        var rows = await (from v in (from v in query
                                     orderby v.SubmittedAt ?? v.CreatedAt, v.ExpectedArrivalAt ?? v.VoucherDate
                                     select v).Take(300)
                          select new InboundReceivingRow
                          {
                              VoucherId = v.VoucherId,
                              VoucherCode = v.VoucherCode,
                              AsnCode = v.AsnCode,
                              WarehouseName = ((v.Warehouse != null) ? v.Warehouse.WarehouseName : ""),
                              PartnerName = ((v.Partner != null) ? v.Partner.PartnerName : null),
                              VoucherDate = v.VoucherDate,
                              ExpectedArrivalAt = v.ExpectedArrivalAt,
                              DockAppointmentStart = v.DockAppointmentStart,
                              DockAppointmentEnd = v.DockAppointmentEnd,
                              DockDoor = v.DockDoor,
                              VehicleNumber = v.VehicleNumber,
                              CarrierName = v.CarrierName,
                              InboundStatus = v.InboundStatus,
                              TotalLines = v.TotalLines,
                              IsPosted = v.IsPosted,
                              CreatedBy = v.CreatedBy,
                              SubmittedBy = v.SubmittedBy,
                              SubmittedAt = v.SubmittedAt
                          }).ToListAsync();

        await DecorateInboundSerialSummaryAsync(rows);

        var warehouses = await (from w in _db.Warehouses.AsNoTracking()
                                where w.IsActive
                                orderby w.WarehouseCode
                                select w).ToListAsync();
        if (scopedWh.HasValue)
        {
            warehouses = warehouses.Where(w => w.WarehouseId == scopedWh.Value).ToList();
        }

        return View(new InboundApprovalQueueViewModel
        {
            WarehouseId = warehouseId,
            Search = search,
            Warehouses = warehouses,
            Rows = rows
        });
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> SerialLookup(string? search, int? warehouseId, byte? status)
    {
        base.ViewBag.Warehouses = (await (from w in _db.Warehouses
                                          where w.IsActive
                                          orderby w.WarehouseCode
                                          select w).ToListAsync());
        base.ViewBag.Search = search;
        base.ViewBag.WarehouseId = warehouseId;
        base.ViewBag.Status = status;
        IQueryable<SerialNumber> query = _db.SerialNumbers.Include((SerialNumber s) => s.Item).Include((SerialNumber s) => s.Location).Include((SerialNumber s) => s.Warehouse)
            .Include((SerialNumber s) => s.Voucher)
            .Include((SerialNumber s) => s.LicensePlate)
            .Include((SerialNumber s) => s.ConsumedPickTask)
            .ThenInclude((PickTask? t) => t!.Voucher)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where((SerialNumber s) => (s.SerialCode != null && s.SerialCode.Contains(search)) || (s.Item != null && (s.Item.ItemCode.Contains(search) || s.Item.ItemName.Contains(search))) || (s.Location != null && s.Location.LocationCode.Contains(search)) || (s.Voucher != null && s.Voucher.VoucherCode.Contains(search)) || (s.LicensePlate != null && s.LicensePlate.LpnCode.Contains(search)));
        }
        if (warehouseId.HasValue)
        {
            query = query.Where((SerialNumber s) => s.WarehouseId == ((int?)warehouseId).Value);
        }
        if (status.HasValue)
        {
            query = query.Where((SerialNumber s) => (byte)s.Status == ((byte?)status).Value);
        }
        List<SerialLookupRow> rows = (await query.OrderByDescending((SerialNumber s) => s.CreatedAt).Take(100).ToListAsync()).Select((SerialNumber s) => new SerialLookupRow
        {
            SerialNumberId = s.SerialNumberId,
            SerialCode = (s.SerialCode ?? ""),
            WarehouseId = s.WarehouseId,
            WarehouseName = (s.Warehouse?.WarehouseName ?? ""),
            VoucherId = s.VoucherId,
            VoucherCode = (s.Voucher?.VoucherCode ?? ""),
            ConsumedVoucherId = s.ConsumedPickTask?.VoucherId,
            ConsumedVoucherCode = s.ConsumedPickTask?.Voucher?.VoucherCode,
            ItemId = s.ItemId,
            ItemCode = (s.Item?.ItemCode ?? ""),
            ItemName = (s.Item?.ItemName ?? ""),
            LocationCode = s.Location?.LocationCode,
            LpnCode = s.LicensePlate?.LpnCode,
            LotNumber = s.LotNumber,
            ExpiryDate = s.ExpiryDate,
            Status = s.Status,
            CreatedAt = s.CreatedAt,
            ConsumedAt = s.ConsumedAt
        }).ToList();
        return View(rows);
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> SerialReceiving(long? voucherId, long? id = null)
    {
        long? actualVoucherId = voucherId ?? id;
        if (!actualVoucherId.HasValue)
        {
            base.TempData["Error"] = "Vui lòng chọn phiếu nhập cần nhận số sê-ri.";
            return RedirectToAction("RfReceiving");
        }
        SerialReceivingPageViewModel? model = await BuildSerialReceivingPageAsync(actualVoucherId.Value, GetScopedWarehouseId());
        if (model == null)
        {
            base.TempData["Error"] = "Không tìm thấy phiếu nhập hoặc bạn không có quyền truy cập phiếu này.";
            return RedirectToAction("RfReceiving");
        }
        return View(model);
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpGet]
    public async Task<IActionResult> RfReceiving(int? warehouseId, string? search = null, string? dock = null)
    {
        int? scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
        {
            warehouseId = scopedWh.Value;
        }
        IQueryable<Voucher> vouchersQuery = from v in _db.Vouchers.Include((Voucher v) => v.Details).ThenInclude((VoucherDetail d) => d.Item).Include((Voucher v) => v.Partner)
                .Include((Voucher v) => v.Warehouse)
                                            where v.VoucherType == VoucherTypeEnum.NhapKho && !v.IsPosted && !v.IsCancelled && (v.InboundStatus == InboundStatusEnum.Approved || v.InboundStatus == InboundStatusEnum.Receiving)
                                            select v;
        if (warehouseId.HasValue)
        {
            vouchersQuery = vouchersQuery.Where((Voucher v) => v.WarehouseId == ((int?)warehouseId).Value);
        }
        List<string> activeDocks = await vouchersQuery
            .Where((Voucher v) => v.DockDoor != null && v.DockDoor != "")
            .Select((Voucher v) => v.DockDoor!)
            .Distinct()
            .OrderBy((string d) => d)
            .ToListAsync();
        if (!string.IsNullOrWhiteSpace(dock))
        {
            string dockCode = dock.Trim();
            vouchersQuery = vouchersQuery.Where((Voucher v) => v.DockDoor == dockCode);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();
            vouchersQuery = vouchersQuery.Where((Voucher v) => v.VoucherCode.Contains(keyword)
                || (v.AsnCode != null && v.AsnCode.Contains(keyword))
                || (v.DockDoor != null && v.DockDoor.Contains(keyword))
                || (v.VehicleNumber != null && v.VehicleNumber.Contains(keyword))
                || (v.CarrierName != null && v.CarrierName.Contains(keyword))
                || (v.Partner != null && v.Partner.PartnerName.Contains(keyword)));
        }
        List<InboundReceivingRow> rows = (await vouchersQuery.OrderByDescending((Voucher v) => v.VoucherDate).Take(100).ToListAsync()).Select((Voucher v) => new InboundReceivingRow
        {
            VoucherId = v.VoucherId,
            VoucherCode = v.VoucherCode,
            AsnCode = v.AsnCode,
            WarehouseName = (v.Warehouse?.WarehouseName ?? ""),
            PartnerName = v.Partner?.PartnerName,
            VoucherDate = v.VoucherDate,
            ExpectedArrivalAt = v.ExpectedArrivalAt,
            DockAppointmentStart = v.DockAppointmentStart,
            DockAppointmentEnd = v.DockAppointmentEnd,
            DockDoor = v.DockDoor,
            VehicleNumber = v.VehicleNumber,
            CarrierName = v.CarrierName,
            InboundStatus = v.InboundStatus,
            TotalLines = v.Details.Count,
            IsPosted = v.IsPosted,
            HasSerialTrackedLines = v.Details.Any((VoucherDetail d) => d.Item != null && d.Item.TrackSerial),
            PendingSerialCount = 0,
            RequiredSerialCount = 0
        }).ToList();
        await DecorateInboundSerialSummaryAsync(rows);
        base.ViewBag.WarehouseId = warehouseId;
        base.ViewBag.Search = search;
        base.ViewBag.CurrentDock = dock;
        base.ViewBag.ActiveDocks = activeDocks;
        return View(rows);
    }

}
