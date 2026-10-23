#!/usr/bin/env bash
# Merged-tree verification (main fc7f7cac merged into 10.1), from the story-10-1 worktree root: one Debug build, the full solution
# test reusing it (--blame-crash), the routine gate subset and the walk test with their reports, the CI tools step, the Release
# worker build, the whitespace checks and the AppBundle absence check. Raw logs in round6/.
set -u
E=_bmad-output/implementation-artifacts/evidence-10-1/round6
mkdir -p "$E"
{ git rev-parse HEAD; git status --short; git diff --stat; git diff | shasum -a 256; } > "$E/candidate-identity.txt"
dotnet build CuriousContraptions.slnx -c Debug > "$E/build-debug.log" 2>&1; echo "build-debug exit $?" | tee "$E/exit-codes.txt"
start=$(date +%s)
dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore --blame-crash > "$E/test-slnx.log" 2>&1; echo "test-slnx exit $? ($(( $(date +%s) - start )) s)" | tee -a "$E/exit-codes.txt"
dotnet test CuriousContraptions.tests -c Debug --no-build --no-restore \
  --filter "FullyQualifiedName~FreshSeedDropsGainNoMoreEnergy|FullyQualifiedName~ARimAimedWalk" \
  --logger "console;verbosity=detailed" > "$E/gate-and-walk.log" 2>&1; echo "gate-and-walk exit $?" | tee -a "$E/exit-codes.txt"
bash _bmad-output/implementation-artifacts/evidence-10-1/ci-tool-step.sh > "$E/ci-tool-step.log" 2>&1; echo "ci-tool-step exit $?" | tee -a "$E/exit-codes.txt"
dotnet build CuriousContraptions.Simulation -c Release > "$E/worker-release.log" 2>&1; echo "worker-release exit $?" | tee -a "$E/exit-codes.txt"
{ git diff --check; echo "worktree exit $?"; git diff --check fc7f7cac HEAD; echo "merge-vs-main exit $?"; } > "$E/diff-check.log" 2>&1
{ find . -type d -name AppBundle -not -path "./node_modules/*"; echo "appbundle dirs listed above (none expected)"; } > "$E/appbundle-check.txt" 2>&1
echo "checks done" | tee -a "$E/exit-codes.txt"
grep -E "Warning\(s\)|Error\(s\)" "$E/build-debug.log" "$E/worker-release.log"
grep -E "Passed!|Failed!" "$E/test-slnx.log" "$E/ci-tool-step.log" "$E/gate-and-walk.log"
grep -E "over .* mJ" "$E/gate-and-walk.log" | sed 's/^.*\]//' | sort -u
cat "$E/diff-check.log" "$E/appbundle-check.txt"
