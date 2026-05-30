import { expect, Page, test, TestInfo } from '@playwright/test';

type RouteTarget = {
  name: string;
  path: string;
  strictOverflow?: boolean;
};

type DerivedRouteTarget = {
  name: string;
  listPath: string;
  selector: string;
};

const staticRoutes: RouteTarget[] = [
  { name: 'home', path: '/' },
  { name: 'help', path: '/Help' },
  { name: 'trusted-devices', path: '/Account/TrustedDevices' },
  { name: 'users', path: '/Users' },
  { name: 'login-help-requests', path: '/Users/LoginHelpRequests' },
  { name: 'items', path: '/Items' },
  { name: 'item-create', path: '/Items/Create' },
  { name: 'categories', path: '/Categories' },
  { name: 'category-create', path: '/Categories/Create' },
  { name: 'partners', path: '/Partners' },
  { name: 'partner-create', path: '/Partners/Create' },
  { name: 'warehouses', path: '/Warehouses' },
  { name: 'warehouse-create', path: '/Warehouses/Create' },
  { name: 'inventory-map', path: '/Warehouses/InventoryMap' },
  { name: 'units', path: '/Units' },
  { name: 'vouchers', path: '/Vouchers' },
  { name: 'voucher-create-inbound', path: '/Vouchers/Create?type=NhapKho' },
  { name: 'voucher-create-outbound', path: '/Vouchers/Create?type=XuatKho' },
  { name: 'voucher-create-transfer', path: '/Vouchers/Create?type=ChuyenKho' },
  { name: 'wave-planning', path: '/Vouchers/WavePlanning' },
  { name: 'receiving', path: '/Operations/Receiving' },
  { name: 'rf-receiving', path: '/Operations/RfReceiving' },
  { name: 'pick-tasks', path: '/Operations/PickTasks' },
  { name: 'rf-picking', path: '/Operations/RfPicking' },
  { name: 'rf-movement', path: '/Operations/RfMovement' },
  { name: 'movement-tasks', path: '/Operations/MovementTasks' },
  { name: 'next-task', path: '/Operations/NextTask' },
  { name: 'lpn-lookup', path: '/Operations/LpnLookup' },
  { name: 'serial-lookup', path: '/Operations/SerialLookup' },
  { name: 'package-lookup', path: '/Operations/PackageLookup' },
  { name: 'quality-inspection', path: '/Operations/QualityInspection' },
  { name: 'inbound-approvals', path: '/Operations/InboundApprovals' },
  { name: 'shipping', path: '/Operations/Shipping' },
  { name: 'shipping-dispatch', path: '/Operations/ShippingDispatch' },
  { name: 'shipment-loads', path: '/Operations/ShipmentLoads' },
  { name: 'delivery-reconciliation', path: '/Operations/DeliveryReconciliation' },
  { name: 'waves', path: '/Operations/Waves' },
  { name: 'zone-assignment', path: '/Operations/ZoneAssignment' },
  { name: 'dock-board', path: '/Operations/DockBoard' },
  { name: 'yard-management', path: '/Operations/YardManagement' },
  { name: 'yard-billing-rates', path: '/Operations/YardBillingRates' },
  { name: 'yard-billing-charges', path: '/Operations/YardBillingCharges' },
  { name: 'labor-productivity', path: '/Operations/LaborProductivity' },
  { name: 'cross-dock-opportunities', path: '/Operations/CrossDockOpportunities' },
  { name: 'replenishment', path: '/Operations/Replenishment' },
  { name: 'slotting', path: '/Operations/Slotting' },
  { name: 'slotting-simulation', path: '/Operations/SlottingSimulation' },
  { name: 'capacity-simulation', path: '/Operations/CapacitySimulation' },
  { name: 'optimization-dashboard', path: '/Operations/OptimizationDashboard' },
  { name: 'automation-dashboard', path: '/Operations/AutomationDashboard' },
  { name: 'integration-dashboard', path: '/Operations/IntegrationDashboard' },
  { name: 'carrier-connectors', path: '/Operations/CarrierConnectors' },
  { name: 'order-streaming-configs', path: '/Operations/OrderStreamingConfigs' },
  { name: 'sortation-configs', path: '/Operations/SortationConfigs' },
  { name: 'exception-center', path: '/Operations/ExceptionCenter' },
  { name: 'mhe-dashboard', path: '/Operations/MheDashboard' },
  { name: 'tenant-owner-scopes', path: '/Operations/TenantOwnerScopes' },
  { name: 'kitting-work-orders', path: '/Operations/KittingWorkOrders' },
  { name: 'create-kitting-work-order', path: '/Operations/CreateKittingWorkOrder' },
  { name: 'vas-work-orders', path: '/Operations/VasWorkOrders' },
  { name: 'create-vas-work-order', path: '/Operations/CreateVasWorkOrder' },
  { name: 'three-pl-runs', path: '/Operations/ThreePlBillingRuns' },
  { name: 'three-pl-rates', path: '/Operations/ThreePlBillingRates' },
  { name: 'three-pl-contracts', path: '/Operations/ThreePlContracts' },
  { name: 'three-pl-client-portal', path: '/Operations/ThreePlClientPortal' },
  { name: 'workflow-profiles', path: '/Operations/WorkflowProfiles' },
  { name: 'labels', path: '/Labels' },
  { name: 'label-templates', path: '/Labels/Templates' },
  { name: 'label-template-create', path: '/Labels/CreateTemplate' },
  { name: 'label-item-rules', path: '/Labels/ItemRules' },
  { name: 'label-print-jobs', path: '/Labels/PrintJobs' },
  { name: 'inventory', path: '/Reports/Inventory' },
  { name: 'stock-movement', path: '/Reports/StockMovement' },
  { name: 'inventory-transactions', path: '/Reports/InventoryTransactions' },
  { name: 'stock-valuation', path: '/Reports/StockValuation' },
  { name: 'stock-snapshot', path: '/Reports/StockSnapshot' },
  { name: 'stock-count', path: '/Reports/StockCount' },
  { name: 'period-locks', path: '/Reports/PeriodLocks' },
  { name: 'alerts', path: '/Reports/Alerts' },
  { name: 'ops-kpi', path: '/Reports/OpsKpi' },
  { name: 'top-items', path: '/Reports/TopItems' },
  { name: 'expiry-report', path: '/Reports/ExpiryReport' },
  { name: 'slow-moving-report', path: '/Reports/SlowMovingReport' },
  { name: 'abc-analysis', path: '/Reports/AbcAnalysis' },
  { name: 'analytics', path: '/Reports/Analytics' },
  { name: 'space-utilization', path: '/Reports/SpaceUtilization' },
  { name: 'dock-to-stock', path: '/Reports/DockToStock' },
  { name: 'audit-trail', path: '/Reports/AuditTrail' },
  { name: 'audit-analytics', path: '/Reports/AuditAnalytics' },
  { name: 'scheduled-reports', path: '/Reports/ScheduledReports' },
  { name: 'semantic-bi', path: '/Reports/SemanticBi' },
  { name: 'financial-cost-dashboard', path: '/Reports/FinancialCostDashboard' },
  { name: 'predictive-alerts', path: '/Reports/PredictiveAlerts' },
  { name: 'ai-assistant', path: '/Reports/AiAssistant' },
  { name: 'sre-dashboard', path: '/System/SreDashboard' }
];

