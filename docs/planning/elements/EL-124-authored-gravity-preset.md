# EL-124 · Authored gravity preset — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-124 delivery slice](../invest/named-elements.md#element-124). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code is cited as `path:line` in that checkout; `path@a6c914e` citations appear only in section 4 and describe the harvest baseline. Values marked **proposed** are starting values ([decision D1](../invest/delivery-batches.md#3-family-level-decisions)). Nothing here was built or run.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-124 · Authored gravity preset · Environment |
| Anchor | [requirements.md#element-124](../requirements.md#element-124) (task `sequence-task-677`); [named-elements entry](../invest/named-elements.md#element-124); umbrella [gap-18](../requirements.md#gap-18) (index only) |
| Owner key | S678 |
| Related identities | [EL-125 Authored atmosphere preset](EL-125-authored-atmosphere-preset.md), the other scene setting, already on the construction. Every dynamic package is a consumer. No CAT refines it. |
| Engine story | ENGINE-FIELD-FORCE ([epics](../../../_bmad-output/planning-artifacts/epics.md): it "unblocks … the Gravity-type identities that alter the local field"); its scene-region slice: the unbounded region of the one uniform-field law under ENGINE-FIELD-FORCE, owner of gravity (coordinator decision 16, 22 November 2026) |
| Status | Not started; blocked on the engine programme (owner decision 3a), whose stories it needs only for their order (the scene field is folded at compile; the region half of row G9 takes candidates from `ENGINE-SPATIAL-QUERIES`), and on ENGINE-FIELD-FORCE. Gravity is declared per body by each package and there is no scene setting (`CuriousContraptions.Abstractions/MassFrameValues.cs:38-44`; `engine/gpu/WorkshopConstruction.cs:8-9`, `:15`). |

### Disposition

**Scene data, not a part.** An authored gravity is one declared value on the construction, applied to every body by one general compile rule. No package, directory, palette entry or catalogue key. It is blocked on the engine programme (owner decision 3a). After it, it is delivered by an engine slice under ENGINE-FIELD-FORCE and then by the level that first uses it (campaign level 17). Not a Family slice member and not a Duplicate closure.

## 2. Declaration

### Declaration to write

One field on the construction, beside the atmosphere, and the same field in authored level content.

| Field | Type | Range | Default | Unit | Editor policy |
| --- | --- | --- | --- | --- | --- |
| `gravity` | f32 magnitude of the scene's uniform acceleration field, along −Y | 1.0 to 16.0 | 9.81 | m/s² | set in level data; in the Free Workshop chosen from the named rows below, in Build only; locked in Run |

Named rows (content and UI data, not an engine enum):

| Name | Value | Reason |
| --- | --- | --- |
| Standard | 9.81 m/s² | today's value in every package and `WorkshopConstruction.Gravity` (`WorkshopConstruction.cs:15`) |
| Light | 4.905 m/s² (**proposed**) | half of Standard: falls take √2 times as long, visibly slower, and the ramp and Receiver lessons stay solvable with the same parts |

- **General form.** The engine carries a validated value, not a preset enum. A closed enum switched inside the engine (as `AtmospherePreset` is today, `CuriousContraptions.Abstractions/AtmosphereBoundary.cs:6-16`) makes each new row an engine edit; the same slice converts the atmosphere to a declared row so the environment record has one form ([EL-125](EL-125-authored-atmosphere-preset.md)). A declared value within a validated range needs no engine edit for a new row, so gravity starts in that form.
- **One law.** Gravity is one uniform-field law (coordinator decision 16, 22 November 2026). A uniform acceleration field declares a vector and a region: unbounded (the scene environment, this identity's value along −Y) or a bounded volume (a declared part's region, as [EL-089](EL-089-gravity-effect-pad.md) of Batch L uses). The field at a body is the sum of the fields whose region contains its centre of mass. A body's declared gravity is its response to the Standard field (9.81 m/s² along −Y), so its acceleration is its declared gravity scaled by the field at it; a declared buoyant ambient load scales with the same field, inside a region and in the scene alike, because buoyant force is proportional to gravity. Buoyancy in air is the body's declared ambient lift (coordinator decision 28, 22 November 2026): the Balloon's declared 11.5 m/s² is the value the field scales. Bodies that declare zero gravity stay at zero. There is no second rule for a region.
- **Compile rule for the scene field.** The scene field is folded into each body's acceleration and each buoyant ambient load at compile (each multiplied by `gravity` ÷ 9.81) and costs nothing per tick. This is the rule the atmosphere already uses for ambient loads (`engine/gpu/WorkshopPhysicsCompiler.Gas.cs:37-38` multiplies by the boundary's relative pressure). No solver, ABI or wire change is needed for the body record: it already holds a gravity vector per body. Bounded regions are not needed by this identity; they take candidates from `ENGINE-SPATIAL-QUERIES` and their work is bounded by the bodies inside.
- **Range.** 16.0 is the existing admission bound on a body's gravity vector (`CuriousContraptions.Abstractions/WorldAccelerationDeclaration.cs:25`; `CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:161`, `:174-175`). 1.0 is **proposed**: slow but playable; weightlessness is a different game and is excluded. A scaled buoyant load has its own single bound (section 5, **Bound**).
- **Fixed for a Run.** The value is compiled at Run and cannot change until Build.
- **Independent of difficulty.** Puzzle precision is a separate field (`engine/gpu/WorkshopPuzzle.cs:11`, `:75`) and never selects or changes gravity.
- **Direction.** Not declared for the scene field: it is always −Y, and the plaque's arrow always points down. Sideways gravity would remove the support every authored level assumes (D19).
- **Bodies, colliders, joints, ports, stores, package parameters:** none.

