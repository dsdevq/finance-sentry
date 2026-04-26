# Quickstart: Verify the Connect-Providers Flow

**Feature**: 011-connect-providers · **Date**: 2026-04-25

## Prerequisites

- Docker stack running: `cd docker && docker compose -f docker-compose.dev.yml up -d`
- Health check: `curl -s http://localhost:5050/api/v1/health` → `{"status":"healthy"}`
- A signed-in user (register or use `marketing@digitalfox.cz` if seeded)

## Provider sandbox credentials

| Provider | How to get test creds |
|---|---|
| Plaid | Plaid Sandbox: any `user_good` / `pass_good` flows through. Pick any institution after the link token is issued. |
| Monobank | https://api.monobank.ua → "Get token" — any valid personal token works. |
| Binance | https://testnet.binance.vision → register → API Management → create read-only key + secret. |
| IBKR | IBKR paper-trading account credentials (`https://www.interactivebrokers.com/en/trading/free-demo.php`). 2FA push must be confirmed on the configured phone. |

## Walkthrough (golden path × 4 providers)

1. Open `http://localhost:4200` and sign in.
2. Land on `/accounts`. With zero accounts, the empty-state card shows a **Connect account** button. Click it.
3. The connect modal opens at the **type-picker** step. Choose:
   - **Bank** → goes to bank-picker → choose Plaid or Monobank.
   - **Crypto** → Binance form.
   - **Brokerage** → IBKR form.
4. Submit the relevant form / complete the Plaid overlay.
5. On success: the modal closes with a brief toast naming the provider and account count, the page transitions to the corresponding view:
   - bank → `/accounts`
   - crypto → `/holdings#binance`
   - broker → `/holdings#ibkr`
6. The connected provider now shows a **Connected** badge in the picker on next open.

## Edge cases to verify

| Scenario | Expected |
|---|---|
| Cancel Plaid overlay | Returns to bank-picker; no account added; no error. |
| Bad Monobank token | Inline `MONOBANK_TOKEN_INVALID` message; form remains editable. |
| Reuse Monobank token | `MONOBANK_TOKEN_DUPLICATE` banner with **Disconnect existing** action. |
| Wrong-format token in Monobank field | Inline "This doesn't look like a Monobank token" rejection without a server round-trip. |
| Binance with write-scope key | `BINANCE_INVALID_CREDENTIALS` banner; form editable. |
| IBKR without confirming 2FA | `IBKR_INVALID_CREDENTIALS` banner with the 2FA-push hint. |
| Network drop mid-submit | Retry banner with **Try again**; form values preserved. |
| Plaid script fails to load (block in DevTools) | `PLAID_SCRIPT_LOAD_FAILED` banner with retry button. |
| Mobile viewport (DevTools 360 px) | Modal renders full-bleed, no horizontal scroll, no clipped content. |
| Disconnect (P3) | Provider section disappears; reconnect with new credentials succeeds. |

## Security spot-check (SC-005)

In Chrome DevTools while submitting:
- **Network**: a single outbound `POST` containing the credentials in the body and no other request leaks them in URLs/query strings.
- **Console**: no logged credential values.
- **Application** → Local Storage / Session Storage / IndexedDB / Cookies: nothing matching the entered token, key, secret, or password.

## Tests

- `npx ng test --watch=false` — unit suites for `ConnectStore`, `AccountsStore`, `HoldingsStore`, plus the new connect-modal component specs.
- Storybook responsive snapshots: `npx storybook test` (or whatever the project's Storybook test runner is) — verifies SC-006 viewports.
- Playwright MCP QA after implementation: drive the four golden paths and the documented edge cases through the live UI.

## Definition of done

- All 4 providers connect to success in the sandbox.
- All 8 acceptance scenarios from spec.md pass.
- All edge cases above render the documented copy.
- ESLint clean on every modified `.ts` file.
- `frontend/package.json` minor version bumped and the matching `frontend-v<MINOR>` tag is created on merge.
