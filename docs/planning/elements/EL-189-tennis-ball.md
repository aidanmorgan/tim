# EL-189 · Tennis ball — current named-identity contract

**Current delivery:** [EL-189 delivery slice](../invest/named-elements.md#element-189), Story 12.1a. [CAT-064](CAT-064-tennis.md) and the [approved native spec](../../../_bmad-output/implementation-artifacts/spec-12-1a-tennis-ball.md) record current declarations and source decisions. Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md). Browser qualification remains deferred until every named element passes.

Rewritten on 22 November 2026 as one current text, checked against `elements/TennisBall/` and its tests in checkout `8d1d68aa`. Citations without `@` are `path:line` in that checkout; `Decl` is `elements/TennisBall/TennisBallElementDeclaration.cs`. `path@a6c914e` citations appear only in section 4 and are source facts.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-189 · Tennis ball · Gravity |
| Anchor | [requirements.md#element-189](../requirements.md#element-189) (task `sequence-task-742`); [named-elements entry](../invest/named-elements.md#element-189); scope source [campaign element coverage](../requirements.md#campaign-element-coverage) |
| Owner key / slice | S297; `named-el-189` |
| Refines | [CAT-064 tennis](CAT-064-tennis.md) ([requirement](../requirements.md#current-cat-064)). Contrasts: [EL-187](EL-187-basketball.md), [EL-188](EL-188-bowling-ball.md) |
| Row and outcome | "Light compliant spherical cargo retains distinct calibrated contact properties." Outcome: "Its bounce differs from the bowling ball because of declared material, not IDs." |
| Status | Delivered through package `tennis`, published in `15c8681d`. The closure review is blocked on the engine programme (owner decision 3a), including `ENGINE-CAPABILITY-INSTANCES` (payload contract), and runs after it (owner decision 22); the seam repair (part B2) is a repair and may run before it |

### Disposition

**Candidate Duplicate closure of the delivered package `tennis`** (`elements/TennisBall/`, palette order 64, `Decl:14`), in the closure-review group "Cargo" ([delivery batches 2a](../invest/delivery-batches.md#2-duplicate-closures)). The identity names no parameter, port, mode or material beyond the package's declaration: no delta. It gets no package, catalogue entry or palette order.

Nothing is deliverable now: the closure review is **blocked on the engine programme (owner decision 3a)**. The closure review runs after that programme (owner decision 22, 23 November 2026). Under the coordinator decision of 22 November 2026 the acceptance rows of [CAT-064](CAT-064-tennis.md) are the single authority for this closure identity; the rows in section 5 cite them and add row A2.

**Closure check.** The review records, against the published SHA:

1. `TennisBallTests.TennisReboundsAboveMatchedHeavierBallsAndSettlesOnTheBench` (row A1): the bounce differs from the Bowling ball's.
2. One added test, row A2: the same Tennis ball with only its `bounce` parameter set to the Bowling ball's 0.14 rebounds as low as a Bowling ball. This is the half of the outcome that says "not IDs", and no delivered test changes `bounce` and observes the rebound.
3. Reset and save/load: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` and `TennisBallTests.EveryConfiguredFieldIsRequiredAtSaveAndWireDecode`.

Then the `element-189` ledger entry cites `tennis` and those tests. The seam repair (section 2) is a defect of the package, owned by its own contract; it is not a delta of this identity and does not block the closure.

## 2. Declaration

Delivered; the full record is [CAT-064](CAT-064-tennis.md). In short: one dynamic body with a `radius`-bound sphere collider (`Decl:33`–`:38`), solid-sphere moments 0.4 × mass × radius², gravity (0, −9.81, 0) m/s², one ambient load whose lift is `buoyancy` and whose drag is `drag`, both scaled by the scene's atmosphere preset (`Decl:55`–`:57`), one airflow receiver, payload policy `CaptureTarget` (`Decl:45`), no port, store or event.

| Parameter | Unit | Minimum | Maximum | Default | `Decl` |
| --- | --- | --- | --- | --- | --- |
| `mass` | kg | 1/1024 | 1024 | 0.35 | `:16` |
| `bounce` | none | 0 | 1 | 0.85 | `:17` |
| `radius` | m | 1/16 | 16 | 0.25 | `:18` |
| `buoyancy` | m/s² | 0 | 16 | 0 | `:19` |
| `drag` | 1/s | 0 | 0.125 | 0.04 | `:20` |

All are `Real`, build-only, refused (not clamped) outside the range.

**Material, as declared data.** The body declares restitution 0.85, friction 0.3, rolling resistance 0 and bounce threshold 0.1 m/s (`Decl:32`), and one `MaterialParameterBinding` writes `bounce` into the body's restitution (`Decl:39`–`:40`; `MaterialComponent.Restitution`, `CuriousContraptions.Abstractions/MaterialParameterBinding.cs:5`). So the bounce is a declared, player-visible value on the general contact law, which is the identity's whole claim. Friction, rolling resistance and the threshold are fixed. The material model has no density, strength, thermal or optical property, and this row asks for none.

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Felt` | sphere r 0.25 m, felt yellow `#b8db59` | solid; collider `col_0_0` | body pose; build-time scale radius × 4 (`Decl:24`) |
| `Visual/Seam` | torus, inner 0.245 m, outer 0.253 m, turned 45° about X, cream | printed seam, no collider (owner decision 24i, 23 November 2026); drawn 3 mm proud of the felt today | body pose; the same scale |

The seam is a print, like the Pool ball's stripe (owner decision 24i): it has no collider, and the ball's only solid is its sphere. Repair: [visible-solid remainder part B2](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-b2.md) is a flush-print repair that draws the seam flush with the felt, with no collider, no radius-bound torus and no matcher change. Part B2 is one of the visible-solid repair parts that may run before the engine programme (owner decision 22, 23 November 2026). At a glance: the small yellow ball with one tilted cream seam ring.

## 3. Engine capabilities

### Capability probe

Probe of 22 November 2026. "Opened" means the test body was read for this spec.

| Capability | Status | Symbol | Test |
| --- | --- | --- | --- |
| Parameter-bound restitution on the general contact law | present | `IParameterizedMaterialElement`, `MaterialParameterBinding` (`CuriousContraptions.Abstractions/MaterialParameterBinding.cs:8`, `:10`) | `TennisBallTests.TennisReboundsAboveMatchedHeavierBallsAndSettlesOnTheBench` (`CuriousContraptions.tests/TennisBallTests.cs:84`, opened) at the default; a non-default `bounce` reaches the compiled body in `IndependentMassAndRadiusCompileCorrectInertiaAndPersist` (`:29`, bounce 0.6). No test observes a rebound at a non-default bounce: row A2 |
| Radius-bound sphere and declared inertia | present | `IParameterizedGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:29`) | `TennisBallTests.IndependentMassAndRadiusCompileCorrectInertiaAndPersist` |
| Ambient lift and drag | present | `IAmbientLoadElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:124`) | `TennisBallTests.ConfiguredBuoyancyAndDragChangeMotionAndResetRestoresAuthoredState` (`:58`) |
| Airflow receiver | present | `IAirflowReceiverElement` (`MechanicalContracts.cs:45`) | `FanGuidanceTests.ElectricalConeGuidesFallingTennisToReceiverWithMatchedControls` (`CuriousContraptions.tests/FanGuidanceTests.cs:124`, opened) |
| Capture payload and contact load | present | `PayloadPolicy.CaptureTarget` | `TennisInteractionTests.ReceiverCapturesOnlyTheAlignedTennisPayload` (`CuriousContraptions.tests/TennisInteractionTests.cs:11`); `TwoDefaultTennisBallsLoadThePlateWhileOneStaysBelowThreshold` (`:30`, opened) |

Counts: 5 present, 0 untested, 0 narrower, 0 missing.

## 4. Sources and legacy

- **Requirements.** Row and outcome as in section 1.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Catalogue parameters mass 0.35, bounce 0.78, radius 0.25, buoyancy 0, drag 0.04 as a string-keyed dictionary. | `parts/catalog/tennis.tres@a6c914e:L14-L14` | Defaults delivered except bounce, which is 0.85 (parent Story 12.1). The dictionary is not carried: five typed parameters. |
| 2 | Isolated drop: rebound ratio bounce² ± 0.015, timing √(2h/g) ± 0.02 s. | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L14-L81` | The ratio is row A1's band; the timing is not acceptance. |
| 3 | Dropped from y = 3 it bounces and settles within 2400 ticks without sinking. | `CuriousContraptions.tests/FloorTests.cs@a6c914e:L26-L51` | Delivered in row A1 (rest within 1 mm in the final two seconds of a twenty-second guard). |
| 4 | Legacy ball contact material: restitution = bounce, threshold 0.1, friction 0.3. | `reference/cpu/MachinePart.cs@a6c914e:L236-L236` | Delivered as declared values. |
| 5 | Further facts (pressure plate, 22 airflow lessons). | [CAT-064 spec](CAT-064-tennis.md) | Held in the CAT spec. |

Files consulted: `parts/catalog/tennis.tres`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/PhysicsCalibrationTests.cs`, `CuriousContraptions.tests/FloorTests.cs`.

## 5. Acceptance outline

Point of truth: [element-189](../requirements.md#element-189) and [CAT-064](../requirements.md#current-cat-064). Chrome proof is pending post-catalogue.

### Behaviour and numbers

- **Rebound at the default.** Rest height is the bench surface −0.46 m plus 0.25 m = −0.21 m, so a release at 3 m falls h = 3.21 m. With the neutral bench the first apex is e²h = 0.7225 × 3.21 = 2.32 m above rest, a height fraction of 0.72. The delivered test accepts 0.70 to 0.75, which excludes the historical 0.78 (fraction 0.61). In the same run the Basketball reaches 0.3025 × 3.12 = 0.94 m and the Bowling ball 0.0196 × 3.18 = 0.06 m; the test requires each gap to exceed 0.5 m.
- **Rebound at the Bowling ball's bounce (row A2).** With `bounce` 0.14 and nothing else changed: 0.0196 × 3.21 = 0.063 m. The Bowling ball's own figure is 0.062 m. A 0.35 kg, 0.25 m ball then bounces like a 4 kg, 0.28 m one, so the bounce follows the declared material and not the part.
- **Why row A2 sets `drag` to 0.** The delivered row A1 does (`TennisBallTests.cs:86`). With the default drag of 0.04 1/s the ball lands at about g·t − g·k·t²/2 = 9.81 × 0.813 − 9.81 × 0.04 × 0.813² / 2 = 7.85 m/s instead of √(2 × 9.81 × 3.21) = 7.94 m/s, leaves at 0.85 × 7.85 = 6.67 m/s and rises (6.67² / 19.62) × (1 − (2/3) × 0.04 × 6.67 / 9.81) = 2.27 × 0.982 = 2.23 m instead of 2.32 m. At `bounce` 0.14 the figures are 0.061 m with drag and 0.063 m without. The Bowling ball's drag is a fixed 0.04 1/s, which gives it 0.061 m instead of 0.062 m. Every bound of row A2 holds either way; drag 0 keeps the comparison to the one changed value.
- **Fit with delivered parts.** 0.5 m across is below the Cannon's 0.6 to 0.84 m window (a `radius` of 0.30 to 0.42 m is seated); it passes the pipe's bore, 1.3 m across; 0.35 kg does not press a default 0.5 kg plate and two do (0.7 kg); a default Fan (9 N) accelerates it at 9 / 0.35 = 25.7 m/s².

### Acceptance rows

| # | Given / When / Then | Test | Control and boundary | State |
| --- | --- | --- | --- | --- |
| A1 | Three balls dropped together from 3 m: the Tennis ball rebounds highest, then the Basketball, then the Bowling ball, and the Tennis ball settles on the bench | `TennisBallTests.TennisReboundsAboveMatchedHeavierBallsAndSettlesOnTheBench` | gaps above 0.5 m; height fraction 0.70 to 0.75; rest within 1 mm and below 0.05 m/s over the last 2 s; 20 s stall guard | executed |
| A2 | The same ball with only `bounce` set to 0.14 rebounds as low as a Bowling ball | planned `TennisBallTests.ATennisBallSetToTheBowlingBounceReboundsAsLowAsTheBowlingBall`: a Tennis ball at its defaults, a Tennis ball with `bounce` 0.14 (both with `drag` 0, as row A1's scene) and a Bowling ball, released at rest from 3 m, 2 m apart, 10 s. Then the altered ball's first apex is below 0.15 m above its rest height and within 0.1 m of the Bowling ball's, while the unaltered ball's exceeds 2.0 m | control: the unaltered ball in the same run. Boundary: `bounce` 0 gives no upward speed above 0.1 m/s after the first impact. 0.15 m is under one ball radius, so a player sees "barely bounces"; 0.1 m separates 0.063 m from any value the default bounce could give (2.32 m, or 2.23 m with drag) | planned, added by the closure review; blocked on the engine programme (owner decision 3a) unless the owner lets closure reviews run first |
| A3 | Mass and radius change contact response; lift and drag change motion | `TennisBallTests.ConfiguredMassChangesMomentumTransferredToAnEqualRadiusPayload`; `ConfiguredRadiusChangesContactSpinAtFixedMass`; `ConfiguredBuoyancyAndDragChangeMotionAndResetRestoresAuthoredState` | two values each | executed |
| A4 | Receiver capture, Impact switch hit, one-versus-two-ball plate load | `TennisInteractionTests` (three theories, `:11`, `:30`, `:56`) | the misaligned drop; one ball | executed |
| A5 | Five fields persist; invalid or missing values are refused | `TennisBallTests.EveryConfiguredFieldIsRequiredAtSaveAndWireDecode` (`:164`); `InvalidConfigurationCannotEnterConstruction` (`:188`) | each bound | executed |
| A6 | Run, Reset, save and load of the package | generic `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` | — | executed |

No port exists, so no wrong-kind connection case applies.

### Integration

- **Collaborators.** Delivered: Fan, Receiver, Impact switch, Pressure plate, Basketball, Bowling ball. In flight: Bellows (12.4). Planned: Bell.
- **Campaign.** 22 levels place it, from level 3 `air_mail`; first use 1–10. Audit [sequence-task-285](../requirements.md#sequence-task-285); interaction rows [IX-01](../requirements.md#interaction-01), [IX-02](../requirements.md#interaction-02), [IX-11](../requirements.md#interaction-11).
- **Coverage binding.** The catalogue key `tennis` in `docs/coverage/catalogue-elements.json`. A closed duplicate has no binding of its own.
- **Lesson fixture (S8).** Only where a level pre-places it as a locked fixture with locked parameters.

### Delivery

Closure review "Cargo": one record, one candidate commit, one approval; owned diff is row A2's test and the ledger row. Data-only review tier.

- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (airflow occlusion), `ENGINE-SENSOR-VOLUMES` (capture), `ENGINE-ISLANDS-SLEEP` (resting) and `ENGINE-CAPABILITY-INSTANCES` (the payload contract). The closure review runs after the programme (owner decision 22, 23 November 2026), against those stories as delivered.

The seam repair (part B2, the flush seam print) is a repair and may run before the programme (owner decision 22); it is independent of the closure, and the closure review re-reads the package as part B2 left it. Disqualifier: the review finds a criterion of the row that needs a declaration change, in which case this is a small slice on the package, not a closure. Buoyancy is the declared ambient lift (owner decision 24, 23 November 2026), executed by row A3.

## 6. Open questions

Resolved:

- **Bounce 0.85**, rolling resistance 0, five build-only parameters, art: decided by Story 12.1a and recorded in CAT-064.
- **Closure group.** "Cargo", as delivery batches 2a lists it.
- **Row A2 belongs to the closure, not to a new slice** (decided now, standing delegation). Reason: it is a test of a delivered declaration, and without it the "not IDs" half of the outcome is argued from the declaration rather than executed.
- **Buoyancy.** The declared ambient lift (owner decision 24, 23 November 2026). This identity closes as planned, its `buoyancy` parameter being executed by `TennisBallTests.ConfiguredBuoyancyAndDragChangeMotionAndResetRestoresAuthoredState` (row A3). Floating in a liquid is `ENGINE-LIQUID`'s own acceptance (at D2's 16 kg/m³ the default ball displaces (4/3)π × 0.25³ × 16 = 1.05 kg against 0.35 kg and floats).
- **Closure reviews before the engine programme.** They wait for it (owner decision 22, 23 November 2026); the seam repair, part B2, is a named repair part and may run before it.

**Needs the owner.** None.

## Engine conformance (22 November 2026)

Against the owner's addendum of 22 November 2026, `AGENTS.md` (element-free engine gate, Tri-Graph) and `_bmad/custom/element-free-engine-design.md`.

1. **Declaration data only.** One declaration class, one scene, one icon. A search of `engine/`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle` and the abstractions projects for `tennis` and `TennisBall` found nothing. One refactor is owed, by a shared contract the part uses and not by the part: its `PayloadPolicy.CaptureTarget` (`Decl:45`) is the product role that `BodyTargetKind.AllPayloads` builds into the target filter (`CuriousContraptions.Abstractions/ContactWorkDeclarationValues.cs:31`, `:43`; engine design audit finding E12). `ENGINE-CAPABILITY-INSTANCES` replaces it with a declared participation mask; this package's declaration changes in that slice and rows A4 and A6 are re-run. The part declares no gas store, so the sphere-only gas coupling (finding E10) does not touch it.
2. **Generic laws.** Bounce: contact restitution, parameterised by `bounce`. Lift and air drag: the ambient-load law. Fan push: the conical airflow law at declared samples. Capture, plate load and switch hit: the other part's sensor rows acting on an ordinary body.
3. **Three layers.** Colliders: one hidden sphere; the seam is a print and has none (owner decision 24i). Visuals: felt and seam, never colliders. Animation: none; the `radius` scale is a build-time binding.
4. **One-way flow and typing.** Typed f32 parameters with typed units; nothing reads art.
5. **Flat state.** Bodies 1 (dynamic); colliders 1 sphere; joints, drives, sensors, domain nodes, stores and ports 0; airflow receivers 1 (4 samples); ambient loads 1; material bindings 1; collider bindings 3; presentation bindings 1 (build-time scale).
6. **Performance.** One primitive, two after the repair, the second because the seam is a drawn solid. The smallest admitted radius is 1/16 m; a small fast ball is kept from tunnelling by the engine's speculative contact, not by a check here. Engine gaps, each owned inside the engine programme (owner decision 3a): island sleeping does not exist, so a resting ball is stepped every tick (`ENGINE-ISLANDS-SLEEP`; CAT-064's sentence that a resting ball sleeps with its island describes the design, not the delivery); every residence sensor tests every payload each tick (`ENGINE-SENSOR-VOLUMES`, candidates from `ENGINE-SPATIAL-QUERIES`); the Pressure plate's load and the Impact switch's trigger that this ball sets off are found by scanning rows (`ENGINE-CONTACT-OBSERVATIONS`); airflow occlusion walks every collider per sample line (`ENGINE-SPATIAL-QUERIES`).
7. **Tests are scenarios.** Every row runs the production simulation through the Workshop harness.

**Verdict: Conforms with fixes** (the flush seam print, part B2; the E12 contract refactor owed by `ENGINE-CAPABILITY-INSTANCES`); blocked on the engine programme (owner decision 3a).
