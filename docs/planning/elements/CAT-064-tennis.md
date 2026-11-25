# CAT-064 · Tennis ball — element spec

**Current delivery:** delivered by Story 12.1a ([native spec](../../../_bmad-output/implementation-artifacts/spec-12-1a-tennis-ball.md)); CAT-064 and EL-189 remain the requirement identities. Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [testing guidance](../../testing-guidance.md). Browser qualification is pending until post-catalogue. EL-189's closure review is **blocked on the engine programme (owner decision 3a, 22 November 2026)**; part B2, the flush seam print, is a repair and runs before the programme (owner decision 22); the stories that own this part's gaps are `ENGINE-ISLANDS-SLEEP` (a resting ball is still stepped) and `ENGINE-CAPABILITY-INSTANCES` (payload participation mask, audit E12).

One current text, repaired 22 November 2026 after the Batch D audit, checked against `elements/TennisBall/` and its tests in checkout `8d1d68aa`. Citations without `@` are `path:line` in that checkout; `Decl` is `elements/TennisBall/TennisBallElementDeclaration.cs` and `Scene` is `elements/TennisBall/tennis.tscn`. Citations with `@a6c914e` are the legacy harvest and appear only in the closing appendix.

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-064 · `tennis` |
| Requirement anchor | [CAT-064](../requirements.md#current-cat-064); owner key CAT-064-I |
| Disposition | **Delivered.** Package id and catalogue key `tennis`, directory `elements/TennisBall/`, category Motion, palette order 64 (`Decl:12`–`:14`). Published in `15c8681d`; slice record status done |
| Mapped identity | [EL-189 Tennis ball](EL-189-tennis-ball.md) is a candidate Duplicate closure of this part ([delivery batches 2a](../invest/delivery-batches.md#2-duplicate-closures), group "Cargo"). Its closure review owns the check: EL-189's outcome "its bounce differs from the bowling ball because of declared material, not IDs", executed through this package |

## 2. Declaration

Interfaces: `IMechanicalElement`, `ISceneGeometryElement`, `IParameterizedGeometryElement`, `IParameterizedMaterialElement`, `IAmbientLoadElement`, `IAirflowReceiverElement` (`Decl:9`–`:10`).

| Parameter | Unit | Min | Max | Default |
| --- | --- | --- | --- | --- |
| `mass` | kg | 1/1024 | 1024 | 0.35 |
| `bounce` | none | 0 | 1 | 0.85 |
| `radius` | m | 1/16 | 16 | 0.25 |
| `buoyancy` | m/s² | 0 | 16 | 0 |
| `drag` | 1/s | 0 | 0.125 | 0.04 |

All five are `Real`, build-only, with the default editor policy, and persist canonically (`Decl:16`–`:20`). Unknown, missing, malformed, non-finite or out-of-range inputs are refused atomically.

| Item | Declaration |
| --- | --- |
| Body | one dynamic body; footprint 1 body, 1 moving, no constraints (`Decl:25`, `:44`); gravity (0, −9.81, 0) and no body drag (`Decl:53`) |
| Collider | one sphere whose half-extents are bound to `radius` (`Decl:33`–`:38`) |
| Material | restitution bound to `bounce` (default 0.85); friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`Decl:32`, `:39`–`:40`). Rolling arrest by resistance is not promised |
| Mass frame | declared mass; moments 0.4 × mass × radius² (`Decl:48`–`:51`) |
| Ambient load | `buoyancy` (upward) and `drag`, scaled by the scene's atmosphere preset: relative pressure 1 in Standard, 0 in Vacuum (`Decl:55`–`:57`) |
| Airflow receiver | one, on the sphere: four collider-normalised samples of weight 0.25 (`Decl:27`–`:30`) |
| Payload | `CaptureTarget` (`Decl:45`). No ports, stores, sensors or events |
| Presentation | `Visual` uniform scale = `radius` × 4, a build-time geometry binding (`Decl:24`) |

Scale decision (coordinator, 19 November 2026): the gameplay scale 0.35 kg and 0.25 m is kept because the one-ball and two-ball load control at a 0.5 kg plate depends on it; restitution is 0.85 as Story 12.1 states.

## 3. Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Felt` | sphere r 0.25 m, felt yellow `#b8db59` (`Scene:7`–`:10`) | solid: sphere collider `col_0_0` (`Scene:35`–`:37`) | the body's pose |
| `Visual/Seam` | torus, inner 0.245 m, outer 0.253 m, turned 45° about X, cream `#e8dccb` (`Scene:16`–`:19`, `:31`–`:33`) | printed seam, no collider (owner decision 24i, 23 November 2026); drawn 3 mm proud of the felt today | the body's pose |

The seam is a print, like the Pool ball's stripe (owner decision 24i): it has no collider, and the ball's only solid is its sphere. The package has no row in the visible-solid table. Repair: [part B2](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-b2.md) is a flush-print repair: it draws the seam flush with the felt, with no collider, no radius-bound torus and no matcher change.

Icon: a yellow disc with one cream seam arc. At a glance it differs from the Basketball by its smaller size and single tilted seam ring.

## 4. Engine capabilities

| Capability | Mark | Symbol and test |
| --- | --- | --- |
| Radius-bound sphere and declared inertia | present | `IParameterizedGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:29`); `TennisBallTests.IndependentMassAndRadiusCompileCorrectInertiaAndPersist` (`CuriousContraptions.tests/TennisBallTests.cs:29`) |
| Parameter-bound restitution | present | `IParameterizedMaterialElement` (`CuriousContraptions.Abstractions/MaterialParameterBinding.cs:10`); `TennisBallTests.TennisReboundsAboveMatchedHeavierBallsAndSettlesOnTheBench` (`:84`) |
| Ambient buoyancy and drag | present | `IAmbientLoadElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:124`); `TennisBallTests.ConfiguredBuoyancyAndDragChangeMotionAndResetRestoresAuthoredState` (`:58`) |
| Airflow receiver | present | `IAirflowReceiverElement` (`MechanicalContracts.cs:45`); `FanGuidanceTests.ElectricalConeGuidesFallingTennisToReceiverWithMatchedControls` (`CuriousContraptions.tests/FanGuidanceTests.cs:124`) |
| Capture payload | present | `PayloadPolicy.CaptureTarget`; `TennisInteractionTests.ReceiverCapturesOnlyTheAlignedTennisPayload` (`CuriousContraptions.tests/TennisInteractionTests.cs:11`) |

