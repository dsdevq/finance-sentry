# Tasks: In-app finance agent (Ledger in FS)

**Input**: Design documents from `/specs/040-in-app-finance-agent/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Contract test for the chat endpoint (mandatory), unit tests for tool-bridge / persona-composer / loop / keyless / user-scoping (mandatory), Vitest for the store, Playwright golden path.

**Note**: **US1 (persona-as-code) is already shipped** (PR #388, `agent/ledger/`). These tasks cover **US2 (browser agent)** and **US3 (parity/coexistence)**.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup (shared infrastructure)

- [x] T001 Create `backend/src/FinanceSentry.Modules.Agent/FinanceSentry.Modules.Agent.csproj` (net10.0, references `FinanceSentry.Core`, `FinanceSentry.Mcp`; `FrameworkReference Microsoft.AspNetCore.App`) and add it to `backend/FinanceSentry.sln`.
- [x] T002 Reference `FinanceSentry.Modules.Agent` from `FinanceSentry.API` (`FinanceSentry.API.csproj`).
- [x] T003 [P] Add `AgentOptions` (`Application/Services/AgentOptions.cs`) bound from config `Agent:*` (Anthropic ApiKey, ModelId default `claude-sonnet-5`, MaxToolIterations, HistoryTurnBudget). Add `ANTHROPIC_API_KEY` → `Agent__Anthropic__ApiKey` to `docker/.env.example` and both compose files (env passthrough).
- [x] T004 [P] Add `agent_not_configured`, `llm_unavailable`, `conversation_not_found` to `frontend/src/app/core/errors/error-messages.registry.ts`.

## Phase 2: Foundational (blocking prerequisites for US2)

- [x] T005 Create `AgentDbContext` (schema `agent`, history table `__ef_migrations_history_agent`) in `Infrastructure/AgentDbContext.cs` with `Conversation` + `Message` entities (`Domain/Conversation.cs`, `Domain/Message.cs`) per data-model.md.
- [x] T006 Generate EF migration `agent/M001_InitialSchema` (`agent_conversations`, `agent_messages` + indexes `(UserId,UpdatedAt desc)`, `(ConversationId,CreatedAt)`) via `dotnet ef` in the sdk:10.0 container — ensure Designer + snapshot + `[DbContext]/[Migration]` attributes present (do NOT hand-write). Register the context + apply-on-startup in `AgentModule.cs`.
- [x] T007 [P] Repositories `Infrastructure/Repositories/ConversationRepository.cs` + interface in `Domain/` — all queries filtered by `UserId` (FR-008); create/append/list/get/delete.
- [x] T008 `AgentModule.cs` — `IModuleRegistrar` registering DbContext, options, `ILlmClient`, `McpToolBridge`, `PersonaComposer`, `AgentConversationService`, repositories, and the named HttpClient `agent-anthropic`.
- [x] T009 [P] `Application/Services/ILlmClient.cs` (domain interface: `StreamAsync(system, messages, tools, ct)` → async stream of typed chunks: text-delta, tool-use, message-stop) + `Infrastructure/AnthropicLlmClient.cs` implementing it over `IHttpClientFactory` + `System.Text.Json` against the Anthropic Messages API (streaming, tool-use). Keyless ⇒ throws a typed `AgentNotConfigured` handled upstream.
- [x] T010 [P] `Application/Services/PersonaComposer.cs` — reads `agent/ledger/persona.core.md` + `adapters/browser.md` + `user.md`, composes + caches the system prompt (resolves the repo path robustly for container + dev).
- [x] T011 `Application/Services/McpToolBridge.cs` — enumerate the registered MCP tools → Anthropic tool schema (verbatim name/description/input_schema); dispatch a `tool_use` by invoking the MCP tool **in the current request scope** (user-scoped), returning `tool_result` (or `is_error`). Optional allow-list from `AgentOptions` (default all). Per contracts/tool-bridge.md.

## Phase 3: US2 — Browser Ledger (the interactive agent)

**Goal**: Denys asks finance questions in the FS web app and gets grounded, tool-backed answers in Ledger's voice, streamed.
**Independent test**: quickstart.md golden path (grounded allocation answer, three-layer market answer, progressive feedback, guardrail, context, scope).

### Backend

- [x] T012 [US2] `Application/Services/AgentConversationService.cs` — the tool-use loop: compose persona (T010) → call `ILlmClient` with bridged tools (T011) → on `tool_use` dispatch + append `tool_result` → iterate to final text (bounded by `MaxToolIterations`); yields typed stream events (conversation, text, tool start/end, error, done). History replay bounded by `HistoryTurnBudget`.
- [x] T013 [US2] `Application/Commands/SendAgentMessageCommand.cs` (+ handler) — persist the user message (create conversation if null, derive title), run the service loop, persist the assistant message on completion; returns the event stream.
- [x] T014 [P] [US2] `Application/Queries/ListConversationsQuery.cs` + `GetConversationQuery.cs` (+ handlers) — user-scoped list + full history; `DeleteConversationCommand.cs` (cascade, owner-only).
- [x] T015 [US2] `FinanceSentry.API/Controllers/AgentChatController.cs` — `POST /api/v1/agent/chat` writing `text/event-stream` per contracts/chat-endpoint.md (events: conversation/text/tool/error/done), plus `GET /conversations`, `GET /conversations/{id}`, `DELETE /conversations/{id}`. Behind `JwtAuthenticationMiddleware`; keyless ⇒ single `error: agent_not_configured`. Exempt path NOT added (auth required).
- [x] T016 [P] [US2] Contract test `backend/tests/.../AgentChatContractTests.cs` — request/response + SSE event shape + status codes + 401 unauth + keyless path + `conversation_not_found` (cross-user 404).
- [x] T017 [P] [US2] Unit tests: `McpToolBridgeTests` (schema conversion verbatim; dispatch is user-scoped; unknown tool → is_error), `PersonaComposerTests` (composes core+browser+user; no OpenClaw adapter content leaks in), `AgentConversationServiceTests` (loop terminates; max-iteration cap; tool_result threaded; LLM mocked), `KeylessTests` (no key → agent_not_configured, no HTTP call), `UserScopingTests` (tools never see another user's id).

### Frontend — UI library first

- [x] T018 [P] [US2] Build `cmn-chat-message` + `cmn-chat-input` in `frontend/projects/dsdevq-common/ui/src/lib/components/chat/` (OnPush, `cmn-` prefix, theme-aware) with Vitest specs + Storybook stories. Message renders role + streamed text + optional tool-progress chips; input handles submit/disabled/loading.

### Frontend — feature module

- [x] T019 [US2] `frontend/src/app/modules/agent/store/agent-chat.{state,computed,methods,effects,store}.ts` — messages/conversations/streaming state; `effects` holds an `rxMethod` that opens the SSE stream and appends `text` deltas + `tool` events to the active assistant message (NO setInterval; no component subscriptions); computed `isStreaming`/`errorMessage` via `ErrorMessageService`.
- [x] T020 [P] [US2] `frontend/src/app/modules/agent/services/agent.service.ts` — HTTP/SSE only (start chat stream, list/get/delete conversations); `models/` + `constants/` per file-org rules.
- [x] T021 [US2] `frontend/src/app/modules/agent/pages/ledger-chat/ledger-chat.component.ts` — declarative OnPush page binding store signals; uses `cmn-chat-message`/`cmn-chat-input`; conversation sidebar (list/new/delete). Add lazy route `/ledger` in `app.routes.ts` behind `authGuard`; add nav entry.
- [x] T022 [P] [US2] Vitest specs for `AgentChatStore` (SSE effect appends deltas; error mapping; new/select/delete conversation) using `signalState` fixtures.
- [x] T023 [US2] `npx eslint` the new Angular files (fix all), `dotnet build backend/` zero warnings, `npx ng test --watch=false` + backend tests green.

## Phase 4: US3 — Parity & coexistence

**Goal**: browser + OpenClaw Ledger consistent from one core; OpenClaw unchanged.

- [x] T024 [P] [US3] Test `PersonaParityTests` — `PersonaComposer` (browser) and a compose of core+openclaw both include the shared core laws (materiality, tier-3, stay-invested, three-layer); confirm the browser system prompt contains no OpenClaw-only mechanics (Kit/sessions/cron) and the openclaw compose does (guards the split).
- [x] T025 [US3] Verify no existing OpenClaw path touched: this feature adds only the new module + endpoint + frontend module (no change to MCP tool definitions, no change to `agent/ledger/persona.core.md` or `adapters/openclaw.md`). Document in the PR that OpenClaw Ledger is unaffected (FR-015).

## Phase 5: Polish & cross-cutting

- [x] T026 [P] Register `agent_conversations` / `agent_messages` in the Retention policy registry (024) with a sensible purge/keep decision; confirm the reflection coverage-guard test passes.
- [x] T027 [P] Update `CLAUDE.md` "Current App State" (040 US2/US3 done: module, endpoint, `/ledger` page, `agent` schema/M001, Anthropic key) and flip `specs/040-in-app-finance-agent/spec.md` Status.
- [x] T028 QA golden path (quickstart.md) via Playwright — ran 2026-08-12 against production (OpenClaw-brain transport, no Anthropic key needed). All six criteria PASS: grounded drift answer (real book via get_allocation_vs_target, needsRebalance:false), three-layer market answer (moves → rotation → book impact), progressive feedback (incremental stream deltas, UI responsive throughout), guardrail ("sell my NVDA" → refused, read-only by design, escalated for rationale; also caught NVDA isn't a Sentry holding), context (follow-up reconciled answers #1/#3 incl. the cash-definition gap), scope (test account's book only). History restores via sidebar conversation select; fresh page intentionally starts a new chat.

## Dependencies

- **Setup (T001–T004)** → **Foundational (T005–T011)** → **US2 (T012–T023)** → **US3 (T024–T025)** → **Polish (T026–T028)**.
- Within Foundational: T009/T010/T011 are parallel after T008; T005→T006→T007.
- Within US2 backend: T012→T013→T015; T014 parallel; tests T016/T017 parallel after their targets. Frontend T018 (lib) before T021 (page); T019/T020 parallel; T022 after T019.
- **Anthropic key** gates only live-chat verification (T028) — all build/test tasks are key-optional (keyless-graceful).

## Implementation strategy

MVP = **US2** (US1 already shipped). Ship US2 end-to-end key-optional; US3 is verification; Polish finalizes retention + docs + QA. The whole feature builds and tests green without the API key; dropping the key in lights up live chat.
