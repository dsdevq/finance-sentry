# inject-knowledge.ps1 — Inject knowledge rules into QWEN.md
#
# Usage:
#   .\inject-knowledge.ps1                     # inject all enabled rules
#   .\inject-knowledge.ps1 angular backend     # inject matching categories only
#   .\inject-knowledge.ps1 --list              # print rules without writing

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$PythonScript = Join-Path $ScriptDir "inject-knowledge.py"

# Prefer py (Windows Launcher), fall back to python
$PyCmd = if (Get-Command py -ErrorAction SilentlyContinue) { "py" }
         elseif (Get-Command python3 -ErrorAction SilentlyContinue) { "python3" }
         else { "python" }

& $PyCmd $PythonScript @args
exit $LASTEXITCODE
