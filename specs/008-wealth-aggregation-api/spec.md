# Feature Specification: Financial Aggregation and Wealth Overview API

**Feature Branch**: `008-wealth-aggregation-api`  
**Created**: 2026-04-19  
**Status**: Draft  
**Input**: User description: "Financial aggregation API that answers questions like what is my total net worth across all providers, what do I have across all banks, what have I spent this month, show me all crypto holdings. The API should support flexible grouping and filtering by provider category (bank, crypto, broker), by specific provider (monobank, plaid, binance), by currency, and by time window for transaction-based metrics like expenses and income. Users should be able to get a unified financial snapshot or drill down into a specific slice."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Total Wealth Snapshot (Priority: P1)

A user wants to see their complete financial picture in a single call — total net worth across all connected providers, broken down by provider category (banks, crypto, brokers) and by currency.

**Why this priority**: This is the core product value proposition. Every other slice is a detail underneath it. It is also the first thing a user sees on a dashboard.

**Independent Test**: Call `GET /api/v1/wealth/summary` with a valid JWT. The response must include total net worth (summed across all accounts), a breakdown by category (banking, crypto, brokerage), and individual account balances. Verify correct aggregation even when accounts are in different currencies.

**Acceptance Scenarios**:

1. **Given** a user has 2 Plaid bank accounts (USD) and 1 Monobank account (UAH), **When** they call the wealth summary endpoint, **Then** the response includes total net worth in a base currency, per-category subtotals, and a list of contributing accounts with individual balances.
2. **Given** a user has no connected accounts, **When** they call the wealth summary endpoint, **Then** the response returns empty categories and a zero total.
3. **Given** an unauthenticated request, **When** the endpoint is called, **Then** it returns 401 Unauthorized.

---

### User Story 2 - Filtered Slice by Category or Provider (Priority: P2)

A user wants to drill into a specific segment of their wealth — e.g., "show me only my bank balances" or "show me only my Monobank accounts" — without receiving the full snapshot.

**Why this priority**: Drilling down by provider type is the primary navigation pattern for a multi-provider app. Required before adding more provider types.

**Independent Test**: Call `GET /api/v1/wealth/summary?category=banking` and verify only banking accounts are included. Call `GET /api/v1/wealth/summary?provider=monobank` and verify only Monobank accounts appear.

**Acceptance Scenarios**:

1. **Given** a user has both bank and crypto accounts, **When** they filter by `category=banking`, **Then** only bank accounts appear in the response and the total reflects only banking balances.
2. **Given** a user has Plaid and Monobank accounts, **When** they filter by `provider=monobank`, **Then** only Monobank accounts appear.
3. **Given** an unknown filter value (e.g., `category=unknown`), **When** the endpoint is called, **Then** it returns an empty result (not an error).

---

### User Story 3 - Expense and Income Summary (Priority: P3)

A user wants to see how much they have spent or received during a specific time window, optionally scoped to a provider category or specific provider.

**Why this priority**: Transaction-based metrics (expenses, income) complete the financial picture. Net worth tells you what you have; spending tells you where it goes.

**Independent Test**: Call `GET /api/v1/wealth/transactions/summary?from=2026-04-01&to=2026-04-30`. Verify the response includes total debits (expenses) and total credits (income) for the period. Optionally add `category=banking` and verify only transactions from bank accounts are counted.

**Acceptance Scenarios**:

1. **Given** a user has transactions in April 2026, **When** they call the transaction summary with `from=2026-04-01&to=2026-04-30`, **Then** the response returns total expenses and total income for that period, broken down by provider category.
2. **Given** a user filters by `provider=monobank`, **When** the transaction summary is called, **Then** only Monobank transactions contribute to the totals.
3. **Given** a time window with no transactions, **When** the transaction summary is called, **Then** the response returns zero for all totals.
4. **Given** an invalid date range (from > to), **When** the endpoint is called, **Then** it returns 400 Bad Request.

---

### Edge Cases