Counts: 5 present, 0 untested, 0 narrower, 0 missing.

## 5. Behaviour and numbers

- **Bounce.** A drop rebounds to about bounce² = 0.72 of its height; heavier controls rebound lower.
- **Air.** In Standard atmosphere drag 0.04 /s slows it slightly; in Vacuum there is no air drag and no buoyancy. Non-zero `buoyancy` lifts it; it is not a placeholder.
- **Fan.** A default Fan (9 N) accelerates it at 25.7 m/s², inside the 64 m/s² transfer bound; the same jet moves a Bowling ball at 2.25 m/s².
- **Cannon.** Width 0.5 m is below the 0.6–0.84 m seating window (`elements/Cannon/CannonElementDeclaration.cs:32`): a default Tennis ball is not seated. It passes the pipe bore, 1.3 m across.
- **Pressure plate.** 0.35 kg does not press a default 0.5 kg plate; two do.
- **Bell** (planned, [CAT-009](CAT-009-bell.md)): a 4 m/s strike has strength 0.35 × 4 / 6 = 0.23, below a default Sound meter's threshold at any distance: a light ball rings quietly, as intended.

## 6. Acceptance

All executed in the published package. Tolerances are quantity-specific and chosen in the tests; no impact tick, trajectory or byte comparison is asserted.

