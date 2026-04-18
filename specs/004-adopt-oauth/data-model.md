# Data Model: Google OAuth Sign-In (004-adopt-oauth)

**Branch**: `004-adopt-oauth` | **Date**: 2026-04-18

---

## Modified Entity: ApplicationUser

**Location**: `backend/src/FinanceSentry.Modules.Auth/Domain/Entities/ApplicationUser.cs`

**Change**: Add one nullable property. No other modifications to the existing Identity schema.

```
ApplicationUser (extends IdentityUser)
├── [existing Identity columns — unchanged]
└── GoogleId : string?   # Google account sub claim. Null if user registered via email/password.
```

| Field | Type | Nullable | Constraint | Notes |
|---|---|---|---|---|
| `GoogleId` | `nvarchar(255)` | Yes | No unique index (Google sub is globally unique but users may share a DB row for tests) | Populated on first Google sign-in; never overwritten once set |

**Migration**: M007_GoogleId (adds `GoogleId` column to `AspNetUsers`)

---

## New Entity: OAuthState

**Location**: `backend/src/FinanceSentry.Modules.Auth/Domain/Entities/OAuthState.cs`

```
OAuthState
├── Id        : Guid           # PK
├── State     : string         # Cryptographically random nonce (Base64, 32 bytes → 44 chars)
├── ExpiresAt : DateTimeOffset # UtcNow + 10 minutes at creation
├── IsUsed    : bool           # True after the callback has consumed this state
└── CreatedAt : DateTimeOffset # Audit field
```

| Field | Type | Nullable | Constraint |
|---|---|---|---|
| `Id` | `uuid` | No | PK |
| `State` | `nvarchar(64)` | No | UNIQUE index (fast lookup on callback) |
| `ExpiresAt` | `timestamptz` | No | Index (for cleanup queries) |
| `IsUsed` | `bool` | No | Default `false` |
| `CreatedAt` | `timestamptz` | No | Set on insert |

**Lifecycle**:
1. Created in `InitiateGoogleLoginQueryHandler` when user clicks "Continue with Google"
2. Validated + marked `IsUsed = true` in `HandleGoogleCallbackCommandHandler` on callback
3. Expired/used rows accumulate — acceptable for v1; future Hangfire job can purge them

**Migration**: M008_GoogleOAuth (adds `OAuthStates` table)

---

## AuthDbContext Changes

```
AuthDbContext
├── [existing] RefreshTokens  : DbSet<RefreshToken>
└── [new]      OAuthStates    : DbSet<OAuthState>
```

Configuration in `OnModelCreating`:
- Unique index on `OAuthState.State`
- Index on `OAuthState.ExpiresAt`

---

## Migration Sequence

| # | Name | Change |
|---|---|---|
| M005 | IdentitySchema | ASP.NET Identity tables |
| M006 | RefreshTokens | RefreshTokens table |
| M007 | GoogleId | `GoogleId` column on `AspNetUsers` |
| M008 | GoogleOAuth | `OAuthStates` table |

**Note**: M007 and M008 are separate migrations matching the task sequence (T001 → T004).
