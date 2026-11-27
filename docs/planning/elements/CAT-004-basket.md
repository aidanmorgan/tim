# CAT-004 · basket (Receiver) — declaration readiness spec

**Status (verified 22 November 2026 against `8d1d68aa`):** delivered as package `basket` (`elements/Receiver/`). The declaration is data, art and bindings only. Two things stay open: an art repair (the halo does not follow the capture margin) and an engine gap (sensors are not frame-relative, not sampled per substep and not broadphase-fed; §3). Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026); the part's engine gaps are owned by `ENGINE-SENSOR-VOLUMES` (frame-relative, in-tick residence and guide; several per element), `ENGINE-SPATIAL-QUERIES` (candidates from the region query), `ENGINE-TICK-ALLOCATION` (no managed pair state in the tick) and `ENGINE-CAPABILITY-INSTANCES` (the payload target filter, audit E12); further goal kinds are `ENGINE-GOALS`. Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is pending until every named element passes. The `@a6c914e` citations in §4 record the harvest source, not current instructions.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-004 · `basket` (display name Receiver; one part identity) |
| Requirement | [CAT-004](../requirements.md#current-cat-004) |
| Mapped identities | EL-192 Receiving basket (refines this part; §8). Goal identities EL-120–EL-123 (quantity, rate-window, ordered-events, protected-end-state goals) extend the Captured goal and have their own specs. |
| Disposition | **Delivered.** Package id `basket`, directory `elements/Receiver/`. Slice records: [C-Ramp+Receiver](../../../_bmad-output/implementation-artifacts/spec-c-ramp-receiver.md) ([review](../../../_bmad-output/implementation-artifacts/review-c-ramp-receiver.md)); [9A.46j independent residence and guide](../../../_bmad-output/implementation-artifacts/spec-9a-46j-independent-residence-guide.md); multi-Receiver state by [engine-multi-collider-dynamic](../../../_bmad-output/implementation-artifacts/spec-engine-multi-collider-dynamic.md) (published `28a2d302`). |
| Roadmap | [CAT-004-I](../invest/vertical-delivery.md#basket-capture); [first_principles](../invest/vertical-delivery.md#first-principles); Stories 2.4, 3.1, 4.1, 4.3, 9A.29 |
| Levels | 12 of the 15 Playable lessons use it (11 place it, `ready_to_rebound` supplies it as inventory); planned levels in [CAT-004-I consumers](../invest/current-consumers.md#cat-004-i) |

## 2. Declaration

Delivered declaration: `elements/Receiver/ReceiverElementDeclaration.cs`; scene `elements/Receiver/basket.tscn`.

| Item | Value and source |
| --- | --- |
| Interfaces | `IResidenceSensorElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:25`) and `IPlanarGuideElement` (`:30`), both extending `IMechanicalElement` (`:7`); `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). Declared at `ReceiverElementDeclaration.cs:10`. Domain: Mechanical (`:66`). |
| Body | One static body (`KinematicMode.Static`, `:68`; footprint 1 body, 0 moving, 0 constraints, `:52`). Mass 0, moments 0, no drag, no gravity (`:72`–`78`). `PayloadPolicy.None` (`:70`). |
| Colliders | Five boxes on that body, baked in the scene as hidden `col_` nodes (centre; half extents, metres): floor `col_0_0` (0, −0.45, 0; 0.75, 0.075, 0.75); left wall `col_0_1` (−0.75, 0, 0; 0.06, 0.5, 0.8); right wall `col_0_2` (0.75, 0, 0; 0.06, 0.5, 0.8); back wall `col_0_3` (0, 0, −0.75; 0.75, 0.5, 0.06); lower front lip `col_0_4` (0, −0.2, 0.75; 0.75, 0.3, 0.06) (`basket.tscn:33`–`46`, `:77`–`105`). |
| Material (f32) | restitution 0.12, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`:61`). |
| Residence sensor | `ResidenceSensorDescriptor` (`:12`–`17`; record at `CuriousContraptions.Abstractions/MechanicalContractValues.cs:188`): local box minimum (−0.66, −0.4, −0.66), maximum (0.66, 0.45, 0.66) m in the body frame; the upper Y bound is raised by `capture_margin`; speed limit `capture_speed_limit`; dwell `capture_dwell`; participation `capture_participation`; target `TargetSelector.AllPayloads`. |
| Planar guide | `PlanarGuideDescriptor` (`:19`–`23`; record at `MechanicalContractValues.cs:203`): region minimum (−1.1, 0.5, −1.1), maximum (1.1, 1.5, 1.1) m, support height 0.5 m, support margin `force_support_margin`, maximum acceleration `force_maximum_acceleration`, target `AllPayloads`. |
| Parameters | Six, all editor policy `Reject` and `BuildOnly` (the record's defaults, `CuriousContraptions.Abstractions/ParameterDescriptor.cs:60`–`61`). `capture_margin`: Real, m, default 0.02, 0 to 1 (`:44`). `capture_speed_limit`: Real, m/s, default 1.5, 1/1024 to 64 (`:45`). `capture_dwell`: Real, s, default 0.35, 1/1024 to 30 (`:46`). `capture_participation`: Choice (Disabled, Enabled), default Enabled (`:47`). `force_support_margin`: Real, m, default 0, 0 to 1 (`:48`). `force_maximum_acceleration`: Real, m/s², default 0, 0 to 12 (`:49`). Lessons set them per precision knot; `first_principles` at precision 0 / 0.45 / 1: margin 0.3 / 0.174 / 0.02, speed 3 / 2.325 / 1.5, dwell 0.15 / 0.24 / 0.35, guide 12 / 6.6 / 0. |
| Ports, nodes, stores, joints, anchors | none (`:54`, `:80`). The goal is a typed objective, not a wire. |
| Events | `Captured` (`:64`), once per sensor and target. |
| Presentation bindings | Pick radius 0.85 m, no resize axes (`:25`). Four event bindings on `Visual/Visual_Halo`, each driven by `Captured`, smoothstep, 0.5 s, overlap Replace (`:27`–`30`): binding 0 `UniformScale` 1.02 to 1.02; bindings 1 to 3 `AlbedoRed`, `AlbedoGreen`, `AlbedoBlue` from (189, 244, 189)/255 to 1 (pale green `#bdf4bd` to white). No parameter binding (`:31`). |
| Catalogue | id `basket`, title Receiver, category Goals, palette order 2, colour (0.29, 0.67, 0.58) = Basket `#4aab94` (`DESIGN.md:229`), icon `res://elements/Receiver/icon.svg`, scene `res://elements/Receiver/basket.tscn` (`:34`–`41`). Any number may be placed; each keeps its own capture truth. |

At a glance: the only open-topped green box with a lower front lip and a pale ring floating above its rim.

## 3. Engine capabilities

Capability probe, 22 November 2026 (tests in `CuriousContraptions.tests/`; each name checked to exist). Counts: 8 present, 0 present but untested, 2 narrower, 1 missing.

| Capability | Status | Symbol | Test or owner |
| --- | --- | --- | --- |
| Static box contact | present | `WorkshopCollisionKernels.CollideBoxSphere` (`CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:349`) | `PayloadInstanceTests.AReceiverCapturesEitherKindAndAShiftedMissNever` |
| Residence sensor: speed limit, dwell, participation, margin, one latch | present | `WorkshopPhysicsEngine.CommitResidence` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:890`) | `WorkshopPhysicsEngineTests.DeclaredReceiverParticipationAndDwellDriveRealCapture`; `.DeclaredCaptureSpeedRejectsFastPassAtTheProductionSensor`; `.LeavingBeforeDwellDiscardsPartialResidenceAndReentryStartsAFullNewDwell`; `.DeclaredCaptureMarginWidensTheMouthWhileABesideDecoyStaysOutside` |
| Planar guide: descending, inside, supported by every collider child, bounded lateral acceleration | present | `WorkshopPhysicsEngine.ApplyGuides` (`WorkshopPhysicsEngine.cs:1029`; child support loop at `:1062`) | `WorkshopPhysicsEngineTests.DeclaredReceiverGuideActsOnlyForDescendingInsideTargetsAndDoesNotMoveItsFrame`; `IndependentResidenceGuideTests.Controls_SpeedLimit_InsufficientDwell_SupportGate_NonDescending` |
| Sensor and guide in an offset, rotated static frame | present | the same two methods | `IndependentResidenceGuideTests.TransformedFrames_OffsetAndRotatedSensorAndGuide_EvaluatedInLocalCoordinateSpace` |
| Several Receivers and payloads; nearest guide wins | present | sparse capture state | `CompoundMassAndRollingTests.DistantReceiversCaptureTheirOwnPayloadsWithSparseStateAndResetReplay`; `.CompetingGuidesChooseNearestThenAuthoredOwnerWithoutSummingAcceleration`; `.OneCapturePolicyKeepsTwoActualTargetsAcrossTheCommittedReadCodec` |
| Captured goal naming a body | present | `WorkshopGoalEvaluator` (`engine/gpu/WorkshopGoalEvaluator.cs:8`) | `CompoundMassAndRollingTests.CaptureTruthKeepsTheAuthoredPayloadThroughGrowthPlacementResetAndLoad`; `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` |
| Event-driven colour feedback, once, cleared by Reset | present | `PresentationDriver.Event` | `BasketballResourceTests.ReceiverHaloFollowsTheDeclaredCaptureBlendAndRestsOnItsNeutralPalette`; `WorkshopActivationAnimationTests.GoalFeedbackOwnsOccurrenceAcrossProjectionRetryAndReset` |
| Parameter-driven uniform scale of an art node (the halo repair needs it) | present | `PresentationParameterBinding` (`CuriousContraptions.Abstractions/ElementPresentation.cs:119`), used with `UniformScale` at `elements/Weight/WeightElementDeclaration.cs:20` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (Weight row sweeps mass) |
| Residence and guide on a **moving** frame body | narrower | Position is evaluated in the frame body's current pose (`WorkshopPhysicsEngine.cs:909`–`912`), but the speed limit reads world speed (`:933`), not speed relative to the frame. The guide has the same limit: its "descending" test rotates world velocity into the guide frame (`:1071`) without subtracting the frame's velocity. A ball riding a carrier faster than the limit can never be captured; a ball at rest in the world that a carrier sweeps over counts dwell. | `ENGINE-SENSOR-VOLUMES` (owner decision 3a), before EL-069, EL-070 or EL-062 |
| Residence sampled at each substep endpoint (requirement text; `docs/gpu-f32-physics.md:154`) | narrower | `CommitResidence` runs once per committed tick, after all substeps (`WorkshopPhysicsEngine.cs:348`, `:358`); it reads the tick's final pose and velocity, and counts dwell in substep ordinals. A body that leaves and returns between two commits is treated as continuously inside. No gameplay effect at the default limits (1.5 m/s × 1/120 s = 12.5 mm of travel); it matters only near the 64 m/s maximum limit or a dwell shorter than one tick. | `ENGINE-SENSOR-VOLUMES` (residence evaluated on the committed motion inside the tick) |
| Further goal kinds and several goals per level | missing | one goal per level today | [ENGINE-GOALS](../../../_bmad-output/planning-artifacts/epics.md#story-engine-goals-declared-goal-kinds-and-ordered-objectives) (`engine-goal-evaluation` and its four follow-ons, owner decision 12) |

Dependencies: a payload (Basketball, Bowling ball, Tennis ball, Baseball, Balloon or a ball-presets member).

### Engine conformance

Authority: the owner's engine-conformance instruction of 22 November 2026, `AGENTS.md` (element-free engine gate, Tri-Graph) and the [element-free engine design](../../../_bmad/custom/element-free-engine-design.md).

**Compiled counts per placed Receiver.** Bodies 1 (static). Colliders 5 (5 boxes). Joints 0. Drives 0. Sensors 2 (1 residence sensor, 1 planar guide region). Domain nodes 0. Stores 0. Ports 0. Events 1. Presentation bindings 4 (after the halo repair: 3 event, 1 parameter). All counts are fixed at compile time.

**Why five colliders.** Visible = solid: each of the five drawn walls is its own box, and the open top and the lowered front lip are the part's gameplay shape. Fewer boxes cannot make an open container.

**Art layers** (`basket.tscn:52`–`75`; classification as `VisibleSolidColliderTests.cs:143`–`144`).

| Art node | Mesh | Class | Collider | Moved by |
| --- | --- | --- | --- | --- |
| `Visual/Visual_Floor` | box 1.5 × 0.15 × 1.5 m | solid | `col_0_0` | nothing (static body) |
| `Visual/Visual_Left`, `Visual_Right` | boxes 0.12 × 1 × 1.6 m | solid | `col_0_1`, `col_0_2` | nothing |
| `Visual/Visual_Back` | box 1.5 × 1 × 0.12 m | solid | `col_0_3` | nothing |
| `Visual/Visual_Front` | box 1.5 × 0.6 × 0.12 m | solid | `col_0_4` | nothing |
| `Visual/Visual_Halo` | torus, inner 0.915 m, outer 0.965 m, at y 0.55 (2.5 cm clear above the rim), pale green `#bdf4bd` | decorative: a glow, and scaled and recoloured by presentation bindings | none; a ball passes through it | presentation bindings (scale, colour) |

Physics reads no art node. The bindings read committed events and an authored parameter and write nothing back.

**Behaviour as generic laws.**

| Behaviour | Generic law | Declared values |
| --- | --- | --- |
| The walls stop and hold a ball | rigid contact with pair restitution and Coulomb friction on static boxes | five boxes; restitution 0.12, friction 0.3, bounce threshold 0.1 m/s |
| A ball that settles inside is captured once | residence-sensor law: a target whose centre stays inside the declared local box at or below the speed limit for the dwell yields one `Captured` occurrence per sensor and target, held until Reset; leaving or exceeding the limit discards partial dwell | box (±0.66, −0.4 to 0.45 + margin, ±0.66) m; `capture_speed_limit`, `capture_dwell`, `capture_participation`, `capture_margin`; target all payloads |
| A descending ball above the mouth is nudged toward the centre | planar-guide law: a target that is inside the region, descending and supported above the support height receives a bounded lateral acceleration toward the region's axis; among eligible guides the nearest horizontal centre wins, then the authored owner | region (±1.1, 0.5 to 1.5, ±1.1) m; support height 0.5 m; `force_support_margin`; `force_maximum_acceleration` (0 by default, so no guide) |
| The level is solved | objective evaluation reads the `Captured` occurrence for the named body | the level's goal, not the package |
| The halo turns white on capture and shows the tolerance | event presentation binding; parameter presentation binding (after the repair) | `Captured`, 0.5 s smoothstep; `capture_margin` |

**Performance fitness.** The body is static, so it never integrates. The sensor and the guide are bounded boxes. Tunnelling through the 0.12 m walls is excluded by the engine's general means, the velocity-fattened broadphase and the speculative contact margin (`docs/gpu-f32-physics.md:210`–`213`, `:242`); the package adds no check of its own.

**Engine gaps recorded (owners in the engine programme, owner decision 3a).** Not element faults, and the package is not designed around them:

- sensors and guides are not frame-relative in velocity, and residence is sampled once per tick (the two narrower rows above); an element may declare only one residence sensor and one planar guide (audit E6). Owner: `ENGINE-SENSOR-VOLUMES`;
- `CommitResidence` scans every dynamic body for every sensor each tick (`WorkshopPhysicsEngine.cs:913`) and keeps pair state in a managed `ImmutableDictionary` builder (`:890`); `ApplyGuides` loops every dynamic body over every guide each substep (`:1032`) and walks every collider in the scene to find the target's children (`:1062`). That is per-tick work proportional to the scene and managed state inside the tick. Owners: `ENGINE-SPATIAL-QUERIES` (sensor and guide candidates from the region query of their bounded volume) and `ENGINE-TICK-ALLOCATION` (pair state in reusable flat tables);
- the residence and guide target `TargetSelector.AllPayloads` (`CuriousContraptions.Abstractions/TargetSelector.cs:8`) selects by the product payload role, the same finding as audit E12 for contact work. Owner: `ENGINE-CAPABILITY-INSTANCES` (a declared participation mask).

**Engine search.** No engine code names the package, its id or its class (audit of 22 November 2026: no id literal, no element-named file, no branch on a package). No residue is recorded against this spec.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Guide eligibility: at margin 0.3 and acceleration 12, a descending ball (vy −1) at local height 0.9 or 0.84 inside the rim is guided, also with the basket rotated 47°; it is not guided at margin 0 and height 0.835, below the guide region (0.49), above it (1.6), outside in depth (z 1.2) or ascending (vy +1). | `CuriousContraptions.tests/BasketGuideTests.cs@a6c914e:L17-L58` | carry forward as the eligibility control matrix. |
| 2 | Guide load: maximum acceleration as authored, minimum height 0.5, minimum support height 0.5 − margin; the vertical component of the change is zero and its magnitude ≤ acceleration × tick; basket pose and boxes never change; precision 1 installs no guide. | `CuriousContraptions.tests/BasketGuideTests.cs@a6c914e:L49-L65` | carry forward. |
| 3 | A 240 m/s pass through the whole guide window receives only the bounded lateral change its brief residence earns; the walls do not move; Reset restores the construction. | `CuriousContraptions.tests/BasketGuideTests.cs@a6c914e:L69-L101` | carry forward the behaviour; do not carry forward the analytic in-window time integral (replaced by substep-endpoint force evaluation, Story 2.4). |
| 4 | Guide clearance uses every collider child of the target body (an extra child protruding 0.6 m below suppresses guidance), not the presentation pose. | `CuriousContraptions.tests/BasketGuideTests.cs@a6c914e:L104-L132` | carry forward (matches the requirement's "all collider-child support heights"). |
| 5 | Residence is evaluated in the receiver's frame: a body co-moving with a translating or rotating frame dwells then captures; a non-co-moving body stays Outside; snapshot replay is exact. | `CuriousContraptions.tests/PhysicsResidenceSensorTests.cs@a6c914e:L28-L52` | carry forward as a requirement; not delivered today (the speed test is in the world frame, §3). Owner: `ENGINE-SENSOR-VOLUMES`. |
| 6 | Disabled participation (frame or body) resets residence to Outside with zero elapsed; capture then needs a full new dwell; once Captured, leaving does not un-capture. | `CuriousContraptions.tests/PhysicsResidenceSensorTests.cs@a6c914e:L53-L69` | carry forward. |
| 7 | High speed or leaving the region resets partial residence to zero; re-entry starts again. | `CuriousContraptions.tests/PhysicsResidenceSensorTests.cs@a6c914e:L70-L80` | carry forward. |
| 8 | Invalid residence declarations (self target, empty region, zero speed, NaN dwell, duplicates, unknown bodies) reject atomically. | `CuriousContraptions.tests/PhysicsResidenceSensorTests.cs@a6c914e:L81-L96` | carry forward (current `ResidenceSensorDeclaration.Validate`). |
| 9 | Physical dwell owns capture regardless of presentation edits (moving or hiding the basket artwork); the Captured event is published once and is not duplicated by further observation; Reset clears it. | `CuriousContraptions.tests/BasketResidenceOwnershipTests.cs@a6c914e:L17-L60` | carry forward; do not carry forward the per-part `ObservePhysics` loop (per-element update loop). |
| 10 | A decoy ball captured by the Receiver does not satisfy a goal naming another ball. | `CuriousContraptions.tests/BasicTests.cs@a6c914e:L236-L258` | carry forward. |
| 11 | In spring_forward, forgiving precision accepts a strictly larger set of receiver x-offsets (−1.6 to 1.6 in 0.1 steps) than precise, and contains every precise success. | `CuriousContraptions.tests/BasicTests.cs@a6c914e:L44-L70` | carry forward as an assistance acceptance fact. |
| 12 | Authoring knots: every instance owns its difficulty curve; capture margin 0.30 / 0.174, speed 3 / 2.325, dwell 0.15 / 0.24 at precision 0 / 0.45; guide acceleration 12 / 6.6 for `basket` only; precision 1 uses defaults. | `tools/Campaign/Program.cs@a6c914e:L540-L563` | carry forward the values; do not carry forward the string-keyed `part.Kind` switch (string-typed closed set). |
| 13 | first_principles guide probe fixtures: interior (0.5, 1.1), entry (0.5, 1.6), exit (0.5, 0.55), rotated π/6, ascending, and a re-entry case; puzzle cases at precision 0 / 0.45 / 1 with ramps at (−3.4, 4.3) and (−0.7, 2.7), both −20°. | `reference/p054-guide/Program.cs@a6c914e:L24-L52` | carry forward the fixture matrix; WGSL root diagnostics and the startup worker in `reference/p054-guide/` are probe artefacts, no element knowledge. |
| 14 | With each authored solution at precision 0, the Receiver captures the ball within 1,200 ticks in clear_pipe and quarter_bend at their authored positions, and in spring_forward with the Receiver moved 0.3 m in +x; moved 0.5 m in −x in spring_forward it does not capture. All three Receivers carry the precision-0 knots capture margin 0.3 and guide acceleration 12. | `CuriousContraptions.tests/FlightCampaignTests.cs@a6c914e:L15-L40`; `content/puzzles.json@a6c914e:L781-L796`; `content/puzzles.json@a6c914e:L7368-L7383`; `content/puzzles.json@a6c914e:L7792-L7807` | carry forward as level-regression boundaries of the Receiver's authored assistance region (content in `content/puzzles.json`, which is kept). |

Files harvested:
- `CuriousContraptions.tests/BasketGuideTests.cs`
- `CuriousContraptions.tests/PhysicsResidenceSensorTests.cs`
- `CuriousContraptions.tests/BasketResidenceOwnershipTests.cs`
- `CuriousContraptions.tests/BasicTests.cs`
- `CuriousContraptions.tests/FlightCampaignTests.cs`
- `tools/Campaign/Program.cs`
- `reference/p054-guide/` (Program.cs; puzzles-candidate.json, root-diagnostic.wgsl, startup-worker.js, inputs.json and csproj hold no further element knowledge)

Reading note for fact 14: the 1,200-tick figure is a historical stall guard, not an arrival-time requirement (game-fidelity policy).

## 5. Acceptance outline

Acceptance: [CAT-004](../requirements.md#current-cat-004) and [simulation and element acceptance](../requirements.md#accept-simulation). Every row runs the production simulation through defined interfaces and proves a consequence of a generic law for this declaration. Native proof is current; the Chrome recipes are pending post-catalogue.

| Given / When / Then | Test (exists unless marked planned) | Control and boundary |
| --- | --- | --- |
| A payload that stays inside below the speed limit for the dwell is captured once | `IndependentResidenceGuideTests.UnassistedResidenceSensor_DetectsDwellAndFiresCaptured_WithoutLateralAcceleration`; `WorkshopPhysicsEngineTests.DeclaredReceiverParticipationAndDwellDriveRealCapture` | disabled participation never captures; dwell is a deliberate timer, so its elapsed time is asserted |
| A fast through-pass never captures | `WorkshopPhysicsEngineTests.DeclaredCaptureSpeedRejectsFastPassAtTheProductionSensor` | speeds either side of the limit |
| Leaving before the dwell discards it; re-entry needs a full dwell | `WorkshopPhysicsEngineTests.LeavingBeforeDwellDiscardsPartialResidenceAndReentryStartsAFullNewDwell` | — |
| The margin widens the mouth; a ball beside the box stays outside | `WorkshopPhysicsEngineTests.DeclaredCaptureMarginWidensTheMouthWhileABesideDecoyStaysOutside` | margin values either side of the held height |
| The guide acts only on a descending, inside, supported payload and never moves the walls | `WorkshopPhysicsEngineTests.DeclaredReceiverGuideActsOnlyForDescendingInsideTargetsAndDoesNotMoveItsFrame`; `IndependentResidenceGuideTests.StandalonePlanarGuide_AppliesCentringAcceleration_WithoutProducingCapture` | ascending, below, above, outside in depth |
| Forgiving settings accept receiver offsets that precise settings refuse | `WorkshopPhysicsEngineTests.ForgivingSpringForwardReceiverOffsetsCaptureInTheShippedEngine` | offsets either side of the forgiving limit |
| Another payload cannot satisfy a goal naming a body; two Receivers keep their own captures | `CompoundMassAndRollingTests.CaptureTruthKeepsTheAuthoredPayloadThroughGrowthPlacementResetAndLoad`; `.DistantReceiversCaptureTheirOwnPayloadsWithSparseStateAndResetReplay` | reversed instance order |
| Capture happens on the physical commit, never on projection or cancel | `WorkshopSimulationTests.NativeCaptureOccursOnPhysicalCommitAndNeverOnProjectionOrCancellation` | — |
| Halo turns white once on capture and Reset restores the neutral colour | `BasketballResourceTests.ReceiverHaloFollowsTheDeclaredCaptureBlendAndRestsOnItsNeutralPalette`; `WorkshopActivationAnimationTests.GoalFeedbackOwnsOccurrenceAcrossProjectionRetryAndReset` | a non-finite cue is refused |
| Run/Reset and save/load restore settings and capture state | `IndependentResidenceGuideTests.Lifecycle_RunResetAndSaveLoadRoundtrip`; `WorkshopSaveTests.ReceiverForceRegionSurvivesSaveAndCompilesWithoutPuzzleSelection` | every precision knot (`WorkshopSaveTests.AValidReceiverSolutionMustMatchItsInventoryAtEveryPrecisionKnot`) |
| Invalid parameters and invalid sensor or guide declarations are refused atomically | `WorkshopSaveTests.InvalidDeclaredReceiverParameterRejectsAndValidSaveStillLoads`; `IndependentResidenceGuideTests.AtomicRefusal_InvalidBounds_InvalidFrames_InvalidTargets` | NaN and infinity |
| Wrong-kind connection | no port exists; generic under S4 row 5 | — |
| Lessons with a Receiver solve, and not with a cause missing | `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` (12 lessons) | 60, 120 and 240 Hz |

Not proven now:

- **Moving frame** (requirement: "in the basket's moving frame"; harvest fact 5). The delivered engine cannot pass it (§3, narrower). After `ENGINE-SENSOR-VOLUMES`, the first slice that carries a capture owner (EL-069, EL-070 or EL-062) adds `<Owner>Tests.APayloadRidingTheMovingOwnerDwellsAndIsCapturedAndABystanderIsNot`: a ball resting in a container that translates at 2 m/s (above the 1.5 m/s default limit) for the dwell is captured; a ball at rest in the world that the container sweeps over at the same speed is not. Stall guard 15 s of game time. The engine story carries the same pair on law scenes with a kinematic frame.
- **Halo follows the margin** (`DESIGN.md:337`). Planned `BasketballResourceTests.ReceiverHaloScaleFollowsTheDeclaredCaptureMargin`: halo scale is 1 + margin at margins 0, 0.02 and 0.3 (an authored mapping, so compared with the f32 value of the same expression), and the capture cue does not change it. Control: the default part rests at 1.02.
- Browser proof is pending post-catalogue; `9r-12-cat-004-receiver-suite` is in review at native scope.

Consistency with delivered parts: the sensed interior is 1.32 m wide and 0.85 m tall, so each of the four delivered balls fits (Basketball 0.70 m at its seam; Baseball 0.54 m at its band, the repair values of coordinator decision 36); the default speed limit 1.5 m/s is below a Bumper's 8 m/s target, so a bumped ball must settle before it counts, which is the intended lesson; the guide's 12 m/s² maximum is of the order of gravity and cannot fling a ball out.

## 6. Open questions

None open.

Resolved:

- **Moving frame (coordinator decision 1, 22 November 2026).** The capability is narrower, not "present, untested": position is evaluated in the frame body's current pose, the speed limit reads world speed. Owner: `ENGINE-SENSOR-VOLUMES` (owner decision 3a). Reason: the requirement names the moving frame, and a carried container would capture wrongly today. The Receiver package itself stays static, because no planned lesson moves a `basket`; the clause is proven by the engine story's law scenes and then by the first carried capture owner. The requirement clause is preserved, not waived.
- **Sensor cost (coordinator decision 1).** The per-sensor scan of every dynamic body and the managed pair state are engine performance gaps owned by `ENGINE-SPATIAL-QUERIES` and `ENGINE-TICK-ALLOCATION`. The package is not changed to work around it.
- **Halo follows the capture margin (standing delegation; audit M1 judged it sound).** Scale 1 + margin, as `DESIGN.md:337` and the catalogue description (`ReceiverElementDeclaration.cs:37`) say. Reason: the player needs to see the tolerance a difficulty tier gives. Repair: add one `PresentationParameterBinding("Visual/Visual_Halo", UniformScale, "capture_margin", 1, 1)` (multiplier then offset, `ElementPresentation.cs:119`) and delete the constant event-driven scale binding. At margin 1 the halo doubles (outer radius 1.93 m); it stays decorative (a glow).
- **Further goal kinds:** ENGINE-GOALS in five engine slices (owner, 21 November 2026; the fifth, `engine-goal-count-window-hold`, by owner decision 12, 22 November 2026).

Needs the owner: nothing.

## 7. Integration

- **Collaborators.** Delivered: Basketball, Bowling ball, Tennis ball, Baseball, Balloon, Ramp, Springboard, Bumper, Pipe, both bends, Funnel, Trampoline, Fan, Cannon. Not delivered: the carried capture owners (EL-062, EL-069, EL-070) and the Goal identities EL-120–EL-123.
- **Campaign.** 12 Playable lessons; first use 1–10 in the [element coverage ledger](../requirements.md#campaign-element-coverage).
- **Coverage binding.** Catalogue key `basket` in `docs/coverage/catalogue-elements.json`.
- **Lesson fixture.** None needed for the part (S8 not required); lessons set its six parameters per knot through authored content.

## 8. Delivery

- **Disposition.** Delivered. No slice is owed for the part.
- **Residual work, in order.** Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element.
  1. Halo-margin repair. Files: `elements/Receiver/ReceiverElementDeclaration.cs` (the binding list), the regenerated bundle, `CuriousContraptions.tests/BasketballResourceTests.cs` (the new test), and `CuriousContraptions.tests/LaunchLessonSolveTests.cs:211`–`212` (`AssertGoalCue`, declared at `:191`): deleting binding 0 moves the halo's red channel from binding 1 to binding 0, so the capture cue's selector and its comment change to binding 0. Data-only tier; after the programme.
  2. `ENGINE-SENSOR-VOLUMES` (engine tier, inside the programme), then the moving-frame proof inside the first carried capture owner's slice.
  3. Goal kinds under ENGINE-GOALS.
  4. Browser suite 9R.12, post-catalogue.
- **EL-192 planning check** (delivery-batches 2c: moving frame; a second Receiver while balls exist). The second-Receiver residual is delivered and tested (`DistantReceiversCaptureTheirOwnPayloadsWithSparseStateAndResetReplay`); EL-192 stays open only for the moving-frame row, which waits for the engine story. EL-216 Capture cradle (static geometry family) may follow this declaration shape and must take its own palette order.
- **Review tier.** Data-only for (1); all six layers for (2).
- **Disqualifiers.** `UniformScale` refusing a parameter binding on a node that also has event colour bindings; any other test that selects a Receiver binding by index (the slice's probe searches for `Binding ==` on a `basket` owner); a change to the sensor or guide boxes.
