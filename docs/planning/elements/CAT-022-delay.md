# CAT-022 · delay — declaration readiness spec

**Status (verified 22 November 2026 against `8d1d68aa`):** delivered as package `delay` (`elements/DelayBox/`, display name Delay). The declaration is data, art and bindings only, and no engine gap or conformance defect is recorded. Owner decision 4 (22 November 2026) settles the art: the delivered square housing stays, the twelve face marks are restored as flush printing, and `DESIGN.md:341` follows the part. Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026); the part needs no specific engine story of its own. Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is pending until every named element passes. The `@a6c914e` citations in §4 record the harvest source, not current instructions.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-022 · `delay` |
| Requirement | [CAT-022](../requirements.md#current-cat-022) |
| Mapped identities | none directly. Related timing identities with their own specs: EL-107 Spiral gravity-delay tube (physical delay), EL-181/EL-182 edge detectors, EL-183 Resettable counter. |
| Disposition | **Delivered.** Package id `delay`, directory `elements/DelayBox/`. Records: `review-9-5a-declared-timer-delay.md`, `review-9a-pkg-3.md`; colliders by `elements-visible-solid` (done). |
| Roadmap | `delayed_signal` is a current puzzle; Story 4.2 (countdown hand and indicator cosmetics) |
| Levels | three Playable lessons use it: `delayed_signal` (inventory), `wind_then_release` and `saved_for_later` (placed). `delayed_solar` is Planned. See [CAT-022-I consumers](../invest/current-consumers.md#cat-022-i) |

## 2. Declaration

Delivered declaration: `elements/DelayBox/DelayBoxElementDeclaration.cs`; scene `elements/DelayBox/delay.tscn`.

| Item | Value and source |
| --- | --- |
| Interfaces | `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). Declared at `DelayBoxElementDeclaration.cs:10`. Domain: Signal only (`:52`); no electrical interface, node or port. |
| Body | One static body (footprint 1 body, 0 moving, 0 constraints, `:34`). No mass frame is declared. |
| Colliders | Seven, on that body, as hidden `col_` nodes (`delay.tscn:55`–`69`, `:116`–`155`). `col_0_0`: box, half extents (0.675, 0.075, 0.4) m at (0, −0.7, 0), the base. `col_0_1`: box, half extents (0.625, 0.625, 0.3) m at the origin, the housing. `col_0_2`: cylinder, radius 0.55 m, height 0.035 m, axis +Z, at (0, 0, 0.32), the face. `col_0_3`: sphere, radius 0.075 m at (0, 0, 0.4), the hub pip. `col_0_4`: sphere, radius 0.065 m at (0, −0.35, 0.37), the indicator. `col_0_5` and `col_0_6`: spheres, radius 0.075 m at (−0.72, 0, 0) and (0.72, 0, 0), the two port sockets. |
| Material (f32) | restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`:47`). Pair restitution is the product of the two materials, so 1 is the neutral value. |
| Parameter | One: `delay_seconds`, Real, seconds, default 1, minimum 0.1, maximum 12 (`:31`); editor policy `Reject`, `BuildOnly` (the record's defaults, `CuriousContraptions.Abstractions/ParameterDescriptor.cs:60`–`61`). Authored in seconds of game time, never ticks (D10). |
| Ports | Two. `PortId.ActivationIn`: domain Signal, direction Input, role `ActivationIn`, signal kind Pulse, at (−0.72, 0, 0), facing −X (`:38`). `PortId.ActivationOut`: domain Signal, direction Output, role `ActivationOut`, signal kind Pulse, at (0.72, 0, 0), facing +X (`:39`). No electrical supply is generated. |
| Signal node | One: `SignalTimerDescriptor` (record at `CuriousContraptions.Abstractions/SignalContractValues.cs:49`), id `signal`, duration parameter `delay_seconds`, completion `TimerCompletionPolicy.Latch`, boundary `TimerBoundary.BeforePhysics`, elapsed signal `TimerElapsedSignal.Activation` (`:55`). Bound to `ActivationIn` as its input terminal and `ActivationOut` as its output terminal (`:57`–`61`). No signal edges (`:56`). |
| Stores, joints, sensors, electrical nodes | none. |
| Events | `TriggerActivated` on node `signal` (`:50`), once when the timer elapses. |
| Presentation bindings | Pick radius 0.9 m, no resize axes (`:12`). Four bindings on node `signal` (`:14`–`17`). Binding 0: `Visual/CountdownHand`, `LocalRotationZ`, driver `CommittedTimerProgress`, identity mapping, 0 to −6.2831855 rad (one clockwise turn), linear. Bindings 1 to 3: `Visual/Indicator`, `AlbedoRed`, `AlbedoGreen`, `AlbedoBlue`, driver `CommittedTimerPhase`, state values Ready (85, 101, 115)/255 = `#556573`, Counting (232, 183, 100)/255 = `#e8b764`, Finished (247, 203, 82)/255 = `#f7cb52`. |
| Catalogue | id `delay`, title Delay, category Control, palette order 6, colour (0.84, 0.61, 0.28) = Delay box `#d69c47` (`DESIGN.md:241`), icon `res://elements/DelayBox/icon.svg`, scene `res://elements/DelayBox/delay.tscn` (`:20`–`32`). Form: `DESIGN.md:341`. |

At a glance: a square ochre block with a round cream clock face, one navy hand and (after the marks repair) twelve flush-printed navy marks; sockets on opposite sides.

## 3. Engine capabilities

Capability probe, 22 November 2026 (tests in `CuriousContraptions.tests/`; each name checked to exist). Counts: 6 present, 0 present but untested, 0 narrower, 0 missing.

| Capability | Status | Symbol | Test |
| --- | --- | --- | --- |
| Declared timer node: deadline = ceiling(seconds × cadence) ticks from the input occurrence; chained timers independent of entity order | present | `engine/gpu/ActivationTimers.cs:18` | `WorkshopDelayTests.CanonicalF32ProductCeilingPreservesDeadlineBoundaries`; `.RealContactChainedTimersFireAtThirtySixTicksRegardlessOfEntityOrder`; `ActivationTimerTests.FractionalStartCeilingAndExactDeadlineEmitDistinctTimerOccurrence` |
| Latch completion: a busy or finished timer ignores inputs until Reset; the due timer emits once; an earlier direct signal is not masked by the timer | present | `ActivationTimerTransitions` (`engine/gpu/ActivationTimers.cs:6`) | `ActivationTimerTests.BusyAndFinishedInputsDoNotRestartOrReemit`; `.TheScheduledStageEmitsNothingAndThePhysicalStageEmitsTheDueTimerOnce`; `.EarlierPhysicalBypassWinsOverDueAtSamePublicationBoundary` |
| Parameter refusal, save, load and Reset; no electrical output port | present | generic typed parameter path | `WorkshopDelayTests.InvalidCanonicalDurationPaddingAndPreviousSchemasReject`; `.UndeclaredOldDurationKeyRejectsAtomically`; `.RealContactNonHalfDurationSurvivesSaveLoadAndResetExactly`; `.DurationPoseAndConnectionsRoundTripAndCompileTheBaseHousingAndFace`; `.PortsAndPuzzlePopulationRemainBounded` |
| Goal "activated after a minimum delay" on the target's occurrence | present | `WorkshopGoalEvaluator` (`engine/gpu/WorkshopGoalEvaluator.cs:8`) | `DelayedSignalTests.GoalUsesTargetOccurrenceNotRootCauseOrTimerAndRejectsEarlyLatch`; `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` (`delayed_signal`) |
| Box, cylinder and sphere colliders as drawn | present | scene geometry extraction | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (row `delay`) |
| Hand and indicator follow committed timer progress and phase and cannot advance the timer | present | `PresentationDriver.CommittedTimerProgress`, `CommittedTimerPhase` | `DeclaredPresentationTests.CommittedLatchAndLampPreserveBothDirectionsAndDelayPhases` (hand sample = −τ × progress at 0, 0.5 and 1; indicator colour applied to the art; Reset restores slate); `ComposableAnimationMappingTests.WorkshopDeclaredPresentation_Discrete3State_AllThreeStatesAndTransitions` (the three phase colours from a real run); `BasketballResourceTests.DelayArtworkUsesSharedFullTurnAndSourcePhasePalette` |

Settled by opening `WorkshopActivationAnimationTests`: it is not the proof of the hand and indicator. Its only test that places a `delay` is `DeclaredPropertyRejectsForeignChannelAndResetCannotReviveOldWorld` (`WorkshopActivationAnimationTests.cs:187`–`208`), which proves that the animation channel for the part's binding 0 refuses a foreign property and cannot be revived across Reset. The binding itself is tested by the three tests in the last row.

Dependencies: a signal source and a signal target.

### Engine conformance

Authority: the owner's engine-conformance instruction of 22 November 2026, `AGENTS.md` (element-free engine gate, Tri-Graph) and the [element-free engine design](../../../_bmad/custom/element-free-engine-design.md).

**Compiled counts per placed Delay** (counted from the declaration and `delay.tscn`). Bodies 1 (static). Colliders 7 (2 boxes, 1 cylinder, 4 spheres). Joints 0. Drives 0. Sensors 0. Domain nodes 1 (signal timer). Stores 0. Ports 2. Events 1. Presentation bindings 4 (1 rotation, 3 colour). All counts are fixed at compile time.

**Why seven colliders.** Visible = solid: the base, housing and face are drawn solids, and the hub pip, the indicator and the two sockets each stand proud of the housing and collide as drawn (coordinator decision, 25 October 2026). The four small spheres are the cost of drawing them proud; none exists for a cosmetic purpose alone, and all seven are primitives on one static body.

**Art layers** (`delay.tscn:75`–`108`; classification as `VisibleSolidColliderTests.cs:83`–`85`).

| Art node | Mesh | Class | Collider | Moved by |
| --- | --- | --- | --- | --- |
| `Visual/Visual_Body` | box 1.25 × 1.25 × 0.6 m at the origin, ochre | solid | `col_0_1` | nothing (static body) |
| `Visual/Visual_Face` | cylinder, radius 0.55 m, 0.035 m thick, axis +Z, at z 0.32, cream | solid | `col_0_2` | nothing |
| `Visual/Visual_Base` | box 1.35 × 0.15 × 0.8 m at y −0.7, navy | solid | `col_0_0` | nothing |
| `Visual/CountdownHand/Visual_HandMesh` | box 0.055 × 0.38 × 0.03 m, offset 0.16 m along the hand from the pivot at (0, 0, 0.37), navy | decorative: moved by a binding | none; a static collider cannot follow it | presentation binding 0 (rotation of `Visual/CountdownHand` about Z) |
| `Visual/Visual_CenterPip` | sphere, radius 0.075 m at (0, 0, 0.4), gold | solid | `col_0_3` | nothing |
| `Visual/Indicator` | sphere, radius 0.065 m at (0, −0.35, 0.37), slate | solid | `col_0_4` | nothing; its colour is set by bindings 1 to 3, its pose is not |
| `Visual/Visual_PortIn`, `Visual_PortOut` | spheres, radius 0.075 m at (∓0.72, 0, 0) | solid | `col_0_5`, `col_0_6` | nothing |
| twelve face marks (to be added by the marks repair, owner decision 4) | flat navy strokes on the face at radius 0.46 m, within 0.5 mm of the face plane | decorative, printed flat | none (no collider added; counts unchanged) | nothing |

Physics reads no art node. The bindings read the committed timer state of node `signal` and write nothing back, which is the requirement's "cannot advance it".

**Behaviour as generic laws.**

| Behaviour | Generic law | Declared values |
| --- | --- | --- |
| A ball bounces off the box as off any fixed solid | rigid contact with pair restitution and Coulomb friction on static boxes, a cylinder and spheres | restitution 1 (neutral under the product rule), friction 0.3 |
| An input starts the wait; the output fires once at the deadline; later inputs are ignored until Reset | signal-network rule for a timer node: Ready to Counting on an input occurrence; Finished after ceiling(seconds × cadence) ticks; with Latch completion a Counting or Finished node ignores inputs; the elapsed occurrence is an activation on the output terminal. The node completes before physics in the tick (`BeforePhysics`) | `delay_seconds` 0.1 to 12 s, default 1 s; completion Latch; elapsed signal Activation |
| The output reaches the wired part | signal-network rule: a pulse output delivers each occurrence to the inputs wired to it | ports `ActivationIn`, `ActivationOut`, kind Pulse |
| The hand turns once and the indicator changes colour | committed-timer presentation bindings (progress to rotation; phase to state colours) | 0 to −2π rad; three state colours |

**Performance fitness.** The body is static. Seven primitive colliders, no sensor, no store. The timer is one node in the signal network's flat node table and costs nothing proportional to the scene. Tunnelling is excluded by the engine's general means (`docs/gpu-f32-physics.md:192`–`195`, `:224`); the package adds no check of its own. No engine gap is recorded.

**Engine search.** No engine code names the package, its id or its class (audit of 22 November 2026). No residue is recorded against this spec.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | The parameter name is `delay_seconds`; an undefined or extra parameter name rejects. | `CuriousContraptions.tests/DelayTests.cs@a6c914e:L16-L23` | carry forward the external name at the resource boundary; do not carry forward string-keyed dictionaries in domain logic. |
| 2 | Timer snapshots restore phase, due tick and progress: Finished → Counting (progress 0) → Ready (start and due ticks −1). | `CuriousContraptions.tests/DelayTests.cs@a6c914e:L24-L49` | carry forward. |
| 3 | For 0.1, 1 and 1.13 s: activation starts Counting; repeated activations before the due tick keep the same due tick and the lamp stays dark; at the due tick it Finishes, lights the lamp and records the event at that tick; activation after Finish is ignored; Reset returns Ready, due −1, progress 0. | `CuriousContraptions.tests/DelayTests.cs@a6c914e:L50-L91` | carry forward. |
| 4 | Chained 0.1 s and 0.2 s delays light the lamp at tick 36 (120 Hz) regardless of entity order; removing a live input during Run is rejected without changing the world. | `CuriousContraptions.tests/DelayTests.cs@a6c914e:L92-L127` | carry forward. |
| 5 | Durations 0, −1, 13 and NaN reject at construction. | `CuriousContraptions.tests/DelayTests.cs@a6c914e:L128-L143` | carry forward. |
| 6 | ActivatedAfter minimum delay −1, 121, NaN or +∞ rejects. | `CuriousContraptions.tests/DelayTests.cs@a6c914e:L144-L161` | carry forward (current bound 0–120). |
| 7 | delayed_signal and delayed_solar win at precision 0, 0.45 and 1, replay with identical tick counts, fail when any wire is removed, and fail with a direct switch → lamp (or → torch) bypass while the delay still Finishes; the placed delay id differs from the authored solution slot id. | `CuriousContraptions.tests/DelayTests.cs@a6c914e:L162-L224` | carry forward (direct-early-bypass control). |
| 8 | Hand and indicator follow committed progress and phase through failed substeps, pause, hiding and completion; physics never writes the material; Reset and Load restore the Ready palette and baseline hand. | `CuriousContraptions.tests/DelayPresentationTests.cs@a6c914e:L34-L95` | carry forward; do not carry forward the per-part scalar-rotation evaluator (replaced by the declared timer binding, Story 4.2). |
| 9 | Unsupported presentation bindings (unit, slot, owner, range, angle, axis, duplicate, timer owner/slot, duplicate colour) reject at Start without changing the construction. | `CuriousContraptions.tests/DelayPresentationTests.cs@a6c914e:L142-L165` | carry forward the reject-before-mutation rule. |
| 10 | Reference delayed_signal: switch (−3, 1, 0), trigger ball (−3, 4, 0), lamp (3, 1, 0), delay_1 (0, 1, 0); links switch → delay → lamp; goal ActivatedAfter(lamp, switch, 1 s). delayed_solar: delay at (0, 1, 2), yaw 180°, switch → delay → torch, goal PoweredAfter(motor, switch, 1 s). | `tools/Campaign/Program.cs@a6c914e:L248-L277` | carry forward (content in `content/puzzles.json`). |

Files harvested:
- `CuriousContraptions.tests/DelayTests.cs`
- `CuriousContraptions.tests/DelayPresentationTests.cs`
- `tools/Campaign/Program.cs`

Reading note for fact 7: `delayed_signal` is Playable and proven today; `delayed_solar` is Planned and has no authored content in `content/puzzles.json` yet (§5).

## 5. Acceptance outline

Acceptance: [CAT-022](../requirements.md#current-cat-022) and [retained behaviour](../requirements.md#todo-130). Every row runs the production simulation through defined interfaces and proves a consequence of a generic law for this declaration. This is the one part in the batch whose elapsed time is asserted exactly, in ticks, because the delay is its contract. Chrome proof (`tools/e2e/anim-1b.test.ts`, `anim-1c.test.ts`) is pending post-catalogue.

| Given / When / Then | Test (exists unless marked planned) | Control and boundary |
| --- | --- | --- |
| The output fires at the deadline and not before | `WorkshopDelayTests.CanonicalF32ProductCeilingPreservesDeadlineBoundaries`; `ActivationTimerTests.FractionalStartCeilingAndExactDeadlineEmitDistinctTimerOccurrence` | the tick before the deadline is dark; 0.1 s and 12 s bounds; 60, 120 and 240 Hz |
| Repeated inputs while Counting or Finished change nothing | `ActivationTimerTests.BusyAndFinishedInputsDoNotRestartOrReemit` | the first input does start it |
| A direct early bypass cannot satisfy a delayed goal | `DelayedSignalTests.GoalUsesTargetOccurrenceNotRootCauseOrTimerAndRejectsEarlyLatch`; `ActivationTimerTests.EarlierPhysicalBypassWinsOverDueAtSamePublicationBoundary` | the wired path does satisfy it |
| Chained delays add, whatever the entity order | `WorkshopDelayTests.RealContactChainedTimersFireAtThirtySixTicksRegardlessOfEntityOrder` | reversed order |
| Invalid durations and undeclared keys are refused atomically | `WorkshopDelayTests.InvalidCanonicalDurationPaddingAndPreviousSchemasReject`; `.UndeclaredOldDurationKeyRejectsAtomically` | a valid save still loads |
| Reset rearms Ready; duration and links survive save and load | `WorkshopDelayTests.RealContactNonHalfDurationSurvivesSaveLoadAndResetExactly`; `.DurationPoseAndConnectionsRoundTripAndCompileTheBaseHousingAndFace` | a non-dyadic duration |
| No electrical supply exists | `WorkshopDelayTests.PortsAndPuzzlePopulationRemainBounded` (no `Supply` output port); wrong-kind connection is generic under S4 row 5 | — |
| Hand and indicator derive from committed timer state | `DeclaredPresentationTests.CommittedLatchAndLampPreserveBothDirectionsAndDelayPhases`; `ComposableAnimationMappingTests.WorkshopDeclaredPresentation_Discrete3State_AllThreeStatesAndTransitions` | Ready is shown, not suppressed (`.WorkshopDeclaredPresentation_UnifiedStateValues_AcrossActuatorContactAndTimer`); Reset restores slate |
| Lessons with a Delay solve, and not with a cause missing | `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` (`delayed_signal`, `wind_then_release`, `saved_for_later`) | 60, 120 and 240 Hz; Reset and a save/load replay |

Levels: `delayed_signal`, `wind_then_release` and `saved_for_later` are Playable and solved with a missing-cause control. Only `delayed_solar` remains: it is Planned, has no authored content yet, and waits for the Flashlight and the Solar panel (Epic 13; neither has a package under `elements/`). Nothing in it is owed by this package.

Integrations named by the requirement: [sequence-task-279](../requirements.md#sequence-task-279) (delay); [IX-07 signal propagation](../requirements.md#interaction-07); campaign first use 21–40 in the [element coverage ledger](../requirements.md#campaign-element-coverage).

Consistency with delivered parts: both ports are Signal-domain pulses, matching the Impact switch, Ball detector, Domino and Counter outputs and the Signal lamp input; the 0.1 s minimum is 12 ticks at 120 Hz, so the shortest delay is still several ticks at every cadence.

## 6. Open questions

None open.

Resolved:

- **Tick order.** Declared: the timer node completes before physics (`TimerBoundary.BeforePhysics`) and latches.
- **Hand and indicator proof (this repair).** Present and tested; `WorkshopActivationAnimationTests` is not the proof (§3).
- **Levels (audit M8).** Only `delayed_solar` waits.
- **Durations are seconds (D10).** No tick count is authored.

- **Face marks and housing (owner decision 4, 22 November 2026).** `DESIGN.md:341` described a "round ochre housing" with "twelve navy marks"; the delivered scene has a square ochre housing with a round cream face and no marks (`delay.tscn:75`–`108`); the harvest art had both a box and a cylinder housing and twelve marks at radius 0.46 m. Decided: keep the square housing (`DESIGN.md` follows the part) and restore the twelve marks as flush printing on the face (decorative, printed flat, within 0.5 mm of the face as `face-markings-flush`), because marks make the countdown readable.

Needs the owner: nothing.

## 7. Integration

- **Collaborators.** Impact switch, Ball detector, Domino, Counter, State latch (sources); Signal lamp, Powered gate and other signal targets. All delivered. Both ends are Signal-domain pulses.
- **Campaign.** `delayed_signal` (inventory), `wind_then_release` and `saved_for_later` (placed), all Playable; `delayed_solar` Planned.
- **Coverage binding.** Catalogue key `delay` in `docs/coverage/catalogue-elements.json`. A level that offers it from the player's inventory needs no lesson-fixture story; a level that locks it as a fixture adds a `FIX-` row to the catalogue fixture table ([current consumers](../invest/current-consumers.md), read by `tools/Coverage/CurrentCatalogue.cs:100`), and needs [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) only for per-parameter or connection locks.

## 8. Delivery

- **Disposition.** Delivered. No slice is owed for the part.
- **Residual work.** Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element; no specific engine story. None for behaviour. The twelve marks (owner decision 4) join the next face-marking slice (`elements/DelayBox/delay.tscn`, `CuriousContraptions.tests/FaceMarkingTests.cs`, the `delay` row of `VisibleSolidColliderTests`, `DESIGN.md:341`), element tier. `delayed_solar` with its campaign slice after Epic 13. Chrome proof post-catalogue.
- **Disqualifiers.** A second timer rule beside the declared node; a tick count authored in place of seconds (D10); a collider added for the hand (art moved by a binding stays decorative).
