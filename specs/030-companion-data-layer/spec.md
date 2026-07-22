# Feature Specification: Companion-Mode Data Layer

**Feature Branch**: `030-companion-data-layer`
**Created**: 2026-07-21
**Status**: Draft
**Input**: User description: "Companion-mode data layer for Ledger: analyst actions ingestion (upgrades/downgrades/price targets from free public sources, market-wide universe), valuation snapshot MCP tool (fundamentals vs 5-year history and peers), and market-wide news ingestion breadth with source-per-thesis registration (e.g. TrendForce for the DRAM thesis). Backend + MCP only; consumed by the Ledger OpenClaw agent for a weekly advisor letter and conversational use; ideas flow into the existing opportunity candidates pipeline."

## Context

Ledger (the finance advisor agent) is currently a *guardian*: every data pipeline filters the market through "does this affect the positions Denys already holds?" That is correct for alerting, but it makes Ledger structurally incapable of being the *companion* Denys actually wants — an advisor who proposes ideas, argues a case, and talks about the broader market the way his trusted Telegram channels do (analyst actions with reasoning, valuation mini-notes, catalysts on names he does not own).

Analysis of those channels (2026-07-21) identified the three content types they carry that Ledger cannot currently produce or cite: **street/analyst actions**, **valuation snapshots vs history and peers**, and **curated market-wide catalysts**. This feature adds the data layer for all three. The advisor-letter cron and persona changes that *consume* this layer are deployment configuration on the agent side, not part of this feature.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ledger cites street actions (Priority: P1)

Denys asks Ledger "what has the street said about Micron lately?" — or Ledger, writing its weekly letter, wants to reference the Morgan Stanley note that called the memory sell-off a buying opportunity. Ledger queries the analyst-actions store and gets recent upgrades, downgrades, price-target changes, and top-ideas mentions for any covered ticker — not just held ones — each with the firm, the action, old/new values where available, the date, and the source it was retrieved from.

**Why this priority**: This is the single biggest content gap versus Denys's trusted channels, and it directly fuels both conversational advice and the DRAM thesis (street actions on memory names). Without it the companion has no view of what professional analysts are doing.

**Independent Test**: Ingest one day of analyst actions, then query for a well-covered ticker (e.g. MU) and for a date range across all tickers; verify sourced, deduplicated results are returned.

**Acceptance Scenarios**:

1. **Given** the nightly ingestion has run, **When** Ledger queries analyst actions for a ticker with recent coverage, **Then** it receives the actions from the lookback window with firm name, action type, rating/target values where published, action date, and source attribution.
2. **Given** the same real-world action appears on two ingested sources, **When** ingestion stores it, **Then** only one deduplicated record exists.
3. **Given** a ticker with no analyst coverage (e.g. a crypto asset), **When** Ledger queries it, **Then** the result is explicitly empty — never fabricated.
4. **Given** Ledger queries actions across the whole universe for "since yesterday", **When** results are returned, **Then** they include tickers outside Denys's holdings and watchlist.

---

### User Story 2 - Valuation snapshot for any ticker (Priority: P2)

Ledger wants to write a channel-style mini valuation note — "MCD forward P/E 21.6x vs its 5-year average 26x, EV/EBITDA at a discount to peers, consensus target implies 25% upside" — for any ticker Denys asks about or the letter features. One query returns the ticker's key valuation metrics, the same metrics against its own history, a peer comparison, and the consensus price target with implied upside.

**Why this priority**: Turns Ledger's advice from qualitative opinion into grounded valuation framing — the analytical voice Denys values most in his channels. Depends on nothing from Story 1 and is independently useful in conversation.

**Independent Test**: Request a snapshot for a large-cap ticker and verify metrics, historical comparison, peer set, and upside are returned in one call with honest gaps where data is missing.

**Acceptance Scenarios**:

1. **Given** a large-cap ticker, **When** Ledger requests a valuation snapshot, **Then** it receives forward P/E, EV/EBITDA, and dividend yield, each compared to the ticker's own 5-year average, plus a consensus target and implied upside vs the current price.
2. **Given** no explicit peer list is supplied, **When** the snapshot is built, **Then** a default peer set from the ticker's sector/industry classification is used and named in the result.
3. **Given** a metric is unavailable from the data source (e.g. no dividend, missing forward estimates), **When** the snapshot is returned, **Then** the metric is explicitly marked unavailable — never fabricated or silently zeroed.
4. **Given** the underlying market data is stale (e.g. weekend), **When** the snapshot is returned, **Then** it carries a staleness flag consistent with existing quote staleness reporting.

