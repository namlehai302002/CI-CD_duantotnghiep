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

    /// <summary>
    /// Gợi ý cất hàng có điều hướng: chọn vị trí nhập kho tự động dựa trên chiến lược cất hàng.
    /// SAP EWM gọi nghiệp vụ tương đương là "Put-away Rule Determination".
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> SuggestPutawayLocation(int itemId, int warehouseId)
    {
        var item = await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.ItemId == itemId && i.IsActive);
        if (item == null) return NotFound(new { message = "Vật tư không tồn tại." });

        // Parse allowed zone types
        var allowedZoneTypes = new HashSet<byte>();
        if (!string.IsNullOrWhiteSpace(item.AllowedZoneTypes))
        {
            foreach (var s in item.AllowedZoneTypes.Split(','))
                if (byte.TryParse(s.Trim(), out var zt)) allowedZoneTypes.Add(zt);
        }

        // Get all eligible locations in the warehouse
        var locQuery = _db.Locations
            .AsNoTracking()
            .Include(l => l.Zone)
            .Include(l => l.ItemLocations)
            .Where(l => l.IsActive && l.Zone != null && l.Zone.IsActive && l.Zone.WarehouseId == warehouseId);

        // Filter by zone type (Storage or CrossDock only for put-away, plus allowed overrides)
        locQuery = locQuery.Where(l =>
            l.Zone!.ZoneType == ZoneTypeEnum.Storage ||
            l.Zone!.ZoneType == ZoneTypeEnum.CrossDock);

        var locations = await locQuery.OrderBy(l => l.LocationCode).ToListAsync();

        // Apply AllowedZoneTypes filter if specified
        if (allowedZoneTypes.Count > 0)
            locations = locations.Where(l => allowedZoneTypes.Contains((byte)l.Zone!.ZoneType)).ToList();

        // Get currently occupied locations (1-slot-1-item rule)
        var occupiedLocationIds = await _db.ItemLocations.AsNoTracking()
            .Where(il => il.Quantity > 0)
            .Select(il => new { il.LocationId, il.ItemId })
            .ToListAsync();
        var occupiedMap = occupiedLocationIds
            .GroupBy(x => x.LocationId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ItemId).Distinct().ToList());

        string strategy = item.PutawayStrategy.ToString();
        int? suggestedLocationId = null;
        string? locationCode = null;
        string reason = "";

        // Strategy 1: Default — use DefaultLocation if available and has capacity
        if (item.DefaultLocationId.HasValue)
        {
            var defLoc = locations.FirstOrDefault(l => l.LocationId == item.DefaultLocationId.Value);
            if (defLoc != null)
            {
                var occupants = occupiedMap.GetValueOrDefault(defLoc.LocationId, new List<int>());
                if (occupants.Count == 0 || (occupants.Count == 1 && occupants[0] == itemId))
                {
                    if (defLoc.CurrentLoad < defLoc.MaxCapacity)
                    {
                        suggestedLocationId = defLoc.LocationId;
                        locationCode = defLoc.LocationCode;
                        reason = "Ô mặc định còn trống.";
                    }
                }
            }
        }

        // Strategy 2: Consolidate — find locations that already have this item
        if (!suggestedLocationId.HasValue &&
            (item.PutawayStrategy == PutawayStrategyEnum.Consolidate || item.PutawayStrategy == PutawayStrategyEnum.Default))
        {
            var consolidateLoc = locations
                .Where(l =>
                {
                    var occupants = occupiedMap.GetValueOrDefault(l.LocationId, new List<int>());
                    return occupants.Count == 1 && occupants[0] == itemId && l.CurrentLoad < l.MaxCapacity;
                })
                .OrderBy(l => l.CurrentLoad) // prefer least loaded
                .FirstOrDefault();

            if (consolidateLoc != null)
            {
                suggestedLocationId = consolidateLoc.LocationId;
                locationCode = consolidateLoc.LocationCode;
                reason = "Gộp vào ô đã có hàng (consolidate).";
                strategy = "Consolidate";
            }
        }

        // Strategy 3: NearestEmpty — find empty location in same zone or any storage zone
        if (!suggestedLocationId.HasValue)
        {
            var emptyLoc = locations
                .Where(l =>
                {
                    var occupants = occupiedMap.GetValueOrDefault(l.LocationId, new List<int>());
                    return occupants.Count == 0 && l.CurrentLoad == 0;
                })
                .OrderBy(l => l.LocationCode) // alphabetical = sequential = nearest
                .FirstOrDefault();

            if (emptyLoc != null)
            {
                suggestedLocationId = emptyLoc.LocationId;
                locationCode = emptyLoc.LocationCode;
                reason = "Ô trống gần nhất cùng khu vực.";
                strategy = "NearestEmpty";
            }
        }

        return Json(new
        {
            suggested = suggestedLocationId.HasValue,
            locationId = suggestedLocationId,
            locationCode,
            strategy,
            reason
        });
    }


    private async Task PopulateVoucherCreateMetadataAsync(VoucherCreateViewModel vm)
    {
        ViewBag.ItemAllowedSourceUomsJson = await _voucherCreateWorkflowService.BuildItemAllowedSourceUomsJsonAsync(vm.Items);
    }


    private async Task<DateTime?> GetActiveLockDateAsync(int warehouseId)
        => await _voucherSharedRuleService.GetActiveLockDateAsync(warehouseId);


    private async Task<int?> GetLocationWarehouseIdAsync(int locationId)
        => await _voucherSharedRuleService.GetLocationWarehouseIdAsync(locationId);


    private async Task<string> GenerateNextAsnCodeAsync()
    {
        var dateStr = VietnamNow.ToString("yyyyMMdd");
        var prefix = $"ASN-{dateStr}-";

        // MAX-based: parse actual highest sequence from existing codes (race-condition-safe inside Serializable tx)
        var maxCode = await _db.Vouchers
            .Where(v => v.AsnCode != null && v.AsnCode.StartsWith(prefix))
            .OrderByDescending(v => v.AsnCode)
            .Select(v => v.AsnCode)
            .FirstOrDefaultAsync();

        var nextSeq = 1;
        if (maxCode != null && maxCode.Length > prefix.Length
            && int.TryParse(maxCode[prefix.Length..], out var lastSeq))
        {
            nextSeq = lastSeq + 1;
        }

        var candidate = $"{prefix}{nextSeq:D5}";
        return candidate;
    }


    private async Task<string> GenerateNextLpnCodeAsync()
    {
        var dateStr = VietnamNow.ToString("yyyyMMdd");
        var prefix = $"LPN-{dateStr}-";

        var maxCode = await _db.LicensePlates
            .Where(l => l.LpnCode.StartsWith(prefix))
            .OrderByDescending(l => l.LpnCode)
            .Select(l => l.LpnCode)
            .FirstOrDefaultAsync();

        var nextSeq = 1;
        if (maxCode != null && maxCode.Length > prefix.Length
            && int.TryParse(maxCode[prefix.Length..], out var lastSeq))
        {
            nextSeq = lastSeq + 1;
        }

        return $"{prefix}{nextSeq:D6}";
    }


    private async Task<string> GenerateNextPackageCodeAsync()
    {
        var dateStr = VietnamNow.ToString("yyyyMMdd");
        var prefix = $"PKG-{dateStr}-";

        var maxCode = await _db.OutboundPackages
            .Where(p => p.PackageCode.StartsWith(prefix))
            .OrderByDescending(p => p.PackageCode)
            .Select(p => p.PackageCode)
            .FirstOrDefaultAsync();

        var nextSeq = 1;
        if (maxCode != null && maxCode.Length > prefix.Length
            && int.TryParse(maxCode[prefix.Length..], out var lastSeq))
        {
            nextSeq = lastSeq + 1;
        }

        return $"{prefix}{nextSeq:D5}";
    }


    /// <summary>
    /// P1.3 — Dock Scheduling Optimizer V2: Scoring-based dock suggestion.
    /// Score = (carriers_with_less_appointments) + (slot_capacity_remaining) + (door_type_match)
    /// Returns ranked doors with scores for ops to choose.
    /// </summary>
    private async Task<List<(string DockDoor, int Score, int CurrentBookings, int MaxCapacity)>> ScoreDockDoorsAsync(
        int warehouseId, DateTime? appointmentStart, DateTime? appointmentEnd, bool isInbound = true)
    {
        var candidateDoors = new List<string>();

        // Lấy cấu hình door capacity từ DB (nếu có)
        var configuredDoors = await _db.DockDoorCapacities
            .AsNoTracking()
            .Where(d => d.WarehouseId == warehouseId)
            .ToListAsync();

        if (configuredDoors.Count > 0)
        {
            candidateDoors = configuredDoors
                .Where(d => !isInbound || d.DoorType == DockDoorTypeEnum.Both || d.DoorType == DockDoorTypeEnum.Receiving)
                .Where(d => isInbound || d.DoorType == DockDoorTypeEnum.Both || d.DoorType == DockDoorTypeEnum.Shipping)
                .Select(d => d.DockDoor)
                .Distinct()
                .ToList();
        }

        if (candidateDoors.Count == 0)
            candidateDoors = Enumerable.Range(1, 8).Select(i => $"DOCK-{i:D2}").ToList();

        var dayOfWeek = appointmentStart?.DayOfWeek;
        var startMin = appointmentStart.HasValue ? (int)(appointmentStart.Value.TimeOfDay.TotalMinutes) : 0;
        var endMin = appointmentEnd.HasValue ? (int)(appointmentEnd.Value.TimeOfDay.TotalMinutes) : 0;
        var durationMinutes = Math.Max(0, endMin - startMin);

        // Đếm số appointment đã book trong khung giờ
        // FIX: Exclude completed/closed vouchers from dock booking count
        var bookedDoors = await _db.Vouchers.AsNoTracking()
            .Where(v => v.WarehouseId == warehouseId
                && !v.IsCancelled
                && v.InboundStatus != InboundStatusEnum.Completed  // FIX: Exclude completed vouchers
                && v.DockDoor != null
                && v.DockAppointmentStart.HasValue
                && v.DockAppointmentEnd.HasValue
                && appointmentStart.HasValue && appointmentEnd.HasValue
                && appointmentStart.Value < v.DockAppointmentEnd.Value
                && appointmentEnd.Value > v.DockAppointmentStart.Value)
            .GroupBy(v => v.DockDoor)
            .Select(g => new { Door = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Door!, x => x.Count);

        // Lấy max capacity từ bảng DockDoorCapacity
        var maxCapacities = configuredDoors
            .GroupBy(d => d.DockDoor)
            .ToDictionary(g => g.Key, g => g.Where(d => !d.DayOfWeek.HasValue || d.DayOfWeek == dayOfWeek)
                .OrderByDescending(d => d.MaxAppointments)
                .First().MaxAppointments);

        // Scoring: ưu tiên cửa ít booking nhất + còn slot trống
        var scored = candidateDoors.Select(door =>
        {
            var currentBookings = bookedDoors.TryGetValue(door, out var b) ? b : 0;
            var maxCap = maxCapacities.TryGetValue(door, out var mc) ? mc : 4;
            var remainingCapacity = Math.Max(0, maxCap - currentBookings);

            var score = remainingCapacity > 0 ? 50 : 0;
            score += Math.Max(0, 10 - currentBookings);
            if (maxCapacities.ContainsKey(door)) score += 20;

            return new { Door = door, Score = score, CurrentBookings = currentBookings, MaxCapacity = maxCap };
        })
        .OrderByDescending(r => r.Score)
        .ThenBy(r => r.CurrentBookings)
        .Select(r => (r.Door, r.Score, r.CurrentBookings, r.MaxCapacity))
        .ToList();

        return scored;
    }


    private async Task<string?> SuggestAvailableDockDoorAsync(int warehouseId, DateTime? appointmentStart, DateTime? appointmentEnd, long? excludingVoucherId = null)
    {
        if (!appointmentStart.HasValue || !appointmentEnd.HasValue)
            return null;

        // Lọc bỏ các cửa đã có appointment trùng lặp (giữ nguyên check cũ)
        var busyDoors = await _db.Vouchers.AsNoTracking()
            .Where(v => v.WarehouseId == warehouseId
                && !v.IsCancelled
                && (v.VoucherType == VoucherTypeEnum.NhapKho
                    || v.VoucherType == VoucherTypeEnum.KhachTra
                    || v.VoucherType == VoucherTypeEnum.NhapThanhPham)
                && v.DockDoor != null
                && (!excludingVoucherId.HasValue || v.VoucherId != excludingVoucherId.Value)
                && v.DockAppointmentStart.HasValue
                && v.DockAppointmentEnd.HasValue
                && appointmentStart.Value < v.DockAppointmentEnd.Value
                && appointmentEnd.Value > v.DockAppointmentStart.Value)
            .Select(v => v.DockDoor!)
            .Distinct()
            .ToListAsync();

        // P1.3: Dùng scoring engine V2 để gợi ý cửa tốt nhất
        var rankedDoors = await ScoreDockDoorsAsync(warehouseId, appointmentStart, appointmentEnd, isInbound: true);

        // Loại cửa đang bận, ưu tiên cửa có score cao nhất
        return rankedDoors
            .Where(r => !busyDoors.Contains(r.DockDoor, StringComparer.OrdinalIgnoreCase) && r.CurrentBookings < r.MaxCapacity)
            .Select(r => r.DockDoor)
            .FirstOrDefault();
    }


    private async Task EnsureDockAvailabilityAsync(int warehouseId, string dockDoor, DateTime? appointmentStart, DateTime? appointmentEnd, long? excludingVoucherId = null)
    {
        if (string.IsNullOrWhiteSpace(dockDoor) || !appointmentStart.HasValue || !appointmentEnd.HasValue)
            return;

        var normalizedDock = dockDoor.Trim().ToUpperInvariant();

        var conflict = await _db.Vouchers.AsNoTracking()
            .Where(v => v.WarehouseId == warehouseId
                && !v.IsCancelled
                && (v.VoucherType == VoucherTypeEnum.NhapKho
                    || v.VoucherType == VoucherTypeEnum.KhachTra
                    || v.VoucherType == VoucherTypeEnum.NhapThanhPham)
                && v.DockDoor == normalizedDock
                && (!excludingVoucherId.HasValue || v.VoucherId != excludingVoucherId.Value)
                && v.DockAppointmentStart.HasValue
                && v.DockAppointmentEnd.HasValue
                && appointmentStart.Value < v.DockAppointmentEnd.Value
                && appointmentEnd.Value > v.DockAppointmentStart.Value)
            .OrderBy(v => v.DockAppointmentStart)
            .Select(v => new { v.VoucherCode, v.DockAppointmentStart, v.DockAppointmentEnd })
            .FirstOrDefaultAsync();

        if (conflict != null)
        {
            throw WmsExceptions.DockConflict(normalizedDock, conflict.VoucherCode, conflict.DockAppointmentStart!.Value, conflict.DockAppointmentEnd!.Value);
        }
    }


    private async Task ValidateInboundPlanningAsync(
        VoucherTypeEnum voucherType,
        int warehouseId,
        bool isSubmitting,
        DateTime? expectedArrivalAt,
        DateTime? dockAppointmentStart,
        DateTime? dockAppointmentEnd,
        string? dockDoor,
        long? excludingVoucherId = null)
    {
        if (!IsInboundVoucherType(voucherType))
            return;

        if (isSubmitting && !expectedArrivalAt.HasValue)
            throw WmsExceptions.InboundExpectedArrivalRequired();

        if (dockAppointmentStart.HasValue ^ dockAppointmentEnd.HasValue)
            throw WmsExceptions.DockAppointmentIncomplete();

        if (dockAppointmentStart.HasValue && dockAppointmentEnd.HasValue)
        {
            if (dockAppointmentEnd.Value <= dockAppointmentStart.Value)
                throw WmsExceptions.DockAppointmentEndBeforeStart();

            if (expectedArrivalAt.HasValue &&
                (expectedArrivalAt.Value < dockAppointmentStart.Value.AddHours(-8)
                 || expectedArrivalAt.Value > dockAppointmentEnd.Value.AddHours(8)))
            {
                throw WmsExceptions.ArrivalTimeMismatch();
            }
        }

        if (!string.IsNullOrWhiteSpace(dockDoor))
        {
            await EnsureDockAvailabilityAsync(warehouseId, dockDoor, dockAppointmentStart, dockAppointmentEnd, excludingVoucherId);
        }
    }


    /// <summary>
    /// P2.5: Advanced FEFO Allocation với ưu tiên theo ServiceLevel và Priority
    /// - SameDay: ưu tiên pick location gần shipping dock trước (giảm travel time)
    /// - Express: ưu tiên location có expiry sớm nhất
    /// - Standard: FEFO thông thường
    /// - PreOrder: ưu tiên location xa shipping dock (bảo toàn hàng gần cho đơn gấp)
    /// </summary>
    private async Task<List<FefoAllocation>> AllocateFefoWithPriorityAsync(
        int itemId, int warehouseId, decimal requiredBaseQty,
        ServiceLevelEnum serviceLevel, int priority)
    {
        var remaining = requiredBaseQty;
        var picks = new List<FefoAllocation>();
        if (requiredBaseQty <= 0) return picks;

        // Lấy thông tin warehouse để biết shipping zone
        var warehouse = await _db.Warehouses
            .Include(w => w.Zones.Where(z => z.ZoneType == ZoneTypeEnum.Shipping))
            .FirstOrDefaultAsync(w => w.WarehouseId == warehouseId);
        var shippingZoneId = warehouse?.Zones.FirstOrDefault()?.ZoneId;

        // BR-OUT-002: Loại bỏ hàng đã hết hạn và hàng có HSD < 30 ngày khỏi outbound allocation
        var minExpiryDate = VietnamNow.Date.AddDays(30);

        var candidates = await _db.ItemLocations
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Where(il => il.ItemId == itemId
                && il.Quantity > il.ReservedQty
                && il.HoldStatus == InventoryHoldStatusEnum.Available
                && il.Location != null
                && il.Location.IsActive
                && il.Location.Zone != null
                && il.Location.Zone.WarehouseId == warehouseId
                // Filter: không allocate hàng hết hạn hoặc sắp hết hạn (< 30 ngày)
                && (il.ExpiryDate == null || il.ExpiryDate >= minExpiryDate))
            .ToListAsync();

        // P2.5: Sắp xếp theo ServiceLevel priority
        // SameDay/Express: ưu tiên pick gần shipping (giảm travel time)
        // PreOrder: ưu tiên pick xa shipping (bảo toàn hàng gần cho đơn gấp)
        // Standard: FEFO thông thường
        var sortedCandidates = serviceLevel switch
        {
            ServiceLevelEnum.SameDay or ServiceLevelEnum.Express =>
                // Ưu tiên zone Shipping > Staging > gần shipping nhất
                candidates
                    .OrderBy(c => c.Location?.Zone?.ZoneType != ZoneTypeEnum.Shipping)
                    .ThenBy(c => c.Location?.Zone?.ZoneType != ZoneTypeEnum.Staging)
                    .ThenBy(c => shippingZoneId.HasValue && c.Location?.ZoneId == shippingZoneId ? 0 : 1)
                    .ThenBy(c => c.ExpiryDate.HasValue ? 0 : 1)  // NULL expiry last (safer stock first)
                    .ThenBy(c => c.ExpiryDate)
                    .ThenByDescending(c => c.Quantity - c.ReservedQty),

            ServiceLevelEnum.Scheduled =>
                // Ưu tiên theo FEFO có giới hạn
                candidates
                    .OrderBy(c => c.ExpiryDate.HasValue ? 0 : 1)  // NULL expiry last
                    .ThenBy(c => c.ExpiryDate)
                    .Take(3) // Chỉ lấy top 3 location để đảm bảo packing đồng nhất
                    .OrderByDescending(c => c.Quantity - c.ReservedQty),

            ServiceLevelEnum.PreOrder =>
                // Ưu tiên pick xa shipping dock (ZoneType.Storage > Staging > Shipping)
                candidates
                    .OrderBy(c => c.Location?.Zone?.ZoneType == ZoneTypeEnum.Storage ? 0 : 1)
                    .ThenBy(c => c.Location?.Zone?.ZoneType == ZoneTypeEnum.Staging ? 0 : 1)
                    .ThenBy(c => shippingZoneId.HasValue && c.Location?.ZoneId == shippingZoneId ? 1 : 0)
                    .ThenByDescending(c => c.ExpiryDate)
                    .ThenBy(c => c.Quantity - c.ReservedQty),

            _ => // Standard hoặc default
                candidates
                    .OrderBy(c => c.ExpiryDate.HasValue ? 0 : 1)  // NULL expiry last
                    .ThenBy(c => c.ExpiryDate)
                    .ThenByDescending(c => c.Quantity - c.ReservedQty)
        };

        foreach (var c in sortedCandidates)
        {
            var available = c.Quantity - c.ReservedQty;
            if (available <= 0) continue;
            var take = Math.Min(remaining, available);
            if (take <= 0) continue;

            picks.Add(new FefoAllocation(c.LocationId, c.LotNumber, c.ExpiryDate, take));
            remaining -= take;
            if (remaining <= 0) break;
        }

        if (picks.Count == 0)
            throw WmsExceptions.NoAvailableStock(itemId);

        return picks;
    }


    private async Task<List<FefoAllocation>> AllocateFefoAsync(int itemId, int warehouseId, decimal requiredBaseQty)
    {
        var remaining = requiredBaseQty;
        var picks = new List<FefoAllocation>();
        if (requiredBaseQty <= 0) return picks;

        // BR-OUT-002: Loại bỏ hàng đã hết hạn và hàng có HSD < 30 ngày khỏi outbound allocation
        var minExpiryDate = VietnamNow.Date.AddDays(30);

        var candidates = await _db.ItemLocations
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Where(il => il.ItemId == itemId
                && il.Quantity > il.ReservedQty
                && il.HoldStatus == InventoryHoldStatusEnum.Available
                && il.Location != null
                && il.Location.IsActive
                && il.Location.Zone != null
                && il.Location.Zone.WarehouseId == warehouseId
                // Filter: không allocate hàng hết hạn hoặc sắp hết hạn (< 30 ngày)
                && (il.ExpiryDate == null || il.ExpiryDate >= minExpiryDate))
            .OrderBy(il => il.ExpiryDate.HasValue ? 0 : 1)  // NULL expiry last
            .ThenBy(il => il.ExpiryDate)
            .ThenByDescending(il => il.Quantity - il.ReservedQty)
            .ToListAsync();

        foreach (var c in candidates)
        {
            var available = c.Quantity - c.ReservedQty;
            if (available <= 0) continue;
            var take = Math.Min(remaining, available);
            if (take <= 0) continue;

            picks.Add(new FefoAllocation(c.LocationId, c.LotNumber, c.ExpiryDate, take));
            remaining -= take;
            if (remaining <= 0) break;
        }

        // Partial allocation: nếu không đủ, giữ chỗ phần có sẵn (hỗ trợ giao theo đợt)
        // Nếu KHÔNG có tồn nào cả → mới throw lỗi
        if (picks.Count == 0)
            throw WmsExceptions.NoAvailableStock(itemId);

        return picks;
    }


    private async Task RecalculateReservedQtyAsync(IEnumerable<int> itemLocationIds)
    {
        await _inventoryReservationService.RecalculateReservedQtyAsync(itemLocationIds);
    }


    private async Task<FefoAllocation?> GetFefoLocationForSingleLineAsync(int itemId, int warehouseId, decimal requiredBaseQty, bool isOutbound = true)
    {
        // FEFO: pick location with earliest expiry (nulls last), then most stock
        // Return the exact lot/expiry row that should be consumed for single-line auto-pick.
        // BR-OUT-002: Xuất kho không được lấy hàng có HSD < 30 ngày
        var minExpiryDate = isOutbound ? VietnamNow.Date.AddDays(30) : (DateTime?)null;

        var candidates = await _db.ItemLocations.AsNoTracking()
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Where(il => il.ItemId == itemId
                && il.Quantity > il.ReservedQty
                && il.HoldStatus == InventoryHoldStatusEnum.Available
                && il.Location != null
                && il.Location.IsActive
                && il.Location.Zone != null
                && il.Location.Zone.WarehouseId == warehouseId
                // BR-OUT-002: Loại bỏ lô hàng có HSD < 30 ngày khỏi outbound
                && (!minExpiryDate.HasValue || il.ExpiryDate == null || il.ExpiryDate >= minExpiryDate.Value))
            .OrderBy(il => il.ExpiryDate.HasValue ? 0 : 1)  // NULL expiry last
            .ThenBy(il => il.ExpiryDate)          // earliest expiry
            .ThenByDescending(il => il.Quantity - il.ReservedQty)  // more available stock first
            .Take(20)
            .ToListAsync();

        if (candidates.Count == 0) return null;

        var enough = candidates.FirstOrDefault(c => (c.Quantity - c.ReservedQty) >= requiredBaseQty);
        var chosen = enough ?? candidates[0];
        var availableQty = Math.Max(0, chosen.Quantity - chosen.ReservedQty);
        var isPartial = enough == null && availableQty < requiredBaseQty;
        return new FefoAllocation(chosen.LocationId, chosen.LotNumber, chosen.ExpiryDate, Math.Min(requiredBaseQty, availableQty), isPartial);
    }




    [HttpGet]
    public async Task<IActionResult> GetConversionRate(int fromUomId, int toUomId, int? itemId = null)
    {
        if (fromUomId == toUomId) return Json(new { rate = 1m, found = true });

        var conversions = await _db.UnitConversions
            .Where(uc => uc.IsActive && (uc.ItemId == null || (itemId.HasValue && uc.ItemId == itemId.Value)))
            .ToListAsync();
        var rate = ResolveConversionRate(conversions, itemId ?? 0, fromUomId, toUomId);
        return rate.HasValue
            ? Json(new { rate = rate.Value, found = true })
            : Json(new { rate = 1m, found = false });
    }


    // ================================================================
    // DIRECTED PUTAWAY ENGINE (Enterprise WMS Core Feature)
    // Thuật toán gợi ý vị trí cất hàng tự động theo chuẩn Oracle WMS
    // ================================================================
    [HttpPost]
    public async Task<IActionResult> SuggestPutaway([FromBody] List<PutawayRequest> items)
    {
        if (items == null || items.Count == 0)
            return Json(new { success = false, error = "Không có dữ liệu vật tư để phân bổ." });

        try
        {
            var warehouseIds = items
                .Where(x => x.WarehouseId.HasValue && x.WarehouseId.Value > 0)
                .Select(x => x.WarehouseId!.Value)
                .Distinct()
                .ToList();
            var scopedWh = GetScopedWarehouseId();
            var warehouseId = scopedWh ?? warehouseIds.SingleOrDefault();
            if (warehouseId <= 0)
                return Json(new { success = false, error = "Không xác định được kho để gợi ý vị trí cất hàng." });
            if (scopedWh.HasValue && warehouseIds.Count > 0 && warehouseIds.Any(x => x != scopedWh.Value))
                return Json(new { success = false, error = "Bạn chỉ được phép gợi ý cất hàng trong kho đang được phân quyền." });

            // Pre-load all active locations with their zones (1 query)
            var allLocations = await _db.Locations
                .Include(l => l.Zone)
                .Where(l => l.IsActive && l.Zone != null && l.Zone.IsActive
                    && l.Zone.WarehouseId == warehouseId
                    && l.Zone.ZoneType == ZoneTypeEnum.Storage) // Chỉ gợi ý vào Zone Lưu Trữ
                .OrderBy(l => l.LocationCode)
                .ToListAsync();

            // Pre-load item-location records in this warehouse
            var itemIds = items.Select(x => x.ItemId).Distinct().ToList();
            var locationIds = allLocations.Select(l => l.LocationId).ToList();
            var existingILs = await _db.ItemLocations
                .Include(il => il.Item)
                .Where(il => locationIds.Contains(il.LocationId) && il.Quantity > 0)
                .ToListAsync();

            // Pre-load item master
            var itemMasters = await _db.Items
                .Where(i => itemIds.Contains(i.ItemId))
                .ToDictionaryAsync(i => i.ItemId, i => new
                {
                    i.DefaultLocationId,
                    i.Weight,
                    i.ItemType
                });

            var occupiedByOtherItem = existingILs
                .GroupBy(il => il.LocationId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(il => il.ItemId).Distinct().ToHashSet());

            var locationLoadLookup = existingILs
                .GroupBy(il => il.LocationId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(il =>
                    {
                        var item = il.Item;
                        var isChemical = item?.ItemType == ItemTypeEnum.HoaChat;
                        return isChemical ? il.Quantity : il.Quantity * (item?.Weight ?? 1m);
                    }));

            // Track which locations are already assigned in this batch
            var assignedLocationIds = new HashSet<int>();
            var plannedAddedLoad = new Dictionary<int, decimal>();
            var results = new List<object>();

            foreach (var req in items)
            {
                int? suggestedLocationId = null;
                string strategy = "";
                decimal bestScore = decimal.MinValue;
                string? reason = null;

                if (!itemMasters.TryGetValue(req.ItemId, out var itemMaster))
                {
                    results.Add(new
                    {
                        itemId = req.ItemId,
                        rowIndex = req.RowIndex,
                        locationId = (int?)null,
                        locationCode = "",
                        zoneName = "",
                        strategy = "Không tìm thấy thông tin vật tư"
                    });
                    continue;
                }

                var requestedQty = req.Quantity.GetValueOrDefault() > 0 ? req.Quantity!.Value : 1m;
                var requestedLoad = itemMaster.ItemType == ItemTypeEnum.HoaChat
                    ? requestedQty
                    : requestedQty * (itemMaster.Weight ?? 1m);

                bool FitsCapacity(int locationId)
                {
                    var currentLoad = locationLoadLookup.TryGetValue(locationId, out var load) ? load : 0m;
                    var pendingLoad = plannedAddedLoad.TryGetValue(locationId, out var added) ? added : 0m;
                    var maxCapacity = itemMaster.ItemType == ItemTypeEnum.HoaChat
                        ? SecurityHelpers.WarehouseCapacity.MaxChemicalLiters
                        : SecurityHelpers.WarehouseCapacity.MaxStorageKg;
                    return currentLoad + pendingLoad + requestedLoad <= maxCapacity;
                }

                decimal FillScore(int locationId)
                {
                    var currentLoad = locationLoadLookup.TryGetValue(locationId, out var load) ? load : 0m;
                    var pendingLoad = plannedAddedLoad.TryGetValue(locationId, out var added) ? added : 0m;
                    var maxCapacity = itemMaster.ItemType == ItemTypeEnum.HoaChat
                        ? SecurityHelpers.WarehouseCapacity.MaxChemicalLiters
                        : SecurityHelpers.WarehouseCapacity.MaxStorageKg;
                    var projectedFill = maxCapacity <= 0 ? 0 : ((currentLoad + pendingLoad + requestedLoad) / maxCapacity);
                    var distanceFromTarget = Math.Abs(0.6m - projectedFill); // prefer around 60% fill
                    return Math.Max(0, 100 - (distanceFromTarget * 100));
                }

                void EvaluateCandidate(int locationId, decimal score, string candidateStrategy, string candidateReason)
                {
                    if (assignedLocationIds.Contains(locationId)) return;
                    if (!FitsCapacity(locationId)) return;

                    var totalScore = score + FillScore(locationId);
                    if (totalScore > bestScore)
                    {
                        bestScore = totalScore;
                        suggestedLocationId = locationId;
                        strategy = candidateStrategy;
                        reason = candidateReason;
                    }
                }

                // STRATEGY 1: addition to exact lot / same item
                var existingBins = existingILs
                    .Where(il => il.ItemId == req.ItemId
                        && !assignedLocationIds.Contains(il.LocationId))
                    .OrderBy(il => req.ExpiryDate.HasValue ? (il.ExpiryDate == req.ExpiryDate ? 0 : 1) : 1)
                    .ThenBy(il => !string.IsNullOrWhiteSpace(req.LotNumber) && il.LotNumber == req.LotNumber ? 0 : 1)
                    .ThenByDescending(il => il.Quantity)
                    .ToList();

                foreach (var bin in existingBins)
                {
                    var loc = allLocations.FirstOrDefault(l => l.LocationId == bin.LocationId);
                    if (loc == null) continue;

                    var exactLotMatch =
                        (!string.IsNullOrWhiteSpace(req.LotNumber) && string.Equals(bin.LotNumber, req.LotNumber, StringComparison.OrdinalIgnoreCase))
                        || (req.ExpiryDate.HasValue && bin.ExpiryDate == req.ExpiryDate);

                    EvaluateCandidate(
                        bin.LocationId,
                        exactLotMatch ? 1000 : 900,
                        exactLotMatch ? "Gom hàng cùng lô / HSD" : "Gom hàng cùng vật tư",
                        exactLotMatch
                            ? "Ưu tiên nhập chung đúng lô/HSD để truy vết và kiểm đếm dễ hơn."
                            : "Ưu tiên gom chung vật tư để giảm phân tán vị trí.");
                }

                // STRATEGY 2: fixed bin / default location
                if (suggestedLocationId == null)
                {
                    if (itemMaster.DefaultLocationId.HasValue)
                    {
                        var defaultLocId = itemMaster.DefaultLocationId.Value;
                        var defaultLoc = allLocations.FirstOrDefault(l => l.LocationId == defaultLocId);
                        if (defaultLoc != null)
                        {
                            var occupiedItems = occupiedByOtherItem.TryGetValue(defaultLocId, out var itemSet)
                                ? itemSet
                                : new HashSet<int>();
                            var hasOtherItem = occupiedItems.Any(x => x != req.ItemId);
                            if (!hasOtherItem)
                            {
                                EvaluateCandidate(
                                    defaultLocId,
                                    850,
                                    "Vị trí mặc định (Fixed Bin)",
                                    "Ưu tiên đúng vị trí mặc định để nhân viên mới dễ tìm hàng và giữ chuẩn vị trí mặc định.");
                            }
                        }
                    }
                }

                // STRATEGY 3/4: empty bin same zone / any zone
                if (suggestedLocationId == null)
                {
                    int? preferredZoneId = null;
                    if (itemMaster.DefaultLocationId.HasValue)
                    {
                        preferredZoneId = allLocations.FirstOrDefault(l => l.LocationId == itemMaster.DefaultLocationId.Value)?.ZoneId;
                    }

                    var occupiedLocIds = existingILs.Select(il => il.LocationId).Distinct().ToHashSet();
                    var emptyBins = allLocations
                        .Where(l => !occupiedLocIds.Contains(l.LocationId)
                            && !assignedLocationIds.Contains(l.LocationId))
                        .ToList();

                    foreach (var emptyBin in emptyBins)
                    {
                        var sameZone = preferredZoneId.HasValue && emptyBin.ZoneId == preferredZoneId.Value;
                        EvaluateCandidate(
                            emptyBin.LocationId,
                            sameZone ? 700 : 600,
                            sameZone ? $"Ô trống cùng Zone ({emptyBin.Zone?.ZoneCode})" : $"Ô trống khả dụng ({emptyBin.Zone?.ZoneCode})",
                            sameZone
                                ? "Ưu tiên cùng khu vực mặc định để giảm quãng đường di chuyển."
                                : "Chọn ô trống khả dụng gần nhất theo thứ tự mã vị trí.");
                    }
                }

                // STRATEGY 5: fallback on same-item location regardless zone if still no fit
                if (suggestedLocationId == null)
                {
                    var fallback = existingILs
                        .Where(il => il.ItemId == req.ItemId && !assignedLocationIds.Contains(il.LocationId))
                        .OrderByDescending(il => il.Quantity - il.ReservedQty)
                        .FirstOrDefault();
                    if (fallback != null && FitsCapacity(fallback.LocationId))
                    {
                        suggestedLocationId = fallback.LocationId;
                        strategy = "Fallback cùng vật tư";
                        reason = "Kho đang chật, tạm thời gom vào vị trí đã có cùng vật tư để tránh tràn vị trí.";
                    }
                }

                if (suggestedLocationId.HasValue)
                {
                    assignedLocationIds.Add(suggestedLocationId.Value);
                    plannedAddedLoad[suggestedLocationId.Value] =
                        (plannedAddedLoad.TryGetValue(suggestedLocationId.Value, out var added) ? added : 0m) + requestedLoad;
                }

                var suggestedLoc = allLocations.FirstOrDefault(l => l.LocationId == suggestedLocationId);

                results.Add(new
                {
                    itemId = req.ItemId,
                    rowIndex = req.RowIndex,
                    locationId = suggestedLocationId,
                    locationCode = suggestedLoc?.LocationCode ?? "",
                    zoneName = suggestedLoc?.Zone?.ZoneName ?? "",
                    strategy = strategy,
                    reason = reason ?? "",
                    score = suggestedLocationId.HasValue ? bestScore : 0
                });
            }

            return Json(new { success = true, suggestions = results });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SuggestPutaway failed");
            return Json(new { success = false, error = UserSafeError.From(ex, "Không thể gợi ý vị trí cất hàng lúc này. Vui lòng thử lại.") });
        }
    }

}
