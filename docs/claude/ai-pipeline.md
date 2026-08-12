# AI Development Pipeline

## AI Development Pipeline

This project uses a two-model pipeline. [`.specify/memory/pipeline.md`](.specify/memory/pipeline.md) is the **source of truth** for pipeline roles, knowledge store structure, and the per-task implementation loop — read it before starting any implementation session.

**Claude** = planner + orchestrator + reviewer (this session).
**Qwen2.5-coder:14b** (local Ollama) = implementer, called via the `qwen-code` MCP server.

> ~~**Hard rule:** Qwen is the sole code producer. If a Qwen MCP call fails with a connection error, **stop immediately** and report the error — do NOT write implementation code directly as a fallback. Wait for the user to confirm Ollama is back up before retrying. Only bypass this rule if the user explicitly says so.~~ **TEMPORARILY DISABLED** — Claude implementing directly while Qwen/Ollama is unavailable.

- MCP config: `.mcp.json` + `.claude/settings.json` → `enabledMcpjsonServers: ["qwen-code"]`
- MCP is only active in **new sessions** — check `/mcp` to confirm it loaded
- Knowledge rules live in `.specify/knowledge/index.yaml` (30 rules); inject into QWEN.md with `py .specify/integrations/qwen/scripts/inject-knowledge.py`
- Per-task loop: Claude calls Qwen MCP → reads diff → reviews inline → approves or requests fix → commits → next task
- Reviews saved to `.specify/knowledge/reviews/<feature>/<task>.yaml`

---
