#!/usr/bin/env bash
# Encrypt docker/.env → docker/.env.sops using the recipients in /.sops.yaml.
# Run after editing docker/.env to persist new values into the encrypted file
# that lives in git.
#
# Requires: sops, age (https://github.com/getsops/sops, https://github.com/FiloSottile/age)

set -euo pipefail

cd "$(dirname "$0")/.."

if [[ ! -f docker/.env ]]; then
  echo "error: docker/.env does not exist. Create it (or run docker/secrets-decrypt.sh first) before encrypting." >&2
  exit 1
fi

if ! command -v sops >/dev/null 2>&1; then
  echo "error: sops not on PATH. Install from https://github.com/getsops/sops/releases or run: curl -sSL -o ~/.local/bin/sops https://github.com/getsops/sops/releases/download/v3.9.1/sops-v3.9.1.linux.amd64 && chmod +x ~/.local/bin/sops" >&2
  exit 1
fi

sops --encrypt --input-type dotenv --output-type dotenv docker/.env > docker/.env.sops
echo "wrote docker/.env.sops (commit this to git)"
