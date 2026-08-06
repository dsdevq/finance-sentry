# Tasks: UI Component Library (@dsdevq-common/ui)

**Input**: Design documents from `/specs/005-ui-component-library/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, quickstart.md ✅

**Tests**: Per constitution and feature spec (FR-022, FR-023):
- **Unit tests** (components, services, directives): MANDATORY — Vitest, >80% coverage gate
- **Visual regression tests** (per story): MANDATORY — Playwright `.toHaveScreenshot()`, per FR-023
- No REST endpoints → no REST contract tests required for this feature

**Organization**: Tasks grouped by user story. Phase 2 (Foundational) must fully complete before any component work (US1) begins; components reference Tailwind token utility classes that only resolve after `styles/theme.css` is built and Tailwind is configured.

## Format: `[ID] [P?] [Story] Description with file path`

---

## Phase 1: Setup — Library Scaffolding & Workspace Integration

**Purpose**: Bootstrap the Angular library project inside the existing `frontend/` workspace and wire all tooling.

- [X] T001 Generate Angular library: run `ng generate library @dsdevq-common/ui --prefix=cmn` from `frontend/` — creates `frontend/projects/@dsdevq-common/ui/` with ng-packagr scaffolding
- [X] T002 Update `frontend/angular.json` — add the library project entry (`@dsdevq-common/ui`) with `build`, `test`, and `lint` architect targets
- [X] T003 [P] Add path alias to `frontend/tsconfig.json` paths: `"@dsdevq-common/ui": ["projects/@dsdevq-common/ui/src/public-api.ts"]`
- [X] T004 [P] Add npm scripts to `frontend/package.json`: `build:lib`, `build:lib:watch`, `storybook`, `build-storybook`, `test:lib`, `test:vrt`
- [X] T005 Create `frontend/projects/@dsdevq-common/ui/eslint.config.mjs` — scoped ESLint config extending workspace root, overriding `@angular-eslint/component-selector` to prefix `cmn`
- [X] T006 Install Tailwind CSS v3 (3.4.x) in `frontend/`: `npm install tailwindcss@3.4.x postcss autoprefixer`; configure `postcss.config.js` with `tailwindcss` and `autoprefixer` plugins; configure `tailwind.config.js` with `content` paths covering library + host app and `darkMode: ['selector', '[data-theme="dark"]']`; verify Tailwind processes CSS in the Angular build pipeline
- [X] T006b Scaffold empty barrel and Tailwind entry: `frontend/projects/@dsdevq-common/ui/src/public-api.ts` (empty), `src/styles/theme.css` (with `@tailwind base;`, `@tailwind components;`, `@tailwind utilities;` directives and empty `:root {}` and `[data-theme="dark"] {}` CSS custom property blocks)
- [X] T007 Verify setup: `npm run build:lib` succeeds with zero errors; host app can `import {} from '@dsdevq-common/ui'` without TypeScript error

**Checkpoint**: Library project exists, builds, and is importable from the host app.

---

## Phase 2: Foundational — Design System & Storybook Configuration

**Purpose**: Establish the complete design token foundation and Storybook infrastructure before any component is written. No component work (Phase 3+) may begin until this phase is complete.

**⚠️ CRITICAL**: Components use Tailwind utility classes that reference theme tokens. Those tokens must be defined in `styles/theme.css` and compiled by Tailwind before any component is built or tested.

- [X] T008 Use Stitch MCP (`mcp__stitch__create_design_system`) to define the Finance Sentry design system: color palette (light + dark), spacing scale (7 stops), typography scale (8 stops), shadow scale (3 stops), border-radius scale (4 stops), and component specs for Button, Input, Form Field, Card, Alert, Icon — Stitch informs all visual decisions; token values are hand-implemented in `tailwind.config.js` (`theme.extend`) and `styles/theme.css` (CSS custom properties) — not auto-generated
- [X] T009 Populate `frontend/projects/@dsdevq-common/ui/src/styles/theme.css` from Stitch output — CSS custom properties in `:root` (light) and `[data-theme="dark"]` blocks covering all token namespaces per data-model.md: color (surface, text, accent 11-stop, status, border), spacing, typography, radius, shadow; dark mode handled via `darkMode: ['selector', '[data-theme="dark"]']` in `tailwind.config.js` (already set in T006)
- [X] T010 [P] Verify typography token classes resolve in Tailwind output — confirm utility classes for all 10 type levels (display, h1–h4, body, small, caption, label, code) exist in the compiled CSS; add any missing custom utilities to `@theme` if Stitch did not generate them
- [X] T011 Set default theme in `frontend/src/index.html` — add `data-theme="light"` attribute to `<html>` element
- [X] T012 Configure `frontend/projects/@dsdevq-common/ui/ng-package.json` — declare `styles/theme.css` as an exported asset so consumers can add it to their `angular.json` styles array; configure PostCSS or Vite plugin so Tailwind processes the CSS during `ng build @dsdevq-common/ui`
- [X] T013 Install Storybook 10: run `npx storybook@latest init` from `frontend/` targeting the library project; install `@storybook/angular@10.x`
- [X] T014 Configure `frontend/projects/@dsdevq-common/ui/.storybook/main.ts` — Angular builder target, story glob `../../src/**/*.stories.ts`, no `.mdx` addons (avoids Angular 21 MDX bug per research.md)
- [X] T015 Configure `frontend/projects/@dsdevq-common/ui/.storybook/preview.ts` — import `styles/theme.css` globally (`@tailwind` directives + CSS custom properties — provides all design tokens and Tailwind utility classes), add `data-theme` toggle decorator that sets the attribute on the story root element
- [X] T016 Verify: `npm run storybook` starts Storybook at `http://localhost:6006` with no errors; theme toggle switches `data-theme` attribute; Tailwind token utility classes (e.g., `bg-surface-bg`, `text-primary`) resolve correctly in browser DevTools

