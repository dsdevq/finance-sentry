#!/usr/bin/env bash
# Decrypt docker/.env.sops → docker/.env using the age private key at
# ~/.config/sops/age/keys.txt (the standard sops location).
#
# Use this on a fresh clone or when you've lost your local docker/.env.
#
# Requires: sops, age, and the age private key whose public counterpart is
# listed in .sops.yaml at the repo root.

set -euo pipefail

cd "$(dirname "$0")/.."

if [[ ! -f docker/.env.sops ]]; then
  echo "error: docker/.env.sops does not exist. Nothing to decrypt." >&2
  exit 1
fi

KEYFILE="${SOPS_AGE_KEY_FILE:-$HOME/.config/sops/age/keys.txt}"
if [[ ! -f "$KEYFILE" ]]; then
  echo "error: no age key at $KEYFILE." >&2
  echo "Restore it from your password manager, or generate a new one with: age-keygen -o $KEYFILE" >&2
  echo "If you generate a new key, you must re-encrypt docker/.env.sops with the new public key (see .sops.yaml)." >&2
  exit 1
fi

if ! command -v sops >/dev/null 2>&1; then
  echo "error: sops not on PATH. Install from https://github.com/getsops/sops/releases" >&2
  exit 1
fi

if [[ -f docker/.env ]]; then
  echo "warning: docker/.env already exists — overwriting." >&2
fi

sops --decrypt --input-type dotenv --output-type dotenv docker/.env.sops > docker/.env
chmod 600 docker/.env
echo "wrote docker/.env"
