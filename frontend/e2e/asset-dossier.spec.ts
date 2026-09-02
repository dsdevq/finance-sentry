import {expect, type Page, test} from '@playwright/test';

const API = '**/api/v1';

const AUTH_RESPONSE = {
  user: {id: 'test-user-id', email: 'test@gmail.com'},
  expiresAt: '2027-01-01T00:00:00Z',
};

const BROKERAGE_HOLDINGS = {
  provider: 'ibkr',
  syncedAt: '2026-09-01T10:00:00Z',
  isStale: false,
  positions: [
    {
      symbol: 'AAPL',
      instrumentType: 'STK',
      quantity: 10,
      usdValue: 17500,
      costBasisUsd: 15000,
      averageCostUsd: 1500,
    },
    {
      symbol: 'MSFT',
      instrumentType: 'STK',
      quantity: 5,
      usdValue: 21000,
      costBasisUsd: 18000,
      averageCostUsd: 3600,
    },
  ],
  totalUsdValue: 38500,
};

const CRYPTO_HOLDINGS = {
  provider: 'binance',
  syncedAt: '2026-09-01T10:00:00Z',
  isStale: false,
  holdings: [],
  totalUsdValue: 0,
};

const DOSSIER_AAPL = {
  symbol: 'AAPL',
  position: {
    provider: 'ibkr',
    quantity: 10,
    currentValueUsd: 17500,
    costBasisUsd: 15000,
    unrealizedPnlUsd: 2500,
    unrealizedPnlPercent: 16.67,
    taxLots: [
      {
        quantity: 10,
        currentValueUsd: 17500,
        averageCostUsd: 1500,
        costBasisUsd: 15000,
        unrealizedPnlUsd: 2500,
        unrealizedPnlPercent: 16.67,
        acquiredAt: '2024-03-15T00:00:00Z',
        isLongTerm: true,
      },
    ],
  },
  thesis: {
    id: 'thesis-1',
    ticker: 'AAPL',
    thesisText: 'Apple continues to expand its services revenue and ecosystem lock-in.',
    keyDataPoints: [],
    catalysts: [{date: '2026-10-15', event: 'Q4 Earnings expected strong services growth'}],
    invalidationTriggers: [
      {
        metric: 'services_revenue_growth',
        direction: 'below',
        threshold: 5,
        proxyTicker: null,
        consecutivePeriods: 2,
        periodType: 'Quarter',
      },
    ],
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-08-01T00:00:00Z',
    brokenAt: null,
    brokenReason: null,
    entryPrice: 175.0,
  },
  valuation: {
    ticker: 'AAPL',
    notApplicable: false,
    price: 175.0,
    isStale: false,
    metrics: {
      trailingPe: {value: 28.5, fiveYearAvg: 25.3, historyWindowYears: 5, historyUnavailable: false},
      forwardPe: {value: 26.2, fiveYearAvg: 23.1, historyWindowYears: 5, historyUnavailable: false},
      evToEbitda: {value: null, fiveYearAvg: null, historyWindowYears: null, historyUnavailable: true},
      dividendYield: {value: 0.5, fiveYearAvg: 0.6, historyWindowYears: 5, historyUnavailable: false},
    },
    consensusTarget: 210.0,
    impliedUpsidePct: 20.0,
    peerSet: null,
    sources: ['yahoo_finance'],
    retrievedAt: '2026-09-01T08:00:00Z',
  },
  analysts: {
    recentActions: [
      {
        ticker: 'AAPL',
        firm: 'Goldman Sachs',
        actionType: 'Upgraded',
        priorRating: 'Neutral',
        newRating: 'Buy',
        priorTarget: 185.0,
        newTarget: 220.0,
        actionDate: '2026-08-20',
        source: 'benzinga',
        sourceUrl: null,
        ingestedAt: '2026-08-20T15:00:00Z',
      },
    ],
    trends: [],
    coverage: 'inUniverse',
  },
  recentNews: [
    {
      id: 'news-1',
      source: 'Reuters',
      title: 'Apple reports record services revenue in Q3',
      url: 'https://reuters.com/apple-q3',
      summary: 'Apple beat analyst expectations with record services revenue.',
      tickers: ['AAPL'],
      categories: ['earnings'],
      publishedAt: '2026-08-15T12:00:00Z',
    },
  ],
  nextEarnings: {
    ticker: 'AAPL',
    eventType: 'Earnings Release',
    eventDate: '2026-10-15',
    isEstimate: true,
    source: 'yahoo_finance',
  },
  radarSignals: [
    {
      timestamp: '2026-08-28T09:00:00Z',
      scanner: 'momentum',
      signalType: 'RSI_OVERSOLD',
      severity: 'medium',
      payload: {rsi: 28},
    },
  ],
  generatedAt: '2026-09-01T10:30:00Z',
};

