# Research: 037 Structured Data Sources

**Date**: 2026-08-05 · **Method**: official docs/pricing pages fetched and parsed (not blog posts); Finnhub endpoint tier flags read from the docs page's embedded endpoint JSON.

## R1 — Provider free-tier viability for per-action analyst data (THE gating fact)

**Decision**: none of the three candidate providers can serve as a free structured source for per-action upgrades/downgrades over a ~460-ticker universe. The spec's provider table was written optimistically; verified reality:

| Provider | Per-action upgrades/downgrades | Rate limit (free) | Verdict |
|---|---|---|---|
| **Finnhub** | `/stock/upgrade-downgrade` — **premium-only** (docs flag: "Premium Access Required"); `/stock/price-target` also premium | 60 calls/min (+30/sec global cap) | ❌ endpoint paywalled |
| **FMP** | `/stable/grades*` + price-target endpoints — callable on free but **locked to a fixed ~87-ticker sample list** ("Symbol Limited to AAPL, TSLA, AMZN and 84 more"); historical grades additionally capped at 10 rows/call | 250 calls/day | ❌ symbol wall; full US coverage starts at **Starter $22/mo** (300 calls/min) |
| **Alpha Vantage** | n/a for actions; `OVERVIEW` fundamentals only | **25 calls/day** | ❌ budget far too small |

What IS free and structured:
- **Finnhub `/stock/recommendation`** — monthly aggregate analyst consensus (strongBuy/buy/hold/sell/strongSell counts) per ticker. Structured, documented, free — but an *aggregate trend*, not per-action events (no firm, no rating change, no date-of-action).
- **Finnhub `/company-news` + `/news`** — free, 1 year history.
- Finnhub auth: `X-Finnhub-Token` header (or `token` query param — we use the header; secrets never in URLs). Upgrade-downgrade response shape (if ever paid): `[{symbol, gradeTime(epoch), company, fromGrade, toGrade, action: up|down|init|reit}]` — near-identical to Yahoo's module, mapping would be trivial.

**Sources**: finnhub.io/docs/api/upgrade-downgrade · finnhub.io/docs/api/rate-limit · github.com/finnhubio/Finnhub-API/issues/122 · site.financialmodelingprep.com/developer/docs/pricing · site.financialmodelingprep.com/pricing-plans · alphavantage.co/support

## R2 — Consequence for FR-001 (structured analyst-actions source)

The spec's own tension rule applies: *"Where a free tier is insufficient, the fallback is the hardened scraper, not a paid plan — a paid plan is an explicit, separate decision."* Three honest paths, decision owner = Denys:

- **(A) FMP Starter, $22/mo** — the only structured per-action path. Full US symbols, grades + price targets + annual ratios, 300 calls/min. Buys exactly what the spec wanted; costs money against the standing "build over pay" preference.
- **(B) Free re-scope** — keep the hardened MarketBeat sweep as the per-action source (it survived 2026-08-05 hardening and carries price targets); **retire the Yahoo `quoteSummary` analyst scraper** (the crumb/cookie 404 generator that motivated this spec) and replace its corroboration role with free Finnhub **recommendation trends** as a new structured signal. Kills the worst scraper, stays free, but per-action data remains scraped (MarketBeat).
- **(C) Defer 037** — the 2026-08-05 hardening (auto-disable + alerting) already made failures loud-and-bounded; live with it.

**Chosen**: **(B) Free re-scope** — Denys, 2026-08-05, in-session. Per-action data stays on the hardened MarketBeat sweep; the Yahoo `quoteSummary` analyst scraper (crumb/cookie 404 generator) is retired outright; free structured Finnhub **recommendation trends** become the new corroborating signal for the tracked set (holdings/watchlist/candidates/manual). Paid FMP Starter remains an explicit future option if per-action coverage degrades — re-open then.

## R3 — Ingestion architecture reuse (independent of R2 outcome)

**Decision**: any new source (paid FMP, or free recommendation-trends) plugs in behind the existing `IAnalystActionsSource` / ingestion-job machinery: per-source failure isolation, `IAnalystSourceHealth` 2-strike alerting, logical-identity upsert, `source` column discrimination.
**Rationale**: this is exactly the swap-point feature 030 built; SC-003 ("new provider = new implementation + config") is already satisfied by the architecture.
**Alternatives considered**: a parallel "structured ingestion" pipeline — rejected, duplicates health/alert/upsert for zero benefit.

## R4 — Key handling & degradation (FR-002/FR-003)

**Decision**: provider API keys ride `AnalystSources:*:ApiKey` options bound from env (`FINNHUB_API_KEY` / `FMP_API_KEY`); an empty key means the source is *not registered* at DI time (one Debug line at startup, zero per-ticker log noise). Keys sent via request header, never query string, never logged.
**Rationale**: mirrors the ResearchRetrieval embedding-provider pattern (enabled-flag + key, off by default) already in the module.

## R5 — Rate-limit budget (FR-005)

**Decision**: recommendation trends are captured nightly for the **tracked set only** (Holding/Watchlist/Candidate/Manual — the same `ValuationCaptureReasons` filter the valuation-snapshot capture uses), not the full 460-ticker index seed: monthly-granularity aggregates gain nothing from nightly full-universe sweeps. Budget: tens of calls/night against Finnhub's 60/min free cap — trivially safe; in-source pacing to a configured `RequestsPerMinute` (default 50) plus bounded 429 backoff still ships as a guard.
**Alternatives considered**: nightly full-universe sweep (~460 calls, ~9 min) — rejected as waste for a monthly-cadence signal; a monthly full-universe pass can be added later if scanner corroboration wants breadth.

## R7 — Recommendation-trends storage & shape

**Decision**: new `research.recommendation_trends` table (migration **M010**, `ResearchDbContext` — next free number after M009), one row per `(ticker, period)` month with the five consensus counts; upsert-on-refetch. Finnhub `/stock/recommendation` returns `[{symbol, period: "YYYY-MM-01", strongBuy, buy, hold, sell, strongSell}]` — a direct column mapping.
**Rationale**: it is a genuinely different signal shape from `AnalystAction` (aggregate counts vs per-event); forcing it into pseudo-actions would poison the actions table and the dedup key. A dedicated table keeps the accumulation-layer philosophy: recorded silently, queryable later.
**Alternatives considered**: extra columns on `valuation_snapshots` (wrong cadence — those are daily point-in-time captures, trends are monthly periods with restatements); reusing `AnalystAction` with a synthetic type (rejected above).

## R6 — TrendForce

**Decision**: out of scope, unchanged (spec): no API, no RSS, no structured data — stays on the hardened scraper.
