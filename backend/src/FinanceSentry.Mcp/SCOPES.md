# Finance Sentry MCP Scopes

`FinanceSentry.Mcp` exposes Finance Sentry to MCP clients as a read-only server.

## Runtime

- Transport: stdio
- API base URL: `FINANCESENTRY_API_BASE_URL` or `FinanceSentryApi:ApiBaseUrl`
- API bearer token: `FINANCESENTRY_API_TOKEN` or `FinanceSentryApi:ApiToken`
- Default API base URL: `http://localhost:5001/api/v1/`

## Allowed Tools

The MCP server may expose query tools for:

- Wealth summaries and history
- Bank accounts and transactions
- Brokerage holdings through IBKR-compatible read names
- Crypto holdings
- Budgets and spending summaries
- Subscriptions
- Alerts
- Research placeholders where Finance Sentry has no matching API yet

## Forbidden Tools

The MCP server must not expose tools that mutate financial state or initiate external actions.

Do not add tools for:

- Placing, modifying, or cancelling orders
- Transfers, deposits, withdrawals, or currency conversion
- Connecting or disconnecting providers
- Creating, updating, dismissing, or deleting alerts, budgets, subscriptions, credentials, or accounts
- Sync triggers that call external providers

If a requested MCP capability has no safe read endpoint, expose a read-only stub that returns:

```json
{
  "status": "not_yet_available",
  "reason": "..."
}
```

Keep the MCP adapter thin. Prefer calling existing API endpoints over importing application handlers or database repositories directly.
