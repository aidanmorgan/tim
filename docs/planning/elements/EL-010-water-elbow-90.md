# EL-010 · 90-degree water elbow — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-010 · 90-degree water elbow · Water |
| Requirement anchor | [element-010](../requirements.md#element-010); source record [todo-335](../requirements.md#todo-335); integration task [sequence-task-385](../requirements.md#sequence-task-385) |
| Named entry | [element-010](../invest/named-elements.md#element-010); per-element proof owner S431 |
| CAT spec refined | None. The 90° ball-pipe bend [CAT-050](../requirements.md#current-cat-050) (Story 6.10) is a separate element. |
| Related identities | [EL-008](EL-008-straight-water-pipe.md) (shared pipe-kit values), [EL-009](EL-009-water-elbow-45.md), [EL-011](EL-011-water-t-junction.md), [EL-012](EL-012-water-pipe-cap.md) (blocks its outlet), [EL-002](EL-002-header-tank.md) (risers) |
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
- **Bodies and shapes.** One static body: a 90° sealed bend, centreline radius 0.40 m, outer radius 0.14 m, standard collar on each end (**proposed**: the same radius as EL-009 so a 90° bend equals two 45° bends in footprint). Origin at the centreline midpoint (legacy construction, `parts/PipeBendPart.cs@a6c914e:L10-L11`). Outside collider: two boxes 0.28 × 0.28 × 0.32 m, one per half-arc (**proposed** stand-in; no torus or cylinder collider, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`).
- **Mass and material.** Static, zero mass; shared vessel material.
- **Constraints.** None.
- **Typed ports.** `WaterMouthA` and `WaterMouthB` at the arc ends, offset by the 0.06 m collar along the end tangents (legacy pattern, `parts/PipeBendPart.cs@a6c914e:L22-L26`); normals at 90°. Domain `Water` (**proposed**).
- **Sensors and activation.** None.
- **Work and energy stores.** None; transit volume π · 0.10² · 0.40 · π/2 ≈ 0.0197 m³, derived from the proposed geometry.
- **Parameters.** None (**proposed**: fixed-angle fitting).
- **Cosmetic curves and UI bindings.** None (static); gold seated ring on joined mouths (**proposed**, as EL-008).
- **Art.** Cream bend `#fff8e9` (`DESIGN.md@a6c914e:L151-L151`), navy collars `#293954` (`DESIGN.md@a6c914e:L147-L147`), two raised navy chevrons on the outside of the bend (EL-009 has one) (**proposed**: angle readable without colour).
- **Catalogue and inventory entry.** Id `water_elbow_90`, title "90° water elbow", category "Water", kind `WaterElbow90` (all **proposed**).

**Variants.** The requirements row lists no variants. The 45° elbow is the separate identity EL-009.

## Engine capabilities

Families ([element map](../general-engine-element-map.md); binding [`element-01.json`](../../coverage/engine/element-01.json), consumer `element/element-010`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Static boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset (`engine/gpu/WorkshopSimulation.cs@a6c914e:L238-L245`).
- **Missing.** Network node and head propagation ([S416](../invest/decisions.md#s416) → S418/S419); mouth joins (EL-001 open question 3); Story 6.10's ball-bend torus collider could replace the box stand-in. Unscheduled.
- **Element dependencies.** EL-008 pipes, EL-012 cap (control), a supply.

## Sources and legacy

- Requirements row: "Blocking its outlet prevents through-flow." No variants.
- Component research pipe-kit row (line 28); refinement [S705](../invest/refinements.md#s705) elbow90.
- **Legacy search.** No liquid elbow. Ball-bend facts:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Bend angle closed enum {Degrees45, Degrees90}; the legacy default bend is 90°; an undefined value rejects. | `engine/MachineData.cs@a6c914e:L8-L8`; `parts/PipeBendPart.cs@a6c914e:L8-L8`; `parts/PipeBendPart.cs@a6c914e:L29-L29` | Carry forward the closed set as two kinds. |
| 2 | Centreline radius 2.4 m, origin at the centreline midpoint, mouths offset by a 0.09 m collar along the tangent. | `parts/PipeBendPart.cs@a6c914e:L9-L26` | Carry forward the construction; not the ball dimensions. |
| 3 | Bends of both angles snap to straight pipes and to each other. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L195-L221` | Carry forward. |
| 4 | Ball 90° bend catalogue `pipe_bend_90`, Motion category, clear-pipe colour. | `parts/catalog/pipe_bend_90.tres@a6c914e:L6-L13` | Do not carry forward (CAT-050). |
| 5 | Occupied mouths are not join candidates; deleting the occupant frees the mouth. | `CuriousContraptions.tests/TubePlacementSnapTests.cs@a6c914e:L103-L121` | Carry forward (the cap occupies the outlet in the control below). |

## Acceptance outline

Point of truth: [element-010](../requirements.md#element-010).

- **Chrome recipe.** Header tank (EL-002) outlet → straight pipe down → 90° elbow → horizontal straight pipe ending over a Catch basin; all joined by snapping mouths.
- **Positive.** Flow turns the corner and fills the basin.
- **Negative / control.** Snap a Cap (EL-012) on the elbow's `WaterMouthB` instead of the horizontal pipe: no through-flow; the riser fills to the tank level and stops; the basin stays empty. Delete the cap: flow resumes from the open mouth.
- **Boundaries.** Join tolerance edges as EL-008; wrong-facing joins refused.
- **Run/Reset.** Transit volumes reset to 0. **Save/Load.** Pose and joins survive reload.
- **Integrations.** [sequence-task-385](../requirements.md#sequence-task-385).

## Open questions

1. Centreline radius shared with EL-009 (0.40 m): confirm.
2. Lossless bends versus a per-fitting head loss.
3. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420). The orifice law and Cd = 0.6 are the S416 advection decision (→ S418).
