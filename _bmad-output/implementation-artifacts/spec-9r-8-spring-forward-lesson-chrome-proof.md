---
title: 'Story 9R.8: spring_forward lesson Chrome proof (reopened Story 6.5e)'
type: 'feature'
created: '2026-10-18'
status: 'ready-for-dev'
route: ''
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r8-spring_forward-lesson-chrome-proof-reopened-story-65e'
  - '{project-root}/docs/planning/elements/CAT-062-spring.md'
  - '{project-root}/tools/e2e/cat-062b.test.ts'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 6.5e's "spring_forward solves identically in Chrome" is a W row. The driver's `WorkshopLevel` union (`tools/e2e/workshop-driver.ts:310`) has no `spring_forward`, and `selectTool` (`:498`) has no springboard. The retro's "in no e2e suite" is stale: at HEAD, `cat-062b.test.ts` already drives `spring_forward` through raw picker coordinates (`choose`, `:113-116`). It also uses raw precision clicks (`:117-120`) and raw settings-editor clicks for the preload (`:137-139`). No dedicated, driver-level suite with snapshot-exact Reset exists.

**Approach:** Promote the lesson into the driver as a first-class level with real-control helpers. Then add `tools/e2e/spring-forward.test.ts`, which proves a preloaded launch, the passive authored solution, matched zero-preload and misplaced-board negatives, exact Reset and Save/Load.

## Boundaries & Constraints

**Always:**
- Common 9R rules:
  - Chrome proof uses actual UI controls through `workshop-driver.ts`. Read-only observations are the CCGPU acks (`engine/MachineWorld.Gpu.cs:363`), the pose ring and the IndexedDB slot.
  - Committed reads arrive only on command acks.
  - Exact Run/Reset uses the Story 9R.1 snapshot. Its contract: the tick-0 read from the last Construct or Load ack plus the authored bytes. Every body's cell, local, rotation, com, velocity and angularVelocity, plus the captures, activations, timers and electrical rows, are deep-equal after Reset (`cat-062b.test.ts:226-231` is the model).
  - Negatives run at Precise.
- Lesson fixtures come from `SpringboardLessons` (`engine/gpu/SpringboardLessons.cs:37-64`):
  - ball locked at (−3, 3.6, 0);
  - Receiver locked at (−0.7, 0.55, 0);
  - inventory one Springboard;
  - no connections.
  These match `content/puzzles.json:708`.
- The preload is set only through the Springboard settings editor (`ui/WorkshopConfiguration.cs:112-140`, field `SpringCompression`, 0–0.20 m).

**Never:**
- No setters, imported solutions or numeric placement.
- Do not delete or weaken `cat-062b` (it remains the CAT-062 preload suite).
- Do not change the springboard physics.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Charged | board at the cat-062b Charged pose, tilt −15°, preload 0.2 | ball launched (plate travel and ball vy > 1 m/s), 1 solve | N/A |
| Passive | board at the authored solution (−3, 0.8, 0), tilt −20°, preload 0 | ball rebounds from the plate, 1 solve (CAT-062 fact 8) | N/A |
| Zero (matched) | Charged pose, preload 0 | 0 solves; ball rests on the plate | N/A |
| Misplaced | Charged pose, preload 0.2, board yawed 180° about Y | 0 solves | N/A |
| Bad preload | text 0.21, −0.01, NaN | no Construct; authored bytes unchanged | editor rejects atomically |

</frozen-after-approval>

## Code Map

- `engine/gpu/SpringboardLessons.cs:31-64`, `:94-124` -- lesson fixtures and Validate (inventory and locks).
- `ui/WorkshopPuzzle.cs:57-62`, `:111`, `:142`; `ui/Workshop.cs:46` -- picker index 7, status text.
- `ui/WorkshopConfiguration.cs:112-140` -- the Springboard settings editor.
- `tools/e2e/workshop-driver.ts:200-212`, `:220-276`, `:310`, `:498-552` -- picker, dock, level union and tool rows.
- `tools/e2e/cat-062b.test.ts:113-195`, `:196-240` -- the existing raw-coordinate recipe and the replay and Reset loop.
- `CuriousContraptions.tests/WorkshopPhysicsEngineTests.cs:165` -- native forgiving-offset proof (CAT-062 fact 18).

## Tasks & Acceptance

