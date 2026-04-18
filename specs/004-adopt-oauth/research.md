# Research: Google OAuth Sign-In (004-adopt-oauth)

**Branch**: `004-adopt-oauth` | **Date**: 2026-04-18

---

## Decision 1: OAuth Flow Type — Authorization Code (Server-Side)

**Decision**: Use OAuth 2.0 Authorization Code flow, server-side. Backend owns the full OAuth dance; Angular never sees a Google token.

**Rationale**: The implicit flow is deprecated by Google (2019). Server-side Authorization Code flow keeps secrets on the server, protects against token interception in browser history/logs, and aligns with the existing server-issued JWT pattern in 003-auth-flow.

**Alternatives considered**:
- PKCE / SPA-side flow — rejected: requires exposing client_id in Angular; token then lives in JS memory; harder to issue a server-side httpOnly refresh cookie.
- Google Sign-In JS library (GSI) — rejected: Google One Tap is out of scope per spec; would require complex client/server token handoff.

---

## Decision 2: No New NuGet Packages Needed

**Decision**: Implement `GoogleOAuthService` using `IHttpClientFactory` (named client) and `System.Text.Json`. No `Google.Apis.Auth` package.

**Rationale**: The two Google endpoints needed (`/token` + `/userinfo`) are simple JSON HTTP calls. Adding the full Google client library (~5 MB) for two HTTP calls is disproportionate. `HttpClientFactory` is already in the DI container.

**Alternatives considered**:
- `Google.Apis.Auth` NuGet — rejected: heavyweight; pulls in Google.Apis.Core, Newtonsoft.Json compatibility shims; adds unnecessary surface area.

---

## Decision 3: CSRF Protection via Server-Side OAuthState Entity

**Decision**: Generate a cryptographically random `state` value, persist it as an `OAuthState` row in PostgreSQL (with 10-minute TTL and `IsUsed` flag), and validate it on callback.

**Rationale**: The state parameter is the standard OAuth CSRF mitigation. Storing it server-side (vs. a cookie or session) is simpler in a stateless JWT setup where there is no server-side session to correlate against. The table is tiny and short-lived.

**Alternatives considered**:
- HMAC-signed state cookie — rejected: adds complexity; needs cookie signing key management; harder to mark as "used" after one-time consumption.

---

## Decision 4: Account Linking Strategy

**Decision**: Match on `GoogleId` (sub) first; fall back to `NormalizedEmail` match; auto-link on email match (set `GoogleId` on the existing record). No manual linking UI.

**Rationale**: Matching by `sub` is stable and prevents account takeover via email address reuse. Email fallback covers the common case where a user registered with email/password using the same Gmail address. Automatic linking avoids friction for v1 (personal app, single user).

**Alternatives considered**:
- Email match only — rejected: if user's Google account email changes, the link breaks; `sub` is permanent.
- Require explicit confirmation step — rejected: out of scope for v1 per spec.

---

## Decision 5: Frontend Callback Handling

**Decision**: Backend redirects to `{FrontendUrl}/auth/callback?token=<jwt>&userId=<id>&expiresAt=<iso>` after successful OAuth. Angular reads query params in `OAuthCallbackComponent` and stores them exactly as email/password login does.

**Rationale**: Reuses the existing `AuthService` token storage logic. No new localStorage key schema. The `OAuthCallbackComponent` is a thin adapter between the URL params and the existing `handleOAuthCallback()` method.

**Alternatives considered**:
- Post-redirect via form — rejected: more complex; anti-pattern in modern SPAs.
- Cookie-only (no query params) — rejected: Angular needs the token in localStorage to attach the `Authorization` header; httpOnly cookie alone is for refresh token only.

---

## Decision 6: Google HttpClient Base Address

**Decision**: Register two named HttpClients: `"google-auth"` (base `https://oauth2.googleapis.com`) and `"google-api"` (base `https://www.googleapis.com`), OR a single client with no base address and full URIs in the service.

**Final choice**: Single named client `"google"` with no base address and full URIs in `GoogleOAuthService`. Avoids confusion from two registered names for what is one conceptual integration.

---

## Resolved: No Unknowns Remaining

All NEEDS CLARIFICATION items from the spec have been resolved above. Implementation can proceed directly from tasks.md.
