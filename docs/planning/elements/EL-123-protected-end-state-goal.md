# EL-123 · Protected-end-state goal — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-123 delivery slice](../invest/named-elements.md#element-123). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code is cited as `path:line` in that checkout; `path@a6c914e` citations appear only in section 4 and describe the harvest baseline. Values marked **proposed** are starting values ([decision D1](../invest/delivery-batches.md#3-family-level-decisions)). Nothing here was built or run.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-123 · Protected-end-state goal · Goal |
| Anchor | [requirements.md#element-123](../requirements.md#element-123) (task `sequence-task-676`); [named-elements entry](../invest/named-elements.md#element-123); umbrella [gap-16](../requirements.md#gap-16) (index only) |
| Owner key | S677 |
| Related identities | Siblings [EL-120](EL-120-quantity-goal.md), [EL-121](EL-121-rate-window-goal.md), [EL-122](EL-122-ordered-events-goal.md). It reads observations that delivered packages declare: the Domino's orientation latch ([CAT-023](CAT-023-domino.md)), the Receiver's residence ([EL-192](EL-192-receiving-basket.md)) and the Balloon's integrity. Later consumers: fragile cargo (EL-084), coated and dyed cargo ([EL-118](EL-118-coating-station.md), [EL-119](EL-119-dye-station.md)). No CAT refines it. |
| Engine story | ENGINE-GOALS: named slice `engine-goal-ordered-events` (forbidden event) and the fifth slice `engine-goal-count-window-hold` (held state; owner decision 12, 22 November 2026) |
| Status | Not started; blocked on the engine programme (owner decision 3a) and on ENGINE-GOALS. Every goal observation today is a latch; none reads a state that must stay true (`engine/gpu/WorkshopGoalEvaluator.cs:27-56`). |

### Disposition

**Level data, not a part.** A protected end state is declared in a level's goal list beside its delivery conditions. No package, directory, palette entry or catalogue key. It is blocked on the engine programme (owner decision 3a). After it, it is two general capabilities, each with one owning ENGINE-GOALS slice, plus the level that first uses it (campaign level 89).

## 2. Declaration

### Declaration to write

The goal list is the one defined by [`spec-engine-goal-evaluation.md`](../../../_bmad-output/implementation-artifacts/spec-engine-goal-evaluation.md). A protected end state adds three things to it. Shapes are indicative; each owning slice fixes the names once.

```json
"goals": [
  {"kind": "Captured", "owner": "fixture_4", "subject": "fixture_1"},
  {"held": {"kind": "Residing", "owner": "fixture_5", "subject": "fixture_2"}},
  {"forbidden": {"kind": "Activated", "owner": "fixture_6", "node": "signal"}}
],
"hold_seconds": 2
```

| Field | Type | Range | Default | Unit | Editor policy |
| --- | --- | --- | --- | --- | --- |
| `forbidden` | one observation selector of any latch kind | a declared observation of a level slot | absent | n/a | level data; not player-editable |
| `held` | one selector of a committed **level** (a state that can become false again); first kind `Residing` (sensor owner, subject) | a declared level of a level slot | absent | n/a | level data |
| `hold_seconds` | f32 | above 0 and below the level's declared Run length (30 s by default); required when any `held` entry exists | none (**proposed** 2.0 for level 89) | s of game time | level data |

- **Unsafe state: a forbidden event.** If the forbidden observation occurs, the attempt is failed until Reset, whatever else happens (ENGINE-GOALS specific acceptance). The Domino's toppling is its declared orientation latch; a burst Balloon is its `IntegrityBroken` observation. The goal declares no sensor, region or tilt limit of its own: packages declare observations and the level selects them (contract Design Notes, point 3).
- **Unfinished state: a held level.** A `held` entry is true while its committed level is true, and carries the committed occurrence at which it last became true (owner decision 12: a held level is a committed level with its became-true occurrence). `Residing` is true while the subject's residence phase for that sensor is `Qualified`; the residence read already records the phase and a start ordinal and phase (`CuriousContraptions.Abstractions/ResidenceRead.cs:6-8`), but it is not on the committed `WorkshopRead` (`CuriousContraptions.Abstractions/WorkshopRead.cs:6-11`), so the owning slice publishes the level and its became-true occurrence there.
- **Solved.** The goal is Solved in the first committed read in which no forbidden observation has occurred, every latch condition holds, every `held` entry is true, and the read's time is at least `hold_seconds` after the latest of: every latch occurrence and every held entry's became-true occurrence. The evaluator keeps no state; a time wake is scheduled for that due time. A level that drops and returns has a later became-true occurrence, so the hold restarts from the committed read alone, even when the drop fell between two wakes. The occurrence is the due time. Delivery alone therefore cannot solve the level.
- **After Solved.** Solved is shown once and is not withdrawn (S635); the hold is what makes the state "final".
- **Bodies, colliders, joints, ports, stores, package parameters:** none.

### Art and solidity

No body, collider or art node in the scene. The goal card (HUD) shows one mark per protected entry: navy outline while waiting, a gold ring that fills during the hold, and a shaped warning mark (not colour alone) when a forbidden event has occurred (cream `#fff8e9`, navy `#293954`, gold `#f7cb52`; [gap-16 visual style](../requirements.md#gap-16)). The filling ring distinguishes it from the other three goal cards. The requirement's "small on-object gauges" are UI overlays that follow a body's committed pose; they have no collider, and nothing reads them.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol | Exercising test (body opened unless noted) |
| --- | --- | --- | --- | --- |
| 1 | Committed capture latch | present | `CuriousContraptions.Abstractions/CaptureLatch.cs:9-10` | `PayloadInstanceTests.AReceiverCapturesEitherKindAndAShiftedMissNever` |
| 2 | A body's orientation threshold published as an activation latch a goal can read | present | `engine/gpu/WorkshopGoalEvaluator.cs:43-50` | `DominoEffectTests.GoalSolvesOnlyWhenTheEndDominoAndTheLampAreBothLatched` (an `OrientationSource` node) |
| 3 | Committed integrity break a goal can read, cleared by Reset | present | `WorkshopGoalEvaluator.cs:37-40` | `BalloonTests.RupturePrecedesTimedPartialLossAndPauseStepResumeResetOwnTheOpenStore` (closing assertions read) |
| 4 | Residence phase (`Outside`, `Dwelling`, `Qualified`) per sensor and subject | narrower: computed with a start ordinal and phase and committed in the interaction snapshot, not carried by `WorkshopRead` | `CuriousContraptions.Abstractions/ResidenceRead.cs:6-8`; `engine/gpu/PhysicsInteractionSnapshot.cs:44-45`; `CuriousContraptions.Abstractions/WorkshopRead.cs:6-11` | none found that reads the phase through a committed read |
| 5 | Forbidden event with a failed-until-Reset phase | missing | `engine/gpu/WorkshopGoalEvaluator.cs:5` has `NotApplicable`, `Waiting`, `Solved` only | named slice `engine-goal-ordered-events` |
| 6 | Held level on the committed read with its became-true occurrence, a goal hold duration, and one time wake at the due time | missing | the goal wake is event-driven only (`engine/gpu/WorkshopGoalWake.cs:28-49`, as the contract cites it) | ENGINE-GOALS slice `engine-goal-count-window-hold` (owner decision 12) |
| 7 | Several conditions per goal | missing | `engine/gpu/WorkshopPuzzle.cs:74-77` | drafted slice `engine-goal-evaluation` |
| 8 | Hold progress and failed state on the observation port and HUD | missing | `engine/gpu/WorkshopObservationProjection.cs:84-93` | named slice `engine-goal-progress-observation` |
| 9 | A Rope-domain connection cut commanded during a Run, committed at the next step and restored by Reset | present | `WorkshopCommandKind.CutRope` (`CuriousContraptions.Abstractions/WorkshopMessageValues.cs:12`) | `RopeTests.CutWaitsForCommittedStepAndSurvivesDiscardThenResetRestoresTheRope` |
| 10 | A declared thin-string connection material (a tensile strand with no strand collider), the Balloon string | missing | a Rope-domain connection carries only a measured length (`engine/gpu/WorkshopConnections.cs:166`); after `ENGINE-CONNECTION-MATERIALS` the delivered rope is the first declared material and is solid | `ENGINE-CONNECTION-MATERIALS` (thin-string material; owner decision 1, coordinator decision 25) |

Counts: present 4; present-untested 0; narrower 1; missing 5. Every missing row has a named slice. ENGINE-GOALS' text in `epics.md` does not name a held state; the proposed text is in the batch summary. Whether publishing the residence level changes the worker read ABI, and which of the read's start and event stamps is the became-true occurrence of the current `Qualified` spell, are for that slice's probe. Under `ENGINE-SENSOR-VOLUMES` residence is evaluated inside the tick against a declared occupancy rule; the level this goal reads is that story's committed result.

Stale facts corrected from the earlier text: goal-owned regions, a goal-owned tilt limit and a scene-wide settle detector are not planned (section 6); orientation sensing is declared by the package, not compiled only for wired Dominoes as the harvest baseline had it.

## 4. Sources and legacy

- **Requirements.** Row: "Requires the observed final machine state to remain within declared bounds"; outcome "Delivery alone cannot bypass an unsafe or unfinished terminal state" ([element-123](../requirements.md#element-123)). Integration: an unsafe final state is the near-miss control; reject premature success ([gap-16](../requirements.md#gap-16)).
- **Audit.** "protected end-state constraints as typed goal variants. Reject transient false wins" (`docs/physics-puzzle-gap-audit.md@a6c914e:L95-L95`).

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Legacy escape bounds: a body with centre y < −5 or y > 20, \|x\| > 18 or \|z\| > 12 was hidden, its collider disabled, and an `Escaped` event recorded once. | `reference/cpu/MachineWorld.cs@a6c914e:L903-L916` | Do not carry forward: a per-tick loop over every body. An escaped body fails a `held` entry instead (it is no longer residing). |
| 2 | A ball, Bowling ball or Tennis ball dropped on the bench bounces and settles below 0.05 m/s. | `CuriousContraptions.tests/FloorTests.cs@a6c914e:L26-L51`, `CuriousContraptions.tests/FloorTests.cs@a6c914e:L53-L67` | Carry forward as context for the hold duration. |
| 3 | Legacy `Won` latched at the first tick when all goals were met and stopped the run; nothing checked the state afterwards. | `reference/cpu/MachineWorld.cs@a6c914e:L921-L925` | Do not carry forward: this is the premature success the hold forbids. |
| 4 | The orientation threshold admitted only 4°–179° because of half-precision rounding. | `engine/gpu/OrientationSensorDeclaration.cs@a6c914e:L11-L12` | Do not carry forward; the threshold belongs to the declaring package. |

Files consulted: `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/FloorTests.cs`.

## 5. Acceptance outline

Point of truth: [element-123](../requirements.md#element-123) and the [gap-16 integration](../requirements.md#gap-16).

### Behaviour and numbers

- **Law.** A conjunction of latches, the absence of forbidden latches, and levels that have stayed true for a declared game time. Evaluated on the main thread over the committed read, after the tick's sensor phase (Phase 5) and commit ([solver model](../../gpu-f32-physics.md#solver-model)). Nothing flows back; the goal never stops, slows or changes the Run.
- **Why 2.0 s (proposed).** The hold must outlast the bouncing of a delivered ball. A Basketball (restitution 0.55, `elements/Ball/BallElementDeclaration.cs:38-41`) dropped 1.0 m lands at √(2 × 9.81 × 1.0) = 4.43 m/s. Its flights after the first impact last 2 × 4.43 × 0.55ⁿ / 9.81 = 0.50 s, 0.27 s, 0.15 s …, a geometric series summing to 0.497 / (1 − 0.55) = 1.10 s. Two seconds is nearly twice that, and short enough that the player does not wait long for Solved.
- **Scenario fixture.** Receiver A; a Tennis ball with `bounce` 0 dropped 1.0 m into it (lands at 0.45 s, latches near 0.80 s after the 0.35 s dwell); a Basketball resting in Receiver B from the start (its residence qualifies after the 0.35 s dwell); a standing Domino as the forbidden observation.
  - *Positive.* All conditions first hold together near 0.80 s, so Solved is expected near 0.80 + 2.0 = 2.8 s. The test asserts Waiting at 1.8 s (1.0 s early) and Solved by a 10 s guard, not an arrival tick.
  - *Unfinished control.* Receiver B's `capture_participation` set to Disabled: the level is never true and the goal stays Waiting although the delivery latched.
  - *Dropped-level control.* The held subject is a Balloon at its smallest `radius`, 0.18 m, with `buoyancy` 11.5 m/s² and `drag` 0.4 1/s at their defaults (`elements/Balloon/BalloonElementDeclaration.cs:23-25`), tied at its centre (`:37-38`) by a 0.25 m Balloon string to a Rope anchor inside Receiver B. **The tie is the Balloon string, not a rope.** It is a Rope-domain connection of the declared **thin-string** material of `ENGINE-CONNECTION-MATERIALS`, a tensile strand that declares no strand collider: owner decision 1's Balloon-string exception, held as material data, not a rule naming a part (coordinator decision 25). A rope is a thick strand and solid (owner decision 1), and is not used here: a solid strand from the tie to the Balloon's centre would run through the envelope's collider for its last 0.265 − 0.085 = 0.18 m. Geometry by hand, in Receiver B's frame: the floor's top is at y = −0.375 (floor box 0.15 m thick centred at y −0.45, `elements/Receiver/basket.tscn`), and the residence sensor spans y −0.40 to 0.45 plus the 0.02 m `capture_margin` (`elements/Receiver/ReceiverElementDeclaration.cs:13`, `:44`), so its top is 0.47. The anchor is laid flat with its tie up and its 0.16 m mount box 0.01 m above the floor (a static part needs no support); the box spans local z −0.20 to −0.04 and the tie is at local z +0.18 (`elements/RopeAnchor/rope_anchor.tscn:20`, `:33`; `elements/RopeAnchor/RopeAnchorElementDeclaration.cs:17-18`), so the box's top is at −0.205 and the tie at −0.365 + 0.20 + 0.18 = 0.015. The taut string holds the Balloon's centre at 0.015 + 0.25 = 0.265: the envelope spans 0.085 to 0.445, 0.07 m above the tie, 0.29 m above the anchor box and under the walls' tops (0.50), and the centre is 0.47 − 0.265 = 0.205 m below the sensor's top, so it qualifies. The default 0.36 m Balloon is not used: any string long enough to keep the tie outside its envelope (over 0.36 m) would put its centre within 0.015 m of the sensor's top. With the string intact the goal solves as in the positive case. With the string cut at 1.2 s by the production cut command (`WorkshopCommandKind.CutRope`, which cuts a Rope-domain connection whatever its material), the Balloon accelerates upward at 11.5 − 9.81 = 1.69 m/s² and clears the 0.205 m after about √(2 × 0.205 / 1.69) = 0.49 s (0.51 s with its 0.4 1/s drag: 4.225 × (0.51 − (1 − e^(−0.204)) / 0.4) = 0.206 m), near 1.7 s: 1.1 s before the hold would be due at 2.8 s. It never returns: the Receiver's lateral guide is off at its default (`force_maximum_acceleration` 0, `ReceiverElementDeclaration.cs:49`) and nothing above pushes it down, so the goal stays Waiting for the whole guard. A ball is not used for this control because no delivered part ejects a ball from the five-box Receiver by a hand-checkable margin.
  - *Unsafe control.* A Bowling ball rolled into the Domino so it topples: Failed, and it stays Failed after the delivery.
- **Exact hold boundary** is proven on the production evaluator with constructed committed reads: a read exactly `hold_seconds` after the latest became-true occurrence solves and one phase unit earlier does not; a level that drops and returns between two wakes shows a later became-true occurrence and restarts the hold.
- **Run length.** The positive case solves near 2.8 s and every guard is 10 s, inside the default 30 s Run (`CuriousContraptions.Abstractions/CadenceValues.cs:77`). A hold must be shorter than the level's declared Run length or the goal can never be solved; admission refuses one that is not. Level 89 follows the thermal lessons, whose process times may exceed 30 s; if its authored solution does, the level declares the Run length it needs and `ENGINE-RUN-LENGTH` (owner decision 13) is a prerequisite of that level slice.

### Acceptance rows

Planned test class `GoalHeldStateTests` (slice `engine-goal-count-window-hold`), one row in `GoalOrderTests` (forbidden event) and `EndStateLessonTests` (level 89). Chrome proof is pending the post-catalogue browser phase.

| Row | Given / When / Then | Planned test | Control | Boundary | Tolerance and guard |
| --- | --- | --- | --- | --- | --- |
| P1 | Given the fixture, when run, then Waiting at 1.8 s and Solved within the guard | `GoalHeldStateTests.DeliveryPlusAHeldStateSolvesAfterTheHold` | n/a | n/a | 1.0 s early check; 10 s guard |
| P2 | Given the unfinished control, then Waiting for the whole guard although the capture latched | `GoalHeldStateTests.DeliveryAloneDoesNotSolve` | P1 (one cause: the sensor's participation) | n/a | 10 s guard |
| P2b | Given the dropped-level control, when the Balloon string is cut during the hold, then the goal is Waiting for the whole guard; with the string intact it is Solved | `GoalHeldStateTests.ASubjectThatLeavesDuringTheHoldLeavesTheGoalWaiting` | the string intact (one cause: the cut) | n/a | 10 s guard; the level drops 1.1 s before the hold is due |
| P3 | Given the unsafe control, then Failed in the read where the forbidden observation is latched, and still Failed at the guard | `GoalOrderTests.AForbiddenEventFailsTheAttemptUntilReset` (run on this fixture) | P1 | the forbidden latch after Solved does not withdraw Solved | Phases exact |
| P4 | Given constructed reads where a level turns false before the hold completes and true again, including a drop and return that fall between two evaluations, then Solved comes one full hold after the later became-true occurrence and the evaluator holds nothing between reads | `GoalHeldStateTests.ABreakRestartsTheHold` | unbroken level | exactly `hold_seconds`; one phase unit short | Exact at the constructed boundary |
| P5 | Given content with a `held` entry and no `hold_seconds`, a hold of 0, below 0, not shorter than the level's declared Run length or non-finite, a `held` selector naming an undeclared level, or a `forbidden` entry equal to a required condition, when loaded, then refused with a typed reason; the installed catalogue is unchanged | `GoalHeldStateTests.MalformedProtectedStatesAreRefusedAtomically` | a valid declaration loads | one phase unit below the Run length admitted | Typed reason exact |
| P6 | Given a Failed or Solved goal, when Reset then Run, then Waiting with the hold at 0, and the positive case solves again | `GoalHeldStateTests.ResetClearsFailureAndHold` | n/a | n/a | Authored state restored |
| P7 | Given a solved construction saved and loaded, then the decoded goal equals the saved one; hold progress is not stored; a fresh run solves | `GoalHeldStateTests.SaveAndLoadPreserveTheProtectedState` | a save whose goal differs from the authored one is refused | n/a | Decoded field equality |
| P8 | Given authored level 89, its solution solves, and a construction that delivers but leaves the protected state unsafe does not | `EndStateLessonTests.AuthoredLessonSolvesAndAnUnsafeFinishDoesNot` | unsafe final state | n/a | the whole declared Run (30 s default) |

Wrong-kind connection and player-parameter refusals do not apply (no port, no player parameter); P5 is the refusal row.

### Integration

- **Collaborators.** Receiver, Domino, Balloon, balls (delivered). Outcome-predicate task [sequence-task-361](../requirements.md#sequence-task-361) ("broken", "arrived safely") is met by observations the cargo packages declare; a treated state (coated, dyed) is a committed first-occurrence `Treated` observation published by `ENGINE-SURFACE-STATE`, which a goal list selects as one more observation kind, alone or as a predecessor; it is a latch, not a held level and not a qualifier on another kind, and it is added by that story, not by this identity.
- **Campaign.** Introduction 89 "Leave It Lovely" (after thermal control), practice 90, reuse 100 and 150 ([GAP-16 allocation](../requirements.md#campaign-gap-allocation)). Cross-element task [sequence-task-549](../requirements.md#sequence-task-549).
- **Lesson fixtures.** Locked authored placements only.

### Delivery

- **Shape.** Blocked on the engine programme (owner decision 3a). Then row P3 inside `engine-goal-ordered-events`; rows P1, P2, P2b and P4 to P7 inside `engine-goal-count-window-hold`; P8 is a level slice. No package.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in which `ENGINE-SENSOR-VOLUMES` defines the residence level this goal reads; S5; `engine-goal-evaluation`; `engine-goal-ordered-events`; `engine-goal-count-window-hold`; `engine-goal-progress-observation` before the card; `ENGINE-CONNECTION-MATERIALS` (the thin-string material) for row P2b's Balloon string; `ENGINE-RUN-LENGTH` only if level 89 needs more than 30 s.
- **Files.** The contract's owned scope, plus the committed read that carries the residence phase.
- **Review tier.** Engine.
- **Disqualifies the plan.** Level 89, when authored, needs a protected state no delivered package declares as an observation (then that package gains the declaration first); the residence phase cannot be read without a worker ABI change (the slice then owns that change, as `engine-goal-supplied-occurrence` does for its read); a tethered Balloon does not qualify as residing, or a static part cannot be placed inside the Receiver just above its floor (row P2b then needs another subject that leaves by a checked margin).

## 6. Open questions

**Resolved.**

1. *Settle rule or authored end time?* Neither: a declared hold duration (decided now). A settle detector scans every dynamic body each tick and never fires for a machine with a running Motor; a hold reads only the declared levels.
2. *Escape bounds.* No world-bounds observation is added (D19): a body that leaves is no longer residing, which fails its `held` entry.
3. *Region and tilt predicates owned by the goal.* Not declared (decided now): a goal-owned sensor would be a second sensor mechanism beside the packages' declared ones.
4. *Material-state predicates.* The committed `Treated` observation of `ENGINE-SURFACE-STATE`, selected like any other latch kind (coordinator decision, 22 November 2026); no qualifier and no second condition form.
5. *Where does the hold's start live?* In the committed read, as each held level's became-true occurrence (owner decision 12); the evaluator keeps nothing between reads.
6. *Which body proves a dropped level?* A Balloon tied by its string and released by cutting the string, not a Basketball knocked out of the Receiver (decided now: the Balloon's exit has a hand-checked 1.1 s margin and uses a tested command; no delivered part ejects a ball from the Receiver by a checkable margin). The tie is the thin-string material, never a rope, because a rope is solid (owner decision 1) and would run through the envelope (section 5, dropped-level control).

**Needs the owner.** None.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026, `_bmad/custom/element-free-engine-design.md` and `docs/gpu-f32-physics.md`.

1. **Declaration data only.** Level data: selectors and one duration. No per-level or per-part code, no settle loop.
2. **Generic laws.** A forbidden latch and a level held for a declared time. Both are general, stated element-free, each with one owning ENGINE-GOALS slice.
3. **Three layers.** Colliders none; visuals none in the scene (HUD card and pose-following UI gauges); animation none.
4. **Flow and typing.** Committed read → main-thread evaluator → cue; nothing flows back. Kinds are closed enums; ids are typed; the duration is f32 seconds.
5. **Flat state.** Bodies 0; colliders 0; joints 0; drives 0; sensors 0 of its own; domain nodes 0; stores 0; bindings 0. A fixed list of selectors and one hold duration per goal; the evaluator keeps nothing between reads.
6. **Performance.** Woken by the event ring for the owners it names and by one due time for the hold; no per-tick scan of the scene. The passes that produce its observations are owned by the engine programme of owner decision 3a: residence and guide scans by `ENGINE-SENSOR-VOLUMES` with candidates from `ENGINE-SPATIAL-QUERIES`, and contact events and integrity observers by `ENGINE-CONTACT-OBSERVATIONS`. Nothing here designs around them.
7. **Tests are scenarios.** P1 to P3, P2b, P6 and P8 run the production simulation; P4, P5 and P7 run the production evaluator, reader and codec.

**Verdict: conforms; blocked on the engine programme (owner decision 3a) and on ENGINE-GOALS for general capabilities stated element-free.**

## 7. Coverage binding (planned, not yet live)

No package, so no spec binding section is planned and none is written. The identity's task source `sequence-task-676` is to be satisfied by the tests of the two engine slices and of level 89, cited from the ledger entry. S7, which widens the reader (owner decision 15, 22 November 2026), supplies the closure rule for an identity with no package (batch summary, section 4 item 16).
