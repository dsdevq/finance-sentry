# Data Model: Thesis Track Record

## ThesisEvent (new, append-only)

Table: `thesis_events` (in `ResearchDbContext`, alongside `investment_theses`, `watchlist_items`, `ips`).

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK, `Guid.NewGuid()` |
| `UserId` | `Guid` | Owning user — denormalized onto the event (not derived via a join at query time) so every read is user-scoped without joining back to `InvestmentThesis`/a future `Candidate` table |
| `SubjectType` | `ThesisSubjectType` (enum: `Thesis`, `Candidate`) | |
| `SubjectId` | `Guid` | FK-by-convention to `InvestmentThesis.Id` (or a future 019 `Candidate.Id`) — no EF navigation/FK constraint across the two concepts since `Candidate` doesn't exist yet; validated in the recorder, not the DB |
| `Ticker` | `string` | Denormalized from the subject at event time (tickers don't change, but avoids a join for every read) |
| `EventType` | `ThesisEventType` (enum: `Created`, `Broken`, `Unbroken`, `Closed`, `Promoted`, `Rejected`, `Expired`, `Snapshot`) | |
| `Timestamp` | `DateTimeOffset` | UTC, set at capture time |
| `SubjectPrice` | `decimal?` | Null when `PricesPending` |
| `BenchmarkPrice` | `decimal?` | SPY price at `Timestamp`; null when `PricesPending` |
| `BenchmarkTicker` | `string` | Defaults to `"SPY"`; stored per-row (not global config) so a future benchmark change doesn't retroactively reinterpret old events |
| `PricesPending` | `bool` | `true` until the snapshot job successfully backfills |
| `DecisionNote` | `string?` | FR-008b — free text, optional, contemporaneous reasoning |

**Indexes**: composite `(SubjectType, SubjectId, Timestamp)` per FR-001; secondary `(UserId, PricesPending)` to make the backfill job's pending-lookup query cheap; secondary `(UserId, EventType, Timestamp)` to support `get_postmortem_packet`'s terminal-event-by-period query.

**Invariants** (enforced in `ThesisEventRecorder`/`ThesisEventRepository`, not DB constraints — keeps the table itself dumb and append-only):
- Exactly one `Created` event per `(SubjectType, SubjectId)` — recorder checks before appending.
- No row is ever updated except `SubjectPrice`/`BenchmarkPrice`/`PricesPending` by the backfill job (the only sanctioned "mutation" of an append-only row — filling in previously-unknown data, not correcting recorded facts, so FR-009 "corrections append" still holds for anything the job doesn't touch).

## ThesisPerformanceResult (calculator output — DTO, not persisted)

| Field | Type | Notes |
|---|---|---|
| `SubjectId` | `Guid` | |
| `FromEvent` / `ToEvent` | `ThesisEventType` | e.g. `Created` → `Broken`, or `Created` → latest |
| `AbsoluteReturnPct` | `decimal?` | Null when `NotEvaluable` |
| `BenchmarkReturnPct` | `decimal?` | |
| `ExcessReturnPct` | `decimal?` | `AbsoluteReturnPct - BenchmarkReturnPct` |
| `NetAbsoluteReturnPct` | `decimal?` | After friction/tax (FR-007b) |
| `NetExcessReturnPct` | `decimal?` | |
| `IsEvaluable` | `bool` | `false` when neither ticker nor proxy is quotable |
| `ExclusionReason` | `string?` | Set when `!IsEvaluable` |
| `PriceSourceUsed` | `string` | Ticker actually used (thesis ticker or `proxyTicker`) — per Edge Cases |

## TrackRecordSummary (aggregate query output — DTO, not persisted)

| Field | Type | Notes |
|---|---|---|
| `TotalCount` | `int` | All subjects with at least a `Created`/`Promoted` event |
| `ClosedCount` | `int` | Subjects with a terminal event |
| `ExcludedCount` | `int` | Not-evaluable subjects (R6) |
| `TerminalHitRate` | `decimal?` | Null if `ClosedCount == 0` |
| `ActiveHitRate` | `decimal?` | Mark-to-market on still-open subjects |
| `AverageExcessReturnPct` | `decimal?` | |
| `MedianExcessReturnPct` | `decimal?` | |
| `BestExcessReturnPct` / `WorstExcessReturnPct` | `decimal?` | |
| `BySource` | `Dictionary<string, TrackRecordSlice>` | `"User"` vs `"Scan"` — `"Scan"` slice will be empty until 019 ships |
| `ByStatus` | `Dictionary<string, TrackRecordSlice>` | Active / Broken / Closed / Promoted / Rejected / Expired |
| `LowSampleCaveat` | `bool` | `true` when `ClosedCount < 30` |

## PostmortemPacket (query output — DTO, not persisted)

| Field | Type | Notes |
|---|---|---|
| `PeriodStart` / `PeriodEnd` | `DateOnly` | |
| `Entries` | `IReadOnlyList<PostmortemEntry>` | One per terminal event in the period |
| `PostmortemEntry` | record | `SubjectId`, `Ticker`, `DecisionNoteAtCreation`, `DecisionNoteAtTerminal`, `EntryPrice`, `ExitPrice`, `ExcessReturnPct`, `EventTypeTerminal` |
| `CounterfactualEntries` | `IReadOnlyList<PostmortemEntry>` | Rejected candidates in the same period, same shape |

## MCP Tool Contracts (summary — full parameter docs in tool source per existing `[Description]` convention)

| Tool | Params | Returns |
|---|---|---|
| `get_track_record` | `source?` (`User`/`Scan`), `status?`, `userId?` | `TrackRecordSummary` |
| `get_thesis_performance` | `id?: Guid`, `ticker?: string`, `userId?` | `ThesisPerformanceResult` (create→latest); 400/null if neither `id` nor `ticker` resolves a subject |
| `list_thesis_events` | `subjectId?: Guid`, `userId?` | `IReadOnlyList<ThesisEventDto>` |
| `get_postmortem_packet` | `periodStart: DateOnly`, `periodEnd: DateOnly`, `userId?` | `PostmortemPacket` |

All four resolve `userId` from `IIdentityResolver` when not explicitly supplied, exactly like `SaveThesisTool`.
