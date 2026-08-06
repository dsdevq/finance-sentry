# Implementation Plan: UI Component Library (@dsdevq-common/ui)

**Branch**: `005-ui-component-library` | **Date**: 2026-04-11 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/005-ui-component-library/spec.md`

## Summary

Create `@dsdevq-common/ui` — a standalone Angular 21 component library living inside the existing `frontend/` Angular CLI workspace at `projects/@dsdevq-common/ui/`. The library is the single source of truth for all Finance Sentry UI components, design tokens, and typography. It ships with Storybook 10 for isolated component development, Vitest for unit tests, and Playwright for visual regression. All visual decisions originate from a Stitch-defined design system before any code is written; Stitch informs design token values, which are hand-implemented in `tailwind.config.js` (Tailwind CSS v3). Components are styled exclusively with Tailwind utility classes — no hand-authored CSS for token values. The library supports light/dark theming and runtime dynamic accent color selection (Discord-style) via Tailwind CSS custom properties and chroma-js palette generation. v1 delivers: Button, Input, Form Field, Card, Alert, Icon, Typography scale, ThemeService.

---

## Technical Context

**Language/Version**: TypeScript 5.3 / Angular 21.2  
**Primary Dependencies**: Angular CDK (behavior primitives), ng-packagr (library build), Tailwind CSS v3 3.4.x (design tokens + component styling via `tailwind.config.js`), Storybook 10 (`@storybook/angular`), chroma-js 3.x (runtime palette generation), Lucide Icons (icon set), Vitest 4 (unit tests), Playwright (visual regression)  
**Storage**: `localStorage` (theme + accent persistence only)  
**Testing**: Vitest 4 (unit, >80% coverage), Playwright (visual regression snapshot tests)  
**Target Platform**: Modern browsers — Chrome 120+, Firefox 120+, Safari 17+  
**Project Type**: Angular component library (ng-packagr, consumed via tsconfig path alias)  
**Performance Goals**: Theme switch <100ms perceived, accent palette generation + propagation <200ms  
**Constraints**: WCAG 2.1 AA on all components; no Angular Material; no Karma/Jasmine; no hardcoded visual values; `cmn-` selector prefix; all styling via Tailwind utility classes (no hand-authored CSS for values)  
**Scale/Scope**: 6 components + 1 directive + 1 service; ~5 Storybook stories per component; 1 VRT test per story

---

## Constitution Check

| Gate | Status | Notes |
|---|---|---|
| Principle I — Modular Monolith | ✅ N/A | Frontend-only library; no backend modules affected |
| Principle II — Code Quality | ✅ Required | ESLint with `cmn-` prefix override, TypeScript strict, OnPush on all components, `inject()` only, zero-warning builds. Library has its own scoped ESLint config. |
| Principle III — Multi-Source Integration | ✅ N/A | No financial data involved |
| Principle IV — AI Analytics | ✅ N/A | No AI/LLM integration |
| Principle V — Security | ✅ N/A | Component library; no auth, no financial data, no secrets |
| Testing Discipline | ✅ Required | Unit tests >80% coverage (Vitest). VRT for all stories (Playwright). No REST endpoints → no REST contract tests. |
| Versioning | ⚠️ Clarified | Library has its own `package.json` with independent version (starts `0.1.0`). Host app `frontend/package.json` version bumped when library integration changes the host app's code. See note below. |
| Branching Strategy | ✅ Required | Per-task branches off `005-ui-component-library`; each task gets its own sub-branch. |
| Jira Sync | ✅ Required | All tasks mirrored to Jira (`SCRUM` project) before implementation starts. |

**Versioning note**: `projects/@dsdevq-common/ui/package.json` carries the library version, starting at `0.1.0`. The existing `frontend/package.json` (host app version `0.2.0`) increments independently only when the host app's own source files change. Changes to the library alone do not require a bump of the host app version unless the host app's imports or configuration change. The CI/CD version check applies to whichever `package.json` is modified in a given PR.

---

## Project Structure

### Documentation (this feature)

```text
specs/005-ui-component-library/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output — component API contracts + token model
├── quickstart.md        # Phase 1 output — build, test, storybook instructions
└── tasks.md             # Phase 2 output (/speckit.tasks — not created by /speckit.plan)
```

### Source Code

```text
frontend/
├── angular.json                              ← add @dsdevq-common/ui library project
├── tsconfig.json                             ← add @dsdevq-common/ui path alias
├── package.json                              ← add build:lib, storybook, test:lib, test:vrt scripts
│
├── projects/
│   └── @dsdevq-common/
│       └── ui/                               ← NEW: Angular library root
│           ├── ng-package.json               ← ng-packagr config (entryFile: src/public-api.ts)
│           ├── package.json                  ← name: @dsdevq-common/ui, version: 0.1.0
│           ├── tsconfig.lib.json
│           ├── tsconfig.spec.json
│           ├── .storybook/
│           │   ├── main.ts                   ← Storybook 10 Angular builder config
│           │   └── preview.ts                ← global decorators, theme switcher addon
│           ├── playwright.config.ts          ← VRT config (webServer → storybook)
│           ├── e2e/
│           │   ├── visual-regression/        ← one .spec.ts per component
│           │   └── screenshots/              ← Playwright snapshot baselines
│           └── src/
│               ├── public-api.ts             ← single barrel export
│               ├── styles/
│               │   └── theme.css             ← CSS custom properties (:root + [data-theme="dark"]), @tailwind directives
│               └── lib/
│                   ├── components/
│                   │   ├── button/           ← cmn-button
│                   │   ├── input/            ← cmn-input (ControlValueAccessor)
│                   │   ├── form-field/       ← cmn-form-field (ControlValueAccessor)
│                   │   ├── card/             ← cmn-card
│                   │   ├── alert/            ← cmn-alert
│                   │   └── icon/             ← cmn-icon (Lucide)
│                   ├── directives/
│                   │   └── typography/       ← [cmnTypography] directive
│                   └── services/
│                       └── theme/            ← ThemeService (theme + accent)
│
└── src/                                      ← existing Finance Sentry app (no structural changes)
    └── app/
        └── ...