async function mockApis(page: Page): Promise<void> {
  await page.route(`${API}/auth/me`, route =>
    route.fulfill({status: 200, contentType: 'application/json', body: JSON.stringify(AUTH_RESPONSE)})
  );
  await page.route(`${API}/auth/refresh`, route =>
    route.fulfill({status: 200, contentType: 'application/json', body: JSON.stringify(AUTH_RESPONSE)})
  );
  await page.route(`${API}/brokerage/holdings`, route =>
    route.fulfill({status: 200, contentType: 'application/json', body: JSON.stringify(BROKERAGE_HOLDINGS)})
  );
  await page.route(`${API}/crypto/holdings`, route =>
    route.fulfill({status: 200, contentType: 'application/json', body: JSON.stringify(CRYPTO_HOLDINGS)})
  );
  await page.route(`${API}/research/assets/AAPL/dossier`, route =>
    route.fulfill({status: 200, contentType: 'application/json', body: JSON.stringify(DOSSIER_AAPL)})
  );
}

test.describe('Asset Dossier', () => {
  test.beforeEach(async ({page}) => {
    await mockApis(page);
  });

  test('click holding symbol navigates to dossier page', async ({page}) => {
    await page.goto('/accounts/investments');
    // Wait for positions to load and AAPL to appear
    await expect(page.getByText('AAPL').first()).toBeVisible();

    // Click the AAPL symbol button
    await page.getByRole('button', {name: /AAPL/i}).first().click();

    // Should land on the dossier URL
    await expect(page).toHaveURL(/\/assets\/AAPL/);
  });

  test('dossier page renders symbol header', async ({page}) => {
    await page.goto('/assets/AAPL');
    await expect(page.getByRole('heading', {name: 'AAPL', level: 1})).toBeVisible();
  });

  test('dossier page renders position section', async ({page}) => {
    await page.goto('/assets/AAPL');
    await expect(page.getByText('Position')).toBeVisible();
    await expect(page.getByText('Current Value')).toBeVisible();
    // Unrealized P&L section
    await expect(page.getByText('Unrealized P&L')).toBeVisible();
  });

  test('dossier page renders thesis section', async ({page}) => {
    await page.goto('/assets/AAPL');
    await expect(page.getByText('Investment Thesis')).toBeVisible();
    await expect(page.getByText('Apple continues to expand its services revenue')).toBeVisible();
    await expect(page.getByText('Active')).toBeVisible();
  });

  test('dossier page renders analyst coverage', async ({page}) => {
    await page.goto('/assets/AAPL');
    await expect(page.getByText('Analyst Coverage')).toBeVisible();
    await expect(page.getByText('Goldman Sachs')).toBeVisible();
  });

  test('dossier page renders recent news', async ({page}) => {
    await page.goto('/assets/AAPL');
    await expect(page.getByText('Recent News')).toBeVisible();
    await expect(page.getByText('Apple reports record services revenue in Q3')).toBeVisible();
  });

  test('back button returns to investments', async ({page}) => {
    await page.goto('/assets/AAPL');
    await expect(page.getByRole('heading', {name: 'AAPL', level: 1})).toBeVisible();

    await page.getByRole('button', {name: /back to investments/i}).click();

    await expect(page).toHaveURL(/\/accounts\/investments/);
  });
});
