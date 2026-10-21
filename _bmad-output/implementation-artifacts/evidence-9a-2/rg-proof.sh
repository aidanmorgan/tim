#!/bin/zsh
# Deletion and AC5 proof for Story 9A.2; run from the worktree root.
echo '# AC5. Excluded, as history rather than current input: _bmad-output (stories, reviews and raw evidence), docs/work-orders'
echo '# (closed P0 work orders) and docs/coverage (the frozen capability inventory snapshot of commit 61e35b3f; no tool reads it).'
echo '# Excluded by definition: the FlatBuffers codec itself, its flatc output and the re-add ratchet (WireFaultTests.NoSecondCodecOrScriptParserIsReAdded names every deleted symbol as a string).'
echo '$ rg -n "WorkshopWire\." (every other tracked source, script, schema, tool and current doc)'
rg -n "WorkshopWire\." --glob '!_bmad-output/**' --glob '!docs/work-orders/**' --glob '!docs/coverage/**' --glob '!engine/gpu/WorkshopWireFlatBuffers.cs' --glob '!engine/gpu/WorkshopWireGenerated.cs' --glob '!CuriousContraptions.tests/WireFaultTests.cs' --glob '!**/bin/**' --glob '!**/obj/**' --glob '!**/AppBundle/**' --glob '!**/_framework/**'
echo "exit=$? (1 = no match)"
echo
echo '$ rg -nw "<deleted symbols>" engine ui CuriousContraptions.Simulation CuriousContraptions.Animation.Worker CuriousContraptions.web/wwwroot CuriousContraptions.tests tools schemas elements'
rg -nw "WorkshopWire|WorkshopActivationWire|WorkshopTimerWire|WorkshopContactWorkWire|WorkshopElectricalWire|WorkshopActivationMemoryWire|WorkshopResponseCodec|ResponseAbi|CommandAbi|IsInvalidStoredValue|IsMalformedRecord|WholeCount|extractReadIdentity|responseAbi|commandAbi|READ_BODIES_OFFSET|READ_BODY_COUNT|READ_CAPTURE_TIME|BODY_WIRE_BYTES|UnsupportedSave|AddMagic|GetConstructionArray|ConstructionLength" engine ui CuriousContraptions.Simulation CuriousContraptions.Animation.Worker CuriousContraptions.web/wwwroot CuriousContraptions.tests tools schemas elements --glob '!CuriousContraptions.tests/WireFaultTests.cs' --glob '!**/bin/**' --glob '!**/obj/**' --glob '!**/AppBundle/**' --glob '!**/_framework/**'
echo "exit=$? (1 = no match)"
echo
echo '$ git ls-files engine/gpu/WorkshopWire.cs engine/gpu/WorkshopActivationWire.cs engine/gpu/WorkshopTimerWire.cs engine/gpu/WorkshopContactWorkWire.cs'
git ls-files engine/gpu/WorkshopWire.cs engine/gpu/WorkshopActivationWire.cs engine/gpu/WorkshopTimerWire.cs engine/gpu/WorkshopContactWorkWire.cs
echo "(empty = deleted)"
echo
echo '$ rg -n "getUint16\(vtablePos|responseAbi|READ_BODIES_OFFSET|vtable|getRootAs" CuriousContraptions.*/wwwroot (no script parses a buffer)'
rg -n "getUint16\(vtablePos|responseAbi|READ_BODIES_OFFSET|vtable|getRootAs" CuriousContraptions.Simulation/wwwroot CuriousContraptions.web/wwwroot CuriousContraptions.Animation.Worker/wwwroot --glob '!**/_framework/**'
echo "exit=$? (1 = no match)"
echo
echo '$ rg -n "catch \(ArgumentException|catch \(Exception\)|catch$" engine/gpu/WorkshopCommandRouter.cs engine/gpu/BrowserWorkshopClient.cs engine/gpu/WorkshopSaveCodec.cs engine/gpu/WorkshopWireFlatBuffers.cs engine/gpu/WorkshopSavePrevalidation.cs'
rg -n "catch \(ArgumentException|catch \(Exception\)|catch$" engine/gpu/WorkshopCommandRouter.cs engine/gpu/BrowserWorkshopClient.cs engine/gpu/WorkshopSaveCodec.cs engine/gpu/WorkshopWireFlatBuffers.cs engine/gpu/WorkshopSavePrevalidation.cs
echo "exit=$? (1 = no match)"
