# Tasks: Close the #443 verify gate

**Spec**: [spec.md](./spec.md) · **Plan**: [plan.md](./plan.md)

## Phase 1 — [US1] Verify gate runs green without Docker (P1)

- [x] T001 [US1] Add `DockerRequiredFactAttribute` in `backend/tests/FinanceSentry.Tests.Integration/Shared/DockerRequiredFactAttribute.cs` — a `FactAttribute` subclass that sets `Skip` at discovery time when no Docker endpoint is reachable (`DOCKER_HOST`, `/var/run/docker.sock`, `$XDG_RUNTIME_DIR/docker.sock`). Probe result cached once per process (FR-001).
- [x] T002 [US1] Apply `[DockerRequiredFact]` to both tests in `ThesisTextColumnLimitTests`. Both test bodies and all assertions unchanged (FR-002, SC-003). *Outcome: `IAsyncLifetime.InitializeAsync` turned out not to run for a fully-skipped class, so the planned lazy-startup refactor was unnecessary and was dropped rather than added speculatively — verified by the run in T006 reporting 2 SKIP and no Docker exception.*
- [x] T003 [US1] Add infra-free `ThesisText` column-limit coverage in `backend/tests/FinanceSentry.Modules.Research.Tests/` — assert the EF model's max length for `InvestmentThesis.ThesisText` is 4000 and that the property is required, so #443 Bug 1 keeps regression coverage on a Docker-less gate (FR-003, SC-004).
- [x] T004 [US1] Fix the month-count drift in `GetInstallmentFxImpactQueryHandlerTests.Handle_TotalsAcrossPlans_AndBuildsAMonthlySeries`: derive the expected point count from `DateTime.UtcNow` and the earliest baseline, and assert the series ends on the current month (FR-004, SC-005).
- [x] T005 [US1] Delete the stray `backend/Program.cs` debug scratch file committed by an earlier increment (FR-005).
- [x] T006 [US1] Run `dotnet test backend/FinanceSentry.sln --no-build -c Release` with **no** filter; confirm 0 failures and that the only new skips are the two Docker-gated tests (SC-001).
- [x] T007 [US1] Correct the stale `AGENTS.md` lines that claim `--filter Category!=Integration` is "the mandatory filter for CI" — CI has run the unfiltered solution since #509.
