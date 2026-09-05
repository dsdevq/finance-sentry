# Tasks — Spec 046: TrendForce Press Center Source Recovery

## [US1] Self-healing seed repair + permalink-only parse

- [x] T001 Capture the live markup of `https://www.trendforce.com/presscenter/news` and
      `https://www.trendforce.com/presscenter/` (2026-09-02) into
      `Companion/Fixtures/trendforce-presscenter-news-2026-09-02.html` and
      `trendforce-presscenter-hub-2026-09-02.html`, each with a header comment naming the root
      cause, and wire them into the test project as copied content
- [x] T002 `NewsSourceHealthTracker` — add `ClearFailures(NewsSource)` (enable + zero counter +
      drop reason) as the single definition of "this source's failure state is void"
- [x] T003 `INewsSourceRepository` + `NewsSourceRepository` — add `RemoveAsync`
- [x] T004 `NewsSourceSeedJob` — `LegacyTrendForceUrls`, `RepairLegacyTrendForceSourcesAsync`:
      rewrite the legacy row to the canonical URL and clear its failure state; delete it
      instead when a canonical row already exists; run before the insert path
- [x] T005 `RegisterThesisSourceCommandHandler` — clear failure state when re-enabling an
      existing source, so a re-registered source above the disable threshold gets a real retry
- [x] T006 `TrendForcePageSource` — gate the selected anchor on `ArticleHrefPattern` across
      both the class-selector and href-fallback discovery paths; keep the throw when cards are
      present but no permalink is
- [x] T007 `TrendForcePageContractTests` — assert both live fixtures parse non-empty with
      title/URL/date, and that the hub fixture yields permalinks only (no chart/video/off-site)
- [x] T008 `NewsSourceSeedJobTests` — legacy row repaired in place (id/thesis/keywords kept),
      failure state cleared, idempotent on a second run, duplicate legacy row deleted, an
      unrelated source left alone, a fresh install still seeding the canonical URL, and the
      repair not inheriting the insert path's DRAM-thesis gate
- [x] T009 `NewsSourceFailureCounterTests` — `ClearFailures` revives a disabled, 17-failure
      source and gives it a full run of attempts before it retires again
- [x] T010 `RegisterThesisSourceCommandTests` — re-registering a retired source resets its
      counter, updates its thesis binding, and a new URL still adds an enabled source

### Refactor taken along the way

- [x] `FakeNewsSourceRepository` lifted out of `SearchMarketNewsQueryTests` into the shared
      `CompanionFakes.cs`, and given real `Update`/`Remove` semantics plus copy-on-read (the
      previous private copy had a no-op `UpdateAsync`, which would have made the repair tests
      pass without the repair persisting anything)

## Verification

- [x] `dotnet build FinanceSentry.sln --no-restore -c Release` — no new warnings (3 pre-existing
      `CS1587` in `Modules.Radar/Domain/Ports/IPortfolioScanDataReader.cs` are untouched)
- [x] `dotnet test FinanceSentry.sln --no-build -c Release --filter "Category!=Integration"` —
      **1262 passed, 0 failed, 6 skipped** across 11 assemblies
      (`FinanceSentry.Modules.Research.Tests`: 229 passed / 2 skipped)
- [x] Fixtures scanned for secret-like strings — only Bootstrap's boilerplate
      `exampleInputPassword1` label id, no credentials

## Deferred (post-deploy, Denys's call)

- [ ] SC-004: after the next deploy, confirm the seed job's
      `Repaired TrendForce source … cleared its failure state` log line, then that
      `consecutive_failures` stops climbing and `last_success_at` updates. The sandbox cannot
      reach the VPS, so this is outside this change's completion contract.
