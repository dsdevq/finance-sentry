# Quickstart: 037 Structured Data Sources (free re-scope)

## 1. Get a Finnhub key (free)

1. Register at <https://finnhub.io/register> (free tier — no card).
2. Copy the API key from the dashboard.

## 2. Configure

Local dev (`docker/.env`):

```bash
FINNHUB_API_KEY=<your key>
```

Prod: add `FINNHUB_API_KEY` to `docker/.env.sops` — compose maps it to `AnalystSources__Finnhub__ApiKey` on the `api` service.

**No key?** Nothing breaks and nothing spams: trends capture is skipped with one Debug line; MarketBeat ingestion runs as always (FR-002/FR-003).

## 3. Run the capture

Hangfire dashboard (<http://localhost:5001/hangfire>) → Recurring jobs → **`analyst-actions-ingestion`** → Trigger now.

Expected logs (Grafana → fs-logs, or `docker logs finance-sentry-api`):

```
Analyst source marketbeat: {N} fetched, {M} new
Recommendation trends captured for {X}/{Y} tracked tickers
```

And **no** `yahoo` analyst source lines and no crumb/404 warnings — that scraper is gone (US2).

## 4. Verify the data

```sql
-- trends landed for the tracked set
SELECT ticker, period, strong_buy, buy, hold, sell, strong_sell, ingested_at
FROM research.recommendation_trends
ORDER BY ticker, period DESC LIMIT 30;

-- one row per ticker+month, restated months updated in place
SELECT ticker, COUNT(*) AS months, MAX(period) AS latest
FROM research.recommendation_trends GROUP BY ticker ORDER BY ticker;
```

Migration check: `M010_RecommendationTrends` present in `research.__ef_migrations_history_research`.

## 5. Verify the retirement (US2 / SC-001)

Over the following days, fs-logs should show **zero** occurrences of:
- `Yahoo analyst-actions` (any level)
- `Yahoo getcrumb returned`

Per-action ingestion health: `analyst_actions` keeps receiving `source = 'marketbeat'` rows; historical `source = 'yahoo'` rows remain as history.

## 6. MCP check (US3)

Via Ledger or MCP inspector, call `get_analyst_actions` with `ticker: "MU"` — response now carries a `recommendationTrends` block (latest months' consensus counts) alongside the per-action rows.

## 7. Tests

```bash
# in the sdk:10.0 container, as usual
dotnet test backend/tests/FinanceSentry.Modules.Research.Tests --filter "FullyQualifiedName~Finnhub"
```

Contract test parses the recorded fixture on every run; the live smoke runs only when `FINNHUB_API_KEY` is set.
