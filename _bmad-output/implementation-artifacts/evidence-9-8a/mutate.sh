#!/usr/bin/env bash
# Usage: mutate.sh <name>  (run from the story-9-8a worktree root after applying a mutation by Edit)
set -u
name="$1"
E=_bmad-output/implementation-artifacts/evidence-9-8a/mutations
mkdir -p "$E"
git diff engine/gpu/SupplyRoute.cs > "$E/$name.diff"
dotnet build CuriousContraptions.slnx -c Debug > "$E/$name.build.log" 2>&1
echo "build $?"
dotnet test CuriousContraptions.tests -c Debug --no-build --no-restore \
  --filter "FullyQualifiedName~ElectricalOr|FullyQualifiedName~ElectricalNand|FullyQualifiedName~ElectricalNor|FullyQualifiedName~GateChooser|FullyQualifiedName~BothGate" \
  > "$E/$name.test.log" 2>&1
echo "test $?"
grep -E "Passed!|Failed!" "$E/$name.test.log"
grep -E "^\s+Failed " "$E/$name.test.log" | sed 's/\[.*//' | sort | uniq -c
