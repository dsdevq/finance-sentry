# Quickstart: Verifying the IPS ↔ Risk Boundary Cleanup

Backend-only cleanup. Success = single home per concept **and** zero behavioural drift where copies agreed.

## Prerequisites
- Full stack up: `cd docker && docker compose -f docker-compose.dev.yml up -d --build`
- Health: `GET http://localhost:5001/api/v1/health` → `{"status":"healthy"}`

## 1. Capture baseline BEFORE the change (golden master)
On current `main` (pre-migration), for the test user:
- Risk compliance report — snapshot `PolicyViolation`s (esp. `AllocationDrift`, `MaxPositionWeight`).
- Opportunity candidate scores — snapshot `IpsFitFacts` + final scores.
- Research `get_allocation_vs_target` — snapshot drift DTO.
- **VPS live values** (production data lives on VPS, not local): `investment_policy_statements.MaxSinglePositionPct` / `AllocationTargets`; `risk_rule_sets.MaxPositionWeightPct` / `allocation_targets_json`. Record which reconciliation branch will fire.

## 2. Apply migrations (order-independent)
Each migration reconciles the concept whose column it drops, writing the survivor into the other schema's retained column — so either order is safe:
```
dotnet ef database update --context RiskDbContext        # M002: reconcile allocation→IPS, drop allocation_targets_json
dotnet ef database update --context ResearchDbContext    # M012: reconcile cap→Risk, drop MaxSinglePositionPct
```
(The app's normal migration-on-startup path is fine regardless of which context applies first.)

## 3. Verify single home (SC-001, US1)
- `get_ips` response has **no** `maxSinglePositionPct`; still has `allocationTargets`.
- `get_risk_rules` / `GET /risk/rules` response has **no** `allocationTargets`; still has `maxPositionWeightPct`.
- DB: `investment_policy_statements.MaxSinglePositionPct` column gone; `risk_rule_sets.allocation_targets_json` column gone.

## 4. Verify zero drift (SC-002, US2)
Re-run step 1's captures post-migration:
- Risk `AllocationDrift` + `MaxPositionWeight` violations **byte-for-byte identical** where copies agreed.
- Candidate scores identical where the cap agreed.
- Any difference must correspond to a **disagreeing** copy documented in step 1 (intended correction, not regression).

## 5. Verify migration integrity (SC-003/SC-004, US3)
- Values preserved per reconciliation rule (stricter cap wins; IPS allocation wins; populated side survives; nothing fabricated).
- **Idempotency**: re-run migrations → zero further writes. **Order-independence**: apply the two contexts in either order → identical result (verified in T017).
- Discarded values present in the migration/audit log.

## 6. Verify contracts (SC-005/SC-006, US4)
- `PUT /risk/rules` with `allocationTargets` in the body → value not persisted.
- Contract test for `/risk/rules` green.
- Change record calls out moved MCP fields + new homes for the agent-config owner (FR-014).
- Backend `<Version>` bumped + tag created.

## Gates
- `dotnet build backend/` — zero warnings.
- `dotnet test` — reconciliation unit tests, characterization tests, `/risk/rules` contract test all green.
