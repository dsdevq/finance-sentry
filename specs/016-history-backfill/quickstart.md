# Quickstart: Historical Net Worth Backfill

**Feature**: 016-history-backfill  
**Date**: 2026-05-03

---

## How backfill is triggered

1. User connects a provider (Binance, IBKR, or Monobank) via the existing connect API.
2. The `Connect*Command` handler calls `IHistoricalBackfillScheduler.ScheduleForUser(userId)` after a successful first sync.
3. Hangfire enqueues `HistoricalBackfillJob.ExecuteForUserAsync(userId)`.
4. The job runs in the background (within ~5 minutes), fetches history from all connected providers, and recreates all snapshots for the user.
5. The existing net worth history chart picks up the new snapshots automatically on next load.

---

## Integration Scenario: Binance connect

```
POST /api/v1/crypto/binance/connect
  { "apiKey": "...", "apiSecret": "..." }

→ ConnectBinanceCommandHandler
    1. Validates credentials
    2. Saves encrypted credential
    3. Runs initial holdings sync (SyncBinanceHoldingsCommand)
    4. Calls IHistoricalBackfillScheduler.ScheduleForUser(userId)
       → Hangfire enqueues HistoricalBackfillJob

→ HistoricalBackfillJob (background)
    For each IProviderMonthlyHistorySource:
      - BinanceHistorySource: fetches 30-day snapshot, produces ProviderMonthlyBalance[]
      - IBKRHistorySource: if IBKR connected, fetches NAV history, produces ProviderMonthlyBalance[]
      - MonobankHistorySource: if Monobank connected, chains statement windows, produces ProviderMonthlyBalance[]

    Aggregates per month:
      banking_total = sum(banking contributions)
      brokerage_total = sum(brokerage contributions)
      crypto_total = sum(crypto contributions)

    INetWorthSnapshotService.ReplaceAllSnapshotsAsync(userId, newSnapshots)
      → DELETE FROM net_worth_snapshots WHERE user_id = @userId
      → INSERT all new snapshots in transaction
```

---

## Integration Scenario: Adding second provider (IBKR after Binance)

```
POST /api/v1/brokerage/ibkr/connect

→ ConnectIBKRCommandHandler
    1. Verifies ibeam session
    2. Creates IBKRCredential
    3. Calls IHistoricalBackfillScheduler.ScheduleForUser(userId)

→ HistoricalBackfillJob
    BinanceHistorySource: produces crypto history (already connected)
    IBKRHistorySource: produces brokerage NAV history (just connected)
    MonobankHistorySource: empty (not connected)

    All months in Binance history: crypto_total = Binance value, brokerage_total = 0
    All months in IBKR history: brokerage_total = IBKR NAV, crypto_total = 0 (unless overlap)
    Months in both: combined totals

    ReplaceAllSnapshotsAsync → existing Binance-only snapshots replaced with combined snapshots
```

---

## Retry behaviour

- Hangfire retries the job up to 2 times on failure (`[AutomaticRetry(Attempts = 2)]`).
- On retry, `ReplaceAllSnapshotsAsync` deletes whatever is there (possibly nothing if the job failed mid-way) and re-inserts from scratch.
- A failed run may leave the user with an empty chart until the retry succeeds.

---

## Manual verification (development)

To manually trigger a backfill for a user during development:

```bash
# POST to Hangfire API (requires hangfire dashboard to be accessible)
curl -X POST http://localhost:5001/hangfire/recurring/trigger \
  -H "Content-Type: application/json"
```

Or call the `IHistoricalBackfillScheduler.ScheduleForUser(userId)` from any command handler during testing.

---

## Rate limiting note (Monobank)

`MonobankHistorySource` introduces a 60-second `Task.Delay` between each 31-day statement window to comply with Monobank's 1-request-per-60-second limit. For a user with 12 months of history, the Monobank phase of the job will take ~12 minutes. The overall 5-minute success criterion (SC-004) applies to Binance and IBKR; Monobank will exceed it due to the mandatory rate limit delay.
