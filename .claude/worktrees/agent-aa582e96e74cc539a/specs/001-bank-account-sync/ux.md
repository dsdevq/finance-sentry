# UX Design: Bank Account Aggregation & Sync

**Feature**: 001 - Bank Account Aggregation & Sync  
**Created**: 2026-03-21  
**Status**: Design Phase  
**Target Users**: Individual users with multiple bank accounts across regions (Ireland, Ukraine)

---

## Overview

Finance Sentry's bank account aggregation feature allows users to connect multiple bank accounts from different countries and regions, view unified financial data, and track money flow across all accounts. The UX prioritizes simplicity, security, and real-time status awareness.

---

## User Flows

### Flow 1: Connect Bank Account

```
User Home → "Add Account" Button (top-right)
       ↓
Bank Selection Screen
  - Ireland: AIB, Revolut
  - Ukraine: Monobank, Private Bank
       ↓
Credential Input (Secure Modal)
  - Username/Email
  - Password (encrypted transmission)
  - Security note: "Credentials encrypted, never stored in plaintext"
       ↓
Account Selection (if multiple accounts available)
  - Checkbox list per account
  - Display: Account name, type, balance
       ↓
Confirmation Screen
  - "Connecting..." spinner
  - Success: "Connected! Importing transaction history..."
  - CTA: "Go to Dashboard" or "Connect Another Account"
```

### Flow 2: View Dashboard

```
User Login
       ↓
Dashboard Home Screen
  - Total Balance card (USD aggregated)
  - Sub-text: "€X + ₴Y = $Z"
  - Sync status widget
  - Recent transactions (last 20)
  - Account summary cards (3-5 visible)
       ↓
User Actions:
  - Click account → Account detail view
  - Click sync button → Refresh all accounts
  - View all transactions → Transactions screen
  - View money flow → Statistics screen
```

### Flow 3: View All Accounts

```
Sidebar/Bottom Nav → "Accounts"
       ↓
Accounts List Screen
  - Add Account button (top)
  - Account cards (one per connected account)
    * Bank logo + name
    * Account type badge (Checking, Savings)
    * Balance in native currency (EUR, UAH)
    * Last sync status ("2 min ago ✓", "Syncing...", "Failed ⚠")
    * Menu (3-dot): View Details, Manual Sync, Disconnect
       ↓
User Actions:
  - Click account → View transactions for that account
  - Click "Sync Now" → Trigger manual sync
  - Click "Disconnect" → Confirmation, then delete
```

### Flow 4: View Transactions (Filtered)

```
Sidebar/Bottom Nav → "Transactions"
       ↓
Transactions Screen
  - Filters (collapsible):
    * Date range picker (From/To)
    * Account filter (dropdown)
    * Category filter (Income, Expenses, Transfers)
  - Transaction list (paginated, 20 per page)
    * Columns: Date | Description | Amount | Category | Source Account
  - Sorting: by date (newest), by amount
       ↓
User Actions:
  - Click transaction row → Expand for full details
  - Apply filters → List updates
  - Paginate → Load more transactions
```

### Flow 5: View Money Flow Statistics

```
Dashboard → Money Flow Section
       ↓
Statistics Screen (or Dashboard Section)
  - Bar chart: Monthly inflows vs outflows (6 months)
    * Green bars = inflow
    * Red bars = outflow
  - Summary stats below:
    * Total inflow (this month)
    * Total outflow (this month)
    * Net change (this month)
  - Pie chart: Top 3 spending categories
  - Account breakdown: Which account has highest balance
       ↓
User Insight:
  - "I spent €450 this month, mostly on groceries (€200)"
  - "My largest balance is in Revolut (€5,100)"
  - "UAH account decreased by ₴500 last month"
```

---

## Screen Designs

### 1. Dashboard / Home Screen

