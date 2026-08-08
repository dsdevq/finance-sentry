# MCP Tool Contract: `get_market_regime`

**Tool type**: `[McpServerToolType]` class `GetMarketRegimeTool` in `FinanceSentry.Mcp/Tools/`.
**Handler**: `IQueryHandler<GetMarketRegimeQuery, RegimeStateDto>` (Radar module), DI-resolved, auto-registered by assembly scan.

## Input

| Param | Type | Required | Notes |
|---|---|---|---|
| `userId` | `Guid?` | no | defaults to authenticated MCP identity (regime is global, so it does not affect the result — accepted for tool-shape consistency) |

## Output — `RegimeStateDto`

Both axes are always present as independent objects; an unavailable axis is reported explicitly, never fabricated.

```json
{
  "asOf": "2026-08-08T23:00:00Z",
  "volatility": {
    "available": true,
    "regime": "Stressed",
    "vixLevel": 24.6,
    "vixSma": 21.2,
    "trend": "Rising",
    "lastChange": "2026-08-05T23:00:00Z"
  },
  "rates": {
    "available": true,
    "regime": "Inverted",
    "dgs10": 3.71,
    "dgs2": 4.08,
    "spread": -0.37,
    "recessionWarning": true,
    "growthValueTilt": "quality/defensive (recession-warning)",
    "lastChange": "2026-07-30T23:00:00Z"
  }
}
```

### Unavailable-axis example (FRED keyless, VIX fetched)

```json
{
  "asOf": "2026-08-08T23:00:00Z",
  "volatility": { "available": true, "regime": "Calm", "vixLevel": 13.1, "vixSma": 13.8, "trend": "Falling", "lastChange": "2026-08-01T23:00:00Z" },
  "rates": { "available": false, "regime": null, "dgs10": null, "dgs2": null, "spread": null, "recessionWarning": false, "growthValueTilt": null, "lastChange": null }
}
```

### No reading ever computed

```json
{ "asOf": null, "volatility": { "available": false, "regime": null, ... }, "rates": { "available": false, "regime": null, ... } }
```

## Guarantees

- The two axes are never merged into one label (FR-010).
- `available: false` + `regime: null` is the only representation of an unavailable axis (FR-017) — no default band.
- Read-only; never triggers a fetch or a compute (reads the persisted latest `regime_readings` row).
