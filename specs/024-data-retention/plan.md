# Implementation Plan: Data Retention & Backups

**Branch**: `024-data-retention` | **Date**: 2026-08-06 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/024-data-retention/spec.md`

## Summary

Bound the production database's growth and make it recoverable. Ship two P1 stories
together: (1) a single versioned **retention policy registry** in the repo giving every
table an explicit decision (purge/downsample/keep), enforced by a generic batched,
idempotent purge job that records what it did; (2) automated nightly **off-host encrypted
backups to Cloudflare R2** with a weekly automated **restore drill** into an isolated
scratch database. Downsampling (US3) is P2. Everything runs as in-app Hangfire jobs so it
inherits the 023 observability rails (metrics + consecutive-failure → Telegram) for free.
Backend-only; no frontend, no new REST endpoints.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (backend only)
**Primary Dependencies**: ASP.NET Core, EF Core 10 (Npgsql), Hangfire (PostgreSql storage,
`hangfire` schema), Serilog, OpenTelemetry (023), `AWSSDK.S3` (**new** — R2 is S3-compatible),
`FinanceSentry.Core.Cqrs` (hand-rolled — no MediatR). CLI tooling added to the API image:
`postgresql-client-14` (`pg_dump`/`pg_restore`/`createdb`/`dropdb`) + `age`.
**Storage**: PostgreSQL 14 — **new `RetentionDbContext`** (schema `retention`, history
`__ef_migrations_history_retention`), migration `M001_InitialSchema` adding `retention_runs`
+ `backup_runs`. No changes to existing module schemas. Backup artifacts live off-host in
Cloudflare R2.
**Testing**: xUnit; backup/restore round-trip tested against a local Postgres scratch DB +
local temp dir (no R2 needed in CI).
**Target Platform**: Linux (Docker Compose on the single VPS)
**Project Type**: Modular monolith — new backend module
**Performance Goals**: purge runs inside its scheduled window with p95 API latency
regression < 5% (SC-004) via bounded batches; first large backlog purge stays batched.
**Constraints**: cutoffs computed in UTC; deletions batched to bound lock time; restore
verification MUST NOT touch production tables.
**Scale/Scope**: single-DB, single-host, single-user. New module + one migration + Docker
image tooling + R2 secrets + one Grafana dashboard.

## Constitution Check

*GATE: must pass before Phase 0 and re-checked after Phase 1.*

| Principle | Status | Notes |
|---|---|---|
| I. Modular Monolith / domain interfaces | ✅ PASS | Self-contained `FinanceSentry.Modules.Retention`; R2 accessed via an `IBackupStore` domain interface with an `S3BackupStore` (R2) implementation in Infrastructure — no module references AWSSDK directly. |
| II. Code Quality (zero-warning build, ESLint) | ✅ PASS | Backend-only; `dotnet build` zero-warning gate applies. No frontend files → ESLint gate N/A. |
| III. Multi-Source Integration resilience | ✅ PASS | Backup/R2 failures are graceful (job records `Failed`, alerts fire); keyless config no-ops. |
| IV. AI-Driven Analytics | ➖ N/A | No analytics surface. |
| V. Security-First | ✅ PASS | Dumps age-encrypted **before** leaving the host; R2 creds + age keys in `.env.sops` (never logged); restore drill is read-only against an isolated DB; user data isolation unaffected (ops-level feature). |
| VI. Frontend State & Composition | ➖ N/A | No frontend changes. |
| Testing Discipline (contract + unit) | ✅ PASS | Registry coverage/whitelist guards, purge idempotency, backup round-trip, R2 prune window — all tested. No new REST endpoint ⇒ no REST contract test required; internal contracts enumerated in `contracts/internal-contracts.md`. |
| Versioning & Tagging | ✅ PASS | Backend `.csproj` version bump in the delivery PR (no client-facing API contract change → PATCH/MINOR per new-module judgment). |

**No violations — Complexity Tracking not required.**

## Project Structure

### Documentation (this feature)

```text
specs/024-data-retention/
├── plan.md              # this file
├── research.md          # Phase 0 — D1–D9 decisions (DONE)
├── data-model.md        # Phase 1 — registry types + retention_runs/backup_runs (DONE)
├── quickstart.md        # Phase 1 — run/verify guide (DONE)
├── contracts/
│   └── internal-contracts.md   # Phase 1 — jobs, registry, metrics, purge/backup contracts (DONE)
└── tasks.md             # Phase 2 — created by /speckit.tasks (NOT in this command)
```

### Source Code (repository root)

```text
backend/src/FinanceSentry.Modules.Retention/          # NEW module
├── Domain/
│   ├── RetentionPolicy.cs            # record + RetentionAction/RetentionEnforcer enums
│   ├── RetentionRun.cs               # entity
│   ├── BackupRun.cs                  # entity
│   └── IBackupStore.cs              # domain interface (put/get/list/delete artifacts)
├── Application/
│   ├── RetentionPolicyRegistry.cs    # the versioned single-source-of-truth list
│   ├── RetentionOptions.cs / BackupOptions.cs
│   ├── Services/
│   │   ├── RetentionPurgeService.cs  # batched idempotent SQL purge + run recording
│   │   ├── DownsampleService.cs      # P2, IDownsampler-driven
│   │   └── RestoreVerifier.cs        # scratch-DB restore + read-only checks
│   └── Downsamplers/                 # P2: DailyBarsDownsampler, NetWorthDownsampler
├── Infrastructure/
│   ├── Persistence/RetentionDbContext.cs (+ RetentionDbContextFactory, Migrations/M001_InitialSchema)
│   ├── Backup/S3BackupStore.cs       # AWSSDK.S3 → Cloudflare R2 (impl of IBackupStore)
│   ├── Backup/PgDumpRunner.cs        # shells pg_dump/pg_restore/createdb/dropdb + age
│   └── Jobs/
│       ├── RetentionPurgeJob.cs      # id: retention-purge   (Cron.Daily(3))
│       ├── DownsampleJob.cs          # id: retention-downsample (P2, gated)
│       ├── BackupJob.cs              # id: db-backup          (Cron.Daily(2))
│       └── RestoreVerifyJob.cs       # id: db-restore-verify  (Cron.Weekly())
├── RetentionModule.cs               # IModuleRegistrar (DI + DbContext + MigrationsHistory) + IJobRegistrar
└── FinanceSentry.Modules.Retention.csproj

