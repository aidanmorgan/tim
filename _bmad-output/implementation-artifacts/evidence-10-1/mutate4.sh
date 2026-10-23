#!/usr/bin/env bash
# Round 4 (single emission fix). Usage: mutate4.sh <name> <repo-relative file>  (from the story-10-1 worktree root, one mutation applied
# by Edit). The diff is taken against the frozen round-4 copy (never git); the file is restored by Edit and checked with cmp.
# Stage 1 runs the fast cylinder tests; only a mutant that survives it runs stage 2 (the energy gate, walks, overlap, tumbles, drops).
set -u
name="$1"; file="$2"
E=_bmad-output/implementation-artifacts/evidence-10-1/mutations-round4
F=/private/tmp/claude-501/-Users-aidan-dev-personal-tim/7e56a042-6670-44d4-ae0f-4efd972de00e/scratchpad/frozen4
mkdir -p "$E"
diff -u "$F/$(basename "$file")" "$file" > "$E/$name.diff"
# An unapplied mutation would test the frozen code: stop instead (no verdict).
if [ ! -s "$E/$name.diff" ]; then echo "empty diff: mutation not applied"; exit 3; fi
dotnet build CuriousContraptions.tests -c Debug > "$E/$name.build.log" 2>&1
built=$?
echo "build $built"
# A mutant that does not compile would test the previous binary: stop instead (no verdict).
if [ $built -ne 0 ]; then grep -E " error " "$E/$name.build.log" | sort -u | head -3; exit 2; fi
heavy="FullyQualifiedName~FreshSeedDrops|FullyQualifiedName~ARimAimedWalk|FullyQualifiedName~OverlapDepth|FullyQualifiedName~Tumbling|FullyQualifiedName~DropsOntoALyingCylinder"
dotnet test CuriousContraptions.tests -c Debug --no-build --no-restore \
  --filter "FullyQualifiedName~WorkshopPhysicsLawCylinderTests&FullyQualifiedName!~FreshSeedDrops&FullyQualifiedName!~ARimAimedWalk&FullyQualifiedName!~OverlapDepth&FullyQualifiedName!~Tumbling&FullyQualifiedName!~DropsOntoALyingCylinder" \
  > "$E/$name.test.log" 2>&1
code=$?
echo "stage1 $code"
if [ $code -eq 0 ]; then
  dotnet test CuriousContraptions.tests -c Debug --no-build --no-restore --filter "FullyQualifiedName~WorkshopPhysicsLawCylinderTests&($heavy)" \
    > "$E/$name.stage2.log" 2>&1
  echo "stage2 $?"
  grep -E "Passed!|Failed!" "$E/$name.stage2.log"
  grep -E "^\s+Failed " "$E/$name.stage2.log" | sed 's/\[.*//' | sort | uniq -c
else
  grep -E "Passed!|Failed!" "$E/$name.test.log"
  grep -E "^\s+Failed " "$E/$name.test.log" | sed 's/\[.*//' | sort | uniq -c | head -5
fi