| Criterion | Test |
| --- | --- |
| Five fields compile, persist and refuse invalid values | `TennisBallTests.IndependentMassAndRadiusCompileCorrectInertiaAndPersist`, `EveryConfiguredFieldIsRequiredAtSaveAndWireDecode` (`:164`), `InvalidConfigurationCannotEnterConstruction` (`:188`) |
| Bounce and rest against heavier controls | `TennisReboundsAboveMatchedHeavierBallsAndSettlesOnTheBench` |
| Buoyancy, drag and Reset | `ConfiguredBuoyancyAndDragChangeMotionAndResetRestoresAuthoredState` |
| Mass and radius change contact response | `ConfiguredMassChangesMomentumTransferredToAnEqualRadiusPayload` (`:118`), `ConfiguredRadiusChangesContactSpinAtFixedMass` (`:137`) |
| Fan trajectory with no-supply, empty, away, out-of-reach, wall and heavy controls | `FanGuidanceTests.ElectricalConeGuidesFallingTennisToReceiverWithMatchedControls` |
| Receiver, Impact switch and one-versus-two-ball plate | `TennisInteractionTests` (three theories, `:11`, `:30`, `:56`) |
| Art scales with the physical sphere | `TennisArtTests.NativeFeltAndSeamScaleTogetherWithTheConfiguredPhysicalSphere` (`CuriousContraptions.tests/TennisArtTests.cs:10`) |
| Run, Reset, save and load of the admitted declaration | generic `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |

No port exists, so no wrong-kind connection case applies. Chrome recipe: post-catalogue.

## 7. Integration

Works with Fan, Receiver, Impact switch, Pressure plate, Basketball and Bowling ball (delivered); Bellows (Story 12.4, in flight: its row B1 propels a Tennis ball); Bell (planned). Campaign: first use in levels 1–10 ([campaign reservations](../invest/campaign-reservations.md)); 5 levels in `content/puzzles.json` place it, from 3 `air_mail` (authored position before decision 24d, paired with its plan level in [campaign coverage, section 5](readiness/campaign-coverage.md#5-removed-and-converted-levels-owner-decision-24d)). Coverage binding: catalogue key `tennis` in `docs/coverage/catalogue-elements.json`. A level that pre-places it as a locked fixture uses the lesson-fixture story (S8).

## 8. Delivery

Delivered. Residual work: (1) part B2, the flush seam print; (2) EL-189's closure review "Cargo", which adds one test: planned `TennisBallTests.ATennisBallSetToTheBowlingBounceReboundsAsLowAsTheBowlingBall` ([EL-189](EL-189-tennis-ball.md) row A2): a default Tennis ball and one with only `bounce` set to 0.14, **both with `drag` 0** (as `TennisBallTests.cs:86` does), beside a Bowling ball, released at rest from 3 m, 2 m apart, 10 s; the altered ball's first apex is below 0.15 m above rest and within 0.1 m of the Bowling ball's, the unaltered ball's exceeds 2.0 m; boundary `bounce` 0 gives no upward speed above 0.1 m/s after the first impact; (3) browser proof, post-catalogue; (4) the [CAT-064](../requirements.md#current-cat-064) checkbox is still open in `requirements.md` (see the [batch summary](readiness/batch-d.md)).

**Prerequisites, in order.** Part B2 runs before the programme, after part B (owner decision 22). The closure review is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element; in particular `ENGINE-ISLANDS-SLEEP` and `ENGINE-CAPABILITY-INSTANCES`; the closure review runs after the programme, and its checks are re-run if a programme story changes the contact or airflow outcomes it reads.

## 9. Open questions

None. Friction 0.3, bounce threshold 0.1 m/s and rolling resistance 0 are declared.

## Engine conformance

Against the owner's conformance addendum of 22 November 2026 (element-free engine gate in [AGENTS.md](../../../AGENTS.md); design contracts D1–D7 in `_bmad/custom/element-free-engine-design.md`). **Verdict: conforms with one fix pending (point 3: the printed seam is drawn 3 mm proud; part B2 draws it flush).**

1. **Declaration data only.** One declaration class of descriptors, one scene, one icon. No runtime project names it: a search of `engine/`, `ui/` and every `CuriousContraptions.*` runtime project for `tennis` and `TennisBall` returns nothing.
2. **Generic laws.** Fall, bounce and rest: rigid-body dynamics and the contact law with declared restitution, friction and threshold. Lift and air drag: the ambient-load law. Fan push: the conical airflow law on declared receiver samples. Capture, plate load and switch hits: the other part's residence, contact-load and contact-trigger rows acting on an ordinary body.
3. **Layers.** Colliders: one hidden `col_0_0` sphere; the seam is a print and has none (owner decision 24i). Visuals: `Visual/Felt` and `Visual/Seam`, never used as colliders. Animation: none.
4. **One-way flow and typing.** Nothing reads art or presentation; parameters are typed f32 with typed units; the id is a typed `PartId` at the bundle boundary.
5. **Flat state.** 1 dynamic body; 1 sphere collider; 0 joints, drives, sensors, domain nodes, stores and ports; 1 airflow receiver with 4 samples; 1 ambient load; 1 geometry-scale binding. All fixed at compile time.
6. **Performance.** One primitive collider; the printed seam adds none. Island sleeping is not implemented (`docs/gpu-f32-physics.md:273` describes it); a resting ball is still stepped. Owned by `ENGINE-ISLANDS-SLEEP`; nothing in this part works around its absence. Its smallest admitted radius is 1/16 m; tunnelling of a small fast ball is excluded by the engine's speculative contacts (Story 1.5), not by a check in this part.
7. **Tests are scenarios.** Every row of section 6 runs the production simulation through the Workshop harness.

## Appendix: Historical evidence

Evidence of the legacy part at `a6c914e`, kept so the harvest stays traceable. Nothing here is a requirement, a tolerance or a design instruction; sections 1–9 are the spec. Legacy impact ticks, tolerances and the old bounce value in the cited files are history only.

| # | Fact | Citation |
| --- | --- | --- |
| 1 | Legacy material: mass 0.35, bounce 0.78, radius 0.25, buoyancy 0, drag 0.04 (bounce is now 0.85) | `parts/catalog/tennis.tres@a6c914e:L14-L14` |
| 2 | Legacy ball contact material: restitution = bounce, bounce threshold 0.1, friction 0.3 | `reference/cpu/MachinePart.cs@a6c914e:L236-L236`; `engine/physics/PersistentContactPair.cs@a6c914e:L23-L23` |
| 3 | Legacy drag and buoyant loads scaled by world pressure, applied in a per-substep loop that is not the design | `reference/cpu/MachineWorld.cs@a6c914e:L869-L883` |
| 4 | Legacy ball parameter set: radius, mass, bounce, buoyancy, drag | `reference/cpu/BallPart.cs@a6c914e:L4-L13` |
| 5 | Each ball kind dropped alone rebounded according to its bounce | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L14-L67` |
| 6 | A dropped tennis ball bounced, did not sink into the bench and settled | `CuriousContraptions.tests/FloorTests.cs@a6c914e:L26-L48` |
| 7 | One tennis ball did not press a 0.5 kg plate; two did | `CuriousContraptions.tests/PressurePlateTests.cs@a6c914e:L38-L62`; `parts/catalog/pressure_plate.tres@a6c914e:L14-L14` |
| 8 | Every catalogue ball had to carry all five parameters | `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs@a6c914e:L47-L81` |
| 9 | Lessons: tennis is the airflow payload in 22 levels; canonical lane `air_mail` | `content/puzzles.json@a6c914e:L495-L706` |
| 10 | Depth lesson `third_dimension` | `content/puzzles.json@a6c914e:L918-L1128` |
| 11 | Activation lesson `wind_signal` | `content/puzzles.json@a6c914e:L1811-L2092` |
| 12 | Gated lesson `cold_start` | `content/puzzles.json@a6c914e:L2093-L2444`; `tools/Campaign/Program.cs@a6c914e:L64-L75` |
| 13 | The other 18 tennis lanes reuse the `air` module shifted in depth | `tools/Campaign/Program.cs@a6c914e:L16-L19`; fixture list in `docs/coverage/catalogue-elements.json` |
| 14 | The legacy catalogue stored no friction, threshold or rolling resistance for tennis (now declared) | `parts/catalog/tennis.tres@a6c914e:L14-L14` |

**Files harvested:**
- `parts/catalog/tennis.tres`
- `parts/scenes/ball.tscn` (no element knowledge: script reference only)
- `reference/cpu/BallPart.cs`
- `reference/cpu/MachinePart.cs`
- `reference/cpu/MachineWorld.cs`
- `engine/physics/PersistentContactPair.cs`
- `CuriousContraptions.tests/PhysicsCalibrationTests.cs`
- `CuriousContraptions.tests/FloorTests.cs`
- `CuriousContraptions.tests/PressurePlateTests.cs`
- `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs`
- `content/puzzles.json` (kept; not deleted by Epic 7)
- `tools/Campaign/Program.cs`
- `reference/p054-guide/puzzles-candidate.json` (no additional knowledge: the same tennis/fan placements in an older orientation schema)
