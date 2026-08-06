# Implementation Plan: Structured Data Sources (retire brittle scraping)

**Branch**: `037-structured-data-sources` | **Date**: 2026-08-05 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/037-structured-data-sources/spec.md`

> **Scope decision (2026-08-05, Denys)**: Phase-0 research falsified the spec's provider table — no free tier offers per-action upgrades/downgrades for our universe (Finnhub paywalls the endpoint; FMP free is locked to an ~87-ticker sample list; Alpha Vantage is 25 calls/day — see research.md R1). Denys chose the **free re-scope (option B)**: retire the Yahoo `quoteSummary` analyst scraper outright, keep the hardened MarketBeat sweep as the per-action source, and add free structured **Finnhub recommendation trends** as the corroborating signal. FMP Starter ($22/mo) stays an explicit future option.

## Summary

Kill the single most fragile scraper in the codebase — `YahooAnalystActionsSource` (crumb/cookie dance, intermittent 404/401/429, the direct motivation for this spec) — and introduce the module's first *documented structured API* integration: Finnhub `/stock/recommendation` (free tier), captured nightly for the tracked set into a new `research.recommendation_trends` table, keyed and rate-limited per config, silent when no API key is configured. Per-action analyst data continues to flow from the hardened MarketBeat sweep (unchanged). The MCP `get_analyst_actions` tool gains an optional recommendation-trends block so Ledger can corroborate street actions against consensus drift.

## User Stories

- **US1 (P1) — Structured recommendation trends**: As the operator, I want monthly analyst-consensus counts (strongBuy/buy/hold/sell/strongSell) captured nightly from a documented API for every tracked ticker, so a stable contract accumulates corroborating street signal without scraping.
- **US2 (P2) — Retire the Yahoo analyst scraper**: As the operator, I want the Yahoo `quoteSummary` analyst source deleted (class, DI registration, named HttpClient, tests), so the recurring crumb/404 failure class disappears from logs and alerts. MarketBeat remains the sole per-action source.
- **US3 (P3) — Trends visible to Ledger**: As Denys-via-Ledger, I want `get_analyst_actions` (ticker-filtered) to include the latest recommendation trend for that ticker, so briefs can cite consensus direction alongside per-action events.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (backend only — no frontend changes)
**Primary Dependencies**: ASP.NET Core, EF Core (Npgsql), Hangfire, `FinanceSentry.Core.Cqrs` (hand-rolled `ICommand`/`IQuery` — no MediatR), `System.Net.Http` via `IHttpClientFactory`, `ModelContextProtocol` (existing MCP project). **No new NuGet packages** — Finnhub is plain REST+JSON.
**Storage**: PostgreSQL 14 — existing `ResearchDbContext` (schema `research`), migration **M010_RecommendationTrends** adding `recommendation_trends` (one row per ticker+period month, upserted). Research migrations M001–M009 exist; next is M010. `analyst_actions` untouched.
**Testing**: xUnit — TDD: contract test against a recorded Finnhub `/stock/recommendation` fixture (public static `Parse` + fixture, `YahooAnalystActionsSource` precedent), unit tests for mapping/throttle/429/key-absent behavior, live smoke gated on `FINNHUB_API_KEY` presence. Yahoo-analyst test files are deleted with the source.
**Target Platform**: Linux server (Docker on VPS); capture runs inside the existing nightly `analyst-actions-ingestion` Hangfire job (01:00 UTC), same pattern as the valuation-snapshot capture step.
**Project Type**: Backend module extension (`FinanceSentry.Modules.Research`) + one MCP tool enrichment (`FinanceSentry.Mcp`).
**Performance Goals**: Tracked-set capture (Holding/Watchlist/Candidate/Manual — tens of tickers) at ≤50 req/min ⇒ seconds per night; far inside Finnhub's 60/min free cap (FR-005). Retiring Yahoo *shortens* the nightly job.
**Constraints**: Free tier only (paid = separate future decision). Key-less deployments behave exactly as today minus Yahoo: MarketBeat-only ingestion, one Debug line, zero Warning/Error spam (FR-002/FR-003). Key sent via `X-Finnhub-Token` header — never query string, never logged (Principle V).
**Scale/Scope**: 1 new source service + options + repository + M010 migration + 1 job step + 1 deleted source + 1 MCP tool field. No REST controller changes ⇒ no API version bump trigger.

## Constitution Check

*GATE: passed pre-Phase-0; re-checked post-Phase-1 design.*

| Principle | Check | Status |
|---|---|---|
| I. Modular monolith / domain interfaces | Finnhub accessed only behind a new `IRecommendationTrendsService` domain interface; concrete class in `Infrastructure/Sources/`, DI-registered. No module references the concrete adapter. | PASS |
| II. Code quality (zero-warning builds) | Standard gate; `dotnet build` in `sdk:10.0` container after every file. | PASS |
| III. Multi-source integration (isolation, graceful failure) | Capture step gets per-run failure isolation like the valuation step (a Finnhub failure never fails the MarketBeat ingestion); 429 backoff bounded; 401/403 throws → existing 2-strike health alerting. | PASS |
| IV. AI analytics | US3 feeds Ledger richer context via MCP; no LLM changes. | N/A |
| V. Security-first | Key from env/config only, header-borne, never logged; key-less mode silent (FR-002). | PASS |
| VI. Frontend discipline | No frontend changes. | N/A |
| Testing discipline (TDD, contract tests) | External-API contract test (recorded fixture) ships with the source; fixture-first TDD. | PASS |
| Versioning | No REST contract change ⇒ no API version bump; MCP tool response gains an optional field (additive). release-please tags via `feat:` commit. | PASS |

**Post-design re-check**: PASS — one new table in an existing context, no new projects/endpoints; deletion of a source class reduces surface.

## Project Structure

### Documentation (this feature)

```text
specs/037-structured-data-sources/
├── plan.md              # This file
├── research.md          # Phase 0 (R1 falsified the spec's provider table → scope decision R2)
├── data-model.md        # Phase 1 — RecommendationTrend entity + M010
├── quickstart.md        # Phase 1 — key setup, manual run, verification queries
├── contracts/
│   └── finnhub-recommendation.md   # endpoint contract + recorded fixture shape
└── tasks.md             # Phase 2 (/speckit.tasks)
```

### Source Code (repository root)

```text
backend/
├── src/FinanceSentry.Modules.Research/
│   ├── Application/Services/
│   │   ├── AnalystSourcesOptions.cs             # NEW — Finnhub {ApiKey, BaseUrl, RequestsPerMinute}; Marketbeat.Enabled
│   │   └── IRecommendationTrendsService.cs      # NEW — domain interface (Principle I)
│   ├── Domain/
│   │   ├── RecommendationTrend.cs               # NEW — entity
│   │   └── Repositories/IRecommendationTrendRepository.cs   # NEW
│   ├── Infrastructure/
│   │   ├── Sources/FinnhubRecommendationTrendsService.cs    # NEW — impl + public static Parse
│   │   ├── Sources/YahooAnalystActionsSource.cs             # DELETED (US2)
│   │   ├── Persistence/Repositories/RecommendationTrendRepository.cs  # NEW
│   │   └── Jobs/AnalystActionsIngestionJob.cs   # MODIFIED — + CaptureRecommendationTrendsAsync step
│   ├── Migrations/…_M010_RecommendationTrends.cs # NEW — EF migration (dotnet ef, not hand-written — M007 lesson)
│   └── ResearchModule.cs                        # MODIFIED — options, named HttpClient, DI; Yahoo-analyst wiring removed
├── src/FinanceSentry.Mcp/Tools/GetAnalystActionsTool.cs     # MODIFIED (US3) — optional trends block on ticker queries
└── tests/FinanceSentry.Modules.Research.Tests/
    ├── Sources/FinnhubRecommendationTrendsServiceTests.cs   # NEW
    ├── Contracts/FinnhubRecommendationContractTests.cs      # NEW (+ key-gated live smoke)
    ├── Fixtures/finnhub-recommendation.json                 # NEW recorded fixture
    └── (Yahoo analyst source tests/fixtures)                # DELETED (US2)

