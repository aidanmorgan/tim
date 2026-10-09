# EL-157 · Double-lobe cam named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-157 |
| Name | Double-lobe cam |
| Type | Mechanical |
| Anchor | [requirements.md#element-157](../requirements.md#element-157); [named entry](../invest/named-elements.md#element-157); scope indexes [todo-424](../requirements.md#todo-424) and [todo-375](../requirements.md#todo-375) |
| Proof owner | S345 |
| Refines / extends | No CAT spec. Same cam-unit layout as [EL-156](EL-156-single-lobe-cam.md); drive from [CAT-042 motor](CAT-042-motor.md); follower slider as [CAT-062 spring](CAT-062-spring.md) |
| Related | EL-156 Single-lobe cam, EL-158 Rise-hold-fall cam |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes
- **Housing (static).** Base box 1.0 × 0.16 × 0.8 m and follower guide frame 0.5 × 1.2 × 0.12 m (full extents) — **proposed**: identical to EL-156 so the three cams are interchangeable on a route.
- **Cam (dynamic shaft body).** Convex prism 0.12 m thick with polar profile r(θ) = r₀ + h·(1 − cos 2θ)/2, r₀ = 0.3 m — **proposed**: two equal lobes 180° apart; r₀ matches the sourced 0.3 m motor rotor radius (`parts/MotorPart.cs@a6c914e:L24-L26`).
- **Follower (dynamic box).** Flat-faced, full extents 0.3 × 0.6 × 0.12 m — **proposed**, as EL-156.
- Current shapes lack a convex prism (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L6-L7`); legacy shafts used convex hulls (`engine/SceneRotaryShaft.cs@a6c914e:L29-L42`).

### Mass and material
- Cam 0.5 kg (the sourced rotor mass, `parts/MotorPart.cs@a6c914e:L24-L24`) and follower 0.3 kg — **proposed**, as EL-156.
- Cam–follower material restitution 0, friction 0.1 — **proposed**: lift equals profile displacement without bounce.

### Constraints and joints
- Cam hinge about local Z, unbounded, connected collision disabled (`engine/SceneRotaryShaft.cs@a6c914e:L25-L28`).
- Follower slider along local +Y, travel 0–0.3 m (slider pattern `parts/SpringPart.cs@a6c914e:L35-L48`); return spring 20 N/m with no preload, damping 1 N·s/m — **proposed**, as EL-156. Two lobes make the profile acceleration (h/2)·4·ω², four times a single lobe of the same lift at equal shaft speed. Held down by g + k·h/m, the follower stays on the cam up to about 8.6 rad/s at the default 0.12 m lift and visibly floats above it, inside the motor's 0–20 rad/s range.

### Typed ports
| Socket | Domain | Direction | Binding | Source |
| --- | --- | --- | --- | --- |
| `DriveIn` | Mechanical | Input | cam hinge, −1 coordinate per radian | `parts/ConveyorPart.cs@a6c914e:L64-L71` |
| `LinkageOut` | Mechanical (linear) | Output | follower slider, +1 coordinate per metre | **proposed**, shared with EL-111 and EL-156 |

### Sensors and activation
None. Two separately observable lifts come from geometry alone.

### Work and energy stores
Spring and follower potential energy, supplied only through `DriveIn`.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `lift` | f32 | 0.05–0.15 | 0.12 | m | **proposed**: the two-lobe profile is convex only while h ≤ r₀/2 = 0.15 m, so the range stops there |

### Cosmetic curves and UI bindings
Cam and follower follow committed poses; two gold lobe marks, one per lobe, so each lift is separately visible — **proposed**. UI: `lift` via the contextual configuration pattern (`ui/WorkshopConfiguration.cs@a6c914e:L43-L75`).

### Art
Drivetrain colour `#d69c47` cam with two gold `#f7cb52` lobe marks; cream `#fff8e9` follower; navy `#293954` base ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.

### Catalogue and inventory entry
Id `cam_double`, title "Double-lobe cam", category Motion — **proposed**. Add `WorkshopPartKind.DoubleLobeCam` with a counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
One outcome in the row; no variants.

## 3. Engine capabilities

Families: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, ShaftTorque, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)).

**Exists now**
- Box contact with friction and restitution: `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L701-L719`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L886-L900`.

**Missing**
- Convex prism collider (GeometryQuery/ContactImpulse), owner S345; hinge (Story 10.3); slider spring (Story 6.4); mechanical input (Story 11.1); linear output domain unscheduled (S257, [decisions](../invest/decisions.md#s257)).

**Dependencies.** A shaft source such as CAT-042 (Story 11.1); shares every capability with EL-156, so it should follow it.

## 4. Sources and legacy

- Requirement: "Two geometric lobes drive two separately observable lifts per revolution"; outcome "One revolution cannot be recorded as a single-lobe cycle" ([element-157](../requirements.md#element-157)).
- Integration under [todo-375](../requirements.md#todo-375); campaign "each cam profile", first use 31–40 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Finite-inertia shaft, hinge guide and convex-hull prism | `engine/SceneRotaryShaft.cs@a6c914e:L13-L42` | carry forward | Cam shaft declaration and shape class. |
| 2 | Slider joint with travel interval | `parts/SpringPart.cs@a6c914e:L35-L48` | carry forward | Follower guide. |

**Files harvested:**
- `engine/SceneRotaryShaft.cs`
- `parts/MotorPart.cs` (rotor radius and mass as scale references)
- `parts/SpringPart.cs` (slider pattern)
- `parts/ConveyorPart.cs` (mechanical input convention)
- Checked, no cam knowledge: `CuriousContraptions.tests/PrescribedBodyTests.cs` (a variable named `follower`), `reference/P0-022-before/docs/coverage/engine/task-017.json` (coverage text naming "wheel/cam/drop" lessons).
- Searched with no hit for `cam` or `lobe`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-157](../requirements.md#element-157) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place battery, motor and the double-lobe cam; connect `Supply` → `PowerIn` and `Drive` → `DriveIn`; feed balls onto the follower ledge. Run for one revolution.
- **Positive.** One revolution gives two follower lifts, each releasing one ball; the lift count per revolution is 2.
- **Negative/control.** Replacing it with EL-156 under the same drive gives one lift per revolution; an unpowered cam gives none; half a revolution gives exactly one lift.
- **Boundaries.** `lift` 0.05 and 0.15; shaft speeds below and above about 8.6 rad/s at the default lift (follower keeps or loses contact); reverse rotation.
- **Run/Reset.** Cam angle and follower height return to authored values.
- **Save/Load.** `lift`, pose and links round-trip.
- **Integrations.** Cam, crank, clutch/brake and ratchet integration task under [todo-375](../requirements.md#todo-375); campaign row "Gears ... each cam profile ...": first use 31–40, reuse 41–50, 61–90, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); paired with EL-156 as its single-lobe contrast.

## 6. Open questions

1. Must the two lobes be equal (proposed) or independently sized? Unspecified — owner decision.
2. Output: typed linear socket (proposed) or contact-only follower? Owner decision.
