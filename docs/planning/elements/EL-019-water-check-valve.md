# EL-019 · Water check valve — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-019 · Water check valve · Water |
| Requirement anchor | [element-019](../requirements.md#element-019); source record [todo-338](../requirements.md#todo-338); integration task [sequence-task-389](../requirements.md#sequence-task-389) |
| Named entry | [element-019](../invest/named-elements.md#element-019); per-element proof owner S440 |
| CAT spec refined | None. |
| Related identities | Pipe kit [EL-008](EL-008-straight-water-pipe.md)–[EL-012](EL-012-water-pipe-cap.md), [EL-002](EL-002-header-tank.md), [EL-022 Water pump](EL-022-water-pump.md) (prevents back-flow through a stopped pump line), EL-024 Primed siphon |
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
- **Bodies and shapes.** One static body: an inline housing 0.40 m long with a bulged centre, radius 0.18 m; collider one box 0.40 × 0.36 × 0.36 m (**proposed**: the bulge is wider than a pipe so the valve is findable in a pipe run).
- **Mass and material.** Static, zero mass; shared vessel material.
- **Constraints.** None (the flap is not a simulated body).
- **Typed ports.** `WaterInlet` (upstream) and `WaterOutlet` (downstream) standard mouths. Unlike a pipe they are not interchangeable: the declared permitted direction is inlet → outlet. Domain `Water` (**proposed**).
- **Sensors and activation.** None.
- **Work and energy stores.** None. Closed state enum `CheckValveState { Seated, Open }`. Open when the head difference across the valve, inlet minus outlet, exceeds the cracking head; seated otherwise. Open: the orifice law at full bore in the permitted direction. Seated: zero flow in either direction (**proposed** zero reverse leakage: the requirement says reverse head "cannot flow through a closed seat").
- **Parameters.** None authored. Fixed cracking head 0.02 m (**proposed**: small enough that any real head opens it, large enough that f32 noise at equal levels cannot chatter the seat).
- **Cosmetic curves and UI bindings.** Flap angle ← committed state, eased open/closed ([component research](../../component-research.md#water), line 34). A raised flow arrow on the housing shows the permitted direction.
- **Art.** Cream housing `#fff8e9` (`DESIGN.md@a6c914e:L151-L151`), gold arrow `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`), navy collars `#293954` (`DESIGN.md@a6c914e:L147-L147`) (**proposed**: direction by a raised arrow shape, not colour alone).
- **Catalogue and inventory entry.** Id `check_valve`, title "Check valve", category "Water", kind `CheckValve` (all **proposed**).

**Variants.** The requirements row lists no variants.

## Engine capabilities

Families ([element map](../general-engine-element-map.md); binding [`element-01.json`](../../coverage/engine/element-01.json), consumer `element/element-019`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Static box (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset (`engine/gpu/WorkshopSimulation.cs@a6c914e:L238-L245`).
- **Missing.** A directional network node (component research line 34 "direction") and pressure-difference evaluation ([S416](../invest/decisions.md#s416) pressure-work → S419; advection → S418); a committed-state feedback for the flap cosmetic. Unscheduled.
- **Element dependencies.** Two head sources or a supply and a receiver joined by pipes (EL-001/EL-002, EL-008).

## Sources and legacy

- Requirements row: "Reverse head cannot flow through a closed seat." No variants.
- Refinement [S707](../invest/refinements.md#s707) check-valve "Block reverse flow"; integration [sequence-task-389](../requirements.md#sequence-task-389) "prevent backflow"; family boundary [fluids](../invest/profiles.md#fluids) "reversed head does not flow uphill".
- **Legacy search.** No liquid check valve. Gas-domain analogue:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | The gas nozzle supports forward flow only; a back pressure above source pressure is rejected with an exception, and equal pressures give NoFlow. | `engine/physics/ConvergingGasNozzle.cs@a6c914e:L39-L57` | Carry forward "equal head gives no flow". Do not carry forward the exception: under the game-grade envelope reverse head simply seats the valve. |

## Acceptance outline

Point of truth: [element-019](../requirements.md#element-019).

- **Chrome recipe.** Two Water tanks joined by a pipe run with a Check valve in the middle (snapped inline, arrow pointing from tank A to tank B); tank A full, tank B empty.
- **Positive.** Run: water flows A → B until the levels differ by less than the cracking head; the flap shows open, then seats.
- **Negative / control.** Reverse the valve (arrow B → A) with the same tanks: the flap stays seated and nothing flows. Swap fills (A empty, B full) with the original orientation: nothing flows back.
- **Boundaries.** Equal levels: seated, no chatter. Head difference just above 0.02 m opens; just below stays seated. A pump (EL-022) on the A side driving through the valve: flow passes forward; stopping the pump seats the valve and the delivered water stays in B.
- **Run/Reset.** Volumes and state restore exactly. **Save/Load.** Orientation and joins survive reload.
- **Integrations.** [sequence-task-389](../requirements.md#sequence-task-389).

## Open questions

1. Cracking head 0.02 m fixed, or authorable?
2. Zero reverse leakage (proposed) versus a small declared leak?
3. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420). The orifice law and Cd = 0.6 are the S416 advection decision (→ S418).
