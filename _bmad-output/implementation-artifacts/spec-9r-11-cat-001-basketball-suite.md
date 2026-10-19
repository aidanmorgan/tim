---
title: 'Story 9R.11: Basketball suite cat-001 (reopened Story 6.5d)'
type: 'feature'
created: '2026-10-18'
status: 'ready-for-dev'
route: ''
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r11-basketball-suite-cat-001-reopened-story-65d'
  - '{project-root}/docs/planning/elements/CAT-001-ball.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem (report #8):** Story 6.5d named `tools/e2e/cat-001.test.ts`, but it never existed (`ls tools/e2e`). The Basketball's roll, bounce, rest, Reset and Save/Load behaviour has no element suite. Today it is touched only incidentally by `engine-core-2*` and `cat-014`.

**Approach:** Add one suite following CAT-001 §5. It builds the ball through the real drawer in the free workshop, and solves `first_principles` with its locked ball and two drawer-placed ramps. All observations are read-only: pose ring, CCGPU acks, `CCGOAL_SOLVED` and the IndexedDB slot.

## Boundaries & Constraints

**Always:**
- Common 9R rules:
  - Chrome proof uses actual UI controls through `workshop-driver.ts`.
  - Committed reads arrive only on command acks.
  - Exact Run/Reset uses the Story 9R.1 snapshot. Its contract: the tick-0 read from the last Construct or Load ack plus the authored bytes. Every body's cell, local, rotation, com, velocity and angularVelocity, plus the captures, activations, timers and electrical rows, are deep-equal after Reset (model: `cat-062b.test.ts:226-231`).
  - Negatives in authored levels run at Precise.
- The ball's declared material per kind:
  - radius 0.34; 1 kg; bounce 0.55; drag 0.04; friction 0.3; rolling resistance 0.035;
  - from `BallMaterial.For(Basketball)` in `engine/gpu/WorkshopConstruction.cs`.
- Bench surface at y −0.46, so a resting centre sits at −0.12 (CAT-001 §2).
- A defect found stops the story and is fixed here (epic rule).

**Never:**
- No setters, imported solutions or numeric placement.
- No new engine behaviour unless a defect is found.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Free ramp | drawer Basketball lifted above a drawer Ramp (−20°) feeding a drawer Receiver | rolls down the ramp; Receiver capture latch Latched | N/A |
| first_principles | locked ball, two drawer ramps at the engine-core-2c recipe | exactly 1 `CCGOAL_SOLVED` | N/A |
| Bench rest | ball placed at the lowest placement height and allowed to settle | after settling, \|Δx\|, \|Δz\| ≤ 1e−3 m over 3 s; height within +0.002 of −0.12; speed ≤ 0.05 m/s | N/A |
| Bounce | drop from the 3 m plane | second apex lower than the first; no penetration > 0.001 m | N/A |
| No ball | free workshop, ramp only | Run read has no dynamic body; no contact | N/A |
| Missing ramp | first_principles at Precise with one ramp | 0 solves | N/A |

</frozen-after-approval>

## Code Map

- `engine/gpu/WorkshopConstruction.cs:52-62` -- `BallMaterial` and `BallMaterial.For`.
- `engine/gpu/WorkshopPuzzle.cs:66-76` -- authored levels admit exactly one named Basketball.
- `engine/gpu/WorkshopPuzzle.cs` `class FirstPrinciples` -- the locked ball (−4, 6.5, 0) and Receiver (2.5, 0.9, 0), inventory `{ramp: 2}` (`content/puzzles.json:3`).
- `ui/WorkshopGuidance.cs:153-156` -- placement clamp (Y 0–9), so the highest and lowest legal placements are 9 and 0.
- `tools/e2e/workshop-driver.ts:178-199`, `:498-552`, `:652-656`, `:805-811` -- palette rows, lift and stacking.
- `tools/e2e/engine-core-2c.test.ts:46-54` -- the proven two-ramp recipe.
- `CuriousContraptions.tests/WorkshopPhysicsEngineTests.cs` -- native rest, bounce and free fall.

## Tasks & Acceptance

