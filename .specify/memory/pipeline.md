# AI-Assisted Development Pipeline

**Version**: 1.1 | **Established**: 2026-04-18 | **Updated**: 2026-04-19

> **Source of truth** for the two-model implementation pipeline. When `CLAUDE.md`, skill definitions, or other docs conflict with this file, this file wins.

---

## Roles

| Role | Model | Responsibility |
|------|-------|----------------|
| **Planner / Orchestrator / Reviewer** | Claude (this session) | Reads spec, generates `tasks.md`, calls Qwen per task, reviews output against knowledge rules, approves or requests fixes |
| **Implementer** | Qwen2.5-coder:14b (local, Ollama) | Implements one task at a time as instructed by Claude |

**Why two models**: Qwen runs locally (free, no latency cost for coding), Claude handles reasoning-heavy work — planning, rule enforcement, and architectural review.

---

## How Qwen Is Accessible

Qwen is exposed as an MCP server named **`qwen-code`** via the `qwen-mcp-tool` npm package.

- Config: `.mcp.json` at repo root
- Enabled: `.claude/settings.json` → `enabledMcpjsonServers: ["qwen-code"]`
- Tools available once MCP loads: `generate-code`, `review-code` (check with `/mcp` in Claude Code)
- **Only available in new sessions** — restart Claude Code or open a new session to pick it up

Qwen connects to Ollama at `http://localhost:11434` using model `qwen2.5-coder:14b`.

---

## Knowledge Store

Lives in `.specify/knowledge/`. Grows automatically as reviews are run.

```
.specify/knowledge/
├── index.yaml              # Machine-readable rule index (id/category/enabled/fire_count/last_fired)
├── review-template.yaml    # YAML schema for review output
├── rules/
│   ├── angular.md          # ANG-001..010 — Angular/TypeScript rules
│   ├── backend.md          # BE-001..007  — ASP.NET Core / C# rules
│   ├── testing.md          # TEST-001..005 — test coverage rules
│   ├── versioning.md       # VER-001..003 — version bump rules
│   ├── architecture.md     # ARCH-001..005 — modular monolith rules
│   └── generated.md        # Auto-generated rules from reviews (may not exist yet)
├── anti-patterns/
│   └── <category>.md       # Auto-generated when a rule's fire_count reaches 2; bad/good code pairs
├── examples/
│   └── <category>.md       # Auto-generated on task approval; positive reference snippets
└── reviews/
    └── <feature>/          # One YAML review file per task, e.g. 004-adopt-oauth/t001.yaml
```

**Inject script**: `.specify/integrations/qwen/scripts/inject-knowledge.py`
Writes all enabled rules into `QWEN.md` between `<!-- KNOWLEDGE RULES START/END -->` markers.
Run via: `py .specify/integrations/qwen/scripts/inject-knowledge.py`

**QWEN.md** at repo root is Qwen's context file (analogous to `CLAUDE.md` for Claude).

---

## Per-Task Implementation Loop

For each task in `tasks.md` marked `- [ ]`:

```
1. Claude calls Qwen MCP tool with the task description + all enabled rules + any relevant anti-patterns
2. Qwen returns generated code as text (MCP is text-in, text-out only)
3. Claude writes/edits the files using Edit/Write tools
4. Claude reviews the changes against knowledge rules (inline)
4b. fire_count + last_fired updated in index.yaml for each violated rule;
    if fire_count reaches 2 → anti-pattern entry auto-written to anti-patterns/<category>.md
5a. APPROVED  → positive example written to examples/<category>.md;
               Claude marks task [x] in tasks.md, commits, moves to next task
5b. VIOLATIONS → Claude shows violations + injects matching anti-patterns into correction prompt,
               feeds back to Qwen, repeats from step 2
```

Reviews are saved to `.specify/knowledge/reviews/<feature>/<task-id>.yaml`.
New rules discovered during review are appended to `index.yaml` + `rules/generated.md`.

---

## Workflow Per Feature

```
speckit toolchain (Claude)          Qwen (via MCP)
──────────────────────────────      ──────────────────────────────
1. spec.md already exists
2. /speckit.tasks               →   (generates tasks.md)
3. For each [ ] task:
   a. Call qwen MCP tool        →   Implement task
   b. Review diff               ←   (writes files)
   c. Fix violations if any     →←  (iterate)
   d. Commit approved task
4. All tasks done → report
```

**Always use speckit slash commands** — do not generate spec/plan/tasks artifacts manually. The commands enforce constitution gates, consistent formatting, and hook execution that manual generation skips.

| Stage | Command | Who runs it |
|-------|---------|-------------|
| Write/update spec | `/speckit.specify` | Claude |
| Clarify spec | `/speckit.clarify` | Claude |
| Generate plan | `/speckit.plan` | Claude |
| Generate tasks | `/speckit.tasks` | Claude |
| **Implement via Qwen MCP** | **`/speckit.implement-qwen`** | **Claude orchestrates, Qwen generates** |
| Implement directly (no Qwen) | `/speckit.implement` | Claude |
| Post-implementation analysis | `/speckit.analyze` | Claude |
| Generate checklist | `/speckit.checklist` | Claude |

Commands are defined in `.claude/commands/speckit.*.md`.

### Why Qwen cannot run speckit commands

The `qwen-code` MCP tool is **text-in, text-out only** — Qwen receives a prompt and returns generated code as text. It has no file system access, no shell, and cannot invoke slash commands. `/speckit.*` commands are Claude Code skills and require the full Claude Code runtime.

`qwen-code` in interactive mode (terminal) can run its own `.qwen/commands/*` skills, but that requires a human at the terminal and cannot be automated reliably via one-shot invocation.

**Practical boundary**:
- Claude owns all speckit stages and file writes
- Qwen (via MCP) owns code generation only — Claude feeds it a task, Qwen returns code, Claude writes the files and reviews

---

## Files Created by This Pipeline

| File | Created by | Purpose |
|------|-----------|---------|
| `specs/<feature>/tasks.md` | Claude | Atomic implementation checklist |
| `QWEN.md` | inject script | Qwen's project context + rules |
| `.specify/knowledge/reviews/<feature>/*.yaml` | Claude (inline review) | Per-task review record |
| `.specify/knowledge/rules/generated.md` | Auto (from reviews) | New rules discovered during reviews |
| `.mcp.json` | One-time setup | Qwen MCP server config |

---

## Key Scripts

| Script | Purpose |
|--------|---------|
| `.specify/integrations/qwen/scripts/inject-knowledge.py` | Injects rules into QWEN.md |
| `.specify/integrations/qwen/scripts/review-task.py` | CLI review (legacy — Claude now reviews inline via MCP) |

