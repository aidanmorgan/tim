# EL-059 · Weight tray declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-059 |
| Name | Weight tray |
| Type | Mechanical |
| Requirement anchor | [element-059](../requirements.md#element-059); scope index [todo-320](../requirements.md#todo-320) |
| Named entry | [named-elements.md#element-059](../invest/named-elements.md#element-059); proof owner S368 |
| CAT spec refined or extended | Extends the contact-load sensing of [CAT-052 pressure plate](CAT-052-pressure_plate.md) (Story 9.1) with a supported, deflecting tray and a measured output; it is a separate identity, not a pressure-plate mode |
| Related identities | [EL-058 size grate](EL-058-size-grate.md) (sorting by size; this sorts by mass), [EL-069 moving bucket](EL-069-moving-bucket.md) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes

- Static base: box 1.8 × 0.25 × 1.8 m. **Proposed**: close to the legacy pressure-plate footprint (navy base 2.2 × 0.2 × 2.2 m, `parts/PressurePlatePart.cs@a6c914e:L45-L46`) but smaller so the tray reads as a different part.
- Dynamic tray: box 1.6 × 0.08 × 1.6 m with a 0.1 m rim (four thin boxes), mass 0.5 kg. **Proposed**: the rim keeps a ball on the tray; mass is light so the reading is dominated by the load. Tray plus rim is a compound dynamic body (see Missing).

### Mass and material

Tray material: restitution 0.1, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 — the legacy pressure-plate contact material (`parts/PressurePlatePart.cs@a6c914e:L28-L28`, carried forward).

### Constraints and joints

- Tray slider along base +Y with travel [−0.1, 0] m and a soft spring row (stiffness 2000 N/m, damping ratio 1). **Proposed**: the full 16 kg threshold range (157 N) deflects 0.078 m and an 8 kg load (78 N) 0.039 m, both inside the travel, so the reading never saturates against the end stop below 16 kg; critical damping settles a dropped load in about 0.4 s.

### Typed ports

| Socket | Domain | Direction | Local position (m) | Meaning |
| --- | --- | --- | --- | --- |
| `ActivationOut` | Activation | Output | (0.95, 0, 0) | threshold reached |
| `MeasureOut` | Signal (scalar) | Output | (−0.95, 0, 0) | measured load in kg |

Identities: `ActivationOut` exists (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`); legacy had a `Signal` domain but no scalar socket (`engine/MachineData.cs@a6c914e:L68-L68`). `MeasureOut` and positions **proposed**.

### Sensors and activation

- Measured load = (spring force − tray weight) / g, from the committed spring row, so only bodies whose weight is transmitted through tray contact contribute. An object beside the tray contributes nothing.
- `ActivationOut` fires once when the measured load stays ≥ `threshold` for `dwell` (**proposed** dwell 0.25 s so an impact spike does not trigger, consistent with the legacy plate ignoring impacts, `parts/PressurePlatePart.cs@a6c914e:L8-L9`).

### Work and energy stores

The spring row stores elastic energy while loaded and returns it on unloading; no external work is created.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `threshold` | f32 | 0.1–16 | 0.5 | kg | Legacy pressure-plate minimum mass range and default (`parts/PressurePlatePart.cs@a6c914e:L38-L40`; `parts/catalog/pressure_plate.tres@a6c914e:L14-L14`) |
| `dwell` | f32 | 0.05–2 | 0.25 | s | **Proposed** (see above) |

Closed parameter enum `WeightTrayParameter { Threshold, Dwell }`.

### Cosmetic curves and UI bindings

- Tray pose is the committed physics pose (it really sinks); no decorative depression.
- A navy needle on a cream dial follows the committed measured load; corner studs slate when empty, ochre below threshold, gold at threshold (legacy plate indicator scheme, `DESIGN.md@a6c914e:L293-L293`).

### Art

Cream tray `#fff8e9`, navy base `#293954`, ochre dial ring `#d69c47`, gold studs `#f7cb52`, slate `#556573`. Catalogue colour **proposed**: the pressure-plate colour (0.91, 0.72, 0.39) from `parts/catalog/pressure_plate.tres@a6c914e:L13-L13`, so both load sensors share a family tone.

### Catalogue and inventory entry

Id `weight_tray`, title "Weighing tray", category "Control"; `WorkshopPartKind.WeightTray` appended last to the free inventory (`engine/gpu/WorkshopInventory.cs@a6c914e:L49-L60`). **Proposed**.

### Variants

The row names no variants. Its "measured force or threshold state" is specified as two simultaneous outputs of one element (`MeasureOut`, `ActivationOut`), each with its own acceptance; see Open question 1.

## 3. Engine capabilities

Binding: ContactImpulse, EnvironmentState, GeometryQuery, RigidBodyDynamics, SignalPropagation (map and coverage JSON agree). Map decision: S257 typed power versus signal.

**Exists now:** box bodies, contacts and friction (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L702-L766`); activation output and network (`engine/gpu/ActivationNetwork.cs@a6c914e:L7-L12`).

**Missing**

- Compound dynamic body (tray plate plus four rim boxes) with combined mass properties: `RigidMassProperties.Compile` admits one homogeneous sphere or box per dynamic body (`engine/gpu/RigidMassProperties.cs@a6c914e:L26-L31`); no story. Decision owner S368.
- Slider with soft spring row: Story 6.4 (CAT-062a prismatic slider and TGS Soft spring).
- Contact-load reading (direct-contact load): Story 9.1 (CAT-052). The tray instead reads its spring force, which is new.
- Scalar signal domain and `MeasureOut`: no story; S257 decision.
- Dwell-qualified threshold occurrence: Story 9.5 (hold timer) has the nearest timing primitive.

**Dependencies.** Loads of different mass: CAT-001 Basketball (1 kg), CAT-014 Bowling ball (4 kg) delivered; CAT-067 Weight (0.25–8 kg) Story 10.4. A consumer for `ActivationOut`: CAT-035 Lamp (delivered).

## 4. Sources and legacy

- Requirements row: "An object beside the tray cannot contribute weight." No variants.
- todo-320 integration: sorting by observable mass.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | The pressure plate sums masses directly resting on its top face — not impacts, not stacked load transmission, not a power supply. | `parts/PressurePlatePart.cs@a6c914e:L8-L9` | carry forward "not impacts" and "not a supply"; do not carry forward "not stacked load" for the tray | The tray measures real transmitted force, so a stacked load counts (Open question 2). |
| 2 | Load region: a box just above the top face, contact normal alignment ≥ 1 − 1e-6 with local +Y, threshold mass. | `parts/PressurePlatePart.cs@a6c914e:L21-L25`; `engine/SceneContactLoadSensorDeclaration.cs@a6c914e:L5-L6` | carry forward (geometry rule for the plate); the tray uses spring force instead | Region test is the plate's mechanism, recorded for comparison. |
| 3 | Threshold 0.1–16 kg, default 0.5 ("one tennis ball is too light"). | `parts/PressurePlatePart.cs@a6c914e:L38-L40`; `parts/catalog/pressure_plate.tres@a6c914e:L8-L14` | carry forward | Sourced threshold scale. |
| 4 | Indicator: slate empty, ochre underweight, gold loaded. | `parts/PressurePlatePart.cs@a6c914e:L59-L59`; `DESIGN.md@a6c914e:L293-L293` | carry forward | Visual language. |
| 5 | Phase enum {Empty, Underweight, Loaded}. | `parts/PressurePlatePart.cs@a6c914e:L20-L20` | carry forward | Closed set for the threshold state read. |

**Files harvested:** `parts/PressurePlatePart.cs`, `parts/catalog/pressure_plate.tres`, `engine/SceneContactLoadSensorDeclaration.cs`. No weight-tray legacy (searched for tray, scale, weigh).

## 5. Acceptance outline

Requirement row: [element-059](../requirements.md#element-059).

- **Chrome UI recipe.** Place a Weighing tray, a Lamp and an activation link tray `ActivationOut` → Lamp; set `threshold` 2 kg; place a Bowling ball above the tray and a Basketball above the bench beside it. Verify placement, threshold and link.
- **Positive.** Run: the Bowling ball lands, the tray sinks, the needle reads 4 kg after settling (about 0.4 s), the Lamp lights.
- **Negative/control.** The Basketball beside the tray: reading 0, Lamp off. A Basketball alone on the tray: reading 1 kg, Lamp off (below 2 kg). A bounce spike above 2 kg shorter than `dwell`: Lamp off.
- **Boundaries.** Loads at threshold ± 0.05 kg; 16 kg load reads 16 kg with the tray 0.078 m down, short of the 0.1 m stop; a ball resting partly on the rim; out-of-range threshold rejected.
- **Run/Reset and Save/Load.** Tray pose, reading and Lamp restore; parameters and the link round-trip.
- **Integrations.** Cross-element task [sequence-task-371](../requirements.md#sequence-task-371) under [todo-320](../requirements.md#todo-320): "Sorting depends on observable size, mass or material" (this element is the mass sorter's sensor).

## 6. Open questions

1. Are measured and threshold outputs both present, or a mode switch? Proposed: both present. Owner decision.
2. Does a stacked load (a ball on a box on the tray) count? Proposed: yes, real transmitted force. Owner decision.
3. Scalar signal domain: does a `MeasureOut` consumer exist in this programme (counter, comparator)? If not, defer `MeasureOut`. Owner decision.
4. Should the threshold output be continuous (clears when unloaded) rather than one-way latched? Current activation latches are one-way. Owner decision.
