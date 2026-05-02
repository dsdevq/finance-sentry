# Finance Sentry — Claude Context

> **Source of truth split**: For architecture principles, testing requirements, code quality gates, and branching rules — the constitution at [`.specify/memory/constitution.md`](.specify/memory/constitution.md) is authoritative. This file covers **current state only** (what's built, what's running, what's next). When in doubt, constitution wins.

## Project Overview

Finance Sentry is a personal finance aggregation app built as an ASP.NET Core 9 modular monolith + Angular 20 SPA. It integrates with Plaid for bank data, with plans for Interactive Brokers, Binance, and AI-driven portfolio analytics.

Sole developer: Denys. Spec-driven development via the **speckit** toolchain (constitution → spec → plan → tasks → implement).

---

## Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 9, EF Core, PostgreSQL 14, MediatR (CQRS), Hangfire, Serilog |
| Frontend | Angular 21.2, TypeScript strict, standalone components, NgRx SignalStore (`@ngrx/signals`), lazy-loaded modules |
| UI library | `@dsdevq-common/ui` (local, ng-packagr) — components, `ToastService`, `ErrorMessageService`, `ThemeService` |
| Auth | Custom `JwtAuthenticationMiddleware` (backend) + `AuthStore` signal store + functional `authInterceptor` (frontend). Access token lives **in memory only** (store signal); refresh token is an httpOnly/Secure/SameSite=Strict cookie set by the backend. Silent refresh fires on app init. |
| Infra | Docker Compose (single file for full stack) |

---

## How to Run

Everything runs in Docker:

```bash
cd docker
docker compose -f docker-compose.dev.yml up -d --build
```

Startup order enforced by health checks: `postgres → api → frontend`

| Service | URL |
|---|---|
| Frontend (Angular) | http://localhost:4200 |
| Backend API | http://localhost:5001/api/v1 |
| Health check | http://localhost:5001/api/v1/health |
| Swagger | http://localhost:5001/swagger |
| Hangfire dashboard | http://localhost:5001/hangfire |
| PostgreSQL | localhost:5432 (user: finance_user / pw: finance_password / db: finance_sentry) |

For faster frontend iteration, run `ng serve` locally while keeping API + DB in Docker:

```bash
# Terminal 1
cd docker && docker compose -f docker-compose.dev.yml up -d postgres api

# Terminal 2
cd frontend && npm start
```

---

## Key Files

```
backend/
  src/
    FinanceSentry.API/
      Program.cs                          # DI registrations, middleware pipeline
    FinanceSentry.Modules.BankSync/
      API/
        Controllers/                      # REST controllers
        Middleware/
          JwtAuthenticationMiddleware.cs  # JWT validation; exempt: /health, /api/v1/health, /swagger, /api/webhook, /hangfire
      Application/                        # CQRS commands/queries (MediatR)
      Domain/                             # Entities, interfaces, repositories
      Infrastructure/                     # EF Core, Plaid HTTP client, encryption

frontend/
  src/app/
    app.routes.ts                         # / → /accounts (lazy bank-sync module), /dashboard, /login, /register
    app.config.ts                         # provideRouter + provideHttpClient(withInterceptors([authInterceptor])) + provideErrorHandler() + provideErrorMessages()
    core/
      providers/                          # provide*() factories returning EnvironmentProviders (one per concern)
      errors/error-messages.registry.ts   # app-owned Record<errorCode, message> consumed by @dsdevq-common/ui's ErrorMessageService
      handlers/http-error.handler.ts      # global ErrorHandler → toasts
    shared/
      enums/app-route.enum.ts             # route literals
      utils/                               # cross-module pure helpers (e.g. getRelativeTime)
    modules/auth/
      store/                              # auth.state/computed/methods/effects/store.ts + specs
      services/auth.service.ts            # HTTP-only (no state)
      interceptors/auth.interceptor.ts    # reads AuthStore.token(), refreshes on 401
      guards/                             # authGuard / guestGuard — signal-based
      pages/login · register              # declarative components bound to AuthStore signals
      validators/password-match.validator.ts
    modules/bank-sync/
      store/dashboard                     # DashboardStore (component-scoped via providers) — RxJS timer refresh
      store/accounts                      # AccountsStore — list + disconnect
      services/bank-sync.service.ts       # HTTP-only
      pages/                              # accounts-list, connect-account, transaction-list, dashboard

docker/
  docker-compose.dev.yml                  # Full stack: postgres + api + frontend
  Dockerfile                              # Multi-stage backend build (includes NuGet.Config copy)
  Dockerfile.frontend                     # Node 22 Alpine, ng serve

.specify/
  memory/constitution.md                  # Project governance (v1.2.0)
  specs/001-bank-account-sync/            # Feature spec, plan, tasks for bank sync
```

---

## Current App State

**What works:**
- Full Docker stack runs and all three containers are healthy
- API health check: `GET /api/v1/health` → `{"status":"healthy"}`
- Auth: login/register/Google sign-in. Access token is held in-memory by `AuthStore` (no localStorage); refresh token is an httpOnly cookie. `authInterceptor` attaches `Bearer` from the store and refreshes on 401. Silent refresh on app init hydrates the session from the cookie. `authGuard`/`guestGuard` protect routes.
- State: `AuthStore`, `DashboardStore`, `AccountsStore` built as NgRx SignalStores with feature-file split (state/computed/methods/effects/store)
- Vitest unit tests covering the signal stores (run with `npx ng test --watch=false`)
- All bank-sync pages render (accounts list, connect, transactions, dashboard)
- Backend: accounts, transactions, sync, webhook, dashboard endpoints all implemented

**Frontend state sweep — remaining:**
- `connect-account.component.ts` still owns Plaid init, state, and error mapping → extract `ConnectStore`, move local errorCode ladder to `ERROR_MESSAGES_REGISTRY`
- `transaction-list.component.ts` → `TransactionsStore`
- `sync-status.component.ts` polling → fold into `AccountsStore` or its own store

**Known broken:** integration tests in `frontend/tests/integration/bank-sync/` are stale (`BankAccount.provider` field, `exchangePublicToken` signature) — they are Playwright e2e, not part of the Vitest unit suite.

---

## Open Follow-ups from speckit.analyze (2026-04-11)

From the cross-artifact analysis of `specs/001-bank-account-sync/`:

- `spec.md`: Resolve `[NEEDS CLARIFICATION]` webhook note — record formal `[DECISION]`
- `plan.md`: Update constitution reference from v1.0.0 → v1.1.1; add versioning compliance row
- `tasks.md`: Add IBankProvider interface task (C2), Phase 4/5 contract test tasks (C3), migration task for `archived_reason` column (H4), re-auth frontend flow task (H5)

---

## Frontend ESLint — mandatory gate

After writing or modifying **any** Angular `.ts` file, run `npx eslint <file>` from `frontend/` and fix all errors before moving on. Non-negotiable rules (see constitution § II for the full list):
- `inject()` only — no constructor parameter injection
- `ChangeDetectionStrategy.OnPush` on every component
- Do **not** add `standalone: true` to `@Component` / `@Pipe` / `@Directive` — it is the default in Angular 19+ and is dead boilerplate
- Selector prefix: `fns-` (e.g. `fns-login`, `fns-dashboard`)
- Explicit access modifiers on all class members (`public`/`private`)
- No magic numbers — extract to named constants
- camelCase class properties, no underscore prefix
- Run `eslint --fix` after writing imports (auto-sorts + auto-formats)

---

## Backend Build Gate — mandatory

After writing or modifying **any** `.cs` file, run `dotnet build backend/` and fix **all warnings** before moving on. Non-negotiable:
- Remove unused `using` directives (`IDE0005`)
- Apply primary constructor where suggested (`IDE0290`)
- Resolve nullable reference warnings (`CS8618`, `CS8600`–`CS8604`) — do not suppress with `!` without a comment
- Apply safe IDE suggestions (`IDE0161`, `IDE0028`, `IDE0059`) that do not change runtime behaviour
- Zero warnings before the task is marked complete — same standard as the ESLint gate

Use `/csharp-quality` for a batch cleanup sweep across multiple files.

---

## UI Component Library Rule

**Any new UI component MUST be created in `@dsdevq-common/ui` first.** Components are never built directly in the host Angular app (`frontend/`). This applies to all future features, starting with 005-ui-component-library. The `cmn-` selector prefix is reserved for library components.

**Before writing any Angular template or UI element**, always check `frontend/projects/dsdevq-common/ui/src/lib/components/` first. Use `cmn-button`, `cmn-input`, `cmn-form-field`, `cmn-alert`, `cmn-card`, etc. — never raw `<input>`, `<button>`, or `<div class="error">` when the library already has the component.

---

## File Organisation Rule (Frontend)

In Angular modules, each concept lives in its own file — no mixing:
- **Interfaces / types** → `models/<entity>/<entity>.model.ts` (or `*.types.ts` for type-alias-only files)
- **Domain constants** → `constants/<entity>/<entity>.constants.ts` (separate sibling tree to `models/`); page-only UI constants → `<page>.constants.ts` next to the page
- **Component class** → `*.component.ts` (no inline interface or constant definitions)
- **Service class** → `*.service.ts` (HTTP-only; no state, no inline interfaces — import from model files)
- **State** → `<feature>/store/*.state.ts` · `*.computed.ts` · `*.methods.ts` · `*.effects.ts` · `*.store.ts` (see State Management rule)
- **Validators** → `<feature>/validators/*.validator.ts`

**Shared rule**: any type, constant, utility, or enum used by more than one feature module MUST live in `frontend/src/app/shared/`. Never define cross-module concerns inside a feature folder. If a piece of code is imported by two or more modules, move it to `shared/` in the same PR.

This is a **hard gate** — inline interfaces in component files and cross-module code outside `shared/` block PR merge (see constitution Principle VI.5). Use `/frontend-code-quality` for an audit sweep.

---

## Frontend State Management — NgRx SignalStore

State belongs in a `signalStore()` under `modules/<feature>/store/`, **never** in component classes. Components are declarative: form definitions, template bindings, one-line dispatch handlers. No `ngOnInit` fetches, no `effect()` in components, no local `isLoading`/`errorMessage` fields.

**Mandatory file split** (per store):

| File | Role |
|---|---|
| `<name>.state.ts` | `interface <Name>State`, literal unions, `initial<Name>State` |
| `<name>.computed.ts` | `<name>Computed(store)` — pure derivations (`isLoading`, `errorMessage`, etc.) |
| `<name>.methods.ts` | `<name>Methods(store)` — synchronous `patchState` mutations only |
| `<name>.effects.ts` | `<name>Effects(store)` — `rxMethod`s for HTTP/async; `<name>Hooks(store)` for router subscriptions and signal effects |
| `<name>.store.ts` | `signalStore(..., withState(initial), withMethods(methods), withComputed(computed), withMethods(effects), withHooks({onInit: hooks}))` |

Rules:
- **Do not annotate return types on `*Methods`, `*Computed`, `*Effects` factories** — `withMethods` composition collapses explicit interfaces to `MethodsDictionary` and breaks `inject`. The `eslint.config.mjs` override for `**/store/**/*.ts` turns off `explicit-module-boundary-types` exactly for this reason.
- **App-wide stores** (e.g. `AuthStore`) use `{providedIn: 'root'}`. **Page-scoped stores** (e.g. `DashboardStore`) are provided on the component via `providers: [Store]` — they tear down with the route.
- **No `setInterval`.** Periodic refresh uses `timer(ms, ms).pipe(switchMap(...))` inside an `rxMethod` in `*.effects.ts`, kicked off by `onInit`.
- **No component subscriptions.** Components inject the store and bind `store.someSignal()` in templates. For flows, call `store.someMethod(payload)` and rely on computed signals for loading/error feedback.
- Unit tests live next to the files (`*.spec.ts`), use `TestBed.runInInjectionContext` and `signalState(initialState)` for lightweight fixtures. Run with `npx ng test --watch=false` (Vitest via `@angular/build:unit-test`).

---

## Type Unification — extract narrow shared bases as duplication appears

When ≥3 model interfaces share the same fields with identical types, extract a structural base into `shared/models/<base-name>/<base-name>.model.ts` and refactor consumers to `extends`. Currently in place: `AccountIdentity` (account identifier fields) and `Timestamped` (`createdAt`). The `frontend-type-unification` skill covers the audit + extract loop and the criteria for *refusing* to extract (type divergence, optional/required mismatch, n=2 duplication, etc.). Don't unify aggressively — duplication of two is fine, hiding legitimate type divergence behind a base is not.

---

## Utility Helpers — always a `*.utils.ts` class

Pure helper functions are NEVER bare `export function`s in a random file. Each helper lives in `<domain>.utils.ts` (e.g. `error.utils.ts`, `time.utils.ts`) under `frontend/src/app/shared/utils/` (cross-module) or `frontend/src/app/modules/<feature>/utils/` (feature-local), as a class with `public static` methods:

```ts
export class TimeUtils {
  public static getRelativeTime(timestamp: Nullable<string>): string { ... }
}
```

Rules:
- One domain per file. `error.utils.ts` holds error helpers, `time.utils.ts` holds time helpers — never mix.
- Methods are `public static`, no instance state, no DI, no `inject()`. If you need DI, make it a service in `services/` instead.
- **Template-bound helpers must have a thin pipe wrapper.** If any `*.html` calls the helper, create `shared/pipes/<name>.pipe.ts` (or `modules/<feature>/pipes/`) whose `transform()` just delegates to the static method. Templates use the pipe; components don't expose the function via `public readonly fooFn = fooFn`.
- Every `*.utils.ts` ships with `<domain>.utils.spec.ts` (Vitest) — one branch per `it`, edge cases (null/undefined/empty), `vi.useFakeTimers()` for time-dependent helpers. Coverage on the util file: 100%.

The `frontend-utils-creation` skill covers the full mechanics; trigger it whenever you're tempted to write a bare helper function.

---

## Custom Providers — always extract

Any provider beyond Angular's built-in `provideX()` helpers (`ErrorHandler`, custom injection tokens, `APP_INITIALIZER`, class-based `HTTP_INTERCEPTORS`, etc.) MUST be extracted to `frontend/src/app/core/providers/<name>.provider.ts`:

```ts
export function provideX(): EnvironmentProviders {
  return makeEnvironmentProviders([{ provide: TOKEN, useValue: ... }]);
}
```

`app.config.ts` then lists `provideX()` calls only. One provider concern per file. Feature-scoped providers live under `modules/<feature>/providers/`. The `angular-provider-extraction` skill enforces this.

---

## Error Message Resolution

Error-code → user-message mapping is centralized. **Do not** add an `if/else` ladder in a component or store.

- Mechanism lives in `@dsdevq-common/ui`: `ERROR_MESSAGES` injection token + `ErrorMessageService.resolve(code)` → `string | null`.
- App provides the registry: `src/app/core/errors/error-messages.registry.ts` holds the flat `Record<string, string>` covering all backend `errorCode` values. Wired via `provideErrorMessages()` in `app.config.ts`.
- Stores consume via `inject(ErrorMessageService)` inside `*.computed.ts`, falling back to a feature-specific default (`'Failed to load dashboard data.'`, `'Invalid email or password.'`, etc.) when `resolve()` returns `null`.
- **When adding a new error code on the backend:** append the message to the registry in the same PR. The `error?.errorCode` extraction helper stays local to `*.effects.ts` (the `extractErrorCode(err)` pattern).

---

## AI Development Pipeline

This project uses a two-model pipeline. [`.specify/memory/pipeline.md`](.specify/memory/pipeline.md) is the **source of truth** for pipeline roles, knowledge store structure, and the per-task implementation loop — read it before starting any implementation session.

**Claude** = planner + orchestrator + reviewer (this session).
**Qwen2.5-coder:14b** (local Ollama) = implementer, called via the `qwen-code` MCP server.

> ~~**Hard rule:** Qwen is the sole code producer. If a Qwen MCP call fails with a connection error, **stop immediately** and report the error — do NOT write implementation code directly as a fallback. Wait for the user to confirm Ollama is back up before retrying. Only bypass this rule if the user explicitly says so.~~ **TEMPORARILY DISABLED** — Claude implementing directly while Qwen/Ollama is unavailable.

- MCP config: `.mcp.json` + `.claude/settings.json` → `enabledMcpjsonServers: ["qwen-code"]`
- MCP is only active in **new sessions** — check `/mcp` to confirm it loaded
- Knowledge rules live in `.specify/knowledge/index.yaml` (30 rules); inject into QWEN.md with `py .specify/integrations/qwen/scripts/inject-knowledge.py`
- Per-task loop: Claude calls Qwen MCP → reads diff → reviews inline → approves or requests fix → commits → next task
- Reviews saved to `.specify/knowledge/reviews/<feature>/<task>.yaml`

---

## QA — Test User Credentials

| Field | Value |
|---|---|
| Email | test@gmail.com |
| Password | Darkfly21 |

This account has connected accounts across Plaid (banking), Monobank (banking), Binance (crypto), and IBKR (brokerage).

### Key test scenarios (check before declaring any fix done)

| Page | Golden path | Key assertions |
|---|---|---|
| **Login** | Enter creds → Submit | Redirects to `/accounts/list`; no JS errors |
| **Accounts** | Load page | Banking/Brokerage/Digital Assets tables render; totalConnections > 0; Net worth shown |
| **Dashboard** | Load page | Total Balance ≠ $0.00 (if accounts exist); category table shows human-readable labels (not `FOOD_AND_DRINK`) |
| **Transactions** | Load page | Transaction rows render; categories human-readable; no spinner stuck |
| **Holdings** | Load page | Summary cards have labels; breakdown table has data |
| **Connect (Plaid)** | Click "Connect Account" → select Plaid | Modal opens; no 422/500 on link token request |
| **Disconnect** | Click Disconnect on any account | Confirmation dialog opens; account removed on confirm |

---

## QA — End-to-End Testing After Implementation

After **all tasks in a feature are complete**, act as a QA engineer: spin up the app and test the feature through the browser using Playwright MCP.

**Steps (mandatory):**
1. Ensure the full Docker stack is running: `cd docker && docker compose -f docker-compose.dev.yml up -d`
2. Wait for health check: `GET http://localhost:5001/api/v1/health` → `{"status":"healthy"}`
3. Open `http://localhost:4200` via Playwright
4. Navigate the golden path of the feature as a real user would — click buttons, fill forms, follow redirects
5. Also test key error/edge cases (invalid input, cancelled flows, etc.)
6. Report findings: what passed, what failed, screenshots of any broken state
7. If bugs are found, fix them (via Qwen) before declaring the feature done

**Tools:** Use `mcp__plugin_playwright_playwright__browser_*` tools — snapshot first, screenshot only when visual proof is needed.

---

## Collaboration Style

- Responses must be short and direct. No trailing summaries — Denys can read the diff.
- Lead with the action, skip preamble.
- One fix at a time. Diagnose before pivoting.
- Never change `Host=postgres` to `localhost` to work around Docker issues — fix Docker instead.
- Never modify connection strings or env config as workarounds — fix the root cause.
- Do not create markdown files at the repo root. Only `README.md` and `CLAUDE.md` belong there. Session artifacts, debug notes, and how-to docs do not get their own files — put relevant content in `README.md` or the appropriate `.specify/` artifact.

## Active Technologies
- TypeScript 5.x strict, Angular 21.2 (frontend only — no backend changes) + `@ngrx/signals` 21.1, `@dsdevq-common/ui` (local lib), Angular ReactiveForms, Plaid Link client SDK (already loaded by `PlaidLinkService`) (011-connect-providers)
- N/A on frontend; credentials are transient form state, never persisted (011-connect-providers)
- C# 13/.NET 9 (backend) · TypeScript 5.x strict / Angular 21.2 (frontend) + ASP.NET Core 9, EF Core 9, MediatR, Hangfire · NgRx SignalStore 21.1, @dsdevq-common/ui (014-subscriptions)
- PostgreSQL 14 — new `detected_subscriptions` table in `SubscriptionsDbContext` (014-subscriptions)

- `@ngrx/signals` 21.1.0 (NgRx SignalStore) — pilot AuthStore 2026-04-24, extended to DashboardStore + AccountsStore same day
- C# 13 / .NET 9 (backend) · TypeScript 5.x strict (frontend) + ASP.NET Core 9, EF Core 9, MediatR, ASP.NET Core Identity (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`), Npgsql.EF Core (backend) · Angular 20, RxJS, Angular standalone routing (frontend) (003-auth-flow)
- PostgreSQL 14 — shared database, separate `AuthDbContext : IdentityDbContext<ApplicationUser>` with independent migrations (003-auth-flow)
- TypeScript 5.3 / Angular 21.2 + Angular CDK (behavior primitives), ng-packagr (library build), Tailwind CSS v3 3.4.x (design tokens via `tailwind.config.js` + CSS custom properties in `styles/theme.css`), Storybook 10 (`@storybook/angular`), chroma-js 3.x (runtime palette generation), Lucide Icons (icon set), Vitest 4 (unit tests), Playwright (visual regression) (005-ui-component-library)
- `localStorage` (theme + accent persistence only) (005-ui-component-library)
- TypeScript 5.3 / Angular 21.2 (strict mode) + `@dsdevq-common/ui` (local library, feature 005), Angular `ReactiveFormsModule`, Angular CLI (006-ui-library-adoption)
- `localStorage` (ThemeService — already implemented in feature 005) (006-ui-library-adoption)
- C# 13 / .NET 9 (backend) · TypeScript 5.x strict (frontend) + ASP.NET Core 9, EF Core 9, MediatR, ASP.NET Core Identity, `Google.Apis.Auth` (new) · Angular 20, RxJS, `@types/google.accounts` (new) (004-adopt-oauth)
- PostgreSQL 14 — `AuthDbContext : IdentityDbContext<ApplicationUser>` — `OAuthStates` table to be DROPPED via new migration (004-adopt-oauth)
- C# 13 / .NET 9 (backend) · TypeScript 5.x strict / Angular 20 (frontend) + ASP.NET Core 9, EF Core 9, MediatR, Hangfire, `System.Net.Http` (no new NuGet packages required — Monobank API is plain REST) (007-monobank-adapter)
- PostgreSQL 14 — existing `BankSyncDbContext`; migration M002 adds `MonobankCredentials` table and modifies `BankAccounts` (007-monobank-adapter)
- C# 13 / .NET 9 (backend only — no frontend changes) + ASP.NET Core 9, EF Core 9, MediatR (existing — no new NuGet packages) (008-wealth-aggregation-api)
- PostgreSQL 14 — read-only queries against existing `BankAccounts` and `Transactions` tables; no new columns or migrations (008-wealth-aggregation-api)
- C# 13 / .NET 9 + ASP.NET Core 9, EF Core 9, MediatR, Hangfire, `System.Net.Http` (no new NuGet packages — Binance is plain REST with HMAC signing via `System.Security.Cryptography`) (009-binance-integration)
- PostgreSQL 14 — new `CryptoSyncDbContext` with migration M001 adding `BinanceCredentials` and `CryptoHoldings` tables; no changes to `BankSyncDbContext` (009-binance-integration)
- C# 13 / .NET 9 + ASP.NET Core 9, EF Core 9, MediatR, Hangfire, `System.Net.Http` (no new NuGet packages — IBKR Client Portal API is plain REST+JSON; no official IBKR .NET SDK exists) (010-ibkr-integration)
- PostgreSQL 14 — new `BrokerageSyncDbContext` with migration M001 adding `IBKRCredentials` and `BrokerageHoldings` tables; no changes to `BankSyncDbContext` or `CryptoSyncDbContext` (010-ibkr-integration)

## Recent Changes
- 003-auth-flow: Added C# 13 / .NET 9 (backend) · TypeScript 5.x strict (frontend) + ASP.NET Core 9, EF Core 9, MediatR, ASP.NET Core Identity (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`), Npgsql.EF Core (backend) · Angular 20, RxJS, Angular standalone routing (frontend)
