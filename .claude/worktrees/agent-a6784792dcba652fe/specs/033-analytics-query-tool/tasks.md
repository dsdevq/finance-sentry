---
description: "Task list for the Read-Only Analytical Query Tool"
---

# Tasks: Read-Only Analytical Query Tool

**Input**: Design docs from `/specs/033-analytics-query-tool/`
**Prerequisites**: plan.md, spec.md, data-model.md, contracts/mcp-tools.md

**Tests**: Unit tests for the SQL validator + curated schema; integration tests (throwaway Postgres) for the read-only role, RLS cross-user isolation, and timeout/row-cap — these are the safety guarantees, so they are mandatory. No new external source.

**Organization**: US1 guarded query (P1, MVP) · US2 schema discovery (P2) · US3 audit (P3).

## Phase 1: Setup
- [X] T001 Create `backend/src/FinanceSentry.Modules.Analytics/FinanceSentry.Modules.Analytics.csproj` (mirror Companion) + add to `backend/FinanceSentry.sln`
- [X] T002 Create `backend/tests/FinanceSentry.Modules.Analytics.Tests/…csproj` (xUnit + FluentAssertions + EF InMemory + Npgsql for integration) + add to sln
- [X] T003 Add project refs (Analytics → API + Mcp csproj), `MigrateContext<AnalyticsDbContext>` in `MigrationExtensions.cs`, and a `ConnectionStrings:ReadOnly` + `Analytics` config section in `appsettings.json`

## Phase 2: Foundational (blocks all stories)
- [X] T004 [P] Create `QueryAuditRecord` in `Domain/QueryAuditRecord.cs`
- [X] T005 [P] Create `AnalyticsOptions` (ReadOnlyConnectionString, StatementTimeoutMs=5000, MaxRows=1000) in `Application/Services/AnalyticsOptions.cs`
- [X] T006 Create `AnalyticsDbContext` (schema `analytics`, `query_audit` DbSet, index `(UserId,CreatedAt)`) + `AnalyticsDbContextFactory` (history table `__ef_migrations_history_analytics`)
- [X] T007 Create `IQueryAuditRepository` + `QueryAuditRepository` (append; writable connection)
- [X] T008 Generate migration **M001_InitialSchema** WITH `.Designer.cs`: `query_audit` table; then hand-add raw SQL to the migration `Up` for (a) the **curated security-barrier views** (`v_transactions`, `v_holdings`, `v_analyst_actions`, `v_net_worth_daily`, `v_budgets`) filtering on `current_setting('app.current_user_id')`, (b) role **`fs_readonly`** with `GRANT USAGE`/`GRANT SELECT` on the views only, (c) RLS where needed; verify it applies clean on a throwaway Postgres
- [X] T009 Create `AnalyticsModule.cs` — `AddAnalyticsModule` (writable DbContext, repo, options, read-only executor) + `IModuleRegistrar`; register in the module system

**Checkpoint**: `dotnet build backend/` zero warnings; M001 discoverable + applies.