**Execution:**
- [ ] `tools/e2e/workshop-driver.ts`:
  - add `spring_forward` (and `ready_to_rebound`, shared with 9R.12) to `WorkshopLevel`, with picker anchors and docks;
  - add a `springboard` tool row (authored single row);
  - add `setPrecision(level: 'forgiving' | 'balanced' | 'precise')` through the real menu slider;
  - add `editSpringboard({ stiffness?, damping?, compression? })` that types into the real editor fields and presses Apply;
  - add `expectNoCommand`, which asserts no new ack and unchanged authored bytes.
  Measure anchors on the 1440×900 viewport; don't copy them blindly.
- [ ] `tools/e2e/spring-forward.test.ts` (new) -- write the suite first, against the helpers. Cover the four matrix routes plus bad preload, each from a fresh `launch`.
- [ ] `tools/e2e/cat-062b.test.ts` -- replace the raw `choose` and `precision` with the driver helpers (same behaviour; removes a duplicated coordinate table).

**Acceptance Criteria:**
- Given `spring_forward` chosen from the real picker at Precise, when the player places the granted Springboard through the drawer, tilts it with the gizmo, sets the preload to 0.2 with the existing editor and presses Run, then the plate is released, the ball launches (pose vy > 1 m/s after contact) and exactly one solve is printed.
- Given the authored passive placement (−3, 0.8, 0) at −20° and zero preload, when run, then exactly one solve is printed (lesson as taught).
- Given the Charged pose with preload 0 (matched zero), or preload 0.2 with the board yawed 180° (misplaced), when run at Precise for ≥ 4 s, then no solve is printed.
- Given out-of-range or non-finite preload text, when Apply is pressed, then no Construct is issued and the authored bytes are unchanged.
- Given any route after Run, when Reset, then the committed read equals the pre-Run snapshot exactly (including plate precompression, CAT-062 §5), and a second Run gives the same solve count.
- Given a saved construction, when reloaded and Loaded, then the board's pose, stiffness, damping and preload are byte-identical, and the route replays its result.
- `tools/e2e/spring-forward.test.ts` passes 100% serially in Chrome.
- `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

**§5 map (CAT-062, this lesson):**
- Construction: AC1.
- Positive: AC1 and AC2.
- Negative/control: AC3 and AC4. The unloaded board is covered by `cat-062`.
- Boundaries: AC4. Travel stops and parameter endpoints are in `cat-062`.
- Run/Reset: AC5.
- Save/Load: AC6.
- Integrations: `ready_to_rebound` is in `cat-062b` and 9R.12. `spring_signal` and the composite lanes are Incomplete (not admitted).

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

**Decisions (owner playability delegation):**
- "Zero preload leaves the goal unsolved" is proven as a matched control at the Charged pose. Rationale: at the authored −20° pose, zero preload does solve. That is the taught passive lesson (CAT-062 fact 8, cat-062b `Route.Passive`). An unmatched reading would contradict the lesson.
- "Misplaced board" is the 180° yaw, cat-062b `Route.Away`. Rationale: an existing, proven, unambiguous failure.
- Precise is pinned for all routes. Rationale: the Forgiving Receiver guide (margin 0.3, acceleration 12) can rescue offsets (CAT-004 fact 14).

**Test-first plan:** write the routes first. Assert solve count from `waitForGoalSolved`, launch from pose-ring samples, and configuration from the decoded IndexedDB slot (preload `initialCompression`, rotation, position). Never set state.

**Mutation plan:** each change below must fail a test.
- Ignore `initialCompression` at Run (Charged fails).
- Make the editor accept 0.21 (the bad-preload row).
- Leave the plate velocity non-zero after Reset (snapshot).
- Drop the Receiver lock in `SpringboardLessons.Validate` (fixture assertion).

**Impacted suites:** `cat-062`, `cat-062b`, `WorkshopSpringboardTests`, `WorkshopPhysicsEngineTests`.

**Risks:**
- Picker anchors at y 277/304 differ from the driver's anchor table. Measure them; the 9R.7 `joined_pipe` item may shift later rows.
- Wall-time windows: use the solve wait, not fixed sleeps.

## Verification

**Commands:**
- `dotnet build CuriousContraptions.slnx` -- expected: 0 warnings.
- `dotnet test CuriousContraptions.slnx` -- expected: 100%.
- Serial Chrome run of `tools/e2e/spring-forward.test.ts`, `cat-062b.test.ts`, then all of `tools/e2e/*.test.ts` against the 8060 production preview -- expected: 100%.
