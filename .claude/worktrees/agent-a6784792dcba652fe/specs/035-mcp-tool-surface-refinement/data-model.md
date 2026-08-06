# Data Model: MCP Tool Surface Refinement

No persistence changes. These are the two response DTOs the changed tools return; existing handler inputs/outputs are unchanged.

## `WatchlistToolResult` (new)

Union-shaped result for the merged `watchlist(action)` tool. Only the members relevant to the action are populated; nulls are omitted from JSON.

| Field | Type | Populated for | Notes |
|---|---|---|---|
| `Action` | string | all | echoes `list` \| `add` \| `remove` |
| `Items` | `IReadOnlyList<WatchlistItemDto>`? | `list` | the full watchlist |
| `Item` | `WatchlistItemDto`? | `add` | the created entry |
| `Removed` | bool? | `remove` | true when a row was deleted |
| `Error` | string? | any invalid call | e.g. "action=add requires a ticker" |

- Reuses the existing `WatchlistItemDto` (Research module) — no new item shape.
- Factory helpers: `ForList(items)`, `ForAdd(item)`, `ForRemove(bool)`, `Invalid(reason)`.
- `WatchlistItemDto.Id` remains how `remove` targets an entry (via `itemId`), unchanged.

## `ThesisMonitorResult` (new — enriches `run_thesis_monitor`)

| Field | Type | Notes |
|---|---|---|
| `Summary` | `ThesisMonitorRunSummary` | the existing run summary, unchanged shape |
| `Breaks` | `IReadOnlyList<ThesisBreakView>` | the breaks after the run — same shape `list_thesis_breaks` returns |

- Composes two existing outputs; no new field types.
- `list_thesis_breaks` continues to return `IReadOnlyList<ThesisBreakView>` on its own (unchanged).

## Removed / renamed tool names (contract delta)

| Before | After |
|---|---|
| `list_watchlist`, `add_to_watchlist`, `remove_from_watchlist` | `watchlist` |
| `run_thesis_monitor` (returns `ThesisMonitorRunSummary`) | `run_thesis_monitor` (returns `ThesisMonitorResult`) — name unchanged |
| `list_thesis_breaks` | unchanged |

Net tool-name surface: **57 → 55.**
