#!/usr/bin/env bash
# DEPRECATED — prefer in-place editing: ./docker/secrets-edit.sh
#
# The old workflow (decrypt → edit plaintext docker/.env → run this to re-encrypt)
# leaves plaintext secrets on disk. secrets-edit.sh avoids that entirely.
#
# This script is kept only for two cases:
#   1. Bootstrapping docker/.env.sops the first time from a plaintext docker/.env.
#   2. Persisting changes you already made directly to docker/.env.
# If you just want to change a secret, stop and run ./docker/secrets-edit.sh instead.
#
# Requires: sops, age (https://github.com/getsops/sops, https://github.com/FiloSottile/age)

set -euo pipefail

cd "$(dirname "$0")/.."

echo "note: secrets-encrypt.sh is deprecated — for routine edits use ./docker/secrets-edit.sh (in-place, no plaintext on disk)." >&2

if [[ ! -f docker/.env ]]; then
  echo "error: docker/.env does not exist. To edit secrets use ./docker/secrets-edit.sh," >&2
  echo "or run docker/secrets-decrypt.sh first if you specifically need a plaintext .env." >&2
  exit 1
fi

if ! command -v sops >/dev/null 2>&1; then
  echo "error: sops not on PATH. Install from https://github.com/getsops/sops/releases or run: curl -sSL -o ~/.local/bin/sops https://github.com/getsops/sops/releases/download/v3.9.1/sops-v3.9.1.linux.amd64 && chmod +x ~/.local/bin/sops" >&2
  exit 1
fi

sops --encrypt --input-type dotenv --output-type dotenv docker/.env > docker/.env.sops
echo "wrote docker/.env.sops (commit this to git)"
