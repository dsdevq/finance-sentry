import {defineConfig, devices} from '@playwright/test';

export default defineConfig({
  testDir: './e2e/visual-regression',
  fullyParallel: false,
  forbidOnly: !!process.env['CI'],
  retries: 0,
  workers: 1,
  reporter: 'html',
  snapshotDir: './e2e/screenshots',
  use: {
    baseURL: 'http://localhost:6006',
    trace: 'on-first-retry',
  },
  expect: {
    toHaveScreenshot: {
      maxDiffPixelRatio: 0.02,
    },
  },
  projects: [
    {
      name: 'chromium',
      use: {...devices['Desktop Chrome']},
    },
  ],
  webServer: {
    command: 'npm run storybook -- --ci',
    url: 'http://localhost:6006',
    reuseExistingServer: !process.env['CI'],
    timeout: 120000,
  },
});