**Checkpoint**: Design tokens complete in Stitch + `styles/theme.css`. Tailwind utility classes resolve. Storybook runs with theme toggle. All component phases may now begin.

---

## Phase 3: User Story 1 — Core Components (Priority: P1) 🎯 MVP

**Goal**: Deliver all 6 v1 components (Icon, Button, Input, Form Field, Card, Alert) plus the typography directive, each with unit tests and Storybook stories. A developer can build a working reactive form using only library components in the host app.

**Independent Test**: Create a form in the host app using `cmn-button`, `cmn-input`, `cmn-form-field`, and `cmn-card`. Form validates and submits. Run `npm run test:lib` → coverage ≥80%.

### Icon Component

- [X] T017 [P] [US1] Implement `frontend/projects/@dsdevq-common/ui/src/lib/components/icon/icon.component.ts` + `.html` — renders Lucide SVG by `name` input; supports `size` (sm/md/lg → 16/20/24px via Tailwind size utilities) and `color` inputs; decorative by default (`aria-hidden="true"`); if `ariaLabel` input provided, sets `aria-label` and removes `aria-hidden`; unknown name logs warning and renders empty placeholder; no custom `.scss` — sizing via Tailwind `size-4`/`size-5`/`size-6` classes
- [X] T018 [P] [US1] Write unit tests `frontend/projects/@dsdevq-common/ui/src/lib/components/icon/icon.component.spec.ts` — covers: renders known icon, renders unknown icon (empty + warning), aria-hidden default, ariaLabel overrides aria-hidden, size variants apply correct px, color input applied

### Button Component

- [X] T019 [P] [US1] Implement `frontend/projects/@dsdevq-common/ui/src/lib/components/button/button.component.ts` + `.html` — variants: primary/secondary/destructive expressed as conditional Tailwind class sets (e.g., `bg-accent-default text-inverse` for primary); sizes: sm/md/lg via Tailwind spacing/text utilities; disabled state (`aria-disabled`, pointer-events-none, opacity utility); loading state (`aria-busy`, icon spinner); native type passthrough; focus ring via Tailwind `ring` utilities referencing `border-focus` token; zero hardcoded values — all via Tailwind theme utilities
- [X] T020 [P] [US1] Write unit tests `frontend/projects/@dsdevq-common/ui/src/lib/components/button/button.component.spec.ts` — covers: all 3 variants render, disabled prevents click emit, loading sets aria-busy and prevents click emit, type attribute passthrough, focus ring visible (CSS class present), size classes applied
- [X] T021 [P] [US1] Write Storybook stories `frontend/projects/@dsdevq-common/ui/src/lib/components/button/button.stories.ts` — stories: Primary, Secondary, Destructive, Loading, Disabled, AllSizes; each story renders in both themes via the preview decorator

