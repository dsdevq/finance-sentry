# Feature Specification: Structured Data Sources (retire brittle scraping)

**Feature Branch**: `037-structured-data-sources`
**Created**: 2026-08-05
**Status**: Implemented

## Decision Log

- **[DECISION 2026-08-05, Denys]** Phase-0 research falsified the provider table below: Finnhub's `/stock/upgrade-downgrade` and `/stock/price-target` are **premium-only**; FMP's free tier locks grades/targets/ratios to an ~87-ticker sample list; Alpha Vantage allows 25 calls/day (see `research.md` R1). Per this spec's own tension rule (free-insufficient ⇒ hardened scraper stays; paid = explicit separate decision), Denys chose the **free re-scope**: FR-001 is satisfied by (a) retiring the Yahoo `quoteSummary` analyst scraper outright, (b) keeping the hardened MarketBeat sweep as the per-action source with a config demotion flag, and (c) adding free structured **Finnhub `/stock/recommendation`** (monthly consensus counts, new `recommendation_trends` table) as the corroborating structured signal. FMP Starter ($22/mo) is the recorded escalation path if per-action coverage degrades.
**Origin**: Reliability session 2026-08-05. Reading VPS logs surfaced that the two most-broken data feeds are both *reverse-engineered scraping*, not real integrations: Yahoo's unofficial `quoteSummary` endpoints (crumb/cookie dance, intermittent 404s) for analyst actions, and HTML scraping of TrendForce. Scraping breaks silently and repeatedly; a stable contract does not.

## Problem

The research/companion data layer pulls several signals by scraping, because when it was built the fastest path was "parse what the website returns." Two classes of fragility resulted:

1. **Reverse-engineered private APIs** — Yahoo `quoteSummary/upgradeDowngradeHistory` (analyst actions) and `quoteSummary` valuation modules. These are undocumented, unversioned, rate-limited, and actively anti-scraped (crumb+cookie, UA sniffing, inconsistent 404/401/429). They work until Yahoo tweaks anything.
2. **HTML page scraping** — MarketBeat ratings table, TrendForce press center. Coupled to page markup that vendors restyle without notice (TrendForce drifted twice; the parser was hardened to key on URL permalinks, but it is still scraping).

The 2026-08-05 fixes hardened these and added auto-disable + alerting so failures are *loud and bounded*. This spec is the durable follow-up: **move the highest-value signals onto structured contracts** so they stop breaking in the first place.

## Proposed direction

Prefer, in order: (1) an official/documented data API with a versioned JSON contract; (2) an RSS/Atom feed; (3) embedded structured data (JSON-LD / news sitemap); (4) DOM scraping only where none of the above exist.

Candidate providers with **free tiers** offering structured, documented endpoints:

| Signal | Current (scraped) | Structured candidate(s) |
|---|---|---|
| Analyst upgrades/downgrades & price targets | Yahoo `quoteSummary` + MarketBeat HTML | Finnhub (`/stock/upgrade-downgrade`), Financial Modeling Prep (`/upgrades-downgrades`, `/price-target`) |
| Valuation multiples / fundamentals | Yahoo `quoteSummary` modules | FMP (`/ratios`, `/key-metrics`), Alpha Vantage (`OVERVIEW`) |
| Company/market news | RSS (already) + scraping for some | Keep RSS; Finnhub `/company-news` / `/news` where an RSS feed is absent |

TrendForce specifically has **no API, no RSS, no usable structured data** — it stays scraped (hardened) or gets dropped; it is out of scope for migration.

## Requirements *(mandatory)*

- **FR-001**: Analyst-actions ingestion MUST be able to source from a documented structured API behind the existing `IAnalystActionsSource` abstraction, with the current Yahoo/MarketBeat scrapers demoted to fallback (or removed) once parity is confirmed.
- **FR-002**: Provider API keys MUST be configuration (env/secret), never hard-coded; the layer MUST degrade gracefully (and log at Debug, not Error) when no key is configured, preserving today's key-less behavior.
- **FR-003**: A structured source MUST NOT introduce per-ticker Warning/Error log spam for expected empty results (no coverage / delisted) — parity with the 2026-08-05 log-hygiene fix.
- **FR-004**: Migration MUST be incremental and reversible per signal — swap analyst-actions first, validate against a week of real data (coverage + freshness vs the scraped baseline), then decide on valuation/news.
- **FR-005**: Free-tier rate limits MUST be respected (documented budget per provider); the universe sweep MUST fit within them or degrade deterministically.

## Success Criteria

- **SC-001**: Analyst-actions failures attributable to source drift/anti-scraping drop to ~zero over a 30-day window (baseline: the 2026-08-05 Yahoo 404 warnings).
- **SC-002**: Coverage (tickers with ≥1 action/quarter) is ≥ the scraped baseline for the same universe.
- **SC-003**: Adding a new provider is a new `IAnalystActionsSource` implementation + config, no changes to ingestion/scoring.

## Assumptions & tension to resolve

- **"Build in Finance Sentry over paid APIs"** (Denys's standing preference) still holds: the point is not to *pay* for data but to consume a **stable contract** instead of scraping. Free tiers of Finnhub/FMP/Alpha Vantage give exactly that. Where a free tier is insufficient, the fallback is the hardened scraper, not a paid plan — a paid plan is an explicit, separate decision.
- The number-crunching/aggregation stays in FS; only the *upstream fetch* changes from "scrape a page" to "call a documented endpoint."

## Out of scope

- TrendForce (no structured option — stays hardened-scraped or dropped).
- Paid data tiers (separate decision if free tiers prove insufficient).
- Any change to how signals are stored, scored, or surfaced downstream.
