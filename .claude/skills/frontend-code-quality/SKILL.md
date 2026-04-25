---
name: frontend-code-quality
description: On-demand audit of Angular TypeScript files for file organization violations (interfaces/constants inline in components), shared/ boundary violations (cross-module code not in shared/), and ESLint issues. Use after implementing a feature, before opening a PR, or when asked to "clean up", "audit", or "fix code quality" on the frontend.
user-invocable: true
allowed-tools:
  - Read
  - Write
  - Edit
  - Glob
  - Grep
  - Bash(cd frontend && npx eslint *)
  - Bash(find *)
  - Bash(ls *)
---

# Frontend Code Quality Audit

Scans Angular code for organization and style violations, reports them with file:line references, and applies fixes. Run after any feature implementation — before committing.

## Scope

If invoked with a path argument (e.g. `/frontend-code-quality src/app/modules/bank-sync/`), audit only that subtree. Otherwise audit the full `frontend/src/app/` tree.

---

## Phase 1 — File Organization Audit

### Rule: one concept per file

Scan every `.ts` file under the target scope for violations. A violation is any file that contains more than one concept type:

| Violation | Example | Correct location |
|---|---|---|
| Interface defined inside `*.component.ts` | `interface LoginForm { ... }` at top of component | Move to `models/<name>.model.ts` |
| Type alias defined inside `*.component.ts` | `type Status = 'active' \| 'inactive'` in component | Move to `models/<name>.types.ts` |
| Interface defined inside `*.service.ts` | `interface ApiResponse { ... }` in service | Move to `models/<name>.model.ts` |
| Constant defined inside `*.component.ts` | `const MAX_RETRIES = 3` in component body | Move to `<name>.constants.ts` |
| Constant defined inside `*.service.ts` | `const BASE_URL = '/api'` in service | Move to `<name>.constants.ts` |
| Enum defined anywhere except `models/*.ts` | `enum Status { Active, Inactive }` in component | Move to `models/<name>.types.ts` |

**How to detect:**
```
Grep: ^(export )?(interface|type |enum |const [A-Z]) in frontend/src/app/**/*.component.ts
Grep: ^(export )?(interface|type |enum ) in frontend/src/app/**/*.service.ts
```

For each violation found:
1. Record `file:line` and the violating declaration
2. Determine the correct target file (create if it doesn't exist)
3. Extract the declaration to the target file and update the import in the source file

### Rule: store files follow the mandatory split

Every store under `modules/<feature>/store/` MUST be split into exactly these files:
- `<name>.state.ts` — interface + initial state only
- `<name>.computed.ts` — pure derivations only
- `<name>.methods.ts` — patchState mutations only
- `<name>.effects.ts` — rxMethod + hooks only
- `<name>.store.ts` — signalStore composition only

A store that keeps state interface AND computed derivations in the same file is a violation.

```
Glob: frontend/src/app/modules/**/store/**/*.ts
```

For each store file, verify it contains only its declared concept type.

---

## Phase 2 — Shared/ Boundary Audit

### Rule: cross-module code lives in `shared/`

Anything imported by more than one feature module MUST live in `frontend/src/app/shared/`.

**How to detect:**

1. For each file under `modules/<feature>/models/`, `modules/<feature>/utils/`, `modules/<feature>/enums/`, check how many other modules import it:
```
Grep: import .* from '.*<feature>/<path>' in frontend/src/app/modules/
```

2. If a file is imported by 2+ different feature modules → it must move to `shared/`.

3. For helpers/utils:
```
Grep: (export function|export const) in frontend/src/app/modules/**/utils/**/*.ts
```
Check each exported function — if it has no module-specific dependencies (no imports from that module's domain), it belongs in `shared/utils/`.

**Current `shared/` structure:**
```
frontend/src/app/shared/
  enums/app-route.enum.ts
  utils/
```

Any new cross-module item goes under the appropriate subdirectory:
- Types/interfaces → `shared/models/`
- Pure functions → `shared/utils/`
- Enums → `shared/enums/`
- Constants → `shared/constants/`

---

## Phase 3 — ESLint Sweep

Run ESLint across all `.ts` files in the audit scope:

```bash
cd frontend && npx eslint src/app/<scope> --ext .ts
```

Fix all errors before reporting done. Key rules in play:
- `inject()` only (no constructor injection)
- `ChangeDetectionStrategy.OnPush` on every component
- Selector prefix `fns-` on app components
- Explicit access modifiers
- No magic numbers
- camelCase properties, no underscore prefix

Run `npx eslint --fix` first to auto-resolve formatting and import-sort issues, then re-run to see remaining manual fixes.

---

## Phase 4 — Report & Fix

Output a violation report grouped by category:

```
FILE ORGANIZATION VIOLATIONS
─────────────────────────────
frontend/src/app/modules/bank-sync/pages/accounts-list/accounts-list.component.ts
  Line 3: interface BankAccountRow { ... }
  → Move to: modules/bank-sync/models/bank-account-row.model.ts
  → Add import: import { BankAccountRow } from '../../models/bank-account-row.model';

SHARED/ BOUNDARY VIOLATIONS
─────────────────────────────
frontend/src/app/modules/auth/utils/format-currency.ts
  Imported by: bank-sync module AND auth module
  → Move to: shared/utils/format-currency.ts
  → Update imports in both modules

ESLINT ISSUES
─────────────
frontend/src/app/modules/bank-sync/store/accounts.effects.ts
  Line 12: magic number 3000 → extract to named constant POLL_INTERVAL_MS
```

**Apply fixes automatically** for:
- Moving an inline interface/type to a model file (create the file, update the import)
- Moving a constant to a `*.constants.ts` file
- ESLint auto-fixable issues (`eslint --fix`)

**Ask before applying** fixes that:
- Move a file to `shared/` (affects multiple modules — confirm the new path)
- Rename a file (could affect imports in many places)

---

## Anti-patterns to watch for (beyond the explicit rules)

- `//` comments explaining WHAT the code does (only WHY is acceptable)
- `any` type without a comment explaining why it's necessary
- Observable subscriptions stored as class properties without `takeUntilDestroyed`
- `ngOnInit` that fetches data (should be in store's `onInit` hook)
- Inline object literals in template bindings `[style]="{ color: 'red' }"` — extract to computed
