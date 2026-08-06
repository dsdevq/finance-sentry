# REST API Contract: Net Worth History (015)

**Base path**: `/api/v1/net-worth`
**Auth**: All endpoints require `Authorization: Bearer <token>`
**Controller**: `NetWorthHistoryController` in `FinanceSentry.Modules.NetWorthHistory.API.Controllers`

---

## GET /api/v1/net-worth/history

Returns the authenticated user's net worth snapshots for the selected date range.

### Query Parameters

| Param | Type | Required | Default | Notes |
|---|---|---|---|---|
| `range` | string | No | `1y` | One of: `3m`, `6m`, `1y`, `all` |

### Response `200 OK`

```json
{
  "snapshots": [
    {
      "snapshotDate": "2025-06-30",
      "bankingTotal": 18400.00,
      "brokerageTotal": 95200.00,
      "cryptoTotal": 9800.00,
      "totalNetWorth": 123400.00,
      "currency": "USD"
    },
    {
      "snapshotDate": "2025-07-31",
      "bankingTotal": 19100.00,
      "brokerageTotal": 97600.00,
      "cryptoTotal": 11200.00,
      "totalNetWorth": 127900.00,
      "currency": "USD"
    }
  ],
  "hasHistory": true,
  "range": "1y"
}
```

**Notes**:
- `snapshots` is ordered by `snapshotDate` ascending (oldest first).
- `hasHistory`: `false` when no snapshots exist yet (brand-new user, no job has run).
- When `hasHistory` is `false`, `snapshots` is an empty array.
- Gaps in months (job failure) appear as missing entries — no interpolation.
- `range=all` returns all snapshots for the user with no date limit.

### Range Mapping

| `range` | Lookback | Notes |
|---|---|---|
| `3m` | 3 months from today | Returns up to 3 snapshots |
| `6m` | 6 months from today | Returns up to 6 snapshots |
| `1y` | 12 months from today | Returns up to 12 snapshots |
| `all` | No limit | Returns all available snapshots |

### Error Responses

| Status | `errorCode` | Condition |
|---|---|---|
| `400` | `INVALID_RANGE` | `range` value is not one of the allowed values |
| `401` | `UNAUTHORIZED` | Missing or invalid token |

---

## Error Body Schema

```json
{
  "errorCode": "INVALID_RANGE",
  "message": "Invalid range. Allowed values: 3m, 6m, 1y, all."
}
```

---

## New `errorCode` Values (add to `error-messages.registry.ts`)

| Code | Frontend Message |
|---|---|
| `INVALID_RANGE` | "Invalid date range selection." |
