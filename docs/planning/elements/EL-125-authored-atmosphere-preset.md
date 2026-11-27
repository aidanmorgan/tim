# EL-125 · Authored atmosphere preset — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-125 delivery slice](../invest/named-elements.md#element-125). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code is cited as `path:line` in that checkout; `path@a6c914e` citations appear only in section 4 and describe the harvest baseline. Values marked **proposed** are starting values ([decision D1](../invest/delivery-batches.md#3-family-level-decisions)). Nothing here was built or run.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-125 · Authored atmosphere preset · Environment |
| Anchor | [requirements.md#element-125](../requirements.md#element-125) (task `sequence-task-678`); [named-elements entry](../invest/named-elements.md#element-125); umbrella [gap-18](../requirements.md#gap-18) (index only) |
| Owner key | S679 |
| Related identities | [EL-124 Authored gravity preset](EL-124-authored-gravity-preset.md). Consumers, all delivered: Balloon ([EL-208](EL-208-balloon.md), [CAT-003](CAT-003-balloon.md)), Fan ([CAT-028](CAT-028-fan.md)), Windmill, and the ambient drag of every ball. No CAT refines it. |
| Engine story | The delivered part needs none. The residual, and the conversion of the enum to a declared row, are owned by ENGINE-FIELD-FORCE's scene-region slice, the unbounded region of the one uniform-field law under ENGINE-FIELD-FORCE, owner of gravity (coordinator decision 16), which carries the scene environment record (shared with EL-124) |
| Status | **Partly delivered; the residual is blocked on the engine programme (owner decision 3a), and so is the closure check of the delivered rows, which runs after it against `ENGINE-SPATIAL-QUERIES`'s airflow candidates (owner decision 22).** The construction carries a typed atmosphere with two rows, compiled into gas stores, ambient loads and airflow, saved, validated and editable in Build (section 3). Not delivered: an atmosphere field in authored level content, the declared-row form, a plaque visible in Run, and the lesson (campaign level 79). The ledger entry still says Backlog. |

### Disposition

**Scene data, not a part; delivered in its two-row form with a residual.** No package, directory, palette entry or catalogue key. The delivered behaviour is closed by a recorded check that cites the executed tests in section 3 (a closure row in the ledger, no new code); that check runs after the engine programme (owner decision 22, 23 November 2026). The residual is blocked on the engine programme (owner decision 3a): one engine slice shared with EL-124, then the level-79 slice. The identity is not complete until both have run. The slices that introduced the setting are recorded in `spec-12-1a-tennis-ball.md` and `spec-12-1b-balloon.md`; the introducing commit was not traced here.

## 2. Declaration

### Declaration as delivered

| Field | Type | Values | Default | Editor policy |
| --- | --- | --- | --- | --- |
| `WorkshopConstruction.Atmosphere` (`engine/gpu/WorkshopConstruction.cs:8-9`) | enum `AtmospherePreset` (`CuriousContraptions.Abstractions/AtmosphereBoundary.cs:6`) | `Standard`, `Vacuum` | `Standard` | Free Workshop picker, Build only, undoable (`ui/WorkshopAtmosphere.cs:38-60`); locked in Run and Pause |

| Row | Boundary pressure | Relative factor | Source |
| --- | --- | --- | --- |
| Standard | 101 325 Pa | 1 | `AtmosphereBoundary.cs:12` |
| Vacuum | 0 Pa | 0 | `AtmosphereBoundary.cs:13` |

- **What the boundary sets.** The absolute pressure is the outside pressure of every sealed gas store (`engine/gpu/WorkshopPhysicsCompiler.Gas.cs:21`, `:31-32`). The relative factor multiplies every declared ambient-load acceleration and ambient linear drag (`:37-38`) and every airflow source (`engine/gpu/WorkshopPhysicsCompiler.Airflow.cs:32`, `:47`). All of this happens at compile; the value is fixed for a Run.
- **Density.** The requirement says "pressure and density". The relative factor is the density ratio to Standard air: buoyancy, drag and airflow force are all proportional to air density, and each is scaled by it. No absolute density in kg/m³ is declared, because no delivered law consumes one. Owner decision 24 (23 November 2026) rewords the requirement's density to a ratio, naming the game's liquid as the reference; the edit to `requirements.md` is the coordinator's. On that scale the Standard atmosphere's density is ρ_air ÷ ρ_liquid = 1.2259 ÷ 16 = 0.0766 and Vacuum's is 0: ρ_air = 101 325 ÷ (287 × 288) = 1.2259 kg/m³ from the thermal conventions (ambient 101 325 Pa and 288 K in T1, air R 287 J/(kg·K) in T11 of [TH-04 section 11](TH-04-electrical-heating-plate.md#thermal-conventions); the value TH-25's envelope-lift binding uses, coordinator decision 28), and the liquid's 16 kg/m³ is D2 ([EL-001](EL-001-finite-reservoir.md) L2). The declared row's relative factor (1 for Standard, 0 for Vacuum, a ratio to Standard air) is unchanged; the liquid-relative density is that factor × 0.0766, a derived figure for the row's wording, not a new declared value, so the delivered values and tests do not change. No delivered law consumes an absolute density in kg/m³; TH-25's envelope-lift binding (lift = envelope volume × ambient air density × g × (1 − T_ambient ÷ T_envelope), owned by ENGINE-THERMAL, coordinator decision 28) is the first that will. There is no displaced-density law for air.
- **Not a finite store.** The atmosphere is a boundary, not an inventory (`AtmosphereBoundary.cs:5`); it has no ledger.
- **Unsupported choices.** The picker offers only the two rows; an undefined value is refused by the boundary lookup, the construction validation and the save pre-validation (`AtmosphereBoundary.cs:14`; `WorkshopConstruction.cs:21`; `engine/gpu/WorkshopSavePrevalidation.cs:46-49`).
- **Form.** As delivered, a closed enum switched in the abstractions: scene data, not element logic. The general form is a declared row of values (boundary pressure in Pa from 0 to 101 325 and relative factor from 0 to 1, each validated; **proposed** ranges, since nothing delivered needs a denser row), with the two named rows as content, as [EL-124](EL-124-authored-gravity-preset.md) uses for gravity. The enum and its switch are converted to that form in ENGINE-FIELD-FORCE's scene-region slice, the slice that adds gravity to the same environment record, so the record never ships in two forms (decided now; section 6). No third row is adopted, and the picker still offers only Standard and Vacuum.
- **Bodies, colliders, joints, ports, stores, package parameters:** none.

