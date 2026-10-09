# EL-085 · Rope-driven character wheel — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-085 · Rope-driven character wheel · Character |
| Requirement anchor | [element-085](../requirements.md#element-085); scope index [todo-200](../requirements.md#todo-200) |
| Named entry | [element-085](../invest/named-elements.md#element-085); source owner **S655** |
| CAT spec refined or extended | Extends the rope family: [CAT-058 Rope anchor](CAT-058-rope_anchor.md) and [CAT-053 Pulley](CAT-053-pulley.md) (rope law), [CAT-067 Weight](CAT-067-weight.md) (driving load) |
| Related identities | EL-205 Rope (connector), EL-204 Weight, GAP-04 Driven wheel, CAT-019 Conveyor (a shaft load) |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

The element-map composition rule applies: routed tension and ShaftTorque bindings, where actual rope work drives the wheel and the character's motion, not animation ([element map](../general-engine-element-map.md)). The character on the wheel is a cosmetic rider whose stride follows the committed wheel angle; it has no controller of its own.

- **Bodies and shapes.**
  - Static frame: two upright side boxes 0.1 × 1.6 × 0.1 m on a base box 1.6 × 0.1 × 0.6 m (**proposed**: a frame just taller than the wheel).
  - Wheel: one dynamic body on a revolute joint about local Z at (0, 0.8, 0): a rim ring r 0.65 m approximated by 12 thin boxes, plus a central rope drum r 0.15 m (**proposed**: about bench-module size; the drum-to-rim ratio is readable at a glance).
- **Mass and material.** Wheel 1.0 kg with inertia from its actual rim geometry; friction 0.5, restitution 0.1 (**proposed**: a light toy wheel that a 4 kg Weight turns briskly).
- **Constraints.** Wheel revolute joint (frame to wheel). A rope attachment on the drum: tension acting at the drum radius produces torque, and wound length changes with angle (**proposed**: one wrap direction, unwinding as the load descends).
- **Typed ports.**

  | Socket | Domain | Direction | Local position (m) | Status |
  | --- | --- | --- | --- | --- |
  | `RopeTie` | Rope | Input (Load role) | drum rim (0.15, 0.8, 0) | **proposed**: the rope domain attachment roles from the rope law |
  | `DriveOut` | Mechanical | Output | (0, 0.8, 0.3) axle face | **proposed**: delivers wheel shaft torque to a load such as a Conveyor |

- **Sensors and activation.** None. "Under load" means the `DriveOut` shaft load is a real resisting torque.
- **Work and energy stores.** None. Work enters only as rope tension × drum travel and leaves through `DriveOut` and friction. A slack rope gives zero tension and therefore zero work.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `drum_radius` | f32 | 0.1–0.3 | 0.15 | m | **proposed**: sets the torque-to-speed trade |
  | `rotation_direction` | enum `RotationDirection` | Clockwise, Counterclockwise | Clockwise | — | **proposed**: the wrap sense |

- **Cosmetic curves and UI bindings.** The rider's run cycle phase equals the committed wheel angle × (rim radius ÷ stride length), so the rider runs only when the wheel turns and freezes when it stops. Rope artwork follows the shared taut and slack presentation ([CAT-058](CAT-058-rope_anchor.md); `DESIGN.md@a6c914e:L284-L284`).
- **Art (DESIGN.md).** Cream rim `#fff8e9` with gold spokes `#f7cb52`, ochre drum `#d69c47`, navy frame `#293954`, and an original simple rider (rounded cream body, navy eyes). Pulley-family colouring (`DESIGN.md@a6c914e:L187-L187`).
- **Catalogue and inventory entry.** Id `character_wheel`, title "Runner wheel", category Ropes, colour `#d69c47` (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, ProgrammableController, RigidBodyDynamics, SignalPropagation, StateTransaction, TimedCommand, TypedContracts), plus the composition-required TensionTransmission and ShaftTorque.

- **Exists now.** Static box frames and dynamic box contact (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`). Cosmetic curve carriers (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L58`).
- **Missing.**
  - Revolute joint: Epic 10 (Stories 10.3/10.2).
  - Tension-only rope route and rope domain sockets (TensionTransmission): Story 10.2; S257 rope-port, next S688 ([decisions](../invest/decisions.md#s257)).
  - Mechanical shaft output (ShaftTorque): Story 11.1; S257 mechanical-port, next S690.
  - Dynamic compound bodies for the rim: owner S655.
- **Dependencies.** CAT-058 Rope anchor or CAT-053 Pulley, CAT-067 Weight; CAT-019 Conveyor as the shaft load.

## 4. Sources and legacy

- **Requirement row.** [element-085](../requirements.md#element-085): rope work drives a visible carrier mechanism under load; slack rope supplies no work.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Decisions.** [S257 rope-port](../invest/decisions.md#s257): one loaded routed rope lifts its load, a slack rope lifts nothing, and an invalid endpoint is refused.
- **Legacy search.** `git grep -i` at a6c914e for "wheel", "hamster", "monkey", "treadmill" and "character" over every Epic 7 deletion path found no character-wheel source. The rope facts below apply; [CAT-058](CAT-058-rope_anchor.md) holds the full rope harvest.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Rope state is Open if incomplete, Slack if current length is below maximum minus 0.001 m, otherwise Taut | `engine/RopeNetwork.cs@a6c914e:L37-L38` | Carry forward the states. Do not carry forward the 0.001 m CPU tolerance (re-freeze under f32). |
| L2 | A rope is tension-only: no equality row; inactive while slack; when active it only resists lengthening | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L114-L122` | Carry forward the law ("slack supplies no work"). Do not carry forward the CPU impulse code. |
| L3 | Weight is a rope-linked load; larger weights pull harder; default mass 4 | `parts/catalog/weight.tres@a6c914e:L11-L14` | Carry forward as the driving load |

**Files harvested:** `engine/RopeNetwork.cs`, `engine/physics/PhysicsRopeJoint.cs`, `parts/catalog/weight.tres`.

## 5. Acceptance outline

- **Chrome recipe.** Place the Runner wheel; place a Weight below a Pulley; connect a rope from the drum `RopeTie` over the Pulley to the Weight with the contextual rope control; connect `DriveOut` to a Conveyor carrying a Basketball. Run.
- **Positive.** The Weight descends, the rope stays taut, the wheel turns, the rider runs, and the Conveyor moves the ball.
- **Negative or control.** A rope lengthened so it starts slack: the wheel does not turn until it becomes taut. With the Weight removed, nothing moves. A stalled Conveyor (blocked) stalls the wheel and the rider stops.
- **Boundaries.** Wheel angle × drum radius equals rope travel within the envelope; the energy delivered never exceeds the Weight's potential energy drop.
- **Run/Reset.** Reset restores the wheel angle, rope length and Weight exactly. **Save/Load.** The rope route and shaft connection round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-200](../requirements.md#todo-200) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-03 joint constraint](../requirements.md#interaction-03), [IX-04 tension transmission](../requirements.md#interaction-04) and [IX-05 shaft torque transmission](../requirements.md#interaction-05). Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row beginning "Original cat/mouse lure/escape roles" ("rope-to-drive character motor") reserves levels 91–100 (reuse 136–150); chapter 10 "Oddly Satisfying" ("rope-driven character motor") in the [campaign plan](../requirements.md#campaign-plan). Element integrations: Rope anchor (CAT-058) or Pulley (CAT-053), Weight (CAT-067), Conveyor shaft load (CAT-019).

## 6. Open questions

1. Which load the wheel drives in its teaching example (Conveyor is proposed). Unspecified — owner decision (S655).
2. Whether the rider has any physical presence (proposed: cosmetic only).
3. Whether rewinding (the load raised by driving the shaft) is supported.