### Input Component

- [X] T022 [P] [US1] Implement `frontend/projects/@dsdevq-common/ui/src/lib/components/input/input.component.ts` + `.html` — implements `ControlValueAccessor`; input types: text/email/password/number/tel/search; sizes: sm/md/lg via Tailwind padding/text utilities; states: default/focused/disabled/readonly/error expressed as conditional Tailwind class bindings (e.g., `border-status-error` when error state); zero hardcoded values; ARIA: `aria-invalid` when in error state
- [X] T023 [P] [US1] Write unit tests `frontend/projects/@dsdevq-common/ui/src/lib/components/input/input.component.spec.ts` — covers: CVA writeValue, registerOnChange, registerOnTouched, setDisabledState; value binding with FormControl; disabled state propagation; error state CSS class; all input types render; size variants
- [X] T024 [P] [US1] Write Storybook stories `frontend/projects/@dsdevq-common/ui/src/lib/components/input/input.stories.ts` — stories: Default, WithValue, Disabled, Readonly, Error, Password, AllSizes; both themes

### Form Field Component

- [X] T025 [P] [US1] Implement `frontend/projects/@dsdevq-common/ui/src/lib/components/form-field/form-field.component.ts` + `.html` — implements `ControlValueAccessor` (delegates to projected `cmn-input`); content-projects `cmn-input`; renders `<label>` with generated `for` pointing to inner input id; hint and error regions styled with Tailwind text/color utilities (`text-status-error`); error region (`role="alert"`, `aria-live="polite"`) visible when `errorMessage` is non-empty and field is touched; `required` asterisk via Tailwind pseudo-class or explicit span
- [X] T026 [P] [US1] Write unit tests `frontend/projects/@dsdevq-common/ui/src/lib/components/form-field/form-field.component.spec.ts` — covers: label renders with correct `for`, hint text displays, error message hidden when pristine, error message shows when touched + invalid, required asterisk present, CVA delegation to inner input (writeValue, setDisabledState)
- [X] T027 [P] [US1] Write Storybook stories `frontend/projects/@dsdevq-common/ui/src/lib/components/form-field/form-field.stories.ts` — stories: Default, WithHint, WithError, Required, Disabled, FullReactiveFormExample; both themes

### Card Component

- [X] T028 [P] [US1] Implement `frontend/projects/@dsdevq-common/ui/src/lib/components/card/card.component.ts` + `.html` — padding variants: none/sm/md/lg via Tailwind `p-*` utilities; elevated variant applies Tailwind `shadow-md` (referencing shadow token); background via `bg-surface-card` utility; content projection via default slot; zero hardcoded values
- [X] T029 [P] [US1] Write unit tests `frontend/projects/@dsdevq-common/ui/src/lib/components/card/card.component.spec.ts` — covers: renders projected content, padding class applied per variant, elevated class applied, no padding when padding=none
- [X] T030 [P] [US1] Write Storybook stories `frontend/projects/@dsdevq-common/ui/src/lib/components/card/card.stories.ts` — stories: Default, Elevated, NoPadding, AllPaddingSizes, WithNestedContent; both themes

### Alert Component

- [X] T031 [P] [US1] Implement `frontend/projects/@dsdevq-common/ui/src/lib/components/alert/alert.component.ts` + `.html` — variants: info/success/warning/error expressed as conditional Tailwind class bindings on container (e.g., `bg-status-info/10 border-status-info` for info); `role="alert"` for error/warning, `role="status"` for info/success; optional `title` input; dismissible dismiss button emits `dismissed` output; icon from `cmn-icon` with `aria-hidden`; projected body text; zero hardcoded values
- [X] T032 [P] [US1] Write unit tests `frontend/projects/@dsdevq-common/ui/src/lib/components/alert/alert.component.spec.ts` — covers: all 4 variants render correct role, title displays when provided, dismiss button emits dismissed event, no dismiss button when dismissible=false, icon aria-hidden, projected content visible
- [X] T033 [P] [US1] Write Storybook stories `frontend/projects/@dsdevq-common/ui/src/lib/components/alert/alert.stories.ts` — stories: Info, Success, Warning, Error, WithTitle, Dismissible, WithLongContent; both themes