### Declaration still to write

- **Authored level content.** An `atmosphere` field in the level definition, read and validated with the level and compiled exactly as the construction field is. Today no level can declare one (no such field in `engine/gpu/PuzzleDefinition.cs` or `content/puzzles.json`; searched).
- **Plaque and projection.** The active row on the typed observation port and on a HUD plaque visible in Build and Run.
- **Declared row.** `WorkshopConstruction` carries the row's two validated values in place of the enum; the save schema, pre-validation, picker and every compile site read the values; `AtmospherePreset` and `AtmosphereBoundary.For` are deleted in the same change (forward-only). An out-of-range or non-finite value, or a save in the enum layout, is refused with a typed reason.

### Art and solidity

No body, collider or art node. The plaque is cream with a navy air symbol (three wavy lines for Standard, an empty ring for Vacuum) and gold selection; the sky stays `#91cbed` and is not recoloured per row (cream `#fff8e9`, navy `#293954`, gold `#f7cb52`; [gap-18 visual style](../requirements.md#gap-18)). The symbol differs from gravity's arrow by shape. Nothing in physics reads art, so a sky change alone cannot affect a Run.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol | Exercising test (body opened) |
| --- | --- | --- | --- | --- |
| 1 | The boundary pressure sets a sealed store's envelope; buoyant rise in Standard, fall in Vacuum, with the same sealed mass | present | `WorkshopPhysicsCompiler.Gas.cs:21-38` | `BalloonTests.StandardRisesAgainstHeavierControlAndVacuumFallsWithTheSameSealedInventory` |
| 2 | Ambient drag acts only in Standard | present | `WorkshopPhysicsCompiler.Gas.cs:37-38` | `WorkshopPhysicsEngineTests.FreeFlightDragChangesOneSecondSpeedWithoutDampingSpin`; `BalloonTests.ExistingBallsHaveAmbientDragOnlyInStandardAndRestoreTheirEnvironment` |
| 3 | Airflow transfers nothing in Vacuum | present | `WorkshopPhysicsCompiler.Airflow.cs:32`, `:47` | `FanIntegrationTests.ActualHubDoesNotOccludeThePaidConeButOffAndVacuumDoNotTransfer` |
| 4 | An undefined row is refused; a save with a missing or unknown atmosphere is refused, not defaulted | present | `AtmosphereBoundary.cs:14`; `WorkshopSavePrevalidation.cs:46-49` | `GasCapabilityTests.InvalidMaterialStateAndEnvironmentAreRefused`; `BalloonTests.MissingOrUnknownSerializedAtmosphereIsRefusedRatherThanDefaulted` |
| 5 | Edited only in Build, undoable, refused edits leave the installed state | present | `ui/WorkshopAtmosphere.cs:38-60` | `PalettePlacementTests.AtmospherePickerEditsOnlyBuildStateAndUndoRestoresTheExplicitPreset` |
| 6 | A change of atmosphere that would make a store overlap a solid is refused atomically | present | construction validation at edit | `BalloonTests.VacuumExpansionRefusesObstructionWithoutChangingInstalledConstruction` |
| 7 | Save and load keep the row | present | `engine/gpu/WorkshopAuthoringFlatBuffers.cs:211`, `:268` | `BalloonTests.AllControlsAndExplicitAtmosphereSurviveSaveAndReset` (asserts the loaded row at `:78` and calls Reset at `:88`) |
| 8 | Atmosphere declared by authored level content | missing; owner ENGINE-FIELD-FORCE (scene region) | none | none |
| 9 | Active row on the observation port and a plaque visible in Run | missing; owner ENGINE-FIELD-FORCE (scene region) | no atmosphere in `engine/gpu/WorkshopObservationProjection.cs` (searched) | none |
| 10 | Sound does not travel in Vacuum | missing, later partner; owner Story 14.0 (ENGINE-ACOUSTIC), which decides the medium rule in its shared model | no acoustic engine | none |
| 11 | The atmosphere as a declared row of validated values rather than an engine enum | narrower: a two-value enum switched in the abstractions; owner ENGINE-FIELD-FORCE (scene region) | `AtmosphereBoundary.cs:6-15` | none |

Counts: present 7; present-untested 0; narrower 1; missing 3 (one a later-partner criterion). Rows 8, 9 and 11 share one owner with EL-124.

Stale facts corrected from the earlier text: the setting, its save field, its picker and its effect on drag, buoyancy and airflow all exist; the old "no pressure or density" status, the unconsumed `Buoyancy` field and the 0.125 1/s drag objection described the harvest baseline.

## 4. Sources and legacy

- **Requirements.** Row: "Supported gas boundary parameters set actual pressure and density"; outcome "Unsupported presets are rejected; a visual sky change alone cannot affect physics" ([element-125](../requirements.md#element-125)). Integration: "Unsupported atmosphere choices are unavailable and rejected at boundaries. Each later adopted pressure/air preset needs a distinct lesson before required use" ([gap-18](../requirements.md#gap-18)).
- **Audit.** "Expose existing gravity and only implemented atmosphere/other settings with typed presets" (`docs/physics-puzzle-gap-audit.md@a6c914e:L97-L97`).

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Default pressure 1; saved and loaded with the machine. | `engine/MachineData.cs@a6c914e:L171-L171`; `reference/cpu/MachineWorld.cs@a6c914e:L355-L356`, `reference/cpu/MachineWorld.cs@a6c914e:L477-L477` | Carried forward as the Standard row. |
| 2 | Per dynamic root body each substep: upward force buoyancy × pressure × mass; drag load drag × pressure. | `reference/cpu/MachineWorld.cs@a6c914e:L877-L883` | The law is carried forward as declared ambient loads scaled at compile; the per-substep CPU loop is not. |
| 3 | Isolated drops were calibrated at pressures 0, 1 and 2; at 0 the drop matched free fall. | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L14-L81` | Vacuum carried forward; pressure 2 was a test value, not a lesson row. |
| 4 | Opposite fans cancel and no pressure means no air force. | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L303-L319` | Carried forward (probe row 3). |
| 5 | External buoyancy can balance gravity without duplicating gravity. | `CuriousContraptions.tests/PhysicsWrenchTests.cs@a6c914e:L33-L39` | Carried forward. |

Files consulted: `engine/MachineData.cs`, `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/PhysicsCalibrationTests.cs`, `CuriousContraptions.tests/WindChimeTests.cs`, `CuriousContraptions.tests/PhysicsWrenchTests.cs`.

## 5. Acceptance outline

Point of truth: [element-125](../requirements.md#element-125) and the [gap-18 integration](../requirements.md#gap-18).

### Behaviour and numbers

- **Laws.** Ambient loads (buoyant acceleration and linear drag) and conical airflow, each multiplied by the row's relative factor at compile; the isobaric gas law with the row's boundary pressure. Airflow is evaluated in Phase 2 and ambient loads in the mechanical substeps of Phase 3 ([solver model](../../gpu-f32-physics.md#solver-model)). The row is fixed for a Run.
- **Drag.** A ball launched at 2.0 m/s with ambient drag 0.04 1/s moves at 2.0 × e^(−0.04 × 1) = 1.922 m/s after 1 s in Standard and 2.0 m/s in Vacuum. The delivered test's ±0.05 m/s band separates them (0.078 m/s apart) and also excludes doubled drag (1.846 m/s).
- **Buoyancy.** A default Balloon (`buoyancy` 11.5 m/s², `elements/Balloon/BalloonElementDeclaration.cs:24`): net 11.5 − 9.81 = 1.69 m/s² upward in Standard; in Vacuum the ambient acceleration is 11.5 × 0 = 0 and it falls at 9.81 m/s². With `drag` 0.4 1/s its terminal rise is 1.69 / 0.4 = 4.2 m/s, the centre of the delivered test's 4.075 to 4.375 m/s band.
- **Airflow.** Relative factor 0 gives zero flow speed, zero work and no debit from the source's store, which the delivered Fan test asserts.

### Acceptance rows

Delivered rows cite executed tests; planned rows belong to ENGINE-FIELD-FORCE's scene-region slice (class `SceneAtmosphereTests`) and the level-79 slice. Chrome proof is pending the post-catalogue browser phase.

| Row | Given / When / Then | Test | Control | Boundary | State |
| --- | --- | --- | --- | --- | --- |
| A1 | Given a Balloon and a Bowling ball, then in Standard the Balloon rises and the Bowling ball falls, and in Vacuum both fall with the same sealed gas mass | `BalloonTests.StandardRisesAgainstHeavierControlAndVacuumFallsWithTheSameSealedInventory` | the Bowling ball | n/a | delivered |
| A2 | Given a ball in free flight, then drag slows it in Standard only | `WorkshopPhysicsEngineTests.FreeFlightDragChangesOneSecondSpeedWithoutDampingSpin` | Vacuum | three cadences | delivered |
| A3 | Given a powered Fan and a ball in its cone, then the ball gains speed in Standard and none in Vacuum, and the store is not debited | `FanIntegrationTests.ActualHubDoesNotOccludeThePaidConeButOffAndVacuumDoNotTransfer` | Fan off | n/a | delivered |
| A4 | Given an undefined row in the boundary lookup or a save, or a save with no atmosphere, then it is refused with a typed reason | `GasCapabilityTests.InvalidMaterialStateAndEnvironmentAreRefused`; `BalloonTests.MissingOrUnknownSerializedAtmosphereIsRefusedRatherThanDefaulted` | a valid save loads | n/a | delivered |
| A5 | Given Run or Pause, then the picker is disabled and sends no edit; in Build the edit applies and Undo restores the previous row | `PalettePlacementTests.AtmospherePickerEditsOnlyBuildStateAndUndoRestoresTheExplicitPreset` | Build edit | n/a | delivered |
| A6 | Given a Vacuum construction, then save and load keep the row and Reset keeps it | `BalloonTests.AllControlsAndExplicitAtmosphereSurviveSaveAndReset` | n/a | n/a | delivered |
| A7 | Given level content naming a row, then the level's construction compiles with it; an unknown or missing row is refused naming the puzzle and field and the installed catalogue is unchanged | `SceneAtmosphereTests.ALevelDeclaresItsAtmosphereAndAnUnknownRowIsRefused` | a level naming Standard | n/a | planned |
| A8 | Given a running construction, then the observation port and the plaque report the active row | `SceneAtmosphereTests.TheActiveRowIsObservableInBuildAndRun` | the other row | n/a | planned |
| A9 | Given authored level 79, its solution solves in its authored atmosphere and the same construction in the other row does not | `AtmosphereLessonTests.AuthoredLessonSolvesOnlyInItsAuthoredAir` | the other row | n/a | planned; the whole declared Run (30 s default) |
| A10 | Given a sound source and listener in Vacuum, then nothing is heard | owned by Story 14.0 | Standard | n/a | later partner |
| A11 | Given a construction whose atmosphere row holds an out-of-range or non-finite value, or a save in the enum layout, then it is refused with a typed reason; given the two named rows as values, then rows A1 to A6 pass unchanged in meaning | `SceneAtmosphereTests.TheAtmosphereIsADeclaredRowAndTheDeliveredBehaviourIsUnchanged` | the Standard values | 0 and 101 325 Pa, 0 and 1 admitted | planned |

"A visual sky change alone cannot affect physics" has no row yet because no sky selector exists; the physics compiler takes no presentation input. The slice that first adds a selectable sky adds the row (same construction, two skies, equal compiled declarations). No wrong-kind connection applies (no port); A4 and A7 are the refusal rows.

### Integration

- **Collaborators.** Balloon, Fan, Windmill, balls (delivered); Bellows (in flight, Story 12.4); acoustic parts after Story 14.0.
- **Campaign.** Introduction 79 "Air on the Side" (after the pressure and air lessons), practice 80, reuse 99 and 143 ([GAP-18 allocation](../requirements.md#campaign-gap-allocation)). Cross-element task [sequence-task-551](../requirements.md#sequence-task-551).
- **Lesson fixtures.** None beyond locked placements.

### Delivery

- **Shape.** (1) A closure check: confirm rows A1 to A6 by reading and running the cited tests and cite them from the ledger entry; no code. It runs after the engine programme (owner decision 22, 23 November 2026), against the airflow candidates of `ENGINE-SPATIAL-QUERIES` as delivered. (2) Rows A7, A8 and A11 inside ENGINE-FIELD-FORCE's scene-region slice with EL-124, blocked on the engine programme (owner decision 3a). (3) A level slice for 79, after (2).
- **Prerequisites.** For (1), blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (airflow candidates, row A3), `ENGINE-SENSOR-VOLUMES` and `ENGINE-ISLANDS-SLEEP`. For (2), blocked on the engine programme (owner decision 3a), then S5 (the declared row changes the save schema). For (3), (2) and the Epic 12 parts the lesson uses.
- **Files.** (1) the ledger entry. (2) `CuriousContraptions.Abstractions/AtmosphereBoundary.cs`, `engine/gpu/WorkshopConstruction.cs`, the gas and airflow compile steps, the authoring declarations of `schemas/workshop_wire.fbs` (marked `(authoring)`) with their codec and pre-validation, `ui/WorkshopAtmosphere.cs`, `engine/gpu/PuzzleDefinition.cs`, `engine/gpu/PuzzleCatalogue.cs`, the observation projection and its TypeScript read model, the Workshop HUD. (3) `content/puzzles.json`.
- **Review tier.** (1) data-only; (2) engine; (3) data-only.
- **Disqualifies the plan.** A delivered consumer ignores the relative factor; a delivered test depends on the enum's type in a way the declared row cannot keep in meaning.

## 6. Open questions

**Resolved.**

1. *A third row (thin air, or the legacy test value 2)?* None is adopted (D19): gap-18 requires a distinct lesson per adopted row and the campaign reserves only one atmosphere lesson.
2. *Does drag scale with the row?* Yes; it is delivered so (probe row 2).
3. *Enum or declared values?* Declared values, converted in ENGINE-FIELD-FORCE's scene-region slice together with gravity (decided now: one slice would otherwise ship the environment record in two forms, an enum for air and a validated value for gravity).
4. *Absolute density or a ratio?* A ratio: the requirement row is reworded (owner decision 24, 23 November 2026); the reworded row is the coordinator's edit. Against the game's liquid, Standard is 0.0766 and Vacuum 0 (the delivered factor × 1.2259 ÷ 16); see Density in section 2.
5. *May the closure check of rows A1 to A6 run before the engine programme?* No: it runs after the programme (owner decision 22, 23 November 2026).
6. *Is humidity part of this record?* No. The scene relative humidity is ENGINE-THERMAL's, with the evaporation row; humidity is not a force field (Batch N, TH-04).

**Needs the owner.** None.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026, `_bmad/custom/element-free-engine-design.md` and `docs/gpu-f32-physics.md`. The engine projects were searched for the setting's consumers: the gas, ambient-load and airflow compile steps read it for every package alike; no code names a part.

1. **Declaration data only.** One scene setting. No per-level branch and no element logic. The enum switch at `AtmosphereBoundary.cs:10-15` maps a scene row to two numbers; it is not a branch on an element, and it is replaced by a declared row in ENGINE-FIELD-FORCE's scene-region slice.
2. **Generic laws.** Ambient loads, conical airflow and the isobaric gas law, each parameterised by the row's two values.
3. **Three layers.** Colliders none; visuals none in the scene (a HUD plaque); animation none. Sky art is never an input to physics.
4. **Flow and typing.** Construction → compile → physics. The row is an enum end to end today and two validated f32 values after the conversion; row names are content strings used only at the UI.
5. **Flat state.** Bodies 0; colliders 0; joints 0; drives 0; sensors 0; domain nodes 0; stores 0; bindings 0. One row on the construction (an enum today, two f32 values after the conversion), folded into existing per-store, per-load and per-source values at compile.
6. **Performance.** Zero cost per tick of its own. The airflow pass it scales scans the scene for occluders today; its candidates are owned by `ENGINE-SPATIAL-QUERIES` in the engine programme of owner decision 3a. Nothing here designs around it, and nothing here relies on resting bodies sleeping (`ENGINE-ISLANDS-SLEEP`; no sleep state exists today).
7. **Tests are scenarios.** A1 to A6 run the production simulation, codec and UI path.

**Verdict: conforms; the residual is blocked on the engine programme (owner decision 3a) and on ENGINE-FIELD-FORCE.**

## 7. Coverage binding (planned, not yet live)

No package, so no spec binding section is planned and none is written. The identity's task source `sequence-task-678` is to be satisfied by the cited delivered tests, the tests of ENGINE-FIELD-FORCE's scene-region slice and the level-79 tests, cited from the ledger entry. S7, which widens the reader (owner decision 15, 22 November 2026), supplies the closure rule for an identity with no package (batch summary, section 4 item 16).
