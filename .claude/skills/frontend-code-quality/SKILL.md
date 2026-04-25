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
| Interface defined inside `*.component.ts` | `interface LoginForm { ... }` at top of component | Move to `models/<entity>/<entity>.model.ts` (see entity-folder rule below) |
| Type alias defined inside `*.component.ts` | `type Status = 'active' \| 'inactive'` in component | Move to `models/<entity>/<entity>.types.ts` |
| Interface defined inside `*.service.ts` | `interface ApiResponse { ... }` in service | Move to `models/<entity>/<entity>.model.ts` |
| Domain constant defined inside `*.component.ts` / `*.service.ts` | `const MAX_RETRIES = 3` tied to a domain entity | Move to `constants/<entity>/<entity>.constants.ts` |
| UI-only constant inside `*.component.ts` (not tied to a domain entity) | `const ANIMATION_MS = 300` for a single page | Move to `<page-name>.constants.ts` next to the page component |
| Enum defined anywhere except an `enums/<entity>/` folder | `enum Status { Active, Inactive }` in component | Move to `enums/<entity>/<entity>.enum.ts` |

**How to detect:**
```
Grep: ^(export )?(interface|type |enum |const [A-Z]) in frontend/src/app/**/*.component.ts
Grep: ^(export )?(interface|type |enum ) in frontend/src/app/**/*.service.ts
```

For each violation found:
1. Record `file:line` and the violating declaration
2. Determine the correct target file (create if it doesn't exist)
3. Extract the declaration to the target file and update the import in the source file

### Rule: each entity lives in its own folder, separated by file type

The `models/`, `constants/`, and `enums/` trees are **siblings**, not parents-of-each-other. Each tree contains a per-entity subfolder. An entity's `.model.ts`, `.constants.ts`, and `.enum.ts` live under the **same entity name** but in **different top-level trees**:

```
modules/bank-sync/
  models/
    transaction/
      transaction.model.ts
    plaid/
      plaid.model.ts
  constants/
    plaid/
      plaid.constants.ts
  enums/
    bank-sync-route/
      bank-sync-route.enum.ts
```

Rules:
- Three sibling trees: `models/`, `constants/`, `enums/`. **Never** put a `.constants.ts` inside `models/<entity>/`, never put a `.enum.ts` inside `models/<entity>/`. Each file type has its own tree.
- Within a tree, every file lives in a per-entity folder named for the entity (kebab-case, singular): `models/transaction/transaction.model.ts`, not `models/transaction.model.ts`.
- File names are prefixed with the entity name: `transaction.model.ts`, `plaid.constants.ts`. No bare `model.ts` / `index.ts` re-exports.
- Single-file entities are still in a folder — `enums/app-route/app-route.enum.ts`, not a flat `enums/app-route.enum.ts`. Future siblings for that entity have a home without restructuring later.
- File suffix `.models.ts` (plural) is a violation — use singular `.model.ts`.

**Placement decision (cross-module vs feature-local):**
- Imported by 2+ feature modules OR by code in `shared/` → `frontend/src/app/shared/{models|constants|enums}/<entity>/`
- Imported by exactly one feature module → `frontend/src/app/modules/<feature>/{models|constants|enums}/<entity>/`

**How to detect violations:**

```bash
# Flat files at the root of any of the three trees (should be in a per-entity folder)
find frontend/src/app -path '*/models/*.ts'    -not -path '*/models/*/*'    | grep -v '.spec.ts'
find frontend/src/app -path '*/constants/*.ts' -not -path '*/constants/*/*' | grep -v '.spec.ts'
find frontend/src/app -path '*/enums/*.ts'     -not -path '*/enums/*/*'     | grep -v '.spec.ts'

# Cross-tree contamination — constants or enums living inside models/
find frontend/src/app -path '*/models/*/*.constants.ts'
find frontend/src/app -path '*/models/*/*.enum*.ts'

# Plural .models.ts suffix
find frontend/src/app -name '*.models.ts'
```

**How to fix:**
1. `mkdir -p frontend/src/app/<scope>/{constants|enums}/<entity>/`
2. `git mv` the misplaced file from `models/<entity>/<entity>.constants.ts` to `constants/<entity>/<entity>.constants.ts` (or the equivalent for enums).
3. Update importers — the path segment changes from `/models/<entity>/<entity>.constants` to `/constants/<entity>/<entity>.constants`.
4. `npx tsc --noEmit -p tsconfig.app.json` to confirm imports resolve.

**Does not apply to:**
- Page-scoped constants files (e.g. `pages/connect-account/connect-account.constants.ts`) — those live next to the page component as UI implementation detail, not as domain entities.
- Component / service / pipe / store files — they have their own placement rules.

---

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

Any new cross-module item goes under the appropriate sibling tree, **always wrapped in a per-entity folder** (per the entity-folder rule above):
- Types/interfaces → `shared/models/<entity>/<entity>.model.ts`
- Constants → `shared/constants/<entity>/<entity>.constants.ts`
- Enums → `shared/enums/<entity>/<entity>.enum.ts`
- Pure functions → `shared/utils/<domain>.utils.ts` (utils use a flat `<domain>.utils.ts` file, no per-entity folder)

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
  → Move to: modules/bank-sync/models/bank-account-row/bank-account-row.model.ts
  → Add import: import { BankAccountRow } from '../../models/bank-account-row/bank-account-row.model';

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

- `standalone: true` in `@Component` / `@Pipe` / `@Directive` decorators — standalone is the **default since Angular 19**, the flag is dead boilerplate. Detect via `grep -rn "standalone: true" src projects --include="*.ts"` and remove. Only `standalone: false` should ever appear (and only when downgrading to NgModule for legacy reasons — which we don't have).
- `//` comments explaining WHAT the code does (only WHY is acceptable)
- `any` type without a comment explaining why it's necessary
- Observable subscriptions stored as class properties without `takeUntilDestroyed`
- `ngOnInit` that fetches data (should be in store's `onInit` hook)
- Inline object literals in template bindings `[style]="{ color: 'red' }"` — extract to computed
