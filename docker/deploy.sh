#!/usr/bin/env bash
# Deploy finance-sentry on the VPS.
#
# - Decrypts docker/.env.sops → docker/.env (requires age key at SOPS_AGE_KEY_FILE
#   or ~/.config/sops/age/keys.txt).
# - Builds + starts the prod compose stack with linux/arm64 images.
# - Idempotent: safe to re-run on every push to main.
#
# Runs on a self-hosted GitHub Actions runner inside the repo working directory.
# Expects: docker, docker compose, sops, age installed on the host.

set -euo pipefail

cd "$(dirname "$0")/.."

KEYFILE="${SOPS_AGE_KEY_FILE:-$HOME/.config/sops/age/keys.txt}"
if [[ ! -f "$KEYFILE" ]]; then
  echo "error: no age key at $KEYFILE — cannot decrypt docker/.env.sops" >&2
  exit 1
fi

if ! command -v sops >/dev/null 2>&1; then
  echo "error: sops missing on PATH. Install from https://github.com/getsops/sops/releases" >&2
  exit 1
fi

echo "[deploy] decrypt docker/.env.sops"
SOPS_AGE_KEY_FILE="$KEYFILE" sops --decrypt --input-type dotenv --output-type dotenv docker/.env.sops > docker/.env
chmod 600 docker/.env

echo "[deploy] detect host docker gid for socket access"
if [[ -S /var/run/docker.sock ]]; then
  DOCKER_GID="$(stat -c '%g' /var/run/docker.sock)"
  export DOCKER_GID
  echo "[deploy] DOCKER_GID=${DOCKER_GID}"
else
  echo "warning: /var/run/docker.sock missing on host — IBeam orchestration will not work" >&2
fi

echo "[deploy] docker compose build + up"
docker compose -f docker/docker-compose.prod.yml --env-file docker/.env up -d --build --remove-orphans

echo "[deploy] prune dangling images (free disk on the VPS)"
docker image prune -f >/dev/null

echo "[deploy] wait for api health"
deadline=$((SECONDS + 120))
until curl -sf http://127.0.0.1:5001/api/v1/health >/dev/null 2>&1; do
  if [[ $SECONDS -gt $deadline ]]; then
    echo "error: api health check timed out after 120s" >&2
    docker compose -f docker/docker-compose.prod.yml logs --tail 60 api
    exit 1
  fi
  sleep 2
done

echo "[deploy] ok — api reachable on 127.0.0.1:5001"
