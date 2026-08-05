# Data Model: 037 Structured Data Sources (free re-scope)

One new entity; everything else is reuse. `analyst_actions` is untouched (per-action data stays MarketBeat-sourced).

## New entity: `RecommendationTrend` (`research.recommendation_trends`, migration **M010_RecommendationTrends**)

Monthly analyst-consensus aggregate per ticker, from Finnhub `/stock/recommendation`. Global market data — no `UserId` (precedent: `AnalystAction`, `NewsArticle`, `QuoteCacheEntry`).

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` PK | |
| `Ticker` | `string` (indexed) | Normalized upper-case, canonical symbol (dots kept — Finnhub accepts `BRK.B`; verify in contract test) |
| `Period` | `DateOnly` | First day of the consensus month, from Finnhub `period` ("YYYY-MM-01") |
| `StrongBuy` | `int` | |
| `Buy` | `int` | |
| `Hold` | `int` | |
| `Sell` | `int` | |
| `StrongSell` | `int` | |
| `Source` | `string` | `finnhub` (constant for now; column keeps SC-003 provider-swap parity with `analyst_actions`) |
| `IngestedAt` | `DateTimeOffset` | Last capture/update time |

**Constraints**: unique index `(Ticker, Period)` — refetch upserts in place (Finnhub restates recent months). Append-only across periods = the trend history.

**Validation**: counts ≥ 0; rows with all five counts = 0 are skipped (no coverage — Debug log, FR-003).

## Reused (unchanged)

- **`AnalystUniverseMember`** — capture set = members whose `Reason ∈ {Holding, Watchlist, Candidate, Manual}` (the existing `ValuationCaptureReasons` filter). Index seed (450 S&P) is *not* swept nightly (research.md R5).
- **`AnalystAction`** — untouched. Row deletion is not part of US2: historical `source = 'yahoo'` rows remain valid history.
- **`IAnalystSourceHealth`** — Finnhub capture failures recorded under source key `finnhub`, 2-strike alert path unchanged.

## New configuration (not persisted)

```jsonc
"AnalystSources": {
  "Marketbeat": { "Enabled": true },          // FR-004 reversibility for the remaining scraper
  "Finnhub": {
    "ApiKey": "",                             // FINNHUB_API_KEY env; empty ⇒ trends capture off (Debug line)
    "BaseUrl": "https://finnhub.io/api/v1",
    "RequestsPerMinute": 50                   // guard; free cap 60/min (+30/sec global)
  }
}
```

## Removed

- `YahooAnalystActionsSource` (class, DI registration, named HttpClient `yahoo-analyst`, tests, fixtures). No schema impact.

## State transitions

None — upsert by `(Ticker, Period)`; no lifecycle states.
