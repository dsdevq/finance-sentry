# Quickstart: Opportunity Scanner

## Run

```bash
cd docker && docker compose -f docker-compose.dev.yml up -d postgres api
# health: GET http://localhost:5001/api/v1/health
```

## Migration

`M006_OpportunityCandidates` (research schema: `opportunity_candidates`, `candidate_scores`) applies on
startup. Manual:
```bash
dotnet ef database update --project backend/src/FinanceSentry.Modules.Research --context ResearchDbContext
```
Verify: `\dt research.opportunity_candidates research.candidate_scores`.

## Demo (SC-002 — conviction amplification)

Via MCP (Ledger / inspector):
```
score_candidate  MSFT            # full scorecard: structure/fundamentals/crowding/IPS-fit + evidence, NO composite
list_candidates  status=Active
promote_candidate <id>           # runs 022 risk gate; if Allowed, creates a monitored thesis with prefilled triggers
reject_candidate  <id>  "prefer semis exposure"   # kept for counterfactuals
```

## Verify

- **US1**: `score_candidate("MSFT")` → persisted candidate + score with every sub-score citing inputs;
  no-EDGAR ticker → fundamentals `null` (partial, labeled); re-score appends (no duplicate candidate).
- **US3 promote**: promote with an oversized implied position → `Refused` naming the rule + max size;
  compliant promote → `InvestmentThesis` created, visible to `run_thesis_monitor` (017), `Promoted`
  event recorded (020). Reject → status `Rejected`, still in `list_candidates`.
- **Expiry**: `CandidateExpiryJob` expires Active candidates past TTL with a final score + `Expired` event.

## Gate + boundaries

- Promotion calls `IRiskPolicyGate` (022) — verify a Refused verdict blocks thesis creation unless overridden.
- Grep the Research module: no LLM/messaging in the scoring path (FR-002/FR-014).

## Build gate

```bash
dotnet build backend/     # 0 warnings
dotnet test backend/tests/FinanceSentry.Modules.Research.Tests --filter FullyQualifiedName~Opportunity
```
