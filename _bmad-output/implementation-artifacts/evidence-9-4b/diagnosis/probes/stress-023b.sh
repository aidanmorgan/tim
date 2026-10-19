#!/bin/zsh
# Diagnosis only: run cat-023b under synthetic CPU load until it fails, to capture the clock-recovery fault record.
cd /Users/aidan/dev/personal/tim
L=/Users/aidan/.npm/_npx/fd45a72a545557e9/node_modules/tsx/dist/loader.mjs
E=_bmad-output/implementation-artifacts/evidence-9-4b/diagnosis
LOAD=${1:-12}
RUNS=${2:-4}
TAG=${3:-stress}
pids=()
for i in $(seq 1 $LOAD); do yes > /dev/null & pids+=($!); done
trap 'kill $pids 2>/dev/null' EXIT
for i in $(seq 1 $RUNS); do
  log=$E/$TAG-cat-023b-run$i.log
  node --import $L --test --test-concurrency=1 --test-timeout=150000 tools/e2e/cat-023b.test.ts > $log 2>&1
  r="$(grep -E '^ℹ (pass|fail)' $log | tr '\n' ' ')"
  echo "run $i (load $LOAD): $r transport=$(grep -c TRANSPORT_FAILURE $log) recovery=$(grep -c CLOCK_RECOVERY_FAULT $log)"
  case "$r" in *"fail 0"*) ;; *) break;; esac
done
