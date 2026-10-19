---
title: 'Story 9R.9: Ball Detector forward pass lights the lamp exactly once (Story 6.8 repair)'
type: 'bugfix'
created: '2026-10-18'
status: 'ready-for-dev'
route: ''
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r9-ball-detector-forward-pass-lights-the-lamp-exactly-once-story-68-repair'
  - '{project-root}/docs/planning/elements/CAT-002-ball_detector.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem (report #15):** `cat-002.test.ts:81-84` scans all animation targets, so the detector's own pulse satisfies "lamp lights", and "once" is never counted. The reverse and glancing checks (`:93-124`, `:126-155`) never prove a crossing occurred. The "second crossing" (`:171-182`) uses Reset.

Code investigation adds a capability gap: a second in-run crossing is unobservable today.
- A source's committed latch keeps only its first occurrence (`ActivationNetwork.cs:436-437`; a later passage is tolerated at `:422-425`).
- The activation animation pump requests one control per node per world (`BrowserWorkshopClient.Activation.cs:86-100`), and the sample `ordinal` is the per-sample pulse sequence, not the occurrence (`:116-117`; `workshop-client.js:227`).
- The per-body `PassedCount` lives in the committed `PassageSensorTracker` (`PassageSensorDeclaration.cs:160-240`; committed and candidate at `WorkshopGpuDevice.cs:144-149`, `:187`) but is not in `WorkshopRead` (`WorkshopGpuDevice.cs:156-159`).

**Approach:**
- Add one compiled Run-time committed row per passage sensor: occurrence count and last occurrence ordinal, carried in `WorkshopRead`, the wire and `workshop-wire.ts`.
- Rewrite cat-002 with target-scoped lamp counting.
- Prove the crossings from pose samples.
- Prove a same-run second crossing with a single ball that rebounds, so its own rearm is exercised.

## Boundaries & Constraints

**Always:**
- Common 9R rules:
  - Chrome proof uses actual UI controls through `workshop-driver.ts`.
  - Committed reads arrive only on command acks (sample at Pause or Step).
  - Exact Run/Reset uses the Story 9R.1 snapshot. Its contract: the tick-0 read from the last Construct or Load ack plus the authored bytes. Bodies, captures, activations, timers, electrical rows and the new passage rows are deep-equal after Reset (model: `cat-062b.test.ts:226-231`).
- The Signal lamp is `StickyFirstInput` (`ActivationNetwork.cs:9-12`, `:443-445`). It latches once per run.
- The new row is part of the committed checkpoint. A discarded candidate tick leaves it unchanged.

**Never:**
- No any-target predicates.
- Do not count samples as activations.
- Don't drive the detector indicator re-pulse from the read-diff pump (Story 9A.23 owns event-driven one-shots).
- No Reset in place of in-run clearance.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Forward | detector forward = down, ball dropped through the bore | passage count 1; lamp target exactly one activation | N/A |
| Reverse | detector forward = up, same drop | pose above then below the plane, radial < 0.65 at the crossing; count 0; lamp target no sample | N/A |
| Glancing | drop offset radial > 0.65 | pose crosses the plane outside the aperture; count 0; lamp target no sample | N/A |
| Rebound | low detector, ball bounces back above the rearm clearance (r + 0.02) and falls through again | count goes 1 → 2; lamp stays at exactly 1 | N/A |
| Jitter | second rebound apex below the rearm threshold | count stays 2 | N/A |

</frozen-after-approval>

## Code Map

- `engine/gpu/PassageSensorDeclaration.cs:27`, `:90-116`, `:160-240` -- per-body Armed/Unarmed state, `PassedCount`, fire.
- `CuriousContraptions.Simulation/WorkshopGpuDevice.cs:140-160`, `:165-200` -- candidate and committed tracker; the read build.
- `engine/gpu/WorkshopRead.cs`, `engine/gpu/WorkshopWireFlatBuffers.cs` (response encode), the FlatBuffers schema, `tools/e2e/workshop-wire.ts` (generated reader) -- the new row.
- `engine/gpu/ActivationNetwork.cs:415-445` -- first-occurrence latch and sticky lamp.
- `engine/gpu/BrowserWorkshopClient.Activation.cs:86-117` -- the one-control-per-node pump.
- `tools/e2e/cat-002.test.ts` -- the suite being repaired. Lamp target = node + 2 (`anim-1b.test.ts:13-14` convention).

## Tasks & Acceptance

**Execution:**
- [ ] `CuriousContraptions.tests/PassageSensorReadTests.cs` (new), red first:
  - one row per sensor; count and last ordinal per fire;
  - a discarded candidate leaves the committed row unchanged;
  - admission row is zero;
  - wire round trip;
  - an unknown sensor is rejected.
