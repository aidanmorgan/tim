#!/bin/zsh
# Story 9A.15 deletion proof; run from the worktree root. Writes to stdout. "rg exit 1" means no hits.
roots=(engine ui elements schemas tools CuriousContraptions.Abstractions CuriousContraptions.Abstractions.Electrical
  CuriousContraptions.Abstractions.Fluid CuriousContraptions.Abstractions.Mechanical CuriousContraptions.Abstractions.Signal
  CuriousContraptions.Animation CuriousContraptions.Animation.Worker CuriousContraptions.Bundle CuriousContraptions.Simulation
  CuriousContraptions.web CuriousContraptions.tests CuriousContraptions.FlatBuffers CuriousContraptions.2dog docs)
echo "HEAD $(git rev-parse HEAD); tracked code differs from it in $(git diff --stat HEAD -- engine ui schemas tools CuriousContraptions.Simulation CuriousContraptions.tests | wc -l | tr -d ' ') diff-stat lines"
echo
echo '== Deleted: PartId.ToNumericId (with _ => 0) and PartId.FromNumericId'
echo '$ rg -nw "ToNumericId|FromNumericId" <code roots and docs>'
rg -nw "ToNumericId|FromNumericId" $roots; echo "rg exit $?"
echo
echo '== Deleted: the hard-coded Decodable element list in the save pre-validation'
echo '$ rg -n "Decodable" engine'
rg -n "Decodable" engine; echo "rg exit $?"
echo
echo '== Deleted: the per-id generic decode list and the untyped unknown-kind refusal in the worker decode'
echo '$ rg -n per-id generic list / "Unsupported instance kind" engine/gpu/WorkshopWireFlatBuffers.cs'
rg -n '"pipe_bend_45" or "pipe_bend_90"|=> new WorkshopGenericInstance|Unsupported instance kind' engine/gpu/WorkshopWireFlatBuffers.cs; echo "rg exit $?"
echo
echo '== Deleted: Coverage reading engine/PartId.cs'
echo '$ rg -n "engine/PartId.cs|ToNumericId" tools/Coverage tools/Coverage.Tests'
rg -n "engine/PartId.cs|ToNumericId" tools/Coverage tools/Coverage.Tests; echo "rg exit $?"
echo
echo '== Kept: element-naming schema fields are strings'
rg -n "part_id|inventory_part|(^|_)element" schemas/workshop_wire.fbs
echo
echo '== Out of scope, with owners: the same id list outside the serialisation boundary'
echo '   engine/MachineWorld.Gpu.cs: main-thread placement capture (CaptureInstance: A3 conversions; its collider fallback: Story 9A.16)'
echo '   engine/gpu/WorkshopConnections.cs: WorkshopPorts.For (Story 9A.17)'
rg -n '"pipe_bend_45" or "pipe_bend_90"' engine ui
echo
echo '== For the record: whole repo (evidence directories excluded): planning text and history only'
rg -lw "ToNumericId|FromNumericId" . | grep -v "evidence-" | sort