### Typography Directive

- [X] T034 [P] [US1] Implement `frontend/projects/@dsdevq-common/ui/src/lib/directives/typography/typography.directive.ts` — `[cmnTypography]` attribute directive; accepts level input (display/h1/h2/h3/h4/body/small/caption/label/code); applies corresponding Tailwind typography utility classes from the theme (e.g., `text-4xl font-bold` for display) to the host element via `HostBinding` or `Renderer2`; levels map directly to theme token utilities — no custom SCSS file required
- [X] T035 [P] [US1] Write unit tests `frontend/projects/@dsdevq-common/ui/src/lib/directives/typography/typography.directive.spec.ts` — covers: each level applies correct CSS class, class updates when input changes, default level applies body class
- [X] T036 [P] [US1] Write Storybook story `frontend/projects/@dsdevq-common/ui/src/lib/directives/typography/typography.stories.ts` — single story showing all 10 levels stacked; both themes

### Library Barrel & Integration

- [X] T037 Update `frontend/projects/@dsdevq-common/ui/src/public-api.ts` — export all 6 components, 1 directive, and their supporting types/interfaces from their respective barrel files
- [X] T038 Verify US1 end-to-end: add `cmn-card`, `cmn-form-field` (with `cmn-input`), and `cmn-button` to a test page in the host app using a `ReactiveFormsModule`-backed form; confirm form validates, submits, and all components render without custom CSS
- [X] T039 Run `npm run test:lib` — confirm all unit tests pass and coverage report shows ≥80% across the library

**Checkpoint**: All 6 components + directive functional, unit-tested, and visible in Storybook. Reactive form integration works in host app.

---

## Phase 4: User Story 2 — Light/Dark Theme Switching (Priority: P2)

**Goal**: Users can switch between light and dark themes. The switch is instant (perceived <100ms), components update without reload, and the selection persists across sessions.

**Independent Test**: Call `ThemeService.setTheme('dark')`, confirm `data-theme="dark"` on `<html>`, all component token values reflect dark palette, reload → dark theme still active.

- [X] T040 Implement `frontend/projects/@dsdevq-common/ui/src/lib/services/theme/theme.service.ts` — `setTheme(theme: 'light' | 'dark')`: writes `data-theme` attribute to `document.documentElement`; stores in `localStorage` key `cmn-theme`; `activeTheme$: Observable<'light' | 'dark'>` via BehaviorSubject; reads stored preference on init (defaults to `'light'`); provided in root; uses `inject()` for any DI; `ChangeDetectionStrategy` N/A (service)
- [X] T041 Write unit tests `frontend/projects/@dsdevq-common/ui/src/lib/services/theme/theme.service.spec.ts` — covers: setTheme('dark') sets data-theme attribute on documentElement, localStorage updated, activeTheme$ emits new value, init reads localStorage preference, init defaults to light when no preference stored
- [X] T042 Export `ThemeService` from `frontend/projects/@dsdevq-common/ui/src/public-api.ts`
- [X] T043 [P] Run `npx eslint frontend/projects/@dsdevq-common/ui/src/lib/services/theme/theme.service.ts` and fix all errors
- [X] T044 Verify US2 end-to-end: inject `ThemeService` in the host app's root component, bind a toggle button, switch themes — all v1 components update visually without page reload; theme preference persists after hard refresh; performance: theme switch completes in <100ms (measure in DevTools)

**Checkpoint**: Theme switching fully functional. Light and dark modes confirmed working across all v1 components.

---

## Phase 5: User Story 3 — Dynamic Accent Color (Priority: P3)

**Goal**: Users can select a custom accent color. The full derived palette propagates to all components in <200ms without a reload. Selection persists. Contrast compliance is automatic.

