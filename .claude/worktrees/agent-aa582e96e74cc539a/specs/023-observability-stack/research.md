# Phase 0 Research: Observability Stack

Operator selected the full self-hosted OSS stack (2026-08-05), so remaining choices are library/config-level. FR references are to `spec.md`.

## D1 — Durable Hangfire storage (FR-010)

- **Decision**: `Hangfire.PostgreSql` against the existing PostgreSQL, isolated in a dedicated `hangfire` schema.
- **Rationale**: Reuses the running DB (no new container), persists job history + recurring-job state across restarts, mature. Its own schema keeps Hangfire tables out of the app's EF migrations. Prerequisite for job-health trends (US3) and consecutive-failure tracking (US4).
- **Alternatives**: In-memory (current — loses everything on restart); EF-based store (couples jobs to app DbContext); Redis (new container, no benefit).

## D2 — Logs → Loki (FR-003)

- **Decision**: Keep Serilog; add `Serilog.Sinks.Grafana.Loki` shipping structured logs to Loki alongside the console sink, **fire-and-forget** (shipping failure must not affect requests, FR-003).
- **Rationale**: No change to the logging API across the codebase; existing enrichers carry correlation ids. Labels (`app`, `module`, `level`) make Loki/Grafana queries cheap (US2). Batched/async so p95 stays flat (SC-005).
- **Alternatives**: OTel logs → Collector → Loki (extra container/config now); files + Promtail (more moving parts).

## D3 — EF-SQL noise suppression (FR-011)

- **Decision**: Serilog `MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Warning)` (+ related EF categories), configurable via settings without a code change (FR-011).
- **Rationale**: This is the noise that made grep-based triage slow. SQL stays available at Debug when explicitly raised.
- **Alternatives**: Disable EF logging entirely (loses debug); filter only in Loki (noise still shipped/stored).

## D4 — Metrics → Prometheus (FR-001, FR-002)

- **Decision**: OpenTelemetry .NET with ASP.NET Core + Runtime instrumentation and the Prometheus exporter at `/metrics`; a custom `Meter` records per-job outcome + duration (FR-002), labelled by job name only (bounded cardinality, FR-007).
- **Rationale**: Standard, vendor-neutral, runtime/HTTP metrics for free (FR-001). Route-template labels (not raw URLs) keep cardinality bounded (FR-007). Scrape 15–30s.
- **Alternatives**: `prometheus-net` (.NET-only, no OTel path); custom JSON metrics endpoint (reinvents Prometheus).

## D5 — Health & readiness (SC-003)

- **Decision**: `AspNetCore.HealthChecks.NpgSql` + `.Hangfire`; keep liveness `GET /api/v1/health`, add readiness `GET /api/v1/health/ready` naming per-dependency status.
- **Rationale**: Feeds the dashboard availability panel + a future uptime probe; readiness distinguishes "process up" from "DB/job-processor usable."
- **Alternatives**: Single combined endpoint (can't separate liveness from dependency failure).

## D6 — Metrics endpoint exposure (FR-006)

- **Decision**: `/metrics` reachable only to Prometheus over the compose network / Tailscale — not the public funnel. No auth token needed if network-scoped; otherwise a scrape token.
- **Rationale**: FR-006 requires non-public metrics; same-host scrape keeps it internal.
- **Alternatives**: Public `/metrics` (leaks internal shape); mTLS (overkill single-host).

## D7 — Consecutive-failure alerting (FR-009 / US4)

- **Decision**: App-side Hangfire `IElectStateFilter` intercepts a job's transition to terminal `Failed`; maintains a per-job **consecutive-failure counter** (persisted with the Hangfire store or a small keyed record); on the **Nth** consecutive failure raise one `Alert{Type=JobFailure}` via `IAlertGeneratorService`; a subsequent `Succeeded` transition resets the counter (clears the streak, FR-009). Delivery is fire-and-forget (FR-009 / US4-AS3). Transient/self-healing errors (reuse the sync layer's transient set) don't count toward the streak.
- **Rationale**: Precise, operator-language, deduped ("one per streak, clears on success" — exactly the spec's ask), and reuses the Alerts→Companion→Telegram path proven by `ConsentExpiring` (#345). N configurable (default e.g. 3).
- **Alternatives**: Grafana-only alerting (no per-job streak semantics, no operator-language summary — deferred to a later slice per spec Notes); alert on every failure (spam).

## D8 — Alert transport reuse (FR-009)

- **Decision**: `CompanionEventKind.OperationalFailure` + map `"JobFailure"` in `MaterialityPolicy.ClassifyAlert`; operational alerts carry higher criticality so they surface even under quieter modes.
- **Rationale**: Mirrors the `ConsentExpiring` wiring — lowest-risk, consistent.
- **Alternatives**: New standalone Telegram notifier (duplicates infra).

## D9 — Dashboards-as-code (FR-004, assumption)

- **Decision**: Grafana provisioning files in `docker/observability/grafana/provisioning/` define datasources (Prometheus, Loki) and dashboards (API traffic/latency/errors; per-job health; log search). Grafana admin auth; Tailscale-only.
- **Rationale**: Survives container recreation (spec assumption); one page answers "healthy now + did syncs run?" in < 30s (SC-001).
- **Alternatives**: Hand-built dashboards (lost on recreate).

## D10 — Retention / resource sizing (FR-005, SC-004)

- **Decision**: Prometheus retention ~30d (FR-005), Loki ~14d, Hangfire succeeded-job history ~7d (failed longer); all on bounded docker volumes with hard caps.
- **Rationale**: Meets the ≥30d metrics / ≥14d logs floor while capping disk so observability can't take down the product it observes (SC-004, edge case).
- **Alternatives**: Unbounded (fills disk); shorter (misses slow-burn incidents like the 13-day silent failure).

## D11 — Dashboard/job-history durability interplay with the reaper

- **Decision**: Reuse the already-shipped **stale-sync reaper** (#344) so a job interrupted by a restart is `Failed`, not stuck `Running`, keeping job-health metrics/trends (US3) truthful.
- **Rationale**: The reaper already exists; Hangfire→Postgres + metrics simply read accurate terminal states.
