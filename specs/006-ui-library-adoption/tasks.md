# Tasks: UI Library Adoption in Host Application

**Input**: Design documents from `/specs/006-ui-library-adoption/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, quickstart.md ✅

**Tests**: No dedicated test tasks — verification is manual browser smoke test + ESLint gate (no new business logic, no new contracts). ESLint runs as a gate after each modified file per constitution.

**Organization**: Tasks grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths included in all descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Wire the library stylesheet into the host app build and bump version. Nothing renders correctly without this.

**⚠️ CRITICAL**: No story work can begin until theme.css is wired. All cmn-* components render unstyled without design tokens.

- [X] T001 Add `projects/dsdevq-common/ui/src/styles/theme.css` to `styles` array for `build` and `test` architect targets in `frontend/angular.json` (after `src/styles.scss`)
- [X] T002 Bump version in `frontend/package.json` from `0.3.0` to `0.4.0` (constitution versioning gate — frontend/src/ will be modified)
- [X] T003 Verify theme tokens load: run `npm start` in `frontend/`, open browser DevTools, confirm `--color-primary` CSS custom property is present on `<html>`

**Checkpoint**: Theme tokens available in host app → proceed to Foundational phase

---

## Phase 2: Foundational (AppComponent Header)

**Purpose**: Upgrade the app shell before migrating individual pages. AppComponent wraps all pages — resolving its hardcoded styles here ensures consistent header across all subsequent story work.

**⚠️ CRITICAL**: Complete before US3 (theme toggle story), required to remove test accent buttons from feature 005.

- [X] T004 In `frontend/src/app/app.component.ts`: add `ButtonComponent` to `imports` array from `@dsdevq-common/ui`
- [X] T005 In `frontend/src/app/app.component.ts` template: replace the raw `<button class="theme-toggle">` (theme toggle) with `<cmn-button variant="secondary" size="sm" [attr.aria-label]="..." (clicked)="toggleTheme()">{{ isDark ? '☀ Light' : '🌙 Dark' }}</cmn-button>`
- [X] T006 In `frontend/src/app/app.component.ts` template: remove the two test accent buttons (`Accent: Red`, `Reset Accent`) and their corresponding `setTestAccent()` and `resetAccent()` methods from the class
- [X] T007 In `frontend/src/app/app.component.ts` component-scoped styles: replace `background-color: #1976d2` with `background-color: var(--color-accent-800)` and `color: white` with `color: var(--color-text-inverse)` in `.fns-header`
- [X] T008 Run `npx eslint src/app/app.component.ts` from `frontend/` — fix all errors before proceeding

**Checkpoint**: App shell uses library button + design tokens. Theme toggle functional.

---

## Phase 3: User Story 1 — Developer Replaces Ad-hoc UI with Library Components (Login + Register)

**Story Goal**: All interactive form elements on login and register pages use `cmn-*` library components. No raw `<input>` or `<button>` elements remain. Zero hardcoded colors/spacing in component files.

**Independent Test**: Navigate to `/login` and `/register`. Inspect DOM — confirm no raw `<input>` or `<button>` elements. Confirm form submit still works (reactive form bindings preserved). Confirm validation errors display via cmn-form-field.

### Login Page

