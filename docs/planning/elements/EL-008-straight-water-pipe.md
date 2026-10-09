# EL-008 · Straight water pipe — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-008 · Straight water pipe · Water |
| Requirement anchor | [element-008](../requirements.md#element-008); source record [todo-335](../requirements.md#todo-335); integration task [sequence-task-385](../requirements.md#sequence-task-385) |
| Named entry | [element-008](../invest/named-elements.md#element-008); per-element proof owner S429 |
| CAT spec refined | None. The clear ball pipe [CAT-048](../requirements.md#current-cat-048) (Stories 6.6–6.7) is a separate element: it carries balls by contact, this one carries liquid in the network. |
| Related identities | Pipe kit: [EL-009](EL-009-water-elbow-45.md), [EL-010](EL-010-water-elbow-90.md), [EL-011](EL-011-water-t-junction.md), [EL-012](EL-012-water-pipe-cap.md), [EL-013](EL-013-water-nozzle.md); supplies [EL-001](EL-001-finite-reservoir.md), [EL-002](EL-002-header-tank.md) |
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

**Shared pipe-kit values** (EL-008 to EL-013):
- Mouth join: two standard mouths join when their positions lie within 0.15 m and their outward normals are within 10° of opposite; the join then snaps them coincident (**proposed**: legacy ball pipes snapped from 0.12 m along, 0.05 m across and 0.08 rad off, and refused 0.72 m and 40°, see Sources facts 1–3).
- Sealed conduits are lossless: head passes unchanged through any joined chain (**proposed**: keeps "a sealed pipe carries head uphill" readable; [component research](../../component-research.md#water), line 13).
- Water in transit inside a conduit = bore area × centreline length, counted in the ledger's "in transit" term (line 12).
- An unjoined mouth that water reaches discharges as a free stream by the orifice law at full bore; it never feeds a neighbour it is not joined to.

**Element declaration:**
- **Bodies and shapes.** One static body: a sealed tube, outer radius 0.14 m, standard bore, with a standard collar at each end (**proposed**: wall 0.04 m reads as solid toy tubing). No cylinder collider exists (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`); the outside is one box 0.28 × 0.28 × L m (**proposed** stand-in until a capsule/cylinder shape lands). Balls never enter the bore.
- **Mass and material.** Static, zero mass; shared vessel material.
- **Constraints.** None.
- **Typed ports.** `WaterMouthA` and `WaterMouthB`, standard mouths on the axis at ±(L/2 + 0.06) m facing outward. Symmetric: either end may be inlet. Domain `Water` (**proposed**).
- **Sensors and activation.** None.
- **Work and energy stores.** None; transit volume π · 0.10² · L (0.063 m³, about one step, at the default 2 m).
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Length | f32 | 0.5–6.0, step 0.25 | 2.0 | m | parameter named by component research line 28; range/default **proposed** (short joins down to 0.5 m; a 2 m default spans a tank-to-basin gap) |

- **Cosmetic curves and UI bindings.** None (static) ([component research](../../component-research.md#water), line 28); collars show seated versus open state by a gold seam ring on joined mouths (**proposed**).
- **Art.** Cream tube `#fff8e9` (`DESIGN.md@a6c914e:L151-L151`), navy collars `#293954` (`DESIGN.md@a6c914e:L147-L147`), gold seated ring `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`) (**proposed**: opaque cream distinguishes it from the transparent cyan ball pipe, `DESIGN.md@a6c914e:L181-L181`).
- **Catalogue and inventory entry.** Id `water_pipe`, title "Water pipe", category "Water", kind `WaterPipe` (all **proposed**).

**Variants.** The requirements row lists no variants. Elbows, T, cap and nozzle are separate identities EL-009 to EL-013.

## Engine capabilities

Families ([element map](../general-engine-element-map.md); binding [`element-01.json`](../../coverage/engine/element-01.json), consumer `element/element-008`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Static box body (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); resizable-length pattern (parked ball-pipe `PipeDimensions` clamps length 1–8 m, `engine/gpu/WorkshopPipe.cs@a6c914e:L6-L29`, excluded from the build by `CuriousContraptions.csproj@a6c914e:L29-L29`); typed connections without a Water domain (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L12`), capacity 8 edges (`engine/gpu/WorkshopConnections.cs@a6c914e:L65-L67`).
- **Missing.** Water network nodes and head propagation ([S416](../invest/decisions.md#s416) advection → S418, pressure-work → S419); mouth-join TopologyTransaction (EL-001 open question 3); the connection capacity of 8 is too small for a pipe network. Unscheduled.
- **Element dependencies.** A supply with an outlet mouth (EL-001, EL-002, EL-003); a receiver (EL-004) or nozzle (EL-013).

## Sources and legacy

- Requirements row: "A disconnected mouth cannot feed its neighbour." No variants.
- Component research "Water pipe kit (EL-008–013)": sealed network nodes, parameter length, compatible-mouth snapping with seated collars (line 28). Refinement [S705](../invest/refinements.md#s705) straight. Family boundary [fluids](../invest/profiles.md#fluids).
- **Legacy search.** No liquid pipe exists. The ball pipe's mouth-snapping tests give carry-forward join rules:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | A pipe placed 0.12 m along and 0.05 m across from the joined position, rotated 0.08 rad, snaps to coincident mouths (< 0.0001 m apart, normals dot < −0.9999); the query does not mutate the part; once joined, the same part no longer reports a snap. | `CuriousContraptions.tests/TubePlacementSnapTests.cs@a6c914e:L15-L52` | Carry forward as the join acceptance (coincident, opposite, idempotent, query is pure). Do not carry forward the ball-travel assertions or Godot types. |
| 2 | Snapping is refused while running and for parts of another world; it works again after Reset; running leaves pose and bodies unchanged. | `CuriousContraptions.tests/TubePlacementSnapTests.cs@a6c914e:L54-L85` | Carry forward: joins are an authoring-only transaction. |
| 3 | Mouths 0.72 m apart, or 40° wrong-facing, do not snap. | `CuriousContraptions.tests/TubePlacementSnapTests.cs@a6c914e:L87-L101` | Carry forward as the wrong-facing / distant control. |
| 4 | An occupied mouth and a locked part are not snap candidates; deleting the occupant frees the mouth. | `CuriousContraptions.tests/TubePlacementSnapTests.cs@a6c914e:L103-L121` | Carry forward. |
| 5 | Ball-pipe catalogue: length bits 17203 (binary16 3.5996 m, i.e. 3.6 m), title "Clear pipe", colour (0.40, 0.72, 0.79). | `parts/catalog/pipe.tres@a6c914e:L8-L20` | Do not carry forward (CAT-048 owns it; binary16 lane). Recorded so the water pipe is not confused with it. |

## Acceptance outline

Point of truth: [element-008](../requirements.md#element-008).

- **Chrome recipe.** Place a Water tank and Tap; drag a Water pipe until `WaterMouthA` snaps to the tap's `WaterOutlet` (seated ring appears); resize it with the length handle; place a Catch basin under `WaterMouthB`.
- **Positive.** Water travels the pipe and discharges from mouth B into the basin; a rising pipe still delivers while its outlet is below the tank surface.
- **Negative / control.** Move the pipe 0.3 m away from the tap before Run (mouth not joined): the tap discharges onto the pipe's outside and the tray; the pipe carries nothing. A second pipe beside, not joined, to mouth B receives nothing.
- **Boundaries.** Join at 0.15 m / 10° admits; 0.2 m or 15° refuses. An occupied mouth refuses a third pipe. Length 0.5 and 6.0 admit; outside rejects. Join attempts during Run are refused and change nothing.
- **Run/Reset.** Transit volume returns to 0 exactly. **Save/Load.** Length, pose and joins survive reload.
- **Integrations.** [sequence-task-385](../requirements.md#sequence-task-385): kit pieces join by compatible water ports; a capped route cannot leak through an invisible connection.

## Open questions

1. Lossless pipes versus a per-length head loss (affects EL-002 lift and EL-013 reach).
2. Join tolerance 0.15 m / 10°: confirm.
3. Connection capacity (8 today) must grow for water networks; what bound?
4. Should a water pipe be a hollow collider so balls can also pass, or stay a solid outside?
5. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420). The orifice law and Cd = 0.6 are the S416 advection decision (→ S418).
