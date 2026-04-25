---
name: ui-library-sync
description: Analyze a Stitch screen design, extract reusable UI patterns, build them as cmn-* components in @dsdevq-common/ui (with Vitest tests + Storybook stories), sync design tokens from the active Stitch design system into tailwind.config.js and theme.css, then produce a component checklist for Angular screen implementation. Use when a Stitch screen has been generated and needs to be turned into real Angular code backed by the shared library.
user-invocable: true
allowed-tools:
  - Read
  - Write
  - Edit
  - Glob
  - Grep
  - Bash(cd frontend && npx ng build @dsdevq-common/ui*)
  - Bash(cd frontend && npx ng test --watch=false*)
  - Bash(cd frontend && npx eslint *)
  - Bash(find *)
  - Bash(ls *)
  - mcp__stitch__get_screen
  - mcp__stitch__list_screens
  - mcp__stitch__get_project
  - mcp__stitch__list_design_systems
---

# UI Library Sync

Bridge the gap between a Stitch screen design and Angular implementation. Every reusable visual pattern becomes a `cmn-*` component in `@dsdevq-common/ui` — with tests and Storybook — before the screen is touched. Design tokens flow from the Stitch design system into Tailwind tokens and CSS custom properties.

## Library paths (do not deviate)

```
Library root:   frontend/projects/dsdevq-common/ui/src/lib/
Components:     .../lib/components/<name>/
Directives:     .../lib/directives/<name>/
Services:       .../lib/services/<name>/
Tokens:         .../lib/tokens/
Barrel:         .../lib/index.ts          ← add every new export here
Public API:     .../src/public-api.ts     ← re-exports lib/index (do not modify)
Tailwind:       frontend/tailwind.config.js
Theme CSS:      frontend/src/styles/theme.css  (or projects/.../theme.css — verify path)
```

## Active Stitch project

- Project ID: `2377738634696453555` (Finance Sentry Wealth Dashboard)
- Design system: `assets/c71e9082d1d14e9dbcc6796dbb1f0ba3`

## Existing library components (already built — do not recreate)

| Selector | File | Notes |
|---|---|---|
| `cmn-button` | `button/button.component.ts` | variants: primary/secondary/destructive; sizes: sm/md/lg |
| `cmn-card` | `card/card.component.ts` | generic container |
| `cmn-alert` | `alert/alert.component.ts` | inline feedback |
| `cmn-icon` | `icon/icon.component.ts` | Lucide wrapper |
| `cmn-input` | `input/input.component.ts` | text input |
| `cmn-form-field` | `form-field/form-field.component.ts` | label + input + error |
| `cmn-toast` | `toast/toast.component.ts` | notification overlay |
| `google-sign-in-button` | `google-sign-in-button/...` | OAuth |
| `[cmnTypography]` | `directives/typography/` | text style directive |

Check this list against the filesystem before deciding a component is new:
`Glob: frontend/projects/dsdevq-common/ui/src/lib/components/**/*.ts`

---

## Invocation

Called as `/ui-library-sync` or `/ui-library-sync <screen-name-or-id>`.

- **With a screen ID or name** (e.g. `/ui-library-sync dashboard`): use it directly — call `mcp__stitch__list_screens` to resolve a name to an ID if needed.
- **Without an argument**: call `mcp__stitch__list_screens` (project `2377738634696453555`), display the list, and ask the user which screen to sync. Wait for the answer before proceeding.

---

## Phase 1 — Component Audit

1. Pull the target screen with `mcp__stitch__get_screen` (project `2377738634696453555`).
2. Read the screen's layout description and identify every distinct visual pattern. Classify each as:
   - **Atom**: self-contained, no children (badge, stat-number, status-dot, pill, avatar, skeleton)
   - **Molecule**: composes atoms (stat-card, data-row, filter-bar, table-row, sidebar-item)
   - **Organism**: page-level section (data-table, sidebar-nav, top-bar, kpi-grid)
3. For each pattern, check if an existing `cmn-*` component covers it (see table above + filesystem check).
4. Output a **component inventory table**:

   | Pattern | Category | Already exists? | cmn-name | Action |
   |---|---|---|---|---|
   | KPI stat card | molecule | no | `cmn-stat-card` | CREATE |
   | Filter pill | atom | no | `cmn-filter-pill` | CREATE |
   | Data table | organism | no | `cmn-data-table` | CREATE |
   | Primary button | atom | yes (`cmn-button`) | — | REUSE |

