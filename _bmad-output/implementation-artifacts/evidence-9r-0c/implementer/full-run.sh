#!/bin/sh
# Story 9R.0c candidate 1: one serial cumulative Chrome run of every tools/e2e/*.test.ts, per-file TAP and exit code.
set -u
repo=/Users/aidan/dev/personal/tim/.claude/worktrees/agent-a67f1ccf03b1c80b5
out=/private/tmp/claude-501/-Users-aidan-dev-personal-tim/49d80295-44c4-4940-bc16-8455cdf5d09d/scratchpad/9r0c/c1-full-run
loader=/Users/aidan/.npm/_npx/e6b261a129b06bcc/node_modules/tsx/dist/loader.mjs
mkdir -p "$out"
cd "$repo" || exit 2
summary="$out/summary.txt"
: > "$summary"
echo "candidate $(git rev-parse HEAD) started $(date -u +%Y-%m-%dT%H:%M:%SZ)" >> "$summary"
for file in tools/e2e/*.test.ts; do
    name=$(basename "$file" .test.ts)
    start=$(date +%s)
    node --import "$loader" --test --test-reporter=tap --test-concurrency=1 --test-timeout=150000 "$file" > "$out/$name.tap" 2>&1
    code=$?
    end=$(date +%s)
    pass=$(grep -E '^# pass ' "$out/$name.tap" | awk '{print $3}')
    fail=$(grep -E '^# fail ' "$out/$name.tap" | awk '{print $3}')
    line="$name exit=$code pass=${pass:-?} fail=${fail:-?} seconds=$((end - start))"
    echo "$line" >> "$summary"
    echo "$line"
done
echo "finished $(date -u +%Y-%m-%dT%H:%M:%SZ)" >> "$summary"
echo "FULL_RUN_DONE"
