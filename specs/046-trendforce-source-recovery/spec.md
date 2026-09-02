# Feature Specification: TrendForce Press Center Source Recovery

**Feature Branch**: `046-trendforce-source-recovery`

**Created**: 2026-09-02

**Status**: Implemented

**GitHub Issue**: #318

## Context

Ledger reported on 2026-07-23 that the DRAM/HBM thesis has a news-coverage gap: the
`TrendForce Press Center` source had **17 consecutive failures**, last alert 2026-07-23 10:00
UTC, attached to DRAM thesis `9c091f57…`, keywords `DRAM`/`HBM`/`NAND`/`memory`. The thesis
monitor reported no thesis breaks, so this is source runtime health, not thesis invalidation.

### Diagnosis (evidence, 2026-09-02)

The sandbox reaches `trendforce.com` directly, so the whole fetch/parse path was reproduced
against the live site rather than guessed at.

**What is NOT the cause** — ruled out by measurement:

| Hypothesis | Evidence against |
|---|---|
| TrendForce blocks server-side requests / missing UA | `HTTP 200`, 174 KB, both with and without a browser UA. Reproduced through an `HttpClient` configured exactly as `ResearchModule` configures the `trendforce` named client (20 s timeout, Chrome UA, `DecompressionMethods.All`): 200 on both URLs. |
| TLS / network failure | No failure from this network. The VPS is unreachable from the sandbox so it cannot be excluded there, but nothing in the code depends on it. |
| Markup drift breaking `ParseAsync` **today** | Live `/presscenter/news` parses to **5 articles** with correct titles, absolute URLs and dates; live `/presscenter/` parses to 25 nodes. Neither throws. |
| Stale seed constant in code | Already corrected by #326 on 2026-07-24. |

**What IS the cause** — a three-link chain, each link established from git history:

1. **The deployed row points at the wrong page.** Feature 030 (`2822b04`, 2026-07-22) seeded
   `TrendForceUrl = "https://www.trendforce.com/presscenter/"` — the press-center *hub*, not
   the news list. That is exactly the URL issue #318 reports. `/presscenter/` 301-redirects to
   `/presscenter`, a promo landing page whose cards are `.advs-box.niche-box-post`; the parser
   shipped in 030 knew only `.press-news-list .list-items > .list-item`, `article` and
   `.press-item`, and had no href fallback. So `ParseAsync` threw `NewsSourceParseException`
   on **every** run from the day the feature deployed — matching the failure onset.

2. **The correction never reached the deployed row.** #326 (`29dc1e9`, 2026-07-24) changed the
   seed *constant* to `/presscenter/news`, but `NewsSourceSeedJob.SeedTrendForceAsync` is
   insert-only and matched **by the new URL**: `GetByUrlAsync("…/presscenter/news")` returns
   null, so it inserts a *second* source and leaves the broken `/presscenter/` row untouched
   and still enabled. A seed job that is idempotent by URL can never repair a row whose URL is
   the thing that is wrong.

