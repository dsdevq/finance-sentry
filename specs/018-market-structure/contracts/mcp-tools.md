# MCP Tool Contracts: Radar Market Structure (6 tools)

All tools: `[McpServerToolType]`, primary-ctor DI of a `IQueryHandler<,>` + `IIdentityResolver`,
`[McpServerTool(Name=…)]` + `[Description]`, resolve `userId ?? identity.GetUserId()`. **Reads never
trigger ingestion** (FR-011). All add to the `ToolNameContractTests` allowlist (33 → 39) + parity facts.

---

## `get_market_structure`
- **Params**: `ticker: string`, `userId?: Guid`
- **Returns**: `TickerStructure` — returns/RS by window {21,63,126,252}, MA20/50/200, extension,
  63-day σ, today z-score, volume ratio, and `stale: bool` (FR-017). Windows with insufficient bars
  are `null` (not evaluable), never 0.

## `get_relative_strength`
- **Params**: `tickers?: string[]` (default = full universe), `userId?: Guid`
- **Returns**: `TickerStructure[]` focused on RS-vs-benchmark per window, ordered by RS(63) desc.

## `get_sector_rotation`
- **Params**: `userId?: Guid`
- **Returns**: `SectorRotationRow[]` — each sector ETF's rank per window + `rankDelta` vs 21 trading
  days prior; plus convenience `leaders`/`laggards`. Deltas ≥ configured threshold correspond to a
  recorded `rotation_shift` signal.

## `get_market_breadth`
- **Params**: `userId?: Guid`
- **Returns**: `BreadthResult` — % of universe above MA20/50/200 and `evaluated` count.

## `list_signals`
- **Params**: `since?: DateOnly`, `scanner?: string`, `type?: string`, `subject?: string`, `userId?: Guid`
- **Returns**: `RadarSignalDto[]` — `timestamp, scanner, signalType, severity, subjectType, subject,
  dedupKey, payload, payloadVersion`. Filters are ANDed; `since` defaults to today.

## `get_radar_summary`
- **Params**: `userId?: Guid`
- **Returns**: `RadarSummary` — today's `notable`+ signals + sector leaders/laggards (with rank deltas)
  + breadth, in one payload (SC-004; Ledger's first-call tool). Carries `stale` when computed over
  stale bars.

---

## Cross-module contract (not MCP): `IRadarSignalWriter`

Defined in `FinanceSentry.Core/Interfaces`. Other scanners (017/019) inject it to append to
`radar_signals` with no dependency on the Radar module.

```csharp
Task AppendSignalAsync(RadarSignalRequest request, CancellationToken ct = default);
// dedup: notable+ suppressed within the configured silence window by DedupKey; info recorded every run.
```

## Behaviour contracts

- Determinism (SC-001): identical persisted bars → identical tool output.
- Log-only launch (FR-015): no tool triggers an Alert; alerting is a scheduled-compute concern gated
  by `ScannerMode`.
- Staleness (FR-017): any read over data older than the freshness bound sets `stale=true` in the payload.
