#!/bin/zsh
# DoD gate: every tools/e2e/*.test.ts suite in ONE serial Chrome run on the production preview, repeated back to back.
cd /Users/aidan/dev/personal/tim
L=/Users/aidan/.npm/_npx/fd45a72a545557e9/node_modules/tsx/dist/loader.mjs
E=_bmad-output/implementation-artifacts/evidence-9-4b/final
RUNS=${1:-2}
for i in $(seq 1 30); do curl -s -o /dev/null http://127.0.0.1:8060/ && break; sleep 1; done
for r in $(seq 1 $RUNS); do
  log=$E/e2e-cumulative-run$r.log
  date -u +%FT%TZ > $E/e2e-cumulative-run$r.started
  ls tools/e2e/*.test.ts > $E/e2e-cumulative-run$r.suites.txt
  node --import $L --test --test-concurrency=1 --test-timeout=150000 tools/e2e/*.test.ts > $log 2>&1
  date -u +%FT%TZ > $E/e2e-cumulative-run$r.finished
  echo "cumulative run $r: $(grep -E '^ℹ (tests|suites|pass|fail|cancelled|skipped|duration_ms)' $log | tr '\n' ' ')"
done
