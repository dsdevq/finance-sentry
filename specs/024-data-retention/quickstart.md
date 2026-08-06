# Quickstart: Data Retention & Backups (024)

Backend-only feature. New module `FinanceSentry.Modules.Retention` +
`postgresql-client`/`age` in the API image + Cloudflare R2 secrets + one Grafana dashboard.

## Prerequisites

1. **Cloudflare R2**: create a bucket (e.g. `finance-sentry-backups`) and an R2 API token
   (S3 credentials). Note the account-scoped S3 endpoint.
2. **Backup age keypair**: `age-keygen -o backup-age.key` → the file's public line is the
   recipient, the `AGE-SECRET-KEY-...` line is the identity.
3. Add to `docker/.env.sops` (then re-encrypt with `docker/secrets-encrypt.sh`):
   ```
   BACKUP_R2_ENDPOINT=https://<account>.r2.cloudflarestorage.com
   BACKUP_R2_BUCKET=finance-sentry-backups
   BACKUP_R2_ACCESS_KEY=...
   BACKUP_R2_SECRET_KEY=...
   BACKUP_AGE_RECIPIENT=age1...
   BACKUP_AGE_IDENTITY=AGE-SECRET-KEY-1...
   ```
   Keyless/unset ⇒ backup jobs no-op with a warning (dev/local safe), same pattern as the
   keyless Finnhub source.

## Run locally

```bash
cd docker && docker compose -f docker-compose.dev.yml up -d --build postgres api
curl -s http://localhost:5001/api/v1/health   # {"status":"healthy"}
```

## Verify retention (US1)

```bash
# Trigger a dry-run purge from the Hangfire dashboard (http://localhost:5001/hangfire)
#   → job "retention-purge", or enqueue RetentionPurgeJob.RunAsync(dryRun:true)
# Then a real run; inspect the run record:
docker exec finance-sentry-postgres psql -U finance_user -d finance_sentry -c \
  "select run_type, outcome, table_results, completed_at-started_at as dur from retention.retention_runs order by started_at desc limit 3;"
```
Expect: dry-run records `examined>0, removed=0`; real run removes only out-of-window rows.

## Verify backup + restore drill (US2)

```bash
# Enqueue db-backup, then db-restore-verify from /hangfire, then:
docker exec finance-sentry-postgres psql -U finance_user -d finance_sentry -c \
  "select kind, verification_status, size_bytes, verified_at from retention.backup_runs order by created_at desc limit 5;"
```
Expect: a `Backup` row `Pending` → after the drill, `Verified` with `verified_at` set;
the scratch DB `restore_verify_*` no longer exists.

**SC-002 check** — "last provably-restorable backup":
```bash
docker exec finance-sentry-postgres psql -U finance_user -d finance_sentry -c \
  "select max(verified_at) from retention.backup_runs where verification_status='Verified';"
```
Or read `finance_backup_last_verified_age_seconds` on the Grafana **Retention & Backups**
dashboard.

## Tests

```bash
dotnet test backend/ --filter FullyQualifiedName~Retention
```
Covers: registry coverage guard + keep-forever whitelist, batched idempotent purge,
dry-run, backup→restore round-trip into a scratch DB, R2 pruning window.

## Observability

Failures surface automatically: retention/backup jobs are Hangfire jobs, so
`JobMetricsFilter` + `ConsecutiveFailureAlertFilter` already emit metrics and escalate
repeated failures to Telegram (023). New Grafana dashboard:
`docker/observability/grafana/dashboards/retention-backups.json`.
