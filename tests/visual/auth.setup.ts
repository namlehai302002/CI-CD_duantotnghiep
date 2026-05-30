import { expect, test as setup } from '@playwright/test';
import { existsSync, readFileSync } from 'node:fs';

const authStatePath = process.env.WMS_AUTH_STATE || 'tests/visual/.auth/wms-auth-state.json';
const baseUrl = process.env.WMS_BASE_URL || '';
let testUser = process.env.WMS_TEST_USER;
let testPassword = process.env.WMS_TEST_PASSWORD;

function isLoopbackBaseUrl(value: string): boolean {
  if (!value) return false;

  try {
    const url = new URL(value);
    const host = url.hostname.toLowerCase();
    const loopbackHostName = 'local' + 'host';
    return host === loopbackHostName
      || host === '::1'
      || host === '[::1]'
      || host === '0:0:0:0:0:0:0:1'
      || host.startsWith('127.');
  } catch {
    return false;
  }
}

if ((!testUser || !testPassword) && existsSync('appsettings.json') && isLoopbackBaseUrl(baseUrl)) {
  const appsettings = JSON.parse(readFileSync('appsettings.json', 'utf-8'));
  const localVerification = appsettings.LocalVerification;
  if (localVerification?.Enabled) {
    testUser ||= localVerification.UserName;
    testPassword ||= localVerification.Password;
  }
}

if (!testUser || !testPassword) {
  throw new Error('WMS_TEST_USER and WMS_TEST_PASSWORD are required. LocalVerification fallback is allowed only when WMS_BASE_URL is a loopback URL.');
}

setup('create authenticated WMS storage state', async ({ page }) => {
  await page.goto('/Account/Login', { waitUntil: 'networkidle' });
  await page.locator('input[name="UserName"]').fill(testUser);
  await page.locator('input[name="Password"]').fill(testPassword);
  await page.locator('button[type="submit"]').click();
  await page.waitForLoadState('networkidle');

  if (page.url().includes('/Account/VerifyMfa')) {
    throw new Error('Visual auth setup reached MFA. Use a pre-created WMS_AUTH_STATE for MFA accounts, or use a dedicated test account/session.');
  }

  await expect(page.locator('body')).toBeVisible();
  await page.context().storageState({ path: authStatePath });
});
