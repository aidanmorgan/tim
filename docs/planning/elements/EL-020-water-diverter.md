# EL-020 · Water diverter — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-020 · Water diverter · Water |
| Requirement anchor | [element-020](../requirements.md#element-020); source record [todo-338](../requirements.md#todo-338); integration task [sequence-task-389](../requirements.md#sequence-task-389) |
| Named entry | [element-020](../invest/named-elements.md#element-020); per-element proof owner S441 |
| CAT spec refined | None. The campaign's "fixed/powered/alternating diverter" is a ball-route element in a different row ([campaign table](../requirements.md#campaign-element-coverage)); this is the liquid route selector. |
| Related identities | [EL-011 Water T](EL-011-water-t-junction.md) (shares instead of selecting), [EL-003 Tap](EL-003-tap.md), pipe kit [EL-008](EL-008-straight-water-pipe.md)–[EL-012](EL-012-water-pipe-cap.md), [EL-004](EL-004-catch-basin.md) |
| Roadmap story | Unscheduled; ELEMENT-n row of the [roadmap](../invest/vertical-delivery.md#rolling-playable-roadmap), campaign chapter 7, levels 61–70. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## Declaration

**Shared water values** (identical in every Water-1 spec, EL-001 to EL-022; each is one family constant shared with Batch G):
- Liquid density ρw = 16 kg/m³, one constant for the whole water family, aligned between Batch F (EL-001–EL-022) and Batch G (EL-023–EL-036, EL-165–EL-172) (**proposed**: catalogue bodies have mean densities of 2–44 kg/m³ — Domino 2.2, Basketball 6.1, Bowling ball 43.5; at 16 the instances this batch chose for the S416 buoyancy construction behave as it asks — the Basketball, lighter than water, floats and the Bowling ball, denser, sinks — and water loads stay comparable to ball masses).
- Volume step 2⁻⁴ m³ ([component research, water](../../component-research.md#water), line 26), which weighs 1 kg at ρw.
- Gravity 9.81 m/s² (`engine/gpu/WorkshopConstruction.cs@a6c914e:L123-L123`).
- Orifice discharge Q = Cd · s · A · √(2 g Δh), Cd = 0.6 (**proposed**: the standard sharp-edged orifice coefficient; one bounded law for every aperture; the law itself is owned by S416 advection → S418).
- Standard water mouth: bore radius 0.10 m, collar outer radius 0.16 m, collar length 0.06 m (**proposed**: visibly narrower than the 0.65 m ball-pipe bore, `engine/gpu/WorkshopPipe.cs@a6c914e:L6-L11`, so water and ball pipes are never confused).
- Static water-vessel contact material, one family constant: restitution 0.12, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0, reusing the Receiver's values (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L63`) (**proposed** reuse: vessels are the same matte toy material as the Basket).
- Buoyancy of floating bodies: immersion damping c = 1.0 1/s, one family constant (**proposed**: aligned with Batch G; full law in [EL-016](EL-016-float.md)); every floating body's buoyant acceleration must stay under the 64 m/s² force-region clamp ([capability inventory](../../gpu-f32-physics.md#capability-inventory)).

**Element declaration:**
- **Bodies and shapes.** One static body: a Y-shaped housing, inlet stem 0.30 m and two outlet legs 0.30 m at ±30° from the stem axis, outer radius 0.14 m, with a selector lever 0.30 m long on top (cosmetic) (**proposed**: legs far enough apart that two pipes snap without collar overlap). Colliders: three boxes along the stem and legs (**proposed** stand-ins).
- **Mass and material.** Static, zero mass; shared vessel material.
- **Constraints.** None.
- **Typed ports.** `WaterInlet`, `WaterOutletA`, `WaterOutletB` standard mouths. Domain `Water` (**proposed**).
- **Sensors and activation.** None.
- **Work and energy stores.** None. Routing: all inlet flow goes to the selected outlet by the orifice law at full bore; the unselected outlet receives only the declared leakage fraction of the inlet flow (default 0). If the selected outlet is unjoined, water spills from that mouth; it is never rerouted to the other outlet.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Selected | enum `DiverterOutlet { OutletA, OutletB }` | closed set | OutletA | — | route selection named by component research line 34; enum and default **proposed** (closed sets are enums end-to-end, [AGENTS.md rule](../../../AGENTS.md)) |
  | Leakage | f32 | 0–0.25, step 0.05 | 0 | fraction of inlet flow to the unselected outlet | "explicitly modelled leakage" from the requirement row; range/default **proposed** (zero is the clean control; a non-zero value must be visible in the selection UI) |

- **Cosmetic curves and UI bindings.** Lever and internal blade ← committed selection ([component research](../../component-research.md#water), line 34 "flap/blade ← committed state"); a thin stream ribbon on the unselected leg only when Leakage > 0.
- **Art.** Cream housing `#fff8e9` (`DESIGN.md@a6c914e:L151-L151`), gold lever `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`), navy collars `#293954` (`DESIGN.md@a6c914e:L147-L147`), raised A/B pips (one and two) on the legs (**proposed**: selection readable by shape).
- **Catalogue and inventory entry.** Id `water_diverter`, title "Water diverter", category "Water", kind `WaterDiverter` (all **proposed**).

**Variants.** The requirements row lists no variants.

## Engine capabilities

Families ([element map](../general-engine-element-map.md); binding [`element-01.json`](../../coverage/engine/element-01.json), consumer `element/element-020`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Static boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); enum-typed authored parameters and their save path (`engine/gpu/WorkshopSaveCodec.cs@a6c914e:L9-L10`); Run/Reset (`engine/gpu/WorkshopSimulation.cs@a6c914e:L238-L245`).
- **Missing.** A route-selecting network node ([S416](../invest/decisions.md#s416) advection → S418); mouth joins (EL-001 open question 3). Unscheduled.
- **Element dependencies.** A supply, pipes and two receivers (EL-004).

## Sources and legacy

- Requirements row: "Unselected route receives only explicitly modelled leakage." No variants.
- Refinement [S707](../invest/refinements.md#s707) diverter "Route declared valve branches"; integration [sequence-task-389](../requirements.md#sequence-task-389) "choose/gate a route … Do not substitute one generic timed valve for all variants"; component research row EL-019–021 (line 34).
- **Legacy search.** Same scope and terms as EL-001 plus "diverter" and "valve": no liquid or ball diverter exists in the tracked legacy at a6c914e.

## Acceptance outline

Point of truth: [element-020](../requirements.md#element-020).

- **Chrome recipe.** Water tank + Tap + pipe into a Water diverter; pipes from both outlets end over two separate Catch basins; choose Selected with the lever control (**proposed** UI: click the lever to toggle A/B while building).
- **Positive.** Selected A: basin A fills; basin B stays at exactly 0.
- **Negative / control.** Selected B: the reverse. Leakage 0.1 with A selected: basin B gains exactly 10% of what passes the diverter.
- **Boundaries.** Selected outlet unjoined: water spills at that mouth and basin B still gets nothing. Toggling Selected during Run is refused (authoring-only, **proposed**). Empty supply: neither basin gains.
- **Run/Reset.** Volumes restore exactly; Selected keeps its authored value. **Save/Load.** Selected, Leakage, pose and joins survive reload.
- **Integrations.** [sequence-task-389](../requirements.md#sequence-task-389): choose a route.

## Open questions

1. Should the selection change during Run through an input (lever linkage, solenoid or activation signal)? If yes, which input — that would be a separate actuation decision.
2. Is non-zero Leakage wanted at all, or is the declared leakage always 0?
3. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420). The orifice law and Cd = 0.6 are the S416 advection decision (→ S418).
