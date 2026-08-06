# Contracts (Existing): Connect Bank, Brokerage, and Crypto Providers

**Feature**: 011-connect-providers · **Date**: 2026-04-25

This feature is frontend-only and consumes existing backend contracts unchanged. Listed below are the endpoints called from the connect modal and the per-provider disconnect flow, with their happy-path and documented error responses. **No new endpoints are introduced; no existing endpoint signature changes.**

All endpoints require the `Authorization: Bearer <accessToken>` header (FR-014). Bodies are JSON.

---

## Plaid (US/CA/EU banks)

### `POST /api/v1/accounts/connect`
Returns Plaid Link token for the hosted overlay.
- **200**: `{ linkToken: string }`
- **401**: not authenticated.

### `POST /api/v1/accounts/link`
Exchange a Plaid `public_token` for a stored item + accounts.
- **Request**: `{ publicToken: string, institutionName: string }`
- **200**: `ConnectBankAccountResult` — `{ accounts: BankAccount[], count: number }`
- **409**: `{ errorCode: 'PLAID_DUPLICATE' }` — the institution is already linked.
- **422**: Plaid rejected the public token; `{ errorCode: string, message: string }`.

### `DELETE /api/v1/accounts/{accountId}`
Disconnect a single Plaid-linked account (P3).
- **204**: no content.
- **404**: account not found / not owned.

---

## Monobank

### `POST /api/v1/accounts/monobank/connect`
- **Request**: `{ token: string }`
- **200**: `ConnectMonobankResult` — `{ accounts: BankAccount[], count: number }`
- **409**: `{ errorCode: 'MONOBANK_TOKEN_DUPLICATE' }`
- **422**: `{ errorCode: 'MONOBANK_TOKEN_INVALID' }` or `{ errorCode: 'MONOBANK_RATE_LIMITED' }`

### `DELETE /api/v1/accounts/monobank` (P3)
- **204**: no content.

---

## Binance

### `POST /api/v1/crypto/binance/connect`
- **Request**: `{ apiKey: string, apiSecret: string }`
- **200**: `ConnectBinanceResult` — `{ holdings: CryptoHolding[], count: number }`
- **409**: `{ errorCode: 'BINANCE_DUPLICATE' }`
- **422**: `{ errorCode: 'BINANCE_INVALID_CREDENTIALS', message: string }` (covers wrong key, IP-restricted, write-scope-only)

### `DELETE /api/v1/crypto/binance` (P3)
- **204**: no content.

---

## IBKR

### `POST /api/v1/brokerage/ibkr/connect`
- **Request**: `{ username: string, password: string }`
- **200**: `ConnectIBKRResult` — `{ holdings: BrokerageHolding[], count: number }`
- **409**: `{ errorCode: 'IBKR_DUPLICATE' }`
- **422**: `{ errorCode: 'IBKR_INVALID_CREDENTIALS', message: string }` (covers gateway rejection / 2FA timeout)

### `DELETE /api/v1/brokerage/ibkr` (P3)
- **204**: no content.

---

## Cross-cutting error contract

Every non-2xx response from the above endpoints conforms to the project's standard error envelope:

```json
{
  "errorCode": "STRING_CODE",
  "message": "Human-readable message (English)",
  "details": [{ "field": "fieldName", "message": "..." }]
}
```

`details` is populated only when `errorCode === 'VALIDATION_ERROR'` (HTTP 400). The frontend renders `details[].message` next to the matching field; otherwise it resolves `errorCode` through `ERROR_MESSAGES_REGISTRY` (Principle VI.3).

## What this feature does NOT change

- No new endpoints.
- No request/response shape changes.
- No error code added on the backend (only registry entries on the client).
- Backend version in `FinanceSentry.API.csproj` is **not** bumped; only `frontend/package.json` minor-bumps for this feature (per Versioning policy: "new component/feature, non-breaking changes").
