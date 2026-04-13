# Feature Specification: UI Component Library (@dsdevq-common/ui)

**Feature Branch**: `005-ui-component-library`  
**Created**: 2026-04-11  
**Status**: Draft  
**Input**: User description: "Create @dsdevq-common/ui — a standalone Angular component library..."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Developer Builds Features with Shared Components (Priority: P1)

A developer building any Finance Sentry feature needs UI building blocks that are consistent, accessible, and ready to use without reinventing common patterns. They reference the shared library, pick a component (button, input, card, etc.), and assemble screens without writing custom styles or interaction logic from scratch.

**Why this priority**: Without a usable component library, every new feature duplicates UI work and produces inconsistent results. This is the foundational deliverable — all other stories depend on it.

**Independent Test**: Can be fully tested by importing the library into the host app, building a simple form using Button, Input, Form Field, and Card components, and confirming the form renders correctly and submits data through the host application's standard form binding mechanism.

**Acceptance Scenarios**:

1. **Given** a developer references the shared library in the host app, **When** they add a `cmn-button` to a template, **Then** the button renders with all specified variants (primary, secondary, destructive) and responds to click events.
2. **Given** a reactive form in the host app, **When** the developer binds `cmn-input` and `cmn-form-field` using standard form control syntax, **Then** the input integrates fully — including validation states, error display, and programmatic enable/disable.
3. **Given** a `cmn-card` and `cmn-alert` are placed on a page, **When** the page loads, **Then** both components render correctly with their respective variant styles (info, success, warning, error for alerts).
4. **Given** the library typography scale is applied, **When** any page renders, **Then** all text elements follow the defined type hierarchy without additional custom CSS.

---

### User Story 2 - End User Switches Between Light and Dark Themes (Priority: P2)

A Finance Sentry user wants to switch the application between a light and dark visual theme to suit their preference or environment. The switch is instant, affects every component on screen consistently, and persists across sessions.

**Why this priority**: Theming is a user-facing feature that depends on the token architecture being correct from the start. Adding themes after the fact requires reworking every component.

**Independent Test**: Can be fully tested by rendering all v1 components in both light and dark modes and confirming no component has hardcoded colors that break in either theme.

**Acceptance Scenarios**:

1. **Given** a user is viewing the app in light mode, **When** they switch to dark mode, **Then** all components update their visual appearance instantly without a page reload.
2. **Given** a user has selected dark mode and closes the app, **When** they return, **Then** dark mode is still active.
3. **Given** either theme is active, **When** any component is inspected for color contrast, **Then** all text/background combinations meet WCAG 2.1 AA contrast ratios (4.5:1 for normal text, 3:1 for large text).

---

### User Story 3 - End User Selects a Custom Accent Color (Priority: P3)

A Finance Sentry user wants to personalize the application by choosing an accent color. The entire UI — buttons, highlights, active states, focus rings — updates dynamically to reflect their chosen color. This works across both light and dark themes.

**Why this priority**: This is a differentiating personalization feature. The architecture must support it from day one; retrofitting dynamic palettes into a hardcoded token system is prohibitively expensive.

**Independent Test**: Can be fully tested by programmatically changing the accent color at runtime and confirming all accent-referencing components update without a page reload.

**Acceptance Scenarios**:

1. **Given** a user selects a custom accent color from a picker, **When** the selection is applied, **Then** all interactive elements (buttons, links, focus indicators, active states) update to use that color and its derived shades.
2. **Given** a custom accent is active, **When** the user switches themes, **Then** the accent color is preserved and remains compliant with the active theme's contrast requirements.
3. **Given** an accent color is selected that would fail contrast requirements, **When** the system applies the palette, **Then** the derived shades are automatically adjusted to maintain WCAG 2.1 AA compliance.

---

### User Story 4 - Developer Explores and Documents Components in Isolation (Priority: P4)

A developer needs to view, interact with, and document every component variant and state in isolation — without running the full Finance Sentry application. This enables faster development, visual QA, and onboarding.

**Why this priority**: Component isolation tooling is a force multiplier for development speed and quality. Every component ships with its full documentation.

**Independent Test**: Can be fully tested by opening the component catalog and confirming every v1 component has stories for all variants, interactive states, and edge cases (loading, disabled, error, empty).

**Acceptance Scenarios**:

1. **Given** the component catalog is open, **When** a developer navigates to any component, **Then** they see all variants, states, and interactive controls documented with live examples.
2. **Given** a developer is building a new component, **When** they add stories for it, **Then** the stories immediately appear in the catalog without any additional configuration.
3. **Given** both light and dark themes exist, **When** a developer views a component story, **Then** they can toggle between themes within the catalog to verify the component in both.

---

### User Story 5 - Visual Regression Catches Unintended Component Changes (Priority: P5)

