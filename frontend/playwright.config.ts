import {defineConfig, devices} from '@playwright/test';

// libXfixes.so.3 is not installed in the sandbox — provide it from /tmp where it was extracted.
// This must be set before Playwright spawns Chromium.
if (!process.env['LD_LIBRARY_PATH']?.includes('/tmp')) {
  process.env['LD_LIBRARY_PATH'] = ['/tmp', process.env['LD_LIBRARY_PATH'] ?? '']
    .filter(Boolean)
    .join(':');
}

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
      env: {LD_LIBRARY_PATH: process.env['LD_LIBRARY_PATH'] ?? '/tmp'},
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
