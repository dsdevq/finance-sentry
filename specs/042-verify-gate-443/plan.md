# Implementation Plan: Close the #443 verify gate

**Spec**: [spec.md](./spec.md) · **Branch**: `goal/issue-443-fix-lifekit-hq-f941c1`

## Approach

Backend tests only. No production code changes — the two #443 production defects were already fixed; what remains is that the issue's own verify command cannot go green on a Docker-less host.

### Load-bearing decisions

- **Skip via a `FactAttribute` subclass, not a runtime skip.** `xunit.runner.visualstudio` 2.5.4 surfaces `SkipException.ForSkip()` as *Failed*, so a runtime skip would trade one red for another. Setting `Skip` in the attribute constructor happens at discovery time, which the installed runner reports correctly as *Skipped*. This is a new mechanism in this repo (no prior custom `FactAttribute` exists) — the shape is the standard xUnit conditional-fact idiom.
- **Probe the Docker endpoint, don't try/catch the container start.** Catching the Testcontainers exception inside the test and returning early would report a *pass* for a test that verified nothing — a fake green. A discovery-time probe of `DOCKER_HOST` / `/var/run/docker.sock` / `$XDG_RUNTIME_DIR/docker.sock` keeps "skipped" and "passed" honest.
- **Keep Testcontainers rather than reusing CI's `postgres` service.** The service container is only present in `backend-ci.yml`; a local `dotnet test` has neither. Testcontainers already ships as a dependency of this project and gives the tests a self-contained Postgres wherever Docker exists.
- **Add model-metadata coverage alongside, not instead of, the container tests.** The realistic regression vector for #443 Bug 1 is someone shrinking `HasMaxLength(...)` in `ResearchDbContext`. EF's model metadata catches exactly that with zero infrastructure, so the guarantee holds even on gates that skip the container tests. The container tests stay as the deeper storage-layer proof.
- **Derive the FX series count from `DateTime.UtcNow`.** The handler ends the series at `DateOnly.FromDateTime(DateTime.UtcNow)`; the test must anchor to the same clock or it re-breaks every month. Asserting the computed span is a *stronger* claim than the old literal, not a weaker one.

## Story slices

### [US1] Verify gate runs green without Docker

**Touches**:

- `backend/tests/FinanceSentry.Tests.Integration/Shared/DockerRequiredFactAttribute.cs` (new) — discovery-time Docker probe.
- `backend/tests/FinanceSentry.Tests.Integration/Research/ThesisTextColumnLimitTests.cs` — swap `[Fact]` → `[DockerRequiredFact]`; test bodies unchanged.
- `backend/tests/FinanceSentry.Modules.Research.Tests/` — new infra-free assertions on the `ThesisText` max length (FR-003 / SC-004).
- `backend/tests/FinanceSentry.Tests.Unit/Subscriptions/GetInstallmentFxImpactQueryHandlerTests.cs` — dynamic month-count expectation (FR-004 / SC-005).
- `backend/Program.cs` (delete) — scratch file committed by accident in an earlier increment (FR-005).

**Constraints discovered**:

- xUnit does **not** construct the test class or run `IAsyncLifetime.InitializeAsync` when every fact in it carries a discovery-time `Skip`. Verified empirically: the unfiltered run reports both tests as `[SKIP]` with no Testcontainers exception. No lazy-startup refactor was needed.
- `ResearchDbContext` lives in `FinanceSentry.Modules.Research`; `FinanceSentry.Modules.Research.Tests` already references it, so the infra-free test needs no new project reference.
- CI (`backend-ci.yml`) runs the solution with **no** category filter, so `[Trait("Category","Integration")]` alone does not keep a Docker-dependent test out of the gate. Keep the trait (AGENTS.md calls it the convention) but do not rely on it.
