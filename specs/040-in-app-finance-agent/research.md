# Research: In-app finance agent (US2 runtime)

Phase 0 decisions. The spec deferred the runtime; this resolves it.

## D1 — Where the agent loop runs

**Decision**: A **server-side tool-use loop inside a new `FinanceSentry.Modules.Agent` module**, exposed by one authenticated SSE endpoint in `FinanceSentry.API`. The browser is a thin chat client.

**Rationale**: The tools, data, and auth are all already server-side and in-process; running the loop there means the agent calls the same CQRS handlers the MCP tools wrap, in the caller's user scope, with no new trust boundary and no secrets on the client. It's the direct realization of "FS is core, agent is thin."

**Alternatives considered**:
- *Proxy chat to the OpenClaw runtime.* Rejected: keeps the OpenClaw dependency and its model for an interactive surface we want native, and complicates auth/user-scoping across the boundary.
- *Client-side agent loop (browser calls the LLM directly).* Rejected outright: would put the API key in the browser (Principle V violation) and can't reach in-process tools.

## D2 — LLM provider & model

**Decision**: **Anthropic Claude** via the Messages API. Default model **`claude-sonnet-5`** for the interactive agent (fast, strong tool-use, cost-appropriate); **`claude-opus-4-8`** as a config-selectable escalation for hard reasoning. Model id in config, not code.

**Rationale**: Project direction is to build on the latest Claude models; Sonnet 5 balances latency and capability for chat, Opus 4.8 is the deep-reasoning option. The system prompt is the persona-as-code — already documented.

**Alternatives**: OpenAI-compatible endpoint (constitution's literal minimum) — rejected to stay aligned with Claude; the constitution's "or compatible" admits Anthropic.

## D3 — How to call Anthropic (SDK vs HTTP)

**Decision**: A thin typed client (`AnthropicLlmClient : ILlmClient`) over `IHttpClientFactory` + `System.Text.Json`, speaking the Messages API with **streaming** (SSE from Anthropic) and **tool-use**. No heavy third-party SDK.

**Rationale**: Consistent with FS's plain-REST integration convention (Finnhub/FRED/Yahoo are all hand-rolled HttpClient). Keeps control over streaming relay and tool-use loop. `ILlmClient` is the domain interface (Principle I) — swappable and mockable in tests.

**Alternatives**: `Anthropic.SDK` (community) — rejected to avoid a heavy dependency and to keep streaming/tool-use handling explicit and testable; revisit if hand-rolling proves brittle.

## D4 — Tool surface: bridge the existing MCP tools

**Decision**: **Bridge the existing MCP tool registry to Anthropic tool-use format** (`McpToolBridge`): enumerate registered `[McpServerTool]`s, convert each tool's name/description/input JSON-schema to an Anthropic `tools` entry, and dispatch each `tool_use` block by invoking that MCP tool, returning its result as a `tool_result`. One tool surface, one source of truth (~57 tools).

**Rationale**: The MCP tools already wrap the CQRS handlers with schemas and descriptions tuned for an LLM. Re-declaring them for the agent would duplicate and drift. Bridging keeps the browser Ledger and the OpenClaw Ledger on the identical tool contract.

**Scope & guardrail**: The browser agent receives the read/research/analysis tools plus FS-state tools (e.g. `save_ips`, `save_thesis`, `promote_candidate`). **No money-movement/trade/credential tool exists in the surface**, so the tier-3 line cannot be crossed by a tool call; the persona additionally enforces draft-and-escalate. FS-state writes (IPS/thesis/candidate) are internal and require in-conversation user confirmation per the persona rituals. (A per-tool allow-list is available if we later want to hard-gate writes; default = expose all, persona-governed.)

**User scoping**: The loop runs inside the authenticated HTTP request; tool dispatch resolves the caller's user id from the request context (same identity path the API uses) — every tool call is his-scoped. No mechanism to target another user (FR-008).

**Alternatives**: A hand-written agent tool catalog mapping to handlers — rejected (duplication/drift). Exposing raw CQRS handlers — rejected (loses the LLM-tuned schemas/descriptions the MCP layer provides).

## D5 — Streaming to the browser

**Decision**: `POST /api/v1/agent/chat` returns **Server-Sent Events**. The server relays Anthropic's stream as typed events: `text` (assistant token deltas), `tool` (a tool call started/finished — for progressive feedback), `error`, and `done` (with the persisted message id). The Angular store consumes the stream in an `rxMethod` and appends deltas to the active assistant-message signal.

**Rationale**: SSE is the simplest one-way streaming fit for chat, works through the YARP gateway's default passthrough, and satisfies SC-007 (never appears frozen) by surfacing tool-progress as it happens. Avoids WebSocket complexity for a one-directional token stream.

**Alternatives**: WebSocket (bidirectional, unnecessary here); non-streaming request/response (fails the "never frozen" criterion for multi-tool answers).

## D6 — Persona as the system prompt

**Decision**: `PersonaComposer` reads `agent/ledger/persona.core.md` + `agent/ledger/adapters/browser.md` + `agent/ledger/user.md` at startup, composes the system prompt, and caches it (invalidated on file change / app restart). US1 is the single source of truth for US2's behavior.

**Rationale**: Closes the loop — the versioned persona *is* the runtime prompt for the browser agent. Editing the core changes both runtimes (the parity guarantee, US3).

**Alternatives**: Hard-coding the prompt in C# — rejected (defeats persona-as-code). DB-stored prompt — rejected (the repo is the source of truth; ship it with the app).

## D7 — Conversation persistence

**Decision**: New `AgentDbContext` (schema `agent`), tables `agent_conversations` (per user) and `agent_messages` (role, content, tool-call/result metadata, timestamps). History is loaded to rebuild context on reconnect and sent to the model per turn.

**Rationale**: Satisfies FR-013 (retain context within a session) and survives page reload; matches the per-module DbContext convention; conversation content is financial context and belongs under retention/backup (024).

**Alternatives**: Stateless/client-held history — rejected (loses context on reload, no audit trail). Redis/session store — unnecessary infra for the scale.

## D8 — Guardrails & injection

**Decision**: (a) The tier-3 guardrail is enforced structurally (no dangerous tool in the surface) **and** by the persona (draft-and-escalate). (b) Content fetched by tools (news/filings/web) is passed to the model as tool-result **data**, never as system/developer instructions; the system prompt states fetched content cannot override guardrails. (c) The endpoint is authenticated; unauthenticated calls are rejected before any model call.

**Rationale**: Defense in depth — the safest state is "the tool that could move money does not exist here," backed by persona discipline and an injection-resistant prompt boundary.

## D9 — Secrets & config

**Decision**: `ANTHROPIC_API_KEY` → `Agent__Anthropic__ApiKey` in `.env.sops` (server-only). Keyless ⇒ the chat endpoint returns a clear "agent not configured" response and the UI shows an unobtrusive disabled state — mirroring the Finnhub/FRED keyless-silent precedent (no crashes).

**Rationale**: Consistent secret handling; graceful degradation when the key isn't set (e.g. local dev without a key).
