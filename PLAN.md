## Destination
One canonical `IBookFiguresReader` / `BookFiguresReader` (Core interface, Risk implementation)
owns all book-figure computation: cash (split banking/brokerage), invested value, sleeve values,
total. Three consumers — `GetPortfolioSnapshotTool`, `GetAllocationDriftQueryHandler`, Risk's
`CheckRiskRulesQueryHandler` (via `BookSnapshotReader`) — do zero independent summing.
Parity test: multi-currency book with idle brokerage cash → three surfaces return identical
cashUsd / investedValueUsd / totalValueUsd. ROADMAP backlog table trimmed to unimplemented specs.
Fixes #411.

## Decisions so far
- `IBookFiguresReader` + `BookFigures` go in **Core** so Research and Risk modules both depend on
  Core (their existing dep) without circular refs; implementation `BookFiguresReader` lives in Risk.
- `BookSnapshotReader` (Risk) adapts `IBookFiguresReader` → `BookSnapshot` to preserve the
  existing risk-module domain surface unchanged internally.
- Cost basis fields added to `BrokerageHoldingSummary` and `CryptoHoldingSummary` (Core records) so
  `BookFiguresReader` can produce per-position cost basis for the portfolio snapshot tool.
- `AssetClassNormalizer` moves to `Core/Utils/` — Research module keeps old file as a one-line
  alias so non-key usages (GetEarningsCalendarQuery etc.) compile without churn.
- `GetAllocationDriftQueryHandler` no longer reads sources directly — uses `IBookFiguresReader`,
  groups `BookFiguresPosition` by `AssetClass`, adds `BookFigures.CashUsd` to "Cash" sleeve.

## Tasks — #411 milestone
- [x] Create PLAN.md
- [ ] Core: add `AssetClassNormalizer`, `IBookFiguresReader`, `BookFigures`, `BookFiguresPosition`
- [ ] Core: extend `BrokerageHoldingSummary` + `CryptoHoldingSummary` with cost basis fields
- [ ] BrokerageSync/CryptoSync: populate cost basis in reader implementations
- [ ] Risk: `BookFiguresReader` implementing `IBookFiguresReader`
- [ ] Risk: extend `BookSnapshot` (add BankingCashUsd/BrokerageCashUsd); adapt `BookSnapshotReader`
- [ ] Risk: register `IBookFiguresReader → BookFiguresReader` in `RiskModule`
- [ ] Research: `GetAllocationDriftQueryHandler` → use `IBookFiguresReader`
- [ ] MCP: `GetPortfolioSnapshotTool` → use `IBookFiguresReader`
- [ ] Tests: `BookSnapshotReaderTests` updated for new fields
- [ ] Tests: `ToolParityTests` DI updated + `BookFiguresParityTest` added
- [ ] ROADMAP.md: drop implemented specs from backlog table
- [ ] Build + test suite green

## Out of scope
- Sleeve name vs asset-class name mismatch in `RiskEvaluationService.ComputeRawViolations` (pre-existing, separate issue)
- Extending cost basis fields in `HoldingSnapshot` persistence
- Frontend / Playwright changes
