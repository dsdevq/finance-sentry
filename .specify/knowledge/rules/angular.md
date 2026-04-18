# Angular Rules

## ANG-001 — Use inject() instead of constructor injection
**Category**: angular
**Source**: constitution § II + CLAUDE.md
**Added**: 2026-04-18

Use the `inject()` function for dependency injection. Never use constructor parameter injection.

```ts
// WRONG
constructor(private service: MyService) {}

// CORRECT
private readonly service = inject(MyService);
```

---

## ANG-002 — ChangeDetectionStrategy.OnPush on every component
**Category**: angular
**Source**: constitution § II
**Added**: 2026-04-18

All components must declare `changeDetection: ChangeDetectionStrategy.OnPush`.

```ts
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
})
```

---

## ANG-003 — Selector prefix must be fns-
**Category**: angular
**Source**: constitution § II
**Added**: 2026-04-18

All host-app component selectors must use the `fns-` prefix in kebab-case.
Library components use `cmn-` prefix (they live in `@dsdevq-common/ui`).

```ts
// CORRECT (host app)
selector: 'fns-login'

// CORRECT (library)
selector: 'cmn-button'
```

---

## ANG-004 — Explicit access modifiers on all class members
**Category**: angular
**Source**: constitution § II
**Added**: 2026-04-18

All class members must have an explicit `public`, `private`, or `protected` modifier.
Constructors use `no-public` (omit `public` on constructor itself).

```ts
// WRONG
name = 'test';

// CORRECT
public name = 'test';
private readonly service = inject(MyService);
```

---

## ANG-005 — No magic numbers
**Category**: angular
**Source**: constitution § II
**Added**: 2026-04-18

Extract numeric literals to named constants. Exceptions: 0, 1, -1, array indexes, readonly class properties.

```ts
// WRONG
setTimeout(() => {}, 3000);

// CORRECT
private readonly DEBOUNCE_MS = 3000;
setTimeout(() => {}, this.DEBOUNCE_MS);
```

---

## ANG-006 — Naming conventions
**Category**: angular
**Source**: constitution § II
**Added**: 2026-04-18

- Class properties: `camelCase` (no underscore prefix)
- Static properties: `UPPER_CASE`
- Module-level variables: `UPPER_CASE` or `camelCase`

---

## ANG-007 — ESLint gate after every Angular TS file
**Category**: angular
**Source**: constitution § II
**Added**: 2026-04-18

After writing or modifying any Angular `.ts` file, run:
```bash
cd frontend && npx eslint <file>
```
Fix all errors before marking the task complete. Non-negotiable.

---

## ANG-008 — New UI components go in @dsdevq-common/ui first
**Category**: angular
**Source**: CLAUDE.md
**Added**: 2026-04-18

Never build UI components directly in `frontend/`. Create them in the library first:
```
packages/ui/src/lib/<component-name>/
```
Then import via `@dsdevq-common/ui` in the host app.

---

## ANG-009 — No NgModules
**Category**: angular
**Source**: CLAUDE.md
**Added**: 2026-04-18

Angular 20 uses standalone components exclusively. Never declare NgModules.
All components, directives, and pipes must be `standalone: true`.

---

## ANG-010 — Import order
**Category**: angular
**Source**: constitution § II
**Added**: 2026-04-18

Import order (enforced by `simple-import-sort`):
1. Third-party packages
2. `@angular/*`
3. Project absolute imports
4. Project relative imports

Run `eslint --fix` to auto-sort after writing imports.
