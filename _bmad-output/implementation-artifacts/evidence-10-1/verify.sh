#!/usr/bin/env bash
# Frozen-candidate verification for Story 10.1 (run from the story-10-1 worktree root): one Debug build, the full solution test
# reusing it, the CI tools step, the Release worker build and the whitespace check. Each step's raw output is kept.
set -u
E=_bmad-output/implementation-artifacts/evidence-10-1
{ git rev-parse HEAD; git status --short; git diff --stat; } > "$E/candidate-identity.txt"
git diff | shasum -a 256 >> "$E/candidate-identity.txt"
shasum -a 256 CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs >> "$E/candidate-identity.txt"
dotnet build CuriousContraptions.slnx -c Debug > "$E/build-debug.log" 2>&1; echo "build-debug exit $?" | tee "$E/exit-codes.txt"
dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore > "$E/test-slnx.log" 2>&1; echo "test-slnx exit $?" | tee -a "$E/exit-codes.txt"
bash "$E/ci-tool-step.sh" > "$E/ci-tool-step.log" 2>&1; echo "ci-tool-step exit $?" | tee -a "$E/exit-codes.txt"
dotnet build CuriousContraptions.Simulation -c Release > "$E/worker-release.log" 2>&1; echo "worker-release exit $?" | tee -a "$E/exit-codes.txt"
git diff --check > "$E/diff-check.log" 2>&1; echo "diff-check exit $?" | tee -a "$E/exit-codes.txt"
grep -E "Warning\(s\)|Error\(s\)" "$E/build-debug.log" "$E/worker-release.log"
grep -E "Passed!|Failed!" "$E/test-slnx.log" "$E/ci-tool-step.log"