const derivedRoutes: DerivedRouteTarget[] = [
  { name: 'item-details-first', listPath: '/Items', selector: 'a[href*="/Items/Details/"]' },
  { name: 'voucher-details-first', listPath: '/Vouchers', selector: 'a[href*="/Vouchers/Details/"]' },
  { name: 'warehouse-details-first', listPath: '/Warehouses', selector: 'a[href*="/Warehouses/Details/"]' },
  { name: 'shipment-load-details-first', listPath: '/Operations/ShipmentLoads', selector: 'a[href*="/Operations/ShipmentLoadDetails"]' },
  { name: 'three-pl-run-details-first', listPath: '/Operations/ThreePlBillingRuns', selector: 'a[href*="/Operations/ThreePlBillingRunDetails"]' },
  { name: 'kitting-work-order-details-first', listPath: '/Operations/KittingWorkOrders', selector: 'a[href*="/Operations/KittingWorkOrderDetails"]' },
  { name: 'vas-work-order-details-first', listPath: '/Operations/VasWorkOrders', selector: 'a[href*="/Operations/VasWorkOrderDetails"]' }
];

const textFromCodePoints = (...points: number[]) => String.fromCodePoint(...points);
const mojibakeTokens = [
  textFromCodePoints(0x00c3, 0x0192),
  textFromCodePoints(0x00c3, 0x201e),
  textFromCodePoints(0x00c3, 0x2020),
  textFromCodePoints(0x00c3, 0x00a1, 0x00c2, 0x00ba),
  textFromCodePoints(0x00c3, 0x00a1, 0x00c2, 0x00bb),
  textFromCodePoints(0x00c3, 0x201a)
];
const allowedHorizontalScrollContainers = [
  '.table-container',
  '.table-responsive',
  '.enterprise-table-wrap',
  '.yardops-table-wrap',
  '.mobile-table-card-list',
  '.mobile-table-card-source',
  '.voucher-mobile-line-editor',
  '.inventory-map-warehouse-strip',
  '.inbound-progress',
  '.leaflet-container',
  '.leaflet-pane',
  '.yardops-two-column > div',
  '.print-page',
  '.print-sheet',
  '.label-page',
  '.no-enterprise-enhance'
];

