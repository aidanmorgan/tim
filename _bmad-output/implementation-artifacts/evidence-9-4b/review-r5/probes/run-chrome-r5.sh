#!/bin/zsh
# Review round 5 Chrome checks (Murdoch-2, Story 9.4b candidate 6): clock-master once, long-stall probe, cat-017 once.
set -u
T=/Users/aidan/dev/personal/tim
S=/private/tmp/claude-501/-Users-aidan-dev-personal-tim/49d80295-44c4-4940-bc16-8455cdf5d09d/scratchpad/murdoch2-9-4b
L=/Users/aidan/.npm/_npx/fd45a72a545557e9/node_modules/tsx/dist/loader.mjs
E=$T/_bmad-output/implementation-artifacts/evidence-9-4b/review-r5
mkdir -p $E/derivations
sum=$E/chrome-summary.txt; : > $sum
served() { for f in simulation/_framework/CuriousContraptions.Simulation.wasm simulation/worker.js simulation/clock-master.js workshop-client.js clock-ring.js animation/worker.js animation/_framework/CuriousContraptions.Animation.Worker.wasm godot.pck; do echo -n "$(curl -s http://127.0.0.1:8060/$f | shasum -a 256 | cut -c1-12) "; done; }
tapsum() { echo "$(grep -E '^# (tests|pass|fail)' $1 | tr '\n' ' ') notok=$(grep -cE '^\s*not ok ' $1) closeFindings=$(grep -c 'Session close finding' $1) recalLines=$(grep -c 'CLOCK_RECALIBRATED' $1) renewLines=$(grep -c 'CCGPU_CLOCK_RENEWED_LATE' $1) rejected=$(grep -c CCGPU_CLOCK_REPLY_REJECTED $1)"; }
echo "served at start: $(served) $(date -u +%T)" >> $sum
cd $T
for n in clock-master cat-017; do
  CLOCK_EVIDENCE_SCREENSHOT=$E/$n-unresponsive.png CLOCK_DERIVATION_LOG=$E/derivations/$n.jsonl node --import $L --test --test-concurrency=1 --test-timeout=150000 \
    --test-reporter=tap --test-reporter-destination=$E/$n.tap tools/e2e/$n.test.ts > $E/$n.log 2>&1
  echo "$n exit=$? $(tapsum $E/$n.tap)" >> $sum
done
cd $S/runner3
CLOCK_DERIVATION_LOG=$E/derivations/murdoch-long-stall.jsonl node --import $L --test --test-concurrency=1 --test-timeout=900000 \
  --test-reporter=spec --test-reporter-destination=stdout --test-reporter=tap --test-reporter-destination=$E/murdoch-long-stall.tap tools/e2e/murdoch-long-stall.test.ts > $E/murdoch-long-stall.log 2>&1
echo "murdoch-long-stall exit=$? $(tapsum $E/murdoch-long-stall.tap)" >> $sum
echo "served at end: $(served) $(date -u +%T)" >> $sum
echo DONE >> $sum
