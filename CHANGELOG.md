# Changelog

All notable changes to Finance Sentry are documented here. The format follows
[Conventional Commits](https://www.conventionalcommits.org/) and versions follow
[Semantic Versioning](https://semver.org/). Entries from v0.12.0 onward are
generated automatically by [release-please](https://github.com/googleapis/release-please).

## 1.0.0 (2026-08-06)

First stable release. Finance Sentry is a personal finance aggregation platform —
an ASP.NET Core (.NET 10) modular monolith + Angular 21 SPA — that consolidates
banking, brokerage, and crypto accounts into one net-worth, spending, and research
view. All golden-path flows (login, accounts, dashboard, transactions, holdings,
budgets, subscriptions) are verified end-to-end against live data.

### Features

- **Research suite (companion data layer)**: analyst actions, valuation snapshots,
  thesis-source news (030); structured analyst data via Finnhub recommendation
  trends, retiring the Yahoo scraper (037); retrieval + RAG context layer with
  `search_research_corpus` / `get_research_context` MCP tools (036); opportunity
  scan job with machine nomination from market structure (019); MCP tool-surface
  refinement (035); guarded read-only analytics query tool (033)
- **Companion notifications**: notification modes + event-driven push (031)
- **Observability stack**: OpenTelemetry, Loki, Prometheus, Grafana dashboards,
  Hangfire-on-Postgres, job-failure Telegram alerts (023)
- **Brokerage**: IBKR holdings via tier-1 Portal session + OAuth
- **FX**: live exchange rates with daily refresh and offline fallback
- **Installments**: dedicated section with smarter detection + management
- **UI library**: new `@dsdevq-common/ui` composites — `cmn-page-header`,
  `cmn-page-container`, `cmn-tab-group`, `cmn-empty-state`, `cmn-disclosure-row`,
  `cmn-editable-field`, `cmn-list-item-row`, `cmn-select`; provider logos,
  per-asset holding icons, app version in the sidebar footer

### Bug Fixes

- **Categorization**: classify Monobank savings-jar top-ups as transfers, not
  government spend; categorize TrueLayer transactions from description text;
  case-insensitive transfer bucketing
- **TrueLayer**: self-healing reconnect, stale-sync reaper, dedup crash fix,
  rotated-refresh-token persistence, inline history-fetch + pre-expiry reminder
- **Reliability**: stop silent cron-failure loops (TrueLayer, TrendForce, Yahoo);
  treat provider 429s as transient (no false SyncFailure alerts)
- **Holdings/subscriptions**: drop zero-quantity and sold-out positions; exclude
  installments and canonicalize noisy merchant brands; USD-normalized monthly cost
- **Frontend**: green lint-build-test enforced pre-commit; alerts page no longer
  crashes on unmapped alert types; chart tooltips + URL-persisted dashboard range
- **Portfolio/quotes**: quote data-quality fixes; missing migration Designer for
  `M007_QuoteSessionMetadata`

## 0.11.0 (2026-07-09)

Baseline release — consolidation of everything built to date under a single
application version (previously the frontend and backend were versioned
independently, last tagged `frontend-v0.4.0` / backend `0.11.0`).

### Highlights

- Multi-provider aggregation: Plaid, Monobank, TrueLayer, Binance, IBKR
- Auth with email/password + Google OAuth, JWT + httpOnly refresh cookie
- Dashboard, transactions, budgets, alerts, subscription detection
- Net worth history snapshots
- Research suite: investment theses, thesis monitor, thesis track record, market structure radar, opportunity scanner, risk rules
- Read-only MCP server (`FinanceSentry.Mcp`) exposing financial data to MCP clients
- Full Docker stack + CI + automated VPS deployment
