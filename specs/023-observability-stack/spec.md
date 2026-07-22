# Feature Specification: Observability Stack

**Feature Branch**: `023-observability-stack`
**Created**: 2026-07-09
**Status**: Roadmap — spec only, not yet planned or implemented
**Input**: User description: "Observability stack: OpenTelemetry metrics in the ASP.NET Core API exposed via /metrics, Prometheus + Grafana + Loki containers on the VPS, Serilog shipping to Loki, dashboards for sync jobs, HTTP latency/errors, Hangfire job health"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See whether the system is healthy at a glance (Priority: P1)

As the operator of Finance Sentry, I open a single dashboard and immediately see whether the API is up, how fast it is responding, what its error rate is, and whether the background sync jobs (bank, crypto, brokerage, market data) succeeded on their last run — without SSH-ing into the server or reading raw container logs.

**Why this priority**: This is the core value of observability — everything else (log search, alert thresholds) builds on having metrics collected and visualized. Today the only diagnosis path is `docker logs` over SSH.

**Independent Test**: Deploy the metrics endpoint and one dashboard; verify API request rate, latency, and error-rate panels populate with live traffic, and that stopping the API turns the health panel red.

**Acceptance Scenarios**:

1. **Given** the full stack is running in production, **When** the operator opens the main dashboard, **Then** it shows API availability, request rate, p50/p95/p99 latency, and 4xx/5xx error rates for at least the last 24 hours.
2. **Given** a background sync job ran, **When** the operator views the jobs panel, **Then** the job's last run time, duration, and success/failure status are visible per provider.
3. **Given** the API process stops, **When** the operator views the dashboard within a minute, **Then** the availability panel clearly shows the outage.

---

### User Story 2 - Search logs without SSH (Priority: P2)

As the operator, I can search structured application logs (by level, request path, provider, correlation id, or free text) from the same dashboard UI, over at least the last 14 days, instead of grepping container stdout.

**Why this priority**: Log search is the second-most-used diagnostic tool after metrics; it depends on the same collection infrastructure but is independently deliverable.

**Independent Test**: Ship logs to the aggregation store; search for a known error message emitted by a test request and find it with its structured fields intact.

**Acceptance Scenarios**:

1. **Given** the API logs an error, **When** the operator searches logs filtered by level=Error within the dashboard UI, **Then** the entry appears with its structured properties (timestamp, source module, message, exception).
2. **Given** 14 days have passed, **When** the operator queries day-1 logs, **Then** they are still available; older logs may be purged.

---

### User Story 3 - Background job health with history (Priority: P3)

As the operator, I can see failure trends of scheduled jobs over time (which jobs fail repeatedly, how long they take, whether durations are degrading), so recurring problems are visible before they become data gaps.

**Why this priority**: Valuable for trend analysis but the instantaneous view (Story 1) covers the urgent need.

**Independent Test**: Force a job failure twice; the trend panel shows two failures for that job with timestamps.

**Acceptance Scenarios**:

1. **Given** a job has failed 3 times this week, **When** the operator opens the job-health dashboard, **Then** the failure count and duration trend per job over the selected period is shown.

### Edge Cases

- Metrics/log infrastructure itself goes down: application must keep serving user traffic unaffected (fire-and-forget shipping, no hard dependency).
- Disk pressure on the single VPS: metrics and log stores must have bounded retention so observability cannot fill the disk and take down the product it observes.
- Metrics endpoint must not leak sensitive data (no account numbers, tokens, or user emails in labels) and must not be publicly reachable without authorization.
- High-cardinality labels (e.g. per-user or per-account) must be avoided so the metrics store does not blow up memory.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The API MUST expose runtime metrics covering: HTTP request count/latency/status per route, active requests, and process health (CPU, memory, GC).
- **FR-002**: The system MUST record per-job metrics for every scheduled background job: last run timestamp, duration, and outcome (success/failure), labelled by job name.
- **FR-003**: Application logs MUST be shipped to a central queryable log store with structured fields preserved; shipping failures MUST NOT affect request handling.
- **FR-004**: A dashboard UI MUST be available to the operator showing (a) API traffic/latency/errors, (b) job health, (c) log search — protected by authentication.
- **FR-005**: Metrics MUST be retained for at least 30 days and logs for at least 14 days; both stores MUST have hard retention caps so total disk usage is bounded.
- **FR-006**: The metrics endpoint MUST NOT be reachable from the public internet without authorization (internal network / scrape-only access).
- **FR-007**: Metric labels MUST NOT contain personal or financial data; label cardinality MUST be bounded by design (route templates, not raw URLs).
- **FR-008**: The observability stack MUST run on the existing single production host alongside the app and MUST be part of the same deployment process.

### Key Entities

- **Metric series**: named measurement with bounded labels (route, method, status class, job name) and timestamped values.
- **Log entry**: structured record with timestamp, level, source module, message template, properties, exception details.
- **Dashboard**: saved visualization definition combining metric queries and log queries; provisioned as code, not hand-built.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The operator can determine "is the system healthy right now, and did last night's syncs run?" in under 30 seconds from opening one page.
- **SC-002**: A deliberately injected API error can be found in log search within 2 minutes of it occurring, without shell access to the server.
- **SC-003**: An API outage is visible on the dashboard within 60 seconds of occurring.
- **SC-004**: Observability storage never exceeds its configured disk budget (verified after 30 days of continuous operation).
- **SC-005**: Enabling observability adds no user-perceivable latency to API requests (p95 regression < 5%).

## Assumptions

- Single-host production deployment (current VPS, docker-compose) is the target; multi-node scraping is out of scope until the orchestration feature lands.
- The operator is the sole user; a single admin login for the dashboard UI is sufficient.
- Dashboards are provisioned from files in the repo so they survive container recreation.
- Alerting (notifications on threshold breach) is deferred — this feature is about *seeing*; alert routing can build on it later.

## Notes

- [DECISION] Placement: observability components deploy on the same host as the app via the existing compose/deploy pipeline — no separate infrastructure host.
- [OUT OF SCOPE] Alert notifications (push/Telegram/email on threshold breach) — deferred to a future feature once baselines are known.
- [OUT OF SCOPE] Frontend (browser) telemetry and distributed tracing — metrics + logs first; tracing becomes relevant when services split.
- [DEFERRED] Uptime probing from outside the host (external synthetic checks).