- [X] T009 [P] [US1] In `frontend/src/app/modules/auth/pages/login/login.component.ts`: add `ButtonComponent`, `FormFieldComponent`, `InputComponent`, `AlertComponent` to `imports` array from `@dsdevq-common/ui`
- [X] T010 [US1] In `frontend/src/app/modules/auth/pages/login/login.component.html`: replace `<input formControlName="email" ...>` with `<cmn-form-field formControlName="email" label="Email" [errorMessage]="emailError"><cmn-input type="email" placeholder="Enter your email" /></cmn-form-field>` — **note**: `formControlName` goes on `cmn-form-field`, NOT on `cmn-input`
- [X] T011 [US1] In `frontend/src/app/modules/auth/pages/login/login.component.html`: replace `<input formControlName="password" ...>` with `<cmn-form-field formControlName="password" label="Password" [errorMessage]="passwordError"><cmn-input type="password" placeholder="Enter your password" /></cmn-form-field>`
- [X] T012 [US1] In `frontend/src/app/modules/auth/pages/login/login.component.ts`: add `emailError` and `passwordError` getter properties that return validation message strings (required, invalid email, etc.) based on form control state
- [X] T013 [US1] In `frontend/src/app/modules/auth/pages/login/login.component.html`: replace raw `<button type="submit" [disabled]="...">` with `<cmn-button type="submit" variant="primary" [disabled]="loginForm.invalid || isLoading">Sign In</cmn-button>`
- [X] T014 [US1] In `frontend/src/app/modules/auth/pages/login/login.component.html`: replace `.error-banner` div with `<cmn-alert variant="error">` using `@if` block
- [X] T015 [US1] In `frontend/src/app/modules/auth/pages/login/login.component.scss`: remove all hardcoded color values — replaced with CSS custom properties
- [X] T016 [US1] Run `npx eslint src/app/modules/auth/pages/login/login.component.ts` from `frontend/` — fix all errors

### Register Page

- [X] T017 [P] [US1] In `frontend/src/app/modules/auth/pages/register/register.component.ts`: add `ButtonComponent`, `FormFieldComponent`, `InputComponent`, `AlertComponent` to `imports` array from `@dsdevq-common/ui`
- [X] T018 [US1] In `frontend/src/app/modules/auth/pages/register/register.component.html`: replace `<input formControlName="email" ...>` with `<cmn-form-field formControlName="email" ...>`
- [X] T019 [US1] In `frontend/src/app/modules/auth/pages/register/register.component.html`: replace `<input formControlName="password" ...>` with `<cmn-form-field formControlName="password" ...>`
- [X] T020 [US1] In `frontend/src/app/modules/auth/pages/register/register.component.html`: replace `<input formControlName="confirmPassword" ...>` with `<cmn-form-field formControlName="confirmPassword" ...>`
- [X] T021 [US1] In `frontend/src/app/modules/auth/pages/register/register.component.ts`: add `emailError`, `passwordError`, `confirmPasswordError` getter properties
- [X] T022 [US1] In `frontend/src/app/modules/auth/pages/register/register.component.html`: replace raw `<button type="submit">` with `<cmn-button type="submit" variant="primary" ...>`
- [X] T023 [US1] In `frontend/src/app/modules/auth/pages/register/register.component.html`: replace error-banner div with `<cmn-alert variant="error">`
- [X] T024 [US1] In `frontend/src/app/modules/auth/pages/register/register.component.scss`: remove all hardcoded color values — replaced with CSS custom properties
- [X] T025 [US1] Run `npx eslint src/app/modules/auth/pages/register/register.component.ts` from `frontend/` — fix all errors

**Story 1 Checkpoint**: Zero raw `<button>` or `<input>` on login/register (SC-001). Form submit and validation work. ESLint clean.

---

## Phase 4: User Story 2 — Consistent Visual Theme Across the App (Dashboard + Accounts)

**Story Goal**: Dashboard and accounts pages use library card/alert components. Global styles reference design tokens instead of hardcoded values. No page looks visually disconnected from the others.

**Independent Test**: Navigate dashboard and accounts pages. Inspect — confirm content panels use `cmn-card`, error states use `cmn-alert`. Switch theme — both pages respond instantly. Check `src/styles.scss` — no hardcoded hex colors.

### Global Styles

- [X] T026 [P] [US2] In `frontend/src/styles.scss`: replace hardcoded `color: #333` with `color: var(--color-text-primary)`, `color: #1976d2` link color with `var(--color-primary)`, `color: #1565c0` hover with `var(--color-primary-dark)` (or `var(--color-primary)` with opacity); remove hardcoded font-family stack if library tokens define it

