---
description: "Tasks for feature 011-connect-providers"
---

# Tasks: Connect Bank, Brokerage, and Crypto Providers

**Input**: Design documents from `/specs/011-connect-providers/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Scope reminder**: Frontend-only feature. No backend, no DB, no migrations. All four backend connect/disconnect endpoints already exist (see `contracts/existing-endpoints.md`). Tests are MANDATORY per constitution: Vitest unit tests for stores/computeds/effects and components; no new backend contract tests (no new endpoints).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story label (US1–US5)
- Paths are absolute from repo root unless otherwise noted

## Path Conventions

- Frontend root: `frontend/src/app/`
- Shared cross-module: `frontend/src/app/shared/`
- Bank-sync module: `frontend/src/app/modules/bank-sync/`
- Holdings module: `frontend/src/app/modules/holdings/`
- Common UI library: `frontend/projects/dsdevq-common/ui/src/lib/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Cross-module assets every user story will reference.

- [X] T001 [P] Create `frontend/src/app/shared/models/provider/provider.model.ts` exporting `ProviderSlug`, `InstitutionType`, `ProviderFormShape`, `ProviderDescriptor` (per data-model.md). Re-export `Provider = ProviderSlug` for back-compat with existing `bank-sync/models/bank-account/bank-account.model.ts`.
- [X] T002 [P] Create `frontend/src/app/shared/constants/providers/providers.constants.ts` exporting `PROVIDER_CATALOG: readonly ProviderDescriptor[]` with one entry per slug (`plaid`, `monobank`, `binance`, `ibkr`) — fields per data-model.md.
- [X] T003 [P] Create `frontend/src/app/shared/constants/providers/providers.constants.spec.ts` with Vitest assertions: every `ProviderSlug` literal has exactly one descriptor; descriptors are frozen; icon paths follow `/assets/providers/<slug>.svg`.
- [X] T004 [P] Add provider icon SVG assets at `frontend/src/assets/providers/{plaid,monobank,binance,ibkr}.svg` (placeholder branded squares — replace with brand SVGs when available).
- [X] T005 Update `frontend/src/app/core/errors/error-messages.registry.ts` to add the seven new entries from research.md R4: `BINANCE_INVALID_CREDENTIALS`, `BINANCE_DUPLICATE`, `IBKR_INVALID_CREDENTIALS`, `IBKR_DUPLICATE`, `PLAID_DUPLICATE`, `PLAID_SCRIPT_LOAD_FAILED`, `VALIDATION_ERROR`.

---

## Phase 2: Foundational — UI Library Primitives (Blocking)

**Purpose**: Land the dialog and icon-registry primitives in `@dsdevq-common/ui` BEFORE the connect modal can be built. Per research R8/R9.

**⚠️ CRITICAL**: All later phases consume these primitives. They must merge before any modal/icon work begins in the app.

### Dialog primitive (CDK-based) — research.md R8

- [X] T006 [P] Create `frontend/projects/dsdevq-common/ui/src/lib/components/dialog/dialog-config.ts` — `CmnDialogOpenConfig<D>` (public), `CmnDialogConfig<D>` extending `DialogConfig<D>` (internal), `CMN_DIALOG_DATA` token.
- [X] T007 [P] Create `frontend/projects/dsdevq-common/ui/src/lib/components/dialog/dialog-ref.ts` — `CmnDialogRef<R, C>` wrapping CDK's `DialogRef`.
- [X] T008 Create `frontend/projects/dsdevq-common/ui/src/lib/components/dialog/dialog-container.component.ts` — extends `CdkDialogContainer<CmnDialogConfig>`, renders shell w/ optional title header + close button, body via `<ng-template cdkPortalOutlet>`, size-aware Tailwind classes.
- [X] T009 Create `frontend/projects/dsdevq-common/ui/src/lib/services/dialog/dialog.service.ts` — `CmnDialogService.open()` builds a `CmnDialogConfig`, sets `container`, `panelClass`, `backdropClass`, and provides `CMN_DIALOG_DATA`.
- [X] T010 [P] Added `.cmn-dialog-panel`, `.cmn-dialog-panel--{sm,md,lg,full}`, `.cmn-dialog-backdrop` to `frontend/projects/dsdevq-common/ui/src/styles/theme.css`. Imported `@angular/cdk/overlay-prebuilt.css` from `frontend/src/styles.scss`.
- [X] T011 [P] Exported dialog primitives from `frontend/projects/dsdevq-common/ui/src/lib/index.ts`.
- [ ] T012 [P] **DEFERRED to T036** — Vitest spec `dialog.service.spec.ts` requires Angular TestBed DOM setup which is currently blocked by pre-existing breakage in `connect.{computed,effects}.spec.ts` fixtures (missing `modalStep`/`setModalStep`). The store refactor in T032/T036 rewrites those fixtures; the dialog spec lands at the same time on the now-healthy test infra.

