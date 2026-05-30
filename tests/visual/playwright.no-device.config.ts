import { defineConfig, devices } from '@playwright/test';
import { existsSync } from 'node:fs';

const baseURL = process.env.WMS_BASE_URL;
const defaultStorageState = 'tests/visual/.auth/wms-auth-state.json';
const storageState = process.env.WMS_AUTH_STATE || (existsSync(defaultStorageState) ? defaultStorageState : undefined);

if (!baseURL) {
  throw new Error('WMS_BASE_URL is required for no-device RF/print evidence.');
}

if (!storageState) {
  throw new Error('WMS_AUTH_STATE is required, or run npm run visual:auth to create tests/visual/.auth/wms-auth-state.json.');
}

export default defineConfig({
  testDir: '.',
  testMatch: 'wms-no-device-evidence.spec.ts',
  timeout: 45_000,
  expect: { timeout: 8_000 },
  outputDir: '../../artifacts/visual-no-device/test-results',
  reporter: [
    ['list'],
    ['html', { outputFolder: '../../artifacts/visual-no-device/playwright-report', open: 'never' }]
  ],
  use: {
    baseURL,
    storageState,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure'
  },
  projects: [
    {
      name: 'desktop-no-device',
      use: { viewport: { width: 1440, height: 900 } }
    },
    {
      name: 'mobile-no-device',
      use: { ...devices['Pixel 7'], viewport: { width: 390, height: 844 } }
    }
  ]
});
