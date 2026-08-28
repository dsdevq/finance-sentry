import {defineConfig, devices} from '@playwright/test';

const SPA_PORT = 4201;

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: !!process.env['CI'],
  // eslint-disable-next-line @typescript-eslint/no-magic-numbers
  retries: process.env['CI'] ? 2 : 0,
  workers: 1,
  reporter: [['json', {outputFile: 'playwright-report/results.json'}], ['html', {open: 'never'}]],
  use: {
    baseURL: `http://localhost:${SPA_PORT}`,
    trace: 'on-first-retry',
    launchOptions: {
      args: ['--no-sandbox', '--disable-dev-shm-usage'],
    },
  },

  webServer: {
    command: `node e2e/serve.mjs`,
    url: `http://localhost:${SPA_PORT}`,
    reuseExistingServer: false,
    env: {PORT: String(SPA_PORT)},
  },

  projects: [
    {
      name: 'chromium',
      use: {...devices['Desktop Chrome']},
    },
  ],
});
