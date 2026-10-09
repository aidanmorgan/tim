# General physics capability requirements

Stub. The general-engine contract is [engine-contracts.md#general-data-driven-engines](engine-contracts.md#general-data-driven-engines); the numeric, compilation and envelope authority is [gpu-f32-physics.md](gpu-f32-physics.md); execution order is the [rolling playable roadmap](planning/invest/vertical-delivery.md#rolling-playable-roadmap). This file keeps only the one surviving requirement that was defined here.

**PERF-23 — element-to-capability coverage manifest.** A compiler-checked manifest and a report generated from it resolve every catalogue entry, fixture, element, named variant and research candidate to a stable typed identity (enum-typed capabilities/statuses, strongly typed IDs; duplicates and references recorded explicitly, never dropped or double-counted). Each element/mode row records:

- its source specification and current status;
- the generic capabilities it instantiates (rigid body, shape, material, constraint/drive, force region, finite store/source, sensor/trigger, network node) and the domains they belong to, with parameters, units and admitted ranges;
- conserved quantities, ports, events and thresholds;
- its declaration path and animation bindings, with an animation classification so cosmetic needs cannot create solver requirements;
- its Chrome/Playwright proof identities (real UI construction, positive/control, exact Reset and Save/Load) and production bundle/revision.

A manifest row or generic interface is not evidence of implementation; missing processes, missing elements and missing proof are tracked separately, and a family is never complete from one representative. The animation classification serves PERF-36–38 (animation never owns physical results). Performance and device numbers (PERF-01–48) and P0-035 engine closure are release-checklist items ([stage gates](delivery-workflow.md#stage-gates)) enforced at LEGACY-0 and CAMPAIGN, not manifest fields or per-slice gates.
