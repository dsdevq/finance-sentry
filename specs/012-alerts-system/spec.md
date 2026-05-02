# Feature Specification: Alerts System

**Feature Branch**: `012-alerts-system`
**Created**: 2026-05-02
**Status**: Draft
**Input**: User description: "Full new feature. Triggers: sync failure, low balance threshold breach, unusual spend pattern. DB schema, dismiss/read flow, event-driven or Hangfire generation."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View and Dismiss Alerts (Priority: P1)

A user opens the Alerts page and sees notifications about their accounts — sync failures, low balance warnings, and unusual spending. They can mark individual alerts as read and dismiss them.

**Why this priority**: Core value of the feature. Without the ability to view and act on alerts, everything else is irrelevant.

**Independent Test**: Navigate to the Alerts page; alerts load and display with correct severity, timestamp, and account context. Marking one as read persists across page reload.

**Acceptance Scenarios**:

1. **Given** the user has active alerts, **When** they open the Alerts page, **Then** alerts are listed sorted by most recent first with severity, message, account name, and timestamp visible.
2. **Given** an unread alert, **When** the user marks it as read, **Then** its visual state updates immediately and remains read after page reload.
3. **Given** a read alert, **When** the user dismisses it, **Then** it is removed from the visible list permanently.
4. **Given** no alerts exist, **When** the user opens the Alerts page, **Then** an empty state message is shown.
5. **Given** multiple unread alerts, **When** the user clicks "Mark all as read", **Then** all alerts are marked read in one action.

---

### User Story 2 - Low Balance Alert Generation (Priority: P2)

The system automatically generates an alert when an account balance drops below the user's configured low-balance threshold (set in Settings → Notifications).

**Why this priority**: Directly tied to user-configurable settings already in place; most predictable and immediately actionable trigger.

**Independent Test**: Set threshold above a known account balance; verify an alert appears after next sync.

**Acceptance Scenarios**:

1. **Given** a user has a threshold of $500, **When** an account syncs with a balance of $420, **Then** a low-balance alert is created for that account.
2. **Given** a low-balance alert already exists for an account, **When** the same account syncs still below threshold, **Then** no duplicate alert is created.
3. **Given** a low-balance alert exists, **When** the account balance recovers above the threshold on next sync, **Then** the alert is auto-resolved.

---

### User Story 3 - Sync Failure Alert Generation (Priority: P2)

The system creates an alert when a provider sync fails, and auto-resolves it when the sync succeeds.

**Why this priority**: Sync failures are already tracked; surfacing them as user alerts is high-value and low-complexity.

**Independent Test**: Trigger a sync failure for any provider; verify an alert appears within the next sync cycle.

**Acceptance Scenarios**:

1. **Given** a provider sync job fails, **When** the failure is recorded, **Then** a sync-failure alert is created for that provider.
2. **Given** a sync-failure alert exists, **When** the provider syncs successfully, **Then** the alert is auto-resolved.
3. **Given** a sync failure persists across multiple attempts, **Then** only one unresolved sync-failure alert exists per provider (no duplicates).

---

### User Story 4 - Unusual Spend Alert Generation (Priority: P3)

The system detects when spending in a category significantly exceeds the user's historical average and creates an alert.

**Why this priority**: Depends on sufficient transaction history; lower confidence in detection accuracy in early stages.

**Independent Test**: With ≥ 3 months of history, a category spend 2× the monthly average triggers an alert.

**Acceptance Scenarios**:

1. **Given** a user has ≥ 3 months of transaction history in a category, **When** current-month spend exceeds 2× the 3-month average, **Then** an unusual-spend alert is created.
2. **Given** insufficient history (< 3 months), **When** evaluating spend patterns, **Then** no unusual-spend alert is generated for that category.
3. **Given** an unusual-spend alert exists for a category this month, **When** the nightly check runs again, **Then** no duplicate alert is created.

---

### Edge Cases

- What happens when a user has no accounts connected? → No alerts are generated; empty state shown.
- What if the threshold is changed while a low-balance alert exists? → Existing alert remains; re-evaluated on next sync.
- What happens if a user disconnects an account that has alerts? → All alerts for that account are deleted.
- How many alerts are retained? → Dismissed or resolved alerts older than 90 days are automatically purged.
- What if `LowBalanceAlerts` or `SyncFailureAlerts` is disabled in Settings? → Corresponding alert types are not generated for that user.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST generate a low-balance alert when an account balance drops below `LowBalanceThreshold` after a sync, if `LowBalanceAlerts` is enabled for the user.
- **FR-002**: The system MUST NOT create a duplicate unresolved alert of the same type for the same account/provider while one already exists.
- **FR-003**: The system MUST auto-resolve a low-balance alert when the account balance recovers above the threshold on the next sync.
- **FR-004**: The system MUST generate a sync-failure alert when a provider sync job fails, if `SyncFailureAlerts` is enabled for the user.
- **FR-005**: The system MUST auto-resolve a sync-failure alert when the provider syncs successfully.
- **FR-006**: The system MUST generate an unusual-spend alert when a category's current-month spend exceeds 2× its 3-month rolling average, for users with ≥ 3 months of transaction history in that category.
- **FR-007**: Users MUST be able to mark individual alerts as read.
- **FR-008**: Users MUST be able to dismiss (permanently hide) individual alerts.
- **FR-009**: Users MUST be able to mark all alerts as read in one action.
- **FR-010**: Alerts MUST display: severity (error/warning/info), human-readable message, account or category name, and timestamp.
- **FR-011**: Auto-resolved alerts MUST remain visible as resolved until explicitly dismissed by the user.
- **FR-012**: Dismissed or resolved alerts older than 90 days MUST be automatically purged.

### Key Entities *(include if feature involves data)*

- **Alert**: Belongs to a user; has type (LowBalance, SyncFailure, UnusualSpend), severity, message, optional account/category reference, read/resolved/dismissed state, and timestamps.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Alerts appear within one sync cycle of the triggering condition occurring.
- **SC-002**: Zero duplicate unresolved alerts of the same type exist for the same account at any point in time.
- **SC-003**: Read and dismiss actions persist correctly across sessions for 100% of interactions.
- **SC-004**: Unusual-spend detection fires correctly for all users with ≥ 3 months of transaction history.
- **SC-005**: The sidebar displays an accurate unread alert count that updates without a page reload.

## Assumptions

- Low-balance and sync-failure alerts are generated as a post-sync side effect within the existing Hangfire sync jobs (event-driven).
- Unusual-spend detection runs as a separate nightly Hangfire scheduled job.
- The 2× threshold for unusual spend is fixed and not user-configurable in this release.
- Unusual spend is evaluated per the existing Plaid/Monobank transaction category already stored in the Transactions table.
- Email delivery of alerts is out of scope; the `emailAlerts` preference will be addressed in a future notification delivery feature.
- The `LowBalanceThreshold`, `LowBalanceAlerts`, and `SyncFailureAlerts` fields already stored in the user profile are the authoritative source for alert preferences.

## Notes

- [DECISION] Alert generation strategy: event-driven (post-sync) for low-balance and sync-failure; nightly scheduled job for unusual-spend. Rationale: low-balance and sync-failure have natural sync hooks; unusual-spend requires full-month aggregation which is better as a batch job.
- [OUT OF SCOPE] Email/push notification delivery for alerts — deferred to a future notifications feature.
- [OUT OF SCOPE] User-configurable unusual-spend multiplier — fixed at 2× in v1.
- [DEFERRED] Budget-vs-actual alerts (spending exceeds a budget category) — will be addressed as part of the Budgets feature.
