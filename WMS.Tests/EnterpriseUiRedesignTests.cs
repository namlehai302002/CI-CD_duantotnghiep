using System.Text;
using System.Text.RegularExpressions;

namespace WMS.Tests;

public class EnterpriseUiRedesignTests
{
    [Fact]
    public void Layout_ShouldExposeEnterpriseTopbarAndScrollableNavigation()
    {
        var root = FindRepositoryRoot();
        var layout = ReadUtf8(Path.Combine(root, "Views", "Shared", "_Layout.cshtml"));

        Assert.Contains("class=\"app-topbar\"", layout, StringComparison.Ordinal);
        Assert.Contains("class=\"sidebar app-sidebar\"", layout, StringComparison.Ordinal);
        Assert.Contains("id=\"sidebarSearchInput\"", layout, StringComparison.Ordinal);
        Assert.Contains("id=\"sidebarPinToggle\"", layout, StringComparison.Ordinal);
        Assert.Contains("class=\"warehouse-context\"", layout, StringComparison.Ordinal);
        Assert.Contains("class=\"topbar-user\"", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"sidebarToggle\"", layout.Replace("id=\"sidebarToggle\" class=\"topbar-icon-btn\"", string.Empty), StringComparison.Ordinal);
    }

    [Fact]
    public void Layout_ShouldHideScannerQueueOnAccountPages()
    {
        var root = FindRepositoryRoot();
        var layout = ReadUtf8(Path.Combine(root, "Views", "Shared", "_Layout.cshtml"));
        var pwa = ReadUtf8(Path.Combine(root, "wwwroot", "js", "pwa.js"));
        var queue = ReadUtf8(Path.Combine(root, "wwwroot", "js", "offline-scan-queue.js"));

        Assert.Contains("var layoutIsAccountPage = layoutControllerName == \"Account\";", layout, StringComparison.Ordinal);
        Assert.Contains("data-wms-operational=\"@(layoutCanOperate ? \"true\" : \"false\")\"", layout, StringComparison.Ordinal);

        var widgetIndex = layout.IndexOf("id=\"offlineQueueWidget\"", StringComparison.Ordinal);
        Assert.True(widgetIndex > 0, "Layout cần có tiện ích hàng đợi quét cho trang vận hành.");
        Assert.True(layout.LastIndexOf("@if (layoutCanOperate)", widgetIndex, StringComparison.Ordinal) >= 0,
            "Tiện ích hàng đợi quét phải nằm trong điều kiện layoutCanOperate để không hiện ở màn đăng nhập.");

        var scannerIndex = layout.IndexOf("<partial name=\"_ScannerModal\" />", StringComparison.Ordinal);
        Assert.True(scannerIndex > 0, "Layout cần có modal quét dùng chung cho trang vận hành.");
        Assert.True(layout.LastIndexOf("@if (layoutCanOperate)", scannerIndex, StringComparison.Ordinal) >= 0,
            "Modal quét phải nằm trong điều kiện layoutCanOperate để không render ở màn đăng nhập.");

        Assert.Contains("if (!operationalPage)", pwa, StringComparison.Ordinal);
        Assert.Contains("document.body?.dataset?.wmsOperational !== 'true'", queue, StringComparison.Ordinal);
    }

