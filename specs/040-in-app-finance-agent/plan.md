# Implementation Plan: In-app finance agent (Ledger in FS)

**Branch**: `040-in-app-finance-agent` | **Date**: 2026-08-08 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/040-in-app-finance-agent/spec.md`

## Summary

Bring Ledger into Finance Sentry. **US1 (persona-as-code) is shipped** — the persona lives at `agent/ledger/` (core + OpenClaw/browser adapters). This plan covers **US2 (the in-browser interactive agent)** and **US3 (parity/coexistence)**.

**Technical approach (the deferred runtime decision, now made):** a **server-side Claude tool-use loop inside a new `FinanceSentry.Modules.Agent` module**. The loop composes the system prompt from `agent/ledger/persona.core.md + adapters/browser.md + user.md`, calls the Anthropic Messages API (Claude) with the **existing MCP tool surface bridged to Anthropic tool-use format**, dispatches each `tool_use` to the same handlers the MCP tools already wrap (so the agent reasons over the exact 57-tool surface, in the logged-in user's scope), and **streams** text + tool-progress to a new Angular chat module over SSE. Conversations persist in a new `agent` schema. The OpenClaw Ledger is untouched — both runtimes consume the same persona core.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (backend), TypeScript 5.x strict / Angular 21.2 (frontend)
**Primary Dependencies**: ASP.NET Core, EF Core 10 (Npgsql), `FinanceSentry.Core.Cqrs` (hand-rolled ICommand/IQuery — no MediatR), `ModelContextProtocol` (existing `FinanceSentry.Mcp` — source of the bridged tool catalog), `System.Net.Http` via `IHttpClientFactory` + `System.Text.Json` (Anthropic Messages API — streaming, no heavy SDK, consistent with FS's plain-REST integrations). Frontend: `@ngrx/signals`, `@dsdevq-common/ui`, native `EventSource`/fetch-stream for SSE.
**Storage**: PostgreSQL 14 — **new `AgentDbContext`** (schema `agent`, history table `__ef_migrations_history_agent`), migration `M001_InitialSchema` adding `agent_conversations` + `agent_messages`. No changes to existing module schemas.
**Testing**: xUnit (backend — tool-bridge conversion, dispatch/user-scoping, guardrail, persona composition, a contract test for the chat endpoint), Vitest (frontend — `AgentChatStore` + SSE effect), Playwright golden-path.
**Target Platform**: Linux server (Docker), browser SPA.
**Project Type**: Web application (backend module + frontend module).
**Performance Goals**: first visible token/progress < ~2 s after send (SC-007 — never appears frozen); tool-progress surfaced as it happens.
**Constraints**: agent runs strictly in the authenticated user's data scope; tier-3 guardrail (no money/trade/credential execution); Anthropic API key server-side only (never reaches the client); fetched content treated as data, not instructions.
**Scale/Scope**: single primary user (Denys) today; design for per-user isolation. ~57 bridged tools; interactive chat (not high-QPS).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Modular Monolith** — ✅ New `FinanceSentry.Modules.Agent` self-registers via `IModuleRegistrar`; no manual `Program.cs` wiring. The Anthropic call sits behind a domain interface (`ILlmClient`), matching the "external integration behind an interface" rule. The agent reuses other modules' tools **via the MCP tool registry**, not by referencing their internals.
- **II. Code Quality** — ✅ Zero `dotnet build` warnings; every Angular `.ts` passes `npx eslint`. Enforced per file.
- **IV. AI-Driven Analytics** — ✅ Directly advances the AI-insights principle. **LLM provider = Anthropic Claude** (`claude-sonnet-5` default for interactive latency; Opus 4.8 escalation). The constitution's Tech-Stack-Minimums name "OpenAI API or compatible; documented prompts" — Anthropic is the selected compatible provider and **the prompt is documented as `agent/ledger/*` (persona-as-code)**. Recorded in Complexity Tracking as an intentional provider choice.
- **V. Security-First** — ✅ Anthropic API key in `.env.sops` (server-only, never client-exposed). Endpoint behind `JwtAuthenticationMiddleware`; tools resolve the caller's user id — no cross-user access. Conversation rows are financial context → live in the `agent` schema and inherit retention/backup (024). Tier-3 guardrail: no money/trade/credential tool exists in the surface, and the persona enforces draft-and-escalate.
- **VI. Frontend State & Composition** — ✅ New `modules/agent/` chat state in a `signalStore()` with the mandated 5-file split; components declarative + `OnPush`; **no `setInterval`** (SSE consumed in an `rxMethod`); error codes added to the registry; chat UI primitives built in `@dsdevq-common/ui` (`cmn-*`) first; cross-module types in `shared/`.

**Result: PASS** (one documented provider choice, no violations).

## Project Structure

### Documentation (this feature)

```text
specs/040-in-app-finance-agent/
├── spec.md
├── plan.md              # this file
├── research.md          # Phase 0 — decisions (runtime, model, tool-bridge, streaming, persona load, persistence)
├── data-model.md        # Phase 1 — agent_conversations, agent_messages
├── quickstart.md        # Phase 1 — run + verify the browser Ledger locally
├── contracts/
│   ├── chat-endpoint.md #   POST /api/v1/agent/chat (SSE) contract
│   └── tool-bridge.md   #   MCP-tool → Anthropic-tool conversion + dispatch contract
└── tasks.md             # Phase 2 (/speckit.tasks — not created here)
```

### Source Code (repository root)

```text
agent/ledger/                                   # US1 (shipped) — persona is the system prompt source
├── persona.core.md · user.md · adapters/browser.md   # composed into the browser system prompt

backend/src/FinanceSentry.Modules.Agent/
├── AgentModule.cs                              # IModuleRegistrar/IJobRegistrar; DI for the loop, LLM client, tool bridge
├── Application/
│   ├── Commands/SendAgentMessageCommand.cs     # orchestrates one turn (persist user msg → run loop → stream)
│   ├── Queries/GetConversationQuery.cs · ListConversationsQuery.cs
│   └── Services/
│       ├── AgentConversationService.cs         # the tool-use loop (call LLM, dispatch tool_use, iterate)
│       ├── PersonaComposer.cs                  # reads agent/ledger/* → system prompt (cached)
│       ├── McpToolBridge.cs                    # MCP tool registry → Anthropic tool schema + dispatch
│       └── ILlmClient.cs / AnthropicLlmClient.cs # streaming Messages API via IHttpClientFactory
├── Domain/                                     # Conversation, Message entities + repo interfaces
└── Infrastructure/
    ├── AgentDbContext.cs + Migrations/M001_InitialSchema
    └── Repositories/

backend/src/FinanceSentry.API/
└── Controllers/AgentChatController.cs          # POST /api/v1/agent/chat (SSE), GET conversations — authenticated

frontend/projects/dsdevq-common/ui/src/lib/components/
└── chat/                                       # cmn-chat-message, cmn-chat-input (+ specs + stories)

frontend/src/app/modules/agent/
├── pages/ledger-chat/                          # declarative chat page (OnPush)
├── store/  (agent-chat.state/computed/methods/effects/store.ts + specs)  # SSE in an rxMethod
├── services/agent.service.ts                   # HTTP/SSE only, no state
└── models/ · constants/
```

**Structure Decision**: Web-application layout. New backend module `FinanceSentry.Modules.Agent` (orchestration + LLM client + tool bridge + conversation persistence) exposed by one authenticated SSE controller in `FinanceSentry.API`; new frontend feature `modules/agent/` with chat primitives promoted into `@dsdevq-common/ui`. The persona system prompt is sourced from the shipped `agent/ledger/` files — US1 is the input to US2.

## Complexity Tracking

| Violation / Deviation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| LLM provider = Anthropic Claude (constitution names "OpenAI API or compatible") | Project direction is to build on the latest Claude models; strongest tool-use + long-context fit for a disciplined finance agent | An OpenAI-compatible endpoint would work but abandons the Claude alignment and the existing Claude-centric tooling; "compatible" in the constitution admits Anthropic, and prompts are documented as persona-as-code |
| New `agent` schema (conversations/messages) | FR-013 (retain context within a session) + reload-survivable chat; matches per-module DbContext convention | Client-held/stateless history loses context on reload and can't support future proactive/audit needs; a shared table in another module's schema breaks module isolation (Principle I) |
