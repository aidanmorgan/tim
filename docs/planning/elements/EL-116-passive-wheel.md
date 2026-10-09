# EL-116 · Passive wheel — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-116 · Passive wheel · Construction |
| Anchor | [requirements.md#element-116](../requirements.md#element-116); [named-elements entry](../invest/named-elements.md#element-116); umbrella [gap-03](../requirements.md#gap-03) (index only) |
| Related identities | [EL-117 Axle](EL-117-axle.md) (couples it to a chassis); [GAP-04 Driven wheel](GAP-04-driven-wheel.md) (same silhouette, supplied torque); [EL-112 Structural beam](EL-112-structural-beam.md) as chassis. No CAT refines it. |
| Roadmap story | Unscheduled. Campaign: introduction 18 "Wheel Meet Again", practice 19, reuse 37, 68, 138 ([gap-03](../requirements.md#gap-03)). |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

The requirements row names no variants; there is one mode.

- **Bodies and shapes.** One dynamic disc (short cylinder) whose axis is part-local Z.
  - **Proposed** radius 0.3 m, width 0.15 m — the Motor rotor disc is 0.3 m × 0.13 m ([CAT-042 spec](CAT-042-motor.md)), so wheels and drives share one scale.
  - The current engine admits only Sphere, Box and Plane colliders, and a dynamic body only one sphere or box (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L7-L7`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L226-L231`). A cylinder or convex-prism collider is required (see section 3).
- **Mass and material.**
  - **Proposed** mass 0.5 kg — equal to the Motor rotor; light beside a 4 kg Bowling-ball load so traction, not wheel mass, dominates.
  - Inertia: solid disc, m·r²/2 about the axle (0.0225 kg m² at default) and m(3r² + w²)/12 transverse — the generic rotary-shaft law the CAT-042 spec harvests.
  - **Proposed** tread friction 0.8 — warm-wood tread grips well enough to roll a loaded chassis down a 15° ramp without slip, yet a coated or icy surface can make it slip.
  - **Proposed** restitution 0.1, bounce threshold 0.1 m/s — a wheel should not bounce a chassis.
  - **Proposed** rolling resistance 0.02 — a hard wheel rolls further than the inflated Basketball (0.035, `engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L51`), within the admitted 0–0.1 (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
- **Constraints.** None of its own; an [EL-117 Axle](EL-117-axle.md) couples the hub to a chassis. A free wheel on the bench rolls as an independent body.
- **Typed ports.** One `Hub` socket at the disc centre on the axis, domain `Structural` (**proposed** in [EL-112](EL-112-structural-beam.md)), accepting exactly one axle.
- **Sensors and activation.** None.
- **Work and energy stores.** None. Motion comes only from gravity, contact and stored kinetic energy: "No hidden drive on level ground."
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `radius` | `Metres` (f32) | 0.15–0.6 | 0.3 | m | **proposed** — smallest still rolls over a 0.06 m bench seam; largest stays under a Wall's 2 m height ratio |

- **Cosmetic curves and UI bindings.** None; spin is the committed body rotation. One gold spoke makes spin and slip visible.
- **Art.** Cream disc `#fff8e9`, warm-wood tread `#c28f52`, navy hub recess `#293954`, one gold spoke `#f7cb52` ([gap-03 visual style](../requirements.md#gap-03); `DESIGN.md@a6c914e:L147-L154`, `DESIGN.md@a6c914e:L169-L169`).
- **Catalogue and inventory.** **Proposed** id `wheel`, title "Wheel", category Motion (with the balls, `parts/catalog/ball.tres@a6c914e:L21-L24`); `WorkshopPartKind.Wheel` appended last in the Free palette (`engine/gpu/WorkshopInventory.cs@a6c914e:L47-L60`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction.

- **Exists now.** Dynamic rigid body, Coulomb friction cone, rolling-resistance row at every contact, linear drag and gravity (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L701-L765`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`).
- **Missing.**
  - Cylinder / convex-prism collider and its mass properties: `RigidMassProperties.Compile` handles only spheres and boxes (`engine/gpu/RigidMassProperties.cs@a6c914e:L26-L38`). Story 6.6 (compound cylindrical pipe) and Story 11.1 (motor rotor) are the first consumers ([epics](../../../_bmad-output/planning-artifacts/epics.md)); a dynamic cylinder is in neither story's scope. Owner S330.
  - The rolling-resistance row today uses the larger sphere radius (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L727-L733`); a cylinder needs its own contact radius. Owner S330.
- **Dependencies.** EL-117 Axle and a chassis (EL-112) for the vehicle integration; a Ramp (CAT-054) and Wall (CAT-066) for the downhill and blocked controls.

## 4. Sources and legacy

- **Requirements.** Row: "Round body rolls according to friction and applied load"; outcome "No hidden drive on level ground" ([element-116](../requirements.md#element-116)). Integration: a loaded chassis rolls downhill, slips under insufficient traction, stops against a block; lifted wheels cannot support it ([gap-03](../requirements.md#gap-03)).
- **Audit.** "Moving assemblies, collision and mass accounting. Compare roll, slip and blocked wheel; no scripted travel path" (`docs/physics-puzzle-gap-audit.md@a6c914e:L82-L82`).
- **Legacy.** No wheel part, level or test. Only decorative wheels exist (Motor, Clutch, Conveyor art: `parts/MotorPart.cs@a6c914e:L83-L85`), with no rolling physics — no element knowledge to carry.

Files consulted: `parts/MotorPart.cs`, `parts/ClutchPart.cs`, `parts/ConveyorPart.cs` (art only).

## 5. Acceptance outline

Point of truth: [element-116](../requirements.md#element-116) and the [gap-03 integration](../requirements.md#gap-03).

- **Chrome recipe.** Place a Wheel on the bench; place a Ramp and tilt it with the rotate ring; place a second Wheel at the ramp top.
- **Positive.** The ramp wheel rolls down without slipping (spoke rotation matches travel / radius) and decelerates on the bench under rolling resistance.
- **Negative or control.** The wheel resting on the level bench stays at rest for the whole Run. A wheel on the ramp with its axle locked (EL-117) slides instead of rolling.
- **Boundaries.** Radius 0.15 and 0.6 m admitted, outside rejected; a wheel rolling into a Wall stops without penetration.
- **Run/Reset.** Reset restores pose and zero spin.
- **Save/Load.** Radius and pose survive save and Load.
- **Integrations.** Wheel/axle integration task [sequence-task-536](../requirements.md#sequence-task-536) (gap-03: loaded chassis rolls downhill, slips under insufficient traction, stops against a block, lifted wheels cannot support it); driven-wheel row [sequence-task-537](../requirements.md#sequence-task-537) (GAP-04, verified separately); interaction rows IX-01 contact impulse ([interaction-01](../requirements.md#interaction-01)), IX-02 sliding friction ([interaction-02](../requirements.md#interaction-02)) and IX-03 joint constraint ([interaction-03](../requirements.md#interaction-03)). Campaign first use: GAP-03 row of the [campaign allocation](../requirements.md#campaign-gap-allocation) — introduction 18, practice 19, reuse 37, 68, 138.

## 6. Open questions

1. Collider representation: true cylinder, convex prism (as the motor rotor), or a sphere approximation with locked tilt. Unspecified — owner decision.
2. Whether wheel width and tread material are configurable.
3. Whether Wheel sits in category Motion or Structure.
