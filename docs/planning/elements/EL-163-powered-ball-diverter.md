# EL-163 · Powered ball diverter named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-163 |
| Name | Powered ball diverter |
| Type | Mechanical |
| Anchor | [requirements.md#element-163](../requirements.md#element-163); [named entry](../invest/named-elements.md#element-163); scope index [todo-246](../requirements.md#todo-246) |
| Proof owner | S374 |
| Refines / extends | No CAT spec. Junction geometry of [EL-162](EL-162-fixed-ball-diverter.md); finite-work actuator of [CAT-051 powered gate](CAT-051-powered_gate.md); supply from [CAT-005 battery](CAT-005-battery.md) |
| Related | EL-162, EL-164 (same scope index); EL-184 Mechanical ball gate |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes
All full extents.
- **Junction (static).** The EL-162 Y channel: inlet and branches 1.5 × 0.1 × 0.9 m, branches at ±30°, walls 0.5 × 0.06 m, 6° fall — **proposed**, shared with EL-162 for one route vocabulary.
- **Blade (dynamic).** A box 1.0 × 0.5 × 0.06 m hinged at the fork apex about local +Y, swinging between −15° (seals left) and +15° (seals right) — **proposed**, as EL-162.
- Housing for the actuator: a box 0.4 × 0.3 × 0.4 m above the apex — **proposed**: visible drive location.
- One homogeneous box per dynamic body is admitted now (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L226-L229`).

### Mass and material
- Blade 0.5 kg — **proposed**: half the sourced 1 kg gate blade (`engine/SlidingBlade.cs@a6c914e:L14-L14`) because it is half the size.
- Channel and blade restitution 0.1, friction 0.3 — **proposed**, as EL-162.

### Constraints and joints
- Blade hinge about local +Y at the apex, travel −15° to +15°, connected collision disabled — **proposed** limits; hinge-with-limits pattern from `parts/ImpactLeverPart.cs@a6c914e:L17-L27`.
- A finite-work hinge servo with modes Hold, Lower and Upper (`engine/physics/PhysicsServo.cs@a6c914e:L9-L9`). Maximum speed 4 rad/s, acceleration 16 rad/s², torque 15 N·m, power 40 W — **proposed**: swings the 0.52 rad stroke in about 0.3 s, quicker than successive balls arrive, yet a ball wedged against the blade (about 10 N at 0.5 m) stalls it.

### Typed ports
| Socket | Domain | Direction | Command | Source |
| --- | --- | --- | --- | --- |
| `SelectLeftIn` | Activation | Input | select left branch | **proposed**: two explicit typed command inputs, the pattern of the latch `SetIn`/`ResetIn` (`parts/LatchPart.cs@a6c914e:L28-L33`) |
| `SelectRightIn` | Activation | Input | select right branch | same |
| `PowerIn` | Electrical | Input | actuator supply | existing identity `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9` |

### Sensors and activation
A command sets the servo mode toward its branch; Hold is entered at the end stop. A command never moves the blade without supply.

### Work and energy stores
None internal. All blade work comes from `PowerIn`; kinetic energy of the blade never exceeds supplied work (legacy acceptance, row 3).

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `initial_branch` | enum `DiverterBranch { Left, Right }` | 2 values | Left | — | **proposed**: authored start pose, shared enum with EL-162 |

### Cosmetic curves and UI bindings
- Blade art follows the committed hinge pose. A supply lamp blends slate `#556573` → gold `#f7cb52` over 0.1 s SmoothStep while supplied, as the motor lamp ([CAT-042 spec](CAT-042-motor.md)) — **proposed**; a jam (stalled blade between stops while commanded) shows the amber `#e8b764` blocked colour of the powered gate (`parts/PoweredGatePart.cs@a6c914e:L57-L63`).
- UI: `initial_branch` through the configuration pattern (`ui/WorkshopConfiguration.cs@a6c914e:L43-L75`); wires through the contextual Connect.

### Art
EL-162 palette: Ramp colour `#c28f52` channel, gold `#f7cb52` blade, navy `#293954` actuator housing ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.

### Catalogue and inventory entry
Id `diverter_powered`, title "Powered diverter", category Motion — **proposed**. Add `WorkshopPartKind.PoweredDiverter` with a counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
One outcome in the row; fixed and alternating modes are EL-162 and EL-164.

## 3. Engine capabilities

Families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, SignalPropagation, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)).

**Exists now**
- Typed activation inputs and the activation network: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`; `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L12`.
- Dynamic box, contact and friction: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`.

