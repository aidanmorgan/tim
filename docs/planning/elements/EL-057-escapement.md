# EL-057 · Escapement declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-057 |
| Name | Escapement |
| Type | Mechanical |
| Requirement anchor | [element-057](../requirements.md#element-057); scope index [todo-375](../requirements.md#todo-375) |
| Named entry | [named-elements.md#element-057](../invest/named-elements.md#element-057); proof owner S343 |
| CAT spec refined or extended | none. Related: [CAT-017 clock](CAT-017-clock.md) (periodic trigger source, Story 9.3), [CAT-063 switch](CAT-063-switch.md), [CAT-071 wound spring](CAT-071-wound_spring.md) (stored drive) |
| Related identities | [EL-056 ratchet](EL-056-ratchet.md) (shared tooth wheel), [EL-063 cable winch](EL-063-cable-winch.md) (weight-driven drum as stored drive), [EL-061 indexed carousel](EL-061-indexed-carousel.md) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes

- Static housing box 0.8 × 0.9 × 0.4 m. **Proposed**: taller than the ratchet to show the pallet above the wheel.
- Dynamic escape-wheel shaft: cylinder r 0.25 m, width 0.12 m, 0.25 kg (legacy shaft mass, `parts/WoundSpringPart.cs@a6c914e:L71-L71`). **Proposed** radius.
- Pallet: cosmetic child that rocks once per release; its stopping action is the joint row below.

### Mass and material

Housing: static default material (restitution 1, threshold 0.1 m/s, friction 0.3; `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`).

### Constraints and joints

- Wheel hinge to the housing.
- Stop row: while Locked, a two-sided hinge limit holds the wheel at the current tooth angle (holding torque ≤ `holding_capacity`). A release occurrence moves the limit to the next tooth angle (2π / `tooth_count` in the drive direction); the wheel then advances only if a stored drive torque turns it, and stops at the new limit. The mechanism never moves the wheel itself.

### Typed ports

| Socket | Domain | Direction | Local position (m) |
| --- | --- | --- | --- |
| `DriveIn` | Mechanical | Input (loaded drive) | (−0.4, 0, 0.25) |
| `Drive` | Mechanical | Output (stepped) | (0.4, 0, 0.25) |
| `ActivationIn` | Activation | Input (release trigger) | (0, 0.45, 0) |
| `ActivationOut` | Activation | Output (step completed) | (0, −0.45, 0) |

Identities `engine/MachineData.cs@a6c914e:L104-L108`; positions **proposed**.

### Sensors and activation

- Each `ActivationIn` occurrence releases exactly one tooth. Occurrences arriving while a step is in progress are not queued (**proposed**).
- `ActivationOut` emits once when the wheel settles at the new tooth: relative speed below 0.05 rad/s. **Proposed** threshold: `docs/gpu-f32-physics.md` names 0.05 m/s (linear resting contact) and 0.02 rad/s (island sleep) but no step-settle value; 0.05 rad/s is looser than the sleep threshold so a completed step reports promptly.
- A release with no stored drive produces no step and no `ActivationOut`.

### Work and energy stores

None inside the escapement. The stored drive lives upstream (a hanging Weight on a winch drum, or a wound spring) and is debited only by the step it actually makes.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `tooth_count` | int | 4–24 | 8 | teeth | **Proposed**: 45° steps are clearly discrete; research row names tooth count as the parameter (`docs/component-research.md@a6c914e:L78-L78`) |
| `drive_direction` | enum `RatchetDirection { Clockwise, Counterclockwise }` | — | Clockwise | — | **Proposed**; reuses the ratchet's enum |
| `holding_capacity` | f32 | 1–200 | 40 | N·m | **Proposed**: matches EL-056 |

### Cosmetic curves and UI bindings

The pallet rocks between two angles on each release (presentation keyed to the committed release occurrence); wheel teeth follow the committed shaft angle; a slate/gold lamp flashes on `ActivationOut`.

### Art

Cream housing `#fff8e9`, navy foot `#293954`, pale grey wheel `#ccd9df`, gold pallet `#f7cb52`, ochre accent `#d69c47`. Catalogue colour **proposed**: ochre (`DESIGN.md@a6c914e:L176-L183`).

### Catalogue and inventory entry

Id `escapement`, title "Escapement", category "Motion"; `WorkshopPartKind.Escapement` appended last to the free inventory (`engine/gpu/WorkshopInventory.cs@a6c914e:L49-L60`). **Proposed**.

### Variants

The requirements row names no variants or modes. One triggered element is specified (a free-running pendulum escapement is not in the row).

## 3. Engine capabilities

Binding: EnvironmentState, FiniteLedger, FiniteWorkActuation, JointConstraint, RigidBodyDynamics, ShaftTorque (map); coverage JSON adds StateTransaction. Map note: "Loaded mechanical release/stop geometry yields discrete steps; timer alone does not advance the mechanism."

**Exists now:** activation contact sources, latches and timers (`engine/gpu/ActivationNetwork.cs@a6c914e:L7-L12`); static boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`).

**Missing**

- Hinge with movable two-sided limit: Story 10.3 (hinge, end stops); the movable limit has no story (decision owner S343).
- Cylinder collider and inertia (escape wheel): no owning story; owner decision 9 Oct 2026: a new cylinder-collider story comes before the first shaft wheel.
- `Mechanical` domain and shaft pass-through: Story 11.1.
- Repeated release occurrences: current activation latches are one-way per run (`engine/gpu/ActivationNetwork.cs@a6c914e:L9-L9`); periodic pulses arrive with CAT-017 Clock (Story 9.3).
- Step-completed occurrence emitted from committed joint state: no story; decision owner S343.

**Dependencies.** A stored drive (EL-063 Winch with CAT-067 Weight, or CAT-071 Wound spring, Story 11.4); a trigger source (CAT-063 Switch delivered; CAT-017 Clock, Story 9.3).

## 4. Sources and legacy

- Requirements row: "No stored drive means no step despite a release trigger." No variants.
- Research rows: ratchet/escapement "stepped release of a stored load" (`docs/component-research.md@a6c914e:L78-L78`); "escapement feeder — one-at-a-time release" (`docs/component-research.md@a6c914e:L82-L82`).

No legacy escapement exists. Facts reused from shared legacy mechanisms:

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Joint travel bounds are finite, ordered and only for sliders or hinges strictly inside (−π, π). | `engine/physics/PhysicsJoint.cs@a6c914e:L104-L113`, `engine/physics/PhysicsJoint.cs@a6c914e:L137-L139` | carry forward (hinge-limit admission) | The stop row is a hinge limit; the principal-angle branch rule matters for a wheel that turns many times. |
| 2 | Holding with zero work does not need an energy source. | `CuriousContraptions.tests/MotorPredictionTests.cs@a6c914e:L149-L150` | carry forward | A locked escapement holds without drawing work. |
| 3 | With no stored energy, a trigger produces no release (`SpringTriggerResult.Empty`, release count 0). | `CuriousContraptions.tests/WoundSpringRuntimeTests.cs@a6c914e:L129-L137` | carry forward (pattern) | Mirrors "no stored drive, no step". |

**Files harvested:** `engine/physics/PhysicsJoint.cs`, `CuriousContraptions.tests/MotorPredictionTests.cs`, `CuriousContraptions.tests/WoundSpringRuntimeTests.cs`, `parts/WoundSpringPart.cs`. Searched with no hit: `parts/`, `engine/`, `content/puzzles.json`, `tools/Campaign`, `reference/` (terms: escapement, pallet, escape).

## 5. Acceptance outline

Requirement row: [element-057](../requirements.md#element-057).

- **Chrome UI recipe.** Place an Escapement, a Cable winch with a Weight hanging on its cable (the stored drive) and a Conveyor; belt Winch `Drive` → Escapement `DriveIn`, Escapement `Drive` → Conveyor `DriveIn`; place two Switches each hit by a ball at different times, wired to `ActivationIn` (or a Clock once Story 9.3 exists). Verify placement and links.
- **Positive.** Each trigger advances the wheel exactly one tooth (45° at default) and the Weight drops by drum radius × 45°; the conveyor moves in matching discrete increments; `ActivationOut` fires once per step.
- **Negative/control.** Same construction with the Weight resting on the bench (no stored drive): triggers produce no step and no `ActivationOut`. No trigger: the loaded wheel stays locked for the whole run.
- **Boundaries.** Stored torque just below and above `holding_capacity`; tooth count 4 and 24; two triggers within one step (second not queued); out-of-range rejected.
- **Run/Reset and Save/Load.** Wheel angle, lock state and step count restore; parameters and links round-trip.
- **Integrations.** Cross-element task [sequence-task-370](../requirements.md#sequence-task-370) under [todo-375](../requirements.md#todo-375): "a ratchet/escapement meters motion", each named mechanism separately implemented and taught.

## 6. Open questions

1. Queue or drop triggers that arrive mid-step? Proposed: drop. Owner decision.
2. Over-capacity stored torque: slip a tooth, or hold rigidly? Owner decision.
3. Should a free-running (pendulum-regulated) escapement mode exist? It is not in the row. Owner decision.
4. The binding has no SignalPropagation, yet the escapement takes and emits activation. Confirm activation is an implied shared capability. Owner decision.
