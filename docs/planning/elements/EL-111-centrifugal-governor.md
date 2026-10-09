# EL-111 · Centrifugal governor named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-111 |
| Name | Centrifugal governor |
| Type | Mechanical |
| Anchor | [requirements.md#element-111](../requirements.md#element-111); [named entry](../invest/named-elements.md#element-111); scope index [todo-377](../requirements.md#todo-377) |
| Proof owner | S338 |
| Refines / extends | No CAT spec. Driven like [CAT-019 conveyor](CAT-019-conveyor.md) through a mechanical input; uses the slider spring of [CAT-062 spring](CAT-062-spring.md) |
| Related | EL-110 Flywheel (same scope index), EL-199 Electric motor, EL-184 Mechanical ball gate (a linear consumer) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes
All full extents.
- **Housing (static).** Base box 1.0 × 0.16 × 0.8 m with a yoke post 0.12 × 1.2 × 0.12 m — **proposed**: the flywheel housing footprint, so drivetrain parts share a base size.
- **Spindle (dynamic shaft).** Vertical, radius 0.06 m, height 1.0 m, mass 0.3 kg — **proposed**: slim enough that the flyballs dominate the response.
- **Two flyballs (dynamic spheres).** Radius 0.12 m, mass 0.5 kg each — **proposed**: visible at bench scale and heavy enough to lift the sleeve against its spring.
- **Sleeve (dynamic box).** 0.2 × 0.1 × 0.2 m, mass 0.2 kg, sliding on the spindle — **proposed**.
- Every dynamic body is one homogeneous sphere or box, which the current compiler admits (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L226-L229`).

### Mass and material
Inertia compiled from the primitives (`engine/gpu/RigidMassProperties.cs@a6c914e:L26-L51`). Contact material restitution 0.1, friction 0.3 — **proposed**: a dead metal surface; the mechanism works by joints, not contacts.

### Constraints and joints
- Spindle hinge about local +Y to the housing, travel unbounded, connected collision disabled (shaft guide pattern, `engine/SceneRotaryShaft.cs@a6c914e:L25-L28`).
- Each flyball hangs from the spindle top on a hinge whose anchor is 0.5 m from the ball centre (a rigid arm), swinging 0–80° outward — **proposed**: with arm length L = 0.5 m the balls start to lift above √(g/L) ≈ 4.4 rad/s, inside the 0–20 rad/s motor range, and stand at about 57° at the default 6 rad/s motor speed.
- Two distance links 0.35 m from each flyball to the sleeve — **proposed** linkage geometry.
- Sleeve slider along the spindle axis, travel 0–0.2 m, with a return spring (see Parameters) and damping 2 N·s/m — **proposed**; slider spring and damping loads follow the sourced pattern (`engine/SlidingBlade.cs@a6c914e:L36-L42`).

### Typed ports
| Socket | Domain | Direction | Binding | Source |
| --- | --- | --- | --- | --- |
| `DriveIn` | Mechanical | Input | spindle hinge, −1 coordinate per radian | relay pattern `parts/ConveyorPart.cs@a6c914e:L64-L71` |
| `LinkageOut` | Mechanical (linear) | Output | sleeve slider, +1 coordinate per metre | **proposed**: the requirement's "displace a linkage" needs a consumable displacement |

### Sensors and activation
None. The sleeve displacement is physical; no speed threshold or cosmetic pose replaces it (map row).

### Work and energy stores
The spring stores ½·k·x² at sleeve lift x; all energy comes from the driven spindle. A stationary spindle leaves the sleeve at rest on its lower stop.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `spring_stiffness` | f32 | 10–100 | 40 | N/m | **proposed**: sets the speed at which the sleeve reaches its 0.2 m stop; the range keeps that speed inside the motor's 0–20 rad/s |

### Cosmetic curves and UI bindings
- Flyballs, arms and sleeve follow committed body poses; arm rods drawn between hinge anchor and ball. No cosmetic pose substitutes for physics (map row).
- UI: `spring_stiffness` through the contextual configuration pattern (`ui/WorkshopConfiguration.cs@a6c914e:L43-L75`).

### Art
Brass-like flyballs in gold `#f7cb52`; spindle and arms pale grey `#ccd8dc` metal detail; housing navy `#293954` with cream `#fff8e9` yoke; sleeve in the drivetrain colour `#d69c47` ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.

### Catalogue and inventory entry
Id `governor`, title "Centrifugal governor", category Motion — **proposed**. Add `WorkshopPartKind.Governor` with a counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
One outcome, no named variants ([element-111](../requirements.md#element-111)).

## 3. Engine capabilities

Families: EnvironmentState, FiniteLedger, JointConstraint, RigidBodyDynamics, ShaftTorque, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)).

**Exists now**
- Dynamic spheres and boxes with gravity: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`.

**Missing**
- Hinge, slider and distance joints in one closed loop (JointConstraint): Story 6.4 (slider), Story 10.3 (hinge), Story 10.2 (distance/rope rows). Owner S338.
- Mechanical input coupling (ShaftTorque): Story 11.1. A linear mechanical output domain: none scheduled; decision S257 mechanical-port ([decisions](../invest/decisions.md#s257)).

**Dependencies.** CAT-042 motor (Story 11.1) as the drive; a linear consumer such as EL-184.

## 4. Sources and legacy

- Requirement: "Rotating masses displace a linkage according to actual speed"; outcome "Stationary shaft does not actuate it" ([element-111](../requirements.md#element-111)). Map: "Actual rotating mass/linkage response supplies governor displacement; no speed-threshold cosmetic pose replacement."
- Integration under [todo-377](../requirements.md#todo-377): "a governor responds to actual speed". Campaign: first use 81–90 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Joint kinds BallSocket, Hinge, Slider | `engine/physics/JointEquations.cs@a6c914e:L5-L5` | carry forward (closed set) | Enum of joint kinds the governor composes. |
| 2 | Shaft hinge about one axis, both directions, connected collision disabled | `engine/SceneRotaryShaft.cs@a6c914e:L25-L28` | carry forward | Spindle guide. |
| 3 | Slider spring and damping loads added each step to a slider guide | `engine/SlidingBlade.cs@a6c914e:L36-L42` | carry forward (law), do not carry forward (per-step `Prepare` loop) | Per-element update loops are banned; the spring becomes declared constraint data. |

No legacy governor exists.

**Files harvested:**
- `engine/physics/JointEquations.cs`
- `engine/SceneRotaryShaft.cs`
- `engine/SlidingBlade.cs`
- `parts/ConveyorPart.cs` (mechanical socket convention)
- Searched with no hit for `governor` or `flyball`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-111](../requirements.md#element-111) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place battery, motor and governor; connect `Supply` → `PowerIn` and motor `Drive` → governor `DriveIn`; connect `LinkageOut` to a linear consumer. Run.
- **Positive.** As the spindle reaches speed the flyballs swing out and the sleeve rises, operating the consumer.
- **Negative/control.** A stationary or disconnected spindle leaves the sleeve down; a slow speed below the lift onset does not actuate.
- **Boundaries.** `spring_stiffness` 10 and 100; motor speed 0 and 20 rad/s; supply loss lowers the sleeve as the spindle slows.
- **Run/Reset.** Spindle, flyballs and sleeve return to rest.
- **Save/Load.** `spring_stiffness`, pose and links round-trip.
- **Integrations.** Governor-responds-to-actual-speed integration task under [todo-377](../requirements.md#todo-377) (with EL-110 and TH-22/TH-23); campaign row "Ratchet, escapement, indexed carousel, flywheel, governor ...": first use 81–90, reuse 91–100, 131–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); driven by CAT-042 motor, output to a linear consumer such as EL-184.

## 6. Open questions

1. Output coupling: a typed linear mechanical socket (proposed) or a physical push rod acting by contact? Unspecified — owner decision.
2. Must the governor regulate the motor (feedback), or only report speed by displacement? Owner decision.
3. Arm length and flyball mass (proposed): confirm the lift-onset speed. Owner decision.
