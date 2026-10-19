---
title: 'Story 9R.15: Impact Switch suite cat-063 (reopened Story 6.5e)'
type: 'feature'
created: '2026-10-18'
status: 'ready-for-dev'
route: ''
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r15-impact-switch-suite-cat-063-reopened-story-65e'
  - '{project-root}/docs/planning/elements/CAT-063-switch.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem (report #8):** `tools/e2e/cat-063.test.ts` never existed. The switch's single latch on a qualifying hit, its below-threshold and miss controls, the wired lamp, Reset and Save/Load have no element suite. Today only `anim-1b` touches it (the depression curve).

**Approach:** Add one free-workshop suite for activation mode only, following CAT-063 §5. It builds switch → lamp through the real Connect controls and uses matched drop gaps for above and below the threshold. Committed latch reads (`phase`, `approachSpeed`, `eventOrdinal`) come from Pause and Reset acks, and the lamp count comes from its own animation target.

## Boundaries & Constraints

**Always:**
- Common 9R rules:
  - Chrome proof uses actual UI controls through `workshop-driver.ts`.
  - Committed reads arrive only on command acks.
  - Exact Run/Reset uses the Story 9R.1 snapshot. Its contract: the tick-0 read from the last Construct or Load ack plus the authored bytes. Every body's cell, local, rotation, com, velocity and angularVelocity, plus the captures, activations, timers and electrical rows, are deep-equal after Reset (model: `cat-062b.test.ts:226-231`).
- Trigger rule: pre-response normal approach speed ≥ threshold, inclusive (`ContactTriggerDeclaration`, `engine/gpu/ContactTriggerDeclaration.cs`). The free-workshop threshold is the default 0.8 m/s (`ContactTriggerSettings`, `engine/gpu/WorkshopActivationParts.cs:6-10`). The trigger latches once per Run.
- The latch records the cause `ApproachSpeed` (`ActivationLatch`, `engine/gpu/ActivationNetwork.cs:24-27`).
- The electrical mode (PowerIn and Supply) is Story 9.5b and out of scope.

**Never:**
- No threshold setter. No editor exists (`ui/WorkshopConfiguration.cs:10-16`); one is not invented here.
- No any-target predicate.
- No numeric placement.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Qualifying | switch stacked under a ball lifted ~0.1 m+ above the button (gap ≥ 0.06 m) | pre-contact \|vy\| ≥ 0.8 (pose); switch latch Latched, approach ≥ 0.8; lamp target exactly one activation | N/A |
| Below | same build, gap < 0.03 m (pre-contact \|vy\| < 0.8) | ball comes to rest on the button (pose proves contact); latch Clear; lamp target no sample | N/A |
| Missed | ball dropped beside the switch | no contact (pose); latch Clear; lamp dark | N/A |
| Duplicate | qualifying ball bounces on the button again | still exactly one latch and one lamp activation | N/A |
| Disconnected | qualifying hit, no wire | switch latches; lamp dark | N/A |
| Reset | after a hit | snapshot equal; switch Clear, button cosmetic neutral (raised) | N/A |

</frozen-after-approval>

## Code Map

- `engine/gpu/WorkshopActivationParts.cs:5-35` -- `ContactTriggerSettings`, `WorkshopSwitch`, `WorkshopLamp`.
- `engine/gpu/ContactTriggerDeclaration.cs`; `engine/gpu/WorkshopPhysicsCompiler.cs` (`ContactTrigger`) -- trigger compile on the owner body; both boxes qualify (CAT-063 §6).
- `engine/gpu/ActivationNetwork.cs:24-35`, `:415-445`, `:700-709` -- latch, the sticky lamp, lamp kind selection.
- `ui/WorkshopConnections.cs:49-95` -- the real "Connect ActivationOut" row and the target choice.
- `tools/e2e/workshop-driver.ts:178-199`, `:795-811` -- palette rows, `connectActivation` (no free-workshop switch row), `stackUnderLiftedBall` (wall, bumper and receiver only).
- `tools/e2e/cat-037.test.ts:63-71` -- the raw-row free-workshop connect recipe. `tools/e2e/anim-1b.test.ts:13-14` -- target id convention.

