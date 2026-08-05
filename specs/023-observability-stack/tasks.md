---
description: "Task list for Observability Stack (023)"
---

# Tasks: Observability Stack

**Input**: Design documents from `/specs/023-observability-stack/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/observability-contracts.md
**Tests**: Contract tests for new endpoints (MANDATORY per constitution) + unit tests for the alerting logic.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no incomplete dependencies)
- Backend paths: `backend/src/…`, `backend/tests/…`. Infra: `docker/…`.

---

## Phase 1: Setup (Shared Infrastructure)

- [X] T001 Add NuGet packages to `backend/Directory.Packages.props` + reference in `FinanceSentry.API` / `FinanceSentry.Infrastructure`: `Hangfire.PostgreSql`, `Serilog.Sinks.Grafana.Loki`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Runtime`, `OpenTelemetry.Exporter.Prometheus.AspNetCore`, `AspNetCore.HealthChecks.NpgSql`, `AspNetCore.HealthChecks.Hangfire`.
- [X] T002 [P] Create `backend/src/FinanceSentry.Infrastructure/Observability/` with stub config classes (`SerilogConfiguration`, `OpenTelemetryConfiguration`, `JobMetrics`, `Hangfire/`, `HealthChecks/`).
- [X] T003 [P] Create `docker/observability/` skeleton: `prometheus/`, `loki/`, `grafana/provisioning/{datasources,dashboards}/`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: the collection layer (durable jobs, metrics emitted, logs shipped, containers running) must exist before any story's dashboards/alerts.

- [X] T004 Replace Hangfire `UseInMemoryStorage` with `UsePostgreSqlStorage` (schema `hangfire`) in `backend/src/FinanceSentry.Modules.BankSync/Infrastructure/Jobs/HangfireSetup.cs` + `FinanceSentry.API/Program.cs`; connection reuses existing Postgres. (FR-010)
- [X] T005 [P] `SerilogConfiguration.cs`: structured enrichers (correlation id, module) + `MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Warning)`, levels driven by config. (FR-011)
- [X] T006 [P] Add fire-and-forget Serilog Loki sink (labels `app`,`module`,`level`) in `SerilogConfiguration.cs`; shipping failure must not affect requests. (FR-003)
- [X] T007 `OpenTelemetryConfiguration.cs` + `Program.cs`: OTel with ASP.NET Core + Runtime instrumentation and Prometheus exporter mapped at `/metrics`. (FR-001)
- [X] T008 [P] `JobMetrics.cs`: custom `Meter` — `finance_jobs_succeeded_total{job}`, `finance_jobs_failed_total{job}`, `finance_job_duration_seconds{job}`, `finance_jobs_scheduled` gauge; bounded labels only (FR-002, FR-007).
- [X] T009 `Observability/Hangfire/JobMetricsFilter.cs` (Hangfire `IApplyStateFilter`) records per-job outcome + duration into `JobMetrics`; register globally. (FR-002)
- [X] T010 [P] Readiness endpoint `GET /api/v1/health/ready` (Npgsql + Hangfire health checks) in `Program.cs` / `Observability/HealthChecks/`; keep existing `/api/v1/health` liveness. (SC-003)
- [X] T011 [P] Add `loki`, `prometheus`, `grafana` services to `docker/docker-compose.dev.yml` and `docker/docker-compose.prod.yml` with bounded named volumes + retention env. (FR-008)
- [X] T012 [P] `docker/observability/prometheus/prometheus.yml`: scrape the API `/metrics` (15–30s), retention ~30d. (FR-005)
- [X] T013 [P] `docker/observability/loki/loki-config.yml`: ≥14d retention, hard size cap. (FR-005)
- [X] T014 `docker/observability/grafana/provisioning/datasources/`: Prometheus + Loki datasources; Grafana admin auth via env. (FR-004)
- [X] T015 Secure surfaces: keep `/metrics` scrape-only / non-public (FR-006); add `Observability/Hangfire/DashboardAuthorizationFilter.cs` and re-enable the Hangfire dashboard behind it; Tailscale-only in prod. (FR-004)

**Checkpoint**: metrics flowing to Prometheus, logs to Loki, jobs durable in Postgres, dashboards reachable.

---

## Phase 3: User Story 1 - Health at a glance (Priority: P1) 🎯 MVP

