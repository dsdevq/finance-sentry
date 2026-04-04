# Interactive Brokers API Integration Contract

**Document Version**: 1.0  
**API Version**: Gateway/REST  
**Last Updated**: 2026-03-21  
**Base URL**: https://localhost:5000/v1 (Client Portal API) OR https://api.interactivebrokers.com (Gateway)  
**Authentication**: OAuth 2.0 + Session Token

---

## Overview

Interactive Brokers (IB) provides multiple API options. This integration uses the **IB Gateway REST API** for account data and positions. All requests require authentication via session token obtained through OAuth or automated login.

### Available APIs
- **IB Gateway**: REST API, runs locally, requires IB Trader Workstation/Gateway
- **Web API**: Deprecated, not used
- **TWS API**: Java/C++, complex (not used for this integration)

---

## Authentication

### Session Management

Interactive Brokers uses session-based authentication:

1. **Obtain Session Token** (OAuth or automated login)
2. **Include token in requests**
3. **Validate token expiry** and refresh if needed

### OAuth Flow (Recommended for Production)

```
1. Redirect user to: https://login.interactivebrokers.com/sso/Login
   
2. Receive authorization_code
   
3. Exchange for access_token:
   POST https://api.interactivebrokers.com/oauth/token
   Headers: Content-Type: application/x-www-form-urlencoded
   Body: grant_type=authorization_code&code={code}&client_id={id}&client_secret={secret}
   
4. Response:
   {
     "access_token": "...",
     "token_type": "Bearer",
     "expires_in": 3600
   }
```

---

## Endpoints

### 1. Get Accounts

**Endpoint**: `GET /v1/api/portfolio/accounts`

**Purpose**: Fetch list of IB accounts associated with the user.

**Headers**:
```http
Authorization: Bearer {access_token}
```

**Response**:

```json
{
  "accounts": [
    {
      "accountId": "DU123456",
      "accountType": "INDIVIDUAL",
      "accountAlias": "Main Trading Account",
      "accountStatus": "Active"
    }
  ]
}
```

**Success Code**: 200

**Error Codes**:
- `401`: Unauthorized / token expired
- `403`: Access denied

---

### 2. Get Account Summary

**Endpoint**: `GET /v1/api/portfolio/{accountId}/summary`

**Purpose**: Fetch account summary including balance, buying power, portfolio value.

**URL Parameters**:
- `accountId` (string): Account ID (e.g., "DU123456")

**Headers**:
```http
Authorization: Bearer {access_token}
```

**Response**:

```json
{
  "accountId": "DU123456",
  "totalCashValue": 50000.00,
  "netLiquidation": 500000.00,
  "buyingPower": 100000.00,
  "currency": "USD"
}
```

**Field Mapping**:
```
netLiquidation → Total Portfolio Value
buyingPower → Available for new trades
totalCashValue → Cash balance
```

**Cache Strategy**: Cache for 15 minutes

---

### 3. Get Portfolio Positions

**Endpoint**: `GET /v1/api/portfolio/{accountId}/positions/{pageId}`

**Purpose**: Fetch all positions (holdings) in account. Paginated (30 positions per page).

**URL Parameters**:
- `accountId` (string): Account ID
- `pageId` (int): Page number (0-indexed)

**Response**:

```json
{
  "pageId": 0,
  "positions": [
    {
      "conId": 265598,
      "contractDetails": {
        "symbol": "AAPL",
        "secType": "STK",
        "exchange": "NASDAQ",
        "currency": "USD"
      },
      "position": 100,
      "marketPrice": 150.25,
      "marketValue": 15025.00,
      "averageCost": 145.00,
      "unrealizedPL": 525.00,
      "unrealizedPLPercent": 3.50
    }
  ],
  "pageCount": 2
}
```

**Field Mapping**:
```
symbol → Asset symbol
position → Quantity held
marketPrice → Current price
marketValue → Total value
averageCost → Cost basis per unit
unrealizedPL → Gain/loss $
```

**Success Code**: 200

**Pagination**: Loop through all pages if `pageCount > 1`

---

## Error Handling & Retry Logic

### Standard Error Response

```json
{
  "error": "Unauthorized",
  "message": "Session token expired or invalid",
  "code": 401
}
```

### Error Codes & Handling

| Code | Message | Retry? | Strategy |
|------|---------|--------|----------|
| 401 | Unauthorized / token expired | Yes | Refresh token, retry once |
| 403 | Access denied | No | Log error |
| 404 | Not found | No | Verify account ID |
| 429 | Too many requests | Yes | Exponential backoff |
| 500 | Server error | Yes | Exponential backoff (max 3 retries) |

---

## Rate Limits

- **Per-second limit**: 100 requests per second per account
- **Per-hour limit**: 10,000 requests per hour

---

## IB Gateway Setup (Development)

### Docker Compose

```yaml
version: '3.8'
services:
  ibgateway:
    image: ghcr.io/waytrade/ib-gateway:stable
    ports:
      - "5000:5000"
```

---

## IB Demo Account Setup

Interactive Brokers provides free demo accounts for testing:

1. Visit https://www.interactivebrokers.com/en/index.php?f=45631
2. Create a demo account
3. Use demo credentials in development
4. Switch to live in production

---

## Implementation Checklist

- [ ] Store OAuth tokens securely (encrypted at rest)
- [ ] Implement token refresh logic (refresh 5 minutes before expiry)
- [ ] Handle 401 errors → refresh token and retry
- [ ] Cache account summary for 15 minutes
- [ ] Cache positions for 15 minutes
- [ ] Paginate through all positions
- [ ] Set request timeout to 15 seconds
- [ ] Log all API responses for debugging
- [ ] Validate response JSON structure before mapping

