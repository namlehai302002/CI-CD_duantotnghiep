import { expect, test } from '@playwright/test';

const routes = [
  { name: 'home', path: '/' },
  { name: 'help', path: '/Help' },
  { name: 'users', path: '/Users' },
  { name: 'voucher-create', path: '/Vouchers/Create?type=NhapKho' },
  { name: 'receiving', path: '/Operations/Receiving' },
  { name: 'rf-receiving', path: '/Operations/RfReceiving' },
  { name: 'picking', path: '/Operations/PickTasks' },
  { name: 'rf-picking', path: '/Operations/RfPicking' },
  { name: 'inventory', path: '/Reports/Inventory' },
  { name: 'exception-center', path: '/Operations/ExceptionCenter' },
  { name: 'yard-management', path: '/Operations/YardManagement' },
  { name: 'dock-board', path: '/Operations/DockBoard' },
  { name: 'optimization-dashboard', path: '/Operations/OptimizationDashboard' },
  { name: 'automation-dashboard', path: '/Operations/AutomationDashboard' },
  { name: 'integration-dashboard', path: '/Operations/IntegrationDashboard' },
  { name: 'carrier-connectors', path: '/Operations/CarrierConnectors' },
  { name: 'delivery-reconciliation', path: '/Operations/DeliveryReconciliation' },
  { name: 'three-pl-runs', path: '/Operations/ThreePlBillingRuns' },
  { name: 'three-pl-rates', path: '/Operations/ThreePlBillingRates' },
  { name: 'semantic-bi', path: '/Reports/SemanticBi' },
  { name: 'predictive-alerts', path: '/Reports/PredictiveAlerts' },
  { name: 'ai-assistant', path: '/Reports/AiAssistant' },
  { name: 'workflow-profiles', path: '/Operations/WorkflowProfiles' },
  { name: 'sre-dashboard', path: '/System/SreDashboard' }
];

const zoomByProject: Record<string, number> = {
  'desktop-100': 1,
  'desktop-110': 1.1,
  'desktop-125': 1.25,
  mobile: 1
};

function screenshotMasks(page, routeName: string) {
  if (routeName !== 'sre-dashboard') return [];
  return [
    page.locator('.metric-grid'),
    page.locator('.enterprise-section').nth(0),
    page.locator('.yardops-two-column')
  ];
}

async function stabilizeRouteForScreenshot(page, routeName: string) {
  if (routeName === 'sre-dashboard') {
    await page.addStyleTag({
      content: `
        .metric-grid { min-height: 128px !important; max-height: 128px !important; overflow: hidden !important; }
        .enterprise-section { min-height: 340px !important; max-height: 340px !important; overflow: hidden !important; }
        .yardops-two-column { min-height: 220px !important; max-height: 220px !important; overflow: hidden !important; }
      `
    });
  }

  if (routeName === 'receiving') {
    await page.addStyleTag({
      content: `
        .table-container tbody tr:nth-child(n+9) { display: none !important; }
        .table-container { max-height: 1180px !important; overflow: hidden !important; }
      `
    });
  }

  if (routeName === 'dock-board') {
    await page.addStyleTag({
      content: `
        .dock-clock { visibility: hidden !important; }
        .dock-appt:nth-child(n+2) { display: none !important; }
        .yardops-table tbody tr:nth-child(n+7) { display: none !important; }
      `
    });
  }

  if (routeName === 'exception-center') {
    await page.addStyleTag({
      content: `
        .exception-center-table-container {
          height: 1180px !important;
          min-height: 1180px !important;
          max-height: 1180px !important;
          overflow: hidden !important;
        }
        .exception-center-table tbody tr:nth-child(n+6) { display: none !important; }
        .exception-center-table th:nth-child(7),
        .exception-center-table td:nth-child(7) { visibility: hidden !important; }
        @media (max-width: 700px) {
          .exception-center-table-container {
            height: 1540px !important;
            min-height: 1540px !important;
            max-height: 1540px !important;
          }
        }
      `
    });
  }
}

for (const route of routes) {
  test(`${route.name} renders without layout collision`, async ({ page }, testInfo) => {
    await page.goto(route.path, { waitUntil: 'networkidle' });
    const zoom = zoomByProject[testInfo.project.name] ?? 1;
    await page.addStyleTag({ content: `html { zoom: ${zoom}; }` });
    await stabilizeRouteForScreenshot(page, route.name);
    await expect(page.locator('body')).toBeVisible();

    if (testInfo.project.name === 'mobile') {
      const overflow = await page.evaluate(() => Math.max(0, document.documentElement.scrollWidth - window.innerWidth));
      expect(overflow).toBeLessThanOrEqual(24);
      await expect(page.locator('.page-title, h1').first()).toBeVisible();
      const primaryAction = page.locator('.btn-primary:visible, .page-actions .btn:visible, .mobile-quick-link:visible').first();
      if (await primaryAction.count()) {
        await expect(primaryAction).toBeVisible();
      }
    }

    await expect(page).toHaveScreenshot(`${route.name}-${testInfo.project.name}.png`, {
      fullPage: true,
      mask: screenshotMasks(page, route.name)
    });
  });
}

