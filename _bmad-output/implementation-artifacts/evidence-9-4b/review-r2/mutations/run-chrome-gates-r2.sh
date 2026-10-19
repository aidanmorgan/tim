#!/bin/zsh
# Review round 2 serial Chrome gates (Murdoch-2, Story 9.4b candidate 3). One Chrome at a time against Amelia's :8060 preview.
set -u
T=/Users/aidan/dev/personal/tim
S=/private/tmp/claude-501/-Users-aidan-dev-personal-tim/49d80295-44c4-4940-bc16-8455cdf5d09d/scratchpad/murdoch2-9-4b
L=/Users/aidan/.npm/_npx/fd45a72a545557e9/node_modules/tsx/dist/loader.mjs
E=$T/_bmad-output/implementation-artifacts/evidence-9-4b/review-r2
mkdir -p $E/derivations $E/cumulative-run1 $E/cumulative-run2 $E/offset
sum=$E/chrome-gates-summary.txt; : > $sum
served() { for f in simulation/_framework/CuriousContraptions.Simulation.wasm simulation/worker.js simulation/clock-master.js workshop-client.js clock-ring.js animation/worker.js animation/_framework/CuriousContraptions.Animation.Worker.wasm; do echo -n "$(curl -s http://127.0.0.1:8060/$f | shasum -a 256 | cut -c1-12) "; done; }
tapsum() { echo "tests=$(grep -cE '^(not )?ok ' $1) notok=$(grep -cE '^\s*not ok ' $1) $(grep -E '^# (pass|fail)' $1 | tr '\n' ' ')"; }
yeses=()
load() { for j in $(seq 1 $1); do yes > /dev/null & yeses+=($!); done; }
unload() { [ ${#yeses[@]} -gt 0 ] && kill $yeses 2>/dev/null; wait $yeses 2>/dev/null; yeses=(); }
trap unload EXIT
echo "served at start: $(served)" >> $sum

# 1. Reviewer probes (runner3: candidate-3 driver, wire, console and close-findings copies).
cd $S/runner3
for p in murdoch-clock-stress murdoch-023b-save-load-cycles murdoch-bumper-ledger; do
  CLOCK_DERIVATION_LOG=$E/derivations/$p.jsonl node --import $L --test --test-concurrency=1 --test-timeout=900000 \
    --test-reporter=spec --test-reporter-destination=stdout --test-reporter=tap --test-reporter-destination=$E/$p.tap tools/e2e/$p.test.ts > $E/$p.log 2>&1
  echo "$p exit=$? $(tapsum $E/$p.tap)" >> $sum
done
# 2. Startup offset under host load (30 sessions each at load 0, 8 and 32 x yes on 16 logical CPUs).
for lv in 0 8 32; do
  unload; [ $lv -gt 0 ] && load $lv
  MURDOCH_OFFSET_SESSIONS=30 MURDOCH_OFFSET_LABEL=load$lv MURDOCH_OFFSET_OUT=$E/offset/load$lv.jsonl \
    node --import $L --test --test-concurrency=1 --test-reporter=tap --test-reporter-destination=$E/offset/load$lv.tap tools/e2e/murdoch-offset-sessions.test.ts > $E/offset/load$lv.log 2>&1
  echo "offset load$lv exit=$? $(grep MURDOCH_OFFSET $E/offset/load$lv.log)" >> $sum
done
unload

# 3. Repeat gates in the candidate's own tools/e2e.
cd $T
for i in 1 2 3 4 5; do
  f=$E/clock-master-run$i; CLOCK_DERIVATION_LOG=$f.derivations.jsonl node --import $L --test --test-concurrency=1 --test-timeout=150000 \
    --test-reporter=tap --test-reporter-destination=$f.tap tools/e2e/clock-master.test.ts > $f.log 2>&1
  echo "clock-master run $i exit=$? $(tapsum $f.tap)" >> $sum
done
for i in 1 2 3 4 5; do
  f=$E/cat-005-run$i; CLOCK_DERIVATION_LOG=$f.derivations.jsonl node --import $L --test --test-concurrency=1 --test-timeout=150000 \
    --test-reporter=tap --test-reporter-destination=$f.tap tools/e2e/cat-005.test.ts > $f.log 2>&1
  echo "cat-005 run $i exit=$? $(tapsum $f.tap)" >> $sum
done
for lv in 0 8; do
  unload; [ $lv -gt 0 ] && load $lv
  for i in 1 2 3 4 5; do
    f=$E/cat-023b-load$lv-run$i; CLOCK_DERIVATION_LOG=$f.derivations.jsonl node --import $L --test --test-concurrency=1 --test-timeout=150000 \
      --test-reporter=tap --test-reporter-destination=$f.tap tools/e2e/cat-023b.test.ts > $f.log 2>&1
    echo "cat-023b load$lv run $i exit=$? $(tapsum $f.tap) recal=$(grep -c CCGPU_CLOCK_RECALIBRATED $f.log) unresponsive=$(grep -c SimulationUnresponsive $f.log)" >> $sum
  done
done
unload

# 4. Cumulative serial, twice back to back: one process per file, per-file TAP, exit code and derivation log.
for r in 1 2; do
  d=$E/cumulative-run$r; date -u +%FT%TZ > $d/started
  for file in tools/e2e/*.test.ts; do
    n=$(basename $file .test.ts)
    CLOCK_DERIVATION_LOG=$d/$n.derivations.jsonl node --import $L --test --test-concurrency=1 --test-timeout=150000 \
      --test-reporter=tap --test-reporter-destination=$d/$n.tap $file > $d/$n.log 2>&1
    code=$?
    echo "$n exit=$code $(tapsum $d/$n.tap) closeFindings=$(grep -c 'Session close finding' $d/$n.log) unresponsive=$(grep -c SimulationUnresponsive $d/$n.log) rejected=$(grep -c CCGPU_CLOCK_REPLY_REJECTED $d/$n.log) recal=$(grep -c CCGPU_CLOCK_RECALIBRATED $d/$n.log)" >> $d/per-file.txt
  done
  date -u +%FT%TZ > $d/finished
  echo "cumulative run $r: files=$(wc -l < $d/per-file.txt | tr -d ' ') nonzero=$(grep -vc 'exit=0' $d/per-file.txt) tests=$(cat $d/*.tap | grep -cE '^(not )?ok |^    (not )?ok ') notok=$(cat $d/*.tap | grep -cE '^\s*not ok ')" >> $sum
done
echo "served at end: $(served)" >> $sum
echo DONE >> $sum
