# API Contract: Transaction Summary

**Feature**: `008-wealth-aggregation-api`  
**Controller**: `WealthController` (new)

---

## GET /api/v1/wealth/transactions/summary

Returns total debits (expenses) and credits (income) for a specified date window, optionally filtered by provider category or specific provider.

### Request

```http
GET /api/v1/wealth/transactions/summary?from=2026-04-01&to=2026-04-30
Authorization: Bearer <jwt>
```

**Required query parameters**:

| Param | Type | Description |
|-------|------|-------------|
| `from` | string (ISO date) | Start of window, inclusive. Format: `yyyy-MM-dd` |
| `to` | string (ISO date) | End of window, inclusive. Format: `yyyy-MM-dd` |

**Optional query parameters**:

| Param | Type | Description |
|-------|------|-------------|
| `category` | string | Filter to one category: `banking`, `crypto`, `brokerage`, `other` |
| `provider` | string | Filter to one provider: `monobank`, `plaid`, etc. |

### Response — 200 OK

```json
{
  "from": "2026-04-01",
  "to": "2026-04-30",
  "totalDebits": 3420.50,
  "totalCredits": 5100.00,
  "netFlow": 1679.50,
  "categories": [
    {
      "name": "banking",
      "totalDebits": 3420.50,
      "totalCredits": 5100.00,
      "netFlow": 1679.50,
      "transactionCount": 47
    }
  ],
  "appliedFilters": {
    "category": null,
    "provider": null
  }
}
```

### Response — 200 OK (no transactions in window)

```json
{
  "from": "2020-01-01",
  "to": "2020-01-31",
  "totalDebits": 0,
  "totalCredits": 0,
  "netFlow": 0,
  "categories": [],
  "appliedFilters": {
    "category": null,
    "provider": null
  }
}
```

### Response — 400 Bad Request (missing required params)

```json
{
  "error": "Query parameters 'from' and 'to' are required.",
  "errorCode": "MISSING_DATE_RANGE"
}
```

### Response — 400 Bad Request (invalid date range)

```json
{
  "error": "Parameter 'from' must be less than or equal to 'to'.",
  "errorCode": "INVALID_DATE_RANGE"
}
```

### Response — 401 Unauthorized

Standard JWT auth failure — no body.

---

## Notes

- Only **posted** (non-pending), **active** transactions are included in totals.
- Transaction amounts are summed in their **native currency** — no cross-currency conversion applied to transaction totals in this feature. If accounts span multiple currencies, totals reflect mixed currencies.
- `from` and `to` are matched against `PostedDate` if present, falling back to `TransactionDate`.
- The `transactionCount` in each category counts individual transaction rows, not unique merchants.
