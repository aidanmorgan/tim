# EL-109 · Speed-sensitive trapdoor named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-109 |
| Name | Speed-sensitive trapdoor |
| Type | Mechanical |
| Anchor | [requirements.md#element-109](../requirements.md#element-109); [named entry](../invest/named-elements.md#element-109); scope index [todo-248](../requirements.md#todo-248); historical trap door [todo-220](../requirements.md#todo-220) |
| Proof owner | S376 |
| Refines / extends | No CAT spec. Reuses the aperture sensor of [CAT-002 ball detector](CAT-002-ball_detector.md) and the hinge of [CAT-034 impact lever](CAT-034-impact_lever.md) |
| Related | EL-107, EL-108 (same scope index); EL-162–EL-164 diverters (alternative routing) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes
- **Track (static).** A floor box 3.0 × 0.18 × 1.3 m with two side walls 3.0 × 0.4 × 0.06 m, open over a 1.2 m door opening — **proposed**: the sourced ramp default 3 × 1.3 m with 0.18 m thickness (`engine/gpu/WorkshopInstances.cs@a6c914e:L22-L30`), so it chains with ramps.
- **Door (dynamic).** One box 1.2 × 0.08 × 1.2 m, hinged at its upstream edge — **proposed**: fills the opening with 0.05 m side clearance.
- **Latch pin (dynamic).** One box 0.12 × 0.12 × 0.3 m on a slider under the door's free edge — **proposed**: a visible pin the player can see retract.
- **Speed gate.** A planar aperture 1.2 × 0.8 m across the track, 0.8 m upstream of the door — **proposed**: a Basketball at 3 m/s covers 0.8 m in about 0.27 s, enough for the released door to start dropping.
- Current dynamic bodies admit one homogeneous sphere or box (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L226-L229`); door and pin each fit.

### Mass and material
- Door 0.5 kg, homogeneous box inertia compiled by `RigidMassProperties.Compile` (`engine/gpu/RigidMassProperties.cs@a6c914e:L26-L51`) — **proposed**: light enough that a 1 kg Basketball tips it open.
- Pin 0.1 kg — **proposed**: small actuator load.
- Track and door material restitution 0.1, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 — **proposed**: the ramp-like dead surface, close to the bend material (`parts/PipeBendPart.cs@a6c914e:L12-L12`).

### Constraints and joints
- Door hinge about the upstream edge, travel 0 to 80° downward, connected collision disabled — **proposed** limits; hinge-with-limits pattern from `parts/ImpactLeverPart.cs@a6c914e:L17-L27`.
- Door closed stop: the pin's top face supports the door's free edge when extended.
- Pin slider along local −Y, travel 0–0.15 m — **proposed**: clears the door edge thickness 0.08 m with margin.

### Typed ports
| Socket | Domain | Direction | Source |
| --- | --- | --- | --- |
| `PowerIn` | Electrical | Input | existing identity `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`; supplies the latch — requirement "supplied latch" |

The sensor-to-latch binding is internal typed discrete state, not a player wire (map row).

### Sensors and activation
- Aperture crossing sensor at the speed gate, admitting only forward crossings whose speed is at or above `speed_threshold`. Reverse, stationary and outside-aperture motion never publish (legacy acceptance, row 3 below).
- A qualifying crossing latches a typed `TrapdoorRelease` state; the latch actuator then retracts the pin if supplied.

### Work and energy stores
- Latch actuator: a finite-work slider drive, maximum speed 1 m/s, effort 20 N, power 10 W — **proposed**, scaled down from the sourced powered-gate controller (effort 60 N, power 120 W, `engine/SlidingBlade.cs@a6c914e:L14-L20`) because the pin is light.
- No supply: the pin stays extended and the door cannot open.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `speed_threshold` | f32 | 0.5–6 | 3 | m/s | **proposed**: a ball rolling off a 0.8 m-high ramp reaches about 3.3 m/s, so the default separates a ramp-fed ball from a gently placed one |

### Cosmetic curves and UI bindings
- A gate indicator lamp blends slate `#556573` to gold `#f7cb52` over 0.16 s SmoothStep on release — **proposed**, reusing the sourced activation curve (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L53-L54`).
- Door and pin art follow committed body poses. UI: `speed_threshold` through the contextual configuration pattern (`ui/WorkshopConfiguration.cs@a6c914e:L43-L75`).

### Art
Track in the Ramp colour `#c28f52`; door in the same colour with navy `#293954` hinge; gold `#f7cb52` latch pin; speed gate frame cream `#fff8e9` ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed** within the approved palette.

### Catalogue and inventory entry
Id `speed_trapdoor`, title "Speed trapdoor", category Motion — **proposed**. Add `WorkshopPartKind.SpeedTrapdoor` with a counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
One outcome in the row. The campaign coverage row lists unnamed "trapdoor variants" ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); [todo-220](../requirements.md#todo-220) asks to verify the historical selection rule rather than guess a size filter. Open question 1.

## 3. Engine capabilities

Families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, SignalPropagation, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)).

