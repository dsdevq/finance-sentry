# API Contract: Wealth Summary

**Feature**: `008-wealth-aggregation-api`  
**Controller**: `WealthController` (new)

---

## GET /api/v1/wealth/summary

Returns the user's total net worth across all connected accounts, broken down by provider category. Supports optional filtering by category or provider.

### Request

```http
GET /api/v1/wealth/summary
Authorization: Bearer <jwt>
```

**Optional query parameters**:

| Param | Type | Description |
|-------|------|-------------|
| `category` | string | Filter to one category: `banking`, `crypto`, `brokerage`, `other` |
| `provider` | string | Filter to one provider: `monobank`, `plaid`, `binance`, etc. |

`category` and `provider` are mutually exclusive filters. If both are supplied, `provider` takes precedence.

### Response — 200 OK (full snapshot, no filters)

```json
{
  "totalNetWorth": 12534.72,
  "baseCurrency": "USD",
  "categories": [
    {
      "name": "banking",
      "totalInBaseCurrency": 12534.72,
      "accounts": [
        {
          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "bankName": "Chase",
          "accountType": "checking",
          "accountNumberLast4": "1234",
          "provider": "plaid",
          "category": "banking",
          "currency": "USD",
          "nativeBalance": 8200.00,
          "balanceInBaseCurrency": 8200.00,
          "syncStatus": "active"
        },
        {
          "id": "4aa96f75-6828-5673-c4gd-3d074g77bfb7",
          "bankName": "Monobank",
          "accountType": "checking",
          "accountNumberLast4": "5678",
          "provider": "monobank",
          "category": "banking",
          "currency": "UAH",
          "nativeBalance": 180000.00,
          "balanceInBaseCurrency": 4334.72,
          "syncStatus": "active"
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

### Response — 200 OK (filtered by category)

```http
GET /api/v1/wealth/summary?category=banking
```

```json
{
  "totalNetWorth": 12534.72,
  "baseCurrency": "USD",
  "categories": [
    {
      "name": "banking",
      "totalInBaseCurrency": 12534.72,
      "accounts": [ ... ]
    }
  ],
  "appliedFilters": {
    "category": "banking",
    "provider": null
  }
}
```

### Response — 200 OK (no matching accounts)

```json
{
  "totalNetWorth": 0,
  "baseCurrency": "USD",
  "categories": [],
  "appliedFilters": {
    "category": "crypto",
    "provider": null
  }
}
```

### Response — 401 Unauthorized

Standard JWT auth failure — no body.

### Response — 400 Bad Request (invalid filter)

```json
{
  "error": "Invalid category value. Allowed: banking, crypto, brokerage, other.",
  "errorCode": "INVALID_FILTER"
}
```
