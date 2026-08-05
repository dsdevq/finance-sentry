# Tasks: Structured Data Sources (retire brittle scraping — free re-scope)

**Input**: Design documents from `/specs/037-structured-data-sources/`
**Prerequisites**: plan.md, spec.md (Decision Log: free re-scope), research.md (R1–R7), data-model.md, contracts/finnhub-recommendation.md, quickstart.md

**Tests**: MANDATORY per constitution — external-API contract test (recorded fixture) for the Finnhub integration, unit tests (>80% on new logic), TDD order inside each story. No new REST endpoints ⇒ no REST contract tests.

**Build discipline**: no local dotnet — build/test in the `mcr.microsoft.com/dotnet/sdk:10.0` container (volume `fs-nuget:/root/.nuget/packages`). Zero-warning gate after every `.cs` change.

**Organization**: grouped by user story; stories are independently testable increments.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: configuration plumbing both US1 and US2 build on

- [ ] T001 Create `AnalystSourcesOptions` (SectionName `AnalystSources`; nested `MarketbeatOptions { Enabled=true }`, `FinnhubOptions { Enabled=true, ApiKey="", BaseUrl="https://finnhub.io/api/v1", RequestsPerMinute=50 }`) in `backend/src/FinanceSentry.Modules.Research/Application/Services/AnalystSourcesOptions.cs`
- [ ] T002 Bind options + add named HttpClient `finnhub` (BaseAddress from options, `X-Finnhub-Token` default header from ApiKey when non-empty, 30s timeout, UA per IBKR-quirks convention) in `backend/src/FinanceSentry.Modules.Research/ResearchModule.cs`
- [ ] T003 [P] Map `FINNHUB_API_KEY` → `AnalystSources__Finnhub__ApiKey` on the `api` service in `docker/docker-compose.prod.yml` and `docker/docker-compose.dev.yml`; document `FINNHUB_API_KEY` (blank = trends capture off) in `docker/.env.example`

**Checkpoint**: `dotnet build` zero warnings; app boots with and without the env var.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: schema + domain surface every story reads

**⚠️ CRITICAL**: complete before any user story

- [ ] T004 Create `RecommendationTrend` entity (fields per data-model.md: Ticker, Period `DateOnly`, five counts, Source, IngestedAt) in `backend/src/FinanceSentry.Modules.Research/Domain/RecommendationTrend.cs`
- [ ] T005 [P] Create `IRecommendationTrendRepository` (`UpsertAsync(IReadOnlyList<RecommendationTrend>, ct)` keyed on (Ticker, Period); `GetLatestAsync(ticker, months, ct)`) in `backend/src/FinanceSentry.Modules.Research/Domain/Repositories/IRecommendationTrendRepository.cs`
- [ ] T006 Add `DbSet<RecommendationTrend>` + entity config (table `recommendation_trends`, schema `research`, unique index (ticker, period), snake_case columns per existing convention) to `ResearchDbContext` in `backend/src/FinanceSentry.Modules.Research/Infrastructure/Persistence/ResearchDbContext.cs`
- [ ] T007 Generate migration **M010_RecommendationTrends** via `dotnet ef migrations add` **inside the sdk:10.0 container** (NEVER hand-write — M007 lesson) into `backend/src/FinanceSentry.Modules.Research/Migrations/`; verify Designer file + snapshot updated
- [ ] T008 Implement `RecommendationTrendRepository` (upsert = insert-or-update-counts by (ticker, period)) in `backend/src/FinanceSentry.Modules.Research/Infrastructure/Persistence/Repositories/RecommendationTrendRepository.cs` + DI registration in `ResearchModule.cs`

**Checkpoint**: migration applies cleanly on the dev stack (`__ef_migrations_history_research` shows M010); build zero warnings.

---

## Phase 3: User Story 1 — Structured recommendation trends (Priority: P1) 🎯 MVP

**Goal**: nightly capture of Finnhub monthly consensus counts for the tracked set (Holding/Watchlist/Candidate/Manual), key-gated, rate-limited, silent when key absent.

**Independent Test**: with `FINNHUB_API_KEY` set, trigger `analyst-actions-ingestion` in Hangfire → `research.recommendation_trends` has rows for tracked tickers and the log shows `Recommendation trends captured for X/Y tracked tickers`; without the key, one Debug line and zero Warning/Error entries (quickstart §3–4).

