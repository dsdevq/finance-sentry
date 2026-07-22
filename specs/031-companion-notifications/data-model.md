# Data Model: Companion Notification Modes + Event-Driven Push

New context: **`CompanionDbContext`** — schema `companion`, history table `__ef_migrations_history_companion`. Migration `M001_InitialSchema` (with `.Designer.cs`).

## Enums (stored as string)

- **`NotificationMode`**: `Quiet | Digest | Scan | Realtime`. Default `Scan`.
- **`CompanionEventKind`**: `RiskViolation | SyncFailure | UnusualSpend | Opportunity | ThesisBreak | AnalystAction`.
- **`EventDisposition`**: `Pending | Dispatched | HeldForDigest | Delivered | SuppressedByMode | SuppressedByDedup | SuppressedByRateLimit | DeferredQuietHours | Failed`.

## Entity: `CompanionNotificationSetting` (table `companion_notification_settings`)

One row per user (the user's proactivity dial + guardrails).

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid (PK) | |
| `UserId` | Guid | **unique**; per-user isolation |
| `Mode` | NotificationMode (string) | default `Scan` |
| `QuietHoursStartLocal` | int? | hour 0–23 in the user's tz; null = no quiet hours |
| `QuietHoursEndLocal` | int? | hour 0–23 |
| `TimeZoneId` | string? | IANA tz for quiet-hours + digest; default from config |
| `MaxProactivePerHour` | int | rate-limit cap; default from config |
| `DigestHourLocal` | int | daily digest hour; default from config |
| `UpdatedAt` | DateTimeOffset | |

Validation: `Mode` must parse to the enum (FR-005); quiet-hours are optional; a missing row means defaults (mode `Scan`) — created lazily on first set/read.

## Entity: `CompanionEvent` (table `companion_events`)

The outbox row — one captured material event and its lifecycle.

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid (PK) | |
| `UserId` | Guid | indexed |
| `Kind` | CompanionEventKind (string) | |
| `Subject` | string | e.g. ticker or rule key (max 128) |
| `Severity` | string | mirrors source severity (`info`/`warning`/`critical`) |
| `Summary` | string | short human line for the agent (max 500) |
| `DedupKey` | string | logical identity; **unique** — collapses re-detection (max 200) |
| `ReferenceId` | Guid? | source row (alert id / thesis id / analyst action id) |
| `SourceModule` | string | `alerts` / `research` |
| `Disposition` | EventDisposition (string) | indexed; drives the relay + digest |
| `OccurredAt` | DateTimeOffset | source event time |
| `CapturedAt` | DateTimeOffset | when the capture job wrote it |
| `DispatchedAt` | DateTimeOffset? | when the wake was sent |
| `DeliveredAt` | DateTimeOffset? | when the agent acked delivery |
| `Attempts` | int | dispatch retry counter |
| `LastError` | string? | last dispatch failure reason |

Indexes: unique `DedupKey`; `(UserId, Disposition, OccurredAt)` for the relay/digest/pull queries; `(UserId, CapturedAt)`.

**State transitions** (disposition):
```
capture ──► Pending ───────────(realtime relay)──► Dispatched ──(agent ack)──► Delivered
        ├─► HeldForDigest ─────(daily digest)────► Delivered
        ├─► SuppressedByMode        (quiet — terminal)
        ├─► SuppressedByDedup       (terminal; never actually inserted — the unique key rejects it)
        ├─► SuppressedByRateLimit / DeferredQuietHours  (realtime, re-evaluated next tick)
        └─► Failed                  (retry-exhausted; visible, re-drivable)
```
Every captured event is recorded with a disposition — none lost (FR-007 / SC-005).

## Watermark

Capture progress is tracked per source so the poll reads only new rows. Stored either as a tiny `companion_capture_state` row (source → last-seen timestamp) or derived from `MAX(OccurredAt)` per source in `companion_events`. **Decision**: a small `companion_capture_state` table (`Source` PK, `Watermark` timestamp) — explicit, survives purges of `companion_events`.

## Cross-module read contracts (in `FinanceSentry.Core.Interfaces`)

- `IMaterialAlertReader.GetNewSinceAsync(watermark, ct)` → alert rows (id, userId, type, severity, title, createdAt) — implemented by **Alerts**.
- `IThesisBreakReader.GetNewBreaksSinceAsync(watermark, ct)` → thesis-break rows — implemented by **Research**.
- `IAnalystActionFeedReader.GetNewSinceAsync(watermark, ct)` → analyst actions — implemented by **Research**; the capture service filters to held tickers via existing `IBrokerageHoldingsReader`.
