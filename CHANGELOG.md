# Changelog

All notable changes to Finance Sentry are documented here. The format follows
[Conventional Commits](https://www.conventionalcommits.org/) and versions follow
[Semantic Versioning](https://semver.org/). Entries from v0.12.0 onward are
generated automatically by [release-please](https://github.com/googleapis/release-please).

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