## Tasks & Acceptance

**Execution:**
- [ ] `tools/e2e/workshop-driver.ts`:
  - `connectFrom(source, socketRow, target, choice)` for free-workshop sources, measured from the real drawer (shared with 9R.10, 9R.16 and 9R.17);
  - `'switch' | 'lamp'` added to the `stackUnderLiftedBall` kinds;
  - `readCommittedAtPause` (shared).
- [ ] `tools/e2e/cat-063.test.ts` (new) -- the matrix cases, each from a fresh page. The drop gap is computed from the saved ball and switch poses, and the approach from pose samples just before contact.
- [ ] Defect fixes, only if a case finds one, with a native `ContactActivationTests` case first.

**Acceptance Criteria:**
- Given an Impact Switch wired to a Signal lamp through the real wiring UI, when a ball strikes the button at or above 0.8 m/s, then the switch latches exactly once (committed approach ≥ 0.8) and the lamp target shows exactly one activation.
- Given a matched below-threshold drop, or a missed ball, when run, then the switch stays Clear and the lamp target has no sample. Pose samples prove contact for the below case and no contact for the miss.
- Given a hit, when Reset, then the read equals the pre-Run snapshot (switch Clear, button raised).
- Given Save, reload and Load, then the wire (ActivationOut → ActivationIn) and the threshold bits (0.8) are byte-identical, and the hit replays.
- `cat-063` passes 100% serially in Chrome.
- `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

**§5 map (CAT-063, activation mode):**
- Construction: AC1.
- Positive: AC1.
- Negative/control: AC2, plus the Duplicate and Disconnected rows. Wrong domain uses `expectNoCommand`.
- Boundaries: native `ContactActivationTests` (below, equal and above; base-box contact is an owner decision).
- Run/Reset: AC3.
- Save/Load: AC4.
- Integrations: lamp (here), Delay (9R.17), latch (`cat-037`). Electrical mode, `power_trip` and `switched_motor` are Incomplete (Story 9.5b and later levels).

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

**Decisions (owner playability delegation):**
- Both negatives are required, not either one. Rationale: "below-threshold or missed" names two distinct failure modes, and only one would be a weaker proof.
- Below and above are matched builds that differ only in drop gap. The gap needs gap < v²/2g = 0.0326 m for 0.8 m/s, and the drop is verified by the measured approach, not by lift pixels. Rationale: a robust, physical proof.
- The threshold stays at the default 0.8 because no editor exists. Story 9A.35 turns the threshold into a declared parameter but adds no editor, so a player-facing threshold editor has no scheduled owner. That is reported to the coordinator.

**§5 items not reachable in UI:**
- Approach speed just below, equal to and just above the threshold (inclusive), and the base-box-only hit: native `ContactActivationTests`. Add the equality case if it is missing. The base-box outcome is an open owner decision (CAT-063 §6).
- Wrong domain: the lamp has no electrical input, so the UI offers no choice. Cover it with `expectNoCommand` on a switch → Battery attempt.
- The electrical contract: Story 9.5b.

**Test-first plan:** the suite runs red against the current build. Failures are defect stops.

**Mutation plan:** each change below must fail a case.
- Make the threshold exclusive (the equality native case).
- Use 0 as the threshold (Below row).
- Remove the single-latch guard (Duplicate row).
- Leave the latch on Reset (snapshot).

**Impacted suites:** `anim-1b`, `anim-1c`, `cat-037`, `cat-023b`, `ContactActivationTests`, `WorkshopActivationAnimationTests`.

**Risks:**
- A gap below 0.03 m is a fine lift drag (about 1.7 px). If the lift handle cannot reach it, stack the ball on the button at rest (gap ≈ 0). Prove contact and zero approach from pose, and record the recipe.

## Verification

**Commands:**
- `dotnet build CuriousContraptions.slnx` -- expected: 0 warnings.
- `dotnet test CuriousContraptions.slnx` -- expected: 100%.
- Serial Chrome run of `tools/e2e/cat-063.test.ts`, then all of `tools/e2e/*.test.ts` against the 8060 production preview -- expected: 100%.
