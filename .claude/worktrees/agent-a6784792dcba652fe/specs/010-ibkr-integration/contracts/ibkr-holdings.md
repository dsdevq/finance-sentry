# Contract: Brokerage Holdings Query

**Feature**: 010-ibkr-integration | **Date**: 2026-04-22

---

## GET /api/v1/brokerage/holdings

Returns the user's current IBKR portfolio positions as stored from the most recent sync. Includes all positions regardless of instrument type; positions with zero value are included with `usdValue: 0`.

### Request

```http
GET /api/v1/brokerage/holdings
Authorization: Bearer <jwt>
```

### Responses

**200 OK** — holdings returned:
```json
{
  "provider": "ibkr",
  "syncedAt": "2026-04-22T10:30:00Z",
  "isStale": false,
  "positions": [
    {
      "symbol": "AAPL",
      "instrumentType": "STK",
      "quantity": 100.0,
      "usdValue": 17500.00
    },
    {
      "symbol": "MSFT",
      "instrumentType": "STK",
      "quantity": 50.0,
      "usdValue": 21000.00
    },
    {
      "symbol": "VXUS",
      "instrumentType": "FUND",
      "quantity": 200.0,
      "usdValue": 12000.00
    }
  ],
  "totalUsdValue": 50500.00
}
```

`isStale` is `true` when the last successful sync was more than 1 hour ago.

**200 OK** — no IBKR account connected:
```json
{
  "provider": "ibkr",
  "syncedAt": null,
  "isStale": false,
  "positions": [],
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

When `IBrokerageHoldingsReader` is registered, `GET /api/v1/wealth/summary` includes a `"brokerage"` category entry:

```json
{
  "totalNetWorth": 91000.00,
  "baseCurrency": "USD",
  "categories": [
    {
      "name": "banking",
      "totalInBaseCurrency": 20500.00,
      "accounts": [...]
    },
    {
      "name": "crypto",
      "totalInBaseCurrency": 20000.00,
      "accounts": [...]
    },
    {
      "name": "brokerage",
      "totalInBaseCurrency": 50500.00,
      "accounts": [
        {
          "id": "00000000-0000-0000-0000-000000000001",
          "bankName": "IBKR",
          "accountType": "brokerage",
          "accountNumberLast4": "AAPL",
          "provider": "ibkr",
          "category": "brokerage",
          "currency": "USD",
          "nativeBalance": 100.0,
          "balanceInBaseCurrency": 17500.00,
          "syncStatus": "synced"
        },
        {
          "id": "00000000-0000-0000-0000-000000000002",
          "bankName": "IBKR",
          "accountType": "brokerage",
          "accountNumberLast4": "MSFT",
          "provider": "ibkr",
          "category": "brokerage",
          "currency": "USD",
          "nativeBalance": 50.0,
          "balanceInBaseCurrency": 21000.00,
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

Each brokerage position appears as a separate entry in the `accounts` array under the `"brokerage"` category. `accountNumberLast4` holds the instrument symbol. `nativeBalance` = position quantity. `balanceInBaseCurrency` = USD value.

The `category=brokerage` query parameter on the wealth summary endpoint filters to only the brokerage category.
