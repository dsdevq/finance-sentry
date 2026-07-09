# Feature Specification: Contract-First Binary RPC for Internal Queries

**Feature Branch**: `029-grpc-internal-contract`
**Created**: 2026-07-09
**Status**: Draft
**Input**: User description: "Convert one internal synchronous service-to-service call (monolith to market-data service query) to a contract-first binary RPC protocol, coexisting with the HTTP contract, to practice schema-first internal APIs, deadlines, and generated typed clients"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - One internal call goes contract-first RPC (Priority: P1)

As the developer, the highest-traffic synchronous query between the monolith and the market-data service is served over a binary RPC protocol defined by a schema file in the repo, with client and server code generated from that schema — while behavior stays byte-for-byte equivalent for the feature that uses it.

**Why this priority**: The single migrated call *is* the feature; the practice value is the full loop: schema → generated types → deadline handling → deployment.

**Independent Test**: Run the consuming feature (e.g. thesis monitor price check) against the RPC path and the HTTP path with identical inputs; results are identical.

**Acceptance Scenarios**:

1. **Given** the RPC contract is deployed on both sides, **When** the consuming feature runs, **Then** it uses the RPC channel and produces results identical to the HTTP path.
2. **Given** the schema file in the repo, **When** either side is built, **Then** its request/response types are generated from that schema — no hand-written duplication.
3. **Given** the service is slow, **When** the caller's deadline elapses, **Then** the call is cancelled promptly and the caller degrades exactly as it does for HTTP timeouts.

---

### User Story 2 - Contract evolution without lockstep deploys (Priority: P2)

As the developer, I can add a field to the RPC contract and deploy server and client at different times without breaking either direction, following an explicit compatibility rule set.

**Why this priority**: Schema evolution discipline is the second lesson of contract-first APIs; cheap to practice once the channel exists.

**Independent Test**: Add an optional field server-side, deploy server only; old client keeps working. Then deploy client; new field is consumed.

**Acceptance Scenarios**:

1. **Given** a server with an additively-evolved contract, **When** the old client calls it, **Then** the call succeeds ignoring the new field.
2. **Given** a proposed breaking change, **When** the contract check runs in CI, **Then** it is flagged before merge.

---

### User Story 3 - Both channels observable and comparable (Priority: P3)

As the operator, I can see RPC call rate, latency, and deadline-exceeded counts next to the HTTP equivalents in the dashboards, so the practice yields a real performance comparison.

**Why this priority**: Turns the exercise into measured learning; pure addition on top of Story 1.

**Independent Test**: Generate traffic on both channels; both appear in dashboards with comparable metrics.

**Acceptance Scenarios**:

1. **Given** mixed traffic, **When** the operator views the internal-calls dashboard, **Then** latency and error/deadline metrics are visible per channel.

### Edge Cases

- The gateway/ingress must route the binary protocol correctly (HTTP/2 end-to-end) — a classic operational gotcha to surface deliberately.
- Fallback: if the RPC channel is misconfigured, the caller must fail soft the same way as HTTP-path failures (no new failure modes for users).
- Deadlines vs retries: retrying a timed-out read is safe (idempotent query), but retry budget must be bounded.
- Contract files are shared between two deployables in one repo — generation must run identically in both builds and in CI.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Exactly one existing internal synchronous query MUST be migrated to a contract-first binary RPC channel; all other calls stay on their current contracts.
- **FR-002**: The contract MUST be a versioned schema file in the repository from which both caller and callee types are generated at build time.
- **FR-003**: Every RPC call MUST carry an explicit deadline; deadline expiry MUST cancel work and surface as the same degraded behavior as an HTTP timeout.
- **FR-004**: The HTTP query contract MUST remain functional during and after migration (coexistence, instant fallback by configuration).
- **FR-005**: CI MUST verify backward compatibility of contract changes (breaking-change detection blocks merge).
- **FR-006**: RPC channel metrics (rate, latency, deadline-exceeded, errors) MUST appear in the observability dashboards alongside HTTP metrics.
- **FR-007**: Routing infrastructure MUST support the binary protocol on the internal path used by the call.

### Key Entities

- **RPC contract**: versioned schema defining the query's request/response messages and service method.
- **Channel configuration**: caller-side switch selecting RPC or HTTP path with deadline/retry policy.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The consuming feature produces identical results on RPC and HTTP paths across a full day of production use.
- **SC-002**: An additive contract change deploys server-first with zero failed calls from the older client.
- **SC-003**: A deliberate breaking contract change is caught by CI before merge.
- **SC-004**: Measured p95 latency of the RPC path is documented against the HTTP path (outcome of the comparison is a learning, not a gate).
- **SC-005**: Zero user-visible behavior change throughout.

## Assumptions

- Hard dependency: 028 (market-data service) exists — before extraction there is no network call to convert.
- The migrated call is chosen at plan time as the highest-frequency monolith→service query.
- Internal-only: the RPC surface is never exposed publicly through the gateway's external routes.

## Notes

- [DECISION] Smallest-delta practice increment: one call, coexisting contracts, config fallback. Success is measured learning, not a wholesale protocol migration.
- [OUT OF SCOPE] Streaming RPCs, public-facing RPC APIs, converting further calls — each would be its own increment if the comparison justifies it.
- [DEFERRED] Decision on migrating remaining internal calls — made after SC-004's measured comparison exists.
