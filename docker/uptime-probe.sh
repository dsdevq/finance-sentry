#!/usr/bin/env bash
# Uptime probe (issue #511) — synthetic monitoring for the deployed stack.
#
# Installed to ~/.fs-uptime/ and scheduled via cron by deploy.sh; runs OUTSIDE the
# compose stack so it still fires when the API/gateway is down (the in-app 023
# alerting path can't report its own outage).
#
# Behavior:
# - Probes the gateway health endpoint (default http://127.0.0.1:8080/api/v1/health).
# - After FAIL_THRESHOLD consecutive failures, sends ONE Telegram alert (Ledger bot,
#   direct Bot API — no OpenClaw cognition involved) and stays quiet until recovery,
#   which sends a single ✅ note and re-arms.
# - Credentials come from probe.env next to this script (written by deploy.sh);
#   without them the probe exits quietly rather than half-working.
set -uo pipefail

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$DIR/probe.env"
STATE_FILE="$DIR/state"

# shellcheck source=/dev/null
[[ -f "$ENV_FILE" ]] && . "$ENV_FILE"

if [[ -z "${UPTIME_TELEGRAM_BOT_TOKEN:-}" || -z "${UPTIME_TELEGRAM_CHAT_ID:-}" ]]; then
  echo "$(date -u +%FT%TZ) probe.env missing UPTIME_TELEGRAM_* — skipping" >&2
  exit 0
fi

TARGET="${UPTIME_TARGET:-http://127.0.0.1:8080/api/v1/health}"
FAIL_THRESHOLD="${UPTIME_FAIL_THRESHOLD:-2}"

send_telegram() {
  curl -sS --max-time 10 \
    "https://api.telegram.org/bot${UPTIME_TELEGRAM_BOT_TOKEN}/sendMessage" \
    -d "chat_id=${UPTIME_TELEGRAM_CHAT_ID}" \
    --data-urlencode "text=$1" >/dev/null
}

fails=0
alerted=0
[[ -f "$STATE_FILE" ]] && read -r fails alerted < "$STATE_FILE" || true

if curl -sf --max-time 10 "$TARGET" >/dev/null 2>&1; then
  if [[ "$alerted" == 1 ]]; then
    send_telegram "✅ finance-sentry is back: health probe green again ($TARGET)."
    echo "$(date -u +%FT%TZ) recovered after $fails failures"
  fi
  printf '0 0\n' > "$STATE_FILE"
else
  fails=$((fails + 1))
  echo "$(date -u +%FT%TZ) probe FAILED ($fails consecutive, target $TARGET)" >&2
  if [[ "$fails" -ge "$FAIL_THRESHOLD" && "$alerted" == 0 ]]; then
    if send_telegram "🔴 finance-sentry DOWN: health probe failed $fails times in a row ($TARGET). Check the VPS."; then
      alerted=1
    fi
  fi
  printf '%s %s\n' "$fails" "$alerted" > "$STATE_FILE"
fi
