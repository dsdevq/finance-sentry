# Portfolio Management Backend REST API Contract

**Document Version**: 1.0  
**Framework**: ASP.NET Core 9+ with OpenAPI/Swagger  
**Base URL**: `https://api.finance-sentry.com/api/v1/portfolio`  
**Authentication**: Bearer Token (JWT)  
**Content-Type**: application/json  

---

## Overview

This document defines the REST API contract for portfolio management. The backend exposes endpoints for connecting investment accounts, fetching holdings, calculating metrics, and managing portfolios.

### Principles

- All responses include `data` and `meta` fields
- All user-scoped queries are automatically filtered by Authorization token
- All errors follow standard error response format
- All timestamps in ISO 8601 format (UTC)
- Pagination supported on list endpoints

---

## Authentication

All requests require Bearer token:

```http
Authorization: Bearer {jwt_token}
```

Token contains user ID (extracted for user-scoped filtering).

---

## Standard Response Format

### Success Response (200, 201)

```json
{
  "data": {
    "id": "uuid",
    "name": "My Binance Account"
  },
  "meta": {
    "timestamp": "2025-03-21T10:30:00Z",
    "version": "1.0"
  }
}
```

### List Response (200)

```json
{
  "data": [
    { "id": "uuid1" },
    { "id": "uuid2" }
  ],
  "meta": {
    "pagination": {
      "page": 1,
      "pageSize": 20,
      "total": 45,
      "hasNext": true
    }
  }
}
```

### Error Response (4xx, 5xx)

```json
{
  "error": {
    "code": "INVALID_REQUEST",
    "message": "API keys are required",
    "statusCode": 400
  },
  "meta": {
    "timestamp": "2025-03-21T10:30:00Z",
    "traceId": "uuid-trace-id"
  }
}
```

---

## Endpoints

### 1. Get Portfolio Summary

**Endpoint**: `GET /`

**Purpose**: Fetch aggregated portfolio metrics (all accounts combined).

**Authentication**: Required (Bearer token)

**Query Parameters**:
- `baseCurrency` (string, optional, default: "USD"): Currency for conversion

**Response** (200):

```json
{
  "data": {
    "totalValue": 500000.00,
    "totalCostBasis": 450000.00,
    "totalGainLoss": 50000.00,
    "portfolioVolatility": 15.5,
    "concentrationRisk": 35.2,
    "diversificationScore": 72.5,
    "numberOfAssets": 25,
    "numberOfPlatforms": 2,
    "allocationByType": {
      "crypto": 40,
      "stocks": 45,
      "etfs": 10,
      "bonds": 5
    }
  }
}
```

---

### 2. List Investment Accounts

**Endpoint**: `GET /accounts`

**Purpose**: List all connected investment accounts for the user.

**Query Parameters**:
- `page` (int, optional, default: 1)
- `pageSize` (int, optional, default: 20, max: 100)
- `platform` (string, optional): Filter by platform
- `status` (string, optional): Filter by status

**Response** (200):

```json
{
  "data": [
    {
      "id": "account-uuid-1",
      "name": "My Binance Account",
      "platform": "binance",
      "status": "active",
      "lastSyncTime": "2025-03-21T10:15:00Z",
      "holdingsCount": 12,
      "totalValue": 200000.00
    }
  ],
  "meta": {
    "pagination": {
      "page": 1,
      "pageSize": 20,
      "total": 2,
      "hasNext": false
    }
  }
}
```

---

### 3. Create Investment Account (Connect)

**Endpoint**: `POST /accounts`

**Purpose**: Connect a new investment account.

**Request Body**:

```json
{
  "name": "My Binance Account",
  "platform": "binance",
  "apiKey": "binance-api-key-here",
  "apiSecret": "binance-api-secret-here",
  "accountId": "optional-ib-account-id"
}
```

**Response** (201):

```json
{
  "data": {
    "id": "account-uuid-new",
    "name": "My Binance Account",
    "platform": "binance",
    "status": "pending",
    "message": "Account registered. Starting initial sync..."
  }
}
```

**Error Codes**:
- `400 INVALID_CREDENTIALS`: API key/secret invalid or incomplete
- `400 DUPLICATE_ACCOUNT`: Account already connected
- `422 VALIDATION_ERROR`: Platform not supported

---

### 4. Get Account Detail

**Endpoint**: `GET /accounts/{accountId}`

**Purpose**: Fetch details and current sync status for a specific account.

**URL Parameters**:
- `accountId` (uuid): Investment account ID

**Response** (200):

```json
{
  "data": {
    "id": "account-uuid-1",
    "name": "My Binance Account",
    "platform": "binance",
    "status": "active",
    "lastSyncTime": "2025-03-21T10:15:00Z",
    "lastSyncStatus": "success",
    "holdingsCount": 12,
    "totalValue": 200000.00,
    "holdings": [
      {
        "id": "holding-uuid-1",
        "symbol": "BTC",
        "assetName": "Bitcoin",
        "quantity": 1.5,
        "currentPrice": 45000.00,
        "currentValue": 67500.00,
        "gainLoss": 7500.00,
        "gainLossPercent": 12.5
      }
    ]
  }
}
```

---

### 5. Sync Account (Manual)

**Endpoint**: `POST /accounts/{accountId}/sync`

**Purpose**: Manually trigger sync for an account.

**URL Parameters**:
- `accountId` (uuid): Investment account ID

**Query Parameters**:
- `syncType` (string, optional, default: "full"): full or incremental

**Response** (202 Accepted):

```json
{
  "data": {
    "syncJobId": "job-uuid-123",
    "status": "in_progress",
    "startedAt": "2025-03-21T10:31:00Z",
    "message": "Sync started. Check back for status."
  }
}
```

