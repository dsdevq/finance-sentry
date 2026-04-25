---
name: csharp-quality
description: Run dotnet build, collect all IDE/analyzer suggestions, apply every suggested fix that doesn't break the build. Use after implementing backend tasks, before committing, or when asked to "clean up", "fix warnings", or "improve C# code quality".
user-invocable: true
allowed-tools:
  - Read
  - Write
  - Edit
  - Glob
  - Grep
  - Bash(dotnet build *)
  - Bash(find *)
  - Bash(ls *)
---

# C# Code Quality

Collect every IDE/analyzer suggestion from `dotnet build`, apply each one, verify the build still passes. If a fix breaks the build — revert it and move on. The goal is zero warnings with a green build, not a specific list of changes.

## Scope

If invoked with a path argument (e.g. `/csharp-quality backend/src/FinanceSentry.Modules.BankSync/`), scope to that project. Otherwise run against the full `backend/` solution.

---

## Phase 1 — Collect Suggestions

```bash
dotnet build backend/ --no-incremental 2>&1
```

Capture the full output. Extract every line that contains `warning` — these are the IDE/analyzer suggestions to work through. Group them by file so each file is Read once and fixed in a single Edit pass.

---

## Phase 2 — Apply Suggestions Per File

For each file with suggestions:

1. **Read the file** once.
2. **Apply every suggested fix** from the analyzer output for that file.
3. **Write the file** with all fixes applied in one shot.

The rule for applying a fix: apply it if it is the IDE's own suggestion and it is semantics-preserving (it does not change what the code does at runtime — only how it's written). If you are not confident a fix is semantics-preserving, skip it and note it in the report.

Do not invent fixes that the analyzer did not suggest. Only apply what `dotnet build` explicitly recommends.

---

## Phase 3 — Verify

```bash
dotnet build backend/ --no-incremental 2>&1
```

- **Build passes, zero warnings** → done.
- **Build passes, warnings remain** → the remaining warnings are ones you skipped (not semantics-preserving). Note them in the report.
- **Build fails** → a fix broke something. Read the error, identify which file caused it, revert that file's changes, and re-run. Report the reverted fix.

---

## Phase 4 — Report

```
C# QUALITY FIXES APPLIED
─────────────────────────
FinanceSentry.Modules.BankSync/Infrastructure/PlaidClient.cs
  3 suggestions applied

FinanceSentry.API/Program.cs
  2 suggestions applied

SKIPPED (not semantics-preserving — manual review needed)
──────────────────────────────────────────────────────────
FinanceSentry.Modules.BankSync/Application/Commands/SyncCommand.cs:47
  Suggestion: <what the IDE said>
  Reason: <one sentence why you didn't apply it>

REVERTED (broke the build)
───────────────────────────
(none)

Build result: ✅ 0 warnings, 0 errors
```

Keep the report minimal — file names and counts, not a line-by-line transcript.
