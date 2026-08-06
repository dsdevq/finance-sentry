---
description: "Task list for feature 024 — Data Retention & Backups"
---

# Tasks: Data Retention & Backups

**Input**: Design documents from `/specs/024-data-retention/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/internal-contracts.md, quickstart.md

**Tests**: Unit + integration tests are MANDATORY (constitution Testing Discipline). No new
REST endpoint → no REST contract test. R2 is an external service → an `IBackupStore`
integration test stands in for the external-contract requirement (runnable in CI against a
local temp dir; a live-R2 smoke test is opt-in). No E2E (not requested).

**Organization**: Grouped by user story. US1 (retention) and US2 (backups) are both P1 and
ship together (spec `[DECISION]`: no purge without proven restore). US3 (downsampling) is P2.

**Module**: all backend paths under `backend/src/FinanceSentry.Modules.Retention/` (new) and
`backend/tests/FinanceSentry.Modules.Retention.Tests/` (new) unless stated.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (Setup/Foundational/Polish carry no story label)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: New module skeleton wired into the monolith.

- [X] T001 Create `FinanceSentry.Modules.Retention.csproj` + folder tree (Domain/Application/Infrastructure) matching plan.md structure; target net10.0, reference `FinanceSentry.Core`, `FinanceSentry.Infrastructure`; add to `FinanceSentry.sln`
- [X] T002 Create test project `backend/tests/FinanceSentry.Modules.Retention.Tests/FinanceSentry.Modules.Retention.Tests.csproj` (xUnit), reference the module + `FinanceSentry.API` for DbContexts; add to `FinanceSentry.sln`; add a `ProjectReference` from `FinanceSentry.API` to the module so the assembly-scan (`FinanceSentry.Modules.*.dll`) loads it
- [X] T003 [P] Add `AWSSDK.S3` NuGet package to the module csproj (R2 S3 client)

**Checkpoint**: `dotnet build backend/` succeeds with the empty module registered.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The `retention` schema + both run-record tables + options + module registration. Blocks US1 and US2.

**⚠️ CRITICAL**: No user story work begins until this phase completes.

- [X] T004 [P] Create `RetentionRun` entity in `Domain/RetentionRun.cs` (Id, RunType, StartedAt, CompletedAt, Outcome, TableResults jsonb, Error) per data-model.md
- [X] T005 [P] Create `BackupRun` entity in `Domain/BackupRun.cs` (Id, Kind, CreatedAt, ArtifactKey, SizeBytes, Sha256, Encrypted, VerificationStatus, VerifiedAt, Error) per data-model.md
- [X] T006 Create `RetentionDbContext` in `Infrastructure/Persistence/RetentionDbContext.cs`: `HasDefaultSchema("retention")`, map both entities, `TableResults` as `jsonb`, indexes `(RunType, StartedAt desc)`, `(CreatedAt desc)`, partial `(VerifiedAt desc) WHERE VerificationStatus='Verified'`
- [X] T007 Create `Infrastructure/Persistence/RetentionDbContextFactory.cs` (design-time) with `MigrationsHistoryTable("__ef_migrations_history_retention", "retention")`
- [X] T008 Generate EF migration `M001_InitialSchema` (in-container `dotnet ef`, see 036 note in CLAUDE.md) creating `retention.retention_runs` + `retention.backup_runs` + indexes; verify Designer file + attributes present (M007 hand-written-migration lesson)
- [X] T009 [P] Create `Application/RetentionOptions.cs` (PurgeHourUtc=3, WindowOverrides map, DefaultBatchSize=5000, Downsample:Enabled=false) bound from `Retention:`
- [X] T010 [P] Create `Application/BackupOptions.cs` (BackupHourUtc=2, R2 endpoint/bucket/keys, AgeRecipient, AgeIdentity, RetainDaily=30, RetainWeekly=8, RestoreVerifyDay) bound from `Backup:` + `BACKUP_*` env
- [X] T011 Create `RetentionModule.cs` implementing `IModuleRegistrar`: register `RetentionDbContext` (Npgsql, MigrationsHistory table), bind both options; wire the context into `app.MigrateAllModules()`

**Checkpoint**: migration applies; `retention` schema + both tables exist in a fresh DB.

---

## Phase 3: User Story 1 — Bounded growth via retention policies (Priority: P1) 🎯 MVP

**Goal**: Every table has an explicit documented decision; a generic batched, idempotent purge job enforces the unmanaged ones and records what it did.

**Independent Test**: Insert rows straddling a policy cutoff into one governed table, run `retention-purge`, verify only out-of-policy rows removed and a `retention_runs` row records examined/removed/duration.

### Tests for User Story 1

- [X] T012 [P] [US1] `RetentionPolicyRegistryTests` in tests: **coverage guard** (reflect over every registered `DbContext`'s entity→table map, assert each table has exactly one policy), **keep-forever whitelist** (pinned list of user-owned tables all `Action=Keep`), **well-formedness** invariants, **no-orphan-enforcement** (every `Bespoke` policy names a real job id)
- [X] T013 [P] [US1] `RetentionPurgeServiceTests` in tests: straddle-cutoff (only older rows deleted), batching (≤ batch per statement), idempotent re-run (no double-count), `dryRun=true` (examined>0, removed=0), UTC cutoff

### Implementation for User Story 1

- [X] T014 [P] [US1] Create `Domain/RetentionPolicy.cs` — `RetentionPolicy` record + `RetentionAction` (Purge/Downsample/Keep) + `RetentionEnforcer` (Generic/Bespoke) enums
- [X] T015 [US1] Create `Application/RetentionPolicyRegistry.cs` — `All : IReadOnlyList<RetentionPolicy>` populated per research.md D9 (all schemas/tables incl. Keep + Bespoke entries)
- [X] T016 [US1] Create `Application/Services/RetentionPurgeService.cs` — for each `Generic` `Purge` policy: batched `DELETE ... WHERE ctid IN (SELECT ctid ... WHERE {ts} < @cutoff LIMIT @batch)` loop; UTC cutoff (registry default or `WindowOverrides`); `dryRun` counts only; write one `RetentionRun` with per-table `TableResults` + Outcome
- [X] T017 [US1] Create `Infrastructure/Jobs/RetentionPurgeJob.cs` — `[AutomaticRetry(Attempts=0)]`, `RunAsync(bool dryRun, CancellationToken)` delegating to the service
- [X] T018 [US1] Add `IJobRegistrar` to `RetentionModule.cs`: register `retention-purge` recurring job `Cron.Daily(PurgeHourUtc)`
- [X] T019 [P] [US1] Add `finance_retention_last_run_age_seconds{run_type}` + `finance_retention_rows_removed_total{table}` to `backend/src/FinanceSentry.Infrastructure/Observability/JobMetrics.cs`
- [X] T020 [US1] FR-004 caps: add `retainedFileCountLimit:14` + `fileSizeLimitBytes:100MB` + `rollOnFileSizeLimit` to the Serilog file sink in `SerilogConfiguration.cs`; set explicit `JobExpirationTimeout` in `BankSync/Infrastructure/Jobs/HangfireSetup.cs`

**Checkpoint**: `retention-purge` (dry-run then real) works, records runs, leaves in-window + keep-forever rows untouched; registry guard tests green.

---

## Phase 4: User Story 2 — Recoverable off-host backups (Priority: P1)

**Goal**: Nightly encrypted `pg_dump` lands off-host in R2; a weekly restore drill into an isolated scratch DB proves it restores; outcomes are visible.

**Independent Test**: Trigger `db-backup`, then `db-restore-verify`; verify the scratch DB restore reproduces row counts + a recent transaction, the `backup_runs` row flips to `Verified`, and the scratch DB is dropped.

### Tests for User Story 2

- [X] T021 [P] [US2] `BackupRoundTripTests` in tests: `pg_dump` → age-encrypt → age-decrypt → `pg_restore` into a scratch DB reproduces key-table counts + a sampled recent `bank_sync.transactions` row (SC-003); `IBackupStore` prune keeps exactly `RetainDaily` newest (use a local temp-dir `IBackupStore` fake + local Postgres)
- [X] T022 [P] [US2] `RestoreIsolationTests` in tests: assert `RestoreVerifier` builds a connection string targeting only `restore_verify_*` and never opens a write connection to the app database (FR-006 + edge case)

### Implementation for User Story 2

- [X] T023 [P] [US2] Create `Domain/IBackupStore.cs` — domain interface: `PutAsync(key, stream)`, `GetAsync(key)`, `ListAsync(prefix)`, `DeleteAsync(key)` (Principle I: no module touches AWSSDK directly)
- [X] T024 [US2] Create `Infrastructure/Backup/S3BackupStore.cs` implementing `IBackupStore` via `AWSSDK.S3` against R2 (endpoint/bucket/keys from `BackupOptions`, `ForcePathStyle`, region `auto`, multipart put); register in `RetentionModule` DI; **no-op + warn when keys blank**
- [X] T025 [P] [US2] Create `Infrastructure/Backup/PgDumpRunner.cs` — shells `pg_dump -Fc` piped through `age --encrypt`, and `age --decrypt` → `createdb`/`pg_restore`/`dropdb`; connection info from the app connection string; streams to avoid buffering full dump
- [X] T026 [US2] Create `Infrastructure/Jobs/BackupJob.cs` — `[AutomaticRetry(0)]`: dump→encrypt→`IBackupStore.Put` under `daily/`|`weekly/`; insert `backup_runs` `Pending` with `Sha256`+`SizeBytes`; prune R2 objects + rows beyond `RetainDaily`/`RetainWeekly`
- [X] T027 [US2] Create `Application/Services/RestoreVerifier.cs` — download latest artifact→decrypt→`createdb restore_verify_<utc>`→`pg_restore`→read-only checks (row counts, latest tx recency)→`dropdb`; return verified/failed
- [X] T028 [US2] Create `Infrastructure/Jobs/RestoreVerifyJob.cs` — `[AutomaticRetry(0)]`, `Cron.Weekly()`; flips the referenced `backup_runs` row to `Verified`(+`VerifiedAt`) or `Failed`
- [X] T029 [US2] Register `db-backup` (`Cron.Daily(BackupHourUtc)`) + `db-restore-verify` (`Cron.Weekly()`) in `RetentionModule` `IJobRegistrar`
- [X] T030 [US2] Add `postgresql-client-14` (version-matched) + `age` binary to the API image in `docker/Dockerfile`; verify `pg_dump --version` = 14.x at build
- [X] T031 [P] [US2] Add `finance_backup_last_verified_age_seconds` + `finance_backup_last_verified_timestamp` observable gauges (from `backup_runs`) to `Observability/JobMetrics.cs`
- [X] T032 [P] [US2] Add Grafana dashboard `docker/observability/grafana/dashboards/retention-backups.json` (backup age, last-verified ts, rows-removed/run, restore-drill status) — dashboards-as-code

**Checkpoint**: backup → R2 artifact exists & encrypted; restore drill verifies into isolation and marks `Verified`; SC-002 query + Grafana panel answer "last provably-restorable backup".

---

## Phase 5: User Story 3 — Downsampling history tables (Priority: P2)

**Goal**: Old fine-grained history compacts to coarser resolution instead of vanishing; charts stay continuous.

**Independent Test**: Seed 2 years of daily `daily_bars`/`net_worth_snapshots`, run `retention-downsample`, verify recent stays daily, old collapses to weekly aggregates, total rows drop, a multi-year chart spans the boundary with no gap/double-count.

- [ ] T033 [P] [US3] `DownsampleTests` in tests: recent-window untouched, old rows replaced by correct aggregates, row count drops, boundary continuity (no gap / no double-count)
- [ ] T034 [P] [US3] Create `Application/Downsamplers/IDownsampler.cs` (per-table aggregate contract)
- [ ] T035 [P] [US3] Create `Application/Downsamplers/DailyBarsDownsampler.cs` — beyond 365d, collapse `radar.daily_bars` to weekly OHLC (first open / max high / min low / last close / sum volume) per `(Ticker, iso-week)`, transactional replace
- [ ] T036 [P] [US3] Create `Application/Downsamplers/NetWorthDownsampler.cs` — beyond 365d, keep last snapshot per ISO week per user in `wealth.net_worth_snapshots`
- [ ] T037 [US3] Create `Application/Services/DownsampleService.cs` — runs enabled `IDownsampler`s, writes a `RetentionRun` (RunType=Downsample)
- [ ] T038 [US3] Create `Infrastructure/Jobs/DownsampleJob.cs` + register `retention-downsample` (`Cron.Daily(4)`), **gated on `Downsample:Enabled` (default off)**

**Checkpoint**: with the flag on, old history compacts correctly; charts continuous.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T039 [P] Bump backend version in `backend/src/FinanceSentry.API/FinanceSentry.API.csproj` (new module → MINOR)
- [ ] T040 [P] Run `/csharp-quality` sweep over all new `.cs` files — zero `dotnet build` warnings gate
- [ ] T041 [P] Log the migration-history-table-name discrepancy (Budgets/Subscriptions/Alerts/Crypto/Brokerage runtime vs design-time) as a tracked follow-up in `specs/ROADMAP.md`
- [X] T042 Run `quickstart.md` end-to-end against the local stack: dry-run purge, real purge, `db-backup`, `db-restore-verify`; confirm run records + Grafana panel
- [ ] T043 [P] Update `CLAUDE.md` Key Files + Current App State with the Retention module

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (P1)** → no deps.
- **Foundational (P2)** → after Setup; **BLOCKS US1 + US2** (both need the `retention` schema + entities + module registration).
- **US1 (P3)** and **US2 (P4)** → after Foundational; independent of each other, can run in parallel. Ship together (both P1).
- **US3 (P5)** → after Foundational; independent (reuses `RetentionRun`). P2, ships after US1/US2.
- **Polish (P6)** → after the stories being delivered are complete.

### Within Each Story

- Tests before implementation (write failing, then implement).
- Registry/entities before services; services before jobs; jobs before registration.

### Parallel Opportunities

- Setup: T003 ∥.
- Foundational: T004 ∥ T005; T009 ∥ T010 (T006–T008, T011 sequential — same context/migration).
- US1: T012 ∥ T013 (tests); T014 ∥ T019 then T015→T016→T017→T018; T020 ∥.
- US2: T021 ∥ T022 (tests); T023 ∥ T025 ∥ T031 ∥ T032; T024→T026, T027→T028→T029; T030 ∥.
- US3: T033 ∥ T034 ∥ T035 ∥ T036 then T037→T038.
- US1 and US2 whole phases can proceed in parallel after Foundational.

---

## Implementation Strategy

### MVP (US1 + US2 together — the P1 bundle)

1. Phase 1 Setup → Phase 2 Foundational.
2. Phase 3 US1 (retention) + Phase 4 US2 (backups) — deliver together; the spec forbids purge without proven restore.
3. **STOP & VALIDATE**: quickstart US1 + US2 checks; confirm restore drill marks `Verified`.
4. Merge → deploy to VPS → verify a real R2 artifact + a real restore drill on prod data.

### Increment

5. Phase 5 US3 (downsampling) behind `Downsample:Enabled`, enabled after validating aggregates on a copy.
6. Phase 6 Polish (version bump, quality sweep, docs, follow-up).

---

## Notes

- [P] = different files, no incomplete-task deps.
- R2 secrets + backup age keypair are already provisioned & verified in `docker/.env.sops` (blank config = jobs no-op, so US1 can build/test without them).
- Reuse the 023 rails: every job here is a Hangfire job, so `JobMetricsFilter` + `ConsecutiveFailureAlertFilter → Alerts → Companion → Telegram` cover FR-007 with no extra wiring.
- Do NOT touch the four existing bespoke purge jobs (`DataRetentionJob`, `AlertPurgeJob`, radar prune, `CandidateExpiryJob`) — the registry documents their tables as `Bespoke`.
- Commit per task or logical group; per-task branch discipline per constitution.
