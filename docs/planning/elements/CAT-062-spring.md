# CAT-062 · spring (Springboard) — declaration readiness spec

**Status (22 November 2026).** Delivered: package `elements/Springboard`, id `spring` (Story 6.4 published at `781e0c81`, preload and `spring_forward` by Story 6.5, generic conversion by C-Bumper+Spring). Sections 2 and 3 are read from the package and its tests in this checkout (`path:line`; tests are in `CuriousContraptions.tests/`). One visible-solid defect is open (the plate stripe, section 3) with a decided repair. Every residual slice, the EL-194 closure review included, is blocked on the engine programme (owner decision 3a, 22 November 2026); the part's engine gaps are owned by `ENGINE-ISLANDS-SLEEP` (the resting plate is still stepped) and `ENGINE-CAPABILITY-INSTANCES` (the coil's span binding is applied along hard-wired presentation axes, audit E7); the composite lanes wait for `ENGINE-GOALS`; the typed launch output of owner decision 7 (section 2) waits for `ENGINE-SENSOR-VOLUMES` (passage sensing inside the physics tick, relative to the frame). Browser qualification is pending until the post-catalogue E2E phase. Citations of the form `path@a6c914e` in section 4 are the legacy harvest, not current instructions.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-062 · `spring` (display name Springboard) |
| Requirement | [CAT-062](../requirements.md#current-cat-062); [retained behaviour](../requirements.md#todo-461); [Springboard elastic contract](../../springboard-elastic-contract.md) |
| Mapped identities | EL-194 Springboard ([EL-194 spec](EL-194-springboard.md)). Related: TH-33 Spring-mounted match uses a generic spring assembly and has its own spec |
| Disposition | Delivered |
| EL-194 | A refinement with new acceptance rows, not a duplicate closure (owner decision 7, 22 November 2026): EL-194 gets its own typed signal output that fires on launch, a declared sensor and port on general capabilities, specified in EL-194's spec. Preload and `spring_forward` are delivered. The part-level check runs in the "Launch parts" closure review after the engine programme (owner decision 3a); the identity stays open until its launch-signal rows, `spring_signal` and the fifteen composite lanes pass (the lanes after ENGINE-GOALS) |
| Levels | placed in `ready_to_rebound`; inventory in `spring_forward` and 15 others; see [CAT-062-I consumers](../invest/current-consumers.md#cat-062-i) |

## 2. Declaration

Read from `elements/Springboard/SpringboardElementDeclaration.cs` (cited as `:line`) and `elements/Springboard/springboard.tscn` (cited as `tscn:line`).

**Interfaces** (`:11`): `ISliderElement`, `ISceneGeometryElement`. Domain Mechanical only (`:47`). The package's own kinematic mode is Static (`:49`); footprint two bodies, one moving, one prismatic constraint (`:32`).

**Catalogue** (`:16-24`): id `spring`, category Motion, palette order 12, colour (0.96, 0.7, 0.33), display name "Springboard".

**Bodies, colliders, mass and material.**

| Body | Kind | Collider | Shape and size (m) | Centre (m) | Mass and moments | Source |
| --- | --- | --- | --- | --- | --- | --- |
| 0, base | static | `col_0_0` | Box 1.3 × 0.12 × 1.2 | (0, −0.36, 0) | none | `tscn:35-36`, `:78-82` |
| 1, plate | dynamic | `col_1_0` | Box 1.3 × 0.15 × 1.2 | plate origin, resting at (0, 0.14, 0) | 0.25 kg; homogeneous box moments (0.0305, 0.0652, 0.0357) kg·m² about x, y, z; no drag; gravity (0, −9.81, 0) m/s² | `tscn:38-39`, `:49-50`, `:84-88`; `:74-80` |

Both bodies (`:41-42`): restitution 0, friction 0.1, rolling resistance 0, bounce threshold 0.05 m/s. The spring returns the energy; contact restitution returns none.

**Slider** (`:63-82`). One `PrismaticSliderDescriptor` along the plate's local Y. Base frame at (0, 0.14, 0); travel −0.25 m to 0; stiffness, damping and starting compression from the parameters; plate and base do not collide with each other; no drive.

**Parameters** (`:27-29`). All Real, editor policy Reject (the default, `CuriousContraptions.Abstractions/ParameterDescriptor.cs:60`), BuildOnly (`:61`).

| Name | Unit | Default | Minimum | Maximum |
| --- | --- | --- | --- | --- |
| `stiffness` | N/m | 400 | 120 | 1200 |
| `damping` | N·s/m | 0.2 | 0 | 8 |
| `initial_compression` | m | 0 | 0 | 0.2 |

**Ports, stores, domain nodes, events:** none today (`:34`, `:45`).

**Launch output (planned, owner decision 7, 22 November 2026; this is the single declaration, to which [EL-194](EL-194-springboard.md) points, coordinator decision 27).** One launch sensor: a `SignalPassageDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:54`) on body 0 (the static base), an aperture of radius 0.65 m (the plate's half length) centred over the plate and 0.8 m above the plate's top face at rest (**proposed**: above the centre of a resting Basketball, 0.34 m, so a ball at rest never crosses it), facing along the plate's travel axis, forward away from the plate, rearm clearance 0.02 m (the Ball detector's), with one `SimulationEventKind.Passage` event declaration on its node. One port `ActivationOut`: Signal domain, Output, Pulse, bound to the sensor's output terminal, socket on the base's +X end face, as the Ball detector binds its passage node (`elements/BallDetector/BallDetectorElementDeclaration.cs:72`–`:74`). A body whose centre crosses the plane forward publishes one Pulse; a body crossing toward the plate publishes nothing. Both are general capabilities any package declares; nothing reads contact on the plate.

The elastic energy ½ · k · q² lives in the slider coordinate, not in a declared store: 8 J at 400 N/m and 0.2 m.

**Presentation** (`:13-14`). Pick radius 0.9 m. No property binding. One body binding: `Visual/Plate` follows body 1. One span binding: `Visual/Coil` is scaled along Y to span from the plate's underside anchor (0, −0.075, 0) to the base anchor (0, −0.3, 0), rest length 0.365 m.

**Numbers.** The 8 J maximum preload lifts the plate plus a Basketball (1.25 kg in all) about 0.65 m, a Tennis ball about 1.36 m, the Baseball (0.15 kg, the repair values of coordinator decision 36; 0.40 kg with the plate) about 2.04 m and a Bowling ball about 0.19 m. The Springboard launches light balls and only nudges the heavy one, which matches the lessons.

## 3. Engine capabilities

Capability probe, 22 November 2026, with the launch-output rows of owner decision 7 added on 23 November 2026. Counts: 6 present, 2 present but untested, 0 narrower, 2 missing.

| Capability | Status | Symbol | Test or owner |
| --- | --- | --- | --- |
| Prismatic slider with travel stops and a soft spring-damper taking its values from typed parameters | present | `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), `PrismaticSliderDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:104`), `PrismaticRows` (`engine/gpu/PrismaticRows.cs:12`), `SoftConstraint` (`engine/gpu/SoftConstraint.cs:43`), `PrismaticConstraintSolver` (`CuriousContraptions.Simulation/PrismaticConstraintSolver.cs:65`) | `SliderInstanceTests.ThePlateHoldsItsLowerAndUpperStops`; `WorkshopPhysicsLawPrismaticTests.DeclaredDampingRemovesEnergyComparedWithTheSameStopFreeSpring`, `.PreservedPassiveBallContactsAlongEachSourceOrientation`, `.TheConnectedCollisionPolicyFiltersOnlyItsDeclaredPair`; `WorkshopSpringboardTests.CompilesDistinctPlateIdentityAndFullDynamicPopulation` |
| Starting compression as finite stored elastic energy, with no launch when uncharged or missed | present | the slider's `Compression` value (`elements/Springboard/SpringboardElementDeclaration.cs:72`) | `SliderInstanceTests.APreloadedBoardLaunchesARestingBallAndAnUnchargedOneDoesNot`, `.AMissedBallGetsNothingFromAChargedBoard`; `WorkshopSpringboardTests.CompiledPreloadUsesActualF32PoseAndEightJoules`; `WorkshopPhysicsLawPrismaticTests.PreloadTransfersOnlyFiniteStoredWorkToContactingPayloadsInUprightAndTiltedFrames` |
| Parameter refusal, save, Reset; an undeclared parameter (the removed `strength`) is refused | present | generic instance parameters | `WorkshopSpringboardTests.InvalidPreloadRejectsResourceAndSaveBeforeAcceptedStateChanges`, `.CurrentSaveAndWirePreserveF32Settings`; `SliderInstanceTests.RunResetAndSaveLoadAreExact`; `PalettePlacementTests.GenericSpringboardEditorRefusesOutOfRangeValuesAtomicallyAndAcceptsEndpoints`; `PreConversionLaunchSaveTests.ACurrentRecordCarryingATypedFieldOrAWrongParameterSetIsRefusedAtBothBoundaries` |
| An art node follows a moving body's pose | present | `PresentationBodyBinding` (`CuriousContraptions.Abstractions/ElementPresentation.cs:114`) | `SliderInstanceTests.ThePlateVisualRestsAtTheDeclaredCompressionAndEqualsItsCompiledBody` |
| A span binding scales an art node between a moving and a fixed anchor (the coil) | present, untested | `PresentationSpanBinding` (`ElementPresentation.cs:115`), applied at `engine/MachinePart.cs:232` | none asserts the coil's scale. Owner: the flush-stripe repair slice adds `SliderInstanceTests.TheCoilSpansFromTheBaseToThePlateAtTheDeclaredCompression` (section 5) |
| `spring_forward` and `ready_to_rebound` solved, unsolved without their cause | present | lesson content in `content/puzzles.json` | `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal`, `.EachLaunchRouteSolvesOrStaysUnsolvedAndResetRestoresTheBench` (routes SpringPassive, SpringCharged, SpringZero, SpringAway, ReadyReceiver, ReadyMissingReceiver); `WorkshopPhysicsEngineTests.ForgivingSpringForwardReceiverOffsetsCaptureInTheShippedEngine`. `WorkshopSpringboardTests.LaunchLessonsPreserveSourceFixturesAssistanceAndNamedCapture` compares authored content and plays nothing |
| `spring_signal` (the Springboard throws a ball onto an Impact switch wired to a Signal lamp) | present, untested | delivered parts only; the level is Planned | Early level proof batch 1, "Signals" group (`_bmad-output/implementation-artifacts/spec-early-level-proof-batch-1.md`) |
| Levels with two to five goals (the fifteen composite spring lanes) | missing | the engine evaluates one goal per level | ENGINE-GOALS; each level's own proof follows it |
| A passage sensor on a static frame body publishing a Pulse to a typed `ActivationOut` port (the launch output, owner decision 7) | present (host-side, after the tick) | `SignalPassageDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:54`); the Ball detector (`elements/BallDetector/BallDetectorElementDeclaration.cs:70`–`:74`) | `BallDetectorTests.PassageSensorTracker_PositiveForwardPass_RotatedAndVariousSpeeds` (`CuriousContraptions.tests/BallDetectorTests.cs:152`), `PassageSensorTracker_NegativeControls_ReverseOutsideStationary` (`:257`) |
| Passage sensing inside the physics tick, relative to the sensor's frame | missing | today it runs on the host after the tick (`engine/gpu/PassageSensorDeclaration.cs:80`–`:112`) | `ENGINE-SENSOR-VOLUMES` (owner decision 3a) |

### Engine conformance

**Compiled counts.** Bodies 2 (1 static, 1 dynamic). Colliders 2 boxes. Joints 1 prismatic, with a soft spring-damper and two travel limits. Drives 0. Sensors 0 today, 1 passage sensor planned (owner decision 7). Signal nodes 0 today, 1 planned (the sensor's passage node). Ports 0 today, 1 planned (`ActivationOut`). Stores 0. Presentation: 0 property bindings, 1 body binding, 1 span binding. All counts are fixed at compile time.

**Art and layers.**

| Art node | Mesh (m) | Classification | Moved by |
| --- | --- | --- | --- |
| `Visual/Visual_Base` | box 1.3 × 0.12 × 1.2 at y −0.36, navy (`tscn:16-18`, `:45-47`) | solid, collider `col_0_0` | nothing |
| `Visual/Plate/Visual_Plate` | box 1.3 × 0.15 × 1.2, gold (`tscn:20-22`, `:52-53`) | solid, collider `col_1_0` | body pose (body 1) |
| `Visual/Plate/Visual_Stripe` | box 0.4 × 0.008 × 0.025, centred 0.079 above the plate centre, navy (`tscn:24-26`, `:55-57`) | **defect:** it spans 0.075 to 0.083 m over a plate top at 0.075 m, so it stands 8 mm proud with no collider, while the test table calls it printed (`VisibleSolidColliderTests.cs:148-149`). Repair below | body pose (child of the plate) |
| `Visual/Coil/Ring_0` to `Ring_3` | four tori, inner 0.17, outer 0.22, silver (`tscn:28-33`, `:59-76`) | decorative, moved by a binding; also enclosed between base and plate, a 0.365 m gap that no delivered ball (0.5 m across or more) can enter | span binding |

**Visible-solid repair (decided under the standing delegation; the audit judged it sound).** The stripe is reprinted flush on the plate's top face, the repair `face-markings-flush` made elsewhere; reason: a ridge on the launch face would deflect a ball. It is in no repair contract yet; the Batch A summary proposes the contract text. The slice edits `springboard.tscn`, classifies the stripe as printed flat, adds the package to `FaceMarkingTests`, and adds the coil test above.

**Generic laws.**

| Behaviour | Law | Declared values |
| --- | --- | --- |
| The plate moves only along its local Y, between stops | Prismatic joint law with lower and upper limits | base frame (0, 0.14, 0); limits −0.25 m and 0 |
| The plate is pushed back toward rest and loses energy | Soft spring-damper on the joint coordinate (TGS Soft) | `stiffness`, `damping` |
| A charged board launches at Run | The same spring law from a displaced starting coordinate; the energy is ½ · k · q² and is finite | `initial_compression` |
| A ball is thrown, at any tilt | Contact between plate and payload; the joint acts along the plate's own axis | restitution 0, friction 0.1, bounce threshold 0.05 m/s; plate mass 0.25 kg |
| Plate and base never fight each other | Declared collision filter for the connected pair | `BodiesCollide: false` |
| The plate art sits on its collider; the coil follows | Body binding and span binding, read from the committed plate pose | section 2 |
| A launch publishes one Pulse (planned, owner decision 7) | Passage sensor law: a body centre crossing a declared plane forward publishes one Pulse on the bound port; crossing backward publishes nothing | aperture radius 0.65 m, 0.8 m above the plate at rest, in the base's frame; rearm 0.02 m |

No row prescribes a payload velocity; there is no latch and no impact callback.

**Why two colliders.** Visible = solid, one box per drawn solid, on two bodies: the base and the moving plate.

**Performance fitness.** The base is a static body. The plate is one dynamic body on one joint. Island sleeping is not implemented; a resting body is still stepped. Owned by `ENGINE-ISLANDS-SLEEP`. No field volume. The planned launch sensor is one bounded passage aperture; today the passage tracker runs on the host after the tick (`engine/gpu/PassageSensorDeclaration.cs:80`–`:112`), and `ENGINE-SENSOR-VOLUMES` moves it into the tick with candidates from the spatial query interface. Tunnelling is excluded by the engine's general means, 480 Hz substeps with speculative contact margins (`docs/gpu-f32-physics.md:375`); both boxes are at least 12 cm thick. Nothing here is proportional to the whole scene.

**No engine code names the package.** The audit's engine search found no id literal, file or branch for `spring`. One refactor is owed by shared presentation code the part uses, not by the part: the plate's rest pose is displaced along a fixed up axis (`engine/MachinePart.cs:162`) and the coil's span binding is aligned on the same fixed axis (`:231`), rather than on the declared joint frame (audit E7); owned by `ENGINE-CAPABILITY-INSTANCES`.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | The plate is a finite-mass body on a passive elastic slider; contact loads the shared potential; no impact callback prescribes a payload velocity. | `parts/SpringPart.cs@a6c914e:L8-L9` | carry forward. |
| 2 | Elastic and damping loads were added to the slider every tick by the part (`PreparePhysics`). | `parts/SpringPart.cs@a6c914e:L67-L72` | do not carry forward: per-element update loop; the spring is a declared constraint in the shared solver. |
| 3 | The elastic law: E = ½ k (q − rest)²; stiffness must be finite and positive. | `engine/physics/AxialElasticPotential.cs@a6c914e:L9-L32` | carry forward the law; do not carry forward the CPU interval-effort implementation. |
| 4 | Rotated 0/0/0, 30/50/70 or 90/0/0, an undamped plate loaded by a ball at 2 m/s compresses 0.01–0.25 m, returns the ball with > 0.2 m/s along its normal, and total energy (plate + ball kinetic + elastic) never exceeds the initial value + 1e−5 J. | `CuriousContraptions.tests/SpringAnimationTests.cs@a6c914e:L43-L71` | carry forward (tilted frame works along its local axis; no free energy). |
| 5 | Zero preload with a resting ball, or 0.2 m preload with the ball 2 m to the side, gives zero ball speed and no hit; 0.2 m preload under the ball launches it (> 1 m/s); initial energy equals ½ · 400 · q² within 1e−5. | `CuriousContraptions.tests/SpringAnimationTests.cs@a6c914e:L73-L95` | carry forward (missed/uncharged controls). |
| 6 | The plate body and its visual share one pose within 1e−6 m every tick; rendering 1 or 4 frames per tick never changes physics; Reset and Load restore the construction exactly. | `CuriousContraptions.tests/SpringAnimationTests.cs@a6c914e:L97-L130` | carry forward. |
| 7 | Stiffness 119 or 1201, damping −0.1 or 8.1, initial compression −0.01 or 0.21, and NaN reject; the removed `strength` property (8.5) rejects. | `CuriousContraptions.tests/SpringAnimationTests.cs@a6c914e:L132-L169` | carry forward the bounds and the obsolete-field rejection; do not carry forward the string property key outside the resource boundary. |
| 8 | spring_forward and spring_signal are won with the spring at precision 1 and 0.45 and lost without it; receiver at (−0.7, 0.55, 0). | `CuriousContraptions.tests/SpringboardLessonTests.cs@a6c914e:L34-L61` | carry forward. `spring_forward` is delivered; `spring_signal` is Planned and owed (§3). |
| 9 | In spring_forward at precision 1 the plate compresses 0.05–0.25 m, the ball rebounds upward faster than 1 m/s and peaks below its start height. | `CuriousContraptions.tests/SpringboardLessonTests.cs@a6c914e:L63-L101` | carry forward (finite height; no energy gain). |
| 10 | A plate at (0, 2.5, 0) tilted −30° with 0.2 m preload lifts a ball resting on it by more than 0.2 m; with zero preload the rise stays below 0.01 m; stored energy at Start equals ½ · 400 · q². | `CuriousContraptions.tests/SpringboardLessonTests.cs@a6c914e:L103-L146` | carry forward. |
| 11 | ready_to_rebound stores 8 J at Start (7.99999–8.00001) on every replay, is won only with the placed basket, and Reset restores the construction exactly. | `CuriousContraptions.tests/SpringboardLessonTests.cs@a6c914e:L148-L176` | carry forward. |
| 12 | spring_forward reference: ball (−3, 3.6, 0), receiver (−0.7, 0.55, 0), spring_1 at (−3, 0.8, 0) tilted −20°. ready_to_rebound: spring locked at (0, 2.5, 0), −30°, initial compression 0.2; ball 0.357 m above along the plate normal; basket_1 solution at (1.8, 1.3, 0) with windows 0.2 m / 5° (0.1 m / 2° at 0.45). | `tools/Campaign/SpringboardLesson.cs@a6c914e:L19-L74` | carry forward (content in `content/puzzles.json`). |
| 13 | Placement assistance widens the set of working spring tilts in spring_forward (−12° to 12°) without relaxing receiver capture; every precise success remains a forgiving success. | `CuriousContraptions.tests/AssistanceTests.cs@a6c914e:L139-L168` | carry forward. |
| 14 | Authoring: spring position step 0.20 m, rotation step 3°. | `tools/Campaign/Program.cs@a6c914e:L544-L545` | carry forward the values. |
| 15 | Each contact on the plate body with approach speed ≥ 0.05 m/s incremented the run-scoped hit count (restored on Reset); the first such contact also recorded the spring's Bounced event at that tick (the event key is per part, so later contacts add no new event); slower or non-plate contacts were ignored. | `parts/SpringPart.cs@a6c914e:L80-L85`; `parts/SpringPart.cs@a6c914e:L31-L33` | do not carry forward: no acceptance consumes a Bounced event or a hit count; the typed launch output of owner decision 7 is a declared passage sensor above the plate (section 2), not a contact count, and the earlier "no port" ruling (D19) is withdrawn. Do not carry forward the per-part `ObserveContact` callback (per-element update loop). |
| 16 | The springboard returns no impact command for a payload on a moving surface frame and leaves both body snapshots unchanged; only the Bumper returns a launch impulse. | `CuriousContraptions.tests/ImpactFrameTests.cs@a6c914e:L122-L127` | carry forward (confirms fact 1: the plate prescribes no payload velocity). |
| 17 | Zero gravity, ball 0.65 m above a springboard placed 0.2 m below its placement target (correction cap 0.3 m, blend 0.4 s at precision 0; cap 0 at precision 1): at precision 0 the plate frame is kinematic, its correction blend launches the ball upward (hit count > 0, vy > 0); at precision 1 the frame is static, nothing hits and the ball keeps its pose and zero velocity; Reset restores the saved machine and a second Run replays identical body states. | `CuriousContraptions.tests/ImpactFrameTests.cs@a6c914e:L16-L86` | carry forward the precision-1 control (no launch without stored energy) and the exact Reset replay; do not carry forward the launch from correction motion: placement correction moving the frame during the Run is a legacy mechanism, and the current springboard launches only from its declared slider and preload (facts 1, 5). |
| 18 | spring_forward at precision 0 with its authored solution: the receiver moved 0.3 m in +x is still captured within 1,200 ticks; moved 0.5 m in −x it is not. The receiver's precision-0 knots are capture margin 0.3 and guide acceleration 12. | `CuriousContraptions.tests/FlightCampaignTests.cs@a6c914e:L15-L40`; `content/puzzles.json@a6c914e:L781-L796` | carry forward the +0.3 m capture. Do not carry forward the −0.5 m miss or the 1,200-tick bound: the owner accepted the physically valid −0.5 m capture on 13 October 2026 (§6), and a deadline is a stall guard, not a timing feature. |

Files harvested:
- `parts/SpringPart.cs`
- `reference/cpu/MachinePart.cs` (AddBox half-size rule)
- `parts/scenes/spring.tscn` (script binding only)
- `engine/SpringParameter.cs`
- `engine/physics/AxialElasticPotential.cs`
- `CuriousContraptions.tests/SpringAnimationTests.cs`
- `CuriousContraptions.tests/SpringboardLessonTests.cs`
- `CuriousContraptions.tests/AssistanceTests.cs`
- `CuriousContraptions.tests/ImpactFrameTests.cs`
- `CuriousContraptions.tests/FlightCampaignTests.cs`
- `tools/Campaign/SpringboardLesson.cs`
- `tools/Campaign/Program.cs`

Not springboard knowledge (wound-spring latches, owned by CAT-071): `CuriousContraptions.tests/PhysicsLatchedSpringTests.cs`, `CuriousContraptions.tests/SpringConstraintObservationTests.cs`, `engine/SceneLatchedSpringDeclaration.cs`, `engine/physics/PhysicsLatchedSpring.cs`.

## 5. Acceptance outline

Acceptance: [CAT-062](../requirements.md#current-cat-062), [retained behaviour](../requirements.md#todo-461) and the [elastic contract](../../springboard-elastic-contract.md). Every row runs the production simulation; none depends on a springboard-specific code path.

- **Construction.** `spring_forward`: place the Springboard under the ball, tilt clockwise, Run. Parameter endpoints through the editor.
- **Positive.** Contact compresses the plate within 0.25 m and rebounds the payload; preload releases at Run.
- **Negative/control.** An unloaded board does not launch; a missed ball gets nothing; a zero-load assembly stays still.
- **Boundaries.** Stops at −0.25 m and 0; parameter endpoints; the removed `strength` parameter is refused visibly.
- **Run/Reset, Save/Load.** Exact rest or precompression after Reset; parameters persist.
- **Integrations.** `spring_signal` (Impact switch and Signal lamp), `ready_to_rebound`, the fifteen composite spring lanes.
- **Launch output (owner decision 7; planned in EL-194's slice after `ENGINE-SENSOR-VOLUMES`).** EL-194's rows L1 to L6 (`SpringboardLaunchSignalTests`, [EL-194 section 5](EL-194-springboard.md)) are this part's launch-output acceptance: one Pulse per launch (a charged board under a resting Tennis ball lights a wired Signal lamp; a Basketball also fires once), none from an uncharged board, a missed ball or a Bowling ball that does not reach the plane, none from a falling ball crossing toward the plate, the plane turning with a board on its side, Reset and save keeping the wire with no pending pulse, and a typed refusal of an electrical or rope link or a second activation link on `ActivationOut`. Stall guard 3 s; counts are exact.
- **Planned with the stripe repair.** `SliderInstanceTests.TheCoilSpansFromTheBaseToThePlateAtTheDeclaredCompression`: given a board with `initial_compression` 0.2 m, before Run the coil's Y scale times its 0.365 m rest length equals the distance from the base anchor to the plate's underside within 0.005 m (a larger gap or overlap is visible as a coil detached from the plate). Control: at 0 the scale is 1. Boundary: at the lower stop (0.25 m) the scale is still positive.

## 6. Open questions

Resolved:

- **Forgiving Spring Forward Receiver offsets.** Owner, 13 October 2026: the physically valid capture at the −0.5 m locked-Receiver offset is accepted beside the +0.3 m capture ([review-6-5 R5](../../../_bmad-output/implementation-artifacts/review-6-5-springboard-preload.md#r5--fact18-owner-resolution-and-full-numerical-pass)). Both are proven by `WorkshopPhysicsEngineTests.ForgivingSpringForwardReceiverOffsetsCaptureInTheShippedEngine`.
- **Plate contact material.** Restitution 0, bounce threshold 0.05 m/s, friction 0.1, from the source; it is what the package declares.
- **Latch.** None. Preload is physical starting compression of the one spring; no latch or second launch path exists or is planned.
- **Plate stripe.** Reprinted flush (standing delegation; reason in section 3).
- **A signal output for EL-194 (owner decision 7, 22 November 2026).** EL-194 gets its own typed signal output that fires on launch: a declared sensor and port on general capabilities, with new acceptance rows in EL-194's spec. The earlier "no port" ruling under D19 is withdrawn. `spring_signal`, where the ball strikes an Impact switch, is unchanged.
- **EL-194 is not a Duplicate closure (audit H2; owner decision 7).** `requirements.md` makes "typed signal integration" and "all fifteen composite lane instances" part of EL-194's qualification, and neither is played today. The part check runs in the "Launch parts" review; the identity stays open on its launch-signal rows and those two level criteria.
- **Coil form (owner decision 4, 22 November 2026).** The Springboard shows four rings; `DESIGN.md:359` and the requirement's "helix" follow the part. No helix mesh is drawn.

Needs the owner: nothing.

## 7. Integration

- **Collaborators.** Basketball, Bowling ball, Tennis ball and Baseball; Receiver; Impact switch and Signal lamp (`spring_signal`); Wall. All delivered. TH-33 Spring-mounted match uses a generic spring assembly, not this package.
- **Campaign.** `ready_to_rebound` and `spring_forward` (Playable), `spring_signal` (Planned) and the fifteen composite lanes in `content/puzzles.json`.
- **Coverage binding.** Catalogue key `spring` in `docs/coverage/catalogue-elements.json`. A level that offers it from the player's inventory needs no lesson-fixture story; a level that locks it as a fixture adds a `FIX-` row to the catalogue fixture table ([current consumers](../invest/current-consumers.md), read by `tools/Coverage/CurrentCatalogue.cs:100`), and needs [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) only for per-parameter or connection locks.

## 8. Delivery

- **Disposition.** Delivered: package `spring`. Records under `_bmad-output/implementation-artifacts/`: `review-6-4-passive-springboard.md`, `review-6-5-springboard-preload.md`, `review-c-bumper-spring.md`.
- **Residual, in order.** Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element. (1) The flush-stripe repair with the coil test (element tier; files in section 3). (2) The EL-194 part check in the "Launch parts" closure review. (3) EL-194's launch-signal output and its acceptance rows (owner decision 7), in EL-194's slice, after `ENGINE-SENSOR-VOLUMES`: the sensor, `ActivationOut` and their binding added to `elements/Springboard/SpringboardElementDeclaration.cs` and the socket locator to `springboard.tscn`. (4) `spring_signal` in Early level proof batch 1. (5) The fifteen composite lanes after ENGINE-GOALS; only then is EL-194 closed. Chrome proof is queued for the post-catalogue phase.
- **Disqualifiers.** A latch or free-launch path beside the declared compression; a stripe left proud without a collider; a launch signal derived from plate contact or a package-specific callback instead of the declared passage sensor.