**Goal**: One dashboard answers "is the system healthy now, and did last night's syncs run?" in < 30s (SC-001).
**Independent Test**: Open the main dashboard → API rate/latency/error panels populate; jobs panel shows per-provider last run; stopping the API turns availability red within 60s.

- [X] T016 [P] [US1] Contract test: `GET /metrics` returns 200 and exposition contains `finance_jobs_*`, in `backend/tests/FinanceSentry.Tests.Integration/Observability/MetricsEndpointTests.cs`.
- [X] T017 [P] [US1] Contract test: `GET /api/v1/health/ready` → 200 both checks Healthy; DB down → 503 naming `database`, in `backend/tests/FinanceSentry.Tests.Integration/Observability/HealthReadyTests.cs`.
- [X] T018 [US1] Grafana main dashboard as code (`docker/observability/grafana/provisioning/dashboards/api-overview.json`): request rate, p50/p95/p99 latency, 4xx/5xx error rate, ≥24h window. (FR-001, SC-001)
- [X] T019 [US1] Jobs panel on the main dashboard: per-provider last run time, duration, success/failure from `finance_jobs_*`. (FR-002)
- [X] T020 [US1] Availability panel (app `up` / health) that visibly goes red within 60s of an API outage. (SC-003)
- [X] T021 [US1] Validate US1 per `quickstart.md` (panels populate; kill API → red).

**Checkpoint**: MVP — live health/traffic/jobs visibility without SSH.

---

## Phase 4: User Story 2 - Search logs without SSH (Priority: P2)

**Goal**: Structured log search in the dashboard UI over ≥14 days, no SSH.
**Independent Test**: Emit a known error → find it in Grafana/Loki by `level=Error` with structured fields; no raw SQL noise.

- [ ] T022 [US2] Grafana Loki logs view/dashboard (`…/dashboards/logs.json`): filter by `level`, `module`, correlation id, free text. (FR-003)
- [ ] T023 [US2] Verify EF SQL suppressed at default level and structured properties present end-to-end; adjust `SerilogConfiguration` levels if noise leaks. (FR-011, SC-002)
- [ ] T024 [US2] Validate US2 per `quickstart.md` (injected error found in < 2 min).

**Checkpoint**: US1 + US2 both independently usable.

---

## Phase 5: User Story 4 - Alert on N consecutive job failures (Priority: P2)