- [ ] `engine/gpu/WorkshopRead.cs`, `WorkshopGpuDevice.cs`, the wire, the schema, `tools/e2e/workshop-wire.ts` -- emit committed passage rows from the committed tracker. Admission validation requires zero rows' state.
- [ ] `tools/e2e/cat-002.test.ts`:
  - delete the any-target predicate (`:81-84`);
  - count lamp activations by target with `readAnimationSamplesForTarget(lampTarget)`: one monotonic run to 1 with no second rise, and the committed lamp latch's `eventOrdinal` equals the detector's first passage ordinal;
  - prove reverse and glancing crossings from pose samples;
  - add the in-run rebound case;
  - assert snapshot Reset.
- [ ] `tools/e2e/workshop-driver.ts` -- add `readCommittedAtPause()`, which returns the decoded read of the Pause ack (no new control).

**Acceptance Criteria:**
- Given a detector wired to a lamp through the real Connect controls, when a ball passes forward, then the lamp target alone shows exactly one activation and the committed passage count is 1.
- Given reverse and glancing drops, when run, then pose samples on both sides of the aperture plane prove each crossing happened (inside the radius for reverse, outside for glancing), the count is 0, and the lamp target has zero samples.
- Given a low detector and a ball that rebounds fully upstream (centre local x < −(0.02 + r) in pose samples) and falls through again in the same run, then the committed count goes from exactly 1 to exactly 2. The lamp stays at exactly one activation.
- Given any case after Run, when Reset, then the committed read, including the passage rows, equals the pre-Run snapshot. Save/Load preserves the wire (§5).
- `cat-002` passes 100% serially in Chrome.
- `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

**§5 map (CAT-002):**
- Construction: AC1.
- Positive: AC1. The per-crossing indicator re-pulse is deferred to 9A.23.
- Negative/control: AC2 and the Jitter row. Stationary and disabled-participant cases are native or Incomplete.
- Boundaries: rotated detector (AC1 runs at −90°) and the second crossing (AC3). The 120 m/s pass is native.
- Run/Reset and Save/Load: AC4.
- Integrations: Counter (`cat-020`). Hold timer is Incomplete (CAT-033 not delivered). Pipe mouths belong to 9R.6.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

**Decisions (owner playability delegation):**
- "Exactly one more" is the detector's committed occurrence count (1 → 2). The lamp stays at one. Rationale: the lamp is sticky first-input by design (CAT-035 §2; 9R.16 "stays lit"), so a second lamp activation would be a defect.
- The rebound recipe is a single ball, not two balls. Rationale: rearm is tracked per body (`PassageBodyState`), so a second ball never exercises clearance.
- The detector indicator re-pulse per crossing (CAT-002 §2 cosmetic) is deferred to Story 9A.23. Rationale: 9A.23 replaces the read-diff pump with event-ring one-shots, so building it now would be throwaway.

**Rebound sizing (verify with pose samples):** bench restitution 0.55 gives a 0.30 height ratio.
- A drop from the 3 m placement plane onto a detector lowered to y ≈ 0 rebounds to a centre of about 0.8 m. That clears the 0.36 m rearm threshold.
- The second rebound reaches about 0.16 m, which does not rearm.
- Adjust the heights with the lift handle until both hold.

**§5 items not reachable in UI:**
- Disabled participant: native `BallDetectorTests`, otherwise Incomplete.
- 120 m/s pass: native.

**Test-first plan:** the native row tests come first. The Chrome cases are written before the row lands and fail on the missing `passages` accessor.

**Mutation plan:** each change below must fail a test.
- Restore the any-target predicate (the lamp-only test must fail with the detector-only wiring control).
- Skip rearm (rebound count stays 1).
- Count the reverse direction (reverse row).
- Write the row from the candidate tracker (discard test).

**Impacted suites:** `cat-020` (Counter driven by detectors), `cat-048a/b` if detectors sit on pipes, `anim-*`, `PassageSensorTests`, `CounterTests`.

**Risks:**
- Rebound lateral drift out of the bore. Assert the radial distance at each crossing.
- The wire schema change touches every read consumer. Regenerate `workshop-wire.ts`; don't hand-edit it.

## Verification

**Commands:**
- `dotnet build CuriousContraptions.slnx` -- expected: 0 warnings.
- `dotnet test CuriousContraptions.slnx` -- expected: 100%.
- Serial Chrome run of `tools/e2e/cat-002.test.ts`, then all of `tools/e2e/*.test.ts` against the 8060 production preview -- expected: 100%.
