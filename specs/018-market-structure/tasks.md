# Tasks: Market Structure Scanner + Radar Signal Log

**Feature**: `018-market-structure` | **Branch**: `018-market-structure`
**Input**: plan.md, spec.md, data-model.md, contracts/mcp-tools.md, research.md, quickstart.md
**Tests**: REQUIRED — spec mandates pure-function unit tests (SC-001) + Test-First per constitution.

New module: `FinanceSentry.Modules.Radar`. Paths under repo root. **Build gate**: `dotnet build backend/` → zero warnings after every `.cs`. (`dotnet` runs via the `mcr.microsoft.com/dotnet/sdk:9.0` container.)

---

## Phase 1: Setup — new module scaffold

- [ ] T001 Create the project `backend/src/FinanceSentry.Modules.Radar/FinanceSentry.Modules.Radar.csproj` (net9.0, references `FinanceSentry.Core` + `FinanceSentry.Infrastructure` mirroring `Modules.Wealth.csproj`) and add it to `backend/FinanceSentry.sln`.
- [ ] T002 Add `ProjectReference` to `FinanceSentry.Modules.Radar` from `backend/src/FinanceSentry.API/FinanceSentry.API.csproj` and `backend/src/FinanceSentry.Mcp/FinanceSentry.Mcp.csproj` (so the assembly loads for reflection-based module + tool discovery).
- [ ] T003 Create the xUnit test project `backend/tests/FinanceSentry.Modules.Radar.Tests/` (reference the Radar module + Core) and add to the solution.

---

## Phase 2: Foundational — Core contracts, domain, persistence (blocks all stories)

- [ ] T004 [P] Add Core interface `backend/src/FinanceSentry.Core/Interfaces/IMarketHistorySource.cs` + `DailyBarData(DateOnly Date, decimal Open, High, Low, Close, AdjClose, long Volume)` record.
- [ ] T005 [P] Add Core interface `backend/src/FinanceSentry.Core/Interfaces/IWatchlistReader.cs` (`ListTickersAsync(Guid userId, ct)`); implement `WatchlistReader` in `backend/src/FinanceSentry.Modules.Research/Application/Services/WatchlistReader.cs` over `IWatchlistRepository` and register it in `ResearchModule.cs`.
- [ ] T006 [P] Add Core `backend/src/FinanceSentry.Core/Interfaces/IRadarSignalWriter.cs` + `RadarSignalRequest(...)` record + `SignalSeverity` enum (enum lives in Core since it crosses the boundary).
- [ ] T007 [P] Radar domain enums in `backend/src/FinanceSentry.Modules.Radar/Domain/Enums.cs`: `UniverseKind`, `SignalSubjectType`, `ScannerMode` (SignalSeverity is imported from Core).
- [ ] T008 [P] Radar entities: `Domain/DailyBar.cs`, `Domain/RadarSignal.cs` (Payload object + PayloadVersion + nullable UserId), `Domain/RadarUniverseMember.cs`.
- [ ] T009 Repository interfaces in `Domain/Repositories/`: `IDailyBarRepository` (UpsertRangeAsync idempotent on Ticker+Date, GetSinceAsync(ticker, since), GetLatestDateAsync), `IRadarSignalRepository` (AppendAsync, HasRecentAsync(dedupKey, since), ListAsync(filters), PruneInfoBeforeAsync), `IRadarUniverseRepository` (ListActiveAsync, UpsertMembersAsync, DeactivateAsync).
- [ ] T010 `Infrastructure/Persistence/RadarDbContext.cs`: `HasDefaultSchema("radar")`; DbSets; entity config per data-model.md (unique `(Ticker,Date)`, signal indexes, jsonb `Payload` via HasConversion Web JSON, universe unique-active). Include `RadarDbContextFactory.cs` (copy `WealthDbContextFactory` with history table `__ef_migrations_history_radar`).
- [ ] T011 Repository impls in `Infrastructure/Persistence/Repositories/` for the three interfaces.
- [ ] T012 `RadarModule.cs`: `ModuleRegistrar` + `AddRadarModule(services, config)` — `AddDbContext<RadarDbContext>` with Npgsql + `MigrationsHistoryTable("__ef_migrations_history_radar")`; register repositories, services, jobs (scoped), `IRadarSignalWriter`, and `AddSingleton<IJobRegistrar, JobRegistrar>()`. Bind `RadarOptions` (thresholds, ScannerMode, lookback, freshness N) from configuration.
- [ ] T013 Register `MigrateContext<RadarDbContext>(sp, app.Logger)` (+ `using`) in `backend/src/FinanceSentry.API/Migrations/MigrationExtensions.cs`.
- [ ] T014 Generate migration `M001_InitialSchema` (`dotnet ef migrations add M001_InitialSchema --project backend/src/FinanceSentry.Modules.Radar --context RadarDbContext`); verify `dotnet ef database update` applies cleanly and creates schema `radar` with the three tables. **Any raw SQL must quote PascalCase columns** (unquoted folds to lowercase and fails — the 017 M004 bug).

