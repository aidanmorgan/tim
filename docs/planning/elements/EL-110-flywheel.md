# EL-110 · Flywheel named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-110 |
| Name | Flywheel |
| Type | Mechanical |
| Anchor | [requirements.md#element-110](../requirements.md#element-110); [named entry](../invest/named-elements.md#element-110); scope index [todo-377](../requirements.md#todo-377) |
| Proof owner | S337 |
| Refines / extends | No CAT spec. Uses the finite-inertia shaft and mechanical ports of [CAT-042 motor](CAT-042-motor.md), [CAT-019 conveyor](CAT-019-conveyor.md) and [CAT-057 reverse transmission](CAT-057-reverse_transmission.md) |
| Related | EL-111 Centrifugal governor (same scope index), EL-199 Electric motor, EL-200 Drive belt |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes
- **Housing (static).** Base box 1.0 × 0.16 × 0.8 m and a cream bearing block 0.3 × 0.7 × 0.3 m (full extents) — **proposed**: a smaller version of the sourced motor base 1.25 × 0.16 × 1 m (`parts/MotorPart.cs@a6c914e:L64-L71`, see [CAT-042](CAT-042-motor.md)).
- **Wheel (dynamic shaft body).** Disc radius 0.5 m, width 0.15 m, on a local-Z axle 0.6 m above the base — **proposed**: larger than the 0.3 m motor rotor so the stored energy reads as "heavy", and below the 0.65 m pipe bore so it fits beside tube routes.
- Collider: the legacy shaft used a 24-sided convex prism (`engine/SceneRotaryShaft.cs@a6c914e:L29-L42`); the current shape set has no cylinder (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L6-L7`).

### Mass and material
- Wheel mass from `mass` (default 4 kg). Inertia: axial m·r²/2, transverse m·(3r² + w²)/12 — sourced generic shaft law (`engine/SceneRotaryShaft.cs@a6c914e:L13-L24`). At 4 kg the axial inertia is 0.5 kg·m², storing 9 J at the default motor speed of 6 rad/s and 100 J at the 20 rad/s motor limit.
- Bearing loss: none. An unloaded rotor coasts at constant speed and kinetic energy after supply loss (sourced, `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L113-L123`); losses come only from connected loads.
- Rim contact material restitution 0.1, friction 0.3 — **proposed**: a dead metal rim so stray balls do not gain energy.

### Constraints and joints
One free revolute hinge about local Z between wheel and housing, travel unbounded, connected collision disabled — sourced shaft guide (`engine/SceneRotaryShaft.cs@a6c914e:L25-L28`).

### Typed ports
| Socket | Domain | Direction | Binding | Source |
| --- | --- | --- | --- | --- |
| `DriveIn` | Mechanical | Input | wheel hinge, −1 coordinate per radian | relay pattern `parts/ConveyorPart.cs@a6c914e:L64-L71` |
| `Drive` | Mechanical | Output | wheel hinge, −1 coordinate per radian | same |

Mechanical sockets are clockwise-positive viewed from +Z (`parts/ConveyorPart.cs@a6c914e:L21-L22`). One authored belt per input; an output may fan out (`engine/MechanicalNetwork.cs@a6c914e:L28-L38`). Socket positions on the axle ends — **proposed**.

### Sensors and activation
None.

### Work and energy stores
The wheel's kinetic energy ½·I·ω² is the store. It charges only through `DriveIn` and discharges only as work delivered through `Drive` or contact; it never gains energy without input (no-free-energy rule, [envelope](../../gpu-f32-physics.md#game-grade-envelope)).

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `mass` | f32 | 1–8 | 4 | kg | **proposed**: shares the sourced Weight mass ceiling of 8 kg (`parts/WeightPart.cs@a6c914e:L22-L27`); since I ∝ m, an 8 kg flywheel stores eight times the energy of a 1 kg one at equal speed and so bridges a much longer supply gap |

### Cosmetic curves and UI bindings
- Wheel art follows the committed shaft pose; a navy index bar and gold index sphere show rotation, as on the reverser wheels (`parts/ReverseTransmissionPart.cs@a6c914e:L36-L52`). Readings are clockwise-positive (`parts/ReverseTransmissionPart.cs@a6c914e:L21-L24`).
- UI: `mass` through the contextual configuration pattern (`ui/WorkshopConfiguration.cs@a6c914e:L43-L75`).

### Art
Wheel in the Reverse transmission colour `#d69c47`; navy `#293954` base and index bar; cream `#fff8e9` bearing block; gold `#f7cb52` index ([DESIGN colour system](../../../DESIGN.md#colour-system); [mechanical work feedback](../../../DESIGN.md#mechanical-work-feedback)). **Proposed**: the drivetrain colour family.

### Catalogue and inventory entry
Id `flywheel`, title "Flywheel", category Motion — **proposed**. Add `WorkshopPartKind.Flywheel` with a counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
One outcome, no named variants ([element-110](../requirements.md#element-110)).

## 3. Engine capabilities

Families: EnvironmentState, FiniteLedger, JointConstraint, RigidBodyDynamics, ShaftTorque, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)).

**Exists now**
- Dynamic bodies with declared inertia and the 128 rad/s spin clamp: `engine/gpu/RigidMassProperties.cs@a6c914e:L26-L51`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L208-L215`.

**Missing**
- Revolute hinge (JointConstraint): Story 10.3. Cylinder collider and inertia: Story 11.1 or Story 6.6.
- Mechanical domain, `DriveIn`/`Drive` sockets and shaft ratio coupling (ShaftTorque): Story 11.1; decision S257 mechanical-port, next implementation S690 ([decisions](../invest/decisions.md#s257)).
- A committed shaft-pose cosmetic source: Story 11.1.

**Dependencies.** CAT-042 motor (Story 11.1) to charge it; CAT-019 conveyor or CAT-057 reverser as loads; CAT-005 battery to show the supply gap.

## 4. Sources and legacy

- Requirement: "Rotating inertia stores work and coasts under load"; outcome "Stored energy falls as output work is delivered" ([element-110](../requirements.md#element-110)).
- Integration task under [todo-377](../requirements.md#todo-377): "A flywheel bridges a measured supply gap using stored rotational energy ... each has a visible depletion/cooling control." Campaign: ratchet/escapement/flywheel row, first use 81–90, reuse 91–100, 131–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Shaft inertia axial m·r²/2, transverse m·(3r² + w²)/12; mass and dimensions finite and positive | `engine/SceneRotaryShaft.cs@a6c914e:L13-L24` | carry forward | Generic finite-inertia law. |
| 2 | Shaft hinge about local Z, travel both ways, connected collision disabled | `engine/SceneRotaryShaft.cs@a6c914e:L25-L28` | carry forward | Hinge declaration. |
| 3 | 24-sided convex prism shaft collider | `engine/SceneRotaryShaft.cs@a6c914e:L29-L42` | carry forward (shape intent), open | Cylinder primitive is an owner decision. |
| 4 | Accepted: after supply loss an unloaded shaft keeps its speed, total kinetic energy changes ≤ 1e-8, supplied work stays constant | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L113-L123` | carry forward (behaviour), do not carry forward (1e-8 literal) | Coasting without hidden damping; tolerance is game-grade. |
| 5 | Link ratio = input coordinate per radian ÷ output coordinate per radian, always engaged | `engine/MechanicalNetwork.cs@a6c914e:L46-L49` | carry forward | Shaft coupling rule. |

No legacy flywheel exists.

**Files harvested:**
- `engine/SceneRotaryShaft.cs`
- `engine/MechanicalNetwork.cs`
- `CuriousContraptions.tests/MechanicalTests.cs`
- `parts/MotorPart.cs` (housing scale)
- `parts/ConveyorPart.cs` (mechanical socket convention)
- `parts/ReverseTransmissionPart.cs` (wheel art and sign convention)
- `parts/WeightPart.cs` (mass range reference)
- Searched with no hit for `flywheel`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-110](../requirements.md#element-110) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place battery, switch-gated motor, flywheel and conveyor; connect `Supply` → `PowerIn`, motor `Drive` → flywheel `DriveIn`, flywheel `Drive` → conveyor `DriveIn`. Run, then open the supply.
- **Positive.** The flywheel keeps the conveyor carrying a ball for a while after supply loss; its speed falls as the ball is carried.
- **Negative/control.** Without the flywheel the conveyor stops sooner; an unloaded flywheel coasts at constant speed; an unconnected flywheel never moves.
- **Boundaries.** `mass` 1 and 8; stall against a blocked load; reverse rotation via a reverser.
- **Run/Reset.** Wheel angle and speed return to zero.
- **Save/Load.** `mass`, pose and mechanical links round-trip.
- **Integrations.** Flywheel supply-gap integration task under [todo-377](../requirements.md#todo-377) (with EL-111 and TH-22/TH-23); campaign row "Ratchet, escapement, indexed carousel, flywheel ...": first use 81–90, reuse 91–100, 131–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); charged by CAT-042 motor, loaded by CAT-019 conveyor.

## 6. Open questions

1. Should the flywheel declare bearing friction, or coast losslessly as the legacy shafts did? Unspecified — owner decision.
2. Is `mass` the player control, or should radius also vary? Owner decision.
3. Does the wheel rim collide with cargo, or is it guarded by the housing? Owner decision.
