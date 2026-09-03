import {defineConfig, devices} from '@playwright/test';

const SPA_PORT = 4201;

export default defineConfig({
  testDir: './e2e',
  // Live-stack smoke specs run via playwright.live.config.ts, never against serve.mjs.
  testIgnore: '**/live/**',
  fullyParallel: false,
  forbidOnly: !!process.env['CI'],
  // eslint-disable-next-line @typescript-eslint/no-magic-numbers
  retries: process.env['CI'] ? 2 : 0,
  workers: 1,
  // The html reporter wipes its output folder on write, so it must not share a directory
  // with the json report — otherwise results.json is deleted at the end of every run and
  // the machine-readable artifact CI reads is silently absent.
  reporter: [
    ['json', {outputFile: 'playwright-report/results.json'}],
    ['html', {open: 'never', outputFolder: 'playwright-report/html'}],
  ],
  use: {
    baseURL: `http://localhost:${SPA_PORT}`,
    trace: 'on-first-retry',
    launchOptions: {
      args: ['--no-sandbox', '--disable-dev-shm-usage'],
    },
  },

  webServer: {
    command: 'node e2e/serve.mjs',
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