**Checkpoint**: module builds, DI resolves, migration applies (verify via live API boot).

---

## Phase 3: User Story 2 — Relative strength & sector rotation (P1) 🎯 core value + MVP compute

> US2 is sequenced before US1's job wiring because the **pure computation core** is the spine everything
> else asserts against; it is unit-testable from seeded bars with no ingestion.

### Tests (write first)

- [ ] T015 [P] [US2] `backend/tests/FinanceSentry.Modules.Radar.Tests/MarketStructure/ReturnMathTests.cs`: returns over 21/63/126/252 from seeded adj-close series; RS = ticker − benchmark; A>SPY>B ordering (Independent Test); `<N` bars → null (not zero).
- [ ] T016 [P] [US2] `SectorRotationTests.cs`: rank sectors by RS; rankDelta vs 21d prior; delta ≥ threshold flagged.

### Implementation

- [ ] T017 [P] [US2] `Domain/MarketStructure/ReturnMath.cs` (returns + RS per window) — pure.
- [ ] T018 [P] [US2] `Domain/MarketStructure/MovingAverages.cs` (20/50/200 MA + extension) — pure.
- [ ] T019 [P] [US2] `Domain/MarketStructure/SectorRotation.cs` (rank + delta) — pure.
- [ ] T020 [US2] `Domain/MarketStructure/MarketStructureCalculator.cs` composing the above into `TickerStructure`; window constants {21,63,126,252}; null-on-insufficient.

**Checkpoint**: computation core green from seeded bars, no I/O.

---

## Phase 4: User Story 1 — Daily bar ingestion (P1)

### Tests (write first)

- [ ] T021 [P] [US1] `Ingestion/IngestDailyBarsIdempotencyTests.cs`: empty store → bars back to lookback; re-run appends only new days (unique Ticker+Date); one ticker's source failure → others still ingest, failure in summary (use a `FakeMarketHistorySource`).

### Implementation

- [ ] T022 [US1] `Infrastructure/MarketData/YahooMarketHistorySource.cs` — impl `IMarketHistorySource` over the `yahoo-finance` HttpClient: fetch `/v8/finance/chart/{ticker}?interval=1d&range=…`, parse full OHLCV from `indicators.quote[0]` + `indicators.adjclose[0].adjclose`, map to `DailyBarData`; per-request throttle; empty on failure. Register the client in `RadarModule` (or reuse the Research-registered `yahoo-finance` client).
- [ ] T023 [US1] `Application/Services/RadarUniverseService.cs` — resolve universe = seed ∪ equity holdings (`IBrokerageHoldingsReader`, `InstrumentType=="STK"`) ∪ watchlist (`IWatchlistReader`); upsert `radar_universe_members`, deactivate departed tickers.
- [ ] T024 [US1] `Application/Commands/IngestDailyBarsCommand.cs` + handler: for each active universe ticker, fetch bars since `latestDate ?? lookback`, `UpsertRangeAsync`, isolate per-ticker failures into `IngestRunSummary`.
- [ ] T025 [US1] `Infrastructure/Jobs/RadarIngestionJob.cs` (daily post-close, `[AutomaticRetry]`) → runs the command; register in `RadarModule` `JobRegistrar` (`AddOrUpdate<RadarIngestionJob>("radar-ingestion", …, Cron.Daily(hour))`) + `AddScoped`.

**Checkpoint**: ingestion idempotent; universe auto-synced.

---

## Phase 5: User Story 4 — Signal log + MCP surface (P1)

### Tests (write first)

- [ ] T026 [P] [US4] `Signals/RadarSignalWriterTests.cs`: `notable`+ deduped by DedupKey within silence window; `info` recorded every run; append-only.
- [ ] T027 [P] [US4] Add 6 tool names to `backend/tests/FinanceSentry.Mcp.Tests/ContractTests/ToolNameContractTests.cs` allowlist (33→39, update the `because:` literal) and add parity facts in `IntegrationTests/ToolParityTests.cs` (register Radar services + a `FakeMarketHistorySource`).

### Implementation

- [ ] T028 [US4] `Application/Services/RadarSignalWriter.cs` impl `IRadarSignalWriter` (dedup via `IRadarSignalRepository.HasRecentAsync` for notable+; append). Register in `RadarModule`.
- [ ] T029 [US4] `Application/Services/SignalThresholds.cs` + `RadarOptions` binding (thresholds, `ScannerMode`, silence window, retention) — all config, no code constants (FR-008).
- [ ] T030 [US4] Queries + handlers in `Application/Queries/`: `GetMarketStructureQuery`, `GetRelativeStrengthQuery`, `GetSectorRotationQuery`, `GetMarketBreadthQuery`, `ListSignalsQuery`, `GetRadarSummaryQuery` — pure reads over persisted bars/signals, never ingest (FR-011); attach `stale` flag (FR-017).
- [ ] T031 [P] [US4] Six MCP tools in `backend/src/FinanceSentry.Mcp/Tools/` per contracts/mcp-tools.md (mirror `GetQuotesTool`/017 tools; `[McpServerTool(Name=…)]`, `IIdentityResolver`).

