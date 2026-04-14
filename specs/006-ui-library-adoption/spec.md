# Feature Specification: UI Library Adoption in Host Application

**Feature Branch**: `006-ui-library-adoption`
**Created**: 2026-04-14
**Status**: Draft
**Input**: User description: "I want to adopt theme and ui library to my main frontend project"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Developer Replaces Ad-hoc UI with Library Components (Priority: P1)

A developer building or updating a Finance Sentry page replaces hand-authored HTML/CSS elements (buttons, inputs, cards, alerts) with the equivalent `@dsdevq-common/ui` components. The pages render correctly using library components, design tokens apply automatically, and no custom inline styles remain.

**Why this priority**: This is the core adoption deliverable. Without replacing existing UI elements, the library provides no value to the host application.

**Independent Test**: Can be fully tested by navigating to the login, register, and dashboard pages and confirming every interactive element (button, input, form field, card, alert) is rendered by a library component with no hand-authored styles visible.

**Acceptance Scenarios**:

1. **Given** the login page is open, **When** a developer inspects the form, **Then** all inputs use `cmn-input` wrapped in `cmn-form-field`, and the submit button uses `cmn-button` with the primary variant.
2. **Given** the register page is open, **When** a developer inspects the form, **Then** all form fields are `cmn-form-field` + `cmn-input` combinations and the submit action uses `cmn-button`.
3. **Given** any page with an alert or notification, **When** the page renders, **Then** the alert is rendered by `cmn-alert` with the correct variant (info/success/warning/error).
4. **Given** a content grouping area (e.g., account summary, dashboard widget), **When** the page renders, **Then** the grouping uses `cmn-card`.

---

### User Story 2 - End User Experiences Consistent Visual Theme Across the App (Priority: P2)

A Finance Sentry user navigates between pages (login, register, dashboard, accounts) and observes a single consistent visual language across all pages. No page looks visually disconnected from the others.

**Why this priority**: Consistency is the primary user-facing benefit of the library adoption. Mixed old/new styles create a jarring experience and undermine trust in a financial application.

**Independent Test**: Can be fully tested by navigating through all migrated pages in both light and dark mode and confirming no page has mismatched colors, fonts, or spacing that diverge from the library design tokens.

**Acceptance Scenarios**:

1. **Given** a user navigates from the login page to the dashboard, **When** both pages are visible in sequence, **Then** typography, button styles, and color palette are identical.
2. **Given** a user switches to dark mode, **When** they navigate across all migrated app pages, **Then** every page responds to the theme switch without a page reload.
3. **Given** the user is on any migrated page, **When** they inspect text elements, **Then** all text follows the library typography scale with no page-level font overrides.

---

### User Story 3 - End User Can Switch Theme (Priority: P3)

A Finance Sentry user can toggle between light and dark themes from within the application. The preference persists across sessions.

**Why this priority**: Theme switching is a user-facing feature already built in the library (ThemeService). Wiring it to a UI control in the host app completes the feature for end users.

**Independent Test**: Can be fully tested by clicking the theme toggle in the app header/nav, confirming the visual switch, closing and reopening the app, and confirming the preference is restored.

**Acceptance Scenarios**:

1. **Given** a user is in light mode, **When** they click the theme toggle, **Then** the entire app switches to dark mode instantly without a page reload.
2. **Given** a user selects dark mode and closes the browser tab, **When** they return to the app, **Then** dark mode is still active.
3. **Given** either theme is active, **When** the user navigates between pages, **Then** the theme is consistent across all migrated pages.

---

### Edge Cases

- What happens when a page has a mix of migrated and non-migrated components -- do the styles conflict or bleed?
- What happens if theme.css is not loaded in the app global styles -- do components render unstyled or throw errors?
- What happens when the theme toggle is activated before the app has fully bootstrapped -- is there a flash of incorrectly themed content?
- What happens when an existing form uses template-driven binding alongside cmn-input -- does the form control integration work correctly?
- What happens to validation error display when a cmn-form-field wraps an input that is invalid but untouched -- is the error correctly hidden until interaction?

## Requirements *(mandatory)*

### Functional Requirements

**Library Integration**

- **FR-001**: The host application MUST import `@dsdevq-common/ui` components via the existing tsconfig path alias -- no npm publish or registry install is required.
- **FR-002**: The host application global styles configuration MUST include `projects/dsdevq-common/ui/src/styles/theme.css` so design tokens are available on all pages.
- **FR-003**: The host application root HTML document MUST have `data-theme="light"` on the `<html>` element as the default theme attribute.

