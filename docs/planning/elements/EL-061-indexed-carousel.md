# EL-061 · Indexed carousel declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-061 |
| Name | Indexed carousel |
| Type | Mechanical |
| Requirement anchor | [element-061](../requirements.md#element-061); scope index [todo-320](../requirements.md#todo-320) |
| Named entry | [named-elements.md#element-061](../invest/named-elements.md#element-061); proof owner S371 |
| CAT spec refined or extended | none. Related: [CAT-042 motor](CAT-042-motor.md) (finite-work drive), [CAT-018 clutch](CAT-018-clutch.md), [CAT-004 basket](CAT-004-basket.md) (open-pocket geometry) |
| Related identities | [EL-057 escapement](EL-057-escapement.md) (discrete steps), [EL-062 docking ferry](EL-062-docking-ferry.md) (carrier transfer), [EL-055 brake](EL-055-mechanical-brake.md) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes

- Static base: box 2.6 × 0.3 × 2.6 m. **Proposed**: carries a platter of the size below with a margin.
- Dynamic platter: disc radius 1.2 m, thickness 0.12 m, mass 3 kg, rotating about vertical +Y, with `positions` radial pocket walls (boxes 1.0 × 0.3 × 0.06 m) that divide it into open pockets. **Proposed**: each 90° pocket holds one Basketball (0.68 m) at default; mass makes the drive visibly work.
- Compound dynamic body (disc + walls) — see Missing.

### Mass and material

Platter material: restitution 0.2, bounce threshold 0.1 m/s, friction 0.5, rolling resistance 0. **Proposed**: cargo rides with the pocket instead of sliding off.

### Constraints and joints

- Platter hinge about base +Y.
- Drive coupling: platter angle = input shaft angle / `reduction` through a ratio row (legacy ratio law, `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L7-L13`).
- Index detent: a soft hinge spring toward the nearest index angle with stiffness `detent_stiffness` (N·m per radian of error), active only within ±10° (0.175 rad) of an index, so its peak restoring torque is `detent_stiffness` × 0.175 rad. **Proposed**: makes rest positions discrete without a timer.

### Typed ports

| Socket | Domain | Direction | Local position (m) |
| --- | --- | --- | --- |
| `DriveIn` | Mechanical | Input | (1.3, 0, 0) on the base |
| `ActivationOut` | Activation | Output (index completed) | (−1.3, 0, 0) |

Identities `engine/MachineData.cs@a6c914e:L104-L108`; positions **proposed**.

### Sensors and activation

- Index completed: the platter is within ±2° of the next index angle with |ω| < 0.05 rad/s (**proposed** settle threshold; `docs/gpu-f32-physics.md` names only 0.02 rad/s for island sleep). Emits one `ActivationOut` occurrence per completed index, never for a partial turn.
- Committed index number (0 … positions−1) is a read for cosmetics and goals.

### Work and energy stores

None stored except the detent spring's small elastic energy. All motion work comes from the drive; an obstructed pocket stalls the drive.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `positions` | int | 2–8 | 4 | pockets | **Proposed**: 90° steps are readable and fit catalogue balls |
| `reduction` | f32 | 1–16 | 4 | input turns per platter turn | **Proposed**: one motor revolution advances one index at 4 positions |
| `detent_stiffness` | f32 | 0.5–20 | 5 | N·m/rad | **Proposed**: peak detent torque 5 × 0.175 = 0.87 N·m at the platter, 0.22 N·m at the input after the 4:1 reduction, far below the default 20 N·m motor torque, so a default motor always passes the detent; the 20 N·m/rad maximum gives 3.5 N·m (0.87 N·m at the input) |

Closed parameter enum `CarouselParameter { Positions, Reduction, DetentStiffness }`.

### Cosmetic curves and UI bindings

Platter, pockets and cargo draw at committed poses; a gold index marker on the base lights when the committed index-completed state is true.

### Art

Cream platter `#fff8e9`, ochre pocket walls `#d69c47`, navy base `#293954`, gold index pointer `#f7cb52`, engraved numerals as shape cues. Catalogue colour **proposed**: ochre (`DESIGN.md@a6c914e:L176-L183`).

### Catalogue and inventory entry

Id `indexed_carousel`, title "Carousel", category "Motion"; `WorkshopPartKind.IndexedCarousel` appended last to the free inventory (`engine/gpu/WorkshopInventory.cs@a6c914e:L49-L60`). **Proposed**.

### Variants

The requirements row names no variants or modes. One element is specified.

## 3. Engine capabilities

Binding: EnvironmentState, FiniteLedger, FiniteWorkActuation, JointConstraint, RigidBodyDynamics, ShaftTorque (map); coverage JSON adds StateTransaction.

**Exists now:** static boxes and contacts (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L702-L766`); activation occurrences (`engine/gpu/ActivationNetwork.cs@a6c914e:L7-L12`).

**Missing**

- Compound dynamic body with combined mass properties: `RigidMassProperties.Compile` admits one homogeneous sphere or box per dynamic body (`engine/gpu/RigidMassProperties.cs@a6c914e:L26-L31`); no story. Decision owner S371.
- Cylinder (disc) collider and inertia: no owning story; owner decision 9 Oct 2026: a new cylinder-collider story comes before the first shaft wheel.
- Hinge: Story 10.3. Ratio row: Story 11.2. `Mechanical` domain and shaft drive: Story 11.1.
- Angular detent spring on a hinge: no story (soft rows arrive with Story 6.4 for sliders).
- Hinge-angle index sensor: no story (the orientation sensor, `engine/gpu/OrientationSensorDeclaration.cs@a6c914e:L24-L26`, is a one-shot threshold from the initial pose, not an indexed angle).

**Dependencies.** CAT-042 Motor and CAT-005 Battery (11.1, 8.1).

## 4. Sources and legacy

- Requirements row: "Obstruction or missing power prevents the completed index." No variants.
- todo-320 integration: carriers transfer cargo only when actually docked or indexed.

No legacy implementation. Shared facts:

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Ratio coupling between axial coordinates is bidirectional and phase-free; shared inertia sets reactions. | `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L7-L13` | carry forward | A blocked platter loads the drive. |
| 2 | Hinge travel bounds must stay strictly inside (−π, π). | `engine/physics/PhysicsJoint.cs@a6c914e:L137-L139` | do not carry forward as a limit on the platter | A carousel turns indefinitely; index angles must be computed on the unwrapped angle. |
| 3 | Motor source: target speed with bounded torque and work (default 6 rad/s, 20 N·m). | [CAT-042 spec](CAT-042-motor.md) legacy facts 5–6 | carry forward via CAT-042 | Drive scale for the parameters above. |

**Files harvested:** `engine/physics/PhysicsTransmissionJoint.cs`, `engine/physics/PhysicsJoint.cs`. Searched with no hit: `parts/`, `engine/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign`, `reference/` (terms: carousel, turntable, index, rotary table).

## 5. Acceptance outline

Requirement row: [element-061](../requirements.md#element-061).

- **Chrome UI recipe.** Place Battery, Motor, Carousel and a Lamp; wire Battery → Motor; belt Motor `Drive` → Carousel `DriveIn`; activation link Carousel `ActivationOut` → Lamp; drop a Basketball into one pocket. Verify placement and links.
- **Positive.** Run: the platter advances one 90° index per motor revolution carrying the ball; at the first completed index the Lamp lights.
- **Negative/control.** No supply: the platter stays and no `ActivationOut`. A Wall placed in the pocket path: the platter stops short, the motor stalls, no index completes and the Lamp stays off.
- **Boundaries.** `positions` 2 and 8; motor torque set just below and just above the input-side peak detent torque (0.22 N·m at default stiffness and reduction: the motor stalls in the detent vs passes); reversed drive; out-of-range rejected.
- **Run/Reset and Save/Load.** Platter angle, index count and cargo restore; parameters and links round-trip.
- **Integrations.** Cross-element task [sequence-task-371](../requirements.md#sequence-task-371) under [todo-320](../requirements.md#todo-320): every named carousel and carrier keeps its own obligation; cargo moves only by actual carriage.

## 6. Open questions

1. Continuous ratio plus detent (proposed) or a Geneva mechanism with dwell (input turns continuously, platter steps)? Owner decision.
2. Does `ActivationOut` fire on every index (needs repeated occurrences, Story 9.3 pattern) or only the first? Owner decision.
3. The binding omits ContactImpulse and GeometryQuery although pockets carry cargo by contact. Confirm implied. Owner decision.
