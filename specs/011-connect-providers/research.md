# Phase 0 Research: Connect Bank, Brokerage, and Crypto Providers

**Feature**: 011-connect-providers · **Date**: 2026-04-25

The spec contains zero `[NEEDS CLARIFICATION]` markers (verified by checklists/requirements.md). All research below resolves design choices, not unknowns.

---

## R1 — Modal vs. dedicated route per provider

**Decision**: Single modal hosted by `connect-account.component`, driven by `ConnectStore.modalStep` (existing state machine: `closed | type-picker | bank-picker | monobank-form | binance-form | ibkr-form`).

**Rationale**:
- The store already models the modal step machine — using a different navigation strategy would force a rewrite of the store.
- The Stitch design renders the flow inside a centered card that overlays the accounts list; a modal matches both the design intent and the spec's "open the connect flow" framing (FR-001).
- Routing to a dedicated `/connect/:provider` page would lose the back-to-picker affordance without extra plumbing (browser back vs. internal back).

**Alternatives considered**:
- Per-provider routes (`/connect/plaid`, `/connect/binance`, …) — rejected: more router boilerplate, breaks the picker → form → success narrative, requires duplicate guards.
- Inline accordion on the accounts list — rejected: violates Stitch design, doesn't scale to four providers without crowding.

---

## R2 — Provider catalog as data, not as switch statements

**Decision**: One `ProviderDescriptor[]` constant in `shared/constants/providers/providers.constants.ts`, plus a `ProviderDescriptor` type in `shared/models/provider/provider.model.ts`. Each entry: `{slug, displayName, institutionType, description, iconAsset, formShape}`. Forms key off `formShape` (`'plaid-link' | 'token' | 'key-secret' | 'user-pass'`), not provider slug.

