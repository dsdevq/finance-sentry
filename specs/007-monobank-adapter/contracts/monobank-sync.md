# API Contract: Monobank Sync

**Feature**: `007-monobank-adapter`  
**Reused endpoints** (no contract changes, Monobank accounts participate equally)

---

## POST /api/v1/accounts/{accountId}/sync (existing, extended)

No contract change. Monobank accounts participate in the same sync trigger endpoint as Plaid accounts. The system resolves the correct `IBankProvider` via `BankProviderFactory` based on `BankAccount.Provider`.

### Request

```http
POST /api/v1/accounts/{accountId}/sync
Authorization: Bearer <jwt>
```

### Response — 202 Accepted (unchanged)

```json
{
  "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "message": "Sync job started."
}
```

---

## GET /api/v1/accounts/{accountId}/sync-status (existing, unchanged)

No contract change. Returns sync status for any account regardless of provider.

---

## Scheduled Sync (internal, no HTTP contract)

Hangfire `ScheduledSyncJob` iterates all active `BankAccount` rows regardless of `Provider`. For each account, it calls `IBankProviderFactory.Resolve(account.Provider).SyncTransactionsAsync(...)`. No new endpoints required.

---

## Monobank Rate Limit Handling

The sync layer MUST back off and retry when Monobank returns HTTP 429. Retry strategy:

| Attempt | Delay |
|---|---|
| 1st retry | 60 seconds |
| 2nd retry | 120 seconds |
| 3rd retry | fail the sync job |

Sync job records `errorCode: "MONOBANK_RATE_LIMITED"` on the `SyncJob` entity after exhausting retries.