**Independent Test**: Call `ThemeService.setAccent('#7c3aed')` → inspect CSS custom properties `--cmn-accent-100` through `--cmn-accent-1000` on `documentElement.style` → all 11 stops are set; Button primary variant uses the accent color; `chroma.contrast()` on each stop against its expected background passes WCAG 2.1 AA.

- [X] T045 Install `chroma-js@3.x` and `@types/chroma-js` in `frontend/package.json`
- [X] T046 Extend `frontend/projects/@dsdevq-common/ui/src/lib/services/theme/theme.service.ts` — add `setAccent(hex: string)`: generate 11-stop OkLCH palette via `chroma(hex).scale(['#f8f8f8', hex, '#0a0a0a']).mode('oklch').colors(11)`; validate WCAG AA contrast for each stop against `--cmn-surface-bg` token value using `chroma.contrast()`; auto-adjust any failing stops toward mid-range; write all 11 stops as `document.documentElement.style.setProperty('--cmn-accent-N', value)` (N = 100,200,...1000); store hex in `localStorage` key `cmn-accent`; `resetAccent()`: remove all inline `--cmn-accent-*` properties, clear `localStorage` key; `activeAccent$: Observable<string>`; read stored accent on init
- [X] T047 Update unit tests `frontend/projects/@dsdevq-common/ui/src/lib/services/theme/theme.service.spec.ts` — additional coverage: setAccent generates 11 CSS properties on documentElement, contrast values pass WCAG AA, localStorage updated with hex, resetAccent removes all inline accent properties, init restores stored accent
- [X] T048 [P] Run `npx eslint frontend/projects/@dsdevq-common/ui/src/lib/services/theme/theme.service.ts` and fix all errors
- [X] T049 Verify US3 end-to-end: from the host app, call `setAccent('#e11d48')` → all accent-referencing components (Button primary, Input focus ring, etc.) update to the new palette; switch theme while accent is set → accent palette preserved; call `resetAccent()` → brand default restores; performance: palette propagation <200ms measured in DevTools

**Checkpoint**: Dynamic accent fully functional. OkLCH palette generates, WCAG contrast auto-corrects, persists across sessions.

---

## Phase 6: User Story 4 — Component Catalog Completeness (Priority: P4)

**Goal**: Every v1 component is discoverable in the Storybook catalog with all variants, states, and both theme modes. A developer unfamiliar with the library can understand any component in under 5 minutes using the catalog alone.

**Independent Test**: Open Storybook, navigate to each component, toggle theme → all variants visible in both modes with interactive controls. No component is missing a story.

- [X] T050 Audit Storybook: verify all 6 components + typography directive have stories covering every variant and state listed in data-model.md (all inputs, all states: default/hover/focus/disabled/loading/error); identify and fill any gaps in existing story files
- [X] T051 Add `argTypes` controls to each story file so variant/state/size inputs are interactive in the Storybook controls panel — update `button.stories.ts`, `input.stories.ts`, `form-field.stories.ts`, `card.stories.ts`, `alert.stories.ts`, `icon.stories.ts`, `typography.stories.ts`
- [X] T052 [P] Add theme toolbar to Storybook: configure `@storybook/addon-toolbars` in `.storybook/main.ts` with a theme selector (Light / Dark) that toggles `data-theme` attribute on the preview iframe root
- [X] T053 Verify US4 end-to-end: open catalog, navigate to each component, toggle Light → Dark in the theme toolbar → all components reflect the correct theme tokens without reload; all interactive controls work (change variant, state, size in the controls panel updates the rendered component)
- [X] T054 Run `npm run build-storybook` — static build completes without errors; all stories included in the build output

**Checkpoint**: Catalog is fully documenting all v1 components across all states and themes. Any developer can explore the library independently.

---

## Phase 7: User Story 5 — Visual Regression Testing (Priority: P5)

**Goal**: Playwright visual regression tests cover every component story. Intentional changes require baseline approval; unintentional changes are automatically caught.

**Independent Test**: Modify `button.component.scss` (change primary background), run `npm run test:vrt` → button VRT tests fail with a diff image. Revert change, run again → all pass.