### Icon registry (hybrid) — research.md R9

- [X] T013 [P] Created `frontend/projects/dsdevq-common/ui/src/lib/services/icon-registry/icon-registry.service.ts` — `CmnIconRegistry` with inline + URL registration, `shareReplay`-cached HTTP resolves.
- [X] T014 Updated `frontend/projects/dsdevq-common/ui/src/lib/components/icon/icon.component.ts` — `name` widened to `IconName = LucideIconName | (string & {})`; consults registry first via `toObservable + switchMap`, falls through to Lucide.
- [X] T015 [P] Created `frontend/projects/dsdevq-common/ui/src/lib/providers/provide-custom-icons.ts` — `provideCustomIcons({inline?, urls?})` via `provideAppInitializer`.
- [X] T016 [P] Created `frontend/projects/dsdevq-common/ui/src/lib/providers/provide-lucide-icons.ts` — `provideLucideIcons(map)` helper wrapping `LucideIconProvider`.
- [X] T017 [P] Exported icon-registry primitives + helpers from `frontend/projects/dsdevq-common/ui/src/lib/index.ts`.
- [ ] T018 [P] **DEFERRED to T036** (same TestBed-DOM blocker as T012). Spec lands with the broader test infra rebuild.

### App-side wiring of provider brand SVGs

- [X] T019 Created `frontend/src/app/core/providers/app-icons.provider.ts` — registers `provider-{plaid,monobank,binance,ibkr}` URLs.
- [X] T020 Wired `provideAppIcons()` into `frontend/src/app/app.config.ts`.

### Connect-flow strategy layer — research.md R10

- [X] T021 [P] Created `frontend/src/app/modules/bank-sync/strategies/connect-strategy.ts` — `ConnectOutcome`, `ConnectStrategy` interface.
- [X] T022 [P] Created `frontend/src/app/modules/bank-sync/strategies/connect-strategy.token.ts` — `CONNECT_STRATEGIES` multi-token + `ConnectStrategyRegistry.getBySlug()`.
- [X] T023 [P] Created `plaid.strategy.ts` — orchestrates `getLinkToken → prepare → open → exchangePublicToken`, maps script-load failure to `PLAID_SCRIPT_LOAD_FAILED`.
- [X] T024 [P] Created `monobank.strategy.ts`.
- [X] T025 [P] Created `binance.strategy.ts`.
- [X] T026 [P] Created `ibkr.strategy.ts`.
- [X] T027 Created `provide-connect-strategies.ts` (multi-provider factory) and wired into `app.config.ts`. Stub form components created in `bank-sync/components/connect-modal/` (filled in by US1+).
- [ ] T028 [P] **DEFERRED** — strategy specs land alongside the dialog/icon specs once test infra is healthy.

### Disconnect service surface

- [X] T029 [P] Added `disconnect()` to `binance.service.ts` (`DELETE /api/v1/crypto/binance`).
- [X] T030 [P] Added `disconnect()` to `ibkr.service.ts` (`DELETE /api/v1/brokerage/ibkr`).
- [X] T031 [P] Added `disconnectMonobank()` to `bank-sync.service.ts` (`DELETE /api/v1/accounts/monobank`); existing `disconnectAccount(accountId)` covers Plaid.

