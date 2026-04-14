# Implementation Plan: UI Library Adoption in Host Application

**Branch**: `006-ui-library-adoption` | **Date**: 2026-04-14 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/006-ui-library-adoption/spec.md`

## Summary

Migrate the Finance Sentry Angular host application (login, register, dashboard, accounts pages) from hand-authored HTML/CSS to `@dsdevq-common/ui` library components. Wire `theme.css` into the host build to activate design tokens. Replace the raw theme-toggle button in `AppComponent` with `cmn-button`. Result: consistent visual language across all migrated pages, zero hardcoded color/spacing values in component files, and working light/dark theme switching persisted across sessions.

## Technical Context

**Language/Version**: TypeScript 5.3 / Angular 21.2 (strict mode)  
**Primary Dependencies**: `@dsdevq-common/ui` (local library, feature 005), Angular `ReactiveFormsModule`, Angular CLI  
**Storage**: `localStorage` (ThemeService — already implemented in feature 005)  
**Testing**: ESLint (zero errors gate), manual browser verification, existing Playwright VRT (library components)  
**Target Platform**: Browser SPA served by Angular dev server / Docker frontend container  
**Project Type**: Web application (Angular SPA host adopting internal library)  
**Performance Goals**: Theme switch < 100ms perceived (SC-002) — CSS custom property update, no re-render required  
**Constraints**: No npm publish required (local path alias only). ReactiveFormsModule bindings must be preserved. No Angular Material components expected in target pages. No new components created in host app — library components only.  
**Scale/Scope**: 4 pages (login, register, dashboard, accounts-list) + AppComponent header

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Rule | Status | Notes |
|------|--------|-------|
| Single Angular project (no new projects) | ✅ PASS | No new Angular projects created |
| inject() only, no constructor DI | ✅ PASS | All modified components already use inject() |
| ChangeDetectionStrategy.OnPush on all components | ✅ PASS | All existing components already OnPush |
| fns- selector prefix for host components | ✅ PASS | No new host components created; existing selectors unchanged |
| No hardcoded colors/spacing (FR-013) | ✅ PASS | This feature removes them |
| ESLint zero errors (FR-014) | ✅ GATE | Run eslint after every modified file |
| UI components in @dsdevq-common/ui only | ✅ PASS | Only consuming library components, not creating new ones in host |
| Version bump required for frontend/src/ changes | ✅ REQUIRED | frontend/package.json must be bumped (0.3.0 → 0.4.0) |
| Git tag after version bump | ✅ REQUIRED | Tag `frontend-v0.4.0` after merge |

**Post-Phase 1 re-check**: No constitution violations found in design. CVA pattern (formControlName on cmn-form-field) is the correct integration approach per source code analysis.

## Project Structure

### Documentation (this feature)

```text
specs/006-ui-library-adoption/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── checklists/
│   └── requirements.md  # Spec quality checklist (all pass)
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (files to be modified)

```text
frontend/
├── angular.json                              # Add theme.css to host app styles array (FR-002)
├── src/
│   ├── styles.scss                           # Remove hardcoded colors; delegate to CSS custom properties
│   └── app/
│       ├── app.component.ts                  # Replace raw toggle button with cmn-button; fix header styles
│       └── modules/
│           ├── auth/pages/
│           │   ├── login/
│           │   │   ├── login.component.ts    # Import ButtonComponent, FormFieldComponent, InputComponent
│           │   │   ├── login.component.html  # Replace inputs+button with cmn-* components
│           │   │   └── login.component.scss  # Remove hardcoded values; use CSS custom properties
│           │   └── register/
│           │       ├── register.component.ts
│           │       ├── register.component.html
│           │       └── register.component.scss
│           └── bank-sync/pages/
│               ├── dashboard/
│               │   ├── dashboard.component.ts
│               │   ├── dashboard.component.html  # cmn-card for panels, cmn-alert for errors
│               │   └── dashboard.component.scss
│               └── accounts-list/
│                   ├── accounts-list.component.ts
│                   ├── accounts-list.component.html
│                   └── accounts-list.component.scss
```

**Structure Decision**: Single Angular project, modifying existing host app files only. No new files except tasks.md. All library components consumed from `@dsdevq-common/ui` via existing tsconfig alias.

## Implementation Phases

### Phase S — Setup (single task)

**S1**: Add `projects/dsdevq-common/ui/src/styles/theme.css` to `angular.json` host app `build` + `test` styles arrays. Verify CSS custom properties load in browser. Bump `frontend/package.json` version `0.3.0` → `0.4.0`.

### Phase A — AppComponent Header

**A1**: Replace raw `<button class="theme-toggle">` with `<cmn-button variant="ghost" size="sm">`. Remove test accent buttons (`setTestAccent`, `resetAccent`) and their methods. Import `ButtonComponent`. Replace `background-color: #1976d2` with `var(--color-primary)` and `color: white` with `var(--color-on-primary)`. ESLint gate.

### Phase B — Login Page

**B1**: Import `ButtonComponent`, `FormFieldComponent`, `InputComponent`, `AlertComponent` in `login.component.ts`. Replace `<input formControlName="email">` with `<cmn-form-field formControlName="email" ...><cmn-input .../></cmn-form-field>`. Replace `<button type="submit">` with `<cmn-button variant="primary">`. Replace `.error-banner` div with `<cmn-alert variant="error">`. Remove all hardcoded colors from `.scss`. ESLint gate.

### Phase C — Register Page

**C1**: Same pattern as Phase B. Three fields: email, password, confirmPassword. Preserve `passwordsMatch` cross-field validator. ESLint gate.

### Phase D — Dashboard Page

**D1**: Import `CardComponent`, `AlertComponent` in `dashboard.component.ts`. Wrap content grouping sections with `<cmn-card>`. Replace inline error div with `<cmn-alert variant="error">`. Remove hardcoded colors from SCSS. ESLint gate.

### Phase E — Accounts List Page

**E1**: Import `ButtonComponent`, `CardComponent`, `AlertComponent` in `accounts-list.component.ts`. Replace `<button class="btn-primary">` with `<cmn-button variant="primary">`. Replace `.error-state` div with `<cmn-alert variant="error">`. Replace `.empty-state` div with `<cmn-card>` or `<cmn-alert variant="info">`. Remove hardcoded colors from SCSS. ESLint gate.

### Phase V — Verification

**V1**: Manual smoke test: navigate login → register → dashboard → accounts. Toggle theme on each page. Confirm consistent visual language, no hardcoded styles visible in DevTools, `data-theme` attribute switches on toggle.

**V2**: Hard browser refresh → confirm dark mode persists (SC-003).

**V3**: ESLint final pass over all modified files: `npx eslint src/app/app.component.ts src/app/modules/auth/pages/**/*.ts src/app/modules/bank-sync/pages/dashboard/*.ts src/app/modules/bank-sync/pages/accounts-list/*.ts`

## Complexity Tracking

No constitution violations requiring justification.
