# Research: UI Component Library (@dsdevq-common/ui)

**Branch**: `005-ui-component-library` | **Date**: 2026-04-11

---

## 1. Angular Library in Existing Workspace

**Decision**: Use `ng generate library @dsdevq-common/ui --prefix=cmn --skip-install` inside the `frontend/` workspace.

**Rationale**: Angular CLI scaffolds the library at `frontend/projects/@dsdevq-common/ui/`, creates `ng-package.json`, `public-api.ts`, and wires ng-packagr as a dev dependency automatically. This is the standard Angular monorepo approach — no separate toolchain required.

**Alternatives considered**:
- Separate standalone workspace at repo root — rejected; would require a separate `node_modules`, separate build pipeline, and a published or symlinked dist. Adds complexity for no benefit at this scale.
- Nx workspace — rejected; introduces Nx toolchain dependency across the whole project. Overkill for a single library at this stage.

**Resulting path**: `frontend/projects/@dsdevq-common/ui/`  
**tsconfig alias**: `"@dsdevq-common/ui": ["projects/@dsdevq-common/ui/src/public-api.ts"]` in `frontend/tsconfig.json`

---

## 2. Storybook Version

**Decision**: Storybook 10 (`@storybook/angular@10.x`).

**Rationale**: Storybook 10 is the current stable release as of 2026 with Angular 21 support. Storybook 8 is outdated. Builder config uses `@storybook/angular:start-storybook` and `@storybook/angular:build-storybook` in `angular.json`.

