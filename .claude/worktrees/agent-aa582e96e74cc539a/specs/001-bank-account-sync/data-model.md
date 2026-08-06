# Data Model: Bank Account Sync Feature

**Phase**: 1  
**Last Updated**: 2026-03-21  
**Status**: Design Complete

---

## Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  User (existing, not in scope)                                  │
│  ├─ user_id (PK)                                               │
│  └─ encrypted_email                                            │
│       │                                                         │
│       ├─────────┬──────────────────┬──────────────────┐        │
│       │         │                  │                  │        │
│       ▼         ▼                  ▼                  ▼        │
│   BankAccount  Transaction    SyncJob        EncryptedCredential
│   (1:N)        (N:1)          (N:1)          (1:1)            │
│                                                                 │
│   Represents    Represents      Represents      Securely stores│
│   connected     individual      sync attempt    bank auth      │
│   bank account  transaction     metadata        credentials    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Entity Definitions

### 1. BankAccount

Represents a user's connected bank account. One row per institution/account pair.

**Attributes**:

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `account_id` | UUID | PK, NOT NULL | Unique identifier (system-generated) |
| `user_id` | UUID | FK, NOT NULL, Indexed | Foreign key to User table; enables user-scoped queries |
| `plaid_item_id` | VARCHAR(24) | UNIQUE, NOT NULL | Opaque token from Plaid (safe to store; never raw credentials) |
| `bank_name` | VARCHAR(255) | NOT NULL | E.g., "AIB Ireland", "Monobank", "Wise" |
| `account_type` | ENUM | NOT NULL | Values: `checking`, `savings`, `credit`, `investment` |
| `account_number_last4` | CHAR(4) | NOT NULL | Last 4 digits only (PCI compliance) |
| `owner_name` | VARCHAR(255) | NOT NULL | Account holder name from bank |
| `currency` | CHAR(3) | NOT NULL | ISO 4217 code (EUR, GBP, UAH, USD); immutable per account |
| `current_balance` | DECIMAL(19,4) | NOT NULL, Default: 0 | Latest known balance in account currency |
| `available_balance` | DECIMAL(19,4) | Nullable | Available balance excluding pending holds |
| `sync_status` | ENUM | NOT NULL, Default: `pending` | State machine values: `pending`, `syncing`, `active`, `failed`, `reauth_required` |
| `last_sync_timestamp` | TIMESTAMPTZ | Nullable | UTC timestamp of most recent successful sync |
| `last_sync_duration_ms` | INTEGER | Nullable | Duration of last sync in milliseconds |
| `is_active` | BOOLEAN | NOT NULL, Default: true | Soft delete flag |
| `created_at` | TIMESTAMPTZ | NOT NULL, Default: CURRENT_TIMESTAMP | Account connection timestamp |
| `updated_at` | TIMESTAMPTZ | NOT NULL, Default: CURRENT_TIMESTAMP | Last modification timestamp |

**Indexes**:

```sql
CREATE INDEX idx_bank_accounts_user_id ON bank_accounts(user_id);
CREATE INDEX idx_bank_accounts_plaid_item_id ON bank_accounts(plaid_item_id);
CREATE INDEX idx_bank_accounts_user_sync_status ON bank_accounts(user_id, sync_status)
    WHERE is_active = true;
```

---

### 2. Transaction

Represents a single bank transaction. Immutable after creation (audit trail).

