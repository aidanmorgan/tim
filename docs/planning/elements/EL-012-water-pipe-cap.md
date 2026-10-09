# EL-012 · Water pipe cap — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-012 · Water pipe cap · Water |
| Requirement anchor | [element-012](../requirements.md#element-012); source record [todo-335](../requirements.md#todo-335); integration task [sequence-task-385](../requirements.md#sequence-task-385) |
| Named entry | [element-012](../invest/named-elements.md#element-012); per-element proof owner S433 |
| CAT spec refined | None. |
| Related identities | Every standard mouth: [EL-001](EL-001-finite-reservoir.md), [EL-002](EL-002-header-tank.md), [EL-003](EL-003-tap.md), [EL-005](EL-005-liquid-funnel.md), [EL-008](EL-008-straight-water-pipe.md) to [EL-011](EL-011-water-t-junction.md); it is the blocked-outlet control for most of them |
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

**Shared pipe-kit values** (EL-008 to EL-013, stated in full in [EL-008](EL-008-straight-water-pipe.md)): mouth join within 0.15 m and 10° of opposite (**proposed**); lossless sealed conduits (**proposed**); transit volume = bore area × centreline length; an unjoined mouth discharges as a free stream and never feeds an unjoined neighbour.

**Element declaration:**
- **Bodies and shapes.** One static body: a domed cap, radius 0.17 m, depth 0.10 m; collider one box 0.34 × 0.34 × 0.10 m (**proposed**: just wider than the 0.16 m collar so it visibly covers the mouth).
- **Mass and material.** Static, zero mass; shared vessel material.
- **Constraints.** None.
- **Typed ports.** One `WaterMouth` standard mouth facing out of the cap's open side. Domain `Water` (**proposed**).
- **Sensors and activation.** None.
- **Work and energy stores.** None; zero internal volume. A joined cap closes the mouth: zero discharge at any head (**proposed** zero leakage: a cap is the "nothing passes" control; a leaking cap would undermine every blocked-outlet test).
- **Parameters.** None.
- **Cosmetic curves and UI bindings.** None (static); gold seated ring when joined (**proposed**, as EL-008).
- **Art.** Navy dome `#293954` (`DESIGN.md@a6c914e:L147-L147`) with a cream band `#fff8e9` (`DESIGN.md@a6c914e:L151-L151`) (**proposed**: dark cap reads as "closed" on cream pipes by shape and contrast).
- **Catalogue and inventory entry.** Id `water_cap`, title "Pipe cap", category "Water", kind `WaterCap` (all **proposed**).

**Variants.** The requirements row lists no variants.

## Engine capabilities

Families ([element map](../general-engine-element-map.md); binding [`element-01.json`](../../coverage/engine/element-01.json), consumer `element/element-012`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Static box (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); part deletion and Run/Reset (`engine/gpu/WorkshopSimulation.cs@a6c914e:L238-L245`).
- **Missing.** Closed network node ([S416](../invest/decisions.md#s416) → S418/S419); mouth joins and their removal on delete (EL-001 open question 3). Unscheduled.
- **Element dependencies.** Any mouth-bearing water element.

## Sources and legacy

- Requirements row: "Capped flow stops; deleting the cap restores a real opening." No variants.
- Component research pipe-kit row (line 28); refinement [S705](../invest/refinements.md#s705) cap "Seal an end"; integration [sequence-task-385](../requirements.md#sequence-task-385) "a capped route cannot leak through an invisible connection".
- **Legacy search.** No liquid cap. Join facts:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | An occupied mouth is not a join candidate; removing the occupant makes the mouth available again. | `CuriousContraptions.tests/TubePlacementSnapTests.cs@a6c914e:L103-L121` | Carry forward: a cap occupies a mouth exclusively; deleting it frees the mouth. |
| 2 | Joins are refused while running and restored by Reset. | `CuriousContraptions.tests/TubePlacementSnapTests.cs@a6c914e:L54-L85` | Carry forward: caps are placed and removed only while building. |
| 3 | Mouth record (id, position at collar face, outward normal, bore radius). | `engine/TubeMouth.cs@a6c914e:L6-L14` | Carry forward the shape; not the Godot types. |

## Acceptance outline

Point of truth: [element-012](../requirements.md#element-012).

- **Chrome recipe.** Water tank + Tap + straight pipe ending over a Catch basin; drag a Pipe cap onto the pipe's open mouth until it seats.
- **Positive (closure).** Run: the pipe fills to the tank level and stops; the basin stays empty; the ledger shows no loss.
- **Control (opening restored).** Reset, select the cap, delete it, Run: water discharges from the now-open mouth into the basin.
- **Negative.** A cap placed 0.3 m from the mouth (not seated) does not close it: water discharges past it.
- **Boundaries.** A cap on a high-head riser (EL-002 at 4 m) still passes nothing. A cap on a mouth already joined to a pipe is refused.
- **Run/Reset.** Exact restoration. **Save/Load.** The seated cap and join survive reload.
- **Integrations.** [sequence-task-385](../requirements.md#sequence-task-385) (source record [todo-335](../requirements.md#todo-335)): caps join by compatible water ports and a capped route cannot leak through an invisible connection. Generic interaction row [IX-08 Fluid advection](../requirements.md#interaction-08). Campaign first use 61–70, reuse 71–90, 114, 126–130, 136–150 ([campaign table](../requirements.md#campaign-element-coverage)).

## Open questions

1. Should a cap also fit the ball-pipe bore (CAT-048) as a separate size, or is it water-only?
2. Should the cap be removable during Run (a "pull the plug" interaction), or authoring-only as legacy joins were?
3. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420).