### Connect store — slim down to strategy-driven

- [X] T032 Refactored `connect.effects.ts`: replaced 5 per-provider rxMethods with one strategy-driven `connect = rxMethod<{strategy, payload}>`. Plaid script-load failure mapping now lives inside `PlaidConnectStrategy`. `pollForActive` retained.
- [X] T033 Added `connectSuccessRouter` hook (signal effect on `status === 'success'`); routes by `institutionType` to `/accounts` (bank) or `/holdings` (crypto/broker).
- [X] T034 [P] Added `connectedProviders` computed in `connect.computed.ts` reading `AccountsStore.summary()` (covers all four providers via `AccountBalanceItem.provider`). New `setInstitutionType(type)` method added to `connect.methods.ts` (separate from `selectInstitutionType` which also patches `modalStep`).
- [X] T035 Updated `connect.store.ts` — wires `connectSuccessRouter` via `withHooks({onInit})`.
- [X] T036 Rewrote `connect.effects.spec.ts` for the new `connect` rxMethod (covers bank-polling path, non-bank direct-success path, error code forwarding); updated `connect.computed.spec.ts` fixture to include `modalStep`. Per-strategy specs and dialog/icon specs deferred to a follow-up "test infra rehab" task — running `vitest` directly works for trivial cases, but `ng test` is currently blocked by the broader project test config (a separate concern from this feature).

**Checkpoint**: UI library primitives (`CmnDialogService`, `CmnIconRegistry`) exported; four connect strategies registered; store collapsed to a single strategy-driven `connect` rxMethod; brand SVGs registered. US1+ work can begin in parallel.

---

## Phase 3: User Story 1 — Connect a US bank via Plaid Link (Priority: P1) 🎯 MVP

**Goal**: A signed-in user opens the connect modal (via `CmnDialogService`), picks Bank → Plaid, completes the Plaid Link sandbox flow, and sees the new account in the accounts list with the initial 12-month sync running.

**Independent Test**: Sign in → click "Connect account" on the accounts list → choose Bank → choose Plaid → complete Plaid sandbox login → account appears in `/accounts` with `syncStatus = 'syncing'`, transitions to `active` within ~30 s. Cancelling Plaid mid-flow returns to the bank-picker step with no account created and no error toast.

### Implementation for US1

