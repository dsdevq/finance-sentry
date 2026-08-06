# MCP Tool Contract: `run_thesis_monitor`

Tool type: `[McpServerToolType]` on `RunThesisMonitorTool` in `FinanceSentry.Mcp/Tools/`.
Backs onto `RunThesisMonitorCommand` (Core.Cqrs `ICommand<ThesisMonitorRunSummary>`).

## Purpose

On-demand trigger of the deterministic thesis-break evaluation for the caller's active theses.
Same code path as the scheduled `ThesisMonitorJob`. Persists any break-state changes and raises/
resolves alerts as a side effect. (FR-002)

## Input

| Param | Type | Required | Description |
|---|---|---|---|
| `userId` | Guid? (string) | No | Overrides identity; defaults to `IIdentityResolver.GetUserId()`. |

## Output — `ThesisMonitorRunSummary`

```jsonc
{
  "thesesEvaluated": 2,
  "triggersEvaluated": 5,
  "breaksRaised": 1,
  "breaksCleared": 0,
  "skipped": 1,      // theses with no triggers or all-non-evaluable
  "errors": 0        // ticker/thesis evaluations that threw (run continued)
}
```

## Behaviour contract

- Deterministic given identical fundamentals + price + triggers (SC-001).
- Never marks a thesis broken on missing/insufficient/div-by-zero data (SC-002 / FR-013).
- Exactly one alert per unbroken→broken transition; no alert on a persisting breach (SC-003 / FR-008).
- One failed thesis/ticker does not abort the run; counted in `errors` (FR-014).
- Raises domain alerts only — no channel delivery (FR-010 / SC-007).

## Acceptance (from spec US2.1)

Calling the tool returns the summary and persists break-state changes; a subsequent
`list_thesis_breaks` reflects theses this run marked broken.
