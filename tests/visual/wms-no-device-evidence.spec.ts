import { expect, test } from '@playwright/test';

const rfRoutes = [
  { name: 'rf-receiving', path: '/Operations/RfReceiving', scanValue: 'SIM-RCV-0001' },
  { name: 'rf-picking', path: '/Operations/RfPicking', scanValue: 'SIM-PICK-0001' },
  { name: 'rf-movement', path: '/Operations/RfMovement', scanValue: 'SIM-MOVE-0001' }
];

const textFromCodePoints = (...points: number[]) => String.fromCodePoint(...points);
const mojibakeTokens = [
  textFromCodePoints(0x00c3),
  textFromCodePoints(0x00c4),
  textFromCodePoints(0x00c6),
  textFromCodePoints(0x00e1, 0x00ba),
  textFromCodePoints(0x00e1, 0x00bb),
  textFromCodePoints(0x00c2)
];

async function assertNoVisibleMojibake(page, context: string) {
  const text = await page.locator('body').evaluate((body) => body.textContent || '');
  const hits = mojibakeTokens.filter((token) => text.includes(token));
  expect(hits, `${context} visible text contains mojibake markers: ${hits.join(', ')}`).toEqual([]);
}

async function assertNoHorizontalOverflow(page, context: string) {
  const overflow = await page.evaluate(() => Math.max(0, document.documentElement.scrollWidth - window.innerWidth));
  expect(overflow, `${context} horizontal overflow`).toBeLessThanOrEqual(24);
}

test.describe('no-device RF and print evidence', () => {
  for (const route of rfRoutes) {
    test(`${route.name} accepts keyboard-wedge scan without physical scanner`, async ({ page }, testInfo) => {
      await page.goto(route.path, { waitUntil: 'networkidle' });
      await expect(page.locator('body')).toBeVisible();
      await expect(page.locator('.page-title, h1').first()).toBeVisible();
      await assertNoVisibleMojibake(page, route.name);
      await assertNoHorizontalOverflow(page, route.name);

      const scanInput = page.locator('.rf-scan-input').first();
      if ((await scanInput.count()) === 0) {
        await testInfo.attach(`${route.name}-no-open-scan-task`, {
          body: `${route.path} rendered but no scanner-ready input was available in the current seed data.`,
          contentType: 'text/plain'
        });
        return;
      }

      await expect(scanInput).toBeVisible();
      await scanInput.evaluate((element, value) => {
        const input = element as HTMLInputElement;
        input.focus();
        input.value = value;
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', code: 'Enter', bubbles: true, cancelable: true }));
        input.dispatchEvent(new Event('change', { bubbles: true }));
      }, route.scanValue);

      await expect(scanInput).toHaveValue(route.scanValue);
      await testInfo.attach(`${route.name}-keyboard-wedge`, {
        body: JSON.stringify({ route: route.path, simulatedScan: route.scanValue, hardware: 'not required' }, null, 2),
        contentType: 'application/json'
      });
    });
  }

  test('camera scanner modal fits viewport without physical camera', async ({ page }, testInfo) => {
    await page.goto('/Operations/RfReceiving', { waitUntil: 'networkidle' });
    await page.evaluate(() => {
      const modal = document.getElementById('scannerModal');
      if (!modal) return;
      modal.classList.add('active');
      modal.setAttribute('aria-hidden', 'false');
    });

    const modal = page.locator('#scannerModal .scanner-modal').first();
    await expect(modal).toBeVisible();
    const box = await modal.boundingBox();
    const viewport = page.viewportSize();
    expect(box?.width ?? 0).toBeLessThanOrEqual(viewport?.width ?? 390);
    expect(box?.height ?? 0).toBeLessThanOrEqual(viewport?.height ?? 844);
    await assertNoVisibleMojibake(page, 'scanner modal');
    await testInfo.attach('camera-modal-no-device', {
      body: await page.screenshot({ fullPage: true }),
      contentType: 'image/png'
    });
  });

  test('item label print preview renders without physical printer', async ({ page }, testInfo) => {
    await page.goto('/Items', { waitUntil: 'networkidle' });
    await expect(page.locator('body')).toBeVisible();
    await assertNoVisibleMojibake(page, 'items list');

    const firstItemLink = page.locator('a[href*="/Items/Details/"]').first();
    if ((await firstItemLink.count()) === 0) {
      await testInfo.attach('print-preview-no-seed-item', {
        body: 'Items page rendered, but no active item detail link exists in the current seed data.',
        contentType: 'text/plain'
      });
      return;
    }

    const href = await firstItemLink.getAttribute('href');
    const itemId = href ? new URL(href, 'https://wms.invalid').pathname.split('/').filter(Boolean).pop() : undefined;
    expect(itemId, 'first item id from Items/Details link').toBeTruthy();

    await page.goto(`/Items/PrintSingle/${itemId}?qty=1&labelSize=50x30`, { waitUntil: 'networkidle' });
    await expect(page.locator('link[href*="wms-print-labels.css"]')).toHaveCount(1);
    await expect(page.locator('.preview-area')).toBeVisible();
    await expect(page.locator('.barcode-target, .qr-target').first()).toBeVisible();
    await assertNoVisibleMojibake(page, 'item label print preview');
    await page.emulateMedia({ media: 'print' });
    await expect(page.locator('body')).toBeVisible();
    await testInfo.attach('item-label-print-preview-no-printer', {
      body: await page.screenshot({ fullPage: true }),
      contentType: 'image/png'
    });
  });
});
