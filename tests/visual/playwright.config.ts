import { defineConfig, devices } from '@playwright/test';
import { existsSync } from 'node:fs';

const baseURL = process.env.WMS_BASE_URL;
const defaultStorageState = 'tests/visual/.auth/wms-auth-state.json';
const storageState = process.env.WMS_AUTH_STATE || (existsSync(defaultStorageState) ? defaultStorageState : undefined);

if (!baseURL) {
  throw new Error('WMS_BASE_URL is required for visual regression.');
}

if (!storageState) {
  throw new Error('WMS_AUTH_STATE is required, or run npm run visual:auth to create tests/visual/.auth/wms-auth-state.json.');
}

export default defineConfig({
  testDir: '.',
  testMatch: 'wms-visual-regression.spec.ts',
  timeout: 60_000,
  expect: {
    timeout: 10_000,
    toHaveScreenshot: {
      maxDiffPixelRatio: 0.01
    }
  },
  use: {
    baseURL,
    storageState,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure'
  },
  outputDir: '../../test-results',
  projects: [
    {
      name: 'desktop-100',
      use: {
        viewport: { width: 1440, height: 900 }
      }
    },
    {
      name: 'desktop-110',
      use: {
        viewport: { width: 1440, height: 900 }
      }
    },
    {
      name: 'desktop-125',
      use: {
        viewport: { width: 1440, height: 900 }
      }
    },
    {
      name: 'mobile',
      use: {
        ...devices['Pixel 7'],
        viewport: { width: 390, height: 844 }
      }
    }
  ],
  reporter: [['list'], ['html', { outputFolder: '../../artifacts/visual-authenticated/playwright-report', open: 'never' }]]
});