3. **The failure counter is sticky, so the source cannot recover.** `NewsSourceHealthTracker`
   disables a source at 12 consecutive failures; the observed count is 17, which is only
   reachable if the row was re-enabled after being retired. `RegisterThesisSourceCommandHandler`
   (the path behind Ledger's `register_thesis_source` tool) sets `Enabled = true` but leaves
   `ConsecutiveFailures` at its old value — so a re-enabled source sitting at 17 is
   auto-disabled again by its very first failure, with no grace period. Even after #342
   (`94d5d1e`, 2026-08-05) taught the parser the `.niche-box-post` shape and added the
   permalink fallback — which is why the hub page parses today — the row stays dark, because
   nothing ever resets the counter and nothing ever fixes the URL.

### A fourth defect found while capturing the fixture

Parsing the live `/presscenter/` hub yields 25 "articles", of which four are not press
releases at all: a `dramexchange.com` spot-price promo, `/presscenter/chart/20231017-40.html`,
and two `/presscenter/video/…` items from 2019–2020. `ArticleHrefPattern` gates only the
*fallback* discovery path; the class-selector fast path accepts any `a[href]` inside a card.
So a source pointed at a promo page silently ingests 2019 webinar teasers as DRAM news. The
gap this issue reports is "no coverage"; left alone it would become "wrong coverage".

---

## User Scenarios

### [US1] The TrendForce source recovers itself and stays on the real news list (P1)

**Acceptance Scenarios**:

1. **Given** a deployed `news_sources` row named `TrendForce Press Center` whose URL is the
   legacy `https://www.trendforce.com/presscenter/`, **When** `NewsSourceSeedJob` runs,
   **Then** that row's URL is rewritten to `https://www.trendforce.com/presscenter/news` —
   repaired in place, keeping its id, thesis binding and keywords.
2. **Given** the legacy row has been repaired, **When** the seed job runs again, **Then** no
   duplicate TrendForce source exists and nothing further is written.
3. **Given** a legacy row that was auto-disabled with 17 consecutive failures, **When** the
   seed job repairs its URL, **Then** it is re-enabled and its failure counter is reset — a
   row whose cause of failure has just been removed must not stay retired.
4. **Given** both a legacy `/presscenter/` row and a correct `/presscenter/news` row exist
   (the duplicate #326 would have created), **When** the seed job runs, **Then** the legacy
   row is removed rather than rewritten onto a URL that is already taken.
5. **Given** a source is re-registered through `register_thesis_source` while sitting above
   the disable threshold, **When** it is re-enabled, **Then** its consecutive-failure counter
   and last-failure reason are cleared, so it gets a full run of attempts before retiring
   again.
6. **Given** the markup captured live from `https://www.trendforce.com/presscenter/news` on
   2026-09-02, **When** `ParseAsync` runs against it, **Then** it returns a non-empty list
   whose entries all carry a title, an absolute article URL and a published date.
7. **Given** the markup captured live from `https://www.trendforce.com/presscenter/` on
   2026-09-02, **When** `ParseAsync` runs against it, **Then** it returns a non-empty list
   containing **only** `/presscenter/news/<date>-<id>.html` permalinks — no `chart`, `video`
   or off-site promo entries.
8. **Given** a page whose cards exist but carry no article permalinks at all, **When**
   `ParseAsync` runs, **Then** it throws `NewsSourceParseException` rather than returning an
   empty list — drift must stay loud (FR-009, feature 030).

---

## Functional Requirements

- **FR-046-01** `NewsSourceSeedJob` MUST repair a TrendForce source whose URL is a known
  legacy value, rather than only inserting when absent.
- **FR-046-02** Repairing a source MUST clear its failure state (`Enabled = true`,
  `ConsecutiveFailures = 0`, `LastFailureReason = null`).
- **FR-046-03** Repair MUST be idempotent and MUST NOT create a duplicate or collide with an
  existing row already on the canonical URL.
- **FR-046-04** Re-enabling a source through `RegisterThesisSourceCommand` MUST reset its
  failure counter.
- **FR-046-05** `TrendForcePageSource.ParseAsync` MUST only emit entries whose URL is a
  press-release permalink, on both the class-selector and the href-fallback discovery paths.
- **FR-046-06** `ParseAsync` MUST continue to throw `NewsSourceParseException` when a page
  yields no article at all.
- **FR-046-07** The current live markup of both TrendForce URLs MUST be committed as fixtures,
  each carrying a header naming what broke, so the next drift diagnosis starts from evidence.

## Success Criteria

- **SC-001** `TrendForcePageContractTests` proves a non-empty, permalink-only parse against
  both committed live fixtures.
- **SC-002** `dotnet test backend/FinanceSentry.sln --filter "Category!=Integration"` passes.
- **SC-003** No secrets in fixtures or logs (the fixtures are public marketing pages).
- **SC-004** *(post-deploy, not part of this contract — the sandbox cannot reach the VPS)*
  `consecutive_failures` stops increasing and `last_success_at` updates.