---

### User Story 3 - Source-per-thesis news breadth (Priority: P3)

Each investment thesis can register the specific external sources that matter to it — for the DRAM thesis, TrendForce press releases. Ingestion pulls those sources plus a market-wide default feed set (not just holdings/watchlist feeds), tags articles with the theses and tickers they match, and makes everything queryable. When TrendForce publishes a DRAM contract-price update, it is in Ledger's queryable news within a day, attached to the DRAM thesis.

**Why this priority**: Completes the "each thesis names its leading indicators" principle and widens the news funnel beyond the book. Valuable, but the two stories above deliver more immediately visible advice quality.

**Independent Test**: Register a source for a thesis, run ingestion, verify articles from that source appear tagged to the thesis; verify market-wide feed articles about non-held tickers are ingested.

**Acceptance Scenarios**:

1. **Given** the DRAM thesis has TrendForce registered as a source, **When** ingestion runs after a new TrendForce press release is published, **Then** the article is stored, tagged to the DRAM thesis, and returned when Ledger queries news for that thesis.
2. **Given** the market-wide feed set is active, **When** ingestion runs, **Then** stored articles include tickers outside holdings and watchlist.
3. **Given** a registered source stops responding, **When** ingestion runs, **Then** the failure is recorded and surfaced through the existing data-freshness alerting path — ingestion of other sources continues.

---

### Edge Cases

- A free source changes its page markup or blocks automated access → that source's ingestion fails visibly (freshness alert), other sources continue; the system never silently reports "no new actions" as if it had checked.
- Two sources report the same action with slightly different values (e.g. rounded price targets) → dedup keeps the richer record; no duplicate rows for the same (ticker, firm, date, action).
- A ticker is queried that exists but has never been ingested (outside the configured universe) → response distinguishes "no coverage in universe" from "no recent actions".
- Valuation history shorter than 5 years (recent IPO) → comparison uses available history and states the actual window.
- Non-equity assets (crypto) → analyst actions and valuation snapshots return explicit not-applicable results; no fabricated fundamentals.
- Ingestion job overlaps a previous still-running ingestion → second run skips or queues; no duplicate ingestion of the same window.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST ingest analyst actions (upgrades, downgrades, coverage initiations, price-target changes, top-ideas list mentions) on a nightly schedule from at least two independent free public sources, with no paid API dependencies.
- **FR-002**: Analyst-action ingestion MUST cover a configurable market-wide universe that is a superset of holdings, watchlist, and open opportunity candidates — breadth beyond the book is a requirement, not an option.
- **FR-003**: Each stored analyst action MUST carry: ticker, research firm, action type, prior and new rating and/or price target where published, action date, originating source reference, and retrieval time. Records MUST be deduplicated across sources by logical identity (ticker + firm + action date + action type).
- **FR-004**: System MUST expose a query surface for analyst actions filterable by ticker, date range, and action type, returning source attribution suitable for Ledger's "every claim carries a fresh source" rule.
- **FR-005**: System MUST provide a valuation snapshot for a requested equity ticker containing: forward P/E, EV/EBITDA, dividend yield; each metric compared to the ticker's own 5-year average; a peer comparison over a named peer set (defaulted from sector/industry, overridable per request); consensus price target and implied upside vs current price.
- **FR-006**: Valuation snapshots MUST report unavailable metrics explicitly and carry data staleness flags consistent with existing quote metadata; the system MUST NOT fabricate or default missing financial values.
- **FR-007**: System MUST allow registering external news sources (feed or page reference plus optional keyword filters) attached to a specific thesis, and MUST maintain a market-wide default feed set independent of holdings.
- **FR-008**: News ingestion MUST tag stored articles with matched tickers and matched theses, and the existing news query surface MUST support filtering by thesis.
- **FR-009**: Ingestion failures (analyst actions or news sources) MUST surface through the existing data-freshness alerting path after 2 consecutive failures per source, consistent with existing pipeline alerting standards.
- **FR-010**: The opportunity-candidate pipeline MUST accept candidates originating from Ledger's own research, distinguishable by source from user-created and scan-nominated candidates.
- **FR-011**: This feature MUST NOT add any new push/notification channel. All new capabilities are query-side; pushing remains governed by the existing scan materiality rules.

