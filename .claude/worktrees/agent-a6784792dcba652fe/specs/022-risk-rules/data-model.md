# Phase 1 Data Model: Risk Rules

## Persisted entities (`RiskDbContext`, new schema `risk`)

### `RiskRuleSet` (table: `risk_rule_sets`)

Versioned per `FR-001`. A new save appends a row and flips `IsCurrent`; never mutates a prior version (audit trail).

| Column | Type | Notes |
|---|---|---|
| `Id` | `uuid` | PK |
| `UserId` | `uuid` | indexed |
| `Version` | `int` | monotonically increasing per user |
| `IsCurrent` | `bool` | exactly one true row per user |
| `MaxPositionWeightPct` | `decimal?` | e.g. 0.25 = 25% |
| `MaxSleeveWeightPct` | `decimal?` | optional per-sleeve cap |
| `MinCashBufferPct` | `decimal?` | optional |
| `MaxLossPerThesisPct` | `decimal?` | from entry; feeds 017's `price_drawdown` default |
| `MaxNewPositionPct` | `decimal?` | sizing cap for any single new bet |
| `TurnoverBudgetPerQuarter` | `int?` | discretionary trades/quarter cap |
| `AllocationTargetsJson` | `jsonb?` | `[{ AssetClass, TargetPct, DriftBandPct }]` — same shape as Research's `AllocationTarget`, duplicated here (not shared) because Risk and Research are independent modules with independent schemas |
| `CreatedAt` | `timestamptz` | |

Validation (`SaveRiskRuleSetCommand`): weights in `(0, 1]`, percentages non-negative and sane (`<= 1` for fractional fields), `TurnoverBudgetPerQuarter >= 0` if set. All fields individually optional — an unset rule is simply not checked (never defaulted).

### `PolicyViolationAck` (table: `policy_violation_acks`)

| Column | Type | Notes |
|---|---|---|
| `Id` | `uuid` | PK |
| `UserId` | `uuid` | indexed |
| `RuleKey` | `text` | e.g. `"MaxPositionWeight"` |
| `Subject` | `text` | e.g. ticker `"DRAM"` |
| `AcknowledgedAt` | `timestamptz` | |
| `RemediationNote` | `text` | free text, e.g. "trim DRAM on strength to ≤30% by Q4" |
| `WorseningStepPct` | `decimal` | re-alert threshold — excess growing past this since ack re-opens the violation |
| `ObservedAtAck` | `decimal` | the observed value (e.g. weight) at acknowledgement time, baseline for the worsening check |

Unique index on `(UserId, RuleKey, Subject)` where still active — one live ack per violation identity; a fresh violation that reopens (worsened) supersedes rather than duplicates.

### `HoldingSnapshot` (table: `holding_snapshots`)

Own point-in-time capture — see `research.md` R1 for why this can't reuse BrokerageSync/CryptoSync tables.

| Column | Type | Notes |
|---|---|---|
| `Id` | `uuid` | PK |
| `UserId` | `uuid` | indexed |
| `Symbol` | `text` | |
| `Sleeve` | `text` | `brokerage` \| `crypto` \| `banking` |
| `Quantity` | `decimal` | |
| `UsdValue` | `decimal` | |
| `CapturedAt` | `timestamptz` | indexed, one row per `(UserId, Symbol, Sleeve)` per `RiskCheckJob` run |

Retention: not pruned in v1 (low volume — one row per position per day); a retention job is an explicit out-of-scope note, same posture as 018's `radar_signals` retention follow-up.

## In-memory / transient types (not persisted)

- **`BookSnapshot`** — `(TotalUsd, Positions: IReadOnlyList<BookPosition>, IsStale, StaleSources: IReadOnlyList<string>)`; `BookPosition(Symbol, Sleeve, Quantity, UsdValue, WeightPct)`. Built fresh per check run by `BookSnapshotReader`, never stored directly (the `HoldingSnapshot` rows are the durable trace of what a `BookSnapshot` looked like at time T).
- **`PolicyViolation`** — `(RuleKey, Subject, ObservedValue, LimitValue, ExcessUsd, ExcessPct, Status)` where `Status` is `New | Acknowledged | Worsened`.
- **`RiskVerdict`** — `(Decision: Allowed | Refused, RuleKey?, ObservedValue?, LimitValue?, MaxCompliantSizeUsd?, HeadroomUsd?)`.
- **`ComplianceReport`** — `(GeneratedAt, IsStale, Violations: IReadOnlyList<PolicyViolation>, CorrelationFacts?, StressLine?)` — the `check_risk_rules()` no-arg response.

## Relationships

- `RiskRuleSet` (current version) is the sole configuration input to `RiskEvaluationService`.
- `PolicyViolationAck` rows are looked up by `(RuleKey, Subject)` against freshly computed violations each run — matches suppress/re-open per R8.
- `HoldingSnapshot` rows feed `TurnoverTracker` (count distinct `(Symbol, Sleeve)` quantity-increase events in the trailing 90 days) and the add-to-broken-thesis check (compare latest two snapshots' `Quantity` for a symbol whose corresponding `InvestmentThesis.BrokenAt` is non-null and precedes the increase).
- No foreign keys into other modules' tables — `Subject`/`Symbol` are plain strings (tickers), consistent with how BrokerageHolding/CryptoHolding already key by symbol string, not a shared entity id.