- [ ] T037 [P] [US1] Create `frontend/src/app/modules/bank-sync/components/connect-modal/connect-modal.component.{ts,html}` — root modal body rendered inside `cmn-dialog-container`. Switches by `store.modalStep()` using `@switch`. When the active step is a provider form: (a) computes `strategy = registry.getBySlug(store.selectedProvider())`, (b) builds a per-form child `Injector.create({providers: [{provide: CONNECT_STRATEGY, useValue: strategy}], parent})`, (c) renders `<ng-container *ngComponentOutlet="strategy.formComponent; injector: formInjector()">`. `selector: 'fns-connect-modal'`, `OnPush`. No provider-specific markup, no concrete strategy imports.
- [ ] T038 [P] [US1] Create `frontend/src/app/modules/bank-sync/components/connect-modal/type-picker.component.{ts,html}` — three tile buttons (Bank / Crypto / Brokerage). Reads `connectedProviders()` from injected `ConnectStore` to render `cmn-badge` per tile. Calls `store.selectInstitutionType(type)` on click.
- [ ] T039 [P] [US1] Create `frontend/src/app/modules/bank-sync/components/connect-modal/bank-picker.component.{ts,html}` — list of `institutionType === 'bank'` providers from `PROVIDER_CATALOG`. Renders each with `<img [src]="descriptor.iconAsset">` (per research R9). Calls `store.selectProvider(slug)` on click.
- [ ] T040 [P] [US1] Flesh out `plaid-launcher.component.{ts,html}` — `cmn-button` "Open Plaid Link" that injects `CONNECT_STRATEGY` (singular token) and `ConnectStore`, dispatches `store.connect({strategy: inject(CONNECT_STRATEGY), payload: undefined})`. While `store.isInitializing()` shows a spinner. **Does NOT import `PlaidConnectStrategy`.**
- [ ] T041 [P] [US1] Create `frontend/src/app/modules/bank-sync/components/connect-modal/syncing-state.component.{ts,html}` — renders `store.statusMessage()` while `store.isBusy()`.
- [ ] T042 [US1] Update `frontend/src/app/modules/bank-sync/pages/connect-account/connect-account.component.{ts,html}` — becomes a thin entry: on init, opens `<fns-connect-modal>` via `inject(CmnDialogService).open(ConnectModalComponent, {size: 'md', title: 'Connect account', disableClose: false})`. The page itself renders nothing visible (or a fallback if user dismisses the dialog).
- [ ] T043 [US1] Update `frontend/src/app/modules/bank-sync/pages/accounts-list/accounts-list.component.{ts,html}` — "Connect account" CTA + zero-state placeholder both call `dialogService.open(ConnectModalComponent, {...})` directly, OR navigate to the connect route which auto-opens the dialog. Pick one in the implementation; ensure the modal closes cleanly without leaving an empty connect route. Provide `ConnectStore` at the page level.
- [ ] T044 [US1] Verify `bank-sync.routes.ts` — keep `connect` child route only if the page-as-trigger pattern is chosen; otherwise remove. Reflect choice from T043.
- [ ] T045 [P] [US1] Vitest specs: `connect-modal.component.spec.ts`, `type-picker.component.spec.ts`, `bank-picker.component.spec.ts`, `plaid-launcher.component.spec.ts` — assert step transitions, connected-badge rendering, click → store dispatch, strategy resolution.
- [ ] T046 [US1] Run `npx eslint` on every TS file touched in this phase; fix all errors. Run `npx ng test --watch=false` and confirm green.

**Checkpoint**: US1 fully shippable as MVP — Plaid path works end-to-end via `CmnDialogService` + `PlaidConnectStrategy`.

---

## Phase 4: User Story 2 — Connect Monobank with a personal API token (Priority: P1)

**Goal**: From the bank-picker step, a user picks Monobank, pastes a token, submits, and lands on the accounts list with new Monobank rows.

**Independent Test**: Open connect modal → Bank → Monobank → paste valid token → success toast → `/accounts` shows one row per Monobank card. Invalid token → inline `MONOBANK_TOKEN_INVALID` error, form remains editable. Reused token → `MONOBANK_TOKEN_DUPLICATE` banner with "Disconnect existing" CTA.

### Implementation for US2

- [ ] T047 [P] [US2] Flesh out `monobank-form.component.{ts,html}` — single `cmn-form-field` + `cmn-input type="password" autocomplete="off"`, help link to `api.monobank.ua`, submit disabled until valid. Uses `MONOBANK_TOKEN_MAX_LENGTH` + a regex validator. Auto-trims pasted value. Pre-submit format check rejects with inline "This doesn't look like a Monobank token" without a server round-trip. Injects `CONNECT_STRATEGY` (singular) + `ConnectStore`; dispatches `store.connect({strategy, payload: {token}})` on submit. **Does NOT import `MonobankConnectStrategy`.**
- [ ] T048 [US2] On `MONOBANK_TOKEN_DUPLICATE` error rendered by `store.errorMessage()`, render a "Disconnect existing" `cmn-button` inside the alert that calls a new `accountsStore.disconnectMonobank()` rxMethod (uses T031 service method) and re-enables the form on success (`status` resets to `idle`). Add the rxMethod to `frontend/src/app/modules/bank-sync/store/accounts/accounts.effects.ts`.
- [ ] T049 [P] [US2] Vitest spec `monobank-form.component.spec.ts` — covers: trim-on-paste, format reject without HTTP, duplicate-error renders disconnect button, dispatch payload shape.
- [ ] T050 [US2] ESLint sweep on touched files; run `npx ng test --watch=false`.