test('collapsed sidebar keeps enterprise rail groups and flyouts', async ({ page }, testInfo) => {
  test.skip(testInfo.project.name === 'mobile', 'Mobile uses drawer navigation instead of desktop mini rail.');

  await page.goto('/', { waitUntil: 'networkidle' });
  await page.evaluate(() => localStorage.removeItem('wms_sidebar_collapsed'));
  await page.reload({ waitUntil: 'networkidle' });
  const zoom = zoomByProject[testInfo.project.name] ?? 1;
  await page.addStyleTag({ content: `html { zoom: ${zoom}; }` });
  const body = page.locator('body');
  if (!(await body.evaluate((element) => element.classList.contains('sidebar-collapsed')))) {
    await page.locator('#sidebarToggle').click();
  }
  await expect(body).toHaveClass(/sidebar-collapsed/);

  for (const label of ['Trang chính', 'Nhập kho', 'Xuất kho', 'Tồn kho', 'Tra cứu phiếu', 'Báo cáo', 'Danh mục', 'Hệ thống', 'Hướng dẫn sử dụng']) {
    await expect(page.locator(`.sidebar .nav-section[data-nav-label="${label}"]`).first()).toBeVisible();
  }

  for (const label of ['Nhập kho', 'Xuất kho', 'Tồn kho', 'Hệ thống']) {
    const group = page.locator(`.sidebar .nav-section[data-nav-label="${label}"]`).first();
    await group.locator('.nav-section-title').focus();
    await expect(group).toHaveClass(/flyout-open/);
    await expect(group.locator('.nav-section-body')).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(group).not.toHaveClass(/flyout-open/);
  }

  await expect(page).toHaveScreenshot(`collapsed-sidebar-${testInfo.project.name}.png`, {
    fullPage: true
  });
});

test('mobile scanner modal fits viewport', async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== 'mobile', 'Scanner fit is a mobile-only visual gate.');

  await page.goto('/Operations/RfReceiving', { waitUntil: 'networkidle' });
  await page.evaluate(() => {
    const modal = document.getElementById('scannerModal');
    if (modal) {
      modal.classList.add('active');
      modal.setAttribute('aria-hidden', 'false');
    }
  });

  const modal = page.locator('#scannerModal .scanner-modal').first();
  await expect(modal).toBeVisible();
  const box = await modal.boundingBox();
  const viewport = page.viewportSize();
  expect(box?.width ?? 0).toBeLessThanOrEqual(viewport?.width ?? 390);
  expect(box?.height ?? 0).toBeLessThanOrEqual(viewport?.height ?? 844);
});

test('voucher create keeps source unit available after item selection', async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== 'desktop-100', 'One authenticated desktop pass is enough for the UOM regression gate.');

  await page.goto('/Vouchers/Create?type=NhapKho', { waitUntil: 'networkidle' });
  const firstRow = page.locator('#linesContainer .line-row').first();
  const itemSelect = firstRow.locator('select.item-select');
  const firstItemValue = await itemSelect.locator('option:not([value=""]):not([disabled])').first().getAttribute('value');
  expect(firstItemValue, 'UOM regression gate requires at least one seeded selectable item').toBeTruthy();

  await itemSelect.selectOption(firstItemValue as string);
  await itemSelect.dispatchEvent('change');

  const sourceUom = firstRow.locator('select.source-uom-select');
  await expect.poll(async () => sourceUom.locator('option:not([value=""])').count()).toBeGreaterThan(0);
  await expect.poll(async () => sourceUom.inputValue()).not.toBe('');
});

test('enterprise toast is not covered by fixed topbar', async ({ page }, testInfo) => {
  test.skip(!['desktop-100', 'mobile'].includes(testInfo.project.name), 'Toast collision is checked on primary desktop and mobile only.');

  await page.goto('/', { waitUntil: 'networkidle' });
  await page.evaluate(() => {
    (window as any).enterpriseNotify?.({
      title: 'Kiểm tra thông báo nghiệp vụ',
      text: 'Toast phải nằm dưới thanh điều hướng và không bị che.',
      icon: 'error',
      timer: 6000
    });
  });

  const popup = page.locator('.swal2-popup').first();
  await expect(popup).toBeVisible();
  const popupBox = await popup.boundingBox();
  const headerBox = await page.locator('.app-topbar').boundingBox();
  expect(popupBox?.y ?? 0).toBeGreaterThanOrEqual((headerBox?.height ?? 56) + 4);
});