**Attributes**:

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `transaction_id` | UUID | PK, NOT NULL | System-generated identifier |
| `account_id` | UUID | FK, NOT NULL, Indexed | Foreign key to BankAccount |
| `user_id` | UUID | FK, NOT NULL, Indexed (denormalized) | Denormalized for query performance |
| `plaid_transaction_id` | VARCHAR(255) | NOT NULL | Transaction ID from Plaid |
| `amount` | DECIMAL(19,4) | NOT NULL | Transaction amount (always positive) |
| `transaction_type` | ENUM | NOT NULL | Values: `debit`, `credit`, `transfer` |
| `currency` | CHAR(3) | NOT NULL | ISO 4217 code; must match account currency |
| `posted_date` | DATE | Nullable | Date when transaction settled |
| `pending_date` | DATE | Nullable | Date when pending transaction initiated |
| `is_pending` | BOOLEAN | NOT NULL, Default: false | true = auth hold, false = settled |
| `description` | TEXT | NOT NULL | Merchant/description text |
| `merchant_category` | VARCHAR(100) | Nullable | Category inferred by Plaid |
| `unique_hash` | CHAR(64) | NOT NULL, UNIQUE | HMAC-SHA256 for deduplication |
| `synced_at` | TIMESTAMPTZ | NOT NULL | Timestamp when fetched from bank |
| `created_at` | TIMESTAMPTZ | NOT NULL, Default: CURRENT_TIMESTAMP | Insertion timestamp |

**Indexes**:

```sql
CREATE INDEX idx_transactions_user_account_date 
    ON transactions(user_id, account_id, posted_date DESC, pending_date DESC);
CREATE UNIQUE INDEX idx_transactions_unique_hash ON transactions(unique_hash);
CREATE INDEX idx_transactions_description_gin 
    ON transactions USING GIN(description gin_trgm_ops);
CREATE INDEX idx_transactions_currency 
    ON transactions(user_id, currency, posted_date DESC);
```

---

### 3. SyncJob

Audit trail for all sync attempts (successful and failed).

**Attributes**:

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `sync_job_id` | UUID | PK, NOT NULL | System-generated identifier |
| `account_id` | UUID | FK, NOT NULL, Indexed | Foreign key to BankAccount |
| `user_id` | UUID | FK, NOT NULL, Indexed (denormalized) | For filtering by user |
| `correlation_id` | VARCHAR(64) | NOT NULL, Indexed | Unique per sync; enables tracing |
| `started_at` | TIMESTAMPTZ | NOT NULL | When sync initiated |
| `completed_at` | TIMESTAMPTZ | Nullable | When sync completed |
| `status` | ENUM | NOT NULL | Values: `pending`, `in_progress`, `success`, `failed`, `circuit_breaker_open`, `partial_failure` |
| `error_code` | VARCHAR(50) | Nullable | Plaid error code |
| `error_message` | TEXT | Nullable | Human-readable error details |
| `transaction_count_fetched` | INTEGER | NOT NULL, Default: 0 | Number of new transactions imported |
| `transaction_count_deduped` | INTEGER | NOT NULL, Default: 0 | Number of duplicates filtered |
| `retry_count` | SMALLINT | NOT NULL, Default: 0 | Number of retry attempts |
| `http_status_code` | SMALLINT | Nullable | HTTP status from Plaid API |
| `webhook_triggered` | BOOLEAN | NOT NULL, Default: false | true = webhook triggered sync |

**Indexes**:

```sql
CREATE INDEX idx_sync_jobs_account_id ON sync_jobs(account_id);
CREATE INDEX idx_sync_jobs_user_id ON sync_jobs(user_id);
CREATE INDEX idx_sync_jobs_correlation_id ON sync_jobs(correlation_id);
CREATE INDEX idx_sync_jobs_status_created ON sync_jobs(status, started_at DESC);
```

---

### 4. EncryptedCredential

Stores Plaid access tokens (encrypted). Never stores raw passwords.

**Attributes**:

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `credential_id` | UUID | PK, NOT NULL | System-generated identifier |
| `account_id` | UUID | FK, NOT NULL, UNIQUE | Foreign key to BankAccount (1:1) |
| `user_id` | UUID | FK, NOT NULL, Indexed (denormalized) | For per-user key derivation |
| `encrypted_data` | BYTEA | NOT NULL | AES-256-GCM encrypted blob |
| `encryption_key_version` | SMALLINT | NOT NULL | Version of master key used |
| `ciphertext_iv` | BYTEA | NOT NULL, Length: 12 | Initialization vector for AES-GCM |
| `auth_tag` | BYTEA | NOT NULL, Length: 16 | HMAC authentication tag |
| `created_at` | TIMESTAMPTZ | NOT NULL, Default: CURRENT_TIMESTAMP | When credential stored |
| `last_used_at` | TIMESTAMPTZ | Nullable | When credential last used |

