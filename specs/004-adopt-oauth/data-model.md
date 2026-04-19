# Data Model: Google Sign-In via Identity Services (004-adopt-oauth)

**Branch**: `004-adopt-oauth` | **Date**: 2026-04-18 (revised — GSI rewrite)

---

## Modified Entity: ApplicationUser

**Location**: `backend/src/FinanceSentry.Modules.Auth/Domain/Entities/ApplicationUser.cs`

**Change**: Retains the `GoogleId` property added in M007. No additional schema changes needed.

```
ApplicationUser (extends IdentityUser)
├── [existing Identity columns — unchanged]
└── GoogleId : string?   # Google account sub claim. Null if user has no Google link.
```

| Field | Type | Nullable | Constraint | Notes |
|---|---|---|---|---|
| `GoogleId` | `nvarchar(255)` | Yes | — | Populated on first Google sign-in via GSI; stable identifier |

---

## Removed Entity: OAuthState *(dropped in this feature)*

The `OAuthState` table (added by M008) is dropped because GSI handles CSRF protection client-side. The `OAuthState.cs` entity file and all associated code are deleted.

---

## AuthDbContext Changes

```
AuthDbContext (after this feature)
└── [existing] RefreshTokens : DbSet<RefreshToken>    ← unchanged
    [REMOVED]  OAuthStates                            ← dropped by M009
```

---

## Migration Sequence

| # | Name | Change | Status |
|---|---|---|---|
| M005 | IdentitySchema | ASP.NET Identity tables | existing |
| M006 | RefreshTokens | RefreshTokens table | existing |
| M007 | GoogleId | `GoogleId` column on `AspNetUsers` | existing — kept |
| M008 | GoogleOAuth | `OAuthStates` table | existing — superseded |
| M009 | DropOAuthStates | Drops `OAuthStates` table | **new in this feature** |

**Note**: M008 and M009 are inverse migrations — M008 creates the table, M009 drops it. Both are preserved in migration history for auditability.
