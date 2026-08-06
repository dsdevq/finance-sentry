# Quickstart: Wealth Aggregation API Scenarios

**Feature**: `008-wealth-aggregation-api` | **Date**: 2026-04-20

---

## Scenario 1: Get Total Net Worth

**Prerequisites**: User is authenticated; at least one bank account is connected and synced.

**Steps**:

1. User (or frontend) calls `GET /api/v1/wealth/summary`
2. System loads all active `BankAccount` rows for the user
3. Groups accounts by provider category via `ProviderCategoryMapper`
4. Converts each account's `CurrentBalance` to USD using static rate table
5. Returns nested breakdown: total → categories → individual accounts

**Expected response**:
- `totalNetWorth`: sum of all balances in USD
- `categories`: one entry per category (only categories with accounts present)
- Each account listed with native balance and USD equivalent

---

## Scenario 2: Filter by Provider Category

**Steps**:

1. Call `GET /api/v1/wealth/summary?category=banking`
2. System loads only accounts whose provider maps to `"banking"`
3. Returns same structure but scoped to banking accounts only

**Expected**: Only Plaid and Monobank accounts appear. `totalNetWorth` reflects banking only.

---

## Scenario 3: Filter by Specific Provider

**Steps**:

1. Call `GET /api/v1/wealth/summary?provider=monobank`
2. System loads only accounts with `Provider = "monobank"`
3. Returns Monobank accounts only

**Expected**: Only Monobank accounts. Useful for "how much do I have in Monobank specifically?"

---

## Scenario 4: No Matching Accounts

**Steps**:

1. User has only bank accounts (no crypto)
2. Call `GET /api/v1/wealth/summary?category=crypto`

**Expected**: 200 OK with `totalNetWorth: 0`, `categories: []`. Not a 404.

---

## Scenario 5: Monthly Expense Summary

**Steps**:

1. Call `GET /api/v1/wealth/transactions/summary?from=2026-04-01&to=2026-04-30`
2. System finds all posted, active transactions in the window across all accounts
3. Splits by `TransactionType` (debit = expense, credit = income)
4. Groups by provider category
5. Returns totals

**Expected**: `totalDebits` = total spending, `totalCredits` = total income for April 2026.

---

## Scenario 6: Filtered Expense Summary

**Steps**:

1. Call `GET /api/v1/wealth/transactions/summary?from=2026-04-01&to=2026-04-30&provider=monobank`
2. Only Monobank transactions contribute to totals

**Expected**: Monobank-only spending/income for the month.

---

## Scenario 7: Invalid Date Range

**Steps**:

1. Call `GET /api/v1/wealth/transactions/summary?from=2026-04-30&to=2026-04-01`

**Expected**: 400 `INVALID_DATE_RANGE`

---

## Manual Testing Checklist

- [ ] `GET /wealth/summary` with multiple connected accounts → correct total
- [ ] `GET /wealth/summary?category=banking` → only banking accounts
- [ ] `GET /wealth/summary?provider=monobank` → only Monobank accounts
- [ ] `GET /wealth/summary?category=crypto` when no crypto accounts → 200 with empty categories
- [ ] `GET /wealth/transactions/summary?from=...&to=...` → correct debit/credit totals
- [ ] Transaction summary with `provider=monobank` filter → Monobank-only totals
- [ ] Transaction summary with `from > to` → 400
- [ ] Transaction summary without `from` or `to` → 400
- [ ] All endpoints return 401 without JWT
- [ ] Accounts with null balance excluded from net worth total but included in account list
