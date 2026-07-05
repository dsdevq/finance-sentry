# Finance Sentry MCP Server

`backend/src/FinanceSentry.Mcp` is an MCP server executable. It does not contain an MCP client. External clients connect to it over either `stdio` or streamable HTTP.

## Transports

| Transport | Selected By | Intended Client |
|---|---|---|
| `stdio` | `MCP_TRANSPORT=stdio` | Claude Desktop, `mcp-probe.sh`, any local process that can spawn `dotnet FinanceSentry.Mcp.dll` |
| `http` | `MCP_TRANSPORT=http` | Containerized clients such as OpenClaw that cannot spawn the MCP process directly |

## Identity

- `stdio` transport uses locally stored MCP OAuth credentials obtained via `dotnet FinanceSentry.Mcp.dll auth login`.
- `http` transport requires per-request authentication via `Authorization: Bearer <mcp access token>`.
- For HTTP, the MCP server resolves identity from the authenticated request user, not from a boot-time server token.
- `stdio` refresh is automatic through the MCP token endpoint and locally stored refresh token.
- HTTP clients refresh through the MCP token endpoint using dedicated MCP refresh tokens.
- The browser-based `auth login` flow is intended for a host-run `stdio` MCP process. Containerized clients should prefer HTTP MCP.

## Tool Surface

The current runtime surface contains 21 tools: 17 read-only and 4 mutating. There are no stub tools in the current source tree.

| Tool Name | Mode | Domain | Key Inputs | Notes |
|---|---|---|---|---|
| `get_account_summary` | Read | Portfolio | `userId?` | Consolidated banking, crypto, and brokerage balances |
| `list_transactions` | Read | Banking | `userId?`, `accountId?`, `fromDate?`, `toDate?`, `category?`, `page`, `pageSize` | Paginated transaction listing |
| `get_budget_status` | Read | Budgets | `userId?`, `year?`, `month?` | Budget utilization for a period |
| `list_active_alerts` | Read | Alerts | `userId?` | Only unread unresolved alerts |
| `get_portfolio_snapshot` | Read | Portfolio | `userId?` | Unified brokerage + crypto holdings |
| `list_subscriptions` | Read | Subscriptions | `userId?` | Detected recurring charges |
| `get_sync_health` | Read | Sync | `userId?` | Status across Plaid, Monobank, Binance, IBKR |
| `get_crypto_pnl_detail` | Read | Crypto | `userId?` | Per-asset crypto P&L from trade history |
| `get_tax_lots` | Read | Brokerage | `userId?` | Current tax lots / average cost data |
| `get_cashflow_report` | Read | Cashflow | `userId?`, `fromDate?`, `toDate?` | Monthly inflow / outflow / net |
| `get_net_worth_history` | Read | Wealth | `userId?`, `fromDate?`, `toDate?` | Historical net worth snapshots |
| `get_macro_calendar` | Read | Research | `from?`, `to?`, `regions?`, `minImportance?` | Scheduled macro events |
| `get_news_for_ticker` | Read | Research | `ticker`, `since?`, `limit` | Recent ticker-specific news |
| `get_quotes` | Read | Research | `tickers` | Current quotes for one or more tickers |
| `search_market_news` | Read | Research | `query?`, `tickers?`, `since?`, `limit` | Search ingested market news |
| `list_watchlist` | Read | Research | `userId?` | Stored watchlist entries |
| `list_theses` | Read | Research | `userId?` | Stored investment theses |
| `add_to_watchlist` | Write | Research | `ticker`, `exchange?`, `note?`, `userId?` | Adds a watchlist entry |
| `remove_from_watchlist` | Write | Research | `itemId`, `userId?` | Removes a watchlist entry |
| `save_thesis` | Write | Research | `ticker`, `thesisText`, `keyDataPoints`, `catalysts`, `invalidationTriggers`, `id?`, `userId?` | Creates or updates a thesis |
| `delete_thesis` | Write | Research | `id`, `userId?` | Deletes a thesis |

## Runtime Model

- The server is assembled in `Program.cs` using `AddMcpServer()`.
- Tools are discovered by assembly scan via `WithToolsFromAssembly(...)`.
- Tool identity comes from the MCP SDK attributes (`[McpServerToolType]` and `[McpServerTool]`), not from a separate local tool interface.
- Each tool is a thin MCP adapter around the existing CQRS handlers in the module projects.
- Development usually runs over `stdio`; production compose runs the same server over HTTP with request-based JWT auth.
- The longer-term OAuth migration plan is documented in `docs/mcp-oauth-roadmap.md`.
