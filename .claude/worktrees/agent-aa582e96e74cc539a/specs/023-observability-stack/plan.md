# Implementation Plan: Observability Stack

**Branch**: `023-observability-stack` | **Date**: 2026-08-05 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/023-observability-stack/spec.md`

## Summary

Give the single-VPS Finance Sentry monolith production-grade observability so failures announce themselves instead of being discovered days later via `ssh`+`grep` (the live-log investigation on 2026-08-05 found a news-ingestion job that had failed **636 consecutive times over ~13 days** unnoticed). Four capabilities: **US1 (P1)** a health-at-a-glance dashboard (API traffic/latency/errors + per-job success); **US2 (P2)** log search without SSH; **US3 (P3)** job-failure trends over time; **US4 (P2)** a pushed Telegram alert when a scheduled job hits N consecutive failures.

Technical approach (operator-selected full OSS stack): instrument the API with **OpenTelemetry** (Prometheus exporter at `/metrics`), run **Prometheus + Grafana + Loki** as compose services on the VPS, ship **Serilog → Loki** with EF-SQL noise suppressed (FR-011), and move **Hangfire storage to PostgreSQL** so job history survives restarts (FR-010). The minimal alerting slice (US4/FR-009) is app-side: a Hangfire failure filter counts consecutive failures per job and, on the Nth, raises a `JobFailure` alert that rides the **existing Alerts→Companion→Telegram** pipeline (the exact path just proven for `ConsentExpiring` in #345), de-duplicated until the job next succeeds. Grafana metric-threshold rules remain deferred until baselines exist.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (backend only; no Angular changes)
**Primary Dependencies**:
- Existing: ASP.NET Core, EF Core 10 (Npgsql), Hangfire, Serilog, `FinanceSentry.Core.Cqrs` (hand-rolled — no MediatR), Alerts module, Companion module (031), and the **stale-sync reaper** already shipped (#344) which prevents jobs stuck "running" after a restart.
- New NuGet: `Hangfire.PostgreSql`; `Serilog.Sinks.Grafana.Loki`; `OpenTelemetry.Extensions.Hosting` + `.Instrumentation.AspNetCore` + `.Instrumentation.Runtime` + `.Exporter.Prometheus.AspNetCore`.
- New infra containers: Loki, Prometheus, Grafana (provisioned datasources + dashboards as code).
**Storage**: PostgreSQL 14 — Hangfire moves to a dedicated `hangfire` schema in the existing DB (FR-010). No new app EF entities: consecutive-failure alerts reuse the existing `alerts` table + Companion `companion_events`. Loki/Prometheus keep bounded on-disk stores.
**Testing**: xUnit + FluentAssertions + Moq (unit); integration project for endpoint contract tests. Zero `dotnet build` warnings gate (constitution II).
**Target Platform**: Linux single VPS, docker-compose (`docker/docker-compose.prod.yml`), Tailscale serve.
**Project Type**: Web service (modular monolith), backend-only.
**Performance Goals**: SC-005 — enabling observability adds < 5% p95 latency (async sinks, batched export, 15–30s scrape). SC-003 — API outage visible within 60s.
**Constraints**: No paid SaaS (FR-008). Bounded retention: metrics ≥ 30d, logs ≥ 14d, both hard-capped (FR-005, SC-004). Metrics endpoint not publicly reachable, no PII/high-cardinality labels (FR-006/007). Dashboards behind auth (FR-004).
**Scale/Scope**: Single operator, single node; ~dozens of recurring jobs.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|---|---|---|
| I. Modular Monolith | ✅ PASS | Cross-cutting wiring in `FinanceSentry.API/Program.cs` + a new `FinanceSentry.Infrastructure/Observability/` folder; the failure-alert filter calls the existing `IAlertGeneratorService` (`Core` interface). Loki/Prometheus/Grafana are infra containers, not backend modules — monolith boundary intact. |
| II. Code Quality (NON-NEGOTIABLE) | ✅ PASS | Zero build warnings; unit + contract tests per unit. |
| III. Multi-Source Integration | N/A | No new financial source. |
| IV. AI Analytics | N/A | — |
| V. Security-First | ✅ PASS (design constraint) | Metrics scrape-only/not public (FR-006); no PII in labels (FR-007); Grafana + Hangfire dashboards auth-gated + Tailscale-only (FR-004); secrets via env. |
| VI. Frontend Discipline | N/A | Backend-only. |
| Testing Discipline | ✅ PASS | New `/metrics`, `/health/ready` ship with contract tests; the consecutive-failure counter/classifier gets unit tests. |
| Versioning & Tagging | ⚠️ ACTION | New API surface (`/metrics`, `/health/ready`) → API version bump + tag in the same PR. Track as a task. |

No violations requiring Complexity Tracking; the 3 added infra containers are an explicit operator decision, bounded by retention/resource caps.

## Project Structure

### Documentation (this feature)

```text
specs/023-observability-stack/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
backend/src/
├── FinanceSentry.API/
│   └── Program.cs                         # Wire OpenTelemetry + Prometheus /metrics, Serilog→Loki,
│                                          # health checks (/health, /health/ready), Hangfire→PostgreSql,
│                                          # dashboard authorization filter.
├── FinanceSentry.Infrastructure/
│   └── Observability/
│       ├── SerilogConfiguration.cs        # structured enrichers + EF-SQL level override (FR-011)
│       ├── OpenTelemetryConfiguration.cs  # metrics setup + custom job Meter (FR-001/002)
│       ├── JobMetrics.cs                  # counters/gauges: succeeded/failed/duration per job
│       ├── HealthChecks/                  # Npgsql + Hangfire readiness
│       └── Hangfire/
│           ├── HangfirePostgresSetup.cs   # InMemory → PostgreSql, schema "hangfire" (FR-010)
│           ├── ConsecutiveFailureAlertFilter.cs  # IElectStateFilter: count consecutive failures, alert on Nth (FR-009/US4)
│           ├── JobMetricsFilter.cs        # record per-job outcome/duration metrics (FR-002)
│           └── DashboardAuthorizationFilter.cs
├── FinanceSentry.Modules.Alerts/Domain/AlertType.cs      # + JobFailure
├── FinanceSentry.Modules.Alerts/.../AlertGeneratorService.cs  # + GenerateJobFailureAlertAsync (+ Core interface)
└── FinanceSentry.Modules.Companion/...    # + CompanionEventKind.OperationalFailure + ClassifyAlert map

