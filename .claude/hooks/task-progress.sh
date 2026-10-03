#!/usr/bin/env bash
# Fires on: TaskCreated, TaskCompleted, TeammateIdle
# Uses python3 for JSON parsing (no jq dependency)

EVENT=$(cat)
TIMESTAMP=$(date '+%H:%M:%S')

# Parse event fields using Python
read_field() {
  echo "$EVENT" | python3 -c "
import sys, json
try:
    d = json.load(sys.stdin)
    print(d.get('$1', d.get('task_name', d.get('teammate_name', d.get('tool_name', '—')))))
except:
    print('—')
" 2>/dev/null || echo "—"
}

EVENT_NAME=$(echo "$EVENT" | python3 -c "
import sys, json
try:
    d = json.load(sys.stdin)
    print(d.get('hook_event_name', 'unknown'))
except:
    print('unknown')
" 2>/dev/null || echo "unknown")

LABEL=$(echo "$EVENT" | python3 -c "
import sys, json
try:
    d = json.load(sys.stdin)
    print(d.get('task_name') or d.get('teammate_name') or d.get('tool_name') or '—')
except:
    print('—')
" 2>/dev/null || echo "—")

mkdir -p progress

# Always log the raw event
echo "[$TIMESTAMP] $EVENT_NAME  $LABEL" >> progress/events.log

# Find the active status file (most recently modified *.status)
STATUS_FILE=$(ls -t progress/*.status 2>/dev/null | head -1)

if [ -n "$STATUS_FILE" ]; then
  update_status() {
    local key="$1"
    local value="$2"
    local tmp
    tmp=$(mktemp)
    grep -v "^${key}=" "$STATUS_FILE" > "$tmp"
    echo "${key}=${value}" >> "$tmp"
    mv "$tmp" "$STATUS_FILE"
  }

  case "$EVENT_NAME" in
    TaskCreated)
      TOTAL=$(grep "^TASKS_TOTAL=" "$STATUS_FILE" | cut -d= -f2)
      TOTAL=$(( ${TOTAL:-0} + 1 ))
      update_status "TASKS_TOTAL" "$TOTAL"
      update_status "LAST_UPDATE" "$TIMESTAMP"
      update_status "LAST_EVENT" "Task created: $LABEL"
      ;;

    TaskCompleted)
      DONE=$(grep "^TASKS_DONE=" "$STATUS_FILE" | cut -d= -f2)
      DONE=$(( ${DONE:-0} + 1 ))
      update_status "TASKS_DONE" "$DONE"
      update_status "LAST_UPDATE" "$TIMESTAMP"
      update_status "LAST_EVENT" "$LABEL complete"
      ;;

    TeammateIdle)
      update_status "LAST_UPDATE" "$TIMESTAMP"
      update_status "LAST_EVENT" "$LABEL idle"
      ;;
  esac
fi

exit 0
