# Feature Specification: Portfolio Scanner

**Feature Branch**: `043-portfolio-scanner`

**Created**: 2026-09-01

**Status**: Implementing

**GitHub Issue**: #413

## Context

ROADMAP identifies the Portfolio Scanner as "exists in pieces — formalized as signals later." This spec formalizes it: a daily Hangfire job that reads the canonical book and emits four signal types into the shared `radar_signals` log (silent tier — no alerts, silent accumulation). Ledger can then cite trend over ≥2 weeks ("drift widening 3 weeks running").

---

## User Scenarios

### [US1] Daily portfolio-state signals written to the log (P1)

The scanner runs nightly after sync jobs. For each user with an IPS or risk rules, it emits:
- Per-sleeve allocation drift vs IPS targets
- Top-position concentration weight vs MaxPositionWeightPct rule
- Cash buffer level vs MinCashBufferPct rule
- Sync health / staleness of the book sources

**Acceptance Scenarios**:

1. **Given** a user has an IPS with equity sleeve at 60% target and actual at 75%, **When** the scanner runs, **Then** an `allocation_drift` signal with severity `Notable` and status `OverBand` is written for the equity sleeve.
2. **Given** the scanner already ran for a given user+sleeve+day, **When** it runs again, **Then** no duplicate signal is appended (idempotent per day via OneTime DedupKey).
3. **Given** no IPS exists for a user, **When** the scanner runs, **Then** no `allocation_drift` signals are written for that user.
4. **Given** a user has a MaxPositionWeightPct of 20% and their top position is 25%, **When** the scanner runs, **Then** a `concentration_weight` signal with severity `Notable` is written.
5. **Given** a user's book sources are stale, **When** the scanner runs, **Then** a `sync_health` signal with severity `Notable` is written naming the stale sources.

### [US2] Signals queryable via existing MCP tools (P2)

Signals written by the portfolio scanner appear in `list_signals` (filter: `scanner=portfolio_scanner`) and contribute to `get_radar_summary`. No new MCP tools or endpoints needed — the scanner is purely a producer.

**Acceptance Scenarios**:

1. **Given** the scanner has run for a user today, **When** `list_signals` is called with `scanner=portfolio_scanner`, **Then** the day's portfolio signals are returned.

---

## Functional Requirements

- **FR-001**: Scanner runs as a Hangfire recurring job, nightly, after banking/brokerage sync jobs.
- **FR-002**: Scanner is idempotent per (day, user, signal type, subject) — re-runs produce no duplicates. Implemented via `OneTime=true` + date-keyed DedupKey.
- **FR-003**: Scanner emits no `Alert` entities — purely silent `radar_signals` accumulation.
- **FR-004**: Scanner emits `allocation_drift` signals per IPS sleeve — Notable when OverBand/UnderBand, Info when Within.
- **FR-005**: Scanner emits `concentration_weight` for the top position — Notable when > MaxPositionWeightPct, Info otherwise.
- **FR-006**: Scanner emits `cash_buffer` when a MinCashBufferPct rule exists — Notable when below threshold.
- **FR-007**: Scanner emits `sync_health` — Notable when any book source is stale.
- **FR-008**: Scanner reads book figures via `IBookFiguresService` (canonical service, feature 411).
- **FR-009**: Scanner reads IPS + allocation drift via the existing `GetAllocationDriftQuery` pipeline.
- **FR-010**: Scanner reads risk rule thresholds via `IRiskRuleSetRepository`.
- **FR-011**: Cross-module access uses the `FinanceSentry.Integration` adapter pattern (port in Modules.Radar, adapter in Integration).

## Signal Constants

| Constant | Value |
|---|---|
| `RadarScanners.Portfolio` | `"portfolio_scanner"` |
| `RadarSignalTypes.AllocationDrift` | `"allocation_drift"` |
| `RadarSignalTypes.ConcentrationWeight` | `"concentration_weight"` |
| `RadarSignalTypes.CashBuffer` | `"cash_buffer"` |
| `RadarSignalTypes.SyncHealth` | `"sync_health"` |
| `RadarSubjectTypes.AssetClass` | `"AssetClass"` |
| `RadarSubjectTypes.Portfolio` | `"Portfolio"` |

## Success Criteria

- **SC-001**: After the job runs, `list_signals?scanner=portfolio_scanner` returns signals for all users with IPS or risk rules.
- **SC-002**: Running the job twice for the same day produces identical signal counts (idempotency).
- **SC-003**: Existing `012` alert count is unchanged after the job runs.
- **SC-004**: Unit tests cover: drift severity mapping, concentration threshold, cash buffer threshold, sync staleness, per-day idempotency.

## Assumptions

- IPS data lives in Research module (`IIpsRepository`); risk rules live in Risk module (`IRiskRuleSetRepository`). Cross-module access via Integration layer adapter pattern (established precedent: `IpsAllocationPolicySource`, `RadarPortfolioValueSource`).
- `BookFigures.IsStale` + `StaleSources` are sufficient for sync health determination.
- Users without IPS get no `allocation_drift` signals; users without risk rules get no `cash_buffer` or `concentration_weight` signals (thresholds undefined).
- Top-position concentration: emit for the single highest-weight position.
