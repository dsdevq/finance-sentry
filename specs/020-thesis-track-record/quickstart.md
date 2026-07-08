# Quickstart: Thesis Track Record (manual QA)

Prerequisite: full Docker stack up (`cd docker && docker compose -f docker-compose.dev.yml up -d`), migration `M004_ThesisEvents` applied.

1. **Create a thesis** via `save_thesis` MCP tool (or `ThesesController`) for a real ticker (e.g. `AAPL`).
   - Assert: `list_thesis_events(subjectId)` returns one `Created` event with `PricesPending = false`, non-null `SubjectPrice`/`BenchmarkPrice` (SPY).
2. **Simulate a quote outage** (stop network to Yahoo, or point `IMarketDataService` at a bad ticker temporarily) and create a second thesis.
   - Assert: the `Created` event is still recorded, with `PricesPending = true` and null prices — the write never fails.
3. **Trigger `ThesisTrackRecordSnapshotJob`** manually via the Hangfire dashboard (`/hangfire`).
   - Assert: the pending event from step 2 now has prices; a `Snapshot` event is appended for every thesis without a terminal event.
4. **Query performance**: call `get_thesis_performance(ticker: "AAPL")`.
   - Assert: `AbsoluteReturnPct`, `BenchmarkReturnPct`, `ExcessReturnPct` all present and consistent with the recorded prices vs. current quote.
5. **Query aggregate**: call `get_track_record()`.
   - Assert: `TotalCount >= 2`, `LowSampleCaveat = true` (fewer than 30 closed records), `BySource["Scan"]` empty (019 not shipped).
6. **Query post-mortem packet**: call `get_postmortem_packet(periodStart, periodEnd)` spanning today.
   - Assert: empty `Entries`/`CounterfactualEntries` (no terminal events yet) — packet call succeeds, doesn't error, on a system with no closed records.
7. **Confirm no regression**: existing `save_thesis`/`list_theses`/`delete_thesis` tools behave unchanged; `dotnet build backend/` has zero warnings.
