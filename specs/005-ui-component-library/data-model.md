# Data Model: UI Component Library (@dsdevq-common/ui)

**Branch**: `005-ui-component-library` | **Date**: 2026-04-11

This document defines the public API surface of the library — the component contracts, token namespaces, and type interfaces that constitute the observable model of `@dsdevq-common/ui`.

---

## Design Token Namespaces

Tokens are defined in two files:
1. **`styles/theme.css`** — CSS custom properties in `:root` (light theme) and `[data-theme="dark"]` blocks (e.g., `--color-surface-bg: #f8f9fa`)
2. **`tailwind.config.js`** — `theme.extend` entries map each custom property to a Tailwind utility (e.g., `colors: { 'surface-bg': 'var(--color-surface-bg)' }` → generates `bg-surface-bg`, `text-surface-bg`)

The `--cmn-` naming convention is used for dynamic tokens written at runtime (accent palette via chroma-js). Static tokens follow the `--color-*` convention and reference their values via `var()` in `tailwind.config.js`.

### Color Tokens

| Token Group | CSS Custom Property | Tailwind utility | Description |
|---|---|---|---|
| Surface | `--color-surface-bg`, `--color-surface-card`, `--color-surface-overlay` | `bg-surface-bg`, `bg-surface-card` | Background layers |
| Text | `--color-text-primary`, `--color-text-secondary`, `--color-text-disabled`, `--color-text-inverse` | `text-primary`, `text-secondary` | Text colors |
| Accent (static) | `--color-accent-100` … `--color-accent-1000` | `bg-accent-100` … `bg-accent-1000` | 11-stop accent palette (default: brand blue) |
| Accent (semantic) | `--color-accent-default`, `--color-accent-hover`, `--color-accent-active`, `--color-accent-subtle` | `bg-accent-default`, `text-accent-default` | Semantic aliases |
| Status | `--color-status-info`, `--color-status-success`, `--color-status-warning`, `--color-status-error` | `bg-status-error`, `text-status-error` | Alert/validation colors |
| Border | `--color-border-default`, `--color-border-strong`, `--color-border-focus` | `border-border-default`, `ring-border-focus` | Border and focus ring colors |

**Dark theme**: defined via a `[data-theme="dark"]` CSS selector block in `theme.css` that overrides the same `--color-*` custom properties. Dark mode is activated in Tailwind via `darkMode: ['selector', '[data-theme="dark"]']` in `tailwind.config.js`. Components reference the same utility classes — dark values activate automatically when `data-theme="dark"` is set on `<html>`.

### Spacing Tokens

| Token | Value (Light/Dark neutral) |
|---|---|
| `--cmn-space-1` | 4px |
| `--cmn-space-2` | 8px |
| `--cmn-space-3` | 12px |
| `--cmn-space-4` | 16px |
| `--cmn-space-6` | 24px |
| `--cmn-space-8` | 32px |
| `--cmn-space-12` | 48px |

### Typography Tokens

| Token | Description |
|---|---|
| `--cmn-font-family-base` | Base sans-serif stack |
| `--cmn-font-family-mono` | Monospace stack (for numeric data) |
| `--cmn-font-size-xs` … `--cmn-font-size-4xl` | 6-stop type scale |
| `--cmn-font-weight-normal`, `--cmn-font-weight-medium`, `--cmn-font-weight-bold` | Weight scale |
| `--cmn-line-height-tight`, `--cmn-line-height-base`, `--cmn-line-height-relaxed` | Line height scale |

### Radius & Shadow Tokens

| Token | Description |
|---|---|
| `--cmn-radius-sm`, `--cmn-radius-md`, `--cmn-radius-lg`, `--cmn-radius-full` | Border radius scale |
| `--cmn-shadow-sm`, `--cmn-shadow-md`, `--cmn-shadow-lg` | Box shadow scale |

---

## Theme Model

```
Theme
  ├── id: 'light' | 'dark'
  ├── attribute: data-theme="light" | data-theme="dark"   (set on <html>)
  └── token-set: Record<CSSCustomProperty, string>
```

**Active theme** is stored in `localStorage` under key `cmn-theme`. Default: `'light'`.

**Accent palette** is a runtime overlay:
```
AccentPalette
  ├── source: string           (user-selected hex, e.g. "#4a90e2")
  └── stops: Record<string, string>   (--cmn-accent-100 … --cmn-accent-1000)
```

Active accent is stored in `localStorage` under key `cmn-accent`. Default: brand blue.

---

## Component API Contracts

### Button (`cmn-button`)

| Input | Type | Default | Description |
|---|---|---|---|
| `variant` | `'primary' \| 'secondary' \| 'destructive'` | `'primary'` | Visual style |
| `size` | `'sm' \| 'md' \| 'lg'` | `'md'` | Size variant |
| `disabled` | `boolean` | `false` | Disables interaction |
| `loading` | `boolean` | `false` | Shows loading indicator, disables interaction |
| `type` | `'button' \| 'submit' \| 'reset'` | `'button'` | Native button type |

