---
title: 'Story 9R.12: Receiver suite cat-004 (reopened Story 6.5d)'
type: 'feature'
created: '2026-10-18'
status: 'ready-for-dev'
route: ''
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r12-receiver-suite-cat-004-reopened-story-65d'
  - '{project-root}/docs/planning/elements/CAT-004-basket.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem (report #8):** `tools/e2e/cat-004.test.ts` never existed. The Receiver's capture, its outside-mouth refusal, Reset and Save/Load have no element suite. Today it is touched only by `engine-core-2c`, `engine-core-2b4`, `anim-1a`/`1c` and `cat-062b` (Route.Ready).

**Approach:** Add one suite following CAT-004 §5:
- a drawer-placed Receiver in the free workshop proves capture and refusal from the committed capture latch;
- a drawer-placed Receiver in `ready_to_rebound`, the only admitted level that grants one, proves "the goal is solved".

## Boundaries & Constraints

**Always:**
- Common 9R rules:
  - Chrome proof uses actual UI controls through `workshop-driver.ts`.
  - Committed reads arrive only on command acks.
  - Exact Run/Reset uses the Story 9R.1 snapshot. Its contract: the tick-0 read from the last Construct or Load ack plus the authored bytes. Every body's cell, local, rotation, com, velocity and angularVelocity, plus the captures, activations, timers and electrical rows, are deep-equal after Reset (model: `cat-062b.test.ts:226-231`).
  - Negatives in authored levels run at Precise.
- Free-workshop capture settings: margin 0.02, speed limit 1.5 m/s, dwell 0.35 s (`ReceiverCaptureSettings.Free`, `engine/gpu/WorkshopConstruction.cs:93`). There is no guide. Capture latches once (`CaptureLatch`).
- In `ready_to_rebound`, the placed Receiver takes the lesson settings, and the goal rebinds to it (`SpringboardLessons.WithReceiver`, `engine/gpu/SpringboardLessons.cs:72-82`).

**Never:**
- No setters, imported solutions or numeric placement.
- Do not move the authored fixtures.
- No new receiver behaviour unless a defect is found.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Free capture | drawer Receiver; drawer Basketball stacked above its mouth | capture latch Latched once after dwell; halo target animates once | N/A |
| Outside | same, ball placed so it falls beside the walls (outside the region) | ball lands outside (pose x/z outside ±0.75 of centre); latch stays Clear; no halo sample | N/A |
| Ready solve | `ready_to_rebound`, Receiver placed at the cat-062b Ready point | exactly 1 `CCGOAL_SOLVED`; latch Latched | N/A |
| Ready miss | Receiver placed off the descent at Precise | 0 solves | N/A |
| Re-run | Reset, Run again | snapshot equal; capture again exactly once | N/A |

</frozen-after-approval>

## Code Map

- `engine/gpu/WorkshopConstruction.cs:79-113` -- capture settings, the force region and `WorkshopReceiver`.
- `engine/gpu/WorkshopPhysicsCompiler.cs:323` (`CaptureSensor`) and the Receiver compile arm -- walls and residence region. `ReceiverGeometry.cs`, cited by CAT-004 §2 at `a6c914e`, no longer exists.
- `engine/gpu/CaptureLatch.cs`, `PhysicsCaptureRead.cs` -- the committed capture read (`captures()` phase and `eventOrdinal`, as used at `cat-062b.test.ts:230-231`).
- `engine/gpu/SpringboardLessons.cs:17-30`, `:37-64`, `:72-82` -- Ready assistance, inventory Receiver ×1, and rebinding.
- `tools/e2e/workshop-driver.ts:178-199`, `:805-811` -- receiver palette row and `stackUnderLiftedBall('receiver')`.
- `tools/e2e/cat-062b.test.ts:121-127`, `:242-273` -- the Ready recipe and the delete/replace rebind.

## Tasks & Acceptance

