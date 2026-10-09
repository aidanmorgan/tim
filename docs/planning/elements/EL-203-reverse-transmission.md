# EL-203 · Reverse transmission named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-203 |
| Name | Reverse transmission |
| Type | Mechanical |
| Anchor | [requirements.md#element-203](../requirements.md#element-203); [named entry](../invest/named-elements.md#element-203); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S312 |
| Refines / extends | Refines [CAT-057 reverse transmission](CAT-057-reverse_transmission.md) ([requirement](../requirements.md#current-cat-057)); the full legacy harvest is in that spec |
| Related | EL-199 Electric motor, EL-202 Conveyor, EL-200 Drive belt, CAT-018 Clutch (same ratio row with ratio +1) |
| Roadmap story | Story 11.2 (CAT-018 with CAT-057) |
| Status | not started |

## 2. Declaration

All values are sourced through [CAT-057](CAT-057-reverse_transmission.md); this EL adds no new value.

### Bodies and shapes
- **Housing (static).** Navy base box full size 1.5 × 0.16 × 0.9 m at (0, −0.4, 0) and cream housing box 1.4 × 0.65 × 0.5 m at (0, 0, −0.08); both collide (`parts/ReverseTransmissionPart.cs@a6c914e:L36-L37`).
- **Input and output wheels (dynamic shafts).** Radius 0.32 m, width 0.14 m, hinge anchors at (∓0.36, 0, 0.32) about local Z (`parts/ReverseTransmissionPart.cs@a6c914e:L11-L16`).

### Mass and material
Each wheel 0.25 kg; inertia axial m·r²/2, transverse m·(3r² + w²)/12 (`parts/ReverseTransmissionPart.cs@a6c914e:L11-L12`; `engine/SceneRotaryShaft.cs@a6c914e:L13-L24`). Material: [CAT-057](CAT-057-reverse_transmission.md).

### Constraints and joints
Two free hinges to the housing and one always-engaged, phase-free ratio row of −1 between them (`parts/ReverseTransmissionPart.cs@a6c914e:L13-L18`). The row couples speeds through shared inertia; no endpoint receives copied speed or work (`engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L9-L13`).

### Typed ports
| Socket | Domain | Direction | Local position (m) | Source |
| --- | --- | --- | --- | --- |
| `DriveIn` | Mechanical | Input, −1 per radian on the input hinge | (−0.36, 0, 0.4) | `parts/ReverseTransmissionPart.cs@a6c914e:L25-L30` |
| `Drive` | Mechanical | Output, −1 per radian on the output hinge | (0.36, 0, 0.4) | same |

### Sensors and activation
None.

### Work and energy stores
None. Kinetic energy lives only in the two finite wheels; reversal never adds work, so the output cannot deliver more than the input supplies.

### Parameters
None (`parts/catalog/reverse_transmission.tres@a6c914e:L14-L14`); the −1 ratio is fixed declaration data.

### Cosmetic curves and UI bindings
Wheels follow committed hinge poses; readings are clockwise-positive (`parts/ReverseTransmissionPart.cs@a6c914e:L21-L24`); index bars turn in opposite senses.

### Art
Reverse transmission colour `#d69c47`; navy `#293954` base and index bars; cream `#fff8e9` housing; gold `#f7cb52` index spheres (`parts/ReverseTransmissionPart.cs@a6c914e:L36-L52`; `parts/catalog/reverse_transmission.tres@a6c914e:L13-L13`; [DESIGN colour system](../../../DESIGN.md#colour-system)).

### Catalogue and inventory entry
Id `reverse_transmission`, title "Reverse gear", category Motion; "It reverses rotation at the same speed; two reversers restore the original direction." (`parts/catalog/reverse_transmission.tres@a6c914e:L8-L14`). Add a `WorkshopPartKind` member with a counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
The row says "gear/belt arrangement" but names one outcome. The legacy is a gear-pair housing; a crossed-belt arrangement is not separately specified (open question 1).

## 3. Engine capabilities

Families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, ShaftTorque, SlidingFriction, plus StateTransaction ([map row](../general-engine-element-map.md); [element-03.json](../../coverage/engine/element-03.json)).

**Exists now**
- Dynamic bodies with declared inertia: `engine/gpu/RigidMassProperties.cs@a6c914e:L26-L51`.

**Missing** ([CAT-057](CAT-057-reverse_transmission.md))
- Hinges (Story 10.3), the ratio coupling row and mechanical domain (Stories 11.1–11.2), cylinder colliders. Decision S257 mechanical-port ([decisions](../invest/decisions.md#s257)). Owner S312.

**Dependencies.** CAT-042 motor and CAT-019 conveyor (Story 11.1).

## 4. Sources and legacy

- Requirement: "Declared gear/belt arrangement reverses signed shaft motion under load"; outcome "It cannot reverse by duplicating input work" ([element-203](../requirements.md#element-203)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Two 0.25 kg wheels, r 0.32, w 0.14; hinges at (∓0.36, 0, 0.32); ratio −1 always engaged | `parts/ReverseTransmissionPart.cs@a6c914e:L11-L18` | carry forward | Declaration. |
| 2 | Ideal phase-free transmission; shared inertia sets speeds and reactions; no copied speed or work allowance | `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L9-L13` | carry forward (law), do not carry forward (CPU joint) | "Cannot duplicate input work." |
| 3 | Accepted: through reversers the last conveyor's speed is the motor speed times (−1)^reversers on the same substep; each reverser's output speed is −input speed | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L100-L112` | carry forward | Signed reversal. |
| 4 | Level `reverse_belt` ("The other way round"): reverse the drive so the conveyor carries the ball back to the basket | `content/puzzles.json@a6c914e:L4648-L5070` | carry forward | Lesson. |

**Files harvested:**
- `parts/ReverseTransmissionPart.cs`
- `parts/catalog/reverse_transmission.tres`
- `engine/SceneRotaryShaft.cs`
- `engine/physics/PhysicsTransmissionJoint.cs`
- `CuriousContraptions.tests/MechanicalTests.cs`
- `content/puzzles.json` (level `reverse_belt`)
- The remaining reverser files are harvested in [CAT-057](CAT-057-reverse_transmission.md#4-legacy-harvest) (Batch C). Searched with no hit for a crossed-belt reverser: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-203](../requirements.md#element-203) and the [CAT-057 spec](CAT-057-reverse_transmission.md).
- **Chrome UI recipe.** Open `reverse_belt`; place the reverser; connect motor `Drive` → reverser `DriveIn` and reverser `Drive` → conveyor `DriveIn`; Run.
- **Positive.** The conveyor runs backward and delivers the ball to the basket.
- **Negative/control.** A direct motor-to-conveyor belt runs forward and fails; two reversers restore forward travel; a loaded output never runs faster than the input.
- **Boundaries.** Stall under load; supply loss coasting; rotated housing (sign unchanged).
- **Run/Reset.** Wheels return to zero angle.
- **Save/Load.** Pose and links round-trip.
- **Integrations.** Retained belt behaviour with the −1 reverser [todo-151](../requirements.md#todo-151) and relay/reverse teaching order [todo-152](../requirements.md#todo-152); campaign row 2 ("... reverse transmission ..."): first use 11–20, moving/load variants into 41–50, reuse 31–50, 61–80, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); between CAT-042 motor and CAT-019 conveyor; sibling of CAT-018 clutch (Story 11.2).

## 6. Open questions

1. Does "gear/belt arrangement" require a separate crossed-belt reverser, or only this gear housing? Unspecified — owner decision.
