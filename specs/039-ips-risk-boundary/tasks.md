---
description: "Task list for IPS ↔ Risk Rules Boundary Cleanup"
---

# Tasks: IPS ↔ Risk Rules Boundary Cleanup

**Input**: Design documents from `/specs/039-ips-risk-boundary/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: MANDATORY per constitution — reconciliation **unit tests** (US3), **characterization tests** for zero-drift (US2, SC-002), **REST contract test** for the changed `/risk/rules` (US4), **integration test** for the cross-module ports (Foundational).

**⚠️ Atomicity note**: This is a structural cleanup, not incremental feature work. You cannot half-remove a duplicated field — **Phase 5 (US1 entity removal) + Phase 6 (US4 contract removal) are one compile unit** and ship together; the build is red between the entity edit and its matching contract edit. Per FR-013 the whole feature (US1–US4) ships as **one PR on one branch** (`039-ips-risk-boundary`). The phase split below is for review/traceability and dependency-correct sequencing, not separate deliverables.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1–US4 from spec.md

## Path Conventions

Backend modular monolith. Modules under `backend/src/FinanceSentry.Modules.*`; host at `backend/src/FinanceSentry.API`; MCP at `backend/src/FinanceSentry.Mcp`; tests under `backend/tests/*`.

---

## Phase 1: Setup & Baseline Capture

**Purpose**: Capture current behaviour before any code changes — the golden master that proves zero drift (SC-002).

- [x] T001 [P] Capture golden-master baselines for the test user against current `main`: Risk compliance `PolicyViolation`s (esp. `AllocationDrift` + `MaxPositionWeight`), opportunity candidate scores + `IpsFitFacts`, and the Research `get_allocation_vs_target` drift DTO. Persist as fixtures in `backend/tests/FinanceSentry.Modules.Risk.Tests/Fixtures/` and `backend/tests/FinanceSentry.Modules.Research.Tests/Fixtures/`.
- [ ] T002 [P] Query VPS production Postgres (production data lives on VPS, not local) for the single user's `research.investment_policy_statements.MaxSinglePositionPct` + `AllocationTargets` and `risk.risk_rule_sets.MaxPositionWeightPct` + `allocation_targets_json`. Record which reconciliation branch fires and any expected live behaviour change; add to the PR/changelog draft (research.md R7).

**Checkpoint**: Baseline captured; safe to change code.

---

## Phase 2: Foundational — Cross-Module Read Ports (BLOCKS repointing)

**Purpose**: The read ports + host adapters that let a reader consume the single home. Additive — `dotnet build backend/` stays green throughout.

**⚠️ Adapters live in `FinanceSentry.API` (host), NOT in module Infrastructure** — Research and Risk don't reference each other; an in-module adapter would create a cyclic assembly reference that won't compile.

- [x] T003 [P] Create `IAllocationPolicySource` port + `AllocationDriftTarget` record (fraction `TargetPct`/`DriftBandPct`) in `backend/src/FinanceSentry.Modules.Risk/Domain/Ports/IAllocationPolicySource.cs`.
- [x] T004 [P] Create `IPositionCapSource` port (`Task<decimal?> GetMaxPositionWeightAsync`) in `backend/src/FinanceSentry.Modules.Research/Domain/Ports/IPositionCapSource.cs`.
- [x] T005 Implement `IpsAllocationPolicySource` adapter in `backend/src/FinanceSentry.API/Integration/IpsAllocationPolicySource.cs` — delegates to Research `IQueryHandler<GetIpsQuery, IpsDto?>` and applies the R4 translation (IPS whole-% + Min/Max/rule → fraction target + symmetric band). (depends T003)
- [x] T006 Implement `RiskPositionCapSource` adapter in `backend/src/FinanceSentry.API/Integration/RiskPositionCapSource.cs` — delegates to Risk `IQueryHandler<GetRiskRuleSetQuery, RiskRuleSetDto?>`, returns `dto?.MaxPositionWeightPct`. (depends T004)
- [x] T007 Add `AddCrossModulePorts()` DI extension in `backend/src/FinanceSentry.API/Integration/CrossModulePortRegistration.cs` and invoke it in `backend/src/FinanceSentry.API/Program.cs`. (depends T005, T006)
- [x] T008 [P] Integration test: each adapter returns correctly translated values against seeded IPS/Risk data (incl. band recovery from Min/Max and from `RebalancingRule`) in `backend/tests/FinanceSentry.Tests.Integration/CrossModulePorts/CrossModulePortTests.cs`. (depends T007)

**Checkpoint**: Ports resolvable via DI; build green; nothing repointed yet.

---

## Phase 3: User Story 2 — Nothing behaves differently (Priority: P1)

**Goal**: Repoint the two moving readers to the single home with **byte-for-byte identical** verdicts/scores where the copies agree (SC-002).

**Independent Test**: Seed a portfolio + policy where the two copies agree; capture drift verdicts, cap enforcement, and candidate scores before; run after repoint; confirm identical.

**Why first among P1**: Repointing is done while the duplicate fields still exist — an isolatable, reversible step whose correctness is provable *before* any schema change. This shrinks behavioural risk.

- [x] T009 [P] [US2] Characterization test for `RiskEvaluationService` — drift (`AllocationDrift`) + cap (`MaxPositionWeight`) `PolicyViolation`s byte-for-byte vs T001 baseline on an agree-case seed, in `backend/tests/FinanceSentry.Modules.Risk.Tests/RiskEvaluationServiceCharacterizationTests.cs`. Written to pass on **current** behaviour first. **Include absent/default cases (FR-007)**: cap `null` → no `MaxPositionWeight` violation; no allocation targets → no `AllocationDrift` violation — same as before the repoint.
- [x] T010 [P] [US2] Characterization test for `ScoreCandidateCommand.BuildIpsFit` — `IpsFitFacts` (`withinConcentration`, surfaced cap) + final score byte-for-byte vs T001 baseline on an agree-case seed, in `backend/tests/FinanceSentry.Modules.Research.Tests/ScoreCandidateCharacterizationTests.cs`. Written to pass on **current** behaviour first. **Include absent/default cases (FR-007)**: cap `null` → `withinConcentration = true`; no IPS on file → unchanged permissive `IpsFitFacts`.
- [x] T011 [US2] Repoint `RiskEvaluationService.ComputeRawViolations` allocation-drift block (L218–240) to read `IAllocationPolicySource` (inject via ctor) instead of `ruleSet.AllocationTargets`; preserve the exact `|actual − target| > driftBand` comparison and emitted violation fields. In `backend/src/FinanceSentry.Modules.Risk/Application/Services/RiskEvaluationService.cs`. (depends T003, T007, T009)
- [x] T012 [US2] Repoint `ScoreCandidateCommand.BuildIpsFit` (L153–174) to read the cap from `IPositionCapSource` instead of `ips.MaxSinglePositionPct`; make `BuildIpsFit` async, thread the port, keep `withinConcentration = currentWeight is null || cap is null || currentWeight <= cap`. In `backend/src/FinanceSentry.Modules.Research/Application/Commands/ScoreCandidateCommand.cs`. (depends T004, T007, T010)
- [x] T013 [US2] Run T009 + T010 green after repoint; confirm zero drift on the agree-case; `dotnet build backend/` zero warnings. (depends T011, T012)

**Checkpoint**: Both moving readers consume the single home; duplicate fields still present; behaviour identical where copies agree.

---

## Phase 4: User Story 3 — No policy value is lost or reset (Priority: P1)

**Goal**: Reconcile existing values into the single home per a documented, deterministic, idempotent rule before dropping duplicate columns (FR-008–FR-012).

**Independent Test**: Seed the two records with matching / differing / one-empty / both-empty values; run the migration; confirm the survivor matches the rule and nothing is fabricated; re-run → no change.

- [ ] T014 [P] [US3] Reconciliation unit tests — matrix: (a) matching, (b) differing cap → stricter (lower) wins, (c) differing allocation → IPS wins, (d) one-side-empty → populated survives, (e) both-empty → unset, (f) unit-ambiguous IPS cap normalized (`>1` ⇒ ÷100) before compare, (g) re-run → zero further change (idempotent), (h) discarded value logged. In `backend/tests/FinanceSentry.Modules.Risk.Tests/ReconciliationTests.cs`.
- [x] T015 [US3] Author Risk migration **M002** (`…_ReconcileAndDropAllocation`) in `backend/src/FinanceSentry.Modules.Risk/Migrations/`. **Reconciles the ALLOCATION concept — the one this migration's column drop concerns.** Cross-schema `Up()` (both schemas share one DB): (1) reconcile allocation → `research.investment_policy_statements.AllocationTargets` (IPS-wins when present; else copy Risk→IPS reversibly via `Min/Max = (target ± band)·100`); (2) log discards (FR-011); (3) then drop `risk.risk_rule_sets.allocation_targets_json`. `Down()` re-adds the column nullable. **Self-contained + order-independent**: reads only the column it drops, writes only to the *retained* IPS column — no dependency on M012's apply order. All writes guarded for idempotency. (depends T014)
- [x] T016 [US3] Author Research migration **M012** (`…_ReconcileAndDropIpsPositionCap`) in `backend/src/FinanceSentry.Modules.Research/Migrations/`. **Reconciles the POSITION-CAP concept — the one this migration's column drop concerns.** Cross-schema `Up()`: (1) read `research.investment_policy_statements.MaxSinglePositionPct`, normalize unit (`>1` ⇒ ÷100), apply stricter-wins vs `risk.risk_rule_sets.MaxPositionWeightPct`, write survivor to `risk...MaxPositionWeightPct` only if different; (2) log discards; (3) then drop `research...MaxSinglePositionPct`. `Down()` re-adds nullable. **Self-contained + order-independent**: reads only the column it drops, writes only to the *retained* Risk column — no cross-context ordering requirement with M002. Idempotency-guarded. (depends T014)
- [x] T017 [US3] Run T014 green; apply migrations to a scratch DB in **both orders** (M002→M012 and M012→M002) to prove order-independence; verify identical reconciliation outcomes and idempotency (second `database update` → zero writes). (depends T015, T016)

**Checkpoint**: Data consolidated into single homes, lossless, idempotent, and **order-independent** across the two contexts; duplicate columns still referenced by entity code (removed next).

---

## Phase 5: User Story 1 — One home per concept (Priority: P1)

**Goal**: Remove the duplicate fields from the entities/persistence so each concept has exactly one stored home (SC-001, FR-001–FR-003).

**Independent Test**: Read the IPS record → has allocation, no cap. Read the Risk record → has cap, no allocation copy. No supported path writes a second copy.

**⚠️ Atomic with Phase 6**: removing an entity field breaks compilation until its matching command/DTO/tool field is also removed. Do Phase 5 + Phase 6 together; final build verification is T027.

- [x] T018 [US1] Remove `MaxSinglePositionPct` from `InvestmentPolicyStatement` entity and its `ResearchDbContext` mapping (L161) in `backend/src/FinanceSentry.Modules.Research/`. Update `ResearchDbContextModelSnapshot` to match M012's end-state (column absent). (depends T012, T016)
- [x] T019 [US1] Remove `AllocationTargets` (and the now-unused `AllocationTargetEntry` record) from `RiskRuleSet` entity and its `RiskDbContext` mapping (`allocation_targets_json` + `ValueComparer`) in `backend/src/FinanceSentry.Modules.Risk/`. Update `RiskDbContextModelSnapshot` to match M002's end-state. (depends T011, T015)
- [x] T020 [US1] Grep-verify no residual reads/writes of the removed entity fields anywhere except the migrations (`MaxSinglePositionPct`, `RiskRuleSet.AllocationTargets`). **Snapshot consistency (N1)**: run `dotnet ef migrations has-pending-model-changes` for both `RiskDbContext` and `ResearchDbContext` — must report **no pending changes** (hand-authored M002/M012 fully match the post-removal model; EF will not want to emit an extra drop migration). (depends T018, T019)

**Checkpoint**: Each concept stored once (build completes only after Phase 6).

---

## Phase 6: User Story 4 — Contracts reflect the single home (Priority: P2)

**Goal**: Drop the moved fields from the write/read contracts so the agent can't write to the wrong home (FR-013), and flag the change for the agent-config owner (FR-014).

**Independent Test**: `save_ips`/`get_ips` no longer carry the cap; `save_risk_rules`/`get_risk_rules`/`PUT /risk/rules` no longer carry allocation; posting a moved field under its old contract has no effect.

- [x] T021 [P] [US4] Remove `MaxSinglePositionPct` from `SaveIpsCommand` (param + handler mapping L49 + DTO build L66), `GetIpsQuery` projection, and `IpsDto` (L21) in `backend/src/FinanceSentry.Modules.Research/`. (compile-paired with T018)
- [x] T022 [P] [US4] Remove `AllocationTargets` from `SaveRiskRuleSetCommand` (param + per-target validation L57–70 + mapping), `GetRiskRuleSetQuery`/`RiskRuleSetDto` (L16), and REST `SaveRiskRulesRequest` (L117 in `RiskController.cs`) in `backend/src/FinanceSentry.Modules.Risk/`. (compile-paired with T019)
- [x] T023 [P] [US4] Remove `maxSinglePositionPct` param from `SaveIpsTool` and `allocationTargets` param from `SaveRiskRulesTool` in `backend/src/FinanceSentry.Mcp/Tools/`.
- [x] T024 [US4] Contract test for `PUT`/`GET /risk/rules` in `backend/tests/FinanceSentry.Tests.Integration/` (or `FinanceSentry.Mcp.Tests` for the MCP surface): `GET` response has no `allocationTargets`; `PUT` including it does not persist it; retained caps round-trip unchanged (SC-005). (depends T022)
- [x] T025 [US4] Bump backend `<Version>` in `backend/src/FinanceSentry.API/FinanceSentry.API.csproj` and prepare the git tag; release notes record the field removals and that **no live SPA consumer exists** (grep-verified). (depends T022)
- [x] T026 [US4] Change record for the agent-config (Ledger persona) owner (FR-014): `save_ips.maxSinglePositionPct` → `save_risk_rules.maxPositionWeightPct`; `save_risk_rules.allocationTargets` → `save_ips.allocationTargets`. Add to PR body / `.specify/` change log (agent-side prompt update is out of scope — performed on OpenClaw).
- [x] T027 [US4] `dotnet build backend/` — zero warnings after the full Phase 5 + Phase 6 field removal (atomic compile unit). (depends T018, T019, T020, T021, T022, T023)

**Checkpoint**: Contracts expose each moved field under exactly one home; full solution compiles clean.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T028 [P] Run full backend suite `dotnet test backend/` — reconciliation, characterization, contract, integration all green (>80% on new logic).
- [ ] T029 Run `quickstart.md` end-to-end: apply migrations M002→M012, verify single home (SC-001), zero drift on agree-case (SC-002), migration integrity + idempotency (SC-003/SC-004), contract behaviour (SC-005/SC-006).
- [x] T030 [P] `/csharp-quality` sweep across all changed `.cs` files; resolve every IDE/analyzer warning (constitution II).
- [x] T031 Update `CLAUDE.md` current-state note: 039 shipped — IPS = sole home of target allocation (intent); Risk = sole home of single-position cap (enforced); cross-module read ports in `FinanceSentry.API/Integration/`.

---

## Dependencies & Execution Order

### Phase order (dependency-driven, not strict priority — all of US1–US3 are P1 and inseparable)

1. **Phase 1 Setup** — no deps.
2. **Phase 2 Foundational (ports)** — blocks all repointing.
3. **Phase 3 US2 (repoint + characterize)** — depends on ports; done while fields still exist.
4. **Phase 4 US3 (migration)** — depends on US2 repoint (readers must consume single home before columns move/drop).
5. **Phase 5 US1 (entity removal)** + **Phase 6 US4 (contract removal)** — **one atomic compile unit**; depend on US2 + US3.
6. **Phase 7 Polish** — depends on everything.

### Key edges
- T005←T003, T006←T004, T007←T005,T006, T008←T007
- T011←T003,T007,T009 · T012←T004,T007,T010 · T013←T011,T012
- T015←T014 · T016←T014 (M002 and M012 are independent — each reconciles the concept it drops) · T017←T015,T016
- T018←T012,T016 · T019←T011,T015 · T020←T018,T019
- T021↔T018, T022↔T019 (compile-paired) · T024←T022 · T027←T018–T023

### Parallel opportunities
- T001, T002 together.
- T003, T004 together; then T005, T006 together.
- T009, T010 together (characterization, different projects).
- T021, T022, T023 together (different files) — but the build only greens once all + T018/T019 land.

---

## Implementation Strategy

**This feature is a single atomic MVP.** US1+US2+US3 cannot be shipped independently (you can't remove a duplicated field without repointing readers and migrating data), and US4 must ship with them (FR-013). Deliver the whole thing on the one branch:

1. Phase 1 → capture baseline (the safety net).
2. Phase 2 → ports (additive, green).
3. Phase 3 → repoint + prove zero drift (green, reversible).
4. Phase 4 → migration (reconcile-then-drop, idempotent).
5. Phase 5 + Phase 6 → atomic field + contract removal (build red mid-way, green at T027).
6. Phase 7 → validate, sweep, version, tag, changelog.
7. Single PR; squash-merge on green; watch VPS deploy.

---

## Notes

- [P] = different files, no incomplete-task dependency.
- Zero-drift (SC-002) is scoped to the **agree** case; where copies disagree today, consolidation to the single source is the intended correction — document it (T002) rather than hide it.
- Never loosen a safety limit in reconciliation: stricter (lower) cap wins; IPS allocation wins (research.md R3/R4).
- Backend build gate + version/tag are constitution hard gates — T025, T027 are not optional.
