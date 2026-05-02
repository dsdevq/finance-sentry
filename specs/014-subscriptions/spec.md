# Feature Specification: Subscriptions Detection

**Feature Branch**: `014-subscriptions`
**Created**: 2026-05-02
**Status**: Draft
**Input**: User description: "Most complex. Requires recurring-transaction detection algorithm (same merchant, ~same amount, ~30-day cadence). Need to decide: run on-demand vs scheduled, how to handle edge cases (annual subscriptions, variable amounts), and confidence threshold."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Detected Subscriptions (Priority: P1)

A user opens the Subscriptions page and sees a list of recurring charges the system has detected from their transaction history, including the merchant, amount, billing cadence, and next expected charge date.

**Why this priority**: The entire feature depends on detection working and surfacing results. This is the MVP.

**Independent Test**: With ≥ 3 months of transaction history containing a known recurring charge (e.g. Netflix), the subscription appears in the list with the correct merchant name, amount, and cadence.

**Acceptance Scenarios**:

1. **Given** a user has recurring transactions from the same merchant at a consistent interval, **When** they view the Subscriptions page, **Then** the subscription is listed with merchant name, amount, cadence, and estimated next charge date.
2. **Given** a detected subscription, **When** the user views it, **Then** they see the last known charge date and how many times it has been detected.
3. **Given** no recurring patterns are detected, **When** the user views the page, **Then** an empty state message is shown.
4. **Given** insufficient transaction history (< 3 months), **When** the user views the page, **Then** a message explains that more history is needed for accurate detection.

---

### User Story 2 - Mark or Dismiss a Subscription (Priority: P2)

A user can confirm a detected subscription as active or dismiss it if the detection was incorrect.

**Why this priority**: False positives are expected; users need a way to curate their subscription list.

**Independent Test**: Dismiss a detected subscription; verify it no longer appears in the list and is not re-detected unless the user resets dismissals.

**Acceptance Scenarios**:

1. **Given** a detected subscription the user considers incorrect, **When** they dismiss it, **Then** it is removed from the list and not re-surfaced automatically.
2. **Given** a dismissed subscription, **When** the user views dismissed items, **Then** they can restore it to the active list.
3. **Given** a confirmed subscription, **When** the next expected charge date passes without a matching transaction, **Then** the subscription is flagged as potentially cancelled.

---

### User Story 3 - Subscription Cost Summary (Priority: P2)

A user sees the total estimated monthly cost of all active subscriptions and a breakdown by category.

**Why this priority**: Provides immediate financial value — users want to know their total recurring spend.

**Independent Test**: With 3 active subscriptions totalling $45/month, the summary card shows $45/month.

**Acceptance Scenarios**:

1. **Given** multiple active subscriptions with different cadences, **When** viewing the summary, **Then** all amounts are normalised to a monthly cost (annual ÷ 12, weekly × 4.33).
2. **Given** a subscription with a variable amount, **When** computing the monthly cost, **Then** the average of the last 3 detected charges is used.

---

### Edge Cases

- Annual subscriptions: detected via same merchant, ~365-day interval; normalised to monthly cost.
- Variable amounts (e.g. utility bills): if variance < 20% of average, still treated as a subscription; if > 20%, excluded from detection.
- Trial periods that become paid: a new recurring pattern is detected once consistent charges begin.
- Merchant name variations (e.g. "NETFLIX.COM" vs "Netflix"): normalised by merchant name fuzzy-matching before grouping.
- What if a recurring charge stops? → After 1.5× the expected interval passes without a charge, the subscription is flagged as potentially cancelled.
- What if a user has multiple accounts and the same subscription appears on both? → Deduplication by merchant + amount + cadence; only one entry shown.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST detect recurring transactions by identifying charges from the same merchant at a consistent interval (monthly ±5 days, annual ±14 days) with ≥ 3 occurrences.
- **FR-002**: The system MUST normalise all subscription costs to a monthly equivalent for display and summary.
- **FR-003**: The system MUST exclude variable-amount charges where variance exceeds 20% of the average charge as non-subscriptions.
- **FR-004**: The system MUST deduplicate subscriptions detected across multiple connected accounts.
- **FR-005**: The system MUST normalise merchant names before grouping to handle minor naming variations.
- **FR-006**: Users MUST be able to dismiss a detected subscription as a false positive.
- **FR-007**: Users MUST be able to restore a previously dismissed subscription.
- **FR-008**: The system MUST display the total estimated monthly recurring cost across all active subscriptions.
- **FR-009**: The system MUST flag a subscription as potentially cancelled when no matching charge is detected within 1.5× the expected interval.
- **FR-010**: Detection MUST run as a nightly scheduled job; the Subscriptions page shows the most recent detection results.
- **FR-011**: A user with fewer than 3 months of transaction history MUST see a message explaining detection requires more history.

### Key Entities *(include if feature involves data)*

- **DetectedSubscription**: Merchant name (normalised), cadence (monthly/annual), average amount, currency, last charge date, next expected date, status (active/dismissed/potentially-cancelled), confidence score.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Known subscriptions (Netflix, Spotify, etc.) are detected correctly for users with ≥ 3 months of transaction history.
- **SC-002**: False positive rate is below 10% on a representative transaction dataset.
- **SC-003**: Total monthly cost calculation is within $1 of the manually summed known subscriptions.
- **SC-004**: Detection results are refreshed nightly; the page always reflects the previous night's run.
- **SC-005**: A user can dismiss a false positive in under 5 seconds.

## Assumptions

- Detection runs nightly as a Hangfire scheduled job; on-demand re-detection is not supported in v1.
- The confidence threshold for surfacing a detected subscription is ≥ 3 occurrences at a consistent interval.
- "Consistent interval" means monthly (28–35 day range) or annual (351–379 day range).
- Variable-amount charges with > 20% variance are excluded from subscription detection.
- Merchant name normalisation uses simple string canonicalisation (lowercase, strip domain suffixes, trim noise words); fuzzy matching is a v2 improvement.
- The subscription category (Entertainment, Utilities, etc.) is derived from the existing transaction category taxonomy.
- Detection history is stored; dismissals persist across re-runs.

## Notes

- [DECISION] Scheduling: nightly Hangfire job. Rationale: detection requires full transaction history aggregation; running on-demand per page load would be too slow and expensive.
- [DECISION] Confidence threshold: ≥ 3 occurrences. Rationale: balances false-positive rate against detection sensitivity; tunable in future.
- [OUT OF SCOPE] On-demand re-detection triggered by user.
- [OUT OF SCOPE] Manual subscription entry (user adds a subscription not in their transactions).
- [OUT OF SCOPE] Subscription cancellation suggestions or price comparison.
- [DEFERRED] Fuzzy merchant name matching (beyond basic normalisation) — deferred to v2.