function routeLabel(route: RouteTarget, testInfo: TestInfo) {
  return `${route.name} (${testInfo.project.name})`;
}

function isSameOriginAsset(page: Page, url: string) {
  try {
    return new URL(url).origin === new URL(page.url()).origin;
  } catch {
    return false;
  }
}

async function gotoAudited(page: Page, route: RouteTarget, testInfo: TestInfo) {
  const consoleErrors: string[] = [];
  const serverErrors: string[] = [];
  const onConsole = (message: { type(): string; text(): string }) => {
    if (message.type() === 'error') consoleErrors.push(message.text());
  };
  const onResponse = (response: { status(): number; url(): string }) => {
    if (response.status() >= 500 && isSameOriginAsset(page, response.url())) {
      serverErrors.push(`${response.status()} ${response.url()}`);
    }
  };

  page.on('console', onConsole);
  page.on('response', onResponse);
  try {
    const response = await page.goto(route.path, { waitUntil: 'networkidle' });
    expect(response?.status() ?? 200, `${routeLabel(route, testInfo)} HTTP status`).toBeLessThan(400);
    await expect(page.locator('body'), `${routeLabel(route, testInfo)} body`).toBeVisible();
    await expect(page.locator('.main-content, body').first(), `${routeLabel(route, testInfo)} main shell`).toBeVisible();
    expect(serverErrors, `${routeLabel(route, testInfo)} same-origin 5xx responses`).toEqual([]);
    expect(consoleErrors, `${routeLabel(route, testInfo)} console errors`).toEqual([]);
  } finally {
    page.off('console', onConsole);
    page.off('response', onResponse);
  }
}

async function assertNoVisibleMojibake(page: Page, context: string) {
  const text = await page.locator('body').evaluate((body) => body.textContent || '');
  const hits = mojibakeTokens.filter((token) => text.includes(token));
  expect(hits, `${context} visible text contains mojibake markers`).toEqual([]);
}

