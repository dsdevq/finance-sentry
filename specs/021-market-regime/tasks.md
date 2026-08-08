# Tasks: Market Regime Scanner

**Input**: Design documents from `/specs/021-market-regime/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Unit tests are MANDATORY (constitution). FRED is an external API → its source ships with a parse/keyless contract test. No new REST endpoint (MCP-only) → no REST contract test; the MCP tool contract is documented under `contracts/`.

**Organization**: Tasks grouped by user story (US1 read regime, US2 signals, US3 019 coupling). Build gate after every `.cs`: `docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet build backend/FinanceSentry.sln -c Debug` → zero warnings.

## Path Conventions

Backend modular monolith. Regime lives inside `backend/src/FinanceSentry.Modules.Radar/`; Core port in `backend/src/FinanceSentry.Core/`; MCP tool in `backend/src/FinanceSentry.Mcp/`; 019 coupling in `backend/src/FinanceSentry.Modules.Research/`.

---

## Phase 1: Setup (Shared Infrastructure)

- [ ] T001 Verify baseline: run `docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet build backend/FinanceSentry.sln -c Debug` and confirm 0 warnings before touching code.
- [ ] T002 Add regime config plumbing: `Regime__Fred__ApiKey: ${FRED_API_KEY:-}` to `docker/docker-compose.dev.yml` and `docker/docker-compose.prod.yml` (api env), and a commented `FRED_API_KEY=` line to `docker/.env.example`. (No `.env.sops` secret edit — blank = rates axis silent.)

---

## Phase 2: Foundational (blocking prerequisites for all stories)

**Purpose**: Enums, constants, options, and the persistence table every story reads/writes.

- [ ] T003 [P] Create regime enums `VolatilityRegime`, `RatesRegime`, `RegimeTrend` in `backend/src/FinanceSentry.Modules.Radar/Domain/Regime/RegimeEnums.cs`.
- [ ] T004 [P] Add scanner/signal-type constants (`RadarScanners.MarketRegime`, `RadarSignalTypes.RegimeVolatility/RegimeRates/RegimeChange`) to `backend/src/FinanceSentry.Modules.Radar/Domain/RadarConstants.cs`.
- [ ] T005 [P] Create `RegimeOptions` (section `Regime`, all thresholds from data-model.md, no magic numbers) in `backend/src/FinanceSentry.Modules.Radar/Application/Services/RegimeOptions.cs`.
- [ ] T006 Create the `RegimeReading` entity in `backend/src/FinanceSentry.Modules.Radar/Domain/Regime/RegimeReading.cs` (both axes + raw drivers + availability flags per data-model.md).
- [ ] T007 Map `RegimeReading` in `RadarDbContext` (table `regime_readings`, enum→string conversions, `numeric(10,4)` yields, `idx_regime_readings_computed_at DESC`) — edit `backend/src/FinanceSentry.Modules.Radar/Infrastructure/Persistence/RadarDbContext.cs` (+ `DbSet<RegimeReading>`).
- [ ] T008 Define `IRegimeReadingRepository` (`AppendAsync`, `LatestAsync`, `PriorAsync(before)`) in `backend/src/FinanceSentry.Modules.Radar/Domain/Repositories/IRegimeReadingRepository.cs` and implement `RegimeReadingRepository` in `backend/src/FinanceSentry.Modules.Radar/Infrastructure/Persistence/Repositories/RegimeReadingRepository.cs`.
- [ ] T009 Generate Radar migration **M002_RegimeReadings** with the EF CLI in the sdk:10.0 container against `RadarDbContextFactory` (from `backend/src/FinanceSentry.API`), producing `.cs` + `.Designer.cs` (with `[DbContext]`/`[Migration]` attributes) + updated `RadarDbContextModelSnapshot.cs`. Verify all three artifacts and that the snapshot includes `regime_readings`. (Migration-safety gate — see research R8.)

**Checkpoint**: `dotnet build` green; `regime_readings` in the model snapshot.

---

## Phase 3: User Story 1 — See the current market regime on both axes (P1) 🎯 MVP

**Goal**: Fetch VIX + FRED, classify both axes deterministically, persist a reading, expose `get_market_regime()`.

**Independent test**: Seed VIX bars + yield observations, run compute, call the tool, assert both bands + raw readings + last-change present; unavailable axis reports `available:false` with null band.

### Tests for US1

- [ ] T010 [P] [US1] `RegimeClassifierTests` in `backend/tests/FinanceSentry.Modules.Radar.Tests/Regime/RegimeClassifierTests.cs` — volatility bands incl. exact boundaries (15/20/30), trend (Rising/Falling/Flat/Unknown on short history), rates bands incl. boundaries (0/0.5/1.5), inversion→recession flag, growth-value tilt hint.
- [ ] T011 [P] [US1] `FredYieldCurveSourceTests` in `backend/tests/FinanceSentry.Modules.Radar.Tests/Regime/FredYieldCurveSourceTests.cs` — keyless ⇒ no fetch/`IsConfigured==false`; `Parse` skips `"."` and picks latest valid; non-array/`observations`-less body throws; spread = DGS10−DGS2.

### Implementation for US1

- [ ] T012 [P] [US1] Create pure `RegimeClassifier` (both axes, config-driven, deterministic) in `backend/src/FinanceSentry.Modules.Radar/Domain/Regime/RegimeClassifier.cs` and `YieldObservation`/latest-pair helper in `backend/src/FinanceSentry.Modules.Radar/Domain/Regime/YieldObservation.cs`.
- [ ] T013 [P] [US1] Define `IYieldCurveSource` and implement keyless-silent `FredYieldCurveSource` (named client `regime-fred`, `sort_order=desc&limit`, skip `"."`, loud-on-broken-body) in `backend/src/FinanceSentry.Modules.Radar/Infrastructure/MarketData/IYieldCurveSource.cs` + `FredYieldCurveSource.cs`.
- [ ] T014 [US1] Create `ComputeMarketRegimeCommand` + handler (fetch `^VIX` via `IMarketHistorySource`, fetch DGS10/DGS2 via `IYieldCurveSource`, classify both axes, persist a `RegimeReading`; both-fail ⇒ persist nothing + warn) in `backend/src/FinanceSentry.Modules.Radar/Application/Commands/ComputeMarketRegimeCommand.cs`. (Signals added in US2.)
- [ ] T015 [US1] Create `GetMarketRegimeQuery` + handler → `RegimeStateDto` (both axes, raw readings, per-axis last-change via `PriorAsync` scan) in `backend/src/FinanceSentry.Modules.Radar/Application/Queries/RegimeQueries.cs`.
- [ ] T016 [US1] Register in `RadarModule`: `RegimeOptions`, `IYieldCurveSource→FredYieldCurveSource`, `IRegimeReadingRepository→RegimeReadingRepository`, the `regime-fred` named HttpClient (base `Regime:Fred:BaseUrl`) — edit `backend/src/FinanceSentry.Modules.Radar/RadarModule.cs`.
- [ ] T017 [US1] Create the MCP tool `GetMarketRegimeTool` (`[McpServerTool(Name="get_market_regime")]`, injects the query handler + identity resolver, returns `RegimeStateDto`) in `backend/src/FinanceSentry.Mcp/Tools/GetMarketRegimeTool.cs`.
- [ ] T018 [US1] Build + run `dotnet test backend/tests/FinanceSentry.Modules.Radar.Tests` in the container; zero warnings, green.

**Checkpoint**: US1 independently demoable — compute produces a persisted reading; `get_market_regime()` returns both axes.

---

## Phase 4: User Story 2 — Regime readings and changes logged as radar signals (P2)

**Goal**: Emit daily `info` per axis and one `regime_change` `notable` per axis that crosses a band, deduped; wire the daily Hangfire job.

**Independent test**: Run compute twice on an unchanged fixture → two `info` per axis, zero change signals; run on a band-crossing fixture → exactly one `regime_change` on the moving axis.

### Tests for US2

- [ ] T019 [P] [US2] `ComputeMarketRegimeTests` in `backend/tests/FinanceSentry.Modules.Radar.Tests/Regime/ComputeMarketRegimeTests.cs` — no-change run emits info-only; band-cross emits exactly one `regime_change` notable on the moving axis (with from/to + raw driver) and info on the other; same-day re-run of a change is deduped; first-ever run emits no change; VIX-fail skips volatility axis while rates still computes.

### Implementation for US2

- [ ] T020 [US2] Extend `ComputeMarketRegimeCommand` handler: append daily `info` `regime_volatility`/`regime_rates` via `IRadarSignalWriter`, and a `regime_change` `notable` for an axis whose band differs from `PriorAsync` (from/to + raw driver, dedup key per data-model.md) — edit `backend/src/FinanceSentry.Modules.Radar/Application/Commands/ComputeMarketRegimeCommand.cs`.
- [ ] T021 [US2] Create `RegimeComputeJob` (daily, `[AutomaticRetry(Attempts=2)]`, delegates to the command) in `backend/src/FinanceSentry.Modules.Radar/Infrastructure/Jobs/RegimeComputeJob.cs`.
- [ ] T022 [US2] Register `RegimeComputeJob` in `RadarModule` DI + `JobRegistrar` (`AddOrUpdate("regime-compute", …, Cron.Daily(RegimeOptions.ComputeHourUtc))`) — edit `backend/src/FinanceSentry.Modules.Radar/RadarModule.cs`.
- [ ] T023 [US2] Build + `dotnet test backend/tests/FinanceSentry.Modules.Radar.Tests`; zero warnings, green.

**Checkpoint**: US2 demoable — signal log carries daily readings + change events; job scheduled.

---

## Phase 5: User Story 3 — Regime context adjusts opportunity scoring (never actions) (P3)

**Goal**: Read latest regime via a Core port; apply a deterministic, documented haircut to a candidate's regime-adjusted structure score; preserve the raw score; never action.

**Independent test**: Same candidate scored under Calm/Steep vs Panic/Inverted → identical raw score, lower adjusted score for Extended crowding, unchanged for Early, rationale present; no-data ⇒ adjusted==raw + `no_regime_data`; no cash/sell/promote side effect.

### Tests for US3

- [ ] T024 [P] [US3] `RegimeScoreAdjusterTests` in `backend/tests/FinanceSentry.Modules.Research.Tests/Regime/RegimeScoreAdjusterTests.cs` — Panic+Extended haircut applied; Panic+Early zero; Stressed magnitudes; Inverted additional haircut stacks; clamp to [0,100]; null raw score or null snapshot ⇒ passthrough + `no_regime_data`; raw score never mutated.

### Implementation for US3

- [ ] T025 [P] [US3] Define `IMarketRegimeSource` + `MarketRegimeSnapshot` record in `backend/src/FinanceSentry.Core/Interfaces/IMarketRegimeSource.cs`.
- [ ] T026 [US3] Implement `MarketRegimeSource` in Radar (reads `IRegimeReadingRepository`, projects to strings, computes per-axis last-change) in `backend/src/FinanceSentry.Modules.Radar/Application/Services/MarketRegimeSource.cs`; register `IMarketRegimeSource→MarketRegimeSource` in `RadarModule`.
- [ ] T027 [P] [US3] Add regime haircut constants to `OpportunityOptions` (`RegimePanicExtendedHaircut=15`, `RegimeStressedExtendedHaircut=8`, `RegimeInvertedExtendedHaircut=5`) in `backend/src/FinanceSentry.Modules.Research/Application/Services/OpportunityOptions.cs`.
- [ ] T028 [US3] Create pure `RegimeScoreAdjuster.Adjust(...)` (rules per research R7, clamp, `no_regime_data` passthrough) in `backend/src/FinanceSentry.Modules.Research/Domain/Scoring/RegimeScoreAdjuster.cs`.
- [ ] T029 [US3] Add `RegimeContext` record + `RegimeContext? Regime` field to `CandidateScorecard` in `backend/src/FinanceSentry.Modules.Research/Domain/Scoring/CandidateScorecard.cs`.
- [ ] T030 [US3] Wire `ScoreCandidateCommandHandler`: inject `IMarketRegimeSource`, fetch latest, call `RegimeScoreAdjuster`, attach `RegimeContext` to the returned scorecard; keep persisted `CandidateScore.StructureScore` = raw; never action — edit `backend/src/FinanceSentry.Modules.Research/Application/Commands/ScoreCandidateCommand.cs`.
- [ ] T031 [US3] Build + `dotnet test backend/tests/FinanceSentry.Modules.Research.Tests` and `backend/tests/FinanceSentry.Mcp.Tests` (DI-resolution test must still resolve every tool now that ScoreCandidate depends on the new port); zero warnings, green.

**Checkpoint**: US3 demoable — scorecard carries regime context; ranking shifts under risk-off; no book change.

---

## Phase 6: Polish & Cross-Cutting

- [ ] T032 [P] Add default `Regime` config section to `backend/src/FinanceSentry.API/appsettings.json` (and MCP `appsettings.json` if it carries module config) so thresholds are documented in-repo (all values from data-model.md; `Fred:ApiKey` empty).
- [ ] T033 Full-solution gate: `dotnet build backend/FinanceSentry.sln -c Debug` (0 warnings) + `dotnet test` across Radar, Research, and Mcp test projects, all green in the sdk:10.0 container. Record actual output.
- [ ] T034 [P] Update `specs/ROADMAP.md` 021 sketch note to "implemented" and confirm `quickstart.md` verify steps match the shipped shape.

---

## Dependencies & Execution Order

- **Setup (P1)** → **Foundational (P2)** → **US1 (P3)** → **US2 (P4)** → **US3 (P5)** → **Polish (P6)**.
- US2 depends on US1 (extends the same compute command + needs the persisted reading). US3 depends on US1 (needs the reading + repository behind the port) but is independent of US2.
- Migration T009 blocks any test that touches persistence (US1/US2 compute tests use EF InMemory so they do not need the migration, but a real DB verify does).

## Parallel Opportunities

- Foundational: T003, T004, T005 in parallel (distinct files); T006→T007→T008→T009 sequential (same context/model).
- US1: T010, T011 (tests) parallel; T012, T013 (classifier/source) parallel; then T014→T015→T016→T017 sequential-ish (shared handler/module wiring).
- US3: T024 (test), T025 (Core port), T027 (options) parallel; T028→T029→T030 sequential.

## Implementation Strategy

MVP = Phase 1+2+US1 (a truthful, queryable two-axis regime read). US2 adds the shared-log history/change events; US3 adds the 019 coupling. Each story is an independently shippable increment; commit per task with `feat(021): …`.