**Missing**
- Hinge with limits (Story 10.3) and a finite-work servo row (Story 11.3/8.2 actuator). Owner S374.
- Electrical supply (Story 8.1); decision S257 electrical-port for signal versus power ([decisions](../invest/decisions.md#s257)).
- Supply-lamp and blocked cosmetic sources (closed set `engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`).

**Dependencies.** EL-162 geometry; CAT-005 battery; CAT-051 actuator family; an activation source such as CAT-063 switch.

## 4. Sources and legacy

- Requirement: "Finite-work actuator changes a routing blade after a command"; outcome "Missing supply or a jam prevents instant route switching" ([element-163](../requirements.md#element-163)).
- Integration under [todo-246](../requirements.md#todo-246): powered mode is added and proved after fixed selection.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Servo modes Hold, Lower, Upper | `engine/physics/PhysicsServo.cs@a6c914e:L9-L9` | carry forward (closed set) | Commanded end-stop motion. |
| 2 | Accepted: ramped command speed (max 2, acceleration 4 in the fixture), reversal and Hold locking the joint at its current coordinate with no further work; replay identical | `CuriousContraptions.tests/PhysicsServoTests.cs@a6c914e:L20-L67` | carry forward (behaviour) | Finite, rate-limited blade motion. |
| 3 | Accepted: supply is finite; blade kinetic energy ≤ supplied work; a direct motor command cannot bypass the servo | `CuriousContraptions.tests/PhysicsServoTests.cs@a6c914e:L69-L90` | carry forward | No free switching energy. |
| 4 | Gate states Closed, Opening, Open, Closing, Blocked; Blocked when speed < 1e-5 between end stops | `engine/SlidingBlade.cs@a6c914e:L7-L7`, `engine/SlidingBlade.cs@a6c914e:L49-L53` | carry forward (states), do not carry forward (1e-5 literal) | Jam is observable state; tolerance is game-grade. |
| 5 | Powered controller drives toward the stroke at min(max speed, √(2·a·distance)), effort 60 N, power 120 W, per-step `Prepare` | `engine/SlidingBlade.cs@a6c914e:L36-L47` | carry forward (profile), do not carry forward (per-step loop) | Per-element update loops are banned. |
| 6 | Accepted: a closing blade meeting moving cargo stops on it; Reset replays | `CuriousContraptions.tests/PoweredGateTests.cs@a6c914e:L179-L241` | carry forward | Jam behaviour. |

No legacy diverter exists.

**Files harvested:**
- `engine/physics/PhysicsServo.cs`
- `engine/SlidingBlade.cs`
- `parts/PoweredGatePart.cs` (blocked indicator)
- `parts/ImpactLeverPart.cs` (hinge pattern)
- `parts/LatchPart.cs` (typed command sockets)
- `CuriousContraptions.tests/PhysicsServoTests.cs`
- `CuriousContraptions.tests/PoweredGateTests.cs`
- Checked, no extra diverter knowledge: `engine/SceneServoDeclaration.cs` (servo record: joint, maximum speed, acceleration, effort, power).
- Searched with no hit for `diverter`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-163](../requirements.md#element-163) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place the diverter, a battery, a switch and receivers under both branches; connect `Supply` → `PowerIn` and switch `ActivationOut` → `SelectRightIn`. Run; let a ball hit the switch, then send a second ball.
- **Positive.** After the command the blade swings to Right and the next ball takes the right branch.
- **Negative/control.** Without supply the command leaves the blade at Left; a ball held against the blade jams it and the blade does not pass through the ball; a wire from an electrical output into `SelectRightIn` is refused.
- **Boundaries.** Command arriving while a ball is mid-junction; both commands in one Run; supply depleted mid-swing.
- **Run/Reset.** Blade returns to `initial_branch`; servo and supply state clear.
- **Save/Load.** `initial_branch`, pose and both connection domains round-trip.
- **Integrations.** Y-junction integration task under [todo-246](../requirements.md#todo-246) (powered mode proved after EL-162); campaign row "... fixed/powered/alternating diverter ...": first use 21–30, reuse 41–50, 111–120, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); commanded by CAT-063 switch, supplied by CAT-005 battery.

## 6. Open questions

1. Two typed select inputs (proposed) or a single toggle input? Unspecified — owner decision.
2. Does an unsupplied blade hold its pose (proposed) or return to a default branch by spring? Owner decision.
3. Servo values (proposed): confirm switching time and jam force. Owner decision.