async function assertHorizontalOverflowIsContained(page: Page, route: RouteTarget, testInfo: TestInfo) {
  const result = await page.evaluate((selectors) => {
    const viewportWidth = window.innerWidth;
    const globalOverflow = Math.max(0, document.documentElement.scrollWidth - viewportWidth);
    const offenders = Array.from(document.body.querySelectorAll<HTMLElement>('*'))
      .filter((element) => {
        const style = window.getComputedStyle(element);
        if (style.display === 'none' || style.visibility === 'hidden' || style.position === 'fixed') return false;
        if (element.closest('.sidebar') && !element.closest('.sidebar.open')) return false;
        const rect = element.getBoundingClientRect();
        if (rect.width <= 1 || rect.height <= 1) return false;
        const outsideViewport = rect.right > viewportWidth + 16 || rect.left < -16;
        if (!outsideViewport) return false;
        return !selectors.some((selector) => element.closest(selector));
      })
      .slice(0, 8)
      .map((element) => ({
        tag: element.tagName.toLowerCase(),
        className: element.className ? String(element.className).slice(0, 120) : '',
        id: element.id || '',
        text: (element.textContent || '').trim().slice(0, 80),
        width: Math.round(element.getBoundingClientRect().width)
      }));
    return { globalOverflow, offenders };
  }, allowedHorizontalScrollContainers);

  if (route.strictOverflow !== false) {
    expect(result.globalOverflow, `${routeLabel(route, testInfo)} document horizontal overflow`).toBeLessThanOrEqual(16);
  }
  expect(result.offenders, `${routeLabel(route, testInfo)} uncontained horizontal overflow offenders`).toEqual([]);
}

async function assertFixedChromeDoesNotCoverActions(page: Page, route: RouteTarget, testInfo: TestInfo) {
  const collisions = await page.evaluate(() => {
    function visible(element: Element) {
      const html = element as HTMLElement;
      const style = window.getComputedStyle(html);
      const rect = html.getBoundingClientRect();
      return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 1 && rect.height > 1;
    }

    function intersects(a: DOMRect, b: DOMRect) {
      const x = Math.max(0, Math.min(a.right, b.right) - Math.max(a.left, b.left));
      const y = Math.max(0, Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top));
      return x * y;
    }

    const fixedElements = Array.from(document.body.querySelectorAll<HTMLElement>('*'))
      .filter((element) => visible(element) && window.getComputedStyle(element).position === 'fixed');
    const actionTargets = Array.from(document.body.querySelectorAll<HTMLElement>(
      '.main-content button:not([disabled]), .main-content a.btn, .action-bar button:not([disabled]), .rf-scan-input, .scanner-modal button:not([disabled]), .modal-footer button:not([disabled])'
    )).filter((element) => visible(element) && !element.closest('.app-topbar, .sidebar, .mobile-quick-dock'));

    const collisions: string[] = [];
    for (const fixed of fixedElements) {
      for (const target of actionTargets) {
        if (fixed.contains(target) || target.contains(fixed)) continue;
        const area = intersects(fixed.getBoundingClientRect(), target.getBoundingClientRect());
        if (area > 24) {
          collisions.push(`${fixed.className || fixed.id || fixed.tagName} covers ${target.className || target.id || target.tagName}`);
        }
      }
    }
    return collisions.slice(0, 8);
  });

  expect(collisions, `${routeLabel(route, testInfo)} fixed chrome/action collisions`).toEqual([]);
}