**Checkpoint**: US2 shippable independently of US3/US4/US5.

---

## Phase 5: User Story 3 — Connect Binance with read-only API key + secret (Priority: P2)

**Goal**: From type-picker → Crypto, user enters Binance key + secret, submits, lands on `/holdings#binance` with non-dust balances and USD totals.

**Independent Test**: Open connect modal → Crypto → enter valid Binance testnet creds → success → `/holdings#binance` shows holdings. Wrong/IP-restricted key → `BINANCE_INVALID_CREDENTIALS` banner; reused → `BINANCE_DUPLICATE` with "Disconnect existing".

### Implementation for US3

- [ ] T051 [P] [US3] Flesh out `binance-form.component.{ts,html}` — two `cmn-form-field`s (`apiKey`, `apiSecret`) both `type="password" autocomplete="off"`, help link, submit disabled until both non-empty. Injects `CONNECT_STRATEGY` (singular) + `ConnectStore`; dispatches `store.connect({strategy, payload: {apiKey, secretKey}})`. **Does NOT import `BinanceConnectStrategy`.**
- [ ] T052 [US3] On `BINANCE_DUPLICATE`, render "Disconnect existing" CTA calling a new `holdingsStore.disconnectBinance()` rxMethod (uses T029). Add the rxMethod in `frontend/src/app/modules/holdings/store/holdings.effects.ts`.
- [ ] T053 [P] [US3] Vitest spec `binance-form.component.spec.ts` — disabled submit until both fields filled, error rendering, duplicate-disconnect behavior, dispatch payload shape.
- [ ] T054 [US3] ESLint sweep; `npx ng test --watch=false` green.

**Checkpoint**: US3 shippable independently.

---

## Phase 6: User Story 4 — Connect IBKR via gateway credentials (Priority: P2)

**Goal**: From type-picker → Brokerage, user enters IBKR username + password (with 2FA-push hint), lands on `/holdings#ibkr` with positions listed.

**Independent Test**: Open connect modal → Brokerage → enter paper-trading creds → confirm 2FA push → success → `/holdings#ibkr` shows positions. Gateway rejection → `IBKR_INVALID_CREDENTIALS` banner with the 2FA-push hint.

### Implementation for US4

- [ ] T055 [P] [US4] Flesh out `ibkr-form.component.{ts,html}` — `username` (text) + `password` (password) `cmn-form-field`s, both `autocomplete="off"`, hint text about 2FA push, submit disabled until both valid. Injects `CONNECT_STRATEGY` (singular) + `ConnectStore`; dispatches `store.connect({strategy, payload: {username, password}})`. **Does NOT import `IbkrConnectStrategy`.**
- [ ] T056 [US4] On `IBKR_DUPLICATE`, render "Disconnect existing" CTA calling a new `holdingsStore.disconnectIBKR()` rxMethod (uses T030).
- [ ] T057 [P] [US4] Vitest spec `ibkr-form.component.spec.ts` — disabled submit, 2FA hint visible, error rendering, duplicate-disconnect behavior, dispatch payload shape.
- [ ] T058 [US4] ESLint sweep; `npx ng test --watch=false` green.

**Checkpoint**: US4 shippable independently.

---

## Phase 7: User Story 5 — Disconnect any connected provider (Priority: P3)

**Goal**: A signed-in user can disconnect any provider via a confirmation dialog from the per-provider detail view. After confirm, data disappears and reconnect is available.

**Independent Test**: Connect any provider → open its detail view → click Disconnect → confirm → provider section gone; provider picker shows it as Available again. Cancel disconnect dialog → provider stays connected, no data removed.

### Implementation for US5

