# EL-011 · Water T junction — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-011 · Water T junction · Water |
| Requirement anchor | [element-011](../requirements.md#element-011); source record [todo-335](../requirements.md#todo-335); integration task [sequence-task-385](../requirements.md#sequence-task-385) |
| Named entry | [element-011](../invest/named-elements.md#element-011); per-element proof owner S432 |
| CAT spec refined | None. |
| Related identities | [EL-008](EL-008-straight-water-pipe.md) (shared pipe-kit values), [EL-012](EL-012-water-pipe-cap.md), [EL-020 Water diverter](EL-020-water-diverter.md) (selects one branch instead of sharing), EL-030 Flow meter |
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
- **Bodies and shapes.** One static body: a run tube 0.60 m long between collars and a perpendicular branch 0.30 m from the run axis to its collar, outer radius 0.14 m (**proposed**: shortest T in which three collars do not overlap). Colliders: two boxes, 0.28 × 0.28 × 0.60 m (run) and 0.28 × 0.16 × 0.28 m (branch stub) (**proposed** stand-ins; no cylinder collider, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`).
- **Mass and material.** Static, zero mass; shared vessel material.
- **Constraints.** None.
- **Typed ports.** Three standard mouths: `WaterMouthA` and `WaterMouthB` on the run, `WaterMouthBranch` on the stub. All three are symmetric: any may be the inlet (it can also merge two flows). Domain `Water` (**proposed**).
- **Sensors and activation.** None.
- **Work and energy stores.** None. Split law: each outgoing branch's demand follows the orifice law from the junction head to that branch's downstream surface; when total demand exceeds the supply, every branch is scaled by the same factor α = supply / demand, all evaluated from one substep snapshot so insertion order never selects a branch ([component research](../../component-research.md#water), line 14; the same α rule as the envelope's power scaling, [envelope](../../gpu-f32-physics.md#game-grade-envelope)). Transit volume ≈ 0.0314 × 0.90 ≈ 0.028 m³ (derived).
- **Parameters.** None (**proposed**: a passive fitting; any branch throttling is EL-003's job).
- **Cosmetic curves and UI bindings.** None (static) (component research line 28); gold seated rings on joined mouths (**proposed**, as EL-008).
- **Art.** Cream body `#fff8e9` (`DESIGN.md@a6c914e:L151-L151`), navy collars `#293954` (`DESIGN.md@a6c914e:L147-L147`) (**proposed** reuse).
- **Catalogue and inventory entry.** Id `water_tee`, title "Water T", category "Water", kind `WaterTee` (all **proposed**).

**Variants.** The requirements row lists no variants.

## Engine capabilities

Families ([element map](../general-engine-element-map.md); binding [`element-01.json`](../../coverage/engine/element-01.json), consumer `element/element-011`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Static boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); proportional scaling of demand against a finite supply exists for contact work only (`engine/gpu/ContactWorkDeclaration.cs@a6c914e:L21-L36`).
- **Missing.** Split/merge transport preserving mass at "split/merge boundaries" — the exact question of [S416](../invest/decisions.md#s416) advection → S418; head (S419); mouth joins (EL-001 open question 3). Unscheduled.
- **Element dependencies.** EL-008 pipes, a supply, two receivers (EL-004); EL-012 for the capped-branch control.

## Sources and legacy

- Requirements row: "Branch totals cannot exceed supply." No variants.
- Component research: "a split shares supply, never copies it" (line 13); "shared supply/capacity resolved before commit, so insertion order never selects a branch" (line 14); recipe Two Gardens (line 53). Refinement [S705](../invest/refinements.md#s705) tee. Integration [sequence-task-385](../requirements.md#sequence-task-385): "splitting divides the available flow".
- **Legacy search.** No liquid junction. Finite-supply allocation analogue:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Several sources sharing one finite store are allocated simultaneously: each request is scaled by a common factor so total work ≤ stored energy; allocation never debits the store. | `engine/physics/StoredFlowSource.cs@a6c914e:L28-L73` | Carry forward the law (common scale, order-independent, prediction separate from commit). Do not carry forward the f64 CPU allocator. |
| 2 | Acceptance: two requests against a 4 J store over 1 s get 2 J each; the result is identical when the request order is reversed; repeating the allocation gives the same result. | `CuriousContraptions.tests/StoredFlowSourceTests.cs@a6c914e:L17-L40` | Carry forward as "branch order and authoring order never change the split". |
| 3 | An empty store or a zero-speed request yields zero force and zero work. | `CuriousContraptions.tests/StoredFlowSourceTests.cs@a6c914e:L42-L54` | Carry forward as "an empty supply splits nothing". |

## Acceptance outline

Point of truth: [element-011](../requirements.md#element-011).

- **Chrome recipe.** Water tank + Tap + pipe into a T's `WaterMouthA`; snap pipes to `WaterMouthB` and `WaterMouthBranch`, each ending over its own Catch basin at the same height.
- **Positive.** Both basins fill; their combined gain equals the tank's loss minus transit; equal outlets give equal fills.
- **Negative / control.** Cap (EL-012) the branch: all flow goes to B and the total is unchanged (never doubled). Empty tank: neither basin gains.
- **Boundaries.** Rebuild the same construction placing the branch pipe first: identical fills (order independence). One outlet lower than the other: the lower receives more, total still equals supply. Merge use (two supplies into A and Branch, out of B): output equals the sum of inputs.
- **Run/Reset.** Exact restoration. **Save/Load.** Pose and joins survive reload.
- **Integrations.** [sequence-task-385](../requirements.md#sequence-task-385) (source record [todo-335](../requirements.md#todo-335)): T junctions join by compatible water ports and splitting divides the available flow. Generic interaction rows [IX-08 Fluid advection](../requirements.md#interaction-08) and [IX-09 Pressure work](../requirements.md#interaction-09). Campaign first use 61–70, reuse 71–90, 114, 126–130, 136–150 ([campaign table](../requirements.md#campaign-element-coverage)).

## Open questions

1. Should a T ever split unequally by geometry (run versus branch loss), or only by downstream head (proposed)?
2. Equal-split tolerance at f32: proposed exact equality for symmetric constructions — confirm.
3. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420). The orifice law and Cd = 0.6 are the S416 advection decision (→ S418).
