# Research: Monobank Bank Provider Adapter

**Branch**: `007-monobank-adapter` | **Date**: 2026-04-19

---

## Decision 1: Monobank API — Endpoint Inventory

**Decision**: Use Monobank personal API at `https://api.monobank.ua`. Authentication via `X-Token` header (personal token obtained at `https://api.monobank.ua/`).

**Endpoints used**:

| Endpoint | Purpose | Rate limit |
|---|---|---|
| `GET /personal/client-info` | Validate token + fetch accounts | 1 req / 60 s |
| `GET /personal/statement/{account}/{from}/{to}` | Fetch transactions for a period | 1 req / 60 s per account |
| `GET /bank/currency` | ISO 4217 numeric → currency code mapping | 5-min cache |
| `POST /personal/webhook` | Register webhook URL for push notifications | No explicit limit |

**Rationale**: Only these endpoints are needed for connect → sync → disconnect flows. Webhooks are available on the personal API (no corporate registration required — spec assumption was wrong, corrected here). Webhooks deferred to a future task.

**Error body shape**:
```json
{ "errorDescription": "string" }
```
HTTP 429 = rate limit exceeded.

---

## Decision 2: Amount and Currency Representation

**Decision**: Monobank amounts are `int64` in the smallest currency unit (kopecks for UAH, cents for USD/EUR). Divide by 100 to get decimal. Currency is ISO 4217 **numeric** code (980 = UAH, 840 = USD, 978 = EUR). Map numeric → alphabetic code using ISO 4217 lookup table in the adapter.

**Rationale**: Domain model (`Transaction.Amount`, `BankAccount.Currency`) uses `decimal` and ISO alphabetic codes (e.g., "UAH"). Conversion must happen in the adapter layer; domain entities must never receive raw numeric codes.

**Alternatives considered**: Storing numeric code in domain — rejected; domain already uses alphabetic codes from Plaid, mixing would require branching in all downstream code.

---

## Decision 3: 90-Day Initial Import Strategy

**Decision**: On initial connect, fetch transactions in three sequential 31-day windows: `[today−90d, today−59d]`, `[today−59d, today−28d]`, `[today−28d, today]`. A 60-second delay is required between calls to the same account to respect rate limits. For accounts with multiple accounts per token (e.g., card + savings), requests across different account IDs can be interleaved without waiting.

**Rationale**: Monobank enforces a max statement window of 31 days + 1 hour (2,682,000 seconds). Three windows cover 90 days. With one account, 90-day import takes ≥ 120 seconds (2 waits). With multiple accounts, the adapter interleaves to minimize total wall time. This fits within SC-001 (2 minutes) for single-account users; multi-account import may take slightly longer but is acceptable for initial connect.

**Alternatives considered**: Single 31-day window only — insufficient; user expects meaningful history on first view.

---

## Decision 4: Incremental Sync Cursor

**Decision**: Monobank has no cursor concept. Incremental sync uses the last stored transaction's Unix timestamp + 1 second as the `from` parameter for subsequent statement calls, up to `now` as `to`. Store `LastSyncAt` (UTC) on `MonobankCredential` and advance it after each successful sync.

**Rationale**: Plaid uses an opaque cursor; Monobank uses time windows. The existing `SyncJob` entity already records timestamps. Using `LastSyncAt` on `MonobankCredential` avoids needing a separate state store.

**Alternatives considered**: Always fetching the last 7 days — simpler but fetches redundant data on every sync; deduplication would catch duplicates but wastes API calls and rate-limit budget.

---

## Decision 5: Credential Storage — MonobankCredential vs EncryptedCredential

**Decision**: Introduce a new `MonobankCredential` domain entity (with its own EF table) rather than reusing `EncryptedCredential`. `MonobankCredential` is user-scoped (1 per user) and referenced by all `BankAccount` rows for that token. `EncryptedCredential` remains 1:1 with `BankAccount` for Plaid only.

**Rationale**: Plaid issues one `access_token` per item (per institution); `EncryptedCredential` is therefore per-account. Monobank issues one personal token per user that covers all cards. Forcing Monobank into the existing 1:1 model would store the same encrypted token N times (one per card). A separate `MonobankCredential` entity with a 1:N relationship to `BankAccount` is correct.

**Alternatives considered**: Reuse `EncryptedCredential` with redundant rows — simpler schema, but wasteful and semantically wrong.

---

## Decision 6: IBankProvider Interface and Factory

**Decision**: Introduce `IBankProvider` as a domain interface for the sync lifecycle (GetAccounts, SyncTransactions, Disconnect). Both `PlaidAdapter` and `MonobankAdapter` implement it. A `BankProviderFactory` resolves the correct provider based on `BankAccount.Provider`. `IPlaidAdapter` and `IMonobankAdapter` remain as provider-specific interfaces for the connect flow (which is fundamentally different per provider).

**Rationale**: Sync operations have the same logical contract regardless of provider. Connection is provider-specific (Plaid: 2-step OAuth link flow; Monobank: direct token submission). Splitting into a common sync interface + provider-specific connect interfaces avoids forcing a single connect signature that doesn't fit both.

**Alternatives considered**: Single `IBankProvider` with all operations (connect + sync) — connect parameters are too different; would require `object params` or excessive overloads.

---

## Decision 7: BankAccount.Provider Column

**Decision**: Add a `Provider` string column to `BankAccount` (values: `"plaid"` | `"monobank"`, default `"plaid"`). Keep the existing `PlaidItemId` column but semantically treat it as `ExternalAccountId` for all providers. A migration renames the column to `ExternalAccountId` and adds the `Provider` column.

**Rationale**: Renaming the column now (while only Plaid accounts exist) is clean and prevents ongoing confusion. The unique index on this column ensures no duplicate accounts across any provider.

**Alternatives considered**: Keep `PlaidItemId` as-is with a comment — rejected; the column name becomes actively misleading as soon as Monobank accounts exist in production.

---

## Decision 8: Frontend Connect Flow

**Decision**: Extend the existing Connect Account page with a provider selection step. "Plaid (US/EU/IE banks)" keeps the existing Plaid Link flow unchanged. "Monobank (Ukraine)" shows a text input for the personal token and a link to `api.monobank.ua` to get one. Both flows converge on the accounts list after successful connect.

**Rationale**: Minimal UI change; no new routing or pages required. Provider selection is a single radio/button group before the existing flow forks.

**Alternatives considered**: Separate `/accounts/connect/monobank` route — not needed; the existing connect page context is correct for both providers.

---

## Decision 9: Webhooks — Deferred

**Decision**: Monobank personal API webhooks DO work without corporate registration (spec assumption was incorrect). However, implementing webhooks is deferred to a future feature. Scheduled polling is sufficient for this feature.

**Rationale**: Webhooks would require exposing a public-facing endpoint, handling webhook validation, and integrating with the push notification flow. Polling is functionally sufficient and avoids scope creep.
