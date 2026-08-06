# Phase 1 Data Model: Market Structure Scanner + Radar Signal Log

New `RadarDbContext` (schema `radar`). Three tables. All computation DTOs are in-memory only.

## Table: `daily_bars`

| Field | Type | Rule / Notes |
|---|---|---|
| `Id` | Guid | PK, `gen_random_uuid()` |
| `Ticker` | string | indexed; upper-invariant |
| `Date` | DateOnly | trading day |
| `Open` `High` `Low` `Close` | decimal(18,6) | raw OHLC |
| `AdjClose` | decimal(18,6) | adjusted close — **used for all return math** (splits/divs) |
| `Volume` | long | shares |

**Unique index** `(Ticker, Date)` — idempotent ingestion (US1.2). Index `(Ticker, Date desc)` for
latest-bar / window reads. Bars are effectively immutable once written.

## Table: `radar_signals` (the shared platform table)

| Field | Type | Rule / Notes |
|---|---|---|
| `Id` | Guid | PK |
| `Timestamp` | DateTimeOffset | UTC emission time |
| `Scanner` | string | e.g. `market_structure`, `thesis_monitor` (017), `opportunity` (019) |
| `SignalType` | string | e.g. `rotation_shift`, `held_sector_laggard`, `breadth`, `unusual_move`, `extended` |
| `Severity` | string enum | `info` \| `notable` \| `alerted` |
| `SubjectType` | string enum | `Ticker` \| `Sector` \| `Universe` |
| `Subject` | string | ticker/sector key, or `"universe"` |
| `UserId` | Guid? | set for holder-scoped signals (held_sector_laggard, unusual_move on a holding); null for global (breadth, rotation) |
| `DedupKey` | string | deterministic key (e.g. `market_structure:unusual_move:MU:2026-07-07`) |
| `Payload` | jsonb | computed evidence (via `HasConversion`, Web JSON) |
| `PayloadVersion` | int | payload-shape version per signal type (default 1) |

**Indexes**: `(Timestamp)`, `(Scanner, SignalType)`, `(Subject)`, and `(DedupKey)` for dedup lookup.
**Append-only** (FR-007): no scanner updates another's rows. **Retention**: `info` pruned after config
horizon (default 2y); `notable`/`alerted` kept indefinitely.

## Table: `radar_universe_members`

| Field | Type | Rule / Notes |
|---|---|---|
| `Id` | Guid | PK |
| `Ticker` | string | unique(Active) |
| `Kind` | string enum | `Benchmark` \| `Sector` \| `Industry` \| `Holding` \| `Watchlist` |
| `Source` | string enum | `Seed` \| `Auto` |
| `Active` | bool | de-activated (not deleted) when a ticker leaves holdings/watchlist — history retained |

Seed rows: `SPY`(Benchmark); `XLB XLC XLE XLF XLI XLK XLP XLRE XLU XLV XLY`(Sector); `SMH`(Industry).
`Holding`/`Watchlist` rows synced each run from `IBrokerageHoldingsReader`(equity) / `IWatchlistReader`.

## Enums

- `SignalSeverity`: `Info`, `Notable`, `Alerted`
- `UniverseKind`: `Benchmark`, `Sector`, `Industry`, `Holding`, `Watchlist`
- `SignalSubjectType`: `Ticker`, `Sector`, `Universe`
- `ScannerMode`: `LogOnly` (default), `Alerting`

## Core interface DTOs (new, in `FinanceSentry.Core/Interfaces`)

- `IMarketHistorySource.GetDailyBarsAsync(string ticker, DateOnly since, CancellationToken)` →
  `IReadOnlyList<DailyBarData>` where `DailyBarData(DateOnly Date, decimal Open, decimal High,
  decimal Low, decimal Close, decimal AdjClose, long Volume)`.
- `IRadarSignalWriter.AppendSignalAsync(RadarSignalRequest, CancellationToken)` where
  `RadarSignalRequest(string Scanner, string SignalType, SignalSeverity Severity, string SubjectType,
  string Subject, Guid? UserId, string DedupKey, object Payload, int PayloadVersion)`.
  *(SignalSeverity crosses the module boundary → the enum lives in Core alongside the interface.)*
- `IWatchlistReader.ListTickersAsync(Guid userId, CancellationToken)` → `IReadOnlyList<string>`.

## Computation DTOs (in-memory, `Domain/MarketStructure`)

- `TickerStructure(string Ticker, IReadOnlyDictionary<int,decimal?> ReturnByWindow,
  IReadOnlyDictionary<int,decimal?> RsByWindow, decimal? Ma20, decimal? Ma50, decimal? Ma200,
  decimal? ExtensionFromMa50, decimal? Vol63, decimal? TodayZScore, decimal? VolumeRatio, bool Stale)`
- `SectorRotationRow(string Sector, int Window, int Rank, int? RankDelta)`
- `BreadthResult(decimal? PctAboveMa20, decimal? PctAboveMa50, decimal? PctAboveMa200, int Evaluated)`
- `RadarSummary(IReadOnlyList<SectorRotationRow> Leaders, Laggards, BreadthResult Breadth,
  IReadOnlyList<RadarSignalDto> TodayNotable, bool Stale)`
- `IngestRunSummary(int TickersIngested, int BarsAdded, int Errors, IReadOnlyList<string> FailedTickers)`
- `ComputeRunSummary(int TickersComputed, IReadOnlyDictionary<string,int> SignalsByType, int Errors)`

Windows constant set: `{21, 63, 126, 252}`. A window returns **null** when fewer bars exist
(not-evaluable, never zero — SC-001 edge).

## MCP tool contracts (summary; full docs in `contracts/`)

| Tool | Params | Returns |
|---|---|---|
| `get_market_structure` | `ticker`, `userId?` | `TickerStructure` (with `stale` flag) |
| `get_relative_strength` | `tickers?`, `userId?` | `TickerStructure[]` RS-focused |
| `get_sector_rotation` | `userId?` | `SectorRotationRow[]` + leaders/laggards |
| `get_market_breadth` | `userId?` | `BreadthResult` |
| `list_signals` | `since?`, `scanner?`, `type?`, `subject?`, `userId?` | `RadarSignalDto[]` |
| `get_radar_summary` | `userId?` | `RadarSummary` (Ledger's first call) |

All reads are pure over persisted bars — **never trigger ingestion** (FR-011). All resolve
`userId` from `IIdentityResolver` when absent.
