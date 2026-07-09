# Feature Specification: Kubernetes Production Migration

**Feature Branch**: `027-k8s-migration`
**Created**: 2026-07-09
**Status**: Draft
**Input**: User description: "Kubernetes production migration: replace docker-compose production deployment with a lightweight single-node Kubernetes cluster on the VPS — declarative manifests, health probes, rolling zero-downtime deploys, secrets management, and the ability to scale API replicas"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Production runs on the orchestrator with zero user-visible change (Priority: P1)

As the operator, the full production stack (gateway, frontend, API, MCP, broker, database, observability) runs under a lightweight orchestrator on the existing host, defined entirely by declarative manifests in the repo — and users notice nothing.

**Why this priority**: The migration itself is the deliverable; everything else (rolling deploys, scaling) presupposes the workloads run and the data survived.

**Independent Test**: Run the QA golden paths (login, accounts, dashboard, transactions, connect, disconnect) against the migrated stack; all pass, existing data intact.

**Acceptance Scenarios**:

1. **Given** the migrated cluster, **When** every QA golden-path scenario is executed, **Then** all pass with pre-migration production data present.
2. **Given** the repo, **When** the operator inspects deployment definitions, **Then** the entire stack is declared in versioned manifests — nothing is hand-created on the host.
3. **Given** a workload crashes, **When** the orchestrator detects the failed health probe, **Then** it restarts the workload automatically and the event is visible in observability.
4. **Given** the host reboots, **When** it comes back up, **Then** the entire stack returns to its declared state without operator action.

---

### User Story 2 - Zero-downtime rolling deploys (Priority: P1)

As the operator, deploying a new version rolls out gradually: the new instance must pass health checks before the old one is retired, so a bad build never takes production down, and a failed rollout can be rolled back with one command.

**Why this priority**: This is the concrete operational win over compose's stop-then-start deploys and the core skill being practiced.

**Independent Test**: Deploy a healthy build during active use — no failed requests. Then deploy a build that fails its health check — rollout halts, old version keeps serving, one-command rollback.

**Acceptance Scenarios**:

1. **Given** an in-flight rolling deploy of a healthy build, **When** requests arrive continuously, **Then** none fail due to the deploy.
2. **Given** a build that fails readiness, **When** deployed, **Then** the rollout stops, traffic continues to the previous version, and the operator sees the failure.
3. **Given** a completed but regressed deploy, **When** the operator triggers rollback, **Then** the previous version is restored within 2 minutes.

---

### User Story 3 - Scale API replicas (Priority: P2)

As the operator, I can change a replica count and get N API instances behind the gateway, with traffic balanced across them — turning the load-balancing configuration from feature 025 into observable reality.

**Why this priority**: The practice payoff (real load balancing, session concerns, connection pooling under multiple instances); not needed for actual load.

**Independent Test**: Scale API to 2 replicas; verify both receive traffic (via metrics), kill one; verify no failed requests while it restarts.

**Acceptance Scenarios**:

1. **Given** API scaled to 2 replicas, **When** traffic flows, **Then** both instances serve requests and all user flows still pass (no instance-local state breaks).
2. **Given** one replica is killed, **When** requests continue, **Then** no user-visible errors occur.
3. **Given** background jobs are scheduled, **When** two API replicas run, **Then** each job executes exactly once per schedule (no duplicate syncs/alerts).

---

### User Story 4 - Secrets out of plaintext files (Priority: P2)

As the operator, credentials (database password, provider API keys, JWT secret, encryption master key) live in the orchestrator's secret store — not in plaintext env files on the host or in the repo.

**Why this priority**: Compose-era `.env` files on the host are the weakest link; migration is the natural moment to fix it.

**Independent Test**: Grep host filesystem and repo for a known secret value — absent; app still functions with secrets injected from the store.

**Acceptance Scenarios**:

1. **Given** the migrated stack, **When** the host filesystem outside the orchestrator's store is searched for secret values, **Then** none are found in plaintext.
2. **Given** a rotated secret, **When** the operator updates it in the store and restarts the workload, **Then** the new value is in effect with no code change.

### Edge Cases

- Database is stateful: its data must be migrated (or its volume adopted) with a verified backup taken immediately before cutover (024 machinery).
- Resource ceilings: the VPS is small (ARM64); workloads need memory/CPU limits so the orchestrator's own overhead plus the stack fits without OOM-killing the database.
- Scheduled jobs under multiple replicas must not double-fire (single-owner semantics required).
- Image availability: the cluster must pull images from a registry — pushing images from CI becomes a prerequisite (today's compose builds on-host).
- Cutover order and rollback: if migration fails mid-way, the compose stack must be restorable from the pre-cutover backup within an hour.
- GitHub Actions deploy pipeline must target the cluster instead of compose after cutover.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The production stack MUST run on a lightweight single-node orchestrator on the existing host, fully declared in versioned manifests.
- **FR-002**: Every workload MUST define liveness and readiness probes; the orchestrator MUST restart failed workloads automatically.
- **FR-003**: Deploys MUST be rolling with readiness gating and support one-command rollback to the previous version.
- **FR-004**: The API MUST run correctly at N ≥ 2 replicas: no instance-local state affecting correctness, and scheduled jobs executing exactly once.
- **FR-005**: All secrets MUST be stored in the orchestrator's secret mechanism; no plaintext secrets in the repo or in host files outside it.
- **FR-006**: The database MUST run with persistent storage surviving workload restarts and host reboots; cutover MUST be preceded by a verified backup.
- **FR-007**: The CI/CD pipeline MUST build and publish images to a registry and deploy to the cluster on push to main, preserving the current continuous-deploy behavior.
- **FR-008**: The observability stack (023) MUST continue to work, now also collecting orchestrator-level signals (restarts, resource usage per workload).
- **FR-009**: Local development workflow MUST remain compose-based and unaffected.

### Key Entities

- **Workload manifest**: declarative definition of a service (image, replicas, probes, resources, secrets refs) versioned in the repo.
- **Secret**: named credential injected into workloads at runtime, managed outside version control.
- **Rollout**: a versioned transition between deployed states with health gating and rollback.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All QA golden paths pass post-migration with pre-migration data; zero data loss.
- **SC-002**: 10 consecutive production deploys complete with zero failed user requests attributable to deployment.
- **SC-003**: A deliberately broken build never receives production traffic (readiness gate proves itself).
- **SC-004**: Rollback from a regressed deploy completes in under 2 minutes.
- **SC-005**: With 2 API replicas, killing one causes zero user-visible errors and no duplicated scheduled-job side effects.
- **SC-006**: Total memory overhead of the orchestrator itself stays within a budget that leaves the app and database performing as before (p95 latency regression < 10%).

## Assumptions

- Depends on 025 (gateway) — ingress concepts map onto it — and benefits from 023/024 being live (observability of the migration, backups for cutover safety). 026 (broker) is just another stateful workload to migrate.
- Single node is accepted: this practices orchestration, not high availability; multi-node is explicitly out of scope.
- A container registry is available (e.g. the Git host's registry) for CI-built images.
- The VPS has capacity for the orchestrator's overhead; if not, resizing the VPS is a prerequisite decision, not part of this feature.

## Notes

- [DECISION] Practice-driven: compose is objectively sufficient for this workload; the migration exists to make orchestration skills real on a production system with real stakes.
- [DECISION] Local dev stays on compose to keep iteration fast; dev/prod parity is accepted as "same images, different orchestrator".
- [OUT OF SCOPE] Multi-node clustering, autoscaling, service mesh.
- [DEFERRED] Canary/blue-green strategies — after rolling deploys are boring.
