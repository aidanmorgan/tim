#!/bin/zsh
# Story 9A.3 deletion proof, run from the worktree root. Production = every C#, script and schema source outside tests, tools and build
# outputs. The AC pattern's only allowed hits are the typed command definitions (the WorkshopCommandKind enum, the schema enum and the
# generated enum) and uses of the enum member WorkshopCommandKind.ObserveMemory.
P=(engine ui elements schemas CuriousContraptions.Abstractions CuriousContraptions.Abstractions.Electrical CuriousContraptions.Abstractions.Fluid
   CuriousContraptions.Abstractions.Mechanical CuriousContraptions.Abstractions.Signal CuriousContraptions.Animation CuriousContraptions.Animation.Worker
   CuriousContraptions.Bundle CuriousContraptions.Simulation CuriousContraptions.web)
G=(-g '*.{cs,js,mjs,fbs}' -g '!**/bin/**' -g '!**/obj/**' -g '!**/AppBundle/**' -g '!**/_framework/**')
echo "HEAD $(git rev-parse --short HEAD)"
echo "## AC: rg \"reliableStalled|memoryRequest|SetReliableStall|ObserveMemory\" (production), every hit:"
rg -n "reliableStalled|memoryRequest|SetReliableStall|ObserveMemory" $G $P
echo "## the same, minus the typed definitions and WorkshopCommandKind.ObserveMemory uses (expected: nothing):"
rg -n "reliableStalled|memoryRequest|SetReliableStall|ObserveMemory" $G $P | grep -v "CommandKind\.ObserveMemory" \
  | grep -vE "^(schemas/workshop_wire.fbs|engine/gpu/WorkshopWireGenerated.cs):[0-9]+:\s*ObserveMemory = 12,?$" \
  | grep -vE "^engine/gpu/WorkshopMessages.cs:[0-9]+:public enum WorkshopCommandKind .*ConfigureElectrical, ObserveMemory \}$"
echo "## every other deleted symbol (expected: nothing):"
rg -n "_reliableStalled|memoryResult|candidateTimedOut|WorkshopMemoryWire|WorkshopMemoryRequest|WorkshopMemoryStatus|MemoryObservationId|BeginMemory|ReceiveMemory\b|ReleaseMemory|SendMemoryRequest|CompleteMemoryObservation|sendMemoryRequest|completeMemoryObservation|settleMemory|receiveMemoryResult|observeMemory|updateReliableStall|pendingReadIdentity|CCGPU_MEMORY_LATE|catch \{\}" $G $P
echo "## deleted file (expected: nothing):"
git ls-files engine/gpu/WorkshopMemoryWire.cs
echo DONE
