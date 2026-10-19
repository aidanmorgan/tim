#!/bin/zsh
# Review-only serial Chrome gates (Murdoch-2, Story 9.4b terminal review). One Chrome at a time against :8060.
set -u
cd /Users/aidan/dev/personal/tim
L=/Users/aidan/.npm/_npx/fd45a72a545557e9/node_modules/tsx/dist/loader.mjs
E=_bmad-output/implementation-artifacts/evidence-9-4b/review
summary=$E/chrome-gates-summary.txt
: > $summary
count() { echo "transport=$(grep -c CCGPU_TRANSPORT_FAILURE $1) unresponsive=$(grep -c SimulationUnresponsive $1) rejected=$(grep -c CCGPU_CLOCK_REPLY_REJECTED $1) recal=$(grep -c 'CCGPU_CLOCK_RECALIBRATED' $1)"; }
result() { grep -E '^ℹ (tests|pass|fail|cancelled|skipped)' $1 | tr '\n' ' '; }
served() { for f in simulation/_framework/CuriousContraptions.Simulation.wasm simulation/worker.js simulation/clock-master.js workshop-client.js; do echo -n "$(curl -s http://127.0.0.1:8060/$f | shasum -a 256 | cut -c1-12) "; done; }
echo "served at start: $(served)" >> $summary

for i in 1 2 3 4 5; do
  log=$E/e2e-cat-023b-load0-run$i.log
  { date -u +%FT%TZ; node --import $L --test --test-concurrency=1 --test-timeout=150000 tools/e2e/cat-023b.test.ts 2>&1; echo "exit=$?"; date -u +%FT%TZ; } > $log 2>&1
  echo "cat-023b load0 run $i: $(result $log) $(count $log)" >> $summary
done

pids=()
for j in $(seq 1 8); do yes > /dev/null & pids+=($!); done
for i in 1 2 3 4 5; do
  log=$E/e2e-cat-023b-load8-run$i.log
  { date -u +%FT%TZ; echo "load: 8 x yes"; node --import $L --test --test-concurrency=1 --test-timeout=150000 tools/e2e/cat-023b.test.ts 2>&1; echo "exit=$?"; date -u +%FT%TZ; } > $log 2>&1
  echo "cat-023b load8 run $i: $(result $log) $(count $log)" >> $summary
done
# Harder than the implementer's bar: 2x the 16 logical CPUs.
for j in $(seq 1 24); do yes > /dev/null & pids+=($!); done
for i in 1 2; do
  log=$E/e2e-cat-023b-load32-run$i.log
  { date -u +%FT%TZ; echo "load: 32 x yes (16 logical CPUs)"; node --import $L --test --test-concurrency=1 --test-timeout=150000 tools/e2e/cat-023b.test.ts 2>&1; echo "exit=$?"; date -u +%FT%TZ; } > $log 2>&1
  echo "cat-023b load32 run $i: $(result $log) $(count $log)" >> $summary
done
kill $pids 2>/dev/null; wait $pids 2>/dev/null

for i in 1 2 3 4 5; do
  log=$E/e2e-cat-005-run$i.log
  { date -u +%FT%TZ; node --import $L --test --test-concurrency=1 --test-timeout=150000 tools/e2e/cat-005.test.ts 2>&1; echo "exit=$?"; date -u +%FT%TZ; } > $log 2>&1
  echo "cat-005 run $i: $(result $log) $(count $log)" >> $summary
done

for r in 1 2; do
  log=$E/e2e-cumulative-run$r.log
  { date -u +%FT%TZ; ls tools/e2e/*.test.ts; node --import $L --test --test-concurrency=1 --test-timeout=150000 \
      --test-reporter=spec --test-reporter-destination=stdout --test-reporter=tap --test-reporter-destination=$E/e2e-cumulative-run$r.tap \
      tools/e2e/*.test.ts 2>&1; echo "exit=$?"; date -u +%FT%TZ; } > $log 2>&1
  echo "cumulative run $r: $(result $log) $(count $log)" >> $summary
done
echo "served at end: $(served)" >> $summary
echo DONE >> $summary
