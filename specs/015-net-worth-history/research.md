# Research: Net Worth History Chart (015)

## Decision 1: Periodic snapshots vs on-demand aggregation

**Decision**: Periodic Hangfire snapshots (already decided in spec).

**Rationale**: Brokerage and crypto holdings change in value independently of transactions (market prices fluctuate). On-demand aggregation from transaction history would only capture cash flow, not investment appreciation/depreciation. Snapshots capture the true synced balance at a point in time across all asset classes.

**Alternatives considered**: On-demand aggregation from transactions — rejected because it is unreliable for brokerage/crypto.

---

## Decision 2: Where does NetWorthSnapshotJob live?

**Decision**: `NetWorthSnapshotJob` in `FinanceSentry.Modules.BankSync`.

**Rationale**: BankSync has direct access to `IAggregationService` (banking totals), and the Core interfaces `ICryptoHoldingsReader` and `IBrokerageHoldingsReader` are already injectable there (same pattern as `DashboardQueryService`). Moving the job to a new module would require elevating `IAggregationService` to a Core interface, adding churn. The job calls `INetWorthSnapshotService` (new Core interface) to persist — this is the identical cross-module pattern used by `SubscriptionDetectionJob` and `ISubscriptionDetectionResultService`.

**Alternatives considered**: Job in new `NetWorthHistory` module — would require a new `IBankBalanceSummaryReader` Core interface; more files, same result.

---

## Decision 3: Monthly snapshot date convention

**Decision**: Last calendar day of the month (e.g., `2026-04-30`, `2026-03-31`).

**Rationale**: Consistent reference point for the "end of month" summary. The Hangfire cron `0 1 L * *` (1am on the last day of each month) fires once and the job computes `DateOnly.FromDateTime(DateTime.UtcNow)`. Idempotent: unique constraint `(userId, snapshotDate)` prevents duplicates on re-runs.

**Alternatives considered**: First day of next month — rejected, creates confusion (April balance stored as May 1).

---

## Decision 4: How to trigger first-sync snapshot (FR-009)

**Decision**: MediatR `INotificationHandler<AccountSyncCompletedEvent>` in BankSync — `FirstSyncSnapshotTrigger`. Handler resolves `UserId` from `AccountId` via `IBankAccountRepository`, then calls `INetWorthSnapshotService.HasSnapshotForCurrentMonthAsync(userId)`. If none exists, enqueues `NetWorthSnapshotJob` for that specific user via Hangfire `IBackgroundJobClient.Enqueue`.

**Rationale**: Keeps trigger logic inside BankSync (where the sync event originates and where account lookup is available). No new events or interface changes needed. The check prevents duplicate snapshots if multiple accounts sync in the same month.

**Alternatives considered**: 
- Extend `AccountSyncCompletedEvent` with `UserId` — would work but adds scope to this feature; the event is already extended in the 012-alerts plan.
- Daily Hangfire job checking for missing snapshots — adds polling overhead, violates FR-009 ("immediately upon first sync").

---

## Decision 5: Currency

**Decision**: Store all snapshot totals in USD. `currency` column is always `"USD"` in v1.

**Rationale**: BankSync's `IAggregationService.GetTotalNetWorthUsdAsync` already produces a USD sum across currencies. `CryptoHolding.UsdValue` and `BrokerageHolding.UsdValue` are already USD. No conversion needed; just sum three already-USD values.

**Alternatives considered**: Store in user's base currency — deferred (v2); would require currency conversion service.

---

## Decision 6: Range parameter

**Decision**: `GET /api/v1/net-worth/history?range=3m|6m|1y|all`. Default: `1y`. Backend returns only the snapshots within the range, ordered by `snapshotDate` ascending.

**Rationale**: Simple enum parameter; avoids open-ended date arithmetic on the frontend. Backend knows exactly how many months to return. `all` returns every snapshot for the user (bounded by 13 months of sync history context, though snapshots can exist forever).

**Alternatives considered**: `from`/`to` date params — more flexible but over-engineered for three fixed UI options.

---

## Decision 7: Frontend — extend DashboardStore vs new store

**Decision**: Extend existing `DashboardStore` (page-scoped) with `netWorthHistory` state, `historyRange` state, and `loadNetWorthHistory` rxMethod.

**Rationale**: Net worth history is displayed on the same page as the rest of the dashboard. A second store would need to be provided alongside DashboardStore on the same component, adding boilerplate for no benefit. The DashboardStore already loads multiple data chunks in parallel; adding history is a natural extension.

**Alternatives considered**: Separate `NetWorthHistoryStore` — rejected as over-engineering for a single chart on one page.

---

## Decision 8: Range selector UI component

**Decision**: Use existing `cmn-button` components in a toggle group within the dashboard component. The selected range is stored in `DashboardStore.historyRange`. Changing the range calls `store.setHistoryRange(range)` → triggers `loadNetWorthHistory` re-fetch.

**Rationale**: No new UI library component needed; `cmn-button` supports an active/outlined variant. Keeps component state minimal — range is a store signal, not a local variable.

**Alternatives considered**: Dropdown select — less visually ergonomic for 4 options.
