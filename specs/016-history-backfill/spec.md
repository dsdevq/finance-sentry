# Feature Specification: Historical Net Worth Backfill

**Feature Branch**: `016-history-backfill`
**Created**: 2026-05-03
**Status**: Draft
**Input**: User description: "016-net-worth-backfill — When a user connects a new provider account (Binance, IBKR, or Monobank), backfill historical net worth snapshots so the chart is populated immediately rather than waiting months for the scheduled job."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Binance Backfill on Connect (Priority: P1)

A user connects their Binance account for the first time. Instead of seeing an empty net worth chart that will only populate over future months, the chart immediately shows historical crypto portfolio value for the months covered by Binance's snapshot API.

**Why this priority**: Binance provides native daily balance snapshots — structured data directly available. Crypto often represents a significant share of portfolio value, so this delivers immediate visible impact.

**Independent Test**: Connect a Binance account and verify the net worth history chart shows historical monthly snapshots without waiting for any scheduled jobs.

**Acceptance Scenarios**:

1. **Given** a user with no existing net worth snapshots, **When** their Binance account completes its first sync, **Then** the system creates monthly snapshots for each calendar month covered by Binance's daily snapshot history, each dated to the last day of that month.
2. **Given** a user with existing monthly snapshots from the scheduled job, **When** they connect Binance, **Then** all snapshots are recomputed to include the Binance crypto contribution.
3. **Given** a Binance account with only 10 days of history, **When** backfill runs, **Then** only the single covered month produces a snapshot — no empty months are created.

---

### User Story 2 — IBKR Backfill on Connect (Priority: P1)

A user connects their Interactive Brokers account. The net worth chart immediately reflects months or years of brokerage portfolio history from IBKR's performance API, combined with any existing crypto or banking data.

**Why this priority**: IBKR provides the deepest historical lookback of all supported providers, making it the most impactful for a meaningful multi-month chart.

**Independent Test**: Connect an IBKR account and verify historical monthly snapshots appear for each month in the IBKR NAV history without triggering manual jobs.

**Acceptance Scenarios**:

1. **Given** a user connecting IBKR for the first time, **When** first sync completes, **Then** historical monthly snapshots are created for all months present in the IBKR NAV history, dated to the last day of each month.
2. **Given** a user who already has Binance connected, **When** they connect IBKR, **Then** all snapshots are recomputed combining Binance crypto totals with IBKR brokerage totals.
3. **Given** an IBKR account with multiple sub-accounts, **When** backfill runs, **Then** all sub-account NAV values are summed into a single brokerage total per snapshot month.

---

### User Story 3 — Monobank Backfill on Connect (Priority: P2)

A user connects their Monobank account. The net worth chart reflects historical banking balances reconstructed from Monobank's transaction statement history, combined with any existing crypto and brokerage data.

**Why this priority**: Monobank balance history requires reconstruction from transaction statements (each transaction carries the post-transaction balance), making it slightly more complex than Binance/IBKR but delivering equal user value.

**Independent Test**: Connect a Monobank account and verify historical monthly snapshots appear reflecting the reconstructed banking balance for each covered month.

**Acceptance Scenarios**:

1. **Given** a user connecting Monobank, **When** first sync completes, **Then** the system fetches statement history in 31-day windows, extracts the last transaction balance per calendar month per account, converts to USD, and creates monthly snapshots.
2. **Given** a user with multiple Monobank accounts in different currencies, **When** backfill runs, **Then** all account balances for each month are converted to USD and summed into a single banking total per snapshot date.
3. **Given** a calendar month where no Monobank transactions occurred, **When** backfill runs, **Then** that month's banking total uses the most recent known balance (last transaction before that month).

---

### User Story 4 — Full Recompute When Additional Provider Is Connected (Priority: P2)

A user who already has snapshots from one provider connects an additional provider. All existing snapshots are recomputed to include contributions from all currently connected providers.

**Why this priority**: Without recompute-on-connect, adding a second provider would leave historical snapshots missing the new provider's contribution for months that predate the connect event.

**Independent Test**: Connect Provider A, verify snapshots. Connect Provider B, verify all snapshots now reflect both providers' contributions.

**Acceptance Scenarios**:

1. **Given** a user with Binance connected and crypto history backfilled, **When** they connect IBKR, **Then** all historical snapshots are recomputed combining both Binance crypto and IBKR brokerage totals.
2. **Given** a recompute that completes, **When** any snapshot is inspected, **Then** its total_net_worth equals the sum of its banking, brokerage, and crypto totals.
3. **Given** a provider with no historical data for a given month, **When** recompute runs, **Then** that provider's contribution for that month is 0 and other providers' contributions are preserved.

---

### Edge Cases

