#!/usr/bin/env bash
# Usage: mutate.sh <name>  (run from the story-9-8b worktree root after applying one mutation by Edit; restore afterwards)
set -u
name="$1"
E=_bmad-output/implementation-artifacts/evidence-9-8b/mutations
mkdir -p "$E"
git diff engine/gpu/SupplyRoute.cs > "$E/$name.diff"
dotnet build CuriousContraptions.slnx -c Debug > "$E/$name.build.log" 2>&1
echo "build $?"
dotnet test CuriousContraptions.tests -c Debug --no-build --no-restore \
  --filter "FullyQualifiedName~ElectricalXor|FullyQualifiedName~ElectricalOr|FullyQualifiedName~ElectricalNand|FullyQualifiedName~ElectricalNor|FullyQualifiedName~GateChooser|FullyQualifiedName~BothGate" \
  > "$E/$name.test.log" 2>&1
echo "test $?"
grep -E "Passed!|Failed!" "$E/$name.test.log"
grep -E "^\s+Failed " "$E/$name.test.log" | sed 's/\[.*//' | sort | uniq -c
