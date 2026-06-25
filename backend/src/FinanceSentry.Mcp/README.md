# FinanceSentry.Mcp

Model Context Protocol (MCP) server for Finance Sentry. Exposes a read-only ledger surface over HTTP/SSE so AI assistants (Claude, Cursor, etc.) can query portfolio data without any mutation risk.

## Transport

HTTP SSE via `ModelContextProtocol.AspNetCore 0.9.0-preview.2`. The MCP endpoint is served at `/mcp` (default).

## MCP Ledger Surface

| Tool name              | Status | Description |
|------------------------|--------|-------------|
| `get_identity`         | stub   | Current user identity info |
| `get_snapshot`         | stub   | Latest net-worth snapshot |
| `get_accounts`         | stub   | All linked financial accounts |
| `get_transactions`     | stub   | Recent transactions across all accounts |
| `get_bank_sync_status` | stub   | BankSync module sync health |
| `get_cash_positions`   | stub   | Cash account balances |
| `get_crypto_positions` | stub   | CryptoSync portfolio holdings |
| `get_brokerage_positions` | stub | BrokerageSync equity holdings |
| `get_budget_status`    | stub   | Budget consumption for the current period |
| `get_alerts`           | stub   | Active financial alerts |
| `get_subscriptions`    | stub   | Detected active subscriptions |
| `get_dashboard_summary` | stub  | Dashboard KPI summary |
| `get_spending_report`  | stub   | Spending report for the current period |
| `get_data_quality`     | stub   | Data quality flags across all sources |
| `search_transactions`  | stub   | Full-text search across transactions |
| `get_metadata`         | **live** | Server version and tool catalog |

All stub tools return `{ status: "not_yet_available", tool: "<name>", message: "..." }` until wired to live data sources.

## Read-only contract

No tool may perform INSERT/UPDATE/DELETE or call any HTTP POST/PUT/PATCH/DELETE endpoint. This is enforced structurally — `LedgerTools` contains no write methods — and verified by the `McpContractTests.NoToolHasMutationVerbs` contract test.

## Running locally

```bash
cd backend && dotnet run --project src/FinanceSentry.Mcp
```

Requires `FINANCE_SENTRY_API_URL` and `FINANCE_SENTRY_API_TOKEN` in the environment (see `.env.example` in `backend/`).
