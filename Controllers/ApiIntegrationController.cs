using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WMS.Data;
using WMS.Models;
using WMS.Common;
using WMS.Services;

namespace WMS.Controllers;

/// <summary>
/// REST API cho tích hợp ERP/TMS — Enterprise Integration Layer.
/// Authentication: API Key via header X-API-Key (cấu hình trong appsettings.json).
/// Response format: { success, data, errors, pagination }
/// </summary>
[Route("api/v1")]
[ApiController]
[EnableRateLimiting("api")] // P1-7: dùng policy có sẵn (60 req/phút/user-IP) để chặn brute force / replay flood.
public class ApiIntegrationController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IInventoryBalanceService _inventoryBalanceService;
    private readonly IMheIntegrationService _mheIntegrationService;
    private readonly ICarrierIntegrationService _carrierIntegrationService;
    private readonly IEnterpriseIntegrationService _enterpriseIntegrationService;

    public ApiIntegrationController(AppDbContext db, IConfiguration config, IInventoryBalanceService inventoryBalanceService, IMheIntegrationService mheIntegrationService, ICarrierIntegrationService carrierIntegrationService, IEnterpriseIntegrationService? enterpriseIntegrationService = null)
    {
        _db = db;
        _config = config;
        _inventoryBalanceService = inventoryBalanceService;
        _mheIntegrationService = mheIntegrationService;
        _carrierIntegrationService = carrierIntegrationService;
        _enterpriseIntegrationService = enterpriseIntegrationService ?? new EnterpriseIntegrationService(db);
    }

    private bool ValidateApiKey()
    {
        var configKey = _config["Api:Key"];
        if (string.IsNullOrWhiteSpace(configKey)) return false;
        var headerKey = Request.Headers["X-API-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(headerKey)) return false;

        var configKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(configKey));
        var headerKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(headerKey));
        return CryptographicOperations.FixedTimeEquals(headerKeyHash, configKeyHash);
    }

    /// <summary>
    /// P1-1: chỉ trả về UnitCost / UnitPrice / LineAmount / TotalAmount khi cấu hình bật.
    /// Mặc định false để tránh rò rỉ giá vốn cho partner ngoài chia sẻ chung 1 API key.
    /// </summary>
    private bool ExposeFinancialFields()
        => string.Equals(_config["Api:ExposeFinancialFields"], "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Giới hạn API scope theo kho — nếu cấu hình Api:ScopedWarehouseId, chỉ cho truy vấn kho đó.
    /// Chuẩn bị cho P3-06 multi-tenant.
    /// </summary>
    private int? GetApiScopedWarehouseId()
    {
        var raw = _config["Api:ScopedWarehouseId"];
        return int.TryParse(raw, out var id) ? id : null;
    }

    private int? GetApiScopedOwnerPartnerId()
    {
        var raw = _config["Api:ScopedOwnerPartnerId"]
            ?? Environment.GetEnvironmentVariable("Api__ScopedOwnerPartnerId")
            ?? Environment.GetEnvironmentVariable("Api:ScopedOwnerPartnerId");
        return int.TryParse(raw, out var id) ? id : null;
    }

    private bool IsWarehouseInApiScope(int? warehouseId)
    {
        var scopedWh = GetApiScopedWarehouseId();
        return !scopedWh.HasValue || (warehouseId.HasValue && warehouseId.Value == scopedWh.Value);
    }

    private bool IsOwnerInApiScope(int? ownerPartnerId)
    {
        var scopedOwner = GetApiScopedOwnerPartnerId();
        return !scopedOwner.HasValue || (ownerPartnerId.HasValue && ownerPartnerId.Value == scopedOwner.Value);
    }

    private bool IsApiScopeAllowed(int? warehouseId, int? ownerPartnerId)
        => IsWarehouseInApiScope(warehouseId) && IsOwnerInApiScope(ownerPartnerId);

    private IActionResult ForbiddenScope403() =>
        StatusCode(403, new { success = false, code = "API_SCOPE_FORBIDDEN", errors = new[] { "Du lieu khong thuoc pham vi tich hop duoc cap." } });

    private IActionResult Unauthorized401() =>
        StatusCode(401, new { success = false, errors = new[] { "Khóa tích hợp không hợp lệ hoặc chưa cung cấp. Vui lòng gửi header X-API-Key." } });

    // ═══════════════════════════════════════════════════════════════
    // GET /api/v1/items — Danh sách vật tư
    // ═══════════════════════════════════════════════════════════════
    [HttpGet("items")]
    public async Task<IActionResult> GetItems(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] bool? active,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (!ValidateApiKey()) return Unauthorized401();
        pageSize = Math.Clamp(pageSize, 1, 200);
        var scopedWh = GetApiScopedWarehouseId();
        var scopedOwner = GetApiScopedOwnerPartnerId();

        var q = _db.Items.Include(i => i.BaseUom).Include(i => i.Category).AsQueryable();
        if (scopedOwner.HasValue) q = q.Where(i => !i.OwnerPartnerId.HasValue || i.OwnerPartnerId == scopedOwner.Value);
        if (active.HasValue) q = q.Where(i => i.IsActive == active.Value);
        if (categoryId.HasValue) q = q.Where(i => i.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(i => i.ItemCode.Contains(search) || i.ItemName.Contains(search) || (i.Barcode != null && i.Barcode.Contains(search)));

        var total = await q.CountAsync();
        var itemRows = await q.OrderBy(i => i.ItemCode)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();
        var stockByItem = await _inventoryBalanceService.GetStockByItemAsync(scopedWh, itemRows.Select(i => i.ItemId), scopedOwner);

        var exposeFinancial = ExposeFinancialFields(); // P1-1
        var items = itemRows.Select(i => new
        {
            i.ItemId,
            i.ItemCode,
            i.ItemName,
            i.Barcode,
            i.SkuCode,
            ItemType = i.ItemType.ToString(),
            BaseUom = i.BaseUom != null ? i.BaseUom.UomCode : null,
            Category = i.Category != null ? i.Category.CategoryName : null,
            CurrentStock = stockByItem.TryGetValue(i.ItemId, out var qty) ? qty : 0m,
            i.MinThreshold,
            i.MaxThreshold,
            i.ReorderPoint,
            i.Weight,
            i.Length,
            i.Width,
            i.Height,
            i.TrackExpiry,
            i.TrackLot,
            i.TrackSerial,
            UnitCost = exposeFinancial ? i.UnitCost : 0m,
            i.AbcClass,
            i.IsActive
        }).ToList();

        return Ok(new
        {
            success = true,
            data = items,
            pagination = new { page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // GET /api/v1/stock — Tồn kho hiện tại
    // ═══════════════════════════════════════════════════════════════
    [HttpGet("stock")]
    public async Task<IActionResult> GetStock(
        [FromQuery] int? warehouseId,
        [FromQuery] int? itemId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        if (!ValidateApiKey()) return Unauthorized401();
        pageSize = Math.Clamp(pageSize, 1, 500);
        var scopedWh = GetApiScopedWarehouseId();
        var scopedOwner = GetApiScopedOwnerPartnerId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        var q = _db.ItemLocations
            .Include(il => il.Item)
            .Include(il => il.Location).ThenInclude(l => l!.Zone)
            .Where(il => il.Quantity > 0)
            .AsQueryable();

        if (itemId.HasValue) q = q.Where(il => il.ItemId == itemId.Value);
        if (warehouseId.HasValue) q = q.Where(il => il.Location != null && il.Location.Zone != null && il.Location.Zone.WarehouseId == warehouseId.Value);
        if (scopedOwner.HasValue) q = q.Where(il => il.OwnerPartnerId == scopedOwner.Value);

        var total = await q.CountAsync();
        var stock = await q.OrderBy(il => il.Item!.ItemCode).ThenBy(il => il.Location!.LocationCode)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(il => new
            {
                il.ItemLocationId,
                ItemCode = il.Item!.ItemCode,
                ItemName = il.Item.ItemName,
                LocationCode = il.Location!.LocationCode,
                WarehouseId = il.Location.Zone != null ? il.Location.Zone.WarehouseId : 0,
                il.Quantity,
                il.ReservedQty,
                AvailableQty = il.Quantity - il.ReservedQty,
                il.LotNumber,
                il.ExpiryDate,
                HoldStatus = il.HoldStatus.ToString()
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = stock,
            pagination = new { page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // GET /api/v1/vouchers — Danh sách phiếu
    // ═══════════════════════════════════════════════════════════════
    [HttpGet("vouchers")]
    public async Task<IActionResult> GetVouchers(
        [FromQuery] int? warehouseId,
        [FromQuery] byte? voucherType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] bool? posted,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (!ValidateApiKey()) return Unauthorized401();
        pageSize = Math.Clamp(pageSize, 1, 200);
        var scopedWh = GetApiScopedWarehouseId();
        var scopedOwner = GetApiScopedOwnerPartnerId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        var q = _db.Vouchers.Include(v => v.Warehouse).Include(v => v.Partner).AsQueryable();
        if (warehouseId.HasValue) q = q.Where(v => v.WarehouseId == warehouseId.Value);
        if (scopedOwner.HasValue) q = q.Where(v => v.OwnerPartnerId == scopedOwner.Value);
        if (voucherType.HasValue) q = q.Where(v => (byte)v.VoucherType == voucherType.Value);
        if (fromDate.HasValue) q = q.Where(v => v.VoucherDate >= fromDate.Value.Date);
        if (toDate.HasValue) q = q.Where(v => v.VoucherDate <= toDate.Value.Date);
        if (posted.HasValue) q = q.Where(v => v.IsPosted == posted.Value);

        var total = await q.CountAsync();
        var exposeFinancial = ExposeFinancialFields(); // P1-1
        var vouchers = await q.OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(v => new
            {
                v.VoucherId,
                v.VoucherCode,
                VoucherType = v.VoucherType.ToString(),
                v.VoucherDate,
                Warehouse = v.Warehouse != null ? v.Warehouse.WarehouseCode : null,
                Partner = v.Partner != null ? v.Partner.PartnerName : null,
                TotalAmount = exposeFinancial ? v.TotalAmount : 0m,
                v.CurrencyCode,
                v.TotalLines,
                v.IsPosted,
                v.IsCancelled,
                InboundStatus = v.InboundStatus.ToString(),
                FulfillmentStatus = v.FulfillmentStatus.ToString(),
                v.AsnCode,
                v.TrackingNumber,
                v.ManifestCode,
                v.CreatedAt,
                v.CreatedBy
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = vouchers,
            pagination = new { page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // GET /api/v1/vouchers/{id} — Chi tiết phiếu
    // ═══════════════════════════════════════════════════════════════
    [HttpGet("vouchers/{id}")]
    public async Task<IActionResult> GetVoucherDetail(long id)
    {
        if (!ValidateApiKey()) return Unauthorized401();

        var v = await _db.Vouchers
            .Include(x => x.Warehouse).Include(x => x.Partner)
            .Include(x => x.Details).ThenInclude(d => d.Item)
            .Include(x => x.Details).ThenInclude(d => d.Location)
            .FirstOrDefaultAsync(x => x.VoucherId == id);

        if (v == null) return NotFound(new { success = false, errors = new[] { "Không tìm thấy phiếu." } });

        if (!IsApiScopeAllowed(v.WarehouseId, v.OwnerPartnerId)
            || v.Details.Any(d => !IsOwnerInApiScope(d.OwnerPartnerId)))
            return ForbiddenScope403();

        var exposeFinancial = ExposeFinancialFields(); // P1-1
        return Ok(new
        {
            success = true,
            data = new
            {
                v.VoucherId,
                v.VoucherCode,
                VoucherType = v.VoucherType.ToString(),
                v.VoucherDate,
                Warehouse = v.Warehouse?.WarehouseCode,
                Partner = v.Partner?.PartnerName,
                TotalAmount = exposeFinancial ? v.TotalAmount : 0m,
                v.CurrencyCode,
                v.TotalLines,
                v.IsPosted,
                v.IsCancelled,
                InboundStatus = v.InboundStatus.ToString(),
                FulfillmentStatus = v.FulfillmentStatus.ToString(),
                v.AsnCode,
                v.CreatedAt,
                v.CreatedBy,
                Details = v.Details.Select(d => new
                {
                    d.VoucherDetailId,
                    d.LineNumber,
                    ItemCode = d.Item?.ItemCode,
                    ItemName = d.Item?.ItemName,
                    LocationCode = d.Location?.LocationCode,
                    d.TransactionQty,
                    d.BaseQty,
                    UnitPrice = exposeFinancial ? d.UnitPrice : 0m,
                    LineAmount = exposeFinancial ? d.LineAmount : 0m,
                    d.LotNumber,
                    d.ExpiryDate,
                    d.ManufacturingDate,
                    QualityStatus = d.QualityStatus.ToString(),
                    d.DefectQty,
                    d.DefectBaseQty
                })
            }
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // GET /api/v1/kpi — Dashboard KPI data
    // ═══════════════════════════════════════════════════════════════
    [HttpGet("kpi")]
    public async Task<IActionResult> GetKpi([FromQuery] int? warehouseId)
    {
        if (!ValidateApiKey()) return Unauthorized401();
        var scopedWh = GetApiScopedWarehouseId();
        var scopedOwner = GetApiScopedOwnerPartnerId();
        if (scopedWh.HasValue) warehouseId = scopedWh.Value;

        var today = VietnamTime.Now.Date;
        var thirtyDaysAgo = today.AddDays(-30);

        var vouchersQ = _db.Vouchers.AsQueryable();
        if (warehouseId.HasValue) vouchersQ = vouchersQ.Where(v => v.WarehouseId == warehouseId.Value);
        if (scopedOwner.HasValue) vouchersQ = vouchersQ.Where(v => v.OwnerPartnerId == scopedOwner.Value);

        var activeItemsQ = _db.Items.AsNoTracking().Where(i => i.IsActive);
        if (scopedOwner.HasValue) activeItemsQ = activeItemsQ.Where(i => !i.OwnerPartnerId.HasValue || i.OwnerPartnerId == scopedOwner.Value);
        var totalItems = await activeItemsQ.CountAsync();
        var stockByItemForKpi = await _inventoryBalanceService.GetStockByItemAsync(warehouseId, ownerPartnerId: scopedOwner);
        var totalStock = stockByItemForKpi.Values.Sum();
        var activeItems = await activeItemsQ
            .Select(i => new { i.ItemId, i.MinThreshold })
            .ToListAsync();
        var lowStockCount = activeItems.Count(i =>
            i.MinThreshold > 0
            && stockByItemForKpi.TryGetValue(i.ItemId, out var qty)
            && qty > 0
            && qty <= i.MinThreshold);
        var inboundToday = await vouchersQ.CountAsync(v => v.VoucherDate == today && v.VoucherType == VoucherTypeEnum.NhapKho);
        var outboundToday = await vouchersQ.CountAsync(v => v.VoucherDate == today && v.VoucherType == VoucherTypeEnum.XuatKho);
        var pendingApproval = await vouchersQ.CountAsync(v => v.InboundStatus == InboundStatusEnum.PendingApproval && !v.IsCancelled);
        var exceptionsQ = _db.OperationExceptionCases.AsQueryable();
        if (warehouseId.HasValue) exceptionsQ = exceptionsQ.Where(e => e.WarehouseId == warehouseId.Value);
        var openExceptions = await exceptionsQ.CountAsync(e => e.Status != OperationExceptionStatusEnum.Resolved);
        var pickTasksQ = _db.PickTasks.AsQueryable();
        if (warehouseId.HasValue) pickTasksQ = pickTasksQ.Where(t => t.SourceLocation.Zone.WarehouseId == warehouseId.Value);
        if (scopedOwner.HasValue) pickTasksQ = pickTasksQ.Where(t => t.OwnerPartnerId == scopedOwner.Value);
        var openPickTasks = await pickTasksQ.CountAsync(t => t.Status != PickTaskStatusEnum.Completed && t.Status != PickTaskStatusEnum.Cancelled);

        // Throughput last 30 days
        var inbound30d = await vouchersQ.CountAsync(v => v.VoucherDate >= thirtyDaysAgo && v.VoucherType == VoucherTypeEnum.NhapKho && v.IsPosted);
        var outbound30d = await vouchersQ.CountAsync(v => v.VoucherDate >= thirtyDaysAgo && v.VoucherType == VoucherTypeEnum.XuatKho && v.IsPosted);

        return Ok(new
        {
            success = true,
            data = new
            {
                asOfDate = VietnamTime.Now,
                totalActiveItems = totalItems,
                totalStock,
                lowStockCount,
                inboundToday,
                outboundToday,
                pendingApproval,
                openExceptions,
                openPickTasks,
                throughput30Days = new { inbound = inbound30d, outbound = outbound30d }
            }
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // GET /api/v1/docs — Tài liệu giao tiếp hệ thống
    // ═══════════════════════════════════════════════════════════════
    [HttpGet("docs")]
    [Produces("application/json")]
    public IActionResult GetDocs()
    {
        return Ok(new
        {
            api = "Tài liệu giao tiếp WMS Pro v1",
            version = "1.0",
            auth = "Bắt buộc gửi khóa tích hợp qua header X-API-Key",
            endpoints = new[]
            {
                new { method = "GET", path = "/api/v1/items", description = "Danh sách vật tư có phân trang và bộ lọc tìm kiếm/nhóm/trạng thái" },
                new { method = "GET", path = "/api/v1/stock", description = "Tồn kho hiện tại theo kho hoặc mã hàng" },
                new { method = "GET", path = "/api/v1/vouchers", description = "Danh sách phiếu theo kho, loại phiếu, ngày và trạng thái" },
                new { method = "GET", path = "/api/v1/vouchers/{id}", description = "Chi tiết phiếu với dòng hàng" },
                new { method = "GET", path = "/api/v1/kpi", description = "Chỉ số điều hành gồm tồn kho, sản lượng và bất thường" },
                new { method = "POST", path = "/api/v1/items", description = "Tạo vật tư mới" },
                new { method = "PUT", path = "/api/v1/items/{id}", description = "Cập nhật vật tư" },
                new { method = "POST", path = "/api/v1/vouchers", description = "Tạo phiếu nhập/xuất kho" },
                new { method = "GET", path = "/api/v1/docs", description = "Tài liệu giao tiếp của hệ thống" }
            }
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // POST /api/v1/items — Tạo vật tư mới
    // ═══════════════════════════════════════════════════════════════
    [HttpPost("items")]
    public async Task<IActionResult> CreateItem([FromBody] ApiCreateItemRequest req)
    {
        if (!ValidateApiKey()) return Unauthorized401();
        if (string.IsNullOrWhiteSpace(req.ItemCode) || string.IsNullOrWhiteSpace(req.ItemName))
            return BadRequest(new { success = false, errors = new[] { "ItemCode và ItemName là bắt buộc." } });
        // P0-3: chặn UnitCost âm để không tạo item có giá trị tồn kho âm.
        if (req.UnitCost.HasValue && req.UnitCost.Value < 0)
            return BadRequest(new { success = false, errors = new[] { "UnitCost không được âm." } });

        var exists = await _db.Items.AnyAsync(i => i.ItemCode == req.ItemCode.Trim());
        if (exists)
            return Conflict(new { success = false, errors = new[] { $"Mã vật tư [{req.ItemCode}] đã tồn tại." } });

        var uomId = req.BaseUomId > 0 ? req.BaseUomId : await _db.UnitsOfMeasure.Select(u => u.UomId).FirstOrDefaultAsync();
        if (uomId <= 0)
            return BadRequest(new { success = false, errors = new[] { "Hệ thống chưa cấu hình đơn vị tính (UoM)." } });

        var item = new Item
        {
            ItemCode = req.ItemCode.Trim(),
            ItemName = req.ItemName.Trim(),
            ItemType = (ItemTypeEnum)(req.ItemType ?? 1),
            BaseUomId = uomId,
            CategoryId = req.CategoryId,
            UnitCost = req.UnitCost ?? 0,
            Barcode = req.Barcode?.Trim(),
            SkuCode = req.SkuCode?.Trim(),
            IsActive = true,
            CreatedBy = "API",
            CreatedAt = VietnamTime.Now
        };
        _db.Items.Add(item);
        await _db.SaveChangesAsync();

        return StatusCode(201, new { success = true, data = new { item.ItemId, item.ItemCode, item.ItemName } });
    }

    // ═══════════════════════════════════════════════════════════════
    // PUT /api/v1/items/{id} — Cập nhật vật tư
    // ═══════════════════════════════════════════════════════════════
    [HttpPut("items/{id}")]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] ApiUpdateItemRequest req)
    {
        if (!ValidateApiKey()) return Unauthorized401();
        var item = await _db.Items.FindAsync(id);
        if (item == null)
            return NotFound(new { success = false, errors = new[] { $"Không tìm thấy vật tư ID={id}." } });

        // P0-3: chặn UnitCost âm khi cập nhật.
        if (req.UnitCost.HasValue && req.UnitCost.Value < 0)
            return BadRequest(new { success = false, errors = new[] { "UnitCost không được âm." } });

        if (!string.IsNullOrWhiteSpace(req.ItemName)) item.ItemName = req.ItemName.Trim();
        if (req.UnitCost.HasValue) item.UnitCost = req.UnitCost.Value;
        if (req.CategoryId.HasValue) item.CategoryId = req.CategoryId.Value;
        if (!string.IsNullOrWhiteSpace(req.Barcode)) item.Barcode = req.Barcode.Trim();
        if (!string.IsNullOrWhiteSpace(req.SkuCode)) item.SkuCode = req.SkuCode.Trim();
        if (req.IsActive.HasValue) item.IsActive = req.IsActive.Value;
        item.UpdatedAt = VietnamTime.Now;

        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = new { item.ItemId, item.ItemCode, item.ItemName, item.IsActive } });
    }

    // ═══════════════════════════════════════════════════════════════
    // POST /api/v1/vouchers — Tạo phiếu nhập/xuất
    // ═══════════════════════════════════════════════════════════════
    [HttpPost("vouchers")]
    public async Task<IActionResult> CreateVoucher([FromBody] ApiCreateVoucherRequest req)
    {
        if (!ValidateApiKey()) return Unauthorized401();
        if (req.WarehouseId <= 0)
            return BadRequest(new { success = false, errors = new[] { "WarehouseId là bắt buộc." } });
        if (req.Lines == null || req.Lines.Count == 0)
            return BadRequest(new { success = false, errors = new[] { "Phải có ít nhất 1 dòng hàng." } });

        // P0-3: nếu API bị scope theo kho, từ chối ghi sang kho khác (trước đây chỉ GET endpoint mới override).
        var scopedWh = GetApiScopedWarehouseId();
        if (scopedWh.HasValue && req.WarehouseId != scopedWh.Value)
            return StatusCode(403, new { success = false, errors = new[] { "API key bị giới hạn theo kho khác." } });

        var warehouse = await _db.Warehouses.FindAsync(req.WarehouseId);
        if (warehouse == null)
            return BadRequest(new { success = false, errors = new[] { "Kho không tồn tại." } });

        var voucherType = (VoucherTypeEnum)(req.VoucherType ?? 1);
        var voucherCode = $"API-{VietnamTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var voucher = new Voucher
        {
            VoucherCode = voucherCode,
            VoucherType = voucherType,
            WarehouseId = req.WarehouseId,
            PartnerId = req.PartnerId,
            VoucherDate = VietnamTime.Now.Date,
            ReferenceNo = req.ReferenceNo?.Trim(),
            Description = req.Description?.Trim(),
            CreatedBy = "API",
            CreatedAt = VietnamTime.Now
        };

        // P1-4: pre-load Items để tránh N+1 query trong vòng lặp.
        var itemIds = req.Lines.Select(l => l.ItemId).Where(id => id > 0).Distinct().ToList();
        var itemsById = await _db.Items
            .Where(i => itemIds.Contains(i.ItemId))
            .ToDictionaryAsync(i => i.ItemId);

        // P0-3: pre-load location-warehouse mapping để validate LocationId thuộc đúng kho.
        var locationIds = req.Lines.Where(l => l.LocationId.HasValue).Select(l => l.LocationId!.Value).Distinct().ToList();
        var locationWarehouseMap = locationIds.Count > 0
            ? await _db.Locations.AsNoTracking()
                .Where(l => locationIds.Contains(l.LocationId) && l.Zone != null)
                .Select(l => new { l.LocationId, WarehouseId = l.Zone!.WarehouseId })
                .ToDictionaryAsync(x => x.LocationId, x => x.WarehouseId)
            : new Dictionary<int, int>();

        var errors = new List<string>();
        foreach (var (line, idx) in req.Lines.Select((l, i) => (l, i)))
        {
            if (!itemsById.TryGetValue(line.ItemId, out var item))
            { errors.Add($"Dòng {idx + 1}: ItemId={line.ItemId} không tồn tại."); continue; }
            if (line.Quantity <= 0) { errors.Add($"Dòng {idx + 1}: Quantity phải > 0."); continue; }
            if (line.UnitPrice.HasValue && line.UnitPrice.Value < 0)
            { errors.Add($"Dòng {idx + 1}: UnitPrice không được âm."); continue; }
            if (line.LocationId.HasValue
                && (!locationWarehouseMap.TryGetValue(line.LocationId.Value, out var locWhId) || locWhId != req.WarehouseId))
            { errors.Add($"Dòng {idx + 1}: LocationId={line.LocationId} không thuộc kho {req.WarehouseId}."); continue; }

            voucher.Details.Add(new VoucherDetail
            {
                ItemId = line.ItemId,
                LocationId = line.LocationId,
                TransactionQty = line.Quantity,
                BaseQty = line.Quantity,
                TransactionUomId = item.BaseUomId,
                UnitPrice = line.UnitPrice ?? item.UnitCost,
                LineAmount = (line.UnitPrice ?? item.UnitCost) * line.Quantity,
                LotNumber = line.LotNumber?.Trim()
            });
        }

        if (errors.Count > 0)
            return BadRequest(new { success = false, errors });

        voucher.TotalLines = voucher.Details.Count;
        voucher.TotalAmount = voucher.Details.Sum(d => d.LineAmount);
        _db.Vouchers.Add(voucher);
        await _db.SaveChangesAsync();

        return StatusCode(201, new { success = true, data = new { voucher.VoucherId, voucher.VoucherCode, voucher.TotalLines, voucher.TotalAmount } });
    }

    [HttpPost("mhe/callback")]
    public async Task<IActionResult> MheCallback([FromBody] MheCallbackRequest request)
    {
        if (!ValidateApiKey()) return Unauthorized401();
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var headerKey = Request.Headers["X-Idempotency-Key"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerKey))
            {
                request = new MheCallbackRequest
                {
                    CorrelationId = request.CorrelationId,
                    IdempotencyKey = headerKey,
                    Status = request.Status,
                    ExternalMissionId = request.ExternalMissionId,
                    Message = request.Message,
                    PayloadJson = request.PayloadJson
                };
            }
        }

        try
        {
            var command = await _mheIntegrationService.ProcessCallbackAsync(request);
            return Ok(new
            {
                success = true,
                data = new
                {
                    command.MheCommandId,
                    command.CommandCode,
                    command.CorrelationId,
                    status = command.Status.ToString(),
                    command.UpdatedAt
                }
            });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { success = false, errors = new[] { UserSafeError.From(ex) }, code = ex.Code });
        }
    }

    [HttpPost("carrier/callback")]
    public async Task<IActionResult> CarrierCallback([FromBody] CarrierShipmentCallbackRequest request)
    {
        if (!ValidateApiKey()) return Unauthorized401();
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var headerKey = Request.Headers["X-Idempotency-Key"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerKey))
            {
                request = new CarrierShipmentCallbackRequest
                {
                    CorrelationId = request.CorrelationId,
                    IdempotencyKey = headerKey,
                    Status = request.Status,
                    TrackingNumber = request.TrackingNumber,
                    ExternalShipmentId = request.ExternalShipmentId,
                    LabelUrl = request.LabelUrl,
                    ProofOfDeliveryUrl = request.ProofOfDeliveryUrl,
                    Message = request.Message,
                    PayloadJson = request.PayloadJson
                };
            }
        }

        try
        {
            var shipment = await _carrierIntegrationService.ProcessCallbackAsync(request);
            return Ok(new
            {
                success = true,
                data = new
                {
                    shipment.CarrierShipmentId,
                    shipment.CorrelationId,
                    status = shipment.Status.ToString(),
                    shipment.TrackingNumber,
                    shipment.UpdatedAt
                }
            });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { success = false, errors = new[] { UserSafeError.From(ex) }, code = ex.Code });
        }
    }

    [HttpGet("openapi.json")]
    public IActionResult OpenApiJson()
    {
        if (!ValidateApiKey()) return Unauthorized401();
        return Ok(_enterpriseIntegrationService.BuildOpenApiContract());
    }

    [HttpPost("edi/import")]
    public async Task<IActionResult> ImportEdi([FromBody] ApiEdiImportRequest request)
    {
        if (!ValidateApiKey()) return Unauthorized401();
        var scopedWh = GetApiScopedWarehouseId();
        var scopedOwner = GetApiScopedOwnerPartnerId();
        var warehouseId = request.WarehouseId ?? scopedWh;
        var partnerId = request.PartnerId ?? scopedOwner;
        if (!IsApiScopeAllowed(warehouseId, partnerId)) return ForbiddenScope403();

        var message = await _enterpriseIntegrationService.ImportEdiAsync(request.MessageType, request.Payload, request.FileName, warehouseId, partnerId, "API");
        return StatusCode(message.Status == EdiMessageStatusEnum.Rejected ? 422 : 201, new
        {
            success = message.Status != EdiMessageStatusEnum.Rejected,
            data = new { message.EdiMessageId, message.MessageType, message.ControlNumber, status = message.Status.ToString(), message.RejectReport },
            errors = message.Status == EdiMessageStatusEnum.Rejected ? new[] { message.RejectReport } : Array.Empty<string>()
        });
    }

    [HttpPost("edi/{id:long}/replay")]
    public async Task<IActionResult> ReplayEdi(long id)
    {
        if (!ValidateApiKey()) return Unauthorized401();
        var originalScope = await _db.EdiMessages.AsNoTracking()
            .Where(x => x.EdiMessageId == id)
            .Select(x => new { x.WarehouseId, x.PartnerId })
            .FirstOrDefaultAsync();
        if (originalScope == null)
            return NotFound(new { success = false, errors = new[] { "EDI message not found." } });
        if (!IsApiScopeAllowed(originalScope.WarehouseId, originalScope.PartnerId)) return ForbiddenScope403();

        try
        {
            var replay = await _enterpriseIntegrationService.ReplayEdiAsync(id, "API");
            return Ok(new { success = true, data = new { replay.EdiMessageId, replay.ControlNumber, replay.ReplayOfMessageId } });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { success = false, errors = new[] { UserSafeError.From(ex) }, code = ex.Code });
        }
    }

    [HttpGet("edi/{id:long}/export")]
    public async Task<IActionResult> ExportEdi(long id)
    {
        if (!ValidateApiKey()) return Unauthorized401();
        var message = await _db.EdiMessages.AsNoTracking().FirstOrDefaultAsync(x => x.EdiMessageId == id);
        if (message == null)
            return NotFound(new { success = false, errors = new[] { "EDI message not found." } });
        if (!IsApiScopeAllowed(message.WarehouseId, message.PartnerId)) return ForbiddenScope403();
        return File(System.Text.Encoding.UTF8.GetBytes(message.Payload), "text/plain", $"{message.ControlNumber}.edi");
    }

    [HttpPost("shipments/{carrierShipmentId:long}/confirm")]
    public async Task<IActionResult> ConfirmShipment(long carrierShipmentId)
    {
        if (!ValidateApiKey()) return Unauthorized401();
        var shipment = await _db.CarrierShipments.FirstOrDefaultAsync(x => x.CarrierShipmentId == carrierShipmentId);
        if (shipment == null)
            return NotFound(new { success = false, errors = new[] { "Carrier shipment not found." } });

        // P1-3: chặn ghi đè trạng thái cuối khác — không cho phép confirm shipment đã Cancelled/Failed/DeliveryFailed.
        if (!IsApiScopeAllowed(shipment.WarehouseId, shipment.OwnerPartnerId)) return ForbiddenScope403();

        if (shipment.Status is CarrierShipmentStatusEnum.Cancelled
            or CarrierShipmentStatusEnum.Failed
            or CarrierShipmentStatusEnum.DeliveryFailed)
        {
            return BadRequest(new { success = false, errors = new[] { $"Shipment đã ở trạng thái cuối khác Delivered ({shipment.Status})." } });
        }
        // Idempotent: nếu đã Delivered thì không emit outbox event lần 2.
        if (shipment.Status == CarrierShipmentStatusEnum.Delivered)
        {
            return Ok(new { success = true, data = new { shipment.CarrierShipmentId, status = shipment.Status.ToString(), shipment.DeliveredAt, outboxId = (long?)null } });
        }

        shipment.Status = CarrierShipmentStatusEnum.Delivered;
        shipment.DeliveredAt ??= VietnamTime.Now;
        shipment.UpdatedAt = VietnamTime.Now;
        await _db.SaveChangesAsync();
        var outbox = await _enterpriseIntegrationService.EmitOutboxEventAsync(
            OutboxEventTypeEnum.ShipmentConfirmed,
            "mock://shipment-confirmed",
            new { shipment.CarrierShipmentId, shipment.VoucherId, shipment.TrackingNumber, shipment.DeliveredAt },
            $"shipment-confirmed:{shipment.CarrierShipmentId}",
            "API");
        return Ok(new { success = true, data = new { shipment.CarrierShipmentId, status = shipment.Status.ToString(), shipment.DeliveredAt, outbox.OutboxId } });
    }

    [HttpPost("3pl/invoices/{invoiceId:long}/issue")]
    public async Task<IActionResult> IssueThreePlInvoice(long invoiceId)
    {
        if (!ValidateApiKey()) return Unauthorized401();
        var invoice = await _db.ThreePlInvoices.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.ThreePlInvoiceId == invoiceId);
        if (invoice == null)
            return NotFound(new { success = false, errors = new[] { "Không tìm thấy hóa đơn kho nhiều chủ hàng." } });
        if (!IsApiScopeAllowed(invoice.WarehouseId, invoice.OwnerPartnerId)) return ForbiddenScope403();
        var outbox = await _enterpriseIntegrationService.EmitOutboxEventAsync(
            OutboxEventTypeEnum.ThreePlInvoiceIssued,
            "mock://3pl-invoice-issued",
            new { invoice.ThreePlInvoiceId, invoice.InvoiceCode, invoice.OwnerPartnerId, invoice.TotalAmount, invoice.Currency },
            $"3pl-invoice-issued:{invoice.ThreePlInvoiceId}",
            "API");
        return Ok(new { success = true, data = new { invoice.ThreePlInvoiceId, invoice.InvoiceCode, outbox.OutboxId } });
    }

    [HttpPost("webhooks/{deliveryId:long}/replay")]
    public async Task<IActionResult> ReplayWebhook(long deliveryId)
    {
        if (!ValidateApiKey()) return Unauthorized401();
        try
        {
            var replay = await _enterpriseIntegrationService.ReplayWebhookAsync(deliveryId, "API");
            return Ok(new { success = true, data = new { replay.WebhookDeliveryId, replay.EventType, status = replay.Status.ToString() } });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { success = false, errors = new[] { UserSafeError.From(ex) }, code = ex.Code });
        }
    }
}

// ═══ API Request DTOs ═══
public class ApiEdiImportRequest
{
    public EdiMessageTypeEnum MessageType { get; set; } = EdiMessageTypeEnum.Asn;
    public string Payload { get; set; } = "";
    public string? FileName { get; set; }
    public int? WarehouseId { get; set; }
    public int? PartnerId { get; set; }
}

public class ApiCreateItemRequest
{
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public int? ItemType { get; set; }
    public int BaseUomId { get; set; }
    public int? CategoryId { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Barcode { get; set; }
    public string? SkuCode { get; set; }
}

public class ApiUpdateItemRequest
{
    public string? ItemName { get; set; }
    public decimal? UnitCost { get; set; }
    public int? CategoryId { get; set; }
    public string? Barcode { get; set; }
    public string? SkuCode { get; set; }
    public bool? IsActive { get; set; }
}

public class ApiCreateVoucherRequest
{
    public int? VoucherType { get; set; }
    public int WarehouseId { get; set; }
    public int? PartnerId { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Description { get; set; }
    public List<ApiVoucherLine> Lines { get; set; } = new();
}

public class ApiVoucherLine
{
    public int ItemId { get; set; }
    public int? LocationId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? LotNumber { get; set; }
}