backend/src/FinanceSentry.Infrastructure/Observability/
└── JobMetrics.cs                    # +2 observable gauges (backup-verified-age, retention-last-run-age)

backend/tests/FinanceSentry.Modules.Retention.Tests/   # NEW
├── RetentionPolicyRegistryTests.cs  # coverage guard + keep-forever whitelist + well-formedness
├── RetentionPurgeServiceTests.cs    # batching, idempotency, dry-run, straddle-cutoff
├── BackupRoundTripTests.cs          # dump→encrypt→restore into scratch DB, prune window
└── RestoreIsolationTests.cs         # never opens a write connection to app DB

docker/
├── Dockerfile                       # + postgresql-client-14 + age in the API image
├── .env.sops / .env.example         # + BACKUP_R2_* / BACKUP_AGE_* keys
└── observability/grafana/dashboards/retention-backups.json   # NEW dashboard-as-code
```

**Structure Decision**: A dedicated `FinanceSentry.Modules.Retention` module — consistent
with how 023 (Hangfire schema), 031 (CompanionDbContext), and 033 (Analytics) each landed
cross-cutting concerns as self-contained modules with their own schema, `IModuleRegistrar`,
and `IJobRegistrar`. The registry and jobs are cross-module by nature but touch other
schemas only by name string (from the compiled registry) via raw batched SQL — no compile
coupling to other modules' DbContexts, honoring Principle I.

## Phased delivery (maps to spec priorities)

- **Phase A — US1 (P1) retention MVP**: `RetentionDbContext`+M001, `RetentionPolicyRegistry`
  with coverage guard, `RetentionPurgeService`/`RetentionPurgeJob` (generic tables only;
  bespoke jobs untouched), run records, FR-004 log/Hangfire caps. Independently shippable.
- **Phase B — US2 (P1) backups**: `IBackupStore`/`S3BackupStore`, `PgDumpRunner`,
  `BackupJob`+`RestoreVerifyJob`, `backup_runs`, Docker image tooling, R2 secrets, metrics
  gauges + Grafana dashboard. Ships with A (spec `[DECISION]`: purge without proven restore
  is unacceptable).
- **Phase C — US3 (P2) downsampling**: `DownsampleService` + `DailyBars`/`NetWorth`
  downsamplers + `DownsampleJob` (config-gated off by default). Follows A/B.

## Complexity Tracking

> No constitution violations — section intentionally empty.

## Phase 0 & 1 status

- **Phase 0 (research.md)**: COMPLETE — D1 registry form, D2 enforcement split, D3 batched
  purge, D4 downsample, D5 run records, D6 backup/R2/restore, D7 log caps, D8 observability,
  D9 policy windows. All clarifications resolved.
- **Phase 1 (data-model.md, contracts/, quickstart.md)**: COMPLETE.
- **Post-design Constitution re-check**: PASS (no new violations introduced by the design).

## Next command

`/speckit.tasks` to generate the dependency-ordered `tasks.md` (Phase A → B → C).
