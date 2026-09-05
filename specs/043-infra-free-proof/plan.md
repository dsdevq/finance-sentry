# Implementation Plan: Infra-free proof that a 4000-char thesis survives save and read-back

**Spec**: [spec.md](./spec.md) · **Tasks**: [tasks.md](./tasks.md)

## Approach

Add an in-memory **SQLite** fixture to `FinanceSentry.Modules.Research.Tests` and drive the real
`SaveThesisCommandHandler` → `ThesisRepository` write path against it, then read back from a
second `DbContext` on the same connection.

## Load-bearing decisions

- **SQLite, not the EF in-memory provider.** In-memory never generates SQL, never binds a
  parameter and never materialises from a row, so it cannot witness truncation. SQLite does all
  three in-process with no daemon. Precedent: `Microsoft.EntityFrameworkCore.InMemory` is already
  a test-only package here; SQLite joins it on the same footing.
- **Subclass `ResearchDbContext` rather than extract an `IEntityTypeConfiguration`.** The test
  context inherits the production `OnModelCreating`, so the `theses` mapping under test *is* the
  production mapping. Extracting a configuration class would have meant touching production code
  for a test's benefit and would have left the other 16 entities inconsistent.
- **`modelBuilder.Ignore(clrType)`, not `Model.RemoveEntityType`.** `RemoveEntityType` throws
  (`AssertCanRemove`) while foreign keys still point at the type — `ResearchChunk` → `ResearchDocument`
  is one such pair. `Ignore` drops the referencing relationships too.
- **Project the declared max length into a SQLite CHECK constraint.** SQLite gives `varchar(n)` no
  meaning, so without this the round-trip would pass even against the #443 bug. The constraint
  reads `GetMaxLength()` off the production model, so the enforced limit tracks
  `ResearchDbContext`, and the test's own `4000` constant is the #443 contract being checked
  *against* it. Verified by mutation: shrinking the model to 60 fails both round-trip tests.
- **Read back with `FindByTickerAsync`, not `ListAsync`.** Both are production read paths;
  `ListAsync` orders by a `DateTimeOffset`, which the SQLite provider refuses to translate.
- **Pin `SQLitePCLRaw.lib.e_sqlite3` to 2.1.13.** EF's SQLite provider resolves 2.1.11, which
  carries GHSA-2m69-gcr7-jv3q, and `NuGetAudit` is warnings-as-errors. Precedent: the existing
  `System.Security.Cryptography.Xml` transitive pin two lines above it in
  `Directory.Packages.props`.

## Story-slice surfaces

- **[US1] Executing save→read proof** — touches
  `backend/Directory.Packages.props` (SQLite + native pin),
  `backend/tests/FinanceSentry.Modules.Research.Tests/FinanceSentry.Modules.Research.Tests.csproj`,
  and two new files under `backend/tests/FinanceSentry.Modules.Research.Tests/Persistence/`.
  No production code changes. Constraint discovered: the Research schema cannot be created whole
  on SQLite (`float[]` vectors, `gen_random_uuid()` defaults), so the fixture narrows the model to
  `InvestmentThesis` and clears the Postgres-only column types and default-value SQL on it.

## Verify gate

Unchanged command — `dotnet test backend/FinanceSentry.sln` (no filter), which already runs
`FinanceSentry.Modules.Research.Tests`. The new tests need no infrastructure, so the gate now
executes the #443 round-trip proof instead of skipping it.
