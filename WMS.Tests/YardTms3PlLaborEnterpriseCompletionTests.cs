using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WMS.Common;
using WMS.Data;
using WMS.Models;
using WMS.Services;
using Xunit;

namespace WMS.Tests;

public sealed class YardTms3PlLaborEnterpriseCompletionTests
{
    [Fact]
    public async Task DockAppointment_ShouldSuggestDoorAndRejectOverlappingWindow()
    {
        await using var db = CreateDb();
        SeedWarehouseAndOwner(db);
        db.DockDoorCapacities.AddRange(
            new DockDoorCapacity
            {
                WarehouseId = 1,
                DockDoor = "DOCK-R1",
                DoorType = DockDoorTypeEnum.Receiving,
                IsRefrigerated = true,
                SlotStartMinutes = 0,
                SlotEndMinutes = 1440,
                MaxAppointments = 1,
                AvgUnloadMinutes = 45
            },
            new DockDoorCapacity
            {
                WarehouseId = 1,
                DockDoor = "DOCK-S1",
                DoorType = DockDoorTypeEnum.Shipping,
                SlotStartMinutes = 0,
                SlotEndMinutes = 1440,
                MaxAppointments = 2,
                AvgUnloadMinutes = 60
            });
        await db.SaveChangesAsync();

        var service = new DockAppointmentService(db);
        var start = VietnamTime.Now.Date.AddHours(9);
        var request = new DockAppointmentRequest
        {
            WarehouseId = 1,
            OwnerPartnerId = 10,
            Direction = DockAppointmentDirectionEnum.Inbound,
            PlannedStartAt = start,
            PlannedEndAt = start.AddHours(1),
            IsRefrigerated = true,
            GoodsType = "Cold",
            Actor = "yard-manager"
        };

        var suggestion = await service.SuggestDoorAsync(request);
        Assert.Equal("DOCK-R1", suggestion.DockDoor);

        var appointment = await service.CreateAsync(request);
        Assert.Equal(DockAppointmentStatusEnum.Scheduled, appointment.Status);
        Assert.Equal("DOCK-R1", appointment.DockDoor);

        var conflict = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(request));
        Assert.Equal("DOCK_APPOINTMENT_CONFLICT", conflict.Code);

        var checkedIn = await service.CheckInAsync(appointment.DockAppointmentId, null, "gate");
        Assert.Equal(DockAppointmentStatusEnum.CheckedIn, checkedIn.Status);
        Assert.NotNull(checkedIn.CheckInAt);

