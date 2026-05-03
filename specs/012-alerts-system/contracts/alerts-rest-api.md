# REST API Contract: Alerts System (012)

**Base path**: `/api/v1/alerts`
**Auth**: All endpoints require `Authorization: Bearer <token>` (enforced by `JwtAuthenticationMiddleware`)
**Controller**: `AlertsController` in `FinanceSentry.Modules.Alerts.API.Controllers`

---

## GET /api/v1/alerts

Fetch the authenticated user's alerts, excluding dismissed ones, sorted by most recent first.

### Query Parameters

| Param | Type | Required | Default | Description |
|---|---|---|---|---|
| `filter` | string | No | `all` | `all` \| `unread` \| `error` \| `warning` \| `info` |
| `page` | int | No | `1` | Page number (1-based) |
| `pageSize` | int | No | `20` | Max 100 |

### Response `200 OK`

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "type": "LowBalance",
      "severity": "Warning",
      "title": "Low balance on Chase Checking",
      "message": "Your Chase Checking balance ($320.00) has dropped below your $500.00 threshold.",
      "referenceId": "b2c4f321-...",
      "referenceLabel": "Chase Checking",
      "isRead": false,
      "isResolved": false,
      "createdAt": "2026-05-01T14:23:00Z",
      "resolvedAt": null
    }
  ],
  "totalCount": 12,
  "unreadCount": 4,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

### Error Responses

| Status | `errorCode` | Condition |
|---|---|---|
| `401` | `UNAUTHORIZED` | Missing or invalid token |

---

## GET /api/v1/alerts/unread-count

Lightweight endpoint for the sidebar badge. Returns only the unread count.

### Response `200 OK`

```json
{
  "count": 4
}
```

---

## PATCH /api/v1/alerts/{id}/read

Mark a single alert as read.

### Path Parameters

| Param | Type | Description |
|---|---|---|
| `id` | UUID | Alert identifier |

### Response `204 No Content`

Empty body on success.

### Error Responses

| Status | `errorCode` | Condition |
|---|---|---|
| `401` | `UNAUTHORIZED` | Missing or invalid token |
| `404` | `ALERT_NOT_FOUND` | Alert does not exist or belongs to a different user |

---

## PATCH /api/v1/alerts/read-all

Mark all of the user's unread alerts as read.

### Response `204 No Content`

Empty body on success.

### Error Responses

| Status | `errorCode` | Condition |
|---|---|---|
| `401` | `UNAUTHORIZED` | Missing or invalid token |

---

## DELETE /api/v1/alerts/{id}

Dismiss (permanently hide) an alert. Sets `is_dismissed = true`.

### Path Parameters

| Param | Type | Description |
|---|---|---|
| `id` | UUID | Alert identifier |

### Response `204 No Content`

Empty body on success.

### Error Responses

| Status | `errorCode` | Condition |
|---|---|---|
| `401` | `UNAUTHORIZED` | Missing or invalid token |
| `404` | `ALERT_NOT_FOUND` | Alert does not exist or belongs to a different user |

---

## Error Body Schema

```json
{
  "errorCode": "ALERT_NOT_FOUND",
  "message": "Alert not found."
}
```

---

## New `errorCode` Values (add to `error-messages.registry.ts`)

| Code | Frontend Message |
|---|---|
| `ALERT_NOT_FOUND` | "Alert not found." |