**Execution:**
- [ ] `tools/e2e/workshop-driver.ts` -- add `ready_to_rebound` as a `WorkshopLevel` with its anchor and dock (shared with 9R.8), and `setPrecision` if 9R.8 has not landed it.
- [ ] `tools/e2e/cat-004.test.ts` (new) -- write the matrix cases first, each from a fresh page. Capture comes from the committed `captures()` row at Pause and Reset acks; the halo from `readAnimationSamplesForTarget(<receiver halo target>)`; solve from `waitForGoalSolved`.
- [ ] Defect fixes, only if a case finds one, with a native test first.

**Acceptance Criteria:**
- Given a Receiver placed through the real drawer, when a ball enters its mouth and dwells, then the committed capture latch becomes Latched exactly once and the halo animates once.
- Given `ready_to_rebound` with the Receiver placed through the drawer under the precharged launch, when run, then exactly one solve is printed (the goal is solved).
- Given a ball passing outside the mouth (free workshop), or a Receiver placed off the descent (`ready_to_rebound` at Precise), when run, then nothing is captured and no solve is printed (negative).
- Given any case after Run, when Reset, then the read equals the pre-Run snapshot (capture Clear, ordinal 0, halo cleared). When Save, reload and Load, then the Receiver's pose and capture settings, and the rebound goal target in `ready_to_rebound`, are byte-identical.
- `cat-004` passes 100% serially in Chrome.
- `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

**§5 map (CAT-004):**
- Construction: AC1 and AC2. The `first_principles` fixed receiver is in 9R.11.
- Positive: AC1 and AC2.
- Negative/control: AC3. Fast pass, disabled participant, re-entry and decoy are native (see Design Notes).
- Boundaries: `engine-core-2b4` plus native.
- Run/Reset and Save/Load: AC4.
- Integrations: Bowling `cat-014`, pipes `cat-048b`/`049`/`050`, Springboard `cat-062b`.
- Moving frame: Incomplete (owner).

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

**Decisions (owner playability delegation):**
- "Captured and the goal is solved" is proven in two places. The free workshop proves capture, since it has no goal. `ready_to_rebound` proves the solve. Rationale: it is the only admitted level whose inventory grants a Receiver (`content/puzzles.json`; `SpringboardLessons.Create`).
- The Ready miss runs at Precise. Rationale: Forgiving guide acceleration can pull near misses in (CAT-004 fact 14).

**§5 items not reachable in UI:**
- Disabled participation: no UI control. Native `WorkshopSimulationTests` residence cases, otherwise Incomplete.
- Fast through-pass and leave-and-re-enter: native residence tests. If no current native test covers them, add one in `CuriousContraptions.tests` in this story.
- Decoy ball: free workshop has no goal and authored levels admit one ball (`WorkshopPuzzle.cs:66-76`), so native only.
- Guide eligibility matrix and knots 0/0.45/1: `engine-core-2b4` plus native.
- Moving frame: CAT-004 §6, an owner decision.

**Test-first plan:** the suite runs red against the current build. Passing assertions remain as regression evidence, and failures are defect stops.

**Mutation plan:** each change below must fail a case.
- Widen the residence region by 1 m (outside case).
- Drop dwell to 0 (single-latch timing).
- Skip the `WithReceiver` rebind (Ready solve).
- Do not clear the capture on Reset (snapshot).

**Impacted suites:** `engine-core-2c`, `engine-core-2b4`, `engine-core-2a3`, `anim-1a`, `anim-1c`, `cat-014`, `cat-062b`.

**Risks:**
- Halo target id: derive it from the committed read or the cosmetic target mapping. Don't hard-code it.
- `stackUnderLiftedBall` geometry assumes the 3 m plane, so verify the stacked pose from the saved bytes.

## Verification

**Commands:**
- `dotnet build CuriousContraptions.slnx` -- expected: 0 warnings.
- `dotnet test CuriousContraptions.slnx` -- expected: 100%.
- Serial Chrome run of `tools/e2e/cat-004.test.ts`, then all of `tools/e2e/*.test.ts` against the 8060 production preview -- expected: 100%.
