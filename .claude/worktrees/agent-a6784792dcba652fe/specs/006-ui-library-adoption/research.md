# Research: UI Library Adoption in Host Application

**Feature**: 006-ui-library-adoption  
**Date**: 2026-04-14  
**Branch**: `006-ui-library-adoption`

---

## Decision 1: angular.json styles array — theme.css inclusion

**Decision**: Add `projects/dsdevq-common/ui/src/styles/theme.css` to the host app's `styles` array in `angular.json` (under `projects.finance-sentry.architect.build.options.styles`).

**Rationale**: The library's CSS custom properties (design tokens) are defined in `theme.css`. Without it in the host app build, `cmn-*` components render with no color, spacing, or typography tokens. Current `angular.json` (line 33) only includes `src/styles.scss`; `theme.css` appears only in the library build targets (lines 176/189), not the host.

**Alternatives considered**: Importing `theme.css` inside `src/styles.scss` via `@import` — also valid, but angular.json styles array is the conventional Angular CLI approach and keeps library styles clearly separated from app-level global styles.

---

## Decision 2: ControlValueAccessor pattern for `cmn-form-field`

**Decision**: Place `formControlName` on `<cmn-form-field>`, not on `<cmn-input>`. Use `label` and `errorMessage` inputs on `<cmn-form-field>`. Project `<cmn-input>` as content child.

**Rationale**: `FormFieldComponent` registers `NG_VALUE_ACCESSOR` via `forwardRef` and delegates `writeValue`, `registerOnChange`, `registerOnTouched`, `setDisabledState` to the projected `InputComponent` via `contentChild(InputComponent)`. Placing `formControlName` on `<cmn-input>` would bypass the delegation chain.

**Migration template**:
```html
<cmn-form-field formControlName="email" label="Email" [errorMessage]="emailError">
  <cmn-input type="email" placeholder="Enter your email" />
</cmn-form-field>
```

**Alternatives considered**: N/A — this is the only correct pattern given the source implementation.

---

## Decision 3: AppComponent theme toggle — replace raw button with `cmn-button`

**Decision**: Replace the raw `<button class="theme-toggle">` in `app.component.ts` with `<cmn-button variant="ghost" size="sm">`. Remove test accent buttons (`setTestAccent`, `resetAccent`) — they are debug artifacts from feature 005. Replace hardcoded `background-color: #1976d2` header style with CSS custom property `var(--color-primary)`.

**Rationale**: FR-010/011/012 require the theme toggle to use library components and ThemeService (already wired). The `toggleTheme()` and `isDark` getter already work correctly — only the template and styles need updating.

**Alternatives considered**: Keep `<button>` for the toggle but apply library token classes via Tailwind — rejected because FR-013 disallows hardcoded colors in component-scoped styles and requires library components for interactive elements.

---

## Decision 4: Removing hardcoded styles from login/register/dashboard/accounts

**Decision**: Replace all hardcoded color/spacing/font values in component `.scss` files with CSS custom properties from `theme.css` (e.g., `var(--color-primary)`, `var(--color-error)`, `var(--color-surface-card)`, `var(--color-text-primary)`). Remove component-scoped stylesheets entirely where they become empty after migration.

**Rationale**: FR-013 is explicit: no hardcoded color, spacing, or font-size in migrated component files. All visual values must reference library design tokens.

**Alternatives considered**: Using Tailwind token classes (bg-primary, text-text-primary) directly in templates — also valid but introduces Tailwind as a direct dependency in the host app (currently only the library uses Tailwind). CSS custom properties are framework-agnostic and already available via `theme.css`.

---

## Decision 5: Dashboard and accounts — `cmn-card` and `cmn-alert`

**Decision**: Wrap content grouping panels in `cmn-card`. Replace inline error/info banners with `cmn-alert variant="error"` / `"info"`.

**Rationale**: FR-008 (cmn-card for panels) and FR-009 (cmn-alert for notifications). Both components exist in the library and are exported from `@dsdevq-common/ui`.

**Alternatives considered**: N/A — these are direct spec requirements.

---

## Pre-Satisfied Requirements (no implementation work needed)

- **FR-001** ✅ tsconfig alias `@dsdevq-common/ui` already resolves to `projects/dsdevq-common/ui/src/public-api.ts`
- **FR-003** ✅ `frontend/src/index.html` already has `data-theme="light"` on `<html>`
- **FR-011/012** ✅ ThemeService already injected in AppComponent, `toggleTheme()` already persists via localStorage (ThemeService implementation)
