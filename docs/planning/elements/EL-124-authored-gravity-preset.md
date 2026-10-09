# EL-124 · Authored gravity preset — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-124 · Authored gravity preset · Environment |
| Anchor | [requirements.md#element-124](../requirements.md#element-124); [named-elements entry](../invest/named-elements.md#element-124); umbrella [gap-18](../requirements.md#gap-18) (index only) |
| Related identities | [EL-125 Authored atmosphere preset](EL-125-authored-atmosphere-preset.md) (the other environment axis). Every dynamic element consumes it ([CAT-001](CAT-001-ball.md), [CAT-023](CAT-023-domino.md) and so on). No CAT refines it. |
| Roadmap story | Unscheduled. Campaign: introduction 17 "Down to Earth", practice 18, reuse 97, 139 ([gap-18](../requirements.md#gap-18)). |
| Status | Not started. Gravity is the constant 9.81 m/s² (`engine/gpu/WorkshopConstruction.cs@a6c914e:L123-L123`). |

## 2. Declaration

Variants: the gap-18 integration requires "two supported gravity environments" ([gap-18](../requirements.md#gap-18)); each preset is a separately specified value of one closed enum.

- **Bodies and shapes.** None. A construction-level (world) declaration, not a part.
- **Mass and material.** None.
- **Constraints.** None.
- **Typed ports.** None.
- **Sensors and activation.** None.
- **Work and energy stores.** None of its own; gravity sets every body's potential energy rate.
- **Preset `Standard`.** 9.81 m/s² along −Y — today's constant and the legacy default (`engine/MachineData.cs@a6c914e:L170-L170`).
- **Preset `Light`.** **Proposed** 4.905 m/s² (½ g) along −Y — fall times grow by √2, visibly slower yet inside the current contact tuning; the ramp-and-receiver lessons stay solvable with the same parts.
- **Direction.** **Proposed** always −Y — sideways gravity would turn the bench into a wall and break every authored level's support assumptions.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `gravity` | enum `GravityPreset { Standard, Light }` | closed set | `Standard` | — | Standard value sourced; Light **proposed** |
  | compiled magnitude | f32 | ≤ 16 | 9.81 | m/s² | admission bound on every body's gravity vector (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L68-L68`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L80-L81`) |

  Out-of-enum values are rejected at the save/level/UI boundary. Difficulty (puzzle precision, `engine/gpu/WorkshopPuzzle.cs@a6c914e:L27-L51`) is a separate field and never selects or changes gravity: "Save and replay preserve it independently of difficulty" ([element-124](../requirements.md#element-124)).
- **Fixed for a Run.** The preset is construction data compiled into the physics document at admission; it cannot change while running, mirroring the legacy idle-only setter (`reference/cpu/MachineWorld.cs@a6c914e:L185-L189`).
- **Cosmetic curves and UI bindings.** A quiet environment plaque shows a navy down-arrow sized by the preset; selection in level authoring only. Visible to the player in Run ("visible and fixed for a Run").
- **Art.** Cream plaque `#fff8e9`, navy direction symbol `#293954`, gold selection `#f7cb52`; no per-preset sky recolouring (`DESIGN.md@a6c914e:L145-L154`; [gap-18 visual style](../requirements.md#gap-18)).
- **Catalogue and inventory.** Not a palette part. **Proposed** field `WorkshopConstruction.Gravity` of type `GravityPreset`, replacing the static constant, carried by the construction wire and save codec.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): EnvironmentState, RigidBodyDynamics.

- **Exists now.** Each dynamic body already declares its own gravity vector (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`), the compiler fills it from the constant (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L36-L38`, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L47-L49`), and the worker integrates it per body each substep (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1008-L1016`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`). Only the authoring and persistence layers are missing.
- **Missing.**
  - Construction-level preset, wire and save fields; the save codec has one version, `CanonicalConstruction = 9`, with no environment (`engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L43`). Forward-only: bump the schema and reject older saves, no migration. Decision owner LAW-ENVIRONMENT-I ([S635 environment](../invest/decisions.md#s635)); owner S678.
  - The environment plaque UI.
- **Dependencies.** None beyond existing dynamic elements.

## 4. Sources and legacy

- **Requirements.** Row: "Validated world acceleration is visible and fixed for a Run"; outcome "Save and replay preserve it independently of difficulty" ([element-124](../requirements.md#element-124)). Integration: the same construction responds predictably to two gravity environments; save/export/replay preserve the world ([gap-18](../requirements.md#gap-18)).
- **Audit.** "Expose existing gravity ... with typed presets and faithful previews ... changing difficulty must not secretly swap the authored world" (`docs/physics-puzzle-gap-audit.md@a6c914e:L97-L97`).
- **Decisions.** [S635 environment](../invest/decisions.md#s635): a nondefault environment visibly changes the ball's fall; out-of-range values refused; Save/Load and Reset keep it.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Machine data carried `Gravity = 9.81` and `Pressure = 1` defaults, saved with the construction and restored on load. | `engine/MachineData.cs@a6c914e:L164-L172`; `reference/cpu/MachineWorld.cs@a6c914e:L355-L356`, `reference/cpu/MachineWorld.cs@a6c914e:L477-L477` | Carry forward persistence; replace the free float with the typed preset. |
| 2 | Gravity could change only while idle. | `reference/cpu/MachineWorld.cs@a6c914e:L185-L189` | Carry forward ("fixed for a Run"). |
| 3 | World gravity became a (0, −g, 0) world setting. | `reference/cpu/MachineWorld.cs@a6c914e:L494-L494` | Carry forward direction −Y. |
| 4 | Precision (difficulty) was a separate saved field from gravity. | `engine/MachineData.cs@a6c914e:L187-L196` | Carry forward the separation. |
| 5 | Isolated drops reach first impact at √(2h/g) ± 0.02 s at zero pressure, and replay identically after Restore. | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L14-L81` | Carry forward as the per-preset fall-time acceptance. |
| 6 | Most legacy tests set `Gravity = 0` to isolate mechanisms. | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L52-L52` | Do not carry forward: zero gravity is a test fixture, not a supported preset. |

Files consulted: `engine/MachineData.cs`, `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/PhysicsCalibrationTests.cs`, `CuriousContraptions.tests/BumperTests.cs`.

## 5. Acceptance outline

Point of truth: [element-124](../requirements.md#element-124) and the [gap-18 integration](../requirements.md#gap-18).

- **Chrome recipe.** Open an authored gravity lesson in each preset; place a Basketball at the same height through the palette and lift gizmo.
- **Positive.** Under `Light` the ball's first impact comes √2 later than under `Standard` (within the ± 0.02 s sampling of fact 5); the plaque shows the active preset during Run.
- **Negative or control.** Changing difficulty precision leaves the preset and fall time unchanged; an unknown preset value in a save is rejected and the previous construction stays.
- **Boundaries.** Preset cannot change during Run (control disabled); compiled magnitude stays ≤ 16 m/s².
- **Run/Reset.** Reset keeps the preset and restores bodies exactly.
- **Save/Load.** Preset survives save, reload and Load; replay of a saved Run is identical.
- **Integrations.** Environment integration task [sequence-task-551](../requirements.md#sequence-task-551) (gap-18: the same construction responds predictably to two supported gravity environments; save/export/replay preserve the chosen world). No IX row is specific to gravity; the observable effect is through IX-01 contact impulse ([interaction-01](../requirements.md#interaction-01)) on falling bodies. Campaign first use: GAP-18 row of the [campaign allocation](../requirements.md#campaign-gap-allocation) — gravity introduction 17 "Down to Earth", practice 18, reuse 97, 139.

## 6. Open questions

1. Which second preset: half gravity (proposed), lunar 1.62 m/s², or a heavy 1.5 g. Unspecified — owner decision.
2. Whether Free Workshop exposes the preset to the player or only authored levels set it.
3. Whether "export" means the GAP-12 portable file format, which is not yet specified.