**Layout**:
```
┌─────────────────────────────────────────────────────────────┐
│ 🏦 Finance Sentry    👤 Denys                          ⚙️    │  ← Header
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │ Total Balance                                             │ │
│  │ $10,912.46                                                │ │
│  │ €7,550.75 + ₴21,950.00                                    │ │
│  │ Last synced: 2 minutes ago  [Sync Now]                    │ │  ← Balance Card
│  └─────────────────────────────────────────────────────────┘ │
│                                                               │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │ 🔄 Sync Status                                            │ │
│  │ All accounts synced ✓  Last sync: 2 min ago               │ │
│  │ Next auto-sync in 58 minutes                              │ │  ← Sync Widget
│  │                                        [Manual Sync] [×]   │ │
│  └─────────────────────────────────────────────────────────┘ │
│                                                               │
│  Recent Transactions                                          │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │ 2025-03-20  Supermarket          -€45.50  Expenses  AIB  │ │
│  │ 2025-03-20  Salary Deposit       +€2,500  Income   AIB   │ │
│  │ 2025-03-19  Revolut Transfer     +€100    Transfer Revolut │
│  │ 2025-03-19  Utilities            -₴1,200  Expenses Mono  │ │  ← Transaction List
│  │ 2025-03-18  Restaurant           -€35.99  Expenses Revolut │
│  │                                       [View All →]        │ │
│  └─────────────────────────────────────────────────────────┘ │
│                                                               │
│  Connected Accounts (4 of 4)                                  │
│  ┌──────────────────┐ ┌──────────────────┐                   │
│  │ 🏦 AIB           │ │ 🏦 Revolut       │                   │
│  │ Checking         │ │ Savings          │                   │
│  │ €2,450.75        │ │ €5,100.00        │                   │  ← Account Cards
│  │ ✓ 2 min ago      │ │ ✓ 2 min ago      │                   │
│  └──────────────────┘ └──────────────────┘                   │
│                                                               │
│  ┌──────────────────┐ ┌──────────────────┐                   │
│  │ 🏦 Monobank      │ │ 🏦 Private Bank  │                   │
│  │ Checking         │ │ Regular          │                   │
│  │ ₴18,750.00       │ │ ₴3,200.00        │                   │
│  │ ✓ 15 min ago     │ │ ⏱ 1 hour ago     │                   │
│  └──────────────────┘ └──────────────────┘                   │
│                                                               │
├─────────────────────────────────────────────────────────────┤
│ [Dashboard] [Accounts] [Transactions] [Statistics]            │  ← Bottom Navigation
└─────────────────────────────────────────────────────────────┘
```

**Component Details**:
- **Balance Card**: Displays total in USD (primary), with native currency breakdown below
- **Sync Widget**: Shows current sync status with color coding (green = synced, orange = syncing, red = failed)
- **Sync Now Button**: Always accessible; triggers immediate sync of all accounts
- **Recent Transactions**: Shows latest 5-10, clickable for details
- **Account Cards**: Mini cards, 2x2 grid, each account is clickable to view full transaction list
- **Navigation**: Bottom navigation for mobile, sidebar for desktop (use responsive design)

---

### 2. Connect Bank Account Modal

**Multi-Step Flow**:

```
STEP 1: Bank Selection
┌─────────────────────────────────────────────────────┐
│ Add Bank Account                              [×]   │
├─────────────────────────────────────────────────────┤
│ Which bank would you like to connect?               │
│                                                      │
│ Ireland                                              │
│ ┌──────────────────┐  ┌──────────────────┐          │
│ │ 🏦 Allied Irish   │  │ 🏦 Revolut       │          │
│ │ Bank (AIB)       │  │                  │          │
│ └──────────────────┘  └──────────────────┘          │
│                                                      │
│ Ukraine                                              │
│ ┌──────────────────┐  ┌──────────────────┐          │
│ │ 🏦 Monobank      │  │ 🏦 Private Bank  │          │
│ │                  │  │ (OpenBank)       │          │
│ └──────────────────┘  └──────────────────┘          │
│                                         [Next →]    │
└─────────────────────────────────────────────────────┘

STEP 2: Credential Input
┌─────────────────────────────────────────────────────┐
│ Connect AIB Account                           [×]   │
├─────────────────────────────────────────────────────┤
│ Enter your banking credentials securely             │
│ 🔒 Your credentials are encrypted and never stored  │
│    in plaintext. We use industry-standard AES-256   │
│    encryption.                                       │
│                                                      │
│ Email / Username                                     │
│ [________________________]                            │
│                                                      │
│ Password                                             │
│ [________________________]                            │
│                                                      │
│ Use secure connection (OAuth):                       │
│ [Connect via AIB Online] (recommended)               │
│                                                      │
│                                 [Back | Connect] →  │
└─────────────────────────────────────────────────────┘

STEP 3: Account Selection
┌─────────────────────────────────────────────────────┐
│ Select Accounts to Connect                     [×]  │
├─────────────────────────────────────────────────────┤
│ Which accounts would you like to add?               │
│                                                      │
│ ☑ Checking Account                                   │
│   Account ending in 1234                            │
│   Balance: €2,450.75                                │
│                                                      │
│ ☐ Savings Account                                    │
│   Account ending in 5678                            │
│   Balance: €12,300.00                               │
│                                                      │
│ ☐ Credit Card                                        │
│   Account ending in 9012                            │
│   Balance: €0.00 (credit limit: €5,000)             │
│                                                      │
│                                   [Back | Next] →   │
└─────────────────────────────────────────────────────┘

STEP 4: Confirmation
┌─────────────────────────────────────────────────────┐
│ Importing Transactions...                      [×]  │
├─────────────────────────────────────────────────────┤
│                                                      │
│         🔄 Syncing your account...                  │
│                                                      │
│    [████████░░░░░░░░░░] 45% complete                │
│                                                      │
│    Fetching 6 months of transaction history         │
│    Estimated time: 1-2 minutes                      │
│                                                      │
│    This is a one-time process.                      │
│    Future syncs will be automatic.                  │
│                                                      │
│                                          [Close]    │
└─────────────────────────────────────────────────────┘

SUCCESS:
┌─────────────────────────────────────────────────────┐
│ Account Connected Successfully!                 [×] │
├─────────────────────────────────────────────────────┤
│                    ✓                                 │
│                                                      │
│ Your AIB Checking Account has been connected        │
│ 🏦 AIB Checking                                      │
│ €2,450.75                                            │
│ 847 transactions imported (Jan 2024 - Mar 2025)     │
│                                                      │
│ Your account will automatically sync every 2 hours. │
│                                                      │
│ [Go to Dashboard] [Connect Another Account]         │
└─────────────────────────────────────────────────────┘
```

