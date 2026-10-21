#!/bin/zsh
# Story 9A.15 deletion proof; run from the worktree root. Writes to stdout.
roots=(engine ui elements schemas tools CuriousContraptions.Abstractions CuriousContraptions.Abstractions.Electrical
  CuriousContraptions.Abstractions.Fluid CuriousContraptions.Abstractions.Mechanical CuriousContraptions.Abstractions.Signal
  CuriousContraptions.Animation CuriousContraptions.Animation.Worker CuriousContraptions.Bundle CuriousContraptions.Simulation
  CuriousContraptions.web CuriousContraptions.tests CuriousContraptions.FlatBuffers CuriousContraptions.2dog docs)
echo "HEAD $(git rev-parse HEAD)"
echo '$ rg -nw "ToNumericId|FromNumericId" <code roots and docs>'
rg -nw "ToNumericId|FromNumericId" $roots; echo "rg exit $? (1 = no hits)"
echo '$ rg -n "Decodable" engine'
rg -n "Decodable" engine; echo "rg exit $? (1 = no hits)"
echo '$ rg -n per-id generic decode list / untyped unknown-kind refusal'
rg -n '"pipe_bend_45" or "pipe_bend_90"|Unsupported instance kind' engine CuriousContraptions.Simulation; echo "rg exit $? (1 = no hits)"
echo '$ rg -n "engine/PartId.cs" tools/Coverage'
rg -n "engine/PartId.cs" tools/Coverage; echo "rg exit $? (1 = no hits)"
echo '$ rg -n element-naming fields in schemas/workshop_wire.fbs (all strings)'
rg -n "part_id|inventory_part|(^|_)element" schemas/workshop_wire.fbs
echo '$ rg -lw "ToNumericId|FromNumericId" . (whole repo, evidence dirs excluded: planning text and history only)'
rg -lw "ToNumericId|FromNumericId" . | grep -v "evidence-" | sort