### Art and solidity

No body, collider or art node. A quiet cream environment plaque (HUD) shows a navy arrow that always points down, whose length follows the value and the row's name, with gold marking the selection; it is visible in Build and Run. No sky recolouring per value (cream `#fff8e9`, navy `#293954`, gold `#f7cb52`; [gap-18 visual style](../requirements.md#gap-18)). The arrow distinguishes it from the atmosphere's air symbol by shape. The plaque reads the construction and writes nothing to physics.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol | Exercising test (body opened) |
| --- | --- | --- | --- | --- |
| 1 | A gravity vector declared per body, compiled and integrated each substep | present | `MassFrameValues.cs:38-44`; `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:454` | `PackageRuntimeParityTests.EveryMechanicalPackageDeclaresTheCompiledBodyMotionMassDragAndGravity`; `PayloadInstanceTests.BothKindsFallAgainAfterExactResetAndReload` |
| 2 | Admission bound of 16 m/s² on a declared acceleration | present | `WorldAccelerationDeclaration.cs:25` | `ParameterizedMassTests.PhysicalDeclarationsRefuseBadDomainsReferencesAndInactiveFields` (admits a 16 m/s² vector) |
| 3 | A scene setting that scales declared per-body values at compile | narrower: atmosphere only; owner ENGINE-FIELD-FORCE (scene region) | `WorkshopPhysicsCompiler.Gas.cs:21`, `:37-38` | `BalloonTests.ExistingBallsHaveAmbientDragOnlyInStandardAndRestoreTheirEnvironment` (asserts compiled gravity −9.81 and ambient drag by preset) |
| 4 | A scene setting saved, refused when missing or undefined, edited only in Build, undoable | narrower: atmosphere only; owner ENGINE-FIELD-FORCE (scene region) | `engine/gpu/WorkshopSavePrevalidation.cs:44-49`; `ui/WorkshopAtmosphere.cs:38-60` | `BalloonTests.MissingOrUnknownSerializedAtmosphereIsRefusedRatherThanDefaulted`; `PalettePlacementTests.AtmospherePickerEditsOnlyBuildStateAndUndoRestoresTheExplicitPreset` |
| 5 | A uniform acceleration field with an unbounded (scene) region, applied to every body and every buoyant ambient load | missing; owner ENGINE-FIELD-FORCE (scene region) | none (`WorkshopConstruction.cs:8-9`) | none |
| 6 | Environment fields in authored level content | missing; owner ENGINE-FIELD-FORCE (scene region) | no environment field in `engine/gpu/PuzzleDefinition.cs` or `content/puzzles.json` (searched) | none |
| 7 | Environment on the observation port and the plaque | missing; owner ENGINE-FIELD-FORCE (scene region) | none in `engine/gpu/WorkshopObservationProjection.cs` (searched) | none |