- What if the backfill job fails mid-way due to a provider API rate limit? Hangfire retries automatically; a failed run must not corrupt existing snapshots (the delete step runs only at the start of a successful recompute, not mid-job).
- What if a user disconnects a provider after backfill? Existing snapshots retain historical values; no automatic recompute occurs on disconnect.
- What if Binance returns no snapshot data (brand new account)? No snapshots are created; the job completes successfully with a no-op result.
- What if Monobank rate-limits statement requests (1 request per 60 seconds)? The job introduces required delays between chained window requests.
- What if a month-end snapshot already exists from the scheduled job? The backfill replaces it with the recomputed value from all currently connected providers.
- What if IBKR's ibeam session expires during backfill? The job fails and is retried; session re-auth is handled by the existing IBKR infrastructure.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST automatically enqueue a historical backfill job when any provider account's first sync completes with a success status.
- **FR-002**: The backfill job MUST delete all existing net worth snapshots for the user before recreating them, ensuring a clean recompute from all currently connected providers.
- **FR-003**: System MUST fetch Binance portfolio history and produce one snapshot per calendar month, using the last available daily entry for each month.
- **FR-004**: System MUST fetch IBKR portfolio NAV history and produce one snapshot per calendar month, using the last available daily NAV entry for each month.
- **FR-005**: System MUST fetch Monobank transaction statement history in 31-day windows, extract the last transaction balance per calendar month per account, convert to USD, and sum across all Monobank accounts.
- **FR-006**: System MUST aggregate contributions from all currently connected providers (banking, crypto, brokerage) into a single snapshot per month-end date.
- **FR-007**: Each snapshot MUST be dated to the last calendar day of its month, consistent with the existing scheduled snapshot format.
- **FR-008**: System MUST skip Plaid accounts during backfill; Plaid banking contributions default to 0 for all historical months.
- **FR-009**: The backfill job MUST be idempotent — running it multiple times produces identical snapshots.
- **FR-010**: System MUST respect Monobank's rate limit of one statement request per 60 seconds.
- **FR-011**: A failed backfill job MUST NOT leave snapshots in a partially-recomputed state; the delete-and-recreate operation must be treated as an atomic unit from the user's perspective.

### Key Entities

- **Net Worth Snapshot**: A monthly record of a user's total net worth broken down by asset class (banking, brokerage, crypto). One per user per month, dated to the last day of the month. Existing entity — backfill extends its write path to support replace semantics.
- **Historical Backfill Job**: A background job triggered on provider connect that orchestrates fetching per-provider history and recomputing all snapshots for a user.
- **Provider Monthly Balance**: An intermediate computed value (per provider, per month) used during backfill aggregation. Not persisted — exists only in job memory during execution.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After connecting any supported provider, the net worth history chart displays historical data within 5 minutes without any manual action from the user.
- **SC-002**: The number of historical months visible in the chart after connecting a provider equals the number of calendar months covered by that provider's history API.
- **SC-003**: When a second provider is connected, all existing snapshots reflect contributions from both providers within 5 minutes.
- **SC-004**: The backfill job completes within 5 minutes for a user with up to 3 connected providers and up to 2 years of history.
- **SC-005**: Running the backfill job twice for the same user produces byte-identical snapshot records (idempotency).

## Assumptions

- Provider API credentials are already stored from the account connect flow — no new credential handling is required.
- The existing `net_worth_snapshots` unique constraint on (user_id, snapshot_date) is retained; the service layer gains a delete-and-recreate path alongside the existing no-op-on-exist path.
- Currency conversion to USD uses the existing static rate table in Core — historical exchange rates at specific past dates are out of scope.
- Only the first sync completion triggers a backfill. Subsequent syncs (recurring monthly) do not re-trigger a full recompute.
- IBKR ibeam session management is handled by the existing IBKR infrastructure; the backfill job consumes the same session.
- No frontend changes are required — the existing line chart and range selector consume whatever snapshots are present.

## Notes

- [DECISION] Recompute strategy: Full delete-and-recreate chosen over additive updates. Rationale: avoids double-counting when multiple providers share an asset class column (e.g., Plaid + Monobank both contribute to banking_total), and eliminates stale-state bugs on disconnect/reconnect cycles.
- [DECISION] Granularity: Monthly snapshots only (last day of month). Rationale: consistent with feature 015; daily granularity would require chart and schema changes and is deferred.
- [DECISION] Atomicity: The delete step runs at the beginning of a successful job execution, not as a pre-step. If provider API calls fail after deletion, Hangfire retries the full job including the delete — the user may briefly see an empty chart during retry, which is acceptable.
- [OUT OF SCOPE] Plaid historical backfill: No Plaid balance history API exists. Banking contributions from Plaid accounts will be 0 in historical snapshots.
- [OUT OF SCOPE] Historical FX rates: Static conversion rates used for all historical snapshots.
- [DEFERRED] Manual backfill re-trigger: No UI button in v1. Automatic trigger on first sync is sufficient.
- [DEFERRED] Disconnect-triggered recompute: Snapshots are not automatically recomputed when a provider is disconnected. This may be revisited if user confusion arises.