### Tests for User Story 1 (TDD — write first, watch them fail)

- [ ] T009 [P] [US1] Record a real free-tier `/stock/recommendation` response into `backend/tests/FinanceSentry.Modules.Research.Tests/Fixtures/finnhub-recommendation.json`; write contract test asserting the documented shape + parse tolerances (unknown fields ignored, missing count → 0, malformed period → row skipped, non-array root → `AnalystSourceParseException`) in `backend/tests/FinanceSentry.Modules.Research.Tests/Contracts/FinnhubRecommendationContractTests.cs`, per `contracts/finnhub-recommendation.md`
- [ ] T010 [P] [US1] Unit tests for the service: mapping fixture→records, zero-coverage `[]` → empty + Debug, 401/403 → throws `AnalystSourceParseException`, 429 → one bounded retry then skip, pacing honors `RequestsPerMinute`, key-absent → no-op, in `backend/tests/FinanceSentry.Modules.Research.Tests/Sources/FinnhubRecommendationTrendsServiceTests.cs`
- [ ] T011 [P] [US1] Key-gated live smoke test (Skip when `FINNHUB_API_KEY` unset): fetch one ticker, assert ≥0 rows parse, in `FinnhubRecommendationContractTests.cs` (same file, `[SkippableFact]`/env-guard pattern used by existing external tests)

### Implementation for User Story 1

- [ ] T012 [P] [US1] Create `IRecommendationTrendsService` (`FetchAsync(tickers, ct)` → `IReadOnlyList<RecommendationTrend>`; `bool IsConfigured`) in `backend/src/FinanceSentry.Modules.Research/Application/Services/IRecommendationTrendsService.cs`
- [ ] T013 [US1] Implement `FinnhubRecommendationTrendsService` (named client `finnhub`; **public static `Parse`** for fixture tests; per-ticker fetch with pacing + error semantics from the contract doc; canonical-ticker preserved over echo) in `backend/src/FinanceSentry.Modules.Research/Infrastructure/Sources/FinnhubRecommendationTrendsService.cs` (depends on T009/T010 failing first, T012)
- [ ] T014 [US1] Add `CaptureRecommendationTrendsAsync(members, ct)` step to `AnalystActionsIngestionJob` (tracked-set filter via existing `ValuationCaptureReasons`; per-run isolation — never fails the actions run; `IAnalystSourceHealth` strikes under key `finnhub` on total failure; skip with one Debug line when `!IsConfigured`) in `backend/src/FinanceSentry.Modules.Research/Infrastructure/Jobs/AnalystActionsIngestionJob.cs`
- [ ] T015 [US1] Register `IRecommendationTrendsService` in `ResearchModule.cs` (always registered; no-op path driven by `IsConfigured` so DI graph is stable) + unit-test the job step (configured/unconfigured/failure paths) in existing `backend/tests/FinanceSentry.Modules.Research.Tests/Jobs/AnalystActionsIngestionJobTests.cs`
- [ ] T016 [US1] Validate on dev stack per quickstart §3–4 (trigger job, check table + logs, both with and without key); fix findings

**Checkpoint**: US1 fully functional — structured signal accumulating. MVP deliverable.

---

## Phase 4: User Story 2 — Retire the Yahoo analyst scraper (Priority: P2)

**Goal**: delete `YahooAnalystActionsSource` and all its wiring; MarketBeat remains the sole per-action source, demotable via config.

**Independent Test**: full unit suite green; trigger ingestion → log shows `marketbeat` (and trends step) but no `yahoo` source line, no crumb warnings; `analyst_actions` keeps receiving `source='marketbeat'` rows (quickstart §5).

### Implementation for User Story 2 (deletion first, tests prove the absence)

