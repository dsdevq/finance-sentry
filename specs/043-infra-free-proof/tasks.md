# Tasks: Infra-free proof that a 4000-char thesis survives save and read-back

**Spec**: [spec.md](./spec.md) · **Plan**: [plan.md](./plan.md)

## Phase 1 — [US1] The no-truncation guarantee is proved by a test that actually runs (P1)

- [x] T001 [US1] Add `Microsoft.EntityFrameworkCore.Sqlite` to `backend/Directory.Packages.props`
  and reference it from `FinanceSentry.Modules.Research.Tests.csproj` (FR-001).
- [x] T002 [US1] Pin the transitive `SQLitePCLRaw.lib.e_sqlite3` to 2.1.13 in
  `backend/Directory.Packages.props` — the 2.1.11 that EF resolves trips `NuGetAudit`
  (GHSA-2m69-gcr7-jv3q) and the build treats audit warnings as errors (FR-005).
- [x] T003 [US1] Add `Persistence/ThesisSqliteFixture.cs`: an in-memory SQLite database whose
  context subclasses `ResearchDbContext`, ignores every entity but `InvestmentThesis`, clears the
  Postgres-only default-value SQL and column types on the surviving entity, and projects the
  model's declared `ThesisText` max length into a CHECK constraint (FR-002, FR-003).
- [x] T004 [US1] Add `Persistence/ThesisTextRoundTripTests.cs`: save a 4000-char thesis and a
  200-char thesis through `SaveThesisCommandHandler` + `ThesisRepository`, read each back from a
  separate context, assert byte-for-byte equality; assert 4001 chars raises `DbUpdateException`
  (FR-001, SC-002, SC-004).
- [x] T005 [US1] Mutation-check the new tests: set `HasMaxLength(60)` on `ThesisText` in
  `ResearchDbContext`, confirm both round-trip tests fail, then revert (SC-003).
  *Outcome: both failed as required; mutation reverted and not committed.*
- [x] T006 [US1] Run `dotnet build FinanceSentry.sln -c Release` (0 warnings) and
  `dotnet test FinanceSentry.sln --no-build -c Release` with **no** filter; confirm the new tests
  are reported passed, not skipped, and that no previously-passing test regressed
  (SC-001, SC-005, FR-004).