**Execution:**
- [ ] `tools/e2e/cat-001.test.ts` (new) -- write the cases in the matrix order. Each test is a fresh `launch` or `reload` (no state reuse). Assertions come from pose samples and decoded saves.
- [ ] `tools/e2e/workshop-driver.ts` -- add only what's missing: `setPrecision` (shared with 9R.8) and `readCommittedAtPause` (shared with 9R.9).
- [ ] Defect fixes, only if a case finds one, with a native regression test added first.

**Acceptance Criteria:**
- Given a Basketball placed through the real drawer above a drawer-placed, tilted Ramp leading to a drawer-placed Receiver, when run, then the ball rolls down the ramp (pose x and y follow the slope) and the committed capture latch becomes Latched.
- Given `first_principles` with its locked Basketball and two drawer-placed ramps, when run, then exactly one solve is printed. With one ramp at Precise, none is.
- Given a ball settled on the flat bench, when observed for 3 s more, then it shows no drift: \|Δx\|, \|Δz\| ≤ 1e−3 m and speed ≤ 0.05 m/s (control).
- Given a drop from the 3 m plane, then the second apex is lower than the first. Given placements at the highest (9 m) and lowest (0 m) legal heights, then the contact order follows the height.
- Given any case after Run, when Reset, then the read equals the pre-Run snapshot exactly. When Save, reload and Load, then the ball's kind, pose and material bits are byte-identical.
- `cat-001` passes 100% serially in Chrome.
- `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

**§5 map (CAT-001):**
- Construction: AC1.
- Positive: AC1, AC2 and AC4. Momentum transfer is covered by `cat-023a/b`.
- Negative/control: AC3, the No-ball row, and the missing ramp in AC2.
- Boundaries: AC4. 40 m/s is native and `engine-core-2b3`.
- Run/Reset and Save/Load: AC5.
- Integrations: Ramp and Receiver (AC1 and AC2). Switch is 9R.15, Bumper `cat-015*`, Domino `cat-023*`, pipes `cat-048*`/`049`/`050`.
- R-N3 and buoyancy: Incomplete (owner and Epic 12).

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

**Decisions (owner playability delegation):**
- The AC "Basketball placed through the real drawer on a Ramp … reaches the Receiver in `first_principles`" is split into two proofs: a free-workshop drawer build, and the `first_principles` solve with its locked ball. Rationale: `first_principles` grants only ramps and admits only its named ball (`WorkshopPuzzle.cs:66-76`), so a drawer ball cannot exist there. Both halves of the AC are kept.
- No per-instance material editing. Rationale: CAT-001 §6 leaves it an open owner decision; per-kind material is the current declaration.

**§5 items not reachable in UI:**
- 40 m/s impact without tunnelling: native `WorkshopPhysicsEngineTests` and `engine-core-2b3`.
- Momentum transfer to a Domino: covered by `cat-023a/b`; cite them, don't duplicate.
- R-N3 stays Fail (CAT-001 §6).
- Non-zero buoyancy waits for Epic 12.

**Test-first plan:** the suite is written and run red against the current preview first. Every expectation that passes immediately is kept as regression evidence, and any failing behaviour is a defect stop.

**Mutation plan:** each change below must fail a case.
- Set Basketball restitution to 1 (bounce apex).
- Remove rolling resistance (drift control).
- Remove a ramp (missing-ramp control).
- Skip the material in the save encode (Save/Load).

**Impacted suites:** `engine-core-2a1`, `2a2`, `2a4`, `2b1`, `2b2`, `2c`, `cat-014`, `anim-1a`.

**Risks:**
- Lowest-placement settling may already overlap the receiver or bench margin. Choose a clear bench area.
- Rolling to rest takes long. Bound waits by condition with a 10 s ceiling.

## Verification

**Commands:**
- `dotnet build CuriousContraptions.slnx` -- expected: 0 warnings.
- `dotnet test CuriousContraptions.slnx` -- expected: 100%.
- Serial Chrome run of `tools/e2e/cat-001.test.ts`, then all of `tools/e2e/*.test.ts` against the 8060 production preview -- expected: 100%.
