#!/usr/bin/env bash
# Claude Code status line for spec-driven development
# Uses python3 for JSON parsing (no jq dependency)

SESSION=$(cat)

# Parse session metrics using Python
read_session() {
  echo "$SESSION" | python3 -c "
import sys, json
try:
    d = json.load(sys.stdin)
    ctx = d.get('context_window', {}).get('used_percentage', 0)
    cost = d.get('cost', {}).get('total_cost_usd', 0)
    model = d.get('model', {}).get('display_name', '')
    print(f'{int(ctx)}|{cost:.2f}|{model}')
except Exception as e:
    print('0|0.00|')
" 2>/dev/null || echo "0|0.00|"
}

IFS='|' read -r CTX COST MODEL <<< "$(read_session)"

# Context bar (10 chars)
BAR_FILLED=$(( ${CTX:-0} / 10 ))
BAR_EMPTY=$(( 10 - BAR_FILLED ))
CTX_BAR=""
for i in $(seq 1 $BAR_FILLED); do CTX_BAR="${CTX_BAR}█"; done
for i in $(seq 1 $BAR_EMPTY);  do CTX_BAR="${CTX_BAR}░"; done

# --- Active team run ---
STATUS_FILE=$(ls -t progress/*.status 2>/dev/null | head -1)

if [ -n "$STATUS_FILE" ]; then
  SLUG=$(grep    "^SLUG="        "$STATUS_FILE" | cut -d= -f2)
  PHASE=$(grep   "^PHASE="       "$STATUS_FILE" | cut -d= -f2)
  DONE=$(grep    "^TASKS_DONE="  "$STATUS_FILE" | cut -d= -f2)
  TOTAL=$(grep   "^TASKS_TOTAL=" "$STATUS_FILE" | cut -d= -f2)
  CURRENT=$(grep "^CURRENT="     "$STATUS_FILE" | cut -d= -f2)
  LAST=$(grep    "^LAST_EVENT="  "$STATUS_FILE" | cut -d= -f2-)
  STATUS=$(grep  "^STATUS="      "$STATUS_FILE" | cut -d= -f2)

  DONE=${DONE:-0}
  TOTAL=${TOTAL:-0}

  # Task progress bar
  if [ "$TOTAL" -gt 0 ]; then
    T_FILLED=$(( (DONE * 10) / TOTAL ))
  else
    T_FILLED=0
  fi
  T_EMPTY=$(( 10 - T_FILLED ))
  TASK_BAR=""
  for i in $(seq 1 $T_FILLED); do TASK_BAR="${TASK_BAR}█"; done
  for i in $(seq 1 $T_EMPTY);  do TASK_BAR="${TASK_BAR}░"; done

  case "$STATUS" in
    running) ICON="⟳" ;;
    blocked) ICON="!" ;;
    done)    ICON="✓" ;;
    *)       ICON="·" ;;
  esac

  echo "$ICON  $SLUG  Phase $PHASE  $TASK_BAR $DONE/$TOTAL tasks  $CURRENT"
  echo "   Last: $LAST"
fi

echo "ctx $CTX_BAR ${CTX}%  \$${COST}  $MODEL"