| Output | Type | Description |
|---|---|---|
| `clicked` | `EventEmitter<MouseEvent>` | Emits on click (not emitted when disabled/loading) |

**Accessibility**: `aria-disabled` when disabled, `aria-busy` when loading. Focus visible via `--cmn-border-focus` ring.

---

### Input (`cmn-input`)

Implements `ControlValueAccessor`.

| Input | Type | Default | Description |
|---|---|---|---|
| `type` | `'text' \| 'email' \| 'password' \| 'number' \| 'tel' \| 'search'` | `'text'` | Native input type |
| `placeholder` | `string` | `''` | Placeholder text |
| `disabled` | `boolean` | `false` | Disables the input (also set by CVA `setDisabledState`) |
| `readonly` | `boolean` | `false` | Makes input read-only |
| `size` | `'sm' \| 'md' \| 'lg'` | `'md'` | Size variant |

**States**: default, focused, disabled, readonly, error (driven by parent Form Field).

**Accessibility**: Associates with label via `aria-labelledby` on parent Form Field.

---

### Form Field (`cmn-form-field`)

Implements `ControlValueAccessor` (delegates to projected `cmn-input`).

| Input | Type | Default | Description |
|---|---|---|---|
| `label` | `string` | required | Field label |
| `hint` | `string` | `''` | Helper text shown below input |
| `errorMessage` | `string` | `''` | Error text; shown when control is invalid and touched |
| `required` | `boolean` | `false` | Marks field as required (asterisk on label) |

**Content projection**: Expects a `cmn-input` as default content slot.

**Accessibility**: Renders `<label>` with `for` pointing to inner input `id`. Error region has `role="alert"` and `aria-live="polite"`.

---

### Card (`cmn-card`)

| Input | Type | Default | Description |
|---|---|---|---|
| `padding` | `'none' \| 'sm' \| 'md' \| 'lg'` | `'md'` | Internal padding |
| `elevated` | `boolean` | `false` | Applies shadow elevation |

**Content projection**: Default slot for arbitrary content.

---

### Alert (`cmn-alert`)

| Input | Type | Default | Description |
|---|---|---|---|
| `variant` | `'info' \| 'success' \| 'warning' \| 'error'` | `'info'` | Color and icon treatment |
| `dismissible` | `boolean` | `false` | Shows dismiss button |
| `title` | `string` | `''` | Optional bold title line |

| Output | Type | Description |
|---|---|---|
| `dismissed` | `EventEmitter<void>` | Emits when user dismisses the alert |

**Content projection**: Default slot for alert body text.

**Accessibility**: `role="alert"` for error/warning; `role="status"` for info/success. Icon is `aria-hidden`.

---

### Icon (`cmn-icon`)

| Input | Type | Default | Description |
|---|---|---|---|
| `name` | `string` | required | Lucide icon name (e.g., `'check'`, `'alert-circle'`) |
| `size` | `number \| 'sm' \| 'md' \| 'lg'` | `'md'` | Icon size (maps to 16/20/24px) |
| `color` | `string` | `'currentColor'` | SVG fill/stroke color (accepts token references) |
| `ariaLabel` | `string` | `''` | If set, icon is meaningful (`aria-label` applied, `aria-hidden` removed) |

**Fallback**: Unknown icon names render an empty placeholder with `aria-hidden="true"` and a console warning.

---

## Typography Scale

Provided as CSS utility classes applied via `[cmnTypography]` directive or `<cmn-text>` component.

| Level | Token | Default size | Weight |
|---|---|---|---|
| `display` | `--cmn-font-size-4xl` | 36px | bold |
| `h1` | `--cmn-font-size-3xl` | 30px | bold |
| `h2` | `--cmn-font-size-2xl` | 24px | semibold |
| `h3` | `--cmn-font-size-xl` | 20px | semibold |
| `h4` | `--cmn-font-size-lg` | 18px | medium |
| `body` | `--cmn-font-size-md` | 16px | normal |
| `small` | `--cmn-font-size-sm` | 14px | normal |
| `caption` | `--cmn-font-size-xs` | 12px | normal |
| `label` | `--cmn-font-size-sm` | 14px | medium |
| `code` | `--cmn-font-family-mono` | 14px | normal |

---

## ThemeService (public API)

```
ThemeService
  ├── activeTheme$: Observable<'light' | 'dark'>
  ├── setTheme(theme: 'light' | 'dark'): void
  ├── activeAccent$: Observable<string>       (hex color)
  ├── setAccent(hex: string): void            (triggers palette generation + CSS var update)
  └── resetAccent(): void                     (restores brand default)
```

Provided in root. Persists selections to `localStorage`.
