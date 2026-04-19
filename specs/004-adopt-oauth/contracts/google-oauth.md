# REST Contracts: Google Sign-In via GSI (004-adopt-oauth)

**Base path**: `/api/v1/auth` — Google endpoints exempt from `JwtAuthenticationMiddleware`

**Revision note**: `GET /google/login` and `GET /google/callback` are **removed**. Replaced by `POST /google/verify`.

---

## REMOVED: GET /api/v1/auth/google/login

Deleted — server-side redirect flow is no longer used.

## REMOVED: GET /api/v1/auth/google/callback

Deleted — no callback needed; credential is sent directly from the browser.

---

## NEW: POST /api/v1/auth/google/verify

**Purpose**: Verify a Google-signed ID token credential received from the GSI client-side library. Creates or authenticates the user and issues a Finance Sentry JWT.

**Auth**: None required (public endpoint, added to `JwtAuthenticationMiddleware` exempt list)

**Request**
```
POST /api/v1/auth/google/verify
Content-Type: application/json

{ "credential": "<google_id_token>" }
```

| Field | Type | Required | Notes |
|---|---|---|---|
| `credential` | `string` | Yes | Google-signed ID token (JWT) from `google.accounts.id` callback |

**Success Response — user authenticated or created**
```
HTTP 200 OK
Content-Type: application/json
Set-Cookie: fs_refresh_token=<token>; HttpOnly; Secure; SameSite=Strict; Path=/; Expires=<30_days>

{
  "token": "<jwt_access_token>",
  "userId": "<guid>",
  "expiresAt": "<iso8601>"
}
```

**Error Response — missing credential**
```
HTTP 400 Bad Request
{ "error": "Credential is required.", "errorCode": "VALIDATION_ERROR" }
```

**Error Response — invalid/expired credential**
```
HTTP 400 Bad Request
{ "error": "Invalid Google credential.", "errorCode": "INVALID_GOOGLE_CREDENTIAL" }
```

**Contract Test Coverage**:
- `POST /google/verify` with valid mock credential → 200 + `{ token, userId, expiresAt }`
- `POST /google/verify` with empty body → 400 `VALIDATION_ERROR`
- `POST /google/verify` with verifier throwing → 400 `INVALID_GOOGLE_CREDENTIAL`
- New user (no existing account) → account created, 200
- Existing Google user → authenticated, 200
- Existing email/password user with matching email → linked + authenticated, 200

---

## Unchanged Auth Endpoints

| Endpoint | Status |
|---|---|
| `POST /api/v1/auth/login` | Unchanged — `GOOGLE_ACCOUNT_ONLY` guard already in place |
| `POST /api/v1/auth/register` | No change |
| `POST /api/v1/auth/refresh` | No change |
| `POST /api/v1/auth/logout` | No change |

---

## Modified Endpoint: POST /api/v1/auth/login (GOOGLE_ACCOUNT_ONLY — unchanged)

```
POST /api/v1/auth/login
{ "email": "user@gmail.com", "password": "anything" }

HTTP 401 Unauthorized
{
  "error": "This account uses Google sign-in. Please use 'Continue with Google'.",
  "errorCode": "GOOGLE_ACCOUNT_ONLY"
}
```