- [ ] T017 [US2] Delete `backend/src/FinanceSentry.Modules.Research/Infrastructure/Sources/YahooAnalystActionsSource.cs`; remove its DI registration + named HttpClient `yahoo-analyst` block from `ResearchModule.cs` (leave `YahooMarketDataService`, `YahooEarningsCalendarService`, `YahooValuationDataService` and their clients strictly untouched)
- [ ] T018 [P] [US2] Delete Yahoo-analyst test files + fixtures under `backend/tests/FinanceSentry.Modules.Research.Tests/` (locate via `grep -rl "YahooAnalystActions"`); update any shared test helpers that referenced the source
- [ ] T019 [US2] Gate MarketBeat registration on `AnalystSources:Marketbeat:Enabled` (default true, FR-004 reversibility) in `ResearchModule.cs` + unit test both flag states in `backend/tests/FinanceSentry.Modules.Research.Tests/ResearchModuleRegistrationTests.cs` (create if absent)
- [ ] T020 [US2] Sweep stale references: `grep -rn "yahoo-analyst\|YahooAnalystActions"` across `backend/` must return nothing; update the `get_analyst_actions` tool `[Description]` (currently says "MarketBeat market-wide sweep + Yahoo per-ticker") in `backend/src/FinanceSentry.Mcp/Tools/GetAnalystActionsTool.cs`; full suite green in container

**Checkpoint**: crumb/404 failure class is gone; per-action ingestion unchanged.

---

## Phase 5: User Story 3 — Trends visible to Ledger (Priority: P3)

**Goal**: `get_analyst_actions` (ticker-filtered) returns the latest recommendation trends alongside per-action rows.

**Independent Test**: MCP call `get_analyst_actions {ticker: "MU"}` → response carries `recommendationTrends` (latest months, counts); whole-universe call (no ticker) omits the block (quickstart §6).

### Tests for User Story 3 (TDD — write first)

- [ ] T021 [P] [US3] Unit tests for the query handler extension: ticker query includes ≤N latest trend rows, no-ticker query omits block, no-trends ticker → empty array (not null), in `backend/tests/FinanceSentry.Modules.Research.Tests/Queries/GetAnalystActionsQueryTests.cs` (extend existing)

### Implementation for User Story 3

- [ ] T022 [US3] Add `RecommendationTrendDto` + optional `RecommendationTrends` list to `AnalystActionsResult` in `backend/src/FinanceSentry.Modules.Research/API/Responses/AnalystActionDto.cs` (or sibling response file, following the one-concept-per-file layout of the module)
- [ ] T023 [US3] Extend `GetAnalystActionsQuery` handler to load latest trends via `IRecommendationTrendRepository.GetLatestAsync` when a ticker filter is present, in `backend/src/FinanceSentry.Modules.Research/Application/Queries/GetAnalystActionsQuery.cs` (depends on T021 failing first)
- [ ] T024 [US3] Update `get_analyst_actions` `[Description]` to mention the trends block + MCP smoke via inspector/Ledger in `backend/src/FinanceSentry.Mcp/Tools/GetAnalystActionsTool.cs`

**Checkpoint**: all three stories independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T025 [P] Zero-warning sweep: full `dotnet build FinanceSentry.sln` + entire unit suite in the sdk:10.0 container; fix all warnings (constitution II)
- [ ] T026 [P] Update `CLAUDE.md` Active Technologies entry for 037 (new table M010, Finnhub client, retired Yahoo analyst source)
- [ ] T027 Run full `quickstart.md` validation end-to-end on the dev stack (§3–§7); record outcomes in the PR description
- [ ] T028 Deploy-time follow-ups (post-merge, VPS): add `FINNHUB_API_KEY` to `docker/.env.sops`; after ~1 week run quickstart §5 log check to confirm SC-001 (zero drift-class failures) — **Denys's call per deploy policy**

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (P1)**: none — start immediately
- **Foundational (P2)**: needs T001 (options exist) — **blocks all stories**
- **US1 (P3)**: needs Foundational
- **US2 (P4)**: needs Foundational only (deletion is independent of US1, but running US1 first keeps a corroborating signal live before the scraper dies — recommended order)
- **US3 (P5)**: needs US1 (trends must exist to surface)
- **Polish (P6)**: needs all desired stories

### Parallel Opportunities

- T003 ∥ T001–T002 · T004/T005 ∥ · T009/T010/T011 ∥ (test files) · T017+T018 ∥ · T025/T026 ∥
- US2 could run parallel to US1 after Phase 2 if desired (different files except `ResearchModule.cs` — serialize edits to that file)

### Implementation Strategy

MVP = Phases 1–3 (US1): structured signal accumulating behind a key. Then US2 (the actual pain removal), then US3 (Ledger surface). Commit + PR per story or as one branch PR per repo convention; squash title MUST be `feat(037): …` so release-please tags.
