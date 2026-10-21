#!/bin/zsh
# Story 9A.17 deletion proof; run from the worktree root. Writes to stdout.
code=(engine ui elements schemas tools CuriousContraptions.Abstractions CuriousContraptions.Abstractions.Electrical
  CuriousContraptions.Abstractions.Fluid CuriousContraptions.Abstractions.Mechanical CuriousContraptions.Abstractions.Signal
  CuriousContraptions.Animation CuriousContraptions.Animation.Worker CuriousContraptions.Bundle CuriousContraptions.Simulation
  CuriousContraptions.web CuriousContraptions.tests CuriousContraptions.FlatBuffers CuriousContraptions.2dog)
echo "HEAD $(git rev-parse HEAD)"
echo '$ rg "WorkshopPorts\.For|WorkbenchFootprint\.Of" (the AC3 literal, whole repo minus planning/evidence history)'
rg -n "WorkshopPorts\.For|WorkbenchFootprint\.Of" $code docs DESIGN.md README.md -g '!docs/planning/**'; echo "rg exit $? (1 = no hits)"
echo '$ rg -l "WorkshopPorts\.For|WorkbenchFootprint\.Of" . (whole repo: only dated history remains, never edited: planning baselines @a6c914e, reviews, specs, evidence and the story text itself)'
rg -l "WorkshopPorts\.For|WorkbenchFootprint\.Of" . | grep -v "/bin/\|/obj/" | grep -v "^./_bmad-output/\|^./docs/planning/"; echo "grep exit $? (1 = no hits outside _bmad-output and docs/planning)"
echo '$ rg -nw "WorkshopPorts|WorkbenchFootprint" <code roots>'
rg -nw "WorkshopPorts|WorkbenchFootprint" $code; echo "rg exit $? (1 = no hits)"
echo '$ rg -nw "TryGetPortTransform|GetPortTransform|GetRelativeTransform" <code roots>'
rg -nw "TryGetPortTransform|GetPortTransform|GetRelativeTransform" $code; echo "rg exit $? (1 = no hits)"
echo '$ rg -n "Loc_Port_\{" engine ui (no runtime scene lookup of a socket locator)'
rg -n 'Loc_Port_\{' engine ui; echo "rg exit $? (1 = no hits)"
echo '$ rg -n alias literals ("power_in", "in", "out", "supply", ...) engine ui CuriousContraptions.Simulation'
rg -n '"(power_in|in|out|supply|activation_in|activation_out|set_in|reset_in)"' engine ui CuriousContraptions.Simulation; echo "rg exit $? (1 = no hits)"
echo '$ rg -n "Ports => PortLocators" engine (the PartDefinition alias)'
rg -n "Ports => PortLocators" engine; echo "rg exit $? (1 = no hits)"
echo '$ rg -n "PortId" <code roots> (PortDescriptor.PortId is now the typed Socket; ExtractedPortLocator and FluidPortDescriptor keep their own names)'
rg -n "PortId" $code | grep -v "ExtractedPortLocator\|FluidPortDescriptor\|FluidContracts.cs\|GltfPhysicsBodyExtractor.cs\|\.PortId);\|portId"; echo "grep exit $? (1 = no hits)"
echo '$ rg -n "Loc_Port_" elements -g "*.tscn" (every locator is a typed socket or a tube mouth)'
rg -n 'name="Loc_Port_' elements -g '*.tscn' | sed -E 's/.*name="(Loc_Port_[^"]*)".*/\1/' | sort | uniq -c
echo '$ rg -n "OrdinalIgnoreCase" engine/gpu/GltfPhysicsBodyExtractor.cs | rg "Loc_Port|ExtractedPortLocator"'
rg -n "OrdinalIgnoreCase" engine/gpu/GltfPhysicsBodyExtractor.cs | rg "Loc_Port|ExtractedPortLocator"; echo "rg exit $? (1 = no hits)"
