# Fix: drop deprecated `-p` flag and add Windows quoting for positional prompt

## Problem

On newer versions of `qwen` CLI, the `-p` / `--prompt` flag is deprecated and **rejected** when a positional argument is also present:

```
Cannot use both a positional prompt and the --prompt (-p) flag together
```

Because `executeCommand` uses `shell: true` on Windows, a multi-word prompt passed without quotes gets split by `cmd.exe` into multiple positional arguments. With `-p word1` followed by `word2 word3 ...` as stray positionals, the CLI throws the error above on every `ask-qwen` call.

## Root cause

`qwenExecutor.ts` builds the args array as:

```ts
args.push(CLI.FLAGS.PROMPT, finalPrompt); // CLI.FLAGS.PROMPT = "-p"
```

Combined with `shell: true` on Windows and the quoting guard only triggering for prompts that contain `@`, any plain multi-word prompt produces a malformed command.

## Fix

Two changes to `qwenExecutor.ts` (applied to both the main path and the fallback path):

### 1. Drop `-p`, use positional arg

```ts
// Before
args.push(CLI.FLAGS.PROMPT, finalPrompt);

// After
args.push(finalPrompt);
```

### 2. Always quote on Windows, not just when `@` is present

```ts
// Before
const finalPrompt = prompt_processed.includes('@') && !prompt_processed.startsWith('"')
    ? `"${prompt_processed}"`
    : prompt_processed;

// After
const needsQuoting = process.platform === 'win32' || prompt_processed.includes('@');
const finalPrompt = needsQuoting && !prompt_processed.startsWith('"')
    ? `"${prompt_processed.replace(/"/g, '\\"')}"`
    : prompt_processed;
```

The `replace(/"/g, '\\"')` prevents prompt content that contains `"` from breaking the shell string on Windows.

## Impact

- **Linux / macOS**: `shell: false` — args are passed directly to the process, no splitting. No behavioural change beyond dropping the deprecated flag.
- **Windows**: `shell: true` — prompts are now always quoted before being handed to `cmd.exe`, so multi-word prompts are treated as a single positional argument.
- **Fallback path** (quota exceeded → weak model): same two changes applied.

## Testing

Verified on Windows 11 with `qwen` CLI (latest). After the fix, `ask-qwen` successfully handles plain multi-word prompts and prompts containing `@` file references.