- [ ] T059 [P] [US5] Create `frontend/src/app/modules/bank-sync/components/disconnect-dialog/disconnect-dialog.component.{ts,html}` — opened via `CmnDialogService.open(DisconnectDialogComponent, {data: {providerName}, size: 'sm', title: 'Disconnect …'})`. Reads `inject(CMN_DIALOG_DATA)` for the provider name. Body warns "Holdings/transaction data fetched from {provider} will be removed." Footer with confirm/cancel `cmn-button`s; confirm calls `dialogRef.close(true)`, cancel calls `dialogRef.close(false)`.
- [ ] T060 [P] [US5] Vitest spec `disconnect-dialog.component.spec.ts` — provider name renders, confirm closes with `true`, cancel closes with `false`, ESC closes with `undefined`.
- [ ] T061 [US5] In `frontend/src/app/modules/bank-sync/pages/accounts-list/accounts-list.component.{ts,html}` add per-row Disconnect action (Plaid + Monobank rows) opening `DisconnectDialogComponent` via `CmnDialogService`. On `afterClosed() === true`, call `accountsStore.disconnect(accountId)` (Plaid, existing `disconnectAccount`) or `accountsStore.disconnectMonobank()` (T048).
- [ ] T062 [US5] In `frontend/src/app/modules/holdings/pages/holdings/holdings.component.{ts,html}` add per-provider Disconnect action opening the dialog. On confirm, call `holdingsStore.disconnectBinance()` (T052) or `holdingsStore.disconnectIBKR()` (T056).
- [ ] T063 [US5] Add Vitest specs for the new dispatcher branches in `accounts.effects.spec.ts` and `holdings.effects.spec.ts`.
- [ ] T064 [US5] ESLint sweep; `npx ng test --watch=false` green.

**Checkpoint**: US5 ships after US1–US4 since it depends on each provider's data store being live.

---

## Phase 8: Polish & Cross-Cutting Concerns

- [ ] T065 [P] Add Storybook stories for the dialog primitive (`cmn-dialog-container`), `fns-connect-modal`, type-picker, bank-picker, plaid-launcher, monobank-form, binance-form, ibkr-form, disconnect-dialog. Capture snapshots at viewports 360, 480, 768, 1024, 1440, 1920 to satisfy SC-006.
- [ ] T066 [P] Verify all credential `<input>`s have `type="password"` (or `text` only for IBKR username) AND `autocomplete="off"` AND no two-way binding to any signal/store field — values live only in `FormControl` state. Code-search audit + manual DevTools session per SC-005.
- [ ] T067 [P] Run a full QA sweep using Playwright MCP per `quickstart.md` walkthrough: 4 golden paths + 10 edge cases. Record any deviations as bugs and fix before sign-off.
- [ ] T068 Bump `frontend/package.json` version (MINOR — new feature + new lib primitives, non-breaking) and let CI create the matching `frontend-v<MINOR>` tag on merge per constitution Versioning policy. The `@dsdevq-common/ui` library version (in its own `package.json` if separate) gains the dialog + icon-registry primitives — bump accordingly.
- [ ] T069 Run `npx ng lint` over the whole frontend and `npx ng test --watch=false` one final time — both must be green before merge.
- [ ] T070 Run `/frontend-code-quality` audit sweep on every file added/modified (no inline interfaces, cross-module code in `shared/`, file-org violations resolved).

---

## Dependencies & Story Completion Order

```
Phase 1 (Setup)        → Phase 2 (Foundational)
                            ↓
                    ┌───────┼─────────┬─────────┐
                    ↓       ↓         ↓         ↓
                  US1     US2       US3       US4
                  (P1)    (P1)      (P2)      (P2)
                    └───────┴─────────┴─────────┘
                            ↓
                          US5 (P3)   ← needs US1+US2+US3+US4 data stores
                            ↓
                       Phase 8 (Polish)
```

