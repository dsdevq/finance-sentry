# Quickstart: UI Component Library (@dsdevq-common/ui)

**Branch**: `005-ui-component-library` | **Date**: 2026-04-11

---

## Prerequisites

- Node.js 22+
- Angular CLI 21+
- Docker (for running the full stack)
- Tailwind CSS v4 (installed in `frontend/` workspace as part of library setup)
- Storybook 10 (installed as part of library setup)

---

## 1. Build the Library

The library lives inside the `frontend/` Angular workspace at `projects/@dsdevq-common/ui/`.

```bash
cd frontend
npm run build:lib        # builds @dsdevq-common/ui with ng-packagr → dist/@dsdevq-common/ui/
```

The `build:lib` script is defined in `frontend/package.json` as:
```json
"build:lib": "ng build @dsdevq-common/ui"
```

> **During development**, use watch mode to rebuild the library on changes:
> ```bash
> npm run build:lib -- --watch
> ```

---

## 2. Consume the Library in the Host App

The library is resolved via a tsconfig path alias — no npm publish required.

`frontend/tsconfig.json` includes:
```json
"paths": {
  "@dsdevq-common/ui": ["projects/@dsdevq-common/ui/src/public-api.ts"]
}
```

This allows the host app to import directly from source during development, bypassing the build step:
```typescript
import { CmnButtonComponent } from '@dsdevq-common/ui';
```

> For production builds, Angular CLI resolves the built dist output via `ng-package.json` — no path change needed.

---

## 3. Include the Library Styles

Add the library's Tailwind CSS entry to your app's `angular.json`:

```json
"styles": [
  "projects/@dsdevq-common/ui/src/styles/theme.css",
  "src/styles.scss"
]
```

This makes all design token utility classes (`bg-surface-bg`, `text-primary`, etc.) available in both the library and the host app.

## 4. Apply the Theme

In the host app's root component (`AppComponent` or `index.html`):

```html
<html data-theme="light">
  ...
</html>
```

To switch themes programmatically, inject `ThemeService`:

```typescript
import { ThemeService } from '@dsdevq-common/ui';

// In any component:
private readonly theme = inject(ThemeService);
this.theme.setTheme('dark');
```

---

## 5. Set a Custom Accent Color

```typescript
private readonly theme = inject(ThemeService);
this.theme.setAccent('#7c3aed');  // generates full 11-stop OkLCH palette
```

Reset to brand default:
```typescript
this.theme.resetAccent();
```

---

## 6. Run Storybook (Component Catalog)

```bash
cd frontend
npm run storybook        # starts Storybook at http://localhost:6006
```

Stories live at:
```
frontend/projects/@dsdevq-common/ui/src/lib/components/<name>/<name>.stories.ts
```

---

## 7. Run Unit Tests

```bash
cd frontend
npm run test:lib         # runs Vitest against the library project
```

Coverage threshold: 80% (configured in `vitest.config.ts`).

---

## 8. Run Visual Regression Tests

Storybook must be running (or `webServer` in Playwright config auto-starts it):

```bash
cd frontend
npm run test:vrt         # runs Playwright visual regression suite
```

Update baselines after intentional changes:
```bash
npm run test:vrt -- --update-snapshots
```

Snapshots stored at:
```
frontend/projects/@dsdevq-common/ui/e2e/screenshots/
```

---

## 9. Design-First Workflow

**Before implementing any component**:

1. Open Stitch MCP and define/verify design system tokens and component specs — Stitch outputs a Tailwind CSS v4 `@theme` config.
2. Paste Stitch output into `styles/theme.css`; confirm all token utility classes resolve (`bg-surface-bg`, `text-primary`, etc.) before writing any component code.
3. Implement components using only Tailwind utility classes in templates — no hardcoded values, no custom CSS values.
4. Write Storybook stories first, then implement to make stories pass.
5. Write visual regression baselines after stories are stable.
6. Run `npx eslint <file>` after every `.ts` file and fix all errors before proceeding.

---

## Directory Layout

```
frontend/
├── angular.json                          ← updated: includes library project
├── tsconfig.json                         ← updated: @dsdevq-common/ui path alias
├── package.json                          ← updated: build:lib, storybook, test:lib, test:vrt scripts
│
├── projects/
│   └── @dsdevq-common/
│       └── ui/
│           ├── ng-package.json           ← ng-packagr config
│           ├── package.json              ← library package metadata (name, version)
│           ├── tsconfig.lib.json
│           ├── tsconfig.spec.json
│           ├── .storybook/               ← Storybook config for library
│           │   ├── main.ts
│           │   └── preview.ts
│           ├── e2e/
│           │   ├── visual-regression/    ← Playwright VRT tests
│           │   └── screenshots/          ← snapshot baselines
│           └── src/
│               ├── public-api.ts         ← single library entrypoint
│               ├── styles/
│               │   └── theme.css         ← Tailwind entry: @import, @theme tokens, @custom-variant dark
│               └── lib/
│                   ├── components/
│                   │   ├── button/
│                   │   │   ├── button.component.ts
│                   │   │   ├── button.component.html
│                   │   │   ├── button.component.scss
│                   │   │   ├── button.component.spec.ts
│                   │   │   └── button.stories.ts
│                   │   ├── input/
│                   │   ├── form-field/
│                   │   ├── card/
│                   │   ├── alert/
│                   │   └── icon/
│                   ├── directives/
│                   │   └── typography/   ← [cmnTypography] directive
│                   └── services/
│                       └── theme/        ← ThemeService
│
└── src/                                  ← existing Finance Sentry app (unchanged)
```
