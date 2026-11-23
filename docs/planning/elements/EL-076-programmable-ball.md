# EL-076 · Programmable ball — named-identity spec

**Current delivery (24 October 2026):** [EL-076 delivery slice](../invest/named-elements.md#element-076). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`), repaired the same day against the independent audit and the owner decisions of 22 November 2026.** A `path:line` citation without a commit is in this checkout; `path@a6c914e:L…` resolves in git history. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#precision-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-076 · Programmable ball · Specialist |
| Anchors | [requirements.md#element-076](../requirements.md#element-076) (task `sequence-task-629`); [named entry](../invest/named-elements.md#element-076); scope index [todo-217](../requirements.md#todo-217) |
| Proof owner | S646 ([decision](../invest/scope-corrections.md#s646)) |
| Disposition | To deliver as a single element after two general engine stories (section 3). Not a member of the "Ball presets" Family slice: that family's members are fixed, and this part's point is choosing. Not a duplicate of the Tennis ball: the Tennis ball takes free numbers, this takes only named presets and refuses two combinations. |
| Planned package | id `programmable_ball`, directory `elements/ProgrammableBall/`, class `ProgrammableBallElementDeclaration` |
| Refines / extends | The ball shape of [CAT-001](CAT-001-ball.md), [CAT-014](CAT-014-bowling.md), [CAT-064](CAT-064-tennis.md); shares its preset pattern with [EL-080 Programmable box](EL-080-programmable-box.md) |
| Status | not started; blocked on the engine programme (owner decision 3a), then `ENGINE-PARAMETER-BOUND-FRAMES` (its Choice clause) and `ENGINE-PARAMETER-COMBINATIONS`, then S7 |

## 2. Declaration

"Programmable" means preset admission, not a controller (S646): the author picks three named presets in Build, and the ball is then an ordinary ball with those values. It has no program, port, sensor or update of any kind, and nothing can be edited during Run.

Follow the Tennis ball (`elements/TennisBall/TennisBallElementDeclaration.cs:9`): the package implements `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IParameterizedGeometryElement` (`SceneGeometry.cs:29`), `IParameterizedMaterialElement` (`CuriousContraptions.Abstractions/MaterialParameterBinding.cs:10`), `IAmbientLoadElement` (`MechanicalContracts.cs:124`) and `IAirflowReceiverElement` (`MechanicalContracts.cs:45`). `KinematicMode.Dynamic`, `SupportedDomains = Mechanical`, `PayloadPolicy.CaptureTarget`, footprint one body, one moving.

### Parameters

| Key | Kind | Options (index order) | Default | Editor policy |
| --- | --- | --- | --- | --- |
| `size` | Choice | `Small`, `Medium`, `Large` | `Medium` | Reject, Build only |
| `mass` | Choice | `Light`, `Standard`, `Heavy` | `Standard` | Reject, Build only |
| `surface` | Choice | `Lively`, `Standard`, `Dead` | `Standard` | Reject, Build only |

A Choice must use the Reject policy (`CuriousContraptions.Bundle/BundleValidator.cs:504`). There is no Real parameter, so there is no numeric field.

Each option carries the declared values it writes (all **starting values**, taken from delivered balls so lessons transfer):

| Choice | Option values |
| --- | --- |
| `size` → radius | 0.25 m (the Tennis ball's), 0.34 m (the Basketball's), 0.45 m |
| `mass` → mass | 0.35 kg (Tennis), 1 kg (Basketball), 4 kg (Bowling) |
| `surface` → restitution | 0.78, 0.55 (Basketball), 0.14 (Bowling) |
| `surface` → rolling resistance | 0.035, 0.035, 0.03 |

**Excluded combinations**, declared as data: (`size` Large, `mass` Light), because it reads as a balloon and has no buoyancy; and (`mass` Heavy, `surface` Lively), because a 4 kg ball at bounce 0.78 swamps every contact-work lesson. Each excludes three of the 27 combinations and they do not overlap, so 21 are admitted. The default combination is admitted.

### Body, colliders and material

| Field | Value | Bound to |
| --- | --- | --- |
| Sphere collider (index 0, hidden) | radius r | `size` (three half-extent bindings, as `TennisBallElementDeclaration.cs:33`–`:38`) |
| Band collider (index 1, hidden) | torus, centreline radius r, tube radius r ÷ 32, full sweep | `size` (two bindings); this part's own proportional band rule (tube = r ÷ 32) |
| Mass | m | `mass` |
| Principal moments | 0.4 m r² on each axis | `PrincipalMomentOperation.ScaledMassLengthSquared` with the mass and radius operands, as `TennisBallElementDeclaration.cs:48`–`:53` |
| Restitution | by option | `surface` (a material binding, as `TennisBallElementDeclaration.cs:39`–`:40`) |
| Rolling resistance | by option | `surface` (a second material binding: `MaterialComponent.RollingResistance` exists, `MaterialParameterBinding.cs:5`, but no delivered package binds it) |
| Friction, bounce threshold | 0.3, 0.1 m/s | fixed |
| Gravity | (0, −9.81, 0) m/s² | fixed |
| Ambient load | linear drag 0.04 1/s, no buoyancy | fixed |
| Airflow receiver | four normalised samples of weight 0.25 | as `TennisBallElementDeclaration.cs:28`–`:31` |

**Band height.** The band follows this part's own proportional rule (tube = r ÷ 32), so it stands 8 mm, 11 mm and 14 mm proud at the three sizes. The owner's "rolling over a 1 cm band is accepted" was given for the fixed 0.32–0.34 m balls; the proportional rule keeps the step relative to the ball the same at every size: it meets the bench acos(r ÷ (r + r ÷ 32)) = 14.1° ahead of the contact point whatever r is. Default combination: radius 0.34 m, 1 kg, moments 0.4 × 1 × 0.34² = 0.04624 kg·m², restitution 0.55.

No joints, drives, stores, sensors, ports or events.

### Metadata and bindings

Category `Motion`; display name "Programmable ball"; description "A ball you set up before the run: pick its size, weight and surface."; colour (0.97, 0.80, 0.32) (gold `#f7cb52`); pick radius 0.45; palette order 101 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)): after every delivered ball, and before the Soccer ball (102), with which it is first used at level 2b (ties go by identity number). Presentation parameter bindings (`PresentationParameterBinding`, `CuriousContraptions.Abstractions/ElementPresentation.cs:119`), each with one value per option:

| Node | Property | Parameter | Values by option |
| --- | --- | --- | --- |
| `Visual` | `UniformScale` | `size` | 1.0, 1.36, 1.8 (the scene is authored at radius 0.25) |
| `Visual/MassPip2` | albedo red, green, blue | `mass` | gold, navy, navy |
| `Visual/MassPip3` | albedo red, green, blue | `mass` | gold, gold, navy |
| `Visual/SurfaceDots` | albedo red, green, blue | `surface` | navy, gold, gold |
| `Visual/SurfaceBar` | albedo red, green, blue | `surface` | gold, gold, navy |

Thirteen bindings (one scale, four marks of three colour channels). No binding uses `Visible`: a mark that is "off" is drawn in the body's gold. So nothing here shows or hides a node, which is the business of the "Parameter-selected parts" row of `ENGINE-PARAMETER-BOUND-FRAMES`; this part does not use that row.

### Art and solidity

No delivered package uses a texture, so the marks are geometry. The sphere is drawn as one body mesh with five shallow cut-outs, each filled by an inlay patch that is a piece of the same spherical surface (a cap or a strip at the sphere's own radius). The body and its inlays never overlap and nothing is proud; together they are the sphere.

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Sphere` | sphere radius 0.25 (authored) less five cut-outs, gold `#f7cb52`, satin | solid: with the inlays below it is the sphere of the sphere collider | body pose; scaled by `size` |
| `Visual/MassPip1`, `MassPip2`, `MassPip3` | three circular cap inlays in a row beside the band; pip 1 is always navy `#293954` | solid: part of the sphere surface, zero proud | body pose; pips 2 and 3 coloured by `mass` |
| `Visual/SurfaceDots`, `Visual/SurfaceBar` | an inlay of three small caps, and a strip inlay, on the other side of the band | solid: part of the sphere surface, zero proud | body pose; coloured by `surface` |
| `Visual/Band` | torus, centreline 0.25, tube 0.0078 (authored), cream `#fff8e9` | solid: the torus collider, the same shape at every size | body pose; scaled by `size` |

One proud band with its own collider, as the owner decided for banded balls (21 November 2026). Mass and surface are read by count and shape (one, two or three navy pips; dots or a bar), not by colour alone. Icon: a gold disc with a cream band and three pips. It differs from every fixed ball at a glance by its gold body with pips.

### Behaviour and numbers

Laws: gravity, contact with restitution (the product of the two colliders' values, `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:117`), friction, rolling resistance, ambient drag. The presets only choose the declared values; after compile the ball is indistinguishable from a fixed ball with the same numbers.

- **Bounce.** Fall fraction (first rebound peak over fall height, both measured from rest) is restitution²: Lively 0.61, Standard 0.30, Dead 0.02 before drag; drag removes about 1 % of speed each way on a 2.7 m fall (k·t/3 with k = 0.04 1/s and t = √(2 × 2.7 ÷ 9.81) = 0.74 s), giving about 0.59, 0.29 and 0.02 (e² × (1 − 2kt/3) × (1 − 2ket/3): 0.608 × 0.980 × 0.985 = 0.59; 0.303 × 0.980 × 0.989 = 0.29; 0.020 × 0.980 = 0.02).
- **Load.** Light (0.35 kg) is under the Pressure plate's 0.5 kg default (`elements/PressurePlate/PressurePlateElementDeclaration.cs:39`); Standard and Heavy press it.
- **Fit.** Across the band the widths are 0.516 m, 0.701 m and 0.928 m. All pass the 1.3 m pipe bore. Only Medium seats in the Toy cannon (window 0.6–0.84 m, `elements/Cannon/CannonElementDeclaration.cs:32`) and lies in the 0.62–0.78 m range the Hopper feeds singly. None enters the 0.46 m Pool pocket.
- **Launch.** In a default cannon (45 J, target 12 m/s) a Medium ball leaves at 12 m/s when Light (the store would allow 16 m/s), 9.5 m/s when Standard and √(2 × 45 ÷ 4) = 4.7 m/s when Heavy.
- **Refusal.** Choosing an option that would complete an excluded combination is refused at the edit, with a typed reason, and the construction is unchanged. A save or a pasted instance holding an excluded combination, an option index outside 0–2, a fractional index or any extra parameter is refused atomically.

## 3. Engine capabilities

Map row families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, ProgrammableController, RigidBodyDynamics, SignalPropagation, TimedCommand. Under D14 the row is navigation: S646 excludes a controller, so ProgrammableController, SignalPropagation, TimedCommand, FiniteWorkActuation and JointConstraint are not used. Tests marked † were opened for this revision.

| Capability | Status | Evidence |
| --- | --- | --- |
| Dynamic sphere whose radius, mass, inertia and restitution are bound to declared parameters | present (for Real parameters) | `TennisBallElementDeclaration.cs:33`–`:53`; `TennisBallTests.IndependentMassAndRadiusCompileCorrectInertiaAndPersist` † (`CuriousContraptions.tests/TennisBallTests.cs:29`) |
| A parameter outside its declaration refused at construction, save and wire | present | `TennisBallTests.InvalidConfigurationCannotEnterConstruction` † (`TennisBallTests.cs:188`) |
| A Choice parameter with closed options; an index out of range or fractional refused | present | `CuriousContraptions.Abstractions/ParameterDescriptor.cs:52`; `FanAdmissionTests.PackageScalarEndpointsAndClosedModeAreAdmittedAtomically` † (`CuriousContraptions.tests/FanAdmissionTests.cs:267`, rows `supply` 3 and 0.5) |
| Bounce, rest, capture, Run, Reset, save and load of a ball | present | `TennisBallTests.TennisReboundsAboveMatchedHeavierBallsAndSettlesOnTheBench` † (`:84`); `PayloadInstanceTests.BothKindsFallAgainAfterExactResetAndReload` † (`CuriousContraptions.tests/PayloadInstanceTests.cs:249`) |
| A torus collider bound to a parameter on a dynamic ball | present, untested | `GeometryComponent.Radius` (`SceneGeometry.cs:24`) is bound on the Weight's ring colliders (`elements/Weight/WeightElementDeclaration.cs:65`–`:70`); this part's band is the first radius-bound torus on a ball, and this slice adds its test |
| A rolling-resistance material binding | present, untested | `MaterialComponent.RollingResistance` (`MaterialParameterBinding.cs:5`); the Tennis ball binds restitution only, and no package binds this component |
| **A Choice option carries the bound value**: any binding that today takes a Real parameter (collider component, material component, mass, principal-moment operand, presentation parameter binding) taking instead a Choice with one declared f32 value per option | missing | every such binding requires a Real today (`BundleValidator.cs:425`, `:567`, `:855`–`:863`); a Choice only selects an airflow input or sensor participation. Owner: `ENGINE-PARAMETER-BOUND-FRAMES`, its Choice clause (owner decision 3a and coordinator decision 16; Batch O G5; clause text in the Batch L summary) |
| **Declared excluded combinations of Choice options**, refused atomically at edit, placement, save and load | missing | no cross-parameter rule exists. Owner: proposed `ENGINE-PARAMETER-COMBINATIONS` (`engine-parameter-combinations`), text in the Batch L summary |
| Coverage admits a named-identity package | narrower | owner: coverage follow-on story S7 (section 7) |

Both missing capabilities are stated without reference to this part: any package may give a Choice per-option values, and any package may exclude option tuples. EL-080 uses both; EL-162, GAP-07 and RAD-07 need the first.

## 4. Sources and legacy

- Requirement: "Author-selected validated mass, size and material preset defines physical behaviour"; outcome "Unsupported combinations are rejected; no runtime numeric solution editor" ([element-076](../requirements.md#element-076)).
- Decision [S646](../invest/scope-corrections.md#s646): "Enumerate the allowed typed programmable material values; one accepted preset behaves as declared, one invalid combination is refused at input."
- A search at `a6c914e` for "programmable" over the Epic 7 deletion paths found no programmable-ball source.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Legacy ball parameters: Radius, Mass, Bounce, Buoyancy, Drag | `reference/cpu/BallPart.cs@a6c914e:L4-L13` | carry forward the field set as preset targets; do not carry forward free numbers |
| L2 | A ball with a missing parameter is rejected; Run and Restore round-trip | `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs@a6c914e:L47-L81` | carry forward as "an incomplete or unknown preset is refused"; current form `TennisBallTests.EveryConfiguredFieldIsRequiredAtSaveAndWireDecode` (`TennisBallTests.cs:164`) |
| L3 | Isolated drop: rebound ratio is bounce² | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L14-L67` | carry forward as row A2 with game-grade bands |
| L4 | Legacy machines could override one ball's radius per instance by a string key | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L65-L70` | do not carry forward: replaced by the closed `size` Choice |
| L5 | Plate minimum 0.5 kg; a 0.35 kg ball does not press, a Basketball does | `CuriousContraptions.tests/PressurePlateTests.cs@a6c914e:L38-L62` | carry forward as row A3 |

**Files harvested:** `reference/cpu/BallPart.cs`, `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs`, `CuriousContraptions.tests/PhysicsCalibrationTests.cs`, `CuriousContraptions.tests/FloorTests.cs`, `CuriousContraptions.tests/PipeTests.cs`, `CuriousContraptions.tests/PressurePlateTests.cs`, `parts/catalog/pressure_plate.tres`, `parts/catalog/tennis.tres`.

## 5. Acceptance outline

Requirement row: [element-076](../requirements.md#element-076). Planned test class `ProgrammableBallTests`. Drop rows release each ball with its centre 3 m above the bench origin.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given each of the 21 admitted combinations, when compiled, then the body has that combination's radius, mass, moments, restitution and rolling resistance, and a band torus of centreline r and tube r ÷ 32 | `EveryAdmittedPresetCompilesItsDeclaredValues` (theory) | the default equals (0.34, 1, 0.04624, 0.55, 0.035) | radius, mass, restitution and rolling resistance compared exactly (authored constants); moments equal to the f32 evaluation of 0.4·m·r² from those operands |
| A2 | Given Medium Standard balls with `surface` Lively, Standard and Dead released together, then their fall fractions are 0.50–0.64, 0.24–0.33 and at most 0.05, and all rest | `TheSurfacePresetDecidesTheBounce` | the Standard ball | predicted 0.59, 0.29, 0.02 (Behaviour, Bounce); the bands do not overlap and leave at least 0.17 between Lively and Standard, about 0.45 m of peak height |
| A3 | Given a Medium Standard-surface ball on a default Pressure plate, then Light leaves it off and Standard and Heavy switch it | `TheMassPresetDecidesTheLoad` | Standard | — |
| A4 | Given Small, Medium and Large balls, then each rests with its centre its own radius above the bench, all pass a straight Pipe, and only Medium seats in a Toy cannon | `TheSizePresetDecidesFitAndRestHeight` | Medium | rest height within 2 mm of r once still (a ball resting on its band is avoided by releasing it with the band's axis vertical, so the band lies at the equator) |
| A5 | Given `mass` Light, when `size` is set to Large, then the edit is refused with a typed reason and the construction is unchanged; likewise `surface` Lively with `mass` Heavy | `AnExcludedCombinationIsRefusedAtTheEdit` | Large with Standard is admitted; Heavy with Dead is admitted | both excluded pairs, each approached from either parameter (four rows) |
| A6 | Given a save or wire record holding an excluded combination, an option index of 3 or 0.5, a missing Choice, or an added `radius`, `mass_kg` or `bounce` number, then it is refused atomically with the typed reason | `ASaveCannotCarryAnExcludedPresetOrAnyNumber` | a save of an admitted non-default combination round-trips | — |
| A7 | Given a Running or Paused world, then the part has no run control and an edit command for any of its parameters is refused | `NothingCanBeEditedDuringRun` | the same edit in Build is accepted | the compiled run-control table has no row for this part |
| A8 | Given Run, Reset, save and load of a non-default combination, then pose and presets are restored and a second Run repeats A2's outcome | `ResetSaveAndLoadKeepThePresets` | — | generic row: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| A9 | Given each size, then the drawn sphere (body mesh and inlays together) and the band have their colliders, a probe is stopped by the band, and no collider reaches beyond the art; for each `mass` and `surface` option the marks carry that option's colours | `TheBandIsSolidAndTheMarksFollowThePresetsAtEverySize` and a `VisibleSolidColliderTests` row | — | probe clearance 2 mm; mark colours are authored constants read through the presentation evaluator |

A1–A4 are "preset defines physical behaviour"; A5 and A6 are "unsupported combinations are rejected"; A6 and A7 are "no runtime numeric solution editor". No ports, so no connection refusal applies. Retained CAT-001, CAT-014 and CAT-064 acceptance (gravity, rolling, bounce, momentum, rest): A2, A4 and the delivered `TennisBallTests.ConfiguredMassChangesMomentumTransferredToAnEqualRadiusPayload`.

### Integration

- Works with Receiver, Pressure plate, Pipe, Toy cannon, Bumper, Ramp, Fan (all delivered); EL-080 Programmable box (its Large size holds this ball's Large size: inner width 1.28 m against 0.93 m).
- Campaign: the coverage ledger row "Basketball, bowling, tennis, baseball, soccer and programmable physical presets" allows "later preset-specific lessons … 91–100 after material rules", reuse 21–30, 41–50, 91–100, 136–150 (`docs/planning/requirements.md:2396`).
- A level that places it with locked presets needs the lesson-fixture story S8 (per-parameter locks).

### Delivery

- **Shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element; then `ENGINE-PARAMETER-BOUND-FRAMES` (which 3a places before its first consumer; this part needs its Choice clause); then `ENGINE-PARAMETER-COMBINATIONS`; then S7.
- **Form.** Port roles, node arity, target masks and mouth identities are written in the form `ENGINE-CAPABILITY-INSTANCES` leaves; the names here are today's (this part declares only `PayloadPolicy` and an airflow receiver).
- **Files.** `elements/ProgrammableBall/` (project, declaration and `.uid`, `programmable_ball.tscn` with the body mesh, five inlay meshes, the band and two hidden colliders; no texture file; `icon.svg` and its import file); `CuriousContraptions.tests/ProgrammableBallTests.cs`; one `CuriousContraptions.slnx` line; until S3, the hand lists a ball package still edits; the ledger row; the binding section once S7 admits it.
- **Review tier.** Engine tier while it has *present, untested* rows; data-only once both engine stories are published with tests and S3 has removed the shared lists.
- **Disqualifiers the probe re-checks.** A Choice that still cannot write a mass or a principal-moment operand after the first story; an exclusion rule that is checked only in the editor and not at save and wire decode; a torus that cannot be bound to a parameter; a visible-solid check that cannot classify a body mesh and its inlays against one sphere collider.

## 6. Open questions

All resolved.

1. **Preset membership and the two exclusions:** the spec's proposal (D1, D19). Three independent Choices are kept rather than one list of 21 named presets, because the requirement names mass, size and material separately and a player reasons about them separately.
2. **The default may equal the Basketball's values.** The S636 distinctness rule concerns fixed presets; this part is distinct by being configurable, and its gold body and pips tell it apart.
3. **No buoyancy preset** (D19): no requirement or lesson uses it.
4. **Surface values:** Lively stays at the spec's 0.78; the delivered Tennis default is now 0.85 (`TennisBallElementDeclaration.cs:17`), and the gap to it is deliberate so that the Tennis ball remains the liveliest ball.
5. **Art:** one proud cream band with its collider, by the proportional seam rule; mass and surface shown by inlaid marks whose colour is bound to the Choice, never by showing or hiding a node.
6. **Owner of Choice-bound values:** `ENGINE-PARAMETER-BOUND-FRAMES` (coordinator decision 16).

Needs the owner: none.

## Engine conformance (22 November 2026)

1. **Declaration data only.** One body, two colliders, three Choice parameters with per-option values, a list of excluded option pairs and thirteen presentation parameter bindings. No program, controller, callback or engine test on this package.
2. **Generic laws.** Gravity, contact, friction, rolling resistance and drag with the chosen values. Refusal of an excluded combination is the general parameter-admission rule applied to declared data, not a check written for this ball.
3. **Three layers.** Colliders: one sphere and one torus, both hidden and both sized from `size`. Visuals: a body mesh and five inlays that together are the sphere, and a band mesh matching the torus at every size; nothing but the band is proud. Animation: none during a Run; the presentation parameter bindings are resolved from authored parameters, and every node follows the body pose. No node is shown or hidden.
4. **Flow and typing.** Three closed Choices; option names appear only at the UI and save boundary; values are f32. No ports or events.
5. **Flat state.** Bodies 1 (dynamic); colliders 2 (sphere 1, torus 1); joints 0; drives 0; sensors 0; domain nodes 0; stores 0; airflow receivers 1; ambient loads 1; presentation parameter bindings 13. Every count is the same for all 21 combinations; the Choices change values only, resolved at compile.
6. **Performance.** Two primitives, the Basketball's count. Nothing is evaluated per tick for the presets. Nothing sleeps today; a ball at rest will sleep with its island when `ENGINE-ISLANDS-SLEEP` lands.
7. **Tests are scenarios.** Rows A2–A4 run the production simulation; A1, A5–A7 exercise the production admission path.

**Verdict: conforms, with two general capabilities missing, each owned by one element-free engine story** (the audit's fixes of 22 November 2026 are applied: one owner for Choice-bound values, no `Visible` by Choice, the mark mechanism, the band rule).

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here: the reader parses every spec that carries that heading and fails the coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `programmable_ball` |
| Named identity | EL-076, `../invest/named-elements.md#element-076` |
| Acceptance | `todo-217`, `sequence-task-629` |
| Decision | S646, `../invest/scope-corrections.md#s646` |
| Mode records | one per Choice: Size (Small, Medium, Large), Mass (Light, Standard, Heavy), Surface (Lively, Standard, Dead); the exact record form for a parameterised part is S7's to fix |

What it has: the `s646` decision block (`docs/planning/invest/scope-corrections.md:45`, and on published main), and its sequence task, `sequence-task-629`, beside the `element-076` anchor (`docs/planning/requirements.md:5003`–`:5005`). What the reader lacks today: it wants the todo and the task adjacent in one row, while `todo-217` is a scope-index row (`:4085`) apart from the task.