Counts: present 2; present-untested 0; narrower 2; missing 3. Every narrower and missing row has one owner, the scene-region slice of [ENGINE-FIELD-FORCE](../../../_bmad-output/planning-artifacts/epics.md#story-engine-field-force-the-one-uniform-field-law-and-declared-force-regions), which states gravity as the one uniform-field law with a scene region and bounded regions (coordinator decisions 16 and 29). The bounded region is the same law's other region kind and is not needed by this identity; Batch L's EL-089 must cite this law. The scene relative humidity of Batch N is not on this record: it is ENGINE-THERMAL's, with the evaporation row, and humidity is not a force field (Batch N, TH-04).

## 4. Sources and legacy

- **Requirements.** Row: "Validated world acceleration is visible and fixed for a Run"; outcome "Save and replay preserve it independently of difficulty" ([element-124](../requirements.md#element-124)). Integration: the same construction responds predictably to two gravity environments; save/export/replay preserve the world ([gap-18](../requirements.md#gap-18)).
- **Audit.** "Expose existing gravity ... with typed presets and faithful previews ... changing difficulty must not secretly swap the authored world" (`docs/physics-puzzle-gap-audit.md@a6c914e:L97-L97`).
- **Decisions.** [S635 environment](../invest/decisions.md#s635): a nondefault environment visibly changes the ball's fall; out-of-range values refused; Save/Load and Reset keep it.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Machine data carried `Gravity = 9.81` and `Pressure = 1` defaults, saved with the construction and restored on load. | `engine/MachineData.cs@a6c914e:L164-L172`; `reference/cpu/MachineWorld.cs@a6c914e:L355-L356`, `reference/cpu/MachineWorld.cs@a6c914e:L477-L477` | Carry forward persistence as a validated value. |
| 2 | Gravity could change only while idle. | `reference/cpu/MachineWorld.cs@a6c914e:L185-L189` | Carry forward ("fixed for a Run"). |
| 3 | World gravity became a (0, −g, 0) world setting. | `reference/cpu/MachineWorld.cs@a6c914e:L494-L494` | Carry forward direction −Y, as each package declares it. |
| 4 | Precision (difficulty) was a separate saved field from gravity. | `engine/MachineData.cs@a6c914e:L187-L196` | Carry forward the separation. |
| 5 | Isolated drops reach first impact at √(2h/g) ± 0.02 s at zero pressure. | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L14-L81` | Carry forward the law; the acceptance below measures distance at a fixed time, not an impact tick. |
| 6 | Most legacy tests set `Gravity = 0` to isolate mechanisms. | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L52-L52` | Do not carry forward: zero is outside the supported range. |

Files consulted: `engine/MachineData.cs`, `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/PhysicsCalibrationTests.cs`, `CuriousContraptions.tests/BumperTests.cs`.

## 5. Acceptance outline

Point of truth: [element-124](../requirements.md#element-124) and the [gap-18 integration](../requirements.md#gap-18).

### Behaviour and numbers

- **Law.** The uniform-field law above. For the scene field each body's acceleration term is its declared gravity scaled once at compile; it is integrated in the mechanical substeps (Phase 3, force integration) of every tick ([solver model](../../gpu-f32-physics.md#solver-model)). Nothing is read or branched per level during a tick.
- **Free fall.** Distance fallen after 0.5 s is ½ g t²: Standard ½ × 9.81 × 0.25 = 1.226 m; Light ½ × 4.905 × 0.25 = 0.613 m. The two differ by 0.613 m. Ambient drag (0.04 1/s) shortens either by about 0.7 %, under 0.01 m. Tolerance ±0.05 m, the existing free-flight allowance (`WorkshopPhysicsEngineTests.FreeFlightDragChangesOneSecondSpeedWithoutDampingSpin` uses 5 cm over a one-second window): an unscaled Light run misses by 0.61 m and a doubly scaled one (2.45 m/s², 0.307 m) by 0.31 m.
- **Fall time.** From 3.0 m: Standard √(2 × 3 / 9.81) = 0.78 s; Light √(6 / 4.905) = 1.11 s, a ratio of √2. Stated for the lesson; not asserted as an arrival tick.
- **Buoyancy.** A Balloon with default `buoyancy` 11.5 m/s² (`elements/Balloon/BalloonElementDeclaration.cs:24`) has net upward acceleration 11.5 − 9.81 = 1.69 m/s² under Standard. With the ambient load scaled, Light gives (11.5 − 9.81) × 0.5 = 0.845 m/s², half as lively. Without scaling it would be 11.5 − 4.905 = 6.6 m/s², nearly four times Standard, which is the defect the rule excludes.
- **Scene and region agree.** The field at a body decides its motion, however the field is made up. A Balloon (`buoyancy` 11.5 m/s², `drag` 0.4 1/s; `BalloonElementDeclaration.cs:24-25`) in a 16.0 m/s² scene has net upward acceleration (11.5 − 9.81) × 16.0 / 9.81 = 1.69 × 1.631 = 2.76 m/s², a terminal rise of 2.76 / 0.4 = 6.9 m/s, and after 3 s a speed of 6.9 × (1 − e^(−0.4 × 3)) = 6.9 × 0.699 = 4.8 m/s. The same Balloon in a Standard scene, inside a bounded region that adds 6.19 m/s² along −Y, meets the same summed field (9.81 + 6.19 = 16.0) and so the same 2.76 m/s² and 4.8 m/s. Outside the region it rises as in Standard: 1.69 / 0.4 × 0.699 = 2.95 m/s after 3 s. A region that added a mass-independent acceleration without scaling buoyancy would give 11.5 − 16.0 = −4.5 m/s²: the Balloon would fall. A doubled field (2 × 9.81 = 19.62 m/s²) is outside the scene's validated range, so the comparison uses 16.0. Inside a region the summed field meets the one load bound below exactly as a scene field does.
- **Bound.** At 16.0 the scale is 16 / 9.81 = 1.631 and a −9.81 body compiles to −16.0, inside the body-gravity bound (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:174-175`). 16.01 is refused by the scene value's own validated range (row G4) before any body is compiled; its compiled −16.01 would also fail that body-gravity bound. A scaled buoyant load has one bound on its compiled (field-scaled) value, the body's combined acceleration |gravity + load| ≤ 16 m/s² already checked at `engine/gpu/GasSceneValidation.cs:78`; ENGINE-FIELD-FORCE's scene-region slice deletes the separate per-load check on the compiled load, |load| ≤ 16 m/s² at `CuriousContraptions.Abstractions/GasCouplingValues.cs:65`, forward-only, so no second bound on the compiled load remains. The declared lift itself stays inside its declaration's own 16 m/s² domain (`CuriousContraptions.Abstractions/WorldAccelerationDeclaration.cs:25`, the type of `AmbientLoadDescriptor.Acceleration`, `GasCouplingValues.cs:19`), which the Balloon's 11.5 m/s² meets. Reason: a load scaled with the field can exceed 16 m/s² while the body's net acceleration stays small. In row G9 the Balloon's lift compiles to 11.5 × 1.631 = 18.76 m/s² against a gravity of −16.0, a combined |−16.0 + 18.76| = 2.76 m/s²: admitted under the one bound, and refused by today's per-load check, which is why G9 runs only in that slice.
- **Resting and contact.** Normal force, friction and rolling resistance follow the body's weight through the existing contact law; no value is restated for them.

### Acceptance rows

Planned test class `SceneGravityTests` (ENGINE-FIELD-FORCE's scene-region slice) and `GravityLessonTests` (level 17). Chrome proof is pending the post-catalogue browser phase.

| Row | Given / When / Then | Planned test | Control | Boundary | Tolerance and guard |
| --- | --- | --- | --- | --- | --- |
| G1 | Given the same Basketball at 20 m under Standard and under Light, when each runs 0.5 s, then it has fallen 1.226 m and 0.613 m | `SceneGravityTests.TheSameDropFallsByTheDeclaredAcceleration` | Standard is the control for Light | a third fixture value, 16.0: 2.0 m (½ × 16 × 0.25) | ±0.05 m |
| G2 | Given a Balloon under Standard and Light, then it rises in both and its upward speed after 1 s under Light is below Standard's | `SceneGravityTests.BuoyancyScalesWithGravity` | a Bowling ball in the same scenes falls in both | n/a | Ordering only; 5 s guard |
| G9 | Given a Balloon in a 16.0 m/s² scene, and the same Balloon in a Standard scene inside a bounded region adding 6.19 m/s² along −Y, when each runs 3 s, then both rise and their upward speeds agree | `SceneGravityTests.ABalloonRisesAtTheSameRateInASceneFieldAndInAnEqualRegionField` (the scene half runs in ENGINE-FIELD-FORCE's scene-region slice; the region half when the same story's bounded region exists) | the same region with the Balloon outside it: 2.95 m/s | a Bowling ball falls at the same rate in both; the Balloon's compiled 18.76 m/s² lift is admitted (combined 2.76 m/s², the one bound) | the two speeds within 0.3 m/s of each other and of 4.8 m/s (the wrong rules give a fall, or 2.95 m/s); 5 s guard |
| G3 | Given a construction at each difficulty precision, then the compiled gravity of every body is unchanged; given two gravity values at one precision, then the precision is unchanged | `SceneGravityTests.GravityAndDifficultyAreIndependent` | n/a | precisions 0, 0.45 and 1 | Compiled values exact (declared data, not simulated) |
| G4 | Given a value below 1.0, above 16.0 or non-finite in an edit, a level or a save, or a save with no gravity field, then it is refused with a typed reason and the installed construction is unchanged | `SceneGravityTests.OutOfRangeAndMissingGravityAreRefusedAtomically` | 9.81 loads | 1.0 and 16.0 admitted | Typed reason exact |
| G5 | Given a running or paused construction, when the picker is used, then no edit is sent and the value is unchanged; in Build the edit applies and Undo restores the previous value | `SceneGravityTests.GravityIsEditedOnlyInBuildAndUndoRestoresIt` | Build edit applies | n/a | Values exact |
| G6 | Given a Light construction, when Run then Reset, then every body is at its authored state and the value is Light; when saved and loaded, the decoded value is 4.905 and a fresh run repeats G1's Light distance | `SceneGravityTests.ResetAndSaveLoadKeepTheWorld` | n/a | n/a | ±0.05 m; decoded field exact |
| G7 | Given a body that declares zero gravity (a static base), then it compiles to zero under every value | `SceneGravityTests.ZeroDeclaredGravityStaysZero` | n/a | n/a | Exact |
| G8 | Given authored level 17, its solution solves under its authored gravity, and the same construction under the other row does not reach the goal | `GravityLessonTests.AuthoredLessonSolvesOnlyInItsAuthoredWorld` | the other row | n/a | the whole declared Run (30 s default) |

The plaque's visibility in Run is a UI row of the same slice (`SceneGravityTests.ThePlaqueShowsTheActiveValueInBuildAndRun`). A wrong-kind connection refusal does not apply (no port); G4 is the parameter refusal.

### Integration

- **Collaborators.** Every dynamic package (all delivered ones declare −9.81 or zero); the Balloon's ambient load; the atmosphere setting (independent of it); a bounded field region ([EL-089](EL-089-gravity-effect-pad.md), Batch L), which sums with the scene field under the one law.
- **Campaign.** Introduction 17 "Down to Earth", practice 18, reuse 97 and 139 ([GAP-18 allocation](../requirements.md#campaign-gap-allocation)). Cross-element task [sequence-task-551](../requirements.md#sequence-task-551).
- **Export.** gap-18 also names export. No portable export format exists (GAP-12); that part of the criterion stays open and is proven when GAP-12 delivers one. Save, load and replay are proven here.
- **Lesson fixtures.** None beyond locked placements.

### Delivery

- **Shape.** Blocked on the engine programme (owner decision 3a). Then one engine slice, ENGINE-FIELD-FORCE's scene-region slice, carrying the uniform-field law with its unbounded region, the construction's environment record (gravity, and the atmosphere as a declared row: [EL-125](EL-125-authored-atmosphere-preset.md)), the compile rule, save schema, level-content fields, projection and plaque; then a level slice for 17. Row G9's region half runs in the slice that delivers the bounded region. No package.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), needed for its order only: the scene field uses no programme story, and the region half of row G9 takes candidates from `ENGINE-SPATIAL-QUERIES`; S5 if it has not landed (the slice changes `schemas/workshop_authoring.fbs`). It does not wait for the bounded region or the rest of ENGINE-FIELD-FORCE.
- **Files.** `engine/gpu/WorkshopConstruction.cs`, the physics compiler's body and ambient-load compile, `CuriousContraptions.Abstractions/GasCouplingValues.cs` (the per-load check deleted; `engine/gpu/GasSceneValidation.cs:78` stays the one load bound), `schemas/workshop_authoring.fbs` and its codec and pre-validation, `engine/gpu/PuzzleDefinition.cs`, the observation projection and its TypeScript read model, a Workshop UI control beside `ui/WorkshopAtmosphere.cs`, every test that builds a `ConstructionRecord` by hand.
- **Review tier.** Engine.
- **Disqualifies the plan.** A delivered package declares gravity in a direction or magnitude that the scale would push past 16 m/s², or a buoyant load whose scaled sum with its body's gravity would pass 16 m/s²; a delivered test depends on `WorkshopConstruction.Gravity` being a constant in a way the field breaks (two tests read it today).

## 6. Open questions

**Resolved.**

1. *Which second environment?* Light, 4.905 m/s² (D1, D19).
2. *Does the Free Workshop expose it?* Yes, with a picker beside the atmosphere's, in Build only (decided now: the atmosphere already works this way and the requirement says the world is visible).
3. *Does buoyancy scale with gravity?* Yes, in the scene and inside a bounded region alike: one uniform-field law (coordinator decision 16, 22 November 2026; see the Balloon arithmetic).
4. *What does "export" mean?* The GAP-12 portable format, which does not exist; that part stays open with GAP-12.
5. *Preset enum or value?* A validated value with named rows as content (decided now: a new row then needs no engine edit).
6. *A doubled-gravity acceptance?* Not used: 2 × 9.81 = 19.62 m/s² is above the 16.0 admission bound, so row G9 compares a 16.0 scene with an equal summed field in a region (decided now). Its scaled Balloon lift of 18.76 m/s² is inside the one combined bound (section 5, **Bound**).

**Needs the owner.** None.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026, `_bmad/custom/element-free-engine-design.md` and `docs/gpu-f32-physics.md`.

1. **Declaration data only.** One scene value. No per-level branch, no solver or kernel change, no update loop.
2. **Generic laws.** One uniform-field law: body acceleration and buoyant ambient load under the field at the body, the scene field folded once at compile for every body alike. The missing capability is general and has one owner, ENGINE-FIELD-FORCE.
3. **Three layers.** Colliders none; visuals none in the scene (a HUD plaque); animation none.
4. **Flow and typing.** Construction → compile → physics; the plaque reads the construction. The value is f32 in a validated range; row names are content strings used only at the UI.
5. **Flat state.** Bodies 0; colliders 0; joints 0; drives 0; sensors 0; domain nodes 0; stores 0; bindings 0. One f32 on the construction; it changes existing per-body values and adds no table.
6. **Performance.** Zero cost per tick for the scene field: the scale is applied at compile, with no region test and no per-body lookup. Bounded regions of the same law take candidates from `ENGINE-SPATIAL-QUERIES` and are not part of this identity. Bodies at rest under any field sleep only once `ENGINE-ISLANDS-SLEEP` exists; no sleep state exists today.
7. **Tests are scenarios.** G1, G2, G6, G8 and G9 run the production simulation; G3, G4 and G7 the production compiler and codec; G5 the production UI path.

**Verdict: conforms; blocked on the engine programme (owner decision 3a) and on ENGINE-FIELD-FORCE for one general capability stated element-free.**

## 7. Coverage binding (planned, not yet live)

No package, so no spec binding section is planned and none is written. The identity's task source `sequence-task-677` is to be satisfied by the tests of ENGINE-FIELD-FORCE's scene-region slice and the level-17 tests, cited from the ledger entry. S7, which widens the reader (owner decision 15, 22 November 2026), supplies the closure rule for an identity with no package (batch summary, section 4 item 16).