When a developer modifies a component or updates a design token, automated visual comparison tests detect any unintended visual changes across all component variants and states before the change is merged.

**Why this priority**: Visual regressions are invisible to unit tests. Without visual regression, token or style changes silently break components.

**Independent Test**: Can be fully tested by intentionally changing a component's background color and running the visual regression suite, which should fail and produce a diff image showing the change.

**Acceptance Scenarios**:

1. **Given** visual baseline snapshots exist for all components, **When** a developer runs the visual regression suite without making any changes, **Then** all tests pass.
2. **Given** a developer changes a component's visual output, **When** the visual regression suite runs, **Then** the affected test fails and produces a diff image highlighting the change.
3. **Given** an intentional visual change is made, **When** the developer approves the new baseline, **Then** subsequent runs pass with the new snapshot.

---

### Edge Cases

- What happens when a custom accent color is extremely light or dark (near white or near black) — does contrast correction produce a usable palette?
- What happens when a consumer references the library without applying any theme at the application root — do components degrade gracefully or produce unstyled output?
- How does the form field component behave when nested inside a disabled fieldset — does the disabled state propagate correctly?
- What happens when an icon name provided to the icon component does not exist in the icon set — is there a visible fallback?
- How does the component catalog behave when a story throws a runtime error — does it isolate the error or crash the whole catalog?

## Requirements *(mandatory)*

### Functional Requirements

**Library Structure & Consumption**

- **FR-001**: The component library MUST be consumable from the host application without publishing to any package registry — library resolution happens via a local module alias.
- **FR-002**: The library MUST export all components, tokens, and utilities from a single entry point, enabling selective (tree-shakeable) imports.
- **FR-003**: Any new UI component in Finance Sentry MUST be created in the shared component library first; components MUST NOT be built directly in the host application.

**Design Tokens & Theming**

- **FR-004**: All visual values (color, spacing, typography scale, shadow, border radius) MUST be expressed as named design tokens — no hardcoded values may appear anywhere in the component library.
- **FR-005**: The library MUST ship with two complete token sets: a light theme and a dark theme.
- **FR-006**: A theme MUST be activatable by setting a single attribute or class on the root application element; no per-component configuration is required.
- **FR-007**: The library MUST support runtime dynamic accent color selection — a user-chosen color generates a complete derived palette that propagates to all components instantly without a page reload.
- **FR-008**: Dynamically generated accent palettes MUST automatically satisfy WCAG 2.1 AA color contrast ratios across all derived shades.

**Components (v1)**

- **FR-009**: The library MUST provide a Button component with at minimum: primary, secondary, and destructive variants; enabled, disabled, and loading states.
- **FR-010**: The library MUST provide an Input component that integrates with the host application's form system via the standard form control binding protocol, including validation state display.
- **FR-011**: The library MUST provide a Form Field component that wraps inputs with label, hint, and error message regions, and integrates with the host application's form system via the standard form control binding protocol.
- **FR-012**: The library MUST provide a Card component for grouping related content.
- **FR-013**: The library MUST provide an Alert component with at minimum: info, success, warning, and error variants.
- **FR-014**: The library MUST provide a typography scale defining all text styles (headings h1–h6, body, caption, label) as reusable classes or components.
- **FR-015**: All interactive components MUST support full keyboard navigation (tab focus, enter/space activation) and include appropriate ARIA roles and labels to meet WCAG 2.1 AA.
- **FR-016**: All component focus states MUST be visually distinct and meet WCAG 2.1 AA contrast requirements.

**Icon System**

- **FR-017**: The library MUST provide an icon component that renders icons by name from the standard icon set and supports size and color customization via design tokens.
- **FR-018**: The icon component MUST render with appropriate accessibility handling — decorative icons are hidden from assistive technology; meaningful icons include a visible or programmatic label.

**Component Catalog**

- **FR-019**: Every v1 component MUST have a catalog entry with stories covering all variants, states (default, hover, focus, disabled, loading, error), and both theme modes.
- **FR-020**: The component catalog MUST be runnable locally as a standalone application without starting the full Finance Sentry stack.

**Design Source**

- **FR-021**: All visual decisions (color palette, spacing scale, typography, component specs) MUST be defined in the Stitch design system before implementation begins. No visual decision may be made in code without a corresponding Stitch design artifact.

**Testing**

- **FR-022**: Every component MUST have unit tests covering rendering, state transitions, keyboard interactions, and form integration (where applicable), with a minimum of 80% code coverage across the library.
- **FR-023**: Every component catalog story MUST have a corresponding visual regression test that captures a baseline snapshot and fails automatically on unintended visual changes.

### Key Entities