---

### 6. Get Sync Status

**Endpoint**: `GET /accounts/{accountId}/sync/{syncJobId}`

**Purpose**: Check sync progress and status.

**URL Parameters**:
- `accountId` (uuid): Investment account ID
- `syncJobId` (uuid): Sync job ID

**Response** (200):

```json
{
  "data": {
    "syncJobId": "job-uuid-123",
    "status": "in_progress",
    "startedAt": "2025-03-21T10:31:00Z",
    "assetsSynced": 8,
    "assetsAdded": 2,
    "assetsRemoved": 0,
    "progress": 0.67,
    "message": "Synced 8 of 12 assets..."
  }
}
```

---

### 7. List Holdings (All Accounts)

**Endpoint**: `GET /holdings`

**Purpose**: List all holdings across all connected accounts.

**Query Parameters**:
- `page`, `pageSize`, `assetType`, `symbol`, `sortBy`, `sortOrder`

**Response** (200):

```json
{
  "data": [
    {
      "id": "holding-uuid-1",
      "accountId": "account-uuid-1",
      "symbol": "BTC",
      "assetType": "crypto_coin",
      "quantity": 1.5,
      "currentPrice": 45000.00,
      "currentValue": 67500.00,
      "gainLoss": 7500.00,
      "gainLossPercent": 12.5
    }
  ]
}
```

---

### 8. Get Holding Detail

**Endpoint**: `GET /holdings/{holdingId}`

**Purpose**: Fetch detailed information including price history.

**URL Parameters**:
- `holdingId` (uuid): Holding ID

**Query Parameters**:
- `historyDays` (int, optional, default: 30): Days of price history

**Response** (200):

```json
{
  "data": {
    "id": "holding-uuid-1",
    "symbol": "BTC",
    "assetName": "Bitcoin",
    "quantity": 1.5,
    "currentPrice": 45000.00,
    "priceHistory": [
      {
        "date": "2025-03-21",
        "price": 45000.00,
        "change24h": 1.5,
        "change7d": 5.2,
        "change30d": 8.3
      }
    ]
  }
}
```

---

### 9. Get Portfolio Metrics

**Endpoint**: `GET /metrics`

**Purpose**: Fetch calculated portfolio metrics.

**Query Parameters**:
- `date` (string, optional, ISO 8601): Specific date (default: today)

**Response** (200):

```json
{
  "data": {
    "metricDate": "2025-03-21",
    "totalValue": 500000.00,
    "portfolioVolatility": 15.5,
    "concentrationRisk": 35.2,
    "diversificationScore": 72.5,
    "numberOfAssets": 25
  }
}
```

---

### 10. Disconnect Account

**Endpoint**: `DELETE /accounts/{accountId}`

**Purpose**: Disconnect and remove an investment account.

**URL Parameters**:
- `accountId` (uuid): Investment account ID

**Response** (200):

```json
{
  "data": {
    "id": "account-uuid-1",
    "status": "deleted",
    "deletedAt": "2025-03-21T10:35:00Z",
    "message": "Account disconnected. Historical data retained for 90 days."
  }
}
```

---

## Deferred: AI Analysis Endpoints

The following endpoints are deferred to Phase 2 (AI Integration phase):
- `POST /holdings/{holdingId}/analysis` - Request asset-level AI analysis
- `GET /analysis/{analysisId}` - Retrieve AI analysis report
- `POST /analysis/portfolio` - Request portfolio-level AI analysis

These endpoints will be added when AI features are implemented.

---

## Error Codes Reference

| Code | HTTP Status | Handling |
|------|-------------|----------|
| UNAUTHORIZED | 401 | Re-authenticate user |
| FORBIDDEN | 403 | Log security event |
| NOT_FOUND | 404 | Return user-friendly message |
| INVALID_REQUEST | 400 | Show validation errors |
| DUPLICATE_ACCOUNT | 400 | Suggest reconnecting |
| INVALID_CREDENTIALS | 400 | Prompt user to re-enter credentials |
| SYNC_IN_PROGRESS | 400 | Ask user to wait |
| REAUTH_REQUIRED | 401 | Prompt user to reconnect account |
| RATE_LIMITED | 429 | Show user-friendly message, retry after delay |

---

## Pagination

### Query Parameters

```http
GET /holdings?page=2&pageSize=50
```

### Response Pagination Metadata

```json
{
  "meta": {
    "pagination": {
      "page": 2,
      "pageSize": 50,
      "total": 150,
      "hasNext": true,
      "hasPrev": true
    }
  }
}
```

---

## Caching Headers

Responses include cache headers:

```http
Cache-Control: public, max-age=300
ETag: "abc123def456"
Last-Modified: 2025-03-21T10:15:00Z
```

---

## Rate Limiting

All endpoints subject to rate limiting:

```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 995
X-RateLimit-Reset: 1622563800
```

**Limits**:
- Authenticated users: 1000 requests per hour
- Portfolio sync endpoints: 20 requests per hour per account

---

## Implementation Checklist

- [ ] All responses follow standard format (data/meta)
- [ ] All user-scoped queries filtered by JWT sub claim
- [ ] All timestamps in UTC ISO 8601 format
- [ ] Pagination on list endpoints (max 500 page size)
- [ ] Error responses include traceId for debugging
- [ ] Request validation on all POST/PUT/PATCH endpoints
- [ ] Async operations return 202 Accepted with polling endpoint
- [ ] All endpoints documented in Swagger/OpenAPI
- [ ] Rate limiting enforced and headers set
- [ ] Cache headers set appropriately
- [ ] All endpoints secured with JWT authentication