---

### 3. Accounts List Screen

```
┌─────────────────────────────────────────────────────┐
│ Connected Accounts                  [+ Add Account] │
├─────────────────────────────────────────────────────┤
│                                                      │
│ ┌──────────────────────────────────────────────────┐ │
│ │ 🏦 Allied Irish Bank (AIB)            [⋮]        │ │
│ │ Checking Account                                  │ │
│ │ €2,450.75                                          │ │
│ │ ✓ Synced 2 minutes ago                            │ │
│ └──────────────────────────────────────────────────┘ │
│                                                      │
│ ┌──────────────────────────────────────────────────┐ │
│ │ 🏦 Revolut                            [⋮]        │ │
│ │ Savings Account                                   │ │
│ │ €5,100.00                                          │ │
│ │ ✓ Synced 2 minutes ago                            │ │
│ └──────────────────────────────────────────────────┘ │
│                                                      │
│ ┌──────────────────────────────────────────────────┐ │
│ │ 🏦 Monobank                           [⋮]        │ │
│ │ Checking Account                                  │ │
│ │ ₴18,750.00                                         │ │
│ │ ✓ Synced 15 minutes ago                           │ │
│ └──────────────────────────────────────────────────┘ │
│                                                      │
│ ┌──────────────────────────────────────────────────┐ │
│ │ 🏦 Private Bank (OpenBank)            [⋮]        │ │
│ │ Regular Account                                   │ │
│ │ ₴3,200.00                                          │ │
│ │ ⏱ Synced 1 hour ago                              │ │
│ │ [Sync Now] ✓ Last sync: successful                │ │
│ └──────────────────────────────────────────────────┘ │
│                                                      │
│ Sync Status Summary:                                │
│ • 4 accounts connected                             │
│ • Last sync: 2 minutes ago                         │
│ • Next auto-sync: in 58 minutes                    │
│                                                      │
└─────────────────────────────────────────────────────┘
```

**Account Card Menu (Click ⋮)**:
- View Details → Opens transaction list for that account only
- Manual Sync → Triggers immediate sync for that account
- Disconnect → Confirmation dialog, deletes account

---

### 4. Transactions Screen

