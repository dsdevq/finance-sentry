# Data Model: In-app finance agent

New `AgentDbContext` — schema `agent`, history table `__ef_migrations_history_agent`, migration `M001_InitialSchema`. No changes to existing module schemas.

## Entity: Conversation (`agent_conversations`)

A chat thread between a user and Ledger.

| Field | Type | Notes |
|---|---|---|
| `Id` | uuid (PK) | |
| `UserId` | string/uuid (FK-by-convention to the auth user) | **owner scope — every read/write filters by this**; indexed |
| `Title` | text, nullable | derived from the first user message (short); editable later |
| `CreatedAt` | timestamptz | |
| `UpdatedAt` | timestamptz | bumped on each new message; index `(UserId, UpdatedAt desc)` for the list |
| `ModelId` | text | model used (e.g. `claude-sonnet-5`) — for auditability |

## Entity: Message (`agent_messages`)

One turn in a conversation (user, assistant, or tool exchange).

| Field | Type | Notes |
|---|---|---|
| `Id` | uuid (PK) | |
| `ConversationId` | uuid (FK → agent_conversations, cascade delete) | indexed `(ConversationId, CreatedAt)` |
| `Role` | text enum | `user` \| `assistant` \| `tool` |
| `Content` | text | user/assistant natural-language content |
| `ToolCalls` | jsonb, nullable | assistant turn's requested tool_use blocks (name + input) — for replay/audit |
| `ToolResults` | jsonb, nullable | tool turn's results returned to the model (name + result summary) |
| `CreatedAt` | timestamptz | ordering key within a conversation |

### Relationships
- `Conversation 1..* Message` (cascade delete).
- `Conversation.UserId` scopes everything; there is **no** cross-user query path.

### Validation / rules
- A conversation and all its messages belong to exactly one `UserId`; the repository always filters by the authenticated caller's id (FR-008).
- `Role` restricted to the three values; `ToolCalls`/`ToolResults` only on assistant/tool rows.
- History replayed to the model per turn is bounded (most-recent-N or token-budgeted) to keep prompt size sane on long threads.

### Retention (024)
- `agent_conversations` / `agent_messages` are financial-context data → registered in the retention policy (purge/keep decision) and covered by nightly backup, same as other app tables.

## Not stored in the database
- **Persona / system prompt** — sourced from the repo files `agent/ledger/*` at runtime (US1), not the DB.
- **Anthropic API key** — `.env.sops` (`Agent__Anthropic__ApiKey`), never persisted to app tables and never sent to the client.
- **Bridged tool catalog** — derived at runtime from the MCP tool registry, not stored.
