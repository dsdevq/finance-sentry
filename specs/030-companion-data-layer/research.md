# Research: Companion-Mode Data Layer

**Feature**: 030-companion-data-layer | **Date**: 2026-07-21

## R1. Analyst-actions sources — which two free sources?

**Decision**: Primary market-wide sweep = **MarketBeat daily ratings page** (`https://www.marketbeat.com/ratings/` — one HTML table listing the day's actions across the whole market, including price targets). Per-ticker depth + corroboration = **Yahoo `quoteSummary` `upgradeDowngradeHistory` module** (JSON: firm, fromGrade, toGrade, action, epochGradeDate), fetched per universe ticker using the existing crumb/cookie pattern.

**Rationale**:
- MarketBeat gives true market-wide breadth (FR-002) from a single page per day — no universe iteration needed — and carries price targets, which Yahoo's module lacks.
- Yahoo `upgradeDowngradeHistory` is structured JSON on infrastructure we already run (crumb + cookie + named client from `YahooEarningsCalendarService`), giving a second independent source (FR-001) and per-ticker backfill.
- Finviz demoted: its free pages carry ratings only per-ticker (`quote.ashx`) behind aggressive bot detection; the market-wide export is Finviz Elite (paid). Kept as a documented fallback source, not v1.

**Alternatives considered**: Finviz screener scraping (bot-detection risk, no market-wide free view); Benzinga API (paid); stockanalysis.com (undocumented API, ToS unclear).

## R2. HTML parsing — new dependency or hand-rolled?

**Decision**: Add **AngleSharp** (MIT) for HTML parsing of the MarketBeat table.

**Rationale**: Regex/string parsing of an HTML table is the classic silent-corruption path; the spec's edge cases demand that markup drift produce a *visible source failure*, which a real parser's structural assertions (expected columns, header names) provide. AngleSharp is dependency-free and well-maintained. This is the only new NuGet package.

**Alternatives considered**: HtmlAgilityPack (fine too; AngleSharp has stricter standards-based parsing); zero-dependency string parsing (rejected: fragility becomes invisible, violating FR-009's spirit).

## R3. Valuation snapshot data — where do metrics and 5-year history come from?

**Decision**:
- **Current metrics**: Yahoo `quoteSummary` modules `summaryDetail` (trailing P/E, forward P/E, dividend yield), `defaultKeyStatistics` (enterprise value), `financialData` (target mean price, EBITDA, total debt/cash) — same crumb pattern, new named client or reuse `"yahoo-earnings"` template.
- **5-year history for trailing P/E**: computed from data we already have — EDGAR XBRL `DilutedEPS` quarterly series (existing `SecEdgarService.GetFundamentalsAsync`) rolled to TTM EPS, divided into Yahoo daily closes (existing `GetDailyClosesAsync`). Honest, source-grounded, zero new dependencies.
- **5-year history for EV/EBITDA and dividend yield**: *not reliably available free* → v1 reports these as `historyUnavailable` (FR-006 honesty rule) and starts accruing our own history: every computed snapshot is persisted, so the comparison window grows organically.

**Rationale**: no free source provides historical forward P/E or EV/EBITDA; fabricating or approximating silently would violate FR-006. The EDGAR+closes trailing P/E reconstruction uses two services that already exist and is deterministic.

**Alternatives considered**: macrotrends.net scraping (ToS-hostile, heavy JS); paid fundamentals APIs (excluded by policy); skipping history entirely (rejected — trailing P/E history is achievable now and is the highest-value comparison).

## R4. Analyst universe — how is "market-wide" defined and stored?

**Decision**: New `analyst_universe_members` table in the `research` schema, following the `RadarUniverseService.SyncAsync` compose-and-deactivate pattern: **checked-in S&P 500 constituent seed list** (JSON resource in the module) ∪ equity holdings ∪ watchlist ∪ open opportunity candidates ∪ manual additions. Departed members deactivate, never delete.

**Rationale**: Wikipedia-scraping the constituent list adds a fragile dependency for data that changes a few times a year; a checked-in seed with a `reason` column satisfies "configurable broad universe" (spec assumption) and updating it is a trivial PR. Radar's own universe (~20 tickers, structure-focused) is too narrow to reuse directly, but its service pattern is proven.

**Note**: MarketBeat ingestion is market-wide regardless (whatever the page lists); the universe governs *Yahoo per-ticker* ingestion, valuation-history capture, and the "no coverage in universe" vs "no recent actions" distinction (edge case in spec).

## R5. News breadth — market-wide default feeds and thesis sources

**Decision**: New `news_sources` table: registered sources with `Kind` (Rss | Page), optional `ThesisId` (null = market-wide default), optional keyword filters, per-source failure counters. `NewsIngestionJob` extends to iterate enabled sources alongside the existing per-ticker Yahoo feeds. Market-wide defaults seeded: Yahoo Finance top-stories RSS (`https://feeds.finance.yahoo.com/rss/2.0/headline?s=^GSPC...` variant), MarketWatch top stories RSS, plus the existing Fed feed (unchanged). TrendForce press-release page (`https://www.trendforce.com/presscenter/news`) registered to the DRAM thesis as the first `Page`-kind source.

**Rationale**: reuses the existing RSS pipeline (`RssMarketNewsService`, `ContentHash` dedup) rather than a parallel ingester; `Page` kind covers TrendForce (no RSS) with AngleSharp extracting headline links. Keyword filters keep thesis-tagged sources on-topic (e.g. "DRAM", "NAND", "contract price").

**Alternatives considered**: config-file source list (rejected: Ledger must be able to register sources at runtime via MCP per US3); separate thesis-news store (rejected: one article store, tags on top).

## R6. Thesis tagging on articles

**Decision**: Add a `ThesisIds` jsonb list column to `news_articles` (mirrors the existing `Tickers` list + `StringListComparer` pattern). Ingestion tags an article when (a) it came from a source registered to that thesis, or (b) a thesis-registered keyword matches title/summary. Query surface gains a thesis filter.

**Alternatives considered**: join-table (over-normalized for jsonb-list precedent already in this table); tagging via `Categories` (semantically muddy).

## R7. Ledger-originated candidates

**Decision**: Add `Ledger` to the existing `CandidateSource` enum. Enum is stored as string via `HasConversion<string>()` — code-only change, no migration risk. `add_candidate` MCP tool accepts the new source value (or a dedicated tool parameter defaulting appropriately).

## R8. Ingestion failure alerting

**Decision**: Reuse `IAlertGeneratorService.GenerateSyncFailureAlertAsync` semantics: each ingestion source tracks `ConsecutiveFailures`; on reaching 2 (FR-009), generate a sync-failure alert with `ReferenceId = source id` and the failure reason, respecting the existing active-alert dedup + silence window. Recovery resets the counter and resolves nothing retroactively (alerts resolve per existing flows).

**Rationale**: matches the radar freshness watchdog pattern exactly; `list_active_alerts` picks it up with zero new plumbing; the ledger-scan prompt already treats freshness alerts as "distrust this data" signals.

## R9. Job scheduling

**Decision**: One new Hangfire job `analyst-actions-ingestion`, nightly at 01:00 UTC (after `opportunity-scan` at 00:00). Valuation-history capture runs inside it for holdings ∪ watchlist ∪ candidates only (small set; full-universe capture deferred). News source ingestion rides the existing `research-news-tickers` 30-min cadence (registered sources appended to the run). Overlap protection via `[DisableConcurrentExecution]` (Hangfire built-in) per the spec's overlap edge case.

## R10. Testing approach

**Decision**: Per constitution: external-contract tests for (a) Yahoo `upgradeDowngradeHistory` JSON shape, (b) Yahoo `quoteSummary` valuation modules shape, (c) MarketBeat ratings-table structural assumptions (recorded HTML fixture; live smoke test marked explicit), (d) TrendForce page structure (fixture). Unit tests for: dedup identity, TTM EPS roll-up + trailing P/E series math, universe sync compose/deactivate, keyword matching, failure-counter/alert threshold. No REST endpoints → no REST contract tests; MCP tools are thin over CQRS handlers, which get the unit tests.
