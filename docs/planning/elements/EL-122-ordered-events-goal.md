# EL-122 · Ordered-events goal — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-122 delivery slice](../invest/named-elements.md#element-122). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code is cited as `path:line` in that checkout; `path@a6c914e` citations appear only in section 4 and describe the harvest baseline. Values marked **proposed** are starting values ([decision D1](../invest/delivery-batches.md#3-family-level-decisions)). Nothing here was built or run.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-122 · Ordered-events goal · Goal |
| Anchor | [requirements.md#element-122](../requirements.md#element-122) (task `sequence-task-675`); [named-elements entry](../invest/named-elements.md#element-122); umbrella [gap-16](../requirements.md#gap-16) (index only) |
| Owner key | S676 |
| Related identities | Siblings [EL-120](EL-120-quantity-goal.md), [EL-121](EL-121-rate-window-goal.md), [EL-123](EL-123-protected-end-state-goal.md). Today's two-event `ActivatedAfter` goal (level `delayed_signal`, [CAT-022 Delay](CAT-022-delay.md)) is its two-step case. No CAT refines it. |
| Engine story | ENGINE-GOALS; named slice `engine-goal-ordered-events` ([`spec-engine-goal-evaluation.md`](../../../_bmad-output/implementation-artifacts/spec-engine-goal-evaluation.md), "Split decision", part 4) |
| Status | Partly present, and blocked on the engine programme (owner decision 3a) and on ENGINE-GOALS: one two-step activation order with a minimum gap exists (`engine/gpu/WorkshopPuzzle.cs:22-24`, `engine/gpu/WorkshopGoalEvaluator.cs:41-54`). |

### Disposition

**Level data, not a part.** An ordered-events goal is a set of entries in a level's declared goal list whose conditions each require an earlier observation. No package, directory, palette entry or catalogue key. It is blocked on the engine programme (owner decision 3a); after it, it is delivered by the ENGINE-GOALS slices `engine-goal-evaluation` (a predecessor of any observation kind, several conditions) and `engine-goal-ordered-events` (the story's own ordered acceptance, any-of and forbidden events), then by the level that first uses it (campaign level 49).

## 2. Declaration

### Declaration to write

A sequence A, then B, then C is declared with the contract's `after` field: each later step is a condition that names the previous step as its earlier observation.

```json
"goals": [
  {"kind": "Captured", "owner": "fixture_4", "subject": "fixture_1"},
  {"kind": "Captured", "owner": "fixture_5", "subject": "fixture_2",
   "after": {"kind": "Captured", "owner": "fixture_4", "subject": "fixture_1", "minimum_seconds": 0.5}},
  {"kind": "Activated", "owner": "fixture_6", "node": "signal",
   "after": {"kind": "Captured", "owner": "fixture_5", "subject": "fixture_2", "minimum_seconds": 0}}
]
```

| Field | Type | Range | Default | Unit | Editor policy |
| --- | --- | --- | --- | --- | --- |
| step observation | typed selector of kind `Captured`, `Activated`, `IntegrityBroken` or `TravelReached` | a declared observation of a level slot | none | n/a | level data; not player-editable |
| `after` | one earlier observation of any kind | not equal to its own condition | absent | n/a | level data |
| `after.minimum_seconds` | f32 | 0 to 120 | 0 | s of game time | level data |

- **Order rule.** A step holds only when its earlier observation occurred first and the gap is at least the minimum (contract matrix M4). Every observation is a first-occurrence latch, so a step that latches before its predecessor can never hold in that Run: wrong order cannot be repaired later.
- **Ties.** With `minimum_seconds` 0, two occurrences at the same ordinal and phase satisfy the step. This is the evaluator's one rule (`WorkshopGoalEvaluator.cs:61`, `:68-69`): the same instant is not the wrong order. A lesson that wants a visible gap declares a positive minimum.
- **Solved.** When every condition holds; the occurrence is the latest one (contract, "Composite occurrence").
- **Step count.** The engine sets no ceiling (contract, "No condition-count ceiling"). The card is designed for up to four numbered marks (**proposed**, a content rule).
- **What `engine-goal-ordered-events` adds.** Any-of in its one form, the count group of [EL-120](EL-120-quantity-goal.md) (at least k of n declared conditions; occurrence is the k-th earliest; one subject once per group; owner decision 12), a forbidden event with a failed-until-Reset phase, and the story's ordered acceptance proven verbatim. If that slice gives a sequence its own list form, the chained `after` form above is rewritten to it in the same change; there is one form at any time.
- **Bodies, colliders, joints, ports, stores, package parameters:** none. Sources are observations that packages already declare.

