# EL-194 · Springboard named-identity spec

**Current delivery (24 October 2026):** [EL-194 delivery slice](../invest/named-elements.md#element-194). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Harvest citations are `path@a6c914e:Lstart-Lend`. **Build-ready revision, 21 November 2026:** sections 1–3, 5 and 6 were verified against checkout `8d1d68aa`; citations of the form `path:line` are in that checkout, and test files are under `CuriousContraptions.tests/`. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-194 |
| Name | Springboard |
| Type | Mechanical |
| Anchor | [requirements.md#element-194](../requirements.md#element-194); [named entry](../invest/named-elements.md#element-194); contract [springboard-elastic-contract.md](../../springboard-elastic-contract.md) |
| Proof owner | S300 |
| Refines / extends | Refines [CAT-062 spring](CAT-062-spring.md) ([requirement](../requirements.md#current-cat-062)) |
| Related | CAT-065 Trampoline (shares the elastic capability, different declaration), EL-215 Damped cushion, CAT-063 Impact switch and CAT-035 Lamp (the `spring_signal` lanes) |
| Delivered part | Package `spring`, directory `elements/Springboard` (passive Story 6.4, published at `781e0c81`; preload Story 6.5; generic conversion `c13d07c5`) |
| Disposition | **Refinement of CAT-062 with one part delta and new acceptance rows; not a Duplicate closure (owner decision 7, 22 November 2026).** The Springboard gets its own typed signal output that fires on launch: a declared passage sensor above the plate and an `ActivationOut` Pulse port, both general capabilities any package declares (the Ball detector's, `elements/BallDetector/BallDetectorElementDeclaration.cs:70`–`:74`). The change is made to the delivered package `spring` in this identity's slice (rows L1 to L6). This identity's requirement (`docs/planning/requirements.md:5998`) also makes the springboard lessons and the surviving composite springboard instance (`double_cold_start` lane 2, until its conversion, completion-ledger OS-08; decision 24d closed the other 14, OS-10) part of its qualification, and level 7 `spring_signal` (authored position before decision 24d, paired with its plan level, 11c, in [campaign coverage, section 5](readiness/campaign-coverage.md#5-removed-and-converted-levels-owner-decision-24d)) stays a level proof; both are owned as stated in section 5. [Delivery batches 2c](../invest/delivery-batches.md#2-duplicate-closures) lists as its own rows the launch output, `spring_signal` and its two open level criteria; preload and `spring_forward`, deltas named by earlier planning, are delivered and tested. No package, palette or catalogue entry of its own |
| Status | Part behaviour delivered except the launch output; slice not started. Blocked on the engine programme (owner decision 3a), relying most on ENGINE-SENSOR-VOLUMES (passage sensing in the physics tick and relative to the frame) and ENGINE-ISLANDS-SLEEP; then this identity's slice; the identity closes after level 7 `spring_signal` (authored position before decision 24d; plan level 11c, [campaign coverage, section 5](readiness/campaign-coverage.md#5-removed-and-converted-levels-owner-decision-24d)) and `double_cold_start`'s composite lane (ENGINE-GOALS; until its conversion, OS-08). Browser qualification pending |

**What EL-194 adds over the delivered part.** Ledger sentence: "Declared finite elastic store transfers work through physical compression and release." Exact source outcome: "Uncharged or missed contact cannot impart a free launch." Both are executed (section 5). The requirement entry adds a qualification list: passive and precharged contact, typed signal integration, no-spring, disconnected and missed controls, the springboard lessons and the surviving composite springboard instance, parameter editing, exact Reset and save. Of these, typed signal integration is met by the part's own launch output (owner decision 7, rows L1 to L6), and the signal lanes and `double_cold_start`'s composite lane are level proofs not yet executed.

## 2. Declaration

The `spring` declaration, art and bindings are [CAT-062 section 2](CAT-062-spring.md#2-declaration); this identity adds no second text (coordinator decision 27, 23 November 2026). Palette: CAT-062's order 12; no order of its own.

- **Launch sensor and `ActivationOut`:** [CAT-062 section 2](CAT-062-spring.md#2-declaration), "Launch output" (coordinator decision 27). This is the one part delta this identity's slice adds to the delivered package (owner decision 7); rows L1 to L6 prove it.

### Variants

Two authored states of the one part, not two parts: **passive** (`initial_compression` = 0) and **precharged** (`initial_compression` > 0).

### Art and solidity

[CAT-062](CAT-062-spring.md), its art table and the flush-stripe repair (`Visual/Plate/Visual_Stripe`, decided under the standing delegation there). The launch sensor has no art of its own: it is an invisible region, and the `ActivationOut` socket follows the delivered socket art.

## 3. Engine capabilities

Map families: ContactImpulse, ElasticStorage, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction ([map row](../general-engine-element-map.md); navigation only, decision D14). The harvest's "Missing: preload" row is superseded.

| Capability | Status | Evidence |
| --- | --- | --- |
| Prismatic slider with parameter-bound spring, damping and stops | present | `PrismaticSliderDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:104`); `SliderInstanceTests.ThePlateHoldsItsLowerAndUpperStops` (`SliderInstanceTests.cs:61`) |
| Declared starting compression as finite stored energy | present | `Compression: MechanicalValue.FromParameter("initial_compression")` (`elements/Springboard/SpringboardElementDeclaration.cs:72`); `WorkshopSpringboardTests.CompiledPreloadUsesActualF32PoseAndEightJoules` (`WorkshopSpringboardTests.cs:146`) |
| Slider body and span presentation | present | `SliderInstanceTests.ThePlateVisualRestsAtTheDeclaredCompressionAndEqualsItsCompiledBody` (`:76`) |
| A passage sensor on a static frame body publishing a Pulse to a typed `ActivationOut` port | present (host-side, after the tick) | `SignalPassageDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:54`); the Ball detector (`elements/BallDetector/BallDetectorElementDeclaration.cs:70`–`:74`); `BallDetectorTests.PassageSensorTracker_PositiveForwardPass_RotatedAndVariousSpeeds` (`CuriousContraptions.tests/BallDetectorTests.cs:152`), `PassageSensorTracker_NegativeControls_ReverseOutsideStationary` (`:257`) |
| Passage sensing inside the physics tick, relative to the sensor's frame | missing | today it runs on the host after the tick (`engine/gpu/PassageSensorDeclaration.cs:80`–`:112`); owner ENGINE-SENSOR-VOLUMES (owner decision 3a) |

Counts: 4 present, 0 untested, 0 narrower, 1 missing (owned by the programme story ENGINE-SENSOR-VOLUMES). Nothing needs element-specific engine code.

## 4. Sources and legacy

- Requirement: "Declared finite elastic store transfers work through physical compression and release"; outcome "Uncharged or missed contact cannot impart a free launch"; qualify passive/precharged contact, typed signal integration, no-spring/disconnected/missed controls, the springboard lessons `spring_forward` (plan level 2), `ready_to_rebound` (3a) and `spring_signal` (11c), and the surviving composite springboard instance (`double_cold_start` lane 2, plan level 19b, until its conversion, completion-ledger OS-08; decision 24d closed the other 14, OS-10), parameter editing and exact Reset/save ([element-194](../requirements.md#element-194)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Rest height 0.14, stroke 0.25, plate 0.25 kg, plate half extents (0.65, 0.075, 0.6) | `parts/SpringPart.cs@a6c914e:L14-L18` | carry forward | Contract values. |
| 2 | Parameter validation 120–1200, 0–8, 0–0.20 with an explicit message | `parts/SpringPart.cs@a6c914e:L50-L59` | carry forward | Admission bounds. |
| 3 | Elastic and damping loads applied in `PreparePhysics` | `parts/SpringPart.cs@a6c914e:L67-L72` | do not carry forward (per-part loop) | Now a declared soft spring row. |
| 4 | `Bounced` event and hit counter on plate contact ≥ 0.05 m/s | `parts/SpringPart.cs@a6c914e:L80-L85` | do not carry forward | No acceptance consumes it (CAT-062 §2); see section 6. |
| 5 | Lesson `spring_forward`: ball at (−3, 3.6, 0), receiver at (−0.7, 0.55, 0), solution springboard at (−3, 0.8, 0) tilted −20° | `tools/Campaign/SpringboardLesson.cs@a6c914e:L25-L36`; `content/puzzles.json@a6c914e:L707-L917` | carry forward | First lesson. |
| 6 | Composite lanes: `Add` places each module lane at (X offset, Z offset) with a yaw about Y, rotating every module part's position and orientation and prefixing its id `laneN_` | `tools/Campaign/Program.cs@a6c914e:L323-L345` | carry forward (layout rule) | How the fifteen lanes are built. |
| 7 | The fifteen springboard lane instances, as (level id, lane, module, X, Z, yaw°): (`bounce_and_roll`, 2, spring, 0, 2, 0); (`spring_and_chain`, 1, spring, 0, −2, 0); (`two_signals`, 2, spring_signal, 0, 2, 0); (`deep_springs`, 1, spring, −2, 0, 90); (`deep_springs`, 2, spring, 2, 0, −90); (`three_deliveries`, 3, spring, 0, 3, 0); (`triple_signal`, 2, spring_signal, 0, 0, 0); (`cold_front`, 3, spring, 0, 3, 0); (`bounce_mail`, 1, spring, 0, −3, 0); (`double_cold_start`, 2, spring, 0, 0, 0); (`signals_and_chain`, 2, spring_signal, 0, 0, 0); (`cold_signals`, 2, spring_signal, 0, 0, 0); (`depth_delivery`, 3, spring, 3, 0, −90); (`depth_telegraph`, 2, spring_signal, 0, 0, 90); (`bridges_and_signal`, 2, spring_signal, 0, 0, 0) | `tools/Campaign/Program.cs@a6c914e:L448-L450`, `tools/Campaign/Program.cs@a6c914e:L463-L468`, `tools/Campaign/Program.cs@a6c914e:L483-L485`, `tools/Campaign/Program.cs@a6c914e:L489-L499`, `tools/Campaign/Program.cs@a6c914e:L503-L505`, `tools/Campaign/Program.cs@a6c914e:L509-L511`, `tools/Campaign/Program.cs@a6c914e:L515-L526`, `tools/Campaign/Program.cs@a6c914e:L530-L532` | carry forward | The requirement's "all fifteen composite lane instances". |
| 8 | Module `spring` is the `spring_forward` lesson; module `spring_signal` is the same lane with the receiver replaced by a switch, a locked lamp at (4.5, 1, 0), goal Activated on the lamp and a solution wire switch → lamp | `tools/Campaign/Program.cs@a6c914e:L9-L9`, `tools/Campaign/Program.cs@a6c914e:L14-L17`, `tools/Campaign/Program.cs@a6c914e:L52-L63` | carry forward | Lane content. |

**Files harvested:**
- `parts/SpringPart.cs`
- `parts/catalog/spring.tres`
- `engine/physics/PersistentContactPair.cs` (material argument order)
- `tools/Campaign/SpringboardLesson.cs`
- `tools/Campaign/Program.cs` (composite lanes and modules)
- `content/puzzles.json` (level `spring_forward`)
- Searched with no further springboard knowledge for this identity: `CuriousContraptions.tests/` (the springboard tests belong to [CAT-062](CAT-062-spring.md), Batch A), `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-194](../requirements.md#element-194) and the [contract](../../springboard-elastic-contract.md#chrome-observable-acceptance-cat-062). The Chrome recipe (open `spring_forward`, place the springboard under the ball and tilt it with the gizmo, Run) stays queued for the post-catalogue phase.

### Closure check for the part

| Criterion | Test |
| --- | --- |
| Precharged: a resting ball is launched by the finite preload | `SliderInstanceTests.APreloadedBoardLaunchesARestingBallAndAnUnchargedOneDoesNot` (`SliderInstanceTests.cs:36`), charged row: rise above 0.2 m |
| **Exact source outcome, uncharged:** no free launch | The same theory, uncharged row: rise below 0.05 m |
| **Exact source outcome, missed contact:** a ball beside a charged board is untouched | `SliderInstanceTests.AMissedBallGetsNothingFromAChargedBoard` (`:49`) |
| Passive contact and the lesson, with no-spring and aimed-away controls | `LaunchLessonSolveTests.EachLaunchRouteSolvesOrStaysUnsolvedAndResetRestoresTheBench` (`LaunchLessonSolveTests.cs:239`): routes SpringPassive, SpringCharged, SpringZero, SpringAway; `AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` (`:111`) |
| Boundaries: stops; parameter endpoints; compiled preload energy | `SliderInstanceTests.cs:61`; `WorkshopSpringboardTests.cs:146` |
| Parameter refusal | `WorkshopSpringboardTests.InvalidPreloadRejectsResourceAndSaveBeforeAcceptedStateChanges` (`WorkshopSpringboardTests.cs:236`) |
| Wrong-kind connection refusal (the part has no port) | the generic wrong-domain refusal in `BundlePortsTests` (`CuriousContraptions.tests/BundlePortsTests.cs:262`); `UnsupportedWireTailRejectsBeforeMutation` (`:98`) is a save-boundary test and is kept as that |
| Run/Reset; Save/Load | `PalettePreviewHasArtWithoutInventedIdentityAndOwnedPlateResets` (`:267`) and the Reset half of `LaunchLessonSolveTests.cs:239`; `CurrentSaveAndWirePreserveF32Settings` (`:199`), `PreloadResourceSaveAndPhysicalRestPreserveExactF32` (`:218`) |

The closure reviewer confirms one assertion by reading: that a delivered CAT-062 test shows the second bounce lower than the first (damping removes energy; the spring returns no more than was stored). If none does, the closure adds `SliderInstanceTests.ASecondBounceOffAPassiveBoardIsLowerThanTheFirst`: a ball dropped 1 m onto a default board, apex of bounce 2 below apex of bounce 1 by more than 0.01 m; control `damping` 8 gives a larger drop; stall guard 6 s.

### Launch output rows (owner decision 7; planned, `SpringboardLaunchSignalTests`)

Run on the production simulation through placed packages and wires. Stall guard 3 s of game time; no arrival tick is asserted, only the occurrence, its count and its causal order.

| Row | Given / When / Then | Control | Boundary | Tolerance and its gameplay reason |
| --- | --- | --- | --- | --- |
| L1 | A board with `initial_compression` 0.2 m under a resting Tennis ball, `ActivationOut` wired to a Signal lamp. Run: exactly one Pulse is published, after Run starts and before the ball's apex, and the lamp lights | L2 | a resting Basketball (rise about 0.65 m, its centre crosses the plane) also fires once | exact count: a launch that fires twice or never breaks every signal lesson built on it |
| L2 | Source outcome, no free launch: an uncharged board under the same ball, and a charged board with the ball 2 m to the side, publish nothing in 3 s | L1 | a resting Bowling ball on a charged board (rise about 0.19 m) publishes nothing | exact count 0 |
| L3 | A Tennis ball dropped from 2 m over a passive board crosses the plane toward the plate: that crossing publishes nothing | L1 | the same drop beside the aperture | forward-only: a falling ball is not a launch |
| L4 | The board placed on its side (launch axis +X) launches a ball sideways through the plane: one Pulse, the plane turning with the board | L1 | — | the sensor is in the board's frame |
| L5 | Reset restores the authored state with no pending pulse; save and load keep the `ActivationOut` wire | — | — | exact field values |
| L6 | An electrical or rope link to `ActivationOut`, and a second activation link on it, are refused with a typed reason and change nothing | the lamp wire is admitted | — | typed refusal |

### Level criteria that stay open on the identity

| Criterion | Level | Owner | Why it is not in the closure |
| --- | --- | --- | --- |
| Level 7 `spring_signal` (authored position before decision 24d; plan level 11c, [campaign coverage, section 5](readiness/campaign-coverage.md#5-removed-and-converted-levels-owner-decision-24d)), with the disconnected control (the part's own typed signal integration is rows L1 to L6) | 7 `spring_signal`: the springboard throws the ball onto an Impact switch wired to a Lamp; removing the wire, or the springboard, leaves the Lamp dark | a later Early level proof "Signals" batch; no contract exists yet (`_bmad-output/implementation-artifacts/spec-early-level-proof-batch-1.md:24` holds only `air_mail`, `third_dimension` and `a_gentle_rebound`) | A level proof, already planned; it uses only delivered parts |
| The surviving composite springboard instance (harvest fact 7; decision 24d closed the other 14, OS-10) | 49 `double_cold_start` lane 2 (authored position before decision 24d; plan level 19b, [campaign coverage, section 5](readiness/campaign-coverage.md#5-removed-and-converted-levels-owner-decision-24d)) in `content/puzzles.json`, until its conversion (completion-ledger OS-08) | That level's own proof after `ENGINE-GOALS` (it has five goals) | The engine evaluates one goal per level today |

Each level proof follows [delivery batches §4](../invest/delivery-batches.md#4-early-level-proof): the level loads, the intended solution solves within a generous stall guard, one missing cause fails, Reset restores and save/load round-trips. No arrival tick is asserted.

### Integration

- **Collaborators.** Ball, Receiver, Impact switch, Lamp, Domino, Fan, Conveyor: all delivered.
- **Campaign.** Row 1 (springboard): first use 1–10, reuse 21–30, 41–50, 91–100, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); retained behaviour [todo-461](../requirements.md#todo-461). Level 4 `spring_forward` is Playable.
- **Coverage binding.** Catalogue key `spring` (CAT-062). EL-194 has no package of its own and so no planned binding section; the launch output is added to the delivered package. The lanes place no locked springboard, so the closure needs no lesson-fixture story (S8); the composite levels are authored when proven.

### Delivery

- **Slice.** `named-el-194`: the launch sensor, `ActivationOut` and their binding added to `elements/Springboard/SpringboardElementDeclaration.cs` and `springboard.tscn` (socket locator), rows L1 to L6, the flush-stripe repair if not yet done, and the part-level check of the closure table above. Review tier: data-only once ENGINE-SENSOR-VOLUMES has moved passage sensing into the tick; otherwise engine. It does not close EL-194.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SENSOR-VOLUMES (passage sensing in the tick, frame-relative) and ENGINE-ISLANDS-SLEEP; then this slice. The identity closes only after level 7 `spring_signal` (authored position before decision 24d; plan level 11c, [campaign coverage, section 5](readiness/campaign-coverage.md#5-removed-and-converted-levels-owner-decision-24d); a later Early level proof Signals batch, no contract yet) and `double_cold_start`'s composite lane (after ENGINE-GOALS, five slices, owner decision 12; until its conversion, OS-08).
- **Files.** `docs/planning/invest/named-elements.md` (the element-194 entry cites `spring`, the tests above, and names the two open level criteria with their owners), this spec's status, `docs/planning/invest/delivery-batches.md` (EL-194 stays in 2c, with its two open criteria and their owners named), the review record.
- **What would disqualify the plan.** A resting ball's centre reaching the launch plane on any admitted placement (then the plane rises, it is not given a dwell); the passage descriptor's forward axis not following the frame's rotation (row L4).

## 6. Open questions

None open.

1. Is the required "typed signal integration" a port on the Springboard? **Decided by the owner: yes** (owner decision 7, 22 November 2026): its own typed signal output that fires on launch, a declared sensor and port on general capabilities, as a refinement with new acceptance rows. The earlier D19 answer ("no port") and its owner item are withdrawn. A passage sensor above the plate is chosen over a contact trigger on the plate: a contact trigger cannot be owned by a moving body today (ENGINE-CONTACT-OBSERVATIONS), and a ball resting on the plate would be in contact before the launch.

**Needs the owner.** None.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026, with the package opened (`elements/Springboard/`) and the engine projects searched for the package id and name (no hit).

1. **Declaration data only.** A static base, one slider body with a declared spring, damping and starting compression, parameters and bindings. No engine code names the package.
2. **Generic laws.** The prismatic joint's spring and damping, a finite elastic store set by the declared starting compression, and contact. "No free launch" is conservation: an uncharged board holds no energy and a missed ball has no contact.
3. **Three layers.** Colliders: the base and the plate, each with its proxy; the launch sensor is a declared region, not a collider. Visuals: base, plate, stripe and coil (four rings); none is a collider. Animation: the coil, drawn by a span binding from the committed compression (decorative, writes nothing back). The plate art follows the slider body's pose. One visible-solid defect is recorded above: the stripe is reported 8 mm proud and needs a flush redraw.
4. **Flow and typing.** One typed `ActivationOut` Pulse port (planned); f32 Real parameters with declared units; nothing flows back from presentation.
5. **Flat state.** Bodies 2 (1 static, 1 dynamic); colliders 2 (boxes); joints 1 prismatic; drives 0; sensors 1 passage (planned); signal nodes 1; stores 0 beyond the joint's elastic state; presentation bindings: the plate's body binding and the coil span.
6. **Performance.** Two primitive colliders; one bounded passage region whose candidates come from ENGINE-SPATIAL-QUERIES through ENGINE-SENSOR-VOLUMES. Nothing is cosmetic in physics. No island sleeping exists today; a resting board costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered.
7. **Tests are scenarios.** The delivered rows run the production simulation.

**Verdict: conforms, with one art fix (the proud stripe) and the launch output of owner decision 7 to add from general capabilities; blocked on the engine programme (owner decision 3a).**

**Record inconsistency noted for the coordinator.** `spec-6-5-springboard-preload.md` has status `in-review` and `spec-c-bumper-spring.md` has status `review`, while the preload parameter, its tests and the generic conversion are on published main. The closure cites the tests, not those records.
