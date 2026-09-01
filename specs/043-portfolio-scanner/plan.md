# Plan: Portfolio Scanner (043)

## Slice map

### [US1] Daily portfolio-state signals (this PR)

**Files/areas touched:**

| File | Change |
|---|---|
| `Modules.Radar/Domain/RadarConstants.cs` | Add `Portfolio` scanner + 4 signal types + `AssetClass`/`Portfolio` subject types |
| `Modules.Radar/Domain/Ports/IPortfolioScanDataReader.cs` | New port: `IPortfolioScanDataReader` + DTOs (`PortfolioScanData`, `ScanSleeveDrift`, `ScanPosition`) |
| `Modules.Radar/Application/Commands/ComputePortfolioSignalsCommand.cs` | Command + handler: iterates users, reads scan data, emits 4 signal types |
| `Modules.Radar/Infrastructure/Jobs/PortfolioScannerJob.cs` | Hangfire job wrapping the command |
| `Modules.Radar/RadarModule.cs` | Register job + RegimeComputeJob pattern |
| `Integration/PortfolioScanDataReader.cs` | Adapter: reads IPS drift (via GetAllocationDriftQuery handler), book figures, and risk rules |
| `Integration/CrossModulePortRegistration.cs` | Register `IPortfolioScanDataReader` → `PortfolioScanDataReader` |
| `Modules.Radar.Tests/Portfolio/PortfolioScannerTests.cs` | Unit tests for signal-type logic |

### [US2] Queryable via existing MCP tools

No new files — signals are queryable through the existing `list_signals` / `get_radar_summary` tools by filtering `scanner=portfolio_scanner`. No code changes needed.

## Key decisions

- **Port in Modules.Radar, adapter in Integration** — established precedent (`IAllocationPolicySource` → `IpsAllocationPolicySource`, `IPortfolioValueSource` → `RadarPortfolioValueSource`).
- **Idempotency via `OneTime=true` + date-keyed DedupKey** — the existing `RadarSignalWriter.AppendSignalAsync` suppresses ever-seen DedupKeys when `OneTime=true`. A key like `portfolio_scanner:allocation_drift:{userId}:Equity:2026-09-01` is new each calendar day → daily write, same-day retry suppressed.
- **Severity mapping**: `Notable` for policy violations (OverBand/UnderBand drift, position over limit, cash below threshold, stale book); `Info` for within-policy baseline reads.
- **User source**: `IPortfolioScanDataReader.GetScanUserIdsAsync()` — adapter returns union of users with risk rules + users with IPS. Scanner is no-op for users with empty book.
- **No new MCP tools or migrations** — signals go into the existing `radar_signals` table; the `list_signals` scanner filter handles querying.
- **Concentration check**: emit one signal per day for the top position (by USD value) — Notable if > `MaxPositionWeightPct`, Info otherwise. Skip if no positions.
- **Cash buffer**: emit when `MinCashBufferPct` is set; skip otherwise.
- **Sync health**: one signal per user per day — Notable when `BookFigures.IsStale`, Info when fresh.
