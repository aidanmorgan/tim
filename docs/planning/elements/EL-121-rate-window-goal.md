# EL-121 · Rate-window goal — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-121 delivery slice](../invest/named-elements.md#element-121). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code is cited as `path:line` in that checkout; `path@a6c914e` citations appear only in section 4 and describe the harvest baseline. Values marked **proposed** are starting values ([decision D1](../invest/delivery-batches.md#3-family-level-decisions)). Nothing here was built or run.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-121 · Rate-window goal · Goal |
| Anchor | [requirements.md#element-121](../requirements.md#element-121) (task `sequence-task-674`); [named-elements entry](../invest/named-elements.md#element-121); umbrella [gap-16](../requirements.md#gap-16) (index only) |
| Owner key | S675 |
| Related identities | [EL-120 Quantity goal](EL-120-quantity-goal.md) (the same count group; this identity adds a time window); siblings [EL-122](EL-122-ordered-events-goal.md), [EL-123](EL-123-protected-end-state-goal.md). No CAT refines it. |
| Engine story | ENGINE-GOALS ([epics](../../../_bmad-output/planning-artifacts/epics.md), "Story ENGINE-GOALS") |
| Status | Not started; blocked on the engine programme (owner decision 3a) and on ENGINE-GOALS. No goal condition has a time window (`engine/gpu/WorkshopPuzzle.cs:12-30`). |

### Disposition

**Level data, not a part.** A rate-window goal is a count group ([EL-120](EL-120-quantity-goal.md)) with a declared game-time window. No package, directory, palette entry or catalogue key. Blocked on the engine programme (owner decision 3a). After it, the group form ships in `engine-goal-ordered-events` and the window in the fifth ENGINE-GOALS slice, `engine-goal-count-window-hold` (owner decision 12, 22 November 2026); the identity is delivered by that slice and by the level that first uses it (campaign level 29). It is a variant of EL-120 with its own behaviour (the window), not a duplicate.

## 2. Declaration

### Declaration to write

One list entry in the level's `goals` (the list defined by [`spec-engine-goal-evaluation.md`](../../../_bmad-output/implementation-artifacts/spec-engine-goal-evaluation.md)): a count group with a `window`. Indicative shape; the slice fixes the names once.

```json
{"at_least": 3,
 "window": {"from_seconds": 5, "length_seconds": 10},
 "of": [ {"kind": "Captured", "owner": "fixture_5", "subject": "fixture_1"}, "..." ]}
```

| Field | Type | Range | Default | Unit | Editor policy |
| --- | --- | --- | --- | --- | --- |
| `at_least`, `of` | as [EL-120](EL-120-quantity-goal.md) | as EL-120 | none | items | level data |
| `window.from_seconds` | f32 | 0 up to the level's declared Run length | none (level 29 **proposed** 5) | s of game time from Run start | level data; not player-editable |
| `window.length_seconds` | f32 | above 0; `from_seconds` + `length_seconds` ≤ the level's declared Run length (30 s by default) | none (level 29 **proposed** 10) | s of game time | level data |

- **Window rule.** A member counts only if its occurrence time t satisfies `from` ≤ t < `from` + `length` (half-open). Because each observation is a first-occurrence latch, a body captured before the window has spent its one occurrence and never counts in that Run, even if it stays in the Receiver. That is the requirement's failing burst.
- **Group truth.** Solved in the first committed read in which `at_least` members have in-window occurrences. The occurrence is the `at_least`-th earliest in-window occurrence. The goal does not wait for the window to close.
- **Declared rate.** The throughput the level asks for is `at_least` ÷ `length_seconds` (level 29: 3 ÷ 10 s = 0.3 items per second), stated on the card in those units.
- **Time.** Durations are f32 game seconds ([D10](../invest/delivery-batches.md#3-family-level-decisions)), never ticks. A window must end within the level's declared Run length, which is per level under `ENGINE-RUN-LENGTH` (owner decision 13, 22 November 2026) and 30 s by default (`CuriousContraptions.Abstractions/CadenceValues.cs:77`); a later end could never be met and is refused at admission. The evaluator's existing 0 to 120 s bound on a goal duration (`engine/gpu/WorkshopGoalEvaluator.cs:60`) is not the window's bound. The comparison keeps the integer ordinal and the phase separate, as the existing elapsed test does (`:62-71`), so a boundary is not rounded.
- **Bodies, colliders, joints, ports, stores, package parameters:** none.

### Art and solidity

No body, collider or art node. The goal card (HUD) shows a gold window bar that advances with committed game time, navy dots for in-window deliveries and hollow marks for deliveries outside the window (cream `#fff8e9`, navy `#293954`, gold `#f7cb52`; [gap-16 visual style](../requirements.md#gap-16)). It differs from the quantity card by the bar and the hollow marks, not by colour. The card reads committed progress and writes nothing back.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol | Exercising test (body opened unless noted) |
| --- | --- | --- | --- | --- |
| 1 | Committed capture latch with an occurrence ordinal and phase | present | `CuriousContraptions.Abstractions/CaptureLatch.cs:9-10` | `PayloadInstanceTests.AReceiverCapturesEitherKindAndAShiftedMissNever` |
| 2 | Minimum elapsed game time between two occurrences, without rounding | present | `engine/gpu/WorkshopGoalEvaluator.cs:57-72` | `DelayedSignalTests.DyadicMinimumDoesNotRoundElapsedBeforeComparison` |
| 3 | Physical ordinals at 480 per game second | present | `CuriousContraptions.Abstractions/CadenceValues.cs:78` | same test (4290 ordinals against 8.9375 s) |
| 4 | Occurrence inside a declared half-open window measured from Run start (lower and strict upper bound) | missing | the evaluator compares two occurrences only; no Run-start origin and no upper bound | ENGINE-GOALS slice `engine-goal-count-window-hold` (owner decision 12) |
| 5 | Count group (at least k of n) | missing | none | ENGINE-GOALS slice `engine-goal-ordered-events` ([EL-120](EL-120-quantity-goal.md) row 6) |
| 6 | Several conditions per goal | missing | `engine/gpu/WorkshopPuzzle.cs:74-77` | drafted slice `engine-goal-evaluation` |
| 7 | Window bounds, in-window count and committed game time on the observation port and HUD | missing | `engine/gpu/WorkshopObservationProjection.cs:84-93` | `engine-goal-progress-observation` |
| 8 | A payload entering a Receiver at 15 m/s is not lost by tunnelling | present, test not opened | general swept contact | `PayloadInstanceTests.APayloadDrivenIntoTheBenchAtFortyMetresPerSecondNeverTunnels` (name and line only) |

Counts: present 3; present-untested 1 (row 8, body not opened); narrower 0; missing 4. Rows 4 and 5 have one owner, ENGINE-GOALS, whose text in `epics.md` names neither; each has its slice by owner decision 12 and the proposed story text is in the batch summary. A window that ends after 30 s also needs `ENGINE-RUN-LENGTH`.

## 4. Sources and legacy

- **Requirements.** Row: "Measures throughput over an explicit simulation-time interval"; outcome "An instantaneous burst outside the window fails" ([element-121](../requirements.md#element-121)). Integration: transient-only throughput is the near-miss control ([gap-16](../requirements.md#gap-16)).
- **Decisions.** [S635 goal-occurrences](../invest/decisions.md#s635): out-of-window routes do not solve.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Legacy timed goals compared first-occurrence ticks: `received − emitted ≥ ceil(delay / tick)`, delay 0–120 s. | `reference/cpu/MachineWorld.cs@a6c914e:L372-L378`, `reference/cpu/MachineWorld.cs@a6c914e:L934-L940` | Carry forward first-occurrence timing. The 0–120 s bound stays the evaluator's bound on a minimum gap; a window is bounded by the Run length. Do not carry forward tick rounding. |
| 2 | No legacy goal measured throughput. | `engine/MachineData.cs@a6c914e:L152-L152` | Recorded: the window rule is new. |

Files consulted: `reference/cpu/MachineWorld.cs`, `engine/MachineData.cs`.

## 5. Acceptance outline

Point of truth: [element-121](../requirements.md#element-121) and the [gap-16 integration](../requirements.md#gap-16).

### Behaviour and numbers

- **Law.** A threshold over first-occurrence latches whose occurrence times lie in a declared half-open interval of game time. Evaluated on the main thread over the committed read, after the tick's sensor phase (Phase 5) and commit ([solver model](../../gpu-f32-physics.md#solver-model)). Nothing flows back.
- **Scenario fixture (window 1.5 s to 3.0 s, target 3).** Dyadic bounds so the ordinals are whole: 1.5 s × 480 = 720 and 3.0 s × 480 = 1440.
  - *Early burst.* A Tennis ball with `bounce` 0 dropped 1.0 m lands after √(2 × 1.0 / 9.81) = 0.45 s and latches after the 0.35 s default dwell, near 0.80 s: 0.70 s before the window opens.
  - *In-window.* The same ball dropped 12 m lands after √(2 × 12 / 9.81) = 1.56 s at 9.81 × 1.56 = 15.3 m/s and latches near 1.91 s: 0.41 s after the window opens and 1.09 s before it closes. Placement admits ±64 m (`PayloadInstanceTests.PlacementBoundariesBuildAndCompileAndBeyondIsRefused`).
  - *Late control.* The 12 m drops against window 1.0 s to 1.5 s latch 0.41 s after it closes.
  - *Uncertainty.* Ambient drag 0.04 1/s over 1.56 s changes the fall time by about 1.6 %, 0.03 s; one tick is 1/120 s = 0.008 s. Every margin above is at least 0.4 s, more than ten times that, so no arrival tick is asserted.
  - Three balls are dropped at (±0.3 m, ±0.3 m) over the Receiver: 0.6 m apart for 0.5 m balls, 0.14 m from the inner wall (0.69 − 0.3 − 0.25).
- **Exact boundary.** Proven on the production evaluator with constructed committed reads, as the existing elapsed-time test does: an occurrence at ordinal 720 phase 0 counts; ordinal 1440 phase 0 does not; one phase unit before 1440 counts.
- **Level 29 values (proposed).** Window 5 s to 15 s, target 3: the 5 s start lets the opening burst of the set-up fall outside the window; 10 s is long enough for a Conveyor to deliver three balls, and the window ends at 15 s, inside the default 30 s Run.
- **Run length.** The fixture's windows end at 3.0 s and its guards are 10 s; level 29's window ends at 15 s. Every row fits the default 30 s Run. Row R5 reads the Run limit from the cadence settings today and from the level's declaration once `ENGINE-RUN-LENGTH` lands; that story is a prerequisite only for a level whose window ends after 30 s, and such a level declares a Run length of at least its window's end.

### Acceptance rows

Planned test class `GoalWindowTests` (engine slice) and `RateLessonTests` (level 29). Chrome proof is pending the post-catalogue browser phase.

| Row | Given / When / Then | Planned test | Control | Boundary | Tolerance and guard |
| --- | --- | --- | --- | --- | --- |
| R1 | Given the fixture and three 12 m drops, when run, then Waiting until the third in-window latch, then Solved; occurrence is the third in-window occurrence | `GoalWindowTests.ThreeDeliveriesInsideTheWindowSolve` | Two 12 m drops and one absent: Waiting (insufficient quantity) | n/a | Phases exact; 10 s guard |
| R2 | Given three 1.0 m drops (burst before the window), then the goal is Waiting for the whole guard although three balls rest in the Receiver | `GoalWindowTests.ABurstBeforeTheWindowNeverSolves` | R1 | n/a | 10 s guard, past the window's end |
| R3 | Given R1's drops and window 1.0 s to 1.5 s, then Waiting for the whole guard | `GoalWindowTests.DeliveriesAfterTheWindowNeverSolve` | R1 (one cause changed: the window) | n/a | 10 s guard |
| R4 | Given constructed committed reads, then an occurrence exactly at `from` counts and one exactly at `from + length` does not | `GoalWindowTests.TheWindowIsHalfOpenAtExactOrdinals` | n/a | one phase unit either side of each bound | Exact (a defined-interface invariant of the clock, not a simulated float) |
| R5 | Given content with a negative or non-finite `from_seconds`, a zero, negative or non-finite `length_seconds`, a window that ends after the level's declared Run length, or a missing field, when loaded, then refused with a typed reason; the installed catalogue is unchanged | `GoalWindowTests.MalformedWindowsAreRefusedAtomically` | A valid window loads | `from` 0; an end exactly at the Run length is admitted, one phase unit later is refused | Typed reason exact |
| R6 | Given a solved goal, when Reset then Run, then Waiting with the window clock at 0, and it solves again | `GoalWindowTests.ResetRestartsTheWindow` | n/a | n/a | Authored state restored |
| R7 | Given a solved construction saved and loaded, then the decoded window and group equal the saved ones and a fresh run solves | `GoalWindowTests.SaveAndLoadPreserveTheWindow` | A save whose window differs from the authored one is refused | n/a | Decoded field equality |
| R8 | Given authored level 29, when its solution runs, then it solves; when the feed is released early so the balls arrive before the window, it does not (transient-only throughput) | `RateLessonTests.AuthoredLessonSolvesAndAnEarlyBurstDoesNot` | one-cause control | n/a | the whole declared Run (30 s default) |

Wrong-kind connection and player-parameter refusals do not apply (no port, no player parameter); R5 is the refusal row. Double counting is excluded by EL-120's rows Q2 and Q4, which the same group runs.

### Integration

- **Collaborators.** Receiver and balls (delivered); a steady feed for the lesson: Conveyor (delivered), later the granular dispenser GAP-06.
- **Campaign.** Introduction 29 "Going Steady" (after delay and clock), practice 30, reuse 97 and 140 ([GAP-16 allocation](../requirements.md#campaign-gap-allocation)). Cross-element task [sequence-task-549](../requirements.md#sequence-task-549).
- **Lesson fixtures.** Locked authored placements only; S8 is needed only for a package no level has fixed.

### Delivery

- **Shape.** Blocked on the engine programme (owner decision 3a). Then the engine slice `engine-goal-count-window-hold`, then a level slice for 29. No package.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in which `ENGINE-SENSOR-VOLUMES` publishes the residence latches the counts read; S5; `engine-goal-evaluation`; `engine-goal-ordered-events` (the group form and [EL-120](EL-120-quantity-goal.md)'s rows); `engine-goal-count-window-hold`; `engine-goal-progress-observation` before the card's bar; `ENGINE-RUN-LENGTH` only for a level whose window ends after 30 s.
- **Files.** As EL-120: goal model, evaluator, wake, content reader, save schema, projection, TypeScript read model; `content/puzzles.json` for the level.
- **Review tier.** Engine.
- **Disqualifies the plan.** Occurrence ordinals are not counted from Run start (then the slice must name the committed Run-start origin it compares against); a Tennis ball with `bounce` 0 does not qualify within about 0.35 s of landing.

## 6. Open questions

**Resolved.**

1. *Fixed or sliding window?* Fixed, measured from Run start (decided now: the requirement says "an explicit simulation-time interval", and a sliding window needs a history of occurrences that first-occurrence latches do not keep).
2. *Does a burst inside the window satisfy the goal?* Yes (decided now: the requirement fails only a burst outside the window; minimum spacing would be a second rule no lesson uses, D19).
3. *Solved at the target arrival or at window close?* At the target arrival (D19; Solved shows once, S635).

**Needs the owner.** None.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026, `_bmad/custom/element-free-engine-design.md` and `docs/gpu-f32-physics.md`.

1. **Declaration data only.** One level-data entry: a group, a threshold and two durations. No code per level or part.
2. **Generic laws.** A time-window predicate on committed occurrence times and a count threshold. Both missing capabilities are general, stated element-free and owned by ENGINE-GOALS.
3. **Three layers.** Colliders none; visuals none in the scene (HUD card only); animation none.
4. **Flow and typing.** Committed read → main-thread evaluator → cue; nothing flows back. Durations are f32 seconds; kinds are enums; ids are typed.
5. **Flat state.** Bodies 0; colliders 0; joints 0; drives 0; sensors 0 of its own; domain nodes 0; stores 0; bindings 0. One group with n selectors and one window, fixed at level load.
6. **Performance.** Evaluated on the event-ring wake of a member's owner; linear in the group's members. No per-tick scan of the scene by the goal. The residence scan it reads (each sensor against each admitted payload today) is owned by `ENGINE-SENSOR-VOLUMES`, with candidates from `ENGINE-SPATIAL-QUERIES`, in the engine programme of owner decision 3a. Nothing here designs around it.
7. **Tests are scenarios.** R1 to R3 and R6 to R8 run the production simulation; R4 and R5 run the production evaluator and reader.

**Verdict: conforms; blocked on the engine programme (owner decision 3a) and on ENGINE-GOALS for two general capabilities stated element-free.**

## 7. Coverage binding (planned, not yet live)

No package, so no spec binding section is planned and none is written. The identity's task source `sequence-task-674` is to be satisfied by the engine slice's tests and the level-29 tests, cited from the ledger entry. S7, which widens the reader (owner decision 15, 22 November 2026), supplies the closure rule for an identity with no package (batch summary, section 4 item 16).