**Component Migration -- Login Page**

- **FR-004**: All submit and action buttons on the login page MUST be replaced with `cmn-button` using the `primary` variant.
- **FR-005**: All text inputs on the login page MUST be replaced with `cmn-input` wrapped in `cmn-form-field`, preserving existing reactive form control bindings and validation.

**Component Migration -- Register Page**

- **FR-006**: All submit and action buttons on the register page MUST be replaced with `cmn-button` using the `primary` variant.
- **FR-007**: All text inputs on the register page MUST be replaced with `cmn-input` wrapped in `cmn-form-field`, preserving existing reactive form control bindings and validation.

**Component Migration -- Dashboard and Accounts Pages**

- **FR-008**: Content grouping panels and summary widgets on the dashboard and accounts pages MUST be replaced with `cmn-card`.
- **FR-009**: Any inline status messages or notification banners on dashboard and accounts pages MUST be replaced with `cmn-alert` using the appropriate variant (info/success/warning/error).

**Theme Switching**

- **FR-010**: The host application MUST provide a theme toggle control visible and accessible on all pages, placed in the top-level navigation or header component.
- **FR-011**: The theme toggle MUST invoke ThemeService.setTheme() and the visual change MUST be reflected immediately across all visible components without a page reload.
- **FR-012**: The selected theme MUST persist and be restored on next app load via ThemeService (which uses localStorage internally).

**Quality**

- **FR-013**: No migrated page component MAY contain hardcoded color, spacing, or font-size values in its template or component-scoped styles -- all visual values MUST reference library design tokens.
- **FR-014**: All modified host application TypeScript files MUST pass ESLint with zero errors after migration.

### Key Entities

- **ThemeService**: The library service (implemented in feature 005) responsible for reading/writing the active theme. Injected into the host app root or header component to power the theme toggle.
- **Migrated Page**: A host application page or component updated to use exclusively `@dsdevq-common/ui` components instead of hand-authored HTML elements.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Zero raw `<button>` or `<input>` elements remain in the login and register page templates after migration -- 100% of interactive form elements use library components.
- **SC-002**: Theme switching (light to dark) completes with full visual update across all migrated pages in under 100ms as perceived by the user.
- **SC-003**: The selected theme persists and is correctly restored after a hard browser refresh.
- **SC-004**: Zero hardcoded color, spacing, or font-size values exist in any migrated host application component file -- verified by ESLint or manual audit.
- **SC-005**: All modified host application files pass ESLint with zero errors.
- **SC-006**: A developer can find and use any library component needed for migration using only the Storybook catalog, without additional documentation.

## Assumptions

- The host application currently has login, register, dashboard, and accounts pages with hand-authored buttons, inputs, and layout elements eligible for migration.
- Existing reactive form bindings (FormControl, ReactiveFormsModule) on login/register pages are preserved intact -- the migration replaces HTML elements but keeps the same form control structure.
- The `@dsdevq-common/ui` library is already built and importable via the tsconfig path alias (delivered in feature 005).
- The dynamic accent color picker is out of scope -- only light/dark theme switching is wired up in this feature.
- Migration of the transaction list page and any pages beyond login, register, dashboard, and accounts is out of scope for this feature.
- No Angular Material components are present in the pages being migrated; if discovered during migration, a separate scoping decision will be made before proceeding.
- The theme toggle UI is a simple icon button -- no custom design is required beyond using cmn-button or a Lucide icon from the library.

## Notes

- [DECISION] Scope of migration: Login, register, dashboard, and accounts pages are the migration targets. Transaction list and other pages are deferred to a follow-up.
- [DECISION] Theme toggle placement: The theme toggle will be placed in the top-level navigation or header component, making it globally accessible.
- [DECISION] Accent color out of scope: ThemeService.setAccent() exists but will not be exposed to the end user in this feature. Dynamic accent color picker is deferred.
- [OUT OF SCOPE] Transaction list and secondary pages: Deferred to a follow-up feature after this adoption is validated.
- [OUT OF SCOPE] Dynamic accent color picker: Supported by the library but not exposed to the user in this feature.
- [OUT OF SCOPE] Component migration was explicitly excluded from feature 005 and is the primary deliverable of this feature.