```
┌─────────────────────────────────────────────────────┐
│ All Transactions                                     │
├─────────────────────────────────────────────────────┤
│ [Filters ▼]  [Date Range ▼]  [Account ▼]  [✕ Clear] │
│                                                      │
│ Date          Description              Amount   Cat. │
│ ─────────────────────────────────────────────────── │
│ 2025-03-20    Supermarket Tesco        -€45.50  EXP │
│               AIB Checking Account                    │
│                                                      │
│ 2025-03-20    Salary Deposit           +€2,500  INC │
│               AIB Checking Account                    │
│                                                      │
│ 2025-03-19    Transfer to Savings      +€100    TRN │
│               Revolut Savings Account                │
│                                                      │
│ 2025-03-19    Utilities Payment        -₴1,200  EXP │
│               Monobank Checking                      │
│                                                      │
│ 2025-03-18    Restaurant Booking       -€35.99  EXP │
│               Revolut Savings Account                │
│                                                      │
│ 2025-03-17    ATM Withdrawal           -€100    EXP │
│               AIB Checking Account                    │
│                                                      │
│ ...                                                  │
│                                                      │
│ ◄ Prev  [1] [2] [3] ... [100]  Next ►              │
│ Showing 20 of 847 transactions                      │
└─────────────────────────────────────────────────────┘

Filters (Expanded):
┌─────────────────────────────────────────────────────┐
│ Date Range:                                          │
│ From: [2025-01-01]  To: [2025-03-21]               │
│                                                      │
│ Account:                                             │
│ [✓] AIB  [✓] Revolut  [✓] Monobank  [✓] Private Bank │
│                                                      │
│ Category:                                            │
│ [✓] Income  [✓] Expenses  [✓] Transfers  [✓] Other │
│                                                      │
│ Amount Range:                                        │
│ From: [0]  To: [5000]                              │
│                                                      │
│                              [Apply Filters] [Reset] │
└─────────────────────────────────────────────────────┘

Transaction Detail (Click Row):
┌─────────────────────────────────────────────────────┐
│ Transaction Details                            [×]  │
├─────────────────────────────────────────────────────┤
│ Date:              March 20, 2025                    │
│ Description:       Supermarket Tesco Dublin          │
│ Amount:            -€45.50                            │
│ Account:           AIB Checking Account              │
│ Category:          Expenses / Groceries              │
│ Running Balance:   €2,450.75 → €2,405.25             │
│ Reference:         TRX-20250320-001234               │
│ Status:            Completed ✓                       │
│                                              [Close] │
└─────────────────────────────────────────────────────┘
```

---

### 5. Money Flow Statistics Screen

```
┌─────────────────────────────────────────────────────┐
│ Money Flow Analysis                                  │
├─────────────────────────────────────────────────────┤
│                                                      │
│ Monthly Inflows vs Outflows (Last 6 Months)         │
│                                                      │
│        €3500 │                                       │
│        €3000 │    ┌──┐                              │
│        €2500 │    │ ░│ ┌──┐                         │
│        €2000 │ ┌──┤ ░│ │░│ ┌──┐                     │
│        €1500 │ │░ │ ░│ │░│ │░│ ┌──┐                │
│        €1000 │ │░ │ ░│ │░│ │░│ │░│                │
│         €500 │ │░ │ ░│ │░│ │░│ │░│                │
│           €0 │_│__│_░│_│__│_│__│_│__                │
│               Sep  Oct Nov Dec Jan Feb Mar           │
│              Legend: ░ = Outflows (Expenses)         │
│                      ▓ = Inflows (Income)            │
│                                                      │
│ Month Summary (March 2025):                          │
│ ┌─────────────────────────────────────────────────┐ │
│ │ Total Inflows:     +€2,800.00 (1 salary)        │ │
│ │ Total Outflows:    -€1,450.75 (groceries, util.) │ │
│ │ Net Change:        +€1,349.25                    │ │
│ │ Average Daily:     +€43.52                       │ │
│ └─────────────────────────────────────────────────┘ │
│                                                      │
│ Top Spending Categories:                             │
│ ┌─────────────────────────────────────────────────┐ │
│ │  Groceries    ███████░░░░░░░░░░  €380 (26%)     │ │
│ │  Utilities    ██████░░░░░░░░░░░░  €320 (22%)    │ │
│ │  Entertainment ████░░░░░░░░░░░░░░  €200 (14%)   │ │
│ │  Transport    ███░░░░░░░░░░░░░░░░  €150 (10%)   │ │
│ │  Other        ████████░░░░░░░░░░░  €400 (28%)   │ │
│ └─────────────────────────────────────────────────┘ │
│                                                      │
│ Account Breakdown (by balance):                      │
│ 🏦 AIB Checking      €2,450.75  ████████░░░░░░░░░  │
│ 🏦 Revolut Savings   €5,100.00  ███████████████░░░  │
│ 🏦 Monobank          ₴18,750.00 ██████████░░░░░░░░  │
│ 🏦 Private Bank      ₴3,200.00  ░░░░░░░░░░░░░░░░░░  │
│                                                      │
└─────────────────────────────────────────────────────┘
```

---

## Design System & Styles

### Colors