### Key Entities

- **Analyst Action**: One street event about one ticker — firm, action type (upgrade / downgrade / initiate / target change / top-ideas mention), prior/new rating, prior/new price target, action date, source, retrieval time.
- **Ingestion Source**: A configured external origin (analyst-actions page or news feed), its enablement state, and its freshness/failure state.
- **Thesis Source Registration**: Link between an investment thesis and an external news source with optional keyword filters (e.g. DRAM thesis ← TrendForce press releases).
- **Valuation Snapshot**: A computed (not necessarily persisted) view of a ticker's valuation metrics vs its own history and a named peer set, with staleness and availability flags.
- **Universe Member**: A ticker included in the analyst-actions ingestion universe and the reason it is included (index constituent, holding, watchlist, candidate, manual).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For a well-covered US large-cap ticker, a query for "street actions in the last 30 days" returns at least one correctly sourced action in a single tool call, whether or not the ticker is held.
- **SC-002**: A valuation snapshot for a large-cap ticker returns all three core metrics with 5-year comparison and implied upside in one tool call, with zero fabricated values across any 20-ticker sample (missing data is flagged, not invented).
- **SC-003**: With TrendForce registered to the DRAM thesis, a new TrendForce press release is queryable and thesis-tagged within 24 hours of publication.
- **SC-004**: The nightly ingestion runs unattended for 14 consecutive days with any per-source failure surfaced via the existing alerting path — zero silent data gaps.
- **SC-005**: Ledger can compose a weekly advisor letter referencing at least 3 non-held tickers using only this feature's query surface plus existing tools — no ad-hoc web fetches required for street actions or valuation framing.
- **SC-006**: Duplicate analyst-action records across sources are below 1% of stored rows in any 30-day window.

## Assumptions

- Scraping free public pages (Finviz, MarketBeat, Yahoo Finance) for personal, non-redistributed use is acceptable to the project owner; the design treats every source as unreliable-by-default (markup changes, blocking) with per-source failure isolation.
- Default analyst-actions universe: US-listed equities in a major large-cap index plus holdings, watchlist, and open candidates; configurable. Full-market (every listed ticker) ingestion is not required for v1.
- Analyst actions and valuation snapshots apply to equities only; crypto assets return explicit not-applicable results.
- Yahoo Finance remains the primary fundamentals/quotes source, consistent with the existing market-data service; this feature reads through existing infrastructure where possible.
- The consumer-side changes (weekly advisor-letter cron, agent persona updates enabling proactive/companion behavior) are agent-platform configuration performed after this feature ships, and are intentionally out of this spec.
- Existing noise discipline stands: acknowledged-violation and scan materiality rules from 2026-07-21 remain unchanged by this feature.

## Notes

- [DECISION] Guardian vs companion split: alerting/materiality rules (guardian) are untouched; this feature builds the query-side data breadth (companion fuel). Rationale: the 2026-07-21 nag fix showed push noise destroys trust; breadth must arrive as pull, not push.
- [DECISION] Breadth is a requirement (FR-002): the ingestion universe must exceed the book. Rationale: idea flow limited to held names makes concentration self-reinforcing; market-wide input is the anti-concentration mechanism and does not conflict with the IPS (which governs sizing, not curiosity).
- [DECISION] Candidate source: Ledger-originated ideas enter the existing opportunity pipeline as a distinct source value, reusing scoring/promote/reject flows from features 019/020 rather than a parallel ideas store.
- [OUT OF SCOPE] Advisor-letter cron, persona changes, forward-to-Telegram reaction flow — agent-side configuration, not backend code.
- [OUT OF SCOPE] Telegram channel ingestion (reading Denys's subscribed channels) — revisit only after the forward-to-Ledger loop proves value.
- [DEFERRED] Whole-market universe (all listed tickers) and analyst-accuracy tracking (grading firms' hit rates) — future iteration if v1 proves useful.
