#!/bin/zsh
# Story 9A.2 re-add mutants for the deletions; run from the worktree root. Appends to <out>.
out=$1
scratch=/private/tmp/claude-501/-Users-aidan-dev-personal-tim/49d80295-44c4-4940-bc16-8455cdf5d09d/scratchpad
run() {
  dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj 2>&1 | grep -E " error |Build succeeded" | sort -u | head -3 | sed 's/^/build: /' >> "$out"
  dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --filter "FullyQualifiedName~WireFaultTests.NoSecondCodecOrScriptParserIsReAdded" 2>&1 \
    | grep -E "\[FAIL\]|Passed!|Failed!|is re-added|re-adds|DoesNotContain" | sed -E 's/\[xUnit.net [0-9:.]+\] +//' | sort -u | head -8 >> "$out"
  echo >> "$out"
}
echo "== R1 re-add an (empty) engine/gpu/WorkshopWire.cs" >> "$out"
cp "$scratch/WorkshopWire.cs.readd" engine/gpu/WorkshopWire.cs
run
rm engine/gpu/WorkshopWire.cs

echo "== R2 re-add extractReadIdentity to worker.js" >> "$out"
cp CuriousContraptions.Simulation/wwwroot/worker.js "$scratch/worker.js.backup"
perl -0pi -e 's/function flushRead\(\) \{/function extractReadIdentity(bytes) { return bytes.subarray(40, 64); }\nfunction flushRead() {/' CuriousContraptions.Simulation/wwwroot/worker.js
run
cp "$scratch/worker.js.backup" CuriousContraptions.Simulation/wwwroot/worker.js

echo "== R3 re-add the router catch-and-retry (catch (ArgumentException))" >> "$out"
cp engine/gpu/WorkshopCommandRouter.cs "$scratch/router.backup"
perl -0pi -e 's/        var command = WorkshopWireFlatBuffers\.DecodeCommand\(bytes\);\n/        WorkshopCommand command;\n        try { command = WorkshopWireFlatBuffers.DecodeCommand(bytes); }\n        catch (ArgumentException) { command = WorkshopWireFlatBuffers.DecodeCommand(bytes); }\n/' engine/gpu/WorkshopCommandRouter.cs
run
cp "$scratch/router.backup" engine/gpu/WorkshopCommandRouter.cs