**Goal**: One Telegram alert when a scheduled job hits N consecutive failures; clears on next success. Closes the silent-outage gap (the 636-consecutive-failure incident). Reuses the Alerts→Companion→Telegram path from `ConsentExpiring` (#345).
**Independent Test**: Force N consecutive failures → exactly one Telegram alert naming job + count; further failures → no dup; a success then failure → re-alerts; transient failure doesn't count.

- [X] T025 [P] [US4] Add `AlertType.JobFailure` (`backend/src/FinanceSentry.Modules.Alerts/Domain/AlertType.cs`) + `GenerateJobFailureAlertAsync(userId, referenceId, jobName, consecutiveCount, lastError)` in `Core/Interfaces/IAlertGeneratorService.cs` and `AlertGeneratorService.cs` with a `JobFailureSilenceWindow`.
- [X] T026 [P] [US4] Append `CompanionEventKind.OperationalFailure` (`…/Companion/Domain/CompanionEventKind.cs`) + map `"JobFailure"` in `MaterialityPolicy.ClassifyAlert`; carry elevated criticality so it surfaces under quieter modes.
- [X] T027 [US4] `Observability/Hangfire/ConsecutiveFailureAlertFilter.cs` (`IElectStateFilter`): durable per-job consecutive-failure counter, exclude transient errors (reuse the sync transient set), raise one `JobFailure` alert on the Nth, reset on `Succeeded`; fire-and-forget so dispatch failure can't break the job. (FR-009)
- [X] T028 [US4] Register the filter in Hangfire config; make N configurable (default 3) via settings.
- [X] T029 [P] [US4] Unit tests in `backend/tests/FinanceSentry.Tests.Unit/Observability/ConsecutiveFailureAlertFilterTests.cs`: Nth→one alert with count; 1..N-1→none; success resets then re-alerts; transient error no-increment; generator throw doesn't propagate.
- [X] T030 [US4] Add the new `IAlertGeneratorService` method to the hand-written fakes (`Research.Tests` `OpportunityFakes.cs`, `RunThesisMonitorHandlerTests.cs`) so the solution compiles.
- [X] T031 [US4] Validate US4 per `quickstart.md` (force N failures → single Telegram; success clears).

**Checkpoint**: silent-failure gap closed; US1/US2/US4 all independently usable.

---

## Phase 6: User Story 3 - Job health with history/trends (Priority: P3)

**Goal**: Failure-count + duration trends per job over a selectable period.
**Independent Test**: Fail a job twice → trend panel shows two failures with timestamps; survives a restart.

- [ ] T032 [US3] Grafana job-health dashboard (`…/dashboards/job-health.json`): per-job failure count + duration trend over selectable range, from `finance_jobs_*` + Hangfire. (FR-002)
- [ ] T033 [US3] Verify trends persist across an API restart (Hangfire Postgres storage, FR-010) and validate per `quickstart.md`.

**Checkpoint**: all four stories independently functional.

---

## Phase 7: Polish & Cross-Cutting

- [ ] T034 [P] Bump API version + create tag for the new endpoints (`/metrics`, `/api/v1/health/ready`) per constitution Versioning policy.
- [ ] T035 [P] Observability runbook in `README.md`: bring-up, dashboard URLs (Tailscale), tuning N, retention windows, where alerts land.
- [ ] T036 Full `dotnet build FinanceSentry.sln` zero-warning sweep + run unit + integration suites in the `sdk:10.0` container.
- [ ] T037 Deploy to VPS; verify end-to-end: dashboards reachable over Tailscale, `/metrics` scrape-only, one forced consecutive-failure alert lands in Telegram, retention volumes bounded.
- [ ] T038 [P] Update agent context (`.specify` / `CLAUDE.md` active technologies) with the observability stack.

---

## Dependencies & Execution Order

- **Setup (P1)** → **Foundational (P2, BLOCKS all stories)** → **Stories** → **Polish**.
- Story order by priority: **US1 (P1, MVP)** → **US2 (P2)** → **US4 (P2)** → **US3 (P3)**. US4 is P2 and addresses the urgent silent-failure incident — it may be pulled ahead of US2/US3 if desired (it depends only on Foundational T004 + the Alerts/Companion modules, not on US1–US3).
- Within a story: contract/unit tests written to fail first, then implementation, then quickstart validation.

### Key cross-task dependencies

- T004 (Hangfire→Postgres) blocks T009, T027, T032/T033 and all durable-job behaviour.
- T007/T008 (OTel + Meter) block T009, T016, T018–T020, T032.
- T005/T006 (Serilog+Loki) block T022–T024.
- T011–T014 (containers + datasources) block all Grafana dashboards (T018–T020, T022, T032).
- T025+T026 block T027 (filter needs the alert type + companion mapping).

### Parallel opportunities

- Setup: T002, T003 parallel.
- Foundational: T005/T006, T008, T010, T011/T012/T013 largely parallel (distinct files); T004/T007/T009/T014/T015 have ordering per above.
- US4: T025 and T026 parallel; T029 parallel with dashboard-only tasks in other stories.
- Different stories (US1/US2/US4/US3) can proceed in parallel once Foundational is done.

---

## Implementation Strategy

### MVP (US1 only)
Setup → Foundational → US1 → **stop & validate** (dashboard shows health + jobs; outage turns red). Deploy.

### Recommended next slice
**US4** immediately after the MVP (or even before US2/US3) — it directly closes the "silent multi-day failure" gap that motivated this feature, and reuses the proven `ConsentExpiring` alert path.

### Incremental delivery
MVP (US1) → US4 → US2 → US3, each independently testable and deployable, none breaking the last.

---

## Notes

- No app EF migration — Hangfire owns its `hangfire` schema; alerts reuse existing `alerts`/`companion_events` (new enum/const values only).
- Reuses the shipped stale-sync reaper (#344) so restart-interrupted jobs read as `Failed`, keeping trends/streaks truthful.
- Grafana metric-threshold alert rules remain **deferred** (spec Notes) until baselines exist; US4 is the app-side minimal alerting slice.
- Commit after each task or logical group; validate each story at its checkpoint before moving on.
