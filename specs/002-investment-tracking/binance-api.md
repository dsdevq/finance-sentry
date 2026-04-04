# Binance API Integration Contract

**Document Version**: 1.0  
**API Version**: v3  
**Last Updated**: 2026-03-21  
**Base URL**: https://api.binance.com/api/v3  
**Authentication**: HMAC SHA256 (API Key + Secret)

---

## Overview

This document defines the contract for Binance API integration. The system connects to Binance using API keys to fetch account balances, holdings, and market data. All endpoints use HTTPS and require authentication.

---

## Authentication

### Request Headers

```http
X-MBX-APIKEY: {api_key}
```

### Query Parameter Signing

All requests with `timestamp` parameter must be signed:

```
signature = HMACSHA256(query_string, api_secret)
```

### Example

```http
GET /api/v3/account?timestamp=1234567890&signature=abc123def456
X-MBX-APIKEY: your-api-key
```

---

## Endpoints

### 1. Account Information

**Endpoint**: `GET /api/v3/account`

**Purpose**: Fetch user's account details including balances and trading status.

**Query Parameters**:
- `timestamp` (int, required): Current timestamp in milliseconds
- `recvWindow` (int, optional, default: 5000): Request valid for X milliseconds

**Response**:

```json
{
  "makerCommission": 10,
  "takerCommission": 10,
  "buyerCommission": 0,
  "sellerCommission": 0,
  "canTrade": true,
  "canWithdraw": true,
  "canDeposit": true,
  "updateTime": 1234567890,
  "accountType": "SPOT",
  "balances": [
    {
      "asset": "BTC",
      "free": "1.50000000",
      "locked": "0.25000000"
    },
    {
      "asset": "USDT",
      "free": "5000.00000000",
      "locked": "1000.00000000"
    }
  ],
  "permissions": ["SPOT", "MARGIN", "FUTURES"]
}
```

**Success Code**: 200

**Error Codes**:
- `401`: Invalid API key or signature
- `403`: Account restricted or IP not whitelisted
- `429`: Rate limit exceeded (too many requests)
- `418`: IP banned for repeated violations

---

### 2. 24-Hour Ticker

**Endpoint**: `GET /api/v3/ticker/24hr`

**Response (Single Symbol)**:

```json
{
  "symbol": "BTCUSDT",
  "lastPrice": "20500.00",
  "volume": "1000000.00000000",
  "quoteAsset": "USDT"
}
```

**Success Code**: 200

**Error Codes**:
- `400`: Invalid symbol format
- `404`: Symbol not found

---

### 3. Klines (Historical Data)

**Endpoint**: `GET /api/v3/klines`

**Purpose**: Fetch candlestick historical data for trend analysis.

**Query Parameters**:
- `symbol` (string, required): e.g., "BTCUSDT"
- `interval` (string, required): e.g., "1d" (1 day)
- `limit` (int, optional, default: 500, max: 1000): Number of records

**Success Code**: 200

---

## Error Handling & Retry Logic

### Standard Error Response

```json
{
  "code": -2015,
  "msg": "Invalid API-key, IP, or permissions for action"
}
```

### Exponential Backoff

```
retry_wait = min(2^retry_count, 300) seconds
Max retries: 5
```

---

## Rate Limits

- **Per-minute limit**: 1200 weight units per minute
- **Per-second limit**: 50 requests per second

---

## Binance Testnet (Sandbox)

For development/testing:

**Base URL**: https://testnet.binance.vision/api/v3

---

## Implementation Checklist

- [ ] Store API keys encrypted (AES-256-GCM)
- [ ] Implement request signing with HMAC SHA256
- [ ] Handle 401/403 errors → set `account_status = reauth_required`
- [ ] Implement exponential backoff for 429 errors
- [ ] Cache balances for 5 minutes
- [ ] Cache prices for 5 minutes
- [ ] Set request timeout to 10 seconds
- [ ] Validate response JSON structure before mapping to AssetHolding

