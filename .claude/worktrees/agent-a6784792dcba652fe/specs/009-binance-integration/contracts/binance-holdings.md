# Contract: Binance Holdings Query

**Feature**: 009-binance-integration | **Date**: 2026-04-21

---

## GET /api/v1/crypto/holdings

Returns the user's current Binance crypto holdings (above dust threshold), as stored from the most recent sync.

### Request

```http
GET /api/v1/crypto/holdings
Authorization: Bearer <jwt>
```

### Responses

**200 OK** — holdings returned:
```json
{
  "provider": "binance",
  "syncedAt": "2026-04-21T10:30:00Z",
  "isStale": false,
  "holdings": [
    {
      "asset": "BTC",
      "freeQuantity": 0.5,
      "lockedQuantity": 0.1,
      "usdValue": 30000.00
    },
    {
      "asset": "ETH",
      "freeQuantity": 2.0,
      "lockedQuantity": 0.0,
      "usdValue": 6000.00
    },
    {
      "asset": "USDT",
      "freeQuantity": 500.0,
      "lockedQuantity": 0.0,
      "usdValue": 500.00
    }
  ],
  "totalUsdValue": 36500.00
}
```

`isStale` is `true` when the last successful sync was more than 1 hour ago.

**200 OK** — no Binance account connected:
```json
{
  "provider": "binance",
  "syncedAt": null,
  "isStale": false,
  "holdings": [],
  "totalUsdValue": 0.0
}
```

**401 Unauthorized**:
```json
{
  "error": "Authentication required.",
  "errorCode": "UNAUTHORIZED"
}
```

---

## Integration with GET /api/v1/wealth/summary

When `ICryptoHoldingsReader` is registered, `GET /api/v1/wealth/summary` includes a `"crypto"` category entry:

```json
{
  "totalNetWorth": 57000.00,
  "baseCurrency": "USD",
  "categories": [
    {
      "name": "banking",
      "totalInBaseCurrency": 20500.00,
      "accounts": [...]
    },
    {
      "name": "crypto",
      "totalInBaseCurrency": 36500.00,
      "accounts": [
        {
          "id": "00000000-0000-0000-0000-000000000001",
          "bankName": "Binance",
          "accountType": "crypto",
          "accountNumberLast4": "BTC",
          "provider": "binance",
          "category": "crypto",
          "currency": "USD",
          "nativeBalance": 0.6,
          "balanceInBaseCurrency": 30000.00,
          "syncStatus": "synced"
        }
      ]
    }
  ],
  "appliedFilters": {
    "category": null,
    "provider": null
  }
}
```

Each crypto asset appears as a separate entry in the `accounts` array under the `"crypto"` category. `accountNumberLast4` holds the asset symbol (up to 4 chars visible; full symbol is the intent). `nativeBalance` = total quantity (free + locked). `balanceInBaseCurrency` = USD value.
