# EL-013 · Water nozzle — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-013 · Water nozzle · Water |
| Requirement anchor | [element-013](../requirements.md#element-013); source record [todo-335](../requirements.md#todo-335); integration task [sequence-task-385](../requirements.md#sequence-task-385) |
| Named entry | [element-013](../invest/named-elements.md#element-013); per-element proof owner S434 |
| CAT spec refined | None. |
| Related identities | [EL-008](EL-008-straight-water-pipe.md) (shared pipe-kit values), [EL-002](EL-002-header-tank.md) (head source), [EL-022](EL-022-water-pump.md) (pumped head), [EL-004](EL-004-catch-basin.md) (target), EL-036 Sprinkler (distributed source, separate) |
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
- **Bodies and shapes.** One static body: a converging cone 0.30 m long from the 0.10 m bore (inside the standard collar) to an exit radius 0.04 m (**proposed**: a (0.10 / 0.04)² = 6.25 : 1 area ratio gives a thin, readable jet). Collider: one box 0.32 × 0.32 × 0.30 m (**proposed** stand-in; no cone collider, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`).
- **Mass and material.** Static, zero mass; shared vessel material.
- **Constraints.** None.
- **Typed ports.** `WaterInlet` standard mouth (joins a pipe) and `JetOutlet`, a non-joinable free-stream emitter on the axis at the exit plane. Domain `Water` (**proposed**).
- **Sensors and activation.** None.
- **Work and energy stores.** None; the nozzle converts the supplied head into jet speed only. Jet exit speed v = Cv · √(2 g Δh), Cv = 0.95 (**proposed**: typical smooth-nozzle velocity coefficient; keeps v strictly below the frictionless value so no reach is invented), where Δh is the supply surface above the exit. Flow Q = A_exit · v. Jet packets launch along the axis at v and then fly under gravity; v ≤ 64 m/s is guaranteed by any bench-scale head (the [envelope](../../gpu-f32-physics.md#game-grade-envelope) clamp never engages).
- **Rated reach (derived, not authored).** At a rated head of 2.0 m (**proposed**: what a full EL-002 at StandHeight 1.0 — surface 2.15 m above the bench — gives above an exit at 0.14 m, the axis height of a pipe lying on the bench): v ≈ 5.95 m/s, Q ≈ 0.030 m³/s (0.48 kg/s), momentum rate ρw · Q · v ≈ 2.85 N, and a 45° throw returns to exit height at v²/g ≈ 3.6 m. Reach scales with Δh, so half the head gives about half the reach.
- **Parameters.** None (**proposed**: direction set by the rotate gizmo; the rated reach is a label derived from the rated head).
- **Cosmetic curves and UI bindings.** Jet ribbon follows committed packets; cosmetic spray never decides capture ([component research](../../component-research.md#water), line 20). Selected-only reach preview arc at the current head (**proposed**, following the "selected-only footprint preview" pattern of line 44).
- **Art.** Gold cone `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`) with a navy tip ring `#293954` (`DESIGN.md@a6c914e:L147-L147`) (**proposed**: the active end is gold like other operational cues, `DESIGN.md@a6c914e:L189-L189`).
- **Catalogue and inventory entry.** Id `water_nozzle`, title "Water nozzle", category "Water", kind `WaterNozzle` (all **proposed**).

**Variants.** The requirements row lists no variants.

## Engine capabilities

Families ([element map](../general-engine-element-map.md); binding [`element-01.json`](../../coverage/engine/element-01.json), consumer `element/element-013`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Static box (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); constant gravity for ballistic flight (`engine/gpu/WorkshopConstruction.cs@a6c914e:L123-L124`); the speed envelope ([game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope)).
- **Missing.** Free-stream packets with initial velocity and swept capture ([S416](../invest/decisions.md#s416) advection → S418); head-to-speed conversion (pressure-work → S419). Jet impact on bodies needs ContactImpulse coupling, which is not in this element's family list (see Open questions). Unscheduled.
- **Element dependencies.** A head source (EL-002 via pipes, or EL-022); EL-004 target.

## Sources and legacy

- Requirements row: "Insufficient head cannot produce the rated reach." No variants.
- Refinement [S705](../invest/refinements.md#s705) nozzle "Convert pressure to a declared outlet jet"; component research pipe-kit row (line 28) and free-stream packet row (line 15).
- **Legacy search.** No liquid nozzle. The gas-domain nozzle gives analogue facts:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Zero area, or equal upstream and back pressure, gives the NoFlow regime with zero mass rate and speed. | `engine/physics/ConvergingGasNozzle.cs@a6c914e:L39-L57` | Carry forward: zero head gives no jet. |
| 2 | Back pressure above source pressure throws ("forward flow only"). | `engine/physics/ConvergingGasNozzle.cs@a6c914e:L54-L55` | Do not carry forward the exception (game-grade clamp-or-continue): reverse head simply gives no forward jet. |
| 3 | Mass rate = area × density × speed; momentum rate = mass rate × speed; thrust adds the pressure force. | `engine/physics/ConvergingGasNozzle.cs@a6c914e:L76-L81` | Carry forward mass rate and momentum rate. Do not carry forward the compressible (choked, adiabatic) law: liquid is incompressible, v = Cv √(2 g Δh). |

## Acceptance outline

Point of truth: [element-013](../requirements.md#element-013).

- **Chrome recipe.** Header tank (EL-002) at StandHeight 1.0 and InitialVolume 0.25 (full surface 2.15 m, head ≈ 2.0 m above the exit) → pipes along the bench → Water nozzle aimed 45° up with the rotate gizmo (exit 0.14 m above the bench). Catch basin centred 2.9 m from the exit, long side (outer 1.49 m, rim aperture 1.25 m) along the jet: outer near wall at 2.155 m, rim aperture 2.275–3.525 m.
- **Positive.** At rated head the jet passes the near wall about 1.0 m above the bench (wall top 0.55 m), crosses the rim plane at ≈ 3.14 m — inside the aperture — and lands inside the basin (it meets the far inner wall at ≈ 0.22 m, above the 0.15 m floor). The basin catches the jet while the head stays above ≈ 1.54 m, about the first 0.115 m³ the header tank releases.
- **Negative / control.** Header tank at StandHeight 0.5 with InitialVolume 0.125 (surface 1.15 m, head ≈ 1.0 m): v ≈ 4.23 m/s and the jet lands on the bench ≈ 1.95 m from the exit, short of the basin's near wall; the basin stays empty — insufficient head cannot reach. Empty tank: no jet.
- **Boundaries.** As the tank drains the jet visibly shortens; once the head falls below ≈ 1.54 m the rim crossing moves inside 2.275 m and the jet strikes the near wall or the bench. Reach never exceeds the value for the current head. A capped nozzle inlet emits nothing.
- **Run/Reset.** Exact restoration of volumes and packets. **Save/Load.** Pose, aim and joins survive reload.
- **Integrations.** [sequence-task-385](../requirements.md#sequence-task-385) (source record [todo-335](../requirements.md#todo-335)): nozzles join by compatible water ports with the straight sections, elbows, T and cap. Generic interaction rows [IX-08 Fluid advection](../requirements.md#interaction-08) and [IX-09 Pressure work](../requirements.md#interaction-09) (head converted to jet speed). Campaign first use 61–70, reuse 71–90, 114, 126–130, 136–150 ([campaign table](../requirements.md#campaign-element-coverage)).

## Open questions

1. Does the jet push bodies (momentum transfer ≈ 2.85 N at rated head)? If yes, add ContactImpulse to the element's families (owner S416/S418).
2. Exit radius 0.04 m and Cv 0.95: confirm.
3. Is the "rated reach" shown to players as a number or only as the selected-only preview arc?
4. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420). Free-stream packet volume and budget are unspecified — owner S418.
