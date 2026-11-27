# EL-192 · Receiving basket — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-192 delivery slice](../invest/named-elements.md#element-192). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text, checked against `elements/Receiver/` and its tests in checkout `8d1d68aa`. Citations without `@` are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4 and are source facts. The delivered declaration is recorded once, in [CAT-004](CAT-004-basket.md); this spec states what the named identity adds and what remains open.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-192 · Receiving basket · Gravity |
| Anchor | [requirements.md#element-192](../requirements.md#element-192) (task `sequence-task-745`); [named-elements entry](../invest/named-elements.md#element-192); scope source [campaign element coverage](../requirements.md#campaign-element-coverage) |
| Owner key / slice | S296; `named-el-192` |
| Refines | [CAT-004 basket](CAT-004-basket.md) ([requirement](../requirements.md#current-cat-004)); catalogue title "Receiver". Goal identities that read its capture: [EL-120](EL-120-quantity-goal.md) to [EL-123](EL-123-protected-end-state-goal.md) |
| Row and outcome | "Physical open container supports cargo and exposes an actual containment goal." Outcome: "A ball beside or passing over it does not satisfy capture." |
| Status | Delivered through package `basket`; its own outcome is executed; one CAT-004 clause is open (Disposition). Further work is blocked on the engine programme (owner decision 3a), in particular `ENGINE-SENSOR-VOLUMES` and `ENGINE-CAPABILITY-INSTANCES`, and runs after it (owner decision 22) |

### Disposition

**Refinement of the delivered package `basket`** (`elements/Receiver/`, palette order 2, `ReceiverElementDeclaration.cs:39`). No package, catalogue entry or palette order of its own, and no declaration delta. Its own outcome is executed (rows A1 to A3). The ledger entry also retains the CAT-004 acceptance, of which one clause is open:

- **Capture "in the basket's moving frame".** The engine evaluates a sensor's position in its frame body's pose but its speed in the world frame, so a carried container would capture wrongly. The capability is *narrower*; owner: `ENGINE-SENSOR-VOLUMES` in the engine programme (owner decision 3a), which evaluates residence, passage and guides inside the physics tick, relative to the sensor's frame, with candidates from `ENGINE-SPATIAL-QUERIES` (`docs/planning/elements/readiness/engine-design-gap.md`, proposed story 4). The delivered `basket` is static and no planned lesson moves one, so the clause is proven by that story's fixture scenes and then by the first carried capture owner (EL-069, EL-070 or EL-062).

The second residual older texts listed, several Receivers while balls exist, is delivered and tested (row A5). When the moving-frame row passes, the identity has no delta and the ledger entry cites `basket` and the rows below.

Nothing is deliverable now: every step is **blocked on the engine programme (owner decision 3a)**. The data-only halo repair (row A8) runs after that programme (owner decision 22, 23 November 2026): it is not one of the named visible-solid repair parts (A, B, B2, C) that may run first. `ENGINE-SENSOR-VOLUMES` also changes this package's declaration (section "Engine conformance", point 1), so rows A1 to A6 are re-run in that story's slice.

## 2. Declaration

Delivered and unchanged by this identity; every value and line is in [CAT-004 §2](CAT-004-basket.md#2-declaration). In short: one static body with five box colliders (floor, two sides, back, lower front lip); one residence sensor, local box (±0.66, −0.4 to 0.45 + `capture_margin`, ±0.66) m, with `capture_speed_limit` (default 1.5 m/s), `capture_dwell` (default 0.35 s) and `capture_participation`; one planar guide above the mouth with `force_support_margin` and `force_maximum_acceleration` (default 0, so no guide); one `Captured` event; no port, node, store or joint. The goal is a typed objective that reads the committed capture, not a wire.

**Material, as declared data.** Restitution 0.12, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`elements/Receiver/ReceiverElementDeclaration.cs:61`), the four fields of `ContactMaterialDeclaration` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:133`–`:143`). Under the product pair law a Basketball lands with 0.55 × 0.12 = 0.066, so it settles instead of bouncing out: "supports cargo" is a consequence of the declared value, not of a capture rule. No density, strength, thermal or optical property is needed or present.

### Art and solidity

Five wall and floor meshes, each with its own box collider; `Visual/Visual_Halo`, a pale ring 2.5 cm above the rim, is decorative (a glow, scaled and recoloured by presentation bindings) and a ball passes through it. The hand table agrees (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:143`); `spec-engine-art-solidity.md` lists no finding. One repair is open on the package, owned by CAT-004: the halo's scale should follow `capture_margin` (one parameter binding replaces a constant event binding; data-only). Pick radius and render meshes are never capture authority. At a glance: the only open-topped green box (`#4aab94`) with a lower front lip and a pale ring above its rim.

## 3. Engine capabilities

### Capability probe

Probe of 22 November 2026. "Opened" means the test body was read for this spec; the others are cited from CAT-004's probe and confirmed by name.

| Capability | Status | Symbol | Test |
| --- | --- | --- | --- |
| Static box contact holding a payload | present | `WorkshopCollisionKernels.CollideBoxSphere` (`CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:349`) | `PayloadInstanceTests.AReceiverCapturesEitherKindAndAShiftedMissNever` (`CuriousContraptions.tests/PayloadInstanceTests.cs:236`) |
| Residence sensor: bounded local box, speed limit, dwell, participation, margin, one latch | present | `WorkshopPhysicsEngine.CommitResidence` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:890`) | `WorkshopPhysicsEngineTests.DeclaredCaptureMarginWidensTheMouthWhileABesideDecoyStaysOutside` (`CuriousContraptions.tests/WorkshopPhysicsEngineTests.cs:157`, opened); `DeclaredCaptureSpeedRejectsFastPassAtTheProductionSensor` (`:102`, opened) |
| Planar guide: descending, inside, supported, bounded acceleration | present | `WorkshopPhysicsEngine.ApplyGuides` (`WorkshopPhysicsEngine.cs:1029`) | `WorkshopPhysicsEngineTests.DeclaredReceiverGuideActsOnlyForDescendingInsideTargetsAndDoesNotMoveItsFrame` |
| Sensor and guide in an offset, rotated static frame | present | the same two methods | `IndependentResidenceGuideTests.TransformedFrames_OffsetAndRotatedSensorAndGuide_EvaluatedInLocalCoordinateSpace` (`CuriousContraptions.tests/IndependentResidenceGuideTests.cs:147`) |
| Several Receivers and payloads, each with its own capture truth | present | sparse capture state | `CompoundMassAndRollingTests.DistantReceiversCaptureTheirOwnPayloadsWithSparseStateAndResetReplay` (`CuriousContraptions.tests/CompoundMassAndRollingTests.cs:566`) |
| A goal naming a body reads the committed capture | present | `WorkshopGoalEvaluator` (`engine/gpu/WorkshopGoalEvaluator.cs:8`) | `FanGuidanceTests.ElectricalConeGuidesFallingTennisToReceiverWithMatchedControls` (`CuriousContraptions.tests/FanGuidanceTests.cs:124`, opened: a `Captured` goal on a `basket`) |
| Event-driven feedback on the halo, cleared by Reset | present | `PresentationDriver.Event` | `BasketballResourceTests.ReceiverHaloFollowsTheDeclaredCaptureBlendAndRestsOnItsNeutralPalette` |
| Parameter-driven uniform scale of an art node (the halo repair) | present | `PresentationParameterBinding` (`CuriousContraptions.Abstractions/ElementPresentation.cs:119`) | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (Weight row) |
| Residence and guide on a **moving** frame body | narrower | position is read in the frame's pose (`WorkshopPhysicsEngine.cs:909`–`:912`), speed in the world frame (`:933`; the guide likewise, `:1071`) | `ENGINE-SENSOR-VOLUMES` |
| Residence sampled at each substep endpoint (`docs/gpu-f32-physics.md:154`) | narrower | sampled once per committed tick, after all substeps (`WorkshopPhysicsEngine.cs:348`, `:358`) | `ENGINE-SENSOR-VOLUMES` (evaluation inside the tick) |
| Further goal kinds and several goals per level | missing | one goal per level today | [ENGINE-GOALS](../../../_bmad-output/planning-artifacts/epics.md#story-engine-goals-declared-goal-kinds-and-ordered-objectives), in five slices (the fifth is `engine-goal-count-window-hold`; owner decision, 22 November 2026); required by the Goal identities, not by this row |

Counts: 8 present, 0 untested, 2 narrower, 1 missing.

## 4. Sources and legacy

- **Requirements.** Row and outcome as in section 1. CAT-004: outside, fast, disabled and leave-and-reenter controls break dwell; capture, event and halo latch once; Reset clears all; pick radius and render meshes are not authority.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Physical dwell owns capture regardless of presentation changes; restores exactly. | `CuriousContraptions.tests/BasketResidenceOwnershipTests.cs@a6c914e:L17-L60` | Delivered (`WorkshopSimulationTests.NativeCaptureOccursOnPhysicalCommitAndNeverOnProjectionOrCancellation`). |
| 2 | Authored margin applies bounded force through shared physics without moving the basket; a fast passage through the whole window still uses shared guidance and resets. | `CuriousContraptions.tests/BasketGuideTests.cs@a6c914e:L17-L101` | Delivered as the planar guide (probe row 3). |
| 3 | Guide clearance uses all owned collider children, not stale presentation. | `CuriousContraptions.tests/BasketGuideTests.cs@a6c914e:L104-L132` | Delivered (`WorkshopPhysicsEngine.cs:1062`). |
| 4 | Legacy pick radius 0.85 m. | `parts/BasketPart.cs@a6c914e:L12-L12` | Delivered as presentation only; never physics authority. |

Files consulted: `CuriousContraptions.tests/BasketResidenceOwnershipTests.cs`, `CuriousContraptions.tests/BasketGuideTests.cs`, `parts/BasketPart.cs`, `parts/catalog/basket.tres`.

## 5. Acceptance outline

Point of truth: [element-192](../requirements.md#element-192) and [CAT-004](../requirements.md#current-cat-004). Chrome proof is pending post-catalogue.

### Behaviour and numbers

- **Passing over.** The sensed box ends 0.45 m + margin above the Receiver's origin. In the delivered test the Receiver is at y = 1 and a ball is held at y = 1.5, that is 0.50 m in the local frame: with the default margin 0.02 the bound is 0.47 m and the ball is Outside; with margin 0.10 the bound is 0.55 m and it is Dwelling. A ball crossing above the rim is therefore not counted unless the level widens the margin.
- **Beside.** The decoy in the same test is at x = 3 m, outside the ±0.66 m box by 2.34 m: Outside in its own sensor row.
- **Fast through-pass.** With the limit set to 1 m/s, a ball inside at 0.5 m/s is Dwelling and one at 8 m/s is Outside; with the limit 0.25 m/s the 0.5 m/s ball is Outside. Capture needs the default 0.35 s of continuous dwell, 42 ticks at 120 Hz; leaving discards the partial dwell.
- **Why the moving frame fails today.** Take a container translating at 2 m/s, above the 1.5 m/s default limit. A ball riding in it has world speed 2 m/s and is never captured, though it is at rest in the container. A ball at rest in the world is swept by the 1.32 m interior in 1.32 / 2 = 0.66 s, longer than the 0.35 s dwell, at world speed 0, so it is captured, though it passed straight through. Both results are wrong, and both follow from reading speed in the world frame.
- **Tick order.** `docs/gpu-f32-physics.md` places sensor evaluation and event dispatch in Phase 5, after the four mechanical substeps of Phase 3. The delivered engine agrees in order: `CommitResidence` runs once per committed tick after all substeps. A goal reads the committed capture on the main thread and nothing flows back.
- **Fit with delivered parts.** The interior is 1.32 m wide and 0.85 m tall, so every delivered ball fits (widest 0.70 m); the 1.5 m/s limit is below a Bumper's 8 m/s target, so a bumped ball must settle before it counts.

### Acceptance rows

| # | Given / When / Then | Test | Control and boundary | State |
| --- | --- | --- | --- | --- |
| A1 | A ball held just above the rim is not inside unless the margin is widened; a ball beside the box is never inside | `WorkshopPhysicsEngineTests.DeclaredCaptureMarginWidensTheMouthWhileABesideDecoyStaysOutside` | margins 0.02 and 0.10 m either side of the 0.05 m the ball stands above the rim. The result depends on the sensor testing the ball's centre: at local y 0.50 m the centre is outside the 0.47 m bound while the 0.34 m ball overlaps the box by 0.31 m. Under `ENGINE-SENSOR-VOLUMES` the Receiver's sensor therefore declares centre containment as its occupancy rule; with shape overlap this row would fail | executed; re-run in the `ENGINE-SENSOR-VOLUMES` slice |
| A2 | A fast pass through the box never dwells | `WorkshopPhysicsEngineTests.DeclaredCaptureSpeedRejectsFastPassAtTheProductionSensor` | speeds either side of two limits; the same declared centre containment, with speed measured relative to the sensor's frame (zero frame velocity here) | executed; re-run in the `ENGINE-SENSOR-VOLUMES` slice |
| A3 | A payload that stays inside below the limit for the dwell is captured once; a shifted drop never is | `PayloadInstanceTests.AReceiverCapturesEitherKindAndAShiftedMissNever`; `WorkshopPhysicsEngineTests.DeclaredReceiverParticipationAndDwellDriveRealCapture` | disabled participation; the dwell is a declared timer, so its elapsed time is asserted | executed |
| A4 | Leaving before the dwell discards it | `WorkshopPhysicsEngineTests.LeavingBeforeDwellDiscardsPartialResidenceAndReentryStartsAFullNewDwell` | re-entry needs a full dwell | executed |
| A5 | Two Receivers keep their own captures; another payload cannot satisfy a goal naming a body | `CompoundMassAndRollingTests.DistantReceiversCaptureTheirOwnPayloadsWithSparseStateAndResetReplay`; `CaptureTruthKeepsTheAuthoredPayloadThroughGrowthPlacementResetAndLoad` | reversed instance order | executed |
| A6 | Run/Reset and save/load restore settings and capture state; invalid parameters are refused | `IndependentResidenceGuideTests.Lifecycle_RunResetAndSaveLoadRoundtrip`; `WorkshopSaveTests.InvalidDeclaredReceiverParameterRejectsAndValidSaveStillLoads` | NaN and infinity | executed |
| A7 | A payload riding a moving capture owner dwells and is captured, and a bystander swept by it is not | owned by `ENGINE-SENSOR-VOLUMES`, on a declared fixture with no element package: `SensorVolumeTests.APayloadRidingAMovingFrameIsResidentAfterItsDwellAndABystanderSweptByTheFrameIsNot` (the 2 m/s scene above: the rider is at rest in the frame, the bystander at rest in the world; stall guard 15 s). The first carried-owner slice (EL-069, EL-070 or EL-062) repeats it as a scenario on its own package | the same frame held still captures the resting ball and not one beside it. Boundary: frame speeds 1.4 and 1.6 m/s either side of the 1.5 m/s limit give the same result, because the limit applies to speed relative to the frame | blocked on `ENGINE-SENSOR-VOLUMES` |
| A8 | The halo's scale is 1 + `capture_margin` and the capture cue does not change it | planned `BasketballResourceTests.ReceiverHaloScaleFollowsTheDeclaredCaptureMargin` (CAT-004 §5; margins 0, 0.02, 0.3) | the default rests at 1.02 | planned, data-only; blocked on the engine programme (owner decision 3a) and run after it (owner decision 22) |
| A9 | Lessons with a Receiver solve, and not with a cause missing | `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` (12 lessons) | 60, 120 and 240 Hz | executed |

Wrong-kind connection: the part has no port; the generic case is S4 row 5.

### Integration

- **Collaborators.** Delivered: every ball kind, Balloon, Ramp, Springboard, Bumper, pipe family, Funnel, Trampoline, Fan, Cannon. Not delivered: the carried capture owners (EL-062, EL-069, EL-070) and the Goal identities EL-120 to EL-123, which read this part's committed capture and add no port to it.
- **Campaign.** 12 of the 15 Playable lessons use it; first use 1–10; reuse 21–30, 41–50, 91–100, 136–150. Interaction rows [IX-01](../requirements.md#interaction-01), [IX-07](../requirements.md#interaction-07).
- **Coverage binding.** The catalogue key `basket` in `docs/coverage/catalogue-elements.json`. A refinement has no named-package binding of its own.
- **Lesson fixture (S8).** Not needed; lessons set its six parameters per knot through authored content.

### Delivery

- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` and `ENGINE-CONTACT-OBSERVATIONS`, then `ENGINE-SENSOR-VOLUMES` (row A7 and the list-valued declarations) and `ENGINE-CAPABILITY-INSTANCES` (the participation mask that replaces `AllPayloads`). Every step runs after the programme (owner decision 22, 23 November 2026).

1. Row A8: CAT-004's halo repair, data-only review tier. It is not one of the named visible-solid repair parts, so it runs after the engine programme (owner decision 22).
2. `ENGINE-SENSOR-VOLUMES` (engine tier, all six layers), after `ENGINE-SPATIAL-QUERIES` and `ENGINE-CONTACT-OBSERVATIONS` in the programme's order. Its slice owns row A7 on a fixture, converts this package to the list-valued sensor and guide declarations, and re-runs rows A1 to A6 and A9. The first carried capture owner's slice then repeats row A7 on its package.
3. The ledger entry then cites `basket` and rows A1 to A9.

Disqualifiers: the engine story adding a second evaluation path for static frames (the static frame must be the zero-velocity case of one rule); a change to the sensor or guide boxes.

## 6. Open questions

Resolved:

- **Is a moving Receiver needed before the capability exists?** No. No scheduled lesson moves a `basket`; the clause is proven by the engine story and the first carried owner (Batch A coordinator decision 1).
- **Several Receivers with balls.** Delivered and tested (row A5); the old capacity refusal is gone.
- **Further goal kinds.** ENGINE-GOALS in five engine slices (owner, 21 and 22 November 2026); a goal is declared data read by one general evaluator from committed state. A held level reads the committed residence of this part's sensor with its became-true occurrence, which `ENGINE-SENSOR-VOLUMES` and the fifth goal slice publish; nothing flows back to the sensor.
- **The halo repair before the engine programme.** It waits for it (owner decision 22, 23 November 2026): only the named visible-solid repair parts A, B, B2 and C run first, and the halo rebinding is none of them.

**Needs the owner.** None.

## Engine conformance (22 November 2026)

Against the owner's addendum of 22 November 2026, `AGENTS.md` (element-free engine gate, Tri-Graph) and `_bmad/custom/element-free-engine-design.md`.

1. **Declaration data only.** One declaration class, one scene, one icon. A search of `engine/`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle` and the abstractions projects for the id `basket`, "Receiver" as a part name and the class name found no code that names the part. The word "receiver" there means an airflow receiver; one comment says "receiver frame" for a capture sensor's frame body (`engine/gpu/WorkshopGoalWake.cs:12`), a wording to make neutral when that file is next touched (the goal-evaluation slice owns it). Two refactors are owed, by shared contracts the package uses. First, it implements the single-instance `IResidenceSensorElement.ResidenceSensor` and `IPlanarGuideElement.PlanarGuide` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:25`–`:33`): one sensor and one guide per element is a product shape (engine design audit finding E6), and `ENGINE-SENSOR-VOLUMES` deletes both properties for list-valued capabilities with local ids; this package then declares a list of one sensor and one guide in that slice. Second, its sensor and guide select `TargetSelector.AllPayloads` (`elements/Receiver/ReceiverElementDeclaration.cs:16`, `:23`), the product role of finding E12, which `ENGINE-CAPABILITY-INSTANCES` replaces with a declared participation mask.
2. **Generic laws.** Holds a ball: rigid contact with pair restitution and friction on static boxes. Captured once: the residence-sensor law (inside a declared box, at or below a declared speed, for a declared dwell, one occurrence per sensor and target until Reset). Nudged toward the centre: the planar-guide law with a bounded acceleration. Solved: objective evaluation reading the committed occurrence. None is "when a ball touches the basket".
3. **Three layers.** Colliders: five hidden boxes. Visuals: five wall meshes and the halo. Animation: the halo's colour and scale bindings read the committed `Captured` event and an authored parameter and write nothing back.
4. **One-way flow and typing.** Capture flows physics to goal to presentation. Participation is a typed choice; parameters are f32.
5. **Flat state.** Bodies 1 (static); colliders 5 (boxes); joints and drives 0; sensors 2 (1 residence, 1 planar guide region); domain nodes, stores and ports 0; events 1; presentation bindings 4 (3 event and 1 parameter after the halo repair).
6. **Performance.** Static body; the sensor and guide are bounded boxes. Five colliders because each drawn wall is a solid and an open container cannot be made from fewer. Engine gap, not designed around: `CommitResidence` scans every dynamic body for every sensor each tick (`WorkshopPhysicsEngine.cs:913`) and keeps pair state in a managed dictionary (`:890`); `ApplyGuides` loops every dynamic body over every guide each substep (`:1032`). These scene-proportional passes are owned by `ENGINE-SENSOR-VOLUMES`, with candidates from `ENGINE-SPATIAL-QUERIES`, in the engine programme scheduled ahead of element delivery (owner decision 3a); the managed pair dictionary is `ENGINE-TICK-ALLOCATION`'s.
7. **Tests are scenarios.** Every row runs the production simulation through defined interfaces.

**Verdict: Conforms with fixes** (the halo rebinding; the E6 and E12 contract refactors owed by `ENGINE-SENSOR-VOLUMES` and `ENGINE-CAPABILITY-INSTANCES`; the moving-frame clause waits on `ENGINE-SENSOR-VOLUMES`, stated element-free); blocked on the engine programme (owner decision 3a).