    [Fact]
    public void DesignSystem_ShouldUseRedwoodInspiredTokensAndSharedComponents()
    {
        var root = FindRepositoryRoot();
        var css = ReadUtf8(Path.Combine(root, "wwwroot", "css", "site.css"));

        var requiredTokens = new[]
        {
            "--accent: #C74634",
            "--header-height: 56px",
            "--sidebar-collapsed-width",
            ".app-topbar",
            ".app-sidebar",
            ".app-springboard",
            ".filter-panel",
            ".data-table",
            ".status-badge",
            ".metric-card",
            ".task-card",
            ".form-footer",
            ".side-panel",
            ".mobile-scan-panel",
            ".mobile-action-bar",
            ".enterprise-form-layout",
            ".enterprise-form-aside",
            ".auth-card",
            ".trusted-device-grid",
            ".audit-checklist"
        };

        var missing = requiredTokens
            .Where(token => !css.Contains(token, StringComparison.Ordinal))
            .ToList();

        Assert.True(missing.Count == 0, "Thieu token/class giao dien enterprise: " + string.Join(", ", missing));
        Assert.DoesNotContain("max-height: 600px", css, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("letter-spacing: -", css, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CollapsedSidebar_ShouldExposePrimaryGroupsWithAccessibleFlyouts()
    {
        var root = FindRepositoryRoot();
        var layout = ReadUtf8(Path.Combine(root, "Views", "Shared", "_Layout.cshtml"));
        var css = ReadUtf8(Path.Combine(root, "wwwroot", "css", "site.css"));

        foreach (var label in new[]
        {
            "Trang chính",
            "Nhập kho",
            "Xuất kho",
            "Tồn kho",
            "Tra cứu phiếu",
            "Báo cáo",
            "Danh mục",
            "Hệ thống",
            "Hướng dẫn sử dụng"
        })
        {
            Assert.Contains($"data-nav-label=\"{label}\"", layout, StringComparison.Ordinal);
        }

        foreach (var token in new[]
        {
            "data-nav-kind=\"group\"",
            "data-nav-kind=\"link\"",
            "button class=\"nav-section-title",
            "aria-expanded=\"true\"",
            "openSidebarFlyout",
            "closeSidebarFlyout",
            "isDesktopCollapsed",
            "flyout-open"
        })
        {
            Assert.Contains(token, layout, StringComparison.Ordinal);
        }

        foreach (var token in new[]
        {
            "body.sidebar-collapsed .nav-section-title",
            "body.sidebar-collapsed .nav-section.flyout-open .nav-section-body",
            "body.sidebar-collapsed .nav-rail-link:hover::after",
            "--flyout-top",
            "position: fixed",
            "max-height: min(560px"
        })
        {
            Assert.Contains(token, css, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("on" + "click=\"toggleSection", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("body.sidebar-collapsed .nav-section-title span", css, StringComparison.Ordinal);
        Assert.DoesNotContain("body.sidebar-collapsed .nav-section-body {\r\n    display: block;", css, StringComparison.Ordinal);
        Assert.DoesNotContain("body.sidebar-collapsed .nav-section-body {\n    display: block;", css, StringComparison.Ordinal);
    }

    [Fact]
    public void InternalOperationLinks_ShouldNotUseTranslatedActionNames()
    {
        var root = FindRepositoryRoot();
        var viewRoot = Path.Combine(root, "Views");
        var failures = Directory.EnumerateFiles(viewRoot, "*.cshtml", SearchOption.AllDirectories)
            .Select(path => new
            {
                Relative = Path.GetRelativePath(root, path),
                Content = ReadUtf8(path)
            })
            .SelectMany(file => Regex.Matches(file.Content, @"href=""/Operations/[^""]*(?:\s|[^\u0000-\u007F])")
                .Select(match => $"{file.Relative}: {match.Value}"))
            .ToList();

        Assert.True(failures.Count == 0, "Link /Operations không được chứa dấu, khoảng trắng hoặc action đã dịch:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void ModalCss_ShouldKeepActionFooterVisibleWithoutFullscreen()
    {
        var root = FindRepositoryRoot();
        var css = ReadUtf8(Path.Combine(root, "wwwroot", "css", "site.css"));

        var requiredRules = new[]
        {
            "max-height: calc(100dvh - 32px)",
            ".modal-body",
            "overflow-y: auto",
            ".app-modal-body",
            ".yardops-modal-body",
            ".scanner-modal-body",
            "overscroll-behavior: contain"
        };

        var missing = requiredRules.Where(rule => !css.Contains(rule, StringComparison.Ordinal)).ToList();
        Assert.True(missing.Count == 0, "CSS modal chưa khóa đủ chiều cao/cuộn nội dung: " + string.Join(", ", missing));
    }

    [Fact]
    public void HomePage_ShouldRenderSpringboardWorkQueueAndEnterpriseSections()
    {
        var root = FindRepositoryRoot();
        var home = ReadUtf8(Path.Combine(root, "Views", "Home", "Index.cshtml"));

        Assert.Contains("class=\"app-springboard\"", home, StringComparison.Ordinal);
        Assert.Contains("Bàn làm việc nhanh", home, StringComparison.Ordinal);
        Assert.Contains("Công việc cần xử lý", home, StringComparison.Ordinal);
        Assert.Contains("Nhập kho", home, StringComparison.Ordinal);
        Assert.Contains("Xuất kho", home, StringComparison.Ordinal);
        Assert.Contains("Vận chuyển", home, StringComparison.Ordinal);
        Assert.Contains("class=\"task-card-grid\"", home, StringComparison.Ordinal);
        Assert.Contains("class=\"enterprise-section workbench-grid\"", home, StringComparison.Ordinal);
    }

    [Fact]
    public void UserManagement_ShouldRenderEnterpriseIdentityWorkspace()
    {
        var root = FindRepositoryRoot();
        var users = ReadUtf8(Path.Combine(root, "Views", "Users", "Index.cshtml"));
        var css = ReadUtf8(Path.Combine(root, "wwwroot", "css", "site.css"));

        Assert.Contains("identity-dashboard", users, StringComparison.Ordinal);
        Assert.Contains("identity-layout", users, StringComparison.Ordinal);
        Assert.Contains("id=\"identityUserTable\"", users, StringComparison.Ordinal);
        Assert.Contains("Tải danh sách", users, StringComparison.Ordinal);
        Assert.Contains("js-reset-password", users, StringComparison.Ordinal);
        Assert.Contains("Quy trình tạo tài khoản", users, StringComparison.Ordinal);
        Assert.Contains("Ma trận vai trò", users, StringComparison.Ordinal);
        Assert.Contains("Tạo tài khoản nhân sự", users, StringComparison.Ordinal);
        Assert.Contains("identity-dashboard", css, StringComparison.Ordinal);
        Assert.Contains("identity-filter", css, StringComparison.Ordinal);
        Assert.Contains("identity-users-table", css, StringComparison.Ordinal);
        Assert.Contains("min-width: 1180px", css, StringComparison.Ordinal);
    }

    [Fact]
    public void OnboardingFolder_ShouldContainCompleteHandoverReadingPath()
    {
        var root = FindRepositoryRoot();
        var folder = Path.Combine(root, "TAI_LIEU_ONBOARDING_WMS");
        var requiredFiles = new[]
        {
            "README.md",
            "01_TONG_QUAN_HE_THONG.md",
            "02_KIEN_TRUC_VA_DU_LIEU.md",
            "03_NGHIEP_VU_NHAP_XUAT_TON.md",
            "04_VAN_HANH_DIEN_THOAI_VAN_CHUYEN.md",
            "05_PHAN_QUYEN_BAO_MAT.md",
            "06_HUONG_DAN_DEV_TEST.md",
            "07_THUAT_NGU.md",
            "08_BAN_DO_MAN_HINH_VA_LUONG_DU_LIEU.md",
            "09_CHECKLIST_BAN_GIAO_VAN_HANH.md"
        };

        foreach (var file in requiredFiles)
        {
            var path = Path.Combine(folder, file);
            Assert.True(File.Exists(path), $"Thieu tai lieu onboarding `{file}`.");
            Assert.True(new FileInfo(path).Length > 500, $"Tai lieu `{file}` qua ngan de ban giao cho nguoi moi.");
        }

        var readme = ReadUtf8(Path.Combine(folder, "README.md"));
        Assert.Contains("Nên Đọc Theo Thứ Tự", readme, StringComparison.Ordinal);
        Assert.Contains("Trạng Thái Hệ Thống", readme, StringComparison.Ordinal);
        Assert.Contains("08_BAN_DO_MAN_HINH_VA_LUONG_DU_LIEU.md", readme, StringComparison.Ordinal);
        Assert.Contains("09_CHECKLIST_BAN_GIAO_VAN_HANH.md", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void MasterDataAndAccountViews_ShouldAvoidThinScreensAndInlineStyling()
    {
        var root = FindRepositoryRoot();
        var files = new[]
        {
            Path.Combine("Views", "Warehouses", "Create.cshtml"),
            Path.Combine("Views", "Warehouses", "Edit.cshtml"),
            Path.Combine("Views", "Partners", "Create.cshtml"),
            Path.Combine("Views", "Partners", "Edit.cshtml"),
            Path.Combine("Views", "Categories", "Create.cshtml"),
            Path.Combine("Views", "Categories", "Edit.cshtml"),
            Path.Combine("Views", "Units", "Index.cshtml"),
            Path.Combine("Views", "Account", "Register.cshtml"),
            Path.Combine("Views", "Account", "TrustedDevices.cshtml"),
            Path.Combine("Views", "Home", "Privacy.cshtml"),
            Path.Combine("Views", "Shared", "Error.cshtml")
        };

        var failures = new List<string>();
        foreach (var relative in files)
        {
            var content = ReadUtf8(Path.Combine(root, relative));
            if (content.Length < 900)
            {
                failures.Add($"{relative} qua ngan, de tao cam giac man hinh so sai.");
            }

            if (!content.Contains("page-header", StringComparison.Ordinal)
                && !content.Contains("auth-card", StringComparison.Ordinal)
                && !content.Contains("empty-state", StringComparison.Ordinal))
            {
                failures.Add($"{relative} thieu cau truc trang enterprise.");
            }

            if (content.Contains("style" + "=", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{relative} con style truc tiep trong giao dien uu tien.");
            }
        }

        Assert.True(failures.Count == 0, "Man hinh nen tang chua dat chuan:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void EnterpriseUiAuditReport_ShouldDocumentReviewedAreasAndOpenFollowUps()
    {
        var root = FindRepositoryRoot();
        var reportPath = Path.Combine(root, "ENTERPRISE_UI_AUDIT_REPORT.md");

        Assert.True(File.Exists(reportPath), "Thieu bao cao kiem toan giao dien enterprise.");

        var report = ReadUtf8(reportPath);
        Assert.Contains("Báo Cáo Kiểm Toán", report, StringComparison.Ordinal);
        Assert.Contains("Kho, đối tác, danh mục, đơn vị tính", report, StringComparison.Ordinal);
        Assert.Contains("Tài khoản và bảo mật", report, StringComparison.Ordinal);
        Assert.Contains("Các điểm còn theo dõi", report, StringComparison.Ordinal);
    }

    [Fact]
    public void FinalUxPolish_ShouldUseVietnameseLabelsAndExcelExportOnPriorityScreens()
    {
        var root = FindRepositoryRoot();
        var priorityFiles = new[]
        {
            Path.Combine("Views", "Account", "Login.cshtml"),
            Path.Combine("Views", "Help", "Index.cshtml"),
            Path.Combine("Views", "Operations", "YardManagement.cshtml"),
            Path.Combine("Views", "Operations", "Receiving.cshtml"),
            Path.Combine("Views", "Operations", "RfReceiving.cshtml"),
            Path.Combine("Views", "Operations", "CarrierConnectors.cshtml"),
            Path.Combine("Views", "Operations", "ShippingDispatch.cshtml"),
            Path.Combine("Views", "Operations", "DeliveryReconciliation.cshtml"),
            Path.Combine("Views", "Shared", "_Layout.cshtml"),
            Path.Combine("wwwroot", "js", "pwa.js"),
            Path.Combine("wwwroot", "js", "offline-scan-queue.js")
        };

        var combined = string.Join(Environment.NewLine, priorityFiles.Select(file => ReadUtf8(Path.Combine(root, file))));
        var banned = new[]
        {
            "Yard Management",
            "Spot trống",
            "Spot đang",
            "Container No.",
            "Dwell (",
            "Free (",
            "Tài khoản (Email)",
            "Tài khoản (thư điện tử)",
            "Xuất tệp phân tách",
            "Tệp phân tách",
            "N/A",
            "local" + "host"
        };

        var failures = banned.Where(value => combined.Contains(value, StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.True(failures.Count == 0, "Màn ưu tiên còn nhãn chưa chuẩn: " + string.Join(", ", failures));

        Assert.Contains("Email Hoặc Tên đăng nhập", combined, StringComparison.Ordinal);
        Assert.Contains("Vị trí bãi trống", combined, StringComparison.Ordinal);
        Assert.Contains("Vị trí đang có xe", combined, StringComparison.Ordinal);
        Assert.Contains("Xuất Excel", combined, StringComparison.Ordinal);
        Assert.Contains("Bộ kết nối vận tải", combined, StringComparison.Ordinal);
        Assert.Contains("Hàng đợi quét", combined, StringComparison.Ordinal);
    }

    [Fact]
    public void Help_ShouldExplainRolesCoreFlowsMobileQueueAndCarrierConnector()
    {
        var root = FindRepositoryRoot();
        var help = ReadUtf8(Path.Combine(root, "Views", "Help", "Index.cshtml"));

        Assert.Contains("class=\"help-workspace\"", help, StringComparison.Ordinal);
        Assert.Contains("class=\"help-toc\"", help, StringComparison.Ordinal);
        Assert.Contains("class=\"help-accordion\"", help, StringComparison.Ordinal);
        Assert.Contains("<details class=\"help-accordion-item\"", help, StringComparison.Ordinal);
        Assert.Contains("helpExpandAll", help, StringComparison.Ordinal);
        Assert.Contains("helpCollapseAll", help, StringComparison.Ordinal);
        Assert.Contains("data-help-role=\"@currentRoleKey\"", help, StringComparison.Ordinal);
        Assert.Contains("wms-help-open-sections-v2-", help, StringComparison.Ordinal);
        Assert.Contains("CanSeeHelpSection", help, StringComparison.Ordinal);
        Assert.Contains("Bạn được thấy/làm gì", help, StringComparison.Ordinal);
        Assert.DoesNotContain("File bàn giao", help, StringComparison.Ordinal);
        Assert.DoesNotContain("HUONG_DAN_THUC_HANH_WMS_CHI_TIET.md", help, StringComparison.Ordinal);
        Assert.DoesNotContain("help-section-roles", help, StringComparison.Ordinal);

        var requiredSections = new[]
        {
            "Bắt đầu sử dụng hệ thống",
            "Vai trò và phân quyền",
            "Nhập kho",
            "Xuất kho",
            "Tồn kho",
            "Mã kiện",
            "Số sê-ri",
            "Cân trọng lượng thực tế",
            "Quét mã bằng điện thoại",
            "Hàng đợi quét khi mạng yếu",
            "Bổ sung hàng",
            "Di chuyển tồn",
            "Kiểm tra chất lượng",
            "Bãi đỗ",
            "Chuyến xe",
            "Vận đơn và bộ kết nối vận tải",
            "Đối soát giao hàng",
            "Trung tâm ngoại lệ",
            "Báo cáo",
            "Danh mục",
            "Người dùng, phân quyền và thiết bị tin cậy",
            "Kiểm tra cuối ca",
            "Mục đích",
            "Bạn được thấy/làm gì",
            "Vào màn nào",
            "Điều kiện cần có trước khi làm",
            "Các bước thao tác chi tiết",
            "Lỗi thường gặp",
            "Kết quả đúng sau khi hoàn tất"
        };

        var missing = requiredSections.Where(section => !help.Contains(section, StringComparison.Ordinal)).ToList();
        Assert.True(missing.Count == 0, "Hướng dẫn sử dụng thiếu mục bàn giao: " + string.Join(", ", missing));

        var sectionCount = help.Split("Key = \"", StringSplitOptions.None).Length - 1;
        Assert.True(sectionCount >= 22, $"Hướng dẫn cần đủ nhóm nghiệp vụ mở/đóng, hiện có {sectionCount} nhóm.");
    }

    [Fact]
    public void ThreePlBilling_ShouldUseEnterpriseWorkspaceAndDownloadActions()
    {
        var root = FindRepositoryRoot();
        var runs = ReadUtf8(Path.Combine(root, "Views", "Operations", "ThreePlBillingRuns.cshtml"));
        var rates = ReadUtf8(Path.Combine(root, "Views", "Operations", "ThreePlBillingRates.cshtml"));

        Assert.Contains("yardops-shell", runs, StringComparison.Ordinal);
        Assert.Contains("yardops-kpis", runs, StringComparison.Ordinal);
        Assert.Contains("id=\"threePlRunsTable\"", runs, StringComparison.Ordinal);
        Assert.Contains("data-wms-export-table=\"#threePlRunsTable\"", runs, StringComparison.Ordinal);
        Assert.Contains("GenerateThreePlBillingRun", runs, StringComparison.Ordinal);

        Assert.Contains("yardops-shell", rates, StringComparison.Ordinal);
        Assert.Contains("yardops-kpis", rates, StringComparison.Ordinal);
        Assert.Contains("id=\"threePlRatesTable\"", rates, StringComparison.Ordinal);
        Assert.Contains("data-wms-export-table=\"#threePlRatesTable\"", rates, StringComparison.Ordinal);
        Assert.Contains("threePlRateModal", rates, StringComparison.Ordinal);
        Assert.Contains("SaveThreePlBillingRate", rates, StringComparison.Ordinal);
    }

    [Fact]
    public void FloatingUtilities_ShouldReserveBottomSpaceForOperationalActions()
    {
        var root = FindRepositoryRoot();
        var css = ReadUtf8(Path.Combine(root, "wwwroot", "css", "site.css"));
        var pwa = ReadUtf8(Path.Combine(root, "wwwroot", "js", "pwa.js"));

        Assert.Contains("--wms-floating-widget-bottom", css, StringComparison.Ordinal);
        Assert.Contains("--wms-install-banner-bottom", css, StringComparison.Ordinal);
        Assert.Contains("bottom: calc(100% + 10px)", css, StringComparison.Ordinal);
        Assert.DoesNotContain("bottom: calc(86px + env(safe-area-inset-bottom))", css, StringComparison.Ordinal);
        Assert.Contains("wms-install-ready", pwa, StringComparison.Ordinal);
    }

    [Fact]
    public void TrustedDevices_ShouldRenderEnterpriseSecurityWorkspace()
    {
        var root = FindRepositoryRoot();
        var view = ReadUtf8(Path.Combine(root, "Views", "Account", "TrustedDevices.cshtml"));
        var css = ReadUtf8(Path.Combine(root, "wwwroot", "css", "site.css"));

        var requiredViewTokens = new[]
        {
            "trusted-device-workspace",
            "trusted-posture-banner",
            "trusted-kpi-strip",
            "trusted-device-main-grid",
            "trusted-device-table",
            "trusted-risk-badge",
            "trusted-action-panel",
            "Thiết bị tin cậy",
            "Trạng thái bảo vệ tài khoản",
            "Thiết bị hiện tại",
            "Tổng thiết bị tin cậy",
            "Lần xác thực gần nhất",
            "Thời hạn tin cậy",
            "Khuyến nghị xử lý",
            "Khi nào cần thu hồi?",
            "Đang tin cậy",
            "Cần kiểm tra",
            "Theo dõi định kỳ",
            "Bình thường",
            "Mức rủi ro",
            "Thu hồi thiết bị hiện tại",
            "Thu hồi tất cả thiết bị",
            "Quay lại trang chính"
        };

        var missingViewTokens = requiredViewTokens.Where(token => !view.Contains(token, StringComparison.Ordinal)).ToList();
        Assert.True(missingViewTokens.Count == 0, "Màn thiết bị tin cậy thiếu thành phần bảo mật enterprise: " + string.Join(", ", missingViewTokens));

        var requiredCssTokens = new[]
        {
            ".trusted-device-workspace",
            ".trusted-posture-banner",
            ".trusted-kpi-strip",
            ".trusted-device-main-grid",
            ".trusted-device-table",
            ".trusted-risk-badge",
            ".trusted-action-panel",
            ".trusted-empty-state"
        };
        var missingCssTokens = requiredCssTokens.Where(token => !css.Contains(token, StringComparison.Ordinal)).ToList();
        Assert.True(missingCssTokens.Count == 0, "CSS thiết bị tin cậy thiếu lớp enterprise: " + string.Join(", ", missingCssTokens));
    }

    [Fact]
    public void VoucherCreate_ShouldNotExposeDemoImport100Rows()
    {
        var root = FindRepositoryRoot();
        var createView = ReadUtf8(Path.Combine(root, "Views", "Vouchers", "Create.cshtml"));

        Assert.DoesNotContain("Mẫu demo 100 dòng", createView, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DownloadDemoImport100", createView, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Tải danh sách từ Excel", createView, StringComparison.Ordinal);
        Assert.Contains("Tải file mẫu", createView, StringComparison.Ordinal);
    }

    [Fact]
    public void DetailedPracticeGuide_ShouldExistAndCoverAllExercises()
    {
        var root = FindRepositoryRoot();
        var guidePath = Path.Combine(root, "HUONG_DAN_THUC_HANH_WMS_CHI_TIET.md");
        Assert.True(File.Exists(guidePath), "Thiếu file hướng dẫn thực hành chi tiết.");

        var guide = ReadUtf8(guidePath);
        Assert.True(guide.Length > 20000, "Hướng dẫn thực hành còn quá ngắn để bàn giao cho người mới.");

        var requiredChapters = Enumerable.Range(1, 25)
            .Select(i => $"Bài Thực Hành {i}:")
            .Append("Checklist Cuối Ca")
            .Append("Checklist Rà Soát Lỗi Thường Gặp")
            .ToList();

        var missingChapters = requiredChapters.Where(chapter => !guide.Contains(chapter, StringComparison.Ordinal)).ToList();
        Assert.True(missingChapters.Count == 0, "Hướng dẫn thực hành thiếu bài hoặc checklist: " + string.Join(", ", missingChapters));
    }

    [Fact]
    public void RoleAndPolicySurface_ShouldKeepAdminManagerStaffViewerBoundaries()
    {
        var root = FindRepositoryRoot();
        var layout = ReadUtf8(Path.Combine(root, "Views", "Shared", "_Layout.cshtml"));
        var usersController = ReadUtf8(Path.Combine(root, "Controllers", "UsersController.cs"));
        var accountController = ReadUtf8(Path.Combine(root, "Controllers", "AccountController.cs"));
        var inboundController = ReadUtf8(Path.Combine(root, "Controllers", "VouchersController.Inbound.cs"));
        var outboundController = ReadUtf8(Path.Combine(root, "Controllers", "VouchersController.Outbound.cs"));

        Assert.Contains("var layoutCanOperate = !layoutIsAccountPage && (User.IsInRole(\"Admin\") || User.IsInRole(\"Manager\") || User.IsInRole(\"Staff\"));", layout, StringComparison.Ordinal);
        Assert.Contains("var isViewer = !isAdmin && !isManager && !isStaff;", layout, StringComparison.Ordinal);
        Assert.Contains("var isAdminOrManager = isAdmin || isManager;", layout, StringComparison.Ordinal);
        Assert.Contains("@if (isAdmin)", layout, StringComparison.Ordinal);
        Assert.Contains("@if (isAdminOrManager)", layout, StringComparison.Ordinal);
        Assert.Contains("@if (isStaff)", layout, StringComparison.Ordinal);

        Assert.Contains("[Microsoft.AspNetCore.Authorization.Authorize(Roles = \"Admin\")]", usersController, StringComparison.Ordinal);
        Assert.Contains("[Microsoft.AspNetCore.Authorization.Authorize(Policy = WmsPermissions.UserManage)]", usersController, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Roles = \"Admin,Manager,Staff\")]", accountController, StringComparison.Ordinal);
        Assert.Contains("TrustedDevices", accountController, StringComparison.Ordinal);
        Assert.Contains("RevokeCurrentTrustedDevice", accountController, StringComparison.Ordinal);
        Assert.Contains("RevokeAllTrustedDevices", accountController, StringComparison.Ordinal);

        Assert.Contains("[Authorize(Roles = \"Admin,Manager\")]", inboundController, StringComparison.Ordinal);
        Assert.Contains("WmsPermissions.VoucherApproveInbound", inboundController, StringComparison.Ordinal);
        Assert.Contains("WmsPermissions.VoucherReleasePicking", outboundController, StringComparison.Ordinal);
        Assert.Contains("WmsPermissions.VoucherPostOutbound", outboundController, StringComparison.Ordinal);
    }

    [Fact]
    public void CatchWeightCapture_ShouldUseStableIdempotencyKey()
    {
        var root = FindRepositoryRoot();
        var controller = ReadUtf8(Path.Combine(root, "Controllers", "VouchersController.CatchWeight.cs"));

        Assert.DoesNotContain("DateTime.UtcNow.Ticks", controller, StringComparison.Ordinal);
        Assert.Contains("BuildCatchWeightIdempotencyKey", controller, StringComparison.Ordinal);
        Assert.Contains("CultureInfo.InvariantCulture", controller, StringComparison.Ordinal);
        Assert.Contains("Không tìm thấy dòng phiếu cần ghi nhận cân trọng lượng thực tế.", controller, StringComparison.Ordinal);
        Assert.Contains("Đã ghi nhận cân trọng lượng thực tế.", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void UserFacingOperationsLabels_ShouldUseClearVietnameseTerms()
    {
        var root = FindRepositoryRoot();
        var files = new[]
        {
            Path.Combine(root, "Views", "Shared", "_Layout.cshtml"),
            Path.Combine(root, "Views", "Home", "Index.cshtml"),
            Path.Combine(root, "Views", "Operations", "ThreePlContracts.cshtml"),
            Path.Combine(root, "Views", "Operations", "ThreePlClientPortal.cshtml"),
            Path.Combine(root, "Views", "Operations", "ThreePlBillingRuns.cshtml"),
            Path.Combine(root, "Views", "Operations", "ThreePlBillingRates.cshtml"),
            Path.Combine(root, "Views", "Operations", "ThreePlInvoiceDetails.cshtml"),
            Path.Combine(root, "Views", "Operations", "KittingWorkOrders.cshtml"),
            Path.Combine(root, "Views", "Operations", "CreateKittingWorkOrder.cshtml"),
            Path.Combine(root, "Views", "Operations", "KittingWorkOrderDetails.cshtml"),
            Path.Combine(root, "Views", "Operations", "OptimizationDashboard.cshtml"),
            Path.Combine(root, "Views", "Operations", "IntegrationDashboard.cshtml"),
            Path.Combine(root, "Views", "Operations", "LaborProductivity.cshtml"),
            Path.Combine(root, "Views", "Operations", "WorkflowProfiles.cshtml"),
            Path.Combine(root, "Views", "Reports", "SemanticBi.cshtml"),
            Path.Combine(root, "Views", "Reports", "AiAssistant.cshtml"),
            Path.Combine(root, "Views", "System", "SreDashboard.cshtml")
        };

        var uiText = string.Join(Environment.NewLine, files.Select(ReadUtf8));

        foreach (var required in new[]
        {
            "Hợp đồng kho nhiều chủ hàng",
            "Khu vực chủ hàng",
            "Phiếu lắp bộ hàng",
            "Báo cáo quản trị dữ liệu",
            "Giám sát vận hành hệ thống",
            "Tối ưu vận hành",
            "Tích hợp hệ thống",
            "Cam kết dịch vụ"
        })
        {
            Assert.Contains(required, uiText, StringComparison.Ordinal);
        }

        foreach (var banned in new[]
        {
            "Hợp đồng 3PL",
            "Portal chủ hàng",
            "Lệnh ráp bộ",
            "minimum charge",
            "service/unit/tier",
            "rating engine",
            "SLA billing",
            "Semantic BI",
            "BI semantic",
            "SRE vận hành",
            "Cấu hình workflow",
            "Thêm hoặc cập nhật profile",
            "Module gợi ý",
            "<th>Module</th>",
            "<th>Tên profile</th>",
            "<th>Scan</th>",
            "<th>QC</th>",
            "Webhook deliveries",
            "Outbox gần nhất",
            "Recommendation</h2>",
            "Chưa có recommendation",
            "Task waveless đã phát",
            "Distance saved",
            "Tote cluster"
        })
        {
            Assert.DoesNotContain(banned, uiText, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void AdvancedOperationsUi_ShouldUseSafeRoutesVietnameseLabelsAndResponsivePatterns()
    {
        var root = FindRepositoryRoot();
        var layout = ReadUtf8(Path.Combine(root, "Views", "Shared", "_Layout.cshtml"));
        var optimization = ReadUtf8(Path.Combine(root, "Views", "Operations", "OptimizationDashboard.cshtml"));
        var automation = ReadUtf8(Path.Combine(root, "Views", "Operations", "AutomationDashboard.cshtml"));
        var integration = ReadUtf8(Path.Combine(root, "Views", "Operations", "IntegrationDashboard.cshtml"));
        var workflow = ReadUtf8(Path.Combine(root, "Views", "Operations", "WorkflowProfiles.cshtml"));
        var dock = ReadUtf8(Path.Combine(root, "Views", "Operations", "DockBoard.cshtml"));
        var yard = ReadUtf8(Path.Combine(root, "Views", "Operations", "YardManagement.cshtml"));
        var labels = ReadUtf8(Path.Combine(root, "ViewModels", "EnterpriseUiLabels.cs"));
        var css = ReadUtf8(Path.Combine(root, "wwwroot", "css", "site.css"));

        Assert.DoesNotContain("Chuyển thẳngOpportunities", layout, StringComparison.Ordinal);
        Assert.Contains("asp-action=\"CrossDockOpportunities\"", layout, StringComparison.Ordinal);
        Assert.Contains("\"CrossDockOpportunities\",\"Chuyển thẳng\"", layout, StringComparison.Ordinal);

        foreach (var view in new[] { automation, integration, workflow })
        {
            Assert.DoesNotContain("<option value=\"@value\">@value</option>", view, StringComparison.Ordinal);
            Assert.DoesNotContain("BI semantic", view, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(">Workflow<", view, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var banned in new[]
        {
            "Conveyor / Sorter / AMR",
            "OK / Down",
            ">@adapter.AdapterType<",
            ">@adapter.HealthStatus<",
            ">@command.Status<",
            ">@e.TelemetryType<",
            ">@e.StatusText<",
            ">@a.Status<",
            ">@a.Direction<",
            ">@e.EvidenceType<",
            ">@d.EventType<",
            ">@o.EventType<"
        })
        {
            Assert.DoesNotContain(banned, string.Join('\n', automation, integration, dock, yard), StringComparison.Ordinal);
        }

        foreach (var required in new[]
        {
            "EnterpriseUiLabels.AutomationScenario",
            "EnterpriseUiLabels.AutomationTelemetryType",
            "EnterpriseUiLabels.IntegrationEvent",
            "EnterpriseUiLabels.DockAppointmentStatus",
            "EnterpriseUiLabels.YardEvidenceType",
            "EnterpriseUiLabels.OptimizationStatus",
            "enterprise-command-strip",
            "enterprise-workbench",
            "enterprise-form-card"
        })
        {
            Assert.Contains(required, string.Join('\n', optimization, automation, integration, workflow, dock, yard, labels, css), StringComparison.Ordinal);
        }
    }

    private static string ReadUtf8(string path)
        => File.ReadAllText(path, Encoding.UTF8);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "WMS.csproj"))
                && Directory.Exists(Path.Combine(directory.FullName, "WMS.Tests")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Khong tim thay thu muc goc du an WMS.");
    }
}
