import { defineConfig } from '@playwright/test';

const baseURL = process.env.WMS_BASE_URL;

if (!baseURL) {
  throw new Error('WMS_BASE_URL is required for visual auth setup.');
}

export default defineConfig({
  testDir: '.',
  testMatch: /auth\.setup\.ts/,
  timeout: 60_000,
  use: {
    baseURL,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure'
  },
  reporter: [['list']]
});
