using System.Text;
using System.Text.RegularExpressions;

namespace WMS.Tests;

public sealed class EnterpriseUiUxPolishTests
{
    [Fact]
    public void Layout_ShouldLoadGlobalEnterpriseUiEnhancer()
    {
        var root = FindRepositoryRoot();
        var layout = Read(Path.Combine(root, "Views", "Shared", "_Layout.cshtml"));
        var siteJs = Read(Path.Combine(root, "wwwroot", "js", "site.js"));

        Assert.Contains("~/js/site.js", layout, StringComparison.Ordinal);
        Assert.Contains("enhanceTables", siteJs, StringComparison.Ordinal);
        Assert.Contains("enhanceForms", siteJs, StringComparison.Ordinal);
        Assert.Contains("enhanceStatusBadges", siteJs, StringComparison.Ordinal);
        Assert.Contains("enterprise-table-wrap", siteJs, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadingFeedback_ShouldUseDelayedScopedEnterpriseHelper()
    {
        var root = FindRepositoryRoot();
        var siteJs = Read(Path.Combine(root, "wwwroot", "js", "site.js"));
        var css = Read(Path.Combine(root, "wwwroot", "css", "site.css"));
        var voucherCreate = Read(Path.Combine(root, "Views", "Vouchers", "Create.cshtml"));
        var itemCreate = Read(Path.Combine(root, "Views", "Items", "Create.cshtml"));
        var dockBoard = Read(Path.Combine(root, "Views", "Operations", "DockBoard.cshtml"));
        var slotting = Read(Path.Combine(root, "Views", "Operations", "Slotting.cshtml"));
        var lpnLookup = Read(Path.Combine(root, "Views", "Operations", "LpnLookup.cshtml"));
        var serialLookup = Read(Path.Combine(root, "Views", "Operations", "SerialLookup.cshtml"));

        foreach (var token in new[]
        {
            "window.wmsLoading",
            "begin: beginLoading",
            "end: endLoading",
            "withBusy: withBusy",
            "setTimeout",
            "aria-busy",
            "aria-live",
            "event.defaultPrevented"
        })
        {
            Assert.Contains(token, siteJs, StringComparison.Ordinal);
        }

        foreach (var token in new[]
        {
            ".wms-loading-overlay",
            ".wms-loading-spinner",
            ".wms-loading-progress",
            ".wms-loading-skeleton-card",
            ".wms-loading-field",
            "@media (prefers-reduced-motion: reduce)"
        })
        {
            Assert.Contains(token, css, StringComparison.Ordinal);
        }

        foreach (var view in new[] { voucherCreate, itemCreate, dockBoard, slotting, lpnLookup, serialLookup })
        {
            Assert.Contains("window.wmsLoading?.begin", view, StringComparison.Ordinal);
            Assert.Contains("window.wmsLoading?.end", view, StringComparison.Ordinal);
        }

        Assert.Contains("finally", voucherCreate, StringComparison.Ordinal);
        Assert.Contains("ImportLinesExcel", voucherCreate, StringComparison.Ordinal);
        Assert.Contains("AnalyzeReceipt", voucherCreate, StringComparison.Ordinal);
        Assert.Contains("SuggestPutaway", voucherCreate, StringComparison.Ordinal);
        Assert.Contains("GetItemByBarcode", voucherCreate, StringComparison.Ordinal);
        Assert.DoesNotContain("fa-robot\" " + "style" + "=", voucherCreate, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ErrorHandling_ShouldNotExposeGenericExceptionMessagesInUiOrQueuedResponses()
    {
        var root = FindRepositoryRoot();
        var helper = Read(Path.Combine(root, "Common", "UserSafeError.cs"));
        var program = Read(Path.Combine(root, "Program.cs"));
        var verificationScript = Read(Path.Combine(root, "scripts", "Run-WmsVerification.ps1"));
        var drScript = Read(Path.Combine(root, "scripts", "Invoke-WmsDrEvidence.ps1"));
        var controllerFiles = Directory.EnumerateFiles(Path.Combine(root, "Controllers"), "*.cs", SearchOption.AllDirectories)
            .ToList();

        foreach (var token in new[]
        {
            "GenericMessage",
            "BusinessRuleException",
            "WarehouseLockedException",
            "DbUpdateException",
            "IOException",
            "IsBusinessSafe"
        })
        {
            Assert.Contains(token, helper, StringComparison.Ordinal);
        }

        var unsafePatterns = new[]
        {
            "TempData[\"Error\"] = ex.Message",
            "base.TempData[\"Error\"] = ex.Message",
            "return BadRequest(ex.Message)",
            "QueuedOperationResponse.Json(this, false, ex.Message",
            "line.ErrorMessage = ex.Message"
        };

        var failures = controllerFiles
            .Select(path => new { Path = path, Content = Read(path) })
            .SelectMany(file => unsafePatterns
                .Where(pattern => file.Content.Contains(pattern, StringComparison.Ordinal))
                .Select(pattern => $"{Path.GetRelativePath(root, file.Path)} leaks `{pattern}`"))
            .ToList();

        Assert.True(failures.Count == 0, "Unsafe exception-message exposure found:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
        Assert.Contains("detail = UserSafeError.From(exception", program, StringComparison.Ordinal);
        Assert.DoesNotContain("detail = app.Environment.IsDevelopment() ? exception.Message", program, StringComparison.Ordinal);
        Assert.Contains("Get-SafeExceptionCode", verificationScript, StringComparison.Ordinal);
        Assert.Contains("Get-SafeExceptionCode", drScript, StringComparison.Ordinal);
        Assert.DoesNotContain("$($_.Exception.Message)", verificationScript, StringComparison.Ordinal);
        Assert.DoesNotContain("$($_.Exception.Message)", drScript, StringComparison.Ordinal);
    }

    [Fact]
    public void ThreePlInvoiceDisputeActions_ShouldRequireConfirmAndResolutionText()
    {
        var root = FindRepositoryRoot();
        var invoiceDetails = Read(Path.Combine(root, "Views", "Operations", "ThreePlInvoiceDetails.cshtml"));

        Assert.Contains("asp-action=\"ConfirmThreePlInvoice\"", invoiceDetails, StringComparison.Ordinal);
        Assert.Contains("data-enterprise-confirm=\"Xác nhận và khóa hóa đơn 3PL này?", invoiceDetails, StringComparison.Ordinal);
        Assert.Contains("asp-action=\"ResolveThreePlDispute\"", invoiceDetails, StringComparison.Ordinal);
        Assert.Contains("data-enterprise-confirm=\"Ghi nhận kết quả xử lý khiếu nại phí này?", invoiceDetails, StringComparison.Ordinal);
        Assert.Contains("name=\"response\" class=\"form-control\" placeholder=\"Phản hồi xử lý\" aria-label=\"Phản hồi xử lý khiếu nại\" required", invoiceDetails, StringComparison.Ordinal);
    }

    [Fact]
    public void TempDataNotifications_ShouldUseEnterpriseToastHelperAndVisualGate()
    {
        var root = FindRepositoryRoot();
        var layout = Read(Path.Combine(root, "Views", "Shared", "_Layout.cshtml"));
        var css = Read(Path.Combine(root, "wwwroot", "css", "site.css"));
        var visualSpec = Read(Path.Combine(root, "tests", "visual", "wms-visual-regression.spec.ts"));

        Assert.Contains("TempData[\"Success\"]", layout, StringComparison.Ordinal);
        Assert.Contains("TempData[\"Error\"]", layout, StringComparison.Ordinal);
        Assert.Contains("window.enterpriseNotify({ title: raw, icon: 'success'", layout, StringComparison.Ordinal);
        Assert.Contains("window.enterpriseNotify({ title: raw, icon: 'error'", layout, StringComparison.Ordinal);
        Assert.Contains("enterprise-toast-popup", layout, StringComparison.Ordinal);
        Assert.Contains(".swal2-container .enterprise-toast-popup", css, StringComparison.Ordinal);
        Assert.Contains("enterprise toast is not covered by fixed topbar", visualSpec, StringComparison.Ordinal);
        Assert.Contains("popupBox?.y", visualSpec, StringComparison.Ordinal);
        Assert.Contains(".app-topbar", visualSpec, StringComparison.Ordinal);
    }

    [Fact]
    public void DynamicLookupHtml_ShouldEscapeRuntimeValuesBeforeRendering()
    {
        var root = FindRepositoryRoot();
        var lpn = Read(Path.Combine(root, "Views", "Operations", "LpnLookup.cshtml"));
        var serial = Read(Path.Combine(root, "Views", "Operations", "SerialLookup.cshtml"));
        var slotting = Read(Path.Combine(root, "Views", "Operations", "Slotting.cshtml"));

        foreach (var view in new[] { lpn, serial })
        {
            Assert.Contains("function escapeLookupText", view, StringComparison.Ordinal);
            Assert.Contains("escapeLookupText(info.itemCode)", view, StringComparison.Ordinal);
            Assert.Contains("escapeLookupText(info.itemName)", view, StringComparison.Ordinal);
            Assert.DoesNotContain("${info.itemName}", view, StringComparison.Ordinal);
            Assert.DoesNotContain("style" + "=\"color:#10b981", view, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("function escapeSlottingText", slotting, StringComparison.Ordinal);
        foreach (var token in new[]
        {
            "escapeSlottingText(item.itemCode)",
            "escapeSlottingText(item.itemName)",
            "escapeSlottingText(item.xyzClass)",
            "escapeSlottingText(item.currentLocation)",
            "Number(item.dailyFrequency || 0)",
            "Number(item.score || 0)"
        })
        {
            Assert.Contains(token, slotting, StringComparison.Ordinal);
        }

        foreach (var unsafeConcat in new[] { "+ item.itemCode +", "+ item.itemName +", "+ item.currentLocation +" })
        {
            Assert.DoesNotContain(unsafeConcat, slotting, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Css_ShouldProvideEnterpriseRetrofitForLegacyBootstrapLikeViews()
    {
        var root = FindRepositoryRoot();
        var css = Read(Path.Combine(root, "wwwroot", "css", "site.css"));

        foreach (var token in new[]
        {
            "Enterprise UI retrofit layer",
            ".row",
            ".col-md-6",
            ".form-select",
            ".table-responsive",
            ".btn-outline-danger",
            ".modal-backdrop",
            ".status-badge.success",
            ".metric-grid"
        })
        {
            Assert.Contains(token, css, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void FormerRoughEnterpriseScreens_ShouldUsePageHeaderSectionsAndDataTables()
    {
        var root = FindRepositoryRoot();
        var files = new[]
        {
            Path.Combine(root, "Views", "Operations", "MheDashboard.cshtml"),
            Path.Combine(root, "Views", "Operations", "TenantOwnerScopes.cshtml"),
            Path.Combine(root, "Views", "Operations", "ThreePlBillingRunDetails.cshtml"),
            Path.Combine(root, "Views", "Reports", "InventoryTransactions.cshtml")
        };

        foreach (var file in files)
        {
            var content = Read(file);
            Assert.Contains("page-title", content, StringComparison.Ordinal);
            Assert.Contains("page-subtitle", content, StringComparison.Ordinal);
            Assert.Contains("data-table", content, StringComparison.Ordinal);
            Assert.DoesNotContain("class=\"table table-sm", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("class=\"row g-", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("form-select", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void PolishPriorityViews_ShouldUseSharedClassesInsteadOfStaticInlineStyles()
    {
        var root = FindRepositoryRoot();
        var priorityViews = new[]
        {
            Path.Combine(root, "Views", "Items", "Create.cshtml"),
            Path.Combine(root, "Views", "Items", "Details.cshtml"),
            Path.Combine(root, "Views", "Items", "Index.cshtml"),
            Path.Combine(root, "Views", "Items", "PrintSetup.cshtml"),
            Path.Combine(root, "Views", "Vouchers", "Index.cshtml"),
            Path.Combine(root, "Views", "Reports", "ScheduledReports.cshtml"),
            Path.Combine(root, "Views", "Reports", "OpsKpi.cshtml"),
            Path.Combine(root, "Views", "Reports", "ExpiryReport.cshtml"),
            Path.Combine(root, "Views", "Operations", "ExceptionCenter.cshtml"),
            Path.Combine(root, "Views", "Operations", "Shipping.cshtml"),
            Path.Combine(root, "Views", "Operations", "ShippingDispatch.cshtml"),
            Path.Combine(root, "Views", "Operations", "QualityInspection.cshtml"),
            Path.Combine(root, "Views", "Operations", "PackageLookup.cshtml"),
            Path.Combine(root, "Views", "Operations", "ShipmentLoadDetails.cshtml"),
            Path.Combine(root, "Views", "Operations", "ShipmentLoads.cshtml")
        };

        var failures = priorityViews
            .Where(path => Read(path).Contains("style" + "=", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(root, path))
            .ToList();

        Assert.True(failures.Count == 0, "Priority views con static inline style: " + string.Join(", ", failures));
    }

    [Fact]
    public void QualityInspection_ShouldUseDisplayHelpersAndSafeInternalLinks()
    {
        var root = FindRepositoryRoot();
        var view = Read(Path.Combine(root, "Views", "Operations", "QualityInspection.cshtml"));

        Assert.Contains("StatusDisplay", view, StringComparison.Ordinal);
        Assert.Contains("StatusBadgeClass", view, StringComparison.Ordinal);
        Assert.Contains("VoucherTypeName", view, StringComparison.Ordinal);
        Assert.Contains("asp-controller=\"Vouchers\"", view, StringComparison.Ordinal);
        Assert.Contains("asp-action=\"Details\"", view, StringComparison.Ordinal);
        Assert.DoesNotContain("InboundStatus.ToString", view, StringComparison.Ordinal);
        Assert.DoesNotContain("VoucherType.ToString", view, StringComparison.Ordinal);
        Assert.DoesNotContain("style" + "=", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("href=\"/Reports/Analytics\"", view, StringComparison.Ordinal);
    }

    [Fact]
    public void ScheduledReports_PostForms_ShouldAllHaveAntiForgeryTokens()
    {
        var root = FindRepositoryRoot();
        var view = Read(Path.Combine(root, "Views", "Reports", "ScheduledReports.cshtml"));
        var postForms = Regex.Matches(view, "<form[^>]*method=\"post\"[\\s\\S]*?</form>", RegexOptions.IgnoreCase);

        Assert.True(postForms.Count >= 3, "ScheduledReports can co cac form POST cho luu/bat tat/xoa lich.");
        foreach (Match form in postForms)
        {
            Assert.Contains("@Html.AntiForgeryToken()", form.Value, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void UiPolishCss_ShouldExposeReusableLayoutUtilitiesForLegacyScreens()
    {
        var root = FindRepositoryRoot();
        var css = Read(Path.Combine(root, "wwwroot", "css", "site.css"));

        foreach (var token in new[]
        {
            ".enterprise-inline-controls",
            ".enterprise-table-tools",
            ".table-card-body",
            ".cell-empty",
            ".badge-roomy",
            ".modal-body-scroll",
            ".warehouse-map",
            ".vas-qc-grid"
        })
        {
            Assert.Contains(token, css, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ProductImageUx_ShouldRenderCompactThumbnailsWithoutReplacingSkuControls()
    {
        var root = FindRepositoryRoot();
        var voucherCreate = Read(Path.Combine(root, "Views", "Vouchers", "Create.cshtml"));
        var itemIndex = Read(Path.Combine(root, "Views", "Items", "Index.cshtml"));
        var css = Read(Path.Combine(root, "wwwroot", "css", "site.css"));

        foreach (var token in new[]
        {
            "data-image-url",
            "renderItemSelectOption",
            "getSafeItemImageUrl",
            "data-wms-change-call=\"itemChanged\""
        })
        {
            Assert.Contains(token, voucherCreate, StringComparison.Ordinal);
        }

        // Oracle WMS pattern: item selection is shown only inside the dropdown control —
        // no duplicate preview row below the select.
        Assert.DoesNotContain("voucher-line-item-preview", voucherCreate, StringComparison.Ordinal);
        Assert.DoesNotContain("updateVoucherLineItemPreview", voucherCreate, StringComparison.Ordinal);

        foreach (var token in new[]
        {
            "item.ImageUrl",
            "item-thumb-table",
            "table-col-image",
            "loading=\"lazy\""
        })
        {
            Assert.Contains(token, itemIndex, StringComparison.Ordinal);
        }

        foreach (var token in new[]
        {
            ".item-thumb",
            ".item-select-option",
            ".item-image-cell"
        })
        {
            Assert.Contains(token, css, StringComparison.Ordinal);
        }
        Assert.DoesNotContain(".voucher-line-item-preview", css, StringComparison.Ordinal);
    }

    [Fact]
    public void ItemImageUpload_ShouldKeepExtensionMimeMagicBytesAndSizeGuards()
    {
        var root = FindRepositoryRoot();
        var controller = Read(Path.Combine(root, "Controllers", "ItemsController.cs"));

        foreach (var token in new[]
        {
            "AllowedImageExtensions",
            "AllowedImageMimeTypes",
            "file.Length > 5 * 1024 * 1024",
            "IsValidImageContent",
            "OpenReadStream",
            "\"uploads\", \"items\"",
            "\"/uploads/items/{fileName}\""
        })
        {
            Assert.Contains(token, controller, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Remaining100EvidenceRegister_ShouldBeHonestAboutLocalAndBlockedArtifacts()
    {
        var root = FindRepositoryRoot();
        var register = Read(Path.Combine(root, "ENTERPRISE_WMS_100_PERCENT_EVIDENCE_REGISTER_2026_05_17.md"));
        var remaining = Read(Path.Combine(root, "ENTERPRISE_WMS_100_PERCENT_REMAINING_TASKS_MOBILE_AUDIT_2026_05_17.md"));

        foreach (var token in new[]
        {
            "EV-BUILD-001",
            "EV-MOB-002",
            "EV-LOAD-001",
            "Blocked: needs staging",
            "Blocked: needs real device",
            "Không xóa hoặc sửa connection string/API key",
            "Không in secret value"
        })
        {
            Assert.Contains(token, register, StringComparison.Ordinal);
        }

        Assert.Contains("ENTERPRISE_WMS_100_PERCENT_EVIDENCE_REGISTER_2026_05_17.md", remaining, StringComparison.Ordinal);
        Assert.DoesNotContain("tests/visual/.auth/wms-auth-state.json` | Pass", register, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MobilePriorityViews_ShouldExposeSharedOneHandAndHybridCardPatterns()
    {
        var root = FindRepositoryRoot();
        var css = Read(Path.Combine(root, "wwwroot", "css", "site.css"));
        var rfReceiving = Read(Path.Combine(root, "Views", "Operations", "RfReceiving.cshtml"));
        var rfPicking = Read(Path.Combine(root, "Views", "Operations", "RfPicking.cshtml"));
        var rfMovement = Read(Path.Combine(root, "Views", "Operations", "RfMovement.cshtml"));
        var movementTasks = Read(Path.Combine(root, "Views", "Operations", "MovementTasks.cshtml"));
        var inventory = Read(Path.Combine(root, "Views", "Reports", "Inventory.cshtml"));
        var stockValuation = Read(Path.Combine(root, "Views", "Reports", "StockValuation.cshtml"));
        var spaceUtilization = Read(Path.Combine(root, "Views", "Reports", "SpaceUtilization.cshtml"));
        var auditTrail = Read(Path.Combine(root, "Views", "Reports", "AuditTrail.cshtml"));
        var dockToStock = Read(Path.Combine(root, "Views", "Reports", "DockToStock.cshtml"));
        var voucherCreate = Read(Path.Combine(root, "Views", "Vouchers", "Create.cshtml"));

        foreach (var token in new[]
        {
            ".mobile-filter-card",
            ".rf-card-grid",
            ".rf-task-card-header",
            ".rf-action-row",
            ".mobile-table-card-list",
            ".mobile-table-card-source",
            ".voucher-mobile-line-editor",
            ".scanner-modal"
        })
        {
            Assert.Contains(token, css, StringComparison.Ordinal);
        }

        foreach (var view in new[] { rfReceiving, rfPicking, rfMovement })
        {
            Assert.Contains("rf-card-grid", view, StringComparison.Ordinal);
            Assert.Contains("rf-task-card-header", view, StringComparison.Ordinal);
            Assert.Contains("rf-action-row", view, StringComparison.Ordinal);
            Assert.DoesNotContain("<" + "style>", view, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var view in new[] { movementTasks, inventory, stockValuation, spaceUtilization, auditTrail, dockToStock })
        {
            Assert.Contains("mobile-table-card-list", view, StringComparison.Ordinal);
            Assert.Contains("mobile-table-card-source", view, StringComparison.Ordinal);
        }

        Assert.Contains("voucher-mobile-line-editor", voucherCreate, StringComparison.Ordinal);
    }

    [Fact]
    public void VisualRegressionSpec_ShouldUseReadableVietnameseAndMobileAssertions()
    {
        var root = FindRepositoryRoot();
        var spec = Read(Path.Combine(root, "tests", "visual", "wms-visual-regression.spec.ts"));
        var mobileDeepSpec = Read(Path.Combine(root, "tests", "visual", "wms-mobile-deep.spec.ts"));
        var mobileDeepConfig = Read(Path.Combine(root, "tests", "visual", "playwright.mobile-deep.config.ts"));
        var packageJson = Read(Path.Combine(root, "package.json"));

        foreach (var token in new[]
        {
            "Trang chính",
            "Nhập kho",
            "Xuất kho",
            "Hướng dẫn sử dụng",
            "mobile scanner modal fits viewport",
            "document.documentElement.scrollWidth",
            "toBeLessThanOrEqual(24)"
        })
        {
            Assert.Contains(token, spec, StringComparison.Ordinal);
        }

        foreach (var mojibake in new[]
        {
            TextFromCodePoints(0x00C3),
            TextFromCodePoints(0x00C4),
            TextFromCodePoints(0x00C6),
            TextFromCodePoints(0x00E1, 0x00BA),
            TextFromCodePoints(0x00E1, 0x00BB)
        })
        {
            Assert.DoesNotContain(mojibake, spec, StringComparison.Ordinal);
        }

        foreach (var token in new[]
        {
            "visual:mobile-deep",
            "phone-small-360",
            "phone-pixel-390",
            "phone-large-430",
            "tablet-portrait-768"
        })
        {
            Assert.Contains(token, packageJson + mobileDeepConfig, StringComparison.Ordinal);
        }

        foreach (var token in new[]
        {
            "assertHorizontalOverflowIsContained",
            "assertFixedChromeDoesNotCoverActions",
            "assertTapTargetsAndButtonTextFit",
            "no-seed-data",
            "console errors"
        })
        {
            Assert.Contains(token, mobileDeepSpec, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void EnterpriseAuthPolish_ShouldKeepLoginImmediateLightweightAndAccessible()
    {
        var root = FindRepositoryRoot();
        var css = Read(Path.Combine(root, "wwwroot", "css", "site.css"));
        var login = Read(Path.Combine(root, "Views", "Account", "Login.cshtml"));
        var mfa = Read(Path.Combine(root, "Views", "Account", "VerifyMfa.cshtml"));
        var setupAdmin = Read(Path.Combine(root, "Views", "Account", "SetupAdmin.cshtml"));
        var devReset = Read(Path.Combine(root, "Views", "Account", "DevResetPassword.cshtml"));

        foreach (var view in new[] { login, mfa, setupAdmin, devReset })
        {
            Assert.Contains("login-page enterprise-auth-page", view, StringComparison.Ordinal);
            Assert.Contains("enterprise-auth-stage", view, StringComparison.Ordinal);
            Assert.Contains("enterprise-auth-visual", view, StringComparison.Ordinal);
            Assert.Contains("enterprise-auth-panel", view, StringComparison.Ordinal);
            Assert.Contains("enterprise-auth-submit", view, StringComparison.Ordinal);
            Assert.DoesNotContain("splash", view, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("setTimeout", view, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("asp-antiforgery=\"true\"", login, StringComparison.Ordinal);
        foreach (var view in new[] { mfa, setupAdmin, devReset })
        {
            Assert.Contains("@Html.AntiForgeryToken()", view, StringComparison.Ordinal);
        }

        Assert.Contains("class=\"enterprise-auth-panel login-card\"", login, StringComparison.Ordinal);
        Assert.Contains("class=\"enterprise-auth-panel login-card\"", mfa, StringComparison.Ordinal);
        Assert.Contains("class=\"enterprise-auth-panel auth-card\"", setupAdmin, StringComparison.Ordinal);
        Assert.Contains("class=\"enterprise-auth-panel auth-card\"", devReset, StringComparison.Ordinal);

        Assert.Contains("name=\"ReturnUrl\" value=\"@Model.ReturnUrl\"", login, StringComparison.Ordinal);
        Assert.Contains("autocomplete=\"username\"", login, StringComparison.Ordinal);
        Assert.Contains("autocomplete=\"current-password\"", login, StringComparison.Ordinal);
        Assert.Contains("Đăng nhập WMS Pro", login, StringComparison.Ordinal);
        Assert.Contains("<h2>WMS Pro</h2>", login, StringComparison.Ordinal);
        Assert.DoesNotContain("Warehouse Management System", login, StringComparison.Ordinal);
        Assert.Contains("Quản lý nhập kho, xuất kho, tồn kho và vận hành kho tập trung.", login, StringComparison.Ordinal);
        Assert.Contains("Theo dõi tồn kho thời gian thực", login, StringComparison.Ordinal);
        Assert.Contains("Quên mật khẩu hoặc tài khoản bị khóa?", login, StringComparison.Ordinal);
        Assert.Contains("Gửi yêu cầu để quản trị kho hoặc bộ phận công nghệ thông tin kiểm tra và liên hệ lại.", login, StringComparison.Ordinal);
        Assert.Contains("asp-action=\"AccessHelp\"", login, StringComparison.Ordinal);
        Assert.Contains("Chỉ dành cho tài khoản nội bộ được cấp quyền.", login, StringComparison.Ordinal);
        Assert.DoesNotContain("hieuctttb01413", login, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("0347681019", login, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name=\"ChallengeId\" value=\"@Model.ChallengeId\"", mfa, StringComparison.Ordinal);
        Assert.Contains("name=\"ReturnUrl\" value=\"@Model.ReturnUrl\"", mfa, StringComparison.Ordinal);
        Assert.Contains("maxlength=\"6\" inputmode=\"numeric\" autocomplete=\"one-time-code\"", mfa, StringComparison.Ordinal);

        foreach (var bannedUiText in new[]
        {
            "HttpOnly",
            "cookie",
            "MFA",
            "Captcha",
            "audit",
            "session",
            "token",
            "API",
            "backend",
            "middleware",
            "encryption",
            "database",
            "security mechanism"
        })
        {
            Assert.DoesNotContain(bannedUiText, login, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var token in new[]
        {
            ".enterprise-auth-page",
            ".enterprise-auth-stage",
            ".enterprise-auth-visual",
            ".enterprise-auth-panel",
            ".enterprise-auth-status",
            ".enterprise-auth-feature-grid",
            ".enterprise-auth-feature-card",
            ".enterprise-auth-help",
            ".enterprise-auth-help-link",
            ".enterprise-auth-help-button",
            ".enterprise-auth-submit",
            "min-height: 100svh",
            "min-height: 44px",
            "@media (max-width: 860px)",
            "@media (prefers-reduced-motion: reduce)"
        })
        {
            Assert.Contains(token, css, StringComparison.Ordinal);
        }

        var authCssStart = css.IndexOf(".enterprise-auth-page", StringComparison.Ordinal);
        var authCssEnd = css.IndexOf("/* ============ AUDIT TRAILS ENHANCED", authCssStart, StringComparison.Ordinal);
        Assert.True(authCssStart >= 0 && authCssEnd > authCssStart, "Khong tim thay block CSS auth enterprise.");
        var authCss = css[authCssStart..authCssEnd];
        Assert.DoesNotContain("background-image: url(", authCss, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("video", authCss, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BrandFavicon_ShouldUseVersionedWarehouseMarkInsteadOfLegacyRootIcon()
    {
        var root = FindRepositoryRoot();
        var layout = Read(Path.Combine(root, "Views", "Shared", "_Layout.cshtml"));
        var serviceWorker = Read(Path.Combine(root, "wwwroot", "service-worker.js"));
        var logo = Read(Path.Combine(root, "wwwroot", "images", "logo.svg"));
        var faviconPath = Path.Combine(root, "wwwroot", "favicon.ico");
        var favicon = File.ReadAllBytes(faviconPath);
        var oldLegacyHash = "26DC5FF4BFB9213291735808465E156D4A4691135F3815E3613761243E1F69C3";
        var currentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(favicon));

        Assert.Contains("<link rel=\"icon\" type=\"image/svg+xml\" href=\"~/images/logo.svg\" asp-append-version=\"true\" />", layout, StringComparison.Ordinal);
        Assert.Contains("<link rel=\"alternate icon\" type=\"image/x-icon\" href=\"~/favicon.ico\" asp-append-version=\"true\" />", layout, StringComparison.Ordinal);
        Assert.Contains("#F39C2A", logo, StringComparison.Ordinal);
        Assert.Contains("#0F3A6F", logo, StringComparison.Ordinal);
        Assert.Contains("'/favicon.ico'", serviceWorker, StringComparison.Ordinal);
        Assert.Contains("wms-pro-pwa-shell-v20260521-p406", serviceWorker, StringComparison.Ordinal);
        Assert.NotEqual(oldLegacyHash, currentHash);

        Assert.Equal((ushort)0, BitConverter.ToUInt16(favicon, 0));
        Assert.Equal((ushort)1, BitConverter.ToUInt16(favicon, 2));
        Assert.Equal((ushort)2, BitConverter.ToUInt16(favicon, 4));
        Assert.Equal((byte)32, favicon[6]);
        Assert.Equal((byte)16, favicon[22]);

        foreach (var entryIndex in new[] { 0, 1 })
        {
            var directoryOffset = 6 + entryIndex * 16;
            var imageSize = BitConverter.ToUInt32(favicon, directoryOffset + 8);
            var imageOffset = BitConverter.ToUInt32(favicon, directoryOffset + 12);
            var imageStart = checked((int)imageOffset);
            Assert.True(imageSize > 500, "Favicon entry is unexpectedly small.");
            Assert.Equal((byte)0x89, favicon[imageStart]);
            Assert.Equal((byte)'P', favicon[imageStart + 1]);
            Assert.Equal((byte)'N', favicon[imageStart + 2]);
            Assert.Equal((byte)'G', favicon[imageStart + 3]);
        }
    }

    [Fact]
    public void ProductionViewsAndCustomScripts_ShouldNotContainConsoleLog()
    {
        var root = FindRepositoryRoot();
        var viewFiles = Directory.EnumerateFiles(Path.Combine(root, "Views"), "*.cshtml", SearchOption.AllDirectories);
        var customScripts = Directory.EnumerateFiles(Path.Combine(root, "wwwroot"), "*.js", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(root, path).StartsWith(Path.Combine("wwwroot", "lib"), StringComparison.OrdinalIgnoreCase));

        var failures = viewFiles.Concat(customScripts)
            .Select(path => new { Path = path, Content = Read(path) })
            .Where(file => Regex.IsMatch(file.Content, @"console\.log\s*\(", RegexOptions.CultureInvariant))
            .Select(file => Path.GetRelativePath(root, file.Path))
            .ToList();

        Assert.True(failures.Count == 0, "Production UI still contains console.log: " + string.Join(", ", failures));
    }

    [Fact]
    public void DockBoard_DynamicMilestoneForms_ShouldCarryAntiForgeryToken()
    {
        var root = FindRepositoryRoot();
        var dockBoard = Read(Path.Combine(root, "Views", "Operations", "DockBoard.cshtml"));

        Assert.Contains("@inject Microsoft.AspNetCore.Antiforgery.IAntiforgery Antiforgery", dockBoard, StringComparison.Ordinal);
        Assert.Contains("id=\"dockAntiForgery\"", dockBoard, StringComparison.Ordinal);
        Assert.Contains("Antiforgery.GetAndStoreTokens(Context).RequestToken", dockBoard, StringComparison.Ordinal);
        Assert.Contains("function actionForm(row, milestone, label, icon, buttonClass)", dockBoard, StringComparison.Ordinal);
        Assert.Contains("const token = document.getElementById('dockAntiForgery')?.value || ''", dockBoard, StringComparison.Ordinal);
        Assert.Contains("name=\"__RequestVerificationToken\"", dockBoard, StringComparison.Ordinal);
        Assert.Contains("value=\"${escapeDockText(token)}\"", dockBoard, StringComparison.Ordinal);
        Assert.Contains("action=\"${dockMilestoneUrl}\"", dockBoard, StringComparison.Ordinal);
    }

    [Fact]
    public void FullSystemFinalPolish_ShouldCoverReportedWeakScreens()
    {
        var root = FindRepositoryRoot();
        var layout = Read(Path.Combine(root, "Views", "Shared", "_Layout.cshtml"));
        var zoneAssignment = Read(Path.Combine(root, "Views", "Operations", "ZoneAssignment.cshtml"));
        var periodLocks = Read(Path.Combine(root, "Views", "Reports", "PeriodLocks.cshtml"));
        var exceptionCenter = Read(Path.Combine(root, "Views", "Operations", "ExceptionCenter.cshtml"));
        var aiAssistant = Read(Path.Combine(root, "Views", "Reports", "AiAssistant.cshtml"));
        var css = Read(Path.Combine(root, "wwwroot", "css", "site.css"));

        Assert.Contains("fa-hourglass-half", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("fa-snail", layout, StringComparison.Ordinal);

        foreach (var token in new[]
        {
            "zone-assignment-section",
            "zone-assignment-table",
            "zone-assignment-options-cell"
        })
        {
            Assert.Contains(token, zoneAssignment, StringComparison.Ordinal);
        }

        foreach (var token in new[]
        {
            "period-lock-control-panel",
            "period-locks-table",
            "period-lock-chip is-active",
            "period-lock-chip is-open",
            "data-enterprise-confirm="
        })
        {
            Assert.Contains(token, periodLocks, StringComparison.Ordinal);
        }
        Assert.DoesNotContain("js-clear-lock", periodLocks, StringComparison.Ordinal);

        Assert.Contains("exception-center-table", exceptionCenter, StringComparison.Ordinal);
        Assert.Contains("exception-action-cell", exceptionCenter, StringComparison.Ordinal);
        Assert.Contains("ai-assistant-command-center", aiAssistant, StringComparison.Ordinal);
        Assert.Contains("ai-assistant-query-form", aiAssistant, StringComparison.Ordinal);

        foreach (var token in new[]
        {
            ".zone-assignment-options-cell .zone-option-list",
            "max-height: 324px",
            ".period-lock-chip.is-active",
            ".period-lock-chip.is-open",
            ".exception-action-cell",
            ".ai-assistant-command-center"
        })
        {
            Assert.Contains(token, css, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void FinalZeroMarkerRepoGate_ShouldRejectUtf8ReplacementQuestionMarkMojibakeAndRootAssetRoutes()
    {
        var root = FindRepositoryRoot();
        var files = EnumerateAuthoredTextFiles(root).ToList();
        var replacementCharacter = char.ConvertFromUtf32(0xFFFD);
        var knownBrokenFragments = new[]
        {
            TextFromCodePoints(0x0050, 0x0068, 0x0069, 0x003F, 0x0075),
            TextFromCodePoints(0x0053, 0x003F, 0x0020, 0x006C, 0x0075, 0x003F, 0x006E, 0x0067),
            TextFromCodePoints(0x004B, 0x0069, 0x003F, 0x006E, 0x003A),
            TextFromCodePoints(0x0056, 0x003F, 0x006E, 0x0020, 0x0064, 0x006F, 0x006E),
            TextFromCodePoints(0x004D, 0x003F, 0x0075, 0x0020, 0x006D, 0x003F, 0x0063)
        };
        var rootAssetMarkers = new[] { "href=\"/", "action=\"/", "src=\"/" };
        var failures = new List<string>();

        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
            var content = Read(file);

            if (content.Contains(replacementCharacter, StringComparison.Ordinal))
                failures.Add($"{relative} contains UTF-8 replacement character.");

            foreach (var fragment in knownBrokenFragments)
            {
                if (content.Contains(fragment, StringComparison.Ordinal))
                    failures.Add($"{relative} contains known question-mark mojibake fragment.");
            }

            if ((relative.StartsWith("Views/", StringComparison.OrdinalIgnoreCase)
                    || relative.StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase))
                && (relative.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase)
                    || relative.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                    || relative.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
                    || relative.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
                    || relative.EndsWith(".webmanifest", StringComparison.OrdinalIgnoreCase)))
            {
                foreach (var marker in rootAssetMarkers)
                {
                    if (content.Contains(marker, StringComparison.OrdinalIgnoreCase))
                        failures.Add($"{relative} contains hard-coded root asset marker `{marker}`.");
                }
            }
        }

        Assert.True(failures.Count == 0, "Final zero-marker repo gate failed:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void RuntimeNoiseFixes_ShouldUseAsyncScopesDeterministicRollbackAndSilentTelemetryCancellation()
    {
        var root = FindRepositoryRoot();
        var replenishment = Read(Path.Combine(root, "Common", "ReplenishmentAutomationHostedService.cs"));
        var dbContext = Read(Path.Combine(root, "Data", "AppDbContext.cs"));
        var enterpriseServices = Read(Path.Combine(root, "Services", "Enterprise1113Services.cs"));
        var verificationScript = Read(Path.Combine(root, "scripts", "Run-WmsVerification.ps1"));

        Assert.Contains("await using var scope = _serviceProvider.CreateAsyncScope();", replenishment, StringComparison.Ordinal);
        Assert.DoesNotContain("_serviceProvider.CreateScope()", replenishment, StringComparison.Ordinal);
        Assert.Contains("createdTx.RollbackAsync(CancellationToken.None)", dbContext, StringComparison.Ordinal);
        Assert.Contains("RecordRequestAsync(new RequestTelemetryLog", enterpriseServices, StringComparison.Ordinal);
        Assert.Contains("}, CancellationToken.None);", enterpriseServices, StringComparison.Ordinal);
        Assert.Contains("catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)", enterpriseServices, StringComparison.Ordinal);
        Assert.DoesNotContain("logger.LogWarning(ex, \"Unable to record request telemetry", enterpriseServices, StringComparison.Ordinal);
        Assert.Contains("Assert-CleanWmsServerLogs", verificationScript, StringComparison.Ordinal);
        Assert.Contains("Skip'/'Take' without an 'OrderBy'", verificationScript, StringComparison.Ordinal);
        Assert.Contains("only implements IAsyncDisposable", verificationScript, StringComparison.Ordinal);
        Assert.Contains("Unable to record request telemetry", verificationScript, StringComparison.Ordinal);
    }

    [Fact]
    public void EfPagingWarnings_ShouldHaveDeterministicOrderingOnKnownRuntimeHotspots()
    {
        var root = FindRepositoryRoot();
        var operations = Read(Path.Combine(root, "Controllers", "OperationsController.cs"));
        var advanced = Read(Path.Combine(root, "Controllers", "OperationsController.Advanced.cs"));
        var enterpriseServices = Read(Path.Combine(root, "Services", "Enterprise1113Services.cs"));
        var labor = Read(Path.Combine(root, "Services", "LaborManagementService.cs"));
        var optimization = Read(Path.Combine(root, "Services", "OptimizationAutomationIntegrationEnterpriseService.cs"));
        var serial = Read(Path.Combine(root, "Services", "SerialInventoryService.cs"));

        Assert.Contains("OrderByDescending(l => l.LpnCode.ToUpper() == lpnCode.ToUpper())", operations, StringComparison.Ordinal);
        Assert.Contains("orderby cycleCountSchedule.NextScheduledAt", advanced, StringComparison.Ordinal);
        Assert.Contains(".OrderBy(x => x.ExpiryDate)", enterpriseServices, StringComparison.Ordinal);
        Assert.Contains(".OrderBy(x => x.RequestedDeliveryDate)", enterpriseServices, StringComparison.Ordinal);
        Assert.Contains(".OrderByDescending(x => x.ChangedAt)", enterpriseServices, StringComparison.Ordinal);
        Assert.Contains(".OrderByDescending(t => t.CompletedAt)", labor, StringComparison.Ordinal);
        Assert.Contains(".OrderByDescending(v => v.CompletedAt)", labor, StringComparison.Ordinal);
        Assert.Contains(".ThenBy(t => t.PickTaskId)", optimization, StringComparison.Ordinal);
        Assert.Contains(".OrderBy(s => s.SerialCode)", serial, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "WMS.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("Cannot find repository root.");
    }

    private static string Read(string path) => File.ReadAllText(path);

    private static IEnumerable<string> EnumerateAuthoredTextFiles(string root)
    {
        var excludedSegments = new[]
        {
            "bin",
            "obj",
            "node_modules",
            "artifacts",
            "test-results",
            "uploads",
            "App_Data"
        };
        var includedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs",
            ".cshtml",
            ".js",
            ".css",
            ".json",
            ".md",
            ".ps1",
            ".ts",
            ".csproj",
            ".sln",
            ".webmanifest",
            ".html",
            ".yml",
            ".yaml",
            ".config"
        };

        return Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(path => includedExtensions.Contains(Path.GetExtension(path)))
            .Where(path => !excludedSegments.Any(segment => path.Contains($"{Path.DirectorySeparatorChar}{segment}{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}wwwroot{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}visual{Path.DirectorySeparatorChar}.auth{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
    }

    private static string TextFromCodePoints(params int[] codePoints)
    {
        var builder = new StringBuilder();
        foreach (var codePoint in codePoints)
            builder.Append(char.ConvertFromUtf32(codePoint));
        return builder.ToString();
    }
}