```

**Structure Decision**: Angular CLI monorepo pattern — library added as a second project in the existing `frontend/` workspace. This avoids a separate npm package, keeps a single `node_modules`, and allows direct source imports during development via tsconfig path alias. The library's Storybook and Playwright configs are scoped to `projects/@dsdevq-common/ui/` and run independently of the host app.

---

## Implementation Phases

### Phase 1 — Library Scaffolding & Workspace Integration

Establish the library project, configure workspace, and set up the build pipeline.

**Tasks**:
- Generate Angular library: `ng generate library @dsdevq-common/ui --prefix=cmn`
- Add `@dsdevq-common/ui` path alias to `frontend/tsconfig.json`
- Add `build:lib` script to `frontend/package.json`
- Configure `ng-package.json` (entryFile, lib output paths)
- Set up scoped ESLint config for `cmn-` prefix
- Install Tailwind CSS v3 (3.4.x) in the workspace; configure `tailwind.config.js` with content paths and `darkMode: ['selector', '[data-theme="dark"]']`
- Scaffold empty `public-api.ts` and `styles/theme.css` (Tailwind directives + empty CSS custom property blocks)
- Verify: `npm run build:lib` succeeds and path alias resolves from host app

**Gate**: Library builds and is importable from the host app before any component work begins.

---

### Phase 2 — Design System Foundation (Stitch → Tailwind)

Define the complete design system in Stitch to inform all visual decisions, then implement tokens in `tailwind.config.js` and `styles/theme.css`.

**Tasks**:
- Use Stitch MCP to define: color palette (light + dark), spacing scale, typography scale, shadow/radius tokens, component specs for all 6 v1 components
- Populate `styles/theme.css`: `@tailwind base/components/utilities` directives + CSS custom properties in `:root` (light) and `[data-theme="dark"]` blocks for all token namespaces
- Populate `tailwind.config.js` `theme.extend`: map each CSS custom property to a Tailwind utility (colors, spacing, fontSize, borderRadius, boxShadow, fontFamily)
- Apply default `data-theme="light"` on `<html>` in the host app's `index.html`
- Configure `ng-package.json` to export `styles/theme.css`; consumers add it to their `angular.json` styles array
- Verify: all token utility classes (e.g., `bg-surface-bg`, `text-primary`) resolve in browser DevTools; dark variant toggles correctly via `data-theme="dark"` on `<html>`

**Gate**: All design tokens defined and present in `styles/theme.css` + `tailwind.config.js`. Tailwind utility classes resolve for every token. Dark variant works.

---

### Phase 3 — Core Components (v1)

Implement all 6 v1 components with full accessibility and token-only styling. Each component is implemented with unit tests and Storybook stories before moving to the next.

**Order** (by dependency):

1. **Icon** (`cmn-icon`) — no dependencies; needed by Button and Alert
2. **Button** (`cmn-button`) — uses Icon for loading state
3. **Input** (`cmn-input`) — ControlValueAccessor; no sub-dependencies
4. **Form Field** (`cmn-form-field`) — wraps Input; ControlValueAccessor
5. **Card** (`cmn-card`) — standalone; content projection only
6. **Alert** (`cmn-alert`) — uses Icon; content projection

**Per-component checklist**:
- [ ] Component, template, styles files (templates use Tailwind utility classes only; `.scss` files minimal)
- [ ] All variants and states expressed via Tailwind utilities referencing theme tokens (no hardcoded values)
- [ ] WCAG 2.1 AA: keyboard navigation, ARIA roles/labels, focus ring visible
- [ ] Unit tests (Vitest): rendering, variants, state transitions, keyboard, form integration (for Input/FormField)
- [ ] Storybook stories: all variants + states + both theme modes
- [ ] ESLint pass: zero errors on all `.ts` files

**Additionally**:
- `[cmnTypography]` directive: applies Tailwind typography utility classes to host element
- Typography utilities come from Tailwind theme config (no separate `_typography.scss` file)

**Gate**: All 6 components pass unit tests, appear in Storybook, and pass automated WCAG 2.1 AA checks in both themes.

---

### Phase 4 — Storybook Setup & Component Documentation

Set up Storybook 10 for the library project with theme toggling and interactive controls.

**Tasks**:
- Install and configure `@storybook/angular@10.x` in the library project
- Configure `.storybook/main.ts` with Angular builder, story paths (`src/**/*.stories.ts`), no `.mdx`
- Configure `.storybook/preview.ts`: global theme-switcher decorator (toggles `data-theme` on `<html>`), import `styles/theme.css` (Tailwind + CSS custom properties — provides all design tokens)
- Add theme toolbar addon for light/dark switching in the catalog
- Add `storybook` and `build-storybook` scripts to `frontend/package.json`
- Verify: Storybook starts, all component stories load, theme toggle works

**Gate**: Storybook runs cleanly; every v1 component visible with all variants in both themes.

---

### Phase 5 — Visual Regression Testing

Set up Playwright-based visual regression against Storybook stories.

**Tasks**:
- Configure `playwright.config.ts` for the library: `webServer` starts Storybook, tests navigate to story URLs
- Write one VRT test file per component (e.g., `button.vrt.spec.ts`) covering all story variants
- Capture initial baselines: `npm run test:vrt -- --update-snapshots`
- Add `test:vrt` script to `frontend/package.json`
- Set snapshot diff tolerance: `maxDiffPixelRatio: 0.02`
- Document baseline update workflow in `quickstart.md`

**Gate**: VRT suite runs in <3 minutes and covers 100% of component stories. Intentional visual change → suite fails with diff image.

---

## Complexity Tracking

No constitution violations. All gates pass without exception.

| Concern | Resolution |
|---|---|
| ESLint `cmn-` prefix vs `fns-` in same workspace | Scoped ESLint override in library project config; no conflict with host app rules |
| `@dsdevq-common/ui` scoped directory name | Standard Angular CLI behavior; workspace handles `@`-scoped project names |
| Storybook version jump (8→10) | No prior Storybook installed; clean install of v10 |
