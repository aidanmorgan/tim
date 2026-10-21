#!/bin/zsh
# Story 9A.2 mutants: each applies one perl edit to one file, rebuilds the tests, runs the named filter and restores the file.
# Usage: mutants.sh <out file>; run from the worktree root.
out=$1
: > "$out"
scratch=/private/tmp/claude-501/-Users-aidan-dev-personal-tim/49d80295-44c4-4940-bc16-8455cdf5d09d/scratchpad/mutant-backup
mkdir -p "$scratch"

mutant() {
  local name=$1 file=$2 edit=$3 filter=$4
  cp "$file" "$scratch/backup"
  perl -0pi -e "$edit" "$file"
  if cmp -s "$file" "$scratch/backup"; then echo "== $name: EDIT DID NOT APPLY" >> "$out"; return; fi
  echo "== $name ($file)" >> "$out"
  diff -U0 "$scratch/backup" "$file" | grep -E '^[-+][^-+]' | head -8 >> "$out"
  local build
  build=$(dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj 2>&1 | grep -E " error |Build succeeded" | sort -u | head -3)
  echo "build: $build" >> "$out"
  dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --filter "$filter" 2>&1 \
    | grep -E "\[FAIL\]|Passed!|Failed!" | sed -E 's/\[xUnit.net [0-9:.]+\] +//' | sort -u | head -12 >> "$out"
  cp "$scratch/backup" "$file"
  echo >> "$out"
}

F=engine/gpu/WorkshopWireFlatBuffers.cs
mutant "M1 skip motion encode (AC1)" $F 's/        if \(motion\.HasValue\) Wire\.WorkshopResponse\.AddMotion\(builder, motion\.Value\);\n//' "FullyQualifiedName~WireFaultTests"
mutant "M2 verifier call removed (AC3)" $F 's/try \{ verified = new Verifier\(buffer\)\.VerifyBuffer\(identifier, true, verify\); \}/try { verified = true; }/' "FullyQualifiedName~WireFaultTests|FullyQualifiedName~WorkshopWireDecodeBoundaryTests"
mutant "M3 size-prefix check removed (AC3)" $F 's/        if \(declared < bytes\.Length\)\n            throw new WireFaultException\(WireFaultReason\.TrailingBytes, [^;]+;\n//' "FullyQualifiedName~WireFaultTests"
mutant "M4 identifier check removed (AC3)" $F 's/bytes\.Length < FrameHeaderBytes \|\| !IsIdentifier\(bytes\[IdentifierOffset\.\.FrameHeaderBytes\], identifier\)/bytes.Length < FrameHeaderBytes/' "FullyQualifiedName~WireFaultTests"
mutant "M5 counter target defaulted (deleted silent default)" $F 's/        0 => throw new WireFaultException\(WireFaultReason\.MissingField, \$"Counter \{inst\.Id\} has no target count\."\),\n/        0 => WorkshopCounter.PlacedTargetCount,\n/' "FullyQualifiedName~ConstructionDecodeBoundaryTests"
mutant "M6 session silently defaulted (deleted fallback)" $F 's/        : throw new WireFaultException\(WireFaultReason\.MissingField, "A Workshop message requires its 16-byte runtime session\."\);/        : default;/' "FullyQualifiedName~WireFaultTests"
mutant "M7 tick-0 motion fabricated again" $F 's/var motion = motionBytes is null \? null : PhysicsMotionRead\.Decode\(motionBytes, bodies, tick\);/var motion = PhysicsMotionRead.Decode(motionBytes ?? new byte[PhysicsMotionRead.ByteLength(bodies.Count)], bodies, tick);/' "FullyQualifiedName~WorkshopReadTests|FullyQualifiedName~WireFaultTests"

R=engine/gpu/WorkshopCommandRouter.cs
mutant "M8 router swallows a refused command (AC3 retry)" $R 's/        var command = WorkshopWireFlatBuffers\.DecodeCommand\(bytes\);\n/        WorkshopCommand command;\n        try { command = WorkshopWireFlatBuffers.DecodeCommand(bytes); }\n        catch (WireFaultException) { command = new(new(_lastSequence + 1), WorkshopCommandKind.Run, default, default, null); }\n/' "FullyQualifiedName~WireFaultTests"

S=engine/gpu/WorkshopSaveCodec.cs
mutant "M9 save path reclassifies a domain ArgumentException (AC4)" $S 's/        WorkshopSavePrevalidation\.Check\(saved\);\n        return Construct\(saved\);/        WorkshopSavePrevalidation.Check(saved);\n        try { return Construct(saved); }\n        catch (ArgumentException error) { throw new WorkshopSaveRejectedException(WorkshopSaveRejection.OutOfRange, error.Message, error); }/' "FullyQualifiedName~WireFaultTests"
mutant "M10 pre-validation skipped (AC4)" $S 's/        WorkshopSavePrevalidation\.Check\(saved\);\n//' "FullyQualifiedName~WorkshopSaveRejectionTests"

P=engine/gpu/WorkshopSavePrevalidation.cs
mutant "M11 duplicate identity check removed (AC4)" $P 's/            if \(!parts\.TryAdd\(new\(instance\.Id\), part\)\)\n                throw Refuse\(WorkshopSaveRejection\.DuplicateInstance, \$"Two saved parts share identity \{instance\.Id\}\."\);/            parts[new(instance.Id)] = part;/' "FullyQualifiedName~WorkshopSaveRejectionTests"
mutant "M12 ramp range one step wide (AC4 parity)" $P 's/Within\(id, "length", r\.Length, \(Half\)\.125, \(Half\)4\);/Within(id, "length", r.Length, (Half).125, (Half)4.004);/' "FullyQualifiedName~WorkshopSaveRejectionTests.PreValidationAdmitsExactlyWhatTheDomainAdmits"
mutant "M13 narrowing skipped on a declared material (AC4 parity)" $P 's/        if \(!HalfBits\.Equal\(Narrow\(id, field, value\), declared\)\)/        if (Narrow(id, field, value) != declared || value != (float)declared)/' "FullyQualifiedName~WorkshopSaveRejectionTests.PreValidationAdmitsExactlyWhatTheDomainAdmits"
mutant "M14 connection rules skipped (AC4)" $P 's/        try \{ new WorkshopConnections\(links\)\.Validate\(id => parts\.TryGetValue\(id, out var part\) \? part : null\); \}/        try { }/' "FullyQualifiedName~WorkshopSaveRejectionTests|FullyQualifiedName~WorkshopSaveTests"

U=ui/WorkshopUiFailure.cs
mutant "M15 save refusal shown as a fault (AC4 player-visible)" $U 's/ or WorkshopSaveRejectedException;/;/' "FullyQualifiedName~WorkshopSaveRejectionTests"

PM=engine/gpu/PhysicsMotionRead.cs
mutant "M16 motion tick overflow restored (fuzz)" $PM 's/tick\.Value > uint\.MaxValue \|\| U32\(bytes, 12\) != tick\.Value \* steps/U32(bytes, 12) != checked(tick.Value * steps)/' "FullyQualifiedName~WorkshopWireDecodeBoundaryTests"
