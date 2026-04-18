---
description: Implement tasks from tasks.md by delegating code generation to Qwen (via qwen-code MCP) — Claude orchestrates, reviews, and commits each task
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Prerequisites

1. Confirm the `qwen-code` MCP server is loaded — run `/mcp` and verify `qwen-code` appears. If it is not listed, stop and tell the user to restart Claude Code so the MCP server is picked up from `.mcp.json`.

2. Run `.specify/scripts/bash/check-prerequisites.sh --json --require-tasks --include-tasks` from repo root to resolve `FEATURE_DIR` and confirm `tasks.md` exists.

3. Load implementation context:
   - **REQUIRED**: Read `tasks.md` for the full task list
   - **REQUIRED**: Read `.specify/knowledge/index.yaml` for active rules (these go into every Qwen prompt)
   - **IF EXISTS**: Read `plan.md`, `data-model.md`, `contracts/`, `research.md` for additional context

4. **Check for extension hooks** under `hooks.before_implement` in `.specify/extensions.yml` and execute any enabled mandatory hooks before proceeding.

---

## Per-Task Loop

Repeat the following for **each task marked `- [ ]`** in `tasks.md`, in order. Complete one task fully before starting the next.

### Step 1 — Build the Qwen prompt

Construct a prompt containing:
- The current task ID and full description (copied verbatim from tasks.md)
- The feature name and branch
- All **enabled** rules from `.specify/knowledge/index.yaml` formatted as a numbered list
- Relevant file paths mentioned in the task description
- Any context from `plan.md` / `data-model.md` / `contracts/` that directly applies to this task
- Explicit instruction: *"Implement only this task. Output the complete file contents for each file that must be created or modified. Do not implement any other tasks."*

### Step 2 — Call Qwen MCP

Call the `qwen-code` MCP `generate-code` tool with the prompt from Step 1.

Wait for the response. If the MCP call fails or returns an error, stop and report the error to the user.

### Step 3 — Write the files

Parse Qwen's response. For each file Qwen specifies:
- Use the **Edit** tool if the file already exists
- Use the **Write** tool if the file is new

Do not skip files. Do not partially apply changes.

### Step 4 — Review

After writing all files, review the changes against the active knowledge rules:

1. Read the current git diff (`git diff HEAD`)
2. Check each enabled rule from `.specify/knowledge/index.yaml` against the diff
3. Produce a review in the following YAML structure:

```yaml
task_id: "<task_id>"
feature: "<branch_name>"
reviewed_at: "<today_iso>"
approved: true|false
files_changed: []
violations:
  - rule_id: "ANG-001"
    file: "path/to/file.ts"
    line: 12
    found: "what Qwen wrote"
    fix: "what it should be"
new_rules: []
next_task_notes: ""
```

Save the review to `.specify/knowledge/reviews/<feature>/<task-id>.yaml`.

If any **new rules** are proposed, append them to `.specify/knowledge/index.yaml` and `.specify/knowledge/rules/generated.md`.

### Step 5a — APPROVED (zero violations)

- Mark the task complete in `tasks.md`: change `- [ ]` to `- [x]`
- Commit:
  ```bash
  git add -A
  git commit -m "[<task_id>] <task_description>"
  ```
- Report: `✓ <task_id> approved and committed`
- Continue to the next task

### Step 5b — VIOLATIONS FOUND

- Display each violation clearly to the user (rule_id, file, line, what was found, what the fix is)
- Build a correction prompt for Qwen:
  - Include the original task description
  - List every violation with the required fix
  - Instruction: *"Fix only the listed violations. Do not change anything else."*
- Call Qwen MCP again (Step 2) with the correction prompt
- Apply the returned fixes (Step 3)
- Re-run the review (Step 4)
- Repeat until approved or until 3 correction attempts are exhausted
- If still failing after 3 attempts: stop, show all remaining violations, and ask the user how to proceed

---

## Completion

After all tasks are done:

1. Report a summary:
   - Tasks completed
   - Reviews saved (paths)
   - New rules added to the knowledge store (if any)

2. Check for `hooks.after_implement` in `.specify/extensions.yml` and execute any enabled mandatory hooks.
