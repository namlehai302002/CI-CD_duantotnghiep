import { defineConfig, devices } from '@playwright/test';

const baseURL = process.env.WMS_BASE_URL;

if (!baseURL) {
  throw new Error('WMS_BASE_URL is required for public visual smoke tests.');
}

export default defineConfig({
  testDir: './',
  testMatch: 'wms-public-auth.spec.ts',
  timeout: 30_000,
  expect: { timeout: 5_000 },
  outputDir: '../../artifacts/visual-public/test-results',
  reporter: [
    ['list'],
    ['html', { outputFolder: '../../artifacts/visual-public/playwright-report', open: 'never' }]
  ],
  use: {
    baseURL,
    actionTimeout: 10_000,
    trace: 'retain-on-failure'
  },
  projects: [
    {
      name: 'desktop-login',
      use: { viewport: { width: 1440, height: 900 } }
    },
    {
      name: 'mobile-login',
      use: { ...devices['Pixel 7'], viewport: { width: 390, height: 844 } }
    }
  ]
});
