# Feature Specification: In-Monolith Event Bus with Transactional Outbox

**Feature Branch**: `026-event-bus-outbox`
**Created**: 2026-07-09
**Status**: Roadmap — spec only, not yet planned or implemented
**Input**: User description: "In-monolith event bus: modules publish and consume domain events through a message broker with a transactional outbox, replacing direct cross-module calls for event-shaped interactions (e.g. alerts reacting to completed transaction syncs), with idempotent consumers and dead-letter handling"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Modules react to events instead of being called (Priority: P1)

As the developer, when one module finishes something other modules care about (e.g. a transaction sync completes), it publishes a domain event; interested modules (alerts, subscriptions, net-worth snapshots) consume that event and react — without the publishing module knowing they exist.

**Why this priority**: This inverts the coupling direction and is the entire architectural point: publishers stop orchestrating consumers, which is the precondition for extracting any module into its own service later.

**Independent Test**: Complete one end-to-end flow (sync finishes → event published → alerts module evaluates rules) with the direct call removed; verify the alert still fires and the publisher has no reference to the consumer module.

**Acceptance Scenarios**:

1. **Given** a bank sync completes for a user, **When** the sync-completed event is published, **Then** subscribed modules receive it and perform their reactions (alert evaluation, subscription detection) within seconds.
2. **Given** the publishing module's code, **When** inspected, **Then** it contains no references to consumer modules for the migrated interactions.
3. **Given** a consumer module is temporarily down or slow, **When** events are published, **Then** they are delivered once the consumer recovers — no events are lost.

---

### User Story 2 - Events are never lost or phantom (Priority: P1)

As the operator, an event is published if and only if the database change it describes was committed: no lost events when the broker is briefly unavailable, no phantom events for rolled-back transactions.

**Why this priority**: Delivery guarantees are the hard part of messaging and the main learning objective; without them the bus is a liability rather than practice.

**Independent Test**: Kill the broker, run a sync (state change commits, event is queued durably), restart the broker; verify the event is delivered exactly as if the broker had been up. Separately force a rollback and verify no event escapes.

**Acceptance Scenarios**:

1. **Given** the broker is unreachable, **When** a state change commits, **Then** the event is durably recorded with the same transaction and relayed automatically once the broker returns.
2. **Given** a database transaction rolls back, **When** the outbox is inspected, **Then** no event from that transaction exists.
3. **Given** a consumer receives the same event twice (redelivery), **When** it processes the duplicate, **Then** the outcome is identical to single delivery (idempotent consumers).

---

### User Story 3 - Failed messages are visible, not silent (Priority: P2)

As the operator, an event that repeatedly fails processing lands in a dead-letter location with its error context, is visible in observability dashboards, and can be re-driven after the bug is fixed — instead of poisoning the queue or vanishing.

**Why this priority**: Operational maturity for messaging; needed before trusting the bus with real reactions, but deliverable after the happy path works.

**Independent Test**: Deploy a consumer that throws on a specific event; publish it; verify it retries with backoff, dead-letters after the limit, appears in metrics, and can be replayed successfully after fixing the consumer.

**Acceptance Scenarios**:

1. **Given** a consumer fails on an event N times, **When** the retry limit is reached, **Then** the event moves to a dead-letter queue with error details, and a metric/log records it.
2. **Given** a dead-lettered event and a fixed consumer, **When** the operator re-drives it, **Then** it processes successfully and leaves the dead-letter queue.

### Edge Cases

- Ordering: consumers must not assume strict global order; events carry occurrence timestamps and versions so out-of-order delivery is tolerable.
- Outbox table growth: relay must mark/purge published rows (ties into the 024 retention registry).
- Duplicate relay after a relay crash (at-least-once): consumer idempotency is mandatory, not optional.
- Schema evolution: events carry a version; consumers tolerate unknown added fields.
- Broker container down for an extended period: outbox accumulates; system degrades gracefully (reactions delayed, never lost) and the backlog drains on recovery.
- Local development must run the broker in the same compose stack with zero extra setup.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Modules MUST be able to publish domain events as part of the same database transaction as the state change they describe (transactional outbox), with a relay delivering them to the broker at-least-once.
- **FR-002**: Modules MUST be able to subscribe to named event types and receive them asynchronously; subscription MUST NOT require changes to the publisher.
- **FR-003**: All consumers MUST be idempotent under redelivery (deduplication by event id or naturally idempotent operations).
- **FR-004**: Events failing processing MUST be retried with backoff and then dead-lettered with error context; dead-letter contents MUST be observable and re-drivable.
- **FR-005**: Events MUST have a defined, versioned contract (type name, schema version, occurrence time, payload) documented in the repository.
- **FR-006**: At minimum these interactions MUST be migrated to events in this feature: transaction-sync completed → alert evaluation; transaction-sync completed → subscription detection. Other cross-module calls MAY migrate in later features.
- **FR-007**: Bus health (queue depth, consumer lag, dead-letter count, relay backlog) MUST be exposed to the observability stack.
- **FR-008**: The broker MUST run as part of the existing deployment stack (dev and prod) with durable storage across restarts.

### Key Entities

- **Domain event**: immutable record of a business fact (type, version, id, occurred-at, aggregate reference, payload).
- **Outbox entry**: event awaiting relay, committed atomically with its state change; tracks published/failed status.
- **Subscription**: binding of a consumer (module handler) to an event type with its retry/dead-letter policy.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The migrated flows behave identically from the user's perspective (alerts and subscription detection still happen after each sync), with reaction latency under 10 seconds.
- **SC-002**: A 10-minute broker outage during active syncing yields zero lost and zero phantom events (reconciliation between outbox and consumer effects proves it).
- **SC-003**: Replaying any day's events into consumers causes zero duplicate side effects (idempotency verified by replay drill).
- **SC-004**: A poisoned event is quarantined within its retry budget and never blocks other events (queue continues draining).
- **SC-005**: Publishing module source contains zero compile-time references to consumer modules for migrated interactions.

## Assumptions

- Depends on 023 (observability) for bus health visibility; can be built before 025/k8s — the broker is just another container.
- One broker instance is sufficient; clustering/HA is out of scope on a single host.
- Events are integration events between modules (coarse business facts), not fine-grained entity-change feeds.
- Synchronous request/response interactions (queries between modules) explicitly stay as direct calls — only event-shaped interactions migrate.

## Notes

- [DECISION] Practice-driven sequencing: the bus lands while everything is still one deployable, so delivery guarantees and idempotency are learned cheaply; later service extraction (027+) reuses the same events unchanged.
- [DECISION] Outbox pattern is mandatory from day one — publishing directly to the broker outside the DB transaction is the known-broken shortcut this feature exists to practice avoiding.
- [OUT OF SCOPE] Event sourcing (events as the source of truth) — state stays in relational tables; events are notifications.
- [OUT OF SCOPE] Saga/process-manager orchestration — no multi-step distributed workflows yet.
- [DEFERRED] Migrating remaining cross-module interactions (net-worth snapshotting, radar-driven research triggers) — follow-up features once the pattern is proven.
