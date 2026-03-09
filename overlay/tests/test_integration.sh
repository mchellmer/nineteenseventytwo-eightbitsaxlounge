#!/usr/bin/env bash
# Simple integration smoke script (requires NATS at $NATS_URL and overlay running at $OVERLAY_URL)
export NATS_URL=${NATS_URL:-nats://127.0.0.1:4222}
export NATS_USER=${NATS_USER:-overlay}
export NATS_PASS=${NATS_PASS:-overlaypw}
OVERLAY_URL=${OVERLAY_URL:-http://localhost:3000/grid.html}

echo "Publishing sample messages to $NATS_URL (user=$NATS_USER pass=$NATS_PASS)"
node ./scripts/publish-sample.js || exit 1

# publish a few different subjects using the node publisher (no external CLI required)
SUBJECTS=(
  "overlay.engine|{\"value\":\"offspring\"}"
  "overlay.time|{\"value\":3}"
  "overlay.dial1|{\"value\":7}"
  "overlay.dial2|{\"value\":10}"
  "overlay.delay|{\"value\":1}"
  "overlay.player|{\"value\":\"SJH\"}"
)
for s in "${SUBJECTS[@]}"; do
  IFS='|' read -r subj payload <<< "$s"
  echo " -> $subj $payload"
  SUBJECT="$subj" PAYLOAD="$payload" node ./scripts/publish-sample.js || exit 1
  sleep 0.2
done

echo "Open $OVERLAY_URL in your browser or OBS browser source to see updates."
