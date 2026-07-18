# Finance Sentry — Agent Harness

## Quick-start

```bash
# Restore + build (required on a fresh clone or after switching branches)
cd backend && dotnet restore FinanceSentry.sln
dotnet build FinanceSentry.sln --no-restore -c Release

# Run tests (excludes DB-dependent integration tests that need a live Postgres)
dotnet test FinanceSentry.sln --no-build -c Release --filter "Category!=Integration"
```

## Project layout

```
backend/
  src/
    FinanceSentry.API/               # ASP.NET Core entry point, DI, middleware
    FinanceSentry.Core/              # CQRS interfaces (ICommand, IQuery, etc.), shared domain primitives
    FinanceSentry.Infrastructure/    # Encryption, cross-cutting infra
    FinanceSentry.Mcp/               # MCP server (7 tools over stdio or HTTP)
    FinanceSentry.Modules.Auth/      # Auth module (JWT, Google OAuth, Identity)
    FinanceSentry.Modules.BankSync/  # Plaid + Monobank adapter, accounts, transactions
    FinanceSentry.Modules.CryptoSync/# Binance adapter, crypto holdings
    FinanceSentry.Modules.BrokerageSync/ # IBKR adapter, brokerage holdings
    FinanceSentry.Modules.Wealth/    # Aggregated net-worth queries
    FinanceSentry.Modules.Alerts/    # Alert rules + Hangfire delivery
    FinanceSentry.Modules.Budgets/   # Budget tracking
    FinanceSentry.Modules.Subscriptions/ # Detected recurring subscriptions
  tests/
    FinanceSentry.Tests.Unit/        # Pure unit tests (223 tests, no DB)
    FinanceSentry.Tests.Integration/ # Contract tests via WebApplicationFactory + mocks
                                     # DB-heavy tests tagged [Trait("Category","Integration")]
    FinanceSentry.Mcp.Tests/         # MCP tool contract + schema tests (43 tests)
```

## Test strategy

- **Unit tests** (`FinanceSentry.Tests.Unit`): pure, no DB. Always fast.
- **Contract/integration tests** (`FinanceSentry.Tests.Integration`): use `WebApplicationFactory<Program>`, mock all external I/O (repos via Moq, DB contexts replaced with `UseInMemoryDatabase`). DB-heavy tests are tagged `[Trait("Category","Integration")]` and skipped with `--filter Category!=Integration`.
- **MCP tests** (`FinanceSentry.Mcp.Tests`): contract + schema tests for MCP tools; all 43 run in <5 s with no DB.

The mandatory filter for CI without a live Postgres: `--filter "Category!=Integration"`.

## Key patterns

### HTTP response shapes (contract tests drive these)
Contract tests define `*Shape` records local to the test file and assert on them. If a new field is added to a test's response shape, the corresponding `*Result` record in the Application layer must gain that field too — mismatch causes a subtle null-deserialization failure, not a compile error.

Example of such a mismatch that was fixed: `ConnectBinanceResult` was missing `Message`; the contract test expected `body.Message.Contains("connected")` but got null because JSON deserialised the missing key as null.

### CQRS (MediatR-lite)
- Commands: `ICommand<TResult>` → `ICommandHandler<TCommand, TResult>`
- Queries: `IQuery<TResult>` → `IQueryHandler<TQuery, TResult>`
- All live under `Application/Commands/` or `Application/Queries/` in each module.

### Auth middleware
`JwtAuthenticationMiddleware` reads `fs_access_token` cookie. In tests, inject the JWT via `client.DefaultRequestHeaders.Add("Cookie", $"fs_access_token={jwt}")`.

### Encryption
`ICredentialEncryptionService` is injected in handlers that store API keys. The test harness wires settings:
```
Encryption:CurrentKeyVersion = "1"
Encryption:Keys:1 = "<base64-key>"
Deduplication:MasterKeyBase64 = "<base64-key>"
```

## Gotchas

- `dotnet build --no-restore` fails on a fresh clone — always restore first.
- In-memory DB per test class: each `WebApplicationFactory` subclass uses a unique GUID database name to avoid cross-test state bleed.
- `MockBehavior.Loose` is used in factory mocks; setup only what the specific test path needs.
- `[Trait("Category","Integration")]` is the convention for skipping DB-live tests — don't change it.

## MCP Verification

Verified 2026-06-27 via `dotnet test FinanceSentry.sln --filter 'Category!=Integration' -c Release`.

| Project | Passed | Failed |
|---|---|---|
| FinanceSentry.Tests.Unit | 223 | 0 |
| FinanceSentry.Mcp.Tests | 43 | 0 |
| FinanceSentry.Tests.Integration (Category!=Integration, 4 skipped) | 128 | 0 |

### Registered MCP tools (11 total)

**Real tools (7)** — all implement `IReadOnlyMcpTool`, non-destructive:
1. `get_account_summary`
2. `list_transactions`
3. `get_budget_status`
4. `list_active_alerts`
5. `get_portfolio_snapshot`
6. `list_subscriptions`
7. `get_sync_health`

**Stubs (4)** — return `{ status: "not_yet_available", reason: string }`:
8. `get_crypto_pnl_detail`
9. `get_tax_lots`
10. `get_cashflow_report`
11. `get_net_worth_history`

Full tool catalogue (input parameters, return schemas, real/stub): [`docs/mcp.md`](docs/mcp.md).

## Frontend test environment gotcha

`ng test @dsdevq-common/ui` and `ng test finance-sentry` both require Chromium (Playwright browser runner).
The sandbox agent environment is missing `libXfixes.so.3` — the browser cannot launch.
The test suite BUILDS without error (all TypeScript compiles); execution is blocked by the missing system lib.
This is not an npm/node issue — `npm install` is fine; root access is needed for `apt-get install libxfixes3`.

Workaround: run `npm run test:lib` on a machine with full browser deps. The pre-commit hook (lint + format, no tests) passes in the sandbox.
