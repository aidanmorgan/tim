#!/bin/zsh
# Diagnosis only: run the cat-023b probe N times under synthetic load and summarise faults.
S=/private/tmp/claude-501/-Users-aidan-dev-personal-tim/49d80295-44c4-4940-bc16-8455cdf5d09d/scratchpad
TAG=$1; LOAD=${2:-14}; RUNS=${3:-3}; CYCLES=${4:-10}
cd /Users/aidan/dev/personal/tim
for i in $(seq 1 30); do curl -s -o /dev/null http://127.0.0.1:8060/ && break; sleep 1; done
pids=()
for i in $(seq 1 $LOAD); do yes > /dev/null & pids+=($!); done
trap 'kill $pids 2>/dev/null' EXIT
for r in $(seq 1 $RUNS); do
  out=$S/$TAG-run$r.log
  : > $out
  PROBE_OUT=$out PROBE_RELOAD=1 PROBE_CYCLES=$CYCLES node --import /Users/aidan/.npm/_npx/fd45a72a545557e9/node_modules/tsx/dist/loader.mjs --test --test-timeout=900000 $S/probe-023b.test.ts > $S/$TAG-run$r-node.log 2>&1
  echo "$TAG run $r: legs=$(grep -c ' solved=' $out) solved=$(grep -c 'solved=true' $out) recoveryFaults=$(grep -c CLOCK_RECOVERY_FAULT $out) maxLag=$(grep -o 'CCDIAG_LAG [0-9.]*' $out | awk '{print $2}' | sort -n | tail -1) lagsOver20=$(grep -o 'CCDIAG_LAG [0-9.]*' $out | awk '$2>20' | wc -l | tr -d ' ')"
done
