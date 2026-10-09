# EL-199 · Electric motor named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-199 |
| Name | Electric motor |
| Type | Mechanical |
| Anchor | [requirements.md#element-199](../requirements.md#element-199); [named entry](../invest/named-elements.md#element-199); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S305 |
| Refines / extends | Refines [CAT-042 motor](CAT-042-motor.md) ([requirement](../requirements.md#current-cat-042)); the full legacy harvest is in that spec |
| Related | EL-196 Battery, EL-202 Conveyor, EL-203 Reverse transmission, EL-110 Flywheel, EL-209 Fan (shaft supply) |
| Roadmap story | Story 11.1 (CAT-042 with CAT-019) |
| Status | not started |

## 2. Declaration

Geometry, ports and parameters are sourced through [CAT-042](CAT-042-motor.md); the declared-loss terms the EL row adds are proposed.

### Bodies and shapes
- **Housing (static).** Base box full size 1.25 × 0.16 × 1 m at (0, −0.43, 0) and body box 1 × 0.8 × 0.85 m at the origin (`parts/MotorPart.cs@a6c914e:L64-L71`).
- **Rotor (dynamic shaft).** Radius 0.3 m, width 0.13 m, centred at (0, 0, 0.54) (`parts/MotorPart.cs@a6c914e:L24-L27`).

### Mass and material
Rotor 0.5 kg; axial inertia m·r²/2 = 0.0225 kg·m², transverse m·(3r² + w²)/12 (`parts/MotorPart.cs@a6c914e:L24-L26`). Contact material: see [CAT-042 open questions](CAT-042-motor.md#6-open-questions).

### Constraints and joints
Free revolute hinge about local +Z at the rotor centre, travel both ways, connected collision disabled (`parts/MotorPart.cs@a6c914e:L30-L35`), with a bounded motor row (target speed, maximum torque, work and power budgets).

### Typed ports
| Socket | Domain | Direction | Local position (m) | Source |
| --- | --- | --- | --- | --- |
| `PowerIn` | Electrical | Input | (−0.55, 0, 0) | `parts/MotorPart.cs@a6c914e:L49-L53` |
| `Drive` | Mechanical | Output, −1 coordinate per radian | (0, 0, 0.65) | `parts/MotorPart.cs@a6c914e:L49-L54` |

### Sensors and activation
No activation input; supply alone drives it. An activation command creates no supply (see [CAT-042](CAT-042-motor.md)).

### Work and energy stores
- Per step: target speed = `speed`; maximum torque = `torque` while supplied, else 0; work budget torque·speed·Δt; power cap torque·speed (`parts/MotorPart.cs@a6c914e:L91-L97`). Zero effort disables actuation; zero work permits only braking (`engine/physics/PhysicsMotorCommand.cs@a6c914e:L5-L22`).
- **Declared losses.** Electrical draw = delivered shaft power ÷ `efficiency`; the difference is dissipated as heat in the finite ledger — **proposed**: the row's "declared losses" without inventing a stall-current model; a stalled shaft delivers no shaft power, so it draws no energy beyond what it delivers.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `speed` | f32 | 0–20 | 6 | rad/s | `parts/MotorPart.cs@a6c914e:L55-L63`, `parts/catalog/motor.tres@a6c914e:L14-L14` |
| `torque` | f32 | > 0 to 100 | 20 | N·m | same |
| `efficiency` | f32 | 0.5–1.0 | 0.8 | ratio | **proposed**: a toy DC motor delivers most of its input; 0.8 makes the battery drain visibly faster than the shaft work alone |

### Cosmetic curves and UI bindings
Supply lamp blends slate `#556573` → gold `#f7cb52` over 0.1 s SmoothStep while supplied (`parts/MotorPart.cs@a6c914e:L40-L45`); rotor and gold index follow the committed shaft pose. UI: `speed` and `torque` (and `efficiency` if adopted) through the configuration pattern (`ui/WorkshopConfiguration.cs@a6c914e:L43-L75`).

### Art
Electric motor colour `#66b8c9` (`parts/catalog/motor.tres@a6c914e:L13-L13`); navy `#293954` base; cream wheel; gold lamp and index ([CAT-042 art](CAT-042-motor.md); [DESIGN colour system](../../../DESIGN.md#colour-system); [mechanical work feedback](../../../DESIGN.md#mechanical-work-feedback)).

### Catalogue and inventory entry
Id `motor`, title "Electric motor", category Power (`parts/catalog/motor.tres@a6c914e:L8-L14`). Add `WorkshopPartKind.Motor` with a counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
One outcome in the row; no variants.

## 3. Engine capabilities

Families: ElectricalPower, EnvironmentState, FiniteLedger, FiniteWorkActuation, JointConstraint, RigidBodyDynamics, ShaftTorque, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)).

**Exists now**
- `PowerIn` socket identity and Electrical domain: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`.
- Dynamic bodies with the 128 rad/s spin clamp: `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L208-L215`.

**Missing** (all in [CAT-042](CAT-042-motor.md#3-engine-capabilities))
- Hinge (Story 10.3), bounded motor row with committed work totals and the `Mechanical` domain (Story 11.1), cylinder collider (Story 11.1 or 6.6), battery supply (Story 8.1); decision S257 electrical-port ([decisions](../invest/decisions.md#s257)).
- Loss accounting into the finite ledger: Story 11.1 with Story 8.1. Owner S305.

**Dependencies.** CAT-005 battery (Story 8.1); first consumer CAT-019 conveyor (Story 11.1).

## 4. Sources and legacy

- Requirement: "Supplied electrical work becomes bounded shaft torque with declared losses"; outcome "Stalled shaft consumes only supported work and cannot bypass load limits" ([element-199](../requirements.md#element-199)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Rotor mass, radius, width, inertia and centre | `parts/MotorPart.cs@a6c914e:L19-L27` | carry forward | Finite rotor. |
| 2 | Supply law: torque only while powered; work torque·speed·Δt; power cap torque·speed | `parts/MotorPart.cs@a6c914e:L91-L97` | carry forward (law), do not carry forward (per-part `PreparePhysics`) | Per-element loops are banned. |
| 3 | Finite non-negative budgets; zero effort disables; zero work only brakes | `engine/physics/PhysicsMotorCommand.cs@a6c914e:L5-L22` | carry forward | Bounded drive. |
| 4 | Accepted: a locked guide stops the rotor (speed and travel ≤ 1e-7); unlocking resumes > 0.1 rad/s | `CuriousContraptions.tests/SourceRotorTests.cs@a6c914e:L125-L148` | carry forward (behaviour), do not carry forward (1e-7) | Stall control. |
| 5 | Accepted: after supply loss an unloaded rotor coasts with constant energy and no new work | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L113-L123` | carry forward | Supply loss is not a brake. |
| 6 | No efficiency or loss term in the legacy | `parts/MotorPart.cs@a6c914e:L91-L97` | open | The EL row's "declared losses" is new; open question 1. |

**Files harvested:**
- `parts/MotorPart.cs`
- `parts/catalog/motor.tres`
- `engine/physics/PhysicsMotorCommand.cs`
- `CuriousContraptions.tests/SourceRotorTests.cs`
- `CuriousContraptions.tests/MechanicalTests.cs`
- The remaining motor files are harvested in [CAT-042](CAT-042-motor.md#4-legacy-harvest) (Batch C). Searched with no hit for a motor efficiency or loss term: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-199](../requirements.md#element-199) and the [CAT-042 outline](CAT-042-motor.md#5-acceptance-outline).
- **Chrome UI recipe.** Open `battery_motor`; place the battery; connect `Supply` → `PowerIn`; Run.
- **Positive.** The rotor reaches its bounded speed; supplied work grows; the battery drains by delivered work ÷ efficiency.
- **Negative/control.** No battery or a depleted battery gives no rotation; an activation wire into the motor is refused.
- **Boundaries.** Stalled shaft (no shaft work, no extra draw); torque 0.01 and 100; speed 0 and 20; supply loss coasting.
- **Run/Reset.** Rotor angle, speed, lamp and work totals return to authored values.
- **Save/Load.** Parameters, pose and connections round-trip.
- **Integrations.** Retained battery-and-motor behaviour [todo-146](../requirements.md#todo-146); campaign row 2 ("... motor ..."): first use 11–20, moving/load variants into 41–50, reuse 31–50, 61–80, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); drives CAT-019 conveyor, CAT-057 reverser and EL-110 flywheel.

## 6. Open questions

1. "Declared losses": an efficiency ratio (proposed), a winding resistance, or both? Unspecified — owner decision.
2. Must a stalled motor draw stall power as heat, or nothing (proposed)? Owner decision.
3. The [CAT-042 open questions](CAT-042-motor.md#6-open-questions) remain.