| Element | Color | Hex | Usage |
|---------|-------|-----|-------|
| Primary | Deep Blue | #1E3A8A | Headers, main CTAs, active states |
| Success | Green | #10B981 | Positive balances, successful syncs, checkmarks |
| Warning | Orange | #F59E0B | Pending syncs, alerts, attention needed |
| Danger | Red | #EF4444 | Failed syncs, errors, outflows |
| Neutral | Gray | #6B7280 | Secondary text, borders, disabled states |
| Background | White/Light Gray | #FFFFFF / #F9FAFB | Page backgrounds |

### Typography

| Element | Font | Size | Weight | Line Height |
|---------|------|------|--------|-------------|
| H1 (Page Title) | Roboto | 28px | Bold (700) | 1.3 |
| H2 (Section Title) | Roboto | 22px | Bold (700) | 1.3 |
| H3 (Card Title) | Roboto | 18px | Bold (700) | 1.3 |
| Body | Roboto | 14px | Regular (400) | 1.5 |
| Small | Roboto | 12px | Regular (400) | 1.4 |
| Currency/Amount | Roboto Mono | 14px | Bold (700) | 1.4 |
| Label | Roboto | 12px | Medium (500) | 1.2 |

### Spacing

- **Base unit**: 8px
- **Cards**: padding 16px (2 units), margin 8px
- **Form fields**: 16px spacing between (2 units)
- **Section gaps**: 24px (3 units)
- **Page padding**: 16-20px (mobile), 24px (desktop)

### Button Styles (Material Design)

| Type | Background | Text | Border | Use Case |
|------|-----------|------|--------|----------|
| Filled | Primary Blue | White | None | Primary CTA (Connect, Sync) |
| Outlined | Transparent | Primary Blue | 1px Primary | Secondary CTA (Cancel, Details) |
| Text | Transparent | Primary Blue | None | Tertiary (Links, Minor actions) |

**Sizing**: 
- Standard: 40px min height, 16px horizontal padding
- Small: 32px height, 12px horizontal padding

### Forms

- **Input fields**: Height 40px, border 1px #D1D5DB, border-radius 6px
- **Focus state**: Border 2px #1E3A8A, box-shadow 0 0 0 3px rgba(30, 58, 138, 0.1)
- **Labels**: Above field, 12px, color #374151, margin-bottom 6px
- **Error state**: Border 1px #EF4444, helper text 12px red

### Cards

- **Border**: 1px #E5E7EB
- **Border-radius**: 8px
- **Box-shadow**: 0 1px 3px rgba(0,0,0,0.1)
- **Padding**: 16px
- **Hover**: shadow 0 4px 6px rgba(0,0,0,0.1)

---

## Interaction Patterns

### Loading States

- **Sync in progress**: Spinner icon (circular, 3 second rotation) with "Syncing..." text
- **Fetching transactions**: Skeleton screens (gray placeholder cards)
- **Page loading**: 0.3s fade-in animation

### Success States

- **Sync completed**: Green checkmark, "Synced X minutes ago" toast notification (auto-dismiss in 3s)
- **Account connected**: Success modal with account details, confetti animation (optional)

### Error States

```
Failed Sync Example:
┌─────────────────────────────────────────────────┐
│ ⚠️  AIB Sync Failed                              │
│ The AIB API is temporarily unavailable.          │
│ Your last successful sync was 2 hours ago.       │
│ Finance Sentry will retry in 5 minutes.          │
│                               [Retry Now] [×]   │
└─────────────────────────────────────────────────┘

Blocked Sync (Auth Expired):
┌─────────────────────────────────────────────────┐
│ 🔐 Reauthorization Required                     │
│ Your AIB credentials have expired.                │
│ Sync is paused until you update your password.   │
│                        [Update Credentials] [×]  │
└─────────────────────────────────────────────────┘
```

### Empty States

```
No Accounts Connected:
┌─────────────────────────────────────────────────┐
│                                                  │
│                    💼                             │
│                                                  │
│          No Bank Accounts Connected               │
│  Get started by adding your first account         │
│                                                  │
│          [+ Add Bank Account]                     │
│                                                  │
└─────────────────────────────────────────────────┘

No Transactions (Empty Account):
┌─────────────────────────────────────────────────┐
│                   📋                              │
│          No transactions yet                      │
│   Transactions will appear here after sync        │
│                                                  │
└─────────────────────────────────────────────────┘
```

### Notifications/Toasts

```
Success: "Account connected successfully! ✓"
         (Green bottom-right, 3s auto-dismiss)

Error:   "Sync failed. Will retry in 5 minutes. ⚠️"
         (Orange bottom-right, 5s auto-dismiss, retry button)

Info:    "New transactions imported (15). View them →"
         (Blue bottom-right, 4s auto-dismiss)
```

