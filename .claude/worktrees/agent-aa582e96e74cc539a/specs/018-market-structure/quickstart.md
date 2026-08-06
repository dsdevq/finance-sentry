# Quickstart: Market Structure Scanner + Radar Signal Log

## Run the stack

```bash
cd docker && docker compose -f docker-compose.dev.yml up -d postgres api
# health: GET http://localhost:5001/api/v1/health -> {"status":"healthy"}
```

## Migration

Radar's `M001_InitialSchema` (schema `radar`: `daily_bars`, `radar_signals`,
`radar_universe_members`) applies on API startup via `MigrateAllModules` — after
`MigrateContext<RadarDbContext>` is registered in `MigrationExtensions.cs`. Manual:

```bash
dotnet ef database update \
  --project backend/src/FinanceSentry.Modules.Radar --context RadarDbContext
```

Verify: `\dt radar.*` shows the three tables; history table `__ef_migrations_history_radar` exists.

## First run (calibration = log-only)

Trigger the Hangfire jobs from the dashboard (http://localhost:5001/hangfire) or wait for schedule:
1. `radar-ingestion` — seeds the universe (SPY + 11 SPDR sectors + SMH + holdings + watchlist) and
   ingests ~300 daily bars per ticker (idempotent; re-run appends only new days).
2. `radar-compute` — computes structure and appends signals (`ScannerMode=LogOnly` → **zero Alerts**).
3. `radar-freshness-watchdog` — alerts if any bar is stale.

## Query via MCP

```
get_radar_summary                 # leaders/laggards + breadth + today's notable signals
get_market_structure  MU          # RS/MA/z-score/extension for one ticker (+ stale flag)
get_sector_rotation               # sector ranks + 21d rank deltas
list_signals type=unusual_move    # recorded signals with evidence payloads
```

## Verify (spec Independent Tests)

- **US1**: seed 3-ticker universe → ingest → ≥200 bars/ticker; re-run inserts only missing days (unique on ticker+date).
- **US2**: bars where A outperforms SPY and B underperforms over 21d → A's RS > 0 > B's RS; ranking orders A above B.
- **US4**: run scanner → `list_signals` by day+type returns emitted signals with payloads; `notable`+ deduped within silence window.

## Historical validation before enabling alerts (FR-016)

```
# one-off replay over >=5y persisted bars — counts signal frequency/precision across
# 2020 crash, 2022 unwind, 2026-07 memory rotation
run RunHistoricalValidationCommand   # via a maintenance job / MCP admin path
```
Only after the replay + 2–4 weeks of observed distributions do you flip `ScannerMode` to `Alerting`.

## Build gate

```bash
dotnet build backend/    # zero warnings before any task is complete
dotnet test backend/tests/FinanceSentry.Modules.Radar.Tests
```
