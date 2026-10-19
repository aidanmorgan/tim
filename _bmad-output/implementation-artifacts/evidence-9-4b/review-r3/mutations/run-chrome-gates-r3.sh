#!/bin/zsh
# Review round 3 serial Chrome gates (Murdoch-2, Story 9.4b candidate 4). One Chrome at a time against the :8060 preview.
set -u
T=/Users/aidan/dev/personal/tim
S=/private/tmp/claude-501/-Users-aidan-dev-personal-tim/49d80295-44c4-4940-bc16-8455cdf5d09d/scratchpad/murdoch2-9-4b
L=/Users/aidan/.npm/_npx/fd45a72a545557e9/node_modules/tsx/dist/loader.mjs
E=$T/_bmad-output/implementation-artifacts/evidence-9-4b/review-r3
mkdir -p $E/derivations $E/cumulative-run1 $E/cumulative-run2
sum=$E/chrome-gates-summary.txt; : > $sum
served() { for f in simulation/_framework/CuriousContraptions.Simulation.wasm simulation/worker.js simulation/clock-master.js workshop-client.js clock-ring.js animation/worker.js animation/_framework/CuriousContraptions.Animation.Worker.wasm godot.pck; do echo -n "$(curl -s http://127.0.0.1:8060/$f | shasum -a 256 | cut -c1-12) "; done; }
tapsum() { echo "$(grep -E '^# (tests|pass|fail)' $1 | tr '\n' ' ') notok=$(grep -cE '^\s*not ok ' $1)"; }
echo "served at start: $(served)" >> $sum
cd $S/runner3
for p in murdoch-clock-stress murdoch-023b-save-load-cycles murdoch-bumper-ledger murdoch-replay-can-fail; do
  CLOCK_DERIVATION_LOG=$E/derivations/$p.jsonl node --import $L --test --test-concurrency=1 --test-timeout=900000 \
    --test-reporter=spec --test-reporter-destination=stdout --test-reporter=tap --test-reporter-destination=$E/$p.tap tools/e2e/$p.test.ts > $E/$p.log 2>&1
  echo "$p exit=$? $(tapsum $E/$p.tap)" >> $sum
done
cd $T
for r in 1 2; do
  d=$E/cumulative-run$r; date -u +%FT%TZ > $d/started
  for file in tools/e2e/*.test.ts; do
    n=$(basename $file .test.ts)
    CLOCK_DERIVATION_LOG=$d/$n.derivations.jsonl node --import $L --test --test-concurrency=1 --test-timeout=150000 \
      --test-reporter=tap --test-reporter-destination=$d/$n.tap $file > $d/$n.log 2>&1
    code=$?
    echo "$n exit=$code $(tapsum $d/$n.tap) closeFindings=$(grep -c 'Session close finding' $d/$n.tap) recal=$(grep -c '\[E2E-CONSOLE log\] CCGPU_CLOCK_RECALIBRATED' $d/$n.tap) renewedLate=$(grep -c '\[E2E-CONSOLE log\] CCGPU_CLOCK_RENEWED_LATE' $d/$n.tap) wide=$(grep -c 'CCGPU_CLOCK_OFFSET_WIDE' $d/$n.tap) unresponsive=$(grep -c SimulationUnresponsive $d/$n.tap) rejected=$(grep -c CCGPU_CLOCK_REPLY_REJECTED $d/$n.tap)" >> $d/per-file.txt
  done
  date -u +%FT%TZ > $d/finished
  echo "cumulative run $r: files=$(wc -l < $d/per-file.txt | tr -d ' ') nonzero=$(grep -vc 'exit=0' $d/per-file.txt)" >> $sum
done
echo "served at end: $(served)" >> $sum
echo DONE >> $sum
