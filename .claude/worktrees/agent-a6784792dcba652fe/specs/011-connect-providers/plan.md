# Implementation Plan: Connect Bank, Brokerage, and Crypto Providers

**Branch**: `011-connect-providers` | **Date**: 2026-04-25 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/011-connect-providers/spec.md`

## Summary

Replace the current bank-only connect-account screen with a unified, Stitch-designed flow that lets a signed-in user connect any of four providers (Plaid, Monobank, Binance, IBKR) from a single entry point, plus a P3 disconnect flow. The backend, the per-provider services (`BankSyncService`, `BinanceService`, `IBKRService`, `PlaidLinkService`), and the `ConnectStore` (with `type-picker` → provider-form modal steps already modelled) all exist.

The frontend work has three architectural pillars:

1. **First-class dialog primitives in `@dsdevq-common/ui`** — this is the app's first modal, so we land a reusable `CmnDialogService` + `CmnDialogContainerComponent` (CDK-based) in the library. Any future feature opens dialogs through this service; the connect modal is its first consumer.
2. **Hybrid icon registry in `@dsdevq-common/ui`** — `CmnIconRegistry` for raw branded SVGs (provider logos, multi-color, `<text>`-bearing) + a thin `provideLucideIcons()` helper wrapping Lucide's native `LucideIconProvider` for future monochrome custom icons. `cmn-icon` resolves: registry → Lucide built-ins → Lucide custom providers → empty fallback.
3. **Strategy pattern for connect entities** — `ConnectStrategy` interface + one strategy class per provider in `bank-sync/strategies/`, each owning its form component, payload type, service call, and error mapping. The connect modal renders the resolved strategy's form via `*ngComponentOutlet`. Adding a fifth provider = 1 new strategy + 1 form component + 1 catalog entry; the modal stays untouched.

The connect flow itself replaces the current tab-only template with a modal-driven flow using the existing four-step state machine, wires all four providers through their strategies, routes to the correct post-success view (banking → accounts list, crypto/brokerage → holdings), surfaces documented error codes through `ERROR_MESSAGES_REGISTRY`, and adds a per-provider disconnect dialog.

## Technical Context

**Language/Version**: TypeScript 5.x strict, Angular 21.2 (frontend only — no backend changes)
**Primary Dependencies**: `@ngrx/signals` 21.1, `@dsdevq-common/ui` (local lib — gains `CmnDialogService` + `CmnIconRegistry`), `@angular/cdk/dialog` v21.2 (already installed; used to build the dialog primitive), `lucide-angular` v1 (used for the icon registry's Lucide custom-provider helper), Angular ReactiveForms, Plaid Link client SDK (already loaded by `PlaidLinkService`)
**Storage**: N/A on frontend; credentials are transient form state, never persisted
**Testing**: Vitest (`@angular/build:unit-test`) for store + component logic; Playwright MCP for QA after implementation; Storybook responsive snapshots for SC-006
**Target Platform**: Web SPA — viewports 360 px to 1920 px wide, modern evergreen browsers
**Project Type**: Web application — frontend changes inside `frontend/src/app/modules/bank-sync/` and `frontend/src/app/modules/holdings/`
**Performance Goals**: Connect-to-success < 90 s end-to-end (SC-001); UI route to data view within 2 s of backend success (FR-008)
**Constraints**: Zero credential leak surface (SC-005); no `localStorage`/`sessionStorage`/cookie writes of credentials; no horizontal scroll at any supported viewport
**Scale/Scope**: Four providers, one connect modal, one disconnect dialog component, ~3 new shared sub-components inside the bank-sync module, ~7 new entries in `ERROR_MESSAGES_REGISTRY`

## Constitution Check

Verified against constitution v1.3.1.

| Principle | Status | Notes |
|---|---|---|
| I. Modular Monolith Architecture | PASS | Frontend-only; no module boundaries crossed. All provider HTTP calls already go through dedicated services in `bank-sync/services/`. |
| II. Code Quality Enforcement | GATE | `npx eslint <file>` after every `.ts` change; no `dotnet build` (no C# touched). |
| III. Multi-Source Financial Integration | PASS | All four providers consumed via existing typed services; no direct fetches from components. |
| IV. AI-Driven Analytics & Insights | N/A | Out of scope. |
| V. Security-First Financial Data Handling | GATE | All credential `<input>`s are `type="password"` with `autocomplete="off"`; values held only in `FormControl` state, never patched into the store, never logged, never persisted. SC-005 verified once via DevTools before sign-off. |
| VI.1 State in SignalStore, not components | PASS | `ConnectStore` already exists; new forms call `store.connectBinance(...)` / `connectIBKR(...)` / `connectMonobank(...)` only. No `isLoading`/`errorMessage` fields in components. |
| VI.2 Custom providers extracted | N/A | No new app-wide providers. |
| VI.3 Error-code → message centralized | GATE | New error codes (`BINANCE_INVALID_CREDENTIALS`, `BINANCE_DUPLICATE`, `IBKR_INVALID_CREDENTIALS`, `IBKR_DUPLICATE`, `PLAID_DUPLICATE`, `PLAID_SCRIPT_LOAD_FAILED`, `VALIDATION_ERROR`) added to `ERROR_MESSAGES_REGISTRY` in the same PR. No `if/else` ladders in components/stores. |
| VI.4 UI library discipline | GATE | Modal step views use `cmn-card`, `cmn-form-field`, `cmn-input`, `cmn-button`, `cmn-alert`, `cmn-badge`. **`CmnDialogService` + `CmnDialogContainerComponent`** (the dialog primitive) and **`CmnIconRegistry`** (the icon registry) ship in `@dsdevq-common/ui` as new primitives — never built directly in the app. Brand provider logos render via plain `<img>` (correct primitive for branded artwork); `cmn-icon` is reserved for stroke/Lucide-style icons. |
| VI.5 File org & shared/ boundary | GATE | Provider catalog (cross-module: bank-sync + holdings reference it) lives in `shared/constants/providers/providers.constants.ts`. Modal-step components live under `bank-sync/components/connect-modal/`. No inline interfaces, no helper functions outside `*.utils.ts` classes. |

No violations to justify. Complexity Tracking section is empty.

## Project Structure

### Documentation (this feature)

```text
specs/011-connect-providers/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output — references existing backend endpoints
│   └── existing-endpoints.md
├── checklists/
│   └── requirements.md  # Already present
└── tasks.md             # Phase 2 — produced by /speckit.tasks
```

### Source Code (repository root)

```text
frontend/
├── projects/dsdevq-common/ui/src/lib/         # === UI LIBRARY (new primitives) ===
│   ├── components/
│   │   ├── dialog/
│   │   │   ├── dialog-container.component.ts  # NEW: extends CdkDialogContainer; default shell
│   │   │   ├── dialog-config.ts               # NEW: CmnDialogConfig + CMN_DIALOG_DATA token
│   │   │   ├── dialog-ref.ts                  # NEW: CmnDialogRef thin wrapper around DialogRef
│   │   │   └── dialog.spec.ts                 # NEW: open/close/data-injection tests
│   │   └── icon/icon.component.ts             # MODIFY: consult CmnIconRegistry first, then Lucide
│   ├── services/
│   │   ├── dialog/
│   │   │   └── dialog.service.ts              # NEW: CmnDialogService.open<T,R,D>(component, config)
│   │   └── icon-registry/
│   │       └── icon-registry.service.ts       # NEW: CmnIconRegistry (registerInline / registerUrl)
│   ├── providers/
│   │   ├── provide-lucide-icons.ts            # NEW: helper wrapping LucideIconProvider for monochrome customs
│   │   └── provide-custom-icons.ts            # NEW: helper that registers raw-SVG icons via CmnIconRegistry on init
│   └── index.ts                               # MODIFY: export the new primitives
└── src/app/
    ├── shared/
    │   ├── constants/providers/
    │   │   └── providers.constants.ts        # NEW: provider catalog (slug, label, description, iconAsset, formShape)
    │   └── models/provider/
    │       └── provider.model.ts             # NEW: ProviderDescriptor (cross-module type)
    ├── core/
    │   ├── errors/
    │   │   └── error-messages.registry.ts    # MODIFY: add new connect error codes
    │   └── providers/
    │       └── provide-app-icons.provider.ts # NEW: registers provider brand SVGs into CmnIconRegistry on init
    └── modules/
        ├── bank-sync/
        │   ├── strategies/                            # === STRATEGY LAYER ===
        │   │   ├── connect-strategy.ts                # NEW: ConnectStrategy interface + ConnectOutcome type
        │   │   ├── connect-strategy.token.ts          # NEW: CONNECT_STRATEGIES multi-token + ConnectStrategyRegistry
        │   │   ├── plaid.strategy.ts                  # NEW: orchestrates link-token → overlay → exchange
        │   │   ├── monobank.strategy.ts               # NEW
        │   │   ├── binance.strategy.ts                # NEW
        │   │   └── ibkr.strategy.ts                   # NEW
        │   ├── pages/connect-account/
        │   │   ├── connect-account.component.ts       # MODIFY: opens connect modal via CmnDialogService
        │   │   └── connect-account.component.html     # REWRITE: thin entry — modal hosts the flow
        │   ├── components/connect-modal/              # NEW: modal contents
        │   │   ├── connect-modal.component.ts         # hosts the step machine; renders strategy form via *ngComponentOutlet
        │   │   ├── type-picker.component.ts           # bank | crypto | broker tiles
        │   │   ├── bank-picker.component.ts           # Plaid | Monobank tiles
        │   │   ├── plaid-launcher.component.ts        # Plaid strategy's "form" — a launch button
        │   │   ├── monobank-form.component.ts         # Monobank strategy's form
        │   │   ├── binance-form.component.ts          # Binance strategy's form
        │   │   └── ibkr-form.component.ts             # IBKR strategy's form
        │   ├── components/disconnect-dialog/          # NEW: shared confirmation dialog (P3)
        │   ├── store/connect/
        │   │   ├── connect.methods.ts                 # MODIFY: simplify — strategies own per-provider logic
        │   │   ├── connect.effects.ts                 # MODIFY: single `connect(slug, payload)` rxMethod resolves strategy
        │   │   └── …spec.ts                           # MODIFY: cover strategy-resolution path
        │   ├── services/
        │   │   ├── binance.service.ts                 # MODIFY: add disconnect()
        │   │   └── ibkr.service.ts                    # MODIFY: add disconnect()
        │   └── pages/accounts-list/                   # MODIFY: open the new connect modal from CTA
        └── holdings/
            └── pages/holdings/                        # MODIFY: per-provider Disconnect button → dialog
```

**Structure Decision**: Frontend-only changes split across three layers:

1. **`@dsdevq-common/ui` library** gains two new primitive groups: dialog (service + container + ref + config token) and icon registry (service + two `provide*()` helpers). These are reusable across the app.
2. **`bank-sync/strategies/`** is the new home of the connect strategy layer — one class per provider, registered via `CONNECT_STRATEGIES` multi-token and resolved by `ConnectStrategyRegistry`.
3. **`bank-sync/pages/connect-account/` + `bank-sync/components/connect-modal/`** become thin shells: the page opens the modal via `CmnDialogService`, the modal hosts the step machine and renders the resolved strategy's form via `*ngComponentOutlet`.

Cross-module provider metadata still lives in `shared/`. No backend, no migrations.

## Complexity Tracking

> No constitution violations. Section intentionally empty.