**Checkpoint**: scanner emits to `radar_signals`; all 6 tools invocable; parity/allowlist green.

---

## Phase 6: User Story 3 — Breadth, unusual moves, extension (P2)

### Tests (write first)

- [ ] T032 [P] [US3] `MarketStructure/VolatilityTests.cs` + `BreadthTests.cs`: 63-day σ + today z-score; ≥3σ flagged with z-score; breadth % above MA20/50/200; zero-volume/short-history edges.

### Implementation

- [ ] T033 [P] [US3] `Domain/MarketStructure/Volatility.cs` (σ, z-score, volume ratio) — pure.
- [ ] T034 [P] [US3] `Domain/MarketStructure/Breadth.cs` (% above MAs) — pure.
- [ ] T035 [US3] `Application/Commands/ComputeMarketStructureCommand.cs` + handler + `RadarComputeJob`: run calculator over the universe, emit signals (`breadth` info; `rotation_shift`/`held_sector_laggard` notable; `unusual_move` with z-score; `extended` info) via `IRadarSignalWriter`; **held-ticker** `unusual_move` at/above alert bar raises `AlertType.MarketStructure` **only when `ScannerMode=Alerting`** (FR-010/015). Register the job in `JobRegistrar` (`radar-compute`, daily shortly after ingestion).
- [ ] T036 [US3] Alerts wiring: add `AlertType.MarketStructure` const (`Modules.Alerts/Domain/AlertType.cs`); add `GenerateMarketStructureAlertAsync` (+ freshness reason) to `IAlertGeneratorService` (Core) + impl in `AlertGeneratorService` (FindActive→HasRecent→AddAsync + silence window). `trim_into_strength` is **deferred to v2** — do not implement.

---

## Phase 7: Resilience & validation

- [ ] T037 [US3] `Infrastructure/Jobs/RadarFreshnessWatchdogJob.cs` (FR-017): raise `AlertType.MarketStructure` freshness alert when any universe ticker's latest bar > N trading days old or ingestion failed; ensure structure reads set `stale=true` over stale data. Register the job.
- [ ] T038 `Application/Commands/RunHistoricalValidationCommand.cs` (FR-016): replay the pure calculator over ≥5y persisted bars, counting signal frequency/precision across the 2020 crash, 2022 unwind, 2026-07 memory rotation. `HistoricalValidation/ReplayTests.cs` asserts the expected signals appear in each seeded episode (SC-002) with low enough frequency not to spam.

---

## Phase 8: Polish

- [ ] T039 [P] `dotnet build backend/` → 0 warnings; `dotnet test` on `Modules.Radar.Tests` + `Mcp.Tests` → all green; confirm `ToolAttributeContractTests`/allowlist pass with the 6 new tools.
- [ ] T040 Live boot: rebuild API image, confirm `M001` applies (schema `radar`, three tables, `__ef_migrations_history_radar`), API healthy, `radar-ingestion`/`radar-compute`/`radar-freshness-watchdog` recurring jobs register without throwing.
- [ ] T041 [P] Confirm `ScannerMode` defaults to `LogOnly` (FR-015 — zero Alerts at launch) and SC-005: grep the Radar module for any Telegram/email/messaging or paid-API dependency — none.

---

## Dependencies & order

- Setup (T001–T003) → Foundational (T004–T014) block all stories.
- US2 compute core (T015–T020) is the spine — implement first; US1 (T021–T025) ingestion feeds it; US4 (T026–T031) exposes it; US3 (T032–T036) adds the P2 signals; then resilience (T037–T038); polish last.
- Core interfaces (T004/T005/T006) are parallel. Metric pure-functions (T017/T018/T019, T033/T034) are parallel. The 6 MCP tools (T031) are parallel.

## MVP scope

**Setup + Foundational + US2 (compute core) + US1 (ingestion) + US4 (signal log + MCP)** = a running
market-structure scanner in log-only mode with the shared signal log and read tools — the core value.
US3 adds breadth/unusual/extension signals; historical validation (T038) gates enabling alerts.

## Implementation notes

Implement with **Sonnet** per the recipe. New-module discipline: reflection auto-discovers the module
and jobs once the `.csproj` references + `MigrateContext<RadarDbContext>` are in place (T002/T013).
Launch in `LogOnly`; alerting is enabled only after historical validation + observed distributions.
