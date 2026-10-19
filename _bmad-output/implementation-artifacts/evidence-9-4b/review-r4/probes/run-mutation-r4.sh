#!/bin/zsh
# Review round 4 mutation runner (Murdoch-2, Story 9.4b candidate 5). Isolated copy mut5; never the main checkout.
# Usage: run-mutation-r4.sh <name> <suite: tests|coverage|node> <relative file> <perl expression>
# A kill is a test (or node subtest) that passes at the copy's baseline and does not pass under the mutant.
set -u
S=/private/tmp/claude-501/-Users-aidan-dev-personal-tim/49d80295-44c4-4940-bc16-8455cdf5d09d/scratchpad/murdoch2-9-4b
M=$S/mut5
name=$1; suite=$2; file=$3; expr=$4
out=$S/mutations-r4/$name; mkdir -p $out
cp "$M/$file" "$out/original"
perl -0pi -e "$expr" "$M/$file"
if cmp -s "$M/$file" "$out/original"; then echo "$name: MUTATION DID NOT APPLY"; exit 2; fi
diff -u "$out/original" "$M/$file" > "$out/mutation.diff"
outcomes() { grep -o 'testName="[^"]*" [^>]*outcome="[A-Za-z]*"' "$1" | sed -E 's/testName="([^"]*)".*outcome="([A-Za-z]*)"/\2 \1/' | sort; }
case $suite in
  tests|coverage)
    proj=CuriousContraptions.tests/CuriousContraptions.tests.csproj; base=$S/mut5-baseline-outcomes.txt
    [ $suite = coverage ] && { proj=tools/Coverage.Tests/Coverage.Tests.csproj; base=$S/mut5-coverage-baseline-outcomes.txt; }
    ( cd $M && timeout 900 dotnet build $proj -v q > "$out/build.log" 2>&1 )
    if ! grep -q " 0 Error(s)" "$out/build.log"; then
      echo "$name: BUILD FAILED (killed by the compiler)"; grep " error " "$out/build.log" | sort -u | head -2
    else
      ( cd $M && timeout 900 dotnet test $proj --no-build --logger "trx;LogFileName=$name.trx" --results-directory "$out" > "$out/test.log" 2>&1 )
      outcomes "$out/$name.trx" > "$out/outcomes.txt"
      comm -12 <(grep '^Passed ' $base | cut -d' ' -f2- | sort) <(grep -v '^Passed ' "$out/outcomes.txt" | cut -d' ' -f2- | sort) > "$out/killed-by.txt"
    fi ;;
  node)
    ( cd $M && timeout 600 node --experimental-vm-modules --test --test-reporter=tap tools/*.test.mjs > "$out/test.tap" 2>&1 )
    grep -E "^\s*not ok " "$out/test.tap" | sed -E 's/^\s*not ok [0-9]+ - //' | sort -u > "$out/killed-by.txt" ;;
esac
if [ -f "$out/killed-by.txt" ]; then
  n=$(wc -l < "$out/killed-by.txt" | tr -d ' ')
  if [ "$n" -gt 0 ]; then echo "$name: KILLED by $n:"; head -4 "$out/killed-by.txt" | cut -c1-170 | sed 's/^/    /'; else echo "$name: SURVIVED"; fi
fi
cp "$out/original" "$M/$file"