---

## State Machine: BankAccount.sync_status

```
         pending → syncing → active (success → active on next sync)
           ↑         ↓
           └─ failed (3 retries exhausted)
                 ↓
         reauth_required (user re-links)
```

**Transitions**:
- `pending` → `syncing`: Sync job starts
- `syncing` → `active`: Sync succeeds
- `syncing` → `failed`: Sync fails
- `syncing` → `reauth_required`: Plaid returns ITEM_LOGIN_REQUIRED
- `failed` → `syncing`: Retry triggered
- `reauth_required` → `pending`: User re-links account

---

## SQL Schema

```sql
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- BankAccount table
CREATE TABLE bank_accounts (
    account_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    plaid_item_id VARCHAR(24) UNIQUE NOT NULL,
    bank_name VARCHAR(255) NOT NULL,
    account_type VARCHAR(20) NOT NULL
        CHECK (account_type IN ('checking', 'savings', 'credit', 'investment')),
    account_number_last4 CHAR(4) NOT NULL,
    owner_name VARCHAR(255) NOT NULL,
    currency CHAR(3) NOT NULL CHECK (currency ~ '^[A-Z]{3}$'),
    current_balance DECIMAL(19,4) NOT NULL DEFAULT 0.0000,
    available_balance DECIMAL(19,4),
    sync_status VARCHAR(20) NOT NULL DEFAULT 'pending'
        CHECK (sync_status IN ('pending', 'syncing', 'active', 'failed', 'reauth_required')),
    last_sync_timestamp TIMESTAMPTZ,
    last_sync_duration_ms INTEGER,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

CREATE INDEX idx_bank_accounts_user_id ON bank_accounts(user_id);
CREATE INDEX idx_bank_accounts_plaid_item_id ON bank_accounts(plaid_item_id);
CREATE INDEX idx_bank_accounts_user_sync_status ON bank_accounts(user_id, sync_status)
    WHERE is_active = true;

-- Transaction table
CREATE TABLE transactions (
    transaction_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id UUID NOT NULL,
    user_id UUID NOT NULL,
    plaid_transaction_id VARCHAR(255) NOT NULL,
    amount DECIMAL(19,4) NOT NULL CHECK (amount > 0),
    transaction_type VARCHAR(20) NOT NULL
        CHECK (transaction_type IN ('debit', 'credit', 'transfer')),
    currency CHAR(3) NOT NULL CHECK (currency ~ '^[A-Z]{3}$'),
    posted_date DATE,
    pending_date DATE,
    is_pending BOOLEAN NOT NULL DEFAULT false,
    description TEXT NOT NULL,
    merchant_category VARCHAR(100),
    unique_hash CHAR(64) UNIQUE NOT NULL,
    synced_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (account_id) REFERENCES bank_accounts(account_id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

CREATE INDEX idx_transactions_user_account_date 
    ON transactions(user_id, account_id, posted_date DESC NULLS LAST, pending_date DESC NULLS LAST);
CREATE UNIQUE INDEX idx_transactions_unique_hash ON transactions(unique_hash);
CREATE INDEX idx_transactions_description_gin 
    ON transactions USING GIN(description gin_trgm_ops);
CREATE INDEX idx_transactions_currency 
    ON transactions(user_id, currency, posted_date DESC NULLS LAST);

-- SyncJob table
CREATE TABLE sync_jobs (
    sync_job_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id UUID NOT NULL,
    user_id UUID NOT NULL,
    correlation_id VARCHAR(64) NOT NULL UNIQUE,
    started_at TIMESTAMPTZ NOT NULL,
    completed_at TIMESTAMPTZ,
    status VARCHAR(30) NOT NULL DEFAULT 'pending'
        CHECK (status IN ('pending', 'in_progress', 'success', 'failed', 'circuit_breaker_open', 'partial_failure')),
    error_code VARCHAR(50),
    error_message TEXT,
    transaction_count_fetched INTEGER NOT NULL DEFAULT 0 CHECK (transaction_count_fetched >= 0),
    transaction_count_deduped INTEGER NOT NULL DEFAULT 0 CHECK (transaction_count_deduped >= 0),
    retry_count SMALLINT NOT NULL DEFAULT 0 CHECK (retry_count >= 0),
    http_status_code SMALLINT CHECK (http_status_code >= 100 AND http_status_code < 600),
    webhook_triggered BOOLEAN NOT NULL DEFAULT false,
    
    FOREIGN KEY (account_id) REFERENCES bank_accounts(account_id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

CREATE INDEX idx_sync_jobs_account_id ON sync_jobs(account_id);
CREATE INDEX idx_sync_jobs_user_id ON sync_jobs(user_id);
CREATE INDEX idx_sync_jobs_correlation_id ON sync_jobs(correlation_id);
CREATE INDEX idx_sync_jobs_status_started ON sync_jobs(status, started_at DESC);

-- EncryptedCredential table
CREATE TABLE encrypted_credentials (
    credential_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id UUID NOT NULL UNIQUE,
    user_id UUID NOT NULL,
    encrypted_data BYTEA NOT NULL,
    encryption_key_version SMALLINT NOT NULL CHECK (encryption_key_version > 0),
    ciphertext_iv BYTEA NOT NULL,
    auth_tag BYTEA NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_used_at TIMESTAMPTZ,
    
    FOREIGN KEY (account_id) REFERENCES bank_accounts(account_id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    
    CHECK (octet_length(ciphertext_iv) = 12),
    CHECK (octet_length(auth_tag) = 16),
    CHECK (octet_length(encrypted_data) >= 32)
);

CREATE INDEX idx_encrypted_credentials_account_id ON encrypted_credentials(account_id);
CREATE INDEX idx_encrypted_credentials_user_id ON encrypted_credentials(user_id);

-- Unique constraint
ALTER TABLE bank_accounts
ADD CONSTRAINT unique_user_plaid_item UNIQUE (user_id, plaid_item_id);
```