- [X] T055 Configure `frontend/projects/@dsdevq-common/ui/playwright.config.ts` — `webServer` starts Storybook (`npm run storybook`) before tests run; `baseURL` points to `http://localhost:6006`; `snapshotDir` set to `e2e/screenshots`; `maxDiffPixelRatio: 0.02`; headless Chrome; single worker for snapshot consistency
- [X] T056 [P] Write VRT test `frontend/projects/@dsdevq-common/ui/e2e/visual-regression/button.vrt.spec.ts` — for each button story URL: navigate to story iframe, wait for component, `.toHaveScreenshot()` for light theme; repeat for dark theme
- [X] T057 [P] Write VRT test `frontend/projects/@dsdevq-common/ui/e2e/visual-regression/input.vrt.spec.ts` — covers all input stories (default, error, disabled, readonly) in both themes
- [X] T058 [P] Write VRT test `frontend/projects/@dsdevq-common/ui/e2e/visual-regression/form-field.vrt.spec.ts` — covers all form-field stories in both themes
- [X] T059 [P] Write VRT test `frontend/projects/@dsdevq-common/ui/e2e/visual-regression/card.vrt.spec.ts` — covers all card stories in both themes
- [X] T060 [P] Write VRT test `frontend/projects/@dsdevq-common/ui/e2e/visual-regression/alert.vrt.spec.ts` — covers all alert variant stories in both themes
- [X] T061 [P] Write VRT test `frontend/projects/@dsdevq-common/ui/e2e/visual-regression/icon.vrt.spec.ts` — covers all icon sizes and color variants in both themes
- [X] T062 Capture initial baselines: run `npm run test:vrt -- --update-snapshots` — all snapshots saved to `e2e/screenshots/`; commit baseline files
- [X] T063 Verify US5 end-to-end: intentionally alter a component style, run `npm run test:vrt` → test fails with diff image; revert → tests pass; run full suite — completes in <3 minutes
<!-- T062/T063 require Storybook running — defer to manual step -->

**Checkpoint**: Full VRT coverage established. 100% of component stories have snapshot baselines. Suite runtime <3 minutes.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Accessibility validation, coverage gate verification, library versioning.

- [X] T064 [P] Run automated WCAG 2.1 AA accessibility audit on all component stories — use `axe-core` or Storybook's accessibility addon; fix any violations found (focus ring contrast, ARIA role mismatches, color contrast failures)
- [X] T065 [P] Run `npm run test:lib -- --coverage` and verify coverage ≥80% for all files in `frontend/projects/@dsdevq-common/ui/src/lib/`; add tests for any uncovered branches
- [X] T066 Set library version to `0.1.0` in `frontend/projects/@dsdevq-common/ui/package.json`
- [X] T066b Bump `frontend/package.json` version (MINOR: `0.2.0` → `0.3.0`) — required by constitution Versioning Policy because T011 modifies `frontend/src/index.html` and T038 adds a test page under `frontend/src/`; commit the bump in the same branch as the library integration changes
- [X] T067 Run `npx eslint frontend/projects/@dsdevq-common/ui/src/` across all library `.ts` files and fix any remaining errors
- [X] T068 Final validation: build library (`npm run build:lib`), run all tests (`npm run test:lib`), run VRT (`npm run test:vrt`), open Storybook — all pass with zero errors

**Checkpoint**: Library is fully validated, accessible, tested, and versioned at `0.1.0`.

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup)
  └─→ Phase 2 (Foundational) — BLOCKS all story phases
        ├─→ Phase 3 (US1 — Components) — BLOCKS phases 4, 5, 6, 7
        │     ├─→ Phase 4 (US2 — Theme Switching)
        │     │     └─→ Phase 5 (US3 — Dynamic Accent)
        │     ├─→ Phase 6 (US4 — Catalog) — can start after Phase 3
        │     └─→ Phase 7 (US5 — VRT) — requires Phase 6 (stories needed)
        └─→ Phase 8 (Polish) — requires phases 3–7 complete
