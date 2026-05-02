# Feature Specification: Net Worth History Chart

**Feature Branch**: `015-net-worth-history`
**Created**: 2026-05-02
**Status**: Draft
**Input**: User description: "Dashboard net worth history chart. Currently 13 hardcoded monthly data points. Need to decide: periodic Hangfire snapshots vs on-demand aggregation from existing data. This decision shapes the schema entirely."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Net Worth Over Time on Dashboard (Priority: P1)

A user opens the Dashboard and sees a chart showing how their total net worth has changed month by month over the past 12 months, broken down by asset class (banking, brokerage, crypto).

**Why this priority**: This is the primary purpose of the feature; replacing hardcoded mock data with real historical data.

**Independent Test**: With the app running for ≥ 1 month, the chart shows at least the current month's snapshot. With ≥ 3 months, a visible trend line appears.

**Acceptance Scenarios**:

1. **Given** the user has connected accounts and at least one historical snapshot exists, **When** they view the Dashboard, **Then** the net worth chart displays real data points with correct totals per asset class.
2. **Given** snapshots exist for multiple months, **When** viewing the chart, **Then** each month's bar/line reflects the balance at the time of the snapshot, not the current balance.
3. **Given** only the current month's snapshot exists (new user), **When** viewing the chart, **Then** a single data point is shown and a message indicates history will grow over time.
4. **Given** no snapshots exist yet (brand new account, no sync has run), **When** viewing the chart, **Then** an empty state is shown rather than mock data.

---

### User Story 2 - Historical Accuracy After Account Changes (Priority: P2)

When a user connects or disconnects an account, past snapshots are unaffected and continue to reflect the balances at the time they were taken.

**Why this priority**: Immutability of historical data is essential for the chart to be meaningful.

**Independent Test**: Disconnect an account; verify that historical snapshots still show the same totals as before the disconnect.

**Acceptance Scenarios**:

1. **Given** historical snapshots exist, **When** a user disconnects an account, **Then** past snapshot totals do not change.
2. **Given** a user connects a new account mid-year, **When** viewing the chart, **Then** months before the account was connected do not include that account's balance.

---

### User Story 3 - Chart Date Range Selection (Priority: P3)

A user can adjust the chart's date range (e.g. 3 months, 6 months, 1 year, all time) to focus on different periods.

**Why this priority**: Useful for context but not required for the core value of showing real historical data.

**Independent Test**: Selecting "3 months" shows only the last 3 snapshot data points.

**Acceptance Scenarios**:

1. **Given** more than 12 months of snapshots exist, **When** the user selects "1 year", **Then** only the last 12 data points are shown.
2. **Given** fewer snapshots exist than the selected range, **When** viewing the chart, **Then** all available snapshots are shown without error.

---

### Edge Cases

- What if no sync has ever run? → Empty state shown; no mock data.
- What if a snapshot job fails for a month? → That month has no data point; a gap in the chart is shown rather than interpolated data.
- What if all accounts are disconnected? → Historical snapshots remain intact; future months show $0.
- What currency is used? → User's base currency; no multi-currency conversion in v1.
- What if balances change within a month? → Only the end-of-month snapshot is stored; intra-month variation is not captured.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST take a net worth snapshot at the end of each calendar month, capturing total balances per asset class (banking, brokerage, crypto) for each user.
- **FR-002**: Snapshots MUST be immutable once written; no retroactive modification when accounts change.
- **FR-003**: The Dashboard chart MUST display real snapshot data, not hardcoded values.
- **FR-004**: Each data point MUST show the total net worth and the breakdown by asset class (banking, brokerage, crypto).
- **FR-005**: The chart MUST show an empty state when no snapshots exist for the user.
- **FR-006**: The chart MUST display a visible gap (no interpolation) for months where no snapshot exists.
- **FR-007**: Users MUST be able to select a date range for the chart (3m, 6m, 1y, all).
- **FR-008**: The snapshot job MUST run after all provider syncs for the month have completed to capture up-to-date balances.
- **FR-009**: A snapshot MUST be taken immediately upon first sync (to provide a starting data point as soon as possible).

### Key Entities *(include if feature involves data)*

- **NetWorthSnapshot**: Belongs to a user; has snapshot date (end of month), total net worth, banking total, brokerage total, crypto total, currency. Immutable once written.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The chart shows zero hardcoded values; all data points come from stored snapshots.
- **SC-002**: Historical totals remain unchanged after account disconnection or reconnection.
- **SC-003**: A new user sees their first real data point within 24 hours of connecting their first account.
- **SC-004**: The chart renders with correct values matching the sum of account balances at the time of each snapshot, verifiable against sync logs.

## Assumptions

- Periodic Hangfire snapshots are used (not on-demand aggregation). Rationale: on-demand aggregation from transaction history would be approximate for brokerage/crypto where holdings value changes independently of transactions; snapshots capture actual synced balances at a point in time.
- Snapshots are taken monthly (end of month) via a Hangfire recurring job.
- An initial snapshot is also triggered immediately after a user's first successful sync.
- The snapshot stores pre-computed totals per asset class; no re-computation needed at read time.
- History before the feature is deployed does not exist; the chart will grow over time starting from the first snapshot.
- Base currency is the user's profile currency; no cross-currency conversion in v1.

## Notes

- [DECISION] Periodic snapshots over on-demand aggregation: snapshots capture the true synced balance at a point in time, including brokerage and crypto holdings whose value changes without transactions. On-demand aggregation from transactions would be unreliable for these asset classes.
- [DECISION] Monthly granularity: balances a useful historical view against storage cost. Daily snapshots are a v2 option if demand exists.
- [OUT OF SCOPE] On-demand historical balance calculation from transactions.
- [OUT OF SCOPE] Intra-month balance history (daily granularity).
- [OUT OF SCOPE] Multi-currency snapshot storage.
- [DEFERRED] Backfill of historical data from transaction history for banking accounts — would give richer history on first use but is complex and approximate.