async function assertTapTargetsAndButtonTextFit(page: Page, route: RouteTarget, testInfo: TestInfo) {
  const result = await page.evaluate(() => {
    function visible(element: Element) {
      const html = element as HTMLElement;
      const style = window.getComputedStyle(html);
      const rect = html.getBoundingClientRect();
      return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 1 && rect.height > 1;
    }

    const candidates = Array.from(document.body.querySelectorAll<HTMLElement>(
      '.main-content button:not([disabled]), .main-content a.btn, .mobile-quick-link, .scanner-modal button:not([disabled])'
    )).filter((element) => visible(element) && !element.closest('.data-table, table, .pagination, .topbar-user-menu'));

    const smallTargets = candidates
      .filter((element) => {
        const rect = element.getBoundingClientRect();
        const iconOnly = element.textContent?.trim().length === 0 || element.getAttribute('aria-label');
        const minimum = iconOnly ? 36 : 40;
        return rect.height < minimum || rect.width < minimum;
      })
      .slice(0, 8)
      .map((element) => `${element.tagName.toLowerCase()}.${String(element.className).slice(0, 80)} "${(element.textContent || element.getAttribute('aria-label') || '').trim().slice(0, 40)}"`);

    const textOverflow = candidates
      .filter((element) => element.scrollWidth > element.clientWidth + 2 || element.scrollHeight > element.clientHeight + 2)
      .slice(0, 8)
      .map((element) => `${element.tagName.toLowerCase()}.${String(element.className).slice(0, 80)} "${(element.textContent || '').trim().slice(0, 40)}"`);

    return { smallTargets, textOverflow };
  });

  expect(result.smallTargets, `${routeLabel(route, testInfo)} small tap targets`).toEqual([]);
  expect(result.textOverflow, `${routeLabel(route, testInfo)} button text overflow`).toEqual([]);
}

async function auditMobileRoute(page: Page, route: RouteTarget, testInfo: TestInfo) {
  await gotoAudited(page, route, testInfo);
  await assertNoVisibleMojibake(page, routeLabel(route, testInfo));
  await assertHorizontalOverflowIsContained(page, route, testInfo);
  await assertFixedChromeDoesNotCoverActions(page, route, testInfo);
  await assertTapTargetsAndButtonTextFit(page, route, testInfo);
}

for (const route of staticRoutes) {
  test(`${route.name} mobile deep audit`, async ({ page }, testInfo) => {
    await auditMobileRoute(page, route, testInfo);
  });
}

for (const target of derivedRoutes) {
  test(`${target.name} mobile deep audit`, async ({ page }, testInfo) => {
    const listRoute = { name: `${target.name}-list`, path: target.listPath };
    await gotoAudited(page, listRoute, testInfo);
    const detailLink = page.locator(target.selector).first();
    const href = await detailLink.count() ? await detailLink.getAttribute('href') : null;
    if (!href) {
      await testInfo.attach(`${target.name}-no-seed-data`, {
        body: `${target.listPath} rendered, but no link matched ${target.selector}.`,
        contentType: 'text/plain'
      });
      return;
    }

    await auditMobileRoute(page, { name: target.name, path: href }, testInfo);
  });
}

test('mobile shell drawer and quick dock do not occlude content', async ({ page }, testInfo) => {
  await auditMobileRoute(page, { name: 'mobile-shell-home', path: '/' }, testInfo);

  const toggle = page.locator('#sidebarToggle');
  await expect(toggle).toBeVisible();
  await toggle.click();
  await expect(page.locator('#sidebar')).toHaveClass(/open/);
  await expect(page.locator('#sidebarOverlay')).toHaveClass(/active/);

  const sidebarBox = await page.locator('#sidebar').boundingBox();
  const viewport = page.viewportSize();
  expect(sidebarBox?.width ?? 0, `${testInfo.project.name} sidebar width`).toBeLessThanOrEqual(viewport?.width ?? 768);

  await page.mouse.click((viewport?.width ?? 768) - 18, 120);
  await expect(page.locator('#sidebar')).not.toHaveClass(/open/);

  await auditMobileRoute(page, { name: 'mobile-shell-rf-quick-dock', path: '/Operations/RfReceiving' }, testInfo);
  const quickDock = page.locator('.mobile-quick-dock');
  await expect(quickDock).toBeVisible();
  const dockBox = await quickDock.boundingBox();
  expect(dockBox?.left ?? 0, `${testInfo.project.name} quick dock left`).toBeGreaterThanOrEqual(0);
  expect(dockBox?.right ?? 0, `${testInfo.project.name} quick dock right`).toBeLessThanOrEqual(viewport?.width ?? 768);
  await expect(page.locator('.mobile-quick-link').first()).toBeVisible();
});