5. For each CREATE entry, evaluate whether it is a better fit for a **third-party library** than a hand-built `cmn-*` component. Apply this heuristic:

   | Signal | Lean toward |
   |---|---|
   | Complex interactivity (drag-drop, virtual scroll, rich date picker, charts) | Third-party |
   | Accessibility-heavy (combobox, dialog, tooltip with ARIA) | Third-party (e.g. Angular CDK) |
   | Data visualization (line chart, donut, bar, heatmap) | Third-party |
   | Simple display-only or layout (badge, stat card, filter pill, skeleton) | Build in library |
   | Already exists in Angular CDK as a behavior primitive | Wrap CDK in a `cmn-*` component |

   Mark candidates with a **THIRD-PARTY?** flag in the inventory table:

   | Pattern | Category | Already exists? | cmn-name | Action |
   |---|---|---|---|---|
   | KPI stat card | molecule | no | `cmn-stat-card` | CREATE |
   | Filter pill | atom | no | `cmn-filter-pill` | CREATE |
   | Data table | organism | no | `cmn-data-table` | THIRD-PARTY? |
   | Line chart | organism | no | `cmn-line-chart` | THIRD-PARTY? |

6. **Stop and present the inventory table to the user.** For every THIRD-PARTY? row, ask: "What library should I use for `<pattern>`?" Wait for the answer before proceeding. The user will name the library (e.g. `@swimlane/ngx-charts`, `ag-grid-angular`, `@angular/cdk`).

   All third-party components are **always wrapped** in a `cmn-*` component — never used directly in app screens. The `cmn-*` wrapper is where design tokens are applied, the public API is narrowed to what Finance Sentry actually needs, and Storybook coverage lives. Update the table with the chosen library and action = WRAP, then continue to Phase 2.

   **Wrapper rule**: keep wrappers thin. Expose only the inputs the app actually uses — do not proxy the full third-party API. Example: `cmn-line-chart` accepts `data: ChartPoint[]` and `currency: string`, not raw chart library config objects.

---

## Phase 2 — Design Token Sync

Pull the active design system via `mcp__stitch__list_design_systems` (project `2377738634696453555`).

Map the Stitch tokens to the library's Tailwind/CSS layer. The mapping rules:

### Colors → `tailwind.config.js` + `theme.css`

Stitch `namedColors` → CSS custom properties in `theme.css`, referenced as `var(--color-*)` in `tailwind.config.js`.

Canonical semantic token names (use these; do not invent new names):

| Stitch token | CSS var | Tailwind key |
|---|---|---|
| `primary` | `--color-accent-default` | `accent-default` |
| `primary_container` | `--color-accent-subtle` | `accent-subtle` |
| `on_surface` | `--color-text-primary` | `text-primary` |
| `on_surface_variant` | `--color-text-secondary` | `text-secondary` |
| `surface_container_lowest` | `--color-surface-card` | `surface-card` |
| `surface_container_low` | `--color-surface-bg` | `surface-bg` |
| `surface_container_high` | `--color-surface-raised` | `surface-raised` |
| `outline_variant` | `--color-border-default` | `border-default` |
| `outline` | `--color-border-strong` | `border-strong` |
| `primary` | `--color-border-focus` | `border-focus` |
| `status-green` | `--color-status-success` | `status-success` |
| `status-red` | `--color-status-error` | `status-error` |
| `status-amber` | `--color-status-warning` | `status-warning` |
| `error` | `--color-status-error` | (same as above) |
| `on_primary` | `--color-text-inverse` | `text-inverse` |

If the Stitch design system changes the primary color (e.g. switching from `#1E3A8A` to `#4F46E5`), update `--color-accent-*` cascade in `theme.css`. Never hardcode hex values in Tailwind — always go through CSS vars.

### Typography → `tailwind.config.js`

Stitch typography scales map to the `fontFamily` and `fontSize` extends:

| Stitch font role | Tailwind key |
|---|---|
| `headlineFont` | `headline` in `fontFamily` |
| `bodyFont` | `base` in `fontFamily` |
| `labelFont` | `label` in `fontFamily` |

For the active design system (Inter-only), set all three to `['Inter', 'system-ui', 'sans-serif']`. Update `tailwind.config.js` if they differ from the current values.

### Roundness → `tailwind.config.js` `borderRadius`

| Stitch roundness | `cmn-sm` | `cmn-md` | `cmn-lg` |
|---|---|---|---|
| ROUND_FOUR | 2px | 4px | 8px |
| ROUND_EIGHT | 4px | 8px | 12px |
| ROUND_TWELVE | 6px | 12px | 16px |
| ROUND_FULL | 4px | 8px | 9999px |

Active design system is `ROUND_EIGHT` — cards use `cmn-lg` (12px), buttons/inputs use `cmn-md` (8px).

After updating `tailwind.config.js`, verify no existing component breaks by grepping for `rounded-cmn-` usage:
`Grep: rounded-cmn- in frontend/projects/dsdevq-common/ui/src/`

