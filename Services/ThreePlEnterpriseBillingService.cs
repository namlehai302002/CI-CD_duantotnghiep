using Microsoft.EntityFrameworkCore;
using WMS.Common;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public sealed class ThreePlContractRequest
{
    public long? ContractId { get; init; }
    public int WarehouseId { get; init; }
    public int OwnerPartnerId { get; init; }
    public string? ContractCode { get; init; }
    public string? ContractName { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public string Currency { get; init; } = "VND";
    public decimal MinimumCharge { get; init; }
    public decimal TaxPercent { get; init; }
    public decimal DiscountPercent { get; init; }
    public bool RequiresAdjustmentApproval { get; init; } = true;
    public string? Notes { get; init; }
    public string Actor { get; init; } = "system";
}

public sealed class ThreePlContractRateRequest
{
    public long? ContractRateId { get; init; }
    public long ContractId { get; init; }
    public ThreePlChargeTypeEnum ChargeType { get; init; }
    public string? RateCode { get; init; }
    public string? ServiceCode { get; init; }
    public string ChargeUnit { get; init; } = "unit";
    public decimal UnitRate { get; init; }
    public decimal TierFromQty { get; init; }
    public decimal? TierToQty { get; init; }
    public decimal IncludedQty { get; init; }
    public decimal MinimumCharge { get; init; }
    public decimal SurchargePercent { get; init; }
    public decimal OffHoursSurcharge { get; init; }
    public decimal UrgentSurcharge { get; init; }
    public decimal HazmatSurcharge { get; init; }
    public decimal ColdStorageSurcharge { get; init; }
    public decimal ManualHandlingSurcharge { get; init; }
    public decimal SlaPenaltyPercent { get; init; }
    public decimal SlaBonusPercent { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public bool IsActive { get; init; } = true;
    public string? Notes { get; init; }
}

public sealed class ThreePlRatingRequest
{
    public int WarehouseId { get; init; }
    public int OwnerPartnerId { get; init; }
    public ThreePlChargeTypeEnum ChargeType { get; init; }
    public decimal Quantity { get; init; }
    public DateTime ServiceDate { get; init; } = VietnamTime.Now.Date;
    public bool IsOffHours { get; init; }
    public bool IsUrgent { get; init; }
    public bool IsHazmat { get; init; }
    public bool IsColdStorage { get; init; }
    public bool IsManualHandling { get; init; }
    public bool SlaBreached { get; init; }
    public bool SlaExceeded { get; init; }
}

public sealed record ThreePlRatingResult(
    long? ContractId,
    long? ContractRateId,
    decimal Quantity,
    decimal UnitRate,
    decimal SubtotalAmount,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal AdjustmentAmount,
    decimal TotalAmount,
    string Currency,
    string ChargeUnit);

public interface IThreePlEnterpriseBillingService
{
    Task<ThreePlContract> SaveContractAsync(ThreePlContractRequest request, int? scopedWarehouseId = null, CancellationToken ct = default);
    Task<ThreePlContractRate> SaveContractRateAsync(ThreePlContractRateRequest request, int? scopedWarehouseId = null, CancellationToken ct = default);
    Task<ThreePlRatingResult> RateAsync(ThreePlRatingRequest request, CancellationToken ct = default);
    Task<ThreePlInvoice> GenerateInvoiceFromRunAsync(long runId, int? scopedWarehouseId, string actor, CancellationToken ct = default);
    Task<ThreePlInvoice> ConfirmInvoiceAsync(long invoiceId, int? scopedWarehouseId, string actor, CancellationToken ct = default);
    Task<ThreePlDispute> CreateDisputeAsync(long invoiceLineId, decimal requestedAmount, string reason, string actor, CancellationToken ct = default);
    Task<ThreePlDispute> ResolveDisputeAsync(long disputeId, bool approve, decimal approvedAmount, string response, int? scopedWarehouseId, string actor, CancellationToken ct = default);
}

public class ThreePlEnterpriseBillingService : IThreePlEnterpriseBillingService
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public ThreePlEnterpriseBillingService(AppDbContext db, IUnitOfWork? unitOfWork = null)
    {
        _db = db;
        _unitOfWork = unitOfWork ?? new EfUnitOfWork(db);
    }

    private static DateTime Now => VietnamTime.Now;

    public async Task<ThreePlContract> SaveContractAsync(ThreePlContractRequest request, int? scopedWarehouseId = null, CancellationToken ct = default)
    {
        EnsureWarehouseScope(request.WarehouseId, scopedWarehouseId);
        if (request.WarehouseId <= 0 || request.OwnerPartnerId <= 0)
            throw new BusinessRuleException("Hop dong 3PL can kho va chu hang.", "THREEPL_CONTRACT_SCOPE_REQUIRED", "ThreePlContract");
        if (request.EffectiveTo.HasValue && request.EffectiveTo.Value.Date < request.EffectiveFrom.Date)
            throw new BusinessRuleException("Ngay het hieu luc khong duoc truoc ngay hieu luc.", "THREEPL_CONTRACT_DATE_INVALID", "ThreePlContract");

        var ownerOk = await _db.Partners.AnyAsync(p => p.PartnerId == request.OwnerPartnerId && p.IsThreePlClient && p.IsActive, ct);
        if (!ownerOk)
            throw new BusinessRuleException("Chu hang 3PL khong hop le.", "THREEPL_OWNER_INVALID", "Partner");

        ThreePlContract contract;
        if (request.ContractId.HasValue)
        {
            contract = await _db.ThreePlContracts.FirstOrDefaultAsync(x => x.ThreePlContractId == request.ContractId.Value, ct)
                ?? throw new BusinessRuleException("Khong tim thay hop dong 3PL.", "THREEPL_CONTRACT_NOT_FOUND", "ThreePlContract");
            EnsureWarehouseScope(contract.WarehouseId, scopedWarehouseId);
            contract.UpdatedAt = Now;
            contract.UpdatedBy = CleanActor(request.Actor);
        }
        else
        {
            contract = new ThreePlContract
            {
                ContractCode = string.IsNullOrWhiteSpace(request.ContractCode)
                    ? await GenerateContractCodeAsync(request.WarehouseId, request.OwnerPartnerId, request.EffectiveFrom, ct)
                    : request.ContractCode.Trim().ToUpperInvariant(),
                CreatedAt = Now,
                CreatedBy = CleanActor(request.Actor)
            };
            _db.ThreePlContracts.Add(contract);
        }

        contract.WarehouseId = request.WarehouseId;
        contract.OwnerPartnerId = request.OwnerPartnerId;
        contract.ContractName = string.IsNullOrWhiteSpace(request.ContractName) ? contract.ContractCode : request.ContractName.Trim();
        contract.EffectiveFrom = request.EffectiveFrom == default ? Now.Date : request.EffectiveFrom.Date;
        contract.EffectiveTo = request.EffectiveTo?.Date;
        contract.Currency = NormalizeCurrency(request.Currency);
        contract.MinimumCharge = Math.Max(0, request.MinimumCharge);
        contract.TaxPercent = Math.Max(0, request.TaxPercent);
        contract.DiscountPercent = Math.Max(0, request.DiscountPercent);
        contract.RequiresAdjustmentApproval = request.RequiresAdjustmentApproval;
        contract.Status = ThreePlContractStatusEnum.Active;
        contract.Notes = Clean(request.Notes);

        await _unitOfWork.SaveChangesAsync(ct);
        return contract;
    }

    public async Task<ThreePlContractRate> SaveContractRateAsync(ThreePlContractRateRequest request, int? scopedWarehouseId = null, CancellationToken ct = default)
    {
        var contract = await _db.ThreePlContracts.FirstOrDefaultAsync(x => x.ThreePlContractId == request.ContractId, ct)
            ?? throw new BusinessRuleException("Khong tim thay hop dong 3PL.", "THREEPL_CONTRACT_NOT_FOUND", "ThreePlContract");
        EnsureWarehouseScope(contract.WarehouseId, scopedWarehouseId);
        if (request.UnitRate < 0 || request.TierFromQty < 0 || request.MinimumCharge < 0)
            throw new BusinessRuleException("Bang gia hop dong khong duoc am.", "THREEPL_CONTRACT_RATE_NEGATIVE", "ThreePlContractRate");
        if (request.TierToQty.HasValue && request.TierToQty.Value < request.TierFromQty)
            throw new BusinessRuleException("Bac ket thuc phai lon hon bac bat dau.", "THREEPL_TIER_INVALID", "ThreePlContractRate");

        ThreePlContractRate rate;
        if (request.ContractRateId.HasValue)
        {
            rate = await _db.ThreePlContractRates.FirstOrDefaultAsync(x => x.ThreePlContractRateId == request.ContractRateId.Value, ct)
                ?? throw new BusinessRuleException("Khong tim thay dong gia hop dong.", "THREEPL_CONTRACT_RATE_NOT_FOUND", "ThreePlContractRate");
        }
        else
        {
            rate = new ThreePlContractRate { CreatedAt = Now };
            _db.ThreePlContractRates.Add(rate);
        }

        rate.ThreePlContractId = request.ContractId;
        rate.ChargeType = request.ChargeType;
        rate.RateCode = string.IsNullOrWhiteSpace(request.RateCode)
            ? $"{request.ChargeType}-{request.ContractId}-{request.TierFromQty:0.####}"
            : request.RateCode.Trim().ToUpperInvariant();
        rate.ServiceCode = Clean(request.ServiceCode) ?? request.ChargeType.ToString();
        rate.ChargeUnit = string.IsNullOrWhiteSpace(request.ChargeUnit) ? "unit" : request.ChargeUnit.Trim();
        rate.UnitRate = request.UnitRate;
        rate.TierFromQty = request.TierFromQty;
        rate.TierToQty = request.TierToQty;
        rate.IncludedQty = Math.Max(0, request.IncludedQty);
        rate.MinimumCharge = request.MinimumCharge;
        rate.SurchargePercent = Math.Max(0, request.SurchargePercent);
        rate.OffHoursSurcharge = Math.Max(0, request.OffHoursSurcharge);
        rate.UrgentSurcharge = Math.Max(0, request.UrgentSurcharge);
        rate.HazmatSurcharge = Math.Max(0, request.HazmatSurcharge);
        rate.ColdStorageSurcharge = Math.Max(0, request.ColdStorageSurcharge);
        rate.ManualHandlingSurcharge = Math.Max(0, request.ManualHandlingSurcharge);
        rate.SlaPenaltyPercent = Math.Max(0, request.SlaPenaltyPercent);
        rate.SlaBonusPercent = Math.Max(0, request.SlaBonusPercent);
        rate.EffectiveFrom = request.EffectiveFrom == default ? contract.EffectiveFrom : request.EffectiveFrom.Date;
        rate.EffectiveTo = request.EffectiveTo?.Date;
        rate.IsActive = request.IsActive;
        rate.Notes = Clean(request.Notes);

        await _unitOfWork.SaveChangesAsync(ct);
        return rate;
    }

    public async Task<ThreePlRatingResult> RateAsync(ThreePlRatingRequest request, CancellationToken ct = default)
    {
        if (request.WarehouseId <= 0 || request.OwnerPartnerId <= 0)
            throw new BusinessRuleException("Can kho va chu hang de tinh gia 3PL.", "THREEPL_RATE_SCOPE_REQUIRED", "ThreePlContractRate");
        if (request.Quantity < 0)
            throw new BusinessRuleException("So luong tinh phi khong duoc am.", "THREEPL_RATE_QTY_NEGATIVE", "ThreePlContractRate");

        var serviceDate = request.ServiceDate == default ? Now.Date : request.ServiceDate.Date;
        var contract = await LoadActiveContractAsync(request.WarehouseId, request.OwnerPartnerId, serviceDate, ct);
        var rate = contract?.Rates
            .Where(r => r.IsActive
                && r.ChargeType == request.ChargeType
                && r.EffectiveFrom.Date <= serviceDate
                && (!r.EffectiveTo.HasValue || r.EffectiveTo.Value.Date >= serviceDate)
                && r.TierFromQty <= request.Quantity
                && (!r.TierToQty.HasValue || r.TierToQty.Value >= request.Quantity))
            .OrderByDescending(r => r.TierFromQty)
            .ThenByDescending(r => r.EffectiveFrom)
            .FirstOrDefault();

        if (contract == null || rate == null)
        {
            var fallback = await _db.ThreePlBillingRates.AsNoTracking()
                .Where(r => r.WarehouseId == request.WarehouseId
                    && r.OwnerPartnerId == request.OwnerPartnerId
                    && r.ChargeType == request.ChargeType
                    && r.IsActive
                    && r.EffectiveFrom.Date <= serviceDate
                    && (!r.EffectiveTo.HasValue || r.EffectiveTo.Value.Date >= serviceDate))
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefaultAsync(ct)
                ?? throw new BusinessRuleException("Chua co hop dong hoac bang gia 3PL hop le.", "THREEPL_RATE_NOT_CONFIGURED", "ThreePlContractRate");

            var subtotal = Math.Round(request.Quantity * fallback.UnitRate, 4, MidpointRounding.AwayFromZero);
            return new ThreePlRatingResult(null, null, request.Quantity, fallback.UnitRate, subtotal, 0m, 0m, 0m, subtotal, fallback.Currency, fallback.ChargeUnit);
        }

        var chargeableQty = Math.Max(0, request.Quantity - rate.IncludedQty);
        var subtotalAmount = Math.Round(chargeableQty * rate.UnitRate, 4, MidpointRounding.AwayFromZero);
        subtotalAmount = Math.Max(subtotalAmount, rate.MinimumCharge);
        subtotalAmount = Math.Max(subtotalAmount, contract.MinimumCharge);

        var fixedSurcharges = 0m;
        if (request.IsOffHours) fixedSurcharges += rate.OffHoursSurcharge;
        if (request.IsUrgent) fixedSurcharges += rate.UrgentSurcharge;
        if (request.IsHazmat) fixedSurcharges += rate.HazmatSurcharge;
        if (request.IsColdStorage) fixedSurcharges += rate.ColdStorageSurcharge;
        if (request.IsManualHandling) fixedSurcharges += rate.ManualHandlingSurcharge;

        var percentSurcharge = subtotalAmount * rate.SurchargePercent / 100m;
        var slaAdjustment = 0m;
        if (request.SlaBreached)
            slaAdjustment -= subtotalAmount * rate.SlaPenaltyPercent / 100m;
        if (request.SlaExceeded)
            slaAdjustment += subtotalAmount * rate.SlaBonusPercent / 100m;

        var taxableBase = subtotalAmount + fixedSurcharges + percentSurcharge + slaAdjustment;
        var discountAmount = Math.Round(Math.Max(0, taxableBase) * contract.DiscountPercent / 100m, 4, MidpointRounding.AwayFromZero);
        var taxAmount = Math.Round(Math.Max(0, taxableBase - discountAmount) * contract.TaxPercent / 100m, 4, MidpointRounding.AwayFromZero);
        var total = Math.Round(taxableBase - discountAmount + taxAmount, 4, MidpointRounding.AwayFromZero);

        return new ThreePlRatingResult(
            contract.ThreePlContractId,
            rate.ThreePlContractRateId,
            request.Quantity,
            rate.UnitRate,
            subtotalAmount,
            taxAmount,
            discountAmount,
            fixedSurcharges + percentSurcharge + slaAdjustment,
            total,
            contract.Currency,
            rate.ChargeUnit);
    }

    public async Task<ThreePlInvoice> GenerateInvoiceFromRunAsync(long runId, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        var run = await _db.ThreePlBillingRuns.Include(x => x.Charges).FirstOrDefaultAsync(x => x.ThreePlBillingRunId == runId, ct)
            ?? throw new BusinessRuleException("Khong tim thay ky tinh phi 3PL.", "THREEPL_RUN_NOT_FOUND", "ThreePlBillingRun");
        EnsureWarehouseScope(run.WarehouseId, scopedWarehouseId);

        var existing = await _db.ThreePlInvoices.Include(x => x.Lines).FirstOrDefaultAsync(x => x.ThreePlBillingRunId == runId && x.Status != ThreePlInvoiceStatusEnum.Voided, ct);
        if (existing != null)
            return existing;

        var contract = await LoadActiveContractAsync(run.WarehouseId, run.OwnerPartnerId, run.PeriodTo, ct);
        var invoice = new ThreePlInvoice
        {
            InvoiceCode = await GenerateInvoiceCodeAsync(run.WarehouseId, run.OwnerPartnerId, run.PeriodTo, ct),
            ThreePlBillingRunId = run.ThreePlBillingRunId,
            ThreePlContractId = contract?.ThreePlContractId,
            WarehouseId = run.WarehouseId,
            OwnerPartnerId = run.OwnerPartnerId,
            PeriodFrom = run.PeriodFrom,
            PeriodTo = run.PeriodTo,
            Status = ThreePlInvoiceStatusEnum.Draft,
            Currency = contract?.Currency ?? run.Currency,
            ApiPublicId = $"3pl-inv-{Guid.NewGuid():N}",
            CreatedBy = CleanActor(actor),
            CreatedAt = Now
        };

        foreach (var charge in run.Charges.Where(c => c.Status != ThreePlBillingChargeStatusEnum.Voided))
        {
            var rating = await RateAsync(new ThreePlRatingRequest
            {
                WarehouseId = charge.WarehouseId,
                OwnerPartnerId = charge.OwnerPartnerId,
                ChargeType = charge.ChargeType,
                Quantity = charge.Quantity,
                ServiceDate = charge.CreatedAt.Date,
                IsOffHours = IsOffHours(charge.CreatedAt),
                IsUrgent = charge.MetadataJson.Contains("urgent", StringComparison.OrdinalIgnoreCase),
                IsHazmat = charge.MetadataJson.Contains("hazmat", StringComparison.OrdinalIgnoreCase),
                IsColdStorage = charge.MetadataJson.Contains("cold", StringComparison.OrdinalIgnoreCase),
                IsManualHandling = charge.ChargeType is ThreePlChargeTypeEnum.Vas or ThreePlChargeTypeEnum.ManualAdjustment,
                SlaBreached = charge.MetadataJson.Contains("slaBreached", StringComparison.OrdinalIgnoreCase),
                SlaExceeded = charge.MetadataJson.Contains("slaExceeded", StringComparison.OrdinalIgnoreCase)
            }, ct);

            invoice.Lines.Add(new ThreePlInvoiceLine
            {
                ThreePlBillingChargeId = charge.ThreePlBillingChargeId,
                ChargeType = charge.ChargeType,
                LineType = "Charge",
                Description = $"{charge.ChargeType} - {charge.SourceCode ?? charge.SourceId ?? charge.SourceType}",
                Quantity = rating.Quantity,
                ChargeUnit = rating.ChargeUnit,
                UnitRate = rating.UnitRate,
                SubtotalAmount = rating.SubtotalAmount,
                TaxAmount = rating.TaxAmount,
                DiscountAmount = rating.DiscountAmount,
                AdjustmentAmount = rating.AdjustmentAmount,
                TotalAmount = rating.TotalAmount,
                Currency = rating.Currency
            });
        }

        Recalculate(invoice);
        _db.ThreePlInvoices.Add(invoice);
        await _unitOfWork.SaveChangesAsync(ct);
        return invoice;
    }

    public async Task<ThreePlInvoice> ConfirmInvoiceAsync(long invoiceId, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        var invoice = await _db.ThreePlInvoices.Include(x => x.Lines).FirstOrDefaultAsync(x => x.ThreePlInvoiceId == invoiceId, ct)
            ?? throw new BusinessRuleException("Khong tim thay hoa don 3PL.", "THREEPL_INVOICE_NOT_FOUND", "ThreePlInvoice");
        EnsureWarehouseScope(invoice.WarehouseId, scopedWarehouseId);
        if (invoice.Status == ThreePlInvoiceStatusEnum.Voided)
            throw new BusinessRuleException("Khong the xac nhan hoa don da huy.", "THREEPL_INVOICE_VOIDED", "ThreePlInvoice");

        invoice.Status = ThreePlInvoiceStatusEnum.Locked;
        invoice.ConfirmedAt ??= Now;
        invoice.ConfirmedBy = CleanActor(actor);
        invoice.LockedAt ??= Now;
        invoice.LockedBy = CleanActor(actor);
        if (invoice.ThreePlBillingRunId.HasValue)
        {
            var run = await _db.ThreePlBillingRuns.Include(x => x.Charges).FirstOrDefaultAsync(x => x.ThreePlBillingRunId == invoice.ThreePlBillingRunId.Value, ct);
            if (run != null && run.Status == ThreePlBillingRunStatusEnum.Draft)
            {
                run.Status = ThreePlBillingRunStatusEnum.Confirmed;
                run.ConfirmedAt = Now;
                run.ConfirmedBy = CleanActor(actor);
                foreach (var charge in run.Charges.Where(c => c.Status == ThreePlBillingChargeStatusEnum.Draft))
                    charge.Status = ThreePlBillingChargeStatusEnum.Confirmed;
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return invoice;
    }

    public async Task<ThreePlDispute> CreateDisputeAsync(long invoiceLineId, decimal requestedAmount, string reason, string actor, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new BusinessRuleException("Can ly do khieu nai dong phi.", "THREEPL_DISPUTE_REASON_REQUIRED", "ThreePlDispute");
        var line = await _db.ThreePlInvoiceLines.Include(x => x.Invoice).FirstOrDefaultAsync(x => x.ThreePlInvoiceLineId == invoiceLineId, ct)
            ?? throw new BusinessRuleException("Khong tim thay dong hoa don 3PL.", "THREEPL_INVOICE_LINE_NOT_FOUND", "ThreePlInvoiceLine");

        var dispute = new ThreePlDispute
        {
            ThreePlInvoiceLineId = invoiceLineId,
            OwnerPartnerId = line.Invoice.OwnerPartnerId,
            Status = ThreePlDisputeStatusEnum.Open,
            RequestedAmount = Math.Max(0, requestedAmount),
            Reason = reason.Trim(),
            OpenedBy = CleanActor(actor),
            OpenedAt = Now
        };
        _db.ThreePlDisputes.Add(dispute);
        await _unitOfWork.SaveChangesAsync(ct);
        return dispute;
    }

    public async Task<ThreePlDispute> ResolveDisputeAsync(long disputeId, bool approve, decimal approvedAmount, string response, int? scopedWarehouseId, string actor, CancellationToken ct = default)
    {
        var dispute = await _db.ThreePlDisputes
            .FirstOrDefaultAsync(x => x.ThreePlDisputeId == disputeId, ct)
            ?? throw new BusinessRuleException("Khong tim thay khieu nai 3PL.", "THREEPL_DISPUTE_NOT_FOUND", "ThreePlDispute");
        var line = await _db.ThreePlInvoiceLines
            .Include(x => x.Invoice)
            .FirstOrDefaultAsync(x => x.ThreePlInvoiceLineId == dispute.ThreePlInvoiceLineId, ct)
            ?? throw new BusinessRuleException("Khong tim thay dong hoa don 3PL.", "THREEPL_INVOICE_LINE_NOT_FOUND", "ThreePlInvoiceLine");
        dispute.InvoiceLine = line;
        EnsureWarehouseScope(line.Invoice.WarehouseId, scopedWarehouseId);

        dispute.Status = approve ? ThreePlDisputeStatusEnum.Approved : ThreePlDisputeStatusEnum.Rejected;
        dispute.ApprovedAmount = approve ? Math.Max(0, approvedAmount) : 0m;
        dispute.StaffResponse = Clean(response);
        dispute.ResolutionNotes = Clean(response);
        dispute.ResolvedAt = Now;
        dispute.ResolvedBy = CleanActor(actor);
        dispute.ManagerApprovedAt = Now;
        dispute.ManagerApprovedBy = CleanActor(actor);

        if (approve && dispute.ApprovedAmount > 0)
        {
            line.AdjustmentAmount -= dispute.ApprovedAmount;
            line.TotalAmount = line.SubtotalAmount
                + line.TaxAmount
                - line.DiscountAmount
                + line.AdjustmentAmount;
            line.AdjustmentReason = "Dispute approved: " + dispute.Reason;
            line.ApprovedAt = Now;
            line.ApprovedBy = CleanActor(actor);
            _db.Entry(line).Property(x => x.AdjustmentAmount).IsModified = true;
            _db.Entry(line).Property(x => x.TotalAmount).IsModified = true;
            _db.Entry(line).Property(x => x.AdjustmentReason).IsModified = true;
            _db.Entry(line).Property(x => x.ApprovedAt).IsModified = true;
            _db.Entry(line).Property(x => x.ApprovedBy).IsModified = true;
            Recalculate(line.Invoice);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        dispute.InvoiceLine = line;
        return dispute;
    }

    private async Task<ThreePlContract?> LoadActiveContractAsync(int warehouseId, int ownerPartnerId, DateTime serviceDate, CancellationToken ct)
    {
        return await _db.ThreePlContracts
            .Include(c => c.Rates)
            .Where(c => c.WarehouseId == warehouseId
                && c.OwnerPartnerId == ownerPartnerId
                && c.Status == ThreePlContractStatusEnum.Active
                && c.EffectiveFrom.Date <= serviceDate.Date
                && (!c.EffectiveTo.HasValue || c.EffectiveTo.Value.Date >= serviceDate.Date))
            .OrderByDescending(c => c.EffectiveFrom)
            .ThenByDescending(c => c.ThreePlContractId)
            .FirstOrDefaultAsync(ct);
    }

    private static void Recalculate(ThreePlInvoice invoice)
    {
        invoice.SubtotalAmount = invoice.Lines.Sum(x => x.SubtotalAmount);
        invoice.TaxAmount = invoice.Lines.Sum(x => x.TaxAmount);
        invoice.DiscountAmount = invoice.Lines.Sum(x => x.DiscountAmount);
        invoice.AdjustmentAmount = invoice.Lines.Sum(x => x.AdjustmentAmount);
        invoice.TotalAmount = invoice.Lines.Sum(x => x.TotalAmount);
    }

    private async Task<string> GenerateContractCodeAsync(int warehouseId, int ownerPartnerId, DateTime effectiveFrom, CancellationToken ct)
    {
        var prefix = $"3PLC-{effectiveFrom:yyyyMM}-{warehouseId}-{ownerPartnerId}-";
        var count = await _db.ThreePlContracts.CountAsync(x => x.ContractCode.StartsWith(prefix), ct);
        return $"{prefix}{count + 1:0000}";
    }

    private async Task<string> GenerateInvoiceCodeAsync(int warehouseId, int ownerPartnerId, DateTime periodTo, CancellationToken ct)
    {
        var prefix = $"3PLI-{periodTo:yyyyMM}-{warehouseId}-{ownerPartnerId}-";
        var count = await _db.ThreePlInvoices.CountAsync(x => x.InvoiceCode.StartsWith(prefix), ct);
        return $"{prefix}{count + 1:0000}";
    }

    private static bool IsOffHours(DateTime value)
        => value.Hour < 8 || value.Hour >= 18 || value.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    private static void EnsureWarehouseScope(int warehouseId, int? scopedWarehouseId)
    {
        if (scopedWarehouseId.HasValue && warehouseId != scopedWarehouseId.Value)
            throw new UnauthorizedAccessException("Khong duoc thao tac du lieu 3PL cua kho khac.");
    }

    private static string NormalizeCurrency(string? value)
        => string.IsNullOrWhiteSpace(value) ? "VND" : value.Trim().ToUpperInvariant()[..Math.Min(value.Trim().Length, 10)];

    private static string CleanActor(string? actor)
        => string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim()[..Math.Min(actor.Trim().Length, 100)];

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
