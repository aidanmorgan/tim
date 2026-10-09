# EL-022 · Water pump — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-022 · Water pump · Water |
| Requirement anchor | [element-022](../requirements.md#element-022); source record [todo-338](../requirements.md#todo-338); integration task [sequence-task-389](../requirements.md#sequence-task-389) |
| Named entry | [element-022](../invest/named-elements.md#element-022); per-element proof owner S443 |
| CAT spec refined | None. Supplied by the Battery ([CAT-005](CAT-005-battery.md), Story 8.1) or driven by a shaft such as the Motor's ([CAT-042](CAT-042-motor.md), Story 11.1). |
| Related identities | [EL-001 Finite reservoir](EL-001-finite-reservoir.md) (intake store), [EL-002 Header tank](EL-002-header-tank.md) (lifted store), [EL-019 Check valve](EL-019-water-check-valve.md), [EL-018 Level switch](EL-018-electronic-level-switch.md) (controls it), [EL-013](EL-013-water-nozzle.md), EL-023 Archimedes screw (separate mechanical lifter), waterwheel [todo-336](../requirements.md#todo-336) |
| Roadmap story | Unscheduled; ELEMENT-n row of the [roadmap](../invest/vertical-delivery.md#rolling-playable-roadmap), campaign chapter 7, levels 61–70. Supply prerequisites: Stories 8.1 and 11.1. |
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
- **Bodies and shapes.** One static body: a pump housing box 0.50 × 0.40 × 0.40 m on a navy foot, with a round impeller window on the front (cosmetic) (**proposed**: about the size of a Delay box, so it sits beside a tank).
- **Mass and material.** Static, zero mass; shared vessel material.
- **Constraints.** None (the impeller is cosmetic; no simulated rotor).
- **Typed ports.**
  - `WaterInlet` (intake, low on the left) and `WaterOutlet` (discharge, high on the right), standard mouths, domain `Water` (**proposed**);
  - `PowerIn`, Electrical, Input (existing enum members, `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`);
  - `ShaftIn`, Mechanical, Input (**proposed**: the element map lists ShaftTorque; no shaft socket exists in today's `WorkshopSocket`, and the legacy domain set had Mechanical, `engine/MachineData.cs@a6c914e:L68-L68`).
  Exactly one input may be connected at a time; connecting both rejects (**proposed**: keeps the energy source unambiguous).
- **Sensors and activation.** None. Visible states: closed enum `PumpState { Unpowered, DryIdle, Pumping }`.
- **Work and energy stores.** None owned; the pump converts supplied work into head. Pump curve Q = Q_max · (1 − H / H_max), with H the head lifted from intake surface to delivery surface (**proposed**: the simplest monotone curve that stalls at a finite head). Hydraulic power ρw · g · Q · H must not exceed η · P_in with η = 0.5 (**proposed**: an obvious loss so no wheel → pump loop can break even); if it would, Q is scaled down (the [envelope](../../gpu-f32-physics.md#game-grade-envelope) α rule). P_in is the supply's available power: the battery network for `PowerIn`, τ · ω from the shaft for `ShaftIn`. Dry intake (no liquid at the inlet): Q = 0 and no input power is drawn (DryIdle). Unpowered: the pump is a closed node — no through-flow either way (**proposed**: makes "unpowered cannot sustain uphill flow" unambiguous and blocks siphoning back through it).
- **Parameters.** Fixed by kind (**proposed**: one rated pump, as ball materials are fixed by kind):

  | Name | Type | Value | Unit | Source |
  | --- | --- | --- | --- | --- |
  | MaxFlow Q_max | f32 | 0.0625 | m³/s | "rate" named by component research line 35; value **proposed** (one 1 kg step per second at zero lift) |
  | MaxHead H_max | f32 | 3.0 | m | "lift head" named by component research line 35; value **proposed** (from a full EL-001, surface 1.15 m above the bench, into an EL-002 at StandHeight 1.0, notch 2.15 m, the lift is 1.0–1.25 m: a margin of at least 1.75 m below H_max; an EL-002 at StandHeight 3.0, notch 4.15 m, is out of reach) |
  | RatedInput | f32 | 15 | W | **proposed**: η · 15 W = 7.5 W covers the curve's peak hydraulic power ρw g (Q_max/2)(H_max/2) ≈ 7.4 W |

- **Cosmetic curves and UI bindings.** Impeller spin ← committed flow rate ([component research](../../component-research.md#water), line 35 "screw/impeller ← committed rate"); state lamp slate `#556573` (unpowered) / pale green `#bdf4bd` (dry idle) / gold `#f7cb52` (pumping) (`DESIGN.md@a6c914e:L277-L278`, `DESIGN.md@a6c914e:L154-L154`) (**proposed**, plus impeller motion so state is not colour-only).
- **Art.** Electric-motor teal `#66b8c9` housing (`DESIGN.md@a6c914e:L180-L180`), cream window ring `#fff8e9` (`DESIGN.md@a6c914e:L151-L151`), navy foot `#293954` (`DESIGN.md@a6c914e:L147-L147`) (**proposed**: a powered machine reads as a relative of the Motor).
- **Catalogue and inventory entry.** Id `water_pump`, title "Water pump", category "Water", kind `WaterPump` (all **proposed**).

**Variants.** The requirements row lists no variants. Electrical versus shaft drive are two input ports of one element, not variants; the Archimedes screw is the separate identity EL-023.

## Engine capabilities

Families ([element map](../general-engine-element-map.md); binding [`element-01.json`](../../coverage/engine/element-01.json), consumer `element/element-022`): ElectricalPower, EnvironmentState, FiniteLedger, FiniteWorkActuation, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, ShaftTorque, StateTransaction, TopologyTransaction.

- **Exists now.** Finite work debiting with proportional scaling, for contact work only (`engine/gpu/ContactWorkDeclaration.cs@a6c914e:L21-L36`); Electrical enum members (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`); Run/Reset (`engine/gpu/WorkshopSimulation.cs@a6c914e:L238-L245`).
- **Missing.** ElectricalPower network (Story 8.1; [S257](../invest/decisions.md#s257) electrical-port → S270); ShaftTorque input (Story 11.1; S257 mechanical-port → S690); FiniteWorkActuation applied to a fluid node; head-raising PressureWork ([S416](../invest/decisions.md#s416) pressure-work → S419) and advection (S418). Unscheduled.
- **Element dependencies.** CAT-005 Battery (electric) or CAT-042 Motor shaft; an intake store feeding `WaterInlet` through a pipe (EL-001's `WaterOutlet` + EL-008); pipes and an elevated receiver (EL-002); EL-019 to hold delivered water when stopped (optional, since the unpowered pump is closed).

## Sources and legacy

- Requirements row: "Unpowered pump cannot sustain uphill flow." No variants.
- Refinement [S707](../invest/refinements.md#s707) pump "Convert finite supplied work to head"; component research: "wheel → pump loops conserve energy, so there are no perpetual fountains" (line 20), "a dry intake is visibly idle" (line 35); recipes Up and Over, Prime Time, Dark at High Tide (lines 54, 55, 59); hydraulic piston test list "no perpetual wheel/pump loop" ([todo-428](../requirements.md#todo-428)).
- **Legacy search.** No liquid pump. Finite-work analogues:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Stored-flow sources sharing one finite store are scaled by a common factor so total work over the step never exceeds stored energy; allocation is order-independent and never debits during prediction. | `engine/physics/StoredFlowSource.cs@a6c914e:L28-L73` | Carry forward the budget law (α scaling, order independence). Do not carry forward the f64 CPU allocator or its exceptions. |
| 2 | Acceptance: an empty store or a stationary source yields zero force and zero work; force rating and power rating cap the effort independently. | `CuriousContraptions.tests/StoredFlowSourceTests.cs@a6c914e:L42-L54` | Carry forward as "dry or unpowered pump moves nothing; the rated input caps hydraulic power". |
| 3 | A finite store's debit may never exceed its contents; capacity and initial energy are validated at declaration. | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L7-L19`; `engine/physics/PhysicsEnergyStore.cs@a6c914e:L38-L52` | Carry forward for the battery-side energy the pump consumes. |
| 4 | Legacy connection domains included Mechanical (for shafts) but no water domain. | `engine/MachineData.cs@a6c914e:L68-L68` | Recorded: `ShaftIn` and the Water domain are new typed sockets. |

## Acceptance outline

Point of truth: [element-022](../requirements.md#element-022).

- **Chrome recipe.** Water tank (EL-001) on the bench with InitialVolume 1.0 (surface 1.15 m above the bench); snap a straight pipe from its `WaterOutlet` to the pump's `WaterInlet`; pipe from the pump's `WaterOutlet` up to the open top of a Header tank (EL-002) at StandHeight 1.0, InitialVolume 0 (notch 2.15 m above the bench); wire Battery `Supply` → pump `PowerIn`.
- **Positive.** Run: the impeller spins, the tank level falls and the header tank fills its 0.25 m³ in about 6.5 s, then spills at its notch. The lift runs from 1.0 m to 1.25 m (the tank drops 0.25 m), so Q falls from about 0.042 to 0.036 m³/s and the hydraulic power, 6.5–7.2 W, stays under η · 15 W = 7.5 W: the pump follows its curve and is not power-limited. The battery's energy falls by at least ρw · g · V · H / η ≈ 88 J for the 0.25 m³ lifted.
- **Negative / control.** Battery unwired: the pump is closed; nothing rises, and the full tank cannot drain through it either. EL-001 InitialVolume 0: DryIdle, no battery drain.
- **Boundaries.** Header tank at StandHeight 3.0 (notch 4.15 m, lift at least 3.0 m from the full tank): flow stalls at H_max and nothing is delivered. A wheel-driven pump whose wheel is fed by the pumped water slows and stops (no perpetual loop). Connecting both `PowerIn` and `ShaftIn` is refused.
- **Run/Reset.** Volumes, state and battery energy restore exactly. **Save/Load.** Pose, joins and wires survive reload.
- **Integrations.** [sequence-task-389](../requirements.md#sequence-task-389): lift water with power; Up and Over; Dark at High Tide (level switch stops a supplied pump).

## Open questions

1. Both electrical and shaft inputs on one pump (proposed), or two pump kinds?
2. Pump curve, η = 0.5 and RatedInput 15 W: confirm; battery capacity (CAT-005) must cover at least the ≈ 88 J of the recipe.
3. Should an unpowered pump pass flow downhill (open) instead of closing (proposed closed)?
4. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420). The orifice law and Cd = 0.6 are the S416 advection decision (→ S418).
