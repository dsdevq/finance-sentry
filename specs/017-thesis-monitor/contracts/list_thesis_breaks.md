# MCP Tool Contract: `list_thesis_breaks`

Tool type: `[McpServerToolType]` on `ListThesisBreaksTool` in `FinanceSentry.Mcp/Tools/`.
Backs onto `ListThesisBreaksQuery` (Core.Cqrs `IQuery<IReadOnlyList<ThesisBreakView>>`).

## Purpose

Read the current set of broken theses for the caller, with full explainability. Read-only —
does not evaluate or mutate state. (FR-015)

## Input

| Param | Type | Required | Description |
|---|---|---|---|
| `userId` | Guid? (string) | No | Overrides identity; defaults to `IIdentityResolver.GetUserId()`. |

## Output — `ThesisBreakView[]`

```jsonc
[
  {
    "thesisId": "9c091f57-521d-441c-95e1-50400ded1966",
    "ticker": "DRAM",
    "metric": "gross_margin",
    "observedValues": [0.31, 0.29],
    "periods": ["2026-Q1", "2025-Q4"],
    "threshold": 0.35,
    "direction": "lessThan",
    "reason": "MU gross_margin 0.31 (2026-Q1), 0.29 (2025-Q4) < 0.35 for 2 quarters"
  }
]
```

Empty array when no theses are broken — never an error (US2.3).

## Behaviour contract

- Every returned item includes at least: breached metric, observed value(s) + period(s),
  threshold, and human-readable reason (SC-005 — 100% explainable).
- User-scoped: only the caller's theses (Principle V).
- Reflects the persisted `BrokenAt`/`BrokenReason` state — no re-evaluation on read.

## Acceptance (from spec US2.2 / US2.3)

Each broken thesis is returned with ticker + breached metric + observed value(s)/period(s) +
threshold + reason; when none are broken an empty list is returned.