- **Design Token**: A named variable representing a single visual decision (e.g., a surface color, a spacing unit). Tokens are organized into sets per theme.
- **Theme**: A complete set of design token values (light or dark) applied at the application root. Activating a theme swaps all token values simultaneously.
- **Accent Palette**: A dynamically generated set of color tokens derived from a user-selected accent color. Replaces static accent tokens at runtime across all components.
- **Component**: A self-contained, reusable UI element that references only design tokens for visual styling and exposes a documented public API (variants, states, inputs, outputs).
- **Component Story**: An isolated rendering of a component in a specific variant and state combination, used for development, documentation, and visual regression testing.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can add any v1 component to a page in the host application in under 5 minutes using only the library's catalog documentation, with no custom CSS required.
- **SC-002**: Theme switching (light ↔ dark) completes with full visual update in under 100ms as perceived by the user.
- **SC-003**: Accent color change propagates to all visible components in under 200ms without a page reload.
- **SC-004**: 100% of v1 components pass WCAG 2.1 AA automated accessibility checks in both light and dark themes.
- **SC-005**: 100% of v1 components have catalog entries covering all variants and states.
- **SC-006**: The visual regression test suite covers 100% of component stories and completes in under 3 minutes on a developer machine.
- **SC-007**: Unit test coverage across the component library is at or above 80%.
- **SC-008**: Zero hardcoded visual values (colors, spacing, font sizes) exist anywhere in the component library — 100% token coverage verified by automated check.
- **SC-009**: The host application can consume the library and build a functional form using Button, Input, Form Field, and Card without any modifications to the library's internal code.

## Assumptions

- The host application (Finance Sentry Angular app) is the sole consumer of this library for now; multi-app consumption is out of scope for v1.
- Users have a modern browser (Chrome 120+, Firefox 120+, Safari 17+); legacy browser support is out of scope.
- The Stitch MCP tooling is available and accessible for design system definition before implementation begins.
- The component catalog runs as a local development tool only; hosting or deploying the catalog as a public site is out of scope for this feature.
- npm package publishing is out of scope; library is consumed via local path resolution only.
- Migration of existing login and register components to use the new library is explicitly out of scope and will be handled in a separate follow-up feature.
- The dynamic accent color feature supports a single active accent per session; per-component accent overrides are out of scope.
- The icon set is fixed to Lucide Icons; custom icon upload or third-party icon set integration is out of scope.

## Clarifications

### Session 2026-04-13

- Q: Which version of Tailwind CSS is in use — v3 or v4? → A: Tailwind CSS v3 (3.4.x). `tailwind.config.js` with `theme.extend` is the config mechanism; `@theme` blocks and `@import "tailwindcss"` are v4-only and do not apply. Design tokens are CSS custom properties in `styles/theme.css`; dark-mode is configured via `darkMode: ['selector', '[data-theme="dark"]']` in `tailwind.config.js`.

---

## Notes

- [DECISION] Component selector prefix: All library components use the `cmn-` prefix (e.g., `cmn-button`, `cmn-input`) to distinguish them from host app components (`fns-` prefix) and avoid selector collisions.
- [DECISION] Design source of truth: Stitch MCP is authoritative for all visual decisions. Implementation of any component begins only after corresponding Stitch design artifacts exist. Stitch informs design token values; tokens are implemented in `tailwind.config.js` (v3 `theme.extend`) and CSS custom properties in `styles/theme.css`.
- [DECISION] Styling approach: Tailwind CSS v3 (3.4.x) is the sole styling mechanism for the component library. All visual values (colors, spacing, typography, shadows, radii) are expressed as Tailwind theme tokens — no hand-authored CSS values anywhere in component templates or styles. Design tokens are defined as CSS custom properties (`:root` / `[data-theme="dark"]`) in `styles/theme.css` and mapped to Tailwind utilities in `tailwind.config.js`.
- [DECISION] No Angular Material: Angular Material is explicitly excluded. Angular CDK is permitted for behavioral primitives (focus trap, overlay, listbox, etc.) only.
- [DECISION] Form control integration: Input and Form Field components implement the standard Angular form control binding protocol (ControlValueAccessor), enabling seamless use with both template-driven and reactive forms.
- [DECISION] CLAUDE.md rule: Any new UI component in Finance Sentry MUST be created in `@dsdevq-common/ui` first. Components are never built directly in the host Angular app. This applies to all future features.
- [OUT OF SCOPE] npm publication: Library is consumed locally for now; publication to a package registry is deferred.
- [OUT OF SCOPE] Component migration: Migrating existing login/register components to use the new library is a separate follow-up task.
- [OUT OF SCOPE] SSR/server-side rendering: Not required; Finance Sentry is a client-rendered SPA.
- [DEFERRED] Additional components beyond v1 set (Table, Modal, Tooltip, Dropdown, Select, etc.): Addressed in future library expansion features.
