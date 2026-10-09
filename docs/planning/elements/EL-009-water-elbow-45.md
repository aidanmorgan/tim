# EL-009 · 45-degree water elbow — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-009 · 45-degree water elbow · Water |
| Requirement anchor | [element-009](../requirements.md#element-009); source record [todo-335](../requirements.md#todo-335); integration task [sequence-task-385](../requirements.md#sequence-task-385) |
| Named entry | [element-009](../invest/named-elements.md#element-009); per-element proof owner S430 |
| CAT spec refined | None. The 45° ball-pipe bend [CAT-049](../requirements.md#current-cat-049) (Story 6.9) is a separate element. |
| Related identities | [EL-008](EL-008-straight-water-pipe.md) (shared pipe-kit values), [EL-010](EL-010-water-elbow-90.md), [EL-011](EL-011-water-t-junction.md), [EL-012](EL-012-water-pipe-cap.md), [EL-013](EL-013-water-nozzle.md) |
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
- **Bodies and shapes.** One static body: a 45° sealed bend, centreline radius 0.40 m, outer radius 0.14 m, standard collar on each end (**proposed**: two bore diameters, close to the legacy ball bend's 2.4 m / 1.3 m ≈ 1.85 bore diameters, `parts/PipeBendPart.cs@a6c914e:L9-L9`). Part origin at the centreline midpoint, as the legacy bend places it (`parts/PipeBendPart.cs@a6c914e:L10-L11`, carry forward). Outside collider: two boxes 0.28 × 0.28 × 0.16 m, one along each half of the arc (**proposed** stand-in; no torus or cylinder collider exists, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`).
- **Mass and material.** Static, zero mass; shared vessel material.
- **Constraints.** None.
- **Typed ports.** `WaterMouthA` and `WaterMouthB` at the arc ends, each offset outward along the end tangent by the 0.06 m collar (the legacy bend offsets its mouths by its 0.09 m collar the same way, `parts/PipeBendPart.cs@a6c914e:L22-L26`). Mouth normals differ by 45°. Domain `Water` (**proposed**).
- **Sensors and activation.** None.
- **Work and energy stores.** None; transit volume π · 0.10² · 0.40 · π/4 ≈ 0.0099 m³, derived from the proposed geometry.
- **Parameters.** None (**proposed**: fixed-angle fitting; orientation by the rotate gizmo). The angle is the identity, not a parameter: 45° here, 90° in EL-010.
- **Cosmetic curves and UI bindings.** None (static); gold seated ring on joined mouths (**proposed**, as EL-008).
- **Art.** Cream bend `#fff8e9` (`DESIGN.md@a6c914e:L151-L151`), navy collars `#293954` (`DESIGN.md@a6c914e:L147-L147`), one raised navy chevron on the outside of the bend to distinguish 45° from 90° without colour (**proposed**, per the shape-cue rule of the [common visual contract](../requirements.md#individual-puzzle-elements)).
- **Catalogue and inventory entry.** Id `water_elbow_45`, title "45° water elbow", category "Water", kind `WaterElbow45` (all **proposed**).

**Variants.** The requirements row lists no variants. The 90° elbow is the separate identity EL-010.

## Engine capabilities

Families ([element map](../general-engine-element-map.md); binding [`element-01.json`](../../coverage/engine/element-01.json), consumer `element/element-009`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Static boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset (`engine/gpu/WorkshopSimulation.cs@a6c914e:L238-L245`).
- **Missing.** Network node and head propagation ([S416](../invest/decisions.md#s416) → S418/S419); mouth joins (EL-001 open question 3). A bend's torus-segment collider is built for balls by Story 6.9 and could replace the box stand-in. Unscheduled.
- **Element dependencies.** EL-008 straight pipes and a supply.

## Sources and legacy

- Requirements row: "Wrong-facing or incompatible joins remain disconnected." No variants.
- Component research pipe-kit row (line 28); refinement [S705](../invest/refinements.md#s705) elbow45.
- **Legacy search.** No liquid elbow. Ball-bend facts:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Bend angle is a closed enum {Degrees45 = 45, Degrees90 = 90}; an undefined value rejects. | `engine/MachineData.cs@a6c914e:L8-L8`; `parts/PipeBendPart.cs@a6c914e:L8-L8`; `parts/PipeBendPart.cs@a6c914e:L29-L29` | Carry forward the closed set as two kinds (EL-009, EL-010), not a parameter. |
| 2 | Centreline radius 2.4 m; origin at the centreline midpoint; mouths at the arc ends offset by the 0.09 m collar along the tangent, bore 0.65 m. | `parts/PipeBendPart.cs@a6c914e:L9-L26` | Carry forward origin and mouth construction; do not carry forward the ball dimensions. |
| 3 | Both bend angles snap mouth-to-mouth to straight pipes and to each other (mouths coincide within 0.0001 m). | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L195-L221` | Carry forward as the kit-wide join acceptance. |
| 4 | Ball 45° bend catalogue: id `pipe_bend_45`, "Nearby matching mouths snap together while building." | `parts/catalog/pipe_bend_45.tres@a6c914e:L6-L13` | Do not carry forward the catalogue (CAT-049); the snap rule is fact 3. |
| 5 | Snap refusals: distant (0.72 m), wrong-facing (40°), occupied mouth, locked part, running world. | `CuriousContraptions.tests/TubePlacementSnapTests.cs@a6c914e:L54-L121` | Carry forward. |

## Acceptance outline

Point of truth: [element-009](../requirements.md#element-009).

- **Chrome recipe.** Water tank + Tap + straight pipe; drag a 45° elbow until `WaterMouthA` snaps to the pipe end; snap a second straight pipe to `WaterMouthB`, ending over a Catch basin.
- **Positive.** Flow turns through 45° and reaches the basin.
- **Negative / control.** Rotate the elbow so `WaterMouthA` points 40° off the pipe end (the legacy wrong-facing case) or away from it: the join is refused, and the route stays open at the first pipe's end (water spills there). Bring a ball-pipe bend (CAT-049) mouth to the water pipe: incompatible bore and domain, refused.
- **Boundaries.** Join at the tolerance edge (0.15 m, 10°) admits; beyond refuses. An occupied mouth refuses a third piece.
- **Run/Reset.** Transit volumes reset to 0. **Save/Load.** Pose and joins survive reload.
- **Integrations.** [sequence-task-385](../requirements.md#sequence-task-385): both elbow angles join by compatible water ports.

## Open questions

1. Centreline radius 0.40 m: confirm.
2. Lossless bends versus a per-fitting head loss.
3. Should incompatible-join refusal show a visible cue (red seam), and is that an existing UI pattern?
4. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420). The orifice law and Cd = 0.6 are the S416 advection decision (→ S418).
