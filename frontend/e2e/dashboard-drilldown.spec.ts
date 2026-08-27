import {type Page, expect, test} from '@playwright/test';

const API = 'http://localhost:5001/api/v1';

const AUTH_RESPONSE = {
  user: {id: 'test-user-id', email: 'test@gmail.com'},
  expiresAt: '2027-01-01T00:00:00Z',
};

const DASHBOARD_DATA = {
  aggregatedBalance: {USD: 50000},
  totalNetWorthUsd: 50000,
  accountCount: 3,
  accountsByType: {banking: 2, brokerage: 1},
  monthlyFlow: [
    {
      month: '2026-07-01',
      currency: 'USD',
      inflow: 5000,
      outflow: 3000,
      net: 2000,
      inflowUsd: 5000,
      outflowUsd: 3000,
      netUsd: 2000,
    },
    {
      month: '2026-08-01',
      currency: 'USD',
      inflow: 4800,
      outflow: 2900,
      net: 1900,
      inflowUsd: 4800,
      outflowUsd: 2900,
      netUsd: 1900,
    },
  ],
  topCategories: [
    {category: 'FOOD_AND_DRINK', totalSpend: 800, percentOfTotal: 30},
    {category: 'TRAVEL', totalSpend: 500, percentOfTotal: 18},
  ],
  lastSyncTimestamp: null,
};

const NET_WORTH_HISTORY = {
  snapshots: [],
  hasHistory: false,
};

const INCOME_TRANSACTIONS = {
  items: [
    {
      transactionId: 'tx-1',
      accountId: 'acc-1',
      bankName: 'Test Bank',
      currency: 'USD',
      amount: 2500,
      amountUsd: 2500,
      date: '2026-08-15',
      postedDate: '2026-08-15',
      description: 'Salary August',
      transactionType: 'credit',
      merchantCategory: null,
      isPending: false,
      createdAt: '2026-08-15T00:00:00Z',
      updatedAt: '2026-08-15T00:00:00Z',
    },
  ],
  totalCount: 1,
  hasMore: false,
};

async function mockApis(page: Page): Promise<void> {
  // Silent refresh / auth check on app init
  await page.route(`${API}/auth/me`, route =>
    route.fulfill({status: 200, contentType: 'application/json', body: JSON.stringify(AUTH_RESPONSE)})
  );
  // Dashboard data
  await page.route(`${API}/dashboard/aggregated`, route =>
    route.fulfill({status: 200, contentType: 'application/json', body: JSON.stringify(DASHBOARD_DATA)})
  );
  // Net-worth history (all ranges)
  await page.route(`${API}/net-worth/history**`, route =>
    route.fulfill({status: 200, contentType: 'application/json', body: JSON.stringify(NET_WORTH_HISTORY)})
  );
  // Income transactions
  await page.route(`${API}/accounts/transactions**`, route =>
    route.fulfill({status: 200, contentType: 'application/json', body: JSON.stringify(INCOME_TRANSACTIONS)})
  );
  // Refresh token (called on 401, should not happen but mock it anyway)
  await page.route(`${API}/auth/refresh`, route =>
    route.fulfill({status: 200, contentType: 'application/json', body: JSON.stringify(AUTH_RESPONSE)})
  );
}

test.describe('Dashboard drill-downs', () => {
  test.beforeEach(async ({page}) => {
    await mockApis(page);
  });

  test('dashboard renders stat cards for authenticated user', async ({page}) => {
    await page.goto('/dashboard');
    // Verify the Dashboard heading is visible
    await expect(page.getByRole('heading', {name: 'Dashboard'})).toBeVisible();
    // Stat cards should render (not empty state)
    await expect(page.getByText('Connect your first account')).not.toBeVisible();
    // Income stat card label is visible
    await expect(page.getByText('Income this month')).toBeVisible();
    // Spending stat card label is visible
    await expect(page.getByText('Spending this month')).toBeVisible();
  });

  test('clicking "Income this month" navigates to /income', async ({page}) => {
    await page.goto('/dashboard');
    await expect(page.getByRole('heading', {name: 'Dashboard'})).toBeVisible();

    // Click the Income drill-down button
    const incomeButton = page.getByRole('button', {name: /view income details/i});
    await expect(incomeButton).toBeVisible();
    await incomeButton.click();

    await expect(page).toHaveURL(/\/income$/);
    await expect(page.getByRole('heading', {name: 'Income'})).toBeVisible();
  });

  test('clicking "Spending this month" navigates to /transactions with debit filter', async ({page}) => {
    await page.goto('/dashboard');
    await expect(page.getByRole('heading', {name: 'Dashboard'})).toBeVisible();

    const spendingButton = page.getByRole('button', {name: /view spending details/i});
    await expect(spendingButton).toBeVisible();
    await spendingButton.click();

    await expect(page).toHaveURL(/\/transactions.*type=debit/);
  });
});

test.describe('Income page', () => {
  test.beforeEach(async ({page}) => {
    await mockApis(page);
  });

  test('income page renders heading and stat cards', async ({page}) => {
    await page.goto('/income');
    await expect(page.getByRole('heading', {name: 'Income'})).toBeVisible();
    await expect(page.getByText('Income this month')).toBeVisible();
    await expect(page.getByText('Monthly average')).toBeVisible();
    await expect(page.getByText('Year to date')).toBeVisible();
  });

  test('income page renders transaction table with mocked data', async ({page}) => {
    await page.goto('/income');
    await expect(page.getByRole('heading', {name: 'Income'})).toBeVisible();
    // Table should render with the mocked transaction
    await expect(page.getByRole('table', {name: 'Income transactions'})).toBeVisible();
    await expect(page.getByText('Salary August')).toBeVisible();
    await expect(page.getByText('Test Bank')).toBeVisible();
  });
});
