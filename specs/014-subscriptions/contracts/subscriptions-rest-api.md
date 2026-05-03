# REST API Contract: Subscriptions Detection (014)

**Base path**: `/api/v1/subscriptions`
**Auth**: All endpoints require `Authorization: Bearer <token>`
**Controller**: `SubscriptionsController` in `FinanceSentry.Modules.Subscriptions.API.Controllers`

---

## GET /api/v1/subscriptions

List the authenticated user's detected subscriptions, excluding dismissed ones by default.

### Query Parameters

| Param | Type | Required | Default | Notes |
|---|---|---|---|---|
| `includeDismissed` | bool | No | `false` | Include dismissed subscriptions in results |

### Response `200 OK`

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "merchantName": "Netflix",
      "cadence": "monthly",
      "averageAmount": 15.49,
      "lastKnownAmount": 15.49,
      "monthlyEquivalent": 15.49,
      "currency": "USD",
      "lastChargeDate": "2026-04-18",
      "nextExpectedDate": "2026-05-18",
      "status": "active",
      "occurrenceCount": 8,
      "category": "entertainment"
    }
  ],
  "totalCount": 5,
  "hasInsufficientHistory": false
}
```

**Notes**:
- `monthlyEquivalent`: for `monthly` cadence = `averageAmount`; for `annual` cadence = `averageAmount / 12`
- `hasInsufficientHistory`: `true` if user has < 3 months of transaction history (FR-011)
- `status` values: `active`, `dismissed`, `potentially_cancelled`

### Error Responses

| Status | `errorCode` | Condition |
|---|---|---|
| `401` | `UNAUTHORIZED` | Missing or invalid token |

---

## GET /api/v1/subscriptions/summary

Total monthly cost and active count across all active subscriptions.

### Response `200 OK`

```json
{
  "totalMonthlyEstimate": 87.43,
  "totalAnnualEstimate": 1049.16,
  "activeCount": 6,
  "potentiallyCancelledCount": 1,
  "currency": "USD"
}
```

### Error Responses

| Status | `errorCode` | Condition |
|---|---|---|
| `401` | `UNAUTHORIZED` | Missing or invalid token |

---

## PATCH /api/v1/subscriptions/{id}/dismiss

Dismiss a detected subscription as a false positive.

### Path Parameters

| Param | Type | Description |
|---|---|---|
| `id` | UUID | Subscription identifier |

### Response `204 No Content`

### Error Responses

| Status | `errorCode` | Condition |
|---|---|---|
| `404` | `SUBSCRIPTION_NOT_FOUND` | Subscription not found or belongs to different user |
| `401` | `UNAUTHORIZED` | Missing or invalid token |

---

## PATCH /api/v1/subscriptions/{id}/restore

Restore a previously dismissed subscription to active status.

### Path Parameters

| Param | Type | Description |
|---|---|---|
| `id` | UUID | Subscription identifier |

### Response `204 No Content`

### Error Responses

| Status | `errorCode` | Condition |
|---|---|---|
| `404` | `SUBSCRIPTION_NOT_FOUND` | Subscription not found or not dismissed |
| `401` | `UNAUTHORIZED` | Missing or invalid token |

---

## Error Body Schema

```json
{
  "errorCode": "SUBSCRIPTION_NOT_FOUND",
  "message": "Subscription not found."
}
```

---

## New `errorCode` Values (add to `error-messages.registry.ts`)

| Code | Frontend Message |
|---|---|
| `SUBSCRIPTION_NOT_FOUND` | "Subscription not found." |
