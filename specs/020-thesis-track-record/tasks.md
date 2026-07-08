# Tasks: Thesis Track Record

**Input**: Design documents from `/specs/020-thesis-track-record/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, quickstart.md ✅

**Tests**: xUnit unit tests are mandatory for all pure business logic (return math, recorder resilience) per constitution Testing Discipline; MCP contract tests are mandatory for every new tool.

**Organization**: Tasks grouped by user story for independent implementation and testing. No frontend tasks — this feature has no UI surface (spec `[OUT OF SCOPE]`).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 = Every lifecycle event is price-stamped | US2 = Performance vs benchmark | US3 = Aggregate track record

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Domain types and persistence must exist before any capture or query logic.

- [ ] T001 [P] Create `ThesisSubjectType` enum (`Thesis`, `Candidate`) in `backend/src/FinanceSentry.Modules.Research/Domain/ThesisSubjectType.cs`
- [ ] T002 [P] Create `ThesisEventType` enum (`Created`, `Broken`, `Unbroken`, `Closed`, `Promoted`, `Rejected`, `Expired`, `Snapshot`) in `backend/src/FinanceSentry.Modules.Research/Domain/ThesisEventType.cs`
- [ ] T003 Create `ThesisEvent` domain entity (fields per data-model.md: `Id`, `UserId`, `SubjectType`, `SubjectId`, `Ticker`, `EventType`, `Timestamp`, `SubjectPrice?`, `BenchmarkPrice?`, `BenchmarkTicker`, `PricesPending`, `DecisionNote?`) in `backend/src/FinanceSentry.Modules.Research/Domain/ThesisEvent.cs`

---

## Phase 2: Foundational (persistence + recorder — Blocking Prerequisites)

**Purpose**: Repository, DbContext wiring, migration, and the capture service — must be complete before any user story.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T004 [P] Create `IThesisEventRepository` (`AppendAsync`, `ListAsync(subjectId?)`, `ListPendingAsync`, `ListForPeriodAsync(userId, from, to)`, `GetLatestForSubjectAsync`) in `backend/src/FinanceSentry.Modules.Research/Domain/Repositories/IThesisEventRepository.cs`
- [ ] T005 Add `DbSet<ThesisEvent>` and entity configuration (table `thesis_events`; composite index `(SubjectType, SubjectId, Timestamp)`; index `(UserId, PricesPending)`; index `(UserId, EventType, Timestamp)`) to `backend/src/FinanceSentry.Modules.Research/Infrastructure/Persistence/ResearchDbContext.cs`
- [ ] T006 Run EF migration `M004_ThesisEvents` for the `thesis_events` table with the three indexes above under `backend/src/FinanceSentry.Modules.Research/Migrations/`
- [ ] T007 Implement `ThesisEventRepository` in `backend/src/FinanceSentry.Modules.Research/Infrastructure/Persistence/Repositories/ThesisEventRepository.cs`
- [ ] T008 [P] Create `IThesisEventRecorder` (`RecordAsync(userId, subjectType, subjectId, ticker, eventType, decisionNote = null, cancellationToken)`) in `backend/src/FinanceSentry.Modules.Research/Application/Services/IThesisEventRecorder.cs`
- [ ] T009 Implement `ThesisEventRecorder` (resolves subject + SPY quotes via `IMarketDataService`; on any quote exception, appends with `PricesPending = true` and null prices instead of throwing; guards against a duplicate `Created` per `(SubjectType, SubjectId)` via `IThesisEventRepository.GetLatestForSubjectAsync`) in `backend/src/FinanceSentry.Modules.Research/Application/Services/ThesisEventRecorder.cs`
- [ ] T010 [P] Unit test: `ThesisEventRecorder` never throws when `IMarketDataService` throws — asserts `PricesPending = true`, null prices, event still appended — in `backend/tests/FinanceSentry.Modules.Research.Tests/Unit/ThesisEventRecorderTests.cs`
- [ ] T011 [P] Unit test: `ThesisEventRecorder` appends exactly one `Created` event per subject even if `RecordAsync(Created)` is called twice (idempotency, FR edge case) in same test file as T010
- [ ] T012 Register `IThesisEventRepository`, `IThesisEventRecorder` (scoped) in `backend/src/FinanceSentry.Modules.Research/ResearchModule.cs`

**Checkpoint**: `dotnet build backend/` passes with zero warnings; `thesis_events` table created on migration; recorder is unit-tested in isolation.

---

## Phase 3: User Story 1 — Every lifecycle event is price-stamped (Priority: P1) 🎯 MVP

**Goal**: Thesis creation produces a price-stamped `Created` event with zero manual steps; capture never blocks the write on a quote failure.

**Independent Test**: Create a thesis via `save_thesis`; assert a `Created` event exists with ticker price and SPY price via `list_thesis_events`.

- [ ] T013 [US1] Wire `SaveThesisCommand`'s handler to call `IThesisEventRecorder.RecordAsync(..., ThesisEventType.Created)` immediately after persisting a *new* thesis (only when `id` was null on input — never on update) in `backend/src/FinanceSentry.Modules.Research/Application/Commands/SaveThesisCommand.cs`
- [ ] T014 [P] [US1] Create `ThesisEventDto` (SubjectType, SubjectId, Ticker, EventType, Timestamp, SubjectPrice, BenchmarkPrice, BenchmarkTicker, PricesPending, DecisionNote) in `backend/src/FinanceSentry.Modules.Research/API/Responses/ThesisEventDto.cs`
- [ ] T015 [US1] Create `ListThesisEventsQuery` (UserId, SubjectId?) + handler (delegates to `IThesisEventRepository.ListAsync`, maps to DTOs, scoped by UserId) in `backend/src/FinanceSentry.Modules.Research/Application/Queries/ListThesisEventsQuery.cs`
- [ ] T016 [US1] Create `ListThesisEventsTool` MCP tool (`list_thesis_events`; resolves `userId` via `IIdentityResolver` like `SaveThesisTool`) in `backend/src/FinanceSentry.Mcp/Tools/ListThesisEventsTool.cs`
- [ ] T017 [P] [US1] Contract test: `list_thesis_events` tool is discoverable and named correctly (extend/verify existing reflection-based `ToolNameContractTests` picks it up) in `backend/tests/FinanceSentry.Mcp.Tests/ContractTests/ToolNameContractTests.cs`
- [ ] T018 [P] [US1] Integration/unit test: creating a thesis via `SaveThesisCommand` handler produces exactly one `Created` `ThesisEvent` with non-null prices when `IMarketDataService` succeeds, in `backend/tests/FinanceSentry.Modules.Research.Tests/Unit/SaveThesisCommandEventCaptureTests.cs`
- [ ] T019 [US1] Implement `ThesisTrackRecordSnapshotJob`: (a) find all `ThesisEvent` rows with `PricesPending = true`, re-attempt quote lookup, update in place on success; (b) for every thesis without a terminal event, append a `Snapshot` event with current prices — in `backend/src/FinanceSentry.Modules.Research/Infrastructure/Jobs/ThesisTrackRecordSnapshotJob.cs`
- [ ] T020 [US1] Register `thesis-track-record-snapshot` weekly recurring Hangfire job in the `JobRegistrar` in `backend/src/FinanceSentry.Modules.Research/ResearchModule.cs`
- [ ] T021 [P] [US1] Unit test: `ThesisTrackRecordSnapshotJob` backfills a pending event's prices and appends a `Snapshot` event for an active thesis with no terminal event, in `backend/tests/FinanceSentry.Modules.Research.Tests/Jobs/ThesisTrackRecordSnapshotJobTests.cs`

**Checkpoint**: Creating a thesis in a live stack produces a complete, correctly priced `Created` event with zero manual steps (SC-002); a simulated quote outage still records the event as pending and the weekly job backfills it.

---

## Phase 4: User Story 2 — Performance vs benchmark (Priority: P1)

**Goal**: For any thesis, report return since creation (and between any two events) alongside benchmark return and excess return.

**Independent Test**: Seed a `Created` event at price 100 (SPY 500) and a later quote at 120 (SPY 510); assert return +20%, benchmark +2%, excess +18%.

- [ ] T022 [P] [US2] Create `IThesisPerformanceCalculator` (`Calculate(fromEvent, toEventOrLatestPrice, frictionConfig) -> ThesisPerformanceResult`) — pure, no I/O — in `backend/src/FinanceSentry.Modules.Research/Application/Services/IThesisPerformanceCalculator.cs`
- [ ] T023 [US2] Implement `ThesisPerformanceCalculator`: absolute/benchmark/excess return between two priced events or event→latest quote; `NotEvaluable` result when neither ticker nor `proxyTicker` (017 field — not yet on `InvestmentThesis`, branch present but unreachable until 017 adds it, per research.md R6) is quotable in `backend/src/FinanceSentry.Modules.Research/Application/Services/ThesisPerformanceCalculator.cs`
- [ ] T024 [US2] Add `FrictionConfig` (per-trade cost bps, short/long-term capital-gains rates, holding-period boundary days) bound from `appsettings.json` section `ThesisTrackRecord:Friction`, applied in `ThesisPerformanceCalculator` to compute `NetAbsoluteReturnPct`/`NetExcessReturnPct` per FR-007b, in `backend/src/FinanceSentry.Modules.Research/Application/Services/FrictionConfig.cs`
- [ ] T025 [P] [US2] Unit test: SC-001 determinism — identical inputs produce identical outputs; gross vs net return math with configured friction; not-evaluable path when no quote available, in `backend/tests/FinanceSentry.Modules.Research.Tests/Unit/ThesisPerformanceCalculatorTests.cs`
- [ ] T026 [US2] Create `GetThesisPerformanceQuery` (UserId, Id?, Ticker?) + handler (resolves subject via `IThesisEventRepository`, calls `IThesisPerformanceCalculator` for create→latest) in `backend/src/FinanceSentry.Modules.Research/Application/Queries/GetThesisPerformanceQuery.cs`
- [ ] T027 [US2] Create `GetThesisPerformanceTool` MCP tool (`get_thesis_performance`; accepts `id` or `ticker`, resolves `userId` via `IIdentityResolver`) in `backend/src/FinanceSentry.Mcp/Tools/GetThesisPerformanceTool.cs`
- [ ] T028 [P] [US2] Contract test: `get_thesis_performance` tool discoverable/named correctly (`ToolNameContractTests`) and returns `null`/handles gracefully when neither `id` nor `ticker` resolves, in `backend/tests/FinanceSentry.Mcp.Tests/GetThesisPerformanceToolTests.cs`

**Checkpoint**: `get_thesis_performance` returns correct absolute/benchmark/excess return (gross and net) for a live thesis; not-evaluable path never throws.

---

## Phase 5: User Story 3 — Aggregate track record (Priority: P2)

**Goal**: One call answers counts, hit rate, average/median excess return, source/status splits, and a low-sample caveat.

**Independent Test**: Given a mix of active/broken/promoted/rejected records, `get_track_record` returns counts, hit rate, average/median excess return, split by source and status, with a low-sample caveat below 30 closed records.

- [ ] T029 [US3] Create `GetTrackRecordQuery` (UserId, Source?, Status?) + handler: fetches all subjects' event histories via `IThesisEventRepository`, computes per-subject performance via `IThesisPerformanceCalculator`, aggregates counts/hit rates/avg/median/best/worst, splits by source (`User`/`Scan`) and status, sets `LowSampleCaveat` when `ClosedCount < 30` (constant `MinimumClosedSampleSize`) in `backend/src/FinanceSentry.Modules.Research/Application/Queries/GetTrackRecordQuery.cs`
- [ ] T030 [US3] Create `GetTrackRecordTool` MCP tool (`get_track_record`) in `backend/src/FinanceSentry.Mcp/Tools/GetTrackRecordTool.cs`
- [ ] T031 [P] [US3] Unit test: `GetTrackRecordQuery` handler — separate `TerminalHitRate`/`ActiveHitRate` (never blended per research.md R4); `LowSampleCaveat = true` below 30 closed records, `false` at/above; excluded (`NotEvaluable`) subjects counted in `ExcludedCount`, not in hit-rate denominators, in `backend/tests/FinanceSentry.Modules.Research.Tests/Unit/GetTrackRecordQueryTests.cs`
- [ ] T032 [P] [US3] Contract test: `get_track_record` tool discoverable/named correctly, in `backend/tests/FinanceSentry.Mcp.Tests/GetTrackRecordToolTests.cs`

**Checkpoint**: `get_track_record` answers "is this earning money" in one call (SC-003); rejected candidates' counterfactual performance is queryable once candidates exist (SC-004 — verifiable in full once 019 ships).

---

## Phase 6: Decision journal & post-mortem packet (FR-008b/c)

**Goal**: Capture contemporaneous reasoning per event and compile a period post-mortem packet for Denys's periodic review.

- [ ] T033 [US1] Confirm `DecisionNote` optional parameter flows end-to-end: `IThesisEventRecorder.RecordAsync` already accepts it (T008/T009) — expose it on `SaveThesisCommand`/`SaveThesisTool` as an optional `decisionNote` parameter so callers can supply reasoning at creation time, in `backend/src/FinanceSentry.Modules.Research/Application/Commands/SaveThesisCommand.cs` and `backend/src/FinanceSentry.Mcp/Tools/SaveThesisTool.cs`
- [ ] T034 Create `PostmortemEntry` and `PostmortemPacket` response records (per data-model.md) in `backend/src/FinanceSentry.Modules.Research/API/Responses/PostmortemPacketDto.cs`
- [ ] T035 Create `GetPostmortemPacketQuery` (UserId, PeriodStart, PeriodEnd) + handler: fetches all terminal events in the period via `IThesisEventRepository.ListForPeriodAsync`, pairs each with its `Created` event's `DecisionNote`, computes excess return per entry via `IThesisPerformanceCalculator`, includes rejected-candidate counterfactuals in the same period, in `backend/src/FinanceSentry.Modules.Research/Application/Queries/GetPostmortemPacketQuery.cs`
- [ ] T036 Create `GetPostmortemPacketTool` MCP tool (`get_postmortem_packet`) in `backend/src/FinanceSentry.Mcp/Tools/GetPostmortemPacketTool.cs`
- [ ] T037 [P] Unit test: `GetPostmortemPacketQuery` returns empty `Entries`/`CounterfactualEntries` (no error) when no terminal events exist in the period; correctly pairs `Created`→terminal decision notes when they do, in `backend/tests/FinanceSentry.Modules.Research.Tests/Unit/GetPostmortemPacketQueryTests.cs`
- [ ] T038 [P] Contract test: `get_postmortem_packet` tool discoverable/named correctly, in `backend/tests/FinanceSentry.Mcp.Tests/GetPostmortemPacketToolTests.cs`

**Checkpoint**: A full period's terminal events, decision notes, and counterfactuals are compiled in one call; the system compiles, it does not judge (per spec's explicit non-goal).

---

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T039 [P] Bump backend version `0.9.0` → `0.10.0` in `backend/src/FinanceSentry.API/FinanceSentry.API.csproj`
- [ ] T040 Run quickstart.md manual QA: create a thesis, verify priced `Created` event; simulate a quote outage, verify pending event + job backfill; query all four MCP tools; confirm zero `dotnet build` warnings

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — BLOCKS all user stories
- **Phase 3 (US1)**: Depends on Phase 2 — primary delivery, MVP
- **Phase 4 (US2)**: Depends on Phase 2; independent of Phase 3 implementation details but conceptually reads the events Phase 3 produces — can be built in parallel once Phase 2 is done, verified against seeded test data rather than live Phase-3 events
- **Phase 5 (US3)**: Depends on Phase 4 (`IThesisPerformanceCalculator` reused for per-subject performance inside the aggregate)
- **Phase 6 (Decision journal/postmortem)**: Depends on Phase 2 (recorder) and Phase 4 (calculator, for per-entry excess return)
- **Phase 7 (Polish)**: Depends on Phases 3–6

### User Story Dependencies

- **US1 (P1)**: Foundation complete → full capture + snapshot job + `list_thesis_events`
- **US2 (P1)**: Foundation complete → performance calculator + `get_thesis_performance`; does not require US1's job to be done, only the event schema
- **US3 (P2)**: US2's calculator must exist → `get_track_record`

### Within Phase 2

- T004 (repository interface) and T008 (recorder interface) can run in parallel
- T005 → T006 → T007 must run sequentially (DbContext config → migration → repository impl against the real schema)
- T009 depends on T007 (repository) and T008 (interface)
- T010, T011 depend on T009
- T012 depends on T007, T009

### Parallel Opportunities

```bash
# Phase 1
T001 (ThesisSubjectType) || T002 (ThesisEventType)

