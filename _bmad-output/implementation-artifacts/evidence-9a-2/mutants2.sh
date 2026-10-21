#!/bin/zsh
# Story 9A.2 mutants whose removed code leaves an unused member (the style analyzers would refuse the build): built with code-style
# enforcement and warnings-as-errors off, and the tests run only when that build succeeds. Run from the worktree root.
out=$1
scratch=/private/tmp/claude-501/-Users-aidan-dev-personal-tim/49d80295-44c4-4940-bc16-8455cdf5d09d/scratchpad/mutant-backup
mkdir -p "$scratch"
mutant() {
  local name=$1 file=$2 edit=$3 filter=$4
  cp "$file" "$scratch/backup"
  perl -0pi -e "$edit" "$file"
  if cmp -s "$file" "$scratch/backup"; then echo "== $name: EDIT DID NOT APPLY" >> "$out"; return; fi
  echo "== $name ($file) [analyzer style enforcement off for this build]" >> "$out"
  diff -U0 "$scratch/backup" "$file" | grep -E '^[-+][^-+]' | head -8 >> "$out"
  if dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj -p:EnforceCodeStyleInBuild=false -p:TreatWarningsAsErrors=false 2>&1 | grep -q "Build succeeded"; then
    echo "build: Build succeeded." >> "$out"
    dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --filter "$filter" 2>&1 \
      | grep -E "\[FAIL\]|Passed!|Failed!" | sed -E 's/\[xUnit.net [0-9:.]+\] +//' | sort -u | head -12 >> "$out"
  else
    echo "build: FAILED (mutant not run)" >> "$out"
  fi
  cp "$scratch/backup" "$file"
  echo >> "$out"
}
F=engine/gpu/WorkshopWireFlatBuffers.cs
mutant "M1 skip motion encode (AC1)" $F 's/        if \(motion\.HasValue\) Wire\.WorkshopResponse\.AddMotion\(builder, motion\.Value\);\n//' "FullyQualifiedName~WireFaultTests"
mutant "M2 verifier call removed (AC3)" $F 's/try \{ verified = new Verifier\(buffer\)\.VerifyBuffer\(identifier, true, verify\); \}/try { verified = true; }/' "FullyQualifiedName~WireFaultTests|FullyQualifiedName~WorkshopWireDecodeBoundaryTests"
mutant "M4 identifier check removed (AC3)" $F 's/bytes\.Length < FrameHeaderBytes \|\| !IsIdentifier\(bytes\[IdentifierOffset\.\.FrameHeaderBytes\], identifier\)/bytes.Length < FrameHeaderBytes/' "FullyQualifiedName~WireFaultTests"
