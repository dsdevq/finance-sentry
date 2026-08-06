# Internal Contracts: Data Retention & Backups

This feature is **backend-internal** — no new REST endpoints or MCP tools. Operator
visibility is delivered through Prometheus metrics + a Grafana dashboard + the
`retention.*` tables. The "contracts" that must not silently break are therefore: the
job entry-point signatures, the policy-registry invariants, the metric names, and the
run-record table shapes. Each has a corresponding test (see below).

---

## C1. Job contracts (Hangfire entry points)

Registered by `RetentionModule : IJobRegistrar`. All `[AutomaticRetry(Attempts = 0)]`.

| Recurring job id | Class.method | Schedule (default) |
|---|---|---|
| `retention-purge` | `RetentionPurgeJob.RunAsync(bool dryRun, CancellationToken)` | `Cron.Daily(PurgeHourUtc=3)` |
| `retention-downsample` | `DownsampleJob.RunAsync(CancellationToken)` | `Cron.Daily(4)` — **disabled unless `Downsample:Enabled`** (P2) |
| `db-backup` | `BackupJob.RunAsync(CancellationToken)` | `Cron.Daily(BackupHourUtc=2)` |
| `db-restore-verify` | `RestoreVerifyJob.RunAsync(CancellationToken)` | `Cron.Weekly()` |

**`dryRun=true`** on `RetentionPurgeJob` MUST examine and log/record counts without
deleting (mirrors existing `DataRetentionJob`) — this is the harness for the US1
independent test.

**Contract tests**:
- Each job resolves from DI and its recurring id registers (module registration test).
- `RetentionPurgeJob` with `dryRun=true` deletes zero rows but records non-zero `examined`.

---

## C2. Policy registry contract

`RetentionPolicyRegistry.All : IReadOnlyList<RetentionPolicy>`.

**Guarantees (unit-tested — these are the FR-001 enforcement):**
1. **Coverage**: every table mapped by every registered `DbContext` has exactly one policy.
2. **Well-formedness**: each policy satisfies the D-column invariants in data-model.md.
3. **No orphan enforcement**: every `EnforcedBy=Bespoke` policy names a job id that exists
   in a registered `IJobRegistrar` (guards against a bespoke job being deleted while its
   table silently stops being purged).
4. **Keep-forever whitelist**: the FR-008 user-owned tables (transactions, bank_accounts,
   theses, watchlist, IPS, budgets, holdings, credentials, connections, rule_sets,
   subscriptions, RAG corpus) MUST have `Action=Keep`. A test pins this list so a future
   edit can't accidentally set a financial table to `Purge`.

---

## C3. Purge behavior contract

For a `Generic` purge policy over `(schema, table, ts, windowDays, batch)`:
- Rows with `ts < now_utc - windowDays` are deleted; rows within the window are untouched (US1-AS1).
- Deletion runs in batches of ≤ `batch`; total deleted is recorded in the run's `TableResults`.
- Re-running immediately is a no-op for in-window rows and does not double-count (idempotent, US1-AS3).
- Identifiers come only from the compiled registry (no user input) → parameterized cutoff, quoted identifiers.

**Integration test**: seed rows straddling the cutoff → run → assert only out-of-policy
rows gone, `retention_runs` row written with correct `examined`/`removed`.

---

## C4. Backup + restore-verify contract

- `BackupJob`: produces an **age-encrypted** `pg_dump -Fc` artifact in R2 under `daily/`
  (or `weekly/`), inserts a `backup_runs` row `VerificationStatus=Pending` with `Sha256`
  + `SizeBytes`, and prunes R2 objects + rows beyond `RetainDaily`/`RetainWeekly`.
- `RestoreVerifyJob`: restores the latest artifact into an **isolated scratch database**
  (`restore_verify_<utc>`), runs read-only checks, drops the scratch DB, and flips the
  artifact row to `Verified`(+`VerifiedAt`) or `Failed`. MUST NOT write to any production
  table (FR-006 + edge case).

**Tests** (CI-runnable without R2 by using a local temp dir + local Postgres):
- Backup→restore round-trip into a scratch DB reproduces row counts + a sampled recent
  transaction (US2 independent test / SC-003).
- Restore target is a distinct database; the job never opens a write connection to the
  app database (asserted via the connection string it builds).
- Pruning keeps exactly `RetainDaily` newest daily artifacts.

---

## C5. Metrics contract (Prometheus)

Added to the existing `FinanceSentry.Jobs` meter (023):

| Metric | Type | Labels | Feeds |
|---|---|---|---|
| `finance_backup_last_verified_age_seconds` | observable gauge | — | SC-002 |
| `finance_backup_last_verified_timestamp` | observable gauge | — | SC-002 dashboard |
| `finance_retention_last_run_age_seconds` | observable gauge | `run_type` | SC-005 monitoring |
| `finance_retention_rows_removed_total` | counter | `table` | growth dashboards |

Retention/backup jobs also inherit `finance_jobs_succeeded/_failed/_duration` and the
consecutive-failure → Telegram alert path automatically (they are Hangfire jobs). Grafana
dashboard `retention-backups.json` is provisioned as code alongside the 023 dashboards.
