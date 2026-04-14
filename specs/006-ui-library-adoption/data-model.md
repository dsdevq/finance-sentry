# Data Model: UI Library Adoption

**Feature**: 006-ui-library-adoption  
**Date**: 2026-04-14

---

## Overview

This feature has **no new data entities**. All state management is handled by the existing `ThemeService` from `@dsdevq-common/ui`.

---

## Existing Service: ThemeService

Implemented in feature 005. Consumed (not modified) by this feature.

| Member | Type | Description |
|--------|------|-------------|
| `getTheme()` | `() => 'light' \| 'dark'` | Returns current theme from localStorage / HTML attribute |
| `setTheme(theme)` | `(t: 'light' \| 'dark') => void` | Writes to localStorage, sets `data-theme` on `<html>` |
| `setAccent(hex)` | `(hex: string) => void` | Sets accent CSS custom properties |
| `resetAccent()` | `() => void` | Resets accent to library defaults |

**Storage**: `localStorage` keys managed internally by `ThemeService`. No new storage keys introduced.

---

## Affected Components (host app, modified in place)

| Component | File | Changes |
|-----------|------|---------|
| `AppComponent` | `frontend/src/app/app.component.ts` | Replace raw toggle button with `cmn-button`; remove test accent buttons; replace hardcoded header color |
| `LoginComponent` | `frontend/src/app/modules/auth/pages/login/` | Replace inputs/button with library components; remove hardcoded SCSS |
| `RegisterComponent` | `frontend/src/app/modules/auth/pages/register/` | Replace inputs/button with library components; remove hardcoded SCSS |
| `DashboardComponent` | `frontend/src/app/modules/bank-sync/pages/dashboard/` | Replace content panels with `cmn-card`; replace error banner with `cmn-alert` |
| `AccountsListComponent` | `frontend/src/app/modules/bank-sync/pages/accounts-list/` | Replace action button with `cmn-button`; replace error/empty state with `cmn-alert` or `cmn-card` |

---

## Library Components Consumed (no modifications)

| Selector | Export | Usage |
|----------|--------|-------|
| `cmn-button` | `ButtonComponent` | All action buttons (submit, theme toggle) |
| `cmn-input` | `InputComponent` | Text/password/email inputs (projected into cmn-form-field) |
| `cmn-form-field` | `FormFieldComponent` | Wraps cmn-input; holds NG_VALUE_ACCESSOR; receives formControlName |
| `cmn-card` | `CardComponent` | Content grouping panels on dashboard/accounts |
| `cmn-alert` | `AlertComponent` | Status messages, error banners, info notifications |
| `ThemeService` | `ThemeService` | Already injected in AppComponent; provides setTheme() |