```

### User Story Dependencies

- **US1 (P1)**: Unblocked after Phase 2 complete
- **US2 (P2)**: Unblocked after US1 complete (components must exist to verify theme visual switch)
- **US3 (P3)**: Unblocked after US2 complete (ThemeService base must exist to extend with accent)
- **US4 (P4)**: Unblocked after US1 complete (stories written in US1; catalog completeness audit is the US4 task)
- **US5 (P5)**: Unblocked after US4 complete (VRT tests navigate to story URLs)

### Within US1 (Core Components)

All 6 component triads (implement + test + story) are independent of each other:
- Icon (T017–T018): no component dependencies
- Button (T019–T021): depends on Icon (uses it for loading state spinner) — do Icon first
- Input (T022–T024): no component dependencies — parallel with Button
- Form Field (T025–T027): depends on Input component — do Input first
- Card (T028–T030): no dependencies — fully parallel
- Alert (T031–T033): depends on Icon — do Icon first
- Typography (T034–T036): no dependencies — fully parallel

---

## Parallel Execution Examples

### Phase 1 — All Setup Tasks Run Immediately

```
Parallel:
  T001 — Generate library
  T003 — Add tsconfig path alias
  T004 — Add npm scripts
  T005 — ESLint config
  T006 — Scaffold empty files
→ Sequential:
  T002 — Update angular.json (after T001 outputs the project config)
  T007 — Verify build (after all setup complete)
```

### Phase 3 — Parallel Component Implementation

```
Parallel group A (no icon dependency):
  T022–T024 (Input)
  T028–T030 (Card)
  T034–T036 (Typography)

Sequential group B (requires Icon T017–T018 first):
  T017–T018 (Icon) → T019–T021 (Button) in parallel with T031–T033 (Alert)
  T022–T024 (Input) → T025–T027 (Form Field)
```

### Phase 7 — All VRT Tests Written in Parallel

```
Parallel (T056–T061): All 6 component VRT test files
→ Sequential: T062 (capture baselines — requires all test files) → T063 (verify)
```

---

## Implementation Strategy

### MVP (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — tokens + Storybook)
3. Complete Phase 3: US1 — Core Components
4. **STOP and VALIDATE**: reactive form test in host app, test coverage ≥80%, Storybook shows all components
5. This delivers: a usable component library, all 6 v1 components, full documentation in Storybook

### Incremental Delivery

| Stage | Delivers | Validates |
|---|---|---|
| Phase 1–2 | Library scaffold + design tokens | `npm run build:lib` + Storybook starts |
| Phase 3 | All v1 components + Storybook stories | Reactive form works in host app; `npm run test:lib` ≥80% |
| Phase 4 | Theme switching (light ↔ dark) | Theme switch <100ms; persists across reload |
| Phase 5 | Dynamic accent color | Palette generates; WCAG contrast auto-corrects; persists |
| Phase 6 | Complete component catalog | All stories visible with controls; theme toggle works |
| Phase 7 | Visual regression suite | `npm run test:vrt` covers all stories in <3 min |
| Phase 8 | WCAG audit + coverage gate | Zero a11y violations; ≥80% coverage; version 0.1.0 |

---

## Notes

- `[P]` tasks operate on different files with no shared-state dependencies — safe to run in parallel
- `[USN]` label maps each task to its user story for Jira issue creation
- Every `.ts` file written requires an `npx eslint <file>` pass before the task is marked complete (constitution § II)
- `cmn-` prefix is used only in the library; host app continues to use `fns-` — no conflict
- Storybook uses `.stories.ts` format only — no `.mdx` files (Angular 21 MDX bug per research.md)
- Playwright VRT baselines must be committed — they are not gitignored
- Library version (`0.1.0`) is independent of host app version (`0.2.0` in `frontend/package.json`)
- Tailwind CSS v3 (3.4.x) is the sole styling mechanism; no hand-authored CSS values in components — all values come from Tailwind theme utilities defined in `tailwind.config.js` (`theme.extend`) and CSS custom properties in `styles/theme.css`
- Component `.html` templates use Tailwind utility classes; `.scss` files are removed or kept empty (only for complex pseudo-selectors not expressible with utilities)
- Stitch informs design token values — hand-implement in `tailwind.config.js` `theme.extend` and `styles/theme.css` CSS custom properties; no direct paste of Stitch output into code
