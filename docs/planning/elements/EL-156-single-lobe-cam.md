# EL-156 · Single-lobe cam named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-156 |
| Name | Single-lobe cam |
| Type | Mechanical |
| Anchor | [requirements.md#element-156](../requirements.md#element-156); [named entry](../invest/named-elements.md#element-156); scope indexes [todo-424](../requirements.md#todo-424) and [todo-375](../requirements.md#todo-375) |
| Proof owner | S344 |
| Refines / extends | No CAT spec. Driven through the mechanical input of [CAT-019 conveyor](CAT-019-conveyor.md) / [CAT-042 motor](CAT-042-motor.md); follower uses the slider of [CAT-062 spring](CAT-062-spring.md) |
| Related | EL-157 Double-lobe cam, EL-158 Rise-hold-fall cam (separate identities, same cam unit layout) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes
- **Housing (static).** Base box 1.0 × 0.16 × 0.8 m and a follower guide frame 0.5 × 1.2 × 0.12 m (full extents) — **proposed**: the drivetrain base footprint shared with EL-110/EL-111.
- **Cam (dynamic shaft body).** A convex prism 0.12 m thick whose polar profile is r(θ) = r₀ + h·(1 − cos θ)/2, with base radius r₀ = 0.3 m and lift h = `lift` — **proposed**: r₀ equals the sourced 0.3 m motor rotor radius (`parts/MotorPart.cs@a6c914e:L24-L26`); this one-lobe profile stays convex for any h ≤ 0.6 m.
- **Follower (dynamic box).** Flat-faced, full extents 0.3 × 0.6 × 0.12 m, riding on top of the cam — **proposed**: a flat face keeps one contact patch and reads clearly.
- Colliders: the cam needs a convex polygon prism; legacy built shaft prisms from a convex hull (`engine/SceneRotaryShaft.cs@a6c914e:L29-L42`), but the current set is Sphere, Box and Plane (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L6-L7`).

### Mass and material
- Cam 0.5 kg — **proposed**: the sourced motor rotor mass (`parts/MotorPart.cs@a6c914e:L24-L24`); inertia from the prism.
- Follower 0.3 kg — **proposed**: light enough to follow the default motor speed under its spring.
- Cam–follower material restitution 0, friction 0.1 — **proposed**: a smooth, non-bouncing follower contact so lift equals profile displacement.

### Constraints and joints
- Cam hinge about local Z, travel unbounded, connected collision disabled (sourced shaft guide, `engine/SceneRotaryShaft.cs@a6c914e:L25-L28`).
- Follower slider along local +Y, travel 0–0.3 m, connected collision disabled — **proposed**: covers the maximum lift with margin; slider pattern from `parts/SpringPart.cs@a6c914e:L35-L48`.
- Follower return spring 20 N/m with no preload, damping 1 N·s/m — **proposed**: the profile's peak downward acceleration is (h/2)·ω², and the follower is held down by g + k·h/m. At the default lift (0.15 m) the follower stays on the cam up to about 16 rad/s and visibly floats above it, so the float boundary lies inside the motor's 0–20 rad/s range (about 14.6 rad/s at the 0.25 m maximum lift).

### Typed ports
| Socket | Domain | Direction | Binding | Source |
| --- | --- | --- | --- | --- |
| `DriveIn` | Mechanical | Input | cam hinge, −1 coordinate per radian | relay pattern `parts/ConveyorPart.cs@a6c914e:L64-L71` |
| `LinkageOut` | Mechanical (linear) | Output | follower slider, +1 coordinate per metre | **proposed**, shared with EL-111 |

The follower's top face also pushes cargo physically by contact.

### Sensors and activation
None. Lift is profile geometry; no timer or event drives it.

### Work and energy stores
Spring energy ½·k·y² and follower potential energy, all supplied through `DriveIn`. No internal source.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `lift` | f32 | 0.05–0.25 | 0.15 | m | **proposed**: half a Basketball radius at the low end, enough to tip a ball over a 0.15 m lip at the default |

### Cosmetic curves and UI bindings
Cam and follower art follow committed poses; a gold mark on the lobe tip shows phase. UI: `lift` through the contextual configuration pattern (`ui/WorkshopConfiguration.cs@a6c914e:L43-L75`).

### Art
Cam in the drivetrain colour `#d69c47` with a gold `#f7cb52` lobe mark; follower cream `#fff8e9`; navy `#293954` base and guide ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.

### Catalogue and inventory entry
Id `cam_single`, title "Single-lobe cam", category Motion — **proposed**. Add `WorkshopPartKind.SingleLobeCam` with a counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
One outcome in the row. EL-157 and EL-158 are separate identities, not variants of this one ([todo-424](../requirements.md#todo-424)).

## 3. Engine capabilities

Families: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, ShaftTorque, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)).

**Exists now**
- Box contact with friction and restitution: `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L701-L719`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L886-L900`.

**Missing**
- Convex prism collider for the profile (GeometryQuery/ContactImpulse). Owner S344.
- Hinge (Story 10.3), slider with spring (Story 6.4), mechanical input (Story 11.1); linear output domain unscheduled (S257 mechanical-port, [decisions](../invest/decisions.md#s257)).

**Dependencies.** CAT-042 motor (Story 11.1) or another shaft source.

## 4. Sources and legacy

- Requirement: "One geometric lobe drives one follower lift per revolution"; outcome "Partial rotation yields only its actual profile displacement" ([element-156](../requirements.md#element-156)).
- Integration under [todo-375](../requirements.md#todo-375): "A cam profile controls a visible stroke ... Each named mechanism is separately implemented and taught." Campaign: "each cam profile", first use 31–40 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Finite-inertia shaft and hinge guide | `engine/SceneRotaryShaft.cs@a6c914e:L13-L28` | carry forward | Cam shaft declaration. |
| 2 | Convex-hull prism collider for shafts | `engine/SceneRotaryShaft.cs@a6c914e:L29-L42` | carry forward (shape intent) | A profile prism is the same shape class. |
| 3 | Slider joint with travel interval and connected collision disabled | `parts/SpringPart.cs@a6c914e:L35-L48` | carry forward | Follower guide. |

**Files harvested:**
- `engine/SceneRotaryShaft.cs`
- `parts/MotorPart.cs` (rotor radius and mass as scale references)
- `parts/SpringPart.cs` (slider pattern)
- `parts/ConveyorPart.cs` (mechanical input convention)
- Checked, no cam knowledge: `CuriousContraptions.tests/PrescribedBodyTests.cs` (a variable named `follower`), `reference/P0-022-before/docs/coverage/engine/task-017.json` (coverage text naming "wheel/cam/drop" lessons).
- Searched with no hit for `cam`, `lobe` or `follower`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-156](../requirements.md#element-156) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place battery, motor and the cam; connect `Supply` → `PowerIn` and `Drive` → `DriveIn`; put a ball on a ledge beside the follower top. Run.
- **Positive.** Each revolution produces exactly one follower lift whose height equals `lift`; the follower tips the ball off the ledge.
- **Negative/control.** A quarter turn (supply stopped early) lifts the follower only by the profile value at that angle; an unpowered cam produces no lift.
- **Boundaries.** `lift` 0.05 and 0.25; shaft speeds 1 and 20 rad/s (the follower stays in contact below about 16 rad/s at the default lift and floats above it); reverse rotation.
- **Run/Reset.** Cam angle and follower height return to authored values.
- **Save/Load.** `lift`, pose and links round-trip.
- **Integrations.** Cam, crank, clutch/brake and ratchet integration task under [todo-375](../requirements.md#todo-375); campaign row "Gears ... each cam profile ...": first use 31–40, reuse 41–50, 61–90, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); driven by CAT-042 motor, output to a linear consumer such as EL-184.

## 6. Open questions

1. Output: typed linear socket (proposed) or contact-only follower? Unspecified — owner decision.
2. Profile law: harmonic (proposed) or cycloidal rise? Owner decision.
3. Is the cam phase player-settable? Owner decision.
