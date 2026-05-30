import { defineConfig } from '@playwright/test';
import { existsSync } from 'node:fs';

const baseURL = process.env.WMS_BASE_URL;
const defaultStorageState = 'tests/visual/.auth/wms-auth-state.json';
const storageState = process.env.WMS_AUTH_STATE || (existsSync(defaultStorageState) ? defaultStorageState : undefined);

if (!baseURL) {
  throw new Error('WMS_BASE_URL is required for mobile deep visual audit.');
}

if (!storageState) {
  throw new Error('WMS_AUTH_STATE is required, or run npm run visual:auth to create tests/visual/.auth/wms-auth-state.json.');
}

const mobileUserAgent = 'Mozilla/5.0 (Linux; Android 14; WMS-Mobile-Deep) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Mobile Safari/537.36';

export default defineConfig({
  testDir: '.',
  testMatch: 'wms-mobile-deep.spec.ts',
  timeout: 55_000,
  expect: { timeout: 8_000 },
  outputDir: '../../artifacts/visual-mobile-deep/test-results',
  reporter: [
    ['list'],
    ['html', { outputFolder: '../../artifacts/visual-mobile-deep/playwright-report', open: 'never' }]
  ],
  use: {
    baseURL,
    storageState,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    actionTimeout: 12_000,
    navigationTimeout: 30_000
  },
  projects: [
    {
      name: 'phone-small-360',
      use: {
        viewport: { width: 360, height: 740 },
        isMobile: true,
        hasTouch: true,
        deviceScaleFactor: 3,
        userAgent: mobileUserAgent
      }
    },
    {
      name: 'phone-pixel-390',
      use: {
        viewport: { width: 390, height: 844 },
        isMobile: true,
        hasTouch: true,
        deviceScaleFactor: 2.75,
        userAgent: mobileUserAgent
      }
    },
    {
      name: 'phone-large-430',
      use: {
        viewport: { width: 430, height: 932 },
        isMobile: true,
        hasTouch: true,
        deviceScaleFactor: 3,
        userAgent: mobileUserAgent
      }
    },
    {
      name: 'tablet-portrait-768',
      use: {
        viewport: { width: 768, height: 1024 },
        isMobile: true,
        hasTouch: true,
        deviceScaleFactor: 2,
        userAgent: mobileUserAgent
      }
    }
  ]
});
