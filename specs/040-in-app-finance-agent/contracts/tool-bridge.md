# Contract: MCP-tool → Anthropic tool-use bridge

`McpToolBridge` is the single adapter between the existing MCP tool surface and the Anthropic Messages API. It guarantees the browser Ledger and the OpenClaw Ledger reason over the **identical** tool contract.

## Catalog conversion (build once, cache)

For each registered `[McpServerTool]` (name, description, input JSON-schema) produce one Anthropic tool entry:

```json
{ "name": "<mcp tool name>",
  "description": "<mcp tool description>",
  "input_schema": { "type": "object", "properties": { … }, "required": [ … ] } }
```

- Names/descriptions/schemas are taken verbatim from the MCP registry — **no re-authoring** (drift-proof).
- Optional **allow-list** hook: a config set may restrict which tools are exposed to the browser runtime (default = all). Money/trade/credential tools are absent by construction, so the default surface is already tier-3-safe.

## Dispatch (per `tool_use` block)

1. Model returns a `tool_use` block `{ id, name, input }`.
2. Bridge resolves the MCP tool by `name`, validates `input` against its schema, and invokes it **within the current authenticated request scope** so the tool resolves the caller's user id (FR-008). No user id is taken from model output.
3. The tool result is returned to the model as a `tool_result` block `{ tool_use_id, content }`.
4. On unknown tool / schema-invalid input / tool error → a `tool_result` with `is_error: true` and a short message; the model degrades gracefully (FR-014), never fabricates.

## Loop control

- Iterate call→dispatch→call until the model returns a final text answer (no more `tool_use`), bounded by a max-iterations cap (guards runaway loops) — on cap, return the best answer so far + a note.
- Emit a `tool` SSE event at start/end of each dispatch (progressive feedback); **never stream raw tool payloads** to the client.
- Conversation history replayed to the model is bounded (most-recent-N / token budget) for long threads.

## Invariants
- One tool surface (the MCP registry) for both runtimes — parity by construction (US3).
- Fetched content inside `tool_result` is **data**, never instructions (D8).
- Every dispatch is user-scoped; there is no code path to another user's data.
