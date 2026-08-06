# Research: Alerts System (012)

## Decision 1 — Backend module placement

**Decision**: New standalone `FinanceSentry.Modules.Alerts` project with its own `AlertsDbContext` and migrations.

**Rationale**: Alerts are a cross-cutting concern that spans all provider modules (BankSync, CryptoSync, BrokerageSync). Placing them in any single sync module would create a god-module. A dedicated module follows the existing pattern (BankSync, CryptoSync, BrokerageSync each own their data).

**Alternatives considered**:
- Add to `FinanceSentry.Modules.BankSync` — rejected: alerts cover crypto and brokerage too; would violate module isolation.
- Add to `FinanceSentry.Core` — rejected: Core is shared infrastructure, not a domain module; it should not own entities.

---

## Decision 2 — Cross-module communication (alert generation)

**Decision**: Define `IAlertGeneratorService` in `FinanceSentry.Core.Interfaces`. Sync jobs (ScheduledSyncService, BinanceSyncJob, IBKRSyncJob) inject this interface. The Alerts module registers the concrete `AlertGeneratorService` implementation.

**Rationale**: The simplest approach that respects module boundaries. All sync modules already depend on `FinanceSentry.Core`. No new inter-module project references needed. MediatR events were evaluated but add indirection without benefit at this scale.

**Alternatives considered**:
- MediatR `INotificationHandler` on `AccountSyncCompletedEvent` — rejected: would require extending all three sync module events and adding a handler per event in the Alerts module; more moving parts with no benefit here.
- Direct project reference (BankSync → Alerts) — rejected: violates module isolation.

---

## Decision 3 — Unusual spend detection placement

**Decision**: `UnusualSpendDetectionJob` lives in `FinanceSentry.Modules.BankSync` (next to the other sync jobs). It queries the existing `Transactions` table via `BankSyncDbContext` and calls `IAlertGeneratorService` to emit alerts.

**Rationale**: The job needs transaction data that is owned by BankSync. Placing it there avoids a cross-context DB query from the Alerts module. It calls `IAlertGeneratorService` (Core interface) — no new inter-module dependencies.

**Alternatives considered**:
- Define `ITransactionReadService` in Core, implement in BankSync, inject in Alerts module's job — rejected: unnecessary interface layer for a single use case.
- Access `BankSyncDbContext` directly from Alerts module — rejected: violates module data ownership.

---

## Decision 4 — Frontend AlertsStore scope

**Decision**: Make `AlertsStore` root-scoped (`{providedIn: 'root'}`) rather than page-scoped.

**Rationale**: SC-005 requires the sidebar unread count to update without a page reload. The sidebar (`AppShellComponent`) is outside the alerts route. A root-scoped store is the correct pattern (as per constitution Principle VI.1: "App-wide stores use `{providedIn: 'root'}`").

**Alternatives considered**:
- Separate lightweight `AlertsBadgeStore` at root + page-scoped `AlertsStore` — rejected: duplicates state and API calls; two sources of truth.
- Pass count via router state — rejected: doesn't work for sidebar updates independent of navigation.

---

## Decision 5 — Deduplication strategy

**Decision**: Application-level deduplication. Before inserting an alert, `AlertGeneratorService` queries for an existing unresolved alert of the same `(UserId, Type, ReferenceId)`. If found, skip insertion. Database-level: partial unique index on `(user_id, type, reference_id) WHERE is_resolved = false AND is_dismissed = false` as a safety net.

**Rationale**: FR-002 forbids duplicate unresolved alerts for the same account/type. Application-level check is explicit and testable. DB partial index is a defensive guard for race conditions.

**Alternatives considered**:
- Full unique index on `(user_id, type, reference_id)` — rejected: would prevent re-creating an alert after resolution.
- No DB constraint, application only — rejected: race condition risk under concurrent sync jobs.

---

## Decision 6 — AccountSyncCompletedEvent extension

**Decision**: Extend `AccountSyncCompletedEvent` to include `UserId` (string), `BalanceAfterSync` (decimal?), `ErrorCode` (string?), and `Provider` (string). The Alerts module's `IAlertGeneratorService` implementation uses these fields directly instead of re-querying the account.

**Rationale**: The event already carries `AccountId` and `Status`. Adding balance and userId avoids a DB round-trip in every alert check. The event is internal to the BankSync domain and the change is additive.

**Alternatives considered**:
- Keep event minimal, fetch balance in handler — rejected: requires the Alerts module to have read access to BankSync's repository, creating a hidden coupling.

---

## Decision 7 — 90-day purge job

**Decision**: Add `AlertPurgeJob` in `FinanceSentry.Modules.Alerts`, registered as a monthly Hangfire recurring job (`alert-purge`, `Cron.Monthly()`).

**Rationale**: FR-012 requires purging dismissed/resolved alerts older than 90 days. Monthly frequency is sufficient; the job deletes in bulk using a date filter.

**Alternatives considered**:
- Daily purge — rejected: low volume of alerts makes daily unnecessary overhead.
- Soft-delete + background archival — rejected: no requirement for audit trail of old alerts.

---

## Decision 8 — Alert severity mapping

| Alert Type | Severity | Rationale |
|---|---|---|
| SyncFailure | Error | Actionable; user must reconnect or investigate |
| LowBalance | Warning | Informational but time-sensitive |
| UnusualSpend | Warning | Pattern anomaly, not an error |

---

## Resolved Unknowns

| Unknown | Resolution |
|---|---|
| Does `AccountSyncCompletedEvent` include balance? | No → extend it (Decision 6) |
| Where does unusual spend job live? | BankSync module (Decision 3) |
| How does sidebar get unread count? | Root-scoped AlertsStore (Decision 4) |
| How do crypto/brokerage sync failures generate alerts? | IAlertGeneratorService injected into BinanceSyncJob / IBKRSyncJob (Decision 2) |
| Dedup mechanism | App-level check + partial unique DB index (Decision 5) |
