#!/usr/bin/env bash
# Refreshes the Stitch MCP access token and re-registers the MCP server in Claude.
# Run this when Claude says "Unauthenticated" for Stitch tools (tokens expire ~1 hour).
#
# Usage: bash scripts/refresh-stitch-token.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$SCRIPT_DIR/../.env"

echo "Fetching new access token via gcloud..."
TOKEN=$(gcloud auth application-default print-access-token)

# Read existing PROJECT_ID from .env (or fall back to env var)
if [[ -f "$ENV_FILE" ]]; then
  PROJECT_ID=$(grep '^GOOGLE_CLOUD_PROJECT=' "$ENV_FILE" | cut -d'=' -f2)
fi
PROJECT_ID="${PROJECT_ID:-${GOOGLE_CLOUD_PROJECT:-}}"

if [[ -z "$PROJECT_ID" ]]; then
  echo "ERROR: GOOGLE_CLOUD_PROJECT not found in .env and not set in environment." >&2
  exit 1
fi

echo "Writing .env..."
{
  echo "GOOGLE_CLOUD_PROJECT=$PROJECT_ID"
  echo "STITCH_ACCESS_TOKEN=$TOKEN"
} > "$ENV_FILE"

echo "Re-registering Stitch MCP with Claude (user scope)..."
claude mcp remove stitch 2>/dev/null || true

claude mcp add stitch \
  --transport http https://stitch.googleapis.com/mcp \
  --header "Authorization: Bearer $TOKEN" \
  --header "X-Goog-User-Project: $PROJECT_ID" \
  -s user

echo "Done. Token refreshed and MCP re-registered."
echo "Restart your Claude session to pick up the new token."
