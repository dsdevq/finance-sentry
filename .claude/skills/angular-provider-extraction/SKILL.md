---
name: angular-provider-extraction
description: Use when adding a custom Angular provider (anything beyond the built-in `provide*()` helpers — e.g. `{provide: ErrorHandler, useClass: X}`, `{provide: SOME_TOKEN, useValue: Y}`, custom `APP_INITIALIZER`, custom `HTTP_INTERCEPTORS` entry). Extract it into a dedicated `provide*()` factory in `frontend/src/app/core/providers/` instead of inlining it in `app.config.ts` or route providers.
allowed-tools:
  - Read
  - Write
  - Edit
  - Bash(ls *)
---

# Angular Provider Extraction — Finance Sentry

Whenever you add a custom provider, do **not** inline it. Extract it into its own factory file under `frontend/src/app/core/providers/`.

## When this applies

Trigger on any provider entry that is **not** already an Angular built-in `provideX()` helper. Examples that must be extracted:

- `{provide: ErrorHandler, useClass: ...}`
- `{provide: SOME_INJECTION_TOKEN, useValue: ...}`
- `{provide: SOME_INJECTION_TOKEN, useFactory: ...}`
- `{provide: HTTP_INTERCEPTORS, useClass: ..., multi: true}` (class-based interceptors; prefer functional `withInterceptors` otherwise)
- `{provide: APP_INITIALIZER, useFactory: ..., multi: true}`
- Any multi-entry custom provider bundle

**Does not apply** to already-idiomatic helpers: `provideRouter`, `provideHttpClient`, `provideAnimations`, `provideStore` (NgRx), etc. — those stay inline in `app.config.ts`.

## What to produce

1. Create `frontend/src/app/core/providers/<name>.provider.ts` — kebab-case, ends in `.provider.ts`.
2. Export a single function `provide<Name>(): EnvironmentProviders`.
3. Use `makeEnvironmentProviders([...])` from `@angular/core` to wrap the provider array.
4. Replace the inline entry in `app.config.ts` (or wherever it lived) with `provide<Name>()`.

## Template

```ts
import {type EnvironmentProviders, ErrorHandler, makeEnvironmentProviders} from '@angular/core';

import {HttpErrorHandler} from '../handlers/http-error.handler';

export function provideErrorHandler(): EnvironmentProviders {
  return makeEnvironmentProviders([{provide: ErrorHandler, useClass: HttpErrorHandler}]);
}
```

Then in `app.config.ts`:

```ts
providers: [
  provideRouter(APP_ROUTES),
  provideHttpClient(withInterceptors([authInterceptor])),
  provideErrorHandler(),
]
```

## Rules

- One provider concern per file. Don't bundle unrelated providers (e.g. error handler + analytics) into one factory — create two.
- If the provider takes configuration (timeout, URL, feature flag), the factory accepts a typed `config` arg: `provideAnalytics(config: AnalyticsConfig): EnvironmentProviders`.
- **Register at the narrowest scope that serves the consumer.** A provider must live where it is actually used, not at app root by default:
  - **App-wide** (`app.config.ts`) — only when every authenticated route or the bootstrapped shell consumes it (auth, error handler, app icons, http interceptors, error-message registry). If only one feature module reads the token, it does not belong here.
  - **Feature route group** (`modules/<feature>/<feature>.routes.ts`, in the route's `providers: []`) — when the feature owns the providers and they should tear down with the route. Use this for things like multi-token strategy registries that only one feature needs.
  - **Page/component** (`@Component({providers: [...]})`) — when the providers' lifetime should match a single page or modal (e.g. page-scoped signal stores, per-form `useValue` tokens supplied via `Injector.create`).
  - **Library** (`@dsdevq-common/ui` `provide*()` helpers) — when the provider belongs to a reusable primitive shipped via the library (e.g. dialog overlay container, icon registry).
- File location follows scope: app-wide → `frontend/src/app/core/providers/`; feature-scoped → `modules/<feature>/providers/` or alongside the consumer (e.g. `modules/<feature>/strategies/provide-*.ts`); library-scoped → inside the library package.
- Return type is always `EnvironmentProviders` — never `Provider[]`. This keeps composition correct with Angular's environment-injector model.
- Never skip the factory for "just one provider" — the point is the pattern, not line count.

## Anti-pattern: feature provider at app root

```ts
// ❌ app.config.ts pulling in a bank-sync-only multi-provider registry
providers: [
  provideRouter(APP_ROUTES),
  provideConnectStrategies(),  // ← only consumed by ConnectStore inside bank-sync
],
```

Move it to the feature's route group so it loads with the feature and tears down with it:

```ts
// ✅ modules/bank-sync/bank-sync.routes.ts
export const BANK_SYNC_ROUTES: Routes = [
  {
    path: '',
    providers: [provideConnectStrategies()],
    children: [/* feature pages */],
  },
];
```

## After extracting

- Run `npx eslint <new file> <app.config.ts>` from `frontend/`.
- Confirm the build still compiles.