---

## Phase 3 — Component Creation

For each component with action = CREATE, follow this exact pattern:

### File structure (per component)

```
frontend/projects/dsdevq-common/ui/src/lib/components/<name>/
  <name>.component.ts       ← class + template + styles (inline, no separate files)
  <name>.component.spec.ts  ← Vitest unit tests
  <name>.stories.ts         ← Storybook stories
```

### Component rules (non-negotiable)

- `ChangeDetectionStrategy.OnPush` (standalone is default in Angular 19+ — do NOT add `standalone: true`)
- Selector: `cmn-<name>` (kebab, always `cmn-` prefix)
- All inputs via `input<T>()` signal, outputs via `output<T>()`
- Explicit access modifiers on all class members (`public`/`private`)
- `inject()` only — no constructor injection
- No magic numbers — extract to named `const` above the class
- Use existing Tailwind tokens only (`accent-default`, `surface-card`, `cmn-md`, etc.) — never raw hex or arbitrary values
- No inline `style=""` attributes
- Use `cmn-icon` for all icons (never raw SVG or `<img>`)

### Test rules

```ts
// Minimal pattern — use TestBed.runInInjectionContext for stores/services
import { TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { ComponentFixture } from '@angular/core/testing';
```

- Test the component's `@Input` signal bindings produce correct CSS classes (follow `button.component.spec.ts` pattern if it exists)
- Test empty/default state renders without error
- At minimum: one smoke test (`should create`) + one per variant/state that has branching logic

### Storybook story rules

```ts
import type { Meta, StoryObj } from '@storybook/angular';
import { ComponentName } from './component-name.component';

const meta: Meta<ComponentName> = {
  title: 'Components/<Name>',
  component: ComponentName,
  tags: ['autodocs'],
};
export default meta;
type Story = StoryObj<ComponentName>;
```

- One story per distinct visual state / variant
- Use `render: args => ({ props: args, template: '...' })` for stories that need content projection
- Label stories: `Default`, `WithData`, `Empty`, `Loading`, `Error` — match the component's actual states

### After creating each component

1. Add export to `frontend/projects/dsdevq-common/ui/src/lib/index.ts`
2. Run `cd frontend && npx eslint projects/dsdevq-common/ui/src/lib/components/<name>/<name>.component.ts` — fix all errors before moving on
3. Do not run the full library build until all components in the batch are done (Phase 4)

---

## Phase 4 — Build & Validate

Run in order — stop on first failure and fix before continuing:

```bash
# 1. Lint all new/modified library files
cd frontend && npx eslint projects/dsdevq-common/ui/src/lib/ --ext .ts

# 2. Build the library
cd frontend && npx ng build @dsdevq-common/ui

# 3. Run unit tests
cd frontend && npx ng test --watch=false
```

Common failures:
- **`NG8001` unknown element**: you used a `cmn-*` component in a template but forgot to add it to `imports: []`
- **`TS2345` type error on input**: signal inputs need explicit generic — `input<string>('')` not `input('')`
- **ESLint `explicit-module-boundary-types`**: return types on store factory functions — but library components are NOT stores, so return types ARE required here

---

## Phase 5 — Handoff

Produce a **screen implementation checklist** listing exactly which `cmn-*` components map to each section of the Stitch screen. Format:

```
Screen: Dashboard

Shell
  ✦ Sidebar nav       → cmn-sidebar-nav (NEW — built in this session)
  ✦ Top bar           → cmn-top-bar (NEW)

KPI row (top)
  ✦ 4× stat cards     → cmn-stat-card (NEW)

Charts section
  ✦ Line chart        → cmn-line-chart (NEW) or third-party (decide)
  ✦ Donut chart       → cmn-donut-chart (NEW) or third-party (decide)

Recent activity
  ✦ Table             → cmn-data-table (NEW)
  ✦ Row               → cmn-data-row (NEW)
  ✦ Status badge      → cmn-badge (NEW)

All built. Ready for Angular screen implementation.
```

Third-party library decisions were already resolved in Phase 1. All components listed here are either hand-built `cmn-*` or confirmed third-party wrappers.

---

## Anti-patterns

- ❌ Never build a component directly in the app (`frontend/src/`) — always library-first
- ❌ Never hardcode design values (hex, px) in component files — go through Tailwind tokens
- ❌ Never modify `public-api.ts` directly — it only contains `export * from './lib/index'`
- ❌ Never skip the ESLint gate between writing a component and moving to the next one
- ❌ Never create a `<name>.component.html` or `<name>.component.scss` file — keep template and styles inline in the `.ts` file (follow the button pattern)