- What happens when accounts are in different currencies? → Aggregation uses a configurable base currency (default: USD); each account balance is converted using a static exchange rate table. Conversion accuracy is best-effort for non-USD accounts.
- What happens when an account has no synced balance? → Accounts with null balance are excluded from totals but still listed in the response with a null balance field.
- What happens when a provider has no accounts connected? → That category is omitted from the response entirely.
- What happens if a filter matches no accounts? → The response returns zero totals and an empty accounts list (not an error).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST return a wealth summary for the authenticated user, including total net worth across all connected accounts.
- **FR-002**: System MUST break down the wealth summary by provider category (banking, crypto, brokerage).
- **FR-003**: System MUST support filtering the wealth summary by a single provider category (e.g., `category=banking`).
- **FR-004**: System MUST support filtering the wealth summary by a specific provider name (e.g., `provider=monobank`).
- **FR-005**: System MUST return a transaction summary for a specified date range, including total debits (expenses) and total credits (income).
- **FR-006**: System MUST support the same category and provider filters on the transaction summary endpoint.
- **FR-007**: System MUST return results scoped strictly to the authenticated user's accounts — no cross-user data leakage.
- **FR-008**: System MUST exclude accounts with null balance from totals but include them in the account list.
- **FR-009**: System MUST convert multi-currency balances to a base currency (USD) for aggregated totals, while also returning each account's native currency and balance.
- **FR-010**: System MUST return 400 for invalid query parameters (e.g., malformed dates, `from` > `to`).

### Key Entities

- **WealthSummary**: Aggregate view of all account balances — total net worth in base currency, per-category subtotals, list of contributing accounts.
- **CategorySummary**: Subtotal for a single provider category (banking / crypto / brokerage) — category name, total in base currency, list of account balances.
- **AccountBalance**: Individual account entry — account ID, provider, category, account type, currency, native balance, base-currency equivalent.
- **TransactionSummary**: Aggregated transaction view for a time window — total debits, total credits, net flow, broken down by category.
- **ProviderCategory**: Classification of each provider — `banking` (Plaid, Monobank), `crypto` (Binance, future), `brokerage` (Interactive Brokers, future).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can retrieve their complete financial snapshot in under 2 seconds for up to 20 connected accounts.
- **SC-002**: Filtering by category or provider returns accurate results — zero false positives (accounts from excluded providers must not appear).
- **SC-003**: Transaction summary totals match the sum of individual transactions for the same time window and filter — 100% numerical accuracy.
- **SC-004**: All endpoints return 401 for unauthenticated requests — no financial data accessible without a valid session.
- **SC-005**: Adding a new provider in the future requires no changes to the aggregation endpoints — only the provider-to-category mapping needs updating.

## Assumptions

- Currency conversion uses a static lookup table (USD, EUR, UAH, GBP); live exchange rates are out of scope.
- The base currency for aggregated totals is USD; no user-configurable base currency in this feature.
- Provider category assignment is determined by a static mapping at the application layer — no new database column required.
- Only `BankAccount` balances and `Transaction` records are in scope; investment positions, crypto asset holdings (beyond account balance), and real estate are out of scope.
- Pagination of the summary response is out of scope; all accounts are returned in a single response.
- This feature is backend-only; no frontend UI changes are in scope.
- Feature 007 (Monobank adapter) does not need to be completed before this feature can be implemented — it operates on existing `BankAccount` and `Transaction` data regardless of provider.

## Notes

- [DECISION] Provider-to-category mapping: `plaid` → `banking`, `monobank` → `banking`, `binance` → `crypto`, `ibkr` → `brokerage`. Static dictionary in application layer, extensible without migrations.
- [DECISION] Base currency: USD. Static rate table seeded with common pairs (USD, EUR, UAH, GBP). Live rates deferred.
- [DECISION] Two separate endpoints: `GET /api/v1/wealth/summary` (balance-based) and `GET /api/v1/wealth/transactions/summary` (transaction-based). Kept separate because data sources and query patterns differ significantly.
- [OUT OF SCOPE] Frontend dashboard widget — frontend consumption of these endpoints is deferred to the UI phase.
- [OUT OF SCOPE] Live exchange rates — static table only in this feature.
- [OUT OF SCOPE] Pagination — revisit when account count exceeds practical limits.
- [OUT OF SCOPE] Per-account transaction breakdown — transaction summary returns category-level and provider-level totals only.
