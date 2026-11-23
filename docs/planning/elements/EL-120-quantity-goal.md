# EL-120 · Quantity goal — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-120 delivery slice](../invest/named-elements.md#element-120). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code is cited as `path:line` in that checkout; `path@a6c914e` citations appear only in section 4 and describe the harvest baseline. Values marked **proposed** are starting values ([decision D1](../invest/delivery-batches.md#3-family-level-decisions)). Nothing here was built or run.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-120 · Quantity goal · Goal |
| Anchor | [requirements.md#element-120](../requirements.md#element-120) (task `sequence-task-673`); [named-elements entry](../invest/named-elements.md#element-120); umbrella [gap-16](../requirements.md#gap-16) (index only) |
| Owner key | S674 |
| Related identities | Siblings [EL-121](EL-121-rate-window-goal.md) (the same count inside a time window), [EL-122](EL-122-ordered-events-goal.md), [EL-123](EL-123-protected-end-state-goal.md). Deliveries are sensed by the delivered Receiver ([CAT-004](CAT-004-basket.md), [EL-192](EL-192-receiving-basket.md)). No CAT refines it. |
| Engine story | ENGINE-GOALS ([epics](../../../_bmad-output/planning-artifacts/epics.md), "Story ENGINE-GOALS") |
| Status | Not started; blocked on the engine programme (owner decision 3a) and on ENGINE-GOALS. The engine evaluates one goal per level (`engine/gpu/WorkshopPuzzle.cs:9`, `:12-13`). |

### Disposition

**Level data, not a part.** A quantity goal is one entry in a level's declared goal list, evaluated by the one general goal evaluator. It gets no package, no directory, no palette entry and no catalogue key. It is blocked on the engine programme (owner decision 3a). After it, the count group ships as the any-of form of the ENGINE-GOALS slice `engine-goal-ordered-events` (owner decision 12, 22 November 2026), and the identity is delivered by that slice plus the level that first uses it (campaign level 25). It is not a Family slice member and not a Duplicate closure: all-of composition cannot express "any three of five".

## 2. Declaration

### Declaration to write

The level's `goals` list is the one defined by the drafted contract [`spec-engine-goal-evaluation.md`](../../../_bmad-output/implementation-artifacts/spec-engine-goal-evaluation.md): an ordered list of conditions, each a typed observation selector (`Captured`, `Activated`, `IntegrityBroken`, `TravelReached`) that is a first-occurrence latch until Reset. The quantity goal uses one list entry form, a **count group**, which `engine-goal-ordered-events` ships as its any-of form (any-of is the group with `at_least` 1; there is one group form):

```json
{"at_least": 3, "of": [
  {"kind": "Captured", "owner": "fixture_5", "subject": "fixture_1"},
  {"kind": "Captured", "owner": "fixture_5", "subject": "fixture_2"},
  {"kind": "Captured", "owner": "fixture_5", "subject": "fixture_3"},
  {"kind": "Captured", "owner": "fixture_5", "subject": "fixture_4"}
]}
```

The JSON above is indicative; the slice fixes the field names in one content shape and one save shape.

| Field | Type | Range | Default | Unit | Editor policy |
| --- | --- | --- | --- | --- | --- |
| `at_least` | u32 | 1 to the member count | none (authored) | items | level data; not player-editable |
| `of` | list of conditions | at least 1 member; members distinct; within one group no two `Captured` members share a subject | none | n/a | level data |

- **Unit.** The unit is the item: one member that holds. For `Captured` members that is one distinct body captured by the named sensor owner. Mass and volume units belong to the liquid and granular stories and are not declared here.
- **Group truth.** The group holds in the first committed read in which at least `at_least` members hold. Its occurrence is the `at_least`-th earliest member occurrence. Member order never changes phase or occurrence.
- **No inflation.** Each (sensor, subject) pair is one first-occurrence latch (`CuriousContraptions.Abstractions/CaptureLatch.cs:9-10`). A ball that bounces out and back, or rests and is sensed for many ticks, holds one member once. The distinct-subject rule stops one ball counting twice through two members.
- **Owners and subjects** are level slots resolved to typed identities at content load, exactly as the contract resolves `owner` and `subject`. No `GpuBodyId` parameter is authored and no part identity appears in engine code.
- **Bodies, colliders, joints, ports, stores, parameters of a package:** none. The goal declares no sensor of its own: the Receiver package declares the residence sensor (`elements/Receiver/ReceiverElementDeclaration.cs:12-17`).

### Art and solidity

There is no body, collider or art node. The goal card (HUD) shows one navy dot per required item, filling gold as the group's count rises, in cream `#fff8e9`, navy `#293954` and gold `#f7cb52` ([DESIGN.md](../../../DESIGN.md), [gap-16 visual style](../requirements.md#gap-16)). The card reads committed goal progress and writes nothing back. It is distinguished from the rate-window card by having dots only and no window bar.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol | Exercising test (body opened unless noted) |
| --- | --- | --- | --- | --- |
| 1 | Committed capture latch per (sensor, subject) with an occurrence | present | `CuriousContraptions.Abstractions/CaptureLatch.cs:9-10`; `engine/gpu/WorkshopGoalEvaluator.cs:30-36` | `PayloadInstanceTests.AReceiverCapturesEitherKindAndAShiftedMissNever` |
| 2 | One sensor owner admits every payload body; several owners coexist with several payloads | present | `engine/gpu/CompiledIdentityMap.cs:81-108` | `PayloadInstanceTests.AReceiverDeclaresOnePayloadPolicyWhateverTheInstanceOrder`; `PayloadInstanceTests.TwoCaptureOwnersAdmitWithEitherPayloadKindAndNonPayloadBodies` |
| 3 | Goal truth from the committed read only, main thread | present for one goal | `engine/gpu/WorkshopGoalEvaluator.cs:10-26` | `DelayedSignalTests.GoalUsesTargetOccurrenceNotRootCauseOrTimerAndRejectsEarlyLatch` |
| 4 | Reset returns a goal to Waiting | present | worker Reset clears observations; evaluator derives phase each read | `BalloonTests.RupturePrecedesTimedPartialLossAndPauseStepResumeResetOwnTheOpenStore` (integrity kind; the capture case is named by the contract's probe and was not opened) |
| 5 | Several conditions in one goal (all-of) | missing | `engine/gpu/WorkshopPuzzle.cs:74-77` holds one `WorkshopGoal` | owned by drafted slice `engine-goal-evaluation` |
| 6 | Count group: at least k of n declared conditions, occurrence = k-th earliest | missing | none | ENGINE-GOALS slice `engine-goal-ordered-events` (owner decision 12) |
| 7 | Group progress (count, required) on the observation port and HUD | missing | `engine/gpu/WorkshopObservationProjection.cs:84-93` projects no phase | owned by `engine-goal-progress-observation`; it must carry a group's count |
| 8 | Save, load and typed refusal of goal data | present for one goal | `engine/gpu/WorkshopSavePrevalidation.cs:161-223` | `GoalSignalSelectorTests.AuthoredIntegrityGoalBindsTheDeclaredObservationAndCurrentSaveField` |

Counts: present 2; present for one goal 2 (narrower); missing 3; present-untested 0. Every missing row has a named slice. ENGINE-GOALS' text in `epics.md` lists "all-of first, then any-of, ordered and forbidden events" and does not name a count threshold; the owner decided on 22 November 2026 (decision 12) that `engine-goal-ordered-events` ships any-of as the count group, so there is one group form from its first delivery. The fifth slice, `engine-goal-count-window-hold`, adds the window of [EL-121](EL-121-rate-window-goal.md) and the hold of [EL-123](EL-123-protected-end-state-goal.md) to the same group and re-runs rows Q1 to Q4 with them; it adds no second counting rule. The proposed story text is in the batch summary.

Stale facts corrected from the earlier text: a second Receiver beside existing balls is admitted (row 2); no sixteen-sensor ceiling was found in this checkout (the old capacity file is gone; `NoFixedCapacityTests` exists, its sensor coverage was not opened).

## 4. Sources and legacy

- **Requirements.** Row: "Counts distinct supported deliveries in explicit units"; outcome "Repeated sensing of one retained item cannot inflate totals" ([element-120](../requirements.md#element-120)). Integration: positive and near-miss (insufficient quantity); reject double counting and premature success; goals stay outcome-based ([gap-16](../requirements.md#gap-16)).
- **Audit.** "Individually specify quantity ... Reject transient false wins, double-counting" (`docs/physics-puzzle-gap-audit.md@a6c914e:L95-L95`).
- **Decisions.** [S635 goal-occurrences](../invest/decisions.md#s635): one level per variant; duplicate routes do not solve; Solved shows once.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Legacy events were a dictionary keyed by (kind, target, body) filled with `TryAdd`, so each event counted once at its first tick. | `reference/cpu/MachineWorld.cs@a6c914e:L915-L915`, `reference/cpu/MachineWorld.cs@a6c914e:L928-L942` | Carry forward the once-per-identity rule. |
| 2 | Legacy had no count goal; multi-delivery levels listed one `captured` goal per basket and required all. | `reference/cpu/MachineWorld.cs@a6c914e:L921-L942`; `content/puzzles.json@a6c914e:L8205-L8673` (`two_deliveries`) | Carry forward as context; a count group is new. |
| 3 | Unknown goal kinds reject before the world is replaced; goals are immutable and round-trip through save. | `CuriousContraptions.tests/ObjectiveOwnershipTests.cs@a6c914e:L18-L83` | Carry forward. |
| 4 | Goal kinds were a string-converted enum (`GoalKindJsonConverter`). | `engine/MachineData.cs@a6c914e:L151-L154` | Do not carry forward the JSON string form; the enum stays typed end-to-end with conversion only at the content and save boundaries. |

Files consulted: `reference/cpu/MachineWorld.cs`, `engine/MachineData.cs`, `CuriousContraptions.tests/ObjectiveOwnershipTests.cs`, `content/puzzles.json`.

## 5. Acceptance outline

Point of truth: [element-120](../requirements.md#element-120) and the [gap-16 integration](../requirements.md#gap-16).

### Behaviour and numbers

- **Law.** Threshold over first-occurrence latches: count = number of members that hold; Solved when count ≥ `at_least`. Evaluation is on the main thread over the committed read, after the tick's sensor phase and commit ([solver model](../../gpu-f32-physics.md#solver-model), Phase 5 then commit). Nothing flows back to the worker.
- **How many balls one Receiver holds.** The Receiver's inner faces are at ±0.69 m (side boxes centred at ±0.75 m, 0.12 m thick: 0.75 − 0.06) and its floor top is at −0.375 m (floor box centred at −0.45 m, 0.15 m thick), from `elements/Receiver/basket.tscn`. The sensor volume is ±0.66 m in x and z and −0.4 m to 0.45 m plus a 0.02 m margin in y (`ReceiverElementDeclaration.cs:13`, `:43`).
  - Tennis ball, radius 0.25 m: a centre rests at −0.375 + 0.25 = −0.125 m. Four fit on the floor (2 × 0.5 = 1.0 m ≤ 1.38 m) with centres inside the volume.
  - Basketball, radius 0.34 m: two side by side need 1.36 m of 1.38 m; a third resting on two has its centre at −0.035 + √(0.68² − 0.34²) = −0.035 + 0.589 = 0.554 m, above the 0.47 m top. One Receiver reliably counts two Basketballs, not three.
  - So the first lesson uses Tennis balls and target 3 (**proposed**: the smallest count that is clearly not "one or two"). This depends on the sensor's occupancy rule. Today residence tests the body's origin point. `ENGINE-SENSOR-VOLUMES` replaces that test with a declared occupancy rule (centre containment or shape overlap), so the Receiver's sensor must declare **centre containment**: with shape overlap a third Basketball resting on two would count, and so would a ball perched on the rim. Rows Q1, Q2 and Q4 state their counts under centre containment and are re-run when that story lands.
- **Fixture timing.** A Tennis ball with `bounce` 0 dropped 1.0 m lands after √(2 × 1.0 / 9.81) = 0.45 s and qualifies after the default 0.35 s dwell, so it latches near 0.8 s. The stall guard is 10 s of game time, more than ten times that.
- **Card capacity.** The dot row is designed for up to nine required items (**proposed**, a content rule for readability; the engine sets no ceiling, as the contract sets none on conditions).
- **Run length.** Every fixture row latches within 1 s and uses a 10 s guard, and level 25 is planned inside the default 30 s Run (`CuriousContraptions.Abstractions/CadenceValues.cs:77`). No row needs a longer Run, so `ENGINE-RUN-LENGTH` (owner decision 13) is not a prerequisite; a level that reuses the goal with a slower feed declares the Run length it needs.

### Acceptance rows

Planned test class `GoalCountGroupTests` (engine slice) and `QuantityLessonTests` (level 25). Chrome proof is pending the post-catalogue browser phase.

| Row | Given / When / Then | Planned test | Control | Boundary | Tolerance and guard |
| --- | --- | --- | --- | --- | --- |
| Q1 | Given a Receiver, four dead Tennis balls and a group `at_least` 3 of four captures, when three balls are dropped into the Receiver, then the goal is Waiting until the third latch and Solved in the first committed read where three hold; its occurrence is the third-earliest member occurrence | `GoalCountGroupTests.SolvesAtTheKthDistinctCapture` | Two balls delivered: Waiting for the whole guard (insufficient quantity) | `at_least` equal to the member count; `at_least` 1 | Counts and phases exact; 10 s guard |
| Q2 | Given Q1 with one ball of `bounce` 0.85 that enters, leaves and re-enters, when it is the only delivery, then the count is 1 | `GoalCountGroupTests.ARepeatedlySensedItemCountsOnce` | The same ball resting for 5 s: count stays 1 | n/a | Count exact |
| Q3 | Given the Q1 members declared in every other order, then phase at every read and the occurrence equal Q1's | `GoalCountGroupTests.MemberOrderDoesNotChangePhaseOrOccurrence` | n/a | n/a | Exact occurrence equality (same latches) |
| Q4 | Given a non-member body captured by the Receiver, then the count does not change | `GoalCountGroupTests.ANonMemberCaptureDoesNotCount` | Q1 | n/a | Count exact |
| Q5 | Given content with `at_least` 0, above the member count, a missing or empty `of`, a duplicate member, two `Captured` members with one subject, or an unknown field, when loaded, then it is refused with a typed reason naming the puzzle and field and the installed catalogue is unchanged | `GoalCountGroupTests.MalformedGroupsAreRefusedAtomically` | A valid group loads | 1 and the member count are admitted | Typed reason exact |
| Q6 | Given a solved group, when Reset then Run, then it is Waiting with count 0 and solves again | `GoalCountGroupTests.ResetClearsTheCountAndItSolvesAgain` | n/a | n/a | Authored state restored |
| Q7 | Given a solved construction saved and loaded, then the decoded goal equals the saved goal, the runtime count is not stored, and a fresh run solves | `GoalCountGroupTests.SaveAndLoadPreserveTheGroup` | A save whose group differs from the authored one is refused | n/a | Decoded field equality |
| Q8 | Given the authored level 25, when its intended solution runs, then it solves; with one required part removed it does not | `QuantityLessonTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` | one-cause control | n/a | the whole declared Run (30 s default) |

A wrong-kind connection refusal and a parameter refusal do not apply: the goal has no port and no player parameter. Q5 is its refusal row.

### Integration

- **Collaborators.** Receiver and the ball packages (all delivered). The Counter ([CAT-020](CAT-020-counter.md)) may show an in-scene readout but is not part of goal truth.
- **Campaign.** Introduction 25 "A Steady Job", practice 26, reuse 96 and 136 ([GAP-16 allocation](../requirements.md#campaign-gap-allocation)). Cross-element task [sequence-task-549](../requirements.md#sequence-task-549): insufficient quantity is this variant's near-miss.
- **Lesson fixtures.** Level 25 fixes the Receiver and the balls as locked authored placements, which today's level shape supports; it needs S8 only if it fixes a package no level has fixed.

### Delivery

- **Shape.** Blocked on the engine programme (owner decision 3a). Then rows Q1 to Q7 are acceptance rows of the engine slice `engine-goal-ordered-events`, followed by a level slice for 25. No element package, so no registration, palette or coverage-membership edit.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in which `ENGINE-SENSOR-VOLUMES` fixes the occupancy rule the counts depend on; S5 (the contract's prerequisite); `engine-goal-evaluation`; `engine-goal-ordered-events`; `engine-goal-progress-observation` before the card's dots.
- **Files.** The goal model, evaluator, wake, content reader, save schema and pre-validation, projection and TypeScript read model named in the contract's owned scope; `content/puzzles.json` for the level.
- **Review tier.** Engine (all six layers on the first pass).
- **Disqualifies the plan.** The Receiver's sensor declares shape overlap rather than centre containment (the Tennis and Basketball counts change); the ordered-events slice ships any-of as a form other than the count group (contrary to owner decision 12; rewrite to one form first).

## 6. Open questions

**Resolved.**

1. *Does a delivered body that later leaves still count?* Yes. Every observation is a first-occurrence latch until Reset (contract, "Composite occurrence"). A goal that needs bodies to stay uses the held-state condition of [EL-123](EL-123-protected-end-state-goal.md).
2. *Can a quantity span several Receivers?* Yes, as members with different owners, but one subject may appear once per group (decided now: it keeps "cannot inflate totals" true without a second counting rule).
3. *Units beyond the item?* Not declared (D19): mass and volume counts arrive with ENGINE-GRANULAR and ENGINE-LIQUID observations.
4. *Target and candidate ranges.* Starting values under D1.

**Needs the owner.** None.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026, `_bmad/custom/element-free-engine-design.md` and `docs/gpu-f32-physics.md`.

1. **Declaration data only.** One level-data entry. No solver, kernel branch, update loop, callback or script; no engine branch on a level, part or package id.
2. **Generic laws.** A threshold over committed first-occurrence observations. The missing general capability (count group) is stated element-free with one owner, ENGINE-GOALS.
3. **Three layers.** Colliders: none. Visuals: none in the scene; the HUD card is UI. Animation: none. The card reads committed progress only.
4. **Flow and typing.** Physics → committed read → evaluator on the main thread → cue. Nothing flows back. Observation kinds are a closed enum; owners and subjects are typed ids; strings exist only in content and save boundaries.
5. **Flat state.** Bodies 0; colliders 0; joints 0; drives 0; sensors 0 of its own; domain nodes 0; stores 0; bindings 0. One group of n member selectors in the goal list, fixed at level load.
6. **Performance.** Evaluation runs when the event-ring wake reports a member's owner, not every tick; cost is linear in the group's members, not in the scene. The sensing it reads is not free: today the residence pass tests each sensor against each admitted payload. That scan is owned by `ENGINE-SENSOR-VOLUMES`, with candidates from `ENGINE-SPATIAL-QUERIES`, both in the engine programme that precedes element delivery (owner decision 3a; `docs/planning/elements/readiness/engine-design-gap.md`, section 6). A quantity level with many candidates relies on those stories and adds no level-specific shortcut.
7. **Tests are scenarios.** Q1 to Q8 run the production simulation or the production evaluator over committed reads.

**Verdict: conforms; blocked on the engine programme (owner decision 3a) and on ENGINE-GOALS for one general capability stated element-free.**

## 7. Coverage binding (planned, not yet live)

No package, so no spec binding section is planned and none is written. Coverage must account for this identity as level data: its task source `sequence-task-673` is satisfied by the engine slice's tests and the level-25 tests, with the ledger entry citing both. The draft S7 contract treats an entry with no package line as "nothing is bound and nothing is required of it"; that leaves a Goal identity with no executable closure, which is recorded as a gap for S7 in the batch summary. S7 widens the reader (owner decision 15, 22 November 2026) and supplies that closure rule (batch summary, section 4 item 16).
