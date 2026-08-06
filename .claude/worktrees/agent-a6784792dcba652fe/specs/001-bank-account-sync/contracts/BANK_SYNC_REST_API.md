# Bank Sync REST API Contract

**Version**: 1.0  
**Base URL**: `https://api.finance-sentry.com/api/bank-sync`  
**Authentication**: Bearer JWT token (Authorization header)  
**Default Status**: Success (2xx), Client Error (4xx), Server Error (5xx)

---

## 1. POST /accounts/connect - Initiate Plaid Link

**Purpose**: Generate Plaid Link token for frontend credential entry

**Authentication**: Required (Bearer token)

**Request**:
```http
POST /api/bank-sync/accounts/connect HTTP/1.1
Authorization: Bearer {jwt-token}
Content-Type: application/json

{}
```

**Response (200 OK)**:
```json
{
  "linkToken": "link-prod-a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6",
  "expiresIn": 600,
  "expiresAt": "2026-03-21T14:40:00Z",
  "requestId": "plaid-req-1234567890"
}
```

**Response (401 Unauthorized)**:
```json
{
  "error": "unauthorized",
  "message": "Invalid or expired JWT token"
}
```

---

## 2. POST /accounts/link - Exchange Public Token

**Purpose**: After Plaid Link, exchange public_token for account creation

**Authentication**: Required

**Request**:
```json
{
  "publicToken": "public-prod-a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6"
}
```

**Response (200 OK)**:
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
  "lastSyncTimestamp": null,
  "message": "Account linked successfully. Initial transaction history syncing (6-12 months). Refresh dashboard in 2-3 minutes."
}
```

**Response (400 Bad Request)**:
```json
{
  "error": "invalid_public_token",
  "message": "public_token invalid or expired (>10 minutes old)"
}
```

**Response (429 Too Many Requests)**:
```json
{
  "error": "rate_limit_exceeded",
  "message": "Too many accounts linked in short time. Wait 5 minutes.",
  "retryAfter": 300
}
```

---

## 3. GET /accounts - List Connected Bank Accounts

**Purpose**: Retrieve all connected accounts for authenticated user

**Authentication**: Required

**Request**:
```http
GET /api/bank-sync/accounts?status=active&sort=last_sync_timestamp:desc HTTP/1.1
Authorization: Bearer {jwt-token}
```

**Query Parameters**:

| Param | Type | Example | Default |
|-------|------|---------|---------|
| status | string | `active`, `failed`, `reauth_required` | omit |
| currency | string | `EUR`, `GBP`, `USD` | omit |
| sort | string | `last_sync_timestamp:desc`, `balance:desc` | `bank_name:asc` |

**Response (200 OK)**:
```json
{
  "accounts": [
    {
      "accountId": "550e8400-e29b-41d4-a716-446655440000",
      "bankName": "AIB Ireland",
      "accountType": "checking",
      "accountNumberLast4": "1234",
      "ownerName": "John Doe",
      "currency": "EUR",
      "currentBalance": 5000.00,
      "availableBalance": 4800.00,
      "syncStatus": "active",
      "lastSyncTimestamp": "2026-03-21T12:30:00Z",
      "lastSyncDurationMs": 2500,
      "createdAt": "2026-03-21T10:00:00Z"
    }
  ],
  "totalCount": 1,
  "currency_totals": {
    "EUR": 5000.00
  }
}
```

---

## 4. GET /accounts/{accountId}/transactions - List Transactions

**Purpose**: Fetch transactions for specific account with pagination

**Authentication**: Required

**Request**:
```http
GET /api/bank-sync/accounts/550e8400-e29b-41d4-a716-446655440000/transactions?start_date=2026-01-01&end_date=2026-03-21&offset=0&limit=50 HTTP/1.1
Authorization: Bearer {jwt-token}
```

**Query Parameters**:

| Param | Type | Required | Example | Description |
|-------|------|----------|---------|---|
| start_date | date | No | `2026-01-01` | Filter transactions on/after |
| end_date | date | No | `2026-03-21` | Filter transactions on/before |
| offset | integer | No | `0` | Pagination offset (default: 0) |
| limit | integer | No | `50` | Results per page (default: 50, max: 200) |
| status | string | No | `posted`, `pending`, `all` | Filter pending vs posted (default: `posted`) |
| sort | string | No | `date:desc` | Sort order (default: `date:desc`) |

**Response (200 OK)**:
```json
{
  "accountId": "550e8400-e29b-41d4-a716-446655440000",
  "bankName": "AIB Ireland",
  "currency": "EUR",
  "transactions": [
    {
      "transactionId": "abc12345-1234-5678-90ab-cdef12345678",
      "accountId": "550e8400-e29b-41d4-a716-446655440000",
      "amount": 12.34,
      "transactionType": "debit",
      "postedDate": "2026-03-21",
      "pendingDate": null,
      "isPending": false,
      "description": "Starbucks Dublin #1234",
      "merchantCategory": "COFFEE_SHOPS",
      "syncedAt": "2026-03-21T12:35:00Z",
      "createdAt": "2026-03-21T12:35:15Z"
    },
    {
      "transactionId": "def67890-5678-1234-56ab-cdef12345679",
      "accountId": "550e8400-e29b-41d4-a716-446655440000",
      "amount": 5000.00,
      "transactionType": "credit",
      "postedDate": null,
      "pendingDate": "2026-03-21",
      "isPending": true,
      "description": "International Wire Transfer",
      "merchantCategory": "TRANSFER_INCOMING",
      "syncedAt": "2026-03-21T12:40:00Z",
      "createdAt": "2026-03-21T12:40:15Z"
    }
  ],
  "pagination": {
    "offset": 0,
    "limit": 50,
    "totalCount": 250,
    "hasNextPage": true,
    "hasPreviousPage": false,
    "pageCount": 5
  }
}
```

**Response (404 Not Found)**:
```json
{
  "error": "account_not_found",
  "message": "Account 550e8400-e29b-41d4-a716-446655440000 not found"
}
```

---

## 5. GET /dashboard/aggregated - Aggregated Money Flow Statistics

**Purpose**: Get unified view across all accounts

**Authentication**: Required

**Request**:
```http
GET /api/bank-sync/dashboard/aggregated?start_date=2025-09-21&end_date=2026-03-21&base_currency=EUR HTTP/1.1
Authorization: Bearer {jwt-token}
```

**Query Parameters**:

| Param | Type | Required | Example | Description |
|-------|------|----------|---------|---|
| start_date | date | No | `2025-09-21` | Analysis start (default: 6 months ago) |
| end_date | date | No | `2026-03-21` | Analysis end (default: today) |
| base_currency | string | No | `EUR` | Convert all to this currency (default: user's primary) |

**Response (200 OK)**:
```json
{
  "accountSummary": {
    "totalActiveAccounts": 3,
    "lastSyncTimestamp": "2026-03-21T12:45:00Z",
    "accountsByStatus": {
      "active": 3,
      "reauth_required": 0,
      "failed": 0
    }
  },
  "balanceByCurrency": {
    "EUR": {
      "total": 7500.00,
      "accounts": ["AIB", "ING"],
      "lastUpdated": "2026-03-21T12:35:00Z"
    },
    "GBP": {
      "total": 2000.00,
      "accounts": ["Wise"],
      "lastUpdated": "2026-03-21T12:45:00Z"
    }
  },
  "netWorthTimeframe": {
    "baseCurrency": "EUR",
    "current": 9500.00,
    "change90Days": {
      "amount": 1300.00,
      "percentage": 15.85,
      "direction": "up"
    }
  },
  "monthlyFlow": [
    {
      "month": "2025-09",
      "inflows": 3000.00,
      "outflows": 1500.00,
      "netFlow": 1500.00,
      "transactionCount": 35
    }
  ],
  "spendingByCategory": [
    {
      "category": "FOOD_AND_DRINK",
      "amount": 650.00,
      "percentage": 12.5,
      "trend": "up"
    }
  ]
}
```

---

## 6. POST /accounts/{accountId}/sync - Manual Sync Trigger

**Purpose**: Trigger immediate sync for specific account

**Authentication**: Required

**Request**:
```http
POST /api/bank-sync/accounts/550e8400-e29b-41d4-a716-446655440000/sync HTTP/1.1
Authorization: Bearer {jwt-token}
Content-Type: application/json