docker/
├── docker-compose.prod.yml + docker-compose.dev.yml         # MODIFIED — FINNHUB_API_KEY → AnalystSources__Finnhub__ApiKey
└── .env.example                                             # MODIFIED — document FINNHUB_API_KEY (blank = trends off)
```

**Structure Decision**: Backend-only extension of the Research module following its established source pattern; net code *shrinks* on the scraping side (Yahoo analyst source + crumb machinery deleted).

## Design Notes (Phase 1)

1. **Not an `IAnalystActionsSource`** — recommendation trends are aggregate monthly counts, not per-event actions; forcing them through `AnalystActionRecord` would poison the actions table and its dedup key (research.md R7). They get their own narrow interface (`IRecommendationTrendsService`), entity, and repository.
2. **Capture step, not new job** — `AnalystActionsIngestionJob` gains `CaptureRecommendationTrendsAsync(members, ct)` mirroring the existing `CaptureValuationSnapshotsAsync`: tracked-set filter (`ValuationCaptureReasons`), per-ticker failure isolation, never fails the actions run. Reuses `IAnalystSourceHealth` under source key `finnhub` so repeated total failures alert via the existing 2-strike path.
3. **Registration gating (FR-002)** — empty `ApiKey` ⇒ a no-op `IRecommendationTrendsService` (or conditional skip in the job) + single Debug line at startup; no per-ticker noise ever (FR-003: expected-empty responses log at Debug).
4. **Upsert semantics** — unique index `(ticker, period)`; refetches update counts in place (Finnhub restates recent months). Append-only history across periods gives the trend.
5. **MarketBeat demotion flag** — `AnalystSources:Marketbeat:Enabled` (default `true`) ships anyway: it costs three lines and preserves FR-004's per-signal reversibility for the remaining scraper.
6. **Yahoo deletion boundary (US2)** — only the *analyst* source dies. `YahooMarketDataService` (quotes/bars), `YahooEarningsCalendarService`, and `YahooValuationDataService` are explicitly untouched — valuation migration is the deferred decision from the spec.
7. **M010 via `dotnet ef migrations add`** inside the sdk container — never hand-written (the M007 hand-rolled migration was silently never applied; see quotes-outage 2026-07-20 post-mortem).

## Complexity Tracking

> No constitution violations — table intentionally empty.
