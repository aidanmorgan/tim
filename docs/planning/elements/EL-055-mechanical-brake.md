# EL-055 · Mechanical brake declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-055 |
| Name | Mechanical brake |
| Type | Mechanical |
| Requirement anchor | [element-055](../requirements.md#element-055); scope index [todo-375](../requirements.md#todo-375) |
| Named entry | [named-elements.md#element-055](../invest/named-elements.md#element-055); proof owner S341 |
| CAT spec refined or extended | none. Related: [CAT-018 clutch](CAT-018-clutch.md) (its requirement keeps "slip/brake variants separate"), [CAT-042 motor](CAT-042-motor.md), [CAT-039 linear pusher](CAT-039-linear_pusher.md) (self-locking hold, not a brake element) |
| Related identities | [EL-056 ratchet](EL-056-ratchet.md), [EL-063 cable winch](EL-063-cable-winch.md), [EL-062 docking ferry](EL-062-docking-ferry.md) (carrier to restrain) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes

- **Shaft mode.** Static housing box 0.7 × 0.7 × 0.4 m; dynamic pass-through shaft cylinder r 0.2 m, width 0.12 m, 0.25 kg (mass from the legacy winding shaft, `parts/WoundSpringPart.cs@a6c914e:L71-L71`). **Proposed** dimensions: the clutch-sized housing family.
- **Carrier mode.** Static clamp box 0.6 × 0.3 × 0.4 m with a 0.1 m slot through which the carrier's rail or slider body passes. **Proposed**.

### Mass and material

Brake lining friction is a declared torque or force capacity (below), not a contact material. Housing contact material: static default restitution 1, threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`).

### Constraints and joints

- **Shaft mode.** Shaft hinge to the housing; a brake row on that hinge targets zero relative angular velocity with |torque| ≤ capacity while engaged, and is absent while released. The row only removes relative motion, so it dissipates work and never supplies it.
- **Carrier mode.** A brake row on the restrained carrier's slider coordinate, |force| ≤ capacity while engaged.

### Typed ports

| Socket | Domain | Direction | Mode | Local position (m) |
| --- | --- | --- | --- | --- |
| `DriveIn` | Mechanical | Input | Shaft | (−0.35, 0, 0.25) |
| `Drive` | Mechanical | Output | Shaft | (0.35, 0, 0.25) |
| `ActivationIn` | Activation | Input | both | (0, 0.35, 0) |

Socket identities: `engine/MachineData.cs@a6c914e:L104-L108`; positions **proposed**. Carrier mode binds to a carrier part through a typed `Restrains` attachment chosen in the wiring drawer (new enum member, **proposed**).

### Sensors and activation

`ActivationIn` flips the brake once from its authored initial state (activation latches are one-way within a run, `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L12`). Committed brake state {Released, Engaged} is a read for cosmetics.

### Work and energy stores

None stored. Dissipated work is debited to the FiniteLedger loss account each tick (no heat store yet).

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `target` | enum `BrakeTarget { Shaft, Carrier }` | — | Shaft | — | Row wording "shaft or carrier"; enum **proposed** |
| `initial_state` | enum `BrakeState { Released, Engaged }` | — | Engaged | — | **Proposed**: the common puzzle is "hold the load until a trigger releases it" |
| `torque_capacity` (Shaft) | f32 | 1–100 | 30 | N·m | **Proposed**: range sits inside the legacy motor torque range (0, 100] N·m (`parts/MotorPart.cs@a6c914e:L55-L63`, via [CAT-042](CAT-042-motor.md)), with a 1 N·m floor so an engaged brake always restrains; default above the default 20 N·m motor so an engaged brake stalls it |
| `force_capacity` (Carrier) | f32 | 5–400 | 100 | N | **Proposed**: holds an 8 kg Weight (78 N) on a hoist |

### Cosmetic curves and UI bindings

Brake shoes close onto the drum (shaft mode) or the clamp jaws close (carrier mode) following the committed state; a slate/gold lamp mirrors it. Easing is presentation-only and never delays the physical state.

### Art

Cream housing `#fff8e9`, navy foot `#293954`, slate shoes `#556573` that turn gold `#f7cb52` when engaged, pale grey drum `#ccd9df`. Catalogue colour **proposed**: navy-on-cream with ochre accent `#d69c47` (drive-train family, `DESIGN.md@a6c914e:L176-L183`).

### Catalogue and inventory entry

Id `mechanical_brake`, title "Brake", category "Motion"; `WorkshopPartKind.MechanicalBrake` appended last to the free inventory (`engine/gpu/WorkshopInventory.cs@a6c914e:L49-L60`). **Proposed**.

### Variants

The row names no variants. Its wording "restrains a supplied shaft or carrier" is specified as two modes of one element, each with its own acceptance:

- **Shaft mode:** pass-through brake on a mechanical shaft.
- **Carrier mode:** clamp on a translating carrier (slider body), for example EL-062.

## 3. Engine capabilities

Binding: EnvironmentState, FiniteLedger, FiniteWorkActuation, JointConstraint, RigidBodyDynamics, ShaftTorque (map); coverage JSON adds StateTransaction. Map note: "broad FiniteWorkActuation membership does not make the brake a drive".

**Exists now:** static boxes and contact materials (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`); activation latches (`engine/gpu/ActivationNetwork.cs@a6c914e:L7-L12`).

**Missing**

- Hinge (Story 10.3) and slider (Story 6.4) rows.
- Cylinder collider and inertia (shaft drum): no owning story; owner decision 9 Oct 2026: a new cylinder-collider story comes before the first shaft wheel.
- Bounded-impulse friction row on a joint coordinate with enum engagement state: no story names it; nearest are Story 11.2 (clutch engagement) and Story 11.3 (self-locking hold). Decision owner S341.
- `Mechanical` domain and shaft pass-through: Story 11.1.
- Work-loss ledger readout: Story 11.1 (motor work totals).

**Dependencies.** A drive to restrain: CAT-042 Motor (11.1) or a falling load through EL-063 Winch. Carrier mode needs a slider carrier (EL-062 or CAT-039).

## 4. Sources and legacy

- Requirements row: "Released brake permits motion; restraint cannot generate shaft work." No variants.
- Current research row: "Clutch / Brake (EL-055) — engagement constraint between shafts, controller input; stops its load without deleting upstream energy" (`docs/component-research.md@a6c914e:L77-L77`).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | A velocity-boundary brake removes exactly the body's kinetic energy (2 J for 1 kg at 2 m/s), supplies zero work, and a later release supplies only paid work. | `CuriousContraptions.tests/ConstrainedPoweredImpulseTests.cs@a6c914e:L12-L34` | carry forward (acceptance arithmetic) | Matches "restraint cannot generate shaft work". |
| 2 | An empty-supply motor cannot start motion but can brake: effort −4, supplied 0, dissipated 4. | `CuriousContraptions.tests/MotorPredictionTests.cs@a6c914e:L136-L147` | carry forward (acceptance) | Braking is dissipative only. |
| 3 | Electrical supply loss is not a brake: an unloaded rotor coasts. | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L120-L124` | carry forward | Released brake must not damp the shaft either. |
| 4 | Transmission engagement is a closed enum {Open, Engaged}. | `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L7-L7` | carry forward (pattern) | Brake state is likewise an enum. |
| 5 | Braking computed by a CPU constrained-impulse predictor. | `CuriousContraptions.tests/ConstrainedPoweredImpulseTests.cs@a6c914e:L20-L20` | do not carry forward | CPU solver path. |

**Files harvested:** `CuriousContraptions.tests/ConstrainedPoweredImpulseTests.cs`, `CuriousContraptions.tests/MotorPredictionTests.cs`, `CuriousContraptions.tests/MechanicalTests.cs`, `engine/physics/PhysicsTransmissionJoint.cs`, `parts/WoundSpringPart.cs`. No brake part exists in `parts/` or `content/puzzles.json`.

## 5. Acceptance outline

Requirement row: [element-055](../requirements.md#element-055).

- **Chrome UI recipe (Shaft).** Place Battery, Motor, Brake and Conveyor; wire Battery → Motor; belt Motor `Drive` → Brake `DriveIn`, Brake `Drive` → Conveyor `DriveIn`; place a Switch and an activation link Switch → Brake `ActivationIn`; a ball rolls onto the Switch. Verify placement, modes and links.
- **Positive (Shaft).** Initial Engaged: the conveyor stays still and the motor stalls; when the ball presses the Switch the brake releases and the conveyor runs.
- **Negative/control.** Released brake: the shaft runs exactly as without a brake (no extra damping). Engaged brake on a coasting shaft: speed falls to zero and dissipated work equals the lost kinetic energy; no reverse rotation, no shaft work gained.
- **Carrier mode recipe.** Brake clamps a Docking ferry carriage pulled by a Weight over a Pulley: engaged holds; release lets the carriage move.
- **Boundaries.** Motor torque just below and just above `torque_capacity` (holds vs slips); load just below and above `force_capacity`; out-of-range capacities rejected; reversed drive direction.
- **Run/Reset and Save/Load.** Reset restores the authored brake state and shaft pose; target, initial state, capacities and links round-trip.
- **Integrations.** Cross-element task [sequence-task-370](../requirements.md#sequence-task-370) under [todo-375](../requirements.md#todo-375): "a clutch/brake chooses drive or restraint", each named mechanism separately implemented and taught.

## 6. Open questions

1. Is the brake commanded by activation (one-shot) or by continuous electrical supply (released while powered)? The binding has no ElectricalPower. Proposed: activation. Owner decision.
2. Should slipping above capacity be kinetic friction (constant torque) or stick-slip with separate static/kinetic capacities? Owner decision.
3. Carrier mode attachment: a typed connection to a carrier part, or contact clamping of any body inside the jaws? Owner decision.
4. Should dissipated work feed a thermal store (TH family) once heat exists? Owner decision.