**Known issue**: Angular 21 + Storybook 10 has a bug with `.mdx` story documentation files ([storybookjs/storybook#34084](https://github.com/storybookjs/storybook/issues/34084)). Mitigation: use `.stories.ts` TypeScript stories only; no `.mdx` files for this feature.

**Alternatives considered**:
- `@storybook/web-components` — rejected; loses Angular-specific bindings and `@Input`/`@Output` controls.

---

## 3. Visual Regression Testing Approach

**Decision**: Direct Playwright + Storybook URL using `.toHaveScreenshot()` snapshot assertions.

**Pattern**: `playwright.config.ts` starts Storybook as a `webServer`. Tests navigate to story iframe URLs and capture per-story screenshots with `.toHaveScreenshot()`. Baselines stored at `frontend/projects/@dsdevq-common/ui/e2e/screenshots/`.

**Rationale**: Simplest approach with no deprecated dependencies. Playwright natively manages snapshot baselines (`--update-snapshots` flag to accept new baselines). CI runs with Docker pinned to a specific browser version for snapshot consistency.

**Alternatives considered**:
- `@storybook/test-runner` — rejected; uses Jest + Playwright under the hood, is in maintenance mode, and adds an unnecessary abstraction layer.
- `@playwright/experimental-ct-angular` — rejected; lacks story discovery; would require reimplementing story setup outside Storybook.

**Snapshot consistency**: Pin Playwright browser version in Docker for CI to avoid antialiasing variance. Set `maxDiffPixelRatio: 0.02` tolerance.

---

## 4. Runtime Dynamic Accent Palette Generation

**Decision**: `chroma-js@3.x` + CSS custom properties on the root element.

**Pattern**: User picks an accent color → `chroma(accent).scale(...).mode('oklch').colors(11)` generates an 11-stop palette (100–1000 stops) → palette emitted as CSS custom properties `--cmn-accent-100` through `--cmn-accent-1000` on `:root` or `[data-theme]` → components reference only the token variables.

**Rationale**: chroma-js provides perceptually uniform gradients via OkLab/OkLCH, producing palettes that feel visually consistent across hues (unlike raw HSL math). Lightweight at ~13 KB minified. Covers the WCAG contrast-checking requirement via `chroma.contrast()`.

**Alternatives considered**:
- `@ctrl/tinycolor` — rejected; no built-in multi-stop scale generation; requires custom math.
- CSS `color-mix()` + `oklch` purely in CSS — rejected; incomplete support in Edge/Safari as of April 2026; not reliable for a financial application.
- Custom HSL math — rejected; HSL hue rotation produces visually uneven palettes.

---

## 5. Public API Structure (Entrypoints)

**Decision**: Single `public-api.ts` entrypoint for v1.

**Rationale**: At 5 components + icon + typography, single entrypoint minimises configuration overhead while keeping tree-shaking effective. Secondary entrypoints (e.g., `@dsdevq-common/ui/tokens`) are deferred until the library grows to 8+ components or a clear concern separation justifies the extra build step.

**Migration path**: ng-packagr secondary entrypoints can be added non-breakingly; consumers referencing the primary entrypoint are unaffected.

---

## 6. ESLint Configuration for Library

**Decision**: The library uses a scoped ESLint config extending the workspace root config, overriding `@angular-eslint/component-selector` to use prefix `cmn` (kebab-case) instead of `fns`.

**Rationale**: Host app uses `fns-` prefix; library uses `cmn-`. Both rules coexist in the monorepo via per-project ESLint config overrides.

---

## 7. Theming Architecture

**Decision**: Theme applied via `data-theme` attribute on the root `<html>` element (e.g., `data-theme="dark"`). Design tokens are defined as CSS custom properties in `styles/theme.css` (`:root` for light, `[data-theme="dark"]` for dark) and mapped to Tailwind utilities in `tailwind.config.js` via `darkMode: ['selector', '[data-theme="dark"]']`.

**Rationale**: Attribute-based theming (vs. class-based) is cleaner for programmatic toggling and does not conflict with host app CSS class namespacing. Widely adopted by modern design systems (Radix UI, shadcn, etc.). CSS custom properties in `theme.css` make all tokens available both as raw variables and (via `tailwind.config.js`) as Tailwind utility classes.

**Accent palette overlay**: Dynamic accent tokens are written directly to `document.documentElement.style.setProperty('--cmn-accent-N', value)` at runtime, overriding the static accent defaults in the active theme.

---

## 8. Styling Approach — Tailwind CSS

**Decision**: Tailwind CSS v3 (3.4.x) as the sole styling mechanism for the component library. Replaces the hand-authored `_tokens.scss` / `_typography.scss` SCSS approach.

**Rationale**: Stitch MCP informs all design decisions. Design tokens are implemented as CSS custom properties in `styles/theme.css` and mapped to Tailwind utilities via `tailwind.config.js`. This preserves the design-token-only constraint (no hardcoded values in component templates — values always trace back to a named theme token). Tailwind v3 is what's installed in the monorepo; v4 was initially planned but v3 is what's actually in use.

**Integration pattern**:
- `styles/theme.css` declares `@tailwind base; @tailwind components; @tailwind utilities;` and CSS custom properties in `:root` (light) / `[data-theme="dark"]` blocks
- `tailwind.config.js` extends theme with `colors`, `spacing`, `fontSize`, `borderRadius`, `boxShadow`, `fontFamily` — each mapped to `var(--color-*)` or a direct value
- Dark mode: `darkMode: ['selector', '[data-theme="dark"]']` in `tailwind.config.js`
- Components use Tailwind utility classes in their HTML templates; component `.scss` files are minimal (only pseudo-states or complex selectors that cannot be expressed with utilities)
- Storybook preview imports `styles/theme.css` as its global stylesheet

**Tailwind + ng-packagr**: The library's `ng-package.json` declares `styles/theme.css` as an exported style. Consumers add the library's stylesheet to their app's `angular.json` styles array. Tailwind processes the CSS via the PostCSS plugin configured in the Angular build pipeline.

**Stitch → Tailwind workflow**: Use `mcp__stitch__create_design_system` to inform visual decisions → manually translate design token values into CSS custom properties in `styles/theme.css` and utility mappings in `tailwind.config.js`.

**Dynamic accent with Tailwind**: Static accent palette defined in `tailwind.config.js` via `var(--cmn-accent-*)`. Runtime overrides from chroma-js write inline CSS custom properties directly on `documentElement.style` — the same utility classes pick up the overridden values automatically.

**Alternatives considered**:
- Hand-authored `_tokens.scss` with CSS custom properties — rejected; Stitch does not export SCSS natively; adds maintenance burden.
- Tailwind v4 with `@theme` CSS-first config — not yet in use; v4 was initially planned but the installed version is v3; a future migration to v4 is possible but not required.