**Exists now**
- Dynamic boxes and spheres with contact and friction: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L701-L719`.
- Approach-speed contact trigger (a speed observation on contact): `engine/gpu/ContactTriggerDeclaration.cs@a6c914e:L7-L18`.
- Residence sensor with a speed limit (upper bound only): `engine/gpu/PhysicsDeclarations.cs@a6c914e:L123-L141`.

**Missing**
- Aperture crossing sensor with a minimum speed: Story 6.8 (CAT-002) builds the directional aperture sensor; the minimum-speed predicate extends it. Owner S376.
- Hinge and slider joints with limits (JointConstraint): Story 6.4 (slider) and Story 10.3 (hinge).
- Finite-work slider actuator: Story 11.3/8.2 (pusher, powered gate). Electrical supply: Story 8.1.

**Dependencies.** CAT-002 (Story 6.8), CAT-062 slider (Story 6.4), CAT-034 hinge (Story 10.3), CAT-051 actuator (Story 8.2), CAT-005 battery (Story 8.1).

## 4. Sources and legacy

- Requirement: "Physical sensor and supplied latch distinguish taught incoming speed"; outcome "Equal-size slow control stays on its declared route" ([element-109](../requirements.md#element-109)). Map: "physics relative-speed observation controls a finite-work trapdoor through typed discrete state, not part-name velocity branch"; decision S257 ([decisions](../invest/decisions.md#s257)).
- [todo-220](../requirements.md#todo-220): hinged/selective passage; test thresholds, heavy/light bodies and moving collision geometry.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Passage sensor declaration: owned rigid frame, aperture radius, rearm clearance | `engine/ScenePassageSensorDeclaration.cs@a6c914e:L3-L4` | carry forward (shape), do not carry forward (class) | Generic aperture sensor data. |
| 2 | Hinge with symmetric travel limits and connected collision disabled | `parts/ImpactLeverPart.cs@a6c914e:L17-L27` | carry forward | Hinge declaration pattern. |
| 3 | Accepted: outside-aperture passage disarms without counting; reverse or stationary motion publishes nothing | `CuriousContraptions.tests/PhysicsPassageSensorTests.cs@a6c914e:L96-L107` | carry forward | Directional, physical sensing. |
| 4 | Slider controller: mass 1 kg, max speed 2.8 m/s, acceleration 14, return spring 14, damping 4, effort 60 N, power 120 W | `engine/SlidingBlade.cs@a6c914e:L12-L26` | carry forward (form), values scaled | Finite-work latch drive. |

No legacy trapdoor exists in code or tests.

**Files harvested:**
- `engine/ScenePassageSensorDeclaration.cs`
- `parts/ImpactLeverPart.cs` (hinge pattern)
- `CuriousContraptions.tests/PhysicsPassageSensorTests.cs`
- `engine/SlidingBlade.cs`
- `parts/PipeBendPart.cs` (material reference)
- Searched with no hit for `trapdoor` or `trap door`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-109](../requirements.md#element-109) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place a ramp, the trapdoor after it and receivers on the upper and lower routes; connect a battery to `PowerIn`; set `speed_threshold`. Run.
- **Positive.** A fast Basketball crosses the gate, the pin retracts, the door drops and the ball reaches the lower route.
- **Negative/control.** An equal-size slow ball rolls over the closed door to the upper route; without supply even a fast ball stays on the upper route.
- **Boundaries.** Speed just below and above the threshold; Bowling ball versus Basketball at equal speed; a ball crossing the gate backwards; a ball resting on the door when it releases.
- **Run/Reset.** Door closed, pin extended, release state clear.
- **Save/Load.** `speed_threshold`, pose and the supply connection round-trip.
- **Integrations.** Scope index [todo-248](../requirements.md#todo-248) and historical selective passage [todo-220](../requirements.md#todo-220); campaign row "spiral delay, airlift and trapdoor variants": first use 21–30, specialist routes 91–100, reuse 41–50, 111–120, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); fed by ramps (CAT-054) and supplied by a battery (CAT-005).

## 6. Open questions

1. Which historical selection rule and which "trapdoor variants" are required (speed only, or also mass)? Unspecified — owner decision.
2. Does the door re-close and re-arm during a Run, or stay open until Reset (proposed)? Owner decision.
3. Should the release state be exposed as an `ActivationOut` socket? Owner decision.
