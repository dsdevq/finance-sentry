# Phase 1 Data Model: Connect Bank, Brokerage, and Crypto Providers

**Feature**: 011-connect-providers · **Date**: 2026-04-25

This feature is frontend-only and adds no database tables. The "data model" here is the frontend type model that backs the connect flow.

---

## ProviderDescriptor (NEW — `shared/models/provider/provider.model.ts`)

```ts
export type ProviderSlug = 'plaid' | 'monobank' | 'binance' | 'ibkr';
export type InstitutionType = 'bank' | 'crypto' | 'broker';
export type ProviderFormShape = 'plaid-link' | 'token' | 'key-secret' | 'user-pass';

export interface ProviderDescriptor {
  readonly slug: ProviderSlug;
  readonly displayName: string;     // "Plaid", "Monobank", "Binance", "Interactive Brokers"
  readonly institutionType: InstitutionType;
  readonly description: string;     // "Connect US/CA/EU banks via Plaid"
  readonly iconAsset: string;       // /assets/providers/<slug>.svg
  readonly formShape: ProviderFormShape;
  readonly helpUrl: Nullable<string>;
}
```

Lives in `shared/` per Principle VI.5 (consumed by both `bank-sync` and `holdings`).

**Validation rules**:
- `slug` is the union literal — TypeScript enforces.
- Catalog completeness: a CI/test asserts every `ProviderSlug` literal has exactly one descriptor.

---

## ConnectState (MODIFY — existing `bank-sync/store/connect/connect.state.ts`)

Existing state already supports the modal step machine:

```ts
type ConnectStatus = 'idle' | 'initializing' | 'ready' | 'syncing' | 'polling' | 'error' | 'success';

interface ConnectState {
  selectedProvider: Provider;          // existing: 'plaid' | 'monobank' | 'binance' | 'ibkr'
  status: ConnectStatus;
  errorCode: Nullable<string>;
  statusMessage: Nullable<string>;
  modalStep: ModalStep;                // 'closed' | 'type-picker' | 'bank-picker' | 'monobank-form' | 'binance-form' | 'ibkr-form'
  institutionType: Nullable<InstitutionType>;
}
```

No structural change required. The existing `Provider` union is renamed (locally) to align with `ProviderSlug`; both names point to the same union literal — no runtime change.

**State transitions** (visualised):

```
closed --openModal--> type-picker
type-picker --selectInstitutionType('bank')--> bank-picker
type-picker --selectInstitutionType('crypto')--> binance-form
type-picker --selectInstitutionType('broker')--> ibkr-form
bank-picker --selectProvider('plaid')--> (Plaid overlay)
bank-picker --selectProvider('monobank')--> monobank-form
*-form --submit ok--> closed (status: success)
*-form --submit err--> *-form (status: error, errorCode set)
*-step --closeModal/back--> closed | parent step
```

---

## ConnectionFormSubmission (transient, NOT persisted)

Per provider, lives only inside Angular `FormGroup`/`FormControl` for the duration of the modal:

| Provider | Fields | Validation (client) |
|---|---|---|
| Plaid | (none — overlay) | n/a |
| Monobank | `token: string` | `required`, `maxLength=64`, trim on submit, regex `^[A-Za-z0-9_-]+$` |
| Binance | `apiKey: string`, `apiSecret: string` | both `required`, `maxLength=128`, no whitespace |
| IBKR | `username: string`, `password: string` | both `required`, `username.maxLength=32`, no leading/trailing whitespace |

Discarded on:
- successful submit (form reset before close)
- modal close / back navigation
- any error response (form remains editable; values stay in `FormControl` state only)

Never written to: `localStorage`, `sessionStorage`, `IndexedDB`, cookies, the SignalStore, the global error handler, or any logger.

---

## ConnectResult (read-side, EXISTING)

The four backend handlers already return typed results consumed verbatim:

- `ConnectBankAccountResult` — Plaid public-token exchange (existing).
- `ConnectMonobankResult` — list of bank accounts created.
- `ConnectBinanceResult` — list of crypto holdings.
- `ConnectIBKRResult` — list of brokerage holdings.

The success step reads the `count` field from each (or derives from `accounts.length` / `holdings.length`) for the "X account(s) added" copy.

---

## Connected-providers derivation (NEW computed)

```ts
// frontend/src/app/shared/store/connected-providers.computed.ts (or in-place inside ConnectStore)
const connectedProviders = computed<ReadonlySet<ProviderSlug>>(() => {
  const set = new Set<ProviderSlug>();
  if (accountsStore.bankAccounts().some(a => a.provider === 'plaid')) set.add('plaid');
  if (accountsStore.bankAccounts().some(a => a.provider === 'monobank')) set.add('monobank');
  if (holdingsStore.binanceHoldings().length > 0) set.add('binance');
  if (holdingsStore.brokerageHoldings().length > 0) set.add('ibkr');
  return set;
});
```

Used by the picker to render `<cmn-badge variant="success">Connected</cmn-badge>` per row.

---

## ConnectStrategy + ConnectOutcome (NEW — `bank-sync/strategies/connect-strategy.ts`)

```ts
export interface ConnectOutcome {
  readonly successCode: 'CONNECTED' | 'POLLING';
  readonly count: number;
  readonly institutionType: InstitutionType;
}

export interface ConnectStrategy {
  readonly slug: ProviderSlug;
  readonly formComponent: Type<unknown>;
  submit(input: unknown): Observable<ConnectOutcome>;
}
```

Per research.md R10. Concrete implementations: `PlaidConnectStrategy`, `MonobankConnectStrategy`, `BinanceConnectStrategy`, `IbkrConnectStrategy`, all `@Injectable({providedIn: 'root'})`, all registered as multi-providers on `CONNECT_STRATEGIES: InjectionToken<readonly ConnectStrategy[]>`. `ConnectStrategyRegistry.getBySlug(slug)` resolves at runtime.

## CmnDialogConfig (NEW — `@dsdevq-common/ui`)

```ts
export interface CmnDialogConfig<D = unknown> {
  readonly data?: D;
  readonly disableClose?: boolean;
  readonly ariaLabel?: string;
  readonly autoFocus?: 'first-tabbable' | 'first-heading' | 'dialog' | false;
  readonly title?: string;
  readonly size?: 'sm' | 'md' | 'lg' | 'full';
}

export const CMN_DIALOG_DATA = new InjectionToken<unknown>('CmnDialogData');
```

Used by the connect modal (`{size: 'md', title: 'Connect account'}`) and the disconnect dialog (`{size: 'sm', title: 'Disconnect …', data: {providerName}}`). Per research.md R8.

## No database / migration / backend schema changes

This feature does not introduce any:
- Backend entity
- EF migration
- New REST endpoint
- New `errorCode` value emitted by the server (only registry entries on the client for codes the backend already returns)
