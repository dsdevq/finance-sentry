# Contract: IBKR Connect & Disconnect

**Feature**: 010-ibkr-integration | **Date**: 2026-04-22

---

## POST /api/v1/brokerage/ibkr/connect

Connect an Interactive Brokers account by submitting IBKR username and password. Validates credentials via the IB Gateway, discovers the account ID, fetches the current portfolio snapshot, and stores everything securely.

### Request

```http
POST /api/v1/brokerage/ibkr/connect
Authorization: Bearer <jwt>
Content-Type: application/json

{
  "username": "string (required)",
  "password": "string (required)"
}
```

### Responses

**201 Created** — connection established, initial sync complete:
```json
{
  "message": "IBKR account connected successfully.",
  "holdingsCount": 12,
  "connectedAt": "2026-04-22T10:30:00Z"
}
```

**400 Bad Request** — missing or empty credential fields:
```json
{
  "error": "username and password are required.",
  "errorCode": "VALIDATION_ERROR"
}
```

**409 Conflict** — user already has an IBKR account connected:
```json
{
  "error": "An IBKR account is already connected for this user.",
  "errorCode": "ALREADY_CONNECTED"
}
```

**422 Unprocessable Entity** — credentials rejected by IBKR (invalid username/password):
```json
{
  "error": "IBKR rejected the provided credentials. Verify your username and password.",
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

## DELETE /api/v1/brokerage/ibkr/disconnect

Disconnect the user's IBKR account. Removes stored credentials and all cached brokerage holdings. Stops future sync jobs for this user.

### Request

```http
DELETE /api/v1/brokerage/ibkr/disconnect
Authorization: Bearer <jwt>
```

### Responses

**204 No Content** — disconnected successfully (no body).

**404 Not Found** — no IBKR account connected:
```json
{
  "error": "No IBKR account is connected for this user.",
  "errorCode": "NOT_FOUND"
}
```

**401 Unauthorized** — missing or invalid JWT:
```json
{
  "error": "Authentication required.",
  "errorCode": "UNAUTHORIZED"
}
```
