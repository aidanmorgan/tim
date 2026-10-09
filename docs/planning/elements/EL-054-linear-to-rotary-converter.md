# EL-054 · Linear-to-rotary converter declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-054 |
| Name | Linear-to-rotary converter |
| Type | Mechanical |
| Requirement anchor | [element-054](../requirements.md#element-054); scope index [todo-211](../requirements.md#todo-211) |
| Named entry | [named-elements.md#element-054](../invest/named-elements.md#element-054); proof owner S340 |
| CAT spec refined or extended | none. Related: [CAT-019 conveyor](CAT-019-conveyor.md) and [CAT-057 reverse transmission](CAT-057-reverse_transmission.md) (shaft consumers), [CAT-067 weight](CAT-067-weight.md) (falling input load), [CAT-053 pulley](CAT-053-pulley.md) (rope pull input) |
| Related identities | [EL-053](EL-053-rotary-to-linear-converter.md) (inverse conversion); EL-158 rack-and-pinion slide (Batch J) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes

- Static frame: box 0.8 × 1.7 × 0.5 m. **Proposed**: holds the 0.9 m rack plus its full 0.6 m stroke (1.5 m) with 0.2 m left for the pad and the pinion bearing, so the rack never leaves the frame at either end stop.
- Dynamic input rack (plunger): box 0.3 × 0.9 × 0.3 m, mass 0.5 kg, with a cream top pad on which loads land. **Proposed**: light enough that a 1 kg Basketball or a Weight visibly drives it.
- Dynamic output pinion shaft: cylinder r 0.1 m, width 0.12 m, mass 0.25 kg. **Proposed**: the pinion radius is the ratio parameter default; mass matches the legacy winding shaft (`parts/WoundSpringPart.cs@a6c914e:L71-L71`).

### Mass and material

Pad material: restitution 0.1, bounce threshold 0.1 m/s, friction 0.5, rolling resistance 0. **Proposed**: a dead pad that converts a landing load into stroke instead of a rebound. Declared with `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).

### Constraints and joints

- Rack: slider along frame −Y with travel [0, stroke] end stops.
- Pinion: hinge to the frame.
- Coupling: pinion angular speed = rack speed / pinion radius, bidirectional (legacy law `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L7-L13`). A stationary rack gives a stationary pinion: no rotation energy without input motion.

### Typed ports

| Socket | Domain | Direction | Local position (m) | Source |
| --- | --- | --- | --- | --- |
| `Drive` | Mechanical | Output | (0.4, 0.3, 0.3) | Identity `engine/MachineData.cs@a6c914e:L104-L108`; position **proposed** |
| `Tie` | Rope | Bidirectional | rack top (0, 0.45, 0) on the rack body | Identity as legacy weight tie (`parts/WeightPart.cs@a6c914e:L16-L21`); **proposed** second input path (rope pull) |

### Sensors and activation

None required.

### Work and energy stores

None. Work arrives only through the rack (contact or rope tension) and leaves through the shaft.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `pinion_radius` | f32 | 0.025–0.2 | 0.1 | m/rad | Range mirrors the legacy shaft-to-slider lead (`parts/WoundSpringPart.cs@a6c914e:L118-L124`); default **proposed**: 0.6 m stroke gives ~1 revolution |
| `stroke` | f32 | 0.3–0.6 | 0.6 | m | **Proposed**: the 1.7 m frame fits the 0.9 m rack plus at most 0.6 m of stroke |

Closed parameter enum `LinearRotaryParameter { PinionRadius, Stroke }`.

### Cosmetic curves and UI bindings

Rack teeth and pinion index marks follow committed rack travel and shaft angle; nothing eases independently.

### Art

Cream frame `#fff8e9`, navy foot `#293954`, pale grey metal rack `#ccd9df` (`parts/SpringPart.cs@a6c914e:L94-L94`), ochre pinion `#d69c47` with gold index mark `#f7cb52`. Catalogue colour **proposed**: ochre `#d69c47` (drive-train family, `DESIGN.md@a6c914e:L176-L183`).

### Catalogue and inventory entry

Id `linear_rotary_converter`, title "Rack drive", category "Motion"; `WorkshopPartKind.LinearRotaryConverter` appended last to the free inventory (`engine/gpu/WorkshopInventory.cs@a6c914e:L49-L60`). **Proposed**.

### Variants

The requirements row names no variants or modes. One element is specified.

## 3. Engine capabilities

Binding: EnvironmentState, FiniteLedger, FiniteWorkActuation, JointConstraint, RigidBodyDynamics, ShaftTorque (map); the coverage JSON adds StateTransaction.

**Exists now:** dynamic and static boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); TGS Soft contacts with friction (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L208-L236`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L702-L766`); gravity and linear drag (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1278`).

**Missing**

- Slider with end stops: Story 6.4. Hinge: Story 10.3.
- Cylinder collider and inertia (pinion): no owning story; owner decision 9 Oct 2026: a new cylinder-collider story comes before the first shaft wheel.
- `Mechanical` domain, `Drive` socket, shaft coupling to consumers: Story 11.1.
- Ratio row between slider and hinge coordinates: Story 11.2 (ratio row) and Story 11.4 (first hinge-to-slider use).
- Rope `Tie` input: Story 10.2.

**Dependencies.** A downstream shaft consumer to show rotation (CAT-019 Conveyor or CAT-057 Reverse transmission, Story 11.1/11.2); an input load (CAT-001/014 balls delivered; CAT-067 Weight, Story 10.4).

## 4. Sources and legacy

- Requirements row: "An unmoving input supplies no rotation energy." No variants.
- Element map: Source D owner. No decisions.md row applies.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Bidirectional phase-free axial coupling; speeds and reactions set by shared inertia; no copied work allowance. | `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L7-L13` | carry forward (law) | Gives energy-honest linear-to-rotary conversion. |
| 2 | Transmission endpoints must be axial (hinge or slider) and distinct; ratio finite and nonzero. | `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L22-L32` | carry forward | Admission rules. |
| 3 | Mechanical output socket `Drive`; one belt per input. | `engine/MachineData.cs@a6c914e:L104-L108`; `engine/MechanicalNetwork.cs@a6c914e:L35-L35` | carry forward | Typed port. |
| 4 | Linear input braking dissipates exactly the removed kinetic energy and supplies no work (2 J at 2 m/s, 1 kg). | `CuriousContraptions.tests/ConstrainedPoweredImpulseTests.cs@a6c914e:L12-L34` | carry forward (as acceptance arithmetic) | Bounds energy at the end stop. |
| 5 | CPU impulse solver implementation. | `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L38-L40` | do not carry forward | CPU solver path. |

**Files harvested:** `engine/physics/PhysicsTransmissionJoint.cs`, `engine/MachineData.cs`, `engine/MechanicalNetwork.cs`, `parts/WoundSpringPart.cs`, `parts/WeightPart.cs`, `parts/SpringPart.cs`, `CuriousContraptions.tests/ConstrainedPoweredImpulseTests.cs`. No element-specific legacy (searched `parts/`, `engine/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign`, `reference/` for rack, pinion, plunger, converter).

## 5. Acceptance outline

Requirement row: [element-054](../requirements.md#element-054).

- **Chrome UI recipe.** In free play place the converter, a Reverse transmission (or Conveyor) and a Bowling ball above the pad; belt converter `Drive` → consumer `DriveIn`. Verify placement and typed connection from the committed read.
- **Positive.** Run: the ball lands on the pad, the rack descends, the pinion and the downstream consumer turn by stroke / pinion radius; shaft kinetic energy never exceeds the ball's lost potential plus kinetic energy.
- **Negative/control.** No load (or a load resting beside the pad): rack, pinion and consumer stay still for the whole run. A rack held by a wall below the pad: no rotation.
- **Boundaries.** End stop at full stroke stops the pinion; reversed-sign consumer; pinion radius 0.025 and 0.2; out-of-range values rejected atomically; rope pull through `Tie` upward drives the opposite sign.
- **Run/Reset.** Rack travel and pinion angle restore exactly.
- **Save/Load.** Parameters and the `Drive`/`Tie` links round-trip.
- **Integrations.** The scope index [todo-211](../requirements.md#todo-211) defines no separate integration task; shared interactions use [IX-05 shaft torque transmission](../requirements.md#interaction-05), [IX-01 contact impulse](../requirements.md#interaction-01) (load on the pad) and [IX-04 tension transmission](../requirements.md#interaction-04) (rope input).

## 6. Open questions

1. Should the rack return to its top (spring return, which needs an elastic store and paid work) or stay where it stops? Proposed: stays. Owner decision.
2. Is the rope `Tie` input wanted, or is contact the only input? Owner decision.
3. Orientation: vertical rack only, or free rotation of the whole part? Proposed: free rotation, gravity acts on the rack in any pose. Owner decision.