# Phase 2
T004 (IThesisEventRepository) || T008 (IThesisEventRecorder interface)
T010 (recorder resilience test) || T011 (idempotency test)   # after T009

# Phase 3
T014 (ThesisEventDto) || T017 (contract test) || T018 (event-capture test)   # after T013/T016
T021 (snapshot job test) can run once T019 lands, independent of T013–T018

# Phase 4
T025 (calculator unit tests) || T028 (contract test)   # after T023/T027

# Phase 5
T031 (aggregate unit test) || T032 (contract test)   # after T029/T030

# Phase 6
T037 (postmortem unit test) || T038 (contract test)   # after T035/T036

# Polish
T039 (version bump) can run in parallel with any task
```

---

## Implementation Strategy

### MVP First (User Story 1 + 2, both P1)

1. Complete Phase 1: Setup (T001–T003)
2. Complete Phase 2: Foundational (T004–T012)
3. Complete Phase 3: User Story 1 (T013–T021)
4. Complete Phase 4: User Story 2 (T022–T028)
5. **STOP and VALIDATE**: Create a thesis → `Created` event priced → `get_thesis_performance` returns correct excess return
6. Deploy/demo

### Incremental Delivery

1. Phase 1 + 2 → foundation ready
2. US1 (Phase 3) → every lifecycle event price-stamped, weekly snapshot/backfill job running (MVP part 1)
3. US2 (Phase 4) → performance vs benchmark queryable (MVP part 2)
4. US3 (Phase 5) → aggregate track record (`get_track_record`)
5. Phase 6 → decision journal + post-mortem packet
6. Polish (Phase 7) → version bump + QA pass

---

## Notes

- [P] tasks = different files, no dependencies on each other
- [Story] label maps to spec.md user story
- No frontend tasks: spec `[OUT OF SCOPE]` explicitly excludes UI beyond MCP/REST in v1
- `SubjectType.Candidate` paths (Promoted/Rejected/Expired event types, `BySource["Scan"]` slice) are modeled and testable in isolation now, but only exercisable end-to-end once 019 ships and calls `IThesisEventRecorder` from its own write paths — no task here blocks on 019, but full acceptance of SC-004 (rejected-candidate counterfactuals) can only be demonstrated after 019 lands
- 017's break/unbreak write path is expected to call `IThesisEventRecorder.RecordAsync(..., Broken/Unbroken)` when it's implemented — not a task in this feature; flagged here so 017's implementer wires it rather than reinventing capture
- Run `dotnet build backend/` after every `.cs` change; zero warnings required before a task is considered done
