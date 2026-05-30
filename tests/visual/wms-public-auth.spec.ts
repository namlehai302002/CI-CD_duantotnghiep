import { expect, test } from '@playwright/test';
import { mkdirSync } from 'fs';
import { join } from 'path';

const routes = [
  { name: 'login', path: '/Account/Login' },
  { name: 'access-help', path: '/Account/AccessHelp' },
  { name: 'access-help-sent', path: '/Account/AccessHelpSent' }
];

const forbiddenUiTerms = [
  'HttpOnly',
  'cookie',
  'MFA',
  'Captcha',
  'audit',
  'session',
  'token',
  'API',
  'backend',
  'middleware',
  'encryption',
  'database',
  'hieuctttb01413',
  '0347681019'
];

for (const route of routes) {
  test(`${route.name} public auth screen is enterprise-ready`, async ({ page }, testInfo) => {
    await page.goto(route.path, { waitUntil: 'networkidle' });
    await expect(page.locator('body')).toBeVisible();

    const bodyText = await page.locator('body').innerText();
    for (const term of forbiddenUiTerms) {
      expect(bodyText.toLowerCase()).not.toContain(term.toLowerCase());
    }

    const overflow = await page.evaluate(() => Math.max(0, document.documentElement.scrollWidth - window.innerWidth));
    expect(overflow).toBeLessThanOrEqual(testInfo.project.name.includes('mobile') ? 24 : 0);

    const artifactDir = join('artifacts', 'visual-public', testInfo.project.name);
    mkdirSync(artifactDir, { recursive: true });
    await page.screenshot({
      path: join(artifactDir, `${route.name}.png`),
      fullPage: true
    });
  });
}
