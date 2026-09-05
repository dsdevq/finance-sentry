# Feature Specification: Infra-free proof that a 4000-char thesis survives save and read-back

**Feature Branch**: `goal/issue-443-fix-lifekit-hq-f941c1`

**Created**: 2026-09-03

**Status**: In progress

**Input**: Issue #443 — "Risk-gate denominator uses phantom cost basis + save_thesis truncates narrative (~60 char cap)", plus review feedback on PR #541.

## Context

Feature [042](../042-verify-gate-443/spec.md) got the unfiltered solution run green on a Docker-less
host by tagging `ThesisTextColumnLimitTests` with `[DockerRequiredFact]` and adding
`ThesisTextLengthModelTests` to keep #443 Bug 1 covered.

Review of PR #541 rejected that trade as incomplete, and correctly:

- `ThesisTextColumnLimitTests` — the only test that actually *writes then reads* a 4000-char
  thesis — is skipped everywhere the gate runs. A skipped test proves nothing.
- `ThesisTextLengthModelTests` reads EF model metadata (`GetMaxLength()`, `GetColumnType()`). It
  catches somebody shrinking `HasMaxLength`, but it never moves a byte through a database. It
  cannot catch truncation introduced anywhere *between* the MCP tool and the column — a `Substring`
  in the command handler, a narrowed DTO, a lossy value converter — which is the shape #443 Bug 2
  actually took.

So the #443 no-truncation guarantee currently has no executing end-to-end proof on the gate. This
feature supplies one that needs no Docker, no network and no external database.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The no-truncation guarantee is proved by a test that actually runs (Priority: P1)

An engineer runs the gate command — `dotnet test backend/FinanceSentry.sln`, no filter — on a host
with no Docker daemon. Among the tests that **execute and pass** is at least one that writes a
4000-character thesis through the production save path into a real relational database and reads it
back out of a fresh connection, asserting the text came back byte-for-byte.

**Acceptance**:

- The round-trip test executes (not skipped) on a Docker-less host and passes.
- The write goes through `SaveThesisCommandHandler` → `ThesisRepository` → a real relational
  provider, not a hand-rolled shortcut and not the EF in-memory provider (which never emits SQL).
- The read comes from a **separate** `DbContext`, so the change tracker cannot serve the answer.
- Shrinking `ThesisText`'s declared max length back towards the #443 cap makes the round-trip test
  **fail**. A test that would pass against the bug it names is not a regression test.

### Edge Cases

- **SQLite does not enforce `varchar(n)` widths.** A naive SQLite round-trip would pass even with
  the column shrunk to 60, because SQLite would happily store 4000 characters anyway. The fixture
  must therefore make the storage layer enforce the *production model's declared* limit, or the
  test is vacuous.
- **The rest of the Research schema is Postgres-only** (`real[]` embedding vectors, `jsonb`
  columns, `gen_random_uuid()` defaults). Creating the whole schema on SQLite is not possible; the
  fixture must narrow to the one table under test without re-declaring its mapping by hand.
- **Adding a SQLite provider pulls a flagged native dependency.** `NuGetAudit` runs as
  warnings-as-errors here, so the transitive `SQLitePCLRaw.lib.e_sqlite3` version must be lifted.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: At least one test MUST persist a thesis whose `ThesisText` is exactly at the declared
  column limit through the production command handler and repository, read it back through a
  separate context, and assert equality with the input — using no Docker, no network and no
  external database process.
- **FR-002**: The storage layer used by that test MUST reject text longer than the limit the
  production `ResearchDbContext` declares, and that limit MUST be read from the production model
  rather than restated in the test fixture.
- **FR-003**: The test fixture MUST reuse the production `InvestmentThesis` mapping (max length,
  required-ness, table and column names) rather than re-declaring it, so a mapping change is felt
  by the test.
- **FR-004**: No existing test may be deleted, emptied or loosened. `ThesisTextColumnLimitTests`
  and `ThesisTextLengthModelTests` both stay exactly as they are — the new test is additional
  evidence, not a replacement.
- **FR-005**: The solution MUST continue to build with zero warnings, including `NuGetAudit`.

### Key Entities

- `ThesisSqliteFixture` — an in-memory SQLite database carrying only the `theses` table, mapped by
  the production `ResearchDbContext.OnModelCreating`, with the declared `ThesisText` max length
  projected into a CHECK constraint so SQLite enforces the width Postgres enforces natively.
- `ThesisTextRoundTripTests` — the executing save→read proof.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On a host with no Docker daemon, `dotnet test backend/FinanceSentry.sln` reports the
  round-trip tests as **passed**, not skipped.
- **SC-002**: A 4000-character thesis read back from a separate context equals the input exactly.
- **SC-003**: With `HasMaxLength(4000)` mutated to `HasMaxLength(60)` in `ResearchDbContext`, the
  round-trip tests fail. (Run as a one-off mutation check; the mutation is not committed.)
- **SC-004**: Text one character past the limit is refused by the storage layer with a
  `DbUpdateException` rather than silently truncated.
- **SC-005**: `dotnet build FinanceSentry.sln -c Release` reports 0 warnings and 0 errors.

## Assumptions

- SQLite is an acceptable stand-in for Postgres for *this* property. It proves the application path
  moves 4000 characters intact; it does not prove Postgres's own `varchar(4000)` behaviour. That
  remains `ThesisTextColumnLimitTests`' job on a Docker-capable host, and the model assertion in
  `ThesisTextLengthModelTests` pins the declared width. The three are complementary.
- Narrowing the SQLite model to a single table is safe here because the property under test is a
  single column on a single entity with no required relationships.
