# Quickstart: Monobank Adapter Integration Scenarios

**Feature**: `007-monobank-adapter` | **Date**: 2026-04-19

---

## Scenario 1: Connect a Monobank Account

**Prerequisites**:
- User is logged in (valid JWT)
- User has a Monobank personal API token from `https://api.monobank.ua/`

**Steps**:

1. User navigates to Connect Account page
2. User selects "Monobank (Ukraine)" as provider
3. User pastes their personal token into the text field
4. User clicks "Connect"
5. System calls `POST /api/v1/accounts/monobank/connect` with `{ token }`
6. Backend validates token via `GET /personal/client-info` → Monobank API
7. Backend creates `MonobankCredential` (encrypted token) + `BankAccount` row(s)
8. Backend enqueues 90-day history import job
9. API returns 201 with account list
10. Frontend redirects to accounts list showing new Monobank accounts (status: "pending" → "active" after import)

**Expected state after completion**:
- `MonobankCredentials` table: 1 row for user
- `BankAccounts` table: N rows (one per Monobank card), `Provider = 'monobank'`
- `Transactions` table: up to 90 days of history per account

---

## Scenario 2: Incremental Sync

**Prerequisites**: Monobank account connected and active

**Steps**:

1. User clicks "Sync" on their Monobank account
2. System calls `POST /api/v1/accounts/{id}/sync`
3. Backend resolves `MonobankAdapter` via `BankProviderFactory`
4. Adapter reads `MonobankCredential.LastSyncAt` as `from` timestamp
5. Adapter calls `GET /personal/statement/{account}/{from}/{to}` for each account
6. New transactions upserted; `LastSyncAt` updated
7. Sync job marked "success"

**Expected state**: Only transactions newer than `LastSyncAt` are inserted; no duplicates.

---

## Scenario 3: Invalid Token at Connect

**Steps**:

1. User submits an invalid or expired token
2. Backend calls `GET /personal/client-info` — Monobank returns 401/403
3. Backend returns `400 MONOBANK_TOKEN_INVALID`
4. Frontend shows error: "Invalid or expired Monobank token."
5. No `MonobankCredential` or `BankAccount` rows created

---

## Scenario 4: Token Revoked After Connect (Sync Fails)

**Steps**:

1. Scheduled sync runs for a Monobank account
2. Adapter calls `/personal/client-info` — Monobank returns 401 (token revoked)
3. Backend marks `BankAccount.SyncStatus = 'failed'`, `LastSyncError = 'MONOBANK_TOKEN_INVALID'`
4. User sees account in error state on accounts list
5. User must re-connect (delete account and connect with new token)

---

## Scenario 5: Rate Limit Hit During 90-Day Import

**Steps**:

1. Initial import job fetches first 31-day window successfully
2. Second request hits Monobank's 60-second rate limit (HTTP 429)
3. Backend waits 60 seconds and retries
4. Third window fetched; import completes
5. `BankAccount.SyncStatus` transitions to `"active"` after all windows processed

---

## Scenario 6: Disconnect Monobank Account

**Steps**:

1. User clicks "Disconnect" on a Monobank account
2. System calls `DELETE /api/v1/accounts/{id}`
3. Backend soft-deletes the `BankAccount` row
4. If no remaining `BankAccount` rows reference the same `MonobankCredential`, the credential is hard-deleted
5. User sees the account removed from their list

---

## Manual Testing Checklist

- [ ] Connect with valid token → accounts appear
- [ ] Connect with invalid token → error shown, no rows created
- [ ] Connect with already-connected token → 409 conflict
- [ ] Transactions visible in transaction list for Monobank account
- [ ] Manual sync fetches new transactions without duplicates
- [ ] Disconnect removes account; re-connect creates fresh account
- [ ] Multiple Monobank cards under one token → all cards appear as separate accounts
- [ ] Accounts list shows `provider: monobank` badge
