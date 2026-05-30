using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;

using Microsoft.EntityFrameworkCore;

using WMS.Data;

using WMS.Models;

using WMS.ViewModels;

using WMS.Authorization;

using WMS.Common;

using WMS.Services;

using System.Text.Json;

using System.Linq;

using ClosedXML.Excel;

using System.Globalization;

using System.Data;

using Microsoft.Extensions.Logging.Abstractions;

namespace WMS.Controllers;

public partial class VouchersController
{

    public async Task<IActionResult> Index(VoucherTypeEnum? type, InboundStatusEnum? inboundStatus, DateTime? dateFrom, DateTime? dateTo, string? search, int page = 1, int pageSize = 50)
    {
        if (page < 1) page = 1;
        if (pageSize < 5) pageSize = 5;
        if (pageSize > 200) pageSize = 200;

        var query = _db.Vouchers.AsNoTracking()
            .Include(v => v.Warehouse).Include(v => v.Partner)
            .Include(v => v.Details)
            .AsQueryable();

        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue)
            query = query.Where(v => v.WarehouseId == scopedWh.Value);

        if (type.HasValue)
            query = query.Where(v => v.VoucherType == type.Value);
        if (inboundStatus.HasValue)
            query = query.Where(v =>
                (v.VoucherType == VoucherTypeEnum.NhapKho || v.VoucherType == VoucherTypeEnum.KhachTra || v.VoucherType == VoucherTypeEnum.NhapThanhPham)
                && v.InboundStatus == inboundStatus.Value);
        if (dateFrom.HasValue)
            query = query.Where(v => v.VoucherDate >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(v => v.VoucherDate <= dateTo.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(v =>
                v.VoucherCode.Contains(search)
                || (v.AsnCode != null && v.AsnCode.Contains(search))
                || (v.Description != null && v.Description.Contains(search))
                || (v.ReferenceNo != null && v.ReferenceNo.Contains(search))
                || (v.DockDoor != null && v.DockDoor.Contains(search))
                || (v.VehicleNumber != null && v.VehicleNumber.Contains(search)));

        // Total count for pagination
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var vouchers = await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        ViewBag.Type = type;
        ViewBag.InboundStatus = inboundStatus;
        ViewBag.DateFrom = dateFrom;
        ViewBag.DateTo = dateTo;
        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = totalPages;
        ViewBag.HasPrevious = page > 1;
        ViewBag.HasNext = page < totalPages;

        return View(vouchers);
    }


    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> Create(VoucherTypeEnum type = VoucherTypeEnum.NhapKho)
    {
        var scopedWh = GetScopedWarehouseId();
        var vm = new VoucherCreateViewModel
        {
            VoucherType = type,
            Warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync(),
            Partners = await _db.Partners.Where(p => p.IsActive).ToListAsync(),
            OwnerPartners = await _db.Partners.Where(p => p.IsThreePlClient && p.IsActive).OrderBy(p => p.PartnerCode).ToListAsync(),
            Items = await _db.Items
                .Include(i => i.BaseUom)
                .Where(i => i.IsActive)
                .OrderBy(i => i.ItemCode)
                .ToListAsync(),
            Uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).ToListAsync(),
            Locations = await _db.Locations.Where(l => l.IsActive).ToListAsync(),
            PackagingUnits = await _db.PackagingUnits
                .Include(p => p.BaseUom)
                .Where(p => p.IsActive)
                .OrderBy(p => p.TenDongGoi)
                .ToListAsync()
        };

        if (scopedWh.HasValue)
            vm.WarehouseId = scopedWh.Value;
        if (IsInboundVoucherType(type))
        {
            vm.ExpectedArrivalAt = VietnamNow.AddHours(4);
            vm.DockAppointmentStart = VietnamNow.AddHours(3.5);
            vm.DockAppointmentEnd = VietnamNow.AddHours(5);
        }
        ViewBag.CanSeeFinancial = CanSeeFinancial();
        await PopulateVoucherCreateMetadataAsync(vm);
        return View(vm);
    }


    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> Create(VoucherCreateViewModel vm, bool submit = false)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var scopedWh = GetScopedWarehouseId();
            if (scopedWh.HasValue && vm.WarehouseId != scopedWh.Value)
                return Forbid();