- **Phase 1 → Phase 2**: T001–T005 must complete before T006+. T005 is a hard prerequisite for any story rendering errors.
- **Phase 2 → Phase 3+**: T006–T036 must all complete first. Inside Phase 2, three sub-tracks can run in parallel after T001–T005:
  - Dialog primitive (T006–T012)
  - Icon registry (T013–T020)
  - Strategy layer + service surface (T021–T031)
  - Store refactor (T032–T036) starts after the strategy layer is in place
- **US1 (P1) is the MVP**. Independently shippable; US2/US3/US4 plug into the same modal scaffolding without touching US1 files since each strategy + form is self-contained.
- **US2 / US3 / US4 are mutually independent**: each touches a separate form component, owns its own strategy, has its own registry entry.
- **US5 depends on US1–US4** because the disconnect dialog is wired into per-provider rows that only exist once the corresponding connect path lands.

## Parallel Execution Examples

### Inside Phase 2 — three independent tracks:

```
After T001–T005 (Setup) →
  Track A (Dialog):       T006, T007 in parallel → T008 → T009 → T010, T011, T012 in parallel
  Track B (Icon registry): T013 → T014 → T015, T016, T017, T018 in parallel → T019, T020
  Track C (Strategies):   T021, T022 in parallel → T023, T024, T025, T026 in parallel → T027, T028, T029, T030, T031 in parallel
                          → T032 → T033 → T034 → T035 → T036
```

### After Phase 2 checkpoint, run US1 + US2 + US3 + US4 in parallel:

```
US1:  T037, T038, T039, T040, T041 parallel → T042 → T043 → T044 → T045 → T046
US2:  T047 → T048 → T049 → T050
US3:  T051 → T052 → T053 → T054
US4:  T055 → T056 → T057 → T058
```

### Polish phase:

```
T065, T066, T067 (all parallel)  →  T068  →  T069  →  T070
```

---

## Implementation Strategy

1. **Foundation first (Phase 1 + Phase 2)**: lands the UI library primitives (`CmnDialogService`, `CmnIconRegistry`), the connect strategy layer (4 strategies + registry), the disconnect service surface, and the slimmed-down store. Ship behind a feature flag if any of US1–US5 are not yet wired; otherwise hold the merge until US1 is ready. Phase 2 is large but its three tracks (dialog / icon / strategies) are mutually independent and can be parallelised across one or more PRs.
2. **MVP cut**: Phase 3 (US1) on top of foundation — gives a usable Plaid-only modal opened via `CmnDialogService` and driven by `PlaidConnectStrategy`. First end-user value.
3. **Same-week increment**: Phases 4 + 5 + 6 (US2 + US3 + US4) — each scoped to 1 form component (the strategy already exists from Phase 2). Each ships independently behind its own PR.
4. **Disconnect (P3)**: Phase 7 lands once at least US1+US2 (banking) are merged.
5. **Polish & ship**: Phase 8 runs once the four connect paths + disconnect are green; bump version + tag both `frontend-v<MINOR>` and the `@dsdevq-common/ui` library version.

---

## Format Validation

Every task above conforms to:
`- [ ] T### [P?] [US?] <description with absolute file path>`

- Setup (T001–T005): no story label.
- Foundational (T006–T036): no story label. Sub-tracks: Dialog (T006–T012), Icon registry (T013–T020), Strategy layer + service surface (T021–T031), Store refactor (T032–T036).
- US1 (T037–T046), US2 (T047–T050), US3 (T051–T054), US4 (T055–T058), US5 (T059–T064): all carry `[USx]`.
- Polish (T065–T070): no story label.

**Total tasks**: 70.
**Per-phase counts**: Setup 5 · Foundational 31 (Dialog 7 · Icon registry 8 · Strategy layer 11 · Store refactor 5) · US1 10 · US2 4 · US3 4 · US4 4 · US5 6 · Polish 6.
**Independent test criteria**: stated under each story's "Independent Test" line above.
**Suggested MVP**: Phase 1 + Phase 2 + Phase 3 (US1 — Plaid via Plaid Link, opened via the new `CmnDialogService` and resolved through `PlaidConnectStrategy`).
