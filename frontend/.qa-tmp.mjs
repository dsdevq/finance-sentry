import { chromium } from 'playwright';
import fs from 'fs';

const CHROME = '/home/dsdevqq/.cache/ms-playwright/chromium-1228/chrome-linux64/chrome';
const OUT = '/tmp/claude-1000/-home-dsdevqq-projects-finance-sentry/d2468800-4d4b-4cb8-a280-d6b8c9870aa2/scratchpad/shots';
fs.mkdirSync(OUT, { recursive: true });

const BASE = 'http://localhost:4200';
const CREDS = { email: 'test@gmail.com', password: 'Darkfly21' };

// console noise we expect and ignore
const IGNORE = [/GSI_LOGGER/, /FedCM/, /accounts list is empty/i, /401/, /net::ERR_ABORTED/, /Failed to load resource.*401/, /google/i, /gsi/i];

const routes = [
  { name: 'accounts', path: '/accounts/list' },
  { name: 'dashboard', path: '/dashboard' },
  { name: 'transactions', path: '/transactions' },
  { name: 'holdings', path: '/holdings' },
  { name: 'budgets', path: '/budgets' },
  { name: 'subscriptions', path: '/subscriptions' },
  { name: 'settings', path: '/settings' },
];

const report = [];

const browser = await chromium.launch({
  executablePath: CHROME,
  headless: true,
  args: ['--no-sandbox', '--disable-dev-shm-usage'],
});
const ctx = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
const page = await ctx.newPage();

let consoleErrs = [];
let pageErrs = [];
page.on('console', (m) => { if (m.type() === 'error') { const t = m.text(); if (!IGNORE.some((r) => r.test(t))) consoleErrs.push(t); } });
page.on('pageerror', (e) => { const t = e.message || String(e); if (!IGNORE.some((r) => r.test(t))) pageErrs.push(t); });

function snapErrs() { const c = [...consoleErrs]; const p = [...pageErrs]; consoleErrs = []; pageErrs = []; return { console: c, pageerror: p }; }

// ---- LOGIN ----
await page.goto(BASE + '/login', { waitUntil: 'domcontentloaded' });
await page.waitForTimeout(1500);
try {
  await page.fill('input[type=email]', CREDS.email);
  await page.fill('input[type=password]', CREDS.password);
  await page.click('button[type="submit"]');
  await page.waitForTimeout(3500);
} catch (e) {
  report.push({ step: 'login', error: 'login form interaction failed: ' + e.message });
}
await page.screenshot({ path: `${OUT}/00-after-login.png`, fullPage: true });
report.push({ step: 'login', url: page.url(), errs: snapErrs() });

// ---- WALK ROUTES ----
for (const r of routes) {
  await page.goto(BASE + r.path, { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(3000);
  const body = await page.evaluate(() => document.body.innerText).catch(() => '');
  const signals = {
    hasDollarZero: /\$0\.00/.test(body),
    hasRawCategory: /[A-Z]{3,}_[A-Z]{3,}/.test(body),        // e.g. FOOD_AND_DRINK
    stuckSpinner: await page.locator('.spinner, [role=progressbar], cmn-spinner, .loading').count().catch(() => 0),
    visibleError: /error|failed|something went wrong|unable to/i.test(body),
    bodyLen: body.length,
    excerpt: body.replace(/\s+/g, ' ').slice(0, 400),
  };
  await page.screenshot({ path: `${OUT}/${r.name}.png`, fullPage: true });
  report.push({ step: r.name, url: page.url(), signals, errs: snapErrs() });
}

fs.writeFileSync(`${OUT}/report.json`, JSON.stringify(report, null, 2));
console.log(JSON.stringify(report, null, 2));

await browser.close();
