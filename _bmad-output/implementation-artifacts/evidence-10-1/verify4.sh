#!/usr/bin/env bash
# Round 4 frozen-candidate verification (from the story-10-1 worktree root): one Debug build, the full solution test reusing it
# (--blame-crash), the explicit 1000-case energy gate on the same build, the CI tools step, the Release worker build and the
# whitespace checks (tracked diff and the untracked files). Raw logs in round5/.
set -u
E=_bmad-output/implementation-artifacts/evidence-10-1/round5
mkdir -p "$E"
{ git rev-parse HEAD; git status --short; git diff --stat; git diff | shasum -a 256
  shasum -a 256 CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs _bmad-output/implementation-artifacts/spec-10-1-solid-cylinder.md \
    CuriousContraptions.Simulation/WorkshopCollisionKernels.cs CuriousContraptions.Simulation/WorkshopConstraintSolver.cs; } > "$E/candidate-identity.txt"
dotnet build CuriousContraptions.slnx -c Debug > "$E/build-debug.log" 2>&1; echo "build-debug exit $?" | tee "$E/exit-codes.txt"
start=$(date +%s)
dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore --blame-crash > "$E/test-slnx.log" 2>&1; echo "test-slnx exit $? ($(( $(date +%s) - start )) s)" | tee -a "$E/exit-codes.txt"
start=$(date +%s)
dotnet test CuriousContraptions.tests -c Debug --no-build --no-restore --filter "Category=CylinderEnergyGate&FullyQualifiedName~FullGate" \
  --logger "console;verbosity=detailed" -- xUnit.Explicit=on > "$E/full-gate-final.log" 2>&1
echo "full-gate exit $? ($(( $(date +%s) - start )) s)" | tee -a "$E/exit-codes.txt"
bash _bmad-output/implementation-artifacts/evidence-10-1/ci-tool-step.sh > "$E/ci-tool-step.log" 2>&1; echo "ci-tool-step exit $?" | tee -a "$E/exit-codes.txt"
dotnet build CuriousContraptions.Simulation -c Release > "$E/worker-release.log" 2>&1; echo "worker-release exit $?" | tee -a "$E/exit-codes.txt"
{ git diff --check; echo "tracked exit $?"
  for f in CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs _bmad-output/implementation-artifacts/spec-10-1-solid-cylinder.md \
    _bmad-output/implementation-artifacts/evidence-10-1/mutate4.sh _bmad-output/implementation-artifacts/evidence-10-1/verify4.sh; do
    git diff --no-index --check /dev/null "$f"; echo "$f no-index exit $? (1 = differs; findings would print above)"; done; } > "$E/diff-check.log" 2>&1
echo "diff-check done" | tee -a "$E/exit-codes.txt"
grep -E "Warning\(s\)|Error\(s\)" "$E/build-debug.log" "$E/worker-release.log"
grep -E "Passed!|Failed!" "$E/test-slnx.log" "$E/ci-tool-step.log" "$E/full-gate-final.log"
grep -E "over .* mJ" "$E/full-gate-final.log" | sed 's/^.*\]//' | sort -u
grep -v "exit 1 (1 = differs" "$E/diff-check.log"
