# Feature Specification: Close the #443 verify gate

**Feature Branch**: `goal/issue-443-fix-lifekit-hq-f941c1`

**Created**: 2026-09-03

**Status**: In progress

**Input**: Issue #443 — "Risk-gate denominator uses phantom cost basis + save_thesis truncates narrative (~60 char cap)"

## Context

Issue #443 listed three "Done when" criteria. Two of them already landed:

1. `save_thesis` persists a 200+ char narrative, with MCP/application regression coverage — landed in `ToolParityTests.SaveThesis_AcceptsLongNarrativeThesisText` and `McpToolBridgeTests.RealBridge_SaveThesis_AllowsNarrativeLengthThesisText`.
2. `check_risk_rules` sizes MinCashBuffer off current market value + cash, never cost basis — the production path (`BookSnapshotReader` → `IBookFiguresService.Positions[].UsdValue`) was fixed out of band and is pinned by `ToolParityTests.CheckRiskRules_Proposal_UsesCurrentValueBook_ForMinCashBuffer`.
3. **`dotnet test backend/FinanceSentry.sln` passes** — still open. This feature closes it.

The unfiltered solution run (the command the issue names as its verify gate, and the command `backend-ci.yml` has run since #509) currently reports three failures, none of them a production defect:

- `ThesisTextColumnLimitTests` (2 tests) throws `ArgumentException: Docker is either not running or misconfigured` from Testcontainers before any assertion runs. The `[Trait("Category","Integration")]` tag only helps a run that passes `--filter Category!=Integration`; the gate and CI pass no filter, so the tag protects nothing.
- `GetInstallmentFxImpactQueryHandlerTests.Handle_TotalsAcrossPlans_AndBuildsAMonthlySeries` asserts a hardcoded 28 series points. The handler ends the series at `DateOnly.FromDateTime(DateTime.UtcNow)`, so the true count grows by one every calendar month. It was correct in Aug 2026 and is wrong from Sep 2026 onward.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The #443 verify gate runs green on a machine without Docker (Priority: P1)

An engineer (or the DevClaw gate) checks out the branch and runs the exact command issue #443 names — `dotnet test backend/FinanceSentry.sln`, no filter. Every test either passes or is reported as genuinely skipped for a named missing dependency. No test fails, and no test was deleted or weakened to get there.

**Acceptance**:

- Full unfiltered solution run: zero failures.
- The two `ThesisTextColumnLimitTests` bodies still exist verbatim and still assert no-truncation round-trips.
- On a Docker-capable host (GitHub Actions `ubuntu-latest`) those two tests still execute for real.

### Edge Cases

- **Docker present but broken/unreachable.** The gate must detect absence at discovery time, not by catching an exception mid-test — a caught exception that silently passes would be a fake green.
- **Docker absent.** The varchar(4000) guarantee must still be verified by something, otherwise skipping the container tests loses the #443 Bug 1 regression coverage entirely.
- **Wall clock advances.** Any assertion over the FX series length must derive its expectation from the same `UtcNow` anchor the handler uses, so it cannot drift again next month.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Tests that require a Docker daemon MUST be reported as *skipped* with a human-readable reason when no daemon is reachable, and MUST execute normally when one is. Detection happens at test-discovery time.
- **FR-002**: No existing test function may be deleted, emptied, or have an assertion loosened in service of FR-001.
- **FR-003**: The "thesis text is not truncated at the storage layer" guarantee MUST be verified by at least one test that needs no external infrastructure, so the guarantee survives on a Docker-less gate.
- **FR-004**: Time-dependent assertions in `GetInstallmentFxImpactQueryHandlerTests` MUST compute their expectation from the same clock anchor as the handler (`DateTime.UtcNow`), never a literal captured at authoring time.
- **FR-005**: No debug/scratch source files may remain in `backend/`.

### Key Entities

- `DockerRequiredFactAttribute` — an xUnit `FactAttribute` subclass that sets `Skip` during discovery when no Docker endpoint is reachable.
- `InvestmentThesis.ThesisText` — the `varchar(4000)` column at the heart of #443 Bug 1.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `dotnet test backend/FinanceSentry.sln` (no filter) reports 0 failures on a host without Docker.
- **SC-002**: The same command reports 0 failures and 0 Docker-skips on a host with Docker.
- **SC-003**: `ThesisTextColumnLimitTests` retains both of its test methods and their assertions.
- **SC-004**: A test asserting `ThesisText` max length = 4000 passes with no database, no container, and no network.
- **SC-005**: The FX-impact series test passes in any calendar month.

## Assumptions

- GitHub Actions `ubuntu-latest` provides a working Docker daemon, so CI keeps real container coverage; the sandbox and the DevClaw gate do not.
- `xunit.runner.visualstudio` 2.5.4 mishandles runtime `SkipException.ForSkip()` (it surfaces as *Failed*), so the skip must be set at discovery time via the attribute's `Skip` property — the only mechanism the installed runner honours.
