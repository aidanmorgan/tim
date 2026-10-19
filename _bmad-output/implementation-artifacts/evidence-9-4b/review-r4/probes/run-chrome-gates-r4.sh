#!/bin/zsh
# Review round 4 serial Chrome checks (Murdoch-2, Story 9.4b candidate 5). One Chrome at a time against the :8060 preview.
# Owner decision 19 Oct 2026: no reviewer cumulative run (Amelia's two are reused after verification); impact-chosen suites:
# clock-master, renewal-rule probes (long stall; stall proof and control under load-8), cat-017 once, three round-2 probes.
set -u
T=/Users/aidan/dev/personal/tim
S=/private/tmp/claude-501/-Users-aidan-dev-personal-tim/49d80295-44c4-4940-bc16-8455cdf5d09d/scratchpad/murdoch2-9-4b
L=/Users/aidan/.npm/_npx/fd45a72a545557e9/node_modules/tsx/dist/loader.mjs
E=$T/_bmad-output/implementation-artifacts/evidence-9-4b/review-r4
mkdir -p $E/derivations $E/stall-load8
sum=$E/chrome-gates-summary.txt; : > $sum
served() { for f in simulation/_framework/CuriousContraptions.Simulation.wasm simulation/worker.js simulation/clock-master.js workshop-client.js clock-ring.js animation/worker.js animation/_framework/CuriousContraptions.Animation.Worker.wasm godot.pck; do echo -n "$(curl -s http://127.0.0.1:8060/$f | shasum -a 256 | cut -c1-12) "; done; }
tapsum() { echo "$(grep -E '^# (tests|pass|fail)' $1 | tr '\n' ' ') notok=$(grep -cE '^\s*not ok ' $1)"; }
counts() { echo "closeFindings=$(grep -c 'Session close finding' $1) recalRaw=$(grep -c '\[E2E-CONSOLE log\] CCGPU_CLOCK_RECALIBRATED' $1) renewedLateRaw=$(grep -c '\[E2E-CONSOLE log\] CCGPU_CLOCK_RENEWED_LATE' $1) wide=$(grep -c 'CCGPU_CLOCK_OFFSET_WIDE' $1) unresponsive=$(grep -c SimulationUnresponsive $1) rejected=$(grep -c CCGPU_CLOCK_REPLY_REJECTED $1)"; }
echo "served at start: $(served) $(date -u +%T)" >> $sum
cd $T
for n in clock-master cat-017; do
  CLOCK_EVIDENCE_SCREENSHOT=$E/$n-unresponsive.png CLOCK_DERIVATION_LOG=$E/derivations/$n.jsonl node --import $L --test --test-concurrency=1 --test-timeout=150000 \
    --test-reporter=tap --test-reporter-destination=$E/$n.tap tools/e2e/$n.test.ts > $E/$n.log 2>&1
  echo "$n exit=$? $(tapsum $E/$n.tap) $(counts $E/$n.tap)" >> $sum
done
cd $S/runner3
for p in murdoch-long-stall murdoch-clock-stress murdoch-023b-save-load-cycles murdoch-bumper-ledger; do
  CLOCK_DERIVATION_LOG=$E/derivations/$p.jsonl node --import $L --test --test-concurrency=1 --test-timeout=900000 \
    --test-reporter=spec --test-reporter-destination=stdout --test-reporter=tap --test-reporter-destination=$E/$p.tap tools/e2e/$p.test.ts > $E/$p.log 2>&1
  echo "$p exit=$? $(tapsum $E/$p.tap)" >> $sum
done
cd $T
# Renewal budget under load: the clock-master stall proof and its control, 5 times under 8 busy loops.
pids=(); for i in 1 2 3 4 5 6 7 8; do yes > /dev/null & pids+=($!); done
for i in 1 2 3 4 5; do
  CLOCK_EVIDENCE_SCREENSHOT=$E/stall-load8/unresponsive-$i.png node --import $L --test --test-concurrency=1 --test-timeout=300000 \
    --test-name-pattern='Browser stall after ready|same stall with the Browser clock' \
    --test-reporter=tap --test-reporter-destination=$E/stall-load8/run$i.tap tools/e2e/clock-master.test.ts > $E/stall-load8/run$i.log 2>&1
  echo "stall-load8 run $i exit=$? $(tapsum $E/stall-load8/run$i.tap) exchanges=$(grep -o 'exchanges[^0-9]*[0-9]*' $E/stall-load8/run$i.tap | grep -o '[0-9]*$' | sort -u | tr '\n' ',')" >> $sum
done
kill $pids; wait 2>/dev/null
echo "served at end: $(served) $(date -u +%T)" >> $sum
echo DONE >> $sum
