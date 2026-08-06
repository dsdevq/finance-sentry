# Quickstart: Risk Rules

## Local setup

```bash
cd docker && docker compose -f docker-compose.dev.yml up -d postgres api
cd backend && dotnet ef database update --project src/FinanceSentry.Modules.Risk --startup-project src/FinanceSentry.API
```

## Seed the real-world scenario (SC-002)

1. Ensure the test user (`test@gmail.com`, see project `CLAUDE.md`) has a synced brokerage position at ~46% of book (DRAM, per the motivating case).
2. `PUT /risk/rules` (or `save_risk_rules` MCP tool) with `{ "maxPositionWeightPct": 0.25 }`.
3. Trigger `RiskCheckJob.ExecuteForUserAsync(userId)` manually (Hangfire dashboard → Recurring Jobs, or call the scheduler directly in a REPL/test).
4. Assert: one `Alert` with `Type = "PolicyViolation"`, `ReferenceLabel` naming the rule + ticker, message citing observed weight (~46%), limit (25%), and excess in USD.
5. `POST /risk/violations/{id}/acknowledge` with a remediation note (e.g. "trim DRAM on strength to ≤30% by Q4") and a `worseningStepPct`.
6. Re-run the job (step 3) — assert no new alert; `GET /risk/compliance` reports the violation with `Status = Acknowledged` and the note.
7. Increase the position further past the worsening step — re-run the job — assert the violation reopens (`Status = Worsened`) and a fresh alert fires.

## Verify the promotion gate (SC-003, manual until 019 ships)

```bash
curl -X POST http://localhost:5001/api/v1/risk/compliance/check \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{ "ticker": "NEWCO", "proposedUsd": 5000 }'
```

Expect `Refused` with the rule (`MaxPositionWeight` or `MinCashBuffer`), the observed/limit values, and `maxCompliantSizeUsd`. A compliant proposal returns `Allowed` with headroom facts.

## MCP tool smoke test

Once `FinanceSentry.Mcp` is running (see `backend/src/FinanceSentry.Mcp/Program.cs`), verify via any MCP client:

- `get_risk_rules` → current rule set (or `null`/setup-nudge if none saved)
- `save_risk_rules` → appends a version
- `check_risk_rules` (no args) → compliance report; `check_risk_rules(ticker, amount)` → verdict

## Contract test checklist before merge

- `dotnet test backend/tests/FinanceSentry.Mcp.Tests --filter ToolNameContractTests` — must show 30 tools (27 existing + 3 new), or the PR is incomplete.
- `dotnet test backend/tests/FinanceSentry.Tests --filter FullyQualifiedName~Risk` — all pure-function and contract tests green.
- `dotnet build backend/` — zero warnings.
