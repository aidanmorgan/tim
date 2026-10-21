#!/bin/zsh
# Story 9A.3 Tier 1 Node harnesses: every tools/*.test.mjs file run alone with --experimental-vm-modules, plus the e2e unit tests.
# Run from the worktree root. Prints one line per file with its pass and fail counts and exit code.
echo "node $(node --version), HEAD $(git rev-parse --short HEAD), status digest $(git status --porcelain | shasum -a 256 | cut -c1-12)"
tsx=$(dirname $(grep -l '"version": "4.23.15"' ~/.npm/_npx/*/node_modules/tsx/package.json | head -1))/dist/loader.mjs
report() {
  echo "$1 exit=$2 $(echo "$3" | grep -E '^ℹ (pass|fail) ' | tr '\n' ' ')"
  if [ $2 -ne 0 ]; then echo "$3" | grep -E '^not ok|✖|Error' | head -20; failed=1; fi
}
failed=0
for f in tools/*.test.mjs; do
  out=$(node --experimental-vm-modules --test "$f" 2>&1); report "$f" $? "$out"
done
# The browserless e2e unit checks run under tsx 4.23.15 (tools/e2e/README.md).
for f in tools/e2e/unit/*.test.ts; do
  out=$(node --import "$tsx" --test "$f" 2>&1); report "$f" $? "$out"
done
exit $failed
