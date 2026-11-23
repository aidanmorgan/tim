# CAT-002 · ball_detector (Ball detector) — declaration readiness spec

**Status (verified 22 November 2026 against `8d1d68aa`):** delivered as package `ball_detector` (`elements/BallDetector/`). The declaration is data, art and bindings only. Open: one visible-solid repair (the chevron stands 15 mm proud with no collider; it is reprinted flush), two residual tests, and an engine performance gap in the passage sensor (§3). Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026); the part's engine gaps are owned by `ENGINE-SENSOR-VOLUMES` (in-tick passage evaluation, moving frames), `ENGINE-SPATIAL-QUERIES` (candidates from the region query instead of every body), `ENGINE-TICK-ALLOCATION` (no per-tick allocation) and `ENGINE-CAPABILITY-INSTANCES` (mouth snapping as declared snap sockets, audit E5). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is pending until every named element passes. The `@a6c914e` citations in §4 record the harvest source, not current instructions.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-002 · `ball_detector` (display name Ball detector) |
| Requirement | [CAT-002](../requirements.md#current-cat-002) |
| Mapped identities | none directly. Related identities with their own specs: EL-181 Rising-edge detector, EL-183 Resettable counter (signal consumers). |
| Disposition | **Delivered.** Package id `ball_detector`, directory `elements/BallDetector/`. Records: `review-6-8-ball-detector.md`, `review-c-lamp-detector.md` (native Pass at `2065d3`); collar Tube published at `eacf561d`. |
| Roadmap | Story 6.8 (generic aperture sensor with directional rearm) |
| Levels | none in `content/puzzles.json`; Free workshop and counter or timer benches only |

## 2. Declaration

Delivered declaration: `elements/BallDetector/BallDetectorElementDeclaration.cs`; scene `elements/BallDetector/ball_detector.tscn`.

| Item | Value and source |
| --- | --- |
| Interfaces | `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). Declared at `BallDetectorElementDeclaration.cs:14`. Domains: Mechanical and Signal (`:51`). |
| Body | One static body (`KinematicMode.Static`, `:53`; footprint 1 body, 0 moving, 0 constraints, `:34`). Mass 0, moments 0 (`:57`–`63`). `PayloadPolicy.None` (`:55`). |
| Colliders | Four, on that body, as hidden `col_` nodes. `col_collar`: Tube along local X, outer radius 0.82 m, wall thickness 0.17 m (bore radius 0.65 m), length 0.56 m, centred at the origin (`ball_detector.tscn:20`–`28`, `:99`–`103`). `col_housing`: box, half extents (0.25, 0.12, 0.25) m at (0, 0.93, 0) (`:48`–`49`, `:105`–`109`). `col_socket`: sphere, radius 0.07 m at (0, 1.12, 0) (`:51`–`52`, `:111`–`115`). `col_indicator`: sphere, radius 0.075 m at (0, 0, 0.86) (`:54`–`55`, `:117`–`121`). |
| Material (f32) | restitution 0.15, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`:46`). Pair restitution is the product of the two materials, so a ball leaves the collar at 0.15 of its own restitution. |
| Parameters | none (`:32`); the editor shows no panel. The aperture is the collar's bore. |
| Port | One: `PortId.ActivationOut`, domain Signal, direction Output, role `ActivationOut`, signal kind Pulse, at (0, 1.12, 0), facing +Y (`:38`). No electrical port. |
| Tube mouths | Two scene locators, not signal ports: `Loc_Port_mouth_start` at (−0.28, 0, 0) facing −X and `Loc_Port_mouth_end` at (0.28, 0, 0) facing +X, each with `bore_radius` 0.65 m (`ball_detector.tscn:91`–`97`). They snap to the tube family's mouths. |
| Signal node | One: `SignalPassageDescriptor` (record at `CuriousContraptions.Abstractions/SignalContractValues.cs:54`), id `signal`, frame body 0, local position (0, 0, 0), identity rotation, aperture radius 0.65 m, rearm clearance 0.02 m (`:70`). Bound to `ActivationOut` as the node's output terminal (`:72`–`75`). No signal edges (`:71`). |
| Stores, joints, anchors | none (`:65`). |
| Events | `Passage` on node `signal` (`:49`), one per counted crossing. |
| Presentation bindings | Pick radius 1.15 m, no resize axes (`:16`). Three event bindings on `Visual/Visual_Indicator`, each driven by `Passage` on node `signal`, smoothstep, 0.35 s, overlap Replace (`:18`–`20`): `AlbedoRed` 85/255 to 247/255, `AlbedoGreen` 101/255 to 203/255, `AlbedoBlue` 115/255 to 82/255 (slate `#556573` to gold `#f7cb52`). Each crossing restarts them from slate. |
| Catalogue | id `ball_detector`, title Ball detector, category Control, palette order 16, colour (0.40, 0.72, 0.79), icon `res://elements/BallDetector/icon.svg` (ring-and-arrow pictogram), scene `res://elements/BallDetector/ball_detector.tscn` (`:23`–`32`). Form: `DESIGN.md:354`. |

At a glance: the only tube-family part with a cyan housing and an arrow on top of a short cream collar.

Stale text in the package: the class summary at `BallDetectorElementDeclaration.cs:10`–`13` still describes a 16-box ring; the scene has the Tube. The chevron slice corrects the comment.

## 3. Engine capabilities

Capability probe, 22 November 2026 (tests in `CuriousContraptions.tests/`; each name checked to exist). Counts: 7 present, 2 present but untested, 0 narrower, 0 missing.

| Capability | Status | Symbol | Test or owner |
| --- | --- | --- | --- |
| Declared passage aperture: one forward centre crossing fires once; reverse and outside fire nothing; rearm only after the whole body clears upstream | present | `WorkshopActivationCompiler.PassageSensorFor` (`engine/gpu/WorkshopActivationCompiler.cs:104`); `PassageSensorTracker.Advance` (`engine/gpu/PassageSensorDeclaration.cs:89`) | `LampDetectorGuardTests.OneBallFiresForwardNotInReverseAndFiresAgainAfterClearingUpstream`; `.AGlancingCrossingOutsideTheApertureFiresNothing`; `.AReverseCrossingThroughTheBoreFiresNothing`; `BallDetectorTests.PassageSensorTracker_JitterAcrossPlane_DoesNotRetriggerWithoutClearance`; `.PassageSensorTracker_NegativeControls_ReverseOutsideStationary` |
| Rotated, fast and multiple-body passes; off-grid placement; bore boundary | present | the same | `BallDetectorTests.PassageSensorTracker_PositiveForwardPass_RotatedAndVariousSpeeds`; `.PassageSensorTracker_MultipleBodies_TwoSeparatedBalls`; `DeclaredIndicatorPassageTests.AnOffGridDetectorCountsACrossingAtItsPlacedPose`; `.ACrossingJustInsideTheBoreCountsAndJustOutsideDoesNot` |
| Pulse output into the signal network; save and load of the wired bench | present | `ActivationNodeKind.PassageSource` (`CuriousContraptions.Abstractions/ActivationValues.cs:18`) | `DeclaredIndicatorPassageTests.TheLampAndDetectorCompileTheNodesTheirDeclarationsSelect`; `.TheSavedAndLoadedWiredBenchPassesTwice`; `EventRingProducerTests.TwoInRunPassesAfterClearancePublishPassageCountOneThenTwoWhileTheFirstInputLampStaysAtOne` |
| Reset restores the passage cursor, pulse and count | present | the same | `DeclaredIndicatorPassageTests.ResetRestoresThePassageCursorPulseLatchAndCountInOneSimulation` |
| Hollow Tube collar with box and sphere colliders; a strike on the outer collar fires nothing | present | `ColliderShapeKind.Tube` (hollow kernels, `CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:282`) | `BallDetectorTests.BallDetectorScene_ExtractsItsCollarTubeHousingSocketIndicatorAndThreePortLocators`; `LampDetectorGuardTests.ABallStrikingTheOuterCollarFiresNothing`; `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (row `ball_detector`) |
| Typed mouths (0.65 m) snapping to tube parts | present | scene mouth locators | `TubeMouthSnapTests.APipeSnapsOntoTheEndMouthOfEveryTubePart` |
| Event-driven indicator pulse restarted at each record's own physical time | present | `PresentationDriver.Event` | `EventRingConsumerTests.EachRecordRestartsItsOwnersClipAtItsPhysicalTime` and `.WithoutItsRecordTheClipNeverLeavesRest` (scenario `detector`); `EventRingProducerTests.AGlancingOrReverseCrossingPublishesNoPassage` |
| A counted crossing leaves the ball's momentum unchanged | present, untested | the sensor reads poses and applies no load (`PassageSensorDeclaration.cs:101`–`148`) | no test compares against a no-detector control; planned in §5 |
| A disabled participant is not counted | present, untested | a body with no enabled collider is not a participant (`PassageSensorDeclaration.cs:59`, `:76`) | no native test found by name; planned in §5 |

Dependencies: the tube family's hollow collider and mouth snapping (CAT-048); an activation target (Signal lamp, Counter, Hold timer).

### Engine conformance

Authority: the owner's engine-conformance instruction of 22 November 2026, `AGENTS.md` (element-free engine gate, Tri-Graph) and the [element-free engine design](../../../_bmad/custom/element-free-engine-design.md).

**Compiled counts per placed detector.** Bodies 1 (static). Colliders 4 (1 tube, 1 box, 2 spheres). Joints 0. Drives 0. Sensors 1 (the passage aperture). Domain nodes 1 (signal; it is the passage node). Stores 0. Ports 1 signal, plus 2 mouth locators. Events 1. Presentation bindings 3. All counts are fixed at compile time.

**Why four colliders.** Visible = solid: the collar is a true hollow Tube; the housing, the gold socket and the indicator are each drawn proud and collide as drawn (coordinator decision, 25 October 2026). None exists for a cosmetic purpose alone.

**Art layers** (`ball_detector.tscn:61`–`86`; classification as `VisibleSolidColliderTests.cs:75`–`77`).

| Art node | Mesh | Class | Collider | Moved by |
| --- | --- | --- | --- | --- |
| `Visual/Visual_Collar` | open cylinder, radius 0.82 m, 0.56 m long, axis X, no end caps, cream `#fff8e9` | hollow | `col_collar` (Tube, bore radius 0.65 m) | nothing (static body) |
| `Visual/Visual_Housing` | box 0.5 × 0.24 × 0.5 m at y 0.93, cyan `#66b8c9` | solid | `col_housing` | nothing |
| `Visual/Visual_Chevron/Arm_1`, `Arm_2` | boxes 0.34 × 0.02 × 0.022 m centred at y 1.055, navy | **defect today:** 15 mm proud of the housing top (y 1.05) with no collider, while the test table classes them printed. After the repair: decorative, printed flat | none | nothing |
| `Visual/Visual_Socket` | sphere, radius 0.07 m at (0, 1.12, 0), gold | solid | `col_socket` | nothing |
| `Visual/Visual_Indicator` | sphere, radius 0.075 m at (0, 0, 0.86), slate | solid | `col_indicator` | nothing; its colour is set by the three event bindings, its pose is not |

Physics reads no art node. The bindings read committed `Passage` records and write nothing back. Art judgement not verified here: the collar mesh is one surface at radius 0.82 m with no end annuli, so the 0.17 m wall thickness the collider has is not drawn at the mouths.

**Behaviour as generic laws.**

| Behaviour | Generic law | Declared values |
| --- | --- | --- |
| A ball smaller than the bore passes; anything else is stopped by the collar or the housing | rigid contact with pair restitution and Coulomb friction on a hollow Tube, a box and two spheres | bore radius 0.65 m, outer 0.82 m, length 0.56 m; restitution 0.15, friction 0.3 |
| One forward pass sends one trigger | passage-sensor law: an armed body whose centre crosses the aperture plane in the +X direction, inside the aperture radius, yields one `Passage` occurrence and disarms; the body rearms once its whole collider is upstream by the clearance. The crossing is tested on the segment between consecutive substep samples, so a fast body cannot skip the plane. The sensor applies no load | aperture radius 0.65 m, rearm clearance 0.02 m, plane at the body origin |
| The trigger reaches the wired part | signal-network rule: a pulse output delivers each occurrence to the inputs wired to it | port `ActivationOut`, kind Pulse |
| The indicator pulses gold | event presentation binding | `Passage`, 0.35 s smoothstep, slate to gold |

**Performance fitness.** The body is static. The aperture is a bounded disc. Tunnelling through the collar wall is excluded by the engine's general means, the velocity-fattened broadphase and the speculative contact margin (`docs/gpu-f32-physics.md:192`–`195`, `:224`); a missed count at speed is excluded by the sensor's segment test. The package adds no check of its own.

**Engine gaps recorded (opened and confirmed; owners in the engine programme, owner decision 3a).** `PassageSensorTracker.Advance` tests every sensor against every dynamic participant at every substep each tick (`PassageSensorDeclaration.cs:101`–`114`) and allocates a dictionary, lists and arrays on every tick (`:96`–`99`, `:186`); the tracker is managed state replaced each tick, not a compiled flat table. That is per-tick work proportional to the scene. `ENGINE-SENSOR-VOLUMES` moves passage evaluation into the physics tick against collider shapes and deletes the host-side tracker; its candidates come from `ENGINE-SPATIAL-QUERIES`' region query, not every dynamic body; `ENGINE-TICK-ALLOCATION` keeps pair state in reusable flat tables with no allocation in the tick. The frame defect of the residence sensor does not occur here: a passage frame must be static (`:42`) and the sensor has no speed limit, so a moving passage frame is refused, not mis-evaluated. Moving passage frames belong to `ENGINE-SENSOR-VOLUMES`.

**Engine search.** No engine code names the package, its id or its class (audit of 22 November 2026). One refactor is owed by a shared contract the part uses, not by the part: its two mouths are the tube family's fixed `TubeEnd { Start, End }` recipe with engine snap constants (audit E5); `ENGINE-CAPABILITY-INSTANCES` makes them a declared list of snap sockets.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | A forward pass at 4, 40 or 120 m/s (also with the detector rotated 20/30/40° and −90° about Z) records exactly one crossing, starts the connected hold timer, leaves the ball speed unchanged (± 0.002 m/s) and the ball beyond local x 1.9; Reset clears count, pulse, Active and the timer, keeping the connection. | `CuriousContraptions.tests/BallDetectorTests.cs@a6c914e:L34-L66` | carry forward. |
| 2 | Reverse motion (from x 2 at −4 m/s), a pass outside the aperture (y + 1.4) and a stationary ball record no crossing. | `CuriousContraptions.tests/BallDetectorTests.cs@a6c914e:L67-L84` | carry forward. |
| 3 | Jitter across the plane (twenty ±0.02 m oscillations) after a crossing does not retrigger; moving fully upstream (x −1) and forward again triggers the second crossing; the pulse decays to 0 and Active clears. | `CuriousContraptions.tests/BallDetectorTests.cs@a6c914e:L85-L118` | carry forward (whole-ball upstream clearance). |
| 4 | Two separated balls produce two events; a hidden (non-participating) ball never arms. | `CuriousContraptions.tests/BallDetectorTests.cs@a6c914e:L119-L135` | carry forward (multiple bodies, disabled participants). |
| 5 | Crossing detection belongs to the physics world, not scene callbacks: moving or hiding artwork does not matter; snapshot restore replays bodies, passage states and events exactly; repeated observation does not duplicate events. | `CuriousContraptions.tests/BallDetectorTests.cs@a6c914e:L137-L173` | carry forward; do not carry forward the per-part `ObservePhysics` loop (per-element update loop). |
| 6 | Sphere, box, rounded hull and compound bodies crossing fixed, translating and rotating frames produce one Passed event at the plane (time 0.25 or 1/3), with the body centre on the plane and velocity unchanged; replay is exact. | `CuriousContraptions.tests/PhysicsPassageSensorTests.cs@a6c914e:L42-L65` | carry forward for fixed and rotated static frames. Translating and rotating frames are not delivered: a passage frame must be static today (`engine/gpu/PassageSensorDeclaration.cs:42`). No requirement of this part needs one; owner if one does: `ENGINE-SENSOR-VOLUMES`. |
| 7 | A curved path that crosses and returns within one step is still detected; outside-aperture crossings publish OutsideAperture and disarm without counting. | `CuriousContraptions.tests/PhysicsPassageSensorTests.cs@a6c914e:L84-L107` | carry forward the behaviour; do not carry forward the 100-step root-finding sweep (sensor root-finding removed by Story 3.1; use substep-endpoint sampling). |
| 8 | A disabled frame or body cannot arm; after re-enabling, a crossing counts only after physical upstream clearance. A geometry change invalidates arming; a compound wider than the aperture crosses as OutsideAperture. | `CuriousContraptions.tests/PhysicsPassageSensorTests.cs@a6c914e:L109-L144` | carry forward. |
| 9 | Invalid declarations (non-positive or NaN radius or clearance, unknown bodies) reject atomically; a failed step restores counts, events, bodies and clock. | `CuriousContraptions.tests/PhysicsPassageSensorTests.cs@a6c914e:L146-L179` | carry forward the atomic rejection. |
| 10 | Detector → counter (target 3) → lamp: two or three forward crossings count exactly; the lamp lights only at three; entity order does not change replay; Reset clears both counts. | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L89-L152` | carry forward as an integration (counter owned by CAT-020). |
| 11 | With a rotated detector and a ball at 10 m/s, crossing uses solved positions and collider participation: disabled participation clears pulse and Active and blocks counting. | `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs@a6c914e:L421-L458` | carry forward. |
| 12 | A failed tick restores the observation cursor (count 0, pulse 0, not Active) and the retried tick delivers the crossing and its activation exactly once. | `CuriousContraptions.tests/ImpactRuntimeCheckpointTests.cs@a6c914e:L108-L142` | carry forward the exact-restore rule. |

Files harvested:
- `parts/BallDetectorPart.cs`
- `parts/scenes/ball_detector.tscn` (script binding only)
- `parts/catalog/ball_detector.tres`
- `reference/cpu/MachinePart.cs` (AddBox half-size rule)
- `engine/physics/PhysicsPassageSensor.cs`
- `engine/ScenePassageSensorDeclaration.cs`
- `CuriousContraptions.tests/BallDetectorTests.cs`
- `CuriousContraptions.tests/PhysicsPassageSensorTests.cs`
- `CuriousContraptions.tests/CounterTests.cs`
- `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs`
- `CuriousContraptions.tests/ImpactRuntimeCheckpointTests.cs`

## 5. Acceptance outline

Acceptance: [CAT-002](../requirements.md#current-cat-002) and [retained behaviour](../requirements.md#todo-264). Every row runs the production simulation through defined interfaces and proves a consequence of a generic law for this declaration. Chrome proof (Story 9R.9, `tools/e2e/cat-002.test.ts`) is pending post-catalogue.

| Given / When / Then | Test (exists unless marked planned) | Control and boundary |
| --- | --- | --- |
| One forward centre crossing lights the wired lamp once | `LampDetectorGuardTests.OneBallFiresForwardNotInReverseAndFiresAgainAfterClearingUpstream` | reverse fires nothing; a second crossing counts only after whole-ball upstream clearance |
| Outside, reverse, stationary and jitter crossings fire nothing | `LampDetectorGuardTests.AGlancingCrossingOutsideTheApertureFiresNothing`; `.AReverseCrossingThroughTheBoreFiresNothing`; `BallDetectorTests.PassageSensorTracker_NegativeControls_ReverseOutsideStationary`; `.PassageSensorTracker_JitterAcrossPlane_DoesNotRetriggerWithoutClearance` | just inside and just outside the bore (`DeclaredIndicatorPassageTests.ACrossingJustInsideTheBoreCountsAndJustOutsideDoesNot`) |
| Rotated, fast and multiple-body passes each count exactly | `BallDetectorTests.PassageSensorTracker_PositiveForwardPass_RotatedAndVariousSpeeds`; `.PassageSensorTracker_MultipleBodies_TwoSeparatedBalls` | off-grid placement (`DeclaredIndicatorPassageTests.AnOffGridDetectorCountsACrossingAtItsPlacedPose`) |
| A second in-run crossing publishes `Passage` count 2 and a first-input lamp stays at one | `EventRingProducerTests.TwoInRunPassesAfterClearancePublishPassageCountOneThenTwoWhileTheFirstInputLampStaysAtOne` | glancing or reverse publishes nothing (`.AGlancingOrReverseCrossingPublishesNoPassage`) |
| The indicator pulses from the committed crossing only | `EventRingConsumerTests.EachRecordRestartsItsOwnersClipAtItsPhysicalTime` (scenario `detector`) | without its record the clip stays at rest (`.WithoutItsRecordTheClipNeverLeavesRest`) |
| A strike on the collar is a contact, not a crossing | `LampDetectorGuardTests.ABallStrikingTheOuterCollarFiresNothing` | — |
| Reset clears the cursor, pulse and count; the wired bench survives save and load | `DeclaredIndicatorPassageTests.ResetRestoresThePassageCursorPulseLatchAndCountInOneSimulation`; `.TheSavedAndLoadedWiredBenchPassesTwice` | — |
| Mouths snap to tube parts | `TubeMouthSnapTests.APipeSnapsOntoTheEndMouthOfEveryTubePart` | — |
| Detector to Counter to lamp counts exactly | `CounterTests.ActivationNetwork_CountingProgression_SingleEmission_AndSaturation` (counter law); the wired detector bench above | — |
| Wrong-kind connection and parameter refusal | generic under S4 rows 5 and 2 (the part has no parameter) | — |
| A counted crossing leaves the ball's velocity unchanged | **planned** `LampDetectorGuardTests.ACountedCrossingLeavesTheBallsVelocityUnchanged`: the same drop through the bore with and without the detector; after the crossing the two velocities agree within 0.01 m/s (far below any speed a lesson depends on; a sensor that braked the ball would differ by a contact impulse) | a ball that clips the collar does differ |
| A disabled body crossing the aperture fires nothing | **planned** `LampDetectorGuardTests.ADisabledBodyCrossingTheApertureFiresNothing`: count exactly 0 | the same body enabled fires once |

Consistency with delivered parts: the 0.65 m aperture equals the tube family's bore radius, so each of the four delivered balls passes (largest: Basketball, 0.35 m at its seam; Baseball 0.27 m at its band, the repair values of coordinator decision 36); the output is a Signal-domain pulse and matches the Signal inputs of the lamp, Counter, Hold timer and State latch; it supplies no electricity.

## 6. Open questions

None open.

Resolved:

- **Chevron (standing delegation; coordinator decision 9; audit judged it sound).** `Visual/Visual_Chevron/Arm_1` and `Arm_2` span y 1.045 to 1.065 m over a housing top at 1.05 m (`ball_detector.tscn:34`–`36`, `:69`–`78`). They are reprinted flush: two planar strokes 0.34 × 0.022 m on the housing top plane, same positions and yaw, separated from the face by at most 0.5 mm as `face-markings-flush` did for the control labels. Reason: visible = solid leaves only flush or collider, and a 15 mm ridge on the housing top has no gameplay use.
- **Passage sensor cost (coordinator decision 1).** The pattern is shared with the residence sensor; it was opened here and confirmed (§3). Owners: `ENGINE-SENSOR-VOLUMES`, `ENGINE-SPATIAL-QUERIES` and `ENGINE-TICK-ALLOCATION` (owner decision 3a).
- **Balls (coordinator decision 7).** Four balls are delivered; the Baseball passes the bore.

Needs the owner: nothing.

## 7. Integration

- **Collaborators.** Signal lamp (`ActivationIn`), Counter, Hold timer, State latch (signal consumers); Clear pipe, bends, Funnel outlet, Powered gate (mouths); the four delivered balls (Basketball, Bowling ball, Tennis ball, Baseball). All delivered.
- **Campaign.** No level in `content/puzzles.json` places it; EL-241 Ball hopper (delivery-batches, selective passage) names it as a collaborator.
- **Coverage binding.** Catalogue key `ball_detector` in `docs/coverage/catalogue-elements.json`. A level that offers it from the player's inventory needs no lesson-fixture story; a level that locks it as a fixture adds a `FIX-` row to the catalogue fixture table ([current consumers](../invest/current-consumers.md), read by `tools/Coverage/CurrentCatalogue.cs:100`), and needs [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) only for per-parameter or connection locks.

## 8. Delivery

- **Disposition.** Delivered. No slice is owed for the part.
- **Residual work, in order.** Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element.
  1. Chevron flush repair, with the next visible-solid repair slice after the programme. Files: `elements/BallDetector/ball_detector.tscn` (the two arms), `CuriousContraptions.tests/FaceMarkingTests.cs` (add the package), the class summary at `BallDetectorElementDeclaration.cs:10`–`13`; the `VisibleSolidColliderTests` row stays as it is and becomes true. Element tier.
  2. The two planned tests, in the same slice or with Story 9R.9's remaining native rows.
  3. `ENGINE-SENSOR-VOLUMES`, `ENGINE-SPATIAL-QUERIES` and `ENGINE-TICK-ALLOCATION` (engine tier, inside the programme) for the sensor cost; no change to this package.
  4. Chrome proof (Story 9R.9), post-catalogue.
- **Disqualifiers.** Either planned test failing through the delivered package; a change to the 0.65 m aperture, which equals the family bore; a flush stroke that the face-marking test measures more than 0.5 mm from the housing top.