### Dashboard Page

- [X] T027 [P] [US2] In `frontend/src/app/modules/bank-sync/pages/dashboard/dashboard.component.ts`: add `CardComponent`, `AlertComponent` to `imports` array from `@dsdevq-common/ui`
- [X] T028 [US2] In `frontend/src/app/modules/bank-sync/pages/dashboard/dashboard.component.html`: wrap each content grouping panel / summary widget with `<cmn-card>...</cmn-card>`
- [X] T029 [US2] In `frontend/src/app/modules/bank-sync/pages/dashboard/dashboard.component.html`: replace any inline error div or notification banner with `<cmn-alert variant="error">` or `<cmn-alert variant="info">` as appropriate
- [X] T030 [US2] In `frontend/src/app/modules/bank-sync/pages/dashboard/dashboard.component.scss`: remove all hardcoded color, spacing, or font-size values — replace with CSS custom properties or delete empty rules
- [X] T031 [US2] Run `npx eslint src/app/modules/bank-sync/pages/dashboard/dashboard.component.ts` from `frontend/` — fix all errors

### Accounts List Page

- [X] T032 [P] [US2] In `frontend/src/app/modules/bank-sync/pages/accounts-list/accounts-list.component.ts`: add `ButtonComponent`, `CardComponent`, `AlertComponent` to `imports` array from `@dsdevq-common/ui`
- [X] T033 [US2] In `frontend/src/app/modules/bank-sync/pages/accounts-list/accounts-list.component.html`: replace `<button class="btn-primary">` with `<cmn-button variant="primary">` (preserve click handler and disabled binding)
- [X] T034 [US2] In `frontend/src/app/modules/bank-sync/pages/accounts-list/accounts-list.component.html`: replace `.error-state` div with `<cmn-alert variant="error">` block
- [X] T035 [US2] In `frontend/src/app/modules/bank-sync/pages/accounts-list/accounts-list.component.html`: replace `.empty-state` div with `<cmn-alert variant="info">` (e.g., "No accounts connected yet") or wrap in `<cmn-card>` if it contains structured content
- [X] T036 [US2] In `frontend/src/app/modules/bank-sync/pages/accounts-list/accounts-list.component.scss`: remove all hardcoded color, spacing, or font-size values — replace with CSS custom properties or delete empty rules
- [X] T037 [US2] Run `npx eslint src/app/modules/bank-sync/pages/accounts-list/accounts-list.component.ts` from `frontend/` — fix all errors

**Story 2 Checkpoint**: Dashboard and accounts pages use cmn-card + cmn-alert. No hardcoded values in component files (SC-004). All pages visually consistent. ESLint clean.

---

## Phase 5: User Story 3 — End User Can Switch Theme

**Story Goal**: Theme toggle in the header switches the app between light and dark mode instantly. Preference persists across browser sessions. Theme is consistent across all migrated pages.

**Independent Test**: Click theme toggle → visual switch happens. Close tab → reopen → previous theme restored (SC-003). Toggle while on each migrated page → all pages respond without reload (SC-002).

- [X] T038 [US3] Verify `ThemeService.setTheme()` writes to `localStorage` and sets `data-theme` on `<html>` — read `frontend/projects/dsdevq-common/ui/src/lib/services/theme.service.ts` to confirm (no code change if already correct)
- [X] T039 [US3] Manual smoke test — light mode: open app, confirm `data-theme="light"` on `<html>`, navigate all 4 migrated pages, confirm consistent colors
- [X] T040 [US3] Manual smoke test — dark mode: click theme toggle in header, confirm `data-theme="dark"` on `<html>`, confirm all migrated pages update instantly without reload
- [X] T041 [US3] Manual persistence test: with dark mode active, do a hard browser refresh (Ctrl+Shift+R) — confirm dark mode is restored (SC-003)
- [X] T042 [US3] Manual cross-page test: toggle theme on login page → navigate to dashboard → confirm theme is consistent

