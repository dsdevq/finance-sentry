# Research: Google Sign-In via Identity Services (004-adopt-oauth)

**Branch**: `004-adopt-oauth` | **Date**: 2026-04-18 (revised — GSI rewrite)

---

## 1. GSI Credential Flow — How It Works

**Decision**: Use `google.accounts.id.initialize()` + `renderButton()` + `prompt()` (One Tap).

**Flow**:
1. Frontend loads `https://accounts.google.com/gsi/client` script (async, in `index.html`)
2. On auth page init: call `google.accounts.id.initialize({ client_id, callback })`
3. Call `google.accounts.id.renderButton(element, config)` to render the official branded button
4. Optionally call `google.accounts.id.prompt()` for One Tap overlay (returns silently if no eligible session)
5. User selects account → Google calls the JS `callback` with `{ credential: <id_token> }`
6. Frontend POSTs `{ credential }` to `POST /api/v1/auth/google/verify`
7. Backend verifies the ID token → finds/creates user → returns JWT + refresh token

**Why this over Authorization Code flow**:
- No CSRF state management, no server-to-server token exchange
- Official Google-rendered button (branding compliance automatic)
- One Tap included at zero extra cost
- Google deprecated implicit flow; GSI is the current recommended approach for SPAs

**Alternatives considered**:
- Server-side Authorization Code flow — rejected (already implemented, being replaced; more complex, requires client secret)
- Firebase Auth — rejected (adds a full Firebase dependency for a single auth method)

---

## 2. Backend: Google ID Token Verification

**Decision**: Use `Google.Apis.Auth` NuGet package (`GoogleJsonWebSignature.ValidateAsync`).

**How verification works**:
- `Google.Apis.Auth` fetches Google's public JWKS at `https://www.googleapis.com/oauth2/v3/certs`, caches it
- Validates: RS256 signature, `aud` == client ID, `iss` ∈ `{accounts.google.com, https://accounts.google.com}`, `exp` not expired
- Returns a `Payload` with: `Subject` (= stable Google user ID), `Email`, `Name`, `EmailVerified`
- No client secret needed

**Package**: `Google.Apis.Auth` — official Google client library, well-maintained, widely used in .NET

**Alternatives considered**:
- Manual JWT verification using `System.IdentityModel.Tokens.Jwt` — rejected (more code, same outcome)
- `Microsoft.Identity.Web` — rejected (designed for AAD/Entra, not Google)

**Validation settings to enforce**:
```csharp
new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { clientId } }
```

---

## 3. Domain Interface: IGoogleCredentialVerifier

**Decision**: Define `IGoogleCredentialVerifier` in the Application layer; implement `GoogleCredentialVerifier` in Infrastructure.

**Why**: Constitution Principle I mandates domain-defined interfaces for all external integrations.

```csharp
// Application/Interfaces/IGoogleCredentialVerifier.cs
public record GoogleUserInfo(string GoogleId, string Email, string? DisplayName);

public interface IGoogleCredentialVerifier
{
    Task<GoogleUserInfo> VerifyAsync(string credential);
}
```

---

## 4. Frontend: Angular GSI Integration Pattern

**Decision**: Wrap GSI in `AuthService` methods called from login/register component `ngOnInit`. Use `renderButton()` for the official button; call `prompt()` for One Tap.

**Type declarations**: Add `@types/google.accounts` dev dependency.

**Angular-specific considerations**:
- GSI callback fires outside Angular's zone → must call `NgZone.run()` to trigger change detection
- Component stores `@ViewChild` ref for the button container div
- One Tap: call `google.accounts.id.cancel()` in `ngOnDestroy`

**Button rendering config**:
```typescript
google.accounts.id.renderButton(divRef.nativeElement, {
  type: 'standard', shape: 'rectangular',
  theme: 'outline', text: 'continue_with',
  size: 'large', width: 368
});
```

---

## 5. Migration Strategy: Drop OAuthStates Table

**Decision**: Generate `M009_DropOAuthStates` EF Core migration. Previous migration files are preserved.

Steps: remove `DbSet<OAuthState>` + entity config → run `dotnet ef migrations add M009_DropOAuthStates`.

---

## 6. Endpoint Design

**Removed**: `GET /auth/google/login`, `GET /auth/google/callback`

**New**: `POST /api/v1/auth/google/verify`
- Request: `{ "credential": "<google_id_token>" }`
- 200: `{ token, userId, expiresAt }` (same shape as login/register)
- 400: `{ "error": "Invalid Google credential" }`

Must be added to `JwtAuthenticationMiddleware` exempt paths.

---

## 7. Configuration Simplification

**Before**: `ClientId`, `ClientSecret`, `RedirectUri`, `FrontendUrl`
**After**: `ClientId` only

Frontend also needs `ClientId` — added to Angular `environment.ts`.

---

## 8. Contract Tests Strategy

**Deleted**: `GoogleOAuthContractTests.cs`

**New** `GoogleVerifyContractTests.cs`:
- Valid mock credential → 200 + AuthResponse
- Missing credential → 400
- Invalid credential (verifier throws) → 400
- New user → account created, 200
- Existing Google user → authenticated, 200
- Email matches existing email/password user → linked, 200

`IGoogleCredentialVerifier` mocked in `AuthApiFactory`; no real Google calls.

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
