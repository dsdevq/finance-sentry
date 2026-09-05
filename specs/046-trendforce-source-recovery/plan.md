# Implementation Plan: TrendForce Press Center Source Recovery

**Branch**: `046-trendforce-source-recovery` | **Date**: 2026-09-02 | **Spec**: [spec.md](./spec.md)

## Architecture Decisions

- **The repair lives in `NewsSourceSeedJob`, not in an EF migration.** Precedent is the job
  itself: feature 030 chose a scheduled idempotent seed job over a data migration precisely so
  the defaults could be corrected without a schema change (its own doc comment says so). The
  job already runs on startup and on a schedule, so a repair placed there self-heals the VPS on
  the next deploy with no manual `psql`. A migration would fire once and could not be re-run if
  Ledger re-registered the legacy URL afterwards.

- **Legacy URLs are an explicit list, not a normalisation rule.** `LegacyTrendForceUrls` holds
  the exact historic value (`https://www.trendforce.com/presscenter/`, seeded by `2822b04`).
  A general "strip the trailing slash and re-point" rule would silently rewrite user-registered
  sources; naming the one URL we shipped wrong keeps the blast radius at the row we broke.

- **Repair clears failure state as well as the URL.** Fixing the cause without clearing the
  effect leaves the row disabled at 17 failures forever — the counter is above
  `DisableThreshold`, so the very next failure re-retires it. `NewsSourceHealthTracker` gains a
  `ClearFailures` so "reset the health of a source" has one definition, used by both the seed
  repair and the register-command re-enable, rather than two copies of three assignments.

- **The duplicate case deletes rather than merges.** If both the legacy row and a canonical row
  exist (the state #326 would have produced on a second deploy), the legacy row cannot be
  rewritten onto an already-taken URL. Deleting it is right rather than merging: the canonical
  row is the one that has been successfully ingesting, and the legacy row's only distinct
  content is failure state we are discarding anyway. This needs `INewsSourceRepository.RemoveAsync`
  — the first delete on that port.

- **Permalink filtering moves into `NewsPageArticle` selection, applied to both discovery
  paths.** `ArticleHrefPattern` already existed but gated only the fallback. Rather than a
  second, divergent filter, the anchor chosen inside each card must match the same pattern, so
  the class-selector fast path and the href fallback agree on what an article is. When cards
  are found but none yields a permalink, `ParseAsync` still throws — an empty return would turn
  a loud drift signal into a silent coverage gap, which is the exact failure mode this issue is
  about.

- **Fixtures are committed verbatim, as files.** Every existing TrendForce fixture is a hand-
  written inline const, which is what let the real page drift unnoticed. The captured bytes are
  the evidence the issue asks for ("the next drift diagnosis starts from evidence"), so they go
  in as `.html` files under `Companion/Fixtures/` with a `<!-- -->` header naming the capture
  date and what broke. Inline consts are kept for the *synthetic* drift cases (they document
  shapes, not reality).

## Story Slice [US1] — Self-healing seed repair + permalink-only parse

One coherent slice: the URL repair is inert unless the failure counter is cleared, and both are
pointless if the page the row now points at ingests promo videos as DRAM news. Issue #318 is
sized as a single investigation-plus-fix PR.

### Files touched

- `backend/src/FinanceSentry.Modules.Research/Infrastructure/Jobs/NewsSourceSeedJob.cs` —
  `LegacyTrendForceUrls`, `RepairLegacyTrendForceSourceAsync`, called before the insert path
- `backend/src/FinanceSentry.Modules.Research/Application/Services/NewsSourceHealthTracker.cs` —
  `ClearFailures(NewsSource)`
- `backend/src/FinanceSentry.Modules.Research/Application/Commands/RegisterThesisSourceCommand.cs` —
  clear failure state on the re-enable path
- `backend/src/FinanceSentry.Modules.Research/Domain/Repositories/INewsSourceRepository.cs` +
  `Infrastructure/Persistence/Repositories/NewsSourceRepository.cs` — `RemoveAsync`
- `backend/src/FinanceSentry.Modules.Research/Infrastructure/Sources/TrendForcePageSource.cs` —
  permalink-gated anchor selection on both discovery paths
- `backend/tests/FinanceSentry.Modules.Research.Tests/Companion/Fixtures/*.html` — the two live
  captures
- `backend/tests/FinanceSentry.Modules.Research.Tests/Companion/TrendForcePageContractTests.cs` —
  fixture-file assertions
- `backend/tests/FinanceSentry.Modules.Research.Tests/Jobs/NewsSourceSeedJobTests.cs` — new
- `backend/tests/FinanceSentry.Modules.Research.Tests/Companion/NewsSourceFailureCounterTests.cs` —
  `ClearFailures` coverage

### Constraints discovered

- `INewsSourceRepository.GetByUrlAsync` is `AsNoTracking()`, so a repaired entity comes back
  detached; `UpdateAsync` uses `db.NewsSources.Update(...)`, which attaches it — repairs must go
  through `UpdateAsync`, never through mutating a tracked query result.
- The test project already references `Microsoft.EntityFrameworkCore.InMemory` and has
  `CompanionTestContext.Create()`; `NewsSourceSeedJob` needs a real `ResearchDbContext` (it
  queries `Theses` directly) plus a mocked `INewsSourceRepository`.
- `dotnet build --no-restore` fails on a fresh checkout of this branch — restore first.
- Backend-only diff: the husky pre-commit hook only runs the frontend gate when frontend files
  are staged, so no `--no-verify` is needed here.
