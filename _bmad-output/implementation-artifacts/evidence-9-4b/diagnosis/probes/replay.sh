#!/bin/zsh
# Diagnosis only: replay the unmodified cat-023b suite RUNS times under LOAD busy processes; stop at the first failure.
S=/private/tmp/claude-501/-Users-aidan-dev-personal-tim/49d80295-44c4-4940-bc16-8455cdf5d09d/scratchpad
TAG=$1; LOAD=${2:-0}; RUNS=${3:-5}
cd /Users/aidan/dev/personal/tim
for i in $(seq 1 30); do curl -s -o /dev/null http://127.0.0.1:8060/ && break; sleep 1; done
pids=()
for i in $(seq 1 $LOAD); do yes > /dev/null & pids+=($!); done
trap 'kill $pids 2>/dev/null' EXIT
for r in $(seq 1 $RUNS); do
  out=$S/$TAG-run$r.console.log; : > $out
  REPLAY_OUT=$out node --import /Users/aidan/.npm/_npx/fd45a72a545557e9/node_modules/tsx/dist/loader.mjs --test --test-concurrency=1 --test-timeout=150000 $S/replay-023b.test.mts > $S/$TAG-run$r.node.log 2>&1
  res="$(grep -E '^ℹ (pass|fail)' $S/$TAG-run$r.node.log | tr '\n' ' ')"
  echo "$TAG run $r (load $LOAD): $res goals=$(grep -c CCGOAL_SOLVED $out) recovery=$(grep -c CLOCK_RECOVERY_FAULT $out) debt=$(grep -c DEBT_FAULT $out) tickFailed=$(grep -c TICK_FAILED $out)"
  [ "${STOP_ON_FAIL:-1}" = 1 ] && case "$res" in *"fail 0"*) ;; *) break;; esac
done
