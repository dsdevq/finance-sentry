# Operations Runbook — Bank Sync

## 1. Sync Failing for an Account

**Symptoms:** `SyncJob.Status = 'failed'`, `BankAccount.SyncStatus = 'failed'`

**Steps:**
1. Check `sync_jobs` table for `error_message` and `error_code`.
2. If `error_code = 'PLAID_RATE_LIMIT'`: Plaid is throttling. Wait and let Polly retry (5m → 15m → 1h).
3. If `error_code = 'PLAID_ITEM_LOGIN_REQUIRED'`: User must re-link. Account status = `reauth_required`. Notify user.
4. If `error_code = 'DATABASE_ERROR'`: Check DB connectivity. Run `SELECT 1` against PostgreSQL.
5. Manually trigger re-sync once root cause resolved:
   ```
   POST /api/accounts/{accountId}/sync?userId={userId}
   ```

## 2. High Error Rate (5xx)

**Symptoms:** Grafana alert, error rate > 1%

**Steps:**
1. Check Serilog/Application Insights for ERROR-level logs.
2. Common causes:
   - DB connection pool exhausted: increase `Max Pool Size` in connection string.
   - Plaid API down: check `https://status.plaid.com`. All syncs will fail until resolved.
   - Memory pressure: restart the API container (`docker-compose restart api`).
3. If DB migration pending: run `dotnet ef database update`.

## 3. Slow Queries (> 100ms)

**Symptoms:** `EFQueryLoggerInterceptor` WARNING logs

**Steps:**
1. Identify slow query in logs (`SLOW QUERY [XXXms] SQL: ...`).
2. Run `EXPLAIN ANALYZE` on the query in PostgreSQL.
3. Common fixes:
   - Missing index: check `idx_transaction_account_active`, `idx_bank_account_user_id`.
   - N+1: use `Include()` / `Join()` instead of looping queries.
4. If N+1 suspected: look for `Potential N+1 detected: X DB round-trips` in logs.

## 4. Plaid Webhook Not Triggering Sync

**Symptoms:** Transactions not updating despite bank activity

**Steps:**
1. Verify webhook URL is registered in Plaid Dashboard: `POST https://your-api/api/webhook/plaid`.
2. Check HMAC signature key matches `Plaid:WebhookKey` in `appsettings.json`.
3. Check `WebhookController` logs for `HMAC validation failed`.
4. Manually trigger sync to confirm the sync pipeline is working.

## 5. Hangfire Jobs Not Running

**Symptoms:** Recurring jobs not appearing in Hangfire Dashboard

**Steps:**
1. Navigate to `/hangfire` in browser.
2. Check server list — if empty, Hangfire worker is not running. Restart API.
3. Check `SyncScheduler.ScheduleAllActiveAccounts()` was called at startup (logged at startup).
4. Re-register recurring jobs: restart the API (scheduler runs on startup).

## 6. Audit Log Table Growing Too Large

**Symptoms:** `audit_logs` table > 10GB

**Steps:**
1. Archive old rows (> 1 year) to cold storage:
   ```sql
   DELETE FROM audit_logs WHERE performed_at < NOW() - INTERVAL '1 year';
   ```
2. Add a partition by month if volume is sustained (consult DBA).

## 7. Data Retention Job Failing

**Symptoms:** `DataRetentionJob` ERROR in logs

**Steps:**
1. Check Hangfire Dashboard for job error details.
2. Common cause: DB connection issue. Fix DB, then re-trigger manually:
   ```csharp
   backgroundJobClient.Enqueue<DataRetentionJob>(j => j.RunAsync(false, CancellationToken.None));
   ```
3. Use dry-run mode first to verify: `j.RunAsync(true, ...)` — logs count without archiving.

## 8. Health Check Returning Unhealthy

```
GET /health/ready → 503
```

**Steps:**
1. Check response body: `{ "status": "Unhealthy", "checks": { "npgsql": "Unhealthy" } }`
2. If `npgsql` unhealthy: PostgreSQL is unreachable. Check Docker container status.
3. If still failing after DB restart: check connection string in `appsettings.json`.

## Contact

- On-call channel: `#finance-sentry-oncall`
- Escalation: DBA team for database issues, Plaid support for API issues.
