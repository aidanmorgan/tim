# GAP-04 · Driven wheel — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | GAP-04 · Driven wheel · Mechanical (P2 potential) |
| Anchor | [requirements.md#gap-04](../requirements.md#gap-04); [named-elements entry](../invest/named-elements.md#gap-04) |
| Related identities | Extends [EL-116 Passive wheel](EL-116-passive-wheel.md) and [EL-117 Axle](EL-117-axle.md) (same silhouette, verified independently of GAP-03); supply from [CAT-042 Motor](CAT-042-motor.md) (shaft) or [CAT-005 Battery](CAT-005-battery.md) (electrical). No CAT refines it. |
| Roadmap story | Unscheduled. Campaign: introduction 37 "Driven to Deliver", practice 38, reuse 48, 68, 138 ([gap-04](../requirements.md#gap-04)). |
| Status | Not started. Disposition potential/conditional ([element map](../general-engine-element-map.md)). |

## 2. Declaration

Two variants, each specified separately: "initially use the already taught electrical motor and mechanical drive connection" ([gap-04](../requirements.md#gap-04)).

- **Bodies and shapes.** A wheel disc as EL-116 plus a drive pod fixed to the chassis side of the axle.
  - **Proposed** wheel radius 0.3 m, width 0.15 m (as EL-116, unchanged silhouette per the visual style); pod 0.25 × 0.25 × 0.2 m box on the chassis — compact enough to sit beside a 0.2 m beam.
- **Mass and material.**
  - **Proposed** wheel mass 0.5 kg (as EL-116), pod 0.4 kg carried by the chassis — the pod adds the motor's weight to the vehicle, not to the rotating wheel.
  - **Proposed** tread friction 0.8, restitution 0.1, rolling resistance 0.02 (as EL-116) — the driven/passive comparison then isolates supply.
- **Constraints.** One revolute axle row (as [EL-117](EL-117-axle.md) `Free`) plus a bounded motor row on that axle: target speed, maximum torque, per-step work budget (the legacy motor row: target, max effort, available work torque × speed × Δt, max power torque × speed, `parts/MotorPart.cs@a6c914e:L91-L97`).
- **Variant `ElectricalDrive`.** `PowerIn` socket, Electrical domain, Input (the current socket and domain, `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`). Supplied: motor row active at the declared torque and speed. Unsupplied: torque 0, the wheel free-wheels.
- **Variant `ShaftDrive`.** `DriveIn` socket, Mechanical domain, Input, coupled to a Motor's `Drive` output (`parts/MotorPart.cs@a6c914e:L49-L54`). The wheel carries no motor row of its own; the upstream shaft supplies torque and work through the mechanical coupling.
- **Typed ports.** As per variant; never both on one instance. Wrong-domain wiring refused at connection (S257 rows, [decisions](../invest/decisions.md#s257)).
- **Sensors and activation.** None; supply state is read from the electrical network or shaft.
- **Work and energy stores.** None of its own: "missing supply produces no new work ... Coasting comes only from stored motion" ([gap-04](../requirements.md#gap-04)). Work delivered ≤ supply budget per step (envelope thermodynamic row, [envelope](../../gpu-f32-physics.md#game-grade-envelope)).
- **Reference vehicle (for the numbers below).** One 2 m Structural beam (1.5 kg), one Driven wheel (0.5 kg) with its pod (0.4 kg), one passive Wheel (0.5 kg) and a Bowling ball (4 kg): 6.9 kg, weight 67.7 N, about 33.8 N on each wheel. Tread-to-Ramp pair friction √(0.8 × 0.3) = 0.49 (geometric mean, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L644-L644`), so the driven wheel's traction limit is 0.49 × 33.8 = 16.6 N. Rolling resistance adds about 0.02 × 67.7 = 1.4 N of drag against the direction of motion.
- **Parameters (ElectricalDrive).**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `speed` | f32 | 0–20 | 8 | rad/s | range from the motor (`parts/MotorPart.cs@a6c914e:L55-L63`); default **proposed** — 2.4 m/s at 0.3 m radius crosses the 16.8 m bench in about 7 s |
  | `torque` | f32 | > 0–100 | 4 | N·m | range from the motor; default **proposed** — 4 / 0.3 = 13.3 N at the rim climbs a 5° slope with the reference vehicle (needs 67.7 sin 5° + 1.4 = 7.3 N) but not 15° (needs 17.5 + 1.4 = 18.9 N), where it rolls back. Because 13.3 N is below the 16.6 N traction limit, against a Wall the motor stalls rather than spinning the tread |

  `ShaftDrive` exposes no parameters; speed and torque come from the upstream Motor.
- **Cosmetic curves and UI bindings.** Gold spoke shows spin (committed pose); a shaped supply mark on the pod supplements a state lamp ← committed supply ([gap-04 visual style](../requirements.md#gap-04)).
- **Art.** Cream disc `#fff8e9`, wood tread `#c28f52`, cyan drive pod `#66b8c9`, gold shaft collar `#f7cb52` (`DESIGN.md@a6c914e:L147-L154`, `DESIGN.md@a6c914e:L169-L172`).
- **Catalogue and inventory.** **Proposed** id `driven_wheel`, title "Driven wheel", category Power (with the Motor); `WorkshopPartKind.DrivenWheel` with enum `DriveInput { Electrical, Shaft }`, appended last in the Free palette (`engine/gpu/WorkshopInventory.cs@a6c914e:L47-L60`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/gap-01.json)): ElectricalPower, EnvironmentState, FiniteLedger, FiniteWorkActuation, JointConstraint, RigidBodyDynamics, ShaftTorque, StateTransaction.

- **Exists now.** Electrical `PowerIn`/`Supply` sockets and domain (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`); friction and rolling resistance (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L701-L765`).
- **Missing.** Battery network (Story 8.1); revolute axle and bounded motor row, Mechanical domain and Drive coupling (Stories 10.3, 11.1) ([epics](../../../_bmad-output/planning-artifacts/epics.md)); wheel cylinder collider (EL-116). Owner S638.
- **Dependencies.** EL-116, EL-117, a chassis (EL-112); Battery (CAT-005) for `ElectricalDrive`; Motor (CAT-042) for `ShaftDrive`; Coating station (EL-118) for the slip control, which coats the Driven wheel's own tread and so requires wheels to be `Coatable` ([EL-118 open question 4](EL-118-coating-station.md)).

## 4. Sources and legacy

- **Requirements.** "Supply drives a loaded vehicle; missing supply produces no new work, an overloaded drive stalls and a low-friction wheel slips. Coasting comes only from stored motion. Verify connected drivetrain and driven-wheel Reset independently of GAP-03" ([gap-04](../requirements.md#gap-04)).
- **Audit.** "GAP-03 and supported torque/load model. Powered travel, power-loss/coasting and overloaded/stalled controls" (`docs/physics-puzzle-gap-audit.md@a6c914e:L83-L83`).

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Motor supply model: target speed, max torque only while powered, work budget torque × speed × Δt, power cap torque × speed. | `parts/MotorPart.cs@a6c914e:L91-L97` | Carry forward as the bounded motor row; the per-part `PreparePhysics` loop does not carry forward. |
| 2 | Motor speed 0–20 rad/s, torque (0, 100] N·m, out-of-range rejected. | `parts/MotorPart.cs@a6c914e:L55-L63` | Carry forward ranges. |
| 3 | Mechanical sockets are clockwise-positive, negative about the hinge +Z. | `parts/MotorPart.cs@a6c914e:L54-L54`, `parts/MotorPart.cs@a6c914e:L96-L97` | Carry forward sign convention for `ShaftDrive`. |

Files consulted: `parts/MotorPart.cs`.

## 5. Acceptance outline

Point of truth: [gap-04](../requirements.md#gap-04).

- **Chrome recipe.** Build the reference vehicle (beam, one Driven wheel (Electrical) and one passive Wheel on axles, Bowling ball as load) with a Battery wired to `PowerIn`; place a 5° Ramp and a 15° Ramp ahead of it. Second build: Motor `Drive` → Driven wheel (Shaft).
- **Positive.** Supplied, the loaded vehicle drives across the bench and up the 5° ramp (13.3 N available against 7.3 N needed).
- **Negative or control.**
  - Battery disconnected: no motion from rest.
  - Supply cut while moving: the vehicle coasts down only by friction and rolling resistance.
  - Too steep: on the 15° ramp (18.9 N needed against 13.3 N) the vehicle cannot climb and rolls back at about 0.4 m/s² (17.5 − 13.3 − 1.4 = 2.8 N down the slope over 6.9 kg; rolling resistance now opposes the backward roll).
  - Overload stall: driven against a Wall, the wheel stops turning — 13.3 N is below the 16.6 N traction limit, so the motor stalls rather than the tread spinning.
  - Low friction: with the Driven wheel's tread coated Slick by an EL-118 station (requires `Coatable` wheels, EL-118 open question 4), pair friction √(0.05 × 0.3) = 0.122 gives 0.122 × 33.8 = 4.1 N of traction < 13.3 N; the wheel spins faster than the vehicle moves (slip).
- **Boundaries.** Speed 0 and 20, torque at the 100 N·m bound admitted; out of range rejected.
- **Run/Reset.** Reset restores vehicle, wiring and zero spin, independently of a passive-wheel build.
- **Save/Load.** Variant, parameters and wiring survive save and Load.

- **Integrations.** Verify both the Battery-to-PowerIn and Motor-to-DriveIn vehicles with the same passive axle, cargo and ramps; the coating/slip lane also verifies EL-118 changes the driven tread's contact material.

## 6. Open questions

1. Which input modes are adopted first (both, or Electrical only). Unspecified — owner decision.
2. Whether an unsupplied drive free-wheels (proposed) or brakes.
3. Default torque/speed (proposed 4 N·m, 8 rad/s).
4. The slip control depends on wheels being `Coatable` (EL-118 open question 4); if they are not, it needs a coatable or low-friction ramp surface instead (EL-190 open question 1).
