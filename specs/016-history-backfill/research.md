# Research: Historical Net Worth Backfill

**Feature**: 016-history-backfill  
**Date**: 2026-05-03

---

## Decision 1: Backfill job trigger point

**Decision**: Inject `IHistoricalBackfillScheduler` into each provider `Connect*Command` handler and call `ScheduleForUser(userId)` after a successful first connect. Do not rely on `AccountSyncCompletedEvent`.

**Rationale**: `AccountSyncCompletedEvent` is only published by BankSync (Plaid / Monobank). Binance and IBKR connect commands run their first sync inline and never publish that event. Triggering from the connect handlers gives a single, consistent pattern across all three providers without cross-module event coupling. Recurring sync jobs (`BinanceSyncJob`, `IBKRSyncJob`, `ScheduledSyncService`) do not call the connect handlers, so they naturally don't re-trigger backfill.

**Alternatives considered**:
- Publish `AccountSyncCompletedEvent` from Binance/IBKR sync handlers → requires cross-module event reuse and wires backfill into the recurring sync path, requiring an `IsFirstSync` guard.
- New `ProviderFirstSyncCompletedEvent` in Core → adds an extra hop with no benefit over calling the scheduler directly.

---

## Decision 2: Provider history abstraction

**Decision**: Introduce `IProviderMonthlyHistorySource` in Core. Each provider module implements it. `HistoricalBackfillJob` injects `IEnumerable<IProviderMonthlyHistorySource>` and calls all of them.

**Rationale**: `HistoricalBackfillJob` lives in the Wealth module and cannot reference BinanceCredential, MonobankCredential, or IBKRCredential (cross-module domain objects). Going through a Core interface keeps modules decoupled, matches the existing pattern (`IBankingTotalsReader`, `ICryptoHoldingsReader`, `IBrokerageHoldingsReader`), and lets new providers be added without touching the job.

**Alternatives considered**:
- Separate command/query per provider injected into the job → tighter coupling, more DI complexity.
- Wealth module reads credentials directly via shared EF context → violates modular monolith boundary.

---

## Decision 3: Binance history API

**Decision**: Use `GET /sapi/v1/accountSnapshot?type=SPOT&limit=30` to retrieve daily spot balance snapshots. Pick the last entry per calendar month. Add `GetAccountSnapshotAsync` to `BinanceHttpClient`.

**Rationale**: This is the only Binance API that returns historical balance snapshots rather than current balances. The endpoint supports `startTime`/`endTime` parameters allowing up to 30 days per request; a single 30-day request covers 1–2 calendar months.

**Limitations**: Maximum lookback is 30 days per request. Multiple requests with sliding windows could extend this, but the spec is silent on extended history. A single 30-day window is used for v1.

---

## Decision 4: IBKR history API

**Decision**: Use `GET /v1/api/portfolio/{accountId}/performance` from the IBeam Client Portal Gateway. This returns an NAV (net asset value) time series keyed by date strings. Pick the last entry per calendar month across all sub-accounts. Add `GetPerformanceAsync` to `IBKRGatewayClient`.

**Rationale**: The IBKR Client Portal API exposes this endpoint under the portfolio group. It is the only endpoint that returns time-series NAV data without requiring daily polling.

**Alternatives considered**:
- Summing current positions per day → not available retrospectively.
- IBKR Flex Queries → requires additional IBKR configuration outside of the ibeam session model.

---

## Decision 5: Monobank history

**Decision**: Reuse existing `MonobankHttpClient.GetStatementsAsync`. Chain 31-day windows backward from today (matching the existing 90-day initial import pattern in `MonobankAdapter`). Each transaction has a `Balance` field (post-transaction kopeck balance). Take the last transaction's balance per calendar month per account, convert kopecks → decimal, then apply `CurrencyConverter.ToUsd`.

**Rationale**: `MonobankHttpClient` already implements the rate-limit retry (0 → 60 → 120 seconds). The existing `MonobankAdapter.SyncTransactionsAsync` already chains 31-day windows. Reusing the same pattern for history is consistent. The 60-second inter-window delay (FR-010) is already handled by the retry loop; the backfill job introduces an explicit `Task.Delay(60_000)` between window requests to be safe.

---

## Decision 6: Delete-and-recreate atomicity

**Decision**: `HistoricalBackfillJob` calls `INetWorthSnapshotService.ReplaceAllSnapshotsAsync(userId, snapshots)` which does:
1. `DeleteAllByUserIdAsync(userId)` — hard-delete all snapshots for the user
2. Bulk-insert all new snapshots in a single EF transaction

A Hangfire job failure after step 1 but before step 2 completes will leave the user with an empty chart. On retry, step 1 is a no-op (nothing to delete) and step 2 re-inserts. The user may briefly see an empty chart during retry, which is accepted per spec Notes.

**Rationale**: The spec explicitly accepts this behaviour: "the user may briefly see an empty chart during retry, which is acceptable." Wrapping in a DB transaction protects against partial inserts within a single run.

---

## Decision 7: No new DB migration

**Decision**: No schema changes. `DeleteAllByUserIdAsync` is a plain `DELETE WHERE user_id = @userId` on the existing `net_worth_snapshots` table.

**Rationale**: The backfill reuses the existing schema. The new service method only adds a write path (delete + re-insert) on the same table.

---

## Decision 8: `IHistoricalBackfillScheduler` placement

**Decision**: Define in `FinanceSentry.Core/Interfaces/IHistoricalBackfillScheduler.cs`. Implement as `HistoricalBackfillJobScheduler` in the Wealth module (mirrors the existing `INetWorthSnapshotJobScheduler` / `NetWorthSnapshotJobScheduler` pattern).

---

## Open Questions (resolved)

| Question | Resolution |
|----------|-----------|
| Does Binance `/sapi/v1/accountSnapshot` require a SPOT-specific permission? | No — it requires the general "Read Info" permission, same as `/api/v3/account`. |
| Does IBKR performance endpoint work when ibeam session is not authenticated? | No — same as existing adapter. The job fails and Hangfire retries; ibeam re-auth is out of scope. |
| Are Monobank accounts in different currencies summed correctly? | Yes — `MonobankHttpClient.KopecksToDecimal` + `CurrencyConverter.ToUsd` converts each account to USD before summing. |
