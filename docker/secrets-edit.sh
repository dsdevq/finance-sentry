#!/usr/bin/env bash
# Edit docker/.env.sops IN PLACE — sops decrypts into your $EDITOR and re-encrypts
# on save, so no plaintext secrets file is ever written to disk.
#
# This replaces the old edit-plaintext-then-encrypt round-trip (secrets-encrypt.sh).
# Just run this, change values, save & quit.
#
#   ./docker/secrets-edit.sh
#
# The decrypted docker/.env is only needed at RUNTIME (docker compose --env-file):
# deploy.sh produces it on the VPS, and secrets-decrypt.sh produces it on a fresh
# clone. You do NOT need it to edit secrets.
#
# Requires: sops, age, and the age private key whose public counterpart is in
# .sops.yaml (default location ~/.config/sops/age/keys.txt).

set -euo pipefail

cd "$(dirname "$0")/.."

if ! command -v sops >/dev/null 2>&1; then
  echo "error: sops not on PATH. Install from https://github.com/getsops/sops/releases" >&2
  exit 1
fi

KEYFILE="${SOPS_AGE_KEY_FILE:-$HOME/.config/sops/age/keys.txt}"
if [[ ! -f "$KEYFILE" ]]; then
  echo "error: no age key at $KEYFILE — cannot decrypt for editing." >&2
  echo "Restore it from your password manager, or generate one with: age-keygen -o $KEYFILE" >&2
  echo "(A new key requires re-encrypting docker/.env.sops with its public half — see .sops.yaml.)" >&2
  exit 1
fi

if [[ ! -f docker/.env.sops ]]; then
  echo "error: docker/.env.sops does not exist." >&2
  echo "Bootstrap it once from a plaintext docker/.env with:" >&2
  echo "  sops --encrypt --input-type dotenv --output-type dotenv docker/.env > docker/.env.sops" >&2
  exit 1
fi

# In-place edit. .env.sops is dotenv content, so pin the type explicitly (the
# .env.sops name has no recognised extension). sops re-encrypts on save and is a
# no-op if you quit without changing anything.
SOPS_AGE_KEY_FILE="$KEYFILE" exec sops --input-type dotenv --output-type dotenv docker/.env.sops
