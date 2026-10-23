#!/usr/bin/env bash
# Usage: mutate.sh <name> <repo-relative file>  (run from the story-10-1 worktree root after applying one mutation by Edit)
# The mutation diff is taken against the frozen pre-mutation copy (never git), and the file is restored by Edit afterwards.
set -u
name="$1"; file="$2"
E=_bmad-output/implementation-artifacts/evidence-10-1/mutations
F=/private/tmp/claude-501/-Users-aidan-dev-personal-tim/7e56a042-6670-44d4-ae0f-4efd972de00e/scratchpad/frozen
mkdir -p "$E"
diff -u "$F/$(basename "$file")" "$file" > "$E/$name.diff"
dotnet build CuriousContraptions.tests -c Debug > "$E/$name.build.log" 2>&1
echo "build $?"
dotnet test CuriousContraptions.tests -c Debug --no-build --no-restore \
  --filter "FullyQualifiedName~WorkshopPhysicsLawCylinderTests" > "$E/$name.test.log" 2>&1
echo "test $?"
grep -E "Passed!|Failed!" "$E/$name.test.log"
grep -E "^\s+Failed " "$E/$name.test.log" | sed 's/\[.*//' | sort | uniq -c