---

## Performance Characteristics

**Query Response Times** (with indexes):

| Query | Index Used | Latency |
|-------|------------|---------|
| Get 100 transactions for account in last 30 days | `idx_transactions_user_account_date` | <50ms |
| Search transactions by description | `idx_transactions_description_gin` | <200ms |
| Sum balance by currency for user | `idx_transactions_currency` | <100ms |
| Get latest sync jobs for account | `idx_sync_jobs_status_started` | <50ms |
| List accounts for user | `idx_bank_accounts_user_id` | <10ms |

---

## Constraints & Validation

**User Scoping**:
- Every query MUST filter by `user_id` to prevent data leaks
- Application middleware injects `user_id` into all queries

**Immutability**:
- Transactions never updated (insert-only)
- Balance updates on sync (snapshot model)
- Credentials changed via delete old + insert new

**Data Quality**:
- All amounts positive; sign in `transaction_type`
- All dates UTC (TIMESTAMPTZ)
- Descriptions trimmed; max 500 chars
- No NULL user_id or account_id

---

## Multi-Currency Handling

**Storage**:
- `Transaction.amount`: Always positive; DECIMAL(19,4)
- `Transaction.currency`: ISO 4217 code
- `BankAccount.currency`: ISO 4217 code; immutable
- `BankAccount.current_balance`: In account currency, not USD

**Aggregation** (Phase 2 feature):
- Application queries Plaid or external service for FX rates
- Converts to user's base currency at read-time (never stored)
- Example: "Net worth: $X,XXX.XX (updated 2 minutes ago)"

---

## Data Retention Policies

- **Transaction Data**: 24 months (per spec requirement FR-008)
- **Credential Data**: For life of account connection
- **Sync Jobs**: 12 months; then archive
- **User Deletion (GDPR)**: Soft delete accounts; anonymize descriptions; retain for compliance
