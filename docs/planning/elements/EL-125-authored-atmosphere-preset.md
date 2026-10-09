# EL-125 · Authored atmosphere preset — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-125 · Authored atmosphere preset · Environment |
| Anchor | [requirements.md#element-125](../requirements.md#element-125); [named-elements entry](../invest/named-elements.md#element-125); umbrella [gap-18](../requirements.md#gap-18) (index only) |
| Related identities | [EL-124 Authored gravity preset](EL-124-authored-gravity-preset.md); consumers [EL-208 Balloon](EL-208-balloon.md) / [CAT-003](CAT-003-balloon.md) (buoyancy), [CAT-028 Fan](CAT-028-fan.md) (airflow), every ball's drag ([CAT-001](CAT-001-ball.md)). No CAT refines it. |
| Roadmap story | Unscheduled. Campaign: introduction 79 "Air on the Side", practice 80, reuse 99, 143 ([gap-18](../requirements.md#gap-18)). |
| Status | Not started. The current world has no pressure or density (`engine/gpu/WorkshopConstruction.cs@a6c914e:L123-L124`). |

## 2. Declaration

Variants: "Unsupported atmosphere choices are unavailable and rejected at boundaries. Each later adopted pressure/air preset needs a distinct lesson before required use" ([gap-18](../requirements.md#gap-18)). Each preset below is specified separately.

- **Bodies and shapes.** None; a construction-level declaration.
- **Mass and material.** None of its own. It sets the boundary gas state every gas-coupled element reads.
- **Constraints.** None.
- **Typed ports.** None.
- **Sensors and activation.** None.
- **Work and energy stores.** None; the atmosphere is an unbounded boundary reservoir, not a finite store.
- **Preset `Standard`.** Relative pressure factor 1 — the legacy default (`engine/MachineData.cs@a6c914e:L171-L171`). **Proposed** absolute values for gas-family consumers: 101.325 kPa, 1.204 kg/m³ (dry air at 20 °C) — the standard sea-level reference the sealed-gas law (p = ρRT, [finite-gas foundation](../../finite-gas-foundation.md)) can be checked against.
- **Preset `Vacuum`.** Relative pressure factor 0 — legacy tests used `Pressure = 0` as a valid world (`CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L14-L81`). **Proposed** 0 kPa, 0 kg/m³: no drag, no buoyancy, no airflow force — the clearest physical contrast for a lesson (a balloon falls, a fan moves nothing).
- **Law (from legacy, carried forward).** Buoyant force = buoyancy × pressure factor × mass, upward; linear drag rate = declared drag × pressure factor (`reference/cpu/MachineWorld.cs@a6c914e:L877-L883`). A visual sky change alone changes nothing: only the typed preset feeds this law.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `atmosphere` | enum `AtmospherePreset { Standard, Vacuum }` | closed set | `Standard` | — | Standard sourced; Vacuum value sourced from legacy tests, adoption **proposed** |
  | pressure factor | f32 | {1, 0} | 1 | — | legacy relative pressure (`engine/MachineData.cs@a6c914e:L171-L171`) |

  Unknown presets and any free-form pressure value are rejected at the save/level/UI boundary ("Unsupported presets are rejected", [element-125](../requirements.md#element-125)).
- **Fixed for a Run.** Set only between Runs, as the legacy idle-only setter (`reference/cpu/MachineWorld.cs@a6c914e:L185-L189`).
- **Cosmetic curves and UI bindings.** Environment plaque with navy air symbol (three wavy lines for Standard, empty for Vacuum); no weather or sky recolouring ([gap-18 visual style](../requirements.md#gap-18)).
- **Art.** Cream plaque `#fff8e9`, navy symbol `#293954`, gold selection `#f7cb52`; sky stays `#91cbed` (`DESIGN.md@a6c914e:L145-L154`).
- **Catalogue and inventory.** Not a palette part. **Proposed** field `WorkshopConstruction.Atmosphere` of type `AtmospherePreset`, in the same schema bump as EL-124.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): AerodynamicDrag, Buoyancy, EnvironmentState, FiniteLedger, GasState, GeometryQuery, RigidBodyDynamics, StateTransaction.

- **Exists now.** Declared per-body linear drag, applied as exact exponential decay each substep (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L69`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`). `BallMaterial` carries a `Buoyancy` field that the compiler does not consume (`engine/gpu/WorkshopConstruction.cs@a6c914e:L41-L53`, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L36-L39`).
- **Missing.**
  - EnvironmentState pressure and density, scaling drag and buoyancy at compile: decision owner LAW-ENVIRONMENT-I ([S635 environment](../invest/decisions.md#s635)); owner S679.
  - Buoyancy consumption: Story 12.1 ([epics](../../../_bmad-output/planning-artifacts/epics.md)).
  - GasState boundary for sealed stores and airflow sources: S470 gas-state (S471) and open-versus-sealed (S697) ([decisions](../invest/decisions.md#s470)).
  - Drag above 0.125 1/s (the balloon declares 0.4) exceeds the admitted range (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L69-L69`).
- **Dependencies.** A gas-coupled consumer to make the preset observable: Balloon (Story 12.1) or Fan (Story 12.2).

## 4. Sources and legacy

- **Requirements.** Row: "Supported gas boundary parameters set actual pressure and density"; outcome "Unsupported presets are rejected; a visual sky change alone cannot affect physics" ([element-125](../requirements.md#element-125)). Integration as above ([gap-18](../requirements.md#gap-18)).
- **Audit.** "Expose existing gravity and only implemented atmosphere/other settings with typed presets" (`docs/physics-puzzle-gap-audit.md@a6c914e:L97-L97`).

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Default pressure 1; saved and loaded with the machine. | `engine/MachineData.cs@a6c914e:L171-L171`; `reference/cpu/MachineWorld.cs@a6c914e:L355-L356`, `reference/cpu/MachineWorld.cs@a6c914e:L477-L477` | Carry forward as the Standard preset. |
| 2 | Per dynamic root body each substep: upward force buoyancy × pressure × mass; drag load drag × pressure. | `reference/cpu/MachineWorld.cs@a6c914e:L877-L883` | Carry forward the law as declaration data; the per-substep CPU loop does not carry forward. |
| 3 | Isolated drops were calibrated at pressures 0, 1 and 2; at 0 the drop matched free fall. | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L14-L81` | Carry forward Vacuum; pressure 2 was a test value, not a lesson preset (see Open questions). |
| 4 | Opposite fans cancel and no pressure means no air force. | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L303-L319` | Carry forward as the Vacuum airflow control. |
| 5 | External buoyancy can balance gravity without duplicating gravity. | `CuriousContraptions.tests/PhysicsWrenchTests.cs@a6c914e:L33-L39` | Carry forward. |

Files consulted: `engine/MachineData.cs`, `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/PhysicsCalibrationTests.cs`, `CuriousContraptions.tests/WindChimeTests.cs`, `CuriousContraptions.tests/PhysicsWrenchTests.cs`.

## 5. Acceptance outline

Point of truth: [element-125](../requirements.md#element-125) and the [gap-18 integration](../requirements.md#gap-18).

- **Chrome recipe.** Authored atmosphere lesson with a Balloon and a Basketball placed through the palette at equal height.
- **Positive (Standard).** The balloon rises (CAT-003 fact: net 1.69 m/s² upward, [CAT-003 spec](CAT-003-balloon.md)); the Basketball falls with drag.
- **Positive (Vacuum).** Both fall at g; a supplied Fan moves nothing.
- **Negative or control.** Changing only sky art (no preset change) leaves every trajectory bit-identical; an unknown preset in a save is rejected and the construction is unchanged.
- **Boundaries.** Preset locked during Run; pressure factor admitted only as 1 or 0.
- **Run/Reset.** Reset keeps the preset.
- **Save/Load.** Preset survives save and Load; replay identical.
- **Integrations.** Environment integration task [sequence-task-551](../requirements.md#sequence-task-551) (gap-18: unsupported atmosphere choices unavailable and rejected; each adopted preset needs its own lesson); interaction rows IX-10 buoyancy ([interaction-10](../requirements.md#interaction-10)), IX-11 aerodynamic drag ([interaction-11](../requirements.md#interaction-11)) and IX-40 gas state evolution ([interaction-40](../requirements.md#interaction-40)). Campaign first use: GAP-18 row of the [campaign allocation](../requirements.md#campaign-gap-allocation) — atmosphere introduction 79 "Air on the Side" (after pressure/air lessons), practice 80, reuse 99, 143.

## 6. Open questions

1. Whether a third preset (thin air, factor 0.5, or the legacy test value 2) is adopted, each needing its own lesson. Unspecified — owner decision.
2. Whether drag scales with the preset (legacy law) once ball drag is a fixed material declaration.
3. Whether absolute pressure/density (proposed 101.325 kPa, 1.204 kg/m³) or only a relative factor is exposed to the gas family.
