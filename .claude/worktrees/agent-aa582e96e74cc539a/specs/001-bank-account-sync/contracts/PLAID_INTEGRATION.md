# Plaid Integration Contract

**Service**: Plaid  
**Version**: 2024-01 (Plaid API v1.217+)  
**Status**: Production-Ready  
**Endpoints**: https://production.plaid.com (EU data residency)

---

## Overview

**What We Use Plaid For**:
1. Secure credential collection via Plaid Link UI
2. Account discovery and current balance fetching
3. Historical transaction retrieval (6-12 months back)
4. Real-time transaction notifications via webhooks
5. Periodic full-sync reconciliation (fallback to webhooks)

**What We DON'T Store**:
- Raw bank usernames/passwords (Plaid handles this)
- PII beyond last-4 account digits and owner name
- Passwords in logs or error messages

---

## Architecture: Hybrid Webhook + Polling

### Sync Strategy

- **Webhook Path (Real-time)**: Transaction posts at bank → Plaid detects (5-30s) → Webhook sent → Fetch immediately → Latency: <1 minute
- **Polling Path (Safety Net)**: Every 2 hours, fetch all transactions → Catches any missed webhooks
- **Result**: 99.95% effective reliability + < 1 minute average latency

---

## 1. Plaid Link Flow (Frontend → Backend)

### Frontend: Initiate Link

**Endpoint**: `POST /api/bank-sync/accounts/connect`

**Response**:
```json
{
  "linkToken": "link-sandbox-xxx-yyy-zzz",
  "expiresIn": 600,
  "requestId": "plaid-req-1234567890"
}
```

### Backend: Exchange Public Token

**Endpoint**: `POST /api/bank-sync/accounts/link`

**Request**:
```json
{
  "publicToken": "public-sandbox-xxx-yyy-zzz"
}
```

**Response**:
```json
{
  "accountId": "550e8400-e29b-41d4-a716-446655440000",
  "bankName": "AIB Ireland",
  "accountType": "checking",
  "accountNumberLast4": "1234",
  "ownerName": "John Doe",
  "currency": "EUR",
  "initialBalance": 5000.00,
  "syncStatus": "pending",
  "message": "Account linked. Syncing initial transaction history (6-12 months back). Check dashboard in 2 minutes."
}
```

---

## 2. Sync API: Get Transactions

### Direct Plaid API Call

```http
POST https://production.plaid.com/transactions/get HTTP/1.1
Authorization: Bearer {plaid-client-id}:{plaid-secret-key}
Content-Type: application/json
Plaid-Request-ID: {correlation-id}

{
  "client_id": "YOUR_CLIENT_ID",
  "secret": "YOUR_SECRET",
  "access_token": "{item_id}",
  "start_date": "2026-03-21",
  "end_date": "2026-03-21",
  "options": {
    "count": 250,
    "offset": 0,
    "include_personal_finance_category": true
  }
}
```

### Transaction Response

```json
{
  "accounts": [
    {
      "account_id": "plaid-aXNhZnFlc2Q=",
      "balances": {
        "available": 110.0,
        "current": 110.0,
        "iso_currency_code": "EUR"
      },
      "mask": "0000",
      "name": "Plaid Savings",
      "subtype": "savings",
      "type": "depository"
    }
  ],
  "transactions": [
    {
      "account_id": "plaid-aXNhZjFlc2Q=",
      "account_owner": "John Doe",
      "amount": 12.34,
      "authorized_date": "2026-03-20",
      "date": "2026-03-21",
      "pending": false,
      "transaction_id": "plaid-txId123456",
      "transaction_type": "debit",
      "name": "Starbucks Dublin #1234",
      "merchant_name": "Starbucks",
      "category": ["Coffee Shops"],
      "personal_finance_category": {
        "primary": "FOOD_AND_DRINK",
        "detailed": "COFFEE_SHOPS"
      }
    }
  ],
  "total_transactions": 150
}
```

---

## 3. Webhook Handling: Real-Time Alerts

### Webhook Endpoint

```http
PUT https://api.finance-sentry.com/webhook/plaid HTTP/1.1
Content-Type: application/json
Plaid-Verification-Header: {hmac-sha256-signature}

{
  "webhook_type": "TRANSACTIONS",
  "webhook_code": "TRANSACTIONS_READY",
  "item_id": "item-1234567890",
  "user_id": "user-uuid-1234",
  "new_transactions": 5,
  "timestamp": "2026-03-21T14:30:00Z"
}
```

**Webhook Event Types**:

| Type | Code | Action |
|------|------|--------|
| TRANSACTIONS | TRANSACTIONS_READY | Queue immediate sync |
| TRANSACTIONS | SYNC_UPDATES_AVAILABLE | Include in next scheduled sync |
| ITEM | ERROR | Mark account as `reauth_required` |
| TRANSACTIONS | PENDING_EXPIRATION | Update pending transaction status |

---

## 4. Error Codes & Handling

**Plaid Error Codes**:

| Error Code | HTTP | Meaning | Our Response |
|------------|------|---------|---|
| INVALID_REQUEST | 400 | Bad request | Log for debugging |
| ITEM_LOGIN_REQUIRED | 401 | Credentials expired | Set `sync_status = reauth_required` |
| RATE_LIMIT_EXCEEDED | 429 | Too many calls | Circuit breaker; delay 2-5 min |
| PLANNED_MAINTENANCE | 503 | Plaid maintenance | Retry after 30 min |
| SERVER_ERROR | 500+ | Plaid server error | Polly retry with jitter |
| INSTITUTION_ERROR | 400 | Bank API error | Retry; surface error after 3 failures |

**Retry Strategy**: Exponential backoff (5s → 25s → 125s → 625s) with circuit breaker

---

## 5. Rate Limiting

**Plaid Limits**:
- Transactions Endpoint: 600 requests/minute per access token
- Accounts Endpoint: 1200 requests/minute per credential

**Our Usage**:
- Each account: 1 sync call every 2 hours = 12 calls/day
- 1000 accounts = 12,000 calls/day (well below limits)

---

## 6. Correlation IDs & Debugging

**Every sync request includes correlation_id**:

```csharp
var correlationId = Guid.NewGuid().ToString("N");
// Pass to Plaid in request header: Plaid-Request-ID: {correlationId}
// Store in SyncJob and all related transaction records
// Query: SELECT * FROM sync_jobs WHERE correlation_id = 'uuid'
```

---

## Implementation Checklist

- [ ] Register for Plaid developer account (sandbox + production)
- [ ] Implement PlaidClient wrapper (NuGet: Plaid or HttpClient)
- [ ] Implement link token endpoint (/api/bank-sync/accounts/connect)
- [ ] Implement token exchange endpoint (/api/bank-sync/accounts/link)
- [ ] Implement transaction sync service with Polly retry + circuit breaker
- [ ] Implement webhook endpoint with signature verification
- [ ] Implement deduplication logic (unique_hash based)
- [ ] Configure encryption for Plaid item_ids
- [ ] Set up monitoring/alerting on Plaid errors
- [ ] Test with Plaid sandbox credentials
- [ ] Load testing: stress test with 1000 concurrent account syncs
