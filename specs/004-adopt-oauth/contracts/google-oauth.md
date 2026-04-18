# REST Contracts: Google OAuth Endpoints (004-adopt-oauth)

**Base path**: `/api/v1/auth/google` — exempt from `JwtAuthenticationMiddleware`

---

## GET /api/v1/auth/google/login

**Purpose**: Initiate Google OAuth Authorization Code flow. Generates CSRF state, stores it, returns a redirect to Google's consent screen.

**Auth**: None required (public endpoint)

**Request**
```
GET /api/v1/auth/google/login
```
No query parameters. No request body.

**Success Response**
```
HTTP 302 Found
Location: https://accounts.google.com/o/oauth2/v2/auth
          ?client_id=<CLIENT_ID>
          &redirect_uri=http%3A%2F%2Flocalhost%3A5000%2Fapi%2Fv1%2Fauth%2Fgoogle%2Fcallback
          &response_type=code
          &scope=openid%20email%20profile
          &state=<random_base64_44chars>
          &access_type=offline
          &prompt=consent
```

**Error Response** (config missing)
```
HTTP 500 Internal Server Error
```

**Contract Test Coverage**: Verify 302, Location starts with `https://accounts.google.com/o/oauth2/v2/auth`, `state` query param is present.

---

## GET /api/v1/auth/google/callback

**Purpose**: Google redirects here after user approves/denies consent. Handles the complete OAuth flow: validate state, exchange code, find/create user, issue JWT + refresh token, redirect to frontend.

**Auth**: None required (public endpoint)

**Request — Success path**
```
GET /api/v1/auth/google/callback?code=<authorization_code>&state=<nonce>
```

**Success Response**
```
HTTP 302 Found
Location: http://localhost:4200/auth/callback?token=<jwt>&userId=<guid>&expiresAt=<iso8601>
Set-Cookie: fs_refresh_token=<token>; HttpOnly; Secure; SameSite=Strict; Path=/; Expires=<30_days>
```

**Request — User cancelled (error=access_denied)**
```
GET /api/v1/auth/google/callback?error=access_denied&state=<nonce>
```

**Cancelled Response**
```
HTTP 302 Found
Location: http://localhost:4200/auth/callback?error=cancelled
```

**Request — Missing/invalid state**
```
GET /api/v1/auth/google/callback?code=<code>
(no state param, or state not found in DB, or state expired/used)
```

**Invalid State Response**
```
HTTP 400 Bad Request
Content-Type: application/json

{ "error": "Invalid or expired OAuth state.", "errorCode": "INVALID_OAUTH_STATE" }
```

**Contract Test Coverage**:
- `GET /callback?error=access_denied&state=<valid>` → 302 to `{FrontendUrl}/auth/callback?error=cancelled`
- `GET /callback` (no state) → 400 with `INVALID_OAUTH_STATE`
- `GET /callback?code=<x>&state=<expired>` → 400
- `GET /login` → 302 Location starts with `https://accounts.google.com`

---

## Unchanged Auth Endpoints

All existing endpoints remain unchanged:

| Endpoint | Change |
|---|---|
| `POST /api/v1/auth/login` | Updated: returns `GOOGLE_ACCOUNT_ONLY` error if user has no password |
| `POST /api/v1/auth/register` | No change |
| `POST /api/v1/auth/refresh` | No change |
| `POST /api/v1/auth/logout` | No change |

---

## Modified Endpoint: POST /api/v1/auth/login (error case)

When a Google-only user attempts password login:

```
POST /api/v1/auth/login
{ "email": "user@gmail.com", "password": "anything" }

HTTP 401 Unauthorized
{
  "error": "This account uses Google sign-in. Please use 'Continue with Google'.",
  "errorCode": "GOOGLE_ACCOUNT_ONLY"
}
```