**Story 3 Checkpoint**: Theme switching works, persists, and is consistent across all migrated pages (SC-002, SC-003).

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final ESLint sweep, audit for any missed hardcoded values, tag release.

- [X] T043 [P] Run full ESLint pass over all modified files from `frontend/`: `npx eslint src/app/app.component.ts src/app/modules/auth/pages/login/login.component.ts src/app/modules/auth/pages/register/register.component.ts src/app/modules/bank-sync/pages/dashboard/dashboard.component.ts src/app/modules/bank-sync/pages/accounts-list/accounts-list.component.ts` — fix any remaining errors
- [X] T044 [P] Audit all migrated component `.scss` files for any remaining hardcoded hex/rgb color, hardcoded `px` spacing (that should be a token), or hardcoded font-size values — replace or remove per SC-004
- [X] T045 Manually verify in browser that Storybook catalog (if running) still shows library components correctly — confirms library was not inadvertently modified
- [ ] T046 Create git tag `frontend-v0.4.0` after all tasks complete and branch is merged to main (constitution requires tag after version bump)

---

## Dependencies

```
Phase 1 (Setup)
  └─► Phase 2 (Foundational / AppComponent)
        ├─► Phase 3 (US1 - Login + Register) [can start after T001-T003]
        ├─► Phase 4 (US2 - Dashboard + Accounts) [can start after T001-T003]
        └─► Phase 5 (US3 - Theme switching) [requires Phase 2 complete]
              └─► Phase 6 (Polish)
```

US1 (Phase 3) and US2 (Phase 4) can execute in parallel after Setup is done.  
US3 (Phase 5) is lightweight and can follow Phase 2 independently.

---

## Parallel Execution Examples

### After Setup (T001-T003 complete):

Run simultaneously:
- **Track A**: T009 → T010 → T011 → T012 → T013 → T014 → T015 → T016 (Login)
- **Track B**: T017 → T018 → T019 → T020 → T021 → T022 → T023 → T024 → T025 (Register)
- **Track C**: T026 (global styles — independent file)

### Within US2 (Phase 4):

Run simultaneously after T027 / T032 imports are added:
- **Track D**: T028 → T029 → T030 → T031 (Dashboard)
- **Track E**: T033 → T034 → T035 → T036 → T037 (Accounts)

### Within Phase 6:

- T043 and T044 can run in parallel (ESLint + manual audit — different activities)

---

## Implementation Strategy

**MVP scope (US1 alone delivers the core spec deliverable)**:

Complete Phase 1 + Phase 2 + Phase 3 (T001–T025). This satisfies FR-001 through FR-007, SC-001, and SC-005 — the minimum for "developer replaces ad-hoc UI with library components."

**Full delivery order**: Phase 1 → Phase 2 → Phase 3 + Phase 4 (parallel) → Phase 5 → Phase 6

---

## Task Summary

| Phase | Tasks | User Story | Parallelizable |
|-------|-------|------------|----------------|
| 1: Setup | T001–T003 | — | T003 (verify step, low value to parallelize) |
| 2: Foundational | T004–T008 | — | Sequential (same file: app.component.ts) |
| 3: US1 Login | T009–T016 | US1 | T009 [P] (import only) |
| 3: US1 Register | T017–T025 | US1 | T017 [P] (import only), parallel with Login track |
| 4: US2 Global+Dashboard | T026–T031 | US2 | T026 [P], T027 [P] |
| 4: US2 Accounts | T032–T037 | US2 | T032 [P], parallel with Dashboard track |
| 5: US3 Theme | T038–T042 | US3 | Mostly manual verification — sequential |
| 6: Polish | T043–T046 | — | T043 [P], T044 [P] |

**Total**: 46 tasks  
**Parallel opportunities**: 8 tasks marked [P], 2 full parallel tracks in US1, 2 full parallel tracks in US2  
**MVP scope**: T001–T025 (25 tasks, US1 complete)
