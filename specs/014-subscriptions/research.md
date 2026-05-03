# Research: Subscriptions Detection (014)

## Decision 1 — Backend module placement

**Decision**: New standalone `FinanceSentry.Modules.Subscriptions` project with its own `SubscriptionsDbContext` and migrations. The `SubscriptionDetectionJob` (Hangfire) lives in `FinanceSentry.Modules.BankSync` because it needs direct access to `ITransactionRepository`.

**Rationale**: `DetectedSubscription` results are user-facing data that belong in their own module. The detection algorithm is transaction-data-intensive — placing the job in BankSync avoids a cross-context query and follows the same pattern as `UnusualSpendDetectionJob` planned in 012-alerts.

**Alternatives considered**:
- Detection job in Subscriptions module + cross-module transaction query via MediatR — rejected: `GetAllTransactionsQuery` is paginated; detection needs all transactions in one batch without pagination overhead. A separate `ITransactionDataProvider` interface would add unnecessary indirection.
- Everything in BankSync — rejected: `DetectedSubscription` data would then be mixed into the bank sync domain.

---

## Decision 2 — Cross-module result persistence

**Decision**: Define `ISubscriptionDetectionResultService` in `FinanceSentry.Core`. The `SubscriptionDetectionJob` (in BankSync) calls this after each user's detection run. The Subscriptions module registers its concrete implementation that upserts `DetectedSubscription` records.

**Rationale**: Consistent with the `IAlertGeneratorService` pattern from 012-alerts. BankSync depends on Core (already does); no new inter-module dependencies.

**Alternatives considered**:
- Direct upsert via shared repository — rejected: would create a direct Subscriptions → BankSync dependency.

---

## Decision 3 — Detection algorithm approach

**Decision**: Per-user batch detection. Algorithm steps:
1. Fetch all non-pending debit transactions for the user for the last 13 months (via `ITransactionRepository.GetByUserIdAsync`)
2. Group by `NormalizeMerchantName(t.MerchantName ?? t.Description)` (lowercase, strip domain suffixes `.com`/`.net`, strip "PAYPAL*" prefix noise, trim leading asterisks and spaces)
3. For each group with ≥ 3 transactions sorted by date:
   - Calculate intervals between consecutive transactions (days)
   - Check if median interval is in monthly range [28, 35] or annual range [351, 379]
   - Calculate coefficient of variation (stddev / mean) of amounts; if > 0.20 → skip (FR-003)
   - Assign cadence, compute next expected date, compute confidence score
4. Deduplicate across accounts within the same user (same normalised merchant + cadence → single entry)
5. Call `ISubscriptionDetectionResultService.UpsertAsync` with results
6. Mark any existing `DetectedSubscription` where `now > LastChargeDate + 1.5 × averageInterval` as `potentially_cancelled` (FR-009)

**Rationale**: Simple interval analysis balances accuracy against complexity. v1 uses string normalisation + interval stats; fuzzy matching is deferred per spec.

---

## Decision 4 — Merchant name normalisation

**Decision**: Implement `MerchantNameNormalizer.Normalize(string?)` as a static utility in `FinanceSentry.Modules.BankSync` (used in the detection job). Steps:
1. Lowercase entire string
2. Strip protocol/domain suffixes: `.com`, `.net`, `.io`, `.co`, etc.
3. Strip `PAYPAL*` prefix
4. Strip leading `*`, `#`, and numeric suffixes
5. Trim whitespace and collapse internal spaces

Display name (`MerchantNameDisplay`) is the most common raw name across all occurrences (by frequency).

**Rationale**: Spec explicitly calls for simple string canonicalisation; fuzzy matching is deferred to v2.

---

## Decision 5 — Scheduling

**Decision**: Single recurring Hangfire job `subscription-detection` runs nightly (`Cron.Daily()`) and iterates over all active users.

**Rationale**: FR-010 specifies nightly. A single global job iterating all users is simpler than per-user jobs. At personal finance scale, a single user's detection completes in milliseconds.

---

## Decision 6 — `DetectedSubscription` upsert strategy

**Decision**: On each nightly run, the job calls `UpsertDetectedSubscriptionsAsync(userId, results)`. The service:
- Inserts new entries where `(UserId, MerchantNameNormalized)` doesn't exist
- Updates existing entries (amounts, dates, occurrence count, status) **unless** the entry is `dismissed` — dismissed entries survive re-runs unchanged (FR-006)
- Sets `potentially_cancelled` status on stale entries (no charge for 1.5× interval) regardless of detection results

**Rationale**: Dismissals persist across runs. The unique key `(UserId, MerchantNameNormalized)` ensures exactly one entry per merchant per user.

---

## Decision 7 — Frontend model alignment

**Decision**: Update the frontend `Subscription` interface to match backend reality. Map backend `status: 'active'|'dismissed'|'potentially_cancelled'` to a frontend display status. The existing `status: 'active'|'paused'` must be replaced. The frontend `logo` and `color` fields remain UI-only (derived from merchant name initial + color hash — no backend field).

**Rationale**: The spec uses dismiss/restore, not pause/resume. The frontend scaffold was built with a placeholder model that pre-dates the spec.

---

## Decision 8 — Confidence score

**Decision**: `ConfidenceScore` is stored as an `int` representing the occurrence count (FR-001 threshold = 3). Display as "detected N times". No complex probability calculation in v1.

**Rationale**: The spec defines confidence purely in terms of occurrence count. A numeric score column gives flexibility to refine in future.

---

## Resolved Unknowns

| Unknown | Resolution |
|---|---|
| On-demand vs scheduled detection? | Nightly Hangfire job (Decision 5) |
| Where does detection job live? | BankSync module (direct transaction access) (Decision 1) |
| How to store results in Subscriptions module? | `ISubscriptionDetectionResultService` Core interface (Decision 2) |
| How to handle annual subscriptions? | 351–379 day interval range (Decision 3) |
| Variable-amount exclusion? | CV > 0.20 threshold (Decision 3) |
| Merchant name normalisation strategy? | String canonicalization in `MerchantNameNormalizer` (Decision 4) |
| Dedup across accounts? | By normalised merchant + cadence key within same user (Decision 3) |
| Dismissals surviving re-runs? | Upsert skips `dismissed` status override (Decision 6) |
