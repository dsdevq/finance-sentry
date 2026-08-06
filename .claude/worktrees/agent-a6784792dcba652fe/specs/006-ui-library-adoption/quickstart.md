# Quickstart: UI Library Adoption

**Feature**: 006-ui-library-adoption  
**Date**: 2026-04-14

---

## Prerequisites

- Feature 005 (`005-ui-component-library`) merged to main — library is built and importable.
- `frontend/` tsconfig path alias `@dsdevq-common/ui` resolves to `projects/dsdevq-common/ui/src/public-api.ts`.
- Angular CLI available: `ng` or `npx ng`.

---

## Step 1: Wire theme.css into the host app (FR-002)

In `frontend/angular.json`, find the host app build styles array (under `projects.finance-sentry.architect.build.options.styles`) and add the library theme:

```json
"styles": [
  "src/styles.scss",
  "projects/dsdevq-common/ui/src/styles/theme.css"
]
```

Also add to the `test` architect target's styles array (same path) to ensure Karma/Jasmine builds include tokens.

Verify by running `npm start` and checking that CSS custom properties like `--color-primary` appear on `<html>` in DevTools.

---

## Step 2: Using `cmn-form-field` + `cmn-input` with Reactive Forms

**Key rule**: `formControlName` goes on `<cmn-form-field>`, NOT on `<cmn-input>`.

```typescript
// Component: import the library modules
import { ButtonComponent, FormFieldComponent, InputComponent } from '@dsdevq-common/ui';

@Component({
  imports: [ReactiveFormsModule, ButtonComponent, FormFieldComponent, InputComponent],
  // ...
})
```

```html
<!-- Template: formControlName on cmn-form-field -->
<form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
  <cmn-form-field formControlName="email" label="Email" [errorMessage]="emailError">
    <cmn-input type="email" placeholder="Enter your email" />
  </cmn-form-field>

  <cmn-form-field formControlName="password" label="Password" [errorMessage]="passwordError">
    <cmn-input type="password" placeholder="Enter your password" />
  </cmn-form-field>

  <cmn-button type="submit" variant="primary" [disabled]="loginForm.invalid">
    Sign In
  </cmn-button>
</form>
```

Error message helper (in component):
```typescript
public get emailError(): string {
  const ctrl = this.loginForm.get('email');
  if (ctrl?.hasError('required')) return 'Email is required';
  if (ctrl?.hasError('email')) return 'Enter a valid email';
  return '';
}
```

---

## Step 3: Using `cmn-card`

```typescript
import { CardComponent } from '@dsdevq-common/ui';

@Component({
  imports: [CardComponent],
  // ...
})
```

```html
<cmn-card>
  <h2>Account Summary</h2>
  <p>Balance: {{ balance | currency }}</p>
</cmn-card>
```

---

## Step 4: Using `cmn-alert`

```typescript
import { AlertComponent } from '@dsdevq-common/ui';
```

```html
<cmn-alert variant="error" *ngIf="errorMessage">
  {{ errorMessage }}
</cmn-alert>

<cmn-alert variant="info">
  No accounts connected yet.
</cmn-alert>
```

Variants: `info` | `success` | `warning` | `error`

---

## Step 5: Theme toggle with `cmn-button`

AppComponent already injects ThemeService. Replace the raw button:

```typescript
import { ButtonComponent } from '@dsdevq-common/ui';

@Component({
  imports: [RouterOutlet, ButtonComponent],
  // ...
})
```

```html
<cmn-button
  variant="ghost"
  size="sm"
  [attr.aria-label]="'Switch to ' + (isDark ? 'light' : 'dark') + ' theme'"
  (click)="toggleTheme()"
>
  {{ isDark ? '☀ Light' : '🌙 Dark' }}
</cmn-button>
```

Replace hardcoded header color:
```scss
.fns-header {
  background-color: var(--color-primary);
  color: var(--color-on-primary);
  // ... rest stays the same
}
```

---

## Verification

After each page migration:

```bash
cd frontend
npx eslint src/app/modules/auth/pages/login/login.component.ts
npx eslint src/app/modules/auth/pages/register/register.component.ts
# etc.
```

Manual check: inspect element in browser → confirm no inline `style=""` attributes, no hardcoded hex values in component SCSS, and `data-theme` switch updates all colors instantly.
