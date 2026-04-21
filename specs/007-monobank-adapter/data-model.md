# Data Model: Monobank Bank Provider Adapter

**Branch**: `007-monobank-adapter` | **Date**: 2026-04-19

---

## New Entities

### MonobankCredential

Stores the user's encrypted Monobank personal API token. One row per user. Referenced by all `BankAccount` rows connected via that token.

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `Id` | `Guid` | PK | |
| `UserId` | `Guid` | NOT NULL, INDEX | Foreign key to AspNetUsers |
| `EncryptedToken` | `byte[]` | NOT NULL | AES-256-GCM encrypted |
| `Iv` | `byte[]` | NOT NULL | Initialisation vector |
| `AuthTag` | `byte[]` | NOT NULL | GCM authentication tag |
| `KeyVersion` | `int` | NOT NULL, DEFAULT 1 | Encryption key version |
| `LastSyncAt` | `DateTime?` | NULL | UTC; used as `from` for incremental sync |
| `CreatedAt` | `DateTime` | NOT NULL, DEFAULT NOW() | |
| `UpdatedAt` | `DateTime?` | NULL | Set on every successful sync |

**Relationships**:
- `MonobankCredential` → `BankAccount`: 1:N (one token, multiple cards/accounts)

**Indexes**:
- `idx_monobank_credential_user_id` on `UserId`
- `idx_monobank_credential_user_unique` UNIQUE on `UserId` (one active token per user)

---

## Modified Entities

### BankAccount (modified)

Two changes:

**1. Rename column** `PlaidItemId` → `ExternalAccountId`

The column stores a provider-opaque external identifier. For Plaid this was the item/account ID. For Monobank it is the account `id` from `/personal/client-info`. The existing UNIQUE index is preserved.

| Column (old) | Column (new) | Change |
|---|---|---|
| `PlaidItemId` | `ExternalAccountId` | Rename only; max length stays 64; UNIQUE constraint stays |

**2. Add column** `Provider`

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `Provider` | `varchar(20)` | NOT NULL, DEFAULT `'plaid'` | `'plaid'` or `'monobank'` |

**3. Add nullable FK** `MonobankCredentialId`

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `MonobankCredentialId` | `Guid?` | NULL, FK → `MonobankCredentials.Id` | NULL for Plaid accounts |

**Navigation property added**: `MonobankCredential? MonobankCredential`

**Relationship**: `BankAccount` → `MonobankCredential`: N:1 (nullable)

---

## Unchanged Entities

- `Transaction` — no changes; populated identically from Monobank statement entries
- `SyncJob` — no changes; records sync attempts for all providers
- `EncryptedCredential` — no changes; remains 1:1 with `BankAccount` for Plaid only
- `AuditLog` — no changes

---

## Migration Summary

Migration `M002_MonobankProvider`:

1. Rename column `BankAccounts.PlaidItemId` → `BankAccounts.ExternalAccountId`
2. Add column `BankAccounts.Provider` VARCHAR(20) NOT NULL DEFAULT 'plaid'
3. Add column `BankAccounts.MonobankCredentialId` UUID NULL
4. Create table `MonobankCredentials` with all columns above
5. Add FK `BankAccounts.MonobankCredentialId` → `MonobankCredentials.Id` ON DELETE SET NULL
6. Add index `idx_monobank_credential_user_id`
7. Add unique index `idx_monobank_credential_user_unique` on `MonobankCredentials.UserId`

---

## IBankProvider Interface (Domain)

New domain interface in `FinanceSentry.Modules.BankSync.Domain`:

```
IBankProvider
  Properties:
    string ProviderName           // "plaid" | "monobank"

  Methods:
    GetAccountsAsync(credential, ct)
      → IReadOnlyList<BankAccountInfo>

    SyncTransactionsAsync(credential, externalAccountId, accountId, userId, since, ct)
      → (IReadOnlyList<TransactionCandidate> Candidates, DateTime? NextSyncFrom)

    DisconnectAsync(credential, ct)
      → Task
```

**`BankAccountInfo`** record (shared across providers):

| Field | Type | Notes |
|---|---|---|
| `ExternalAccountId` | `string` | Provider account ID |
| `Name` | `string` | Display name |
| `AccountType` | `string` | Normalized: `checking`, `savings`, `credit` |
| `AccountNumberLast4` | `string` | Masked PAN last 4 digits |
| `CurrentBalance` | `decimal?` | In account's native currency |
| `Currency` | `string` | ISO 4217 alphabetic (e.g., "UAH") |
| `OwnerName` | `string` | Account holder name |

**`IBankProviderFactory`**:

```
IBankProviderFactory
  Resolve(provider: string) → IBankProvider
```

Registered in DI; resolves `PlaidAdapter` for `"plaid"`, `MonobankAdapter` for `"monobank"`.

---

## Monobank-Specific Interfaces (Infrastructure)

New interface `IMonobankAdapter` in `Infrastructure/Monobank/`:

```
IMonobankAdapter
  ConnectAsync(token, ct)
    → IReadOnlyList<MonobankAccountInfo>    // validates token + returns accounts

  GetAccountsAsync(token, ct)
    → IReadOnlyList<MonobankAccountInfo>    // refreshes balances

  GetStatementsAsync(token, accountId, from, to, ct)
    → IReadOnlyList<MonobankTransaction>   // raw statement entries

  SetWebhookAsync(token, url, ct)          // deferred — stub for now
    → Task
```

`MonobankAdapter` implements both `IMonobankAdapter` and `IBankProvider`.

---

## ISO 4217 Numeric → Alphabetic Currency Map (Adapter Responsibility)

The adapter must maintain a lookup for the currencies Monobank supports:

| Numeric | Code |
|---|---|
| 980 | UAH |
| 840 | USD |
| 978 | EUR |
| 826 | GBP |
| 985 | PLN |
| 756 | CHF |
| 203 | CZK |

Unknown numeric codes fall back to `"UNKNOWN_<code>"`.
