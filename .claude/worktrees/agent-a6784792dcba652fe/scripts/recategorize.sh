#!/usr/bin/env bash
#
# One-shot: re-categorize existing transactions.
#
# Rows ingested before the category overhaul never stored their raw MCC/PFC, so their
# category cannot be recomputed from the database alone — this re-fetches them from the
# bank providers (Plaid full history; Monobank full history in 31-day windows, rate-limited;
# TrueLayer last ~90 days) and updates the stored rows in place. Safe to re-run; it only
# touches category fields. Monobank history can take a while (one API call per ~minute).
#
# Runs a throwaway instance of the API inside the already-running container, so it reuses
# the app's credentials, decryption key, and provider clients. It does NOT start the web
# server or bind any ports — it runs the task and exits.
#
# Usage:
#   ./scripts/recategorize.sh            # every user with bank accounts
#   ./scripts/recategorize.sh <userId>   # a single user
#
# Override the container name with API_CONTAINER=... if it differs.
set -euo pipefail

CONTAINER="${API_CONTAINER:-finance-sentry-api}"

if [ "$#" -ge 1 ]; then
  exec docker exec "$CONTAINER" dotnet FinanceSentry.API.dll recategorize "$1"
else
  exec docker exec "$CONTAINER" dotnet FinanceSentry.API.dll recategorize
fi
