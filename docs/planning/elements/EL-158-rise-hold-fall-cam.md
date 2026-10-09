# EL-158 · Rise-hold-fall cam named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-158 |
| Name | Rise-hold-fall cam |
| Type | Mechanical |
| Anchor | [requirements.md#element-158](../requirements.md#element-158); [named entry](../invest/named-elements.md#element-158); scope indexes [todo-424](../requirements.md#todo-424) and [todo-375](../requirements.md#todo-375) |
| Proof owner | S346 |
| Refines / extends | No CAT spec. Same cam-unit layout as [EL-156](EL-156-single-lobe-cam.md); drive from [CAT-042 motor](CAT-042-motor.md); follower slider as [CAT-062 spring](CAT-062-spring.md) |
| Related | EL-156 Single-lobe cam, EL-157 Double-lobe cam; contrasts with timer elements such as CAT-033 Hold timer |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes
- **Housing (static).** Base box 1.0 × 0.16 × 0.8 m and follower guide frame 0.5 × 1.2 × 0.12 m (full extents) — **proposed**, identical to EL-156.
- **Cam (dynamic shaft body).** Convex prism 0.12 m thick. Profile, in order of rotation: a 90° harmonic rise from r₀ to r₀ + h; a dwell arc at constant radius r₀ + h over `dwell_angle`; a 90° harmonic fall back to r₀; then a base arc at r₀ for the rest of the turn. r₀ = 0.3 m — **proposed**: the sourced motor rotor radius (`parts/MotorPart.cs@a6c914e:L24-L26`); 90° rise and fall sectors leave room for dwell angles up to 180°.
- **Follower (dynamic box).** Flat-faced, full extents 0.3 × 0.6 × 0.12 m — **proposed**, as EL-156.
- Current shapes lack a convex prism (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L6-L7`); legacy shafts used convex hulls (`engine/SceneRotaryShaft.cs@a6c914e:L29-L42`).

### Mass and material
- Cam 0.5 kg (sourced rotor mass, `parts/MotorPart.cs@a6c914e:L24-L24`) and follower 0.3 kg — **proposed**, as EL-156.
- Cam–follower restitution 0, friction 0.1 — **proposed**: the dwell arc holds the follower without bounce.

### Constraints and joints
- Cam hinge about local Z, unbounded, connected collision disabled (`engine/SceneRotaryShaft.cs@a6c914e:L25-L28`).
- Follower slider along local +Y, travel 0–0.3 m (`parts/SpringPart.cs@a6c914e:L35-L48`); return spring 20 N/m with no preload, damping 1 N·s/m — **proposed**, as EL-156. A 90° harmonic rise has peak acceleration (h/2)·4·ω², the same as EL-157, so at the default 0.12 m lift the follower holds contact up to about 8.6 rad/s and overshoots the dwell height above it, inside the motor's 0–20 rad/s range.

### Typed ports
| Socket | Domain | Direction | Binding | Source |
| --- | --- | --- | --- | --- |
| `DriveIn` | Mechanical | Input | cam hinge, −1 coordinate per radian | `parts/ConveyorPart.cs@a6c914e:L64-L71` |
| `LinkageOut` | Mechanical (linear) | Output | follower slider, +1 coordinate per metre | **proposed**, shared with EL-111 and EL-156 |

### Sensors and activation
None. The dwell is a geometric sector: dwell time equals `dwell_angle` divided by the actual shaft speed; there is no timer (map row: "hold-animation is not a mechanical dwell").

### Work and energy stores
Spring and follower potential energy, supplied only through `DriveIn`. During dwell the follower is held by contact at constant height with no work exchanged.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `lift` | f32 | 0.05–0.15 | 0.12 | m | **proposed**: a 90° harmonic rise is convex only while h ≤ r₀/2 = 0.15 m |
| `dwell_angle` | f32 | 30–180 | 90 | degrees | **proposed**: at the default 6 rad/s motor speed 90° holds for about 0.26 s, and at 1 rad/s for about 1.6 s, so speed visibly changes dwell time |

### Cosmetic curves and UI bindings
Cam and follower follow committed poses; the dwell arc is drawn as a gold `#f7cb52` band on the cam edge so players can see the hold sector — **proposed**. UI: `lift` and `dwell_angle` via the contextual configuration pattern (`ui/WorkshopConfiguration.cs@a6c914e:L43-L75`).

### Art
Drivetrain colour `#d69c47` cam with a gold dwell band; cream `#fff8e9` follower; navy `#293954` base ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.

### Catalogue and inventory entry
Id `cam_dwell`, title "Rise-hold-fall cam", category Motion — **proposed**. Add `WorkshopPartKind.DwellCam` with a counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
One outcome in the row; no variants.

## 3. Engine capabilities

Families: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, ShaftTorque, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)).

**Exists now**
- Box contact with friction and restitution: `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L701-L719`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L886-L900`.

**Missing**
- Convex prism collider with a constant-radius arc (GeometryQuery/ContactImpulse), owner S346; hinge (Story 10.3); slider spring (Story 6.4); mechanical input (Story 11.1); linear output domain unscheduled (S257, [decisions](../invest/decisions.md#s257)).

**Dependencies.** A shaft source such as CAT-042 (Story 11.1); shares every capability with EL-156.

## 4. Sources and legacy

- Requirement: "A declared dwell sector physically retains follower height"; outcome "Changing shaft speed changes dwell time; no hidden timer" ([element-158](../requirements.md#element-158)). Map: "Cam geometry/support retains follower during declared dwell sector; hold-animation is not a mechanical dwell."
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
- Checked, no cam knowledge: `CuriousContraptions.tests/PrescribedBodyTests.cs` (a variable named `follower`), `reference/P0-022-before/docs/coverage/engine/task-017.json` (coverage text naming "wheel/cam/drop" lessons); the `dwell` hits in `engine/` and `CuriousContraptions.tests/` are timer and residence-sensor code.
- Searched with no hit for `cam`, `lobe` or `dwell cam`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-158](../requirements.md#element-158) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place battery, motor and the dwell cam; connect `Supply` → `PowerIn` and `Drive` → `DriveIn`; let the follower hold a gate or ball stop. Run at two motor speeds.
- **Positive.** The follower rises, holds at constant height through the dwell sector, then falls.
- **Negative/control.** Doubling the shaft speed halves the hold time; stopping the shaft mid-dwell holds the follower indefinitely; an unpowered cam never rises.
- **Boundaries.** `dwell_angle` 30 and 180; `lift` 0.05 and 0.15; shaft speeds below and above about 8.6 rad/s at the default lift; reverse rotation (fall becomes rise).
- **Run/Reset.** Cam angle and follower height return to authored values.
- **Save/Load.** `lift`, `dwell_angle`, pose and links round-trip.
- **Integrations.** Cam, crank, clutch/brake and ratchet integration task under [todo-375](../requirements.md#todo-375); campaign row "Gears ... each cam profile ...": first use 31–40, reuse 41–50, 61–90, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); a timed-hold contrast with CAT-033 Hold timer.

## 6. Open questions

1. Are rise and fall sector angles player-settable or fixed at 90° (proposed)? Unspecified — owner decision.
2. Output: typed linear socket (proposed) or contact-only follower? Owner decision.