**Rationale**:
- FR-002 requires "all four providers" with name, icon, description, status. A list-driven render keeps the picker open/closed state and the "Connected/Available" badge data-bound rather than hand-coded.
- Five future providers (Coinbase, Schwab, Revolut, etc., per Assumptions) onto the same form shapes will be one-line registry adds.
- Cross-module use (bank-sync picker + holdings module's "Connect more" CTA) requires `shared/` per Principle VI.5.

**Alternatives considered**:
- Hard-coded JSX-style provider buttons — rejected: violates spec's growth assumption and Principle VI.5.
- Backend-driven catalog endpoint — rejected: providers are a front-end concept (icon, copy, form shape); querying the backend adds latency to a static list.

---

## R3 — Where to source "already connected" status

**Decision**: Hydrate "connected" badges from existing list endpoints already loaded by `AccountsStore` (Plaid + Monobank rows in `BankAccount[]`) and `HoldingsStore` (Binance + IBKR provider sections). The connect modal's picker reads from a derived computed signal `connectedProviders: Signal<ReadonlySet<ProviderSlug>>` exposed by a new app-level lookup composed from both stores' loaded data.

**Rationale**:
- Avoids a new "GET /providers/status" endpoint — the data is already on the client by the time the user opens the connect modal (accounts list is the parent route).
- Keeps the modal in sync with disconnect/connect state without extra invalidation glue.

**Alternatives considered**:
- New backend "status" endpoint — rejected: pure derivation; introducing an endpoint is busywork.
- Reading `localStorage` flags — rejected: violates Principle V (no client-side state of credential presence).

---

## R4 — Error code mapping additions

**Decision**: Add the following keys to `ERROR_MESSAGES_REGISTRY` in the same PR:

| `errorCode` | Message |
|---|---|
| `BINANCE_INVALID_CREDENTIALS` | "Binance rejected the provided credentials. Use a read-only API key with no IP restrictions." |
| `BINANCE_DUPLICATE` | "Binance account already connected. Disconnect the existing one to use new keys." |
| `IBKR_INVALID_CREDENTIALS` | "IB Gateway rejected the provided credentials. Confirm the 2FA push notification on your phone and try again." |
| `IBKR_DUPLICATE` | "IBKR account already connected. Disconnect the existing one to reconnect." |
| `PLAID_DUPLICATE` | "This bank is already connected. View it in your accounts list." |
| `PLAID_SCRIPT_LOAD_FAILED` | "Plaid is unavailable. Disable any ad blocker and refresh the page." |
| `VALIDATION_ERROR` | (Resolved per-field from backend `details` array; registry holds form-level fallback "Some fields look wrong — please review the highlighted errors.") |

`MONOBANK_TOKEN_INVALID`, `MONOBANK_TOKEN_DUPLICATE`, and `MONOBANK_RATE_LIMITED` already exist.

**Rationale**: Principle VI.3 — every backend `errorCode` referenced by FR-009/-010/-011/-012 must resolve through the registry. Form-field-level errors for `VALIDATION_ERROR` come straight from `details[].field/message` and bypass the registry; the registry value is the banner fallback.

**Alternatives considered**:
- Local-only ladder in `connect.computed.ts` — rejected: violates Principle VI.3.

---

## R5 — Plaid script-load failure detection

**Decision**: `PlaidLinkService.prepare()` already rejects/throws when the Plaid script fails to load; the connect store's `initPlaid` rxMethod catches and calls `setError(PLAID_SCRIPT_LOAD_FAILED)` when the error has no upstream `errorCode`. Frontend translates that fixed code via the registry (R4).

**Rationale**: Reuses the existing rxMethod error path; one new constant in `connect.effects.ts` (`PLAID_SCRIPT_LOAD_FAILED` literal) is the only change.

**Alternatives considered**:
- Window-level `error` listener on the script tag — rejected: would couple connect logic to DOM details that `PlaidLinkService` already abstracts.

---

## R6 — Routing after success

**Decision**: After `setSuccess()` fires, a new `connectOnSuccess` effect (`signalEffect` on `status === 'success'`) reads `institutionType` and routes:

- `bank` → `/accounts`
- `crypto` → `/holdings#binance`
- `broker` → `/holdings#ibkr`

Routing happens within 2 s (FR-008) — implemented as an immediate `router.navigateByUrl(...)` after a short debounce so the success toast remains visible briefly before the page transition.

**Rationale**: Centralises the post-success navigation in the store (Principle VI.1). Components stay declarative.

**Alternatives considered**:
- Component-level `effect()` on `store.status()` — rejected: violates "no `effect()` in components" rule.

---

## R7 — Disconnect (P3) implementation

**Decision**: One shared `DisconnectDialogComponent` (lives in `bank-sync/components/disconnect-dialog/`, registered as a `cmn-` candidate if it proves library-grade in code review). Each store that owns provider data exposes a `disconnect(slug)` rxMethod that calls the matching service (`bankSyncService.disconnect(accountId)`, `binanceService.disconnect()`, `ibkrService.disconnect()`); on success, it removes the row locally and re-emits the connected-providers set.

**Rationale**: Disconnect is a hygiene action that belongs to the store that owns the data. The dialog is a pure UI component — its `confirmed` output drives the call.

**Alternatives considered**:
- One central "Disconnect" rxMethod on `ConnectStore` switching by slug — rejected: forces the connect store to depend on `AccountsStore` and `HoldingsStore`, inverting ownership.

---

## R8 — Dialog primitive in `@dsdevq-common/ui` (CDK-based)

**Decision**: Build `CmnDialogService` + `CmnDialogContainerComponent` on `@angular/cdk/dialog`. The service exposes `open<R, D, C>(component: ComponentType<C>, config?: CmnDialogConfig<D>): CmnDialogRef<R>`. The container extends `CdkDialogContainer` and provides the default visual shell: backdrop, panel sizing (`sm | md | lg | full`), optional header (title + close button), scrollable body, footer slot, ESC-to-close, backdrop-click-to-close (configurable via `disableClose`). Data is passed through a `CMN_DIALOG_DATA` `InjectionToken<D>` analogous to CDK's `DIALOG_DATA`.

**Rationale**:
- This is the app's first modal. Building the primitive once in the library means every future modal (disconnect dialog, future settings modals, future confirms) reuses the same shell, accessibility, and styling.
- CDK's `Dialog` already gives focus trapping, ARIA roles (`role="dialog"` / `aria-modal`), ESC handling, and overlay management — we don't reimplement that, just style and slot.
- Extending `CdkDialogContainer` (instead of using a wrapper component with `NgComponentOutlet`) means CDK still handles the portal lifecycle natively; we only add layout.

**Alternatives considered**:
- Hand-rolled overlay using `@angular/cdk/overlay` directly — rejected: rebuilds focus management and ARIA from scratch.
- `NgComponentOutlet`-based wrapper without extending `CdkDialogContainer` — rejected: requires re-piping `DIALOG_DATA` and breaks CDK's intended container extension point.
- Adopt Angular Material's `MatDialog` — rejected: pulls in Material theming + a parallel UI library, conflicts with `@dsdevq-common/ui` discipline.

---

## R9 — Icon registry: hybrid (raw-SVG registry + Lucide custom-provider helper)

**Decision**: Add `CmnIconRegistry` (raw SVG / URL-loaded, sanitized, cached) AND export a thin `provideLucideIcons(map)` helper that wraps Lucide's native `LucideIconProvider` for monochrome custom icons. `cmn-icon` resolution order: `CmnIconRegistry` → Lucide built-ins → Lucide custom providers → empty fallback. **Brand provider logos do NOT go through `cmn-icon`** — they render as plain `<img src="/assets/providers/<slug>.svg">` in provider tiles. `cmn-icon` stays reserved for stroke/Lucide-style icons.

**Rationale**:
- Lucide's `LucideIconData = readonly [tagName, attrs][]` format is flat (no children, no `<text>`); it can't express colored brand monograms or any logo with text content. Trying to force brand artwork through Lucide loses fidelity.
- Brand artwork has different sizing, color, and a11y semantics than icons (logos have intrinsic colors and aspect ratios; icons inherit `currentColor`). Mixing them under one component muddies both abstractions.
- Keeping `CmnIconRegistry` future-proofs us for any future need to register raw SVGs (custom illustrations, status badges with text, etc.).
- Exposing `provideLucideIcons()` as a thin wrapper around the canonical Lucide mechanism makes future monochrome custom icons trivial without forking Lucide's pattern.

**Alternatives considered**:
- Lucide-only via `LucideIconProvider` — rejected: cannot express brand monograms (no `<text>` or children support).
- Raw-registry-only — rejected: forfeits Lucide's strong stroke-icon ecosystem if we ever want to add monochrome custom icons.
- Render brand logos through `cmn-icon` with `<img>` fallback inside the component — rejected: overloads `cmn-icon`'s semantics (icons inherit `currentColor`, brand logos do not).

**Migration path**: If branded icon needs change (e.g. design hands off proper Lucide-format strokes), the same `cmn-icon name="provider-plaid"` call works once we switch the registration from `CmnIconRegistry` to `LucideIconProvider` — no consumer-site changes.

---

## R10 — Strategy pattern for connect entities

**Decision**: One `ConnectStrategy` interface in `bank-sync/strategies/connect-strategy.ts`:

```ts
export interface ConnectOutcome {
  successCode: 'CONNECTED' | 'POLLING';
  count: number;
  institutionType: InstitutionType;
}

export interface ConnectStrategy {
  readonly slug: ProviderSlug;
  readonly formComponent: Type<unknown>;
  submit(input: unknown): Observable<ConnectOutcome>;
}
```

Four concrete classes — `PlaidConnectStrategy`, `MonobankConnectStrategy`, `BinanceConnectStrategy`, `IbkrConnectStrategy` — each `@Injectable({providedIn: 'root'})`, each injecting its own service, each registered as a multi-provider on `CONNECT_STRATEGIES` (`InjectionToken<readonly ConnectStrategy[]>`). A `ConnectStrategyRegistry` consumes the multi-token and exposes `getBySlug(slug): ConnectStrategy`.

The connect modal:
1. Reads `store.selectedProvider()` → asks the registry for the matching strategy.
2. Builds a child `Injector` at render time that provides the resolved strategy as `CONNECT_STRATEGY` (singular `InjectionToken<ConnectStrategy>`).
3. Renders the strategy's form via `<ng-container *ngComponentOutlet="strategy.formComponent; injector: formInjector()">`.
4. The form component (a) is responsible for its own UI (inputs + submit button or, for Plaid, a single "Open Plaid Link" button), (b) `inject(CONNECT_STRATEGY)` — never imports a concrete strategy class — and dispatches `connectStore.connect({strategy, payload})`.
5. `ConnectStore.connect({strategy, payload})` is a single rxMethod that calls `strategy.submit(payload)`, then triggers polling and routing on success.

**Two tokens, two roles**:
- `CONNECT_STRATEGIES` (multi, plural) — every concrete strategy registers here. Read once by `ConnectStrategyRegistry` to enumerate (e.g. for the type-picker's connected-status badges) and to resolve by slug.
- `CONNECT_STRATEGY` (singular) — provided fresh in each form-component injector by the connect modal. Form components consume this and stay agnostic of concrete strategy classes.

**Why the per-form injector matters**: form components must stay decoupled from concrete strategy classes. If `MonobankFormComponent` `inject(MonobankConnectStrategy)`, then adding a 5th provider that wants to reuse the same form shape would force a new component or a switch statement. With `inject(CONNECT_STRATEGY)`, the modal picks the right strategy and the form just runs.

**Rationale**:
- **Adding a 5th provider** = 1 new strategy class + 1 form component + 1 catalog entry. The modal, the store's connect rxMethod, and the form-rendering machinery all stay unchanged. This is the spec's growth assumption (Coinbase, Schwab, Revolut, etc.) made concrete.
- **Encapsulation**: each strategy owns its payload type, its service call, its error code mapping. The store no longer needs `connectMonobank` / `connectBinance` / `connectIBKR` / Plaid-specific rxMethods — it has one `connect`.
- **Testability**: each strategy is a small `@Injectable` with one method. Tests mock the service and assert the outcome shape. No need to spin up the full store to test provider logic.
- **Plaid uniformity**: Plaid is the awkward one — it's a hosted overlay, not a form. Wrapping its `getLinkToken → prepare → open → exchange` flow inside `PlaidConnectStrategy.submit(void)` lets it look identical to the others from the store's perspective. The Plaid form component is just a launcher button that dispatches `submit(undefined)`.

**Alternatives considered**:
- One mega-rxMethod-per-provider in the store — rejected: the current shape; doesn't scale; couples store to every new provider.
- Strategies that don't render their own form (form lives in modal, strategy is pure logic) — rejected: requires the modal to know each provider's form shape, defeating extensibility.
- Strategy classes registered via decorators / metadata — rejected: TypeScript's `Type<T>` reflection is awkward; explicit multi-token is clearer and more discoverable.

**Open question for the implementation phase**: how to type `submit(input: unknown)` cleanly. Two options to revisit during implementation: (a) generic `ConnectStrategy<TPayload>` with each strategy specializing the parameter type, modal narrows via `as` cast at the call site; (b) keep `unknown` and let each form component cast its own payload. Decide during T-coding.

---

## R11 — Mobile responsiveness strategy

**Decision**: The modal renders as a centered card on ≥ 768 px and as a full-bleed sheet on < 768 px (Tailwind breakpoint `md`). Plaid Link's own responsive behavior is sufficient on mobile; no extra wrapping needed. Verify SC-006 with Storybook viewport snapshots at 360, 480, 768, 1024, 1440, 1920.

**Rationale**: Matches the rest of the app's Stitch mobile pattern and the existing `cmn-card` responsive defaults.

**Alternatives considered**:
- Bottom-sheet on mobile, modal on desktop — rejected: extra component, no usability win at our viewport spread.
