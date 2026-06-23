# IBKR Parity Matrix

This MCP server provides a read-only Finance Sentry surface with IBKR-compatible tool names where Finance Sentry has equivalent data.

| MCP tool | Finance Sentry source | Status |
| --- | --- | --- |
| `get_account_summary` | `GET /api/v1/brokerage/holdings` | Available |
| `get_account_balances` | `GET /api/v1/brokerage/holdings` | Available |
| `get_account_positions` | `GET /api/v1/brokerage/holdings` | Available |
| `get_account_trades` | None | Stubbed |
| `get_pa_allocation` | None | Stubbed |
| `get_pa_performance_all_periods` | None | Stubbed |
| `search_contracts` | None | Stubbed |
| `get_price_history` | None | Stubbed |

## Non-IBKR Finance Tools

| MCP tool | Finance Sentry source | Status |
| --- | --- | --- |
| `get_net_worth` | `GET /api/v1/wealth/summary` | Available |
| `get_net_worth_history` | `GET /api/v1/net-worth/history` | Available |
| `get_cashflow_summary` | `GET /api/v1/wealth/transactions/summary` | Available |
| `get_all_accounts` | `GET /api/v1/accounts` | Available |
| `get_bank_transactions` | `GET /api/v1/accounts/transactions` | Available |
| `get_crypto_positions` | `GET /api/v1/crypto/holdings` | Available |
| `get_spending_by_category` | `GET /api/v1/budgets/summary` | Available |
| `get_subscriptions` | `GET /api/v1/subscriptions` | Available |
| `get_alerts` | `GET /api/v1/alerts` | Available |
| `get_total_exposure` | None | Stubbed |
| `get_fx_exposure` | None | Stubbed |
| `get_watchlist` | None | Stubbed |
| `get_investment_thesis` | None | Stubbed |
| `get_report_calendar` | None | Stubbed |

Mutation parity is intentionally out of scope.
