#!/usr/bin/env bash
# Fix round 2 frozen-candidate verification (from the story-10-1 worktree root): one Debug build, the full solution test reusing it,
# the CI tools step, the Release worker build and the whitespace checks (tracked diff and both untracked files). Raw logs in round3/.
set -u
E=_bmad-output/implementation-artifacts/evidence-10-1/round3
mkdir -p "$E"
{ git rev-parse HEAD; git status --short; git diff --stat; git diff | shasum -a 256
  shasum -a 256 CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs _bmad-output/implementation-artifacts/spec-10-1-solid-cylinder.md; } > "$E/candidate-identity.txt"
dotnet build CuriousContraptions.slnx -c Debug > "$E/build-debug.log" 2>&1; echo "build-debug exit $?" | tee "$E/exit-codes.txt"
dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore > "$E/test-slnx.log" 2>&1; echo "test-slnx exit $?" | tee -a "$E/exit-codes.txt"
bash _bmad-output/implementation-artifacts/evidence-10-1/ci-tool-step.sh > "$E/ci-tool-step.log" 2>&1; echo "ci-tool-step exit $?" | tee -a "$E/exit-codes.txt"
dotnet build CuriousContraptions.Simulation -c Release > "$E/worker-release.log" 2>&1; echo "worker-release exit $?" | tee -a "$E/exit-codes.txt"
{ git diff --check; echo "tracked exit $?"
  for f in CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs _bmad-output/implementation-artifacts/spec-10-1-solid-cylinder.md; do
    git diff --no-index --check /dev/null "$f"; echo "$f no-index exit $? (1 = differs; findings would print above)"; done; } > "$E/diff-check.log" 2>&1
echo "diff-check done" | tee -a "$E/exit-codes.txt"
grep -E "Warning\(s\)|Error\(s\)" "$E/build-debug.log" "$E/worker-release.log"
grep -E "Passed!|Failed!" "$E/test-slnx.log" "$E/ci-tool-step.log"
cat "$E/diff-check.log"