            if (!ModelState.IsValid)
            {
                vm.Warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync();
                vm.Partners = await _db.Partners.Where(p => p.IsActive).ToListAsync();
                vm.OwnerPartners = await _db.Partners.Where(p => p.IsThreePlClient && p.IsActive).OrderBy(p => p.PartnerCode).ToListAsync();
                vm.Items = await _db.Items.Include(i => i.BaseUom).Where(i => i.IsActive).OrderBy(i => i.ItemCode).ToListAsync();
                vm.Uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).ToListAsync();
                vm.Locations = await _db.Locations.Where(l => l.IsActive).ToListAsync();
                ViewBag.CanSeeFinancial = CanSeeFinancial();
                await PopulateVoucherCreateMetadataAsync(vm);
                return View(vm);
            }

            if (RequiresPartner(vm.VoucherType, vm.ExportMode) && !vm.PartnerId.HasValue)
                throw WmsExceptions.RequiredPartner();
            await EnsureVoucherOwnerScopeAsync(vm.OwnerPartnerId);
            if (vm.OwnerPartnerId.HasValue)
            {
                var ownerOk = await _db.Partners.AnyAsync(p => p.PartnerId == vm.OwnerPartnerId.Value && p.IsThreePlClient && p.IsActive);
                if (!ownerOk)
                    throw new BusinessRuleException("Chủ hàng kho nhiều chủ hàng không hợp lệ.", "TENANT_OWNER_INVALID", "Voucher");
            }

            var createVoucherDate = VietnamNow.Date;
            var lockDate = await GetActiveLockDateAsync(vm.WarehouseId);
            if (IsLocked(createVoucherDate, lockDate))
                throw WmsExceptions.WarehouseLocked(createVoucherDate.ToString("dd/MM/yyyy"), lockDate!.Value);

            // Basic cross-field validations for correctness
            if (vm.VoucherType == VoucherTypeEnum.ChuyenKho) // Transfer
            {
                if (!vm.DestWarehouseId.HasValue || vm.DestWarehouseId.Value <= 0)
                    throw WmsExceptions.DestinationWarehouseRequired();
                if (vm.DestWarehouseId.Value == vm.WarehouseId)
                    throw WmsExceptions.DestinationWarehouseSameAsSource();
            }

            vm.ReferenceNo = NormalizeText(vm.ReferenceNo);
            vm.Description = NormalizeText(vm.Description);
            vm.CarrierName = NormalizeText(vm.CarrierName);
            vm.VehicleNumber = NormalizeText(vm.VehicleNumber, toUpper: true);
            vm.DriverName = NormalizeText(vm.DriverName);
            vm.DriverPhone = NormalizeText(vm.DriverPhone);
            vm.DockDoor = NormalizeText(vm.DockDoor, toUpper: true);

            await ValidateInboundPlanningAsync(
                vm.VoucherType,
                vm.WarehouseId,
                submit,
                vm.ExpectedArrivalAt,
                vm.DockAppointmentStart,
                vm.DockAppointmentEnd,
                vm.DockDoor);

            if (IsInboundVoucherType(vm.VoucherType)
                && string.IsNullOrWhiteSpace(vm.DockDoor)
                && vm.DockAppointmentStart.HasValue
                && vm.DockAppointmentEnd.HasValue)
            {
                vm.DockDoor = await SuggestAvailableDockDoorAsync(vm.WarehouseId, vm.DockAppointmentStart, vm.DockAppointmentEnd);
            }

            // Generate voucher code
            var prefix = vm.VoucherType switch
            {
                VoucherTypeEnum.NhapKho => "PN",
                VoucherTypeEnum.XuatKho => "PX",
                VoucherTypeEnum.TraNCC => "PTN",
                VoucherTypeEnum.KhachTra => "PTK",
                VoucherTypeEnum.DieuChinh => "PDC",
                VoucherTypeEnum.ChuyenKho => "PCK",
                VoucherTypeEnum.NhapThanhPham => "NTP",
                VoucherTypeEnum.XuatSanXuat => "XSX",
                _ => "PH"
            };
            var dateStr = VietnamNow.ToString("yyyyMMdd");
            Voucher? voucher = null;
            var voucherCode = "";
            for (int attempt = 0; attempt < 10; attempt++)
            {
                // FIX: Move CountAsync inside retry loop to avoid race condition
                var baseSeq = await _db.Vouchers
                    .Where(v => v.VoucherCode.StartsWith(prefix + "-" + dateStr))
                    .CountAsync();
                var seq = baseSeq + 1 + attempt;
                voucherCode = $"{prefix}-{dateStr}-{seq:D5}";

                voucher = new Voucher
                {
                    VoucherCode = voucherCode,
                    VoucherType = vm.VoucherType,
                    VoucherDate = createVoucherDate,
                    WarehouseId = vm.WarehouseId,
                    OwnerPartnerId = vm.OwnerPartnerId,
                    DestWarehouseId = vm.DestWarehouseId,
                    PartnerId = vm.PartnerId,
                    ReferenceNo = vm.ReferenceNo,
                    Description = vm.Description,
                    SourceType = SourceTypeEnum.Manual,
                    CreatedBy = User.Identity?.Name ?? "system",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ParentVoucherId = vm.ParentVoucherId,
                    RequestedDeliveryDate = vm.RequestedDeliveryDate,
                    AsnCode = IsInboundVoucherType(vm.VoucherType) ? await GenerateNextAsnCodeAsync() : null,
                    ExpectedArrivalAt = vm.ExpectedArrivalAt,
                    CarrierName = vm.CarrierName,
                    VehicleNumber = vm.VehicleNumber,
                    DriverName = vm.DriverName,
                    DriverPhone = vm.DriverPhone,
                    DockAppointmentStart = vm.DockAppointmentStart,
                    DockAppointmentEnd = vm.DockAppointmentEnd,
                    DockDoor = vm.DockDoor,
                    // P2.5: Advanced allocation
                    ServiceLevel = vm.ServiceLevel,
                    Priority = vm.Priority,
                    PartialShipmentAllowed = vm.PartialShipmentAllowed,
                    SlaCode = vm.SlaCode,
                    SlaHours = vm.SlaHours,
                    IsPosted = false,
                    FulfillmentStatus = FulfillmentStatusEnum.Draft,
                    InboundStatus = (vm.VoucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham)
                        ? (submit ? InboundStatusEnum.PendingApproval : InboundStatusEnum.Draft)
                        : InboundStatusEnum.Draft,
                    SubmittedBy = submit ? (User.Identity?.Name ?? "system") : null,
                    SubmittedAt = submit ? VietnamNow : null
                };

                _db.Vouchers.Add(voucher);
                try
                {
                    await _unitOfWork.SaveChangesAsync();
                    break;
                }
                catch (DbUpdateException ex) when ((ex.InnerException?.Message?.Contains("2601") ?? false)
                    || (ex.InnerException?.Message?.Contains("2627") ?? false)
                    || (ex.InnerException?.Message?.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ?? false)
                    || (ex.InnerException?.Message?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    _db.Entry(voucher).State = EntityState.Detached;
                    voucher = null;
                    if (attempt == 9)
                        throw WmsExceptions.CodeGenerationFailed("phiếu");
                }
            }
            if (voucher == null || string.IsNullOrWhiteSpace(voucherCode))
                throw WmsExceptions.VoucherCodeFailed();

            // If this is a replenishment, find the original voucher and resolve its defects
            if (vm.ParentVoucherId.HasValue)
            {
                var original = await _db.Vouchers.Include(v => v.Details)
                    .FirstOrDefaultAsync(v => v.VoucherId == vm.ParentVoucherId.Value);
                if (original != null)
                {
                    foreach (var d in original.Details.Where(x => x.DefectQty > 0))
                    {
                        d.Notes = (d.Notes ?? "") + $" [Replenished by {voucherCode}]";
                        d.DefectQty = 0; // Resolving the defect qty marks the original as "Completed" (No longer partial)
                    }
                    original.Description = (original.Description ?? "") + $" [Replenished by {voucherCode}]";
                    _db.Vouchers.Update(original);
                }
            }

            decimal totalAmount = 0;
            int lineNum = 0;
            var canSeeFinancial = CanSeeFinancial();

            // Theo dõi các mặt hàng được đưa vào vị trí trong cùng 1 phiếu để chặn lỗi "trùng ô ngay trong 1 phiếu"
            var locationsUsedInThisVoucher = new Dictionary<int, int>();
            var locationsAddedWeightThisVoucher = new Dictionary<int, decimal>();

            // ═══ Bulk-load Items & UnitConversions để tránh N+1 query ═══
            var lineItemIds = vm.Lines.Where(l => l.ItemId > 0 && l.TransactionQty > 0).Select(l => l.ItemId).Distinct().ToList();
            var itemsDict = await _db.Items.Where(i => lineItemIds.Contains(i.ItemId)).ToDictionaryAsync(i => i.ItemId, i => i);
            var runningStockByItem = await _inventoryBalanceService.GetStockByItemAsync(null, lineItemIds);
            var allConversions = await _db.UnitConversions
                .Where(uc => uc.IsActive && (uc.ItemId == null || lineItemIds.Contains(uc.ItemId.Value)))
                .ToListAsync();
            // Pre-load location stock totals cho tất cả location liên quan (dùng cho capacity check)
            var lineLocationIds = vm.Lines.Where(l => l.LocationId.HasValue && l.LocationId > 0).Select(l => l.LocationId!.Value)
                .Union(vm.Lines.Where(l => l.DestLocationId.HasValue && l.DestLocationId > 0).Select(l => l.DestLocationId!.Value))
                .Distinct().ToList();
            var locationStockTotals = lineLocationIds.Count > 0
                ? await _db.ItemLocations.Where(il => lineLocationIds.Contains(il.LocationId))
                    .GroupBy(il => il.LocationId)
                    .Select(g => new { LocationId = g.Key, Total = g.Sum(il => il.Quantity) })
                    .ToDictionaryAsync(x => x.LocationId, x => x.Total)
                : new Dictionary<int, decimal>();
            // Pre-load occupied locations to avoid N+1 query for single-item-per-location check
            var occupiedLocations = lineLocationIds.Count > 0
                ? await _db.ItemLocations
                    .Include(il => il.Item)
                    .Where(il => lineLocationIds.Contains(il.LocationId) && il.Quantity > 0)
                    .ToListAsync()
                : new List<ItemLocation>();

            // P0-2: KHÔNG silent-drop dòng có TransactionQty <= 0.
            // Phiếu Điều Chỉnh dùng AdjustSign để biểu diễn tăng/giảm, qty bản thân vẫn phải dương.
            var invalidQtyLine = vm.Lines
                .Select((l, idx) => new { l, idx })
                .FirstOrDefault(x => x.l.ItemId > 0 && x.l.TransactionQty <= 0);
            if (invalidQtyLine != null)
                throw WmsExceptions.Validation($"Dòng {invalidQtyLine.idx + 1}: Số lượng phải lớn hơn 0.");

            foreach (var line in vm.Lines.Where(l => l.ItemId > 0))
            {
                lineNum++;
                if (!itemsDict.TryGetValue(line.ItemId, out var item))
                    throw WmsExceptions.ItemNotFound(line.ItemId);

                // BaseQty luôn lấy theo bảng UnitConversion (item-specific ưu tiên hơn global).
                var sourceUomId = line.TransactionUomId > 0 ? line.TransactionUomId : item.BaseUomId;
                var resolvedRate = ResolveConversionRate(allConversions, item.ItemId, sourceUomId, item.BaseUomId);
                if (!resolvedRate.HasValue)
                    throw WmsExceptions.UnitConversionNotFound(item.ItemCode);
                var baseQty = line.TransactionQty * resolvedRate.Value;
                if (line.DestQty > 0 && Math.Abs(line.DestQty - resolvedRate.Value) > 0.000001m)
                    throw WmsExceptions.ConversionRateMismatch(item.ItemCode);

                // VoucherType 5 (Điều chỉnh): apply sign (Tăng/Giảm) without changing input rules
                if (vm.VoucherType == VoucherTypeEnum.DieuChinh && line.AdjustSign < 0)
                {
                    baseQty = -baseQty;
                }

                // ═══ BR-INB-003: Hàng có HSD/Lô phải nhập bắt buộc khi nhập kho ═══
                var isInboundVoucher = vm.VoucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham;
                if (isInboundVoucher)
                {
                    if (item.TrackExpiry && !line.ExpiryDate.HasValue)
                        throw WmsExceptions.HsdRequired(item.ItemCode);
                    if (item.TrackLot && string.IsNullOrWhiteSpace(line.LotNumber))
                        throw WmsExceptions.LotRequired(item.ItemCode);
                }

                // Transfer must have both source and destination locations
                if (vm.VoucherType == VoucherTypeEnum.ChuyenKho)
                {
                    if (!line.LocationId.HasValue || line.LocationId.Value <= 0)
                        throw WmsExceptions.LocationRequired(item.ItemCode);
                    if (!line.DestLocationId.HasValue || line.DestLocationId.Value <= 0)
                        throw WmsExceptions.DestinationLocationRequired(item.ItemCode);
                    if (line.DestLocationId.Value == line.LocationId.Value)
                        throw WmsExceptions.DestinationLocationSameAsSource(item.ItemCode);

                    // Validate source/destination locations belong to selected warehouses
                    var srcWhId = await _db.Locations.AsNoTracking()
                        .Include(l => l.Zone)
                        .Where(l => l.LocationId == line.LocationId.Value && l.Zone != null)
                        .Select(l => (int?)l.Zone!.WarehouseId)
                        .FirstOrDefaultAsync();
                    if (!srcWhId.HasValue || srcWhId.Value != vm.WarehouseId)
                        throw WmsExceptions.LocationNotInWarehouse(item.ItemCode, "nguồn");

                    var destWhId = await _db.Locations.AsNoTracking()
                        .Include(l => l.Zone)
                        .Where(l => l.LocationId == line.DestLocationId.Value && l.Zone != null)
                        .Select(l => (int?)l.Zone!.WarehouseId)
                        .FirstOrDefaultAsync();
                    if (!destWhId.HasValue || !vm.DestWarehouseId.HasValue || destWhId.Value != vm.DestWarehouseId.Value)
                        throw WmsExceptions.LocationDestinationNotInWarehouse(item.ItemCode, "đích");
                }

                // Auto-discover the single fixed location for this item
                if (line.LocationId == null || line.LocationId == 0)
                {
                    // FEFO auto-pick for exports / returns-to-supplier / production-issue / transfer-out / adjustment-decrease
                    var needsSourceStock = vm.VoucherType is VoucherTypeEnum.XuatKho or VoucherTypeEnum.TraNCC or VoucherTypeEnum.XuatSanXuat or VoucherTypeEnum.ChuyenKho || (vm.VoucherType == VoucherTypeEnum.DieuChinh && baseQty < 0);
                    if (needsSourceStock)
                    {
                        var required = Math.Abs(baseQty);
                        var fefoPick = await GetFefoLocationForSingleLineAsync(item.ItemId, vm.WarehouseId, required);
                        if (fefoPick != null)
                        {
                            line.LocationId = fefoPick.LocationId;
                            // FEFO must carry the exact lot/expiry that was chosen, otherwise later stock updates
                            // may hit the wrong batch row or create a null-batch phantom row.
                            line.LotNumber = fefoPick.LotNumber;
                            line.ExpiryDate = fefoPick.ExpiryDate;
                        }
                    }

                    if (!needsSourceStock && (line.LocationId == null || line.LocationId == 0) && item.DefaultLocationId.HasValue && item.DefaultLocationId > 0)
                    {
                        line.LocationId = item.DefaultLocationId;
                    }
                    else if (!needsSourceStock && (line.LocationId == null || line.LocationId == 0))
                    {
                        // Fallback chỉ chạy khi FEFO chưa chọn được location
                        var defaultLoc = await _db.ItemLocations
                            .Include(il => il.Location).ThenInclude(l => l!.Zone)
                            .Where(il => il.ItemId == line.ItemId
                                && il.Location != null
                                && il.Location.Zone != null
                                && il.Location.Zone.WarehouseId == vm.WarehouseId)
                            .Select(il => il.LocationId)
                            .FirstOrDefaultAsync();
                        if (defaultLoc == 0)
                        {
                            defaultLoc = await _db.Locations
                                .Include(l => l.Zone)
                                .Where(l => l.Zone != null && l.Zone.WarehouseId == vm.WarehouseId)
                                .Select(l => l.LocationId)
                                .FirstOrDefaultAsync();
                        }
                        line.LocationId = defaultLoc > 0 ? defaultLoc : null;
                    }
                }

                if (line.LocationId.HasValue && vm.VoucherType != VoucherTypeEnum.ChuyenKho)
                {
                    var lineWhId = await GetLocationWarehouseIdAsync(line.LocationId.Value);
                    if (!lineWhId.HasValue || lineWhId.Value != vm.WarehouseId)
                        throw WmsExceptions.LocationRequired(item.ItemCode);
                }

                var isChemical = item.ItemType == ItemTypeEnum.HoaChat;
                var unitWeight = item.Weight ?? 1m;
                var weightAdded = isChemical ? baseQty : baseQty * unitWeight;
                var maxCapacity = isChemical ? SecurityHelpers.WarehouseCapacity.MaxChemicalLiters : SecurityHelpers.WarehouseCapacity.MaxStorageKg;
                var unitName = isChemical ? SecurityHelpers.WarehouseCapacity.VolumeUnit : SecurityHelpers.WarehouseCapacity.WeightUnit;

                if ((vm.VoucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham || (vm.VoucherType == VoucherTypeEnum.DieuChinh && baseQty > 0)) && line.LocationId.HasValue)
                {
                    var currentStock = locationStockTotals.TryGetValue(line.LocationId.Value, out var cs1) ? cs1 : 0m;
                    var localAdded = locationsAddedWeightThisVoucher.ContainsKey(line.LocationId.Value) ? locationsAddedWeightThisVoucher[line.LocationId.Value] : 0;

                    var totalExpectedWeight = (isChemical ? currentStock : currentStock * unitWeight) + localAdded + weightAdded;
                    if (totalExpectedWeight > maxCapacity) throw WmsExceptions.CapacityExceeded(line.LocationId.Value.ToString(), maxCapacity, unitName, item.ItemCode, totalExpectedWeight);

                    locationsAddedWeightThisVoucher[line.LocationId.Value] = localAdded + weightAdded;
                }
                else if (vm.VoucherType == VoucherTypeEnum.ChuyenKho && line.DestLocationId.HasValue)
                {
                    var currentStock = locationStockTotals.TryGetValue(line.DestLocationId.Value, out var cs2) ? cs2 : 0m;
                    var localAdded = locationsAddedWeightThisVoucher.ContainsKey(line.DestLocationId.Value) ? locationsAddedWeightThisVoucher[line.DestLocationId.Value] : 0;

                    var totalExpectedWeight = (isChemical ? currentStock : currentStock * unitWeight) + localAdded + weightAdded;
                    if (totalExpectedWeight > maxCapacity) throw WmsExceptions.CapacityExceeded(line.DestLocationId.Value.ToString(), maxCapacity, unitName, item.ItemCode, totalExpectedWeight);

                    locationsAddedWeightThisVoucher[line.DestLocationId.Value] = localAdded + weightAdded;
                }

                var absBaseQty = Math.Abs(baseQty);
                decimal lineAmount;
                decimal lineUnitPrice;
                if (!canSeeFinancial)
                {
                    // Least privilege: staff/viewer không cung cấp/không nhìn financial fields.
                    // Backend luôn lấy giá vốn từ hệ thống (Item.UnitCost) để đảm bảo tính đúng nghiệp vụ.
                    if (vm.VoucherType == VoucherTypeEnum.XuatKho && vm.ExportMode == ExportModeEnum.Internal)
                    {
                        lineAmount = 0m;
                        lineUnitPrice = 0m;
                    }
                    else
                    {
                        lineUnitPrice = item.UnitCost;
                        lineAmount = item.UnitCost * absBaseQty;
                    }
                }
                else
                {
                    if (line.UnitPrice < 0)
                        throw WmsExceptions.NegativeUnitPrice(item.ItemCode);
                    if (line.LineAmount < 0)
                        throw WmsExceptions.NegativeLineAmount(item.ItemCode);

                    lineAmount = (vm.VoucherType == VoucherTypeEnum.XuatKho && vm.ExportMode == ExportModeEnum.Internal) ? 0 :
                        (line.LineAmount > 0 ? line.LineAmount : (line.UnitPrice * absBaseQty));
                    lineUnitPrice = absBaseQty > 0 ? lineAmount / absBaseQty : 0m;
                }

                var defectQty = (vm.VoucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham) ? Math.Max(0, line.DefectQty) : 0;
                if (defectQty > absBaseQty)
                    throw WmsExceptions.DefectQtyExceedsLineQty(item.ItemCode, defectQty, absBaseQty);

                // IMPORTANT: DefectBaseQty must be in base-stock units (same unit as BaseQty)
                var conversionRate = line.TransactionQty > 0 ? baseQty / line.TransactionQty : 1m;
                var defectBaseQty = defectQty * Math.Abs(conversionRate);
                if (defectBaseQty > absBaseQty)
                    throw WmsExceptions.DefectQtyExceedsLineQty(item.ItemCode, defectBaseQty, absBaseQty);

                var detail = new VoucherDetail
                {
                    VoucherId = voucher.VoucherId,
                    OwnerPartnerId = voucher.OwnerPartnerId,
                    ItemId = line.ItemId,
                    LocationId = line.LocationId,
                    DestLocationId = line.DestLocationId,
                    TransactionQty = line.TransactionQty,
                    TransactionUomId = line.TransactionUomId > 0 ? line.TransactionUomId : item.BaseUomId,
                    PackagingUnitId = line.PackagingUnitId,
                    ConversionRate = conversionRate,
                    BaseQty = baseQty,
                    UnitPrice = lineUnitPrice,
                    LineAmount = lineAmount,
                    QualityStatus = line.QualityStatus,
                    ExpiryDate = line.ExpiryDate,
                    LotNumber = string.IsNullOrWhiteSpace(line.LotNumber) ? null : line.LotNumber.Trim(),
                    Notes = line.Notes,
                    LineNumber = lineNum,
                    DefectQty = defectQty,
                    DefectBaseQty = defectBaseQty,
                    ManufacturingDate = line.ManufacturingDate
                };

                _db.VoucherDetails.Add(detail);
                totalAmount += lineAmount;

                // Cập nhật tồn kho NẾU PHIẾU Đã GHI SỔ
                if (voucher.IsPosted)
                {
                    // Điều chỉnh (5) cũng phải áp dụng check sức chứa giống nhập kho nếu là tăng tồn
                    if (vm.VoucherType == VoucherTypeEnum.DieuChinh && detail.BaseQty > 0 && detail.LocationId.HasValue)
                    {
                        var currentStock = locationStockTotals.TryGetValue(detail.LocationId.Value, out var cs3) ? cs3 : 0m;
                        var localAdded = locationsAddedWeightThisVoucher.ContainsKey(detail.LocationId.Value) ? locationsAddedWeightThisVoucher[detail.LocationId.Value] : 0;

                        var isChemicalAdj = item.ItemType == ItemTypeEnum.HoaChat;
                        var unitWeightAdj = item.Weight ?? 1m;
                        var weightAddedAdj = isChemicalAdj ? detail.BaseQty : detail.BaseQty * unitWeightAdj;
                        var maxCapacityAdj = isChemicalAdj ? SecurityHelpers.WarehouseCapacity.MaxChemicalLiters : SecurityHelpers.WarehouseCapacity.MaxStorageKg;
                        var unitNameAdj = isChemicalAdj ? SecurityHelpers.WarehouseCapacity.VolumeUnit : SecurityHelpers.WarehouseCapacity.WeightUnit;

                        var totalExpectedWeight = (isChemicalAdj ? currentStock : currentStock * unitWeightAdj) + localAdded + weightAddedAdj;
                        if (totalExpectedWeight > maxCapacityAdj)
                            throw WmsExceptions.CapacityExceeded(detail.LocationId.Value.ToString(), maxCapacityAdj, unitNameAdj, item.ItemCode, totalExpectedWeight);

                        locationsAddedWeightThisVoucher[detail.LocationId.Value] = localAdded + weightAddedAdj;
                    }

                    if (detail.LocationId.HasValue)
                    {
                        // Ràng buộc quy tắc: 1 ô chỉ chứa 1 vật tư
                        if ((vm.VoucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham) || (vm.VoucherType == VoucherTypeEnum.DieuChinh && detail.BaseQty > 0)) // Điều chỉnh TĂNG mới cần check đè ô
                        {
                            // 1. Kiểm tra ngay trong bộ nhớ của cái phiếu hiện tại (trường hợp ngta ghi 3 dòng khác nhau nhưng nhét vào cùng 1 ô)
                            if (locationsUsedInThisVoucher.TryGetValue(detail.LocationId.Value, out int usedItemId) && usedItemId != detail.ItemId)
                            {
                                var conflictNameLocal = itemsDict.TryGetValue(usedItemId, out var conflictItem) ? conflictItem.ItemCode : usedItemId.ToString();
                                throw WmsExceptions.OneLocationOneItemConflictLocal(item.ItemCode, conflictNameLocal);
                            }
                            locationsUsedInThisVoucher[detail.LocationId.Value] = detail.ItemId;

                            // 2. Kiểm tra dữ liệu đã có trong kho cũ (sử dụng bộ nhớ đã bulk-load)
                            var otherItemsInLocation = occupiedLocations
                                .FirstOrDefault(il => il.LocationId == detail.LocationId.Value
                                          && il.ItemId != detail.ItemId);

                            if (otherItemsInLocation != null)
                            {
                                var conflictName = otherItemsInLocation.Item != null ? otherItemsInLocation.Item.ItemCode : otherItemsInLocation.ItemId.ToString();
                                throw WmsExceptions.OneLocationOneItemConflict(item.ItemCode, conflictName, detail.LocationId.Value.ToString());
                            }
                        }

                        var itemLocation = await _db.ItemLocations
                            .FirstOrDefaultAsync(il => il.ItemId == detail.ItemId
                                && il.LocationId == detail.LocationId.Value
                                && il.LotNumber == detail.LotNumber
                                && il.ExpiryDate == detail.ExpiryDate);

                        if (itemLocation == null)
                        {
                            itemLocation = new ItemLocation
                            {
                                ItemId = detail.ItemId,
                                LocationId = detail.LocationId.Value,
                                Quantity = 0,
                                ExpiryDate = detail.ExpiryDate,
                                LotNumber = detail.LotNumber,
                                UpdatedAt = VietnamNow
                            };
                            _db.ItemLocations.Add(itemLocation);
                        }

                        // Nhập kho (1), Khách trả (4), Nhập TP (7)
                        if (vm.VoucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham)
                        {
                            // Posted receipts/returns add full BaseQty here (defects are handled on Approve stage for types 1/7),
                            // but for VoucherType 4 (Khách Trả) this is already posted immediately => treat as all good.
                            itemLocation.Quantity += detail.BaseQty;
                        }
                        // Xuất kho (2), Trả NCC (3), Xuất SX (8)
                        else if (vm.VoucherType is VoucherTypeEnum.XuatKho or VoucherTypeEnum.TraNCC or VoucherTypeEnum.XuatSanXuat)
                        {
                            itemLocation.Quantity -= detail.BaseQty;
                            if (itemLocation.Quantity < 0)
                                throw WmsExceptions.InsufficientStock(item.ItemCode, itemLocation.Quantity + detail.BaseQty);
                        }
                        // Điều chỉnh (5): BaseQty đã có dấu (+ tăng, - giảm)
                        else if (vm.VoucherType == VoucherTypeEnum.DieuChinh)
                        {
                            itemLocation.Quantity += detail.BaseQty;
                            if (itemLocation.Quantity < 0)
                                throw WmsExceptions.NegativeLocationStock(item.ItemCode, itemLocation.Quantity - detail.BaseQty);
                        }
                        // Chuyển kho (6)
                        else if (vm.VoucherType == VoucherTypeEnum.ChuyenKho)
                        {
                            itemLocation.Quantity -= detail.BaseQty;
                            if (itemLocation.Quantity < 0)
                                throw WmsExceptions.TransferInsufficientStock(item.ItemCode, itemLocation.Quantity + detail.BaseQty);

                            if (!detail.DestLocationId.HasValue)
                                throw WmsExceptions.DestinationLocationRequired(item.ItemCode);

                            var destItemLocation = await _db.ItemLocations
                                .FirstOrDefaultAsync(il => il.ItemId == detail.ItemId
                                    && il.LocationId == detail.DestLocationId.Value
                                    && il.LotNumber == detail.LotNumber
                                    && il.ExpiryDate == detail.ExpiryDate);

                            if (destItemLocation == null)
                            {
                                destItemLocation = new ItemLocation
                                {
                                    ItemId = detail.ItemId,
                                    LocationId = detail.DestLocationId.Value,
                                    Quantity = 0,
                                    ExpiryDate = detail.ExpiryDate,
                                    LotNumber = detail.LotNumber,
                                    UpdatedAt = VietnamNow
                                };
                                _db.ItemLocations.Add(destItemLocation);
                            }

                            destItemLocation.Quantity += detail.BaseQty;
                            destItemLocation.UpdatedAt = VietnamNow;
                        }


                        itemLocation.UpdatedAt = VietnamNow;
                    }

                    // Cập nhật tổng tồn kho của toàn bộ Item (hiển thị trên Dashboard và Báo Cáo Tồn Kho)
                    if (vm.VoucherType is VoucherTypeEnum.NhapKho or VoucherTypeEnum.KhachTra or VoucherTypeEnum.NhapThanhPham)
                    {
                        // ═══ WEIGHTED AVERAGE COST (Bình quân gia quyền — VAS-02) ═══
                        // Formula: NewAvgCost = (OldStock × OldCost + NewQty × PurchasePrice) / (OldStock + NewQty)
                        var purchasePrice = detail.UnitPrice;
                        runningStockByItem.TryGetValue(item.ItemId, out var oldStock);
                        var oldCost = item.UnitCost;
                        var newStock = oldStock + detail.BaseQty;
                        item.CurrentStock = newStock;
                        runningStockByItem[item.ItemId] = newStock;
                        if (newStock > 0)
                        {
                            item.UnitCost = (oldStock * oldCost + detail.BaseQty * purchasePrice) / newStock;
                        }
                        item.LastCost = purchasePrice;
                    }
                    else if (vm.VoucherType is VoucherTypeEnum.XuatKho or VoucherTypeEnum.TraNCC or VoucherTypeEnum.XuatSanXuat)
                    {
                        runningStockByItem.TryGetValue(item.ItemId, out var oldStock);
                        var newStock = oldStock - detail.BaseQty;
                        item.CurrentStock = newStock;
                        runningStockByItem[item.ItemId] = newStock;
                        // UnitCost không thay đổi khi xuất (bình quân gia quyền giữ nguyên đến lần nhập tiếp)
                    }
                    else if (vm.VoucherType == VoucherTypeEnum.DieuChinh)
                    {
                        runningStockByItem.TryGetValue(item.ItemId, out var oldStock);
                        var newStock = oldStock + detail.BaseQty;
                        item.CurrentStock = newStock;
                        runningStockByItem[item.ItemId] = newStock;
                        if (newStock < 0)
                            throw WmsExceptions.NegativeItemStock(item.ItemCode, 0);
                    }
                    // Loại 6 (Chuyển kho) không làm thay đổi tổng tồn của Item

                    item.TotalStockValue = item.CurrentStock * item.UnitCost;
                    item.UpdatedAt = VietnamNow;

                    // ───── Auto-create StockAlert khi tồn kho thấp ─────
                    if (item.MinThreshold > 0 && item.CurrentStock <= item.MinThreshold)
                    {
                        // Chỉ tạo alert mới nếu chưa có alert chưa giải quyết cho item này
                        var existingAlert = await _db.StockAlerts
                            .AnyAsync(a => a.ItemId == item.ItemId && !a.IsResolved && a.AlertType == AlertTypeEnum.LowStock);
                        if (!existingAlert)
                        {
                            _db.StockAlerts.Add(new StockAlert
                            {
                                ItemId = item.ItemId,
                                AlertType = AlertTypeEnum.LowStock,
                                CurrentStock = item.CurrentStock,
                                Threshold = item.MinThreshold,
                                IsRead = false,
                                IsResolved = false,
                                CreatedAt = VietnamNow
                            });
                        }
                    }
                    // Auto-resolve LowStock alert nếu tồn kho đã phục hồi trên ngưỡng
                    else if (item.MinThreshold > 0 && item.CurrentStock > item.MinThreshold)
                    {
                        var activeAlerts = await _db.StockAlerts
                            .Where(a => a.ItemId == item.ItemId && !a.IsResolved && a.AlertType == AlertTypeEnum.LowStock)
                            .ToListAsync();
                        foreach (var alert in activeAlerts)
                        {
                            alert.IsResolved = true;
                            alert.ResolvedAt = VietnamNow;
                        }
                    }

                    // ───── Auto-create OverStock Alert khi vượt ngưỡng tối đa (Đặc tả 6.1) ─────
                    if (item.MaxThreshold.HasValue && item.MaxThreshold > 0 && item.CurrentStock >= item.MaxThreshold.Value)
                    {
                        var existingOverStock = await _db.StockAlerts
                            .AnyAsync(a => a.ItemId == item.ItemId && !a.IsResolved && a.AlertType == AlertTypeEnum.OverStock);
                        if (!existingOverStock)
                        {
                            _db.StockAlerts.Add(new StockAlert
                            {
                                ItemId = item.ItemId,
                                AlertType = AlertTypeEnum.OverStock,
                                CurrentStock = item.CurrentStock,
                                Threshold = item.MaxThreshold.Value,
                                IsRead = false,
                                IsResolved = false,
                                CreatedAt = VietnamNow
                            });
                        }
                    }
                    // Auto-resolve OverStock alert khi tồn giảm xuống dưới ngưỡng
                    else if (item.MaxThreshold.HasValue && item.MaxThreshold > 0 && item.CurrentStock < item.MaxThreshold.Value)
                    {
                        var activeOverStockAlerts = await _db.StockAlerts
                            .Where(a => a.ItemId == item.ItemId && !a.IsResolved && a.AlertType == AlertTypeEnum.OverStock)
                            .ToListAsync();
                        foreach (var alert in activeOverStockAlerts)
                        {
                            alert.IsResolved = true;
                            alert.ResolvedAt = VietnamNow;
                        }
                    }
                }
            }

            voucher.TotalAmount = totalAmount;
            voucher.TotalLines = lineNum;

            await _unitOfWork.SaveChangesAsync();

            // P0-03: Sync CurrentStock from ItemLocation source of truth
            var createAffectedItemIds = vm.Lines.Select(l => l.ItemId).Distinct();
            await _inventoryBalanceService.SyncCurrentStockAsync(createAffectedItemIds);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            TempData["Success"] = submit && IsInboundVoucherType(voucher.VoucherType)
                ? $"Phiếu {voucherCode} đã gửi duyệt. Nhân viên xem lại ở Tra cứu phiếu; quản lý duyệt ở Nhập kho > Duyệt phiếu nhập."
                : $"Tạo phiếu {voucherCode} thành công! ({lineNum} dòng, tổng {totalAmount:N0} VNĐ)";
            if (voucher.VoucherType is VoucherTypeEnum.XuatKho or VoucherTypeEnum.TraNCC or VoucherTypeEnum.ChuyenKho or VoucherTypeEnum.XuatSanXuat)
            {
                var autoRelease = await _orderStreamingService.TryAutoReleaseAsync(voucher.VoucherId, User.Identity?.Name ?? "system", GetScopedWarehouseId());
                if (autoRelease.Succeeded && autoRelease.Message?.StartsWith("Đã phát hành trực tiếp", StringComparison.OrdinalIgnoreCase) == true)
                    TempData["Success"] = $"{TempData["Success"]} {autoRelease.Message}";
                else if (!autoRelease.Succeeded && !autoRelease.NotFound && !autoRelease.Forbidden)
                    TempData["Warning"] = autoRelease.Message;
            }
            return RedirectToAction("Details", new { id = voucher.VoucherId });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            TempData["Error"] = UserSafeError.From(ex, "Không thể tạo phiếu lúc này. Vui lòng kiểm tra dữ liệu và thử lại.");

            // Re-populate view model for return
            vm.Warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync();
            vm.Partners = await _db.Partners.Where(p => p.IsActive).ToListAsync();
            vm.Items = await _db.Items.Include(i => i.BaseUom).Where(i => i.IsActive).OrderBy(i => i.ItemCode).ToListAsync();
            vm.Uoms = await _db.UnitsOfMeasure.Where(u => u.IsActive).ToListAsync();
            vm.Locations = await _db.Locations.Where(l => l.IsActive).ToListAsync();
            ViewBag.CanSeeFinancial = CanSeeFinancial();
            await PopulateVoucherCreateMetadataAsync(vm);
            return View(vm);
        }
    }


    public async Task<IActionResult> Details(long id)
    {
        var voucher = await _db.Vouchers.AsNoTracking()
            .Include(v => v.Warehouse).Include(v => v.DestWarehouse).Include(v => v.Partner)
            .Include(v => v.Details).ThenInclude(d => d.Item)
            .Include(v => v.Details).ThenInclude(d => d.Location)
            .Include(v => v.Details).ThenInclude(d => d.TransactionUom)
            .Include(v => v.Details).ThenInclude(d => d.PackagingUnit)
            .Include(v => v.Packages)
            .FirstOrDefaultAsync(v => v.VoucherId == id);

        if (voucher == null) return NotFound();
        var scopedWh = GetScopedWarehouseId();
        if (scopedWh.HasValue && voucher.WarehouseId != scopedWh.Value)
            return Forbid();
        ViewBag.CanSeeFinancial = CanSeeFinancial();

        // Load reservation history for outbound multi-release timeline
        var isOutbound = voucher.VoucherType is VoucherTypeEnum.XuatKho or VoucherTypeEnum.TraNCC or VoucherTypeEnum.ChuyenKho or VoucherTypeEnum.XuatSanXuat;
        if (isOutbound)
        {
            var reservations = await _db.StockReservations
                .Where(r => r.VoucherId == voucher.VoucherId)
                .Include(r => r.Item)
                .Include(r => r.Location)
                .OrderBy(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
            ViewBag.Reservations = reservations;

            var pickTasks = await _db.PickTasks
                .Where(t => t.VoucherId == voucher.VoucherId
                    || t.Allocations.Any(a => a.VoucherId == voucher.VoucherId))
                .Include(t => t.SourceLocation)
                .Include(t => t.Item)
                .Include(t => t.Allocations)
                .OrderBy(t => t.PickTaskId)
                .ToListAsync();
            ViewBag.PickTasks = pickTasks;

            var taskIds = pickTasks.Select(t => t.PickTaskId).ToList();
            var pickScanLogs = await _db.PickTaskScanLogs
                .Where(l => taskIds.Contains(l.PickTaskId))
                .OrderBy(l => l.ScannedAt)
                .ToListAsync();
            ViewBag.PickTaskScanLogs = pickScanLogs;

            var outboundPackages = await _db.OutboundPackages
                .Where(p => p.VoucherId == voucher.VoucherId)
                .Include(p => p.ShipmentLoad)
                .Include(p => p.CatchWeightUom)
                .OrderBy(p => p.PackedAt)
                .ThenBy(p => p.PackageCode)
                .ToListAsync();
            ViewBag.OutboundPackages = outboundPackages;

            var shippingHandovers = await _db.ShippingHandoverLogs
                .Where(x => x.VoucherId == voucher.VoucherId)
                .Include(x => x.ShipmentLoad)
                .OrderByDescending(x => x.HandedOverAt)
                .ToListAsync();
            ViewBag.ShippingHandovers = shippingHandovers;

            var catchWeightEntries = await _db.CatchWeightEntries
                .Where(x => x.VoucherId == voucher.VoucherId && x.Status == CatchWeightStatusEnum.Captured)
                .Include(x => x.WeightUom)
                .OrderByDescending(x => x.CapturedAt)
                .ToListAsync();
            ViewBag.CatchWeightEntries = catchWeightEntries;
        }
        else if (voucher.IsInboundFlow)
        {
            var licensePlates = await _db.LicensePlates
                .Where(l => l.VoucherId == voucher.VoucherId)
                .Include(l => l.CurrentLocation)
                .Include(l => l.Details).ThenInclude(d => d.Item)
                .OrderBy(l => l.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
            ViewBag.LicensePlates = licensePlates;

            var catchWeightEntries = await _db.CatchWeightEntries
                .Where(x => x.VoucherId == voucher.VoucherId && x.Status == CatchWeightStatusEnum.Captured)
                .Include(x => x.WeightUom)
                .OrderByDescending(x => x.CapturedAt)
                .ToListAsync();
            ViewBag.CatchWeightEntries = catchWeightEntries;

            var detailIds = voucher.Details.Select(d => d.VoucherDetailId).ToList();
            ViewBag.SerialCountsByDetail = detailIds.Count == 0
                ? new Dictionary<long, int>()
                : await _db.SerialNumbers.AsNoTracking()
                    .Where(s => s.VoucherId == voucher.VoucherId
                        && s.VoucherDetailId.HasValue
                        && detailIds.Contains(s.VoucherDetailId.Value)
                        && s.Status == SerialNumberStatusEnum.Active
                        && s.VoidedAt == null)
                    .GroupBy(s => s.VoucherDetailId!.Value)
                    .Select(g => new { VoucherDetailId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.VoucherDetailId, x => x.Count);
            ViewBag.CompletedCrossDockByDetail = detailIds.Count == 0
                ? new Dictionary<long, decimal>()
                : await _db.CrossDockTasks.AsNoTracking()
                    .Where(t => t.InboundVoucherDetailId.HasValue
                        && detailIds.Contains(t.InboundVoucherDetailId.Value)
                        && t.Status == CrossDockTaskStatusEnum.Completed)
                    .GroupBy(t => t.InboundVoucherDetailId!.Value)
                    .Select(g => new { VoucherDetailId = g.Key, Qty = g.Sum(t => t.ActualQty ?? t.ScheduledQty) })
                    .ToDictionaryAsync(x => x.VoucherDetailId, x => x.Qty);
        }

        return View(voucher);
    }

}
