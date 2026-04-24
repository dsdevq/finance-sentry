---
name: stitch-prompts
description: Generate Stitch (Google Labs UI design tool) prompts for Finance Sentry screens. Use when the user wants to design, redesign, or mock up a page with Stitch — or asks for "stitch prompts", "UI prompts", or help with the Stitch workspace DESIGN.md. Produces plain-language, one-change-at-a-time prompts aligned with Stitch's official prompt guide.
user-invocable: true
allowed-tools:
  - Read
  - Write
  - Bash(ls *)
  - Bash(find *)
  - Bash(grep *)
---

# Stitch Prompt Generator — Finance Sentry

Generate prompts the user can paste into [Google Stitch](https://stitch.withgoogle.com) to design or refine Finance Sentry screens. Follow Stitch's [official prompt guide](https://discuss.ai.google.dev/t/stitch-prompt-guide/83844) — prompts must be plain conversational language, short, and scoped to one change at a time.

## Rules (non-negotiable)

1. **Plain English, not markdown specs.** No bullet lists inside the prompt body, no XML tags, no JSON. Short paragraphs Stitch can parse.
2. **One screen per prompt. One change per refinement prompt.** Never bundle "add a filter bar AND change the table AND add a drawer" into one prompt — Stitch will re-layout and drop elements.
3. **Keep each prompt under ~1,500 characters.** Stitch starts omitting components past ~5,000; stay well below.
4. **Zoom-Out → Zoom-In.** For a new workspace, the first prompt is always a broad one-sentence app concept. Screen prompts come next. Refinement prompts come last.
5. **Put shared design context in a DESIGN.md, not in every prompt.** Stitch attaches a workspace-level `DESIGN.md` to every generation. Generate it once per feature; never paste its content into individual screen prompts.
6. **Reference-by-location when refining.** Start refinement prompts with where the change lands: *"On the dashboard, in the hero row, …"*.
7. **Use vibe adjectives.** Stitch responds to mood words. Finance Sentry's vibe is: *minimal, institutional, data-dense, numbers-as-hero, low-chrome.* Never "playful", "vibrant", or "consumer".

## Finance Sentry design context (source material for DESIGN.md)

Feed these facts into any DESIGN.md you generate. Verify against the current repo before locking numbers — the UI library (`@dsdevq-common/ui`, feature 005) is the source of truth for component names and tokens.

- **App**: Personal finance aggregator. Banks (Plaid, Monobank), crypto (Binance), brokerage (IBKR). Single user, power tool.
- **Vibe**: Institutional ops dashboard, not a consumer app. Data-dense. Numbers are the hero.
- **Color**: Single user-configurable accent (default indigo) on primary actions and key numbers. Everything else neutral grays. Status: green = gains/synced, red = losses/errors, amber = stale/warning.
- **Themes**: Light and dark both mandatory. Never hardcode white or black.
- **Type**: Inter sans. Tabular monospaced numerals for all money, right-aligned.
- **Surfaces**: 1px borders over drop shadows. Cards = 12px radius + border, flat. Inputs/buttons = 8px radius.
- **Icons**: Lucide, 20px, stroke 1.75.
- **Shell** (authenticated pages): 240px left sidebar (Dashboard, Accounts, Transactions, Holdings, Settings; collapsible to 64px). Top bar: title left, ⌘K search center, theme toggle + avatar right. Main maxes at 1440px with 32px padding.
- **Responsive breakpoints**: 1440 desktop, 768 tablet, 375 mobile.
- **Components in library** (prefer these names when instructing Stitch for traceability): `cmn-button`, `cmn-input`, `cmn-form-field`, `cmn-card`, `cmn-alert`, `cmn-toast`, `cmn-icon`, `google-sign-in-button`.

## Current screens (pages that exist or are planned)

Verify via `find frontend/src/app/modules -type d -name pages` before claiming coverage. As of last check:

- Auth: login, register
- Bank-sync: accounts-list, connect-account, transaction-list, dashboard
- Not yet scaffolded but planned: holdings (crypto + brokerage), settings, global shell

## Workflow when invoked

1. **Ask what they need** if the args don't say:
   - A fresh DESIGN.md for the workspace? (once per project)
   - Prompts for a specific screen? (name it)
   - Prompts for every screen? (full pack)
   - Refinement prompts for an already-generated screen? (need them to describe what's wrong)
2. **Check the codebase** for up-to-date context before generating — component names, existing page list, any new providers added since this SKILL was written. Do not assume.
3. **Output the prompts as fenced code blocks**, each labeled with its purpose (*"Broad concept — send first"*, *"Dashboard — send after concept"*, *"Dashboard refinement 1"*, etc.).
4. **Order them in send-order** so the user can run them top-to-bottom.
5. **Remind the user** at the end: attach DESIGN.md to the workspace first; send broad concept first; then one screen; then refine one element per prompt.

## Output templates

### Template — DESIGN.md (generate once per workspace)

Plain prose, 200–400 words. Covers: app concept, vibe, color system (accent + neutrals + status), themes, typography, surfaces, icons, shell/layout, breakpoints. No bullet lists. See the design context block above for source facts.

### Template — Broad concept prompt (send first)

One or two sentences. Example:

```
A personal finance dashboard called Finance Sentry that aggregates a user's bank 
accounts, crypto holdings, and brokerage positions into one wealth view. Minimal, 
institutional, data-dense. Desktop-first.
```

### Template — Screen prompt (send one at a time)

Short paragraph, 3–6 sentences. State the page name, top-level layout, and the primary elements in their position. Do **not** describe every micro-detail — leave room for refinement.

Good example:

```
Design the dashboard as the main landing screen. Four KPI cards across the top: 
Total Wealth with a sparkline and month-over-month delta, then Banks, Crypto, 
Brokerage. Below that, two charts side by side — a 12-month net worth line chart 
taking two thirds of the width, and an allocation donut chart taking one third. 
Below the charts, a Recent Activity card with the last 10 transactions in a table.
```

Bad example (too dense, bundles too much):

```
Design the dashboard with 4 KPI cards, 2 charts, a recent activity table with 
48px rows, a sync status panel, an empty state, a slide-out drawer, hover states, 
loading skeletons, and a mobile breakdown.
```

### Template — Refinement prompt (send after the screen exists)

One sentence. Start with location. One change only.

```
On the dashboard, in the hero row, make the dollar amounts larger and use tabular 
monospaced numerals.
```

```
On the accounts page, add a filter pill row under the title with All, Banks, 
Crypto, Brokerage options.
```

## Anti-patterns to avoid in generated prompts

- ❌ Markdown headers inside the prompt body.
- ❌ Bullet lists inside the prompt body.
- ❌ Pixel-perfect measurements for every element (Stitch ignores most; leave refinement for follow-ups).
- ❌ "Also add…", "And then…", multi-clause changes.
- ❌ Component library jargon the Stitch model won't recognize. Prefer visual descriptions: "a card with a border and 12px rounded corners" beats "a cmn-card with default slots". Mention `cmn-*` only if the user asks for traceability.
- ❌ Theme toggling instructions mid-screen. Let DESIGN.md carry theming; don't re-explain per screen.

## Final check before returning output

- Every screen prompt is under ~1,500 characters.
- Each refinement prompt contains exactly one change.
- Prompts are ordered send-first to send-last.
- Vibe language is institutional, not playful.
- DESIGN.md is attached instructions are called out if this is a fresh workspace.