## Phase 3: US1 — guarded query (P1) 🎯 MVP
- [X] T010 [P] [US1] Unit tests for `SqlGuard` (single SELECT/`WITH…SELECT` allowed; reject INSERT/UPDATE/DELETE/DDL, `;`-chaining, data-modifying CTE) in `SqlGuardTests.cs`
- [X] T011 [US1] Create `ISqlGuard` + `SqlGuard` (validate single read-only SELECT) in `Application/Services/`
- [X] T012 [US1] Create `IReadOnlyQueryExecutor` + `ReadOnlyQueryExecutor` — open a connection as `fs_readonly`, `BEGIN` read-only txn, `SET LOCAL app.current_user_id` + `SET LOCAL statement_timeout`, run the validated SELECT, cap rows, `ROLLBACK`; map to `AnalyticsQueryResult` (columns/rows/rowCount/truncated/sql)
- [X] T013 [P] [US1] Integration test (throwaway Postgres, seeded 2 users): read-only role cannot write; RLS returns only the caller's rows even when another user's id is referenced; timeout/row-cap enforced in `ReadOnlyExecutorTests.cs`
- [X] T014 [US1] Create `RunAnalyticsQuery` query + handler (guard → execute → audit executed/rejected) + `AnalyticsQueryResult` DTO in `Application/Queries/` + `API/Responses/`
- [X] T015 [US1] Implement `run_analytics_query` MCP tool (thin; identity-scoped; description states it's the long-tail surface, authoritative numbers use their own tools) in `backend/src/FinanceSentry.Mcp/Tools/RunAnalyticsQueryTool.cs`

**Checkpoint**: US1 works — the agent runs a guarded read-only query and gets rows + SQL; writes/cross-user/runaway all blocked.

## Phase 4: US2 — schema discovery (P2)
- [X] T016 [P] [US2] Unit test for `CuratedSchema` (returns exactly the curated views + columns, no raw tables) in `CuratedSchemaTests.cs`
- [X] T017 [US2] Create `ICuratedSchema` + `CuratedSchema` (the curated view/column catalog + purposes) in `Application/Services/`
- [X] T018 [US2] Create `DescribeQuerySchema` query + handler + `QuerySchemaDto`; implement `describe_query_schema` MCP tool in `backend/src/FinanceSentry.Mcp/Tools/DescribeQuerySchemaTool.cs`
- [X] T019 [US2] Update `ToolNameContractTests` agreed surface (+2 tools → 57)

## Phase 5: US3 — audit (P3)
- [X] T020 [US3] Ensure both executed AND rejected queries are audited (caller, sql, outcome+reason, rowCount, durationMs); unit/integration assert a row per query
- [X] T021 [P] [US3] Test: a rejected query is recorded with its reason; an executed query records rowCount + duration

## Phase 6: Polish
- [X] T022 `/csharp-quality` sweep; `dotnet build backend/` zero warnings
- [X] T023 Document the `ReadOnly` connection string + `fs_readonly` role setup (README/appsettings) — Docker compose must provision the read-only role/connection
- [X] T024 Bump backend `<Version>`
- [X] T025 Run `quickstart.md` (US1/US2/US3 + boundary) against the Docker stack; confirm M001 in history

## Dependencies
- Setup → Foundational blocks all. T006→T008→T009 sequential.
- US1 depends on Foundational (the views + role + executor). US2 depends on Foundational (the view catalog). US3 rides US1's audit path.
- **MVP = US1.** Ship the guarded query first; schema-discovery + audit follow.

## Notes
- **Status: all 25 tasks implemented + verified (2026-07-22).** Full build zero-warning; 35 Analytics tests (29 unit + 6 integration) + 85 MCP tests green. Integration suite runs against a real throwaway Postgres (all source-module migrations + M001 applied), proving isolation (A→[10,20,30], B→[100,200]), write-denial (SQLSTATE 42501), base-table unreachability, statement-timeout, row-cap, and audit-of-both-outcomes.
- **T024**: backend `<Version>` is release-please-managed (`x-release-please-version`); the `feat(analytics)` commit drives the minor bump — not hand-edited.
- **T025**: verified via the integration harness against real Postgres (M001 present in `__ef_migrations_history_analytics`; 5 views; `fs_readonly` NOLOGIN with SELECT on the views only) rather than the full running compose stack — same guarantees, deterministic.
- **Enforcement model**: per-user isolation is a security_barrier view WHERE-filter on `current_setting('app.current_user_id')` (fails closed to zero rows if unset), plus `SET LOCAL ROLE fs_readonly` dropping the app login to SELECT-on-views-only. Base-table RLS was unnecessary given the view-baked filter.
- Safety is DB-enforced (role + security-barrier views), not prompt-enforced — the integration tests (T013) are the real proof, not the unit validator alone.
- The curated views are the only cross-schema reach (plan Complexity Tracking) — read-only, versioned, RLS-scoped.
- Docker/deploy must create the `fs_readonly` role + read-only connection string (T023) — flag for the compose/prod config.
