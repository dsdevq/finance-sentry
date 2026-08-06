# Feature Specification: Extract Market Data Service

**Feature Branch**: `028-extract-market-data-service`
**Created**: 2026-07-09
**Status**: Roadmap — spec only, not yet planned or implemented
**Input**: User description: "Extract market data ingestion (radar daily bars and signals) into a standalone service: own deployable, own database schema, consumes and publishes domain events over the message bus, synchronous queries stay available to the monolith, deployed behind the gateway on the cluster"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Market data runs as its own service, invisibly (Priority: P1)

As the operator, the market-data capability (daily bar ingestion, radar signal computation, universe management) runs as a separate deployable with its own data store, independently restartable and deployable — while every consumer of market data (research module, MCP tools, dashboards) works exactly as before.

**Why this priority**: The extraction itself is the deliverable and the practice goal; "no consumer notices" is the definition of doing it right.

**Independent Test**: Stop the monolith; verify the market-data service still ingests bars on schedule. Stop the market-data service; verify the monolith serves everything except fresh market data, degrading gracefully.

**Acceptance Scenarios**:

1. **Given** the extracted service is live, **When** the scheduled ingestion window passes, **Then** new daily bars and signals exist, produced entirely by the service.
2. **Given** the monolith needs market data (thesis monitor, opportunity scanner, MCP tools), **When** those features run, **Then** results are identical to pre-extraction behavior.
3. **Given** the market-data service is down, **When** users use the app, **Then** all non-market-data features work normally and market-data-dependent features fail soft with clear staleness/unavailability signals, not errors that break pages.
4. **Given** the service is deployed independently, **When** a new service version rolls out, **Then** the monolith requires no redeploy.

---

### User Story 2 - Communication via events, queries via the sync channel (Priority: P1)

As the developer, the service publishes "bars ingested" / "signals computed" events to the bus that the monolith's modules consume (thesis monitor triggers, scanner scoring), and exposes a synchronous query interface for on-demand reads — no shared database access in either direction.

**Why this priority**: The interaction contract is the microservice lesson: async for facts, sync for queries, no back-door coupling through the database.

**Independent Test**: Verify by inspection and by runtime: the monolith has no database connection to the service's schema and vice versa; kill the bus and verify queries still work while event-driven reactions queue up.

**Acceptance Scenarios**:

1. **Given** ingestion completes, **When** the completion event is published, **Then** monolith consumers (e.g. thesis monitor) react without polling.
2. **Given** an on-demand read (current price context for a thesis), **When** the monolith queries the service synchronously, **Then** it gets a versioned, contract-typed response within its timeout.
3. **Given** static analysis of both codebases, **When** dependencies are checked, **Then** neither deployable references the other's database schema or internal types — only the published contracts.

---

### User Story 3 - Own data, migrated cleanly (Priority: P2)

As the operator, the radar data (daily bars, signals, universe members) is owned by the service — schema, migrations, retention policies — and the historical data was migrated without loss.

**Why this priority**: Data ownership completes the extraction; a service reading the monolith's tables is a distributed monolith.

**Independent Test**: Compare row counts and sampled content between pre-migration radar tables and the service's store; verify the monolith's connection can no longer read them.

**Acceptance Scenarios**:

1. **Given** the migration ran, **When** historical bars/signals are compared source-vs-target, **Then** they match completely.
2. **Given** the service owns retention, **When** its retention jobs run, **Then** its data follows the 024 policy registry independently of the monolith's jobs.

### Edge Cases

- Bus outage: events queue via outbox on both sides; no data loss, delayed reactions only.
- Query timeout/service degradation: monolith callers must have timeouts and degrade features to "stale data as of T" rather than hard failure.
- Contract evolution: service API and event schemas are versioned; the monolith tolerates additive changes.
- Duplicate ingestion after redelivery: ingestion and signal computation idempotent per (symbol, date).
- Deployment ordering: contract changes deploy consumer-compatible first (expand/contract), since the two deployables no longer release atomically.
- Partial extraction trap: if any monolith code path still writes radar tables, ingestion would fork — the migration must remove all write paths from the monolith.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Market-data capability (bar ingestion, signal computation, universe management, its scheduled jobs) MUST run as a standalone deployable with its own lifecycle.
- **FR-002**: The service MUST own its data exclusively: dedicated schema/store, own migrations, own retention; no other deployable reads or writes it directly.
- **FR-003**: The service MUST publish domain events for ingestion/computation milestones via the established bus (026 semantics: outbox, at-least-once, idempotent consumers).
- **FR-004**: The service MUST expose a versioned synchronous query contract for on-demand reads, routed through the gateway's internal routing, with defined timeouts.
- **FR-005**: All existing market-data consumers in the monolith (research features, MCP tools) MUST work unchanged in behavior, now via events/queries instead of in-process calls.
- **FR-006**: Historical radar data MUST be migrated to the service's store with verified completeness before the monolith's copies are retired.
- **FR-007**: The service MUST integrate with observability (023): its own metrics, logs, and job health appear in the same dashboards.
- **FR-008**: Market-data unavailability MUST degrade features softly (stale-data indicators) — no cascading failures in the monolith.

### Key Entities

- **Market-data service**: the new deployable owning bars, signals, universe.
- **Ingestion event**: published fact that a symbol set's bars/signals for a date are available.
- **Query contract**: versioned request/response shapes for synchronous reads by the monolith.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All market-data-dependent features produce identical results pre- and post-extraction (golden-path comparison on same inputs).
- **SC-002**: The service deploys independently: 5 consecutive service-only deploys with zero monolith redeploys and zero user-visible disruption.
- **SC-003**: With the service down for 1 hour, zero non-market-data user flows are affected.
- **SC-004**: Historical data migration is 100% complete by row-count and sampled-content comparison.
- **SC-005**: No cross-deployable database access exists (verified by connection audit and static dependency check).

## Assumptions

- Hard dependencies: 026 (event bus) and 025 (gateway); 027 (cluster) is the intended runtime, though the service could run as a compose container if extraction precedes cluster migration.
- The radar/market-structure module (018) is the extraction candidate because it is naturally async, low-coupling, and its consumers tolerate staleness.
- The service reuses the shared platform conventions (CQRS seams, module structure) so code moves rather than being rewritten.
- Single instance of the service is sufficient; its jobs are singleton-scheduled.

## Notes

- [DECISION] First extraction is deliberately the lowest-risk module: async workload, no user-facing UI of its own, tolerant consumers. The lesson generalizes; the risk doesn't.
- [DECISION] Sync queries go through a versioned HTTP contract initially; converting one internal call to a binary RPC contract is deferred to 029 as its own increment.
- [OUT OF SCOPE] Extracting any other module; shared authentication service; distributed tracing (though extraction is the natural trigger to add it later).
- [DEFERRED] Service-level autoscaling and replicas — singleton is correct for scheduled ingestion.
