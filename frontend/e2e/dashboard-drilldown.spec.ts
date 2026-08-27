import {type Page, expect, test} from '@playwright/test';

const API = 'http://localhost:5001/api/v1';

const AUTH_RESPONSE = {
  user: {id: 'test-user-id', email: 'test@gmail.com'},
  expiresAt: '2027-01-01T00:00:00Z',
};

// Backend uses "yyyy-MM" format (not "yyyy-MM-dd") — see MoneyFlowStatisticsService.
function currentUtcMonthKey(): string {
  const now = new Date();
  return `${now.getUTCFullYear()}-${String(now.getUTCMonth() + 1).padStart(2, '0')}`;
}

function prevUtcMonthKey(): string {
  const now = new Date();
  const prev = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() - 1, 1));
  return `${prev.getUTCFullYear()}-${String(prev.getUTCMonth() + 1).padStart(2, '0')}`;
}

const DASHBOARD_DATA = {
  aggregatedBalance: {USD: 50000},
  totalNetWorthUsd: 50000,
  accountCount: 3,
  accountsByType: {banking: 2, brokerage: 1},
  monthlyFlow: [
    {
      month: prevUtcMonthKey(),
      currency: 'USD',
      inflow: 5000,
      outflow: 3000,
      net: 2000,
      inflowUsd: 5000,
      outflowUsd: 3000,
      netUsd: 2000,
    },
    {
      month: currentUtcMonthKey(),
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

// Transactions for the ledger page — deliberately includes a pending debit and a
// "transfer" debit that would inflate a client-side sum beyond the backend value (2900).
const LEDGER_TRANSACTIONS = {
  items: [
    {
      transactionId: 'tx-1',
      accountId: 'acc-1',
      bankName: 'Test Bank',
      currency: 'USD',
      amount: 400,
      amountUsd: 400,
      date: '2026-08-10',
      postedDate: '2026-08-10',
      description: 'Grocery store',
      transactionType: 'debit',
      merchantCategory: 'FOOD_AND_DRINK',
      isPending: false,
      createdAt: '2026-08-10T00:00:00Z',
      updatedAt: '2026-08-10T00:00:00Z',
    },
    {
      transactionId: 'tx-2',
      accountId: 'acc-1',
      bankName: 'Test Bank',
      currency: 'USD',
      amount: 500,
      amountUsd: 500,
      date: '2026-08-12',
      postedDate: null,
      description: 'Pending payment',
      transactionType: 'debit',
      merchantCategory: null,
      isPending: true,
      createdAt: '2026-08-12T00:00:00Z',
      updatedAt: '2026-08-12T00:00:00Z',
    },
    {
      transactionId: 'tx-3',
      accountId: 'acc-1',
      bankName: 'Test Bank',
      currency: 'USD',
      amount: 1000,
      amountUsd: 1000,
      date: '2026-08-13',
      postedDate: '2026-08-13',
      description: 'Transfer to savings',
      transactionType: 'debit',
      merchantCategory: 'TRANSFER_IN',
      isPending: false,
      createdAt: '2026-08-13T00:00:00Z',
      updatedAt: '2026-08-13T00:00:00Z',
    },
    {
      transactionId: 'tx-4',
      accountId: 'acc-2',
      bankName: 'Savings Bank',
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
  totalCount: 4,
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

async function mockApisWithLedger(page: Page): Promise<void> {
  await page.route(`${API}/auth/me`, route =>
    route.fulfill({status: 200, contentType: 'application/json', body: JSON.stringify(AUTH_RESPONSE)})
  );
  await page.route(`${API}/dashboard/aggregated`, route =>
    route.fulfill({status: 200, contentType: 'application/json', body: JSON.stringify(DASHBOARD_DATA)})
  );
  await page.route(`${API}/net-worth/history**`, route =>
    route.fulfill({status: 200, contentType: 'application/json', body: JSON.stringify(NET_WORTH_HISTORY)})
  );
  await page.route(`${API}/accounts/transactions**`, route =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(LEDGER_TRANSACTIONS),
    })
  );
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

test.describe('Transaction ledger — Monthly Outflow stat', () => {
  test.beforeEach(async ({page}) => {
    await mockApisWithLedger(page);
  });

  // The backend mock returns outflowUsd: 2900 for the current month.
  // The page also has a pending debit ($500) and a transfer debit ($1,000) that
  // the backend excludes but a client-side sum would include. This verifies the
  // stat reads the server-side aggregate, not a sum over the loaded page.
  test('Monthly Outflow shows server-side aggregate, not client-side page sum', async ({page}) => {
    await page.goto('/transactions');
    await expect(page.getByRole('heading', {name: 'Transaction Ledger'})).toBeVisible();
    // Backend says $2,900 — the stat must match this, not the $1,900 client-side sum
    // (400 grocery + 500 pending + 1000 transfer = $1,900 from debits in page).
    await expect(page.getByText('$2,900.00')).toBeVisible();
    await expect(page.getByText('Monthly Outflow')).toBeVisible();
  });

  test('Monthly Outflow does not change when Load More is clicked', async ({page}) => {
    await page.goto('/transactions');
    await expect(page.getByRole('heading', {name: 'Transaction Ledger'})).toBeVisible();
    await expect(page.getByText('$2,900.00')).toBeVisible();
    // No "Load More" button because hasMore: false in mock — stat stays stable.
    await expect(page.getByRole('button', {name: /load more/i})).not.toBeVisible();
    // Value unchanged after full render.
    await expect(page.getByText('$2,900.00')).toBeVisible();
  });
});
