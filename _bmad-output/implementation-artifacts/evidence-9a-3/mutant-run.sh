#!/bin/zsh
# Story 9A.3 mutant runner, run from the worktree root after one mutant is applied with the Edit tool (it is restored afterwards with
# git checkout). Usage: mutant-run.sh <label> <test filter> [node harness ...]. Appends the diff, the build result, the failing
# native tests and each named Node harness's result to mutants.txt. A mutant that does not compile is reported as such and no test
# runs (a stale binary would pass).
out=_bmad-output/implementation-artifacts/evidence-9a-3/mutants.txt
label=$1; filter=$2; shift 2
echo "== $label" >> $out
git diff -U0 -- . ':(exclude)_bmad-output' | grep -E '^[-+][^-+]' | sed 's/^/diff: /' | head -12 >> $out
build=$(dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj -nologo 2>&1)
if echo "$build" | grep -q ": error "; then
  echo "$build" | grep -E ": error " | sed -E 's#^.*/agent-a71abbddd0caa4be4/##; s# \[[^]]*\]$##' | sort -u | head -3 | sed 's/^/build error: /' >> $out
else
  echo "build: succeeded" >> $out
  dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --filter "$filter" 2>&1 \
    | grep -E "^\s+Failed [A-Za-z]|Passed!|Failed!" | sed -E 's/^\s+//; s/ \[[^]]*\]$//' | sort -u | head -12 >> $out
fi
for harness in "$@"; do
  result=$(node --experimental-vm-modules --test "$harness" 2>&1); code=$?
  echo "node $harness exit=$code $(echo "$result" | grep -E '^ℹ (pass|fail) ' | tr '\n' ' ')" >> $out
  [ $code -ne 0 ] && echo "$result" | grep -E "AssertionError|Expected|actual|message:" | head -4 | sed 's/^/  /' >> $out
done
echo >> $out
