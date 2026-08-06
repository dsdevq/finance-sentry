---
description: "Task list for Companion-Mode Data Layer implementation"
---

# Tasks: Companion-Mode Data Layer

**Input**: Design documents from `/specs/030-companion-data-layer/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/mcp-tools.md

**Tests**: Constitution mandates external-contract tests for every new external source (MarketBeat, Yahoo modules, TrendForce) and unit tests for business logic (dedup, P/E series, universe sync, tagging, failure counters). No REST endpoints in this feature → no REST contract tests. MCP tools are thin over CQRS handlers.

**Organization**: Grouped by user story (P1 analyst actions, P2 valuation snapshot, P3 thesis-source news breadth).

## Path Conventions

- Backend module: `backend/src/FinanceSentry.Modules.Research/`
- MCP tools: `backend/src/FinanceSentry.Mcp/Tools/`
- Module tests: `backend/tests/FinanceSentry.Modules.Research.Tests/`
- MCP tests: `backend/tests/FinanceSentry.Mcp.Tests/`

**Structure note**: Service interfaces live in `Application/Services/` (e.g. `IMarketDataService`); repository interfaces in `Domain/Repositories/` — this feature follows that existing convention, NOT the `Domain/Interfaces/` folder named in plan.md.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Dependency and enum groundwork shared by all stories.

- [X] T001 Add AngleSharp NuGet package (MIT) to `backend/src/FinanceSentry.Modules.Research/FinanceSentry.Modules.Research.csproj`; run `dotnet restore backend/`
- [X] T002 [P] Add `Ledger` value to `CandidateSource` enum in `backend/src/FinanceSentry.Modules.Research/Domain/Opportunity/CandidateSource.cs` (stored-as-string; no migration) and update its XML doc
- [X] T003 [P] Add checked-in S&P 500 constituent seed resource `backend/src/FinanceSentry.Modules.Research/Infrastructure/Resources/sp500-constituents.json` and mark it `EmbeddedResource` (or copy-to-output) in the csproj

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entities, enums, DbContext wiring, and the M008 migration that ALL stories depend on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete. The migration MUST ship with its `.Designer.cs` (M007 outage lesson — plan.md "Migration discipline").

### Domain entities & enums

- [X] T004 [P] Create `AnalystAction` entity + `AnalystActionType` enum (`Upgrade`,`Downgrade`,`Initiate`,`TargetChange`,`Reiterate`,`TopIdea`) in `backend/src/FinanceSentry.Modules.Research/Domain/AnalystAction.cs` and `.../Domain/AnalystActionType.cs`
- [X] T005 [P] Create `AnalystUniverseMember` entity + `UniverseReason` enum (`IndexConstituent`,`Holding`,`Watchlist`,`Candidate`,`Manual`) in `.../Domain/AnalystUniverseMember.cs` and `.../Domain/UniverseReason.cs`
- [X] T006 [P] Create `NewsSource` entity + `NewsSourceKind` enum (`Rss`,`Page`) with `ConsecutiveFailures`/`LastSuccessAt`/`LastFailureReason`/`Keywords` in `.../Domain/NewsSource.cs` and `.../Domain/NewsSourceKind.cs`
- [X] T007 [P] Create `ValuationSnapshot` entity in `.../Domain/ValuationSnapshot.cs`
- [X] T008 [P] Add `ThesisIds` (`List<Guid>`) property to `NewsArticle` in `.../Domain/NewsArticle.cs`

### Repository interfaces & implementations

- [X] T009 [P] Create `IAnalystActionRepository` in `.../Domain/Repositories/IAnalystActionRepository.cs` (query by ticker/date-range/type; upsert-with-merge dedup) and impl `.../Infrastructure/Persistence/Repositories/AnalystActionRepository.cs`
- [X] T010 [P] Create `IAnalystUniverseRepository` in `.../Domain/Repositories/IAnalystUniverseRepository.cs` and impl `.../Infrastructure/Persistence/Repositories/AnalystUniverseRepository.cs`
- [X] T011 [P] Create `INewsSourceRepository` in `.../Domain/Repositories/INewsSourceRepository.cs` and impl `.../Infrastructure/Persistence/Repositories/NewsSourceRepository.cs`
- [X] T012 [P] Create `IValuationSnapshotRepository` in `.../Domain/Repositories/IValuationSnapshotRepository.cs` and impl `.../Infrastructure/Persistence/Repositories/ValuationSnapshotRepository.cs`

### DbContext + migration (sequential — same files)

- [X] T013 Register DbSets and EF configuration (indexes, unique `(Ticker,Firm,ActionDate,ActionType)`, jsonb converters+`StringListComparer` for `Keywords`/`ThesisIds`, enum-as-string conversions, `theses` FK SET NULL) in `.../Infrastructure/Persistence/ResearchDbContext.cs`
- [X] T014 Generate migration **M008_CompanionDataLayer** WITH its `.Designer.cs` via `dotnet ef migrations add M008_CompanionDataLayer` (adds `analyst_actions`, `analyst_universe_members`, `news_sources`, `valuation_snapshots`; alters `news_articles` + `ThesisIds`); verify snapshot updated in `.../Migrations/`
- [X] T015 Register repositories + `AddResearchModule` DI wiring for new repos in `.../ResearchModule.cs`

**Checkpoint**: `dotnet build backend/` zero warnings; migration discoverable. User stories can now begin.

---

## Phase 3: User Story 1 - Ledger cites street actions (Priority: P1) 🎯 MVP

**Goal**: Nightly ingestion of analyst actions from MarketBeat (market-wide table) + Yahoo `upgradeDowngradeHistory` (per universe ticker) with logical dedup, source attribution, market-wide universe, and a `get_analyst_actions` MCP query surface.

**Independent Test**: Trigger ingestion, query `get_analyst_actions {"ticker":"MU","since":"<30d>"}` → sourced, deduped rows; query all-universe "since yesterday" → includes non-held tickers; query a crypto ticker → explicit empty (no fabrication).

### Tests for User Story 1

- [X] T016 [P] [US1] Contract test for Yahoo `quoteSummary?modules=upgradeDowngradeHistory` JSON shape (path `quoteSummary.result[0].upgradeDowngradeHistory.history[]` with `firm`,`toGrade`,`fromGrade`,`action`,`epochGradeDate`) in `backend/tests/FinanceSentry.Modules.Research.Tests/Contract/YahooUpgradeDowngradeHistoryContractTests.cs`
- [X] T017 [P] [US1] Contract test for MarketBeat `/ratings/` HTML table structure using a recorded fixture (columns: company/ticker, action, brokerage, rating change, price target) + explicit `[Trait]`-gated live smoke test in `.../Contract/MarketBeatRatingsContractTests.cs`; add fixture `.../Contract/Fixtures/marketbeat-ratings.html`
- [X] T018 [P] [US1] Unit test for dedup logical identity + richer-record merge (two sources, rounded targets → one row) in `.../Unit/AnalystActionDedupTests.cs`
- [X] T019 [P] [US1] Unit test for `AnalystUniverseService` compose (seed ∪ holdings ∪ watchlist ∪ candidates) + deactivate-on-departure in `.../Unit/AnalystUniverseServiceTests.cs`

### Implementation for User Story 1

- [X] T020 [P] [US1] Create `IAnalystActionsSource` interface (`Task<IReadOnlyList<AnalystActionRecord>> FetchAsync(...)`) + `AnalystActionRecord` DTO in `.../Application/Services/IAnalystActionsSource.cs`
- [X] T021 [US1] Implement `MarketBeatAnalystActionsSource` (AngleSharp table parse, structural assertions that throw on markup drift → FR-009) in `.../Infrastructure/Sources/MarketBeatAnalystActionsSource.cs`
- [X] T022 [US1] Implement `YahooAnalystActionsSource` (per-ticker `upgradeDowngradeHistory`, reuse crumb/cookie named client from `YahooEarningsCalendarService`) in `.../Infrastructure/Sources/YahooAnalystActionsSource.cs`
- [X] T023 [US1] Implement `AnalystUniverseService` (seed JSON load + sync from holdings/watchlist/candidates, compose-and-deactivate per `RadarUniverseService` pattern) in `.../Application/Services/AnalystUniverseService.cs`
- [X] T024 [US1] Implement dedup/upsert-with-merge in `AnalystActionRepository` (unique index conflict → fill NULL target/rating fields from richer record) in `.../Infrastructure/Persistence/Repositories/AnalystActionRepository.cs`
- [X] T025 [US1] Implement `AnalystActionsIngestionJob` (`[DisableConcurrentExecution]`; run both sources with per-source failure isolation + `ConsecutiveFailures` tracking; sync universe first) in `.../Infrastructure/Jobs/AnalystActionsIngestionJob.cs`
- [X] T026 [US1] Implement `GetAnalystActionsQuery` + handler (filters ticker/since/actionType/limit; `coverage` envelope = `inUniverse`|`notInUniverse`|`marketWide`) in `.../Application/Queries/GetAnalystActionsQuery.cs`
- [X] T027 [US1] Register `analyst-actions-ingestion` recurring job (nightly 01:00 UTC), named HttpClients for the two sources, and source/service DI in `.../ResearchModule.cs`
- [X] T028 [US1] Implement `get_analyst_actions` MCP tool (thin over handler; response per contract) in `backend/src/FinanceSentry.Mcp/Tools/GetAnalystActionsTool.cs`
- [X] T029 [P] [US1] Unit test for `GetAnalystActionsQuery` coverage-flag logic (in-universe vs not-in-universe vs empty) in `.../Unit/GetAnalystActionsQueryTests.cs`

**Checkpoint**: US1 fully functional — analyst actions ingested, deduped, queryable market-wide via MCP.

---

## Phase 4: User Story 2 - Valuation snapshot for any ticker (Priority: P2)

**Goal**: `get_valuation_snapshot` MCP tool computing current metrics (Yahoo `quoteSummary`) with trailing-P/E 5-year history reconstructed from EDGAR EPS × Yahoo closes, default sector/industry peer set, consensus target + implied upside, honest gaps for unavailable history, staleness flag, and every computation persisted.

**Independent Test**: `get_valuation_snapshot {"ticker":"MCD"}` → trailing P/E w/ 5yr avg, forward P/E & EV/EBITDA flagged `historyUnavailable`, consensus target + implied upside, named peer set; `{"ticker":"SOL"}` → explicit `notApplicable`; no fabricated values across 20-ticker sample; a `valuation_snapshots` row is written each call.

### Tests for User Story 2

- [X] T030 [P] [US2] Contract test for Yahoo `quoteSummary?modules=summaryDetail,defaultKeyStatistics,financialData` shape (`trailingPE`,`forwardPE`,`dividendYield`,`enterpriseValue`,`ebitda`,`targetMeanPrice`, each optional-tolerant) in `.../Contract/YahooValuationModulesContractTests.cs`
- [X] T031 [P] [US2] Unit test for TTM EPS roll-up + trailing-P/E series math (EDGAR DilutedEPS quarterly → TTM ÷ daily closes; short-history IPO uses actual window; missing → null not zero) in `.../Unit/ValuationHistoryServiceTests.cs`

### Implementation for User Story 2

- [X] T032 [P] [US2] Create `IValuationDataService` interface + `ValuationSnapshotResult` DTO (per-metric `{value, fiveYearAvg?, historyWindowYears?, historyUnavailable?}`, peer rows, `impliedUpsidePct?`, `isStale`, `notApplicable`) in `.../Application/Services/IValuationDataService.cs`
- [X] T033 [US2] Implement `YahooValuationDataService` (current metrics via `quoteSummary` modules, crumb pattern; equity-only guard → crypto `notApplicable`) in `.../Infrastructure/Sources/YahooValuationDataService.cs`
- [X] T034 [US2] Implement `ValuationHistoryService` (TTM EPS from `ISecEdgarService.GetFundamentalsAsync` × `IMarketDataService` daily closes → 5yr trailing-P/E avg; EV/EBITDA & div-yield history → `historyUnavailable`) in `.../Application/Services/ValuationHistoryService.cs`
- [X] T035 [US2] Implement `GetValuationSnapshotQuery` + handler (compose current + history + default sector/industry peer set overridable via `peers`, compute implied upside, persist a `valuation_snapshots` row each call) in `.../Application/Queries/GetValuationSnapshotQuery.cs`
- [X] T036 [US2] Register valuation service DI + named HttpClient in `.../ResearchModule.cs`; add valuation-history capture for holdings ∪ watchlist ∪ candidates into `AnalystActionsIngestionJob` (R9)
- [X] T037 [US2] Implement `get_valuation_snapshot` MCP tool (thin over handler; response per contract; missing metric → `value:null` + reason flag, never zero) in `backend/src/FinanceSentry.Mcp/Tools/GetValuationSnapshotTool.cs`

**Checkpoint**: US1 AND US2 both work independently.

---

## Phase 5: User Story 3 - Source-per-thesis news breadth (Priority: P3)

**Goal**: `news_sources` registry (RSS + Page kinds, optional keyword filters, per-source failure counters), market-wide default feeds + TrendForce→DRAM page source seeded, `NewsIngestionJob` iterates registered sources, articles tagged with matched theses, and query surface + `register_thesis_source`/`list_news_sources` MCP tools with `search_market_news` thesis filter.

**Independent Test**: `list_news_sources` shows seeded feeds + TrendForce→DRAM; `register_thesis_source` adds a source; run ingestion → articles from registered source appear tagged to the thesis and market-wide feeds ingest non-held tickers; point a source at an unreachable URL, run twice → sync-failure alert, other sources continue.

### Tests for User Story 3

- [X] T038 [P] [US3] Contract test for TrendForce press-center page structure using a recorded fixture (article list selector → title,url,date) in `.../Contract/TrendForcePageContractTests.cs`; add fixture `.../Contract/Fixtures/trendforce-presscenter.html`
- [X] T039 [P] [US3] Unit test for thesis/keyword tagging (source-registered thesis OR keyword match on title/summary) in `.../Unit/ThesisTaggingTests.cs`
- [X] T040 [P] [US3] Unit test for per-source failure counter + alert-at-2-consecutive threshold (FR-009) in `.../Unit/NewsSourceFailureCounterTests.cs`

### Implementation for User Story 3

- [X] T041 [P] [US3] Create `INewsPageSource` interface (`Page`-kind sources → article candidates for shared pipeline) in `.../Application/Services/INewsPageSource.cs`
- [X] T042 [US3] Implement `TrendForcePageSource` (AngleSharp article-list extraction, structural assertions) in `.../Infrastructure/Sources/TrendForcePageSource.cs`
- [X] T043 [US3] Extend `NewsIngestionJob` to iterate enabled `news_sources` (RSS via existing `RssMarketNewsService`, Page via `INewsPageSource`), tag `ThesisIds`, track per-source `ConsecutiveFailures`, and raise sync-failure alert at 2 via `IAlertGeneratorService.GenerateSyncFailureAlertAsync` in `.../Infrastructure/Jobs/NewsIngestionJob.cs`
- [X] T044 [US3] Implement `RegisterThesisSourceCommand` + handler in `.../Application/Commands/RegisterThesisSourceCommand.cs` and `ListNewsSourcesQuery` + handler in `.../Application/Queries/ListNewsSourcesQuery.cs`
- [X] T045 [US3] Add `thesisId` optional filter to `SearchMarketNewsQuery` + handler (filter by `ThesisIds` column; backward-compatible) in `.../Application/Queries/SearchMarketNewsQuery.cs`
- [X] T046 [US3] Seed market-wide default sources (Yahoo top-stories RSS, MarketWatch top-stories RSS) + TrendForce page → seeded DRAM thesis (skip gracefully if thesis absent) via startup/migration seeder in `.../Infrastructure/Persistence/` (or `ResearchModule` seeder)
- [X] T047 [P] [US3] Implement `register_thesis_source` MCP tool (Denys-confirmation phrasing per `acknowledge_risk_violation`) in `backend/src/FinanceSentry.Mcp/Tools/RegisterThesisSourceTool.cs`
- [X] T048 [P] [US3] Implement `list_news_sources` MCP tool (source health fields) in `backend/src/FinanceSentry.Mcp/Tools/ListNewsSourcesTool.cs`
- [X] T049 [US3] Add `thesisId` param to `search_market_news` MCP tool in `backend/src/FinanceSentry.Mcp/Tools/SearchMarketNewsTool.cs`

**Checkpoint**: All three user stories independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T050 Extend candidate-creation MCP tool(s) to accept `source: "Ledger"` (FR-010) in the relevant tool under `backend/src/FinanceSentry.Mcp/Tools/` and verify `list_candidates` surfaces it
- [X] T051 `/csharp-quality` sweep across all new files; `dotnet build backend/` zero warnings
- [X] T052 Bump backend `<Version>` in `backend/src/FinanceSentry.API/FinanceSentry.API.csproj`
- [X] T053 Run `quickstart.md` verification (P1/P2/P3 + failure-alerting) against the Docker stack; confirm M008 in `__ef_migrations_history_research` *(Done 2026-08-06 locally: M008 applied; P1/P2/P3 pass end-to-end via MCP stdio + Hangfire triggers. Failure-alerting verified through counter/threshold/one-shot logic; final alert row unproducible locally — no active bank accounts → empty fan-out. Crypto valuation needs `SOL-USD`, not bare `SOL`. Known wart filed: MarketBeat firm names carry a "Subscribe to MarketBeat All Access…" suffix (parser drift). TrendForce seed correctly skipped (no DRAM thesis locally); quickstart's Yahoo contract-test mention is stale post-037.)*

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories. T013→T014→T015 are sequential (shared DbContext/module files); T004–T012 are parallel.
- **User Stories (Phase 3–5)**: All depend on Foundational. US1/US2/US3 are independent of each other and can run in parallel; recommended order P1→P2→P3.
  - Note: T036 (US2) edits `AnalystActionsIngestionJob` created in T025 (US1). If US2 runs before US1 ships, add the valuation capture when the job exists; otherwise coordinate the edit.
- **Polish (Phase 6)**: After desired stories complete.

### Within Each User Story

- Tests written first and observed to FAIL before implementation.
- Interfaces/DTOs → sources/services → repositories → job/query → MCP tool.
- `ResearchModule.cs` DI edits are sequential where they touch the same file across stories.

### Parallel Opportunities

- T002, T003 (Setup).
- T004–T012 (all Foundational entities/repos — different files).
- Per story, all `[P]` test tasks together, then `[P]` interface/DTO tasks.
- Whole user stories in parallel once Foundational is done.

---

## Parallel Example: Foundational Phase

```bash
# Entities + repos together (different files):
Task: "Create AnalystAction entity + AnalystActionType enum"
Task: "Create AnalystUniverseMember entity + UniverseReason enum"
Task: "Create NewsSource entity + NewsSourceKind enum"
Task: "Create ValuationSnapshot entity"
Task: "Add ThesisIds to NewsArticle"
Task: "Create IAnalystActionRepository + impl"
Task: "Create IAnalystUniverseRepository + impl"
Task: "Create INewsSourceRepository + impl"
Task: "Create IValuationSnapshotRepository + impl"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 Setup → Phase 2 Foundational (migration discoverable, zero warnings).
2. Phase 3 US1 → **STOP & VALIDATE**: ingest one day, query MU + all-universe + crypto.
3. This alone closes the biggest content gap (analyst actions) and can ship.

### Incremental Delivery

1. Setup + Foundational → foundation ready.
2. US1 (analyst actions) → validate → ship.
3. US2 (valuation snapshot) → validate → ship.
4. US3 (thesis-source news breadth) → validate → ship.
5. Polish (Ledger candidate source, version bump, quickstart).

---

## Notes

- Constitution gates per file: `dotnet build backend/` zero warnings after every `.cs`; external-contract test for every new external source.
- M008 MUST carry its `.Designer.cs` — verify with the quickstart migration-history query (M007 outage lesson).
- No new push channels (FR-011); ingestion failures ride the existing `SyncFailure` alert path only.
- Market-data tables (`analyst_actions`, `valuation_snapshots`, `news_sources`) are global (no `UserId`), precedent `news_articles`/`quote_cache`.
