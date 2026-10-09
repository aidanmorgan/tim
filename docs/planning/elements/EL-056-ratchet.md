# EL-056 · Ratchet declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-056 |
| Name | Ratchet |
| Type | Mechanical |
| Requirement anchor | [element-056](../requirements.md#element-056); scope index [todo-375](../requirements.md#todo-375) |
| Named entry | [named-elements.md#element-056](../invest/named-elements.md#element-056); proof owner S342 |
| CAT spec refined or extended | none as a part. The one-way joint law is shared with [CAT-071 wound spring](CAT-071-wound_spring.md) (latched plunger guide). Related: [CAT-042 motor](CAT-042-motor.md), [CAT-019 conveyor](CAT-019-conveyor.md) |
| Related identities | [EL-057 escapement](EL-057-escapement.md), [EL-055 brake](EL-055-mechanical-brake.md), [EL-063 cable winch](EL-063-cable-winch.md) (hold a lifted load) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes

- Static housing box 0.7 × 0.7 × 0.4 m. **Proposed** (same family as the brake housing).
- Dynamic pass-through shaft carrying the toothed wheel: cylinder r 0.25 m, width 0.12 m, 0.25 kg (mass from the legacy winding shaft, `parts/WoundSpringPart.cs@a6c914e:L71-L71`). **Proposed** radius.
- Pawl: cosmetic child; it has no body (its holding action is the joint row below).

### Mass and material

Housing: static default material restitution 1, threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`).

### Constraints and joints

- Shaft hinge to the housing.
- Directional row on the hinge coordinate: impulse bounded to one sign so free rotation in the permitted direction is unresisted and reverse rotation is stopped (legacy `JointTravelDirection` row, `engine/physics/PhysicsJoint.cs@a6c914e:L170-L178`). Holding torque is bounded by `holding_capacity`; above it the ratchet slips back by whole teeth.
- Tooth engagement: reversal is caught at the next tooth boundary, so up to one tooth pitch of back-travel is visible. The legacy row was continuous (no pitch); the pitch is new.

### Typed ports

| Socket | Domain | Direction | Local position (m) |
| --- | --- | --- | --- |
| `DriveIn` | Mechanical | Input | (−0.35, 0, 0.25) |
| `Drive` | Mechanical | Output | (0.35, 0, 0.25) |

Identities `engine/MachineData.cs@a6c914e:L104-L108`; positions **proposed**.

### Sensors and activation

None. The row asks for passive one-way holding; release is a separate element (EL-057 or a brake).

### Work and energy stores

None. The ratchet stores nothing; a held load keeps its own potential energy.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `free_direction` | enum `RatchetDirection { Clockwise, Counterclockwise }` | — | Clockwise | — | Legacy enum {Both, Positive, Negative} (`engine/physics/PhysicsJoint.cs@a6c914e:L117-L117`); `Both` is excluded because it is not a ratchet (`CuriousContraptions.tests/PhysicsJointTests.cs@a6c914e:L106-L115`). Clockwise viewed from +Z is positive, as for the motor ([CAT-042](CAT-042-motor.md)) |
| `tooth_count` | int | 6–36 | 12 | teeth | **Proposed**: 30° pitch reads clearly at 0.25 m radius |
| `holding_capacity` | f32 | 1–200 | 40 | N·m | **Proposed**: twice the default motor torque, so a default motor never back-drives it |

### Cosmetic curves and UI bindings

Wheel teeth follow the committed shaft angle; the pawl clicks to the next tooth as each tooth boundary passes (presentation derived from the committed angle, no separate timer).

### Art

Cream housing `#fff8e9`, navy foot `#293954`, pale grey toothed wheel `#ccd9df`, gold pawl `#f7cb52`, navy arrow showing the free direction (shape cue, not colour only). Catalogue colour **proposed**: ochre `#d69c47` (`DESIGN.md@a6c914e:L176-L183`).

### Catalogue and inventory entry

Id `ratchet`, title "Ratchet", category "Motion"; `WorkshopPartKind.Ratchet` appended last to the free inventory (`engine/gpu/WorkshopInventory.cs@a6c914e:L49-L60`). **Proposed**.

### Variants

The requirements row names no variants or modes. `free_direction` is a parameter of one element.

## 3. Engine capabilities

Binding: EnvironmentState, FiniteLedger, FiniteWorkActuation, JointConstraint, RigidBodyDynamics, ShaftTorque (map); coverage JSON adds StateTransaction. Map note: "Directional tooth/contact constraints permit one direction and resist reversal; no sign-clamped commanded velocity."

**Exists now:** static boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`).

**Missing**

- Hinge row: Story 10.3.
- Cylinder collider and inertia (toothed wheel): no owning story; owner decision 9 Oct 2026: a new cylinder-collider story comes before the first shaft wheel.
- One-sign bounded joint row (directional limit): first built for CAT-071 in Story 11.4 ("paid winding/ratchet/release").
- `Mechanical` domain and pass-through shaft: Story 11.1.
- Tooth-pitch quantisation of the catch: no story; decision owner S342.

**Dependencies.** A drive (CAT-042 Motor, 11.1) and a reverse load (EL-063 Winch with a CAT-067 Weight, or a hanging load on a drum).

## 4. Sources and legacy

- Requirements row: "Reverse load holds within capacity without inventing forward movement." No variants.
- Research row: "Ratchet / Escapement — one-way hinge limit; tooth count; holds gained motion" (`docs/component-research.md@a6c914e:L78-L78`).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Direction policy is an enum {Both, Positive, Negative} allowed only on axial (hinge or slider) joints. | `engine/physics/PhysicsJoint.cs@a6c914e:L117-L117`, `engine/physics/PhysicsJoint.cs@a6c914e:L131-L134` | carry forward | Typed direction; ratchet needs a hinge. |
| 2 | A one-way joint adds a unilateral row on the travel axis whose impulse is bounded to [0, ∞) or (−∞, 0]. | `engine/physics/PhysicsJoint.cs@a6c914e:L170-L178` | carry forward (law) | Impulse bound, not a clamped velocity, satisfies the map note. |
| 3 | `Both` is the negative control: travel bounds alone do not make a ratchet; a one-way joint hit in reverse stops dead (zero reverse velocity) and replays identically after restore. | `CuriousContraptions.tests/PhysicsJointTests.cs@a6c914e:L106-L145` | carry forward (acceptance) | Positive/control pair. |
| 4 | A ratchet stops a reverse impulse without advancing its lower bound: position retained within 1e-7 m and speed 0 after a −2 N·s impulse. | `CuriousContraptions.tests/ScenePlungerBindingTests.cs@a6c914e:L119-L142` | carry forward (re-freeze tolerance under f32) | "Holds without inventing forward movement". |
| 5 | Ratchet and drive losses may dissipate energy, never create it. | `CuriousContraptions.tests/WoundSpringRuntimeTests.cs@a6c914e:L280-L280` | carry forward | Energy bound. |
| 6 | Event-chatter suppression and CPU acceleration rows for active ratchets. | `CuriousContraptions.tests/JointBoundaryTests.cs@a6c914e:L112-L112`; `engine/physics/PhysicsJoint.cs@a6c914e:L192-L198` | do not carry forward | CPU solver event machinery. |

**Files harvested:** `engine/physics/PhysicsJoint.cs`, `CuriousContraptions.tests/PhysicsJointTests.cs`, `CuriousContraptions.tests/ScenePlungerBindingTests.cs`, `CuriousContraptions.tests/JointBoundaryTests.cs`, `CuriousContraptions.tests/SharedContactMigrationTests.cs` (checked; plunger-contact ratchet, same law), `CuriousContraptions.tests/WoundSpringRuntimeTests.cs`, `parts/WoundSpringPart.cs`.

## 5. Acceptance outline

Requirement row: [element-056](../requirements.md#element-056).

- **Chrome UI recipe.** Place Battery, Motor, Ratchet and Cable winch with a Weight on its cable; wire Battery → Motor; belt Motor `Drive` → Ratchet `DriveIn`, Ratchet `Drive` → Winch `DriveIn`; a Switch cuts motor supply mid-lift (or the battery is omitted in the control). Verify placement, direction and links.
- **Positive.** Run: the motor lifts the Weight; when supply stops the Weight holds within one tooth pitch of its lifted height and the shaft stays still.
- **Negative/control.** Same construction with `free_direction` reversed: the motor cannot lift (the ratchet blocks the drive direction) and nothing moves forward. Without a motor the hanging Weight cannot turn the shaft backwards and nothing advances.
- **Boundaries.** Reverse torque just below and above `holding_capacity` (holds vs slips whole teeth); tooth count 6 and 36; out-of-range rejected.
- **Run/Reset and Save/Load.** Shaft angle and pawl phase restore; direction, teeth and capacity round-trip.
- **Integrations.** Cross-element task [sequence-task-370](../requirements.md#sequence-task-370) under [todo-375](../requirements.md#todo-375): "a ratchet/escapement meters motion", each named mechanism separately implemented and taught.

## 6. Open questions

1. Over-capacity behaviour: slip back by teeth (proposed), or hold rigidly (infinite capacity)? Owner decision.
2. Is a linear ratchet (slider form, as in the wound spring plunger) a mode of this element or a separate identity? Owner decision.
3. Should a release command be added (pawl lift), or is release always a separate element? Owner decision.