backend/tests/
├── FinanceSentry.Tests.Unit/             # consecutive-failure counter/dedup; metrics recording; health logic
└── FinanceSentry.Tests.Integration/      # contract: /health/ready, /metrics, dashboard authz

docker/
├── docker-compose.prod.yml               # + loki, prometheus, grafana (bounded volumes)
├── docker-compose.dev.yml                # local parity
└── observability/
    ├── prometheus/prometheus.yml         # scrape app /metrics
    ├── loki/loki-config.yml              # ≥14d retention, capped
    └── grafana/provisioning/             # datasources + dashboards-as-code (API, jobs, logs)
```

**Structure Decision**: Web-service modular monolith. Observability wiring lives in `Program.cs` + a new infrastructure `Observability/` folder (not a domain module). Alerting extends the Alerts + Companion modules exactly as `ConsentExpiring` did, so US4 reuses a proven, low-risk path. Dashboards/scrape configs are provisioned-as-code under `docker/observability/` (FR-004 assumption: survive container recreation).

## Complexity Tracking

> No constitution violations. The one notable complexity — 3 new infra containers (Loki/Prometheus/Grafana) — is an explicit operator decision in service of the production-practice roadmap, bounded by retention/scrape caps to fit the single VPS. The minimal-ops alternative was consciously declined.