---

## Mobile Responsiveness

### Breakpoints

- **Mobile**: < 640px (stacked layout, full-width cards)
- **Tablet**: 640px - 1024px (2-column grid)
- **Desktop**: > 1024px (3-column grid, sidebar nav)

### Mobile Adjustments

- **Dashboard**: Stack cards vertically, hide account preview grid (show "View All")
- **Accounts**: Full-width cards instead of grid
- **Transactions**: Hide category column, show in expanded view
- **Navigation**: Bottom tab bar (Dashboard, Accounts, Transactions, More)
- **Modals**: Full-screen on mobile, centered on desktop
- **Forms**: Single column on mobile, larger touch targets (min 44px)

---

## Accessibility (WCAG 2.1 AA)

### Color Contrast

- Normal text (14px): 4.5:1 contrast ratio
- Large text (18px+): 3:1 contrast ratio
- UI components: 3:1 contrast ratio

### Keyboard Navigation

- Tab order: logical, left-to-right, top-to-bottom
- Escape key: closes modals and menus
- Enter key: confirms dialogs, submits forms
- Arrow keys: navigate dropdown menus

### Form Labels

- All inputs: associated `<label>` tags (never placeholder-only)
- Required fields: marked with asterisk (*) and aria-required="true"
- Error messages: aria-live="polite" announcements

### Images & Icons

- All images: alt text describing content
- Icons: aria-label when used without text (e.g., "Close button")

---

## Security Considerations in UX

1. **Credential Input**: 
   - Never auto-save credentials in browser
   - Use secure transmission (HTTPS only)
   - Show "Password" field with toggle visibility
   - Display security note: "Encrypted, never stored in plaintext"

2. **Data Display**:
   - Mask full account numbers (show last 4 digits only)
   - Don't display passwords in any UI
   - Show "synced X minutes ago" not exact timestamps

3. **Session Management**:
   - Auto-logout after 15 minutes of inactivity (optional, configurable)
   - Session warning at 10 minutes before logout

---

## Status Indicators

| Status | Icon | Color | Meaning |
|--------|------|-------|---------|
| Synced | ✓ | Green | Account synced successfully |
| Syncing | 🔄 | Orange | Currently syncing |
| Error | ⚠️ | Red | Sync failed, will retry |
| Expired | 🔐 | Red | Credentials need renewal |
| Pending | ⏱ | Gray | Waiting for scheduled sync |

---

## User Scenarios & Validation

### Scenario 1: First-Time User
1. User signs up, lands on empty dashboard
2. Sees CTA: "Add Bank Account"
3. Connects AIB (Ireland) account
4. System imports 6 months of transactions
5. Dashboard shows balance €2,450.75 + recent transactions
6. **Expected**: User feels confident they can manage all accounts in one place

### Scenario 2: Multi-Currency User
1. User wants to see all accounts (EUR + UAH)
2. Dashboard shows: "€7,550.75 + ₴21,950.00 = $10,912.46"
3. User can click on each account to filter
4. Money flow chart shows inflow/outflow in each currency
5. **Expected**: User understands their total wealth across regions

### Scenario 3: Sync Failure
1. User's Monobank sync fails (API down)
2. Dashboard shows: "⚠️ Monobank sync failed, will retry in 5 min"
3. After 5 min, sync succeeds automatically
4. Dashboard updates to show "✓ Synced 1 min ago"
5. **Expected**: User is informed, system recovers gracefully

---

## Notes for Design Tool (Figma/Sketch)

1. **Use Material Design Components** from Google's design system
2. **Create reusable components**: Button (filled/outlined/text), Card, Modal, Form Input
3. **Set up auto-layout**: Responsive grid systems for different breakpoints
4. **Prototype interactions**: Click → Modal opens, Sync button → Loading state → Success toast
5. **Share before dev**: Dev team reviews designs, aligns on component specs
6. **Document specs**: Font sizes, spacing, colors in a shared style guide

---

## Success Metrics

- **Onboarding**: Users connect first account in < 2 minutes
- **Usability**: 95%+ users can find transaction history without help
- **Trust**: 90%+ users report feeling secure with credential handling
- **Performance**: Dashboard loads in < 3 seconds with 50+ accounts
- **Mobile**: 70%+ mobile users prefer bottom navigation over sidebar

---

**This UX design is ready for refinement in Figma, Adobe XD, or similar design tools.**
