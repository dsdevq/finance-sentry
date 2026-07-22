# Quickstart: Companion-Mode Data Layer

**Feature**: 030-companion-data-layer

## Prereqs

Full Docker stack running (`cd docker && docker compose -f docker-compose.dev.yml up -d`); API healthy at `http://localhost:5001/api/v1/health`.

## Verify P1 — analyst actions

```bash
# 1. Migration applied (look for M008)
docker exec finance-sentry-postgres psql -U finance_user -d finance_sentry \
  -c "SELECT \"MigrationId\" FROM public.__ef_migrations_history_research ORDER BY 1;"

# 2. Trigger the ingestion job once (Hangfire dashboard http://localhost:5001/hangfire
#    → Recurring → analyst-actions-ingestion → Trigger now), then:
docker exec finance-sentry-postgres psql -U finance_user -d finance_sentry \
  -c "SELECT \"Ticker\",\"Firm\",\"ActionType\",\"NewTarget\",\"ActionDate\",\"Source\" FROM research.analyst_actions ORDER BY \"IngestedAt\" DESC LIMIT 10;"

# 3. MCP tool (via Ledger drive or MCP inspector): get_analyst_actions {"ticker":"MU","since":"<30d ago>"}
#    Expect: sourced rows, coverage flag, no fabricated fields.
```

## Verify P2 — valuation snapshot

```bash
# MCP: get_valuation_snapshot {"ticker":"MCD"}
# Expect: trailingPe with fiveYearAvg (EDGAR+closes), forwardPe/evToEbitda with
#         historyUnavailable:true, consensusTarget + impliedUpsidePct, named peer set.
# MCP: get_valuation_snapshot {"ticker":"SOL"}  → explicit notApplicable (crypto).
docker exec finance-sentry-postgres psql -U finance_user -d finance_sentry \
  -c "SELECT \"Ticker\",\"TrailingPe\",\"ForwardPe\",\"CapturedAt\" FROM research.valuation_snapshots ORDER BY \"CapturedAt\" DESC LIMIT 5;"
```

## Verify P3 — thesis sources

```bash
# MCP: list_news_sources → seeded market-wide feeds + TrendForce→DRAM row
# MCP: register_thesis_source {"thesisId":"<dram-thesis-id>","name":"Test","url":"<rss>","kind":"Rss"}
# Trigger research-news-tickers job, then:
docker exec finance-sentry-postgres psql -U finance_user -d finance_sentry \
  -c "SELECT \"Title\",\"Source\",\"ThesisIds\" FROM research.news_articles WHERE \"ThesisIds\" IS NOT NULL ORDER BY \"IngestedAt\" DESC LIMIT 5;"
# MCP: search_market_news {"thesisId":"<dram-thesis-id>"} → returns tagged articles
```

## Verify failure alerting (FR-009)

```bash
# Point a test source at an unreachable URL, run ingestion twice, then:
# MCP: list_active_alerts → sync-failure alert referencing the source
docker exec finance-sentry-postgres psql -U finance_user -d finance_sentry \
  -c "SELECT \"ConsecutiveFailures\",\"LastFailureReason\" FROM research.news_sources;"
```

## Gates

- `dotnet build backend/` — zero warnings (constitution II)
- Contract tests green: Yahoo upgradeDowngradeHistory, quoteSummary valuation modules, MarketBeat fixture, TrendForce fixture
- Unit tests: dedup identity, TTM EPS → trailing P/E series, universe sync, keyword tagging, failure-counter threshold
- Backend version bump in `FinanceSentry.API.csproj` in the same PR
