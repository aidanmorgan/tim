#!/bin/zsh
# Round 5 (Murdoch-2, Story 9.4b candidate 6): isolated-copy baseline, R4-F1 boundary mutants, then main-checkout gates.
set -u
S=/private/tmp/claude-501/-Users-aidan-dev-personal-tim/49d80295-44c4-4940-bc16-8455cdf5d09d/scratchpad/murdoch2-9-4b
cd $S/mut5
dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj -v q > $S/mut6-build.log 2>&1
dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --logger "trx;LogFileName=baseline6.trx" --results-directory $S/mut6-results > $S/mut6-baseline.log 2>&1
grep -o 'testName="[^"]*" [^>]*outcome="[A-Za-z]*"' $S/mut6-results/baseline6.trx | sed -E 's/testName="([^"]*)".*outcome="([A-Za-z]*)"/\2 \1/' | sort > $S/mut5-baseline-outcomes.txt
F=engine/gpu/WorkshopClockMapping.cs
r() { zsh $S/run-mutation-r4.sh "$1" tests $F "$2"; }
{
r R5-01-old-bound-dropped 's/TryRetained\(reply.RequesterReceived, out retained\) && WithinMappedWidth\(retained\) &&/TryRetained(reply.RequesterReceived, out retained) \&\&/'
r R5-02-bound-one-ns-loose 's/widened.Width \+ 2 \* TimestampUncertainty.Maximum <= MaximumMappedWidth;/widened.Width + 2 * TimestampUncertainty.Maximum <= MaximumMappedWidth + 1;/'
r R5-03-bound-strict 's/widened.Width \+ 2 \* TimestampUncertainty.Maximum <= MaximumMappedWidth;/widened.Width + 2 * TimestampUncertainty.Maximum < MaximumMappedWidth;/'
r R5-04-old-bound-without-sample-allowance 's/WithinMappedWidth\(retained\) &&/retained.Width <= MaximumMappedWidth \&\&/'
r R5-05-old-bound-over-refresh-horizon 's/WithinMappedWidth\(retained\) &&/WithinMappedWidth(Expand(retained, RefreshSpacing)) \&\&/'
r R5-06-budget-inherited-from-recovery 's/State = ClockMappingState.Renewing;\n(\s+)ClearRecovery\(\);/State = ClockMappingState.Renewing;/'
} > $S/mutations-r5-batch.txt 2>&1
cd /Users/aidan/dev/personal/tim
R=_bmad-output/implementation-artifacts/evidence-9-4b/review-r5
{ dotnet build CuriousContraptions.slnx --no-incremental 2>&1 | grep -E "Warning\(s\)|Error\(s\)"; echo "exit=$?"; } > $R/build-slnx.log 2>&1
{ dotnet test CuriousContraptions.slnx 2>&1 | grep -E "^(Passed|Failed)!|\[FAIL\]"; } > $R/dotnet-test.log 2>&1
{ node --experimental-vm-modules --test tools/*.test.mjs 2>&1 | grep -E "^ℹ (tests|pass|fail) "; } > $R/node-harnesses.log 2>&1
echo R5-NATIVE-DONE >> $S/mutations-r5-batch.txt