### Art and solidity

No body, collider or art node. The goal card (HUD) shows numbered navy order marks; each turns gold when its step holds; a step that latched before its predecessor shows a crossed mark, a shape and not a colour change (cream `#fff8e9`, navy `#293954`, gold `#f7cb52`; [gap-16 visual style](../requirements.md#gap-16)). The numbering distinguishes it from the quantity and rate cards. The card reads committed progress and writes nothing back.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol | Exercising test (body opened) |
| --- | --- | --- | --- | --- |
| 1 | Committed activation latch with an occurrence time | present | `engine/gpu/WorkshopGoalEvaluator.cs:43-50` | `DominoEffectTests.GoalSolvesOnlyWhenTheEndDominoAndTheLampAreBothLatched` |
| 2 | Order of two activation occurrences with a minimum gap; an early target latch never solves | narrower: two steps, activation nodes only | `WorkshopGoalEvaluator.cs:41-54`; `engine/gpu/WorkshopPuzzle.cs:22-24` | `DelayedSignalTests.GoalUsesTargetOccurrenceNotRootCauseOrTimerAndRejectsEarlyLatch` |
| 3 | Exact elapsed comparison, 0 to 120 s | present | `WorkshopGoalEvaluator.cs:57-72` | `DelayedSignalTests.DyadicMinimumDoesNotRoundElapsedBeforeComparison` |
| 4 | Committed capture latch with an occurrence | present | `CuriousContraptions.Abstractions/CaptureLatch.cs:9-10` | `PayloadInstanceTests.AReceiverCapturesEitherKindAndAShiftedMissNever` |
| 5 | An earlier observation of any kind, on any of several conditions (chains of more than two steps, mixed kinds) | missing | `WorkshopPuzzle.cs:74-77` holds one goal | drafted slice `engine-goal-evaluation` (matrix M4) |
| 6 | The story's ordered acceptance, any-of as a count group, forbidden event with failed-until-Reset | missing | none | named slice `engine-goal-ordered-events`; no contract is drafted yet |
| 7 | Per-step progress and out-of-order mark on the observation port and HUD | missing | `engine/gpu/WorkshopObservationProjection.cs:84-93` | named slice `engine-goal-progress-observation` |

Counts: present 3; present-untested 0; narrower 1; missing 3. Every missing row has one owner under ENGINE-GOALS, and the story's text in `epics.md` covers them ("ordered and forbidden events", "per-goal progress").

## 4. Sources and legacy

- **Requirements.** Row: "Tracks declared event identities in a required sequence"; outcome "Wrong order does not satisfy the sequence" ([element-122](../requirements.md#element-122)). Integration: reversed order is the near-miss control ([gap-16](../requirements.md#gap-16)).
- **Decisions.** [S635 goal-occurrences](../invest/decisions.md#s635): wrong-order routes do not solve.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | `ActivatedAfter`: target activation tick − source tick ≥ ceil(delay / tick); `PoweredAfter` additionally required the powered tick strictly after the trigger. | `reference/cpu/MachineWorld.cs@a6c914e:L934-L940` | Carry forward order and minimum gap; tick rounding does not carry forward. |
| 2 | Goal delay finite and within 0–120 s, validated before world replacement. | `reference/cpu/MachineWorld.cs@a6c914e:L372-L378` | Carry forward. |
| 3 | Levels using order: `delayed_signal` (`activated_after`), `switched_motor` and `delayed_solar` (`powered_after`). | `content/puzzles.json@a6c914e:L6575-L6864`, `content/puzzles.json@a6c914e:L3580-L3873`, `content/puzzles.json@a6c914e:L6865-L7293` | Carry forward as lesson references; `powered_after` is owned by `engine-goal-supplied-occurrence`. |

Files consulted: `reference/cpu/MachineWorld.cs`, `content/puzzles.json`.

## 5. Acceptance outline

Point of truth: [element-122](../requirements.md#element-122), the [gap-16 integration](../requirements.md#gap-16) and the ENGINE-GOALS specific acceptance.

### Behaviour and numbers

- **Law.** Order and minimum gap between committed first-occurrence times, evaluated on the main thread over the committed read after the tick's sensor phase (Phase 5) and commit ([solver model](../../gpu-f32-physics.md#solver-model)). Nothing flows back.
- **Scenario fixture (three Receivers A, B, C, 3 m apart; three Tennis balls with `bounce` 0).** Fall time is √(2h / 9.81); a dead ball latches after the 0.35 s default dwell.
  - Ball 1 over A from 1.0 m: 0.45 s, latch near 0.80 s.
  - Ball 2 over B from 12 m: 1.56 s at 15.3 m/s, latch near 1.91 s.
  - Ball 3 over C from 30 m: √(60 / 9.81) = 2.47 s at 9.81 × 2.47 = 24.3 m/s, latch near 2.82 s.
  - Gaps: A to B 1.11 s; B to C 0.91 s. Declared minimums 0.5 s leave 0.61 s and 0.41 s of margin. Drag (0.04 1/s) moves a 2.5 s fall by about 2.5 %, 0.06 s, and one tick is 0.008 s, so each margin is more than six times the uncertainty and no arrival tick is asserted.
  - *Reversed control.* Swap the heights of balls 1 and 2: B latches near 0.80 s, before A at 1.91 s, so step B never holds.
  - *Gap control.* Correct order with the A-to-B minimum raised to 2.0 s: the 1.11 s gap is 0.89 s short.
- **Run length.** The fixture's last latch is near 2.8 s and its guards are 10 s; level 49 is planned inside the default 30 s Run (`CuriousContraptions.Abstractions/CadenceValues.cs:77`). A sequence whose declared minimums sum to more than the level's Run length can never be solved, so admission refuses it once Run length is per level (`ENGINE-RUN-LENGTH`, owner decision 13); that story is a prerequisite only for a level whose sequence needs more than 30 s, and such a level declares the Run length it needs.
- **Exact ties and gap boundaries** are proven on the production evaluator with constructed committed reads (equal times with minimum 0 hold; one phase unit short of a positive minimum does not).

### Acceptance rows

Planned test class `GoalOrderTests` (in `engine-goal-ordered-events`) and `OrderLessonTests` (level 49). Chrome proof is pending the post-catalogue browser phase.

| Row | Given / When / Then | Planned test | Control | Boundary | Tolerance and guard |
| --- | --- | --- | --- | --- | --- |
| O1 | Given the fixture, when the three balls land in order A, B, C, then Waiting until C's step holds, then Solved at C's occurrence | `GoalOrderTests.EventsInTheRequiredOrderSolve` | Ball 3 absent: Waiting for the guard | n/a | Phases exact; 10 s guard |
| O2 | Given the reversed control, then the goal is not Solved for the whole guard although all three captures occur | `GoalOrderTests.TheSameEventsOutOfOrderDoNotSolve` | O1 | n/a | 10 s guard |
| O3 | Given the gap control, then not Solved; with the minimum at 0.5 s it is | `GoalOrderTests.AGapBelowTheMinimumDoesNotSolve` | O1 | constructed reads: gap exactly the minimum holds; one phase unit less does not | Exact at the constructed boundary |
| O4 | Given constructed reads with two steps at one ordinal and phase and minimum 0, then the later step holds | `GoalOrderTests.SimultaneousOccurrencesSatisfyAZeroMinimum` | with a positive minimum it does not | n/a | Exact |
| O5 | Given a sequence mixing a capture, an activation and a travel observation, then it solves in order and not out of order | `GoalOrderTests.AMixedKindSequenceIsOrdered` | one step's cause removed | n/a | 10 s guard |
| O6 | Given a forbidden event declared beside the sequence, when it occurs, then the attempt is failed until Reset even if the sequence later completes | `GoalOrderTests.AForbiddenEventFailsTheAttemptUntilReset` | the forbidden event absent: Solved | n/a | Phases exact |
| O7 | Given content whose `after` equals its own condition, names an undeclared observation, or has a minimum below 0, above 120 or non-finite, when loaded, then refused with a typed reason; the installed catalogue is unchanged | owned by the contract's `GoalAdmissionTests` (matrix M6) | a valid chain loads | 0 and 120 admitted | Typed reason exact |
| O8 | Given a solved sequence, Reset then Run solves again; saved and loaded, the decoded goal equals the saved one | owned by the contract's matrix M8 and M9 tests, run on a three-step chain | n/a | n/a | Authored state restored |
| O9 | Given authored level 49, its solution solves and its reversed-order construction does not | `OrderLessonTests.AuthoredLessonSolvesAndReversedOrderDoesNot` | reversed order | n/a | the whole declared Run (30 s default) |

Wrong-kind connection and player-parameter refusals do not apply (no port, no player parameter); O7 is the refusal row.

### Integration

- **Collaborators.** Any package that declares an observation: Receiver, Impact switch, Domino, Signal lamp, Delay box, Balloon (integrity), and a travel observer once a package declares one (all delivered except the travel declaration, which the contract assigns to a level slice).
- **Campaign.** Introduction 49 "First Things First" (after loading), practice 50, reuse 100 and 146 ([GAP-16 allocation](../requirements.md#campaign-gap-allocation)). Cross-element tasks [sequence-task-549](../requirements.md#sequence-task-549) and [sequence-task-279](../requirements.md#sequence-task-279).
- **Lesson fixtures.** Locked authored placements only.

### Delivery

- **Shape.** Blocked on the engine programme (owner decision 3a). No slice of its own beyond ENGINE-GOALS: rows O1 to O6 are acceptance rows of `engine-goal-ordered-events`; O9 is a level slice. No package.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in which `ENGINE-SENSOR-VOLUMES` publishes the residence latches its Receiver conditions read; S5; `engine-goal-evaluation`; `engine-goal-ordered-events`; `engine-goal-progress-observation` before the card's marks; `ENGINE-RUN-LENGTH` only for a level whose sequence needs more than 30 s.
- **Files.** Those in the contract's owned scope; `content/puzzles.json` for the level.
- **Review tier.** Engine.
- **Disqualifies the plan.** The ordered-events contract, when drafted, defines a sequence form that differs from chained `after`: this spec's declaration is then rewritten to that form before any level is authored.

## 6. Open questions

**Resolved.**

1. *Is `ActivatedAfter` deleted?* Yes. The contract replaces it with an `Activated` condition carrying `after` (its Design Notes, "Deleted"); one current path.
2. *Does an out-of-order step fail the Run or get ignored?* It can never hold in that Run, so the goal stays unsolved until Reset; the card shows the crossed mark. A separate Failed phase is reserved for forbidden events, as the ENGINE-GOALS acceptance words it.
3. *Maximum step count?* No engine ceiling; four marks on the card as a content rule (D1).
4. *Ties.* Decided now: equal times satisfy a zero minimum, because that is the evaluator's existing rule and a second rule for the same comparison is not allowed.

**Needs the owner.** None.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026, `_bmad/custom/element-free-engine-design.md` and `docs/gpu-f32-physics.md`.

1. **Declaration data only.** Conditions and predecessors in level data. No per-level or per-part code.
2. **Generic laws.** Order and minimum gap between committed occurrences; the missing parts are general and owned by named ENGINE-GOALS slices.
3. **Three layers.** Colliders none; visuals none in the scene (HUD card only); animation none.
4. **Flow and typing.** Committed read → main-thread evaluator → cue. Observation kinds are a closed enum; owners, nodes and local observation ids are typed.
5. **Flat state.** Bodies 0; colliders 0; joints 0; drives 0; sensors 0 of its own; domain nodes 0; stores 0; bindings 0. n conditions with at most one predecessor each, fixed at level load.
6. **Performance.** Woken by the event ring for the owners its conditions name; linear in the number of conditions. The passes that produce the observations are owned by the engine programme of owner decision 3a: residence and guide scans by `ENGINE-SENSOR-VOLUMES` with candidates from `ENGINE-SPATIAL-QUERIES`, and contact events by `ENGINE-CONTACT-OBSERVATIONS`. Nothing here designs around them.
7. **Tests are scenarios.** O1 to O3, O5, O6 and O9 run the production simulation; O3's boundary, O4 and O7 run the production evaluator and reader.

**Verdict: conforms; blocked on the engine programme (owner decision 3a) and on the ENGINE-GOALS slices already named.**

## 7. Coverage binding (planned, not yet live)

No package, so no spec binding section is planned and none is written. The identity's task source `sequence-task-675` is to be satisfied by the tests of `engine-goal-ordered-events` and of level 49, cited from the ledger entry. S7, which widens the reader (owner decision 15, 22 November 2026), supplies the closure rule for an identity with no package (batch summary, section 4 item 16).