        var completed = await service.CheckOutAsync(appointment.DockAppointmentId, null, "gate");
        Assert.Equal(DockAppointmentStatusEnum.Completed, completed.Status);
        Assert.NotNull(completed.CheckOutAt);
    }

    [Fact]
    public async Task ThreePlBilling_ShouldRateInvoiceLockAndResolveDispute()
    {
        await using var db = CreateDb();
        SeedWarehouseAndOwner(db);
        var service = new ThreePlEnterpriseBillingService(db);

        var contract = await service.SaveContractAsync(new ThreePlContractRequest
        {
            WarehouseId = 1,
            OwnerPartnerId = 10,
            ContractCode = "3PLC-TEST",
            ContractName = "Enterprise 3PL contract",
            EffectiveFrom = VietnamTime.Now.Date.AddDays(-1),
            Currency = "VND",
            MinimumCharge = 1000m,
            TaxPercent = 10m,
            DiscountPercent = 5m,
            Actor = "billing"
        });

        await service.SaveContractRateAsync(new ThreePlContractRateRequest
        {
            ContractId = contract.ThreePlContractId,
            ChargeType = ThreePlChargeTypeEnum.Storage,
            RateCode = "STORAGE-TIER",
            ChargeUnit = "pallet-day",
            UnitRate = 100m,
            TierFromQty = 0m,
            TierToQty = 100m,
            IncludedQty = 2m,
            MinimumCharge = 500m,
            SurchargePercent = 10m,
            OffHoursSurcharge = 50m,
            UrgentSurcharge = 70m,
            SlaPenaltyPercent = 5m,
            EffectiveFrom = VietnamTime.Now.Date.AddDays(-1)
        });

        var rating = await service.RateAsync(new ThreePlRatingRequest
        {
            WarehouseId = 1,
            OwnerPartnerId = 10,
            ChargeType = ThreePlChargeTypeEnum.Storage,
            Quantity = 12m,
            ServiceDate = VietnamTime.Now.Date,
            IsOffHours = true,
            IsUrgent = true,
            SlaBreached = true
        });

        Assert.Equal(1000m, rating.SubtotalAmount);
        Assert.Equal(170m, rating.AdjustmentAmount);
        Assert.Equal(58.5m, rating.DiscountAmount);
        Assert.Equal(111.15m, rating.TaxAmount);
        Assert.Equal(1222.65m, rating.TotalAmount);

        var run = new ThreePlBillingRun
        {
            WarehouseId = 1,
            OwnerPartnerId = 10,
            RunCode = "RUN-TEST",
            PeriodFrom = VietnamTime.Now.Date,
            PeriodTo = VietnamTime.Now.Date,
            Currency = "VND",
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            CreatedBy = "billing"
        };
        run.Charges.Add(new ThreePlBillingCharge
        {
            WarehouseId = 1,
            OwnerPartnerId = 10,
            ChargeType = ThreePlChargeTypeEnum.Storage,
            SourceType = "Inventory",
            SourceId = "INV-1",
            SourceCode = "INV-1",
            Quantity = 5m,
            UnitRate = 100m,
            Amount = 500m,
            Currency = "VND",
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            MetadataJson = "{}",
            CreatedBy = "billing",
            CreatedAt = VietnamTime.Now.Date.AddHours(10)
        });
        db.ThreePlBillingRuns.Add(run);
        await db.SaveChangesAsync();

        var invoice = await service.GenerateInvoiceFromRunAsync(run.ThreePlBillingRunId, null, "billing");
        Assert.Single(invoice.Lines);
        Assert.Equal(ThreePlInvoiceStatusEnum.Draft, invoice.Status);

        var locked = await service.ConfirmInvoiceAsync(invoice.ThreePlInvoiceId, null, "manager");
        Assert.Equal(ThreePlInvoiceStatusEnum.Locked, locked.Status);
        Assert.NotNull(locked.LockedAt);

        var invoiceLine = locked.Lines.Single();
        var lineId = invoiceLine.ThreePlInvoiceLineId;
        var adjustmentBeforeDispute = invoiceLine.AdjustmentAmount;
        var dispute = await service.CreateDisputeAsync(lineId, 100m, "Rate mismatch", "owner");
        Assert.Equal(ThreePlDisputeStatusEnum.Open, dispute.Status);
        Assert.Equal(10, dispute.OwnerPartnerId);

        var resolved = await service.ResolveDisputeAsync(dispute.ThreePlDisputeId, approve: true, approvedAmount: 100m, response: "Approved credit", scopedWarehouseId: 1, actor: "manager");
        Assert.Equal(ThreePlDisputeStatusEnum.Approved, resolved.Status);
        Assert.Equal(100m, resolved.ApprovedAmount);
        var adjustedLine = await db.ThreePlInvoiceLines.AsNoTracking().SingleAsync(x => x.ThreePlInvoiceLineId == lineId);
        Assert.Equal(adjustmentBeforeDispute - 100m, adjustedLine.AdjustmentAmount);
    }

    [Fact]
    public async Task LaborManagement_ShouldCaptureExceptionAndManagerApproval()
    {
        await using var db = CreateDb();
        SeedWarehouseAndOwner(db);
        var service = new LaborManagementService(db);

        var standard = await service.SaveStandardAsync(new LaborStandardRequest
        {
            WarehouseId = 1,
            TaskType = "Pick",
            TaskTypeName = "Picking",
            UnitOfWork = "line",
            ExpectedMinutesPerUnit = 10m,
            MinPerformancePercent = 80m,
            ExcellentPerformancePercent = 120m,
            EffectiveFrom = VietnamTime.Now.Date.AddDays(-1)
        });
        Assert.True(standard.LaborStandardId > 0);

        var activity = await service.StartActivityAsync(new LaborActivityRequest
        {
            WarehouseId = 1,
            OwnerPartnerId = 10,
            UserName = "staff01",
            ShiftCode = "DAY",
            TaskType = "Pick",
            TaskSourceType = "PickTask",
            TaskSourceId = "PICK-1",
            WorkQuantity = 1m,
            UnitOfWork = "line",
            StartedAt = VietnamTime.Now.AddMinutes(-100),
            WaitingMinutes = 45,
            BacklogAtStart = 12,
            Actor = "staff01"
        });

        var completed = await service.CompleteActivityAsync(activity.LaborActivityId, 1m, null, 1, "staff01");
        Assert.Equal(LaborActivityStatusEnum.Exception, completed.Status);
        Assert.True(completed.IsException);
        Assert.True(completed.ProductivityPercent < 80m);

        var review = await db.LaborExceptionReviews.SingleAsync();
        var approved = await service.ApproveExceptionAsync(review.LaborExceptionReviewId, approve: true, productivityAfter: 95m, incentiveAmount: 25000m, notes: "Traffic at dock", scopedWarehouseId: 1, actor: "manager");

        Assert.Equal(LaborExceptionStatusEnum.Approved, approved.Status);
        Assert.Equal(95m, approved.ProductivityAfter);
        Assert.Equal(25000m, approved.IncentiveAmount);
        Assert.Equal(LaborActivityStatusEnum.Completed, approved.LaborActivity.Status);
    }

    [Fact]
    public void Enterprise567StaticArtifacts_ShouldExposeUiExportsMigrationAndChecklist()
    {
        var root = FindRepositoryRoot();
        var controller = Read(Path.Combine(root, "Controllers", "OperationsController.Enterprise567.cs"));
        var dockBoard = Read(Path.Combine(root, "Views", "Operations", "DockBoard.cshtml"));
        var yard = Read(Path.Combine(root, "Views", "Operations", "YardManagement.cshtml"));
        var contracts = Read(Path.Combine(root, "Views", "Operations", "ThreePlContracts.cshtml"));
        var invoice = Read(Path.Combine(root, "Views", "Operations", "ThreePlInvoiceDetails.cshtml"));
        var portal = Read(Path.Combine(root, "Views", "Operations", "ThreePlClientPortal.cshtml"));
        var labor = Read(Path.Combine(root, "Views", "Operations", "LaborProductivity.cshtml"));
        var css = Read(Path.Combine(root, "wwwroot", "css", "site.css"));
        var tasks = Read(Path.Combine(root, "ENTERPRISE_WMS_100_PERCENT_TASKS.md"));

        Assert.Contains("CreateDockAppointment", controller, StringComparison.Ordinal);
        Assert.Contains("UploadYardVisitEvidence", controller, StringComparison.Ordinal);
        Assert.Contains("GenerateThreePlInvoice", controller, StringComparison.Ordinal);
        Assert.Contains("ResolveThreePlDispute", controller, StringComparison.Ordinal);
        Assert.Contains("ExportLaborProductivity", controller, StringComparison.Ordinal);
        Assert.True(controller.Split("[HttpPost]").Length - 1 <= controller.Split("[ValidateAntiForgeryToken]").Length - 1,
            "Every POST in the enterprise 5-7 controller must carry anti-forgery.");

        Assert.Contains("ExportDockAppointments", dockBoard, StringComparison.Ordinal);
        Assert.Contains("yardops-evidence-form", yard, StringComparison.Ordinal);
        Assert.Contains("SaveThreePlContract", contracts, StringComparison.Ordinal);
        Assert.Contains("ExportThreePlInvoiceExcel", invoice, StringComparison.Ordinal);
        Assert.Contains("ExportThreePlInvoicePdf", invoice, StringComparison.Ordinal);
        Assert.Contains("OwnerPartnerId", portal, StringComparison.Ordinal);
        Assert.Contains("ExportLaborProductivity", labor, StringComparison.Ordinal);
        Assert.Contains(".yardops-inline-card", css, StringComparison.Ordinal);
        Assert.Contains("CompleteYard3PlLaborEnterprise", string.Join("\n", Directory.GetFiles(Path.Combine(root, "Migrations")).Select(Path.GetFileName)), StringComparison.Ordinal);

        foreach (var code in new[] { "YARD-01", "YARD-02", "YARD-03", "YARD-04", "CAR-01", "CAR-02", "3PL-01", "3PL-02", "3PL-03", "3PL-04", "3PL-05", "3PL-06", "3PL-07", "3PL-08", "LAB-01", "LAB-02", "LAB-03", "LAB-04", "LAB-05" })
        {
            Assert.Contains($"- [x] `{code}`", tasks, StringComparison.Ordinal);
        }
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static void SeedWarehouseAndOwner(AppDbContext db)
    {
        db.Warehouses.Add(new Warehouse { WarehouseId = 1, WarehouseCode = "WH01", WarehouseName = "Main warehouse" });
        db.Partners.Add(new Partner
        {
            PartnerId = 10,
            PartnerCode = "OWN01",
            PartnerName = "Owner 01",
            PartnerType = PartnerTypeEnum.Customer,
            IsThreePlClient = true,
            IsActive = true
        });
        db.SaveChanges();
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "WMS.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate WMS.sln from test output directory.");
    }

    private static string Read(string path)
        => File.ReadAllText(path);
}
