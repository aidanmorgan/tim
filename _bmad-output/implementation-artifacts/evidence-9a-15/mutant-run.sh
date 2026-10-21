#!/bin/zsh
# Story 9A.15 mutant runner; run from the worktree root after a mutant is applied with the Edit tool (and restored afterwards
# with git checkout). Usage: mutant-run.sh <label> <test project> <test filter>. Appends the build and test result to mutants.txt.
# A mutant that does not compile is reported as such and its tests are not run (a stale binary would pass).
out=_bmad-output/implementation-artifacts/evidence-9a-15/mutants.txt
echo "== $1" >> $out
git diff --stat -- . ':(exclude)_bmad-output' | sed 's/^/diff: /' >> $out
build=$(dotnet build "$2" 2>&1)
if echo "$build" | grep -q ": error "; then
  echo "$build" | grep -E ": error " | sed -E 's#^.*/agent-ae9d1607d961be684/##; s# \[[^]]*\]$##' | sort -u | head -3 | sed 's/^/build error: /' >> $out
else
  echo "build: succeeded" >> $out
  dotnet test "$2" --no-build --filter "$3" 2>&1 | grep -E "^\s+Failed [A-Za-z]|Passed!|Failed!" | sed -E 's/^\s+//; s/ \[[^]]*\]$//' | sort -u | head -12 >> $out
fi
echo >> $out
