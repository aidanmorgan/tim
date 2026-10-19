#!/bin/zsh
# DoD gate: cat-023b N consecutive serial runs on the production preview, every result recorded (no retry).
cd /Users/aidan/dev/personal/tim
L=/Users/aidan/.npm/_npx/fd45a72a545557e9/node_modules/tsx/dist/loader.mjs
E=_bmad-output/implementation-artifacts/evidence-9-4b/final
N=${1:-5}; TAG=${2:-cat-023b}
for i in $(seq 1 30); do curl -s -o /dev/null http://127.0.0.1:8060/ && break; sleep 1; done
for i in $(seq 1 $N); do
  log=$E/e2e-$TAG-consecutive$i.log
  node --import $L --test --test-concurrency=1 --test-timeout=150000 tools/e2e/cat-023b.test.ts > $log 2>&1
  echo "cat-023b consecutive run $i: $(grep -E '^ℹ (tests|pass|fail|skipped)' $log | tr '\n' ' ') transportFailures=$(grep -c TRANSPORT_FAILURE $log)"
done
