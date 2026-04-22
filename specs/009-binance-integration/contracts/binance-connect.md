# Contract: Binance Connect & Disconnect

**Feature**: 009-binance-integration | **Date**: 2026-04-21

---

## POST /api/v1/crypto/binance/connect

Connect a Binance account by submitting API key and secret. Validates credentials with Binance, stores them encrypted, and runs an initial balance sync.

### Request

```http
POST /api/v1/crypto/binance/connect
Authorization: Bearer <jwt>
Content-Type: application/json

{
  "apiKey": "string (required, 64 chars)",
  "apiSecret": "string (required, 64 chars)"
}
```

### Responses

**201 Created** — connection established, initial sync complete:
```json
{
  "message": "Binance account connected successfully.",
  "holdingsCount": 5,
  "syncedAt": "2026-04-21T10:30:00Z"
}
```

**400 Bad Request** — missing or malformed fields:
```json
{
  "error": "apiKey and apiSecret are required.",
  "errorCode": "VALIDATION_ERROR"
}
```

**409 Conflict** — user already has a Binance account connected:
```json
{
  "error": "A Binance account is already connected for this user.",
  "errorCode": "ALREADY_CONNECTED"
}
```

**422 Unprocessable Entity** — credentials rejected by Binance (invalid key, insufficient permissions, etc.):
```json
{
  "error": "Binance rejected the provided credentials. Verify your API key and secret.",
  "errorCode": "INVALID_CREDENTIALS"
}
```

**401 Unauthorized** — missing or invalid JWT:
```json
{
  "error": "Authentication required.",
  "errorCode": "UNAUTHORIZED"
}
```

---

## DELETE /api/v1/crypto/binance/disconnect

Disconnect the user's Binance account. Removes stored credentials and all cached holdings. Stops future sync jobs for this user.

### Request

```http
DELETE /api/v1/crypto/binance/disconnect
Authorization: Bearer <jwt>
```

### Responses

**204 No Content** — disconnected successfully (no body).

**404 Not Found** — no Binance account connected:
```json
{
  "error": "No Binance account is connected for this user.",
  "errorCode": "NOT_FOUND"
}
```

**401 Unauthorized** — missing or invalid JWT.
