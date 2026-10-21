#!/bin/zsh
# Story 9A.2 AC2 mutant: the TS decoder reads the former unframed root. Run from the worktree root.
out=$1
S=/private/tmp/claude-501/-Users-aidan-dev-personal-tim/49d80295-44c4-4940-bc16-8455cdf5d09d/scratchpad
cp tools/e2e/workshop-wire.ts "$S/wire.ts.backup"
perl -0pi -e 's/    bb\.setPosition\(flatbuffers\.SIZE_PREFIX_LENGTH\);\n    if \(!bb\.__has_identifier\(identifier\)\) throw new Error\(`not a \$\{identifier\} buffer`\);\n/    \/\/ MUTANT: the former unframed read (root at position 0, no identifier)\n/' tools/e2e/workshop-wire.ts
{
  echo "== M17 (AC2) TS decoder reads the former unframed root (no size prefix, no identifier check)"
  diff -U0 "$S/wire.ts.backup" tools/e2e/workshop-wire.ts | grep -E '^[-+][^-+]'
  timeout 120 node --import /Users/aidan/.npm/_npx/e6b261a129b06bcc/node_modules/tsx/dist/loader.mjs --test tools/e2e/unit/run-reset-snapshot.test.ts 2>&1 | grep -E "^ℹ (pass|fail)|^not ok" | head -8
  cp "$S/wire.ts.backup" tools/e2e/workshop-wire.ts
  echo "== control (restored)"
  timeout 120 node --import /Users/aidan/.npm/_npx/e6b261a129b06bcc/node_modules/tsx/dist/loader.mjs --test tools/e2e/unit/run-reset-snapshot.test.ts 2>&1 | grep -E "^ℹ (pass|fail)" | head -4
} > "$out"
