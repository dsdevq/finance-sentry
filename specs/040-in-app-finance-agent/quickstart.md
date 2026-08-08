# Quickstart: Browser Ledger (US2)

## Configure the model key

Add the Anthropic key to the encrypted env (server-only):

```
# docker/.env.sops  →  Agent__Anthropic__ApiKey
ANTHROPIC_API_KEY=sk-ant-...
```

Keyless is fine for the rest of the app — the chat endpoint just returns `agent_not_configured` and the UI shows a disabled state.

## Run

```bash
cd docker && docker compose -f docker-compose.dev.yml up -d --build api frontend postgres
```
- Migration `agent/M001` creates `agent.agent_conversations` + `agent.agent_messages` on API start.

## Verify the golden path

Log in as the test user, open the **Ledger** page (`/ledger`), and:

1. **Grounded answer** — ask *"what's my allocation drift?"* → the reply reflects the **real** book (via `get_allocation_vs_target`), each claim attributed to its source, in Ledger's voice. (FR-007/09/10, SC-003)
2. **Three-layer market answer** — *"what happened in the market today?"* → what moved → where money rotated → what it implies for his book/IPS (not a headline). (FR-009)
3. **Progressive feedback** — a multi-tool question surfaces tool-progress; the UI never freezes. (FR-012, SC-007)
4. **Guardrail** — *"sell my NVDA"* / *"move €5k to savings"* → Ledger drafts + escalates for explicit confirmation, **does not act** (no tool exists to act). (FR-011, SC-004)
5. **Context** — a follow-up ("and vs last quarter?") uses the thread's context. (FR-013)
6. **Scope** — the reply only ever reflects the logged-in user's data. (FR-008)

### API smoke (SSE)
```bash
curl -N -X POST http://localhost:5001/api/v1/agent/chat \
  -H "Authorization: Bearer <token>" -H "Content-Type: application/json" \
  -d '{"conversationId":null,"message":"what is my net worth?"}'
# → event: conversation … text deltas … tool start/end … done
```

## Parity check (US3)
Pose the same question to the Telegram Ledger and the browser Ledger — substance, discipline, and voice are consistent (both compose `agent/ledger/persona.core.md`). Confirm the OpenClaw Ledger still runs unchanged.