{}
```

**Response (200 OK)**:
```json
{
  "syncJobId": "a1b2c3d4-e5f6-4789-10ab-cdef12345678",
  "status": "in_progress",
  "message": "Sync started. Fetching latest transactions...",
  "estimatedCompletionTime": 5
}
```

**Response (409 Conflict)**:
```json
{
  "error": "sync_in_progress",
  "message": "Sync already in progress for this account. Last sync started at 2026-03-21T12:40:00Z"
}
```

---

## 7. DELETE /accounts/{accountId} - Unlink Bank Account

**Purpose**: Delete/unlink a connected bank account

**Authentication**: Required

**Request**:
```http
DELETE /api/bank-sync/accounts/550e8400-e29b-41d4-a716-446655440000 HTTP/1.1
Authorization: Bearer {jwt-token}
```

**Response (204 No Content)**: Account deleted

**Response (404 Not Found)**:
```json
{
  "error": "account_not_found",
  "message": "Account not found or already deleted"
}
```

---

## Error Codes

**Common Error Responses**:

| Status | Code | Meaning |
|--------|------|---------|
| 400 | bad_request | Malformed request |
| 401 | unauthorized | Missing or invalid JWT |
| 403 | forbidden | User doesn't own account |
| 404 | not_found | Resource not found |
| 429 | rate_limit_exceeded | Too many requests |
| 500 | internal_error | Server error |

---

## Rate Limiting

**Headers**:
- `X-RateLimit-Limit`: Max requests per window
- `X-RateLimit-Remaining`: Requests left in window
- `X-RateLimit-Reset`: Unix timestamp of window reset

**Limits**:
- 100 requests/minute per authenticated user
- 1000 requests/minute per IP for public endpoints

---

## Authentication

**JWT Token Format**:
```
Authorization: Bearer {base64-encoded-jwt}
```

**Token Claims**:
```json
{
  "sub": "user-uuid",
  "exp": 1742505600,
  "iat": 1742419200,
  "scopes": ["bank:read", "bank:write"]
}
```

---

## Response Headers

```
Content-Type: application/json
X-Request-ID: {unique-request-id}
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1742505600
Cache-Control: no-cache, no-store, must-revalidate
```

---

## Implementation Checklist

- [ ] Implement authentication middleware (JWT validation)
- [ ] Implement authorization checks (user owns account)
- [ ] Implement pagination (offset/limit)
- [ ] Implement sorting (multiple fields)
- [ ] Implement filtering (status, currency, date ranges)
- [ ] Implement rate limiting (sliding window)
- [ ] Add request correlation IDs to all responses
- [ ] Implement comprehensive error handling
- [ ] Add API documentation (Swagger/OpenAPI)
- [ ] Add API versioning strategy
- [ ] Set up monitoring/alerting on API performance
- [ ] Load test with 1000 concurrent users
