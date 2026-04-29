import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './tests/ui',
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'playwright-report' }]],
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: process.env.CI ? 1 : undefined,
  use: {
    baseURL: process.env.BASE_URL || 'http://127.0.0.1:4173',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    viewport: { width: 1440, height: 920 }
  },
  webServer: {
    command: 'node scripts/serve-web-dev.mjs',
    url: 'http://127.0.0.1:4173/webapp/index.html',
    reuseExistingServer: !process.env.CI,
    stdout: 'pipe',
    stderr: 'pipe'
  }
});
