#!/bin/zsh
# Review-only native mutation runner (Murdoch-2, Story 9.4b). Applies one mutation to the isolated copy, builds, runs the
# tests, lists every test that passed at baseline but fails under the mutant (a kill), then restores the file.
# Usage: run-mutation.sh <name> <relative file> <perl substitution expression> [extra file to append to]
set -u
S=/private/tmp/claude-501/-Users-aidan-dev-personal-tim/49d80295-44c4-4940-bc16-8455cdf5d09d/scratchpad/murdoch2-9-4b
M=$S/mut
name=$1; file=$2; expr=$3
out=$S/mutations/$name; mkdir -p $out
cp "$M/$file" "$out/original"
perl -0pi -e "$expr" "$M/$file"
if cmp -s "$M/$file" "$out/original"; then echo "$name: MUTATION DID NOT APPLY"; exit 2; fi
diff -u "$out/original" "$M/$file" > "$out/mutation.diff"
( cd $M && timeout 900 dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj -v q > "$out/build.log" 2>&1 )
if ! grep -q " 0 Error(s)" "$out/build.log"; then
  echo "$name: BUILD FAILED (killed by the compiler)"; grep " error " "$out/build.log" | sort -u | head -3
else
  ( cd $M && timeout 900 dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build \
      --logger "trx;LogFileName=$name.trx" --results-directory "$out" > "$out/test.log" 2>&1 )
  grep -o 'testName="[^"]*" [^>]*outcome="[A-Za-z]*"' "$out/$name.trx" | sed -E 's/testName="([^"]*)".*outcome="([A-Za-z]*)"/\2 \1/' | sort > "$out/outcomes.txt"
  # A kill is a test that passed at baseline and does not pass under the mutant.
  comm -12 <(grep '^Passed ' $S/mut-baseline-outcomes.txt | cut -d' ' -f2- | sort) <(grep -v '^Passed ' "$out/outcomes.txt" | cut -d' ' -f2- | sort) > "$out/killed-by.txt"
  n=$(wc -l < "$out/killed-by.txt" | tr -d ' ')
  if [ "$n" -gt 0 ]; then echo "$name: KILLED by $n test(s):"; head -6 "$out/killed-by.txt" | sed 's/^/    /'; else echo "$name: SURVIVED (no baseline-passing test fails)"; fi
fi
cp "$out/original" "$M/$file"
