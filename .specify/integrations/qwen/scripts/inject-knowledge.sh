#!/usr/bin/env bash
# inject-knowledge.sh — Inject knowledge rules into QWEN.md
#
# Usage:
#   ./inject-knowledge.sh                     # inject all enabled rules
#   ./inject-knowledge.sh angular backend     # inject matching categories only
#   ./inject-knowledge.sh --list              # print rules without writing

set -euo pipefail

_script_dir="$(cd "$(dirname "$0")" && pwd)"

# Windows uses `py`, Unix uses `python3`
if command -v py &>/dev/null; then
    _python="py"
elif command -v python3 &>/dev/null; then
    _python="python3"
else
    _python="python"
fi

"$_python" "$_script_dir/inject-knowledge.py" "$@"
