# Finance Sentry — QA Guide

## QA — Test User Credentials

| Field | Value |
|---|---|
| Email | test@gmail.com |
| Password | Darkfly21 |

This account has connected accounts across TrueLayer (banking), Monobank (banking), Binance (crypto), and IBKR (brokerage).

### Key test scenarios (check before declaring any fix done)

| Page | Golden path | Key assertions |
|---|---|---|
| **Login** | Enter creds → Submit | Redirects to `/accounts/list`; no JS errors |
| **Accounts** | Load page | Banking/Brokerage/Digital Assets tables render; totalConnections > 0; Net worth shown |
| **Dashboard** | Load page | Total Balance ≠ $0.00 (if accounts exist); category table shows human-readable labels (not `FOOD_AND_DRINK`) |
| **Transactions** | Load page | Transaction rows render; categories human-readable; no spinner stuck |
| **Holdings** | Load page | Summary cards have labels; breakdown table has data |
| **Connect (TrueLayer)** | Click "Connect Account" → select Open Banking | Modal opens; provider list loads without 422/500 |
| **Disconnect** | Click Disconnect on any account | Confirmation dialog opens; account removed on confirm |

---

## QA — End-to-End Testing After Implementation

After **all tasks in a feature are complete**, act as a QA engineer: spin up the app and test the feature through the browser using Playwright MCP.

**Steps (mandatory):**
1. Ensure the full Docker stack is running: `cd docker && docker compose -f docker-compose.dev.yml up -d`
2. Wait for health check: `GET http://localhost:5001/api/v1/health` → `{"status":"healthy"}`
3. Open `http://localhost:4200` via Playwright
4. Navigate the golden path of the feature as a real user would — click buttons, fill forms, follow redirects
5. Also test key error/edge cases (invalid input, cancelled flows, etc.)
6. Report findings: what passed, what failed, screenshots of any broken state
7. If bugs are found, fix them (via Qwen) before declaring the feature done

**Tools:** Use `mcp__plugin_playwright_playwright__browser_*` tools — snapshot first, screenshot only when visual proof is needed.

---
